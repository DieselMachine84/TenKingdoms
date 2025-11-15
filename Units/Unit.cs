using System;
using System.Collections.Generic;

namespace TenKingdoms;

public abstract partial class Unit : Sprite
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
	public int UnitModeParam { get; set; } // if UnitMode == UNIT_MODE_REBEL, UnitModeParam is rebelId this unit belongs to
	public int SpyId { get; set; }
	public int NationContribution { get; private set; } // For humans: contribution to the nation. For weapons: the tech level!
	public int TotalReward { get; private set; } // total amount of reward you have given to the unit
	public int WeaponVersion { get; private set; }
	public bool AggressiveMode { get; set; }
	public int HomeCampId { get; set; }
	public TeamInfo TeamInfo { get; } = new TeamInfo();
	public int TeamId { get; set; } // id of defined team
	public int LeaderId { get; set; } // id of this unit's leader
	public int GroupId { get; set; } // the group id this unit belong to if it is selected
	public bool AIUnit { get; set; }

	
	public int ActionMode { get; set; }
	public int ActionParam { get; set; }
	public int ActionLocX { get; set; }
	public int ActionLocY { get; set; }
	public int ActionMisc { get; set; }
	public int ActionMiscParam { get; set; }
	public int ActionMode2 { get; set; } // store the existing action for speeding up the performance if same action is ordered.
	public int ActionPara2 { get; set; } // to re-activate the unit if its CurAction is idle
	public int ActionLocX2 { get; set; }
	public int ActionLocY2 { get; set; }
	private int OriginalActionMode { get; set; }
	private int OriginalActionParam { get; set; }
	private int OriginalActionLocX { get; set; }
	private int OriginalActionLocY { get; set; }
	// the original location of the attacking target
	// when the Attack() function is called ActionLocX2 & ActionLocY2 will change when the unit move,
	// but these two will not.
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

	private bool _canAttack;
	public bool CanAttack // true able to attack, false unable to attack no matter what AttackCount is
	{
		get => _canAttack && AttackCount > 0;
		protected set => _canAttack = value;
	}
	private int CanGuard { get; set; } // bit0 = standing guard, bit1 = moving guard
	public bool CanStandGuard => (CanGuard & 1) != 0;
	public bool CanMoveGuard => (CanGuard & 2) != 0;
	public bool CanAttackGuard => (CanGuard & 4) != 0;


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
	private static int CycleWaitUnitArrayMultiplier { get; set; }

	private static int HelpMode { get; set; }
	private static int HelpAttackTargetId { get; set; }

	
	protected ConfigAdv ConfigAdv => Sys.Instance.ConfigAdv;
	protected Info Info => Sys.Instance.Info;
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

	public virtual void Init(int unitType, int nationId, int rank, int unitLoyalty, int startLocX, int startLocY)
	{
		NationId = nationId;
		Rank = rank; // Rank must be initialized before InitUnitType() as InitUnitType() may overwrite it
		NationContribution = 0; // nation_contribution must be initialized before InitUnitType() as InitUnitType() may overwrite it

		InitUnitType(unitType);

		GroupId = UnitArray.CurGroupId++;
		RaceId = UnitRes[UnitType].race_id;

		if (RaceId != 0)
		{
			NameId = RaceRes[RaceId].get_new_name_id();
		}
		else //---- init non-human unit ----//
		{
			if (NationId != 0)
				NameId = ++NationArray[NationId].last_unit_name_id_array[UnitType - 1];
			else
				NameId = 0;
		}

		AIUnit = (NationId != 0 && NationArray[NationId].nation_type == NationBase.NATION_AI);
		AIActionId = 0;

		ActionMisc = UnitConstants.ACTION_MISC_STOP;
		ActionMiscParam = 0;

		ActionMode = ActionMode2 = UnitConstants.ACTION_STOP;
		ActionParam = ActionPara2 = 0;
		ActionLocX = ActionLocY = ActionLocX2 = ActionLocY2 = -1;

		RangeAttackLocX = -1;
		RangeAttackLocY = -1;

		MaxHitPoints = UnitRes[UnitType].hit_points;
		HitPoints = MaxHitPoints;

		Loyalty = unitLoyalty;
		UpdateLoyalty();

		CanGuard = 0;
		CanAttack = true;
		AggressiveMode = true; // the default mode is true

		//--------- init skill potential ---------//

		if (Misc.Random(10) == 0) // 1 out of 10 has a higher than normal potential in this skill
		{
			Skill.SkillPotential = 50 + Misc.Random(51); // 50 to 100 potential
		}

		//------ initialize the base Sprite class -------//

		if (startLocX >= 0)
		{
			InitSprite(startLocX, startLocY);
		}
		else
		{
			CurX = -1;
			CurY = -1;
		}

		AttackDirection = FinalDir;

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
	}

	public override void Deinit()
	{
		if (UnitType == 0)
			return;

		if (NationId != 0)
		{
			if (Rank == RANK_KING) // check NationId because monster kings will die also.
			{
				KingDie();
			}
			else if (Rank == RANK_GENERAL)
			{
				GeneralDie();
			}
		}

		//---- if this is a general, deinit its link with its soldiers ----//
		//
		// We do not use TeamInfo because monsters and rebels also use
		// LeaderId and they do not keep the member info in TeamInfo.
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
				UnitMarine ship = (UnitMarine)UnitArray[UnitModeParam];
				ship.del_unit(SpriteId);
			}
		}

		//----- if this is a ship in the harbor -----//

		else if (UnitMode == UnitConstants.UNIT_MODE_IN_HARBOR)
		{
			// the ship may have been destroyed at the same time. Actually when the ship is destroyed,
			// all units onboard are killed and this function is called.
			if (!FirmArray.IsDeleted(UnitModeParam))
			{
				FirmHarbor harbor = (FirmHarbor)FirmArray[UnitModeParam];
				harbor.del_hosted_ship(SpriteId);
			}
		}

		//----- if this unit is a constructor in a firm -------//

		else if (UnitMode == UnitConstants.UNIT_MODE_CONSTRUCT)
		{
			FirmArray[UnitModeParam].BuilderId = 0;
		}

		//-------- if this is a spy ---------//

		if (SpyId != 0)
		{
			SpyArray.DeleteSpy(SpyArray[SpyId]);
			SpyId = 0;
		}

		//-----------------------------------//

		DeinitUnitType();

		//-------- reset seek path ----------//

		ResetPath();

		//----- if CurX == -1, the unit has not yet been drawn -----//

		if (CurX >= 0)
			DeinitSprite();

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

		UnitType = 0;
	}

	public void InitSprite(int startLocX, int startLocY)
	{
		base.Init(UnitRes[UnitType].sprite_id, startLocX, startLocY);

		//--------------------------------------------------------------------//
		// MoveToLocX and MoveToLocY are always the current location of the unit as CurAction == SPRITE_IDLE
		//--------------------------------------------------------------------//

		OriginalActionMode = 0;
		AIOriginalTargetLocX = -1;
		AIOriginalTargetLocY = -1;

		AttackRange = 0;

		MoveToLocX = NextLocX;
		MoveToLocY = NextLocY;

		GoX = NextX;
		GoY = NextY;

		for (int h = 0, y = startLocY; h < SpriteInfo.LocHeight; h++, y++)
		{
			for (int w = 0, x = startLocX; w < SpriteInfo.LocWidth; w++, x++)
			{
				World.GetLoc(x, y).SetUnit(MobileType, SpriteId);
			}
		}

		if (IsOwn() || (NationId != 0 && NationArray[NationId].is_allied_with_player))
		{
			World.Unveil(startLocX, startLocY, startLocX + SpriteInfo.LocWidth - 1, startLocY + SpriteInfo.LocHeight - 1);
			World.Visit(startLocX, startLocY, startLocX + SpriteInfo.LocWidth - 1, startLocY + SpriteInfo.LocHeight - 1,
				UnitRes[UnitType].visual_range, UnitRes[UnitType].visual_extend);
		}
	}

	public void DeinitSprite(bool keepSelected = false)
	{
		if (CurX == -1)
			return;

		//---- if this unit is led by a leader, only mobile units has LeaderId assigned to a leader -----//
		// units are still considered mobile when boarding a ship

		if (LeaderId != 0 && UnitMode != UnitConstants.UNIT_MODE_ON_SHIP)
		{
			if (!UnitArray.IsDeleted(LeaderId)) // the leader unit may have been killed at the same time
				UnitArray[LeaderId].DeleteTeamMember(SpriteId);

			LeaderId = 0;
		}

		//-------- clear the CargoId ----------//

		for (int h = 0, y = NextLocY; h < SpriteInfo.LocHeight; h++, y++)
		{
			for (int w = 0, x = NextLocX; w < SpriteInfo.LocWidth; w++, x++)
			{
				World.GetLoc(x, y).SetUnit(MobileType, 0);
			}
		}

		CurX = -1;
		CurY = -1;

		//---- reset other parameters related to this unit ----//

		if (!keepSelected)
		{
			//TODO rewrite
			//if (Power.command_unit_recno == sprite_recno)
				//Power.command_id = 0;
		}

		DeinitUnitMode();
	}

	private void InitUnitType(int unitType)
	{
		UnitType = unitType;

		UnitInfo unitInfo = UnitRes[UnitType];

		SpriteResId = unitInfo.sprite_id;
		SpriteInfo = SpriteRes[SpriteResId];

		MobileType = unitInfo.mobile_type;

		//--- if this unit is a weapon unit with multiple versions ---//

		SetCombatLevel(100); // set combat level default to 100, for human units, it will be adjusted later by individual functions

		int techLevel;
		if (NationId != 0 && unitInfo.unit_class == UnitConstants.UNIT_CLASS_WEAPON &&
		    (techLevel = unitInfo.nation_tech_level_array[NationId - 1]) > 0)
		{
			WeaponVersion = techLevel;
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

	private void DeinitUnitType()
	{
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
		// by both the true controlling nation and the deceiving nation.
		//
		//-----------------------------------------//

		if (SpyId != 0 && TrueNationId() != NationId)
		{
			// TODO there is no same code in InitUnitType(). Check this
			
			int trueNationId = TrueNationId();

			if (Rank != RANK_KING)
				unitInfo.dec_nation_unit_count(trueNationId);

			if (Rank == RANK_GENERAL)
				unitInfo.dec_nation_general_count(trueNationId);
		}

		//--------- decrease monster count ----------//

		if (UnitRes[UnitType].unit_class == UnitConstants.UNIT_CLASS_MONSTER)
		{
			UnitRes.mobile_monster_count--;
		}
	}

	private void DeinitUnitMode()
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
			SetMode(0); // reset mode
		}

		//----- this is a monster unit defending its town ------//

		else if (UnitMode == UnitConstants.UNIT_MODE_MONSTER && UnitModeParam != 0)
		{
			// TODO is this condition correct? Check
			if (((UnitMonster)this).monster_action_mode != UnitConstants.MONSTER_ACTION_DEFENSE)
			{
				if (!FirmArray.IsDeleted(UnitModeParam))
				{
					FirmMonster firmMonster = (FirmMonster)FirmArray[UnitModeParam];
					firmMonster.reduce_defender_count(Rank);
				}
				SetMode(0); // reset mode
			}
		}
	}

	public void InitFromWorker(Worker worker)
	{
		Skill.SkillId = worker.skill_id;
		Skill.SkillLevel = worker.skill_level;
		Skill.SkillLevelMinor = worker.skill_level_minor;
		SetCombatLevel(worker.combat_level);
		Skill.CombatLevelMinor = worker.combat_level_minor;
		HitPoints = worker.hit_points;
		Loyalty = worker.loyalty();
		Rank = worker.rank_id;

		if (UnitRes[UnitType].unit_class == UnitConstants.UNIT_CLASS_WEAPON)
		{
			WeaponVersion = worker.extra_para;
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

		if (worker.name_id != 0 && worker.race_id != 0) // if this worker is formerly a unit who has a name
			SetName(worker.name_id);

		//------ if the unit is a spy -------//

		if (worker.spy_recno != 0)
		{
			SpyId = worker.spy_recno;
			Spy spy = SpyArray[SpyId];
			AIUnit = spy.CloakedNationId != 0 && NationArray[spy.CloakedNationId].is_ai();
			SetName(spy.NameId);
			spy.SetPlace(Spy.SPY_MOBILE, SpriteId);
		}
	}


	public override void PreProcess()
	{
		//------ if all the hit points are lost, die now ------//
		if (HitPoints <= 0.0 && ActionMode != UnitConstants.ACTION_DIE)
		{
			SetDie();

			if (AIActionId != 0 && NationId != 0)
				NationArray[NationId].action_failure(AIActionId, SpriteId);

			return;
		}

		if (Config.fog_of_war)
		{
			if (IsOwn() || (NationId != 0 && NationArray[NationId].is_allied_with_player))
			{
				World.Visit(NextLocX, NextLocY, NextLocX + SpriteInfo.LocWidth - 1, NextLocY + SpriteInfo.LocHeight - 1,
					UnitRes[UnitType].visual_range, UnitRes[UnitType].visual_extend);
			}
		}

		//--------- process action corresponding to ActionMode ----------//

		switch (ActionMode)
		{
			case UnitConstants.ACTION_ATTACK_UNIT:
				//------------------------------------------------------------------//
				// if unit is in defense mode, check situation to follow the target or return back to camp
				//------------------------------------------------------------------//
				if (ActionMode != ActionMode2)
				{
					if (ActionMode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET)
					{
						if (!DefenseFollowTarget()) // false if abort attacking
							break; // cancel attack and go back to military camp
					}
					else if (ActionMode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET)
					{
						if (!DefendTownFollowTarget())
							break;
					}
					else if (ActionMode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
					{
						if (!MonsterDefendFollowTarget())
							break;
					}
				}

				ProcessAttackUnit();
				break;

			case UnitConstants.ACTION_ATTACK_FIRM:
				ProcessAttackFirm();
				break;

			case UnitConstants.ACTION_ATTACK_TOWN:
				ProcessAttackTown();
				break;

			case UnitConstants.ACTION_ATTACK_WALL:
				ProcessAttackWall();
				break;

			case UnitConstants.ACTION_ASSIGN_TO_FIRM:
			case UnitConstants.ACTION_ASSIGN_TO_TOWN:
			case UnitConstants.ACTION_ASSIGN_TO_VEHICLE:
				ProcessAssign();
				break;

			case UnitConstants.ACTION_ASSIGN_TO_SHIP:
				ProcessAssignToShip();
				break;

			case UnitConstants.ACTION_SHIP_TO_BEACH:
				ProcessShipToBeach();
				break;

			case UnitConstants.ACTION_BUILD_FIRM:
				ProcessBuildFirm();
				break;

			case UnitConstants.ACTION_SETTLE:
				ProcessSettle();
				break;

			case UnitConstants.ACTION_BURN:
				ProcessBurn();
				break;

			case UnitConstants.ACTION_GO_CAST_POWER:
				ProcessGoCastPower();
				break;
		}

		//-****** don't add code here, the unit may be removed after the above function call*******-//
	}

	private void ProcessAssign()
	{
		if (CurAction != SPRITE_IDLE)
		{
			//------------------------------------------------------------------------------------------------
			// change unit's action if the firm/town/unit assign to has been deleted or has changed its nation
			//------------------------------------------------------------------------------------------------
			switch (ActionMode2)
			{
				case UnitConstants.ACTION_ASSIGN_TO_FIRM:
				case UnitConstants.ACTION_AUTO_DEFENSE_BACK_CAMP:
				case UnitConstants.ACTION_MONSTER_DEFEND_BACK_FIRM:
					if (FirmArray.IsDeleted(ActionParam))
					{
						Stop2();
						return;
					}
					else
					{
						Firm firm = FirmArray[ActionParam];
						if (firm.NationId != NationId && !firm.CanAssignCapture())
						{
							Stop2();
							return;
						}
					}

					break;

				case UnitConstants.ACTION_ASSIGN_TO_TOWN:
				case UnitConstants.ACTION_DEFEND_TOWN_BACK_TOWN:
					if (TownArray.IsDeleted(ActionParam) || TownArray[ActionParam].NationId != NationId)
					{
						Stop2();
						return;
					}

					break;

				case UnitConstants.ACTION_ASSIGN_TO_VEHICLE:
					if (UnitArray.IsDeleted(ActionParam) || UnitArray[ActionParam].NationId != NationId)
					{
						Stop2();
						return;
					}

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
					FirmInfo firmInfo = FirmRes[firm.FirmType];

					//---------- resume action if the unit has not reached the firm surrounding ----------//
					if (!IsInSurrounding(MoveToLocX, MoveToLocY, SpriteInfo.LocWidth,
						    ActionLocX, ActionLocY, firmInfo.loc_width, firmInfo.loc_height))
					{
						//------------ not in the surrounding -----------//
						if (ActionMode != ActionMode2) // for defense mode
							SetMoveToSurround(ActionLocX, ActionLocY, firmInfo.loc_width, firmInfo.loc_height, UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO);
						return;
					}

					//------------ in the firm surrounding ------------//
					if (!firm.UnderConstruction)
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
						// These parameters will be destroyed after calling AssignUnit()
						//---------------------------------------------------------------//
						int nationId = NationId;
						int unitId = SpriteId;
						int aiActionId = AIActionId;

						ResetActionParameters2();

						firm.AssignUnit(SpriteId);

						//----------------------------------------------------------//
						// firm.AssignUnit() must be done first.  Then a town will be created
						// and the reaction to build other firms requires the location of the town.
						//----------------------------------------------------------//

						if (aiActionId != 0)
							NationArray[nationId].action_finished(aiActionId, unitId);

						if (UnitArray.IsDeleted(unitId))
							return;

						//--- else the firm is full, the unit's skill level is lower than those in firm, or no space to create town ---//
					}
					else
					{
						//---------- change the builder ------------//
						if (AIUnit && firm.UnderConstruction)
							return; // not allow AI to change firm builder

						ResetActionParameters2();
						if (Skill.GetSkillLevel(Skill.SKILL_CONSTRUCTION) != 0 || Skill.GetSkillLevel(firm.FirmSkillId) != 0)
							firm.SetBuilder(SpriteId);
					}

					//------------ update UnitArray's selected parameters ------------//
					ResetActionParameters();
					//TODO selection
					/*if (SelectedFlag)
					{
						SelectedFlag = false;
						UnitArray.SelectedCount--;
					}*/
				}
				else if (loc.IsTown() && loc.TownId() == ActionParam)
				{
					//---------------- a town on the location -----------------//
					if (!IsInSurrounding(MoveToLocX, MoveToLocY, SpriteInfo.LocWidth,
						    ActionLocX, ActionLocY, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT))
					{
						return;
					}

					int actionParam = ActionParam;

					if (AIActionId != 0 && NationId != 0)
						NationArray[NationId].action_finished(AIActionId, SpriteId);

					//------------ update UnitArray's selected parameters ------------//
					ResetActionParameters2();
					ResetActionParameters();
					//TODO selection
					/*if (SelectedFlag)
					{
						SelectedFlag = false;
						UnitArray.SelectedCount--;
					}*/

					//-------------- assign the unit to the town -----------------//
					TownArray[actionParam].AssignUnit(this);
				}

				//------ embarking a ship ------//

				else if (loc.HasUnit(UnitConstants.UNIT_SEA) && loc.UnitId(UnitConstants.UNIT_SEA) == ActionParam)
				{
					//------------ update UnitArray's selected parameters ------------//
					ResetActionParameters2();
					ResetActionParameters();
					//TODO selection
					/*if (SelectedFlag)
					{
						SelectedFlag = false;
						UnitArray.SelectedCount--;
					}*/

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
		}
	}
	
	private void ProcessAssignToShip()
	{
		//---------------------------------------------------------------------------//
		// clear unit's action if situation is changed
		//---------------------------------------------------------------------------//
		if (UnitArray.IsDeleted(ActionPara2))
		{
			Stop2();
			return; // stop the unit as the ship is deleted
		}

		UnitMarine ship = (UnitMarine)UnitArray[ActionPara2];
		if (ship.NationId != NationId)
		{
			Stop2();
			return; // stop the unit as the ship's NationId != the unit's NationId
		}

		if (ship.ActionMode2 != UnitConstants.ACTION_SHIP_TO_BEACH)
		{
			Stop2(); // the ship has changed its action
			return;
		}

		int curLocX = NextLocX;
		int curLocY = NextLocY;
		int shipLocX = ship.NextLocX;
		int shipLocY = ship.NextLocY;

		if (ship.CurX == ship.NextX && ship.CurY == ship.NextY && Math.Abs(shipLocX - curLocX) <= 1 && Math.Abs(shipLocY - curLocY) <= 1)
		{
			//----------- assign the unit now -----------//
			if (Math.Abs(CurX - NextX) < SpriteInfo.Speed && Math.Abs(CurY - NextY) < SpriteInfo.Speed)
			{
				if (AIActionId != 0)
					NationArray[NationId].action_finished(AIActionId, SpriteId);

				Stop2();
				SetDir(curLocX, curLocY, shipLocX, shipLocY);
				ship.load_unit(SpriteId);
				return;
			}
		}
		else if (CurAction == SPRITE_IDLE)
			SetDir(curLocX, curLocY, ship.MoveToLocX, ship.MoveToLocY);

		//---------------------------------------------------------------------------//
		// update location to embark
		//---------------------------------------------------------------------------//
		int shipActionLocX = ship.ActionLocX2;
		int shipActionLocY = ship.ActionLocY2;
		if (Math.Abs(shipActionLocX - ActionLocX2) > 1 || Math.Abs(shipActionLocY - ActionLocY2) > 1)
		{
			if (shipActionLocX != ActionLocX2 || shipActionLocY != ActionLocY2)
			{
				Location unitLoc = World.GetLoc(curLocX, curLocY);
				Location shipActionLoc = World.GetLoc(shipActionLocX, shipActionLocY);
				if (unitLoc.RegionId != shipActionLoc.RegionId)
				{
					Stop2();
					return;
				}

				AssignToShip(shipActionLocX, shipActionLocY, ActionPara2);
			}
		}
	}

	private void ProcessShipToBeach()
	{
		//----- action_mode never clear, in_beach to skip idle checking
		if (CurAction == SPRITE_IDLE)
		{
			int shipLocX = NextLocX;
			int shipLocY = NextLocY;
			if (shipLocX == MoveToLocX && shipLocY == MoveToLocY)
			{
				if (Math.Abs(MoveToLocX - ActionLocX2) <= 2 && Math.Abs(MoveToLocY - ActionLocY2) <= 2)
				{
					//------------------------------------------------------------------------------//
					// determine whether extra_move is required
					//------------------------------------------------------------------------------//
					UnitMarine ship = (UnitMarine)this;
					switch (ship.extra_move_in_beach)
					{
						case UnitMarine.NO_EXTRA_MOVE:
							if (Math.Abs(shipLocX - ActionLocX2) > 1 || Math.Abs(shipLocY - ActionLocY2) > 1)
							{
								ship.extra_move();
							}
							else
							{
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
					}
				}
			}
			else
				ResetActionParameters();
		}
		else if (CurAction == SPRITE_TURN && IsDirCorrect())
			SetMove();
	}

	private void ProcessBuildFirm()
	{
		if (CurAction == SPRITE_IDLE) // the unit is at the build location now
		{
			// **BUGHERE, the unit shouldn't be hidden when building structures otherwise,
			// it's cargo_recno will be conflict with the structure's cargo_recno

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
				if (!IsInSurrounding(MoveToLocX, MoveToLocY, SpriteInfo.LocWidth,
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
				if (shouldProceed && World.CanBuildFirm(ActionLocX, ActionLocY, ActionParam, SpriteId) != 0 && CanBuild(ActionParam))
				{
					//---------------------------------------------------------------------------//
					// if the unit inside the firm location, deinit the unit to free the space for building firm
					//---------------------------------------------------------------------------//
					if (MoveToLocX >= ActionLocX && MoveToLocX < ActionLocX + width &&
					    MoveToLocY >= ActionLocY && MoveToLocY < ActionLocY + height)
						DeinitSprite();

					if (FirmArray.BuildFirm(ActionLocX, ActionLocY, NationId, ActionParam, SpriteInfo.SpriteCode, SpriteId) != 0)
					{
						//--------- able to build the firm --------//
						ResetActionParameters2();
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

			ResetActionParameters();
		}
	}

	private void ProcessSettle()
	{
		if (CurAction == SPRITE_IDLE) // the unit is at the settle location now
		{
			ResetPath();

			if (CurLocX == MoveToLocX && CurLocY == MoveToLocY)
			{
				if (!IsInSurrounding(MoveToLocX, MoveToLocY, SpriteInfo.LocWidth, ActionLocX, ActionLocY,
					    InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT))
				{
					return;
				}

				Location location = World.GetLoc(ActionLocX, ActionLocY);
				if (!location.IsTown())
				{
					int unitId = SpriteId;

					ResetActionParameters2();
					TownArray.Settle(SpriteId, ActionLocX, ActionLocY);

					if (UnitArray.IsDeleted(unitId))
						return;

					ResetActionParameters();
				}
				else if (TownArray[location.TownId()].NationId == NationId)
				{
					//---------- a town zone already exists ---------//
					Assign(ActionLocX, ActionLocY);
					return;
				}
			}
			else
			{
				// TODO why code is different with ProcessBuildFirm?
				ResetActionParameters();
			}
		}
	}

	private void ProcessBurn()
	{
		if (CurAction == SPRITE_IDLE) // the unit is at the burn location now
		{
			if (NextLocX == ActionLocX && NextLocY == ActionLocY)
			{
				ResetActionParameters2();
				SetDir(MoveToLocX, MoveToLocY, ActionLocX, ActionLocY);
				World.SetupFire(ActionLocX, ActionLocY);
			}

			ResetActionParameters();
		}
	}

	private void ProcessGoCastPower()
	{
		UnitGod unitGod = (UnitGod)this;
		if (CurAction == SPRITE_IDLE)
		{
			//----------------------------------------------------------------------------------------//
			// Checking condition to do casting power or resume action
			//----------------------------------------------------------------------------------------//
			if (Misc.points_distance(CurLocX, CurLocY, ActionLocX2, ActionLocY2) <= UnitConstants.DO_CAST_POWER_RANGE)
			{
				if (NextLocX != ActionLocX2 || NextLocY != ActionLocY2)
				{
					SetDir(NextLocX, NextLocY, ActionLocX2, ActionLocY2);
				}

				SetAttack(); // set cur_action=sprite_attack to cast power 
				CurFrame = 1;
			}
			else
			{
				GoCastPower(ActionLocX2, ActionLocY2, unitGod.cast_power_type, InternalConstants.COMMAND_AUTO);
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
				AddCloseAttackEffect();

				unitGod.cast_power(ActionLocX2, ActionLocY2);
				SetRemainAttackDelay();
			}

			if (CurFrame == 1 && RemainAttackDelay == 0) // last frame of delaying
				Stop2();
		}
	}

	public int CanAssignToFirm(int firmId)
	{
		Firm firm = FirmArray[firmId];
		FirmInfo firmInfo = FirmRes[firm.FirmType];

		switch (UnitRes[UnitType].unit_class)
		{
			case UnitConstants.UNIT_CLASS_HUMAN:
				if (NationId == firm.NationId)
				{
					if (Skill.SkillId == Skill.SKILL_CONSTRUCTION && firm.FirmType != Firm.FIRM_MONSTER)
					{
						return 3;
					}

					switch (firm.FirmType)
					{
						case Firm.FIRM_CAMP:
							return Rank == RANK_SOLDIER ? 1 : 2;

						case Firm.FIRM_BASE:
							if (RaceId == firm.RaceId)
							{
								if (Skill.SkillId == 0 || Skill.SkillId == Skill.SKILL_PRAYING) // non-skilled worker
									return 1;
								if (Rank != RANK_SOLDIER)
									return 2;
							}

							break;

						default:
							return Rank == RANK_SOLDIER && firmInfo.need_unit() ? 1 : 0;
					}
				}

				break;

			case UnitConstants.UNIT_CLASS_WEAPON:
				if (firm.FirmType == Firm.FIRM_CAMP && NationId == firm.NationId)
					return 1;
				break;

			case UnitConstants.UNIT_CLASS_SHIP:
				if (firm.FirmType == Firm.FIRM_HARBOR && NationId == firm.NationId)
					return 1;
				break;

			case UnitConstants.UNIT_CLASS_MONSTER:
				if (firm.FirmType == Firm.FIRM_MONSTER && MobileType == UnitConstants.UNIT_LAND)
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

	public void Assign(int assignLocX, int assignLocY, int curAssignUnitNum = 1)
	{
		if (IsUnitDead())
			return;

		//----------- BUGHERE : cannot assign when on a ship -----------//
		if (!IsVisible())
			return;

		//----------- cannot assign for caravan -----------//
		if (UnitType == UnitConstants.UNIT_CARAVAN)
			return;

		//----------------------------------------------------------------//
		// move there if the destination in other territory
		//----------------------------------------------------------------//
		Location loc = World.GetLoc(assignLocX, assignLocY);
		int unitRegionId = World.GetLoc(NextLocX, NextLocY).RegionId;
		if (loc.IsFirm())
		{
			Firm firm = FirmArray[loc.FirmId()];
			bool differentRegions = false;

			if (firm.FirmType == Firm.FIRM_HARBOR)
			{
				FirmHarbor harbor = (FirmHarbor)firm;
				switch (UnitRes[UnitType].unit_class)
				{
					case UnitConstants.UNIT_CLASS_HUMAN:
						if (unitRegionId != harbor.land_region_id)
							differentRegions = true;
						break;

					case UnitConstants.UNIT_CLASS_SHIP:
						if (unitRegionId != harbor.sea_region_id)
							differentRegions = true;
						break;
				}
			}
			else if (unitRegionId != loc.RegionId)
			{
				differentRegions = true;
			}

			if (differentRegions)
			{
				MoveToFirmSurround(assignLocX, assignLocY, SpriteInfo.LocWidth, SpriteInfo.LocHeight, firm.FirmType);
				return;
			}
		}
		else if (loc.IsTown())
		{
			if (unitRegionId != loc.RegionId)
			{
				MoveToTownSurround(assignLocX, assignLocY, SpriteInfo.LocWidth, SpriteInfo.LocHeight);
				return;
			}
		}

		//---------------- define parameters --------------------//
		int width, height;
		int buildingType;
		int newActionParam;

		if (loc.IsFirm())
		{
			int firmId = loc.FirmId();

			//----------------------------------------------------------------//
			// ActionMode2: checking for equal action or idle action
			//----------------------------------------------------------------//
			if (ActionMode2 == UnitConstants.ACTION_ASSIGN_TO_FIRM && ActionPara2 == firmId &&
			    ActionLocX2 == assignLocX && ActionLocY2 == assignLocY)
			{
				if (CurAction != SPRITE_IDLE)
					return;
			}
			else
			{
				//----------------------------------------------------------------//
				// ActionMode2: store new order
				//----------------------------------------------------------------//
				ActionMode2 = UnitConstants.ACTION_ASSIGN_TO_FIRM;
				ActionPara2 = firmId;
				ActionLocX2 = assignLocX;
				ActionLocY2 = assignLocY;
			}

			Firm firm = FirmArray[firmId];
			FirmInfo firmInfo = FirmRes[firm.FirmType];

			if (CanAssignToFirm(firmId) == 0)
			{
				MoveToFirmSurround(assignLocX, assignLocY, SpriteInfo.LocWidth, SpriteInfo.LocHeight, firm.FirmType);
				return;
			}

			newActionParam = firmId;
			width = firmInfo.loc_width;
			height = firmInfo.loc_height;
			buildingType = UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO;
		}
		else if (loc.IsTown())
		{
			if (UnitRes[UnitType].unit_class != UnitConstants.UNIT_CLASS_HUMAN)
				return;

			int townId = loc.TownId();

			//----------------------------------------------------------------//
			// ActionMode2: checking for equal action or idle action
			//----------------------------------------------------------------//
			if (ActionMode2 == UnitConstants.ACTION_ASSIGN_TO_TOWN && ActionPara2 == townId &&
			    ActionLocX2 == assignLocX && ActionLocY2 == assignLocY)
			{
				if (CurAction != SPRITE_IDLE)
					return;
			}
			else
			{
				//----------------------------------------------------------------//
				// ActionMode2: store new order
				//----------------------------------------------------------------//
				ActionMode2 = UnitConstants.ACTION_ASSIGN_TO_TOWN;
				ActionPara2 = townId;
				ActionLocX2 = assignLocX;
				ActionLocY2 = assignLocY;
			}

			Town targetTown = TownArray[townId];
			if (TownArray[townId].NationId != NationId)
			{
				MoveToTownSurround(assignLocX, assignLocY, SpriteInfo.LocWidth, SpriteInfo.LocHeight);
				return;
			}

			newActionParam = townId;
			width = targetTown.LocWidth();
			height = targetTown.LocHeight();
			buildingType = UnitConstants.BUILDING_TYPE_TOWN_MOVE_TO;
		}
		else
		{
			Stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//-----------------------------------------------------------------//
		// order the sprite to stop as soon as possible (new order)
		//-----------------------------------------------------------------//
		Stop();
		SetMoveToSurround(assignLocX, assignLocY, width, height, buildingType, 0, 0, curAssignUnitNum);

		//-----------------------------------------------------------------//
		// able to reach building surrounding, set action parameters
		//-----------------------------------------------------------------//
		ActionParam = newActionParam;
		ActionLocX = assignLocX;
		ActionLocY = assignLocY;

		switch (buildingType)
		{
			case UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO:
				ActionMode = UnitConstants.ACTION_ASSIGN_TO_FIRM;
				break;

			case UnitConstants.BUILDING_TYPE_TOWN_MOVE_TO:
				ActionMode = UnitConstants.ACTION_ASSIGN_TO_TOWN;
				break;
		}
	}

	public void AssignToShip(int destLocX, int destLocY, int shipId, int miscNo = 0)
	{
		if (IsUnitDead())
			return;

		if (UnitArray.IsDeleted(shipId))
			return;

		//----------------------------------------------------------------//
		// ActionMode2: checking for equal action or idle action
		//----------------------------------------------------------------//
		if (ActionMode2 == UnitConstants.ACTION_ASSIGN_TO_SHIP && ActionPara2 == shipId &&
		    ActionLocX2 == destLocX && ActionLocY2 == destLocY)
		{
			if (CurAction != SPRITE_IDLE)
				return;
		}
		else
		{
			//----------------------------------------------------------------//
			// ActionMode2: store new order
			//----------------------------------------------------------------//
			ActionMode2 = UnitConstants.ACTION_ASSIGN_TO_SHIP;
			ActionPara2 = shipId;
			ActionLocX2 = destLocX;
			ActionLocY2 = destLocY;
		}

		//----- order the sprite to stop as soon as possible -----//
		Stop(); // new order

		Unit ship = UnitArray[shipId];
		int shipLocX = ship.NextLocX;
		int shipLocY = ship.NextLocY;
		int resultLocX = -1;
		int resultLocY = -1;
		if (miscNo == 0)
		{
			//-------- find a suitable location since no offset location is given ---------//
			if (Math.Abs(shipLocX - ActionLocX2) <= 1 && Math.Abs(shipLocY - ActionLocY2) <= 1)
			{
				int regionId = World.GetLoc(NextLocX, NextLocY).RegionId;
				for (int i = 2; i <= 9; i++)
				{
					Misc.cal_move_around_a_point(i, 3, 3, out int xShift, out int yShift);
					int checkLocX = shipLocX + xShift;
					int checkLocY = shipLocY + yShift;
					if (!Misc.IsLocationValid(checkLocX, checkLocY))
						continue;

					if (World.GetLoc(checkLocX, checkLocY).RegionId != regionId)
						continue;

					resultLocX = checkLocX;
					resultLocY = checkLocY;
					break;
				}
			}
			else
			{
				resultLocX = ActionLocX2;
				resultLocY = ActionLocY2;
			}
		}
		else
		{
			//------------ offset location is given, move there directly ----------//
			Misc.cal_move_around_a_point(miscNo, GameConstants.MapSize, GameConstants.MapSize, out int xShift, out int yShift);
			resultLocX = destLocX + xShift;
			resultLocY = destLocY + yShift;
		}

		//--------- start searching ----------//
		int curLocX = NextLocX;
		int curLocY = NextLocY;
		if ((curLocX != destLocX || curLocY != destLocY) && (Math.Abs(shipLocX - curLocX) > 1 || Math.Abs(shipLocY - curLocY) > 1))
		{
			Search(resultLocX, resultLocY, 1);
		}

		//-------- set action parameters ----------//
		ActionMode = UnitConstants.ACTION_ASSIGN_TO_SHIP;
		ActionParam = shipId;
		ActionLocX = destLocX;
		ActionLocY = destLocY;
	}

	public void ShipToBeach(int destLocX, int destLocY, out int finalDestLocX, out int finalDestLocY) // for ship only
	{
		//----------------------------------------------------------------//
		// change to MoveTo if the unit is dead or if the ship cannot carry units
		//----------------------------------------------------------------//
		if (IsUnitDead() || UnitRes[UnitType].carry_unit_capacity <= 0)
		{
			MoveTo(destLocX, destLocY, 1);
			finalDestLocX = finalDestLocY = -1;
			return;
		}

		//----------------------------------------------------------------//
		// calculate new destination
		//----------------------------------------------------------------//
		int curLocX = NextLocX;
		int curLocY = NextLocY;
		int resultLocX, resultLocY;

		Stop();

		if (Math.Abs(destLocX - curLocX) > 1 || Math.Abs(destLocY - curLocY) > 1)
		{
			//-----------------------------------------------------------------------------//
			// get a suitable location in the territory as a reference location
			//-----------------------------------------------------------------------------//
			Location loc = World.GetLoc(destLocX, destLocY);
			int regionId = loc.RegionId;
			int xStep = curLocX - destLocX;
			int yStep = curLocY - destLocY;
			int absXStep = Math.Abs(xStep);
			int absYStep = Math.Abs(yStep);
			int count = (absXStep >= absYStep) ? absXStep : absYStep;
			int sameTerr = 0;

			for (int i = 1; i <= count; i++)
			{
				int x = destLocX + (i * xStep) / count;
				int y = destLocY + (i * yStep) / count;

				loc = World.GetLoc(x, y);
				if (loc.RegionId == regionId)
				{
					if (loc.Walkable())
						sameTerr = i;
				}
			}

			if (sameTerr != 0 && count != 0)
			{
				resultLocX = destLocX + (sameTerr * xStep) / count;
				resultLocY = destLocY + (sameTerr * yStep) / count;
			}
			else
			{
				resultLocX = destLocX;
				resultLocY = destLocY;
			}

			//------------------------------------------------------------------------------//
			// find the path from the ship location in the ocean to the reference location in the territory
			//------------------------------------------------------------------------------//
			if (!ShipToBeachPathEdit(ref resultLocX, ref resultLocY, regionId))
			{
				finalDestLocX = finalDestLocY = -1;
				return; // calling MoveTo() instead
			}
		}
		else
		{
			resultLocX = destLocX;
			resultLocY = destLocY;
		}

		ActionMode = ActionMode2 = UnitConstants.ACTION_SHIP_TO_BEACH;
		ActionParam = ActionPara2 = 0;
		finalDestLocX = ActionLocX = ActionLocX2 = resultLocX;
		finalDestLocY = ActionLocY = ActionLocY2 = resultLocY;
	}

	public void BuildFirm(int buildLocX, int buildLocY, int firmType, int remoteAction)
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

		if (IsUnitDead())
			return;

		//----------------------------------------------------------------//
		// location is blocked, cannot build, so move there instead
		//----------------------------------------------------------------//
		if (World.CanBuildFirm(buildLocX, buildLocY, firmType, SpriteId) == 0)
		{
			MoveTo(buildLocX, buildLocY);
			return;
		}

		//----------------------------------------------------------------//
		// different territory
		//----------------------------------------------------------------//

		int harborDir = World.CanBuildFirm(buildLocX, buildLocY, firmType, SpriteId);
		int goLocX = buildLocX, goLocY = buildLocY;
		if (FirmRes[firmType].tera_type == 4)
		{
			switch (harborDir)
			{
				case 1: // north exit
					goLocX += 1;
					goLocY += 2;
					break;
				case 2: // south exit
					goLocX += 1;
					break;
				case 4: // west exit
					goLocX += 2;
					goLocY += 1;
					break;
				case 8: // east exit
					goLocY += 1;
					break;
				default:
					MoveTo(buildLocX, buildLocY);
					return;
			}

			if (World.GetLoc(NextLocX, NextLocY).RegionId != World.GetLoc(goLocX, goLocY).RegionId)
			{
				MoveTo(buildLocX, buildLocY);
				return;
			}
		}
		else
		{
			if (World.GetLoc(NextLocX, NextLocY).RegionId != World.GetLoc(buildLocX, buildLocY).RegionId)
			{
				MoveTo(buildLocX, buildLocY);
				return;
			}
		}

		//----------------------------------------------------------------//
		// ActionMode2: checking for equal action or idle action
		//----------------------------------------------------------------//
		if (ActionMode2 == UnitConstants.ACTION_BUILD_FIRM && ActionPara2 == firmType &&
		    ActionLocX2 == buildLocX && ActionLocY2 == buildLocY)
		{
			if (CurAction != SPRITE_IDLE)
				return;
		}
		else
		{
			//----------------------------------------------------------------//
			// ActionMode2: store new order
			//----------------------------------------------------------------//
			ActionMode2 = UnitConstants.ACTION_BUILD_FIRM;
			ActionPara2 = firmType;
			ActionLocX2 = buildLocX;
			ActionLocY2 = buildLocY;
		}

		//----- order the sprite to stop as soon as possible -----//
		Stop(); // new order

		//---------------- define parameters -------------------//
		FirmInfo firmInfo = FirmRes[firmType];
		int firmWidth = firmInfo.loc_width;
		int firmHeight = firmInfo.loc_height;

		if (!IsInSurrounding(MoveToLocX, MoveToLocY, SpriteInfo.LocWidth,
			    buildLocX, buildLocY, firmWidth, firmHeight))
		{
			//----------- not in the firm surrounding ---------//
			SetMoveToSurround(buildLocX, buildLocY, firmWidth, firmHeight, UnitConstants.BUILDING_TYPE_FIRM_BUILD, firmType);
		}
		else
		{
			//------- the unit is in the firm surrounding -------//
			SetCur(NextX, NextY);
			SetDir(MoveToLocX, MoveToLocY, buildLocX + firmWidth / 2, buildLocY + firmHeight / 2);
		}

		//----------- set action to build the firm -----------//
		ActionMode = UnitConstants.ACTION_BUILD_FIRM;
		ActionParam = firmType;
		ActionLocX = buildLocX;
		ActionLocY = buildLocY;
	}

	public void Settle(int settleLocX, int settleLocY, int curSettleUnitNum = 1)
	{
		if (IsUnitDead())
			return;

		//---------- no settle for non-human -----------//
		if (UnitRes[UnitType].unit_class != UnitConstants.UNIT_CLASS_HUMAN)
			return;

		//----------------------------------------------------------------//
		// move there if cannot settle
		//----------------------------------------------------------------//
		if (!World.CanBuildTown(settleLocX, settleLocY, SpriteId))
		{
			Location loc = World.GetLoc(settleLocX, settleLocY);
			if (loc.IsTown() && TownArray[loc.TownId()].NationId == NationId)
				Assign(settleLocX, settleLocY);
			else
				MoveTo(settleLocX, settleLocY);
			return;
		}

		//----------------------------------------------------------------//
		// move there if location is in different territory
		//----------------------------------------------------------------//
		if (World.GetLoc(NextLocX, NextLocY).RegionId != World.GetLoc(settleLocX, settleLocY).RegionId)
		{
			MoveTo(settleLocX, settleLocY);
			return;
		}

		//----------------------------------------------------------------//
		// ActionMode2: checking for equal action or idle action
		//----------------------------------------------------------------//
		if (ActionMode2 == UnitConstants.ACTION_SETTLE && ActionLocX2 == settleLocX && ActionLocY2 == settleLocY)
		{
			if (CurAction != SPRITE_IDLE)
				return;
		}
		else
		{
			//----------------------------------------------------------------//
			// ActionMode2: store new order
			//----------------------------------------------------------------//
			ActionMode2 = UnitConstants.ACTION_SETTLE;
			ActionPara2 = 0;
			ActionLocX2 = settleLocX;
			ActionLocY2 = settleLocY;
		}

		//----- order the sprite to stop as soon as possible -----//
		Stop(); // new order

		if (!IsInSurrounding(MoveToLocX, MoveToLocY, SpriteInfo.LocWidth,
			    settleLocX, settleLocY, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT))
		{
			//------------ not in the town surrounding ------------//
			SetMoveToSurround(settleLocX, settleLocY, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT,
				UnitConstants.BUILDING_TYPE_SETTLE, 0, 0, curSettleUnitNum);
		}
		else
		{
			//------- the unit is within the settle location -------//
			SetCur(NextX, NextY);
			SetDir(MoveToLocX, MoveToLocY, settleLocX + InternalConstants.TOWN_WIDTH / 2, settleLocY + InternalConstants.TOWN_HEIGHT / 2);
		}

		//----------- set action to settle -----------//
		ActionMode = UnitConstants.ACTION_SETTLE;
		ActionParam = 0;
		ActionLocX = settleLocX;
		ActionLocY = settleLocY;
	}

	private void Burn(int burnLocX, int burnLocY, int remoteAction)
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

		if (MoveToLocX == burnLocX && MoveToLocY == burnLocY)
			return; // should not burn the unit itself

		//----------------------------------------------------------------//
		// return if the unit is dead
		//----------------------------------------------------------------//
		if (IsUnitDead())
			return;

		//----------------------------------------------------------------//
		// move there instead if ordering to different territory
		//----------------------------------------------------------------//
		if (World.GetLoc(NextLocX, NextLocY).RegionId != World.GetLoc(burnLocX, burnLocY).RegionId)
		{
			MoveTo(burnLocX, burnLocY);
			return;
		}

		//----------------------------------------------------------------//
		// ActionMode2: checking for equal action or idle action
		//----------------------------------------------------------------//
		if (ActionMode2 == UnitConstants.ACTION_BURN && ActionLocX2 == burnLocX && ActionLocY2 == burnLocY)
		{
			if (CurAction != SPRITE_IDLE)
				return;
		}
		else
		{
			//----------------------------------------------------------------//
			// ActionMode2: store new order
			//----------------------------------------------------------------//
			ActionMode2 = UnitConstants.ACTION_BURN;
			ActionPara2 = 0;
			ActionLocX2 = burnLocX;
			ActionLocY2 = burnLocY;
		}

		//----- order the sprite to stop as soon as possible -----//
		Stop(); // new order

		if (Math.Abs(burnLocX - NextLocX) > 1 || Math.Abs(burnLocY - NextLocY) > 1)
		{
			//--- if the unit is not in the burning surrounding location, move there first ---//
			Search(burnLocX, burnLocY, 1, SeekPath.SEARCH_MODE_A_UNIT_IN_GROUP);

			if (MoveToLocX != burnLocX || MoveToLocY != burnLocY) // cannot reach the destination
			{
				ActionMode = UnitConstants.ACTION_BURN;
				ActionParam = 0;
				ActionLocX = burnLocX;
				ActionLocY = burnLocY;
				return; // just move to the closest location returned by shortest path searching
			}
		}
		else
		{
			if (CurX == NextX && CurY == NextY)
				SetDir(NextLocX, NextLocY, burnLocX, burnLocY);
		}

		BurnPathEdit(burnLocX, burnLocY);

		//-- set action if the burning location can be reached, otherwise just move nearby --//
		ActionMode = UnitConstants.ACTION_BURN;
		ActionParam = 0;
		ActionLocX = burnLocX;
		ActionLocY = burnLocY;
	}

	protected void GoCastPower(int castLocX, int castLocY, int castPowerType, int remoteAction)
	{
		if (IsUnitDead())
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
		// ActionMode2: checking for equal action or idle action
		//----------------------------------------------------------------//
		if (ActionMode2 == UnitConstants.ACTION_GO_CAST_POWER && ActionPara2 == 0 &&
		    ActionLocX2 == castLocX && ActionLocY2 == castLocY && unitGod.cast_power_type == castPowerType)
		{
			if (CurAction != SPRITE_IDLE)
				return;
		}
		else
		{
			//----------------------------------------------------------------//
			// ActionMode2: store new order
			//----------------------------------------------------------------//
			ActionMode2 = UnitConstants.ACTION_GO_CAST_POWER;
			ActionPara2 = 0;
			ActionLocX2 = castLocX;
			ActionLocY2 = castLocY;
		}

		//----- order the sprite to stop as soon as possible -----//
		Stop(); // new order

		//------------- do searching if necessary -------------//
		if (Misc.points_distance(NextLocX, NextLocY, castLocX, castLocY) > UnitConstants.DO_CAST_POWER_RANGE)
			Search(castLocX, castLocY, 1);

		//----------- set action to build the firm -----------//
		ActionMode = UnitConstants.ACTION_GO_CAST_POWER;
		ActionParam = 0;
		ActionLocX = castLocX;
		ActionLocY = castLocY;

		unitGod.cast_power_type = castPowerType;
		unitGod.cast_origin_x = NextLocX;
		unitGod.cast_origin_y = NextLocY;
		unitGod.cast_target_x = castLocX;
		unitGod.cast_target_y = castLocY;
	}

	public override void ProcessIdle()
	{
		//---- if the unit is defending the town ----//

		switch (UnitMode)
		{
			case UnitConstants.UNIT_MODE_REBEL:
				if (ActionMode2 == UnitConstants.ACTION_STOP)
				{
					ProcessRebel(); // redirect to ProcessRebel for rebel units
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
							Firm monsterFirm = FirmArray[UnitModeParam];
							Assign(monsterFirm.LocX1, monsterFirm.LocY1);
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

		base.ProcessIdle();

		//---------------------------------------------------------------------------//
		// reset idle blocked attacking unit.  If the previous condition is
		// totally blocked for attacking target, try again now
		// Note: reset blocked_edge is essential for idle unit to reactivate attack action
		//---------------------------------------------------------------------------//
		if (ActionMode >= UnitConstants.ACTION_ATTACK_UNIT && ActionMode <= UnitConstants.ACTION_ATTACK_WALL)
		{
			bool isAllZero = true;
			for (int i = 0; i < BlockedEdges.Length; i++)
			{
				if (BlockedEdges[i] != 0)
					isAllZero = false;
			}

			if (UnitArray.IdleBlockedUnitResetCount != 0 && !isAllZero)
			{
				UnitArray.IdleBlockedUnitResetCount = 0;
				for (int i = 0; i < BlockedEdges.Length; i++)
				{
					BlockedEdges[i] = 0;
				}
			}
		}

		//--------- reactivate action -----------//

		if (ReactivateIdleAction())
			return; // true if an action is reactivated

		//----------- for ai unit idle -----------//

		// only detect attack when the unit is really idle
		if (ActionMode != UnitConstants.ACTION_STOP || ActionMode2 != UnitConstants.ACTION_STOP)
			return;

		if (!CanAttack)
			return;

		//--- only detect attack if in aggressive mode or the unit is a monster ---//

		UnitInfo unitInfo = UnitRes[UnitType];

		if (unitInfo.unit_class == UnitConstants.UNIT_CLASS_MONSTER || AggressiveMode)
		{
			//----------- detect target to attack -----------//

			if (IdleDetectAttack())
				return; // target detected

		}

		//------------------------------------------------------------------//
		// wander around for monster
		//------------------------------------------------------------------//

		if (unitInfo.unit_class == UnitConstants.UNIT_CLASS_MONSTER)
		{
			// TODO check this
			if (Misc.Random(500) == 0)
			{
				const int WANDER_DIST = 20;
				int destX = NextLocX + Misc.Random(WANDER_DIST) - WANDER_DIST / 2;
				int destY = NextLocY + Misc.Random(WANDER_DIST) - WANDER_DIST / 2;
				Misc.BoundLocation(ref destX, ref destY);
				MoveTo(destX, destY);
			}
		}
	}

	private bool ReactivateIdleAction()
	{
		if (ActionMode2 == UnitConstants.ACTION_STOP)
			return false; // return for no idle action

		if (!IsDirCorrect())
			return true; // cheating for turning the direction

		int curLocX = MoveToLocX;
		int curLocY = MoveToLocY;
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
					Stop2();
				else
				{
					Unit unit = UnitArray[ActionPara2];
					SpriteInfo spriteInfo = unit.SpriteInfo;

					if (SpaceForAttack(ActionLocX2, ActionLocY2, unit.MobileType, spriteInfo.LocWidth, spriteInfo.LocHeight))
					{
						AttackUnit(ActionPara2, 0, 0, false);
						hasSearch = true;
						returnFlag = true;
					}
				}

				break;

			case UnitConstants.ACTION_ATTACK_FIRM:
				location = World.GetLoc(ActionLocX2, ActionLocY2);
				if (ActionPara2 == 0 || !location.IsFirm())
					Stop2(); // stop since target is already destroyed
				else
				{
					Firm firm = FirmArray[ActionPara2];
					FirmInfo firmInfo = FirmRes[firm.FirmType];

					if (SpaceForAttack(ActionLocX2, ActionLocY2, UnitConstants.UNIT_LAND, firmInfo.loc_width, firmInfo.loc_height))
					{
						AttackFirm(ActionLocX2, ActionLocY2, 0, 0, 0);
						hasSearch = true;
						returnFlag = true;
					}
				}

				break;

			case UnitConstants.ACTION_ATTACK_TOWN:
				location = World.GetLoc(ActionLocX2, ActionLocY2);
				if (ActionPara2 == 0 || !location.IsTown())
					Stop2(); // stop since target is deleted
				else
				{
					if (SpaceForAttack(ActionLocX2, ActionLocY2, UnitConstants.UNIT_LAND,
						    InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT))
					{
						AttackTown(ActionLocX2, ActionLocY2, 0, 0, 0);
						hasSearch = true;
						returnFlag = true;
					}
				}

				break;

			case UnitConstants.ACTION_ATTACK_WALL:
				location = World.GetLoc(ActionLocX2, ActionLocY2);
				if (!location.IsWall())
					Stop2(); // stop since target doesn't exist
				else
				{
					if (SpaceForAttack(ActionLocX2, ActionLocY2, UnitConstants.UNIT_LAND, 1, 1))
					{
						AttackWall(ActionLocX2, ActionLocY2, 0, 0, 0);
						hasSearch = true;
						returnFlag = true;
					}
				}

				break;

			case UnitConstants.ACTION_ASSIGN_TO_FIRM:
			case UnitConstants.ACTION_ASSIGN_TO_TOWN:
			case UnitConstants.ACTION_ASSIGN_TO_VEHICLE:
				Assign(ActionLocX2, ActionLocY2);
				hasSearch = true;
				WaitingTerm = 0;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_ASSIGN_TO_SHIP:
				AssignToShip(ActionLocX2, ActionLocY2, ActionPara2);
				hasSearch = true;
				WaitingTerm = 0;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_BUILD_FIRM:
				BuildFirm(ActionLocX2, ActionLocY2, ActionPara2, InternalConstants.COMMAND_AUTO);
				hasSearch = true;
				WaitingTerm = 0;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_SETTLE:
				Settle(ActionLocX2, ActionLocY2);
				hasSearch = true;
				WaitingTerm = 0;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_BURN:
				Burn(ActionLocX2, ActionLocY2, InternalConstants.COMMAND_AUTO);
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
				ProcessAutoDefenseAttackTarget();
				hasSearch = true;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET:
				ProcessAutoDefenseDetectTarget();
				//TODO hasSearch = true;?
				returnFlag = true;
				break;

			case UnitConstants.ACTION_AUTO_DEFENSE_BACK_CAMP:
				ProcessAutoDefenseBackCamp();
				hasSearch = true;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET:
				ProcessDefendTownAttackTarget();
				hasSearch = true;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET:
				ProcessDefendTownDetectTarget();
				//TODO hasSearch = true;?
				returnFlag = true;
				break;

			case UnitConstants.ACTION_DEFEND_TOWN_BACK_TOWN:
				ProcessDefendTownBackTown();
				hasSearch = true;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET:
				ProcessMonsterDefendAttackTarget();
				hasSearch = true;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_MONSTER_DEFEND_DETECT_TARGET:
				ProcessMonsterDefendDetectTarget();
				//TODO hasSearch = true;?
				returnFlag = true;
				break;

			case UnitConstants.ACTION_MONSTER_DEFEND_BACK_FIRM:
				ProcessMonsterDefendBackFirm();
				hasSearch = true;
				returnFlag = true;
				break;

			case UnitConstants.ACTION_SHIP_TO_BEACH:
				UnitMarine ship = (UnitMarine)this;
				if (!ship.in_beach || ship.extra_move_in_beach == UnitMarine.EXTRA_MOVE_FINISH)
				{
					ShipToBeach(ActionLocX2, ActionLocY2, out _, out _);
					hasSearch = true;
				}

				returnFlag = true;
				break;

			case UnitConstants.ACTION_GO_CAST_POWER:
				GoCastPower(ActionLocX2, ActionLocY2, ((UnitGod)this).cast_power_type, InternalConstants.COMMAND_AUTO);
				//TODO hasSearch = true;?
				returnFlag = true;
				break;
		}

		if (hasSearch && SeekPath.PathStatus == SeekPath.PATH_IMPOSSIBLE && NextLocX == MoveToLocX && NextLocY == MoveToLocY)
		{
			//TODO check
			
			//-------------------------------------------------------------------------//
			// abort actions since the unit tries to move and move no more.
			//-------------------------------------------------------------------------//
			Stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return true;
		}

		bool abort = false;
		if (returnFlag)
		{
			if (curLocX == MoveToLocX && curLocY == MoveToLocY && SeekPath.PathStatus == SeekPath.PATH_IMPOSSIBLE)
			{
				//TODO check

				if (ActionMode2 == UnitConstants.ACTION_ASSIGN_TO_SHIP || ActionMode2 == UnitConstants.ACTION_SHIP_TO_BEACH ||
				    InAnyDefenseMode())
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
			Stop2(UnitConstants.KEEP_DEFENSE_MODE);
		}

		return false;
	}
	
	private void ProcessRebel()
	{
		Rebel rebel = RebelArray[UnitModeParam];

		// TODO Rebel.REBEL_ATTACK_FIRM is not processed
		switch (rebel.ActionMode)
		{
			case Rebel.REBEL_ATTACK_TOWN:
				if (TownArray.IsDeleted(rebel.ActionParam))
				{
					rebel.SetAction(Rebel.REBEL_IDLE);
				}
				else
				{
					Town town = TownArray[rebel.ActionParam];
					AttackTown(town.LocX1, town.LocY1);
				}

				break;

			case Rebel.REBEL_SETTLE_NEW:
				if (!World.CanBuildTown(rebel.ActionParam, rebel.ActionParam2, SpriteId))
				{
					Location location = World.GetLoc(rebel.ActionParam, rebel.ActionParam2);

					if (location.IsTown() && TownArray[location.TownId()].RebelId == rebel.RebelId)
					{
						rebel.ActionMode = Rebel.REBEL_SETTLE_TO;
					}
					else
					{
						rebel.ActionMode = Rebel.REBEL_IDLE;
					}
				}
				else
				{
					Settle(rebel.ActionParam, rebel.ActionParam2);
				}

				break;

			case Rebel.REBEL_SETTLE_TO:
				Assign(rebel.ActionParam, rebel.ActionParam2);
				break;
		}
	}
	
	public override void ProcessMove()
	{
		//--------------------------------------------------------//
		// if the unit reach its destination, then Cur(X,Y) == Next(X,Y) == Go(X,Y)
		//--------------------------------------------------------//

		if (CurX == GoX && CurY == GoY)
		{
			NextMove();
			if (CurAction != SPRITE_MOVE) // if NextMove() is not successful, the movement has been stopped
				return;

			//---------------------------------------------------------------------------//
			// If (1) the unit is blocked at Cur(X,Y) == Go(X,Y) and Go(X,Y) != destination and 
			//    (2) a new path is generated if calling the previous NextMove(),
			// then Cur(X,Y) still equal to Go(X,Y).
			//
			// The following base.ProcessMove() call will set the unit to SPRITE_IDLE 
			// since Cur(X,Y) == Go(X,Y) Thus, the unit terminates its move although
			// it has not reached its destination.
			//
			// (note: if it has reached its destination, Cur(X,Y) == Go(X,Y) and CurAction = SPRITE_IDLE)
			//
			// if the unit is still moving and Cur(X,Y) == Go(X,Y), call NextMove() again to reset the Go(X,Y).
			//---------------------------------------------------------------------------//
			if (CurX == GoX && CurY == GoY)
				NextMove();
		}

		//--------- process the move, update sprite position ---------//
		//--------------------------------------------------------//
		// if the unit is moving, Cur(X,Y) != Go(X,Y) and
		// if Next(X,Y) != Cur(X,Y), the direction from Cur(X,Y) to Next(X,Y)
		// should equal to that from Cur(X,Y) to Go(X,Y)
		//--------------------------------------------------------//

		base.ProcessMove();

		if (CurX == GoX && CurY == GoY && CurAction == SPRITE_IDLE) // the sprite has reached its destination
		{
			MoveToLocX = NextLocX;
			MoveToLocY = NextLocY;
		}

		//--------------------------------------------------------//
		// after base.ProcessMove(), if the unit is blocked, its CurAction is set to SPRITE_WAIT.
		// Otherwise, its CurAction is still SPRITE_MOVE.  Then Cur(X,Y) != Next(X,Y) if the unit has not reached its destination.
		//--------------------------------------------------------//
	}

	public override void ProcessWait()
	{
		if (!MatchDir())
			return;

		//-----------------------------------------------------//
		// If the unit is moving to the destination and was blocked by something.
		// If it is now no longer blocked, continue the movement.
		//-----------------------------------------------------//
		//
		// When this function is called Next(X,Y) == Cur(X,Y), it's the location of the sprite.
		//
		//-----------------------------------------------------//

		// find out the next location which the sprite should be moving towards

		//-----------------------------------------------------//
		// If the unit is waiting, Go(X,Y) != Cur(X,Y) and Go(X,Y) != Next(X,Y)
		//
		// If the unit is not under swapping, the NextLoc(X,Y) is always the MoveToLoc(X,Y).
		// Thus, the unit is ordered to stop.
		//-----------------------------------------------------//

		if (NextLocX == MoveToLocX && NextLocY == MoveToLocY && !Swapping)
		{
			TerminateMove();
			return; // terminate since already in destination
		}

		int stepCoeff = MoveStepCoeff();
		int nextX = CurX + stepCoeff * MoveXPixels[FinalDir];
		int nextY = CurY + stepCoeff * MoveYPixels[FinalDir];

		int nextLocX = nextX >> InternalConstants.CellWidthShift;
		int nextLocY = nextY >> InternalConstants.CellHeightShift;
		Location location = World.GetLoc(nextLocX, nextLocY);
		bool blocked = !location.IsAccessible(MobileType) || (location.HasUnit(MobileType) && location.UnitId(MobileType) != SpriteId);

		if (!blocked || MoveActionCallFlag)
		{
			//--------- not blocked, continue to move --------//
			WaitingTerm = 0;
			SetMove();
			CurFrame = 1;
			SetNext(nextX, nextY, -stepCoeff, 1);
		}
		else
		{
			//------- blocked, call HandleBlockedMove() ------//
			HandleBlockedMove(location);
		}
	}

	public override bool ProcessDie()
	{
		SERes.sound(CurLocX, CurLocY, CurFrame, 'S', SpriteResId, "DIE");

		//------------- add die effect on first frame --------- //
		if (CurFrame == 1 && UnitRes[UnitType].die_effect_id != 0)
		{
			EffectArray.AddEffect(UnitRes[UnitType].die_effect_id, CurX, CurY, SPRITE_DIE, CurDir,
				MobileType == UnitConstants.UNIT_AIR ? 8 : 2, 0);
		}

		//--------- next frame ---------//
		if (++CurFrame > SpriteInfo.Die.FrameCount)
			return true;

		return false;
	}

	public virtual void Die()
	{
		if (UnitMode == UnitConstants.UNIT_MODE_REBEL)
			RebelArray.DropRebelIdentity(SpriteId);
	}

	public void NextDay()
	{
		if (UnitArray.IsDeleted(SpriteId)) // if its hit points go down to 0, IsDeleted() will return 1.
			return;

		if (NationId != 0)
		{
			PayExpense();

			if (Info.TotalDays % 30 == SpriteId % 30)
			{
				UpdateLoyalty();
			}

			if (Info.TotalDays % 15 == SpriteId % 15)
			{
				if (ThinkBetray())
					return;
			}
		}

		if (Info.TotalDays % 15 == SpriteId % 15) // recover one point per two weeks
		{
			ProcessRecover();
		}

		CurPower += 5;

		if (CurPower > MaxPower)
			CurPower = MaxPower;

		if (Config.king_undie_flag && Rank == RANK_KING && NationId != 0 && !NationArray[NationId].is_ai())
			HitPoints = MaxHitPoints;

		if (NationId != 0 && IsVisible())
			ThinkAggressiveAction();

		// TODO why this code is here?
		if (Skill.CombatLevel > 100)
			Skill.CombatLevel = 100;
	}


	public void Stop(int preserveAction = 0)
	{
		if (ActionMode2 != UnitConstants.ACTION_MOVE)
			ResetWayPoints();

		ResetPath();

		//-------------- keep action or not --------------//
		switch (preserveAction)
		{
			case 0:
			case UnitConstants.KEEP_DEFENSE_MODE:
				ResetActionParameters();
				RangeAttackLocX = RangeAttackLocY = -1; // should set here or reset for attack
				break;

			case UnitConstants.KEEP_PRESERVE_ACTION:
				break;

			/*case UnitConstants.KEEP_DEFEND_TOWN_MODE:
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
				SetIdle();
				break;

			case SPRITE_TURN:
			case SPRITE_WAIT:
				GoX = NextX;
				GoY = NextY;
				MoveToLocX = NextLocX;
				MoveToLocY = NextLocY;
				FinalDir = CurDir;
				TurnDelay = 0;
				SetIdle();
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
							SetIdle();
							return;
						}

						break;

					case UnitMarine.EXTRA_MOVING_IN:
						if (CurX == NextX && CurY == NextY && (CurX != GoX || CurY != GoY))
						{
							// not yet move although location is chosen
							ship.extra_move_in_beach = UnitMarine.NO_EXTRA_MOVE;
						}

						break;

					case UnitMarine.EXTRA_MOVING_OUT:
						if (CurX == NextX && CurY == NextY && (CurX != GoX || CurY != GoY))
						{
							// not yet move although location is chosen
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
					SetIdle();
				break;

			//--- if its current action is SPRITE_ATTACK, stop immediately ---//

			case SPRITE_ATTACK:
				SetNext(CurX, CurY, 0, 1); //********** BUGHERE
				GoX = NextX;
				GoY = NextY;
				MoveToLocX = NextLocX;
				MoveToLocY = NextLocY;
				SetIdle();

				CurFrame = 1;
				break;
		}
	}

	public void Stop2(int preserveAction = 0)
	{
		Stop(preserveAction);
		ResetActionParameters2(preserveAction);

		//----------------------------------------------------------//
		// set the original location of the attacking target when the Attack() function is called,
		// actionLocX2 and actionLocY2 will change when the unit move, but these two will not.
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

	private void ResetActionParameters()
	{
		ActionMode = UnitConstants.ACTION_STOP;
		ActionLocX = ActionLocY = -1;
		ActionParam = 0;
	}

	private void ResetActionParameters2(int keepMode = 0)
	{
		if (keepMode != UnitConstants.KEEP_DEFENSE_MODE || !InAnyDefenseMode())
		{
			ActionMode2 = UnitConstants.ACTION_STOP;
			ActionLocX2 = ActionLocY2 = -1;
			ActionPara2 = 0;
		}
		else
		{
			switch (UnitMode)
			{
				case UnitConstants.UNIT_MODE_DEFEND_TOWN:
					if (ActionMode2 != UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET)
						DefendTownDetectTarget();
					break;

				case UnitConstants.UNIT_MODE_REBEL:
					if (ActionMode2 != UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET)
						DefenseDetectTarget();
					break;

				case UnitConstants.UNIT_MODE_MONSTER:
					if (ActionMode2 != UnitConstants.ACTION_MONSTER_DEFEND_DETECT_TARGET)
						MonsterDefendDetectTarget();
					break;
			}
		}
	}

	public void ResetActionMiscParameters()
	{
		ActionMisc = UnitConstants.ACTION_MISC_STOP;
		ActionMiscParam = 0;
	}

	protected override void SetNext(int newNextX, int newNextY, int param = 0, int blockedChecked = 0)
	{
		int curNextLocX = NextLocX;
		int curNextLocY = NextLocY;
		int newNextLocX = newNextX >> InternalConstants.CellWidthShift;
		int newNextLocY = newNextY >> InternalConstants.CellHeightShift;

		if (curNextLocX != newNextLocX || curNextLocY != newNextLocY)
		{
			if (!MatchDir())
			{
				SetWait();
				return;
			}
		}

		bool blocked = false;
		int locX = -1, locY = -1;

		if (curNextLocX != newNextLocX || curNextLocY != newNextLocY)
		{
			//------- if the next location is blocked ----------//

			if (blockedChecked == 0)
			{
				locX = newNextLocX;
				locY = newNextLocY;
				Location location = World.GetLoc(locX, locY);
				blocked = !location.IsAccessible(MobileType) || (location.HasUnit(MobileType) && location.UnitId(MobileType) != SpriteId);
			}

			//--- no change to NextX & NextY if the new next location is blocked ---//

			if (blocked)
			{
				SetCur(NextX, NextY); // align the sprite to 32x32 location when it stops

				//------ avoid infinitely looping in calling HandleBlockedMove() ------//
				if (BlockedByMember != 0 || MoveActionCallFlag)
				{
					SetWait();
					BlockedByMember = 0;
				}
				else
				{
					HandleBlockedMove(World.GetLoc(locX, locY));
				}
			}
			else
			{
				if (param != 0)
				{
					//----------------------------------------------------------------------------//
					// calculate the _pathNodeDistance as the unit move from one tile to another
					//----------------------------------------------------------------------------//
					_pathNodeDistance += param;
				}

				NextX = newNextX;
				NextY = newNextY;

				Swapping = false;
				BlockedByMember = 0;

				//---- move SpriteId to the next location ------//

				locY = curNextLocY;
				for (int h = 0; h < SpriteInfo.LocHeight; h++, locY++)
				{
					locX = curNextLocX;
					for (int w = 0; w < SpriteInfo.LocWidth; w++, locX++)
					{
						World.GetLoc(locX, locY).SetUnit(MobileType, 0);
					}
				}

				locY = NextLocY;
				for (int h = 0; h < SpriteInfo.LocHeight; h++, locY++)
				{
					locX = NextLocX;
					for (int w = 0; w < SpriteInfo.LocWidth; w++, locX++)
					{
						World.GetLoc(locX, locY).SetUnit( MobileType, SpriteId);
					}
				}

				//--------- explore land ----------//

				if (!Config.explore_whole_map && IsOwn())
				{
					int xLoc1 = Math.Max(0, newNextLocX - GameConstants.EXPLORE_RANGE);
					int yLoc1 = Math.Max(0, newNextLocY - GameConstants.EXPLORE_RANGE);
					int xLoc2 = Math.Min(GameConstants.MapSize - 1, newNextLocX + GameConstants.EXPLORE_RANGE);
					int yLoc2 = Math.Min(GameConstants.MapSize - 1, newNextLocY + GameConstants.EXPLORE_RANGE);
					int exploreWidth = MoveStepCoeff() - 1;

					if (newNextLocY < curNextLocY) // if move upwards, explore upper area
						World.Explore(xLoc1, yLoc1, xLoc2, yLoc1 + exploreWidth);

					else if (newNextLocY > curNextLocY) // if move downwards, explore lower area
						World.Explore(xLoc1, yLoc2 - exploreWidth, xLoc2, yLoc2);

					if (newNextLocX < curNextLocX) // if move towards left, explore left area
						World.Explore(xLoc1, yLoc1, xLoc1 + exploreWidth, yLoc2);

					else if (newNextLocX > curNextLocX) // if move towards right, explore right area
						World.Explore(xLoc2 - exploreWidth, yLoc1, xLoc2, yLoc2);
				}
			}
		}
	}

	private void SetWait()
	{
		CurAction = SPRITE_WAIT;
		CurFrame = 1;
		WaitingTerm++;
	}

	private void SetIdle()
	{
		CurAction = SPRITE_IDLE;
		FinalDir = CurDir;
		TurnDelay = 0;
	}

	public void SetReady()
	{
		CurAction = SPRITE_READY_TO_MOVE;
		FinalDir = CurDir;
		TurnDelay = 0;
	}

	private void SetMove()
	{
		CurAction = SPRITE_MOVE;
	}

	private void SetAttack()
	{
		CurAction = SPRITE_ATTACK;
		FinalDir = CurDir;
		TurnDelay = 0;
	}

	private void SetTurn()
	{
		CurAction = SPRITE_TURN;
	}

	private void SetShipExtraMove()
	{
		CurAction = SPRITE_SHIP_EXTRA_MOVE;
	}

	protected void SetDie()
	{
		if (ActionMode == UnitConstants.ACTION_DIE)
			return;

		ActionMode = UnitConstants.ACTION_DIE;
		CurAction = SPRITE_DIE;
		CurFrame = 1;

		//---- if this unit is led by a leader, only mobile units has LeaderId assigned to a leader -----//

		// the leader unit may have been killed at the same time
		if (LeaderId != 0 && !UnitArray.IsDeleted(LeaderId))
		{
			UnitArray[LeaderId].DeleteTeamMember(SpriteId);
			LeaderId = 0;
		}
	}

	public void SetMode(int modeId, int modeParameter = 0)
	{
		UnitMode = modeId;
		UnitModeParam = modeParameter;
	}
	

	public bool IsUnitDead()
	{
		return (HitPoints <= 0.0 || ActionMode == UnitConstants.ACTION_DIE || CurAction == SPRITE_DIE);
	}

	public virtual bool IsAIAllStop()
	{
		return ActionMode == UnitConstants.ACTION_STOP && ActionMode2 == UnitConstants.ACTION_STOP &&
		       CurAction == SPRITE_IDLE && AIActionId == 0;
	}
	
	public bool IsVisible()
	{
		return CurX >= 0;
	}

	private bool IsInSurrounding(int checkLocX, int checkLocY, int width, int targetLocX, int targetLocY, int targetWidth, int targetHeight)
	{
		switch (MoveStepCoeff())
		{
			case 1:
				if (checkLocX >= targetLocX - width && checkLocX <= targetLocX + targetWidth &&
				    checkLocY >= targetLocY - width && checkLocY <= targetLocY + targetHeight)
					return true;
				break;

			case 2:
				if (checkLocX >= targetLocX - width - 1 && checkLocX <= targetLocX + targetWidth + 1 &&
				    checkLocY >= targetLocY - width - 1 && checkLocY <= targetLocY + targetHeight + 1)
					return true;
				break;
		}

		return false;
	}
	
	public override bool IsStealth()
	{
		return Config.fog_of_war && World.GetLoc(NextLocX, NextLocY).Visibility() < UnitRes[UnitType].stealth;
	}

	public bool IsCivilian()
	{
		return RaceId > 0 && Skill.SkillId != Skill.SKILL_LEADING && UnitMode != UnitConstants.UNIT_MODE_REBEL;
	}

	public bool IsOwn()
	{
		return BelongsToNation(NationArray.player_recno);
	}

	public bool IsOwnSpy()
	{
		return SpyId != 0 && SpyArray[SpyId].TrueNationId == NationArray.player_recno;
	}

	public bool BelongsToNation(int nationId)
	{
		if (NationId == nationId)
			return true;

		if (SpyId != 0 && SpyArray[SpyId].TrueNationId == nationId)
			return true;

		return false;
	}

	public int TrueNationId()
	{
		return SpyId != 0 ? SpyArray[SpyId].TrueNationId : NationId;
	}
	
	public int RegionId()
	{
		if (IsVisible())
		{
			return World.GetRegionId(NextLocX, NextLocY);
		}
		else
		{
			if (UnitMode == UnitConstants.UNIT_MODE_OVERSEE || UnitMode == UnitConstants.UNIT_MODE_CONSTRUCT)
				return FirmArray[UnitModeParam].RegionId;
		}

		return 0;
	}

	public virtual bool ShouldShowInfo()
	{
		if (Config.show_ai_info || IsOwn())
			return true;
        
		if (NationArray.player_recno != 0 && NationArray.player.revealed_by_phoenix(NextLocX, NextLocY))
			return true;

		return false;
	}
	
	
	public virtual string GetUnitName(bool withTitle = true)
	{
		UnitInfo unitInfo = UnitRes[UnitType];

		string result = String.Empty;

		if (RaceId != 0)
		{
			if (withTitle)
			{
				if (UnitMode == UnitConstants.UNIT_MODE_REBEL)
				{
					if (Rank == RANK_GENERAL)
					{
						result = "Rebel Leader ";
					}
				}
				else
				{
					if (Rank == RANK_KING)
					{
						result = "King ";
					}
					else if (Rank == RANK_GENERAL)
					{
						result = "General ";
					}
				}
			}

			if (Rank == RANK_KING) // use the player name
				result += NationArray[NationId].king_name();
			else
				result += RaceRes[RaceId].get_name(NameId);
		}
		else
		{
			result = unitInfo.name;

			if (unitInfo.unit_class == UnitConstants.UNIT_CLASS_WEAPON && WeaponVersion > 1)
			{
				result += " " + Misc.roman_number(WeaponVersion);
			}

			if (unitInfo.unit_class != UnitConstants.UNIT_CLASS_GOD) // God doesn't have any series no.
			{
				result += " " + NameId; // name id is the series no. of the unit
			}
		}

		return result;
	}

	public void SetName(int newNameId)
	{
		//------- free up the existing name id. ------//

		RaceRes[RaceId].free_name_id(NameId);

		//------- set the new name id. ---------//

		NameId = newNameId;

		//-------- register usage of the new name id. ------//

		RaceRes[RaceId].use_name_id(NameId);
	}

	public int UnitPower()
	{
		int unitPower = (int)HitPoints;
		
		UnitInfo unitInfo = UnitRes[UnitType];
		if (unitInfo.unit_class == UnitConstants.UNIT_CLASS_WEAPON)
			unitPower += (unitInfo.weapon_power + WeaponVersion - 1) * 15;

		return unitPower;
	}

	public int CommanderPower()
	{
		//---- if the commander is in a military camp -----//

		int commanderPower = 0;

		if (UnitMode == UnitConstants.UNIT_MODE_OVERSEE)
		{
			Firm firm = FirmArray[UnitModeParam];

			if (firm.FirmType == Firm.FIRM_CAMP)
			{
				for (int i = firm.LinkedTowns.Count - 1; i >= 0; i--)
				{
					if (firm.LinkedTownsEnable[i] == InternalConstants.LINK_EE)
					{
						Town town = TownArray[firm.LinkedTowns[i]];

						commanderPower += town.Population / town.LinkedActiveCampCount();
					}
				}

				commanderPower += firm.Workers.Count * 3; // 0 to 24
			}
			else if (firm.FirmType == Firm.FIRM_BASE)
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

	private void DeleteTeamMember(int unitId)
	{
		for (int i = TeamInfo.Members.Count - 1; i >= 0; i--)
		{
			if (TeamInfo.Members[i] == unitId)
			{
				TeamInfo.Members.RemoveAt(i);
				return;
			}
		}

		//-------------------------------------------------------//
		//
		// Note: for rebels and monsters, although they have LeaderId, their TeamInfo is not used.
		//       So DeleteTeamMember() won't be able to match the unit in its TeamInfo.Members.
		//
		//-------------------------------------------------------//
	}

	public bool GetNextLoc(out int locX, out int locY)
	{
		locX = -1;
		locY = -1;

		if (IsVisible())
		{
			locX = NextLocX;
			locY = NextLocY;
		}
		else if (UnitMode == UnitConstants.UNIT_MODE_OVERSEE || UnitMode == UnitConstants.UNIT_MODE_CONSTRUCT || UnitMode == UnitConstants.UNIT_MODE_IN_HARBOR)
		{
			Firm firm = FirmArray[UnitModeParam];
			locX = firm.LocCenterX;
			locY = firm.LocCenterY;
		}
		else if (UnitMode == UnitConstants.UNIT_MODE_ON_SHIP)
		{
			Unit ship = UnitArray[UnitModeParam];
			if (ship.IsVisible())
			{
				locX = ship.NextLocX;
				locY = ship.NextLocY;
			}
			else
			{
				Firm firm = FirmArray[ship.UnitModeParam];
				locX = firm.LocCenterX;
				locY = firm.LocCenterY;
			}
		}
		else
		{
			return false;
		}

		return true;
	}

	private bool GetCurLoc(out int locX, out int locY)
	{
		locX = -1;
		locY = -1;
		
		if (IsVisible())
		{
			locX = CurLocX;
			locY = CurLocY;
		}
		else if (UnitMode == UnitConstants.UNIT_MODE_OVERSEE || UnitMode == UnitConstants.UNIT_MODE_CONSTRUCT || UnitMode == UnitConstants.UNIT_MODE_IN_HARBOR)
		{
			Firm firm = FirmArray[UnitModeParam];
			locX = firm.LocCenterX;
			locY = firm.LocCenterY;
		}
		else if (UnitMode == UnitConstants.UNIT_MODE_ON_SHIP)
		{
			Unit ship = UnitArray[UnitModeParam];
			if (ship.IsVisible())
			{
				locX = ship.CurLocX;
				locY = ship.CurLocY;
			}
			else
			{
				Firm firm = FirmArray[ship.UnitModeParam];
				locX = firm.LocCenterX;
				locY = firm.LocCenterY;
			}
		}
		else
		{
			return false;
		}

		return true;
	}

	private bool IsLeaderInRange()
	{
		if (LeaderId == 0)
			return false;

		if (UnitArray.IsDeleted(LeaderId))
		{
			LeaderId = 0;
			return false;
		}

		Unit leaderUnit = UnitArray[LeaderId];
		if (!leaderUnit.IsVisible() && leaderUnit.UnitMode == UnitConstants.UNIT_MODE_CONSTRUCT)
			return false;

		GetCurLoc(out int locX, out int locY);
		leaderUnit.GetCurLoc(out int leaderLocX, out int leaderLocY);

		if (leaderLocX >= 0 && Misc.points_distance(locX, locY, leaderLocX, leaderLocY) <= GameConstants.EFFECTIVE_LEADING_DISTANCE)
			return true;

		return false;
	}

	private void GainExperience()
	{
		if (UnitRes[UnitType].unit_class != UnitConstants.UNIT_CLASS_HUMAN)
			return;

		if (NationContribution < GameConstants.MAX_NATION_CONTRIBUTION)
			NationContribution++;

		IncMinorCombatLevel(6);

		//--- if this is a soldier led by a commander, increase the leadership of its commander -----//

		if (IsLeaderInRange())
		{
			Unit leaderUnit = UnitArray[LeaderId];
			leaderUnit.IncMinorSkillLevel(1);

			//-- give additional increase if the leader has skill potential on leadership --//

			if (leaderUnit.Skill.SkillPotential > 0)
			{
				if (Misc.Random(10 - leaderUnit.Skill.SkillPotential / 10) == 0)
					leaderUnit.IncMinorSkillLevel(5);
			}

			//--- if this soldier has leadership potential and is led by a commander ---//
			//--- he learns leadership by watching how the commander commands the troop --//

			if (Skill.SkillPotential > 0)
			{
				if (Misc.Random(10 - Skill.SkillPotential / 10) == 0)
					IncMinorSkillLevel(5);
			}
		}
	}

	private void IncMinorCombatLevel(int incLevel)
	{
		Skill.CombatLevelMinor += incLevel;

		if (Skill.CombatLevelMinor > 100)
		{
			if (Skill.CombatLevel < 100)
				SetCombatLevel(Skill.CombatLevel + 1);

			Skill.CombatLevelMinor -= 100;
		}
	}

	private void IncMinorSkillLevel(int incLevel)
	{
		Skill.SkillLevelMinor += incLevel;

		if (Skill.SkillLevelMinor > 100)
		{
			if (Skill.SkillLevel < 100)
				Skill.SkillLevel++;

			Skill.SkillLevelMinor -= 100;
		}
	}

	public void SetCombatLevel(int combatLevel)
	{
		Skill.CombatLevel = combatLevel;

		UnitInfo unitInfo = UnitRes[UnitType];

		int oldMaxHitPoints = MaxHitPoints;

		MaxHitPoints = unitInfo.hit_points * combatLevel / 100;

		if (oldMaxHitPoints != 0)
			HitPoints = HitPoints * MaxHitPoints / oldMaxHitPoints;

		HitPoints = Math.Min(HitPoints, MaxHitPoints);

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

		MaxPower = Skill.CombatLevel + 50;
		CurPower = Math.Min(CurPower, MaxPower);
	}
	
	private void PayExpense()
	{
		if (NationId == 0)
			return;

		//--- if it's a mobile spy or the spy is in its own firm, no need to pay salary here as Spy.PayExpense() will do that ---//
		//
		// -If your spies are mobile or in your own town or firm:
		//  >your nation pays them 1 food and $5 dollars per month
		//
		// -If your spies are in an enemy's town or firm:
		//  >the enemy pays them 1 food and the normal salary of their jobs.
		//  >your nation pays them $5 dollars per month. (your nation pays them no food)
		//
		//------------------------------------------------------//

		if (SpyId != 0)
		{
			if (IsVisible()) // the cost will be deducted in SpyArray
				return;

			if (UnitMode == UnitConstants.UNIT_MODE_OVERSEE && FirmArray[UnitModeParam].NationId == TrueNationId())
				return;
		}

		//---------- if it's a human unit -----------//
		//
		// The unit is paid even during its training period in a town
		//
		//-------------------------------------------//

		Nation nation = NationArray[NationId];

		if (UnitRes[UnitType].race_id != 0)
		{
			if (nation.cash > 0)
			{
				if (Rank == RANK_SOLDIER)
					nation.add_expense(NationBase.EXPENSE_MOBILE_UNIT, GameConstants.SOLDIER_YEAR_SALARY / 365.0, true);

				if (Rank == RANK_GENERAL)
					nation.add_expense(NationBase.EXPENSE_GENERAL, GameConstants.GENERAL_YEAR_SALARY / 365.0, true);
			}
			else // decrease loyalty if the nation cannot pay the unit
			{
				ChangeLoyalty(-1);
			}

			if (nation.food > 0)
			{
				nation.consume_food(GameConstants.PERSON_FOOD_YEAR_CONSUMPTION / 365.0);
			}
			else
			{
				// decrease 1 loyalty point every 5 days
				if (Info.TotalDays % GameConstants.NO_FOOD_LOYALTY_DECREASE_INTERVAL == 0)
					ChangeLoyalty(-1);
			}
		}
		else //----- it's a non-human unit ------//
		{
			if (nation.cash > 0)
			{
				int expenseType = UnitRes[UnitType].unit_class switch
				{
					UnitConstants.UNIT_CLASS_WEAPON => NationBase.EXPENSE_WEAPON,
					UnitConstants.UNIT_CLASS_SHIP => NationBase.EXPENSE_SHIP,
					UnitConstants.UNIT_CLASS_CARAVAN => NationBase.EXPENSE_CARAVAN,
					_ => NationBase.EXPENSE_MOBILE_UNIT
				};

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
									NewsArray.weapon_ship_worn_out(UnitType, WeaponVersion);

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

	private void ProcessRecover()
	{
		if (IsUnitDead() || HitPoints >= MaxHitPoints)
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
	
	public int LeaderInfluence()
	{
		Nation nation = NationArray[NationId];

		int thisInfluence = Skill.GetSkillLevel(Skill.SKILL_LEADING) * 2 / 3; // 66% of the leadership

		if (RaceRes.is_same_race(nation.race_id, RaceId))
			thisInfluence += thisInfluence / 3; // 33% bonus if the king's race is also the same as the general

		thisInfluence += (int)(nation.reputation / 2.0);

		thisInfluence = Math.Min(100, thisInfluence);

		return thisInfluence;
	}

	public virtual double ActualDamage()
	{
		AttackInfo attackInfo = AttackInfos[CurAttack];
		int attackDamage = attackInfo.attack_damage;

		//-------- pierce damage --------//

		attackDamage += Misc.Random(3) + attackInfo.pierce_damage
			* Misc.Random(Skill.CombatLevel - attackInfo.combat_level)
			/ (100 - attackInfo.combat_level);

		//--- if this unit is led by a general, its attacking ability is influenced by the general ---//
		//
		// The unit's attacking ability is increased by a percentage equivalent to the leader unit's leadership.
		//

		if (IsLeaderInRange())
		{
			Unit leaderUnit = UnitArray[LeaderId];
			attackDamage += attackDamage * leaderUnit.Skill.SkillLevel / 100;
		}

		// lessen all attacking damages, thus slowing down all battles.
		return (double)attackDamage / (double)InternalConstants.ATTACK_SLOW_DOWN;
	}

	private bool CanAttackNation(int targetNationId) // can this nation be attacked, no if alliance or etc..
	{
		if (!AIUnit)
			return targetNationId != NationId; // able to attack all nation except our own nation

		if (NationId == targetNationId)
			return false; // ai unit don't attack its own nation, except special order

		if (NationId == 0 || targetNationId == 0)
			return true; // true if either nation is independent

		Nation nation = NationArray[NationId];
		int relationStatus = nation.get_relation_status(targetNationId);
		return relationStatus != NationBase.NATION_FRIENDLY && relationStatus != NationBase.NATION_ALLIANCE;
	}

	private bool CanIndependentUnitAttackNation(int targetNationId)
	{
		switch (UnitMode)
		{
			case UnitConstants.UNIT_MODE_DEFEND_TOWN:
				if (TownArray.IsDeleted(UnitModeParam))
					return false; // don't attack independent unit with no town

				Town town = TownArray[UnitModeParam];
				return town.IsHostileNation(targetNationId);

			case UnitConstants.UNIT_MODE_REBEL:
				if (RebelArray.IsDeleted(UnitModeParam))
					return false;

				Rebel rebel = RebelArray[UnitModeParam];
				return rebel.IsHostileNation(targetNationId);

			case UnitConstants.UNIT_MODE_MONSTER:
				if (UnitModeParam == 0)
					return targetNationId != 0; // attack anything that is not independent

				FirmMonster firmMonster = (FirmMonster)FirmArray[UnitModeParam];
				return firmMonster.is_hostile_nation(targetNationId);

			default:
				return false;
		}
	}

	public bool Betray(int newNationId)
	{
		if (NationId == newNationId)
			return false;

		if (UnitMode == UnitConstants.UNIT_MODE_CONSTRUCT || UnitMode == UnitConstants.UNIT_MODE_ON_SHIP)
			return false;

		//---------- add news -----------//

		if (NationId == NationArray.player_recno || newNationId == NationArray.player_recno)
		{
			//--- if this is a spy, don't display news message for betrayal as it is already displayed in Unit.SpyChangeNation() ---//

			if (SpyId == 0)
				NewsArray.unit_betray(SpriteId, newNationId);
		}

		//------ change nation now ------//

		ChangeNation(newNationId);

		//-------- set the loyalty of the unit -------//

		if (NationId != 0)
		{
			Nation nation = NationArray[NationId];

			Loyalty = GameConstants.UNIT_BETRAY_LOYALTY + Misc.Random(5);

			if (nation.reputation > 0)
				ChangeLoyalty((int)nation.reputation);

			if (RaceRes.is_same_race(nation.race_id, RaceId))
				ChangeLoyalty(30);

			UpdateLoyalty(); // update target loyalty
		}
		else //------ if change to independent rebel -------//
		{
			Loyalty = 0; // no loyalty needed
		}

		//--- if this unit is a general, change nation for the units he commands ---//

		int newTeamId = UnitArray.CurTeamId++;

		if (Rank == RANK_GENERAL)
		{
			foreach (Unit unit in UnitArray)
			{
				//---- capture the troop this general commands -----//

				if (unit.LeaderId == SpriteId && unit.Rank == RANK_SOLDIER && unit.IsVisible())
				{
					if (unit.SpyId != 0) // if the unit is a spy
					{
						// 1-group defection of this unit, allowing us to handle the change of nation
						unit.SpyChangeNation(newNationId, InternalConstants.COMMAND_AUTO, 1);
					}

					unit.ChangeNation(newNationId);
					unit.TeamId = newTeamId; // assign new team_id or checking for nation_recno
				}
			}
		}

		TeamId = newTeamId;

		//------ go to meet the new master -------//

		if (IsVisible() && NationId != 0)
		{
			if (SpyId == 0 || SpyArray[SpyId].NotifyCloakedNation)
			{
				// generals shouldn't automatically be assigned to camps, they should just move near your villages
				if (Rank == RANK_GENERAL)
					AIMoveToNearbyTown();
				else
					ThinkNormalHumanAction();
			}
		}

		return true;
	}
	
	private bool IsCaravanInsideFirm()
	{
		return CurX == -2;
	}

	public void UpdateLoyalty()
	{
		if (NationId == 0 || Rank == RANK_KING || UnitRes[UnitType].race_id == 0)
			return;

		// constructor worker will not change their loyalty when they are in a building
		if (UnitMode == UnitConstants.UNIT_MODE_CONSTRUCT)
			return;

		if (Rank == RANK_GENERAL)
		{
			//----- the general's power affect his loyalty ----//

			int targetLoyalty = CommanderPower();

			//----- the king's race affects the general's loyalty ----//

			Nation ownNation = NationArray[NationId];
			if (ownNation.race_id == RaceId)
				targetLoyalty += 20;

			//----- the kingdom's reputation affects the general's loyalty ----//

			targetLoyalty += (int)(ownNation.reputation / 4.0);

			//--- the king's leadership also affect the general's loyalty -----//

			if (ownNation.king_unit_recno != 0)
				targetLoyalty += UnitArray[ownNation.king_unit_recno].Skill.SkillLevel / 4;

			//-- if the unit is rewarded less than the amount of contribution he made, he will become unhappy --//

			if (NationContribution > TotalReward * 2)
			{
				int decLoyalty = (NationContribution - TotalReward * 2) / 2;
				targetLoyalty -= Math.Min(50, decLoyalty); // this affect 50 points at maximum
			}

			targetLoyalty = Math.Min(targetLoyalty, 100);
			TargetLoyalty = Math.Max(targetLoyalty, 0);
		}

		if (Rank == RANK_SOLDIER)
		{
			bool leaderBonus = ConfigAdv.unit_loyalty_require_local_leader ? IsLeaderInRange() : LeaderId != 0;
			if (leaderBonus)
			{
				//----------------------------------------//
				//
				// If this soldier is led by a general, the target loyalty
				// = race friendliness between the unit and the general / 2 + the general leadership / 2
				//
				//----------------------------------------//

				Unit leaderUnit = UnitArray[LeaderId];

				int targetLoyalty = 30 + leaderUnit.Skill.GetSkillLevel(Skill.SKILL_LEADING);

				//---------------------------------------------------//
				//
				// Soldiers with higher combat and leadership skill
				// will get discontented if they are led by a general with low leadership.
				//
				//---------------------------------------------------//

				targetLoyalty -= Skill.CombatLevel / 2;
				targetLoyalty -= Skill.SkillLevel;

				if (leaderUnit.Rank == RANK_KING)
					targetLoyalty += 20;

				if (RaceRes.is_same_race(RaceId, leaderUnit.RaceId))
					targetLoyalty += 20;

				targetLoyalty = Math.Min(targetLoyalty, 100);
				TargetLoyalty = Math.Max(targetLoyalty, 0);
			}
			else
			{
				TargetLoyalty = 0;
			}
		}

		//--------- update loyalty ---------//

		if (TargetLoyalty > Loyalty)
		{
			int incValue = (TargetLoyalty - Loyalty) / 10;

			int newLoyalty = Loyalty + Math.Max(1, incValue);

			if (newLoyalty > TargetLoyalty)
				newLoyalty = TargetLoyalty;

			Loyalty = newLoyalty;
		}
		else if (TargetLoyalty < Loyalty)
		{
			Loyalty--;
		}
	}

	public void ChangeLoyalty(int loyaltyChange)
	{
		int newLoyalty = Loyalty + loyaltyChange;

		newLoyalty = Math.Max(0, newLoyalty);

		Loyalty = Math.Min(100, newLoyalty);
	}
	
	public void SetRank(int rankId)
	{
		if (Rank == rankId)
			return;

		//------- promote --------//

		if (rankId > Rank)
			ChangeLoyalty(GameConstants.PROMOTE_LOYALTY_INCREASE);

		//------- demote -----------//

		// no decrease in loyalty if a spy king hands his nation to his parent nation and become a general again
		else if (rankId < Rank && Rank != RANK_KING)
			ChangeLoyalty(-GameConstants.DEMOTE_LOYALTY_DECREASE);

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

		if (Rank == RANK_GENERAL && rankId == RANK_SOLDIER)
		{
			//----- reset LeaderId of the units he commands ----//

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

			TeamInfo.Members.Clear();
			TeamId = 0;
		}

		//----- if this is a soldier being promoted to a general -----//

		if (Rank == RANK_SOLDIER && rankId == RANK_GENERAL)
		{
			//-- if this soldier is formerly commanded by a general, detach it ---//

			if (LeaderId != 0)
			{
				if (!UnitArray.IsDeleted(LeaderId)) // the leader unit may have been killed at the same time
					UnitArray[LeaderId].DeleteTeamMember(SpriteId);

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

		if (Rank == RANK_GENERAL || Rank == RANK_KING)
		{
			//--- set leadership if this unit does not have any now ----//

			if (Skill.SkillId != Skill.SKILL_LEADING)
			{
				Skill.SkillId = Skill.SKILL_LEADING;
				// TODO add random value?
				Skill.SkillLevel = 10 + Misc.Random(40);
			}
		}
	}

	public virtual bool CanResign()
	{
		return Rank != RANK_KING;
	}

	public bool CanBuild(int firmType)
	{
		if (NationId == 0)
			return false;

		FirmInfo firmInfo = FirmRes[firmType];
		if (!firmInfo.buildable)
			return false;

		if (firmInfo.get_nation_tech_level(NationId) == 0)
			return false;

		if (firmType == Firm.FIRM_BASE) // only if the nation has acquired the myth to build it
		{
			if (Rank == RANK_GENERAL || Rank == RANK_KING || Skill.SkillId == Skill.SKILL_CONSTRUCTION)
			{
				//----- each nation can only build one seat of power -----//

				if (RaceId > 0 && NationArray[NationId].base_count_array[RaceId - 1] == 0)
				{
					//--- if this nation has acquired the needed scroll of power ---//

					return NationArray[NationId].know_base_array[RaceId - 1] != 0;
				}
			}

			return false;
		}

		if (Rank == RANK_KING)
		{
			if (firmType == Firm.FIRM_CAMP || firmType == Firm.FIRM_INN)
				return true;
		}

		//------ unit with construction skill knows how to build all buildings -----//

		if (Skill.SkillId == Skill.SKILL_CONSTRUCTION && firmInfo.firm_race_id == 0)
			return true;

		//----- if the firm is race specific, if the unit is right race, return true ----//

		if (firmInfo.firm_race_id == RaceId)
			return true;

		//---- if the unit has the skill needed by the firm or the unit has general construction skill ----//

		if (firmInfo.firm_skill_id != 0 && firmInfo.firm_skill_id == Skill.SkillId)
			return true;

		return false;
	}

	public void Resign(int remoteAction)
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

		if (IsVisible())
			Stop2();

		//--- if the spy is resigned by an enemy, display a message ---//

		// the spy is cloaked in an enemy nation when it is resigned
		if (SpyId != 0 && TrueNationId() != NationId)
		{
			//------ decrease reputation ------//

			NationArray[TrueNationId()].change_reputation(-GameConstants.SPY_KILLED_REPUTATION_DECREASE);

			//------- add news message -------//

			// display when the player's spy is revealed or the player has revealed an enemy spy
			if (TrueNationId() == NationArray.player_recno || NationId == NationArray.player_recno)
			{
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

	public void Reward(int rewardNationId)
	{
		if (NationArray[rewardNationId].cash < GameConstants.REWARD_COST)
			return;

		//--------- if this is a spy ---------//

		if (SpyId != 0 && TrueNationId() == rewardNationId) // if the spy's own nation rewards the spy
		{
			SpyArray[SpyId].ChangeLoyalty(GameConstants.REWARD_LOYALTY_INCREASE);
		}

		//--- if this spy's NationId & TrueNationId() are both == rewardNationId,
		//it's true loyalty and cloaked loyalty will both be increased ---//

		if (NationId == rewardNationId)
		{
			TotalReward += GameConstants.REWARD_COST;

			ChangeLoyalty(GameConstants.REWARD_LOYALTY_INCREASE);
		}

		NationArray[rewardNationId].add_expense(NationBase.EXPENSE_REWARD_UNIT, GameConstants.REWARD_COST);
	}

	public void ChangeNation(int newNationId)
	{
		bool oldAiUnit = AIUnit;
		int oldNationId = NationId;

		if (WayPoints.Count > 0)
			ResetWayPoints();

		//-- if the player is giving a command to this unit, cancel the command --//

		//TODO rewrite
		/*if (NationId == NationArray.player_recno && SpriteId == UnitArray.SelectedUnitId && Power.command_id != 0)
		{
			Power.command_id = 0;
		}*/

		//---------- stop all action to attack this unit ------------//

		UnitArray.StopAttackUnit(SpriteId);

		//---- update nation_unit_count_array[] ----//

		UnitRes[UnitType].unit_change_nation(newNationId, NationId, Rank);

		//------- if the nation has an AI action -------//

		Stop2(); // clear the existing order

		//---------------- update vars ----------------//

		GroupId = UnitArray.CurGroupId++; // separate from the current group
		NationId = newNationId;

		HomeCampId = 0;
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
			Nation nation = NationArray[oldNationId];

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
			FirmArray[UnitModeParam].ChangeNation(newNationId);

		//----- this unit was defending the town before it gets killed ----//

		else if (UnitMode == UnitConstants.UNIT_MODE_DEFEND_TOWN)
		{
			if (!TownArray.IsDeleted(UnitModeParam))
				TownArray[UnitModeParam].ReduceDefenderCount();

			SetMode(0); // reset unit mode
		}

		//---- if the unit is no longer the same nation as the leader ----//

		if (LeaderId != 0)
		{
			Unit leaderUnit = UnitArray[LeaderId];

			if (leaderUnit.NationId != NationId)
			{
				leaderUnit.DeleteTeamMember(SpriteId);
				LeaderId = 0;
				TeamId = 0;
			}
		}
	}

	public void SpyChangeNation(int newNationId, int remoteAction, int groupDefect = 0)
	{
		// TODO what is groupDefect?
		
		if (newNationId == NationId)
			return;

		if (newNationId != 0 && NationArray.IsDeleted(newNationId)) // this can happen in a multiplayer message
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

		Spy spy = SpyArray[SpyId];

		//--- when a spy change cloak to another nation, he can't cloak as a general, he must become a soldier first ---//

		// if the spy is a commander in a camp, don't set its rank to soldier
		if (IsVisible() && Rank == RANK_GENERAL && newNationId != spy.TrueNationId)
		{
			SetRank(RANK_SOLDIER);
		}

		//---------------------------------------------------//
		//
		// If this spy unit is a general or an overseer of the cloaked nation,
		// when he changes nation, that will inevitably be noticed by the cloaked nation.
		//
		//---------------------------------------------------//

		// only send news message if he is not the player's own spy
		if (spy.TrueNationId != NationArray.player_recno)
		{
			if (Rank == RANK_GENERAL || UnitMode == UnitConstants.UNIT_MODE_OVERSEE ||
			    (spy.NotifyCloakedNation && groupDefect == 0))
			{
				//-- if this spy's cloaked nation is the player's nation, the player will be notified --//

				if (NationId == NationArray.player_recno)
					NewsArray.unit_betray(SpriteId, newNationId);
			}

			//---- send news to the cloaked nation if notify flag is on ---//

			if (spy.NotifyCloakedNation && groupDefect == 0)
			{
				if (newNationId == NationArray.player_recno) // cloaked as the player's nation
					NewsArray.unit_betray(SpriteId, newNationId);
			}
		}

		//--------- change nation now --------//

		spy.CloakedNationId = newNationId;

		// call the betray function to change nation. There is no difference between a spy changing nation and a unit truly betrays
		if (groupDefect == 0)
			Betray(newNationId);
	}

	public bool CanSpyChangeNation()
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

		int trueNationId = TrueNationId();

		for (int yLoc = yLoc1; yLoc <= yLoc2; yLoc++)
		{
			for (int xLoc = xLoc1; xLoc <= xLoc2; xLoc++)
			{
				Location loc = World.GetLoc(xLoc, yLoc);

				int unitId = 0;

				if (loc.HasUnit(UnitConstants.UNIT_LAND))
					unitId = loc.UnitId(UnitConstants.UNIT_LAND);

				else if (loc.HasUnit(UnitConstants.UNIT_SEA))
					unitId = loc.UnitId(UnitConstants.UNIT_SEA);

				else if (loc.HasUnit(UnitConstants.UNIT_AIR))
					unitId = loc.UnitId(UnitConstants.UNIT_AIR);

				if (unitId == 0 || UnitArray.IsDeleted(unitId)) // the unit is dying, its id is still in the location
					continue;

				Unit otherUnit = UnitArray[unitId];
				if (otherUnit.TrueNationId() != trueNationId)
				{
					if (otherUnit.SpyId != 0 && SpyId != 0)
					{
						if (SpyArray[otherUnit.SpyId].SpySkill >= SpyArray[SpyId].SpySkill)
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

	public void ChangeHitPoints(double changePoints)
	{
		HitPoints += changePoints;

		if (HitPoints < 0.0)
			HitPoints = 0.0;

		if (HitPoints > MaxHitPoints)
			HitPoints = MaxHitPoints;
	}

	private int CalcDistance(int targetLocX, int targetLocY, int targetWidth, int targetHeight)
	{
		int curLocX = NextLocX;
		int curLocY = NextLocY;
		int distX, distY;

		if (curLocX < targetLocX)
			distX = (targetLocX - curLocX - SpriteInfo.LocWidth) + 1;
		else if ((distX = curLocX - targetLocX - targetWidth + 1) < 0)
			distX = 0;

		if (curLocY < targetLocY)
			distY = (targetLocY - curLocY - SpriteInfo.LocHeight) + 1;
		else if ((distY = curLocY - targetLocY - targetHeight + 1) < 0)
			return distX;

		return (distX >= distY) ? distX : distY;
	}

	private void KingDie()
	{
		NewsArray.king_die(NationId);

		Nation nation = NationArray[NationId];
		nation.king_unit_recno = 0;
	}

	private void GeneralDie()
	{
		if (NationId == NationArray.player_recno)
			NewsArray.general_die(SpriteId);
	}

	public abstract void DrawDetails(IRenderer renderer);

	public abstract void HandleDetailsInput(IRenderer renderer);

	#region Functions for unit AI mode

	public bool ThinkBetray()
	{
		if (SpyId != 0) // spies do not betray here, spy has its own functions for betrayal
			return false;

		//----- if the unit is in training or is constructing a building, do not betray -------//

		if (!IsVisible() && UnitMode != UnitConstants.UNIT_MODE_OVERSEE)
			return false;

		if (Loyalty >= GameConstants.UNIT_BETRAY_LOYALTY)
			return false;

		if (UnitRes[UnitType].race_id == 0 || NationId == 0 || Rank == RANK_KING)
			return false;

		//------ turn towards other nation --------//

		int bestNationId = 0, bestScore = Loyalty; // the score must be larger than the current loyalty
		int unitRegionId = RegionId();

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

			int nationScore = (int)nation.reputation + (nation.overall_rating - curNation.overall_rating);

			if (RaceRes.is_same_race(nation.race_id, RaceId))
				nationScore += 30;

			if (nationScore > bestScore)
			{
				bestScore = nationScore;
				bestNationId = nation.nation_recno;
			}
		}

		if (bestNationId != 0)
		{
			return Betray(bestNationId);
		}
		else if (Loyalty == 0)
		{
			//----------------------------------------------//
			// If there is no good nation to turn towards to and
			// the loyalty has dropped to 0, resign itself and leave the nation.
			//
			// However, if the unit is spy, it will stay with the
			// nation as it has never been really loyal to the nation.
			//---------------------------------------------//

			if (Rank != RANK_KING && IsVisible() && SpyId == 0)
			{
				Resign(InternalConstants.COMMAND_AUTO);
				return true;
			}
		}

		return false;
	}

	private bool ThinkAggressiveAction()
	{
		//------ think about resuming the original action -----//

		// if it is currently attacking somebody, don't think about resuming the original action
		if (OriginalActionMode != 0 && CurAction != SPRITE_ATTACK)
		{
			return ThinkResumeOriginalAction();
		}

		//---- think about attacking nearby units if this unit is attacking a town or a firm ---//

		// only in attack mode, as if the unit is still moving the target may be far away from the current position
		if (AggressiveMode && UnitMode == 0 && CurAction == SPRITE_ATTACK)
		{
			//--- only when the unit is currently attacking a firm or a town ---//

			if (ActionMode2 == UnitConstants.ACTION_ATTACK_FIRM || ActionMode2 == UnitConstants.ACTION_ATTACK_TOWN)
			{
				if (Info.TotalDays % 5 == 0) // check once every 5 days
					return ThinkChangeAttackTarget();
			}
		}

		return false;
	}

	private bool ThinkResumeOriginalAction()
	{
		if (!IsVisible()) // if the unit is no longer visible, cancel the saved original action
		{
			OriginalActionMode = 0;
			return false;
		}

		//---- if the unit is in defense mode now, don't do anything ----//

		if (InAnyDefenseMode())
			return false;

		//----------------------------------------------------//
		//
		// If the action has been changed or the target unit has been deleted,
		// stop the chase right now and move back to the original position before the auto guard attack.
		//
		//----------------------------------------------------//

		if (ActionMode2 != UnitConstants.ACTION_ATTACK_UNIT || UnitArray.IsDeleted(ActionPara2))
		{
			ResumeOriginalAction();
			return true;
		}

		//-----------------------------------------------------//
		//
		// Stop the chase if the target is being far away from
		// its original location and move back to its original position before the auto guard attack.
		//
		//-----------------------------------------------------//

		Unit targetUnit = UnitArray[ActionPara2];

		int curDistance = Misc.points_distance(targetUnit.NextLocX, targetUnit.NextLocY, OriginalTargetLocX, OriginalTargetLocY);

		if (curDistance > UnitConstants.AUTO_GUARD_CHASE_ATTACK_DISTANCE)
		{
			ResumeOriginalAction();
			return true;
		}

		return false;
	}

	private void SaveOriginalAction()
	{
		if (OriginalActionMode == 0)
		{
			OriginalActionMode = ActionMode2;
			OriginalActionParam = ActionPara2;
			OriginalActionLocX = ActionLocX2;
			OriginalActionLocY = ActionLocY2;
		}
	}

	private void ResumeOriginalAction()
	{
		if (OriginalActionMode == 0)
			return;

		//--------- If it is an attack action ---------//

		if (OriginalActionMode == UnitConstants.ACTION_ATTACK_UNIT ||
		    OriginalActionMode == UnitConstants.ACTION_ATTACK_FIRM ||
		    OriginalActionMode == UnitConstants.ACTION_ATTACK_TOWN)
		{
			ResumeOriginalAttackAction();
			return;
		}

		//--------------------------------------------//

		if (!Misc.IsLocationValid(OriginalActionLocX, OriginalActionLocY))
		{
			OriginalActionMode = 0;
			return;
		}

		// use UnitArray.Attack() instead of unit.Attack_???() as we are unsure about what type of object the target is.
		List<int> selectedArray = new List<int>();
		selectedArray.Add(SpriteId);

		Location location = World.GetLoc(OriginalActionLocX, OriginalActionLocY);

		//--------- resume assign to town -----------//

		if (OriginalActionMode == UnitConstants.ACTION_ASSIGN_TO_TOWN && location.IsTown())
		{
			if (location.TownId() == OriginalActionParam && TownArray[OriginalActionParam].NationId == NationId)
			{
				UnitArray.Assign(OriginalActionLocX, OriginalActionLocY, false, selectedArray, InternalConstants.COMMAND_AUTO);
			}
		}

		//--------- resume assign to firm ----------//

		else if (OriginalActionMode == UnitConstants.ACTION_ASSIGN_TO_FIRM && location.IsFirm())
		{
			if (location.FirmId() == OriginalActionParam && FirmArray[OriginalActionParam].NationId == NationId)
			{
				UnitArray.Assign(OriginalActionLocX, OriginalActionLocY, false, selectedArray, InternalConstants.COMMAND_AUTO);
			}
		}

		//--------- resume build firm ---------//

		else if (OriginalActionMode == UnitConstants.ACTION_BUILD_FIRM)
		{
			if (World.CanBuildFirm(OriginalActionLocX, OriginalActionLocY, OriginalActionParam, SpriteId) != 0)
			{
				BuildFirm(OriginalActionLocX, OriginalActionLocY, OriginalActionParam, InternalConstants.COMMAND_AUTO);
			}
		}

		//--------- resume settle ---------//

		else if (OriginalActionMode == UnitConstants.ACTION_SETTLE)
		{
			if (World.CanBuildTown(OriginalActionLocX, OriginalActionLocY, SpriteId))
			{
				UnitArray.Settle(OriginalActionLocX, OriginalActionLocY, false, selectedArray, InternalConstants.COMMAND_AUTO);
			}
		}

		//--------- resume move ----------//

		else if (OriginalActionMode == UnitConstants.ACTION_MOVE)
		{
			UnitArray.MoveTo(OriginalActionLocX, OriginalActionLocY, false, selectedArray, InternalConstants.COMMAND_AUTO);
		}

		OriginalActionMode = 0;
	}

	private void ResumeOriginalAttackAction()
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

		Location location = World.GetLoc(OriginalActionLocX, OriginalActionLocY);
		int targetNationId = -1;

		if (OriginalActionMode == UnitConstants.ACTION_ATTACK_UNIT && location.HasUnit(UnitConstants.UNIT_LAND))
		{
			int unitId = location.UnitId(UnitConstants.UNIT_LAND);
			if (unitId == OriginalActionParam)
				targetNationId = UnitArray[unitId].NationId;
		}
		else if (OriginalActionMode == UnitConstants.ACTION_ATTACK_FIRM && location.IsFirm())
		{
			int firmId = location.FirmId();
			if (firmId == OriginalActionParam)
				targetNationId = FirmArray[firmId].NationId;
		}
		else if (OriginalActionMode == UnitConstants.ACTION_ATTACK_TOWN && location.IsTown())
		{
			int townId = location.TownId();
			if (townId == OriginalActionParam)
				targetNationId = TownArray[townId].NationId;
		}

		//----- the original target is no longer valid ----//

		if (targetNationId == -1)
		{
			OriginalActionMode = 0;
			return;
		}

		//---- only resume attacking the target if the target nation is at war with us currently ---//

		if (targetNationId == 0 ||
		    (targetNationId != NationId && NationArray[NationId].get_relation_status(targetNationId) == NationBase.NATION_HOSTILE))
		{
			// use UnitArray.Attack() instead of unit.Attack_???() as we are unsure about what type of object the target is.
			List<int> selectedArray = new List<int>();
			selectedArray.Add(SpriteId);

			UnitArray.Attack(OriginalActionLocX, OriginalActionLocY, false, selectedArray, InternalConstants.COMMAND_AI, 0);
		}

		OriginalActionMode = 0;
	}

	private bool ThinkChangeAttackTarget()
	{
		int attackRange = Math.Max(AttackRange, 8);
		int attackScanRange = attackRange * 2 + 1;

		int curLocX = NextLocX, curLocY = NextLocY;
		int regionId = World.GetRegionId(curLocX, curLocY);

		for (int i = 2; i < attackScanRange * attackScanRange; i++)
		{
			Misc.cal_move_around_a_point(i, attackScanRange, attackScanRange, out int xOffset, out int yOffset);

			int locX = curLocX + xOffset;
			int locY = curLocY + yOffset;

			locX = Math.Max(0, locX);
			locX = Math.Min(GameConstants.MapSize - 1, locX);

			locY = Math.Max(0, locY);
			locY = Math.Min(GameConstants.MapSize - 1, locY);

			Location location = World.GetLoc(locX, locY);

			if (location.RegionId != regionId)
				continue;

			//----- if there is a unit on the location ------//

			if (location.HasUnit(UnitConstants.UNIT_LAND))
			{
				int unitId = location.UnitId(UnitConstants.UNIT_LAND);

				if (UnitArray.IsDeleted(unitId))
					continue;

				if (UnitArray[unitId].NationId != NationId && IdleDetectUnitChecking(unitId))
				{
					SaveOriginalAction();

					OriginalTargetLocX = locX;
					OriginalTargetLocY = locY;

					AttackUnit(locX, locY, 0, 0, true);
					return true;
				}
			}
		}

		return false;
	}

	public bool ReturnCamp()
	{
		if (HomeCampId == 0)
			return false;

		if (IsUnitDead() || !IsVisible())
			return false;

		Firm firm = FirmArray[HomeCampId];

		if (firm.RegionId != RegionId())
			return false;

		Assign(firm.LocX1, firm.LocY1);

		ForceMove = true;

		return CurAction != SPRITE_IDLE;
	}

	public void ThinkIndependentUnit()
	{
		if (!IsAIAllStop())
			return;

		//--- don't process if it's a spy and the notify cloak flag is on ---//

		if (SpyId != 0)
		{
			//---------------------------------------------//
			//
			// If notify_cloaked_nation_flag is 0, the AI won't control the unit.
			//
			// If notify_cloaked_nation_flag is 1, the AI will control the unit. But not immediately,
			// it will do it once 5 days so the player can have a chance to select the unit and set its
			// notify_cloaked_nation_flag back to 0 if the player wants.
			//
			//---------------------------------------------//

			if (!SpyArray[SpyId].NotifyCloakedNation)
				return;

			if (Info.TotalDays % 5 != SpriteId % 5)
				return;
		}

		//-------- if this is a rebel ----------//

		if (UnitMode == UnitConstants.UNIT_MODE_REBEL)
		{
			Rebel rebel = RebelArray[UnitModeParam];

			//--- if the group this rebel belongs to already has a rebel town, assign to it now ---//

			if (rebel.TownId != 0)
			{
				if (!TownArray.IsDeleted(rebel.TownId))
				{
					Town town = TownArray[rebel.TownId];
					Assign(town.LocX1, town.LocY1);
				}

				return; // don't do anything if the town has been destroyed, Rebel.NextDay() will take care of it. 
			}
		}

		//---- look for towns to assign to -----//

		Town bestTown = null;
		int regionId = World.GetRegionId(NextLocX, NextLocY);
		int bestRating = 0;
		int curLocX = NextLocX, curLocY = NextLocY;

		foreach (Town town in TownArray)
		{
			if (town.NationId != 0 || town.Population >= GameConstants.MAX_TOWN_POPULATION || town.RegionId != regionId)
				continue;

			int curRating = World.DistanceRating(curLocX, curLocY, town.LocCenterX, town.LocCenterY);
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

			// TODO UNIT_MODE_REBEL already processed before
			if (UnitMode == UnitConstants.UNIT_MODE_REBEL)
				RebelArray.DropRebelIdentity(SpriteId);

			Assign(bestTown.LocX1, bestTown.LocY1);
		}
		else
		{
			Resign(InternalConstants.COMMAND_AI);
		}
	}
	
	public void AIMoveToNearbyTown()
	{
		//---- look for towns to assign to -----//

		int regionId = World.GetRegionId(NextLocX, NextLocY);
		int bestRating = 0;
		Town bestTown = null;
		int curLocX = NextLocX, curLocY = NextLocY;

		// can't use ai_town_array[] because this function will be called by Unit.Betray() when a unit defected to the player's kingdom
		foreach (Town town in TownArray)
		{
			if (town.NationId != NationId)
				continue;

			if (town.RegionId != regionId)
				continue;

			int curDistance = Misc.points_distance(curLocX, curLocY, town.LocCenterX, town.LocCenterY);

			if (curDistance < 10) // no need to move if the unit is already close enough to the town.
				return;

			int curRating = 100 - 100 * curDistance / GameConstants.MapSize;

			curRating += town.Population;

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestTown = town;
			}
		}

		if (bestTown != null)
			MoveToTownSurround(bestTown.LocX1, bestTown.LocY1, SpriteInfo.LocWidth, SpriteInfo.LocHeight);
	}
	
	private void AILeaderBeingAttacked(int attackerUnitId)
	{
		// this can happen when the unit has just changed nation
		if (UnitArray[attackerUnitId].NationId == NationId)
			return;

		int callIntervalDays = 0;
		bool defend = false;

		if (Rank == RANK_KING)
		{
			defend = true;
			callIntervalDays = 7;
		}
		else if (Rank == RANK_GENERAL)
		{
			defend = (Skill.SkillLevel >= 30 + (100 - NationArray[NationId].pref_keep_general) / 2); // 30 to 80
			callIntervalDays = 15; // don't call too frequently
		}

		if (defend)
		{
			if (Info.game_date > TeamInfo.AILastRequestDefenseDate.AddDays(callIntervalDays))
			{
				TeamInfo.AILastRequestDefenseDate = Info.game_date;
				NationArray[NationId].ai_defend(attackerUnitId);
			}
		}
	}

	#endregion
	

	#region Old AI Functions
	
	public int AIActionId { get; set; } // an unique id. for locating the AI action node this unit belongs to in Nation::action_array
	private int AIOriginalTargetLocX { get; set; } // for AI only
	private int AIOriginalTargetLocY { get; set; }
	private bool AINoSuitableAction { get; set; }
	
	public virtual void ProcessAI()
	{
		//------ the aggressive_mode of AI units is always 1 ------//

		AggressiveMode = true;

		//--- if it's a spy from other nation, don't control it ---//

		if (SpyId != 0 && TrueNationId() != NationId && !SpyArray[SpyId].NotifyCloakedNation && Info.TotalDays % 60 != SpriteId % 60)
		{
			return;
		}

		//----- think about rewarding this unit -----//

		if (RaceId != 0 && Rank != RANK_KING && Info.TotalDays % 5 == SpriteId % 5)
			ThinkReward();

		if (!IsVisible())
			return;

		//--- if the unit has stopped, but AIActionId hasn't been reset ---//

		if (CurAction == SPRITE_IDLE && ActionMode == UnitConstants.ACTION_STOP && ActionMode2 == UnitConstants.ACTION_STOP &&
		    AIActionId != 0 && NationId != 0)
		{
			NationArray[NationId].action_failure(AIActionId, SpriteId);
		}

		//---- King flees under attack or surrounded by enemy ---//

		if (RaceId != 0 && Rank == RANK_KING)
		{
			if (ThinkKingFlee())
				return;
		}

		//---- General flees under attack or surrounded by enemy ---//

		if (RaceId != 0 && Rank == RANK_GENERAL && Info.TotalDays % 7 == SpriteId % 7)
		{
			if (ThinkGeneralFlee())
				return;
		}

		//-- let Unit.NextDay() process it OriginalActionMode --//

		if (OriginalActionMode != 0)
			return;

		//------ if the unit is not stop right now ------//

		if (!IsAIAllStop())
		{
			ThinkStopChase();
			return;
		}

		//-----------------------------------------//

		if (MobileType == UnitConstants.UNIT_LAND)
		{
			if (AIEscapeFire())
				return;
		}

		//---------- if this is your spy --------//

		if (SpyId != 0 && TrueNationId() == NationId)
			ThinkSpyAction();

		//------ if this unit is from a camp --------//

		if (HomeCampId != 0)
		{
			Firm firmCamp = FirmArray[HomeCampId];
			bool rc;

			if (Rank == RANK_SOLDIER)
				rc = firmCamp.Workers.Count < Firm.MAX_WORKER;
			else
				rc = (firmCamp.OverseerId == 0);

			if (rc)
			{
				if (ReturnCamp())
					return;
			}

			HomeCampId = 0; // the camp is already occupied by somebody
		}

		//----------------------------------------//

		if (RaceId != 0 && Rank == RANK_KING)
		{
			ThinkKingAction();
		}
		else if (RaceId != 0 && Rank == RANK_GENERAL)
		{
			ThinkGeneralAction();
		}
		else
		{
			if (UnitRes[UnitType].unit_class == UnitConstants.UNIT_CLASS_WEAPON)
			{
				// don't call too often as the action may fail and it takes a while to call the function each time
				if (Info.TotalDays % 15 == SpriteId % 15)
				{
					ThinkWeaponAction(); //-- ships AI are called in UnitMarine --//
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

				if (!ThinkNormalHumanAction())
				{
					// set this flag so think_normal_human_action() won't be called continuously
					AINoSuitableAction = true;
					AIMoveToNearbyTown();
				}
			}
		}
	}

	private bool ThinkKingAction()
	{
		return ThinkLeaderAction();
	}

	private bool ThinkGeneralAction()
	{
		if (ThinkLeaderAction())
			return true;

		//--- if the general is not assigned to a camp due to its low competency ----//

		Nation ownNation = NationArray[NationId];
		bool rc = false;

		if (TeamInfo.Members.Count <= 1)
		{
			rc = true;
		}

		//--- if the skill of the general and the number of soldiers he commands is not large enough to justify building a new camp ---//

		else if (Skill.SkillLevel /* + team_info.member_count*4*/ < 40 + ownNation.pref_keep_general / 5) // 40 to 60
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
				Reward(NationId);

			SetRank(RANK_SOLDIER);
			return ThinkNormalHumanAction();
		}

		return false;
	}

	private void ValidateTeam()
	{
		for (int i = TeamInfo.Members.Count - 1; i >= 0; i--)
		{
			if (UnitArray.IsDeleted(TeamInfo.Members[i]))
			{
				TeamInfo.Members.RemoveAt(i);
			}
		}
	}

	private bool ThinkLeaderAction()
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

			if (firmCamp.RegionId != curRegionId)
				continue;

			//--- if the commander of this camp is the king, never replace him ---//

			if (firmCamp.OverseerId == nation.king_unit_recno)
				continue;

			//--- we have separate logic for choosing generals of capturing camps ---//

			if (firmCamp.ai_is_capturing_independent_village())
				continue;

			//-------------------------------------//

			int curLeadership = firmCamp.cur_commander_leadership();
			int newLeadership = firmCamp.new_commander_leadership(RaceId, Skill.SkillLevel);

			int curRating = newLeadership - curLeadership;

			//-------------------------------------//

			if (curRating > bestRating)
			{
				//--- if there is already somebody being assigned to it ---//

				ActionNode actionNode = null;

				if (Rank != RANK_KING) // don't check this if the current unit is the king
				{
					actionNode = nation.get_action(firmCamp.LocX1, firmCamp.LocY1,
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
		if (memberCount > 0 && memberCount - 1 <= Firm.MAX_WORKER - bestCamp.Workers.Count)
		{
			ValidateTeam();

			UnitArray.Assign(bestCamp.LocX1, bestCamp.LocY1, false, TeamInfo.Members, InternalConstants.COMMAND_AI);
			return true;
		}
		else //--- otherwise assign the general only ---//
		{
			return nation.add_action(bestCamp.LocX1, bestCamp.LocY1, -1, -1,
				Nation.ACTION_AI_ASSIGN_OVERSEER, Firm.FIRM_CAMP, 1, SpriteId) != null;
		}

		return false;
	}

	public bool ThinkNormalHumanAction()
	{
		if (HomeCampId != 0)
			return false;

		// if the unit is led by a commander, let the commander makes the decision.
		// If the leader has been assigned to a firm, don't consider it as a leader anymore
		if (LeaderId != 0 && UnitArray[LeaderId].IsVisible())
		{
			return false;
		}

		//---- think about assign the unit to a firm that needs workers ----//

		Nation ownNation = NationArray[NationId];
		Firm bestFirm = null;
		int regionId = World.GetRegionId(NextLocX, NextLocY);
		int skillId = Skill.SkillId;
		int skillLevel = Skill.SkillLevel;
		int bestRating = 0;
		int curXLoc = NextLocX, curYLoc = NextLocY;

		if (Skill.SkillId != 0)
		{
			foreach (Firm firm in FirmArray)
			{
				if (firm.NationId != NationId)
					continue;

				if (firm.RegionId != regionId)
					continue;

				int curRating = 0;

				if (Skill.SkillId == Skill.SKILL_CONSTRUCTION) // if this is a construction worker
				{
					if (firm.BuilderId != 0) // assign the construction worker to this firm as an residental builder
						continue;
				}
				else
				{
					if (firm.Workers.Count == 0 || firm.FirmSkillId != skillId)
					{
						continue;
					}

					//----- if the firm is full of worker ------//

					if (firm.IsWorkerFull())
					{
						if (firm.FirmType != Firm.FIRM_CAMP)
						{
							//---- get the lowest skill worker of the firm -----//

							int minSkill = 1000;

							for (int j = 0; j < firm.Workers.Count; j++)
							{
								Worker worker = firm.Workers[j];
								if (worker.skill_level < minSkill)
									minSkill = worker.skill_level;
							}

							//------------------------------//

							if (firm.MajorityRace() == RaceId)
							{
								if (Skill.SkillLevel < minSkill + 10)
									continue;
							}
							else //-- for different race, only assign if the skill is significantly higher than the existing ones --//
							{
								if (Skill.SkillLevel < minSkill + 30)
									continue;
							}
						}
						else
						{
							//---- get the lowest max hit points worker of the camp -----//

							int minMaxHitPoints = 1000;
							int minMaxHitPointsOtherRace = 1000;
							bool hasOtherRace = false;

							for (int j = 0; j < firm.Workers.Count; j++)
							{
								Worker worker = firm.Workers[j];
								if (worker.max_hit_points() < minMaxHitPoints)
									minMaxHitPoints = worker.max_hit_points();

								if (worker.race_id != firm.MajorityRace())
								{
									hasOtherRace = true;
									if (worker.max_hit_points() < minMaxHitPointsOtherRace)
										minMaxHitPointsOtherRace = worker.max_hit_points();
								}
							}

							if (firm.MajorityRace() == RaceId)
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

				curRating += World.DistanceRating(curXLoc, curYLoc, firm.LocCenterX, firm.LocCenterY);

				if (firm.MajorityRace() == RaceId)
					curRating += 70;

				curRating += (Firm.MAX_WORKER - firm.Workers.Count) * 10;

				//-------------------------------------//

				if (curRating > bestRating)
				{
					bestRating = curRating;
					bestFirm = firm;
				}
			}

			if (bestFirm != null)
			{
				if (bestFirm.FirmType == Firm.FIRM_CAMP && bestFirm.Workers.Count == Firm.MAX_WORKER &&
				    Misc.points_distance(curXLoc, curYLoc, bestFirm.LocX1, bestFirm.LocY1) < 5)
				{
					int minMaxHitPointsOtherRace = 1000;
					int bestWorkerId = -1;
					for (int j = 0; j < bestFirm.Workers.Count; j++)
					{
						Worker worker = bestFirm.Workers[j];
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
						for (int j = 0; j < bestFirm.Workers.Count; j++)
						{
							Worker worker = bestFirm.Workers[j];
							if (worker.max_hit_points() < minMaxHitPoints)
							{
								minMaxHitPoints = worker.max_hit_points();
								bestWorkerId = j + 1;
							}
						}
					}

					if (bestWorkerId != -1)
					{
						bestFirm.MobilizeWorker(bestWorkerId, InternalConstants.COMMAND_AI);
					}
				}

				Assign(bestFirm.LocX1, bestFirm.LocY1);
				if (bestFirm.FirmType == Firm.FIRM_CAMP)
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
				Assign(bestTown.LocX1, bestTown.LocY1);
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

				// if this unit is commanded by a leader, let the leader build the camp
				if (ownNation.cash > FirmRes[Firm.FIRM_CAMP].setup_cost && CanBuild(Firm.FIRM_CAMP) && LeaderId == 0)
				{
					AIBuildCamp();
				}
			}
			else // if there is already a camp in this region, try to settle a new town next to the camp
			{
				AISettleNewTown();
			}

			// if we don't have any town in this region, return 1, so this unit won't be resigned
			// and so it can wait for other units to set up camps and villages later ---//
			return true;
		}

		return false;
	}

	private bool ThinkWeaponAction()
	{
		//---- first try to assign the weapon to an existing camp ----//

		if (ThinkAssignWeaponToCamp())
			return true;

		//----- if no camp to assign, build a new one ------//

		//if (ThinkBuildCamp())
			//return true;

		return false;
	}

	private bool ThinkAssignWeaponToCamp()
	{
		Nation nation = NationArray[NationId];
		FirmCamp bestCamp = null;
		int bestRating = 0;
		int regionId = World.GetRegionId(NextLocX, NextLocY);
		int curXLoc = NextLocX, curYLoc = NextLocY;

		for (int i = 0; i < nation.ai_camp_array.Count; i++)
		{
			FirmCamp firmCamp = (FirmCamp)FirmArray[nation.ai_camp_array[i]];

			if (firmCamp.RegionId != regionId || firmCamp.IsWorkerFull())
				continue;

			int curRating = World.DistanceRating(curXLoc, curYLoc, firmCamp.LocCenterX, firmCamp.LocCenterY);
			curRating += (Firm.MAX_WORKER - firmCamp.Workers.Count) * 10;

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestCamp = firmCamp;
			}
		}

		if (bestCamp != null)
		{
			Assign(bestCamp.LocX1, bestCamp.LocY1);
			return true;
		}

		return false;
	}

	public bool ThinkBuildCamp()
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

	private int CommandedSoldierCount()
	{
		if (Rank != RANK_GENERAL && Rank != RANK_KING)
			return 0;

		int soldierCount = 0;

		if (IsVisible())
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
				if (firm.FirmType == Firm.FIRM_CAMP) // it can be an overseer of a seat of power
					soldierCount = firm.Workers.Count;
			}
		}

		return soldierCount;
	}

	private bool ThinkReward()
	{
		Nation ownNation = NationArray[NationId];

		//----------------------------------------------------------//
		// The need to secure high loyalty on this unit is based on:
		// -its skill
		// -its combat level 
		// -soldiers commanded by this unit
		//----------------------------------------------------------//

		if (SpyId != 0 && TrueNationId() == NationId) // if this is a spy of ours
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
		else if (Skill.SkillId == Skill.SKILL_LEADING)
		{
			//----- calculate the needed loyalty --------//

			neededLoyalty = CommandedSoldierCount() * 5 + Skill.SkillLevel;

			if (UnitMode == UnitConstants.UNIT_MODE_OVERSEE) // if this unit is an overseer
			{
				// if this unit's loyalty is < betray level, reward immediately
				if (Loyalty < GameConstants.UNIT_BETRAY_LOYALTY)
				{
					Reward(NationId); // reward it immediately if it's an overseer, don't check ai_should_spend()
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
			Reward(NationId);
			return true;
		}

		return false;
	}

	private void ThinkSpyAction()
	{
		//ai_move_to_nearby_town();		// just move it to one of our towns
	}

	private bool ThinkKingFlee()
	{
		// the king is already fleeing now
		if (ForceMove && CurAction != SPRITE_IDLE && CurAction != SPRITE_ATTACK)
			return true;

		//------- if the king is alone --------//

		Nation ownNation = NationArray[NationId];

		//------------------------------------------------//
		// When the king is alone and there is no assigned action OR
		// when the king is injured, the king will flee back to its camp.
		//------------------------------------------------//

		if ((TeamInfo.Members.Count == 0 && AIActionId == 0) || HitPoints < 125 - ownNation.pref_military_courage / 4)
		{
			//------------------------------------------//
			// If the king is currently under attack, flee to the nearest camp with the maximum protection.
			//------------------------------------------//

			FirmCamp bestCamp = null;
			int bestRating = 0;
			int curLocX = NextLocX, curLocY = NextLocY;
			int curRegionId = World.GetRegionId(curLocX, curLocY);

			if (CurAction == SPRITE_ATTACK)
			{
				for (int i = ownNation.ai_camp_array.Count - 1; i >= 0; i--)
				{
					FirmCamp firmCamp = (FirmCamp)FirmArray[ownNation.ai_camp_array[i]];

					if (firmCamp.RegionId != curRegionId)
						continue;

					// if there is already a commander in this camp. However if this is the king, than ignore this
					if (firmCamp.OverseerId != 0 && Rank != RANK_KING)
						continue;

					if (firmCamp.ai_is_capturing_independent_village())
						continue;

					int curRating = World.DistanceRating(curLocX, curLocY, firmCamp.LocCenterX, firmCamp.LocCenterY);

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

			if (bestCamp != null)
			{
				if (Config.ai_aggressiveness > Config.OPTION_LOW)
					ForceMove = true;

				Assign(bestCamp.LocX1, bestCamp.LocY1);
			}
			else // if the king is neither under attack or has a home camp, then call the standard ThinkLeaderAction()
			{
				ThinkLeaderAction();
			}

			return CurAction != SPRITE_IDLE;
		}

		return false;
	}

	private bool ThinkGeneralFlee()
	{
		// the general is already fleeing now
		if (ForceMove && CurAction != SPRITE_IDLE && CurAction != SPRITE_ATTACK)
			return true;

		//------- if the general is alone --------//

		Nation ownNation = NationArray[NationId];

		//------------------------------------------------//
		// When the general is alone and there is no assigned action OR
		// when the general is injured, the general will flee back to its camp.
		//------------------------------------------------//

		if ((TeamInfo.Members.Count == 0 && AIActionId == 0) ||
		    HitPoints < MaxHitPoints * (75 + ownNation.pref_military_courage / 2) / 200.0) // 75 to 125 / 200
		{
			//------------------------------------------//
			// If the general is currently under attack, flee to the nearest camp with the maximum protection.
			//------------------------------------------//

			FirmCamp bestCamp = null;
			int bestRating = 0;
			int curLocX = NextLocX, curLocY = NextLocY;
			int curRegionId = World.GetRegionId(curLocX, curLocY);

			if (CurAction == SPRITE_ATTACK)
			{
				for (int i = ownNation.ai_camp_array.Count - 1; i >= 0; i--)
				{
					FirmCamp firmCamp = (FirmCamp)FirmArray[ownNation.ai_camp_array[i]];

					if (firmCamp.RegionId != curRegionId)
						continue;

					if (firmCamp.ai_is_capturing_independent_village())
						continue;

					int curRating = World.DistanceRating(curLocX, curLocY, firmCamp.LocCenterX, firmCamp.LocCenterY);

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

			if (bestCamp != null)
			{
				// if there is already an overseer there, just move close to the camp for protection
				if (bestCamp.OverseerId != 0)
				{
					if (Config.ai_aggressiveness > Config.OPTION_LOW)
						ForceMove = true;

					MoveTo(bestCamp.LocX1, bestCamp.LocY1);
				}
				else
				{
					Assign(bestCamp.LocX1, bestCamp.LocY1);
				}
			}
			else // if the general is neither under attack or has a home camp, then call the standard ThinkLeaderAction()
			{
				ThinkLeaderAction();
			}

			return CurAction != SPRITE_IDLE;
		}

		return false;
	}

	private bool ThinkStopChase()
	{
		//-----------------------------------------------------//
		// Stop the chase if the target is being far away from its original attacking location.
		//-----------------------------------------------------//

		if (!(ActionMode == UnitConstants.ACTION_ATTACK_UNIT && AIOriginalTargetLocX >= 0))
			return false;

		if (UnitArray.IsDeleted(ActionParam))
		{
			Stop2();
			return true;
		}

		Unit targetUnit = UnitArray[ActionParam];
		if (!targetUnit.IsVisible())
		{
			Stop2();
			return true;
		}

		//----------------------------------------//

		int aiChaseDistance = 10 + NationArray[NationId].pref_military_courage / 20; // chase distance: 10 to 15

		int curDistance = Misc.points_distance(targetUnit.NextLocX, targetUnit.NextLocY, AIOriginalTargetLocX, AIOriginalTargetLocY);

		if (curDistance <= aiChaseDistance)
			return false;

		//--------- stop the unit ----------------//

		Stop2();

		//--- if this unit leads a troop, stop the action of all troop members as well ---//

		int leaderUnitId = LeaderId != 0 ? LeaderId : SpriteId;

		TeamInfo teamInfo = UnitArray[leaderUnitId].TeamInfo;

		for (int i = teamInfo.Members.Count - 1; i >= 0; i--)
		{
			int unitId = teamInfo.Members[i];

			if (UnitArray.IsDeleted(unitId))
				continue;

			UnitArray[unitId].Stop2();
		}

		return true;
	}

	private bool AIEscapeFire()
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
		int curXLoc = NextLocX;
		int curYLoc = NextLocY;

		for (int i = 2; i < checkLimit; i++)
		{
			Misc.cal_move_around_a_point(i, 20, 20, out int xShift, out int yShift);

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

	private bool AIBuildCamp()
	{
		//--- to prevent building more than one camp at the same time ---//

		int curRegionId = RegionId();
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

	private bool AISettleNewTown()
	{
		//----- locate a suitable camp for the new town to settle next to ----//

		Nation ownNation = NationArray[NationId];
		FirmCamp bestCamp = null;
		int curRegionId = RegionId();
		int bestRating = 0;

		for (int i = ownNation.ai_camp_array.Count - 1; i >= 0; i--)
		{
			FirmCamp firmCamp = (FirmCamp)FirmArray[ownNation.ai_camp_array[i]];

			if (firmCamp.RegionId != curRegionId)
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

		int xLoc = bestCamp.LocX1;
		int yLoc = bestCamp.LocY1;

		if (World.LocateSpace(ref xLoc, ref yLoc, bestCamp.LocX2, bestCamp.LocY2,
			    InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, UnitConstants.UNIT_LAND, curRegionId, true))
		{
			Settle(xLoc, yLoc);
			return true;
		}

		return false;
	}

	#endregion
}
