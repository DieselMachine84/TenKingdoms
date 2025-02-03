using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class Rebel
{
    public const int REBEL_IDLE = 1;
    public const int REBEL_ATTACK_TOWN = 2; // Attack town without capturing
    public const int REBEL_ATTACK_FIRM = 3; // Attack firm without capturing
    public const int REBEL_SETTLE_NEW = 4; // Settle to a new town
    public const int REBEL_SETTLE_TO = 5; // Settle to an existing town 

    public int rebel_recno; // recno of this rebel in rebel_array
    public int leader_unit_recno;
    public int action_mode;
    public int action_para;
    public int action_para2;
    public int mobile_rebel_count; // no. of units in this rebel group
    public int town_recno; // the town controlled by the rebel, one rebel can only control one town
    public int hostile_nation_bits;

    private Info Info => Sys.Instance.Info;
    private World World => Sys.Instance.World;
    private UnitArray UnitArray => Sys.Instance.UnitArray;
    private RebelArray RebelArray => Sys.Instance.RebelArray;
    private NationArray NationArray => Sys.Instance.NationArray;
    private TownArray TownArray => Sys.Instance.TownArray;
    private FirmArray FirmArray => Sys.Instance.FirmArray;

    public Rebel()
    {
    }

    public void Init(int unitRecno, int hostileNationRecno, int actionMode, int actionPara)
    {
        leader_unit_recno = unitRecno;
        action_mode = actionMode;
        action_para = actionPara;
        mobile_rebel_count = 1;
        set_hostile_nation(hostileNationRecno);

        UnitArray[unitRecno].set_mode(UnitConstants.UNIT_MODE_REBEL, rebel_recno);
    }

    public void Deinit()
    {
        foreach (Unit unit in UnitArray)
        {
            if (unit.unit_mode == UnitConstants.UNIT_MODE_REBEL && unit.unit_mode_para == rebel_recno)
            {
                unit.set_mode(0);
            }
        }
    }

    public void next_day()
    {
        //---- if the rebels has a town and this rebel is a defender from a town ----//

        if (town_recno != 0)
        {
            //--- check if the rebel town is destroyed ----//

            if (TownArray.IsDeleted(town_recno))
            {
                town_recno = 0;

                //--- if the town has been destroyed, the rebel group becomes mobile again, and a new leader needs to be selected.

                if (mobile_rebel_count > 0 && leader_unit_recno == 0)
                {
                    select_new_leader();
                }
            }

            return; // don't think new action as this rebel defender need to go back to its town.
        }

        //------- no rebels left in the group --------//

        if (town_recno == 0 && mobile_rebel_count == 0)
        {
            RebelArray.DeleteRebel(this);
            return;
        }

        //---------------------------------------------//

        if (mobile_rebel_count > 0) // if there are mobile rebel units on the map
        {
            if (action_mode == REBEL_IDLE)
                think_new_action(); // think about a new action
            else
                think_cur_action(); // think if there should be any changes to the current action
        }
        else // if there are no mobile rebel units
        {
            if (town_recno > 0)
            {
                if (Info.TotalDays % 30 == rebel_recno % 30) // if the rebel has a town
                    think_town_action();
            }
        }
    }

    public void join(int unitRecno)
    {
        Unit unit = UnitArray[unitRecno];

        unit.set_mode(UnitConstants.UNIT_MODE_REBEL, rebel_recno);

        mobile_rebel_count++;
    }

    public void set_action(int actionMode, int actionPara = 0, int actionPara2 = 0)
    {
        action_mode = actionMode;
        action_para = actionPara;
        action_para2 = actionPara2;
    }

    public void town_being_attacked(int attackerUnitRecno)
    {
        //----------------------------------------------//
        //
        // Set the hostile_nation_recno. So that if the rebel
        // town is destroyed and there are rebel units left,
        // they can form a rebel group again and battle with
        // the attacking nation.
        //
        //----------------------------------------------//

        set_hostile_nation(UnitArray[attackerUnitRecno].nation_recno);
    }

    public void set_hostile_nation(int nationRecno)
    {
        if (nationRecno == 0)
            return;

        hostile_nation_bits |= (0x1 << nationRecno);
    }

    public void reset_hostile_nation(int nationRecno)
    {
        if (nationRecno == 0)
            return;

        hostile_nation_bits &= ~(0x1 << nationRecno);
    }

    public bool is_hostile_nation(int nationRecno)
    {
        if (nationRecno == 0)
            return false;

        return (hostile_nation_bits & (0x1 << nationRecno)) != 0;
    }

    public void think_new_action()
    {
        if (UnitArray.IsDeleted(leader_unit_recno))
            return;

        bool rc = false;

        switch (Misc.Random(4))
        {
            case 0:
                rc = think_settle_new();
                break;

            case 1:
                rc = think_settle_to();
                break;

            case 2:
                rc = think_capture_attack_town();
                break;

            case 3:
                rc = think_attack_firm();
                break;
        }

        if (rc)
            execute_new_action();
    }

    public void think_cur_action()
    {
        //------ if the rebel is attacking a town -------//

        if (action_mode == REBEL_ATTACK_TOWN)
        {
            //----- the town has already been captured -----//

            if (TownArray.IsDeleted(action_para) || TownArray[action_para].NationId == 0)
            {
                //--- stop all units that are still attacking the town ---//

                stop_all_rebel_unit();
                set_action(REBEL_IDLE);
            }
        }

        //----- if the rebel should be attacking a firm -----//

        if (action_mode == REBEL_ATTACK_FIRM)
        {
            if (FirmArray.IsDeleted(action_para) || FirmArray[action_para].nation_recno == 0)
            {
                //--- stop all units that are still attacking the firm ---//

                stop_all_rebel_unit();

                set_action(REBEL_IDLE);
            }
        }
    }

    public void think_town_action()
    {
        if (town_recno == 0 || mobile_rebel_count > 0) // only when all rebel units has settled in the town
            return;

        //----- neutralize to an independent town -----//

        if (Misc.Random(10) == 0)
        {
            turn_indepedent();
        }

        //---------- form a nation ---------//

        else if (Misc.Random(10) == 0)
        {
            if (TownArray[town_recno].Population >= 20 && NationArray.can_form_new_ai_nation())
            {
                TownArray[town_recno].FormNewNation();
            }
        }
    }

    public bool think_settle_new()
    {
        //------- get the leader unit's info -----------//

        Unit leaderUnit = UnitArray[leader_unit_recno];

        int leaderXLoc = leaderUnit.cur_x_loc();
        int leaderYLoc = leaderUnit.cur_y_loc();

        //----------------------------------------------//

        int xLoc2 = leaderXLoc + InternalConstants.TOWN_WIDTH - 1;
        int yLoc2 = leaderYLoc + InternalConstants.TOWN_HEIGHT - 1;

        if (xLoc2 >= GameConstants.MapSize)
        {
            xLoc2 = GameConstants.MapSize - 1;
            leaderXLoc = xLoc2 - InternalConstants.TOWN_WIDTH + 1;
        }

        if (yLoc2 >= GameConstants.MapSize)
        {
            yLoc2 = GameConstants.MapSize - 1;
            leaderYLoc = yLoc2 - InternalConstants.TOWN_HEIGHT + 1;
        }

        int regionId = World.get_region_id(leaderXLoc, leaderYLoc);

        if (World.locate_space(ref leaderXLoc, ref leaderYLoc, xLoc2, yLoc2,
                InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, UnitConstants.UNIT_LAND, regionId, true))
        {
            action_mode = REBEL_SETTLE_NEW;
            action_para = leaderXLoc;
            action_para2 = leaderYLoc;

            return true;
        }

        return false;
    }

    public bool think_settle_to()
    {
        Unit leaderUnit = UnitArray[leader_unit_recno];
        int curRegionId = World.get_region_id(leaderUnit.cur_x_loc(), leaderUnit.cur_y_loc());

        foreach (Town town in TownArray.EnumerateRandom())
        {
            if (town.RebelId == 0)
                continue;

            if (World.get_region_id(town.LocX1, town.LocY1) != curRegionId)
                continue;

            if (leaderUnit.race_id == town.MajorityRace())
            {
                action_mode = REBEL_SETTLE_TO;
                action_para = town.LocX1;
                action_para2 = town.LocY1;
                return true;
            }
        }

        return false;
    }

    public bool think_capture_attack_town()
    {
        //------- get the leader unit's info -----------//

        Unit leaderUnit = UnitArray[leader_unit_recno];
        int leaderXLoc = leaderUnit.cur_x_loc();
        int leaderYLoc = leaderUnit.cur_y_loc();
        int curRegionId = World.get_region_id(leaderUnit.cur_x_loc(), leaderUnit.cur_y_loc());

        //----------------------------------------------//

        int actionMode = REBEL_ATTACK_TOWN;

        int bestTownRecno = 0;
        int closestTownDistance = Int32.MaxValue;

        foreach (Town town in TownArray.EnumerateRandom())
        {
            if (!is_hostile_nation(town.NationId))
                continue;

            if (World.get_region_id(town.LocX1, town.LocY1) != curRegionId)
                continue;

            int townDistance = Misc.rects_distance(leaderXLoc, leaderYLoc, leaderXLoc, leaderYLoc,
                town.LocX1, town.LocY1, town.LocX2, town.LocY2);

            if (townDistance < closestTownDistance)
            {
                closestTownDistance = townDistance;
                bestTownRecno = town.TownId;
            }
        }

        if (bestTownRecno != 0)
        {
            action_mode = actionMode;
            action_para = bestTownRecno; // attack this town
            return true;
        }

        return false;
    }

    public bool think_attack_firm()
    {
        //------- get the leader unit's info -----------//

        Unit leaderUnit = UnitArray[leader_unit_recno];
        int leaderXLoc = leaderUnit.cur_x_loc();
        int leaderYLoc = leaderUnit.cur_y_loc();
        int curRegionId = World.get_region_id(leaderUnit.cur_x_loc(), leaderUnit.cur_y_loc());

        //----------------------------------------------//

        int bestFirmRecno = 0;
        int closestFirmDistance = Int32.MaxValue;

        foreach (Firm firm in FirmArray.EnumerateRandom())
        {
            if (!is_hostile_nation(firm.nation_recno))
                continue;

            if (World.get_region_id(firm.loc_x1, firm.loc_y1) != curRegionId)
                continue;

            int firmDistance = Misc.points_distance(leaderXLoc, leaderYLoc, firm.center_x, firm.center_y);

            if (firmDistance < closestFirmDistance)
            {
                closestFirmDistance = firmDistance;
                bestFirmRecno = firm.firm_recno;
            }
        }

        if (bestFirmRecno != 0)
        {
            action_mode = REBEL_ATTACK_FIRM;
            action_para = bestFirmRecno; // attack this town
            return true;
        }

        return false;
    }

    public void execute_new_action()
    {
        //----- create an recno array of the rebel units ----//

        List<int> rebelRecnoArray = new List<int>();

        foreach (Unit unit in UnitArray)
        {
            if (unit.unit_mode == UnitConstants.UNIT_MODE_REBEL && unit.unit_mode_para == rebel_recno)
            {
                rebelRecnoArray.Add(unit.sprite_recno);
            }
        }

        if (rebelRecnoArray.Count == 0)
            return; // all rebel units are dead

        //-------- execute the new action now --------//

        switch (action_mode)
        {
            case REBEL_ATTACK_TOWN:
                Town town = TownArray[action_para];
                UnitArray.attack(town.LocX1, town.LocY1, false, rebelRecnoArray,
                    InternalConstants.COMMAND_AI, 0);
                break;

            case REBEL_ATTACK_FIRM:
                Firm firm = FirmArray[action_para];
                UnitArray.attack(firm.loc_x1, firm.loc_y1, false, rebelRecnoArray,
                    InternalConstants.COMMAND_AI, 0);
                break;

            case REBEL_SETTLE_NEW:
                UnitArray.settle(action_para, action_para2, false, 1, rebelRecnoArray);
                break;

            case REBEL_SETTLE_TO:
                UnitArray.assign(action_para, action_para2, false, 1, rebelRecnoArray);
                break;
        }
    }

    public void stop_all_rebel_unit()
    {
        foreach (Unit unit in UnitArray)
        {
            if (unit.unit_mode == UnitConstants.UNIT_MODE_REBEL && unit.unit_mode_para == rebel_recno)
            {
                unit.stop();
            }
        }
    }

    public void turn_indepedent()
    {
        TownArray[town_recno].RebelId = 0;

        RebelArray.DeleteRebel(this);
    }

    public int select_new_leader()
    {
        if (mobile_rebel_count == 0)
            return 0;

        foreach (Unit unit in UnitArray)
        {
            if (unit.unit_mode == UnitConstants.UNIT_MODE_REBEL && unit.unit_mode_para == rebel_recno)
            {
                unit.set_rank(Unit.RANK_GENERAL);
                leader_unit_recno = unit.sprite_recno;
                break;
            }
        }

        if (leader_unit_recno == 0)
            return 0;

        //--- update the leader_unit_renco of all units in this rebel group ---//

        foreach (Unit unit in UnitArray)
        {
            if (unit.unit_mode == UnitConstants.UNIT_MODE_REBEL && unit.unit_mode_para == rebel_recno)
            {
                unit.leader_unit_recno = leader_unit_recno;
            }
        }

        return 0;
    }

    public void process_leader_quit()
    {
        //-------------------------------------------//
        //
        // When the rebel leader gets killed, a new rebel unit
        // in the group is elected as the new leader.
        //
        // Some the rebel units leave the rebel group to:
        // surrender to a nation (moving into a town as town people)
        //
        //-------------------------------------------//

        //----- select a new unit as the leader ------//

        leader_unit_recno = 0;

        if (mobile_rebel_count > 0) // it must at least be 2: the dying leader + one rebel soldier
        {
            select_new_leader();
        }
        else
        {
            // if mobile_label_count is 0, the rebel group must have a town

            // if the rebel has a town, leader_unit_recno can be 0
        }

        //------ some the rebel units leave the rebel group -----//

        if (mobile_rebel_count > 1)
        {
            //---- surrender to a nation ----//

            int maxReputation = 0, bestNationRecno = 0;

            foreach (Nation nation in NationArray)
            {
                if (nation.reputation > maxReputation)
                {
                    maxReputation = (int)nation.reputation;
                    bestNationRecno = nation.nation_recno;
                }
            }

            if (bestNationRecno == 0) // no nation has a positive reputation
                return;

            //------- process the rebel units -------//

            foreach (Unit unit in UnitArray)
            {
                if (unit.unit_mode == UnitConstants.UNIT_MODE_REBEL && unit.unit_mode_para == leader_unit_recno)
                {
                    unit.set_mode(0);
                    unit.change_nation(bestNationRecno);
                }
            }
        }
    }
}