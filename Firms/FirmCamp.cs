using System;
using System.Collections.Generic;
using System.IO;

namespace TenKingdoms;

public class DefenseUnit
{
	public const int INSIDE_CAMP = 0;
	public const int OUTSIDE_CAMP = 1;

	public int UnitId { get; set; }
	public int Status { get; set; } // inside / outside the camp
}

public class FirmCamp : Firm
{
	private List<DefenseUnit> DefenseUnits { get; } = new List<DefenseUnit>();
	private bool			EmployNewWorker { get; set; }
	public int			DefendTargetId { get; private set; } // used in defend mode, store id of the latest target attacking this firm
	public bool			DefenseFlag { get; set; }


	//---------- AI related vars ----------//

	public List<int> PatrolUnits { get; } = new List<int>(MAX_WORKER + 1);
	public List<int> ComingUnits { get; } = new List<int>(MAX_WORKER + 1);

	public int			AICaptureTownId { get; set; }		// >0 if the troop is in the process of attacking the independent village for capturing it.
	public bool			AIRecruitingSoldier { get; set; }
	public bool			IsAttackCamp { get; set; }

	public FirmCamp()
	{
		FirmSkillId = Skill.SKILL_LEADING;

		EmployNewWorker = true;
		DefenseFlag = true;

		AICaptureTownId = 0;
		AIRecruitingSoldier = true;
		IsAttackCamp = false;
	}

	public override void Deinit()
	{
		int firmId = FirmId;

		base.Deinit();

		int saveOverseerId = OverseerId;

		//TODO looks like it is already reset in base.Deinit()
		OverseerId = 0; // set OverseerId to 0 when calling UpdateInfluence(), so this base is disregarded.

		UpdateInfluence();

		OverseerId = saveOverseerId;

		ClearDefenseMode(firmId);

		ResetUnitHomeCamp(firmId); // this must be called at last as Firm.Deinit() will create new units.

		ResetAttackCamp(firmId);
	}

	public override void NextDay()
	{
		base.NextDay();

		ValidatePatrolUnit();

		if (Info.TotalDays % 15 == FirmId % 15)
		{
			TrainUnit();
			RecoverHitPoints();
		}

		PayWeaponExpense();
	}

	public void ValidatePatrolUnit()
	{
		for (int i = PatrolUnits.Count - 1; i >= 0; i--)
		{
			int unitId = PatrolUnits[i];
			bool isPatrolUnit;
			if (UnitArray.IsDeleted(unitId))
			{
				isPatrolUnit = false;
			}
			else
			{
				Unit unit = UnitArray[unitId];
				isPatrolUnit = (unit.IsVisible() && unit.NationId == NationId);
			}

			if (!isPatrolUnit)
			{
				PatrolUnits.RemoveAt(i);
			}
		}
	}
	
	private void TrainUnit()
	{
		if (OverseerId == 0)
			return;

		Unit overseerUnit = UnitArray[OverseerId];

		if (overseerUnit.Skill.SkillId != Skill.SKILL_LEADING)
			return;

		//------- increase the commander's leadership ---------//

		if (Workers.Count > 0 && overseerUnit.Skill.SkillLevel < 100)
		{
			//-- the more soldiers this commander has, the higher the leadership will increase ---//

			int incValue = (int)(5.0 * Workers.Count * overseerUnit.HitPoints / overseerUnit.MaxHitPoints
				* (100.0 + overseerUnit.Skill.SkillPotential * 2.0) / 100.0);

			overseerUnit.IncMinorSkillLevel(incValue);
		}

		//------- increase the commander's combat level ---------//

		if (overseerUnit.Skill.CombatLevel < 100)
		{
			int incValue = (int)(20.0 * overseerUnit.HitPoints / overseerUnit.MaxHitPoints
				* (100.0 + overseerUnit.Skill.SkillPotential * 2.0) / 100.0);

			overseerUnit.IncMinorCombatLevel(incValue);
		}

		//------- increase the soldier's combat level -------//

		foreach (Worker worker in Workers)
		{
			if (worker.RaceId == 0)
				continue;

			//------- increase worker combat level -----------//
			int overseerSkill = overseerUnit.Skill.SkillLevel;

			if (worker.CombatLevel < overseerSkill)
			{
				int incValue = Math.Max(20, overseerSkill - worker.CombatLevel)
					* worker.HitPoints / worker.MaxHitPoints()
					* (100 + worker.SkillPotential * 2) / 100;
				
				// with random factors, resulting in 75% to 125% of the original
				int levelMinor = worker.CombatLevelMinor + incValue;

				while (levelMinor >= 100)
				{
					levelMinor -= 100;
					worker.CombatLevel++;
				}

				worker.CombatLevelMinor = levelMinor;
			}

			//-- if the soldier has leadership potential, he learns leadership --//

			if (worker.SkillPotential > 0 && worker.SkillLevel < 100)
			{
				int incValue = Math.Max(50, overseerUnit.Skill.SkillLevel - worker.SkillLevel)
					* worker.HitPoints / worker.MaxHitPoints()
					* worker.SkillPotential * 2 / 100;

				int levelMinor = worker.SkillLevelMinor + incValue;

				while (levelMinor >= 100)
				{
					levelMinor -= 100;
					worker.SkillLevel++;
				}

				worker.SkillLevelMinor = levelMinor;
			}
		}

		SortWorkers();
	}
	
	private void RecoverHitPoints()
	{
		foreach (Worker worker in Workers)
		{
			if (worker.HitPoints < worker.MaxHitPoints())
				worker.HitPoints++;
		}
	}
	
	private void PayWeaponExpense()
	{
		Nation nation = NationArray[NationId];

		for (int i = Workers.Count - 1; i >= 0; i--)
		{
			Worker worker = Workers[i];
			if (worker.UnitType != 0 && UnitRes[worker.UnitType].UnitClass == UnitConstants.UNIT_CLASS_WEAPON)
			{
				if (nation.Cash > 0)
				{
					nation.AddExpense(NationBase.EXPENSE_WEAPON, (double)UnitRes[worker.UnitType].YearCost / 365.0, true);
				}
				else // decrease hit points if the nation cannot pay the unit
				{
					if (worker.HitPoints > 0)
						worker.HitPoints--;

					if (worker.HitPoints == 0)
						KillWorker(worker); // if its hit points is zero, delete it
				}
			}
		}
	}
	
