using System;

namespace TenKingdoms;

public class UnitCaravan : Unit
{
	public const int MAX_STOP_FOR_CARAVAN = 3;

	public int JourneyStatus { get; set; } // 1 for not unload but can up load, 2 for unload but not up load
	public int DestStopId { get; set; } // destination stop id. the stop which the train currently is moving towards
	public int StopDefinedNum { get; set; } // num of stop defined
	public int WaitCount { get; set; } // set to -1 to indicate only one stop is specified

	public int StopLocX { get; set; } // the x location the unit entering the stop
	public int StopLocY { get; set; } // the y location the unit entering the stop

	public CaravanStop[] Stops { get; } = new CaravanStop[MAX_STOP_FOR_CARAVAN];

	public DateTime LastSetStopDate { get; set; } // the date when stops were last set.
	public DateTime LastLoadGoodsDate { get; set; } // the last date when the caravan load goods from a firm  

	//------ goods that the caravan carries -------//

	public int[] RawQty { get; } = new int[GameConstants.MAX_RAW];
	public int[] ProductQty { get; } = new int[GameConstants.MAX_PRODUCT];

	private int[] ProcessedRawQty { get; } = new int[GameConstants.MAX_RAW];
	private int[] ProcessedProductQty { get; } = new int[GameConstants.MAX_PRODUCT];
	
	public UnitCaravan()
	{
		for (int i = 0; i < Stops.Length; i++)
			Stops[i] = new CaravanStop();

		JourneyStatus = InternalConstants.ON_WAY_TO_FIRM;
		DestStopId = 1;
		StopDefinedNum = 0;
		WaitCount = 0;
		StopLocX = 0;
		StopLocY = 0;
		Loyalty = 100;
	}

	public override void Init(int unitType, int nationId, int rank, int unitLoyalty, int startLocX, int startLocY)
	{
		base.Init(unitType, nationId, rank, unitLoyalty, startLocX, startLocY);
		LastLoadGoodsDate = Info.game_date;
	}

