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

	public const int SHIP_MENU_UNIT = 1;
	public const int SHIP_MENU_GOODS = 2;

	//TODO make it work
	//public Sprite splash;

	public int menu_mode; // goods menu or units menu
	public int extra_move_in_beach;
	public bool in_beach;

	//------ vars for carrying units ------//

	public int selected_unit_id;

	public readonly List<int> UnitsOnBoard = new List<int>();

	//------ vars for carrying goods ------//

	public int journey_status; // 1 for not unload but can up load, 2 for unload but not up load
	public int dest_stop_id; // destination stop id. the stop which the train currently is moving towards
	public int stop_defined_num; // num of stop defined
	public int wait_count; // set to -1 to indicate only one stop is specified

	public int stop_x_loc; // the x location the unit entering the stop
	public int stop_y_loc; // the y location the unit entering the stop

	public int auto_mode; // indicate whether auto mode is on/off, 1 - on, 0 - off
	public int cur_firm_recno; // the recno of current firm the ship entered
	public int carry_goods_capacity;

	// an array of firm_recno telling train stop stations
	public ShipStop[] stop_array = new ShipStop[MAX_STOP_FOR_SHIP];

	public int[] raw_qty_array = new int[GameConstants.MAX_RAW];
	public int[] product_raw_qty_array = new int[GameConstants.MAX_PRODUCT];
	private List<int> linkedMines = new List<int>();
	private List<int> linkedFactories = new List<int>();
	private List<int> linkedMarkets = new List<int>();
	private List<int[]> mprocessed_raw_qty_array = new List<int[]>();
	private List<int[]> mprocessed_product_raw_qty_array = new List<int[]>();
	private List<int> empty_slot_position_array = new List<int>();
	private List<int> firm_selected_array = new List<int>();

	//----------- vars for attacking ----------//

	public AttackInfo ship_attack_info;
	public int attack_mode_selected;

	//-------------- vars for AI --------------//

	public DateTime last_load_goods_date;

	public UnitMarine()
	{
		for (int i = 0; i < stop_array.Length; i++)
			stop_array[i] = new ShipStop();

		journey_status = InternalConstants.ON_WAY_TO_FIRM;
		auto_mode = 1; // there should be no button to toggle it if the ship is only for trading
	}

	public override void Init(int unitType, int nationId, int rank, int unitLoyalty, int startLocX, int startLocY)
	{
		attack_mode_selected = 0; // for fix_attack_info() to set attack_info_array

		base.Init(unitType, nationId, rank, unitLoyalty, startLocX, startLocY);
		
		extra_move_in_beach = NO_EXTRA_MOVE;
		last_load_goods_date = Info.game_date;

		int spriteId = SpriteInfo.GetSubSpriteInfo(1).SpriteId;
		//splash.init(spriteId, cur_x_loc(), cur_y_loc());
		//splash.cur_frame = 1;

		UnitInfo unitInfo = UnitRes[unitType];
		carry_goods_capacity = unitInfo.carry_goods_capacity;
		if (unitInfo.carry_unit_capacity == 0 && unitInfo.carry_goods_capacity > 0) // if this ship only carries goods
			menu_mode = SHIP_MENU_GOODS;
		else
			menu_mode = SHIP_MENU_UNIT;
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

	public void update_stop_list()
	{
		//-------------------------------------------------------//
		// backup original destination stop firm recno
		//-------------------------------------------------------//
		int nextStopRecno = dest_stop_id != 0 ? stop_array[dest_stop_id - 1].firm_recno : 0;

		//----------------------------------------------------------------------//
		// check stop existence and the relationship between firm's nation
		//----------------------------------------------------------------------//
		ShipStop stop;
		int i;
		for (i = 0; i < MAX_STOP_FOR_SHIP; i++)
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

			if (!can_set_stop(firm.FirmId) || firm.LocX1 != stop.firm_loc_x1 || firm.LocY1 != stop.firm_loc_y1)
			{
				stop.firm_recno = 0;
				stop_defined_num--;
				continue;
			}
		}

		//-------------------------------------------------------//
		// remove duplicate node
		//-------------------------------------------------------//
		//ShipStop *insertNode = stop_array;
		int insertNodeIndex = 0;

		if (stop_defined_num < 1)
		{
			for (i = 0; i < stop_array.Length; i++)
				stop_array[i] = new ShipStop();
			dest_stop_id = 0;
			return; // no stop
		}

		//-------------------------------------------------------//
		// move the only firm_recno to the beginning of the array
		//-------------------------------------------------------//
		int compareRecno = 0;
		int nodeIndex = 0;
		for (i = 0; i < MAX_STOP_FOR_SHIP; i++, nodeIndex++)
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
				stop_array[i] = new ShipStop();
			dest_stop_id = 1;
			return;
		}

		int unprocessed = stop_defined_num - 1;
		insertNodeIndex++;
		nodeIndex++;

		for (; i < MAX_STOP_FOR_SHIP && unprocessed > 0; i++, nodeIndex++)
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

		if (stop_defined_num < MAX_STOP_FOR_SHIP)
		{
			for (i = stop_defined_num; i < stop_array.Length; i++)
				stop_array[i] = new ShipStop();
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
				stop_array[i] = new ShipStop();
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

	public int get_next_stop_id(int curStopId)
	{
		int nextStopId = (curStopId >= stop_defined_num) ? 1 : curStopId + 1;

		ShipStop stop = stop_array[nextStopId - 1];

		bool needUpdate = false;

		if (FirmArray.IsDeleted(stop.firm_recno))
		{
			needUpdate = true;
		}
		else
		{
			Firm firm = FirmArray[stop.firm_recno];

			if (!can_set_stop(firm.FirmId) || firm.LocX1 != stop.firm_loc_x1 || firm.LocY1 != stop.firm_loc_y1)
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

			for (int i = 0; i < stop_defined_num; i++)
			{
				stop = stop_array[i];
				if (stop.firm_recno == preStopRecno)
					nextStopId = (i >= stop_defined_num) ? 1 : i + 1;
			}
		}

		return nextStopId;
	}

	public override void PreProcess()
	{
		base.PreProcess();
		if (HitPoints <= 0.0 || ActionMode == UnitConstants.ACTION_DIE || CurAction == SPRITE_DIE)
			return;

		if (ActionMode2 >= UnitConstants.ACTION_ATTACK_UNIT && ActionMode2 <= UnitConstants.ACTION_ATTACK_WALL)
			return; // don't process trading if unit is attacking

		if (auto_mode != 0) // process trading automatically, same as caravan
		{
			if (journey_status == InternalConstants.INSIDE_FIRM)
			{
				ship_in_firm();
				return;
			}

			if (stop_defined_num == 0)
				return;

			//----------------- if there is only one defined stop --------------------//
			if (stop_defined_num == 1)
			{
				ShipStop stop = stop_array[0];

				if (FirmArray.IsDeleted(stop.firm_recno))
				{
					update_stop_list();
					return;
				}

				Firm firm = FirmArray[stop.firm_recno];
				if (firm.LocX1 != stop.firm_loc_x1 || firm.LocY1 != stop.firm_loc_y1)
				{
					update_stop_list();
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
						journey_status = InternalConstants.ON_WAY_TO_FIRM;
					//#### end alex 6/10 ####//
				}
				else
				{
					if (CurX == NextX && CurY == NextY && CurAction == SPRITE_IDLE)
					{
						journey_status = InternalConstants.SURROUND_FIRM;
						if (NationArray[NationId].get_relation(firm.NationId).trade_treaty)
						{
							if (wait_count <= 0)
							{
								//---------- unloading goods -------------//
								cur_firm_recno = stop_array[0].firm_recno;
								get_harbor_linked_firm_info();
								harbor_unload_goods();
								wait_count = GameConstants.MAX_SHIP_WAIT_TERM * InternalConstants.SURROUND_FIRM_WAIT_FACTOR;
								cur_firm_recno = 0;
							}
							else
								wait_count--;
						}
					}
				}

				return;
			}

			//------------ if there are more than one defined stop ---------------//
			ship_on_way();
		}
		else if (journey_status == InternalConstants.INSIDE_FIRM)
			ship_in_firm(0); // autoMode is off
	}

	public void set_stop_pick_up(int stopId, int newPickUpType, int remoteAction)
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
				stop_array[stopId - 1].pick_up_set_auto();
				break;

			case TradeStop.NO_PICK_UP:
				stop_array[stopId - 1].pick_up_set_none();
				break;

			default:
				stop_array[stopId - 1].pick_up_toggle(newPickUpType);
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

	public void ship_in_firm(int autoMode = 1)
	{
		//-----------------------------------------------------------------------------//
		// the harbor is deleted while the ship is in harbor
		//-----------------------------------------------------------------------------//
		if (cur_firm_recno != 0 && FirmArray.IsDeleted(cur_firm_recno))
		{
			HitPoints = 0.0; // ship also die if the harbor is deleted
			UnitArray.DisappearInFirm(SpriteId); // ship also die if the harnor is deleted
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
		// leave the harbor and go to another harbor if possible
		//-----------------------------------------------------------------------------//
		ShipStop shipStop = stop_array[dest_stop_id - 1];
		int xLoc = stop_x_loc;
		int yLoc = stop_y_loc;
		Location loc = World.GetLoc(xLoc, yLoc);
		Firm firm;

		//TODO change %2 == 0
		if (xLoc % 2 == 0 && yLoc % 2 == 0 && loc.CanMove(MobileType))
			InitSprite(xLoc, yLoc); // appear in the location the unit disappeared before
		else
		{
			//---- the entering location is blocked, select another location to leave ----//
			firm = FirmArray[cur_firm_recno];

			if (appear_in_firm_surround(ref xLoc, ref yLoc, firm))
			{
				InitSprite(xLoc, yLoc);
				Stop();
			}
			else
			{
				wait_count = GameConstants.MAX_SHIP_WAIT_TERM * 10; //********* BUGHERE, continue to wait or ....
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

		cur_firm_recno = 0;
		journey_status = InternalConstants.ON_WAY_TO_FIRM;

		if (autoMode != 0) // move to next firm only if autoMode is on
			MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, Firm.FIRM_HARBOR);
	}

	public void ship_on_way()
	{
		ShipStop shipStop = stop_array[dest_stop_id - 1];
		Firm firm;
		int nextXLoc;
		int nextYLoc;
		int moveStep;

		if (CurAction == SPRITE_IDLE && journey_status != InternalConstants.SURROUND_FIRM)
		{
			if (!FirmArray.IsDeleted(shipStop.firm_recno))
			{
				firm = FirmArray[shipStop.firm_recno];
				MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, Firm.FIRM_HARBOR);
				nextXLoc = NextLocX;
				nextYLoc = NextLocY;
				moveStep = MoveStepCoeff();
				if (nextXLoc >= firm.LocX1 - moveStep && nextXLoc <= firm.LocX2 + moveStep && nextYLoc >= firm.LocY1 - moveStep &&
				    nextYLoc <= firm.LocY2 + moveStep)
					journey_status = InternalConstants.SURROUND_FIRM;

				return;
			}
		}

		if (UnitArray.IsDeleted(SpriteId))
			return; //-***************** BUGHERE ***************//

		if (FirmArray.IsDeleted(shipStop.firm_recno))
		{
			update_stop_list();

			if (stop_defined_num != 0) // move to next stop
			{
				firm = FirmArray[stop_array[stop_defined_num - 1].firm_recno];
				MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, firm.FirmType);
			}

			return;
		}

		//ShipStop *stop = stop_array + dest_stop_id - 1;
		firm = FirmArray[shipStop.firm_recno];

		nextXLoc = NextLocX;
		nextYLoc = NextLocY;
		moveStep = MoveStepCoeff();
		if (journey_status == InternalConstants.SURROUND_FIRM ||
		    (nextXLoc == MoveToLocX && nextYLoc == MoveToLocY && CurX == NextX && CurY == NextY && // move in a tile exactly
		     (nextXLoc >= firm.LocX1 - moveStep && nextXLoc <= firm.LocX2 + moveStep && nextYLoc >= firm.LocY1 - moveStep &&
		      nextYLoc <= firm.LocY2 + moveStep)))
		{
			extra_move_in_beach = NO_EXTRA_MOVE; // since the ship may enter the firm in odd location		

			shipStop.update_pick_up();

			//-------------------------------------------------------//
			// load/unload goods
			//-------------------------------------------------------//
			cur_firm_recno = shipStop.firm_recno;

			if (NationArray[NationId].get_relation(firm.NationId).trade_treaty)
			{
				get_harbor_linked_firm_info();
				harbor_unload_goods();
				if (shipStop.pick_up_type == TradeStop.AUTO_PICK_UP)
					harbor_auto_load_goods();
				else if (shipStop.pick_up_type != TradeStop.NO_PICK_UP)
					harbor_load_goods();
			}

			//-------------------------------------------------------//
			//-------------------------------------------------------//
			stop_x_loc = MoveToLocX; // store entering location
			stop_y_loc = MoveToLocY;
			wait_count = GameConstants.MAX_SHIP_WAIT_TERM; // set waiting term

			ResetPath();
			DeinitSprite(true); // the ship enters the harbor now. 1-keep it selected if it is currently selected

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
				MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, Firm.FIRM_HARBOR);
				journey_status = InternalConstants.ON_WAY_TO_FIRM;
			}
		}
	}

	public bool appear_in_firm_surround(ref int xLoc, ref int yLoc, Firm firm)
	{
		FirmInfo firmInfo = FirmRes[Firm.FIRM_HARBOR];
		int firmWidth = firmInfo.loc_width;
		int firmHeight = firmInfo.loc_height;
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

	public void get_harbor_linked_firm_info()
	{
		FirmHarbor firmHarbor = (FirmHarbor)FirmArray[cur_firm_recno];

		firmHarbor.update_linked_firm_info();
		linkedMines.Clear();
		linkedMines.AddRange(firmHarbor.linked_mine_array);
		linkedFactories.Clear();
		linkedFactories.AddRange(firmHarbor.linked_factory_array);
		linkedMarkets.Clear();
		linkedMarkets.AddRange(firmHarbor.linked_market_array);
	}

	public void harbor_unload_goods()
	{
		if (linkedMines.Count + linkedFactories.Count + linkedMarkets.Count == 0)
			return;

		mprocessed_raw_qty_array.Clear();
		mprocessed_product_raw_qty_array.Clear();

		harbor_unload_product();
		harbor_unload_raw();
	}

	public void harbor_unload_product()
	{
		if (linkedMarkets.Count == 0)
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
			if (product_raw_qty_array[i] == 0)
				continue; // without this goods

			//----------------------------------------------------------------------//
			// calculate the demand of this goods in market
			//----------------------------------------------------------------------//
			totalDemand = 0;
			empty_slot_position_array.Clear();
			firm_selected_array.Clear();
			linkedMarketIndex = 0;
			firmSelectedIndex = 0;
			for (j = 0; j < linkedMarkets.Count; j++, linkedMarketIndex++, firmSelectedIndex++)
			{
				market = (FirmMarket)FirmArray[linkedMarkets[linkedMarketIndex]];

				if (market.NationId != NationId)
					continue; // don't unload goods to market of other nation

				if (market.AIStatus == Firm.MARKET_FOR_SELL)
					continue; // clearing the market stock, so no unloading

				//---------- check the demand of this goods in the market ---------//
				marketProduct = market.market_product_array[i];
				if (marketProduct != null)
				{
					totalDemand += (int)(market.MaxStockQty - marketProduct.StockQty);
					firmSelectedIndex++;
				}
				else // don't have this product, clear for empty slot
				{
					for (k = 0; k < GameConstants.MAX_MARKET_GOODS; k++)
					{
						marketGoods = market.market_goods_array[k];
						if (marketGoods.StockQty == 0 && marketGoods.Supply30Days() <= 0.0)
						{
							empty_slot_position_array[j] = k;
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
			curStock = product_raw_qty_array[i];
			linkedMarketIndex = 0;
			firmSelectedIndex = 0;
			for (j = 0; j < linkedMarkets.Count; j++, linkedMarketIndex++, firmSelectedIndex++)
			{
				if (firm_selected_array[firmSelectedIndex] == 0)
					continue;

				market = (FirmMarket)FirmArray[linkedMarkets[linkedMarketIndex]];

				marketProduct = market.market_product_array[i];
				if (marketProduct == null) // using empty slot, don't set the pointer to the market_goods_array until unloadQty>0
				{
					useEmptySlot = true;
					marketProduct = market.market_goods_array[empty_slot_position_array[j]];
				}
				else
					useEmptySlot = false;

				unloadQty = totalDemand != 0 ? (int)((market.MaxStockQty - marketProduct.StockQty) * curStock / totalDemand + 0.5) : 0;
				unloadQty = Math.Min((int)(market.MaxStockQty - marketProduct.StockQty), unloadQty);
				unloadQty = Math.Min(product_raw_qty_array[i], unloadQty);

				if (unloadQty != 0)
				{
					if (useEmptySlot)
						market.SetGoods(false, i + 1, empty_slot_position_array[j]);

					marketProduct.StockQty += unloadQty;
					product_raw_qty_array[i] -= unloadQty;
					mprocessed_product_raw_qty_array[linkedMines.Count + linkedFactories.Count + j][i] += 2;
				}
			}
		}
	}

	public void harbor_unload_raw()
	{
		if (linkedFactories.Count == 0 && linkedMarkets.Count == 0)
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
			if (raw_qty_array[i] == 0)
				continue; // without this goods

			totalDemand = 0;
			empty_slot_position_array.Clear();
			firm_selected_array.Clear();
			firmSelectedIndex = 0;
			//----------------------------------------------------------------------//
			// calculate the demand of this goods in factory
			//----------------------------------------------------------------------//
			linkedFactoryIndex = 0;
			for (j = 0; j < linkedFactories.Count; j++, linkedFactoryIndex++, firmSelectedIndex++)
			{
				factory = (FirmFactory)FirmArray[linkedFactories[linkedFactoryIndex]];

				if (factory.NationId != NationId)
					continue; // don't unload goods to factory of other nation

				if (factory.AIStatus == Firm.FACTORY_RELOCATE)
					continue; // clearing the factory stock, so no unloading

				if (factory.ProductRawId - 1 == i)
				{
					totalDemand = (int)(factory.MaxRawStockQty - factory.RawStockQty);
					firmSelectedIndex++;
				}
			}

			//----------------------------------------------------------------------//
			// calculate the demand of this goods in market
			//----------------------------------------------------------------------//
			linkedMarketIndex = 0;
			for (j = 0; j < linkedMarkets.Count; j++, linkedMarketIndex++, firmSelectedIndex++)
			{
				market = (FirmMarket)FirmArray[linkedMarkets[linkedMarketIndex]];

				if (market.NationId != NationId)
					continue; // don't unload goods to market of other nation

				if (market.AIStatus == Firm.MARKET_FOR_SELL)
					continue; // clearing the market stock, so no unloading

				//---------- check the demand of this goods in the market ---------//
				marketRaw = market.market_raw_array[i];
				if (marketRaw != null)
				{
					totalDemand += (int)(market.MaxStockQty - marketRaw.StockQty);
					firmSelectedIndex++;
				}
				else // don't have this raw, clear for empty slot
				{
					for (k = 0; k < GameConstants.MAX_MARKET_GOODS; k++)
					{
						marketGoods = market.market_goods_array[k];
						if (marketGoods.StockQty <= 0.0 && marketGoods.Supply30Days() <= 0.0)
						{
							empty_slot_position_array[j] = k;
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
			curStock = raw_qty_array[i];
			linkedFactoryIndex = 0;
			firmSelectedIndex = 0;
			for (j = 0; j < linkedFactories.Count; j++, linkedFactoryIndex++, firmSelectedIndex++)
			{
				if (firm_selected_array[firmSelectedIndex] == 0)
					continue;

				factory = (FirmFactory)FirmArray[linkedFactories[linkedFactoryIndex]];

				unloadQty = totalDemand != 0 ? (int)((factory.MaxRawStockQty - factory.RawStockQty) * curStock / totalDemand + 0.5) : 0;
				unloadQty = Math.Min((int)(factory.MaxRawStockQty - factory.RawStockQty), unloadQty);
				unloadQty = Math.Min(raw_qty_array[i], unloadQty);

				factory.RawStockQty += unloadQty;
				raw_qty_array[i] -= unloadQty;
				mprocessed_raw_qty_array[linkedMines.Count + j][i] += 2;
			}

			//----------------------------------------------------------------------//
			// distribute the stock into each market
			//----------------------------------------------------------------------//
			linkedMarketIndex = 0;
			for (j = 0; j < linkedMarkets.Count; j++, linkedMarketIndex++, firmSelectedIndex++)
			{
				if (firm_selected_array[firmSelectedIndex] == 0)
					continue;

				market = (FirmMarket)FirmArray[linkedMarkets[linkedMarketIndex]];

				marketRaw = market.market_raw_array[i];
				if (marketRaw == null) // using empty slot, don't set the pointer to the market_goods_array until unloadQty>0
				{
					useEmptySlot = true;
					marketRaw = market.market_goods_array[empty_slot_position_array[j]];
				}
				else
					useEmptySlot = false;

				unloadQty = totalDemand != 0 ? (int)((market.MaxStockQty - marketRaw.StockQty) * curStock / totalDemand + 0.5) : 0;
				unloadQty = Math.Min((int)(market.MaxStockQty - marketRaw.StockQty), unloadQty);
				unloadQty = Math.Min(raw_qty_array[i], unloadQty);

				if (unloadQty != 0)
				{
					if (useEmptySlot)
						market.SetGoods(true, i + 1, empty_slot_position_array[j]);

					marketRaw.StockQty += unloadQty;
					raw_qty_array[i] -= unloadQty;
					mprocessed_raw_qty_array[linkedMines.Count + linkedFactories.Count + j][i] += 2;
				}
			}
		}
	}

	public void harbor_load_goods()
	{
		if (linkedMines.Count + linkedFactories.Count + linkedMarkets.Count == 0)
			return;

		ShipStop shipStop = stop_array[dest_stop_id - 1];
		if (shipStop.pick_up_type == TradeStop.NO_PICK_UP)
			return; // return if not allowed to load any goods

		for (int i = 0; i < TradeStop.MAX_PICK_UP_GOODS; i++)
		{
			if (!shipStop.pick_up_array[i])
				continue;

			int pickUpType = i + 1;
			int goodsId;
			if (pickUpType >= TradeStop.PICK_UP_RAW_FIRST && pickUpType <= TradeStop.PICK_UP_RAW_LAST)
			{
				goodsId = pickUpType - TradeStop.PICK_UP_RAW_FIRST;

				if (raw_qty_array[goodsId] < carry_goods_capacity)
					harbor_load_raw(goodsId, false, 1); // 1 -- only consider our firm

				if (raw_qty_array[goodsId] < carry_goods_capacity)
					harbor_load_raw(goodsId, false, 0); // 0 -- only consider firm of other nation
			}
			else if (pickUpType >= TradeStop.PICK_UP_PRODUCT_FIRST && pickUpType <= TradeStop.PICK_UP_PRODUCT_LAST)
			{
				goodsId = pickUpType - TradeStop.PICK_UP_PRODUCT_FIRST;

				if (product_raw_qty_array[goodsId] < carry_goods_capacity) // 1 -- only consider our firm
					harbor_load_product(goodsId, false, 1);

				if (product_raw_qty_array[goodsId] < carry_goods_capacity) // 0 -- only consider firm of other nation
					harbor_load_product(goodsId, false, 0);
			}
		}
	}

	public void harbor_auto_load_goods()
	{
		if (linkedMines.Count + linkedFactories.Count + linkedMarkets.Count == 0)
			return;

		for (int i = 0; i < GameConstants.MAX_PRODUCT; i++)
		{
			if (product_raw_qty_array[i] < carry_goods_capacity)
				harbor_load_product(i, true, 1); // 1 -- only consider our market
		}

		for (int i = 0; i < GameConstants.MAX_RAW; i++)
		{
			if (raw_qty_array[i] < carry_goods_capacity)
				harbor_load_raw(i, true, 1); // 1 -- only consider our market
		}
	}

	public void harbor_load_product(int goodsId, bool autoPickUp, int considerMode)
	{
		if (linkedFactories.Count + linkedMarkets.Count == 0)
			return;

		if (product_raw_qty_array[goodsId] == carry_goods_capacity)
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
		firm_selected_array.Clear();
		firmSelectedIndex = 0;
		//----------------------------------------------------------------------//
		// calculate the supply of this goods in factory
		//----------------------------------------------------------------------//
		if (linkedFactories.Count > 0)
		{
			factory = (FirmFactory)FirmArray[linkedFactories[0]];
			keepStockQty = autoPickUp ? (int)(factory.MaxStockQty / 5.0) : 0;
		}

		linkedFactoryIndex = 0;
		for (i = 0; i < linkedFactories.Count; i++, linkedFactoryIndex++, firmSelectedIndex++)
		{
			if (mprocessed_product_raw_qty_array[linkedMines.Count + i][goodsId] == 2)
				continue;

			factory = (FirmFactory)FirmArray[linkedFactories[linkedFactoryIndex]];

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
			if (factory.ProductRawId != goodsId + 1)
				continue; // incorrect product

			totalSupply += Math.Max((int)(factory.StockQty - keepStockQty), 0);
			firmSelectedIndex++;
		}

		//----------------------------------------------------------------------//
		// calculate the supply of this goods in market
		//----------------------------------------------------------------------//
		if (linkedMarkets.Count > 0)
		{
			market = (FirmMarket)FirmArray[linkedMarkets[0]];
			keepStockQty = autoPickUp ? (int)(market.MaxStockQty / 5.0) : 0;
		}

		linkedMarketIndex = 0;
		for (i = 0; i < linkedMarkets.Count; i++, linkedMarketIndex++, firmSelectedIndex++)
		{
			if (mprocessed_product_raw_qty_array[linkedMines.Count + linkedFactories.Count + i][goodsId] == 2)
				continue;

			market = (FirmMarket)FirmArray[linkedMarkets[linkedMarketIndex]];

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
			marketProduct = market.market_product_array[goodsId];
			if (marketProduct != null)
			{
				totalSupply += Math.Max((int)(marketProduct.StockQty - keepStockQty), 0);
				firmSelectedIndex++;
			}
		}

		Nation nation = NationArray[NationId];
		int curDemand = carry_goods_capacity - product_raw_qty_array[goodsId];
		firmSelectedIndex = 0;
		//----------------------------------------------------------------------//
		// get the stock from each factory
		//----------------------------------------------------------------------//
		if (linkedFactories.Count > 0)
		{
			factory = (FirmFactory)FirmArray[linkedFactories[0]];
			keepStockQty = autoPickUp ? (int)(factory.MaxStockQty / 5.0) : 0;
		}

		linkedFactoryIndex = 0;
		for (i = 0; i < linkedFactories.Count; i++, linkedFactoryIndex++, firmSelectedIndex++)
		{
			if (firm_selected_array[firmSelectedIndex] == 0)
				continue;

			factory = (FirmFactory)FirmArray[linkedFactories[linkedFactoryIndex]];

			loadQty = Math.Max((int)(factory.StockQty - keepStockQty), 0);
			loadQty = totalSupply != 0 ? Math.Min(loadQty * curDemand / totalSupply, loadQty) : 0;

			if (factory.NationId != NationId)
			{
				loadQty = (nation.cash > 0) ? (int)Math.Min(nation.cash / GameConstants.PRODUCT_PRICE, loadQty) : 0;
				if (loadQty > 0)
					nation.import_goods(NationBase.IMPORT_PRODUCT, factory.NationId, loadQty * GameConstants.PRODUCT_PRICE);
			}

			factory.StockQty -= loadQty;
			product_raw_qty_array[goodsId] += loadQty;
		}

		//----------------------------------------------------------------------//
		// get the stock from each market
		//----------------------------------------------------------------------//
		if (linkedMarkets.Count > 0)
		{
			market = (FirmMarket)FirmArray[linkedMarkets[0]];
			keepStockQty = autoPickUp ? (int)(market.MaxStockQty / 5.0) : 0;
		}

		linkedMarketIndex = 0;
		for (i = 0; i < linkedMarkets.Count; i++, linkedMarketIndex++, firmSelectedIndex++)
		{
			if (firm_selected_array[firmSelectedIndex] == 0)
				continue;

			market = (FirmMarket)FirmArray[linkedMarkets[linkedMarketIndex]];

			marketProduct = market.market_product_array[goodsId];

			loadQty = Math.Max((int)marketProduct.StockQty - keepStockQty, 0);
			loadQty = totalSupply != 0 ? Math.Min(loadQty * curDemand / totalSupply, loadQty) : 0;

			if (market.NationId != NationId)
			{
				loadQty = (nation.cash > 0) ? (int)Math.Min(nation.cash / GameConstants.PRODUCT_PRICE, loadQty) : 0;
				if (loadQty > 0)
					nation.import_goods(NationBase.IMPORT_PRODUCT, market.NationId, loadQty * GameConstants.PRODUCT_PRICE);
			}

			marketProduct.StockQty -= loadQty;
			product_raw_qty_array[goodsId] += loadQty;
		}
	}

	public void harbor_load_raw(int goodsId, bool autoPickUp, int considerMode)
	{
		if (linkedMines.Count + linkedMarkets.Count == 0)
			return;

		if (raw_qty_array[goodsId] == carry_goods_capacity)
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
		firm_selected_array.Clear();
		firmSelectedIndex = 0;
		//----------------------------------------------------------------------//
		// calculate the supply of this goods in mine
		//----------------------------------------------------------------------//
		if (linkedMines.Count > 0)
		{
			mine = (FirmMine)FirmArray[linkedMines[0]];
			keepStockQty = autoPickUp ? (int)(mine.MaxStockQty / 5.0) : 0;
		}

		linkedMineIndex = 0;
		for (i = 0; i < linkedMines.Count; i++, linkedMineIndex++, firmSelectedIndex++)
		{
			if (mprocessed_raw_qty_array[i][goodsId] == 2)
				continue;

			mine = (FirmMine)FirmArray[linkedMines[linkedMineIndex]];

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
		if (linkedMarkets.Count > 0)
		{
			market = (FirmMarket)FirmArray[linkedMarkets[0]];
			keepStockQty = autoPickUp ? (int)(market.MaxStockQty / 5.0) : 0;
		}

		linkedMarketIndex = 0;
		for (i = 0; i < linkedMarkets.Count; i++, linkedMarketIndex++, firmSelectedIndex++)
		{
			if (mprocessed_raw_qty_array[linkedMines.Count + linkedFactories.Count + i][goodsId] == 2)
				continue;

			market = (FirmMarket)FirmArray[linkedMarkets[linkedMarketIndex]];

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
			marketRaw = market.market_raw_array[goodsId];
			if (marketRaw != null)
			{
				totalSupply += Math.Max((int)(marketRaw.StockQty - keepStockQty), 0);
				firmSelectedIndex++;
			}
		}

		Nation nation = NationArray[NationId];
		int curDemand = carry_goods_capacity - raw_qty_array[goodsId];
		firmSelectedIndex = 0;
		//----------------------------------------------------------------------//
		// get the stock from each mine
		//----------------------------------------------------------------------//
		if (linkedMines.Count > 0)
		{
			mine = (FirmMine)FirmArray[linkedMines[0]];
			keepStockQty = autoPickUp ? (int)(mine.MaxStockQty / 5.0) : 0;
		}

		linkedMineIndex = 0;
		for (i = 0; i < linkedMines.Count; i++, linkedMineIndex++, firmSelectedIndex++)
		{
			if (firm_selected_array[firmSelectedIndex] == 0)
				continue;

			mine = (FirmMine)FirmArray[linkedMines[linkedMineIndex]];

			loadQty = Math.Max((int)(mine.StockQty - keepStockQty), 0);
			loadQty = totalSupply != 0 ? Math.Min(loadQty * curDemand / totalSupply, loadQty) : 0;

			if (mine.NationId != NationId)
			{
				loadQty = (nation.cash > 0) ? (int)Math.Min(nation.cash / GameConstants.RAW_PRICE, loadQty) : 0;
				if (loadQty > 0)
					nation.import_goods(NationBase.IMPORT_RAW, mine.NationId, loadQty * GameConstants.RAW_PRICE);
			}

			mine.StockQty -= loadQty;
			raw_qty_array[goodsId] += loadQty;
		}

		//----------------------------------------------------------------------//
		// get the stock from each market
		//----------------------------------------------------------------------//
		if (linkedMarkets.Count > 0)
		{
			market = (FirmMarket)FirmArray[linkedMarkets[0]];
			keepStockQty = autoPickUp ? (int)(market.MaxStockQty / 5.0) : 0;
		}

		linkedMarketIndex = 0;
		for (i = 0; i < linkedMarkets.Count; i++, linkedMarketIndex++, firmSelectedIndex++)
		{
			if (firm_selected_array[firmSelectedIndex] == 0)
				continue;

			market = (FirmMarket)FirmArray[linkedMarkets[linkedMarketIndex]];

			marketRaw = market.market_raw_array[goodsId];

			loadQty = Math.Max((int)marketRaw.StockQty - keepStockQty, 0);
			loadQty = totalSupply != 0 ? Math.Min(loadQty * curDemand / totalSupply, loadQty) : 0;

			if (market.NationId != NationId)
			{
				loadQty = (nation.cash > 0) ? (int)Math.Min(nation.cash / GameConstants.RAW_PRICE, loadQty) : 0;
				if (loadQty > 0)
					nation.import_goods(NationBase.IMPORT_RAW, market.NationId, loadQty * GameConstants.RAW_PRICE);
			}

			marketRaw.StockQty -= loadQty;
			raw_qty_array[goodsId] += loadQty;
		}
	}

	public int total_carried_goods()
	{
		int totalQty = 0;

		for (int i = 0; i < GameConstants.MAX_RAW; i++)
		{
			totalQty += raw_qty_array[i];
			totalQty += product_raw_qty_array[i];
		}

		return totalQty;
	}

	protected override void UpdateAbsPos(SpriteFrame spriteFrame = null)
	{
		base.UpdateAbsPos(spriteFrame);
		int h = wave_height(6);
		//abs_y1 -= h;
		//abs_y2 -= h;
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

	public int wave_height(int phase = 0)
	{
		int[] heights = { 4, 3, 2, 1, 0, 1, 2, 3 };
		return heights[(Sys.Instance.FrameNumber / 4 + phase) % InternalConstants.WAVE_CYCLE];
	}

	public bool can_unload_unit()
	{
		return false;
	}

	public void load_unit(int unitRecno)
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

	public void unload_unit(int unitSeqId, int remoteAction)
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

		if (unloading_unit(false, unitSeqId - 1)) // unit is unloaded
		{
			UnitsOnBoard.RemoveAt(unitSeqId - 1);
		}
	}

	public void unload_all_units(int remoteAction)
	{
		//if (!remoteAction && remote.is_enable())
		//{
			//// packet structure : <unit recno>
			//short* shortPtr = (short*)remote.new_send_queue_msg(MSG_U_SHIP_UNLOAD_ALL_UNITS, sizeof(short));
			//*shortPtr = sprite_recno;
			//return;
		//}

		unloading_unit(true); // unload all units
	}

	public bool unloading_unit(bool isAll, int unitSeqId = 0)
	{
		if (!is_on_coast())
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

	public void del_unit(int unitRecno)
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

	public bool can_set_stop(int firmRecno)
	{
		Firm firm = FirmArray[firmRecno];

		if (firm.UnderConstruction)
			return false;

		if (firm.FirmType != Firm.FIRM_HARBOR)
			return false;

		return NationArray[NationId].get_relation(firm.NationId).trade_treaty;
	}

	public void extra_move()
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
				World.SetUnitId(curXLoc, curYLoc, MobileType, 0);
				World.SetUnitId(goXLoc, goYLoc, MobileType, SpriteId);
				NextX = GoX;
				NextY = GoY;

				//TODO %2 == 0
				in_beach = !(curXLoc % 2 != 0 || curYLoc % 2 != 0);

				if (goXLoc % 2 != 0 || goYLoc % 2 != 0) // not even location
					extra_move_in_beach = EXTRA_MOVING_IN;
				else // even location
					extra_move_in_beach = EXTRA_MOVING_OUT;
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

			if (in_beach)
			{
				extra_move_in_beach = EXTRA_MOVE_FINISH;
			}
			else
			{
				extra_move_in_beach = NO_EXTRA_MOVE;
			}
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
		// return if the harbor stop is in another territory
		//-------------------------------------------------------//
		FirmHarbor harbor = (FirmHarbor)firm;

		if (World.GetLoc(NextLocX, NextLocY).RegionId != harbor.sea_region_id)
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

		if (stop_array[stopId - 1].firm_recno == 0)
			stop_defined_num++; // no plus one if the recno is defined originally

		//-------------------------------------------------------//
		// set the station recno of the stop
		//-------------------------------------------------------//
		ShipStop shipStop = stop_array[stopId - 1];
		if (shipStop.firm_recno == firm.FirmId)
		{
			return; // same stop as before
		}

		int oldStopFirmRecno = dest_stop_id != 0 ? stop_array[dest_stop_id - 1].firm_recno : 0;
		shipStop.firm_recno = firm.FirmId;
		shipStop.firm_loc_x1 = firm.LocX1;
		shipStop.firm_loc_y1 = firm.LocY1;

		//-------------------------------------------------------//
		// set pick up selection based on availability
		//-------------------------------------------------------//
		shipStop.pick_up_set_auto();

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
					id = ((FirmFactory)firm).ProductRawId + GameConstants.MAX_RAW;
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
						MarketGoods goods = ((FirmMarket)firm).market_goods_array[j];
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
			shipStop.pick_up_toggle(goodsId); // cancel auto_pick_up
		else if (goodsNum == 0)
			shipStop.pick_up_set_none();

		//-------------------------------------------------------//
		// remove duplicate stop or stop change nation
		//-------------------------------------------------------//
		update_stop_list();

		//-------------------------------------------------------//
		// handle if current stop changed when mobile
		//-------------------------------------------------------//
		if (dest_stop_id != 0 && journey_status != InternalConstants.INSIDE_FIRM)
		{
			int newStopFirmRecno = stop_array[dest_stop_id - 1].firm_recno;
			if (newStopFirmRecno != oldStopFirmRecno)
			{
				firm = FirmArray[newStopFirmRecno];
				MoveToFirmSurround(firm.LocX1, firm.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight, Firm.FIRM_HARBOR);
				journey_status = InternalConstants.ON_WAY_TO_FIRM;
			}
		}
		else if (journey_status != InternalConstants.INSIDE_FIRM)
			Stop2();
	}

	public void del_stop(int stopId, int remoteAction)
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

		stop_array[stopId - 1].firm_recno = 0;
		stop_defined_num--;
		update_stop_list();
	}

	public override bool IsAIAllStop()
	{
		if (CurAction != SPRITE_IDLE || AIActionId != 0)
			return false;

		//---- if the ship is on the beach, it's action mode is always ACTION_SHIP_TO_BEACH, so we can't check it against ACTION_STOP ---//

		if (ActionMode2 == UnitConstants.ACTION_SHIP_TO_BEACH)
		{
			if (in_beach && (extra_move_in_beach == NO_EXTRA_MOVE || extra_move_in_beach == EXTRA_MOVE_FINISH))
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
			if (attack_mode_selected == 0)
			{
				ship_attack_info = UnitRes.GetAttackInfo(UnitRes[UnitType].first_attack);
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

	public void copy_route(int copyUnitRecno, int remoteAction)
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
		int num_stops = stop_defined_num;
		for (int i = 0; i < num_stops; i++)
			del_stop(1, InternalConstants.COMMAND_AUTO); // stop ids shift up

		for (int i = 0; i < MAX_STOP_FOR_SHIP; i++)
		{
			ShipStop shipStopA = copyUnit.stop_array[i];
			ShipStop shipStopB = stop_array[i];
			if (shipStopA.firm_recno == 0)
				break;

			if (FirmArray.IsDeleted(shipStopA.firm_recno))
				continue;

			Firm firm = FirmArray[shipStopA.firm_recno];
			set_stop(i + 1, shipStopA.firm_loc_x1, shipStopA.firm_loc_y1, InternalConstants.COMMAND_AUTO);

			if (shipStopA.pick_up_type == TradeStop.AUTO_PICK_UP)
			{
				set_stop_pick_up(i + 1, TradeStop.AUTO_PICK_UP, InternalConstants.COMMAND_AUTO);
			}

			else if (shipStopA.pick_up_type == TradeStop.NO_PICK_UP)
			{
				set_stop_pick_up(i + 1, TradeStop.NO_PICK_UP, InternalConstants.COMMAND_AUTO);
			}

			else
			{
				for (int b = 0; b < TradeStop.MAX_PICK_UP_GOODS; ++b)
				{
					if (shipStopA.pick_up_array[b] != shipStopB.pick_up_array[b])
						set_stop_pick_up(i + 1, b + 1, InternalConstants.COMMAND_PLAYER);
				}
			}
		}
	}

	//------- ai functions --------//

	public override void ProcessAI()
	{
		//-- Think about removing stops whose owner nation is at war with us. --//

		// AI does do any sea trade

		//	if( info.game_date%30 == sprite_recno%30 )
		//		think_del_stop();

		//---- Think about setting new trade route -------//

		if (Info.TotalDays % 15 == SpriteId % 15)
		{
			if (stop_defined_num < 2 && IsVisible() && IsAIAllStop())
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

		for (int i = stop_defined_num; i > 0; i--)
		{
			if (FirmArray.IsDeleted(stop_array[i - 1].firm_recno))
			{
				del_stop(i, InternalConstants.COMMAND_AI);
				continue;
			}

			//----------------------------------------------//

			int nationRecno = FirmArray[stop_array[i - 1].firm_recno].NationId;

			if (nation.get_relation_status(nationRecno) == NationBase.NATION_HOSTILE)
			{
				del_stop(i, InternalConstants.COMMAND_AI);
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

			if (firmHarbor.sea_region_id != curRegionId)
				continue;

			int curRating = World.DistanceRating(curXLoc, curYLoc, firmHarbor.LocCenterX, firmHarbor.LocCenterY);

			curRating += (GameConstants.MAX_SHIP_IN_HARBOR - firmHarbor.ship_recno_array.Count) * 100;

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

	public bool is_on_coast()
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
}