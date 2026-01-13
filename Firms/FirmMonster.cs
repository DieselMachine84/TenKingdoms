using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class MonsterInFirm
{
	public int MonsterId { get; set; }

	public int MobileUnitId { get; set; } // unit id of this monster when it is a mobile unit

	// this is only used as a reference for soldiers to find their leaders
	public int CombatLevel { get; private set; }
	public int HitPoints { get; set; }
	public int MaxHitPoints { get; private set; }

	public int SoldierMonsterId { get; set; } // monster id. of the soldiers led by this monster general
	public int SoldierCount { get; set; } // no. of soldiers commanded by this monster general/king

	private UnitRes UnitRes => Sys.Instance.UnitRes;
	private MonsterRes MonsterRes => Sys.Instance.MonsterRes;

	public void SetCombatLevel(int combatLevel)
	{
		UnitInfo unitInfo = UnitRes[MonsterRes[MonsterId].unit_id];

		CombatLevel = combatLevel;

		MaxHitPoints = unitInfo.hit_points * combatLevel / 100;
	}
}

public class FirmMonster : Firm
{
	public int MonsterId { get; set; } // the monster type id. of the firm.

	private int MonsterAggressiveness { get; set; } // 0-100
	private int MonsterNationRelation { get; set; } // each bit n is high representing this monster will attack nation n.

	private int CurrentMonsterActionMode { get; set; }
	
	private int DefendingKingCount { get; set; }
	private int DefendingGeneralCount { get; set; }
	private int DefendingSoldierCount { get; set; }
	public int DefendTargetId { get; private set; } // used in defend mode, store id of the latest unit attacking this firm

	private MonsterInFirm MonsterKing { get; set; }
	private List<MonsterInFirm> MonsterGenerals { get; } = new List<MonsterInFirm>();

	//TODO what if there are waiting soldiers which general is killed?
	private List<int> WaitingSoldiers { get; } = new List<int>(); // the unit id of their generals are kept here

	//TODO PatrolUnits contains only one general troop. Check
	private List<int> PatrolUnits { get; } = new List<int>();

	private static string[] MonsterFirmName { get; } =
	{
		"Deezboanz Lair", "Rattus Lair", "Broosken Lair", "Haubudam Lair", "Pfith Lair", "Rokken Lair",
		"Doink Lair", "Wyrm Lair", "Droog Lair", "Ick Lair", "Sauroid Lair", "Karrotten Lair", "Holgh Lair"
	};

	private MonsterRes MonsterRes => Sys.Instance.MonsterRes;
	private SiteArray SiteArray => Sys.Instance.SiteArray;

	public FirmMonster()
	{
	}

	protected override void InitDerived()
	{
		MonsterKing = new MonsterInFirm();

		//----------------------------------------//
		// Set monster aggressiveness. It affects:
		//
		// -the number of defenders will be called out at one time.
		//----------------------------------------//

		//-- these vars must be initialized here instead of in FirmMonster.FirmMonster() for random seed sync during load game --//

		MonsterAggressiveness = 20 + Misc.Random(50); // 20 to 70
	}

	protected override void DeinitDerived()
	{
		//-------- mobilize all monsters in the firm --------//

		if (MonsterKing.MonsterId != 0)
			MobilizeKing();

		while (MonsterGenerals.Count > 0)
		{
			if (MobilizeGeneral(1) == 0)
				break;
		}

		ClearDefenseMode();

		int goldAmount = 800 * (MonsterRes[MonsterId].level * 30 + Misc.Random(50)) / 100;

		SiteArray.AddSite(LocCenterX, LocCenterY, Site.SITE_GOLD_COIN, goldAmount);
		SiteArray.OrderAIUnitsToGetSites(); // ask AI units to get the gold coins
	}

	public override string FirmName()
	{
		return MonsterFirmName[MonsterId - 1];
	}

	private int TotalDefendersCount()
	{
		return DefendingKingCount + DefendingGeneralCount + DefendingSoldierCount;
	}

