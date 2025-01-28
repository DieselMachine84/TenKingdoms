using System;
using System.Collections.Generic;

namespace TenKingdoms;

public abstract class Firm
{
	public const int MAX_FIRM_TYPE = 10;
	public const int FIRM_BASE = 1;
	public const int FIRM_FACTORY = 2;
	public const int FIRM_INN = 3;
	public const int FIRM_MARKET = 4;
	public const int FIRM_CAMP = 5;
	public const int FIRM_MINE = 6;
	public const int FIRM_RESEARCH = 7;
	public const int FIRM_WAR_FACTORY = 8;
	public const int FIRM_HARBOR = 9;
	public const int FIRM_MONSTER = 10;

	public const int MAX_WORKER = 8;
	public const int MAX_CARGO = 9;
	public const int MIN_MIGRATE_ATTRACT_LEVEL = 30;

	public const int FIRM_WITHOUT_ACTION = 0;
	public const int FACTORY_RELOCATE = 1;
	public const int MARKET_FOR_SELL = 2;
	public const int CAMP_IN_DEFENSE = 3;

	public int firm_id; // Firm ID, meanings are defined in OFIRMID.H
	public int firm_build_id;
	public int firm_recno; // record no. of this firm in the firm_array
	public bool firm_ai; // whether Computer AI control this firm or not

	// some ai actions are processed once only in the processing day. To prevent multiple checking in the processing day
	public bool ai_processed;
	public int ai_status;

	// AI checks firms and towns location by links, disable checking by setting this parameter to 1
	public bool ai_link_checked;

	public bool ai_sell_flag; // this is true if the AI has queued the command to sell this firm

	public int race_id;
	public int nation_recno; // this firm's parent company nation

	//-------- firm name vars ---------//

	public int closest_town_name_id; // name id. of the town that is closest to this firm when it is built

	public int firm_name_instance_id;

	//--------- display info ----------//

	public int loc_x1, loc_y1, loc_x2, loc_y2;
	public int center_x, center_y;
	public int region_id;

	public int cur_frame; // current animation frame id.
	public int remain_frame_delay;
	private bool remove_firm;

	//---------- game vars ------------//

	public double hit_points;
	public double max_hit_points;
	public bool under_construction; // whether the firm is under construction

	public int firm_skill_id;
	public int overseer_recno;
	public int overseer_town_recno;
	public int builder_recno; // the recno of the builder
	public int builder_region_id; // the original region no. of builder
	public double productivity;

	public List<Worker> workers = new List<Worker>();
	public int selected_worker_id;

	public int player_spy_count;
	public int sabotage_level; // 0-100 for counter productivity

	//------ inter-relationship -------//

	public List<int> linked_firm_array = new List<int>();
	public List<int> linked_town_array = new List<int>();

	public List<int> linked_firm_enable_array = new List<int>();
	public List<int> linked_town_enable_array = new List<int>();

	//--------- financial vars ---------//

	public double last_year_income;
	public double cur_year_income;

	//---------------------------------//

	public DateTime setup_date; // the date which this firm is setup

	public bool should_set_power;
	public DateTime last_attacked_date; // the date when the firm was last being attacked.

	//----------- AI vars ------------//

	public bool should_close_flag;
	public bool no_neighbor_space; // no space to build firms/towns next to this town
	public int ai_should_build_factory_count;

	//--------- static vars ----------//

	public static int firm_menu_mode;

	// recno of the spy that is doing the bribing or viewing secret reports of other nations
	public static int action_spy_recno;

	public static int bribe_result;
	public static int assassinate_result;

	protected TownRes TownRes => Sys.Instance.TownRes;
	protected FirmRes FirmRes => Sys.Instance.FirmRes;
	protected RaceRes RaceRes => Sys.Instance.RaceRes;
	protected SpriteRes SpriteRes => Sys.Instance.SpriteRes;
	protected TalkRes TalkRes => Sys.Instance.TalkRes;
	protected UnitRes UnitRes => Sys.Instance.UnitRes;
	protected SERes SERes => Sys.Instance.SERes;
	protected Config Config => Sys.Instance.Config;
	protected ConfigAdv ConfigAdv => Sys.Instance.ConfigAdv;
	protected Info Info => Sys.Instance.Info;
	protected Power Power => Sys.Instance.Power;
	protected World World => Sys.Instance.World;
	protected NationArray NationArray => Sys.Instance.NationArray;
	protected UnitArray UnitArray => Sys.Instance.UnitArray;
	protected RebelArray RebelArray => Sys.Instance.RebelArray;
	protected SpyArray SpyArray => Sys.Instance.SpyArray;
	protected FirmArray FirmArray => Sys.Instance.FirmArray;
	protected FirmDieArray FirmDieArray => Sys.Instance.FirmDieArray;
	protected TownArray TownArray => Sys.Instance.TownArray;
	protected NewsArray NewsArray => Sys.Instance.NewsArray;

	public Firm()
	{
	}

	public virtual void init(int xLoc, int yLoc, int nationRecno, int firmId,
		string buildCode = "", int builderRecno = 0)
	{
		FirmInfo firmInfo = FirmRes[firmId];

		firm_id = firmId;

		if (!String.IsNullOrEmpty(buildCode))
			firm_build_id = firmInfo.get_build_id(buildCode);
		else
			firm_build_id = firmInfo.first_build_id;

		//----------- set vars -------------//

		nation_recno = nationRecno;
		setup_date = Info.game_date;

		overseer_recno = 0;

		//----- set the firm's absolute positions on the map -----//

		FirmBuild firmBuild = FirmRes.get_build(firm_build_id);

		race_id = firmBuild.race_id;

		loc_x1 = xLoc;
		loc_y1 = yLoc;
		loc_x2 = loc_x1 + firmBuild.loc_width - 1;
		loc_y2 = loc_y1 + firmBuild.loc_height - 1;

		center_x = (loc_x1 + loc_x2) / 2;
		center_y = (loc_y1 + loc_y2) / 2;

		region_id = World.get_region_id(center_x, center_y);

		//--------- set animation frame vars ---------//

		if (firmBuild.animate_full_size)
			cur_frame = 1;
		else
			cur_frame = 2; // start with the 2nd frame as the 1st frame is the common frame

		remain_frame_delay = firmBuild.frame_delay(cur_frame);

		//--------- initialize gaming vars ----------//

		hit_points = 0.0;
		max_hit_points = firmInfo.max_hit_points;

		//------ set construction and builder -------//

		under_construction =
			firmInfo.buildable; // whether the firm is under construction, if the firm is not buildable it is completed in the first place

		if (!under_construction) // if this firm doesn't been to be constructed, set its hit points to the maximum
			hit_points = max_hit_points;

		if (builderRecno != 0)
			set_builder(builderRecno);
		else
			builder_recno = 0;

		//------ update firm counter -------//

		firmInfo.total_firm_count++;

		if (nation_recno != 0)
			firmInfo.inc_nation_firm_count(nation_recno);

		//-------------------------------------------//

		if (nation_recno > 0)
		{
			Nation nation = NationArray[nation_recno];

			firm_ai = nation.is_ai();
			ai_processed = true;

			//--------- increase firm counter -----------//

			nation.nation_firm_count++;

			//-------- update last build date ------------//

			nation.last_build_firm_date = Info.game_date;
		}
		else
		{
			firm_ai = false;
			ai_processed = false;
		}

		ai_status = FIRM_WITHOUT_ACTION;
		ai_link_checked = true; // check the connected firms if ai_link_checked = 0;

		//--------------------------------------------//

		setup_link();

		set_world_matrix();

		init_name();

		//----------- init AI -----------//

		if (firm_ai)
			NationArray[nation_recno].add_firm_info(firm_id, firm_recno);

		//-------- init derived ---------//

		init_derived(); // init_derived() before set_world_matrix() so that init_derived has access to the original land info.
	}

	protected virtual void init_derived()
	{
	}

	public virtual void deinit()
	{
		if (firm_recno == 0) // already deleted
			return;

		deinit_derived();

		remove_firm = true; // set static parameter

		//------- delete AI info ----------//

		if (firm_ai)
		{
			Nation nation = NationArray[nation_recno];

			if (should_close_flag)
				nation.firm_should_close_array[firm_id - 1]--;

			nation.del_firm_info(firm_id, firm_recno);
		}

		//--------- clean up related stuff -----------//

		restore_world_matrix();
		release_link();

		//------ all workers and the overseer resign ------//

		if (!under_construction)
		{
			// -------- create a firm die record ------//
			// can be called as soon as restore_world_matrix
			FirmDieArray.Add(this);
		}

		// this function must be called before restore_world_matrix(), otherwise the power area can't be completely reset
		assign_overseer(0);

		resign_all_worker(); // the workers in the firm will be killed if there is no space for creating the workers

		if (builder_recno != 0)
			mobilize_builder(builder_recno);

		//--------- decrease firm counter -----------//

		if (nation_recno != 0)
			NationArray[nation_recno].nation_firm_count--;

		//------ update firm counter -------//

		FirmInfo firmInfo = FirmRes[firm_id];

		firmInfo.total_firm_count--;

		if (nation_recno != 0)
			firmInfo.dec_nation_firm_count(nation_recno);

		//------- update town border ---------//

		loc_x1 = -1; // mark deleted

		//------- if the current firm is the selected -----//

		if (FirmArray.selected_recno == firm_recno)
		{
			FirmArray.selected_recno = 0;
			//TODO drawing
			//info.disp();
		}

		//-------------------------------------------------//

		//TODO check
		//firm_recno = 0;
		remove_firm = false; // reset static parameter
	}

	protected virtual void deinit_derived()
	{
	}

	public void init_name()
	{
		// if this firm does not have any short name, display the full name without displaying the town name together
		if (String.IsNullOrEmpty(FirmRes[firm_id].short_name))
			return;

		//---- find the closest town and set closest_town_name_id -----//

		closest_town_name_id = get_closest_town_name_id();

		//--------- set firm_name_instance_id -----------//

		bool[] usedInstanceArray = new bool[256];

		foreach (Firm firm in FirmArray)
		{
			if (firm.firm_id == firm_id &&
			    firm.closest_town_name_id == closest_town_name_id &&
			    firm.firm_name_instance_id != 0)
			{
				usedInstanceArray[firm.firm_name_instance_id - 1] = true;
			}
		}

		for (int i = 0; i < usedInstanceArray.Length; i++) // get the smallest id. which are not used by existing firms
		{
			if (!usedInstanceArray[i])
			{
				firm_name_instance_id = i + 1;
				break;
			}
		}
	}

	public virtual string firm_name()
	{
		string str = String.Empty;

		if (closest_town_name_id == 0)
		{
			str = FirmRes[firm_id].name;
		}
		else
		{
			// display number when there are multiple linked firms of the same type
			// TRANSLATORS: <Town> <Short Firm Name> <Firm #>
			// This is the name of the firm when there are multiple linked firms to a town.
			str = TownRes.get_name(closest_town_name_id) + " " + FirmRes[firm_id].short_name;
			if (firm_name_instance_id > 1)
			{
				str += " " + firm_name_instance_id;
			}
		}

		return str;
	}

	public int get_closest_town_name_id()
	{
		//---- find the closest town and set closest_town_name_id -----//

		int townDistance, minTownDistance = Int32.MaxValue;
		int closestTownNameId = 0;
		foreach (Town town in TownArray)
		{
			townDistance = Misc.points_distance(town.center_x, town.center_y, center_x, center_y);

			if (townDistance < minTownDistance)
			{
				minTownDistance = townDistance;
				closestTownNameId = town.town_name_id;
			}
		}

		return closestTownNameId;
	}

	public int majority_race() // the race that has the majority of the population
	{
		//--- if there is a overseer, return the overseer's race ---//

		if (overseer_recno != 0)
			return UnitArray[overseer_recno].race_id;

		if (workers.Count == 0)
			return 0;

		//----- count the no. people in each race ------//

		int[] raceCountArray = new int[GameConstants.MAX_RACE];

		foreach (Worker worker in workers)
		{
			if (worker.race_id != 0)
				raceCountArray[worker.race_id - 1]++;
		}

		//---------------------------------------------//

		int mostRaceCount = 0, mostRaceId = 0;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (raceCountArray[i] > mostRaceCount)
			{
				mostRaceCount = raceCountArray[i];
				mostRaceId = i + 1;
			}
		}

