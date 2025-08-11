using System;

namespace TenKingdoms;

public class UnitCaravan : Unit
{
	public const int MAX_STOP_FOR_CARAVAN = 3;

	public int caravan_id; // id. of the caravan, for name display

	public int journey_status; // 1 for not unload but can up load, 2 for unload but not up load
	public int dest_stop_id; // destination stop id. the stop which the train currently is moving towards
	public int stop_defined_num; // num of stop defined
	public int wait_count; // set to -1 to indicate only one stop is specified

	public int stop_x_loc; // the x location the unit entering the stop
	public int stop_y_loc; // the y location the unit entering the stop

	// an array of firm_recno telling train stop stations
	public CaravanStop[] stop_array = new CaravanStop[MAX_STOP_FOR_CARAVAN];

	public DateTime last_set_stop_date; // the date when stops were last set.
	public DateTime last_load_goods_date; // the last date when the caravan load goods from a firm  

	//------ goods that the caravan carries -------//

	public int[] raw_qty_array = new int[GameConstants.MAX_RAW];
	public int[] product_raw_qty_array = new int[GameConstants.MAX_PRODUCT];

	private int[] processed_raw_qty_array = new int[GameConstants.MAX_RAW];
	private int[] processed_product_raw_qty_array = new int[GameConstants.MAX_PRODUCT];
	
	public UnitCaravan()
	{
		for (int i = 0; i < stop_array.Length; i++)
			stop_array[i] = new CaravanStop();

		journey_status = InternalConstants.ON_WAY_TO_FIRM;
		dest_stop_id = 1;
		stop_defined_num = 0;
		wait_count = 0;
		stop_x_loc = 0;
		stop_y_loc = 0;
		Loyalty = 100;
	}

	public override void Init(int unitType, int nationId, int rank, int unitLoyalty, int startLocX, int startLocY)
	{
		base.Init(unitType, nationId, rank, unitLoyalty, startLocX, startLocY);
		last_load_goods_date = Info.game_date;
	}

	public int carrying_qty(int pickUpType)
	{
		if (pickUpType >= TradeStop.PICK_UP_RAW_FIRST && pickUpType <= TradeStop.PICK_UP_RAW_LAST)
		{
			return raw_qty_array[pickUpType - TradeStop.PICK_UP_RAW_FIRST];
		}
		else if (pickUpType >= TradeStop.PICK_UP_PRODUCT_FIRST && pickUpType <= TradeStop.PICK_UP_PRODUCT_LAST)
		{
			return product_raw_qty_array[pickUpType - TradeStop.PICK_UP_PRODUCT_FIRST];
		}
		else
		{
			return 0;
		}
	}

	public void set_stop(int stopId, int stopXLoc, int stopYLoc, int remoteAction)
	{
		//-------------------------------------------------------//
		// check if there is a station in the given location
		//-------------------------------------------------------//
		Location loc = World.GetLoc(stopXLoc, stopYLoc);
		if (!loc.IsFirm())
			return;

		Firm firm = FirmArray[loc.FirmId()];

		if (!can_set_stop(firm.FirmId))
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

		if (stop_array[stopId - 1].firm_recno == 0)
		{
			stop_defined_num++; // no plus one if the recno is defined originally
		}

		//-------------------------------------------------------//
		// set the station recno of the stop
		//-------------------------------------------------------//
		CaravanStop stop = stop_array[stopId - 1];
		if (stop.firm_recno == firm.FirmId)
		{
			return; // same stop as before
		}

		//-------------- reset ignore_power_nation -------------//
		IgnorePowerNation = 0;

		int oldStopFirmRecno = dest_stop_id != 0 ? stop_array[dest_stop_id - 1].firm_recno : 0;
		int newStopFirmRecno;
		for (int i = 0; i < stop.pick_up_array.Length; i++)
			stop.pick_up_array[i] = false;
		stop.firm_recno = firm.FirmId;
		stop.firm_id = firm.FirmType;
		stop.firm_loc_x1 = firm.LocX1;
		stop.firm_loc_y1 = firm.LocY1;

		int goodsId = 0;
		//------------------------------------------------------------------------------------//
		// codes for setting pick_up_type
		//------------------------------------------------------------------------------------//
		switch (firm.FirmType)
		{
			case Firm.FIRM_MINE:
				goodsId = ((FirmMine)firm).raw_id;
				if (goodsId != 0)
					stop.pick_up_toggle(goodsId); // enable
				else
					stop.pick_up_set_none();
				break;

			case Firm.FIRM_FACTORY:
				goodsId = ((FirmFactory)firm).product_raw_id + GameConstants.MAX_RAW;
				if (goodsId != 0)
					stop.pick_up_toggle(goodsId); // enable
				else
					stop.pick_up_set_none();
				break;

			case Firm.FIRM_MARKET:
				int goodsNum = 0;
				for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
				{
					MarketGoods goods = ((FirmMarket)firm).market_goods_array[i];
					if (goods.raw_id != 0)
					{
						if (goodsNum == 0)
							goodsId = goods.raw_id;

						goodsNum++;
					}
					else if (goods.product_raw_id != 0)
					{
						if (goodsNum == 0)
							goodsId = goods.product_raw_id + GameConstants.MAX_RAW;

						goodsNum++;
					}
				}

				if (goodsNum == 1)
					stop.pick_up_toggle(goodsId); // cancel auto_pick_up
				else if (goodsNum == 0)
					stop.pick_up_set_none();
				else
					stop.pick_up_set_auto();
				break;

			default:
				break;
		}

		last_set_stop_date = Info.game_date;

		//-------------------------------------------------------//
		// remove duplicate stop or stop change nation
		//-------------------------------------------------------//
		update_stop_list();

		//-------------------------------------------------------//
		// handle if current stop changed when mobile
		//-------------------------------------------------------//
		if (dest_stop_id != 0 && journey_status != InternalConstants.INSIDE_FIRM)
		{
			if ((newStopFirmRecno = stop_array[dest_stop_id - 1].firm_recno) != oldStopFirmRecno)
			{
				firm = FirmArray[newStopFirmRecno];
				MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, stop_array[dest_stop_id - 1].firm_id);
				journey_status = InternalConstants.ON_WAY_TO_FIRM;
			}
		}
		else if (journey_status != InternalConstants.INSIDE_FIRM)
			Stop2();