	public override void NextDay()
	{
		ValidatePatrolUnit();

		//---- the monster boss recruit new monsters ----//

		if (Info.TotalDays % 10 == FirmId % 10)
			RecruitSoldier();

		//----- monsters recover hit points -------//

		if (Info.TotalDays % 15 == FirmId % 15)
			RecoverHitPoints();

		//------ monster thinks about expansion -------//

		if (Config.monster_type == Config.OPTION_MONSTER_OFFENSIVE)
		{
			if (Info.TotalDays % 30 == FirmId % 30 && Misc.Random(3) == 0)
				RecruitGeneral();

			//------ attack human towns and firms randomly -----//

			// only start attacking 3 years after the game starts so the human can build up things
			if (Info.GameDate > Info.GameStartDate.AddDays(1000.0) &&
			    Info.TotalDays % 30 == FirmId % 30 &&
			    Misc.Random(FirmRes[FIRM_MONSTER].total_firm_count * ConfigAdv.monster_attack_divisor) == 0)
			{
				ThinkAttackHuman();
			}

			//--------- think expansion ---------//

			// it will expand slower when there are already a lot of the monster structures on the map
			if (Info.TotalDays % 180 == FirmId % 180 && Misc.Random(FirmRes[FIRM_MONSTER].total_firm_count * 10) == 0)
			{
				ThinkExpansion();
			}
		}
	}

	public override void AssignUnit(int unitId)
	{
		UnitMonster unit = (UnitMonster)UnitArray[unitId];

		switch (unit.Rank)
		{
			case Unit.RANK_KING:
				SetKing(unit.MonsterId, unit.Skill.CombatLevel);
				break;

			case Unit.RANK_GENERAL:
				AddGeneral(unitId);
				break;

			case Unit.RANK_SOLDIER:
				AddSoldier(unit.LeaderId);
				break;
		}

		UnitArray.DisappearInFirm(unitId);
	}

	public void SetKing(int monsterId, int combatLevel)
	{
		MonsterKing.MonsterId = monsterId;
		MonsterKing.SetCombatLevel(combatLevel);
		MonsterKing.HitPoints = MonsterKing.MaxHitPoints;
	}

	private void AddGeneral(int generalUnitId)
	{
		if (MonsterGenerals.Count >= GameConstants.MAX_MONSTER_GENERAL_IN_FIRM)
			return;

		UnitMonster unit = (UnitMonster)UnitArray[generalUnitId];

		MonsterInFirm monsterInFirm = new MonsterInFirm();
		monsterInFirm.MonsterId = unit.MonsterId;
		monsterInFirm.SetCombatLevel(unit.Skill.CombatLevel);
		monsterInFirm.HitPoints = (int)unit.HitPoints;

		if (monsterInFirm.HitPoints == 0)
			monsterInFirm.HitPoints = 1;

		monsterInFirm.SoldierMonsterId = unit.SoldierMonsterId;
		monsterInFirm.SoldierCount = 0;

		monsterInFirm.MobileUnitId = generalUnitId; // unit id of this monster when it is a mobile unit
		// this is only used as a reference for soldiers to find their leaders
		MonsterGenerals.Add(monsterInFirm);

		//----- check if there are any soldiers waiting for this general ----//
		//
		// These are soldiers who follow the general to go out to defend
		// against the attack but then went back to the firm sooner
		// than the general does.
		//
		//-------------------------------------------------------------------//

		for (int i = WaitingSoldiers.Count - 1; i >= 0; i--)
		{
			//--- if this waiting soldier was led by this general ---//

			if (WaitingSoldiers[i] == generalUnitId)
			{
				monsterInFirm.SoldierCount++;

				WaitingSoldiers.RemoveAt(i);
			}
		}
	}