		return mostRaceId;
	}

	public bool own_firm() // whether the firm is controlled by the current player
	{
		return nation_recno == NationArray.player_recno;
	}

	public bool can_sell()
	{
		return hit_points >= max_hit_points * GameConstants.CAN_SELL_HIT_POINTS_PERCENT / 100.0;
	}

	public int average_worker_skill()
	{
		if (workers.Count == 0)
			return 0;

		//------- calculate the productivity of the workers -----------//

		int totalSkill = 0;

		foreach (Worker worker in workers)
		{
			totalSkill += worker.skill_level;
		}

		//----- include skill in the calculation ------//

		return totalSkill / workers.Count;
	}

	public virtual bool is_operating()
	{
		return productivity > 0;
	}

	public double income_365days()
	{
		return last_year_income * (365 - Info.year_day) / 365 + cur_year_income;
	}

	public int year_expense()
	{
		int totalExpense = FirmRes[firm_id].year_cost;

		//---- pay salary to workers from foreign towns ----//

		if (FirmRes[firm_id].live_in_town)
		{
			int payWorkerCount = 0;

			foreach (Worker worker in workers)
			{
				if (TownArray[worker.town_recno].nation_recno != nation_recno)
					payWorkerCount++;
			}

			totalExpense += GameConstants.WORKER_YEAR_SALARY * payWorkerCount;
		}

		return totalExpense;
	}

	public virtual void assign_unit(int unitRecno)
	{
		Unit unit = UnitArray[unitRecno];

		//------- if this is a construction worker -------//

		if (unit.skill.skill_id == Skill.SKILL_CONSTRUCTION)
		{
			set_builder(unitRecno);
			return;
		}

		//---- if the unit does not belong to the firm's nation ----//

		if (unit.nation_recno != nation_recno)
		{
			// can no longer capture a firm with a normal unit - must use spy  

			//----- capture this firm if there is nobody here -----//
/*
		if( worker_array && worker_count==0 && overseer_recno==0 &&		// if the firm is empty, assign to take over the firm
			 unit.skill.skill_id == firm_skill_id )						// the takeover unit must have the skill of this firm
		{
			change_nation(unit.nation_recno);
		}
		else
*/
			return; // if cannot capture, the nations are not the same, return now. This will happen if the unit's nation was changed during his moving to the firm.
		}

		//-- if there isn't any overseer in this firm or this unit's skill is higher than the current overseer's skill --//

		//### begin alex 18/10 ###//
		unit.group_select_id = 0; // clear group select id
		//#### end alex 18/10 ####//

		FirmInfo firmInfo = FirmRes[firm_id];

		if (firmInfo.need_overseer && (overseer_recno == 0 ||
		                               (unit.skill.skill_id == firm_skill_id &&
		                                UnitArray[overseer_recno].skill.skill_id != firm_skill_id) ||
		                               (unit.skill.skill_id == firm_skill_id && unit.skill.skill_level >
			                               UnitArray[overseer_recno].skill.skill_level)
		    ))
		{
			assign_overseer(unitRecno);
		}
		else if (firmInfo.need_worker)
		{
			assign_worker(unitRecno);
			sort_worker();
		}
	}

	public virtual void assign_overseer(int newOverseerRecno)
	{
		if (!FirmRes[firm_id].need_overseer)
			return;

		if (newOverseerRecno == 0 && overseer_recno == 0)
			return;

		//--- if the new overseer's nation is not the same as the firm's nation, don't assign ---//

		if (newOverseerRecno != 0 && UnitArray[newOverseerRecno].nation_recno != nation_recno)
			return;

		//------------------------------------------//

		int oldOverseerRecno = overseer_recno;

		if (newOverseerRecno == 0)
		{
			//------------------------------------------------------------------------------------------------//
			// the old overseer may be kept in firm or killed if remove_firm is true
			//------------------------------------------------------------------------------------------------//
			Unit oldUnit = UnitArray[overseer_recno];
			SpriteInfo spriteInfo = SpriteRes[UnitRes[oldUnit.unit_id].sprite_id];
			int xLoc = loc_x1;
			int yLoc = loc_y1;

			if (!locate_space(remove_firm, ref xLoc, ref yLoc, loc_x2, loc_y2, spriteInfo.loc_width,
				    spriteInfo.loc_height))
			{
				if (remove_firm)
					kill_overseer();
			}
			else
			{
				//------ there should be space for creating the overseer -----//

				mobilize_overseer();
				/*
				//-- if the overseer is resigned without successor, mobilize a worker as overseer --//
				if(!newOverseerRecno && worker_array)
				{
					int bestWorkerId = best_worker_id();      // find the most skilled worker
					if( bestWorkerId )
						newOverseerRecno = mobilize_worker(bestWorkerId,1);
				}
				*/
			}
		}
		else
		{
			//----------- there should be space for creating the overseer ---------//
			Unit newOverseer = UnitArray[newOverseerRecno];

			int originalXLoc = newOverseer.next_x_loc();
			int originalYLoc = newOverseer.next_y_loc();

			newOverseer.deinit_sprite();

			//----------------------------------------------------------------------------------------//
			// There should be at least one location (occupied by the new overseer) for creating the old
			//	overseer.
			//
			// 1) If a town is already created, the new overseer settle down there, free its space for
			//		creating the new overseer.
			// 2) If the overseer and the workers live in the firm, no town will be created.  Thus, the
			//		space occupied by the old overseer is free for creating the new overseer.
			// 3) If the overseer and the workers need live in town, and a town is created.  i.e. there
			//		is no overseer or worker in the firm, so just assign the new overseer in the firm
			//----------------------------------------------------------------------------------------//

			if (overseer_recno == 0 && workers.Count == 0)
			{
				//------------------------------------------------------------------------------------------------//
				// the firm is empty
				//------------------------------------------------------------------------------------------------//
				if (FirmRes[firm_id].live_in_town)
				{
					// the overseer settles down
					overseer_town_recno = assign_settle(newOverseer.race_id, newOverseer.loyalty, 1);
					if (overseer_town_recno == 0)
						return; // no space for creating the town, just return without assigning
				}

				//------- set the unit to overseer mode and deinit the sprite ------//
				overseer_recno = newOverseerRecno;
				Unit overseer = UnitArray[overseer_recno];
				overseer.set_mode(UnitConstants.UNIT_MODE_OVERSEE, firm_recno);
				overseer.deinit_sprite(); // hide the unit from the world map

				//--------- if the unit is a spy -----------//

				if (overseer.spy_recno != 0)
					SpyArray[overseer.spy_recno].set_place(Spy.SPY_FIRM, firm_recno);
				/*
				//------ capture the firm if the overseer is from another nation ---//
				if(UnitArray[overseer_recno].nation_recno != nation_recno)
					change_nation(UnitArray[overseer_recno].nation_recno);
				*/
			}
			else
			{
				//------------------------------------------------------------------------------------------------//
				// a town should exist if the overseer need live in town
				//------------------------------------------------------------------------------------------------//
				if (FirmRes[firm_id].live_in_town)
				{
					// the overseer settles down
					overseer_town_recno = assign_settle(newOverseer.race_id, newOverseer.loyalty, 1);

					if (overseer_town_recno == 0)
						return; // reach MAX population and no space to create town, return without assigning
				}

				//Unit *unit = UnitArray[newOverseerRecno];
				//TODO check as deinit_sprite was already called
				//unit.deinit_sprite();

				if (overseer_recno != 0)
					mobilize_overseer();

				overseer_recno = newOverseerRecno;
				Unit overseer = UnitArray[overseer_recno];
				overseer.set_mode(UnitConstants.UNIT_MODE_OVERSEE, firm_recno);

				//--------- if the unit is a spy -----------//

				if (overseer.spy_recno != 0)
					SpyArray[overseer.spy_recno].set_place(Spy.SPY_FIRM, firm_recno);
				/*
				//------ capture the firm if the overseer is from another nation ---//
				if(UnitArray[overseer_recno].nation_recno != nation_recno)
					change_nation(UnitArray[overseer_recno].nation_recno);
				*/
			}
		}

		//------- update loyalty -------//

		if (newOverseerRecno != 0 && !UnitArray.IsDeleted(newOverseerRecno))
			UnitArray[newOverseerRecno].update_loyalty();

		//----------- refresh display if this firm is selected ----------//

		//TODO drawing
		//if (FirmArray.selected_recno == firm_recno)
		//info.disp();
	}

	public virtual void assign_worker(int workerUnitRecno)
	{
		//-- if the unit is a spy, only allow assign when there is room in the firm --//

		Unit unit = UnitArray[workerUnitRecno];

		if (unit.true_nation_recno() != nation_recno && workers.Count == MAX_WORKER)
			return;

		//---- if all worker space are full, resign the worst worker to release one worker space for the overseer ----//

		int unitXLoc = -1;
		int unitYLoc = -1;

		if (workers.Count == MAX_WORKER)
		{
			int minWorkerSkill = Int32.MaxValue;
			Worker worstWorker = workers[0];

			foreach (Worker worker in workers)
			{
				int workerSkill = worker.skill_level;

				if (workerSkill < minWorkerSkill)
				{
					minWorkerSkill = workerSkill;
					worstWorker = worker;
				}
			}

			// save the location for later init_sprite() if the assign settle action failed
			unitXLoc = unit.next_x_loc();
			unitYLoc = unit.next_y_loc();

			unit.deinit_sprite(); // free the location for creating the worst unit

			resign_worker(worstWorker);
		}

		//---------- there is room for the new worker ------------//

		Worker newWorker = new Worker();

		if (FirmRes[firm_id].live_in_town)
		{
			newWorker.town_recno = assign_settle(unit.race_id, unit.loyalty, 0); // the worker settles down

			if (newWorker.town_recno == 0)
			{
				//--- the unit was deinit_sprite(), and now the assign settle action failed, we need to init_sprite() to restore it ---//

				if (unitXLoc >= 0 && !unit.is_visible())
					unit.init_sprite(unitXLoc, unitYLoc);

				return;
			}
		}
		else
		{
			newWorker.town_recno = 0;
			newWorker.worker_loyalty = unit.loyalty;
		}

		//------- add the worker to the firm -------//

		workers.Add(newWorker);

		newWorker.name_id = unit.name_id;
		newWorker.race_id = unit.race_id;
		newWorker.unit_id = unit.unit_id;
		newWorker.rank_id = unit.rank_id;

		newWorker.skill_id = firm_skill_id;
		newWorker.skill_level = unit.skill.get_skill(firm_skill_id);

		if (newWorker.skill_level == 0 && newWorker.race_id != 0)
			newWorker.skill_level = GameConstants.CITIZEN_SKILL_LEVEL;

		newWorker.combat_level = unit.skill.combat_level;
		newWorker.hit_points = (int)unit.hit_points;

		if (newWorker.hit_points == 0) // 0.? will become 0 in (float) to (int) conversion
			newWorker.hit_points = 1;

		if (UnitRes[unit.unit_id].unit_class == UnitConstants.UNIT_CLASS_WEAPON)
		{
			newWorker.extra_para = unit.get_weapon_version();
		}
		else if (unit.race_id != 0)
		{
			newWorker.extra_para = unit.cur_power;
		}
		else
		{
			newWorker.extra_para = 0;
		}

		newWorker.init_potential();

		//------ if the recruited worker is a spy -----//

		if (unit.spy_recno != 0)
		{
			SpyArray[unit.spy_recno].set_place(Spy.SPY_FIRM, firm_recno);

			newWorker.spy_recno = unit.spy_recno;
			unit.spy_recno = 0; // reset it now so Unit::deinit() won't delete the Spy in SpyArray
		}

		//--------- the unit disappear in firm -----//

		if (!FirmRes[firm_id].live_in_town) // if the unit does not live in town, increase the unit count now
			UnitRes[unit.unit_id].inc_nation_unit_count(nation_recno);

		UnitArray.disappear_in_firm(workerUnitRecno);
	}

	public void kill_overseer()
	{
		if (overseer_recno == 0)
			return;

		//-------- if the overseer is a spy -------//

		Unit overseer = UnitArray[overseer_recno];

		if (overseer.spy_recno != 0)
			SpyArray[overseer.spy_recno].set_place(Spy.SPY_UNDEFINED, 0);

		//-- no need to del the spy here, UnitArray.del() will del the spy --//

		//-----------------------------------------//

		if (overseer_town_recno != 0)
			TownArray[overseer_town_recno].dec_pop(UnitArray[overseer_recno].race_id, true);

		UnitArray.DeleteUnit(overseer);

		overseer_recno = 0;
	}

	public void kill_all_worker()
	{
		while (workers.Count > 0)
		{
			kill_worker(workers[0]);
		}
	}

	public void kill_worker(Worker worker)
	{
		//------- decrease worker no. and create an unit -----//

		if (worker.race_id != 0 && worker.name_id != 0)
			RaceRes[worker.race_id].free_name_id(worker.name_id);

		if (worker.town_recno != 0) // town_recno is 0 if the workers in the firm do not live in towns
			TownArray[worker.town_recno].dec_pop(worker.race_id, true); // 1-has job

		//-------- if this worker is a spy ---------//

		if (worker.spy_recno != 0)
		{
			Spy spy = SpyArray[worker.spy_recno];
			spy.set_place(Spy.SPY_UNDEFINED, 0);
			SpyArray.DeleteSpy(spy);
		}

		//--- decrease the nation unit count as the Unit has already increased it ----//

		if (!FirmRes[firm_id].live_in_town) // if the unit does not live in town, increase the unit count now
			UnitRes[worker.unit_id].dec_nation_unit_count(nation_recno);

		//------- delete the record from the worker_array ------//

		workers.Remove(worker);

		//TODO rewrite it
		//if( selected_worker_id > workerId || selected_worker_id == worker_count )
		//selected_worker_id--;

		if (workers.Count == 0)
			selected_worker_id = 0;
	}

	public void kill_builder(int builderRecno)
	{
		UnitArray.DeleteUnit(UnitArray[builderRecno]);
	}

	public virtual void being_attacked(int attackerUnitRecno)
	{
		last_attacked_date = Info.game_date;

		if (nation_recno != 0 && firm_ai)
		{
			// this can happen when the unit has just changed nation
			if (UnitArray[attackerUnitRecno].nation_recno == nation_recno)
				return;

			NationArray[nation_recno].ai_defend(attackerUnitRecno);
		}
	}

	public virtual bool pull_town_people(int townRecno, int remoteAction, int raceId = 0, bool forcePull = false)
	{
		// this can happen in a multiplayer game as Town::draw_detect_link_line() still have the old worker_count and thus allow this function being called.
		if (workers.Count == MAX_WORKER)
			return false;

		//if(!remoteAction && remote.is_enable() )
		//{
		//// packet structure : <firm recno> <town recno> <race Id or 0> <force Pull>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_FIRM_PULL_TOWN_PEOPLE, 4*sizeof(short));
		//shortPtr[0] = firm_recno;
		//shortPtr[1] = townRecno;
		//shortPtr[2] = raceId;	
		//// if raceId == 0, let each player choose the race by random number, to sychronize the random number
		//shortPtr[3] = forcePull;
		//return 0;
		//}

		//---- people in the town go to work for the firm ---//

		Town town = TownArray[townRecno];
		int popAdded = 0;

		//---- if doesn't specific a race, randomly pick one ----//

		if (raceId == 0)
			raceId = Misc.Random(GameConstants.MAX_RACE) + 1;

		//----------- scan the races -----------//

		for (int i = 0; i < GameConstants.MAX_RACE; i++) // maximum 8 tries
		{
			//---- see if there is any population of this race to move to the firm ----//

			int recruitableCount = town.recruitable_race_pop(raceId, true); // 1-allow recruiting spies

			if (recruitableCount > 0)
			{
				//----- if the unit is forced to move to the firm ---//

				if (forcePull) // right-click to force pulling a worker from the village
				{
					if (town.race_loyalty_array[raceId - 1] <
					    GameConstants.MIN_RECRUIT_LOYALTY + town.recruit_dec_loyalty(raceId, false))
						return false;

					town.recruit_dec_loyalty(raceId);
				}
				else //--- see if the unit will voluntarily move to the firm ---//
				{
					//--- the higher the loyalty is, the higher the chance of working for the firm ---//

					if (town.nation_recno != 0)
					{
						if (Misc.Random((100 - Convert.ToInt32(town.race_loyalty_array[raceId - 1])) / 10) > 0)
							return false;
					}
					else
					{
						if (Misc.Random(Convert.ToInt32(town.race_resistance_array[raceId - 1, nation_recno - 1]) / 10) > 0)
							return false;
					}
				}

				//----- get the chance of getting people to your command base is higher when the loyalty is higher ----//

				if (FirmRes[firm_id].live_in_town)
				{
					town.jobless_race_pop_array[raceId - 1]--; // decrease the town's population
					town.jobless_population--;
				}
				else
				{
					town.dec_pop(raceId, false);
				}

				//------- add the worker to the firm -----//

				Worker worker = new Worker();
				workers.Add(worker);

				worker.race_id = raceId;
				worker.rank_id = Unit.RANK_SOLDIER;
				worker.unit_id = RaceRes[raceId].basic_unit_id;
				worker.worker_loyalty = Convert.ToInt32(town.race_loyalty_array[raceId - 1]);

				if (FirmRes[firm_id].live_in_town)
					worker.town_recno = townRecno;

				worker.combat_level = GameConstants.CITIZEN_COMBAT_LEVEL;
				worker.hit_points = GameConstants.CITIZEN_HIT_POINTS;

				worker.skill_id = firm_skill_id;
				worker.skill_level = GameConstants.CITIZEN_SKILL_LEVEL;

				worker.init_potential();

				//--------- if this is a military camp ---------//
				//
				// Increase armed unit count of the race of the worker assigned,
				// as when a unit is assigned to a camp, Unit::deinit() will decrease
				// the counter, so we need to increase it back here.
				//
				//---------------------------------------------------//

				if (!FirmRes[firm_id].live_in_town)
					UnitRes[worker.unit_id].inc_nation_unit_count(nation_recno);

				//------ if the recruited worker is a spy -----//

				int spyCount = town.race_spy_count_array[raceId - 1];

				if (spyCount >= Misc.Random(recruitableCount) + 1)
				{
					// the 3rd parameter is which spy to recruit
					int spyRecno = SpyArray.find_town_spy(townRecno, raceId, Misc.Random(spyCount) + 1);

					worker.spy_recno = spyRecno;

					SpyArray[spyRecno].set_place(Spy.SPY_FIRM, firm_recno);
				}

				return true;
			}

			if (++raceId > GameConstants.MAX_RACE)
				raceId = 1;
		}

		return false;
	}

	public void resign_overseer()
	{
		assign_overseer(0);
	}

	public void set_world_matrix()
	{
		//--- if a nation set up a firm in a location that the player has explored, contact between the nation and the player is established ---//

		for (int yLoc = loc_y1; yLoc <= loc_y2; yLoc++)
		{
			for (int xLoc = loc_x1; xLoc <= loc_x2; xLoc++)
			{
				World.get_loc(xLoc, yLoc).set_firm(firm_recno);
			}
		}

		//--- if a nation set up a town in a location that the player has explored, contact between the nation and the player is established ---//

		establish_contact_with_player();

		//------------ reveal new land ----------//

		if (nation_recno == NationArray.player_recno ||
		    (nation_recno != 0 && NationArray[nation_recno].is_allied_with_player))
		{
			World.unveil(loc_x1, loc_y1, loc_x2, loc_y2);
			World.visit(loc_x1, loc_y1, loc_x2, loc_y2, GameConstants.EXPLORE_RANGE - 1);
		}

		//-------- set should_set_power --------//

		should_set_power = get_should_set_power();

		//---- set this town's influence on the map ----//

		if (should_set_power)
			World.set_power(loc_x1, loc_y1, loc_x2, loc_y2, nation_recno);

		//---- if the newly built firm is visual in the zoom window, redraw the zoom buffer ----//

		//TODO drawing
		//if( is_in_zoom_win() )
		//sys.zoom_need_redraw = 1;  // set the flag on so it will be redrawn in the next frame
	}

	public void restore_world_matrix()
	{
		for (int yLoc = loc_y1; yLoc <= loc_y2; yLoc++)
		{
			for (int xLoc = loc_x1; xLoc <= loc_x2; xLoc++)
			{
				World.get_loc(xLoc, yLoc).remove_firm();
			}
		}

		//---- restore this town's influence on the map ----//

		if (should_set_power) // no power region for harbor as it build on coast which cannot be set with power region
			World.restore_power(loc_x1, loc_y1, loc_x2, loc_y2, 0, firm_recno);

		//---- if the newly built firm is visual in the zoom window, redraw the zoom buffer ----//

		//TODO drawing
		//if( is_in_zoom_win() )
		//sys.zoom_need_redraw = 1;
	}

	public bool get_should_set_power()
	{
		bool shouldSetPower = true;

		if (firm_id == FIRM_HARBOR) // don't set power for harbors
		{
			shouldSetPower = false;
		}
		else if (firm_id == FIRM_MARKET)
		{
			//--- don't set power for a market if it's linked to another nation's town ---//

			shouldSetPower = false;

			//--- only set the shouldSetPower to 1 if the market is linked to a firm of ours ---//

			for (int i = 0; i < linked_town_array.Count; i++)
			{
				Town town = TownArray[linked_town_array[i]];

				if (town.nation_recno == nation_recno)
				{
					shouldSetPower = true;
					break;
				}
			}
		}

		return shouldSetPower;
	}

	public void establish_contact_with_player()
	{
		if (nation_recno == 0)
			return;

		for (int yLoc = loc_y1; yLoc <= loc_y2; yLoc++)
		{
			for (int xLoc = loc_x1; xLoc <= loc_x2; xLoc++)
			{
				Location location = World.get_loc(xLoc, yLoc);

				location.set_firm(firm_recno);

				if (location.explored() && NationArray.player_recno != 0)
				{
					NationRelation relation = NationArray.player.get_relation(nation_recno);

					//if( !remote.is_enable() )
					//{
					relation.has_contact = true;
					//}
					//else
					//{
					//if( !relation.has_contact && !relation.contact_msg_flag )
					//{
					//// packet structure : <player nation> <explored nation>
					//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_NATION_CONTACT, 2*sizeof(short));
					//*shortPtr = NationArray.player_recno;
					//shortPtr[1] = nation_recno;
					//relation.contact_msg_flag = 1;
					//}
					//}
				}
			}
		}
	}

	public void complete_construction()
	{
		if (under_construction)
		{
			hit_points = max_hit_points;
			under_construction = false;
		}
	}

	public void capture_firm(int newNationRecno)
	{
		if (nation_recno == NationArray.player_recno)
			NewsArray.firm_captured(firm_recno, newNationRecno, 0); // 0 - the capturer is not a spy

		//-------- if this is an AI firm --------//

		if (firm_ai)
			ai_firm_captured(newNationRecno);

		//------------------------------------------//
		//
		// If there is an overseer in this firm, then the only
		// unit who can capture this firm will be the overseer only,
		// so calling its betray() function will capture the whole
		// firm already.
		//
		//------------------------------------------//

		if (overseer_recno != 0 && UnitArray[overseer_recno].spy_recno != 0)
			UnitArray[overseer_recno].spy_change_nation(newNationRecno, InternalConstants.COMMAND_AUTO);
		else
			change_nation(newNationRecno);
	}

	public virtual void change_nation(int newNationRecno)
	{
		if (nation_recno == newNationRecno)
			return;

		//---------- stop all attack actions to this firm ----------//
		UnitArray.stop_attack_firm(firm_recno);
		RebelArray.stop_attack_firm(firm_recno);

		Nation oldNation = NationArray[nation_recno];
		Nation newNation = NationArray[newNationRecno];

		//------ if there is a builder in this firm, change its nation also ----//

		if (builder_recno != 0)
		{
			Unit unit = UnitArray[builder_recno];

			unit.change_nation(newNationRecno);

			//--- if this is a spy, chance its cloak ----//

			if (unit.spy_recno != 0)
				SpyArray[unit.spy_recno].cloaked_nation_recno = newNationRecno;
		}

		//---------- stop all actions attacking this firm --------//

		UnitArray.stop_attack_firm(firm_recno);

		//------ clear defense mode for military camp -----//

		if (firm_id == FIRM_CAMP)
			((FirmCamp)this).clear_defense_mode(firm_recno);

		//---- update nation_unit_count_array[] ----//

		FirmInfo firmInfo = FirmRes[firm_id];

		if (nation_recno != 0)
			firmInfo.dec_nation_firm_count(nation_recno);

		if (newNationRecno != 0)
			firmInfo.inc_nation_firm_count(newNationRecno);

		//---- reset should_close_flag -----//

		if (firm_ai)
		{
			if (should_close_flag)
			{
				oldNation.firm_should_close_array[firm_id - 1]--;
				should_close_flag = false;
			}
		}

		//------- update player_spy_count -------//

		SpyArray.update_firm_spy_count(firm_recno);

		//--- update the cloaked_nation_recno of all spies in the firm ---//

		// check the cloaked nation recno of all spies in the firm
		SpyArray.change_cloaked_nation(Spy.SPY_FIRM, firm_recno, nation_recno, newNationRecno);

		//-----------------------------------------//

		if (firm_ai)
			oldNation.del_firm_info(firm_id, firm_recno);

		//------ update power nation recno ----------//

		if (should_set_power)
			World.restore_power(loc_x1, loc_y1, loc_x2, loc_y2, 0, firm_recno);

		should_set_power = get_should_set_power();

		// set power of the new nation
		if (should_set_power)
			World.set_power(loc_x1, loc_y1, loc_x2, loc_y2, newNationRecno);

		//------------ update link --------------//

		release_link(); // need to update link because firms are only linked to firms of the same nation

		nation_recno = newNationRecno;

		setup_link();

		//---------------------------------------//

		firm_ai = NationArray[nation_recno].is_ai();

		if (firm_ai)
			newNation.add_firm_info(firm_id, firm_recno);

		//--- if a nation set up a town in a location that the player has explored
		// contact between the nation and the player is established ---//

		establish_contact_with_player();

		//---- reset the action mode of all spies in this town ----//

		// we need to reset it. e.g. when we have captured an enemy town, SPY_SOW_DISSENT action must be reset to SPY_IDLE
		SpyArray.set_action_mode(Spy.SPY_FIRM, firm_recno, Spy.SPY_IDLE);

		//-- refresh display if this firm is currently selected --//

		if (FirmArray.selected_recno == firm_recno)
			Info.disp();
	}

	public void setup_link()
	{
		//-----------------------------------------------------------------------------//
		// check the connected firms location and structure if ai_link_checked is true
		//-----------------------------------------------------------------------------//

		if (firm_ai)
			ai_link_checked = false;

		//----- build firm-to-firm link relationship -------//

		FirmInfo firmInfo = FirmRes[firm_id];

		linked_firm_array.Clear();

		foreach (Firm firm in FirmArray)
		{
			if (firm.firm_recno == firm_recno)
				continue;

			//---- do not allow links between firms of different nation ----//

			if (firm.nation_recno != nation_recno)
				continue;

			//---------- check if the firm is close enough to this firm -------//

			if (Misc.rects_distance(firm.loc_x1, firm.loc_y1, firm.loc_x2, firm.loc_y2,
				    loc_x1, loc_y1, loc_x2, loc_y2) > InternalConstants.EFFECTIVE_FIRM_FIRM_DISTANCE)
			{
				continue;
			}

			//------ check if both are on the same terrain type ------//

			if (World.get_loc(firm.center_x, firm.center_y).is_plateau()
			    != World.get_loc(center_x, center_y).is_plateau())
			{
				continue;
			}

			//----- if the firms are linkable to each other -----//

			if (!firmInfo.is_linkable_to_firm(firm.firm_id))
				continue;

			//------- determine the default link status ------//

			// if the two firms are of the same nation, get the default link status which is based on the types of the firms
			// if the two firms are of different nations, default link status is both side disabled
			int defaultLinkStatus;
			if (firm.nation_recno == nation_recno)
				defaultLinkStatus = firmInfo.default_link_status(firm.firm_id);
			else
				defaultLinkStatus = InternalConstants.LINK_DD;

			//-------- add the link now -------//

			linked_firm_array.Add(firm.firm_recno);
			linked_firm_enable_array.Add(defaultLinkStatus);

			// now from the other firm's side
			if (defaultLinkStatus == InternalConstants.LINK_ED) // Reverse the link status for the opposite linker
				defaultLinkStatus = InternalConstants.LINK_DE;

			else if (defaultLinkStatus == InternalConstants.LINK_DE)
				defaultLinkStatus = InternalConstants.LINK_ED;

			firm.linked_firm_array.Add(firm_recno);
			firm.linked_firm_enable_array.Add(defaultLinkStatus);

			if (firm.firm_ai)
				firm.ai_link_checked = false;

			if (firm.firm_id == FIRM_HARBOR)
			{
				FirmHarbor harbor = (FirmHarbor)firm;
				harbor.link_checked = false;
			}
		}

		//----- build firm-to-town link relationship -------//

		linked_town_array.Clear();

		if (!firmInfo.is_linkable_to_town)
			return;

		foreach (Town town in TownArray)
		{
			//------ check if the town is close enough to this firm -------//

			if (Misc.rects_distance(town.loc_x1, town.loc_y1, town.loc_x2, town.loc_y2,
				    loc_x1, loc_y1, loc_x2, loc_y2) > InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE)
			{
				continue;
			}

			//------ check if both are on the same terrain type ------//

			if (World.get_loc(town.center_x, town.center_y).is_plateau()
			    != World.get_loc(center_x, center_y).is_plateau())
			{
				continue;
			}


			//------- determine the default link status ------//
			// if the two firms are of the same nation, get the default link status which is based on the types of the firms
			// if the two firms are of different nations, default link status is both side disabled
			int defaultLinkStatus;
			if (town.nation_recno == nation_recno)
				defaultLinkStatus = InternalConstants.LINK_EE;
			else
				defaultLinkStatus = InternalConstants.LINK_DD;

			//---------------------------------------------------//
			//
			// If this is a camp, it can be linked to the town when
			// either the town is an independent one or the town
			// is not linked to any camps of its own.
			//
			//---------------------------------------------------//

			if (firm_id == FIRM_CAMP)
			{
				if (town.nation_recno == 0 || !town.has_linked_own_camp)
					defaultLinkStatus = InternalConstants.LINK_EE;
			}

			//-------- add the link now -------//

			linked_town_array.Add(town.town_recno);
			linked_town_enable_array.Add(defaultLinkStatus);

			// now from the town's side
			if (defaultLinkStatus == InternalConstants.LINK_ED) // Reverse the link status for the opposite linker
				defaultLinkStatus = InternalConstants.LINK_DE;

			else if (defaultLinkStatus == InternalConstants.LINK_DE)
				defaultLinkStatus = InternalConstants.LINK_ED;

			town.linked_firm_array.Add(firm_recno);
			town.linked_firm_enable_array.Add(defaultLinkStatus);

			if (town.ai_town)
				town.ai_link_checked = false;
		}
	}

	public void release_link()
	{
		//------ release linked firms ------//

		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			Firm firm = FirmArray[linked_firm_array[i]];
			firm.release_firm_link(firm_recno);

			if (firm.firm_ai)
				firm.ai_link_checked = false;
		}

		//------ release linked towns ------//

		for (int i = 0; i < linked_town_array.Count; i++)
		{
			Town town = TownArray[linked_town_array[i]];
			town.release_firm_link(firm_recno);

			if (town.ai_town)
				town.ai_link_checked = false;
		}
	}

	public void release_firm_link(int releaseFirmRecno)
	{
		//-----------------------------------------------------------------------------//
		// check the connected firms location and structure if ai_link_checked is true
		//-----------------------------------------------------------------------------//
		if (firm_ai)
			ai_link_checked = false;

		for (int i = linked_firm_array.Count - 1; i >= 0; i--)
		{
			if (linked_firm_array[i] == releaseFirmRecno)
			{
				linked_firm_array.RemoveAt(i);
				linked_firm_enable_array.RemoveAt(i);
				return;
			}
		}
	}

	public void release_town_link(int releaseTownRecno)
	{
		//-----------------------------------------------------------------------------//
		// check the connected firms location and structure if ai_link_checked is true
		//-----------------------------------------------------------------------------//

		if (firm_ai)
			ai_link_checked = false;

		for (int i = linked_town_array.Count - 1; i >= 0; i--)
		{
			if (linked_town_array[i] == releaseTownRecno)
			{
				linked_town_array.RemoveAt(i);
				linked_town_enable_array.RemoveAt(i);
				return;
			}
		}
	}

	public bool can_toggle_town_link()
	{
		return firm_id != FIRM_MARKET; // only a market cannot toggle its link as it is
	}

	public bool can_toggle_firm_link(int firmRecno)
	{
		Firm firm = FirmArray[firmRecno];

		//--- market to harbor link is determined by trade treaty ---//

		if ((firm_id == FIRM_MARKET && firm.firm_id == FIRM_HARBOR) ||
		    (firm_id == FIRM_HARBOR && firm.firm_id == FIRM_MARKET))
		{
			return false;
		}

		return FirmRes[firm_id].is_linkable_to_firm(firm.firm_id);
	}

	//public int      is_in_zoom_win();

	public int find_settle_town()
	{
		int minDistance = Int32.MaxValue;
		int nearestTownRecno = 0;

		//-------- scan for our own town first -----------//

		for (int i = 0; i < linked_town_array.Count; i++)
		{
			Town town = TownArray[linked_town_array[i]];

			if (town.population >= GameConstants.MAX_TOWN_POPULATION)
				continue;

			if (town.nation_recno != nation_recno)
				continue;

			int townDistance = Misc.rects_distance(town.loc_x1, town.loc_y1, town.loc_x2, town.loc_y2,
				loc_x1, loc_y1, loc_x2, loc_y2);

			if (townDistance < minDistance)
			{
				minDistance = townDistance;
				nearestTownRecno = town.town_recno;
			}
		}

		return nearestTownRecno;
	}

	public bool should_show_info()
	{
		if (Config.show_ai_info || nation_recno == NationArray.player_recno || player_spy_count > 0)
		{
			return true;
		}

		//------ if the builder is a spy of the player ------//

		if (builder_recno != 0)
		{
			if (UnitArray[builder_recno].true_nation_recno() == NationArray.player_recno)
				return true;
		}

		//----- if any of the workers belong to the player, show the info of this firm -----//

		if (have_own_workers(true))
			return true;

		//---- if there is a phoenix of the player over this firm ----//

		if (NationArray.player_recno != 0 && NationArray.player.revealed_by_phoenix(loc_x1, loc_y1))
			return true;

		return false;
	}

	public bool set_builder(int newBuilderRecno)
	{
		//------------------------------------//

		int oldBuilderRecno = builder_recno; // store the old builder recno

		builder_recno = newBuilderRecno;

		//-------- assign the new builder ---------//

		if (builder_recno != 0)
		{
			Unit unit = UnitArray[builder_recno];
			//### begin alex 18/10 ###//
			unit.group_select_id = 0; // clear group select id
			//#### end alex 18/10 ####//
			if (unit.is_visible()) // is visible if the unit is not inside the firm location
			{
				builder_region_id = World.get_region_id(unit.cur_x_loc(), unit.cur_y_loc());
				unit.deinit_sprite();

				if (unit.selected_flag)
				{
					unit.selected_flag = false;
					UnitArray.selected_count--;
				}
			}

			unit.set_mode(UnitConstants.UNIT_MODE_CONSTRUCT, firm_recno);
		}

		if (oldBuilderRecno != 0)
			mobilize_builder(oldBuilderRecno);

		return true;
	}

	public int find_idle_builder(bool nearest)
	{
		int minDist = Int32.MaxValue;
		int resultRecno = 0;

		foreach (Unit unit in UnitArray)
		{
			if (unit.nation_recno != nation_recno || unit.race_id == 0)
				continue;

			if (unit.skill.skill_id != Skill.SKILL_CONSTRUCTION)
				continue;

			if (unit.is_visible() && unit.region_id() != region_id)
				continue;

			if (unit.unit_mode == UnitConstants.UNIT_MODE_CONSTRUCT)
			{
				Firm firm = FirmArray[unit.unit_mode_para];

				if (firm.under_construction || (firm.hit_points * 100 / firm.max_hit_points) <= 90 ||
				    Info.game_date <= firm.last_attacked_date.AddDays(8))
					continue;
			}
			else if (unit.unit_mode == UnitConstants.UNIT_MODE_UNDER_TRAINING)
			{
				continue;
			}
			else if (unit.action_mode == UnitConstants.ACTION_ASSIGN_TO_FIRM && unit.action_para2 == firm_recno)
			{
				return unit.sprite_recno;
			}
			else if (unit.action_mode != UnitConstants.ACTION_STOP)
			{
				continue;
			}

			if (!nearest)
				return unit.sprite_recno;

			int curDist = Misc.points_distance(unit.next_x_loc(), unit.next_y_loc(), loc_x1, loc_y1);
			if (curDist < minDist)
			{
				resultRecno = unit.sprite_recno;
				minDist = curDist;
			}
		}

		return resultRecno;
	}

	public void send_idle_builder_here(char remoteAction)
	{
		if (builder_recno != 0)
			return;

		//if( remote.is_enable() && !remoteAction )
		//{
		//// packet structure : <firm recno>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_FIRM_REQ_BUILDER, sizeof(short));
		//*shortPtr = firm_recno;
		//return;
		//}

		int unitRecno = find_idle_builder(true);
		if (unitRecno == 0)
			return;

		Unit unit = UnitArray[unitRecno];
		if (unit.unit_mode == UnitConstants.UNIT_MODE_CONSTRUCT)
		{
			Firm firm = FirmArray[unit.unit_mode_para];

			// order idle unit out of the building
			if (!firm.set_builder(0))
			{
				return;
			}
		}

		unit.assign(loc_x1, loc_y1);
	}

	public virtual void sell_firm(int remoteAction)
	{
		//if( !remoteAction && remote.is_enable() )
		//{
		//// packet structure : <firm recno>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_FIRM_SELL, sizeof(short));
		//*shortPtr = firm_recno;
		//return;
		//}
		//------- sell at 50% of the original cost -------//

		Nation nation = NationArray[nation_recno];

		double sellIncome = FirmRes[firm_id].setup_cost / 2.0 * hit_points / max_hit_points;

		nation.add_income(NationBase.INCOME_SELL_FIRM, sellIncome);

		SERes.sound(center_x, center_y, 1, 'F', firm_id, "SELL");

		FirmArray.DeleteFirm(this);
	}

	public void destruct_firm(int remoteAction)
	{
		//if( !remoteAction && remote.is_enable() )
		//{
		//// packet structure : <firm recno>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_FIRM_DESTRUCT, sizeof(short));
		//*shortPtr = firm_recno;
		//return;
		//}

		SERes.sound(center_x, center_y, 1, 'F', firm_id, "DEST");

		FirmArray.DeleteFirm(this);
	}

	public void cancel_construction(int remoteAction)
	{
		//if( !remoteAction && remote.is_enable())
		//{
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_FIRM_CANCEL, sizeof(short));
		//shortPtr[0] = firm_recno;
		//return;
		//}
		//------ get half of the construction cost back -------//

		Nation nation = NationArray[nation_recno];

		nation.add_expense(NationBase.EXPENSE_FIRM, -FirmRes[firm_id].setup_cost / 2.0);

		FirmArray.DeleteFirm(this);
	}

	public bool can_assign_capture()
	{
		return overseer_recno == 0 && workers.Count == 0;
	}

	public bool can_worker_capture(int captureNationRecno)
	{
		if (captureNationRecno == 0) // neutral units cannot capture
			return false;

		if (nation_recno == captureNationRecno) // cannot capture its own firm
			return false;

		//----- if this firm needs an overseer, can only capture it when the overseer is the spy ---//

		if (FirmRes[firm_id].need_overseer)
		{
			return overseer_recno != 0 && UnitArray[overseer_recno].true_nation_recno() == captureNationRecno;
		}

		//--- if this firm doesn't need an overseer, can capture it if all the units in the firm are the player's spies ---//

		int captureUnitCount = 0, otherUnitCount = 0;

		foreach (Worker worker in workers)
		{
			if (worker.spy_recno != 0 && SpyArray[worker.spy_recno].true_nation_recno == captureNationRecno)
			{
				captureUnitCount++;
			}
			else if (worker.town_recno != 0)
			{
				if (TownArray[worker.town_recno].nation_recno == captureNationRecno)
					captureUnitCount++;
				else
					otherUnitCount++;
			}
			else
			{
				otherUnitCount++; // must be an own unit in camps and bases if the unit is not a spy
			}
		}

		return captureUnitCount > 0 && otherUnitCount == 0;
	}

	public virtual bool is_worker_full()
	{
		return workers.Count == MAX_WORKER;
	}

	public bool have_own_workers(bool checkSpy = false)
	{
		foreach (Worker worker in workers)
		{
			if (worker.is_nation(firm_recno, NationArray.player_recno, checkSpy))
				return true;
		}

		return false;
	}

	public void set_worker_home_town(int townRecno, char remoteAction, int workerId = 0)
	{
		if (workerId == 0)
			workerId = selected_worker_id;

		if (workerId == 0 || workerId > workers.Count)
			return;

		Town town = TownArray[townRecno];
		Worker worker = workers[workerId - 1];

		if (worker.town_recno != townRecno)
		{
			if (!worker.is_nation(firm_recno, nation_recno))
				return;
			if (town.population >= GameConstants.MAX_TOWN_POPULATION)
				return;
		}

		//if(!remoteAction && remote.is_enable() )
		//{
		//// packet structure : <firm recno> <town recno> <workderId>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_FIRM_SET_WORKER_HOME, 3*sizeof(short));
		//shortPtr[0] = firm_recno;
		//shortPtr[1] = townRecno;
		//shortPtr[2] = workerId;
		//return;
		//}

		//-------------------------------------------------//

		if (worker.town_recno == townRecno)
		{
			resign_worker(worker);
		}

		//--- otherwise, set the worker's home town to the new one ---//

		else if (worker.is_nation(firm_recno, nation_recno) &&
		         town.nation_recno ==
		         nation_recno) // only allow when the worker lives in a town belonging to the same nation and moving domestically
		{
			int workerLoyalty = worker.loyalty();

			TownArray[worker.town_recno].dec_pop(worker.race_id, true);
			town.inc_pop(worker.race_id, true, workerLoyalty);

			worker.town_recno = townRecno;
		}
	}

	public bool can_spy_bribe(int bribeWorkerId, int briberNationRecno)
	{
		bool canBribe = false;
		int spyRecno;

		if (bribeWorkerId != 0) // the overseer is selected
			spyRecno = workers[bribeWorkerId - 1].spy_recno;
		else
			spyRecno = UnitArray[overseer_recno].spy_recno;

		if (spyRecno != 0)
		{
			// only when the unit is not yet a spy of the player. Still display the bribe button when it's a spy of another nation
			canBribe = SpyArray[spyRecno].true_nation_recno != briberNationRecno;
		}
		else
		{
			if (bribeWorkerId != 0)
				canBribe = workers[bribeWorkerId - 1].race_id > 0; // cannot bribe if it's a weapon
			else
				canBribe = UnitArray[overseer_recno].rank_id != Unit.RANK_KING; // cannot bribe a king
		}

		return canBribe;
	}

	public int spy_bribe(int bribeAmount, int briberSpyRecno, int workerId)
	{
		// this can happen in multiplayer as there is a one frame delay when the message is sent and when it is processed
		if (!can_spy_bribe(workerId, SpyArray[briberSpyRecno].true_nation_recno))
			return 0;

		//---------------------------------------//

		int succeedChance = spy_bribe_succeed_chance(bribeAmount, briberSpyRecno, workerId);

		NationArray[SpyArray[briberSpyRecno].true_nation_recno].add_expense(NationBase.EXPENSE_BRIBE, bribeAmount, false);

		//------ if the bribe succeeds ------//

		if (succeedChance > 0 && Misc.Random(100) < succeedChance)
		{
			Spy briber = SpyArray[briberSpyRecno];
			Spy newSpy = SpyArray.AddSpy(0, 10); // add a new Spy record

			newSpy.action_mode = Spy.SPY_IDLE;
			newSpy.spy_loyalty = Math.Min(100, Math.Max(30, succeedChance)); // within the 30-100 range

			newSpy.true_nation_recno = briber.true_nation_recno;
			newSpy.cloaked_nation_recno = briber.cloaked_nation_recno;

			if (workerId != 0)
			{
				Worker worker = workers[workerId - 1];
				worker.spy_recno = newSpy.spy_recno;
				newSpy.race_id = worker.race_id;
				newSpy.name_id = worker.name_id;

				// if this worker does not have a name, give him one now as a spy must reserve a name (see below on use_name_id() for reasons)
				if (newSpy.name_id == 0)
					newSpy.name_id = RaceRes[newSpy.race_id].get_new_name_id();
			}
			else if (overseer_recno != 0)
			{
				Unit unit = UnitArray[overseer_recno];
				unit.spy_recno = newSpy.spy_recno;
				newSpy.race_id = unit.race_id;
				newSpy.name_id = unit.name_id;
			}

			newSpy.set_place(Spy.SPY_FIRM, firm_recno);

			//-- Spy always registers its name twice as his name will be freed up in deinit(). Keep an additional right because when a spy is assigned to a town, the normal program will free up the name id., so we have to keep an additional copy

			RaceRes[newSpy.race_id].use_name_id(newSpy.name_id);

			bribe_result = Spy.BRIBE_SUCCEED;

			//TODO drawing
			//if( firm_recno == FirmArray.selected_recno )
			//info.disp();

			return newSpy.spy_recno;
		}
		else //------- if the bribe fails --------//
		{
			SpyArray[briberSpyRecno].get_killed(0); // the spy gets killed when the action failed.
			// 0 - don't display new message for the spy being killed, so we already display the msg on the interface
			bribe_result = Spy.BRIBE_FAIL;

			//TODO drawing
			//if( firm_recno == FirmArray.selected_recno )
			//info.disp();

			return 0;
		}
	}

	public int spy_bribe_succeed_chance(int bribeAmount, int briberSpyRecno, int workerId)
	{
		Spy spy = SpyArray[briberSpyRecno];

		//---- if the bribing target is a worker ----//

		int unitLoyalty = 0, unitRaceId = 0, unitCommandPower = 0;
		int targetSpyRecno = 0;

		if (workerId != 0)
		{
			Worker worker = workers[workerId - 1];

			unitLoyalty = worker.loyalty();
			unitRaceId = worker.race_id;
			unitCommandPower = 0;
			targetSpyRecno = worker.spy_recno;
		}
		else if (overseer_recno != 0)
		{
			Unit unit = UnitArray[overseer_recno];

			unitLoyalty = unit.loyalty;
			unitRaceId = unit.race_id;
			unitCommandPower = unit.commander_power();
			targetSpyRecno = unit.spy_recno;
		}

		//---- determine whether the bribe will be successful ----//

		int succeedChance;

		if (targetSpyRecno != 0) // if the bribe target is also a spy
		{
			succeedChance = 0;
		}
		else
		{
			succeedChance = spy.spy_skill - unitLoyalty - unitCommandPower
			                + (int)NationArray[spy.true_nation_recno].reputation
			                + 200 * bribeAmount / GameConstants.MAX_BRIBE_AMOUNT;

			//-- the chance is higher if the spy or the spy's king is racially homongenous to the bribe target,

			int spyKingRaceId = NationArray[spy.true_nation_recno].race_id;

			succeedChance += (RaceRes.is_same_race(spy.race_id, unitRaceId) ? 1 : 0) * 10 +
			                 (RaceRes.is_same_race(spyKingRaceId, unitRaceId) ? 1 : 0) * 10;

			if (unitLoyalty > 60) // harder for bribe units with over 60 loyalty
				succeedChance -= (unitLoyalty - 60);

			if (unitLoyalty > 70) // harder for bribe units with over 70 loyalty
				succeedChance -= (unitLoyalty - 70);

			if (unitLoyalty > 80) // harder for bribe units with over 80 loyalty
				succeedChance -= (unitLoyalty - 80);

			if (unitLoyalty > 90) // harder for bribe units with over 90 loyalty
				succeedChance -= (unitLoyalty - 90);

			if (unitLoyalty == 100)
				succeedChance = 0;
		}

		return succeedChance;
	}

	public bool validate_cur_bribe()
	{
		if (SpyArray.IsDeleted(action_spy_recno) ||
		    SpyArray[action_spy_recno].true_nation_recno != NationArray.player_recno)
		{
			return false;
		}

		return can_spy_bribe(selected_worker_id, SpyArray[action_spy_recno].true_nation_recno);
	}

	//------------------- defense --------------------//
	public virtual void auto_defense(int targetRecno)
	{
		//--------------------------------------------------------//
		// if the firm_id is FIRM_CAMP, send the units to defense
		// the firm
		//--------------------------------------------------------//
		if (firm_id == FIRM_CAMP)
		{
			FirmCamp camp = (FirmCamp)this;
			camp.defend_target_recno = targetRecno;
			camp.defense(targetRecno);
		}

		for (int i = linked_town_array.Count - 1; i >= 0; i--)
		{
			if (linked_town_array[i] == 0 || TownArray.IsDeleted(linked_town_array[i]))
				continue;

			Town town = TownArray[linked_town_array[i]];

			//-------------------------------------------------------//
			// find whether military camp is linked to this town. If
			// so, defense for this firm
			//-------------------------------------------------------//
			if (town.nation_recno == nation_recno)
				town.auto_defense(targetRecno);

			//-------------------------------------------------------//
			// some linked town may be deleted after calling auto_defense().
			// Also, the data in the linked_town_array may also be changed.
			//-------------------------------------------------------//
			if (i > linked_town_array.Count)
				i = linked_town_array.Count;
		}
	}

	public bool locate_space(bool removeFirm, ref int xLoc, ref int yLoc, int xLoc2, int yLoc2,
		int width, int height, int mobileType = UnitConstants.UNIT_LAND, int regionId = 0)
	{
		int checkXLoc, checkYLoc;

		if (removeFirm)
		{
			//*** note only for land unit with size 1x1 ***//
			int mType = UnitConstants.UNIT_LAND;

			for (checkYLoc = loc_y1; checkYLoc <= loc_y2; checkYLoc++)
			{
				for (checkXLoc = loc_x1; checkXLoc <= loc_x2; checkXLoc++)
				{
					if (World.get_loc(checkXLoc, checkYLoc).can_move(mType))
					{
						xLoc = checkXLoc;
						yLoc = checkYLoc;
						return true;
					}
				}
			}
		}
		else
		{
			checkXLoc = loc_x1;
			checkYLoc = loc_y1;
			if (!World.locate_space(ref checkXLoc, ref checkYLoc, xLoc2, yLoc2, width, height, mobileType, regionId))
			{
				return false;
			}
			else
			{
				xLoc = checkXLoc;
				yLoc = checkYLoc;
				return true;
			}
		}

		return false;
	}

	public void sort_worker()
	{
		//TODO this function is for UI
	}

	public void process_animation()
	{
		//-------- process animation ----------//

		FirmBuild firmBuild = FirmRes.get_build(firm_build_id);
		int frameCount = firmBuild.frame_count;

		if (frameCount == 1) // no animation for this firm
			return;

		//---------- next frame -----------//

		if (--remain_frame_delay == 0) // if it is in the delay between frames
		{
			remain_frame_delay = firmBuild.frame_delay(cur_frame);

			if (++cur_frame > frameCount)
			{
				if (firmBuild.animate_full_size)
					cur_frame = 1;
				else
				{
					cur_frame = 2; // start with the 2nd frame as the 1st frame is the common frame
				}
			}
		}
	}

	public void process_construction()
	{
		if (firm_id == FIRM_MONSTER)
		{
			//--------- process construction for monster firm ----------//
			hit_points++;

			if (hit_points >= max_hit_points)
			{
				hit_points = max_hit_points;
				under_construction = false;
			}

			return;
		}

		if (!under_construction)
			return;

		//--- can only do construction when the firm is not under attack ---//

		if (Info.game_date <= last_attacked_date.AddDays(1.0))
			return;

		if (Sys.Instance.FrameNumber % 2 != 0) // one build every 2 frames
			return;

		//------ increase the construction progress ------//

		Unit unit = UnitArray[builder_recno];

		if (unit.skill.skill_id == Skill.SKILL_CONSTRUCTION) // if builder unit has construction skill
			hit_points += 1 + unit.skill.skill_level / 30;
		else
			hit_points++;

		if (Config.fast_build && nation_recno == NationArray.player_recno)
			hit_points += 10;

		//----- increase skill level of the builder unit -----//

		if (unit.skill.skill_id == Skill.SKILL_CONSTRUCTION) // if builder unit has construction skill
		{
			if (++unit.skill.skill_level_minor > 100)
			{
				unit.skill.skill_level_minor = 0;

				if (unit.skill.skill_level < 100)
					unit.skill.skill_level++;
			}
		}

		//------- when the construction is complete ----------//

		if (hit_points >= max_hit_points) // finished construction
		{
			hit_points = max_hit_points;

			bool needAssignUnit = false;

			under_construction = false;

			if (nation_recno == NationArray.player_recno)
				SERes.far_sound(center_x, center_y, 1, 'S', unit.sprite_id, "FINS", 'F', firm_id);

			FirmInfo firmInfo = FirmRes[firm_id];

			if ((firmInfo.need_overseer || firmInfo.need_worker) &&
			    (firmInfo.firm_skill_id == 0 ||
			     firmInfo.firm_skill_id == unit.skill.skill_id)) // the builder with the skill required
			{
				unit.set_mode(0); // reset it from UNIT_MODE_CONSTRUCT

				needAssignUnit = true;
			}
			else
			{
				set_builder(0);
			}

			//---------------------------------------------------------------------------------------//
			// should call assign_unit() first before calling action_finished(...UNDER_CONSTRUCTION)
			//---------------------------------------------------------------------------------------//

			if (needAssignUnit)
			{
				assign_unit(builder_recno);
				//------------------------------------------------------------------------------//
				// Note: there may be chance the unit cannot be assigned into the firm
				//------------------------------------------------------------------------------//
				if (workers.Count == 0 && overseer_recno == 0) // no assignment, can't assign
				{
					//------- init_sprite or delete the builder ---------//
					int xLoc = loc_x1, yLoc = loc_y1; // xLoc & yLoc are used for returning results
					SpriteInfo spriteInfo = unit.sprite_info;
					if (!locate_space(remove_firm, ref xLoc, ref yLoc, loc_x2, loc_y2,
						    spriteInfo.loc_width, spriteInfo.loc_height))
						UnitArray.disappear_in_firm(builder_recno); // kill the unit
					else
						unit.init_sprite(xLoc, yLoc); // restore the unit
				}
			}

			// ##### begin Gilbert 10/10 #######//
			//if( nation_recno == NationArray.player_recno )
			//	se_res.far_sound(center_x, center_y, 1, 'S', unit.sprite_id,
			//		"FINS", 'F',  firm_id);
			// ##### end Gilbert 10/10 #######//

			builder_recno = 0;
		}
	}

	public void process_repair()
	{
		if (NationArray[nation_recno].cash < 0) // if you don't have cash, the repair workers will not work
			return;

		if (builder_recno == 0)
			return;

		Unit unit = UnitArray[builder_recno];

		//--- can only do construction when the firm is not under attack ---//

		if (Info.game_date <= last_attacked_date.AddDays(1.0))
		{
			//---- if the construction worker is a spy, it will damage the building when the building is under attack ----//

			if (unit.spy_recno != 0 && unit.true_nation_recno() != nation_recno)
			{
				hit_points -= SpyArray[unit.spy_recno].spy_skill / 30.0;

				if (hit_points < 0)
					hit_points = 0.0;
			}

			return;
		}

		//------- repair now - only process once every 3 days -----//

		if (hit_points >= max_hit_points)
			return;

		// repair once every 1 to 6 days, depending on the skill level of the construction worker
		int dayInterval = (100 - unit.skill.skill_level) / 20 + 1;

		if (Info.TotalDays % dayInterval == firm_recno % dayInterval)
		{
			hit_points++;

			if (hit_points > max_hit_points)
				hit_points = max_hit_points;
		}
	}

	public void process_common_ai()
	{
		if (Info.TotalDays % 30 == firm_recno % 30)
			think_repair();

		//------ think about closing this firm ------//

		if (!should_close_flag)
		{
			if (ai_should_close())
			{
				should_close_flag = true;
				NationArray[nation_recno].firm_should_close_array[firm_id - 1]++;
			}
		}
	}

	public abstract void process_ai();

	public virtual void next_day()
	{
		if (nation_recno == 0)
			return;

		//------ think about updating link status -------//
		//
		// This part must be done here instead of in
		// process_ai() because it will be too late to do
		// it in process_ai() as the next_day() will call
		// first and some wrong goods may be input to markets.
		//
		//-----------------------------------------------//

		if (firm_ai)
		{
			// once 30 days or when the link has been changed.
			if (Info.TotalDays % 30 == firm_recno % 30 || !ai_link_checked)
			{
				ai_update_link_status();
				ai_link_checked = true;
			}
		}

		//-------- pay expenses ----------//

		pay_expense();

		//------- update loyalty --------//

		if (Info.TotalDays % 30 == firm_recno % 30)
			update_loyalty();

		//-------- consume food --------//

		if (!FirmRes[firm_id].live_in_town && workers.Count > 0)
			consume_food();

		//------ think worker migration -------//

		if (Info.TotalDays % 30 == firm_recno % 30)
			think_worker_migrate();

		//--------- repairing ----------//

		process_repair();

		//------ catching spies -------//

		if (Info.TotalDays % 30 == firm_recno % 30)
			SpyArray.catch_spy(Spy.SPY_FIRM, firm_recno);

		//----- process workers from other town -----//

		if (FirmRes[firm_id].live_in_town)
		{
			process_independent_town_worker();
		}

		//--- recheck no_neighbor_space after a period, there may be new space available now ---//

		if (no_neighbor_space && Info.TotalDays % 10 == firm_recno % 10)
		{
			// whether it's FIRM_INN or not really doesn't matter, just any firm type will do
			if (NationArray[nation_recno].find_best_firm_loc(FIRM_INN, loc_x1, loc_y1, out _, out _))
				no_neighbor_space = false;
		}
	}


	public virtual void next_month()
	{
		//------ update nation power recno ------//

		bool newShouldSetPower = get_should_set_power();

		if (newShouldSetPower == should_set_power)
			return;

		if (should_set_power)
			World.restore_power(loc_x1, loc_y1, loc_x2, loc_y2, 0, firm_recno);

		should_set_power = newShouldSetPower;

		if (should_set_power)
			World.set_power(loc_x1, loc_y1, loc_x2, loc_y2, nation_recno);
	}

	public virtual void next_year()
	{
		//------- post income data --------//

		last_year_income = cur_year_income;
		cur_year_income = 0.0;
	}

	public void mobilize_all_workers(int remoteAction)
	{
		//if( !remoteAction && remote.is_enable() )
		//{
		//// packet strcture : <firm_recno>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_FIRM_MOBL_ALL_WORKERS, sizeof(short) );
		//shortPtr[0] = firm_recno;
		//return;
		//}

		if (nation_recno == NationArray.player_recno)
			Power.reset_selection();

		//------- detect buttons on hiring firm workers -------//

		int mobileWorkerId = 1;

		while (workers.Count > 0 && mobileWorkerId <= workers.Count)
		{
			Worker worker = workers[mobileWorkerId - 1];

			if (!worker.is_nation(firm_recno, nation_recno))
			{
				// prohibit mobilizing workers not under your color
				mobileWorkerId++;
				continue;
			}

			// always record 1 as the workers info are moved forward from the back to the front
			int unitRecno = mobilize_worker(mobileWorkerId, InternalConstants.COMMAND_AUTO);

			if (unitRecno == 0)
				break; // keep the rest workers as there is no space for creating the unit

			Unit unit = UnitArray[unitRecno];
			unit.team_id = UnitArray.cur_team_id;

			if (nation_recno == NationArray.player_recno)
			{
				unit.selected_flag = true;
				UnitArray.selected_count++;
				if (UnitArray.selected_recno == 0)
					UnitArray.selected_recno = unitRecno; // set first worker as selected
			}
		}

		UnitArray.cur_team_id++;

		// for player, mobilize_all_workers can only be called when the player presses the button.
		//TODO drawing
		//if( nation_recno == NationArray.player_recno )
		//info.disp();
	}

	public virtual int mobilize_worker(int workerId, int remoteAction)
	{
		Worker worker = workers[workerId - 1];

		if (remoteAction <= InternalConstants.COMMAND_REMOTE && !worker.is_nation(firm_recno, nation_recno))
		{
			// cannot order mobilization of foreign workers
			return 0;
		}

		//if(!remoteAction && remote.is_enable() )
		//{
		//// packet strcture : <firm_recno> <workerId>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_FIRM_MOBL_WORKER, 2*sizeof(short) );
		//shortPtr[0] = firm_recno;
		//shortPtr[1] = workerId;
		//return 0;
		//}

		//err_when( !worker_array );    // this function shouldn't be called if this firm does not need worker

		//------------- resign worker --------------//

		int oldWorkerCount = workers.Count;

		int unitRecno2 = resign_worker(worker);

		if (unitRecno2 == 0 && workers.Count == oldWorkerCount)
			return 0;

		//------ create a mobile unit -------//

		int unitRecno = 0;

		// if does not live_in_town, resign_worker() create the unit already, so don't create it again here.
		if (FirmRes[firm_id].live_in_town)
		{
			//TODO check. It seems that resign_worker() also calls create_worker_unit()
			unitRecno = create_worker_unit(worker);

			if (unitRecno == 0) // no space for creating units
				return 0;
		}

		//------------------------------------//

		sort_worker();

		return unitRecno != 0 ? unitRecno : unitRecno2;
	}

	public int create_worker_unit(Worker worker)
	{
		//--------- copy the worker's info --------//

		int unitLoyalty = worker.loyalty();

		//------------ create an unit --------------//

		int unitId = worker.unit_id;
		// this worker no longer has a job as it has been resigned
		int unitRecno = create_unit(unitId, worker.town_recno, false);

		if (unitRecno == 0)
			return 0;

		Unit unit = UnitArray[unitRecno];

		//------- set the unit's parameters --------//

		unit.skill.skill_id = worker.skill_id;
		unit.skill.skill_level = worker.skill_level;
		unit.skill.skill_level_minor = worker.skill_level_minor;
		unit.set_combat_level(worker.combat_level);
		unit.skill.combat_level_minor = worker.combat_level_minor;
		unit.loyalty = unitLoyalty;
		unit.hit_points = worker.hit_points;
		unit.rank_id = worker.rank_id;

		if (UnitRes[unit.unit_id].unit_class == UnitConstants.UNIT_CLASS_WEAPON)
		{
			unit.set_weapon_version(worker.extra_para); // restore nation contribution
		}
		else if (unit.race_id != 0)
		{
			unit.cur_power = worker.extra_para;

			if (unit.cur_power < 0)
				unit.cur_power = 0;

			if (unit.cur_power > 150)
				unit.cur_power = 150;
		}

		unit.fix_attack_info();

		//if( unitInfo.unit_class == UnitRes.UNIT_CLASS_WEAPON )
		//{
		//	switch( unitId )
		//	{
		//		case UNIT_BALLISTA:
		//			unit.attack_count = 2;
		//			break;
		//		case UNIT_EXPLOSIVE_CART:
		//			unit.attack_count = 0;
		//			break;
		//		default:
		//			unit.attack_count = 1;
		//	}
		//		if( unit.attack_count > 0)
		//		{
		//			unit.attack_info_array = unit_res.attack_info_array
		//				+ unitInfo.first_attack-1
		//				+ (thisWorker.extra_para -1) * unit.attack_count;		// extra para keeps the weapon version
		//		}
		//		else
		//		{
		//			// no attack like explosive cart
		//			unit.attack_info_array = NULL;
		//		}
		//}

		if (worker.name_id != 0 && worker.race_id != 0) // if this worker is formerly an unit who has a name
			unit.set_name(worker.name_id);

		//------ if the unit is a spy -------//

		if (worker.spy_recno != 0)
		{
			Spy spy = SpyArray[worker.spy_recno];

			unit.spy_recno = worker.spy_recno;
			unit.ai_unit = spy.cloaked_nation_recno != 0 && NationArray[spy.cloaked_nation_recno].is_ai();

			unit.set_name(spy.name_id); // set the name id. of this unit

			spy.set_place(Spy.SPY_MOBILE, unitRecno);
		}

		//--- decrease the nation unit count as the Unit has already increased it ----//

		if (!FirmRes[firm_id].live_in_town) // if the unit does not live in town, increase the unit count now
			UnitRes[unit.unit_id].dec_nation_unit_count(nation_recno);

		//--- set non-military units to non-aggressive, except ai ---//
		if (!ConfigAdv.firm_mobilize_civilian_aggressive && unit.race_id > 0 &&
		    unit.skill.skill_id != Skill.SKILL_LEADING && !unit.ai_unit)
			unit.aggressive_mode = 0;

		return unitRecno;
	}

	public virtual int mobilize_overseer()
	{
		if (overseer_recno == 0)
			return 0;

		//--------- restore overseer's harmony ---------//

		int overseerRecno = overseer_recno;

		Unit overseer = UnitArray[overseer_recno];

		//-------- if the overseer is a spy -------//

		if (overseer.spy_recno != 0)
			SpyArray[overseer.spy_recno].set_place(Spy.SPY_MOBILE, overseer.sprite_recno);

		//---- cancel the overseer's presence in the town -----//

		if (FirmRes[firm_id].live_in_town)
			TownArray[overseer_town_recno].dec_pop(overseer.race_id, true);

		//----- get this overseer out of the firm -----//

		SpriteInfo spriteInfo = SpriteRes[UnitRes[overseer.unit_id].sprite_id];
		int xLoc = loc_x1, yLoc = loc_y1; // xLoc & yLoc are used for returning results

		bool spaceFound = locate_space(remove_firm, ref xLoc, ref yLoc, loc_x2, loc_y2,
			spriteInfo.loc_width, spriteInfo.loc_height);

		if (spaceFound)
		{
			overseer.init_sprite(xLoc, yLoc);
			overseer.set_mode(0); // reset overseen firm recno
		}
		else
		{
			UnitArray.DeleteUnit(overseer); // delete it when there is no space for the unit
			return 0;
		}

		//--------- reset overseer_recno -------------//

		overseer_recno = 0;
		overseer_town_recno = 0;

		//------- update loyalty -------//

		if (overseerRecno != 0 && !UnitArray.IsDeleted(overseerRecno))
			UnitArray[overseerRecno].update_loyalty();

		return overseerRecno;
	}

	public bool mobilize_builder(int recno)
	{
		//----------- mobilize the builder -------------//
		Unit unit = UnitArray[recno];

		SpriteInfo spriteInfo = unit.sprite_info;
		int xLoc = loc_x1, yLoc = loc_y1;

		if (!locate_space(remove_firm, ref xLoc, ref yLoc, loc_x2, loc_y2,
			    spriteInfo.loc_width, spriteInfo.loc_height, UnitConstants.UNIT_LAND, builder_region_id) &&
		    !World.locate_space(ref xLoc, ref yLoc, loc_x2, loc_y2,
			    spriteInfo.loc_width, spriteInfo.loc_height, UnitConstants.UNIT_LAND, builder_region_id))
		{
			kill_builder(recno);
			return false;
		}

		unit.init_sprite(xLoc, yLoc);
		unit.stop2(); // clear all previously defined action
		unit.set_mode(0);

		//--- set builder to non-aggressive, except ai ---//
		if (!ConfigAdv.firm_mobilize_civilian_aggressive && !unit.ai_unit)
			unit.aggressive_mode = 0;

		return true;
	}

	public void resign_all_worker(bool disappearFlag = false)
	{
		//------- detect buttons on hiring firm workers -------//

		while (workers.Count > 0)
		{
			Worker worker = workers[0];
			int townRecno = worker.town_recno;
			int raceId = worker.race_id;
			int oldWorkerCount = workers.Count;

			if (resign_worker(worker) == 0)
			{
				if (oldWorkerCount == workers.Count)
					break; // no space to resign the worker, keep them in firm
			}

			if (disappearFlag && townRecno != 0)
				TownArray[townRecno].dec_pop(raceId, false);
		}
	}

	public virtual int resign_worker(Worker worker)
	{
		//------- decrease worker no. and create an unit -----//
		int unitRecno = 0;

		if (worker.race_id != 0 && worker.name_id != 0)
			RaceRes[worker.race_id].free_name_id(worker.name_id);

		if (worker.town_recno != 0) // town_recno is 0 if the workers in the firm do not live in towns
		{
			Town town = TownArray[worker.town_recno];

			town.jobless_race_pop_array[worker.race_id - 1]++; // move into jobless population
			town.jobless_population++;

			//------ put the spy in the town -------//

			if (worker.spy_recno != 0)
				SpyArray[worker.spy_recno].set_place(Spy.SPY_TOWN, worker.town_recno);
		}
		else
		{
			unitRecno = create_worker_unit(worker); // if he is a spy, create_worker_unit wil call set_place(SPY_MOBILE)

			if (unitRecno == 0)
				return 0; // return 0 eg there is no space to create the unit
		}

		//------- delete the record from the worker_array ------//

		workers.Remove(worker);

		//TODO rewrite it
		//if (selected_worker_id > workerId || selected_worker_id == worker_count)
		//selected_worker_id--;

		return unitRecno;
	}

	public void reward(int workerId, int remoteAction)
	{
		//if( remoteAction==InternalConstants.COMMAND_PLAYER && remote.is_enable() )
		//{
		//if( !remoteAction && remote.is_enable() )
		//{
		//// packet structure : <firm recno> <worker id>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_FIRM_REWARD, 2*sizeof(short) );
		//*shortPtr = firm_recno;
		//shortPtr[1] = workerId;
		//}
		//}
		//else
		//{
		if (workerId == 0)
		{
			if (overseer_recno != 0)
				UnitArray[overseer_recno].reward(nation_recno);
		}
		else
		{
			workers[workerId - 1].change_loyalty(GameConstants.REWARD_LOYALTY_INCREASE);

			NationArray[nation_recno].add_expense(NationBase.EXPENSE_REWARD_UNIT, GameConstants.REWARD_COST);
		}
		//}
	}

	public void toggle_firm_link(int linkId, bool toggleFlag, int remoteAction, int setBoth = 0)
	{
		//if( !remoteAction && remote.is_enable() )
		//{
		//// packet structure : <firm recno> <link Id> <toggle Flag>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_FIRM_TOGGLE_LINK_FIRM, 3*sizeof(short));
		//shortPtr[0] = firm_recno;
		//shortPtr[1] = linkId;
		//shortPtr[2] = toggleFlag;
		//return;
		//}

		int linkedNationRecno = FirmArray[linked_firm_array[linkId - 1]].nation_recno;

		// if one of the linked end is an indepdendent firm/nation, consider this link as a single nation link
		bool sameNation = linkedNationRecno == nation_recno || linkedNationRecno == 0 || nation_recno == 0;

		if (toggleFlag)
		{
			if ((sameNation && setBoth == 0) || setBoth == 1)
				linked_firm_enable_array[linkId - 1] = InternalConstants.LINK_EE;
			else
				linked_firm_enable_array[linkId - 1] |= InternalConstants.LINK_ED;
		}
		else
		{
			if ((sameNation && setBoth == 0) || setBoth == 1)
				linked_firm_enable_array[linkId - 1] = InternalConstants.LINK_DD;
			else
				linked_firm_enable_array[linkId - 1] &= ~InternalConstants.LINK_ED;
		}

		//---------- if this firm is harbor, set FirmHarbor's parameter link_checked to 0

		if (firm_id == FIRM_HARBOR)
		{
			FirmHarbor harbor = (FirmHarbor)this;
			harbor.link_checked = false;
		}

		//------ set the linked flag of the opposite firm -----//

		Firm firm = FirmArray[linked_firm_array[linkId - 1]];

		//---------- if firm is harbor, set FirmHarbor's parameter link_checked to 0

		if (firm.firm_id == FIRM_HARBOR)
		{
			FirmHarbor harbor = (FirmHarbor)firm;
			harbor.link_checked = false;
		}

		for (int i = 0; i < firm.linked_firm_array.Count; i++)
		{
			if (firm.linked_firm_array[i] == firm_recno)
			{
				if (toggleFlag)
				{
					if ((sameNation && setBoth == 0) || setBoth == 1)
						firm.linked_firm_enable_array[i] = InternalConstants.LINK_EE;
					else
						firm.linked_firm_enable_array[i] |= InternalConstants.LINK_DE;
				}
				else
				{
					if ((sameNation && setBoth == 0) || setBoth == 1)
						firm.linked_firm_enable_array[i] = InternalConstants.LINK_DD;
					else
						firm.linked_firm_enable_array[i] &= ~InternalConstants.LINK_DE;
				}

				break;
			}
		}
	}

	public void toggle_town_link(int linkId, bool toggleFlag, int remoteAction, int setBoth = 0)
	{
		//if( !remoteAction && remote.is_enable() )
		//{
		//// packet structure : <firm recno> <link Id> <toggle Flag>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_FIRM_TOGGLE_LINK_TOWN, 3*sizeof(short));
		//shortPtr[0] = firm_recno;
		//shortPtr[1] = linkId;
		//shortPtr[2] = toggleFlag;
		//return;
		//}

		int linkedNationRecno = TownArray[linked_town_array[linkId - 1]].nation_recno;

		// if one of the linked end is an indepdendent firm/nation, consider this link as a single nation link
		// town cannot decide whether it wants to link to Command Base or not, it is the Command Base which influences the town.
		bool sameNation = linkedNationRecno == nation_recno || firm_id == FIRM_BASE;

		if (toggleFlag)
		{
			if ((sameNation && setBoth == 0) || setBoth == 1)
				linked_town_enable_array[linkId - 1] = InternalConstants.LINK_EE;
			else
				linked_town_enable_array[linkId - 1] |= InternalConstants.LINK_ED;
		}
		else
		{
			if ((sameNation && setBoth == 0) || setBoth == 1)
				linked_town_enable_array[linkId - 1] = InternalConstants.LINK_DD;
			else
				linked_town_enable_array[linkId - 1] &= ~InternalConstants.LINK_ED;
		}

		//------ set the linked flag of the opposite town -----//

		Town town = TownArray[linked_town_array[linkId - 1]];

		for (int i = 0; i < town.linked_firm_array.Count; i++)
		{
			if (town.linked_firm_array[i] == firm_recno)
			{
				if (toggleFlag)
				{
					if ((sameNation && setBoth == 0) || setBoth == 1)
						town.linked_firm_enable_array[i] = InternalConstants.LINK_EE;
					else
						town.linked_firm_enable_array[i] |= InternalConstants.LINK_DE;
				}
				else
				{
					if ((sameNation && setBoth == 0) || setBoth == 1)
						town.linked_firm_enable_array[i] = InternalConstants.LINK_DD;
					else
						town.linked_firm_enable_array[i] &= ~InternalConstants.LINK_DE;
				}

				break;
			}
		}

		//-------- update the town's influence --------//

		if (town.nation_recno == 0)
			town.update_target_resistance();

		//--- redistribute demand if a link to market place has been toggled ---//

		if (firm_id == FIRM_MARKET)
			TownArray.distribute_demand();
	}

	//---------- AI functions ----------//

	public void think_repair()
	{
		Nation ownNation = NationArray[nation_recno];

		//----- check if the damage is serious enough -----//

		// 70% to 95%
		if (hit_points >= max_hit_points * (70 + ownNation.pref_repair_concern / 4) / 100.0)
			return;

		//--- if it's no too heavily damaged, it is just that the AI has a high concern on this ---//

		if (hit_points >= max_hit_points * 80.0 / 100.0)
		{
			if (ownNation.total_jobless_population < 15)
				return;
		}

		//------- queue assigning a construction worker now ------//

		ownNation.add_action(loc_x1, loc_y1, -1, -1, Nation.ACTION_AI_ASSIGN_CONSTRUCTION_WORKER, firm_id);
	}

	public void ai_del_firm()
	{
		if (under_construction)
		{
			cancel_construction(InternalConstants.COMMAND_AI);
		}
		else
		{
			if (can_sell())
				sell_firm(InternalConstants.COMMAND_AI);
			else
				destruct_firm(InternalConstants.COMMAND_AI);
		}
	}

	public bool ai_recruit_worker()
	{
		if (workers.Count == MAX_WORKER)
			return false;

		Nation nation = NationArray[nation_recno];

		for (int i = 0; i < linked_town_array.Count; i++)
		{
			if (linked_town_enable_array[i] != InternalConstants.LINK_EE)
				continue;

			Town town = TownArray[linked_town_array[i]];

			//-- only recruit workers from towns of other nations if we don't have labor ourselves

			if (town.nation_recno != nation_recno && nation.total_jobless_population > MAX_WORKER)
				continue;

			// don't order units to move into it as they will be recruited from the town automatically
			if (town.jobless_population > 0)
				return false;
		}

		//---- order workers to move into the firm ----//

		nation.add_action(loc_x1, loc_y1, -1, -1, Nation.ACTION_AI_ASSIGN_WORKER,
			firm_id, MAX_WORKER - workers.Count);

		return true;
	}

	// whether the AI has excess workers on this firm or not
	public virtual bool ai_has_excess_worker()
	{
		return false;
	}

	public bool think_build_factory(int rawId)
	{
		if (no_neighbor_space) // if there is no space in the neighbor area for building a new firm.
			return false;

		Nation nation = NationArray[nation_recno];

		//--- check whether the AI can build a new firm next this firm ---//

		if (!nation.can_ai_build(FIRM_FACTORY))
			return false;

		//---------------------------------------------------//

		int factoryCount = 0;
		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			Firm firm = FirmArray[linked_firm_array[i]];

			if (firm.firm_id != FIRM_FACTORY || firm.nation_recno != nation_recno)
				continue;

			FirmFactory firmFactory = (FirmFactory)firm;
			if (firmFactory.product_raw_id != rawId)
				continue;

			//--- if one of own factories still has not recruited enough workers ---//

			if (firmFactory.workers.Count < MAX_WORKER)
				return false;

			//---------------------------------------------------//
			//
			// If this factory has a medium to high level of stock,
			// this means the bottleneck is not at the factories,
			// building more factories won't solve the problem.
			//
			//---------------------------------------------------//

			if (firmFactory.stock_qty > firmFactory.max_stock_qty * 0.1)
				return false;

			//---------------------------------------------------//
			//
			// Check if this factory is just outputing goods to
			// a market and it is actually not overcapacity.
			//
			//---------------------------------------------------//

			for (int j = firmFactory.linked_firm_array.Count - 1; j >= 0; j--)
			{
				if (firmFactory.linked_firm_enable_array[j] != InternalConstants.LINK_EE)
					continue;

				Firm linkedFirm = FirmArray[firmFactory.linked_firm_array[j]];

				if (linkedFirm.firm_id != FIRM_MARKET)
					continue;

				//--- if this factory is producing enough goods to the market place, then it means it is still quite efficient

				MarketGoods marketGoods = ((FirmMarket)linkedFirm).market_product_array[rawId - 1];

				if (marketGoods != null && marketGoods.stock_qty > 100)
					return false;
			}

			//----------------------------------------------//

			factoryCount++;
		}

		//---- don't build additional factory if we don't have enough peasants ---//

		if (factoryCount >= 1 && !nation.ai_has_enough_food())
			return false;

		//-- if there isn't much raw reserve left, don't build new factories --//

		if (firm_id == FIRM_MINE)
		{
			if (((FirmMine)this).reserve_qty < 1000 && factoryCount >= 1)
				return false;
		}

		//--- only build additional factories if we have a surplus of labor ---//

		if (nation.total_jobless_population < factoryCount * MAX_WORKER)
			return false;

		//--- only when we have checked it three times and all say it needs a factory, we then build a factory ---//

		if (++ai_should_build_factory_count >= 3)
		{
			int buildXLoc, buildYLoc;

			if (!nation.find_best_firm_loc(FIRM_FACTORY, loc_x1, loc_y1, out buildXLoc, out buildYLoc))
			{
				no_neighbor_space = true;
				return false;
			}

			nation.add_action(buildXLoc, buildYLoc, loc_x1, loc_y1, Nation.ACTION_AI_BUILD_FIRM, FIRM_FACTORY);

			ai_should_build_factory_count = 0;
		}

		return true;
	}

	public virtual bool ai_should_close()
	{
		return false;
	}

	public bool ai_build_neighbor_firm(int firmId)
	{
		Nation nation = NationArray[nation_recno];
		int buildXLoc, buildYLoc;

		if (!nation.find_best_firm_loc(firmId, loc_x1, loc_y1, out buildXLoc, out buildYLoc))
		{
			no_neighbor_space = true;
			return false;
		}

		nation.add_action(buildXLoc, buildYLoc, loc_x1, loc_y1, Nation.ACTION_AI_BUILD_FIRM, firmId);
		return true;
	}

	public virtual void ai_update_link_status()
	{
		if (workers.Count == 0) // if this firm does not need any workers. 
			return;

		if (is_worker_full()) // if this firm already has all the workers it needs. 
			return;

		//------------------------------------------------//

		Nation ownNation = NationArray[nation_recno];
		bool rc = false;

		for (int i = 0; i < linked_town_array.Count; i++)
		{
			Town town = TownArray[linked_town_array[i]];

			//--- enable link to hire people from the town ---//

			// either it's an independent town or it's friendly or allied to our nation
			rc = town.nation_recno == 0 || ownNation.get_relation_status(town.nation_recno) >= NationBase.NATION_FRIENDLY;

			toggle_town_link(i + 1, rc, InternalConstants.COMMAND_AI);
		}
	}

	public bool think_hire_inn_unit()
	{
		if (!NationArray[nation_recno].ai_should_hire_unit(30)) // 30 - importance rating
			return false;

		//---- one firm only hire one foreign race worker ----//

		int foreignRaceCount = 0;
		int majorityRace = majority_race();

		if (majorityRace != 0)
		{
			foreach (var worker in workers)
			{
				if (worker.race_id != majorityRace)
					foreignRaceCount++;
			}
		}

		//-------- try to get skilled workers from inns --------//

		Nation nation = NationArray[nation_recno];
		FirmInn bestInn = null;
		int curRating, bestRating = 0, bestInnUnitId = 0;
		int prefTownHarmony = nation.pref_town_harmony;

		foreach (var innRecNo in nation.ai_inn_array)
		{
			FirmInn firmInn = (FirmInn)FirmArray[innRecNo];

			if (firmInn.region_id != region_id)
				continue;

			for (int j = 0; j < firmInn.inn_unit_array.Count; j++)
			{
				InnUnit innUnit = firmInn.inn_unit_array[j];
				if (innUnit.skill.skill_id != firm_skill_id)
					continue;

				//-------------------------------------------//
				// Rating of a unit to be hired is based on:
				//
				// -distance between the inn and this firm.
				// -whether the unit is racially homogenous to the majority of the firm workers
				//
				//-------------------------------------------//

				curRating = World.distance_rating(center_x, center_y,
					firmInn.center_x, firmInn.center_y);

				curRating += innUnit.skill.skill_level;

				if (majorityRace == UnitRes[innUnit.unit_id].race_id)
				{
					curRating += prefTownHarmony;
				}
				else
				{
					//----------------------------------------------------//
					// Don't pick this unit if it isn't racially homogenous
					// to the villagers, and its pref_town_harmony is higher
					// than its skill level. (This means if its skill level
					// is low, its chance of being selected is lower.
					//----------------------------------------------------//

					if (majorityRace != 0)
					{
						if (foreignRaceCount > 0 || prefTownHarmony > innUnit.skill.skill_level - 50)
							continue;
					}
				}

				if (curRating > bestRating)
				{
					bestRating = curRating;
					bestInn = firmInn;
					bestInnUnitId = j + 1;
				}
			}
		}

		//-----------------------------------------//

		if (bestInn != null)
		{
			int unitRecno = bestInn.hire(bestInnUnitId);

			if (unitRecno != 0)
			{
				UnitArray[unitRecno].assign(loc_x1, loc_y1);
				return true;
			}
		}

		return false;
	}

	public bool think_capture()
	{
		Nation captureNation = null;

		foreach (Nation nation in NationArray)
		{
			if (nation.is_ai() && can_worker_capture(nation.nation_recno))
			{
				captureNation = nation;
				break;
			}
		}

		if (captureNation == null)
			return false;

		//------- do not capture firms of our ally --------//
		if (NationArray[nation_recno].is_ai() && captureNation.get_relation_status(nation_recno) == NationBase.NATION_ALLIANCE)
			return false;

		//------- capture the firm --------//

		capture_firm(captureNation.nation_recno);

		//------ order troops to attack nearby enemy camps -----//

		FirmCamp bestTarget = null;
		int minDistance = Int32.MaxValue;

		foreach (Firm firm in FirmArray)
		{
			//----- only attack enemy camps -----//

			if (firm.nation_recno != nation_recno || firm.firm_id != FIRM_CAMP)
				continue;

			int curDistance = Misc.points_distance(center_x, center_y, firm.center_x, firm.center_y);

			//--- only attack camps within 15 location distance to this firm ---//

			if (curDistance < 15 && curDistance < minDistance)
			{
				minDistance = curDistance;
				bestTarget = (FirmCamp)firm;
			}
		}

		if (bestTarget != null)
		{
			bool useAllCamp = captureNation.pref_military_courage > 60 || Misc.Random(3) == 0;

			captureNation.ai_attack_target(bestTarget.loc_x1, bestTarget.loc_y1,
				bestTarget.total_combat_level(), false, 0, 0, useAllCamp);
		}

		return true;
	}

	public void ai_firm_captured(int capturerNationRecno)
	{
		Nation ownNation = NationArray[nation_recno];

		if (!ownNation.is_ai()) //**BUGHERE
			return;

		if (ownNation.get_relation(capturerNationRecno).status >= NationBase.NATION_FRIENDLY)
			ownNation.ai_end_treaty(capturerNationRecno);

		TalkRes.ai_send_talk_msg(capturerNationRecno, nation_recno, TalkMsg.TALK_DECLARE_WAR);
	}

	protected void recruit_worker()
	{
		if (workers.Count == MAX_WORKER)
			return;

		if (Info.TotalDays % 5 != firm_recno % 5) // update population once 10 days
			return;

		//-------- pull from neighbor towns --------//

		Nation nation = NationArray[nation_recno];

		for (int i = 0; i < linked_town_array.Count; i++)
		{
			if (linked_town_enable_array[i] != InternalConstants.LINK_EE)
				continue;

			Town town = TownArray[linked_town_array[i]];

			//--- don't hire foreign workers if we don't have cash to pay them ---//

			if (nation.cash < 0 && nation_recno != town.nation_recno)
				continue;

			//-------- if the town has any unit ready for jobs -------//

			if (town.jobless_population == 0)
				continue;

			//---- if nation of the town is not hositle to this firm's nation ---//

			if (pull_town_people(town.town_recno, InternalConstants.COMMAND_AUTO))
				return;
		}
	}

	protected void free_worker_room()
	{
		//---- if there is space for one more worker, demote the overseer to worker ----//

		if (workers.Count < MAX_WORKER)
			return;

		//---- if all worker space are full, resign the worst worker to release one worker space for the overseer ----//

		int minWorkerSkill = Int32.MaxValue;
		Worker worstWorker = null;

		foreach (Worker worker in workers)
		{
			if (worker.skill_level < minWorkerSkill)
			{
				minWorkerSkill = worker.skill_level;
				worstWorker = worker;
			}
		}

		if (worstWorker != null)
			resign_worker(worstWorker);
	}

	protected int assign_settle(int raceId, int unitLoyalty, int isOverseer)
	{
		//--- if there is a town of our nation within the effective distance ---//

		int townRecno = find_settle_town();

		if (townRecno != 0)
		{
			TownArray[townRecno].inc_pop(raceId, true, unitLoyalty);
			return townRecno;
		}

		//--- should create a town near the this firm, if there is no other town in the map ---//

		int xLoc = loc_x1, yLoc = loc_y1; // xLoc & yLoc are used for returning results

		// the town must be in the same region as this firm.
		if (World.locate_space(ref xLoc, ref yLoc, loc_x2, loc_y2,
			    InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, UnitConstants.UNIT_LAND, region_id, true))
		{
			if (Misc.rects_distance(xLoc, yLoc, xLoc + InternalConstants.TOWN_WIDTH - 1, yLoc + InternalConstants.TOWN_HEIGHT - 1,
				    loc_x1, loc_y1, loc_x2, loc_y2) <= InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE)
			{
				Town town = TownArray.AddTown(nation_recno, raceId, xLoc, yLoc);

				town.init_pop(raceId, 1, unitLoyalty, true);

				town.auto_set_layout();

				return town.town_recno;
			}
		}

		//---- not able to find a space for a new town within the effective distance ----//

		return 0;
	}

	protected int best_worker_id()
	{
		int bestWorkerId = 0, maxWorkerSkill = 0;
		bool liveInTown = FirmRes[firm_id].live_in_town;

		for (int i = 0; i < workers.Count; i++)
		{
			//--- if the town the worker lives and the firm are of the same nation ---//

			if (!liveInTown || TownArray[workers[i].town_recno].nation_recno == nation_recno)
			{
				if (firm_id == FIRM_CAMP)
				{
					int rankId = workers[i].rank_id;
					if (rankId != Unit.RANK_GENERAL && rankId != Unit.RANK_KING)
						continue;
				}

				if (workers[i].skill_level > maxWorkerSkill)
				{
					maxWorkerSkill = workers[i].skill_level;
					bestWorkerId = i + 1;
				}
			}
		}

		return bestWorkerId;
	}

	protected void calc_productivity()
	{
		productivity = 0.0;

		//------- calculate the productivity of the workers -----------//

		double totalSkill = 0.0;

		foreach (Worker worker in workers)
		{
			totalSkill += Convert.ToDouble(worker.skill_level * worker.hit_points)
			              / Convert.ToDouble(worker.max_hit_points());
		}

		//----- include skill in the calculation ------//

		productivity = totalSkill / MAX_WORKER - sabotage_level;

		if (productivity < 0)
			productivity = 0.0;
	}

	protected void update_worker()
	{
		if (Info.TotalDays % 15 != firm_recno % 15)
			return;

		if (workers.Count == 0)
			return;

		//------- update the worker's para ---------//

		int incValue, levelMinor;

		foreach (Worker worker in workers)
		{
			//------- increase worker skill -----------//

			if (is_operating() && worker.skill_level < 100) // only train when the workers are working
			{
				incValue = Math.Max(10, 100 - worker.skill_level)
					* worker.hit_points / worker.max_hit_points()
					* (100 + worker.skill_potential) / 100 / 2;

				//-------- increase level minor now --------//

				// with random factors, resulting in 75% to 125% of the original
				levelMinor = worker.skill_level_minor + incValue * (75 + Misc.Random(50)) / 100;

				while (levelMinor >= 100)
				{
					levelMinor -= 100;
					worker.skill_level++;
				}

				worker.skill_level_minor = levelMinor;
			}

			//------- increase worker hit points --------//

			int maxHitPoints = worker.max_hit_points();

			if (worker.hit_points < maxHitPoints)
			{
				worker.hit_points += 2; // units in firms recover twice as fast as they are mobile

				if (worker.hit_points > maxHitPoints)
					worker.hit_points = maxHitPoints;
			}
		}

		sort_worker();
	}

	protected void add_income(int incomeType, double incomeAmt)
	{
		cur_year_income += incomeAmt;

		NationArray[nation_recno].add_income(incomeType, incomeAmt, true);
	}

	protected void pay_expense()
	{
		if (nation_recno == 0)
			return;

		Nation nation = NationArray[nation_recno];

		//-------- fixed expenses ---------//

		double dayExpense = FirmRes[firm_id].year_cost / 365.0;

		if (nation.cash >= dayExpense)
		{
			nation.add_expense(NationBase.EXPENSE_FIRM, dayExpense, true);
		}
		else
		{
			if (hit_points > 0)
				hit_points--;

			if (hit_points < 0)
				hit_points = 0.0;

			//--- when the hit points drop to zero and the firm is destroyed ---//

			if (hit_points == 0 && nation_recno == NationArray.player_recno)
				NewsArray.firm_worn_out(firm_recno);
		}

		//----- paying salary to workers from other nations -----//

		if (FirmRes[firm_id].live_in_town)
		{
			int townNationRecno, payWorkerCount = 0;

			for (int i = workers.Count - 1; i >= 0; i--)
			{
				Worker worker = workers[i];
				townNationRecno = TownArray[worker.town_recno].nation_recno;

				if (townNationRecno != nation_recno)
				{
					//--- if we don't have cash to pay the foreign workers, resign them ---//

					if (nation.cash < 0.0)
					{
						resign_worker(worker);
					}
					else //----- pay salaries to the foreign workers now -----//
					{
						payWorkerCount++;

						if (townNationRecno != 0) // the nation of the worker will get income
							NationArray[townNationRecno].add_income(NationBase.INCOME_FOREIGN_WORKER,
								(double)GameConstants.WORKER_YEAR_SALARY / 365.0, true);
					}
				}
			}

			nation.add_expense(NationBase.EXPENSE_FOREIGN_WORKER,
				(double)GameConstants.WORKER_YEAR_SALARY * payWorkerCount / 365.0, true);
		}
	}

	protected void consume_food()
	{
		if (NationArray[nation_recno].food > 0)
		{
			int humanUnitCount = 0;

			foreach (Worker worker in workers)
			{
				if (worker.race_id != 0)
					humanUnitCount++;
			}

			NationArray[nation_recno].consume_food(
				Convert.ToDouble(humanUnitCount * GameConstants.PERSON_FOOD_YEAR_CONSUMPTION) / 365.0);
		}
		else //--- decrease loyalty if the food has been run out ---//
		{
			// decrease 1 loyalty point every 2 days
			if (Info.TotalDays % GameConstants.NO_FOOD_LOYALTY_DECREASE_INTERVAL == 0)
			{
				foreach (Worker worker in workers)
				{
					if (worker.race_id != 0)
						worker.change_loyalty(-1);
				}
			}
		}
	}

	protected void update_loyalty()
	{
		if (FirmRes[firm_id].live_in_town) // only for those who do not live in town
			return;

		//----- update loyalty of the soldiers -----//

		foreach (Worker worker in workers)
		{
			int targetLoyalty = worker.target_loyalty(firm_recno);

			if (targetLoyalty > worker.worker_loyalty)
			{
				int incValue = (targetLoyalty - worker.worker_loyalty) / 10;

				int newLoyalty = (int)worker.worker_loyalty + Math.Max(1, incValue);

				if (newLoyalty > targetLoyalty)
					newLoyalty = targetLoyalty;

				worker.worker_loyalty = newLoyalty;
			}
			else if (targetLoyalty < worker.worker_loyalty)
			{
				worker.worker_loyalty--;
			}
		}
	}

	protected void think_worker_migrate()
	{

		if (workers.Count == 0 || !FirmRes[firm_id].live_in_town)
			return;

		foreach (Town town in TownArray.EnumerateRandom())
		{
			if (town.population >= GameConstants.MAX_TOWN_POPULATION)
				continue;

			//------ check if this town is linked to the current firm -----//

			int j;
			for (j = town.linked_firm_array.Count - 1; j >= 0; j--)
			{
				if (town.linked_firm_array[j] == firm_recno &&
				    town.linked_firm_enable_array[j] != 0)
				{
					break;
				}
			}

			if (j < 0)
				continue;

			//------------------------------------------------//
			//
			// Calculate the attractive factor, it is based on:
			//
			// - the reputation of the target nation (+0 to 100)
			// - the racial harmony of the race in the target town (+0 to 100)
			// - the no. of people of the race in the target town
			// - distance between the current town and the target town (-0 to 100)
			//
			// Attractiveness level range: 0 to 200
			//
			//------------------------------------------------//

			int targetBaseAttractLevel = 0;

			if (town.nation_recno != 0)
				targetBaseAttractLevel += (int)NationArray[town.nation_recno].reputation;

			//---- scan all workers, see if any of them want to worker_migrate ----//

			int workerId = Misc.Random(workers.Count) + 1;

			for (j = 0; j < workers.Count; j++)
			{
				if (++workerId > workers.Count)
					workerId = 1;

				Worker worker = workers[workerId - 1];

				if (worker.town_recno == town.town_recno)
					continue;

				int raceId = worker.race_id;
				Town workerTown = TownArray[worker.town_recno];

				//-- do not migrate if the target town's population of that race is less than half of the population of the current town --//

				if (town.race_pop_array[raceId - 1] < workerTown.race_pop_array[raceId - 1] / 2)
					continue;

				//-- do not migrate if the target town might not be a place this worker will stay --//

				if (ConfigAdv.firm_migrate_stricter_rules &&
				    town.race_loyalty_array[raceId - 1] < 40) // < 40 is considered as negative force
					continue;

				//------ calc the current and target attractiveness level ------//

				int curBaseAttractLevel;
				if (workerTown.nation_recno != 0)
					curBaseAttractLevel = (int)NationArray[workerTown.nation_recno].reputation;
				else
					curBaseAttractLevel = 0;

				int targetAttractLevel = targetBaseAttractLevel + town.race_harmony(raceId);

				if (targetAttractLevel < MIN_MIGRATE_ATTRACT_LEVEL)
					continue;

				// loyalty > 40 is considered as positive force, < 40 is considered as negative force
				int curAttractLevel = curBaseAttractLevel + workerTown.race_harmony(raceId) + (worker.loyalty() - 40);

				if (ConfigAdv.firm_migrate_stricter_rules
					    ? targetAttractLevel - curAttractLevel > MIN_MIGRATE_ATTRACT_LEVEL / 2
					    : targetAttractLevel > curAttractLevel)
				{
					int newLoyalty = Math.Max(GameConstants.REBEL_LOYALTY + 1, targetAttractLevel / 2);

					worker_migrate(workerId, town.town_recno, newLoyalty);
					return;
				}
			}
		}
	}

	protected void worker_migrate(int workerId, int destTownRecno, int newLoyalty)
	{
		Worker worker = workers[workerId - 1];

		int raceId = worker.race_id;
		Town srcTown = TownArray[worker.town_recno];
		Town destTown = TownArray[destTownRecno];

		//------------- add news --------------//

		if (srcTown.nation_recno == NationArray.player_recno || destTown.nation_recno == NationArray.player_recno)
		{
			if (srcTown.nation_recno != destTown.nation_recno) // don't add news for migrating between own towns 
				NewsArray.migrate(srcTown.town_recno, destTownRecno, raceId, 1, firm_recno);
		}

		//--------- migrate now ----------//

		worker.town_recno = destTownRecno;

		//--------- decrease the population of the home town ------//

		srcTown.dec_pop(raceId, true);

		//--------- increase the population of the target town ------//

		destTown.inc_pop(raceId, true, newLoyalty);
	}

	protected void process_independent_town_worker()
	{
		if (Info.TotalDays % 15 != firm_recno % 15)
			return;

		foreach (Worker worker in workers)
		{
			Town town = TownArray[worker.town_recno];

			if (town.nation_recno == 0) // if it's an independent town
			{
				town.race_resistance_array[worker.race_id - 1, nation_recno - 1] -=
					GameConstants.RESISTANCE_DECREASE_PER_WORKER;

				if (town.race_resistance_array[worker.race_id - 1, nation_recno - 1] < 0.0)
					town.race_resistance_array[worker.race_id - 1, nation_recno - 1] = 0.0;
			}
		}
	}

	protected int create_unit(int unitId, int townRecno = 0, bool unitHasJob = false)
	{
		//----look for an empty location for the unit to stand ----//
		//--- scan for the 5 rows right below the building ---//

		SpriteInfo spriteInfo = SpriteRes[UnitRes[unitId].sprite_id];
		int xLoc = loc_x1, yLoc = loc_y1;

		if (!locate_space(remove_firm, ref xLoc, ref yLoc, loc_x2, loc_y2,
			    spriteInfo.loc_width, spriteInfo.loc_height))
			return 0;

		//------------ add the unit now ----------------//

		int unitNationRecno = townRecno != 0 ? TownArray[townRecno].nation_recno : nation_recno;

		Unit unit = UnitArray.AddUnit(unitId, unitNationRecno, Unit.RANK_SOLDIER, 0, xLoc, yLoc);

		//----- update the population of the town ------//

		if (townRecno != 0)
			TownArray[townRecno].dec_pop(unit.race_id, unitHasJob);

		return unit.sprite_recno;
	}

	public int construction_frame() // for under construction only
	{
		FirmBuild firmBuild = FirmRes.get_build(firm_build_id);
		int r = Convert.ToInt32(hit_points * firmBuild.under_construction_bitmap_count / max_hit_points);
		if (r >= firmBuild.under_construction_bitmap_count)
			r = firmBuild.under_construction_bitmap_count - 1;
		return r;
	}
}