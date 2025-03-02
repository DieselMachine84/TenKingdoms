using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class UnitArray : SpriteArray
{
	private const int MAX_TARGET_SIZE = 4;
	private const int MAX_UNIT_SURROUND_SIZE = MAX_TARGET_SIZE + 2;
	private const int SHIFT_ADJUST = 1;
	
    public int selected_recno;
    public int selected_count;

    public int cur_group_id;            // for Unit::unit_group_id
    public int cur_team_id;             // for Unit::team_id

    public int idle_blocked_unit_reset_count; // used to improve performance for searching related to attack

    public int visible_unit_count;

    public List<int> selected_land_units = new List<int>();
    public List<int> selected_sea_units = new List<int>();
    public List<int> selected_air_units = new List<int>();

    public override Unit this[int recNo] => (Unit)base[recNo];

    private Power Power => Sys.Instance.Power;
    private World World => Sys.Instance.World;
    private SeekPath SeekPath => Sys.Instance.SeekPath;
    private TerrainRes TerrainRes => Sys.Instance.TerrainRes;
    private FirmRes FirmRes => Sys.Instance.FirmRes;
    private UnitRes UnitRes => Sys.Instance.UnitRes;
    private GodRes GodRes => Sys.Instance.GodRes;
    private NationArray NationArray => Sys.Instance.NationArray;
    private FirmArray FirmArray => Sys.Instance.FirmArray;
    private TownArray TownArray => Sys.Instance.TownArray;
    private RebelArray RebelArray => Sys.Instance.RebelArray;
    
    public UnitArray()
    {
	    //-------- initialize group selection parameter -------//

	    cur_group_id = 1;
	    cur_team_id  = 1;

	    idle_blocked_unit_reset_count = 0;
	    visible_unit_count = 0;
    }

    protected override Sprite CreateNewObject(int objectType)
    {
	    switch (objectType)
	    {
		    case UnitConstants.UNIT_CARAVAN:
			    return new UnitCaravan();

		    case UnitConstants.UNIT_VESSEL:
		    case UnitConstants.UNIT_TRANSPORT:
		    case UnitConstants.UNIT_CARAVEL:
		    case UnitConstants.UNIT_GALLEON:
			    return new UnitMarine();

		    case UnitConstants.UNIT_EXPLOSIVE_CART:
			    return new UnitExpCart();

		    default:
			    UnitInfo unitInfo = UnitRes[objectType];

			    if (unitInfo.is_monster != 0)
				    return new UnitMonster();

			    if (unitInfo.solider_id != 0) // if it is a vehicle unit
				    return new UnitVehicle();

			    if (GodRes.is_god_unit(objectType))
				    return new UnitGod();

			    return new Unit();
	    }
    }

    public Unit AddUnit(int unitId, int nationRecno, int rankId = 0, int unitLoyalty = 0,
	    int startXLoc = -1, int startYLoc = -1)
    {
	    Unit unit = (Unit)AddSprite(unitId);
	    unit.init(unitId, nationRecno, rankId, unitLoyalty, startXLoc, startYLoc);

	    return unit;
    }

    public void DeleteUnit(Unit unit)
    {
	    DeleteSprite(unit);
    }

    public void DisappearInTown(Unit unit, Town town)
    {
	    if (unit.unit_mode == UnitConstants.UNIT_MODE_REBEL)
		    RebelArray.SettleTown(unit, town);

	    DeleteUnit(unit);
    }

    public void disappear_in_firm(int unitRecno)
    {
	    Unit unit = this[unitRecno];

	    DeleteUnit(unit);
    }

    public override void die(int unitRecno)
    {
	    Unit unit = this[unitRecno];

	    if( unit.unit_mode == UnitConstants.UNIT_MODE_REBEL )
		    RebelArray.drop_rebel_identity(unitRecno);

	    unit.die();

	    DeleteUnit(unit);
    }

    public override bool IsDeleted(int recNo)
    {
	    if (base.IsDeleted(recNo))
		    return true;

	    Unit unit = this[recNo];
	    return unit.hit_points <= 0 || unit.cur_action == Sprite.SPRITE_DIE || unit.action_mode == UnitConstants.ACTION_DIE;
    }
    
    //public override IEnumerator<Unit> GetEnumerator()
    //{
	    //var baseEnumerator = base.GetEnumerator();
	    //return (IEnumerator<Unit>)baseEnumerator;
    //}

    public override void Process()
    {
	    foreach (Unit unit in this)
	    {
		    int unitRecno = unit.sprite_recno;

		    // only process each firm once per day
		    if (unitRecno % InternalConstants.FRAMES_PER_DAY ==
		        Sys.Instance.FrameNumber % InternalConstants.FRAMES_PER_DAY)
		    {
			    //-------------- reset ignore_power_nation ------------//
			    if ((unitRecno % (365 * InternalConstants.FRAMES_PER_DAY) == Sys.Instance.FrameNumber % (365 * InternalConstants.FRAMES_PER_DAY))
			        && unit.unit_id != UnitConstants.UNIT_CARAVAN)
			    {
				    unit.ignore_power_nation = 0;
			    }

			    unit.next_day();

			    if (IsDeleted(unitRecno))
				    continue;

			    if (unit.ai_unit)
				    unit.process_ai();

			    //----- if it's an independent unit -------//

			    else if (unit.nation_recno == 0 && unit.race_id != 0 && unit.spy_recno == 0)
				    unit.think_independent_unit();
		    }
	    }

	    if (idle_blocked_unit_reset_count < 50)
		    idle_blocked_unit_reset_count++; // the ability to restart idle blocked attacking unit

	    //------- process Sprite ---------//

	    base.Process();
    }

    public void return_camp(int remoteAction, List<int> selectedUnitArray = null)
    {
	    if (selectedUnitArray == null)
	    {
		    /*if (!remoteAction && remote.is_enable())
		    {
			    int selectedCount = 0;

			    foreach (Unit unit in this)
			    {
				    if (!unit.selected_flag || !unit.is_visible())
					    continue;
				    if (unit.hit_points <= 0 || unit.cur_action == Sprite.SPRITE_DIE ||
				        unit.action_mode == UnitConstants.ACTION_DIE)
					    continue;
				    if (!unit.is_own()) // only if the unit belongs to us (a spy is also okay if true_nation_recno is ours)
					    continue;
				    //---------------------------------//
				    if (unit.home_camp_firm_recno != 0)
					    selectedCount++;
			    }

			    // packet structure : <no. of units> <unit recno>...
			    short* shortPtr = (short*)remote.new_send_queue_msg(MSG_UNITS_RETURN_CAMP, (1 + selectedCount) * sizeof(short));
			    *shortPtr = selectedCount;
			    shortPtr++;

			    int reCount = 0;
			    foreach (Unit unit in this)
			    {
				    if (!unit.selected_flag || !unit.is_visible())
					    continue;
				    if (unit.hit_points <= 0 || unit.cur_action == Sprite.SPRITE_DIE ||
				        unit.action_mode == UnitConstants.ACTION_DIE)
					    continue;
				    if (!unit.is_own()) // only if the unit belongs to us (a spy is also okay if true_nation_recno is ours)
					    continue;
				    //---------------------------------//
				    if (unit.home_camp_firm_recno != 0)
				    {
					    *shortPtr = i;
					    shortPtr++;
					    reCount++;
				    }
			    }

			    return;
		    }*/
		    //else
		    //{
			    foreach (Unit unit in this)
			    {
				    if (!unit.selected_flag || !unit.is_visible())
					    continue;
				    if (unit.hit_points <= 0 || unit.cur_action == Sprite.SPRITE_DIE ||
				        unit.action_mode == UnitConstants.ACTION_DIE)
					    continue;
				    if (!unit.is_own()) // only if the unit belongs to us (a spy is also okay if true_nation_recno is ours)
					    continue;
				    //---------------------------------//
				    if (unit.home_camp_firm_recno != 0)
					    unit.return_camp();
			    }
		    //}
	    }
	    else
	    {
		    /*if (!remoteAction && remote.is_enable())
		    {
			    if (selectedUnitArray.Count > 0)
			    {
				    // packet structure : <no. of units> <unit recno>...
				    short* shortPtr =
					    (short*)remote.new_send_queue_msg(MSG_UNITS_RETURN_CAMP, (1 + selectedCount) * sizeof(short));
				    *shortPtr = selectedCount;
				    shortPtr++;
				    memcpy(shortPtr, selectedUnitArray, sizeof(short) * selectedCount);
			    }

			    return;
		    }*/
		    //else
		    //{
			    for (int j = selectedUnitArray.Count - 1; j >= 0; j--)
			    {
				    // descending unit recno
				    int recNo = selectedUnitArray[j];
				    if (IsDeleted(recNo))
					    continue;
				    Unit unit = this[recNo];
				    if (!unit.is_visible())
					    continue;
				    if (unit.hit_points <= 0 || unit.cur_action == Sprite.SPRITE_DIE ||
				        unit.action_mode == UnitConstants.ACTION_DIE)
					    continue;
				    //---------------------------------//
				    if (unit.home_camp_firm_recno != 0)
					    unit.return_camp();
			    }
		    //}
	    }
    }

    public void stop(List<int> selectedUnitArray, int remoteAction)
    {
	    //-------- if it's a multiplayer game --------//
	    //
	    // Queue the action.
	    //
	    // Local: this action will be processed when all action messages of all
	    //			 remote players are ready from the next frame.
	    //
	    // Remote: this action will be processed when the remote has received
	    //			  frame sync notification from all other players.
	    //
	    //--------------------------------------------//

	    /*if (!remoteAction && remote.is_enable())
	    {
		    short* shortPtr = (short*)remote.new_send_queue_msg(MSG_UNIT_STOP, sizeof(short) * (1 + selectedCount));

		    shortPtr[0] = selectedCount;

		    memcpy(shortPtr + 1, selectedUnitArray, sizeof(short) * selectedCount);
	    }*/
	    //else
	    //{
		    int curGroupId = cur_group_id++;

		    //-------------- stop now ---------------//

		    for (int i = 0; i < selectedUnitArray.Count; i++)
		    {
			    Unit unit = this[selectedUnitArray[i]];
			    unit.unit_group_id = curGroupId;
			    unit.stop2();
		    }
	    //}
    }

    public void stop_all_war(int oldNationRecno)
    {
	    foreach (Unit unit in this)
	    {
		    if (!unit.is_visible())
			    continue;

		    if (unit.nation_recno == oldNationRecno)
		    {
			    //------ stop all attacking unit with nation_recno = oldNationRecno -----//
			    if (unit.is_action_attack())
				    unit.stop2(UnitConstants.KEEP_DEFENSE_MODE);
		    }
		    else if (unit.is_action_attack())
		    {
			    int targetType;
			    int targetRecno;
			    int targetXLoc, targetYLoc;

			    //---- stop all attacking unit with target's nation_recno = oldNationRecno ----//
			    if (unit.action_mode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET)
			    {
				    targetType = unit.action_mode;
				    targetRecno = unit.action_para;
				    targetXLoc = unit.action_x_loc;
				    targetYLoc = unit.action_y_loc;
			    }
			    else
			    {
				    targetType = unit.action_mode2;
				    targetRecno = unit.action_para2;
				    targetXLoc = unit.action_x_loc2;
				    targetYLoc = unit.action_y_loc2;
			    }

			    switch (targetType)
			    {
				    case UnitConstants.ACTION_ATTACK_UNIT:
				    case UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET:
				    case UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET:
					    if (IsDeleted(targetRecno))
						    continue;

					    Unit targetUnit = this[targetRecno];
					    if (targetUnit.nation_recno == oldNationRecno)
						    unit.stop2(UnitConstants.KEEP_DEFENSE_MODE);
					    break;

				    case UnitConstants.ACTION_ATTACK_FIRM:
					    if (FirmArray.IsDeleted(targetRecno))
						    continue;

					    Firm targetFirm = FirmArray[targetRecno];
					    if (targetFirm.nation_recno == oldNationRecno)
						    unit.stop2(UnitConstants.KEEP_DEFENSE_MODE);
					    break;

				    case UnitConstants.ACTION_ATTACK_TOWN:
					    if (TownArray.IsDeleted(targetRecno))
						    continue;

					    Town targetTown = TownArray[targetRecno];
					    if (targetTown.NationId == oldNationRecno)
						    unit.stop2(UnitConstants.KEEP_DEFENSE_MODE);
					    break;

				    case UnitConstants.ACTION_ATTACK_WALL:
					    Location targetLoc = World.get_loc(targetXLoc, targetYLoc);
					    if (targetLoc.wall_nation_recno() == oldNationRecno)
						    unit.stop2(UnitConstants.KEEP_DEFENSE_MODE);
					    break;
			    }
		    }
	    }
    }

    public void stop_war_between(int nationRecno1, int nationRecno2)
    {
	    foreach (Unit unit in this)
	    {
		    if (unit.nation_recno != nationRecno1 && unit.nation_recno != nationRecno2)
			    continue;

		    if (unit.is_action_attack())
		    {
			    int targetType;
			    int targetRecno;
			    int targetXLoc, targetYLoc;

			    //---- stop all attacking unit with target's nation_recno = oldNationRecno ----//
			    if (unit.action_mode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET)
			    {
				    targetType = unit.action_mode;
				    targetRecno = unit.action_para;
				    targetXLoc = unit.action_x_loc;
				    targetYLoc = unit.action_y_loc;
			    }
			    else
			    {
				    targetType = unit.action_mode2;
				    targetRecno = unit.action_para2;
				    targetXLoc = unit.action_x_loc2;
				    targetYLoc = unit.action_y_loc2;
			    }

			    switch (targetType)
			    {
				    case UnitConstants.ACTION_ATTACK_UNIT:
				    case UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET:
				    case UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET:
					    if (IsDeleted(targetRecno))
						    continue;

					    Unit targetUnit = this[targetRecno];
					    if (targetUnit.nation_recno == nationRecno1 || targetUnit.nation_recno == nationRecno2)
						    unit.stop2(UnitConstants.KEEP_DEFENSE_MODE);
					    break;

				    case UnitConstants.ACTION_ATTACK_FIRM:
					    if (FirmArray.IsDeleted(targetRecno))
						    continue;

					    Firm targetFirm = FirmArray[targetRecno];
					    if (targetFirm.nation_recno == nationRecno1 || targetFirm.nation_recno == nationRecno2)
						    unit.stop2(UnitConstants.KEEP_DEFENSE_MODE);
					    break;

				    case UnitConstants.ACTION_ATTACK_TOWN:
					    if (TownArray.IsDeleted(targetRecno))
						    continue;

					    Town targetTown = TownArray[targetRecno];
					    if (targetTown.NationId == nationRecno1 || targetTown.NationId == nationRecno2)
						    unit.stop2(UnitConstants.KEEP_DEFENSE_MODE);
					    break;

				    case UnitConstants.ACTION_ATTACK_WALL:
					    Location targetLoc = World.get_loc(targetXLoc, targetYLoc);
					    if (targetLoc.wall_nation_recno() == nationRecno1 ||
					        targetLoc.wall_nation_recno() == nationRecno2)
						    unit.stop2(UnitConstants.KEEP_DEFENSE_MODE);
					    break;
			    }
		    }
	    }
    }

    public void stop_attack_unit(int unitRecno)
    {
	    if (NationBase.nation_hand_over_flag != 0)
		    return;

	    foreach (Unit unit in this)
	    {
		    if (!unit.is_visible())
			    continue;

		    if ((unit.action_para == unitRecno && unit.action_mode == UnitConstants.ACTION_ATTACK_UNIT) ||
		        (unit.action_para2 == unitRecno && unit.action_mode2 == UnitConstants.ACTION_ATTACK_UNIT))
			    unit.stop2(UnitConstants.KEEP_DEFENSE_MODE);
	    }
    }

    public void stop_attack_firm(int firmRecno)
    {
	    if (NationBase.nation_hand_over_flag != 0)
		    return;

	    foreach (Unit unit in this)
	    {
		    if (!unit.is_visible())
			    continue;

		    if ((unit.action_para == firmRecno && unit.action_mode == UnitConstants.ACTION_ATTACK_FIRM) ||
		        (unit.action_para2 == firmRecno && unit.action_mode2 == UnitConstants.ACTION_ATTACK_FIRM))
			    unit.stop2(UnitConstants.KEEP_DEFENSE_MODE);
	    }
    }

    public void stop_attack_town(int townRecno)
    {
	    if (NationBase.nation_hand_over_flag != 0)
		    return;

	    foreach (Unit unit in this)
	    {
		    if (!unit.is_visible())
			    continue;

		    if ((unit.action_para == townRecno && unit.action_mode == UnitConstants.ACTION_ATTACK_TOWN) ||
		        (unit.action_para2 == townRecno && unit.action_mode2 == UnitConstants.ACTION_ATTACK_TOWN))
			    unit.stop2(UnitConstants.KEEP_DEFENSE_MODE);
	    }
    }

    //---------- move main functions -------------//
    public void move_to(int destXLoc, int destYLoc, bool divided, List<int> selectedUnitArray, int remoteAction)
    {
	    //-------- if it's a multiplayer game --------//
	    /*if (!remoteAction && remote.is_enable())
	    {
		    short* shortPtr = (short*)remote.new_send_queue_msg(MSG_UNIT_MOVE,
			    sizeof(short) * (4 + selectedCount));

		    shortPtr[0] = destXLoc;
		    shortPtr[1] = destYLoc;
		    shortPtr[2] = selectedCount;
		    shortPtr[3] = divided;

		    memcpy(shortPtr + 4, selectedUnitArray, sizeof(short) * selectedCount);
	    }*/
	    //else
	    //{
		    if (!divided)
		    {
			    //----------- divide units ------------//
			    divide_array(destXLoc, destYLoc, selectedUnitArray);

			    //---------- process group move --------------//
			    if (selected_land_units.Count > 0)
				    move_to(destXLoc, destYLoc, true, selected_land_units, InternalConstants.COMMAND_AUTO);

			    if (selected_sea_units.Count > 0)
			    {
				    Location location = World.get_loc(destXLoc, destYLoc);
				    if (TerrainRes[location.terrain_id].average_type == TerrainTypeCode.TERRAIN_OCEAN)
					    move_to(destXLoc, destYLoc, true, selected_sea_units, InternalConstants.COMMAND_AUTO);
				    else
					    ship_to_beach(destXLoc, destYLoc, true, selected_sea_units, InternalConstants.COMMAND_AUTO);
			    }

			    if (selected_air_units.Count > 0)
				    move_to(destXLoc, destYLoc, true, selected_air_units, InternalConstants.COMMAND_AUTO);

			    //---------------- deinit static parameters ------------------//
			    selected_land_units.Clear();
			    selected_sea_units.Clear();
			    selected_air_units.Clear();
			    return;
		    }
		    else
		    {
			    //---------------------------------------------------------//
			    // set the unit_group_id
			    //---------------------------------------------------------//
			    int curGroupId = cur_group_id++;

			    for (int k = 0; k < selectedUnitArray.Count; k++)
			    {
				    Unit unit = this[selectedUnitArray[k]];

				    unit.unit_group_id = curGroupId;
				    unit.action_mode = UnitConstants.ACTION_MOVE;
				    unit.action_para = 0;

				    if (unit.action_mode2 != UnitConstants.ACTION_MOVE)
				    {
					    unit.action_mode2 = UnitConstants.ACTION_MOVE;
					    unit.action_para2 = 0;
					    unit.action_x_loc2 = unit.action_y_loc2 = -1;
				    } // else keep the data to check whether same action mode is ordered
			    }

			    //--------------------------------------------------------------//
			    // if only the leader unit is moving, no need to use formation
			    // movement although the button is pushed
			    //--------------------------------------------------------------//
			    if (selectedUnitArray.Count == 1)
			    {
				    Unit unit = this[selectedUnitArray[0]];
				    unit.move_to(destXLoc, destYLoc, 1);
			    }
			    else
			    {
				    Unit firstUnit = this[selectedUnitArray[0]];

				    if (firstUnit.mobile_type == UnitConstants.UNIT_LAND)
				    {
					    move_to_now_with_filter(destXLoc, destYLoc, selectedUnitArray);
					    SeekPath.SetSubMode(); // reset sub_mode searching
				    }
				    else
				    {
					    move_to_now_with_filter(destXLoc, destYLoc, selectedUnitArray);
				    }
			    }
		    }
	    //}
    }

    //------------- attack main functions ----------//
    public void attack(int targetXLoc, int targetYLoc, bool divided, List<int> selectedUnitArray,
	    int remoteAction, int targetUnitRecno)
    {
	    int targetNationRecno = 0;

	    if (targetUnitRecno == 0)
	    {
		    // unit determined from the location

		    Location location = World.get_loc(targetXLoc, targetYLoc);
		    targetUnitRecno = location.get_any_unit(out _);
	    }
	    else
	    {
		    // location determined by the unit
		    if (!IsDeleted(targetUnitRecno))
		    {
			    Unit unit = this[targetUnitRecno];
			    if (unit.is_visible())
			    {
				    targetXLoc = unit.next_x_loc();
				    targetYLoc = unit.next_y_loc();
				    if (unit.unit_id != UnitConstants.UNIT_EXPLOSIVE_CART) // attacking own porcupine is allowed
					    targetNationRecno = unit.nation_recno;
			    }
			    else
			    {
				    targetUnitRecno = 0;
			    }
		    }
		    else
		    {
			    targetUnitRecno = 0;
		    }
	    }

	    if (targetUnitRecno == 0)
	    {
		    //--- set the target coordination to the top left position of the town/firm ---//

		    Location location = World.get_loc(targetXLoc, targetYLoc);

		    //---- if there is a firm on this location -----//

		    if (location.is_firm())
		    {
			    Firm firm = FirmArray[location.firm_recno()];

			    targetXLoc = firm.loc_x1;
			    targetYLoc = firm.loc_y1;
			    targetNationRecno = firm.nation_recno;
		    }

		    //---- if there is a town on this location -----//

		    else if (location.is_town())
		    {
			    Town town = TownArray[location.town_recno()];

			    targetXLoc = town.LocX1;
			    targetYLoc = town.LocY1;
			    targetNationRecno = town.NationId;
		    }

		    else
			    return;
	    }

	    //--------- AI debug code ---------//

	    //--- AI attacking a nation which its NationRelation::should_attack is 0 ---//

	    Unit attackUnit = this[selectedUnitArray[0]];

	    if (attackUnit.nation_recno != 0 && targetNationRecno != 0)
	    {
		    if (!NationArray[attackUnit.nation_recno].get_relation(targetNationRecno).should_attack)
			    return;
	    }

	    //-------- if it's a multiplayer game --------//
	    /*if (!remoteAction && remote.is_enable())
	    {
		    short* shortPtr = (short*)remote.new_send_queue_msg(MSG_UNIT_ATTACK, sizeof(short) * (5 + selectedCount));

		    shortPtr[0] = targetXLoc;
		    shortPtr[1] = targetYLoc;
		    shortPtr[2] = targetUnitRecno;
		    shortPtr[3] = selectedCount;
		    shortPtr[4] = divided;

		    memcpy(shortPtr + 5, selectedUnitArray, sizeof(short) * selectedCount);
	    }*/
	    //else
	    //{
		    if (!divided)
		    {
			    // 1 for excluding the recno in target location
			    divide_array(targetXLoc, targetYLoc, selectedUnitArray, 1);

			    Location location = World.get_loc(targetXLoc, targetYLoc);
			    int targetMobileType = location.has_any_unit();

			    if (selected_land_units.Count > 0)
				    attack_call(targetXLoc, targetYLoc, UnitConstants.UNIT_LAND, targetMobileType, true,
					    selected_land_units, targetUnitRecno);

			    if (selected_sea_units.Count > 0)
				    attack_call(targetXLoc, targetYLoc, UnitConstants.UNIT_SEA, targetMobileType, true,
					    selected_sea_units, targetUnitRecno);

			    if (selected_air_units.Count > 0)
				    attack_call(targetXLoc, targetYLoc, UnitConstants.UNIT_AIR, targetMobileType, true,
					    selected_air_units, targetUnitRecno);

			    selected_land_units.Clear();
			    selected_sea_units.Clear();
			    selected_air_units.Clear();
		    }
	    //}
    }

    public void attack_unit(int targetXLoc, int targetYLoc, int targetUnitRecno, List<int> selectedUnits)
    {
	    if (selectedUnits.Count == 1)
	    {
		    Unit selectedUnit = this[selectedUnits[0]];
		    selectedUnit.unit_group_id = cur_group_id++;
		    selectedUnit.attack_unit(targetXLoc, targetYLoc, 0, 0, true);
		    return;
	    }

	    //********************** improve later begin **************************//
	    //---------------------------------------------------------------------//
	    // codes for differnt territory or different mobile_type attacking should
	    // be added in the future.
	    //---------------------------------------------------------------------//
	    Unit firstUnit = this[selectedUnits[0]];
	    Unit targetUnit = this[targetUnitRecno];
	    if ((World.get_loc(targetXLoc, targetYLoc).region_id !=
	         World.get_loc(firstUnit.next_x_loc(), firstUnit.next_y_loc()).region_id) ||
	        (targetUnit.mobile_type != firstUnit.mobile_type))
	    {
		    set_group_id(selectedUnits);

		    for (int i = 0; i < selectedUnits.Count; i++)
		    {
			    Unit selectedUnit = this[selectedUnits[i]];
			    selectedUnit.attack_unit(targetXLoc, targetYLoc, 0, 0, true);
		    }

		    return;
	    }
	    //*********************** improve later end ***************************//

	    //----------- initialize local parameters ------------//
	    int targetWidth = targetUnit.sprite_info.loc_width;
	    int targetHeight = targetUnit.sprite_info.loc_height;
	    int targetXLoc2 = targetXLoc + targetWidth - 1;
	    int targetYLoc2 = targetYLoc + targetHeight - 1;
	    int surroundLoc = get_target_surround_loc(targetWidth, targetHeight);
	    int[] xOffsetPtr = UnitAttackHelper.get_target_x_offset(targetWidth, targetHeight, 0);
	    int[] yOffsetPtr = UnitAttackHelper.get_target_y_offset(targetWidth, targetHeight, 0);

	    //---------------------------------------------------------------------//
	    // construct data structure
	    //---------------------------------------------------------------------//
	    int[][] dir_array_ptr = new int[UnitConstants.ATTACK_DIR][];
	    int[] dir_array_count = new int[UnitConstants.ATTACK_DIR];
	    int[] unit_processed_array = new int[selectedUnits.Count];
	    int unit_processed_count = 0;

	    for (int count = 0; count < UnitConstants.ATTACK_DIR; count++)
	    {
		    dir_array_ptr[count] = new int[selectedUnits.Count];
	    }

	    //---------------------------------------------------------------------//
	    // divide the units into each region
	    //---------------------------------------------------------------------//
	    Unit unit = this[selectedUnits[0]];
	    int groupId = cur_group_id++;
	    arrange_units_in_group(targetXLoc, targetYLoc, targetXLoc2, targetYLoc2,
		    selectedUnits, groupId, 1, dir_array_ptr, dir_array_count);

	    //---------------------------------------------------------------------//
	    // now the attackers are divided into 8 groups to attack the target
	    //---------------------------------------------------------------------//
	    int xOffset, yOffset; // offset location of the target
	    int dist, xDist, yDist;
	    int destCount;
	    int unitPos = 0; // store the position of the unit with minDist

	    bool[,] unreachable_table = new bool[MAX_UNIT_SURROUND_SIZE, MAX_UNIT_SURROUND_SIZE];

	    //---------------------------------------------------------------------//
	    // analyze the surrounding location of the target
	    //---------------------------------------------------------------------//
	    int analyseResult = analyse_surround_location(targetXLoc, targetYLoc, targetWidth, targetHeight,
			    targetUnit.mobile_type, unreachable_table);

	    if (analyseResult == 0) // 0 if all surround location is not accessible
	    {
		    //------------------------------------------------------------//
		    // special handling for this case
		    //------------------------------------------------------------//
		    handle_attack_target_totally_blocked(targetXLoc, targetYLoc, targetUnitRecno, selectedUnits, 1,
			    unit_processed_array, unit_processed_count);

		    return;
	    }

	    //---------------------------------------------------------------------//
	    // let the units move to the rest accessible location
	    //---------------------------------------------------------------------//
	    for (int count = 0; count < UnitConstants.ATTACK_DIR; count++) // for each array/group
	    {
		    //--------- initialize for each group --------//
		    int unprocessed = dir_array_count[count]; // get the number of units in this region
		    if (unprocessed == 0)
			    continue;

		    destCount = surroundLoc - 1;
		    int[] curArray = dir_array_ptr[count]; // get the recno of units in this region

		    xOffsetPtr = UnitAttackHelper.get_target_x_offset(targetWidth, targetHeight, count);
		    yOffsetPtr = UnitAttackHelper.get_target_y_offset(targetWidth, targetHeight, count);
		    int xOffsetPtrIndex = 0;
		    int yOffsetPtrIndex = 0;
		    xOffset = xOffsetPtr[xOffsetPtrIndex];
		    yOffset = yOffsetPtr[yOffsetPtrIndex];

		    //-----------------------------------------------------------------//
		    // determine a suitable location for the attacker to move to
		    //-----------------------------------------------------------------//
		    while (unprocessed > 0)
		    {
			    //-----------------------------------------------------//
			    // find a reachable location, or not searched location
			    //-----------------------------------------------------//
			    if (analyseResult == 0)
			    {
				    handle_attack_target_totally_blocked(targetXLoc, targetYLoc, targetUnitRecno, selectedUnits, 1,
					    unit_processed_array, unit_processed_count);

				    return;
			    }
			    else
			    {
				    for (int i = 0; i < surroundLoc; i++)
				    {
					    if ((++destCount) >= surroundLoc)
					    {
						    destCount = 0;
						    xOffsetPtr = UnitAttackHelper.get_target_x_offset(targetWidth, targetHeight, count);
						    yOffsetPtr = UnitAttackHelper.get_target_y_offset(targetWidth, targetHeight, count);
						    xOffsetPtrIndex = 0;
						    yOffsetPtrIndex = 0;
						    xOffset = xOffsetPtr[xOffsetPtrIndex];
						    yOffset = yOffsetPtr[yOffsetPtrIndex];
					    }
					    else
					    {
						    xOffsetPtrIndex++;
						    yOffsetPtrIndex++;
						    xOffset = xOffsetPtr[xOffsetPtrIndex];
						    yOffset = yOffsetPtr[yOffsetPtrIndex];
					    }

					    if (!unreachable_table[xOffset + SHIFT_ADJUST, yOffset + SHIFT_ADJUST])
						    break;
				    }
			    }

			    //------------------------------------------------------------//
			    // find the closest attacker
			    //------------------------------------------------------------//
			    // actual target surrounding location to move to
			    int xLoc = targetXLoc + xOffset;
			    int yLoc = targetYLoc + yOffset;

			    int minDist = Int32.MaxValue;
			    for (int i = 0; i < unprocessed; i++)
			    {
				    unit = this[curArray[i]];
				    xDist = Math.Abs(xLoc - unit.next_x_loc());
				    yDist = Math.Abs(yLoc - unit.next_y_loc());
				    dist = (xDist >= yDist) ? xDist * 10 + yDist : yDist * 10 + xDist;

				    if (dist < minDist)
				    {
					    minDist = dist;
					    unitPos = i;
				    }
			    }

			    unit = this[curArray[unitPos]];
			    curArray[unitPos] = curArray[--unprocessed]; // move the units in the back to the front

			    //TODO remove
			    //SeekPath.set_status(SeekPath.PATH_WAIT);
			    unit.attack_unit(targetXLoc, targetYLoc, xOffset, yOffset, true);

			    //------------------------------------------------------------//
			    // store the unit sprite_recno in the array
			    //------------------------------------------------------------//
			    unit_processed_array[unit_processed_count++] = unit.sprite_recno;

			    //------------------------------------------------------------//
			    // set the flag if unreachable
			    //------------------------------------------------------------//
			    if (SeekPath.PathStatus == SeekPath.PATH_IMPOSSIBLE)
			    {
				    unreachable_table[xLoc - targetXLoc + SHIFT_ADJUST, yLoc - targetYLoc + SHIFT_ADJUST] = true;
				    analyseResult--;

				    //------------------------------------------------------------//
				    // the nearby location should also be unreachable
				    //------------------------------------------------------------//
				    check_nearby_location(targetXLoc, targetYLoc, xOffset, yOffset, targetWidth, targetHeight,
					    targetUnit.mobile_type, unreachable_table, ref analyseResult);
			    }

			    update_unreachable_table(targetXLoc, targetYLoc, targetWidth, targetHeight, unit.mobile_type,
				    ref analyseResult, unreachable_table);
		    }
	    }

	    //---------------------------------------------------------------------//
	    // set the unreachable flag for each units
	    //---------------------------------------------------------------------//
	    //-************** codes here ***************-//
    }

    public void attack_firm(int targetXLoc, int targetYLoc, int firmRecno, List<int> selectedUnits)
    {
	    if (selectedUnits.Count == 1)
	    {
		    Unit selectedUnit = this[selectedUnits[0]];
		    selectedUnit.unit_group_id = cur_group_id++;
		    selectedUnit.attack_firm(targetXLoc, targetYLoc);
		    return;
	    }

	    //********************** improve later begin **************************//
	    //---------------------------------------------------------------------//
	    // codes for differnt territory or different mobile_type attacking should
	    // be added in the future.
	    //---------------------------------------------------------------------//
	    Unit firstUnit = this[selectedUnits[0]];
	    if (World.get_loc(targetXLoc, targetYLoc).region_id !=
	        World.get_loc(firstUnit.next_x_loc(), firstUnit.next_y_loc()).region_id)
	    {
		    set_group_id(selectedUnits);

		    for (int i = 0; i < selectedUnits.Count; i++)
		    {
			    Unit selectedUnit = this[selectedUnits[i]];
			    selectedUnit.attack_firm(targetXLoc, targetYLoc);
		    }

		    return;
	    }
	    //*********************** improve later end ***************************//

	    //----------- initialize local parameters ------------//
	    Firm firm = FirmArray[firmRecno];
	    FirmInfo firmInfo = FirmRes[firm.firm_id];
	    int firmWidth = firmInfo.loc_width;
	    int firmHeight = firmInfo.loc_height;
	    int targetXLoc2 = targetXLoc + firmWidth - 1; // the lower right corner of the firm
	    int targetYLoc2 = targetYLoc + firmHeight - 1;

	    //---------------------------------------------------------------------//
	    // construct data structure
	    //---------------------------------------------------------------------//
	    int[][] dir_array_ptr = new int[UnitConstants.ATTACK_DIR][];
	    int[] dir_array_count = new int[UnitConstants.ATTACK_DIR];
	    int[] unit_processed_array = new int[selectedUnits.Count];
	    int unit_processed_count = 0;

	    for (int count = 0; count < UnitConstants.ATTACK_DIR; count++)
	    {
		    dir_array_ptr[count] = new int[selectedUnits.Count];
	    }

	    //---------------------------------------------------------------------//
	    // divide the units into each region
	    //---------------------------------------------------------------------//
	    Unit unit = this[selectedUnits[0]];
	    int groupId = cur_group_id++;
	    arrange_units_in_group(targetXLoc, targetYLoc, targetXLoc2, targetYLoc2,
		    selectedUnits, groupId, 2, dir_array_ptr, dir_array_count);

	    //---------------------------------------------------------------------//
	    // now the attackers are divided into 8 groups to attack the target
	    //---------------------------------------------------------------------//
	    int xOffset, yOffset; // offset location of the target
	    int unprocessed; // number of units in this group
	    int destCount;
	    int dist, xDist, yDist;
	    int unitPos = 0; // store the position of the unit with minDist
	    int surroundLoc = get_target_surround_loc(firmWidth, firmHeight);

	    bool[,] unreachable_table = new bool[MAX_UNIT_SURROUND_SIZE, MAX_UNIT_SURROUND_SIZE];

	    //---------------------------------------------------------------------//
	    // analyse the surrounding location of the target
	    //---------------------------------------------------------------------//
	    int analyseResult = analyse_surround_location(targetXLoc, targetYLoc, firmWidth, firmHeight,
		    unit.mobile_type, unreachable_table);

	    if (analyseResult == 0) // 0 if all surround location is not accessible
	    {
		    //------------------------------------------------------------//
		    // special handling for this case
		    //------------------------------------------------------------//
		    handle_attack_target_totally_blocked(targetXLoc, targetYLoc, firmRecno, selectedUnits, 2,
			    unit_processed_array, unit_processed_count);

		    return;
	    }

	    //---------------------------------------------------------------------//
	    // let the units move to the rest accessible location
	    //---------------------------------------------------------------------//
	    for (int count = 0; count < UnitConstants.ATTACK_DIR; count++) // for each array/group
	    {
		    //--------- initialize for each group --------//
		    unprocessed = dir_array_count[count]; // get the number of units in this region
		    int[] curArray = dir_array_ptr[count]; // get the recno of units in this region
		    destCount = surroundLoc - 1;
		    int[] xOffsetPtr = UnitAttackHelper.get_target_x_offset(firmWidth, firmHeight, count);
		    int[] yOffsetPtr = UnitAttackHelper.get_target_y_offset(firmWidth, firmHeight, count);
		    int xOffsetPtrIndex = 0;
		    int yOffsetPtrIndex = 0;
		    xOffset = xOffsetPtr[xOffsetPtrIndex];
		    yOffset = yOffsetPtr[yOffsetPtrIndex];

		    //-----------------------------------------------------------------//
		    // determine a suitable location for the attacker to move to
		    //-----------------------------------------------------------------//
		    while (unprocessed > 0)
		    {
			    //-----------------------------------------------------//
			    // find a reachable location, or not searched location
			    //-----------------------------------------------------//

			    if (analyseResult == 0)
			    {
				    handle_attack_target_totally_blocked(targetXLoc, targetYLoc, firmRecno, selectedUnits, 2,
					    unit_processed_array, unit_processed_count);

				    return;
			    }
			    else
			    {
				    for (int i = 0; i < surroundLoc; i++)
				    {
					    if ((++destCount) >= surroundLoc)
					    {
						    destCount = 0;
						    xOffsetPtr = UnitAttackHelper.get_target_x_offset(firmWidth, firmHeight, count);
						    yOffsetPtr = UnitAttackHelper.get_target_y_offset(firmWidth, firmHeight, count);
						    xOffsetPtrIndex = 0;
						    yOffsetPtrIndex = 0;
						    xOffset = xOffsetPtr[xOffsetPtrIndex];
						    yOffset = yOffsetPtr[yOffsetPtrIndex];
					    }
					    else
					    {
						    xOffsetPtrIndex++;
						    yOffsetPtrIndex++;
						    xOffset = xOffsetPtr[xOffsetPtrIndex];
						    yOffset = yOffsetPtr[yOffsetPtrIndex];
					    }

					    if (!unreachable_table[xOffset + SHIFT_ADJUST, yOffset + SHIFT_ADJUST])
						    break;
				    }
			    }

			    //------------------------------------------------------------//
			    // find the closest attacker
			    //------------------------------------------------------------//
			    // actual target surrounding location to move to
			    int xLoc = targetXLoc + xOffset;
			    int yLoc = targetYLoc + yOffset;

			    int minDist = Int32.MaxValue;
			    for (int i = 0; i < unprocessed; i++)
			    {
				    unit = this[curArray[i]];
				    xDist = Math.Abs(xLoc - unit.next_x_loc());
				    yDist = Math.Abs(yLoc - unit.next_y_loc());
				    dist = (xDist >= yDist) ? xDist * 10 + yDist : yDist * 10 + xDist;

				    if (dist < minDist)
				    {
					    minDist = dist;
					    unitPos = i;
				    }
			    }

			    unit = this[curArray[unitPos]];
			    curArray[unitPos] = curArray[--unprocessed]; // move the units in the back to the front

			    //TODO remove
			    //SeekPath.set_status(SeekPath.PATH_WAIT);
			    unit.attack_firm(targetXLoc, targetYLoc, xOffset, yOffset);

			    //------------------------------------------------------------//
			    // set the flag if unreachable
			    //------------------------------------------------------------//
			    if (SeekPath.PathStatus == SeekPath.PATH_IMPOSSIBLE)
			    {
				    unreachable_table[xOffset + SHIFT_ADJUST, yOffset + SHIFT_ADJUST] = true;
				    analyseResult--;

				    //------------------------------------------------------------//
				    // the nearby location should also be unreachable
				    //------------------------------------------------------------//
				    check_nearby_location(targetXLoc, targetYLoc, xOffset, yOffset, firmWidth, firmHeight,
					    unit.mobile_type, unreachable_table, ref analyseResult);
			    }

			    update_unreachable_table(targetXLoc, targetYLoc, firmWidth, firmHeight, unit.mobile_type,
				    ref analyseResult, unreachable_table);
		    }
	    }

	    //---------------------------------------------------------------------//
	    // set the unreachable flag for each units
	    //---------------------------------------------------------------------//
	    //-************** codes here ***************-//
    }

    public void attack_town(int targetXLoc, int targetYLoc, int townRecno, List<int> selectedUnits)
    {
	    if (selectedUnits.Count == 1)
	    {
		    Unit selectedUnit = this[selectedUnits[0]];
		    selectedUnit.unit_group_id = cur_group_id++;
		    selectedUnit.attack_town(targetXLoc, targetYLoc);
		    return;
	    }

	    //********************** improve later begin **************************//
	    //---------------------------------------------------------------------//
	    // codes for differnt territory or different mobile_type attacking should
	    // be added in the future.
	    //---------------------------------------------------------------------//
	    Unit firstUnit = this[selectedUnits[0]];
	    if (World.get_loc(targetXLoc, targetYLoc).region_id !=
	        World.get_loc(firstUnit.next_x_loc(), firstUnit.next_y_loc()).region_id)
	    {
		    set_group_id(selectedUnits);

		    for (int i = 0; i < selectedUnits.Count; i++)
		    {
			    Unit selectedUnit = this[selectedUnits[i]];
			    selectedUnit.attack_town(targetXLoc, targetYLoc);
		    }

		    return;
	    }
	    //*********************** improve later end ***************************//

	    //----------- initialize local parameters ------------//
	    int targetXLoc2 = targetXLoc + InternalConstants.TOWN_WIDTH - 1; // the lower right corner of the firm
	    int targetYLoc2 = targetYLoc + InternalConstants.TOWN_HEIGHT - 1;

	    //---------------------------------------------------------------------//
	    // construct data structure
	    //---------------------------------------------------------------------//
	    int[][] dir_array_ptr = new int[UnitConstants.ATTACK_DIR][];
	    int[] dir_array_count = new int[UnitConstants.ATTACK_DIR];
	    int[] unit_processed_array = new int[selectedUnits.Count];
	    int unit_processed_count = 0;

	    for (int count = 0; count < UnitConstants.ATTACK_DIR; count++)
	    {
		    dir_array_ptr[count] = new int[selectedUnits.Count];
	    }

	    //---------------------------------------------------------------------//
	    // divide the units into each region
	    //---------------------------------------------------------------------//
	    Unit unit = this[selectedUnits[0]];
	    int groupId = cur_group_id++;
	    arrange_units_in_group(targetXLoc, targetYLoc, targetXLoc2, targetYLoc2,
		    selectedUnits, groupId, 3, dir_array_ptr, dir_array_count);

	    //---------------------------------------------------------------------//
	    // now the attackers are divided into 8 groups to attack the target
	    //---------------------------------------------------------------------//
	    int xOffset, yOffset; // offset location of the target
	    int unprocessed; // number of units in this group
	    int dist, xDist, yDist;
	    int destCount;
	    int unitPos = 0; // store the position of the unit with minDist
	    int surroundLoc = get_target_surround_loc(InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT);

	    bool[,] unreachable_table = new bool[MAX_UNIT_SURROUND_SIZE, MAX_UNIT_SURROUND_SIZE];

	    //---------------------------------------------------------------------//
	    // analyse the surrounding location of the target
	    //---------------------------------------------------------------------//
	    int analyseResult = analyse_surround_location(targetXLoc, targetYLoc,
		    InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, unit.mobile_type, unreachable_table);

	    if (analyseResult == 0) // 0 if all surround location is not accessible
	    {
		    //------------------------------------------------------------//
		    // special handling for this case
		    //------------------------------------------------------------//
		    handle_attack_target_totally_blocked(targetXLoc, targetYLoc, townRecno, selectedUnits, 3,
			    unit_processed_array, unit_processed_count);

		    return;
	    }

	    //---------------------------------------------------------------------//
	    // let the units move to the rest accessible location
	    //---------------------------------------------------------------------//
	    for (int count = 0; count < UnitConstants.ATTACK_DIR; count++) // for each array/group
	    {
		    //--------- initialize for each group --------//
		    unprocessed = dir_array_count[count]; // get the number of units in this region
		    int[] curArray = dir_array_ptr[count]; // get the recno of units in this region
		    destCount = surroundLoc - 1;
		    int[] xOffsetPtr = UnitAttackHelper.get_target_x_offset(
			    InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, count);
		    int[] yOffsetPtr = UnitAttackHelper.get_target_y_offset(
			    InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, count);
		    int xOffsetPtrIndex = 0;
		    int yOffsetPtrIndex = 0;
		    xOffset = xOffsetPtr[xOffsetPtrIndex];
		    yOffset = yOffsetPtr[yOffsetPtrIndex];

		    //-----------------------------------------------------------------//
		    // determine a suitable location for the attacker to move to
		    //-----------------------------------------------------------------//
		    while (unprocessed > 0)
		    {
			    //-----------------------------------------------------//
			    // find a reachable location, or not searched location
			    //-----------------------------------------------------//
			    if (analyseResult == 0)
			    {
				    handle_attack_target_totally_blocked(targetXLoc, targetYLoc, townRecno, selectedUnits, 3,
					    unit_processed_array, unit_processed_count);

				    return;
			    }
			    else
			    {
				    for (int i = 0; i < surroundLoc; i++)
				    {
					    if ((++destCount) >= surroundLoc)
					    {
						    destCount = 0;
						    xOffsetPtr = UnitAttackHelper.get_target_x_offset(InternalConstants.TOWN_WIDTH,
							    InternalConstants.TOWN_HEIGHT, count);
						    yOffsetPtr = UnitAttackHelper.get_target_y_offset(InternalConstants.TOWN_WIDTH,
							    InternalConstants.TOWN_HEIGHT, count);
						    xOffsetPtrIndex = 0;
						    yOffsetPtrIndex = 0;
						    xOffset = xOffsetPtr[xOffsetPtrIndex];
						    yOffset = yOffsetPtr[yOffsetPtrIndex];
					    }
					    else
					    {
						    xOffsetPtrIndex++;
						    yOffsetPtrIndex++;
						    xOffset = xOffsetPtr[xOffsetPtrIndex];
						    yOffset = yOffsetPtr[yOffsetPtrIndex];
					    }

					    if (!unreachable_table[xOffset + SHIFT_ADJUST, yOffset + SHIFT_ADJUST])
						    break;
				    }
			    }

			    //------------------------------------------------------------//
			    // find the closest attacker
			    //------------------------------------------------------------//
			    // actual target surrounding location to move to
			    int xLoc = targetXLoc + xOffset;
			    int yLoc = targetYLoc + yOffset;

			    int minDist = Int32.MaxValue;
			    for (int i = 0; i < unprocessed; i++)
			    {
				    unit = this[curArray[i]];
				    xDist = Math.Abs(xLoc - unit.next_x_loc());
				    yDist = Math.Abs(yLoc - unit.next_y_loc());
				    dist = (xDist >= yDist) ? xDist * 10 + yDist : yDist * 10 + xDist;

				    if (dist < minDist)
				    {
					    minDist = dist;
					    unitPos = i;
				    }
			    }

			    unit = this[curArray[unitPos]];
			    curArray[unitPos] = curArray[--unprocessed]; // move the units in the back to the front

			    //TODO remove
			    //SeekPath.set_status(SeekPath.PATH_WAIT);
			    unit.attack_town(targetXLoc, targetYLoc, xOffset, yOffset);

			    //------------------------------------------------------------//
			    // set the flag if unreachable
			    //------------------------------------------------------------//
			    if (SeekPath.PathStatus == SeekPath.PATH_IMPOSSIBLE)
			    {
				    unreachable_table[xOffset + SHIFT_ADJUST, yOffset + SHIFT_ADJUST] = true;
				    analyseResult--;

				    //------------------------------------------------------------//
				    // the nearby location should also be unreachable
				    //------------------------------------------------------------//
				    check_nearby_location(targetXLoc, targetYLoc, xOffset, yOffset, InternalConstants.TOWN_WIDTH,
					    InternalConstants.TOWN_HEIGHT, unit.mobile_type, unreachable_table, ref analyseResult);
			    }

			    update_unreachable_table(targetXLoc, targetYLoc, InternalConstants.TOWN_WIDTH,
				    InternalConstants.TOWN_HEIGHT, unit.mobile_type, ref analyseResult, unreachable_table);
		    }
	    }

	    //---------------------------------------------------------------------//
	    // set the unreachable flag for each units
	    //---------------------------------------------------------------------//
	    //-************** codes here ***************-//
    }

    public void attack_wall(int targetXLoc, int targetYLoc, List<int> selectedUnits)
    {
	    if (selectedUnits.Count == 1)
	    {
		    Unit selectedUnit = this[selectedUnits[0]];
		    selectedUnit.unit_group_id = cur_group_id++;
		    selectedUnit.attack_wall(targetXLoc, targetYLoc);
		    return;
	    }

	    //********************** improve later begin **************************//
	    //---------------------------------------------------------------------//
	    // codes for differnt territory or different mobile_type attacking should
	    // be added in the future.
	    //---------------------------------------------------------------------//
	    Unit firstUnit = this[selectedUnits[0]];
	    if (World.get_loc(targetXLoc, targetYLoc).region_id !=
	        World.get_loc(firstUnit.next_x_loc(), firstUnit.next_y_loc()).region_id)
	    {
		    set_group_id(selectedUnits);

		    for (int i = 0; i < selectedUnits.Count; i++)
		    {
			    Unit selectedUnit = this[selectedUnits[i]];
			    selectedUnit.attack_wall(targetXLoc, targetYLoc);
		    }

		    return;
	    }
	    //*********************** improve later end ***************************//

	    //----------- initialize local parameters ------------//

	    //---------------------------------------------------------------------//
	    // construct data structure
	    //---------------------------------------------------------------------//
	    int[][] dir_array_ptr = new int[UnitConstants.ATTACK_DIR][];
	    int[] dir_array_count = new int[UnitConstants.ATTACK_DIR];
	    int[] unit_processed_array = new int[selectedUnits.Count];
	    int unit_processed_count = 0;

	    for (int count = 0; count < UnitConstants.ATTACK_DIR; count++)
	    {
		    dir_array_ptr[count] = new int[selectedUnits.Count];
	    }

	    //---------------------------------------------------------------------//
	    // divide the units into each region
	    //---------------------------------------------------------------------//
	    Unit unit = this[selectedUnits[0]];
	    int groupId = cur_group_id++;
	    arrange_units_in_group(targetXLoc, targetYLoc, targetXLoc, targetYLoc,
		    selectedUnits, groupId, 0, dir_array_ptr, dir_array_count);

	    //---------------------------------------------------------------------//
	    // now the attackers are divided into 8 groups to attack the target
	    //---------------------------------------------------------------------//
	    int xOffset, yOffset; // offset location of the target
	    int unprocessed; // number of units in this group
	    int dist, xDist, yDist;
	    int destCount;
	    int unitPos = 0; // store the position of the unit with minDist
	    int surroundLoc = get_target_surround_loc(1, 1);

	    bool[,] unreachable_table = new bool[MAX_UNIT_SURROUND_SIZE, MAX_UNIT_SURROUND_SIZE];

	    //---------------------------------------------------------------------//
	    // analyse the surrounding location of the target
	    //---------------------------------------------------------------------//
	    int analyseResult = analyse_surround_location(targetXLoc, targetYLoc, 1, 1,
		    unit.mobile_type, unreachable_table);

	    if (analyseResult == 0) // 0 if all surround location is not accessible
	    {
		    //------------------------------------------------------------//
		    // special handling for this case
		    //------------------------------------------------------------//
		    handle_attack_target_totally_blocked(targetXLoc, targetYLoc, 0, selectedUnits, 0,
			    unit_processed_array, unit_processed_count);

		    return;
	    }

	    //---------------------------------------------------------------------//
	    // let the units move to the rest accessible location
	    //---------------------------------------------------------------------//
	    for (int count = 0; count < UnitConstants.ATTACK_DIR; count++) // for each array/group
	    {
		    //--------- initialize for each group --------//
		    unprocessed = dir_array_count[count]; // get the number of units in this region
		    int[] curArray = dir_array_ptr[count]; // get the recno of units in this region
		    destCount = surroundLoc - 1;
		    int[] xOffsetPtr = UnitAttackHelper.get_target_x_offset(1, 1, count);
		    int[] yOffsetPtr = UnitAttackHelper.get_target_y_offset(1, 1, count);
		    int xOffsetPtrIndex = 0;
		    int yOffsetPtrIndex = 0;
		    xOffset = xOffsetPtr[xOffsetPtrIndex];
		    yOffset = yOffsetPtr[yOffsetPtrIndex];

		    //-----------------------------------------------------------------//
		    // determine a suitable location for the attacker to move to
		    //-----------------------------------------------------------------//
		    while (unprocessed > 0)
		    {
			    //-----------------------------------------------------//
			    // find a reachable location, or not searched location
			    //-----------------------------------------------------//
			    if (analyseResult == 0)
			    {
				    handle_attack_target_totally_blocked(targetXLoc, targetYLoc, 0, selectedUnits, 0,
					    unit_processed_array, unit_processed_count);

				    return;
			    }
			    else
			    {
				    for (int i = 0; i < surroundLoc; i++)
				    {
					    if ((++destCount) >= surroundLoc)
					    {
						    destCount = 0;
						    xOffsetPtr = UnitAttackHelper.get_target_x_offset(1, 1, count);
						    yOffsetPtr = UnitAttackHelper.get_target_y_offset(1, 1, count);
						    xOffsetPtrIndex = 0;
						    yOffsetPtrIndex = 0;
						    xOffset = xOffsetPtr[xOffsetPtrIndex];
						    yOffset = yOffsetPtr[yOffsetPtrIndex];
					    }
					    else
					    {
						    xOffsetPtrIndex++;
						    yOffsetPtrIndex++;
						    xOffset = xOffsetPtr[xOffsetPtrIndex];
						    yOffset = yOffsetPtr[yOffsetPtrIndex];
					    }

					    if (!unreachable_table[xOffset + SHIFT_ADJUST, yOffset + SHIFT_ADJUST])
						    break;
				    }
			    }

			    //------------------------------------------------------------//
			    // find the closest attacker
			    //------------------------------------------------------------//
			    // actual target surrounding location to move to
			    int xLoc = targetXLoc + xOffset;
			    int yLoc = targetYLoc + yOffset;

			    int minDist = Int32.MaxValue;
			    for (int i = 0; i < unprocessed; i++)
			    {
				    unit = this[curArray[i]];
				    xDist = Math.Abs(xLoc - unit.next_x_loc());
				    yDist = Math.Abs(yLoc - unit.next_y_loc());
				    dist = (xDist >= yDist) ? xDist * 10 + yDist : yDist * 10 + xDist;

				    if (dist < minDist)
				    {
					    minDist = dist;
					    unitPos = i;
				    }
			    }

			    unit = this[curArray[unitPos]];
			    curArray[unitPos] = curArray[--unprocessed]; // move the units in the back to the front

			    //TODO remove
			    //SeekPath.set_status(SeekPath.PATH_WAIT);
			    unit.attack_wall(targetXLoc, targetYLoc, xOffset, yOffset);

			    //------------------------------------------------------------//
			    // set the flag if unreachable
			    //------------------------------------------------------------//
			    if (SeekPath.PathStatus == SeekPath.PATH_IMPOSSIBLE)
			    {
				    unreachable_table[xOffset + SHIFT_ADJUST, yOffset + SHIFT_ADJUST] = true;
				    analyseResult--;
				    //------------------------------------------------------------//
				    // the nearby location should also be unreachable
				    //------------------------------------------------------------//
				    check_nearby_location(targetXLoc, targetYLoc, xOffset, yOffset, 1, 1,
					    unit.mobile_type, unreachable_table, ref analyseResult);
			    }

			    update_unreachable_table(targetXLoc, targetYLoc, 1, 1, unit.mobile_type,
				    ref analyseResult, unreachable_table);
		    }
	    }

	    //---------------------------------------------------------------------//
	    // set the unreachable flag for each units
	    //---------------------------------------------------------------------//
	    //-************** codes here ***************-//
    }

    //---------- other actions functions -----------//
    public void assign(int destX, int destY, bool divided, int remoteAction, List<int> selectedUnits)
    {
	    //--- set the destination to the top left position of the town/firm ---//

	    Location location = World.get_loc(destX, destY);

	    //---- if there is a firm on this location -----//

	    if (location.is_firm())
	    {
		    Firm firm = FirmArray[location.firm_recno()];

		    destX = firm.loc_x1;
		    destY = firm.loc_y1;
	    }

	    //---- if there is a town on this location -----//

	    else if (location.is_town())
	    {
		    Town town = TownArray[location.town_recno()];

		    destX = town.LocX1;
		    destY = town.LocY1;
	    }

	    //-------------------------------------------//

	    //TODO remove null value
	    if (selectedUnits == null)
	    {
		    selectedUnits = new List<int>();

		    // find myself
		    foreach (Unit unit in this)
		    {
			    if (!unit.is_visible())
				    continue;

			    if (unit.selected_flag && unit.is_own())
			    {
				    selectedUnits.Add(unit.sprite_recno);
			    }
		    }
	    }

	    /*if (!remoteAction && remote.is_enable())
	    {
		    // packet structure : <xLoc> <yLoc> <no. of units> <unit recno ...>
		    short* shortPtr = (short*)remote.new_send_queue_msg(MSG_UNIT_ASSIGN,
			    sizeof(short) * (4 + selectedCount));
		    shortPtr[0] = destX;
		    shortPtr[1] = destY;
		    shortPtr[2] = selectedCount;
		    shortPtr[3] = divided;
		    memcpy(shortPtr + 4, selectedArray, sizeof(short) * selectedCount);
	    }*/
	    //else
	    //{
		    if (!divided)
		    {
			    for (int j = 0; j < selectedUnits.Count; ++j)
			    {
				    int unitRecNo = selectedUnits[j];

				    if (IsDeleted(unitRecNo))
					    continue;

				    Unit unit = this[unitRecNo]; //unit_array[i];
				    unit.stop2();
			    }

			    //--------- divide the unit by their mobile_type ------------//
			    divide_array(destX, destY, selectedUnits);

			    if (selected_land_units.Count > 0)
				    assign(destX, destY, true, remoteAction, selected_land_units);

			    if (selected_sea_units.Count > 0)
			    {
				    Location loc = World.get_loc(destX, destY);
				    if (loc.is_firm())
				    {
					    Firm firm = FirmArray[loc.firm_recno()];
					    if (firm.firm_id == Firm.FIRM_HARBOR) // recursive call
						    assign(destX, destY, true, remoteAction, selected_sea_units);
					    else
						    ship_to_beach(destX, destY, true, selected_sea_units, remoteAction);
				    }
				    //else if(loc.is_town())
				    else
				    {
					    ship_to_beach(destX, destY, true, selected_sea_units, remoteAction);
				    }
			    }

			    if (selected_air_units.Count > 0) // no assign for air units
				    move_to(destX, destY, true, selected_air_units, remoteAction);

			    //------------ deinit static variables ------------//
			    selected_land_units.Clear();
			    selected_sea_units.Clear();
			    selected_air_units.Clear();
		    }
		    else
		    {
			    //---------- set unit to assign -----------//
			    if (selectedUnits.Count == 1)
			    {
				    Unit unit = this[selectedUnits[0]];

				    if (unit.sprite_info.loc_width <= 1)
				    {
					    unit.unit_group_id = cur_group_id++;
					    unit.assign(destX, destY);
				    }
				    else // move to object surrounding
				    {
					    Location loc = World.get_loc(destX, destY);
					    if (loc.is_firm())
						    unit.move_to_firm_surround(destX, destY, unit.sprite_info.loc_width,
							    unit.sprite_info.loc_height, loc.firm_recno());
					    else if (loc.is_town())
						    unit.move_to_town_surround(destX, destY, unit.sprite_info.loc_width,
							    unit.sprite_info.loc_height);
					    else if (loc.has_unit(UnitConstants.UNIT_LAND))
						    unit.move_to_unit_surround(destX, destY, unit.sprite_info.loc_width,
							    unit.sprite_info.loc_height, loc.unit_recno(UnitConstants.UNIT_LAND));
				    }
			    }
			    else // for more than one unit selecting, call group_assign() to take care of it
			    {
				    set_group_id(selectedUnits);
				    group_assign(destX, destY, selectedUnits);
			    }
		    }
	    //}
    }

    public void assign_to_camp(int destX, int destY, int remoteAction, List<int> selectedUnits)
    {
	    divide_array(destX, destY, selectedUnits);

	    if (selected_land_units.Count > 0)
	    {
		    List<int> assignArray = new List<int>();
		    List<int> moveArray = new List<int>();

		    //----------------------------------------------------------------//
		    // Only human and weapon can be assigned to the camp. Others are
		    // ordered to move to camp as close as possible.
		    //----------------------------------------------------------------//
		    for (int i = 0; i < selectedUnits.Count; i++)
		    {
			    Unit unit = this[selectedUnits[i]];
			    int unitClass = UnitRes[unit.unit_id].unit_class;
			    if (unitClass == UnitConstants.UNIT_CLASS_HUMAN || unitClass == UnitConstants.UNIT_CLASS_WEAPON)
				    assignArray.Add(selectedUnits[i]);
			    else
				    moveArray.Add(selectedUnits[i]);
		    }

		    if (assignArray.Count > 0)
			    assign(destX, destY, true, remoteAction, assignArray);
		    if (moveArray.Count > 0)
			    move_to(destX, destY, true, moveArray, remoteAction);
	    }

	    if (selected_sea_units.Count > 0)
		    ship_to_beach(destX, destY, true, selected_sea_units, remoteAction);

	    if (selected_air_units.Count > 0)
		    move_to(destX, destY, true, selected_air_units, remoteAction);

	    //---------------- deinit static parameters ---------------//
	    selected_land_units.Clear();
	    selected_sea_units.Clear();
	    selected_air_units.Clear();
    }

    public void settle(int destX, int destY, bool divided, int remoteAction, List<int> selectedUnits)
    {
	    //TODO remove null value
	    if (selectedUnits == null)
	    {
		    selectedUnits = new List<int>();

		    // find myself
		    foreach (Unit unit in this)
		    {
			    if (!unit.is_visible())
				    continue;

			    if (unit.selected_flag && unit.is_own())
			    {
				    selectedUnits.Add(unit.sprite_recno);
			    }
		    }
	    }

	    /*if (!remoteAction && remote.is_enable())
	    {
		    // packet structure : <xLoc> <yLoc> <no. of units> <divided> <unit recno ...>
		    short* shortPtr = (short*)remote.new_send_queue_msg(MSG_UNITS_SETTLE, sizeof(short) * (4 + selectedCount));
		    shortPtr[0] = destX;
		    shortPtr[1] = destY;
		    shortPtr[2] = selectedCount;
		    shortPtr[3] = divided;
		    memcpy(shortPtr + 4, selectedArray, sizeof(short) * selectedCount);
	    }*/
	    //else
	    //{
		    if (!divided)
		    {
			    for (int j = 0; j < selectedUnits.Count; ++j)
			    {
				    int unitRecNo = selectedUnits[j];

				    if (IsDeleted(unitRecNo))
					    continue;

				    Unit unit = this[unitRecNo]; //unit_array[i];
				    unit.stop2();
			    }

			    divide_array(destX, destY, selectedUnits);

			    if (selected_land_units.Count > 0)
				    settle(destX, destY, true, remoteAction, selected_land_units);

			    if (selected_sea_units.Count > 0)
				    ship_to_beach(destX, destY, true, selected_sea_units, remoteAction);

			    if (selected_air_units.Count > 0)
				    move_to(destX, destY, true, selected_air_units, remoteAction);

			    //-------------- deinit static parameters --------------//
			    selected_land_units.Clear();
			    selected_sea_units.Clear();
			    selected_air_units.Clear();
		    }
		    else
		    {
			    //---------- set unit to settle -----------//
			    if (selectedUnits.Count == 1)
			    {
				    Unit unit = this[selectedUnits[0]];
				    unit.unit_group_id = cur_group_id++;
				    unit.settle(destX, destY);
			    }
			    else
			    {
				    set_group_id(selectedUnits);
				    group_settle(destX, destY, selectedUnits);
			    }
		    }
	    //}
    }

    public void assign_to_ship(int shipXLoc, int shipYLoc, bool divided, List<int> selectedUnits,
	    int remoteAction, int shipRecno)
    {
	    /*if (!remoteAction && remote.is_enable())
	    {
		    // packet structure : <xLoc> <yLoc> <ship recno> <no. of units> <divided> <unit recno ...>
		    short* shortPtr = (short*)remote.new_send_queue_msg(MSG_UNITS_ASSIGN_TO_SHIP,
			    sizeof(short) * (5 + selectedCount));
		    shortPtr[0] = shipXLoc;
		    shortPtr[1] = shipYLoc;
		    shortPtr[2] = shipRecno;
		    shortPtr[3] = selectedCount;
		    shortPtr[4] = divided;
		    memcpy(shortPtr + 5, selectedArray, sizeof(short) * selectedCount);

		    return;
	    }*/

	    if (!divided)
	    {
		    divide_array(shipXLoc, shipYLoc, selectedUnits);

		    if (selected_sea_units.Count > 0) // Note: the order to call ship unit first
			    move_to(shipXLoc, shipYLoc, true, selected_sea_units, remoteAction);

		    if (selected_land_units.Count > 0)
			    assign_to_ship(shipXLoc, shipYLoc, true, selected_land_units, remoteAction, shipRecno);

		    if (selected_air_units.Count > 0)
			    move_to(shipXLoc, shipYLoc, true, selected_air_units, remoteAction);

		    //---------------- deinit static parameters -----------------//
		    selected_sea_units.Clear();
		    selected_land_units.Clear();
		    selected_air_units.Clear();
		    return;
	    }
	    else
	    {
		    // ----- dont not use shipXLoc, shipYLoc passed, use shipRecno --------//

		    UnitMarine ship = (UnitMarine)this[shipRecno];

		    if (ship != null && ship.is_visible())
		    {
			    shipXLoc = ship.next_x_loc();
			    shipYLoc = ship.next_y_loc();
		    }
		    else
			    return;

		    //------------------------------------------------------------------------------------//
		    // find the closest unit to the ship
		    //------------------------------------------------------------------------------------//
		    int minDist = Int32.MaxValue;
		    int closestUnitRecno = -1;
		    for (int i = 0; i < selectedUnits.Count; i++)
		    {
			    Unit unit = this[selectedUnits[i]];
			    int distX = Math.Abs(shipXLoc - unit.next_x_loc());
			    int distY = Math.Abs(shipYLoc - unit.next_y_loc());
			    int dist = (distX > distY) ? distX : distY;
			    if (dist < minDist)
			    {
				    minDist = dist;
				    closestUnitRecno = i;
			    }
		    }

		    //------------------------------------------------------------------------------------//
		    // If the seleceted units are distributed on different territories, select those unit
		    // on the same territory as the closet unit for processing, there will be no action for
		    // the rest units.
		    //------------------------------------------------------------------------------------//
		    int curGroupId = cur_group_id++;
		    Unit closestUnit = this[selectedUnits[closestUnitRecno]];
		    int closestUnitXLoc = closestUnit.next_x_loc();
		    int closestUnitYLoc = closestUnit.next_y_loc();
		    int defaultRegionId = World.get_loc(closestUnitXLoc, closestUnitYLoc).region_id;
		    List<int> newSelectedArray = new List<int>();

		    if (selectedUnits.Count > 1)
		    {
			    for (int i = 0; i < selectedUnits.Count; i++)
			    {
				    Unit unit = this[selectedUnits[i]];
				    if (World.get_loc(unit.next_x_loc(), unit.next_y_loc()).region_id == defaultRegionId)
				    {
					    newSelectedArray.Add(selectedUnits[i]); // on the same territory
					    unit.unit_group_id = curGroupId;
				    }

				    if (unit.cur_action == Sprite.SPRITE_IDLE)
					    unit.set_ready();
			    }
		    }
		    else
		    {
			    newSelectedArray.Add(selectedUnits[0]);
			    closestUnit.unit_group_id = curGroupId;
		    }

		    //-------------- ordering the ship move near the coast ----------------//
		    int curXLoc = closestUnit.next_x_loc();
		    int curYLoc = closestUnit.next_y_loc();

		    // ##### patch begin Gilbert 5/8 #######//
		    // UnitMarine *ship = (UnitMarine*) get_ptr(world.get_unit_recno(shipXLoc, shipYLoc, UnitConstants.UNIT_SEA));
		    // ##### patch end Gilbert 5/8 #######//

		    int landX, landY;
		    ship.unit_group_id = curGroupId;
		    ship.ship_to_beach(curXLoc, curYLoc, out landX, out landY);

		    if (landX != -1 && landY != -1)
		    {
			    //-------------- ordering the units ---------------//
			    const int TRY_SIZE = 5;
			    int countLimit = TRY_SIZE * TRY_SIZE;
			    int regionId = World.get_loc(landX, landY).region_id;
			    for (int i = 0, k = 0; i < newSelectedArray.Count; i++)
			    {
				    for (int j = 1; j < countLimit; j++)
				    {
					    if (++k > countLimit)
						    k = 1;

					    int xShift, yShift;
					    Misc.cal_move_around_a_point(k, TRY_SIZE, TRY_SIZE, out xShift, out yShift);
					    int checkXLoc = landX + xShift;
					    int checkYLoc = landY + yShift;
					    if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
						    continue;

					    Location loc = World.get_loc(checkXLoc, checkYLoc);
					    if (loc.region_id != regionId || !loc.walkable())
						    continue;

					    Unit unit = this[newSelectedArray[i]];
					    unit.assign_to_ship(landX, landY, ship.sprite_recno, k);
					    break;
				    }
			    }
		    }
	    }
    }

    public void ship_to_beach(int destX, int destY, bool divided, List<int> selectedUnits, int remoteAction)
    {
	    /*if (!remoteAction && remote.is_enable())
	    {
		    // packet structure : <xLoc> <yLoc> <no. of units> <divided> <unit recno ...>
		    short* shortPtr = (short*)remote.new_send_queue_msg(MSG_UNITS_SHIP_TO_BEACH,
			    sizeof(short) * (4 + selectedCount));
		    shortPtr[0] = destX;
		    shortPtr[1] = destY;
		    shortPtr[2] = selectedCount;
		    shortPtr[3] = divided;
		    memcpy(shortPtr + 4, selectedArray, sizeof(short) * selectedCount);

		    return;
	    }*/

	    set_group_id(selectedUnits);

	    //--------------------------------------------------------------------//
	    //--------------------------------------------------------------------//
	    const int CHECK_SEA_DIMENSION = 50;
	    const int CHECK_SEA_SIZE = CHECK_SEA_DIMENSION * CHECK_SEA_DIMENSION;
	    Location loc = World.get_loc(destX, destY);
	    int regionId = loc.region_id;
	    int xShift, yShift, checkXLoc, checkYLoc;
	    int landX = -1, landY = -1, tempX, tempY;

	    int i = 0, j = 1, k, found;

	    //--------------------------------------------------------------------//
	    // find a unit that can carrying units.  Let it to do the first searching.
	    // Use the returned reference parameters (landX, landY) for the other
	    // ships to calculate their final location to move to
	    //--------------------------------------------------------------------//
	    for (; i < selectedUnits.Count; i++) // for first unit
	    {
		    Unit unit = this[selectedUnits[i]];
		    if (UnitRes[unit.unit_id].carry_unit_capacity > 0)
		    {
			    // landX=landY=-1 if calling move_to() instead
			    unit.ship_to_beach(destX, destY, out landX, out landY);
			    i++;
			    break;
		    }
		    else
		    {
			    unit.move_to(destX, destY, 1);
		    }
	    }

	    int totalCheck = 0;
	    for (; i < selectedUnits.Count; i++) // for the rest units
	    {
		    Unit unit = this[selectedUnits[i]];
		    if (UnitRes[unit.unit_id].carry_unit_capacity > 0 && landX != -1 && landY != -1)
		    {
			    for (found = 0; j <= CHECK_SEA_SIZE; j++, totalCheck++)
			    {
				    Misc.cal_move_around_a_point(j, CHECK_SEA_DIMENSION, CHECK_SEA_DIMENSION,
					    out xShift, out yShift);

				    if (j >= CHECK_SEA_SIZE)
					    j = 1;

				    if (totalCheck == CHECK_SEA_SIZE)
				    {
					    //--------------------------------------------------------------------//
					    // can't handle this case
					    //--------------------------------------------------------------------//
					    unit.ship_to_beach(landX, landY, out tempX, out tempY);
					    totalCheck = 0;
					    break;
				    }

				    int seaX = landX + xShift;
				    int seaY = landY + yShift;
				    if (seaX < 0 || seaX >= GameConstants.MapSize || seaY < 0 || seaY >= GameConstants.MapSize)
					    continue;

				    loc = World.get_loc(seaX, seaY);
				    if (TerrainRes[loc.terrain_id].average_type != TerrainTypeCode.TERRAIN_OCEAN)
					    continue;

				    //--------------------------------------------------------------------//
				    // if it is able to find a location around the surrounding location with
				    // same region id we prefer, order the unit to move there.
				    //--------------------------------------------------------------------//
				    for (k = 2; k <= 9; k++)
				    {
					    Misc.cal_move_around_a_point(k, 3, 3, out xShift, out yShift);
					    checkXLoc = seaX + xShift;
					    checkYLoc = seaY + yShift;
					    if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
						    continue;

					    loc = World.get_loc(checkXLoc, checkYLoc);
					    if (loc.region_id != regionId)
						    continue;

					    unit.ship_to_beach(checkXLoc, checkYLoc, out tempX, out tempY);
					    found++;
					    break;
				    }

				    if (found != 0)
				    {
					    totalCheck = 0;
					    break;
				    }
			    }
		    }
		    else // cannot carry units
			    unit.move_to(destX, destY, 1);
	    }
    }

    public void AddWayPoint(int pointX, int pointY, List<int> selectedUnits, int remoteAction)
    {
	    /*if (!remoteAction && remote.is_enable())
	    {
		    // packet structure : <xLoc> <yLoc> <no. of units> <unit recno ...>
		    short* shortPtr = (short*)remote.new_send_queue_msg(MSG_UNIT_ADD_WAY_POINT,
			    sizeof(short) * (3 + selectedCount));
		    shortPtr[0] = pointX;
		    shortPtr[1] = pointY;
		    shortPtr[2] = selectedCount;
		    memcpy(shortPtr + 3, selectedArray, sizeof(short) * selectedCount);
	    }*/
	    //else
	    //{
		    int groupId = cur_group_id;

		    for (int i = 0; i < selectedUnits.Count; ++i)
		    {
			    Unit unit = this[selectedUnits[i]];
			    unit.unit_group_id = groupId;
		    }

		    cur_group_id++;

		    for (int i = 0; i < selectedUnits.Count; ++i)
		    {
			    Unit unit = this[selectedUnits[i]];
			    unit.AddWayPoint(pointX, pointY);
		    }
	    //}
    }

    //--------- unit filter function ----------//
    public int divide_attack_by_nation(int nationRecno, List<int> selectedUnits)
    {
	    // elements before i are pass, elements on or after passCount are not pass
	    int passCount = selectedUnits.Count;
	    for (int i = 0; i < passCount;)
	    {
		    int unitRecno = selectedUnits[i];

		    // a clocked spy cannot be commanded by original nation to attack
		    if (!IsDeleted(unitRecno) && this[unitRecno].nation_recno == nationRecno)
		    {
			    // pass
			    ++i;
		    }
		    else
		    {
			    // fail, swap [i] with [passCount-1]
			    --passCount;

			    selectedUnits[i] = selectedUnits[passCount];
			    selectedUnits[passCount] = unitRecno;
		    }
	    }

	    return passCount;
    }

    public void disp_next(int seekDir, bool sameNation)
    {
	    if (selected_recno == 0)
		    return;

	    Unit selectedUnit = this[selected_recno];
	    int unitClass = UnitRes[selectedUnit.unit_id].unit_class;
	    int nationRecno = selectedUnit.nation_recno;
	    var enumerator = (seekDir >= 0) ? EnumerateAll(selected_recno, true) : EnumerateAll(selected_recno, false);

	    foreach (int recNo in enumerator)
	    {
		    Unit unit = this[recNo];

		    if (!unit.is_visible())
			    continue;

		    //--- check if the location of the unit has been explored ---//

		    if (!World.get_loc(unit.next_x_loc(), unit.next_y_loc()).explored())
			    continue;

		    //-------- if are of the same nation --------//

		    if (sameNation && unit.nation_recno != nationRecno)
			    continue;

		    //---------------------------------//

		    if (UnitRes[unit.unit_id].unit_class == unitClass)
		    {
			    Power.reset_selection();
			    unit.selected_flag = true;
			    selected_recno = unit.sprite_recno;
			    selected_count++;

			    World.go_loc(unit.cur_x_loc(), unit.cur_y_loc());
			    return;
		    }
	    }
    }

    private void divide_array(int locX, int locY, List<int> selectedUnits, int excludeSelectedLocUnit = 0)
	{
		selected_land_units.Clear();
		selected_sea_units.Clear();
		selected_air_units.Clear();

		int excludeUnitRecno = 0;
		if (excludeSelectedLocUnit != 0)
		{
			Location location = World.get_loc(locX, locY);
			int targetMobileType = location.has_any_unit();
			excludeUnitRecno = targetMobileType != 0 ? location.unit_recno(targetMobileType) : 0;
		}

		for (int i = 0; i < selectedUnits.Count; i++)
		{
			int selectedUnitRecno = selectedUnits[i];
			if (excludeUnitRecno != 0 && selectedUnitRecno == excludeUnitRecno)
				continue;

			Unit unit = this[selectedUnitRecno];
			switch (unit.mobile_type)
			{
				case UnitConstants.UNIT_LAND:
					selected_land_units.Add(selectedUnitRecno);
					break;

				case UnitConstants.UNIT_SEA:
					selected_sea_units.Add(selectedUnitRecno);
					break;

				case UnitConstants.UNIT_AIR:
					selected_air_units.Add(selectedUnitRecno);
					break;
			}
		}
	}

	private void set_group_id(List<int> selectedUnits)
	{
		//-------------------------------------------------//
		// set unit cur_action and unit_group_id
		//-------------------------------------------------//
		int curGroupId = cur_group_id++;

		for (int j = 0; j < selectedUnits.Count; j++)
		{
			Unit unit = this[selectedUnits[j]];

			unit.unit_group_id = curGroupId;

			if (unit.cur_action == Sprite.SPRITE_IDLE) //**maybe need to include SPRITE_ATTACK as well
				unit.set_ready();
		}
	}

	//------------ move sub-functions --------------//
	private void move_to_now_with_filter(int destX, int destY, List<int> selectedUnits)
	{
		int destRegionId = World.get_loc(destX, destY).region_id;
		Unit unit = this[selectedUnits[0]];

		//-------------- no filtering for unit air --------------------------//
		if (unit.mobile_type == UnitConstants.UNIT_AIR)
		{
			move_to_now(destX, destY, selectedUnits);
			return;
		}

		//----------------- init data structure --------------//
		List<int> filtered_unit_array = new List<int>();
		List<int> filtering_unit_array = new List<int>();
		filtering_unit_array.AddRange(selectedUnits);
		int filtering_unit_count = selectedUnits.Count;

		int unprocessCount = selectedUnits.Count;
		int filterRegionId = destRegionId;
		int filterDestX = destX, filterDestY = destY;
		int loopCount, i;

		//-------------------------------------------------------------------------------//
		// group the unit by their region id and process group searching for each group
		//-------------------------------------------------------------------------------//
		// checking for unprocessCount+1, plus one for the case that unit not on the same territory of destination
		for (loopCount = 0; loopCount <= unprocessCount; loopCount++)
		{
			filtered_unit_array.Clear();
			int filteringCount = filtering_unit_array.Count;
			filtering_unit_count = 0;

			//-------------- filter for filterRegionId --------------//
			for (i = 0; i < filteringCount; i++)
			{
				unit = this[filtering_unit_array[i]];
				if (World.get_loc(unit.next_x_loc(), unit.next_y_loc()).region_id == filterRegionId)
					filtered_unit_array.Add(filtering_unit_array[i]);
				else
					filtering_unit_array[filtering_unit_count++] = filtering_unit_array[i];
			}

			//---- process for filtered_unit_array and prepare for looping ----//
			if (filtered_unit_array.Count > 0)
				move_to_now(filterDestX, filterDestY, filtered_unit_array);

			if (filtering_unit_count == 0)
				break;

			//---------------- update parameters for next checking ------------------//
			unit = this[filtering_unit_array[0]];
			filterRegionId = World.get_loc(unit.next_x_loc(), unit.next_y_loc()).region_id;
			filterDestX = destX;
			filterDestY = destY;
			unit.different_territory_destination(ref filterDestX, ref filterDestY);
		}
	}

	private void move_to_now(int destXLoc, int destYLoc, List<int> selectedUnits)
	{
		//------------ define vars -----------------------//
		int unprocessCount; // = selectedCount;		// num. of unprocessed sprite
		int k; // for counting
		int vecX, vecY; // used to reset x, y
		int oddCount, evenCount;
		Unit firstUnit = this[selectedUnits[0]];
		int curGroupId = firstUnit.unit_group_id;
		int mobileType = firstUnit.mobile_type;
		//int		sizeOneSelectedCount=0, sizeTwoSelectedCount=0;
		int sizeOneSelectedCount = selectedUnits.Count;

		//---------- set Unit::unit_group_id and count the unit by size ----------//
		for (int i = 0; i < selectedUnits.Count; i++)
		{
			Unit unit = this[selectedUnits[i]];

			if (unit.cur_action == Sprite.SPRITE_ATTACK)
				unit.stop();

			if (unit.cur_action == Sprite.SPRITE_IDLE)
				unit.set_ready();

			/*switch(unit.sprite_info.loc_width)
			{
				case 1:	sizeOneSelectedCount++;
							break;
	
				case 2:	sizeTwoSelectedCount++;
							break;
	
				default:	err_here();
							break;
			}*/
		}

		unprocessCount = sizeOneSelectedCount;

		//---- construct array to store size one selected unit ----//
		List<int> selectedSizeOneUnitArray = new List<int>();
		if (sizeOneSelectedCount > 0)
		{
			for (int i = 0; i < selectedUnits.Count && unprocessCount > 0; i++)
			{
				Unit unit = this[selectedUnits[i]];
				if (unit.sprite_info.loc_width == 1)
				{
					selectedSizeOneUnitArray.Add(selectedUnits[i]);
					unprocessCount--;
				}
			}
		}

		unprocessCount = sizeOneSelectedCount;

		//----------- variables initialization ---------------//
		int destX, destY, x, y, move_scale;
		if (mobileType == UnitConstants.UNIT_LAND)
		{
			x = destX = destXLoc;
			y = destY = destYLoc;
			move_scale = 1;
		}
		else // UnitConstants.UNIT_AIR, UnitConstants.UNIT_SEA
		{
			x = destX = (destXLoc / 2) * 2;
			y = destY = (destYLoc / 2) * 2;
			move_scale = 2;
		}

		int square_size, not_tested_loc, lower_right_case, upper_left_case;
		int[] distance = new int[sizeOneSelectedCount];
		int[] sorted_distance = new int[sizeOneSelectedCount];
		int[] sorted_member = new int[sizeOneSelectedCount];
		int[] done_flag = new int[sizeOneSelectedCount];
		//if(sizeOneSelectedCount)
		//{
		//----- initialize parameters and construct data structure -----//
		oddCount = 1;
		evenCount = 3;
		square_size = not_tested_loc = lower_right_case = upper_left_case = 0;

		//--- calculate the rectangle size used to allocate space for the sprites----//
		unprocessCount = sizeOneSelectedCount;
		while (unprocessCount > 0)
		{
			//=============================
			// process odd size square
			//=============================
			vecX = (oddCount / 4) * move_scale;
			vecY = vecX;
			k = 0;

			int j;
			for (j = 0; j < oddCount && unprocessCount > 0; j++)
			{
				x = destX + vecX;
				y = destY + vecY;

				if (x >= 0 && y >= 0 && x < GameConstants.MapSize && y < GameConstants.MapSize)
				{
					Location location = World.get_loc(x, y);
					if (location.is_unit_group_accessible(mobileType, curGroupId))
						unprocessCount--;
				}

				if (k++ < oddCount / 2) // reset vecX, vecY
					vecX -= move_scale;
				else
					vecY -= move_scale;
			}

			square_size += move_scale;
			if (j < oddCount)
				not_tested_loc = oddCount - j;
			oddCount += 4;

			if (unprocessCount > 0)
			{
				//=============================
				// process even size square
				//=============================
				vecY = (-(evenCount / 4) - 1) * move_scale;
				vecX = vecY + move_scale;
				k = 0;

				for (j = 0; j < evenCount && unprocessCount > 0; j++)
				{
					x = destX + vecX;
					y = destY + vecY;

					if (x >= 0 && y >= 0 && x < GameConstants.MapSize && y < GameConstants.MapSize)
					{
						Location location = World.get_loc(x, y);
						if (location.is_unit_group_accessible(mobileType, curGroupId))
							unprocessCount--;
					}

					if (k++ < evenCount / 2) // reset vecX, vecY
						vecX += move_scale;
					else
						vecY += move_scale;
				}

				square_size += move_scale;
				if (j < evenCount)
					not_tested_loc = evenCount - j;
				evenCount += 4;
			}
		}

		int rec_height = square_size; // get the height and width of the rectangle
		int rec_width = square_size;
		if (not_tested_loc >= (square_size / move_scale))
			rec_width -= move_scale;

		//--- decide to use upper_left_case or lower_right_case, however, it maybe changed for boundary improvement----//
		x = cal_rectangle_lower_right_x(destX, move_scale, rec_width);
		y = cal_rectangle_lower_right_y(destY, move_scale, rec_height);

		for (int i = 0; i < sizeOneSelectedCount; i++)
		{
			Unit unit = this[selectedSizeOneUnitArray[i]];

			if (unit.next_y_loc() < y) // lower_right_case or upper_left_case
				lower_right_case++;
			else if (unit.next_y_loc() > y)
				upper_left_case++;
		}

		if (lower_right_case == upper_left_case) // in case both values are equal, check by upper_left_case
		{
			x = cal_rectangle_upper_left_x(destX, move_scale, rec_width);
			y = cal_rectangle_upper_left_y(destY, move_scale, rec_height);

			lower_right_case = upper_left_case = 0;
			for (int i = 0; i < sizeOneSelectedCount; i++)
			{
				Unit unit = this[selectedSizeOneUnitArray[i]];

				if (unit.next_y_loc() < y) // lower_right_case or upper_left_case
					lower_right_case++;
				else if (unit.next_y_loc() > y)
					upper_left_case++;
			}
		}

		//------------ determine x, y and lower_right_case/upper_left_case-----------//
		determine_position_to_construct_table(selectedUnits.Count, destX, destY, mobileType,
			ref x, ref y, move_scale, ref rec_width, ref rec_height,
			ref lower_right_case, ref upper_left_case, not_tested_loc, square_size);

		//------------ construct a table to store distance -------//
		// distance and sorted_member should be initialized first
		construct_sorted_array(selectedSizeOneUnitArray, sizeOneSelectedCount, x, y,
			lower_right_case, upper_left_case, distance, sorted_distance, sorted_member, done_flag);

		//------------ process the movement -----------//
		unprocessCount = sizeOneSelectedCount; //selectedCount;
		k = 0;

		//-******************* auto correct ***********************-//
		int autoCorrectStartX = x;
		int autoCorrectStartY = y;
		//-******************* auto correct ***********************-//

		if (lower_right_case >= upper_left_case)
		{
			while (unprocessCount > 0)
			{
				for (int i = x; i > x - rec_width && unprocessCount > 0; i -= move_scale)
				{
					Location location = World.get_loc(i, y);
					if (location.is_unit_group_accessible(mobileType, curGroupId))
					{
						Unit unit;
						do
						{
							unit = this[selectedSizeOneUnitArray[sorted_member[k++]]];
						} while (unit.sprite_info.loc_width > 1);

						if (sizeOneSelectedCount > 1)
						{
							if (unprocessCount == sizeOneSelectedCount) // the first unit to move
							{
								unit.move_to(i, y, 1, SeekPath.SEARCH_MODE_IN_A_GROUP, 0, sizeOneSelectedCount);
								if (unit.mobile_type == UnitConstants.UNIT_LAND && unit.nation_recno != 0)
									unit.select_search_sub_mode(unit.next_x_loc(), unit.next_y_loc(), i, y,
										unit.nation_recno, SeekPath.SEARCH_MODE_IN_A_GROUP);
								unit.move_to(i, y, 1, SeekPath.SEARCH_MODE_IN_A_GROUP, 0, sizeOneSelectedCount);
							}
							else
							{
								if (unit.mobile_type == UnitConstants.UNIT_LAND && unit.nation_recno != 0)
									unit.select_search_sub_mode(unit.next_x_loc(), unit.next_y_loc(), i, y,
										unit.nation_recno, SeekPath.SEARCH_MODE_IN_A_GROUP);
								unit.move_to(i, y, 1, SeekPath.SEARCH_MODE_IN_A_GROUP, 0, sizeOneSelectedCount);
							}
						}
						else
							unit.move_to(i, y, 1);

						unprocessCount--;
					}
				}

				y -= move_scale;
			}
		}
		else // upper_left_case
		{
			while (unprocessCount > 0)
			{
				for (int i = x; i < x + rec_width && unprocessCount > 0; i += move_scale)
				{
					Location location = World.get_loc(i, y);
					if (location.is_unit_group_accessible(mobileType, curGroupId))
					{
						Unit unit;
						do
						{
							unit = this[selectedSizeOneUnitArray[sorted_member[k++]]];
						} while (unit.sprite_info.loc_width > 1);

						if (sizeOneSelectedCount > 1)
						{
							if (unprocessCount == sizeOneSelectedCount) // the first unit to move
							{
								unit.move_to(i, y, 1, SeekPath.SEARCH_MODE_IN_A_GROUP, 0, sizeOneSelectedCount);
								if (unit.mobile_type == UnitConstants.UNIT_LAND && unit.nation_recno != 0)
									unit.select_search_sub_mode(unit.next_x_loc(), unit.next_y_loc(), i, y,
										unit.nation_recno, SeekPath.SEARCH_MODE_IN_A_GROUP);
								unit.move_to(i, y, 1, SeekPath.SEARCH_MODE_IN_A_GROUP, 0, sizeOneSelectedCount);
							}
							else
							{
								if (unit.mobile_type == UnitConstants.UNIT_LAND && unit.nation_recno != 0)
									unit.select_search_sub_mode(unit.next_x_loc(), unit.next_y_loc(), i, y,
										unit.nation_recno, SeekPath.SEARCH_MODE_IN_A_GROUP);
								unit.move_to(i, y, 1, SeekPath.SEARCH_MODE_IN_A_GROUP, 0, sizeOneSelectedCount);
							}
						}
						else
							unit.move_to(i, y, 1);

						unprocessCount--;
					}
				}

				y += move_scale;
				//-******************* auto correct ***********************-//
				if (unprocessCount > 0 && y >= GameConstants.MapSize)
				{
					y = autoCorrectStartY;
				}
				//-******************* auto correct ***********************-//
			}
		}
		//}// end if (sizeOneSelectedCount)
		/*
		//=============================================================================//
		//----- order sprite with size two to move to a specified position ------------//
		//=============================================================================//
	
		int sizeTwoUnprocessCount = sizeTwoSelectedCount;
		int surX, surY, suaCount=0;
		char w, h, blocked=0;
	
		if(sizeOneSelectedCount)	// mix, size one units have processed
		{	
			if(rec_width>square_size)
				square_size = rec_width;
			if(rec_height>square_size)
				square_size = rec_height;
			square_size = ((square_size+1)/2)<<1;	// change to multiply of two
			rec_width = rec_height = square_size;
	
			x = destX-rec_width/2+1;
			y = destY-rec_height/2;
		}
		else	// all are size 2 units
		{
			//-***************** testing ********************-//
			err_here();
			//-***************** testing ********************-//
	
			square_size = rec_width = rec_height = 2;
			x = destX;
			y = destY;
	
			if(x<0)
				x = 0;
			else if(x>=MAX_WORLD_X_LOC-1)
				x = MAX_WORLD_X_LOC-2;
	
			if(y<0)
				y = 0;
			else if(y>=MAX_WORLD_Y_LOC-1)
				y = MAX_WORLD_Y_LOC-2;
			
			blocked = 0;
			for(h=0, surY=y; h<2 && !blocked; h++, surY++)
			{
				for(w=0, surX=x; w<2 && !blocked; w++, surX++)
				{	
					loc = worldLocMatrix+surY*MAX_WORLD_X_LOC+surX;
					blocked = !loc.is_unit_group_accessible(mobileType, curGroupId);
				}
			}
	
			if(!blocked)
			{
				do
				{
					unit = (Unit*) get_ptr(selectedUnitArray[suaCount++]);
				}while(unit.sprite_info.loc_width<2);
	
				if(sizeTwoSelectedCount>1)
				{
					unit.move_to(x, y, 1, 4, 0, sizeTwoSelectedCount, GENERAL_GROUP_MOVEMENT, REUSE_PATH_INITIAL);
					unit.move_to(x, y, 1, 4, 0, sizeTwoSelectedCount, GENERAL_GROUP_MOVEMENT, REUSE_PATH_FIRST_SEEK);
				}
				else
					unit.move_to(x, y, 1);
				sizeTwoUnprocessCount--;
			}
		}
	
		while(sizeTwoUnprocessCount)
		{
			//-***************** testing ********************-//
			err_here();
			//-***************** testing ********************-//
	
			int moveToX, moveToY;
			int boundedX, boundedY;
			
			//------------- upper edge --------------//
			moveToY = y-2;
			moveToX = x;
			if(moveToY>=0)
			{
				if(x+rec_width+2 > MAX_WORLD_X_LOC-2)
					boundedX = MAX_WORLD_X_LOC-1;
				else
					boundedX = x+rec_width+2;
				
				while(moveToX<boundedX && sizeTwoUnprocessCount)
				{
					//--------------- is the position blocked? ----------//
					if(moveToX>=MAX_WORLD_X_LOC-1 || moveToY>=MAX_WORLD_Y_LOC-1)
						blocked = 1;
					else
					{
						blocked = 0;
						for(h=0, surY=moveToY; h<2 && !blocked; h++, surY++)
						{
							for(w=0, surX=moveToX; w<2 && !blocked; w++, surX++)
							{	
								loc = worldLocMatrix+surY*MAX_WORLD_X_LOC+surX;
								blocked = !loc.is_unit_group_accessible(mobileType, curGroupId);
							}
						}
					}
	
					if(!blocked)
					{
						do
						{
							unit = (Unit*) get_ptr(selectedUnitArray[suaCount++]);
						}while(unit.sprite_info.loc_width<2);
	
						if(sizeTwoSelectedCount>1)
						{
							if(sizeTwoUnprocessCount==sizeTwoSelectedCount) // the first unit to move
							{	
								unit.move_to(moveToX, moveToY, 1, 4, 0, sizeTwoSelectedCount, GENERAL_GROUP_MOVEMENT, REUSE_PATH_INITIAL);
								unit.move_to(moveToX, moveToY, 1, 4, 0, sizeTwoSelectedCount, GENERAL_GROUP_MOVEMENT, REUSE_PATH_FIRST_SEEK);
							}
							else
							{
								err_if(sizeTwoUnprocessCount==sizeTwoSelectedCount)
									err_here();
								
								unit.move_to(moveToX, moveToY, 1, 4, 0, sizeTwoSelectedCount, GENERAL_GROUP_MOVEMENT, REUSE_PATH_SEARCH);
							}
						}
						else
							unit.move_to(moveToX, moveToY, 1);
						sizeTwoUnprocessCount--;
					}
					moveToX+=2;
				}	
			}
	
			//------------- right edge --------------//
			moveToX = x+rec_width;
			moveToY = y;
			if(moveToX<MAX_WORLD_X_LOC-1)
			{
				if(y+rec_height+2 > MAX_WORLD_Y_LOC-2)
					boundedY = MAX_WORLD_Y_LOC-1;
				else
					boundedY = y+rec_height+2;
				
				while(moveToY<boundedY && sizeTwoUnprocessCount)
				{
					//--------------- is the position blocked? ----------//
					if(moveToX>=MAX_WORLD_X_LOC-1 || moveToY>=MAX_WORLD_Y_LOC-1)
						blocked = 1;
					else
					{
						blocked = 0;
						for(h=0, surY=moveToY; h<2 && !blocked; h++, surY++)
						{
							for(w=0, surX=moveToX; w<2 && !blocked; w++, surX++)
							{
								loc = worldLocMatrix+surY*MAX_WORLD_X_LOC+surX;
								blocked = !loc.is_unit_group_accessible(mobileType, curGroupId);
							}
						}
					}
	
					if(!blocked)
					{
						do
						{
							unit = (Unit*) get_ptr(selectedUnitArray[suaCount++]);
						}while(unit.sprite_info.loc_width<2);
	
						if(sizeTwoSelectedCount>1)
						{
							if(sizeTwoUnprocessCount==sizeTwoSelectedCount) // the first unit to move
							{	
								unit.move_to(moveToX, moveToY, 1, 4, 0, sizeTwoSelectedCount, GENERAL_GROUP_MOVEMENT, REUSE_PATH_INITIAL);
								unit.move_to(moveToX, moveToY, 1, 4, 0, sizeTwoSelectedCount, GENERAL_GROUP_MOVEMENT, REUSE_PATH_FIRST_SEEK);
							}
							else
							{
								err_if(sizeTwoUnprocessCount==sizeTwoSelectedCount)
									err_here();
								
								unit.move_to(moveToX, moveToY, 1, 4, 0, sizeTwoSelectedCount, GENERAL_GROUP_MOVEMENT, REUSE_PATH_SEARCH);
							}
						}
						else
							unit.move_to(moveToX, moveToY, 1);
						sizeTwoUnprocessCount--;
					}
					moveToY+=2;
				}
			}
	
			//------------- lower edge ----------------//
			moveToX = x+rec_width-2;
			moveToY = y+rec_height;
			if(moveToY<MAX_WORLD_Y_LOC-1)
			{
				if(x-3 < 0)
					boundedX = -1;
				else
					boundedX = x-3;
				
				while(moveToX>boundedX && sizeTwoUnprocessCount)
				{
					//--------------- is the position blocked? ----------//
					if(moveToX>=MAX_WORLD_X_LOC-1 || moveToY>=MAX_WORLD_Y_LOC-1)
						blocked = 1;
					else
					{
						blocked = 0;
						for(h=0, surY=moveToY; h<2 && !blocked; h++, surY++)
						{
							for(w=0, surX=moveToX; w<2 && !blocked; w++, surX++)
							{
								loc = worldLocMatrix+surY*MAX_WORLD_X_LOC+surX;
								blocked = !loc.is_unit_group_accessible(mobileType, curGroupId);
							}
						}
					}
	
					if(!blocked)
					{
						do
						{
							unit = (Unit*) get_ptr(selectedUnitArray[suaCount++]);
						}while(unit.sprite_info.loc_width<2);
						if(sizeTwoSelectedCount>1)
						{
							if(sizeTwoUnprocessCount==sizeTwoSelectedCount) // the first unit to move
							{	
								unit.move_to(moveToX, moveToY, 1, 4, 0, sizeTwoSelectedCount, GENERAL_GROUP_MOVEMENT, REUSE_PATH_INITIAL);
								unit.move_to(moveToX, moveToY, 1, 4, 0, sizeTwoSelectedCount, GENERAL_GROUP_MOVEMENT, REUSE_PATH_FIRST_SEEK);
							}
							else
							{
								err_if(sizeTwoUnprocessCount==sizeTwoSelectedCount)
									err_here();
								
								unit.move_to(moveToX, moveToY, 1, 4, 0, sizeTwoSelectedCount, GENERAL_GROUP_MOVEMENT, REUSE_PATH_SEARCH);
							}
						}
						else
							unit.move_to(moveToX, moveToY, 1);
						sizeTwoUnprocessCount--;
					}
					moveToX-=2;
				}
			}
	
			//------------- left edge ---------------//
			moveToX = x-2;
			moveToY = y+rec_height-2;
			if(moveToX>=0)
			{
				if(y-3 < 0)
					boundedY = -1;
				else
					boundedY = y-3;
				
				while(moveToY>boundedY && sizeTwoUnprocessCount)
				{
					//--------------- is the position blocked? ----------//
					if(moveToX>=MAX_WORLD_X_LOC-1 || moveToY>=MAX_WORLD_Y_LOC-1)
						blocked = 1;
					else
					{
						blocked = 0;
						for(h=0, surY=moveToY; h<2 && !blocked; h++, surY++)
						{
							for(w=0, surX=moveToX; w<2 && !blocked; w++, surX++)
							{
								loc = worldLocMatrix+surY*MAX_WORLD_X_LOC+surX;
								blocked = !loc.is_unit_group_accessible(mobileType, curGroupId);
							}
						}
					}
	
					if(!blocked)
					{
						do
						{
							unit = (Unit*) get_ptr(selectedUnitArray[suaCount++]);
						}while(unit.sprite_info.loc_width<2);
	
						if(sizeTwoSelectedCount>1)
						{
							if(sizeTwoUnprocessCount==sizeTwoSelectedCount) // the first unit to move
							{	
								unit.move_to(moveToX, moveToY, 1, 4, 0, sizeTwoSelectedCount, GENERAL_GROUP_MOVEMENT, REUSE_PATH_INITIAL);
								unit.move_to(moveToX, moveToY, 1, 4, 0, sizeTwoSelectedCount, GENERAL_GROUP_MOVEMENT, REUSE_PATH_FIRST_SEEK);
							}
							else
							{
								err_if(sizeTwoUnprocessCount==sizeTwoSelectedCount)
									err_here();
								
								unit.move_to(moveToX, moveToY, 1, 4, 0, sizeTwoSelectedCount, GENERAL_GROUP_MOVEMENT, REUSE_PATH_SEARCH);
							}
						}
						else
							unit.move_to(moveToX, moveToY, 1);
						sizeTwoUnprocessCount--;
					}
					moveToY-=2;
				}
			}
	
			//------- reset square_size, rec_width, rec_height -----------//
			rec_width+=4;
			rec_height+=4;
			x-=2;
			y-=2;
		}*/
	}

	private int cal_rectangle_lower_right_x(int refXLoc, int move_scale, int rec_width)
	{
		// the rule:	refXLoc + (rec_width/(move_scale*2))*move_scale
		if (move_scale == 1)
			return refXLoc + rec_width / 2;
		else // move_scale == 2
			return refXLoc + (rec_width / 4) * 2;
	}

	private int cal_rectangle_lower_right_y(int refYLoc, int move_scale, int rec_height)
	{
		// the rule:	refYLoc + ((rec_height-move_scale)/(move_scale*2))*move_scale
		if (move_scale == 1)
			return refYLoc + (rec_height - 1) / 2;
		else // move_scale == 2
			return refYLoc + ((rec_height - 2) / 4) * 2;
	}

	private int cal_rectangle_upper_left_x(int refXLoc, int move_scale, int rec_width)
	{
		// the rule:	refXLoc - ((rec_width-move_scale)/(move_scale*2))*move_scale
		if (move_scale == 1)
			return refXLoc - (rec_width - 1) / 2;
		else // move_scale == 2
			return refXLoc - ((rec_width - 2) / 4) * 2;
	}

	private int cal_rectangle_upper_left_y(int refYLoc, int move_scale, int rec_height)
	{
		// the rule:	refYLoc - (rec_height/(move_scale*2))*move_scale
		if (move_scale == 1)
			return refYLoc - rec_height / 2;
		else // move_scale == 2
			return refYLoc - (rec_height / 4) * 2;
	}

	private void construct_sorted_array(List<int> selectedUnitArray, int selectedCount, int x, int y,
		int lower_right_case, int upper_left_case, int[] distance, int[] sorted_distance,
		int[] sorted_member, int[] done_flag)
	{
		int min, dist; // for comparison
		int i, j, k = 0;
		const int c = 1000; // c value for the d(x,y) function

		if (lower_right_case >= upper_left_case)
		{
			for (i = 0; i < selectedCount; i++)
			{
				Unit unit = this[selectedUnitArray[i]];
				// d(x,y)=x+c*|y|
				distance[i] = GameConstants.MapSize; // to aviod -ve no. in the following line
				// plus/minus x coord difference
				distance[i] += (x - unit.cur_x_loc() + c * Math.Abs(unit.cur_y_loc() - y));
			}
		}
		else // upper_left_case
		{
			for (i = 0; i < selectedCount; i++)
			{
				Unit unit = this[selectedUnitArray[i]];
				// d(x,y)=x+c*|y|
				distance[i] = GameConstants.MapSize; // to aviod -ve no. in the following line
				// plus/minus x coord difference
				distance[i] += (unit.cur_x_loc() - x + c * Math.Abs(unit.cur_y_loc() - y));
			}
		}

		//---------------------------------------------------------------//
		// this part of code using a technique to adjust the distance value
		// such that the selected group can change from lower right form
		// to upper left form or upper left form to lower right form in a
		// better way.
		//---------------------------------------------------------------//
		//------ sorting the distance and store in sortedDistance Array -------//
		for (j = 0; j < selectedCount; j++)
		{
			min = Int32.MaxValue;
			for (i = 0; i < selectedCount; i++)
			{
				if (done_flag[i] == 0 && (dist = distance[i]) < min)
				{
					min = dist;
					k = i;
				}
			}

			sorted_distance[j] = k;
			done_flag[k] = 1;
		}

		//----------------- find the minimum value --------------//
		min = distance[sorted_distance[0]];

		int defArraySize = 5; //****** BUGHERE, 5 is chosen arbitrary
		int[] leftQuotZ = new int[defArraySize];
		int[] rightQuotZ = new int[defArraySize];
		int remainder = min % c;
		int index;

		//-- adjust the value to allow changing form between upper left and lower right shape --//

		for (j = 0; j < defArraySize; j++)
			leftQuotZ[j] = rightQuotZ[j] = min - remainder;

		for (j = 0; j < selectedCount; j++)
		{
			if ((dist = distance[sorted_distance[j]] % c) < remainder)
			{
				if ((index = remainder - dist) <= defArraySize) // the case can be handled by this array size
				{
					distance[sorted_distance[j]] = leftQuotZ[index - 1] + dist;
					leftQuotZ[index - 1] += c;
				}
			}
			else
			{
				if (dist >= remainder)
				{
					if ((index = dist - remainder) < defArraySize) // the case can be handled by this array size
					{
						distance[sorted_distance[j]] = rightQuotZ[index] + dist;
						rightQuotZ[index] += c;
					}
				}
			}
		}

		//---------- sorting -------------//
		for (j = 0; j < selectedCount; j++)
		{
			min = Int32.MaxValue;
			for (i = 0; i < selectedCount; i++)
			{
				if ((dist = distance[i]) < min)
				{
					min = dist;
					k = i;
				}
			}

			sorted_member[j] = k;
			distance[k] = Int32.MaxValue;
		}
	}

	private void determine_position_to_construct_table(int selectedCount, int destXLoc, int destYLoc, int mobileType,
		ref int x, ref int y, int move_scale, ref int rec_width, ref int rec_height,
		ref int lower_right_case, ref int upper_left_case, int not_tested_loc, int square_size)
	{
		//======================================================================//
		// boundary, corner improvement
		//======================================================================//
		int sqrtValue;

		//======================================================================//
		// lower right case
		//======================================================================//
		if (lower_right_case >= upper_left_case)
		{
			//--------- calculate x, y location for lower right case ---------//
			x = cal_rectangle_lower_right_x(destXLoc, move_scale, rec_width);
			y = cal_rectangle_lower_right_y(destYLoc, move_scale, rec_height);

			if (x < rec_width)
			{
				//================== left edge =================//
				sqrtValue = (int)Math.Sqrt(selectedCount);
				if (sqrtValue * sqrtValue != selectedCount)
					sqrtValue++;
				if (mobileType != UnitConstants.UNIT_LAND)
					sqrtValue = sqrtValue << 1; // change to scale 2
				rec_width = rec_height = sqrtValue;

				//------------- top left corner --------------//
				if (y < rec_height)
				{
					upper_left_case = lower_right_case + 1;
					x = y = 0;
				}
				//------------ bottom left corner ---------------//
				else if (y >= GameConstants.MapSize - move_scale)
				{
					if (not_tested_loc >= square_size / move_scale)
						rec_width -= move_scale;

					x = rec_width - move_scale;
					y = GameConstants.MapSize - move_scale;
				}
				//------------- just left edge -------------//
				else
					x = rec_width - move_scale;
			}
			else if (x >= GameConstants.MapSize - move_scale)
			{
				//============== right edge ==============//

				//----------- top right corner -----------//
				if (y < rec_height)
				{
					sqrtValue = (int)Math.Sqrt(selectedCount);
					if (sqrtValue * sqrtValue != selectedCount)
						sqrtValue++;
					if (mobileType != UnitConstants.UNIT_LAND)
						sqrtValue = sqrtValue << 1; // change to scale 2
					rec_width = rec_height = sqrtValue;

					upper_left_case = lower_right_case + 1;
					x = GameConstants.MapSize - rec_width;
					y = 0;
				}
				//---------- bottom right corner ------------//
				else if (y >= GameConstants.MapSize - move_scale)
				{
					y = GameConstants.MapSize - move_scale;
					x = GameConstants.MapSize - move_scale;
				}
				//---------- just right edge ---------------//
				else
				{
					int squareSize = square_size / move_scale;
					if (squareSize * (squareSize - 1) >= selectedCount)
						rec_width -= move_scale;
					x = GameConstants.MapSize - move_scale;
				}
			}
			else if (y < rec_height)
			{
				//================= top edge ===============//
				sqrtValue = (int)Math.Sqrt(selectedCount);
				if (sqrtValue * sqrtValue != selectedCount)
					sqrtValue++;
				if (mobileType != UnitConstants.UNIT_LAND)
					sqrtValue = sqrtValue << 1; // change to scale 2
				rec_width = rec_height = sqrtValue;

				upper_left_case = lower_right_case + 1;
				//if(mobileType==UnitConstants.UNIT_LAND)
				//	x = destXLoc-((rec_width-1)/2);
				//else
				//	x = destXLoc-(rec_width/4)*2;
				x = cal_rectangle_upper_left_x(destXLoc, move_scale, rec_width);
				y = 0;
			}
			else if (y >= GameConstants.MapSize - move_scale)
			{
				//================== bottom edge ====================//
				if (not_tested_loc >= square_size / move_scale)
					rec_width += move_scale;

				//if(mobileType==UnitConstants.UNIT_LAND)
				//	x = destXLoc+(rec_width/2);
				//else
				//	x = destXLoc+(rec_width/4)*2;
				x = cal_rectangle_lower_right_x(destXLoc, move_scale, rec_width);
				y = GameConstants.MapSize - move_scale;
			}
		}
		//======================================================================//
		// upper left case
		//======================================================================//
		else
		{
			//--------- calculate x, y location for upper left case ---------//
			x = cal_rectangle_upper_left_x(destXLoc, move_scale, rec_width);
			y = cal_rectangle_upper_left_y(destYLoc, move_scale, rec_height);

			if (x < 0)
			{
				//================= left edge ==================//

				//------------- top left corner --------------//
				if (y < 0)
				{
					sqrtValue = (int)Math.Sqrt(selectedCount);
					if (sqrtValue * sqrtValue != selectedCount)
						sqrtValue++;
					if (mobileType != UnitConstants.UNIT_LAND)
						sqrtValue = sqrtValue << 1; // change to scale 2
					rec_width = rec_height = sqrtValue;
					x = y = 0;
				}
				//------------- bottom left corner --------------//
				else if (y + rec_height >= GameConstants.MapSize - move_scale)
				{
					lower_right_case = upper_left_case + 1;
					x = rec_width - move_scale;
					y = GameConstants.MapSize - move_scale;
				}
				//------------- just left edge ------------------//
				else
				{
					sqrtValue = (int)Math.Sqrt(selectedCount);
					if (sqrtValue * sqrtValue != selectedCount)
						sqrtValue++;
					if (mobileType != UnitConstants.UNIT_LAND)
						sqrtValue = sqrtValue << 1; // change to scale 2
					rec_width = rec_height = sqrtValue;
					x = 0;
				}
			}
			//================ right edge ================//
			else if (x + rec_width >= GameConstants.MapSize - move_scale)
			{
				//------------- top right corner ------------------//
				if (y < 0)
				{
					sqrtValue = (int)Math.Sqrt(selectedCount);
					if (sqrtValue * sqrtValue != selectedCount)
						sqrtValue++;
					if (mobileType != UnitConstants.UNIT_LAND)
						sqrtValue = sqrtValue << 1; // change to scale 2
					rec_width = rec_height = sqrtValue;
					x = GameConstants.MapSize - rec_width;
					y = 0;
				}
				//------------- bottom right corner ------------------//
				else if (y + rec_height >= GameConstants.MapSize - move_scale)
				{
					lower_right_case = upper_left_case + 1;
					x = GameConstants.MapSize - move_scale;
					y = GameConstants.MapSize - move_scale;
				}
				//------------- just right edge ------------------//
				else
				{
					sqrtValue = (int)Math.Sqrt(selectedCount);
					if (sqrtValue * sqrtValue != selectedCount)
						sqrtValue++;
					if (mobileType != UnitConstants.UNIT_LAND)
						sqrtValue = sqrtValue << 1; // change to scale 2
					rec_width = rec_height = sqrtValue;

					int squareSize = square_size / move_scale;
					if (squareSize * (squareSize - 1) >= selectedCount)
						rec_width -= move_scale;
					lower_right_case = upper_left_case + 1;
					x = GameConstants.MapSize - move_scale;
					//if(mobileType==UNIT_LAND)
					//	y = destYLoc+((rec_height-1)/2);
					//else
					//	y = destYLoc+((rec_height-2)/4)*2;
					y = cal_rectangle_lower_right_y(destYLoc, move_scale, rec_height);
				}
			}
			//================= top edge ================//
			else if (y < 0)
			{
				sqrtValue = (int)Math.Sqrt(selectedCount);
				if (sqrtValue * sqrtValue != selectedCount)
					sqrtValue++;

				rec_width = rec_height = sqrtValue;
				y = 0;
			}
			//================= bottom edge ================//
			else if (y + rec_height >= GameConstants.MapSize - move_scale)
			{
				if (not_tested_loc >= square_size)
					rec_width += move_scale;
				y = GameConstants.MapSize - move_scale;
			}
		}

		/*if(lower_right_case>=upper_left_case)
		{
			x = cal_rectangle_lower_right_x(destXLoc);
			y = cal_rectangle_lower_right_y(destYLoc);
	
			rec_x1 = x - rec_width + move_scale;
			rec_y1 = y - rec_height + move_scale;
			rec_x2 = x;
			rec_y2 = y;
		}
		else
		{
			x = cal_rectangle_upper_left_x(destXLoc);
			y = cal_rectangle_upper_left_y(destYLoc);
		}*/
	}

	//-------------- attack sub-functions -----------//
	private void attack_call(int targetXLoc, int targetYLoc, int mobileType, int targetMobileType, bool divided,
		List<int> selectedUnits, int targetUnitRecno)
	{
		//------------- attack now -------------//
		Location loc = World.get_loc(targetXLoc, targetYLoc);

		//if(targetMobileType)
		if (targetUnitRecno != 0 && !IsDeleted(targetUnitRecno))
		{
			//---------------- attack unit --------------//
			//Unit *targetUnit = unit_array[loc.unit_recno(targetMobileType)];
			Unit targetUnit = this[targetUnitRecno];
			if (!targetUnit.is_visible() || targetUnit.hit_points <= 0)
				return;

			// short targetUnitRecno = targetUnit.sprite_recno;
			attack_unit(targetXLoc, targetYLoc, targetUnitRecno, selectedUnits);
		}
		else if (loc.is_firm())
		{
			//------------------ attack firm -------------------//
			Firm firm = FirmArray[loc.firm_recno()];
			if (firm.hit_points <= 0.0)
				return;

			attack_firm(targetXLoc, targetYLoc, firm.firm_recno, selectedUnits);
		}
		else if (loc.is_town())
		{
			//-------------------- attack town -------------------//
			Town town = TownArray[loc.town_recno()];
			attack_town(targetXLoc, targetYLoc, town.TownId, selectedUnits);
		}
		else if (loc.is_wall())
		{
			//------------------ attack wall ---------------------//
			attack_wall(targetXLoc, targetYLoc, selectedUnits);
		}
	}

	private void update_unreachable_table(int targetXLoc, int targetYLoc, int targetWidth, int targetHeight,
		int mobileType, ref int analyseResult, bool[,] unreachable_table)
	{
		int xLoc1, yLoc1, xLoc2, yLoc2, i;

		//----------------------------------------------------------------------//
		// checking for left & right edges, calculate location and counter
		//----------------------------------------------------------------------//
		xLoc1 = targetXLoc - 1;
		xLoc2 = targetXLoc + targetWidth;

		if (targetYLoc == 0)
		{
			i = targetHeight + 1;
			yLoc1 = targetYLoc;
		}
		else
		{
			i = targetHeight + 2;
			yLoc1 = targetYLoc - 1;
		}

		if (targetYLoc + targetHeight >= GameConstants.MapSize)
			i--;

		for (yLoc2 = yLoc1 - targetYLoc + SHIFT_ADJUST; i > 0; i--, yLoc1++, yLoc2++)
		{
			//---- left edge ----//
			if (xLoc1 >= 0 && !unreachable_table[0, yLoc2] && !World.get_loc(xLoc1, yLoc1).can_move(mobileType))
			{
				unreachable_table[0, yLoc2] = true;
				analyseResult--;
			}

			//----- right edge -----//
			if (xLoc2 < GameConstants.MapSize && !unreachable_table[targetWidth + 1, yLoc2] &&
			    !World.get_loc(xLoc2, yLoc1).can_move(mobileType))
			{
				unreachable_table[targetWidth + 1, yLoc2] = true;
				analyseResult--;
			}

			if (analyseResult == 0)
				return;
		}

		//----------------------------------------------------------------------//
		// checking for the top and bottom edges
		//----------------------------------------------------------------------//
		yLoc1 = targetYLoc - 1;
		yLoc2 = targetYLoc + targetHeight;
		for (i = 0, xLoc1 = targetXLoc, xLoc2 = xLoc1 - targetXLoc + SHIFT_ADJUST;
		     i < targetWidth;
		     i++, xLoc1++, xLoc2++)
		{
			//---- top edge ----//
			if (yLoc1 >= 0 && !unreachable_table[xLoc2, 0] && !World.get_loc(xLoc1, yLoc1).can_move(mobileType))
			{
				unreachable_table[xLoc2, 0] = true;
				analyseResult--;
			}

			//----- bottom edge -----//
			if (yLoc2 < GameConstants.MapSize && !unreachable_table[xLoc2, targetHeight + 1] &&
			    !World.get_loc(xLoc1, yLoc2).can_move(mobileType))
			{
				unreachable_table[xLoc2, targetHeight + 1] = true;
				analyseResult--;
			}

			if (analyseResult == 0)
				return;
		}
	}

	private int get_target_surround_loc(int targetWidth, int targetHeight)
	{
		int[,] surround_loc = { { 8, 10, 12, 14 }, { 10, 12, 14, 16 }, { 12, 14, 16, 18 }, { 14, 16, 18, 20 } };

		return surround_loc[targetWidth - 1, targetHeight - 1];
	}

	private void arrange_units_in_group(int xLoc1, int yLoc1, int xLoc2, int yLoc2,
		List<int> selectedUnits, int unitGroupId, int targetType, int[][] dir_array_ptr, int[] dir_array_count)
	{
		for (int i = 0; i < selectedUnits.Count; i++)
		{
			Unit unit = this[selectedUnits[i]];

			unit.unit_group_id = unitGroupId; // set unit_group_id
			if (unit.cur_action == Sprite.SPRITE_IDLE)
				unit.set_ready();

			int curXLoc = unit.next_x_loc();
			int curYLoc = unit.next_y_loc();
			if (curXLoc >= xLoc1 - 1 && curXLoc <= xLoc2 + 1 && curYLoc >= yLoc1 - 1 && curYLoc <= yLoc2 + 1)
			{
				//------------- already in the target surrounding ----------------//
				switch (targetType)
				{
					case 0:
						unit.attack_wall(xLoc1, yLoc1);
						break;

					case 1:
						unit.attack_unit(xLoc1, yLoc1, 0, 0, true);
						break;

					case 2:
						unit.attack_firm(xLoc1, yLoc1);
						break;

					case 3:
						unit.attack_town(xLoc1, yLoc1);
						break;
				}

				continue;
			}

			//---- the attacker need to call searching to reach the target ----//
			if (curXLoc < xLoc1)
			{
				if (curYLoc < yLoc1) // 8
					dir_array_ptr[7][dir_array_count[7]++] = selectedUnits[i];
				else if (curYLoc > yLoc2) // 2
					dir_array_ptr[1][dir_array_count[1]++] = selectedUnits[i];
				else // 1
					dir_array_ptr[0][dir_array_count[0]++] = selectedUnits[i];
			}
			else if (curXLoc > xLoc2)
			{
				if (curYLoc < yLoc1) // 6
					dir_array_ptr[5][dir_array_count[5]++] = selectedUnits[i];
				else if (curYLoc > yLoc2) // 4
					dir_array_ptr[3][dir_array_count[3]++] = selectedUnits[i];
				else // 5
					dir_array_ptr[4][dir_array_count[4]++] = selectedUnits[i];
			}
			else // curXLoc==targetXLoc2
			{
				if (curYLoc < yLoc1) // 7
					dir_array_ptr[6][dir_array_count[6]++] = selectedUnits[i];
				else if (curYLoc > yLoc2) // 3
					dir_array_ptr[2][dir_array_count[2]++] = selectedUnits[i];
				else // curXLoc==xLoc2 && curYLoc==yLoc2
				{
					// target is one of the selected unit, error
				}
			}
		}
	}

	private int analyse_surround_location(int targetXLoc, int targetYLoc, int targetWidth, int targetHeight,
		int mobileType, bool[,] unreachable_table)
	{
		int[] xIncreTable = { 1, 0, -1, 0 };
		int[] yIncreTable = { 0, 1, 0, -1 };

		int xLoc = targetXLoc - 1;
		int yLoc = targetYLoc - 1;
		int targetXLoc2 = targetXLoc + targetWidth - 1;
		int targetYLoc2 = targetYLoc + targetHeight - 1;
		int bound = 2 * (targetWidth + targetHeight) + 4; // (x+2)*(y+2) - xy
		int increCount = 4, xIncre = 0, yIncre = 0, found = 0;

		for (int i = 0; i < bound; i++)
		{
			if (xLoc < 0 || xLoc >= GameConstants.MapSize || yLoc < 0 || yLoc >= GameConstants.MapSize)
				unreachable_table[xLoc - targetXLoc + SHIFT_ADJUST, yLoc - targetYLoc + SHIFT_ADJUST] = true;
			else
			{
				Location location = World.get_loc(xLoc, yLoc);
				if (!location.can_move(mobileType))
					unreachable_table[xLoc - targetXLoc + SHIFT_ADJUST, yLoc - targetYLoc + SHIFT_ADJUST] = true;
				else
					found++;
			}

			if ((xLoc == targetXLoc - 1 || xLoc == targetXLoc2 + 1) &&
			    (yLoc == targetYLoc - 1 || yLoc == targetYLoc2 + 1)) // at the corner
			{
				if ((++increCount) >= 4)
					increCount = 0;

				xIncre = xIncreTable[increCount];
				yIncre = yIncreTable[increCount];
			}

			xLoc += xIncre;
			yLoc += yIncre;
		}

		return found;
	}

	private void check_nearby_location(int targetXLoc, int targetYLoc, int xOffset, int yOffset,
		int targetWidth, int targetHeight, int targetMobileType, bool[,] unreachable_table, ref int analyseResult)
	{
		int[] leftXIncreTable = { 1, 0, -1, 0 };
		int[] leftYIncreTable = { 0, 1, 0, -1 };
		int[] rightXIncreTable = { -1, 0, 1, 0 };
		int[] rightYIncreTable = { 0, 1, 0, -1 };

		int targetXLoc2 = targetXLoc + targetWidth - 1;
		int targetYLoc2 = targetYLoc + targetHeight - 1;
		int bound = 2 * (targetWidth + targetHeight) + 4; // (x+2)*(y+2) - xy

		int leftXLoc, leftYLoc, leftContinue = 1;
		int leftXIncre = 0, leftYIncre = 0, leftIncreCount = 0;
		int rightXLoc, rightYLoc, rightContinue = 1;
		int rightXIncre = 0, rightYIncre = 0, rightIncreCount = 1;

		bool haveValidSituation = true;

		//-------------------------------------------------------------------------------------//
		// determine the initial situation
		//-------------------------------------------------------------------------------------//
		if ((xOffset == -1 || xOffset == targetWidth) && (yOffset == -1 || yOffset == targetHeight)) // at the corner
		{
			if (xOffset == -1)
			{
				if (yOffset == -1) // upper left corner
				{
					leftXIncre = 1;
					leftYIncre = 0;
					leftIncreCount = 0;

					rightXIncre = 0;
					rightYIncre = 1;
					rightIncreCount = 1;
				}
				else // lower left corner
				{
					leftXIncre = 0;
					leftYIncre = -1;
					leftIncreCount = 3;

					rightXIncre = 1;
					rightYIncre = 0;
					rightIncreCount = 2;
				}
			}
			else
			{
				if (yOffset == -1) // upper right corner
				{
					leftXIncre = 0;
					leftYIncre = 1;
					leftIncreCount = 1;

					rightXIncre = -1;
					rightYIncre = 0;
					rightIncreCount = 0;
				}
				else // lower right corner
				{
					leftXIncre = -1;
					leftYIncre = 0;
					leftIncreCount = 2;

					rightXIncre = 0;
					rightYIncre = -1;
					rightIncreCount = 3;
				}
			}
		}
		else // at the edge
		{
			if (xOffset == -1) // left edge
			{
				leftXIncre = 0;
				leftYIncre = -1;
				leftIncreCount = 3;

				rightXIncre = 0;
				rightYIncre = 1;
				rightIncreCount = 1;
			}
			else if (xOffset == targetWidth) // right edge
			{
				leftXIncre = 0;
				leftYIncre = 1;
				leftIncreCount = 1;

				rightXIncre = 0;
				rightYIncre = -1;
				rightIncreCount = 3;
			}
			else if (yOffset == -1) // upper edge
			{
				leftXIncre = 1;
				leftYIncre = 0;
				leftIncreCount = 0;

				rightXIncre = -1;
				rightYIncre = 0;
				rightIncreCount = 0;
			}
			else if (yOffset == targetHeight) // lower edge
			{
				leftXIncre = -1;
				leftYIncre = 0;
				leftIncreCount = 2;

				rightXIncre = 1;
				rightYIncre = 0;
				rightIncreCount = 2;
			}
			else
			{
				haveValidSituation = false;
			}
		}

		leftXLoc = rightXLoc = targetXLoc + xOffset;
		leftYLoc = rightYLoc = targetYLoc + yOffset;
		int canReach;
		int outBoundary; // true if out of map boundary

		//-------------------------------------------------------------------------------------//
		// count the reachable location
		//-------------------------------------------------------------------------------------//
		for (int i = 1; i < bound; i++) // exclude the starting location
		{
			//------------------------------------------------------------//
			// process left hand side checking
			//------------------------------------------------------------//
			if (leftContinue == 1)
			{
				canReach = 0;
				outBoundary = 0;

				leftXLoc += leftXIncre;
				leftYLoc += leftYIncre;
				if ((leftXLoc == targetXLoc - 1 || leftXLoc == targetXLoc2 + 1) &&
				    (leftYLoc == targetYLoc - 1 || leftYLoc == targetYLoc2 + 1))
				{
					if ((++leftIncreCount) >= 4)
						leftIncreCount = 0;

					leftXIncre = leftXIncreTable[leftIncreCount];
					leftYIncre = leftYIncreTable[leftIncreCount];
				}

				if (leftXLoc >= 0 && leftXLoc < GameConstants.MapSize && leftYLoc >= 0 &&
				    leftYLoc < GameConstants.MapSize)
				{
					// concept incorrect, but it is used to terminate this part of checking
					if (unreachable_table[leftXLoc - targetXLoc + SHIFT_ADJUST, leftYLoc - targetYLoc + SHIFT_ADJUST])
						canReach = 1;
					else
					{
						Location location = World.get_loc(leftXLoc, leftYLoc);
						if (location.can_move(targetMobileType))
							canReach = 1;
					}
				}
				else
					outBoundary = 1;

				if (canReach != 0)
					leftContinue = 0;
				else if (outBoundary == 0)
				{
					unreachable_table[leftXLoc - targetXLoc + SHIFT_ADJUST, leftYLoc - targetYLoc + SHIFT_ADJUST] = true;
					analyseResult--;
				}

				i++;
			}

			//------------------------------------------------------------//
			// process right hand side checking
			//------------------------------------------------------------//
			if (rightContinue != 0)
			{
				canReach = 0;
				outBoundary = 0;

				rightXLoc += rightXIncre;
				rightYLoc += rightYIncre;
				if ((rightXLoc == targetXLoc - 1 || rightXLoc == targetXLoc2 + 1) &&
				    (rightYLoc == targetYLoc - 1 || rightYLoc == targetYLoc2 + 1))
				{
					if ((++rightIncreCount) >= 4)
						rightIncreCount = 0;

					rightXIncre = rightXIncreTable[rightIncreCount];
					rightYIncre = rightYIncreTable[rightIncreCount];
				}

				if (rightXLoc >= 0 && rightXLoc < GameConstants.MapSize && rightYLoc >= 0 &&
				    rightYLoc < GameConstants.MapSize)
				{
					if (unreachable_table[rightXLoc - targetXLoc + SHIFT_ADJUST, rightYLoc - targetYLoc + SHIFT_ADJUST])
						canReach = 1; // concept incorrect, but it is used to terminate this part of checking
					else
					{
						Location location = World.get_loc(rightXLoc, rightYLoc);
						if (location.can_move(targetMobileType))
							canReach = 1;
					}
				}
				else
					outBoundary = 1;

				if (canReach != 0)
					rightContinue = 0;
				else if (outBoundary == 0)
				{
					unreachable_table[rightXLoc - targetXLoc + SHIFT_ADJUST, rightYLoc - targetYLoc + SHIFT_ADJUST] = true;
					analyseResult--;
				}
			}

			if (leftContinue == 0 && rightContinue == 0)
				break;
		}
	}

	private void handle_attack_target_totally_blocked(int targetXLoc, int targetYLoc, int targetRecno,
		List<int> selectedUnits, int targetType, int[] unit_processed_array, int unit_processed_count)
	{
		if (unit_processed_count > 0) // some units can reach the target surrounding
		{
			int proCount = unit_processed_count - 1;
			int unproCount = selectedUnits.Count - proCount - 1; // number of unprocessed
			int sCount = selectedUnits.Count - 1;
			int found, i, recno;

			//------------------------------------------------------------------------------------//
			// use the result of those processed units as a reference of those unprocessed units
			//------------------------------------------------------------------------------------//
			while (unproCount > 0)
			{
				Unit processed = this[unit_processed_array[proCount]];
				do
				{
					found = 0;
					recno = selectedUnits[sCount];
					for (i = 0; i < unit_processed_count; i++)
					{
						if (unit_processed_array[i] == recno)
						{
							found++;
							break;
						}
					}

					sCount--;
				} while (found > 0);

				Unit unit = this[recno];
				unit.move_to(processed.move_to_x_loc, processed.move_to_y_loc);

				switch (targetType)
				{
					case 0: // wall
						unit.action_mode = unit.action_mode2 = UnitConstants.ACTION_ATTACK_WALL;
						break;

					case 1: // unit
						unit.action_mode = unit.action_mode2 = UnitConstants.ACTION_ATTACK_UNIT;
						break;

					case 2: // firm
						unit.action_mode = unit.action_mode2 = UnitConstants.ACTION_ATTACK_FIRM;
						break;

					case 3: // town
						unit.action_mode = unit.action_mode2 = UnitConstants.ACTION_ATTACK_TOWN;
						break;
				}

				unit.action_para = unit.action_para2 = targetRecno;
				unit.action_x_loc = unit.action_x_loc2 = targetXLoc;
				unit.action_y_loc = unit.action_y_loc2 = targetYLoc;

				proCount--;
				if (proCount < 0)
					proCount = unit_processed_count - 1;

				unproCount--;
			}
		}
		else // none of the units reaches the target surrounding
		{
			//----------------------------------------------------------------//
			// handle the case for 1x1 units now, no 2x2 units
			//----------------------------------------------------------------//
			//-*********** improve later ************-//
			int unprocessed = selectedUnits.Count;
			Unit first = this[selectedUnits[unprocessed - 1]];

			switch (targetType)
			{
				case 0:
					first.attack_wall(targetXLoc, targetYLoc);
					break;

				case 1:
					first.attack_unit(targetXLoc, targetYLoc, 0, 0, true);
					break;

				case 2:
					first.attack_firm(targetXLoc, targetYLoc);
					break;

				case 3:
					first.attack_town(targetXLoc, targetYLoc);
					break;
			}

			int moveToXLoc = first.move_to_x_loc;
			int moveToYLoc = first.move_to_y_loc;

			/*if(seek_path.path_status==PATH_NODE_USED_UP)
			{
				int debug = 0;
			}*/

			while (unprocessed > 0)
			{
				Unit unit = this[selectedUnits[unprocessed - 1]];
				unit.move_to(moveToXLoc, moveToYLoc);

				switch (targetType)
				{
					case 0: // wall
						unit.action_mode = unit.action_mode2 = UnitConstants.ACTION_ATTACK_WALL;
						unit.action_para = unit.action_para2 = 0;
						break;

					case 1: // unit
						unit.action_mode = unit.action_mode2 = UnitConstants.ACTION_ATTACK_UNIT;
						unit.action_para = unit.action_para2 = targetRecno;
						break;

					case 2: // firm
						unit.action_mode = unit.action_mode2 = UnitConstants.ACTION_ATTACK_FIRM;
						unit.action_para = unit.action_para2 = targetRecno;
						break;

					case 3: // town
						unit.action_mode = unit.action_mode2 = UnitConstants.ACTION_ATTACK_TOWN;
						unit.action_para = unit.action_para2 = targetRecno;
						break;
				}

				unit.action_x_loc = unit.action_x_loc2 = targetXLoc;
				unit.action_y_loc = unit.action_y_loc2 = targetYLoc;

				unprocessed--;
			}
		}
	}

	//---------- other actions functions -----------//
	private void group_assign(int destX, int destY, List<int> selectedUnits)
	{
		const int ASSIGN_TYPE_UNIT = 1;
		const int ASSIGN_TYPE_FIRM = 2;
		const int ASSIGN_TYPE_TOWN = 3;

		Location loc = World.get_loc(destX, destY);

		int assignType = 0; // 1 for unit, 2 for firm, 3 for town
		int miscNo = 0; // usedd to store the recno of the object
		if (loc.has_unit(UnitConstants.UNIT_LAND))
		{
			assignType = ASSIGN_TYPE_UNIT;
			miscNo = loc.unit_recno(UnitConstants.UNIT_LAND);
		}
		else if (loc.is_firm())
		{
			assignType = ASSIGN_TYPE_FIRM;
			miscNo = FirmArray[loc.firm_recno()].firm_id;
		}
		else if (loc.is_town())
		{
			assignType = ASSIGN_TYPE_TOWN;
			miscNo = 0;
		}

		for (int i = 0; i < selectedUnits.Count; i++)
		{
			Unit unit = this[selectedUnits[i]];
			if (unit.sprite_info.loc_width <= 1)
			{
				// the third parameter is used to generate different result for the searching
				unit.assign(destX, destY, i + 1);
			}
			else
			{
				//-----------------------------------------------------------------//
				// for 2x2 unit, unable to assign, so move to the surrounding
				//-----------------------------------------------------------------//
				switch (assignType)
				{
					case ASSIGN_TYPE_UNIT:
						unit.move_to_unit_surround(destX, destY, unit.sprite_info.loc_width,
							unit.sprite_info.loc_height, miscNo);
						break; // is a unit

					case ASSIGN_TYPE_FIRM:
						unit.move_to_firm_surround(destX, destY, unit.sprite_info.loc_width,
							unit.sprite_info.loc_height, miscNo);
						break; // is a firm

					case ASSIGN_TYPE_TOWN:
						unit.move_to_town_surround(destX, destY, unit.sprite_info.loc_width,
							unit.sprite_info.loc_height);
						break; // is a town

					default:
						break;
				}
			}
		}
	}

	private void group_settle(int destX, int destY, List<int> selectedUnits)
	{
		for (int i = 0; i < selectedUnits.Count; i++)
		{
			Unit unit = this[selectedUnits[i]];

			unit.settle(destX, destY, i + 1);
		}
	}
}