	private void AddSoldier(int generalUnitId)
	{
		//----- check if the soldier's leading general is here ----//

		for (int i = 0; i < MonsterGenerals.Count; i++)
		{
			if (MonsterGenerals[i].MobileUnitId == generalUnitId)
			{
				if (MonsterGenerals[i].SoldierCount < GameConstants.MAX_SOLDIER_PER_GENERAL)
					MonsterGenerals[i].SoldierCount++;
				
				return;
			}
		}

		//---- if not, put the soldier into the waiting list ----//

		WaitingSoldiers.Add(generalUnitId);
	}

	public void RecruitGeneral()
	{
		if (MonsterGenerals.Count >= GameConstants.MAX_MONSTER_GENERAL_IN_FIRM * MonsterAggressiveness / 100)
			return;

		if (MonsterKing.MonsterId == 0)
			return;

		//---------- recruit the general now ----------//

		int combatLevel = 40 + Misc.Random(30); // 40 to 70

		MonsterInFirm monsterInFirm = new MonsterInFirm();
		monsterInFirm.MonsterId = MonsterKing.MonsterId;
		monsterInFirm.SetCombatLevel(combatLevel);
		monsterInFirm.HitPoints = monsterInFirm.MaxHitPoints;
		monsterInFirm.SoldierMonsterId = MonsterKing.MonsterId;
		monsterInFirm.SoldierCount = Misc.Random(GameConstants.MAX_SOLDIER_PER_GENERAL / 2) + 1;

		MonsterGenerals.Add(monsterInFirm);
	}

	private void RecruitSoldier()
	{
		for (int i = 0; i < MonsterGenerals.Count; i++)
		{
			MonsterInFirm monsterInFirm = MonsterGenerals[i];
			// 2/3 chance of recruiting a soldier
			if (monsterInFirm.SoldierCount < GameConstants.MAX_SOLDIER_PER_GENERAL && Misc.Random(3) > 0)
			{
				monsterInFirm.SoldierCount++;
			}
		}
	}

	private bool MobilizeKing()
	{
		if (MobilizeMonster(MonsterKing.MonsterId, Unit.RANK_KING, MonsterKing.CombatLevel, MonsterKing.HitPoints) == 0)
			return false;

		MonsterKing.MonsterId = 0;
		return true;
	}

	private int MobilizeGeneral(int generalId, int mobilizeSoldier = 1)
	{
		MonsterInFirm monsterInFirm = MonsterGenerals[generalId - 1];

		//------ mobilize the monster general ------//

		int generalUnitId = MobilizeMonster(monsterInFirm.MonsterId, Unit.RANK_GENERAL, monsterInFirm.CombatLevel, monsterInFirm.HitPoints);

		if (generalUnitId == 0)
			return 0;

		UnitMonster generalUnit = (UnitMonster)UnitArray[generalUnitId];
		generalUnit.TeamId = UnitArray.CurTeamId;
		generalUnit.SoldierMonsterId = monsterInFirm.SoldierMonsterId;

		int mobilizedCount = 1;

		PatrolUnits.Clear();
		PatrolUnits.Add(generalUnitId);

		//------ mobilize soldiers commanded by the monster general ------//

		if (mobilizeSoldier != 0)
		{
			for (int i = 0; i < monsterInFirm.SoldierCount; i++)
			{
				//--- the combat level of its soldiers ranges from 25% to 50% of the combat level of the general ---//

				int soldierCombatLevel =
					monsterInFirm.CombatLevel / GameConstants.MONSTER_SOLDIER_COMBAT_LEVEL_DIVIDER +
					Misc.Random(monsterInFirm.CombatLevel / GameConstants.MONSTER_SOLDIER_COMBAT_LEVEL_DIVIDER);

				int unitId = MobilizeMonster(monsterInFirm.SoldierMonsterId, Unit.RANK_SOLDIER, soldierCombatLevel);

				if (unitId == 0)
					break;

				UnitArray[unitId].TeamId = UnitArray.CurTeamId;
				UnitArray[unitId].LeaderId = generalUnitId;
				mobilizedCount++;

				PatrolUnits.Add(unitId);
				if (PatrolUnits.Count == TeamInfo.MAX_TEAM_MEMBER)
					break;
			}
		}

		generalUnit.TeamInfo.Members.Clear();

		for (int i = 0; i < PatrolUnits.Count; i++)
		{
			generalUnit.TeamInfo.Members.Add(PatrolUnits[i]);
		}

		MonsterGenerals.Remove(monsterInFirm);

		UnitArray.CurTeamId++;

		return mobilizedCount;
	}

