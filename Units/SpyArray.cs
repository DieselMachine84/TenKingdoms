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

    protected override Spy CreateNewObject(int objectType)
    {
        return new Spy();
    }

    public Spy AddSpy(int unitId, int spySkill)
    {
        Spy spy = CreateNew();
        spy.Init(unitId, spySkill);
        return spy;
    }

    public void DeleteSpy(Spy spy)
    {
        spy.Deinit();
        Delete(spy.SpyId);
    }

    public void NextDay()
    {
        foreach (Spy spy in this)
        {
            spy.NextDay();

            if (IsDeleted(spy.SpyId))
                continue;

            if (NationArray[spy.TrueNationId].is_ai())
                spy.ProcessAI();
        }

        if (Info.TotalDays % 15 == 0)
            ProcessSabotage();
    }

    public int FindTownSpy(int townId, int raceId, int spySeq)
    {
        int matchCount = 0;

        foreach (Spy spy in this)
        {
            if (spy.SpyPlace == Spy.SPY_TOWN && spy.SpyPlaceId == townId && spy.RaceId == raceId)
            {
                matchCount++;
                if (matchCount == spySeq)
                    return spy.SpyId;
            }
        }

        return 0;
    }

    private void ProcessSabotage()
    {
        //-------- reset firms' SabotageLevel -------//

        foreach (Firm firm in FirmArray)
        {
            firm.sabotage_level = 0;
        }

        //------- increase firms' SabotageLevel -----//

        foreach (Spy spy in this)
        {
            if (spy.ActionMode == Spy.SPY_SABOTAGE)
            {
                Firm firm = FirmArray[spy.SpyPlaceId];

                firm.sabotage_level += spy.SpySkill / 2;

                if (firm.sabotage_level > 100)
                    firm.sabotage_level = 100;
            }
        }
    }

    public void UpdateFirmSpyCount(int firmRecno)
    {
        Firm firm = FirmArray[firmRecno];

        firm.player_spy_count = 0;

        foreach (Spy spy in this)
        {
            if (spy.SpyPlace == Spy.SPY_FIRM && spy.SpyPlaceId == firmRecno && spy.TrueNationId == NationArray.player_recno)
            {
                firm.player_spy_count++;
            }
        }
    }

    public void ChangeCloakedNation(int spyPlace, int spyPlaceId, int fromNationId, int toNationId)
    {
        foreach (Spy spy in this)
        {
            if (spy.CloakedNationId != fromNationId)
                continue;

            if (spy.SpyPlace != spyPlace)
                continue;

            //--- check if the spy is in the specific firm or town ---//

            // only check spyPlacePara when spyPlace is SPY_TOWN or SPY_FIRM
            if (spyPlace == Spy.SPY_FIRM || spyPlace == Spy.SPY_TOWN)
            {
                if (spy.SpyPlaceId != spyPlaceId)
                    continue;
            }

            if (spyPlace == spy.SpyPlace && spyPlaceId == spy.SpyPlaceId && spy.TrueNationId == toNationId)
                spy.ActionMode = Spy.SPY_IDLE;

            //----- if the spy is associated with a unit (mobile or firm overseer), we call Unit.SpyChangeNation() ---//

            if (spyPlace == Spy.SPY_FIRM)
            {
                int firmOverseerId = FirmArray[spy.SpyPlaceId].overseer_recno;

                if (firmOverseerId != 0 && UnitArray[firmOverseerId].SpyId == spy.SpyId)
                {
                    UnitArray[firmOverseerId].SpyChangeNation(toNationId, InternalConstants.COMMAND_AUTO);
                    continue;
                }
            }
            else if (spyPlace == Spy.SPY_MOBILE)
            {
                UnitArray[spy.SpyPlaceId].SpyChangeNation(toNationId, InternalConstants.COMMAND_AUTO);
                continue;
            }

            //---- otherwise, just change the spy cloak ----//

            spy.CloakedNationId = toNationId;
        }
    }

    public void SetActionMode(int spyPlace, int spyPlacePara, int actionMode)
    {
        foreach (Spy spy in this)
        {
            if (spy.SpyPlace == spyPlace && spy.SpyPlaceId == spyPlacePara)
            {
                spy.ActionMode = actionMode;
            }
        }
    }

    public bool CatchSpy(int spyPlace, int spyPlaceId)
    {
        int nationId = 0, totalPop = 0;

        if (spyPlace == Spy.SPY_TOWN)
        {
            Town town = TownArray[spyPlaceId];

            nationId = town.NationId;
            totalPop = town.Population;
        }
        else if (spyPlace == Spy.SPY_FIRM)
        {
            Firm firm = FirmArray[spyPlaceId];

            nationId = firm.nation_recno;
            totalPop = firm.workers.Count + (firm.overseer_recno != 0 ? 1 : 0);
        }

        //--- calculate the total of anti-spy skill in this town ----//

        int enemySpyCount = 0, counterSpySkill = 0;

        foreach (Spy spy in this)
        {
            if (spy.SpyPlace == spyPlace && spy.SpyPlaceId == spyPlaceId)
            {
                if (spy.TrueNationId == nationId)
                    counterSpySkill += spy.SpySkill;
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
            if (spy.ActionMode == Spy.SPY_IDLE) // it is very hard to get caught in sleep mode
                continue;

            // doesn't get caught in sleep mode
            if (spy.SpyPlace == spyPlace && spy.SpyPlaceId == spyPlaceId && spy.TrueNationId != nationId)
            {
                int escapeChance = 100 + spy.SpySkill - counterSpySkill;

                escapeChance = Math.Max(spy.SpySkill / 10, escapeChance);

                if (Misc.Random(escapeChance) == 0)
                {
                    spy.GetKilled(); // only catch one spy per calling
                    return true;
                }
            }
        }

        return false;
    }

    public int TotalSpySkillLevel(int spyPlace, int spyPlaceId, int spyNationId, out int spyCount)
    {
        spyCount = 0;

        int totalSpyLevel = 0;

        foreach (Spy spy in this)
        {
            if (spy.TrueNationId != spyNationId)
                continue;

            if (spy.SpyPlace != spyPlace)
                continue;

            if (spy.SpyPlaceId != spyPlaceId)
                continue;

            spyCount++;

            totalSpyLevel += spy.SpySkill;
        }

        return totalSpyLevel;
    }

    public int NeededViewSecretSkill(int viewMode)
    {
        //TODO rewrite

        return 0;
    }

    public void AISpyTownRebel(int townId)
    {
        foreach (Spy spy in this)
        {
            if (spy.SpyPlace == Spy.SPY_TOWN && spy.SpyPlaceId == townId && NationArray[spy.TrueNationId].is_ai())
            {
                //-------- mobilize the spy ----------//

                Unit unit = spy.MobilizeTownSpy();

                //----- think new action for the spy ------//

                if (unit != null)
                    spy.ThinkMobileSpyNewAction();
            }
        }
    }
}