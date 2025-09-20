using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class Rebel : IIdObject
{
    public const int REBEL_IDLE = 1;
    public const int REBEL_ATTACK_TOWN = 2; // Attack town without capturing
    public const int REBEL_ATTACK_FIRM = 3; // Attack firm without capturing
    public const int REBEL_SETTLE_NEW = 4; // Settle to a new town
    public const int REBEL_SETTLE_TO = 5; // Settle to an existing town 

    public int RebelId { get; private set; }
    public int LeaderUnitId { get; private set; }
    public int ActionMode { get; set; }
    public int ActionParam { get; private set; }
    public int ActionParam2 { get; private set; }
    public int MobileRebelCount  { get; set; } // no. of units in this rebel group
    public int TownId  { get; set; } // the town controlled by the rebel group, one rebel group can only control one town
    private int HostileNationBits  { get; set; }

    private Info Info => Sys.Instance.Info;
    private World World => Sys.Instance.World;
    private NationArray NationArray => Sys.Instance.NationArray;
    private TownArray TownArray => Sys.Instance.TownArray;
    private FirmArray FirmArray => Sys.Instance.FirmArray;
    private UnitArray UnitArray => Sys.Instance.UnitArray;
    private RebelArray RebelArray => Sys.Instance.RebelArray;

    public Rebel()
    {
    }
    
    void IIdObject.SetId(int id)
    {
        RebelId = id;
    }

    public void Init(Unit unit, int hostileNationId, int actionMode, int actionParam)
    {
        LeaderUnitId = unit.SpriteId;
        ActionMode = actionMode;
        ActionParam = actionParam;
        MobileRebelCount = 1;
        SetHostileNation(hostileNationId);

        unit.SetMode(UnitConstants.UNIT_MODE_REBEL, RebelId);
    }

    public void Deinit()
    {
        foreach (Unit unit in UnitArray)
        {
            if (unit.UnitMode == UnitConstants.UNIT_MODE_REBEL && unit.UnitModeParam == RebelId)
            {
                unit.SetMode(0);
            }
        }
    }

    public void NextDay()
    {
        //---- if the rebels has a town and this rebel is a defender from a town ----//

        if (TownId != 0)
        {
            //--- check if the rebel town is destroyed ----//

            if (TownArray.IsDeleted(TownId))
            {
                TownId = 0;

                //--- if the town has been destroyed, the rebel group becomes mobile again, and a new leader needs to be selected.

                if (MobileRebelCount > 0 && LeaderUnitId == 0)
                {
                    SelectNewLeader();
                }
            }

            return; // don't think new action as this rebel defender need to go back to its town.
        }

        //------- no rebels left in the group --------//

        if (TownId == 0 && MobileRebelCount == 0)
        {
            RebelArray.DeleteRebel(this);
            return;
        }

        //---------------------------------------------//

        if (MobileRebelCount > 0) // if there are mobile rebel units on the map
        {
            if (ActionMode == REBEL_IDLE)
                ThinkNewAction(); // think about a new action
            else
                ThinkCurrentAction(); // think if there should be any changes to the current action
        }
        else // if there are no mobile rebel units
        {
            if (TownId > 0)
            {
                // TODO check if this code is executed
                if (Info.TotalDays % 30 == RebelId % 30) // if the rebel group has a town
                    ThinkTownAction();
            }
        }
    }

    public void SetAction(int actionMode, int actionParam = 0, int actionParam2 = 0)
    {
        ActionMode = actionMode;
        ActionParam = actionParam;
        ActionParam2 = actionParam2;
    }

    public void Join(Unit unit)
    {
        unit.SetMode(UnitConstants.UNIT_MODE_REBEL, RebelId);
        MobileRebelCount++;
    }

    public void TownBeingAttacked(int attackerUnitId)
    {
        //----------------------------------------------//
        //
        // Set the hostile nation. So that if the rebel town is destroyed and there are rebel units left,
        // they can form a rebel group again and battle with the attacking nation.
        //
        //----------------------------------------------//

        SetHostileNation(UnitArray[attackerUnitId].NationId);
    }

    private void SetHostileNation(int nationId)
    {
        if (nationId == 0)
            return;

        HostileNationBits |= (0x1 << nationId);
    }

    public void ResetHostileNation(int nationId)
    {
        if (nationId == 0)
            return;

        HostileNationBits &= ~(0x1 << nationId);
    }

    public bool IsHostileNation(int nationId)
    {
        if (nationId == 0)
            return false;

        return (HostileNationBits & (0x1 << nationId)) != 0;
    }

    private void ThinkNewAction()
    {
        if (UnitArray.IsDeleted(LeaderUnitId))
            return;

        bool rc = false;

        switch (Misc.Random(4))
        {
            case 0:
                rc = ThinkSettleNew();
                break;

            case 1:
                rc = ThinkSettleTo();
                break;

            case 2:
                rc = ThinkCaptureAttackTown();
                break;

            case 3:
                rc = ThinkAttackFirm();
                break;
        }

        if (rc)
            ExecuteNewAction();
    }

    private void ThinkCurrentAction()
    {
        //------ if the rebel is attacking a town -------//

        if (ActionMode == REBEL_ATTACK_TOWN)
        {
            //----- the town has already been captured -----//

            if (TownArray.IsDeleted(ActionParam) || TownArray[ActionParam].NationId == 0)
            {
                //--- stop all units that are still attacking the town ---//

                StopAllRebelUnit();
                SetAction(REBEL_IDLE);
            }
        }

        //----- if the rebel should be attacking a firm -----//

        if (ActionMode == REBEL_ATTACK_FIRM)
        {
            if (FirmArray.IsDeleted(ActionParam) || FirmArray[ActionParam].NationId == 0)
            {
                //--- stop all units that are still attacking the firm ---//

                StopAllRebelUnit();
                SetAction(REBEL_IDLE);
            }
        }
    }

    private void ThinkTownAction()
    {
        if (TownId == 0 || MobileRebelCount > 0) // only when all rebel units has settled in the town
            return;

        //----- neutralize to an independent town -----//

        if (Misc.Random(10) == 0)
        {
            TurnIndependent();
        }

        //---------- form a nation ---------//

        else if (Misc.Random(10) == 0)
        {
            if (TownArray[TownId].Population >= 20 && NationArray.can_form_new_ai_nation())
            {
                TownArray[TownId].FormNewNation();
            }
        }
    }

    private bool ThinkSettleNew()
    {
        //------- get the leader unit's info -----------//

        Unit leaderUnit = UnitArray[LeaderUnitId];

        int leaderLocX = leaderUnit.CurLocX;
        int leaderLocY = leaderUnit.CurLocY;

        //----------------------------------------------//

        int locX2 = leaderLocX + InternalConstants.TOWN_WIDTH - 1;
        int locY2 = leaderLocY + InternalConstants.TOWN_HEIGHT - 1;

        if (locX2 >= GameConstants.MapSize)
        {
            locX2 = GameConstants.MapSize - 1;
            leaderLocX = locX2 - InternalConstants.TOWN_WIDTH + 1;
        }

        if (locY2 >= GameConstants.MapSize)
        {
            locY2 = GameConstants.MapSize - 1;
            leaderLocY = locY2 - InternalConstants.TOWN_HEIGHT + 1;
        }

        int regionId = World.GetRegionId(leaderLocX, leaderLocY);

        if (World.LocateSpace(ref leaderLocX, ref leaderLocY, locX2, locY2,
                InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, UnitConstants.UNIT_LAND, regionId, true))
        {
            ActionMode = REBEL_SETTLE_NEW;
            ActionParam = leaderLocX;
            ActionParam2 = leaderLocY;

            return true;
        }

        return false;
    }

    private bool ThinkSettleTo()
    {
        Unit leaderUnit = UnitArray[LeaderUnitId];
        int curRegionId = World.GetRegionId(leaderUnit.CurLocX, leaderUnit.CurLocY);

        foreach (Town town in TownArray.EnumerateRandom())
        {
            if (town.RebelId == 0)
                continue;

            if (World.GetRegionId(town.LocX1, town.LocY1) != curRegionId)
                continue;

            if (leaderUnit.RaceId == town.MajorityRace())
            {
                ActionMode = REBEL_SETTLE_TO;
                ActionParam = town.LocX1;
                ActionParam2 = town.LocY1;
                return true;
            }
        }

        return false;
    }

    private bool ThinkCaptureAttackTown()
    {
        //------- get the leader unit's info -----------//

        Unit leaderUnit = UnitArray[LeaderUnitId];
        int leaderLocX = leaderUnit.CurLocX;
        int leaderLocY = leaderUnit.CurLocY;
        int curRegionId = World.GetRegionId(leaderUnit.CurLocX, leaderUnit.CurLocY);

        //----------------------------------------------//

        int bestTownId = 0;
        int closestTownDistance = Int32.MaxValue;

        foreach (Town town in TownArray.EnumerateRandom())
        {
            if (!IsHostileNation(town.NationId))
                continue;

            if (World.GetRegionId(town.LocX1, town.LocY1) != curRegionId)
                continue;

            int townDistance = Misc.rects_distance(leaderLocX, leaderLocY, leaderLocX, leaderLocY,
                town.LocX1, town.LocY1, town.LocX2, town.LocY2);

            if (townDistance < closestTownDistance)
            {
                closestTownDistance = townDistance;
                bestTownId = town.TownId;
            }
        }

        if (bestTownId != 0)
        {
            ActionMode = REBEL_ATTACK_TOWN;
            ActionParam = bestTownId; // attack this town
            return true;
        }

        return false;
    }

    private bool ThinkAttackFirm()
    {
        //------- get the leader unit's info -----------//

        Unit leaderUnit = UnitArray[LeaderUnitId];
        int leaderLocX = leaderUnit.CurLocX;
        int leaderLocY = leaderUnit.CurLocY;
        int curRegionId = World.GetRegionId(leaderUnit.CurLocX, leaderUnit.CurLocY);

        //----------------------------------------------//

        int bestFirmId = 0;
        int closestFirmDistance = Int32.MaxValue;

        foreach (Firm firm in FirmArray.EnumerateRandom())
        {
            if (!IsHostileNation(firm.NationId))
                continue;

            if (World.GetRegionId(firm.LocX1, firm.LocY1) != curRegionId)
                continue;

            int firmDistance = Misc.points_distance(leaderLocX, leaderLocY, firm.LocCenterX, firm.LocCenterY);

            if (firmDistance < closestFirmDistance)
            {
                closestFirmDistance = firmDistance;
                bestFirmId = firm.FirmId;
            }
        }

        if (bestFirmId != 0)
        {
            ActionMode = REBEL_ATTACK_FIRM;
            ActionParam = bestFirmId; // attack this firm
            return true;
        }

        return false;
    }

    private void ExecuteNewAction()
    {
        List<int> rebelUnits = new List<int>();

        foreach (Unit unit in UnitArray)
        {
            if (unit.UnitMode == UnitConstants.UNIT_MODE_REBEL && unit.UnitModeParam == RebelId)
            {
                rebelUnits.Add(unit.SpriteId);
            }
        }

        if (rebelUnits.Count == 0)
            return; // all rebel units are dead

        //-------- execute the new action now --------//

        switch (ActionMode)
        {
            case REBEL_ATTACK_TOWN:
                Town town = TownArray[ActionParam];
                UnitArray.Attack(town.LocX1, town.LocY1, false, rebelUnits, InternalConstants.COMMAND_AI, 0);
                break;

            case REBEL_ATTACK_FIRM:
                Firm firm = FirmArray[ActionParam];
                UnitArray.Attack(firm.LocX1, firm.LocY1, false, rebelUnits, InternalConstants.COMMAND_AI, 0);
                break;

            case REBEL_SETTLE_NEW:
                UnitArray.Settle(ActionParam, ActionParam2, false, rebelUnits, InternalConstants.COMMAND_AI);
                break;

            case REBEL_SETTLE_TO:
                UnitArray.Assign(ActionParam, ActionParam2, false, rebelUnits, InternalConstants.COMMAND_AI);
                break;
        }
    }

    public void StopAllRebelUnit()
    {
        foreach (Unit unit in UnitArray)
        {
            if (unit.UnitMode == UnitConstants.UNIT_MODE_REBEL && unit.UnitModeParam == RebelId)
            {
                unit.Stop();
            }
        }
    }

    private void TurnIndependent()
    {
        TownArray[TownId].RebelId = 0;

        RebelArray.DeleteRebel(this);
    }

    private void SelectNewLeader()
    {
        if (MobileRebelCount == 0)
            return;

        foreach (Unit unit in UnitArray)
        {
            if (unit.UnitMode == UnitConstants.UNIT_MODE_REBEL && unit.UnitModeParam == RebelId)
            {
                unit.SetRank(Unit.RANK_GENERAL);
                LeaderUnitId = unit.SpriteId;
                break;
            }
        }

        if (LeaderUnitId == 0)
            return;

        //--- update the LeaderUnitId of all units in this rebel group ---//

        foreach (Unit unit in UnitArray)
        {
            if (unit.UnitMode == UnitConstants.UNIT_MODE_REBEL && unit.UnitModeParam == RebelId)
            {
                unit.LeaderId = LeaderUnitId;
            }
        }

        return;
    }

    public void ProcessLeaderQuit()
    {
        //-------------------------------------------//
        //
        // When the rebel leader gets killed, a new rebel unit in the group is elected as the new leader.
        //
        // Some the rebel units leave the rebel group to:
        // surrender to a nation (moving into a town as town people)
        //
        //-------------------------------------------//

        //----- select a new unit as the leader ------//

        LeaderUnitId = 0;

        if (MobileRebelCount > 0) // it must at least be 2: the dying leader + one rebel soldier
        {
            SelectNewLeader();
        }
        else
        {
            // if MobileRebelCount is 0, the rebel group must have a town

            // if the rebel has a town, LeaderUnitId can be 0
        }

        //------ some the rebel units leave the rebel group -----//

        if (MobileRebelCount > 1)
        {
            //---- surrender to a nation ----//

            int maxReputation = 0, bestNationId = 0;

            foreach (Nation nation in NationArray)
            {
                if (nation.reputation > maxReputation)
                {
                    maxReputation = (int)nation.reputation;
                    bestNationId = nation.nation_recno;
                }
            }

            if (bestNationId == 0) // no nation has a positive reputation
                return;

            //------- process the rebel units -------//

            foreach (Unit unit in UnitArray)
            {
                // TODO unit.UnitModeParam has RebelId
                
                if (unit.UnitMode == UnitConstants.UNIT_MODE_REBEL && unit.UnitModeParam == LeaderUnitId)
                {
                    unit.SetMode(0);
                    unit.ChangeNation(bestNationId);
                }
            }
        }
    }
}