	private int MobilizeMonster(int monsterId, int rankId, int combatLevel, int hitPoints = 0)
	{
		//------- locate a space first --------//

		int locX = LocCenterX, locY = LocCenterY;
		MonsterInfo monsterInfo = MonsterRes[monsterId];
		UnitInfo unitInfo = UnitRes[monsterInfo.unit_id];
		SpriteInfo spriteInfo = SpriteRes[unitInfo.sprite_id];

		if (!World.LocateSpace(ref locX, ref locY, locX, locY, spriteInfo.LocWidth, spriteInfo.LocHeight, unitInfo.mobile_type))
			return 0;

		Unit unit = UnitArray.AddUnit(unitInfo.unit_id, 0, rankId, 0, locX, locY);

		UnitMonster monster = (UnitMonster)unit;
		monster.SetMode(UnitConstants.UNIT_MODE_MONSTER, FirmId);
		monster.SetCombatLevel(combatLevel);
		monster.MonsterId = monsterId;
		monster.MonsterActionMode = CurrentMonsterActionMode;
		monster.HitPoints = hitPoints != 0 ? hitPoints : monster.MaxHitPoints;

		//-----------------------------------------------------//
		// enable unit defend mode
		//-----------------------------------------------------//
		if (FirmId != 0) // 0 when firm is ready to be deleted
		{
			monster.Stop2();
			monster.ActionMode2 = UnitConstants.ACTION_MONSTER_DEFEND_DETECT_TARGET;
			monster.ActionPara2 = UnitConstants.MONSTER_DEFEND_DETECT_COUNT;
			monster.ActionMisc = UnitConstants.ACTION_MISC_MONSTER_DEFEND_FIRM_RECNO;
			monster.ActionMiscParam = FirmId;
		}

		return monster.SpriteId;
	}
	
	public int TotalCombatLevel()
	{
		int totalCombatLevel = 50; // for the structure 

		for (int i = 0; i < MonsterGenerals.Count; i++)
		{
			MonsterInFirm monsterInFirm = MonsterGenerals[i];
			// *2 because TotalCombatLevel() actually takes HitPoints instead of CombatLevel
			// *3/2 because the function use 100% + random(100%) for the combat level, so we take 150% as the average
			totalCombatLevel += monsterInFirm.HitPoints +
			                    (monsterInFirm.CombatLevel * 2 / GameConstants.MONSTER_SOLDIER_COMBAT_LEVEL_DIVIDER) * 3 / 2 * monsterInFirm.SoldierCount;
		}

		return totalCombatLevel;
	}

	public override void BeingAttacked(int attackerUnitId)
	{
		int attackerNationId = UnitArray[attackerUnitId].NationId;

		//--- increase reputation of the nation that attacks monsters ---//

		if (attackerNationId != 0)
		{
			NationArray[attackerNationId].change_reputation(GameConstants.REPUTATION_INCREASE_PER_ATTACK_MONSTER);
			SetHostileNation(attackerNationId); // also set hostile with the nation
		}

		//------ MAX no. of defender it should call out -----//

		int maxDefender = GameConstants.MAX_MONSTER_IN_FIRM * MonsterAggressiveness / 200;

		if (TotalDefendersCount() >= maxDefender) // only mobilize new ones when the MAX defender no. has been reached yet
			return;

		CurrentMonsterActionMode = UnitConstants.MONSTER_ACTION_DEFENSE;

		//---- mobilize monster general to defend against the attack ----//

		if (MonsterGenerals.Count > 0)
		{
			while (TotalDefendersCount() < maxDefender)
			{
				if (MonsterGenerals.Count == 0)
					break;

				int mobilizedCount = MobilizeGeneral(Misc.Random(MonsterGenerals.Count) + 1);

				if (mobilizedCount > 0)
				{
					DefendingGeneralCount++;
					DefendingSoldierCount += mobilizedCount - 1;
				}
				else
				{
					break;
				}
			}
		}

		else if (MonsterKing.MonsterId != 0)
		{
			if (MobilizeKing())
				DefendingKingCount++;
		}

		DefendTargetId = attackerUnitId;
	}

