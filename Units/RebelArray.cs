using System;

namespace TenKingdoms;

public class RebelArray : DynArray<Rebel>
{
    private UnitArray UnitArray => Sys.Instance.UnitArray;
    private TownArray TownArray => Sys.Instance.TownArray;

    protected override Rebel CreateNewObject(int objectId)
    {
        return new Rebel();
    }

    public Rebel AddRebel(int unitRecno, int hostileNationRecno, int actionMode = Rebel.REBEL_IDLE, int actionPara = 0)
    {
        //------------------------------------------//
        //	See if there are a rebel group nearby
        // with the same objectives, join the rebel
        // group without creating a new one.
        //------------------------------------------//

        foreach (Rebel rebel in EnumerateRandom())
        {
            if (rebel.action_mode == actionMode && rebel.action_para == actionPara)
            {
                rebel.join(unitRecno); // join the rebel group
                return rebel;
            }
        }

        //-------- create a new rebel group ---------//

        Rebel newRebel = CreateNew();
        newRebel.rebel_recno = nextRecNo;
        nextRecNo++;
        newRebel.Init(unitRecno, hostileNationRecno, actionMode, actionPara);

        return newRebel;
    }

    public void DeleteRebel(Rebel rebel)
    {
        int rebelRecno = rebel.rebel_recno;
        rebel.Deinit();
        Delete(rebelRecno);
    }
    public void DeleteRebel(int rebelRecno)
    {
        DeleteRebel(this[rebelRecno]);
    }

    public void drop_rebel_identity(int unitRecno)
    {
        Unit unit = UnitArray[unitRecno];

        //---- decrease the unit count of the rebel group ----//

        int rebelRecno = unit.unit_mode_para;
        Rebel rebel = this[rebelRecno];

        rebel.mobile_rebel_count--;

        unit.set_mode(0); // drop its rebel identity 

        //----- if all rebels are dead and the rebel doesn't occupy a town, del the rebel group ----//

        if (rebel.mobile_rebel_count == 0 && rebel.town_recno == 0)
        {
            DeleteRebel(rebel);
            return;
        }

        //----- when the rebel leader is killed -----//

        if (rebel.leader_unit_recno == unitRecno)
            rebel.process_leader_quit();
    }

    public void settle_town(int unitRecno, int townRecno)
    {
        Unit unit = UnitArray[unitRecno];

        //---- decrease the unit count of the rebel group ----//

        int rebelRecno = unit.unit_mode_para;
        Rebel rebel = this[rebelRecno];

        rebel.mobile_rebel_count--;

        //--------- settle in a town ----------//

        Town town = TownArray[townRecno];

        if (rebel.town_recno == 0 && town.RebelId == 0)
        {
            rebel.town_recno = townRecno;
            town.RebelId = rebelRecno;
        }
    }

    public void next_day()
    {
        foreach (Rebel rebel in this)
        {
            rebel.next_day();
        }
    }

    public void stop_attack_town(int townRecno)
    {
        foreach (Rebel rebel in this)
        {
            if (rebel.action_mode == Rebel.REBEL_ATTACK_TOWN)
            {
                if (rebel.action_para == townRecno)
                {
                    rebel.set_action(Rebel.REBEL_IDLE);
                    rebel.stop_all_rebel_unit();
                    break;
                }
            }
        }
    }

    public void stop_attack_firm(int firmRecno)
    {
        foreach (Rebel rebel in this)
        {
            if (rebel.action_mode == Rebel.REBEL_ATTACK_FIRM)
            {
                if (rebel.action_para == firmRecno)
                {
                    rebel.set_action(Rebel.REBEL_IDLE);
                    rebel.stop_all_rebel_unit();
                    return;
                }
            }
        }
    }

    public void stop_attack_nation(int nationRecno)
    {
        foreach (Rebel rebel in this)
        {
            rebel.reset_hostile_nation(nationRecno);
        }
    }
}