using System;
using System.Collections.Generic;
using System.Linq;

namespace TenKingdoms;

public class FirmHarbor : Firm
{
	public int land_region_id;
	public int sea_region_id;

	// Number of units enqueued when holding shift - ensure this is less than MAX_BUILD_SHIP_QUEUE
	public const int HARBOR_BUILD_BATCH_COUNT = 5;
	public List<int> ship_recno_array = new List<int>(GameConstants.MAX_SHIP_IN_HARBOR);

	public int build_unit_id; // race id. of the unit the town is currently building, 0-if currently not building any
	public long start_build_frame_no;

	public Queue<int> build_queue = new Queue<int>();

	//----------- for harbor trading ------------//

	public bool link_checked; // similar to ai_link_checked, but this parameter can be used for players' nation
	public List<int> linked_mine_array = new List<int>();
	public List<int> linked_factory_array = new List<int>();
	public List<int> linked_market_array = new List<int>();

	protected RegionArray RegionArray => Sys.Instance.RegionArray;

	public FirmHarbor()
	{
	}

	public override void init(int nationRecno, int firmId, int xLoc, int yLoc, string buildCode = "", int builderRecno = 0)
	{
		// ignore raceId and find north, south, west or east harbor

		if (World.GetLoc(xLoc + 1, yLoc + 2).CanBuildHarbor(1))
		{
			// check north harbour
			base.init(nationRecno, firmId, xLoc, yLoc, "N", builderRecno);
			land_region_id = World.GetLoc(xLoc + 1, yLoc + 2).RegionId;
			sea_region_id = World.GetLoc(xLoc + 1, yLoc).RegionId;
		}
		else if (World.GetLoc(xLoc + 1, yLoc).CanBuildHarbor(1))
		{
			// check south harbour
			base.init(nationRecno, firmId, xLoc, yLoc, "S", builderRecno);
			land_region_id = World.GetLoc(xLoc + 1, yLoc).RegionId;
			sea_region_id = World.GetLoc(xLoc + 1, yLoc + 2).RegionId;
		}
		else if (World.GetLoc(xLoc + 2, yLoc + 1).CanBuildHarbor(1))
		{
			// check west harbour
			base.init(nationRecno, firmId, xLoc, yLoc, "W", builderRecno);
			land_region_id = World.GetLoc(xLoc + 2, yLoc + 1).RegionId;
			sea_region_id = World.GetLoc(xLoc, yLoc + 1).RegionId;
		}
		else if (World.GetLoc(xLoc, yLoc + 1).CanBuildHarbor(1))
		{
			// check east harbour
			base.init(nationRecno, firmId, xLoc, yLoc, "E", builderRecno);
			land_region_id = World.GetLoc(xLoc, yLoc + 1).RegionId;
			sea_region_id = World.GetLoc(xLoc + 2, yLoc + 1).RegionId;
		}

		region_id = land_region_id; // set region_id to land_region_id

		//------- update the harbor count of the regions ------//

		// TODO remove?
		RegionArray.UpdateRegionStat();
	}

	protected override void deinit_derived()
	{
		for (int i = ship_recno_array.Count - 1; i >= 0; i--)
		{
			int shipUnitRecno = ship_recno_array[i];

			//---- mobilize ships in the harbor ----//

			sail_ship(shipUnitRecno, InternalConstants.COMMAND_AUTO);

			//--- if the ship does not sailed out due to no limit sea space, delete it  ---//

			if (ship_recno_array.Count == i)
			{
				del_hosted_ship(shipUnitRecno);
				UnitArray.DeleteUnit(UnitArray[shipUnitRecno]);
			}
		}
	}