	private void ClearDefenseMode()
	{
		//------------------------------------------------------------------//
		// change defense unit's to non-defense mode
		//------------------------------------------------------------------//

		foreach (Unit unit in UnitArray)
		{
			//------ reset the monster's defense mode -----//

			if (unit.InMonsterDefendMode() &&
			    unit.ActionMisc == UnitConstants.ACTION_MISC_MONSTER_DEFEND_FIRM_RECNO &&
			    unit.ActionMiscParam == FirmId)
			{
				unit.ClearMonsterDefendMode();
				((UnitMonster)unit).MonsterActionMode = UnitConstants.MONSTER_ACTION_STOP;
			}

			//--- if this unit belongs to this firm, reset its association with this firm ---//

			if (unit.UnitMode == UnitConstants.UNIT_MODE_MONSTER && unit.UnitModeParam == FirmId)
			{
				unit.UnitModeParam = 0;
			}
		}
	}

	public void ReduceDefenderCount(int rankId)
	{
		switch (rankId)
		{
			case Unit.RANK_KING:
				DefendingKingCount--;
				if (DefendingKingCount < 0) //**BUGHERE
					DefendingKingCount = 0;
				break;

			case Unit.RANK_GENERAL:
				DefendingGeneralCount--;
				if (DefendingGeneralCount < 0) //**BUGHERE
					DefendingGeneralCount = 0;
				break;

			case Unit.RANK_SOLDIER:
				DefendingSoldierCount--;
				if (DefendingSoldierCount < 0) //**BUGHERE
					DefendingSoldierCount = 0;
				break;
		}

		//TODO check this reset condition
		if (TotalDefendersCount() == 0)
			MonsterNationRelation = 0;
	}

	private void SetHostileNation(int nationId)
	{
		if (nationId == 0)
			return;

		MonsterNationRelation |= (0x1 << nationId);
	}

	public void ResetHostileNation(int nationId)
	{
		if (nationId == 0)
			return;

		MonsterNationRelation &= ~(0x1 << nationId);
	}

	public bool IsHostileNation(int nationId)
	{
		if (nationId == 0)
			return false;

		return (MonsterNationRelation & (0x1 << nationId)) != 0;
	}

	private void ValidatePatrolUnit()
	{
		if (PatrolUnits.Count == 0)
			return;

		for (int i = PatrolUnits.Count - 1; i >= 0; i--)
		{
			int unitId = PatrolUnits[i];

			if (UnitArray.IsDeleted(unitId) || !UnitArray[unitId].IsVisible())
			{
				PatrolUnits.RemoveAt(i);
			}
		}

		//TODO check this reset condition
		if (PatrolUnits.Count == 0 && TotalDefendersCount() == 0)
			MonsterNationRelation = 0;
	}

	private void RecoverHitPoints()
	{
		//------ recover the king's hit points -------//

		if (MonsterKing.HitPoints < MonsterKing.MaxHitPoints)
			MonsterKing.HitPoints++;

		//------ recover the generals' hit points -------//

		for (int i = 0; i < MonsterGenerals.Count; i++)
		{
			if (MonsterGenerals[i].HitPoints < MonsterGenerals[i].MaxHitPoints)
				MonsterGenerals[i].HitPoints++;
		}
	}

