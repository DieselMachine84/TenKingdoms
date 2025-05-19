using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class DefenseUnit
{
	public const int INSIDE_CAMP = 0;
	public const int OUTSIDE_CAMP = 1;

	public int unit_recno;
	public int status; // inside / outside the camp
}

public class FirmCamp : Firm
{
	public List<DefenseUnit> defense_array = new List<DefenseUnit>();
	public bool			employ_new_worker;
	public int			defend_target_recno; // used in defend mode, store recno of the latest target attacking this firm
	public bool			defense_flag;

	//---------- AI related vars ----------//

	public List<int> patrol_unit_array = new List<int>(MAX_WORKER+1);
	public List<int> coming_unit_array = new List<int>(MAX_WORKER+1);

	public int			ai_capture_town_recno;		// >0 if the troop is in the process of attacking the independent village for capturing it.
	public bool			ai_recruiting_soldier;

	public bool			is_attack_camp;

	public FirmCamp()
	{
		firm_skill_id = Skill.SKILL_LEADING;

		employ_new_worker = true;
		defend_target_recno = 0;
		defense_flag = true;

		is_attack_camp = false;
	}

	protected override void init_derived()
	{
		ai_capture_town_recno = 0;
		ai_recruiting_soldier = true;
	}

	public override void deinit()
	{
		int firmRecno = firm_recno; // save the firm_recno first for reset_unit_home_camp()

		base.deinit();

		//-------------------------------------------//

		int saveOverseerRecno = overseer_recno;

		overseer_recno = 0; // set overseer_recno to 0 when calling update_influence(), so this base is disregarded.

		update_influence();

		overseer_recno = saveOverseerRecno;

		clear_defense_mode(firmRecno);

		//---- reset all units whose home_camp_firm_recno is this firm ----//

		reset_unit_home_camp(firmRecno); // this must be called at last as Firm::deinit() will create new units.

		//--- if this camp is in the Nation::attack_camp_array[], remove it now ---//

		reset_attack_camp(firmRecno);
	}

	public override void next_day()
	{
		//----- call next_day() of the base class -----//

		base.next_day();

		//----- update the patrol_unit_array -----//

		validate_patrol_unit();

		//--------------------------------------//

		if (Info.TotalDays % 15 == firm_recno % 15) // once a week
		{
			train_unit();
			recover_hit_point();
		}

		//--- if there are weapons in the firm, pay for their expenses ---//

		pay_weapon_expense();
	}

	public int total_combat_level()
	{
		int totalCombatLevel = 0;

		foreach (Worker worker in workers)
		{
			// use it points instead of combat level because hit_points represent both combat level and hit points left
			totalCombatLevel += worker.hit_points;

			//---- the combat level of weapons are higher ------//

			UnitInfo unitInfo = UnitRes[worker.unit_id];

			// extra_para keeps the weapon version
			if (unitInfo.unit_class == UnitConstants.UNIT_CLASS_WEAPON)
				totalCombatLevel += (unitInfo.weapon_power + worker.extra_para - 1) * 30;
		}

		if (overseer_recno != 0)
		{
			Unit unit = UnitArray[overseer_recno];

			//--- the commander's leadership effects over the soldiers ---//

			// divided by 150 instead of 100 because while the attacking ability of the unit is affected by the general,
			// the hit points isn't, so we shouldn't do a direct multiplication.
			totalCombatLevel += totalCombatLevel * unit.Skill.SkillLevel / 150;

			//------ the leader's own hit points ------//

			totalCombatLevel += (int)unit.HitPoints;
		}

		return totalCombatLevel;
	}

	public int average_combat_level()
	{
		int personCount = workers.Count + (overseer_recno > 0 ? 1 : 0);

		return personCount > 0 ? total_combat_level() / personCount : 0;
	}

	public int ai_combat_level_needed()
	{
		int combatNeeded = 0;
		Nation nation = NationArray[nation_recno];

		//------- scan for linked towns ---------//

		for (int i = 0; i < linked_town_array.Count; i++)
		{
			Town town = TownArray[linked_town_array[i]];

			if (town.NationId == 0)
				combatNeeded += 1000;

			//------- this is its own town -------//

			if (town.NationId == nation_recno)
			{
				if (town.ShouldAIMigrate()) // no need for this if this town is going to migrate
					continue;

				combatNeeded += town.Population * 10; // 30 people need 300 combat levels

				if (town.IsBaseTown)
					combatNeeded += town.Population * 10; // double the combat level need for base town
			}
		}

		//--- if the overseer is the king, increase its combat level needed ---//

		if (overseer_recno != 0 && UnitArray[overseer_recno].Rank == Unit.RANK_KING)
			combatNeeded = Math.Max(400, combatNeeded);

		//---------------------------------------//

		return combatNeeded;
	}

	public bool ai_is_capturing_independent_village()
	{
		for (int i = 0; i < linked_town_array.Count; i++)
		{
			Town town = TownArray[linked_town_array[i]];
			if (town.NationId == 0)
			{
				return true;
			}
		}

		return false;
	}

	public override bool ai_has_excess_worker()
	{
		if (linked_town_array.Count == 0)
			return true;

		if (ai_is_capturing_independent_village()) // no if the camp is trying to capture an independent town
			return false;

		if (is_attack_camp) // no if the camp is trying to capture an independent town
			return false;

		if (should_close_flag)
			return true;

		return false;
	}

	public override bool is_worker_full()
	{
		return workers.Count + patrol_unit_array.Count + coming_unit_array.Count >= MAX_WORKER;
	}

	public void patrol()
	{
		if (nation_recno == NationArray.player_recno)
			Power.reset_selection();

		//------------------------------------------------------------//
		// If the commander in this camp has units under his lead
		// outside and he is now going to lead a new team, then
		// the old team members should be reset.
		//------------------------------------------------------------//

		if (overseer_recno != 0)
		{
			TeamInfo teamInfo = UnitArray[overseer_recno].TeamInfo;

			if (workers.Count > 0 && teamInfo.Members.Count > 0)
			{
				for (int i = 0; i < teamInfo.Members.Count; i++)
				{
					int unitRecno = teamInfo.Members[i];

					if (UnitArray.IsDeleted(unitRecno))
						continue;

					UnitArray[unitRecno].LeaderId = 0;
				}
			}
		}

		//------------------------------------------------------------//
		// mobilize workers first, then the overseer.
		//------------------------------------------------------------//

		int overseerRecno = overseer_recno;

		if (patrol_all_soldier() && overseer_recno != 0)
		{
			Unit unit = UnitArray[overseer_recno];
			// set it to the same team as the soldiers which are defined in mobilize_all_worker()
			unit.TeamId = UnitArray.cur_team_id - 1;

			if (nation_recno == NationArray.player_recno)
			{
				unit.SelectedFlag = true;
				UnitArray.selected_recno = overseer_recno;
				UnitArray.selected_count++;
			}
		}

		assign_overseer(0);

		//---------------------------------------------------//

		if (overseerRecno != 0 && overseer_recno == 0) // has overseer and the overseer is mobilized
		{
			Unit overseerUnit = UnitArray[overseerRecno];

			if (overseerUnit.IsOwn())
			{
				SERes.sound(overseerUnit.CurLocX, overseerUnit.CurLocY, 1, 'S', overseerUnit.SpriteResId, "SEL");
			}

			//--- add the overseer into the patrol_unit_array[] of this camp ---//

			patrol_unit_array.Add(overseerRecno);

			//------- set the team_info of the overseer -------//

			overseerUnit.TeamInfo.Members.Clear();
			for (int i = 0; i < patrol_unit_array.Count; i++)
			{
				overseerUnit.TeamInfo.Members.Add(patrol_unit_array[i]);
			}
		}

		//-------- display info --------//

		// for player's camp, patrol() can only be called when the player presses the button.
		if (nation_recno == NationArray.player_recno)
			Info.disp();
	}

