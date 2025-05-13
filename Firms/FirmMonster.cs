using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class MonsterInFirm
{
	public int monster_id;
	public int _unused;

	public int mobile_unit_recno; // unit recno of this monster when it is a mobile unit

	// this is only used as a reference for soldiers to find their leaders
	public int combat_level;
	public int hit_points;
	public int max_hit_points;

	public int soldier_monster_id; // monster id. of the soldiers led by this monster general
	public int soldier_count; // no. of soldiers commaned by this monster general/king

	private UnitRes UnitRes => Sys.Instance.UnitRes;
	private MonsterRes MonsterRes => Sys.Instance.MonsterRes;

	public void set_combat_level(int combatLevel)
	{
		UnitInfo unitInfo = UnitRes[MonsterRes[monster_id].unit_id];

		combat_level = combatLevel;

		max_hit_points = unitInfo.hit_points * combatLevel / 100;
	}
}

public class FirmMonster : Firm
{
	public int monster_id; // the monster type id. of the firm.

	public int monster_aggressiveness; // 0-100

	public int defending_king_count;
	public int defending_general_count;
	public int defending_soldier_count;

	public MonsterInFirm monster_king;
	//public MonsterInFirm[] monster_general_array = new MonsterInFirm[GameConstants.MAX_MONSTER_GENERAL_IN_FIRM];
	public List<MonsterInFirm> monsterGenerals = new List<MonsterInFirm>();

	public List<int> waitingSoldiers = new List<int>(); // the unit recno of their generals are kept here

	public int monster_nation_relation; // each bit n is high representing this independent town will attack nation n.
	public int defend_target_recno; // used in defend mode, store recno of the latest unit attacking this firm

	public List<int> patrolUnits = new List<int>();

	//TODO why static?
	public static int current_monster_action_mode;

	private static string[] MonsterFirmName =
	{
		"Deezboanz Lair", "Rattus Lair", "Broosken Lair", "Haubudam Lair",
		"Pfith Lair", "Rokken Lair", "Doink Lair", "Wyrm Lair", "Droog Lair", "Ick Lair", "Sauroid Lair",
		"Karrotten Lair", "Holgh Lair"
	};

	private MonsterRes MonsterRes => Sys.Instance.MonsterRes;
	private SiteArray SiteArray => Sys.Instance.SiteArray;

	public FirmMonster()
	{
	}

	protected override void init_derived()
	{
		monster_king = new MonsterInFirm();

		defending_king_count = 0;
		defending_general_count = 0;
		defending_soldier_count = 0;

		monster_nation_relation = 0;

		//----------------------------------------//
		// Set monster aggressiveness. It affects:
		//
		// -the number of defenders will be called out at one time.
		//----------------------------------------//

		defend_target_recno = 0;

		//-- these vars must be initialized here instead of in FirmMonster::FirmMonster() for random seed sync during load game --//

		monster_aggressiveness = 20 + Misc.Random(50); // 20 to 70
	}

	protected override void deinit_derived()
	{
		//-------- mobilize all monsters in the firm --------//

		if (monster_king.monster_id != 0)
			mobilize_king();

		while (monsterGenerals.Count > 0)
		{
			if (mobilize_general(1) == 0)
				break;
		}

		clear_defense_mode();

		int goldAmount = 800 * (MonsterRes[monster_id].level * 30 + Misc.Random(50)) / 100;

		SiteArray.AddSite(center_x, center_y, Site.SITE_GOLD_COIN, goldAmount);
		SiteArray.OrderAIUnitsToGetSites(); // ask AI units to get the gold coins
	}

	public override string firm_name()
	{
		return MonsterFirmName[monster_id - 1];
	}

	public int total_defender()
	{
		return defending_king_count + defending_general_count + defending_soldier_count;
	}