	public override void assign_unit(int unitRecno)
	{
		//------- if this is a construction worker -------//

		if (UnitArray[unitRecno].skill.skill_id == Skill.SKILL_CONSTRUCTION)
		{
			set_builder(unitRecno);
			return;
		}

		//---------------------------------------//

		if (ship_recno_array.Count + (build_unit_id > 0 ? 1 : 0) == GameConstants.MAX_SHIP_IN_HARBOR)
			return; // leave a room for the building unit

		add_hosted_ship(unitRecno);

		UnitArray[unitRecno].deinit_sprite();

		UnitMarine unit = (UnitMarine)UnitArray[unitRecno];
		unit.extra_move_in_beach = UnitMarine.NO_EXTRA_MOVE;
		unit.deinit_sprite();

		//------------------------------------------------//

		//TODO drawing
		//if( FirmArray.selected_recno == firm_recno )
		//info.disp();
	}

	public override void next_day()
	{
		base.next_day();

		//------- process building -------//

		if (build_unit_id != 0)
			process_build();
		else
			process_queue();
	}

	public int total_linked_trade_firm()
	{
		return linked_mine_array.Count + linked_factory_array.Count + linked_market_array.Count;
	}

	public override bool is_operating()
	{
		return true;
	}

	public bool can_build_ship()
	{
		return build_unit_id == 0;
	}

	public void build_ship(int unitId, int remoteAction)
	{
		if (ship_recno_array.Count >= GameConstants.MAX_SHIP_IN_HARBOR)
			return;

		Nation nation = NationArray[nation_recno];

		if (nation.cash < UnitRes[unitId].build_cost)
			return;

		nation.add_expense(NationBase.EXPENSE_SHIP, UnitRes[unitId].build_cost);

		build_unit_id = unitId;
		start_build_frame_no = Sys.Instance.FrameNumber;
	}

	public void sail_ship(int unitRecno, int remoteAction)
	{
		//if( !remoteAction && remote.is_enable() )
		//{
		//// packet structure : <firm recno> <browseRecno>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_F_HARBOR_SAIL_SHIP, 2*sizeof(short) );
		//shortPtr[0] = firm_recno;
		//shortPtr[1] = unitRecno;
		//return;
		//}

		//----- get the browse recno of the ship in the harbor's ship_recno_array[] ----//

		int browseRecno = 0;

		for (int i = ship_recno_array.Count - 1; i >= 0; i--)
		{
			if (ship_recno_array[i] == unitRecno)
			{
				browseRecno = i + 1;
				break;
			}
		}

		if (browseRecno == 0)
			return;

		//------------------------------------------------------------//

		Unit unit = UnitArray[unitRecno];

		SpriteInfo spriteInfo = unit.sprite_info;
		int xLoc = loc_x1; // xLoc & yLoc are used for returning results
		int yLoc = loc_y1;

		if (!World.LocateSpace(ref xLoc, ref yLoc, loc_x2, loc_y2,
			    spriteInfo.loc_width, spriteInfo.loc_height, UnitConstants.UNIT_SEA, sea_region_id))
		{
			return;
		}

		unit.init_sprite(xLoc, yLoc);

		del_hosted_ship(ship_recno_array[browseRecno - 1]);

		//-------- selected the ship --------//

		if (FirmArray.selected_recno == firm_recno && nation_recno == NationArray.player_recno)
		{
			Power.reset_selection();
			unit.selected_flag = true;
			UnitArray.selected_recno = unit.sprite_recno;
			UnitArray.selected_count = 1;

			Info.disp();
		}
	}

	public void del_hosted_ship(int delUnitRecno)
	{
		//---- reset the unit_mode of the ship ----//

		UnitArray[delUnitRecno].set_mode(0);

		//-----------------------------------------//

		for (int i = ship_recno_array.Count - 1; i >= 0; i--)
		{
			if (ship_recno_array[i] == delUnitRecno)
			{
				ship_recno_array.RemoveAt(i);
				break;
			}
		}

		//TODO drawing
		//if( firm_recno == FirmArray.selected_recno )
		//put_info(INFO_UPDATE);
	}

	//-------- for harbor trading ----------//

