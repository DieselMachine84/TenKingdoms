using System;
using System.Collections.Generic;
using System.Linq;

namespace TenKingdoms;

public class Nation : NationBase
{
	public const int ACTION_AI_BUILD_FIRM = 1;
	public const int ACTION_AI_ASSIGN_OVERSEER = 2;
	public const int ACTION_AI_ASSIGN_CONSTRUCTION_WORKER = 3;
	public const int ACTION_AI_ASSIGN_WORKER = 4;
	public const int ACTION_AI_ASSIGN_SPY = 5;
	public const int ACTION_AI_SCOUT = 6;
	public const int ACTION_AI_SETTLE_TO_OTHER_TOWN = 7;
	public const int ACTION_AI_PROCESS_TALK_MSG = 8;
	public const int ACTION_AI_SEA_TRAVEL = 9;
	public const int ACTION_AI_SEA_TRAVEL2 = 10;
	public const int ACTION_AI_SEA_TRAVEL3 = 11;

	public const int SEA_ACTION_SETTLE = 1;
	public const int SEA_ACTION_BUILD_CAMP = 2;
	public const int SEA_ACTION_ASSIGN_TO_FIRM = 3;
	public const int SEA_ACTION_MOVE = 4;
	public const int SEA_ACTION_NONE = 5;

	private List<ActionNode> action_array = new List<ActionNode>();
	private int last_action_id;

	public List<int> ai_town_array = new List<int>();
	public List<int> ai_base_array = new List<int>();
	public List<int> ai_mine_array = new List<int>();
	public List<int> ai_factory_array = new List<int>();
	public List<int> ai_camp_array = new List<int>();
	public List<int> ai_research_array = new List<int>();
	public List<int> ai_war_array = new List<int>();
	public List<int> ai_harbor_array = new List<int>();
	public List<int> ai_market_array = new List<int>();
	public List<int> ai_inn_array = new List<int>();
	public List<int> ai_general_array = new List<int>();
	public List<int> ai_caravan_array = new List<int>();
	public List<int> ai_ship_array = new List<int>();
	public List<AIRegion> ai_region_array = new List<AIRegion>();

	public int ai_base_town_count;

	public int[] firm_should_close_array = new int[Firm.MAX_FIRM_TYPE];

	public int pref_force_projection;
	public int pref_military_development; // pref_military_development + pref_economic_development = 100
	public int pref_economic_development;
	public int pref_inc_pop_by_capture; // pref_inc_pop_by_capture + pref_inc_pop_by_growth = 100
	public int pref_inc_pop_by_growth;
	public int pref_peacefulness;
	public int pref_military_courage;
	public int pref_territorial_cohesiveness;
	public int pref_trading_tendency;
	public int pref_allying_tendency;
	public int pref_honesty;
	public int pref_town_harmony;
	public int pref_loyalty_concern;
	public int pref_forgiveness;
	public int pref_collect_tax;
	public int pref_hire_unit;
	public int pref_use_weapon;
	public int pref_keep_general; // whether to keep currently non-useful the general, or demote them.
	public int pref_keep_skilled_unit; // whether to keep currently non-useful skilled units, or assign them to towns.
	public int pref_diplomacy_retry; // tedency to retry diplomatic actions after previous ones have been rejected.
	public int pref_attack_monster;
	public int pref_spy;
	public int pref_counter_spy;
	public int pref_food_reserve;
	public int pref_cash_reserve;
	public int pref_use_marine;
	public int pref_unit_chase_distance;
	public int pref_repair_concern;
	public int pref_scout;

	//------- AI action vars --------//

	public int ai_capture_enemy_town_recno;
	public DateTime ai_capture_enemy_town_plan_date;
	public DateTime ai_capture_enemy_town_start_attack_date;
	public bool ai_capture_enemy_town_use_all_camp;

	public DateTime ai_last_defend_action_date;

	public int ai_attack_target_x_loc;
	public int ai_attack_target_y_loc;
	public int ai_attack_target_nation_recno; //	nation recno of the target

	public List<AttackCamp> attack_camps = new List<AttackCamp>();
	public int lead_attack_camp_recno; // the firm recno of the lead attacking firm

	private World World => Sys.Instance.World;

	//--------------------------------------------------------------//
	// functions to init. parameters and process ai actions
	//--------------------------------------------------------------//
	public override void init(int nationType, int raceId, int colorSchemeId, int playerId)
	{
		base.init(nationType, raceId, colorSchemeId, playerId);

		//----- init other AI vars -----//

		last_action_id = 0;

		ai_capture_enemy_town_recno = 0;
		ai_capture_enemy_town_start_attack_date = default;
		ai_last_defend_action_date = default;
		ai_base_town_count = 0;

		init_personalty();
	}

	public void init_personalty()
	{
		pref_force_projection = Misc.Random(101);
		pref_military_development = Misc.Random(101);
		pref_economic_development = 100 - pref_military_development;
		pref_inc_pop_by_capture = Misc.Random(101);
		pref_inc_pop_by_growth = 100 - pref_inc_pop_by_capture;
		pref_peacefulness = Misc.Random(101);
		pref_military_courage = Misc.Random(101);
		pref_territorial_cohesiveness = Misc.Random(101);
		pref_trading_tendency = Misc.Random(101);
		pref_allying_tendency = Misc.Random(101);
		pref_honesty = Misc.Random(101);
		pref_town_harmony = Misc.Random(101);
		pref_loyalty_concern = Misc.Random(101);
		pref_forgiveness = Misc.Random(101);
		pref_collect_tax = Misc.Random(101);
		pref_hire_unit = Misc.Random(101);
		pref_use_weapon = Misc.Random(101);
		pref_keep_general = Misc.Random(101);
		pref_keep_skilled_unit = Misc.Random(101);
		pref_diplomacy_retry = Misc.Random(101);
		pref_attack_monster = Misc.Random(101);
		pref_spy = Misc.Random(101);
		pref_counter_spy = Misc.Random(101);
		pref_cash_reserve = Misc.Random(101);
		pref_food_reserve = Misc.Random(101);
		pref_use_marine = Misc.Random(101);
		pref_unit_chase_distance = 15 + Misc.Random(15);
		pref_repair_concern = Misc.Random(101);
		pref_scout = Misc.Random(101);
	}

	public void ProcessAI()
	{
		if (Config.disable_ai_flag)
			return;

		//---- if the king has just been killed ----//

		int nationRecno = nation_recno;

		if (king_unit_recno == 0)
		{
			if (think_succeed_king())
				return;

			if (think_surrender())
				return;

			defeated();
			return;
		}

		//-------- process main AI actions ---------//

		process_ai_main();

		if (NationArray.IsDeleted(nationRecno)) // the nation can have surrendered 
			return;

		//------ process queued diplomatic messges first --------//

		if ((Info.TotalDays - nation_recno) % 3 == 0)
		{
			process_action(null, ACTION_AI_PROCESS_TALK_MSG);

			if (NationArray.IsDeleted(nationRecno)) // the nation can have surrendered 
				return;
		}

		//--------- process queued actions ----------//

		if ((Info.TotalDays - nation_recno) % 3 == 0)
		{
			process_action();

			if (NationArray.IsDeleted(nationRecno)) // the nation can have surrendered 
				return;
		}

		//--- process action that are on-going and need continous checking ---//

		process_on_going_action();

		//----- think about updating relationship with other nations -----//

		if (Info.TotalDays % 360 == nation_recno % 360)
			ai_improve_relation();

		//------ think about surrendering -------//

		if (Info.TotalDays % 60 == nation_recno % 60)
		{
			if (think_surrender())
				return;

			//Do not surrender to unite
			//if (think_unite_against_big_enemy())
			//return;
		}
	}

	public void process_ai_main()
	{
		int[] intervalDaysArray = { 90, 30, 15, 15 };

		int intervalDays = intervalDaysArray[Config.ai_aggressiveness - Config.OPTION_LOW];

		switch ((Info.TotalDays - nation_recno * 4) % intervalDays)
		{
			case 0:
				think_build_firm();
				break;

			case 1:
				think_trading();
				break;

			case 2:
				think_capture();
				break;

			case 3:
				think_explore();
				break;

			case 4: // think about expanding its military force
				think_military();
				break;

			case 5:
				think_secret_attack();
				break;

			case 6:
				think_attack_monster();
				break;

			case 7:
				think_diplomacy();
				break;

			case 8:
				think_marine();
				break;

			case 9:
				think_grand_plan();
				break;

			case 10:
				think_reduce_expense();
				break;

			case 11:
				think_town();
				break;
		}
	}

	public void process_on_going_action()
	{
		//--- if the nation is in the process of trying to capture an enemy town ---//

		if (ai_capture_enemy_town_recno != 0)
		{
			if (Info.TotalDays % 5 == nation_recno % 5)
				think_capturing_enemy_town();
		}

		//----- if the nation is in the process of attacking a target ----//

		//DieselMachine TODO we should pass 0 if we are just moving to the target
		if (attack_camps.Count > 0)
			ai_attack_target_execute(true);
	}

	//---------------------------------------------------------------//
	// main AI thinking functions
	//---------------------------------------------------------------//
	public void think_trading()
	{
		//
	}

	public void think_explore()
	{
		//
	}

	public bool think_capture()
	{
		if (ai_camp_array.Count == 0) // this can happen when a new nation has just emerged
			return false;

		//--- don't capture if the AI is using growth and capture strategy (as opposite to build mine strategy) ---//

		if (ai_mine_array.Count == 0 && total_population < 25)
			return false;

		//-----------------------------------------//

		if (think_capture_independent())
			return true;

		return false;
	}

	protected override void set_relation_status_ai(int nationRecno, int newStatus)
	{
		// cease fire or new friendly/alliance treaty
		if (newStatus == NATION_TENSE || newStatus >= NATION_FRIENDLY)
		{
			if (ai_attack_target_nation_recno == nationRecno)
				reset_ai_attack_target();
		}
	}

	public void ai_improve_relation()
	{
		foreach (Nation nation in NationArray)
		{
			NationRelation nationRelation = get_relation(nation.nation_recno);

			if (nationRelation.status == NATION_HOSTILE)
				continue;

			//--- It improves the AI relation with nations that have trade with us. ---//

			change_ai_relation_level(nation.nation_recno, trade_rating(nation.nation_recno) / 10);

			//--- decrease the started_war_on_us_count once per year, gradually forgiving other nations' wrong doing ---//

			if (nationRelation.started_war_on_us_count > 0 && Misc.Random(5 - pref_forgiveness / 20) > 0)
			{
				nationRelation.started_war_on_us_count--;
			}
		}
	}

	//---------------------------------------------------------------//
	// functions for processing waiting actions
	//---------------------------------------------------------------//
	public int ai_build_firm(ActionNode actionNode)
	{
		//-------- determine the skill id. needed -------//

		int firmId = actionNode.action_para;
		int raceId = actionNode.action_para2;
		int skillId = FirmRes[firmId].firm_skill_id;

		// if the firm does not have a specific skill (e.g. Inn), use the general SKILL_CONSTRUCTION
		if (skillId == 0)
			skillId = Skill.SKILL_CONSTRUCTION;

		//---- if there are camps that should be closed available now, transfer soldiers there to the new camp,
		//ask a construction worker to build the camp so we can transfer the whole troop to the new camp ---//
		if (firmId == Firm.FIRM_CAMP &&
		    ai_has_should_close_camp(World.GetRegionId(actionNode.action_x_loc, actionNode.action_y_loc)))
		{
			skillId = Skill.SKILL_CONSTRUCTION;
		}

		//------------- get a skilled unit --------------//

		Unit skilledUnit = get_skilled_unit(skillId, raceId, actionNode);

		if (skilledUnit == null)
			return 0;

		//------- build the firm now ---------//

		skilledUnit.build_firm(actionNode.action_x_loc, actionNode.action_y_loc, firmId, InternalConstants.COMMAND_AI);

		if (skilledUnit.action_x_loc == actionNode.action_x_loc && skilledUnit.action_y_loc == actionNode.action_y_loc)
		{
			skilledUnit.ai_action_id = actionNode.action_id;
			actionNode.unit_recno = skilledUnit.sprite_recno;

			return 1;
		}
		else
		{
			skilledUnit.stop2();
			return 0;
		}
	}

	public int ai_assign_overseer(ActionNode actionNode)
	{
		//---------------------------------------------------------------------------//
		// cancel action if the firm is deleted, has incorrect firm_id or nation is
		// changed
		//---------------------------------------------------------------------------//

		int firmId = actionNode.action_para;

		if (!check_firm_ready(actionNode.action_x_loc, actionNode.action_y_loc, firmId))
			return -1; // -1 means remove the current action immediately

		Location location = World.GetLoc(actionNode.action_x_loc, actionNode.action_y_loc);

		Firm firm = FirmArray[location.FirmId()];

		//-------- get a skilled unit --------//

		int raceId; // the race of the needed unit

		if (actionNode.action_para2 != 0)
		{
			raceId = actionNode.action_para2;
		}
		else
		{
			if (firm.firm_id == Firm.FIRM_BASE) // for seat of power, the race must be specific
				raceId = FirmRes.get_build(firm.firm_build_id).race_id;
			else
				raceId = firm.majority_race();
		}

		Unit skilledUnit = get_skilled_unit(Skill.SKILL_LEADING, raceId, actionNode);

		if (skilledUnit == null)
			return 0;

		//---------------------------------------------------------------------------//

		if (skilledUnit.rank_id == Unit.RANK_SOLDIER)
			skilledUnit.set_rank(Unit.RANK_GENERAL);

		skilledUnit.assign(actionNode.action_x_loc, actionNode.action_y_loc);
		skilledUnit.ai_action_id = actionNode.action_id;

		actionNode.unit_recno = skilledUnit.sprite_recno;

		return 1;
	}

	public int ai_assign_construction_worker(ActionNode actionNode)
	{
		//---------------------------------------------------------------------------//
		// cancel action if the firm is deleted, has incorrect firm_id or nation is
		// changed
		//---------------------------------------------------------------------------//

		if (!check_firm_ready(actionNode.action_x_loc, actionNode.action_y_loc))
			return -1; // -1 means remove the current action immediately

		//-------- get the poisnter to the firm -------//

		Location location = World.GetLoc(actionNode.action_x_loc, actionNode.action_y_loc);

		Firm firm = FirmArray[location.FirmId()];

		if (firm.builder_recno != 0) // if the firm already has a construction worker
			return -1;

		//-------- get a skilled unit --------//

		Unit skilledUnit = get_skilled_unit(Skill.SKILL_CONSTRUCTION, 0, actionNode);

		if (skilledUnit == null)
			return 0;

		//------------------------------------------------------------------//

		skilledUnit.assign(actionNode.action_x_loc, actionNode.action_y_loc);
		skilledUnit.ai_action_id = actionNode.action_id;

		actionNode.unit_recno = skilledUnit.sprite_recno;

		return 1;
	}

	public int ai_assign_worker(ActionNode actionNode)
	{
		//---------------------------------------------------------------------------//
		// cancel action if the firm is deleted, has incorrect firm_id or nation is
		// changed
		//---------------------------------------------------------------------------//

		int firmId = actionNode.action_para;

		if (!check_firm_ready(actionNode.action_x_loc, actionNode.action_y_loc, firmId))
			return -1; // -1 means remove the current action immediately

		//---------------------------------------------------------------------------//
		// cancel this action if the firm already has enough workers
		//---------------------------------------------------------------------------//

		Location location = World.GetLoc(actionNode.action_x_loc, actionNode.action_y_loc);

		Firm firm = FirmArray[location.FirmId()];
		if (firm.firm_id == Firm.FIRM_CAMP)
		{
			//DieselMachine
			//printf("ai_assign_worker is called for a camp. It is a bug\n");
		}

		if (firm.workers.Count >= Firm.MAX_WORKER)
			return -1;

		// if the firm now has more workers, reduce the number needed to be assigned to the firm
		if (Firm.MAX_WORKER - firm.workers.Count < actionNode.instance_count)
		{
			actionNode.instance_count = Math.Max(actionNode.processing_instance_count + 1,
				Firm.MAX_WORKER - firm.workers.Count);
		}

		//---------------------------------------------------------------------------//
		// firm exists and belongs to our nation. Assign worker to firm
		//---------------------------------------------------------------------------//

		int unitRecno = 0;
		Unit unit = null;

		//----------- use a trained unit --------//

		if (actionNode.unit_recno != 0)
			unit = UnitArray[actionNode.unit_recno];

		//------ recruit on job worker ----------//

		//Seat of power shouldn't call this function at all, as it doesn't handle the racial issue.
		//DieselMachine TODO seat of power actually calls this function
		if (unit == null && firm.firm_id != Firm.FIRM_BASE)
		{
			unitRecno = recruit_on_job_worker(firm, actionNode.action_para2);

			if (unitRecno != 0)
				unit = UnitArray[unitRecno];
		}

		//------- train a unit --------------//

		if (unit == null && firm.firm_id == Firm.FIRM_CAMP &&
		    ai_should_spend(20 + pref_military_development / 2)) // 50 to 70
		{
			if (train_unit(firm.firm_skill_id, firm.majority_race(), actionNode.action_x_loc, actionNode.action_y_loc,
				    out _, actionNode.action_id) != 0)
			{
				actionNode.next_retry_date = Info.game_date.AddDays(GameConstants.TOTAL_TRAIN_DAYS + 1);
				actionNode.retry_count++;
				return 0; // training in process
			}
		}

		//-------- recruit a unit ----------//

		if (unit == null)
		{
			unitRecno = recruit_jobless_worker(firm, actionNode.action_para2);

			if (unitRecno != 0)
				unit = UnitArray[unitRecno];
		}

		if (unit == null)
			return 0;

		//---------------------------------------------------------------------------//

		if (!World.GetLoc(actionNode.action_x_loc, actionNode.action_y_loc).IsFirm()) // firm exists, so assign
			return -1;

		unit.assign(actionNode.action_x_loc, actionNode.action_y_loc);
		unit.ai_action_id = actionNode.action_id;

		return 1;
	}

	public int ai_scout(ActionNode actionNode)
	{
		//------- check if the town is ready --------//

		if (!check_town_ready(actionNode.ref_x_loc, actionNode.ref_y_loc))
			return -1;

		//----------------------------------------------------//
		// stop if no jobless population
		//----------------------------------------------------//

		Location location = World.GetLoc(actionNode.ref_x_loc, actionNode.ref_y_loc);

		Town town = TownArray[location.TownId()]; // point to the old town

		int raceId = town.PickRandomRace(false, true); // 0-don't pick has job unit, 1-pick spies

		if (raceId == 0)
			return -1;

		//---- if cannot recruit because the loyalty is too low ---//

		if (!town.CanRecruit(raceId))
			return 0;

		//------------------------------------------------------//
		// recruit
		//------------------------------------------------------//

		int unitRecno = town.Recruit(-1, raceId, InternalConstants.COMMAND_AI);

		if (unitRecno == 0)
			return 0;

		//---------------------------------------------------------------------------//
		// since it is not an important action, no need to add processing action
		//---------------------------------------------------------------------------//

		Unit unit = UnitArray[unitRecno];

		List<int> selectedArray = new List<int>();
		selectedArray.Add(unitRecno);

		//-- must use UnitArray.move_to() instead of unit.move_to() because the destination may be reachable, it can be in a different region --//

		UnitArray.MoveTo(actionNode.action_x_loc, actionNode.action_y_loc, false, selectedArray, InternalConstants.COMMAND_AI);

		unit.ai_action_id = actionNode.action_id;

		return 1;
	}

	public int ai_settle_to_other_town(ActionNode actionNode)
	{
		//------- check if both towns are ready first --------//

		if (!check_town_ready(actionNode.action_x_loc, actionNode.action_y_loc) ||
		    !check_town_ready(actionNode.ref_x_loc, actionNode.ref_y_loc))
		{
			return -1;
		}

		//----------------------------------------------------//
		// stop if no jobless population
		//----------------------------------------------------//

		Location location = World.GetLoc(actionNode.ref_x_loc, actionNode.ref_y_loc);

		Town town = TownArray[location.TownId()]; // point to the old town

		int raceId = town.PickRandomRace(false, true); // 0-don't pick has job unit, 1-pick spies

		if (raceId == 0)
			return -1;

		//---- if cannot recruit because the loyalty is too low, try reward ---//

		if (!town.CanRecruit(raceId))
		{
			if (!town.HasLinkedOwnCamp) // need overseer to reward
				return 0;

			//DieselMachine TODO use can_recruit instead
			int minRecruitLoyalty = GameConstants.MIN_RECRUIT_LOYALTY + town.RecruitDecLoyalty(raceId, false);

			//--- if cannot recruit because of low loyalty, reward the town people now ---//

			if (town.RacesLoyalty[raceId - 1] < minRecruitLoyalty)
			{
				if (cash > 0 && town.AccumulatedRewardPenalty == 0)
				{
					town.Reward(InternalConstants.COMMAND_AI);
				}

				if (!town.CanRecruit(raceId)) // if still cannot be recruited, return 0 now
					return 0;
			}
			else
			{
				// can't recruit for some other reason
				return 0;
			}
		}

		//------------------------------------------------------//
		// recruit
		//------------------------------------------------------//

		int unitRecno = town.Recruit(-1, raceId, InternalConstants.COMMAND_AI);

		if (unitRecno == 0)
			return 0;

		//---------------------------------------------------------------------------//
		// since it is not an important action, no need to add processing action
		//---------------------------------------------------------------------------//

		Unit unit = UnitArray[unitRecno];

		unit.assign(actionNode.action_x_loc, actionNode.action_y_loc); // assign to the town
		unit.ai_action_id = actionNode.action_id;

		return 1;
	}

	public int ai_assign_spy(ActionNode actionNode)
	{
		if (UnitArray.IsDeleted(actionNode.unit_recno))
			return -1;

		Unit spyUnit = UnitArray[actionNode.unit_recno];

		if (!spyUnit.is_visible()) // it's still under training, not available yet
			return -1;

		if (spyUnit.spy_recno == 0 || spyUnit.true_nation_recno() != nation_recno)
			return -1;

		Spy spy = SpyArray[spyUnit.spy_recno];

		//------ change the cloak of the spy ------//
		Town nearbyTown = null;
		for (int x = spyUnit.next_x_loc() - 1; x <= spyUnit.next_x_loc() + 1; x++)
		{
			for (int y = spyUnit.next_y_loc() - 1; y <= spyUnit.next_y_loc() + 1; y++)
			{
				x = Math.Max(0, x);
				x = Math.Min(GameConstants.MapSize - 1, x);
				y = Math.Max(0, y);
				y = Math.Min(GameConstants.MapSize - 1, y);
				Location location = World.GetLoc(x, y);
				if (location.IsTown())
				{
					nearbyTown = TownArray[location.TownId()];
					goto doublebreak;
				}
			}
		}

		doublebreak:
		spy.notify_cloaked_nation_flag = 0;
		if (nearbyTown != null && nearbyTown.NationId == 0 && spy.cloaked_nation_recno == 0)
		{
			Location location = World.GetLoc(actionNode.action_x_loc, actionNode.action_y_loc);
			if (location.IsTown())
			{
				Town targetTown = TownArray[location.TownId()];
				if (targetTown.NationId != 0 && targetTown.NationId != spyUnit.true_nation_recno())
				{
					spy.notify_cloaked_nation_flag = 1;
				}
			}
		}

		if (!spyUnit.can_spy_change_nation()) // if the spy can't change nation recno now
		{
			int destXLoc = spyUnit.next_x_loc() + Misc.Random(20) - 10;
			int destYLoc = spyUnit.next_y_loc() + Misc.Random(20) - 10;

			destXLoc = Math.Max(0, destXLoc);
			destXLoc = Math.Min(GameConstants.MapSize - 1, destXLoc);

			destYLoc = Math.Max(0, destYLoc);
			destYLoc = Math.Min(GameConstants.MapSize - 1, destXLoc);

			spyUnit.MoveTo(destXLoc, destYLoc);

			actionNode.retry_count++; // never give up
			return 0; // return now and try again later
		}

		spyUnit.spy_change_nation(actionNode.action_para, InternalConstants.COMMAND_AI);

		//------- assign the spy to the target -------//

		if (spy.notify_cloaked_nation_flag == 0)
			spyUnit.assign(actionNode.action_x_loc, actionNode.action_y_loc);
		else
			spyUnit.ai_move_to_nearby_town();

		//----------------------------------------------------------------//
		// Since the spy has already changed its cloaked nation recno
		// we cannot set the ai_action_id of the unit as when it needs
		// to call action_finished() or action_failure() it will
		// use the cloaked nation recno, which is incorrect.
		// So we just return -1, noting that the action has been completed.
		//----------------------------------------------------------------//

		return -1;
	}

	//-----------------------------------------------------------//
	// functions used to update internal parameters
	//-----------------------------------------------------------//

	public void add_town_info(int townRecno)
	{
		ai_town_array.Add(townRecno);

		update_ai_region();
	}

	public void del_town_info(int townRecno)
	{
		//--- if this is a base town, decrease the base town counter ---//

		if (TownArray[townRecno].IsBaseTown)
		{
			ai_base_town_count--;
		}

		ai_town_array.Remove(townRecno);

		update_ai_region();
	}

	public void add_firm_info(int firmId, int firmRecno)
	{
		switch (firmId)
		{
			case Firm.FIRM_BASE:
				ai_base_array.Add(firmRecno);
				break;

			case Firm.FIRM_CAMP:
				ai_camp_array.Add(firmRecno);
				break;

			case Firm.FIRM_FACTORY:
				ai_factory_array.Add(firmRecno);
				break;

			case Firm.FIRM_MARKET:
				ai_market_array.Add(firmRecno);
				break;

			case Firm.FIRM_INN:
				ai_inn_array.Add(firmRecno);
				break;

			case Firm.FIRM_MINE:
				ai_mine_array.Add(firmRecno);
				break;

			case Firm.FIRM_RESEARCH:
				ai_research_array.Add(firmRecno);
				break;

			case Firm.FIRM_WAR_FACTORY:
				ai_war_array.Add(firmRecno);
				break;

			case Firm.FIRM_HARBOR:
				ai_harbor_array.Add(firmRecno);
				break;
		}
	}

	public void del_firm_info(int firmId, int firmRecno)
	{
		switch (firmId)
		{
			case Firm.FIRM_BASE:
				ai_base_array.Remove(firmRecno);
				break;

			case Firm.FIRM_CAMP:
				ai_camp_array.Remove(firmRecno);
				break;

			case Firm.FIRM_FACTORY:
				ai_factory_array.Remove(firmRecno);
				break;

			case Firm.FIRM_MARKET:
				ai_market_array.Remove(firmRecno);
				break;

			case Firm.FIRM_INN:
				ai_inn_array.Remove(firmRecno);
				break;

			case Firm.FIRM_MINE:
				ai_mine_array.Remove(firmRecno);
				break;

			case Firm.FIRM_RESEARCH:
				ai_research_array.Remove(firmRecno);
				break;

			case Firm.FIRM_WAR_FACTORY:
				ai_war_array.Remove(firmRecno);
				break;

			case Firm.FIRM_HARBOR:
				ai_harbor_array.Remove(firmRecno);
				break;
		}
	}

	public List<int> get_firm_array(int firmId)
	{
		switch (firmId)
		{
			case Firm.FIRM_BASE:
				return ai_base_array;

			case Firm.FIRM_CAMP:
				return ai_camp_array;

			case Firm.FIRM_FACTORY:
				return ai_factory_array;

			case Firm.FIRM_MARKET:
				return ai_market_array;

			case Firm.FIRM_INN:
				return ai_inn_array;

			case Firm.FIRM_MINE:
				return ai_mine_array;

			case Firm.FIRM_RESEARCH:
				return ai_research_array;

			case Firm.FIRM_WAR_FACTORY:
				return ai_war_array;

			case Firm.FIRM_HARBOR:
				return ai_harbor_array;
		}

		return null;
	}

	public void add_general_info(int unitRecno)
	{
		ai_general_array.Add(unitRecno);
	}

	public void del_general_info(int unitRecno)
	{
		ai_general_array.Remove(unitRecno);
	}

	public void add_caravan_info(int unitRecno)
	{
		ai_caravan_array.Add(unitRecno);
	}

	public void del_caravan_info(int unitRecno)
	{
		ai_caravan_array.Remove(unitRecno);
	}

	public void add_ship_info(int unitRecno)
	{
		ai_ship_array.Add(unitRecno);
	}

	public void del_ship_info(int unitRecno)
	{
		ai_ship_array.Remove(unitRecno);
	}

	public void update_ai_region()
	{
		ai_region_array.Clear();

		for (int i = 0; i < ai_town_array.Count; i++)
		{
			Town town = TownArray[ai_town_array[i]];

			//---- see if this region has been included -------//

			AIRegion aiRegion = null;

			for (int j = 0; j < ai_region_array.Count; j++)
			{
				if (ai_region_array[j].region_id == town.RegionId)
				{
					aiRegion = ai_region_array[j];
					break;
				}
			}

			if (aiRegion == null) // not included yet
			{
				aiRegion = new AIRegion();
				aiRegion.region_id = town.RegionId;
				ai_region_array.Add(aiRegion);
			}

			//--- increase the town and base_town_count of the nation ---//

			aiRegion.town_count++;

			if (town.IsBaseTown)
				aiRegion.base_town_count++;
		}
	}

	public AIRegion get_ai_region(int regionId)
	{
		for (int i = 0; i < ai_region_array.Count; i++)
		{
			if (ai_region_array[i].region_id == regionId)
				return ai_region_array[i];
		}

		return null;
	}

	public bool has_base_town_in_region(int regionId)
	{
		for (int i = 0; i < ai_region_array.Count; i++)
		{
			if (ai_region_array[i].region_id == regionId)
				return ai_region_array[i].base_town_count > 0;
		}

		return false;
	}

	//-----------------------------------------------------------//
	// functions for building firms
	//-----------------------------------------------------------//
	public void think_build_firm()
	{
		if (!ai_should_build_mine())
			return;

		//----- think building mine --------//

		if (think_build_mine())
			return;

		//---- if there is a mine action currently -----//

		think_destroy_raw_site_guard();
	}

	public bool think_build_mine()
	{
		//------- queue to build the new mine -------//

		//reference location is the raw material location
		int rawId = seek_mine(out int xLoc, out int yLoc, out int refXLoc, out int refYLoc);

		if (rawId == 0)
			return false;

		//--- if we have a mine producing that raw type already ---//

		if (raw_count_array[rawId - 1] > 0)
		{
			if (!ai_should_spend(20)) // then it's not important to build it
				return false;
		}

		//-------------------------------------------//
		// If the map is set to unexplored, wait for a
		// reasonable amount of time before moving out
		// to build the mine.
		//-------------------------------------------//

		if (!Config.explore_whole_map)
		{
			int i;
			for (i = 0; i < ai_town_array.Count; i++)
			{
				Town town = TownArray[ai_town_array[i]];

				int rawDistance = Misc.points_distance(xLoc, yLoc, town.LocCenterX, town.LocCenterY);

				if ((Info.game_date - Info.game_start_date).Days >
				    rawDistance * (5 - Config.ai_aggressiveness) / 5) // 3 to 5 / 5
				{
					break;
				}
			}

			if (i == ai_town_array.Count)
				return false;
		}

		return add_action(xLoc, yLoc, refXLoc, refYLoc, ACTION_AI_BUILD_FIRM, Firm.FIRM_MINE) != null;
	}

	public bool think_destroy_raw_site_guard()
	{
		foreach (Site site in SiteArray)
		{
			//--- if there is already a mine built on this raw site ---//

			if (site.HasMine)
				continue;

			//----- if there is a unit standing on this site -----//

			Location location = World.GetLoc(site.LocX, site.LocY);

			if (!location.HasUnit(UnitConstants.UNIT_LAND))
				continue;

			Unit unit = UnitArray[location.UnitId(UnitConstants.UNIT_LAND)];

			if (unit.cur_action != Sprite.SPRITE_IDLE) // only attack if this unit is idle
				continue;

			if (unit.nation_recno == nation_recno) // don't attack our own units
				continue;

			//------ check if we have a presence in this region ----//

			// ####### patch begin Gilbert 16/3 ########//
			//if( region_array.get_region_stat(site.region_id).base_town_nation_count_array[nation_recno-1] == 0 )
			//	continue;
			if (base_town_count_in_region(site.RegionId) == 0)
				continue;
			// ####### patch end Gilbert 16/3 ########//

			//------ check the relationship with this unit ------//
			//
			// If we are friendly with this nation, don't attack it.
			//
			//---------------------------------------------------//

			if (get_relation_status(unit.nation_recno) >= NATION_FRIENDLY)
				continue;

			//--------- attack the enemy unit ---------//

			if (is_battle(site.LocX, site.LocY) > 0)
				continue;

			int enemyCombatLevel = ai_evaluate_target_combat_level(site.LocX, site.LocY, unit.nation_recno);
			if (ai_attack_target(site.LocX, site.LocY, enemyCombatLevel,
				    false, 0, 0, true))
				return true;
		}

		return false;
	}

	public int ai_supported_inn_count()
	{
		double fixedExpense = fixed_expense_365days();

		int innCount = Convert.ToInt32(cash / 5000.0 * (100 + pref_hire_unit) / 100.0);

		innCount = Math.Min(3, innCount); // maximum 3 inns, minimum 1 inn.

		return Math.Max(1, innCount);
	}

	public bool ai_has_should_close_camp(int regionId)
	{
		//--- if this nation has some firms going to be closed ---//

		if (firm_should_close_array[Firm.FIRM_CAMP - 1] > 0)
		{
			//--- check if any of them are in the same region as the current town ---//

			for (int i = 0; i < ai_camp_array.Count; i++)
			{
				FirmCamp firmCamp = (FirmCamp)FirmArray[ai_camp_array[i]];

				if (firmCamp.should_close_flag && firmCamp.region_id == regionId)
				{
					return true;
				}
			}
		}

		return false;
	}

	public bool ai_should_build_mine()
	{
		//---- only build mines when it has enough population ----//

		if (total_jobless_population < (100 - pref_economic_development) / 2)
			return false;

		// only build mine when you have enough population to support the economic chain: mine + factory + camp
		if (total_jobless_population < 16)
			return false;

		if (SiteArray.UntappedRawCount == 0)
			return false;

		if (!can_ai_build(Firm.FIRM_MINE))
			return false;

		//--- don't build additional mines unless we have enough population and demand to support it ----//

		if (ai_mine_array.Count == 1)
		{
			if (true_profit_365days() < 0 && total_population < 40 + pref_economic_development / 5)
				return false;
		}

		//-- if the nation is already in the process of building a new one --//

		if (is_action_exist(ACTION_AI_BUILD_FIRM, Firm.FIRM_MINE))
			return false;

		//--- if the population is low, make sure existing mines are in full production before building a new one ---//

		if (total_jobless_population < 30)
		{
			for (int i = 0; i < ai_mine_array.Count; i++)
			{
				Firm firm = FirmArray[ai_mine_array[i]];

				if (firm.workers.Count < Firm.MAX_WORKER)
					return false;

				// if the firm does not have any linked firms, that means the firm is still not in full operation
				if (firm.linked_firm_array.Count == 0 && !firm.no_neighbor_space)
					return false;
			}
		}

		return true;
	}