	public override void next_day()
	{
		validate_patrol_unit();

		//---- the monster boss recruit new monsters ----//

		if (Info.TotalDays % 10 == firm_recno % 10)
			recruit_soldier();

		//----- monsters recover hit points -------//

		if (Info.TotalDays % 15 == firm_recno % 15) // once a week
			recover_hit_points();

		//------ monster thinks about expansion -------//

		if (Config.monster_type == Config.OPTION_MONSTER_OFFENSIVE)
		{
			if (Info.TotalDays % 30 == firm_recno % 30 && Misc.Random(3) == 0)
				recruit_general();
			/*
			if( info.game_date%90 == firm_recno%90 )
				think_attack_neighbor();
			*/

			//------ attack human towns and firms randomly -----//

			// only start attacking 3 years after the game starts so the human can build up things
			if (Info.game_date > Info.game_start_date.AddDays(1000.0) &&
			    Info.TotalDays % 30 == firm_recno % 30 && fryhtan_random_attack())
			{
				think_attack_human();
			}

			//--------- think expansion ---------//

			// it will expand slower when there are already a lot of the monster structures on the map
			if (Info.TotalDays % 180 == firm_recno % 180 &&
			    Misc.Random(FirmRes[FIRM_MONSTER].total_firm_count * 10) == 0)
			{
				think_expansion();
			}
		}
	}

	public override void process_ai()
	{
	}

	public override void assign_unit(int unitRecno)
	{
		UnitMonster unit = (UnitMonster)UnitArray[unitRecno];

		switch (unit.Rank)
		{
			case Unit.RANK_KING:
				set_king(unit.MonsterId, unit.Skill.combat_level);
				break;

			case Unit.RANK_GENERAL:
				add_general(unitRecno);
				break;

			case Unit.RANK_SOLDIER:
				add_soldier(unit.LeaderId);
				break;
		}

		//--------- the unit disappear in firm -----//

		UnitArray.disappear_in_firm(unitRecno);
	}

	public bool can_assign_monster(int unitRecno)
	{
		UnitMonster unit = (UnitMonster)UnitArray[unitRecno];

		// can assign if the build code are the same
		return FirmRes.get_build(firm_build_id).build_code == MonsterRes[unit.MonsterId].firm_build_code;
	}

	public void set_king(int monsterId, int combatLevel)
	{
		monster_king.monster_id = monsterId;
		monster_king.set_combat_level(combatLevel);
		monster_king.hit_points = monster_king.max_hit_points;

		//TODO drawing
		//if( FirmArray.selected_recno == firm_recno )
		//sys.need_redraw_flag = 1;
	}

	public void add_general(int generalUnitRecno)
	{
		if (monsterGenerals.Count >= GameConstants.MAX_MONSTER_GENERAL_IN_FIRM)
			return;

		UnitMonster unitPtr = (UnitMonster)UnitArray[generalUnitRecno];

		MonsterInFirm monsterInFirm = new MonsterInFirm();

		// contribution is used for storing the monster id. temporary
		monsterInFirm.monster_id = unitPtr.MonsterId;
		monsterInFirm.set_combat_level(unitPtr.Skill.combat_level);
		monsterInFirm.hit_points = (int)unitPtr.HitPoints;

		if (monsterInFirm.hit_points == 0)
			monsterInFirm.hit_points = 1;

		// total_reward is used for storing the soldier monster id temporarily
		monsterInFirm.soldier_monster_id = unitPtr.MonsterSoldierId;
		monsterInFirm.soldier_count = 0;

		monsterInFirm.mobile_unit_recno = generalUnitRecno; // unit recno of this monster when it is a mobile unit
		// this is only used as a reference for soldiers to find their leaders
		monsterGenerals.Add(monsterInFirm);

		//----- check if there are any soldiers waiting for this general ----//
		//
		// These are soldiers who follow the general to go out to defend
		// against the attack but then went back to the firm sooner
		// than the general does.
		//
		//-------------------------------------------------------------------//

		for (int i = waitingSoldiers.Count - 1; i >= 0; i--)
		{
			//--- if this waiting soldier was led by this general ---//

			if (waitingSoldiers[i] == generalUnitRecno)
			{
				monsterInFirm.soldier_count++;

				waitingSoldiers.RemoveAt(i);
			}
		}

		//TODO drawing
		//if( FirmArray.selected_recno == firm_recno )
		//sys.need_redraw_flag = 1;
	}

