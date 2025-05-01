using System;
using System.Collections.Generic;

namespace TenKingdoms;

public partial class Unit : Sprite
{
	public const int RANK_SOLDIER = 0;
	public const int RANK_GENERAL = 1;
	public const int RANK_KING = 2;

	public int UnitType { get; private set; }
	public int Rank { get; private set; }
	public int RaceId { get; private set; }
	public int NationId { get; private set; }
	public int NameId { get; private set; }

	public double HitPoints { get; set; }
	public int MaxHitPoints { get; private set; }
	public int Loyalty { get; set; }
	public int TargetLoyalty { get; private set; }
	public Skill Skill { get; } = new Skill();
	public int UnitMode { get; set; }
	public int UnitModeParam { get; set; } // if unit_mode == UNIT_MODE_REBEL, unit_mode_para is rebelId this unit belongs to
	public int SpyId { get; set; }
	public int NationContribution { get; private set; } // For humans: contribution to the nation. For weapons: the tech level!
	public int TotalReward { get; private set; } // total amount of reward you have given to the unit
	public bool AggressiveMode { get; set; }
	public int HomeCampId { get; set; }
	public TeamInfo TeamInfo { get; } = new TeamInfo();
	public int TeamId { get; set; } // id of defined team
	public int LeaderId { get; set; } // id of this unit's leader
	public bool AIUnit { get; set; }
	
	
	public bool SelectedFlag { get; set; } // whether the unit has been selected or not
	public int GroupId { get; set; } // the group id this unit belong to if it is selected

	
	public int ActionMode { get; set; }
	public int ActionParam { get; set; }
	public int ActionLocX { get; set; }
	public int ActionLocY { get; set; }
	public int ActionMisc { get; set; }
	public int ActionMiscParam { get; set; }
	public int ActionMode2 { get; set; } // store the existing action for speeding up the performance if same action is ordered.
	public int ActionPara2 { get; set; } // to re-activate the unit if its cur_action is idle
	public int ActionLocX2 { get; set; }
	public int ActionLocY2 { get; set; }
	private int OriginalActionMode { get; set; }
	private int OriginalActionParam { get; set; }
	private int OriginalActionLocX { get; set; }
	private int OriginalActionLocY { get; set; }
	// the original location of the attacking target when the Attack() function is called
	// ActionLocX2 & ActionLocY2 will change when the unit move, but these two will not.
	private int OriginalTargetLocX { get; set; }
	private int OriginalTargetLocY { get; set; }

	
	protected bool ForceMove { get; set; }
	public int IgnorePowerNation { get; set; }
	public int MoveToLocX { get; protected set; }
	public int MoveToLocY { get; protected set; }
	public int RangeAttackLocX { get; private set; } // -1 for unable to do range_attack, use to store previous range attack location
	public int RangeAttackLocY { get; private set; } // -1 for unable to do range_attack, use to store previous range attack location
	public List<int> PathNodes { get; } = new List<int>();
	public int PathNodeIndex { get; private set; } = -1;
	private int _pathNodeDistance;
	private List<int> WayPoints { get; } = new List<int>();

	
	public AttackInfo[] AttackInfos { get; protected set; }
	public int AttackCount { get; private set; }
	public int AttackRange { get; private set; }
	public int AttackDirection { get; private set; }
	public int CurPower { get; private set; } // power for power attack
	public int MaxPower { get; private set; }
	protected bool CanAttack { get; set; } // 1 able to attack, 0 unable to attack no matter what attack_count is
	private int CanGuard { get; set; } // bit0= standing guard, bit1=moving guard


	private int WaitingTerm { get; set; } // for 2x2 unit only, the term to wait before recalling A* to get a new path
	private byte[] BlockedEdges { get; } = new byte[4]; // for calling searching in attacking
	private int BlockedByMember { get; set; }
	private bool Swapping { get; set; }

	
	private static bool MoveActionCallFlag { get; set; }
	private static bool IdleDetectHasUnit { get; set; }
	private static bool IdleDetectHasFirm { get; set; }
	private static bool IdleDetectHasTown { get; set; }
	private static bool IdleDetectHasWall { get; set; }
	private static int IdleDetectTargetUnitId { get; set; }
	private static int IdleDetectTargetFirmId { get; set; }
	private static int IdleDetectTargetTownId { get; set; }
	private static int IdleDetectTargetWallX1 { get; set; }
	private static int IdleDetectTargetWallY1 { get; set; }
	private static int CycleWaitUnitIndex { get; set; }
	private static int[] CycleWaitUnitArray { get; set; }
	private static int CycleWaitUnitArrayDefSize { get; set; }
	private static int CycleWaitUnitArrayMultipler { get; set; }

	private static int HelpMode { get; set; }
	private static int HelpAttackTargetId { get; set; }

	
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

	public virtual void init(int unitId, int nationRecno, int rankId = 0, int unitLoyalty = 0, int startXLoc = -1, int startYLoc = -1)
	{
		//------------ set basic vars -------------//

		NationId = nationRecno;
		Rank = rankId; // rank_id must be initialized before init_unit_id() as init_unit_id() may overwrite it
		// nation_contribution must be initialized before init_unit_id() as init_unit_id() may overwrite it
		NationContribution = 0;

		init_unit_id(unitId);

		GroupId = UnitArray.cur_group_id++;
		RaceId = UnitRes[UnitType].race_id;

		//------- init unit name ---------//

		if (RaceId != 0)
		{
			NameId = RaceRes[RaceId].get_new_name_id();
		}
		else //---- init non-human unit series no. ----//
		{
			if (NationId != 0)
				NameId = ++NationArray[NationId].last_unit_name_id_array[UnitType - 1];
			else
				NameId = 0;
		}

		//------- init ai_unit ----------//

		if (NationId != 0)
			AIUnit = NationArray[NationId].nation_type == NationBase.NATION_AI;
		else
			AIUnit = false;

		//----------------------------------------------//

		AIActionId = 0;
		ActionMisc = UnitConstants.ACTION_MISC_STOP;
		ActionMiscParam = 0;

		ActionMode2 = ActionMode = UnitConstants.ACTION_STOP;
		ActionPara2 = ActionParam = 0;
		ActionLocX2 = ActionLocY2 = ActionLocX = ActionLocY = -1;

		AttackRange = 0; //store the attack range of the current attack mode if the unit is ordered to attack

		LeaderId = 0;
		TeamId = 0;
		SelectedFlag = false;

		WaitingTerm = 0;
		Swapping = false; // indicate whether swapping is processed.

		SpyId = 0;

		RangeAttackLocX = -1;
		RangeAttackLocY = -1;

		//------- initialize way point vars -------//
		WayPoints.Clear();

		//---------- initialize game vars ----------//

		UnitMode = 0;
		UnitModeParam = 0;

		MaxHitPoints = UnitRes[UnitType].hit_points;
		HitPoints = MaxHitPoints;

		Loyalty = unitLoyalty;

		CanGuard = 0;
		CanAttack = true;
		ForceMove = false;
		AINoSuitableAction = false;
		CurPower = 0;
		MaxPower = 0;

		TotalReward = 0;

		HomeCampId = 0;

		IgnorePowerNation = 0;
		AggressiveMode = true; // the default mode is true

		//--------- init skill potential ---------//

		if (Misc.Random(10) == 0) // 1 out of 10 has a higher than normal potential in this skill
		{
			Skill.skill_potential = 50 + Misc.Random(51); // 50 to 100 potential
		}

		//------ initialize the base Sprite class -------//

		if (startXLoc >= 0)
			init_sprite(startXLoc, startYLoc);
		else
		{
			CurX = -1;
		}

		//------------- set attack_dir ------------//

		AttackDirection = FinalDir;

		//-------------- update loyalty -------------//

		update_loyalty();

		//--------------- init AI info -------------//

		if (AIUnit)
		{
			Nation nation = NationArray[NationId];

			if (Rank == RANK_GENERAL || Rank == RANK_KING)
				nation.add_general_info(SpriteId);

			switch (UnitRes[UnitType].unit_class)
			{
				case UnitConstants.UNIT_CLASS_CARAVAN:
					nation.add_caravan_info(SpriteId);
					break;

				case UnitConstants.UNIT_CLASS_SHIP:
					nation.add_ship_info(SpriteId);
					break;
			}
		}

		//----------- init derived class ----------//

		init_derived();
	}

