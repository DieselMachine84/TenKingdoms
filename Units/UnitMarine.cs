using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class UnitMarine : Unit
{
	public const int MAX_STOP_FOR_SHIP = 3;

	public const int NO_EXTRA_MOVE = 0;
	public const int EXTRA_MOVING_IN = 1;
	public const int EXTRA_MOVING_OUT = 2;
	public const int EXTRA_MOVE_FINISH = 3;

	//TODO make it work
	//public Sprite splash;

	public int ExtraMoveInBeach { get; set; }
	public bool InBeach { get; set; }
	public List<int> UnitsOnBoard { get; } = new List<int>();
	public AttackInfo ShipAttackInfo { get; set; }
	public DateTime LastLoadGoodsDate { get; set; }

	private int JourneyStatus { get; set; }
	private int DestStopId { get; set; } // destination stop id. the stop which the train currently is moving towards
	public int StopDefinedNum { get; set; } // num of stop defined
	private int WaitCount { get; set; } // set to -1 to indicate only one stop is specified

	private int StopLocX { get; set; } // the x location the unit entering the stop
	private int StopLocY { get; set; } // the y location the unit entering the stop

	public bool AutoMode { get; set; }
	//TODO why not use ActionParam as in UnitCaravan?
	private int CurFirmId { get; set; } // the id of current firm the ship entered
	private int CarryGoodsCapacity { get; set; }

	public ShipStop[] Stops { get; } = new ShipStop[MAX_STOP_FOR_SHIP];

	public int[] RawQty { get; } = new int[GameConstants.MAX_RAW];
	public int[] ProductQty { get; } = new int[GameConstants.MAX_PRODUCT];
	
	//TODO check this
	private List<(Firm, int)> NotAutoLoadRawGoods { get; } = new List<(Firm, int)>();
	private List<(Firm, int)> NotAutoLoadProductGoods { get; } = new List<(Firm, int)>();
	
	private readonly List<int> _linkedMines = new List<int>();
	private readonly List<int> _linkedFactories = new List<int>();
	private readonly List<int> _linkedMarkets = new List<int>();

	public UnitMarine()
	{
		for (int i = 0; i < Stops.Length; i++)
			Stops[i] = new ShipStop();

		JourneyStatus = InternalConstants.ON_WAY_TO_FIRM;
		StopLocX = -1;
		StopLocY = -1;
		Loyalty = 100;
		AutoMode = true;
	}

	public override void Init(int unitType, int nationId, int rank, int unitLoyalty, int startLocX, int startLocY)
	{
		base.Init(unitType, nationId, rank, unitLoyalty, startLocX, startLocY);
		
		ExtraMoveInBeach = NO_EXTRA_MOVE;
		CarryGoodsCapacity = UnitRes[unitType].carry_goods_capacity;
		LastLoadGoodsDate = Info.game_date;

		//int spriteId = SpriteInfo.GetSubSpriteInfo(1).SpriteId;
		//splash.init(spriteId, cur_x_loc(), cur_y_loc());
		//splash.cur_frame = 1;
	}

	public override void Deinit()
	{
		foreach (var unitId in UnitsOnBoard)
		{
			if (!UnitArray.IsDeleted(unitId))
				UnitArray.DeleteUnit(UnitArray[unitId]);
		}

		base.Deinit();
	}

	public void DelUnit(int unitId)
	{
		for (int i = UnitsOnBoard.Count - 1; i >= 0; i--)
		{
			if (UnitsOnBoard[i] == unitId)
			{
				UnitsOnBoard.RemoveAt(i);
				return;
			}
		}
	}
	
	public override void PreProcess()
	{
		base.PreProcess();

		//TODO condition is not the same as in UnitCaravan
		if (HitPoints <= 0.0 || ActionMode == UnitConstants.ACTION_DIE || CurAction == SPRITE_DIE)
			return;

		if (ActionMode2 >= UnitConstants.ACTION_ATTACK_UNIT && ActionMode2 <= UnitConstants.ACTION_ATTACK_WALL)
			return; // don't process trading if unit is attacking

		if (AutoMode) // process trading automatically, same as caravan
		{
			if (JourneyStatus == InternalConstants.INSIDE_FIRM)
			{
				ShipInFirm();
				return;
			}

			//TODO condition is not the same as in UnitCaravan
			if (StopDefinedNum == 0)
				return;

			//-----------------------------------------------------------------------------//
			// wait in the surrounding of the stop if StopDefinedNum == 1 (only one stop)
			//-----------------------------------------------------------------------------//
			if (StopDefinedNum == 1)
			{
				ShipStop stop = Stops[0];

				if (FirmArray.IsDeleted(stop.FirmId))
				{
					UpdateStopList();
					return;
				}

				Firm firm = FirmArray[stop.FirmId];
				if (firm.LocX1 != stop.FirmLocX1 || firm.LocY1 != stop.FirmLocY1)
				{
					UpdateStopList();
					return;
				}

				int curLocX = NextLocX;
				int curLocY = NextLocY;
				int moveStep = MoveStepCoeff();

				if (curLocX < firm.LocX1 - moveStep || curLocX > firm.LocX2 + moveStep || curLocY < firm.LocY1 - moveStep || curLocY > firm.LocY2 + moveStep)
				{
					if (!IsVisible())
						return; // may get here if player manually ordered ship to dock
					
					if (CurAction == SPRITE_IDLE)
						MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, firm.FirmType);
					else
						JourneyStatus = InternalConstants.ON_WAY_TO_FIRM;
				}
				else
				{
					//TODO condition is not the same as in UnitCaravan
					if (CurX == NextX && CurY == NextY && CurAction == SPRITE_IDLE)
					{
						JourneyStatus = InternalConstants.SURROUND_FIRM;
						if (NationArray[NationId].get_relation(firm.NationId).trade_treaty)
						{
							if (WaitCount <= 0)
							{
								//---------- unloading goods -------------//
								CurFirmId = stop.FirmId;
								GetHarborLinkedFirmInfo();
								HarborUnloadGoods();
								WaitCount = GameConstants.MAX_SHIP_WAIT_TERM * InternalConstants.SURROUND_FIRM_WAIT_FACTOR;
								CurFirmId = 0;
							}
							else
							{
								WaitCount--;
							}
						}
					}
				}

				return;
			}

			//------------ if there are more than one defined stop ---------------//
			ShipOnWay();
		}
		else
		{
			if (JourneyStatus == InternalConstants.INSIDE_FIRM)
				ShipInFirm();
		}
	}
	
	private void ShipInFirm()
	{
		//-----------------------------------------------------------------------------//
		// the harbor is deleted while the ship is in harbor
		//-----------------------------------------------------------------------------//
		if (CurFirmId != 0 && FirmArray.IsDeleted(CurFirmId))
		{
			HitPoints = 0.0; // ship also die if the harbor is deleted
			UnitArray.DisappearInFirm(SpriteId); // ship also die if the harbor is deleted
			return;
		}

		//-----------------------------------------------------------------------------//
		// waiting (time to upload/download cargo)
		//-----------------------------------------------------------------------------//
		if (WaitCount > 0)
		{
			WaitCount--;
			return;
		}

		//-----------------------------------------------------------------------------//
		// leave the harbor and go to another harbor if possible
		//-----------------------------------------------------------------------------//
		int locX = StopLocX;
		int locY = StopLocY;
		Location location = World.GetLoc(locX, locY);
		Firm firm;

		if (locX % 2 == 0 && locY % 2 == 0 && location.CanMove(MobileType))
		{
			InitSprite(locX, locY); // appear in the location the unit disappeared before
		}
		else
		{
			//---- the entering location is blocked, select another location to leave ----//
			firm = FirmArray[CurFirmId];

			if (AppearInFirmSurround(ref locX, ref locY, firm))
			{
				InitSprite(locX, locY);
				Stop();
			}
			else
			{
				WaitCount = GameConstants.MAX_SHIP_WAIT_TERM * 10; //********* BUGHERE, continue to wait or ....
				return;
			}
		}

		int nextStopId = GetNextStopId(DestStopId);
		if (nextStopId == 0 || DestStopId == nextStopId)
		{
			DestStopId = nextStopId;
			JourneyStatus = (nextStopId == 0) ? InternalConstants.NO_STOP_DEFINED : InternalConstants.SURROUND_FIRM;
			return; // no stop or only one stop is valid
		}

		DestStopId = nextStopId;
		firm = FirmArray[Stops[DestStopId - 1].FirmId];

		CurFirmId = 0;
		JourneyStatus = InternalConstants.ON_WAY_TO_FIRM;

		if (AutoMode)
			MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, firm.FirmType);
	}
	
	//TODO rewrite
	private bool AppearInFirmSurround(ref int locX, ref int locY, Firm firm)
	{
		FirmInfo firmInfo = FirmRes[firm.FirmType];
		int firmWidth = firmInfo.LocWidth;
		int firmHeight = firmInfo.LocHeight;
		int smallestCount = firmWidth * firmHeight + 1;
		int largestCount = (firmWidth + 2) * (firmHeight + 2);
		int countLimit = largestCount - smallestCount;
		int count = Misc.Random(countLimit) + smallestCount;
		int checkLocX = 0, checkLocY = 0;
		bool found = false;

		//-----------------------------------------------------------------//
		for (int i = 0; i < countLimit; i++)
		{
			Misc.cal_move_around_a_point(count, firmWidth, firmHeight, out int xOffset, out int yOffset);
			checkLocX = firm.LocX1 + xOffset;
			checkLocY = firm.LocY1 + yOffset;

			if (checkLocX % 2 != 0 || checkLocY % 2 != 0 || !Misc.IsLocationValid(checkLocX, checkLocY))
			{
				count++;
				continue;
			}

			Location location = World.GetLoc(checkLocX, checkLocY);
			if (location.CanMove(MobileType))
			{
				found = true;
				break;
			}

			count++;
			if (count > largestCount)
				count = smallestCount;
		}

		if (found)
		{
			locX = checkLocX;
			locY = checkLocY;
			return true;
		}

		return false;
	}

	private int GetNextStopId(int curStopId)
	{
		int nextStopId = (curStopId >= StopDefinedNum) ? 1 : curStopId + 1;

		ShipStop stop = Stops[nextStopId - 1];

		if (!FirmArray.IsDeleted(stop.FirmId))
		{
			Firm firm = FirmArray[stop.FirmId];
			if (firm.LocX1 == stop.FirmLocX1 && firm.LocY1 == stop.FirmLocY1 && CanSetStop(stop.FirmId))
			{
				return nextStopId;
			}
		}

		int preStopId = Stops[curStopId - 1].FirmId;
		
		UpdateStopList();

		if (StopDefinedNum == 0)
			return 0; // no stop is valid

		//TODO check this loop and return values
		for (int i = 1; i <= StopDefinedNum; i++)
		{
			stop = Stops[i - 1];
			if (stop.FirmId == preStopId)
				return (i >= StopDefinedNum) ? 1 : i + 1;
		}

		return 1;
	}

	private void ShipOnWay()
	{
		ShipStop stop = Stops[DestStopId - 1];
		Firm firm;
		int nextLocX;
		int nextLocY;
		int moveStep;

		if (CurAction == SPRITE_IDLE && JourneyStatus != InternalConstants.SURROUND_FIRM)
		{
			if (!FirmArray.IsDeleted(stop.FirmId))
			{
				firm = FirmArray[stop.FirmId];
				MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, firm.FirmType);
				nextLocX = NextLocX;
				nextLocY = NextLocY;
				moveStep = MoveStepCoeff();
				if (nextLocX >= firm.LocX1 - moveStep && nextLocX <= firm.LocX2 + moveStep && nextLocY >= firm.LocY1 - moveStep && nextLocY <= firm.LocY2 + moveStep)
					JourneyStatus = InternalConstants.SURROUND_FIRM;

				return;
			}
		}

		if (UnitArray.IsDeleted(SpriteId))
			return; //-***************** BUGHERE ***************//

		if (FirmArray.IsDeleted(stop.FirmId))
		{
			UpdateStopList();

			//TODO choose better to which firm to move to
			if (StopDefinedNum != 0) // move to next stop
			{
				firm = FirmArray[Stops[StopDefinedNum - 1].FirmId];
				MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, firm.FirmType);
			}

			return;
		}

		firm = FirmArray[stop.FirmId];
		nextLocX = NextLocX;
		nextLocY = NextLocY;
		moveStep = MoveStepCoeff();
		if (JourneyStatus == InternalConstants.SURROUND_FIRM ||
		    (nextLocX == MoveToLocX && nextLocY == MoveToLocY && CurX == NextX && CurY == NextY && // move in a tile exactly
		     nextLocX >= firm.LocX1 - moveStep && nextLocX <= firm.LocX2 + moveStep && nextLocY >= firm.LocY1 - moveStep && nextLocY <= firm.LocY2 + moveStep))
		{
			ExtraMoveInBeach = NO_EXTRA_MOVE; // since the ship may enter the firm in odd location		

			//-------------------------------------------------------//
			// load/unload goods
			//-------------------------------------------------------//
			CurFirmId = stop.FirmId;

			if (NationArray[NationId].get_relation(firm.NationId).trade_treaty)
			{
				GetHarborLinkedFirmInfo();
				HarborUnloadGoods();
				if (stop.PickUpType == TradeStop.AUTO_PICK_UP)
					HarborAutoLoadGoods();
				else if (stop.PickUpType != TradeStop.NO_PICK_UP)
					HarborLoadGoods();
			}

			StopLocX = MoveToLocX; // store entering location
			StopLocY = MoveToLocY;
			WaitCount = GameConstants.MAX_SHIP_WAIT_TERM; // set waiting term

			ResetPath();
			DeinitSprite(true); // the ship enters the harbor now

			CurX = -2; // set CurX to -2, such that invisible but still process in PreProcess()

			JourneyStatus = InternalConstants.INSIDE_FIRM;
		}
		else
		{
			if (CurAction != SPRITE_MOVE)
			{
				//----------------------------------------------------//
				// blocked by something, go to the destination again
				// note: if return value is 0, cannot reach the firm.		//*********BUGHERE
				//----------------------------------------------------//
				MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, firm.FirmType);
				JourneyStatus = InternalConstants.ON_WAY_TO_FIRM;
			}
		}
	}
	
	private void GetHarborLinkedFirmInfo()
	{
		FirmHarbor firmHarbor = (FirmHarbor)FirmArray[CurFirmId];

		_linkedMines.Clear();
		_linkedFactories.Clear();
		_linkedMarkets.Clear();
		
		for (int i = 0; i < firmHarbor.LinkedFirms.Count; i++)
		{
			if (firmHarbor.LinkedFirmsEnable[i] != InternalConstants.LINK_EE)
				continue;

			Firm linkedFirm = FirmArray[firmHarbor.LinkedFirms[i]];
			if (!NationArray[firmHarbor.NationId].get_relation(linkedFirm.NationId).trade_treaty)
				continue;

			switch (linkedFirm.FirmType)
			{
				case Firm.FIRM_MINE:
					_linkedMines.Add(linkedFirm.FirmId);
					break;

				case Firm.FIRM_FACTORY:
					_linkedFactories.Add(linkedFirm.FirmId);
					break;

				case Firm.FIRM_MARKET:
					_linkedMarkets.Add(linkedFirm.FirmId);
					break;
			}
		}
	}
	
	public bool CanSetStop(int firmId)
	{
		Firm firm = FirmArray[firmId];

		if (firm.UnderConstruction)
			return false;

		if (firm.FirmType != Firm.FIRM_HARBOR)
			return false;

		return NationArray[NationId].get_relation(firm.NationId).trade_treaty;
	}
	
	public void SetStop(int stopId, int stopLocX, int stopLocY, int remoteAction)
	{
		//-------------------------------------------------------//
		// check if there is a station in the given location
		//-------------------------------------------------------//
		Location loc = World.GetLoc(stopLocX, stopLocY);
		if (!loc.IsFirm())
			return;

		FirmHarbor harbor = (FirmHarbor)FirmArray[loc.FirmId()];
		
		if (!CanSetStop(harbor.FirmId))
			return;

		//-------------------------------------------------------//
		// return if the harbor stop is in another territory
		//-------------------------------------------------------//
		if (World.GetLoc(NextLocX, NextLocY).RegionId != harbor.SeaRegionId)
			return;

		//if (!remoteAction && remote.is_enable())
		//{
			//// packet structure : <unit recno> <stop id> <stop x> <stop y>
			//short* shortPtr = (short*)remote.new_send_queue_msg(MSG_U_SHIP_SET_STOP, 4 * sizeof(short));
			//*shortPtr = sprite_recno;
			//shortPtr[1] = stopId;
			//shortPtr[2] = stopXLoc;
			//shortPtr[3] = stopYLoc;
			//return;
		//}

		ShipStop stop = Stops[stopId - 1];
		if (stop.FirmId == 0)
			StopDefinedNum++; // no plus one if the recno is defined originally

		//-------------------------------------------------------//
		// set the station id of the stop
		//-------------------------------------------------------//
		if (stop.FirmId == harbor.FirmId)
		{
			return; // same stop as before
		}

		int oldStopFirmId = DestStopId != 0 ? Stops[DestStopId - 1].FirmId : 0;
		
		stop.FirmId = harbor.FirmId;
		stop.FirmLocX1 = harbor.LocX1;
		stop.FirmLocY1 = harbor.LocY1;
		stop.PickUpSetNone();

		//-------------------------------------------------------//
		// set pick up selection based on availability
		//-------------------------------------------------------//
		bool hasRawGoods = false;
		bool[] availableProductGoods = new bool[GameConstants.MAX_PRODUCT];
		for (int i = 0; i < harbor.LinkedFirms.Count; i++)
		{
			int goodsId = 0;
			Firm linkedFirm = FirmArray[harbor.LinkedFirms[i]];
			switch (linkedFirm.FirmType)
			{
				case Firm.FIRM_MINE:
					goodsId = ((FirmMine)linkedFirm).RawId;
					if (goodsId != 0)
					{
						hasRawGoods = true;
						stop.PickUpToggle(goodsId); // enable
					}

					break;
				case Firm.FIRM_FACTORY:
					goodsId = ((FirmFactory)linkedFirm).ProductId;
					if (goodsId != 0)
					{
						availableProductGoods[goodsId - 1] = true;
						stop.PickUpToggle(goodsId + GameConstants.MAX_RAW); // enable
					}

					break;
				case Firm.FIRM_MARKET:
					for (int j = 0; j < GameConstants.MAX_MARKET_GOODS; j++)
					{
						MarketGoods goods = ((FirmMarket)linkedFirm).MarketGoods[j];
						if (goods.RawId != 0)
						{
							hasRawGoods = true;
							stop.PickUpToggle(goods.RawId); // enable
						}
						else if (goods.ProductId != 0)
						{
							availableProductGoods[goods.ProductId - 1] = true;
							stop.PickUpToggle(goods.ProductId + GameConstants.MAX_RAW); // enable
						}
					}

					break;
			}
		}

		if (!hasRawGoods)
		{
			int productGoodsCount = 0;
			for (int i = 0; i < availableProductGoods.Length; i++)
			{
				if (availableProductGoods[i])
					productGoodsCount++;
			}
			
			if (productGoodsCount > 1)
				stop.PickUpSetAuto();
		}

		//-------------------------------------------------------//
		// remove duplicate stop or stop change nation
		//-------------------------------------------------------//
		UpdateStopList();

		//-------------------------------------------------------//
		// handle if current stop changed when mobile
		//-------------------------------------------------------//
		if (DestStopId != 0 && JourneyStatus != InternalConstants.INSIDE_FIRM)
		{
			int newStopFirmId = Stops[DestStopId - 1].FirmId;
			if (newStopFirmId != oldStopFirmId)
			{
				Firm newStopFirm = FirmArray[newStopFirmId];
				MoveToFirmSurround(newStopFirm.LocX1, newStopFirm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, newStopFirm.FirmType);
				JourneyStatus = InternalConstants.ON_WAY_TO_FIRM;
			}
		}
		else
		{
			if (JourneyStatus != InternalConstants.INSIDE_FIRM)
				Stop2();
		}
	}

	public void DelStop(int stopId, int remoteAction)
	{
		//if (!remoteAction && remote.is_enable())
		//{
			//// packet structure : <unit recno> <stop id>
			//short* shortPtr = (short*)remote.new_send_queue_msg(MSG_U_SHIP_DEL_STOP, 2 * sizeof(short));
			//*shortPtr = sprite_recno;
			//shortPtr[1] = stopId;
			//return;
		//}

		//if (remote.is_enable() && stop_array[stopId - 1].firm_recno == 0)
			//return;

		Stops[stopId - 1].FirmId = 0;
		StopDefinedNum--;
		UpdateStopList();
	}
	
	private void UpdateStopList()
	{
		//-------------------------------------------------------//
		// backup original destination stop firm id
		//-------------------------------------------------------//
		int nextStopFirmId = DestStopId != 0 ? Stops[DestStopId - 1].FirmId : 0;

		//----------------------------------------------------------------------//
		// check stop existence and the relationship between firm's nation
		//----------------------------------------------------------------------//
		int i;
		for (i = 0; i < MAX_STOP_FOR_SHIP; i++)
		{
			ShipStop stop = Stops[i];
			if (stop.FirmId == 0)
				continue;

			if (FirmArray.IsDeleted(stop.FirmId))
			{
				stop.FirmId = 0;
				StopDefinedNum--;
				continue;
			}

			Firm firm = FirmArray[stop.FirmId];

			if (firm.LocX1 != stop.FirmLocX1 || firm.LocY1 != stop.FirmLocY1 || !CanSetStop(firm.FirmId))
			{
				stop.FirmId = 0;
				StopDefinedNum--;
				continue;
			}
		}

		//-------------------------------------------------------//
		// remove duplicate node
		//-------------------------------------------------------//
		if (StopDefinedNum < 1)
		{
			for (i = 0; i < Stops.Length; i++)
				Stops[i] = new ShipStop();
			DestStopId = 0;
			return; // no stop
		}

		//-------------------------------------------------------//
		// move the only FirmId to the beginning of the array
		//-------------------------------------------------------//
		int compareId = 0;
		int nodeIndex = 0;
		for (i = 0; i < MAX_STOP_FOR_SHIP; i++, nodeIndex++)
		{
			ShipStop stop = Stops[nodeIndex];
			if (stop.FirmId != 0)
			{
				compareId = stop.FirmId;
				break;
			}
		}

		int insertNodeIndex = 0;
		if (i > 0) // else, the first record is already in the beginning of the array
		{
			Stops[insertNodeIndex] = Stops[nodeIndex];
		}

		i++;

		if (StopDefinedNum == 1)
		{
			for (i = 1; i < Stops.Length; i++)
				Stops[i] = new ShipStop();
			DestStopId = 1;
			return;
		}

		int unprocessed = StopDefinedNum - 1;
		insertNodeIndex++;
		nodeIndex++;

		for (; i < MAX_STOP_FOR_SHIP && unprocessed > 0; i++, nodeIndex++)
		{
			ShipStop stop = Stops[nodeIndex];
			if (stop.FirmId == 0)
				continue; // empty

			if (stop.FirmId == compareId)
			{
				stop.FirmId = 0;
				StopDefinedNum--;
			}
			else
			{
				compareId = stop.FirmId;

				if (insertNodeIndex != nodeIndex)
					Stops[insertNodeIndex] = Stops[nodeIndex];

				insertNodeIndex++;
			}

			unprocessed--;
		}

		if (StopDefinedNum > 2)
		{
			//-------- compare the first and the end record -------//
			nodeIndex = StopDefinedNum - 1;
			ShipStop stop = Stops[nodeIndex]; // point to the end
			if (stop.FirmId == Stops[0].FirmId)
			{
				stop.FirmId = 0; // remove the end record
				StopDefinedNum--;
			}
		}

		if (StopDefinedNum < MAX_STOP_FOR_SHIP)
		{
			for (i = StopDefinedNum; i < Stops.Length; i++)
				Stops[i] = new ShipStop();
		}

		//-----------------------------------------------------------------------------------------//
		// There should be at least one stop in the list.  Otherwise, clear all the stops
		//-----------------------------------------------------------------------------------------//
		bool ourFirmExist = false;
		for (i = 0; i < StopDefinedNum; i++)
		{
			ShipStop stop = Stops[i];
			Firm firm = FirmArray[stop.FirmId];
			if (firm.NationId == NationId)
			{
				ourFirmExist = true;
				break;
			}
		}

		if (!ourFirmExist) // none of the markets belong to our nation
		{
			for (i = 0; i < Stops.Length; i++)
				Stops[i] = new ShipStop();
			if (JourneyStatus != InternalConstants.INSIDE_FIRM)
				JourneyStatus = InternalConstants.ON_WAY_TO_FIRM;
			DestStopId = 0;
			StopDefinedNum = 0;
			return;
		}

		//-----------------------------------------------------------------------------------------//
		// reset DestStopId since the order of the stop may be changed
		//-----------------------------------------------------------------------------------------//
		int locX = NextLocX;
		int locY = NextLocY;
		int minDist = Int32.MaxValue;

		for (i = 0, DestStopId = 0; i < StopDefinedNum; i++)
		{
			ShipStop stop = Stops[i];
			if (stop.FirmId == nextStopFirmId)
			{
				DestStopId = i + 1;
				break;
			}
			else
			{
				Firm firm = FirmArray[stop.FirmId];
				int dist = Misc.points_distance(locX, locY, firm.LocCenterX, firm.LocCenterY);

				if (dist < minDist)
				{
					//TODO bug
					dist = minDist;
					DestStopId = i + 1;
				}
			}
		}
	}

	public void SetStopPickUp(int stopId, int newPickUpType, int remoteAction)
	{
		//if (!remoteAction && remote.is_enable())
		//{
			//// packet structure : <unit recno> <stop id> <new pick_up_type>
			//short* shortPtr = (short*)remote.new_send_queue_msg(MSG_U_SHIP_CHANGE_GOODS, 3 * sizeof(short));
			//*shortPtr = sprite_recno;
			//shortPtr[1] = stopId;
			//shortPtr[2] = newPickUpType;
			//return;
		//}

		switch (newPickUpType)
		{
			case TradeStop.AUTO_PICK_UP:
				Stops[stopId - 1].PickUpSetAuto();
				break;

			case TradeStop.NO_PICK_UP:
				Stops[stopId - 1].PickUpSetNone();
				break;

			default:
				Stops[stopId - 1].PickUpToggle(newPickUpType);
				break;
		}
	}
	
	public void CopyRoute(int copyUnitId, int remoteAction)
	{
		if (SpriteId == copyUnitId)
			return;

		UnitMarine copyUnit = (UnitMarine)UnitArray[copyUnitId];

		if (copyUnit.NationId != NationId)
			return;

		//if (remote.is_enable() && !remoteAction)
		//{
			//// packet structure : <unit recno> <copy recno>
			//short* shortPtr = (short*)remote.new_send_queue_msg(MSG_U_SHIP_COPY_ROUTE, 2 * sizeof(short));
			//*shortPtr = sprite_recno;
			//shortPtr[1] = copyUnitRecno;
			//return;
		//}

		// clear existing stops
		int numStops = StopDefinedNum;
		for (int i = 0; i < numStops; i++)
			DelStop(1, InternalConstants.COMMAND_AUTO); // stop ids shift up

		for (int i = 0; i < MAX_STOP_FOR_SHIP; i++)
		{
			ShipStop shipStopA = copyUnit.Stops[i];
			ShipStop shipStopB = Stops[i];
			if (shipStopA.FirmId == 0)
				break;

			if (FirmArray.IsDeleted(shipStopA.FirmId))
				continue;

			SetStop(i + 1, shipStopA.FirmLocX1, shipStopA.FirmLocY1, InternalConstants.COMMAND_AUTO);

			if (shipStopA.PickUpType == TradeStop.AUTO_PICK_UP)
			{
				SetStopPickUp(i + 1, TradeStop.AUTO_PICK_UP, InternalConstants.COMMAND_AUTO);
			}

			else if (shipStopA.PickUpType == TradeStop.NO_PICK_UP)
			{
				SetStopPickUp(i + 1, TradeStop.NO_PICK_UP, InternalConstants.COMMAND_AUTO);
			}

			else
			{
				for (int j = 0; j < TradeStop.MAX_PICK_UP_GOODS; j++)
				{
					if (shipStopA.PickUpEnabled[j] != shipStopB.PickUpEnabled[j])
						SetStopPickUp(i + 1, j + 1, InternalConstants.COMMAND_PLAYER);
				}
			}
		}
	}


	private void HarborUnloadGoods()
	{
		if (_linkedMines.Count + _linkedFactories.Count + _linkedMarkets.Count == 0)
			return;

		NotAutoLoadRawGoods.Clear();
		NotAutoLoadProductGoods.Clear();

		HarborUnloadRaw();
		HarborUnloadProduct();
	}

	private void HarborUnloadRaw()
	{
		if (_linkedFactories.Count == 0 && _linkedMarkets.Count == 0)
			return;

		for (int i = 0; i < GameConstants.MAX_RAW; i++)
		{
			if (RawQty[i] == 0)
				continue; // without this goods

			int totalDemand = 0;
			List<Firm> selectedFirms = new List<Firm>();
			//----------------------------------------------------------------------//
			// calculate the demand of this goods in factory
			//----------------------------------------------------------------------//
			for (int j = 0; j < _linkedFactories.Count; j++)
			{
				FirmFactory factory = (FirmFactory)FirmArray[_linkedFactories[j]];

				if (factory.NationId != NationId)
					continue; // don't unload goods to factory of other nation

				if (factory.ProductId - 1 == i)
				{
					totalDemand = (int)(factory.MaxRawStockQty - factory.RawStockQty);
					selectedFirms.Add(factory);
				}
			}

			//----------------------------------------------------------------------//
			// calculate the demand of this goods in market
			//----------------------------------------------------------------------//
			for (int j = 0; j < _linkedMarkets.Count; j++)
			{
				FirmMarket market = (FirmMarket)FirmArray[_linkedMarkets[j]];

				if (market.NationId != NationId)
					continue; // don't unload goods to market of other nation

				//---------- check the demand of this goods in the market ---------//
				MarketGoods marketRaw = market.GetRawGoods(i + 1);
				if (marketRaw != null)
				{
					totalDemand += (int)(market.MaxStockQty - marketRaw.StockQty);
					selectedFirms.Add(market);
				}
				else
				{
					for (int k = 0; k < GameConstants.MAX_MARKET_GOODS; k++)
					{
						MarketGoods marketGoods = market.MarketGoods[k];
						if (marketGoods.StockQty <= 0.0 && marketGoods.Supply30Days() <= 0.0 && market.IsRawMarket())
						{
							totalDemand += (int)market.MaxStockQty;
							selectedFirms.Add(market);
							break;
						}
					}
				}
			}

			//----------------------------------------------------------------------//
			// distribute the stock into each factory
			//----------------------------------------------------------------------//
			int curStock = RawQty[i];
			for (int j = 0; j < _linkedFactories.Count; j++)
			{
				FirmFactory factory = (FirmFactory)FirmArray[_linkedFactories[j]];
				
				if (!selectedFirms.Contains(factory))
					continue;
				
				int unloadQty = totalDemand != 0 ? (int)((factory.MaxRawStockQty - factory.RawStockQty) * curStock / totalDemand + 0.5) : 0;
				unloadQty = Math.Min((int)(factory.MaxRawStockQty - factory.RawStockQty), unloadQty);
				unloadQty = Math.Min(RawQty[i], unloadQty);

				factory.RawStockQty += unloadQty;
				RawQty[i] -= unloadQty;
			}

			//----------------------------------------------------------------------//
			// distribute the stock into each market
			//----------------------------------------------------------------------//
			for (int j = 0; j < _linkedMarkets.Count; j++)
			{
				FirmMarket market = (FirmMarket)FirmArray[_linkedMarkets[j]];
				
				if (!selectedFirms.Contains(market))
					continue;

				int emptySlotIndex = -1;
				MarketGoods marketRaw = market.GetRawGoods(i + 1);
				if (marketRaw == null) // using empty slot, don't set the pointer to the market_goods_array until unloadQty>0
				{
					for (int k = 0; k < GameConstants.MAX_MARKET_GOODS; k++)
					{
						MarketGoods marketGoods = market.MarketGoods[k];
						if (marketGoods.StockQty <= 0.0 && marketGoods.Supply30Days() <= 0.0)
						{
							marketRaw = marketGoods;
							emptySlotIndex = k;
							break;
						}
					}
				}

				int unloadQty = totalDemand != 0 ? (int)((market.MaxStockQty - marketRaw.StockQty) * curStock / totalDemand + 0.5) : 0;
				unloadQty = Math.Min((int)(market.MaxStockQty - marketRaw.StockQty), unloadQty);
				unloadQty = Math.Min(RawQty[i], unloadQty);

				if (unloadQty != 0)
				{
					if (emptySlotIndex != -1)
						market.SetRawGoods(i + 1, emptySlotIndex);

					marketRaw.StockQty += unloadQty;
					RawQty[i] -= unloadQty;
					if (unloadQty > 0)
						NotAutoLoadRawGoods.Add((market, i + 1));
				}
			}
		}
	}
	
	private void HarborUnloadProduct()
	{
		if (_linkedMarkets.Count == 0)
			return;

		for (int i = 0; i < GameConstants.MAX_PRODUCT; i++)
		{
			if (ProductQty[i] == 0)
				continue; // without this goods

			//----------------------------------------------------------------------//
			// calculate the demand of this goods in market
			//----------------------------------------------------------------------//
			int totalDemand = 0;
			List<Firm> selectedFirms = new List<Firm>();
			for (int j = 0; j < _linkedMarkets.Count; j++)
			{
				FirmMarket market = (FirmMarket)FirmArray[_linkedMarkets[j]];

				if (market.NationId != NationId)
					continue; // don't unload goods to market of other nation

				//---------- check the demand of this goods in the market ---------//
				MarketGoods marketProduct = market.GetProductGoods(i + 1);
				if (marketProduct != null)
				{
					totalDemand += (int)(market.MaxStockQty - marketProduct.StockQty);
					selectedFirms.Add(market);
				}
				else
				{
					for (int k = 0; k < GameConstants.MAX_MARKET_GOODS; k++)
					{
						MarketGoods marketGoods = market.MarketGoods[k];
						if (marketGoods.StockQty <= 0.0 && marketGoods.Supply30Days() <= 0.0 && market.IsRetailMarket())
						{
							totalDemand += (int)market.MaxStockQty;
							selectedFirms.Add(market);
							break;
						}
					}
				}
			}

			//----------------------------------------------------------------------//
			// distribute the stock into each market
			//----------------------------------------------------------------------//
			int curStock = ProductQty[i];
			for (int j = 0; j < _linkedMarkets.Count; j++)
			{
				FirmMarket market = (FirmMarket)FirmArray[_linkedMarkets[j]];
				
				if (!selectedFirms.Contains(market))
					continue;

				int emptySlotIndex = -1;
				MarketGoods marketProduct = market.GetProductGoods(i + 1);
				if (marketProduct == null) // using empty slot, don't set the pointer to the market_goods_array until unloadQty>0
				{
					for (int k = 0; k < GameConstants.MAX_MARKET_GOODS; k++)
					{
						MarketGoods marketGoods = market.MarketGoods[k];
						if (marketGoods.StockQty <= 0.0 && marketGoods.Supply30Days() <= 0.0)
						{
							marketProduct = marketGoods;
							emptySlotIndex = k;
							break;
						}
					}
				}

				int unloadQty = totalDemand != 0 ? (int)((market.MaxStockQty - marketProduct.StockQty) * curStock / totalDemand + 0.5) : 0;
				unloadQty = Math.Min((int)(market.MaxStockQty - marketProduct.StockQty), unloadQty);
				unloadQty = Math.Min(ProductQty[i], unloadQty);

				if (unloadQty != 0)
				{
					if (emptySlotIndex != -1)
						market.SetProductGoods(i + 1, emptySlotIndex);

					marketProduct.StockQty += unloadQty;
					ProductQty[i] -= unloadQty;
					if (unloadQty > 0)
						NotAutoLoadProductGoods.Add((market, i + 1));
				}
			}
		}
	}

	private void HarborLoadGoods()
	{
		if (_linkedMines.Count + _linkedFactories.Count + _linkedMarkets.Count == 0)
			return;

		ShipStop shipStop = Stops[DestStopId - 1];
		if (shipStop.PickUpType == TradeStop.NO_PICK_UP)
			return; // return if not allowed to load any goods

		for (int i = 0; i < TradeStop.MAX_PICK_UP_GOODS; i++)
		{
			if (!shipStop.PickUpEnabled[i])
				continue;

			int pickUpType = i + 1;
			if (pickUpType >= TradeStop.PICK_UP_RAW_FIRST && pickUpType <= TradeStop.PICK_UP_RAW_LAST)
			{
				int goodsId = pickUpType - TradeStop.PICK_UP_RAW_FIRST;

				if (RawQty[goodsId] < CarryGoodsCapacity)
					HarborLoadRaw(goodsId, false, 1); // 1 -- only consider our firm

				if (RawQty[goodsId] < CarryGoodsCapacity)
					HarborLoadRaw(goodsId, false, 0); // 0 -- only consider firm of other nation
			}
			else if (pickUpType >= TradeStop.PICK_UP_PRODUCT_FIRST && pickUpType <= TradeStop.PICK_UP_PRODUCT_LAST)
			{
				int goodsId = pickUpType - TradeStop.PICK_UP_PRODUCT_FIRST;

				if (ProductQty[goodsId] < CarryGoodsCapacity) // 1 -- only consider our firm
					HarborLoadProduct(goodsId, false, 1);

				if (ProductQty[goodsId] < CarryGoodsCapacity) // 0 -- only consider firm of other nation
					HarborLoadProduct(goodsId, false, 0);
			}
		}
	}

	private void HarborAutoLoadGoods()
	{
		if (_linkedMines.Count + _linkedFactories.Count + _linkedMarkets.Count == 0)
			return;

		//TODO support auto load for other markets too?
		for (int i = 0; i < GameConstants.MAX_RAW; i++)
		{
			if (RawQty[i] < CarryGoodsCapacity)
				HarborLoadRaw(i, true, 1); // 1 -- only consider our market
		}
		
		for (int i = 0; i < GameConstants.MAX_PRODUCT; i++)
		{
			if (ProductQty[i] < CarryGoodsCapacity)
				HarborLoadProduct(i, true, 1); // 1 -- only consider our market
		}
	}

	private void HarborLoadRaw(int goodsId, bool autoPickUp, int considerMode)
	{
		if (_linkedMines.Count + _linkedMarkets.Count == 0)
			return;

		if (RawQty[goodsId] == CarryGoodsCapacity)
			return;

		int totalSupply = 0;
		List<Firm> selectedFirms = new List<Firm>();
		//----------------------------------------------------------------------//
		// calculate the supply of this goods in mine
		//----------------------------------------------------------------------//
		for (int i = 0; i < _linkedMines.Count; i++)
		{
			FirmMine mine = (FirmMine)FirmArray[_linkedMines[i]];
			if (mine.RawId != goodsId + 1)
				continue; // incorrect goods

			if (considerMode == 0 && mine.NationId == NationId)
				continue; // not consider our mine for this mode
			
			if (considerMode != 0 && mine.NationId != NationId)
				continue; // not consider other mine for this mode
			
			//---------- check the supply of this goods in the mine ---------//
			int keepStockQty = autoPickUp ? (int)(mine.MaxStockQty / 5.0) : 0;
			totalSupply += Math.Max((int)(mine.StockQty - keepStockQty), 0);
			selectedFirms.Add(mine);
		}

		//----------------------------------------------------------------------//
		// calculate the supply of this goods in market
		//----------------------------------------------------------------------//
		for (int i = 0; i < _linkedMarkets.Count; i++)
		{
			FirmMarket market = (FirmMarket)FirmArray[_linkedMarkets[i]];

			if (considerMode == 0 && market.NationId == NationId)
				continue; // not consider our market for this mode

			if (considerMode != 0 && market.NationId != NationId)
				continue; // not consider other market for this mode

			if (autoPickUp)
			{
				bool shouldAutoLoad = true;
				for (int j = 0; j < NotAutoLoadRawGoods.Count; j++)
				{
					if (NotAutoLoadRawGoods[j].Item1 == market && NotAutoLoadRawGoods[j].Item2 == goodsId + 1)
						shouldAutoLoad = false;
				}
				
				if (shouldAutoLoad)
					continue;
			}
			
			//---------- check the supply of this goods in the market ---------//
			MarketGoods marketRaw = market.GetRawGoods(goodsId + 1);
			if (marketRaw != null)
			{
				int keepStockQty = autoPickUp ? (int)(market.MaxStockQty / 5.0) : 0;
				totalSupply += Math.Max((int)(marketRaw.StockQty - keepStockQty), 0);
				selectedFirms.Add(market);
			}
		}

		Nation nation = NationArray[NationId];
		int curDemand = CarryGoodsCapacity - RawQty[goodsId];
		//----------------------------------------------------------------------//
		// get the stock from each mine
		//----------------------------------------------------------------------//
		for (int i = 0; i < _linkedMines.Count; i++)
		{
			FirmMine mine = (FirmMine)FirmArray[_linkedMines[i]];
			
			if (!selectedFirms.Contains(mine))
				continue;

			int keepStockQty = autoPickUp ? (int)(mine.MaxStockQty / 5.0) : 0;
			int loadQty = Math.Max((int)(mine.StockQty - keepStockQty), 0);
			loadQty = totalSupply != 0 ? Math.Min(loadQty * curDemand / totalSupply, loadQty) : 0;

			if (mine.NationId != NationId)
			{
				loadQty = (nation.cash > 0.0) ? (int)Math.Min(nation.cash / GameConstants.RAW_PRICE, loadQty) : 0;
				if (loadQty > 0)
					nation.import_goods(NationBase.IMPORT_RAW, mine.NationId, loadQty * GameConstants.RAW_PRICE);
			}

			mine.StockQty -= loadQty;
			RawQty[goodsId] += loadQty;
		}

		//----------------------------------------------------------------------//
		// get the stock from each market
		//----------------------------------------------------------------------//
		for (int i = 0; i < _linkedMarkets.Count; i++)
		{
			FirmMarket market = (FirmMarket)FirmArray[_linkedMarkets[i]];
			
			if (!selectedFirms.Contains(market))
				continue;

			MarketGoods marketRaw = market.GetRawGoods(goodsId + 1);

			int keepStockQty = autoPickUp ? (int)(market.MaxStockQty / 5.0) : 0;
			int loadQty = Math.Max((int)marketRaw.StockQty - keepStockQty, 0);
			loadQty = totalSupply != 0 ? Math.Min(loadQty * curDemand / totalSupply, loadQty) : 0;

			if (market.NationId != NationId)
			{
				loadQty = (nation.cash > 0.0) ? (int)Math.Min(nation.cash / GameConstants.RAW_PRICE, loadQty) : 0;
				if (loadQty > 0)
					nation.import_goods(NationBase.IMPORT_RAW, market.NationId, loadQty * GameConstants.RAW_PRICE);
			}

			marketRaw.StockQty -= loadQty;
			RawQty[goodsId] += loadQty;
		}
	}
	
	private void HarborLoadProduct(int goodsId, bool autoPickUp, int considerMode)
	{
		if (_linkedFactories.Count + _linkedMarkets.Count == 0)
			return;

		if (ProductQty[goodsId] == CarryGoodsCapacity)
			return;

		int totalSupply = 0;
		List<Firm> selectedFirms = new List<Firm>();
		//----------------------------------------------------------------------//
		// calculate the supply of this goods in factory
		//----------------------------------------------------------------------//
		for (int i = 0; i < _linkedFactories.Count; i++)
		{
			FirmFactory factory = (FirmFactory)FirmArray[_linkedFactories[i]];
			if (factory.ProductId != goodsId + 1)
				continue; // incorrect product

			if (considerMode == 0 && factory.NationId == NationId)
				continue; // not consider our factory for this mode

			if (considerMode != 0 && factory.NationId != NationId)
				continue; // not consider other factory for this mode

			//---------- check the supply of this goods in the factory ---------//
			int keepStockQty = autoPickUp ? (int)(factory.MaxStockQty / 5.0) : 0;
			totalSupply += Math.Max((int)(factory.StockQty - keepStockQty), 0);
			selectedFirms.Add(factory);
		}

		//----------------------------------------------------------------------//
		// calculate the supply of this goods in market
		//----------------------------------------------------------------------//
		for (int i = 0; i < _linkedMarkets.Count; i++)
		{
			FirmMarket market = (FirmMarket)FirmArray[_linkedMarkets[i]];

			if (considerMode == 0 && market.NationId == NationId)
				continue; // not consider our market for this mode

			if (considerMode != 0 && market.NationId != NationId)
				continue; // not consider other market for this mode

			if (autoPickUp)
			{
				bool shouldAutoLoad = true;
				for (int j = 0; j < NotAutoLoadProductGoods.Count; j++)
				{
					if (NotAutoLoadProductGoods[j].Item1 == market && NotAutoLoadProductGoods[j].Item2 == goodsId + 1)
						shouldAutoLoad = false;
				}
				
				if (shouldAutoLoad)
					continue;
			}
			
			//---------- check the supply of this goods in the market ---------//
			MarketGoods marketProduct = market.GetProductGoods(goodsId + 1);
			if (marketProduct != null)
			{
				int keepStockQty = autoPickUp ? (int)(market.MaxStockQty / 5.0) : 0;
				totalSupply += Math.Max((int)(marketProduct.StockQty - keepStockQty), 0);
				selectedFirms.Add(market);
			}
		}

		Nation nation = NationArray[NationId];
		int curDemand = CarryGoodsCapacity - ProductQty[goodsId];
		//----------------------------------------------------------------------//
		// get the stock from each factory
		//----------------------------------------------------------------------//
		for (int i = 0; i < _linkedFactories.Count; i++)
		{
			FirmFactory factory = (FirmFactory)FirmArray[_linkedFactories[i]];
			
			if (!selectedFirms.Contains(factory))
				continue;

			int keepStockQty = autoPickUp ? (int)(factory.MaxStockQty / 5.0) : 0;
			int loadQty = Math.Max((int)(factory.StockQty - keepStockQty), 0);
			loadQty = totalSupply != 0 ? Math.Min(loadQty * curDemand / totalSupply, loadQty) : 0;

			if (factory.NationId != NationId)
			{
				loadQty = (nation.cash > 0.0) ? (int)Math.Min(nation.cash / GameConstants.PRODUCT_PRICE, loadQty) : 0;
				if (loadQty > 0)
					nation.import_goods(NationBase.IMPORT_PRODUCT, factory.NationId, loadQty * GameConstants.PRODUCT_PRICE);
			}

			factory.StockQty -= loadQty;
			ProductQty[goodsId] += loadQty;
		}

		//----------------------------------------------------------------------//
		// get the stock from each market
		//----------------------------------------------------------------------//
		for (int i = 0; i < _linkedMarkets.Count; i++)
		{
			FirmMarket market = (FirmMarket)FirmArray[_linkedMarkets[i]];
			
			if (!selectedFirms.Contains(market))
				continue;

			MarketGoods marketProduct = market.GetProductGoods(goodsId + 1);

			int keepStockQty = autoPickUp ? (int)(market.MaxStockQty / 5.0) : 0;
			int loadQty = Math.Max((int)marketProduct.StockQty - keepStockQty, 0);
			loadQty = totalSupply != 0 ? Math.Min(loadQty * curDemand / totalSupply, loadQty) : 0;

			if (market.NationId != NationId)
			{
				loadQty = (nation.cash > 0.0) ? (int)Math.Min(nation.cash / GameConstants.PRODUCT_PRICE, loadQty) : 0;
				if (loadQty > 0)
					nation.import_goods(NationBase.IMPORT_PRODUCT, market.NationId, loadQty * GameConstants.PRODUCT_PRICE);
			}

			marketProduct.StockQty -= loadQty;
			ProductQty[goodsId] += loadQty;
		}
	}

	
	public bool CanUnloadUnit()
	{
		return UnitsOnBoard.Count > 0 && (CurAction == SPRITE_IDLE || CurAction == SPRITE_ATTACK) && IsOnCoast();
	}

	public void UnloadUnit(int unitSeqId, int remoteAction)
	{
		//if (!remoteAction && remote.is_enable())
		//{
			//// packet structure : <unit recno> <unitSeqId>
			//short* shortPtr = (short*)remote.new_send_queue_msg(MSG_U_SHIP_UNLOAD_UNIT, 2 * sizeof(short));
			//*shortPtr = sprite_recno;
			//shortPtr[1] = unitSeqId;
			//return;
		//}

		UnloadingUnit(unitSeqId - 1);
	}

	public void UnloadAllUnits(int remoteAction)
	{
		//if (!remoteAction && remote.is_enable())
		//{
			//// packet structure : <unit recno>
			//short* shortPtr = (short*)remote.new_send_queue_msg(MSG_U_SHIP_UNLOAD_ALL_UNITS, sizeof(short));
			//*shortPtr = sprite_recno;
			//return;
		//}

		UnloadingUnit(-1); // unload all units
	}

	private bool UnloadingUnit(int unitIndex = 0)
	{
		if (!IsOnCoast())
			return false;

		int curLocX = NextLocX; // ship location
		int curLocY = NextLocY;
		int unprocessed = unitIndex == -1 ? UnitsOnBoard.Count : 1;
		Unit unit = unitIndex == -1 ? UnitArray[UnitsOnBoard[unprocessed - 1]] : UnitArray[UnitsOnBoard[unitIndex]];
		int regionId = 0; // unload all the units in the same territory
		int i = 2;
		bool found = false;
		int sqtSize = 5, sqtArea = sqtSize * sqtSize;

		while (unprocessed > 0) // using the calculated 'i' to reduce useless calculation
		{
			Misc.cal_move_around_a_point(i, GameConstants.MapSize, GameConstants.MapSize, out int xShift, out int yShift);
			int checkLocX = curLocX + xShift;
			int checkLocY = curLocY + yShift;
			if (!Misc.IsLocationValid(checkLocX, checkLocY))
			{
				i++;
				continue;
			}

			//-------------------------------------------------------------------------//
			// check for space to unload the unit
			//-------------------------------------------------------------------------//
			Location loc = World.GetLoc(checkLocX, checkLocY);
			if (regionId == 0 || loc.RegionId == regionId)
			{
				if (loc.Walkable())
					found = true;

				if (loc.CanMove(UnitConstants.UNIT_LAND))
				{
					regionId = loc.RegionId;

					unit.InitSprite(checkLocX, checkLocY);
					unit.SetMode(0);

					//TODO selection
					/*if (isAll && NationId == NationArray.player_recno)
					{
						unit.SelectedFlag = true; // mark selected if unload all
						UnitArray.SelectedCount++;

						if (UnitArray.SelectedUnitId == 0)
							UnitArray.SelectedUnitId = unit.SpriteId;
					}*/

					unprocessed--;
					UnitsOnBoard.Remove(unit.SpriteId);

					if (unprocessed > 0)
						unit = UnitArray[UnitsOnBoard[unprocessed - 1]]; // point to next unit
					else
						break; // finished, all have been unloaded
				}
			}

			//-------------------------------------------------------------------------//
			// stop checking if there is totally bounded by inaccessible location
			//-------------------------------------------------------------------------//
			if (i == sqtArea)
			{
				if (found)
				{
					found = false; // reset found
					sqtSize += 2;
					sqtArea = sqtSize * sqtSize;
				}
				else // no continuous location for the unit to unload, some units can't be unloaded
				{
					return false;
				}
			}

			i++;
		}

		return true;
	}

	private bool IsOnCoast()
	{
		int curLocX = NextLocX; // ship location
		int curLocY = NextLocY;

		for (int i = 2; i <= 9; i++) // checking for the surrounding location
		{
			Misc.cal_move_around_a_point(i, 3, 3, out int xShift, out int yShift);

			int checkLocX = curLocX + xShift;
			int checkLocY = curLocY + yShift;

			if (!Misc.IsLocationValid(checkLocX, checkLocY))
				continue;

			Location location = World.GetLoc(checkLocX, checkLocY);
			if (TerrainRes[location.TerrainId].AverageType != TerrainTypeCode.TERRAIN_OCEAN && location.Walkable())
			{
				return true;
			}
		}

		return false;
	}
	
	public void LoadUnit(int unitId)
	{
		if (UnitsOnBoard.Count == UnitConstants.MAX_UNIT_IN_SHIP)
			return;
		
		if (UnitArray.IsDeleted(unitId))
			return;

		UnitsOnBoard.Add(unitId);
		
		Unit unit = UnitArray[unitId];
		unit.SetMode(UnitConstants.UNIT_MODE_ON_SHIP, SpriteId); // set unit mode
		unit.DeinitSprite();

		//TODO selection
		/*if (unit.SelectedFlag)
		{
			unit.SelectedFlag = false;
			UnitArray.SelectedCount--;
		}*/
	}
	

	public void ExtraMove()
	{
		int[] offset = { 0, 1, -1 };

		int curLocX = NextLocX;
		int curLocY = NextLocY;

		int vecX = ActionLocX2 - curLocX;
		int vecY = ActionLocY2 - curLocY;
		int checkLocX = -1, checkLocY = -1;
		bool found = false;

		if (vecX == 0 || vecY == 0)
		{
			if (vecX == 0)
			{
				vecY /= Math.Abs(vecY);
				checkLocY = curLocY + vecY;
			}
			else // vecY==0
			{
				vecX /= Math.Abs(vecX);
				checkLocX = curLocX + vecX;
			}

			for (int i = 0; i < 3; i++)
			{
				if (vecX == 0)
					checkLocX = curLocX + offset[i];
				else
					checkLocY = curLocY + offset[i];

				if (!Misc.IsLocationValid(checkLocX, checkLocY))
					continue;

				if (World.GetLoc(checkLocX, checkLocY).CanMove(MobileType))
				{
					found = true;
					break;
				}
			}
		}
		else
		{
			vecX /= Math.Abs(vecX);
			vecY /= Math.Abs(vecY);
			checkLocX = curLocX + vecX;
			checkLocY = curLocY + vecY;

			if (World.GetLoc(checkLocX, checkLocY).CanMove(MobileType))
				found = true;
		}

		if (!found)
			return;

		SetDir(curLocX, curLocY, checkLocX, checkLocY);
		CurAction = SPRITE_SHIP_EXTRA_MOVE;
		GoX = checkLocX * InternalConstants.CellWidth;
		GoY = checkLocY * InternalConstants.CellHeight;
	}

	public override void ProcessExtraMove()
	{
		int[] vectorsX = { 0, 1, 1, 1, 0, -1, -1, -1 }; // default vectors, temporarily only
		int[] vectorsY = { -1, -1, 0, 1, 1, 1, 0, -1 };

		if (!MatchDir()) // process turning
			return;

		if (CurX != GoX || CurY != GoY)
		{
			//------------------------------------------------------------------------//
			// set cargo_recno, extra_move_in_beach
			//------------------------------------------------------------------------//
			if (CurX == NextX && CurY == NextY)
			{
				int goLocX = GoX >> InternalConstants.CellWidthShift;
				int goLocY = GoY >> InternalConstants.CellHeightShift;
				if (!World.GetLoc(goLocX, goLocY).CanMove(MobileType))
				{
					GoX = NextX;
					GoY = NextY;
					return;
				}

				int curLocX = NextLocX;
				int curLocY = NextLocY;
				World.GetLoc(curLocX, curLocY).SetUnit(MobileType, 0);
				World.GetLoc(goLocX, goLocY).SetUnit(MobileType, SpriteId);
				NextX = GoX;
				NextY = GoY;

				InBeach = (curLocX % 2 == 0 && curLocY % 2 == 0);

				if (goLocX % 2 != 0 || goLocY % 2 != 0) // not even location
					ExtraMoveInBeach = EXTRA_MOVING_IN;
				else // even location
					ExtraMoveInBeach = EXTRA_MOVING_OUT;
			}

			//---------- process moving -----------//
			int stepX = SpriteInfo.Speed;
			int stepY = SpriteInfo.Speed;
			int vectorX = vectorsX[FinalDir] * SpriteInfo.Speed; // CurDir may be changed in the above SetNext() call
			int vectorY = vectorsY[FinalDir] * SpriteInfo.Speed;

			if (Math.Abs(CurX - GoX) <= stepX)
				CurX = GoX;
			else
				CurX += vectorX;

			if (Math.Abs(CurY - GoY) <= stepY)
				CurY = GoY;
			else
				CurY += vectorY;
		}

		if (CurX == GoX && CurY == GoY)
		{
			if (PathNodes.Count == 0)
			{
				CurAction = SPRITE_IDLE;
				CurFrame = 1;
				MoveToLocX = NextLocX;
				MoveToLocY = NextLocY;
			}
			else
			{
				CurAction = SPRITE_MOVE;
				NextMove();
			}

			ExtraMoveInBeach = InBeach ? EXTRA_MOVE_FINISH : NO_EXTRA_MOVE;
		}
	}

	public override bool IsAIAllStop()
	{
		if (CurAction != SPRITE_IDLE || AIActionId != 0)
			return false;

		//---- if the ship is on the beach, it's action mode is always ACTION_SHIP_TO_BEACH, so we can't check it against ACTION_STOP ---//

		if (ActionMode2 == UnitConstants.ACTION_SHIP_TO_BEACH)
		{
			if (InBeach && (ExtraMoveInBeach == NO_EXTRA_MOVE || ExtraMoveInBeach == EXTRA_MOVE_FINISH))
				return true;
		}

		return ActionMode == UnitConstants.ACTION_STOP && ActionMode2 == UnitConstants.ACTION_STOP;
	}

	public override bool CanResign()
	{
		return UnitsOnBoard.Count == 0;
	}

	protected override void FixAttackInfo() // set AttackInfos appropriately
	{
		base.FixAttackInfo();

		if (AttackCount > 0)
		{
			ShipAttackInfo = UnitRes.GetAttackInfo(UnitRes[UnitType].first_attack);

			AttackInfos = new AttackInfo[AttackCount];
			for (int i = 0; i < AttackCount; i++)
			{
				AttackInfos[i] = UnitRes.GetAttackInfo(UnitRes[UnitType].first_attack + i);
			}
		}
		else
		{
			AttackInfos = Array.Empty<AttackInfo>();
		}
	}

	public override double ActualDamage()
	{
		//-----------------------------------------//
		// If there is units on the ship, the units leadership will increase the attacking damage.
		//-----------------------------------------//

		int highestLeadership = 0;

		for (int i = 0; i < UnitsOnBoard.Count; i++)
		{
			Unit unit = UnitArray[UnitsOnBoard[i]];

			//TODO check only general skill
			if (unit.Skill.SkillId == Skill.SKILL_LEADING)
			{
				if (unit.Skill.SkillLevel > highestLeadership)
					highestLeadership = unit.Skill.SkillLevel;
			}
		}

		return base.ActualDamage() * (100 + highestLeadership) / 100.0;
	}
	
	public override bool ShouldShowInfo()
	{
		if (base.ShouldShowInfo())
			return true;

		//--- if any of the units on the ship are spies of the player ---//

		for (int i = 0; i < UnitsOnBoard.Count; i++)
		{
			if (UnitArray[UnitsOnBoard[i]].IsOwn())
				return true;
		}

		return false;
	}

	public override void DrawDetails(IRenderer renderer)
	{
		renderer.DrawShipDetails(this);
	}

	public override void HandleDetailsInput(IRenderer renderer)
	{
		renderer.HandleShipDetailsInput(this);
	}
	
	#region Old AI Functions

	public override void ProcessAI()
	{
		//-- Think about removing stops whose owner nation is at war with us. --//

		// AI does do any sea trade

		//	if( info.game_date%30 == sprite_recno%30 )
		//		think_del_stop();

		//---- Think about setting new trade route -------//

		if (Info.TotalDays % 15 == SpriteId % 15)
		{
			if (StopDefinedNum < 2 && IsVisible() && IsAIAllStop())
				ai_sail_to_nearby_harbor();
		}

		//------ Think about resigning this caravan -------//

		if (Info.TotalDays % 60 == SpriteId % 60)
			think_resign();
	}

	public bool think_resign()
	{
		//---- only resign when the ship has stopped ----//

		if (!IsAIAllStop())
			return false;

		//--- retire this ship if we have better ship technology available ---//

		if (UnitType == UnitConstants.UNIT_TRANSPORT)
		{
			if (UnitRes[UnitConstants.UNIT_CARAVEL].get_nation_tech_level(NationId) > 0 ||
			    UnitRes[UnitConstants.UNIT_GALLEON].get_nation_tech_level(NationId) > 0)
			{
				if (!NationArray[NationId].ai_is_sea_travel_safe())
				{
					Resign(InternalConstants.COMMAND_AI);
					return true;
				}
			}
		}

		return false;
	}

	public void think_del_stop()
	{
		if (!IsVisible()) // cannot del stop if the caravan is inside a market place.
			return;

		Nation nation = NationArray[NationId];

		for (int i = StopDefinedNum; i > 0; i--)
		{
			if (FirmArray.IsDeleted(Stops[i - 1].FirmId))
			{
				DelStop(i, InternalConstants.COMMAND_AI);
				continue;
			}

			//----------------------------------------------//

			int nationRecno = FirmArray[Stops[i - 1].FirmId].NationId;

			if (nation.get_relation_status(nationRecno) == NationBase.NATION_HOSTILE)
			{
				DelStop(i, InternalConstants.COMMAND_AI);
			}
		}
	}

	public void ai_sail_to_nearby_harbor()
	{
		Nation ownNation = NationArray[NationId];
		FirmHarbor bestHarbor = null;
		int bestRating = 0;
		int curXLoc = CurLocX, curYLoc = CurLocY;
		int curRegionId = RegionId();

		for (int i = 0; i < ownNation.ai_harbor_array.Count; i++)
		{
			FirmHarbor firmHarbor = (FirmHarbor)FirmArray[ownNation.ai_harbor_array[i]];

			if (firmHarbor.SeaRegionId != curRegionId)
				continue;

			int curRating = World.DistanceRating(curXLoc, curYLoc, firmHarbor.LocCenterX, firmHarbor.LocCenterY);

			curRating += (GameConstants.MAX_SHIP_IN_HARBOR - firmHarbor.Ships.Count) * 100;

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestHarbor = firmHarbor;
			}
		}

		if (bestHarbor != null)
			Assign(bestHarbor.LocX1, bestHarbor.LocY1);
	}

	public void ai_ship_being_attacked(int attackerUnitRecno)
	{
		Unit attackerUnit = UnitArray[attackerUnitRecno];

		if (attackerUnit.NationId == NationId) // this can happen when the unit has just changed nation
			return;

		if (Info.TotalDays % 5 == SpriteId % 5)
		{
			NationArray[NationId].ai_sea_attack_target(attackerUnit.NextLocX, attackerUnit.NextLocY);
		}
	}

	#endregion
}