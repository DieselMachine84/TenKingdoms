using System;

namespace TenKingdoms;

public class UnitCaravan : Unit
{
	public const int MAX_STOP_FOR_CARAVAN = 3;

	public const int NO_STOP_DEFINED = 0;
	public const int ON_WAY_TO_FIRM = 1;
	public const int SURROUND_FIRM = 2;
	public const int INSIDE_FIRM = 3;

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

	public UnitCaravan()
	{
		for (int i = 0; i < stop_array.Length; i++)
			stop_array[i] = new CaravanStop();

		journey_status = ON_WAY_TO_FIRM;
		dest_stop_id = 1;
		stop_defined_num = 0;
		wait_count = 0;
		stop_x_loc = 0;
		stop_y_loc = 0;
	}

	public override void init_derived()
	{
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
		Location loc = World.get_loc(stopXLoc, stopYLoc);
		if (!loc.is_firm())
			return;

		Firm firm = FirmArray[loc.firm_recno()];

		if (!can_set_stop(firm.firm_recno))
			return;

		//-------------------------------------------------------//
		// return if the market stop is in another territory
		//-------------------------------------------------------//
		if (World.get_loc(next_x_loc(), next_y_loc()).region_id != loc.region_id)
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
		if (stop.firm_recno == firm.firm_recno)
		{
			return; // same stop as before
		}

		//-------------- reset ignore_power_nation -------------//
		ignore_power_nation = 0;

		int oldStopFirmRecno = dest_stop_id != 0 ? stop_array[dest_stop_id - 1].firm_recno : 0;
		int newStopFirmRecno;
		for (int i = 0; i < stop.pick_up_array.Length; i++)
			stop.pick_up_array[i] = false;
		stop.firm_recno = firm.firm_recno;
		stop.firm_id = firm.firm_id;
		stop.firm_loc_x1 = firm.loc_x1;
		stop.firm_loc_y1 = firm.loc_y1;

		int goodsId = 0;
		//------------------------------------------------------------------------------------//
		// codes for setting pick_up_type
		//------------------------------------------------------------------------------------//
		switch (firm.firm_id)
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
		if (dest_stop_id != 0 && journey_status != INSIDE_FIRM)
		{
			if ((newStopFirmRecno = stop_array[dest_stop_id - 1].firm_recno) != oldStopFirmRecno)
			{
				firm = FirmArray[newStopFirmRecno];
				move_to_firm_surround(firm.loc_x1, firm.loc_y1, sprite_info.loc_width, sprite_info.loc_height,
					stop_array[dest_stop_id - 1].firm_id);
				journey_status = ON_WAY_TO_FIRM;
			}
		}
		else if (journey_status != INSIDE_FIRM)
			stop2();

		if (UnitArray.selected_recno == sprite_recno)
		{
			if (nation_recno == NationArray.player_recno || Config.show_ai_info)
				Info.disp();
		}
	}

	public void del_stop(int stopId, char remoteAction)
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