	public override void ChangeNation(int newNationId)
	{
		ClearDefenseMode(FirmId);

		ResetUnitHomeCamp(FirmId);

		ResetAttackCamp(FirmId);

		AICaptureTownId = 0;
		AIRecruitingSoldier = false;

		base.ChangeNation(newNationId);
	}

	private void ResetUnitHomeCamp(int firmId)
	{
		foreach (Unit unit in UnitArray)
		{
			if (unit.HomeCampId == firmId)
				unit.HomeCampId = 0;
		}
	}

	private void ResetAttackCamp(int firmId)
	{
		if (AIFirm)
		{
			Nation nation = NationArray[NationId];

			for (int i = nation.attack_camps.Count - 1; i >= 0; i--)
			{
				if (nation.attack_camps[i].firm_recno == firmId)
				{
					nation.attack_camps.RemoveAt(i);
				}
			}
		}
	}
	
	
	public override void AssignUnit(int unitId)
	{
		Unit unit = UnitArray[unitId];

		if (unit.Skill.SkillId == Skill.SKILL_CONSTRUCTION)
		{
			SetBuilder(unitId);
			return;
		}

		if (unit.Rank == Unit.RANK_GENERAL || unit.Rank == Unit.RANK_KING)
		{
			AssignOverseer(unitId);
		}
		else
		{
			AssignWorker(unitId);
		}
	}

	public override void AssignOverseer(int newOverseerId)
	{
		if (newOverseerId != 0)
		{
			Unit unit = UnitArray[newOverseerId];
			unit.TeamInfo.Members.Clear();
			unit.HomeCampId = 0;
		}

		base.AssignOverseer(newOverseerId);

		UpdateInfluence();
	}

	protected override void AssignWorker(int workerUnitId)
	{
		base.AssignWorker(workerUnitId);

		ValidatePatrolUnit();
	}
	
	public override int MobilizeOverseer()
	{
		int unitId = base.MobilizeOverseer();

		if (unitId != 0)
			UnitArray[unitId].HomeCampId = FirmId;

		return unitId;
	}
	
	public override int MobilizeWorker(int workerId, int remoteAction)
	{
		int unitId = base.MobilizeWorker(workerId, remoteAction);

		if (unitId != 0)
			UnitArray[unitId].HomeCampId = FirmId;

		return unitId;
	}
	
	public List<int> Patrol()
	{
		//------------------------------------------------------------//
		// If the commander in this camp has units under his lead outside
		// and he is now going to lead a new team, then the old team members should be reset.
		//------------------------------------------------------------//

		if (OverseerId != 0)
		{
			TeamInfo teamInfo = UnitArray[OverseerId].TeamInfo;

			if (Workers.Count > 0 && teamInfo.Members.Count > 0)
			{
				for (int i = 0; i < teamInfo.Members.Count; i++)
				{
					int unitId = teamInfo.Members[i];

					if (UnitArray.IsDeleted(unitId))
						continue;

					UnitArray[unitId].LeaderId = 0;
				}
			}
		}

		//------------------------------------------------------------//
		// mobilize workers first, then the overseer.
		//------------------------------------------------------------//

		int overseerId = OverseerId;

		List<int> patrolSoldiers = PatrolAllSoldier();
		
		if (Workers.Count == 0 && OverseerId != 0)
		{
			Unit unit = UnitArray[OverseerId];
			// set it to the same team as the soldiers which are defined in MobilizeAllWorkers()
			unit.TeamId = UnitArray.CurTeamId - 1;
		}

		AssignOverseer(0);

		if (overseerId != 0 && OverseerId == 0) // has overseer and the overseer is mobilized
		{
			PatrolUnits.Add(overseerId);
			patrolSoldiers.Insert(0, overseerId);

			Unit overseerUnit = UnitArray[overseerId];
			overseerUnit.TeamInfo.Members.Clear();
			foreach (var patrolUnitId in PatrolUnits)
			{
				overseerUnit.TeamInfo.Members.Add(patrolUnitId);
			}
		}

		return patrolSoldiers;
	}

	public List<int> PatrolAllSoldier()
	{
		PatrolUnits.Clear(); // reset it, it will be increased later
		
		int mobileWorkerId = 1;

		List<int> patrolSoldiers = new List<int>();

		while (Workers.Count > 0 && mobileWorkerId <= Workers.Count)
		{
			int unitId;
			
			if (Workers[mobileWorkerId - 1].SkillId == Skill.SKILL_LEADING)
			{
				//TODO unitId may be zero
				unitId = MobilizeWorker(mobileWorkerId, InternalConstants.COMMAND_AUTO);
				if (unitId != 0)
				{
					PatrolUnits.Add(unitId);
					patrolSoldiers.Add(unitId);
				}
			}
			else
			{
				mobileWorkerId++;
				continue;
			}

			if (unitId == 0)
				return patrolSoldiers; // keep the rest workers as there is no space for creating the unit

			Unit unit = UnitArray[unitId];
			unit.TeamId = UnitArray.CurTeamId; // define it as a team

			if (OverseerId != 0)
			{
				unit.LeaderId = OverseerId;
				unit.UpdateLoyalty(); // the unit is just assigned to a new leader, set its target loyalty
			}
		}

		UnitArray.CurTeamId++;
		return patrolSoldiers;
	}
	
	
	public override void AutoDefense(int targetId)
	{
		DefendTargetId = targetId;
		Defense(targetId);
		
		base.AutoDefense(targetId);
	}
	