	//-----------------------------------------------------------//
	// functions used to locate position to build firms
	//-----------------------------------------------------------//
	public int seek_mine(out int xLoc, out int yLoc, out int refXLoc, out int refYLoc)
	{
		xLoc = -1;
		yLoc = -1;
		refXLoc = -1;
		refYLoc = -1;

		if (SiteArray.UntappedRawCount == 0)
			return 0;

		int[] raw_kind_mined = new int[GameConstants.MAX_RAW];

		//-----------------------------------------------------------------//
		// count each kind of raw material that is being mined
		//-----------------------------------------------------------------//

		for (int i = 0; i < ai_mine_array.Count; i++)
		{
			FirmMine mine = (FirmMine)FirmArray[ai_mine_array[i]];

			if (mine.raw_id >= 1 && mine.raw_id <= GameConstants.MAX_RAW)
				raw_kind_mined[mine.raw_id - 1]++;
		}

		//-------------------- define parameters ----------------------//

		FirmInfo firmInfo = FirmRes[Firm.FIRM_MINE];
		int[] nearSite = new int[GameConstants.MAX_RAW];
		int[] minDist = new int[GameConstants.MAX_RAW];
		int[] townWithMine = new int[GameConstants.MAX_RAW];
		int[] buildXLoc = new int[GameConstants.MAX_RAW];
		int[] buildYLoc = new int[GameConstants.MAX_RAW];

		for (int i = 0; i < GameConstants.MAX_RAW; i++)
			minDist[i] = Int32.MaxValue;

		//--------------------------------------------//
		// scan for the site array
		//--------------------------------------------//
		foreach (Site site in SiteArray)
		{
			if (site.SiteType != Site.SITE_RAW)
				continue;

			Location siteLoc = World.GetLoc(site.LocX, site.LocY);

			if (!siteLoc.CanBuildFirm())
				continue;

			int siteId = site.ObjectId - 1;

			if (townWithMine[siteId] != 0)
				continue; // a site connected to town is found before

			//--------------------------------------------//
			// continue if action to this site already exist
			//--------------------------------------------//

			if (get_action(-1, -1, site.LocX, site.LocY,
				    ACTION_AI_BUILD_FIRM, Firm.FIRM_MINE) != null)
				continue;

			for (int j = 0; j < ai_town_array.Count; j++)
			{
				Town town = TownArray[ai_town_array[j]];
				Location location = World.GetLoc(town.LocX1, town.LocY1);

				//-********* codes to move to other territory ***********-//
				if (siteLoc.RegionId != location.RegionId)
					continue; // not on the same territory

				int dist = Misc.rects_distance(site.LocX, site.LocY, site.LocX, site.LocY,
					town.LocX1, town.LocY1, town.LocX2, town.LocY2);

				//-------------------------------------------------------------------------//
				// check whether a mine is already connected to this town, if so, use it
				//-------------------------------------------------------------------------//
				bool connected = false;
				for (int k = town.LinkedFirms.Count - 1; k >= 0; k--)
				{
					Firm firm = FirmArray[town.LinkedFirms[k]];

					if (firm.nation_recno == nation_recno && firm.firm_id == Firm.FIRM_MINE)
					{
						connected = true;
						break;
					}
				}

				//-------------------------------------------------------------------------//
				// calculate the minimum distance from own towns
				//-------------------------------------------------------------------------//
				if (dist < minDist[siteId] || (connected && dist <= InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE))
				{
					//------ can build or not ----------//
					bool canBuild = false;

					for (int ix = site.LocX - firmInfo.loc_width + 1; ix <= site.LocX && !canBuild; ix++)
					{
						if (ix < 0 || ix >= GameConstants.MapSize)
							continue;

						for (int iy = site.LocY - firmInfo.loc_height + 1; iy <= site.LocY && !canBuild; iy++)
						{
							if (iy < 0 || iy >= GameConstants.MapSize)
								continue;

							if (World.CanBuildFirm(ix, iy, Firm.FIRM_MINE) != 0)
							{
								canBuild = true;
								buildXLoc[siteId] = ix;
								buildYLoc[siteId] = iy;
								break;
							}
						}
					}

					if (canBuild)
					{
						nearSite[siteId] = site.SiteId;
						minDist[siteId] = dist;

						if (connected && dist <= InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE)
							townWithMine[siteId]++;
					}
				}
			}

			bool allHave = true;
			for (int j = 0; j < GameConstants.MAX_RAW; j++)
			{
				if (townWithMine[j] == 0) //if(!nearSite[j])
				{
					allHave = false;
					break;
				}
			}

			if (allHave)
				break; // sites of each raw material have been found
		}

		//---------------------------------------------------------------------------//
		// determine which raw material is the best choice to build
		// Note: a better sorting algorithm should be used if there are many kind of
		//			raw material
		//---------------------------------------------------------------------------//
		int pos = -1;
		int weight = Int32.MaxValue; // weight is the such kind of material mined, pos is the position in the array
		int siteRecno = 0; // siteRecno is the recno of site to build
		bool withoutThisRaw = false; // withoutThisRaw shows that this raw material is strongly recommended
		int closestDist = Int32.MaxValue;

		for (int j = 0; j < GameConstants.MAX_RAW; j++)
		{
			if (nearSite[j] == 0)
				continue; // no such kind of raw material

			if (raw_kind_mined[j] == 0) // no such kind of material and there is a possible site
			{
				if (withoutThisRaw)
				{
					if (minDist[j] < closestDist) // more than one kind of material we don't have
					{
						siteRecno = nearSite[j];
						closestDist = minDist[j];
						pos = j;
					}
				}
				else
				{
					siteRecno = nearSite[j];
					closestDist = minDist[j];
					withoutThisRaw = true;
					pos = j;
				}
			}
			else if
				(!withoutThisRaw &&
				 weight > raw_kind_mined[j]) // scan for the kind of material with least num of this site
			{
				weight = raw_kind_mined[j];
				pos = j;
			}
		}

		if (siteRecno == 0 && pos >= 0)
			siteRecno = nearSite[pos];

		if (siteRecno != 0)
		{
			Site site = SiteArray[siteRecno];
			xLoc = buildXLoc[pos];
			yLoc = buildYLoc[pos];
			refXLoc = site.LocX;
			refYLoc = site.LocY;

			//--------------------------------------------------------------//
			// do some adjustment such that the firm will be built far away
			// from other firms by at least one step.
			//--------------------------------------------------------------//
			seek_best_build_mine_location(ref xLoc, ref yLoc, site.LocX, site.LocY);

			return site.ObjectId; // the raw id.
		}

		return 0;
	}

	public void seek_best_build_mine_location(ref int xLoc, ref int yLoc, int mapXLoc, int mapYLoc)
	{
		const int MAX_SCORE = 600;

		//--------------- define parameters -----------------//

		FirmInfo firmInfo = FirmRes[Firm.FIRM_MINE];
		int weight = 0, maxWeight = 0;
		int xLeftLimit = mapXLoc - firmInfo.loc_width + 1;
		int yLeftLimit = mapYLoc - firmInfo.loc_height + 1;
		int resultXLoc = xLoc;
		int resultYLoc = yLoc;

		for (int ix = xLeftLimit; ix <= mapXLoc; ix++)
		{
			if (ix < 0 || ix >= GameConstants.MapSize)
				continue;

			for (int iy = yLeftLimit; iy <= mapYLoc; iy++)
			{
				if (iy < 0 || iy >= GameConstants.MapSize)
					continue;

				//---------------------------------------------------------------//
				// remove previous checked and useless locaton
				// Since all the possible location is checked from the top left
				// to the bottom right, the previous checked location should all
				// be impossible to build the mine.
				//---------------------------------------------------------------//
				if (ix < xLoc && iy < yLoc)
					continue;

				if (World.CanBuildFirm(ix, iy, Firm.FIRM_MINE) != 0)
				{
					//----------------------------------------//
					// calculate weight
					//----------------------------------------//
					cal_location_score(ix, iy, firmInfo.loc_width, firmInfo.loc_height, out weight);

					if (weight > maxWeight)
					{
						resultXLoc = ix;
						resultYLoc = iy;

						if (weight == MAX_SCORE) // very good locaton, stop checking
							break;

						maxWeight = weight;
					}
				}
			}
		}

		xLoc = resultXLoc;
		yLoc = resultYLoc;
	}

	public void cal_location_score(int x1, int y1, int width, int height, out int score)
	{
		//-----------------------------------------------------------------//
		//	the score is calculated as follows, for instance, the firm is
		//	2x2 in dimension
		//
		// LU U U RU	L--left, R--right, U--upper, O--lower
		//	 L	x x R
		//	 L	x x R
		// LO O O RO
		//
		//	if any L can build, score += 1, if all set of L can build the total
		// score of this edge is 100. For each corner, the score is 50 if
		// can build.  Thus, the MAX. score == 600
		//
		//-----------------------------------------------------------------//
		int x, y;
		score = 0;

		//---------- left edge ---------//
		if ((x = x1 - 1) >= 0)
		{
			int count = 0;
			for (int i = 0; i < height; i++)
			{
				if (World.GetLoc(x, y1 + i).CanBuildFirm())
					count++;
			}

			score += (count == height) ? 100 : count;
		}

		//---------- upper edge ---------//
		if ((y = y1 - 1) >= 0)
		{
			int count = 0;
			for (int i = 0; i < width; i++)
			{
				if (World.GetLoc(x1 + i, y).CanBuildFirm())
					count++;
			}

			score += (count == width) ? 100 : count;
		}

		//---------- right edge ---------//
		if ((x = x1 + width) < GameConstants.MapSize)
		{
			int count = 0;
			for (int i = 0; i < height; i++)
			{
				if (World.GetLoc(x, y1 + i).CanBuildFirm())
					count++;
			}

			score += (count == height) ? 100 : count;
		}

		//----------- lower edge -----------//

		if ((y = y1 + height) < GameConstants.MapSize)
		{
			int count = 0;
			for (int i = 0; i < width; i++)
			{
				if (World.GetLoc(x1 + i, y).CanBuildFirm())
					count++;
			}

			score += (count == width) ? 100 : count;
		}

		//------------------------------------------//
		// extra score
		//------------------------------------------//

		//------- upper left corner -------//
		if (x1 > 0 && y1 > 0 && World.GetLoc(x1 - 1, y1 - 1).CanBuildFirm())
			score += 50;

		//------- upper right corner ---------//
		if (x1 < GameConstants.MapSize - 1 && y1 > 0 && World.GetLoc(x1 + 1, y1 - 1).CanBuildFirm())
			score += 50;

		//----------- lower left corner ----------//
		if (x1 > 0 && y1 < GameConstants.MapSize - 1 && World.GetLoc(x1 - 1, y1 + 1).CanBuildFirm())
			score += 50;

		//------- lower right corner ---------//
		if (x1 < GameConstants.MapSize - 1 && y1 < GameConstants.MapSize - 1 && World.GetLoc(x1 + 1, y1 + 1).CanBuildFirm())
			score += 50;
	}

	public bool find_best_firm_loc(int buildFirmId, int refXLoc, int refYLoc, out int resultXLoc, out int resultYLoc)
	{
		Location location = World.GetLoc(refXLoc, refYLoc);
		int centerX = -1, centerY = -1;
		int refX1 = -1, refY1 = -1, refX2 = -1, refY2 = -1;
		int xLoc, yLoc;

		//-------- get the refective area ---------//

		int buildRegionId = location.RegionId;
		bool buildIsPlateau = location.IsPlateau();

		if (location.IsFirm())
		{
			int originFirmRecno = location.FirmId();

			Firm firm = FirmArray[originFirmRecno];

			centerX = firm.center_x;
			centerY = firm.center_y;

			refX1 = centerX - InternalConstants.EFFECTIVE_FIRM_FIRM_DISTANCE;
			refY1 = centerY - InternalConstants.EFFECTIVE_FIRM_FIRM_DISTANCE;
			refX2 = centerX + InternalConstants.EFFECTIVE_FIRM_FIRM_DISTANCE;
			refY2 = centerY + InternalConstants.EFFECTIVE_FIRM_FIRM_DISTANCE;

			if (firm.firm_id == Firm.FIRM_HARBOR)
			{
				buildRegionId = ((FirmHarbor)firm).land_region_id;
				buildIsPlateau = false;
			}
		}
		else if (location.IsTown())
		{
			int originTownRecno = location.TownId();

			Town town = TownArray[originTownRecno];

			centerX = town.LocCenterX;
			centerY = town.LocCenterY;

			refX1 = centerX - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE;
			refY1 = centerY - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE;
			refX2 = centerX + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE;
			refY2 = centerY + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE;
		}

		//------------------------------------------------------//

		FirmInfo firmInfo = FirmRes[buildFirmId];
		int firmLocWidth = firmInfo.loc_width;
		int firmLocHeight = firmInfo.loc_height;

		// since we use loc_x1 as the building reference, we need to shift it so it will match the use of center_x in effective distance
		refX1 -= firmLocWidth / 2;
		refY1 -= firmLocHeight / 2;
		refX1 = Math.Max(0, refX1);
		refY1 = Math.Max(0, refY1);

		if (refX2 - firmLocWidth / 2 >= GameConstants.MapSize)
			refX2 = GameConstants.MapSize - 1;
		else
			refX2 -= firmLocWidth / 2;

		if (refY2 - firmLocHeight / 2 >= GameConstants.MapSize)
			refY2 = GameConstants.MapSize - 1;
		else
			refY2 -= firmLocHeight / 2;

		//-------- build a matrix on the refective area ---------//

		int refWidth = refX2 - refX1 + 1, refHeight = refY2 - refY1 + 1;
		int[] refMatrix = new int[refWidth * refHeight];
		int refMatrixIndex;

		//------ initialize the weights of the matrix ------//

		// inner locations in the matrix receives more weights than outer locations do
		for (yLoc = refY1; yLoc <= refY2; yLoc++)
		{
			refMatrixIndex = (yLoc - refY1) * refWidth;

			for (xLoc = refX1; xLoc <= refX2; xLoc++, refMatrixIndex++)
			{
				location = World.GetLoc(xLoc, yLoc);
				int t1 = Math.Abs(xLoc - centerX);
				int t2 = Math.Abs(yLoc - centerY);

				if (location.RegionId != buildRegionId || location.IsPlateau() != buildIsPlateau ||
				    location.IsPowerOff())
				{
					refMatrix[refMatrixIndex] = -1000;
				}
				else
				{
					// it's negative value, and the value is lower for the outer ones
					refMatrix[refMatrixIndex] = 10 - Math.Max(t1, t2);
				}
			}
		}

		//----- calculate weights of the locations in the matrix ----//

		int weightAdd, weightReduce;

		for (yLoc = refY1; yLoc <= refY2; yLoc++)
		{

			for (xLoc = refX1; xLoc <= refX2; xLoc++)
			{
				location = World.GetLoc(refX1, yLoc);
				if (location.RegionId != buildRegionId || location.IsPlateau() != buildIsPlateau ||
				    location.IsPowerOff())
				{
					continue;
				}

				//------- if there is a firm on the location ------//

				weightAdd = 0;
				weightReduce = 0;

				int refBX1 = -1, refBY1 = -1, refBX2 = -1, refBY2 = -1;
				int refCX1 = -1, refCY1 = -1, refCX2 = -1, refCY2 = -1;
				if (location.IsFirm())
				{
					Firm firm = FirmArray[location.FirmId()];

					// only factories & market places need building close to other firms
					if (buildFirmId == Firm.FIRM_MARKET || buildFirmId == Firm.FIRM_FACTORY)
					{
						bool rc = true;

						if (firm.nation_recno != nation_recno)
							rc = false;

						//----- check if the firm is of the right type ----//

						if (buildFirmId == Firm.FIRM_MARKET) // build a market place close to mines and factories
						{
							if (firm.firm_id != Firm.FIRM_MINE && firm.firm_id != Firm.FIRM_FACTORY)
								rc = false;
						}
						else // build a factory close to mines and market places
						{
							if (firm.firm_id != Firm.FIRM_MINE && firm.firm_id != Firm.FIRM_MARKET)
								rc = false;
						}

						//------------------------------------------/

						if (rc)
						{
							refBX1 = firm.center_x - InternalConstants.EFFECTIVE_FIRM_FIRM_DISTANCE;
							refBY1 = firm.center_y - InternalConstants.EFFECTIVE_FIRM_FIRM_DISTANCE;
							refBX2 = firm.center_x + InternalConstants.EFFECTIVE_FIRM_FIRM_DISTANCE;
							refBY2 = firm.center_y + InternalConstants.EFFECTIVE_FIRM_FIRM_DISTANCE;

							weightAdd = 30;
						}
					}

					refCX1 = firm.loc_x1 - 1; // add negative weights on space around this firm
					refCY1 = firm.loc_y1 - 1; // so to prevent firms from building right next to the firm
					refCX2 = firm.loc_x2 + 1; // and leave some space for walking path.
					refCY2 = firm.loc_y2 + 1;

					weightReduce = 20;
				}

				//------- if there is a town on the location ------//

				else if (location.IsTown())
				{
					Town town = TownArray[location.TownId()];

					refBX1 = town.LocCenterX - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE;
					refBY1 = town.LocCenterY - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE;
					refBX2 = town.LocCenterX + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE;
					refBY2 = town.LocCenterY + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE;

					weightAdd = town.Population * 2;

					//----- if the town is not our own -----//

					if (town.NationId != nation_recno)
					{
						if (town.NationId == 0) // it's an independent town
							weightAdd = weightAdd * (100 - town.AverageResistance(nation_recno)) / 100;
						else // more friendly nations get higher weights
						{
							int relationStatus = get_relation_status(town.NationId);

							if (relationStatus >= NATION_NEUTRAL)
								weightAdd = weightAdd * (relationStatus - NATION_NEUTRAL + 1) / 4;
						}
					}

					refCX1 = town.LocX1 - 1; // add negative weights on space around this firm
					refCY1 = town.LocY1 - 1; // so to prevent firms from building right next to the firm
					refCX2 = town.LocX2 + 1; // and leave some space for walking path.
					refCY2 = town.LocY2 + 1;

					weightReduce = 100;
				}
				else
					continue;

				//------ add weights to the matrix ------//

				if (weightAdd != 0)
				{
					for (int yLocB = Math.Max(refY1, refBY1); yLocB <= Math.Min(refY2, refBY2); yLocB++)
					{
						int xLocB = Math.Max(refX1, refBX1);
						refMatrixIndex = (yLocB - refY1) * refWidth + (xLocB - refX1);

						for (; xLocB <= Math.Min(refX2, refBX2); xLocB++)
						{
							refMatrix[refMatrixIndex] += weightAdd;
							refMatrixIndex++;
						}
					}
				}

				//------ reduce weights from the matrix ------//

				if (weightReduce != 0)
				{
					for (int yLocB = Math.Max(refY1, refCY1); yLocB <= Math.Min(refY2, refCY2); yLocB++)
					{
						int xLocB = Math.Max(refX1, refCX1);
						refMatrixIndex = (yLocB - refY1) * refWidth + (xLocB - refX1);

						for (; xLocB <= Math.Min(refX2, refCX2); xLocB++)
						{
							refMatrix[refMatrixIndex] -= weightReduce;
							refMatrixIndex++;
						}
					}
				}
			}
		}

		//------ select the best building site in the matrix -------//

		resultXLoc = -1;
		resultYLoc = -1;

		int bestWeight = 0;

		refX2 -= firmLocWidth - 1; // do not scan beyond the border
		refY2 -= firmLocHeight - 1;

		yLoc = refY1 + Misc.Random(refY2 - refY1 + 1);
		xLoc = refX1 + Misc.Random(refX2 - refX1 + 1);

		for (int yCounter = 0; yCounter < refY2 - refY1 + 1; yCounter++)
		{
			for (int xCounter = 0; xCounter < refX2 - refX1 + 1; xCounter++)
			{
				if (World.GetRegionId(xLoc, yLoc) != buildRegionId ||
				    World.CanBuildFirm(xLoc, yLoc, buildFirmId) == 0)
				{
					continue;
				}

				//---- calculate the average weight of a firm area ----//

				int totalWeight = 0;

				refMatrixIndex = (yLoc - refY1) * refWidth + (xLoc - refX1);

				for (int yCount = 0; yCount < firmLocHeight; yCount++)
				{
					for (int xCount = 0; xCount < firmLocWidth; xCount++)
					{
						totalWeight += refMatrix[refMatrixIndex];
						refMatrixIndex++;
					}

					refMatrixIndex += refWidth - firmLocWidth;
				}

				//------- compare the weights --------//

				var thisWeight = totalWeight / (firmLocWidth * firmLocHeight);

				if (thisWeight > bestWeight)
				{
					bestWeight = thisWeight;

					resultXLoc = xLoc;
					resultYLoc = yLoc;
				}

				xLoc++;
				if (xLoc == refX2 + 1)
					xLoc = refX1;
			}

			yLoc++;
			if (yLoc == refY2 + 1)
				yLoc = refY1;
		}

		return resultXLoc >= 0;
	}

	//------------------------------------------------------------//
	// functions for dealing with the AI action array
	//------------------------------------------------------------//
	public int action_count()
	{
		return action_array.Count;
	}

	public ActionNode get_action(int recNo)
	{
		return action_array[recNo - 1];
	}

	public ActionNode get_action(int actionXLoc, int actionYLoc, int refXLoc, int refYLoc,
		int actionMode, int actionPara, int unitRecno = 0, int checkMode = 0)
	{
		for (int i = action_count(); i > 0; i--)
		{
			ActionNode actionNode = get_action(i);

			if (actionNode.action_mode == actionMode && actionNode.action_para == actionPara)
			{
				// it requests to match the unit recno and it is not matched here
				if (unitRecno != 0 && unitRecno != actionNode.unit_recno)
					continue;

				if (refXLoc >= 0)
				{
					if (checkMode == 0 || checkMode == 2)
					{
						if (actionNode.ref_x_loc == refXLoc && actionNode.ref_y_loc == refYLoc)
							return actionNode;
					}
				}
				else
				{
					if (checkMode == 0 || checkMode == 1)
					{
						if (actionNode.action_x_loc == actionXLoc && actionNode.action_y_loc == actionYLoc)
							return actionNode;
					}
				}
			}
		}

		return null;
	}
	
	private ActionNode add_action(ActionNode actionNode, bool immediateProcess = false)
	{
		//----------- reset some vars -----------//

		actionNode.add_date = Info.game_date;
		actionNode.action_id = ++last_action_id;
		actionNode.retry_count = InternalConstants.STD_ACTION_RETRY_COUNT;

		actionNode.processing_instance_count = 0;
		actionNode.processed_instance_count = 0;

		//------- link into action_array --------//

		action_array.Add(actionNode);

		if (immediateProcess)
			process_action(actionNode);

		return actionNode;
	}

	public ActionNode add_action(int xLoc, int yLoc, int refXLoc, int refYLoc, int actionMode, int actionPara,
		int instanceCount = 1, int unitRecno = 0, int actionPara2 = 0, List<int> groupUnitArray = null)
	{
		//--- check if the action has been added already or not ---//

		if (get_action(xLoc, yLoc, refXLoc, refYLoc, actionMode, actionPara, unitRecno) != null)
			return null;

		//---------- queue the action ----------//

		ActionNode actionNode = new ActionNode();
		actionNode.action_mode = actionMode; // what kind of action
		actionNode.action_para = actionPara; // parameter of the action
		actionNode.action_para2 = actionPara2; // parameter of the action
		actionNode.action_x_loc = xLoc; // location to act to
		actionNode.action_y_loc = yLoc;
		actionNode.ref_x_loc = refXLoc; // the refective location of this action make to
		actionNode.ref_y_loc = refYLoc;
		// number of term to wait before discarding this action
		actionNode.retry_count = InternalConstants.STD_ACTION_RETRY_COUNT;
		actionNode.instance_count = instanceCount; // num of this action being processed in the waiting queue

		bool immediateProcess = false;

		if (groupUnitArray != null)
		{
			actionNode.group_unit_array.Clear();
			actionNode.group_unit_array.AddRange(groupUnitArray);

			immediateProcess = true; // have to execute this command immediately as the unit in unit_array[] may change
			actionNode.retry_count = 1; // only try once as the unit in unit_array[] may change
		}

		if (unitRecno != 0)
		{
			//-- this may happen when the unit is a spy and has just changed cloak --//
			Unit unit = UnitArray[unitRecno];

			if (!NationArray[unit.true_nation_recno()].is_ai() && !NationArray[unit.nation_recno].is_ai())
			{
				return null;
			}

			//-------------------------------------//

			actionNode.unit_recno = unitRecno;

			if (unit.is_visible())
			{
				// have to execute this command immediately as the unit in unit_array[] may change
				immediateProcess = true;
				actionNode.retry_count = 1; // only try once as the unit in unit_array[] may change
			}
			else //--- the unit is still being trained ---//
			{
				actionNode.next_retry_date = Info.game_date.AddDays(GameConstants.TOTAL_TRAIN_DAYS + 1);
			}
		}

		//-------- set action type ---------//

		actionNode.action_type = ActionNode.ACTION_FIXED; // default action type

		//------- link into action_array --------//

		return add_action(actionNode, immediateProcess);
	}

	public void del_action(ActionNode actionNode)
	{
		action_array.Remove(actionNode);
	}

	public bool is_action_exist(int actionMode, int actionPara, int regionId = 0)
	{
		for (int i = action_count(); i > 0; i--)
		{
			ActionNode actionNode = get_action(i);

			if (actionNode.action_mode == actionMode && actionNode.action_para == actionPara)
			{
				if (regionId == 0)
					return true;

				if (World.GetRegionId(actionNode.action_x_loc, actionNode.action_y_loc) == regionId)
					return true;
			}
		}

		return false;
	}

	public bool is_build_action_exist(int firmId, int xLoc, int yLoc)
	{
		for (int i = action_count(); i > 0; i--)
		{
			ActionNode actionNode = get_action(i);

			if (actionNode.action_mode == ACTION_AI_BUILD_FIRM && actionNode.action_para == firmId)
			{
				if (Misc.points_distance(actionNode.action_x_loc, actionNode.action_y_loc,
					    xLoc, yLoc) <= InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE)
				{
					return true;
				}
			}
		}

		return false;
	}

	private bool process_action(ActionNode priorityAction = null, int processActionMode = 0) // waiting --> processing
	{
		bool doneFlag = false;
		int actionRecno, rc = 0;
		int thisSessionProcessCount = 0; // actions processed in this call session

		int divider = 4 - Config.ai_aggressiveness; // the more nations there, the less process count
		int nationRecno = nation_recno;
		int maxSessionProcessCount = 70 / Math.Max(NationArray.nation_count, 1) / Math.Max(divider, 1);

		// if processActionMode has been specific, then all messages in the queue of this type will be processed
		for (actionRecno = 1;
		     actionRecno <= action_count() &&
		     (thisSessionProcessCount < maxSessionProcessCount || processActionMode != 0) && !doneFlag;
		     actionRecno++)
		{
			ActionNode actionNode = get_action(actionRecno);

			//----- priority action ------//

			if (priorityAction != null)
			{
				actionNode = priorityAction;
				doneFlag = true; // mark it done, so if the function "continue" to the next loop, the function will end
			}

			//----- if only process specific action mode -----//

			if (processActionMode != 0 && actionNode.action_mode != processActionMode)
				continue;

			//--- if the AI action is about processing diplomatic message ---//

			if (actionNode.action_mode == ACTION_AI_PROCESS_TALK_MSG && processActionMode != ACTION_AI_PROCESS_TALK_MSG)
			{
				if (Misc.Random(10) > 0) // 1/10 chance of processing the diplomatic messages
					continue;
			}

			//----------------------------------------------//

			if (actionNode.processing_instance_count == actionNode.instance_count)
			{
				//---------------------------------------------//
				//
				// If this action has been marked processing for over 6 months
				// and we still haven't received finishing notifications,
				// then there may be some accidents (or bugs) happened, and
				// we will need to delete the action.
				//
				//---------------------------------------------//

				if (Info.game_date > actionNode.add_date.AddDays(30 * 6))
				{
					//TODO check for bugs
					del_action(actionNode);
					actionRecno--; // stay in this array position as the current one has been deleted, the following one replace the current one's position
				}

				continue;
			}

			// priorityAction bypass retry date checking
			if (Info.game_date < actionNode.next_retry_date && priorityAction == null)
				continue;

			// the actionNode may still exist even when retry_count==0, waiting for processed_count to reach processing_count
			if (actionNode.retry_count == 0)
				continue;

			//-- there is an unprocessing action in this waiting node --//

			switch (actionNode.action_mode)
			{
				case ACTION_AI_BUILD_FIRM:
					rc = ai_build_firm(actionNode);
					break;

				case ACTION_AI_ASSIGN_OVERSEER:
					rc = ai_assign_overseer(actionNode);
					break;

				case ACTION_AI_ASSIGN_CONSTRUCTION_WORKER:
					rc = ai_assign_construction_worker(actionNode);
					break;

				case ACTION_AI_ASSIGN_WORKER:
					rc = ai_assign_worker(actionNode);
					break;

				case ACTION_AI_ASSIGN_SPY:
					rc = ai_assign_spy(actionNode);
					break;

				case ACTION_AI_SCOUT:
					rc = ai_scout(actionNode);
					break;

				case ACTION_AI_SETTLE_TO_OTHER_TOWN:
					rc = ai_settle_to_other_town(actionNode);
					break;

				case ACTION_AI_PROCESS_TALK_MSG:
					rc = ai_process_talk_msg(actionNode);
					break;

				case ACTION_AI_SEA_TRAVEL:
					rc = ai_sea_travel(actionNode);
					break;

				case ACTION_AI_SEA_TRAVEL2:
					rc = ai_sea_travel2(actionNode);
					break;

				case ACTION_AI_SEA_TRAVEL3:
					rc = ai_sea_travel3(actionNode);
					break;
			}

			if (NationArray.IsDeleted(nationRecno)) // diplomatic option can result in surrendering 
				return false;

			//TODO check for bugs
			//actionNode = get_action(actionRecno); // in case an action_array resize invalidated the prior ptr copy

			thisSessionProcessCount++;

			//------ check the return result -------//

			bool delFlag = false;

			if (rc == 1) // the action has been processed, but not sure whether it is complete or not
			{
				actionNode.processing_instance_count++;

				//---------------------------------------------------//
				// for ACTION_DYNAMIC, the action is immediately
				// deleted when processing_instance_count == instance_count.
				//---------------------------------------------------//

				if (actionNode.action_type == ActionNode.ACTION_DYNAMIC)
				{
					if (actionNode.processing_instance_count > actionNode.instance_count)
						delFlag = true;
				}
			}
			else if (rc == 0) // action failed, retry
			{
				actionNode.next_retry_date = Info.game_date.AddDays(7); // try again one week later

				if (--actionNode.retry_count == 0)
					delFlag = true;
			}
			else if (rc == -1) // action failed, remove immediately if return -1
			{
				actionNode.retry_count = 0;
				delFlag = true;
			}

			//-----------------------------------------//

			// if processing_count > processed_count, do not remove this ActionNode, as there are some unit using this actionNode,
			// when they finish or fail the action, processed_count will increase and processing_count will reach processed_count
			if (delFlag && actionNode.processing_instance_count == actionNode.processed_instance_count)
			{
				del_action(actionNode);
				actionRecno--; // stay in this array position as the current one has been deleted, the following one replace the current one's position
			}
		}

		return actionRecno > action_count() || doneFlag;
	}

	public int process_action_id(int actionId)
	{
		for (int i = action_count(); i > 0; i--)
		{
			ActionNode actionNode = get_action(i);
			if (actionNode.action_id == actionId)
			{
				process_action(actionNode);
				return 1;
			}
		}

		return 0;
	}

	public void action_finished(int aiActionId, int unitRecno = 0, int actionFailure = 0)
	{
		//----- locate the ActionNode of this action ------//

		ActionNode targetNode = null;

		//----- try to match actions by unitRecno first ----//

		for (int actionRecno = action_count(); actionRecno > 0; actionRecno--)
		{
			ActionNode actionNode = get_action(actionRecno);

			if (aiActionId == actionNode.action_id)
			{
				targetNode = actionNode;
				break;
			}
		}

		if (targetNode == null) // not found
		{
			stop_unit_action(unitRecno);
			return;
		}

		//------------------------------------------------//
		//
		// In the above condition is true, that means this ship
		// unit has called this function once and the current
		// calling is a duplicated calling.
		//
		//------------------------------------------------//

		bool shouldStop = true;

		// don't reset the unit's ai_action_id in ACTION_AI_SEA_TRAVEL mode as if we reset it,
		// the ship will take new action and won't wait for the units to go aboard.
		if (targetNode.action_mode == ACTION_AI_SEA_TRAVEL)
		{
			if (!UnitArray.IsDeleted(unitRecno) &&
			    UnitRes[UnitArray[unitRecno].unit_id].unit_class == UnitConstants.UNIT_CLASS_SHIP)
			{
				if (targetNode.action_para != 0)
				{
					return;
				}
				else
				{
					targetNode.action_para2 = unitRecno;
					shouldStop = false;
				}
			}
		}

		//---------------------------------------------//
		//
		// Only handle ACTION_FIXED, for ACTION_DYNAMIC,
		// the action is immediately deleted when
		// processing_instance_count == instance_count.
		//
		//---------------------------------------------//

		if (targetNode.action_type != ActionNode.ACTION_FIXED)
		{
			stop_unit_action(unitRecno);
			return;
		}

		//-------------------------------------------------//

		targetNode.processed_instance_count++;

		//---- if all requested instances are processed ----//

		bool allDoneFlag = false;

		if (targetNode.processed_instance_count >= targetNode.instance_count)
			allDoneFlag = true;

		//---- if the action is failed and all the outstanding units are finished, del the action ---//

		else if (targetNode.retry_count == 0 && targetNode.processed_instance_count >= targetNode.processing_instance_count)
		{
			allDoneFlag = true;
		}

		//------- stop the AI actions of the unit -----//

		if (shouldStop)
			stop_unit_action(unitRecno);

		//---- if the action is done, see if there needs to be a following action ----//

		if (allDoneFlag)
		{
			auto_next_action(targetNode);
			del_action(targetNode);
		}
	}

	public void action_failure(int aiActionId, int unitRecno = 0)
	{
		action_finished(aiActionId, unitRecno, 1); // 1 - action failure
	}