	public bool patrol_all_soldier()
	{
		//------- detect buttons on hiring firm workers -------//

		int unitRecno;
		int mobileWorkerId = 1;

		patrol_unit_array.Clear(); // reset it, it will be increased later

		while (workers.Count > 0 && mobileWorkerId <= workers.Count)
		{
			if (workers[mobileWorkerId - 1].skill_id == Skill.SKILL_LEADING)
			{
				unitRecno = mobilize_worker(mobileWorkerId, InternalConstants.COMMAND_AUTO);

				patrol_unit_array.Add(unitRecno);
			}
			else
			{
				mobileWorkerId++;
				continue;
			}

			if (unitRecno == 0)
				return false; // keep the rest workers as there is no space for creating the unit

			Unit unit = UnitArray[unitRecno];

			unit.TeamId = UnitArray.cur_team_id; // define it as a team

			if (overseer_recno != 0)
			{
				unit.LeaderId = overseer_recno;
				unit.UpdateLoyalty(); // the unit is just assigned to a new leader, set its target loyalty
			}

			if (nation_recno == NationArray.player_recno)
			{
				unit.SelectedFlag = true;
				UnitArray.selected_count++;
				// set the first soldier as selected; this is also the soldier with the highest leadership (because of sorting)
				if (UnitArray.selected_recno == 0)
					UnitArray.selected_recno = unitRecno;
			}
		}

		UnitArray.cur_team_id++;
		return true;
	}

	public override void assign_unit(int unitRecno)
	{
		Unit unit = UnitArray[unitRecno];

		//------- if this is a construction worker -------//

		if (unit.Skill.SkillId == Skill.SKILL_CONSTRUCTION)
		{
			set_builder(unitRecno);
			return;
		}

		//-------- assign the unit ----------//

		int rankId = UnitArray[unitRecno].Rank;

		if (rankId == Unit.RANK_GENERAL || rankId == Unit.RANK_KING)
		{
			assign_overseer(unitRecno);
		}
		else
		{
			assign_worker(unitRecno);
		}
	}

	public override void assign_overseer(int overseerRecno)
	{
		//---- reset the team member count of the general ----//

		if (overseerRecno != 0)
		{
			Unit unit = UnitArray[overseerRecno];
			unit.TeamInfo.Members.Clear();
			unit.HomeCampId = 0;
		}

		//----- assign the overseer now -------//

		base.assign_overseer(overseerRecno);

		//------------- update influence -----------//

		update_influence();
	}

	public override void assign_worker(int workerUnitRecno)
	{
		base.assign_worker(workerUnitRecno);

		//--- remove the unit from patrol_unit_array when it returns to the base ---//

		validate_patrol_unit();

		//-------- sort soldiers ---------//

		sort_worker();
	}

	public void defense(int targetRecno, bool useRangeAttack = false)
	{
		//--******* BUGHERE , please provide a reasonable condition to set useRangeAttack to 1
		useRangeAttack = UnitArray[targetRecno].MobileType != UnitConstants.UNIT_LAND ? true : false;
		//#### end alex 15/10 ####//

		if (!defense_flag)
			return;

		if (employ_new_worker)
		{
			//---------- reset unit's parameters in the previous defense -----------//
			foreach (DefenseUnit defenseUnit in defense_array)
			{
				if (defenseUnit.status == DefenseUnit.OUTSIDE_CAMP &&
				    defenseUnit.unit_recno != 0 && !UnitArray.IsDeleted(defenseUnit.unit_recno))
				{
					Unit unit = UnitArray[defenseUnit.unit_recno];
					if (unit.NationId == nation_recno &&
					    unit.ActionMisc == UnitConstants.ACTION_MISC_DEFENSE_CAMP_RECNO &&
					    unit.ActionMiscParam == firm_recno)
					{
						unit.clear_unit_defense_mode();
					}
				}
			}

			defense_array.Clear();
		}

		//------------------------------------------------------------------//
		// check all the exist(not dead) units outside the camp and arrange
		// them in the front part of the array.
		//------------------------------------------------------------------//

		if (!employ_new_worker) // some soliders may be outside the camp
		{
			List<DefenseUnit> newDefenseUnits = new List<DefenseUnit>();
			foreach (DefenseUnit defenseUnit in defense_array)
			{
				if (defenseUnit.unit_recno == 0)
					continue; // a free slot

				if (UnitArray.IsDeleted(defenseUnit.unit_recno))
				{
					defenseUnit.unit_recno = 0;
					continue; // unit is dead
				}

				//----------------------------------------------------------------//
				// arrange the recno in the array front part
				//----------------------------------------------------------------//
				if (defenseUnit.status == DefenseUnit.OUTSIDE_CAMP)
				{
					Unit unit = UnitArray[defenseUnit.unit_recno];

					//-------------- ignore this unit if it is dead --------------------//
					if (unit.IsUnitDead())
						continue;

					if (!unit.in_auto_defense_mode())
						continue; // the unit is ordered by the player to do other thing, so cannot control it afterwards

					//--------------- the unit is in defense mode ----------------//
					DefenseUnit newDefenseUnit = new DefenseUnit();
					newDefenseUnit.unit_recno = defenseUnit.unit_recno;
					newDefenseUnit.status = DefenseUnit.OUTSIDE_CAMP;
					newDefenseUnits.Add(newDefenseUnit);
				}
			}

			defense_array.Clear();
			defense_array.AddRange(newDefenseUnits);
		}

		set_employ_worker(false);

		//------------------------------------------------------------------//
		// the unit outside the camp should be in defense mode and ready to
		// attack new target
		//------------------------------------------------------------------//
		foreach (DefenseUnit defenseUnit in defense_array)
		{
			Unit unit = UnitArray[defenseUnit.unit_recno];
			defense_outside_camp(defenseUnit.unit_recno, targetRecno);
			unit.ActionMisc = UnitConstants.ACTION_MISC_DEFENSE_CAMP_RECNO;
			unit.ActionMiscParam = firm_recno;
		}

		//TODO check for bugs
		for (int i = 0; i < workers.Count; i++)
		{
			//------------------------------------------------------------------//
			// order those soldier inside the firm to move to target for attacking
			// keep those unable to attack inside the firm
			//------------------------------------------------------------------//
			//### begin alex 13/10 ###//
			//if(worker_array[mobilizePos].unit_id==UNIT_EXPLOSIVE_CART)
			if (workers[i].unit_id == UnitConstants.UNIT_EXPLOSIVE_CART || (useRangeAttack && workers[i].max_attack_range() == 1))
				//#### end alex 13/10 ####//
			{
				continue;
			}

			int unitRecno = mobilize_worker(i + 1, InternalConstants.COMMAND_AUTO);
			if (unitRecno == 0)
				break;

			Unit unit = UnitArray[unitRecno];
			unit.TeamId = UnitArray.cur_team_id; // define it as a team
			unit.ActionMisc = UnitConstants.ACTION_MISC_DEFENSE_CAMP_RECNO;
			unit.ActionMiscParam = firm_recno; // store the firm_recno for going back camp

			if (overseer_recno != 0)
			{
				unit.LeaderId = overseer_recno;
				unit.UpdateLoyalty(); // update target loyalty based on having a leader assigned
			}

			defense_inside_camp(unitRecno, targetRecno);
			DefenseUnit newDefenseUnit = new DefenseUnit();
			newDefenseUnit.unit_recno = unitRecno;
			newDefenseUnit.status = DefenseUnit.OUTSIDE_CAMP;
			defense_array.Add(newDefenseUnit);
		}

		/*if(overseer_recno>0)
		{
			//------------------------------------------------------------------//
			// order those overseer inside the firm to move to target for attacking
			//------------------------------------------------------------------//
			unit = UnitArray[overseer_recno];
			assign_overseer(0);
			unit.team_id = UnitArray.cur_team_id;   // define it as a team
			unit.action_misc = UnitConstants.ACTION_MISC_DEFENSE_CAMP_RECNO;
			unit.action_misc_para = firm_recno; // store the firm_recno for going back camp
	
			defense_inside_camp(unit.sprite_recno, targetRecno);
			def.unit_recno = unit.sprite_recno;
			def.status = OUTSIDE_CAMP;
		}*/

		UnitArray.cur_team_id++;
	}