	public void add_soldier(int generalUnitRecno)
	{
		//----- check if the soldier's leading general is here ----//

		for (int i = 0; i < monsterGenerals.Count; i++)
		{
			if (monsterGenerals[i].mobile_unit_recno == generalUnitRecno)
			{
				if (monsterGenerals[i].soldier_count >= GameConstants.MAX_SOLDIER_PER_GENERAL)
					return;

				monsterGenerals[i].soldier_count++;
				return;
			}
		}

		//---- if not, put the soldier into the waiting list ----//

		waitingSoldiers.Add(generalUnitRecno);
	}

	public int recruit_general(int soldierCount = -1)
	{
		if (monsterGenerals.Count >= GameConstants.MAX_MONSTER_GENERAL_IN_FIRM * monster_aggressiveness / 100)
			return 0;

		if (monster_king.monster_id == 0)
			return 0;

		//---------- recruit the general now ----------//

		MonsterInFirm monsterInFirm = new MonsterInFirm();

		int combatLevel = 40 + Misc.Random(30); // 40 to 70

		monsterInFirm.monster_id = monster_king.monster_id;
		monsterInFirm.set_combat_level(combatLevel);
		monsterInFirm.hit_points = monsterInFirm.max_hit_points;

		monsterInFirm.soldier_monster_id = monster_king.monster_id;

		if (soldierCount >= 0)
			monsterInFirm.soldier_count = soldierCount;
		else
			monsterInFirm.soldier_count = Misc.Random(GameConstants.MAX_SOLDIER_PER_GENERAL / 2) + 1;

		monsterGenerals.Add(monsterInFirm);

		//TODO drawing
		//if (FirmArray.selected_recno == firm_recno)
		//sys.need_redraw_flag = 1;

		return 1;
	}

	public void recruit_soldier()
	{
		for (int i = 0; i < monsterGenerals.Count; i++)
		{
			MonsterInFirm monsterInFirm = monsterGenerals[i];
			// 2/3 chance of recruiting a soldier
			if (monsterInFirm.soldier_count < GameConstants.MAX_SOLDIER_PER_GENERAL && Misc.Random(3) > 0)
			{
				monsterInFirm.soldier_count++;
			}
		}
	}

	public bool mobilize_king()
	{
		if (mobilize_monster(monster_king.monster_id, Unit.RANK_KING,
			    monster_king.combat_level, monster_king.hit_points) == 0)
			return false;

		monster_king.monster_id = 0;

		//TODO drawing
		//if( FirmArray.selected_recno == firm_recno )
		//sys.need_redraw_flag = 1;

		return true;
	}

	public int mobilize_general(int generalId, int mobilizeSoldier = 1)
	{
		MonsterInFirm monsterInFirm = monsterGenerals[generalId - 1];

		//------ mobilize the monster general ------//

		int generalUnitRecno = mobilize_monster(monsterInFirm.monster_id, Unit.RANK_GENERAL,
			monsterInFirm.combat_level, monsterInFirm.hit_points);

		if (generalUnitRecno == 0)
			return 0;

		UnitMonster generalUnit = (UnitMonster)UnitArray[generalUnitRecno];
		generalUnit.TeamId = UnitArray.cur_team_id;
		generalUnit.MonsterSoldierId = monsterInFirm.soldier_monster_id;

		int mobilizedCount = 1;

		patrolUnits.Add(generalUnitRecno);

		//------ mobilize soldiers commanded by the monster general ------//

		if (mobilizeSoldier != 0)
		{
			for (int i = 0; i < monsterInFirm.soldier_count; i++)
			{
				//--- the combat level of its soldiers ranges from 25% to 50% of the combat level of the general ---//

				int soldierCombatLevel =
					monsterInFirm.combat_level / GameConstants.MONSTER_SOLDIER_COMBAT_LEVEL_DIVIDER +
					Misc.Random(monsterInFirm.combat_level / GameConstants.MONSTER_SOLDIER_COMBAT_LEVEL_DIVIDER);

				int unitRecno = mobilize_monster(monsterInFirm.soldier_monster_id, Unit.RANK_SOLDIER,
					soldierCombatLevel);

				if (unitRecno != 0)
				{
					UnitArray[unitRecno].TeamId = UnitArray.cur_team_id;
					UnitArray[unitRecno].LeaderId = generalUnitRecno;
					mobilizedCount++;

					patrolUnits.Add(unitRecno);

					if (patrolUnits.Count == TeamInfo.MAX_TEAM_MEMBER)
						break;
				}
				else
				{
					break; // no space for init_sprite
				}
			}
		}

		//------- set the team_info of the general -------//

		generalUnit.TeamInfo.Members.Clear();

		for (int i = 0; i < patrolUnits.Count; i++)
		{
			generalUnit.TeamInfo.Members.Add(patrolUnits[i]);
		}

		//---- delete the monster general record from the array ----//

		monsterGenerals.Remove(monsterInFirm);

		UnitArray.cur_team_id++;

		//TODO drawing
		//if( FirmArray.selected_recno == firm_recno )
		//sys.need_redraw_flag = 1;

		return mobilizedCount;
	}