	public override void DrawDetails(IRenderer renderer)
	{
		renderer.DrawMonsterLairDetails(this);
	}

	public override void HandleDetailsInput(IRenderer renderer)
	{
		renderer.HandleMonsterLairDetailsInput(this);
	}
	
	#region MonsterAIFunctions
	
	public override void ProcessAI()
	{
	}
	
	private int ThinkAttackNeighbor()
	{
		//-- don't attack new target if some mobile monsters are already attacking somebody --//

		if (PatrolUnits.Count > 0)
			return 0;

		//-------- only attack if we have enough generals ---------//

		int generalCount = 0;

		//--- count the number of generals commanding at least 5 soldiers ---//

		for (int i = 0; i < MonsterGenerals.Count; i++)
		{
			MonsterInFirm monsterInFirm = MonsterGenerals[i];
			if (monsterInFirm.SoldierCount >= 5)
				generalCount++;
		}

		if (generalCount <= 1) // don't attack if there is only one general in the firm
			return 0;

		//------ look for neighbors to attack ------//

		int locX = LocCenterX, locY = LocCenterY;
		bool attackFlag = false;
		FirmInfo firmInfo = FirmRes[FirmType];
		int scanLocWidth = GameConstants.MONSTER_ATTACK_NEIGHBOR_RANGE * 2;
		int scanLocHeight = GameConstants.MONSTER_ATTACK_NEIGHBOR_RANGE * 2;
		int scanLimit = scanLocWidth * scanLocHeight;
		int targetNation = 0;

		for (int i = firmInfo.LocWidth * firmInfo.LocHeight + 1; i <= scanLimit; i++)
		{
			Misc.cal_move_around_a_point(i, scanLocWidth, scanLocHeight, out int xOffset, out int yOffset);

			locX = LocCenterX + xOffset;
			locY = LocCenterY + yOffset;
			Misc.BoundLocation(ref locX, ref locY);

			Location location = World.GetLoc(locX, locY);

			if (location.IsFirm())
			{
				Firm firm = FirmArray[location.FirmId()];
				if (firm.NationId != 0)
				{
					targetNation = firm.NationId;
					attackFlag = true;
					locX = firm.LocX1;
					locY = firm.LocY1;
					break;
				}
			}
			else if (location.IsTown())
			{
				Town town = TownArray[location.TownId()];
				if (town.NationId != 0)
				{
					targetNation = town.NationId;
					attackFlag = true;
					locX = town.LocX1;
					locY = town.LocY1;
					break;
				}
			}
		}

		if (!attackFlag)
			return 0;

		//------- attack the civilian now --------//
		CurrentMonsterActionMode = UnitConstants.MONSTER_ACTION_ATTACK;

		MobilizeGeneral(Misc.Random(MonsterGenerals.Count) + 1);

		if (PatrolUnits.Count > 0)
		{
			SetHostileNation(targetNation);
			UnitArray.Attack(locX, locY, false, PatrolUnits, InternalConstants.COMMAND_AI, 0);
			return 1;
		}
		
		return 0;
	}