	public void defense_inside_camp(int unitRecno, int targetRecno)
	{
		Unit unit = UnitArray[unitRecno];
		unit.defense_attack_unit(targetRecno);

		if (unit.ActionMode == UnitConstants.ACTION_STOP && unit.ActionParam == 0 &&
		    unit.ActionLocX == -1 && unit.ActionLocY == -1)
			unit.defense_detect_target();
	}

	public void defense_outside_camp(int unitRecno, int targetRecno)
	{
		Unit unit = UnitArray[unitRecno];

		if (unit.ActionMode2 == UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET ||
		    unit.ActionMode2 == UnitConstants.ACTION_AUTO_DEFENSE_BACK_CAMP ||
		    (unit.ActionMode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
		     unit.CurAction == Sprite.SPRITE_IDLE))
		{
			//----------------- attack new target now -------------------//
			unit.defense_attack_unit(targetRecno);

			if (unit.ActionMode == UnitConstants.ACTION_STOP && unit.ActionParam == 0 &&
			    unit.ActionLocX == -1 && unit.ActionLocY == -1)
				unit.defense_detect_target();
		}
	}

	public void clear_defense_mode(int firmRecno)
	{
		//------------------------------------------------------------------//
		// change defense unit's to non-defense mode
		//------------------------------------------------------------------//
		foreach (Unit unit in UnitArray)
		{
			if (unit.in_auto_defense_mode() && unit.ActionMisc == UnitConstants.ACTION_MISC_DEFENSE_CAMP_RECNO &&
			    unit.ActionMiscParam == firmRecno)
				unit.clear_unit_defense_mode();
		}

		defense_array.Clear();
	}

	public override int mobilize_worker(int workerId, int remoteAction)
	{
		int unitRecno = base.mobilize_worker(workerId, remoteAction);

		//--- set the home camp firm recno of the unit for later return ---//

		if (unitRecno != 0)
		{
			UnitArray[unitRecno].HomeCampId = firm_recno;
			return unitRecno;
		}

		return 0;
	}

	public override int mobilize_overseer()
	{
		int unitRecno = base.mobilize_overseer();

		//--- set the home camp firm recno of the unit for later return ---//

		if (unitRecno != 0)
		{
			UnitArray[unitRecno].HomeCampId = firm_recno;
			return unitRecno;
		}

		return 0;
	}

	public override void change_nation(int newNationRecno)
	{
		//--- update the UnitInfo vars of the workers in this firm ---//

		foreach (Worker worker in workers)
		{
			UnitRes[worker.unit_id].unit_change_nation(newNationRecno, nation_recno, worker.rank_id);
		}

		//----- reset unit's home camp to this firm -----//

		reset_unit_home_camp(firm_recno);

		//--- if this camp is in the Nation::attack_camp_array[], remove it now ---//

		reset_attack_camp(firm_recno);

		//---- reset AI vars --------//

		ai_capture_town_recno = 0;
		ai_recruiting_soldier = false;

		//-------- change the nation of this firm now ----------//

		base.change_nation(newNationRecno);
	}

	public void update_defense_unit(int unitRecno)
	{
		bool allInCamp = true;

		foreach (DefenseUnit defenseUnit in defense_array)
		{
			if (defenseUnit.unit_recno == 0)
				continue; // empty slot

			if (UnitArray.IsDeleted(defenseUnit.unit_recno))
			{
				defenseUnit.unit_recno = 0;
				defenseUnit.status = DefenseUnit.INSIDE_CAMP;
				continue;
			}

			if (defenseUnit.unit_recno == unitRecno)
			{
				defenseUnit.unit_recno = 0;
				defenseUnit.status = DefenseUnit.INSIDE_CAMP;
				Unit unit = UnitArray[unitRecno];
				unit.Stop2();
				unit.ResetActionMiscParameters();
			}
			else
			{
				allInCamp = false; // some units are still outside camp
			}
		}

		if (allInCamp)
		{
			set_employ_worker(true);
			defense_array.Clear();
		}
	}

	public void validate_patrol_unit()
	{
		for (int i = patrol_unit_array.Count - 1; i >= 0; i--)
		{
			int unitRecno = patrol_unit_array[i];
			bool isPatrolUnit = true;
			if (UnitArray.IsDeleted(unitRecno))
			{
				isPatrolUnit = false;
			}
			else
			{
				Unit unit = UnitArray[unitRecno];
				isPatrolUnit = (unit.IsVisible() && unit.NationId == nation_recno);
			}

			if (!isPatrolUnit)
			{
				patrol_unit_array.RemoveAt(i);
			}
		}
	}

	public void set_employ_worker(bool flag)
	{
		employ_new_worker = flag;
	
		if(!flag)
			ai_status = CAMP_IN_DEFENSE;
		else
			ai_status = FIRM_WITHOUT_ACTION;
	}

	//----------- AI functions ----------//

	public override void process_ai()
	{
		if (Info.TotalDays % 10 == firm_recno % 10)
		{
			if (think_close())
				return;
		}

		// do not call too often as the penalty of accumulation is 10 days
		if (Info.TotalDays % 15 == firm_recno % 15)
			think_use_cash_to_capture();

		//------- assign overseer and workers -------//

		// do not call too often because when an AI action is queued, it will take a while to carry it out
		if (Info.TotalDays % 15 == firm_recno % 15)
			think_recruit();

		//---- if this firm is currently trying to capture a town ----//

		if (ai_capture_town_recno != 0)
		{
			// disable should_close_flag when trying to capture a firm, should_close_flag is set in process_common_ai()
			should_close_flag = false;

			// only when attack_camp_count==0, the attack mission is complete
			if (NationArray[nation_recno].attack_camps.Count == 0 && patrol_unit_array.Count == 0)
			{
				ai_capture_town_recno = 0;
				defense_flag = true; // turn it on again after the capturing plan is finished
				return;
			}

			//process_ai_capturing();
			return;
		}

		//--- if the firm is empty and should be closed, sell/destruct it now ---//

		if (should_close_flag && workers.Count == 0 && patrol_unit_array.Count == 0 && coming_unit_array.Count == 0 &&
		    ai_status != CAMP_IN_DEFENSE)
		{
			ai_del_firm();
			return;
		}

		//----- think about assigning a better commander -----//

		if (Info.TotalDays % 30 == firm_recno % 30)
			think_assign_better_commander();

		//----- think about finding soldiers with the same race as general -----//

		if (Info.TotalDays % 30 == firm_recno % 30)
			think_optimize_soldiers_race();

		//----- think about changing links to foreign town -----//

		if (Info.TotalDays % 5 == firm_recno % 5)
			think_change_town_link();

		//------ think about attacking enemies nearby -------//

		int checkInterval = 13 - NationArray[nation_recno].pref_military_development / 10;

		if (Info.TotalDays % checkInterval == firm_recno % checkInterval)
			think_attack_nearby_enemy();

		//------ think about capturing independent town -------//

		int[] interval_days_array = new int[] { 60, 30, 20, 10 };

		int intervalDays = interval_days_array[Config.ai_aggressiveness - 1];

		// do not call too often because when an AI action is queued, it will take a while to carry it out
		if (Info.TotalDays % intervalDays == firm_recno % intervalDays)
			think_capture();

		//---------------------------------------//

		if (Info.TotalDays % 30 == firm_recno % 30)
		{
			ai_reset_defense_mode(); // reset defense mode if all soldiers are dead
		}
	}

	public override bool ai_should_close()
	{
		return linked_town_array.Count == 0 && linked_firm_array.Count == 0;
	}