	public int total_combat_level()
	{
		int totalCombatLevel = 50; // for the structure 

		for (int i = 0; i < monsterGenerals.Count; i++)
		{
			MonsterInFirm monsterInFirm = monsterGenerals[i];
			// *2 because total_combat_level() actually takes hit_points instead of combat_level
			// *3/2 because the function use 100% + random(100%) for the combat level, so we take 150% as the average
			totalCombatLevel += monsterInFirm.hit_points +
			                    (monsterInFirm.combat_level * 2 / GameConstants.MONSTER_SOLDIER_COMBAT_LEVEL_DIVIDER)
			                    * 3 / 2 * monsterInFirm.soldier_count;
		}

		return totalCombatLevel;
	}

	public override void being_attacked(int attackerUnitRecno)
	{
		int attackerNationRecno = UnitArray[attackerUnitRecno].NationId;

		//--- increase reputation of the nation that attacks monsters ---//

		if (attackerNationRecno != 0)
		{
			NationArray[attackerNationRecno].change_reputation(GameConstants.REPUTATION_INCREASE_PER_ATTACK_MONSTER);
			set_hostile_nation(attackerNationRecno); // also set hostile with the nation
		}

		//------ MAX no. of defender it should call out -----//

		int maxDefender = GameConstants.MAX_MONSTER_IN_FIRM * monster_aggressiveness / 200;

		if (total_defender() >= maxDefender) // only mobilize new ones when the MAX defender no. has been reached yet
			return;

		current_monster_action_mode = UnitConstants.MONSTER_ACTION_DEFENSE;

		//---- mobilize monster general to defend against the attack ----//

		if (monsterGenerals.Count > 0)
		{
			while (total_defender() < maxDefender)
			{
				if (monsterGenerals.Count == 0)
					break;

				int mobilizedCount = mobilize_general(Misc.Random(monsterGenerals.Count) + 1);

				if (mobilizedCount > 0)
				{
					defending_general_count++;
					defending_soldier_count += mobilizedCount - 1;
				}
				else
				{
					break;
				}
			}
		}

		else if (monster_king.monster_id != 0)
		{
			if (mobilize_king())
				defending_king_count++;
		}

		defend_target_recno = attackerUnitRecno;
	}

	public void clear_defense_mode()
	{
		//------------------------------------------------------------------//
		// change defense unit's to non-defense mode
		//------------------------------------------------------------------//

		foreach (Unit unit in UnitArray)
		{
			if (UnitArray.IsDeleted(unit.SpriteId))
				continue;

			//------ reset the monster's defense mode -----//

			if (unit.in_monster_defend_mode() &&
			    unit.ActionMisc == UnitConstants.ACTION_MISC_MONSTER_DEFEND_FIRM_RECNO &&
			    unit.ActionMiscParam == firm_recno)
			{
				unit.clear_monster_defend_mode();
				((UnitMonster)unit).set_monster_action_mode(UnitConstants.MONSTER_ACTION_STOP);
			}

			//--- if this unit belongs to this firm, reset its association with this firm ---//

			if (unit.UnitMode == UnitConstants.UNIT_MODE_MONSTER && unit.UnitModeParam == firm_recno)
			{
				unit.UnitModeParam = 0;
			}
		}
	}