	public override void PreProcess()
	{
		base.PreProcess();

		if (CurX == -1) // can't use !is_visible(), keep process if cur_x < -1
			return;

		//-----------------------------------------------------------------------------//
		// if all the hit points are lost, die now
		//-----------------------------------------------------------------------------//
		if (HitPoints <= 0)
		{
			if (ActionMode != UnitConstants.ACTION_DIE)
				SetDie();

			return;
		}

		//-----------------------------------------------------------------------------//
		// process when in firm
		//-----------------------------------------------------------------------------//
		if (JourneyStatus == InternalConstants.INSIDE_FIRM)
		{
			CaravanInFirm();
			return;
		}

		//-----------------------------------------------------------------------------//
		// stop action if no stop is defined
		//-----------------------------------------------------------------------------//
		if (StopDefinedNum == 0)
		{
			if (JourneyStatus != InternalConstants.NO_STOP_DEFINED)
				Stop(); // stop if no valid stop is defined

			JourneyStatus = InternalConstants.NO_STOP_DEFINED;
			return;
		}

		//-----------------------------------------------------------------------------//
		// wait in the surrounding of the stop if stop_defined_num==1 (only one stop)
		//-----------------------------------------------------------------------------//
		if (StopDefinedNum == 1)
		{
			CaravanStop stop = Stops[0];

			if (FirmArray.IsDeleted(stop.FirmId))
			{
				UpdateStopList();
				return;
			}

			Firm firm = FirmArray[stop.FirmId];
			int firmXLoc1 = firm.LocX1;
			int firmYLoc1 = firm.LocY1;
			int firmXLoc2 = firm.LocX2;
			int firmYLoc2 = firm.LocY2;
			int firmId = firm.FirmType;
			if (firmXLoc1 != stop.FirmLocX1 || firmYLoc1 != stop.FirmLocY1 ||
			    (firmId != Firm.FIRM_MINE && firmId != Firm.FIRM_FACTORY && firmId != Firm.FIRM_MARKET))
			{
				UpdateStopList();
				return;
			}

			int curXLoc = NextLocX;
			int curYLoc = NextLocY;

			if (curXLoc < firmXLoc1 - 1 || curXLoc > firmXLoc2 + 1 || curYLoc < firmYLoc1 - 1 || curYLoc > firmYLoc2 + 1)
			{
				if (CurAction == SPRITE_IDLE)
					MoveToFirmSurround(firmXLoc1, firmYLoc1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, firmId);
				else
					JourneyStatus = InternalConstants.ON_WAY_TO_FIRM;
			}
			else
			{
				JourneyStatus = InternalConstants.SURROUND_FIRM;
				//if(firm->nation_recno==nation_recno)
				if (NationArray[NationId].get_relation(firm.NationId).trade_treaty)
				{
					if (WaitCount <= 0)
					{
						//---------- unloading goods -------------//
						switch (stop.FirmType)
						{
							case Firm.FIRM_MINE:
								break; // no goods unload to mine

							case Firm.FIRM_FACTORY:
								FactoryUnloadGoods();
								break;

							case Firm.FIRM_MARKET:
								MarketUnloadGoods();
								break;
						}

						WaitCount = GameConstants.MAX_CARAVAN_WAIT_TERM * InternalConstants.SURROUND_FIRM_WAIT_FACTOR;
					}
					else
						WaitCount--;
				}
			}

			return;
		}

		//-----------------------------------------------------------------------------//
		// at least 2 stops for the caravan to move between
		//-----------------------------------------------------------------------------//

		CaravanOnWay();
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
		// return if the market stop is in another territory
		//-------------------------------------------------------//
		if (World.GetLoc(NextLocX, NextLocY).RegionId != loc.RegionId)
			return;

		//-------------------------------------------//

		//if (!remoteAction && remote.is_enable())
		//{
		//// packet structure : <unit recno> <stop id> <stop x> <stop y>
		//short* shortPtr = (short*)remote.new_send_queue_msg(MSG_U_CARA_SET_STOP, 4 * sizeof(short));
		//*shortPtr = sprite_recno;
		//shortPtr[1] = stopId;
		//shortPtr[2] = stopXLoc;
		//shortPtr[3] = stopYLoc;
		//return;
		//}

		if (Stops[stopId - 1].FirmId == 0)
		{
			StopDefinedNum++; // no plus one if the recno is defined originally
		}

		//-------------------------------------------------------//
		// set the station recno of the stop
		//-------------------------------------------------------//
		CaravanStop stop = Stops[stopId - 1];
		if (stop.FirmId == firm.FirmId)
		{
			return; // same stop as before
		}

		//-------------- reset ignore_power_nation -------------//
		IgnorePowerNation = 0;

		int oldStopFirmRecno = DestStopId != 0 ? Stops[DestStopId - 1].FirmId : 0;
		int newStopFirmRecno;
		for (int i = 0; i < stop.PickUpEnabled.Length; i++)
			stop.PickUpEnabled[i] = false;
		stop.FirmId = firm.FirmId;
		stop.FirmType = firm.FirmType;
		stop.FirmLocX1 = firm.LocX1;
		stop.FirmLocY1 = firm.LocY1;

		int goodsId = 0;
		//------------------------------------------------------------------------------------//
		// codes for setting pick_up_type
		//------------------------------------------------------------------------------------//
		switch (firm.FirmType)
		{
			case Firm.FIRM_MINE:
				goodsId = ((FirmMine)firm).RawId;
				if (goodsId != 0)
					stop.PickUpToggle(goodsId); // enable
				else
					stop.PickUpSetNone();
				break;

			case Firm.FIRM_FACTORY:
				goodsId = ((FirmFactory)firm).ProductId + GameConstants.MAX_RAW;
				if (goodsId != 0)
					stop.PickUpToggle(goodsId); // enable
				else
					stop.PickUpSetNone();
				break;

			case Firm.FIRM_MARKET:
				int goodsNum = 0;
				for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
				{
					MarketGoods goods = ((FirmMarket)firm).MarketGoods[i];
					if (goods.RawId != 0)
					{
						if (goodsNum == 0)
							goodsId = goods.RawId;

						goodsNum++;
					}
					else if (goods.ProductId != 0)
					{
						if (goodsNum == 0)
							goodsId = goods.ProductId + GameConstants.MAX_RAW;

						goodsNum++;
					}
				}

				if (goodsNum == 1)
					stop.PickUpToggle(goodsId); // cancel auto_pick_up
				else if (goodsNum == 0)
					stop.PickUpSetNone();
				else
					stop.PickUpSetAuto();
				break;

			default:
				break;
		}

		LastSetStopDate = Info.game_date;

		//-------------------------------------------------------//
		// remove duplicate stop or stop change nation
		//-------------------------------------------------------//
		UpdateStopList();

		//-------------------------------------------------------//
		// handle if current stop changed when mobile
		//-------------------------------------------------------//
		if (DestStopId != 0 && JourneyStatus != InternalConstants.INSIDE_FIRM)
		{
			if ((newStopFirmRecno = Stops[DestStopId - 1].FirmId) != oldStopFirmRecno)
			{
				firm = FirmArray[newStopFirmRecno];
				MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, Stops[DestStopId - 1].FirmType);
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
		//short* shortPtr = (short*)remote.new_send_queue_msg(MSG_U_CARA_DEL_STOP, 2 * sizeof(short));
		//*shortPtr = sprite_recno;
		//shortPtr[1] = stopId;
		//return;
		//}

		//------ stop is deleted before receiving this message, thus, ignore invalid message -----//
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
		int nextStopRecno = Stops[DestStopId - 1].FirmId;

		//----------------------------------------------------------------------//
		// check stop existence and the relationship between firm's nation
		//----------------------------------------------------------------------//
		CaravanStop stop;
		int i = 0;
		for (i = 0; i < MAX_STOP_FOR_CARAVAN; i++)
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

			if (!CanSetStop(stop.FirmId) || firm.LocX1 != stop.FirmLocX1 || firm.LocY1 != stop.FirmLocY1)
			{
				stop.FirmId = 0;
				StopDefinedNum--;
				continue;
			}
		}

		//-------------------------------------------------------//
		// remove duplicate node
		//-------------------------------------------------------//
		//CaravanStop insertNodePtr = stop_array[0];
		int insertNodeIndex = 0;

		if (StopDefinedNum < 1)
		{
			for (i = 0; i < Stops.Length; i++)
				Stops[i] = new CaravanStop();
			DestStopId = 0;
			return; // no stop
		}

		//-------------------------------------------------------//
		// pack the firm_recno to the beginning part of the array
		//-------------------------------------------------------//
		int compareRecno = 0;
		int nodeIndex = 0;
		stop = Stops[nodeIndex];
		for (i = 0; i < MAX_STOP_FOR_CARAVAN; i++, nodeIndex++)
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
				Stops[i] = new CaravanStop();
			DestStopId = 1;
			return;
		}

		int unprocessed = StopDefinedNum - 1;
		insertNodeIndex++;
		nodeIndex++;

		for (; i < MAX_STOP_FOR_CARAVAN && unprocessed > 0; i++, nodeIndex++)
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