	public override void ai_update_link_status()
	{
		Nation ownNation = NationArray[nation_recno];

		//------ always enable links to all towns -----//

		for (int i = 0; i < linked_town_array.Count; i++)
		{
			Town town = TownArray[linked_town_array[i]];

			//---- don't try to capture other nation's towns unless the AI is at war or tense with the nation ----//

			if (town.NationId != 0 && ownNation.get_relation_status(town.NationId) <= NationBase.NATION_TENSE)
			{
				continue;
			}

			toggle_town_link(i + 1, true, InternalConstants.COMMAND_AI);

			//------------------------------------------------------------------//
			// Here we only update this camp's link to the town. 
			// The town's link to this firm is updated in Town::update_target_loyalty().
			//------------------------------------------------------------------//
		}
	}

	public int cur_commander_leadership(int bestRaceId = 0)
	{
		int commanderLeadership = 0;

		//--- get the current leadership of the commander ----//

		if (overseer_recno != 0)
		{
			if (bestRaceId == 0)
				bestRaceId = best_commander_race();

			Unit unitCommander = UnitArray[overseer_recno];

			if (unitCommander.RaceId == bestRaceId)
				commanderLeadership = unitCommander.Skill.SkillLevel;
			else
				commanderLeadership = unitCommander.Skill.SkillLevel / 2; // divided by 2 if the race doesn't match
		}

		return commanderLeadership;
	}

	public int new_commander_leadership(int newRaceId, int newSkillLevel)
	{
		int commanderLeadership = 0;
		int bestRaceId = best_commander_race();

		//--- get the current leadership of the commander ----//

		if (overseer_recno != 0)
		{
			if (newRaceId == bestRaceId)
				commanderLeadership = newSkillLevel;
			else
				commanderLeadership = newSkillLevel / 2; // divided by 2 if the race doesn't match
		}

		return commanderLeadership;
	}

	//-------------- multiplayer checking codes ---------------//
	//virtual	uint8_t crc8();
	//virtual	void	clear_ptr();
	//virtual	void	init_crc(FirmCampCrc *c);

	private void reset_unit_home_camp(int firmRecno)
	{
		//---- reset all units whose home_camp_firm_recno is this firm ----//

		foreach (Unit unit in UnitArray)
		{
			if (unit.HomeCampId == firmRecno)
				unit.HomeCampId = 0;
		}
	}

	private void reset_attack_camp(int firmRecno)
	{
		//--- if this camp is in the Nation::attack_camp_array[], remove it now ---//

		if (firm_ai)
		{
			Nation nation = NationArray[nation_recno];

			for (int i = nation.attack_camps.Count - 1; i >= 0; i--)
			{
				if (nation.attack_camps[i].firm_recno == firmRecno)
				{
					nation.attack_camps.RemoveAt(i);
				}
			}
		}
	}

	//private void		disp_camp_info(int dispY1, int refreshFlag);

	private void train_unit()
	{
		if (overseer_recno == 0)
			return;

		Unit overseerUnit = UnitArray[overseer_recno];

		if (overseerUnit.Skill.SkillId != Skill.SKILL_LEADING)
			return;

		int overseerSkill = overseerUnit.Skill.SkillLevel;
		int incValue;

		//------- increase the commander's leadership ---------//

		if (workers.Count > 0 && overseerUnit.Skill.SkillLevel < 100)
		{
			//-- the more soldiers this commander has, the higher the leadership will increase ---//

			incValue = (int)(5.0 * workers.Count * overseerUnit.HitPoints / overseerUnit.MaxHitPoints
				* (100.0 + overseerUnit.Skill.SkillPotential * 2.0) / 100.0);

			overseerUnit.Skill.SkillLevelMinor += incValue;

			if (overseerUnit.Skill.SkillLevelMinor >= 100)
			{
				overseerUnit.Skill.SkillLevelMinor -= 100;
				overseerUnit.Skill.SkillLevel++;
			}
		}

		//------- increase the commander's combat level ---------//

		if (overseerUnit.Skill.CombatLevel < 100)
		{
			incValue = (int)(20.0 * overseerUnit.HitPoints / overseerUnit.MaxHitPoints
				* (100.0 + overseerUnit.Skill.SkillPotential * 2.0) / 100.0);

			overseerUnit.Skill.CombatLevelMinor += incValue;

			if (overseerUnit.Skill.CombatLevelMinor >= 100)
			{
				overseerUnit.Skill.CombatLevelMinor -= 100;

				overseerUnit.SetCombatLevel(overseerUnit.Skill.CombatLevel + 1);
			}
		}

		//------- increase the solider's combat level -------//

		foreach (Worker worker in workers)
		{
			if (worker.race_id == 0)
				continue;

			//------- increase worker skill -----------//

			if (worker.combat_level < overseerSkill)
			{
				incValue = Math.Max(20, overseerSkill - worker.combat_level)
					* worker.hit_points / worker.max_hit_points()
					* (100 + worker.skill_potential * 2) / 100;
				// with random factors, resulting in 75% to 125% of the original
				int levelMinor = worker.combat_level_minor + incValue;

				while (levelMinor >= 100)
				{
					levelMinor -= 100;
					worker.combat_level++;
				}

				worker.combat_level_minor = levelMinor;
			}

			//-- if the soldier has leadership potential, he learns leadership --//

			if (worker.skill_potential > 0 && worker.skill_level < 100)
			{
				incValue = Math.Max(50, overseerUnit.Skill.SkillLevel - worker.skill_level)
					* worker.hit_points / worker.max_hit_points()
					* worker.skill_potential * 2 / 100;

				worker.skill_level_minor += incValue;

				if (worker.skill_level_minor > 100)
				{
					worker.skill_level++;
					worker.skill_level_minor -= 100;
				}
			}
		}

		sort_worker();
	}

	private void recover_hit_point()
	{
		foreach (Worker worker in workers)
		{
			if (worker.hit_points < worker.max_hit_points())
				worker.hit_points++;
		}
	}

	private void pay_weapon_expense()
	{
		Nation nation = NationArray[nation_recno];

		for (int i = workers.Count - 1; i >= 0; i--)
		{
			Worker worker = workers[i];
			if (worker.unit_id != 0 && UnitRes[worker.unit_id].unit_class == UnitConstants.UNIT_CLASS_WEAPON)
			{
				if (nation.cash > 0)
				{
					nation.add_expense(NationBase.EXPENSE_WEAPON,
						Convert.ToDouble(UnitRes[worker.unit_id].year_cost) / 365.0, true);
				}
				else // decrease hit points if the nation cannot pay the unit
				{
					if (worker.hit_points > 0)
						worker.hit_points--;

					if (worker.hit_points == 0)
						kill_worker(worker); // if its hit points is zero, delete it
				}
			}
		}
	}

	private void update_influence()
	{
		for (int i = 0; i < linked_town_array.Count; i++)
		{
			if (TownArray.IsDeleted(linked_town_array[i]))
				continue;

			Town town = TownArray[linked_town_array[i]];

			if (linked_town_enable_array[i] == InternalConstants.LINK_EE)
			{
				if (town.NationId != 0)
					town.UpdateTargetLoyalty();
				else
					town.UpdateTargetResistance();
			}
		}
	}

	//-------------- AI functions --------------//

	private void ai_reset_defense_mode()
	{
		if (ai_status != CAMP_IN_DEFENSE)
			return;

		//------------------------------------------------------------//
		// reset defense mode if all soldiers are dead
		//------------------------------------------------------------//
		bool found = false;

		foreach (DefenseUnit defenseUnit in defense_array)
		{
			if (defenseUnit.unit_recno == 0)
				continue; // empty slot

			if (UnitArray.IsDeleted(defenseUnit.unit_recno))
				continue;

			Unit unit = UnitArray[defenseUnit.unit_recno];
			if (unit.NationId == nation_recno && unit.ActionMisc == UnitConstants.ACTION_MISC_DEFENSE_CAMP_RECNO &&
			    unit.ActionMiscParam == firm_recno) // is a soldier of this camp
				found = true;
		}

		if (!found)
		{
			set_employ_worker(true); // all soldiers have died, reset defense mode to employ new workers
			defense_array.Clear();
		}
	}