	public void reduce_defender_count(int rankId)
	{
		switch (rankId)
		{
			case Unit.RANK_KING:
				defending_king_count--;
				if (defending_king_count < 0) //**BUGHERE
					defending_king_count = 0;
				break;

			case Unit.RANK_GENERAL:
				defending_general_count--;
				if (defending_general_count < 0) //**BUGHERE
					defending_general_count = 0;
				break;

			case Unit.RANK_SOLDIER:
				defending_soldier_count--;
				if (defending_soldier_count < 0) //**BUGHERE
					defending_soldier_count = 0;
				break;
		}

		if (total_defender() == 0)
			monster_nation_relation = 0;
	}

	public void set_hostile_nation(int nationRecno)
	{
		if (nationRecno == 0)
			return;

		//TODO check
		//err_when(nationRecno>7); // only 8 bits
		monster_nation_relation |= (0x1 << nationRecno);
	}

	public void reset_hostile_nation(int nationRecno)
	{
		if (nationRecno == 0)
			return;

		//TODO check
		//err_when(nationRecno>7); // only 8 bits
		monster_nation_relation &= ~(0x1 << nationRecno);
	}

	public bool is_hostile_nation(int nationRecno)
	{
		if (nationRecno == 0)
			return false;

		//TODO check
		//err_when(nationRecno>7); // only 8 bits
		return (monster_nation_relation & (0x1 << nationRecno)) != 0;
	}

	private void validate_patrol_unit()
	{
		if (patrolUnits.Count == 0)
			return;

		for (int i = patrolUnits.Count - 1; i >= 0; i--)
		{
			int unitRecno = patrolUnits[i];

			bool isDeleted = UnitArray.IsDeleted(unitRecno);
			bool isVisible = !isDeleted && UnitArray[unitRecno].IsVisible();

			if (isDeleted || !isVisible)
			{
				patrolUnits.RemoveAt(i);
			}
		}

		if (patrolUnits.Count == 0 && total_defender() == 0)
			monster_nation_relation = 0;
	}

	private void recover_hit_points()
	{
		//------ recover the king's hit points -------//

		if (monster_king.hit_points < monster_king.max_hit_points)
			monster_king.hit_points++;

		//------ recover the generals' hit points -------//

		for (int i = 0; i < monsterGenerals.Count; i++)
		{
			if (monsterGenerals[i].hit_points < monsterGenerals[i].max_hit_points)
				monsterGenerals[i].hit_points++;
		}
	}

	private int mobilize_monster(int monsterId, int rankId, int combatLevel, int hitPoints = 0)
	{
		MonsterInfo monsterInfo = MonsterRes[monsterId];
		UnitInfo unitInfo = UnitRes[monsterInfo.unit_id];

		//------- locate a space first --------//

		int xLoc = center_x, yLoc = center_y;
		SpriteInfo spriteInfo = SpriteRes[unitInfo.sprite_id];

		if (!World.LocateSpace(ref xLoc, ref yLoc, xLoc, yLoc,
			    spriteInfo.LocWidth, spriteInfo.LocHeight, unitInfo.mobile_type))
		{
			return 0;
		}

		//---------- add the unit now -----------//

		Unit unit = UnitArray.AddUnit(unitInfo.unit_id, 0, rankId, 0, xLoc, yLoc);

		UnitMonster monster = (UnitMonster)unit;

		monster.SetMode(UnitConstants.UNIT_MODE_MONSTER, firm_recno);
		monster.SetCombatLevel(combatLevel);
		monster.MonsterId = monsterId;
		monster.set_monster_action_mode(current_monster_action_mode);

		if (hitPoints != 0)
			monster.HitPoints = hitPoints;
		else
			monster.HitPoints = monster.MaxHitPoints;

		//-----------------------------------------------------//
		// enable unit defend mode
		//-----------------------------------------------------//
		if (firm_recno != 0) // 0 when firm is ready to be deleted
		{
			monster.Stop2();
			monster.ActionMode2 = UnitConstants.ACTION_MONSTER_DEFEND_DETECT_TARGET;
			monster.ActionPara2 = UnitConstants.MONSTER_DEFEND_DETECT_COUNT;

			monster.ActionMisc = UnitConstants.ACTION_MISC_MONSTER_DEFEND_FIRM_RECNO;
			monster.ActionMiscParam = firm_recno;
		}

		return monster.SpriteId;
	}