	private void auto_next_action(ActionNode nextAction)
	{
		ActionNode actionNode = null;

		switch (nextAction.action_mode)
		{
			case ACTION_AI_SEA_TRAVEL:
			{
				nextAction.action_mode = ACTION_AI_SEA_TRAVEL2;
				// only move one ship, it was previously set to the no. units to aboard the ship
				nextAction.instance_count = 1;
				actionNode = add_action(nextAction, true); // 1-immediate process flag
			}
				break;

			case ACTION_AI_SEA_TRAVEL2:
			{
				nextAction.action_mode = ACTION_AI_SEA_TRAVEL3;
				actionNode = add_action(nextAction, true); // 1-immediate process flag
			}
				break;
		}

		if (actionNode != null)
			process_action(actionNode);
	}

	public void stop_unit_action(int unitRecno)
	{
		//------- stop the AI actions of the unit -----//
		//
		// It is possible that this is not an AI unit as
		// when a player spy cloaked as an enemy unit,
		// the AI will control it.
		//
		//---------------------------------------------//

		if (unitRecno == 0 || UnitArray.IsDeleted(unitRecno))
			return;

		Unit unit = UnitArray[unitRecno];

		unit.ai_action_id = 0;

		//---- if the unit is a ship on the beach and it's mode isn't NO_EXTRA_MOVE, we couldn't call stop2() as that will cause bug ---//

		if (unit.action_mode2 == UnitConstants.ACTION_SHIP_TO_BEACH)
		{
			UnitMarine ship = (UnitMarine)unit;


			if (ship.extra_move_in_beach != UnitMarine.NO_EXTRA_MOVE)
				return;
		}

		//--------------------------------------------------//

		unit.stop2();
		unit.reset_action_misc_para();
	}

	// check whether firm exists and belongs to our nation
	public bool check_firm_ready(int xLoc, int yLoc, int firmId = 0)
	{
		Location location = World.GetLoc(xLoc, yLoc);

		if (!location.IsFirm())
			return false; // no firm there

		int firmRecno = location.FirmId();

		if (FirmArray.IsDeleted(firmRecno))
			return false; // firm deleted

		Firm firm = FirmArray[firmRecno];

		if (firm.nation_recno != nation_recno)
			return false; // firm changed nation

		if (firmId != 0 && firm.firm_id != firmId)
			return false;

		return true;
	}

	public bool check_town_ready(int xLoc, int yLoc) // check whether town exists and belongs to our nation
	{
		Location location = World.GetLoc(xLoc, yLoc);

		if (!location.IsTown())
			return false; // no town there

		int townRecno = location.TownId();

		if (TownArray.IsDeleted(townRecno))
			return false; // town deleted

		Town town = TownArray[townRecno];

		if (town.NationId != nation_recno)
			return false; // town changed nation

		return true;
	}

	//------------------------------------------------------------//
	// functions used to find skilled units
	//------------------------------------------------------------//
	public Unit get_skilled_unit(int skillId, int raceId, ActionNode actionNode)
	{
		Unit skilledUnit;

		if (actionNode.unit_recno != 0) // a unit has started training previously
		{
			if (UnitArray.IsDeleted(actionNode.unit_recno))
				return null; // but unit is lost already and action has failed
			skilledUnit = UnitArray[actionNode.unit_recno];
		}
		else
		{
			int resultFlag;
			int xLoc = -1, yLoc = -1;

			//---- for harbor, we have to get the land region id. instead of the sea region id. ----//

			if (actionNode.action_mode == ACTION_AI_BUILD_FIRM && actionNode.action_para == Firm.FIRM_HARBOR)
			{
				bool rc = false;

				for (yLoc = actionNode.action_y_loc; yLoc < actionNode.action_y_loc + 3; yLoc++)
				{
					for (xLoc = actionNode.action_x_loc; xLoc < actionNode.action_x_loc + 3; xLoc++)
					{
						if (RegionArray.GetRegionInfo(World.GetRegionId(xLoc, yLoc)).RegionType == RegionType.LAND)
						{
							rc = true;
							break;
						}
					}

					if (rc)
						break;
				}
			}
			else
			{
				xLoc = actionNode.action_x_loc;
				yLoc = actionNode.action_y_loc;
			}

			//-----------------------------------------//

			skilledUnit = find_skilled_unit(skillId, raceId, xLoc, yLoc, out resultFlag, actionNode.action_id);

			if (skilledUnit == null) // skilled unit not found
				return null;
		}

		//------ if the unit is still in training -----//

		if (!skilledUnit.is_visible())
		{
			// continue processing this action after this date, this is used when training a unit for construction
			actionNode.next_retry_date = Info.game_date.AddDays(GameConstants.TOTAL_TRAIN_DAYS + 1);
			return null;
		}

		return skilledUnit;
	}

	public Unit find_skilled_unit(int skillId, int raceId, int destX, int destY, out int resultFlag, int actionId = 0)
	{
		resultFlag = 0;

		//----- try to find an existing unit with the required skill -----//

		Unit skilledUnit = null;
		int minDist = Int32.MaxValue;
		int destRegionId = World.GetRegionId(destX, destY);

		foreach (Unit unit in UnitArray)
		{
			if (unit.nation_recno != nation_recno || unit.race_id == 0)
				continue;

			if (raceId != 0 && unit.race_id != raceId)
				continue;

			//---- if this unit is on a mission ----//

			if (unit.home_camp_firm_recno != 0)
				continue;

			if (unit.region_id() != destRegionId)
				continue;

			//----- if this is a mobile unit ------//

			if (unit.is_visible())
			{
				if (!unit.is_ai_all_stop())
					continue;

				if (unit.skill.skill_id == skillId && unit.cur_action != Sprite.SPRITE_ATTACK && unit.ai_action_id == 0)
				{
					int curDist = Misc.points_distance(unit.next_x_loc(), unit.next_y_loc(), destX, destY);

					if (curDist < minDist)
					{
						skilledUnit = unit;
						minDist = curDist;
					}
				}
			}

			//------- if this is an overseer ------//

			else if (skillId == Skill.SKILL_LEADING && unit.unit_mode == UnitConstants.UNIT_MODE_OVERSEE)
			{
				Firm firm = FirmArray[unit.unit_mode_para];

				if (firm.region_id != destRegionId)
					continue;

				if (firm.firm_id == Firm.FIRM_CAMP)
				{
					//--- if this military camp is going to be closed, use this overseer ---//

					if (firm.should_close_flag)
					{
						firm.mobilize_overseer();
						skilledUnit = unit; // pick this overseer
						break;
					}
				}
			}
			else if (skillId == Skill.SKILL_CONSTRUCTION && unit.unit_mode == UnitConstants.UNIT_MODE_CONSTRUCT)
			{
				// the unit is a residental builder for repairing the firm
				Firm firm = FirmArray[unit.unit_mode_para];

				if (!firm.under_construction) // only if the unit is repairing instead of constructing the firm
				{
					// return 1 if the builder is mobilized successfully, 0 if the builder was killed because of out of space on the map
					if (firm.set_builder(0))
					{
						skilledUnit = unit;
						break;
					}
				}
			}
		}

		//---------------------------------------------------//

		if (skilledUnit != null)
		{
			resultFlag = 1;
		}
		else
		{
			//--- if no existing skilled unit found, try to hire one from inns ---//

			// this function will try going with hiring units that are better than training your own ones
			int unitRecno = hire_unit(skillId, raceId, destX, destY);

			if (unitRecno != 0)
			{
				skilledUnit = UnitArray[unitRecno];
				resultFlag = 2;
			}
			else //--- if still cannot get a skilled unit, train one ---//
			{
				int trainTownRecno;

				if (train_unit(skillId, raceId, destX, destY, out trainTownRecno, actionId) != 0)
					resultFlag = 3;
			}
		}

		return skilledUnit;
	}

	public int hire_unit(int skillId, int raceId, int destX, int destY)
	{
		if (!ai_should_hire_unit(20)) // 20 - importance rating
			return 0;

		//-------------------------------------------//

		int bestRating = 0, bestInnRecno = 0, bestInnUnitId = 0;
		int destRegionId = World.GetRegionId(destX, destY);

		for (int i = 0; i < ai_inn_array.Count; i++)
		{
			Firm firm = FirmArray[ai_inn_array[i]];

			if (firm.region_id != destRegionId)
				continue;

			FirmInn firmInn = (FirmInn)firm;

			int innUnitCount = firmInn.inn_unit_array.Count;

			if (innUnitCount == 0)
				continue;


			int curFirmDist = Misc.points_distance(firm.center_x, firm.center_y, destX, destY);

			//------- check units in the inn ---------//

			for (int j = firmInn.inn_unit_array.Count - 1; j >= 0; j--)
			{
				InnUnit innUnit = firmInn.inn_unit_array[j];
				Skill innUnitSkill = innUnit.skill;

				if (innUnitSkill.skill_id == skillId && (raceId != 0 || UnitRes[innUnit.unit_id].race_id == raceId) &&
				    cash >= innUnit.hire_cost)
				{
					//----------------------------------------------//
					// evalute a unit on:
					// -its race, whether it's the same as the nation's race
					// -the inn's distance from the destination
					// -the skill level of the unit.
					//----------------------------------------------//

					int curRating = innUnitSkill.skill_level - (100 - 100 * curFirmDist / GameConstants.MapSize);

					if (UnitRes[innUnit.unit_id].race_id == race_id)
						curRating += 50;

					if (curRating > bestRating)
					{
						bestRating = curRating;

						bestInnRecno = firmInn.firm_recno;
						bestInnUnitId = j;
					}
				}
			}
		}

		//----------------------------------------------------//

		if (bestInnUnitId != 0)
		{
			Firm firm = FirmArray[bestInnRecno];
			FirmInn firmInn = (FirmInn)firm;

			return firmInn.hire(bestInnUnitId);
		}

		return 0;
	}

	public int train_unit(int skillId, int raceId, int destX, int destY, out int trainTownRecno, int actionId = 0)
	{
		trainTownRecno = 0;
		if (skillId != 0 && cash < EXPENSE_TRAIN_UNIT) // training costs money
			return 0;

		//----- locate the best town for training the unit -----//

		int bestRating = 0;
		int destRegionId = World.GetLoc(destX, destY).RegionId;

		for (int i = 0; i < ai_town_array.Count; i++)
		{
			Town town = TownArray[ai_town_array[i]];

			// no jobless population or currently a unit is being trained
			if (town.JoblessPopulation == 0 || town.TrainUnitId != 0 || !town.HasLinkedOwnCamp)
				continue;

			if (town.RegionId != destRegionId)
				continue;

			if (raceId != 0 && town.RacesJoblessPopulation[raceId - 1] <= 0)
				continue;

			//--------------------------------------//

			int curDist = Misc.points_distance(town.LocCenterX, town.LocCenterY, destX, destY);

			int curRating = 100 - 100 * curDist / GameConstants.MapSize;

			if (curRating > bestRating)
			{
				bestRating = curRating;
				trainTownRecno = town.TownId;
			}
		}

		if (trainTownRecno == 0)
			return 0;

		//---------- train the unit ------------//

		Town trainTown = TownArray[trainTownRecno];

		if (raceId == 0)
			raceId = trainTown.PickRandomRace(false, true); // 0-pick jobless units, 1-pick spy units

		if (raceId == 0)
			return 0;

		int unitRecno = trainTown.Recruit(skillId, raceId, InternalConstants.COMMAND_AI);

		if (unitRecno == 0)
			// can happen when training a spy and the selected recruit is an enemy spy
			return 0;

		// set train_unit_action_id so the unit can immediately execute the action when he has finished training.
		trainTown.TrainUnitActionId = actionId;
		return unitRecno;
	}

	public int recruit_jobless_worker(Firm destFirm, int preferedRaceId = 0)
	{
		const int MIN_AI_TOWN_POP = 16;

		int needSpecificRace, raceId; // the race of the needed unit

		if (preferedRaceId != 0)
		{
			raceId = preferedRaceId;
			needSpecificRace = 1;
		}
		else
		{
			if (destFirm.firm_id == Firm.FIRM_BASE) // for seat of power, the race must be specific
			{
				raceId = FirmRes.get_build(destFirm.firm_build_id).race_id;
				needSpecificRace = 1;
			}
			else
			{
				raceId = destFirm.majority_race();
				needSpecificRace = 0;
			}
		}

		if (raceId == 0)
			return 0;

		//----- locate the best town for recruiting the unit -----//

		int bestRating = 0, bestTownRecno = 0;

		for (int i = 0; i < ai_town_array.Count; i++)
		{
			Town town = TownArray[ai_town_array[i]];

			if (town.JoblessPopulation == 0) // no jobless population or currently a unit is being recruited
				continue;

			if (!town.ShouldAIMigrate()) // if the town is going to migrate, disregard the minimum population consideration
			{
				if (town.Population < MIN_AI_TOWN_POP) // don't recruit workers if the population is low
					continue;
			}

			// cannot recruit from this town if there are enemy camps but no own camps
			if (!town.HasLinkedOwnCamp && town.HasLinkedEnemyCamp)
				continue;

			if (town.RegionId != destFirm.region_id)
				continue;

			if (!town.CanRecruitPeople())
				continue;

			//--- get the distance beteween town & the destination firm ---//

			int curDist = Misc.points_distance(town.LocCenterX, town.LocCenterY, destFirm.center_x, destFirm.center_y);

			int curRating = 100 - 100 * curDist / GameConstants.MapSize;

			//--- recruit units from non-base town first ------//

			if (!town.IsBaseTown)
				curRating += 100;

			//---- if the town has the race that the firm needs most ----//

			if (town.CanRecruit(raceId))
			{
				curRating += 50 + (int)town.RacesLoyalty[raceId - 1];
			}
			else
			{
				// if the firm must need this race, don't consider the town if it doesn't have the race.
				if (needSpecificRace != 0)
					continue;
			}

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestTownRecno = town.TownId;
			}
		}

		if (bestTownRecno == 0)
			return 0;

		//---------- recruit the unit ------------//

		Town bestTown = TownArray[bestTownRecno];

		if (bestTown.RecruitableRacePopulation(raceId, true) == 0)
		{
			raceId = bestTown.PickRandomRace(false, true); // 0-pick jobless only, 1-pick spy units

			if (raceId == 0)
				return 0;
		}

		//--- if the chosen race is not recruitable, pick any recruitable race ---//

		if (!bestTown.CanRecruit(raceId))
		{
			//---- if the loyalty is too low to recruit, grant the town people first ---//

			if (cash > 0 && bestTown.AccumulatedRewardPenalty < 10)
				bestTown.Reward(InternalConstants.COMMAND_AI);

			//---- if the loyalty is still too low, return now ----//

			if (!bestTown.CanRecruit(raceId))
				return 0;
		}