		if (UnitArray.SelectedUnitId == SpriteId)
		{
			if (NationId == NationArray.player_recno || Config.show_ai_info)
				Info.disp();
		}
	}

	public void del_stop(int stopId, int remoteAction)
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

		stop_array[stopId - 1].firm_recno = 0;
		stop_defined_num--;

		update_stop_list();

		if (UnitArray.SelectedUnitId == SpriteId)
		{
			if ( /*!remote.is_enable() ||*/ NationId == NationArray.player_recno || Config.show_ai_info)
				Info.disp();
		}
	}

	public void update_stop_list()
	{
		//-------------------------------------------------------//
		// backup original destination stop firm recno
		//-------------------------------------------------------//
		int nextStopRecno = stop_array[dest_stop_id - 1].firm_recno;

		//----------------------------------------------------------------------//
		// check stop existence and the relationship between firm's nation
		//----------------------------------------------------------------------//
		CaravanStop stop;
		int i = 0;
		for (i = 0; i < MAX_STOP_FOR_CARAVAN; i++)
		{
			stop = stop_array[i];
			if (stop.firm_recno == 0)
				continue;

			if (FirmArray.IsDeleted(stop.firm_recno))
			{
				stop.firm_recno = 0; // clear the recno
				stop_defined_num--;
				continue;
			}

			Firm firm = FirmArray[stop.firm_recno];

			if (!can_set_stop(stop.firm_recno) || firm.LocX1 != stop.firm_loc_x1 || firm.LocY1 != stop.firm_loc_y1)
			{
				stop.firm_recno = 0;
				stop_defined_num--;
				continue;
			}
		}

		//-------------------------------------------------------//
		// remove duplicate node
		//-------------------------------------------------------//
		//CaravanStop insertNodePtr = stop_array[0];
		int insertNodeIndex = 0;

		if (stop_defined_num < 1)
		{
			for (i = 0; i < stop_array.Length; i++)
				stop_array[i] = new CaravanStop();
			dest_stop_id = 0;
			return; // no stop
		}

		//-------------------------------------------------------//
		// pack the firm_recno to the beginning part of the array
		//-------------------------------------------------------//
		int compareRecno = 0;
		int nodeIndex = 0;
		stop = stop_array[nodeIndex];
		for (i = 0; i < MAX_STOP_FOR_CARAVAN; i++, nodeIndex++)
		{
			stop = stop_array[nodeIndex];
			if (stop.firm_recno != 0)
			{
				compareRecno = stop.firm_recno;
				break;
			}
		}

		if (i > 0) // else, the first record is already in the beginning of the array
		{
			stop_array[insertNodeIndex] = stop_array[nodeIndex];
		}

		i++;

		if (stop_defined_num == 1)
		{
			for (i = 1; i < stop_array.Length; i++)
				stop_array[i] = new CaravanStop();
			dest_stop_id = 1;
			return;
		}

		int unprocessed = stop_defined_num - 1;
		insertNodeIndex++;
		nodeIndex++;

		for (; i < MAX_STOP_FOR_CARAVAN && unprocessed > 0; i++, nodeIndex++)
		{
			stop = stop_array[nodeIndex];
			if (stop.firm_recno == 0)
				continue; // empty

			if (stop.firm_recno == compareRecno)
			{
				stop.firm_recno = 0;
				stop_defined_num--;
			}
			else
			{
				compareRecno = stop.firm_recno;

				if (insertNodeIndex != nodeIndex)
					stop_array[insertNodeIndex] = stop_array[nodeIndex];

				insertNodeIndex++;
			}

			unprocessed--;
		}

		if (stop_defined_num > 2)
		{
			//-------- compare the first and the end record -------//
			nodeIndex = stop_defined_num - 1;
			stop = stop_array[nodeIndex]; // point to the end
			if (stop.firm_recno == stop_array[0].firm_recno)
			{
				stop.firm_recno = 0; // remove the end record
				stop_defined_num--;
			}
		}

		if (stop_defined_num < MAX_STOP_FOR_CARAVAN)
		{
			for (i = stop_defined_num; i < stop_array.Length; i++)
				stop_array[i] = new CaravanStop();
		}

		//-----------------------------------------------------------------------------------------//
		// There should be at least one stop in the list.  Otherwise, clear all the stops
		//-----------------------------------------------------------------------------------------//
		bool ourFirmExist = false;
		for (i = 0; i < stop_defined_num; i++)
		{
			stop = stop_array[i];
			Firm firm = FirmArray[stop.firm_recno];
			if (firm.NationId == NationId)
			{
				ourFirmExist = true;
				break;
			}
		}

		if (!ourFirmExist) // none of the markets belong to our nation
		{
			for (i = 0; i < stop_array.Length; i++)
				stop_array[i] = new CaravanStop();
			if (journey_status != InternalConstants.INSIDE_FIRM)
				journey_status = InternalConstants.ON_WAY_TO_FIRM;
			dest_stop_id = 0;
			stop_defined_num = 0;
			return;
		}

		//-----------------------------------------------------------------------------------------//
		// reset dest_stop_id since the order of the stop may be changed
		//-----------------------------------------------------------------------------------------//
		int xLoc = NextLocX;
		int yLoc = NextLocY;
		int minDist = Int32.MaxValue;

		for (i = 0, dest_stop_id = 0; i < stop_defined_num; i++)
		{
			stop = stop_array[i];
			if (stop.firm_recno == nextStopRecno)
			{
				dest_stop_id = i + 1;
				break;
			}
			else
			{
				Firm firm = FirmArray[stop.firm_recno];
				int dist = Misc.points_distance(xLoc, yLoc, firm.LocCenterX, firm.LocCenterY);

				if (dist < minDist)
				{
					//TODO bug
					dist = minDist;
					dest_stop_id = i + 1;
				}
			}
		}
	}

	public void set_stop_pick_up(int stopId, int newPickUpType, int remoteAction)
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
				stop_array[stopId - 1].pick_up_set_auto();
				break;

			case TradeStop.NO_PICK_UP:
				stop_array[stopId - 1].pick_up_set_none();
				break;

			default:
				stop_array[stopId - 1].pick_up_toggle(newPickUpType);
				break;
		}

		if (UnitArray.SelectedUnitId == SpriteId)
		{
			//TODO
			//if (nation_recno == NationArray.player_recno || Config.show_ai_info)
				//disp_stop(INFO_Y1 + 54, INFO_UPDATE);
		}
	}

	public bool can_set_stop(int firmRecno)
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

	public bool has_pick_up_type(int stopId, int pickUpType)
	{
		return stop_array[stopId - 1].pick_up_array[pickUpType - 1];
	}

	public new void caravan_in_firm()
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
		if (wait_count > 0)
		{
			wait_count--;
			return;
		}

		//-----------------------------------------------------------------------------//
		// leave the market and go to another market if possible
		//-----------------------------------------------------------------------------//
		CaravanStop caravanStop = stop_array[dest_stop_id - 1];
		int xLoc = stop_x_loc;
		int yLoc = stop_y_loc;
		Location loc = World.GetLoc(xLoc, yLoc);
		Firm firm;

		if (loc.CanMove(MobileType))
			InitSprite(xLoc, yLoc); // appear in the location the unit disappeared before
		else
		{
			//---- the entering location is blocked, select another location to leave ----//
			firm = FirmArray[ActionParam];

			if (appear_in_firm_surround(ref xLoc, ref yLoc, firm))
			{
				InitSprite(xLoc, yLoc);
				Stop();
			}
			else
			{
				wait_count = GameConstants.MAX_CARAVAN_WAIT_TERM * 10; //********* BUGHERE, continue to wait or ....
				return;
			}
		}

		//-------------- get next stop id. ----------------//
		int nextStopId = get_next_stop_id(dest_stop_id);
		if (nextStopId == 0 || dest_stop_id == nextStopId)
		{
			dest_stop_id = nextStopId;
			journey_status = (nextStopId == 0) ? InternalConstants.NO_STOP_DEFINED : InternalConstants.SURROUND_FIRM;
			return; // no stop or only one stop is valid
		}

		dest_stop_id = nextStopId;
		firm = FirmArray[stop_array[dest_stop_id - 1].firm_recno];

		ActionParam = 0; // since action_para is used to store the current market recno, reset before searching
		MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, firm.FirmType);

		journey_status = InternalConstants.ON_WAY_TO_FIRM;
	}

	public void caravan_on_way()
	{
		CaravanStop stop = stop_array[dest_stop_id - 1];
		Firm firm = null;
		int nextXLoc = -1;
		int nextYLoc = -1;

		if (CurAction == SPRITE_IDLE && journey_status != InternalConstants.SURROUND_FIRM)
		{
			if (!FirmArray.IsDeleted(stop.firm_recno))
			{
				firm = FirmArray[stop.firm_recno];
				MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, firm.FirmType);
				nextXLoc = NextLocX;
				nextYLoc = NextLocY;

				// hard code 1 for caravan size 1x1
				if (nextXLoc >= firm.LocX1 - 1 && nextXLoc <= firm.LocX2 + 1 && nextYLoc >= firm.LocY1 - 1 &&
				    nextYLoc <= firm.LocY2 + 1)
					journey_status = InternalConstants.SURROUND_FIRM;

				if (nextXLoc == MoveToLocX && nextYLoc == MoveToLocY && IgnorePowerNation == 0)
					IgnorePowerNation = 1;

				return;
			}
		}

		int unitRecno = SpriteId;

		if (UnitArray.IsDeleted(unitRecno))
			return; //-***************** BUGHERE ***************//

		if (FirmArray.IsDeleted(stop.firm_recno))
		{
			update_stop_list();

			if (stop_defined_num != 0) // move to next stop
			{
				firm = FirmArray[stop_array[stop_defined_num - 1].firm_recno];
				MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, firm.FirmType);
			}

			return;
		}

		//CaravanStop *stop = stop_array + dest_stop_id - 1;
		firm = FirmArray[stop.firm_recno];

		nextXLoc = NextLocX;
		nextYLoc = NextLocY;

		if (journey_status == InternalConstants.SURROUND_FIRM ||
		    (nextXLoc == MoveToLocX && nextYLoc == MoveToLocY && CurX == NextX && CurY == NextY && // move in a tile exactly
		     (nextXLoc >= firm.LocX1 - 1 && nextXLoc <= firm.LocX2 + 1 &&
		      nextYLoc >= firm.LocY1 - 1 && nextYLoc <= firm.LocY2 + 1))) // in the surrounding of the firm
		{
			//-------------------- update pick_up_array --------------------//
			stop.update_pick_up();

			//-------------------------------------------------------//
			// load/unload goods
			//-------------------------------------------------------//
			if (NationArray[NationId].get_relation(firm.NationId).trade_treaty)
			{
				switch (firm.FirmType)
				{
					case Firm.FIRM_MINE:
						mine_load_goods(stop.pick_up_type);
						break;

					case Firm.FIRM_FACTORY:
						factory_unload_goods();
						factory_load_goods(stop.pick_up_type);
						break;

					case Firm.FIRM_MARKET:
						market_unload_goods();

						if (stop.pick_up_type == TradeStop.AUTO_PICK_UP)
							market_auto_load_goods();
						else if (stop.pick_up_type != TradeStop.NO_PICK_UP)
							market_load_goods();
						break;
				}
			}

			//-------------------------------------------------------//
			// action_para is used to store the firm_recno of the market
			// where the caravan move in.
			//-------------------------------------------------------//
			ActionParam = stop.firm_recno;

			stop_x_loc = MoveToLocX; // store entering location
			stop_y_loc = MoveToLocY;
			wait_count = GameConstants.MAX_CARAVAN_WAIT_TERM; // set waiting term

			ResetPath();
			DeinitSprite(true); // the caravan enters the market now. 1-keep it selected if it is currently selected

			CurX--; // set cur_x to -2, such that invisible but still process pre_process()

			journey_status = InternalConstants.INSIDE_FIRM;
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
				journey_status = InternalConstants.ON_WAY_TO_FIRM;
			}
		}
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
		if (journey_status == InternalConstants.INSIDE_FIRM)
		{
			caravan_in_firm();
			return;
		}

		//-----------------------------------------------------------------------------//
		// stop action if no stop is defined
		//-----------------------------------------------------------------------------//
		if (stop_defined_num == 0)
		{
			if (journey_status != InternalConstants.NO_STOP_DEFINED)
				Stop(); // stop if no valid stop is defined

			journey_status = InternalConstants.NO_STOP_DEFINED;
			return;
		}

		//-----------------------------------------------------------------------------//
		// wait in the surrounding of the stop if stop_defined_num==1 (only one stop)
		//-----------------------------------------------------------------------------//
		if (stop_defined_num == 1)
		{
			CaravanStop stop = stop_array[0];

			if (FirmArray.IsDeleted(stop.firm_recno))
			{
				update_stop_list();
				return;
			}

			Firm firm = FirmArray[stop.firm_recno];
			int firmXLoc1 = firm.LocX1;
			int firmYLoc1 = firm.LocY1;
			int firmXLoc2 = firm.LocX2;
			int firmYLoc2 = firm.LocY2;
			int firmId = firm.FirmType;
			if (firmXLoc1 != stop.firm_loc_x1 || firmYLoc1 != stop.firm_loc_y1 ||
			    (firmId != Firm.FIRM_MINE && firmId != Firm.FIRM_FACTORY && firmId != Firm.FIRM_MARKET))
			{
				update_stop_list();
				return;
			}

			int curXLoc = NextLocX;
			int curYLoc = NextLocY;

			if (curXLoc < firmXLoc1 - 1 || curXLoc > firmXLoc2 + 1 || curYLoc < firmYLoc1 - 1 || curYLoc > firmYLoc2 + 1)
			{
				if (CurAction == SPRITE_IDLE)
					MoveToFirmSurround(firmXLoc1, firmYLoc1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, firmId);
				else
					journey_status = InternalConstants.ON_WAY_TO_FIRM;
			}
			else
			{
				journey_status = InternalConstants.SURROUND_FIRM;
				//if(firm->nation_recno==nation_recno)
				if (NationArray[NationId].get_relation(firm.NationId).trade_treaty)
				{
					if (wait_count <= 0)
					{
						//---------- unloading goods -------------//
						switch (stop.firm_id)
						{
							case Firm.FIRM_MINE:
								break; // no goods unload to mine

							case Firm.FIRM_FACTORY:
								factory_unload_goods();
								break;

							case Firm.FIRM_MARKET:
								market_unload_goods();
								break;
						}

						wait_count = GameConstants.MAX_CARAVAN_WAIT_TERM * InternalConstants.SURROUND_FIRM_WAIT_FACTOR;
					}
					else
						wait_count--;
				}
			}

			return;
		}

		//-----------------------------------------------------------------------------//
		// at least 2 stops for the caravan to move between
		//-----------------------------------------------------------------------------//

		caravan_on_way();
	}

	public void copy_route(int copyUnitRecno, int remoteAction)
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
		int num_stops = stop_defined_num;
		for (int i = 0; i < num_stops; i++)
			del_stop(1, InternalConstants.COMMAND_AUTO); // stop ids shift up

		for (int i = 0; i < MAX_STOP_FOR_CARAVAN; i++)
		{
			CaravanStop caravanStopA = copyUnit.stop_array[i];
			CaravanStop caravanStopB = stop_array[i];
			if (caravanStopA.firm_recno == 0)
				break;

			if (FirmArray.IsDeleted(caravanStopA.firm_recno))
				continue;

			Firm firm = FirmArray[caravanStopA.firm_recno];
			set_stop(i + 1, caravanStopA.firm_loc_x1, caravanStopA.firm_loc_y1, InternalConstants.COMMAND_AUTO);

			if (caravanStopA.pick_up_type == TradeStop.AUTO_PICK_UP)
			{
				set_stop_pick_up(i + 1, TradeStop.AUTO_PICK_UP, InternalConstants.COMMAND_AUTO);
			}

			else if (caravanStopA.pick_up_type == TradeStop.NO_PICK_UP)
			{
				set_stop_pick_up(i + 1, TradeStop.NO_PICK_UP, InternalConstants.COMMAND_AUTO);
			}

			else
			{
				for (int b = 0; b < TradeStop.MAX_PICK_UP_GOODS; ++b)
				{
					if (caravanStopA.pick_up_array[b] != caravanStopB.pick_up_array[b])
						set_stop_pick_up(i + 1, b + 1, InternalConstants.COMMAND_PLAYER);
				}
			}
		}
	}

	//------- ai functions --------//

	public override void ProcessAI()
	{
		//-- Think about removing stops whose owner nation is at war with us. --//

		if (Info.TotalDays % 30 == SpriteId % 30)
		{
			if (think_del_stop())
				return;

			//------ think about setting pickup goods type -----//

			think_set_pick_up_type();
		}

		//------ Think about resigning this caravan -------//

		think_resign();
	}

	public int think_resign()
	{
		if (!IsVisible()) // can only resign when the caravan is not in a stop
			return 0;

		//---- resign this caravan if it has only one stop ----//

		if (stop_defined_num < 2)
		{
			Resign(InternalConstants.COMMAND_AI);
			return 1;
		}

		//---- if the caravan hasn't loaded any goods for a year ----//

		// don't call too often as the action may fail and it takes a while to call the function each time
		if (Info.game_date > last_load_goods_date.AddDays(365) && Info.TotalDays % 30 == SpriteId % 30)
		{
			//--- don't resign if this caravan carries any goods ---//

			for (int i = 0; i < GameConstants.MAX_RAW; i++)
			{
				if (raw_qty_array[i] > 0 || product_raw_qty_array[i] > 0)
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
			for (int i = stop_defined_num; i > 0; i--)
			{
				int firmRecno = stop_array[i - 1].firm_recno;

				if (FirmArray.IsDeleted(firmRecno) || FirmArray[firmRecno].FirmType != Firm.FIRM_MARKET)
				{
					del_stop(i, InternalConstants.COMMAND_AI);
					return 1;
				}

				//--- see if this market has any direct supply ---//

				FirmMarket firmMarket = (FirmMarket)FirmArray[firmRecno];

				for (int j = 0; j < GameConstants.MAX_MARKET_GOODS; j++)
				{
					MarketGoods marketGoods = firmMarket.market_goods_array[j];
					if (marketGoods.supply_30days() > 0)
						return 0;
				}
			}

			//--- resign now if none of the linked markets have any direct supplies ---//

			Resign(InternalConstants.COMMAND_AI);
			return 1;
		}

		return 0;
	}

	public bool think_del_stop()
	{
		if (!IsVisible()) // cannot del stop if the caravan is inside a market place.
			return false;

		Firm firm;
		Nation nation = NationArray[NationId];

		int i;
		for (i = stop_defined_num; i > 0; i--)
		{
			int firmRecno = stop_array[i - 1].firm_recno;

			if (FirmArray.IsDeleted(firmRecno))
			{
				del_stop(i, InternalConstants.COMMAND_AI);
				return true;
			}

			//---- AI only knows how to trade from a market to another ------//

			firm = FirmArray[firmRecno];

			// if the treaty trade has been terminated, delete the stop
			if (firm.FirmType != Firm.FIRM_MARKET || !nation.get_relation(firm.NationId).trade_treaty)
			{
				del_stop(i, InternalConstants.COMMAND_AI);
				return true;
			}

			//--- If this market is not linked to any towns ---//

			FirmMarket firmMarket = (FirmMarket)FirmArray[stop_array[i - 1].firm_recno];

			if (!firmMarket.is_market_linked_to_town())
			{
				//--- and the caravan is not currently picking up goods from the market ---//

				bool hasPickUp = false;
				TradeStop tradeStop = stop_array[i - 1];

				int j;
				for (j = TradeStop.PICK_UP_RAW_FIRST; j <= TradeStop.PICK_UP_RAW_LAST; j++)
				{
					if (tradeStop.pick_up_array[j - 1])
						hasPickUp = true;
				}

				for (j = TradeStop.PICK_UP_PRODUCT_FIRST; j <= TradeStop.PICK_UP_PRODUCT_LAST; j++)
				{
					if (tradeStop.pick_up_array[j - 1])
						hasPickUp = true;
				}

				//---- then delete the stop -----//

				if (!hasPickUp)
				{
					del_stop(i, InternalConstants.COMMAND_AI);
					return true;
				}
			}

			//----------------------------------------------//

			int nationRecno = firmMarket.NationId;

			if (nation.get_relation_status(nationRecno) == NationBase.NATION_HOSTILE)
			{
				del_stop(i, InternalConstants.COMMAND_AI);
				return true;
			}
		}

		return false;
	}

	public void think_set_pick_up_type()
	{
		if (!IsVisible()) // cannot change pickup type if the caravan is inside a market place.
			return;

		if (stop_defined_num < 2)
			return;

		Firm firm1 = FirmArray[stop_array[0].firm_recno];
		Firm firm2 = FirmArray[stop_array[1].firm_recno];

		// only when both firms are markets
		if (firm1.FirmType != Firm.FIRM_MARKET || firm2.FirmType != Firm.FIRM_MARKET)
			return;

		// only when the market is our own, we can use it as a TO market
		if (firm2.NationId == NationId && ((FirmMarket)firm2).is_retail_market())
		{
			think_set_pick_up_type2(1, 2);
		}

		if (firm1.NationId == NationId && ((FirmMarket)firm1).is_retail_market())
		{
			think_set_pick_up_type2(2, 1);
		}
	}

	public void think_set_pick_up_type2(int fromStopId, int toStopId)
	{
		FirmMarket fromMarket = (FirmMarket)FirmArray[stop_array[fromStopId - 1].firm_recno];
		FirmMarket toMarket = (FirmMarket)FirmArray[stop_array[toStopId - 1].firm_recno];

		//----- AI only knows about market to market trade -----//

		if (fromMarket.FirmType != Firm.FIRM_MARKET || toMarket.FirmType != Firm.FIRM_MARKET)
			return;

		//---- think about adding new pick up types -----//

		TradeStop tradeStop = stop_array[fromStopId - 1];

		int i;
		for (i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = fromMarket.market_goods_array[i];
			if (marketGoods.product_raw_id == 0)
				continue;

			//----- only if this market has direct supplies -----//

			if (marketGoods.supply_30days() == 0)
				continue;

			//-- when the from market has the product and the to market does not have the product, then trade this good --//

			int pickUpType = TradeStop.PICK_UP_PRODUCT_FIRST + marketGoods.product_raw_id - 1;

			//------ toggle it if the current flag and the flag we need are different ----//

			if (!tradeStop.pick_up_array[pickUpType - 1])
				set_stop_pick_up(fromStopId, pickUpType, InternalConstants.COMMAND_AI);
		}

		//---- think about droping existing pick up types -----//

		for (i = TradeStop.PICK_UP_RAW_FIRST; i <= TradeStop.PICK_UP_RAW_LAST; i++)
		{
			if (!tradeStop.pick_up_array[i - 1])
				continue;

			MarketGoods marketGoods = fromMarket.market_raw_array[i - TradeStop.PICK_UP_RAW_FIRST];

			//----- if there is no supply, drop the pick up type -----//

			if (marketGoods == null || marketGoods.supply_30days() == 0)
				set_stop_pick_up(fromStopId, i, InternalConstants.COMMAND_AI);
		}

		for (i = TradeStop.PICK_UP_PRODUCT_FIRST; i <= TradeStop.PICK_UP_PRODUCT_LAST; i++)
		{
			if (!tradeStop.pick_up_array[i - 1])
				continue;

			MarketGoods marketGoods = fromMarket.market_product_array[i - TradeStop.PICK_UP_PRODUCT_FIRST];

			//--- if the supply is not enough, drop the pick up type ---//

			if (marketGoods == null || marketGoods.supply_30days() == 0)
				set_stop_pick_up(fromStopId, i, InternalConstants.COMMAND_AI);
		}
	}

	private int get_next_stop_id(int curStopId = MAX_STOP_FOR_CARAVAN)
	{
		int nextStopId = (curStopId >= stop_defined_num) ? 1 : curStopId + 1;

		CaravanStop stop = stop_array[nextStopId - 1];

		bool needUpdate = false;

		if (FirmArray.IsDeleted(stop.firm_recno))
		{
			needUpdate = true;
		}
		else
		{
			Firm firm = FirmArray[stop.firm_recno];

			if (!can_set_stop(stop.firm_recno) ||
			    firm.LocX1 != stop.firm_loc_x1 || firm.LocY1 != stop.firm_loc_y1)
			{
				needUpdate = true;
			}
		}

		if (needUpdate)
		{
			int preStopRecno = stop_array[curStopId - 1].firm_recno;

			update_stop_list();

			if (stop_defined_num == 0)
				return 0; // no stop is valid

			for (int i = 1; i <= stop_defined_num; i++)
			{
				stop = stop_array[i - 1];
				if (stop.firm_recno == preStopRecno)
					return (i >= stop_defined_num) ? 1 : i + 1;
			}

			return 1;
		}
		else
		{
			return nextStopId;
		}
	}

	//-------- for mine ----------//
	private void mine_load_goods(int pickUpType)
	{
		if (pickUpType == TradeStop.NO_PICK_UP)
			return; // return if not allowed to load any goods

		CaravanStop stop = stop_array[dest_stop_id - 1];
		FirmMine curMine = (FirmMine)FirmArray[stop.firm_recno];

		if (curMine.NationId != NationId)
			return; // no action if this is not our own mine

		//------------- load goods -----------//
		int searchRawId = pickUpType - TradeStop.PICK_UP_RAW_FIRST + 1;
		if (pickUpType == TradeStop.AUTO_PICK_UP || curMine.raw_id == searchRawId) // auto_pick_up or is the raw to pick up
		{
			int goodsId = curMine.raw_id - 1;
			int maxLoadQty = (pickUpType != TradeStop.AUTO_PICK_UP)
				? (int)curMine.stock_qty
				: Math.Max(0, (int)curMine.stock_qty - GameConstants.MIN_FIRM_STOCK_QTY); // MAX Qty mine can supply
			int qty = Math.Min(GameConstants.MAX_CARAVAN_CARRY_QTY - raw_qty_array[goodsId], maxLoadQty); // MAX Qty caravan can carry

			raw_qty_array[goodsId] += qty;
			curMine.stock_qty -= qty;

			if (maxLoadQty > 0)
				last_load_goods_date = Info.game_date;
		}
	}

	//-------- for factory ---------//
	private void factory_unload_goods()
	{
		CaravanStop stop = stop_array[dest_stop_id - 1];
		FirmFactory curFactory = (FirmFactory)FirmArray[stop.firm_recno];

		if (curFactory.NationId != NationId)
			return; // don't unload goods if this isn't our own factory

		//--- if the factory does not have any stock and there is no production, set it to type of raw materials the caravan is carring ---//

		if (curFactory.stock_qty == 0 && curFactory.raw_stock_qty == 0 && curFactory.production_30days() == 0)
		{
			int rawCount = 0;
			int rawId = 0;

			for (int i = 0; i < GameConstants.MAX_RAW; i++)
			{
				if (raw_qty_array[i] > 0)
				{
					rawCount++;
					rawId = i + 1;
				}
			}

			//-- only if the caravan only carries one type of raw material --//

			if (rawCount == 1 && rawId != 0)
				curFactory.product_raw_id = rawId;
		}

		//---------- unload materials automatically --------//
		int goodsId = curFactory.product_raw_id - 1;

		if (raw_qty_array[goodsId] != 0) // caravan has this raw materials
		{
			int qty = Math.Min(raw_qty_array[goodsId], (int)(curFactory.max_raw_stock_qty - curFactory.raw_stock_qty));
			raw_qty_array[goodsId] -= qty;
			curFactory.raw_stock_qty += qty;
		}
	}

	private void factory_load_goods(int pickUpType)
	{
		if (pickUpType == TradeStop.NO_PICK_UP)
			return; // return not allowed to load any goods

		CaravanStop stop = stop_array[dest_stop_id - 1];
		FirmFactory curFactory = (FirmFactory)FirmArray[stop.firm_recno];

		if (curFactory.NationId != NationId)
			return; // don't load goods if this isn't our own factory

		//------------- load goods -----------//
		int searchProductRawId = pickUpType - TradeStop.PICK_UP_PRODUCT_FIRST + 1;
		if (pickUpType == TradeStop.AUTO_PICK_UP || curFactory.product_raw_id == searchProductRawId) // auto_pick_up or is the product to pick up
		{
			int goodsId = curFactory.product_raw_id - 1;
			int maxLoadQty = (pickUpType != TradeStop.AUTO_PICK_UP)
				? (int)curFactory.stock_qty
				: Math.Max(0, (int)curFactory.stock_qty - GameConstants.MIN_FIRM_STOCK_QTY); // MAX Qty factory can supply
			int qty = Math.Min(GameConstants.MAX_CARAVAN_CARRY_QTY - product_raw_qty_array[goodsId], maxLoadQty); // MAX Qty caravan can carry

			product_raw_qty_array[goodsId] += qty;
			curFactory.stock_qty -= qty;

			if (maxLoadQty > 0)
				last_load_goods_date = Info.game_date;
		}
	}

	//-------- for market ---------//
	private void market_unload_goods()
	{
		FirmMarket curMarket = (FirmMarket)FirmArray[stop_array[dest_stop_id - 1].firm_recno];

		for (int i = 0; i < processed_raw_qty_array.Length; i++)
			processed_raw_qty_array[i] = 0;

		for (int i = 0; i < processed_product_raw_qty_array.Length; i++)
			processed_product_raw_qty_array[i] = 0;

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
			MarketGoods marketGoods = curMarket.market_goods_array[i];
			int unloadQty;
			int goodsId;
			if (marketGoods.raw_id != 0)
			{
				//-------------- is raw material ----------------//
				goodsId = marketGoods.raw_id - 1;

				//if( (marketGoods->supply_30days()==0 && marketGoods->stock_qty<curMarket->max_stock_qty) || // no supply and stock isn't full
				//	 (marketGoods->stock_qty<CARAVAN_UNLOAD_TO_MARKET_QTY &&
				//	 //##### begin trevor 16/7 #######//
				//	  marketGoods->month_demand > marketGoods->supply_30days()) ) // demand > supply
				//	 //##### end trevor 16/7 #######//
				if (marketGoods.stock_qty < curMarket.max_stock_qty)
				{
					//-------- demand > supply and stock is not full ----------//
					if (raw_qty_array[goodsId] != 0) // have this goods
					{
						//---------- process unload -------------//
						unloadQty = Math.Min(raw_qty_array[goodsId], (int)(curMarket.max_stock_qty - marketGoods.stock_qty));
						raw_qty_array[goodsId] -= unloadQty;
						marketGoods.stock_qty += unloadQty;
						processed_raw_qty_array[goodsId] += 2;
					}
					else if (marketGoods.stock_qty <= 0.0 && marketGoods.supply_30days() <= 0.0)
					{
						//---------- no supply, no stock, without this goods ------------//
						withEmptySlot++;
						//processed_raw_qty_array[goodsId] = 0; // reset to zero for handling empty slot
					}
				}
				else if (raw_qty_array[goodsId] != 0) // have this goods
				{
					processed_raw_qty_array[goodsId]++;
				}
			}
			else if (marketGoods.product_raw_id != 0)
			{
				//---------------- is product -------------------//
				goodsId = marketGoods.product_raw_id - 1;

				//if( (marketGoods->supply_30days()==0 && marketGoods->stock_qty<curMarket->max_stock_qty) || // no supply and stock isn't full
				//	 //##### begin trevor 16/7 #######//
				//	 (marketGoods->stock_qty<50 && marketGoods->month_demand > marketGoods->supply_30days()) ) // demand > supply
				//	 //##### end trevor 16/7 #######//
				if (marketGoods.stock_qty < curMarket.max_stock_qty)
				{
					if (product_raw_qty_array[goodsId] != 0) // have this goods
					{
						unloadQty = Math.Min(product_raw_qty_array[goodsId], (int)(curMarket.max_stock_qty - marketGoods.stock_qty));
						product_raw_qty_array[goodsId] -= unloadQty;
						marketGoods.stock_qty += unloadQty;
						processed_product_raw_qty_array[goodsId] += 2;
					}
					else if (marketGoods.stock_qty <= 0.0 && marketGoods.supply_30days() <= 0.0) // no supply, no stock, without this goods
					{
						withEmptySlot++;
						//processed_product_raw_qty_array[goodsId] = 0; // reset to zero for handling empty slot
					}
				}
				else if (product_raw_qty_array[goodsId] != 0) // have this goods
				{
					processed_product_raw_qty_array[goodsId]++;
				}
			}
			else // is empty
			{
				if (!market_unload_goods_in_empty_slot(curMarket, i))
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
				MarketGoods marketGoods = curMarket.market_goods_array[i];
				if (marketGoods.stock_qty > 0.0 || marketGoods.supply_30days() > 0.0)
					continue;

				market_unload_goods_in_empty_slot(curMarket, i);
				withEmptySlot--;
			}
		}
	}

	private bool market_unload_goods_in_empty_slot(FirmMarket curMarket, int position)
	{
		bool moreToUnload = false;
		MarketGoods marketGoods = curMarket.market_goods_array[position];

		//-------------------------------------------------//
		// unload product and then raw
		//-------------------------------------------------//
		int processed, j;
		for (processed = 0, j = 0; j < GameConstants.MAX_PRODUCT; j++)
		{
			if (processed_product_raw_qty_array[j] != 0 || product_raw_qty_array[j] == 0)
				continue; // this product is processed or no stock in the caravan

			// this can be unloaded, but check if it can be
			// unloaded into an already provided market slot
			// for this product type

			bool productExistInOtherSlot = false;
			for (int k = 0; k < GameConstants.MAX_MARKET_GOODS; k++)
			{
				MarketGoods checkGoods = curMarket.market_goods_array[k];
				if (checkGoods.product_raw_id == j + 1)
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
			marketGoods.stock_qty = 0.0; // BUGHERE, there is a case that marketGoods->stock_qty > 0
			//-**************************************************-//
			processed_product_raw_qty_array[j] += 2;
			curMarket.set_goods(false, j + 1, position);

			int unloadQty = Math.Min(product_raw_qty_array[j], (int)(curMarket.max_stock_qty - marketGoods.stock_qty));
			product_raw_qty_array[j] -= unloadQty;
			marketGoods.stock_qty += unloadQty;
			processed++;
			break;
		}

		if (processed == 0)
		{
			for (j = 0; j < GameConstants.MAX_PRODUCT; j++)
			{
				if (processed_raw_qty_array[j] != 0 || raw_qty_array[j] == 0)
					continue; // this product is processed or no stock in the caravan

				// this can be unloaded, but check if it can be
				// unloaded into an already provided market slot
				// for this product type

				bool rawExistInOtherSlot = false;
				for (int k = 0; k < GameConstants.MAX_MARKET_GOODS; k++)
				{
					MarketGoods checkGoods = curMarket.market_goods_array[k];
					if (checkGoods.raw_id == j + 1)
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
				marketGoods.stock_qty = 0.0; // BUGHERE, there is a case that marketGoods->stock_qty > 0
				//-**************************************************-//
				processed_raw_qty_array[j] += 2;
				curMarket.set_goods(true, j + 1, position);

				int unloadQty = Math.Min(raw_qty_array[j], (int)(curMarket.max_stock_qty - marketGoods.stock_qty));
				raw_qty_array[j] -= unloadQty;
				marketGoods.stock_qty += unloadQty;
				processed++;
				break;
			}
		}

		if (UnitArray.SelectedUnitId == SpriteId)
			Info.disp();

		return processed > 0 || moreToUnload;
	}

	private void market_load_goods()
	{
		CaravanStop stop = stop_array[dest_stop_id - 1];

		FirmMarket curMarket = (FirmMarket)FirmArray[stop.firm_recno];

		//------------------------------------------------------------//
		// scan the market, see if it has the specified pickup goods
		//------------------------------------------------------------//
		for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = curMarket.market_goods_array[i];
			if (marketGoods.raw_id != 0)
			{
				if (stop.pick_up_array[marketGoods.raw_id - 1])
					market_load_goods_now(marketGoods, marketGoods.stock_qty);
			}
			else if (marketGoods.product_raw_id != 0)
			{
				if (stop.pick_up_array[marketGoods.product_raw_id - 1 + GameConstants.MAX_RAW])
					market_load_goods_now(marketGoods, marketGoods.stock_qty);
			}
		}
	}

	private void market_auto_load_goods()
	{
		FirmMarket curMarket = (FirmMarket)FirmArray[stop_array[dest_stop_id - 1].firm_recno];

		//int	isOurMarket = (curMarket->nation_recno==nation_recno); // is 1 or 0

		//----------------------------------------------------------------------//
		// keep empty stock if the market(AI) is for sale, otherwise use the
		// default value
		//----------------------------------------------------------------------//
		//short minFirmStockQty = (int)curMarket->max_stock_qty/5; // keep at least 20% capacity in the firm if the market is not for sale

		for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = curMarket.market_goods_array[i];
			if (marketGoods.stock_qty <= 0.0)
				continue;

			int goodsId;
			int loadQty;
			if (marketGoods.raw_id != 0)
			{
				goodsId = marketGoods.raw_id;
				goodsId--;
				if (processed_raw_qty_array[goodsId] == 2)
					continue; // continue if it is the goods unloaded

				if (marketGoods.stock_qty > GameConstants.MIN_FIRM_STOCK_QTY)
				{
					loadQty = (int)marketGoods.stock_qty - GameConstants.MIN_FIRM_STOCK_QTY;
					market_load_goods_now(marketGoods, loadQty);
				}
			}
			//else if(marketGoods->product_raw_id && isOurMarket) // only load product in our market
			else if (marketGoods.product_raw_id != 0)
			{
				goodsId = marketGoods.product_raw_id;
				goodsId--;
				if (processed_product_raw_qty_array[goodsId] == 2)
					continue; // continue if it is the goods unloaded

				if (marketGoods.stock_qty > GameConstants.MIN_FIRM_STOCK_QTY)
				{
					loadQty = (int)marketGoods.stock_qty - GameConstants.MIN_FIRM_STOCK_QTY;
					market_load_goods_now(marketGoods, loadQty);
				}
			}
		}
	}

	private void market_load_goods_now(MarketGoods marketGoods, double loadQty)
	{
		Nation nation = NationArray[NationId];
		int marketNationRecno = FirmArray[stop_array[dest_stop_id - 1].firm_recno].NationId;
		int qty = 0;
		int goodsId;

		if (marketGoods.product_raw_id != 0)
		{
			//---------------- is product ------------------//
			goodsId = marketGoods.product_raw_id;
			goodsId--;

			qty = Math.Min(GameConstants.MAX_CARAVAN_CARRY_QTY - product_raw_qty_array[goodsId], (int)loadQty);
			if (marketNationRecno != NationId) // calculate the qty again if this is not our own market
			{
				qty = (nation.cash > 0.0) ? Math.Min((int)(nation.cash / GameConstants.PRODUCT_PRICE), qty) : 0;

				if (qty != 0)
					nation.import_goods(NationBase.IMPORT_PRODUCT, marketNationRecno, qty * GameConstants.PRODUCT_PRICE);
			}

			product_raw_qty_array[goodsId] += qty;
			marketGoods.stock_qty -= qty;
		}
		else if (marketGoods.raw_id != 0)
		{
			//---------------- is raw ---------------------//
			goodsId = marketGoods.raw_id;
			goodsId--;

			qty = Math.Min(GameConstants.MAX_CARAVAN_CARRY_QTY - raw_qty_array[goodsId], (int)loadQty);
			if (marketNationRecno != NationId) // calculate the qty again if this is not our own market
			{
				qty = (nation.cash > 0.0) ? Math.Min((int)(nation.cash / GameConstants.RAW_PRICE), qty) : 0;

				if (qty != 0)
					nation.import_goods(NationBase.IMPORT_RAW, marketNationRecno, qty * GameConstants.RAW_PRICE);
			}

			raw_qty_array[goodsId] += qty;
			marketGoods.stock_qty -= qty;
		}

		if (qty > 0)
			last_load_goods_date = Info.game_date;
	}

	// select a suitable location to leave the stop
	private bool appear_in_firm_surround(ref int xLoc, ref int yLoc, Firm firm)
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
}