	public void Defense(int targetId)
	{
		if (!DefenseFlag)
			return;

		if (EmployNewWorker)
		{
			//---------- reset unit's parameters in the previous defense -----------//
			foreach (DefenseUnit defenseUnit in DefenseUnits)
			{
				if (defenseUnit.Status == DefenseUnit.OUTSIDE_CAMP && defenseUnit.UnitId != 0 && !UnitArray.IsDeleted(defenseUnit.UnitId))
				{
					Unit unit = UnitArray[defenseUnit.UnitId];
					if (unit.NationId == NationId &&
					    unit.ActionMisc == UnitConstants.ACTION_MISC_DEFENSE_CAMP_ID &&
					    unit.ActionMiscParam == FirmId)
					{
						unit.ClearUnitDefenseMode();
					}
				}
			}

			DefenseUnits.Clear();
		}

		//------------------------------------------------------------------//
		// check all the exist(not dead) units outside the camp
		// and arrange them in the front part of the array.
		//------------------------------------------------------------------//

		if (!EmployNewWorker) // some soldiers may be outside the camp
		{
			List<DefenseUnit> newDefenseUnits = new List<DefenseUnit>();
			foreach (DefenseUnit defenseUnit in DefenseUnits)
			{
				if (defenseUnit.UnitId == 0 || UnitArray.IsDeleted(defenseUnit.UnitId))
					continue;

				//----------------------------------------------------------------//
				// arrange the ids in the array front part
				//----------------------------------------------------------------//
				if (defenseUnit.Status == DefenseUnit.OUTSIDE_CAMP)
				{
					Unit unit = UnitArray[defenseUnit.UnitId];

					//-------------- ignore this unit if it is dead --------------------//
					if (unit.IsUnitDead())
						continue;

					if (!unit.InAutoDefenseMode())
						continue; // the unit is ordered by the player to do other thing, so cannot control it afterwards

					//--------------- the unit is in defense mode ----------------//
					DefenseUnit newDefenseUnit = new DefenseUnit();
					newDefenseUnit.UnitId = defenseUnit.UnitId;
					newDefenseUnit.Status = DefenseUnit.OUTSIDE_CAMP;
					newDefenseUnits.Add(newDefenseUnit);
				}
			}

			DefenseUnits.Clear();
			DefenseUnits.AddRange(newDefenseUnits);
		}

		SetEmployWorker(false);

		//-----------------------------------------------------------------------------------//
		// the unit outside the camp should be in defense mode and ready to attack new target
		//-----------------------------------------------------------------------------------//
		foreach (DefenseUnit defenseUnit in DefenseUnits)
		{
			Unit unit = UnitArray[defenseUnit.UnitId];
			DefenseOutsideCamp(defenseUnit.UnitId, targetId);
			unit.ActionMisc = UnitConstants.ACTION_MISC_DEFENSE_CAMP_ID;
			unit.ActionMiscParam = FirmId;
		}

		//--******* BUGHERE , please provide a reasonable condition to set useRangeAttack to 1
		bool useRangeAttack = UnitArray[targetId].MobileType != UnitConstants.UNIT_LAND;
		
		//TODO check for bugs
		for (int i = 0; i < Workers.Count; i++)
		{
			//------------------------------------------------------------------//
			// order those soldier inside the firm to move to target for attacking
			// keep those unable to attack inside the firm
			//------------------------------------------------------------------//
			if (Workers[i].UnitType == UnitConstants.UNIT_EXPLOSIVE_CART || (useRangeAttack && Workers[i].MaxAttackRange() == 1))
				continue;

			int unitId = MobilizeWorker(i + 1, InternalConstants.COMMAND_AUTO);
			if (unitId == 0)
				break;

			Unit unit = UnitArray[unitId];
			unit.TeamId = UnitArray.CurTeamId; // define it as a team
			unit.ActionMisc = UnitConstants.ACTION_MISC_DEFENSE_CAMP_ID;
			unit.ActionMiscParam = FirmId;

			if (OverseerId != 0)
			{
				unit.LeaderId = OverseerId;
				unit.UpdateLoyalty(); // update target loyalty based on having a leader assigned
			}

			DefenseInsideCamp(unitId, targetId);
			DefenseUnit newDefenseUnit = new DefenseUnit();
			newDefenseUnit.UnitId = unitId;
			newDefenseUnit.Status = DefenseUnit.OUTSIDE_CAMP;
			DefenseUnits.Add(newDefenseUnit);
		}

		UnitArray.CurTeamId++;
	}

	private void DefenseInsideCamp(int unitId, int targetId)
	{
		Unit unit = UnitArray[unitId];
		unit.DefenseAttackUnit(targetId);

		if (unit.ActionMode == UnitConstants.ACTION_STOP && unit.ActionParam == 0 && unit.ActionLocX == -1 && unit.ActionLocY == -1)
			unit.DefenseDetectTarget();
	}

	private void DefenseOutsideCamp(int unitId, int targetId)
	{
		Unit unit = UnitArray[unitId];

		if (unit.ActionMode2 == UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET || unit.ActionMode2 == UnitConstants.ACTION_AUTO_DEFENSE_BACK_CAMP ||
		    (unit.ActionMode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET && unit.CurAction == Sprite.SPRITE_IDLE))
		{
			unit.DefenseAttackUnit(targetId);

			if (unit.ActionMode == UnitConstants.ACTION_STOP && unit.ActionParam == 0 && unit.ActionLocX == -1 && unit.ActionLocY == -1)
				unit.DefenseDetectTarget();
		}
	}
	