	private bool think_close()
	{
		Nation ownNation = NationArray[nation_recno];
		bool shouldClose = true;
		for (int i = 0; i < linked_town_array.Count; i++)
		{
			Town town = TownArray[linked_town_array[i]];
			if (town.NationId == 0 || town.NationId == nation_recno)
			{
				shouldClose = false;
				break;
			}

			NationRelation nationRelation = ownNation.get_relation(town.NationId);
			if (nationRelation.status < NationBase.NATION_NEUTRAL)
			{
				shouldClose = false;
				break;
			}
		}

		if (shouldClose)
		{
			patrol();
			ai_del_firm();
			return true;
		}

		return false;
	}

	private void think_recruit()
	{
		if (patrol_unit_array.Count > 0) // if there are units of this camp patrolling outside
			return;

		//Do not recruit if we are defending until battle ends
		bool defending = false;
		foreach (DefenseUnit defenseUnit in defense_array)
		{
			if (defenseUnit.unit_recno != 0)
			{
				defending = true;
				break;
			}
		}

		if (defending)
			return;

		Nation nation = NationArray[nation_recno];

		ai_recruiting_soldier = true; // the AI is currently trying to recruit soldiers

		//---- if there are currently units coming to this firm ----//

		int realComingCount = 0;
		for (int i = 0; i < coming_unit_array.Count; i++)
		{
			if (UnitArray.IsDeleted(coming_unit_array[i]))
				continue;

			Unit unit = UnitArray[coming_unit_array[i]];

			//--- check if any of them are still on their way to this firm ---//

			if (unit.NationId == nation_recno && unit.ActionMode == UnitConstants.ACTION_ASSIGN_TO_FIRM)
			{
				realComingCount++;
			}
		}

		if (realComingCount == 0)
			coming_unit_array.Clear();

		//-- if this camp is empty, think about move a whole troop from a useless camp (should_ai_close()==1)

		if (overseer_recno == 0 && workers.Count == 0 && nation.firm_should_close_array[FIRM_CAMP - 1] > 0)
		{
			FirmCamp bestCamp = null;
			int bestRating = 0;

			//--- see if there are any useless camps around and pick the most suitable one ---//

			for (int i = nation.ai_camp_array.Count - 1; i >= 0; i--)
			{
				FirmCamp firmCamp = (FirmCamp)FirmArray[nation.ai_camp_array[i]];

				if (firmCamp.region_id != region_id)
					continue;

				if (firmCamp.should_close_flag && (firmCamp.overseer_recno != 0 || firmCamp.workers.Count > 0))
				{
					int curRating = 100 - Misc.points_distance(center_x, center_y,
						firmCamp.center_x, firmCamp.center_y);

					if (curRating > bestRating)
					{
						bestRating = curRating;
						bestCamp = firmCamp;
					}
				}
			}

			//--------- start the move now -------//

			if (bestCamp != null)
			{
				bestCamp.patrol();

				// there could be chances that there are no some for mobilizing the units
				if (bestCamp.patrol_unit_array.Count == 0)
					return;

				//--- set coming_unit_count for later checking ---//

				coming_unit_array.Clear();
				coming_unit_array.AddRange(bestCamp.patrol_unit_array);

				//---- order the unit to move to this camp now ---//

				UnitArray.assign(loc_x1, loc_y1, false,
					InternalConstants.COMMAND_AI, bestCamp.patrol_unit_array);

				//------ delete the camp as it no longer has any use ----//

				bestCamp.ai_del_firm();
				return;
			}
		}

		//------- get an overseer if there isn't any right now -----//

		if (overseer_recno == 0)
			nation.add_action(loc_x1, loc_y1, -1, -1,
				Nation.ACTION_AI_ASSIGN_OVERSEER, FIRM_CAMP);

		if (workers.Count == MAX_WORKER)
		{
			ai_recruiting_soldier = false;
			return;
		}

		ai_recruit(realComingCount); //first parameter is not used
	}

	private int ai_recruit(int realComingCount)
	{
		if (workers.Count == MAX_WORKER || overseer_recno == 0)
			return 0;

		int recruitCount = MAX_WORKER - workers.Count - realComingCount;
		if (recruitCount <= 0)
			return 0;

		//--- first try to recruit soldiers directly from a linked village ---//

		int majorityRace = majority_race();

		for (int i = 0; i < linked_town_array.Count; i++)
		{
			Town town = TownArray[linked_town_array[i]];

			if (town.NationId != nation_recno || town.JoblessPopulation == 0)
				continue;

			if (!town.CanRecruitPeople())
				continue;

			//-- recruit majority race first, but will also consider other races --//

			int raceId = Math.Max(1, majorityRace);

			for (int j = 0; j < GameConstants.MAX_RACE; j++)
			{
				if (++raceId > GameConstants.MAX_RACE)
					raceId = 1;

				//--- if the loyalty is too low, reward before recruiting ---//

				if (town.RacesJoblessPopulation[raceId - 1] > 0 && town.RacesLoyalty[raceId - 1] < 40)
				{
					if (town.AccumulatedRewardPenalty > 30) // if the reward penalty is too high, don't reward
						break;

					if (NationArray[nation_recno].cash < 1000) // must have cash to reward
						break;

					town.Reward(InternalConstants.COMMAND_AI);
				}

				//---- recruit the soldiers we needed ----//

				while (town.CanRecruit(raceId))
				{
					// last 1-force pulling people from the town to the firm
					pull_town_people(town.TownId, InternalConstants.COMMAND_AI, raceId, true);

					if (--recruitCount == 0)
						return 1;
				}
			}
		}

		//------ next, try to recruit from remote villages only if this fort is capturing something -----//

		bool linkedToOurTown = false;
		for (int i = 0; i < linked_town_array.Count; i++)
		{
			if (TownArray.IsDeleted(linked_town_array[i]))
				continue;

			Town linkedTown = TownArray[linked_town_array[i]];

			if (linkedTown.NationId == nation_recno)
			{
				linkedToOurTown = true;
				break;
			}
		}

		if (linkedToOurTown)
			return 0;

		for (int i = 0; i < recruitCount; i++)
		{
			int unitRecno = NationArray[nation_recno].recruit_jobless_worker(this, 0);
			if (unitRecno != 0)
			{
				Unit unit = UnitArray[unitRecno];
				unit.Assign(loc_x1, loc_y1);
				if (coming_unit_array.Count < MAX_WORKER)
					coming_unit_array.Add(unitRecno);
			}
		}

		//DieselMachine TODO if we need to recruit more and have cash, try to hire from inn

		return 1;
	}

	private void ai_attack_town_defender(Unit attackerUnit)
	{
		bool shouldAttackUnit = false;

		if (attackerUnit.CurAction == Sprite.SPRITE_IDLE)
			shouldAttackUnit = true;

		else if (attackerUnit.CurAction == Sprite.SPRITE_ATTACK)
		{
			//--- if this unit is currently attacking the town, ask it to attack a defender unit ---//

			Town town = TownArray[ai_capture_town_recno];

			if (attackerUnit.ActionLocX == town.LocX1 && attackerUnit.ActionLocY == town.LocY1)
				shouldAttackUnit = true;
		}

		if (!shouldAttackUnit)
			return;

		//---- if there are still town defenders out there ---//

		foreach (Unit unit in UnitArray.EnumerateRandom())
		{
			if (unit.UnitMode == UnitConstants.UNIT_MODE_DEFEND_TOWN && unit.UnitModeParam == ai_capture_town_recno)
			{
				if (unit.NationId != 0)
					NationArray[nation_recno].set_relation_should_attack(unit.NationId, true,
						InternalConstants.COMMAND_AI);

				List<int> selectedUnits = new List<int>(1);
				selectedUnits.Add(attackerUnit.SpriteId);
				UnitArray.attack(unit.NextLocX, unit.NextLocY, false, selectedUnits,
					InternalConstants.COMMAND_AI, unit.SpriteId);
				break;
			}
		}
	}