		if (UnitArray.selected_recno == sprite_recno)
		{
			if ( /*!remote.is_enable() ||*/ nation_recno == NationArray.player_recno || Config.show_ai_info)
				Info.disp();
		}
	}

	public void update_stop_list()
	{
		//TODO rewrite
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

		/*Firm *firm = FirmArray[stop_array[stopId-1].firm_recno];
		
		switch(firm.firm_id)
		{
			case FIRM_MINE:
					//firm.sell_firm(InternalConstants.COMMAND_AUTO);
					//FirmArray[stop_array[0].firm_recno].sell_firm(InternalConstants.COMMAND_AUTO);
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

		if (UnitArray.selected_recno == sprite_recno)
		{
			//if(nation_recno==NationArray.player_recno || Config.show_ai_info)
			//disp_stop(INFO_Y1+54, INFO_UPDATE);
		}
	}

	public bool can_set_stop(int firmRecno)
	{
		Firm firm = FirmArray[firmRecno];

		if (firm.under_construction)
			return false;

		switch (firm.firm_id)
		{
			case Firm.FIRM_MARKET:
				return NationArray[nation_recno].get_relation(firm.nation_recno).trade_treaty;

			case Firm.FIRM_MINE:
			case Firm.FIRM_FACTORY:
				return nation_recno == firm.nation_recno;

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
		if (FirmArray.IsDeleted(action_para))
		{
			hit_points = 0; // caravan also die if the market is deleted
			UnitArray.disappear_in_firm(sprite_recno); // caravan also die if the market is deleted
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
		Location loc = World.get_loc(xLoc, yLoc);
		Firm firm;

		if (loc.can_move(mobile_type))
			init_sprite(xLoc, yLoc); // appear in the location the unit disappeared before
		else
		{
			//---- the entering location is blocked, select another location to leave ----//
			firm = FirmArray[action_para];

			if (appear_in_firm_surround(ref xLoc, ref yLoc, ref firm))
			{
				init_sprite(xLoc, yLoc);
				stop();
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
			journey_status = (nextStopId == 0) ? NO_STOP_DEFINED : SURROUND_FIRM;
			return; // no stop or only one stop is valid
		}

		dest_stop_id = nextStopId;
		firm = FirmArray[stop_array[dest_stop_id - 1].firm_recno];

		action_para = 0; // since action_para is used to store the current market recno, reset before searching
		move_to_firm_surround(firm.loc_x1, firm.loc_y1, sprite_info.loc_width, sprite_info.loc_height, firm.firm_id);

		journey_status = ON_WAY_TO_FIRM;
	}

	public void caravan_on_way()
	{
		//TODO rewrite
	}

	public override void pre_process()
	{
		//TODO rewrite
	}

	public void copy_route(int copyUnitRecno, int remoteAction)
	{
		//TODO rewrite
	}

	//------- ai functions --------//

	public override void process_ai()
	{
		//-- Think about removing stops whose owner nation is at war with us. --//

		if (Info.TotalDays % 30 == sprite_recno % 30)
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
		//TODO rewrite
		return 0;
	}

	public bool think_del_stop()
	{
		//TODO rewrite
		return false;
	}

	public void think_set_pick_up_type()
	{
		if (!is_visible()) // cannot change pickup type if the caravan is inside a market place.
			return;

		if (stop_defined_num < 2)
			return;

		//------------------------------------------//

		Firm firm1 = FirmArray[stop_array[0].firm_recno];
		Firm firm2 = FirmArray[stop_array[1].firm_recno];

		// only when both firms are markets
		if (firm1.firm_id != Firm.FIRM_MARKET || firm2.firm_id != Firm.FIRM_MARKET)
			return;

		// only when the market is our own, we can use it as a TO market
		if (firm2.nation_recno == nation_recno && ((FirmMarket)firm2).is_retail_market())
		{
			think_set_pick_up_type2(1, 2);
		}

		if (firm1.nation_recno == nation_recno && ((FirmMarket)firm1).is_retail_market())
		{
			think_set_pick_up_type2(2, 1);
		}
	}

	public void think_set_pick_up_type2(int fromStopId, int toStopId)
	{
		//TODO rewrite
	}

	private int get_next_stop_id(int curStopId = MAX_STOP_FOR_CARAVAN)
	{
		//TODO rewrite
		return 0;
	}

	//-------- for mine ----------//
	private void mine_load_goods(char pickUpType)
	{
		//TODO rewrite
	}

	//-------- for factory ---------//
	private void factory_unload_goods()
	{
		//TODO rewrite
	}

	private void factory_load_goods(char pickupType)
	{
		//TODO rewrite
	}

	//-------- for market ---------//
	private void market_unload_goods()
	{
		//TODO rewrite
	}

	private int market_unload_goods_in_empty_slot(FirmMarket curMarket, int position)
	{
		//TODO rewrite
		return 0;
	}

	private void market_load_goods()
	{
		//TODO rewrite
	}

	private void market_auto_load_goods()
	{
		//TODO rewrite
	}

	private void market_load_goods_now(MarketGoods marketGoods, float loadQty)
	{
	}

	// select a suitable location to leave the stop
	private bool appear_in_firm_surround(ref int xLoc, ref int yLoc, ref Firm firm)
	{
		return false;
	}
}