	public int get_linked_mine_num()
	{
		return linked_mine_array.Count;
	}

	public int get_linked_factory_num()
	{
		return linked_factory_array.Count;
	}

	public int get_linked_market_num()
	{
		return linked_market_array.Count;
	}

	public void update_linked_firm_info()
	{
		if (link_checked)
			return; // no need to check again

		linked_mine_array.Clear();
		linked_factory_array.Clear();
		linked_market_array.Clear();

		for (int i = linked_firm_array.Count - 1; i >= 0; i--)
		{
			if (linked_firm_enable_array[i] != InternalConstants.LINK_EE)
				continue;

			Firm firm = FirmArray[linked_firm_array[i]];
			if (!NationArray[nation_recno].get_relation(firm.nation_recno).trade_treaty)
				continue;

			switch (firm.firm_id)
			{
				case FIRM_MINE:
					linked_mine_array.Add(firm.firm_recno);
					break;

				case FIRM_FACTORY:
					linked_factory_array.Add(firm.firm_recno);
					break;

				case FIRM_MARKET:
					linked_market_array.Add(firm.firm_recno);
					break;
			}
		}
	}

	//----------- AI functions -------------//

	public override void process_ai()
	{
		if (Info.TotalDays % 30 == firm_recno % 30)
			think_build_ship();

		/*
		if( info.game_date%90 == firm_recno%90 )
			think_build_firm();

		if( info.game_date%60 == firm_recno%60 )
			think_trade();
		*/
	}

	public void think_build_ship()
	{
		if (build_unit_id != 0) // if it's currently building a ship
			return;

		if (!can_build_ship()) // full, cannot build anymore
			return;

		Nation ownNation = NationArray[nation_recno];

		if (!ownNation.ai_should_spend(50 + ownNation.pref_use_marine / 4))
			return;

		//---------------------------------------------//
		//
		// For Transport, in most cases, an AI will just
		// need one to two.
		//
		// For Caravel and Galleon, the AI will build as many
		// as the harbor can hold if any of the human players
		// has more ships than the AI has.
		//
		//---------------------------------------------//

		int buildId = 0;
		bool rc = false;
		// return the no. of ships of the enemy that is strongest on the sea
		int enemyShipCount = ownNation.max_human_battle_ship_count();
		int aiShipCount = ownNation.ai_ship_array.Count;

		if (UnitRes[UnitConstants.UNIT_GALLEON].get_nation_tech_level(nation_recno) > 0)
		{
			buildId = UnitConstants.UNIT_GALLEON;

			if (ownNation.ai_ship_array.Count < 2 + (ownNation.pref_use_marine > 50 ? 1 : 0))
				rc = true;
			else
				rc = enemyShipCount > aiShipCount;
		}
		else if (UnitRes[UnitConstants.UNIT_CARAVEL].get_nation_tech_level(nation_recno) > 0)
		{
			buildId = UnitConstants.UNIT_CARAVEL;

			if (aiShipCount < 2 + (ownNation.pref_use_marine > 50 ? 1 : 0))
				rc = true;
			else
				rc = enemyShipCount > aiShipCount;
		}
		else
		{
			buildId = UnitConstants.UNIT_TRANSPORT;

			if (aiShipCount < 2)
				rc = true;
		}

		//---------------------------------------------//

		if (rc)
			build_ship(buildId, InternalConstants.COMMAND_AI);
	}

	public void think_build_firm()
	{
		Nation ownNation = NationArray[nation_recno];

		if (ownNation.cash < 2000) // don't build if the cash is too low
			return;

		if (ownNation.true_profit_365days() < (50 - ownNation.pref_use_marine) * 20) //	-1000 to +1000
			return;

		//----- think about building markets ------//

		if (ownNation.pref_trading_tendency >= 60)
		{
			if (ai_build_firm(FIRM_MARKET))
				return;
		}

		//----- think about building camps ------//

		if (ownNation.pref_military_development / 2 +
		    (linked_firm_array.Count + ownNation.ai_ship_array.Count + ship_recno_array.Count) * 10 +
		    ownNation.total_jobless_population * 2 > 150)
		{
			ai_build_firm(FIRM_CAMP);
		}
	}