		if (StopDefinedNum < MAX_STOP_FOR_CARAVAN)
		{
			for (i = StopDefinedNum; i < Stops.Length; i++)
				Stops[i] = new CaravanStop();
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
				Stops[i] = new CaravanStop();
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
				//short* shortPtr = (short*)remote.new_send_queue_msg(MSG_U_CARA_CHANGE_GOODS, 3 * sizeof(short));
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
				OutputDebugString(mess);*/

				/*Firm *firm = firm_array[stop_array[stopId-1].firm_recno];
				
				switch(firm->firm_id)
				{
					case FIRM_MINE:
							//firm->sell_firm(COMMAND_AUTO);
							//firm_array[stop_array[0].firm_recno]->sell_firm(COMMAND_AUTO);
							break;
					case FIRM_FACTORY:
							break;
					case FIRM_MARKET:
							break;
				}
	
				update_stop_list();
				if(unit_array.selected_recno == sprite_recno)
				{
					if(!remote.is_enable() || nation_renco==nation_array.player_recno || config.show_ai_info)
						disp_stop(INFO_Y1+54, INFO_UPDATE);
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
	}

	public bool CanSetStop(int firmRecno)
	{
		Firm firm = FirmArray[firmRecno];

		if (firm.UnderConstruction)
			return false;

		switch (firm.FirmType)
		{
			case Firm.FIRM_MARKET:
				return NationArray[NationId].get_relation(firm.NationId).trade_treaty;

			case Firm.FIRM_MINE:
			case Firm.FIRM_FACTORY:
				return NationId == firm.NationId;

			default:
				return false;
		}
	}

	public bool HasPickUpType(int stopId, int pickUpType)
	{
		return Stops[stopId - 1].PickUpEnabled[pickUpType - 1];
	}

	public void CaravanInFirm()
	{
		//-----------------------------------------------------------------------------//
		// the market is deleted while the caravan is in market
		//-----------------------------------------------------------------------------//
		if (FirmArray.IsDeleted(ActionParam))
		{
			HitPoints = 0.0; // caravan also die if the market is deleted
			UnitArray.DisappearInFirm(SpriteId); // caravan also die if the market is deleted
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
		// leave the market and go to another market if possible
		//-----------------------------------------------------------------------------//
		CaravanStop caravanStop = Stops[DestStopId - 1];
		int xLoc = StopLocX;
		int yLoc = StopLocY;
		Location loc = World.GetLoc(xLoc, yLoc);
		Firm firm;

		if (loc.CanMove(MobileType))
			InitSprite(xLoc, yLoc); // appear in the location the unit disappeared before
		else
		{
			//---- the entering location is blocked, select another location to leave ----//
			firm = FirmArray[ActionParam];

			if (AppearInFirmSurround(ref xLoc, ref yLoc, firm))
			{
				InitSprite(xLoc, yLoc);
				Stop();
			}
			else
			{
				WaitCount = GameConstants.MAX_CARAVAN_WAIT_TERM * 10; //********* BUGHERE, continue to wait or ....
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

		ActionParam = 0; // since action_para is used to store the current market recno, reset before searching
		MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, firm.FirmType);

		JourneyStatus = InternalConstants.ON_WAY_TO_FIRM;
	}

	public void CaravanOnWay()
	{
		CaravanStop stop = Stops[DestStopId - 1];
		Firm firm = null;
		int nextXLoc = -1;
		int nextYLoc = -1;

		if (CurAction == SPRITE_IDLE && JourneyStatus != InternalConstants.SURROUND_FIRM)
		{
			if (!FirmArray.IsDeleted(stop.FirmId))
			{
				firm = FirmArray[stop.FirmId];
				MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, firm.FirmType);
				nextXLoc = NextLocX;
				nextYLoc = NextLocY;

				// hard code 1 for caravan size 1x1
				if (nextXLoc >= firm.LocX1 - 1 && nextXLoc <= firm.LocX2 + 1 && nextYLoc >= firm.LocY1 - 1 &&
				    nextYLoc <= firm.LocY2 + 1)
					JourneyStatus = InternalConstants.SURROUND_FIRM;

				if (nextXLoc == MoveToLocX && nextYLoc == MoveToLocY && IgnorePowerNation == 0)
					IgnorePowerNation = 1;

				return;
			}
		}

		int unitRecno = SpriteId;

		if (UnitArray.IsDeleted(unitRecno))
			return; //-***************** BUGHERE ***************//

		if (FirmArray.IsDeleted(stop.FirmId))
		{
			UpdateStopList();

			if (StopDefinedNum != 0) // move to next stop
			{
				firm = FirmArray[Stops[StopDefinedNum - 1].FirmId];
				MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, firm.FirmType);
			}

			return;
		}

		//CaravanStop *stop = stop_array + dest_stop_id - 1;
		firm = FirmArray[stop.FirmId];

		nextXLoc = NextLocX;
		nextYLoc = NextLocY;