	private int ThinkAttackHuman()
	{
		//-- don't attack new target if some mobile monsters are already attacking somebody --//

		if (PatrolUnits.Count > 0)
			return 0;

		//-------- only attack if we have enough generals ---------//

		int generalCount = 0;

		//--- count the number of generals commanding at least 5 soldiers ---//

		for (int i = 0; i < MonsterGenerals.Count; i++)
		{
			MonsterInFirm monsterInFirm = MonsterGenerals[i];
			if (monsterInFirm.SoldierCount >= 5)
				generalCount++;
		}

		if (generalCount <= 1) // don't attack if there is only one general in the firm
			return 0;

		Info.SetRankData(false);
		int totalScore = 0;
		//------ the more score the player has, the more often monsters will attack him ------//
		foreach (Nation nation in NationArray)
		{
			totalScore += Info.GetTotalScore(nation.nation_recno);
		}

		int randomValue = Misc.Random(totalScore) + 1;

		totalScore = 0;
		Nation targetNation = null;
		foreach (Nation nation in NationArray)
		{
			totalScore += Info.GetTotalScore(nation.nation_recno);
			if (randomValue <= totalScore)
			{
				targetNation = nation;
				break;
			}
		}

		if (targetNation == null)
			return 0;

		int targetLocX = -1, targetLocY = -1;
		int targetNationId = targetNation.nation_recno;
		List<Firm> targetFirms = new List<Firm>();
		foreach (Firm firm in FirmArray)
		{
			if (firm.NationId != targetNationId || firm.RegionId != RegionId)
				continue;

			targetFirms.Add(firm);
		}

		if (targetFirms.Count > 0)
		{
			Firm selectedFirm = targetFirms[Misc.Random(targetFirms.Count)];
			targetLocX = selectedFirm.LocX1;
			targetLocY = selectedFirm.LocY1;
		}

		if (targetLocX == -1 || targetLocY == -1) // no target selected
			return 0;

		//------- attack the civilian now --------//

		CurrentMonsterActionMode = UnitConstants.MONSTER_ACTION_ATTACK;

		MobilizeGeneral(Misc.Random(MonsterGenerals.Count) + 1);

		if (PatrolUnits.Count > 0)
		{
			SetHostileNation(targetNationId);
			UnitArray.Attack(targetLocX, targetLocY, false, PatrolUnits, InternalConstants.COMMAND_AI, 0);
			return 1;
		}
		
		return 0;
	}

	private int ThinkExpansion()
	{
		if (PatrolUnits.Count > 0)
			return 0;

		if (MonsterKing.MonsterId == 0 || MonsterGenerals.Count < GameConstants.MIN_GENERAL_EXPAND_NUM)
			return 0;

		//--- count the number of generals commanding 8 soldiers ----//

		int generalCount = 0;

		for (int i = 0; i < MonsterGenerals.Count; i++)
		{
			MonsterInFirm monsterInFirm = MonsterGenerals[i];
			if (monsterInFirm.SoldierCount == GameConstants.MAX_SOLDIER_PER_GENERAL)
				generalCount++;
		}

		if (generalCount < GameConstants.MIN_GENERAL_EXPAND_NUM) // don't expand if the no. of generals is less than 3
			return 0;

		//------------- locate space to build monster firm randomly -------------//

		MonsterInfo monsterInfo = MonsterRes[MonsterKing.MonsterId];
		FirmInfo firmInfo = FirmRes[FIRM_MONSTER];
		int teraMask = UnitRes.mobile_type_to_mask(UnitConstants.UNIT_LAND);
		int locX1 = Math.Max(0, LocX1 - GameConstants.EXPAND_FIRM_DISTANCE);
		int locY1 = Math.Max(0, LocY1 - GameConstants.EXPAND_FIRM_DISTANCE);
		int locX2 = Math.Min(GameConstants.MapSize - 1, LocX2 + GameConstants.EXPAND_FIRM_DISTANCE);
		int locY2 = Math.Min(GameConstants.MapSize - 1, LocY2 + GameConstants.EXPAND_FIRM_DISTANCE);

		if (!World.LocateSpaceRandom(ref locX1, ref locY1, locX2, locY2,
			    firmInfo.LocWidth + GameConstants.FREE_SPACE_DISTANCE * 2,
			    firmInfo.LocHeight + GameConstants.FREE_SPACE_DISTANCE * 2, // leave at least 3 location space around the building
			    (locX2 - locX1 + 1) * (locY2 - locY1 + 1), 0, true, teraMask))
		{
			return 0;
		}

		MonsterGenerals.RemoveAt(MonsterGenerals.Count - 1);

		return monsterInfo.build_firm_monster(locX1 + GameConstants.FREE_SPACE_DISTANCE, locY1 + GameConstants.FREE_SPACE_DISTANCE, 1);
	}
	
	#endregion
}