	private int think_attack_neighbor()
	{
		//-- don't attack new target if some mobile monsters are already attacking somebody --//

		if (patrolUnits.Count > 0)
			return 0;

		//-------- only attack if we have enough generals ---------//

		int generalCount = 0;

		//--- count the number of generals commanding at least 5 soldiers ---//

		for (int i = 0; i < monsterGenerals.Count; i++)
		{
			MonsterInFirm monsterInFirm = monsterGenerals[i];
			if (monsterInFirm.soldier_count >= 5)
				generalCount++;
		}

		if (generalCount <= 1) // don't attack if there is only one general in the firm
			return 0;

		//------ look for neighbors to attack ------//

		int xOffset = 0, yOffset = 0;
		int xLoc = center_x, yLoc = center_y;
		int attackFlag = 0;
		FirmInfo firmInfo = FirmRes[firm_id];

		int scanLocWidth = GameConstants.MONSTER_ATTACK_NEIGHBOR_RANGE * 2;
		int scanLocHeight = GameConstants.MONSTER_ATTACK_NEIGHBOR_RANGE * 2;
		int scanLimit = scanLocWidth * scanLocHeight;
		int targetNation = 0;

		for (int i = firmInfo.loc_width * firmInfo.loc_height + 1; i <= scanLimit; i++)
		{
			Misc.cal_move_around_a_point(i, scanLocWidth, scanLocHeight, out xOffset, out yOffset);

			xLoc = center_x + xOffset;
			yLoc = center_y + yOffset;

			xLoc = Math.Max(0, xLoc);
			xLoc = Math.Min(GameConstants.MapSize - 1, xLoc);

			yLoc = Math.Max(0, yLoc);
			yLoc = Math.Min(GameConstants.MapSize - 1, yLoc);

			Location location = World.GetLoc(xLoc, yLoc);

			if (location.IsFirm())
			{
				Firm firm = FirmArray[location.FirmId()];
				if (firm.nation_recno != 0)
				{
					targetNation = firm.nation_recno;
					attackFlag = 1;
					xLoc = firm.loc_x1;
					yLoc = firm.loc_y1;
					break;
				}
			}
			else if (location.IsTown())
			{
				Town town = TownArray[location.TownId()];
				if (town.NationId != 0)
				{
					targetNation = town.NationId;
					attackFlag = 1;
					xLoc = town.LocX1;
					yLoc = town.LocY1;
					break;
				}
			}
		}

		if (attackFlag == 0)
			return 0;

		//------- attack the civilian now --------//
		current_monster_action_mode = UnitConstants.MONSTER_ACTION_ATTACK;

		mobilize_general(Misc.Random(monsterGenerals.Count) + 1);

		if (patrolUnits.Count > 0)
		{
			set_hostile_nation(targetNation);
			UnitArray.attack(xLoc, yLoc, false, patrolUnits, InternalConstants.COMMAND_AI, 0);
			return 1;
		}
		else
		{
			return 0;
		}
	}