	public void UpdateDefenseUnit(int unitId)
	{
		bool allInCamp = true;

		foreach (DefenseUnit defenseUnit in DefenseUnits)
		{
			if (defenseUnit.UnitId == 0)
				continue; // empty slot

			if (UnitArray.IsDeleted(defenseUnit.UnitId))
			{
				defenseUnit.UnitId = 0;
				defenseUnit.Status = DefenseUnit.INSIDE_CAMP;
				continue;
			}

			if (defenseUnit.UnitId == unitId)
			{
				defenseUnit.UnitId = 0;
				defenseUnit.Status = DefenseUnit.INSIDE_CAMP;
				Unit unit = UnitArray[unitId];
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
			SetEmployWorker(true);
			DefenseUnits.Clear();
		}
	}
	
	private void ClearDefenseMode(int firmId)
	{
		//------------------------------------------------------------------//
		// change defense unit's to non-defense mode
		//------------------------------------------------------------------//
		foreach (Unit unit in UnitArray)
		{
			if (unit.InAutoDefenseMode() && unit.ActionMisc == UnitConstants.ACTION_MISC_DEFENSE_CAMP_ID && unit.ActionMiscParam == firmId)
				unit.ClearUnitDefenseMode();
		}

		DefenseUnits.Clear();
	}
	
	
	private void SetEmployWorker(bool newValue)
	{
		EmployNewWorker = newValue;

		AIStatus = EmployNewWorker ? FIRM_WITHOUT_ACTION : CAMP_IN_DEFENSE;
	}

	private void UpdateInfluence()
	{
		for (int i = 0; i < LinkedTowns.Count; i++)
		{
			if (TownArray.IsDeleted(LinkedTowns[i]))
				continue;

			Town town = TownArray[LinkedTowns[i]];

			if (LinkedTownsEnable[i] == InternalConstants.LINK_EE)
			{
				if (town.NationId != 0)
					town.UpdateTargetLoyalty();
				else
					town.UpdateTargetResistance();
			}
		}
	}

	public int TotalCombatLevel()
	{
		int totalCombatLevel = 0;

		foreach (Worker worker in Workers)
		{
			// use it points instead of combat level because HitPoints represent both combat level and hit points left
			totalCombatLevel += worker.HitPoints;

			//---- the combat level of weapons are higher ------//

			UnitInfo unitInfo = UnitRes[worker.UnitType];

			// ExtraPara keeps the weapon version
			if (unitInfo.UnitClass == UnitConstants.UNIT_CLASS_WEAPON)
				totalCombatLevel += (unitInfo.WeaponPower + worker.ExtraPara - 1) * 30;
		}

		if (OverseerId != 0)
		{
			Unit unit = UnitArray[OverseerId];

			//--- the commander's leadership effects over the soldiers ---//

			// divided by 150 instead of 100 because while the attacking ability of the unit is affected by the general,
			// the hit points isn't, so we shouldn't do a direct multiplication.
			totalCombatLevel += totalCombatLevel * unit.Skill.SkillLevel / 150;

			//------ the leader's own hit points ------//

			totalCombatLevel += (int)unit.HitPoints;
		}

		return totalCombatLevel;
	}
	
	
	public override void DrawDetails(IRenderer renderer)
	{
		renderer.DrawCampDetails(this);
	}

	public override void HandleDetailsInput(IRenderer renderer)
	{
		renderer.HandleCampDetailsInput(this);
	}
	

	#region Old AI Functions
	
	public override void ProcessAI()
	{
		if (Info.TotalDays % 10 == FirmId % 10)
		{
			if (think_close())
				return;
		}

		// do not call too often as the penalty of accumulation is 10 days
		if (Info.TotalDays % 15 == FirmId % 15)
			think_use_cash_to_capture();

		//------- assign overseer and workers -------//

		// do not call too often because when an AI action is queued, it will take a while to carry it out
		if (Info.TotalDays % 15 == FirmId % 15)
			think_recruit();

		//---- if this firm is currently trying to capture a town ----//

		if (AICaptureTownId != 0)
		{
			// disable should_close_flag when trying to capture a firm, should_close_flag is set in process_common_ai()
			ShouldCloseFlag = false;

			// only when attack_camp_count==0, the attack mission is complete
			if (NationArray[NationId].attack_camps.Count == 0 && PatrolUnits.Count == 0)
			{
				AICaptureTownId = 0;
				DefenseFlag = true; // turn it on again after the capturing plan is finished
				return;
			}

			//process_ai_capturing();
			return;
		}

		//--- if the firm is empty and should be closed, sell/destruct it now ---//

		if (ShouldCloseFlag && Workers.Count == 0 && PatrolUnits.Count == 0 && ComingUnits.Count == 0 && AIStatus != CAMP_IN_DEFENSE)
		{
			AIDelFirm();
			return;
		}

		//----- think about assigning a better commander -----//

		if (Info.TotalDays % 30 == FirmId % 30)
			think_assign_better_commander();

		//----- think about finding soldiers with the same race as general -----//

		if (Info.TotalDays % 30 == FirmId % 30)
			think_optimize_soldiers_race();

		//----- think about changing links to foreign town -----//

		if (Info.TotalDays % 5 == FirmId % 5)
			think_change_town_link();

		//------ think about attacking enemies nearby -------//

		int checkInterval = 13 - NationArray[NationId].pref_military_development / 10;

		if (Info.TotalDays % checkInterval == FirmId % checkInterval)
			think_attack_nearby_enemy();

		//------ think about capturing independent town -------//

		int[] intervalDaysArray = { 60, 30, 20, 10 };

		int intervalDays = intervalDaysArray[Config.AIAggressiveness - 1];

		// do not call too often because when an AI action is queued, it will take a while to carry it out
		if (Info.TotalDays % intervalDays == FirmId % intervalDays)
			think_capture();

		//---------------------------------------//

		if (Info.TotalDays % 30 == FirmId % 30)
		{
			ai_reset_defense_mode(); // reset defense mode if all soldiers are dead
		}
	}

	public override bool AIShouldClose()
	{
		return LinkedTowns.Count == 0 && LinkedFirms.Count == 0;
	}

	public override void AIUpdateLinkStatus()
	{
		Nation ownNation = NationArray[NationId];

		//------ always enable links to all towns -----//

		for (int i = 0; i < LinkedTowns.Count; i++)
		{
			Town town = TownArray[LinkedTowns[i]];

			//---- don't try to capture other nation's towns unless the AI is at war or tense with the nation ----//

			if (town.NationId != 0 && ownNation.GetRelationStatus(town.NationId) <= NationBase.NATION_TENSE)
			{
				continue;
			}

			ToggleTownLink(i + 1, true, InternalConstants.COMMAND_AI);

			//------------------------------------------------------------------//
			// Here we only update this camp's link to the town. 
			// The town's link to this firm is updated in Town::update_target_loyalty().
			//------------------------------------------------------------------//
		}
	}

	public override bool IsWorkerFull()
	{
		return Workers.Count + PatrolUnits.Count + ComingUnits.Count >= MAX_WORKER;
	}
	
	public int cur_commander_leadership(int bestRaceId = 0)
	{
		int commanderLeadership = 0;

		//--- get the current leadership of the commander ----//

		if (OverseerId != 0)
		{
			if (bestRaceId == 0)
				bestRaceId = best_commander_race();

			Unit unitCommander = UnitArray[OverseerId];

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

		if (OverseerId != 0)
		{
			if (newRaceId == bestRaceId)
				commanderLeadership = newSkillLevel;
			else
				commanderLeadership = newSkillLevel / 2; // divided by 2 if the race doesn't match
		}

		return commanderLeadership;
	}
	
	private void ai_reset_defense_mode()
	{
		if (AIStatus != CAMP_IN_DEFENSE)
			return;

		//------------------------------------------------------------//
		// reset defense mode if all soldiers are dead
		//------------------------------------------------------------//
		bool found = false;

		foreach (DefenseUnit defenseUnit in DefenseUnits)
		{
			if (defenseUnit.UnitId == 0)
				continue; // empty slot

			if (UnitArray.IsDeleted(defenseUnit.UnitId))
				continue;

			Unit unit = UnitArray[defenseUnit.UnitId];
			if (unit.NationId == NationId && unit.ActionMisc == UnitConstants.ACTION_MISC_DEFENSE_CAMP_ID && unit.ActionMiscParam == FirmId)
				found = true;
		}

		if (!found)
		{
			SetEmployWorker(true); // all soldiers have died, reset defense mode to employ new workers
			DefenseUnits.Clear();
		}
	}

	private bool think_close()
	{
		Nation ownNation = NationArray[NationId];
		bool shouldClose = true;
		for (int i = 0; i < LinkedTowns.Count; i++)
		{
			Town town = TownArray[LinkedTowns[i]];
			if (town.NationId == 0 || town.NationId == NationId)
			{
				shouldClose = false;
				break;
			}

			NationRelation nationRelation = ownNation.GetRelation(town.NationId);
			if (nationRelation.Status < NationBase.NATION_NEUTRAL)
			{
				shouldClose = false;
				break;
			}
		}

		if (shouldClose)
		{
			Patrol();
			AIDelFirm();
			return true;
		}

		return false;
	}

	private void think_recruit()
	{
		if (PatrolUnits.Count > 0) // if there are units of this camp patrolling outside
			return;

		//Do not recruit if we are defending until battle ends
		bool defending = false;
		foreach (DefenseUnit defenseUnit in DefenseUnits)
		{
			if (defenseUnit.UnitId != 0)
			{
				defending = true;
				break;
			}
		}

		if (defending)
			return;

		Nation nation = NationArray[NationId];

		AIRecruitingSoldier = true; // the AI is currently trying to recruit soldiers

		//---- if there are currently units coming to this firm ----//

		int realComingCount = 0;
		for (int i = 0; i < ComingUnits.Count; i++)
		{
			if (UnitArray.IsDeleted(ComingUnits[i]))
				continue;

			Unit unit = UnitArray[ComingUnits[i]];

			//--- check if any of them are still on their way to this firm ---//

			if (unit.NationId == NationId && unit.ActionMode == UnitConstants.ACTION_ASSIGN_TO_FIRM)
			{
				realComingCount++;
			}
		}

		if (realComingCount == 0)
			ComingUnits.Clear();

		//-- if this camp is empty, think about move a whole troop from a useless camp (should_ai_close()==1)

		if (OverseerId == 0 && Workers.Count == 0 && nation.firm_should_close_array[FIRM_CAMP - 1] > 0)
		{
			FirmCamp bestCamp = null;
			int bestRating = 0;

			//--- see if there are any useless camps around and pick the most suitable one ---//

			for (int i = nation.ai_camp_array.Count - 1; i >= 0; i--)
			{
				FirmCamp firmCamp = (FirmCamp)FirmArray[nation.ai_camp_array[i]];

				if (firmCamp.RegionId != RegionId)
					continue;

				if (firmCamp.ShouldCloseFlag && (firmCamp.OverseerId != 0 || firmCamp.Workers.Count > 0))
				{
					int curRating = 100 - Misc.PointsDistance(LocCenterX, LocCenterY, firmCamp.LocCenterX, firmCamp.LocCenterY);

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
				bestCamp.Patrol();

				// there could be chances that there are no some for mobilizing the units
				if (bestCamp.PatrolUnits.Count == 0)
					return;

				//--- set coming_unit_count for later checking ---//

				ComingUnits.Clear();
				ComingUnits.AddRange(bestCamp.PatrolUnits);

				//---- order the unit to move to this camp now ---//

				UnitArray.Assign(LocX1, LocY1, bestCamp.PatrolUnits, InternalConstants.COMMAND_AI);

				//------ delete the camp as it no longer has any use ----//

				bestCamp.AIDelFirm();
				return;
			}
		}

		//------- get an overseer if there isn't any right now -----//

		if (OverseerId == 0)
			nation.add_action(LocX1, LocY1, -1, -1, Nation.ACTION_AI_ASSIGN_OVERSEER, FIRM_CAMP);

		if (Workers.Count == MAX_WORKER)
		{
			AIRecruitingSoldier = false;
			return;
		}

		ai_recruit(realComingCount); //first parameter is not used
	}

	private int ai_recruit(int realComingCount)
	{
		if (Workers.Count == MAX_WORKER || OverseerId == 0)
			return 0;

		int recruitCount = MAX_WORKER - Workers.Count - realComingCount;
		if (recruitCount <= 0)
			return 0;

		//--- first try to recruit soldiers directly from a linked village ---//

		int majorityRace = MajorityRace();

		for (int i = 0; i < LinkedTowns.Count; i++)
		{
			Town town = TownArray[LinkedTowns[i]];

			if (town.NationId != NationId || town.JoblessPopulation == 0)
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

					if (NationArray[NationId].Cash < 1000) // must have cash to reward
						break;

					town.Reward(InternalConstants.COMMAND_AI);
				}

				//---- recruit the soldiers we needed ----//

				while (town.CanRecruit(raceId))
				{
					// last 1-force pulling people from the town to the firm
					PullTownPeople(town.TownId, InternalConstants.COMMAND_AI, raceId, true);

					if (--recruitCount == 0)
						return 1;
				}
			}
		}

		//------ next, try to recruit from remote villages only if this fort is capturing something -----//

		bool linkedToOurTown = false;
		for (int i = 0; i < LinkedTowns.Count; i++)
		{
			if (TownArray.IsDeleted(LinkedTowns[i]))
				continue;

			Town linkedTown = TownArray[LinkedTowns[i]];

			if (linkedTown.NationId == NationId)
			{
				linkedToOurTown = true;
				break;
			}
		}

		if (linkedToOurTown)
			return 0;

		for (int i = 0; i < recruitCount; i++)
		{
			int unitId = NationArray[NationId].recruit_jobless_worker(this, 0);
			if (unitId != 0)
			{
				Unit unit = UnitArray[unitId];
				unit.Assign(LocX1, LocY1);
				if (ComingUnits.Count < MAX_WORKER)
					ComingUnits.Add(unitId);
			}
		}

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

			Town town = TownArray[AICaptureTownId];

			if (attackerUnit.ActionLocX == town.LocX1 && attackerUnit.ActionLocY == town.LocY1)
				shouldAttackUnit = true;
		}

		if (!shouldAttackUnit)
			return;

		//---- if there are still town defenders out there ---//

		foreach (Unit unit in UnitArray.EnumerateRandom())
		{
			if (unit.UnitMode == UnitConstants.UNIT_MODE_DEFEND_TOWN && unit.UnitModeParam == AICaptureTownId)
			{
				if (unit.NationId != 0)
					NationArray[NationId].SetRelationShouldAttack(unit.NationId, true, InternalConstants.COMMAND_AI);

				List<int> selectedUnits = new List<int>(1);
				selectedUnits.Add(attackerUnit.SpriteId);
				UnitArray.Attack(unit.NextLocX, unit.NextLocY, false, selectedUnits, InternalConstants.COMMAND_AI, unit.SpriteId);
				break;
			}
		}
	}

