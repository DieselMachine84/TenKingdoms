using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class UnitMarine : Unit
{
	public const int NO_EXTRA_MOVE = 0;
	public const int EXTRA_MOVING_IN = 1;
	public const int EXTRA_MOVING_OUT = 2;
	public const int EXTRA_MOVE_FINISH = 3;

	public const int MAX_STOP_FOR_SHIP = 3;

	public Sprite splash;

	public int menu_mode; // goods menu or units menu
	public int extra_move_in_beach;
	public bool in_beach;

	//------ vars for carrying units ------//

	public int selected_unit_id;

	public List<int> unit_recno_array = new List<int>();
	//public char 		unit_count;

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

	//----------- vars for attacking ----------//

	public AttackInfo ship_attack_info;
	public int attack_mode_selected;

	//-------------- vars for AI --------------//

	public DateTime last_load_goods_date;

	public override void init(int unitId, int nationRecno, int rankId, int unitLoyalty, int startX = -1, int startY = -1)
	{
	}

	public override void init_derived()
	{
	}

	public void update_stop_list()
	{
	}

	public int get_next_stop_id(int curStopId)
	{
		return 0;
	}

	public override void pre_process()
	{
	}

	public void set_stop_pick_up(int stopId, int newPickUpType, int remoteAction)
	{
	}

	public void ship_in_firm(int autoMode = 1)
	{
	}

	public void ship_on_way()
	{
	}

	public int appear_in_firm_surround(ref int xLoc, ref int yLoc, Firm firmPtr)
	{
		return 0;
	}

	public void get_harbor_linked_firm_info()
	{
	}

	public void harbor_unload_goods()
	{
	}

	public void harbor_unload_product()
	{
	}

	public void harbor_unload_raw()
	{
	}

	public void harbor_load_goods()
	{
	}

	public void harbor_auto_load_goods()
	{
	}

	public void harbor_load_product(int goodsId, int autoPickUp, int considerMode)
	{
	}

	public void harbor_load_raw(int goodsId, int autoPickUp, int considerMode)
	{
	}

	public int total_carried_goods()
	{
		return 0;
	}

	public void update_abs_pos(SpriteFrame spriteFrame = null)
	{
		//TODO take wave height into account
	}

	public override double actual_damage()
	{
		return 0.0;
	}

	public int wave_height(int phase = 0)
	{
		return 0;
	}

	public bool can_unload_unit()
	{
		return false;
	}

	public void load_unit(int unitRecno)
	{
	}

	public void unload_unit(int unitSeqId, int remoteAction)
	{
	}

	public void unload_all_units(int remoteAction)
	{
	}

	public int unloading_unit(int isAll, int unitSeqId = 0)
	{
		return 0;
	}

	public void del_unit(int unitRecno)
	{
	}

	public int can_set_stop(int firmRecno)
	{
		return 0;
	}

	public void extra_move()
	{
	}

	public override void process_extra_move()
	{
	}

	public void set_stop(int stopId, int stopXLoc, int stopYLoc, int remoteAction)
	{
	}

	public void del_stop(int stopId, int remoteAction)
	{
	}

	public void select_attack_weapon()
	{
	}

	public override bool is_ai_all_stop()
	{
		return false;
	}

	public override bool can_resign()
	{
		return false;
	}

	public override void fix_attack_info() // set attack_info_array appropriately
	{
	}

	public void copy_route(int copyUnitRecno, int remoteAction)
	{
	}

	//------- ai functions --------//

	public override void process_ai()
	{
	}

	public int think_resign()
	{
		return 0;
	}

	public void think_del_stop()
	{
	}

	public void ai_sail_to_nearby_harbor()
	{
	}

	public void ai_ship_being_attacked(int attackerUnitRecno)
	{
	}

	public int is_on_coast()
	{
		return 0;
	}

	public bool should_show_info()
	{
		return false;
	}
}