	private int think_attack_human()
	{
		//-- don't attack new target if some mobile monsters are already attacking somebody --//

		if (patrolUnits.Count > 0)
			return 0;

		//-------- only attack if we have enough generals ---------//

		int generalCount = 0;

		//--- count the number of generals commanding at least 5 soldiers ---//

		for (int i = 0; i < monsterGenerals.Count; i++)
		{
			MonsterInFirm monsterInFirm = monsterGenerals[i];
			if (monsterInFirm.soldier_count >= 5)
				generalCount++;
		}

		if (generalCount <= 1) // don't attack if there is only one general in the firm
			return 0;

		Info.set_rank_data(false);
		int totalScore = 0;
		//------ the more score the player has, the more often mosters will attack him ------//
		foreach (Nation nation in NationArray)
		{
			int nationScore = Info.get_total_score(nation.nation_recno);
			totalScore += nationScore;
		}

		int randomValue = Misc.Random(totalScore) + 1;

		totalScore = 0;
		Nation targetNation = null;
		foreach (Nation nation in NationArray)
		{
			int nationScore = Info.get_total_score(nation.nation_recno);
			totalScore += nationScore;
			if (randomValue <= totalScore)
			{
				targetNation = nation;
				break;
			}
		}

		if (targetNation == null)
			return 0;

		int targetXLoc = -1, targetYLoc = -1, targetNationRecno = targetNation.nation_recno;
		List<Firm> targetFirms = new List<Firm>();
		foreach (Firm firm in FirmArray)
		{
			if (firm.nation_recno != targetNationRecno || firm.region_id != region_id)
				continue;

			targetFirms.Add(firm);
		}

		if (targetFirms.Count > 0)
		{
			int selectedFirmIndex = Misc.Random(targetFirms.Count);
			Firm selectedFirm = targetFirms[selectedFirmIndex];
			targetXLoc = selectedFirm.loc_x1;
			targetYLoc = selectedFirm.loc_y1;
		}

		//------ look for neighbors to attack ------//

		/*int   firmRecno, townRecno;
		int   targetXLoc= -1, targetYLoc, targetNationRecno=0;
		Firm* firmPtr;
		Town* townPtr;
	
		for(i=1 ; i<100 ; i++ )
		{
			//----- randomly pick a firm ------//
	
			firmRecno = misc.random(firm_array.size()) + 1;
	
			if( firm_array.is_deleted(firmRecno) )
				continue;
	
			firmPtr = firm_array[firmRecno];
	
			if( firmPtr.region_id == region_id )
			{
				targetXLoc = firmPtr.loc_x1;
				targetYLoc = firmPtr.loc_y1;
				targetNationRecno = firmPtr.nation_recno;
				break;
			}
	
			//----- randomly pick a town ------//
	
			townRecno = misc.random(town_array.size()) + 1;
	
			if( town_array.is_deleted(townRecno) )
				continue;
	
			townPtr = town_array[townRecno];
	
			if( townPtr.nation_recno && townPtr.region_id == region_id )
			{
				targetXLoc = townPtr.loc_x1;
				targetYLoc = townPtr.loc_y1;
				targetNationRecno = townPtr.nation_recno;
				break;
			}
		}*/

		if (targetXLoc == -1) // no target selected
			return 0;

		//------- attack the civilian now --------//

		current_monster_action_mode = UnitConstants.MONSTER_ACTION_ATTACK;

		mobilize_general(Misc.Random(monsterGenerals.Count) + 1);

		if (patrolUnits.Count > 0)
		{
			set_hostile_nation(targetNationRecno);
			UnitArray.attack(targetXLoc, targetYLoc, false, patrolUnits, InternalConstants.COMMAND_AI, 0);
			return 1;
		}
		else
		{
			return 0;
		}
	}

