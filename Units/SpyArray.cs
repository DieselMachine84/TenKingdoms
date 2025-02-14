using System;

namespace TenKingdoms;

public class SpyArray : DynArray<Spy>
{
    private ConfigAdv ConfigAdv => Sys.Instance.ConfigAdv;
    private Info Info => Sys.Instance.Info;
    private NationArray NationArray => Sys.Instance.NationArray;
    private FirmArray FirmArray => Sys.Instance.FirmArray;
    private TownArray TownArray => Sys.Instance.TownArray;
    private UnitArray UnitArray => Sys.Instance.UnitArray;

    public SpyArray()
    {
    }

    protected override Spy CreateNewObject(int objectId)
    {
        return new Spy();
    }

    public override bool IsDeleted(int recNo)
    {
        if (base.IsDeleted(recNo))
            return true;

        return this[recNo].spy_recno == 0;
    }

    public Spy AddSpy(int unitRecno, int spySkill)
    {
        Spy spy = CreateNew();
        spy.spy_recno = nextRecNo;
        nextRecNo++;
        spy.Init(unitRecno, spySkill);

        return spy;
    }

    public void DeleteSpy(Spy spy)
    {
        int spyRecno = spy.spy_recno;
        spy.Deinit();
        Delete(spyRecno);
    }

    public void NextDay()
    {
        foreach (Spy spy in this)
        {
            spy.next_day();

            if (IsDeleted(spy.spy_recno))
                continue;

            if (NationArray[spy.true_nation_recno].is_ai())
                spy.process_ai();
        }

        if (Info.TotalDays % 15 == 0)
            process_sabotage();
    }

    public int find_town_spy(int townRecno, int raceId, int spySeq)
    {
        int matchCount = 0;

        foreach (Spy spy in this)
        {
            if (spy.spy_place == Spy.SPY_TOWN && spy.spy_place_para == townRecno && spy.race_id == raceId)
            {
                matchCount++;
                if (matchCount == spySeq)
                    return spy.spy_recno;
            }
        }

        return 0;
    }

    public void process_sabotage()
    {
        //-------- reset firms' sabotage_level -------//

        foreach (Firm firm in FirmArray)
        {
            firm.sabotage_level = 0;
        }

        //------- increase firms' sabotage_level -----//

        foreach (Spy spy in this)
        {
            if (spy.action_mode == Spy.SPY_SABOTAGE)
            {
                Firm firm = FirmArray[spy.spy_place_para];

                firm.sabotage_level += spy.spy_skill / 2;

                if (firm.sabotage_level > 100)
                    firm.sabotage_level = 100;
            }
        }
    }

    public void mobilize_all_spy(int spyPlace, int spyPlacePara, int nationRecno)
    {
        foreach (Spy spy in this)
        {
            if (spy.spy_place == spyPlace && spy.spy_place_para == spyPlacePara && spy.true_nation_recno == nationRecno)
            {
                if (spy.spy_place == Spy.SPY_TOWN)
                    spy.mobilize_town_spy();

                else if (spy.spy_place == Spy.SPY_FIRM)
                    spy.mobilize_firm_spy();
            }
        }
    }

    public void update_firm_spy_count(int firmRecno)
    {
        //---- recalculate Firm::player_spy_count -----//

        Firm firm = FirmArray[firmRecno];

        firm.player_spy_count = 0;

        foreach (Spy spy in this)
        {
            if (spy.spy_place == Spy.SPY_FIRM &&
                spy.spy_place_para == firmRecno &&
                spy.true_nation_recno == NationArray.player_recno)
            {
                firm.player_spy_count++;
            }
        }
    }