	private bool think_attack_nearby_enemy()
	{
		//------------------------------------------//

		Nation nation = NationArray[NationId];

		int scanRange = 6 + nation.pref_military_courage / 20; // 6 to 11

		int xLoc1 = LocX1 - scanRange;
		int yLoc1 = LocY1 - scanRange;
		int xLoc2 = LocX2 + scanRange;
		int yLoc2 = LocY2 + scanRange;

		xLoc1 = Math.Max(xLoc1, 0);
		yLoc1 = Math.Max(yLoc1, 0);
		xLoc2 = Math.Min(xLoc2, Config.MapSize - 1);
		yLoc2 = Math.Min(yLoc2, Config.MapSize - 1);

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

					if (unit.CurAction == Sprite.SPRITE_ATTACK && nation.GetRelationStatus(unit.NationId) == NationBase.NATION_HOSTILE)
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

		if (Workers.Count > 0)
		{
			PatrolAllSoldier();

			if (PatrolUnits.Count > 0)
			{
				int targetUnitId = World.GetLoc(enemyXLoc, enemyYLoc).UnitId(UnitConstants.UNIT_LAND);
				UnitArray.Attack(enemyXLoc, enemyYLoc, false, PatrolUnits, InternalConstants.COMMAND_AI, targetUnitId);
				return true;
			}
		}

		return false;
	}