	public bool ai_build_firm(int firmId)
	{
		if (no_neighbor_space) // if there is no space in the neighbor area for building a new firm.
			return false;

		Nation ownNation = NationArray[nation_recno];

		//--- check whether the AI can build a new firm next this firm ---//

		if (!ownNation.can_ai_build(firmId))
			return false;

		//-- only build one market place next to this mine, check if there is any existing one --//

		//### begin alex 24/9 ###//
		/*FirmMarket* firm;
	
		for(int i=0; i<linked_firm_count; i++)
		{
			err_when(!linked_firm_array[i] || FirmArray.is_deleted(linked_firm_array[i]));
	
			firm = (FirmMarket*) FirmArray[linked_firm_array[i]];
	
			if( firm.firm_id!=firmId )
				continue;
	
			//------ if this market is our own one ------//
	
			if( firm.nation_recno == nation_recno )
				return 0;
		}*/

		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			Firm firm = FirmArray[linked_firm_array[i]];

			if (firm.firm_id != firmId)
				continue;

			//------ if this market is our own one ------//

			if (firm.nation_recno == nation_recno)
				return false;
		}
		//#### end alex 24/9 ####//

		//------ queue building a new market -------//

		int buildXLoc, buildYLoc;

		if (!ownNation.find_best_firm_loc(firmId, loc_x1, loc_y1, out buildXLoc, out buildYLoc))
		{
			no_neighbor_space = true;
			return false;
		}

		ownNation.add_action(buildXLoc, buildYLoc, loc_x1, loc_y1,
			Nation.ACTION_AI_BUILD_FIRM, firmId);