	private int think_expansion()
	{
		if (patrolUnits.Count > 0)
			return 0;

		if (monster_king.monster_id == 0 || monsterGenerals.Count < GameConstants.MIN_GENERAL_EXPAND_NUM)
			return 0;

		//--- count the number of generals commanding 8 soldiers ----//

		int generalCount = 0;

		for (int i = 0; i < monsterGenerals.Count; i++)
		{
			MonsterInFirm monsterInFirm = monsterGenerals[i];
			if (monsterInFirm.soldier_count == GameConstants.MAX_SOLDIER_PER_GENERAL)
				generalCount++;
		}

		if (generalCount < GameConstants.MIN_GENERAL_EXPAND_NUM) // don't expand if the no. of generals is less than 3
			return 0;

		//------------- locate space to build monster firm randomly -------------//

		MonsterInfo monsterInfo = MonsterRes[monster_king.monster_id];
		FirmInfo firmInfo = FirmRes[FIRM_MONSTER];
		int teraMask = UnitRes.mobile_type_to_mask(UnitConstants.UNIT_LAND);
		int xLoc1 = Math.Max(0, loc_x1 - GameConstants.EXPAND_FIRM_DISTANCE);
		int yLoc1 = Math.Max(0, loc_y1 - GameConstants.EXPAND_FIRM_DISTANCE);
		int xLoc2 = Math.Min(GameConstants.MapSize - 1, loc_x2 + GameConstants.EXPAND_FIRM_DISTANCE);
		int yLoc2 = Math.Min(GameConstants.MapSize - 1, loc_y2 + GameConstants.EXPAND_FIRM_DISTANCE);

		if (!World.LocateSpaceRandom(ref xLoc1, ref yLoc1, xLoc2, yLoc2,
			    firmInfo.loc_width + GameConstants.FREE_SPACE_DISTANCE * 2,
			    firmInfo.loc_height + GameConstants.FREE_SPACE_DISTANCE * 2, // leave at least 3 location space around the building
			    (xLoc2 - xLoc1 + 1) * (yLoc2 - yLoc1 + 1), 0, true, teraMask))
		{
			return 0;
		}

		monsterGenerals.RemoveAt(monsterGenerals.Count - 1);
		//monsterInFirm = monster_general_array + monster_general_count;
		//unit_array.disappear_in_firm(monsterInFirm.mobile_unit_recno);

		return monsterInfo.build_firm_monster(xLoc1 + GameConstants.FREE_SPACE_DISTANCE,
			yLoc1 + GameConstants.FREE_SPACE_DISTANCE, 1);
	}

	private static int fryhtan_attacks_per_six_months(int numOfLairs)
	{
		// This algorithm encodes the continuation of the following table:
		//
		// # Lairs     | 0   5   10  15  20  30  40  50  60  70  80  95  110 ...
		// -----------------------------------------------------------------------
		// # attacks   | 1   2   3   4   5   6   7   8   9   10  11  12  13  ...
		//
		//                      4 x +5            6 x +10                  8 x +15 
		//
		// The plateaus in this table are given by
		//   x0 = 0
		//   x1 = x0 + 4 * 5    = 20
		//   x2 = x1 + 6 * 10   = 80
		//   x3 = x2 + 8 * 15   = 200
		//   ...
		// 
		int attacks = 1;
		int plateauThreshold = 0;
		int plateauLength = 4;
		int plateau = 5;

		while (plateauThreshold < numOfLairs)
		{
			int currentPlateau = numOfLairs - plateauThreshold;
			plateauThreshold += plateauLength * plateau;
			attacks += Math.Min(currentPlateau, plateauThreshold) / plateau;
			plateauLength += 2;
			plateau += 5;
		}

		return attacks;
	}

	private bool fryhtan_random_attack()
	{
		if (ConfigAdv.monster_alternate_attack_curve)
		{
			// roll of 1/(lairs*6)
			int chance = Misc.Random(Sys.Instance.FirmRes[FIRM_MONSTER].total_firm_count * 6);
			// From the desired average number of attacks per 6 months, we want the attack chance per lair (which is per month):
			//   Lairs * P(Attack) * 6 = Desired      <=>    P(Attack) = Desired / 6 / Lairs
			// Note that Desired < (6 * Lairs). We can use: rand(6 * Lairs) < Desired to simulate this chance.
			return chance < fryhtan_attacks_per_six_months(Sys.Instance.FirmRes[FIRM_MONSTER].total_firm_count);
		}

		// original probability 1/6
		return Misc.Random(Sys.Instance.FirmRes[FIRM_MONSTER].total_firm_count * ConfigAdv.monster_attack_divisor) == 0;
	}
	
	public override void DrawDetails(IRenderer renderer)
	{
		renderer.DrawMonsterLairDetails(this);
	}

	public override void HandleDetailsInput(IRenderer renderer)
	{
		renderer.HandleMonsterLairDetailsInput(this);
	}
}