		if (JourneyStatus == InternalConstants.SURROUND_FIRM ||
		    (nextXLoc == MoveToLocX && nextYLoc == MoveToLocY && CurX == NextX && CurY == NextY && // move in a tile exactly
		     (nextXLoc >= firm.LocX1 - 1 && nextXLoc <= firm.LocX2 + 1 &&
		      nextYLoc >= firm.LocY1 - 1 && nextYLoc <= firm.LocY2 + 1))) // in the surrounding of the firm
		{
			//-------------------- update pick_up_array --------------------//
			stop.UpdatePickUp();

			//-------------------------------------------------------//
			// load/unload goods
			//-------------------------------------------------------//
			if (NationArray[NationId].get_relation(firm.NationId).trade_treaty)
			{
				switch (firm.FirmType)
				{
					case Firm.FIRM_MINE:
						MineLoadGoods(stop.PickUpType);
						break;

					case Firm.FIRM_FACTORY:
						FactoryUnloadGoods();
						FactoryLoadGoods(stop.PickUpType);
						break;

					case Firm.FIRM_MARKET:
						MarketUnloadGoods();

						if (stop.PickUpType == TradeStop.AUTO_PICK_UP)
							MarketAutoLoadGoods();
						else if (stop.PickUpType != TradeStop.NO_PICK_UP)
							MarketLoadGoods();
						break;
				}
			}

			//-------------------------------------------------------//
			// action_para is used to store the firm_recno of the market
			// where the caravan move in.
			//-------------------------------------------------------//
			ActionParam = stop.FirmId;

			StopLocX = MoveToLocX; // store entering location
			StopLocY = MoveToLocY;
			WaitCount = GameConstants.MAX_CARAVAN_WAIT_TERM; // set waiting term

			ResetPath();
			DeinitSprite(true); // the caravan enters the market now. 1-keep it selected if it is currently selected

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
				MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, firm.FirmType);
				JourneyStatus = InternalConstants.ON_WAY_TO_FIRM;
			}
		}
	}

	private int GetNextStopId(int curStopId = MAX_STOP_FOR_CARAVAN)
	{
		int nextStopId = (curStopId >= StopDefinedNum) ? 1 : curStopId + 1;

		CaravanStop stop = Stops[nextStopId - 1];

		bool needUpdate = false;

		if (FirmArray.IsDeleted(stop.FirmId))
		{
			needUpdate = true;
		}
		else
		{
			Firm firm = FirmArray[stop.FirmId];

			if (!CanSetStop(stop.FirmId) ||
			    firm.LocX1 != stop.FirmLocX1 || firm.LocY1 != stop.FirmLocY1)
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

			for (int i = 1; i <= StopDefinedNum; i++)
			{
				stop = Stops[i - 1];
				if (stop.FirmId == preStopRecno)
					return (i >= StopDefinedNum) ? 1 : i + 1;
			}

			return 1;
		}
		else
		{
			return nextStopId;
		}
	}
	
	private bool AppearInFirmSurround(ref int xLoc, ref int yLoc, Firm firm)
	{
		int upperLeftBoundX = firm.LocX1 - 1; // the surrounding coordinates of the firm
		int upperLeftBoundY = firm.LocY1 - 1;
		int lowerRightBoundX = firm.LocX2 + 1;
		int lowerRightBoundY = firm.LocY2 + 1;

		int count = 1;
		int testXLoc = xLoc;
		int testYLoc = yLoc;
		bool found = false;
		bool inside = true;

		//---------------------------------------------------------//
		//		9  10  11  12		the location is tested in the order
		//		8   1   2  13		shown, if the location is the surrounding
		//		7   x   3  14		of the firm and non-blocked, break
		//		6   5   4  ...		the test
		//---------------------------------------------------------//

		while (inside)
		{
			inside = false;
			int limit = count << 1;

			//------------ upper --------------//
			testXLoc = xLoc - count + 1;
			testYLoc = yLoc - count;
			for (int i = 0; i < limit; i++)
			{
				if (testXLoc < 0 || testXLoc >= GameConstants.MapSize || testYLoc < 0 || testYLoc >= GameConstants.MapSize)
					continue;

				if (testXLoc < upperLeftBoundX || testXLoc > lowerRightBoundX || testYLoc < upperLeftBoundY || testYLoc > lowerRightBoundY)
					continue;

				Location location = World.GetLoc(testXLoc, testYLoc);
				if (location.CanMove(MobileType))
				{
					found = true;
					break;
				}
				else
					xLoc++;

				inside = true;
			}

			if (found)
				break;

			//------------ right --------------//
			testXLoc = xLoc + count;
			testYLoc = yLoc - count + 1;
			for (int i = 0; i < limit; i++)
			{
				if (testXLoc < 0 || testXLoc >= GameConstants.MapSize || testYLoc < 0 || testYLoc >= GameConstants.MapSize)
					continue;

				if (testXLoc < upperLeftBoundX || testXLoc > lowerRightBoundX || testYLoc < upperLeftBoundY || testYLoc > lowerRightBoundY)
					continue;

				Location location = World.GetLoc(testXLoc, testYLoc);
				if (location.CanMove(MobileType))
				{
					found = true;
					break;
				}
				else
					yLoc++;

				inside = true;
			}

			if (found)
				break;

			//------------- down --------------//
			testXLoc = xLoc + count - 1;
			testYLoc = yLoc + count;
			for (int i = 0; i < limit; i++)
			{
				if (testXLoc < 0 || testXLoc >= GameConstants.MapSize || testYLoc < 0 || testYLoc >= GameConstants.MapSize)
					continue;

				if (testXLoc < upperLeftBoundX || testXLoc > lowerRightBoundX || testYLoc < upperLeftBoundY || testYLoc > lowerRightBoundY)
					continue;

				Location location = World.GetLoc(testXLoc, testYLoc);
				if (location.CanMove(MobileType))
				{
					found = true;
					break;
				}
				else
					xLoc--;

				inside = true;
			}

			if (found)
				break;

			//------------- left --------------//
			testXLoc = xLoc - count;
			testYLoc = yLoc + count - 1;
			for (int i = 0; i < limit; i++)
			{
				if (testXLoc < 0 || testXLoc >= GameConstants.MapSize || testYLoc < 0 || testYLoc >= GameConstants.MapSize)
					continue;

				if (testXLoc < upperLeftBoundX || testXLoc > lowerRightBoundX || testYLoc < upperLeftBoundY || testYLoc > lowerRightBoundY)
					continue;

				Location location = World.GetLoc(testXLoc, testYLoc);
				if (location.CanMove(MobileType))
				{
					found = true;
					break;
				}
				else
					yLoc--;

				inside = true;
			}

			if (found)
				break;

			count++;
		}

		if (found)
		{
			xLoc = testXLoc;
			yLoc = testYLoc;
			return true;
		}

		return false;
	}

	public void CopyRoute(int copyUnitRecno, int remoteAction)
	{
		if (SpriteId == copyUnitRecno)
			return;

		UnitCaravan copyUnit = (UnitCaravan)UnitArray[copyUnitRecno];

		if (copyUnit.NationId != NationId)
			return;

		//if (remote.is_enable() && !remoteAction)
		//{
		//// packet structure : <unit recno> <copy recno>
		//short* shortPtr = (short*)remote.new_send_queue_msg(MSG_U_CARA_COPY_ROUTE, 2 * sizeof(short));
		//*shortPtr = sprite_recno;

		//shortPtr[1] = copyUnitRecno;
		//return;
		//}

		// clear existing stops
		int num_stops = StopDefinedNum;
		for (int i = 0; i < num_stops; i++)
			DelStop(1, InternalConstants.COMMAND_AUTO); // stop ids shift up

		for (int i = 0; i < MAX_STOP_FOR_CARAVAN; i++)
		{
			CaravanStop caravanStopA = copyUnit.Stops[i];
			CaravanStop caravanStopB = Stops[i];
			if (caravanStopA.FirmId == 0)
				break;

			if (FirmArray.IsDeleted(caravanStopA.FirmId))
				continue;

			Firm firm = FirmArray[caravanStopA.FirmId];
			SetStop(i + 1, caravanStopA.FirmLocX1, caravanStopA.FirmLocY1, InternalConstants.COMMAND_AUTO);

			if (caravanStopA.PickUpType == TradeStop.AUTO_PICK_UP)
			{
				SetStopPickUp(i + 1, TradeStop.AUTO_PICK_UP, InternalConstants.COMMAND_AUTO);
			}

			else if (caravanStopA.PickUpType == TradeStop.NO_PICK_UP)
			{
				SetStopPickUp(i + 1, TradeStop.NO_PICK_UP, InternalConstants.COMMAND_AUTO);
			}

			else
			{
				for (int b = 0; b < TradeStop.MAX_PICK_UP_GOODS; ++b)
				{
					if (caravanStopA.PickUpEnabled[b] != caravanStopB.PickUpEnabled[b])
						SetStopPickUp(i + 1, b + 1, InternalConstants.COMMAND_PLAYER);
				}
			}
		}
	}

	private void MineLoadGoods(int pickUpType)
	{
		if (pickUpType == TradeStop.NO_PICK_UP)
			return; // return if not allowed to load any goods

		CaravanStop stop = Stops[DestStopId - 1];
		FirmMine curMine = (FirmMine)FirmArray[stop.FirmId];

		if (curMine.NationId != NationId)
			return; // no action if this is not our own mine

		//------------- load goods -----------//
		int searchRawId = pickUpType - TradeStop.PICK_UP_RAW_FIRST + 1;
		if (pickUpType == TradeStop.AUTO_PICK_UP || curMine.RawId == searchRawId) // auto_pick_up or is the raw to pick up
		{
			int goodsId = curMine.RawId - 1;
			int maxLoadQty = (pickUpType != TradeStop.AUTO_PICK_UP)
				? (int)curMine.StockQty
				: Math.Max(0, (int)curMine.StockQty - GameConstants.MIN_FIRM_STOCK_QTY); // MAX Qty mine can supply
			int qty = Math.Min(GameConstants.MAX_CARAVAN_CARRY_QTY - RawQty[goodsId], maxLoadQty); // MAX Qty caravan can carry

			RawQty[goodsId] += qty;
			curMine.StockQty -= qty;

			if (maxLoadQty > 0)
				LastLoadGoodsDate = Info.game_date;
		}
	}

	private void FactoryUnloadGoods()
	{
		CaravanStop stop = Stops[DestStopId - 1];
		FirmFactory curFactory = (FirmFactory)FirmArray[stop.FirmId];

		if (curFactory.NationId != NationId)
			return; // don't unload goods if this isn't our own factory

		//--- if the factory does not have any stock and there is no production, set it to type of raw materials the caravan is carring ---//

		if (curFactory.StockQty == 0 && curFactory.RawStockQty == 0 && curFactory.Production30Days() == 0)
		{
			int rawCount = 0;
			int rawId = 0;

			for (int i = 0; i < GameConstants.MAX_RAW; i++)
			{
				if (RawQty[i] > 0)
				{
					rawCount++;
					rawId = i + 1;
				}
			}

			//-- only if the caravan only carries one type of raw material --//

			if (rawCount == 1 && rawId != 0)
				curFactory.ProductId = rawId;
		}

		//---------- unload materials automatically --------//
		int goodsId = curFactory.ProductId - 1;

		if (RawQty[goodsId] != 0) // caravan has this raw materials
		{
			int qty = Math.Min(RawQty[goodsId], (int)(curFactory.MaxRawStockQty - curFactory.RawStockQty));
			RawQty[goodsId] -= qty;
			curFactory.RawStockQty += qty;
		}
	}

	private void FactoryLoadGoods(int pickUpType)
	{
		if (pickUpType == TradeStop.NO_PICK_UP)
			return; // return not allowed to load any goods

		CaravanStop stop = Stops[DestStopId - 1];
		FirmFactory curFactory = (FirmFactory)FirmArray[stop.FirmId];

		if (curFactory.NationId != NationId)
			return; // don't load goods if this isn't our own factory

		//------------- load goods -----------//
		int searchProductRawId = pickUpType - TradeStop.PICK_UP_PRODUCT_FIRST + 1;
		if (pickUpType == TradeStop.AUTO_PICK_UP || curFactory.ProductId == searchProductRawId) // auto_pick_up or is the product to pick up
		{
			int goodsId = curFactory.ProductId - 1;
			int maxLoadQty = (pickUpType != TradeStop.AUTO_PICK_UP)
				? (int)curFactory.StockQty
				: Math.Max(0, (int)curFactory.StockQty - GameConstants.MIN_FIRM_STOCK_QTY); // MAX Qty factory can supply
			int qty = Math.Min(GameConstants.MAX_CARAVAN_CARRY_QTY - ProductQty[goodsId], maxLoadQty); // MAX Qty caravan can carry

			ProductQty[goodsId] += qty;
			curFactory.StockQty -= qty;

			if (maxLoadQty > 0)
				LastLoadGoodsDate = Info.game_date;
		}
	}

	private void MarketUnloadGoods()
	{
		FirmMarket curMarket = (FirmMarket)FirmArray[Stops[DestStopId - 1].FirmId];

		for (int i = 0; i < ProcessedRawQty.Length; i++)
			ProcessedRawQty[i] = 0;

		for (int i = 0; i < ProcessedProductQty.Length; i++)
			ProcessedProductQty[i] = 0;

		//--------------------------------------------------------------------//
		// only unload goods to our market
		//--------------------------------------------------------------------//
		if (curMarket.NationId != NationId)
			return;

		//--------------------------------------------------------------------//
		// unload goods
		//-------------------------------------------------//
		int withEmptySlot = 0;

		for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = curMarket.MarketGoods[i];
			int unloadQty;
			int goodsId;
			if (marketGoods.RawId != 0)
			{
				//-------------- is raw material ----------------//
				goodsId = marketGoods.RawId - 1;

				//if( (marketGoods->supply_30days()==0 && marketGoods->stock_qty<curMarket->max_stock_qty) || // no supply and stock isn't full
				//	 (marketGoods->stock_qty<CARAVAN_UNLOAD_TO_MARKET_QTY &&
				//	 //##### begin trevor 16/7 #######//
				//	  marketGoods->month_demand > marketGoods->supply_30days()) ) // demand > supply
				//	 //##### end trevor 16/7 #######//
				if (marketGoods.StockQty < curMarket.MaxStockQty)
				{
					//-------- demand > supply and stock is not full ----------//
					if (RawQty[goodsId] != 0) // have this goods
					{
						//---------- process unload -------------//
						unloadQty = Math.Min(RawQty[goodsId], (int)(curMarket.MaxStockQty - marketGoods.StockQty));
						RawQty[goodsId] -= unloadQty;
						marketGoods.StockQty += unloadQty;
						ProcessedRawQty[goodsId] += 2;
					}
					else if (marketGoods.StockQty <= 0.0 && marketGoods.Supply30Days() <= 0.0)
					{
						//---------- no supply, no stock, without this goods ------------//
						withEmptySlot++;
						//processed_raw_qty_array[goodsId] = 0; // reset to zero for handling empty slot
					}
				}
				else if (RawQty[goodsId] != 0) // have this goods
				{
					ProcessedRawQty[goodsId]++;
				}
			}
			else if (marketGoods.ProductId != 0)
			{
				//---------------- is product -------------------//
				goodsId = marketGoods.ProductId - 1;

				//if( (marketGoods->supply_30days()==0 && marketGoods->stock_qty<curMarket->max_stock_qty) || // no supply and stock isn't full
				//	 //##### begin trevor 16/7 #######//
				//	 (marketGoods->stock_qty<50 && marketGoods->month_demand > marketGoods->supply_30days()) ) // demand > supply
				//	 //##### end trevor 16/7 #######//
				if (marketGoods.StockQty < curMarket.MaxStockQty)
				{
					if (ProductQty[goodsId] != 0) // have this goods
					{
						unloadQty = Math.Min(ProductQty[goodsId], (int)(curMarket.MaxStockQty - marketGoods.StockQty));
						ProductQty[goodsId] -= unloadQty;
						marketGoods.StockQty += unloadQty;
						ProcessedProductQty[goodsId] += 2;
					}
					else if (marketGoods.StockQty <= 0.0 && marketGoods.Supply30Days() <= 0.0) // no supply, no stock, without this goods
					{
						withEmptySlot++;
						//processed_product_raw_qty_array[goodsId] = 0; // reset to zero for handling empty slot
					}
				}
				else if (ProductQty[goodsId] != 0) // have this goods
				{
					ProcessedProductQty[goodsId]++;
				}
			}
			else // is empty
			{
				if (!MarketUnloadGoodsInEmptySlot(curMarket, i))
					break; // no goods for further checking
			}
		}

		//-------------------------------------------------//
		// unload new goods in the empty slots
		//-------------------------------------------------//
		if (withEmptySlot > 0)
		{
			for (int i = 0; i < GameConstants.MAX_MARKET_GOODS && withEmptySlot > 0; i++)
			{
				MarketGoods marketGoods = curMarket.MarketGoods[i];
				if (marketGoods.StockQty > 0.0 || marketGoods.Supply30Days() > 0.0)
					continue;

				MarketUnloadGoodsInEmptySlot(curMarket, i);
				withEmptySlot--;
			}
		}
	}

	private bool MarketUnloadGoodsInEmptySlot(FirmMarket curMarket, int position)
	{
		bool moreToUnload = false;
		MarketGoods marketGoods = curMarket.MarketGoods[position];

		//-------------------------------------------------//
		// unload product and then raw
		//-------------------------------------------------//
		int processed, j;
		for (processed = 0, j = 0; j < GameConstants.MAX_PRODUCT; j++)
		{
			if (ProcessedProductQty[j] != 0 || ProductQty[j] == 0)
				continue; // this product is processed or no stock in the caravan

			// this can be unloaded, but check if it can be
			// unloaded into an already provided market slot
			// for this product type

			bool productExistInOtherSlot = false;
			for (int k = 0; k < GameConstants.MAX_MARKET_GOODS; k++)
			{
				MarketGoods checkGoods = curMarket.MarketGoods[k];
				if (checkGoods.ProductId == j + 1)
				{
					productExistInOtherSlot = true;
					break;
				}
			}

			if (productExistInOtherSlot)
			{
				moreToUnload = true;
				continue;
			}

			// this does not exist in a market slot, so unload
			// in this empty one

			//-**************************************************-//
			marketGoods.StockQty = 0.0; // BUGHERE, there is a case that marketGoods->stock_qty > 0
			//-**************************************************-//
			ProcessedProductQty[j] += 2;
			curMarket.SetProductGoods(j + 1, position);

			int unloadQty = Math.Min(ProductQty[j], (int)(curMarket.MaxStockQty - marketGoods.StockQty));
			ProductQty[j] -= unloadQty;
			marketGoods.StockQty += unloadQty;
			processed++;
			break;
		}

		if (processed == 0)
		{
			for (j = 0; j < GameConstants.MAX_PRODUCT; j++)
			{
				if (ProcessedRawQty[j] != 0 || RawQty[j] == 0)
					continue; // this product is processed or no stock in the caravan

				// this can be unloaded, but check if it can be
				// unloaded into an already provided market slot
				// for this product type

				bool rawExistInOtherSlot = false;
				for (int k = 0; k < GameConstants.MAX_MARKET_GOODS; k++)
				{
					MarketGoods checkGoods = curMarket.MarketGoods[k];
					if (checkGoods.RawId == j + 1)
					{
						rawExistInOtherSlot = true;
						break;
					}
				}

				if (rawExistInOtherSlot)
				{
					moreToUnload = true;
					continue;
				}

				// this does not exist in a market slot, so unload
				// in this empty one

				//-**************************************************-//
				marketGoods.StockQty = 0.0; // BUGHERE, there is a case that marketGoods->stock_qty > 0
				//-**************************************************-//
				ProcessedRawQty[j] += 2;
				curMarket.SetRawGoods(j + 1, position);

				int unloadQty = Math.Min(RawQty[j], (int)(curMarket.MaxStockQty - marketGoods.StockQty));
				RawQty[j] -= unloadQty;
				marketGoods.StockQty += unloadQty;
				processed++;
				break;
			}
		}

		return processed > 0 || moreToUnload;
	}

	private void MarketLoadGoods()
	{
		CaravanStop stop = Stops[DestStopId - 1];

		FirmMarket curMarket = (FirmMarket)FirmArray[stop.FirmId];

		//------------------------------------------------------------//
		// scan the market, see if it has the specified pickup goods
		//------------------------------------------------------------//
		for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = curMarket.MarketGoods[i];
			if (marketGoods.RawId != 0)
			{
				if (stop.PickUpEnabled[marketGoods.RawId - 1])
					MarketLoadGoodsNow(marketGoods, marketGoods.StockQty);
			}
			else if (marketGoods.ProductId != 0)
			{
				if (stop.PickUpEnabled[marketGoods.ProductId - 1 + GameConstants.MAX_RAW])
					MarketLoadGoodsNow(marketGoods, marketGoods.StockQty);
			}
		}
	}

	private void MarketAutoLoadGoods()
	{
		FirmMarket curMarket = (FirmMarket)FirmArray[Stops[DestStopId - 1].FirmId];

		//int	isOurMarket = (curMarket->nation_recno==nation_recno); // is 1 or 0

		//----------------------------------------------------------------------//
		// keep empty stock if the market(AI) is for sale, otherwise use the
		// default value
		//----------------------------------------------------------------------//
		//short minFirmStockQty = (int)curMarket->max_stock_qty/5; // keep at least 20% capacity in the firm if the market is not for sale

		for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = curMarket.MarketGoods[i];
			if (marketGoods.StockQty <= 0.0)
				continue;

			int goodsId;
			int loadQty;
			if (marketGoods.RawId != 0)
			{
				goodsId = marketGoods.RawId;
				goodsId--;
				if (ProcessedRawQty[goodsId] == 2)
					continue; // continue if it is the goods unloaded

				if (marketGoods.StockQty > GameConstants.MIN_FIRM_STOCK_QTY)
				{
					loadQty = (int)marketGoods.StockQty - GameConstants.MIN_FIRM_STOCK_QTY;
					MarketLoadGoodsNow(marketGoods, loadQty);
				}
			}
			//else if(marketGoods->product_raw_id && isOurMarket) // only load product in our market
			else if (marketGoods.ProductId != 0)
			{
				goodsId = marketGoods.ProductId;
				goodsId--;
				if (ProcessedProductQty[goodsId] == 2)
					continue; // continue if it is the goods unloaded

				if (marketGoods.StockQty > GameConstants.MIN_FIRM_STOCK_QTY)
				{
					loadQty = (int)marketGoods.StockQty - GameConstants.MIN_FIRM_STOCK_QTY;
					MarketLoadGoodsNow(marketGoods, loadQty);
				}
			}
		}
	}

	private void MarketLoadGoodsNow(MarketGoods marketGoods, double loadQty)
	{
		Nation nation = NationArray[NationId];
		int marketNationRecno = FirmArray[Stops[DestStopId - 1].FirmId].NationId;
		int qty = 0;
		int goodsId;

		if (marketGoods.ProductId != 0)
		{
			//---------------- is product ------------------//
			goodsId = marketGoods.ProductId;
			goodsId--;

			qty = Math.Min(GameConstants.MAX_CARAVAN_CARRY_QTY - ProductQty[goodsId], (int)loadQty);
			if (marketNationRecno != NationId) // calculate the qty again if this is not our own market
			{
				qty = (nation.cash > 0.0) ? Math.Min((int)(nation.cash / GameConstants.PRODUCT_PRICE), qty) : 0;

				if (qty != 0)
					nation.import_goods(NationBase.IMPORT_PRODUCT, marketNationRecno, qty * GameConstants.PRODUCT_PRICE);
			}

			ProductQty[goodsId] += qty;
			marketGoods.StockQty -= qty;
		}
		else if (marketGoods.RawId != 0)
		{
			//---------------- is raw ---------------------//
			goodsId = marketGoods.RawId;
			goodsId--;

			qty = Math.Min(GameConstants.MAX_CARAVAN_CARRY_QTY - RawQty[goodsId], (int)loadQty);
			if (marketNationRecno != NationId) // calculate the qty again if this is not our own market
			{
				qty = (nation.cash > 0.0) ? Math.Min((int)(nation.cash / GameConstants.RAW_PRICE), qty) : 0;

				if (qty != 0)
					nation.import_goods(NationBase.IMPORT_RAW, marketNationRecno, qty * GameConstants.RAW_PRICE);
			}

			RawQty[goodsId] += qty;
			marketGoods.StockQty -= qty;
		}

		if (qty > 0)
			LastLoadGoodsDate = Info.game_date;
	}

	public override void DrawDetails(IRenderer renderer)
	{
		renderer.DrawCaravanDetails(this);
	}

	public override void HandleDetailsInput(IRenderer renderer)
	{
		renderer.HandleCaravanDetailsInput(this);
	}
	
	#region Old AI Functions
	
	public override void ProcessAI()
	{
		//-- Think about removing stops whose owner nation is at war with us. --//

		if (Info.TotalDays % 30 == SpriteId % 30)
		{
			if (ThinkDelStop())
				return;

			//------ think about setting pickup goods type -----//

			ThinkSetPickUpType();
		}

		//------ Think about resigning this caravan -------//

		ThinkResign();
	}

	public int ThinkResign()
	{
		if (!IsVisible()) // can only resign when the caravan is not in a stop
			return 0;

		//---- resign this caravan if it has only one stop ----//

		if (StopDefinedNum < 2)
		{
			Resign(InternalConstants.COMMAND_AI);
			return 1;
		}

		//---- if the caravan hasn't loaded any goods for a year ----//

		// don't call too often as the action may fail and it takes a while to call the function each time
		if (Info.game_date > LastLoadGoodsDate.AddDays(365) && Info.TotalDays % 30 == SpriteId % 30)
		{
			//--- don't resign if this caravan carries any goods ---//

			for (int i = 0; i < GameConstants.MAX_RAW; i++)
			{
				if (RawQty[i] > 0 || ProductQty[i] > 0)
					return 0;
			}

			//------ resign now --------//

			Resign(InternalConstants.COMMAND_AI);
			return 1;
		}

		//--- if this caravan is travelling between two retail markets ---//
		//--- (neither of them has any direct supplies) ------//

		if (Info.TotalDays % 30 ==
		    SpriteId % 30) // don't call too often as the action may fail and it takes a while to call the function each time
		{
			for (int i = StopDefinedNum; i > 0; i--)
			{
				int firmRecno = Stops[i - 1].FirmId;

				if (FirmArray.IsDeleted(firmRecno) || FirmArray[firmRecno].FirmType != Firm.FIRM_MARKET)
				{
					DelStop(i, InternalConstants.COMMAND_AI);
					return 1;
				}

				//--- see if this market has any direct supply ---//

				FirmMarket firmMarket = (FirmMarket)FirmArray[firmRecno];

				for (int j = 0; j < GameConstants.MAX_MARKET_GOODS; j++)
				{
					MarketGoods marketGoods = firmMarket.MarketGoods[j];
					if (marketGoods.Supply30Days() > 0)
						return 0;
				}
			}

			//--- resign now if none of the linked markets have any direct supplies ---//

			Resign(InternalConstants.COMMAND_AI);
			return 1;
		}

		return 0;
	}

	public bool ThinkDelStop()
	{
		if (!IsVisible()) // cannot del stop if the caravan is inside a market place.
			return false;

		Firm firm;
		Nation nation = NationArray[NationId];

		int i;
		for (i = StopDefinedNum; i > 0; i--)
		{
			int firmRecno = Stops[i - 1].FirmId;

			if (FirmArray.IsDeleted(firmRecno))
			{
				DelStop(i, InternalConstants.COMMAND_AI);
				return true;
			}

			//---- AI only knows how to trade from a market to another ------//

			firm = FirmArray[firmRecno];

			// if the treaty trade has been terminated, delete the stop
			if (firm.FirmType != Firm.FIRM_MARKET || !nation.get_relation(firm.NationId).trade_treaty)
			{
				DelStop(i, InternalConstants.COMMAND_AI);
				return true;
			}

			//--- If this market is not linked to any towns ---//

			FirmMarket firmMarket = (FirmMarket)FirmArray[Stops[i - 1].FirmId];

			if (!firmMarket.IsMarketLinkedToTown())
			{
				//--- and the caravan is not currently picking up goods from the market ---//

				bool hasPickUp = false;
				TradeStop tradeStop = Stops[i - 1];

				int j;
				for (j = TradeStop.PICK_UP_RAW_FIRST; j <= TradeStop.PICK_UP_RAW_LAST; j++)
				{
					if (tradeStop.PickUpEnabled[j - 1])
						hasPickUp = true;
				}

				for (j = TradeStop.PICK_UP_PRODUCT_FIRST; j <= TradeStop.PICK_UP_PRODUCT_LAST; j++)
				{
					if (tradeStop.PickUpEnabled[j - 1])
						hasPickUp = true;
				}

				//---- then delete the stop -----//

				if (!hasPickUp)
				{
					DelStop(i, InternalConstants.COMMAND_AI);
					return true;
				}
			}

			//----------------------------------------------//

			int nationRecno = firmMarket.NationId;

			if (nation.get_relation_status(nationRecno) == NationBase.NATION_HOSTILE)
			{
				DelStop(i, InternalConstants.COMMAND_AI);
				return true;
			}
		}

		return false;
	}

	public void ThinkSetPickUpType()
	{
		if (!IsVisible()) // cannot change pickup type if the caravan is inside a market place.
			return;

		if (StopDefinedNum < 2)
			return;

		Firm firm1 = FirmArray[Stops[0].FirmId];
		Firm firm2 = FirmArray[Stops[1].FirmId];

		// only when both firms are markets
		if (firm1.FirmType != Firm.FIRM_MARKET || firm2.FirmType != Firm.FIRM_MARKET)
			return;

		// only when the market is our own, we can use it as a TO market
		if (firm2.NationId == NationId && ((FirmMarket)firm2).IsRetailMarket())
		{
			ThinkSetPickUpType2(1, 2);
		}

		if (firm1.NationId == NationId && ((FirmMarket)firm1).IsRetailMarket())
		{
			ThinkSetPickUpType2(2, 1);
		}
	}

	public void ThinkSetPickUpType2(int fromStopId, int toStopId)
	{
		FirmMarket fromMarket = (FirmMarket)FirmArray[Stops[fromStopId - 1].FirmId];
		FirmMarket toMarket = (FirmMarket)FirmArray[Stops[toStopId - 1].FirmId];

		//----- AI only knows about market to market trade -----//

		if (fromMarket.FirmType != Firm.FIRM_MARKET || toMarket.FirmType != Firm.FIRM_MARKET)
			return;

		//---- think about adding new pick up types -----//

		TradeStop tradeStop = Stops[fromStopId - 1];

		int i;
		for (i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = fromMarket.MarketGoods[i];
			if (marketGoods.ProductId == 0)
				continue;

			//----- only if this market has direct supplies -----//

			if (marketGoods.Supply30Days() == 0)
				continue;

			//-- when the from market has the product and the to market does not have the product, then trade this good --//

			int pickUpType = TradeStop.PICK_UP_PRODUCT_FIRST + marketGoods.ProductId - 1;

			//------ toggle it if the current flag and the flag we need are different ----//

			if (!tradeStop.PickUpEnabled[pickUpType - 1])
				SetStopPickUp(fromStopId, pickUpType, InternalConstants.COMMAND_AI);
		}

		//---- think about droping existing pick up types -----//

		for (i = TradeStop.PICK_UP_RAW_FIRST; i <= TradeStop.PICK_UP_RAW_LAST; i++)
		{
			if (!tradeStop.PickUpEnabled[i - 1])
				continue;

			//----- if there is no supply, drop the pick up type -----//

			MarketGoods marketGoods = fromMarket.GetRawGoods(i - TradeStop.PICK_UP_RAW_FIRST + 1);
			if (marketGoods == null || marketGoods.Supply30Days() == 0)
				SetStopPickUp(fromStopId, i, InternalConstants.COMMAND_AI);
		}

		for (i = TradeStop.PICK_UP_PRODUCT_FIRST; i <= TradeStop.PICK_UP_PRODUCT_LAST; i++)
		{
			if (!tradeStop.PickUpEnabled[i - 1])
				continue;

			//--- if the supply is not enough, drop the pick up type ---//
			
			MarketGoods marketGoods = fromMarket.GetProductGoods(i - TradeStop.PICK_UP_PRODUCT_FIRST + 1);
			if (marketGoods == null || marketGoods.Supply30Days() == 0)
				SetStopPickUp(fromStopId, i, InternalConstants.COMMAND_AI);
		}
	}
	
	#endregion
}