	public override void Deinit()
	{
		if (UnitType == 0)
			return;

		//-------- if this is a king --------//

		if (NationId != 0)
		{
			if (Rank == RANK_KING) // check nation_recno because monster kings will die also.
			{
				king_die();
			}
			else if (Rank == RANK_GENERAL)
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

		if (Rank == RANK_GENERAL || Rank == RANK_KING)
		{
			foreach (Unit unit in UnitArray)
			{
				if (unit.LeaderId == SpriteId)
					unit.LeaderId = 0;
			}
		}

		//----- if this is a unit on a ship ------//

		if (UnitMode == UnitConstants.UNIT_MODE_ON_SHIP)
		{
			// the ship may have been destroyed at the same time. Actually when the ship is destroyed,
			// all units onboard are killed and this function is called.
			if (!UnitArray.IsDeleted(UnitModeParam))
			{
				Unit unit = UnitArray[UnitModeParam];
				((UnitMarine)unit).del_unit(SpriteId);
			}
		}

		//----- if this is a ship in the harbor -----//

		else if (UnitMode == UnitConstants.UNIT_MODE_IN_HARBOR)
		{
			// the ship may have been destroyed at the same time. Actually when the ship is destroyed,
			// all firms onboard are killed and this function is called.
			if (!FirmArray.IsDeleted(UnitModeParam))
			{
				Firm firm = FirmArray[UnitModeParam];
				((FirmHarbor)firm).del_hosted_ship(SpriteId);
			}
		}

		//----- if this unit is a constructor in a firm -------//

		else if (UnitMode == UnitConstants.UNIT_MODE_CONSTRUCT)
		{
			FirmArray[UnitModeParam].builder_recno = 0;
		}

		//-------- if this is a spy ---------//

		if (SpyId != 0)
		{
			SpyArray.DeleteSpy(SpyArray[SpyId]);
			SpyId = 0;
		}

		//---------- reset command ----------//

		if (Power.command_unit_recno == SpriteId)
			Power.reset_command();

		//-----------------------------------//

		deinit_unit_id();

		//-------- reset seek path ----------//

		ResetPath();

		//----- if cur_x == -1, the unit has not yet been hired -----//

		if (CurX >= 0)
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

		if (AIUnit)
		{
			if (!NationArray.IsDeleted(NationId))
			{
				Nation nation = NationArray[NationId];

				if (Rank == RANK_GENERAL || Rank == RANK_KING)
					nation.del_general_info(SpriteId);

				switch (UnitRes[UnitType].unit_class)
				{
					case UnitConstants.UNIT_CLASS_CARAVAN:
						nation.del_caravan_info(SpriteId);
						break;

					case UnitConstants.UNIT_CLASS_SHIP:
						nation.del_ship_info(SpriteId);
						break;
				}
			}
		}

		//-------------- reset unit_id ---------------//

		UnitType = 0;
	}

	public virtual void init_derived()
	{
	}

	public void init_sprite(int startXLoc, int startYLoc)
	{
		base.Init(UnitRes[UnitType].sprite_id, startXLoc, startYLoc);

		//--------------------------------------------------------------------//
		// move_to_?_loc is always the current location of the unit as
		// cur_action == SPRITE_IDLE
		//--------------------------------------------------------------------//
		OriginalActionMode = 0;
		AIOriginalTargetLocX = -1;

		AttackRange = 0;

		MoveToLocX = NextLocX;
		MoveToLocY = NextLocY;

		GoX = NextX;
		GoY = NextY;

		//-------- set the cargo_recno -------------//

		for (int h = 0, y = startYLoc; h < SpriteInfo.LocHeight; h++, y++)
		{
			for (int w = 0, x = startXLoc; w < SpriteInfo.LocWidth; w++, x++)
			{
				World.SetUnitId(x, y, MobileType, SpriteId);
			}
		}

		if (is_own() || (NationId != 0 && NationArray[NationId].is_allied_with_player))
		{
			World.Unveil(startXLoc, startYLoc, startXLoc + SpriteInfo.LocWidth - 1,
				startYLoc + SpriteInfo.LocHeight - 1);

			World.Visit(startXLoc, startYLoc, startXLoc + SpriteInfo.LocWidth - 1,
				startYLoc + SpriteInfo.LocHeight - 1, UnitRes[UnitType].visual_range,
				UnitRes[UnitType].visual_extend);
		}
	}

	public void deinit_sprite(bool keepSelected = false)
	{
		if (CurX == -1)
			return;

		//---- if this unit is led by a leader, only mobile units has leader_unit_recno assigned to a leader -----//
		// units are still considered mobile when boarding a ship

		if (LeaderId != 0 && UnitMode != UnitConstants.UNIT_MODE_ON_SHIP)
		{
			if (!UnitArray.IsDeleted(LeaderId)) // the leader unit may have been killed at the same time
				UnitArray[LeaderId].del_team_member(SpriteId);

			LeaderId = 0;
		}

		//-------- clear the cargo_recno ----------//

		for (int h = 0, y = NextLocY; h < SpriteInfo.LocHeight; h++, y++)
		{
			for (int w = 0, x = NextLocX; w < SpriteInfo.LocWidth; w++, x++)
			{
				World.SetUnitId(x, y, MobileType, 0);
			}
		}

		CurX = -1;

		//---- reset other parameters related to this unit ----//

		if (!keepSelected)
		{
			if (UnitArray.selected_recno == SpriteId)
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
		UnitType = unitId;

		UnitInfo unitInfo = UnitRes[UnitType];

		SpriteResId = unitInfo.sprite_id;
		SpriteInfo = SpriteRes[SpriteResId];

		MobileType = unitInfo.mobile_type;

		//--- if this unit is a weapon unit with multiple versions ---//

		set_combat_level(100); // set combat level default to 100, for human units, it will be adjusted later by individual functions

		int techLevel;
		if (NationId != 0 && unitInfo.unit_class == UnitConstants.UNIT_CLASS_WEAPON &&
		    (techLevel = unitInfo.nation_tech_level_array[NationId - 1]) > 0)
		{
			set_weapon_version(techLevel);
		}

		FixAttackInfo();

		//-------- set unit count ----------//

		if (NationId != 0)
		{
			if (Rank != RANK_KING)
				unitInfo.inc_nation_unit_count(NationId);

			if (Rank == RANK_GENERAL)
				unitInfo.inc_nation_general_count(NationId);
		}

		//--------- increase monster count ----------//

		if (UnitRes[UnitType].unit_class == UnitConstants.UNIT_CLASS_MONSTER)
			UnitRes.mobile_monster_count++;
	}

	public void deinit_unit_id()
	{
		//-----------------------------------------//

		UnitInfo unitInfo = UnitRes[UnitType];

		if (NationId != 0)
		{
			if (Rank != RANK_KING)
				unitInfo.dec_nation_unit_count(NationId);

			if (Rank == RANK_GENERAL)
				unitInfo.dec_nation_general_count(NationId);
		}

		//--------- if the unit is a spy ----------//
		//
		// A spy has double identity and is counted
		// by both the true controlling nation and
		// the deceiving nation.
		//
		//-----------------------------------------//

		if (SpyId != 0 && true_nation_recno() != NationId)
		{
			int trueNationRecno = true_nation_recno();

			if (Rank != RANK_KING)
				unitInfo.dec_nation_unit_count(trueNationRecno);

			if (Rank == RANK_GENERAL)
				unitInfo.dec_nation_general_count(trueNationRecno);
		}

		//--------- decrease monster count ----------//

		if (UnitRes[UnitType].unit_class == UnitConstants.UNIT_CLASS_MONSTER)
		{
			UnitRes.mobile_monster_count--;
		}
	}

	public void deinit_unit_mode()
	{
		//----- this unit was defending the town before it gets killed ----//

		if (UnitMode == UnitConstants.UNIT_MODE_DEFEND_TOWN)
		{
			if (!TownArray.IsDeleted(UnitModeParam))
			{
				Town town = TownArray[UnitModeParam];

				if (NationId == town.NationId)
					town.ReduceDefenderCount();
			}

			set_mode(0); // reset mode
		}

		//----- this is a monster unit defending its town ------//

		else if (UnitMode == UnitConstants.UNIT_MODE_MONSTER && UnitModeParam != 0)
		{
			if (((UnitMonster)this).monster_action_mode != UnitConstants.MONSTER_ACTION_DEFENSE)
				return;

			FirmMonster firmMonster = (FirmMonster)FirmArray[UnitModeParam];
			firmMonster.reduce_defender_count(Rank);
		}
	}

	public void InitFromWorker(Worker worker)
	{
		Skill.skill_id = worker.skill_id;
		Skill.skill_level = worker.skill_level;
		Skill.skill_level_minor = worker.skill_level_minor;
		set_combat_level(worker.combat_level);
		Skill.combat_level_minor = worker.combat_level_minor;
		HitPoints = worker.hit_points;
		Loyalty = worker.loyalty();
		Rank = worker.rank_id;

		if (UnitRes[UnitType].unit_class == UnitConstants.UNIT_CLASS_WEAPON)
		{
			set_weapon_version(worker.extra_para); // restore nation contribution
		}
		else if (RaceId != 0)
		{
			CurPower = worker.extra_para;

			if (CurPower < 0)
				CurPower = 0;

			if (CurPower > 150)
				CurPower = 150;
		}

		FixAttackInfo();

		if (worker.name_id != 0 && worker.race_id != 0) // if this worker is formerly an unit who has a name
			set_name(worker.name_id);

		//------ if the unit is a spy -------//

		if (worker.spy_recno != 0)
		{
			Spy spy = SpyArray[worker.spy_recno];

			SpyId = worker.spy_recno;
			AIUnit = spy.cloaked_nation_recno != 0 && NationArray[spy.cloaked_nation_recno].is_ai();
			set_name(spy.name_id); // set the name id. of this unit

			spy.set_place(Spy.SPY_MOBILE, SpriteId);
		}
	}

	//---- share the use of nation_contribution and total_reward ----//

	public int get_monster_id()
	{
		return NationContribution;
	}

	public void set_monster_id(int monsterId)
	{
		NationContribution = monsterId;
	}

	public int get_monster_soldier_id()
	{
		return TotalReward;
	}

	public void set_monster_soldier_id(int monsterSoldierId)
	{
		TotalReward = monsterSoldierId;
	}

	public int get_weapon_version()
	{
		return NationContribution;
	}

	public void set_weapon_version(int weaponVersion)
	{
		NationContribution = weaponVersion;
	}


	public int unit_power()
	{
		UnitInfo unitInfo = UnitRes[UnitType];

		if (unitInfo.unit_class == UnitConstants.UNIT_CLASS_WEAPON)
		{
			return (int)HitPoints + (unitInfo.weapon_power + get_weapon_version() - 1) * 15;
		}
		else
		{
			return (int)HitPoints;
		}
	}

	public int commander_power()
	{
		//---- if the commander is in a military camp -----//

		int commanderPower = 0;

		if (UnitMode == UnitConstants.UNIT_MODE_OVERSEE)
		{
			Firm firm = FirmArray[UnitModeParam];

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
			commanderPower = TeamInfo.Members.Count * 3; // 0 to 24
		}

		return commanderPower;
	}

	public int commanded_soldier_count()
	{
		if (Rank != RANK_GENERAL && Rank != RANK_KING)
			return 0;

		//--------------------------------------//

		int soldierCount = 0;

		if (is_visible())
		{
			soldierCount = TeamInfo.Members.Count - 1;

			if (soldierCount < 0) // member_count can be 0
				soldierCount = 0;
		}
		else
		{
			if (UnitMode == UnitConstants.UNIT_MODE_OVERSEE)
			{
				Firm firm = FirmArray[UnitModeParam];

				if (firm.firm_id == Firm.FIRM_CAMP) // it can be an overseer of a seat of powre
					soldierCount = firm.workers.Count;
			}
		}

		return soldierCount;
	}

	public virtual string unit_name(int withTitle = 1)
	{
		UnitInfo unitInfo = UnitRes[UnitType];

		//------------------------------------//
		string str = String.Empty;

		if (RaceId != 0)
		{
			if (withTitle != 0)
			{
				if (UnitMode == UnitConstants.UNIT_MODE_REBEL)
				{
					if (Rank == RANK_GENERAL)
					{
						str = "Rebel Leader ";
					}
				}
				else
				{
					if (Rank == RANK_KING)
					{
						str = "King ";
					}
					else if (Rank == RANK_GENERAL)
					{
						str = "General ";
					}
				}
			}

			if (Rank == RANK_KING) // use the player name
				str += NationArray[NationId].king_name();
			else
				str += RaceRes[RaceId].get_name(NameId);
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
				str += NameId; // name id is the series no. of the unit
			}
		}

		return str;
	}

	public void set_name(int newNameId)
	{
		//------- free up the existing name id. ------//

		RaceRes[RaceId].free_name_id(NameId);

		//------- set the new name id. ---------//

		NameId = newNameId;

		//-------- register usage of the new name id. ------//

		RaceRes[RaceId].use_name_id(NameId);
	}

	public void set_mode(int modeId, int modePara = 0)
	{
		UnitMode = modeId;
		UnitModeParam = modePara;
	}

	public int region_id()
	{
		if (is_visible())
		{
			return World.GetRegionId(NextLocX, NextLocY);
		}
		else
		{
			if (UnitMode == UnitConstants.UNIT_MODE_OVERSEE || UnitMode == UnitConstants.UNIT_MODE_CONSTRUCT)
				return FirmArray[UnitModeParam].region_id;
		}

		return 0;
	}

	public bool is_visible()
	{
		return CurX >= 0;
	}

	public override bool IsStealth()
	{
		return Config.fog_of_war && World.GetLoc(NextLocX, NextLocY).Visibility() < UnitRes[UnitType].stealth;
	}

	public void del_team_member(int unitRecno)
	{
		for (int i = TeamInfo.Members.Count - 1; i >= 0; i--)
		{
			if (TeamInfo.Members[i] == unitRecno)
			{
				TeamInfo.Members.RemoveAt(i);
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
		for (int i = TeamInfo.Members.Count - 1; i >= 0; i--)
		{
			int unitRecno = TeamInfo.Members[i];

			if (UnitArray.IsDeleted(unitRecno))
			{
				TeamInfo.Members.RemoveAt(i);
			}
		}
	}

	public bool is_civilian()
	{
		return RaceId > 0 && Skill.skill_id != Skill.SKILL_LEADING && UnitMode != UnitConstants.UNIT_MODE_REBEL;
	}

	public bool is_own()
	{
		return is_nation(NationArray.player_recno);
	}

	public bool is_own_spy()
	{
		return SpyId != 0 && SpyArray[SpyId].true_nation_recno == NationArray.player_recno;
	}

	public bool is_nation(int nationRecno)
	{
		if (NationId == nationRecno)
			return true;

		if (SpyId != 0 && SpyArray[SpyId].true_nation_recno == nationRecno)
			return true;

		return false;
	}

	// the true nation recno of the unit, taking care of the situation where the unit is a spy
	public int true_nation_recno()
	{
		return SpyId != 0 ? SpyArray[SpyId].true_nation_recno : NationId;
	}

	public virtual bool is_ai_all_stop()
	{
		return CurAction == SPRITE_IDLE &&
		       ActionMode == UnitConstants.ACTION_STOP &&
		       ActionMode2 == UnitConstants.ACTION_STOP &&
		       AIActionId == 0;
	}

	public bool get_cur_loc(out int xLoc, out int yLoc)
	{
		xLoc = -1;
		yLoc = -1;
		if (is_visible())
		{
			xLoc = NextLocX; // update location
			yLoc = NextLocY;
		}
		else if (UnitMode == UnitConstants.UNIT_MODE_OVERSEE ||
		         UnitMode == UnitConstants.UNIT_MODE_CONSTRUCT ||
		         UnitMode == UnitConstants.UNIT_MODE_IN_HARBOR)
		{
			Firm firm = FirmArray[UnitModeParam];

			xLoc = firm.center_x;
			yLoc = firm.center_y;
		}
		else if (UnitMode == UnitConstants.UNIT_MODE_ON_SHIP)
		{
			Unit unit = UnitArray[UnitModeParam];

			//xLoc = unit.next_x_loc();
			//yLoc = unit.next_y_loc();
			if (unit.is_visible())
			{
				xLoc = unit.NextLocX;
				yLoc = unit.NextLocY;
			}
			else
			{
				Firm firm = FirmArray[unit.UnitModeParam];
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
			xLoc = CurLocX;
			yLoc = CurLocY;
		}
		else if (UnitMode == UnitConstants.UNIT_MODE_OVERSEE ||
		         UnitMode == UnitConstants.UNIT_MODE_CONSTRUCT ||
		         UnitMode == UnitConstants.UNIT_MODE_IN_HARBOR)
		{
			Firm firm = FirmArray[UnitModeParam];
			xLoc = firm.center_x;
			yLoc = firm.center_y;
		}
		else if (UnitMode == UnitConstants.UNIT_MODE_ON_SHIP)
		{
			Unit unit = UnitArray[UnitModeParam];

			if (unit.is_visible())
			{
				xLoc = unit.CurLocX;
				yLoc = unit.CurLocY;
			}
			else
			{
				Firm firm = FirmArray[unit.UnitModeParam];
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
		if (LeaderId == 0)
			return false;

		if (UnitArray.IsDeleted(LeaderId))
		{
			LeaderId = 0;
			return false;
		}

		Unit leaderUnit = UnitArray[LeaderId];

		if (!leaderUnit.is_visible() && leaderUnit.UnitMode == UnitConstants.UNIT_MODE_CONSTRUCT)
			return false;

		int leaderXLoc, leaderYLoc, xLoc, yLoc;
		leaderUnit.get_cur_loc2(out leaderXLoc, out leaderYLoc);
		get_cur_loc2(out xLoc, out yLoc);

		if (leaderXLoc >= 0 && Misc.points_distance(xLoc, yLoc, leaderXLoc, leaderYLoc) <= GameConstants.EFFECTIVE_LEADING_DISTANCE)
			return LeaderId != 0;

		return false;
	}

	public bool is_unit_dead()
	{
		return (HitPoints <= 0.0 || ActionMode == UnitConstants.ACTION_DIE || CurAction == SPRITE_DIE);
	}

	public virtual void die()
	{
	}

	//------------- processing functions --------------------//

	public override void PreProcess()
	{
		//------ if all the hit points are lost, die now ------//
		if (HitPoints <= 0.0 && ActionMode != UnitConstants.ACTION_DIE)
		{
			set_die();

			if (AIActionId != 0 && NationId != 0)
				NationArray[NationId].action_failure(AIActionId, SpriteId);

			return;
		}

		if (Config.fog_of_war)
		{
			if (is_own() || (NationId != 0 && NationArray[NationId].is_allied_with_player))
			{
				World.Visit(NextLocX, NextLocY,
					NextLocX + SpriteInfo.LocWidth - 1, NextLocY + SpriteInfo.LocHeight - 1,
					UnitRes[UnitType].visual_range, UnitRes[UnitType].visual_extend);
			}
		}

		//--------- process action corresponding to action_mode ----------//

		switch (ActionMode)
		{
			case UnitConstants.ACTION_ATTACK_UNIT:
				//------------------------------------------------------------------//
				// if unit is in defense mode, check situation to follow the target
				// or return back to camp
				//------------------------------------------------------------------//
				if (ActionMode != ActionMode2)
				{
					if (ActionMode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET)
					{
						if (!defense_follow_target()) // false if abort attacking
							break; // cancel attack and go back to military camp
					}
					else if (ActionMode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET)
					{
						if (!defend_town_follow_target())
							break;
					}
					else if (ActionMode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
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

	public override void ProcessIdle() // derived function of Sprite
	{
		//---- if the unit is defending the town ----//

		switch (UnitMode)
		{
			case UnitConstants.UNIT_MODE_REBEL:
				if (ActionMode2 == UnitConstants.ACTION_STOP)
				{
					process_rebel(); // redirect to process_rebel for rebel units
					return;
				}

				break;

			case UnitConstants.UNIT_MODE_MONSTER:
				if (ActionMode2 == UnitConstants.ACTION_STOP)
				{
					if (UnitModeParam != 0)
					{
						if (!FirmArray.IsDeleted(UnitModeParam))
						{
							//-------- return to monster firm -----------//
							FirmMonster monsterFirm = (FirmMonster)FirmArray[UnitModeParam];
							assign(monsterFirm.loc_x1, monsterFirm.loc_y1);
							return;
						}
						else
						{
							UnitModeParam = 0;
						}
					}
				}

				break;
		}

		//------------- process way point ------------//
		if (ActionMode == UnitConstants.ACTION_STOP && ActionMode2 == UnitConstants.ACTION_STOP && WayPoints.Count > 0)
		{
			if (WayPoints.Count == 1)
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

		if (MatchDir())
		{
			if (!IsGuarding() && RaceId != 0) // only these units can move
			{
				if (Misc.Random(150) == 0) // change direction randomly
					SetDir(Misc.Random(8));
			}
		}
		else
		{
			return;
		}

		//------- call Sprite::process_idle() -------//

		base.ProcessIdle();

		//---------------------------------------------------------------------------//
		// reset idle blocked attacking unit.  If the previous condition is totally
		// blocked for attacking target, try again now
		// Note: reset blocked_edge is essentail for idle unit to reactivate attack
		// action
		//---------------------------------------------------------------------------//
		if (ActionMode >= UnitConstants.ACTION_ATTACK_UNIT && ActionMode <= UnitConstants.ACTION_ATTACK_WALL)
		{
			bool isAllZero = true;
			for (int i = 0; i < BlockedEdges.Length; i++)
			{
				if (BlockedEdges[i] != 0)
					isAllZero = false;
			}

			if (UnitArray.idle_blocked_unit_reset_count != 0 && !isAllZero)
			{
				UnitArray.idle_blocked_unit_reset_count = 0;
				for (int i = 0; i < BlockedEdges.Length; i++)
				{
					BlockedEdges[i] = 0;
				}
			}
		}

		//--------- reactivate action -----------//

		if (reactivate_idle_action())
			return; // true if an action is reactivated

		//----------- for ai unit idle -----------//

		// only detect attack when the unit is really idle
		if (ActionMode != UnitConstants.ACTION_STOP || ActionMode2 != UnitConstants.ACTION_STOP)
			return;

		if (!can_attack())
			return;

		//--- only detect attack if in aggressive mode or the unit is a monster ---//

		UnitInfo unitInfo = UnitRes[UnitType];

		if (unitInfo.unit_class == UnitConstants.UNIT_CLASS_MONSTER || AggressiveMode)
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
				int destX = NextLocX + xOffset;
				int destY = NextLocY + yOffset;

				if (destX < 0)
					destX = 0;
				else if (destX >= GameConstants.MapSize)
					destX = GameConstants.MapSize - 1;

				if (destY < 0)
					destY = 0;
				else if (destY >= GameConstants.MapSize)
					destY = GameConstants.MapSize - 1;

				MoveTo(destX, destY);
			}
		}
	}

	public override void ProcessMove() // derived function of Sprite
	{
		//----- if the sprite has reach the destintion ----//

		//--------------------------------------------------------//
		// if the unit reach its destination, then
		// cur_? == next_? == go_?
		//--------------------------------------------------------//

		if (CurX == GoX && CurY == GoY)
		{
			NextMove();
			if (CurAction != SPRITE_MOVE) // if next_move() is not successful, the movement has been stopped
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
			if (CurAction == SPRITE_MOVE && CurX == GoX && CurY == GoY)
				NextMove();
		}

		//--------- process the move, update sprite position ---------//
		//--------------------------------------------------------//
		// if the unit is moving, cur_?!=go_? and
		// if next_? != cur_?, the direction from cur_? to next_?
		// should equal to that from cur_? to go_?
		//--------------------------------------------------------//

		base.ProcessMove();

		if (CurX == GoX && CurY == GoY && CurAction == SPRITE_IDLE) // the sprite has reached its destination
		{
			MoveToLocX = NextLocX;
			MoveToLocY = NextLocY;
		}

		//--------------------------------------------------------//
		// after Sprite::process_move(), if the unit is blocked, its
		// cur_action is set to SPRITE_WAIT. Otherwise, its cur_action
		// is still SPRITE_MOVE.  Then cur_? != next_? if the unit
		// has not reached its destination.
		//--------------------------------------------------------//
	}

	public override void ProcessWait() // derived function of Sprite
	{
		if (!MatchDir())
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

		if (NextLocX == MoveToLocX && NextLocY == MoveToLocY && !Swapping)
		{
			TerminateMove();
			return; // terminate since already in destination
		}

		int stepMagn = MoveStepCoeff();
		int nextX = CurX + stepMagn * MoveXPixels[FinalDir];
		int nextY = CurY + stepMagn * MoveYPixels[FinalDir];

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
		Location location = World.GetLoc(x, y);
		bool blocked = !location.IsAccessible(MobileType) ||
		               (location.HasUnit(MobileType) && location.UnitId(MobileType) != SpriteId);

		if (!blocked || MoveActionCallFlag)
		{
			//--------- not blocked, continue to move --------//
			WaitingTerm = 0;
			set_move();
			CurFrame = 1;
			SetNext(nextX, nextY, -stepMagn, 1);
		}
		else
		{
			//------- blocked, call handle_blocked_move() ------//
			//loc = world.get_loc(blockedX, blockedY);
			HandleBlockedMove(location);
		}
	}

	// derived function of Sprite, for ship only
	public override void ProcessExtraMove()
	{
	}

	public override bool ProcessDie()
	{
		//--------- voice ------------//
		SERes.sound(CurLocX, CurLocY, CurFrame, 'S', SpriteResId, "DIE");

		//------------- add die effect on first frame --------- //
		if (CurFrame == 1 && UnitRes[UnitType].die_effect_id != 0)
		{
			EffectArray.AddEffect(UnitRes[UnitType].die_effect_id, CurX, CurY,
				SPRITE_DIE, CurDir, MobileType == UnitConstants.UNIT_AIR ? 8 : 2, 0);
		}

		//--------- next frame ---------//
		if (++CurFrame > SpriteInfo.Die.FrameCount)
			return true;

		return false;
	}

	public virtual void next_day()
	{
		int unitRecno = SpriteId;

		//------- functions for non-independent nations only ------//

		if (NationId != 0)
		{
			pay_expense();

			if (UnitArray.IsDeleted(unitRecno)) // if its hit points go down to 0, IsDeleted() will return 1.
				return;

			//------- update loyalty -------------//

			if (Info.TotalDays % 30 == SpriteId % 30)
			{
				update_loyalty();
			}

			//------- think about rebeling -------------//

			if (Info.TotalDays % 15 == SpriteId % 15)
			{
				if (think_betray())
					return;
			}
		}

		//------- recover from damage -------//

		if (Info.TotalDays % 15 == SpriteId % 15) // recover one point per two weeks
		{
			process_recover();
		}

		//------- restore cur_power --------//

		CurPower += 5;

		if (CurPower > MaxPower)
			CurPower = MaxPower;

		//------- king undie flag (for testing games only) --------//

		if (Config.king_undie_flag && Rank == RANK_KING && NationId != 0 && !NationArray[NationId].is_ai())
			HitPoints = MaxHitPoints;

		//-------- if aggresive_mode is 1 --------//

		if (NationId != 0 && is_visible())
			think_aggressive_action();


		if (Skill.combat_level > 100)
			Skill.combat_level = 100;
	}

	protected override void SetNext(int newNextX, int newNextY, int para = 0, int blockedChecked = 0)
	{
		int curNextXLoc = NextLocX;
		int curNextYLoc = NextLocY;
		int newNextXLoc = newNextX >> InternalConstants.CellWidthShift;
		int newNextYLoc = newNextY >> InternalConstants.CellHeightShift;

		if (curNextXLoc != newNextXLoc || curNextYLoc != newNextYLoc)
		{
			if (!MatchDir())
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
				Location location = World.GetLoc(x, y);
				blocked = !location.IsAccessible(MobileType) ||
				          (location.HasUnit(MobileType) && location.UnitId(MobileType) != SpriteId);
			} //else, then blockedChecked = 0

			//--- no change to next_x & next_y if the new next location is blocked ---//

			if (blocked)
			{
				SetCur(NextX, NextY); // align the sprite to 32x32 location when it stops

				//------ avoid infinitely looping in calling handle_blocked_move() ------//
				if (BlockedByMember != 0 || MoveActionCallFlag)
				{
					set_wait();
					BlockedByMember = 0;
				}
				else
				{
					Location location = World.GetLoc(x, y);
					HandleBlockedMove(location);
				}
			}
			else
			{
				if (para != 0)
				{
					//----------------------------------------------------------------------------//
					// calculate the result_path_dist as the unit move from one tile to another
					//----------------------------------------------------------------------------//
					_pathNodeDistance += para;
				}

				NextX = newNextX;
				NextY = newNextY;

				Swapping = false;
				BlockedByMember = 0;

				//---- move sprite_recno to the next location ------//

				y = curNextYLoc;
				for (int h = 0; h < SpriteInfo.LocHeight; h++, y++)
				{
					x = curNextXLoc;
					for (int w = 0; w < SpriteInfo.LocWidth; w++, x++)
					{
						World.SetUnitId(x, y, MobileType, 0);
					}
				}

				y = NextLocY;
				for (int h = 0; h < SpriteInfo.LocHeight; h++, y++)
				{
					x = NextLocX;
					for (int w = 0; w < SpriteInfo.LocWidth; w++, x++)
					{
						World.SetUnitId(x, y, MobileType, SpriteId);
					}
				}

				//--------- explore land ----------//

				if (!Config.explore_whole_map && is_own())
				{
					int xLoc1 = Math.Max(0, newNextXLoc - GameConstants.EXPLORE_RANGE);
					int yLoc1 = Math.Max(0, newNextYLoc - GameConstants.EXPLORE_RANGE);
					int xLoc2 = Math.Min(GameConstants.MapSize - 1, newNextXLoc + GameConstants.EXPLORE_RANGE);
					int yLoc2 = Math.Min(GameConstants.MapSize - 1, newNextYLoc + GameConstants.EXPLORE_RANGE);
					int exploreWidth = MoveStepCoeff() - 1;

					if (newNextYLoc < curNextYLoc) // if move upwards, explore upper area
						World.Explore(xLoc1, yLoc1, xLoc2, yLoc1 + exploreWidth);

					else if (newNextYLoc > curNextYLoc) // if move downwards, explore lower area
						World.Explore(xLoc1, yLoc2 - exploreWidth, xLoc2, yLoc2);

					if (newNextXLoc < curNextXLoc) // if move towards left, explore left area
						World.Explore(xLoc1, yLoc1, xLoc1 + exploreWidth, yLoc2);

					else if (newNextXLoc > curNextXLoc) // if move towards right, explore right area
						World.Explore(xLoc2 - exploreWidth, yLoc1, xLoc2, yLoc2);
				}
			}
		}
	}


	//------- functions for unit AI mode ---------//

	public bool think_aggressive_action()
	{
		//------ think about resuming the original action -----//

		// if it is currently attacking somebody, don't think about resuming the original action
		if (OriginalActionMode != 0 && CurAction != SPRITE_ATTACK)
		{
			return think_resume_original_action();
		}

		//---- think about attacking nearby units if this unit is attacking a town or a firm ---//

		// only in attack mode, as if the unit is still moving the target may be far away from the current position
		if (AggressiveMode && UnitMode == 0 && CurAction == SPRITE_ATTACK)
		{
			//--- only when the unit is currently attacking a firm or a town ---//

			if (ActionMode2 == UnitConstants.ACTION_ATTACK_FIRM || ActionMode2 == UnitConstants.ACTION_ATTACK_TOWN)
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

		int attackRange = Math.Max(AttackRange, 8);
		int attackScanRange = attackRange * 2 + 1;

		int xOffset, yOffset;
		int curXLoc = NextLocX, curYLoc = NextLocY;
		int regionId = World.GetRegionId(curXLoc, curYLoc);

		for (int i = 2; i < attackScanRange * attackScanRange; i++)
		{
			Misc.cal_move_around_a_point(i, attackScanRange, attackScanRange, out xOffset, out yOffset);

			int xLoc = curXLoc + xOffset;
			int yLoc = curYLoc + yOffset;

			xLoc = Math.Max(0, xLoc);
			xLoc = Math.Min(GameConstants.MapSize - 1, xLoc);

			yLoc = Math.Max(0, yLoc);
			yLoc = Math.Min(GameConstants.MapSize - 1, yLoc);

			Location location = World.GetLoc(xLoc, yLoc);

			if (location.RegionId != regionId)
				continue;

			//----- if there is a unit on the location ------//

			if (location.HasUnit(UnitConstants.UNIT_LAND))
			{
				int unitRecno = location.UnitId(UnitConstants.UNIT_LAND);

				if (UnitArray.IsDeleted(unitRecno))
					continue;

				if (UnitArray[unitRecno].NationId != NationId && idle_detect_unit_checking(unitRecno))
				{
					save_original_action();

					OriginalTargetLocX = xLoc;
					OriginalTargetLocY = yLoc;

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
			OriginalActionMode = 0;
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

		if (ActionMode2 != UnitConstants.ACTION_ATTACK_UNIT || UnitArray.IsDeleted(ActionPara2))
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

		Unit targetUnit = UnitArray[ActionPara2];

		int curDistance = Misc.points_distance(targetUnit.NextLocX, targetUnit.NextLocY,
			OriginalTargetLocX, OriginalTargetLocY);

		if (curDistance > AUTO_GUARD_CHASE_ATTACK_DISTANCE)
		{
			resume_original_action();
			return true;
		}

		return false;
	}

	public void save_original_action()
	{
		if (OriginalActionMode == 0)
		{
			OriginalActionMode = ActionMode2;
			OriginalActionParam = ActionPara2;
			OriginalActionLocX = ActionLocX2;
			OriginalActionLocY = ActionLocY2;
		}
	}

	public void resume_original_action()
	{
		if (OriginalActionMode == 0)
			return;

		//--------- If it is an attack action ---------//

		if (OriginalActionMode == UnitConstants.ACTION_ATTACK_UNIT ||
		    OriginalActionMode == UnitConstants.ACTION_ATTACK_FIRM ||
		    OriginalActionMode == UnitConstants.ACTION_ATTACK_TOWN)
		{
			resume_original_attack_action();
			return;
		}

		//--------------------------------------------//

		if (OriginalActionLocX < 0 || OriginalActionLocX >= GameConstants.MapSize ||
		    OriginalActionLocY < 0 || OriginalActionLocY >= GameConstants.MapSize)
		{
			OriginalActionMode = 0;
			return;
		}

		// use UnitArray.attack() instead of unit.attack_???() as we are unsure about what type of object the target is.
		List<int> selectedArray = new List<int>();
		selectedArray.Add(SpriteId);

		Location location = World.GetLoc(OriginalActionLocX, OriginalActionLocY);

		//--------- resume assign to town -----------//

		if (OriginalActionMode == UnitConstants.ACTION_ASSIGN_TO_TOWN && location.IsTown())
		{
			if (location.TownId() == OriginalActionParam &&
			    TownArray[OriginalActionParam].NationId == NationId)
			{
				UnitArray.assign(OriginalActionLocX, OriginalActionLocY, false,
					InternalConstants.COMMAND_AUTO, selectedArray);
			}
		}

		//--------- resume assign to firm ----------//

		else if (OriginalActionMode == UnitConstants.ACTION_ASSIGN_TO_FIRM && location.IsFirm())
		{
			if (location.FirmId() == OriginalActionParam &&
			    FirmArray[OriginalActionParam].nation_recno == NationId)
			{
				UnitArray.assign(OriginalActionLocX, OriginalActionLocY, false,
					InternalConstants.COMMAND_AUTO, selectedArray);
			}
		}

		//--------- resume build firm ---------//

		else if (OriginalActionMode == UnitConstants.ACTION_BUILD_FIRM)
		{
			if (World.CanBuildFirm(OriginalActionLocX, OriginalActionLocY,
				    OriginalActionParam, SpriteId) != 0)
			{
				build_firm(OriginalActionLocX, OriginalActionLocY,
					OriginalActionParam, InternalConstants.COMMAND_AUTO);
			}
		}

		//--------- resume settle ---------//

		else if (OriginalActionMode == UnitConstants.ACTION_SETTLE)
		{
			if (World.CanBuildTown(OriginalActionLocX, OriginalActionLocY, SpriteId))
			{
				UnitArray.settle(OriginalActionLocX, OriginalActionLocY, false,
					InternalConstants.COMMAND_AUTO, selectedArray);
			}
		}

		//--------- resume move ----------//

		else if (OriginalActionMode == UnitConstants.ACTION_MOVE)
		{
			UnitArray.MoveTo(OriginalActionLocX, OriginalActionLocY, false,
				selectedArray, InternalConstants.COMMAND_AUTO);
		}

		OriginalActionMode = 0;
	}

	public void resume_original_attack_action()
	{
		if (OriginalActionMode == 0)
			return;

		if (OriginalActionMode != UnitConstants.ACTION_ATTACK_UNIT &&
		    OriginalActionMode != UnitConstants.ACTION_ATTACK_FIRM &&
		    OriginalActionMode != UnitConstants.ACTION_ATTACK_TOWN)
		{
			OriginalActionMode = 0;
			return;
		}

		//--------------------------------------------//

		Location location = World.GetLoc(OriginalActionLocX, OriginalActionLocY);
		int targetNationRecno = -1;

		if (OriginalActionMode == UnitConstants.ACTION_ATTACK_UNIT && location.HasUnit(UnitConstants.UNIT_LAND))
		{
			int unitRecno = location.UnitId(UnitConstants.UNIT_LAND);

			if (unitRecno == OriginalActionParam)
				targetNationRecno = UnitArray[unitRecno].NationId;
		}
		else if (OriginalActionMode == UnitConstants.ACTION_ATTACK_FIRM && location.IsFirm())
		{
			int firmRecno = location.FirmId();

			if (firmRecno == OriginalActionParam)
				targetNationRecno = FirmArray[firmRecno].nation_recno;
		}
		else if (OriginalActionMode == UnitConstants.ACTION_ATTACK_TOWN && location.IsTown())
		{
			int townRecno = location.TownId();

			if (townRecno == OriginalActionParam)
				targetNationRecno = TownArray[townRecno].NationId;
		}

		//----- the original target is no longer valid ----//

		if (targetNationRecno == -1)
		{
			OriginalActionMode = 0;
			return;
		}

		//---- only resume attacking the target if the target nation is at war with us currently ---//

		if (targetNationRecno == 0 || (targetNationRecno != NationId &&
		                           NationArray[NationId].get_relation_status(targetNationRecno) ==
		                           NationBase.NATION_HOSTILE))
		{
			// use UnitArray.attack() instead of unit.attack_???() as we are unsure about what type of object the target is.
			List<int> selectedArray = new List<int>();
			selectedArray.Add(SpriteId);

			UnitArray.attack(OriginalActionLocX, OriginalActionLocY, false, selectedArray,
				InternalConstants.COMMAND_AI, 0);
		}

		OriginalActionMode = 0;
	}

	public void ask_team_help_attack(Unit attackerUnit)
	{
		//--- if the attacking unit is our unit (this can happen if the unit is porcupine) ---//

		if (attackerUnit.NationId == NationId)
			return;

		//-----------------------------------------//

		int leaderUnitRecno = SpriteId;

		if (LeaderId != 0) // if the current unit is a soldier, get its leader's recno
			leaderUnitRecno = LeaderId;

		TeamInfo teamInfo = UnitArray[leaderUnitRecno].TeamInfo;

		for (int i = teamInfo.Members.Count - 1; i >= 0; i--)
		{
			int unitRecno = teamInfo.Members[i];

			if (UnitArray.IsDeleted(unitRecno))
				continue;

			Unit unit = UnitArray[unitRecno];

			if (unit.CurAction == SPRITE_IDLE && unit.is_visible())
			{
				unit.attack_unit(attackerUnit.SpriteId, 0, 0, true);
				return;
			}

			if (ConfigAdv.unit_ai_team_help && (unit.AIUnit || NationId == 0) &&
			    unit.is_visible() && unit.HitPoints > 15.0 &&
			    (unit.ActionMode == UnitConstants.ACTION_STOP ||
			     unit.ActionMode == UnitConstants.ACTION_ASSIGN_TO_FIRM ||
			     unit.ActionMode == UnitConstants.ACTION_ASSIGN_TO_TOWN ||
			     unit.ActionMode == UnitConstants.ACTION_SETTLE))
			{
				unit.attack_unit(attackerUnit.SpriteId, 0, 0, true);
				return;
			}
		}
	}


	public bool return_camp()
	{
		if (HomeCampId == 0)
			return false;

		Firm firm = FirmArray[HomeCampId];

		if (firm.region_id != region_id())
			return false;

		//--------- assign now ---------//

		assign(firm.loc_x1, firm.loc_y1);

		ForceMove = true;

		return CurAction != SPRITE_IDLE;
	}

	//----------- parameters resetting functions ------------//
	public virtual void stop(int preserveAction = 0)
	{
		//------------- reset vars ------------//
		if (ActionMode2 != UnitConstants.ACTION_MOVE)
			ResetWayPoints();

		ResetPath();

		//-------------- keep action or not --------------//
		switch (preserveAction)
		{
			case 0:
			case UnitConstants.KEEP_DEFENSE_MODE:
				reset_action_para();
				RangeAttackLocX = RangeAttackLocY = -1; // should set here or reset for attack
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

		WaitingTerm = 0; // for idle_detect_attack(), oscillate between 0 and 1

		//----------------- update parameters ----------------//
		switch (CurAction)
		{
			//----- if the unit is moving right now, ask it to stop as soon as possible -----//

			case SPRITE_READY_TO_MOVE:
				set_idle();
				break;

			case SPRITE_TURN:
			case SPRITE_WAIT:
				GoX = NextX;
				GoY = NextY;
				MoveToLocX = NextLocX;
				MoveToLocY = NextLocY;
				FinalDir = CurDir;
				TurnDelay = 0;
				set_idle();
				break;

			case SPRITE_SHIP_EXTRA_MOVE:
				UnitMarine ship = (UnitMarine)this;
				switch (ship.extra_move_in_beach)
				{
					case UnitMarine.NO_EXTRA_MOVE:
						if (CurX == NextX && CurY == NextY)
						{
							GoX = NextX;
							GoY = NextY;
							MoveToLocX = NextLocX;
							MoveToLocY = NextLocY;
							set_idle();
							return;
						}

						break;

					case UnitMarine.EXTRA_MOVING_IN:
						if (CurX == NextX && CurY == NextY && (CurX != GoX || CurY != GoY))
						{
							// not yet move although location is chosed
							ship.extra_move_in_beach = UnitMarine.NO_EXTRA_MOVE;
						}

						break;

					case UnitMarine.EXTRA_MOVING_OUT:
						if (CurX == NextX && CurY == NextY && (CurX != GoX || CurY != GoY))
						{
							// not yet move although location is chosed
							ship.extra_move_in_beach = UnitMarine.EXTRA_MOVE_FINISH;
						}

						break;

					case UnitMarine.EXTRA_MOVE_FINISH:
						break;
				}

				GoX = NextX;
				GoY = NextY;
				MoveToLocX = NextLocX;
				MoveToLocY = NextLocY;
				break;

			case SPRITE_MOVE:
				GoX = NextX;
				GoY = NextY;
				MoveToLocX = NextLocX;
				MoveToLocY = NextLocY;
				if (CurX == NextX && CurY == NextY)
					set_idle();
				break;

			//--- if its current action is SPRITE_ATTACK, stop immediately ---//

			case SPRITE_ATTACK:
				SetNext(CurX, CurY, 0, 1); //********** BUGHERE
				GoX = NextX;
				GoY = NextY;
				MoveToLocX = NextLocX;
				MoveToLocY = NextLocY;
				set_idle();

				CurFrame = 1;
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

		ForceMove = false;
		AINoSuitableAction = false;

		if (preserveAction == 0)
		{
			OriginalActionMode = 0;
			AIOriginalTargetLocX = -1;

			if (AIActionId != 0 && NationId != 0)
				NationArray[NationId].action_failure(AIActionId, SpriteId);
		}
	}

	public void reset_action_para() // reset action_mode parameters
	{
		ActionMode = UnitConstants.ACTION_STOP;
		ActionLocX = ActionLocY = -1;
		ActionParam = 0;
	}

	public void reset_action_para2(int keepMode = 0) // reset action_mode2 parameters
	{
		if (keepMode != UnitConstants.KEEP_DEFENSE_MODE || !in_any_defense_mode())
		{
			ActionMode2 = UnitConstants.ACTION_STOP;
			ActionPara2 = 0;
			ActionLocX2 = ActionLocY2 = -1;
		}
		else
		{
			switch (UnitMode)
			{
				case UnitConstants.UNIT_MODE_DEFEND_TOWN:
					if (ActionMode2 != UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET)
						defend_town_detect_target();
					break;

				case UnitConstants.UNIT_MODE_REBEL:
					if (ActionMode2 != UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET)
						defense_detect_target();
					break;

				case UnitConstants.UNIT_MODE_MONSTER:
					if (ActionMode2 != UnitConstants.ACTION_MONSTER_DEFEND_DETECT_TARGET)
						monster_defend_detect_target();
					break;
			}
		}
	}

	public void reset_action_misc_para()
	{
		ActionMisc = UnitConstants.ACTION_MISC_STOP;
		ActionMiscParam = 0;
	}

	public void enable_force_move()
	{
		ForceMove = true;
	}

	public void disable_force_move()
	{
		ForceMove = false;
	}

	public void gain_experience()
	{
		if (UnitRes[UnitType].unit_class != UnitConstants.UNIT_CLASS_HUMAN)
			return; // no experience gain if unit is not human

		//---- increase the unit's contribution to the nation ----//

		if (NationContribution < GameConstants.MAX_NATION_CONTRIBUTION)
		{
			NationContribution++;
		}

		//------ increase combat skill -------//

		inc_minor_combat_level(6);

		//--- if this is a soldier led by a commander, increase the leadership of its commander -----//

		if (is_leader_in_range())
		{
			Unit leaderUnit = UnitArray[LeaderId];

			leaderUnit.inc_minor_skill_level(1);

			//-- give additional increase if the leader has skill potential on leadership --//

			if (leaderUnit.Skill.skill_potential > 0)
			{
				if (Misc.Random(10 - leaderUnit.Skill.skill_potential / 10) == 0)
					leaderUnit.inc_minor_skill_level(5);
			}

			//--- if this soldier has leadership potential and is led by a commander ---//
			//--- he learns leadership by watching how the commander commands the troop --//

			if (Skill.skill_potential > 0)
			{
				if (Misc.Random(10 - Skill.skill_potential / 10) == 0)
					inc_minor_skill_level(5);
			}
		}
	}

	protected void pay_expense()
	{
		if (NationId == 0)
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

		if (SpyId != 0)
		{
			if (is_visible()) // the cost will be deducted in SpyArray
				return;

			if (UnitMode == UnitConstants.UNIT_MODE_OVERSEE &&
			    FirmArray[UnitModeParam].nation_recno == true_nation_recno())
			{
				return;
			}
		}

		//---------- if it's a human unit -----------//
		//
		// The unit is paid even during its training period in a town
		//
		//-------------------------------------------//

		Nation nation = NationArray[NationId];

		if (UnitRes[UnitType].race_id != 0)
		{
			//---------- reduce cash -----------//

			if (nation.cash > 0)
			{
				if (Rank == RANK_SOLDIER)
					nation.add_expense(NationBase.EXPENSE_MOBILE_UNIT,
						GameConstants.SOLDIER_YEAR_SALARY / 365.0, true);

				if (Rank == RANK_GENERAL)
					nation.add_expense(NationBase.EXPENSE_GENERAL,
						GameConstants.GENERAL_YEAR_SALARY / 365.0, true);
			}
			else // decrease loyalty if the nation cannot pay the unit
			{
				change_loyalty(-1);
			}

			//---------- reduce food -----------//

			if (UnitRes[UnitType].race_id != 0) // if it's a human unit
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

				switch (UnitRes[UnitType].unit_class)
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

				nation.add_expense(expenseType, UnitRes[UnitType].year_cost / 365.0, true);
			}
			else // decrease hit points if the nation cannot pay the unit
			{
				// Even when caravans are not paid, they still stay in your service.
				if (UnitRes[UnitType].unit_class != UnitConstants.UNIT_CLASS_CARAVAN)
				{
					if (HitPoints > 0.0)
					{
						HitPoints--;

						//--- when the hit points drop to zero and the unit is destroyed ---//

						if (HitPoints <= 0.0)
						{
							if (NationId == NationArray.player_recno)
							{
								int unitClass = UnitRes[UnitType].unit_class;

								if (unitClass == UnitConstants.UNIT_CLASS_WEAPON)
									NewsArray.weapon_ship_worn_out(UnitType, get_weapon_version());

								else if (unitClass == UnitConstants.UNIT_CLASS_SHIP)
									NewsArray.weapon_ship_worn_out(UnitType, 0);
							}

							HitPoints = 0.0;
						}
					}
				}
			}
		}
	}
	
	public int CampInfluence()
	{
		Nation nation = NationArray[NationId]; // nation of the unit

		int thisInfluence = Skill.get_skill(Skill.SKILL_LEADING) * 2 / 3; // 66% of the leadership

		if (RaceRes.is_same_race(nation.race_id, RaceId))
			thisInfluence += thisInfluence / 3; // 33% bonus if the king's race is also the same as the general

		thisInfluence += (int)(nation.reputation / 2.0);

		thisInfluence = Math.Min(100, thisInfluence);

		return thisInfluence;
	}

	public virtual double actual_damage()
	{
		AttackInfo attackInfo = AttackInfos[CurAttack];

		int attackDamage = attackInfo.attack_damage;

		//-------- pierce damage --------//

		attackDamage += Misc.Random(3) + attackInfo.pierce_damage
			* Misc.Random(Skill.combat_level - attackInfo.combat_level)
			/ (100 - attackInfo.combat_level);

		//--- if this unit is led by a general, its attacking ability is influenced by the general ---//
		//
		// The unit's attacking ability is increased by a percentage equivalent to
		// the leader unit's leadership.
		//
		//------------------------------------------------------------------------//

		if (is_leader_in_range())
		{
			Unit leaderUnit = UnitArray[LeaderId];
			attackDamage += attackDamage * leaderUnit.Skill.skill_level / 100;
		}

		// lessen all attacking damages, thus slowing down all battles.
		return (double)attackDamage / (double)InternalConstants.ATTACK_SLOW_DOWN;
	}

	public bool nation_can_attack(int nationRecno) // can this nation be attacked, no if alliance or etc..
	{
		if (!AIUnit)
		{
			return nationRecno != NationId; // able to attack all nation except our own nation
		}
		else if (NationId == nationRecno)
			return false; // ai unit don't attack its own nation, except special order

		if (NationId == 0 || nationRecno == 0)
			return true; // true if either nation is independent

		Nation nation = NationArray[NationId];

		int relationStatus = nation.get_relation_status(nationRecno);
		if (relationStatus == NationBase.NATION_FRIENDLY || relationStatus == NationBase.NATION_ALLIANCE)
			return false;

		return true;
	}

	public bool independent_nation_can_attack(int nationRecno)
	{
		switch (UnitMode)
		{
			case UnitConstants.UNIT_MODE_DEFEND_TOWN:
				if (TownArray.IsDeleted(UnitModeParam))
					return false; // don't attack independent unit with no town

				Town town = TownArray[UnitModeParam];

				if (!town.IsHostileNation(nationRecno))
					return false; // false if the independent unit don't want to attack us

				break;

			case UnitConstants.UNIT_MODE_REBEL:
				if (RebelArray.IsDeleted(UnitModeParam))
					return false;

				Rebel rebel = RebelArray[UnitModeParam];

				if (!rebel.is_hostile_nation(nationRecno))
					return false;

				break;

			case UnitConstants.UNIT_MODE_MONSTER:
				if (UnitModeParam == 0)
					return nationRecno != 0; // attack anything that is not independent

				FirmMonster firmMonster = (FirmMonster)FirmArray[UnitModeParam];

				if (!firmMonster.is_hostile_nation(nationRecno))
					return false; // false if the independent unit don't want to attack us

				break;

			default:
				return false;
		}

		return true;
	}

	//---------- embark to ship and other ship functions ---------//
	public void assign_to_ship(int destX, int destY, int shipRecno, int miscNo = 0)
	{
		//----------------------------------------------------------------//
		// return if the unit is dead
		//----------------------------------------------------------------//
		if (HitPoints <= 0.0 || ActionMode == UnitConstants.ACTION_DIE || CurAction == SPRITE_DIE)
			return;

		if (UnitArray.IsDeleted(shipRecno))
			return;

		//----------------------------------------------------------------//
		// action_mode2: checking for equal action or idle action
		//----------------------------------------------------------------//
		if (ActionMode2 == UnitConstants.ACTION_ASSIGN_TO_SHIP &&
		    ActionPara2 == shipRecno && ActionLocX2 == destX && ActionLocY2 == destY)
		{
			if (CurAction != SPRITE_IDLE)
				return;
		}
		else
		{
			//----------------------------------------------------------------//
			// action_mode2: store new order
			//----------------------------------------------------------------//
			ActionMode2 = UnitConstants.ACTION_ASSIGN_TO_SHIP;
			ActionPara2 = shipRecno;
			ActionLocX2 = destX;
			ActionLocY2 = destY;
		}

		//----- order the sprite to stop as soon as possible -----//
		stop(); // new order

		Unit ship = UnitArray[shipRecno];
		int shipXLoc = ship.NextLocX;
		int shipYLoc = ship.NextLocY;
		bool resultXYLocWritten = false;
		int resultXLoc = -1, resultYLoc = -1;
		if (miscNo == 0)
		{
			//-------- find a suitable location since no offset location is given ---------//
			if (Math.Abs(shipXLoc - ActionLocX2) <= 1 && Math.Abs(shipYLoc - ActionLocY2) <= 1)
			{
				Location loc = World.GetLoc(NextLocX, NextLocY);
				int regionId = loc.RegionId;
				for (int i = 2; i <= 9; i++)
				{
					int xShift, yShift;
					Misc.cal_move_around_a_point(i, 3, 3, out xShift, out yShift);
					int checkXLoc = shipXLoc + xShift;
					int checkYLoc = shipYLoc + yShift;
					if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
						continue;

					loc = World.GetLoc(checkXLoc, checkYLoc);
					if (loc.RegionId != regionId)
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
				resultXLoc = ActionLocX2;
				resultYLoc = ActionLocY2;
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
		int curXLoc = NextLocX;
		int curYLoc = NextLocY;
		if ((curXLoc != destX || curYLoc != destY) &&
		    (Math.Abs(shipXLoc - curXLoc) > 1 || Math.Abs(shipYLoc - curYLoc) > 1))
			Search(resultXLoc, resultYLoc, 1);

		//-------- set action parameters ----------//
		ActionMode = UnitConstants.ACTION_ASSIGN_TO_SHIP;
		ActionParam = shipRecno;
		ActionLocX = destX;
		ActionLocY = destY;
	}

	public void ship_to_beach(int destX, int destY, out int finalDestX, out int finalDestY) // for ship only
	{
		//----------------------------------------------------------------//
		// change to move_to if the unit is dead
		//----------------------------------------------------------------//
		if (HitPoints <= 0.0 || ActionMode == UnitConstants.ACTION_DIE || CurAction == SPRITE_DIE)
		{
			MoveTo(destX, destY, 1);
			finalDestX = finalDestY = -1;
			return;
		}

		//----------------------------------------------------------------//
		// change to move_to if the ship cannot carry units
		//----------------------------------------------------------------//
		if (UnitRes[UnitType].carry_unit_capacity <= 0)
		{
			MoveTo(destX, destY, 1);
			finalDestX = finalDestY = -1;
			return;
		}

		//----------------------------------------------------------------//
		// calculate new destination
		//----------------------------------------------------------------//
		int curXLoc = NextLocX;
		int curYLoc = NextLocY;
		int resultXLoc, resultYLoc;

		stop();

		if (Math.Abs(destX - curXLoc) > 1 || Math.Abs(destY - curYLoc) > 1)
		{
			//-----------------------------------------------------------------------------//
			// get a suitable location in the territory as a reference location
			//-----------------------------------------------------------------------------//
			Location loc = World.GetLoc(destX, destY);
			int regionId = loc.RegionId;
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

				loc = World.GetLoc(x, y);
				if (loc.RegionId == regionId)
				{
					if (loc.Walkable())
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
			if (!ShipToBeachPathEdit(ref resultXLoc, ref resultYLoc, regionId))
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

		ActionMode = ActionMode2 = UnitConstants.ACTION_SHIP_TO_BEACH;
		ActionParam = ActionPara2 = 0;
		finalDestX = ActionLocX = ActionLocX2 = resultXLoc;
		finalDestY = ActionLocY = ActionLocY2 = resultYLoc;
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
		if (HitPoints <= 0.0 || ActionMode == UnitConstants.ACTION_DIE || CurAction == SPRITE_DIE)
			return;

		//----------------------------------------------------------------//
		// location is blocked, cannot build. so move there instead
		//----------------------------------------------------------------//
		if (World.CanBuildFirm(buildXLoc, buildYLoc, firmId, SpriteId) == 0)
		{
			//reset_action_para2();
			MoveTo(buildXLoc, buildYLoc);
			return;
		}

		//----------------------------------------------------------------//
		// different territory
		//----------------------------------------------------------------//

		int harborDir = World.CanBuildFirm(buildXLoc, buildYLoc, firmId, SpriteId);
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
					MoveTo(buildXLoc, buildYLoc);
					return;
			}

			if (World.GetLoc(NextLocX, NextLocY).RegionId != World.GetLoc(goX, goY).RegionId)
			{
				MoveTo(buildXLoc, buildYLoc);
				return;
			}
		}
		else
		{
			if (World.GetLoc(NextLocX, NextLocY).RegionId != World.GetLoc(buildXLoc, buildYLoc).RegionId)
			{
				MoveTo(buildXLoc, buildYLoc);
				return;
			}
		}

		//----------------------------------------------------------------//
		// action_mode2: checking for equal action or idle action
		//----------------------------------------------------------------//
		if (ActionMode2 == UnitConstants.ACTION_BUILD_FIRM &&
		    ActionPara2 == firmId && ActionLocX2 == buildXLoc && ActionLocY2 == buildYLoc)
		{
			if (CurAction != SPRITE_IDLE)
				return;
		}
		else
		{
			//----------------------------------------------------------------//
			// action_mode2: store new order
			//----------------------------------------------------------------//
			ActionMode2 = UnitConstants.ACTION_BUILD_FIRM;
			ActionPara2 = firmId;
			ActionLocX2 = buildXLoc;
			ActionLocY2 = buildYLoc;
		}

		//----- order the sprite to stop as soon as possible -----//
		stop(); // new order

		//---------------- define parameters -------------------//
		FirmInfo firmInfo = FirmRes[firmId];
		int firmWidth = firmInfo.loc_width;
		int firmHeight = firmInfo.loc_height;

		if (!is_in_surrounding(MoveToLocX, MoveToLocY, SpriteInfo.LocWidth,
			    buildXLoc, buildYLoc, firmWidth, firmHeight))
		{
			//----------- not in the firm surrounding ---------//
			SetMoveToSurround(buildXLoc, buildYLoc, firmWidth, firmHeight, UnitConstants.BUILDING_TYPE_FIRM_BUILD, firmId);
		}
		else
		{
			//------- the unit is in the firm surrounding -------//
			SetCur(NextX, NextY);
			SetDir(MoveToLocX, MoveToLocY, buildXLoc + firmWidth / 2, buildYLoc + firmHeight / 2);
		}

		//----------- set action to build the firm -----------//
		ActionMode = UnitConstants.ACTION_BUILD_FIRM;
		ActionParam = firmId;
		ActionLocX = buildXLoc;
		ActionLocY = buildYLoc;
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

		if (MoveToLocX == burnXLoc && MoveToLocY == burnYLoc)
			return; // should not burn the unit itself

		//----------------------------------------------------------------//
		// return if the unit is dead
		//----------------------------------------------------------------//
		if (HitPoints <= 0.0 || ActionMode == UnitConstants.ACTION_DIE || CurAction == SPRITE_DIE)
			return;

		//----------------------------------------------------------------//
		// move there instead if ordering to different territory
		//----------------------------------------------------------------//
		if (World.GetLoc(NextLocX, NextLocY).RegionId != World.GetLoc(burnXLoc, burnYLoc).RegionId)
		{
			MoveTo(burnXLoc, burnYLoc);
			return;
		}

		//----------------------------------------------------------------//
		// action_mode2: checking for equal action or idle action
		//----------------------------------------------------------------//
		if (ActionMode2 == UnitConstants.ACTION_BURN && ActionLocX2 == burnXLoc && ActionLocY2 == burnYLoc)
		{
			if (CurAction != SPRITE_IDLE)
				return;
		}
		else
		{
			//----------------------------------------------------------------//
			// action_mode2: store new order
			//----------------------------------------------------------------//
			ActionMode2 = UnitConstants.ACTION_BURN;
			ActionPara2 = 0;
			ActionLocX2 = burnXLoc;
			ActionLocY2 = burnYLoc;
		}

		//----- order the sprite to stop as soon as possible -----//
		stop(); // new order

		if (Math.Abs(burnXLoc - NextLocX) > 1 || Math.Abs(burnYLoc - NextLocY) > 1)
		{
			//--- if the unit is not in the burning surrounding location, move there first ---//
			Search(burnXLoc, burnYLoc, 1, SeekPath.SEARCH_MODE_A_UNIT_IN_GROUP);

			if (MoveToLocX != burnXLoc || MoveToLocY != burnYLoc) // cannot reach the destination
			{
				ActionMode = UnitConstants.ACTION_BURN;
				ActionParam = 0;
				ActionLocX = burnXLoc;
				ActionLocY = burnYLoc;
				return; // just move to the closest location returned by shortest path searching
			}
		}
		else
		{
			if (CurX == NextX && CurY == NextY)
				SetDir(NextLocX, NextLocY, burnXLoc, burnYLoc);
		}

		//--------------------------------------------------------//
		// edit the result path such that the unit can reach the burning location surrounding
		//--------------------------------------------------------//
		if (PathNodes.Count > 0)
		{
			//--------------------------------------------------------//
			// there should be at least two nodes, and should take at least two steps to the destination
			//--------------------------------------------------------//

			int lastNode1 = PathNodes[^1]; // the last node
			World.GetLocXAndLocY(lastNode1, out int lastNode1LocX, out int lastNode1LocY);
			int lastNode2 = PathNodes[^2]; // the node before the last node
			World.GetLocXAndLocY(lastNode2, out int lastNode2LocX, out int lastNode2LocY);

			int vX = lastNode1LocX - lastNode2LocX; // get the vectors direction
			int vY = lastNode1LocY - lastNode2LocY;
			int vDirX = (vX != 0) ? vX / Math.Abs(vX) : 0;
			int vDirY = (vY != 0) ? vY / Math.Abs(vY) : 0;

			if (PathNodes.Count > 2) // go_? should not be the burning location 
			{
				if (Math.Abs(vX) > 1 || Math.Abs(vY) > 1)
				{
					lastNode1LocX -= vDirX;
					lastNode1LocY -= vDirY;
					PathNodes[^1] = World.GetMatrixIndex(lastNode1LocX, lastNode1LocY);

					MoveToLocX = lastNode1LocX;
					MoveToLocY = lastNode1LocY;
				}
				else // move only one step
				{
					//TODO check
					PathNodes.RemoveAt(PathNodes.Count - 1); // remove a node
					MoveToLocX = lastNode2LocX;
					MoveToLocY = lastNode2LocY;
				}
			}
			else // go_? may be the burning location
			{
				lastNode1LocX -= vDirX;
				lastNode1LocY -= vDirY;
				PathNodes[^1] = World.GetMatrixIndex(lastNode1LocX, lastNode1LocY);

				if (GoX >> InternalConstants.CellWidthShift == burnXLoc && GoY >> InternalConstants.CellHeightShift == burnYLoc)
				{
					// go_? is the burning location
					//--- edit parameters such that only moving to the nearby location to do the action ---//

					GoX = lastNode1LocX * InternalConstants.CellWidth;
					GoY = lastNode1LocY * InternalConstants.CellHeight;
				}
				//else the unit is still doing something else, no action here

				MoveToLocX = lastNode1LocX;
				MoveToLocY = lastNode1LocY;
			}

			//--------------------------------------------------------------//
			// reduce the result_path_dist by 1
			//--------------------------------------------------------------//
			_pathNodeDistance--;
		}

		//-- set action if the burning location can be reached, otherwise just move nearby --//
		ActionMode = UnitConstants.ACTION_BURN;
		ActionParam = 0;
		ActionLocX = burnXLoc;
		ActionLocY = burnYLoc;
	}

	public void settle(int settleXLoc, int settleYLoc, int curSettleUnitNum = 1)
	{
		//----------------------------------------------------------------//
		// return if the unit is dead
		//----------------------------------------------------------------//
		if (HitPoints <= 0.0 || ActionMode == UnitConstants.ACTION_DIE || CurAction == SPRITE_DIE)
			return;

		//---------- no settle for non-human -----------//
		if (UnitRes[UnitType].unit_class != UnitConstants.UNIT_CLASS_HUMAN)
			return;

		//----------------------------------------------------------------//
		// move there if cannot settle
		//----------------------------------------------------------------//
		if (!World.CanBuildTown(settleXLoc, settleYLoc, SpriteId))
		{
			Location loc = World.GetLoc(settleXLoc, settleYLoc);
			if (loc.IsTown() && TownArray[loc.TownId()].NationId == NationId)
				assign(settleXLoc, settleYLoc);
			else
				MoveTo(settleXLoc, settleYLoc);
			return;
		}

		//----------------------------------------------------------------//
		// move there if location is in different territory
		//----------------------------------------------------------------//
		if (World.GetLoc(NextLocX, NextLocY).RegionId != World.GetLoc(settleXLoc, settleYLoc).RegionId)
		{
			MoveTo(settleXLoc, settleYLoc);
			return;
		}

		//----------------------------------------------------------------//
		// action_mode2: checking for equal action or idle action
		//----------------------------------------------------------------//
		if (ActionMode2 == UnitConstants.ACTION_SETTLE && ActionLocX2 == settleXLoc && ActionLocY2 == settleYLoc)
		{
			if (CurAction != SPRITE_IDLE)
				return;
		}
		else
		{
			//----------------------------------------------------------------//
			// action_mode2: store new order
			//----------------------------------------------------------------//
			ActionMode2 = UnitConstants.ACTION_SETTLE;
			ActionPara2 = 0;
			ActionLocX2 = settleXLoc;
			ActionLocY2 = settleYLoc;
		}

		//----- order the sprite to stop as soon as possible -----//
		stop(); // new order

		if (!is_in_surrounding(MoveToLocX, MoveToLocY, SpriteInfo.LocWidth,
			    settleXLoc, settleYLoc, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT))
		{
			//------------ not in the town surrounding ------------//
			SetMoveToSurround(settleXLoc, settleYLoc, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT,
				UnitConstants.BUILDING_TYPE_SETTLE, 0, 0, curSettleUnitNum);
		}
		else
		{
			//------- the unit is within the settle location -------//
			SetCur(NextX, NextY);
			SetDir(MoveToLocX, MoveToLocY, settleXLoc + InternalConstants.TOWN_WIDTH / 2,
				settleYLoc + InternalConstants.TOWN_HEIGHT / 2);
		}

		//----------- set action to settle -----------//
		ActionMode = UnitConstants.ACTION_SETTLE;
		ActionParam = 0;
		ActionLocX = settleXLoc;
		ActionLocY = settleYLoc;
	}

	public void assign(int assignXLoc, int assignYLoc, int curAssignUnitNum = 1)
	{
		//----------------------------------------------------------------//
		// return if the unit is dead
		//----------------------------------------------------------------//
		if (HitPoints <= 0.0 || ActionMode == UnitConstants.ACTION_DIE || CurAction == SPRITE_DIE)
			return;

		//----------- BUGHERE : cannot assign when on a ship -----------//
		if (!is_visible())
			return;

		//----------- cannot assign for caravan -----------//
		if (UnitType == UnitConstants.UNIT_CARAVAN)
			return;

		//----------------------------------------------------------------//
		// move there if the destination in other territory
		//----------------------------------------------------------------//
		Location loc = World.GetLoc(assignXLoc, assignYLoc);
		int unitRegionId = World.GetLoc(NextLocX, NextLocY).RegionId;
		if (loc.IsFirm())
		{
			Firm firm = FirmArray[loc.FirmId()];
			bool quit = false;

			if (firm.firm_id == Firm.FIRM_HARBOR)
			{
				FirmHarbor harbor = (FirmHarbor)firm;
				switch (UnitRes[UnitType].unit_class)
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
			else if (unitRegionId != loc.RegionId)
			{
				quit = true;
			}

			if (quit)
			{
				MoveToFirmSurround(assignXLoc, assignYLoc, SpriteInfo.LocWidth, SpriteInfo.LocHeight,
					firm.firm_id);
				return;
			}
		}
		else if (unitRegionId != loc.RegionId)
		{
			if (loc.IsTown())
				MoveToTownSurround(assignXLoc, assignYLoc, SpriteInfo.LocWidth, SpriteInfo.LocHeight);
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

		if (loc.IsFirm())
		{
			//-------------------------------------------------------//
			// the location is firm
			//-------------------------------------------------------//
			recno = loc.FirmId();

			//----------------------------------------------------------------//
			// action_mode2: checking for equal action or idle action
			//----------------------------------------------------------------//
			if (ActionMode2 == UnitConstants.ACTION_ASSIGN_TO_FIRM &&
			    ActionPara2 == recno && ActionLocX2 == assignXLoc && ActionLocY2 == assignYLoc)
			{
				if (CurAction != SPRITE_IDLE)
					return;
			}
			else
			{
				//----------------------------------------------------------------//
				// action_mode2: store new order
				//----------------------------------------------------------------//
				ActionMode2 = UnitConstants.ACTION_ASSIGN_TO_FIRM;
				ActionPara2 = recno;
				ActionLocX2 = assignXLoc;
				ActionLocY2 = assignYLoc;
			}

			Firm firm = FirmArray[recno];
			FirmInfo firmInfo = FirmRes[firm.firm_id];

			if (firm_can_assign(recno) == 0)
			{
				//firmNeedUnit = 0; // move to the surrounding of the firm
				MoveToFirmSurround(assignXLoc, assignYLoc, SpriteInfo.LocWidth, SpriteInfo.LocHeight,
					firm.firm_id);
				return;
			}

			width = firmInfo.loc_width;
			height = firmInfo.loc_height;
			buildingType = UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO;
		}
		else if (loc.IsTown()) // there is town
		{
			if (UnitRes[UnitType].unit_class != UnitConstants.UNIT_CLASS_HUMAN)
				return;

			//-------------------------------------------------------//
			// the location is town
			//-------------------------------------------------------//
			recno = loc.TownId();

			//----------------------------------------------------------------//
			// action_mode2: checking for equal action or idle action
			//----------------------------------------------------------------//
			if (ActionMode2 == UnitConstants.ACTION_ASSIGN_TO_TOWN &&
			    ActionPara2 == recno && ActionLocX2 == assignXLoc && ActionLocY2 == assignYLoc)
			{
				if (CurAction != SPRITE_IDLE)
					return;
			}
			else
			{
				//----------------------------------------------------------------//
				// action_mode2: store new order
				//----------------------------------------------------------------//
				ActionMode2 = UnitConstants.ACTION_ASSIGN_TO_TOWN;
				ActionPara2 = recno;
				ActionLocX2 = assignXLoc;
				ActionLocY2 = assignYLoc;
			}

			Town targetTown = TownArray[recno];
			if (TownArray[recno].NationId != NationId)
			{
				MoveToTownSurround(assignXLoc, assignYLoc, SpriteInfo.LocWidth, SpriteInfo.LocHeight);
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
		SetMoveToSurround(assignXLoc, assignYLoc, width, height, buildingType, 0, 0, curAssignUnitNum);

		//-----------------------------------------------------------------//
		// able to reach building surrounding, set action parameters
		//-----------------------------------------------------------------//
		ActionParam = recno;
		ActionLocX = assignXLoc;
		ActionLocY = assignYLoc;

		switch (buildingType)
		{
			case UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO:
				ActionMode = UnitConstants.ACTION_ASSIGN_TO_FIRM;
				break;

			case UnitConstants.BUILDING_TYPE_TOWN_MOVE_TO:
				ActionMode = UnitConstants.ACTION_ASSIGN_TO_TOWN;
				break;

			case UnitConstants.BUILDING_TYPE_VEHICLE:
				ActionMode = UnitConstants.ACTION_ASSIGN_TO_VEHICLE;
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
		if (HitPoints <= 0.0 || ActionMode == UnitConstants.ACTION_DIE || CurAction == SPRITE_DIE)
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
		if (ActionMode2 == UnitConstants.ACTION_GO_CAST_POWER &&
		    ActionPara2 == 0 && ActionLocX2 == castXLoc && ActionLocY2 == castYLoc &&
		    unitGod.cast_power_type == castPowerType)
		{
			if (CurAction != SPRITE_IDLE)
				return;
		}
		else
		{
			//----------------------------------------------------------------//
			// action_mode2: store new order
			//----------------------------------------------------------------//
			ActionMode2 = UnitConstants.ACTION_GO_CAST_POWER;
			ActionPara2 = 0;
			ActionLocX2 = castXLoc;
			ActionLocY2 = castYLoc;
		}

		//----- order the sprite to stop as soon as possible -----//
		stop(); // new order

		//------------- do searching if neccessary -------------//
		if (Misc.points_distance(NextLocX, NextLocY, castXLoc, castYLoc) > UnitConstants.DO_CAST_POWER_RANGE)
			Search(castXLoc, castYLoc, 1);

		//----------- set action to build the firm -----------//
		ActionMode = UnitConstants.ACTION_GO_CAST_POWER;
		ActionParam = 0;
		ActionLocX = castXLoc;
		ActionLocY = castYLoc;

		unitGod.cast_power_type = castPowerType;
		unitGod.cast_origin_x = NextLocX;
		unitGod.cast_origin_y = NextLocY;
		unitGod.cast_target_x = castXLoc;
		unitGod.cast_target_y = castYLoc;
	}

	//------------------------------------//

	public void change_nation(int newNationRecno)
	{
		bool oldAiUnit = AIUnit;
		int oldNationRecno = NationId;

		if (WayPoints.Count > 0)
			ResetWayPoints();

		//-- if the player is giving a command to this unit, cancel the command --//

		if (NationId == NationArray.player_recno && SpriteId == UnitArray.selected_recno && Power.command_id != 0)
		{
			Power.command_id = 0;
		}

		//---------- stop all action to attack this unit ------------//

		UnitArray.stop_attack_unit(SpriteId);

		//---- update nation_unit_count_array[] ----//

		UnitRes[UnitType].unit_change_nation(newNationRecno, NationId, Rank);

		//------- if the nation has an AI action -------//

		stop2(); // clear the existing order

		//---------------- update vars ----------------//

		GroupId = UnitArray.cur_group_id++; // separate from the current group
		NationId = newNationRecno;

		HomeCampId = 0; // reset it
		OriginalActionMode = 0;

		if (RaceId != 0)
		{
			NationContribution = 0; // contribution to the nation
			TotalReward = 0;
		}

		//-------- if change to one of the existing nations ------//

		AIUnit = NationId != 0 && NationArray[NationId].is_ai();

		//------------ update AI info --------------//

		if (oldAiUnit)
		{
			Nation nation = NationArray[oldNationRecno];

			if (Rank == RANK_GENERAL || Rank == RANK_KING)
				nation.del_general_info(SpriteId);

			else if (UnitRes[UnitType].unit_class == UnitConstants.UNIT_CLASS_CARAVAN)
				nation.del_caravan_info(SpriteId);

			else if (UnitRes[UnitType].unit_class == UnitConstants.UNIT_CLASS_SHIP)
				nation.del_ship_info(SpriteId);
		}

		if (AIUnit && NationId != 0)
		{
			Nation nation = NationArray[NationId];

			if (Rank == RANK_GENERAL || Rank == RANK_KING)
				nation.add_general_info(SpriteId);

			else if (UnitRes[UnitType].unit_class == UnitConstants.UNIT_CLASS_CARAVAN)
				nation.add_caravan_info(SpriteId);

			else if (UnitRes[UnitType].unit_class == UnitConstants.UNIT_CLASS_SHIP)
				nation.add_ship_info(SpriteId);
		}

		//------ if this unit oversees a firm -----//

		if (UnitMode == UnitConstants.UNIT_MODE_OVERSEE)
			FirmArray[UnitModeParam].change_nation(newNationRecno);

		//----- this unit was defending the town before it gets killed ----//

		else if (UnitMode == UnitConstants.UNIT_MODE_DEFEND_TOWN)
		{
			if (!TownArray.IsDeleted(UnitModeParam))
				TownArray[UnitModeParam].ReduceDefenderCount();

			set_mode(0); // reset unit mode
		}

		//---- if the unit is no longer the same nation as the leader ----//

		if (LeaderId != 0)
		{
			Unit leaderUnit = UnitArray[LeaderId];

			if (leaderUnit.NationId != NationId)
			{
				leaderUnit.del_team_member(SpriteId);
				LeaderId = 0;
				TeamId = 0;
			}
		}

		//------ if it is currently selected -------//

		if (SelectedFlag)
			Info.disp();
	}

	public void overseer_migrate(int destTownRecno)
	{
		int curTownRecno = FirmArray[UnitModeParam].overseer_town_recno;

		//------- decrease the population of the unit's home town ------//

		TownArray[curTownRecno].DecPopulation(RaceId, true);

		//--------- increase the population of the target town ------//

		TownArray[destTownRecno].IncPopulation(RaceId, true, Loyalty);
	}

	public bool caravan_in_firm()
	{
		return CurX == -2;
	}

	public void update_loyalty()
	{
		if (NationId == 0 || Rank == RANK_KING || UnitRes[UnitType].race_id == 0)
			return;

		// constructor worker will not change their loyalty when they are in a building
		if (UnitMode == UnitConstants.UNIT_MODE_CONSTRUCT)
			return;

		// The following never really worked that well, since it created a dead give away due to the constant loyalty.
		//----- if this unit is a spy, set its fake loyalty ------//

		if (ConfigAdv.unit_spy_fixed_target_loyalty && SpyId != 0) // a spy's loyalty is always >= 70
		{
			if (Loyalty < 70)
				Loyalty = 70 + Misc.Random(20); // initialize it to be a number between 70 and 90

			TargetLoyalty = Loyalty;
			return;
		}

		//-------- if this is a general ---------//

		Nation ownNation = NationArray[NationId];
		int rc = 0;

		if (Rank == RANK_GENERAL)
		{
			//----- the general's power affect his loyalty ----//

			int targetLoyalty = commander_power();

			//----- the king's race affects the general's loyalty ----//

			if (ownNation.race_id == RaceId)
				targetLoyalty += 20;

			//----- the kingdom's reputation affects the general's loyalty ----//

			targetLoyalty += (int)(ownNation.reputation / 4.0);

			//--- the king's leadership also affect the general's loyalty -----//

			if (ownNation.king_unit_recno != 0)
				targetLoyalty += UnitArray[ownNation.king_unit_recno].Skill.skill_level / 4;

			//-- if the unit is rewarded less than the amount of contribution he made, he will become unhappy --//

			if (NationContribution > TotalReward * 2)
			{
				int decLoyalty = (NationContribution - TotalReward * 2) / 2;
				targetLoyalty -= Math.Min(50, decLoyalty); // this affect 50 points at maximum
			}

			targetLoyalty = Math.Min(targetLoyalty, 100);
			TargetLoyalty = Math.Max(targetLoyalty, 0);
		}

		//-------- if this is a soldier ---------//

		else if (Rank == RANK_SOLDIER)
		{
			bool leader_bonus = ConfigAdv.unit_loyalty_require_local_leader ? is_leader_in_range() : LeaderId != 0;
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

				Unit leaderUnit = UnitArray[LeaderId];

				int targetLoyalty = 30 + leaderUnit.Skill.get_skill(Skill.SKILL_LEADING);

				//---------------------------------------------------//
				//
				// Soldiers with higher combat and leadership skill
				// will get discontented if they are led by a general
				// with low leadership.
				//
				//---------------------------------------------------//

				targetLoyalty -= Skill.combat_level / 2;
				targetLoyalty -= Skill.skill_level;

				if (leaderUnit.Rank == RANK_KING)
					targetLoyalty += 20;

				if (RaceRes.is_same_race(RaceId, leaderUnit.RaceId))
					targetLoyalty += 20;

				if (targetLoyalty < 0)
					targetLoyalty = 0;

				targetLoyalty = Math.Min(targetLoyalty, 100);
				TargetLoyalty = Math.Max(targetLoyalty, 0);
			}
			else
			{
				TargetLoyalty = 0;
			}
		}

		//--------- update loyalty ---------//

		// only increase, no decrease. Decrease are caused by events. Increases are made gradually
		if (TargetLoyalty > Loyalty)
		{
			int incValue = (TargetLoyalty - Loyalty) / 10;

			int newLoyalty = Loyalty + Math.Max(1, incValue);

			if (newLoyalty > TargetLoyalty)
				newLoyalty = TargetLoyalty;

			Loyalty = newLoyalty;
		}
		// only increase, no decrease. Decrease are caused by events. Increases are made gradually
		else if (TargetLoyalty < Loyalty)
		{
			Loyalty--;
		}
	}

	public void set_combat_level(int combatLevel)
	{
		Skill.combat_level = combatLevel;

		UnitInfo unitInfo = UnitRes[UnitType];

		int oldMaxHitPoints = MaxHitPoints;

		MaxHitPoints = unitInfo.hit_points * combatLevel / 100;

		if (oldMaxHitPoints != 0)
			HitPoints = HitPoints * MaxHitPoints / oldMaxHitPoints;

		HitPoints = Math.Min(HitPoints, MaxHitPoints);

		// --------- update can_guard_flag -------//

		if (combatLevel >= unitInfo.guard_combat_level)
		{
			CanGuard = SpriteInfo.CanGuard;
			if (UnitType == UnitConstants.UNIT_ZULU)
				CanGuard |= 4; // shield during attack delay
		}
		else
		{
			CanGuard = 0;
		}

		MaxPower = Skill.combat_level + 50;
		CurPower = Math.Min(CurPower, MaxPower);
	}

	public void inc_minor_combat_level(int incLevel)
	{
		Skill.combat_level_minor += incLevel;

		if (Skill.combat_level_minor > 100)
		{
			if (Skill.combat_level < 100)
				set_combat_level(Skill.combat_level + 1);

			Skill.combat_level_minor -= 100;
		}
	}

	public void inc_minor_skill_level(int incLevel)
	{
		Skill.skill_level_minor += incLevel;

		if (Skill.skill_level_minor > 100)
		{
			if (Skill.skill_level < 100)
				Skill.skill_level++;

			Skill.skill_level_minor -= 100;
		}
	}

	public void set_rank(int rankId)
	{
		if (Rank == rankId)
			return;

		//------- promote --------//

		if (rankId > Rank)
			change_loyalty(GameConstants.PROMOTE_LOYALTY_INCREASE);

		//------- demote -----------//

		// no decrease in loyalty if a spy king hands his nation to his parent nation and become a general again
		else if (rankId < Rank && Rank != RANK_KING)
			change_loyalty(-GameConstants.DEMOTE_LOYALTY_DECREASE);

		//---- update nation_general_count_array[] ----//

		if (NationId != 0)
		{
			UnitInfo unitInfo = UnitRes[UnitType];

			if (Rank == RANK_GENERAL) // if it was a general originally
				unitInfo.dec_nation_general_count(NationId);

			if (rankId == RANK_GENERAL) // if the new rank is general
				unitInfo.inc_nation_general_count(NationId);

			//------ if demote a king to a unit ------//

			// since kings are not included in nation_unit_count, when it is no longer a king, we need to re-increase it.
			if (Rank == RANK_KING && rankId != RANK_KING)
				unitInfo.inc_nation_unit_count(NationId);

			//------ if promote a unit to a king ------//

			// since kings are not included in nation_unit_count, we need to decrease it
			if (Rank != RANK_KING && rankId == RANK_KING)
				unitInfo.dec_nation_unit_count(NationId);
		}

		//----- reset leader_unit_recno if demote a general to soldier ----//

		if (Rank == RANK_GENERAL && rankId == RANK_SOLDIER)
		{
			//----- reset leader_unit_recno of the units he commands ----//

			foreach (Unit unit in UnitArray)
			{
				//TODO
				// don't use IsDeleted() as it filters out units that are currently dying
				if (unit.LeaderId == SpriteId)
				{
					unit.LeaderId = 0;
					unit.TeamId = 0;
				}
			}

			//--------- deinit team_info ---------//

			TeamInfo.Members.Clear();
			TeamId = 0;
		}

		//----- if this is a soldier being promoted to a general -----//

		else if (Rank == RANK_SOLDIER && rankId == RANK_GENERAL)
		{
			//-- if this soldier is formerly commanded by a general, detech it ---//

			if (LeaderId != 0)
			{
				if (!UnitArray.IsDeleted(LeaderId)) // the leader unit may have been killed at the same time
					UnitArray[LeaderId].del_team_member(SpriteId);

				LeaderId = 0;
			}
		}

		//-------------- update AI info --------------//

		if (AIUnit)
		{
			if (Rank == RANK_GENERAL || Rank == RANK_KING)
				NationArray[NationId].del_general_info(SpriteId);

			Rank = rankId;

			if (Rank == RANK_GENERAL || Rank == RANK_KING)
				NationArray[NationId].add_general_info(SpriteId);
		}
		else
		{
			Rank = rankId;
		}

		//----- if this is a general/king ------//

		if (Rank == RANK_GENERAL || Rank == RANK_KING)
		{
			//--- set leadership if this unit does not have any now ----//

			if (Skill.skill_id != Skill.SKILL_LEADING)
			{
				Skill.skill_id = Skill.SKILL_LEADING;
				Skill.skill_level = 10 + Misc.Random(40);
			}
		}

		//------ refresh if the current unit is selected -----//

		if (UnitArray.selected_recno == SpriteId)
			Info.disp();
	}

	public virtual bool can_resign()
	{
		return Rank != RANK_KING;
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
		if (SpyId != 0 && true_nation_recno() != NationId)
		{
			//------ decrease reputation ------//

			NationArray[true_nation_recno()].change_reputation(-GameConstants.SPY_KILLED_REPUTATION_DECREASE);

			//------- add news message -------//

			// display when the player's spy is revealed or the player has revealed an enemy spy
			if (true_nation_recno() == NationArray.player_recno || NationId == NationArray.player_recno)
			{
				//--- If a spy is caught, the spy's nation's reputation wil decrease ---//

				NewsArray.spy_killed(SpyId);
			}
		}
		else
		{
			if (NationId != 0)
				NationArray[NationId].change_reputation(-1.0);
		}

		//----------------------------------------------//

		// if this is a general, news_array.general_die() will be called, set news_add_flag to 0 to suppress the display of thew news
		if (Rank == RANK_GENERAL)
			NewsArray.news_add_flag = false;

		UnitArray.DeleteUnit(this);

		NewsArray.news_add_flag = true;
	}

	public void reward(int rewardNationRecno)
	{
		if (NationArray[rewardNationRecno].cash < GameConstants.REWARD_COST)
			return;

		//--------- if this is a spy ---------//

		if (SpyId != 0 && true_nation_recno() == rewardNationRecno) // if the spy's owning nation rewards the spy
		{
			SpyArray[SpyId].change_loyalty(GameConstants.REWARD_LOYALTY_INCREASE);
		}

		//--- if this spy's nation_recno & true_nation_recno() are both == rewardNationRecno,
		//it's true loyalty and cloaked loyalty will both be increased ---//

		if (NationId == rewardNationRecno)
		{
			TotalReward += GameConstants.REWARD_COST;

			change_loyalty(GameConstants.REWARD_LOYALTY_INCREASE);
		}

		NationArray[rewardNationRecno].add_expense(NationBase.EXPENSE_REWARD_UNIT, GameConstants.REWARD_COST);
	}

	public void spy_change_nation(int newNationRecno, int remoteAction, int groupDefect = 0)
	{
		if (newNationRecno == NationId)
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

		Spy spy = SpyArray[SpyId];

		//--- when a spy change cloak to another nation, he can't cloak as a general, he must become a soldier first ---//

		// if the spy is a commander in a camp, don't set its rank to soldier
		if (is_visible() && Rank == RANK_GENERAL && newNationRecno != spy.true_nation_recno)
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
			if (Rank == RANK_GENERAL || UnitMode == UnitConstants.UNIT_MODE_OVERSEE ||
			    (spy.notify_cloaked_nation_flag != 0 && groupDefect == 0))
			{
				//-- if this spy's cloaked nation is the player's nation, the player will be notified --//

				if (NationId == NationArray.player_recno)
					NewsArray.unit_betray(SpriteId, newNationRecno);
			}

			//---- send news to the cloaked nation if notify flag is on ---//

			if (spy.notify_cloaked_nation_flag != 0 && groupDefect == 0)
			{
				if (newNationRecno == NationArray.player_recno) // cloaked as the player's nation
					NewsArray.unit_betray(SpriteId, newNationRecno);
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
		if (SpyId == 0)
			return false;

		//--------------------------------------------//

		int xLoc1 = CurLocX - GameConstants.SPY_ENEMY_RANGE, yLoc1 = CurLocY - GameConstants.SPY_ENEMY_RANGE;
		int xLoc2 = CurLocX + GameConstants.SPY_ENEMY_RANGE, yLoc2 = CurLocY + GameConstants.SPY_ENEMY_RANGE;

		xLoc1 = Math.Max(0, xLoc1);
		yLoc1 = Math.Max(0, yLoc1);
		xLoc2 = Math.Min(GameConstants.MapSize - 1, xLoc2);
		yLoc2 = Math.Min(GameConstants.MapSize - 1, yLoc2);

		int trueNationRecno = true_nation_recno();

		for (int yLoc = yLoc1; yLoc <= yLoc2; yLoc++)
		{
			for (int xLoc = xLoc1; xLoc <= xLoc2; xLoc++)
			{
				Location loc = World.GetLoc(xLoc, yLoc);

				int unitRecno;

				if (loc.HasUnit(UnitConstants.UNIT_LAND))
					unitRecno = loc.UnitId(UnitConstants.UNIT_LAND);

				else if (loc.HasUnit(UnitConstants.UNIT_SEA))
					unitRecno = loc.UnitId(UnitConstants.UNIT_SEA);

				else if (loc.HasUnit(UnitConstants.UNIT_AIR))
					unitRecno = loc.UnitId(UnitConstants.UNIT_AIR);

				else
					continue;

				if (UnitArray.IsDeleted(unitRecno)) // the unit is dying, its recno is still in the location
					continue;

				Unit otherUnit = UnitArray[unitRecno];
				if (otherUnit.true_nation_recno() != trueNationRecno)
				{
					if (otherUnit.SpyId != 0 && SpyId != 0)
					{
						if (SpyArray[otherUnit.SpyId].spy_skill >= SpyArray[SpyId].spy_skill)
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
		HitPoints += changePoints;

		if (HitPoints < 0.0)
			HitPoints = 0.0;

		if (HitPoints > MaxHitPoints)
			HitPoints = MaxHitPoints;
	}

	public void change_loyalty(int loyaltyChange)
	{
		int newLoyalty = Loyalty + loyaltyChange;

		newLoyalty = Math.Max(0, newLoyalty);

		Loyalty = Math.Min(100, newLoyalty);
	}

	public bool think_betray()
	{
		int unitRecno = SpriteId;

		if (SpyId != 0) // spies do not betray here, spy has its own functions for betrayal
			return false;

		//----- if the unit is in training or is constructing a building, do not rebel -------//

		if (!is_visible() && UnitMode != UnitConstants.UNIT_MODE_OVERSEE)
			return false;

		if (Loyalty >= GameConstants.UNIT_BETRAY_LOYALTY) // you when unit is
			return false;

		if (UnitRes[UnitType].race_id == 0 || NationId == 0 || Rank == RANK_KING || SpyId != 0)
			return false;

		//------ turn towards other nation --------//

		int bestNationRecno = 0, bestScore = Loyalty; // the score must be larger than the current loyalty
		int unitRegionId = region_id();

		if (Loyalty == 0) // if the loyalty is 0, it will definitely betray
			bestScore = -100;

		Nation curNation = NationArray[NationId];

		foreach (Nation nation in NationArray)
		{
			if (nation == curNation || !curNation.get_relation(nation.nation_recno).has_contact)
				continue;

			//--- only if the nation has a base town in the region where the unit stands ---//

			// TODO should not take base town into account
			if (!RegionArray.NationHasBaseTown(unitRegionId, nation.nation_recno))
				continue;

			//------------------------------------------------//

			int nationScore = (int)nation.reputation + (nation.overall_rating - curNation.overall_rating);

			if (RaceRes.is_same_race(nation.race_id, RaceId))
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
		else if (Loyalty == 0)
		{
			//----------------------------------------------//
			// If there is no good nation to turn towards to and
			// the loyalty has dropped to 0, resign itself and
			// leave the nation.
			//
			// However, if the unit is spy, it will stay with the
			// nation as it has never been really loyal to the nation.
			//---------------------------------------------//

			if (Rank != RANK_KING && is_visible() && SpyId == 0)
			{
				resign(InternalConstants.COMMAND_AUTO);
				return true;
			}
		}

		return false;
	}

	public bool betray(int newNationRecno)
	{
		int unitRecno = SpriteId;

		if (NationId == newNationRecno)
			return false;

		// don't change nation when the unit is constructing a firm
		// don't change nation when the unit is constructing a firm
		if (UnitMode == UnitConstants.UNIT_MODE_CONSTRUCT || UnitMode == UnitConstants.UNIT_MODE_ON_SHIP)
			return false;

		//---------- add news -----------//

		if (NationId == NationArray.player_recno || newNationRecno == NationArray.player_recno)
		{
			//--- if this is a spy, don't display news message for betrayal as it is already displayed in Unit::spy_change_nation() ---//

			if (SpyId == 0)
				NewsArray.unit_betray(SpriteId, newNationRecno);
		}

		//------ change nation now ------//

		change_nation(newNationRecno);

		//-------- set the loyalty of the unit -------//

		if (NationId != 0)
		{
			Nation nation = NationArray[NationId];

			Loyalty = GameConstants.UNIT_BETRAY_LOYALTY + Misc.Random(5);

			if (nation.reputation > 0)
				change_loyalty((int)nation.reputation);

			if (RaceRes.is_same_race(nation.race_id, RaceId))
				change_loyalty(30);

			update_loyalty(); // update target loyalty
		}
		else //------ if change to independent rebel -------//
		{
			Loyalty = 0; // no loyalty needed
		}

		//--- if this unit is a general, change nation for the units he commands ---//

		int newTeamId = UnitArray.cur_team_id++;

		if (Rank == RANK_GENERAL)
		{
			int nationReputation = (int)NationArray[NationId].reputation;

			foreach (Unit unit in UnitArray)
			{
				//---- capture the troop this general commands -----//

				if (unit.LeaderId == SpriteId && unit.Rank == RANK_SOLDIER && unit.is_visible())
				{
					if (unit.SpyId != 0) // if the unit is a spy
					{
						// 1-group defection of this unit, allowing us to hande the change of nation
						unit.spy_change_nation(newNationRecno, InternalConstants.COMMAND_AUTO, 1);
					}

					unit.change_nation(newNationRecno);

					unit.TeamId = newTeamId; // assign new team_id or checking for nation_recno
				}
			}
		}

		TeamId = newTeamId;

		//------ go to meet the new master -------//

		if (is_visible() && NationId != 0)
		{
			if (SpyId == 0 || SpyArray[SpyId].notify_cloaked_nation_flag != 0)
			{
				// generals shouldn't automatically be assigned to camps, they should just move near your villages
				if (Rank == RANK_GENERAL)
					ai_move_to_nearby_town();
				else
					think_normal_human_action(); // this is an AI function in OUNITAI.CPP
			}
		}

		return true;
	}

	public bool can_stand_guard()
	{
		return (CanGuard & 1) != 0;
	}

	public bool can_move_guard()
	{
		return (CanGuard & 2) != 0;
	}

	public bool can_attack_guard()
	{
		return (CanGuard & 4) != 0;
	}

	public int firm_can_assign(int firmRecno)
	{
		Firm firm = FirmArray[firmRecno];
		FirmInfo firmInfo = FirmRes[firm.firm_id];

		switch (UnitRes[UnitType].unit_class)
		{
			case UnitConstants.UNIT_CLASS_HUMAN:
				if (NationId == firm.nation_recno)
				{
					if (Skill.skill_id == Skill.SKILL_CONSTRUCTION && firm.firm_id != Firm.FIRM_MONSTER)
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
							return Rank == RANK_SOLDIER ? 1 : 2;

						case Firm.FIRM_BASE:
							if (RaceId == firm.race_id)
							{
								if (Skill.skill_id == 0 || Skill.skill_id == Skill.SKILL_PRAYING) // non-skilled worker
									return 1;
								if (Rank != RANK_SOLDIER)
									return 2;
							}

							break;

						//case FIRM_INN:
						// stealthed soldier spy can assign to inn
						//	return rank_id == RANK_SOLDIER && nation_recno != true_nation_recno() ? 4 : 0;

						default:
							return Rank == RANK_SOLDIER && firmInfo.need_unit() ? 1 : 0;
					}
				}

				break;

			case UnitConstants.UNIT_CLASS_WEAPON:
				if (firm.firm_id == Firm.FIRM_CAMP && NationId == firm.nation_recno)
					return 1;
				break;

			case UnitConstants.UNIT_CLASS_SHIP:
				if (firm.firm_id == Firm.FIRM_HARBOR && NationId == firm.nation_recno)
					return 1;
				break;

			case UnitConstants.UNIT_CLASS_MONSTER:
				if (firm.firm_id == Firm.FIRM_MONSTER && MobileType == UnitConstants.UNIT_LAND)
				{
					// BUGHERE : suppose only land monster can assign
					return Rank == RANK_SOLDIER ? 1 : 2;
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
		FinalDir = CurDir;
		TurnDelay = 0;
		CurAction = SPRITE_IDLE;
	}

	public void set_ready()
	{
		FinalDir = CurDir;
		TurnDelay = 0;
		CurAction = SPRITE_READY_TO_MOVE;
	}

	public void set_move()
	{
		CurAction = SPRITE_MOVE;
	}

	public void set_wait()
	{
		CurAction = SPRITE_WAIT;
		CurFrame = 1;
		WaitingTerm++;
	}

	public void set_attack()
	{
		FinalDir = CurDir;
		TurnDelay = 0;
		CurAction = SPRITE_ATTACK;
	}

	public void set_turn()
	{
		CurAction = SPRITE_TURN;
	}

	public void set_ship_extra_move()
	{
		CurAction = SPRITE_SHIP_EXTRA_MOVE;
	}

	public void set_die()
	{
		if (ActionMode == UnitConstants.ACTION_DIE)
			return;

		ActionMode = UnitConstants.ACTION_DIE;
		CurAction = SPRITE_DIE;
		CurFrame = 1;

		//---- if this unit is led by a leader, only mobile units has leader_unit_recno assigned to a leader -----//

		// the leader unit may have been killed at the same time
		if (LeaderId != 0 && !UnitArray.IsDeleted(LeaderId))
		{
			UnitArray[LeaderId].del_team_member(SpriteId);
			LeaderId = 0;
		}
	}

	//------------------ idle functions -------------------//
	private bool reactivate_idle_action()
	{
		if (ActionMode2 == UnitConstants.ACTION_STOP)
			return false; // return for no idle action

		if (!IsDirCorrect())
			return true; // cheating for turning the direction

		int curXLoc = MoveToLocX;
		int curYLoc = MoveToLocY;
		Location location;

		bool hasSearch = false;
		bool returnFlag = false;

		switch (ActionMode2)
		{
			case UnitConstants.ACTION_STOP:
			case UnitConstants.ACTION_DIE:
				return false; // do nothing

			case UnitConstants.ACTION_ATTACK_UNIT:
				if (ActionPara2 == 0 || UnitArray.IsDeleted(ActionPara2))
					stop2();
				else
				{
					Unit unit = UnitArray[ActionPara2];
					SpriteInfo spriteInfo = unit.SpriteInfo;

					if (space_for_attack(ActionLocX2, ActionLocY2, unit.MobileType, spriteInfo.LocWidth, spriteInfo.LocHeight))
					{
						//------ there should be place for this unit to attack the target, attempts to attack it ------//
						attack_unit(ActionPara2, 0, 0, false); // last 0 for not reset blocked_edge
						hasSearch = true;
						returnFlag = true;
					}
				}

				break;

			case UnitConstants.ACTION_ATTACK_FIRM:
				location = World.GetLoc(ActionLocX2, ActionLocY2);
				if (ActionPara2 == 0 || !location.IsFirm())
					stop2(); // stop since target is already destroyed
				else
				{
					Firm firm = FirmArray[ActionPara2];
					FirmInfo firmInfo = FirmRes[firm.firm_id];

					if (space_for_attack(ActionLocX2, ActionLocY2, UnitConstants.UNIT_LAND, firmInfo.loc_width, firmInfo.loc_height))
					{
						//-------- attack target since space is found for this unit to move to ---------//
						attack_firm(ActionLocX2, ActionLocY2, 0, 0, 0);
						hasSearch = true;
						returnFlag = true;
					}
				}

				break;

			case UnitConstants.ACTION_ATTACK_TOWN:
				location = World.GetLoc(ActionLocX2, ActionLocY2);
				if (ActionPara2 == 0 || !location.IsTown())
					stop2(); // stop since target is deleted
				else
				{
					if (space_for_attack(ActionLocX2, ActionLocY2, UnitConstants.UNIT_LAND,
						    InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT))
					{
						//---------- attack target --------//
						attack_town(ActionLocX2, ActionLocY2, 0, 0, 0);
						hasSearch = true;
						returnFlag = true;
					}
				}

				break;

			case UnitConstants.ACTION_ATTACK_WALL:
				location = World.GetLoc(ActionLocX2, ActionLocY2);
				if (!location.IsWall())
					stop2(); // stop since target doesn't exist
				else
				{
					if (space_for_attack(ActionLocX2, ActionLocY2, UnitConstants.UNIT_LAND, 1, 1))
					{
						//----------- attack target -----------//
						attack_wall(ActionLocX2, ActionLocY2, 0, 0, 0);
						hasSearch = true;
						returnFlag = true;
					}
				}

				break;

			case UnitConstants.ACTION_ASSIGN_TO_FIRM:
			case UnitConstants.ACTION_ASSIGN_TO_TOWN:
			case UnitConstants.ACTION_ASSIGN_TO_VEHICLE:
				//---------- resume assign actions -------------//
				assign(ActionLocX2, ActionLocY2);
				hasSearch = true;
				WaitingTerm = 0;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_ASSIGN_TO_SHIP:
				//------------ try to assign to marine ------------//
				assign_to_ship(ActionLocX2, ActionLocY2, ActionPara2);
				hasSearch = true;
				WaitingTerm = 0;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_BUILD_FIRM:
				//-------------- build again ----------------//
				build_firm(ActionLocX2, ActionLocY2, ActionPara2, InternalConstants.COMMAND_AUTO);
				hasSearch = true;
				WaitingTerm = 0;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_SETTLE:
				//------------- try again to settle -----------//
				settle(ActionLocX2, ActionLocY2);
				hasSearch = true;
				WaitingTerm = 0;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_BURN:
				//---------------- resume burn action -----------------//
				burn(ActionLocX2, ActionLocY2, InternalConstants.COMMAND_AUTO);
				hasSearch = true;
				WaitingTerm = 0;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_MOVE:
				if (MoveToLocX != ActionLocX2 || MoveToLocY != ActionLocY2)
				{
					//------- move since the unit has not reached its destination --------//
					MoveTo(ActionLocX2, ActionLocY2, 1);
					hasSearch = true;
					returnFlag = true;
					break;
				}

				WaitingTerm = 0;
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
					ship_to_beach(ActionLocX2, ActionLocY2, out _, out _);
					hasSearch = true;
				}

				returnFlag = true;
				break;

			case UnitConstants.ACTION_GO_CAST_POWER:
				go_cast_power(ActionLocX2, ActionLocY2, ((UnitGod)this).cast_power_type, InternalConstants.COMMAND_AUTO);
				returnFlag = true;
				break;
		}

		if (hasSearch && SeekPath.PathStatus == SeekPath.PATH_IMPOSSIBLE && NextLocX == MoveToLocX && NextLocY == MoveToLocY)
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
			if (curXLoc == MoveToLocX && curYLoc == MoveToLocY && SeekPath.PathStatus == SeekPath.PATH_IMPOSSIBLE)
			{
				//TODO check

				if (ActionMode2 == UnitConstants.ACTION_ASSIGN_TO_SHIP || ActionMode2 == UnitConstants.ACTION_SHIP_TO_BEACH ||
				    in_any_defense_mode())
					return true;

				//------- number of nodes is not enough to find the destination -------//
				if (ActionMisc != UnitConstants.ACTION_MISC_STOP)
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
		IdleDetectHasUnit = IdleDetectHasFirm = IdleDetectHasTown = IdleDetectHasWall = false;
		HelpMode = UnitConstants.HELP_NOTHING;

		//-----------------------------------------------------------------------------------------------//
		// adjust waiting_term for default_mode
		//-----------------------------------------------------------------------------------------------//
		++WaitingTerm;
		WaitingTerm = Math.Max(WaitingTerm, 0); //**BUGHERE
		int lowestBit = WaitingTerm % detectDelay;

		if (ActionMode2 == UnitConstants.ACTION_STOP)
		{
			WaitingTerm = lowestBit;
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
			int checkXLoc = MoveToLocX + xOffset;
			int checkYLoc = MoveToLocY + yOffset;
			if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
				continue;

			//------------------ verify location ---------------//
			loc = World.GetLoc(checkXLoc, checkYLoc);
			if (defenseMode != 0 && ActionMode2 != UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET)
			{
				if (ActionMode2 == UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET)
					if (loc.PowerNationId != NationId && loc.PowerNationId != 0)
						continue; // skip this location because it is not neutral nation or our nation
			}

			//----------------------------------------------------------------------------//
			// checking the target type
			//----------------------------------------------------------------------------//
			if ((targetMobileType = loc.HasAnyUnit(i == 1 ? MobileType : UnitConstants.UNIT_LAND)) != 0 &&
			    (targetRecno = loc.UnitId(targetMobileType)) != 0 && !UnitArray.IsDeleted(targetRecno))
			{
				//=================== is unit ======================//
				if (IdleDetectHasUnit || (ActionParam == targetRecno &&
				                             ActionMode == UnitConstants.ACTION_ATTACK_UNIT &&
				                             checkXLoc == ActionLocX && checkYLoc == ActionLocY))
					continue; // same target as before

				unit = UnitArray[targetRecno];
				if (NationId != 0 && unit.NationId == NationId && HelpMode != UnitConstants.HELP_ATTACK_UNIT)
					idle_detect_helper_attack(targetRecno); // help our troop
				else if ((HelpMode == UnitConstants.HELP_ATTACK_UNIT && HelpAttackTargetId == targetRecno) ||
				         (unit.NationId != NationId && idle_detect_unit_checking(targetRecno)))
				{
					IdleDetectTargetUnitId = targetRecno;
					IdleDetectHasUnit = true;
					break; // break with highest priority
				}
			}
			else if (loc.IsFirm() && (targetRecno = loc.FirmId()) != 0)
			{
				//=============== is firm ===============//
				if (IdleDetectHasFirm || (ActionParam == targetRecno &&
				                             ActionMode == UnitConstants.ACTION_ATTACK_FIRM &&
				                             ActionLocX == checkXLoc && ActionLocY == checkYLoc))
					continue; // same target as before

				if (idle_detect_firm_checking(targetRecno) != 0)
				{
					IdleDetectTargetFirmId = targetRecno;
					IdleDetectHasFirm = true;
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
			if (ActionMode2 == UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET)
			{
				//----------- defense units allow to attack units and firms -----------//

				if (IdleDetectHasUnit)
					defense_attack_unit(IdleDetectTargetUnitId);
				else if (IdleDetectHasFirm)
				{
					Firm targetFirm = FirmArray[IdleDetectTargetFirmId];
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
			else if (ActionMode2 == UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET)
			{
				//----------- town units only attack units ------------//

				if (IdleDetectHasUnit)
					defend_town_attack_unit(IdleDetectTargetUnitId);
				else
					return false;

				return true;
			}
			else if (ActionMode2 == UnitConstants.ACTION_MONSTER_DEFEND_DETECT_TARGET)
			{
				//---------- monsters can attack units and firms -----------//

				if (IdleDetectHasUnit)
					monster_defend_attack_unit(IdleDetectTargetUnitId);
				else if (IdleDetectHasFirm)
				{
					Firm targetFirm = FirmArray[IdleDetectTargetFirmId];
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

			if (IdleDetectHasUnit)
			{
				attack_unit(IdleDetectTargetUnitId, 0, 0, true);

				//--- set the original position of the target, so the unit won't chase too far away ---//

				Unit unit = UnitArray[IdleDetectTargetUnitId];

				OriginalTargetLocX = unit.NextLocX;
				OriginalTargetLocY = unit.NextLocY;

				rc = 1;
			}

			else if (HelpMode == UnitConstants.HELP_ATTACK_UNIT)
			{
				attack_unit(HelpAttackTargetId, 0, 0, true);

				//--- set the original position of the target, so the unit won't chase too far away ---//

				Unit unit = UnitArray[HelpAttackTargetId];

				OriginalTargetLocX = unit.NextLocX;
				OriginalTargetLocY = unit.NextLocY;

				rc = 1;
			}
			else if (IdleDetectHasFirm)
			{
				Firm targetFirm = FirmArray[IdleDetectTargetFirmId];
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

			if (rc != 0 && OriginalActionMode == 0)
			{
				OriginalActionMode = UnitConstants.ACTION_MOVE;
				OriginalActionParam = 0;
				OriginalActionLocX = NextLocX;
				OriginalActionLocY = NextLocY;
			}

			return true;
		}

		return false;
	}

	private void idle_detect_helper_attack(int unitRecno)
	{
		const int HELP_DISTANCE = 15;

		Unit unit = UnitArray[unitRecno];
		if (unit.UnitType == UnitConstants.UNIT_CARAVAN)
			return;

		//char	actionMode;
		int actionPara = 0;
		//short actionXLoc, actionYLoc;
		int isUnit = 0;

		//------------- is the unit attacking other unit ------------//
		switch (unit.ActionMode2)
		{
			case UnitConstants.ACTION_ATTACK_UNIT:
				actionPara = unit.ActionPara2;
				isUnit++;
				break;

			default:
				switch (unit.ActionMode)
				{
					case UnitConstants.ACTION_ATTACK_UNIT:
						actionPara = unit.ActionParam;
						isUnit++;
						break;
				}

				break;
		}

		if (isUnit != 0 && !UnitArray.IsDeleted(actionPara))
		{
			Unit targetUnit = UnitArray[actionPara];

			if (targetUnit.NationId == NationId)
				return;

			// the targetUnit this unit is attacking may have entered a
			// building by now due to processing order -- skip this one
			if (!targetUnit.is_visible())
				return;

			if (Misc.points_distance(NextLocX, NextLocY,
				    targetUnit.NextLocX, targetUnit.NextLocY) < HELP_DISTANCE)
			{
				if (idle_detect_unit_checking(actionPara))
				{
					HelpAttackTargetId = actionPara;
					HelpMode = UnitConstants.HELP_ATTACK_UNIT;
				}

				for (int i = 0; i < BlockedEdges.Length; i++)
					BlockedEdges[i] = 0;
			}
		}
	}

	private bool idle_detect_unit_checking(int targetRecno)
	{
		Unit targetUnit = UnitArray[targetRecno];

		if (targetUnit.UnitType == UnitConstants.UNIT_CARAVAN)
			return false;

		//-------------------------------------------//
		// If the target is moving, don't attack it.
		// Only attack when the unit stands still or
		// is attacking.
		//-------------------------------------------//

		if (targetUnit.CurAction != SPRITE_ATTACK && targetUnit.CurAction != SPRITE_IDLE)
			return false;

		//-------------------------------------------//
		// If the target is a spy of our own and the
		// notification flag is set to 0, then don't
		// attack.
		//-------------------------------------------//

		if (targetUnit.SpyId != 0) // if the target unit is our spy, don't attack 
		{
			Spy spy = SpyArray[targetUnit.SpyId];

			if (spy.true_nation_recno == NationId && spy.notify_cloaked_nation_flag == 0)
				return false;
		}

		if (SpyId != 0) // if this unit is our spy, don't attack own units
		{
			Spy spy = SpyArray[SpyId];

			if (spy.true_nation_recno == targetUnit.NationId && spy.notify_cloaked_nation_flag == 0)
				return false;
		}

		SpriteInfo spriteInfo = targetUnit.SpriteInfo;
		Nation nation = NationId != 0 ? NationArray[NationId] : null;
		int targetNationRecno = targetUnit.NationId;

		//-------------------------------------------------------------------//
		// checking nation relationship
		//-------------------------------------------------------------------//

		if (NationId != 0)
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
			else if (!targetUnit.independent_nation_can_attack(NationId))
				return false;
		}
		else if (!independent_nation_can_attack(targetNationRecno)) // independent unit
			return false;

		//---------------------------------------------//
		if (space_for_attack(targetUnit.NextLocX, targetUnit.NextLocY, targetUnit.MobileType,
			    spriteInfo.LocWidth, spriteInfo.LocHeight))
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

		Nation nation = NationId != 0 ? NationArray[NationId] : null;
		int targetNationRecno = firm.nation_recno;
		int targetMobileType = MobileType == UnitConstants.UNIT_SEA ? UnitConstants.UNIT_SEA : UnitConstants.UNIT_LAND;

		//-------------------------------------------------------------------------------//
		// checking nation relationship
		//-------------------------------------------------------------------------------//
		if (NationId != 0)
		{
			if (targetNationRecno != 0)
			{
				//------- don't attack own units and non-hostile units -------//

				if (targetNationRecno == NationId)
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

				if (!monsterFirm.is_hostile_nation(NationId))
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
		Nation nation = NationId != 0 ? NationArray[NationId] : null;
		int targetNationRecno = town.NationId;

		//-------------------------------------------------------------------------------//
		// checking nation relationship
		//-------------------------------------------------------------------------------//
		if (NationId != 0)
		{
			if (targetNationRecno != 0)
			{
				//------- don't attack own units and non-hostile units -------//

				if (targetNationRecno == NationId)
					return 0;

				//--------------------------------------------------------------//
				// if the unit is hostile, only attack if should_attack flag to
				// that nation is true or the unit is attacking somebody or something.
				//--------------------------------------------------------------//

				NationRelation nationRelation = nation.get_relation(targetNationRecno);

				if (nationRelation.status != NationBase.NATION_HOSTILE || !nationRelation.should_attack)
					return 0;
			}
			else if (!town.IsHostileNation(NationId))
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
		Location loc = World.GetLoc(targetXLoc, targetYLoc);
		Nation nation = NationId != 0 ? NationArray[NationId] : null;
		int targetNationRecno = loc.WallNationId();

		//-------------------------------------------------------------------------------//
		// checking nation relationship
		//-------------------------------------------------------------------------------//
		if (NationId != 0)
		{
			if (targetNationRecno != 0)
			{
				//------- don't attack own units and non-hostile units -------//

				if (targetNationRecno == NationId)
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

	private void unit_auto_guarding(Unit attackUnit)
	{
		if (ForceMove)
			return;

		//---------------------------------------//
		//
		// If the aggressive_mode is off, then don't fight back when the unit is moving,
		// only fight back when the unit is already fighting or is idle.
		//
		//---------------------------------------//

		if (!AggressiveMode && CurAction != SPRITE_ATTACK && CurAction != SPRITE_IDLE)
		{
			return;
		}

		//--------------------------------------------------------------------//
		// decide attack or not
		//--------------------------------------------------------------------//

		int changeToAttack = 0;
		if (CurAction == SPRITE_ATTACK || (SpriteInfo.NeedTurning != 0 && CurAction == SPRITE_TURN &&
		                                    (Math.Abs(NextLocX - ActionLocX) < AttackRange ||
		                                     Math.Abs(NextLocY - ActionLocY) < AttackRange)))
		{
			if (ActionMode != UnitConstants.ACTION_ATTACK_UNIT)
			{
				changeToAttack++; //else continue to attack the target unit
			}
			else
			{
				if (ActionParam == 0 || UnitArray.IsDeleted(ActionParam))
					changeToAttack++; // attack new target
			}
		}
		else if
			(CurAction != SPRITE_DIE) // && abs(cur_x-next_x)<spriteInfo.speed && abs(cur_y-next_y)<spriteInfo.speed)
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
			if (AIUnit) // allow ai unit to select target to attack
			{
				//------------------------------------------------------------//
				// conditions to let the unit escape
				//------------------------------------------------------------//
				//-************* codes here ************-//

				//------------------------------------------------------------//
				// select the weaker target to attack first, if more than one
				// unit attack this unit
				//------------------------------------------------------------//
				int attackXLoc = attackUnit.NextLocX;
				int attackYLoc = attackUnit.NextLocY;

				int attackDistance = cal_distance(attackXLoc, attackYLoc,
					attackUnit.SpriteInfo.LocWidth, attackUnit.SpriteInfo.LocHeight);
				if (attackDistance == 1) // only consider close attack
				{
					Unit targetUnit = UnitArray[ActionParam];
					if (targetUnit.HitPoints > attackUnit.HitPoints) // the new attacker is weaker
						attack_unit(attackUnit.SpriteId, 0, 0, true);
				}
			}

			return;
		}

		//--------------------------------------------------------------------//
		// cancel AI actions
		//--------------------------------------------------------------------//
		if (AIActionId != 0 && NationId != 0)
			NationArray[NationId].action_failure(AIActionId, SpriteId);

		if (!attackUnit.is_visible())
			return;

		//--------------------------------------------------------------------------------//
		// checking for ship processing trading
		//--------------------------------------------------------------------------------//
		if (SpriteInfo.SpriteSubType == 'M') //**** BUGHERE, is sprite_sub_type really representing UNIT_MARINE???
		{
			UnitInfo unitInfo = UnitRes[UnitType];
			if (unitInfo.carry_goods_capacity != 0)
			{
				UnitMarine ship = (UnitMarine)this;
				if (ship.auto_mode != 0 && ship.stop_defined_num > 1)
				{
					int targetXLoc = attackUnit.NextLocX;
					int targetYLoc = attackUnit.NextLocY;
					SpriteInfo targetSpriteInfo = attackUnit.SpriteInfo;
					int attackDistance = cal_distance(targetXLoc, targetYLoc,
						targetSpriteInfo.LocWidth, targetSpriteInfo.LocHeight);
					int maxAttackRange = max_attack_range();
					if (maxAttackRange < attackDistance)
						return; // can't attack the target
				}
			}
		}

		switch (ActionMode2)
		{
			case UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET:
			case UnitConstants.ACTION_AUTO_DEFENSE_BACK_CAMP:
				ActionMode2 = UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET;
				break;

			case UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET:
			case UnitConstants.ACTION_DEFEND_TOWN_BACK_TOWN:
				ActionMode2 = UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET;
				break;

			case UnitConstants.ACTION_MONSTER_DEFEND_DETECT_TARGET:
			case UnitConstants.ACTION_MONSTER_DEFEND_BACK_FIRM:
				ActionMode2 = UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET;
				break;
		}

		save_original_action();

		//----------------------------------------------------------//
		// set the original location of the attacking target when
		// the attack() function is called, action_x_loc2 & action_y_loc2
		// will change when the unit move, but these two will not.
		//----------------------------------------------------------//

		OriginalTargetLocX = attackUnit.NextLocX;
		OriginalTargetLocY = attackUnit.NextLocY;

		if (!UnitArray.IsDeleted(attackUnit.SpriteId))
			attack_unit(attackUnit.SpriteId, 0, 0, true);
	}

	private void set_unreachable_location(int xLoc, int yLoc)
	{
	}

	private void check_self_surround()
	{
	}

	private void get_hit_x_y(out int x, out int y)
	{
		switch (CurDir)
		{
			case 0: // north
				x = CurX;
				y = CurY - InternalConstants.CellHeight;
				break;
			case 1: // north east
				x = CurX + InternalConstants.CellWidth;
				y = CurY - InternalConstants.CellHeight;
				break;
			case 2: // east
				x = CurX + InternalConstants.CellWidth;
				y = CurY;
				break;
			case 3: // south east
				x = CurX + InternalConstants.CellWidth;
				y = CurY + InternalConstants.CellHeight;
				break;
			case 4: // south
				x = CurX;
				y = CurY + InternalConstants.CellHeight;
				break;
			case 5: // south west
				x = CurX - InternalConstants.CellWidth;
				y = CurY + InternalConstants.CellHeight;
				break;
			case 6: // west
				x = CurX - InternalConstants.CellWidth;
				y = CurY;
				break;
			case 7: // north west
				x = CurX - InternalConstants.CellWidth;
				y = CurY - InternalConstants.CellHeight;
				break;
			default:
				x = CurX;
				y = CurY;
				break;
		}
	}

	private void add_close_attack_effect()
	{
		int effectId = AttackInfos[CurAttack].effect_id;
		if (effectId != 0)
		{
			int x, y;
			get_hit_x_y(out x, out y);
			EffectArray.AddEffect(effectId, x, y, SPRITE_IDLE, CurDir, MobileType == UnitConstants.UNIT_AIR ? 8 : 2, 0);
		}
	}

	//---------------- other functions -----------------//
	// calculate distance from this unit(can be 1x1, 2x2) to a known size object
	private int cal_distance(int targetXLoc, int targetYLoc, int targetWidth, int targetHeight)
	{
		int curXLoc = NextLocX;
		int curYLoc = NextLocY;
		int dispX = 0, dispY = 0;

		if (curXLoc < targetXLoc)
			dispX = (targetXLoc - curXLoc - SpriteInfo.LocWidth) + 1;
		else if ((dispX = curXLoc - targetXLoc - targetWidth + 1) < 0)
			dispX = 0;

		if (curYLoc < targetYLoc)
			dispY = (targetYLoc - curYLoc - SpriteInfo.LocHeight) + 1;
		else if ((dispY = curYLoc - targetYLoc - targetHeight + 1) < 0)
			return dispX;

		return (dispX >= dispY) ? dispX : dispY;
	}

	private bool is_in_surrounding(int checkXLoc, int checkYLoc, int width,
		int targetXLoc, int targetYLoc, int targetWidth, int targetHeight)
	{
		switch (MoveStepCoeff())
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
		if (ActionMode2 == ActionMode && ActionPara2 == ActionPara2 &&
		    ActionLocX2 == ActionLocX && ActionLocY2 == ActionLocY)
		{
			ActionPara2 = 0;
		}

		ActionParam = 0;
	}

	protected void process_build_firm()
	{
		if (CurAction == SPRITE_IDLE) // the unit is at the build location now
		{
			// **BUGHERE, the unit shouldn't be hidden when building structures
			// otherwise, it's cargo_recno will be conflict with the structure's
			// cargo_recno

			bool succeedFlag = false;
			bool shouldProceed = true;

			if (CurLocX == MoveToLocX && CurLocY == MoveToLocY)
			{
				FirmInfo firmInfo = FirmRes[ActionParam];
				int width = firmInfo.loc_width;
				int height = firmInfo.loc_height;

				//---------------------------------------------------------//
				// check whether the unit in the building surrounding
				//---------------------------------------------------------//
				if (!is_in_surrounding(MoveToLocX, MoveToLocY, SpriteInfo.LocWidth,
					    ActionLocX, ActionLocY, width, height))
				{
					//---------- not in the building surrounding ---------//
					return;
				}

				//---------------------------------------------------------//
				// the unit in the firm surrounding
				//---------------------------------------------------------//

				if (NationId != 0)
				{
					Nation nation = NationArray[NationId];

					if (nation.cash < firmInfo.setup_cost)
						shouldProceed = false; // out of cash
				}

				//---------------------------------------------------------//
				// check whether the firm can be built in the specified location
				//---------------------------------------------------------//
				if (shouldProceed && World.CanBuildFirm(ActionLocX, ActionLocY, ActionParam, SpriteId) != 0 &&
				    FirmRes[ActionParam].can_build(SpriteId))
				{
					bool aiUnit = AIUnit;
					int actionXLoc = ActionLocX;
					int actionYLoc = ActionLocY;
					int unitRecno = SpriteId;

					//---------------------------------------------------------------------------//
					// if unit inside the firm location, deinit the unit to free the space for
					// building firm
					//---------------------------------------------------------------------------//
					if (MoveToLocX >= ActionLocX && MoveToLocX < ActionLocX + width &&
					    MoveToLocY >= ActionLocY && MoveToLocY < ActionLocY + height)
						deinit_sprite(false); // 0-if the unit is currently selected, deactivate it.

					// action_para = firm id.
					if (FirmArray.BuildFirm(ActionLocX, ActionLocY, NationId, ActionParam,
						    SpriteInfo.SpriteCode, SpriteId) != 0)
					{
						//--------- able to build the firm --------//

						reset_action_para2();
						succeedFlag = true;
					}
				}
			}

			//----- call action finished/failure -----//

			if (AIActionId != 0 && NationId != 0)
			{
				if (succeedFlag)
					NationArray[NationId].action_finished(AIActionId, SpriteId);
				else
					NationArray[NationId].action_failure(AIActionId, SpriteId);
			}

			//---------------------------------------//

			reset_action_para();
		}
	}

	protected void process_assign()
	{
		if (CurAction != SPRITE_IDLE)
		{
			//------------------------------------------------------------------//
			// change units' action if the firm/town/unit assign to has been deleted
			// or has changed its nation
			//------------------------------------------------------------------//
			switch (ActionMode2)
			{
				case UnitConstants.ACTION_ASSIGN_TO_FIRM:
				case UnitConstants.ACTION_AUTO_DEFENSE_BACK_CAMP:
				case UnitConstants.ACTION_MONSTER_DEFEND_BACK_FIRM:
					if (FirmArray.IsDeleted(ActionParam))
					{
						stop2();
						return;
					}
					else
					{
						Firm firm = FirmArray[ActionParam];
						if (firm.nation_recno != NationId && !firm.can_assign_capture())
						{
							stop2();
							return;
						}
					}

					break;

				case UnitConstants.ACTION_ASSIGN_TO_TOWN:
				case UnitConstants.ACTION_DEFEND_TOWN_BACK_TOWN:
					if (TownArray.IsDeleted(ActionParam))
					{
						stop2();
						return;
					}
					else if (TownArray[ActionParam].NationId != NationId)
					{
						stop2();
						return;
					}

					break;

				case UnitConstants.ACTION_ASSIGN_TO_VEHICLE:
					if (UnitArray.IsDeleted(ActionParam))
					{
						stop2();
						return;
					}
					else if (UnitArray[ActionParam].NationId != NationId)
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
			if (CurLocX == MoveToLocX && CurLocY == MoveToLocY)
			{
				//----- first check if there is firm in the given location ------//
				Location loc = World.GetLoc(ActionLocX, ActionLocY);

				if (loc.IsFirm() && loc.FirmId() == ActionParam)
				{
					//---------------- a firm on the location -----------------//
					Firm firm = FirmArray[ActionParam];
					FirmInfo firmInfo = FirmRes[firm.firm_id];

					//---------- resume action if the unit has not reached the firm surrounding ----------//
					if (!is_in_surrounding(MoveToLocX, MoveToLocY, SpriteInfo.LocWidth,
						    ActionLocX, ActionLocY, firmInfo.loc_width, firmInfo.loc_height))
					{
						//------------ not in the surrounding -----------//
						if (ActionMode != ActionMode2) // for defense mode
							SetMoveToSurround(ActionLocX, ActionLocY, firmInfo.loc_width, firmInfo.loc_height,
								UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO);
						return;
					}

					//------------ in the firm surrounding ------------//
					if (!firm.under_construction)
					{
						//-------------------------------------------------------//
						// if in defense mode, update parameters in military camp
						//-------------------------------------------------------//
						if (ActionMode2 == UnitConstants.ACTION_AUTO_DEFENSE_BACK_CAMP)
						{
							FirmCamp camp = (FirmCamp)firm;
							camp.update_defense_unit(SpriteId);
						}

						//---------------------------------------------------------------//
						// remainder useful parameters to do reaction to Nation.
						// These parameters will be destroyed after calling assign_unit()
						//---------------------------------------------------------------//
						int nationRecno = NationId;
						int unitRecno = SpriteId;
						int actionXLoc = ActionLocX;
						int actionYLoc = ActionLocY;
						int aiActionId = AIActionId;
						bool aiUnit = AIUnit;

						reset_action_para2();

						firm.assign_unit(SpriteId);

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
						if (AIUnit && firm.under_construction)
							return; // not allow AI to change firm builder

						reset_action_para2();
						if (Skill.get_skill(Skill.SKILL_CONSTRUCTION) != 0 || Skill.get_skill(firm.firm_skill_id) != 0)
							firm.set_builder(SpriteId);
					}

					//------------ update UnitArray's selected parameters ------------//
					reset_action_para();
					if (SelectedFlag)
					{
						SelectedFlag = false;
						UnitArray.selected_count--;
					}
				}
				else if (loc.IsTown() && loc.TownId() == ActionParam)
				{
					//---------------- a town on the location -----------------//
					if (!is_in_surrounding(MoveToLocX, MoveToLocY, SpriteInfo.LocWidth, ActionLocX,
						    ActionLocY,
						    InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT))
					{
						//----------- not in the surrounding ------------//
						return;
					}

					int actionPara = ActionParam;
					int spriteRecno = SpriteId;

					if (AIActionId != 0 && NationId != 0)
						NationArray[NationId].action_finished(AIActionId, SpriteId);

					//------------ update UnitArray's selected parameters ------------//
					reset_action_para2();
					reset_action_para();
					if (SelectedFlag)
					{
						SelectedFlag = false;
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

				else if (loc.HasUnit(UnitConstants.UNIT_SEA) && loc.UnitId(UnitConstants.UNIT_SEA) == ActionParam)
				{
					//------------ update UnitArray's selected parameters ------------//
					reset_action_para2();
					reset_action_para();
					if (SelectedFlag)
					{
						SelectedFlag = false;
						UnitArray.selected_count--;
					}

					//----------------- load the unit to the marine -----------------//
					((UnitMarine)UnitArray[ActionParam]).load_unit(SpriteId);
				}
				else
				{
					//------------------------------------------------------------------//
					// abort actions for ai_unit since the target location has nothing
					//------------------------------------------------------------------//

					if (AIActionId != 0 && NationId != 0)
						NationArray[NationId].action_failure(AIActionId, SpriteId);
				}
			}

			//-***** don't place codes here as unit may be removed above *****-//
			//reset_action_para();
			//selected_flag = 0;
		}
	}

	protected void process_burn()
	{
		if (CurAction == SPRITE_IDLE) // the unit is at the build location now
		{
			if (NextLocX == ActionLocX && NextLocY == ActionLocY)
			{
				reset_action_para2();
				SetDir(MoveToLocX, MoveToLocY, ActionLocX, ActionLocY);
				World.SetupFire(ActionLocX, ActionLocY);
			}

			reset_action_para();
		}
	}

	protected void process_settle()
	{
		if (CurAction == SPRITE_IDLE) // the unit is at the build location now
		{
			ResetPath();

			if (CurLocX == MoveToLocX && CurLocY == MoveToLocY)
			{
				if (!is_in_surrounding(MoveToLocX, MoveToLocY, SpriteInfo.LocWidth, ActionLocX, ActionLocY,
					    InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT))
					return;

				Location loc = World.GetLoc(ActionLocX, ActionLocY);
				if (!loc.IsTown())
				{
					int unitRecno = SpriteId;

					reset_action_para2();
					//------------ settle the unit now -------------//
					TownArray.Settle(SpriteId, ActionLocX, ActionLocY);

					if (UnitArray.IsDeleted(unitRecno))
						return;

					reset_action_para();
				}
				else if (TownArray[loc.TownId()].NationId == NationId)
				{
					//---------- a town zone already exists ---------//
					assign(ActionLocX, ActionLocY);
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
		if (UnitArray.IsDeleted(ActionPara2))
		{
			stop2();
			return; // stop the unit as the ship is deleted
		}
		else
		{
			ship = (UnitMarine)UnitArray[ActionPara2];
			if (ship.NationId != NationId)
			{
				stop2();
				return; // stop the unit as the ship's nation_recno != the unit's nation_recno
			}
		}

		if (ship.ActionMode2 != UnitConstants.ACTION_SHIP_TO_BEACH)
		{
			stop2(); // the ship has changed its action
			return;
		}

		int curXLoc = NextLocX;
		int curYLoc = NextLocY;
		int shipXLoc = ship.NextLocX;
		int shipYLoc = ship.NextLocY;

		if (ship.CurX == ship.NextX && ship.CurY == ship.NextY &&
		    Math.Abs(shipXLoc - curXLoc) <= 1 && Math.Abs(shipYLoc - curYLoc) <= 1)
		{
			//----------- assign the unit now -----------//
			if (Math.Abs(CurX - NextX) < SpriteInfo.Speed && Math.Abs(CurY - NextY) < SpriteInfo.Speed)
			{
				if (AIActionId != 0)
					NationArray[NationId].action_finished(AIActionId, SpriteId);

				stop2();
				SetDir(curXLoc, curYLoc, shipXLoc, shipYLoc);
				ship.load_unit(SpriteId);
				return;
			}
		}
		else if (CurAction == SPRITE_IDLE)
			SetDir(curXLoc, curYLoc, ship.MoveToLocX, ship.MoveToLocY);

		//---------------------------------------------------------------------------//
		// update location to embark
		//---------------------------------------------------------------------------//
		int shipActionXLoc = ship.ActionLocX2;
		int shipActionYLoc = ship.ActionLocY2;
		if (Math.Abs(shipActionXLoc - ActionLocX2) > 1 || Math.Abs(shipActionYLoc - ActionLocY2) > 1)
		{
			if (shipActionXLoc != ActionLocX2 || shipActionYLoc != ActionLocY2)
			{
				Location unitLoc = World.GetLoc(curXLoc, curYLoc);
				Location shipActionLoc = World.GetLoc(shipActionXLoc, shipActionYLoc);
				if (unitLoc.RegionId != shipActionLoc.RegionId)
				{
					stop2();
					return;
				}

				assign_to_ship(shipActionXLoc, shipActionYLoc, ActionPara2);
				return;
			}
		}
	}

	protected void process_ship_to_beach()
	{
		//----- action_mode never clear, in_beach to skip idle checking
		if (CurAction == SPRITE_IDLE)
		{
			int shipXLoc = NextLocX;
			int shipYLoc = NextLocY;
			if (shipXLoc == MoveToLocX && shipYLoc == MoveToLocY)
			{
				if (Math.Abs(MoveToLocX - ActionLocX2) <= 2 && Math.Abs(MoveToLocY - ActionLocY2) <= 2)
				{
					UnitMarine ship = (UnitMarine)this;
					//------------------------------------------------------------------------------//
					// determine whether extra_move is required
					//------------------------------------------------------------------------------//
					switch (ship.extra_move_in_beach)
					{
						case UnitMarine.NO_EXTRA_MOVE:
							if (Math.Abs(shipXLoc - ActionLocX2) > 1 || Math.Abs(shipYLoc - ActionLocY2) > 1)
							{
								ship.extra_move();
							}
							else
							{
								//ship.in_beach = 1;
								ship.extra_move_in_beach = UnitMarine.NO_EXTRA_MOVE;

								if (AIActionId != 0)
									NationArray[NationId].action_finished(AIActionId, SpriteId);
							}

							break;

						case UnitMarine.EXTRA_MOVING_IN:
						case UnitMarine.EXTRA_MOVING_OUT:
							break;

						case UnitMarine.EXTRA_MOVE_FINISH:
							if (AIActionId != 0)
								NationArray[NationId].action_finished(AIActionId, SpriteId);
							break;

						default:
							break;
					}
				}
			}
			else
				reset_action_para();
		}
		else if (CurAction == SPRITE_TURN && IsDirCorrect())
			set_move();
	}

	protected void process_rebel()
	{
		Rebel rebel = RebelArray[UnitModeParam];

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
				if (!World.CanBuildTown(rebel.action_para, rebel.action_para2, SpriteId))
				{
					Location loc = World.GetLoc(rebel.action_para, rebel.action_para2);

					if (loc.IsTown() && TownArray[loc.TownId()].RebelId == rebel.rebel_recno)
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
		if (CurAction == SPRITE_IDLE)
		{
			//----------------------------------------------------------------------------------------//
			// Checking condition to do casting power, Or resume action
			//----------------------------------------------------------------------------------------//
			if (Misc.points_distance(CurLocX, CurLocY, ActionLocX2, ActionLocY2) <= UnitConstants.DO_CAST_POWER_RANGE)
			{
				if (NextLocX != ActionLocX2 || NextLocY != ActionLocY2)
				{
					SetDir(NextLocX, NextLocY, ActionLocX2, ActionLocY2);
				}

				set_attack(); // set cur_action=sprite_attack to cast power 
				CurFrame = 1;
			}
			else
			{
				go_cast_power(ActionLocX2, ActionLocY2, unitGod.cast_power_type, InternalConstants.COMMAND_AUTO);
			}
		}
		else if (CurAction == SPRITE_ATTACK)
		{
			//----------------------------------------------------------------------------------------//
			// do casting power now
			//----------------------------------------------------------------------------------------//
			AttackInfo attackInfo = AttackInfos[CurAttack];
			if (CurFrame == attackInfo.bullet_out_frame)
			{
				// add effect
				add_close_attack_effect();

				unitGod.cast_power(ActionLocX2, ActionLocY2);
				set_remain_attack_delay();
				// stop2();
			}

			if (CurFrame == 1 && RemainAttackDelay == 0) // last frame of delaying
				stop2();
		}
	}

	protected void king_die()
	{
		//--------- add news ---------//

		NewsArray.king_die(NationId);

		//--- see if the units, firms and towns of the nation are all destroyed ---//

		Nation nation = NationArray[NationId];

		nation.king_unit_recno = 0;
	}

	protected void general_die()
	{
		//--------- add news ---------//

		if (NationId == NationArray.player_recno)
			NewsArray.general_die(SpriteId);
	}

	protected void process_recover()
	{
		if (HitPoints == 0.0 || HitPoints == MaxHitPoints) // this unit is dead already
			return;

		//---- overseers in firms and ships in harbors recover faster ----//

		int hitPointsInc;

		if (UnitMode == UnitConstants.UNIT_MODE_OVERSEE || UnitMode == UnitConstants.UNIT_MODE_IN_HARBOR)
		{
			hitPointsInc = 2;
		}

		//------ for units on ships --------//

		else if (UnitMode == UnitConstants.UNIT_MODE_ON_SHIP)
		{
			//--- if the ship where the unit is on is in the harbor, the unit recovers faster ---//

			if (UnitArray[UnitModeParam].UnitMode == UnitConstants.UNIT_MODE_IN_HARBOR)
				hitPointsInc = 2;
			else
				hitPointsInc = 1;
		}

		//----- only recover when the unit is not moving -----//

		else if (CurAction == SPRITE_IDLE)
		{
			hitPointsInc = 1;
		}
		else
			return;

		//---------- recover now -----------//

		HitPoints += hitPointsInc;

		if (HitPoints > MaxHitPoints)
			HitPoints = MaxHitPoints;
	}

	
	#region Old AI Functions
	
	public int AIActionId { get; set; } // an unique id. for locating the AI action node this unit belongs to in Nation::action_array
	private int AIOriginalTargetLocX { get; set; } // for AI only
	private int AIOriginalTargetLocY { get; set; }
	private bool AINoSuitableAction { get; set; }
	
	public virtual void process_ai()
	{
		//------ the aggressive_mode of AI units is always 1 ------//

		AggressiveMode = true;

		//--- if it's a spy from other nation, don't control it ---//

		if (SpyId != 0 && true_nation_recno() != NationId &&
		    SpyArray[SpyId].notify_cloaked_nation_flag == 0 && Info.TotalDays % 60 != SpriteId % 60)
		{
			return;
		}

		//----- think about rewarding this unit -----//

		if (RaceId != 0 && Rank != RANK_KING && Info.TotalDays % 5 == SpriteId % 5)
			think_reward();

		//-----------------------------------------//

		if (!is_visible())
			return;

		//--- if the unit has stopped, but ai_action_id hasn't been reset ---//

		if (CurAction == SPRITE_IDLE &&
		    ActionMode == UnitConstants.ACTION_STOP && ActionMode2 == UnitConstants.ACTION_STOP &&
		    AIActionId != 0 && NationId != 0)
		{
			NationArray[NationId].action_failure(AIActionId, SpriteId);
		}

		//---- King flees under attack or surrounded by enemy ---//

		if (RaceId != 0 && Rank == RANK_KING)
		{
			if (think_king_flee())
				return;
		}

		//---- General flees under attack or surrounded by enemy ---//

		if (RaceId != 0 && Rank == RANK_GENERAL && Info.TotalDays % 7 == SpriteId % 7)
		{
			if (think_general_flee())
				return;
		}

		//-- let Unit::next_day() process it process original_action_mode --//

		if (OriginalActionMode != 0)
			return;

		//------ if the unit is not stop right now ------//

		if (!is_ai_all_stop())
		{
			think_stop_chase();
			return;
		}

		//-----------------------------------------//

		if (MobileType == UnitConstants.UNIT_LAND)
		{
			if (ai_escape_fire())
				return;
		}

		//---------- if this is your spy --------//

		if (SpyId != 0 && true_nation_recno() == NationId)
			think_spy_action();

		//------ if this unit is from a camp --------//

		if (HomeCampId != 0)
		{
			Firm firmCamp = FirmArray[HomeCampId];
			bool rc;

			if (Rank == RANK_SOLDIER)
				rc = firmCamp.workers.Count < Firm.MAX_WORKER;
			else
				rc = (firmCamp.overseer_recno == 0);

			if (rc)
			{
				if (return_camp())
					return;
			}

			HomeCampId = 0; // the camp is already occupied by somebody
		}

		//----------------------------------------//

		if (RaceId != 0 && Rank == RANK_KING)
		{
			think_king_action();
		}
		else if (RaceId != 0 && Rank == RANK_GENERAL)
		{
			think_general_action();
		}
		else
		{
			if (UnitRes[UnitType].unit_class == UnitConstants.UNIT_CLASS_WEAPON)
			{
				// don't call too often as the action may fail and it takes a while to call the function each time
				if (Info.TotalDays % 15 == SpriteId % 15)
				{
					think_weapon_action(); //-- ships AI are called in UnitMarine --//
				}
			}
			else if (RaceId != 0)
			{
				//--- if previous attempts for new action failed, don't call think_normal_human_action() so frequently then ---//

				if (AINoSuitableAction)
				{
					// don't call too often as the action may fail and it takes a while to call the function each time
					if (Info.TotalDays % 15 != SpriteId % 15)
						return;
				}

				//---------------------------------//

				if (!think_normal_human_action())
				{
					// set this flag so think_normal_human_action() won't be called continously
					AINoSuitableAction = true;

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

		Nation ownNation = NationArray[NationId];
		bool rc = false;

		if (TeamInfo.Members.Count <= 1)
		{
			rc = true;
		}

		//--- if the skill of the general and the number of soldiers he commands is not large enough to justify building a new camp ---//

		else if (Skill.skill_level /* + team_info.member_count*4*/ < 40 + ownNation.pref_keep_general / 5) // 40 to 60
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
				reward(NationId);
			set_rank(RANK_SOLDIER);
			return think_normal_human_action();
		}

		return false;
	}

	public bool think_leader_action()
	{
		Nation nation = NationArray[NationId];
		FirmCamp bestCamp = null;
		int bestRating = 10;
		ActionNode delActionNode = null;
		int curXLoc = NextLocX, curYLoc = NextLocY;
		int curRegionId = World.GetRegionId(curXLoc, curYLoc);

		// if this unit is the king, always assign it to a camp regardless of whether the king is a better commander than the existing one
		if (Rank == RANK_KING)
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
			int newLeadership = firmCamp.new_commander_leadership(RaceId, Skill.skill_level);

			int curRating = newLeadership - curLeadership;

			//-------------------------------------//

			if (curRating > bestRating)
			{
				//--- if there is already somebody being assigned to it ---//

				ActionNode actionNode = null;

				if (Rank != RANK_KING) // don't check this if the current unit is the king
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

		int memberCount = TeamInfo.Members.Count;
		if (memberCount > 0 && memberCount - 1 <= Firm.MAX_WORKER - bestCamp.workers.Count)
		{
			validate_team();

			UnitArray.assign(bestCamp.loc_x1, bestCamp.loc_y1, false, InternalConstants.COMMAND_AI, TeamInfo.Members);
			return true;
		}
		else //--- otherwise assign the general only ---//
		{
			return nation.add_action(bestCamp.loc_x1, bestCamp.loc_y1, -1, -1,
				Nation.ACTION_AI_ASSIGN_OVERSEER, Firm.FIRM_CAMP, 1, SpriteId) != null;
		}

		return false;
	}

	public bool think_normal_human_action()
	{
		if (HomeCampId != 0)
			return false;

		// if the unit is led by a commander, let the commander makes the decision.
		// If the leader has been assigned to a firm, don't consider it as a leader anymore
		if (LeaderId != 0 && UnitArray[LeaderId].is_visible())
		{
			return false;
		}

		//---- think about assign the unit to a firm that needs workers ----//

		Nation ownNation = NationArray[NationId];
		Firm bestFirm = null;
		int regionId = World.GetRegionId(NextLocX, NextLocY);
		int skillId = Skill.skill_id;
		int skillLevel = Skill.skill_level;
		int bestRating = 0;
		int curXLoc = NextLocX, curYLoc = NextLocY;

		if (Skill.skill_id != 0)
		{
			foreach (Firm firm in FirmArray)
			{
				if (firm.nation_recno != NationId)
					continue;

				if (firm.region_id != regionId)
					continue;

				int curRating = 0;

				if (Skill.skill_id == Skill.SKILL_CONSTRUCTION) // if this is a construction worker
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

							if (firm.majority_race() == RaceId)
							{
								if (Skill.skill_level < minSkill + 10)
									continue;
							}
							else //-- for different race, only assign if the skill is significantly higher than the existing ones --//
							{
								if (Skill.skill_level < minSkill + 30)
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

							if (firm.majority_race() == RaceId)
							{
								if ((!hasOtherRace && (minMaxHitPoints > MaxHitPoints - 10)) ||
								    (hasOtherRace && (minMaxHitPointsOtherRace > MaxHitPoints + 30)))
									continue;
							}
							else
							{
								if ((hasOtherRace && (minMaxHitPointsOtherRace > MaxHitPoints - 10)) ||
								    (!hasOtherRace && (minMaxHitPoints > MaxHitPoints - 30)))
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

				curRating += World.DistanceRating(curXLoc, curYLoc, firm.center_x, firm.center_y);

				if (firm.majority_race() == RaceId)
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
						if (worker.race_id != RaceId)
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
						firmCamp.coming_unit_array.Add(SpriteId);
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
			if (town.NationId != NationId)
				continue;

			if (town.RegionId != regionId)
				continue;

			hasTownInRegion = true;

			if (town.Population >= GameConstants.MAX_TOWN_POPULATION || !town.IsBaseTown)
				continue;

			//--- only assign to towns of the same race ---//

			if (ownNation.pref_town_harmony > 50)
			{
				if (town.MajorityRace() != RaceId)
					continue;
			}

			//-------- calculate the rating ---------//

			int curRating = World.DistanceRating(curXLoc, curYLoc, town.LocCenterX, town.LocCenterY);

			curRating += 300 * town.RacesPopulation[RaceId - 1] / town.Population; // racial homogenous bonus

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
			if (MaxHitPoints < 50)
			{
				assign(bestTown.LocX1, bestTown.LocY1);
				return true;
			}
		}

		//----- if we don't have any existing towns in this region ----//

		if (!hasTownInRegion)
		{

			// --- if region is too small don't consider this area, stay in the island forever --//
			if (RegionArray.GetRegionInfo(regionId).RegionStatId == 0)
				return false;

			//-- if we also don't have any existing camps in this region --//

			if (RegionArray.GetRegionStat(regionId).CampNationCounts[NationId - 1] == 0)
			{
				//---- try to build one if this unit can ----//

				if (ownNation.cash > FirmRes[Firm.FIRM_CAMP].setup_cost &&
				    FirmRes[Firm.FIRM_CAMP].can_build(SpriteId) &&
				    LeaderId == 0) // if this unit is commanded by a leader, let the leader build the camp
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
		Nation nation = NationArray[NationId];
		FirmCamp bestCamp = null;
		int bestRating = 0;
		int regionId = World.GetRegionId(NextLocX, NextLocY);
		int curXLoc = NextLocX, curYLoc = NextLocY;

		for (int i = 0; i < nation.ai_camp_array.Count; i++)
		{
			FirmCamp firmCamp = (FirmCamp)FirmArray[nation.ai_camp_array[i]];

			if (firmCamp.region_id != regionId || firmCamp.is_worker_full())
				continue;

			//-------- calculate the rating ---------//

			int curRating = World.DistanceRating(curXLoc, curYLoc, firmCamp.center_x, firmCamp.center_y);

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

		Nation ownNation = NationArray[NationId];
		Town bestTown = null;
		int bestRating = 0;
		int regionId = World.GetRegionId(NextLocX, NextLocY);
		int curXLoc = NextLocX, curYLoc = NextLocY;

		for (int i = ownNation.ai_town_array.Count - 1; i >= 0; i--)
		{
			Town town = TownArray[ownNation.ai_town_array[i]];

			if (town.RegionId != regionId)
				continue;

			if (!town.IsBaseTown || town.NoNeighborSpace)
				continue;

			int curRating = World.DistanceRating(curXLoc, curYLoc, town.LocCenterX, town.LocCenterY);

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
		Nation ownNation = NationArray[NationId];

		//----------------------------------------------------------//
		// The need to secure high loyalty on this unit is based on:
		// -its skill
		// -its combat level 
		// -soldiers commanded by this unit
		//----------------------------------------------------------//

		if (SpyId != 0 && true_nation_recno() == NationId) // if this is a spy of ours
		{
			return false; // Spy.think_reward() will handle this.
		}

		int curLoyalty = Loyalty;
		int neededLoyalty;

		//----- if this unit is on a mission ------/

		if (AIActionId != 0)
		{
			neededLoyalty = GameConstants.UNIT_BETRAY_LOYALTY + 10;
		}

		//----- otherwise only reward soldiers and generals ------//

		//DieselMachine TODO do not reward generals that are not is the camps
		else if (Skill.skill_id == Skill.SKILL_LEADING)
		{
			//----- calculate the needed loyalty --------//

			neededLoyalty = commanded_soldier_count() * 5 + Skill.skill_level;

			if (UnitMode == UnitConstants.UNIT_MODE_OVERSEE) // if this unit is an overseer
			{
				// if this unit's loyalty is < betrayel level, reward immediately
				if (Loyalty < GameConstants.UNIT_BETRAY_LOYALTY)
				{
					reward(NationId); // reward it immediatley if it's an overseer, don't check ai_should_spend()
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
			reward(NationId);
			return true;
		}

		return false;
	}

	public void think_independent_unit()
	{
		if (!is_ai_all_stop())
			return;

		//--- don't process if it's a spy and the notify cloak flag is on ---//

		if (SpyId != 0)
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

			if (SpyArray[SpyId].notify_cloaked_nation_flag == 0)
				return;

			if (Info.TotalDays % 5 != SpriteId % 5)
				return;
		}

		//-------- if this is a rebel ----------//

		if (UnitMode == UnitConstants.UNIT_MODE_REBEL)
		{
			Rebel rebel = RebelArray[UnitModeParam];

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
		int regionId = World.GetRegionId(NextLocX, NextLocY);
		int bestRating = 0;
		int curXLoc = NextLocX, curYLoc = NextLocY;

		foreach (Town town in TownArray)
		{
			if (town.NationId != 0 || town.Population >= GameConstants.MAX_TOWN_POPULATION ||
			    town.RegionId != regionId)
			{
				continue;
			}

			int curRating = World.DistanceRating(curXLoc, curYLoc, town.LocCenterX, town.LocCenterY);
			curRating += 100 * town.RacesPopulation[RaceId - 1] / town.Population;

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestTown = town;
			}
		}

		if (bestTown != null)
		{
			//--- drop its rebel identity and becomes a normal unit if he decides to settle to a town ---//

			if (UnitMode == UnitConstants.UNIT_MODE_REBEL)
				RebelArray.drop_rebel_identity(SpriteId);

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
		if (ForceMove && CurAction != SPRITE_IDLE && CurAction != SPRITE_ATTACK)
			return true;

		//------- if the king is alone --------//

		Nation ownNation = NationArray[NationId];

		//------------------------------------------------//
		// When the king is alone and there is no assigned action OR
		// when the king is injured, the king will flee
		// back to its camp.
		//------------------------------------------------//

		if ((TeamInfo.Members.Count == 0 && AIActionId == 0) || HitPoints < 125 - ownNation.pref_military_courage / 4)
		{
			//------------------------------------------//
			//
			// If the king is currently under attack, flee
			// to the nearest camp with the maximum protection.
			//
			//------------------------------------------//

			FirmCamp bestCamp = null;
			int bestRating = 0;
			int curXLoc = NextLocX, curYLoc = NextLocY;
			int curRegionId = World.GetRegionId(curXLoc, curYLoc);

			if (CurAction == SPRITE_ATTACK)
			{
				for (int i = ownNation.ai_camp_array.Count - 1; i >= 0; i--)
				{
					FirmCamp firmCamp = (FirmCamp)FirmArray[ownNation.ai_camp_array[i]];

					if (firmCamp.region_id != curRegionId)
						continue;

					// if there is already a commander in this camp. However if this is the king, than ingore this
					if (firmCamp.overseer_recno != 0 && Rank != RANK_KING)
						continue;

					if (firmCamp.ai_is_capturing_independent_village())
						continue;

					int curRating = World.DistanceRating(curXLoc, curYLoc, firmCamp.center_x, firmCamp.center_y);

					if (curRating > bestRating)
					{
						bestRating = curRating;
						bestCamp = firmCamp;
					}
				}

			}
			else if (HomeCampId != 0) // if there is a home for the king
			{
				bestCamp = (FirmCamp)FirmArray[HomeCampId];
			}

			//------------------------------------//

			if (bestCamp != null)
			{
				if (Config.ai_aggressiveness > Config.OPTION_LOW)
					ForceMove = true;

				assign(bestCamp.loc_x1, bestCamp.loc_y1);
			}
			else // if the king is neither under attack or has a home camp, then call the standard think_leader_action()
			{
				think_leader_action();
			}

			return CurAction != SPRITE_IDLE;
		}

		return false;
	}

	public bool think_general_flee()
	{
		// the general is already fleeing now
		if (ForceMove && CurAction != SPRITE_IDLE && CurAction != SPRITE_ATTACK)
			return true;

		//------- if the general is alone --------//

		Nation ownNation = NationArray[NationId];

		//------------------------------------------------//
		// When the general is alone and there is no assigned action OR
		// when the general is injured, the general will flee
		// back to its camp.
		//------------------------------------------------//

		if ((TeamInfo.Members.Count == 0 && AIActionId == 0) ||
		    HitPoints < MaxHitPoints * (75 + ownNation.pref_military_courage / 2) / 200) // 75 to 125 / 200
		{
			//------------------------------------------//
			//
			// If the general is currently under attack, flee
			// to the nearest camp with the maximum protection.
			//
			//------------------------------------------//

			FirmCamp bestCamp = null;
			int bestRating = 0;
			int curXLoc = NextLocX, curYLoc = NextLocY;
			int curRegionId = World.GetRegionId(curXLoc, curYLoc);

			if (CurAction == SPRITE_ATTACK)
			{
				for (int i = ownNation.ai_camp_array.Count - 1; i >= 0; i--)
				{
					FirmCamp firmCamp = (FirmCamp)FirmArray[ownNation.ai_camp_array[i]];

					if (firmCamp.region_id != curRegionId)
						continue;

					if (firmCamp.ai_is_capturing_independent_village())
						continue;

					int curRating = World.DistanceRating(curXLoc, curYLoc, firmCamp.center_x, firmCamp.center_y);

					if (curRating > bestRating)
					{
						bestRating = curRating;
						bestCamp = firmCamp;
					}
				}

			}
			else if (HomeCampId != 0) // if there is a home for the general
			{
				bestCamp = (FirmCamp)FirmArray[HomeCampId];
			}

			//------------------------------------//

			if (bestCamp != null)
			{
				// if there is already an overseer there, just move close to the camp for protection
				if (bestCamp.overseer_recno != 0)
				{
					if (Config.ai_aggressiveness > Config.OPTION_LOW)
						ForceMove = true;

					MoveTo(bestCamp.loc_x1, bestCamp.loc_y1);
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

			return CurAction != SPRITE_IDLE;
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

		if (!(ActionMode == UnitConstants.ACTION_ATTACK_UNIT && AIOriginalTargetLocX >= 0))
			return false;

		if (UnitArray.IsDeleted(ActionParam))
		{
			stop2();
			return true;
		}

		Unit targetUnit = UnitArray[ActionParam];

		if (!targetUnit.is_visible())
		{
			stop2();
			return true;
		}

		//----------------------------------------//

		int aiChaseDistance = 10 + NationArray[NationId].pref_military_courage / 20; // chase distance: 10 to 15

		int curDistance = Misc.points_distance(targetUnit.NextLocX, targetUnit.NextLocY,
			AIOriginalTargetLocX, AIOriginalTargetLocY);

		if (curDistance <= aiChaseDistance)
			return false;

		//--------- stop the unit ----------------//

		stop2();

		//--- if this unit leads a troop, stop the action of all troop members as well ---//

		int leaderUnitRecno;

		if (LeaderId != 0)
			leaderUnitRecno = LeaderId;
		else
			leaderUnitRecno = SpriteId;

		TeamInfo teamInfo = UnitArray[leaderUnitRecno].TeamInfo;

		for (int i = teamInfo.Members.Count - 1; i >= 0; i--)
		{
			int unitRecno = teamInfo.Members[i];

			if (UnitArray.IsDeleted(unitRecno))
				continue;

			UnitArray[unitRecno].stop2();
		}

		return true;
	}

	public void ai_move_to_nearby_town()
	{
		//---- look for towns to assign to -----//

		Nation ownNation = NationArray[NationId];
		Town bestTown = null;
		int regionId = World.GetRegionId(NextLocX, NextLocY);
		int bestRating = 0;
		int curXLoc = NextLocX, curYLoc = NextLocY;

		// can't use ai_town_array[] because this function will be called by Unit::betray() when a unit defected to the player's kingdom
		foreach (Town town in TownArray)
		{
			if (town.NationId != NationId)
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
			MoveToTownSurround(bestTown.LocX1, bestTown.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight);
	}

	public bool ai_escape_fire()
	{
		if (CurAction != SPRITE_IDLE)
			return false;

		if (MobileType != UnitConstants.UNIT_LAND)
			return false;

		Location location = World.GetLoc(NextLocX, NextLocY);

		if (location.FireStrength() == 0)
			return false;

		//--------------------------------------------//

		int checkLimit = 400; // checking for 400 location
		int xShift, yShift;
		int curXLoc = NextLocX;
		int curYLoc = NextLocY;

		for (int i = 2; i < checkLimit; i++)
		{
			Misc.cal_move_around_a_point(i, 20, 20, out xShift, out yShift);

			int checkXLoc = curXLoc + xShift;
			int checkYLoc = curYLoc + yShift;

			if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
				continue;

			if (!location.CanMove(MobileType))
				continue;

			location = World.GetLoc(checkXLoc, checkYLoc);

			if (location.FireStrength() == 0) // move to a safe place now
			{
				MoveTo(checkXLoc, checkYLoc);
				return true;
			}
		}

		return false;
	}

	public void ai_leader_being_attacked(int attackerUnitRecno)
	{
		// this can happen when the unit has just changed nation
		if (UnitArray[attackerUnitRecno].NationId == NationId)
			return;

		//------------------------------------//

		int callIntervalDays = 0;
		bool rc = false;

		if (Rank == RANK_KING)
		{
			rc = true;
			callIntervalDays = 7;
		}
		else if (Rank == RANK_GENERAL)
		{
			rc = (Skill.skill_level >= 30 + (100 - NationArray[NationId].pref_keep_general) / 2); // 30 to 80
			callIntervalDays = 15; // don't call too freqently
		}

		if (rc)
		{
			if (Info.game_date > TeamInfo.AILastRequestDefenseDate.AddDays(callIntervalDays))
			{
				TeamInfo.AILastRequestDefenseDate = Info.game_date;
				NationArray[NationId].ai_defend(attackerUnitRecno);
			}
		}
	}

	public bool ai_build_camp()
	{
		//--- to prevent building more than one camp at the same time ---//

		int curRegionId = region_id();
		Nation ownNation = NationArray[NationId];

		if (ownNation.is_action_exist(Nation.ACTION_AI_BUILD_FIRM, Firm.FIRM_CAMP, curRegionId))
			return false;

		//------- locate a place for the camp --------//

		FirmInfo firmInfo = FirmRes[Firm.FIRM_CAMP];
		int xLoc = 0, yLoc = 0;
		int teraMask = UnitRes.mobile_type_to_mask(UnitConstants.UNIT_LAND);

		// leave at least one location space around the building
		if (World.LocateSpaceRandom(ref xLoc, ref yLoc, GameConstants.MapSize - 1, GameConstants.MapSize - 1,
			    firmInfo.loc_width + 2, firmInfo.loc_height + 2,
			    GameConstants.MapSize * GameConstants.MapSize, curRegionId, true, teraMask))
		{
			return ownNation.add_action(xLoc, yLoc, -1, -1,
				Nation.ACTION_AI_BUILD_FIRM, Firm.FIRM_CAMP, 1, SpriteId) != null;
		}

		return false;
	}

	public bool ai_settle_new_town()
	{
		//----- locate a suitable camp for the new town to settle next to ----//

		Nation ownNation = NationArray[NationId];
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

		if (World.LocateSpace(ref xLoc, ref yLoc, bestCamp.loc_x2, bestCamp.loc_y2,
			    InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, UnitConstants.UNIT_LAND, curRegionId, true))
		{
			settle(xLoc, yLoc);
			return true;
		}

		return false;
	}

	#endregion
}