	private void think_change_town_link()
	{
		Nation ownNation = NationArray[NationId];

		for (int i = LinkedTowns.Count - 1; i >= 0; i--)
		{
			Town town = TownArray[LinkedTowns[i]];

			//--- only change links to foreign towns, links to own towns are always on ---//

			if (town.NationId == NationId)
				continue;

			//---- only enable links to non-friendly towns ----//

			bool enableFlag = town.NationId == 0 || ownNation.GetRelation(town.NationId).Status == NationBase.NATION_HOSTILE;

			ToggleTownLink(i + 1, enableFlag, InternalConstants.COMMAND_AI);
		}
	}

	private void think_capture()
	{
		if (IsAttackCamp) // if this camp has been assigned to an attack mission already
			return;

		int targetTownRecno = think_capture_target_town();

		if (targetTownRecno == 0)
			return;

		//----- if the target town is a nation town -----//

		Town targetTown = TownArray[targetTownRecno];
		Nation ownNation = NationArray[NationId];

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
			if (targetTown.RacesPopulation[i] < 5) // if the pop count is lower than 5, ignore it
				continue;

			if (targetTown.NationId != 0)
			{
				thisResistance = targetTown.RacesLoyalty[i];
				resistanceDiff = thisResistance - GameConstants.MIN_NATION_DEFEND_LOYALTY;
			}
			else
			{
				thisResistance = targetTown.RacesResistance[i, NationId - 1];
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
				if (defenseCombatLevel > 100 + ownNation.pref_military_courage && resistanceDiff > (100 - ownNation.pref_peacefulness) / 5)
				{
					return;
				}
			}
			/*else
			{
				//--- don't attack if the target nation's military rating is higher than ours ---//
	
				if( NationArray[targetTown.nation_recno].military_rank_rating() > ownNation.military_rank_rating() )
				{
					return;
				}
			}*/
		}

		bool hasLinkedEnemyCamps = false;
		for (int j = 0; j < targetTown.LinkedFirms.Count; j++)
		{
			Firm linkedFirm = FirmArray[targetTown.LinkedFirms[j]];
			if (linkedFirm.NationId != NationId && linkedFirm.FirmType == FIRM_CAMP)
			{
				hasLinkedEnemyCamps = true;
				break;
			}
		}

		//If no other kingdom tries to capture this village, we will wait
		int averageResistance = targetTown.AverageResistance(NationId);
		int averageTargetResistance = targetTown.AverageTargetResistance(NationId);
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
			AICaptureTownId = targetTownRecno;
			// turn off the defense flag during capturing so the general is staying in the base to influence the town
			DefenseFlag = false;

			//--- as the current commander has been out to attack the town by ai_attack_target(),
			//we need to assign him back to the camp for influencing the town and eventually capture it ---//

