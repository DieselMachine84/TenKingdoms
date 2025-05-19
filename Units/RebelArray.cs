using System;

namespace TenKingdoms;

public class RebelArray : DynArray<Rebel>
{
    private UnitArray UnitArray => Sys.Instance.UnitArray;

    protected override Rebel CreateNewObject(int objectType)
    {
        return new Rebel();
    }

    public Rebel AddRebel(Unit rebelUnit, int hostileNationId, int actionMode = Rebel.REBEL_IDLE, int actionParam = 0)
    {
        //---------------------------------------------------------------//
        // See if there is a rebel group nearby with the same objectives,
        // join the rebel group without creating a new one.
        //---------------------------------------------------------------//

        foreach (Rebel rebel in EnumerateRandom())
        {
            if (rebel.ActionMode == actionMode && rebel.ActionParam == actionParam)
            {
                rebel.Join(rebelUnit); // join the rebel group
                return rebel;
            }
        }

        //-------- create a new rebel group ---------//

        Rebel newRebel = CreateNew();
        newRebel.Init(rebelUnit, hostileNationId, actionMode, actionParam);
        return newRebel;
    }

    public void DeleteRebel(Rebel rebel)
    {
        rebel.Deinit();
        Delete(rebel.RebelId);
    }

    public void DeleteRebel(int rebelId)
    {
        DeleteRebel(this[rebelId]);
    }

    public void NextDay()
    {
        foreach (Rebel rebel in this)
        {
            rebel.NextDay();
        }
    }
    
    public void DropRebelIdentity(int unitId)
    {
        Unit unit = UnitArray[unitId];
        int rebelId = unit.UnitModeParam;
        Rebel rebel = this[rebelId];
        rebel.MobileRebelCount--; // decrease the unit count of the rebel group
        unit.SetMode(0); // drop its rebel identity 

        //----- if all rebels are dead and the rebel doesn't occupy a town, del the rebel group ----//

        if (rebel.MobileRebelCount == 0 && rebel.TownId == 0)
        {
            DeleteRebel(rebel);
            return;
        }

        //----- when the rebel leader is killed -----//

        if (rebel.LeaderUnitId == unitId)
            rebel.ProcessLeaderQuit();
    }

    public void SettleTown(Unit unit, Town town)
    {
        int rebelId = unit.UnitModeParam;
        Rebel rebel = this[rebelId];
        rebel.MobileRebelCount--; // decrease the unit count of the rebel group

        //--------- settle in a town ----------//

        if (rebel.TownId == 0 && town.RebelId == 0)
        {
            rebel.TownId = town.TownId;
            town.RebelId = rebelId;
        }
    }

    public void StopAttackTown(int townId)
    {
        foreach (Rebel rebel in this)
        {
            if (rebel.ActionMode == Rebel.REBEL_ATTACK_TOWN)
            {
                if (rebel.ActionParam == townId)
                {
                    rebel.SetAction(Rebel.REBEL_IDLE);
                    rebel.StopAllRebelUnit();
                    break;
                }
            }
        }
    }

    public void StopAttackFirm(int firmId)
    {
        foreach (Rebel rebel in this)
        {
            if (rebel.ActionMode == Rebel.REBEL_ATTACK_FIRM)
            {
                if (rebel.ActionParam == firmId)
                {
                    rebel.SetAction(Rebel.REBEL_IDLE);
                    rebel.StopAllRebelUnit();
                    return;
                }
            }
        }
    }

    public void StopAttackNation(int nationId)
    {
        foreach (Rebel rebel in this)
        {
            rebel.ResetHostileNation(nationId);
        }
    }
}