	private bool think_attack_nearby_enemy()
	{
		//------------------------------------------//

		Nation nation = NationArray[nation_recno];

		int scanRange = 6 + nation.pref_military_courage / 20; // 6 to 11

		int xLoc1 = loc_x1 - scanRange;
		int yLoc1 = loc_y1 - scanRange;
		int xLoc2 = loc_x2 + scanRange;
		int yLoc2 = loc_y2 + scanRange;

		xLoc1 = Math.Max(xLoc1, 0);
		yLoc1 = Math.Max(yLoc1, 0);
		xLoc2 = Math.Min(xLoc2, GameConstants.MapSize - 1);
		yLoc2 = Math.Min(yLoc2, GameConstants.MapSize - 1);

		//------------------------------------------//

		int enemyCombatLevel = 0; // the higher the rating, the easier we can attack the target town.
		int enemyXLoc = -1;
		int enemyYLoc = -1;

		for (int yLoc = yLoc1; yLoc <= yLoc2; yLoc++)
		{
			for (int xLoc = xLoc1; xLoc <= xLoc2; xLoc++)
			{
				Location location = World.GetLoc(xLoc, yLoc);

				//--- if there is an enemy unit here ---//

				if (location.HasUnit(UnitConstants.UNIT_LAND))
				{
					Unit unit = UnitArray[location.UnitId(UnitConstants.UNIT_LAND)];

					if (unit.NationId == 0)
						continue;

					//--- if the unit is idle and he is our enemy ---//

					if (unit.CurAction == Sprite.SPRITE_ATTACK &&
					    nation.get_relation_status(unit.NationId) == NationBase.NATION_HOSTILE)
					{
						enemyCombatLevel += (int)unit.HitPoints;

						if (enemyXLoc == -1 || Misc.Random(5) == 0)
						{
							enemyXLoc = xLoc;
							enemyYLoc = yLoc;
						}
					}
				}
			}
		}

		if (enemyCombatLevel == 0)
			return false;

		//--------- attack the target now -----------//

		if (workers.Count > 0)
		{
			patrol_all_soldier();

			if (patrol_unit_array.Count > 0)
			{
				UnitArray.attack(enemyXLoc, enemyYLoc, false, patrol_unit_array, InternalConstants.COMMAND_AI,
					World.GetLoc(enemyXLoc, enemyYLoc).UnitId(UnitConstants.UNIT_LAND));
				return true;
			}
		}

		return false;
	}

	private void think_change_town_link()
	{
		Nation ownNation = NationArray[nation_recno];

		for (int i = linked_town_array.Count - 1; i >= 0; i--)
		{
			Town town = TownArray[linked_town_array[i]];

			//--- only change links to foreign towns, links to own towns are always on ---//

			if (town.NationId == nation_recno)
				continue;

			//---- only enable links to non-friendly towns ----//

			bool enableFlag = town.NationId == 0 ||
			                  ownNation.get_relation(town.NationId).status == NationBase.NATION_HOSTILE;

			toggle_town_link(i + 1, enableFlag, InternalConstants.COMMAND_AI);
		}
	}

	private new void think_capture()
	{
		if (is_attack_camp) // if this camp has been assigned to an attack mission already
			return;

		int targetTownRecno = think_capture_target_town();

		if (targetTownRecno == 0)
			return;

		//----- if the target town is a nation town -----//

		Town targetTown = TownArray[targetTownRecno];
		Nation ownNation = NationArray[nation_recno];

		if (targetTown.NationId != 0)
		{
			//--- if there are any defenses (camps and mobile units) on the target town, destroy them all first -----//

			// only proceed further when the result is -1, which means no defense on the target town, no attacking is needed.
			if (ownNation.attack_enemy_town_defense(targetTown) != -1)
				return;
		}

		//------ check if the town people will go out to defend -----//

		double thisResistance = 0.0;
		double resistanceDiff = 0.0;
		int defenseCombatLevel = 0;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (targetTown.RacesPopulation[i] < 5) // if the pop count is lower than 5, ingore it
				continue;

			if (targetTown.NationId != 0)
			{
				thisResistance = targetTown.RacesLoyalty[i];
				resistanceDiff = thisResistance - GameConstants.MIN_NATION_DEFEND_LOYALTY;
			}
			else
			{
				thisResistance = targetTown.RacesResistance[i, nation_recno - 1];
				resistanceDiff = thisResistance - GameConstants.MIN_INDEPENDENT_DEFEND_LOYALTY;
			}

			if (resistanceDiff >= 0)
			{
				// resistance decrease per new defender
				double resistanceDecPerDefender = thisResistance / targetTown.RacesPopulation[i];

				// no. of defenders will go out if you attack this town
				int defenderCount = Convert.ToInt32(resistanceDiff / resistanceDecPerDefender) + 1;

				// *2 is defenseCombatLevel is actually the sum of hit points, not combat level
				defenseCombatLevel += targetTown.TownCombatLevel * 2 * defenderCount;
			}
		}

		//--- try using spies if there are defense forces and the nation likes to use spies ---//

		// 1/3 chance of using spies here, otherwise only use spies when we are not strong enough to take over the village by force
		if (defenseCombatLevel > 0)	// && ownNation.pref_spy >= 50 && misc.random(3)==0 )
		{
			// if the camp is trying to capture an independent town, the leadership and race id. of the overseer matters.
			if (targetTown.NationId == 0)
			{
				// a better general is being assigned to this firm, wait for it
				if (think_assign_better_overseer(targetTown))
					return;

				//if( think_capture_use_spy(targetTown) )
				//return;

				// depending on the peacefulness, the nation won't attack if resistance > (0-20)
				if (defenseCombatLevel > 100 + ownNation.pref_military_courage &&
				    resistanceDiff > (100 - ownNation.pref_peacefulness) / 5)
				{
					return;
				}
			}
			/*else
			{
				//--- don't attack if the target nation's military rating is higher than ours ---//
	
				if( NationArray[targetTown.nation_recno].military_rank_rating()
					 > ownNation.military_rank_rating() )
				{
					return;
				}
			}*/
		}

		bool hasLinkedEnemyCamps = false;
		for (int j = 0; j < targetTown.LinkedFirms.Count; j++)
		{
			Firm linkedFirm = FirmArray[targetTown.LinkedFirms[j]];
			if (linkedFirm.nation_recno != nation_recno && linkedFirm.firm_id == FIRM_CAMP)
			{
				hasLinkedEnemyCamps = true;
				break;
			}
		}

		//If no other kingdom tries to capture this village, we will wait
		int averageResistance = targetTown.AverageResistance(nation_recno);
		int averageTargetResistance = targetTown.AverageTargetResistance(nation_recno);
		if (!hasLinkedEnemyCamps && (averageResistance > 25 || averageResistance > averageTargetResistance))
			return;

		//------ send out troops to capture the target town now ----//

		bool rc;

		if (targetTown.NationId != 0)
			rc = ai_capture_enemy_town(targetTown, defenseCombatLevel);
		else
			rc = ai_capture_independent_town(targetTown, defenseCombatLevel);

		//-- use the same approach to capture both enemy and independent towns --//