		return bestTown.Recruit(-1, raceId, InternalConstants.COMMAND_AI);
	}

	public int recruit_on_job_worker(Firm destFirm, int preferedRaceId = 0)
	{
		if (preferedRaceId == 0)
		{
			preferedRaceId = destFirm.majority_race();

			if (preferedRaceId == 0)
				return 0;
		}

		//--- scan existing firms to see if any of them have excess workers ---//

		List<int> ai_firm_array = get_firm_array(destFirm.firm_id);
		Firm bestFirm = null;
		int bestRating = 0;

		for (int i = 0; i < ai_firm_array.Count; i++)
		{
			Firm firm = FirmArray[ai_firm_array[i]];

			if (firm.firm_recno == destFirm.firm_recno)
				continue;

			if (firm.region_id != destFirm.region_id)
				continue;

			if (!firm.ai_has_excess_worker())
				continue;

			//-----------------------------------//

			int curDistance = Misc.points_distance(firm.center_x, firm.center_y, destFirm.center_x, destFirm.center_y);

			int curRating = 100 - 100 * curDistance / GameConstants.MapSize;

			bool hasHuman = false;

			foreach (Worker worker in firm.workers)
			{
				if (worker.race_id != 0)
					hasHuman = true;

				if (worker.race_id == preferedRaceId)
				{
					//---- can't recruit this unit if he lives in a foreign town ----//

					if (worker.town_recno != 0 && TownArray[worker.town_recno].NationId != nation_recno)
						continue;

					//--------------------------//

					curRating += 100;
					break;
				}
			}

			if (hasHuman && curRating > bestRating)
			{
				bestRating = curRating;
				bestFirm = firm;
			}
		}

		if (bestFirm == null)
			return 0;

		//------ mobilize a worker form the selected firm ----//

		int workerId = 0;

		for (int i = 0; i < bestFirm.workers.Count; i++)
		{
			Worker worker = bestFirm.workers[i];

			//---- can't recruit this unit if he lives in a foreign town ----//

			if (worker.town_recno != 0 && TownArray[worker.town_recno].NationId != nation_recno)
				continue;

			//--------------------------------//

			if (worker.race_id != 0) // if this is a human unit, take it first
				workerId = i + 1;

			if (worker.race_id == preferedRaceId) // if we have a better one, take the better one
			{
				workerId = i + 1;
				break;
			}
		}

		if (workerId == 0) // this can happen if all the workers are foreign workers. 
			return 0;

		return bestFirm.mobilize_worker(workerId, InternalConstants.COMMAND_AI);
	}

	public bool ai_should_hire_unit(int importanceRating)
	{
		if (ai_inn_array.Count == 0)
			return false;

		// -10 to +10 depending on pref_hire_unit
		return ai_should_spend(importanceRating + pref_hire_unit / 5 - 10);
	}

	//------------------------------------------------------------//
	// other functions
	//------------------------------------------------------------//

	public bool can_ai_build(int firmId)
	{
		return cash > FirmRes[firmId].setup_cost;
	}

	public bool think_succeed_king()
	{
		int bestRating = 0;
		Unit bestUnit = null;
		Firm bestFirm = null;
		int bestWorkerId = 0;

		//---- try to find the best successor from mobile units ----//

		foreach (Unit unit in UnitArray)
		{
			if (unit.nation_recno != nation_recno || unit.race_id == 0)
				continue;

			if (!unit.is_visible() && unit.unit_mode != UnitConstants.UNIT_MODE_OVERSEE)
				continue;

			int curRating = 0;

			if (unit.race_id == race_id)
				curRating += 50;

			if (unit.rank_id == Unit.RANK_GENERAL)
				curRating += 50;

			if (unit.skill.skill_id == Skill.SKILL_LEADING)
				curRating += unit.skill.skill_level;

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestUnit = unit;
			}
		}

		//---- try to find the best successor from military camps ----//

		foreach (Firm firm in FirmArray)
		{
			if (firm.nation_recno != nation_recno)
				continue;

			//------ only military camps -------//

			if (firm.firm_id == Firm.FIRM_CAMP)
			{
				for (int i = 0; i < firm.workers.Count; i++)
				{
					Worker worker = firm.workers[i];
					if (worker.race_id == 0)
						continue;

					int curRating = 0;

					if (worker.race_id == race_id)
						curRating += 50;

					if (worker.rank_id == Unit.RANK_GENERAL)
						curRating += 50;

					if (worker.skill_id == Skill.SKILL_LEADING)
						curRating += worker.skill_level;

					if (curRating > bestRating)
					{
						bestRating = curRating;
						bestUnit = null;
						bestFirm = firm;
						bestWorkerId = i + 1;
					}
				}
			}
		}

		//------- if the best successor is a mobile unit -------//

		if (bestUnit != null)
		{
			//-- if the unit is in a command base or seat of power, mobilize it --//

			if (!bestUnit.is_visible())
			{
				FirmArray[bestUnit.unit_mode_para].mobilize_overseer();
			}

			//---------- succeed the king -------------//

			if (bestUnit.is_visible()) // it may still be not visible if there is no space for the unit to be mobilized
			{
				// if this is a spy and he's our spy
				if (bestUnit.spy_recno != 0 && bestUnit.true_nation_recno() == nation_recno)
					SpyArray[bestUnit.spy_recno].drop_spy_identity(); // revert the spy to a normal unit

				succeed_king(bestUnit);
				return true;
			}
		}

		//------- if the best successor is a soldier in a camp -------//

		if (bestFirm != null)
		{
			int unitRecno = bestFirm.mobilize_worker(bestWorkerId, InternalConstants.COMMAND_AI);

			if (unitRecno != 0)
			{
				succeed_king(UnitArray[unitRecno]);
				return true;
			}
		}

		//--- if stil not found here, then try to locate the successor from villages ---//

		foreach (Town town in TownArray)
		{
			if (town.NationId != nation_recno)
				continue;

			// if this town has people with the same race as the original king
			if (town.RecruitableRacePopulation(race_id, false) > 0)
			{
				Unit unit = town.MobilizeTownPeople(race_id, true, false);

				if (unit != null)
				{
					succeed_king(unit);
					return true;
				}
			}
		}

		return false;
	}

	public int closest_enemy_firm_distance(int firmId, int xLoc, int yLoc)
	{
		int minDistance = Int32.MaxValue;

		foreach (Firm firm in FirmArray)
		{
			// belonging to own nation, not enemy nation
			if (firm.firm_id != firmId || firm.nation_recno == nation_recno)
				continue;

			int curDistance = Misc.points_distance(firm.center_x, firm.center_y, xLoc, yLoc);

			if (curDistance < minDistance)
				minDistance = curDistance;
		}

		return minDistance;
	}

	//------------------------------------------------------------//
	// military related functions
	//------------------------------------------------------------//

	public void think_military()
	{
		//Do not think about military expanding too early because we can move our first town
		if (Info.game_date < Info.game_start_date.AddDays(180))
			return;

		//---- don't build new camp if we our food consumption > production ----//

		if (yearly_food_change() < 0)
		{
			think_close_camp(); // think about closing down an existing one
			return;
		}

		//--- think about whether it should expand now ---//

		if (!ai_should_expand_military() && !ai_is_troop_need_new_camp())
			return;

		//----- think about where to expand -----//

		int bestRating = 0;
		Town bestTown = null;

		for (int i = ai_town_array.Count - 1; i >= 0; i--)
		{
			Town town = TownArray[ai_town_array[i]];

			if (!town.IsBaseTown) // only expand on base towns
				continue;

			if (town.NoNeighborSpace) // if there is no space in the neighbor area for building a new firm.
				continue;

			int curRating = town.Population; //**BUGHERE, to be modified.

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestTown = town;
			}
		}

		if (bestTown == null)
			return;

		//--------- queue building the camp now ---------//

		int buildXLoc, buildYLoc;

		if (!find_best_firm_loc(Firm.FIRM_CAMP, bestTown.LocX1, bestTown.LocY1,
			    out buildXLoc, out buildYLoc))
		{
			bestTown.NoNeighborSpace = true;
			return;
		}

		add_action(buildXLoc, buildYLoc, bestTown.LocX1, bestTown.LocY1,
			ACTION_AI_BUILD_FIRM, Firm.FIRM_CAMP);
	}

	public bool think_secret_attack()
	{
		//--- never secret attack if its peacefulness >= 80 ---//

		if (pref_peacefulness >= 80)
			return false;

		//--- don't try to get new enemies if we already have many ---//

		int totalEnemyMilitary = total_enemy_military();

		if (totalEnemyMilitary > 20 + pref_military_courage - pref_peacefulness)
			return false;

		//---------------------------------------------//

		int bestRating = 0;
		int bestNationRecno = 0;
		int ourMilitary = military_rank_rating();

		foreach (Nation nation in NationArray)
		{
			if (nation.nation_recno == nation_recno)
				continue;

			NationRelation nationRelation = get_relation(nation.nation_recno);

			// existing trade and possible trade
			int tradeRating = trade_rating(nation.nation_recno) / 2 + ai_trade_with_rating(nation.nation_recno) / 2;

			//---- if the secret attack flag is not enabled yet ----//

			if (!nationRelation.ai_secret_attack)
			{
				int relationStatus = nationRelation.status;

				//---- if we have a friendly treaty with this nation ----//

				if (relationStatus == NATION_FRIENDLY)
				{
					if (totalEnemyMilitary > 0) // do not attack if we still have enemies
						continue;
				}

				//-------- never attacks an ally ---------//

				else if (relationStatus == NATION_ALLIANCE)
				{
					continue;
				}

				//---- don't attack if we have a big trade volume with the nation ---//

				if (tradeRating > (50 - pref_trading_tendency / 2)) // 0 to 50, 0 if trade tendency is 100, it is 0
				{
					continue;
				}
			}

			//--------- calculate the rating ----------//

			int curRating = (ourMilitary - nation.military_rank_rating() / 2) * 2
			                + (overall_rank_rating() - 50) // if <50 negative, if >50 positive
			                - tradeRating * 2
			                - nationRelation.ai_relation_level / 2
			                - pref_peacefulness / 2;

			//------- if aggressiveness config is medium or high ----//

			if (!nation.is_ai()) // more aggressive towards human players
			{
				switch (Config.ai_aggressiveness)
				{
					case Config.OPTION_MODERATE:
						curRating += 100;
						break;

					case Config.OPTION_HIGH:
						curRating += 300;
						break;

					case Config.OPTION_VERY_HIGH:
						curRating += 500;
						break;
				}
			}

			//----- if the secret attack is already on -----//

			if (nationRelation.ai_secret_attack)
			{
				//--- cancel secret attack if the situation has changed ---//

				if (curRating < 0)
				{
					nationRelation.ai_secret_attack = false;
					continue;
				}
			}

			//--------- compare ratings -----------//

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestNationRecno = nation.nation_recno;
			}
		}

		//-------------------------------//

		if (bestNationRecno != 0)
		{
			get_relation(bestNationRecno).ai_secret_attack = true;
			return true;
		}

		return false;
	}

	public bool think_close_camp()
	{
		return false;
	}

	public bool ai_attack_target(int targetXLoc, int targetYLoc, int targetCombatLevel, bool justMoveToFlag = false,
		int attackerMinCombatLevel = 0, int leadAttackCampRecno = 0, bool useAllCamp = false)
	{
		//if (get_target_nation_recno(targetXLoc, targetYLoc) == 1)
		//return 0;

		//--- if the AI is already on an attack mission ---//
		if (attack_camps.Count > 0)
			return false;

		int targetRegionId = World.GetLoc(targetXLoc, targetYLoc).RegionId;
		ai_attack_target_x_loc = targetXLoc;
		ai_attack_target_y_loc = targetYLoc;
		ai_attack_target_nation_recno = get_target_nation_recno(targetXLoc, targetYLoc);

		//------- if there is a pre-selected camp -------//
		lead_attack_camp_recno = leadAttackCampRecno;
		if (leadAttackCampRecno != 0)
		{

			FirmCamp firmCamp = (FirmCamp)FirmArray[leadAttackCampRecno];
			AttackCamp attackCamp = new AttackCamp();
			attackCamp.firm_recno = leadAttackCampRecno;
			attackCamp.combat_level = firmCamp.total_combat_level();
			attackCamp.distance = Misc.points_distance(firmCamp.center_x, firmCamp.center_y, targetXLoc, targetYLoc);
			attack_camps.Add(attackCamp);
		}

		List<int> protectionCamps = new List<int>();
		for (int i = 0; i < ai_town_array.Count; i++)
		{
			Town town = TownArray[ai_town_array[i]];

			if (town.RegionId != targetRegionId)
				continue;

			town.AddProtectionCamps(protectionCamps, useAllCamp);
		}

		for (int i = 0; i < ai_camp_array.Count; i++)
		{
			int firmRecno = ai_camp_array[i];
			FirmCamp firmCamp = (FirmCamp)FirmArray[firmRecno];

			if (firmCamp.region_id != targetRegionId)
				continue;

			bool isProtectionCamp = false;
			for (int j = 0; j < protectionCamps.Count; j++)
			{
				if (protectionCamps[j] == firmRecno)
				{
					isProtectionCamp = true;
					break;
				}
			}

			if (isProtectionCamp)
				continue;

			if (firmCamp.ai_is_capturing_independent_village()) // the camp is trying to capture an independent town
				continue;

			if (firmCamp.is_attack_camp) // the camp is on another attack mission already
				continue;

			if (leadAttackCampRecno != 0 && firmRecno == leadAttackCampRecno) // the camp was already added before
				continue;

			if (attackerMinCombatLevel > 0 && firmCamp.average_combat_level() < attackerMinCombatLevel)
				continue;

			AttackCamp attackCamp = new AttackCamp();
			attackCamp.firm_recno = firmRecno;
			attackCamp.combat_level = firmCamp.total_combat_level();
			attackCamp.distance = Misc.points_distance(firmCamp.center_x, firmCamp.center_y, targetXLoc, targetYLoc);
			attack_camps.Add(attackCamp);
		}

		//---- now we get all suitable camps in the list, it's time to attack ---//
		//----- think about which ones in the list should be used -----//
		//--- try to send troop with maxTargetCombatLevel, and don't send troop if available combat level < minTargetCombatLevel ---//
		int minTargetCombatLevel = targetCombatLevel * (125 + pref_force_projection / 4) / 100; // 125% to 150%
		int maxTargetCombatLevel = targetCombatLevel * (150 + pref_force_projection / 2) / 100; // 150% to 200%

		//--- first calculate the total combat level of these camps ---//
		int totalCombatLevel = 0;
		foreach (AttackCamp attackCamp in attack_camps)
			totalCombatLevel += attackCamp.combat_level;

		//--- see if we are not strong enough to attack yet -----//
		if (totalCombatLevel < minTargetCombatLevel)
		{
			attack_camps.Clear();
			return false;
		}

		//TODO rewrite
		//---- now sort the camps based on their distances & combat levels ----//
		//qsort(&attack_camp_array, attack_camp_count, sizeof(attack_camp_array[0]), sort_attack_camp_function);

		//----- now take out the lowest rating ones -----//
		/*for (int i = attack_camp_count - 1; i >= 0; i--)
		{
			//----- if camps are close to the target use all of them -----//
			if (attack_camp_array[attack_camp_count].distance < MAX_WORLD_X_LOC / 3)
				break;
	
			if (totalCombatLevel - attack_camp_array[i].combat_level > maxTargetCombatLevel)
			{
				totalCombatLevel -= attack_camp_array[i].combat_level;
				attack_camp_count--;
			}
		}*/

		//------- declare war with the target nation -------//

		if (ai_attack_target_nation_recno != 0)
			TalkRes.ai_send_talk_msg(ai_attack_target_nation_recno, nation_recno, TalkMsg.TALK_DECLARE_WAR);

		//------- synchronize the attack date for different camps ----//
		ai_attack_target_sync();

		ai_attack_target_execute(!justMoveToFlag);

		return true;
	}

	public void ai_attack_target_sync()
	{
		//---- find the distance of the camp that is farest to the target ----//

		int maxDistance = 0;

		foreach (AttackCamp attackCamp in attack_camps)
		{
			if (attackCamp.distance > maxDistance)
				maxDistance = attackCamp.distance;
		}

		int maxTravelDays = SpriteRes[UnitRes[UnitConstants.UNIT_NORMAN].sprite_id].travel_days(maxDistance);

		//------ set the date which the troop should start moving -----//

		for (int i = 0; i < attack_camps.Count; i++)
		{
			int travelDays = maxTravelDays * attack_camps[i].distance / maxDistance;

			attack_camps[i].patrol_date = Info.game_date.AddDays(maxTravelDays - travelDays);
			//use distance to store attack date, but we need to store date diff instead because distance is of short type
			//TODO rewrite
			attack_camps[i].distance = (Info.game_date.AddDays(maxTravelDays) - Info.game_start_date).Days / 10;
		}

		//----- set the is_attack_camp flag of the camps ------//

		for (int i = 0; i < attack_camps.Count; i++)
		{
			Firm firm = FirmArray[attack_camps[i].firm_recno];
			((FirmCamp)firm).is_attack_camp = true;
		}
	}

	public void ai_attack_target_execute(bool directAttack)
	{
		//---- if the target no longer exist -----//

		//DieselMachine TODO need better check for reset
		if (ai_attack_target_nation_recno != get_target_nation_recno(ai_attack_target_x_loc, ai_attack_target_y_loc))
		{
			reset_ai_attack_target();
		}

		//Attack date came. Let them fight, now we can prepare for the next attack
		//restore attack date from distance
		if (attack_camps.Count > 0 && Info.game_date > Info.game_start_date.AddDays(attack_camps[0].distance * 10))
		{
			reset_ai_attack_target();
		}

		//---- if enemy forces came and we need to cancel our attack -----//

		if (Info.TotalDays % 5 == nation_recno % 5)
		{
			int targetCombatLevel = ai_evaluate_target_combat_level(ai_attack_target_x_loc, ai_attack_target_y_loc,
				ai_attack_target_nation_recno);
			int ourCombatLevel = 0;
			for (int i = 0; i < attack_camps.Count; i++)
			{
				ourCombatLevel += attack_camps[i].combat_level;
			}

			//DieselMachine TODO this code is duplicated with ai_attack_target()
			int minTargetCombatLevel = targetCombatLevel * (125 + pref_force_projection / 4) / 100; // 125% to 150%
			if (ourCombatLevel < minTargetCombatLevel)
			{
				for (int i = 0; i < attack_camps.Count; i++)
				{
					int firmRecno = attack_camps[i].firm_recno;
					if (!FirmArray.IsDeleted(firmRecno))
					{
						if (attack_camps[i].patrol_date == default(DateTime)) //troop is patrolling, call them back
						{
							FirmCamp firmCamp = (FirmCamp)FirmArray[firmRecno];
							firmCamp.validate_patrol_unit();
							if (firmCamp.patrol_unit_array.Count > 0)
							{
								UnitArray.assign_to_camp(firmCamp.loc_x1, firmCamp.loc_y1,
									InternalConstants.COMMAND_AI, firmCamp.patrol_unit_array);
							}
						}
					}
				}

				reset_ai_attack_target();
			}
		}

		//---- send camps to the target -----//

		for (int i = 0; i < attack_camps.Count; i++)
		{
			//----- if camp was sent already or if it's still not the date to move to attack ----//
			if (attack_camps[i].patrol_date == default(DateTime) || Info.game_date < attack_camps[i].patrol_date)
				continue;

			attack_camps[i].patrol_date = default(DateTime);

			int firmRecno = attack_camps[i].firm_recno;
			if (FirmArray.IsDeleted(firmRecno))
				continue;

			FirmCamp firmCamp = (FirmCamp)FirmArray[firmRecno];

			if (firmCamp.overseer_recno != 0 || firmCamp.workers.Count > 0)
			{
				//--- if this is the lead attack camp, don't mobilize the overseer ---//

				if (lead_attack_camp_recno == firmRecno)
					firmCamp.patrol_all_soldier(); // don't mobilize the overseer
				else
					firmCamp.patrol(); // mobilize the overseer and the soldiers

				//----------------------------------------//

				firmCamp.validate_patrol_unit();
				// there could be chances that there are no some for mobilizing the units
				if (firmCamp.patrol_unit_array.Count > 0)
				{
					//------- declare war with the target nation -------//

					//if( ai_attack_target_nation_recno )
					//talk_res.ai_send_talk_msg(ai_attack_target_nation_recno, nation_recno, TalkMsg.TALK_DECLARE_WAR);

					//--- in defense mode, just move close to the target, the unit will start attacking themselves as their relationship is hostile already ---//

					if (!directAttack)
					{
						UnitArray.MoveTo(ai_attack_target_x_loc, ai_attack_target_y_loc, false,
							firmCamp.patrol_unit_array, InternalConstants.COMMAND_AI);
					}
					else
					{
						//-------- set should_attack on the target to 1 --------//

						enable_should_attack_on_target(ai_attack_target_x_loc, ai_attack_target_y_loc);

						//---------- attack now -----------//

						UnitArray.attack(ai_attack_target_x_loc, ai_attack_target_y_loc, false,
							firmCamp.patrol_unit_array, InternalConstants.COMMAND_AI, 0);
					}
				}
			}
		}
	}

	public int ai_attack_order_nearby_mobile(int targetXLoc, int targetYLoc, int targetCombatLevel)
	{
		int scanRange = 15 + pref_military_development / 20; // 15 to 20
		int xOffset, yOffset;
		int targetRegionId = World.GetRegionId(targetXLoc, targetYLoc);

		for (int i = 2; i < scanRange * scanRange; i++)
		{
			Misc.cal_move_around_a_point(i, scanRange, scanRange, out xOffset, out yOffset);

			int xLoc = targetXLoc + xOffset;
			int yLoc = targetYLoc + yOffset;

			xLoc = Math.Max(0, xLoc);
			xLoc = Math.Min(GameConstants.MapSize - 1, xLoc);

			yLoc = Math.Max(0, yLoc);
			yLoc = Math.Min(GameConstants.MapSize - 1, yLoc);

			Location location = World.GetLoc(xLoc, yLoc);

			if (location.RegionId != targetRegionId)
				continue;

			if (!location.HasUnit(UnitConstants.UNIT_LAND))
				continue;

			//----- if there is a unit on the location ------//

			int unitRecno = location.UnitId(UnitConstants.UNIT_LAND);

			if (UnitArray.IsDeleted(unitRecno)) // the unit is dying
				continue;

			Unit unit = UnitArray[unitRecno];

			//--- if if this is our own military unit ----//

			if (unit.nation_recno != nation_recno || unit.skill.skill_id != Skill.SKILL_LEADING)
				continue;

			//--------- if this unit is injured ----------//

			if (unit.hit_points < unit.max_hit_points * (150 - pref_military_courage / 2) / 200)
				continue;

			//---- only if this is not assigned to an action ---//

			if (unit.ai_action_id != 0)
				continue;

			//---- if this unit is stop or assigning to a firm ----//

			if (unit.action_mode2 == UnitConstants.ACTION_STOP ||
			    unit.action_mode2 == UnitConstants.ACTION_ASSIGN_TO_FIRM)
			{
				//-------- set should_attack on the target to 1 --------//

				enable_should_attack_on_target(targetXLoc, targetYLoc);

				//---------- attack now -----------//

				unit.attack_unit(targetXLoc, targetYLoc, 0, 0, true);

				targetCombatLevel -= (int)unit.hit_points; // reduce the target combat level
			}
		}

		return targetCombatLevel;
	}

	public void ai_collect_military_force(int targetXLoc, int targetYLoc, int targetRecno,
		List<int> camps, List<int> units, List<int> ourUnits)
	{
		//--- the scanning distance is determined by the AI aggressiveness setting ---//

		int scanRangeX = 5 + Config.ai_aggressiveness * 2;
		int scanRangeY = scanRangeX;

		int xLoc1 = targetXLoc - scanRangeX;
		int yLoc1 = targetYLoc - scanRangeY;
		int xLoc2 = targetXLoc + scanRangeX;
		int yLoc2 = targetYLoc + scanRangeY;

		xLoc1 = Math.Max(xLoc1, 0);
		yLoc1 = Math.Max(yLoc1, 0);
		xLoc2 = Math.Min(xLoc2, GameConstants.MapSize - 1);
		yLoc2 = Math.Min(yLoc2, GameConstants.MapSize - 1);

		//------------------------------------------//

		//Collect all camps in the square + all camps linked to towns in the square
		//Collect all units in the square
		List<int> towns = new List<int>();
		for (int yLoc = yLoc1; yLoc <= yLoc2; yLoc++)
		{
			for (int xLoc = xLoc1; xLoc <= xLoc2; xLoc++)
			{
				Location location = World.GetLoc(xLoc, yLoc);
				if (location.IsTown())
				{
					int townRecno = location.TownId();
					Town town = TownArray[townRecno];
					if (town.NationId == targetRecno)
					{
						bool found = false;
						for (int i = 0; i < towns.Count; i++)
						{
							if (towns[i] == townRecno)
							{
								found = true;
								break;
							}
						}

						if (!found)
						{
							towns.Add(townRecno);
						}
					}
				}

				if (location.IsFirm())
				{
					int firmRecno = location.FirmId();
					Firm firm = FirmArray[firmRecno];
					if (firm.nation_recno == targetRecno && firm.firm_id == Firm.FIRM_CAMP)
					{
						bool found = false;
						for (int i = 0; i < camps.Count; i++)
						{
							if (camps[i] == firmRecno)
							{
								found = true;
								break;
							}
						}

						if (!found)
						{
							camps.Add(firmRecno);
						}
					}
				}

				if (location.HasUnit(UnitConstants.UNIT_LAND))
				{
					int unitRecno = location.UnitId(UnitConstants.UNIT_LAND);
					Unit unit = UnitArray[unitRecno];
					if (unit.nation_recno == targetRecno)
					{
						units.Add(unitRecno);
					}

					if (unit.nation_recno == nation_recno)
					{
						ourUnits.Add(nation_recno);
					}
				}
			}
		}

		for (int i = 0; i < towns.Count; i++)
		{
			Town town = TownArray[towns[i]];
			for (int j = 0; j < town.LinkedFirms.Count; j++)
			{
				int firmRecno = town.LinkedFirms[j];
				Firm firm = FirmArray[firmRecno];
				if (firm.nation_recno == targetRecno && firm.firm_id == Firm.FIRM_CAMP)
				{
					bool found = false;
					for (int k = 0; k < camps.Count; k++)
					{
						if (camps[k] == firmRecno)
						{
							found = true;
							break;
						}
					}

					if (!found)
					{
						camps.Add(firmRecno);
					}
				}
			}
		}
	}

	public int ai_evaluate_target_combat_level(int targetXLoc, int targetYLoc, int targetRecno)
	{
		List<int> camps = new List<int>();
		List<int> units = new List<int>();
		List<int> ourUnits = new List<int>();
		ai_collect_military_force(targetXLoc, targetYLoc, targetRecno, camps, units, ourUnits);
		int totalCombatLevel = 0;
		for (int i = 0; i < camps.Count; i++)
		{
			FirmCamp firmCamp = (FirmCamp)FirmArray[camps[i]];
			totalCombatLevel += firmCamp.total_combat_level();
		}

		for (int i = 0; i < units.Count; i++)
		{
			if (UnitArray.IsDeleted(units[i])) // the unit is dying
				continue;

			Unit unit = UnitArray[units[i]];
			totalCombatLevel += unit.unit_power();
		}

		// if targetRecno == nation_recno units and ourUnits are the same, so we already counted them
		if (targetRecno != nation_recno)
		{
			for (int i = 0; i < ourUnits.Count; i++)
			{
				if (UnitArray.IsDeleted(ourUnits[i])) // the unit is dying
					continue;

				Unit unit = UnitArray[ourUnits[i]];
				// only units that are currently attacking or idle are counted, moving units may just be passing by
				if (unit.cur_action == Sprite.SPRITE_ATTACK || unit.cur_action == Sprite.SPRITE_IDLE)
				{
					totalCombatLevel -= unit.unit_power();
				}
			}
		}

		return totalCombatLevel;
	}

	public bool ai_sea_attack_target(int targetXLoc, int targetYLoc)
	{
		int targetRegionId = World.GetRegionId(targetXLoc, targetYLoc);
		bool result = false;

		for (int i = 0; i < ai_ship_array.Count; i++)
		{
			UnitMarine unitMarine = (UnitMarine)UnitArray[ai_ship_array[i]];

			if (unitMarine.attack_count == 0)
				continue;

			if (!unitMarine.is_ai_all_stop())
				continue;

			//----- if the ship is in the harbor now -----//

			if (unitMarine.unit_mode == UnitConstants.UNIT_MODE_IN_HARBOR)
			{
				FirmHarbor firmHarbor = (FirmHarbor)FirmArray[unitMarine.unit_mode_para];

				if (firmHarbor.sea_region_id != targetRegionId)
					continue;

				firmHarbor.sail_ship(unitMarine.sprite_recno, InternalConstants.COMMAND_AI);
			}

			if (!unitMarine.is_visible()) // no space in the sea for placing the ship
				continue;

			if (unitMarine.region_id() != targetRegionId)
				continue;

			//------ order the ship to attack the target ------//

			unitMarine.attack_unit(targetXLoc, targetYLoc, 0, 0, true);
			result = true;
		}

		return result;
	}

	public void ai_attack_unit_in_area(int xLoc1, int yLoc1, int xLoc2, int yLoc2)
	{
		int enemyXLoc = -1, enemyYLoc = -1, enemyCombatLevel = 0;
		int enemyStatus = NATION_FRIENDLY;

		//--------------------------------------------------//

		for (int yLoc = yLoc1; yLoc <= yLoc2; yLoc++)
		{
			for (int xLoc = xLoc1; xLoc <= xLoc2; xLoc++)
			{
				Location location = World.GetLoc(xLoc, yLoc);

				if (!location.HasUnit(UnitConstants.UNIT_LAND))
					continue;

				Unit unit = UnitArray[location.UnitId(UnitConstants.UNIT_LAND)];

				//--- if there is an idle unit on the mine building site ---//

				if (unit.cur_action != Sprite.SPRITE_IDLE || unit.nation_recno == 0)
					continue;

				//----- if this is our spy cloaked in another nation, reveal its true identity -----//

				if (unit.nation_recno != nation_recno && unit.true_nation_recno() == nation_recno)
				{
					unit.spy_change_nation(nation_recno, InternalConstants.COMMAND_AI);
				}

				//--- if this is our own unit, order him to stay out of the building site ---//

				if (unit.nation_recno == nation_recno)
				{
					unit.think_normal_human_action(); // send the unit to a firm or a town
				}
				else //--- if it is an enemy unit, attack it ------//
				{
					int nationStatus = get_relation_status(unit.nation_recno);

					if (nationStatus < enemyStatus) // if the status is worse than the current target
					{
						enemyXLoc = xLoc;
						enemyYLoc = yLoc;
						enemyStatus = nationStatus;
						enemyCombatLevel += unit.unit_power();
					}
				}
			}
		}

		//--- if there are enemies on our firm building site, attack them ---//

		if (enemyCombatLevel > 0)
		{
			ai_attack_target(enemyXLoc, enemyYLoc, enemyCombatLevel);
		}
	}

	public int ai_defend(int attackerUnitRecno)
	{
		//--- don't call for defense too frequently, only call once 7 days
		//(since this function will be called every time our king/firm/town is attacked, so this filtering is necessary ---//

		if (Info.game_date < ai_last_defend_action_date.AddDays(7.0))
			return 0;

		ai_last_defend_action_date = Info.game_date;

		//---------- analyse the situation first -----------//

		Unit attackerUnit = UnitArray[attackerUnitRecno];


		int attackerXLoc = attackerUnit.next_x_loc();
		int attackerYLoc = attackerUnit.next_y_loc();
		int targetRegionId = World.GetLoc(attackerXLoc, attackerYLoc).RegionId;

		int enemyCombatLevel = ai_evaluate_target_combat_level(attackerXLoc, attackerYLoc, attackerUnit.nation_recno);

		//-- the value returned is enemy strength minus your own strength, so if it's positive,
		//it means that your enemy is stronger than you, otherwise you're stronger than your enemy --//

		int targetCombatLevel = ai_attack_order_nearby_mobile(attackerXLoc, attackerYLoc, enemyCombatLevel);
		if (targetCombatLevel < 0) // the mobile force alone can finish all the enemies
			return 1;

		List<int> protectionCamps = new List<int>();
		for (int i = 0; i < ai_town_array.Count; i++)
		{
			Town town = TownArray[ai_town_array[i]];

			if (town.RegionId != targetRegionId)
				continue;

			town.AddProtectionCamps(protectionCamps, true);
		}

		List<int> defendingCamps = new List<int>();
		int totalCombatLevel = 0;
		for (int i = 0; i < ai_camp_array.Count; i++)
		{
			int firmRecno = ai_camp_array[i];
			FirmCamp firmCamp = (FirmCamp)FirmArray[firmRecno];

			if (firmCamp.region_id != targetRegionId)
				continue;

			bool isProtectionCamp = false;
			for (int j = 0; j < protectionCamps.Count; j++)
			{
				if (protectionCamps[j] == firmRecno)
				{
					isProtectionCamp = true;
					break;
				}
			}

			if (isProtectionCamp)
				continue;

			int distanceFromAttacker =
				Misc.points_distance(firmCamp.center_x, firmCamp.center_y, attackerXLoc, attackerYLoc);
			if (distanceFromAttacker > GameConstants.MapSize / 2)
				continue;

			if (firmCamp.is_attack_camp && distanceFromAttacker < 15)
			{
				bool calledFromAttack = false;
				for (int j = attack_camps.Count - 1; j >= 0; j--)
				{
					if (attack_camps[j].firm_recno == firmRecno)
					{
						if (attack_camps[j].patrol_date == default(DateTime)) //troop is patrolling, call them back
						{
							firmCamp.validate_patrol_unit();
							if (firmCamp.patrol_unit_array.Count > 0)
							{
								UnitArray.MoveTo(attackerXLoc, attackerYLoc, false,
									firmCamp.patrol_unit_array, InternalConstants.COMMAND_AI);
							}

							calledFromAttack = true;
						}

						//remove this camp from the attack camps
						firmCamp.is_attack_camp = false;
						attack_camps.RemoveAt(j);
						break;
					}
				}

				if (calledFromAttack)
					continue;
			}

			totalCombatLevel += firmCamp.total_combat_level();
			defendingCamps.Add(firmRecno);
		}

		//---- now we get all suitable camps in the list, it's time to defend ---//
		//--- don't send troop if available combat level < minTargetCombatLevel ---//
		int minTargetCombatLevel = targetCombatLevel * (100 - pref_military_courage / 2) / 100; // 50% to 100%

		//--- if we are not strong enough to defend -----//
		if (totalCombatLevel < minTargetCombatLevel)
			return 0;

		for (int i = 0; i < defendingCamps.Count; i++)
		{
			FirmCamp firmCamp = (FirmCamp)FirmArray[defendingCamps[i]];
			firmCamp.patrol();
			firmCamp.validate_patrol_unit();
			if (firmCamp.patrol_unit_array.Count > 0)
			{
				UnitArray.MoveTo(attackerXLoc, attackerYLoc, false,
					firmCamp.patrol_unit_array, InternalConstants.COMMAND_AI);
			}
		}

		//------ request military aid from allies ----//
		if (totalCombatLevel < enemyCombatLevel && attackerUnit.nation_recno > 0)
		{
			ai_request_military_aid();
		}

		return 1;
	}

	public bool ai_request_military_aid()
	{
		foreach (Nation nation in NationArray.EnumerateRandom())
		{
			if (get_relation(nation.nation_recno).status != NATION_ALLIANCE)
				continue;

			if (should_diplomacy_retry(TalkMsg.TALK_REQUEST_MILITARY_AID, nation.nation_recno))
			{
				TalkRes.ai_send_talk_msg(nation.nation_recno, nation_recno,
					TalkMsg.TALK_REQUEST_MILITARY_AID);
				return true;
			}
		}

		return false;
	}

	public void reset_ai_attack_target()
	{
		//------ reset all is_attack_camp -------//

		for (int i = 0; i < attack_camps.Count; i++)
		{
			FirmCamp firmCamp = (FirmCamp)FirmArray[attack_camps[i].firm_recno];

			firmCamp.is_attack_camp = false;
		}

		attack_camps.Clear();
	}

	public bool think_attack_monster()
	{
		if (Config.monster_type == Config.OPTION_MONSTER_NONE) // no monsters in the game
			return false;

		//--- if the AI has run out of money and is currently cheating, it will have a urgent need to attack monsters to get money ---//

		bool useAllCamp = income_365days(INCOME_CHEAT) > 0;

		if (!useAllCamp) // use all camps to attack the monster
		{
			if (!is_at_war())
			{
				if (cash < 500 && military_rank_rating() >= 75 - pref_attack_monster / 4) //  50 to 75
					useAllCamp = true;
			}
		}

		if (!useAllCamp)
		{
			// don't attack if the military strength is too low, 25 to 50
			if (military_rank_rating() < 50 - pref_attack_monster / 4)
				return false;
		}

		//------- select a monster target ---------//

		int targetCombatLevel;

		int targetFirmRecno = think_monster_target(out targetCombatLevel);

		if (targetFirmRecno == 0)
			return false;

		targetCombatLevel = targetCombatLevel * 150 / 100; // X 150%

		//--- we need to use veteran soldiers to attack powerful monsters ---//

		FirmMonster targetFirm = (FirmMonster)FirmArray[targetFirmRecno];

		int monsterLevel = MonsterRes[targetFirm.monster_id].level;

		int attackerMinCombatLevel = 0;

		if (targetCombatLevel > 100) // if the nation's cash runs very low, it will attack anyway
			attackerMinCombatLevel = 20 + monsterLevel * 3;

		//--- call ai_attack_target() to attack the target town ---//

		return ai_attack_target(targetFirm.loc_x1, targetFirm.loc_y1, targetCombatLevel, false,
			attackerMinCombatLevel, 0, useAllCamp);
	}

	public int think_monster_target(out int targetCombatLevel)
	{
		targetCombatLevel = 0;

		if (largest_town_recno == 0)
			return 0;

		//TODO not only largest town
		Town largestTown = TownArray[largest_town_recno];
		int bestRating = Int32.MinValue, bestFirmRecno = 0;

		foreach (Firm firm in FirmArray)
		{
			if (firm.firm_id != Firm.FIRM_MONSTER || firm.region_id != largestTown.RegionId)
				continue;

			//----- take into account of the mobile units around this town -----//

			if (is_battle(firm.center_x, firm.center_y) > 0)
				continue;

			int mobileCombatLevel = ai_evaluate_target_combat_level(firm.center_x, firm.center_y, firm.nation_recno);

			int curRating = 3 * Misc.points_distance(largestTown.LocCenterX, largestTown.LocCenterY,
				firm.center_x, firm.center_y);

			int combatLevel = mobileCombatLevel + ((FirmMonster)firm).total_combat_level();

			curRating -= combatLevel;

			//---------------------------------//

			if (curRating > bestRating)
			{
				targetCombatLevel = combatLevel;
				bestRating = curRating;
				bestFirmRecno = firm.firm_recno;
			}
		}

		return bestFirmRecno;
	}

	public bool ai_should_expand_military()
	{
		//----- don't expand if it is losing money -----//

		if (true_profit_365days() < 0)
			return false;

		//----- expand if it has enough cash -----//

		if (!ai_should_spend(pref_military_development / 2))
			return false;

		//--- check whether the current military force is already big enough ---//

		// (50 to 150) / 150
		if (ai_camp_array.Count * 9 > total_jobless_population * (50 + pref_military_development) / 150)
			return false;

		//---- see if any of the camps are still needed soldiers -----//

		int freeSpaceCount = 0;

		for (int i = 0; i < ai_camp_array.Count; i++)
		{
			FirmCamp firmCamp = (FirmCamp)FirmArray[ai_camp_array[i]];

			if (firmCamp.should_close_flag) // exclude those going to be closed down
				continue;

			//---- only build a new one when existing ones are all full ----//

			int soldierCount = (firmCamp.overseer_recno > 0 ? 1 : 0) + firmCamp.workers.Count +
			                   firmCamp.patrol_unit_array.Count;

			freeSpaceCount += Firm.MAX_WORKER + 1 - soldierCount;

			if (firmCamp.ai_recruiting_soldier)
				return false;
		}

		return freeSpaceCount < Firm.MAX_WORKER + 1; // build build new ones when the existing ones are almost full
	}

	public bool ai_is_troop_need_new_camp()
	{
		//--- check whether the current military force is already big enough ---//

		if (ai_has_too_many_camp())
			return false;

		//----- expand if it has enough cash -----//

		if (!ai_should_spend(50 + pref_military_development / 2))
			return false;

		//----- if existing camps can already host all units ----//

		int neededSoldierSpace = total_human_count + total_weapon_count - ai_camp_array.Count * Firm.MAX_WORKER;
		int neededGeneralSpace = total_general_count - ai_camp_array.Count;

		return neededSoldierSpace >= 9 + 20 * (100 - pref_military_development) / 200 &&
		       neededGeneralSpace >= 2 + 4 * (100 - pref_military_development) / 200;
	}

	public bool ai_has_too_many_camp()
	{
		// (150 to 250) / 150
		return ai_camp_array.Count * 9 > total_jobless_population * (150 + pref_military_development) / 150;
	}

	public bool ai_should_attack_friendly(int friendlyNationRecno, int attackTemptation)
	{
		Nation friendlyNation = NationArray[friendlyNationRecno];
		NationRelation nationRelation = get_relation(friendlyNationRecno);

		//--- don't change terminate treaty too soon ---//

		// only after 60 to 110 days
		if (Info.game_date < nationRelation.last_change_status_date.AddDays(60 + pref_honesty / 2))
			return false;

		//------------------------------------------------//

		int resistanceRating = friendlyNation.military_rank_rating() - military_rank_rating();

		resistanceRating += nationRelation.ai_relation_level - 50;

		resistanceRating += trade_rating(friendlyNationRecno);

		return attackTemptation > resistanceRating;
	}

	public void enable_should_attack_on_target(int targetXLoc, int targetYLoc)
	{
		//------ set should attack to 1 --------//

		int targetNationRecno = get_target_nation_recno(targetXLoc, targetYLoc);
		if (targetNationRecno != 0)
		{
			set_relation_should_attack(targetNationRecno, true, InternalConstants.COMMAND_AI);
		}
	}

	//------------------------------------------------------------//
	// economic related functions
	//------------------------------------------------------------//

	public void think_reduce_expense()
	{
		if (true_profit_365days() > 0 || cash > 1000 + 2000 * pref_cash_reserve / 100)
			return;

		//-------- close down firms ---------//

		int bestRating = 0;
		Firm bestFirm = null;

		foreach (Firm firm in FirmArray)
		{
			if (firm.nation_recno != nation_recno)
				continue;

			if (firm.firm_id != Firm.FIRM_RESEARCH && firm.firm_id != Firm.FIRM_WAR_FACTORY)
				continue;

			int curRating = 100 - (int)firm.productivity;

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestFirm = firm;
			}
		}

		if (bestFirm != null)
			bestFirm.ai_del_firm();

		//-------- drop spy identity ---------//
		think_drop_spy_identity();
	}

	public int surplus_supply_rating()
	{
		double totalStockQty = 0.0;
		int totalStockSlot = 0;

		for (int i = 0; i < ai_market_array.Count; i++)
		{
			FirmMarket firmMarket = (FirmMarket)FirmArray[ai_market_array[i]];

			for (int j = 0; j < GameConstants.MAX_MARKET_GOODS; j++)
			{
				MarketGoods marketGoods = firmMarket.market_goods_array[j];
				if (marketGoods.raw_id != 0 || marketGoods.product_raw_id != 0)
				{
					double stockQty = marketGoods.stock_qty;

					totalStockQty += stockQty;
					totalStockSlot++;
				}
			}
		}

		if (totalStockSlot == 0)
			return 0;

		double avgStockQty = totalStockQty / totalStockSlot;

		return (int)(100.0 * avgStockQty / GameConstants.MAX_MARKET_STOCK);
	}

	public int ai_trade_with_rating(int withNationRecno)
	{
		Nation nation = NationArray[withNationRecno];
		int tradeRating = 0;

		for (int i = 0; i < GameConstants.MAX_RAW; i++)
		{
			//--------------------------------------------------------------//
			//
			// If we have the raw material and it doesn't have, then we
			// can export to it. And it is more favorite if the nation's
			// population is high, so we can export more.
			//
			//--------------------------------------------------------------//

			if (raw_count_array[i] != 0 && nation.raw_count_array[i] == 0)
				tradeRating += Math.Min(30, nation.total_population / 3);

			//--------------------------------------------------------------//
			//
			// If the nation has the supply a raw material that we don't
			// have, then we can import it.
			//
			//--------------------------------------------------------------//

			else if (nation.raw_count_array[i] != 0 && raw_count_array[i] == 0)
				tradeRating += 30;
		}

		return tradeRating;
	}

	public bool ai_should_spend(int importanceRating, double spendAmt = 0)
	{
		if (cash < spendAmt)
			return false;

		double fixedExpense = fixed_expense_365days();
		double stdCashLevel = Math.Max(fixedExpense, 2000) * (150 + pref_cash_reserve) / 100; //from 3000 to 5000
		double trueProfit = true_profit_365days();

		//----- if we are losing money, don't spend on non-important things -----//

		if (trueProfit < 0)
		{
			if (400.0 * (-trueProfit) / fixedExpense > importanceRating)
				return false;
		}

		//--------------------------------------//

		//cash = 0 means 0, cash = (from 6000 to 10000) means 100
		double curCashLevel = 100 * (cash - spendAmt) / (stdCashLevel * 2);

		return importanceRating >= 100 - curCashLevel;
	}

	public bool ai_should_spend_war(int enemyMilitaryRating, bool considerCeaseFire = false)
	{
		int importanceRating = 30 + pref_military_development / 5; // 30 to 50

		importanceRating += military_rank_rating() - enemyMilitaryRating / 2;

		// only when we are very powerful, we will start a battle. So won't cease fire too soon after declaring war
		// less early to return 0, for cease fire
		if (considerCeaseFire)
			importanceRating += 20;

		return ai_should_spend(importanceRating);
	}

	public bool ai_has_enough_food()
	{
		if (food > 5000 + 5000 * pref_food_reserve / 100)
			return true;

		if (food < 1000 + 1000 * pref_food_reserve / 100)
			return false;

		return yearly_food_change() > 0;
	}

	//------------------------------------------------------------//
	// town related functions
	//------------------------------------------------------------//

	public void think_town()
	{
		//DieselMachine TODO buggy code, rewrite
		//optimize_town_race();
	}

	public void optimize_town_race()
	{
		foreach (RegionStat regionStat in RegionArray.RegionStats)
		{
			if (regionStat.TownNationCounts[nation_recno - 1] > 0)
				optimize_town_race_region(regionStat.RegionId);
		}
	}

	public void optimize_town_race_region(int regionId)
	{
		//---- reckon the minority jobless pop of each race ----//

		int[] racePopArray = new int[GameConstants.MAX_RACE];

		for (int i = 0; i < ai_town_array.Count; i++)
		{
			Town town = TownArray[ai_town_array[i]];

			if (town.RegionId != regionId)
				continue;

			int majorityRace = town.MajorityRace();

			for (int j = 0; j < GameConstants.MAX_RACE; j++)
			{
				if (j + 1 != majorityRace)
					racePopArray[j] += town.RacesJoblessPopulation[j];
			}
		}

		//--- locate for towns with minority being majority and those minority race can move to ---//

		Town destTown = null;

		for (int raceId = 1; raceId <= GameConstants.MAX_RACE; raceId++)
		{
			if (racePopArray[raceId - 1] == 0) // we don't have any minority of this race
				continue;

			destTown = null;

			for (int i = 0; i < ai_town_array.Count; i++)
			{
				Town town = TownArray[ai_town_array[i]];

				if (town.RegionId != regionId)
					continue;

				if (!town.IsBaseTown)
					continue;

				if (town.MajorityRace() == raceId && town.Population < GameConstants.MAX_TOWN_POPULATION)
				{
					destTown = town;
					break;
				}
			}

			if (destTown == null)
				continue;

			//---- if there is a suitable town for minority to move to ---//

			for (int i = 0; i < ai_town_array.Count; i++)
			{
				Town town = TownArray[ai_town_array[i]];

				if (town.RegionId != regionId)
					continue;

				//---- move minority units from towns -----//

				int joblessCount = town.RacesJoblessPopulation[raceId - 1];

				if (joblessCount > 0 && town.MajorityRace() != raceId)
				{
					int migrateCount = Math.Min(8, joblessCount); // migrate a maximum of 8 units at a time

					add_action(destTown.LocX1, destTown.LocY1, town.LocX1, town.LocY1,
						ACTION_AI_SETTLE_TO_OTHER_TOWN, 0, migrateCount);
				}
			}
		}
	}

	//--------------------------------------------------------------//
	// functions for capturing independent and enemy towns
	//--------------------------------------------------------------//

	public bool think_capture_independent()
	{
		//------- Capture target choices -------//

		List<CaptureTown> captureTownQueue = new List<CaptureTown>();

		//--- find the town that makes most sense to capture ---//

		int numberOfTownsWeAlreadyCapturing = 0;
		foreach (Firm firm in FirmArray)
		{
			if (firm.nation_recno != nation_recno || firm.firm_id != Firm.FIRM_CAMP)
				continue;

			FirmCamp camp = (FirmCamp)firm;
			if (camp.ai_is_capturing_independent_village())
				numberOfTownsWeAlreadyCapturing++;
		}

		foreach (Town town in TownArray)
		{
			if (town.NationId != 0) // only capture independent towns
				continue;

			if (town.NoNeighborSpace) // if there is no space in the neighbor area for building a new firm.
				continue;

			// towns controlled by rebels will not drop in resistance even if a command base is present
			if (town.RebelId != 0)
				continue;

			// do not capture too many villages
			if (numberOfTownsWeAlreadyCapturing >= 3 &&
			    town.RacesPopulation[town.MajorityRace() - 1] != town.Population)
				continue;

			if (town.RacesPopulation[town.MajorityRace() - 1] < 15) // do not capture villages with low population
				continue;

			//------ only if we have a presence/a base town in this region -----//

			//DieselMachine TODO consider capture villages on islands
			if (!has_base_town_in_region(town.RegionId))
				continue;

			//---- check if there are already camps linked to this town ----//

			int i;
			for (i = town.LinkedFirms.Count - 1; i >= 0; i--)
			{
				Firm firm = FirmArray[town.LinkedFirms[i]];

				if (firm.firm_id != Firm.FIRM_CAMP)
					continue;

				//------ if we already have a camp linked to this town -----//

				//DieselMachine TODO what if this camp is linked to our town also and is not intended for capturing?
				if (firm.nation_recno == nation_recno)
					break;

				//--- if there is an overseer with high leadership and right race in the opponent's camp, don't bother to compete with him ---//

				//DieselMachine TODO maybe we need to compete. Think about it
				if (firm.overseer_recno != 0)
				{
					Unit unit = UnitArray[firm.overseer_recno];

					if (unit.skill.skill_level >= 70 && unit.race_id == town.MajorityRace())
					{
						break;
					}
				}
			}

			if (i >= 0) // there is already a camp linked to this town and we don't want to get involved with its capturing plan
				continue;

			//------ no linked camps interfering with potential capture ------//

			int captureUnitRecno;
			int targetResistance = capture_expected_resistance(town.TownId, out captureUnitRecno);

			if (targetResistance < 50 - pref_peacefulness / 5) // 30 to 50 depending on
			{
				CaptureTown captureTown = new CaptureTown();
				captureTown.town_recno = town.TownId;
				captureTown.min_resistance = targetResistance;
				captureTown.capture_unit_recno = captureUnitRecno;
				captureTownQueue.Add(captureTown);
			}
		}

		//------- try to capture the town in their resistance order ----//

		bool needToCheckDistance = !Config.explore_whole_map && (Info.game_date - Info.game_start_date).Days >
			Math.Max(GameConstants.MapSize, GameConstants.MapSize) * (5 - Config.ai_aggressiveness) / 5; // 3 to 5 / 5

		foreach (CaptureTown captureTown in captureTownQueue.OrderByDescending(t => t.min_resistance))
		{
			int captureRecno = captureTown.town_recno;
			int captureUnitRecno = captureTown.capture_unit_recno;

			//-------------------------------------------//
			// If the map is set to unexplored, wait for a
			// reasonable amount of time before moving out
			// to build the camp.
			//-------------------------------------------//

			if (needToCheckDistance)
			{
				Town targetTown = TownArray[captureRecno];

				int j;
				for (j = 0; j < ai_town_array.Count; j++)
				{
					Town ownTown = TownArray[ai_town_array[j]];

					int townDistance = Misc.points_distance(targetTown.LocCenterX, targetTown.LocCenterY,
						ownTown.LocCenterX, ownTown.LocCenterY);

					if ((Info.game_date - Info.game_start_date).Days >
					    townDistance * (5 - Config.ai_aggressiveness) / 5) // 3 to 5 / 5
					{
						break;
					}
				}

				if (j == ai_town_array.Count)
					continue;
			}

			if (start_capture(captureRecno, captureUnitRecno))
				return true;
		}

		return false;
	}

	public int capture_expected_resistance(int townRecno, out int captureUnitRecno)
	{
		//--- we have plenty of cash, use cash to decrease the resistance of the villagers ---//

		captureUnitRecno = 0;

		if (should_use_cash_to_capture())
			return 0; // return zero resistance

		//----- the average resistance determines the captureRating ------//

		int captureRating = 0;
		Town town = TownArray[townRecno];

		int averageResistance;

		if (town.NationId != 0)
			averageResistance = town.AverageLoyalty();
		else
			averageResistance = town.AverageResistance(nation_recno);

		//---- see if there are general available for capturing this town ---//

		int majorityRace = town.MajorityRace();
		int targetResistance = 100;
		int bestCapturer = find_best_capturer(townRecno, majorityRace, ref targetResistance);
		if (bestCapturer == 0)
		{
			bestCapturer = hire_best_capturer(townRecno, majorityRace, ref targetResistance, false);
		}

		if (bestCapturer != 0)
		{
			captureUnitRecno = bestCapturer;
			return (targetResistance * town.RacesPopulation[majorityRace - 1]
			        + averageResistance * (town.Population - town.RacesPopulation[majorityRace - 1]))
			       / town.Population;
		}
		else
		{
			return targetResistance;
		}
	}

	public bool start_capture(int townRecno, int captureUnitRecno)
	{
		//--- find the two races with most population in the town ---//

		Town town = TownArray[townRecno];

		int majorityRace = 0;

		//--- if it's an independent town, the race of the commander must match with the race of the town ---//

		if (town.NationId == 0)
		{
			majorityRace = town.MajorityRace();
		}

		//---- see if we have generals in the most populated race, if so build a camp next to the town ----//

		return capture_build_camp(townRecno, majorityRace, captureUnitRecno);
	}

	public bool capture_build_camp(int townRecno, int raceId, int captureUnitRecno)
	{
		Town captureTown = TownArray[townRecno];

		//------- locate a place to build the camp -------//

		int buildXLoc, buildYLoc;

		if (!find_best_firm_loc(Firm.FIRM_CAMP, captureTown.LocX1, captureTown.LocY1,
			    out buildXLoc, out buildYLoc))
		{
			captureTown.NoNeighborSpace = true;
			return false;
		}

		//---- find the best available general for the capturing action ---//

		int unitRecno = captureUnitRecno;
		int targetResistance = 100;

		if (unitRecno == 0)
			unitRecno = find_best_capturer(townRecno, raceId, ref targetResistance);

		if (unitRecno == 0)
			unitRecno = hire_best_capturer(townRecno, raceId, ref targetResistance, true);

		if (unitRecno == 0)
		{
			//--- if we have plenty of cash and can use cash to decrease the resistance of the independent villagers ---//

			if (should_use_cash_to_capture())
			{
				Unit skilledUnit = find_skilled_unit(Skill.SKILL_LEADING, raceId,
					captureTown.LocCenterX, captureTown.LocCenterY, out _);

				if (skilledUnit != null)
					unitRecno = skilledUnit.sprite_recno;
			}

			if (unitRecno == 0)
				return false;
		}

		//--- if the picked unit is an overseer of an existng camp ---//

		if (!mobilize_capturer(unitRecno))
			return false;

		//---------- add the action to the queue now ----------//

		ActionNode actionNode = add_action(buildXLoc, buildYLoc, captureTown.LocX1, captureTown.LocY1,
			ACTION_AI_BUILD_FIRM, Firm.FIRM_CAMP, 1, unitRecno);

		if (actionNode != null)
			process_action(actionNode);

		return true;
	}

	public int find_best_capturer(int townRecno, int raceId, ref int bestTargetResistance)
	{
		Town targetTown = TownArray[townRecno];
		int bestUnitRecno = 0;

		bestTargetResistance = 100;

		for (int i = ai_general_array.Count - 1; i >= 0; i--)
		{
			Unit unit = UnitArray[ai_general_array[i]];

			if (raceId != 0 && unit.race_id != raceId)
				continue;

			if (unit.nation_recno != nation_recno)
				continue;

			//---- if this unit is on a mission ----//

			if (unit.home_camp_firm_recno != 0)
				continue;

			//---- don't use the king to build camps next to capture enemy towns, only next to independent towns ----//

			if (unit.rank_id == Unit.RANK_KING && targetTown.NationId != 0)
				continue;

			//----- if this unit is in a camp -------//

			if (unit.unit_mode == UnitConstants.UNIT_MODE_OVERSEE)
			{
				Firm firm = FirmArray[unit.unit_mode_para];

				if (firm.firm_id != Firm.FIRM_CAMP) //Use generals only from forts
					continue;

				//--- check if the unit currently in a command base trying to take over an independent town ---//

				int j;
				for (j = firm.linked_town_array.Count - 1; j >= 0; j--)
				{
					Town town = TownArray[firm.linked_town_array[j]];

					//--- if the unit is trying to capture an independent town and he is still influencing the town to decrease resistance ---//

					if (town.NationId == 0 &&
					    town.AverageTargetResistance(nation_recno) < town.AverageResistance(nation_recno))
					{
						break; // then don't use this unit
					}
				}

				if (j >= 0) // if so, don't use this unit
					continue;
			}

			//--- if this unit is idle and the region ids are matched ---//

			if (unit.action_mode != UnitConstants.ACTION_STOP || unit.region_id() != targetTown.RegionId)
			{
				continue;
			}

			//------- get the unit's influence index --------//

			// influence of this unit if he is assigned as a commander of a military camp
			int targetResistance = 100 - unit.CampInfluence();

			//-- see if this unit's rating is higher than the current best --//

			if (targetResistance < bestTargetResistance)
			{
				bestTargetResistance = targetResistance;
				bestUnitRecno = unit.sprite_recno;
			}
		}

		for (int i = 0; i < ai_camp_array.Count; i++)
		{
			FirmCamp firmCamp = (FirmCamp)FirmArray[ai_camp_array[i]];

			if (firmCamp.region_id != targetTown.RegionId)
				continue;


			for (int j = 0; j < firmCamp.workers.Count; j++)
			{
				Worker worker = firmCamp.workers[j];
				if ((worker.race_id == 0) || (raceId != 0 && worker.race_id != raceId))
					continue;

				int workerLeadership = worker.skill_level;
				int workerInfluence = worker.skill_level * 2 / 3; // 66% of the leadership

				//if (race_res.is_same_race(race_id, worker.race_id))
				//workerInfluence += workerInfluence / 3;		// 33% bonus if the king's race is also the same as the general

				workerInfluence += (int)reputation / 2;
				workerInfluence = Math.Min(100, workerInfluence);
				int targetResistance = 100 - workerInfluence;

				if (targetResistance < bestTargetResistance)
				{
					bestTargetResistance = targetResistance;
					int unitRecno = firmCamp.mobilize_worker(j + 1, InternalConstants.COMMAND_AI);
					Unit unit = UnitArray[unitRecno];
					unit.set_rank(Unit.RANK_GENERAL);
					return unitRecno;
				}
			}
		}

		return bestUnitRecno;
	}

	public int hire_best_capturer(int townRecno, int raceId, ref int targetResistance, bool hire)
	{
		if (!ai_should_hire_unit(30)) // 30 - importance rating
			return 0;

		int bestRating = 70, bestInnRecno = 0, bestInnUnitId = 0;
		Town town = TownArray[townRecno];
		int destRegionId = World.GetRegionId(town.LocX1, town.LocY1);

		targetResistance = 100;

		for (int i = 0; i < ai_inn_array.Count; i++)
		{
			FirmInn firmInn = (FirmInn)FirmArray[ai_inn_array[i]];

			if (firmInn.region_id != destRegionId)
				continue;

			//------- check units in the inn ---------//

			for (int j = 0; j < firmInn.inn_unit_array.Count; j++)
			{
				InnUnit innUnit = firmInn.inn_unit_array[j];

				if (innUnit.skill.skill_id == Skill.SKILL_LEADING && UnitRes[innUnit.unit_id].race_id == raceId && cash >= innUnit.hire_cost)
				{
					//----------------------------------------------//
					// evaluate a unit on:
					// -its race, whether it's the same as the nation's race
					// -the inn's distance from the destination
					// -the skill level of the unit.
					//----------------------------------------------//

					int curRating = innUnit.skill.skill_level;

					if (curRating > bestRating)
					{
						bestRating = curRating;
						bestInnRecno = firmInn.firm_recno;
						bestInnUnitId = j;
					}
				}
			}
		}

		if (bestInnUnitId == 0)
			return 0;

		int unitInfluence = bestRating * 2 / 3; // 66% of the leadership
		unitInfluence += (int)(reputation / 2.0);
		unitInfluence = Math.Min(100, unitInfluence);
		// influence of this unit if he is assigned as a commander of a military camp
		targetResistance = 100 - unitInfluence;

		//----------------------------------------------------//
		int unitRecno = 0;
		if (hire)
		{
			FirmInn firmInn = (FirmInn)FirmArray[bestInnRecno];

			unitRecno = firmInn.hire(bestInnUnitId);
			if (unitRecno == 0)
				return 0;

			UnitArray[unitRecno].set_rank(Unit.RANK_GENERAL);
		}

		return unitRecno;
	}

	public bool mobilize_capturer(int unitRecno)
	{
		Unit unit = UnitArray[unitRecno];

		if (unit.unit_mode == UnitConstants.UNIT_MODE_OVERSEE)
		{
			//--- if the picked unit is an overseer of an existing camp ---//

			if (cash < EXPENSE_TRAIN_UNIT) // training a replacement costs money
				return false;

			Firm firm = FirmArray[unit.unit_mode_para];

			//-- can recruit from either a command base or seat of power --//

			//-- train a villager with leadership to replace current overseer --//

			//TODO DieselMachine do not train. Find existing leaders first

			int i;
			for (i = 0; i < firm.linked_town_array.Count; i++)
			{
				Town town = TownArray[firm.linked_town_array[i]];

				if (town.NationId != nation_recno)
					continue;

				//--- first try to train a unit who is racially homogenous to the commander ---//

				int newUnitRecno = town.Recruit(Skill.SKILL_LEADING, firm.majority_race(),
					InternalConstants.COMMAND_AI);

				//--- if unsucessful, then try to train a unit whose race is the same as the majority of the town ---//

				if (newUnitRecno == 0)
					newUnitRecno = town.Recruit(Skill.SKILL_LEADING, town.MajorityRace(),
						InternalConstants.COMMAND_AI);

				if (newUnitRecno != 0)
				{
					add_action(firm.loc_x1, firm.loc_y1, -1, -1,
						ACTION_AI_ASSIGN_OVERSEER, Firm.FIRM_CAMP);
					break;
				}
			}

			if (i == firm.linked_town_array.Count) // unsuccessful
				return false;

			//------- mobilize the current overseer --------//

			firm.mobilize_overseer();
		}

		return true;
	}

	public bool think_capture_new_enemy_town(Town capturerTown, bool useAllCamp = false)
	{
		if (ai_camp_array.Count == 0) // this can happen when a new nation has just emerged
			return false;

		if (ai_capture_enemy_town_recno != 0) // no new action if we are still trying to capture a town
			return false;

		//---- only attack when we have enough money to support the war ----//

		if (cash < 2000 + 3000 * pref_cash_reserve / 100) // if the cash is really too low now
			return false;

		//--------------------------------------//

		Town targetTown = think_capture_enemy_town_target(capturerTown);

		if (targetTown == null)
			return false;

		//---- attack enemy's defending forces on the target town ----//

		int rc = attack_enemy_town_defense(targetTown, useAllCamp);

		if (rc == 0) // 0 means we don't have enough troop to attack the enemy
		{
			return false;
		}

		if (rc == 1) // 1 means a troop has been sent to attack the town
		{
			ai_capture_enemy_town_recno = targetTown.TownId; // this nation is currently trying to capture this town
			ai_capture_enemy_town_plan_date = Info.game_date;
			ai_capture_enemy_town_start_attack_date = default;
			ai_capture_enemy_town_use_all_camp = useAllCamp;

			return true;
		}

		if (rc == -1) // -1 means no defense on the target town, no attacking is needed.
		{
			return start_capture(targetTown.TownId, 0);
		}

		return false;
	}

	public void think_capturing_enemy_town()
	{
		if (ai_capture_enemy_town_recno == 0)
			return;

		if (TownArray.IsDeleted(ai_capture_enemy_town_recno) ||
		    TownArray[ai_capture_enemy_town_recno].NationId == nation_recno) // this town has been captured already
		{
			ai_capture_enemy_town_recno = 0;
			return;
		}

		//--- check the enemy's defense combat level around the town ---//

		Town targetTown = TownArray[ai_capture_enemy_town_recno];

		//---- if we haven't started attacking the town yet -----//

		int isBattle = is_battle(targetTown.LocCenterX, targetTown.LocCenterY);
		if (ai_capture_enemy_town_start_attack_date == default)
		{
			if (isBattle == 2) // we are at war with the nation now
				ai_capture_enemy_town_start_attack_date = Info.game_date;

			// when 3 months have gone and there still hasn't been any attack on the town,
			// there must be something bad happened to our troop, cancel the entire action
			if (Info.game_date > ai_capture_enemy_town_plan_date.AddDays(90))
				ai_capture_enemy_town_recno = 0;

			return; // do nothing if the attack hasn't started yet
		}

		//--------- check if we need reinforcement --------//

		//-----------------------------------------------------------//
		// Check how long we have started attacking because only
		// when the it has been started for a while, our force
		// will reach the target and the offensive and defensive force
		// total can be calculated accurately.
		//-----------------------------------------------------------//

		if ((Info.game_date - ai_capture_enemy_town_start_attack_date).Days >= 15)
		{
			//-------- check if we need any reinforcement --------//

			int targetCombatLevel =
				ai_evaluate_target_combat_level(targetTown.LocCenterX, targetTown.LocCenterY, targetTown.NationId);
			if (targetCombatLevel > 0 && isBattle == 2) // we are still in war with the enemy
			{
				ai_attack_target(targetTown.LocCenterX, targetTown.LocCenterY, targetCombatLevel, true);
				return;
			}
		}

		//----- there is currently no war at the town  -----//
		//
		// - either we are defeated or we have destroyed their command base.
		//
		//--------------------------------------------------//

		if (isBattle != 2)
		{
			//---- attack enemy's defending forces on the target town ----//

			int rc = attack_enemy_town_defense(targetTown, ai_capture_enemy_town_use_all_camp);

			if (rc == 1) // 1 means a troop has been sent to attack the town
			{
				ai_capture_enemy_town_start_attack_date = default;
				return;
			}

			//---------- reset the vars --------//

			ai_capture_enemy_town_recno = 0;
			ai_capture_enemy_town_start_attack_date = default;

			//--------- other situations --------//

			if (rc == -1) // -1 means no defense on the target town, no attacking is needed.
			{
				start_capture(targetTown.TownId, 0); // call AI functions in OAI_CAPT.CPP to capture the town
			}

			// 0 means we don't have enough troop to attack the enemy
		}
	}

	public int attack_enemy_town_defense(Town targetTown, bool useAllCamp = false)
	{
		if (targetTown.NationId == 0)
			return -1;

		//--- if there are any command bases linked to the town, attack them first ---//

		int maxCampCombatLevel = -1;
		Firm bestTargetFirm = null;

		for (int i = targetTown.LinkedFirms.Count - 1; i >= 0; i--)
		{
			Firm firm = FirmArray[targetTown.LinkedFirms[i]];

			if (firm.nation_recno == targetTown.NationId && firm.firm_id == Firm.FIRM_CAMP)
			{
				int campCombatLevel = ((FirmCamp)firm).total_combat_level();

				if (campCombatLevel > maxCampCombatLevel)
				{
					maxCampCombatLevel = campCombatLevel;
					bestTargetFirm = firm;
				}
			}
		}

		//----- get the defense combat level around the town ----//

		int targetCombatLevel =
			ai_evaluate_target_combat_level(targetTown.LocCenterX, targetTown.LocCenterY, targetTown.NationId);

		//----------------------------------------//

		if (bestTargetFirm != null)
		{
			Nation targetNation = NationArray[bestTargetFirm.nation_recno];

			if (targetNation.is_at_war()) // use all camps force if the nation is at war
				useAllCamp = true;

			return ai_attack_target(bestTargetFirm.loc_x1, bestTargetFirm.loc_y1, targetCombatLevel, false,
				0, 0, useAllCamp)
				? 1
				: 0;
		}
		else
		{
			//--- if there are any mobile defense force around the town ----//

			if (targetCombatLevel > 0)
				return ai_attack_target(targetTown.LocCenterX, targetTown.LocCenterY, targetCombatLevel, true) ? 1 : 0;
		}

		return -1;
	}

	public Town think_capture_enemy_town_target(Town capturerTown)
	{
		Town bestTown = null;
		int bestRating = Int32.MinValue;

		foreach (Town town in TownArray)
		{
			if (town.NationId == 0 || town.NationId == nation_recno)
				continue;

			if (town.RegionId != capturerTown.RegionId)
				continue;

			//----- if we have already built a camp next to this town -----//

			if (town.HasLinkedCamp(nation_recno, false)) //0-count both camps with or without overseers
				continue;

			//--------- only attack enemies -----------//

			NationRelation nationRelation = get_relation(town.NationId);

			bool rc = false;

			if (nationRelation.status == NATION_HOSTILE)
				rc = true;

			// even if the relation is not hostile, if the ai_relation_level is < 10, attack anyway
			else if (nationRelation.ai_relation_level < 10)
				rc = true;

			else if (nationRelation.status <= NATION_NEUTRAL &&
			         town.NationId == NationArray.max_overall_nation_recno && // if this is our biggest enemy
			         nationRelation.ai_relation_level < 30)
			{
				rc = true;
			}

			if (!rc)
				continue;

			//----- if this town does not have any linked camps, capture this town immediately -----//

			if (!town.HasLinkedCamp(town.NationId, false)) //0-count both camps with or without overseers
				return town;

			//--- if the enemy is very powerful overall, don't attack it yet ---//

			/*if( NationArray[targetTown.nation_recno].military_rank_rating() >
				 military_rank_rating() * (80+pref_military_courage/2) / 100 )
			{
				continue;
			}*/

			//------ only attack if we have enough money to support the war ----//

			if (!ai_should_spend_war(NationArray[town.NationId].military_rank_rating()))
				continue;

			//---- do not attack this town because a battle is already going on ----//

			if (is_battle(town.LocCenterX, town.LocCenterY) > 0)
				continue;

			int townCombatLevel = ai_evaluate_target_combat_level(town.LocCenterX, town.LocCenterY, town.NationId);

			//------- calculate the rating --------------//

			//DieselMachine TODO better calculate distance rating
			//curRating = world.distance_rating(capturerTown.center_x, capturerTown.center_y, targetTown.center_x, targetTown.center_y);
			int curRating = 0;

			curRating -= townCombatLevel / 10;

			curRating -= town.AverageLoyalty();

			curRating += town.Population; // put a preference on capturing villages with large population

			//----- the power of between the nation also affect the rating ----//

			//curRating += 2 * (military_rank_rating() - NationArray[targetTown.nation_recno].military_rank_rating());

			//-- AI Aggressive is set above Low, than the AI will try to capture the player's town first ---//

			if (!town.AITown)
			{
				switch (Config.ai_aggressiveness)
				{
					case Config.OPTION_MODERATE:
						curRating += 100;
						break;

					case Config.OPTION_HIGH:
						curRating += 300;
						break;

					case Config.OPTION_VERY_HIGH:
						curRating += 500;
						break;
				}
			}

			//--- if there are mines linked to this town, increase its rating ---//

			for (int i = town.LinkedFirms.Count - 1; i >= 0; i--)
			{
				Firm firm = FirmArray[town.LinkedFirms[i]];

				if (firm.nation_recno != town.NationId)
					continue;

				if (firm.firm_id == Firm.FIRM_MINE)
				{
					//--- if this mine's raw materials is one that we don't have --//

					FirmMine firmMine = (FirmMine)firm;
					if (raw_count_array[firmMine.raw_id - 1] == 0)
						curRating += 150 * (int)firmMine.reserve_qty / GameConstants.MAX_RAW_RESERVE_QTY;
				}
			}

			//--- more linked towns increase the attractiveness rating ---//

			curRating += town.LinkedFirms.Count * 5;

			curRating = curRating * World.DistanceRating(capturerTown.LocCenterX, capturerTown.LocCenterY,
				town.LocCenterX, town.LocCenterY) / 100;

			//-------- compare with the current best rating ---------//

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestTown = town;
			}
		}

		return bestTown;
	}

	public int is_battle(int targetXLoc, int targetYLoc)
	{
		//--- the scanning distance is determined by the AI aggressiveness setting ---//

		int scanRangeX = 5 + Config.ai_aggressiveness * 2;
		int scanRangeY = scanRangeX;

		int xLoc1 = targetXLoc - scanRangeX;
		int yLoc1 = targetYLoc - scanRangeY;
		int xLoc2 = targetXLoc + scanRangeX;
		int yLoc2 = targetYLoc + scanRangeY;

		xLoc1 = Math.Max(xLoc1, 0);
		yLoc1 = Math.Max(yLoc1, 0);
		xLoc2 = Math.Min(xLoc2, GameConstants.MapSize - 1);
		yLoc2 = Math.Min(yLoc2, GameConstants.MapSize - 1);

		//------------------------------------------//

		int isBattle = 0;

		for (int yLoc = yLoc1; yLoc <= yLoc2; yLoc++)
		{
			for (int xLoc = xLoc1; xLoc <= xLoc2; xLoc++)
			{
				Location location = World.GetLoc(xLoc, yLoc);
				if (!location.HasUnit(UnitConstants.UNIT_LAND))
					continue;

				Unit unit = UnitArray[location.UnitId(UnitConstants.UNIT_LAND)];
				if (unit.cur_action == Sprite.SPRITE_ATTACK)
				{
					if (unit.nation_recno == nation_recno)
					{
						return 2;
					}
					else
					{
						isBattle = 1;
					}
				}
			}
		}

		return isBattle;
	}

	public bool should_use_cash_to_capture()
	{
		return /*military_rank_rating() < 50+pref_peacefulness/5 &&		// 50 to 70*/
			ai_should_spend(pref_loyalty_concern / 4);
	}

	//--------------------------------------------------------------//
	// marine functions
	//--------------------------------------------------------------//

	public void think_marine()
	{
		return;
		//if( pref_use_marine < 50 )		// don't use marine at all
		//return;

		if (!ai_should_spend(20 + pref_use_marine / 2)) // 20 to 70 importance rating
			return;

		//--- think over building harbor network ---//

		think_build_harbor_network();

		if (ai_harbor_array.Count == 0)
			return;

		//------ think about sea attack enemies -------//

		if (Misc.Random(3) == 0) // 33% chance
		{
			if (think_sea_attack_enemy())
				return;
		}

		//---- check if it is safe for sea travel now ----//

		if (!ai_is_sea_travel_safe())
			return;

		//----- think over moving between regions -----//

		think_move_between_region();

		//think_move_to_region_with_mine();
	}

	public bool think_build_harbor_network()
	{
		//--- only build one harbor at a time, to avoid double building ---//

		if (is_action_exist(ACTION_AI_BUILD_FIRM, Firm.FIRM_HARBOR))
			return false;

		//--------------------------------------------//

		for (int i = 0; i < RegionArray.RegionStats.Count; i++)
		{
			RegionStat regionStat = RegionArray.RegionStats[i];

			//--- only build on those regions that this nation has base towns ---//
			if (regionStat.BaseTownNationCounts[nation_recno - 1] == 0)
				continue;

			// if we already have a harbor in this region
			if (regionStat.HarborNationCounts[nation_recno - 1] > 0)
				continue;

			//-----------------------------------------------------------------------//
			//
			// Scan thru all regions which this region can be connected to thru sea.
			// If one of them is worth our landing, then builld a harbor in this 
			// region so we can sail to that region. 
			//
			//-----------------------------------------------------------------------//

			foreach (RegionPath regionPath in regionStat.ReachableRegions)
			{
				//--------------------------------------//

				// if we have already built one harbor, then we should continue to build others asa single harbor isn't useful
				if (ai_harbor_array.Count == 0 && ai_should_sail_to_rating(regionPath.LandRegionStatId) <= 0)
					continue;

				//--------- build a harbor now ---------//

				if (ai_build_harbor(regionStat.RegionId, regionPath.SeaRegionId))
					return true;
			}
		}

		return false;
	}

	public bool think_move_between_region()
	{
		if (think_move_people_between_region())
			return true;

		if (think_move_troop_between_region())
			return true;

		return false;
	}

	public bool think_move_troop_between_region()
	{
		//----- find the region with the least population -----//

		int maxCampCount = 0, minCampCount = Int32.MaxValue;
		int maxRegionId = 0, minRegionId = 0;
		int minRegionRating = 0;

		for (int i = 0; i < RegionArray.RegionStats.Count; i++)
		{
			RegionStat regionStat = RegionArray.RegionStats[i];
			if (regionStat.NationPresenceCount == 0 && regionStat.IndependentTownCounts == 0 &&
			    regionStat.RawResourceCount == 0)
			{
				continue;
			}

			int campCount = regionStat.CampNationCounts[nation_recno - 1];

			if (campCount > maxCampCount)
			{
				maxCampCount = campCount;
				maxRegionId = regionStat.RegionId;
			}

			if (campCount <= minCampCount)
			{
				int curRating = ai_should_sail_to_rating(i + 1);

				if (campCount < minCampCount || curRating >= minRegionRating)
				{
					minCampCount = campCount;
					minRegionId = regionStat.RegionId;
					minRegionRating = curRating;
				}
			}
		}

		if (maxRegionId == 0 || minRegionId == 0 || maxRegionId == minRegionId)
			return false;

		//----- only move if the difference is big enough ------//

		int minJoblessPop = RegionArray.GetRegionStat(minRegionId).NationJoblessPopulation[nation_recno - 1];
		int maxJoblessPop = RegionArray.GetRegionStat(maxRegionId).NationJoblessPopulation[nation_recno - 1];

		if (pref_use_marine < 90) // if > 90, it will ignore all these and move anyway  
		{
			if (minCampCount == 0)
			{
				// 150 to 200 (pref_use_marine is always >= 50, if it is < 50, marine functions are not called at all
				if (maxJoblessPop - minJoblessPop < 200 - pref_use_marine)
					return false;
			}
			else
			{
				// 100 to 150 (pref_use_marine is always >= 50, if it is < 50, marine functions are not called at all
				if (maxJoblessPop - minJoblessPop < 150 - pref_use_marine)
					return false;
			}
		}
		else
		{
			if (maxJoblessPop < 20) // don't move if we only have a few jobless people
				return false;
		}

		//------------ see if we have any camps in the region -----------//

		int destRegionId = minRegionId;

		for (int i = 0; i < ai_camp_array.Count; i++)
		{
			Firm firm = FirmArray[ai_camp_array[i]];

			// if it's under construction there may be unit waiting outside of the camp
			if (firm.region_id == destRegionId && !firm.under_construction)
			{
				//--- if there is one, must move the troop close to it ---//

				return ai_patrol_to_region(firm.center_x, firm.center_y, SEA_ACTION_NONE);
			}
		}
		//----- if we don't have any camps in the region, build one ----//

		int xLoc = 0, yLoc = 0;
		FirmInfo firmInfo = FirmRes[Firm.FIRM_CAMP];

		if (World.LocateSpaceRandom(ref xLoc, ref yLoc, GameConstants.MapSize - 1, GameConstants.MapSize - 1,
			    firmInfo.loc_width, firmInfo.loc_height, GameConstants.MapSize * GameConstants.MapSize, destRegionId, true))
		{
			return ai_patrol_to_region(xLoc, yLoc, SEA_ACTION_BUILD_CAMP);
		}

		return false;
	}

	public bool think_move_people_between_region()
	{
		//----- find the region with the least population -----//

		int maxJoblessPop = 0, minJoblessPop = Int32.MinValue;
		int maxRegionId = 0, minRegionId = 0;

		for (int i = 0; i < RegionArray.RegionStats.Count; i++)
		{
			RegionStat regionStat = RegionArray.RegionStats[i];

			//--- only move to regions in which we have camps ---//

			if (regionStat.CampNationCounts[nation_recno - 1] == 0)
				continue;

			int joblessPop = regionStat.NationJoblessPopulation[nation_recno - 1];

			if (joblessPop > maxJoblessPop)
			{
				maxJoblessPop = joblessPop;
				maxRegionId = regionStat.RegionId;
			}

			if (joblessPop < minJoblessPop)
			{
				minJoblessPop = joblessPop;
				minRegionId = regionStat.RegionId;
			}
		}

		if (maxRegionId == 0 || minRegionId == 0 || maxRegionId == minRegionId)
			return false;

		//----- only move if the difference is big enough ------//

		if (pref_use_marine < 90) // if > 90, it will ignore all these and move anyway
		{
			// 100 to 150 (pref_use_marine is always >= 50, if it is < 50, marine functions are not called at all
			if (maxJoblessPop - minJoblessPop < 150 - pref_use_marine)
				return false;
		}
		else
		{
			if (maxJoblessPop < 20) // don't move if we only have a few jobless people
				return false;
		}

		//------------ see if we have any towns in the region -----------//

		int destRegionId = minRegionId;

		for (int i = 0; i < ai_town_array.Count; i++)
		{
			Town town = TownArray[ai_town_array[i]];

			if (town.RegionId == destRegionId)
			{
				//--- if there is one, must move the people to it ---//

				return ai_settle_to_region(town.LocCenterX, town.LocCenterY, SEA_ACTION_NONE);
			}
		}

		//----- if we don't have any towns in the region, settle one ----//

		int xLoc = 0, yLoc = 0;

		if (World.LocateSpaceRandom(ref xLoc, ref yLoc,
			    GameConstants.MapSize - 1, GameConstants.MapSize - 1,
			    InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT,
			    GameConstants.MapSize * GameConstants.MapSize, destRegionId, true))
		{
			return ai_settle_to_region(xLoc, yLoc, SEA_ACTION_SETTLE);
		}

		return false;
	}

	public bool think_sea_attack_enemy()
	{
		if (total_ship_combat_level < 700 - (pref_military_courage + pref_use_marine) * 2) // 300 to 700
			return false;

		//-----------------------------------------//

		foreach (Firm firm in FirmArray.EnumerateRandom())
		{
			if (firm.firm_id != Firm.FIRM_HARBOR)
				continue;

			if (get_relation_status(firm.nation_recno) != NATION_HOSTILE)
				continue;

			//--- if the AI has more powerful fleets than the enemy ---//

			if (total_ship_combat_level > NationArray[firm.nation_recno].total_ship_combat_level)
			{
				ai_sea_attack_target(firm.center_x, firm.center_y);
				return true;
			}
		}

		return false;
	}

	public bool think_move_to_region_with_mine()
	{
		if (total_jobless_population < 30)
			return false;

		//---------------------------------------------------//

		int bestRating = 0, bestRegionId = 0;

		for (int i = 0; i < RegionArray.RegionStats.Count; i++)
		{
			RegionStat regionStat = RegionArray.RegionStats[i];
			if (regionStat.TownNationCounts[nation_recno - 1] > 0) // if we already have towns there
				continue;

			if (regionStat.RawResourceCount == 0)
				continue;

			//-- if we have already build one camp there, just waiting for sending a few peasants there, then process it first --//

			if (regionStat.CampNationCounts[nation_recno - 1] > 0)
			{
				bestRegionId = regionStat.RegionId;
				break;
			}

			//-----------------------------------------------//

			int curRating = regionStat.RawResourceCount * 3 - regionStat.NationPresenceCount;

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestRegionId = regionStat.RegionId;
			}
		}

		if (bestRegionId == 0)
			return false;

		//----- select the raw site to acquire -----//

		Site bestSite = null;
		foreach (Site site in SiteArray)
		{
			//TODO only raw resource?
			if (site.RegionId == bestRegionId)
			{
				bestSite = site;
				break;
			}
		}

		if (bestSite == null)
			return false;

		//----- decide the location of the settlement -----//

		return ai_build_camp_town_next_to(bestSite.LocX - 1, bestSite.LocY - 1, bestSite.LocX + 1, bestSite.LocY + 1);
	}

	public bool ai_build_camp_town_next_to(int xLoc1, int yLoc1, int xLoc2, int yLoc2)
	{
		//---- first see if we already have a camp in the region ---//

		int regionId = World.GetRegionId(xLoc1, yLoc1);

		if (RegionArray.GetRegionInfo(regionId).RegionStatId == 0)
			return false;

		if (RegionArray.GetRegionStat(regionId).CampNationCounts[nation_recno - 1] == 0)
		{
			//--- if we don't have one yet, build one next to the destination ---//

			if (!World.LocateSpace(ref xLoc1, ref yLoc1, xLoc2, yLoc2, 3, 3,
				    UnitConstants.UNIT_LAND, regionId, true))
			{
				return false;
			}

			if (World.CanBuildFirm(xLoc1, yLoc1, Firm.FIRM_CAMP) == 0)
				return false;

			return ai_patrol_to_region(xLoc1, yLoc1, SEA_ACTION_BUILD_CAMP);
		}
		else //-- if there's already a camp there, then set people there to settle --//
		{
			for (int i = 0; i < ai_camp_array.Count; i++)
			{
				FirmCamp firmCamp = (FirmCamp)FirmArray[ai_camp_array[i]];

				if (firmCamp.region_id != regionId)
					continue;

				xLoc1 = firmCamp.loc_x1;
				yLoc1 = firmCamp.loc_y1;
				xLoc2 = firmCamp.loc_x2;
				yLoc2 = firmCamp.loc_y2;

				if (World.LocateSpace(ref xLoc1, ref yLoc1, xLoc2, yLoc2, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT,
					    UnitConstants.UNIT_LAND, regionId, true))
				{
					if (World.CanBuildTown(xLoc1, yLoc1))
						return ai_settle_to_region(xLoc1, yLoc1, SEA_ACTION_SETTLE);
				}
			}
		}

		return false;
	}

	public bool ai_settle_to_region(int destXLoc, int destYLoc, int seaActionId)
	{
		const int SETTLE_REGION_UNIT_COUNT = 9; // no. of units to move to settle on a new region each time

		//---- think about which town to recruit the people -----//

		int destRegionId = World.GetRegionId(destXLoc, destYLoc);
		int bestRating = 0;
		Town bestTown = null;

		for (int i = 0; i < ai_town_array.Count; i++)
		{
			Town town = TownArray[ai_town_array[i]];

			// if there is no command base linked to this town, we cannot recruit any peasants from it
			if (!town.HasLinkedOwnCamp)
				continue;

			// don't get peasant from this town if the jobless population is less than 20
			if (town.JoblessPopulation < SETTLE_REGION_UNIT_COUNT)
				continue;

			//--- only send units from this region if we have a harbor in that region ---//

			// region_stat_id of a region may be zero
			if (
				RegionArray.GetRegionInfo(town.RegionId).RegionStatId == 0 ||
				RegionArray.GetRegionStat(town.RegionId).HarborNationCounts[nation_recno - 1] == 0)
			{
				continue;
			}

			int curRating = World.DistanceRating(destXLoc, destYLoc, town.LocCenterX, town.LocCenterY);

			curRating += town.JoblessPopulation;

			curRating += town.AverageLoyalty(); // select a town with high loyalty

			if (curRating <= bestRating)
				continue;

			//------- see if we have ships ready currently -----//

			int seaRegionId = RegionArray.GetSeaPathRegionId(town.RegionId, destRegionId);

			// 0-don't have to find the best, return immediately whenever a suitable one is found
			if (ai_find_transport_ship(seaRegionId, town.LocCenterX, town.LocCenterY, false) == 0)
				continue;

			bestRating = curRating;
			bestTown = town;
		}

		if (bestTown == null)
			return false;

		//------- try to recruit 9 units from one of our towns --------//

		List<int> recruitedUnits = new List<int>(SETTLE_REGION_UNIT_COUNT);
		int raceId = bestTown.MajorityRace();

		while (recruitedUnits.Count < SETTLE_REGION_UNIT_COUNT)
		{
			if (bestTown.RecruitableRacePopulation(raceId, true) > 0)
			{
				if (!bestTown.CanRecruit(raceId))
					break;
				int unitRecno = bestTown.Recruit(-1, raceId, InternalConstants.COMMAND_AI);

				if (unitRecno == 0) // no space for new unit 
					break;

				recruitedUnits.Add(unitRecno);
			}
			else
			{
				raceId = bestTown.PickRandomRace(false, true);

				if (raceId == 0)
					break;
			}
		}

		//--- if due to some reasons that the no. of units recruited is less than half of what we need, do not continue to sea travel.

		if (recruitedUnits.Count < SETTLE_REGION_UNIT_COUNT / 2)
			return false;

		ActionNode actionNode = add_action(destXLoc, destYLoc, 0, 0,
			ACTION_AI_SEA_TRAVEL, seaActionId, recruitedUnits.Count, 0, 0, recruitedUnits);

		if (actionNode != null) // must process it immediately otherwise the recruited units will do something else
			return process_action(actionNode);
		else
			return false;
	}

	public bool ai_patrol_to_region(int destXLoc, int destYLoc, int seaActionId)
	{
		//---- think about which town to recruit the people -----//

		int destRegionId = World.GetRegionId(destXLoc, destYLoc);
		int bestRating = 0;
		int kingRecno = NationArray[nation_recno].king_unit_recno;
		FirmCamp bestCamp = null;

		for (int i = 0; i < ai_camp_array.Count; i++)
		{
			FirmCamp firmCamp = (FirmCamp)FirmArray[ai_camp_array[i]];

			// only when the camp is filled with workers
			if (firmCamp.overseer_recno == 0 || firmCamp.workers.Count != Firm.MAX_WORKER)
				continue;

			if (firmCamp.ai_is_capturing_independent_village()) // the base is trying to capture an independent town
				continue;

			if (firmCamp.is_attack_camp)
				continue;

			if (firmCamp.overseer_recno == kingRecno) // if the king oversees this firm
				continue;

			//--- only send units from this region if we have a harbor in that region ---//

			if (RegionArray.GetRegionInfo(firmCamp.region_id).RegionStatId == 0 ||
			    RegionArray.GetRegionStat(firmCamp.region_id).HarborNationCounts[nation_recno - 1] == 0)
			{
				continue;
			}

			int curRating = World.DistanceRating(destXLoc, destYLoc, firmCamp.center_x, firmCamp.center_y);

			if (curRating <= bestRating)
				continue;

			//------- see if we have ships ready currently -----//

			int seaRegionId = RegionArray.GetSeaPathRegionId(firmCamp.region_id, destRegionId);

			// 0-don't have to find the best, return immediately whenever a suitable one is found
			if (ai_find_transport_ship(seaRegionId, firmCamp.center_x, firmCamp.center_y, false) == 0)
				continue;

			bestRating = curRating;
			bestCamp = firmCamp;
		}

		if (bestCamp == null)
			return false;

		//----- patrol the camp troop ajnd assign it to a ship -----//

		bestCamp.patrol();

		// there could be chances that there are no some for mobilizing the units
		if (bestCamp.patrol_unit_array.Count > 0)
		{
			ActionNode actionNode = add_action(destXLoc, destYLoc, 0, 0,
				ACTION_AI_SEA_TRAVEL, seaActionId,
				bestCamp.patrol_unit_array.Count, 0, 0, bestCamp.patrol_unit_array);

			if (actionNode != null) // must process it immediately otherwise the recruited units will do something else
				return process_action(actionNode);
		}

		return false;
	}

	public int ai_should_sail_to_rating(int regionStatId)
	{
		RegionStat regionStat = RegionArray.RegionStats[regionStatId - 1];

		int curRating = regionStat.RawResourceCount * 100 +
		                regionStat.IndependentTownCounts * 20 +
		                regionStat.NationPresenceCount * 30;
		/*
			- (regionStat.total_town_count - regionStat.town_nation_count_array[nation_recno-1] ) * 10 // towns of other nations
			- (regionStat.total_firm_count - regionStat.firm_nation_count_array[nation_recno-1] ) * 5  // firms of other nations
			- (regionStat.total_unit_count - regionStat.unit_nation_count_array[nation_recno-1] ) * 2  // units of other nations
			- regionStat.independent_unit_count * 2;				// monsters or rebel units
		*/
		return curRating;
	}

	public bool ai_build_harbor(int landRegionId, int seaRegionId)
	{
		const int ADEQUATE_ENEMY_HARBOR_DISTANCE = 10;

		//---- randomly pick a base town of this nation ----//

		int townSeq = Misc.Random(ai_town_array.Count);

		Town targetTown = null;
		for (int i = 0; i < ai_town_array.Count; i++)
		{
			if (++townSeq >= ai_town_array.Count)
				townSeq = 0;

			Town town = TownArray[ai_town_array[townSeq]];

			if (town.IsBaseTown && landRegionId == town.RegionId)
			{
				targetTown = town;
				break;
			}
		}

		if (targetTown == null) // not found
			return false;

		int homeXLoc = targetTown.LocCenterX;
		int homeYLoc = targetTown.LocCenterY;

		//---- scan out from the town and find the nearest suitable location to build the harbor ----//

		int xLoc = -1, yLoc = -1, bestXLoc = -1, bestYLoc = -1, maxEnemyDistance = 0;

		for (int i = 2; i < GameConstants.MapSize * GameConstants.MapSize; i++)
		{
			int xOffset;
			int yOffset;
			Misc.cal_move_around_a_point(i, GameConstants.MapSize, GameConstants.MapSize, out xOffset, out yOffset);

			xLoc = homeXLoc + xOffset;
			yLoc = homeYLoc + yOffset;

			xLoc = Math.Max(0, xLoc);
			xLoc = Math.Min(GameConstants.MapSize - 1, xLoc);

			yLoc = Math.Max(0, yLoc);
			yLoc = Math.Min(GameConstants.MapSize - 1, yLoc);

			Location location = World.GetLoc(xLoc, yLoc);

			if (!location.CanBuildWholeHarbor())
				continue;

			if (!World.IsHarborRegion(xLoc, yLoc, landRegionId, seaRegionId))
				continue;

			if (World.CanBuildFirm(xLoc, yLoc, Firm.FIRM_HARBOR) == 0)
				continue;

			//--------------------------------------//

			int enemyDistance = closest_enemy_firm_distance(Firm.FIRM_HARBOR, xLoc, yLoc);

			if (enemyDistance > maxEnemyDistance)
			{
				maxEnemyDistance = enemyDistance;
				bestXLoc = xLoc;
				bestYLoc = yLoc;

				if (enemyDistance >= ADEQUATE_ENEMY_HARBOR_DISTANCE)
					break;
			}
		}

		//--------------------------------//

		if (bestXLoc >= 0)
		{
			add_action(xLoc, yLoc, homeXLoc, homeYLoc, ACTION_AI_BUILD_FIRM, Firm.FIRM_HARBOR);
			return true;
		}

		return false;
	}

	public int ai_sea_travel(ActionNode actionNode)
	{
		Unit unit = UnitArray[actionNode.group_unit_array[0]];

		//---- figure out the sea region id which the ship should appear ----//

		int unitRegionId = World.GetRegionId(unit.next_x_loc(), unit.next_y_loc());
		int destRegionId = World.GetRegionId(actionNode.action_x_loc, actionNode.action_y_loc);

		int seaRegionId = RegionArray.GetSeaPathRegionId(unitRegionId, destRegionId);

		//------- 1. try to locate a ship --------//

		int shipUnitRecno = ai_find_transport_ship(seaRegionId, unit.next_x_loc(), unit.next_y_loc());

		// must return -1 instead of 0 as the action must be executed immediately
		// otherwise the units will be assigned with other action and the unit list may no longer be valid
		if (shipUnitRecno == 0)
			return -1;

		//---- if this ship is in the harbor, sail it out ----//

		UnitMarine unitMarine = (UnitMarine)UnitArray[shipUnitRecno];

		if (unitMarine.unit_mode == UnitConstants.UNIT_MODE_IN_HARBOR)
		{
			FirmHarbor firmHarbor = (FirmHarbor)FirmArray[unitMarine.unit_mode_para];

			firmHarbor.sail_ship(unitMarine.sprite_recno, InternalConstants.COMMAND_AI);
		}

		if (!unitMarine.is_visible()) // no space in the sea for placing the ship 
			return -1;

		//------ 2. Assign the units to the ship -------//

		unitMarine.ai_action_id = actionNode.action_id;

		UnitArray.assign_to_ship(unitMarine.next_x_loc(), unitMarine.next_y_loc(), false,
			actionNode.group_unit_array, InternalConstants.COMMAND_AI, unitMarine.sprite_recno);

		for (int i = 0; i < actionNode.instance_count; i++)
			UnitArray[actionNode.group_unit_array[i]].ai_action_id = actionNode.action_id;

		actionNode.instance_count++; // +1 for the ship

		// -1 because when we return 1, it will be increased by 1 automatically
		actionNode.processing_instance_count = actionNode.instance_count - 1;
		actionNode.action_para2 = 0; // reset it, it is set in Nation::action_finished()

		return 1;
	}

	public int ai_sea_travel2(ActionNode actionNode)
	{
		if (UnitArray.IsDeleted(actionNode.action_para2))
			return -1;

		UnitMarine unitMarine = (UnitMarine)UnitArray[actionNode.action_para2];

		if (UnitRes[unitMarine.unit_id].unit_class != UnitConstants.UNIT_CLASS_SHIP)
			return -1;

		if (unitMarine.nation_recno != nation_recno)
			return -1;

		//--------------------------------------------------------//

		unitMarine.ship_to_beach(actionNode.action_x_loc, actionNode.action_y_loc, out _, out _);
		unitMarine.ai_action_id = actionNode.action_id;

		return 1;
	}

	public int ai_sea_travel3(ActionNode actionNode)
	{
		if (UnitArray.IsDeleted(actionNode.action_para2))
			return -1;

		UnitMarine unitMarine = (UnitMarine)UnitArray[actionNode.action_para2];

		if (UnitRes[unitMarine.unit_id].unit_class != UnitConstants.UNIT_CLASS_SHIP)
			return -1;

		if (unitMarine.nation_recno != nation_recno)
			return -1;

		//-------- 4. Units disembark on the coast. -------//

		if (!unitMarine.can_unload_unit())
			return 0;

		//--- make a copy of the recnos of the unit on the ship ---//

		List<int> unitRecnoArray = new List<int>();
		unitRecnoArray.AddRange(unitMarine.UnitsOnBoard);

		unitMarine.unload_all_units(InternalConstants.COMMAND_AI); // unload all units now

		if (!ConfigAdv.fix_sea_travel_final_move)
			return 1; // finish the action.


		//---------- 5. Validate all units ----------//

		for (int i = unitRecnoArray.Count - 1; i >= 0; i--)
		{
			if (UnitArray.IsDeleted(unitRecnoArray[i]) || UnitArray[unitRecnoArray[i]].nation_recno != nation_recno)
			{
				unitRecnoArray.RemoveAt(i);
			}
		}

		if (unitRecnoArray.Count == 0)
			return -1;

		//--- 6. Unit actions after they have arrived the destination region ----//

		int destXLoc = actionNode.action_x_loc;
		int destYLoc = actionNode.action_y_loc;
		Location location = World.GetLoc(destXLoc, destYLoc);

		switch (actionNode.action_para)
		{
			case SEA_ACTION_SETTLE:
				if (location.IsTown() && TownArray[location.TownId()].NationId == nation_recno)
				{
					Town town = TownArray[location.TownId()];
					UnitArray.assign(town.LocX1, town.LocY1, false, InternalConstants.COMMAND_AI, unitRecnoArray);
				}
				else //-- if there is no town there, the unit will try to settle, if there is no space for settle, settle() will just have the units move to the destination
				{
					UnitArray.settle(destXLoc, destYLoc, false, InternalConstants.COMMAND_AI, unitRecnoArray);
				}

				break;

			case SEA_ACTION_BUILD_CAMP:
				Unit unit = UnitArray[unitRecnoArray[0]];
				unit.build_firm(destXLoc, destYLoc, Firm.FIRM_CAMP, InternalConstants.COMMAND_AI);
				unit.ai_action_id = actionNode.action_id;
				actionNode.processing_instance_count++;
				break;

			case SEA_ACTION_ASSIGN_TO_FIRM:
				if (check_firm_ready(destXLoc, destYLoc))
					UnitArray.assign(destXLoc, destYLoc, false, InternalConstants.COMMAND_AI, unitRecnoArray);
				break;

			case SEA_ACTION_MOVE:
				UnitArray.MoveTo(destXLoc, destYLoc, false, unitRecnoArray, InternalConstants.COMMAND_AI);
				break;

			case SEA_ACTION_NONE:
				// just transport them to the specific region and disembark and wait for their own actions
				break;
		}

		//---------- set the action id. of the units ---------//

		// with the exception of SEA_ACTION_BUILD_CAMP, units in all other actions are immediately executed
		if (actionNode.action_para != SEA_ACTION_BUILD_CAMP)
		{
			for (int i = unitRecnoArray.Count - 1; i >= 0; i--)
			{
				UnitArray[unitRecnoArray[i]].ai_action_id = actionNode.action_id;
				actionNode.processing_instance_count++;
			}
		}

		//---------------------------------------------//

		actionNode.processing_instance_count--; // decrease it by one as it will be increased in process_action()

		// set the instance count so process_action() won't cause error.
		actionNode.instance_count = actionNode.processing_instance_count + 1;

		return 1;
	}

	public int ai_find_transport_ship(int seaRegionId, int unitXLoc, int unitYLoc, bool findBest = true)
	{
		//------- locate a suitable ship --------//

		int bestRating = 0, bestUnitRecno = 0;

		for (int i = 0; i < ai_ship_array.Count; i++)
		{
			UnitMarine unitMarine = (UnitMarine)UnitArray[ai_ship_array[i]];

			// if there are already units in the ship or if the ship does not carry units
			if (unitMarine.UnitsOnBoard.Count > 0 || UnitRes[unitMarine.unit_id].carry_unit_capacity == 0)
			{
				continue;
			}

			//------- if this ship is in the harbor ---------//

			if (unitMarine.unit_mode == UnitConstants.UNIT_MODE_IN_HARBOR)
			{
				FirmHarbor firmHarbor = (FirmHarbor)FirmArray[unitMarine.unit_mode_para];

				if (firmHarbor.sea_region_id != seaRegionId)
					continue;
			}
			else
			{
				//--------- if this ship is on the sea ----------//
				if (!unitMarine.is_ai_all_stop())
					continue;

				if (unitMarine.region_id() != seaRegionId)
					continue;

				if (!unitMarine.is_visible())
					continue;
			}

			//--------- check if the sea region is matched ---------//

			if (!findBest) // return immediately when a suitable one is found
				return unitMarine.sprite_recno;

			int curRating = World.DistanceRating(unitXLoc, unitYLoc, unitMarine.next_x_loc(), unitMarine.next_y_loc());

			// damage + ship class
			curRating += (int)(unitMarine.hit_points / 10.0 + unitMarine.max_hit_points / 10.0);

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestUnitRecno = unitMarine.sprite_recno;
			}
		}

		return bestUnitRecno;
	}

	public int ai_build_ship(int seaRegionId, int preferXLoc, int preferYLoc, bool needTransportUnit)
	{
		//------ select the harbor for building ship -----//

		FirmHarbor bestHarbor = null;
		int bestRating = 0;

		for (int i = 0; i < ai_harbor_array.Count; i++)
		{
			FirmHarbor firmHarbor = (FirmHarbor)FirmArray[ai_harbor_array[i]];

			if (!firmHarbor.can_build_ship())
				continue;

			if (firmHarbor.sea_region_id != seaRegionId)
				continue;

			int curRating = World.DistanceRating(preferXLoc, preferYLoc, firmHarbor.center_x, firmHarbor.center_y);

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestHarbor = firmHarbor;
			}
		}

		if (bestHarbor == null)
			return 0;

		//------ think about the type of ship to build -----//

		int unitId;

		if (needTransportUnit)
		{
			if (UnitRes[UnitConstants.UNIT_GALLEON].get_nation_tech_level(nation_recno) > 0)
				unitId = UnitConstants.UNIT_GALLEON;

			else if (UnitRes[UnitConstants.UNIT_CARAVEL].get_nation_tech_level(nation_recno) > 0)
				unitId = UnitConstants.UNIT_GALLEON;

			else
				unitId = UnitConstants.UNIT_VESSEL;
		}
		else
		{
			if (UnitRes[UnitConstants.UNIT_GALLEON].get_nation_tech_level(nation_recno) > 0)
				unitId = UnitConstants.UNIT_GALLEON;
			else // don't use Caravel as it can only transport 5 units at a time
				unitId = UnitConstants.UNIT_TRANSPORT;
		}

		bestHarbor.build_ship(unitId, InternalConstants.COMMAND_AI);

		return 1;
	}

	public bool has_trade_ship(int firmRecno1, int firmRecno2)
	{
		for (int i = ai_ship_array.Count - 1; i >= 0; i--)
		{
			UnitMarine unitMarine = (UnitMarine)UnitArray[ai_ship_array[i]];

			if (unitMarine.stop_defined_num < 2)
				continue;

			if ((unitMarine.stop_array[0].firm_recno == firmRecno1 &&
			     unitMarine.stop_array[1].firm_recno == firmRecno2) ||
			    (unitMarine.stop_array[1].firm_recno == firmRecno1 &&
			     unitMarine.stop_array[0].firm_recno == firmRecno2))
			{
				return unitMarine.sprite_recno != 0;
			}
		}

		return false;
	}

	public bool ai_is_sea_travel_safe()
	{
		//--- count the no. of battle ships owned by each nation ---//

		int[] nationShipCountArray = new int[GameConstants.MAX_NATION];

		foreach (Unit unit in UnitArray)
		{
			if (unit.unit_id != UnitConstants.UNIT_CARAVEL && unit.unit_id != UnitConstants.UNIT_GALLEON)
				continue;

			nationShipCountArray[unit.nation_recno - 1]++;
		}

		//--- compare the no. of ships of ours and those of the human players ---//

		int ourBattleShipCount = nationShipCountArray[nation_recno - 1];

		foreach (Nation nation in NationArray.EnumerateRandom())
		{
			if (get_relation(nation.nation_recno).status != NATION_HOSTILE) // only check enemies
				continue;

			if (NationArray[nation.nation_recno].is_ai()) // only check human players
				continue;

			//-- if enemy has battle ships, it is not safe for sea travel, destroy them first ---//

			if (nationShipCountArray[nation.nation_recno - 1] > 0)
			{
				//--- if enemy ships significantly outnumber ours, don't do any sea travel ---//

				// 0 to 3
				if (nationShipCountArray[nation.nation_recno - 1] - ourBattleShipCount > pref_military_courage / 3)
				{
					return false;
				}
			}
		}

		return true;
	}

	public int max_human_battle_ship_count()
	{
		//--- count the no. of battle ships owned by each nation ---//

		int[] nationShipCountArray = new int[GameConstants.MAX_NATION];

		foreach (Unit unit in UnitArray)
		{
			if (unit.unit_id != UnitConstants.UNIT_CARAVEL && unit.unit_id != UnitConstants.UNIT_GALLEON)
				continue;

			nationShipCountArray[unit.nation_recno - 1]++;
		}

		//--- compare the no. of ships of ours and those of the human players ---//

		int maxShipCount = 0;

		foreach (Nation nation in NationArray)
		{
			if (nation.is_ai()) // only check human players
				continue;

			//-- if enemy has battle ships, it is not safe for sea travel, destroy them first ---//

			if (nationShipCountArray[nation.nation_recno - 1] > maxShipCount)
			{
				maxShipCount = nationShipCountArray[nation.nation_recno - 1];
			}
		}

		return maxShipCount;
	}

	//--------------------------------------------------------------//
	// spy functions
	//--------------------------------------------------------------//

	public int think_assign_spy_target_camp(int raceId, int regionId, int loc_x1, int loc_y1, int cloakedNationRecno)
	{
		if (reputation < 0)
			return 0;

		Town nearbyTown = null;
		for (int x = loc_x1 - 1; x <= loc_x1 + 1; x++)
		{
			for (int y = loc_y1 - 1; y <= loc_y1 + 1; y++)
			{
				x = Math.Max(0, x);
				x = Math.Min(GameConstants.MapSize - 1, x);
				y = Math.Max(0, y);
				y = Math.Min(GameConstants.MapSize - 1, y);
				Location location = World.GetLoc(x, y);
				if (location.IsTown())
				{
					nearbyTown = TownArray[location.TownId()];
					goto doublebreak;
				}
			}
		}

		doublebreak:
		int bestRating = Int32.MaxValue, bestFirmRecno = 0;
		int loc_x = 0;
		int loc_y = 0;
		if (nearbyTown != null && cloakedNationRecno == nearbyTown.NationId)
		{
			for (int i = nearbyTown.LinkedFirms.Count - 1; i >= 0; i--)
			{
				Firm firm = FirmArray[nearbyTown.LinkedFirms[i]];

				if (firm.nation_recno == nation_recno) // don't assign to own firm
					continue;

				if (cloakedNationRecno != 0 && firm.nation_recno != cloakedNationRecno)
					continue;

				if (firm.overseer_recno == 0 || firm.workers.Count == Firm.MAX_WORKER)
					continue;

				if (firm.majority_race() != raceId)
					continue;

				Unit overseerUnit = UnitArray[firm.overseer_recno];

				if (overseerUnit.spy_recno != 0) // if the overseer is already a spy
					continue;

				int curRating = overseerUnit.loyalty * 2 +
				                Misc.points_distance(firm.center_x, firm.center_y, loc_x1, loc_y1);

				if (curRating < bestRating)
				{
					bestRating = curRating;
					bestFirmRecno = firm.firm_recno;
					loc_x = firm.center_x;
					loc_y = firm.center_y;
				}
			}

			if (bestFirmRecno != 0)
				return bestFirmRecno;
		}

		bestRating = Int32.MaxValue;
		bestFirmRecno = 0;
		foreach (Firm firm in FirmArray)
		{
			if (firm.nation_recno == nation_recno) // don't assign to own firm
				continue;

			if (firm.region_id != regionId)
				continue;

			if (firm.overseer_recno == 0 || firm.workers.Count == Firm.MAX_WORKER)
				continue;

			if (firm.majority_race() != raceId)
				continue;

			//---------------------------------//

			Unit overseerUnit = UnitArray[firm.overseer_recno];

			if (overseerUnit.spy_recno != 0) // if the overseer is already a spy
				continue;

			int curRating = overseerUnit.loyalty * 2 +
			                Misc.points_distance(firm.center_x, firm.center_y, loc_x1, loc_y1);

			if (curRating < bestRating)
			{
				bestRating = curRating;
				bestFirmRecno = firm.firm_recno;
				loc_x = firm.center_x;
				loc_y = firm.center_y;
			}
		}

		return bestFirmRecno;
	}

	public int think_assign_spy_target_town(int raceId, int regionId)
	{
		foreach (Town town in TownArray.EnumerateRandom())
		{
			if (town.NationId == nation_recno) // don't assign to own firm
				continue;

			if (town.RegionId != regionId)
				continue;

			// -5 so that even if we assign too many spies to a town at the same time, there will still room for them
			if (town.Population > GameConstants.MAX_TOWN_POPULATION - 5)
				continue;

			if (town.MajorityRace() != raceId)
				continue;

			if (reputation < 0 && town.NationId != 0)
				continue;

			return town.TownId;
		}

		return 0;
	}

	public int think_assign_spy_own_town(int raceId, int regionId)
	{
		foreach (Town town in TownArray.EnumerateRandom())
		{
			if (town.NationId != nation_recno) // only assign to own firm
				continue;

			if (town.RegionId != regionId)
				continue;

			if (town.Population > GameConstants.MAX_TOWN_POPULATION - 5)
				continue;

			if (town.MajorityRace() != raceId)
				continue;

			int curSpyLevel = SpyArray.total_spy_skill_level(Spy.SPY_TOWN, town.TownId,
				nation_recno, out _);
			int neededSpyLevel = town.NeededAntiSpyLevel();

			if (neededSpyLevel > curSpyLevel + 30)
				return town.TownId;
		}

		return 0;
	}

	public bool think_spy_new_mission(int raceId, int regionId, out int loc_x1, out int loc_y1,
		out int cloakedNationRecno)
	{
		int townRecno = 0;
		loc_x1 = 0;
		loc_y1 = 0;
		cloakedNationRecno = 0;

		if (Misc.Random(2) == 0)
		{
			//TODO. This is a bug
			int firmRecno = think_assign_spy_target_camp(raceId, regionId, loc_x1, loc_x1, cloakedNationRecno);
			if (firmRecno != 0)
			{
				Firm firm = FirmArray[firmRecno];
				loc_x1 = firm.loc_x1;
				loc_y1 = firm.loc_y1;
				cloakedNationRecno = firm.nation_recno;

				return true;
			}
		}
		else
		{
			townRecno = think_assign_spy_target_town(raceId, regionId);
			if (townRecno != 0)
			{
				Town town = TownArray[townRecno];
				loc_x1 = town.LocX1;
				loc_y1 = town.LocY1;
				cloakedNationRecno = town.NationId;

				return true;
			}
		}

		townRecno = think_assign_spy_own_town(raceId, regionId);
		if (townRecno != 0)
		{
			Town town = TownArray[townRecno];
			loc_x1 = town.LocX1;
			loc_y1 = town.LocY1;
			cloakedNationRecno = town.NationId;

			return true;
		}

		return false;
	}

	public void think_drop_spy_identity()
	{
		Spy worstSpy = null;
		int worstSkill = Int32.MaxValue;

		foreach (Spy spy in SpyArray)
		{
			if (spy.true_nation_recno != nation_recno || spy.cloaked_nation_recno != nation_recno)
				continue;

			if (spy.spy_skill < worstSkill)
			{
				worstSpy = spy;
				worstSkill = spy.spy_skill;
			}
		}

		if (worstSpy != null)
			worstSpy.drop_spy_identity();
	}

	public void ai_start_spy_new_mission(Unit spyUnit, int loc_x1, int loc_y1, int cloakedNationRecno)
	{
		if (cloakedNationRecno == 0 || cloakedNationRecno == nation_recno)
		{
			//--- move to the independent or our town ---//
			add_action(loc_x1, loc_y1, -1, -1,
				ACTION_AI_ASSIGN_SPY, cloakedNationRecno, 1, spyUnit.sprite_recno);
		}
		else
		{
			//--- move to the random location and then change its color there ---//
			int destXLoc = Misc.Random(GameConstants.MapSize);
			int destYLoc = Misc.Random(GameConstants.MapSize);
			spyUnit.MoveTo(destXLoc, destYLoc);
		}
	}

	public bool ai_should_create_new_spy(int onlyCounterSpy)
	{
		int aiPref = (onlyCounterSpy != 0 ? pref_counter_spy : pref_spy);

		if (total_spy_count > total_population * (10 + aiPref / 10) / 100) // 10% to 20%
			return false;

		if (!ai_should_spend(aiPref / 2))
			return false;

		//--- the expense of spies should not be too large ---//

		if (expense_365days(EXPENSE_SPY) > expense_365days() * (50 + aiPref) / 400)
			return false;

		return true;
	}

	//--------------------------------------------------------------//
	// strategic grand planning functions
	//--------------------------------------------------------------//

	public void think_grand_plan()
	{
		think_deal_with_all_enemy();

		think_against_mine_monopoly();

		think_ally_against_big_enemy();

		think_attack_enemy_firm();
	}

	public int total_alliance_military()
	{
		int totalPower = military_rank_rating();

		foreach (Nation nation in NationArray)
		{
			if (nation.nation_recno == nation_recno)
				continue;

			switch (get_relation_status(nation.nation_recno))
			{
				case NATION_ALLIANCE:
					totalPower += nation.military_rank_rating() * 3 / 4; // 75%
					break;
				/*
				case NATION_FRIENDLY:
					totalPower += NationArray[i].military_rank_rating() / 2;	//50%
					break;
				*/
			}
		}

		return totalPower;
	}

	public int total_enemy_military()
	{
		int totalPower = 0;
		//int relationStatus2, enemyRelationStatus;

		foreach (Nation nation in NationArray)
		{
			if (nation.nation_recno == nation_recno)
				continue;

			int relationStatus = get_relation_status(nation.nation_recno);

			if (relationStatus == NATION_HOSTILE)
			{
				totalPower += nation.military_rank_rating();
			}
			/*
			else
			{
				//--- check this nation's status with our enemies ---//

				enemyRelationStatus = 0;

				for( int j=NationArray.size() ; j>0 ; j-- )
				{
					if( NationArray.is_deleted(j) || j==nation_recno )
						continue;

					if( get_relation_status(j) != NATION_HOSTILE )   // only if this is one of our enemies.
						continue;

					//--- check if it is allied or friendly to any of our enemies ---//

					relationStatus2 = nation.get_relation_status(j);

					if( relationStatus2 > enemyRelationStatus )		// Friendly will replace none, Alliance will replace Friendly
						enemyRelationStatus = relationStatus2;
				}

				if( enemyRelationStatus == NATION_ALLIANCE )
					totalPower += nation.military_rank_rating() * 3 / 4;	 // 75%

				else if( enemyRelationStatus == NATION_FRIENDLY )
					totalPower += nation.military_rank_rating() / 2;		 // 50%
			}
			*/
		}

		return totalPower;
	}

	public int total_enemy_count()
	{
		int totalEnemy = 0;

		foreach (Nation nation in NationArray)
		{
			if (nation.nation_recno == nation_recno)
				continue;

			if (get_relation_status(nation.nation_recno) == NATION_HOSTILE)
				totalEnemy++;
		}

		return totalEnemy;
	}

	public bool think_against_mine_monopoly()
	{
		//-- only think this after the game has been running for at least one year --//

		if (Config.ai_aggressiveness < Config.OPTION_HIGH) // only attack if aggressiveness >= high
			return false;

		if ((Info.game_date - Info.game_start_date).Days > 365)
			return false;

		if (profit_365days() > 0) // if we are making a profit, don't attack
			return false;

		//-- for high aggressiveness, it will check cash before attack, for very high aggressiveness, it won't check cash before attack ---//

		if (Config.ai_aggressiveness < Config.OPTION_VERY_HIGH) // only attack if aggressiveness >= high
		{
			if (cash > 2000 + 1000 * pref_cash_reserve / 100) // only attack if we run short of cash
				return false;
		}

		//--------------------------------------------------------//

		if (largest_town_recno == 0)
			return false;

		//--------------------------------------------------//

		int baseRegionId = TownArray[largest_town_recno].RegionId;

		// no region stat (region is too small), don't care
		if (RegionArray.GetRegionInfo(baseRegionId).RegionStatId == 0)
			return false;

		RegionStat regionStat = RegionArray.GetRegionStat(baseRegionId);

		//---- if we already have a mine in this region ----//

		if (regionStat.MineNationCounts[nation_recno - 1] > 0)
			return false;

		//----- if there is no mine in this region -----//

		if (regionStat.RawResourceCount == 0)
			return false;

		//----- if enemies have occupied all mines -----//

		int bestRating = 0, targetNationRecno = 0;

		foreach (Nation nation in NationArray)
		{
			//------ only deal with human players ------//

			if (NationArray[nation.nation_recno].is_ai() || nation.nation_recno == nation_recno)
				continue;

			//------------------------------------------//

			//todo nation_recno as index
			int mineCount = regionStat.MineNationCounts[nation.nation_recno - 1];

			int curRating = mineCount * 100 - get_relation(nation.nation_recno).ai_relation_level -
			                trade_rating(nation.nation_recno);

			if (curRating > bestRating)
			{
				bestRating = curRating;
				targetNationRecno = nation.nation_recno;
			}
		}

		if (targetNationRecno == 0)
			return false;

		//--- if the relationship with this nation is still good, don't attack yet, ask for aid first ---//

		NationRelation nationRelation = get_relation(targetNationRecno);

		if (nationRelation.ai_relation_level > 30)
		{
			int talkId;

			if (nationRelation.status >= NATION_FRIENDLY)
				talkId = TalkMsg.TALK_DEMAND_AID;
			else
				talkId = TalkMsg.TALK_DEMAND_TRIBUTE;

			if (should_diplomacy_retry(talkId, targetNationRecno))
			{
				int[] aidAmountArray = { 500, 1000, 2000 };

				int aidAmount = aidAmountArray[Misc.Random(3)];

				TalkRes.ai_send_talk_msg(targetNationRecno, nation_recno, talkId, aidAmount);
			}

			return false;
		}

		//------- attack one of the target enemy's mines -------//

		foreach (Firm firm in FirmArray)
		{
			if (firm.firm_id != Firm.FIRM_MINE || firm.nation_recno != targetNationRecno ||
			    firm.region_id != baseRegionId)
			{
				continue;
			}

			//--------------------------------------------//

			int targetCombatLevel = ai_evaluate_target_combat_level(firm.center_x, firm.center_y, firm.nation_recno);
			return ai_attack_target(firm.loc_x1, firm.loc_y1, targetCombatLevel, false,
				0, 0, true);
		}

		return false;
	}

	public int think_ally_against_big_enemy()
	{
		// don't ask for tribute too soon, as in the beginning, the ranking are all the same for all nations
		if (Info.game_date < Info.game_start_date.AddDays(365 + nation_recno * 70))
			return 0;

		//---------------------------------------//

		int enemyNationRecno = NationArray.max_overall_nation_recno;

		if (enemyNationRecno == nation_recno)
			return 0;

		//-- if AI aggressiveness > high, only deal against the player, but not other kingdoms ---//

		if (Config.ai_aggressiveness >= Config.OPTION_HIGH)
		{
			if (NationArray[enemyNationRecno].is_ai())
				return 0;
		}

		//-- if AI aggressiveness is low, don't do this against the human player --//

		else if (Config.ai_aggressiveness == Config.OPTION_LOW)
		{
			if (!NationArray[enemyNationRecno].is_ai())
				return 0;
		}

		//--- increase the ai_relation_level towards other nations except the enemy so we can ally against the enemy ---//

		Nation enemyNation = NationArray[enemyNationRecno];
		int incRelationLevel = (100 - overall_rank_rating()) / 10;

		foreach (Nation nation in NationArray)
		{
			if (nation.nation_recno == nation_recno || nation.nation_recno == enemyNationRecno)
				continue;

			int thisIncLevel = incRelationLevel * (100 - get_relation(nation.nation_recno).ai_relation_level) / 100;

			change_ai_relation_level(nation.nation_recno, thisIncLevel);
		}

		//---- don't have all nations doing it the same time ----//

		if (Misc.Random(NationArray.ai_nation_count) == 0)
			return 0;

		//---- if the trade rating is high, stay war-less with it ----//

		if (trade_rating(enemyNationRecno) + ai_trade_with_rating(enemyNationRecno) > 100 - pref_trading_tendency / 3)
			return 0;

		//---- if the nation relation level is still high, then request aid/tribute ----//

		NationRelation nationRelation = get_relation(enemyNationRecno);

		if (nationRelation.ai_relation_level > 20)
		{
			int talkId;

			if (nationRelation.status >= NATION_FRIENDLY)
				talkId = TalkMsg.TALK_DEMAND_AID;
			else
				talkId = TalkMsg.TALK_DEMAND_TRIBUTE;

			if (should_diplomacy_retry(talkId, enemyNationRecno))
			{
				int[] aidAmountArray = { 500, 1000, 2000 };

				int aidAmount = aidAmountArray[Misc.Random(3)];

				TalkRes.ai_send_talk_msg(enemyNationRecno, nation_recno, talkId, aidAmount);
			}

			return 0;
		}

		return 0;
	}

	public bool think_unite_against_big_enemy()
	{
		// only do this after 3 to 6 years into the game
		if ((Info.game_date - Info.game_start_date).Days < 365 * 3 * (100 + pref_military_development) / 100)
			return false;

		//-----------------------------------------------//

		if (Config.ai_aggressiveness < Config.OPTION_HIGH)
			return false;

		if (Config.ai_aggressiveness == Config.OPTION_HIGH)
		{
			if (Misc.Random(10) != 0)
				return false;
		}
		else // OPTION_VERY_HIGH
		{
			if (Misc.Random(5) != 0)
				return false;
		}

		//---------------------------------------//

		int enemyNationRecno = NationArray.max_overall_nation_recno;

		if (enemyNationRecno == 0)
			return false;

		Nation enemyNation = NationArray[enemyNationRecno];

		//----- only against human players -----//

		if (enemyNation.is_ai())
			return false;

		//---- find the overall rank rating of the second most powerful computer kingdom ---//

		int secondBestOverall = 0, secondBestNationRecno = 0;

		foreach (Nation nation in NationArray)
		{
			if (nation.nation_recno == enemyNationRecno)
				continue;

			if (!nation.is_ai()) // don't count human players
				continue;

			if (nation.overall_rank_rating() > secondBestOverall)
			{
				secondBestOverall = nation.overall_rank_rating();
				secondBestNationRecno = nation.nation_recno;
			}
		}

		if (secondBestNationRecno == 0 || secondBestNationRecno == nation_recno)
			return false;

		//------- don't surrender to hostile nation -------//

		// defaults to NATION_NEUTRAL
		if (get_relation_status(secondBestNationRecno) < ConfigAdv.nation_ai_unite_min_relation_level)
			return false;

		//--- if all AI kingdoms are way behind the human players, unite to against the human player ---//

		int compareRating;

		if (Config.ai_aggressiveness == Config.OPTION_HIGH)
			compareRating = 50;
		else // OPTION_VERY_AGGRESSIVE
			compareRating = 80;

		if (secondBestOverall < compareRating && secondBestNationRecno != nation_recno)
		{
			surrender(secondBestNationRecno);
			return true;
		}

		return false;
	}

	public void think_deal_with_all_enemy()
	{
		int ourMilitary = military_rank_rating();
		int enemyCount = total_enemy_count();

		foreach (Nation nation in NationArray)
		{
			if (nation.nation_recno == nation_recno)
				continue;

			if (get_relation_status(nation.nation_recno) != NATION_HOSTILE)
				continue;

			//------- think about eliminating the enemy ------//

			bool rc = (nation.total_population == 0); // the enemy has no towns left

			if (enemyCount == 1 && ourMilitary > 100 - pref_military_courage / 5) // 80 to 100
			{
				int enemyMilitary = nation.military_rank_rating();

				if (enemyMilitary < 20 && ai_should_spend_war(enemyMilitary))
					rc = true;
			}

			if (rc)
			{
				if (think_eliminate_enemy_firm(nation.nation_recno))
					continue;

				//enemy towns should be captured
				//if( think_eliminate_enemy_town(i) )
				//continue;

				think_eliminate_enemy_unit(nation.nation_recno);
				continue;
			}

			//----- think about dealing with the enemy with diplomacy -----//

			think_deal_with_one_enemy(nation.nation_recno);
		}
	}

	public void think_deal_with_one_enemy(int enemyNationRecno)
	{
		foreach (Nation nation in NationArray.EnumerateRandom())
		{
			if (nation.nation_recno == nation_recno)
				continue;

			NationRelation nationRelation = nation.get_relation(nation_recno);

			//--- if this nation is already allied to us, request it to declare war with the enemy ---//

			if (nationRelation.status == NATION_ALLIANCE &&
			    nation.get_relation_status(enemyNationRecno) != NATION_HOSTILE)
			{
				if (should_diplomacy_retry(TalkMsg.TALK_REQUEST_DECLARE_WAR, nation.nation_recno))
				{
					TalkRes.ai_send_talk_msg(nation.nation_recno, nation_recno,
						TalkMsg.TALK_REQUEST_DECLARE_WAR, enemyNationRecno);
					continue;
				}
			}

			//---- if this nation is not friendly or alliance to our enemy ----//

			if (nation.get_relation_status(enemyNationRecno) < NATION_FRIENDLY)
			{
				//--- and this nation is neutral or friendly with us ---//

				if (nationRelation.status >= NATION_NEUTRAL &&
				    nation.get_relation(enemyNationRecno).trade_treaty)
				{
					//--- ask it to join a trade embargo on the enemy ---//

					if (should_diplomacy_retry(TalkMsg.TALK_REQUEST_TRADE_EMBARGO, nation.nation_recno))
						TalkRes.ai_send_talk_msg(nation.nation_recno, nation_recno,
							TalkMsg.TALK_REQUEST_TRADE_EMBARGO, enemyNationRecno);
				}
			}
			else //---- if this nation is friendly or alliance to our enemy ----//
			{
				//----- and this nation is not at war with us -----//

				if (nationRelation.status != NATION_HOSTILE)
				{
					//--- if we do not have trade treaty with this nation, propose one ---//

					if (!nationRelation.trade_treaty)
					{
						if (should_diplomacy_retry(TalkMsg.TALK_PROPOSE_TRADE_TREATY, nation.nation_recno) &&
						    consider_trade_treaty(nation.nation_recno) > 0)
							TalkRes.ai_send_talk_msg(nation.nation_recno, nation_recno,
								TalkMsg.TALK_PROPOSE_TRADE_TREATY);
					}
					else //--- if we already have a trade treaty with this nation ---//
					{
						// if this nation is already friendly to us, propose an alliance treaty now --//

						if (nationRelation.status == NATION_FRIENDLY)
						{
							if (should_diplomacy_retry(TalkMsg.TALK_PROPOSE_ALLIANCE_TREATY, nation.nation_recno) &&
							    consider_alliance_treaty(nation.nation_recno) > 0)
								TalkRes.ai_send_talk_msg(nation.nation_recno, nation_recno,
									TalkMsg.TALK_PROPOSE_ALLIANCE_TREATY);
						}

						//-- if the nation has significant trade with us, propose a friendly treaty now --//

						// or if the product complement each other very well
						else if (nation.trade_rating(nation_recno) > 10 ||
						         nation.ai_trade_with_rating(nation_recno) >= 50)
						{
							if (should_diplomacy_retry(TalkMsg.TALK_PROPOSE_FRIENDLY_TREATY, nation.nation_recno) &&
							    consider_friendly_treaty(nation.nation_recno) > 0)
								TalkRes.ai_send_talk_msg(nation.nation_recno, nation_recno,
									TalkMsg.TALK_PROPOSE_FRIENDLY_TREATY);
						}
					}
				}
			}
		}
	}

	public bool think_eliminate_enemy_town(int enemyNationRecno)
	{
		//---- look for enemy firms to attack ----//

		foreach (Town town in TownArray)
		{
			if (town.NationId != enemyNationRecno)
				continue;

			//--- only attack if we have any base town in the enemy town's region ---//

			if (base_town_count_in_region(town.RegionId) == 0)
				continue;

			//----- take into account of the mobile units around this town -----//

			if (is_battle(town.LocCenterX, town.LocCenterY) > 0)
				continue;

			int enemyCombatLevel = ai_evaluate_target_combat_level(town.LocCenterX, town.LocCenterY, town.NationId);

			return ai_attack_target(town.LocX1, town.LocY1, enemyCombatLevel);
		}

		return false;
	}

	public bool think_eliminate_enemy_firm(int enemyNationRecno)
	{
		//---- look for enemy firms to attack ----//

		foreach (Firm firm in FirmArray)
		{
			if (firm.nation_recno != enemyNationRecno)
				continue;

			//--- only attack if we have any base town in the enemy firm's region ---//

			if (base_town_count_in_region(firm.region_id) == 0)
				continue;

			//----- take into account of the mobile units around this town -----//

			if (is_battle(firm.center_x, firm.center_y) > 0)
				continue;

			int enemyCombatLevel = ai_evaluate_target_combat_level(firm.center_x, firm.center_y, firm.nation_recno);

			return ai_attack_target(firm.loc_x1, firm.loc_y1, enemyCombatLevel);
		}

		return false;
	}

	public bool think_eliminate_enemy_unit(int enemyNationRecno)
	{
		foreach (Unit unit in UnitArray)
		{
			if (unit.nation_recno != enemyNationRecno)
				continue;

			if (!unit.is_visible() || unit.mobile_type != UnitConstants.UNIT_LAND) // only deal with land units now 
				continue;

			//--- only attack if we have any base town in the enemy unit's region ---//

			if (base_town_count_in_region(unit.region_id()) == 0)
				continue;

			//----- take into account of the mobile units around this town -----//

			if (is_battle(unit.next_x_loc(), unit.next_y_loc()) > 0)
				continue;

			int enemyCombatLevel =
				ai_evaluate_target_combat_level(unit.next_x_loc(), unit.next_y_loc(), unit.nation_recno);

			return ai_attack_target(unit.next_x_loc(), unit.next_y_loc(), enemyCombatLevel);
		}

		return false;
	}

	public bool think_attack_enemy_firm()
	{
		if (Config.ai_aggressiveness < Config.OPTION_HIGH)
			return false;

		List<int> firmsToAttack = new List<int>();
		foreach (Firm firm in FirmArray)
		{
			if (firm.nation_recno == nation_recno || firm.nation_recno == 0)
				continue;

			if (NationArray[firm.nation_recno].is_ai())
				continue;

			if (get_relation_status(firm.nation_recno) != NATION_HOSTILE)
				continue;

			bool linkedToTown = false;
			for (int j = 0; j < firm.linked_town_array.Count; j++)
			{
				Town linkedTown = TownArray[firm.linked_town_array[j]];
				if (linkedTown.NationId == firm.nation_recno)
				{
					linkedToTown = true;
					break;
				}
			}

			if (linkedToTown)
				continue;

			firmsToAttack.Add(firm.firm_recno);
		}

		if (firmsToAttack.Count == 0)
			return false;

		int targetRecno = firmsToAttack[Misc.Random(firmsToAttack.Count)];
		Firm targetFirm = FirmArray[targetRecno];
		int targetCombatLevel =
			ai_evaluate_target_combat_level(targetFirm.loc_x1, targetFirm.loc_y1, targetFirm.nation_recno);
		return ai_attack_target(targetFirm.loc_x1, targetFirm.loc_y1, targetCombatLevel, false,
			0, 0, false);
	}

	public bool think_surrender()
	{
		//--- don't surrender if the nation still has a town ---//

		bool rc = (total_population == 0);

		if (cash <= 0 && income_365days() == 0)
			rc = true;

		if (!rc)
			return false;

		//---- see if there is any nation worth getting our surrender ---//

		int bestRating = 0, bestNationRecno = 0;

		if (king_unit_recno == 0) // if there is no successor to the king, the nation will tend more to surrender
			bestRating = -100;

		foreach (Nation nation in NationArray)
		{
			if (nation.nation_recno == nation_recno)
				continue;

			if (!get_relation(nation.nation_recno).has_contact) // don't surrender to a nation without contact
				continue;

			if (nation.cash <= 300.0) // don't surrender to an economically handicapped nation
				continue;

			int curRating = ai_surrender_to_rating(nation.nation_recno);

			//--- the nation will tend to surrender if there is only a small number of units left ---//

			curRating += 50 - total_unit_count * 5;

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestNationRecno = nation.nation_recno;
			}
		}

		//---------------------------------------//

		if (bestNationRecno != 0)
		{
			surrender(bestNationRecno);
			return true;
		}

		return false;
	}

	public int ai_surrender_to_rating(int nationRecno)
	{
		Nation nation = NationArray[nationRecno];
		NationRelation nationRelation = get_relation(nationRecno);

		//--- higher tendency to surrender to a powerful nation ---//

		int curRating = nation.overall_rank_rating() - overall_rank_rating();

		curRating += nationRelation.ai_relation_level - 40;

		curRating += (int)nationRelation.good_relation_duration_rating * 3;

		curRating += (int)nation.reputation / 2;

		//------ shouldn't surrender to an enemy --------//

		if (nationRelation.status == NATION_HOSTILE)
			curRating -= 100;

		//--- if the race of the kings are the same, the chance is higher ---//

		if (RaceRes.is_same_race(nation.race_id, race_id))
			curRating += 20;

		return curRating;
	}

	//--------------------------------------------------------------//
	// functions for responding to diplomatic messages
	//--------------------------------------------------------------//

	public void think_diplomacy()
	{
		//--- process incoming messages first, so we won't send out the same request to nation which has already proposed the same thing ---//

		int nationRecno = nation_recno;

		process_action(null, ACTION_AI_PROCESS_TALK_MSG);

		// the nation may have been deleted, if the nation accepts a purchase kingdom offer
		if (NationArray.IsDeleted(nationRecno))
			return;

		//---- thinking about war first -----//

		if (think_declare_war())
			return;

		//----- think buy food first -------//

		think_request_buy_food(); // don't return even if this request is sent

		//----- think request cease fire ----//

		if (think_request_cease_war())
			return;

		//------ thinking about treaty ---------//

		if (think_trade_treaty())
			return;

		if (think_propose_alliance_treaty()) // try proposing alliance treaty first, then try proposing friendly treaty
			return;

		if (think_propose_friendly_treaty())
			return;

		if (think_end_treaty())
			return;

		//-------- think about other matters --------//

		if (think_demand_tribute_aid())
			return;

		if (think_give_tech())
			return;

		if (think_demand_tech())
			return;

		//---- think about offering to purchase throne ----//

		if (think_request_surrender())
			return;
	}

	public bool think_trade_treaty()
	{
		foreach (Nation nation in NationArray.EnumerateRandom())
		{
			if (nation.nation_recno == nation_recno)
				continue;

			NationRelation ourRelation = get_relation(nation.nation_recno);

			if (!ourRelation.has_contact)
				continue;

			//------- propose a trade treaty --------//

			if (!ourRelation.trade_treaty)
			{
				if (consider_trade_treaty(nation.nation_recno) > 0)
				{
					if (should_diplomacy_retry(TalkMsg.TALK_PROPOSE_TRADE_TREATY, nation.nation_recno))
					{
						TalkRes.ai_send_talk_msg(nation.nation_recno, nation_recno,
							TalkMsg.TALK_PROPOSE_TRADE_TREATY);
						ourRelation.ai_demand_trade_treaty = 0;
						return true;
					}
				}
			}
		}

		return false;
	}

	public bool think_propose_friendly_treaty()
	{
		//--- think about which nation this nation should propose treaty to ---//

		int bestRating = 0, bestNationRecno = 0;

		foreach (Nation nation in NationArray)
		{
			if (nation.nation_recno == nation_recno)
				continue;

			NationRelation nationRelation = get_relation(nation.nation_recno);

			if (!nationRelation.has_contact || nationRelation.status >= NATION_FRIENDLY)
				continue;

			if (!should_diplomacy_retry(TalkMsg.TALK_PROPOSE_FRIENDLY_TREATY, nation.nation_recno))
				continue;

			int curRating = consider_friendly_treaty(nation.nation_recno);

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestNationRecno = nation.nation_recno;
			}
		}

		if (bestNationRecno != 0)
		{
			TalkRes.ai_send_talk_msg(bestNationRecno, nation_recno, TalkMsg.TALK_PROPOSE_FRIENDLY_TREATY);
			return true;
		}

		return false;
	}

	public bool think_propose_alliance_treaty()
	{
		//--- think about which nation this nation should propose treaty to ---//

		int bestRating = 0, bestNationRecno = 0;

		foreach (Nation nation in NationArray)
		{
			if (nation.nation_recno == nation_recno)
				continue;

			NationRelation nationRelation = get_relation(nation.nation_recno);

			if (!nationRelation.has_contact || nationRelation.status == NATION_ALLIANCE)
				continue;

			if (!should_diplomacy_retry(TalkMsg.TALK_PROPOSE_ALLIANCE_TREATY, nation.nation_recno))
				continue;

			int curRating = consider_alliance_treaty(nation.nation_recno);

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestNationRecno = nation.nation_recno;
			}
		}

		if (bestNationRecno != 0)
		{
			TalkRes.ai_send_talk_msg(bestNationRecno, nation_recno, TalkMsg.TALK_PROPOSE_ALLIANCE_TREATY);
			return true;
		}

		return false;
	}

	public bool think_end_treaty()
	{
		if (pref_honesty < 30) // never formally end a treaty if the honesty is < 30
			return false;

		foreach (Nation nation in NationArray.EnumerateRandom())
		{
			if (nation.nation_recno == nation_recno)
				continue;

			NationRelation nationRelation = get_relation(nation.nation_recno);

			if (nationRelation.status < NATION_FRIENDLY)
				continue;

			if (nationRelation.ai_secret_attack ||
			    (nationRelation.ai_relation_level < 30 && trade_rating(nation.nation_recno) < 50))
			{
				//--- don't change terminate treaty too soon ---//

				// only after 60 to 110 days
				if (Info.game_date < nationRelation.last_change_status_date.AddDays(60 + pref_honesty / 2))
					continue;

				//----------------------------------------//

				if (!TalkRes.can_send_msg(nation.nation_recno, nation_recno,
					    nationRelation.status == NATION_FRIENDLY
						    ? TalkMsg.TALK_END_FRIENDLY_TREATY
						    : TalkMsg.TALK_END_ALLIANCE_TREATY))
					continue;

				//-----------------------------------------//
				// What makes it tend to end treaty:
				// -higher honesty
				// -a larger overall power over the target nation.
				//
				// If honesty is > 50, if will end treaty
				// if its power is equal to the enemy.
				//
				// If honesty is < 50, if will end treaty
				// if its power is larger than the enemy.
				//
				// If honesty is > 50, if will end treaty
				// even if its power is lower than the enemy.
				//-----------------------------------------//

				if (pref_honesty - 50 > nation.overall_rating - overall_rating)
				{
					if (nationRelation.status == NATION_FRIENDLY)
						TalkRes.ai_send_talk_msg(nation.nation_recno, nation_recno,
							TalkMsg.TALK_END_FRIENDLY_TREATY);
					else
						TalkRes.ai_send_talk_msg(nation.nation_recno, nation_recno,
							TalkMsg.TALK_END_ALLIANCE_TREATY);

					return true;
				}
			}
		}

		return false;
	}

	public bool think_request_cease_war()
	{
		foreach (Nation nation in NationArray)
		{
			if (nation.nation_recno == nation_recno)
				continue;

			NationRelation nationRelation = get_relation(nation.nation_recno);

			if (nationRelation.status != NATION_HOSTILE)
				continue;

			if (!should_diplomacy_retry(TalkMsg.TALK_REQUEST_CEASE_WAR, nation.nation_recno))
				continue;

			//----- think about if it should cease war with the nation ------//

			if (consider_cease_war(nation.nation_recno) > 0)
			{
				TalkRes.ai_send_talk_msg(nation.nation_recno, nation_recno, TalkMsg.TALK_REQUEST_CEASE_WAR);
			}

			//--------------------------------------------//
			// The relation improves slowly if there is
			// no attack. However, if there is any battles
			// started between the two nations, the status will be
			// set to hostile and ai_relation_level will be set to 0 again.
			//--------------------------------------------//

			else
			{
				change_ai_relation_level(nation.nation_recno, 1);
			}
		}

		return false;
	}

	public int think_request_buy_food()
	{
		//------ first see if we need to buy food ------//

		int yearFoodChange = yearly_food_change();
		int neededFoodLevel;

		if (yearFoodChange > 0)
		{
			if (food > 0)
				return 0;
			else
				neededFoodLevel = (int)-food; // if the food is negative
		}
		else
		{
			neededFoodLevel = -yearFoodChange * (100 + pref_food_reserve) / 50;

			if (food > neededFoodLevel) // one to three times (based on pref_food_reserve) of the food needed in a year,
				return 0;
		}

		//----- think about which nation to buy food from -----//

		Nation bestNation = null;
		int bestRating = 0;

		foreach (Nation nation in NationArray)
		{
			if (nation.nation_recno == nation_recno)
				continue;

			if (nation.food < 500) // if the nation is short of food itself. The minimum request purchase qty is 500
				continue;

			int relationStatus = get_relation_status(nation.nation_recno);

			if (relationStatus == NATION_HOSTILE || !get_relation(nation.nation_recno).has_contact)
				continue;

			if (nation.yearly_food_change() < 0 && nation.food < 1500)
				continue;

			if (!should_diplomacy_retry(TalkMsg.TALK_REQUEST_BUY_FOOD, nation.nation_recno))
				continue;

			//-----------------------------------//

			int curRating = relationStatus * 20 + (int)nation.food / 100 + nation.yearly_food_change() / 10;

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestNation = nation;
			}
		}

		if (bestNation == null)
			return 0;

		//------------------------------------//

		int[] buyQtyArray = { 500, 1000, 2000, 4000 };

		int buyQty = 0, buyPrice;

		for (int i = buyQtyArray.Length - 1; i >= 0; i--)
		{
			if (bestNation.food / 2 > buyQtyArray[i])
			{
				buyQty = buyQtyArray[i];
				break;
			}
		}

		if (buyQty == 0)
			return 0;

		//------- set the offering price ------//

		if (food < neededFoodLevel / 4.0) // if we need the food badly
		{
			buyPrice = 30;
		}
		else if (food < neededFoodLevel / 3.0)
		{
			buyPrice = 20;
		}
		else
		{
			// if the nation has plenty and food or if the nation runs short of cash
			if (bestNation.food > bestNation.all_population() * GameConstants.PERSON_FOOD_YEAR_CONSUMPTION * 5 &&
			    bestNation.cash < bestNation.fixed_expense_365days() / 2)
			{
				buyPrice = 5;
			}
			else
			{
				buyPrice = 10;
			}
		}

		TalkRes.ai_send_talk_msg(bestNation.nation_recno, nation_recno,
			TalkMsg.TALK_REQUEST_BUY_FOOD, buyQty, buyPrice);
		return 1;
	}

	public bool think_declare_war()
	{
		//---- don't declare a new war if we already has enemies ---//

		foreach (Nation nation in NationArray)
		{
			if (nation.nation_recno == nation_recno)
				continue;

			if (get_relation(nation.nation_recno).status == NATION_HOSTILE)
				return false;
		}

		//------------------------------------------------//

		int minStrength = Int32.MaxValue, bestTargetNation = 0;

		foreach (Nation nation in NationArray)
		{
			if (nation.nation_recno == nation_recno)
				continue;

			NationRelation nationRelation = get_relation(nation.nation_recno);

			if (!nationRelation.has_contact)
				continue;

			if (nationRelation.status == NATION_HOSTILE) // already at war
				continue;

			if (nationRelation.ai_relation_level >= 10)
				continue;

			// if trade_rating is 0, importanceRating will be 100, if trade_rating is 100, importanceRating will be 0
			if (!ai_should_spend(100 - trade_rating(nation.nation_recno)))
				continue;

			//----------------------------------------//

			int targetStrength = nation.military_rank_rating() +
			                     nation.population_rank_rating() / 2 +
			                     nation.economic_rank_rating() / 3;

			if (targetStrength < minStrength)
			{
				minStrength = targetStrength;
				bestTargetNation = nation.nation_recno;
			}
		}

		//------------------------------------------//

		if (bestTargetNation != 0)
		{
			if (should_diplomacy_retry(TalkMsg.TALK_DECLARE_WAR, bestTargetNation))
			{
				TalkRes.ai_send_talk_msg(bestTargetNation, nation_recno, TalkMsg.TALK_DECLARE_WAR);
				return true;
			}
		}

		return false;
	}

	public bool think_give_tech()
	{
		return false;
	}

	public bool think_demand_tech()
	{
		if (Misc.Random(10) > 0) // only 1/10 chance of calling this function
			return false;

		foreach (Nation nation in NationArray.EnumerateRandom())
		{
			if (nation.nation_recno == nation_recno)
				continue;

			if (nation.total_tech_level() == 0)
				continue;

			if (!should_diplomacy_retry(TalkMsg.TALK_DEMAND_TECH, nation.nation_recno))
				continue;

			//--- don't request from hostile or tense nations -----//

			if (get_relation(nation.nation_recno).status < NATION_NEUTRAL)
				continue;

			//---- scan which tech that the nation has but we don't have ----//

			int techId;
			for (techId = 1; techId <= TechRes.tech_info_array.Length; techId++)
			{
				TechInfo techInfo = TechRes[techId];

				if (techInfo.get_nation_tech_level(nation_recno) == 0 && techInfo.get_nation_tech_level(nation.nation_recno) > 0)
				{
					break;
				}
			}

			if (techId > TechRes.tech_info_array.Length)
				continue;

			//-------- send the message now ---------//

			TalkRes.ai_send_talk_msg(nation.nation_recno, nation_recno,
				TalkMsg.TALK_DEMAND_TECH, techId);
			return true;
		}

		return false;
	}

	public bool think_demand_tribute_aid()
	{
		// don't ask for tribute too soon, as in the beginning, the ranking are all the same for all nations
		if (Info.game_date < Info.game_start_date.AddDays(180 + nation_recno * 50))
			return false;

		//--------------------------------------//

		int ourMilitary = military_rank_rating();
		int ourEconomy = economic_rank_rating();

		foreach (Nation nation in NationArray.EnumerateRandom())
		{
			if (nation.nation_recno == nation_recno)
				continue;

			//-- only demand tribute from non-friendly nations --//

			int talkId;
			if (get_relation(nation.nation_recno).status <= NATION_NEUTRAL)
				talkId = TalkMsg.TALK_DEMAND_TRIBUTE;
			else
				talkId = TalkMsg.TALK_DEMAND_AID;

			//-----------------------------------------------//

			double fixedExpense = fixed_expense_365days();
			int curRating;
			int requestRating;

			if (talkId == TalkMsg.TALK_DEMAND_TRIBUTE)
			{
				if (!should_diplomacy_retry(talkId, nation.nation_recno))
					continue;

				curRating = ourMilitary - nation.military_rank_rating();

				if (curRating < -50)
					continue;

				//----------------------------------------------//
				//
				// Some nation will actually consider the ability
				// of the target nation to pay tribute, so nation
				// will not and just ask anyway.
				//
				//----------------------------------------------//

				if (pref_economic_development > 50)
				{
					int addRating = nation.economic_rank_rating() - ourEconomy;

					if (addRating > 0)
						curRating += addRating;
				}

				requestRating = 20 + trade_rating(nation.nation_recno) / 2 + (100 - pref_peacefulness) / 3;

				if (cash < fixedExpense && fixedExpense != 0)
					requestRating -= (int)(requestRating * cash / fixedExpense);
			}
			else
			{
				if (cash > 3000 + 2000 * pref_cash_reserve / 100 || cash > nation.cash)
					continue;

				if (cash >= fixedExpense)
					continue;

				// if the nation is running short of cash, don't wait a while until next retry, retry immediately
				if (cash > fixedExpense * (50 + pref_cash_reserve) / 300 &&
				    !should_diplomacy_retry(talkId, nation.nation_recno))
				{
					continue;
				}

				//----- only ask for aid when the nation is short of cash ----//

				curRating = (ourMilitary - nation.military_rank_rating()) / 2 +
				            (nation.economic_rank_rating() - ourEconomy);

				requestRating = 20 + 50 * (int)(cash / fixedExpense);
			}

			//----- if this is a human player's nation -----//

			if (!nation.is_ai())
			{
				switch (Config.ai_aggressiveness)
				{
					case Config.OPTION_LOW:
						requestRating += 40; // don't go against the player too easily
						break;

					case Config.OPTION_HIGH:
						requestRating -= 20;
						break;

					case Config.OPTION_VERY_HIGH:
						requestRating -= 40;
						break;
				}

				//--- if the nation has plenty of cash, demand from it ----//

				if (nation.cash > cash && Config.ai_aggressiveness >= Config.OPTION_HIGH)
				{
					requestRating -= (int)((nation.cash - cash) / 500.0);
				}
			}

			//--------------------------------------//

			if (curRating > requestRating)
			{
				int tributeAmount;

				if (curRating - requestRating > 120)
					tributeAmount = 4000;

				else if (curRating - requestRating > 80)
					tributeAmount = 3000;

				else if (curRating - requestRating > 40)
					tributeAmount = 2000;

				else if (curRating - requestRating > 20)
					tributeAmount = 1000;

				else
					tributeAmount = 500;

				TalkRes.ai_send_talk_msg(nation.nation_recno, nation_recno, talkId, tributeAmount);

				return true;
			}
		}

		return false;
	}

	public bool think_give_tribute_aid(TalkMsg rejectedMsg)
	{
		//-----------get the talk id. ------------//

		int talkId;
		int talkNationRecno = rejectedMsg.to_nation_recno;
		int rejectedTalkId = rejectedMsg.talk_id;
		int rejectedMsgPara1 = rejectedMsg.talk_para1;
		int rejectedMsgPara2 = rejectedMsg.talk_para2;
		NationRelation nationRelation = get_relation(talkNationRecno);

		if (nationRelation.status >= NATION_FRIENDLY)
			talkId = TalkMsg.TALK_GIVE_AID;
		else
			talkId = TalkMsg.TALK_GIVE_TRIBUTE;

		//-------- don't give tribute too frequently -------//

		if (Info.game_date < nationRelation.last_talk_reject_date_array[talkId - 1].AddDays(365 - pref_allying_tendency))
			return false;

		//---- think if the nation should spend money now ----//

		int[] tributeAmountArray = { 500, 1000 };
		int tributeAmount = tributeAmountArray[Misc.Random(2)];

		if (!ai_should_spend(0, tributeAmount)) // importance rating is 0
			return false;

		//--------------------------------------//

		Nation talkNation = NationArray[talkNationRecno];
		bool rc = false;

		if (rejectedTalkId == TalkMsg.TALK_PROPOSE_TRADE_TREATY)
		{
			rc = ai_trade_with_rating(talkNationRecno) > 100 - pref_trading_tendency / 2;
		}
		else if (rejectedTalkId == TalkMsg.TALK_PROPOSE_FRIENDLY_TREATY ||
		         rejectedTalkId == TalkMsg.TALK_PROPOSE_ALLIANCE_TREATY)
		{
			int curRating = talkNation.trade_rating(talkNationRecno) + ai_trade_with_rating(talkNationRecno) +
				talkNation.overall_rating - overall_rating;

			int acceptRating = 200 - pref_trading_tendency / 4 - pref_allying_tendency / 4;

			rc = curRating >= acceptRating;
		}
		else if (rejectedTalkId == TalkMsg.TALK_REQUEST_CEASE_WAR)
		{
			rc = talkNation.military_rank_rating() >
			     military_rank_rating() + (100 - pref_peacefulness) / 2;
		}

		//--------------------------------------//

		if (rc)
		{
			//------ give tribute --------//

			TalkRes.ai_send_talk_msg(talkNationRecno, nation_recno, talkId, tributeAmount);

			nationRelation.last_talk_reject_date_array[talkId - 1] = Info.game_date;

			//------ request again after giving tribute ----//

			nationRelation.last_talk_reject_date_array[rejectedTalkId - 1] = default; // reset the rejected talk id.

			TalkRes.ai_send_talk_msg(talkNationRecno, nation_recno,
				rejectedTalkId, rejectedMsgPara1, rejectedMsgPara2);
		}

		return rc;
	}

	public bool think_request_surrender()
	{
		if (Info.game_date < Info.game_start_date.AddDays(1000)) // offer 3 years after the game starts
			return false;

		if (Misc.Random(5) != 0) // don't do this too often
			return false;

		//---- only do so when we have enough cash ----//

		if (cash < fixed_expense_365days() + 5000.0 + 100.0 * pref_cash_reserve)
			return false;

		if (profit_365days() < 0 && cash < 20000.0) // don't ask if we are losing money and the cash isn't plenty
			return false;

		//----- calculate the amount this nation can offer ----//

		int offerAmount = (int)(cash * 3.0 / 4.0) - Math.Min(5000, (int)fixed_expense_365days());

		int[] amtArray = { 5000, 7500, 10000, 15000, 20000, 30000, 40000, 50000 };

		int i;
		for (i = 7; i >= 0; i--)
		{
			if (offerAmount >= amtArray[i])
			{
				offerAmount = amtArray[i];
				break;
			}
		}

		if (i < 0)
			return false;

		//---------------------------------------------//

		int ourOverallRankRating = overall_rank_rating();

		foreach (Nation nation in NationArray.EnumerateRandom())
		{
			if (nation.nation_recno == nation_recno)
				continue;

			//--- don't ask for a kingdom that is more powerful to surrender to us ---//

			if (nation.cash > 100) // unless it is running short of cash
			{
				if (nation.overall_rank_rating() > ourOverallRankRating)
					continue;
			}

			//-------------------------------------------//

			if (!should_diplomacy_retry(TalkMsg.TALK_REQUEST_SURRENDER, nation.nation_recno))
				continue;

			//-------------------------------------------//

			// divide by 10 to cope with <short>'s upper limit
			TalkRes.ai_send_talk_msg(nation.nation_recno, nation_recno,
				TalkMsg.TALK_REQUEST_SURRENDER, offerAmount / 10);

			return true;
		}

		return false;
	}

	public int ai_process_talk_msg(ActionNode actionNode)
	{
		if (TalkRes.is_talk_msg_deleted(actionNode.action_para)) // if the talk message has been deleted
			return -1;

		TalkMsg talkMsg = TalkRes.get_talk_msg(actionNode.action_para);

		if (!talkMsg.is_valid_to_reply()) // if it is no longer valid
			return -1;

		//----- call the consider function -------//

		if (talkMsg.reply_type == TalkRes.REPLY_WAITING)
		{
			bool rc = consider_talk_msg(talkMsg);

			if (rc) // if rc is not 1 or 0, than the consider function have processed itself, no need to call reply_talk_msg() here
				TalkRes.reply_talk_msg(actionNode.action_para, TalkRes.REPLY_ACCEPT, InternalConstants.COMMAND_AI);

			else
				TalkRes.reply_talk_msg(actionNode.action_para, TalkRes.REPLY_REJECT, InternalConstants.COMMAND_AI);

			//TODO check it
			// don't reply if rc is neither 0 or 1
		}

		return -1; // always return -1 to remove the action from action_array. 
	}

	public void ai_notify_reply(int talkMsgRecno)
	{
		TalkMsg talkMsg = TalkRes.get_talk_msg(talkMsgRecno);
		int relationChange = 0;
		NationRelation nationRelation = get_relation(talkMsg.to_nation_recno);

		if (talkMsg.reply_type == TalkRes.REPLY_REJECT)
			nationRelation.last_talk_reject_date_array[talkMsg.talk_id - 1] = Info.game_date;
		else
			nationRelation.last_talk_reject_date_array[talkMsg.talk_id - 1] = default;

		switch (talkMsg.talk_id)
		{
			case TalkMsg.TALK_PROPOSE_TRADE_TREATY:
				if (talkMsg.reply_type == TalkRes.REPLY_ACCEPT)
					relationChange = pref_trading_tendency / 10;
				else
					relationChange = -pref_trading_tendency / 10;
				break;

			case TalkMsg.TALK_PROPOSE_FRIENDLY_TREATY:
			case TalkMsg.TALK_PROPOSE_ALLIANCE_TREATY:
				if (talkMsg.reply_type == TalkRes.REPLY_REJECT)
					relationChange = -5;
				break;

			case TalkMsg.TALK_REQUEST_MILITARY_AID:
				if (talkMsg.reply_type == TalkRes.REPLY_ACCEPT)
					relationChange = 0; // the AI never knows whether the player has really aided him in the war
				else
					relationChange = -(20 - pref_military_courage / 10); // -10 to -20
				break;

			case TalkMsg.TALK_REQUEST_TRADE_EMBARGO:
				if (talkMsg.reply_type == TalkRes.REPLY_ACCEPT)
					relationChange = (10 + pref_trading_tendency / 10); // +10 to +20
				else
					relationChange = -(10 + pref_trading_tendency / 20); // -10 to -15
				break;

			case TalkMsg.TALK_REQUEST_CEASE_WAR:
				if (talkMsg.reply_type == TalkRes.REPLY_REJECT)
					relationChange = -5;
				break;

			case TalkMsg.TALK_REQUEST_DECLARE_WAR:
				if (talkMsg.reply_type == TalkRes.REPLY_ACCEPT)
					relationChange = pref_allying_tendency / 10;
				else
					relationChange = -30;
				break;

			case TalkMsg.TALK_REQUEST_BUY_FOOD:
				if (talkMsg.reply_type == TalkRes.REPLY_ACCEPT)
					relationChange = pref_food_reserve / 10;
				else
					relationChange = -pref_food_reserve / 10;
				break;

			case TalkMsg.TALK_DEMAND_TRIBUTE:
			case TalkMsg.TALK_DEMAND_AID:
				if (talkMsg.reply_type == TalkRes.REPLY_ACCEPT)
				{
					//-- the less cash the nation, the more it will appreciate the tribute --//
					relationChange = 100 * talkMsg.talk_para1 / Math.Max(1000, (int)cash);
				}
				else
				{
					// -30 to 40 points depending the peacefulness preference
					relationChange = -(400 - pref_peacefulness) / 10;
				}

				break;

			case TalkMsg.TALK_DEMAND_TECH:
				if (talkMsg.reply_type == TalkRes.REPLY_ACCEPT)
					relationChange = 10 + pref_use_weapon / 5; // +10 to +30
				else
					relationChange = -(10 + pref_use_weapon / 10); // -10 to -20
				break;

			case TalkMsg.TALK_GIVE_TRIBUTE:
			case TalkMsg.TALK_GIVE_AID:
			case TalkMsg.TALK_GIVE_TECH:
				if (talkMsg.reply_type == TalkRes.REPLY_REJECT) // reject your gift
					relationChange = -5;
				break;

			case TalkMsg.TALK_REQUEST_SURRENDER: // no relation change on this request 
				break;
		}

		//------- chance relationship now -------//

		if (relationChange < 0)
			relationChange -= relationChange * (200 - pref_forgiveness) / 200;

		if (relationChange != 0)
			change_ai_relation_level(talkMsg.to_nation_recno, relationChange);

		//---- think about giving tribute to become more friendly with the nation so it will accept our request next time ---//

		if (talkMsg.reply_type == TalkRes.REPLY_REJECT)
		{
			if (think_give_tribute_aid(talkMsg))
				return;

			//--- if our request was rejected, end treaty if the ai_nation_relation is low enough ---//

			if (talkMsg.talk_id !=
			    TalkMsg.TALK_PROPOSE_ALLIANCE_TREATY && // the rejected request is not alliance treaty
			    nationRelation.status >= NATION_FRIENDLY &&
			    nationRelation.ai_relation_level < 40 - pref_allying_tendency / 5) // 20 to 40
			{
				int talkId;

				if (nationRelation.status == NATION_FRIENDLY)
					talkId = TalkMsg.TALK_END_FRIENDLY_TREATY;
				else
					talkId = TalkMsg.TALK_END_ALLIANCE_TREATY;

				TalkRes.ai_send_talk_msg(talkMsg.to_nation_recno, nation_recno, talkId);
			}

			//----- declare war if ai_relation_level==0 -----//

			else if (nationRelation.ai_relation_level == 0)
			{
				//--------- declare war ---------//

				if (Config.ai_aggressiveness >= Config.OPTION_HIGH || pref_peacefulness < 50)
				{
					TalkRes.ai_send_talk_msg(talkMsg.to_nation_recno, nation_recno, TalkMsg.TALK_DECLARE_WAR);

					//------- attack immediately --------//

					if (Config.ai_aggressiveness >= Config.OPTION_VERY_HIGH ||
					    (Config.ai_aggressiveness >= Config.OPTION_HIGH && pref_peacefulness < 50))
					{
						if (largest_town_recno != 0)
						{
							// 1-use forces from all camps to attack the target
							think_capture_new_enemy_town(TownArray[largest_town_recno], true);
						}
					}
				}
			}
		}
	}

	public bool should_diplomacy_retry(int talkId, int nationRecno)
	{
		if (!TalkRes.can_send_msg(nationRecno, nation_recno, talkId))
			return false;

		int retryInterval;

		//--- shorter retry interval for demand talk message ----//

		if (talkId == TalkMsg.TALK_DEMAND_TRIBUTE || talkId == TalkMsg.TALK_DEMAND_AID ||
		    talkId == TalkMsg.TALK_DEMAND_TECH)
		{
			retryInterval = 60 + 60 * (100 - pref_diplomacy_retry) / 100; // 2-4 months
		}
		else
		{
			retryInterval = 90 + 270 * (100 - pref_diplomacy_retry) / 100; // 3 months to 12 months before next try
		}

		return Info.game_date > get_relation(nationRecno).last_talk_reject_date_array[talkId - 1].AddDays(retryInterval);
	}

	public void ai_end_treaty(int nationRecno)
	{
		NationRelation nationRelation = get_relation(nationRecno);

		if (nationRelation.status == NATION_FRIENDLY)
		{
			TalkRes.ai_send_talk_msg(nationRecno, nation_recno,
				TalkMsg.TALK_END_FRIENDLY_TREATY, 0, 0, true);
		}
		else if (nationRelation.status == NATION_ALLIANCE)
		{
			TalkRes.ai_send_talk_msg(nationRecno, nation_recno,
				TalkMsg.TALK_END_ALLIANCE_TREATY, 0, 0, true);
		}
	}

	private bool has_sent_same_msg(TalkMsg talkMsg)
	{
		TalkMsg tempMsg = new TalkMsg();

		tempMsg.talk_id = talkMsg.talk_id;
		tempMsg.talk_para1 = talkMsg.talk_para1;
		tempMsg.talk_para2 = talkMsg.talk_para2;
		tempMsg.date = talkMsg.date;
		tempMsg.from_nation_recno = talkMsg.to_nation_recno;
		tempMsg.to_nation_recno = talkMsg.from_nation_recno;
		tempMsg.reply_type = talkMsg.reply_type;
		tempMsg.reply_date = talkMsg.reply_date;

		return TalkRes.is_talk_msg_exist(tempMsg, true) != 0; // 1-check talk_para1 & talk_para2
	}

	public bool consider_talk_msg(TalkMsg talkMsg)
	{
		//--------------------------------------------//
		// Whether the nation has already sent out a
		// message that is the same as the one it received.
		// If so, accept the message right now.
		//--------------------------------------------//

		switch (talkMsg.talk_id)
		{
			case TalkMsg.TALK_PROPOSE_TRADE_TREATY:
			case TalkMsg.TALK_PROPOSE_FRIENDLY_TREATY:
			case TalkMsg.TALK_PROPOSE_ALLIANCE_TREATY:
			case TalkMsg.TALK_REQUEST_TRADE_EMBARGO:
			case TalkMsg.TALK_REQUEST_CEASE_WAR:
			case TalkMsg.TALK_REQUEST_DECLARE_WAR:
				if (has_sent_same_msg(talkMsg))
					return true;
				break;
		}

		//-------------------------------//

		switch (talkMsg.talk_id)
		{
			case TalkMsg.TALK_PROPOSE_TRADE_TREATY:
				return consider_trade_treaty(talkMsg.from_nation_recno) > 0;

			case TalkMsg.TALK_PROPOSE_FRIENDLY_TREATY:
				return consider_friendly_treaty(talkMsg.from_nation_recno) > 0;

			case TalkMsg.TALK_PROPOSE_ALLIANCE_TREATY:
				return consider_alliance_treaty(talkMsg.from_nation_recno) > 0;

			case TalkMsg.TALK_REQUEST_MILITARY_AID:
				return consider_military_aid(talkMsg);

			case TalkMsg.TALK_REQUEST_TRADE_EMBARGO:
				return consider_trade_embargo(talkMsg);

			case TalkMsg.TALK_REQUEST_CEASE_WAR:
				return consider_cease_war(talkMsg.from_nation_recno) > 0;

			case TalkMsg.TALK_REQUEST_DECLARE_WAR:
				return consider_declare_war(talkMsg);

			case TalkMsg.TALK_REQUEST_BUY_FOOD:
				return consider_sell_food(talkMsg);

			case TalkMsg.TALK_GIVE_TRIBUTE:
				return consider_take_tribute(talkMsg);

			case TalkMsg.TALK_DEMAND_TRIBUTE:
				return consider_give_tribute(talkMsg);

			case TalkMsg.TALK_GIVE_AID:
				return consider_take_aid(talkMsg);

			case TalkMsg.TALK_DEMAND_AID:
				return consider_give_aid(talkMsg);

			case TalkMsg.TALK_GIVE_TECH:
				return consider_take_tech(talkMsg);

			case TalkMsg.TALK_DEMAND_TECH:
				return consider_give_tech(talkMsg);

			case TalkMsg.TALK_REQUEST_SURRENDER:
				return consider_accept_surrender_request(talkMsg);

			default:
				return false;
		}
	}

	public void notify_talk_msg(TalkMsg talkMsg)
	{
		int relationChange = 0;
		NationRelation nationRelation = get_relation(talkMsg.from_nation_recno);

		switch (talkMsg.talk_id)
		{
			case TalkMsg.TALK_END_TRADE_TREATY: // it's a notification message only, no accept or reject
				relationChange = -5;
				nationRelation.last_talk_reject_date_array[TalkMsg.TALK_PROPOSE_TRADE_TREATY - 1] = Info.game_date;
				break;

			case TalkMsg.TALK_END_FRIENDLY_TREATY: // it's a notification message only, no accept or reject
			case TalkMsg.TALK_END_ALLIANCE_TREATY:
				relationChange = -5;
				nationRelation.last_talk_reject_date_array[TalkMsg.TALK_PROPOSE_FRIENDLY_TREATY - 1] = Info.game_date;
				nationRelation.last_talk_reject_date_array[TalkMsg.TALK_PROPOSE_ALLIANCE_TREATY - 1] = Info.game_date;
				break;

			case TalkMsg.TALK_DECLARE_WAR: // it already drops to zero when the status is set to hostile
				break;

			case TalkMsg.TALK_GIVE_TRIBUTE:
			case TalkMsg.TALK_GIVE_AID:

				//--------------------------------------------------------------//
				// The less cash the nation, the more it will appreciate the
				// tribute.
				//
				// $1000 for 100 ai relation increase if the nation's cash is 1000.
				//--------------------------------------------------------------//

				relationChange = 100 * talkMsg.talk_para1 / Math.Max(1000, (int)cash);
				break;

			case TalkMsg.TALK_GIVE_TECH:

				//--------------------------------------------------------------//
				// The lower tech the nation has, the more it will appreciate the
				// tech giveaway.
				//
				// Giving a level 2 weapon which the nation is unknown of
				// increase the ai relation by 60 if its pref_use_weapon is 100.
				// (by 30 if its pref_use_weapon is 0).
				//--------------------------------------------------------------//
			{
				int ownLevel = TechRes[talkMsg.talk_para1].get_nation_tech_level(nation_recno);

				if (talkMsg.talk_para2 > ownLevel)
					relationChange = 30 * (talkMsg.talk_para2 - ownLevel) * (100 + pref_use_weapon) / 200;
				break;
			}

			case TalkMsg.TALK_SURRENDER:
				break;
		}

		//------- chance relationship now -------//

		if (relationChange < 0)
			relationChange -= relationChange * pref_forgiveness / 100;

		if (relationChange != 0)
			change_ai_relation_level(talkMsg.from_nation_recno, relationChange);
	}

	public int consider_trade_treaty(int withNationRecno)
	{
		NationRelation nationRelation = get_relation(withNationRecno);

		//---- don't accept new trade treaty soon when the trade treaty was terminated not too long ago ----//

		if (Info.game_date < nationRelation.last_talk_reject_date_array[TalkMsg.TALK_END_TRADE_TREATY - 1].AddDays(365 - pref_forgiveness))
		{
			return -1;
		}

		//-- if we look forward to have a trade treaty with this nation ourselves --//

		if (nationRelation.ai_demand_trade_treaty != 0)
			return 1;

		return ai_trade_with_rating(withNationRecno) > 0 ? 1 : -1;
	}

	public int consider_friendly_treaty(int withNationRecno)
	{
		NationRelation nationRelation = get_relation(withNationRecno);

		if (nationRelation.status >= NATION_FRIENDLY) // already has a friendly relationship
			return -1; // -1 means don't reply

		if (nationRelation.ai_relation_level < 20)
			return -1;

		//------- some consideration first -------//

		if (!should_consider_friendly(withNationRecno))
			return -1;

		//------ total import and export amounts --------//

		int curRating = consider_alliance_rating(withNationRecno);

		int acceptRating = 60 - pref_allying_tendency / 8 - pref_peacefulness / 4; // range of acceptRating: 23 to 60

		return curRating - acceptRating;
	}

	public int consider_alliance_treaty(int withNationRecno)
	{
		NationRelation nationRelation = get_relation(withNationRecno);

		if (nationRelation.status >= NATION_ALLIANCE) // already has a friendly relationship
			return -1; // -1 means don't reply

		if (nationRelation.ai_relation_level < 40)
			return -1;

		//------- some consideration first -------//

		if (!should_consider_friendly(withNationRecno))
			return -1;

		//------ total import and export amounts --------//

		int curRating = consider_alliance_rating(withNationRecno);

		int acceptRating = 80 - pref_allying_tendency / 4 - pref_peacefulness / 8; // range of acceptRating: 43 to 80

		return curRating - acceptRating;
	}

	public bool consider_military_aid(TalkMsg talkMsg)
	{
		Nation fromNation = NationArray[talkMsg.from_nation_recno];
		NationRelation fromRelation = get_relation(talkMsg.from_nation_recno);

		//----- don't aid too frequently ------//

		if (Info.game_date < fromRelation.last_military_aid_date.AddDays(200 - pref_allying_tendency))
			return false;

		//------- only when the AI relation >= 60 --------//

		if (fromRelation.ai_relation_level < 60)
			return false;

		//--- if the requesting nation is not at war now ----//

		if (!fromNation.is_at_war())
			return false;

		//---- can't aid if we are at war ourselves -----//

		if (is_at_war())
			return false;

		//--- if the nation is having a financial difficulty, it won't agree ---//

		if (cash < 2000 + 3000 * pref_cash_reserve / 100)
			return false;

		//----- can't aid if we are too weak ourselves ---//

		if (ai_general_array.Count * 10 + total_human_count < 100 - pref_military_courage / 2)
			return false;

		//----- see what units are attacking the nation -----//

		if (UnitArray.IsDeleted(fromNation.last_attacker_unit_recno))
			return false;

		Unit unit = UnitArray[fromNation.last_attacker_unit_recno];

		if (unit.nation_recno == nation_recno) // if it's our own units
			return false;

		if (unit.nation_recno == 0)
			return false;

		if (!unit.is_visible())
			return false;

		//------ only attack if it's a common enemy to us and our ally -----//

		if (get_relation(unit.nation_recno).status != NATION_HOSTILE)
			return false;

		//------- calculate the combat level of the target units there ------//

		int targetCombatLevel =
			ai_evaluate_target_combat_level(unit.next_x_loc(), unit.next_y_loc(), unit.nation_recno);

		if (ai_attack_target(unit.next_x_loc(), unit.next_y_loc(), targetCombatLevel, true))
		{
			fromRelation.last_military_aid_date = Info.game_date;
			return true;
		}

		return false;
	}

	public bool consider_trade_embargo(TalkMsg talkMsg)
	{
		int fromRelationRating = ai_overall_relation_rating(talkMsg.from_nation_recno);
		int againstRelationRating = ai_overall_relation_rating(talkMsg.talk_para1);

		NationRelation fromRelation = get_relation(talkMsg.from_nation_recno);
		NationRelation againstRelation = get_relation(talkMsg.talk_para1);

		//--- if we don't have a good enough relation with the requesting nation, turn down the request ---//

		if (fromRelation.good_relation_duration_rating < 5)
			return false;

		//--- if we are more friendly with the against nation than the requesting nation, turn down the request ---//

		if (againstRelation.good_relation_duration_rating > fromRelation.good_relation_duration_rating)
			return false;

		//--- if we have a large trade with the against nation or have a larger trade with the against nation than the requesting nation ---//

		int fromTrade = trade_rating(talkMsg.from_nation_recno);
		int againstTrade = trade_rating(talkMsg.talk_para1);

		if (againstTrade > 40 || (againstTrade > 10 && againstTrade - fromTrade > 15))
			return false;

		//--- if the nation is having a financial difficulty, it won't agree ---//

		if (cash < 2000 + 3000 * pref_cash_reserve / 100)
			return false;

		//--------------------------------------------//

		int acceptRating = 75;

		//--- it won't declare war with a friendly or allied nation easily ---//

		// no need to handle NATION_ALLIANCE separately as ai_overall_relation_relation() has already taken it into account
		if (againstRelation.status >= NATION_FRIENDLY)
			acceptRating += 100;

		return fromRelationRating - againstRelationRating > acceptRating;
	}

	public int consider_cease_war(int withNationRecno)
	{
		NationRelation nationRelation = get_relation(withNationRecno);

		if (nationRelation.status != NATION_HOSTILE)
			return -1; // -1 means don't reply

		//---- if we are attacking the nation, don't cease fire ----//

		if (ai_attack_target_nation_recno == withNationRecno)
			return -1;

		//---- if we are planning to capture the enemy's town ---//

		if (ai_capture_enemy_town_recno != 0 && !TownArray.IsDeleted(ai_capture_enemy_town_recno) &&
		    TownArray[ai_capture_enemy_town_recno].NationId == withNationRecno)
		{
			return -1;
		}

		//--- don't cease fire too soon after a war is declared ---//

		// more peaceful nation may cease fire sooner (but the minimum is 60 days).
		if (Info.game_date < nationRelation.last_change_status_date.AddDays(60 + (100 - pref_peacefulness)))
			return -1;

		Nation withNation = NationArray[withNationRecno];

		//------ if this is our biggest enemy do not cease fire -----//

		if (Config.ai_aggressiveness == Config.OPTION_VERY_HIGH &&
		    NationArray.max_overall_nation_recno == withNationRecno)
			return -1;

		//------ if we're run short of money for war -----//

		// if we shouldn't spend any more on war, then return 1
		if (!ai_should_spend_war(withNation.military_rank_rating(), true))
			return 1;

		//------------------------------------------------//

		int curRating = consider_alliance_rating(withNationRecno);

		//------------------------------------//
		//
		// Tend to be easier to accept cease-fire if this nation's
		// military strength is weak.
		//
		// If the nation's peacefulness concern is high, it will
		// also be more likely to accept cease-fire.
		//
		//-------------------------------------//

		//--- if the enemy is more power than us, tend more to request cease-fire ---//

		curRating += total_enemy_military() - military_rank_rating();

		// when we have excessive supply, we may want to cease-fire with our enemy
		curRating += ai_trade_with_rating(withNationRecno) * (100 + pref_trading_tendency) / 300;

		// if our military ranking is high, we may like to continue the war, otherwise the nation should try to cease-fire
		curRating -= (military_rank_rating() - 50) / 2;

		// the number of times this nation has started a war with us, the higher the number, the more unlikely we will accept cease-fire
		curRating -= nationRelation.started_war_on_us_count * 10;

		int acceptRating = pref_peacefulness / 4;

		return curRating - acceptRating;
	}

	public bool consider_declare_war(TalkMsg talkMsg)
	{
		//--- if it even won't consider trade embargo, there is no reason that it will consider declaring war ---//

		if (!consider_trade_embargo(talkMsg))
			return false;

		//---------------------------------------//

		int fromRelationRating = ai_overall_relation_rating(talkMsg.from_nation_recno);
		int againstRelationRating = ai_overall_relation_rating(talkMsg.talk_para1);

		Nation againstNation = NationArray[talkMsg.talk_para1];

		NationRelation fromRelation = get_relation(talkMsg.from_nation_recno);
		NationRelation againstRelation = get_relation(talkMsg.talk_para1);

		//--- if we don't have a good enough relation with the requesting nation, turn down the request ---//

		if (fromRelation.good_relation_duration_rating < 10)
			return false;

		//--- if we are more friendly with the against nation than the requesting nation, turn down the request ---//

		if (againstRelation.good_relation_duration_rating >
		    fromRelation.good_relation_duration_rating)
		{
			return false;
		}

		//--- if the nation is having a financial difficulty, it won't agree ---//

		if (cash < 2000 + 3000 * pref_cash_reserve / 100)
			return false;

		//--------------------------------------------//

		int acceptRating = 100 + againstNation.total_enemy_military() - military_rank_rating();

		//--- it won't declare war with a friendly or allied nation easily ---//

		// no need to handle NATION_ALLIANCE separately as ai_overall_relation_relation() has already taken it into account
		if (againstRelation.status >= NATION_FRIENDLY)
			acceptRating += 100;

		return fromRelationRating - againstRelationRating > acceptRating;
	}

	public bool consider_sell_food(TalkMsg talkMsg)
	{
		int relationStatus = get_relation_status(talkMsg.from_nation_recno);

		if (relationStatus == NATION_HOSTILE)
			return false;

		//--- if after selling the food, the remaining is not enough for its own consumption for ? years ---//

		double newFood = food - talkMsg.talk_para1;
		double yearConsumption = yearly_food_consumption();
		int offeredAmount = talkMsg.talk_para2;
		int relationLevel = get_relation(talkMsg.from_nation_recno).ai_relation_level;

		if (newFood < 1000 + 1000 * pref_food_reserve / 100)
			return false;

		if (relationLevel >= 50)
			offeredAmount += 5; // increase the chance of selling food

		else if (relationLevel < 30) // decrease the chance of selling food
			offeredAmount -= 5;

		//---- if we run short of cash, we tend to accept the offer ---//

		double fixedExpense = fixed_expense_365days();

		if (cash < fixedExpense)
			offeredAmount += (int)(20 * (fixedExpense - cash) / fixedExpense);

		//---------------------------------//

		double reserveYears = (100 + pref_food_reserve) / 100.0; // 1 to 2 years

		if (yearly_food_change() > 0 && newFood > yearConsumption * reserveYears)
		{
			if (offeredAmount >= 10) // offered >= $10
			{
				return true;
			}
			else // < $10, only if we have plenty of reserve
			{
				if (newFood > yearConsumption * reserveYears * 2)
					return true;
			}
		}
		else
		{
			if (offeredAmount >= 20)
			{
				if (yearly_food_change() > 0 && newFood > yearConsumption * reserveYears / 2)
				{
					return true;
				}
			}

			if (offeredAmount >= 30)
			{
				return yearly_food_change() > 0 || newFood > yearConsumption * reserveYears;
			}
		}

		return false;
	}

	public bool consider_take_tribute(TalkMsg talkMsg)
	{
		int cashSignificance = 100 * talkMsg.talk_para1 / Math.Max(1000, (int)cash);

		//--- It does not necessarily want the tribute ---//

		int aiRelationLevel = get_relation(talkMsg.from_nation_recno).ai_relation_level;

		if (true_profit_365days() > 0 && cashSignificance < (100 - aiRelationLevel) / 5)
		{
			return false;
		}

		//----------- take the tribute ------------//

		int relationChange = cashSignificance * (100 + pref_cash_reserve) / 200;

		change_ai_relation_level(talkMsg.from_nation_recno, relationChange);

		return true;
	}

	public bool consider_give_tribute(TalkMsg talkMsg)
	{
		//-------- don't give tribute too frequently -------//

		NationRelation nationRelation = get_relation(talkMsg.from_nation_recno);

		if (Info.game_date < nationRelation.last_talk_reject_date_array[TalkMsg.TALK_GIVE_TRIBUTE - 1].AddDays(365 - pref_allying_tendency))
		{
			return false;
		}

		//---------------------------------------------//

		Nation fromNation = NationArray[talkMsg.from_nation_recno];

		if (true_profit_365days() < 0) // don't give tribute if we are losing money
			return false;

		int reserveYears = 1 + 3 * pref_cash_reserve / 100; // 1 to 4 years

		if (cash - talkMsg.talk_para1 < fixed_expense_365days() * reserveYears)
			return false;

		int militaryDiff = fromNation.military_rank_rating() - military_rank_rating();

		if (militaryDiff > 10 + pref_military_courage / 2)
		{
			nationRelation.last_talk_reject_date_array[TalkMsg.TALK_GIVE_TRIBUTE - 1] = Info.game_date;
			return true;
		}

		return false;
	}

	public bool consider_take_aid(TalkMsg talkMsg)
	{
		int cashSignificance = 100 * talkMsg.talk_para1 / Math.Max(1000, (int)cash);

		//--- It does not necessarily want the tribute ---//

		int aiRelationLevel = get_relation(talkMsg.from_nation_recno).ai_relation_level;

		if (true_profit_365days() > 0 && cashSignificance < (100 - aiRelationLevel) / 5)
		{
			return false;
		}

		//----------- take the aid ------------//

		int relationChange = cashSignificance * (100 + pref_cash_reserve) / 200;

		change_ai_relation_level(talkMsg.from_nation_recno, relationChange);

		return true;
	}

	public bool consider_give_aid(TalkMsg talkMsg)
	{
		//-------- don't give tribute too frequently -------//

		NationRelation nationRelation = get_relation(talkMsg.from_nation_recno);

		if (Info.game_date < nationRelation.last_talk_reject_date_array[TalkMsg.TALK_GIVE_AID - 1].AddDays(365 - pref_allying_tendency))
		{
			return false;
		}

		//--------------------------------------------------//

		int importanceRating = (int)nationRelation.good_relation_duration_rating;

		if (nationRelation.status >= NATION_FRIENDLY && ai_should_spend(importanceRating, talkMsg.talk_para1))
		{
			// we have allied with this nation for quite some while
			if (Info.game_date > nationRelation.last_change_status_date.AddDays(720 - pref_allying_tendency))
			{
				nationRelation.last_talk_reject_date_array[TalkMsg.TALK_GIVE_AID - 1] = Info.game_date;
				return true;
			}
		}

		return false;
	}

	public bool consider_take_tech(TalkMsg talkMsg)
	{
		int ourTechLevel = TechRes[talkMsg.talk_para1].get_nation_tech_level(nation_recno);

		if (ourTechLevel >= talkMsg.talk_para2)
			return false;

		int relationChange = (talkMsg.talk_para2 - ourTechLevel) * (15 + pref_use_weapon / 10);

		change_ai_relation_level(talkMsg.from_nation_recno, relationChange);

		return true;
	}

	public bool consider_give_tech(TalkMsg talkMsg)
	{
		//-------- don't give tribute too frequently -------//

		NationRelation nationRelation = get_relation(talkMsg.from_nation_recno);

		if (Info.game_date < nationRelation.last_talk_reject_date_array[TalkMsg.TALK_GIVE_TECH - 1].AddDays(365 - pref_allying_tendency))
		{
			return false;
		}

		//----------------------------------------------------//

		int importanceRating = (int)nationRelation.good_relation_duration_rating;

		if (nationRelation.status == NATION_ALLIANCE && importanceRating + pref_allying_tendency / 10 > 30)
		{
			nationRelation.last_talk_reject_date_array[TalkMsg.TALK_GIVE_TECH - 1] = Info.game_date;
			return true;
		}

		return false;
	}

	public bool consider_accept_surrender_request(TalkMsg talkMsg)
	{
		Nation nation = NationArray[talkMsg.from_nation_recno];
		// *10 to restore its original value which has been divided by 10 to cope with <short> upper limit
		int offeredAmt = talkMsg.talk_para1 * 10;

		//---- don't surrender to the player if the player is already the most powerful nation ---//

		if (!nation.is_ai() && Config.ai_aggressiveness >= Config.OPTION_HIGH)
		{
			if (NationArray.max_overall_nation_recno == nation.nation_recno)
				return false;
		}

		//--- if we are running out of cash, ignore all normal thinking ---//

		if (cash >= 100.0 || profit_365days() > 0.0)
		{
			//----- never surrender to a weaker nation ------//

			if (nation.overall_rank_rating() < overall_rank_rating())
				return false;

			//------ don't surrender if we are still strong -----//

			if (overall_rank_rating() > 30 + pref_peacefulness / 4) // 30 to 55
				return false;

			//---- don't surrender if our cash is more than the amount they offered ----//

			if (offeredAmt < cash * (75 + pref_cash_reserve / 2) / 100) // 75% to 125%
				return false;

			//-- if there are only two nations left, don't surrender if we still have some power --//

			if (NationArray.nation_count == 2)
			{
				if (overall_rank_rating() > 20 - 10 * pref_military_courage / 100)
					return false;
			}
		}

		//-------------------------------------//

		int surrenderToRating = ai_surrender_to_rating(talkMsg.from_nation_recno);

		surrenderToRating += 100 * offeredAmt / 13000;

		int acceptRating = overall_rank_rating() * 13 + 100;

		//------ AI aggressiveness effects -------//

		switch (Config.ai_aggressiveness)
		{
			case Config.OPTION_HIGH:
				if (nation.is_ai()) // tend to accept AI kingdom offer easier
					acceptRating -= 75;
				else
					acceptRating += 75;
				break;

			case Config.OPTION_VERY_HIGH:
				if (nation.is_ai()) // tend to accept AI kingdom offer easier
					acceptRating -= 150;
				else
					acceptRating += 150;
				break;
		}

		return surrenderToRating > acceptRating;
	}

	public int consider_alliance_rating(int nationRecno)
	{
		Nation nation = NationArray[nationRecno];

		//---- the current relation affect the alliance tendency ---//

		NationRelation nationRelation = get_relation(nationRecno);

		int allianceRating = nationRelation.ai_relation_level - 20;

		//--- if the nation has a bad record of starting wars with us before, decrease the rating ---//

		allianceRating -= nationRelation.started_war_on_us_count * 20;

		//------ add the trade rating -------//

		// existing trade amount and possible trade
		int tradeRating = trade_rating(nationRecno) + ai_trade_with_rating(nationRecno) / 2;

		allianceRating += tradeRating;

		//---- if the nation's power is larger than us, it's a plus ----//

		// if the nation's power is larger than ours, it's good to form treaty with them
		int powerRating = nation.military_rank_rating() - military_rank_rating();

		if (powerRating > 0)
			allianceRating += powerRating;

		return allianceRating;
	}

	public bool should_consider_friendly(int withNationRecno)
	{
		//------- if this is a larger nation -------//

		if (overall_rank_rating() > 10)
		{
			//--- big nations don't ally with their biggest opponents ---//

			int maxOverallRating = 0;
			int biggestOpponentNationRecno = 0;

			foreach (Nation nation in NationArray)
			{
				if (nation.nation_recno == nation_recno)
					continue;

				int overallRating = nation.overall_rating;

				if (overallRating > maxOverallRating)
				{
					maxOverallRating = overallRating;
					biggestOpponentNationRecno = nation.nation_recno;
				}
			}

			if (biggestOpponentNationRecno == withNationRecno)
				return false;
		}

		//--- don't ally with nations with too low reputation ---//

		Nation withNation = NationArray[withNationRecno];
		return withNation.reputation >= Math.Min(20, reputation) - 20;
	}

	public int ai_overall_relation_rating(int withNationRecno)
	{
		Nation nation = NationArray[withNationRecno];
		NationRelation nationRelation = get_relation(withNationRecno);

		int overallRating = nationRelation.ai_relation_level +
		                    (int)nationRelation.good_relation_duration_rating +
		                    (int)nation.reputation +
		                    nation.military_rank_rating() +
		                    trade_rating(withNationRecno) +
		                    ai_trade_with_rating(withNationRecno) / 2 +
		                    nation.total_alliance_military();

		return overallRating;
	}

	private int get_target_nation_recno(int targetXLoc, int targetYLoc)
	{
		Location location = World.GetLoc(targetXLoc, targetYLoc);

		if (location.IsFirm())
			return FirmArray[location.FirmId()].nation_recno;

		if (location.IsTown())
			return TownArray[location.TownId()].NationId;

		if (location.HasUnit(UnitConstants.UNIT_LAND))
			return UnitArray[location.UnitId(UnitConstants.UNIT_LAND)].nation_recno;

		return 0;
	}
}