		return true;
	}

	public bool think_trade()
	{
		//---- see if we have any free ship available ----//

		Nation ownNation = NationArray[nation_recno];
		UnitMarine unitMarine = ai_get_free_trade_ship();

		if (unitMarine == null)
			return false;

		//--- if this harbor is not linked to any trade firms, return now ---//

		if (total_linked_trade_firm() == 0)
			return false;

		//----- scan for another harbor to trade with this harbor ----//

		bool hasTradeShip = false;
		FirmHarbor toHarbor = null;
		foreach (Firm firm in FirmArray)
		{
			if (firm.firm_id != FIRM_HARBOR)
				continue;

			FirmHarbor firmHarbor = (FirmHarbor)firm;

			if (firmHarbor.total_linked_trade_firm() == 0)
				continue;

			// if there has been any ships trading between these two harbors yet
			if (!ownNation.has_trade_ship(firm_recno, firmHarbor.firm_recno))
			{
				hasTradeShip = true;
				toHarbor = firmHarbor;
				break;
			}
		}

		if (!hasTradeShip)
			return false;

		//------ try to set up a sea trade now ------//

		unitMarine.set_stop(1, loc_x1, loc_y1, InternalConstants.COMMAND_AI);
		unitMarine.set_stop(2, toHarbor.loc_x1, toHarbor.loc_y1, InternalConstants.COMMAND_AI);

		unitMarine.set_stop_pick_up(1, TradeStop.AUTO_PICK_UP, InternalConstants.COMMAND_AI);
		unitMarine.set_stop_pick_up(2, TradeStop.AUTO_PICK_UP, InternalConstants.COMMAND_AI);

		return true;
	}

	public UnitMarine ai_get_free_trade_ship()
	{
		Nation ownNation = NationArray[nation_recno];

		for (int i = ownNation.ai_ship_array.Count - 1; i >= 0; i--)
		{
			UnitMarine unitMarine = (UnitMarine)UnitArray[ownNation.ai_ship_array[i]];

			//--- if this is a goods carrying ship and it doesn't have a defined trade route ---//

			if (unitMarine.stop_defined_num < 2 && UnitRes[unitMarine.unit_id].carry_goods_capacity > 0)
			{
				return unitMarine;
			}
		}

		return null;
	}

	//--------------------------------------//

	public void cancel_build_unit()
	{
		build_unit_id = 0;

		if (FirmArray.selected_recno == firm_recno)
		{
			//TODO drawing
			//disable_refresh = 1;
			//info.disp();
			//disable_refresh = 0;
		}
	}

	private bool should_show_harbor_info()
	{
		if (should_show_info())
			return true;

		//--- if any of the ships in the harbor has the spies of the player ---//

		for (int i = 0; i < ship_recno_array.Count; i++)
		{
			UnitMarine unitMarine = (UnitMarine)UnitArray[ship_recno_array[i]];

			if (unitMarine.should_show_info())
				return true;
		}

		return false;
	}

	private void add_hosted_ship(int shipRecno)
	{
		ship_recno_array.Add(shipRecno);

		//---- set the unit_mode of the ship ----//

		UnitArray[shipRecno].set_mode(UnitConstants.UNIT_MODE_IN_HARBOR, firm_recno);

		//---------------------------------------//

		//TODO drawing
		//if( firm_recno == FirmArray.selected_recno )
		//put_info(INFO_UPDATE);
	}

	private void process_build()
	{
		int totalBuildDays = UnitRes[build_unit_id].build_days;


		if ((Sys.Instance.FrameNumber - start_build_frame_no) / InternalConstants.FRAMES_PER_DAY >= totalBuildDays)
		{
			Unit unit = UnitArray.AddUnit(build_unit_id, nation_recno);

			add_hosted_ship(unit.sprite_recno);

			if (own_firm())
				SERes.far_sound(center_x, center_y, 1, 'F', firm_id,
					"FINS", 'S', UnitRes[build_unit_id].sprite_id);

			build_unit_id = 0;

			if (FirmArray.selected_recno == firm_recno)
			{
				//TODO drawing
				//disable_refresh = 1;
				//info.disp();
				//disable_refresh = 0;
			}
		}
	}

	public void add_queue(int unitId, int amount = 1)
	{
		if (amount < 0)
			return;

		int queueSpace = GameConstants.MAX_BUILD_SHIP_QUEUE - build_queue.Count - (build_unit_id > 0 ? 1 : 0);
		int enqueueAmount = Math.Min(queueSpace, amount);

		for (int i = 0; i < enqueueAmount; ++i)
			build_queue.Enqueue(unitId);
	}

	public void remove_queue(int unitId, int amount = 1)
	{
		if (amount < 1)
			return;

		List<int> newQueue = build_queue.ToList();
		for (int i = newQueue.Count - 1; i >= 0 && amount > 0; i--)
		{
			if (newQueue[i] == unitId)
			{
				newQueue.RemoveAt(i);
				amount--;
			}
		}

		build_queue.Clear();
		foreach (var item in newQueue)
			build_queue.Enqueue(item);

		// If there were less units of unitId in the queue than were requested to be removed then
		// also cancel build unit
		if (amount > 0 && build_unit_id == unitId)
			cancel_build_unit();
	}

	private void process_queue()
	{
		if (build_queue.Count > 0)
		{
			// remove the queue no matter build_ship success or not

			build_ship(build_queue.Dequeue(), InternalConstants.COMMAND_AUTO);

			if (FirmArray.selected_recno == firm_recno)
			{
				//TODO drawing
				//disable_refresh = 1;
				//info.disp();
				//disable_refresh = 0;
			}
		}
	}

	public override void DrawDetails(IRenderer renderer)
	{
		renderer.DrawHarborDetails(this);
	}

	public override void HandleDetailsInput(IRenderer renderer)
	{
		renderer.HandleHarborDetailsInput(this);
	}
}