		if (rc)
		{
			ai_capture_town_recno = targetTownRecno;
			// turn off the defense flag during capturing so the general is staying in the base to influence the town
			defense_flag = false;

			//--- as the current commander has been out to attack the town by ai_attack_target(), we need to assign him back to the camp for influencing the town and eventually capture it ---//

			if (overseer_recno == 0 && targetTown.NationId != 0 && patrol_unit_array.Count > 0)
				UnitArray[patrol_unit_array[0]].Assign(loc_x1, loc_y1);
		}
	}

	private bool think_capture_return()
	{
		//----- if the target town is destroyed ------//

		bool returnCamp = false;

		if (TownArray.IsDeleted(ai_capture_town_recno))
		{
			returnCamp = true;
		}
		else //---- check whether the town has been captured ----//
		{
			Town town = TownArray[ai_capture_town_recno];

			if (town.NationId == nation_recno) // the town has been captured
				returnCamp = true;
		}

		//-------- if should return to the camp now ---------//

		if (returnCamp)
		{
			for (int i = 0; i < patrol_unit_array.Count; i++)
			{
				Unit unit = UnitArray[patrol_unit_array[i]];

				if (unit.IsVisible())
					unit.Assign(loc_x1, loc_y1);
			}

			ai_capture_town_recno = 0; // turn it on again after the capturing plan is finished
			defense_flag = true;
			return true;
		}

		return false;
	}

	private bool think_capture_use_spy(Town targetTown)
	{
		return false;
	}

	private bool think_capture_use_spy2(Town targetTown, int raceId, int curSpyLevel)
	{
		return false;
	}

	private bool think_assign_better_overseer(Town targetTown)
	{
		//----- check if there is already a queued action -----//

		Nation ownNation = NationArray[nation_recno];

		if (ownNation.get_action(loc_x1, loc_y1, -1, -1,
			    Nation.ACTION_AI_ASSIGN_OVERSEER, FIRM_CAMP) != null)
		{
			return true; // there is a queued action being processed already
		}

		//------ get the two most populated races of the town ----//

		int mostRaceId1, mostRaceId2;

		targetTown.GetMostPopulatedRace(out mostRaceId1, out mostRaceId2);

		//-- if the resistance of the majority race has already dropped to its lowest possible --//
		if (mostRaceId1 != 0 && think_assign_better_overseer2(targetTown.TownId, mostRaceId1))
			return true;

		//-- if the resistance of the 2nd majority race has already dropped to its lowest possible --//
		if (mostRaceId2 != 0 && think_assign_better_overseer2(targetTown.TownId, mostRaceId2))
			return true;

		return false;
	}

	private bool think_assign_better_overseer2(int targetTownRecno, int raceId)
	{
		Town town = TownArray[targetTownRecno];
		Nation ownNation = NationArray[nation_recno];

		int currentTargetResistance = 100;
		if (overseer_recno != 0)
		{
			Unit unit = UnitArray[overseer_recno];
			if (unit.RaceId == raceId)
				currentTargetResistance = 100 - unit.LeaderInfluence();
		}

		int targetResistance = 100;
		//DieselMachine TODO we should try to hire capturer also
		int bestUnitRecno = ownNation.find_best_capturer(targetTownRecno, raceId, ref targetResistance);
		if (targetResistance >= currentTargetResistance ||
		    targetResistance >= 50 - ownNation.pref_peacefulness / 5) // 30 to 50 depending on
			return false;

		if (bestUnitRecno == 0 || bestUnitRecno == overseer_recno) // if we already got the best one here
			return false;

		//---- only assign new overseer if the new one's leadership is significantly higher than the current one ----//

		//DieselMachine we may be sending a general of another race. This condition should not be applied in this case
		/*if( overseer_recno && UnitArray[bestUnitRecno].skill.skill_level < UnitArray[overseer_recno].skill.skill_level + 15 )
		{
			return 0;
		}*/

		//------ check what the best unit is -------//

		if (!ownNation.mobilize_capturer(bestUnitRecno))
			return false;

		//---------- add the action to the queue now ----------//

		ownNation.add_action(loc_x1, loc_y1, -1, -1,
			Nation.ACTION_AI_ASSIGN_OVERSEER, FIRM_CAMP, 1, bestUnitRecno);

		return true;
	}

	private void process_ai_capturing()
	{
		if (ai_capture_town_recno == 0)
			return;

		if (think_capture_return()) // if the capturing units should now return to their base.
			return;

		//--- there are still town defender out there, order idle units to attack them ---//

		Town town = TownArray[ai_capture_town_recno];

		if (town.DefendersCount > 0)
		{
			for (int i = patrol_unit_array.Count; i > 0; i--)
			{
				Unit unit = UnitArray[patrol_unit_array[i - 1]];

				if (unit.CurAction == Sprite.SPRITE_IDLE)
					ai_attack_town_defender(unit);
			}
		}

		//--- if the town is still not captured but all mobile town defenders are destroyed, attack the town again ---//

		if (town.DefendersCount == 0)
		{
			//----- have one of the units attack the town ----//

			if (town.NationId != 0)
				NationArray[nation_recno].set_relation_should_attack(town.NationId, true,
					InternalConstants.COMMAND_AI);

			List<int> selectedUnits = new List<int>(1);
			selectedUnits.Add(patrol_unit_array[Misc.Random(patrol_unit_array.Count)]);
			UnitArray.attack(town.LocX1, town.LocY1, false, selectedUnits,
				InternalConstants.COMMAND_AI, 0);
		}
	}

	private int think_capture_target_town()
	{
		if (linked_town_array.Count == 0 || overseer_recno == 0)
			return 0;

		//-- decide which town to attack (only when the camp is linked to more than one town ---//

		int curResistance, curTargetResistance, resistanceDec;
		int minResistance = 100, bestTownRecno = 0;
		Nation ownNation = NationArray[nation_recno];
		int overseerRaceId = UnitArray[overseer_recno].RaceId;

		for (int i = 0; i < linked_town_array.Count; i++)
		{
			Town town = TownArray[linked_town_array[i]];

			if (town.NationId == nation_recno)
				continue;

			//------- if it's an independent town -------//

			if (town.NationId == 0) // only capture independent town
			{
				curResistance = town.AverageResistance(nation_recno);
				curTargetResistance = town.AverageTargetResistance(nation_recno);

				resistanceDec = curResistance - curTargetResistance;

				//------- if the resistance is decreasing ---------//

				// for nation that has a high peacefulness preference they will wait for the loyalty to fall and try not to attack the town unless necessary
				// if it's less than 5, don't count it, as that it will be easy to attack
				if (resistanceDec > 0 &&
				    curResistance > 25 - 25 * ownNation.pref_peacefulness / 100 &&
				    town.RacesPopulation[overseerRaceId - 1] >= 5)
				{
					continue; // let it decrease until it can no longer decrease
				}
			}
			else //-------- if it's a nation town ---------//
			{
				NationRelation nationRelation = ownNation.get_relation(town.NationId);

				if (nationRelation.status != NationBase.NATION_HOSTILE)
					continue;

				curResistance = town.AverageLoyalty();
			}

			//--------------------------------------//

			if (curResistance < minResistance)
			{
				minResistance = curResistance;
				bestTownRecno = town.TownId;
			}
		}

		return bestTownRecno;
	}

	private bool ai_capture_independent_town(Town targetTown, int defenseCombatLevel)
	{
		//---- attack the target town if the force is strong enough ----//

		if (NationArray[nation_recno].ai_attack_target(targetTown.LocX1, targetTown.LocY1,
			    defenseCombatLevel, false, 0, firm_recno))
			return true;

		return false;
	}

	private bool ai_capture_enemy_town(Town targetTown, int defenseCombatLevel)
	{
		bool useAllCamp = false;

		Nation ownNation = NationArray[nation_recno];
		Nation targetNation = NationArray[targetTown.NationId];

		int ourMilitary = ownNation.military_rank_rating();
		int enemyMilitary = targetNation.military_rank_rating();

		//--- use all camps to attack if we have money and we are stronger than the enemy ---//

		if (ourMilitary - enemyMilitary > 30 && ownNation.ai_should_spend(ownNation.pref_military_courage / 2))
			useAllCamp = true;

		//---- use all camps to attack the enemy if the enemy is a human player

		else if (Config.ai_aggressiveness >= Config.OPTION_MODERATE &&
		         !targetNation.is_ai() && ourMilitary > enemyMilitary)
		{
			if (Config.ai_aggressiveness >= Config.OPTION_HIGH || ownNation.pref_peacefulness < 50)
			{
				useAllCamp = true;
			}
		}

		return NationArray[nation_recno].ai_attack_target(targetTown.LocX1, targetTown.LocY1,
			defenseCombatLevel, false, 0, firm_recno, useAllCamp);
	}

	private bool think_use_cash_to_capture()
	{
		if (!NationArray[nation_recno].should_use_cash_to_capture())
			return false;

		for (int i = 0; i < linked_town_array.Count; i++)
		{
			Town town = TownArray[linked_town_array[i]];

			if (town.NationId == nation_recno)
				continue;

			if (town.AccumulatedEnemyGrantPenalty > 0)
				continue;

			if (town.CanGrantToNonOwnTown(nation_recno))
				town.GrantToNonOwnTown(nation_recno, InternalConstants.COMMAND_AI);
		}

		return true;
	}

	private bool think_assign_better_commander()
	{
		//--- we are capturing a village, there is a separate place to assign a better commander ---//

		if (ai_is_capturing_independent_village())
			return false;

		//----- if there is already an overseer being assigned to the camp ---//

		Nation ownNation = NationArray[nation_recno];

		ActionNode actionNode = ownNation.get_action(loc_x1, loc_y1, -1, -1,
			Nation.ACTION_AI_ASSIGN_OVERSEER, FIRM_CAMP);

		//--- if there is already one existing ---//

		//DieselMachine TODO: when we are moving the whole troop from alone standing fort there is no such action
		if (actionNode != null)
		{
			//--- if the action still is already being processed, don't bother with it ---//

			if (actionNode.processing_instance_count > 0)
				return false;

			//--- however, if the action hasn't been processed, we still try to use the approach here ---//
		}

		//-------------------------------------------------//

		int bestRaceId = best_commander_race();
		int bestFirmRecno = 0;
		int bestLeadership = 0;
		int bestWorkerId = 0;

		if (overseer_recno != 0)
		{
			bestLeadership = cur_commander_leadership(bestRaceId);
		}

		//--- first look for soldiers of the current fort ---//

		for (int i = 0; i < workers.Count; i++)
		{
			Worker worker = workers[i];
			if (worker.race_id == 0)
				continue;

			int workerLeadership = worker.skill_level;

			if (worker.race_id != bestRaceId)
				workerLeadership /= 2;

			if (workerLeadership > bestLeadership)
			{
				bestLeadership = workerLeadership;
				bestFirmRecno = firm_recno;
				bestWorkerId = i + 1;
			}
		}

		if (bestFirmRecno == 0)
		{
			//--- locate for a soldier who has a higher leadership ---//

			// nations that have higher loyalty concern will not switch commander too frequently
			bestLeadership += 10 + ownNation.pref_loyalty_concern / 10;

			for (int i = ownNation.ai_camp_array.Count - 1; i >= 0; i--)
			{
				Firm firm = FirmArray[ownNation.ai_camp_array[i]];

				if (firm.region_id != region_id)
					continue;

				for (int j = 0; j < workers.Count; j++)
				{
					Worker worker = workers[j];
					if (worker.race_id == 0)
						continue;

					int workerLeadership = worker.skill_level;

					if (worker.race_id != bestRaceId)
						workerLeadership /= 2;

					if (workerLeadership > bestLeadership)
					{
						bestLeadership = workerLeadership;
						bestFirmRecno = firm.firm_recno;
						bestWorkerId = j + 1;
					}
				}
			}
		}

		if (bestFirmRecno == 0)
		{
			if (overseer_recno != 0 && bestLeadership < 40)
			{
				Unit unitCommander = UnitArray[overseer_recno];
				if (unitCommander.RaceId != bestRaceId)
				{
					Nation ourNation = NationArray[nation_recno];
					int newLeaderRecno = ourNation.train_unit(firm_skill_id, bestRaceId, loc_x1, loc_y1, out _);
					if (newLeaderRecno != 0)
						return ourNation.add_action(loc_x1, loc_y1, -1, -1,
							Nation.ACTION_AI_ASSIGN_OVERSEER, FIRM_CAMP, 1, newLeaderRecno) != null;
				}
			}

			return false;
		}

		//-------- assign the overseer now -------//

		int unitRecno = FirmArray[bestFirmRecno].mobilize_worker(bestWorkerId, InternalConstants.COMMAND_AI);

		if (unitRecno == 0)
			return false;

		Unit unit = UnitArray[unitRecno];

		unit.SetRank(Unit.RANK_GENERAL);

		//---- if there is already an existing but unprocessed one, delete it first ---//

		if (actionNode != null)
			ownNation.del_action(actionNode);

		return ownNation.add_action(loc_x1, loc_y1, -1, -1,
			Nation.ACTION_AI_ASSIGN_OVERSEER, FIRM_CAMP, 1, unitRecno) != null;
	}

	private int best_commander_race()
	{
		//---------------------------------------------//
		//
		// If this camp is the commanding camp of a town,
		// then return the majority race of the town.
		//
		// A camp is the commanding camp of a town when
		// it is the closest camp to the town.
		//
		//---------------------------------------------//

		for (int i = linked_town_array.Count - 1; i >= 0; i--)
		{
			Town town = TownArray[linked_town_array[i]];

			if (town.ClosestOwnCamp() == this)
				return town.MajorityRace();
		}

		//----- check if this camp is trying to capture an independent town ---//

		int targetTownRecno = think_capture_target_town();

		if (targetTownRecno != 0 && TownArray[targetTownRecno].NationId == 0)
			return TownArray[targetTownRecno].MajorityRace();

		//----- Otherwise return the majority race of this camp -----//

		return majority_race();
	}

	private void think_optimize_soldiers_race()
	{
		if (overseer_recno == 0)
			return;

		Unit overseer = UnitArray[overseer_recno];

		bool hasSoldierOfDifferentRace = false;
		foreach (Worker worker in workers)
		{
			if (worker.race_id > 0 && worker.race_id != overseer.RaceId)
			{
				hasSoldierOfDifferentRace = true;
				break;
			}
		}

		if (!hasSoldierOfDifferentRace)
			return;

		FirmCamp bestCamp = null;
		int bestWorkerId = 0;
		int bestDistance = Int32.MaxValue;
		Nation nation = NationArray[nation_recno];
		for (int i = nation.ai_camp_array.Count - 1; i >= 0; i--)
		{
			FirmCamp firmCamp = (FirmCamp)FirmArray[nation.ai_camp_array[i]];

			if (firmCamp.region_id != region_id)
				continue;

			if (firmCamp.firm_recno == firm_recno)
				continue;

			if (firmCamp.overseer_recno != 0 && UnitArray[firmCamp.overseer_recno].RaceId == overseer.RaceId)
				continue;

			for (int j = 0; j < firmCamp.workers.Count; j++)
			{
				if (firmCamp.workers[j].race_id == overseer.RaceId)
				{
					int distance = Misc.points_distance(center_x, center_y, firmCamp.center_x, firmCamp.center_y);
					if (distance < bestDistance)
					{
						bestDistance = distance;
						bestCamp = firmCamp;
						bestWorkerId = j + 1;
						break;
					}
				}
			}
		}

		if (bestCamp != null)
		{
			int unitRecno = bestCamp.mobilize_worker(bestWorkerId, InternalConstants.COMMAND_AI);
			if (unitRecno == 0)
				return;

			Unit unit = UnitArray[unitRecno];
			unit.Assign(loc_x1, loc_y1);
		}
	}

	public override void DrawDetails(IRenderer renderer)
	{
		renderer.DrawCampDetails(this);
	}

	public override void HandleDetailsInput(IRenderer renderer)
	{
		renderer.HandleCampDetailsInput(this);
	}
}