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
	public int AttackModeSelected { get; set; }
	public DateTime LastLoadGoodsDate { get; set; }

	public int JourneyStatus { get; set; } // 1 for not unload but can up load, 2 for unload but not up load
	public int DestStopId { get; set; } // destination stop id. the stop which the train currently is moving towards
	public int StopDefinedNum { get; set; } // num of stop defined
	public int WaitCount { get; set; } // set to -1 to indicate only one stop is specified

	public int StopLocX { get; set; } // the x location the unit entering the stop
	public int StopLocY { get; set; } // the y location the unit entering the stop

	public int AutoMode { get; set; } // indicate whether auto mode is on/off, 1 - on, 0 - off
	public int CurFirmId { get; set; } // the recno of current firm the ship entered
	public int CarryGoodsCapacity { get; set; }

	// an array of firm_recno telling train stop stations
	public ShipStop[] Stops { get; } = new ShipStop[MAX_STOP_FOR_SHIP];

	public int[] RawQty { get; } = new int[GameConstants.MAX_RAW];
	public int[] ProductQty { get; } = new int[GameConstants.MAX_PRODUCT];
	private List<int[]> ProcessedRawQty { get; } = new List<int[]>();
	private List<int[]> ProcessedProductQty { get; } = new List<int[]>();
	
	private readonly List<int> _linkedMines = new List<int>();
	private readonly List<int> _linkedFactories = new List<int>();
	private readonly List<int> _linkedMarkets = new List<int>();
	private readonly List<int> _emptySlotPositions = new List<int>();
	private readonly List<int> _selectedFirms = new List<int>();

	public UnitMarine()
	{
		for (int i = 0; i < Stops.Length; i++)
			Stops[i] = new ShipStop();

		JourneyStatus = InternalConstants.ON_WAY_TO_FIRM;
		AutoMode = 1; // there should be no button to toggle it if the ship is only for trading
	}

	public override void Init(int unitType, int nationId, int rank, int unitLoyalty, int startLocX, int startLocY)
	{
		AttackModeSelected = 0; // for fix_attack_info() to set attack_info_array

		base.Init(unitType, nationId, rank, unitLoyalty, startLocX, startLocY);
		
		ExtraMoveInBeach = NO_EXTRA_MOVE;
		LastLoadGoodsDate = Info.game_date;

		//int spriteId = SpriteInfo.GetSubSpriteInfo(1).SpriteId;
		//splash.init(spriteId, cur_x_loc(), cur_y_loc());
		//splash.cur_frame = 1;

		//TODO UI
		/*UnitInfo unitInfo = UnitRes[unitType];
		carry_goods_capacity = unitInfo.carry_goods_capacity;
		if (unitInfo.carry_unit_capacity == 0 && unitInfo.carry_goods_capacity > 0) // if this ship only carries goods
			menu_mode = SHIP_MENU_GOODS;
		else
			menu_mode = SHIP_MENU_UNIT;*/
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

	public void DelUnit(int unitRecno)
	{
		for (int i = UnitsOnBoard.Count - 1; i >= 0; i--)
		{
			if (UnitsOnBoard[i] == unitRecno)
			{
				UnitsOnBoard.RemoveAt(i);
				return;
			}
		}
	}
	
	public override void PreProcess()
	{
		base.PreProcess();
		if (HitPoints <= 0.0 || ActionMode == UnitConstants.ACTION_DIE || CurAction == SPRITE_DIE)
			return;

		if (ActionMode2 >= UnitConstants.ACTION_ATTACK_UNIT && ActionMode2 <= UnitConstants.ACTION_ATTACK_WALL)
			return; // don't process trading if unit is attacking

		if (AutoMode != 0) // process trading automatically, same as caravan
		{
			if (JourneyStatus == InternalConstants.INSIDE_FIRM)
			{
				ShipInFirm();
				return;
			}

			if (StopDefinedNum == 0)
				return;

			//----------------- if there is only one defined stop --------------------//
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

				int curXLoc = NextLocX;
				int curYLoc = NextLocY;
				int moveStep = MoveStepCoeff();
				if (curXLoc < firm.LocX1 - moveStep || curXLoc > firm.LocX2 + moveStep || curYLoc < firm.LocY1 - moveStep ||
				    curYLoc > firm.LocY2 + moveStep)
				{
					if (!IsVisible())
						return; // may get here if player manually ordered ship to dock
					//### begin alex 6/10 ###//
					/*if((move_to_x_loc>=firm.loc_x1-moveStep || move_to_x_loc<=firm.loc_x2+moveStep) &&
						(move_to_y_loc>=firm.loc_y1-moveStep || move_to_y_loc<=firm.loc_y2+moveStep))
						return;
	
					move_to_firm_surround(firm.loc_x1, firm.loc_y1, sprite_info.loc_width, sprite_info.loc_height, firm.firm_id);
					journey_status = ON_WAY_TO_FIRM;*/
					if (CurAction == SPRITE_IDLE)
						MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, firm.FirmType);
					else
						JourneyStatus = InternalConstants.ON_WAY_TO_FIRM;
					//#### end alex 6/10 ####//
				}
				else
				{
					if (CurX == NextX && CurY == NextY && CurAction == SPRITE_IDLE)
					{
						JourneyStatus = InternalConstants.SURROUND_FIRM;
						if (NationArray[NationId].get_relation(firm.NationId).trade_treaty)
						{
							if (WaitCount <= 0)
							{
								//---------- unloading goods -------------//
								CurFirmId = Stops[0].FirmId;
								GetHarborLinkedFirmInfo();
								HarborUnloadGoods();
								WaitCount = GameConstants.MAX_SHIP_WAIT_TERM * InternalConstants.SURROUND_FIRM_WAIT_FACTOR;
								CurFirmId = 0;
							}
							else
								WaitCount--;
						}
					}
				}

				return;
			}

			//------------ if there are more than one defined stop ---------------//
			ShipOnWay();
		}
		else if (JourneyStatus == InternalConstants.INSIDE_FIRM)
			ShipInFirm(0); // autoMode is off
	}
	
	public void ShipInFirm(int autoMode = 1)
	{
		//-----------------------------------------------------------------------------//
		// the harbor is deleted while the ship is in harbor
		//-----------------------------------------------------------------------------//
		if (CurFirmId != 0 && FirmArray.IsDeleted(CurFirmId))
		{
			HitPoints = 0.0; // ship also die if the harbor is deleted
			UnitArray.DisappearInFirm(SpriteId); // ship also die if the harnor is deleted
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
		ShipStop shipStop = Stops[DestStopId - 1];
		int xLoc = StopLocX;
		int yLoc = StopLocY;
		Location loc = World.GetLoc(xLoc, yLoc);
		Firm firm;

		//TODO change %2 == 0
		if (xLoc % 2 == 0 && yLoc % 2 == 0 && loc.CanMove(MobileType))
			InitSprite(xLoc, yLoc); // appear in the location the unit disappeared before
		else
		{
			//---- the entering location is blocked, select another location to leave ----//
			firm = FirmArray[CurFirmId];

			if (AppearInFirmSurround(ref xLoc, ref yLoc, firm))
			{
				InitSprite(xLoc, yLoc);
				Stop();
			}
			else
			{
				WaitCount = GameConstants.MAX_SHIP_WAIT_TERM * 10; //********* BUGHERE, continue to wait or ....
				return;
			}
		}

		//-------------- get next stop id. ----------------//
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

		if (autoMode != 0) // move to next firm only if autoMode is on
			MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, Firm.FIRM_HARBOR);
	}
	
	public bool AppearInFirmSurround(ref int xLoc, ref int yLoc, Firm firm)
	{
		FirmInfo firmInfo = FirmRes[Firm.FIRM_HARBOR];
		int firmWidth = firmInfo.LocWidth;
		int firmHeight = firmInfo.LocHeight;
		int smallestCount = firmWidth * firmHeight + 1;
		int largestCount = (firmWidth + 2) * (firmHeight + 2);
		int countLimit = largestCount - smallestCount;
		int count = Misc.Random(countLimit) + smallestCount;
		int checkXLoc = 0, checkYLoc = 0;
		bool found = false;

		//-----------------------------------------------------------------//
		for (int i = 0; i < countLimit; i++)
		{
			Misc.cal_move_around_a_point(count, firmWidth, firmHeight, out int xOffset, out int yOffset);
			checkXLoc = firm.LocX1 + xOffset;
			checkYLoc = firm.LocY1 + yOffset;

			//TODO %2 == 0
			if (checkXLoc % 2 != 0 || checkYLoc % 2 != 0 ||
			    checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
			{
				count++;
				continue;
			}

			Location location = World.GetLoc(checkXLoc, checkYLoc);
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
			xLoc = checkXLoc;
			yLoc = checkYLoc;
			return true;
		}

		return false;
	}
	
	public int GetNextStopId(int curStopId)
	{
		int nextStopId = (curStopId >= StopDefinedNum) ? 1 : curStopId + 1;

		ShipStop stop = Stops[nextStopId - 1];

		bool needUpdate = false;

		if (FirmArray.IsDeleted(stop.FirmId))
		{
			needUpdate = true;
		}
		else
		{
			Firm firm = FirmArray[stop.FirmId];

			if (!CanSetStop(firm.FirmId) || firm.LocX1 != stop.FirmLocX1 || firm.LocY1 != stop.FirmLocY1)
			{
				needUpdate = true;
			}
		}

		if (needUpdate)
		{
			int preStopRecno = Stops[curStopId - 1].FirmId;
			UpdateStopList();

			if (StopDefinedNum == 0)
				return 0; // no stop is valid

			for (int i = 0; i < StopDefinedNum; i++)
			{
				stop = Stops[i];
				if (stop.FirmId == preStopRecno)
					nextStopId = (i >= StopDefinedNum) ? 1 : i + 1;
			}
		}

		return nextStopId;
	}
	
	public void ShipOnWay()
	{
		ShipStop shipStop = Stops[DestStopId - 1];
		Firm firm;
		int nextXLoc;
		int nextYLoc;
		int moveStep;

		if (CurAction == SPRITE_IDLE && JourneyStatus != InternalConstants.SURROUND_FIRM)
		{
			if (!FirmArray.IsDeleted(shipStop.FirmId))
			{
				firm = FirmArray[shipStop.FirmId];
				MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, Firm.FIRM_HARBOR);
				nextXLoc = NextLocX;
				nextYLoc = NextLocY;
				moveStep = MoveStepCoeff();
				if (nextXLoc >= firm.LocX1 - moveStep && nextXLoc <= firm.LocX2 + moveStep && nextYLoc >= firm.LocY1 - moveStep &&
				    nextYLoc <= firm.LocY2 + moveStep)
					JourneyStatus = InternalConstants.SURROUND_FIRM;

				return;
			}
		}

		if (UnitArray.IsDeleted(SpriteId))
			return; //-***************** BUGHERE ***************//

		if (FirmArray.IsDeleted(shipStop.FirmId))
		{
			UpdateStopList();

			if (StopDefinedNum != 0) // move to next stop
			{
				firm = FirmArray[Stops[StopDefinedNum - 1].FirmId];
				MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, firm.FirmType);
			}

			return;
		}

		//ShipStop *stop = stop_array + dest_stop_id - 1;
		firm = FirmArray[shipStop.FirmId];

		nextXLoc = NextLocX;
		nextYLoc = NextLocY;
		moveStep = MoveStepCoeff();
		if (JourneyStatus == InternalConstants.SURROUND_FIRM ||
		    (nextXLoc == MoveToLocX && nextYLoc == MoveToLocY && CurX == NextX && CurY == NextY && // move in a tile exactly
		     (nextXLoc >= firm.LocX1 - moveStep && nextXLoc <= firm.LocX2 + moveStep && nextYLoc >= firm.LocY1 - moveStep &&
		      nextYLoc <= firm.LocY2 + moveStep)))
		{
			ExtraMoveInBeach = NO_EXTRA_MOVE; // since the ship may enter the firm in odd location		

			shipStop.UpdatePickUp();

			//-------------------------------------------------------//
			// load/unload goods
			//-------------------------------------------------------//
			CurFirmId = shipStop.FirmId;

			if (NationArray[NationId].get_relation(firm.NationId).trade_treaty)
			{
				GetHarborLinkedFirmInfo();
				HarborUnloadGoods();
				if (shipStop.PickUpType == TradeStop.AUTO_PICK_UP)
					HarborAutoLoadGoods();
				else if (shipStop.PickUpType != TradeStop.NO_PICK_UP)
					HarborLoadGoods();
			}

			//-------------------------------------------------------//
			//-------------------------------------------------------//
			StopLocX = MoveToLocX; // store entering location
			StopLocY = MoveToLocY;
			WaitCount = GameConstants.MAX_SHIP_WAIT_TERM; // set waiting term

			ResetPath();
			DeinitSprite(true); // the ship enters the harbor now. 1-keep it selected if it is currently selected

			CurX--; // set cur_x to -2, such that invisible but still process pre_process()

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
				MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, Firm.FIRM_HARBOR);
				JourneyStatus = InternalConstants.ON_WAY_TO_FIRM;
			}
		}
	}
	
	public void GetHarborLinkedFirmInfo()
	{
		FirmHarbor firmHarbor = (FirmHarbor)FirmArray[CurFirmId];

		firmHarbor.UpdateLinkedFirmsInfo();
		_linkedMines.Clear();
		_linkedMines.AddRange(firmHarbor.LinkedMines);
		_linkedFactories.Clear();
		_linkedFactories.AddRange(firmHarbor.LinkedFactories);
		_linkedMarkets.Clear();
		_linkedMarkets.AddRange(firmHarbor.LinkedMarkets);
	}
	
	public bool CanSetStop(int firmRecno)
	{
		Firm firm = FirmArray[firmRecno];

		if (firm.UnderConstruction)
			return false;

		if (firm.FirmType != Firm.FIRM_HARBOR)
			return false;

		return NationArray[NationId].get_relation(firm.NationId).trade_treaty;
	}
	
	public void SetStop(int stopId, int stopXLoc, int stopYLoc, int remoteAction)
	{
		//-------------------------------------------------------//
		// check if there is a station in the given location
		//-------------------------------------------------------//
		Location loc = World.GetLoc(stopXLoc, stopYLoc);
		if (!loc.IsFirm())
			return;

		Firm firm = FirmArray[loc.FirmId()];
		if (!CanSetStop(firm.FirmId))
			return;

		//-------------------------------------------------------//
		// return if the harbor stop is in another territory
		//-------------------------------------------------------//
		FirmHarbor harbor = (FirmHarbor)firm;

		if (World.GetLoc(NextLocX, NextLocY).RegionId != harbor.SeaRegionId)
			return;

		//-----------------------------------------//

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

		if (Stops[stopId - 1].FirmId == 0)
			StopDefinedNum++; // no plus one if the recno is defined originally

		//-------------------------------------------------------//
		// set the station recno of the stop
		//-------------------------------------------------------//
		ShipStop shipStop = Stops[stopId - 1];
		if (shipStop.FirmId == firm.FirmId)
		{
			return; // same stop as before
		}

		int oldStopFirmRecno = DestStopId != 0 ? Stops[DestStopId - 1].FirmId : 0;
		shipStop.FirmId = firm.FirmId;
		shipStop.FirmLocX1 = firm.LocX1;
		shipStop.FirmLocY1 = firm.LocY1;

		//-------------------------------------------------------//
		// set pick up selection based on availability
		//-------------------------------------------------------//
		shipStop.PickUpSetAuto();

		int goodsId = 0, goodsNum = 0;
		for (int i = harbor.LinkedFirms.Count - 1; i >= 0 && goodsNum < 2; --i)
		{
			int id = 0;
			firm = FirmArray[harbor.LinkedFirms[i]];

			switch (firm.FirmType)
			{
				case Firm.FIRM_MINE:
					id = ((FirmMine)firm).RawId;
					if (id != 0)
					{
						if (goodsNum == 0)
							goodsId = id;
						goodsNum++;
					}

					break;
				case Firm.FIRM_FACTORY:
					id = ((FirmFactory)firm).ProductId + GameConstants.MAX_RAW;
					if (id != 0)
					{
						if (goodsNum == 0)
							goodsId = id;
						goodsNum++;
					}

					break;
				case Firm.FIRM_MARKET:
					for (int j = 0; j < GameConstants.MAX_MARKET_GOODS && goodsNum < 2; j++)
					{
						MarketGoods goods = ((FirmMarket)firm).MarketGoods[j];
						if (goods.RawId != 0)
						{
							id = goods.RawId;

							if (goodsNum == 0)
								goodsId = id;
							goodsNum++;
						}
						else if (goods.ProductId != 0)
						{
							id = goods.ProductId + GameConstants.MAX_RAW;

							if (goodsNum == 0)
								goodsId = id;
							goodsNum++;
						}
					}

					break;
				default:
					break;
			}
		}

		if (goodsNum == 1)
			shipStop.PickUpToggle(goodsId); // cancel auto_pick_up
		else if (goodsNum == 0)
			shipStop.PickUpSetNone();

		//-------------------------------------------------------//
		// remove duplicate stop or stop change nation
		//-------------------------------------------------------//
		UpdateStopList();

		//-------------------------------------------------------//
		// handle if current stop changed when mobile
		//-------------------------------------------------------//
		if (DestStopId != 0 && JourneyStatus != InternalConstants.INSIDE_FIRM)
		{
			int newStopFirmRecno = Stops[DestStopId - 1].FirmId;
			if (newStopFirmRecno != oldStopFirmRecno)
			{
				firm = FirmArray[newStopFirmRecno];
				MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, Firm.FIRM_HARBOR);
				JourneyStatus = InternalConstants.ON_WAY_TO_FIRM;
			}
		}
		else if (JourneyStatus != InternalConstants.INSIDE_FIRM)
			Stop2();
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
	
	public void UpdateStopList()
	{
		//-------------------------------------------------------//
		// backup original destination stop firm recno
		//-------------------------------------------------------//
		int nextStopRecno = DestStopId != 0 ? Stops[DestStopId - 1].FirmId : 0;

		//----------------------------------------------------------------------//
		// check stop existence and the relationship between firm's nation
		//----------------------------------------------------------------------//
		ShipStop stop;
		int i;
		for (i = 0; i < MAX_STOP_FOR_SHIP; i++)
		{
			stop = Stops[i];
			if (stop.FirmId == 0)
				continue;

			if (FirmArray.IsDeleted(stop.FirmId))
			{
				stop.FirmId = 0; // clear the recno
				StopDefinedNum--;
				continue;
			}

			Firm firm = FirmArray[stop.FirmId];

			if (!CanSetStop(firm.FirmId) || firm.LocX1 != stop.FirmLocX1 || firm.LocY1 != stop.FirmLocY1)
			{
				stop.FirmId = 0;
				StopDefinedNum--;
				continue;
			}
		}

		//-------------------------------------------------------//
		// remove duplicate node
		//-------------------------------------------------------//
		//ShipStop *insertNode = stop_array;
		int insertNodeIndex = 0;

		if (StopDefinedNum < 1)
		{
			for (i = 0; i < Stops.Length; i++)
				Stops[i] = new ShipStop();
			DestStopId = 0;
			return; // no stop
		}

		//-------------------------------------------------------//
		// move the only firm_recno to the beginning of the array
		//-------------------------------------------------------//
		int compareRecno = 0;
		int nodeIndex = 0;
		for (i = 0; i < MAX_STOP_FOR_SHIP; i++, nodeIndex++)
		{
			stop = Stops[nodeIndex];
			if (stop.FirmId != 0)
			{
				compareRecno = stop.FirmId;
				break;
			}
		}

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
			stop = Stops[nodeIndex];
			if (stop.FirmId == 0)
				continue; // empty

			if (stop.FirmId == compareRecno)
			{
				stop.FirmId = 0;
				StopDefinedNum--;
			}
			else
			{
				compareRecno = stop.FirmId;

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
			stop = Stops[nodeIndex]; // point to the end
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
			stop = Stops[i];
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
		// reset dest_stop_id since the order of the stop may be changed
		//-----------------------------------------------------------------------------------------//
		int xLoc = NextLocX;
		int yLoc = NextLocY;
		int minDist = Int32.MaxValue;

		for (i = 0, DestStopId = 0; i < StopDefinedNum; i++)
		{
			stop = Stops[i];
			if (stop.FirmId == nextStopRecno)
			{
				DestStopId = i + 1;
				break;
			}
			else
			{
				Firm firm = FirmArray[stop.FirmId];
				int dist = Misc.points_distance(xLoc, yLoc, firm.LocCenterX, firm.LocCenterY);

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
		//if (remote.is_enable())
		//{
			//if (!remoteAction)
			//{
				//// packet structure : <unit recno> <stop id> <new pick_up_type>
				//short* shortPtr = (short*)remote.new_send_queue_msg(MSG_U_SHIP_CHANGE_GOODS, 3 * sizeof(short));
				//*shortPtr = sprite_recno;

				//shortPtr[1] = stopId;
				//shortPtr[2] = newPickUpType;
				//return;
			//}
			//else //-------- validate remote message ----------//
			//{
				//-*******************************************************-//
				/*char mess[255];
				sprintf(mess, "Change Seed !!!! \r\n");
				OutputDebugString(mess);
	
				Firm *firm = FirmArray[stop_array[stopId-1].firm_recno];
				
				switch(firm.firm_id)
				{
					case FIRM_MINE:
							//firm.sell_firm(COMMAND_AUTO);
							//FirmArray[stop_array[0].firm_recno].sell_firm(COMMAND_AUTO);
							break;
					case FIRM_FACTORY:
							break;
					case FIRM_MARKET:
							break;
				}
	
				update_stop_list();
				if(UnitArray.selected_recno == sprite_recno)
				{
					if(!remote.is_enable() || nation_recno==NationArray.player_recno || config.show_ai_info)
					{
						int y=INFO_Y1+54;
						UnitInfo* unitInfo = UnitRes[unit_id];
						if( unitInfo.carry_unit_capacity && unitInfo.carry_goods_capacity )
							y+=25;
	
						disp_stop(y, INFO_UPDATE);
					}
				}*/
				//-*******************************************************-//

				//if (FirmArray.IsDeleted(stop_array[stopId - 1].firm_recno))
					//return; // firm is deleted

				//if (stop_defined_num < stopId)
					//return; // stop_list is updated, stop exists no more
			//}
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

		//TODO UI
		/*if (UnitArray.SelectedUnitId == SpriteId)
		{
			if (nation_recno == NationArray.player_recno || Config.show_ai_info)
			{
				int y = INFO_Y1 + 54;
				UnitInfo* unitInfo = UnitRes[unit_id];
				if (unitInfo.carry_unit_capacity && unitInfo.carry_goods_capacity)
					y += 25;

				disp_stop(y, INFO_UPDATE);
			}
		}*/
	}
	
	public void CopyRoute(int copyUnitRecno, int remoteAction)
	{
		if (SpriteId == copyUnitRecno)
			return;

		UnitMarine copyUnit = (UnitMarine)UnitArray[copyUnitRecno];

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
		int num_stops = StopDefinedNum;
		for (int i = 0; i < num_stops; i++)
			DelStop(1, InternalConstants.COMMAND_AUTO); // stop ids shift up

		for (int i = 0; i < MAX_STOP_FOR_SHIP; i++)
		{
			ShipStop shipStopA = copyUnit.Stops[i];
			ShipStop shipStopB = Stops[i];
			if (shipStopA.FirmId == 0)
				break;

			if (FirmArray.IsDeleted(shipStopA.FirmId))
				continue;

			Firm firm = FirmArray[shipStopA.FirmId];
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
				for (int b = 0; b < TradeStop.MAX_PICK_UP_GOODS; ++b)
				{
					if (shipStopA.PickUpEnabled[b] != shipStopB.PickUpEnabled[b])
						SetStopPickUp(i + 1, b + 1, InternalConstants.COMMAND_PLAYER);
				}
			}
		}
	}


	public void HarborUnloadGoods()
	{
		if (_linkedMines.Count + _linkedFactories.Count + _linkedMarkets.Count == 0)
			return;

		ProcessedRawQty.Clear();
		ProcessedProductQty.Clear();

		HarborUnloadProduct();
		HarborUnloadRaw();
	}

	public void HarborUnloadRaw()
	{
		if (_linkedFactories.Count == 0 && _linkedMarkets.Count == 0)
			return;

		int i, j, k;
		int totalDemand;
		int linkedFactoryIndex; // point to linked_factory_array
		int linkedMarketIndex; // point to linked_market_array
		int firmSelectedIndex; // mark which firm is used (for factory and market)
		FirmFactory factory;
		FirmMarket market;
		MarketGoods marketRaw;
		MarketGoods marketGoods; // used to find empty slot
		int curStock, unloadQty;
		bool useEmptySlot;

		for (i = 0; i < GameConstants.MAX_RAW; i++)
		{
			if (RawQty[i] == 0)
				continue; // without this goods

			totalDemand = 0;
			_emptySlotPositions.Clear();
			_selectedFirms.Clear();
			firmSelectedIndex = 0;
			//----------------------------------------------------------------------//
			// calculate the demand of this goods in factory
			//----------------------------------------------------------------------//
			linkedFactoryIndex = 0;
			for (j = 0; j < _linkedFactories.Count; j++, linkedFactoryIndex++, firmSelectedIndex++)
			{
				factory = (FirmFactory)FirmArray[_linkedFactories[linkedFactoryIndex]];

				if (factory.NationId != NationId)
					continue; // don't unload goods to factory of other nation

				if (factory.AIStatus == Firm.FACTORY_RELOCATE)
					continue; // clearing the factory stock, so no unloading

				if (factory.ProductId - 1 == i)
				{
					totalDemand = (int)(factory.MaxRawStockQty - factory.RawStockQty);
					firmSelectedIndex++;
				}
			}

			//----------------------------------------------------------------------//
			// calculate the demand of this goods in market
			//----------------------------------------------------------------------//
			linkedMarketIndex = 0;
			for (j = 0; j < _linkedMarkets.Count; j++, linkedMarketIndex++, firmSelectedIndex++)
			{
				market = (FirmMarket)FirmArray[_linkedMarkets[linkedMarketIndex]];

				if (market.NationId != NationId)
					continue; // don't unload goods to market of other nation

				if (market.AIStatus == Firm.MARKET_FOR_SELL)
					continue; // clearing the market stock, so no unloading

				//---------- check the demand of this goods in the market ---------//
				marketRaw = market.GetRawGoods(i + 1);
				if (marketRaw != null)
				{
					totalDemand += (int)(market.MaxStockQty - marketRaw.StockQty);
					firmSelectedIndex++;
				}
				else // don't have this raw, clear for empty slot
				{
					for (k = 0; k < GameConstants.MAX_MARKET_GOODS; k++)
					{
						marketGoods = market.MarketGoods[k];
						if (marketGoods.StockQty <= 0.0 && marketGoods.Supply30Days() <= 0.0)
						{
							_emptySlotPositions[j] = k;
							totalDemand += (int)market.MaxStockQty;
							firmSelectedIndex++;
							break;
						}
					}
				}
			}

			//----------------------------------------------------------------------//
			// distribute the stock into each factory
			//----------------------------------------------------------------------//
			curStock = RawQty[i];
			linkedFactoryIndex = 0;
			firmSelectedIndex = 0;
			for (j = 0; j < _linkedFactories.Count; j++, linkedFactoryIndex++, firmSelectedIndex++)
			{
				if (_selectedFirms[firmSelectedIndex] == 0)
					continue;

				factory = (FirmFactory)FirmArray[_linkedFactories[linkedFactoryIndex]];

				unloadQty = totalDemand != 0 ? (int)((factory.MaxRawStockQty - factory.RawStockQty) * curStock / totalDemand + 0.5) : 0;
				unloadQty = Math.Min((int)(factory.MaxRawStockQty - factory.RawStockQty), unloadQty);
				unloadQty = Math.Min(RawQty[i], unloadQty);

				factory.RawStockQty += unloadQty;
				RawQty[i] -= unloadQty;
				ProcessedRawQty[_linkedMines.Count + j][i] += 2;
			}

			//----------------------------------------------------------------------//
			// distribute the stock into each market
			//----------------------------------------------------------------------//
			linkedMarketIndex = 0;
			for (j = 0; j < _linkedMarkets.Count; j++, linkedMarketIndex++, firmSelectedIndex++)
			{
				if (_selectedFirms[firmSelectedIndex] == 0)
					continue;

				market = (FirmMarket)FirmArray[_linkedMarkets[linkedMarketIndex]];

				marketRaw = market.GetRawGoods(i + 1);
				if (marketRaw == null) // using empty slot, don't set the pointer to the market_goods_array until unloadQty>0
				{
					useEmptySlot = true;
					marketRaw = market.MarketGoods[_emptySlotPositions[j]];
				}
				else
					useEmptySlot = false;

				unloadQty = totalDemand != 0 ? (int)((market.MaxStockQty - marketRaw.StockQty) * curStock / totalDemand + 0.5) : 0;
				unloadQty = Math.Min((int)(market.MaxStockQty - marketRaw.StockQty), unloadQty);
				unloadQty = Math.Min(RawQty[i], unloadQty);

				if (unloadQty != 0)
				{
					if (useEmptySlot)
						market.SetRawGoods(i + 1, _emptySlotPositions[j]);

					marketRaw.StockQty += unloadQty;
					RawQty[i] -= unloadQty;
					ProcessedRawQty[_linkedMines.Count + _linkedFactories.Count + j][i] += 2;
				}
			}
		}
	}
	
	public void HarborUnloadProduct()
	{
		if (_linkedMarkets.Count == 0)
			return;

		int i, j, k;
		int totalDemand;
		int linkedMarketIndex; // point to linked_market_array
		int firmSelectedIndex; // mark which firm is used
		FirmMarket market;
		MarketGoods marketProduct;
		MarketGoods marketGoods; // used to find empty slot
		int curStock, unloadQty;
		bool useEmptySlot;

		for (i = 0; i < GameConstants.MAX_PRODUCT; i++)
		{
			if (ProductQty[i] == 0)
				continue; // without this goods

			//----------------------------------------------------------------------//
			// calculate the demand of this goods in market
			//----------------------------------------------------------------------//
			totalDemand = 0;
			_emptySlotPositions.Clear();
			_selectedFirms.Clear();
			linkedMarketIndex = 0;
			firmSelectedIndex = 0;
			for (j = 0; j < _linkedMarkets.Count; j++, linkedMarketIndex++, firmSelectedIndex++)
			{
				market = (FirmMarket)FirmArray[_linkedMarkets[linkedMarketIndex]];

				if (market.NationId != NationId)
					continue; // don't unload goods to market of other nation

				if (market.AIStatus == Firm.MARKET_FOR_SELL)
					continue; // clearing the market stock, so no unloading

				//---------- check the demand of this goods in the market ---------//
				marketProduct = market.GetProductGoods(i + 1);
				if (marketProduct != null)
				{
					totalDemand += (int)(market.MaxStockQty - marketProduct.StockQty);
					firmSelectedIndex++;
				}
				else // don't have this product, clear for empty slot
				{
					for (k = 0; k < GameConstants.MAX_MARKET_GOODS; k++)
					{
						marketGoods = market.MarketGoods[k];
						if (marketGoods.StockQty == 0 && marketGoods.Supply30Days() <= 0.0)
						{
							_emptySlotPositions[j] = k;
							totalDemand += (int)market.MaxStockQty;
							firmSelectedIndex++;
							break;
						}
					}
				}
			}

			//----------------------------------------------------------------------//
			// distribute the stock into each market
			//----------------------------------------------------------------------//
			curStock = ProductQty[i];
			linkedMarketIndex = 0;
			firmSelectedIndex = 0;
			for (j = 0; j < _linkedMarkets.Count; j++, linkedMarketIndex++, firmSelectedIndex++)
			{
				if (_selectedFirms[firmSelectedIndex] == 0)
					continue;

				market = (FirmMarket)FirmArray[_linkedMarkets[linkedMarketIndex]];

				marketProduct = market.GetProductGoods(i + 1);
				if (marketProduct == null) // using empty slot, don't set the pointer to the market_goods_array until unloadQty>0
				{
					useEmptySlot = true;
					marketProduct = market.MarketGoods[_emptySlotPositions[j]];
				}
				else
					useEmptySlot = false;

				unloadQty = totalDemand != 0 ? (int)((market.MaxStockQty - marketProduct.StockQty) * curStock / totalDemand + 0.5) : 0;
				unloadQty = Math.Min((int)(market.MaxStockQty - marketProduct.StockQty), unloadQty);
				unloadQty = Math.Min(ProductQty[i], unloadQty);

				if (unloadQty != 0)
				{
					if (useEmptySlot)
						market.SetProductGoods(i + 1, _emptySlotPositions[j]);

					marketProduct.StockQty += unloadQty;
					ProductQty[i] -= unloadQty;
					ProcessedProductQty[_linkedMines.Count + _linkedFactories.Count + j][i] += 2;
				}
			}
		}
	}

	public void HarborLoadGoods()
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
			int goodsId;
			if (pickUpType >= TradeStop.PICK_UP_RAW_FIRST && pickUpType <= TradeStop.PICK_UP_RAW_LAST)
			{
				goodsId = pickUpType - TradeStop.PICK_UP_RAW_FIRST;

				if (RawQty[goodsId] < CarryGoodsCapacity)
					HarborLoadRaw(goodsId, false, 1); // 1 -- only consider our firm

				if (RawQty[goodsId] < CarryGoodsCapacity)
					HarborLoadRaw(goodsId, false, 0); // 0 -- only consider firm of other nation
			}
			else if (pickUpType >= TradeStop.PICK_UP_PRODUCT_FIRST && pickUpType <= TradeStop.PICK_UP_PRODUCT_LAST)
			{
				goodsId = pickUpType - TradeStop.PICK_UP_PRODUCT_FIRST;

				if (ProductQty[goodsId] < CarryGoodsCapacity) // 1 -- only consider our firm
					HarborLoadProduct(goodsId, false, 1);

				if (ProductQty[goodsId] < CarryGoodsCapacity) // 0 -- only consider firm of other nation
					HarborLoadProduct(goodsId, false, 0);
			}
		}
	}

	public void HarborAutoLoadGoods()
	{
		if (_linkedMines.Count + _linkedFactories.Count + _linkedMarkets.Count == 0)
			return;

		for (int i = 0; i < GameConstants.MAX_PRODUCT; i++)
		{
			if (ProductQty[i] < CarryGoodsCapacity)
				HarborLoadProduct(i, true, 1); // 1 -- only consider our market
		}

		for (int i = 0; i < GameConstants.MAX_RAW; i++)
		{
			if (RawQty[i] < CarryGoodsCapacity)
				HarborLoadRaw(i, true, 1); // 1 -- only consider our market
		}
	}

	public void HarborLoadRaw(int goodsId, bool autoPickUp, int considerMode)
	{
		if (_linkedMines.Count + _linkedMarkets.Count == 0)
			return;

		if (RawQty[goodsId] == CarryGoodsCapacity)
			return;

		int i;
		int totalSupply;
		int linkedMineIndex; // point to linked_factory_array
		int linkedMarketIndex; // point to linked_market_array
		int firmSelectedIndex; // mark which firm is used (for factory and market)
		FirmMine mine;
		FirmMarket market;
		MarketGoods marketRaw;
		int loadQty, keepStockQty = 0;

		totalSupply = 0;
		_selectedFirms.Clear();
		firmSelectedIndex = 0;
		//----------------------------------------------------------------------//
		// calculate the supply of this goods in mine
		//----------------------------------------------------------------------//
		if (_linkedMines.Count > 0)
		{
			mine = (FirmMine)FirmArray[_linkedMines[0]];
			keepStockQty = autoPickUp ? (int)(mine.MaxStockQty / 5.0) : 0;
		}

		linkedMineIndex = 0;
		for (i = 0; i < _linkedMines.Count; i++, linkedMineIndex++, firmSelectedIndex++)
		{
			if (ProcessedRawQty[i][goodsId] == 2)
				continue;

			mine = (FirmMine)FirmArray[_linkedMines[linkedMineIndex]];

			if (considerMode != 0)
			{
				if (mine.NationId != NationId)
					continue; // not our market
			}
			else
			{
				if (mine.NationId == NationId)
					continue; // not consider our market for this mode
			}

			//---------- check the supply of this goods in the mine ---------//
			if (mine.RawId != goodsId + 1)
				continue; // incorrect goods

			totalSupply += Math.Max((int)(mine.StockQty - keepStockQty), 0);
			firmSelectedIndex++;
		}

		//----------------------------------------------------------------------//
		// calculate the supply of this goods in market
		//----------------------------------------------------------------------//
		if (_linkedMarkets.Count > 0)
		{
			market = (FirmMarket)FirmArray[_linkedMarkets[0]];
			keepStockQty = autoPickUp ? (int)(market.MaxStockQty / 5.0) : 0;
		}

		linkedMarketIndex = 0;
		for (i = 0; i < _linkedMarkets.Count; i++, linkedMarketIndex++, firmSelectedIndex++)
		{
			if (ProcessedRawQty[_linkedMines.Count + _linkedFactories.Count + i][goodsId] == 2)
				continue;

			market = (FirmMarket)FirmArray[_linkedMarkets[linkedMarketIndex]];

			if (considerMode != 0)
			{
				if (market.NationId != NationId)
					continue; // not our market
			}
			else
			{
				if (market.NationId == NationId)
					continue; // not consider our market for this mode
			}

			//---------- check the supply of this goods in the market ---------//
			marketRaw = market.GetRawGoods(goodsId + 1);
			if (marketRaw != null)
			{
				totalSupply += Math.Max((int)(marketRaw.StockQty - keepStockQty), 0);
				firmSelectedIndex++;
			}
		}

		Nation nation = NationArray[NationId];
		int curDemand = CarryGoodsCapacity - RawQty[goodsId];
		firmSelectedIndex = 0;
		//----------------------------------------------------------------------//
		// get the stock from each mine
		//----------------------------------------------------------------------//
		if (_linkedMines.Count > 0)
		{
			mine = (FirmMine)FirmArray[_linkedMines[0]];
			keepStockQty = autoPickUp ? (int)(mine.MaxStockQty / 5.0) : 0;
		}

		linkedMineIndex = 0;
		for (i = 0; i < _linkedMines.Count; i++, linkedMineIndex++, firmSelectedIndex++)
		{
			if (_selectedFirms[firmSelectedIndex] == 0)
				continue;

			mine = (FirmMine)FirmArray[_linkedMines[linkedMineIndex]];

			loadQty = Math.Max((int)(mine.StockQty - keepStockQty), 0);
			loadQty = totalSupply != 0 ? Math.Min(loadQty * curDemand / totalSupply, loadQty) : 0;

			if (mine.NationId != NationId)
			{
				loadQty = (nation.cash > 0) ? (int)Math.Min(nation.cash / GameConstants.RAW_PRICE, loadQty) : 0;
				if (loadQty > 0)
					nation.import_goods(NationBase.IMPORT_RAW, mine.NationId, loadQty * GameConstants.RAW_PRICE);
			}

			mine.StockQty -= loadQty;
			RawQty[goodsId] += loadQty;
		}

		//----------------------------------------------------------------------//
		// get the stock from each market
		//----------------------------------------------------------------------//
		if (_linkedMarkets.Count > 0)
		{
			market = (FirmMarket)FirmArray[_linkedMarkets[0]];
			keepStockQty = autoPickUp ? (int)(market.MaxStockQty / 5.0) : 0;
		}

		linkedMarketIndex = 0;
		for (i = 0; i < _linkedMarkets.Count; i++, linkedMarketIndex++, firmSelectedIndex++)
		{
			if (_selectedFirms[firmSelectedIndex] == 0)
				continue;

			market = (FirmMarket)FirmArray[_linkedMarkets[linkedMarketIndex]];

			marketRaw = market.GetRawGoods(goodsId + 1);

			loadQty = Math.Max((int)marketRaw.StockQty - keepStockQty, 0);
			loadQty = totalSupply != 0 ? Math.Min(loadQty * curDemand / totalSupply, loadQty) : 0;

			if (market.NationId != NationId)
			{
				loadQty = (nation.cash > 0) ? (int)Math.Min(nation.cash / GameConstants.RAW_PRICE, loadQty) : 0;
				if (loadQty > 0)
					nation.import_goods(NationBase.IMPORT_RAW, market.NationId, loadQty * GameConstants.RAW_PRICE);
			}

			marketRaw.StockQty -= loadQty;
			RawQty[goodsId] += loadQty;
		}
	}
	
	public void HarborLoadProduct(int goodsId, bool autoPickUp, int considerMode)
	{
		if (_linkedFactories.Count + _linkedMarkets.Count == 0)
			return;

		if (ProductQty[goodsId] == CarryGoodsCapacity)
			return;

		int i;
		int totalSupply;
		int linkedFactoryIndex; // point to linked_factory_array
		int linkedMarketIndex; // point to linked_market_array
		int firmSelectedIndex; // mark which firm is used (for factory and market)
		FirmFactory factory;
		FirmMarket market;
		MarketGoods marketProduct;
		int loadQty, keepStockQty = 0;

		totalSupply = 0;
		_selectedFirms.Clear();
		firmSelectedIndex = 0;
		//----------------------------------------------------------------------//
		// calculate the supply of this goods in factory
		//----------------------------------------------------------------------//
		if (_linkedFactories.Count > 0)
		{
			factory = (FirmFactory)FirmArray[_linkedFactories[0]];
			keepStockQty = autoPickUp ? (int)(factory.MaxStockQty / 5.0) : 0;
		}

		linkedFactoryIndex = 0;
		for (i = 0; i < _linkedFactories.Count; i++, linkedFactoryIndex++, firmSelectedIndex++)
		{
			if (ProcessedProductQty[_linkedMines.Count + i][goodsId] == 2)
				continue;

			factory = (FirmFactory)FirmArray[_linkedFactories[linkedFactoryIndex]];

			if (considerMode != 0)
			{
				if (factory.NationId != NationId)
					continue; // not our market
			}
			else
			{
				if (factory.NationId == NationId)
					continue; // not consider our market for this mode
			}

			//---------- check the supply of this goods in the factory ---------//
			if (factory.ProductId != goodsId + 1)
				continue; // incorrect product

			totalSupply += Math.Max((int)(factory.StockQty - keepStockQty), 0);
			firmSelectedIndex++;
		}

		//----------------------------------------------------------------------//
		// calculate the supply of this goods in market
		//----------------------------------------------------------------------//
		if (_linkedMarkets.Count > 0)
		{
			market = (FirmMarket)FirmArray[_linkedMarkets[0]];
			keepStockQty = autoPickUp ? (int)(market.MaxStockQty / 5.0) : 0;
		}

		linkedMarketIndex = 0;
		for (i = 0; i < _linkedMarkets.Count; i++, linkedMarketIndex++, firmSelectedIndex++)
		{
			if (ProcessedProductQty[_linkedMines.Count + _linkedFactories.Count + i][goodsId] == 2)
				continue;

			market = (FirmMarket)FirmArray[_linkedMarkets[linkedMarketIndex]];

			if (considerMode != 0)
			{
				if (market.NationId != NationId)
					continue; // not our market
			}
			else
			{
				if (market.NationId == NationId)
					continue; // not consider our market for this mode
			}

			//---------- check the supply of this goods in the market ---------//
			marketProduct = market.GetProductGoods(goodsId + 1);
			if (marketProduct != null)
			{
				totalSupply += Math.Max((int)(marketProduct.StockQty - keepStockQty), 0);
				firmSelectedIndex++;
			}
		}

		Nation nation = NationArray[NationId];
		int curDemand = CarryGoodsCapacity - ProductQty[goodsId];
		firmSelectedIndex = 0;
		//----------------------------------------------------------------------//
		// get the stock from each factory
		//----------------------------------------------------------------------//
		if (_linkedFactories.Count > 0)
		{
			factory = (FirmFactory)FirmArray[_linkedFactories[0]];
			keepStockQty = autoPickUp ? (int)(factory.MaxStockQty / 5.0) : 0;
		}

		linkedFactoryIndex = 0;
		for (i = 0; i < _linkedFactories.Count; i++, linkedFactoryIndex++, firmSelectedIndex++)
		{
			if (_selectedFirms[firmSelectedIndex] == 0)
				continue;

			factory = (FirmFactory)FirmArray[_linkedFactories[linkedFactoryIndex]];

			loadQty = Math.Max((int)(factory.StockQty - keepStockQty), 0);
			loadQty = totalSupply != 0 ? Math.Min(loadQty * curDemand / totalSupply, loadQty) : 0;

			if (factory.NationId != NationId)
			{
				loadQty = (nation.cash > 0) ? (int)Math.Min(nation.cash / GameConstants.PRODUCT_PRICE, loadQty) : 0;
				if (loadQty > 0)
					nation.import_goods(NationBase.IMPORT_PRODUCT, factory.NationId, loadQty * GameConstants.PRODUCT_PRICE);
			}

			factory.StockQty -= loadQty;
			ProductQty[goodsId] += loadQty;
		}

		//----------------------------------------------------------------------//
		// get the stock from each market
		//----------------------------------------------------------------------//
		if (_linkedMarkets.Count > 0)
		{
			market = (FirmMarket)FirmArray[_linkedMarkets[0]];
			keepStockQty = autoPickUp ? (int)(market.MaxStockQty / 5.0) : 0;
		}

		linkedMarketIndex = 0;
		for (i = 0; i < _linkedMarkets.Count; i++, linkedMarketIndex++, firmSelectedIndex++)
		{
			if (_selectedFirms[firmSelectedIndex] == 0)
				continue;

			market = (FirmMarket)FirmArray[_linkedMarkets[linkedMarketIndex]];

			marketProduct = market.GetProductGoods(goodsId + 1);

			loadQty = Math.Max((int)marketProduct.StockQty - keepStockQty, 0);
			loadQty = totalSupply != 0 ? Math.Min(loadQty * curDemand / totalSupply, loadQty) : 0;

			if (market.NationId != NationId)
			{
				loadQty = (nation.cash > 0) ? (int)Math.Min(nation.cash / GameConstants.PRODUCT_PRICE, loadQty) : 0;
				if (loadQty > 0)
					nation.import_goods(NationBase.IMPORT_PRODUCT, market.NationId, loadQty * GameConstants.PRODUCT_PRICE);
			}

			marketProduct.StockQty -= loadQty;
			ProductQty[goodsId] += loadQty;
		}
	}

	
	public bool CanUnloadUnit()
	{
		return false;
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

		//-------- unload unit now -------//

		if (UnloadingUnit(false, unitSeqId - 1)) // unit is unloaded
		{
			UnitsOnBoard.RemoveAt(unitSeqId - 1);
		}
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

		UnloadingUnit(true); // unload all units
	}

	public bool UnloadingUnit(bool isAll, int unitSeqId = 0)
	{
		if (!IsOnCoast())
			return false;

		//-------------------------------------------------------------------------//
		// return if no territory is nearby the ship
		//-------------------------------------------------------------------------//

		int curXLoc = NextLocX; // ship location
		int curYLoc = NextLocY;
		int unprocess = isAll ? UnitsOnBoard.Count : 1;
		Unit unit = isAll ? UnitArray[UnitsOnBoard[unprocess - 1]] : UnitArray[UnitsOnBoard[unitSeqId]];
		int regionId = 0; // unload all the units in the same territory
		int i = 2;
		bool found = false;
		int sqtSize = 5, sqtArea = sqtSize * sqtSize;

		while (unprocess > 0) // using the calculated 'i' to reduce useless calculation
		{
			Misc.cal_move_around_a_point(i, GameConstants.MapSize, GameConstants.MapSize, out int xShift, out int yShift);
			int checkXLoc = curXLoc + xShift;
			int checkYLoc = curYLoc + yShift;
			if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
			{
				i++;
				continue;
			}

			//-------------------------------------------------------------------------//
			// check for space to unload the unit
			//-------------------------------------------------------------------------//
			Location loc = World.GetLoc(checkXLoc, checkYLoc);
			if (regionId == 0 || loc.RegionId == regionId)
			{
				if (loc.Walkable())
					found = true;

				if (loc.CanMove(UnitConstants.UNIT_LAND))
				{
					regionId = loc.RegionId;

					unit.InitSprite(checkXLoc, checkYLoc);
					unit.SetMode(0);

					//TODO selection
					/*if (isAll && NationId == NationArray.player_recno)
					{
						unit.SelectedFlag = true; // mark selected if unload all
						UnitArray.SelectedCount++;

						if (UnitArray.SelectedUnitId == 0)
							UnitArray.SelectedUnitId = unit.SpriteId;
					}*/

					unprocess--;
					UnitsOnBoard.Remove(unit.SpriteId);

					if (unprocess > 0)
						unit = UnitArray[UnitsOnBoard[unprocess - 1]]; // point to next unit
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
					return false;
			}

			i++;
		}

		return true;
	}

	public bool IsOnCoast()
	{
		int curXLoc = NextLocX; // ship location
		int curYLoc = NextLocY;

		for (int i = 2; i <= 9; i++) // checking for the surrounding location
		{
			Misc.cal_move_around_a_point(i, 3, 3, out int xShift, out int yShift);

			int checkXLoc = curXLoc + xShift;
			int checkYLoc = curYLoc + yShift;

			if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
				continue;

			Location location = World.GetLoc(checkXLoc, checkYLoc);

			if (TerrainRes[location.TerrainId].AverageType != TerrainTypeCode.TERRAIN_OCEAN && location.Walkable())
			{
				return true;
			}
		}

		return false;
	}
	
	public void LoadUnit(int unitRecno)
	{
		if (UnitArray.IsDeleted(unitRecno))
			return;

		Unit unit = UnitArray[unitRecno];

		if (unit.HitPoints <= 0.0 || unit.CurAction == SPRITE_DIE || unit.ActionMode2 == UnitConstants.ACTION_DIE)
			return;

		if (UnitsOnBoard.Count == UnitConstants.MAX_UNIT_IN_SHIP)
			return;

		UnitsOnBoard.Add(unitRecno);

		unit.SetMode(UnitConstants.UNIT_MODE_ON_SHIP, SpriteId); // set unit mode

		//TODO selection
		/*if (unit.SelectedFlag)
		{
			unit.SelectedFlag = false;
			UnitArray.SelectedCount--;
		}*/

		unit.DeinitSprite();

		//--- if this marine unit is currently selected ---//

		//TODO UI
		/*if (UnitArray.SelectedUnitId == SpriteId)
		{
			if (!remote.is_enable() || nation_recno == NationArray.player_recno || Config.show_ai_info)
				disp_info(INFO_UPDATE);
		}*/
	}
	

	public void ExtraMove()
	{
		int[] offset = { 0, 1, -1 };

		int curXLoc = NextLocX;
		int curYLoc = NextLocY;

		int vecX = ActionLocX2 - curXLoc;
		int vecY = ActionLocY2 - curYLoc;
		int checkXLoc = -1, checkYLoc = -1;
		bool found = false;

		if (vecX == 0 || vecY == 0)
		{
			if (vecX == 0)
			{
				vecY /= Math.Abs(vecY);
				checkYLoc = curYLoc + vecY;
			}
			else // vecY==0
			{
				vecX /= Math.Abs(vecX);
				checkXLoc = curXLoc + vecX;
			}

			for (int i = 0; i < 3; i++)
			{
				if (vecX == 0)
					checkXLoc = curXLoc + offset[i];
				else
					checkYLoc = curYLoc + offset[i];

				if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
					continue;

				if (World.GetLoc(checkXLoc, checkYLoc).CanMove(MobileType))
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
			checkXLoc = curXLoc + vecX;
			checkYLoc = curYLoc + vecY;

			if (World.GetLoc(checkXLoc, checkYLoc).CanMove(MobileType))
				found = true;
		}

		if (!found)
			return;

		SetDir(curXLoc, curYLoc, checkXLoc, checkYLoc);
		CurAction = SPRITE_SHIP_EXTRA_MOVE;
		GoX = checkXLoc * InternalConstants.CellWidth;
		GoY = checkYLoc * InternalConstants.CellHeight;
		//extra_move_in_beach = EXTRA_MOVING_IN;
	}

	public override void ProcessExtraMove()
	{
		int[] vector_x_array = { 0, 1, 1, 1, 0, -1, -1, -1 }; // default vectors, temporarily only
		int[] vector_y_array = { -1, -1, 0, 1, 1, 1, 0, -1 };

		if (!MatchDir()) // process turning
			return;

		if (CurX != GoX || CurY != GoY)
		{
			//------------------------------------------------------------------------//
			// set cargo_recno, extra_move_in_beach
			//------------------------------------------------------------------------//
			if (CurX == NextX && CurY == NextY)
			{
				int goXLoc = GoX >> InternalConstants.CellWidthShift;
				int goYLoc = GoY >> InternalConstants.CellHeightShift;
				if (!World.GetLoc(goXLoc, goYLoc).CanMove(MobileType))
				{
					GoX = NextX;
					GoY = NextY;
					return;
				}

				int curXLoc = NextLocX;
				int curYLoc = NextLocY;
				World.GetLoc(curXLoc, curYLoc).SetUnit(MobileType, 0);
				World.GetLoc(goXLoc, goYLoc).SetUnit(MobileType, SpriteId);
				NextX = GoX;
				NextY = GoY;

				//TODO %2 == 0
				InBeach = !(curXLoc % 2 != 0 || curYLoc % 2 != 0);

				if (goXLoc % 2 != 0 || goYLoc % 2 != 0) // not even location
					ExtraMoveInBeach = EXTRA_MOVING_IN;
				else // even location
					ExtraMoveInBeach = EXTRA_MOVING_OUT;
			}

			//---------- process moving -----------//
			int stepX = SpriteInfo.Speed;
			int stepY = SpriteInfo.Speed;
			int vectorX = vector_x_array[FinalDir] * SpriteInfo.Speed; // cur_dir may be changed in the above set_next() call
			int vectorY = vector_y_array[FinalDir] * SpriteInfo.Speed;

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

			if (InBeach)
			{
				ExtraMoveInBeach = EXTRA_MOVE_FINISH;
			}
			else
			{
				ExtraMoveInBeach = NO_EXTRA_MOVE;
			}
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
			if (AttackModeSelected == 0)
			{
				ShipAttackInfo = UnitRes.GetAttackInfo(UnitRes[UnitType].first_attack);
			}

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