public class ActionNode
{
	//public const int MAX_ACTION_GROUP_UNIT = 9;
	public const int ACTION_DYNAMIC = 0;
	public const int ACTION_FIXED = 1;

	public int action_mode; // eg build firm, attack, etc
	public int action_type; // action type. For 7kaa, this is always ACTION_FIXED.
	public int action_para; // parameter for the action. e.g. firmId for AI_BUILD_FIRM
	public int action_para2; // parameter for the action. e.g. firm race id. for building FirmBase
	public int action_id; // an unique id. for identifying this node

	public DateTime add_date; // when this action is added
	public int unit_recno; // unit associated with this action.

	public int action_x_loc; // can be firm loc, or target loc, etc
	public int action_y_loc;
	public int ref_x_loc; // reference x loc, eg the raw material location
	public int ref_y_loc;

	// number of term to wait before this action is removed from the array if it cannot be processed
	public int retry_count;

	public int instance_count; // no. of times this action needs to be carried out

	// for group unit actions, the no. of units in the array is stored in instance_count
	//public int[] group_unit_array = new int[MAX_ACTION_GROUP_UNIT];
	public List<int> group_unit_array = new List<int>();

	public int processing_instance_count;
	public int processed_instance_count;

	// continue processing this action after this date, this is used when training a unit for construction}
	public DateTime next_retry_date;
}

public class AIRegion
{
	public int region_id;
	public int town_count;
	public int base_town_count;
}

public class AttackCamp
{
    public int firm_recno;
    public int combat_level;
    public int distance;
    public DateTime patrol_date;
}

public class CaptureTown
{
    public int town_recno;
    public int min_resistance;
    public int capture_unit_recno; // if > 0, the unit that was found to best be able to capture the town.
}