    public void change_cloaked_nation(int spyPlace, int spyPlacePara, int fromNationRecno, int toNationRecno)
    {
        foreach (Spy spy in this)
        {
            if (spy.cloaked_nation_recno != fromNationRecno)
                continue;

            if (spy.spy_place != spyPlace)
                continue;

            //--- check if the spy is in the specific firm or town ---//

            // only check spy_place_para when spyPlace is SPY_TOWN or SPY_FIRM
            if (spyPlace == Spy.SPY_FIRM || spyPlace == Spy.SPY_TOWN)
            {
                if (spy.spy_place_para != spyPlacePara)
                    continue;
            }

            if (spyPlace == spy.spy_place && spyPlacePara == spy.spy_place_para &&
                spy.true_nation_recno == toNationRecno)
                spy.set_action_mode(Spy.SPY_IDLE);

            //----- if the spy is associated with a unit (mobile or firm overseer), we call Unit.spy_change_nation() ---//

            if (spyPlace == Spy.SPY_FIRM)
            {
                int firmOverseerRecno = FirmArray[spy.spy_place_para].overseer_recno;

                if (firmOverseerRecno != 0 && UnitArray[firmOverseerRecno].spy_recno == spy.spy_recno)
                {
                    UnitArray[firmOverseerRecno].spy_change_nation(toNationRecno, InternalConstants.COMMAND_AUTO);
                    continue;
                }
            }
            else if (spyPlace == Spy.SPY_MOBILE)
            {
                UnitArray[spy.spy_place_para].spy_change_nation(toNationRecno, InternalConstants.COMMAND_AUTO);
                continue;
            }

            //---- otherwise, just change the spy cloak ----//

            spy.cloaked_nation_recno = toNationRecno;
        }
    }

    public void set_action_mode(int spyPlace, int spyPlacePara, int actionMode)
    {
        foreach (Spy spy in this)
        {
            if (spy.spy_place == spyPlace && spy.spy_place_para == spyPlacePara)
            {
                spy.set_action_mode(actionMode);
            }
        }
    }

    public bool catch_spy(int spyPlace, int spyPlacePara)
    {
        int nationRecno = 0, totalPop = 0;

        if (spyPlace == Spy.SPY_TOWN)
        {
            Town town = TownArray[spyPlacePara];

            nationRecno = town.NationId;
            totalPop = town.Population;
        }
        else if (spyPlace == Spy.SPY_FIRM)
        {
            Firm firm = FirmArray[spyPlacePara];

            nationRecno = firm.nation_recno;
            totalPop = firm.workers.Count + (firm.overseer_recno != 0 ? 1 : 0);
        }

        //--- calculate the total of anti-spy skill in this town ----//

        int enemySpyCount = 0, counterSpySkill = 0;

        foreach (Spy spy in this)
        {
            if (spy.spy_place == spyPlace && spy.spy_place_para == spyPlacePara)
            {
                if (spy.true_nation_recno == nationRecno)
                    counterSpySkill += spy.spy_skill;
                else
                    enemySpyCount++;
            }
        }

        //----- if all villagers are enemy spies ----//

        if (enemySpyCount == totalPop)
            return false;

        //-------- try to catch enemy spies now ------//

        foreach (Spy spy in this)
        {
            if (spy.action_mode == Spy.SPY_IDLE) // it is very hard to get caught in sleep mode
                continue;

            // doesn't get caught in sleep mode
            if (spy.spy_place == spyPlace && spy.spy_place_para == spyPlacePara && spy.true_nation_recno != nationRecno)
            {
                int escapeChance = 100 + spy.spy_skill - counterSpySkill;

                escapeChance = Math.Max(spy.spy_skill / 10, escapeChance);

                if (Misc.Random(escapeChance) == 0)
                {
                    spy.get_killed(); // only catch one spy per calling
                    return true;
                }
            }
        }

        return false;
    }

    public int total_spy_skill_level(int spyPlace, int spyPlacePara, int spyNationRecno, out int spyCount)
    {
        spyCount = 0;

        int totalSpyLevel = 0;

        foreach (Spy spy in this)
        {
            if (spy.true_nation_recno != spyNationRecno)
                continue;

            if (spy.spy_place != spyPlace)
                continue;

            if (spy.spy_place_para != spyPlacePara)
                continue;

            spyCount++;

            totalSpyLevel += spy.spy_skill;
        }

        return totalSpyLevel;
    }

    public int needed_view_secret_skill(int viewMode)
    {
        //TODO rewrite

        return 0;
    }

    public void ai_spy_town_rebel(int townRecno)
    {
        foreach (Spy spy in this)
        {
            if (spy.spy_place == Spy.SPY_TOWN &&
                spy.spy_place_para == townRecno &&
                NationArray[spy.true_nation_recno].is_ai())
            {
                //-------- mobilize the spy ----------//

                Unit unit = spy.mobilize_town_spy();

                //----- think new action for the spy ------//

                if (unit != null)
                    spy.think_mobile_spy_new_action();
            }
        }
    }
}