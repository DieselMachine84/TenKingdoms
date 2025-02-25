using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class Unit : Sprite
{
	public const int RANK_SOLDIER = 0;
	public const int RANK_GENERAL = 1;
	public const int RANK_KING = 2;

	public int unit_id;
	public int rank_id;
	public int race_id;
	public int nation_recno;

	public bool ai_unit;
	public int name_id; // id. of the unit's name in RaceRes::first_name_array;

	public int unit_group_id; // the group id this unit belong to if it is selected
	public int team_id; // id. of defined team
	public bool selected_flag; // whether the unit has been selected or not
	public int group_select_id; // id for group selection

	public int waiting_term; // for 2x2 unit only, the term to wait before recalling A* to get a new path
	public int blocked_by_member;
	public int swapping;

	public int leader_unit_recno; // recno of this unit's leader

	//--------- action vars ------------//
	public int action_misc;
	public int action_misc_para;

	public int action_mode;
	public int action_para;
	public int action_x_loc;
	public int action_y_loc;

	public int action_mode2; // store the existing action for speeding up the performance if same action is ordered.
	public int action_para2; // to re-activiate the unit if its cur_action is idle
	public int action_x_loc2;
	public int action_y_loc2;

	public byte[] blocked_edge = new byte[4]; // for calling searching in attacking
	public int attack_dir;

	//------------ attack parameters -----------//

	public int range_attack_x_loc; // -1 for unable to do range_attack, use to store previous range attack location
	public int range_attack_y_loc; // -1 for unable to do range_attack, use to store previous range attack location

	//------------- for unit movement ---------------//

	public int move_to_x_loc; // the location the unit should be moving to
	public int move_to_y_loc;

	//---------- game vars -------------//

	public int loyalty;
	public int target_loyalty;

	public double hit_points;
	public int max_hit_points;

	public Skill skill = new Skill();

	public int unit_mode;
	public int unit_mode_para; // if unit_mode==UNIT_MODE_REBEL, unit_mode_para is rebel_recno this unit belongs to

	public int rebel_recno()
	{
		return unit_mode == UnitConstants.UNIT_MODE_REBEL ? unit_mode_para : 0;
	}

	public int spy_recno; // spy parameters

	public int nation_contribution; // For humans: contribution to the nation. For weapons: the tech level!
	public int total_reward; // total amount of reward you have given to the unit

	private static bool move_action_call_flag;
	private static bool idle_detect_has_unit;
	private static bool idle_detect_has_firm;
	private static bool idle_detect_has_town;
	private static bool idle_detect_has_wall;
	private static int idle_detect_target_unit_recno;
	private static int idle_detect_target_firm_recno;
	private static int idle_detect_target_town_recno;
	private static int idle_detect_target_wall_x1;
	private static int idle_detect_target_wall_y1;
	private static int cycle_wait_unit_index;
	private static int[] cycle_wait_unit_array;
	private static int cycle_wait_unit_array_def_size;
	private static int cycle_wait_unit_array_multipler;

	private static int help_mode;
	private static int help_attack_target_recno;

	public int commander_power()
	{
		//---- if the commander is in a military camp -----//

		int commanderPower = 0;

		if (unit_mode == UnitConstants.UNIT_MODE_OVERSEE)
		{
			Firm firm = FirmArray[unit_mode_para];

			if (firm.firm_id == Firm.FIRM_CAMP)
			{
				for (int i = firm.linked_town_array.Count - 1; i >= 0; i--)
				{
					if (firm.linked_town_enable_array[i] == InternalConstants.LINK_EE)
					{
						Town town = TownArray[firm.linked_town_array[i]];

						commanderPower += town.Population / town.LinkedActiveCampCount();
					}
				}

				commanderPower += firm.workers.Count * 3; // 0 to 24
			}
			else if (firm.firm_id == Firm.FIRM_BASE)
			{
				commanderPower = 60;
			}
		}
		else
		{
			commanderPower = team_info.member_unit_array.Count * 3; // 0 to 24
		}

		return commanderPower;
	}

	//---- share the use of nation_contribution and total_reward ----//

	public int get_monster_id()
	{
		return nation_contribution;
	}

	public void set_monster_id(int monsterId)
	{
		nation_contribution = monsterId;
	}

	public int get_monster_soldier_id()
	{
		return total_reward;
	}

	public void set_monster_soldier_id(int monsterSoldierId)
	{
		total_reward = monsterSoldierId;
	}

	public int get_weapon_version()
	{
		return nation_contribution;
	}

	public void set_weapon_version(int weaponVersion)
	{
		nation_contribution = weaponVersion;
	}

	public int unit_power()
	{
		UnitInfo unitInfo = UnitRes[unit_id];

		if (unitInfo.unit_class == UnitConstants.UNIT_CLASS_WEAPON)
		{
			return (int)hit_points + (unitInfo.weapon_power + get_weapon_version() - 1) * 15;
		}
		else
		{
			return (int)hit_points;
		}
	}

	//------- attack parameters --------//

	public AttackInfo[] attack_info_array;
	public int attack_count;
	public int attack_range;
	public int cur_power; // power for power attack
	public int max_power;

	//------- path seeking vars --------//

	public ResultNode[] result_node_array;
	public int result_node_count;
	public int result_node_recno;
	public int result_path_dist;

	//----------- way points -----------//
	private List<ResultNode> wayPoints = new List<ResultNode>();

	//--------- AI parameters ------------//

	public int ai_action_id; // an unique id. for locating the AI action node this unit belongs to in Nation::action_array

	public int original_action_mode;
	public int original_action_para;
	public int original_action_x_loc;
	public int original_action_y_loc;

	// the original location of the attacking target when the attack() function is called
	// action_x_loc2 & action_y_loc2 will change when the unit move, but these two will not.
	public int original_target_x_loc;
	public int original_target_y_loc;

	public int ai_original_target_x_loc; // for AI only
	public int ai_original_target_y_loc;

	public bool ai_no_suitable_action;

	//-------- defense blocking ability flag ----------//

	public int can_guard_flag; // bit0= standing guard, bit1=moving guard

	// copy from sprite_info.can_guard_flag when skill.combat level is high enough
	public bool can_attack_flag; // 1 able to attack, 0 unable to attack no matter what attack_count is
	public bool force_move_flag;

	public int home_camp_firm_recno;

	public int aggressive_mode;

	public int ignore_power_nation;

	//------ TeamInfo structure for general and king only ------//

	public TeamInfo team_info;

	public int commanded_soldier_count()
	{
		if (rank_id != RANK_GENERAL && rank_id != RANK_KING)
			return 0;

		//--------------------------------------//

		int soldierCount = 0;

		if (is_visible())
		{
			soldierCount = team_info.member_unit_array.Count - 1;

			if (soldierCount < 0) // member_count can be 0
				soldierCount = 0;
		}
		else
		{
			if (unit_mode == UnitConstants.UNIT_MODE_OVERSEE)
			{
				Firm firm = FirmArray[unit_mode_para];

				if (firm.firm_id == Firm.FIRM_CAMP) // it can be an overseer of a seat of powre
					soldierCount = firm.workers.Count;
			}
		}

		return soldierCount;
	}

	// whether the unit is visible on the map, it is not invisable if cur_x == -1
	public bool is_visible()
	{
		return cur_x >= 0;
	}

	public virtual string unit_name(int withTitle = 1)
	{
		UnitInfo unitInfo = UnitRes[unit_id];

		//------------------------------------//
		string str = String.Empty;

		if (race_id != 0)
		{
			if (withTitle != 0)
			{
				if (unit_mode == UnitConstants.UNIT_MODE_REBEL)
				{
					if (rank_id == RANK_GENERAL)
					{
						str = "Rebel Leader ";
					}
				}
				else
				{
					if (rank_id == RANK_KING)
					{
						str = "King ";
					}
					else if (rank_id == RANK_GENERAL)
					{
						str = "General ";
					}
				}
			}

			if (rank_id == RANK_KING) // use the player name
				str += NationArray[nation_recno].king_name();
			else
				str += RaceRes[race_id].get_name(name_id);
		}
		else
		{
			str = unitInfo.name;

			//--- for weapons, the rank_id is used to store the version of the weapon ---//

			if (unitInfo.unit_class == UnitConstants.UNIT_CLASS_WEAPON && get_weapon_version() > 1)
			{
				str += " ";
				str += Misc.roman_number(get_weapon_version());
			}

			if (unitInfo.unit_class != UnitConstants.UNIT_CLASS_GOD) // God doesn't have any series no.
			{
				str += " ";
				str += name_id; // name id is the series no. of the unit
			}
		}

		return str;
	}

	public int region_id()
	{
		if (is_visible())
		{
			return World.get_region_id(next_x_loc(), next_y_loc());
		}
		else
		{
			if (unit_mode == UnitConstants.UNIT_MODE_OVERSEE || unit_mode == UnitConstants.UNIT_MODE_CONSTRUCT)
				return FirmArray[unit_mode_para].region_id;
		}

		return 0;
	}

	protected ConfigAdv ConfigAdv => Sys.Instance.ConfigAdv;
	protected Info Info => Sys.Instance.Info;
	protected Power Power => Sys.Instance.Power;
	private SeekPath SeekPath => Sys.Instance.SeekPath;
	protected TerrainRes TerrainRes => Sys.Instance.TerrainRes;
	protected FirmRes FirmRes => Sys.Instance.FirmRes;
	protected RaceRes RaceRes => Sys.Instance.RaceRes;
	protected UnitRes UnitRes => Sys.Instance.UnitRes;
	protected RegionArray RegionArray => Sys.Instance.RegionArray;
	protected BulletArray BulletArray => Sys.Instance.BulletArray;
	protected WarPointArray WarPointArray => Sys.Instance.WarPointArray;
	protected RebelArray RebelArray => Sys.Instance.RebelArray;
	protected SpyArray SpyArray => Sys.Instance.SpyArray;
	protected NewsArray NewsArray => Sys.Instance.NewsArray;
	protected EffectArray EffectArray => Sys.Instance.EffectArray;

	public Unit()
	{
	}

	//------- derived functions from Sprite ------//

	public virtual void init(int unitId, int nationRecno, int rankId = 0, int unitLoyalty = 0,
		int startXLoc = -1, int startYLoc = -1)
	{
		//------------ set basic vars -------------//

		nation_recno = nationRecno;
		rank_id = rankId; // rank_id must be initialized before init_unit_id() as init_unit_id() may overwrite it
		// nation_contribution must be initialized before init_unit_id() as init_unit_id() may overwrite it
		nation_contribution = 0;

		if (rank_id == RANK_GENERAL || rank_id == RANK_KING)
		{
			team_info = new TeamInfo();
			team_info.ai_last_request_defense_date = default;
		}

		init_unit_id(unitId);

		group_select_id = 0;
		unit_group_id = UnitArray.cur_group_id++;
		race_id = UnitRes[unit_id].race_id;

		//------- init unit name ---------//

		if (race_id != 0)
		{
			name_id = RaceRes[race_id].get_new_name_id();
		}
		else //---- init non-human unit series no. ----//
		{
			if (nation_recno != 0)
				name_id = ++NationArray[nation_recno].last_unit_name_id_array[unit_id - 1];
			else
				name_id = 0;
		}

		//------- init ai_unit ----------//

		if (nation_recno != 0)
			ai_unit = NationArray[nation_recno].nation_type == NationBase.NATION_AI;
		else
			ai_unit = false;

		//----------------------------------------------//

		ai_action_id = 0;
		action_misc = UnitConstants.ACTION_MISC_STOP;
		action_misc_para = 0;

		action_mode2 = action_mode = UnitConstants.ACTION_STOP;
		action_para2 = action_para = 0;
		action_x_loc2 = action_y_loc2 = action_x_loc = action_y_loc = -1;

		attack_range = 0; //store the attack range of the current attack mode if the unit is ordered to attack

		leader_unit_recno = 0;
		team_id = 0;
		selected_flag = false;

		waiting_term = 0;
		swapping = 0; // indicate whether swapping is processed.

		spy_recno = 0;

		range_attack_x_loc = -1;
		range_attack_y_loc = -1;

		//------- initialize path seek vars -------//

		result_node_count = result_node_recno = result_path_dist = 0;

		//------- initialize way point vars -------//
		wayPoints.Clear();

		//---------- initialize game vars ----------//

		unit_mode = 0;
		unit_mode_para = 0;

		max_hit_points = UnitRes[unit_id].hit_points;
		hit_points = max_hit_points;

		loyalty = unitLoyalty;

		can_guard_flag = 0;
		can_attack_flag = true;
		force_move_flag = false;
		ai_no_suitable_action = false;
		cur_power = 0;
		max_power = 0;

		total_reward = 0;

		home_camp_firm_recno = 0;

		ignore_power_nation = 0;
		aggressive_mode = 1; // the default mode is 1

		//--------- init skill potential ---------//

		if (Misc.Random(10) == 0) // 1 out of 10 has a higher than normal potential in this skill
		{
			skill.skill_potential = 50 + Misc.Random(51); // 50 to 100 potential
		}

		//------ initialize the base Sprite class -------//

		if (startXLoc >= 0)
			init_sprite(startXLoc, startYLoc);
		else
		{
			cur_x = -1;
		}

		//------------- set attack_dir ------------//

		attack_dir = final_dir;

		//-------------- update loyalty -------------//

		update_loyalty();

		//--------------- init AI info -------------//

		if (ai_unit)
		{
			Nation nation = NationArray[nation_recno];

			if (rank_id == RANK_GENERAL || rank_id == RANK_KING)
				nation.add_general_info(sprite_recno);

			switch (UnitRes[unit_id].unit_class)
			{
				case UnitConstants.UNIT_CLASS_CARAVAN:
					nation.add_caravan_info(sprite_recno);
					break;

				case UnitConstants.UNIT_CLASS_SHIP:
					nation.add_ship_info(sprite_recno);
					break;
			}
		}

		//----------- init derived class ----------//

		init_derived();
	}

	public override void Deinit()
	{
		if (unit_id == 0)
			return;

		//-------- if this is a king --------//

		if (nation_recno != 0)
		{
			if (rank_id == RANK_KING) // check nation_recno because monster kings will die also.
			{
				king_die();
			}
			else if (rank_id == RANK_GENERAL)
			{
				general_die();
			}
		}

		//---- if this is a general, deinit its link with its soldiers ----//
		//
		// We do not use team_info because monsters and rebels also use
		// leader_unit_recno and they do not use keep the member info in team_info.
		//
		//-----------------------------------------------------------------//

		if (rank_id == RANK_GENERAL || rank_id == RANK_KING)
		{
			foreach (Unit unit in UnitArray)
			{
				if (unit.leader_unit_recno == sprite_recno)
					unit.leader_unit_recno = 0;
			}
		}

		//----- if this is a unit on a ship ------//

		if (unit_mode == UnitConstants.UNIT_MODE_ON_SHIP)
		{
			// the ship may have been destroyed at the same time. Actually when the ship is destroyed,
			// all units onboard are killed and this function is called.
			if (!UnitArray.IsDeleted(unit_mode_para))
			{
				Unit unit = UnitArray[unit_mode_para];
				((UnitMarine)unit).del_unit(sprite_recno);
			}
		}

		//----- if this is a ship in the harbor -----//

		else if (unit_mode == UnitConstants.UNIT_MODE_IN_HARBOR)
		{
			// the ship may have been destroyed at the same time. Actually when the ship is destroyed,
			// all firms onboard are killed and this function is called.
			if (!FirmArray.IsDeleted(unit_mode_para))
			{
				Firm firm = FirmArray[unit_mode_para];
				((FirmHarbor)firm).del_hosted_ship(sprite_recno);
			}
		}

		//----- if this unit is a constructor in a firm -------//

		else if (unit_mode == UnitConstants.UNIT_MODE_CONSTRUCT)
		{
			FirmArray[unit_mode_para].builder_recno = 0;
		}

		//-------- if this is a spy ---------//

		if (spy_recno != 0)
		{
			SpyArray.DeleteSpy(SpyArray[spy_recno]);
			spy_recno = 0;
		}

		//---------- reset command ----------//

		if (Power.command_unit_recno == sprite_recno)
			Power.reset_command();

		//-----------------------------------//

		deinit_unit_id();

		//-------- reset seek path ----------//

		reset_path();

		//----- if cur_x == -1, the unit has not yet been hired -----//

		if (cur_x >= 0)
			deinit_sprite();

		//------------------------------------------------//
		//
		// Prime rule:
		//
		// world.get_loc(next_x_loc() and next_y_loc()).cargo_recno
		// is always = sprite_recno
		//
		// no matter what cur_action is.
		//
		//------------------------------------------------//
		//
		// Relationship between (next_x, next_y) and (cur_x, cur_y)
		//
		// when SPRITE_WAIT, SPRITE_IDLE, SPRITE_READY_TO_MOVE,
		//      SPRITE_ATTACK, SPRITE_DIE:
		//
		// (next_x, next_y) == (cur_x, cur_y), it's the location of the sprite.
		//
		// when SPRITE_MOVE:
		//
		// (next_x, next_y) != (cur_x, cur_y)
		// (next_x, next_y) is where the sprite is moving towards.
		// (cur_x , cur_y ) is the location of the sprite.
		//
		//------------------------------------------------//

		//--------------- deinit AI info -------------//

		if (ai_unit)
		{
			if (!NationArray.IsDeleted(nation_recno))
			{
				Nation nation = NationArray[nation_recno];

				if (rank_id == RANK_GENERAL || rank_id == RANK_KING)
					nation.del_general_info(sprite_recno);

				switch (UnitRes[unit_id].unit_class)
				{
					case UnitConstants.UNIT_CLASS_CARAVAN:
						nation.del_caravan_info(sprite_recno);
						break;

					case UnitConstants.UNIT_CLASS_SHIP:
						nation.del_ship_info(sprite_recno);
						break;
				}
			}
		}

		//-------------- reset unit_id ---------------//

		unit_id = 0;
	}

	public virtual void init_derived()
	{
	}

	public void init_sprite(int startXLoc, int startYLoc)
	{
		base.init(UnitRes[unit_id].sprite_id, startXLoc, startYLoc);

		//--------------------------------------------------------------------//
		// move_to_?_loc is always the current location of the unit as
		// cur_action == SPRITE_IDLE
		//--------------------------------------------------------------------//
		original_action_mode = 0;
		ai_original_target_x_loc = -1;

		attack_range = 0;

		move_to_x_loc = next_x_loc();
		move_to_y_loc = next_y_loc();

		go_x = next_x;
		go_y = next_y;

		//-------- set the cargo_recno -------------//

		for (int h = 0, y = startYLoc; h < sprite_info.loc_height; h++, y++)
		{
			for (int w = 0, x = startXLoc; w < sprite_info.loc_width; w++, x++)
			{
				World.set_unit_recno(x, y, mobile_type, sprite_recno);
			}
		}

		if (is_own() || (nation_recno != 0 && NationArray[nation_recno].is_allied_with_player))
		{
			World.unveil(startXLoc, startYLoc, startXLoc + sprite_info.loc_width - 1,
				startYLoc + sprite_info.loc_height - 1);

			World.visit(startXLoc, startYLoc, startXLoc + sprite_info.loc_width - 1,
				startYLoc + sprite_info.loc_height - 1, UnitRes[unit_id].visual_range,
				UnitRes[unit_id].visual_extend);
		}
	}

	public void deinit_sprite(bool keepSelected = false)
	{
		if (cur_x == -1)
			return;

		//---- if this unit is led by a leader, only mobile units has leader_unit_recno assigned to a leader -----//
		// units are still considered mobile when boarding a ship

		if (leader_unit_recno != 0 && unit_mode != UnitConstants.UNIT_MODE_ON_SHIP)
		{
			if (!UnitArray.IsDeleted(leader_unit_recno)) // the leader unit may have been killed at the same time
				UnitArray[leader_unit_recno].del_team_member(sprite_recno);

			leader_unit_recno = 0;
		}

		//-------- clear the cargo_recno ----------//

		for (int h = 0, y = next_y_loc(); h < sprite_info.loc_height; h++, y++)
		{
			for (int w = 0, x = next_x_loc(); w < sprite_info.loc_width; w++, x++)
			{
				World.set_unit_recno(x, y, mobile_type, 0);
			}
		}

		cur_x = -1;

		//---- reset other parameters related to this unit ----//

		if (!keepSelected)
		{
			if (UnitArray.selected_recno == sprite_recno)
			{
				UnitArray.selected_recno = 0;
				Info.disp();
			}

			//TODO rewrite
			//if (Power.command_unit_recno == sprite_recno)
				//Power.command_id = 0;
		}

		//------- deinit unit mode -------//

		deinit_unit_mode();
	}

	private void init_unit_id(int unitId)
	{
		unit_id = unitId;

		UnitInfo unitInfo = UnitRes[unit_id];

		sprite_id = unitInfo.sprite_id;
		sprite_info = SpriteRes[sprite_id];

		mobile_type = unitInfo.mobile_type;

		//--- if this unit is a weapon unit with multiple versions ---//

		set_combat_level(100); // set combat level default to 100, for human units, it will be adjusted later by individual functions

		int techLevel;
		if (nation_recno != 0 && unitInfo.unit_class == UnitConstants.UNIT_CLASS_WEAPON &&
		    (techLevel = unitInfo.nation_tech_level_array[nation_recno - 1]) > 0)
		{
			set_weapon_version(techLevel);
		}

		fix_attack_info();

		//-------- set unit count ----------//

		if (nation_recno != 0)
		{
			if (rank_id != RANK_KING)
				unitInfo.inc_nation_unit_count(nation_recno);

			if (rank_id == RANK_GENERAL)
				unitInfo.inc_nation_general_count(nation_recno);
		}

		//--------- increase monster count ----------//

		if (UnitRes[unit_id].unit_class == UnitConstants.UNIT_CLASS_MONSTER)
			UnitRes.mobile_monster_count++;
	}

	public void deinit_unit_id()
	{
		//-----------------------------------------//

		UnitInfo unitInfo = UnitRes[unit_id];

		if (nation_recno != 0)
		{
			if (rank_id != RANK_KING)
				unitInfo.dec_nation_unit_count(nation_recno);

			if (rank_id == RANK_GENERAL)
				unitInfo.dec_nation_general_count(nation_recno);
		}

		//--------- if the unit is a spy ----------//
		//
		// A spy has double identity and is counted
		// by both the true controlling nation and
		// the deceiving nation.
		//
		//-----------------------------------------//

		if (spy_recno != 0 && true_nation_recno() != nation_recno)
		{
			int trueNationRecno = true_nation_recno();

			if (rank_id != RANK_KING)
				unitInfo.dec_nation_unit_count(trueNationRecno);

			if (rank_id == RANK_GENERAL)
				unitInfo.dec_nation_general_count(trueNationRecno);
		}

		//--------- decrease monster count ----------//

		if (UnitRes[unit_id].unit_class == UnitConstants.UNIT_CLASS_MONSTER)
		{
			UnitRes.mobile_monster_count--;
		}
	}

	public void deinit_unit_mode()
	{
		//----- this unit was defending the town before it gets killed ----//

		if (unit_mode == UnitConstants.UNIT_MODE_DEFEND_TOWN)
		{
			if (!TownArray.IsDeleted(unit_mode_para))
			{
				Town town = TownArray[unit_mode_para];

				if (nation_recno == town.NationId)
					town.ReduceDefenderCount();
			}

			set_mode(0); // reset mode
		}

		//----- this is a monster unit defending its town ------//

		else if (unit_mode == UnitConstants.UNIT_MODE_MONSTER && unit_mode_para != 0)
		{
			if (((UnitMonster)this).monster_action_mode != UnitConstants.MONSTER_ACTION_DEFENSE)
				return;

			FirmMonster firmMonster = (FirmMonster)FirmArray[unit_mode_para];
			firmMonster.reduce_defender_count(rank_id);
		}
	}

	public void del_team_member(int unitRecno)
	{
		for (int i = team_info.member_unit_array.Count - 1; i >= 0; i--)
		{
			if (team_info.member_unit_array[i] == unitRecno)
			{
				team_info.member_unit_array.RemoveAt(i);
				return;
			}
		}

		//-------------------------------------------------------//
		//
		// Note: for rebels and monsters, although they have
		//       leader_unit_recno, their team_info is not used.
		//       So del_team_member() won't be able to match the
		//       unit in its member_unit_array[].
		//
		//-------------------------------------------------------//
	}

	public void validate_team()
	{
		for (int i = team_info.member_unit_array.Count - 1; i >= 0; i--)
		{
			int unitRecno = team_info.member_unit_array[i];

			if (UnitArray.IsDeleted(unitRecno))
			{
				team_info.member_unit_array.RemoveAt(i);
			}
		}
	}

	public void set_spy(int spyRecno)
	{
		spy_recno = spyRecno;
	}

	public void set_name(int newNameId)
	{
		//------- free up the existing name id. ------//

		RaceRes[race_id].free_name_id(name_id);

		//------- set the new name id. ---------//

		name_id = newNameId;

		//-------- register usage of the new name id. ------//

		RaceRes[race_id].use_name_id(name_id);
	}

	public void set_mode(int modeId, int modePara = 0)
	{
		unit_mode = modeId;
		unit_mode_para = modePara;
	}

	public override bool is_shealth()
	{
		return Config.fog_of_war && World.get_loc(next_x_loc(), next_y_loc()).visibility() < UnitRes[unit_id].shealth;
	}

	public bool is_civilian()
	{
		return race_id > 0 && skill.skill_id != Skill.SKILL_LEADING && unit_mode != UnitConstants.UNIT_MODE_REBEL;
	}

	public bool is_own()
	{
		return is_nation(NationArray.player_recno);
	}

	public bool is_own_spy()
	{
		return spy_recno != 0 && SpyArray[spy_recno].true_nation_recno == NationArray.player_recno;
	}

	public bool is_nation(int nationRecno)
	{
		if (nation_recno == nationRecno)
			return true;

		if (spy_recno != 0 && SpyArray[spy_recno].true_nation_recno == nationRecno)
			return true;

		return false;
	}

	// the true nation recno of the unit, taking care of the situation where the unit is a spy
	public int true_nation_recno()
	{
		return spy_recno != 0 ? SpyArray[spy_recno].true_nation_recno : nation_recno;
	}

	public virtual bool is_ai_all_stop()
	{
		return cur_action == SPRITE_IDLE &&
		       action_mode == UnitConstants.ACTION_STOP &&
		       action_mode2 == UnitConstants.ACTION_STOP &&
		       ai_action_id == 0;
	}

	public bool get_cur_loc(out int xLoc, out int yLoc)
	{
		xLoc = -1;
		yLoc = -1;
		if (is_visible())
		{
			xLoc = next_x_loc(); // update location
			yLoc = next_y_loc();
		}
		else if (unit_mode == UnitConstants.UNIT_MODE_OVERSEE ||
		         unit_mode == UnitConstants.UNIT_MODE_CONSTRUCT ||
		         unit_mode == UnitConstants.UNIT_MODE_IN_HARBOR)
		{
			Firm firm = FirmArray[unit_mode_para];

			xLoc = firm.center_x;
			yLoc = firm.center_y;
		}
		else if (unit_mode == UnitConstants.UNIT_MODE_ON_SHIP)
		{
			Unit unit = UnitArray[unit_mode_para];

			//xLoc = unit.next_x_loc();
			//yLoc = unit.next_y_loc();
			if (unit.is_visible())
			{
				xLoc = unit.next_x_loc();
				yLoc = unit.next_y_loc();
			}
			else
			{
				Firm firm = FirmArray[unit.unit_mode_para];
				xLoc = firm.center_x;
				yLoc = firm.center_y;
			}
		}
		else
		{
			return false;
		}

		return true;
	}

	public bool get_cur_loc2(out int xLoc, out int yLoc)
	{
		xLoc = -1;
		yLoc = -1;
		
		if (is_visible())
		{
			xLoc = cur_x_loc();
			yLoc = cur_y_loc();
		}
		else if (unit_mode == UnitConstants.UNIT_MODE_OVERSEE ||
		         unit_mode == UnitConstants.UNIT_MODE_CONSTRUCT ||
		         unit_mode == UnitConstants.UNIT_MODE_IN_HARBOR)
		{
			Firm firm = FirmArray[unit_mode_para];
			xLoc = firm.center_x;
			yLoc = firm.center_y;
		}
		else if (unit_mode == UnitConstants.UNIT_MODE_ON_SHIP)
		{
			Unit unit = UnitArray[unit_mode_para];

			if (unit.is_visible())
			{
				xLoc = unit.cur_x_loc();
				yLoc = unit.cur_y_loc();
			}
			else
			{
				Firm firm = FirmArray[unit.unit_mode_para];
				xLoc = firm.center_x;
				yLoc = firm.center_y;
			}
		}
		else
		{
			return false;
		}

		return true;
	}

	public bool is_leader_in_range()
	{
		if (leader_unit_recno == 0)
			return false;

		if (UnitArray.IsDeleted(leader_unit_recno))
		{
			leader_unit_recno = 0;
			return false;
		}

		Unit leaderUnit = UnitArray[leader_unit_recno];

		if (!leaderUnit.is_visible() && leaderUnit.unit_mode == UnitConstants.UNIT_MODE_CONSTRUCT)
			return false;

		int leaderXLoc, leaderYLoc, xLoc, yLoc;
		leaderUnit.get_cur_loc2(out leaderXLoc, out leaderYLoc);
		get_cur_loc2(out xLoc, out yLoc);

		if (leaderXLoc >= 0 && Misc.points_distance(xLoc, yLoc, leaderXLoc, leaderYLoc) <= GameConstants.EFFECTIVE_LEADING_DISTANCE)
			return leader_unit_recno != 0;

		return false;
	}

	public virtual void die()
	{
	}

	//-------------- AI functions -------------//

	public virtual void process_ai()
	{
		//------ the aggressive_mode of AI units is always 1 ------//

		aggressive_mode = 1;

		//--- if it's a spy from other nation, don't control it ---//

		if (spy_recno != 0 && true_nation_recno() != nation_recno &&
		    SpyArray[spy_recno].notify_cloaked_nation_flag == 0 && Info.TotalDays % 60 != sprite_recno % 60)
		{
			return;
		}

		//----- think about rewarding this unit -----//

		if (race_id != 0 && rank_id != RANK_KING && Info.TotalDays % 5 == sprite_recno % 5)
			think_reward();

		//-----------------------------------------//

		if (!is_visible())
			return;

		//--- if the unit has stopped, but ai_action_id hasn't been reset ---//

		if (cur_action == SPRITE_IDLE &&
		    action_mode == UnitConstants.ACTION_STOP && action_mode2 == UnitConstants.ACTION_STOP &&
		    ai_action_id != 0 && nation_recno != 0)
		{
			NationArray[nation_recno].action_failure(ai_action_id, sprite_recno);
		}

		//---- King flees under attack or surrounded by enemy ---//

		if (race_id != 0 && rank_id == RANK_KING)
		{
			if (think_king_flee())
				return;
		}

		//---- General flees under attack or surrounded by enemy ---//

		if (race_id != 0 && rank_id == RANK_GENERAL && Info.TotalDays % 7 == sprite_recno % 7)
		{
			if (think_general_flee())
				return;
		}

		//-- let Unit::next_day() process it process original_action_mode --//

		if (original_action_mode != 0)
			return;

		//------ if the unit is not stop right now ------//

		if (!is_ai_all_stop())
		{
			think_stop_chase();
			return;
		}

		//-----------------------------------------//

		if (mobile_type == UnitConstants.UNIT_LAND)
		{
			if (ai_escape_fire())
				return;
		}

		//---------- if this is your spy --------//

		if (spy_recno != 0 && true_nation_recno() == nation_recno)
			think_spy_action();

		//------ if this unit is from a camp --------//

		if (home_camp_firm_recno != 0)
		{
			Firm firmCamp = FirmArray[home_camp_firm_recno];
			bool rc;

			if (rank_id == RANK_SOLDIER)
				rc = firmCamp.workers.Count < Firm.MAX_WORKER;
			else
				rc = (firmCamp.overseer_recno == 0);

			if (rc)
			{
				if (return_camp())
					return;
			}

			home_camp_firm_recno = 0; // the camp is already occupied by somebody
		}

		//----------------------------------------//

		if (race_id != 0 && rank_id == RANK_KING)
		{
			think_king_action();
		}
		else if (race_id != 0 && rank_id == RANK_GENERAL)
		{
			think_general_action();
		}
		else
		{
			if (UnitRes[unit_id].unit_class == UnitConstants.UNIT_CLASS_WEAPON)
			{
				// don't call too often as the action may fail and it takes a while to call the function each time
				if (Info.TotalDays % 15 == sprite_recno % 15)
				{
					think_weapon_action(); //-- ships AI are called in UnitMarine --//
				}
			}
			else if (race_id != 0)
			{
				//--- if previous attempts for new action failed, don't call think_normal_human_action() so frequently then ---//

				if (ai_no_suitable_action)
				{
					// don't call too often as the action may fail and it takes a while to call the function each time
					if (Info.TotalDays % 15 != sprite_recno % 15)
						return;
				}

				//---------------------------------//

				if (!think_normal_human_action())
				{
					// set this flag so think_normal_human_action() won't be called continously
					ai_no_suitable_action = true;

					ai_move_to_nearby_town();
				}
			}
		}
	}

	public bool think_king_action()
	{
		return think_leader_action();
	}

	public bool think_general_action()
	{
		if (think_leader_action())
			return true;

		//--- if the general is not assigned to a camp due to its low competency ----//

		Nation ownNation = NationArray[nation_recno];
		bool rc = false;

		if (team_info.member_unit_array.Count <= 1)
		{
			rc = true;
		}

		//--- if the skill of the general and the number of soldiers he commands is not large enough to justify building a new camp ---//

		else if (skill.skill_level /* + team_info.member_count*4*/ < 40 + ownNation.pref_keep_general / 5) // 40 to 60
		{
			rc = true;
		}

		//-- think about splitting the team and assign them into other forts --//

		else if (ownNation.ai_has_too_many_camp())
		{
			rc = true;
		}

		//--------- demote the general to soldier and disband the troop -------//

		if (rc)
		{
			for (int i = 0; i < 4; i++)
				reward(nation_recno);
			set_rank(RANK_SOLDIER);
			return think_normal_human_action();
		}

		return false;
	}

	public bool think_leader_action()
	{
		Nation nation = NationArray[nation_recno];
		FirmCamp bestCamp = null;
		int bestRating = 10;
		ActionNode delActionNode = null;
		int curXLoc = next_x_loc(), curYLoc = next_y_loc();
		int curRegionId = World.get_region_id(curXLoc, curYLoc);

		// if this unit is the king, always assign it to a camp regardless of whether the king is a better commander than the existing one
		if (rank_id == RANK_KING)
			bestRating = -1000;

		//----- think about which camp to move to -----//

		for (int i = nation.ai_camp_array.Count - 1; i >= 0; i--)
		{
			FirmCamp firmCamp = (FirmCamp)FirmArray[nation.ai_camp_array[i]];

			if (firmCamp.region_id != curRegionId)
				continue;

			//--- if the commander of this camp is the king, never replace him ---//

			if (firmCamp.overseer_recno == nation.king_unit_recno)
				continue;

			//--- we have separate logic for choosing generals of capturing camps ---//

			if (firmCamp.ai_is_capturing_independent_village())
				continue;

			//-------------------------------------//

			int curLeadership = firmCamp.cur_commander_leadership();
			int newLeadership = firmCamp.new_commander_leadership(race_id, skill.skill_level);

			int curRating = newLeadership - curLeadership;

			//-------------------------------------//

			if (curRating > bestRating)
			{
				//--- if there is already somebody being assigned to it ---//

				ActionNode actionNode = null;

				if (rank_id != RANK_KING) // don't check this if the current unit is the king
				{
					actionNode = nation.get_action(firmCamp.loc_x1, firmCamp.loc_y1,
						-1, -1, Nation.ACTION_AI_ASSIGN_OVERSEER, Firm.FIRM_CAMP);

					if (actionNode != null && actionNode.processing_instance_count > 0)
						continue;
				}

				bestRating = curRating;
				bestCamp = firmCamp;
				delActionNode = actionNode;
			}
		}

		if (bestCamp == null)
			return false;

		//----- delete an unprocessed queued action if there is any ----//

		if (delActionNode != null)
			nation.del_action(delActionNode);

		//--------- move to the camp now ---------//

		//-- if there is room in the camp to host all soldiers led by this general --//

		int memberCount = team_info.member_unit_array.Count;
		if (memberCount > 0 && memberCount - 1 <= Firm.MAX_WORKER - bestCamp.workers.Count)
		{
			validate_team();

			UnitArray.assign(bestCamp.loc_x1, bestCamp.loc_y1, false, InternalConstants.COMMAND_AI,
				team_info.member_unit_array);
			return true;
		}
		else //--- otherwise assign the general only ---//
		{
			return nation.add_action(bestCamp.loc_x1, bestCamp.loc_y1, -1, -1,
				Nation.ACTION_AI_ASSIGN_OVERSEER, Firm.FIRM_CAMP, 1, sprite_recno) != null;
		}

		return false;
	}

	public bool think_normal_human_action()
	{
		if (home_camp_firm_recno != 0)
			return false;

		// if the unit is led by a commander, let the commander makes the decision.
		// If the leader has been assigned to a firm, don't consider it as a leader anymore
		if (leader_unit_recno != 0 && UnitArray[leader_unit_recno].is_visible())
		{
			return false;
		}

		//---- think about assign the unit to a firm that needs workers ----//

		Nation ownNation = NationArray[nation_recno];
		Firm bestFirm = null;
		int regionId = World.get_region_id(next_x_loc(), next_y_loc());
		int skillId = skill.skill_id;
		int skillLevel = skill.skill_level;
		int bestRating = 0;
		int curXLoc = next_x_loc(), curYLoc = next_y_loc();

		if (skill.skill_id != 0)
		{
			foreach (Firm firm in FirmArray)
			{
				if (firm.nation_recno != nation_recno)
					continue;

				if (firm.region_id != regionId)
					continue;

				int curRating = 0;

				if (skill.skill_id == Skill.SKILL_CONSTRUCTION) // if this is a construction worker
				{
					if (firm.builder_recno != 0) // assign the construction worker to this firm as an residental builder
						continue;
				}
				else
				{
					if (firm.workers.Count == 0 || firm.firm_skill_id != skillId)
					{
						continue;
					}

					//----- if the firm is full of worker ------//

					if (firm.is_worker_full())
					{
						if (firm.firm_id != Firm.FIRM_CAMP)
						{
							//---- get the lowest skill worker of the firm -----//

							int minSkill = 1000;

							for (int j = 0; j < firm.workers.Count; j++)
							{
								Worker worker = firm.workers[j];
								if (worker.skill_level < minSkill)
									minSkill = worker.skill_level;
							}

							//------------------------------//

							if (firm.majority_race() == race_id)
							{
								if (skill.skill_level < minSkill + 10)
									continue;
							}
							else //-- for different race, only assign if the skill is significantly higher than the existing ones --//
							{
								if (skill.skill_level < minSkill + 30)
									continue;
							}
						}
						else
						{
							//---- get the lowest max hit points worker of the camp -----//

							int minMaxHitPoints = 1000;
							int minMaxHitPointsOtherRace = 1000;
							bool hasOtherRace = false;

							for (int j = 0; j < firm.workers.Count; j++)
							{
								Worker worker = firm.workers[j];
								if (worker.max_hit_points() < minMaxHitPoints)
									minMaxHitPoints = worker.max_hit_points();

								if (worker.race_id != firm.majority_race())
								{
									hasOtherRace = true;
									if (worker.max_hit_points() < minMaxHitPointsOtherRace)
										minMaxHitPointsOtherRace = worker.max_hit_points();
								}
							}

							if (firm.majority_race() == race_id)
							{
								if ((!hasOtherRace && (minMaxHitPoints > max_hit_points - 10)) ||
								    (hasOtherRace && (minMaxHitPointsOtherRace > max_hit_points + 30)))
									continue;
							}
							else
							{
								if ((hasOtherRace && (minMaxHitPointsOtherRace > max_hit_points - 10)) ||
								    (!hasOtherRace && (minMaxHitPoints > max_hit_points - 30)))
									continue;
							}
						}
					}
					else
					{
						curRating += 300; // if the firm is not full, rating + 300
					}
				}

				//-------- calculate the rating ---------//

				curRating += World.distance_rating(curXLoc, curYLoc, firm.center_x, firm.center_y);

				if (firm.majority_race() == race_id)
					curRating += 70;

				curRating += (Firm.MAX_WORKER - firm.workers.Count) * 10;

				//-------------------------------------//

				if (curRating > bestRating)
				{
					bestRating = curRating;
					bestFirm = firm;
				}
			}

			if (bestFirm != null)
			{
				if (bestFirm.firm_id == Firm.FIRM_CAMP && bestFirm.workers.Count == Firm.MAX_WORKER
				                                       && Misc.points_distance(curXLoc, curYLoc, bestFirm.loc_x1,
					                                       bestFirm.loc_y1) < 5)
				{
					int minMaxHitPointsOtherRace = 1000;
					int bestWorkerId = -1;
					for (int j = 0; j < bestFirm.workers.Count; j++)
					{
						Worker worker = bestFirm.workers[j];
						if (worker.race_id != race_id)
						{
							if (worker.max_hit_points() < minMaxHitPointsOtherRace)
							{
								minMaxHitPointsOtherRace = worker.max_hit_points();
								bestWorkerId = j + 1;
							}
						}
					}

					if (bestWorkerId == -1)
					{
						int minMaxHitPoints = 1000;
						for (int j = 0; j < bestFirm.workers.Count; j++)
						{
							Worker worker = bestFirm.workers[j];
							if (worker.max_hit_points() < minMaxHitPoints)
							{
								minMaxHitPoints = worker.max_hit_points();
								bestWorkerId = j + 1;
							}
						}
					}

					if (bestWorkerId != -1)
					{
						bestFirm.mobilize_worker(bestWorkerId, InternalConstants.COMMAND_AI);
					}
				}

				assign(bestFirm.loc_x1, bestFirm.loc_y1);
				if (bestFirm.firm_id == Firm.FIRM_CAMP)
				{
					FirmCamp firmCamp = (FirmCamp)bestFirm;
					if (firmCamp.coming_unit_array.Count < Firm.MAX_WORKER)
					{
						firmCamp.coming_unit_array.Add(sprite_recno);
					}
				}

				return true;
			}
		}

		//---- look for towns to assign to -----//

		bestRating = 0;
		bool hasTownInRegion = false;
		Town bestTown = null;

		// can't use ai_town_array[] because this function will be called by Unit::betray() when a unit defected to the player's kingdom
		foreach (Town town in TownArray)
		{
			if (town.NationId != nation_recno)
				continue;

			if (town.RegionId != regionId)
				continue;

			hasTownInRegion = true;

			if (town.Population >= GameConstants.MAX_TOWN_POPULATION || !town.IsBaseTown)
				continue;

			//--- only assign to towns of the same race ---//

			if (ownNation.pref_town_harmony > 50)
			{
				if (town.MajorityRace() != race_id)
					continue;
			}

			//-------- calculate the rating ---------//

			int curRating = World.distance_rating(curXLoc, curYLoc, town.LocCenterX, town.LocCenterY);

			curRating += 300 * town.RacesPopulation[race_id - 1] / town.Population; // racial homogenous bonus

			curRating += GameConstants.MAX_TOWN_POPULATION - town.Population;

			//-------------------------------------//

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestTown = town;
			}
		}

		if (bestTown != null)
		{
			//DieselMachine TODO do not settle skilled soldiers
			if (max_hit_points < 50)
			{
				assign(bestTown.LocX1, bestTown.LocY1);
				return true;
			}
		}

		//----- if we don't have any existing towns in this region ----//

		if (!hasTownInRegion)
		{

			// --- if region is too small don't consider this area, stay in the island forever --//
			if (RegionArray.GetRegionInfo(regionId).region_stat_id == 0)
				return false;

			//-- if we also don't have any existing camps in this region --//

			if (RegionArray.get_region_stat(regionId).camp_nation_count_array[nation_recno - 1] == 0)
			{
				//---- try to build one if this unit can ----//

				if (ownNation.cash > FirmRes[Firm.FIRM_CAMP].setup_cost &&
				    FirmRes[Firm.FIRM_CAMP].can_build(sprite_recno) &&
				    leader_unit_recno == 0) // if this unit is commanded by a leader, let the leader build the camp
				{
					ai_build_camp();
				}
			}
			else // if there is already a camp in this region, try to settle a new town next to the camp
			{
				ai_settle_new_town();
			}

			// if we don't have any town in this region, return 1, so this unit won't be resigned
			// and so it can wait for other units to set up camps and villages later ---//
			return true;
		}

		return false;
	}

	public bool think_weapon_action()
	{
		//---- first try to assign the weapon to an existing camp ----//

		if (think_assign_weapon_to_camp())
			return true;

		//----- if no camp to assign, build a new one ------//

		//if (think_build_camp())
			//return true;

		return false;
	}

	public bool think_assign_weapon_to_camp()
	{
		Nation nation = NationArray[nation_recno];
		FirmCamp bestCamp = null;
		int bestRating = 0;
		int regionId = World.get_region_id(next_x_loc(), next_y_loc());
		int curXLoc = next_x_loc(), curYLoc = next_y_loc();

		for (int i = 0; i < nation.ai_camp_array.Count; i++)
		{
			FirmCamp firmCamp = (FirmCamp)FirmArray[nation.ai_camp_array[i]];

			if (firmCamp.region_id != regionId || firmCamp.is_worker_full())
				continue;

			//-------- calculate the rating ---------//

			int curRating = World.distance_rating(curXLoc, curYLoc, firmCamp.center_x, firmCamp.center_y);

			curRating += (Firm.MAX_WORKER - firmCamp.workers.Count) * 10;

			//-------------------------------------//

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestCamp = firmCamp;
			}
		}

		//-----------------------------------//

		if (bestCamp != null)
		{
			assign(bestCamp.loc_x1, bestCamp.loc_y1);
			return true;
		}

		return false;
	}

	public bool think_build_camp()
	{
		//---- select a town to build the camp ---//

		Nation ownNation = NationArray[nation_recno];
		Town bestTown = null;
		int bestRating = 0;
		int regionId = World.get_region_id(next_x_loc(), next_y_loc());
		int curXLoc = next_x_loc(), curYLoc = next_y_loc();

		for (int i = ownNation.ai_town_array.Count - 1; i >= 0; i--)
		{
			Town town = TownArray[ownNation.ai_town_array[i]];

			if (town.RegionId != regionId)
				continue;

			if (!town.IsBaseTown || town.NoNeighborSpace)
				continue;

			int curRating = World.distance_rating(curXLoc, curYLoc, town.LocCenterX, town.LocCenterY);

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestTown = town;
			}
		}

		if (bestTown != null)
			return bestTown.AIBuildNeighborFirm(Firm.FIRM_CAMP);

		return false;
	}

	public bool think_reward()
	{
		Nation ownNation = NationArray[nation_recno];

		//----------------------------------------------------------//
		// The need to secure high loyalty on this unit is based on:
		// -its skill
		// -its combat level 
		// -soldiers commanded by this unit
		//----------------------------------------------------------//

		if (spy_recno != 0 && true_nation_recno() == nation_recno) // if this is a spy of ours
		{
			return false; // Spy.think_reward() will handle this.
		}

		int curLoyalty = loyalty;
		int neededLoyalty;

		//----- if this unit is on a mission ------/

		if (ai_action_id != 0)
		{
			neededLoyalty = GameConstants.UNIT_BETRAY_LOYALTY + 10;
		}

		//----- otherwise only reward soldiers and generals ------//

		//DieselMachine TODO do not reward generals that are not is the camps
		else if (skill.skill_id == Skill.SKILL_LEADING)
		{
			//----- calculate the needed loyalty --------//

			neededLoyalty = commanded_soldier_count() * 5 + skill.skill_level;

			if (unit_mode == UnitConstants.UNIT_MODE_OVERSEE) // if this unit is an overseer
			{
				// if this unit's loyalty is < betrayel level, reward immediately
				if (loyalty < GameConstants.UNIT_BETRAY_LOYALTY)
				{
					reward(nation_recno); // reward it immediatley if it's an overseer, don't check ai_should_spend()
					return true;
				}

				neededLoyalty += 30;
			}

			// 10 points above the betray loyalty level to prevent betrayal
			neededLoyalty = Math.Max(GameConstants.UNIT_BETRAY_LOYALTY + 10, neededLoyalty);
			neededLoyalty = Math.Min(90, neededLoyalty);
		}
		else
		{
			return false;
		}

		//------- if the loyalty is already high enough ------//

		if (curLoyalty >= neededLoyalty)
			return false;

		//---------- see how many cash & profit we have now ---------//

		int rewardNeedRating = neededLoyalty - curLoyalty;

		if (curLoyalty < GameConstants.UNIT_BETRAY_LOYALTY + 5)
			rewardNeedRating += 50;

		if (ownNation.ai_should_spend(rewardNeedRating))
		{
			reward(nation_recno);
			return true;
		}

		return false;
	}

	public void think_independent_unit()
	{
		if (!is_ai_all_stop())
			return;

		//--- don't process if it's a spy and the notify cloak flag is on ---//

		if (spy_recno != 0)
		{
			//---------------------------------------------//
			//
			// If notify_cloaked_nation_flag is 0, the AI
			// won't control the unit.
			//
			// If notify_cloaked_nation_flag is 1, the AI
			// will control the unit. But not immediately,
			// it will do it once 5 days so the player can
			// have a chance to select the unit and set its
			// notify_cloaked_nation_flag back to 0 if the
			// player wants.
			//
			//---------------------------------------------//

			if (SpyArray[spy_recno].notify_cloaked_nation_flag == 0)
				return;

			if (Info.TotalDays % 5 != sprite_recno % 5)
				return;
		}

		//-------- if this is a rebel ----------//

		if (unit_mode == UnitConstants.UNIT_MODE_REBEL)
		{
			Rebel rebel = RebelArray[unit_mode_para];

			//--- if the group this rebel belongs to already has a rebel town, assign to it now ---//

			if (rebel.town_recno != 0)
			{
				if (!TownArray.IsDeleted(rebel.town_recno))
				{
					Town town = TownArray[rebel.town_recno];

					assign(town.LocX1, town.LocY1);
				}

				return; // don't do anything if the town has been destroyed, Rebel.next_day() will take care of it. 
			}
		}

		//---- look for towns to assign to -----//

		Town bestTown = null;
		int regionId = World.get_region_id(next_x_loc(), next_y_loc());
		int bestRating = 0;
		int curXLoc = next_x_loc(), curYLoc = next_y_loc();

		foreach (Town town in TownArray)
		{
			if (town.NationId != 0 || town.Population >= GameConstants.MAX_TOWN_POPULATION ||
			    town.RegionId != regionId)
			{
				continue;
			}

			int curRating = World.distance_rating(curXLoc, curYLoc, town.LocCenterX, town.LocCenterY);
			curRating += 100 * town.RacesPopulation[race_id - 1] / town.Population;

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestTown = town;
			}
		}

		if (bestTown != null)
		{
			//--- drop its rebel identity and becomes a normal unit if he decides to settle to a town ---//

			if (unit_mode == UnitConstants.UNIT_MODE_REBEL)
				RebelArray.drop_rebel_identity(sprite_recno);

			assign(bestTown.LocX1, bestTown.LocY1);
		}
		else
		{
			resign(InternalConstants.COMMAND_AI);
		}
	}

	public void think_spy_action()
	{
		//ai_move_to_nearby_town();		// just move it to one of our towns
	}

	public bool think_king_flee()
	{
		// the king is already fleeing now
		if (force_move_flag && cur_action != SPRITE_IDLE && cur_action != SPRITE_ATTACK)
			return true;

		//------- if the king is alone --------//

		Nation ownNation = NationArray[nation_recno];

		//------------------------------------------------//
		// When the king is alone and there is no assigned action OR
		// when the king is injured, the king will flee
		// back to its camp.
		//------------------------------------------------//

		if ((team_info.member_unit_array.Count == 0 && ai_action_id == 0) ||
		    hit_points < 125 - ownNation.pref_military_courage / 4)
		{
			//------------------------------------------//
			//
			// If the king is currently under attack, flee
			// to the nearest camp with the maximum protection.
			//
			//------------------------------------------//

			FirmCamp bestCamp = null;
			int bestRating = 0;
			int curXLoc = next_x_loc(), curYLoc = next_y_loc();
			int curRegionId = World.get_region_id(curXLoc, curYLoc);

			if (cur_action == SPRITE_ATTACK)
			{
				for (int i = ownNation.ai_camp_array.Count - 1; i >= 0; i--)
				{
					FirmCamp firmCamp = (FirmCamp)FirmArray[ownNation.ai_camp_array[i]];

					if (firmCamp.region_id != curRegionId)
						continue;

					// if there is already a commander in this camp. However if this is the king, than ingore this
					if (firmCamp.overseer_recno != 0 && rank_id != RANK_KING)
						continue;

					if (firmCamp.ai_is_capturing_independent_village())
						continue;

					int curRating = World.distance_rating(curXLoc, curYLoc, firmCamp.center_x, firmCamp.center_y);

					if (curRating > bestRating)
					{
						bestRating = curRating;
						bestCamp = firmCamp;
					}
				}

			}
			else if (home_camp_firm_recno != 0) // if there is a home for the king
			{
				bestCamp = (FirmCamp)FirmArray[home_camp_firm_recno];
			}

			//------------------------------------//

			if (bestCamp != null)
			{
				if (Config.ai_aggressiveness > Config.OPTION_LOW)
					force_move_flag = true;

				assign(bestCamp.loc_x1, bestCamp.loc_y1);
			}
			else // if the king is neither under attack or has a home camp, then call the standard think_leader_action()
			{
				think_leader_action();
			}

			return cur_action != SPRITE_IDLE;
		}

		return false;
	}

	public bool think_general_flee()
	{
		// the general is already fleeing now
		if (force_move_flag && cur_action != SPRITE_IDLE && cur_action != SPRITE_ATTACK)
			return true;

		//------- if the general is alone --------//

		Nation ownNation = NationArray[nation_recno];

		//------------------------------------------------//
		// When the general is alone and there is no assigned action OR
		// when the general is injured, the general will flee
		// back to its camp.
		//------------------------------------------------//

		if ((team_info.member_unit_array.Count == 0 && ai_action_id == 0) ||
		    hit_points < max_hit_points * (75 + ownNation.pref_military_courage / 2) / 200) // 75 to 125 / 200
		{
			//------------------------------------------//
			//
			// If the general is currently under attack, flee
			// to the nearest camp with the maximum protection.
			//
			//------------------------------------------//

			FirmCamp bestCamp = null;
			int bestRating = 0;
			int curXLoc = next_x_loc(), curYLoc = next_y_loc();
			int curRegionId = World.get_region_id(curXLoc, curYLoc);

			if (cur_action == SPRITE_ATTACK)
			{
				for (int i = ownNation.ai_camp_array.Count - 1; i >= 0; i--)
				{
					FirmCamp firmCamp = (FirmCamp)FirmArray[ownNation.ai_camp_array[i]];

					if (firmCamp.region_id != curRegionId)
						continue;

					if (firmCamp.ai_is_capturing_independent_village())
						continue;

					int curRating = World.distance_rating(curXLoc, curYLoc, firmCamp.center_x, firmCamp.center_y);

					if (curRating > bestRating)
					{
						bestRating = curRating;
						bestCamp = firmCamp;
					}
				}

			}
			else if (home_camp_firm_recno != 0) // if there is a home for the general
			{
				bestCamp = (FirmCamp)FirmArray[home_camp_firm_recno];
			}

			//------------------------------------//

			if (bestCamp != null)
			{
				// if there is already an overseer there, just move close to the camp for protection
				if (bestCamp.overseer_recno != 0)
				{
					if (Config.ai_aggressiveness > Config.OPTION_LOW)
						force_move_flag = true;

					move_to(bestCamp.loc_x1, bestCamp.loc_y1);
				}
				else
				{
					assign(bestCamp.loc_x1, bestCamp.loc_y1);
				}
			}
			else // if the general is neither under attack or has a home camp, then call the standard think_leader_action()
			{
				think_leader_action();
			}

			return cur_action != SPRITE_IDLE;
		}

		return false;
	}

	public bool think_stop_chase()
	{
		//-----------------------------------------------------//
		//
		// Stop the chase if the target is being far away from
		// its original attacking location.
		//
		//-----------------------------------------------------//

		if (!(action_mode == UnitConstants.ACTION_ATTACK_UNIT && ai_original_target_x_loc >= 0))
			return false;

		if (UnitArray.IsDeleted(action_para))
		{
			stop2();
			return true;
		}

		Unit targetUnit = UnitArray[action_para];

		if (!targetUnit.is_visible())
		{
			stop2();
			return true;
		}

		//----------------------------------------//

		int aiChaseDistance = 10 + NationArray[nation_recno].pref_military_courage / 20; // chase distance: 10 to 15

		int curDistance = Misc.points_distance(targetUnit.next_x_loc(), targetUnit.next_y_loc(),
			ai_original_target_x_loc, ai_original_target_y_loc);

		if (curDistance <= aiChaseDistance)
			return false;

		//--------- stop the unit ----------------//

		stop2();

		//--- if this unit leads a troop, stop the action of all troop members as well ---//

		int leaderUnitRecno;

		if (leader_unit_recno != 0)
			leaderUnitRecno = leader_unit_recno;
		else
			leaderUnitRecno = sprite_recno;

		TeamInfo teamInfo = UnitArray[leaderUnitRecno].team_info;

		if (teamInfo != null)
		{
			for (int i = teamInfo.member_unit_array.Count - 1; i >= 0; i--)
			{
				int unitRecno = teamInfo.member_unit_array[i];

				if (UnitArray.IsDeleted(unitRecno))
					continue;

				UnitArray[unitRecno].stop2();
			}
		}

		return true;
	}

	public void ai_move_to_nearby_town()
	{
		//---- look for towns to assign to -----//

		Nation ownNation = NationArray[nation_recno];
		Town bestTown = null;
		int regionId = World.get_region_id(next_x_loc(), next_y_loc());
		int bestRating = 0;
		int curXLoc = next_x_loc(), curYLoc = next_y_loc();

		// can't use ai_town_array[] because this function will be called by Unit::betray() when a unit defected to the player's kingdom
		foreach (Town town in TownArray)
		{
			if (town.NationId != nation_recno)
				continue;

			if (town.RegionId != regionId)
				continue;

			//-------------------------------------//

			int curDistance = Misc.points_distance(curXLoc, curYLoc, town.LocCenterX, town.LocCenterY);

			if (curDistance < 10) // no need to move if the unit is already close enough to the town.
				return;

			int curRating = 100 - 100 * curDistance / GameConstants.MapSize;

			curRating += town.Population;

			//-------------------------------------//

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestTown = town;
			}
		}

		if (bestTown != null)
			move_to_town_surround(bestTown.LocX1, bestTown.LocY1, sprite_info.loc_width, sprite_info.loc_height);
	}

	public bool ai_escape_fire()
	{
		if (cur_action != SPRITE_IDLE)
			return false;

		if (mobile_type != UnitConstants.UNIT_LAND)
			return false;

		Location location = World.get_loc(next_x_loc(), next_y_loc());

		if (location.fire_str() == 0)
			return false;

		//--------------------------------------------//

		int checkLimit = 400; // checking for 400 location
		int xShift, yShift;
		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();

		for (int i = 2; i < checkLimit; i++)
		{
			Misc.cal_move_around_a_point(i, 20, 20, out xShift, out yShift);

			int checkXLoc = curXLoc + xShift;
			int checkYLoc = curYLoc + yShift;

			if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
				continue;

			if (!location.can_move(mobile_type))
				continue;

			location = World.get_loc(checkXLoc, checkYLoc);

			if (location.fire_str() == 0) // move to a safe place now
			{
				move_to(checkXLoc, checkYLoc);
				return true;
			}
		}

		return false;
	}

	public void ai_leader_being_attacked(int attackerUnitRecno)
	{
		// this can happen when the unit has just changed nation
		if (UnitArray[attackerUnitRecno].nation_recno == nation_recno)
			return;

		//------------------------------------//

		int callIntervalDays = 0;
		bool rc = false;

		if (rank_id == RANK_KING)
		{
			rc = true;
			callIntervalDays = 7;
		}
		else if (rank_id == RANK_GENERAL)
		{
			rc = (skill.skill_level >= 30 + (100 - NationArray[nation_recno].pref_keep_general) / 2); // 30 to 80
			callIntervalDays = 15; // don't call too freqently
		}

		if (rc)
		{
			if (Info.game_date > team_info.ai_last_request_defense_date.AddDays(callIntervalDays))
			{
				team_info.ai_last_request_defense_date = Info.game_date;
				NationArray[nation_recno].ai_defend(attackerUnitRecno);
			}
		}
	}

	public bool ai_build_camp()
	{
		//--- to prevent building more than one camp at the same time ---//

		int curRegionId = region_id();
		Nation ownNation = NationArray[nation_recno];

		if (ownNation.is_action_exist(Nation.ACTION_AI_BUILD_FIRM, Firm.FIRM_CAMP, curRegionId))
			return false;

		//------- locate a place for the camp --------//

		FirmInfo firmInfo = FirmRes[Firm.FIRM_CAMP];
		int xLoc = 0, yLoc = 0;
		int teraMask = UnitRes.mobile_type_to_mask(UnitConstants.UNIT_LAND);

		// leave at least one location space around the building
		if (World.locate_space_random(ref xLoc, ref yLoc, GameConstants.MapSize - 1, GameConstants.MapSize - 1,
			    firmInfo.loc_width + 2, firmInfo.loc_height + 2,
			    GameConstants.MapSize * GameConstants.MapSize, curRegionId, true, teraMask))
		{
			return ownNation.add_action(xLoc, yLoc, -1, -1,
				Nation.ACTION_AI_BUILD_FIRM, Firm.FIRM_CAMP, 1, sprite_recno) != null;
		}

		return false;
	}

	public bool ai_settle_new_town()
	{
		//----- locate a suitable camp for the new town to settle next to ----//

		Nation ownNation = NationArray[nation_recno];
		FirmCamp bestCamp = null;
		int curRegionId = region_id();
		int bestRating = 0;

		for (int i = ownNation.ai_camp_array.Count - 1; i >= 0; i--)
		{
			FirmCamp firmCamp = (FirmCamp)FirmArray[ownNation.ai_camp_array[i]];

			if (firmCamp.region_id != curRegionId)
				continue;

			int curRating = firmCamp.total_combat_level();

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestCamp = firmCamp;
			}
		}

		if (bestCamp == null)
			return false;

		//--------- settle a new town now ---------//

		int xLoc = bestCamp.loc_x1;
		int yLoc = bestCamp.loc_y1;

		if (World.locate_space(ref xLoc, ref yLoc, bestCamp.loc_x2, bestCamp.loc_y2,
			    InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, UnitConstants.UNIT_LAND, curRegionId, true))
		{
			settle(xLoc, yLoc);
			return true;
		}

		return false;
	}

	//------- functions for unit AI mode ---------//

	public bool think_aggressive_action()
	{
		//------ think about resuming the original action -----//

		// if it is currently attacking somebody, don't think about resuming the original action
		if (original_action_mode != 0 && cur_action != SPRITE_ATTACK)
		{
			return think_resume_original_action();
		}

		//---- think about attacking nearby units if this unit is attacking a town or a firm ---//

		// only in attack mode, as if the unit is still moving the target may be far away from the current position
		if (aggressive_mode != 0 && unit_mode == 0 && cur_action == SPRITE_ATTACK)
		{
			//--- only when the unit is currently attacking a firm or a town ---//

			if (action_mode2 == UnitConstants.ACTION_ATTACK_FIRM || action_mode2 == UnitConstants.ACTION_ATTACK_TOWN)
			{
				if (Info.TotalDays % 5 == 0) // check once every 5 days
					return think_change_attack_target();
			}
		}

		return false;
	}

	public bool think_change_attack_target()
	{
		//----------------------------------------------//

		int attackRange = Math.Max(attack_range, 8);
		int attackScanRange = attackRange * 2 + 1;

		int xOffset, yOffset;
		int curXLoc = next_x_loc(), curYLoc = next_y_loc();
		int regionId = World.get_region_id(curXLoc, curYLoc);

		for (int i = 2; i < attackScanRange * attackScanRange; i++)
		{
			Misc.cal_move_around_a_point(i, attackScanRange, attackScanRange, out xOffset, out yOffset);

			int xLoc = curXLoc + xOffset;
			int yLoc = curYLoc + yOffset;

			xLoc = Math.Max(0, xLoc);
			xLoc = Math.Min(GameConstants.MapSize - 1, xLoc);

			yLoc = Math.Max(0, yLoc);
			yLoc = Math.Min(GameConstants.MapSize - 1, yLoc);

			Location location = World.get_loc(xLoc, yLoc);

			if (location.region_id != regionId)
				continue;

			//----- if there is a unit on the location ------//

			if (location.has_unit(UnitConstants.UNIT_LAND))
			{
				int unitRecno = location.unit_recno(UnitConstants.UNIT_LAND);

				if (UnitArray.IsDeleted(unitRecno))
					continue;

				if (UnitArray[unitRecno].nation_recno != nation_recno && idle_detect_unit_checking(unitRecno))
				{
					save_original_action();

					original_target_x_loc = xLoc;
					original_target_y_loc = yLoc;

					attack_unit(xLoc, yLoc, 0, 0, true);
					return true;
				}
			}
		}

		return false;
	}

	public bool think_resume_original_action()
	{
		if (!is_visible()) // if the unit is no longer visible, cancel the saved orignal action
		{
			original_action_mode = 0;
			return false;
		}

		//---- if the unit is in defense mode now, don't do anything ----//

		if (in_any_defense_mode())
			return false;

		//----------------------------------------------------//
		//
		// If the action has been changed or the target unit has been deleted,
		// stop the chase right and move back to the original position
		// before the auto guard attack.
		//
		//----------------------------------------------------//

		if (action_mode2 != UnitConstants.ACTION_ATTACK_UNIT || UnitArray.IsDeleted(action_para2))
		{
			resume_original_action();
			return true;
		}

		//-----------------------------------------------------//
		//
		// Stop the chase if the target is being far away from
		// its original location and move back to its original
		// position before the auto guard attack.
		//
		//-----------------------------------------------------//

		const int AUTO_GUARD_CHASE_ATTACK_DISTANCE = 5;

		Unit targetUnit = UnitArray[action_para2];

		int curDistance = Misc.points_distance(targetUnit.next_x_loc(), targetUnit.next_y_loc(),
			original_target_x_loc, original_target_y_loc);

		if (curDistance > AUTO_GUARD_CHASE_ATTACK_DISTANCE)
		{
			resume_original_action();
			return true;
		}

		return false;
	}

	public void save_original_action()
	{
		if (original_action_mode == 0)
		{
			original_action_mode = action_mode2;
			original_action_para = action_para2;
			original_action_x_loc = action_x_loc2;
			original_action_y_loc = action_y_loc2;
		}
	}

	public void resume_original_action()
	{
		if (original_action_mode == 0)
			return;

		//--------- If it is an attack action ---------//

		if (original_action_mode == UnitConstants.ACTION_ATTACK_UNIT ||
		    original_action_mode == UnitConstants.ACTION_ATTACK_FIRM ||
		    original_action_mode == UnitConstants.ACTION_ATTACK_TOWN)
		{
			resume_original_attack_action();
			return;
		}

		//--------------------------------------------//

		if (original_action_x_loc < 0 || original_action_x_loc >= GameConstants.MapSize ||
		    original_action_y_loc < 0 || original_action_y_loc >= GameConstants.MapSize)
		{
			original_action_mode = 0;
			return;
		}

		// use UnitArray.attack() instead of unit.attack_???() as we are unsure about what type of object the target is.
		List<int> selectedArray = new List<int>();
		selectedArray.Add(sprite_recno);

		Location location = World.get_loc(original_action_x_loc, original_action_y_loc);

		//--------- resume assign to town -----------//

		if (original_action_mode == UnitConstants.ACTION_ASSIGN_TO_TOWN && location.is_town())
		{
			if (location.town_recno() == original_action_para &&
			    TownArray[original_action_para].NationId == nation_recno)
			{
				UnitArray.assign(original_action_x_loc, original_action_y_loc, false,
					InternalConstants.COMMAND_AUTO, selectedArray);
			}
		}

		//--------- resume assign to firm ----------//

		else if (original_action_mode == UnitConstants.ACTION_ASSIGN_TO_FIRM && location.is_firm())
		{
			if (location.firm_recno() == original_action_para &&
			    FirmArray[original_action_para].nation_recno == nation_recno)
			{
				UnitArray.assign(original_action_x_loc, original_action_y_loc, false,
					InternalConstants.COMMAND_AUTO, selectedArray);
			}
		}

		//--------- resume build firm ---------//

		else if (original_action_mode == UnitConstants.ACTION_BUILD_FIRM)
		{
			if (World.can_build_firm(original_action_x_loc, original_action_y_loc,
				    original_action_para, sprite_recno) != 0)
			{
				build_firm(original_action_x_loc, original_action_y_loc,
					original_action_para, InternalConstants.COMMAND_AUTO);
			}
		}

		//--------- resume settle ---------//

		else if (original_action_mode == UnitConstants.ACTION_SETTLE)
		{
			if (World.can_build_town(original_action_x_loc, original_action_y_loc, sprite_recno))
			{
				UnitArray.settle(original_action_x_loc, original_action_y_loc, false,
					InternalConstants.COMMAND_AUTO, selectedArray);
			}
		}

		//--------- resume move ----------//

		else if (original_action_mode == UnitConstants.ACTION_MOVE)
		{
			UnitArray.move_to(original_action_x_loc, original_action_y_loc, false,
				selectedArray, InternalConstants.COMMAND_AUTO);
		}

		original_action_mode = 0;
	}

	public void resume_original_attack_action()
	{
		if (original_action_mode == 0)
			return;

		if (original_action_mode != UnitConstants.ACTION_ATTACK_UNIT &&
		    original_action_mode != UnitConstants.ACTION_ATTACK_FIRM &&
		    original_action_mode != UnitConstants.ACTION_ATTACK_TOWN)
		{
			original_action_mode = 0;
			return;
		}

		//--------------------------------------------//

		Location location = World.get_loc(original_action_x_loc, original_action_y_loc);
		int targetNationRecno = -1;

		if (original_action_mode == UnitConstants.ACTION_ATTACK_UNIT && location.has_unit(UnitConstants.UNIT_LAND))
		{
			int unitRecno = location.unit_recno(UnitConstants.UNIT_LAND);

			if (unitRecno == original_action_para)
				targetNationRecno = UnitArray[unitRecno].nation_recno;
		}
		else if (original_action_mode == UnitConstants.ACTION_ATTACK_FIRM && location.is_firm())
		{
			int firmRecno = location.firm_recno();

			if (firmRecno == original_action_para)
				targetNationRecno = FirmArray[firmRecno].nation_recno;
		}
		else if (original_action_mode == UnitConstants.ACTION_ATTACK_TOWN && location.is_town())
		{
			int townRecno = location.town_recno();

			if (townRecno == original_action_para)
				targetNationRecno = TownArray[townRecno].NationId;
		}

		//----- the original target is no longer valid ----//

		if (targetNationRecno == -1)
		{
			original_action_mode = 0;
			return;
		}

		//---- only resume attacking the target if the target nation is at war with us currently ---//

		if (targetNationRecno == 0 || (targetNationRecno != nation_recno &&
		                           NationArray[nation_recno].get_relation_status(targetNationRecno) ==
		                           NationBase.NATION_HOSTILE))
		{
			// use UnitArray.attack() instead of unit.attack_???() as we are unsure about what type of object the target is.
			List<int> selectedArray = new List<int>();
			selectedArray.Add(sprite_recno);

			UnitArray.attack(original_action_x_loc, original_action_y_loc, false, selectedArray,
				InternalConstants.COMMAND_AI, 0);
		}

		original_action_mode = 0;
	}

	public void ask_team_help_attack(Unit attackerUnit)
	{
		//--- if the attacking unit is our unit (this can happen if the unit is porcupine) ---//

		if (attackerUnit.nation_recno == nation_recno)
			return;

		//-----------------------------------------//

		int leaderUnitRecno = 0;

		if (leader_unit_recno != 0) // if the current unit is a soldier, get its leader's recno
			leaderUnitRecno = leader_unit_recno;

		else if (team_info != null) // this unit is the commander
			leaderUnitRecno = sprite_recno;

		if (leaderUnitRecno != 0)
		{
			TeamInfo teamInfo = UnitArray[leaderUnitRecno].team_info;

			for (int i = teamInfo.member_unit_array.Count - 1; i >= 0; i--)
			{
				int unitRecno = teamInfo.member_unit_array[i];

				if (UnitArray.IsDeleted(unitRecno))
					continue;

				Unit unit = UnitArray[unitRecno];

				if (unit.cur_action == SPRITE_IDLE && unit.is_visible())
				{
					unit.attack_unit(attackerUnit.sprite_recno, 0, 0, true);
					return;
				}

				if (ConfigAdv.unit_ai_team_help && (unit.ai_unit || nation_recno == 0) &&
				    unit.is_visible() && unit.hit_points > 15.0 &&
				    (unit.action_mode == UnitConstants.ACTION_STOP ||
				     unit.action_mode == UnitConstants.ACTION_ASSIGN_TO_FIRM ||
				     unit.action_mode == UnitConstants.ACTION_ASSIGN_TO_TOWN ||
				     unit.action_mode == UnitConstants.ACTION_SETTLE))
				{
					unit.attack_unit(attackerUnit.sprite_recno, 0, 0, true);
					return;
				}
			}
		}
	}

	//------------- processing functions --------------------//

	public override void pre_process()
	{
		//------ if all the hit points are lost, die now ------//
		if (hit_points <= 0.0 && action_mode != UnitConstants.ACTION_DIE)
		{
			set_die();

			if (ai_action_id != 0 && nation_recno != 0)
				NationArray[nation_recno].action_failure(ai_action_id, sprite_recno);

			return;
		}

		if (Config.fog_of_war)
		{
			if (is_own() || (nation_recno != 0 && NationArray[nation_recno].is_allied_with_player))
			{
				World.visit(next_x_loc(), next_y_loc(),
					next_x_loc() + sprite_info.loc_width - 1, next_y_loc() + sprite_info.loc_height - 1,
					UnitRes[unit_id].visual_range, UnitRes[unit_id].visual_extend);
			}
		}

		//--------- process action corresponding to action_mode ----------//

		switch (action_mode)
		{
			case UnitConstants.ACTION_ATTACK_UNIT:
				//------------------------------------------------------------------//
				// if unit is in defense mode, check situation to follow the target
				// or return back to camp
				//------------------------------------------------------------------//
				if (action_mode != action_mode2)
				{
					if (action_mode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET)
					{
						if (!defense_follow_target()) // false if abort attacking
							break; // cancel attack and go back to military camp
					}
					else if (action_mode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET)
					{
						if (!defend_town_follow_target())
							break;
					}
					else if (action_mode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
					{
						if (!monster_defend_follow_target())
							break;
					}
				}

				process_attack_unit();
				break;

			case UnitConstants.ACTION_ATTACK_FIRM:
				process_attack_firm();
				break;

			case UnitConstants.ACTION_ATTACK_TOWN:
				process_attack_town();
				break;

			case UnitConstants.ACTION_ATTACK_WALL:
				process_attack_wall();
				break;

			case UnitConstants.ACTION_ASSIGN_TO_FIRM:
			case UnitConstants.ACTION_ASSIGN_TO_TOWN:
			case UnitConstants.ACTION_ASSIGN_TO_VEHICLE:
				process_assign();
				break;

			case UnitConstants.ACTION_ASSIGN_TO_SHIP:
				process_assign_to_ship();
				break;

			case UnitConstants.ACTION_BUILD_FIRM:
				process_build_firm();
				break;

			case UnitConstants.ACTION_BURN:
				process_burn();
				break;

			case UnitConstants.ACTION_SETTLE:
				process_settle();
				break;

			case UnitConstants.ACTION_SHIP_TO_BEACH:
				process_ship_to_beach();
				break;

			case UnitConstants.ACTION_GO_CAST_POWER:
				process_go_cast_power();
				break;
		}

		//-****** don't add code here, the unit may be removed after the above function call*******-//
	}

	public override void process_idle() // derived function of Sprite
	{
		//---- if the unit is defending the town ----//

		switch (unit_mode)
		{
			case UnitConstants.UNIT_MODE_REBEL:
				if (action_mode2 == UnitConstants.ACTION_STOP)
				{
					process_rebel(); // redirect to process_rebel for rebel units
					return;
				}

				break;

			case UnitConstants.UNIT_MODE_MONSTER:
				if (action_mode2 == UnitConstants.ACTION_STOP)
				{
					if (unit_mode_para != 0)
					{
						if (!FirmArray.IsDeleted(unit_mode_para))
						{
							//-------- return to monster firm -----------//
							FirmMonster monsterFirm = (FirmMonster)FirmArray[unit_mode_para];
							assign(monsterFirm.loc_x1, monsterFirm.loc_y1);
							return;
						}
						else
						{
							unit_mode_para = 0;
						}
					}
				}

				break;
		}

		//------------- process way point ------------//
		if (action_mode == UnitConstants.ACTION_STOP && action_mode2 == UnitConstants.ACTION_STOP &&
		    wayPoints.Count > 0)
		{
			if (wayPoints.Count == 1)
				ResetWayPoints();
			else
				ProcessWayPoint();
			return;
		}

		//-------- randomize direction --------//

		//-------------------------------------------------------//
		// when the unit is idle, the following should be always true
		// move_to_x_loc == next_x_loc()
		// move_to_y_loc == next_y_loc()
		//-------------------------------------------------------//

		//move_to_x_loc = next_x_loc();     //***************BUGHERE
		//move_to_y_loc = next_y_loc();

		if (match_dir())
		{
			if (!is_guarding() && race_id != 0) // only these units can move
			{
				if (Misc.Random(150) == 0) // change direction randomly
					set_dir(Misc.Random(8));
			}
		}
		else
		{
			return;
		}

		//------- call Sprite::process_idle() -------//

		base.process_idle();

		//---------------------------------------------------------------------------//
		// reset idle blocked attacking unit.  If the previous condition is totally
		// blocked for attacking target, try again now
		// Note: reset blocked_edge is essentail for idle unit to reactivate attack
		// action
		//---------------------------------------------------------------------------//
		if (action_mode >= UnitConstants.ACTION_ATTACK_UNIT && action_mode <= UnitConstants.ACTION_ATTACK_WALL)
		{
			bool isAllZero = true;
			for (int i = 0; i < blocked_edge.Length; i++)
			{
				if (blocked_edge[i] != 0)
					isAllZero = false;
			}

			if (UnitArray.idle_blocked_unit_reset_count != 0 && !isAllZero)
			{
				UnitArray.idle_blocked_unit_reset_count = 0;
				for (int i = 0; i < blocked_edge.Length; i++)
				{
					blocked_edge[i] = 0;
				}
			}
		}

		//--------- reactivate action -----------//

		if (reactivate_idle_action())
			return; // true if an action is reactivated

		//----------- for ai unit idle -----------//

		// only detect attack when the unit is really idle
		if (action_mode != UnitConstants.ACTION_STOP || action_mode2 != UnitConstants.ACTION_STOP)
			return;

		if (!can_attack())
			return;

		//--- only detect attack if in aggressive mode or the unit is a monster ---//

		UnitInfo unitInfo = UnitRes[unit_id];

		if (unitInfo.unit_class == UnitConstants.UNIT_CLASS_MONSTER || aggressive_mode != 0)
		{
			//----------- detect target to attack -----------//

			if (idle_detect_attack())
				return; // target detected

		}

		//------------------------------------------------------------------//
		// wander around for monster
		//------------------------------------------------------------------//

		if (unitInfo.unit_class == UnitConstants.UNIT_CLASS_MONSTER)
		{
			if (Misc.Random(500) == 0)
			{
				const int WANDER_DIST = 20;

				int xOffset = Misc.Random(WANDER_DIST) - WANDER_DIST / 2;
				int yOffset = Misc.Random(WANDER_DIST) - WANDER_DIST / 2;
				int destX = next_x_loc() + xOffset;
				int destY = next_y_loc() + yOffset;

				if (destX < 0)
					destX = 0;
				else if (destX >= GameConstants.MapSize)
					destX = GameConstants.MapSize - 1;

				if (destY < 0)
					destY = 0;
				else if (destY >= GameConstants.MapSize)
					destY = GameConstants.MapSize - 1;

				move_to(destX, destY);
			}
		}
	}

	public override void process_move() // derived function of Sprite
	{
		//----- if the sprite has reach the destintion ----//

		//--------------------------------------------------------//
		// if the unit reach its destination, then
		// cur_? == next_? == go_?
		//--------------------------------------------------------//

		if (cur_x == go_x && cur_y == go_y)
		{
			if (result_node_array != null)
			{
				next_move();
				if (cur_action != SPRITE_MOVE) // if next_move() is not successful, the movement has been stopped
					return;

				//---------------------------------------------------------------------------//
				// If (1) the unit is blocked at cur_? == go_? and go_? != destination and 
				//		(2) a new path is generated if calling the previous next_move(),
				//	then cur_? still equal to go_?.
				//
				// The following Sprite::process_move() call will set the unit to SPRITE_IDLE 
				// since cur_? == go_?. Thus, the unit terminates its move although it has not
				// reached its destination.
				//
				// (note: if it has reached its destination, cur_? == go_? and cur_action =
				//			 SPRITE_IDLE)
				//
				// if the unit is still moving and cur_? == go_?, call next_move() again to reset
				// the go_?.
				//---------------------------------------------------------------------------//
				if (cur_action == SPRITE_MOVE && cur_x == go_x && cur_y == go_y)
					next_move();
			}
		}

		//--------- process the move, update sprite position ---------//
		//--------------------------------------------------------//
		// if the unit is moving, cur_?!=go_? and
		// if next_? != cur_?, the direction from cur_? to next_?
		// should equal to that from cur_? to go_?
		//--------------------------------------------------------//

		base.process_move();

		if (cur_x == go_x && cur_y == go_y && cur_action == SPRITE_IDLE) // the sprite has reached its destination
		{
			move_to_x_loc = next_x_loc();
			move_to_y_loc = next_y_loc();
		}

		//--------------------------------------------------------//
		// after Sprite::process_move(), if the unit is blocked, its
		// cur_action is set to SPRITE_WAIT. Otherwise, its cur_action
		// is still SPRITE_MOVE.  Then cur_? != next_? if the unit
		// has not reached its destination.
		//--------------------------------------------------------//
	}

	public override void process_wait() // derived function of Sprite
	{
		if (!match_dir())
			return;

		//-----------------------------------------------------//
		// If the unit is moving to the destination and was
		// blocked by something. If it is now no longer blocked,
		// continue the movement.
		//-----------------------------------------------------//
		//
		// When this funciton is called:
		//
		// (next_x, next_y)==(cur_x, cur_y), it's the location of the sprite.
		//
		//-----------------------------------------------------//

		//--- find out the next location which the sprite should be moving towards ---//

		//-----------------------------------------------------//
		// If the unit is waiting,
		//		go_? != cur_?
		//		go_? != next_?
		//
		// If the unit is not under swapping, the next_?_loc()
		//	is always the move_to_?_loc. Thus, the unit is ordered
		//	to stop.
		//-----------------------------------------------------//

		if (next_x_loc() == move_to_x_loc && next_y_loc() == move_to_y_loc && swapping == 0)
		{
			terminate_move();
			return; // terminate since already in destination
		}

		int stepMagn = move_step_magn();
		int nextX = cur_x + stepMagn * move_x_pixel_array[final_dir];
		int nextY = cur_y + stepMagn * move_y_pixel_array[final_dir];

		/*short w, h, blocked=0;
		short x, y, blockedX, blockedY;
		Location* loc;
	
		//---------- check whether the unit is blocked -----------//
		for(h=0, y=nextY>>InternalConstants.CellHeightShift; h<sprite_info.loc_height && !blocked; h++, y++)
		{
			for(w=0, x=nextX>>InternalConstants.CellWidthShift; w<sprite_info.loc_width && !blocked; w++, x++)
			{
				loc = world.get_loc(x, y);
				blocked = ( (!loc.is_accessible(mobile_type)) || (loc.has_unit(mobile_type) &&
								loc.unit_recno(mobile_type)!=sprite_recno) );
	
				if(blocked)
				{
					blockedX = x;
					blockedY = y;
				}
			}
		}*/
		int x = nextX >> InternalConstants.CellWidthShift;
		int y = nextY >> InternalConstants.CellHeightShift;
		Location location = World.get_loc(x, y);
		bool blocked = !location.is_accessible(mobile_type) ||
		               (location.has_unit(mobile_type) && location.unit_recno(mobile_type) != sprite_recno);

		if (!blocked || move_action_call_flag)
		{
			//--------- not blocked, continue to move --------//
			waiting_term = 0;
			set_move();
			cur_frame = 1;
			set_next(nextX, nextY, -stepMagn, 1);
		}
		else
		{
			//------- blocked, call handle_blocked_move() ------//
			//loc = world.get_loc(blockedX, blockedY);
			handle_blocked_move(location);
		}
	}

	// derived function of Sprite, for ship only
	public override void process_extra_move()
	{
	}

	public override bool process_die()
	{
		//--------- voice ------------//
		SERes.sound(cur_x_loc(), cur_y_loc(), cur_frame, 'S', sprite_id, "DIE");

		//------------- add die effect on first frame --------- //
		if (cur_frame == 1 && UnitRes[unit_id].die_effect_id != 0)
		{
			EffectArray.AddEffect(UnitRes[unit_id].die_effect_id, cur_x, cur_y,
				SPRITE_DIE, cur_dir, mobile_type == UnitConstants.UNIT_AIR ? 8 : 2, 0);
		}

		//--------- next frame ---------//
		if (++cur_frame > sprite_info.die.frame_count)
			return true;

		return false;
	}

	public virtual void next_day()
	{
		int unitRecno = sprite_recno;

		//------- functions for non-independent nations only ------//

		if (nation_recno != 0)
		{
			pay_expense();

			if (UnitArray.IsDeleted(unitRecno)) // if its hit points go down to 0, IsDeleted() will return 1.
				return;

			//------- update loyalty -------------//

			if (Info.TotalDays % 30 == sprite_recno % 30)
			{
				update_loyalty();
			}

			//------- think about rebeling -------------//

			if (Info.TotalDays % 15 == sprite_recno % 15)
			{
				if (think_betray())
					return;
			}
		}

		//------- recover from damage -------//

		if (Info.TotalDays % 15 == sprite_recno % 15) // recover one point per two weeks
		{
			process_recover();
		}

		//------- restore cur_power --------//

		cur_power += 5;

		if (cur_power > max_power)
			cur_power = max_power;

		//------- king undie flag (for testing games only) --------//

		if (Config.king_undie_flag && rank_id == RANK_KING && nation_recno != 0 && !NationArray[nation_recno].is_ai())
			hit_points = max_hit_points;

		//-------- if aggresive_mode is 1 --------//

		if (nation_recno != 0 && is_visible())
			think_aggressive_action();


		if (skill.combat_level > 100)
			skill.combat_level = 100;
	}

	public override void set_next(int newNextX, int newNextY, int para = 0, int blockedChecked = 0)
	{
		int curNextXLoc = next_x_loc();
		int curNextYLoc = next_y_loc();
		int newNextXLoc = newNextX >> InternalConstants.CellWidthShift;
		int newNextYLoc = newNextY >> InternalConstants.CellHeightShift;

		if (curNextXLoc != newNextXLoc || curNextYLoc != newNextYLoc)
		{
			if (!match_dir())
			{
				set_wait();
				return;
			}
		}

		bool blocked = false;
		int x = -1, y = -1;

		if (curNextXLoc != newNextXLoc || curNextYLoc != newNextYLoc)
		{
			//------- if the next location is blocked ----------//

			if (blockedChecked == 0)
			{
				/*for(h=0, y=newNextYLoc; h<sprite_info.loc_height && !blocked; h++, y++)
				{
					for(w=0, x=newNextXLoc; w<sprite_info.loc_width && !blocked; w++, x++)
					{
						loc = world.get_loc(x, y);
						blocked = ( (!loc.is_accessible(mobile_type)) || (loc.has_unit(mobile_type) &&
										loc.unit_recno(mobile_type)!=sprite_recno) );
	
						if(blocked)
						{
							blockedX = x;
							blockedY = y;
						}
					}
				}*/
				x = newNextXLoc;
				y = newNextYLoc;
				Location location = World.get_loc(x, y);
				blocked = !location.is_accessible(mobile_type) ||
				          (location.has_unit(mobile_type) && location.unit_recno(mobile_type) != sprite_recno);
			} //else, then blockedChecked = 0

			//--- no change to next_x & next_y if the new next location is blocked ---//

			if (blocked)
			{
				set_cur(next_x, next_y); // align the sprite to 32x32 location when it stops

				//------ avoid infinitely looping in calling handle_blocked_move() ------//
				if (blocked_by_member != 0 || move_action_call_flag)
				{
					set_wait();
					blocked_by_member = 0;
				}
				else
				{
					Location location = World.get_loc(x, y);
					handle_blocked_move(location);
				}
			}
			else
			{
				if (para != 0)
				{
					//----------------------------------------------------------------------------//
					// calculate the result_path_dist as the unit move from one tile to another
					//----------------------------------------------------------------------------//
					result_path_dist += para;
				}

				next_x = newNextX;
				next_y = newNextY;

				swapping = blocked_by_member = 0;

				//---- move sprite_recno to the next location ------//

				y = curNextYLoc;
				for (int h = 0; h < sprite_info.loc_height; h++, y++)
				{
					x = curNextXLoc;
					for (int w = 0; w < sprite_info.loc_width; w++, x++)
					{
						World.set_unit_recno(x, y, mobile_type, 0);
					}
				}

				y = next_y_loc();
				for (int h = 0; h < sprite_info.loc_height; h++, y++)
				{
					x = next_x_loc();
					for (int w = 0; w < sprite_info.loc_width; w++, x++)
					{
						World.set_unit_recno(x, y, mobile_type, sprite_recno);
					}
				}

				//--------- explore land ----------//

				if (!Config.explore_whole_map && is_own())
				{
					int xLoc1 = Math.Max(0, newNextXLoc - GameConstants.EXPLORE_RANGE);
					int yLoc1 = Math.Max(0, newNextYLoc - GameConstants.EXPLORE_RANGE);
					int xLoc2 = Math.Min(GameConstants.MapSize - 1, newNextXLoc + GameConstants.EXPLORE_RANGE);
					int yLoc2 = Math.Min(GameConstants.MapSize - 1, newNextYLoc + GameConstants.EXPLORE_RANGE);
					int exploreWidth = move_step_magn() - 1;

					if (newNextYLoc < curNextYLoc) // if move upwards, explore upper area
						World.explore(xLoc1, yLoc1, xLoc2, yLoc1 + exploreWidth);

					else if (newNextYLoc > curNextYLoc) // if move downwards, explore lower area
						World.explore(xLoc1, yLoc2 - exploreWidth, xLoc2, yLoc2);

					if (newNextXLoc < curNextXLoc) // if move towards left, explore left area
						World.explore(xLoc1, yLoc1, xLoc1 + exploreWidth, yLoc2);

					else if (newNextXLoc > curNextXLoc) // if move towards right, explore right area
						World.explore(xLoc2 - exploreWidth, yLoc1, xLoc2, yLoc2);
				}
			}
		}
	}

	//------------------------------------//

	public bool return_camp()
	{
		if (home_camp_firm_recno == 0)
			return false;

		Firm firm = FirmArray[home_camp_firm_recno];

		if (firm.region_id != region_id())
			return false;

		//--------- assign now ---------//

		assign(firm.loc_x1, firm.loc_y1);

		force_move_flag = true;

		return cur_action != SPRITE_IDLE;
	}

	//----------- parameters reseting functions ------------//
	public virtual void stop(int preserveAction = 0)
	{
		//------------- reset vars ------------//
		if (action_mode2 != UnitConstants.ACTION_MOVE)
			ResetWayPoints();

		reset_path();

		//-------------- keep action or not --------------//
		switch (preserveAction)
		{
			case 0:
			case UnitConstants.KEEP_DEFENSE_MODE:
				reset_action_para();
				range_attack_x_loc = range_attack_y_loc = -1; // should set here or reset for attack
				break;

			case UnitConstants.KEEP_PRESERVE_ACTION:
				break;

			/*case 3:
				err_when(action_mode!=ACTION_ATTACK_UNIT);
				go_x = next_x;	// combine move_to_attack() into move_to()
				go_y = next_y;
				move_to_x_loc = next_x_loc();
				move_to_y_loc = next_y_loc();
				return;*/
		}

		waiting_term = 0; // for idle_detect_attack(), oscillate between 0 and 1

		//----------------- update parameters ----------------//
		switch (cur_action)
		{
			//----- if the unit is moving right now, ask it to stop as soon as possible -----//

			case SPRITE_READY_TO_MOVE:
				set_idle();
				break;

			case SPRITE_TURN:
			case SPRITE_WAIT:
				go_x = next_x;
				go_y = next_y;
				move_to_x_loc = next_x_loc();
				move_to_y_loc = next_y_loc();
				final_dir = cur_dir;
				turn_delay = 0;
				set_idle();
				break;

			case SPRITE_SHIP_EXTRA_MOVE:
				UnitMarine ship = (UnitMarine)this;
				switch (ship.extra_move_in_beach)
				{
					case UnitMarine.NO_EXTRA_MOVE:
						if (cur_x == next_x && cur_y == next_y)
						{
							go_x = next_x;
							go_y = next_y;
							move_to_x_loc = next_x_loc();
							move_to_y_loc = next_y_loc();
							set_idle();
							return;
						}

						break;

					case UnitMarine.EXTRA_MOVING_IN:
						if (cur_x == next_x && cur_y == next_y && (cur_x != go_x || cur_y != go_y))
						{
							// not yet move although location is chosed
							ship.extra_move_in_beach = UnitMarine.NO_EXTRA_MOVE;
						}

						break;

					case UnitMarine.EXTRA_MOVING_OUT:
						if (cur_x == next_x && cur_y == next_y && (cur_x != go_x || cur_y != go_y))
						{
							// not yet move although location is chosed
							ship.extra_move_in_beach = UnitMarine.EXTRA_MOVE_FINISH;
						}

						break;

					case UnitMarine.EXTRA_MOVE_FINISH:
						break;
				}

				go_x = next_x;
				go_y = next_y;
				move_to_x_loc = next_x_loc();
				move_to_y_loc = next_y_loc();
				break;

			case SPRITE_MOVE:
				go_x = next_x;
				go_y = next_y;
				move_to_x_loc = next_x_loc();
				move_to_y_loc = next_y_loc();
				if (cur_x == next_x && cur_y == next_y)
					set_idle();
				break;

			//--- if its current action is SPRITE_ATTACK, stop immediately ---//

			case SPRITE_ATTACK:
				set_next(cur_x, cur_y, 0, 1); //********** BUGHERE
				go_x = next_x;
				go_y = next_y;
				move_to_x_loc = next_x_loc();
				move_to_y_loc = next_y_loc();
				set_idle();

				cur_frame = 1;
				break;
		}
	}

	public void stop2(int preserveAction = 0)
	{
		stop(preserveAction);
		reset_action_para2(preserveAction);

		//----------------------------------------------------------//
		// set the original location of the attacking target when
		// the attack() function is called, action_x_loc2 & action_y_loc2
		// will change when the unit move, but these two will not.
		//----------------------------------------------------------//

		force_move_flag = false;
		ai_no_suitable_action = false;

		if (preserveAction == 0)
		{
			original_action_mode = 0;
			ai_original_target_x_loc = -1;

			if (ai_action_id != 0 && nation_recno != 0)
				NationArray[nation_recno].action_failure(ai_action_id, sprite_recno);
		}
	}

	public void reset_action_para() // reset action_mode parameters
	{
		action_mode = UnitConstants.ACTION_STOP;
		action_x_loc = action_y_loc = -1;
		action_para = 0;
	}

	public void reset_action_para2(int keepMode = 0) // reset action_mode2 parameters
	{
		if (keepMode != UnitConstants.KEEP_DEFENSE_MODE || !in_any_defense_mode())
		{
			action_mode2 = UnitConstants.ACTION_STOP;
			action_para2 = 0;
			action_x_loc2 = action_y_loc2 = -1;
		}
		else
		{
			switch (unit_mode)
			{
				case UnitConstants.UNIT_MODE_DEFEND_TOWN:
					if (action_mode2 != UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET)
						defend_town_detect_target();
					break;

				case UnitConstants.UNIT_MODE_REBEL:
					if (action_mode2 != UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET)
						defense_detect_target();
					break;

				case UnitConstants.UNIT_MODE_MONSTER:
					if (action_mode2 != UnitConstants.ACTION_MONSTER_DEFEND_DETECT_TARGET)
						monster_defend_detect_target();
					break;
			}
		}
	}

	public void reset_action_misc_para()
	{
		action_misc = UnitConstants.ACTION_MISC_STOP;
		action_misc_para = 0;
	}

	//--------------- die actions --------------//
	public bool is_unit_dead()
	{
		return (hit_points <= 0.0 || action_mode == UnitConstants.ACTION_DIE || cur_action == SPRITE_DIE);
	}

	//------------ movement action -----------------//
	public void move_to(int destX, int destY, int preserveAction = 0, int searchMode = SeekPath.SEARCH_MODE_IN_A_GROUP, int miscNo = 0, int numOfPath = 1)
	{
		//---------- reset way point array since new action is assigned --------//
		if (wayPoints.Count > 0)
		{
			ResultNode node = wayPoints[0];
			if (node.node_x != destX || node.node_y != destY)
				ResetWayPoints();
		}

		//----------------------------------------------------------------//
		// calculate new destination if trying to move to different territory
		//----------------------------------------------------------------//
		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();
		Location loc = World.get_loc(curXLoc, curYLoc);
		Location destLoc = World.get_loc(destX, destY);
		int destXLoc = destX;
		int destYLoc = destY;

		if (loc.region_id != destLoc.region_id && mobile_type != UnitConstants.UNIT_AIR) // different territory
			different_territory_destination(ref destXLoc, ref destYLoc);

		if (is_unit_dead())
			return;

		//-----------------------------------------------------------------------------------//
		// The codes here is used to check for equal action in movement.
		//
		// mainly checked by action_mode2. If previous action is ACTION_MOVE, action_mode2,
		// action_para2, action_x_loc2 and action_x_loc2 need to be kept for this checking.
		//
		// If calling from UnitArray.move_to(), action_mode is set to ACTION_MOVE, action_para
		// is set to 0 while action_x_loc and action_y_loc are kept as original value for checking.
		// Meanwhile, action_mode2, action_para2, action_x_loc2 and action_y_loc2 are kept if
		// the condition is fulfilled (action_mode2==ACTION_MOVE)
		//-----------------------------------------------------------------------------------//
		if (action_mode2 == UnitConstants.ACTION_MOVE && action_mode == UnitConstants.ACTION_MOVE)
		{
			//------ previous action is ACTION_MOVE -------//
			if (action_x_loc2 == destXLoc && action_y_loc2 == destYLoc)
			{
				//-------- equal order --------//
				action_x_loc = action_x_loc2;
				action_y_loc = action_y_loc2;

				if (cur_action != SPRITE_IDLE)
				{
					//-------- the old order is processing --------//
					if (result_node_array == null) // cannot move
					{
						if (UnitRes[unit_id].unit_class == UnitConstants.UNIT_CLASS_SHIP)
						{
							if (cur_action != SPRITE_SHIP_EXTRA_MOVE)
							{
								if (cur_x != next_x || cur_y != next_y)
									set_move();
								else
									set_idle();
							}
							//else keep extra_moving
						}
						else
						{
							if (cur_x != next_x || cur_y != next_y)
								set_move();
							else
								set_idle();
						}
					}

					return;
				} //else action is hold due to some problems, re-activiate again
			}
		} //else, new order or searching is required

		move_action_call_flag = true; // set flag to avoid calling move_to_my_loc()

		action_mode2 = UnitConstants.ACTION_MOVE;
		action_para2 = 0;

		search(destXLoc, destYLoc, preserveAction, searchMode, miscNo, numOfPath);
		move_action_call_flag = false;

		//----------------------------------------------------------------//
		// store new order in action parameters
		//----------------------------------------------------------------//
		action_mode = UnitConstants.ACTION_MOVE;
		action_para = 0;
		action_x_loc = action_x_loc2 = move_to_x_loc;
		action_y_loc = action_y_loc2 = move_to_y_loc;
	}

	public void move_to_unit_surround(int destXLoc, int destYLoc, int width, int height, int miscNo = 0, int readyDist = 0)
	{
		//----------------------------------------------------------------//
		// calculate new destination if trying to move to different territory
		//----------------------------------------------------------------//
		Location loc = World.get_loc(destXLoc, destYLoc);
		if (World.get_loc(next_x_loc(), next_y_loc()).region_id != loc.region_id)
		{
			move_to(destXLoc, destYLoc);
			return;
		}

		//----------------------------------------------------------------//
		// return if the unit is dead
		//----------------------------------------------------------------//
		if (is_unit_dead())
			return;

		//----------------------------------------------------------------//
		// check for equal actions
		//----------------------------------------------------------------//
		if (action_mode2 == UnitConstants.ACTION_MOVE && action_mode == UnitConstants.ACTION_MOVE)
		{
			//------ previous action is ACTION_MOVE -------//
			if (action_x_loc2 == destXLoc && action_y_loc2 == destYLoc)
			{
				//-------- equal order --------//
				action_x_loc = action_x_loc2;
				action_y_loc = action_y_loc2;

				if (cur_action != SPRITE_IDLE)
				{
					//-------- the old order is processing --------//
					if (result_node_array == null) // cannot move
					{
						set_idle();
					}

					return;
				} //else action is hold due to some problems, re-activiate again
			}
		} //else, new order or searching is required

		int destX = Math.Max(0, ((width > 1) ? destXLoc : destXLoc - width + 1));
		int destY = Math.Max(0, ((height > 1) ? destYLoc : destYLoc - height + 1));

		Unit unit = UnitArray[miscNo];
		SpriteInfo spriteInfo = unit.sprite_info;
		stop();
		set_move_to_surround(destX, destY, spriteInfo.loc_width, spriteInfo.loc_height, UnitConstants.BUILDING_TYPE_VEHICLE);

		//----------------------------------------------------------------//
		// store new order in action parameters
		//----------------------------------------------------------------//
		action_mode = action_mode2 = UnitConstants.ACTION_MOVE;
		action_para = action_para2 = 0;
		action_x_loc = action_x_loc2 = move_to_x_loc;
		action_y_loc = action_y_loc2 = move_to_y_loc;
	}

	public void move_to_firm_surround(int destXLoc, int destYLoc, int width, int height, int miscNo = 0, int readyDist = 0)
	{
		//----------------------------------------------------------------//
		// calculate new destination if trying to move to different territory
		//----------------------------------------------------------------//
		Location loc = World.get_loc(destXLoc, destYLoc);
		if (UnitRes[unit_id].unit_class == UnitConstants.UNIT_CLASS_SHIP && miscNo == Firm.FIRM_HARBOR)
		{
			Firm firm = FirmArray[loc.firm_recno()];
			FirmHarbor harbor = (FirmHarbor)firm;
			if (World.get_loc(next_x_loc(), next_y_loc()).region_id != harbor.sea_region_id)
			{
				move_to(destXLoc, destYLoc);
				return;
			}
		}
		else
		{
			if (World.get_loc(next_x_loc(), next_y_loc()).region_id != loc.region_id)
			{
				move_to(destXLoc, destYLoc);
				return;
			}
		}

		//----------------------------------------------------------------//
		// return if the unit is dead
		//----------------------------------------------------------------//
		if (is_unit_dead())
			return;

		//----------------------------------------------------------------//
		// check for equal actions
		//----------------------------------------------------------------//
		if (action_mode2 == UnitConstants.ACTION_MOVE && action_mode == UnitConstants.ACTION_MOVE)
		{
			//------ previous action is ACTION_MOVE -------//
			if (action_x_loc2 == destXLoc && action_y_loc2 == destYLoc)
			{
				//-------- equal order --------//
				action_x_loc = action_x_loc2;
				action_y_loc = action_y_loc2;

				if (cur_action != SPRITE_IDLE)
				{
					//-------- the old order is processing --------//
					if (result_node_array == null) // cannot move
					{
						set_idle();
					}

					return;
				} //else action is hold due to some problems, re-activiate again
			}
		} //else, new order or searching is required

		int destX = Math.Max(0, ((width > 1) ? destXLoc : destXLoc - width + 1));
		int destY = Math.Max(0, ((height > 1) ? destYLoc : destYLoc - height + 1));

		FirmInfo firmInfo = FirmRes[miscNo];
		stop();
		set_move_to_surround(destX, destY, firmInfo.loc_width, firmInfo.loc_height, UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO, miscNo);

		//----------------------------------------------------------------//
		// store new order in action parameters
		//----------------------------------------------------------------//
		action_mode = action_mode2 = UnitConstants.ACTION_MOVE;
		action_para = action_para2 = 0;
		action_x_loc = action_x_loc2 = move_to_x_loc;
		action_y_loc = action_y_loc2 = move_to_y_loc;
	}

	public void move_to_town_surround(int destXLoc, int destYLoc, int width, int height, int miscNo = 0, int readyDist = 0)
	{
		//----------------------------------------------------------------//
		// calculate new destination if trying to move to different territory
		//----------------------------------------------------------------//
		Location loc = World.get_loc(destXLoc, destYLoc);
		if (World.get_loc(next_x_loc(), next_y_loc()).region_id != loc.region_id)
		{
			move_to(destXLoc, destYLoc);
			return;
		}

		//----------------------------------------------------------------//
		// return if the unit is dead
		//----------------------------------------------------------------//
		if (is_unit_dead())
			return;

		//----------------------------------------------------------------//
		// check for equal actions
		//----------------------------------------------------------------//
		if (action_mode2 == UnitConstants.ACTION_MOVE && action_mode == UnitConstants.ACTION_MOVE)
		{
			//------ previous action is ACTION_MOVE -------//
			if (action_x_loc2 == destXLoc && action_y_loc2 == destYLoc)
			{
				//-------- equal order --------//
				action_x_loc = action_x_loc2;
				action_y_loc = action_y_loc2;

				if (cur_action != SPRITE_IDLE)
				{
					//-------- the old order is processing --------//
					if (result_node_array == null) // cannot move
					{
						set_idle();
					}

					return;
				} //else action is hold due to some problems, re-activiate again
			}
		} //else, new order or searching is required

		int destX = Math.Max(0, ((width > 1) ? destXLoc : destXLoc - width + 1));
		int destY = Math.Max(0, ((height > 1) ? destYLoc : destYLoc - height + 1));

		stop();
		set_move_to_surround(destX, destY, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, UnitConstants.BUILDING_TYPE_TOWN_MOVE_TO);

		//----------------------------------------------------------------//
		// store new order in action parameters
		//----------------------------------------------------------------//
		action_mode = action_mode2 = UnitConstants.ACTION_MOVE;
		action_para = action_para2 = 0;
		action_x_loc = action_x_loc2 = move_to_x_loc;
		action_y_loc = action_y_loc2 = move_to_y_loc;
	}

	public void move_to_wall_surround(int destXLoc, int destYLoc, int width, int height, int miscNo = 0, int readyDist = 0)
	{
		//----------------------------------------------------------------//
		// calculate new destination if trying to move to different territory
		//----------------------------------------------------------------//
		Location loc = World.get_loc(destXLoc, destYLoc);
		if (World.get_loc(next_x_loc(), next_y_loc()).region_id != loc.region_id)
		{
			move_to(destXLoc, destYLoc);
			return;
		}

		//----------------------------------------------------------------//
		// return if the unit is dead
		//----------------------------------------------------------------//
		if (is_unit_dead())
			return;

		//----------------------------------------------------------------//
		// check for equal actions
		//----------------------------------------------------------------//
		if (action_mode2 == UnitConstants.ACTION_MOVE && action_mode == UnitConstants.ACTION_MOVE)
		{
			//------ previous action is ACTION_MOVE -------//
			if (action_x_loc2 == destXLoc && action_y_loc2 == destYLoc)
			{
				//-------- equal order --------//
				action_x_loc = action_x_loc2;
				action_y_loc = action_y_loc2;

				if (cur_action != SPRITE_IDLE)
				{
					//-------- the old order is processing --------//
					if (result_node_array == null) // cannot move
					{
						set_idle();
					}

					return;
				} //else action is hold due to some problems, re-activiate again
			}
		} //else, new order or searching is required

		int destX = Math.Max(0, ((width > 1) ? destXLoc : destXLoc - width + 1));
		int destY = Math.Max(0, ((height > 1) ? destYLoc : destYLoc - height + 1));

		stop();
		set_move_to_surround(destX, destY, 1, 1, UnitConstants.BUILDING_TYPE_WALL);

		//----------------------------------------------------------------//
		// store new order in action parameters
		//----------------------------------------------------------------//
		action_mode = action_mode2 = UnitConstants.ACTION_MOVE;
		action_para = action_para2 = 0;
		action_x_loc = action_x_loc2 = move_to_x_loc;
		action_y_loc = action_y_loc2 = move_to_y_loc;
	}

	public void enable_force_move()
	{
		force_move_flag = true;
	}

	public void disable_force_move()
	{
		force_move_flag = false;
	}

	public void select_search_sub_mode(int sx, int sy, int dx, int dy, int nationRecno, int searchMode)
	{
		if (!ConfigAdv.unit_allow_path_power_mode)
		{
			// cancel the selection
			SeekPath.set_sub_mode();
			return;
		}

		if (nation_recno == 0 || ignore_power_nation != 0)
		{
			SeekPath.set_sub_mode(); // always using normal mode for independent unit
			return;
		}

		//--------------------------------------------------------------------------------//
		// Checking for starting location and destination to determine sub_mode used
		// N - not hostile, H - hostile
		// 1) N . N, using normal mode
		// 2) N . H, H . N, H . H, using sub_mode SEARCH_SUB_MODE_PASSABLE
		//--------------------------------------------------------------------------------//
		Location startLoc = World.get_loc(sx, sy);
		Location destLoc = World.get_loc(dx, dy);
		Nation nation = NationArray[nationRecno];

		bool subModeOn = (startLoc.power_nation_recno == 0 || nation.get_relation_passable(startLoc.power_nation_recno)) &&
		                 (destLoc.power_nation_recno == 0 || nation.get_relation_passable(destLoc.power_nation_recno));

		if (subModeOn) // true only when both start and end locations are passable for this nation
		{
			SeekPath.set_nation_passable(nation.relation_passable_array);
			SeekPath.set_sub_mode(SeekPath.SEARCH_SUB_MODE_PASSABLE);
		}
		else
		{
			SeekPath.set_sub_mode(); //----- normal sub mode, normal searching
		}
	}

	// calculate new destination for move to location on different territory
	public void different_territory_destination(ref int destX, ref int destY)
	{
		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();

		Location loc = World.get_loc(curXLoc, curYLoc);
		int regionId = loc.region_id;
		int xStep = destX - curXLoc;
		int yStep = destY - curYLoc;
		int absXStep = Math.Abs(xStep);
		int absYStep = Math.Abs(yStep);
		int count = (absXStep >= absYStep) ? absXStep : absYStep;

		int sameTerr = 0;

		//------------------------------------------------------------------------------//
		// draw a line from the unit location to the destination, find the last location
		// with the same region id.
		//------------------------------------------------------------------------------//
		for (int i = 1; i <= count; i++)
		{
			int x = curXLoc + (i * xStep) / count;
			int y = curYLoc + (i * yStep) / count;

			loc = World.get_loc(x, y);
			if (loc.region_id == regionId)
				sameTerr = i;
		}

		if (sameTerr != 0 && count != 0)
		{
			destX = curXLoc + (sameTerr * xStep) / count;
			destY = curYLoc + (sameTerr * yStep) / count;
		}
		else
		{
			destX = curXLoc;
			destY = curYLoc;
		}
	}

	//----------------- attack action ----------------//
	public void attack_unit(int targetXLoc, int targetYLoc, int xOffset, int yOffset, bool resetBlockedEdge)
	{
		Location loc = World.get_loc(targetXLoc, targetYLoc);

		//--- AI attacking a nation which its NationRelation::should_attack is 0 ---//

		int targetNationRecno = 0;

		if (loc.has_unit(UnitConstants.UNIT_LAND))
		{
			Unit unit = UnitArray[loc.unit_recno(UnitConstants.UNIT_LAND)];

			if (unit.unit_id != UnitConstants.UNIT_EXPLOSIVE_CART) // attacking own porcupine is allowed
				targetNationRecno = unit.nation_recno;
		}
		else if (loc.is_firm())
		{
			targetNationRecno = FirmArray[loc.firm_recno()].nation_recno;
		}
		else if (loc.is_town())
		{
			targetNationRecno = TownArray[loc.town_recno()].NationId;
		}

		if (nation_recno != 0 && targetNationRecno != 0)
		{
			if (!NationArray[nation_recno].get_relation(targetNationRecno).should_attack)
				return;
		}

		//------------------------------------------------------------//
		// return if this unit cannot do the attack action, or die
		//------------------------------------------------------------//
		if (!can_attack())
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}
		else if (is_unit_dead())
		{
			return;
		}

		loc = World.get_loc(targetXLoc, targetYLoc);

		int targetMobileType = (next_x_loc() == targetXLoc && next_y_loc() == targetYLoc)
			? loc.has_any_unit(mobile_type) : loc.has_any_unit();

		if (targetMobileType != 0)
		{
			Unit targetUnit = UnitArray[loc.unit_recno(targetMobileType)];
			attack_unit(targetUnit.sprite_recno, xOffset, yOffset, resetBlockedEdge);
		}

		//------ set ai_original_target_?_loc --------//

		if (ai_unit)
		{
			ai_original_target_x_loc = targetXLoc;
			ai_original_target_y_loc = targetYLoc;
		}
	}

	public void attack_unit(int targetRecno, int xOffset, int yOffset, bool resetBlockedEdge)
	{
		//------------------------------------------------------------//
		// return if this unit cannot do the attack action, or die
		//------------------------------------------------------------//
		if (!can_attack())
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}
		else if (is_unit_dead())
		{
			return;
		}

		//----------------------------------------------------------------------------------//
		// Note for non-air unit,
		// 1) If target's mobile type == mobile_type and thir territory id are different,
		//		call move_to() instead of attacking.
		// 2) In the case, this unit is a land unit and the target is a sea unit, skip
		//		checking for range attacking.  It is because the ship may be in the coast side
		//		there this unit can attack it by close attack.  In other cases, a unit without
		//		the ability of doing range attacking cannot attack target with different mobile
		//		type to its.
		// 3) If the region_id of the target located is same as that of thus unit located,
		//		order this unit to move to it and process attacking
		// 4) If the unit can't reach a location there it can do range attack, call move_to()
		//		rather than resume the action.  The reason not to resume the action, even though
		//		the unit reach a location there can do range attack later, is that the action
		//		will be resumed in function idle_detect_target()
		//----------------------------------------------------------------------------------//
		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();
		Unit targetUnit = UnitArray[targetRecno];
		int targetMobileType = targetUnit.mobile_type;
		int targetXLoc = targetUnit.next_x_loc();
		int targetYLoc = targetUnit.next_y_loc();
		int maxRange = 0;
		bool diffTerritoryAttack = false;
		Location loc = World.get_loc(targetUnit.next_x_loc(), targetUnit.next_y_loc());

		if (targetMobileType != 0 && mobile_type != UnitConstants.UNIT_AIR) // air unit can move to anywhere
		{
			//------------------------------------------------------------------------//
			// return if not feasible condition
			//------------------------------------------------------------------------//
			if ((mobile_type != UnitConstants.UNIT_LAND || targetMobileType != UnitConstants.UNIT_SEA) &&
			    mobile_type != targetMobileType)
			{
				if (!can_attack_different_target_type())
				{
					//-************ improve later **************-//
					//-******** should escape from being attacked ********-//
					if (in_any_defense_mode())
						general_defend_mode_detect_target();
					return;
				}
			}

			//------------------------------------------------------------------------//
			// handle the case the unit and the target are in different territory
			//------------------------------------------------------------------------//
			if (World.get_loc(curXLoc, curYLoc).region_id != loc.region_id)
			{
				maxRange = max_attack_range();
				Unit unit = UnitArray[loc.unit_recno(targetMobileType)];
				if (!possible_place_for_range_attack(targetXLoc, targetYLoc,
					    unit.sprite_info.loc_width, unit.sprite_info.loc_height, maxRange))
				{
					if (action_mode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
					    action_mode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
					    action_mode2 != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
						move_to(targetXLoc, targetYLoc);
					else // in defend mode, but unable to attack target
						general_defend_mode_detect_target(1);

					return;
				}
				else // can reach
				{
					diffTerritoryAttack = true;
				}
			}
		}

		//------------------------------------------------------------//
		// no unit there
		//------------------------------------------------------------//
		if (targetMobileType == 0)
		{
			if (action_mode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
			    action_mode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
			    action_mode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
			{
				stop2(UnitConstants.KEEP_DEFENSE_MODE);
			}

			return;
		}

		//------------------------------------------------------------//
		// cannot attack this nation
		//------------------------------------------------------------//
		if (!nation_can_attack(targetUnit.nation_recno) && targetUnit.unit_id != UnitConstants.UNIT_EXPLOSIVE_CART)
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//----------------------------------------------------------------//
		// action_mode2: checking for equal action or idle action
		//----------------------------------------------------------------//
		if ((action_mode2 == UnitConstants.ACTION_ATTACK_UNIT ||
		     action_mode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
		     action_mode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
		     action_mode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET) &&
		    action_para2 == targetUnit.sprite_recno && action_x_loc2 == targetXLoc && action_y_loc2 == targetYLoc)
		{
			//------------ old order ------------//

			if (cur_action != SPRITE_IDLE)
			{
				//------- the old order is processing, return -------//
				return;
			} //else the action becomes idle
		}
		else
		{
			//-------------- store new order ----------------//
			if (action_mode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
			    action_mode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
			    action_mode2 != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
			{
				action_mode2 = UnitConstants.ACTION_ATTACK_UNIT;
			}

			action_para2 = targetUnit.sprite_recno;
			action_x_loc2 = targetXLoc;
			action_y_loc2 = targetYLoc;
		}

		//-------------------------------------------------------------//
		// process new order
		//-------------------------------------------------------------//
		stop();
		cur_attack = 0;

		int attackDistance = cal_distance(targetXLoc, targetYLoc, targetUnit.sprite_info.loc_width,
			targetUnit.sprite_info.loc_height);
		choose_best_attack_mode(attackDistance, targetMobileType);

		AttackInfo attackInfo = attack_info_array[cur_attack];
		if (attackInfo.attack_range < attackDistance) // need to move to target
		{
			int searchResult = 1;

			if (xOffset != 0 || yOffset != 0)
			{
				int xLoc = targetXLoc + xOffset, yLoc = targetYLoc + yOffset;
				if (xLoc < 0)
					xLoc = 0;
				else if (xLoc >= GameConstants.MapSize)
					xLoc = GameConstants.MapSize - 1;
				if (yLoc < 0)
					yLoc = 0;
				else if (yLoc >= GameConstants.MapSize)
					yLoc = GameConstants.MapSize - 1;

				search(xLoc, yLoc, 1); // offset location is given, so move there directly
			}
			else
			{
				//if(mobile_type!=targetMobileType)
				if (diffTerritoryAttack)
				{
					//--------------------------------------------------------------------------------//
					// 1) different type from target, target located in different territory from this
					//		unit. But able to attack this target by range attacking
					//--------------------------------------------------------------------------------//
					move_to_range_attack(targetXLoc, targetYLoc, targetUnit.sprite_id, SeekPath.SEARCH_MODE_ATTACK_UNIT_BY_RANGE,
						maxRange);
				}
				else
				{
					//--------------------------------------------------------------------------------//
					// 1) same type of target,
					// 2) this unit is air unit, or
					// 3) different type from target, but target located in the same territory of this
					//		unit.
					//--------------------------------------------------------------------------------//
					searchResult = search(targetXLoc, targetYLoc, 1, SeekPath.SEARCH_MODE_TO_ATTACK, targetUnit.sprite_recno);
				}
			}

			//---------------------------------------------------------------//
			// initialize parameters for blocked edge handling in attacking
			//---------------------------------------------------------------//
			if (searchResult != 0)
			{
				waiting_term = 0;
				if (resetBlockedEdge)
				{
					for (int i = 0; i < blocked_edge.Length; i++)
					{
						blocked_edge[i] = 0;
					}
				}
			}
			else
			{
				for (int i = 0; i < blocked_edge.Length; i++)
				{
					blocked_edge[i] = 0xff;
				}
			}
		}
		else if (cur_action == SPRITE_IDLE) // the target is within attack range, attacks it now if the unit is idle
		{
			//---------------------------------------------------------------//
			// attack now
			//---------------------------------------------------------------//
			set_cur(next_x, next_y);
			set_attack_dir(curXLoc, curYLoc, targetXLoc, targetYLoc);
			if (is_dir_correct())
			{
				if (attackInfo.attack_range == 1)
				{
					set_attack();
					turn_delay = 0;
				}
			}
			else
			{
				set_turn();
			}
		}

		action_mode = UnitConstants.ACTION_ATTACK_UNIT;
		action_para = targetUnit.sprite_recno;
		action_x_loc = targetXLoc;
		action_y_loc = targetYLoc;

		//------ set ai_original_target_?_loc --------//

		if (ai_unit)
		{
			ai_original_target_x_loc = targetXLoc;
			ai_original_target_y_loc = targetYLoc;
		}
	}

	public void attack_firm(int firmXLoc, int firmYLoc, int xOffset = 0, int yOffset = 0, int resetBlockedEdge = 1)
	{
		//------------------------------------------------------------//
		// return if this unit cannot do the attack action
		//------------------------------------------------------------//
		if (!can_attack())
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}
		else if (is_unit_dead())
		{
			return;
		}

		Location loc = World.get_loc(firmXLoc, firmYLoc);

		//------------------------------------------------------------//
		// no firm there
		//------------------------------------------------------------//
		if (!loc.is_firm())
		{
			if (action_mode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
			    action_mode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
			    action_mode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
			{
				stop2(UnitConstants.KEEP_DEFENSE_MODE);
			}

			return;
		}

		//------------------------------------------------------------//
		// cannot attack this nation
		//------------------------------------------------------------//
		Firm firm = FirmArray[loc.firm_recno()];
		if (!nation_can_attack(firm.nation_recno))
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//------------------------------------------------------------------------------------//
		// move there if cannot reach the effective attacking region
		//------------------------------------------------------------------------------------//
		FirmInfo firmInfo = FirmRes[firm.firm_id];
		int maxRange = 0;
		bool diffTerritoryAttack = false;
		if (mobile_type != UnitConstants.UNIT_AIR &&
		    World.get_loc(next_x_loc(), next_y_loc()).region_id != loc.region_id)
		{
			maxRange = max_attack_range();
			//Firm		*firm = FirmArray[loc.firm_recno()];
			if (!possible_place_for_range_attack(firmXLoc, firmYLoc, firmInfo.loc_width, firmInfo.loc_height, maxRange))
			{
				if (action_mode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
				    action_mode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
				    action_mode2 != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
				{
					move_to(firmXLoc, firmYLoc);
				}

				return;
			}
			else // can reach
			{
				diffTerritoryAttack = true;
			}
		}

		//----------------------------------------------------------------//
		// action_mode2: checking for equal action or idle action
		//----------------------------------------------------------------//
		if ((action_mode2 == UnitConstants.ACTION_ATTACK_FIRM ||
		     action_mode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
		     action_mode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
		     action_mode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET) &&
		    action_para2 == firm.firm_recno && action_x_loc2 == firmXLoc && action_y_loc2 == firmYLoc)
		{
			//-------------- old order -------------//
			if (cur_action != SPRITE_IDLE)
			{
				return;
			}
		}
		else
		{
			//-------------- new order -------------//
			if (action_mode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
			    action_mode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
			    action_mode2 != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
				action_mode2 = UnitConstants.ACTION_ATTACK_FIRM;

			action_para2 = firm.firm_recno;
			action_x_loc2 = firmXLoc;
			action_y_loc2 = firmYLoc;
		}

		//-------------------------------------------------------------//
		// process new order
		//-------------------------------------------------------------//
		stop();
		cur_attack = 0;

		int attackDistance = cal_distance(firmXLoc, firmYLoc, firmInfo.loc_width, firmInfo.loc_height);
		choose_best_attack_mode(attackDistance);

		AttackInfo attackInfo = attack_info_array[cur_attack];
		if (attackInfo.attack_range < attackDistance) // need to move to target
		{
			int searchResult = 1;

			if (xOffset != 0 || yOffset != 0)
			{
				int xLoc = firmXLoc + xOffset, yLoc = firmYLoc + yOffset;
				if (xLoc < 0)
					xLoc = 0;
				else if (xLoc >= GameConstants.MapSize)
					xLoc = GameConstants.MapSize - 1;
				if (yLoc < 0)
					yLoc = 0;
				else if (yLoc >= GameConstants.MapSize)
					yLoc = GameConstants.MapSize - 1;

				search(xLoc, yLoc, 1); // offset location is given, so move there directly
			}
			else // without offset given, so call set_move_to_surround()
			{
				if (diffTerritoryAttack)
				{
					//--------------------------------------------------------------------------------//
					// 1) different type from target, target located in different territory from this
					//		unit. But able to attack this target by range attacking
					//--------------------------------------------------------------------------------//
					move_to_range_attack(firmXLoc, firmYLoc, firm.firm_id, SeekPath.SEARCH_MODE_ATTACK_FIRM_BY_RANGE, maxRange);
				}
				else
				{
					//--------------------------------------------------------------------------------//
					// 1) same type of target,
					// 2) this unit is air unit, or
					// 3) different type from target, but target located in the same territory of this
					//		unit.
					//--------------------------------------------------------------------------------//
					searchResult = set_move_to_surround(firmXLoc, firmYLoc, firmInfo.loc_width, firmInfo.loc_height,
						UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO, 0, 0);
				}
			}

			//---------------------------------------------------------------//
			// initialize parameters for blocked edge handling in attacking
			//---------------------------------------------------------------//
			if (searchResult != 0)
			{
				waiting_term = 0;
				if (resetBlockedEdge != 0)
				{
					for (int i = 0; i < blocked_edge.Length; i++)
						blocked_edge[i] = 0;
				}
			}
			else
			{
				for (int i = 0; i < blocked_edge.Length; i++)
					blocked_edge[i] = 0xff;
			}
		}
		else if (cur_action == SPRITE_IDLE)
		{
			//---------------------------------------------------------------//
			// attack now
			//---------------------------------------------------------------//
			set_cur(next_x, next_y);

			if (firm.firm_id != Firm.FIRM_RESEARCH)
			{
				set_attack_dir(next_x_loc(), next_y_loc(), firm.center_x, firm.center_y);
			}
			else // FIRM_RESEARCH with size 2x3
			{
				int curXLoc = next_x_loc();
				int curYLoc = next_y_loc();

				int hitXLoc = (curXLoc > firm.loc_x1) ? firm.loc_x2 : firm.loc_x1;

				int hitYLoc;
				if (curYLoc < firm.center_y)
					hitYLoc = firm.loc_y1;
				else if (curYLoc == firm.center_y)
					hitYLoc = firm.center_y;
				else
					hitYLoc = firm.loc_y2;

				set_attack_dir(curXLoc, curYLoc, hitXLoc, hitYLoc);
			}

			if (is_dir_correct())
			{
				if (attackInfo.attack_range == 1)
					set_attack();
				//else range_attack is processed in calling process_attack_firm()
			}
			else
			{
				set_turn();
			}
		}

		action_mode = UnitConstants.ACTION_ATTACK_FIRM;
		action_para = firm.firm_recno;
		action_x_loc = firmXLoc;
		action_y_loc = firmYLoc;
	}

	public void attack_town(int townXLoc, int townYLoc, int xOffset = 0, int yOffset = 0, int resetBlockedEdge = 1)
	{
		//------------------------------------------------------------//
		// return if this unit cannot do the attack action
		//------------------------------------------------------------//
		if (!can_attack())
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}
		else if (is_unit_dead())
		{
			return;
		}

		Location loc = World.get_loc(townXLoc, townYLoc);

		//------------------------------------------------------------//
		// no town there
		//------------------------------------------------------------//
		if (!loc.is_town())
		{
			if (action_mode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
			    action_mode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
			    action_mode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
			{
				stop(UnitConstants.KEEP_DEFENSE_MODE);
			}

			return;
		}

		//------------------------------------------------------------//
		// cannot attack this nation
		//------------------------------------------------------------//
		Town town = TownArray[loc.town_recno()];
		if (!nation_can_attack(town.NationId))
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//------------------------------------------------------------------------------------//
		// move there if cannot reach the effective attacking region
		//------------------------------------------------------------------------------------//
		int maxRange = 0;
		bool diffTerritoryAttack = false;
		if (mobile_type != UnitConstants.UNIT_AIR &&
		    World.get_loc(next_x_loc(), next_y_loc()).region_id != loc.region_id)
		{
			maxRange = max_attack_range();
			if (!possible_place_for_range_attack(townXLoc, townYLoc, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, maxRange))
			{
				if (action_mode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
				    action_mode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
				    action_mode != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
				{
					move_to(townXLoc, townYLoc);
				}

				return;
			}
			else // can reach
			{
				diffTerritoryAttack = true;
			}
		}

		//----------------------------------------------------------------//
		// action_mode2: checking for equal action or idle action
		//----------------------------------------------------------------//
		if ((action_mode2 == UnitConstants.ACTION_ATTACK_TOWN ||
		     action_mode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
		     action_mode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
		     action_mode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET) &&
		    action_para2 == town.TownId && action_x_loc2 == townXLoc && action_y_loc2 == townYLoc)
		{
			//----------- old order ------------//

			if (cur_action != SPRITE_IDLE)
			{
				//-------- old order is processing -------//
				return;
			}
		}
		else
		{
			//------------ new order -------------//
			if (action_mode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
			    action_mode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
			    action_mode2 != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
				action_mode2 = UnitConstants.ACTION_ATTACK_TOWN;

			action_para2 = town.TownId;
			action_x_loc2 = townXLoc;
			action_y_loc2 = townYLoc;
		}

		//-------------------------------------------------------------//
		// process new order
		//-------------------------------------------------------------//
		stop();
		cur_attack = 0;

		int attackDistance = cal_distance(townXLoc, townYLoc, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT);
		choose_best_attack_mode(attackDistance);

		AttackInfo attackInfo = attack_info_array[cur_attack];
		if (attackInfo.attack_range < attackDistance)
		{
			int searchResult = 1;

			if (xOffset != 0 || yOffset != 0)
			{
				int xLoc = townXLoc + xOffset, yLoc = townYLoc + yOffset;
				if (xLoc < 0)
					xLoc = 0;
				else if (xLoc >= GameConstants.MapSize)
					xLoc = GameConstants.MapSize - 1;
				if (yLoc < 0)
					yLoc = 0;
				else if (yLoc >= GameConstants.MapSize)
					yLoc = GameConstants.MapSize - 1;

				search(xLoc, yLoc, 1); // offset location is given, so move there directly
			}
			else // without offset given, so call set_move_to_surround()
			{
				if (diffTerritoryAttack)
				{
					//--------------------------------------------------------------------------------//
					// 1) different type from target, target located in different territory but able to
					//		attack this target by range attacking
					//--------------------------------------------------------------------------------//
					move_to_range_attack(townXLoc, townYLoc, 0, SeekPath.SEARCH_MODE_ATTACK_TOWN_BY_RANGE, maxRange);
				}
				else
				{
					//--------------------------------------------------------------------------------//
					// 1) same type of target,
					// 2) this unit is air unit, or
					// 3) different type from target, but target located in the same territory
					//--------------------------------------------------------------------------------//
					searchResult = set_move_to_surround(townXLoc, townYLoc, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT,
						UnitConstants.BUILDING_TYPE_TOWN_MOVE_TO, 0, 0);
				}
			}

			//---------------------------------------------------------------//
			// initialize parameters for blocked edge handling in attacking
			//---------------------------------------------------------------//
			if (searchResult != 0)
			{
				waiting_term = 0;
				if (resetBlockedEdge != 0)
				{
					for (int i = 0; i < blocked_edge.Length; i++)
						blocked_edge[i] = 0;
				}
			}
			else
			{
				for (int i = 0; i < blocked_edge.Length; i++)
					blocked_edge[i] = 0xff;
			}
		}
		else if (cur_action == SPRITE_IDLE)
		{
			//---------------------------------------------------------------//
			// attack now
			//---------------------------------------------------------------//
			set_cur(next_x, next_y);
			set_attack_dir(next_x_loc(), next_y_loc(), town.LocCenterX, town.LocCenterY);
			if (is_dir_correct())
			{
				if (attackInfo.attack_range == 1)
					set_attack();
			}
			else
			{
				set_turn();
			}
		}

		action_mode = UnitConstants.ACTION_ATTACK_TOWN;
		action_para = town.TownId;
		action_x_loc = townXLoc;
		action_y_loc = townYLoc;
	}

	public void attack_wall(int wallXLoc, int wallYLoc, int xOffset = 0, int yOffset = 0, int resetBlockedEdge = 1)
	{
		//------------------------------------------------------------//
		// return if this unit cannot do the attack action
		//------------------------------------------------------------//
		if (!can_attack())
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}
		else if (is_unit_dead())
		{
			return;
		}

		Location loc = World.get_loc(wallXLoc, wallYLoc);

		//------------------------------------------------------------//
		// no wall there
		//------------------------------------------------------------//
		if (!loc.is_wall())
		{
			if (action_mode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
			    action_mode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
			    action_mode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
			{
				stop(UnitConstants.KEEP_DEFENSE_MODE);
			}

			return;
		}

		//------------------------------------------------------------//
		// cannot attack this nation
		//------------------------------------------------------------//
		if (!nation_can_attack(loc.wall_nation_recno()))
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//------------------------------------------------------------------------------------//
		// move there if cannot reach the effective attacking region
		//------------------------------------------------------------------------------------//
		int maxRange = 0;
		bool diffTerritoryAttack = false;
		if (mobile_type != UnitConstants.UNIT_AIR &&
		    World.get_loc(next_x_loc(), next_y_loc()).region_id != loc.region_id)
		{
			maxRange = max_attack_range();
			if (!possible_place_for_range_attack(wallXLoc, wallYLoc, 1, 1, maxRange))
			{
				if (action_mode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
				    action_mode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
				    action_mode != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
				{
					move_to(wallXLoc, wallYLoc);
				}

				return;
			}
			else // can reach
			{
				diffTerritoryAttack = true;
			}
		}

		//----------------------------------------------------------------//
		// action_mode2: checking for equal action or idle action
		//----------------------------------------------------------------//
		if ((action_mode2 == UnitConstants.ACTION_ATTACK_WALL ||
		     action_mode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
		     action_mode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
		     action_mode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET) &&
		    action_para2 == 0 && action_x_loc2 == wallXLoc && action_y_loc2 == wallYLoc)
		{
			//------------ old order ------------//

			if (cur_action != SPRITE_IDLE)
			{
				//------- old order is processing --------//
				return;
			}
		}
		else
		{
			//-------------- new order -------------//
			if (action_mode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
			    action_mode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
			    action_mode2 != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
				action_mode2 = UnitConstants.ACTION_ATTACK_WALL;

			action_para2 = 0;
			action_x_loc2 = wallXLoc;
			action_y_loc2 = wallYLoc;
		}

		//-------------------------------------------------------------//
		// process new order
		//-------------------------------------------------------------//
		stop();
		cur_attack = 0;

		int attackDistance = cal_distance(wallXLoc, wallYLoc, 1, 1);
		choose_best_attack_mode(attackDistance);

		AttackInfo attackInfo = attack_info_array[cur_attack];
		if (attackInfo.attack_range < attackDistance)
		{
			int searchResult = 1;

			if (xOffset != 0 || yOffset != 0)
			{
				int xLoc = wallXLoc + xOffset, yLoc = wallYLoc + yOffset;
				if (xLoc < 0)
					xLoc = 0;
				else if (xLoc >= GameConstants.MapSize)
					xLoc = GameConstants.MapSize - 1;
				if (yLoc < 0)
					yLoc = 0;
				else if (yLoc >= GameConstants.MapSize)
					yLoc = GameConstants.MapSize - 1;

				search(xLoc, yLoc, 1); // offset location is given, so move there directly
			}
			else
			{
				if (diffTerritoryAttack)
				{
					//--------------------------------------------------------------------------------//
					// 1) different type from target, target located in different territory but able to
					//		attack this target by range attacking
					//--------------------------------------------------------------------------------//
					move_to_range_attack(wallXLoc, wallYLoc, 0, SeekPath.SEARCH_MODE_ATTACK_WALL_BY_RANGE, maxRange);
				}
				else
				{
					//--------------------------------------------------------------------------------//
					// 1) same type of target,
					// 2) this unit is air unit, or
					// 3) different type from target, but target located in the same territory
					//--------------------------------------------------------------------------------//
					searchResult = set_move_to_surround(wallXLoc, wallYLoc, 1, 1,
						UnitConstants.BUILDING_TYPE_WALL, 0, 0);
				}
			}

			//---------------------------------------------------------------//
			// initialize parameters for blocked edge handling in attacking
			//---------------------------------------------------------------//
			if (searchResult != 0)
			{
				waiting_term = 0;
				if (resetBlockedEdge != 0)
				{
					for (int i = 0; i < blocked_edge.Length; i++)
						blocked_edge[i] = 0;
				}
			}
			else
			{
				for (int i = 0; i < blocked_edge.Length; i++)
					blocked_edge[i] = 0xff;
			}
		}
		else if (cur_action == SPRITE_IDLE)
		{
			//---------------------------------------------------------------//
			// attack now
			//---------------------------------------------------------------//
			set_cur(next_x, next_y);
			set_attack_dir(next_x_loc(), next_y_loc(), wallXLoc, wallYLoc);
			if (is_dir_correct())
			{
				if (attackInfo.attack_range == 1)
					set_attack();
			}
			else
			{
				set_turn();
			}
		}

		action_mode = UnitConstants.ACTION_ATTACK_WALL;
		action_para = 0;
		action_x_loc = wallXLoc;
		action_y_loc = wallYLoc;
	}

	public void hit_target(Unit parentUnit, Unit targetUnit, double attackDamage, int parentNationRecno)
	{
		int targetNationRecno = targetUnit.nation_recno;

		//------------------------------------------------------------//
		// if the attacked unit is in defense mode, order other available
		// unit in the same camp to help this unit
		// Note : checking for nation_recno since one unit can attack units
		//			 in same nation by bullet accidentally
		//------------------------------------------------------------//
		if (parentUnit != null && parentUnit.cur_action != SPRITE_DIE && parentUnit.is_visible() &&
		    parentNationRecno != targetNationRecno && parentUnit.nation_can_attack(targetNationRecno) &&
		    targetUnit.in_auto_defense_mode())
		{
			if (!FirmArray.IsDeleted(targetUnit.action_misc_para))
			{
				Firm firm = FirmArray[targetUnit.action_misc_para];
				if (firm.firm_id == Firm.FIRM_CAMP)
				{
					FirmCamp camp = (FirmCamp)firm;
					camp.defense(parentUnit.sprite_recno);
				}
			}
			else
			{
				targetUnit.clear_unit_defense_mode();
			}
		}

		// ---------- add indicator on the map ----------//
		if (NationArray.player_recno != 0 && targetUnit.is_own())
			WarPointArray.AddPoint(targetUnit.next_x_loc(), targetUnit.next_y_loc());

		//-----------------------------------------------------------------------//
		// decrease the hit points of the target Unit
		//-----------------------------------------------------------------------//
		const int DEFAULT_ARMOR = 4;
		const double DEFAULT_ARMOR_OVER_ATTACK_SLOW_DOWN = (double)DEFAULT_ARMOR / (double)InternalConstants.ATTACK_SLOW_DOWN;
		const double ONE_OVER_ATTACK_SLOW_DOWN = 1.0 / (double)InternalConstants.ATTACK_SLOW_DOWN;
		const double COMPARE_POINT = DEFAULT_ARMOR_OVER_ATTACK_SLOW_DOWN + ONE_OVER_ATTACK_SLOW_DOWN;

		if (attackDamage >= COMPARE_POINT)
			targetUnit.hit_points -= attackDamage - DEFAULT_ARMOR_OVER_ATTACK_SLOW_DOWN;
		else
			targetUnit.hit_points -= Math.Min(attackDamage, ONE_OVER_ATTACK_SLOW_DOWN);  // in case attackDamage = 0, no hit_point is reduced
		
		Nation parentNation = parentNationRecno != 0 ? NationArray[parentNationRecno] : null;
		Nation targetNation = targetNationRecno != 0 ? NationArray[targetNationRecno] : null;
		int targetUnitClass = UnitRes[targetUnit.unit_id].unit_class;

		if (targetUnit.hit_points <= 0.0)
		{
			targetUnit.hit_points = 0.0;

			//---- if the unit killed is a human unit -----//

			if (targetUnit.race_id != 0)
			{
				//---- if the unit killed is a town defender unit -----//

				if (targetUnit.is_civilian() && targetUnit.in_defend_town_mode())
				{
					if (targetNationRecno != 0)
					{
						targetNation.civilian_killed(targetUnit.race_id, false, 1);
						targetNation.own_civilian_killed++;
					}

					if (parentNation != null)
					{
						parentNation.civilian_killed(targetUnit.race_id, true, 1);
						parentNation.enemy_civilian_killed++;
					}
				}
				else if (targetUnit.is_civilian() && targetUnit.skill.combat_level < 20) //--- mobile civilian ---//
				{
					if (targetNationRecno != 0)
					{
						targetNation.civilian_killed(targetUnit.race_id, false, 0);
						targetNation.own_civilian_killed++;
					}

					if (parentNation != null)
					{
						parentNation.civilian_killed(targetUnit.race_id, true, 0);
						parentNation.enemy_civilian_killed++;
					}
				}
				else //---- if the unit killed is a soldier -----//
				{
					if (targetNationRecno != 0)
						targetNation.own_soldier_killed++;

					if (parentNation != null)
						parentNation.enemy_soldier_killed++;
				}
			}

			//--------- if it's a non-human unit ---------//

			else
			{
				switch (UnitRes[targetUnit.unit_id].unit_class)
				{
					case UnitConstants.UNIT_CLASS_WEAPON:
						if (parentNation != null)
							parentNation.enemy_weapon_destroyed++;

						if (targetNationRecno != 0)
							targetNation.own_weapon_destroyed++;
						break;


					case UnitConstants.UNIT_CLASS_SHIP:
						if (parentNation != null)
							parentNation.enemy_ship_destroyed++;

						if (targetNationRecno != 0)
							targetNation.own_ship_destroyed++;
						break;
				}

				//---- if the unit destroyed is a trader or caravan -----//

				// killing a caravan is resented by all races
				if (targetUnit.unit_id == UnitConstants.UNIT_CARAVAN || targetUnit.unit_id == UnitConstants.UNIT_VESSEL)
				{
					// Race-Id of 0 means a loyalty penalty applied for all races
					if (targetNationRecno != 0)
						targetNation.civilian_killed(0, false, 3);

					if (parentNation != null)
						parentNation.civilian_killed(0, true, 3);
				}
			}

			return;
		}

		if (parentUnit != null && parentNationRecno != targetNationRecno)
			parentUnit.gain_experience(); // gain experience to increase combat level

		//-----------------------------------------------------------------------//
		// action of the target to take
		//-----------------------------------------------------------------------//
		if (parentUnit == null) // do nothing if parent is dead
			return;

		if (parentUnit.cur_action == SPRITE_DIE) // skip for explosive cart
			return;

		// the target and the attacker's nations are different
		// (it's possible that when a unit who has just changed nation has its bullet hitting its own nation)
		if (targetNationRecno == parentNationRecno)
			return;

		//------- two nations at war ---------//

		if (parentNation != null && targetNationRecno != 0)
		{
			parentNation.set_at_war_today();
			targetNation.set_at_war_today(parentUnit.sprite_recno);
		}

		//-------- increase battling fryhtan score --------//

		if (parentNation != null && targetUnitClass == UnitConstants.UNIT_CLASS_MONSTER)
		{
			parentNation.kill_monster_score += 0.1;
		}

		//------ call target unit being attack functions -------//

		if (targetNationRecno != 0)
		{
			targetNation.being_attacked(parentNationRecno);

			if (targetUnit.ai_unit)
			{
				if (targetUnit.rank_id >= RANK_GENERAL)
					targetUnit.ai_leader_being_attacked(parentUnit.sprite_recno);

				if (UnitRes[targetUnit.unit_id].unit_class == UnitConstants.UNIT_CLASS_SHIP)
					((UnitMarine)targetUnit).ai_ship_being_attacked(parentUnit.sprite_recno);
			}

			//--- if a member in a troop is under attack, ask for other troop members to help ---//

			if (Info.TotalDays % 2 == sprite_recno % 2)
			{
				if (targetUnit.leader_unit_recno != 0 ||
				    (targetUnit.team_info != null && targetUnit.team_info.member_unit_array.Count > 1))
				{
					if (!UnitArray.IsDeleted(parentUnit
						    .sprite_recno)) // it is possible that parentUnit is dying right now 
					{
						targetUnit.ask_team_help_attack(parentUnit);
					}
				}
			}
		}

		//--- increase reputation of the nation that attacks monsters ---//

		else if (targetUnitClass == UnitConstants.UNIT_CLASS_MONSTER)
		{
			if (parentNation != null)
				parentNation.change_reputation(GameConstants.REPUTATION_INCREASE_PER_ATTACK_MONSTER);

			//--- if a member in a troop is under attack, ask for other troop members to help ---//

			if (Info.TotalDays % 2 == sprite_recno % 2)
			{
				if (targetUnit.leader_unit_recno != 0 ||
				    (targetUnit.team_info != null && targetUnit.team_info.member_unit_array.Count > 1))
				{
					if (!UnitArray.IsDeleted(parentUnit
						    .sprite_recno)) // it is possible that parentUnit is dying right now 
					{
						targetUnit.ask_team_help_attack(parentUnit);
					}
				}
			}
		}

		//------------------------------------------//

		if (!targetUnit.can_attack()) // no action if the target unit is unable to attack
			return;

		targetUnit.unit_auto_guarding(parentUnit);
	}

	public void hit_building(Unit attackUnit, int targetXLoc, int targetYLoc, double attackDamage, int attackNationRecno)
	{
		Location loc = World.get_loc(targetXLoc, targetYLoc);

		if (loc.is_firm())
			hit_firm(attackUnit, targetXLoc, targetYLoc, attackDamage, attackNationRecno);
		else if (loc.is_town())
			hit_town(attackUnit, targetXLoc, targetYLoc, attackDamage, attackNationRecno);
	}

	public void hit_firm(Unit attackUnit, int targetXLoc, int targetYLoc, double attackDamage, int attackNationRecno)
	{
		Location loc = World.get_loc(targetXLoc, targetYLoc);
		if (!loc.is_firm())
			return; // do nothing if no firm there

		//----------- attack firm ------------//
		Firm targetFirm = FirmArray[loc.firm_recno()];

		Nation attackNation = NationArray.IsDeleted(attackNationRecno) ? null : NationArray[attackNationRecno];

		//------------------------------------------------------------------------------//
		// change relation to hostile
		// check for NULL to skip unhandled case by bullets
		// check for SPRITE_DIE to skip the case by EXPLOSIVE_CART
		//------------------------------------------------------------------------------//
		// the target and the attacker's nations are different
		// (it's possible that when a unit who has just changed nation has its bullet hitting its own nation)
		if (attackUnit != null && attackUnit.cur_action != SPRITE_DIE && targetFirm.nation_recno != attackNationRecno)
		{
			if (attackNation != null && targetFirm.nation_recno != 0)
			{
				attackNation.set_at_war_today();
				NationArray[targetFirm.nation_recno].set_at_war_today(attackUnit.sprite_recno);
			}

			if (targetFirm.nation_recno != 0)
				NationArray[targetFirm.nation_recno].being_attacked(attackNationRecno);

			//------------ auto defense -----------------//
			if (attackUnit.is_visible())
				targetFirm.auto_defense(attackUnit.sprite_recno);

			if (attackNationRecno != targetFirm.nation_recno)
				attackUnit.gain_experience(); // gain experience to increase combat level

			targetFirm.being_attacked(attackUnit.sprite_recno);

			//------ increase battling fryhtan score -------//

			if (attackNation != null && targetFirm.firm_id == Firm.FIRM_MONSTER)
				attackNation.kill_monster_score += 0.01;
		}

		//---------- add indicator on the map ----------//

		if (NationArray.player_recno != 0 && targetFirm.own_firm())
			WarPointArray.AddPoint(targetFirm.center_x, targetFirm.center_y);

		//---------- damage to the firm ------------//

		targetFirm.hit_points -= attackDamage / 3.0; // /3 so that it takes longer to destroy a firm

		if (targetFirm.hit_points <= 0.0)
		{
			targetFirm.hit_points = 0.0;

			SERes.sound(targetFirm.center_x, targetFirm.center_y, 1, 'F', targetFirm.firm_id, "DIE");

			if (targetFirm.nation_recno == NationArray.player_recno)
				NewsArray.firm_destroyed(targetFirm.firm_recno, attackUnit, attackNationRecno);

			if (targetFirm.nation_recno != 0)
			{
				if (attackNation != null)
					attackNation.enemy_firm_destroyed++;

				NationArray[targetFirm.nation_recno].own_firm_destroyed++;
			}

			else if (targetFirm.firm_id == Firm.FIRM_MONSTER)
			{
				NewsArray.monster_firm_destroyed(((FirmMonster)targetFirm).monster_id, targetFirm.center_x,
					targetFirm.center_y);
			}

			FirmArray.DeleteFirm(targetFirm);
		}
	}

	public void hit_town(Unit attackUnit, int targetXLoc, int targetYLoc, double attackDamage, int attackNationRecno)
	{
		Location loc = World.get_loc(targetXLoc, targetYLoc);

		if (!loc.is_town())
			return; // do nothing if no town there

		//----------- attack town ----------//

		Town targetTown = TownArray[loc.town_recno()];
		int targetTownRecno = targetTown.TownId;
		int targetTownNameId = targetTown.TownNameId;
		int targetTownXLoc = targetTown.LocCenterX;
		int targetTownYLoc = targetTown.LocCenterY;

		// ---------- add indicator on the map ----------//
		if (NationArray.player_recno != 0 && targetTown.NationId == NationArray.player_recno)
			WarPointArray.AddPoint(targetTown.LocCenterX, targetTown.LocCenterY);

		//------------------------------------------------------------------------------//
		// change relation to hostile
		// check for NULL to skip unhandled case by bullets
		// check for SPRITE_DIE to skip the case by EXPLOSIVE_CART
		//------------------------------------------------------------------------------//
		// the target and the attacker's nations are different
		// (it's possible that when a unit who has just changed nation has its bullet hitting its own nation)
		if (attackUnit != null && attackUnit.cur_action != SPRITE_DIE && targetTown.NationId != attackNationRecno)
		{
			int townNationRecno = targetTown.NationId;

			//------- change to hostile relation -------//

			if (attackNationRecno != 0 && targetTown.NationId != 0)
			{
				NationArray[attackNationRecno].set_at_war_today();
				NationArray[targetTown.NationId].set_at_war_today(attackUnit.sprite_recno);
			}

			if (targetTown.NationId != 0)
			{
				NationArray[targetTown.NationId].being_attacked(attackNationRecno);
			}

			// don't add the town abandon news that might be called by Town::dec_pop() as the town is actually destroyed not abandoned
			NewsArray.disable();

			targetTown.BeingAttacked(attackUnit.sprite_recno, attackDamage);

			NewsArray.enable();

			//------ if the town is destroyed, add a news --------//

			if (TownArray.IsDeleted(targetTownRecno) && townNationRecno == NationArray.player_recno)
			{
				NewsArray.town_destroyed(targetTownNameId, targetTownXLoc, targetTownYLoc, attackUnit,
					attackNationRecno);
			}

			//---------- gain experience --------//

			if (attackNationRecno != targetTown.NationId)
				attackUnit.gain_experience(); // gain experience to increase combat level

			//------------ auto defense -----------------//

			if (!FirmArray.IsDeleted(targetTownRecno))
				targetTown.AutoDefense(attackUnit.sprite_recno);
		}
	}

	public void hit_wall(Unit attackUnit, int targetXLoc, int targetYLoc, double attackDamage, int attackNationRecno)
	{
		Location loc = World.get_loc(targetXLoc, targetYLoc);

		/*
		if(attackUnit!=NULL)
			attackUnit.change_relation(attackNationRecno, loc.wall_nation_recno(), NationBase.NATION_HOSTILE);
		*/

		//TODO rewrite
		//if (!loc.attack_wall((int)attackDamage))
			//World.correct_wall(targetXLoc, targetYLoc);
	}

	public int max_attack_range()
	{
		int maxRange = 0;

		for (int i = 0; i < attack_count; i++)
		{
			AttackInfo attackInfo = attack_info_array[i];
			if (can_attack_with(attackInfo) && attackInfo.attack_range > maxRange)
				maxRange = attackInfo.attack_range;
		}

		return maxRange;
	}

	public void gain_experience()
	{
		if (UnitRes[unit_id].unit_class != UnitConstants.UNIT_CLASS_HUMAN)
			return; // no experience gain if unit is not human

		//---- increase the unit's contribution to the nation ----//

		if (nation_contribution < GameConstants.MAX_NATION_CONTRIBUTION)
		{
			nation_contribution++;
		}

		//------ increase combat skill -------//

		inc_minor_combat_level(6);

		//--- if this is a soldier led by a commander, increase the leadership of its commander -----//

		if (is_leader_in_range())
		{
			Unit leaderUnit = UnitArray[leader_unit_recno];

			leaderUnit.inc_minor_skill_level(1);

			//-- give additional increase if the leader has skill potential on leadership --//

			if (leaderUnit.skill.skill_potential > 0)
			{
				if (Misc.Random(10 - leaderUnit.skill.skill_potential / 10) == 0)
					leaderUnit.inc_minor_skill_level(5);
			}

			//--- if this soldier has leadership potential and is led by a commander ---//
			//--- he learns leadership by watching how the commander commands the troop --//

			if (skill.skill_potential > 0)
			{
				if (Misc.Random(10 - skill.skill_potential / 10) == 0)
					inc_minor_skill_level(5);
			}
		}
	}

	public virtual double actual_damage()
	{
		AttackInfo attackInfo = attack_info_array[cur_attack];

		int attackDamage = attackInfo.attack_damage;

		//-------- pierce damage --------//

		attackDamage += Misc.Random(3) + attackInfo.pierce_damage
			* Misc.Random(skill.combat_level - attackInfo.combat_level)
			/ (100 - attackInfo.combat_level);

		//--- if this unit is led by a general, its attacking ability is influenced by the general ---//
		//
		// The unit's attacking ability is increased by a percentage equivalent to
		// the leader unit's leadership.
		//
		//------------------------------------------------------------------------//

		if (is_leader_in_range())
		{
			Unit leaderUnit = UnitArray[leader_unit_recno];
			attackDamage += attackDamage * leaderUnit.skill.skill_level / 100;
		}

		// lessen all attacking damages, thus slowing down all battles.
		return (double)attackDamage / (double)InternalConstants.ATTACK_SLOW_DOWN;
	}

	public bool nation_can_attack(int nationRecno) // can this nation be attacked, no if alliance or etc..
	{
		if (!ai_unit)
		{
			return nationRecno != nation_recno; // able to attack all nation except our own nation
		}
		else if (nation_recno == nationRecno)
			return false; // ai unit don't attack its own nation, except special order

		if (nation_recno == 0 || nationRecno == 0)
			return true; // true if either nation is independent

		Nation nation = NationArray[nation_recno];

		int relationStatus = nation.get_relation_status(nationRecno);
		if (relationStatus == NationBase.NATION_FRIENDLY || relationStatus == NationBase.NATION_ALLIANCE)
			return false;

		return true;
	}

	public bool independent_nation_can_attack(int nationRecno)
	{
		switch (unit_mode)
		{
			case UnitConstants.UNIT_MODE_DEFEND_TOWN:
				if (TownArray.IsDeleted(unit_mode_para))
					return false; // don't attack independent unit with no town

				Town town = TownArray[unit_mode_para];

				if (!town.IsHostileNation(nationRecno))
					return false; // false if the independent unit don't want to attack us

				break;

			case UnitConstants.UNIT_MODE_REBEL:
				if (RebelArray.IsDeleted(unit_mode_para))
					return false;

				Rebel rebel = RebelArray[unit_mode_para];

				if (!rebel.is_hostile_nation(nationRecno))
					return false;

				break;

			case UnitConstants.UNIT_MODE_MONSTER:
				if (unit_mode_para == 0)
					return nationRecno != 0; // attack anything that is not independent

				FirmMonster firmMonster = (FirmMonster)FirmArray[unit_mode_para];

				if (!firmMonster.is_hostile_nation(nationRecno))
					return false; // false if the independent unit don't want to attack us

				break;

			default:
				return false;
		}

		return true;
	}

	public void cycle_eqv_attack()
	{
		int trial = SpriteInfo.MAX_UNIT_ATTACK_TYPE + 2;
		if (attack_info_array[cur_attack].eqv_attack_next > 0)
		{
			do
			{
				cur_attack = attack_info_array[cur_attack].eqv_attack_next - 1;
			} while (!can_attack_with(cur_attack));
		}
		else
		{
			if (!can_attack_with(cur_attack))
			{
				// force to search again
				int attackRange = attack_info_array[cur_attack].attack_range;
				for (int i = 0; i < attack_count; i++)
				{
					AttackInfo attackInfo = attack_info_array[i];
					if (attackInfo.attack_range >= attackRange && can_attack_with(attackInfo))
					{
						cur_attack = i;
						break;
					}
				}
			}
		}
	}

	public bool is_action_attack()
	{
		switch (action_mode2)
		{
			case UnitConstants.ACTION_ATTACK_UNIT:
			case UnitConstants.ACTION_ATTACK_FIRM:
			case UnitConstants.ACTION_ATTACK_TOWN:
			case UnitConstants.ACTION_ATTACK_WALL:
			case UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET:
			case UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET:
			case UnitConstants.ACTION_AUTO_DEFENSE_BACK_CAMP:
			case UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET:
			case UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET:
			case UnitConstants.ACTION_DEFEND_TOWN_BACK_TOWN:
			case UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET:
			case UnitConstants.ACTION_MONSTER_DEFEND_DETECT_TARGET:
			case UnitConstants.ACTION_MONSTER_DEFEND_BACK_FIRM:
				return true;

			default:
				return false;
		}
	}

	public bool can_attack()
	{
		return can_attack_flag && attack_count > 0;
	}

	//-----------------  defense actions ---------------------//
	//========== unit's defense mode ==========//
	public void defense_attack_unit(int targetRecno)
	{
		action_mode2 = UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET;
		attack_unit(targetRecno, 0, 0, true);
	}

	public void defense_attack_firm(int targetXLoc, int targetYLoc)
	{
		action_mode2 = UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET;
		attack_firm(targetXLoc, targetYLoc);
	}

	public void defense_attack_town(int targetXLoc, int targetYLoc)
	{
		action_mode2 = UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET;
		attack_town(targetXLoc, targetYLoc);
	}

	public void defense_attack_wall(int targetXLoc, int targetYLoc)
	{
		action_mode2 = UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET;
		attack_wall(targetXLoc, targetYLoc);
	}

	public void defense_detect_target()
	{
		action_mode2 = UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET;
		action_para2 = UnitConstants.AUTO_DEFENSE_DETECT_COUNT;
		action_x_loc2 = -1;
		action_y_loc2 = -1;
	}

	public bool in_auto_defense_mode()
	{
		return (action_mode2 >= UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
		        action_mode2 <= UnitConstants.ACTION_AUTO_DEFENSE_BACK_CAMP);
	}

	public bool in_defend_town_mode()
	{
		return (action_mode2 >= UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
		        action_mode2 <= UnitConstants.ACTION_DEFEND_TOWN_BACK_TOWN);
	}

	public bool in_monster_defend_mode()
	{
		return (action_mode2 >= UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET &&
		        action_mode2 <= UnitConstants.ACTION_MONSTER_DEFEND_BACK_FIRM);
	}

	public void clear_unit_defense_mode()
	{
		//------- cancel defense mode and continue the current action -------//
		action_mode2 = action_mode;
		action_para2 = action_para;
		action_x_loc2 = action_x_loc;
		action_y_loc2 = action_y_loc;

		reset_action_misc_para();

		if (unit_mode == UnitConstants.UNIT_MODE_DEFEND_TOWN)
			set_mode(0); // reset unit mode 
	}

	public void clear_town_defend_mode()
	{
		//------- cancel defense mode and continue the current action -------//
		action_mode2 = action_mode;
		action_para2 = action_para;
		action_x_loc2 = action_x_loc;
		action_y_loc2 = action_y_loc;

		reset_action_misc_para();
	}

	public void clear_monster_defend_mode()
	{
		//------- cancel defense mode and continue the current action -------//
		action_mode2 = action_mode;
		action_para2 = action_para;
		action_x_loc2 = action_x_loc;
		action_y_loc2 = action_y_loc;

		reset_action_misc_para();
	}

	//---------- embark to ship and other ship functions ---------//
	public void assign_to_ship(int destX, int destY, int shipRecno, int miscNo = 0)
	{
		//----------------------------------------------------------------//
		// return if the unit is dead
		//----------------------------------------------------------------//
		if (hit_points <= 0.0 || action_mode == UnitConstants.ACTION_DIE || cur_action == SPRITE_DIE)
			return;

		if (UnitArray.IsDeleted(shipRecno))
			return;

		//----------------------------------------------------------------//
		// action_mode2: checking for equal action or idle action
		//----------------------------------------------------------------//
		if (action_mode2 == UnitConstants.ACTION_ASSIGN_TO_SHIP &&
		    action_para2 == shipRecno && action_x_loc2 == destX && action_y_loc2 == destY)
		{
			if (cur_action != SPRITE_IDLE)
				return;
		}
		else
		{
			//----------------------------------------------------------------//
			// action_mode2: store new order
			//----------------------------------------------------------------//
			action_mode2 = UnitConstants.ACTION_ASSIGN_TO_SHIP;
			action_para2 = shipRecno;
			action_x_loc2 = destX;
			action_y_loc2 = destY;
		}

		//----- order the sprite to stop as soon as possible -----//
		stop(); // new order

		Unit ship = UnitArray[shipRecno];
		int shipXLoc = ship.next_x_loc();
		int shipYLoc = ship.next_y_loc();
		bool resultXYLocWritten = false;
		int resultXLoc = -1, resultYLoc = -1;
		if (miscNo == 0)
		{
			//-------- find a suitable location since no offset location is given ---------//
			if (Math.Abs(shipXLoc - action_x_loc2) <= 1 && Math.Abs(shipYLoc - action_y_loc2) <= 1)
			{
				Location loc = World.get_loc(next_x_loc(), next_y_loc());
				int regionId = loc.region_id;
				for (int i = 2; i <= 9; i++)
				{
					int xShift, yShift;
					Misc.cal_move_around_a_point(i, 3, 3, out xShift, out yShift);
					int checkXLoc = shipXLoc + xShift;
					int checkYLoc = shipYLoc + yShift;
					if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
						continue;

					loc = World.get_loc(checkXLoc, checkYLoc);
					if (loc.region_id != regionId)
						continue;

					resultXYLocWritten = true;
					resultXLoc = checkXLoc;
					resultYLoc = checkYLoc;

					break;
				}
			}
			else
			{
				resultXYLocWritten = true;
				resultXLoc = action_x_loc2;
				resultYLoc = action_y_loc2;
			}
		}
		else
		{
			//------------ offset location is given, move there directly ----------//
			int xShift, yShift;
			Misc.cal_move_around_a_point(miscNo, GameConstants.MapSize, GameConstants.MapSize, out xShift, out yShift);
			resultXLoc = destX + xShift;
			resultYLoc = destY + yShift;
		}

		//--------- start searching ----------//
		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();
		if ((curXLoc != destX || curYLoc != destY) &&
		    (Math.Abs(shipXLoc - curXLoc) > 1 || Math.Abs(shipYLoc - curYLoc) > 1))
			search(resultXLoc, resultYLoc, 1);

		//-------- set action parameters ----------//
		action_mode = UnitConstants.ACTION_ASSIGN_TO_SHIP;
		action_para = shipRecno;
		action_x_loc = destX;
		action_y_loc = destY;
	}

	public void ship_to_beach(int destX, int destY, out int finalDestX, out int finalDestY) // for ship only
	{
		//----------------------------------------------------------------//
		// change to move_to if the unit is dead
		//----------------------------------------------------------------//
		if (hit_points <= 0.0 || action_mode == UnitConstants.ACTION_DIE || cur_action == SPRITE_DIE)
		{
			move_to(destX, destY, 1);
			finalDestX = finalDestY = -1;
			return;
		}

		//----------------------------------------------------------------//
		// change to move_to if the ship cannot carry units
		//----------------------------------------------------------------//
		if (UnitRes[unit_id].carry_unit_capacity <= 0)
		{
			move_to(destX, destY, 1);
			finalDestX = finalDestY = -1;
			return;
		}

		//----------------------------------------------------------------//
		// calculate new destination
		//----------------------------------------------------------------//
		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();
		int resultXLoc, resultYLoc;

		stop();

		if (Math.Abs(destX - curXLoc) > 1 || Math.Abs(destY - curYLoc) > 1)
		{
			//-----------------------------------------------------------------------------//
			// get a suitable location in the territory as a reference location
			//-----------------------------------------------------------------------------//
			Location loc = World.get_loc(destX, destY);
			int regionId = loc.region_id;
			int xStep = curXLoc - destX;
			int yStep = curYLoc - destY;
			int absXStep = Math.Abs(xStep);
			int absYStep = Math.Abs(yStep);
			int count = (absXStep >= absYStep) ? absXStep : absYStep;
			int sameTerr = 0;

			for (int i = 1; i <= count; i++)
			{
				int x = destX + (i * xStep) / count;
				int y = destY + (i * yStep) / count;

				loc = World.get_loc(x, y);
				if (loc.region_id == regionId)
				{
					if (loc.walkable())
						sameTerr = i;
				}
			}

			if (sameTerr != 0)
			{
				resultXLoc = destX + (sameTerr * xStep) / count;
				resultYLoc = destY + (sameTerr * yStep) / count;
			}
			else
			{
				resultXLoc = destX;
				resultYLoc = destY;
			}

			//------------------------------------------------------------------------------//
			// find the path from the ship location in the ocean to the reference location
			// in the territory
			//------------------------------------------------------------------------------//
			if (!ship_to_beach_path_edit(ref resultXLoc, ref resultYLoc, regionId))
			{
				finalDestX = finalDestY = -1;
				return; // calling move_to() instead
			}
		}
		else
		{
			resultXLoc = destX;
			resultYLoc = destY;
		}

		action_mode = action_mode2 = UnitConstants.ACTION_SHIP_TO_BEACH;
		action_para = action_para2 = 0;
		finalDestX = action_x_loc = action_x_loc2 = resultXLoc;
		finalDestY = action_y_loc = action_y_loc2 = resultYLoc;
	}

	//----------- other main action functions -------------//
	public void build_firm(int buildXLoc, int buildYLoc, int firmId, int remoteAction)
	{
		//if(!remoteAction && remote.is_enable() )
		//{
		//// packet structure : <unit recno> <xLoc> <yLoc> <firmId>
		//short *shortPtr =(short *)remote.new_send_queue_msg(MSG_UNIT_BUILD_FIRM, 4*sizeof(short) );
		//shortPtr[0] = sprite_recno;
		//shortPtr[1] = buildXLoc;
		//shortPtr[2] = buildYLoc;
		//shortPtr[3] = firmId;
		//return;
		//}

		//----------------------------------------------------------------//
		// return if the unit is dead
		//----------------------------------------------------------------//
		if (hit_points <= 0.0 || action_mode == UnitConstants.ACTION_DIE || cur_action == SPRITE_DIE)
			return;

		//----------------------------------------------------------------//
		// location is blocked, cannot build. so move there instead
		//----------------------------------------------------------------//
		if (World.can_build_firm(buildXLoc, buildYLoc, firmId, sprite_recno) == 0)
		{
			//reset_action_para2();
			move_to(buildXLoc, buildYLoc);
			return;
		}

		//----------------------------------------------------------------//
		// different territory
		//----------------------------------------------------------------//

		int harborDir = World.can_build_firm(buildXLoc, buildYLoc, firmId, sprite_recno);
		int goX = buildXLoc, goY = buildYLoc;
		if (FirmRes[firmId].tera_type == 4)
		{
			switch (harborDir)
			{
				case 1: // north exit
					goX += 1;
					goY += 2;
					break;
				case 2: // south exit
					goX += 1;
					break;
				case 4: // west exit
					goX += 2;
					goY += 1;
					break;
				case 8: // east exit
					goY += 1;
					break;
				default:
					move_to(buildXLoc, buildYLoc);
					return;
			}

			if (World.get_loc(next_x_loc(), next_y_loc()).region_id != World.get_loc(goX, goY).region_id)
			{
				move_to(buildXLoc, buildYLoc);
				return;
			}
		}
		else
		{
			if (World.get_loc(next_x_loc(), next_y_loc()).region_id != World.get_loc(buildXLoc, buildYLoc).region_id)
			{
				move_to(buildXLoc, buildYLoc);
				return;
			}
		}

		//----------------------------------------------------------------//
		// action_mode2: checking for equal action or idle action
		//----------------------------------------------------------------//
		if (action_mode2 == UnitConstants.ACTION_BUILD_FIRM &&
		    action_para2 == firmId && action_x_loc2 == buildXLoc && action_y_loc2 == buildYLoc)
		{
			if (cur_action != SPRITE_IDLE)
				return;
		}
		else
		{
			//----------------------------------------------------------------//
			// action_mode2: store new order
			//----------------------------------------------------------------//
			action_mode2 = UnitConstants.ACTION_BUILD_FIRM;
			action_para2 = firmId;
			action_x_loc2 = buildXLoc;
			action_y_loc2 = buildYLoc;
		}

		//----- order the sprite to stop as soon as possible -----//
		stop(); // new order

		//---------------- define parameters -------------------//
		FirmInfo firmInfo = FirmRes[firmId];
		int firmWidth = firmInfo.loc_width;
		int firmHeight = firmInfo.loc_height;

		if (!is_in_surrounding(move_to_x_loc, move_to_y_loc, sprite_info.loc_width,
			    buildXLoc, buildYLoc, firmWidth, firmHeight))
		{
			//----------- not in the firm surrounding ---------//
			set_move_to_surround(buildXLoc, buildYLoc, firmWidth, firmHeight, UnitConstants.BUILDING_TYPE_FIRM_BUILD, firmId);
		}
		else
		{
			//------- the unit is in the firm surrounding -------//
			set_cur(next_x, next_y);
			set_dir(move_to_x_loc, move_to_y_loc, buildXLoc + firmWidth / 2, buildYLoc + firmHeight / 2);
		}

		//----------- set action to build the firm -----------//
		action_mode = UnitConstants.ACTION_BUILD_FIRM;
		action_para = firmId;
		action_x_loc = buildXLoc;
		action_y_loc = buildYLoc;
	}

	public void burn(int burnXLoc, int burnYLoc, int remoteAction)
	{
		//if( !remoteAction && remote.is_enable() )
		//{
		//// packet structure : <unit recno> <xLoc> <yLoc>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_UNIT_BURN, 3*sizeof(short) );
		//shortPtr[0] = sprite_recno;
		//shortPtr[1] = burnXLoc;
		//shortPtr[2] = burnYLoc;
		//return;
		//}

		if (move_to_x_loc == burnXLoc && move_to_y_loc == burnYLoc)
			return; // should not burn the unit itself

		//----------------------------------------------------------------//
		// return if the unit is dead
		//----------------------------------------------------------------//
		if (hit_points <= 0.0 || action_mode == UnitConstants.ACTION_DIE || cur_action == SPRITE_DIE)
			return;

		//----------------------------------------------------------------//
		// move there instead if ordering to different territory
		//----------------------------------------------------------------//
		if (World.get_loc(next_x_loc(), next_y_loc()).region_id != World.get_loc(burnXLoc, burnYLoc).region_id)
		{
			move_to(burnXLoc, burnYLoc);
			return;
		}

		//----------------------------------------------------------------//
		// action_mode2: checking for equal action or idle action
		//----------------------------------------------------------------//
		if (action_mode2 == UnitConstants.ACTION_BURN && action_x_loc2 == burnXLoc && action_y_loc2 == burnYLoc)
		{
			if (cur_action != SPRITE_IDLE)
				return;
		}
		else
		{
			//----------------------------------------------------------------//
			// action_mode2: store new order
			//----------------------------------------------------------------//
			action_mode2 = UnitConstants.ACTION_BURN;
			action_para2 = 0;
			action_x_loc2 = burnXLoc;
			action_y_loc2 = burnYLoc;
		}

		//----- order the sprite to stop as soon as possible -----//
		stop(); // new order

		if (Math.Abs(burnXLoc - next_x_loc()) > 1 || Math.Abs(burnYLoc - next_y_loc()) > 1)
		{
			//--- if the unit is not in the burning surrounding location, move there first ---//
			search(burnXLoc, burnYLoc, 1, SeekPath.SEARCH_MODE_A_UNIT_IN_GROUP);

			if (move_to_x_loc != burnXLoc || move_to_y_loc != burnYLoc) // cannot reach the destination
			{
				action_mode = UnitConstants.ACTION_BURN;
				action_para = 0;
				action_x_loc = burnXLoc;
				action_y_loc = burnYLoc;
				return; // just move to the closest location returned by shortest path searching
			}
		}
		else
		{
			if (cur_x == next_x && cur_y == next_y)
				set_dir(next_x_loc(), next_y_loc(), burnXLoc, burnYLoc);
		}

		//--------------------------------------------------------//
		// edit the result path such that the unit can reach the
		// burning location surrounding
		//--------------------------------------------------------//
		if (result_node_array != null && result_node_count > 0)
		{
			//--------------------------------------------------------//
			// there should be at least two nodes, and should take at
			// least two steps to the destination
			//--------------------------------------------------------//

			ResultNode lastNode1 = result_node_array[result_node_count - 1]; // the last node
			ResultNode lastNode2 = result_node_array[result_node_count - 2]; // the node before the last node

			int vX = lastNode1.node_x - lastNode2.node_x; // get the vectors direction
			int vY = lastNode1.node_y - lastNode2.node_y;
			int vDirX = (vX != 0) ? vX / Math.Abs(vX) : 0;
			int vDirY = (vY != 0) ? vY / Math.Abs(vY) : 0;

			if (result_node_count > 2) // go_? should not be the burning location 
			{
				if (Math.Abs(vX) > 1 || Math.Abs(vY) > 1)
				{
					lastNode1.node_x -= vDirX;
					lastNode1.node_y -= vDirY;

					move_to_x_loc = lastNode1.node_x;
					move_to_y_loc = lastNode1.node_y;
				}
				else // move only one step
				{
					result_node_count--; // remove a node
					move_to_x_loc = lastNode2.node_x;
					move_to_y_loc = lastNode2.node_y;
				}
			}
			else // go_? may be the burning location
			{
				lastNode1.node_x -= vDirX;
				lastNode1.node_y -= vDirY;

				if (go_x >> InternalConstants.CellWidthShift == burnXLoc &&
				    go_y >> InternalConstants.CellHeightShift == burnYLoc) // go_? is the burning location
				{
					//--- edit parameters such that only moving to the nearby location to do the action ---//

					go_x = lastNode1.node_x * InternalConstants.CellWidth;
					go_y = lastNode1.node_y * InternalConstants.CellHeight;
				}
				//else the unit is still doing sthg else, no action here

				move_to_x_loc = lastNode1.node_x;
				move_to_y_loc = lastNode1.node_y;
			}

			//--------------------------------------------------------------//
			// reduce the result_path_dist by 1
			//--------------------------------------------------------------//
			result_path_dist--;
		}

		//-- set action if the burning location can be reached, otherwise just move nearby --//
		action_mode = UnitConstants.ACTION_BURN;
		action_para = 0;
		action_x_loc = burnXLoc;
		action_y_loc = burnYLoc;
	}

	public void settle(int settleXLoc, int settleYLoc, int curSettleUnitNum = 1)
	{
		//----------------------------------------------------------------//
		// return if the unit is dead
		//----------------------------------------------------------------//
		if (hit_points <= 0.0 || action_mode == UnitConstants.ACTION_DIE || cur_action == SPRITE_DIE)
			return;

		//---------- no settle for non-human -----------//
		if (UnitRes[unit_id].unit_class != UnitConstants.UNIT_CLASS_HUMAN)
			return;

		//----------------------------------------------------------------//
		// move there if cannot settle
		//----------------------------------------------------------------//
		if (!World.can_build_town(settleXLoc, settleYLoc, sprite_recno))
		{
			Location loc = World.get_loc(settleXLoc, settleYLoc);
			if (loc.is_town() && TownArray[loc.town_recno()].NationId == nation_recno)
				assign(settleXLoc, settleYLoc);
			else
				move_to(settleXLoc, settleYLoc);
			return;
		}

		//----------------------------------------------------------------//
		// move there if location is in different territory
		//----------------------------------------------------------------//
		if (World.get_loc(next_x_loc(), next_y_loc()).region_id != World.get_loc(settleXLoc, settleYLoc).region_id)
		{
			move_to(settleXLoc, settleYLoc);
			return;
		}

		//----------------------------------------------------------------//
		// action_mode2: checking for equal action or idle action
		//----------------------------------------------------------------//
		if (action_mode2 == UnitConstants.ACTION_SETTLE && action_x_loc2 == settleXLoc && action_y_loc2 == settleYLoc)
		{
			if (cur_action != SPRITE_IDLE)
				return;
		}
		else
		{
			//----------------------------------------------------------------//
			// action_mode2: store new order
			//----------------------------------------------------------------//
			action_mode2 = UnitConstants.ACTION_SETTLE;
			action_para2 = 0;
			action_x_loc2 = settleXLoc;
			action_y_loc2 = settleYLoc;
		}

		//----- order the sprite to stop as soon as possible -----//
		stop(); // new order

		if (!is_in_surrounding(move_to_x_loc, move_to_y_loc, sprite_info.loc_width,
			    settleXLoc, settleYLoc, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT))
		{
			//------------ not in the town surrounding ------------//
			set_move_to_surround(settleXLoc, settleYLoc, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT,
				UnitConstants.BUILDING_TYPE_SETTLE, 0, 0, curSettleUnitNum);
		}
		else
		{
			//------- the unit is within the settle location -------//
			set_cur(next_x, next_y);
			set_dir(move_to_x_loc, move_to_y_loc, settleXLoc + InternalConstants.TOWN_WIDTH / 2,
				settleYLoc + InternalConstants.TOWN_HEIGHT / 2);
		}

		//----------- set action to settle -----------//
		action_mode = UnitConstants.ACTION_SETTLE;
		action_para = 0;
		action_x_loc = settleXLoc;
		action_y_loc = settleYLoc;
	}

	public void assign(int assignXLoc, int assignYLoc, int curAssignUnitNum = 1)
	{
		//----------------------------------------------------------------//
		// return if the unit is dead
		//----------------------------------------------------------------//
		if (hit_points <= 0.0 || action_mode == UnitConstants.ACTION_DIE || cur_action == SPRITE_DIE)
			return;

		//----------- BUGHERE : cannot assign when on a ship -----------//
		if (!is_visible())
			return;

		//----------- cannot assign for caravan -----------//
		if (unit_id == UnitConstants.UNIT_CARAVAN)
			return;

		//----------------------------------------------------------------//
		// move there if the destination in other territory
		//----------------------------------------------------------------//
		Location loc = World.get_loc(assignXLoc, assignYLoc);
		int unitRegionId = World.get_loc(next_x_loc(), next_y_loc()).region_id;
		if (loc.is_firm())
		{
			Firm firm = FirmArray[loc.firm_recno()];
			bool quit = false;

			if (firm.firm_id == Firm.FIRM_HARBOR)
			{
				FirmHarbor harbor = (FirmHarbor)firm;
				switch (UnitRes[unit_id].unit_class)
				{
					case UnitConstants.UNIT_CLASS_HUMAN:
						if (unitRegionId != harbor.land_region_id)
							quit = true;
						break;

					case UnitConstants.UNIT_CLASS_SHIP:
						if (unitRegionId != harbor.sea_region_id)
							quit = true;
						break;

					default:
						break;
				}
			}
			else if (unitRegionId != loc.region_id)
			{
				quit = true;
			}

			if (quit)
			{
				move_to_firm_surround(assignXLoc, assignYLoc, sprite_info.loc_width, sprite_info.loc_height,
					firm.firm_id);
				return;
			}
		}
		else if (unitRegionId != loc.region_id)
		{
			if (loc.is_town())
				move_to_town_surround(assignXLoc, assignYLoc, sprite_info.loc_width, sprite_info.loc_height);
			/*else if(loc.has_unit(UnitRes.UNIT_LAND))
			{
				Unit *unit = UnitArray[loc.unit_recno(UnitRes.UNIT_LAND)];
				move_to_unit_surround(assignXLoc, assignYLoc, sprite_info.loc_width, sprite_info.loc_height, unit.sprite_recno);
			}*/

			return;
		}

		//---------------- define parameters --------------------//
		int width, height;
		int buildingType = 0; // 1 for Firm, 2 for TownZone
		int recno;
		int firmNeedUnit = 1;

		if (loc.is_firm())
		{
			//-------------------------------------------------------//
			// the location is firm
			//-------------------------------------------------------//
			recno = loc.firm_recno();

			//----------------------------------------------------------------//
			// action_mode2: checking for equal action or idle action
			//----------------------------------------------------------------//
			if (action_mode2 == UnitConstants.ACTION_ASSIGN_TO_FIRM &&
			    action_para2 == recno && action_x_loc2 == assignXLoc && action_y_loc2 == assignYLoc)
			{
				if (cur_action != SPRITE_IDLE)
					return;
			}
			else
			{
				//----------------------------------------------------------------//
				// action_mode2: store new order
				//----------------------------------------------------------------//
				action_mode2 = UnitConstants.ACTION_ASSIGN_TO_FIRM;
				action_para2 = recno;
				action_x_loc2 = assignXLoc;
				action_y_loc2 = assignYLoc;
			}

			Firm firm = FirmArray[recno];
			FirmInfo firmInfo = FirmRes[firm.firm_id];

			if (firm_can_assign(recno) == 0)
			{
				//firmNeedUnit = 0; // move to the surrounding of the firm
				move_to_firm_surround(assignXLoc, assignYLoc, sprite_info.loc_width, sprite_info.loc_height,
					firm.firm_id);
				return;
			}

			width = firmInfo.loc_width;
			height = firmInfo.loc_height;
			buildingType = UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO;
		}
		else if (loc.is_town()) // there is town
		{
			if (UnitRes[unit_id].unit_class != UnitConstants.UNIT_CLASS_HUMAN)
				return;

			//-------------------------------------------------------//
			// the location is town
			//-------------------------------------------------------//
			recno = loc.town_recno();

			//----------------------------------------------------------------//
			// action_mode2: checking for equal action or idle action
			//----------------------------------------------------------------//
			if (action_mode2 == UnitConstants.ACTION_ASSIGN_TO_TOWN &&
			    action_para2 == recno && action_x_loc2 == assignXLoc && action_y_loc2 == assignYLoc)
			{
				if (cur_action != SPRITE_IDLE)
					return;
			}
			else
			{
				//----------------------------------------------------------------//
				// action_mode2: store new order
				//----------------------------------------------------------------//
				action_mode2 = UnitConstants.ACTION_ASSIGN_TO_TOWN;
				action_para2 = recno;
				action_x_loc2 = assignXLoc;
				action_y_loc2 = assignYLoc;
			}

			Town targetTown = TownArray[recno];
			if (TownArray[recno].NationId != nation_recno)
			{
				move_to_town_surround(assignXLoc, assignYLoc, sprite_info.loc_width, sprite_info.loc_height);
				return;
			}

			width = targetTown.LocWidth();
			height = targetTown.LocHeight();
			buildingType = UnitConstants.BUILDING_TYPE_TOWN_MOVE_TO;
		}
		/*else if(loc.has_unit(UnitRes.UNIT_LAND)) // there is vehicle
		{
			//-------------------------------------------------------//
			// the location is vehicle
			//-------------------------------------------------------//
			Unit* vehicleUnit = UnitArray[loc.unit_recno(UnitRes.UNIT_LAND)];
			if( vehicleUnit.unit_id!=UnitRes[unit_id].vehicle_id )
				return;
	
			recno = vehicleUnit.sprite_recno;
	
			//----------------------------------------------------------------//
			// action_mode2: checking for equal action or idle action
			//----------------------------------------------------------------//
			if(action_mode2==ACTION_ASSIGN_TO_VEHICLE && action_para2==recno && action_x_loc2==assignXLoc && action_y_loc2==assignYLoc)
			{
				if(cur_action!=SPRITE_IDLE)
					return;
			}
			else
			{
				//----------------------------------------------------------------//
				// action_mode2: store new order
				//----------------------------------------------------------------//
				action_mode2  = ACTION_ASSIGN_TO_VEHICLE;
				action_para2  = recno;
				action_x_loc2 = assignXLoc;
				action_y_loc2 = assignYLoc;
			}
	
			SpriteInfo* spriteInfo = vehicleUnit.sprite_info;
			width	 = spriteInfo.loc_width;
			height = spriteInfo.loc_height;
			buildingType = UnitConstants.BUILDING_TYPE_VEHICLE;
		}*/
		else
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//-----------------------------------------------------------------//
		// order the sprite to stop as soon as possible (new order)
		//-----------------------------------------------------------------//
		stop();
		set_move_to_surround(assignXLoc, assignYLoc, width, height, buildingType, 0, 0, curAssignUnitNum);

		//-----------------------------------------------------------------//
		// able to reach building surrounding, set action parameters
		//-----------------------------------------------------------------//
		action_para = recno;
		action_x_loc = assignXLoc;
		action_y_loc = assignYLoc;

		switch (buildingType)
		{
			case UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO:
				action_mode = UnitConstants.ACTION_ASSIGN_TO_FIRM;
				break;

			case UnitConstants.BUILDING_TYPE_TOWN_MOVE_TO:
				action_mode = UnitConstants.ACTION_ASSIGN_TO_TOWN;
				break;

			case UnitConstants.BUILDING_TYPE_VEHICLE:
				action_mode = UnitConstants.ACTION_ASSIGN_TO_VEHICLE;
				break;
		}

		//##### begin trevor 9/10 #######//

		//	force_move_flag = 1;		// don't stop and fight back on an assign mission

		//##### end trevor 9/10 #######//

		//-----------------------------------------------------------------//
		// edit parameters for those firms don't need unit
		//-----------------------------------------------------------------//
		/*if(!firmNeedUnit)
		{
			action_mode2 = action_mode = ACTION_MOVE;
			action_para2 = action_para = 0;
			action_x_loc2 = action_x_loc = move_to_x_loc;
			action_y_loc2 = action_y_loc = move_to_y_loc;
		}*/
	}

	public void go_cast_power(int castXLoc, int castYLoc, int castPowerType, int remoteAction)
	{
		//----------------------------------------------------------------//
		// return if the unit is dead
		//----------------------------------------------------------------//
		if (hit_points <= 0.0 || action_mode == UnitConstants.ACTION_DIE || cur_action == SPRITE_DIE)
			return;

		//if(!remoteAction && remote.is_enable() )
		//{
		////------------ process multiplayer calling ---------------//
		//// packet structure : <unit recno> <xLoc> <yLoc> <power type>
		//short *short =(short *)remote.new_send_queue_msg(MSG_U_GOD_CAST, 4*sizeof(short) );
		//shortPtr[0] = sprite_recno;
		//shortPtr[1] = castXLoc;
		//shortPtr[2] = castYLoc;
		//shortPtr[3] = castPowerType;
		//return;
		//}

		UnitGod unitGod = (UnitGod)this;

		//----------------------------------------------------------------//
		// action_mode2: checking for equal action or idle action
		//----------------------------------------------------------------//
		if (action_mode2 == UnitConstants.ACTION_GO_CAST_POWER &&
		    action_para2 == 0 && action_x_loc2 == castXLoc && action_y_loc2 == castYLoc &&
		    unitGod.cast_power_type == castPowerType)
		{
			if (cur_action != SPRITE_IDLE)
				return;
		}
		else
		{
			//----------------------------------------------------------------//
			// action_mode2: store new order
			//----------------------------------------------------------------//
			action_mode2 = UnitConstants.ACTION_GO_CAST_POWER;
			action_para2 = 0;
			action_x_loc2 = castXLoc;
			action_y_loc2 = castYLoc;
		}

		//----- order the sprite to stop as soon as possible -----//
		stop(); // new order

		//------------- do searching if neccessary -------------//
		if (Misc.points_distance(next_x_loc(), next_y_loc(), castXLoc, castYLoc) > UnitConstants.DO_CAST_POWER_RANGE)
			search(castXLoc, castYLoc, 1);

		//----------- set action to build the firm -----------//
		action_mode = UnitConstants.ACTION_GO_CAST_POWER;
		action_para = 0;
		action_x_loc = castXLoc;
		action_y_loc = castYLoc;

		unitGod.cast_power_type = castPowerType;
		unitGod.cast_origin_x = next_x_loc();
		unitGod.cast_origin_y = next_y_loc();
		unitGod.cast_target_x = castXLoc;
		unitGod.cast_target_y = castYLoc;
	}

	public void AddWayPoint(int x, int y)
	{
		if (wayPoints.Count > 1) // don't allow to remove the 1st node, since the unit is moving there
		{
			for (int i = wayPoints.Count - 1; i >= 0; i--)
			{
				ResultNode node = wayPoints[i];
				if (node.node_x == x && node.node_y == y) // remove this node
				{
					wayPoints.RemoveAt(i);
					return; // there should be one and only one node with the same value
				}
			}
		}

		//-------------- add new node -----------------//
		ResultNode newNode = new ResultNode(x, y);
		wayPoints.Add(newNode);

		if (wayPoints.Count == 1)
			move_to(x, y);
	}

	public void ResetWayPoints()
	{
		//------------------------------------------------------------------------------------//
		// There are only two conditions to reset the way_point_array
		// 1) action_mode2!=ACTION_MOVE in Unit::stop()
		// 2) dest? != node_? in the first node of way_point_array in calling Unit::move_to()
		//------------------------------------------------------------------------------------//
		wayPoints.Clear();
	}

	public void ProcessWayPoint()
	{
		int destX, destY;
		if (wayPoints.Count > 1)
		{
			ResultNode node = wayPoints[1];
			destX = node.node_x;
			destY = node.node_y;
			wayPoints.RemoveAt(1);
		}
		else // only one unprocessed node
		{
			ResultNode node = wayPoints[0];
			destX = node.node_x;
			destY = node.node_y;
		}

		move_to(destX, destY);
	}

	//------------------------------------//

	public void change_nation(int newNationRecno)
	{
		bool oldAiUnit = ai_unit;
		int oldNationRecno = nation_recno;

		group_select_id = 0; // clear group select id
		if (wayPoints.Count > 0)
			ResetWayPoints();

		//-- if the player is giving a command to this unit, cancel the command --//

		if (nation_recno == NationArray.player_recno && sprite_recno == UnitArray.selected_recno && Power.command_id != 0)
		{
			Power.command_id = 0;
		}

		//---------- stop all action to attack this unit ------------//

		UnitArray.stop_attack_unit(sprite_recno);

		//---- update nation_unit_count_array[] ----//

		UnitRes[unit_id].unit_change_nation(newNationRecno, nation_recno, rank_id);

		//------- if the nation has an AI action -------//

		stop2(); // clear the existing order

		//---------------- update vars ----------------//

		unit_group_id = UnitArray.cur_group_id++; // separate from the current group
		nation_recno = newNationRecno;

		home_camp_firm_recno = 0; // reset it
		original_action_mode = 0;

		if (race_id != 0)
		{
			nation_contribution = 0; // contribution to the nation
			total_reward = 0;
		}

		//-------- if change to one of the existing nations ------//

		ai_unit = nation_recno != 0 && NationArray[nation_recno].is_ai();

		//------------ update AI info --------------//

		if (oldAiUnit)
		{
			Nation nation = NationArray[oldNationRecno];

			if (rank_id == RANK_GENERAL || rank_id == RANK_KING)
				nation.del_general_info(sprite_recno);

			else if (UnitRes[unit_id].unit_class == UnitConstants.UNIT_CLASS_CARAVAN)
				nation.del_caravan_info(sprite_recno);

			else if (UnitRes[unit_id].unit_class == UnitConstants.UNIT_CLASS_SHIP)
				nation.del_ship_info(sprite_recno);
		}

		if (ai_unit && nation_recno != 0)
		{
			Nation nation = NationArray[nation_recno];

			if (rank_id == RANK_GENERAL || rank_id == RANK_KING)
				nation.add_general_info(sprite_recno);

			else if (UnitRes[unit_id].unit_class == UnitConstants.UNIT_CLASS_CARAVAN)
				nation.add_caravan_info(sprite_recno);

			else if (UnitRes[unit_id].unit_class == UnitConstants.UNIT_CLASS_SHIP)
				nation.add_ship_info(sprite_recno);
		}

		//------ if this unit oversees a firm -----//

		if (unit_mode == UnitConstants.UNIT_MODE_OVERSEE)
			FirmArray[unit_mode_para].change_nation(newNationRecno);

		//----- this unit was defending the town before it gets killed ----//

		else if (unit_mode == UnitConstants.UNIT_MODE_DEFEND_TOWN)
		{
			if (!TownArray.IsDeleted(unit_mode_para))
				TownArray[unit_mode_para].ReduceDefenderCount();

			set_mode(0); // reset unit mode
		}

		//---- if the unit is no longer the same nation as the leader ----//

		if (leader_unit_recno != 0)
		{
			Unit leaderUnit = UnitArray[leader_unit_recno];

			if (leaderUnit.nation_recno != nation_recno)
			{
				leaderUnit.del_team_member(sprite_recno);
				leader_unit_recno = 0;
				team_id = 0;
			}
		}

		//------ if it is currently selected -------//

		if (selected_flag)
			Info.disp();
	}

	public void overseer_migrate(int destTownRecno)
	{
		int curTownRecno = FirmArray[unit_mode_para].overseer_town_recno;

		//------- decrease the population of the unit's home town ------//

		TownArray[curTownRecno].DecPopulation(race_id, true);

		//--------- increase the population of the target town ------//

		TownArray[destTownRecno].IncPopulation(race_id, true, loyalty);
	}

	public bool caravan_in_firm()
	{
		return cur_x == -2;
	}

	public void update_loyalty()
	{
		if (nation_recno == 0 || rank_id == RANK_KING || UnitRes[unit_id].race_id == 0)
			return;

		// constructor worker will not change their loyalty when they are in a building
		if (unit_mode == UnitConstants.UNIT_MODE_CONSTRUCT)
			return;

		// The following never really worked that well, since it created a dead give away due to the constant loyalty.
		//----- if this unit is a spy, set its fake loyalty ------//

		if (ConfigAdv.unit_spy_fixed_target_loyalty && spy_recno != 0) // a spy's loyalty is always >= 70
		{
			if (loyalty < 70)
				loyalty = 70 + Misc.Random(20); // initialize it to be a number between 70 and 90

			target_loyalty = loyalty;
			return;
		}

		//-------- if this is a general ---------//

		Nation ownNation = NationArray[nation_recno];
		int rc = 0;

		if (rank_id == RANK_GENERAL)
		{
			//----- the general's power affect his loyalty ----//

			int targetLoyalty = commander_power();

			//----- the king's race affects the general's loyalty ----//

			if (ownNation.race_id == race_id)
				targetLoyalty += 20;

			//----- the kingdom's reputation affects the general's loyalty ----//

			targetLoyalty += (int)(ownNation.reputation / 4.0);

			//--- the king's leadership also affect the general's loyalty -----//

			if (ownNation.king_unit_recno != 0)
				targetLoyalty += UnitArray[ownNation.king_unit_recno].skill.skill_level / 4;

			//-- if the unit is rewarded less than the amount of contribution he made, he will become unhappy --//

			if (nation_contribution > total_reward * 2)
			{
				int decLoyalty = (nation_contribution - total_reward * 2) / 2;
				targetLoyalty -= Math.Min(50, decLoyalty); // this affect 50 points at maximum
			}

			targetLoyalty = Math.Min(targetLoyalty, 100);
			target_loyalty = Math.Max(targetLoyalty, 0);
		}

		//-------- if this is a soldier ---------//

		else if (rank_id == RANK_SOLDIER)
		{
			bool leader_bonus = ConfigAdv.unit_loyalty_require_local_leader ? is_leader_in_range() : leader_unit_recno != 0;
			if (leader_bonus)
			{
				//----------------------------------------//
				//
				// If this soldier is led by a general,
				// the targeted loyalty
				//
				// = race friendliness between the unit and the general / 2
				//   + the leader unit's leadership / 2
				//
				//----------------------------------------//

				Unit leaderUnit = UnitArray[leader_unit_recno];

				int targetLoyalty = 30 + leaderUnit.skill.get_skill(Skill.SKILL_LEADING);

				//---------------------------------------------------//
				//
				// Soldiers with higher combat and leadership skill
				// will get discontented if they are led by a general
				// with low leadership.
				//
				//---------------------------------------------------//

				targetLoyalty -= skill.combat_level / 2;
				targetLoyalty -= skill.skill_level;

				if (leaderUnit.rank_id == RANK_KING)
					targetLoyalty += 20;

				if (RaceRes.is_same_race(race_id, leaderUnit.race_id))
					targetLoyalty += 20;

				if (targetLoyalty < 0)
					targetLoyalty = 0;

				targetLoyalty = Math.Min(targetLoyalty, 100);
				target_loyalty = Math.Max(targetLoyalty, 0);
			}
			else
			{
				target_loyalty = 0;
			}
		}

		//--------- update loyalty ---------//

		// only increase, no decrease. Decrease are caused by events. Increases are made gradually
		if (target_loyalty > loyalty)
		{
			int incValue = (target_loyalty - loyalty) / 10;

			int newLoyalty = loyalty + Math.Max(1, incValue);

			if (newLoyalty > target_loyalty)
				newLoyalty = target_loyalty;

			loyalty = newLoyalty;
		}
		// only increase, no decrease. Decrease are caused by events. Increases are made gradually
		else if (target_loyalty < loyalty)
		{
			loyalty--;
		}
	}

	public void set_combat_level(int combatLevel)
	{
		skill.combat_level = combatLevel;

		UnitInfo unitInfo = UnitRes[unit_id];

		int oldMaxHitPoints = max_hit_points;

		max_hit_points = unitInfo.hit_points * combatLevel / 100;

		if (oldMaxHitPoints != 0)
			hit_points = hit_points * max_hit_points / oldMaxHitPoints;

		hit_points = Math.Min(hit_points, max_hit_points);

		// --------- update can_guard_flag -------//

		if (combatLevel >= unitInfo.guard_combat_level)
		{
			can_guard_flag = sprite_info.can_guard_flag;
			if (unit_id == UnitConstants.UNIT_ZULU)
				can_guard_flag |= 4; // shield during attack delay
		}
		else
		{
			can_guard_flag = 0;
		}

		max_power = skill.combat_level + 50;
		cur_power = Math.Min(cur_power, max_power);
	}

	public void inc_minor_combat_level(int incLevel)
	{
		skill.combat_level_minor += incLevel;

		if (skill.combat_level_minor > 100)
		{
			if (skill.combat_level < 100)
				set_combat_level(skill.combat_level + 1);

			skill.combat_level_minor -= 100;
		}
	}

	public void inc_minor_skill_level(int incLevel)
	{
		skill.skill_level_minor += incLevel;

		if (skill.skill_level_minor > 100)
		{
			if (skill.skill_level < 100)
				skill.skill_level++;

			skill.skill_level_minor -= 100;
		}
	}

	public void set_rank(int rankId)
	{
		if (rank_id == rankId)
			return;

		//------- promote --------//

		if (rankId > rank_id)
			change_loyalty(GameConstants.PROMOTE_LOYALTY_INCREASE);

		//------- demote -----------//

		// no decrease in loyalty if a spy king hands his nation to his parent nation and become a general again
		else if (rankId < rank_id && rank_id != RANK_KING)
			change_loyalty(-GameConstants.DEMOTE_LOYALTY_DECREASE);

		//---- update nation_general_count_array[] ----//

		if (nation_recno != 0)
		{
			UnitInfo unitInfo = UnitRes[unit_id];

			if (rank_id == RANK_GENERAL) // if it was a general originally
				unitInfo.dec_nation_general_count(nation_recno);

			if (rankId == RANK_GENERAL) // if the new rank is general
				unitInfo.inc_nation_general_count(nation_recno);

			//------ if demote a king to a unit ------//

			// since kings are not included in nation_unit_count, when it is no longer a king, we need to re-increase it.
			if (rank_id == RANK_KING && rankId != RANK_KING)
				unitInfo.inc_nation_unit_count(nation_recno);

			//------ if promote a unit to a king ------//

			// since kings are not included in nation_unit_count, we need to decrease it
			if (rank_id != RANK_KING && rankId == RANK_KING)
				unitInfo.dec_nation_unit_count(nation_recno);
		}

		//----- reset leader_unit_recno if demote a general to soldier ----//

		if (rank_id == RANK_GENERAL && rankId == RANK_SOLDIER)
		{
			//----- reset leader_unit_recno of the units he commands ----//

			foreach (Unit unit in UnitArray)
			{
				//TODO
				// don't use IsDeleted() as it filters out units that are currently dying
				if (unit.leader_unit_recno == sprite_recno)
				{
					unit.leader_unit_recno = 0;
					unit.team_id = 0;
				}
			}

			//--------- deinit team_info ---------//

			team_info = null;
			team_id = 0;
		}

		//----- if this is a soldier being promoted to a general -----//

		else if (rank_id == RANK_SOLDIER && rankId == RANK_GENERAL)
		{
			//-- if this soldier is formerly commanded by a general, detech it ---//

			if (leader_unit_recno != 0)
			{
				if (!UnitArray.IsDeleted(leader_unit_recno)) // the leader unit may have been killed at the same time
					UnitArray[leader_unit_recno].del_team_member(sprite_recno);

				leader_unit_recno = 0;
			}
		}

		//-------------- update AI info --------------//

		if (ai_unit)
		{
			if (rank_id == RANK_GENERAL || rank_id == RANK_KING)
				NationArray[nation_recno].del_general_info(sprite_recno);

			rank_id = rankId;

			if (rank_id == RANK_GENERAL || rank_id == RANK_KING)
				NationArray[nation_recno].add_general_info(sprite_recno);
		}
		else
		{
			rank_id = rankId;
		}

		//----- if this is a general/king ------//

		if (rank_id == RANK_GENERAL || rank_id == RANK_KING)
		{
			//--------- init team_info -------//

			if (team_info == null)
			{
				team_info = new TeamInfo();
				team_info.ai_last_request_defense_date = default;
			}

			//--- set leadership if this unit does not have any now ----//

			if (skill.skill_id != Skill.SKILL_LEADING)
			{
				skill.skill_id = Skill.SKILL_LEADING;
				skill.skill_level = 10 + Misc.Random(40);
			}
		}

		//------ refresh if the current unit is selected -----//

		if (UnitArray.selected_recno == sprite_recno)
			Info.disp();
	}

	public virtual bool can_resign()
	{
		return rank_id != RANK_KING;
	}

	public void resign(int remoteAction)
	{
		//if (!remoteAction && remote.is_enable())
		//{
		//// packet structure : <unit recno> <nation recno>
		//short* shortPtr = (short*)remote.new_send_queue_msg(MSG_UNIT_RESIGN, 2 * sizeof(short));
		//*shortPtr = sprite_recno;
		//shortPtr[1] = NationArray.player_recno;

		//return;
		//}

		//--- if the unit is visible, call stop2() so if it has an AI action queue, that will be reset ---//

		if (is_visible())
			stop2();

		//--- if the spy is resigned by an enemy, display a message ---//

		// the spy is cloaked in an enemy nation when it is resigned
		if (spy_recno != 0 && true_nation_recno() != nation_recno)
		{
			//------ decrease reputation ------//

			NationArray[true_nation_recno()].change_reputation(-GameConstants.SPY_KILLED_REPUTATION_DECREASE);

			//------- add news message -------//

			// display when the player's spy is revealed or the player has revealed an enemy spy
			if (true_nation_recno() == NationArray.player_recno || nation_recno == NationArray.player_recno)
			{
				//--- If a spy is caught, the spy's nation's reputation wil decrease ---//

				NewsArray.spy_killed(spy_recno);
			}
		}
		else
		{
			if (nation_recno != 0)
				NationArray[nation_recno].change_reputation(-1.0);
		}

		//----------------------------------------------//

		// if this is a general, news_array.general_die() will be called, set news_add_flag to 0 to suppress the display of thew news
		if (rank_id == RANK_GENERAL)
			NewsArray.news_add_flag = false;

		UnitArray.DeleteUnit(this);

		NewsArray.news_add_flag = true;
	}

	public void reward(int rewardNationRecno)
	{
		if (NationArray[rewardNationRecno].cash < GameConstants.REWARD_COST)
			return;

		//--------- if this is a spy ---------//

		if (spy_recno != 0 && true_nation_recno() == rewardNationRecno) // if the spy's owning nation rewards the spy
		{
			SpyArray[spy_recno].change_loyalty(GameConstants.REWARD_LOYALTY_INCREASE);
		}

		//--- if this spy's nation_recno & true_nation_recno() are both == rewardNationRecno,
		//it's true loyalty and cloaked loyalty will both be increased ---//

		if (nation_recno == rewardNationRecno)
		{
			total_reward += GameConstants.REWARD_COST;

			change_loyalty(GameConstants.REWARD_LOYALTY_INCREASE);
		}

		NationArray[rewardNationRecno].add_expense(NationBase.EXPENSE_REWARD_UNIT, GameConstants.REWARD_COST);
	}

	public void spy_change_nation(int newNationRecno, int remoteAction, int groupDefect = 0)
	{
		if (newNationRecno == nation_recno)
			return;

		if (newNationRecno != 0 && NationArray.IsDeleted(newNationRecno)) // this can happen in a multiplayer message
			return;

		//------- if this is a remote action -------//

		//if (!remoteAction && remote.is_enable())
		//{
		//// packet structure <unit recno> <new nation Recno> <group defect>
		//short* shortPtr = (short*)remote.new_send_queue_msg(MSG_UNIT_SPY_NATION, 3 * sizeof(short));
		//*shortPtr = sprite_recno;
		//shortPtr[1] = newNationRecno;
		//shortPtr[2] = groupDefect;
		//return;
		//}

		//----- update the var in Spy ------//

		Spy spy = SpyArray[spy_recno];

		//--- when a spy change cloak to another nation, he can't cloak as a general, he must become a soldier first ---//

		// if the spy is a commander in a camp, don't set its rank to soldier
		if (is_visible() && rank_id == RANK_GENERAL && newNationRecno != spy.true_nation_recno)
		{
			set_rank(RANK_SOLDIER);
		}

		//---------------------------------------------------//
		//
		// If this spy unit is a general or an overseer of the
		// cloaked nation, when he changes nation, that will
		// inevitably be noticed by the cloaked nation.
		//
		//---------------------------------------------------//

		// only send news message if he is not the player's own spy
		if (spy.true_nation_recno != NationArray.player_recno)
		{
			if (rank_id == RANK_GENERAL || unit_mode == UnitConstants.UNIT_MODE_OVERSEE ||
			    (spy.notify_cloaked_nation_flag != 0 && groupDefect == 0))
			{
				//-- if this spy's cloaked nation is the player's nation, the player will be notified --//

				if (nation_recno == NationArray.player_recno)
					NewsArray.unit_betray(sprite_recno, newNationRecno);
			}

			//---- send news to the cloaked nation if notify flag is on ---//

			if (spy.notify_cloaked_nation_flag != 0 && groupDefect == 0)
			{
				if (newNationRecno == NationArray.player_recno) // cloaked as the player's nation
					NewsArray.unit_betray(sprite_recno, newNationRecno);
			}
		}

		//--------- change nation recno now --------//

		spy.cloaked_nation_recno = newNationRecno;

		// call the betray function to change nation. There is no difference between a spy changing nation and a unit truly betrays
		if (groupDefect == 0)
			betray(newNationRecno);
	}

	public bool can_spy_change_nation()
	{
		if (spy_recno == 0)
			return false;

		//--------------------------------------------//

		int xLoc1 = cur_x_loc() - GameConstants.SPY_ENEMY_RANGE, yLoc1 = cur_y_loc() - GameConstants.SPY_ENEMY_RANGE;
		int xLoc2 = cur_x_loc() + GameConstants.SPY_ENEMY_RANGE, yLoc2 = cur_y_loc() + GameConstants.SPY_ENEMY_RANGE;

		xLoc1 = Math.Max(0, xLoc1);
		yLoc1 = Math.Max(0, yLoc1);
		xLoc2 = Math.Min(GameConstants.MapSize - 1, xLoc2);
		yLoc2 = Math.Min(GameConstants.MapSize - 1, yLoc2);

		int trueNationRecno = true_nation_recno();

		for (int yLoc = yLoc1; yLoc <= yLoc2; yLoc++)
		{
			for (int xLoc = xLoc1; xLoc <= xLoc2; xLoc++)
			{
				Location loc = World.get_loc(xLoc, yLoc);

				int unitRecno;

				if (loc.has_unit(UnitConstants.UNIT_LAND))
					unitRecno = loc.unit_recno(UnitConstants.UNIT_LAND);

				else if (loc.has_unit(UnitConstants.UNIT_SEA))
					unitRecno = loc.unit_recno(UnitConstants.UNIT_SEA);

				else if (loc.has_unit(UnitConstants.UNIT_AIR))
					unitRecno = loc.unit_recno(UnitConstants.UNIT_AIR);

				else
					continue;

				if (UnitArray.IsDeleted(unitRecno)) // the unit is dying, its recno is still in the location
					continue;

				Unit otherUnit = UnitArray[unitRecno];
				if (otherUnit.true_nation_recno() != trueNationRecno)
				{
					if (otherUnit.spy_recno != 0 && spy_recno != 0)
					{
						if (SpyArray[otherUnit.spy_recno].spy_skill >= SpyArray[spy_recno].spy_skill)
						{
							continue;
						}
					}

					return false;
				}
			}
		}

		return true;
	}

	public void change_hit_points(double changePoints)
	{
		hit_points += changePoints;

		if (hit_points < 0.0)
			hit_points = 0.0;

		if (hit_points > max_hit_points)
			hit_points = max_hit_points;
	}

	public void change_loyalty(int loyaltyChange)
	{
		int newLoyalty = loyalty + loyaltyChange;

		newLoyalty = Math.Max(0, newLoyalty);

		loyalty = Math.Min(100, newLoyalty);
	}

	public bool think_betray()
	{
		int unitRecno = sprite_recno;

		if (spy_recno != 0) // spies do not betray here, spy has its own functions for betrayal
			return false;

		//----- if the unit is in training or is constructing a building, do not rebel -------//

		if (!is_visible() && unit_mode != UnitConstants.UNIT_MODE_OVERSEE)
			return false;

		if (loyalty >= GameConstants.UNIT_BETRAY_LOYALTY) // you when unit is
			return false;

		if (UnitRes[unit_id].race_id == 0 || nation_recno == 0 || rank_id == RANK_KING || spy_recno != 0)
			return false;

		//------ turn towards other nation --------//

		int bestNationRecno = 0, bestScore = loyalty; // the score must be larger than the current loyalty
		int unitRegionId = region_id();

		if (loyalty == 0) // if the loyalty is 0, it will definitely betray
			bestScore = -100;

		Nation curNation = NationArray[nation_recno];

		foreach (Nation nation in NationArray)
		{
			if (nation == curNation || !curNation.get_relation(nation.nation_recno).has_contact)
				continue;

			//--- only if the nation has a base town in the region where the unit stands ---//

			if (!RegionArray.nation_has_base_town(unitRegionId, nation.nation_recno))
				continue;

			//------------------------------------------------//

			int nationScore = (int)nation.reputation + (nation.overall_rating - curNation.overall_rating);

			if (RaceRes.is_same_race(nation.race_id, race_id))
				nationScore += 30;

			if (nationScore > bestScore)
			{
				bestScore = nationScore;
				bestNationRecno = nation.nation_recno;
			}
		}

		if (bestNationRecno != 0)
		{
			return betray(bestNationRecno);
		}
		else if (loyalty == 0)
		{
			//----------------------------------------------//
			// If there is no good nation to turn towards to and
			// the loyalty has dropped to 0, resign itself and
			// leave the nation.
			//
			// However, if the unit is spy, it will stay with the
			// nation as it has never been really loyal to the nation.
			//---------------------------------------------//

			if (rank_id != RANK_KING && is_visible() && spy_recno == 0)
			{
				resign(InternalConstants.COMMAND_AUTO);
				return true;
			}
		}

		return false;
	}

	public bool betray(int newNationRecno)
	{
		int unitRecno = sprite_recno;

		if (nation_recno == newNationRecno)
			return false;

		// don't change nation when the unit is constructing a firm
		// don't change nation when the unit is constructing a firm
		if (unit_mode == UnitConstants.UNIT_MODE_CONSTRUCT || unit_mode == UnitConstants.UNIT_MODE_ON_SHIP)
			return false;

		//---------- add news -----------//

		if (nation_recno == NationArray.player_recno || newNationRecno == NationArray.player_recno)
		{
			//--- if this is a spy, don't display news message for betrayal as it is already displayed in Unit::spy_change_nation() ---//

			if (spy_recno == 0)
				NewsArray.unit_betray(sprite_recno, newNationRecno);
		}

		//------ change nation now ------//

		change_nation(newNationRecno);

		//-------- set the loyalty of the unit -------//

		if (nation_recno != 0)
		{
			Nation nation = NationArray[nation_recno];

			loyalty = GameConstants.UNIT_BETRAY_LOYALTY + Misc.Random(5);

			if (nation.reputation > 0)
				change_loyalty((int)nation.reputation);

			if (RaceRes.is_same_race(nation.race_id, race_id))
				change_loyalty(30);

			update_loyalty(); // update target loyalty
		}
		else //------ if change to independent rebel -------//
		{
			loyalty = 0; // no loyalty needed
		}

		//--- if this unit is a general, change nation for the units he commands ---//

		int newTeamId = UnitArray.cur_team_id++;

		if (rank_id == RANK_GENERAL)
		{
			int nationReputation = (int)NationArray[nation_recno].reputation;

			foreach (Unit unit in UnitArray)
			{
				//---- capture the troop this general commands -----//

				if (unit.leader_unit_recno == sprite_recno && unit.rank_id == RANK_SOLDIER && unit.is_visible())
				{
					if (unit.spy_recno != 0) // if the unit is a spy
					{
						// 1-group defection of this unit, allowing us to hande the change of nation
						unit.spy_change_nation(newNationRecno, InternalConstants.COMMAND_AUTO, 1);
					}

					unit.change_nation(newNationRecno);

					unit.team_id = newTeamId; // assign new team_id or checking for nation_recno
				}
			}
		}

		team_id = newTeamId;

		//------ go to meet the new master -------//

		if (is_visible() && nation_recno != 0)
		{
			if (spy_recno == 0 || SpyArray[spy_recno].notify_cloaked_nation_flag != 0)
			{
				// generals shouldn't automatically be assigned to camps, they should just move near your villages
				if (rank_id == RANK_GENERAL)
					ai_move_to_nearby_town();
				else
					think_normal_human_action(); // this is an AI function in OUNITAI.CPP
			}
		}

		return true;
	}

	public bool can_stand_guard()
	{
		return (can_guard_flag & 1) != 0;
	}

	public bool can_move_guard()
	{
		return (can_guard_flag & 2) != 0;
	}

	public bool can_attack_guard()
	{
		return (can_guard_flag & 4) != 0;
	}

	public int firm_can_assign(int firmRecno)
	{
		Firm firm = FirmArray[firmRecno];
		FirmInfo firmInfo = FirmRes[firm.firm_id];

		switch (UnitRes[unit_id].unit_class)
		{
			case UnitConstants.UNIT_CLASS_HUMAN:
				if (nation_recno == firm.nation_recno)
				{
					if (skill.skill_id == Skill.SKILL_CONSTRUCTION && firm.firm_id != Firm.FIRM_MONSTER)
					{
						return 3;
					}

					// ###### begin Gilbert 22/10 #######//
					//----------------------------------------//
					// If this is a spy, then he can only be
					// assigned to an enemy firm when there is
					// space for the unit.
					//----------------------------------------//

					//if( spy_recno && true_nation_recno() != firm.nation_recno )
					//{
					//	if( rank_id == RANK_GENERAL )
					//	{
					//		if( firm.overseer_recno )
					//			return 0;
					//	}
					//	else
					//	{
					//		if( firm.worker_count == MAX_WORKER )
					//			return 0;
					//	}
					//}
					//--------------------------------------//
					// ###### end Gilbert 22/10 #######//

					switch (firm.firm_id)
					{
						case Firm.FIRM_CAMP:
							return rank_id == RANK_SOLDIER ? 1 : 2;

						case Firm.FIRM_BASE:
							if (race_id == firm.race_id)
							{
								if (skill.skill_id == 0 || skill.skill_id == Skill.SKILL_PRAYING) // non-skilled worker
									return 1;
								if (rank_id != RANK_SOLDIER)
									return 2;
							}

							break;

						//case FIRM_INN:
						// shealthed soldier spy can assign to inn
						//	return rank_id == RANK_SOLDIER && nation_recno != true_nation_recno() ? 4 : 0;

						default:
							return rank_id == RANK_SOLDIER && firmInfo.need_unit() ? 1 : 0;
					}
				}

				break;

			case UnitConstants.UNIT_CLASS_WEAPON:
				if (firm.firm_id == Firm.FIRM_CAMP && nation_recno == firm.nation_recno)
					return 1;
				break;

			case UnitConstants.UNIT_CLASS_SHIP:
				if (firm.firm_id == Firm.FIRM_HARBOR && nation_recno == firm.nation_recno)
					return 1;
				break;

			case UnitConstants.UNIT_CLASS_MONSTER:
				if (firm.firm_id == Firm.FIRM_MONSTER && mobile_type == UnitConstants.UNIT_LAND)
				{
					// BUGHERE : suppose only land monster can assign
					return rank_id == RANK_SOLDIER ? 1 : 2;
				}

				break;

			case UnitConstants.UNIT_CLASS_GOD:
			case UnitConstants.UNIT_CLASS_CARAVAN:
				break;
		}

		return 0;
	}

	public void set_idle()
	{
		final_dir = cur_dir;
		turn_delay = 0;
		cur_action = SPRITE_IDLE;
	}

	public void set_ready()
	{
		final_dir = cur_dir;
		turn_delay = 0;
		cur_action = SPRITE_READY_TO_MOVE;
	}

	public void set_move()
	{
		cur_action = SPRITE_MOVE;
	}

	public void set_wait()
	{
		cur_action = SPRITE_WAIT;
		cur_frame = 1;
		waiting_term++;
	}

	public void set_attack()
	{
		final_dir = cur_dir;
		turn_delay = 0;
		cur_action = SPRITE_ATTACK;
	}

	public void set_turn()
	{
		cur_action = SPRITE_TURN;
	}

	public void set_ship_extra_move()
	{
		cur_action = SPRITE_SHIP_EXTRA_MOVE;
	}

	public void set_die()
	{
		if (action_mode == UnitConstants.ACTION_DIE)
			return;

		action_mode = UnitConstants.ACTION_DIE;
		cur_action = SPRITE_DIE;
		cur_frame = 1;

		//---- if this unit is led by a leader, only mobile units has leader_unit_recno assigned to a leader -----//

		// the leader unit may have been killed at the same time
		if (leader_unit_recno != 0 && !UnitArray.IsDeleted(leader_unit_recno))
		{
			UnitArray[leader_unit_recno].del_team_member(sprite_recno);
			leader_unit_recno = 0;
		}
	}

	public virtual void fix_attack_info() // set attack_info_array appropriately
	{
		UnitInfo unitInfo = UnitRes[unit_id];

		attack_count = unitInfo.attack_count;

		if (attack_count > 0 && unitInfo.first_attack > 0)
		{
			attack_info_array = new AttackInfo[attack_count];
			for (int i = 0; i < attack_count; i++)
			{
				attack_info_array[i] = UnitRes.attack_info_array[unitInfo.first_attack - 1 + i];
			}
		}
		else
		{
			attack_info_array = null;
		}

		int old_attack_count = attack_count;
		int techLevel;
		if (unitInfo.unit_class == UnitConstants.UNIT_CLASS_WEAPON && (techLevel = get_weapon_version()) > 0)
		{
			switch (unit_id)
			{
				case UnitConstants.UNIT_BALLISTA:
				case UnitConstants.UNIT_F_BALLISTA:
					attack_count = 2;
					break;
				case UnitConstants.UNIT_EXPLOSIVE_CART:
					attack_count = 0;
					break;
				default:
					attack_count = 1;
					break;
			}

			if (attack_count > 0)
			{
				//TODO check this
				attack_info_array = new AttackInfo[attack_count];
				for (int i = 0; i < attack_count; i++)
				{
					attack_info_array[i] = UnitRes.attack_info_array[old_attack_count + (techLevel - 1) * attack_count + i];
				}
			}
			else
			{
				// no attack like explosive cart
				attack_info_array = null;
			}
		}
	}

	//------------------ idle functions -------------------//
	private bool reactivate_idle_action()
	{
		if (action_mode2 == UnitConstants.ACTION_STOP)
			return false; // return for no idle action

		if (!is_dir_correct())
			return true; // cheating for turning the direction

		Location loc;
		int curXLoc = move_to_x_loc;
		int curYLoc = move_to_y_loc;

		bool hasSearch = false;
		bool returnFlag = false;

		SeekPath.set_status(SeekPath.PATH_WAIT);
		switch (action_mode2)
		{
			case UnitConstants.ACTION_STOP:
			case UnitConstants.ACTION_DIE:
				return false; // do nothing

			case UnitConstants.ACTION_ATTACK_UNIT:
				if (action_para2 == 0 || UnitArray.IsDeleted(action_para2))
					stop2();
				else
				{
					Unit unit = UnitArray[action_para2];
					SpriteInfo spriteInfo = unit.sprite_info;

					if (space_for_attack(action_x_loc2, action_y_loc2, unit.mobile_type, spriteInfo.loc_width, spriteInfo.loc_height))
					{
						//------ there should be place for this unit to attack the target, attempts to attack it ------//
						attack_unit(action_para2, 0, 0, false); // last 0 for not reset blocked_edge
						hasSearch = true;
						returnFlag = true;
					}
				}

				break;

			case UnitConstants.ACTION_ATTACK_FIRM:
				loc = World.get_loc(action_x_loc2, action_y_loc2);
				if (action_para2 == 0 || !loc.is_firm())
					stop2(); // stop since target is already destroyed
				else
				{
					Firm firm = FirmArray[action_para2];
					FirmInfo firmInfo = FirmRes[firm.firm_id];

					if (space_for_attack(action_x_loc2, action_y_loc2, UnitConstants.UNIT_LAND, firmInfo.loc_width, firmInfo.loc_height))
					{
						//-------- attack target since space is found for this unit to move to ---------//
						attack_firm(action_x_loc2, action_y_loc2, 0, 0, 0);
						hasSearch = true;
						returnFlag = true;
					}
				}

				break;

			case UnitConstants.ACTION_ATTACK_TOWN:
				loc = World.get_loc(action_x_loc2, action_y_loc2);
				if (action_para2 == 0 || !loc.is_town())
					stop2(); // stop since target is deleted
				else
				{
					if (space_for_attack(action_x_loc2, action_y_loc2, UnitConstants.UNIT_LAND,
						    InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT))
					{
						//---------- attack target --------//
						attack_town(action_x_loc2, action_y_loc2, 0, 0, 0);
						hasSearch = true;
						returnFlag = true;
					}
				}

				break;

			case UnitConstants.ACTION_ATTACK_WALL:
				loc = World.get_loc(action_x_loc2, action_y_loc2);
				if (!loc.is_wall())
					stop2(); // stop since target doesn't exist
				else
				{
					if (space_for_attack(action_x_loc2, action_y_loc2, UnitConstants.UNIT_LAND, 1, 1))
					{
						//----------- attack target -----------//
						attack_wall(action_x_loc2, action_y_loc2, 0, 0, 0);
						hasSearch = true;
						returnFlag = true;
					}
				}

				break;

			case UnitConstants.ACTION_ASSIGN_TO_FIRM:
			case UnitConstants.ACTION_ASSIGN_TO_TOWN:
			case UnitConstants.ACTION_ASSIGN_TO_VEHICLE:
				//---------- resume assign actions -------------//
				assign(action_x_loc2, action_y_loc2);
				hasSearch = true;
				waiting_term = 0;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_ASSIGN_TO_SHIP:
				//------------ try to assign to marine ------------//
				assign_to_ship(action_x_loc2, action_y_loc2, action_para2);
				hasSearch = true;
				waiting_term = 0;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_BUILD_FIRM:
				//-------------- build again ----------------//
				build_firm(action_x_loc2, action_y_loc2, action_para2, InternalConstants.COMMAND_AUTO);
				hasSearch = true;
				waiting_term = 0;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_SETTLE:
				//------------- try again to settle -----------//
				settle(action_x_loc2, action_y_loc2);
				hasSearch = true;
				waiting_term = 0;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_BURN:
				//---------------- resume burn action -----------------//
				burn(action_x_loc2, action_y_loc2, InternalConstants.COMMAND_AUTO);
				hasSearch = true;
				waiting_term = 0;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_MOVE:
				if (move_to_x_loc != action_x_loc2 || move_to_y_loc != action_y_loc2)
				{
					//------- move since the unit has not reached its destination --------//
					move_to(action_x_loc2, action_y_loc2, 1);
					hasSearch = true;
					returnFlag = true;
					break;
				}

				waiting_term = 0;
				break;

			case UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET:
				//---------- resume action -----------//
				process_auto_defense_attack_target();
				hasSearch = true;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET:
				process_auto_defense_detect_target();
				//TODO hasSearch = true;?
				returnFlag = true;
				break;

			case UnitConstants.ACTION_AUTO_DEFENSE_BACK_CAMP:
				process_auto_defense_back_camp();
				hasSearch = true;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET:
				process_defend_town_attack_target();
				hasSearch = true;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET:
				process_defend_town_detect_target();
				returnFlag = true;
				break;

			case UnitConstants.ACTION_DEFEND_TOWN_BACK_TOWN:
				process_defend_town_back_town();
				hasSearch = true;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET:
				process_monster_defend_attack_target();
				hasSearch = true;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_MONSTER_DEFEND_DETECT_TARGET:
				process_monster_defend_detect_target();
				returnFlag = true;
				break;

			case UnitConstants.ACTION_MONSTER_DEFEND_BACK_FIRM:
				process_monster_defend_back_firm();
				hasSearch = true;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_SHIP_TO_BEACH:
				UnitMarine ship = (UnitMarine)this;
				if (!ship.in_beach || ship.extra_move_in_beach == UnitMarine.EXTRA_MOVE_FINISH)
				{
					//----------- the ship has not reached inlet, so move again --------------//
					ship_to_beach(action_x_loc2, action_y_loc2, out _, out _);
					hasSearch = true;
				}

				returnFlag = true;
				break;

			case UnitConstants.ACTION_GO_CAST_POWER:
				go_cast_power(action_x_loc2, action_y_loc2, ((UnitGod)this).cast_power_type, InternalConstants.COMMAND_AUTO);
				returnFlag = true;
				break;
		}

		if (hasSearch && SeekPath.path_status == SeekPath.PATH_IMPOSSIBLE && next_x_loc() == move_to_x_loc && next_y_loc() == move_to_y_loc)
		{
			//TODO check
			
			//-------------------------------------------------------------------------//
			// abort actions since the unit tries to move and move no more.
			//-------------------------------------------------------------------------//
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return true;
		}

		bool abort = false;
		if (returnFlag)
		{
			if (curXLoc == move_to_x_loc && curYLoc == move_to_y_loc && SeekPath.path_status == SeekPath.PATH_IMPOSSIBLE)
			{
				//TODO check

				if (action_mode2 == UnitConstants.ACTION_ASSIGN_TO_SHIP || action_mode2 == UnitConstants.ACTION_SHIP_TO_BEACH ||
				    in_any_defense_mode())
					return true;

				//------- number of nodes is not enough to find the destination -------//
				if (action_misc != UnitConstants.ACTION_MISC_STOP)
				{
					abort = true; // assume destination unreachable, abort action
				}
			}
			else // action resumed, return true
			{
				return true;
			}
		}

		if (!returnFlag || abort)
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
		}

		return false;
	}

	// detect target to attack
	private bool idle_detect_attack(int startLoc = 0, int dimensionInput = 0, int defenseMode = 0)
	{
		//---------------------------------------------------//
		// Set detectDelay.
		//
		// The larger its value, the less CPU time it will takes,
		// but it will also take longer to detect enemies.
		//---------------------------------------------------//

		int detectDelay = 1;

		Location loc;
		Unit unit;
		int targetMobileType;
		int countLimit;
		int targetRecno;
		bool idle_detect_default_mode = (startLoc == 0 && dimensionInput == 0 && defenseMode == 0); //----- true when all zero
		idle_detect_has_unit = idle_detect_has_firm = idle_detect_has_town = idle_detect_has_wall = false;
		help_mode = UnitConstants.HELP_NOTHING;

		//-----------------------------------------------------------------------------------------------//
		// adjust waiting_term for default_mode
		//-----------------------------------------------------------------------------------------------//
		++waiting_term;
		waiting_term = Math.Max(waiting_term, 0); //**BUGHERE
		int lowestBit = waiting_term % detectDelay;

		if (action_mode2 == UnitConstants.ACTION_STOP)
		{
			waiting_term = lowestBit;
		}

		int dimension = (dimensionInput != 0 ? dimensionInput : UnitConstants.ATTACK_DETECT_DISTANCE) << 1;
		dimension++;
		countLimit = dimension * dimension;
		int i = startLoc != 0 ? startLoc : 1 + lowestBit;
		int incAmount = (idle_detect_default_mode) ? detectDelay : 1;

		//-----------------------------------------------------------------------------------------------//
		// check the location around the unit
		//
		// The priority to choose target is (value of targetType)
		// 1) unit, 2) firm, 3) wall
		//-----------------------------------------------------------------------------------------------//
		for (; i <= countLimit; i += incAmount) // 1 is the self location
		{
			int xOffset, yOffset;
			Misc.cal_move_around_a_point(i, dimension, dimension, out xOffset, out yOffset);
			int checkXLoc = move_to_x_loc + xOffset;
			int checkYLoc = move_to_y_loc + yOffset;
			if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
				continue;

			//------------------ verify location ---------------//
			loc = World.get_loc(checkXLoc, checkYLoc);
			if (defenseMode != 0 && action_mode2 != UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET)
			{
				if (action_mode2 == UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET)
					if (loc.power_nation_recno != nation_recno && loc.power_nation_recno != 0)
						continue; // skip this location because it is not neutral nation or our nation
			}

			//----------------------------------------------------------------------------//
			// checking the target type
			//----------------------------------------------------------------------------//
			if ((targetMobileType = loc.has_any_unit(i == 1 ? mobile_type : UnitConstants.UNIT_LAND)) != 0 &&
			    (targetRecno = loc.unit_recno(targetMobileType)) != 0 && !UnitArray.IsDeleted(targetRecno))
			{
				//=================== is unit ======================//
				if (idle_detect_has_unit || (action_para == targetRecno &&
				                             action_mode == UnitConstants.ACTION_ATTACK_UNIT &&
				                             checkXLoc == action_x_loc && checkYLoc == action_y_loc))
					continue; // same target as before

				unit = UnitArray[targetRecno];
				if (nation_recno != 0 && unit.nation_recno == nation_recno && help_mode != UnitConstants.HELP_ATTACK_UNIT)
					idle_detect_helper_attack(targetRecno); // help our troop
				else if ((help_mode == UnitConstants.HELP_ATTACK_UNIT && help_attack_target_recno == targetRecno) ||
				         (unit.nation_recno != nation_recno && idle_detect_unit_checking(targetRecno)))
				{
					idle_detect_target_unit_recno = targetRecno;
					idle_detect_has_unit = true;
					break; // break with highest priority
				}
			}
			else if (loc.is_firm() && (targetRecno = loc.firm_recno()) != 0)
			{
				//=============== is firm ===============//
				if (idle_detect_has_firm || (action_para == targetRecno &&
				                             action_mode == UnitConstants.ACTION_ATTACK_FIRM &&
				                             action_x_loc == checkXLoc && action_y_loc == checkYLoc))
					continue; // same target as before

				if (idle_detect_firm_checking(targetRecno) != 0)
				{
					idle_detect_target_firm_recno = targetRecno;
					idle_detect_has_firm = true;
				}
			}
			/*else if(loc.is_town() && (targetRecno = loc.town_recno()))
			{
			   //=============== is town ===========//
			   if(idle_detect_has_town || (action_para==targetRecno && action_mode==ACTION_ATTACK_TOWN &&
			      action_x_loc==checkXLoc && action_y_loc==checkYLoc))
			      continue; // same target as before
	  
			   if(idle_detect_town_checking(targetRecno))
			   {
					  idle_detect_target_town_recno = targetRecno;
					  idle_detect_has_town++;
			   }
			}
			else if(loc.is_wall())
			{
			   //================ is wall ==============//
			   if(idle_detect_has_wall || (action_mode==ACTION_ATTACK_WALL && action_para==targetRecno &&
			      action_x_loc==checkXLoc && action_y_loc==checkYLoc))
			      continue; // same target as before
	  
			   if(idle_detect_wall_checking(checkXLoc, checkYLoc))
			   {
					  idle_detect_target_wall_x1 = checkXLoc;
					  idle_detect_target_wall_y1 = checkYLoc;
					  idle_detect_has_wall++;
			   }
			}*/

			//if(hasUnit && hasFirm && hasTown && hasWall)
			//if(hasUnit && hasFirm && hasWall)
			//  break; // there is target for attacking
		}

		return idle_detect_choose_target(defenseMode);
	}

	private bool idle_detect_choose_target(int defenseMode)
	{
		//-----------------------------------------------------------------------------------------------//
		// Decision making for choosing target to attack
		//-----------------------------------------------------------------------------------------------//
		if (defenseMode != 0)
		{
			if (action_mode2 == UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET)
			{
				//----------- defense units allow to attack units and firms -----------//

				if (idle_detect_has_unit)
					defense_attack_unit(idle_detect_target_unit_recno);
				else if (idle_detect_has_firm)
				{
					Firm targetFirm = FirmArray[idle_detect_target_firm_recno];
					defense_attack_firm(targetFirm.loc_x1, targetFirm.loc_y1);
				}
				/*else if(idle_detect_has_town)
				{
					Town *targetTown = TownArray[idle_detect_target_town_recno];
					defense_attack_town(targetTown.loc_x1, targetTown.loc_y1);
				}
				else if(idle_detect_has_wall)
				   defense_attack_wall(idle_detect_target_wall_x1, idle_detect_target_wall_y1);*/
				else
					return false;

				return true;
			}
			else if (action_mode2 == UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET)
			{
				//----------- town units only attack units ------------//

				if (idle_detect_has_unit)
					defend_town_attack_unit(idle_detect_target_unit_recno);
				else
					return false;

				return true;
			}
			else if (action_mode2 == UnitConstants.ACTION_MONSTER_DEFEND_DETECT_TARGET)
			{
				//---------- monsters can attack units and firms -----------//

				if (idle_detect_has_unit)
					monster_defend_attack_unit(idle_detect_target_unit_recno);
				else if (idle_detect_has_firm)
				{
					Firm targetFirm = FirmArray[idle_detect_target_firm_recno];
					monster_defend_attack_firm(targetFirm.loc_x1, targetFirm.loc_y1);
				}
				/*else if(idle_detect_has_town)
				{
				   Town *targetTown = TownArray[idle_detect_target_town_recno];
				   monster_defend_attack_town(targetTown.loc_x1, targetTown.loc_y1);
				}
				else if(idle_detect_has_wall)
				   monster_defend_attack_wall(idle_detect_target_wall_x1, idle_detect_target_wall_y1);*/
				else
					return false;

				return true;
			}
		}
		else // default mode
		{
			int rc = 0;

			if (idle_detect_has_unit)
			{
				attack_unit(idle_detect_target_unit_recno, 0, 0, true);

				//--- set the original position of the target, so the unit won't chase too far away ---//

				Unit unit = UnitArray[idle_detect_target_unit_recno];

				original_target_x_loc = unit.next_x_loc();
				original_target_y_loc = unit.next_y_loc();

				rc = 1;
			}

			else if (help_mode == UnitConstants.HELP_ATTACK_UNIT)
			{
				attack_unit(help_attack_target_recno, 0, 0, true);

				//--- set the original position of the target, so the unit won't chase too far away ---//

				Unit unit = UnitArray[help_attack_target_recno];

				original_target_x_loc = unit.next_x_loc();
				original_target_y_loc = unit.next_y_loc();

				rc = 1;
			}
			else if (idle_detect_has_firm)
			{
				Firm targetFirm = FirmArray[idle_detect_target_firm_recno];
				attack_firm(targetFirm.loc_x1, targetFirm.loc_y1);
			}
			/*else if(idle_detect_has_town)
			{
				Town *targetTown = TownArray[idle_detect_target_town_recno];
				attack_town(targetTown.loc_x1, targetTown.loc_y1);
			}
			else if(idle_detect_has_wall)
				attack_wall(idle_detect_target_wall_x1, idle_detect_target_wall_y1);*/
			else
				return false;

			//---- set original action vars ----//

			if (rc != 0 && original_action_mode == 0)
			{
				original_action_mode = UnitConstants.ACTION_MOVE;
				original_action_para = 0;
				original_action_x_loc = next_x_loc();
				original_action_y_loc = next_y_loc();
			}

			return true;
		}

		return false;
	}

	private void idle_detect_helper_attack(int unitRecno)
	{
		const int HELP_DISTANCE = 15;

		Unit unit = UnitArray[unitRecno];
		if (unit.unit_id == UnitConstants.UNIT_CARAVAN)
			return;

		//char	actionMode;
		int actionPara = 0;
		//short actionXLoc, actionYLoc;
		int isUnit = 0;

		//------------- is the unit attacking other unit ------------//
		switch (unit.action_mode2)
		{
			case UnitConstants.ACTION_ATTACK_UNIT:
				actionPara = unit.action_para2;
				isUnit++;
				break;

			default:
				switch (unit.action_mode)
				{
					case UnitConstants.ACTION_ATTACK_UNIT:
						actionPara = unit.action_para;
						isUnit++;
						break;
				}

				break;
		}

		if (isUnit != 0 && !UnitArray.IsDeleted(actionPara))
		{
			Unit targetUnit = UnitArray[actionPara];

			if (targetUnit.nation_recno == nation_recno)
				return;

			// the targetUnit this unit is attacking may have entered a
			// building by now due to processing order -- skip this one
			if (!targetUnit.is_visible())
				return;

			if (Misc.points_distance(next_x_loc(), next_y_loc(),
				    targetUnit.next_x_loc(), targetUnit.next_y_loc()) < HELP_DISTANCE)
			{
				if (idle_detect_unit_checking(actionPara))
				{
					help_attack_target_recno = actionPara;
					help_mode = UnitConstants.HELP_ATTACK_UNIT;
				}

				for (int i = 0; i < blocked_edge.Length; i++)
					blocked_edge[i] = 0;
			}
		}
	}

	private bool idle_detect_unit_checking(int targetRecno)
	{
		Unit targetUnit = UnitArray[targetRecno];

		if (targetUnit.unit_id == UnitConstants.UNIT_CARAVAN)
			return false;

		//-------------------------------------------//
		// If the target is moving, don't attack it.
		// Only attack when the unit stands still or
		// is attacking.
		//-------------------------------------------//

		if (targetUnit.cur_action != SPRITE_ATTACK && targetUnit.cur_action != SPRITE_IDLE)
			return false;

		//-------------------------------------------//
		// If the target is a spy of our own and the
		// notification flag is set to 0, then don't
		// attack.
		//-------------------------------------------//

		if (targetUnit.spy_recno != 0) // if the target unit is our spy, don't attack 
		{
			Spy spy = SpyArray[targetUnit.spy_recno];

			if (spy.true_nation_recno == nation_recno && spy.notify_cloaked_nation_flag == 0)
				return false;
		}

		if (spy_recno != 0) // if this unit is our spy, don't attack own units
		{
			Spy spy = SpyArray[spy_recno];

			if (spy.true_nation_recno == targetUnit.nation_recno && spy.notify_cloaked_nation_flag == 0)
				return false;
		}

		SpriteInfo spriteInfo = targetUnit.sprite_info;
		Nation nation = nation_recno != 0 ? NationArray[nation_recno] : null;
		int targetNationRecno = targetUnit.nation_recno;

		//-------------------------------------------------------------------//
		// checking nation relationship
		//-------------------------------------------------------------------//

		if (nation_recno != 0)
		{
			if (targetNationRecno != 0)
			{
				//------- don't attack own units and non-hostile units -------//

				//--------------------------------------------------------------//
				// if the unit is hostile, only attack if should_attack flag to
				// that nation is true or the unit is attacking somebody or something.
				//--------------------------------------------------------------//
				NationRelation nationRelation = nation.get_relation(targetNationRecno);

				if (nationRelation.status != NationBase.NATION_HOSTILE || !nationRelation.should_attack)
					return false;
			}
			else if (!targetUnit.independent_nation_can_attack(nation_recno))
				return false;
		}
		else if (!independent_nation_can_attack(targetNationRecno)) // independent unit
			return false;

		//---------------------------------------------//
		if (space_for_attack(targetUnit.next_x_loc(), targetUnit.next_y_loc(), targetUnit.mobile_type,
			    spriteInfo.loc_width, spriteInfo.loc_height))
			return true;
		else
			return false;
	}

	private int idle_detect_firm_checking(int targetRecno)
	{
		Firm firm = FirmArray[targetRecno];

		//------------ code to select firm for attacking -----------//
		switch (firm.firm_id)
		{
			case Firm.FIRM_CAMP:
			case Firm.FIRM_BASE:
			case Firm.FIRM_WAR_FACTORY:
				break;

			default:
				return 0;
		}

		Nation nation = nation_recno != 0 ? NationArray[nation_recno] : null;
		int targetNationRecno = firm.nation_recno;
		int targetMobileType = mobile_type == UnitConstants.UNIT_SEA ? UnitConstants.UNIT_SEA : UnitConstants.UNIT_LAND;

		//-------------------------------------------------------------------------------//
		// checking nation relationship
		//-------------------------------------------------------------------------------//
		if (nation_recno != 0)
		{
			if (targetNationRecno != 0)
			{
				//------- don't attack own units and non-hostile units -------//

				if (targetNationRecno == nation_recno)
					return 0;

				//--------------------------------------------------------------//
				// if the unit is hostile, only attack if should_attack flag to
				// that nation is true or the unit is attacking somebody or something.
				//--------------------------------------------------------------//

				NationRelation nationRelation = nation.get_relation(targetNationRecno);

				if (nationRelation.status != NationBase.NATION_HOSTILE || !nationRelation.should_attack)
					return 0;
			}
			else // independent firm
			{
				FirmMonster monsterFirm = (FirmMonster)FirmArray[targetRecno];

				if (!monsterFirm.is_hostile_nation(nation_recno))
					return 0;
			}
		}
		else if (!independent_nation_can_attack(targetNationRecno)) // independent town
			return 0;

		FirmInfo firmInfo = FirmRes[firm.firm_id];
		if (space_for_attack(firm.loc_x1, firm.loc_y1, UnitConstants.UNIT_LAND,
			    firmInfo.loc_width, firmInfo.loc_height))
			return 1;
		else
			return 0;
	}

	private int idle_detect_town_checking(int targetRecno)
	{
		Town town = TownArray[targetRecno];
		Nation nation = nation_recno != 0 ? NationArray[nation_recno] : null;
		int targetNationRecno = town.NationId;

		//-------------------------------------------------------------------------------//
		// checking nation relationship
		//-------------------------------------------------------------------------------//
		if (nation_recno != 0)
		{
			if (targetNationRecno != 0)
			{
				//------- don't attack own units and non-hostile units -------//

				if (targetNationRecno == nation_recno)
					return 0;

				//--------------------------------------------------------------//
				// if the unit is hostile, only attack if should_attack flag to
				// that nation is true or the unit is attacking somebody or something.
				//--------------------------------------------------------------//

				NationRelation nationRelation = nation.get_relation(targetNationRecno);

				if (nationRelation.status != NationBase.NATION_HOSTILE || !nationRelation.should_attack)
					return 0;
			}
			else if (!town.IsHostileNation(nation_recno))
				return 0; // false if the indepentent unit don't want to attack us
		}
		else if (!independent_nation_can_attack(targetNationRecno)) // independent town
			return 0;

		if (space_for_attack(town.LocX1, town.LocY1, UnitConstants.UNIT_LAND, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT))
			return 1;
		else
			return 0;
	}

	private int idle_detect_wall_checking(int targetXLoc, int targetYLoc)
	{
		Location loc = World.get_loc(targetXLoc, targetYLoc);
		Nation nation = nation_recno != 0 ? NationArray[nation_recno] : null;
		int targetNationRecno = loc.wall_nation_recno();

		//-------------------------------------------------------------------------------//
		// checking nation relationship
		//-------------------------------------------------------------------------------//
		if (nation_recno != 0)
		{
			if (targetNationRecno != 0)
			{
				//------- don't attack own units and non-hostile units -------//

				if (targetNationRecno == nation_recno)
					return 0;

				//--------------------------------------------------------------//
				// if the unit is hostile, only attack if should_attack flag to
				// that nation is true or the unit is attacking somebody or something.
				//--------------------------------------------------------------//

				NationRelation nationRelation = nation.get_relation(targetNationRecno);

				if (nationRelation.status != NationBase.NATION_HOSTILE || !nationRelation.should_attack)
					return 0;
			}
			else
				return 0;
		}
		else if (!independent_nation_can_attack(targetNationRecno)) // independent town
			return 0;

		if (space_for_attack(targetXLoc, targetYLoc, UnitConstants.UNIT_LAND, 1, 1))
			return 1;
		else
			return 0;
	}

	//------------ movement action -----------------//
	private int search(int destXLoc, int destYLoc, int preserveAction = 0, int searchMode = 1, int miscNo = 0, int numOfPath = 1)
	{
		if (destXLoc < 0 || destXLoc >= GameConstants.MapSize || destYLoc < 0 || destYLoc >= GameConstants.MapSize ||
		    hit_points <= 0.0 || action_mode == UnitConstants.ACTION_DIE || cur_action == SPRITE_DIE ||
		    searchMode <= 0 || searchMode > SeekPath.MAX_SEARCH_MODE_TYPE)
		{
			//TODO check, this code should be never executed
			stop2(UnitConstants.KEEP_DEFENSE_MODE); //-********** BUGHERE, err_handling for retailed version
			return 1;
		}

		int result = 0;
		if (UnitRes[unit_id].unit_class == UnitConstants.UNIT_CLASS_SHIP)
		{
			UnitMarine ship = (UnitMarine)this;
			switch (ship.extra_move_in_beach)
			{
				case UnitMarine.NO_EXTRA_MOVE:
					result = searching(destXLoc, destYLoc, preserveAction, searchMode, miscNo, numOfPath);
					break;

				case UnitMarine.EXTRA_MOVING_IN:
				case UnitMarine.EXTRA_MOVING_OUT:
					return 0;

				case UnitMarine.EXTRA_MOVE_FINISH:
					ship_leave_beach(next_x_loc(), next_y_loc());
					break;
			}
		}
		else
		{
			result = searching(destXLoc, destYLoc, preserveAction, searchMode, miscNo, numOfPath);
		}

		if (wayPoints.Count > 0 && result_node_array == null) // can move no more
			ResetWayPoints();

		// 0 means extra_move_in_beach != UnitMarine.NO_EXTRA_MOVE
		return result != 0 ? 1 : 0;
	}

	private static double avgTimes;
	private static System.Collections.Generic.List<double> times = new System.Collections.Generic.List<double>();
	
	private int searching(int destXLoc, int destYLoc, int preserveAction, int searchMode, int miscNo, int numOfPath)
	{
		stop(preserveAction); // stop the unit as soon as possible

		int startXLocLoc = next_x_loc(); // next location the sprite is moving towards
		int startYLocLoc = next_y_loc();

		//---------------------------------------------------------------------------//
		// adjust the destination for unit size
		//---------------------------------------------------------------------------//
		/*err_when(sprite_info.loc_width!=sprite_info.loc_height);
		if(sprite_info.loc_width>1) // not size 1x1
		{
			destXLoc = move_to_x_loc = MIN(destXLoc, MAX_WORLD_X_LOC-sprite_info.loc_width);
			destYLoc = move_to_y_loc = MIN(destYLoc, MAX_WORLD_Y_LOC-sprite_info.loc_height);
		}
		else
		{*/
		move_to_x_loc = destXLoc;
		move_to_y_loc = destYLoc;
		//}

		//------------------------------------------------------------//
		// fast checking for destination == current location
		//------------------------------------------------------------//
		//if(startXLocLoc==move_to_x_loc && startYLocLoc==move_to_y_loc) // already here
		if (startXLocLoc == destXLoc && startYLocLoc == destYLoc) // already here
		{
			if (cur_x != next_x || cur_y != next_y)
				set_move();
			else
				set_idle();

			return 1;
		}

		//------------------------ find the shortest path --------------------------//
		//
		// Note: seek() will never return PATH_SEEKING as the maxTries==max_node in calling seek()
		//
		// decide the searching to use according to the unit size
		// assume the unit size is always 1x1, 2x2, 3x3 and so on
		// i.e. sprite_info.loc_width == sprite_info.loc_height
		//--------------------------------------------------------------------------//

		result_node_recno = result_node_count = 0;

		SeekPath.set_nation_recno(nation_recno);

		if (mobile_type == UnitConstants.UNIT_LAND)
			select_search_sub_mode(startXLocLoc, startYLocLoc, destXLoc, destYLoc, nation_recno, searchMode);
		int seekResult = SeekPath.seek(startXLocLoc, startYLocLoc, destXLoc, destYLoc, unit_group_id,
			mobile_type, searchMode, miscNo, numOfPath);

		result_node_array = SeekPath.get_result(out result_node_count, out result_path_dist);
		SeekPath.set_sub_mode(); // reset sub_mode searching

		if (seekResult == SeekPath.PATH_IMPOSSIBLE)
			reset_path();

		//-----------------------------------------------------------------------//
		// update ignore_power_nation
		//-----------------------------------------------------------------------//

		if (ai_unit)
		{
			//------- set ignore_power_nation -------//

			if (seekResult == SeekPath.PATH_IMPOSSIBLE)
			{
				switch (ignore_power_nation)
				{
					case 0:
						ignore_power_nation = 1;
						break;
					case 1:
						ignore_power_nation = 2;
						break;
					case 2:
						break;
				}
			}
			else
			{
				if (ignore_power_nation == 1)
					ignore_power_nation = 0;
			}
		}

		//-----------------------------------------------------------------------//
		// if closest node is returned, the destination should not be the real
		// location to go to.  Thus, move_to_?_loc should be adjusted
		//-----------------------------------------------------------------------//
		if (result_node_array != null && result_node_count != 0)
		{
			ResultNode lastNode = result_node_array[result_node_count - 1];
			move_to_x_loc = lastNode.node_x; // adjust move_to_?_loc
			move_to_y_loc = lastNode.node_y;

			result_node_recno = 1; // skip the first node which is the current location
			// check if the unit is moving right now, wait until it reaches the nearest complete tile.
			if (cur_action != SPRITE_MOVE)
			{
				ResultNode nextNode = result_node_array[1];
				set_dir(startXLocLoc, startYLocLoc, nextNode.node_x, nextNode.node_y);
				next_move();
			}
		}
		else // stay in the current location
		{
			move_to_x_loc = startXLocLoc; // adjust move_to_?_loc
			move_to_y_loc = startYLocLoc;

			if (cur_x != next_x || cur_y != next_y)
				set_move();
			else
				set_idle();
		}

		//-------------------------------------------------------//
		// PATH_NODE_USED_UP happens when:
		// Exceed the object's MAX's node limitation, the closest path
		// is returned. Get to the closest path first and continue
		// to seek the path in the background.
		//-------------------------------------------------------//

		return 1;
	}

	private int set_move_to_surround(int buildXLoc, int buildYLoc, int width, int height, int buildingType,
		int miscNo = 0, int readyDist = 0, int curProcessUnitNum = 1)
	{
		//--------------------------------------------------------------//
		// calculate the distance from the object
		//--------------------------------------------------------------//
		int found = 0, foundAgain = 0;
		// 0 for inside, 1 for surrounding, >1 for the rest
		int distance = cal_distance(buildXLoc, buildYLoc, width, height);

		//--------------------------------------------------------------//
		// inside the building
		//--------------------------------------------------------------//
		if (distance == 0)
		{
			reset_path();
			if (cur_x == next_x && cur_y == next_y)
				set_idle();

			return 1;
		}

		if (distance > 1)
		{
			//--------------------------------------------------------------//
			// the searching is divided into 2 parts.
			//
			// part 1 using the firm_type and firm_id to find a shortest path.
			// 
			// part 2
			//	if the width and height is the actual width and height of the
			// firm, the unit move to the surrounding of the firm.
			//
			// if the width and height > the actual width and height of the
			// firm, the unit move to a location far away from the surrounding
			// of the firm.
			//--------------------------------------------------------------//

			//====================================================================//
			// part 1
			//====================================================================//

			Location loc = World.get_loc(buildXLoc, buildYLoc);
			Firm firm = null;
			Town targetTown = null;
			int searchResult = 0;

			switch (buildingType)
			{
				case UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO: // (assign) firm is on the location
					firm = FirmArray[loc.firm_recno()];
					searchResult = search(buildXLoc, buildYLoc, 1, SeekPath.SEARCH_MODE_TO_FIRM, firm.firm_id,
						curProcessUnitNum);
					break;

				case UnitConstants.BUILDING_TYPE_FIRM_BUILD: // (build firm) no firm on the location
					searchResult = search(buildXLoc, buildYLoc, 1, SeekPath.SEARCH_MODE_TO_FIRM, miscNo);
					break;

				case UnitConstants.BUILDING_TYPE_TOWN_MOVE_TO: // (assign) town is on the location
					targetTown = TownArray[loc.town_recno()];
					searchResult = search(buildXLoc, buildYLoc, 1, SeekPath.SEARCH_MODE_TO_TOWN,
						targetTown.TownId, curProcessUnitNum);
					break;

				case UnitConstants.BUILDING_TYPE_SETTLE: // (settle, first unit) no town on the location
					//---------------------------------------------------------------------//
					// the record number sent to the searching algorithm is used to determine
					// the width and the height of the building.  However, the standard
					// dimension for settling is used and the building built is a type of
					// town.  Thus, passing -1 as the recno. to show that "settle" is
					// processed
					//---------------------------------------------------------------------//
					searchResult = search(buildXLoc, buildYLoc, 1, SeekPath.SEARCH_MODE_TO_TOWN, -1, curProcessUnitNum);
					break;

				case UnitConstants.BUILDING_TYPE_VEHICLE:
					searchResult = search(buildXLoc, buildYLoc, 1, SeekPath.SEARCH_MODE_TO_VEHICLE,
						World.get_loc(buildXLoc, buildYLoc).cargo_recno);
					break;

				case UnitConstants.BUILDING_TYPE_WALL: // wall is on the location
					searchResult = search(buildXLoc, buildYLoc, 1,
						miscNo != 0 ? SeekPath.SEARCH_MODE_TO_WALL_FOR_UNIT : SeekPath.SEARCH_MODE_TO_WALL_FOR_GROUP);
					break;

				default:
					break;
			}

			if (searchResult == 0)
				return 0; // incomplete searching

			//====================================================================//
			// part 2
			//====================================================================//
			if (result_node_array != null && result_node_count != 0)
				return edit_path_to_surround(buildXLoc, buildYLoc, buildXLoc + width - 1, buildYLoc + height - 1,
					readyDist);
			else
				return 0;
		}
		else // in the surrounding, no need to move
		{
			reset_path();

			if (cur_x == next_x && cur_y == next_y)
			{
				move_to_x_loc = next_x_loc();
				move_to_y_loc = next_y_loc();
				go_x = cur_x;
				go_y = cur_y;
				set_idle();
				set_dir(move_to_x_loc, move_to_y_loc, buildXLoc + width / 2, buildYLoc + height / 2);
			}

			return 1;
		}
	}

	private int edit_path_to_surround(int objectXLoc1, int objectYLoc1, int objectXLoc2, int objectYLoc2, int readyDist)
	{
		if (result_node_count < 2)
			return 0;

		//----------------------------------------------------------------------------//
		// At this moment, the unit generally has a path to the location inside the object,
		// walk through it and extract a path to the surrounding of the object.
		//----------------------------------------------------------------------------//

		//------- calculate the surrounding top-left and bottom-right points ------//
		int moveScale = move_step_magn();
		int xLoc1 = objectXLoc1 - readyDist - 1;
		int yLoc1 = objectYLoc1 - readyDist - 1;
		int xLoc2 = objectXLoc2 + readyDist + 1;
		int yLoc2 = objectYLoc2 + readyDist + 1;

		//------------------- boundary checking -------------------//
		if (xLoc1 < 0)
			xLoc1 = 0;
		if (yLoc1 < 0)
			yLoc1 = 0;
		if (xLoc2 >= GameConstants.MapSize)
			yLoc1 = GameConstants.MapSize - moveScale;
		if (yLoc2 >= GameConstants.MapSize)
			xLoc2 = GameConstants.MapSize - moveScale;

		//--------------- adjust for air and sea units -----------------//
		if (mobile_type != UnitConstants.UNIT_LAND)
		{
			//------ assume even x, y coordinate is used for UnitConstants.UNIT_SEA and UnitConstants.UNIT_AIR -------//
			if (xLoc1 % 2 != 0)
				xLoc1--;
			if (yLoc1 % 2 != 0)
				yLoc1--;
			if (xLoc2 % 2 != 0)
				xLoc2++;
			if (yLoc2 % 2 != 0)
				yLoc2++;

			if (xLoc2 > GameConstants.MapSize - moveScale)
				xLoc2 = GameConstants.MapSize - moveScale;
			if (yLoc2 > GameConstants.MapSize - moveScale)
				yLoc2 = GameConstants.MapSize - moveScale;
		}

		int checkXLoc = next_x_loc();
		int checkYLoc = next_y_loc();
		int editNode1Index = 0;
		int editNode2Index = 1;
		ResultNode editNode1 = result_node_array[editNode1Index]; // alias the unit's result_node_array
		ResultNode editNode2 = result_node_array[editNode2Index]; // ditto

		int hasMoveStep = 0;
		if (checkXLoc != editNode1.node_x || checkYLoc != editNode1.node_y)
		{
			hasMoveStep += moveScale;
			checkXLoc = editNode1.node_x;
			checkYLoc = editNode1.node_y;
		}

		int i, j;
		// pathDist - counts the disitance of the generated path, found - whether a path to the surrounding is found
		int pathDist = 0, found = 0;
		int vecX, vecY, xMagn, yMagn, magn;

		//------- find the first node that is on the surrounding of the object -------//
		for (i = 1; i < result_node_count; ++i, editNode1Index++, editNode2Index++)
		{
			editNode1 = result_node_array[editNode1Index]; // alias the unit's result_node_array
			editNode2 = result_node_array[editNode2Index]; // ditto

			//------------ calculate parameters for checking ------------//
			vecX = editNode2.node_x - editNode1.node_x;
			vecY = editNode2.node_y - editNode1.node_y;

			magn = ((xMagn = Math.Abs(vecX)) > (yMagn = Math.Abs(vecY))) ? xMagn : yMagn;
			if (xMagn != 0)
			{
				vecX /= xMagn;
				vecX *= moveScale;
			}

			if (yMagn != 0)
			{
				vecY /= yMagn;
				vecY *= moveScale;
			}

			//------------- check each location bewteen editNode1 and editNode2 -------------//
			for (j = 0; j < magn; j += moveScale)
			{
				checkXLoc += vecX;
				checkYLoc += vecY;

				if (checkXLoc >= xLoc1 && checkXLoc <= xLoc2 && checkYLoc >= yLoc1 && checkYLoc <= yLoc2)
				{
					found++;
					break;
				}
			}

			//-------------------------------------------------------------------------------//
			// a path is found, then set unit's parameters for its movement
			//-------------------------------------------------------------------------------//
			if (found != 0)
			{
				editNode2.node_x = checkXLoc;
				editNode2.node_y = checkYLoc;

				if (i == 1) // first editing
				{
					ResultNode firstNode = result_node_array[0];
					if (cur_x == firstNode.node_x * InternalConstants.CellWidth && cur_y == firstNode.node_y * InternalConstants.CellHeight)
					{
						go_x = checkXLoc * InternalConstants.CellWidth;
						go_y = checkYLoc * InternalConstants.CellHeight;
					}
				}

				pathDist += (j + moveScale);
				pathDist -= hasMoveStep;
				result_node_count = i + 1;
				result_path_dist = pathDist;
				move_to_x_loc = checkXLoc;
				move_to_y_loc = checkYLoc;
				break;
			}
			else
			{
				pathDist += magn;
			}
		}

		return found;
	}

	private void search_or_stop(int destX, int destY, int preserveAction = 0, int searchMode = 1, int miscNo = 0)
	{
		Location loc = World.get_loc(destX, destY);
		if (!loc.can_move(mobile_type))
		{
			stop(UnitConstants.KEEP_PRESERVE_ACTION); // let reactivate..() call searching later
			//waiting_term = MAX_SEARCH_OT_STOP_WAIT_TERM;
		}
		else
		{
			search(destX, destY, preserveAction, searchMode, miscNo);
			/*if(mobile_type==UnitRes.UNIT_LAND)
				search(destX, destY, preserveAction, searchMode, miscNo);
			else
				waiting_term = 0;*/
		}
	}

	private void search_or_wait()
	{
		const int SQUARE1 = 9;
		const int SQUARE2 = 25;
		const int SQUARE3 = 49;
		const int DIMENSION = 7;

		int curXLoc = next_x_loc(), curYLoc = next_y_loc();
		int[] surrArray = new int[SQUARE3];
		int xShift, yShift, checkXLoc, checkYLoc, hasFree, i, shouldWait;
		int unitRecno;
		Unit unit;
		Location loc;

		//----------------------------------------------------------------------------//
		// wait if the unit is totally blocked.  Otherwise, call searching
		//----------------------------------------------------------------------------//
		for (shouldWait = 0, hasFree = 0, i = 2; i <= SQUARE3; i++)
		{
			if (i == SQUARE1 || i == SQUARE2 || i == SQUARE3)
			{
				if (hasFree == 0)
				{
					shouldWait++;
					break;
				}
				else
					hasFree = 0;
			}

			Misc.cal_move_around_a_point(i, DIMENSION, DIMENSION, out xShift, out yShift);
			checkXLoc = curXLoc + xShift;
			checkYLoc = curYLoc + yShift;
			if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
				continue;

			loc = World.get_loc(checkXLoc, checkYLoc);
			if (!loc.has_unit(mobile_type))
			{
				hasFree++;
				continue;
			}

			unitRecno = loc.unit_recno(mobile_type);
			if (UnitArray.IsDeleted(unitRecno))
				continue;

			unit = UnitArray[unitRecno];
			if (unit.nation_recno == nation_recno && unit.unit_group_id == unit_group_id &&
			    ((unit.cur_action == SPRITE_WAIT && unit.waiting_term > 1) || unit.cur_action == SPRITE_TURN ||
			     unit.cur_action == SPRITE_MOVE))
			{
				surrArray[i - 2] = unitRecno;
				unit.unit_group_id++;
			}
		}

		//------------------- call searching if should not wait --------------------//
		if (shouldWait == 0)
			search(move_to_x_loc, move_to_y_loc, 1, SeekPath.SEARCH_MODE_IN_A_GROUP);
		//search_or_stop(move_to_x_loc, move_to_y_loc, 1, SEARCH_MODE_IN_A_GROUP);

		for (i = 0; i < SQUARE3; i++)
		{
			if (surrArray[i] != 0)
			{
				unit = UnitArray[surrArray[i]];
				unit.unit_group_id--;
			}
		}

		if (shouldWait == 1)
			set_wait();
	}

	//void   move_to_surround_s2(int destXLoc, int destYLoc); // for 2x2 unit only
	// move to target for using range attack
	private int move_to_range_attack(int targetXLoc, int targetYLoc, int miscNo, int searchMode, int maxRange)
	{
		//---------------------------------------------------------------------------------//
		// part 1, searching
		//---------------------------------------------------------------------------------//
		SeekPath.set_attack_range_para(maxRange);
		search(targetXLoc, targetYLoc, 1, searchMode, miscNo);
		SeekPath.reset_attack_range_para();
		//search(targetXLoc, targetYLoc, 1, searchMode, maxRange);

		if (result_node_array == null || result_node_count == 0)
			return 0;

		//---------------------------------------------------------------------------------//
		// part 2, editing result path
		//---------------------------------------------------------------------------------//
		Location loc = World.get_loc(next_x_loc(), next_y_loc());

		int regionId = loc.region_id; // the region_id this unit in

		//----------------------------------------------------//
		int editNode1Index = result_node_count - 1;
		int editNode2Index = result_node_count - 2;
		ResultNode editNode1 = result_node_array[editNode1Index];
		ResultNode editNode2 = result_node_array[editNode2Index];
		int vecX = editNode1.node_x - editNode2.node_x;
		int vecY = editNode1.node_y - editNode2.node_y;

		if (vecX != 0)
			vecX = ((vecX > 0) ? 1 : -1) * move_step_magn();
		if (vecY != 0)
			vecY = ((vecY > 0) ? 1 : -1) * move_step_magn();

		int x = editNode1.node_x;
		int y = editNode1.node_y;
		int i, found = 0, removedStep = 0, preX = 0, preY = 0;

		for (i = result_node_count; i > 1; i--)
		{
			while (x != editNode2.node_x || y != editNode2.node_y)
			{
				loc = World.get_loc(x, y);
				if (loc.region_id == regionId)
				{
					found = i;
					preX = x;
					preY = y;
					break;
				}

				x -= vecX;
				y -= vecY;
				removedStep++;
			}

			if (found != 0 || i < 3) // found a spot or there is at least more node to try
				break;

			// one more node to try

			editNode1Index = editNode2Index;
			editNode2Index--;
			editNode1 = result_node_array[editNode1Index];
			editNode2 = result_node_array[editNode2Index];

			vecX = editNode1.node_x - editNode2.node_x;
			vecY = editNode1.node_y - editNode2.node_y;
			if (vecX != 0)
				vecX = ((vecX > 0) ? 1 : -1) * move_step_magn();
			if (vecY != 0)
				vecY = ((vecY > 0) ? 1 : -1) * move_step_magn();

			x = editNode1.node_x;
			y = editNode1.node_y;
		}

		//---------------------------------------------------------------------------//
		// update unit parameters
		//---------------------------------------------------------------------------//
		if (found != 0)
		{
			result_node_count = found;
			ResultNode lastNode = result_node_array[result_node_count - 1];
			int goX = go_x >> InternalConstants.CellWidthShift;
			int goY = go_y >> InternalConstants.CellHeightShift;

			//---------------------------------------------------------------------//
			// note: build?Loc-1, build?Loc+width, build?Loc+height may <0 or
			//			>MAX_WORLD_?_LOC.  To prevent errors from occuring, goX, goY
			//			must not be outside the map boundary
			//---------------------------------------------------------------------//
			if (goX == editNode1.node_x && goY == editNode1.node_y)
			{
				go_x = preX * InternalConstants.CellWidth;
				go_y = preY * InternalConstants.CellHeight;
			}
			else if (result_node_count == 2)
			{
				int magnCG = Misc.points_distance(cur_x, cur_y, go_x, go_y);
				int magnNG = Misc.points_distance(next_x, next_y, go_x, go_y);

				if (magnCG != 0 && magnNG != 0)
				{
					//---------- lie on the same line -----------//
					if ((go_x - cur_x) / magnCG == (go_x - next_x) / magnNG &&
					    (go_y - cur_y) / magnCG == (go_y - next_y) / magnNG)
					{
						go_x = preX * InternalConstants.CellWidth;
						go_y = preY * InternalConstants.CellHeight;
					}
				}
			}

			lastNode.node_x = preX;
			lastNode.node_y = preY;
			move_to_x_loc = lastNode.node_x;
			move_to_y_loc = lastNode.node_y;

			result_path_dist -= (removedStep) * move_step_magn();
		}

		return found;
	}

	//---------------- handle blocked action ------------------//
	private void move_to_my_loc(Unit unit)
	{
		int unitDestX, unitDestY;
		if (unit.action_mode2 == UnitConstants.ACTION_MOVE)
		{
			unitDestX = unit.action_x_loc2;
			unitDestY = unit.action_y_loc2;
		}
		else
		{
			unitDestX = unit.move_to_x_loc;
			unitDestY = unit.move_to_y_loc;
		}

		//--------------- init parameters ---------------//
		int unitCurX = unit.next_x_loc();
		int unitCurY = unit.next_y_loc();
		int destX = action_x_loc2;
		int destY = action_y_loc2;
		int curX = next_x_loc();
		int curY = next_y_loc();
		int moveScale = move_step_magn();

		//------------------------------------------------------------------//
		// setting for unit pointed by unit
		//------------------------------------------------------------------//
		if (result_node_array == null) //************BUGHERE
		{
			unit.move_to(destX, destY, 1); // unit pointed by unit is idle before calling searching
		}
		else
		{
			ResultNode resultNode = result_node_array[result_node_recno - 1];
			if (go_x == unit.next_x && go_y == unit.next_y)
			{
				//------ Unit B is in one of the node of the result_node_array ---//
				unit.result_node_count = result_node_count - result_node_recno + 1; // at least there are 2 nodes
				unit.result_node_array = new ResultNode[unit.result_node_count];
				for (int i = 0; i < unit.result_node_count; i++)
				{
					unit.result_node_array[i] = result_node_array[result_node_recno - 1 + i];
				}
			}
			else
			{
				//----- Unit B is in the middle of two nodes in the result_node_array -----//
				unit.result_node_count = result_node_count - result_node_recno + 2;
				unit.result_node_array = new ResultNode[unit.result_node_count];
				ResultNode curNode = unit.result_node_array[0];
				curNode.node_x = unitCurX;
				curNode.node_y = unitCurY;
				for (int i = 0; i < unit.result_node_count - 1; i++)
				{
					unit.result_node_array[i + 1] = result_node_array[result_node_recno - 1 + i];
				}
			}

			//--------------- set unit action ---------------//
			// unit is idle now
			if (unit.action_mode2 == UnitConstants.ACTION_STOP || unit.action_mode2 == UnitConstants.ACTION_MOVE)
			{
				//---------- activate unit pointed by unit now ------------//
				unit.action_mode = unit.action_mode2 = UnitConstants.ACTION_MOVE;
				unit.action_para = unit.action_para2 = 0;
				if (destX != -1 && destY != -1)
				{
					unit.action_x_loc = unit.action_x_loc2 = destX;
					unit.action_y_loc = unit.action_y_loc2 = destY;
				}
				else
				{
					ResultNode lastNode = unit.result_node_array[unit.result_node_count - 1];
					unit.action_x_loc = unit.action_x_loc2 = lastNode.node_x;
					unit.action_y_loc = unit.action_y_loc2 = lastNode.node_y;
				}
			}

			//----------------- set unit movement parameters -----------------//
			unit.result_node_recno = 1;
			unit.result_path_dist = result_path_dist - moveScale;
			unit.move_to_x_loc = move_to_x_loc;
			unit.move_to_y_loc = move_to_y_loc;
			unit.next_move();
		}

		//------------------------------------------------------------------//
		// setting for this unit
		//------------------------------------------------------------------//
		int shouldWait = 0;
		if (next_x == unit.cur_x && next_y == unit.cur_y)
		{
			reset_path();
			result_path_dist = 0;
		}
		else
		{
			terminate_move();
			shouldWait++;
			result_path_dist = moveScale;
		}

		go_x = unit.cur_x;
		go_y = unit.cur_y;
		move_to_x_loc = unitCurX;
		move_to_y_loc = unitCurY;

		if (action_mode2 == UnitConstants.ACTION_MOVE)
		{
			action_x_loc = action_x_loc2 = unitDestX;
			action_y_loc = action_y_loc2 = unitDestY;
		}

		//---------- note: the cur_dir is already the correct direction ---------------//
		result_node_array = new ResultNode[2];
		result_node_array[0] = new ResultNode(curX, curY);
		result_node_array[1] = new ResultNode(unitCurX, unitCurY);
		result_node_count = 2;
		result_node_recno = 2;
		if (shouldWait != 0)
			set_wait(); // wait for the blocking unit to move first
	}

	// used to determine unit size and call other handle_blocked_move.. functions
	private void handle_blocked_move(Location blockedLoc)
	{
		//--- check if the tile we are moving at is blocked by a building ---//
		if (blockedLoc.is_firm() || blockedLoc.is_town() || blockedLoc.is_wall())
		{
			//------------------------------------------------//
			// firm/town/wall is on the blocked location
			//------------------------------------------------//
			reset_path();
			search_or_stop(move_to_x_loc, move_to_y_loc, 1);
			//search(move_to_x_loc, move_to_y_loc, 1);
			return;
		}

		if (next_x_loc() == move_to_x_loc && next_y_loc() == move_to_y_loc && swapping == 0)
		{
			terminate_move(); // terminate since already reaching destination
			return;
		}

		if (!blockedLoc.is_accessible(mobile_type))
		{
			terminate_move(); // the location is not accessible
			return;
		}

		//-----------------------------------------------------------------------------------//
		// there is another sprite on the move_to location, check the combination of both sizes
		//-----------------------------------------------------------------------------------//
		blocked_by_member = 1;

		Unit unit = UnitArray[blockedLoc.unit_recno(mobile_type)];
		//if(unit.sprite_info.loc_width>1 || sprite_info.loc_width>1)
		//{
		//	set_wait();
		//	return;
		//}
		//else
		handle_blocked_move_s11(unit); //------ both units size 1x1

		return;
	}

	private void handle_blocked_move_s11(Unit unit)
	{
		int waitTerm;
		int moveStep = move_step_magn();

		switch (unit.cur_action)
		{
			//------------------------------------------------------------------------------------//
			// handle blocked for units belonging to the same nation.  For those belonging to other
			// nations, wait for it moving to other locations or search for another path.
			//------------------------------------------------------------------------------------//
			case SPRITE_WAIT: // the blocking unit is waiting
			case SPRITE_TURN:
				if (unit.nation_recno == nation_recno)
					handle_blocked_wait(unit); // check for cycle wait for our nation
				else if (waiting_term >= UnitConstants.MAX_WAITING_TERM_DIFF)
				{
					search_or_stop(move_to_x_loc, move_to_y_loc, 1); // recall searching
					waiting_term = 0;
				}
				else // wait
					set_wait();

				return;

			//------------------------------------------------------------------------------------//
			// We know from the cur_action of the blocking unit it is moving to another locations,
			// the blocked unit wait for a number of terms or search again.
			//------------------------------------------------------------------------------------//
			case SPRITE_MOVE:
			case SPRITE_READY_TO_MOVE:
			case SPRITE_SHIP_EXTRA_MOVE:
				// don't wait for caravans, and caravans don't wait for other units
				if (unit_id != UnitConstants.UNIT_CARAVAN && unit.unit_id == UnitConstants.UNIT_CARAVAN)
				{
					search(move_to_x_loc, move_to_y_loc, 1, SeekPath.SEARCH_MODE_A_UNIT_IN_GROUP);
				}
				else
				{
					waitTerm = (nation_recno == unit.nation_recno) ? UnitConstants.MAX_WAITING_TERM_SAME : UnitConstants.MAX_WAITING_TERM_DIFF;
					if (waiting_term >= waitTerm)
					{
						search_or_wait();
						waiting_term = 0;
					}
					else
						set_wait();
				}

				return;

			//------------------------------------------------------------------------------------//
			// handling blocked for idle unit
			//------------------------------------------------------------------------------------//
			case SPRITE_IDLE:
				if (unit.action_mode == UnitConstants.ACTION_SHIP_TO_BEACH)
				{
					//----------------------------------------------------------------------//
					// the blocking unit is trying to move to beach, so wait a number of terms,
					// or call searching again
					//----------------------------------------------------------------------//
					if (Math.Abs(unit.next_x_loc() - unit.action_x_loc2) <= moveStep &&
					    Math.Abs(unit.next_y_loc() - unit.action_y_loc2) <= moveStep &&
					    TerrainRes[World.get_loc(unit.action_x_loc2, unit.action_y_loc2).terrain_id].average_type != TerrainTypeCode.TERRAIN_OCEAN)
					{
						if (action_mode2 == UnitConstants.ACTION_SHIP_TO_BEACH &&
						    action_x_loc2 == unit.action_x_loc2 && action_y_loc2 == unit.action_y_loc2)
						{
							int tempX, tempY;
							ship_to_beach(action_x_loc2, action_y_loc2, out tempX, out tempY);
						}
						else
						{
							waitTerm = (nation_recno == unit.nation_recno)
								? UnitConstants.MAX_WAITING_TERM_SAME
								: UnitConstants.MAX_WAITING_TERM_DIFF;
							if (waiting_term >= waitTerm)
								stop2();
							else
								set_wait();
						}

						return;
					}
				}

				if (unit.nation_recno == nation_recno) //-------- same nation
				{
					//------------------------------------------------------------------------------------//
					// units from our nation
					//------------------------------------------------------------------------------------//
					if (unit.unit_group_id == unit_group_id)
					{
						//--------------- from the same group -----------------//
						if (wayPoints.Count != 0 && unit.wayPoints.Count == 0)
						{
							//------------ reset way point --------------//
							stop2();
							ResetWayPoints();
						}
						else if ((unit.next_x_loc() != move_to_x_loc || unit.next_y_loc() != move_to_y_loc) &&
						         (unit.cur_action == SPRITE_IDLE && unit.action_mode2 == UnitConstants.ACTION_STOP))
							if (ConfigAdv.fix_path_blocked_by_team)
								handle_blocked_by_idle_unit(unit);
							else
								move_to_my_loc(unit); // push the blocking unit and exchange their destination
						else if (unit.action_mode == UnitConstants.ACTION_SETTLE)
							set_wait(); // wait for the settler
						else if (waiting_term > UnitConstants.MAX_WAITING_TERM_SAME)
						{
							//---------- stop if wait too long ----------//
							terminate_move();
							waiting_term = 0;
						}
						else
							set_wait();
					}
					else if (unit.action_mode2 == UnitConstants.ACTION_STOP)
						handle_blocked_by_idle_unit(unit);
					else if (wayPoints.Count != 0 && unit.wayPoints.Count == 0)
					{
						stop2();
						ResetWayPoints();
					}
					else
						search_or_stop(move_to_x_loc, move_to_y_loc, 1); // recall A* algorithm by default mode
				}
				else // different nation
				{
					//------------------------------------------------------------------------------------//
					// units from other nations
					//------------------------------------------------------------------------------------//
					if (unit.next_x_loc() == move_to_x_loc && unit.next_y_loc() == move_to_y_loc)
					{
						terminate_move(); // destination occupied by other unit

						if (action_mode == UnitConstants.ACTION_ATTACK_UNIT &&
						    unit.nation_recno != nation_recno && unit.sprite_recno == action_para)
						{
							set_dir(next_x, next_y, unit.next_x, unit.next_y);
							if (is_dir_correct())
								attack_unit(action_para, 0, 0, true);
							else
								set_turn();
							cur_frame = 1;
						}
					}
					else
						search_or_stop(move_to_x_loc, move_to_y_loc, 1); // recall A* algorithm by default mode
				}

				return;

			//------------------------------------------------------------------------------------//
			// don't wait for attackers from other nations, search for another path.
			//------------------------------------------------------------------------------------//
			case SPRITE_ATTACK:
				//----------------------------------------------------------------//
				// don't wait for other nation unit, call searching again
				//----------------------------------------------------------------//
				if (nation_recno != unit.nation_recno)
				{
					search_or_stop(move_to_x_loc, move_to_y_loc, 1);
					return;
				}

				//------------------------------------------------------------------------------------//
				// for attackers owned by our commander, handled blocked case by case as follows.
				//------------------------------------------------------------------------------------//
				switch (unit.action_mode)
				{
					case UnitConstants.ACTION_ATTACK_UNIT:
						if (action_para != 0 && !UnitArray.IsDeleted(action_para))
						{
							Unit target = UnitArray[action_para];
							handle_blocked_attack_unit(unit, target);
						}
						else
							search_or_stop(move_to_x_loc, move_to_y_loc, 1, SeekPath.SEARCH_MODE_A_UNIT_IN_GROUP);

						break;

					case UnitConstants.ACTION_ATTACK_FIRM:
						if (unit.action_para == 0 || FirmArray.IsDeleted(unit.action_para))
							set_wait();
						else
							handle_blocked_attack_firm(unit);
						break;

					case UnitConstants.ACTION_ATTACK_TOWN:
						if (unit.action_para == 0 || TownArray.IsDeleted(unit.action_para))
							set_wait();
						else
							handle_blocked_attack_town(unit);
						break;

					case UnitConstants.ACTION_ATTACK_WALL:
						if (unit.action_para != 0)
							set_wait();
						else
							handle_blocked_attack_wall(unit);
						break;

					case UnitConstants.ACTION_GO_CAST_POWER:
						set_wait();
						break;

					default:
						break;
				}

				return;

			//------------------------------------------------------------------------------------//
			// the blocked unit can pass after the blocking unit disappears in air.
			//------------------------------------------------------------------------------------//
			case SPRITE_DIE:
				set_wait(); // assume this unit will not wait too long
				return;

			default:
				break;
		}
	}

	private void handle_blocked_by_idle_unit(Unit unit)
	{
		const int TEST_DIMENSION = 10;
		const int TEST_LIMIT = TEST_DIMENSION * TEST_DIMENSION;

		bool notLandUnit = (mobile_type != UnitConstants.UNIT_LAND);
		int unitXLoc = unit.next_x_loc();
		int unitYLoc = unit.next_y_loc();
		int xShift, yShift;
		int checkXLoc, checkYLoc;

		int xSign = Misc.Random(2) != 0 ? 1 : -1;
		int ySign = Misc.Random(2) != 0 ? 1 : -1;

		for (int i = 2; i <= TEST_LIMIT; i++)
		{
			Misc.cal_move_around_a_point(i, TEST_DIMENSION, TEST_DIMENSION, out xShift, out yShift);
			xShift *= xSign;
			yShift *= ySign;

			if (notLandUnit)
			{
				checkXLoc = unitXLoc + xShift * 2;
				checkYLoc = unitYLoc + yShift * 2;
			}
			else
			{
				checkXLoc = unitXLoc + xShift;
				checkYLoc = unitYLoc + yShift;
			}

			if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
				continue;

			Location loc = World.get_loc(checkXLoc, checkYLoc);
			if (!loc.can_move(unit.mobile_type))
				continue;

			if (on_my_path(checkXLoc, checkYLoc))
				continue;

			unit.move_to(checkXLoc, checkYLoc);
			set_wait();
			return;
		}

		stop(UnitConstants.KEEP_DEFENSE_MODE);

		//--------------------------------------------------------------------------------//
		// improved version!!!
		//--------------------------------------------------------------------------------//
		/*int testResult = 0, worstCase=0;
		int worstXLoc=-1, worstYLoc=-1;
		int startCount, endCount;
		int i, j;
		
		for(j=0; j<2; j++)
		{
			//----------- set the startCount and endCount ------------//
			if(j==0)
			{
				startCount = 2;
				endCount = 9;
			}
			else
			{
				startCount = 10;
				endCount = TEST_LIMIT;
			}
	
			for(i=startCount; i<=endCount; i++)
			{
				misc.cal_move_around_a_point(i, TEST_DIMENSION, TEST_DIMENSION, xShift, yShift);
				if(notLandUnit)
				{
					checkXLoc = unitXLoc + xShift*2;
					checkYLoc = unitYLoc + yShift*2;
				}
				else
				{
					checkXLoc = unitXLoc + xShift;
					checkYLoc = unitYLoc + yShift;
				}
			
				if(checkXLoc<0 || checkXLoc>=MAX_WORLD_X_LOC || checkYLoc<0 || checkYLoc>=MAX_WORLD_Y_LOC)
					continue;
	
				loc = world.get_loc(checkXLoc , checkYLoc);
				if(!loc.can_move(unit.mobile_type))
					continue;
	
				//-------------------------------------------------------------------//
				// a possible location
				//-------------------------------------------------------------------//
				testResult = on_my_path(checkXLoc, checkYLoc);
				if(testResult)
				{
					if(j==0 && !worstCase)
					{
						worstCase++;
						worstXLoc = checkXLoc;
						worstYLoc = checkYLoc;
					}
					continue;
				}
	
				unit.move_to(checkXLoc, checkYLoc):
				set_wait();
				return;
			}
		}
	
		//-------------------------------------------------------------------//
		if(worstCase)
		{
			unit.move_to(worstXLoc, worstYLoc);
			set_wait();
		}
		else
			stop(UnitConstants.KEEP_DEFENSE_MODE);*/
	}

	private bool on_my_path(int checkXLoc, int checkYLoc)
	{
		for (int i = result_node_recno - 1; i < result_node_count; i++)
		{
			ResultNode curNode = result_node_array[i - 1];
			ResultNode nextNode = result_node_array[i];
			if ((curNode.node_x - checkXLoc) * (checkYLoc - nextNode.node_y) ==
			    (curNode.node_y - checkYLoc) * (checkXLoc - nextNode.node_x)) // point of division
				return true;
		}

		return false;
	}

	private void handle_blocked_wait(Unit unit)
	{
		int stepMagn = move_step_magn();
		int cycleWait = 0;
		Location loc;

		if (is_dir_correct())
		{
			Unit blockedUnit = unit;
			SpriteInfo unitSpriteInfo = unit.sprite_info;
			int nextX, nextY, loop = 0, i;

			//---------------------------------------------------------------//
			// construct a cycle_waiting array to store the sprite_recno of
			// those units in cycle_waiting in order to prevent forever looping
			// in the checking
			//---------------------------------------------------------------//
			int arraySize = 20;
			cycle_wait_unit_array_def_size = arraySize;
			cycle_wait_unit_index = 0;
			cycle_wait_unit_array_multipler = 1;
			cycle_wait_unit_array = new int[cycle_wait_unit_array_def_size];

			//---------------------------------------------------------------//
			// don't handle the case blocked by size 2x2 unit in this moment
			//---------------------------------------------------------------//
			while (cycleWait == 0 && blockedUnit.cur_action == SPRITE_WAIT)
			{
				if (unitSpriteInfo.loc_width > 1)
					break; // don't handle unit size > 1

				if (!blockedUnit.is_dir_correct())
					break;

				//----------------------------------------------------------------------------------------//
				// cur_x, cur_y of unit pointed by blockedUnit should be exactly inside a tile
				//----------------------------------------------------------------------------------------//
				nextX = blockedUnit.cur_x + stepMagn * move_x_pixel_array[blockedUnit.final_dir];
				nextY = blockedUnit.cur_y + stepMagn * move_y_pixel_array[blockedUnit.final_dir];

				//---------- calculate location blocked unit attempts to move to ---------//
				nextX >>= InternalConstants.CellWidthShift;
				nextY >>= InternalConstants.CellHeightShift;

				loc = World.get_loc(nextX, nextY);
				bool blocked = loc.has_unit(mobile_type);

				//---------------- the unit is also waiting ---------------//
				if (blocked && (blockedUnit.move_to_x_loc != blockedUnit.cur_x_loc() ||
				                blockedUnit.move_to_y_loc != blockedUnit.cur_y_loc()))
				{
					if (loc.unit_recno(mobile_type) == sprite_recno)
						cycleWait = 1;
					else
					{
						for (i = 0; i < cycle_wait_unit_index; i++)
						{
							//---------- checking for forever loop ----------------//
							if (cycle_wait_unit_array[i] == blockedUnit.sprite_recno)
							{
								loop = 1;
								break;
							}
						}

						if (loop != 0)
							break;

						//------------------------------------------------------//
						// resize array if required size is larger than arraySize
						//------------------------------------------------------//
						if (cycle_wait_unit_index >= arraySize)
						{
							cycle_wait_unit_array_multipler++;
							arraySize = cycle_wait_unit_array_def_size * cycle_wait_unit_array_multipler;
							int[] cycle_wait_unit_array_new = new int[arraySize];
							for (int j = 0; j < cycle_wait_unit_array.Length; j++)
							{
								cycle_wait_unit_array_new[j] = cycle_wait_unit_array[j];
							}

							cycle_wait_unit_array = cycle_wait_unit_array_new;
						}
						else
						{
							//-------- store recno of next blocked unit ----------//
							cycle_wait_unit_array[cycle_wait_unit_index++] = blockedUnit.sprite_recno;
							loc = World.get_loc(nextX, nextY);
							blockedUnit = UnitArray[loc.unit_recno(mobile_type)];
							unitSpriteInfo = blockedUnit.sprite_info;
						}
					}
				}
				else
					break;
			}

			//---------- deinit data structure -------//
			cycle_wait_unit_array = null;
		}

		if (cycleWait != 0)
		{
			//----------------------------------------------------------------------//
			// shift the recno of all the unit in the cycle
			//----------------------------------------------------------------------//
			int backupSpriteRecno;
			World.set_unit_recno(cur_x_loc(), cur_y_loc(), mobile_type, 0); // empty the firt node in the cycle
			cycle_wait_shift_recno(this, unit); // shift all the unit in the cycle
			backupSpriteRecno = World.get_unit_recno(cur_x_loc(), cur_y_loc(), mobile_type);
			World.set_unit_recno(cur_x_loc(), cur_y_loc(), mobile_type, sprite_recno);
			set_next(unit.cur_x, unit.cur_y, -stepMagn, 1);
			set_move();
			World.set_unit_recno(unit.cur_x_loc(), unit.cur_y_loc(), mobile_type, sprite_recno);
			World.set_unit_recno(cur_x_loc(), cur_y_loc(), mobile_type, backupSpriteRecno);
			swapping = 1;
		}
		else // not in a cycle
		{
			set_wait();

			//if(waiting_term>=MAX_WAITING_TERM_SAME)
			if (waiting_term >= UnitConstants.MAX_WAITING_TERM_SAME * move_step_magn())
			{
				//-----------------------------------------------------------------//
				// codes used to speed up frame rate
				//-----------------------------------------------------------------//
				loc = World.get_loc(move_to_x_loc, move_to_y_loc);
				if (!loc.can_move(mobile_type) && action_mode2 != UnitConstants.ACTION_MOVE)
					stop(UnitConstants.KEEP_PRESERVE_ACTION); // let reactivate..() call searching later
				else
					search_or_wait();

				waiting_term = 0;
			}
		}
	}

	private void cycle_wait_shift_recno(Unit curUnit, Unit nextUnit)
	{
		int stepMagn = move_step_magn();
		Unit blockedUnit;
		Location loc;

		//----------- find the next location ------------//
		int nextX = nextUnit.cur_x + stepMagn * move_x_pixel_array[nextUnit.final_dir];
		int nextY = nextUnit.cur_y + stepMagn * move_y_pixel_array[nextUnit.final_dir];

		nextX >>= InternalConstants.CellWidthShift;
		nextY >>= InternalConstants.CellHeightShift;

		if (nextX != cur_x_loc() || nextY != cur_y_loc())
		{
			loc = World.get_loc(nextX, nextY);
			blockedUnit = UnitArray[loc.unit_recno(nextUnit.mobile_type)];
		}
		else
		{
			blockedUnit = this;
		}

		if (blockedUnit != this)
		{
			cycle_wait_shift_recno(nextUnit, blockedUnit);
			nextUnit.set_next(blockedUnit.cur_x, blockedUnit.cur_y, -stepMagn, 1);
			nextUnit.set_move();
			World.set_unit_recno(blockedUnit.cur_x_loc(), blockedUnit.cur_y_loc(),
				nextUnit.mobile_type, nextUnit.sprite_recno);
			World.set_unit_recno(nextUnit.cur_x_loc(), nextUnit.cur_y_loc(), nextUnit.mobile_type, 0);
			nextUnit.swapping = 1;
		}
		else // the cycle shift is ended
		{
			nextUnit.set_next(cur_x, cur_y, -stepMagn, 1);
			nextUnit.set_move();
			World.set_unit_recno(cur_x_loc(), cur_y_loc(), nextUnit.mobile_type, nextUnit.sprite_recno);
			World.set_unit_recno(nextUnit.cur_x_loc(), nextUnit.cur_y_loc(), nextUnit.mobile_type, 0);

			nextUnit.swapping = 1;
		}
	}

	private void opposite_direction_blocked(int vecX, int vecY, int unitVecX, int unitVecY, Unit unit)
	{
		//---------------------------------------------------------------------------//
		// processing swapping only when both units are exactly in the tiles
		//---------------------------------------------------------------------------//
		if (unit.cur_action != SPRITE_IDLE)
		{
			if (unit.move_to_x_loc != move_to_x_loc || unit.move_to_y_loc != move_to_y_loc)
			{
				int stepMagn = move_step_magn();

				World.set_unit_recno(unit.cur_x_loc(), unit.cur_y_loc(), mobile_type, 0);
				set_next(unit.cur_x, unit.cur_y, -stepMagn, -1);

				World.set_unit_recno(unit.cur_x_loc(), unit.cur_y_loc(), mobile_type, unit.sprite_recno);
				World.set_unit_recno(cur_x_loc(), cur_y_loc(), unit.mobile_type, 0);
				unit.set_next(cur_x, cur_y, -stepMagn, 1);

				World.set_unit_recno(unit.cur_x_loc(), unit.cur_y_loc(), mobile_type, sprite_recno);
				World.set_unit_recno(cur_x_loc(), cur_y_loc(), unit.mobile_type, unit.sprite_recno);

				set_move();
				unit.set_move();

				swapping = 1;
				unit.swapping = 1;
			}
			else
			{
				terminate_move();
			}
		}
		else
		{
			//----------------------------------------------------------------------//
			// If the unit pointed by unit (unit B) has the same unit_id, rank_id
			//	and both	are in the same group, this unit will order the other unit to
			// move to its location and this unit will occupy the location of the unit B.
			//
			// If the above condition is not fulfilled, swapping is processed.
			//----------------------------------------------------------------------//
			if (unit_id != unit.unit_id || rank_id != unit.rank_id || unit_group_id != unit.unit_group_id)
			{
				if (unit.move_to_x_loc != move_to_x_loc || unit.move_to_y_loc != move_to_y_loc)
				{
					//----------------- process swapping ---------------//
					set_wait();
					unit.set_dir(unit.next_x, unit.next_y, next_x, next_y);
					set_dir(next_x, next_y, unit.next_x, unit.next_y);

					unit.result_node_array = new ResultNode[2];
					unit.result_node_array[0].node_x = next_x_loc();
					unit.result_node_array[0].node_y = next_y_loc();
					unit.result_node_array[1].node_x = unit.next_x_loc();
					unit.result_node_array[1].node_y = unit.next_y_loc();
					unit.result_node_count = 2;
					unit.result_node_recno = 1;

					unit.set_wait();
					unit.go_x = next_x;
					unit.go_y = next_y;

					unit.result_path_dist = 2;

					swapping = 1;
					unit.swapping = 1;
				}
				else
				{
					terminate_move();
				}
			}
			else
			{
				//------------ process move_to_my_loc or terminate the movement -----------//
				if (unit.move_to_x_loc != move_to_x_loc || unit.move_to_y_loc != move_to_y_loc)
					move_to_my_loc(unit);
				else
					terminate_move();
			}
		}
	}

	private void handle_blocked_attack_unit(Unit unit, Unit target)
	{
		if (action_para == target.sprite_recno && unit.action_para == target.sprite_recno &&
		    action_mode == unit.action_mode)
		{
			//----------------- both attack the same target --------------------//
			handle_blocked_same_target_attack(unit, target);
		}
		else
		{
			search_or_stop(move_to_x_loc, move_to_y_loc, 1, SeekPath.SEARCH_MODE_A_UNIT_IN_GROUP); // recall A* algorithm
		}
		//search(move_to_x_loc, move_to_y_loc, 1, SEARCH_MODE_A_UNIT_IN_GROUP); // recall A* algorithm
	}

	private void handle_blocked_attack_firm(Unit unit)
	{
		if (action_x_loc == unit.action_x_loc && action_y_loc == unit.action_y_loc &&
		    action_para == unit.action_para && action_mode == unit.action_mode)
		{
			//------------- both attacks the same firm ------------//
			Location loc = World.get_loc(action_x_loc, action_y_loc);
			if (!loc.is_firm())
				stop2(UnitConstants.KEEP_DEFENSE_MODE); // stop since firm is deleted
			else
			{
				Firm firm = FirmArray[action_para];
				FirmInfo firmInfo = FirmRes[firm.firm_id];

				if (space_for_attack(action_x_loc, action_y_loc, UnitConstants.UNIT_LAND,
					    firmInfo.loc_width, firmInfo.loc_height))
				{
					//------------ found surrounding place to attack the firm -------------//
					if (mobile_type == UnitConstants.UNIT_LAND)
						set_move_to_surround(firm.loc_x1, firm.loc_y1,
							firmInfo.loc_width, firmInfo.loc_height, UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO);
					else
						attack_firm(firm.loc_x1, firm.loc_y1);
				}
				else // no surrounding place found, stop now
					stop(UnitConstants.KEEP_PRESERVE_ACTION);
			}
		}
		else // let process_idle() handle it
			stop();
	}

	private void handle_blocked_attack_town(Unit unit)
	{
		if (action_x_loc == unit.action_x_loc && action_y_loc == unit.action_y_loc &&
		    action_para == unit.action_para && action_mode == unit.action_mode)
		{
			//---------------- both attacks the same town ----------------------//
			Location loc = World.get_loc(action_x_loc, action_y_loc);
			if (!loc.is_town())
				stop2(UnitConstants.KEEP_DEFENSE_MODE); // stop since town is deleted
			else if (space_for_attack(action_x_loc, action_y_loc, UnitConstants.UNIT_LAND,
				         InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT))
			{
				//------------ found surrounding place to attack the town -------------//
				Town town = TownArray[action_para];
				{
					if (mobile_type == UnitConstants.UNIT_LAND)
						set_move_to_surround(town.LocX1, town.LocY1,
							InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, UnitConstants.BUILDING_TYPE_TOWN_MOVE_TO);
					else
						attack_town(town.LocX1, town.LocY1);
				}
			}
			else // no surrounding place found, stop now
				stop(UnitConstants.KEEP_PRESERVE_ACTION);
		}
		else
			stop();
	}

	private void handle_blocked_attack_wall(Unit unit)
	{
		if (action_x_loc == unit.action_x_loc && action_y_loc == unit.action_y_loc && action_mode == unit.action_mode)
		{
			//------------- both attacks the same wall ------------//
			Location loc = World.get_loc(action_x_loc, action_y_loc);
			if (!loc.is_wall())
				stop2(UnitConstants.KEEP_DEFENSE_MODE); // stop since wall is deleted
			else if (space_for_attack(action_x_loc, action_y_loc, UnitConstants.UNIT_LAND, 1, 1))
			{
				//------------ found surrounding place to attack the wall -------------//
				// search for a unit only, not for a group
				if (mobile_type == UnitConstants.UNIT_LAND)
					set_move_to_surround(action_x_loc, action_y_loc, 1, 1, UnitConstants.BUILDING_TYPE_WALL);
				else
					attack_wall(action_x_loc, action_y_loc);
			}
			else // no surrounding place found, stop now
				stop(UnitConstants.KEEP_PRESERVE_ACTION); // no space available, so stop to wait for space to attack the wall
		}
		else
		{
			if (action_x_loc == -1 || action_y_loc == -1)
				stop();
			else
				set_wait();
		}
	}

	private void handle_blocked_same_target_attack(Unit unit, Unit target)
	{
		//----------------------------------------------------------//
		// this unit is now waiting and the unit pointed by unit
		// is attacking the unit pointed by target
		//----------------------------------------------------------//
		if (space_for_attack(action_x_loc, action_y_loc, target.mobile_type,
			    target.sprite_info.loc_width, target.sprite_info.loc_height))
		{
			search_or_stop(move_to_x_loc, move_to_y_loc, 1, SeekPath.SEARCH_MODE_TO_ATTACK, target.sprite_recno);
			//search(move_to_x_loc, move_to_y_loc, 1, SEARCH_MODE_TO_ATTACK, target.sprite_recno);
		}
		else if (in_any_defense_mode())
		{
			general_defend_mode_detect_target();
		}
		else if (Misc.points_distance(next_x_loc(), next_y_loc(), action_x_loc, action_y_loc) < UnitConstants.ATTACK_DETECT_DISTANCE)
		{
			//------------------------------------------------------------------------//
			// if the target is within the detect range, stop the unit's action to detect
			// another target if any exist. In case, there is no other target, the unit
			// will still attack the original target since it is the only target in the
			// detect range
			//------------------------------------------------------------------------//
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
		}
		else
			set_wait(); // set wait to stop the movement
	}

	//====== support functions for process_attack_unit()
	private void target_move(Unit targetUnit)
	{
		//------------------------------------------------------------------------------------//
		// chekcing whether ship can follow to attack target. It is always true if the unit is
		// not ship. 1 for allowing, 0 otherwise
		//------------------------------------------------------------------------------------//
		int allowMove = 1;
		if (sprite_info.sprite_sub_type == 'M')
		{
			UnitInfo unitInfo = UnitRes[unit_id];
			if (unitInfo.carry_goods_capacity != 0)
			{
				UnitMarine ship = (UnitMarine)this;
				if (ship.auto_mode != 0 && ship.stop_defined_num > 1)
					allowMove = 0;
			}
		}

		//---------------------------------------------------------//
		int targetXLoc = targetUnit.next_x_loc();
		int targetYLoc = targetUnit.next_y_loc();
		SpriteInfo targetSpriteInfo = targetUnit.sprite_info;

		int attackDistance = cal_distance(targetXLoc, targetYLoc, targetSpriteInfo.loc_width, targetSpriteInfo.loc_height);
		action_x_loc2 = action_x_loc = targetXLoc; // update target location
		action_y_loc2 = action_y_loc = targetYLoc;

		//---------------------------------------------------------------------//
		// target is out of attacking range, move closer to it
		//---------------------------------------------------------------------//
		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();
		if (attackDistance > attack_range)
		{
			//---------------- stop all actions if not allow to move -----------------//
			if (allowMove == 0)
			{
				stop2();
				return;
			}

			//---------------------------------------------------------------------//
			// follow the target using the result_path_dist
			//---------------------------------------------------------------------//
			if (!update_attack_path_dist())
			{
				if (cur_action == SPRITE_MOVE || cur_action == SPRITE_WAIT || cur_action == SPRITE_READY_TO_MOVE)
					return;
			}

			if (move_try_to_range_attack(targetUnit) != 0)
			{
				//-----------------------------------------------------------------------//
				// reset attack parameters
				//-----------------------------------------------------------------------//
				range_attack_x_loc = range_attack_y_loc = -1;

				// choose better attack mode to attack the target
				choose_best_attack_mode(attackDistance, targetUnit.mobile_type);
			}
		}
		else // attackDistance <= attack_range
		{
			//-----------------------------------------------------------------------------//
			// although the target has moved, the unit can still attack it. no need to move
			//-----------------------------------------------------------------------------//
			if (Math.Abs(cur_x - next_x) >= sprite_info.speed || Math.Abs(cur_y - next_y) >= sprite_info.speed)
				return; // return as moving

			if (attackDistance == 1 && attack_range > 1) // may change attack mode
				choose_best_attack_mode(attackDistance, targetUnit.mobile_type);

			if (attack_range > 1) // range attack
			{
				//------------------ do range attack ----------------------//
				AttackInfo attackInfo = attack_info_array[cur_attack];
				// range attack possible
				if (BulletArray.add_bullet_possible(curXLoc, curYLoc, mobile_type, targetXLoc, targetYLoc,
					    targetUnit.mobile_type, targetSpriteInfo.loc_width, targetSpriteInfo.loc_height,
					    out range_attack_x_loc, out range_attack_y_loc, attackInfo.bullet_speed, attackInfo.bullet_sprite_id))
				{
					set_cur(next_x, next_y);

					set_attack_dir(curXLoc, curYLoc, range_attack_x_loc, range_attack_y_loc);
					if (ConfigAdv.unit_target_move_range_cycle)
					{
						cycle_eqv_attack();
						attackInfo = attack_info_array[cur_attack]; // cur_attack may change
						cur_frame = 1;
					}

					if (is_dir_correct())
						set_attack();
					else
						set_turn();
				}
				else // unable to do range attack, move to target
				{
					if (allowMove == 0)
					{
						stop2();
						return;
					}

					if (move_try_to_range_attack(targetUnit) == 1)
					{
						//range_attack_x_loc = range_attack_y_loc = -1;
						choose_best_attack_mode(attackDistance, targetUnit.mobile_type);
					}
				}
			}
			else if (attackDistance == 1) // close attack
			{
				set_cur(next_x, next_y);
				set_attack_dir(curXLoc, curYLoc, targetXLoc, targetYLoc);
				cur_frame = 1;

				if (is_dir_correct())
					set_attack();
				else
					set_turn();
			}
		}
	}

	private void attack_target(Unit targetUnit)
	{
		if (remain_attack_delay != 0)
			return;

		int unitXLoc = next_x_loc();
		int unitYLoc = next_y_loc();

		if (attack_range > 1) // use range attack
		{
			//---------------- use range attack -----------------//
			AttackInfo attackInfo = attack_info_array[cur_attack];

			if (cur_frame != attackInfo.bullet_out_frame)
				return; // wait for bullet_out_frame

			if (!BulletArray.bullet_path_possible(unitXLoc, unitYLoc, mobile_type,
				    range_attack_x_loc, range_attack_y_loc, targetUnit.mobile_type,
				    attackInfo.bullet_speed, attackInfo.bullet_sprite_id))
			{
				SpriteInfo targetSpriteInfo = targetUnit.sprite_info;
				// seek for another possible point to attack if target size > 1x1
				if ((targetSpriteInfo.loc_width > 1 || targetSpriteInfo.loc_height > 1) &&
				    !BulletArray.add_bullet_possible(unitXLoc, unitYLoc, mobile_type, action_x_loc, action_y_loc,
					    targetUnit.mobile_type, targetSpriteInfo.loc_width, targetSpriteInfo.loc_height,
					    out range_attack_x_loc, out range_attack_y_loc, attackInfo.bullet_speed, attackInfo.bullet_sprite_id))
				{
					//------ no suitable location to attack target by bullet, move to target --------//
					if (result_node_array == null || result_node_count == 0 || result_path_dist == 0)
						if (move_try_to_range_attack(targetUnit) == 0)
							return; // can't reach a location to attack target
				}
			}

			BulletArray.AddBullet(this, targetUnit);
			add_close_attack_effect();

			// ------- reduce power --------//
			cur_power -= attackInfo.consume_power;
			if (cur_power < 0) // ***** BUGHERE
				cur_power = 0;
			set_remain_attack_delay();
			return; // bullet emits
		}
		else // close attack
		{
			//--------------------- close attack ------------------------//
			AttackInfo attackInfo = attack_info_array[cur_attack];

			if (cur_frame == cur_sprite_attack().frame_count)
			{
				if (targetUnit.unit_id == UnitConstants.UNIT_EXPLOSIVE_CART && targetUnit.is_nation(nation_recno))
					((UnitExpCart)targetUnit).trigger_explode();
				else
					hit_target(this, targetUnit, actual_damage(), nation_recno);

				add_close_attack_effect();

				//------- reduce power --------//
				cur_power -= attackInfo.consume_power;
				if (cur_power < 0) // ***** BUGHERE
					cur_power = 0;
				set_remain_attack_delay();
			}
		}
	}

	private bool on_way_to_attack(Unit targetUnit)
	{
		if (mobile_type == UnitConstants.UNIT_LAND)
		{
			if (attack_range == 1)
			{
				//------------------------------------------------------------//
				// for close attack, the unit unable to attack the target if
				// it is not in the target surrounding
				//------------------------------------------------------------//
				if (result_path_dist > attack_range)
					return detect_surround_target();
			}
			else if (result_path_dist != 0 && cur_action != SPRITE_TURN)
			{
				if (detect_surround_target())
					return true; // detect surrounding target while walking
			}
		}

		int targetXLoc = targetUnit.next_x_loc();
		int targetYLoc = targetUnit.next_y_loc();
		SpriteInfo targetSpriteInfo = targetUnit.sprite_info;

		int attackDistance = cal_distance(targetXLoc, targetYLoc, targetSpriteInfo.loc_width, targetSpriteInfo.loc_height);

		if (attackDistance <= attack_range) // able to attack target
		{
			if ((attackDistance == 1) && attack_range > 1) // often false condition is checked first
				choose_best_attack_mode(1, targetUnit.mobile_type); // may change to close attack

			if (attack_range > 1) // use range attack
			{
				set_cur(next_x, next_y);

				AttackInfo attackInfo = attack_info_array[cur_attack];
				int curXLoc = next_x_loc();
				int curYLoc = next_y_loc();
				if (!BulletArray.add_bullet_possible(curXLoc, curYLoc, mobile_type, targetXLoc, targetYLoc,
					    targetUnit.mobile_type, targetSpriteInfo.loc_width, targetSpriteInfo.loc_height,
					    out range_attack_x_loc, out range_attack_y_loc,
					    attackInfo.bullet_speed, attackInfo.bullet_sprite_id))
				{
					//------- no suitable location, move to target ---------//
					if (result_node_array == null || result_node_count == 0 || result_path_dist == 0)
						if (move_try_to_range_attack(targetUnit) == 0)
							return false; // can't reach a location to attack target

					return false;
				}

				//---------- able to do range attack ----------//
				set_attack_dir(next_x_loc(), next_y_loc(), range_attack_x_loc, range_attack_y_loc);
				cur_frame = 1;

				if (is_dir_correct())
					set_attack();
				else
					set_turn();
			}
			else // close attack
			{
				//---------- attack now ---------//
				set_cur(next_x, next_y);
				terminate_move();
				set_attack_dir(next_x_loc(), next_y_loc(), targetXLoc, targetYLoc);

				if (is_dir_correct())
					set_attack();
				else
					set_turn();
			}
		}

		return false;
	}

	private bool detect_surround_target()
	{
		const int DIMENSION = 3;
		const int CHECK_SIZE = DIMENSION * DIMENSION;

		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();
		int checkXLoc, checkYLoc, xShift, yShift;
		Unit target;
		int targetRecno;

		for (int i = 2; i <= CHECK_SIZE; ++i)
		{
			Misc.cal_move_around_a_point(i, DIMENSION, DIMENSION, out xShift, out yShift);
			checkXLoc = curXLoc + xShift;
			checkYLoc = curYLoc + yShift;
			if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
				continue;

			Location loc = World.get_loc(checkXLoc, checkYLoc);

			if (loc.has_unit(UnitConstants.UNIT_LAND))
			{
				targetRecno = loc.unit_recno(UnitConstants.UNIT_LAND);
				if (UnitArray.IsDeleted(targetRecno))
					continue;

				target = UnitArray[targetRecno];
				if (target.nation_recno == nation_recno)
					continue;

				if (idle_detect_unit_checking(targetRecno))
				{
					attack_unit(targetRecno, 0, 0, true);
					return true;
				}
			}
		}

		return false;
	}

	private bool update_attack_path_dist()
	{
		if (result_path_dist <= 6) //1-6
		{
			return true;
		}
		else if (result_path_dist <= 10) // 8, 10
		{
			return ((result_path_dist - 6) % 2) == 0;
		}
		else if (result_path_dist <= 20) // 15, 20
		{
			return ((result_path_dist - 10) % 5) == 0;
		}
		else if (result_path_dist <= 60) // 28, 36, 44, 52, 60
		{
			return ((result_path_dist - 20) % 8) == 0;
		}
		else if (result_path_dist <= 90) // 75, 90
		{
			return ((result_path_dist - 60) % 15) == 0;
		}
		else // 110, 130, 150, etc
		{
			return ((result_path_dist - 90) % 20) == 0;
		}
	}

	private void set_attack_dir(int curX, int curY, int targetX, int targetY)
	{
		int targetDir = get_dir(curX, curY, targetX, targetY);
		if (UnitRes[unit_id].unit_class == UnitConstants.UNIT_CLASS_SHIP)
		{
			int attackDir1 = (targetDir + 2) % InternalConstants.MAX_SPRITE_DIR_TYPE;
			int attackDir2 = (targetDir + 6) % InternalConstants.MAX_SPRITE_DIR_TYPE;

			if ((attackDir1 + 8 - final_dir) % InternalConstants.MAX_SPRITE_DIR_TYPE <=
			    (attackDir2 + 8 - final_dir) % InternalConstants.MAX_SPRITE_DIR_TYPE)
				final_dir = attackDir1;
			else
				final_dir = attackDir2;

			attack_dir = targetDir;
		}
		else
		{
			attack_dir = targetDir;
			set_dir(targetDir);
		}
	}

	//====== functions for attacking between UnitConstants.UNIT_LAND, UnitConstants.UNIT_SEA, UnitConstants.UNIT_AIR
	private int move_try_to_range_attack(Unit targetUnit)
	{
		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();
		int targetXLoc = targetUnit.next_x_loc();
		int targetYLoc = targetUnit.next_y_loc();

		if (World.get_loc(curXLoc, curYLoc).region_id == World.get_loc(targetXLoc, targetYLoc).region_id)
		{
			//------------ for same region id, search now ---------------//
			if (search(targetXLoc, targetYLoc, 1, SeekPath.SEARCH_MODE_TO_ATTACK, action_para) != 0)
				return 1;
			else // search failure,
			{
				stop2(UnitConstants.KEEP_DEFENSE_MODE);
				return 0;
			}
		}
		else
		{
			//--------------- different territory ------------------//
			int targetWidth = targetUnit.sprite_info.loc_width;
			int targetHeight = targetUnit.sprite_info.loc_height;
			int maxRange = max_attack_range();

			if (possible_place_for_range_attack(targetXLoc, targetYLoc, targetWidth, targetHeight, maxRange))
			{
				//---------------------------------------------------------------------------------//
				// space is found, attack target now
				//---------------------------------------------------------------------------------//
				if (move_to_range_attack(targetXLoc, targetYLoc,
					    targetUnit.sprite_id, SeekPath.SEARCH_MODE_ATTACK_UNIT_BY_RANGE, maxRange) != 0)
					return 1;
				else
				{
					stop2(UnitConstants.KEEP_DEFENSE_MODE);
					return 0;
				}

				return 1;
			}
			else
			{
				//---------------------------------------------------------------------------------//
				// unable to find location to attack the target, stop or move to the target
				//---------------------------------------------------------------------------------//
				if (action_mode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
				    action_mode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
				    action_mode2 != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
					move_to(targetXLoc, targetYLoc, 1); // abort attacking, just call move_to() instead
				else
					stop2(UnitConstants.KEEP_DEFENSE_MODE);
				return 0;
			}
		}

		return 0;
	}

	//private void move_to_range_attack(int targetXLoc, int targetYLoc, short miscNo, short searchMode, short maxRange); //---defined above
	private bool can_attack_different_target_type()
	{
		int maxRange = max_attack_range();
		if (mobile_type == UnitConstants.UNIT_LAND && maxRange == 0)
			return false; // unable to do range attack or cannot attack

		if (maxRange > 1)
			return true;
		else
			return false;
	}

	private bool possible_place_for_range_attack(int targetXLoc, int targetYLoc, int targetWidth, int targetHeight,
		int maxRange)
	{
		if (mobile_type == UnitConstants.UNIT_AIR)
			return true; // air unit can reach any region

		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();

		if (Math.Abs(curXLoc - targetXLoc) <= maxRange &&
		    Math.Abs(curYLoc - targetYLoc) <= maxRange) // inside the attack range
			return true;

		//----------------- init parameters -----------------//
		Location loc = World.get_loc(curXLoc, curYLoc);
		int regionId = loc.region_id;
		int xLoc1 = Math.Max(targetXLoc - maxRange, 0);
		int yLoc1 = Math.Max(targetYLoc - maxRange, 0);
		int xLoc2 = Math.Min(targetXLoc + targetWidth - 1 + maxRange, GameConstants.MapSize - 1);
		int yLoc2 = Math.Min(targetYLoc + targetHeight - 1 + maxRange, GameConstants.MapSize - 1);
		int checkXLoc, checkYLoc;

		//--------- do adjustment for UnitConstants.UNIT_SEA and UnitConstants.UNIT_AIR ---------//
		if (mobile_type != UnitConstants.UNIT_LAND)
		{
			if (xLoc1 % 2 != 0)
				xLoc1++;
			if (yLoc1 % 2 != 0)
				yLoc1++;
			if (xLoc2 % 2 != 0)
				xLoc2--;
			if (yLoc2 % 2 != 0)
				yLoc2--;
		}

		//-------- checking for surrounding location ----------//
		switch (mobile_type)
		{
			case UnitConstants.UNIT_LAND:
				for (checkXLoc = xLoc1; checkXLoc <= xLoc2; checkXLoc++)
				{
					loc = World.get_loc(checkXLoc, yLoc1);
					if (loc.region_id == regionId && loc.is_accessible(mobile_type))
						return true;

					loc = World.get_loc(checkXLoc, yLoc2);
					if (loc.region_id == regionId && loc.is_accessible(mobile_type))
						return true;
				}

				for (checkYLoc = yLoc1 + 1; checkYLoc < yLoc2; checkYLoc++)
				{
					loc = World.get_loc(xLoc1, checkYLoc);
					if (loc.region_id == regionId && loc.is_accessible(mobile_type))
						return true;

					loc = World.get_loc(xLoc2, checkYLoc);
					if (loc.region_id == regionId && loc.is_accessible(mobile_type))
						return true;
				}

				break;

			case UnitConstants.UNIT_SEA:
				for (checkXLoc = xLoc1; checkXLoc <= xLoc2; checkXLoc++)
				{
					if (checkXLoc % 2 == 0 && yLoc1 % 2 == 0)
					{
						loc = World.get_loc(checkXLoc, yLoc1);
						if (loc.region_id == regionId && loc.is_accessible(mobile_type))
							return true;
					}

					if (checkXLoc % 2 == 0 && yLoc2 % 2 == 0)
					{
						loc = World.get_loc(checkXLoc, yLoc2);
						if (loc.region_id == regionId && loc.is_accessible(mobile_type))
							return true;
					}
				}

				for (checkYLoc = yLoc1 + 1; checkYLoc < yLoc2; checkYLoc++)
				{
					if (xLoc1 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.get_loc(xLoc1, checkYLoc);
						if (loc.region_id == regionId && loc.is_accessible(mobile_type))
							return true;
					}

					if (xLoc2 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.get_loc(xLoc2, checkYLoc);
						if (loc.region_id == regionId && loc.is_accessible(mobile_type))
							return true;
					}
				}

				break;

			case UnitConstants.UNIT_AIR:
				for (checkXLoc = xLoc1; checkXLoc <= xLoc2; checkXLoc++)
				{
					if (checkXLoc % 2 == 0 && yLoc1 % 2 == 0)
					{
						loc = World.get_loc(checkXLoc, yLoc1);
						if (loc.is_accessible(mobile_type))
							return true;
					}

					if (checkXLoc % 2 == 0 && yLoc2 % 2 == 0)
					{
						loc = World.get_loc(checkXLoc, yLoc2);
						if (loc.is_accessible(mobile_type))
							return true;
					}
				}

				for (checkYLoc = yLoc1 + 1; checkYLoc < yLoc2; checkYLoc++)
				{
					if (xLoc1 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.get_loc(xLoc1, checkYLoc);
						if (loc.is_accessible(mobile_type))
							return true;
					}

					if (xLoc2 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.get_loc(xLoc2, checkYLoc);
						if (loc.is_accessible(mobile_type))
							return true;
					}
				}

				break;

			default:
				break;
		}

		return false;
	}

	//====== functions for reactivating idle units and blocked units that are ordered to attack
	private bool space_for_attack(int targetXLoc, int targetYLoc, int targetMobileType,
		int targetWidth, int targetHeight)
	{
		if (mobile_type == UnitConstants.UNIT_LAND && targetMobileType == UnitConstants.UNIT_LAND)
			return space_around_target(targetXLoc, targetYLoc, targetWidth, targetHeight);

		if ((mobile_type == UnitConstants.UNIT_SEA && targetMobileType == UnitConstants.UNIT_SEA) ||
		    (mobile_type == UnitConstants.UNIT_AIR && targetMobileType == UnitConstants.UNIT_AIR))
			return space_around_target_ver2(targetXLoc, targetYLoc, targetWidth, targetHeight);

		//-------------------------------------------------------------------------//
		// mobile_type is differet from that of target unit
		//-------------------------------------------------------------------------//
		Location loc = World.get_loc(next_x_loc(), next_y_loc());
		if (mobile_type == UnitConstants.UNIT_LAND && targetMobileType == UnitConstants.UNIT_SEA &&
		    !can_attack_different_target_type() && ship_surr_has_free_land(targetXLoc, targetYLoc, loc.region_id))
			return true;

		int maxRange = max_attack_range();
		if (maxRange == 1)
			return false;

		if (free_space_for_range_attack(targetXLoc, targetYLoc, targetWidth, targetHeight, targetMobileType, maxRange))
			return true;

		return false;
	}

	private bool space_around_target(int squareXLoc, int squareYLoc, int width, int height)
	{
		//				edge 1
		//				1 1 4
		// edge 2	2 x 4		edge 4
		//				2 3 3
		//				edge3

		Location loc;
		Unit unit;
		byte sum, locWeight;
		int testXLoc, testYLoc, i, equal = 1;

		//------------------ top edge ---------------//
		sum = 0;
		if ((testYLoc = squareYLoc - 1) >= 0)
		{
			if (squareXLoc >= 1) // have upper left corner
			{
				i = -1;
				locWeight = 1;
			}
			else
			{
				i = 0;
				locWeight = 2;
			}

			for (; i < width; i++, locWeight <<= 1)
			{
				loc = World.get_loc(squareXLoc + i, testYLoc);
				if (loc.can_move(mobile_type))
					sum ^= locWeight;
				else if (loc.has_unit(mobile_type))
				{
					unit = UnitArray[loc.unit_recno(mobile_type)];
					if (unit.cur_action != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (blocked_edge[0] != sum)
		{
			blocked_edge[0] = sum;
			equal = 0;
		}

		//----------------- left edge -----------------//
		sum = 0;
		if ((testXLoc = squareXLoc - 1) >= 0)
		{
			if (squareYLoc + height <= GameConstants.MapSize - 1) // have lower left corner
			{
				i = height;
				locWeight = 1;
			}
			else
			{
				i = height - 1;
				locWeight = 2;
			}

			for (; i >= 0; i--, locWeight <<= 1)
			{
				loc = World.get_loc(testXLoc, squareYLoc + i);
				if (loc.can_move(mobile_type))
					sum ^= locWeight;
				else if (loc.has_unit(mobile_type))
				{
					unit = UnitArray[loc.unit_recno(mobile_type)];
					if (unit.cur_action != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (blocked_edge[1] != sum)
		{
			blocked_edge[1] = sum;
			equal = 0;
		}

		//------------------- bottom edge ------------------//
		sum = 0;
		if ((testYLoc = squareYLoc + height) <= GameConstants.MapSize - 1)
		{
			if (squareXLoc + width <= GameConstants.MapSize - 1) // have lower right corner
			{
				i = width;
				locWeight = 1;
			}
			else
			{
				i = width - 1;
				locWeight = 2;
			}

			for (; i >= 0; i--, locWeight <<= 1)
			{
				loc = World.get_loc(squareXLoc + i, testYLoc);
				if (loc.can_move(mobile_type))
					sum ^= locWeight;
				else if (loc.has_unit(mobile_type))
				{
					unit = UnitArray[loc.unit_recno(mobile_type)];
					if (unit.cur_action != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (blocked_edge[2] != sum)
		{
			blocked_edge[2] = sum;
			equal = 0;
		}

		//---------------------- right edge ----------------------//
		sum = 0;
		if ((testXLoc = squareXLoc + width) <= GameConstants.MapSize - 1)
		{
			if (squareYLoc >= 1) // have upper right corner
			{
				i = -1;
				locWeight = 1;
			}
			else
			{
				i = 0;
				locWeight = 2;
			}

			for (; i < height; i++, locWeight <<= 1)
			{
				loc = World.get_loc(testXLoc, squareYLoc + i);
				if (loc.can_move(mobile_type))
					sum ^= locWeight;
				else if (loc.has_unit(mobile_type))
				{
					unit = UnitArray[loc.unit_recno(mobile_type)];
					if (unit.cur_action != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (blocked_edge[3] != sum)
		{
			blocked_edge[3] = sum;
			equal = 0;
		}

		return equal == 0;
	}

	private bool space_around_target_ver2(int targetXLoc, int targetYLoc, int targetWidth, int targetHeight)
	{
		Location loc;
		Unit unit;
		byte sum, locWeight;
		int xLoc1, yLoc1, xLoc2, yLoc2;
		int i, equal = 1;
		//int testXLoc, testYLoc, 

		xLoc1 = (targetXLoc % 2 != 0) ? targetXLoc - 1 : targetXLoc - 2;
		yLoc1 = (targetYLoc % 2 != 0) ? targetYLoc - 1 : targetYLoc - 2;
		xLoc2 = ((targetXLoc + targetWidth - 1) % 2 != 0) ? targetXLoc + targetWidth : targetXLoc + targetWidth + 1;
		yLoc2 = ((targetYLoc + targetHeight - 1) % 2 != 0) ? targetYLoc + targetHeight : targetYLoc + targetHeight + 1;

		//------------------------ top edge ------------------------//
		sum = 0;
		if (yLoc1 >= 0)
		{
			if (xLoc1 >= 0)
			{
				i = xLoc1;
				locWeight = 1;
			}
			else
			{
				i = xLoc1 + 2;
				locWeight = 2;
			}

			for (; i <= xLoc2; i += 2, locWeight <<= 1)
			{
				loc = World.get_loc(i, yLoc1);
				if (loc.can_move(mobile_type))
					sum ^= locWeight;
				else if (loc.has_unit(mobile_type))
				{
					unit = UnitArray[loc.unit_recno(mobile_type)];
					if (unit.cur_action != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (blocked_edge[0] != sum)
		{
			blocked_edge[0] = sum;
			equal = 0;
		}

		//---------------------- left edge -----------------------//
		sum = 0;
		if (xLoc1 >= 0)
		{
			if (yLoc2 <= GameConstants.MapSize - 1)
			{
				i = yLoc2;
				locWeight = 1;
			}
			else
			{
				i = yLoc2 - 2;
				locWeight = 2;
			}

			for (; i > yLoc1; i -= 2, locWeight <<= 1)
			{
				loc = World.get_loc(xLoc1, i);
				if (loc.can_move(mobile_type))
					sum ^= locWeight;
				else if (loc.has_unit(mobile_type))
				{
					unit = UnitArray[loc.unit_recno(mobile_type)];
					if (unit.cur_action != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (blocked_edge[1] != sum)
		{
			blocked_edge[1] = sum;
			equal = 0;
		}

		//----------------------- bottom edge ---------------------------//
		sum = 0;
		if (yLoc2 <= GameConstants.MapSize - 1)
		{
			if (xLoc2 <= GameConstants.MapSize - 1)
			{
				i = xLoc2;
				locWeight = 1;
			}
			else
			{
				i = xLoc2 - 2;
				locWeight = 2;
			}

			for (; i > xLoc1; i -= 2, locWeight <<= 1)
			{
				loc = World.get_loc(i, yLoc2);
				if (loc.can_move(mobile_type))
					sum ^= locWeight;
				else if (loc.has_unit(mobile_type))
				{
					unit = UnitArray[loc.unit_recno(mobile_type)];
					if (unit.cur_action != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (blocked_edge[2] != sum)
		{
			blocked_edge[2] = sum;
			equal = 0;
		}

		//---------------------- right edge ------------------------//
		sum = 0;
		if (xLoc2 <= GameConstants.MapSize - 1)
		{
			if (yLoc1 >= 0)
			{
				i = yLoc1;
				locWeight = 1;
			}
			else
			{
				i = yLoc1 + 2;
				locWeight = 2;
			}

			for (; i < yLoc2; i += 2, locWeight <<= 1)
			{
				loc = World.get_loc(xLoc2, i);
				if (loc.can_move(mobile_type))
					sum ^= locWeight;
				else if (loc.has_unit(mobile_type))
				{
					unit = UnitArray[loc.unit_recno(mobile_type)];
					if (unit.cur_action != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (blocked_edge[3] != sum)
		{
			blocked_edge[3] = sum;
			equal = 0;
		}

		return equal == 0;
	}

	private bool ship_surr_has_free_land(int targetXLoc, int targetYLoc, int regionId)
	{
		Location loc;
		int xShift, yShift, checkXLoc, checkYLoc;

		for (int i = 2; i < 9; i++)
		{
			Misc.cal_move_around_a_point(i, 3, 3, out xShift, out yShift);
			checkXLoc = targetXLoc + xShift;
			checkYLoc = targetYLoc + yShift;

			if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
				continue;

			loc = World.get_loc(checkXLoc, checkYLoc);
			if (loc.region_id == regionId && loc.can_move(mobile_type))
				return true;
		}

		return false;
	}

	private bool free_space_for_range_attack(int targetXLoc, int targetYLoc, int targetWidth, int targetHeight,
		int targetMobileType, int maxRange)
	{
		//if(mobile_type==UnitConstants.UNIT_AIR)
		//	return true; // air unit can reach any region

		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();

		if (Math.Abs(curXLoc - targetXLoc) <= maxRange &&
		    Math.Abs(curYLoc - targetYLoc) <= maxRange) // inside the attack range
			return true;

		Location loc = World.get_loc(curXLoc, curYLoc);
		int regionId = loc.region_id;
		int xLoc1 = Math.Max(targetXLoc - maxRange, 0);
		int yLoc1 = Math.Max(targetYLoc - maxRange, 0);
		int xLoc2 = Math.Min(targetXLoc + targetWidth - 1 + maxRange, GameConstants.MapSize - 1);
		int yLoc2 = Math.Min(targetYLoc + targetHeight - 1 + maxRange, GameConstants.MapSize - 1);
		int checkXLoc, checkYLoc;

		//--------- do adjustment for UnitConstants.UNIT_SEA and UnitConstants.UNIT_AIR ---------//
		if (mobile_type != UnitConstants.UNIT_LAND)
		{
			if (xLoc1 % 2 != 0)
				xLoc1++;
			if (yLoc1 % 2 != 0)
				yLoc1++;
			if (xLoc2 % 2 != 0)
				xLoc2--;
			if (yLoc2 % 2 != 0)
				yLoc2--;
		}

		//-------- checking for surrounding location ----------//
		switch (mobile_type)
		{
			case UnitConstants.UNIT_LAND:
				for (checkXLoc = xLoc1; checkXLoc <= xLoc2; checkXLoc++)
				{
					loc = World.get_loc(checkXLoc, yLoc1);
					if (loc.region_id == regionId && loc.can_move(mobile_type))
						return true;

					loc = World.get_loc(checkXLoc, yLoc2);
					if (loc.region_id == regionId && loc.can_move(mobile_type))
						return true;
				}

				for (checkYLoc = yLoc1 + 1; checkYLoc < yLoc2; checkYLoc++)
				{
					loc = World.get_loc(xLoc1, checkYLoc);
					if (loc.region_id == regionId && loc.can_move(mobile_type))
						return true;

					loc = World.get_loc(xLoc2, checkYLoc);
					if (loc.region_id == regionId && loc.can_move(mobile_type))
						return true;
				}

				break;

			case UnitConstants.UNIT_SEA:
				for (checkXLoc = xLoc1; checkXLoc <= xLoc2; checkXLoc++)
				{
					if (checkXLoc % 2 == 0 && yLoc1 % 2 == 0)
					{
						loc = World.get_loc(checkXLoc, yLoc1);
						if (loc.region_id == regionId && loc.can_move(mobile_type))
							return true;
					}

					if (checkXLoc % 2 == 0 && yLoc2 % 2 == 0)
					{
						loc = World.get_loc(checkXLoc, yLoc2);
						if (loc.region_id == regionId && loc.can_move(mobile_type))
							return true;
					}
				}

				for (checkYLoc = yLoc1 + 1; checkYLoc < yLoc2; checkYLoc++)
				{
					if (xLoc1 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.get_loc(xLoc1, checkYLoc);
						if (loc.region_id == regionId && loc.can_move(mobile_type))
							return true;
					}

					if (xLoc2 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.get_loc(xLoc2, checkYLoc);
						if (loc.region_id == regionId && loc.can_move(mobile_type))
							return true;
					}
				}

				break;

			case UnitConstants.UNIT_AIR:
				for (checkXLoc = xLoc1; checkXLoc <= xLoc2; checkXLoc++)
				{
					if (checkXLoc % 2 == 0 && yLoc1 % 2 == 0)
					{
						loc = World.get_loc(checkXLoc, yLoc1);
						if (loc.can_move(mobile_type))
							return true;
					}

					if (checkXLoc % 2 == 0 && yLoc2 % 2 == 0)
					{
						loc = World.get_loc(checkXLoc, yLoc2);
						if (loc.can_move(mobile_type))
							return true;
					}
				}

				for (checkYLoc = yLoc1 + 1; checkYLoc < yLoc2; checkYLoc++)
				{
					if (xLoc1 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.get_loc(xLoc1, checkYLoc);
						if (loc.can_move(mobile_type))
							return true;
					}

					if (xLoc2 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.get_loc(xLoc2, checkYLoc);
						if (loc.can_move(mobile_type))
							return true;
					}
				}

				break;

			default:
				break;
		}

		return false;
	}

	private void choose_best_attack_mode(int attackDistance, int targetMobileType = UnitConstants.UNIT_LAND)
	{
		//------------ enable/disable range attack -----------//
		//cur_attack = 0;
		//return;

		//-------------------- define parameters -----------------------//
		int attackModeBeingUsed = cur_attack;
		//UCHAR maxAttackRangeMode = 0;
		int maxAttackRangeMode = cur_attack;
		AttackInfo attackInfoMaxRange = attack_info_array[0];
		AttackInfo attackInfoChecking;
		AttackInfo attackInfoSelected = attack_info_array[cur_attack];

		//--------------------------------------------------------------//
		// If targetMobileType==UnitConstants.UNIT_AIR or mobile_type==UnitConstants.UNIT_AIR,
		//	force to use range_attack.
		// If there is no range_attack, return 0, i.e. cur_attack=0
		//--------------------------------------------------------------//
		if (attack_count > 1)
		{
			bool canAttack = false;
			int checkingDamageWeight, selectedDamageWeight;

			for (int i = 0; i < attack_count; i++)
			{
				if (attackModeBeingUsed == i)
					continue; // it is the mode already used

				attackInfoChecking = attack_info_array[i];
				if (can_attack_with(attackInfoChecking) && attackInfoChecking.attack_range >= attackDistance)
				{
					//-------------------- able to attack ----------------------//
					canAttack = true;

					if (attackInfoSelected.attack_range < attackDistance)
					{
						attackModeBeingUsed = i;
						attackInfoSelected = attackInfoChecking;
						continue;
					}

					checkingDamageWeight = attackInfoChecking.attack_damage;
					selectedDamageWeight = attackInfoSelected.attack_damage;

					if (attackDistance == 1 &&
					    (targetMobileType != UnitConstants.UNIT_AIR && mobile_type != UnitConstants.UNIT_AIR))
					{
						//------------ force to use close attack if possible -----------//
						if (attackInfoSelected.attack_range == attackDistance)
						{
							if (attackInfoChecking.attack_range == attackDistance &&
							    checkingDamageWeight > selectedDamageWeight)
							{
								attackModeBeingUsed = i; // choose the one with strongest damage
								attackInfoSelected = attackInfoChecking;
							}

							continue;
						}
						else if (attackInfoChecking.attack_range == 1)
						{
							attackModeBeingUsed = i;
							attackInfoSelected = attackInfoChecking;
							continue;
						}
					}

					//----------------------------------------------------------------------//
					// further selection
					//----------------------------------------------------------------------//
					if (checkingDamageWeight == selectedDamageWeight)
					{
						if (attackInfoChecking.attack_range < attackInfoSelected.attack_range)
						{
							if (attackInfoChecking.attack_range > 1 ||
							    (targetMobileType != UnitConstants.UNIT_AIR && mobile_type != UnitConstants.UNIT_AIR))
							{
								//--------------------------------------------------------------------------//
								// select one with shortest attack_range
								//--------------------------------------------------------------------------//
								attackModeBeingUsed = i;
								attackInfoSelected = attackInfoChecking;
							}
						}
					}
					else
					{
						//--------------------------------------------------------------------------//
						// select one that can do the attacking immediately with the strongest damage point
						//--------------------------------------------------------------------------//
						attackModeBeingUsed = i;
						attackInfoSelected = attackInfoChecking;
					}
				}

				if (!canAttack)
				{
					//------------------------------------------------------------------------------//
					// if unable to attack the target, choose the mode with longer attack_range and
					// heavier damage
					//------------------------------------------------------------------------------//
					if (can_attack_with(attackInfoChecking) &&
					    (attackInfoChecking.attack_range > attackInfoMaxRange.attack_range ||
					     (attackInfoChecking.attack_range == attackInfoMaxRange.attack_range &&
					      attackInfoChecking.attack_damage > attackInfoMaxRange.attack_damage)))
					{
						maxAttackRangeMode = i;
						attackInfoMaxRange = attackInfoChecking;
					}
				}
			}

			if (canAttack)
				cur_attack = attackModeBeingUsed; // choose the strongest damage mode if able to attack
			else
				cur_attack = maxAttackRangeMode; //	choose the longest attack range if unable to attack

			attack_range = attack_info_array[cur_attack].attack_range;
		}
		else
		{
			cur_attack = 0; // only one mode is supported
			attack_range = attack_info_array[0].attack_range;
			return;
		}
	}

	private void unit_auto_guarding(Unit attackUnit)
	{
		if (force_move_flag)
			return;

		//---------------------------------------//
		//
		// If the aggressive_mode is off, then don't
		// fight back when the unit is moving, only
		// fight back when the unit is already fighting
		// or is idle.
		//
		//---------------------------------------//

		if (aggressive_mode == 0 && cur_action != SPRITE_ATTACK && cur_action != SPRITE_IDLE)
		{
			return;
		}

		//--------------------------------------------------------------------//
		// decide attack or not
		//--------------------------------------------------------------------//

		int changeToAttack = 0;
		if (cur_action == SPRITE_ATTACK || (sprite_info.need_turning != 0 && cur_action == SPRITE_TURN &&
		                                    (Math.Abs(next_x_loc() - action_x_loc) < attack_range ||
		                                     Math.Abs(next_y_loc() - action_y_loc) < attack_range)))
		{
			if (action_mode != UnitConstants.ACTION_ATTACK_UNIT)
			{
				changeToAttack++; //else continue to attack the target unit
			}
			else
			{
				if (action_para == 0 || UnitArray.IsDeleted(action_para))
					changeToAttack++; // attack new target
			}
		}
		else if
			(cur_action != SPRITE_DIE) // && abs(cur_x-next_x)<spriteInfo.speed && abs(cur_y-next_y)<spriteInfo.speed)
		{
			changeToAttack++;
			/*if(!ai_unit) // player unit
			{
				if(action_mode!=ACTION_ATTACK_UNIT)
					changeToAttack++;  //else continue to attack the target unit
				else
				{
					err_when(!action_para);
					if(UnitArray.IsDeleted(action_para))
						changeToAttack++; // attack new target
				}
			}
			else
				changeToAttack++;*/
		}

		if (changeToAttack == 0)
		{
			if (ai_unit) // allow ai unit to select target to attack
			{
				//------------------------------------------------------------//
				// conditions to let the unit escape
				//------------------------------------------------------------//
				//-************* codes here ************-//

				//------------------------------------------------------------//
				// select the weaker target to attack first, if more than one
				// unit attack this unit
				//------------------------------------------------------------//
				int attackXLoc = attackUnit.next_x_loc();
				int attackYLoc = attackUnit.next_y_loc();

				int attackDistance = cal_distance(attackXLoc, attackYLoc,
					attackUnit.sprite_info.loc_width, attackUnit.sprite_info.loc_height);
				if (attackDistance == 1) // only consider close attack
				{
					Unit targetUnit = UnitArray[action_para];
					if (targetUnit.hit_points > attackUnit.hit_points) // the new attacker is weaker
						attack_unit(attackUnit.sprite_recno, 0, 0, true);
				}
			}

			return;
		}

		//--------------------------------------------------------------------//
		// cancel AI actions
		//--------------------------------------------------------------------//
		if (ai_action_id != 0 && nation_recno != 0)
			NationArray[nation_recno].action_failure(ai_action_id, sprite_recno);

		if (!attackUnit.is_visible())
			return;

		//--------------------------------------------------------------------------------//
		// checking for ship processing trading
		//--------------------------------------------------------------------------------//
		if (sprite_info.sprite_sub_type == 'M') //**** BUGHERE, is sprite_sub_type really representing UNIT_MARINE???
		{
			UnitInfo unitInfo = UnitRes[unit_id];
			if (unitInfo.carry_goods_capacity != 0)
			{
				UnitMarine ship = (UnitMarine)this;
				if (ship.auto_mode != 0 && ship.stop_defined_num > 1)
				{
					int targetXLoc = attackUnit.next_x_loc();
					int targetYLoc = attackUnit.next_y_loc();
					SpriteInfo targetSpriteInfo = attackUnit.sprite_info;
					int attackDistance = cal_distance(targetXLoc, targetYLoc,
						targetSpriteInfo.loc_width, targetSpriteInfo.loc_height);
					int maxAttackRange = max_attack_range();
					if (maxAttackRange < attackDistance)
						return; // can't attack the target
				}
			}
		}

		switch (action_mode2)
		{
			case UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET:
			case UnitConstants.ACTION_AUTO_DEFENSE_BACK_CAMP:
				action_mode2 = UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET;
				break;

			case UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET:
			case UnitConstants.ACTION_DEFEND_TOWN_BACK_TOWN:
				action_mode2 = UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET;
				break;

			case UnitConstants.ACTION_MONSTER_DEFEND_DETECT_TARGET:
			case UnitConstants.ACTION_MONSTER_DEFEND_BACK_FIRM:
				action_mode2 = UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET;
				break;
		}

		save_original_action();

		//----------------------------------------------------------//
		// set the original location of the attacking target when
		// the attack() function is called, action_x_loc2 & action_y_loc2
		// will change when the unit move, but these two will not.
		//----------------------------------------------------------//

		original_target_x_loc = attackUnit.next_x_loc();
		original_target_y_loc = attackUnit.next_y_loc();

		if (!UnitArray.IsDeleted(attackUnit.sprite_recno))
			attack_unit(attackUnit.sprite_recno, 0, 0, true);
	}

	private void set_unreachable_location(int xLoc, int yLoc)
	{
	}

	private void check_self_surround()
	{
	}

	private bool can_attack_with(int i) // 0 to attack_count-1
	{
		AttackInfo attackInfo = attack_info_array[i];
		return skill.combat_level >= attackInfo.combat_level && cur_power >= attackInfo.min_power;
	}

	private bool can_attack_with(AttackInfo attackInfo)
	{
		return skill.combat_level >= attackInfo.combat_level && cur_power >= attackInfo.min_power;
	}

	private void get_hit_x_y(out int x, out int y)
	{
		switch (cur_dir)
		{
			case 0: // north
				x = cur_x;
				y = cur_y - InternalConstants.CellHeight;
				break;
			case 1: // north east
				x = cur_x + InternalConstants.CellWidth;
				y = cur_y - InternalConstants.CellHeight;
				break;
			case 2: // east
				x = cur_x + InternalConstants.CellWidth;
				y = cur_y;
				break;
			case 3: // south east
				x = cur_x + InternalConstants.CellWidth;
				y = cur_y + InternalConstants.CellHeight;
				break;
			case 4: // south
				x = cur_x;
				y = cur_y + InternalConstants.CellHeight;
				break;
			case 5: // south west
				x = cur_x - InternalConstants.CellWidth;
				y = cur_y + InternalConstants.CellHeight;
				break;
			case 6: // west
				x = cur_x - InternalConstants.CellWidth;
				y = cur_y;
				break;
			case 7: // north west
				x = cur_x - InternalConstants.CellWidth;
				y = cur_y - InternalConstants.CellHeight;
				break;
			default:
				x = cur_x;
				y = cur_y;
				break;
		}
	}

	private void add_close_attack_effect()
	{
		int effectId = attack_info_array[cur_attack].effect_id;
		if (effectId != 0)
		{
			int x, y;
			get_hit_x_y(out x, out y);
			EffectArray.AddEffect(effectId, x, y, SPRITE_IDLE, cur_dir, mobile_type == UnitConstants.UNIT_AIR ? 8 : 2, 0);
		}
	}

	//-----------------  defense actions ---------------------//
	//=========== unit's defend mode generalized functions ============//
	private bool in_any_defense_mode()
	{
		return action_mode2 >= UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
		       action_mode2 <= UnitConstants.ACTION_MONSTER_DEFEND_BACK_FIRM;
	}

	private void general_defend_mode_detect_target(int checkDefendMode = 0)
	{
		stop();
		switch (action_mode2)
		{
			case UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET:
				defense_detect_target();
				break;

			case UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET:
				defend_town_detect_target();
				break;

			case UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET:
				monster_defend_detect_target();
				break;

			default:
				break;
		}
	}

	private bool general_defend_mode_process_attack_target()
	{
		Location loc;
		SpriteInfo spriteInfo;
		FirmInfo firmInfo;
		Unit unit = null;
		Firm firm = null;
		int clearToDetect = 0;

		//------------------------------------------------------------------------------//
		// if the unit's action mode is in defensive attack action, process the corresponding
		// checking.
		//------------------------------------------------------------------------------//
		switch (action_mode)
		{
			case UnitConstants.ACTION_ATTACK_UNIT:
				if (UnitArray.IsDeleted(action_para2))
				{
					clearToDetect++;
				}
				else
				{
					unit = UnitArray[action_para2];

					//if(unit.cur_action==SPRITE_IDLE)
					//	clearToDetect++;

					if (!nation_can_attack(unit.nation_recno)) // cannot attack this nation
						clearToDetect++;
				}

				break;

			case UnitConstants.ACTION_ATTACK_FIRM:
				if (FirmArray.IsDeleted(action_para2))
				{
					clearToDetect++;
				}
				else
				{
					firm = FirmArray[action_para2];

					if (!nation_can_attack(firm.nation_recno)) // cannot attack this nation
						clearToDetect++;
				}

				break;

			case UnitConstants.ACTION_ATTACK_TOWN:
				if (TownArray.IsDeleted(action_para2))
				{
					clearToDetect++;
				}
				else
				{
					Town town = TownArray[action_para2];

					if (!nation_can_attack(town.NationId)) // cannot attack this nation
						clearToDetect++;
				}

				break;

			case UnitConstants.ACTION_ATTACK_WALL:
				loc = World.get_loc(action_x_loc2, action_y_loc2);

				if (!loc.is_wall() || !nation_can_attack(loc.power_nation_recno))
					clearToDetect++;
				break;

			default:
				clearToDetect++;
				break;
		}

		//------------------------------------------------------------------------------//
		// suitation changed to defensive detecting mode
		//------------------------------------------------------------------------------//
		if (clearToDetect != 0)
		{
			//----------------------------------------------------------//
			// target is dead, change to detect state for another target
			//----------------------------------------------------------//
			reset_action_para();
			return true;
		}
		else if (waiting_term < UnitConstants.ATTACK_WAITING_TERM)
			waiting_term++;
		else
		{
			//------------------------------------------------------------------------------//
			// process the corresponding attacking procedure.
			//------------------------------------------------------------------------------//
			waiting_term = 0;
			switch (action_mode)
			{
				case UnitConstants.ACTION_ATTACK_UNIT:
					spriteInfo = unit.sprite_info;

					//-----------------------------------------------------------------//
					// attack the target if able to reach the target surrounding, otherwise
					// continue to wait
					//-----------------------------------------------------------------//
					action_x_loc2 = unit.next_x_loc(); // update target location
					action_y_loc2 = unit.next_y_loc();
					if (space_for_attack(action_x_loc2, action_y_loc2, unit.mobile_type,
						    spriteInfo.loc_width, spriteInfo.loc_height))
						attack_unit(unit.sprite_recno, 0, 0, true);
					break;

				case UnitConstants.ACTION_ATTACK_FIRM:
					firmInfo = FirmRes[firm.firm_id];

					//-----------------------------------------------------------------//
					// attack the target if able to reach the target surrounding, otherwise
					// continue to wait
					//-----------------------------------------------------------------//
					attack_firm(action_x_loc2, action_y_loc2);

					if (!is_in_surrounding(move_to_x_loc, move_to_y_loc, sprite_info.loc_width,
						    action_x_loc2, action_y_loc2, firmInfo.loc_width, firmInfo.loc_height))
						waiting_term = 0;
					break;

				case UnitConstants.ACTION_ATTACK_TOWN:
					//-----------------------------------------------------------------//
					// attack the target if able to reach the target surrounding, otherwise
					// continue to wait
					//-----------------------------------------------------------------//
					attack_town(action_x_loc2, action_y_loc2);

					if (!is_in_surrounding(move_to_x_loc, move_to_y_loc, sprite_info.loc_width,
						    action_x_loc2, action_y_loc2, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT))
						waiting_term = 0;
					break;

				case UnitConstants.ACTION_ATTACK_WALL:
					attack_wall(action_x_loc2, action_y_loc2);
					if (!is_in_surrounding(move_to_x_loc, move_to_y_loc, sprite_info.loc_width,
						    action_x_loc2, action_y_loc2, 1, 1))
						waiting_term = 0;
					break;

				default:
					break;
			}
		}

		return false;
	}

	//========== unit's defense mode ==========//
	private void defense_back_camp(int firmXLoc, int firmYLoc)
	{
		assign(firmXLoc, firmYLoc);
		action_mode2 = UnitConstants.ACTION_AUTO_DEFENSE_BACK_CAMP;
	}

	private void process_auto_defense_attack_target()
	{
		if (general_defend_mode_process_attack_target())
		{
			defense_detect_target();
		}
	}

	private void process_auto_defense_detect_target()
	{
		//----------------------------------------------------------------//
		// no target or target is out of detect range, so change state to
		// back camp
		//----------------------------------------------------------------//
		if (action_para2 == 0)
		{
			if (FirmArray.IsDeleted(action_misc_para))
			{
				process_auto_defense_back_camp();
				return;
			}

			Firm firm = FirmArray[action_misc_para];
			if (firm.firm_id != Firm.FIRM_CAMP || firm.nation_recno != nation_recno)
			{
				process_auto_defense_back_camp();
				return;
			}

			FirmCamp camp = (FirmCamp)firm;
			if (UnitArray.IsDeleted(camp.defend_target_recno))
			{
				process_auto_defense_back_camp();
				return;
			}

			Unit target = UnitArray[camp.defend_target_recno];
			if (target.action_mode != UnitConstants.ACTION_ATTACK_FIRM || target.action_para != camp.firm_recno)
			{
				process_auto_defense_back_camp();
				return;
			}

			//action_mode2 = UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET;
			action_para2 = UnitConstants.AUTO_DEFENSE_DETECT_COUNT;
			return;
		}

		//----------------------------------------------------------------//
		// defense_detecting target algorithm
		//----------------------------------------------------------------//
		int startLoc;
		int dimension;

		switch (action_para2 % InternalConstants.FRAMES_PER_DAY)
		{
			case 3:
				startLoc = 2; // 1-7, check 224 = 15^2-1
				dimension = 7;
				break;

			case 2:
				startLoc = 122; // 6-8, check 168 = 17^2-11^2
				dimension = 8;
				break;

			case 1:
				startLoc = 170; // 7-9, check 192 = 19^2-13^2
				dimension = UnitConstants.EFFECTIVE_AUTO_DEFENSE_DISTANCE;
				break;

			default:
				action_para2--;
				return;
		}

		//---------------------------------------------------------------//
		// attack the target if target detected, or change the detect region
		//---------------------------------------------------------------//
		if (!idle_detect_attack(startLoc, dimension, 1)) // defense mode is on
			action_para2--;
	}

	private void process_auto_defense_back_camp()
	{
		int clearDefenseMode = 0;
		// the unit may become idle or unable to reach firm, reactivate it
		if (action_mode != UnitConstants.ACTION_ASSIGN_TO_FIRM)
		{
			if (action_misc != UnitConstants.ACTION_MISC_DEFENSE_CAMP_RECNO ||
			    action_misc_para == 0 || FirmArray.IsDeleted(action_misc_para))
				clearDefenseMode++;
			else
			{
				Firm firm = FirmArray[action_misc_para];
				if (firm.firm_id != Firm.FIRM_CAMP || firm.nation_recno != nation_recno)
					clearDefenseMode++;
				else
				{
					defense_back_camp(firm.loc_x1, firm.loc_y1); // go back to the military camp
					return;
				}
			}
		}
		else if (cur_action == SPRITE_IDLE)
		{
			if (FirmArray.IsDeleted(action_misc_para))
				clearDefenseMode++;
			else
			{
				Firm firm = FirmArray[action_misc_para];
				defense_back_camp(firm.loc_x1, firm.loc_y1);
				return;
			}
		}

		//----------------------------------------------------------------//
		// clear order if the camp is deleted
		//----------------------------------------------------------------//
		stop2();
		reset_action_misc_para();
	}

	private bool defense_follow_target()
	{
		const int PROB_HOSTILE_RETURN = 10;
		const int PROB_FRIENDLY_RETURN = 20;
		const int PROB_NEUTRAL_RETURN = 30;

		if (UnitArray.IsDeleted(action_para))
			return true;

		if (cur_action == SPRITE_ATTACK)
			return true;

		Unit target = UnitArray[action_para];
		Location loc = World.get_loc(action_x_loc, action_y_loc);
		if (!loc.has_unit(target.mobile_type))
			return true; // the target may be dead or invisible

		int returnFactor;

		//-----------------------------------------------------------------//
		// calculate the chance to go back to military camp in following the
		// target
		//-----------------------------------------------------------------//
		if (loc.power_nation_recno == nation_recno)
			return true; // target within our nation
		else if (loc.power_nation_recno == 0) // is neutral
			returnFactor = PROB_NEUTRAL_RETURN;
		else
		{
			Nation locNation = NationArray[loc.power_nation_recno];
			if (locNation.get_relation_status(nation_recno) == NationBase.NATION_HOSTILE)
				returnFactor = PROB_HOSTILE_RETURN;
			else
				returnFactor = PROB_FRIENDLY_RETURN;
		}

		SpriteInfo targetSpriteInfo = target.sprite_info;

		//-----------------------------------------------------------------//
		// if the target moves faster than this unit, it is more likely for
		// this unit to go back to military camp.
		//-----------------------------------------------------------------//
		//-**** should also consider the combat level and hit_points of both unit ****-//
		if (targetSpriteInfo.speed > sprite_info.speed)
			returnFactor -= 5;

		if (Misc.Random(returnFactor) != 0) // return to camp if true
			return true;

		process_auto_defense_back_camp();
		return false; // cancel attack
	}

	//========== town unit's defend mode ==========//
	private void defend_town_attack_unit(int targetRecno)
	{
		action_mode2 = UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET;
		attack_unit(targetRecno, 0, 0, true);
	}

	private void defend_town_detect_target()
	{
		action_mode2 = UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET;
		action_para2 = UnitConstants.UNIT_DEFEND_TOWN_DETECT_COUNT;
		action_x_loc2 = -1;
		action_y_loc2 = -1;
	}

	private void defend_town_back_town(int townRecno)
	{
		Town town = TownArray[townRecno];

		assign(town.LocX1, town.LocY1);
		action_mode2 = UnitConstants.ACTION_DEFEND_TOWN_BACK_TOWN;
	}

	private void process_defend_town_attack_target()
	{
		if (general_defend_mode_process_attack_target())
		{
			action_mode2 = UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET;
			action_para2 = UnitConstants.UNIT_DEFEND_TOWN_DETECT_COUNT;
			action_x_loc2 = action_y_loc2 = -1;
		}
	}

	private void process_defend_town_detect_target()
	{
		//----------------------------------------------------------------//
		// no target or target is out of detect range, so change state to
		// back camp
		//----------------------------------------------------------------//
		if (action_para2 == 0)
		{
			int back = 0;

			if (TownArray.IsDeleted(action_misc_para))
				back++;
			else
			{
				Town town = TownArray[action_misc_para];
				if (UnitArray.IsDeleted(town.DefendTargetId))
					back++;
				else
				{
					Unit target = UnitArray[town.DefendTargetId];
					if (target.action_mode != UnitConstants.ACTION_ATTACK_TOWN || target.action_para != town.TownId)
						back++;
				}
			}

			if (back == 0)
			{
				//action_mode2 = ACTION_DEFEND_TOWN_DETECT_TARGET;
				action_para2 = UnitConstants.UNIT_DEFEND_TOWN_DETECT_COUNT;
				return;
			}

			process_defend_town_back_town();
			return;
		}

		//----------------------------------------------------------------//
		// defense_detecting target algorithm
		//----------------------------------------------------------------//
		int startLoc;
		int dimension;

		switch (action_para2 % InternalConstants.FRAMES_PER_DAY)
		{
			case 3:
				startLoc = 2; // 1-7, check 224 = 15^2-1
				dimension = 7;
				break;

			case 2:
				startLoc = 122; // 6-8, check 168 = 17^2-11^2
				dimension = 8;
				break;

			case 1:
				startLoc = 170; // 7-9, check 192 = 19^2-13^2
				dimension = UnitConstants.EFFECTIVE_DEFEND_TOWN_DISTANCE;
				break;

			default:
				action_para2--;
				return;
		}

		//---------------------------------------------------------------//
		// attack the target if target detected, or change the detect region
		//---------------------------------------------------------------//

		if (!idle_detect_attack(startLoc, dimension, 1)) // defense mode is on
			action_para2--;
	}

	private void process_defend_town_back_town()
	{
		int clearDefenseMode = 0;
		// the unit may become idle or unable to reach town, reactivate it
		if (action_mode != UnitConstants.ACTION_ASSIGN_TO_TOWN)
		{
			if (action_misc != UnitConstants.ACTION_MISC_DEFEND_TOWN_RECNO ||
			    action_misc_para == 0 || TownArray.IsDeleted(action_misc_para))
				clearDefenseMode++;
			else
			{
				Town town = TownArray[action_misc_para];
				if (town.NationId != nation_recno)
					clearDefenseMode++;
				else
				{
					defend_town_back_town(action_misc_para); // go back to the town
					return;
				}
			}
		}
		else if (cur_action == SPRITE_IDLE)
		{
			if (TownArray.IsDeleted(action_misc_para))
				clearDefenseMode++;
			else
			{
				defend_town_back_town(action_misc_para);
				return;
			}
		}

		//----------------------------------------------------------------//
		// clear order if the town is deleted
		//----------------------------------------------------------------//
		stop2();
		reset_action_misc_para();
	}

	private bool defend_town_follow_target()
	{
		if (cur_action == SPRITE_ATTACK)
			return true;

		if (TownArray.IsDeleted(unit_mode_para))
		{
			stop2(); //**** BUGHERE
			set_mode(0); //***BUGHERE
			return false;
		}

		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();

		Town town = TownArray[unit_mode_para];
		if ((curXLoc < town.LocCenterX - UnitConstants.UNIT_DEFEND_TOWN_DISTANCE) ||
		    (curXLoc > town.LocCenterX + UnitConstants.UNIT_DEFEND_TOWN_DISTANCE) ||
		    (curYLoc < town.LocCenterY - UnitConstants.UNIT_DEFEND_TOWN_DISTANCE) ||
		    (curYLoc > town.LocCenterY + UnitConstants.UNIT_DEFEND_TOWN_DISTANCE))
		{
			defend_town_back_town(unit_mode_para);
			return false;
		}

		return true;
	}

	//========== monster unit's defend mode ==========//
	private void monster_defend_attack_unit(int targetRecno)
	{
		action_mode2 = UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET;
		attack_unit(targetRecno, 0, 0, true);
	}

	private void monster_defend_attack_firm(int targetXLoc, int targetYLoc)
	{
		action_mode2 = UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET;
		attack_firm(targetXLoc, targetYLoc);
	}

	private void monster_defend_attack_town(int targetXLoc, int targetYLoc)
	{
		action_mode2 = UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET;
		attack_town(targetXLoc, targetYLoc);
	}

	private void monster_defend_attack_wall(int targetXLoc, int targetYLoc)
	{
		action_mode2 = UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET;
		attack_wall(targetXLoc, targetYLoc);
	}

	private void monster_defend_detect_target()
	{
		action_mode2 = UnitConstants.ACTION_MONSTER_DEFEND_DETECT_TARGET;
		action_para2 = UnitConstants.MONSTER_DEFEND_DETECT_COUNT;
		action_x_loc2 = -1;
		action_y_loc2 = -1;
	}

	private void monster_defend_back_firm(int firmXLoc, int firmYLoc)
	{
		assign(firmXLoc, firmYLoc);
		action_mode2 = UnitConstants.ACTION_MONSTER_DEFEND_BACK_FIRM;
	}

	private void process_monster_defend_attack_target()
	{
		if (general_defend_mode_process_attack_target())
		{
			monster_defend_detect_target();
		}
	}

	private void process_monster_defend_detect_target()
	{
		//----------------------------------------------------------------//
		// no target or target is out of detect range, so change state to
		// back camp
		//----------------------------------------------------------------//
		if (action_para2 == 0)
		{
			int back = 0;

			if (FirmArray.IsDeleted(action_misc_para))
				back++;
			else
			{
				FirmMonster firmMonster = (FirmMonster)FirmArray[action_misc_para];
				if (UnitArray.IsDeleted(firmMonster.defend_target_recno))
					back++;
				else
				{
					Unit target = UnitArray[firmMonster.defend_target_recno];
					if (target.action_mode != UnitConstants.ACTION_ATTACK_FIRM ||
					    target.action_para != firmMonster.firm_recno)
						back++;
				}
			}

			if (back == 0)
			{
				//action_mode2 = ACTION_MONSTER_DEFEND_DETECT_TARGET;
				action_para2 = UnitConstants.MONSTER_DEFEND_DETECT_COUNT;
				return;
			}

			process_monster_defend_back_firm();
			return;
		}

		//----------------------------------------------------------------//
		// defense_detecting target algorithm
		//----------------------------------------------------------------//
		int startLoc;
		int dimension;

		switch (action_para2 % InternalConstants.FRAMES_PER_DAY)
		{
			case 3:
				startLoc = 2; // 1-7, check 224 = 15^2-1
				dimension = 7;
				break;

			case 2:
				startLoc = 122; // 6-8, check 168 = 17^2-11^2
				dimension = 8;
				break;

			case 1:
				startLoc = 170; // 7-9, check 192 = 19^2-13^2
				dimension = UnitConstants.EFFECTIVE_MONSTER_DEFEND_FIRM_DISTANCE;
				break;

			default:
				action_para2--;
				return;
		}

		//---------------------------------------------------------------//
		// attack the target if target detected, or change the detect region
		//---------------------------------------------------------------//
		if (!idle_detect_attack(startLoc, dimension, 1)) // defense mode is on
			action_para2--;
	}

	private void process_monster_defend_back_firm()
	{
		int clearDefendMode = 0;
		// the unit may become idle or unable to reach firm, reactivate it
		if (action_mode != UnitConstants.ACTION_ASSIGN_TO_FIRM)
		{
			if (action_misc != UnitConstants.ACTION_MISC_MONSTER_DEFEND_FIRM_RECNO ||
			    action_misc_para == 0 || FirmArray.IsDeleted(action_misc_para))
				clearDefendMode++;
			else
			{
				Firm firm = FirmArray[action_misc_para];
				if (firm.firm_id != Firm.FIRM_MONSTER || firm.nation_recno != nation_recno)
					clearDefendMode++;
				else
				{
					monster_defend_back_firm(firm.loc_x1, firm.loc_y1); // go back to the military camp
					return;
				}
			}
		}
		else if (cur_action == SPRITE_IDLE)
		{
			if (FirmArray.IsDeleted(action_misc_para))
				clearDefendMode++;
			else
			{
				Firm firm = FirmArray[action_misc_para];
				monster_defend_back_firm(firm.loc_x1, firm.loc_y1);
				return;
			}
		}

		//----------------------------------------------------------------//
		// clear order if the camp is deleted
		//----------------------------------------------------------------//
		stop2();
		reset_action_misc_para();
	}

	private bool monster_defend_follow_target()
	{
		if (cur_action == SPRITE_ATTACK)
			return true;
		/*
		if(FirmArray.IsDeleted(action_misc_para))
		{
			stop2(); //**** BUGHERE
			//set_mode(0); //***BUGHERE
			if(monster_array.IsDeleted(unit_mode_para))
				return 0;

			Monster *monster = monster_array[unit_mode_para];
			monster.firm_recno = 0;
			return 0;
		}
		*/

		//--------------------------------------------------------------------------------//
		// choose to return to firm
		//--------------------------------------------------------------------------------//
		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();

		Firm firm = FirmArray[action_misc_para];
		if ((curXLoc < firm.center_x - UnitConstants.MONSTER_DEFEND_FIRM_DISTANCE) ||
		    (curXLoc > firm.center_x + UnitConstants.MONSTER_DEFEND_FIRM_DISTANCE) ||
		    (curYLoc < firm.center_y - UnitConstants.MONSTER_DEFEND_FIRM_DISTANCE) ||
		    (curYLoc > firm.center_y + UnitConstants.MONSTER_DEFEND_FIRM_DISTANCE))
		{
			monster_defend_back_firm(firm.loc_x1, firm.loc_y1);
			return false;
		}

		return true;
	}

	//---------- embark to ship and other ship functions ---------//
	private bool ship_to_beach_path_edit(ref int resultXLoc, ref int resultYLoc, int regionId)
	{
		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();
		if (Math.Abs(curXLoc - resultXLoc) <= 1 && Math.Abs(curYLoc - resultYLoc) <= 1)
			return true;

		//--------------- find a path to land area -------------------//
		UnitMarine ship = (UnitMarine)this;
		int result = search(resultXLoc, resultYLoc, 1, SeekPath.SEARCH_MODE_TO_LAND_FOR_SHIP, regionId);
		if (result == 0)
			return true;

		//----------- update cur location --------//
		curXLoc = next_x_loc();
		curYLoc = next_y_loc();

		//------------------------------------------------------------------------------//
		// edit the result path to get a location for embarking
		//------------------------------------------------------------------------------//

		if (result_node_array != null && result_node_count > 0)
		{
			int curNodeIndex = 0;
			int nextNodeIndex = 1;
			ResultNode curNode = result_node_array[curNodeIndex];
			ResultNode nextNode = result_node_array[nextNodeIndex];
			int moveScale = move_step_magn();
			int nodeCount = result_node_count;
			Location loc;
			int i, j, found, pathDist;

			int preXLoc = -1, preYLoc = -1;
			int checkXLoc = curXLoc;
			int checkYLoc = curYLoc;
			int hasMoveStep = 0;
			if (checkXLoc != curNode.node_x || checkYLoc != curNode.node_y)
			{
				hasMoveStep += moveScale;
				checkXLoc = curNode.node_x;
				checkYLoc = curNode.node_y;
			}

			//-----------------------------------------------------------------//
			// find the pair of points that one is in ocean and one in land
			//-----------------------------------------------------------------//
			int xMagn, yMagn, magn;

			for (pathDist = 0, found = 0, i = 1; i < nodeCount; ++i, curNodeIndex++, nextNodeIndex++)
			{
				curNode = result_node_array[curNodeIndex];
				nextNode = result_node_array[nextNodeIndex];
				int vecX = nextNode.node_x - curNode.node_x;
				int vecY = nextNode.node_y - curNode.node_y;
				magn = ((xMagn = Math.Abs(vecX)) > (yMagn = Math.Abs(vecY))) ? xMagn : yMagn;
				if (xMagn != 0)
				{
					vecX /= xMagn;
					vecX *= moveScale;
				}

				if (yMagn != 0)
				{
					vecY /= yMagn;
					vecY *= moveScale;
				}

				//------------- check each location bewteen editNode1 and editNode2 -------------//
				for (j = 0; j < magn; j += moveScale)
				{
					preXLoc = checkXLoc;
					preYLoc = checkYLoc;
					checkXLoc += vecX;
					checkYLoc += vecY;

					loc = World.get_loc(checkXLoc, checkYLoc);
					if (TerrainRes[loc.terrain_id].average_type != TerrainTypeCode.TERRAIN_OCEAN) // found
					{
						found++;
						break;
					}
				}

				if (found != 0)
				{
					//------------ a soln is found ---------------//
					if (j == 0) // end node should be curNode pointed at
					{
						pathDist -= hasMoveStep;
						result_node_count = i;
						result_path_dist = pathDist;
					}
					else
					{
						nextNode.node_x = checkXLoc;
						nextNode.node_y = checkYLoc;

						if (i == 1) // first editing
						{
							ResultNode firstNode = result_node_array[0];
							if (cur_x == firstNode.node_x * InternalConstants.CellWidth &&
							    cur_y == firstNode.node_y * InternalConstants.CellHeight)
							{
								go_x = checkXLoc * InternalConstants.CellWidth;
								go_y = checkYLoc * InternalConstants.CellHeight;
							}
						}

						pathDist += (j + moveScale);
						pathDist -= hasMoveStep;
						result_node_count = i + 1;
						result_path_dist = pathDist;
					}

					move_to_x_loc = preXLoc;
					move_to_y_loc = preYLoc;
					loc = World.get_loc((preXLoc + checkXLoc) / 2, (preYLoc + checkYLoc) / 2);
					if (TerrainRes[loc.terrain_id].average_type != TerrainTypeCode.TERRAIN_OCEAN)
					{
						resultXLoc = (preXLoc + checkXLoc) / 2;
						resultYLoc = (preYLoc + checkYLoc) / 2;
					}
					else
					{
						resultXLoc = checkXLoc;
						resultYLoc = checkYLoc;
					}

					break;
				}
				else
					pathDist += magn;
			}

			if (found == 0)
			{
				ResultNode endNode = result_node_array[result_node_count - 1];
				if (Math.Abs(endNode.node_x - resultXLoc) > 1 || Math.Abs(endNode.node_y - resultYLoc) > 1)
				{
					move_to(resultXLoc, resultYLoc, -1);
					return false;
				}
			}
		}
		else
		{
			//------------- scan for the surrounding for a land location -----------//
			for (int i = 2; i <= 9; ++i)
			{
				int xShift, yShift;
				Misc.cal_move_around_a_point(i, 3, 3, out xShift, out yShift);
				int checkXLoc = curXLoc + xShift;
				int checkYLoc = curYLoc + yShift;
				if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
					continue;

				Location loc = World.get_loc(checkXLoc, checkYLoc);
				if (loc.region_id != regionId)
					continue;

				if (TerrainRes[loc.terrain_id].average_type != TerrainTypeCode.TERRAIN_OCEAN &&
				    loc.can_move(UnitConstants.UNIT_LAND))
				{
					resultXLoc = checkXLoc;
					resultYLoc = checkYLoc;
					return true;
				}
			}

			return false;
		}

		return true;
	}

	private void ship_leave_beach(int shipOldXLoc, int shipOldYLoc)
	{
		//--------------------------------------------------------------------------------//
		// scan for location to leave the beach
		//--------------------------------------------------------------------------------//
		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();
		int xShift, yShift, checkXLoc = -1, checkYLoc = -1, found = 0;

		//------------- find a location to leave the beach ------------//
		for (int i = 2; i <= 9; i++)
		{
			Misc.cal_move_around_a_point(i, 3, 3, out xShift, out yShift);
			checkXLoc = curXLoc + xShift;
			checkYLoc = curYLoc + yShift;

			if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
				continue;

			if (checkXLoc % 2 != 0 || checkYLoc % 2 != 0)
				continue;

			Location loc = World.get_loc(checkXLoc, checkYLoc);
			if (TerrainRes[loc.terrain_id].average_type == TerrainTypeCode.TERRAIN_OCEAN && loc.can_move(mobile_type))
			{
				found++;
				break;
			}
		}

		if (found == 0)
			return; // no suitable location, wait until finding suitable location

		//---------------- leave now --------------------//
		set_dir(shipOldXLoc, shipOldYLoc, checkXLoc, checkYLoc);
		set_ship_extra_move();
		go_x = checkXLoc * InternalConstants.CellWidth;
		go_y = checkYLoc * InternalConstants.CellHeight;
	}

	//---------------- other functions -----------------//
	// calculate distance from this unit(can be 1x1, 2x2) to a known size object
	private int cal_distance(int targetXLoc, int targetYLoc, int targetWidth, int targetHeight)
	{
		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();
		int dispX = 0, dispY = 0;

		if (curXLoc < targetXLoc)
			dispX = (targetXLoc - curXLoc - sprite_info.loc_width) + 1;
		else if ((dispX = curXLoc - targetXLoc - targetWidth + 1) < 0)
			dispX = 0;

		if (curYLoc < targetYLoc)
			dispY = (targetYLoc - curYLoc - sprite_info.loc_height) + 1;
		else if ((dispY = curYLoc - targetYLoc - targetHeight + 1) < 0)
			return dispX;

		return (dispX >= dispY) ? dispX : dispY;
	}

	private bool is_in_surrounding(int checkXLoc, int checkYLoc, int width,
		int targetXLoc, int targetYLoc, int targetWidth, int targetHeight)
	{
		switch (move_step_magn())
		{
			case 1:
				if (checkXLoc >= targetXLoc - width && checkXLoc <= targetXLoc + targetWidth &&
				    checkYLoc >= targetYLoc - width && checkYLoc <= targetYLoc + targetHeight)
					return true;
				break;

			case 2:
				if (checkXLoc >= targetXLoc - width - 1 && checkXLoc <= targetXLoc + targetWidth + 1 &&
				    checkYLoc >= targetYLoc - width - 1 && checkYLoc <= targetYLoc + targetHeight + 1)
					return true;
				break;

			default:
				break;
		}

		return false;
	}

	private void invalidate_attack_target()
	{
		if (action_mode2 == action_mode && action_para2 == action_para2 &&
		    action_x_loc2 == action_x_loc && action_y_loc2 == action_y_loc)
		{
			action_para2 = 0;
		}

		action_para = 0;
	}

	protected void process_attack_unit()
	{
		if (!can_attack())
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//------- if the targeted unit has been destroyed --------//
		if (action_para == 0)
			return;

		//--------------------------------------------------------------------------//
		// stop if the targeted unit has been killed or target belongs our nation
		//--------------------------------------------------------------------------//
		int clearOrder = 0;
		Unit targetUnit = null;

		if (UnitArray.IsDeleted(action_para) || action_para == sprite_recno)
		{
			if (!ConfigAdv.unit_finish_attack_move || cur_action == SPRITE_ATTACK)
				clearOrder++;
			else
			{
				// keep attack action alive to finish movement before going idle
				invalidate_attack_target();
				return;
			}
		}
		else
		{
			targetUnit = UnitArray[action_para];
			if (targetUnit.nation_recno != 0 && !nation_can_attack(targetUnit.nation_recno) &&
			    targetUnit.unit_id != UnitConstants.UNIT_EXPLOSIVE_CART) // cannot attack this nation
				clearOrder++;
		}

		if (clearOrder != 0)
		{
			//------------------------------------------------------------//
			// change to detect target if in defense mode
			//------------------------------------------------------------//
			/*if(action_mode2==UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET || action_mode2==ACTION_DEFEND_TOWN_ATTACK_TARGET ||
				action_mode2==ACTION_MONSTER_DEFEND_ATTACK_TARGET)
			{
				stop2(UnitConstants.KEEP_DEFENSE_MODE);
				err_when((action_misc!=UnitConstants.ACTION_MISC_DEFENSE_CAMP_RECNO && action_misc!=UnitConstants.ACTION_MISC_DEFEND_TOWN_RECNO &&
							 action_misc!=UnitConstants.ACTION_MISC_MONSTER_DEFEND_FIRM_RECNO) || !action_misc_para);
				return;
			}
	
			stop2(); // clear order
			err_when(cur_action==SPRITE_ATTACK && (move_to_x_loc!=next_x_loc() || move_to_y_loc!=next_y_loc()));
			err_when(action_mode==ACTION_ATTACK_UNIT && !action_para);
			err_when(cur_action==SPRITE_ATTACK && action_mode==UnitConstants.ACTION_STOP);
			return;*/

			stop2(UnitConstants.KEEP_DEFENSE_MODE);

			return;
		}

		//--------------------------------------------------------------------------//
		// stop action if target goes into town/firm, ships (go to other territory)
		//--------------------------------------------------------------------------//
		if (!targetUnit.is_visible())
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE); // clear order
			return;
		}

		//------------------------------------------------------------//
		// if the caravan is entered a firm, attack the firm
		//------------------------------------------------------------//
		if (targetUnit.caravan_in_firm())
		{
			//----- for caravan entering the market -------//
			// the current firm recno of the firm the caravan entered is stored in action_para
			if (FirmArray.IsDeleted(targetUnit.action_para))
				stop2(UnitConstants.KEEP_DEFENSE_MODE); // clear order
			else
			{
				Firm firm = FirmArray[targetUnit.action_para];
				attack_firm(firm.loc_x1, firm.loc_y1);
			}

			return;
		}

		//---------------- define parameters ---------------------//
		int targetXLoc = targetUnit.next_x_loc();
		int targetYLoc = targetUnit.next_y_loc();
		int spriteXLoc = next_x_loc();
		int spriteYLoc = next_y_loc();
		AttackInfo attackInfo = attack_info_array[cur_attack];

		//------------------------------------------------------------//
		// If this unit's target has moved, change the destination accordingly.
		//------------------------------------------------------------//
		if (targetXLoc != action_x_loc || targetYLoc != action_y_loc)
		{
			target_move(targetUnit);
			if (action_mode == UnitConstants.ACTION_STOP)
				return;
		}

		//-----------------------------------------------------//
		// If the unit is currently attacking somebody.
		//-----------------------------------------------------//
		//if( cur_action==SPRITE_ATTACK && next_x==cur_x && next_y==cur_y)

		if (Math.Abs(cur_x - next_x) <= sprite_info.speed && Math.Abs(cur_y - next_y) <= sprite_info.speed)
		{
			if (cur_action == SPRITE_ATTACK)
			{
				attack_target(targetUnit);
			}
			else
			{
				//-----------------------------------------------------//
				// If the unit is on its way to attack somebody and it
				// has got close next to the target, attack now
				//-----------------------------------------------------//
				on_way_to_attack(targetUnit);
			}
		}
	}

	protected void process_attack_firm()
	{
		if (!can_attack())
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//------- if the targeted firm has been destroyed --------//
		if (action_para == 0)
			return;

		Firm targetFirm = null;
		int clearOrder = 0;
		//------------------------------------------------------------//
		// check attack conditions
		//------------------------------------------------------------//
		if (FirmArray.IsDeleted(action_para))
		{
			if (!ConfigAdv.unit_finish_attack_move || cur_action == SPRITE_ATTACK)
				clearOrder++;
			else
			{
				// keep attack action alive to finish movement before going idle
				invalidate_attack_target();
				return;
			}
		}
		else
		{
			targetFirm = FirmArray[action_para];

			if (!nation_can_attack(targetFirm.nation_recno)) // cannot attack this nation
				clearOrder++;
		}

		if (clearOrder != 0)
		{
			//------------------------------------------------------------//
			// change to detect target if in defend mode
			//------------------------------------------------------------//
			/*if(action_mode2==UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET || action_mode2==ACTION_DEFEND_TOWN_ATTACK_TARGET ||
				action_mode2==ACTION_MONSTER_DEFEND_ATTACK_TARGET)
			{
				err_when((action_misc!=UnitConstants.ACTION_MISC_DEFENSE_CAMP_RECNO && action_misc!=UnitConstants.ACTION_MISC_DEFEND_TOWN_RECNO &&
							 action_misc!=UnitConstants.ACTION_MISC_MONSTER_DEFEND_FIRM_RECNO) || !action_misc_para);
				stop2(UnitConstants.KEEP_DEFENSE_MODE);
				return;
			}
	
			err_when(action_mode2==ACTION_AUTO_DEFENSE_DETECT_TARGET || action_mode2==ACTION_AUTO_DEFENSE_BACK_CAMP ||
						action_mode2==ACTION_DEFEND_TOWN_DETECT_TARGET || action_mode2==ACTION_DEFEND_TOWN_BACK_TOWN ||
						action_mode2==ACTION_MONSTER_DEFEND_DETECT_TARGET || action_mode2==ACTION_MONSTER_DEFEND_BACK_FIRM);
	
			stop2(); // clear order
			err_when(cur_action==SPRITE_ATTACK && (move_to_x_loc!=next_x_loc() || move_to_y_loc!=next_y_loc()));
			err_when(action_mode==ACTION_ATTACK_FIRM && !action_para);
			err_when(cur_action==SPRITE_ATTACK && action_mode==UnitConstants.ACTION_STOP);
			return;*/

			stop2(UnitConstants.KEEP_DEFENSE_MODE);

			return;
		}

		//-----------------------------------------------------//
		// If the unit is currently attacking somebody.
		//-----------------------------------------------------//
		if (cur_action == SPRITE_ATTACK)
		{
			if (remain_attack_delay != 0)
				return;

			AttackInfo attackInfo = attack_info_array[cur_attack];
			if (attackInfo.attack_range > 1) // range attack
			{
				//--------- wait for bullet emit ----------//
				if (cur_frame != attackInfo.bullet_out_frame)
					return;

				//------- seek location to attack by bullet ----------//
				int curXLoc = next_x_loc();
				int curYLoc = next_y_loc();
				if (!BulletArray.bullet_path_possible(curXLoc, curYLoc, mobile_type,
					    range_attack_x_loc, range_attack_y_loc, UnitConstants.UNIT_LAND,
					    attackInfo.bullet_speed, attackInfo.bullet_sprite_id))
				{
					FirmInfo firmInfo = FirmRes[targetFirm.firm_id];
					if (!BulletArray.add_bullet_possible(curXLoc, curYLoc, mobile_type, action_x_loc, action_y_loc,
						    UnitConstants.UNIT_LAND, firmInfo.loc_width, firmInfo.loc_height,
						    out range_attack_x_loc, out range_attack_y_loc, attackInfo.bullet_speed,
						    attackInfo.bullet_sprite_id))
					{
						//------- no suitable location, so move to target again ---------//
						set_move_to_surround(action_x_loc, action_y_loc, firmInfo.loc_width, firmInfo.loc_height,
							UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO);
						return;
					}
				}

				//--------- add bullet, bullet emits ----------//
				BulletArray.AddBullet(this, action_x_loc, action_y_loc);
				add_close_attack_effect();

				// ------- reduce power --------//
				cur_power -= attackInfo.consume_power;
				if (cur_power < 0) // ***** BUGHERE
					cur_power = 0;
				set_remain_attack_delay();
				return;
			}
			else // close attack
			{
				if (cur_frame != cur_sprite_attack().frame_count)
					return; // is attacking

				hit_firm(this, action_x_loc, action_y_loc, actual_damage(), nation_recno);
				add_close_attack_effect();

				// ------- reduce power --------//
				cur_power -= attackInfo.consume_power;
				if (cur_power < 0) // ***** BUGHERE
					cur_power = 0;
				set_remain_attack_delay();
			}
		}
		//--------------------------------------------------------------------------------------------------//
		// If the unit is on its way to attack somebody, if it has gotten close next to the target, attack now
		//--------------------------------------------------------------------------------------------------//
		// it has moved to the specified location. check cur_x & go_x to make sure the sprite has completely move to the location, not just crossing it.
		else if (Math.Abs(cur_x - next_x) <= sprite_info.speed && Math.Abs(cur_y - next_y) <= sprite_info.speed)
		{
			if (mobile_type == UnitConstants.UNIT_LAND)
			{
				if (detect_surround_target())
					return;
			}

			if (attack_range == 1)
			{
				//------------------------------------------------------------//
				// for close attack, the unit unable to attack the firm if
				// it is not in the firm surrounding
				//------------------------------------------------------------//
				if (result_path_dist > attack_range)
					return;
			}

			FirmInfo firmInfo = FirmRes[targetFirm.firm_id];
			int targetXLoc = targetFirm.loc_x1;
			int targetYLoc = targetFirm.loc_y1;

			int attackDistance = cal_distance(targetXLoc, targetYLoc, firmInfo.loc_width, firmInfo.loc_height);
			int curXLoc = next_x_loc();
			int curYLoc = next_y_loc();

			if (attackDistance <= attack_range) // able to attack target
			{
				if ((attackDistance == 1) && attack_range > 1) // often false condition is checked first
					choose_best_attack_mode(1); // may change to use close attack

				if (attack_range > 1) // use range attack
				{
					set_cur(next_x, next_y);

					AttackInfo attackInfo = attack_info_array[cur_attack];
					if (!BulletArray.add_bullet_possible(curXLoc, curYLoc, mobile_type, targetXLoc, targetYLoc,
						    UnitConstants.UNIT_LAND, firmInfo.loc_width, firmInfo.loc_height,
						    out range_attack_x_loc, out range_attack_y_loc, attackInfo.bullet_speed,
						    attackInfo.bullet_sprite_id))
					{
						//------- no suitable location, move to target ---------//
						if (result_node_array == null || result_node_count == 0) // no step for continue moving
							set_move_to_surround(action_x_loc, action_y_loc, firmInfo.loc_width, firmInfo.loc_height,
								UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO);

						return; // unable to attack, continue to move
					}

					//---------- able to do range attack ----------//
					set_attack_dir(curXLoc, curYLoc, range_attack_x_loc, range_attack_y_loc);
					cur_frame = 1;

					if (is_dir_correct())
						set_attack();
					else
						set_turn();
				}
				else // close attack
				{
					//---------- attack now ---------//
					set_cur(next_x, next_y);
					terminate_move();

					if (targetFirm.firm_id != Firm.FIRM_RESEARCH)
						set_attack_dir(curXLoc, curYLoc, targetFirm.center_x, targetFirm.center_y);
					else // FIRM_RESEARCH with size 2x3
					{
						int hitXLoc = (curXLoc > targetFirm.loc_x1) ? targetFirm.loc_x2 : targetFirm.loc_x1;

						int hitYLoc;
						if (curYLoc < targetFirm.center_y)
							hitYLoc = targetFirm.loc_y1;
						else if (curYLoc == targetFirm.center_y)
							hitYLoc = targetFirm.center_y;
						else
							hitYLoc = targetFirm.loc_y2;

						set_attack_dir(curXLoc, curYLoc, hitXLoc, hitYLoc);
					}

					if (is_dir_correct())
						set_attack();
					else
						set_turn();
				}

			}
		}
	}

	protected void process_build_firm()
	{
		if (cur_action == SPRITE_IDLE) // the unit is at the build location now
		{
			// **BUGHERE, the unit shouldn't be hidden when building structures
			// otherwise, it's cargo_recno will be conflict with the structure's
			// cargo_recno

			bool succeedFlag = false;
			bool shouldProceed = true;

			if (cur_x_loc() == move_to_x_loc && cur_y_loc() == move_to_y_loc)
			{
				FirmInfo firmInfo = FirmRes[action_para];
				int width = firmInfo.loc_width;
				int height = firmInfo.loc_height;

				//---------------------------------------------------------//
				// check whether the unit in the building surrounding
				//---------------------------------------------------------//
				if (!is_in_surrounding(move_to_x_loc, move_to_y_loc, sprite_info.loc_width,
					    action_x_loc, action_y_loc, width, height))
				{
					//---------- not in the building surrounding ---------//
					return;
				}

				//---------------------------------------------------------//
				// the unit in the firm surrounding
				//---------------------------------------------------------//

				if (nation_recno != 0)
				{
					Nation nation = NationArray[nation_recno];

					if (nation.cash < firmInfo.setup_cost)
						shouldProceed = false; // out of cash
				}

				//---------------------------------------------------------//
				// check whether the firm can be built in the specified location
				//---------------------------------------------------------//
				if (shouldProceed && World.can_build_firm(action_x_loc, action_y_loc, action_para, sprite_recno) != 0 &&
				    FirmRes[action_para].can_build(sprite_recno))
				{
					bool aiUnit = ai_unit;
					int actionXLoc = action_x_loc;
					int actionYLoc = action_y_loc;
					int unitRecno = sprite_recno;

					//---------------------------------------------------------------------------//
					// if unit inside the firm location, deinit the unit to free the space for
					// building firm
					//---------------------------------------------------------------------------//
					if (move_to_x_loc >= action_x_loc && move_to_x_loc < action_x_loc + width &&
					    move_to_y_loc >= action_y_loc && move_to_y_loc < action_y_loc + height)
						deinit_sprite(false); // 0-if the unit is currently selected, deactivate it.

					// action_para = firm id.
					if (FirmArray.BuildFirm(action_x_loc, action_y_loc, nation_recno, action_para,
						    sprite_info.sprite_code, sprite_recno) != 0)
					{
						//--------- able to build the firm --------//

						reset_action_para2();
						succeedFlag = true;
					}
				}
			}

			//----- call action finished/failure -----//

			if (ai_action_id != 0 && nation_recno != 0)
			{
				if (succeedFlag)
					NationArray[nation_recno].action_finished(ai_action_id, sprite_recno);
				else
					NationArray[nation_recno].action_failure(ai_action_id, sprite_recno);
			}

			//---------------------------------------//

			reset_action_para();
		}
	}

	protected void process_attack_town()
	{
		if (!can_attack())
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//------- if the targeted town has been destroyed --------//
		if (action_para == 0)
			return;

		Town targetTown = null;
		int clearOrder = 0;
		//------------------------------------------------------------//
		// check attack conditions
		//------------------------------------------------------------//
		if (TownArray.IsDeleted(action_para))
		{
			if (!ConfigAdv.unit_finish_attack_move || cur_action == SPRITE_ATTACK)
				clearOrder++;
			else
			{
				// keep attack action alive to finish movement before going idle
				invalidate_attack_target();
				return;
			}
		}
		else
		{
			targetTown = TownArray[action_para];

			if (!nation_can_attack(targetTown.NationId)) // cannot attack this nation
				clearOrder++;
		}

		if (clearOrder != 0)
		{
			//------------------------------------------------------------//
			// change to detect target if in defend mode
			//------------------------------------------------------------//
			/*if(action_mode2==UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET || action_mode2==ACTION_DEFEND_TOWN_ATTACK_TARGET ||
				action_mode2==ACTION_MONSTER_DEFEND_ATTACK_TARGET)
			{
				err_when((action_misc!=UnitConstants.ACTION_MISC_DEFENSE_CAMP_RECNO && action_misc!=UnitConstants.ACTION_MISC_DEFEND_TOWN_RECNO &&
							 action_misc!=UnitConstants.ACTION_MISC_MONSTER_DEFEND_FIRM_RECNO) || !action_misc_para);
				stop2(UnitConstants.KEEP_DEFENSE_MODE);
				return;
			}
	
			err_when(action_mode2==ACTION_AUTO_DEFENSE_DETECT_TARGET || action_mode2==ACTION_AUTO_DEFENSE_BACK_CAMP ||
						action_mode2==ACTION_DEFEND_TOWN_DETECT_TARGET || action_mode2==ACTION_DEFEND_TOWN_BACK_TOWN ||
						action_mode2==ACTION_MONSTER_DEFEND_DETECT_TARGET || action_mode2==ACTION_MONSTER_DEFEND_BACK_FIRM);
	
			stop2(); // clear order
			err_when(cur_action==SPRITE_ATTACK && (move_to_x_loc!=next_x_loc() || move_to_y_loc!=next_y_loc()));
			err_when(action_mode==ACTION_ATTACK_TOWN && !action_para);
			err_when(cur_action==SPRITE_ATTACK && action_mode==UnitConstants.ACTION_STOP);
			return;*/

			stop2(UnitConstants.KEEP_DEFENSE_MODE);

			return;
		}

		//-----------------------------------------------------//
		// If the unit is currently attacking somebody.
		//-----------------------------------------------------//
		if (cur_action == SPRITE_ATTACK)
		{
			if (remain_attack_delay != 0)
				return;

			AttackInfo attackInfo = attack_info_array[cur_attack];
			if (attackInfo.attack_range > 1) // range attack
			{
				//---------- wait for bullet emit ---------//
				if (cur_frame != attackInfo.bullet_out_frame)
					return;

				//------- seek location to attack target by bullet --------//
				int curXLoc = next_x_loc();
				int curYLoc = next_y_loc();
				if (!BulletArray.bullet_path_possible(curXLoc, curYLoc, mobile_type, range_attack_x_loc,
					    range_attack_y_loc,
					    UnitConstants.UNIT_LAND, attackInfo.bullet_speed, attackInfo.bullet_sprite_id))
				{
					if (!BulletArray.add_bullet_possible(curXLoc, curYLoc, mobile_type, action_x_loc, action_y_loc,
						    UnitConstants.UNIT_LAND, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT,
						    out range_attack_x_loc, out range_attack_y_loc, attackInfo.bullet_speed,
						    attackInfo.bullet_sprite_id))
					{
						//----- no suitable location, move to target --------//
						set_move_to_surround(action_x_loc, action_y_loc, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT,
							UnitConstants.BUILDING_TYPE_TOWN_MOVE_TO);
						return;
					}
				}

				//--------- add bullet, bullet emits --------//
				BulletArray.AddBullet(this, action_x_loc, action_y_loc);
				add_close_attack_effect();

				// ------- reduce power --------//
				cur_power -= attackInfo.consume_power;
				if (cur_power < 0) // ***** BUGHERE
					cur_power = 0;
				set_remain_attack_delay();
				return;
			}
			else // close attack
			{
				if (cur_frame != cur_sprite_attack().frame_count)
					return; // attacking

				hit_town(this, action_x_loc, action_y_loc, actual_damage(), nation_recno);
				add_close_attack_effect();

				// ------- reduce power --------//
				cur_power -= attackInfo.consume_power;
				if (cur_power < 0) // ***** BUGHERE
					cur_power = 0;
				set_remain_attack_delay();
			}
		}
		//--------------------------------------------------------------------------------------------------//
		// If the unit is on its way to attack the town, if it has gotten close next to it, attack now
		//--------------------------------------------------------------------------------------------------//
		// it has moved to the specified location. check cur_x & go_x to make sure the sprite has completely move to the location, not just crossing it.
		else if (Math.Abs(cur_x - next_x) <= sprite_info.speed && Math.Abs(cur_y - next_y) <= sprite_info.speed)
		{
			if (mobile_type == UnitConstants.UNIT_LAND)
			{
				if (detect_surround_target())
					return;
			}

			if (attack_range == 1)
			{
				//------------------------------------------------------------//
				// for close attack, the unit unable to attack the firm if
				// it is not in the firm surrounding
				//------------------------------------------------------------//
				if (result_path_dist > attack_range)
					return;
			}

			int targetXLoc = targetTown.LocX1;
			int targetYLoc = targetTown.LocY1;

			int attackDistance = cal_distance(targetXLoc, targetYLoc, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT);

			if (attackDistance <= attack_range) // able to attack target
			{
				if ((attackDistance == 1) && attack_range > 1) // often false condition is checked first
					choose_best_attack_mode(1); // may change to use close attack

				if (attack_range > 1) // use range attack
				{
					set_cur(next_x, next_y);

					AttackInfo attackInfo = attack_info_array[cur_attack];
					int curXLoc = next_x_loc();
					int curYLoc = next_y_loc();
					if (!BulletArray.add_bullet_possible(curXLoc, curYLoc, mobile_type, targetXLoc, targetYLoc,
						    UnitConstants.UNIT_LAND, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT,
						    out range_attack_x_loc, out range_attack_y_loc, attackInfo.bullet_speed,
						    attackInfo.bullet_sprite_id))
					{
						//------- no suitable location, move to target ---------//
						if (result_node_array == null || result_node_count == 0) // no step for continuing moving
							set_move_to_surround(action_x_loc, action_y_loc, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT,
								UnitConstants.BUILDING_TYPE_TOWN_MOVE_TO);

						return; // unable to attack, continue to move
					}

					//---------- able to do range attack ----------//
					set_attack_dir(next_x_loc(), next_y_loc(), range_attack_x_loc, range_attack_y_loc);
					cur_frame = 1;

					if (is_dir_correct())
						set_attack();
					else
						set_turn();
				}
				else // close attack
				{
					//---------- attack now ---------//
					set_cur(next_x, next_y);
					terminate_move();
					set_dir(next_x_loc(), next_y_loc(), targetTown.LocCenterX, targetTown.LocCenterY);

					if (is_dir_correct())
						set_attack();
					else
						set_turn();
				}
			}
		}
	}

	protected void process_attack_wall()
	{
		if (!can_attack())
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//------------------------------------------------------------//
		// if the targeted wall has been destroyed
		//------------------------------------------------------------//
		Location loc = World.get_loc(action_x_loc, action_y_loc);
		if (!loc.is_wall())
		{
			if (!ConfigAdv.unit_finish_attack_move || cur_action == SPRITE_ATTACK)
			{
				stop2(UnitConstants.KEEP_DEFENSE_MODE);
			}

			return;
		}

		//-----------------------------------------------------//
		// If the unit is currently attacking.
		//-----------------------------------------------------//
		if (cur_action == SPRITE_ATTACK)
		{
			if (remain_attack_delay != 0)
				return;

			AttackInfo attackInfo = attack_info_array[cur_attack];
			if (attackInfo.attack_range > 1) // range attack
			{
				//--------- wait for bullet emit ----------//
				if (cur_frame != attackInfo.bullet_out_frame)
					return;

				//---------- seek location to attack target by bullet --------//
				int curXLoc = next_x_loc();
				int curYLoc = next_y_loc();
				if (!BulletArray.bullet_path_possible(curXLoc, curYLoc, mobile_type, range_attack_x_loc,
					    range_attack_y_loc,
					    UnitConstants.UNIT_LAND, attackInfo.bullet_speed, attackInfo.bullet_sprite_id))
				{
					if (!BulletArray.add_bullet_possible(curXLoc, curYLoc, mobile_type, 
						    action_x_loc, action_y_loc,
						    UnitConstants.UNIT_LAND, 1, 1,
						    out range_attack_x_loc, out range_attack_y_loc,
						    attackInfo.bullet_speed, attackInfo.bullet_sprite_id))
					{
						//--------- no suitable location, move to target ----------//
						set_move_to_surround(action_x_loc, action_y_loc, 1, 1, UnitConstants.BUILDING_TYPE_WALL);
						return;
					}
				}

				//---------- add bullet, bullet emits -----------//
				BulletArray.AddBullet(this, action_x_loc, action_y_loc);
				add_close_attack_effect();

				// ------- reduce power --------//
				cur_power -= attackInfo.consume_power;
				if (cur_power < 0) // ***** BUGHERE
					cur_power = 0;
				set_remain_attack_delay();
				return;
			}
			else
			{
				if (cur_frame != cur_sprite_attack().frame_count)
					return; // attacking

				hit_wall(this, action_x_loc, action_y_loc, actual_damage(), nation_recno);
				add_close_attack_effect();

				//------- reduce power --------//
				cur_power -= attackInfo.consume_power;
				if (cur_power < 0) // ***** BUGHERE
					cur_power = 0;
				set_remain_attack_delay();
			}
		}
		//--------------------------------------------------------------------------------------------------//
		// If the unit is on its way to attack somebody, if it has gotten close next to the target, attack now
		//--------------------------------------------------------------------------------------------------//
		// it has moved to the specified location. check cur_x & go_x to make sure the sprite has completely move to the location, not just crossing it.
		else if (Math.Abs(cur_x - next_x) <= sprite_info.speed && Math.Abs(cur_y - next_y) <= sprite_info.speed)
		{
			if (mobile_type == UnitConstants.UNIT_LAND)
			{
				if (detect_surround_target())
					return;
			}

			if (attack_range == 1)
			{
				//------------------------------------------------------------//
				// for close attack, the unit unable to attack the firm if
				// it is not in the firm surrounding
				//------------------------------------------------------------//
				if (result_path_dist > attack_range)
					return;
			}

			int attackDistance = cal_distance(action_x_loc, action_y_loc, 1, 1);

			if (attackDistance <= attack_range) // able to attack target
			{
				if ((attackDistance == 1) && attack_range > 1) // often false condition is checked first
					choose_best_attack_mode(1); // may change to use close attack

				if (attack_range > 1) // use range attack
				{
					set_cur(next_x, next_y);

					AttackInfo attackInfo = attack_info_array[cur_attack];
					int curXLoc = next_x_loc();
					int curYLoc = next_y_loc();
					if (!BulletArray.add_bullet_possible(curXLoc, curYLoc, mobile_type,
						    action_x_loc, action_y_loc,
						    UnitConstants.UNIT_LAND, 1, 1,
						    out range_attack_x_loc, out range_attack_y_loc,
						    attackInfo.bullet_speed, attackInfo.bullet_sprite_id))
					{
						//------- no suitable location, move to target ---------//
						if (result_node_array == null || result_node_count == 0) // no step for continuing moving
							set_move_to_surround(action_x_loc, action_y_loc, 1, 1, UnitConstants.BUILDING_TYPE_WALL);

						return; // unable to attack, continue to move
					}

					//---------- able to do range attack ----------//
					set_attack_dir(curXLoc, curYLoc, range_attack_x_loc, range_attack_y_loc);
					cur_frame = 1;

					if (is_dir_correct())
						set_attack();
					else
						set_turn();
				}
				else // close attack
				{
					//---------- attack now ---------//
					set_cur(next_x, next_y);
					terminate_move();
					set_attack_dir(next_x_loc(), next_y_loc(), action_x_loc, action_y_loc);

					if (is_dir_correct())
						set_attack();
					else
						set_turn();
				}

			}
		}
	}

	protected void process_assign()
	{
		if (cur_action != SPRITE_IDLE)
		{
			//------------------------------------------------------------------//
			// change units' action if the firm/town/unit assign to has been deleted
			// or has changed its nation
			//------------------------------------------------------------------//
			switch (action_mode2)
			{
				case UnitConstants.ACTION_ASSIGN_TO_FIRM:
				case UnitConstants.ACTION_AUTO_DEFENSE_BACK_CAMP:
				case UnitConstants.ACTION_MONSTER_DEFEND_BACK_FIRM:
					if (FirmArray.IsDeleted(action_para))
					{
						stop2();
						return;
					}
					else
					{
						Firm firm = FirmArray[action_para];
						if (firm.nation_recno != nation_recno && !firm.can_assign_capture())
						{
							stop2();
							return;
						}
					}

					break;

				case UnitConstants.ACTION_ASSIGN_TO_TOWN:
				case UnitConstants.ACTION_DEFEND_TOWN_BACK_TOWN:
					if (TownArray.IsDeleted(action_para))
					{
						stop2();
						return;
					}
					else if (TownArray[action_para].NationId != nation_recno)
					{
						stop2();
						return;
					}

					break;

				case UnitConstants.ACTION_ASSIGN_TO_VEHICLE:
					if (UnitArray.IsDeleted(action_para))
					{
						stop2();
						return;
					}
					else if (UnitArray[action_para].nation_recno != nation_recno)
					{
						stop2();
						return;
					}

					break;

				default:
					break;
			}
		}
		else //--------------- unit is idle -----------------//
		{
			if (cur_x_loc() == move_to_x_loc && cur_y_loc() == move_to_y_loc)
			{
				//----- first check if there is firm in the given location ------//
				Location loc = World.get_loc(action_x_loc, action_y_loc);

				if (loc.is_firm() && loc.firm_recno() == action_para)
				{
					//---------------- a firm on the location -----------------//
					Firm firm = FirmArray[action_para];
					FirmInfo firmInfo = FirmRes[firm.firm_id];

					//---------- resume action if the unit has not reached the firm surrounding ----------//
					if (!is_in_surrounding(move_to_x_loc, move_to_y_loc, sprite_info.loc_width,
						    action_x_loc, action_y_loc, firmInfo.loc_width, firmInfo.loc_height))
					{
						//------------ not in the surrounding -----------//
						if (action_mode != action_mode2) // for defense mode
							set_move_to_surround(action_x_loc, action_y_loc, firmInfo.loc_width, firmInfo.loc_height,
								UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO);
						return;
					}

					//------------ in the firm surrounding ------------//
					if (!firm.under_construction)
					{
						//-------------------------------------------------------//
						// if in defense mode, update parameters in military camp
						//-------------------------------------------------------//
						if (action_mode2 == UnitConstants.ACTION_AUTO_DEFENSE_BACK_CAMP)
						{
							FirmCamp camp = (FirmCamp)firm;
							camp.update_defense_unit(sprite_recno);
						}

						//---------------------------------------------------------------//
						// remainder useful parameters to do reaction to Nation.
						// These parameters will be destroyed after calling assign_unit()
						//---------------------------------------------------------------//
						int nationRecno = nation_recno;
						int unitRecno = sprite_recno;
						int actionXLoc = action_x_loc;
						int actionYLoc = action_y_loc;
						int aiActionId = ai_action_id;
						bool aiUnit = ai_unit;

						reset_action_para2();

						firm.assign_unit(sprite_recno);

						//----------------------------------------------------------//
						// FirmArray[].assign_unit() must be done first.  Then a
						// town will be created and the reaction to build other firms
						// requires the location of the town.
						//----------------------------------------------------------//

						if (aiActionId != 0)
							NationArray[nationRecno].action_finished(aiActionId, unitRecno);

						if (UnitArray.IsDeleted(unitRecno))
							return;

						//--- else the firm is full, the unit's skill level is lower than those in firm, or no space to create town ---//
					}
					else
					{
						//---------- change the builder ------------//
						if (ai_unit && firm.under_construction)
							return; // not allow AI to change firm builder

						reset_action_para2();
						if (skill.get_skill(Skill.SKILL_CONSTRUCTION) != 0 || skill.get_skill(firm.firm_skill_id) != 0)
							firm.set_builder(sprite_recno);
					}

					//------------ update UnitArray's selected parameters ------------//
					reset_action_para();
					if (selected_flag)
					{
						selected_flag = false;
						UnitArray.selected_count--;
					}
				}
				else if (loc.is_town() && loc.town_recno() == action_para)
				{
					//---------------- a town on the location -----------------//
					if (!is_in_surrounding(move_to_x_loc, move_to_y_loc, sprite_info.loc_width, action_x_loc,
						    action_y_loc,
						    InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT))
					{
						//----------- not in the surrounding ------------//
						return;
					}

					int actionPara = action_para;
					int spriteRecno = sprite_recno;

					if (ai_action_id != 0 && nation_recno != 0)
						NationArray[nation_recno].action_finished(ai_action_id, sprite_recno);

					//------------ update UnitArray's selected parameters ------------//
					reset_action_para2();
					reset_action_para();
					if (selected_flag)
					{
						selected_flag = false;
						UnitArray.selected_count--;
					}

					//-------------- assign the unit to the town -----------------//
					TownArray[actionPara].AssignUnit(this);
				}

				//####### begin trevor 18/8 #########// the following code was called wrongly and causing bug
				/*
				//------ embarking a ground vehicle/animal ------//

				else if(loc.has_unit(UnitRes.UNIT_LAND) && loc.unit_recno(UnitRes.UNIT_LAND) == action_para)
				{
					reset_action_para2();
					reset_action_para();
					if(selected_flag)
					{
						selected_flag = 0;
						UnitArray.selected_count--;
					}

					embark(action_para);
				}
				*/
				//####### end trevor 18/8 #########//

				//------ embarking a sea vehicle/animal ------//

				else if (loc.has_unit(UnitConstants.UNIT_SEA) && loc.unit_recno(UnitConstants.UNIT_SEA) == action_para)
				{
					//------------ update UnitArray's selected parameters ------------//
					reset_action_para2();
					reset_action_para();
					if (selected_flag)
					{
						selected_flag = false;
						UnitArray.selected_count--;
					}

					//----------------- load the unit to the marine -----------------//
					((UnitMarine)UnitArray[action_para]).load_unit(sprite_recno);
				}
				else
				{
					//------------------------------------------------------------------//
					// abort actions for ai_unit since the target location has nothing
					//------------------------------------------------------------------//

					if (ai_action_id != 0 && nation_recno != 0)
						NationArray[nation_recno].action_failure(ai_action_id, sprite_recno);
				}
			}

			//-***** don't place codes here as unit may be removed above *****-//
			//reset_action_para();
			//selected_flag = 0;
		}
	}

	protected void process_burn()
	{
		if (cur_action == SPRITE_IDLE) // the unit is at the build location now
		{
			if (next_x_loc() == action_x_loc && next_y_loc() == action_y_loc)
			{
				reset_action_para2();
				set_dir(move_to_x_loc, move_to_y_loc, action_x_loc, action_y_loc);
				World.setup_fire(action_x_loc, action_y_loc);
			}

			reset_action_para();
		}
	}

	protected void process_settle()
	{
		if (cur_action == SPRITE_IDLE) // the unit is at the build location now
		{
			reset_path();

			if (cur_x_loc() == move_to_x_loc && cur_y_loc() == move_to_y_loc)
			{
				if (!is_in_surrounding(move_to_x_loc, move_to_y_loc, sprite_info.loc_width, action_x_loc, action_y_loc,
					    InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT))
					return;

				Location loc = World.get_loc(action_x_loc, action_y_loc);
				if (!loc.is_town())
				{
					int unitRecno = sprite_recno;

					reset_action_para2();
					//------------ settle the unit now -------------//
					TownArray.Settle(sprite_recno, action_x_loc, action_y_loc);

					if (UnitArray.IsDeleted(unitRecno))
						return;

					reset_action_para();
				}
				else if (TownArray[loc.town_recno()].NationId == nation_recno)
				{
					//---------- a town zone already exists ---------//
					assign(action_x_loc, action_y_loc);
					return;
				}
			}
			else
				reset_action_para();
		}
	}

	protected void process_assign_to_ship()
	{
		//---------------------------------------------------------------------------//
		// clear unit's action if situation is changed
		//---------------------------------------------------------------------------//
		UnitMarine ship;
		if (UnitArray.IsDeleted(action_para2))
		{
			stop2();
			return; // stop the unit as the ship is deleted
		}
		else
		{
			ship = (UnitMarine)UnitArray[action_para2];
			if (ship.nation_recno != nation_recno)
			{
				stop2();
				return; // stop the unit as the ship's nation_recno != the unit's nation_recno
			}
		}

		if (ship.action_mode2 != UnitConstants.ACTION_SHIP_TO_BEACH)
		{
			stop2(); // the ship has changed its action
			return;
		}

		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();
		int shipXLoc = ship.next_x_loc();
		int shipYLoc = ship.next_y_loc();

		if (ship.cur_x == ship.next_x && ship.cur_y == ship.next_y &&
		    Math.Abs(shipXLoc - curXLoc) <= 1 && Math.Abs(shipYLoc - curYLoc) <= 1)
		{
			//----------- assign the unit now -----------//
			if (Math.Abs(cur_x - next_x) < sprite_info.speed && Math.Abs(cur_y - next_y) < sprite_info.speed)
			{
				if (ai_action_id != 0)
					NationArray[nation_recno].action_finished(ai_action_id, sprite_recno);

				stop2();
				set_dir(curXLoc, curYLoc, shipXLoc, shipYLoc);
				ship.load_unit(sprite_recno);
				return;
			}
		}
		else if (cur_action == SPRITE_IDLE)
			set_dir(curXLoc, curYLoc, ship.move_to_x_loc, ship.move_to_y_loc);

		//---------------------------------------------------------------------------//
		// update location to embark
		//---------------------------------------------------------------------------//
		int shipActionXLoc = ship.action_x_loc2;
		int shipActionYLoc = ship.action_y_loc2;
		if (Math.Abs(shipActionXLoc - action_x_loc2) > 1 || Math.Abs(shipActionYLoc - action_y_loc2) > 1)
		{
			if (shipActionXLoc != action_x_loc2 || shipActionYLoc != action_y_loc2)
			{
				Location unitLoc = World.get_loc(curXLoc, curYLoc);
				Location shipActionLoc = World.get_loc(shipActionXLoc, shipActionYLoc);
				if (unitLoc.region_id != shipActionLoc.region_id)
				{
					stop2();
					return;
				}

				assign_to_ship(shipActionXLoc, shipActionYLoc, action_para2);
				return;
			}
		}
	}

	protected void process_ship_to_beach()
	{
		//----- action_mode never clear, in_beach to skip idle checking
		if (cur_action == SPRITE_IDLE)
		{
			int shipXLoc = next_x_loc();
			int shipYLoc = next_y_loc();
			if (shipXLoc == move_to_x_loc && shipYLoc == move_to_y_loc)
			{
				if (Math.Abs(move_to_x_loc - action_x_loc2) <= 2 && Math.Abs(move_to_y_loc - action_y_loc2) <= 2)
				{
					UnitMarine ship = (UnitMarine)this;
					//------------------------------------------------------------------------------//
					// determine whether extra_move is required
					//------------------------------------------------------------------------------//
					switch (ship.extra_move_in_beach)
					{
						case UnitMarine.NO_EXTRA_MOVE:
							if (Math.Abs(shipXLoc - action_x_loc2) > 1 || Math.Abs(shipYLoc - action_y_loc2) > 1)
							{
								ship.extra_move();
							}
							else
							{
								//ship.in_beach = 1;
								ship.extra_move_in_beach = UnitMarine.NO_EXTRA_MOVE;

								if (ai_action_id != 0)
									NationArray[nation_recno].action_finished(ai_action_id, sprite_recno);
							}

							break;

						case UnitMarine.EXTRA_MOVING_IN:
						case UnitMarine.EXTRA_MOVING_OUT:
							break;

						case UnitMarine.EXTRA_MOVE_FINISH:
							if (ai_action_id != 0)
								NationArray[nation_recno].action_finished(ai_action_id, sprite_recno);
							break;

						default:
							break;
					}
				}
			}
			else
				reset_action_para();
		}
		else if (cur_action == SPRITE_TURN && is_dir_correct())
			set_move();
	}

	protected void process_rebel()
	{
		Rebel rebel = RebelArray[unit_mode_para];

		switch (rebel.action_mode)
		{
			case Rebel.REBEL_ATTACK_TOWN:
				if (TownArray.IsDeleted(rebel.action_para)) // if the town has been destroyed.
					rebel.set_action(Rebel.REBEL_IDLE);
				else
				{
					Town town = TownArray[rebel.action_para];
					attack_town(town.LocX1, town.LocY1);
				}

				break;

			case Rebel.REBEL_SETTLE_NEW:
				if (!World.can_build_town(rebel.action_para, rebel.action_para2, sprite_recno))
				{
					Location loc = World.get_loc(rebel.action_para, rebel.action_para2);

					if (loc.is_town() && TownArray[loc.town_recno()].RebelId == rebel.rebel_recno)
					{
						rebel.action_mode = Rebel.REBEL_SETTLE_TO;
					}
					else
					{
						rebel.action_mode = Rebel.REBEL_IDLE;
					}
				}
				else
				{
					settle(rebel.action_para, rebel.action_para2);
				}

				break;

			case Rebel.REBEL_SETTLE_TO:
				assign(rebel.action_para, rebel.action_para2);
				break;
		}
	}

	protected void process_go_cast_power()
	{
		UnitGod unitGod = (UnitGod)this;
		if (cur_action == SPRITE_IDLE)
		{
			//----------------------------------------------------------------------------------------//
			// Checking condition to do casting power, Or resume action
			//----------------------------------------------------------------------------------------//
			if (Misc.points_distance(cur_x_loc(), cur_y_loc(), action_x_loc2, action_y_loc2) <= UnitConstants.DO_CAST_POWER_RANGE)
			{
				if (next_x_loc() != action_x_loc2 || next_y_loc() != action_y_loc2)
				{
					set_dir(next_x_loc(), next_y_loc(), action_x_loc2, action_y_loc2);
				}

				set_attack(); // set cur_action=sprite_attack to cast power 
				cur_frame = 1;
			}
			else
			{
				go_cast_power(action_x_loc2, action_y_loc2, unitGod.cast_power_type, InternalConstants.COMMAND_AUTO);
			}
		}
		else if (cur_action == SPRITE_ATTACK)
		{
			//----------------------------------------------------------------------------------------//
			// do casting power now
			//----------------------------------------------------------------------------------------//
			AttackInfo attackInfo = attack_info_array[cur_attack];
			if (cur_frame == attackInfo.bullet_out_frame)
			{
				// add effect
				add_close_attack_effect();

				unitGod.cast_power(action_x_loc2, action_y_loc2);
				set_remain_attack_delay();
				// stop2();
			}

			if (cur_frame == 1 && remain_attack_delay == 0) // last frame of delaying
				stop2();
		}
	}

	protected void next_move()
	{
		if (result_node_array == null || result_node_count == 0 || result_node_recno == 0)
			return;

		if (++result_node_recno > result_node_count)
		{
			//------------ all nodes are visited --------------//
			result_node_array = null;
			set_idle();

			if (action_mode2 == UnitConstants.ACTION_MOVE) //--------- used to terminate action_mode==ACTION_MOVE
			{
				force_move_flag = false;

				//------- reset ACTION_MOVE parameters ------//
				reset_action_para();
				if (move_to_x_loc == action_x_loc2 && move_to_y_loc == action_y_loc2)
					reset_action_para2();
			}

			return;
		}

		//---- order the unit to move to the next checkpoint following the path ----//

		ResultNode resultNode = result_node_array[result_node_recno - 1];

		sprite_move(resultNode.node_x * InternalConstants.CellWidth, resultNode.node_y * InternalConstants.CellHeight);
	}

	protected void terminate_move()
	{
		go_x = next_x;
		go_y = next_y;

		move_to_x_loc = next_x_loc();
		move_to_y_loc = next_y_loc();

		cur_frame = 1;

		reset_path();
		set_idle();
	}

	protected void reset_path()
	{
		result_node_array = null;
		result_path_dist = result_node_count = result_node_recno = 0;
	}

	protected void king_die()
	{
		//--------- add news ---------//

		NewsArray.king_die(nation_recno);

		//--- see if the units, firms and towns of the nation are all destroyed ---//

		Nation nation = NationArray[nation_recno];

		nation.king_unit_recno = 0;
	}

	protected void general_die()
	{
		//--------- add news ---------//

		if (nation_recno == NationArray.player_recno)
			NewsArray.general_die(sprite_recno);
	}

	protected void pay_expense()
	{
		if (nation_recno == 0)
			return;

		//--- if it's a mobile spy or the spy is in its own firm, no need to pay salary here as Spy::pay_expense() will do that ---//
		//
		// -If your spies are mobile:
		//  >your nation pays them 1 food and $5 dollars per month
		//
		// -If your spies are in an enemy's town or firm:
		//  >the enemy pays them 1 food and the normal salary of their jobs.
		//
		//  >your nation pays them $5 dollars per month. (your nation pays them no food)
		//
		// -If your spies are in your own town or firm:
		//  >your nation pays them 1 food and $5 dollars per month
		//
		//------------------------------------------------------//

		if (spy_recno != 0)
		{
			if (is_visible()) // the cost will be deducted in SpyArray
				return;

			if (unit_mode == UnitConstants.UNIT_MODE_OVERSEE &&
			    FirmArray[unit_mode_para].nation_recno == true_nation_recno())
			{
				return;
			}
		}

		//---------- if it's a human unit -----------//
		//
		// The unit is paid even during its training period in a town
		//
		//-------------------------------------------//

		Nation nation = NationArray[nation_recno];

		if (UnitRes[unit_id].race_id != 0)
		{
			//---------- reduce cash -----------//

			if (nation.cash > 0)
			{
				if (rank_id == RANK_SOLDIER)
					nation.add_expense(NationBase.EXPENSE_MOBILE_UNIT,
						GameConstants.SOLDIER_YEAR_SALARY / 365.0, true);

				if (rank_id == RANK_GENERAL)
					nation.add_expense(NationBase.EXPENSE_GENERAL,
						GameConstants.GENERAL_YEAR_SALARY / 365.0, true);
			}
			else // decrease loyalty if the nation cannot pay the unit
			{
				change_loyalty(-1);
			}

			//---------- reduce food -----------//

			if (UnitRes[unit_id].race_id != 0) // if it's a human unit
			{
				if (nation.food > 0)
					nation.consume_food(GameConstants.PERSON_FOOD_YEAR_CONSUMPTION / 365.0);
				else
				{
					// decrease 1 loyalty point every 2 days
					if (Info.TotalDays % GameConstants.NO_FOOD_LOYALTY_DECREASE_INTERVAL == 0)
						change_loyalty(-1);
				}
			}
		}
		else //----- it's a non-human unit ------//
		{
			if (nation.cash > 0)
			{
				int expenseType;

				switch (UnitRes[unit_id].unit_class)
				{
					case UnitConstants.UNIT_CLASS_WEAPON:
						expenseType = NationBase.EXPENSE_WEAPON;
						break;

					case UnitConstants.UNIT_CLASS_SHIP:
						expenseType = NationBase.EXPENSE_SHIP;
						break;

					case UnitConstants.UNIT_CLASS_CARAVAN:
						expenseType = NationBase.EXPENSE_CARAVAN;
						break;

					default:
						expenseType = NationBase.EXPENSE_MOBILE_UNIT;
						break;
				}

				nation.add_expense(expenseType, UnitRes[unit_id].year_cost / 365.0, true);
			}
			else // decrease hit points if the nation cannot pay the unit
			{
				// Even when caravans are not paid, they still stay in your service.
				if (UnitRes[unit_id].unit_class != UnitConstants.UNIT_CLASS_CARAVAN)
				{
					if (hit_points > 0.0)
					{
						hit_points--;

						//--- when the hit points drop to zero and the unit is destroyed ---//

						if (hit_points <= 0.0)
						{
							if (nation_recno == NationArray.player_recno)
							{
								int unitClass = UnitRes[unit_id].unit_class;

								if (unitClass == UnitConstants.UNIT_CLASS_WEAPON)
									NewsArray.weapon_ship_worn_out(unit_id, get_weapon_version());

								else if (unitClass == UnitConstants.UNIT_CLASS_SHIP)
									NewsArray.weapon_ship_worn_out(unit_id, 0);
							}

							hit_points = 0.0;
						}
					}
				}
			}
		}
	}

	protected void process_recover()
	{
		if (hit_points == 0.0 || hit_points == max_hit_points) // this unit is dead already
			return;

		//---- overseers in firms and ships in harbors recover faster ----//

		int hitPointsInc;

		if (unit_mode == UnitConstants.UNIT_MODE_OVERSEE || unit_mode == UnitConstants.UNIT_MODE_IN_HARBOR)
		{
			hitPointsInc = 2;
		}

		//------ for units on ships --------//

		else if (unit_mode == UnitConstants.UNIT_MODE_ON_SHIP)
		{
			//--- if the ship where the unit is on is in the harbor, the unit recovers faster ---//

			if (UnitArray[unit_mode_para].unit_mode == UnitConstants.UNIT_MODE_IN_HARBOR)
				hitPointsInc = 2;
			else
				hitPointsInc = 1;
		}

		//----- only recover when the unit is not moving -----//

		else if (cur_action == SPRITE_IDLE)
		{
			hitPointsInc = 1;
		}
		else
			return;

		//---------- recover now -----------//

		hit_points += hitPointsInc;

		if (hit_points > max_hit_points)
			hit_points = max_hit_points;
	}

	public int CampInfluence()
	{
		Nation nation = NationArray[nation_recno]; // nation of the unit

		int thisInfluence = skill.get_skill(Skill.SKILL_LEADING) * 2 / 3; // 66% of the leadership

		if (RaceRes.is_same_race(nation.race_id, race_id))
			thisInfluence += thisInfluence / 3; // 33% bonus if the king's race is also the same as the general

		thisInfluence += (int)(nation.reputation / 2.0);

		thisInfluence = Math.Min(100, thisInfluence);

		return thisInfluence;
	}
}