			if (OverseerId == 0 && targetTown.NationId != 0 && PatrolUnits.Count > 0)
				UnitArray[PatrolUnits[0]].Assign(LocX1, LocY1);
		}
	}

	private bool think_capture_return()
	{
		//----- if the target town is destroyed ------//

		bool returnCamp = false;

		if (TownArray.IsDeleted(AICaptureTownId))
		{
			returnCamp = true;
		}
		else //---- check whether the town has been captured ----//
		{
			Town town = TownArray[AICaptureTownId];

			if (town.NationId == NationId) // the town has been captured
				returnCamp = true;
		}

		//-------- if should return to the camp now ---------//

		if (returnCamp)
		{
			for (int i = 0; i < PatrolUnits.Count; i++)
			{
				Unit unit = UnitArray[PatrolUnits[i]];

				if (unit.IsVisible())
					unit.Assign(LocX1, LocY1);
			}

			AICaptureTownId = 0; // turn it on again after the capturing plan is finished
			DefenseFlag = true;
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

		Nation ownNation = NationArray[NationId];

		if (ownNation.get_action(LocX1, LocY1, -1, -1, Nation.ACTION_AI_ASSIGN_OVERSEER, FIRM_CAMP) != null)
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
		Nation ownNation = NationArray[NationId];

		int currentTargetResistance = 100;
		if (OverseerId != 0)
		{
			Unit unit = UnitArray[OverseerId];
			if (unit.RaceId == raceId)
				currentTargetResistance = 100 - unit.LeaderInfluence();
		}

		int targetResistance = 100;
		//DieselMachine TODO we should try to hire capturer also
		int bestUnitRecno = ownNation.find_best_capturer(targetTownRecno, raceId, ref targetResistance);
		if (targetResistance >= currentTargetResistance || targetResistance >= 50 - ownNation.pref_peacefulness / 5) // 30 to 50 depending on
			return false;

		if (bestUnitRecno == 0 || bestUnitRecno == OverseerId) // if we already got the best one here
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

		ownNation.add_action(LocX1, LocY1, -1, -1, Nation.ACTION_AI_ASSIGN_OVERSEER, FIRM_CAMP, 1, bestUnitRecno);

		return true;
	}

	private void process_ai_capturing()
	{
		if (AICaptureTownId == 0)
			return;

		if (think_capture_return()) // if the capturing units should now return to their base.
			return;

		//--- there are still town defender out there, order idle units to attack them ---//

		Town town = TownArray[AICaptureTownId];

		if (town.DefendersCount > 0)
		{
			for (int i = PatrolUnits.Count; i > 0; i--)
			{
				Unit unit = UnitArray[PatrolUnits[i - 1]];

				if (unit.CurAction == Sprite.SPRITE_IDLE)
					ai_attack_town_defender(unit);
			}
		}

		//--- if the town is still not captured but all mobile town defenders are destroyed, attack the town again ---//

		if (town.DefendersCount == 0)
		{
			//----- have one of the units attack the town ----//

			if (town.NationId != 0)
				NationArray[NationId].SetRelationShouldAttack(town.NationId, true, InternalConstants.COMMAND_AI);

			List<int> selectedUnits = new List<int>(1);
			selectedUnits.Add(PatrolUnits[Misc.Random(PatrolUnits.Count)]);
			UnitArray.Attack(town.LocX1, town.LocY1, false, selectedUnits, InternalConstants.COMMAND_AI, 0);
		}
	}

	private int think_capture_target_town()
	{
		if (LinkedTowns.Count == 0 || OverseerId == 0)
			return 0;

		//-- decide which town to attack (only when the camp is linked to more than one town ---//

		int curResistance, curTargetResistance, resistanceDec;
		int minResistance = 100, bestTownRecno = 0;
		Nation ownNation = NationArray[NationId];
		int overseerRaceId = UnitArray[OverseerId].RaceId;

		for (int i = 0; i < LinkedTowns.Count; i++)
		{
			Town town = TownArray[LinkedTowns[i]];

			if (town.NationId == NationId)
				continue;

			//------- if it's an independent town -------//

			if (town.NationId == 0) // only capture independent town
			{
				curResistance = town.AverageResistance(NationId);
				curTargetResistance = town.AverageTargetResistance(NationId);

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
				NationRelation nationRelation = ownNation.GetRelation(town.NationId);

				if (nationRelation.Status != NationBase.NATION_HOSTILE)
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

		if (NationArray[NationId].ai_attack_target(targetTown.LocX1, targetTown.LocY1,
			    defenseCombatLevel, false, 0, FirmId))
			return true;

		return false;
	}

	private bool ai_capture_enemy_town(Town targetTown, int defenseCombatLevel)
	{
		bool useAllCamp = false;

		Nation ownNation = NationArray[NationId];
		Nation targetNation = NationArray[targetTown.NationId];

		int ourMilitary = ownNation.MilitaryRankRating();
		int enemyMilitary = targetNation.MilitaryRankRating();

		//--- use all camps to attack if we have money and we are stronger than the enemy ---//

		if (ourMilitary - enemyMilitary > 30 && ownNation.ai_should_spend(ownNation.pref_military_courage / 2))
			useAllCamp = true;

		//---- use all camps to attack the enemy if the enemy is a human player

		else if (Config.AIAggressiveness >= Config.OPTION_MODERATE && !targetNation.IsAI() && ourMilitary > enemyMilitary)
		{
			if (Config.AIAggressiveness >= Config.OPTION_HIGH || ownNation.pref_peacefulness < 50)
			{
				useAllCamp = true;
			}
		}

		return NationArray[NationId].ai_attack_target(targetTown.LocX1, targetTown.LocY1,
			defenseCombatLevel, false, 0, FirmId, useAllCamp);
	}

	private bool think_use_cash_to_capture()
	{
		if (!NationArray[NationId].should_use_cash_to_capture())
			return false;

		for (int i = 0; i < LinkedTowns.Count; i++)
		{
			Town town = TownArray[LinkedTowns[i]];

			if (town.NationId == NationId)
				continue;

			if (town.AccumulatedEnemyGrantPenalty > 0)
				continue;

			if (town.CanGrantToNonOwnTown(NationId))
				town.GrantToNonOwnTown(NationId, InternalConstants.COMMAND_AI);
		}

		return true;
	}

	private bool think_assign_better_commander()
	{
		//--- we are capturing a village, there is a separate place to assign a better commander ---//

		if (ai_is_capturing_independent_village())
			return false;

		//----- if there is already an overseer being assigned to the camp ---//

		Nation ownNation = NationArray[NationId];

		ActionNode actionNode = ownNation.get_action(LocX1, LocY1, -1, -1,
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

		if (OverseerId != 0)
		{
			bestLeadership = cur_commander_leadership(bestRaceId);
		}

		//--- first look for soldiers of the current fort ---//

		for (int i = 0; i < Workers.Count; i++)
		{
			Worker worker = Workers[i];
			if (worker.RaceId == 0)
				continue;

			int workerLeadership = worker.SkillLevel;

			if (worker.RaceId != bestRaceId)
				workerLeadership /= 2;

			if (workerLeadership > bestLeadership)
			{
				bestLeadership = workerLeadership;
				bestFirmRecno = FirmId;
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

				if (firm.RegionId != RegionId)
					continue;

				for (int j = 0; j < Workers.Count; j++)
				{
					Worker worker = Workers[j];
					if (worker.RaceId == 0)
						continue;

					int workerLeadership = worker.SkillLevel;

					if (worker.RaceId != bestRaceId)
						workerLeadership /= 2;

					if (workerLeadership > bestLeadership)
					{
						bestLeadership = workerLeadership;
						bestFirmRecno = firm.FirmId;
						bestWorkerId = j + 1;
					}
				}
			}
		}

		if (bestFirmRecno == 0)
		{
			if (OverseerId != 0 && bestLeadership < 40)
			{
				Unit unitCommander = UnitArray[OverseerId];
				if (unitCommander.RaceId != bestRaceId)
				{
					Nation ourNation = NationArray[NationId];
					int newLeaderRecno = ourNation.train_unit(FirmSkillId, bestRaceId, LocX1, LocY1, out _);
					if (newLeaderRecno != 0)
						return ourNation.add_action(LocX1, LocY1, -1, -1,
							Nation.ACTION_AI_ASSIGN_OVERSEER, FIRM_CAMP, 1, newLeaderRecno) != null;
				}
			}

			return false;
		}

		//-------- assign the overseer now -------//

		int unitRecno = FirmArray[bestFirmRecno].MobilizeWorker(bestWorkerId, InternalConstants.COMMAND_AI);

		if (unitRecno == 0)
			return false;

		Unit unit = UnitArray[unitRecno];

		unit.SetRank(Unit.RANK_GENERAL);

		//---- if there is already an existing but unprocessed one, delete it first ---//

		if (actionNode != null)
			ownNation.del_action(actionNode);

		return ownNation.add_action(LocX1, LocY1, -1, -1,
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

		for (int i = LinkedTowns.Count - 1; i >= 0; i--)
		{
			Town town = TownArray[LinkedTowns[i]];

			if (town.ClosestOwnCamp() == this)
				return town.MajorityRace();
		}

		//----- check if this camp is trying to capture an independent town ---//

		int targetTownRecno = think_capture_target_town();

		if (targetTownRecno != 0 && TownArray[targetTownRecno].NationId == 0)
			return TownArray[targetTownRecno].MajorityRace();

		//----- Otherwise return the majority race of this camp -----//

		return MajorityRace();
	}

	private void think_optimize_soldiers_race()
	{
		if (OverseerId == 0)
			return;

		Unit overseer = UnitArray[OverseerId];

		bool hasSoldierOfDifferentRace = false;
		foreach (Worker worker in Workers)
		{
			if (worker.RaceId > 0 && worker.RaceId != overseer.RaceId)
			{
				hasSoldierOfDifferentRace = true;
				break;
			}
		}

		if (!hasSoldierOfDifferentRace)
			return;

		FirmCamp bestCamp = null;
		int bestWorkerId = 0;
		int bestDistance = Int16.MaxValue;
		Nation nation = NationArray[NationId];
		for (int i = nation.ai_camp_array.Count - 1; i >= 0; i--)
		{
			FirmCamp firmCamp = (FirmCamp)FirmArray[nation.ai_camp_array[i]];

			if (firmCamp.RegionId != RegionId)
				continue;

			if (firmCamp.FirmId == FirmId)
				continue;

			if (firmCamp.OverseerId != 0 && UnitArray[firmCamp.OverseerId].RaceId == overseer.RaceId)
				continue;

			for (int j = 0; j < firmCamp.Workers.Count; j++)
			{
				if (firmCamp.Workers[j].RaceId == overseer.RaceId)
				{
					int distance = Misc.PointsDistance(LocCenterX, LocCenterY, firmCamp.LocCenterX, firmCamp.LocCenterY);
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
			int unitRecno = bestCamp.MobilizeWorker(bestWorkerId, InternalConstants.COMMAND_AI);
			if (unitRecno == 0)
				return;

			Unit unit = UnitArray[unitRecno];
			unit.Assign(LocX1, LocY1);
		}
	}
	
	public int AverageCombatLevel()
	{
		int personCount = Workers.Count + (OverseerId > 0 ? 1 : 0);

		return personCount > 0 ? TotalCombatLevel() / personCount : 0;
	}

	public bool ai_is_capturing_independent_village()
	{
		for (int i = 0; i < LinkedTowns.Count; i++)
		{
			Town town = TownArray[LinkedTowns[i]];
			if (town.NationId == 0)
				return true;
		}

		return false;
	}

	public override bool AIHasExcessWorker()
	{
		if (LinkedTowns.Count == 0)
			return true;

		if (ai_is_capturing_independent_village()) // no if the camp is trying to capture an independent town
			return false;

		if (IsAttackCamp) // no if the camp is trying to capture an independent town
			return false;

		if (ShouldCloseFlag)
			return true;

		return false;
	}
	
	#endregion
	
	#region SaveAndLoad

	public override void SaveTo(BinaryWriter writer)
	{
		base.SaveTo(writer);
		writer.Write(DefenseUnits.Count);
		for (int i = 0; i < DefenseUnits.Count; i++)
		{
			writer.Write(DefenseUnits[i].UnitId);
			writer.Write(DefenseUnits[i].Status);
		}
		writer.Write(EmployNewWorker);
		writer.Write(DefendTargetId);
		writer.Write(DefenseFlag);
		writer.Write(PatrolUnits.Count);
		for (int i = 0; i < PatrolUnits.Count; i++)
			writer.Write(PatrolUnits[i]);
		writer.Write(ComingUnits.Count);
		for (int i = 0; i < ComingUnits.Count; i++)
			writer.Write(ComingUnits[i]);
		writer.Write(AICaptureTownId);
		writer.Write(AIRecruitingSoldier);
		writer.Write(IsAttackCamp);
	}

	public override void LoadFrom(BinaryReader reader)
	{
		base.LoadFrom(reader);
		int defenseUnitsCount = reader.ReadInt32();
		for (int i = 0; i < defenseUnitsCount; i++)
		{
			DefenseUnit defenseUnit = new DefenseUnit();
			defenseUnit.UnitId = reader.ReadInt32();
			defenseUnit.Status = reader.ReadInt32();
			DefenseUnits.Add(defenseUnit);
		}
		EmployNewWorker = reader.ReadBoolean();
		DefendTargetId = reader.ReadInt32();
		DefenseFlag = reader.ReadBoolean();
		int patrolUnitsCount = reader.ReadInt32();
		for (int i = 0; i < patrolUnitsCount; i++)
			PatrolUnits.Add(reader.ReadInt32());
		int comingUnitsCount = reader.ReadInt32();
		for (int i = 0; i < comingUnitsCount; i++)
			ComingUnits.Add(reader.ReadInt32());
		AICaptureTownId = reader.ReadInt32();
		AIRecruitingSoldier = reader.ReadBoolean();
		IsAttackCamp = reader.ReadBoolean();
	}
	
	#endregion
}