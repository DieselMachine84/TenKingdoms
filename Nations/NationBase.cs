using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class NationBase : IIdObject
{
    public const int NATION_OWN = 1;
    public const int NATION_REMOTE = 2;
    public const int NATION_AI = 3;

    public const int NATION_HOSTILE = 0;
    public const int NATION_TENSE = 1;
    public const int NATION_NEUTRAL = 2;
    public const int NATION_FRIENDLY = 3;
    public const int NATION_ALLIANCE = 4;

    public const int RELATION_LEVEL_PER_STATUS = 20;

    public const int IMPORT_TYPE_COUNT = 3;
    public const int IMPORT_RAW = 0;
    public const int IMPORT_PRODUCT = 1;
    public const int IMPORT_TOTAL = 2;

    public const int INCOME_TYPE_COUNT = 8;
    public const int INCOME_SELL_GOODS = 0;
    public const int INCOME_EXPORTS = 1;
    public const int INCOME_TAX = 2;
    public const int INCOME_TREASURE = 3;
    public const int INCOME_FOREIGN_WORKER = 4;
    public const int INCOME_SELL_FIRM = 5;
    public const int INCOME_TRIBUTE = 6;
    public const int INCOME_CHEAT = 7;

    public const int EXPENSE_TYPE_COUNT = 16;
    public const int EXPENSE_GENERAL = 0;
    public const int EXPENSE_SPY = 1;
    public const int EXPENSE_MOBILE_UNIT = 2;
    public const int EXPENSE_CARAVAN = 3;
    public const int EXPENSE_WEAPON = 4;
    public const int EXPENSE_SHIP = 5;
    public const int EXPENSE_FIRM = 6;
    public const int EXPENSE_TRAIN_UNIT = 7;
    public const int EXPENSE_HIRE_UNIT = 8;
    public const int EXPENSE_REWARD_UNIT = 9;
    public const int EXPENSE_FOREIGN_WORKER = 10;
    public const int EXPENSE_GRANT_OWN_TOWN = 11;
    public const int EXPENSE_GRANT_OTHER_TOWN = 12;
    public const int EXPENSE_IMPORTS = 13;
    public const int EXPENSE_TRIBUTE = 14;
    public const int EXPENSE_BRIBE = 15;

    public int NationId { get; private set; }
    public int NationType { get; private set; }
    public int RaceId { get; private set; }
    public byte NationColor { get; set; }
    public int ColorSchemeId { get; set; }
    public int KingUnitId { get; set; }
    public int KingLeadership { get; set; }
    public int NationNameId { get; set; }
    public string NationNameString { get; set; }
    public int PlayerId { get; set; }

    public double Cash { get; set; }
    public double Food { get; set; }
    public double Reputation { get; set; }
    public double KillMonsterScore { get; set; }

    public bool IsAtWarToday { get; set; }
    public bool IsAtWarYesterday { get; set; }
    public DateTime LastWarDate { get; set; }
    public int LastAttackerUnitId { get; set; }
    public DateTime LastIndependentUnitJoinDate { get; set; }

    public int AutoCollectTaxLoyalty { get; set; }
    public int AutoGrantLoyalty { get; set; }

    public double CurYearProfit { get; set; }
    public double LastYearProfit { get; set; }

    public double[] CurYearIncomes { get; } = new double[INCOME_TYPE_COUNT];
    public double[] LastYearIncomes { get; } = new double[INCOME_TYPE_COUNT];
    public double CurYearIncome { get; set; }
    public double LastYearIncome { get; set; }
    public double CurYearFixedIncome { get; set; }
    public double LastYearFixedIncome { get; set; }

    public double[] CurYearExpenses { get; } = new double[EXPENSE_TYPE_COUNT];
    public double[] LastYearExpenses { get; } = new double[EXPENSE_TYPE_COUNT];
    public double CurYearExpense { get; set; }
    public double LastYearExpense { get; set; }
    public double CurYearFixedExpense { get; set; }
    public double LastYearFixedExpense { get; set; }

    public double CurYearCheat { get; set; }
    public double LastYearCheat { get; set; }

    public double CurYearFoodIn { get; set; }
    public double LastYearFoodIn { get; set; }
    public double CurYearFoodOut { get; set; }
    public double LastYearFoodOut { get; set; }
    public double CurYearFoodChange { get; set; }
    public double LastYearFoodChange { get; set; }

    public double CurYearReputationChange { get; set; }
    public double LastYearReputationChange { get; set; }

    public int NationFirmCount { get; set; }
    public DateTime LastBuildFirmDate { get; set; }
    public int[] KnowBases { get; } = new int[GameConstants.MAX_RACE];
    public int[] BaseCounts { get; } = new int[GameConstants.MAX_RACE];
    
    public int TotalPopulation { get; set; }
    public int TotalJoblessPopulation { get; set; }

    public int TotalUnitCount { get; set; }
    public int TotalHumanCount { get; set; }
    public int TotalGeneralCount { get; set; }
    public int TotalWeaponCount { get; set; }
    public int TotalShipCount { get; set; }
    public int TotalFirmCount { get; set; }
    public int TotalSpyCount { get; set; }
    public int TotalShipCombatLevel { get; set; }

    public int LargestTownId { get; set; } // the id of the biggest town of this nation
    public int LargestTownPop { get; set; }

    // no. of natural resources site this nation possesses
    public int[] RawCounts { get; } = new int[GameConstants.MAX_RAW];

    public int[] LastUnitNameIds { get; } = new int[UnitConstants.MAX_UNIT_TYPE];

    public int PopulationRating { get; set; }
    public int MilitaryRating { get; set; }
    public int EconomicRating { get; set; }
    public int OverallRating { get; set; }

    public int EnemySoldierKilled { get; set; }
    public int OwnSoldierKilled { get; set; }
    public int EnemyCivilianKilled { get; set; }
    public int OwnCivilianKilled { get; set; }
    public int EnemyWeaponDestroyed { get; set; }
    public int OwnWeaponDestroyed { get; set; }
    public int EnemyShipDestroyed { get; set; }
    public int OwnShipDestroyed { get; set; }
    public int EnemyFirmDestroyed { get; set; }
    public int OwnFirmDestroyed { get; set; }

    // inter-relationship with other nations
    public NationRelation[] NationRelations { get; } = new NationRelation[GameConstants.MAX_NATION];
    public int[] RelationStatuses { get; } = new int[GameConstants.MAX_NATION]; // replace status in struct NationRelation
    // for seeking to indicate whether passing other nation region
    public bool[] RelationPassable { get; } = new bool[GameConstants.MAX_NATION];
    public bool[] RelationShouldAttack { get; } = new bool[GameConstants.MAX_NATION];
    public bool IsAlliedWithPlayer { get; set; } // for fast access in visiting world functions

    public static int NationHandOverFlag { get; set; }

    protected FirmRes FirmRes => Sys.Instance.FirmRes;
    protected RaceRes RaceRes => Sys.Instance.RaceRes;
    protected SpriteRes SpriteRes => Sys.Instance.SpriteRes;
    protected UnitRes UnitRes => Sys.Instance.UnitRes;
    protected MonsterRes MonsterRes => Sys.Instance.MonsterRes;
    protected GodRes GodRes => Sys.Instance.GodRes;
    protected TechRes TechRes => Sys.Instance.TechRes;

    protected Config Config => Sys.Instance.Config;
    protected ConfigAdv ConfigAdv => Sys.Instance.ConfigAdv;
    protected Info Info => Sys.Instance.Info;
    protected TalkRes TalkRes => Sys.Instance.TalkRes;

    protected NationArray NationArray => Sys.Instance.NationArray;
    protected FirmArray FirmArray => Sys.Instance.FirmArray;
    protected TownArray TownArray => Sys.Instance.TownArray;
    protected UnitArray UnitArray => Sys.Instance.UnitArray;
    protected RebelArray RebelArray => Sys.Instance.RebelArray;
    protected SpyArray SpyArray => Sys.Instance.SpyArray;
    protected RegionArray RegionArray => Sys.Instance.RegionArray;
    protected SiteArray SiteArray => Sys.Instance.SiteArray;
    protected NewsArray NewsArray => Sys.Instance.NewsArray;

    public string NationName()
    {
        return KingName(true) + "'s Kingdom";
    }

    public string KingName(bool firstWordOnly = false)
    {
        if (NationNameId < 0) // human player custom names
        {
            return NationArray.GetHumanName(NationNameId, firstWordOnly);
        }
        else
        {
            if (firstWordOnly)
                return RaceRes[RaceId].get_single_name(NationNameId);
            else
                return RaceRes[RaceId].get_name(NationNameId);
        }
    }

    public int PeacefulDays()
    {
        return (Info.GameDate - LastWarDate).Days;
    }

    public string PeaceDurationString()
    {
        int peaceDays = PeacefulDays();
        int peaceYear = peaceDays / 365;
        int peaceMonth = (peaceDays - peaceYear * 365) / 30;

        string str = String.Empty;

        if (peaceYear > 0)
        {
            if (peaceYear != 1)
                str = $"{peaceYear} years and ";
            else
                str = "1 year and ";
        }

        if (peaceMonth != 1)
            str += peaceMonth + " months";
        else
            str += "1 month";

        return str;
    }

    public void SetAtWarToday(int attackerUnitRecno = 0)
    {
        IsAtWarToday = true;
        if (attackerUnitRecno != 0)
            LastAttackerUnitId = attackerUnitRecno;
    }

    public bool IsAtWar()
    {
        return IsAtWarToday || IsAtWarYesterday;
    }

    public int YearlyFoodConsumption()
    {
        return GameConstants.PERSON_FOOD_YEAR_CONSUMPTION * AllPopulation();
    }

    public int YearlyFoodProduction()
    {
        return GameConstants.PEASANT_FOOD_YEAR_PRODUCTION * TotalJoblessPopulation;
    }

    public int YearlyFoodChange()
    {
        return YearlyFoodProduction() - YearlyFoodConsumption();
    }

    public double Profit365Days()
    {
        return LastYearProfit * (365 - Info.YearDay) / 365 + CurYearProfit;
    }

    public double FixedIncome365Days()
    {
        return LastYearFixedIncome * (365 - Info.YearDay) / 365 + CurYearFixedIncome;
    }

    public double FixedExpense365Days()
    {
        return LastYearFixedExpense * (365 - Info.YearDay) / 365 + CurYearFixedExpense;
    }

    public double FixedProfit365Days()
    {
        return FixedIncome365Days() - FixedExpense365Days();
    }

    public double Income365Days()
    {
        return LastYearIncome * (365 - Info.YearDay) / 365 + CurYearIncome;
    }

    public double Income365Days(int incomeType)
    {
        return LastYearIncomes[incomeType] * (365 - Info.YearDay) / 365 + CurYearIncomes[incomeType];
    }

    public double TrueIncome365Days()
    {
        double curYearIncome = 0.0;
        double lastYearIncome = 0.0;

        for (int i = 0; i < INCOME_TYPE_COUNT - 1; i++) // -1 to exclude cheat
        {
            curYearIncome += CurYearIncomes[i];
            lastYearIncome += LastYearIncomes[i];
        }

        return lastYearIncome * (365 - Info.YearDay) / 365 + curYearIncome;
    }

    public double Expense365Days()
    {
        return LastYearExpense * (365 - Info.YearDay) / 365 + CurYearExpense;
    }

    public double Expense365Days(int expenseType)
    {
        return LastYearExpenses[expenseType] * (365 - Info.YearDay) / 365 + CurYearExpenses[expenseType];
    }

    public double Cheat365Days()
    {
        return LastYearCheat * (365 - Info.YearDay) / 365 + CurYearCheat;
    }

    public double TrueProfit365Days()
    {
        return Profit365Days() - Cheat365Days();
    }

    public double FoodChange365Days()
    {
        return LastYearFoodChange * (365 - Info.YearDay) / 365 + CurYearFoodChange;
    }

    public double ReputationChange365Days()
    {
        return LastYearReputationChange * (365 - Info.YearDay) / 365 + CurYearReputationChange;
    }

    protected virtual void SetRelationStatusAI(int nationRecno, int newStatus)
    {
    }

    public void SetRelationStatus(int nationRecno, int newStatus, bool recursiveCall = false)
    {
        if (nationRecno == NationId) // cannot set relation to itself
            return;

        NationRelation nationRelation = GetRelation(nationRecno);

        //-------------------------------------------------//
        //
        // When two nations agree to a cease-fire, there may
        // still be some bullets on their ways, and those
        // will set the status back to War status, so we need
        // the following code to handle this case.
        //
        //-------------------------------------------------//

        // 5 days after the cease-fire, the nation will remain cease-fire
        if (!recursiveCall && nationRelation.Status == NATION_TENSE && newStatus == NATION_HOSTILE &&
            Info.GameDate < nationRelation.LastChangeStatusDate.AddDays(5.0))
        {
            return;
        }

        //-------------------------------------------------//
        //
        // If the nation cease fire or form a friendly/alliance
        // treaty with a nation. And this nation current
        // has plan to attack that nation, then cancel the plan.
        //
        //-------------------------------------------------//

        if (IsAI())
        {
            SetRelationStatusAI(nationRecno, newStatus);
        }

        //------------------------------------------------//

        RelationStatuses[nationRecno - 1] = newStatus;
        nationRelation.Status = newStatus;
        nationRelation.LastChangeStatusDate = Info.GameDate;

        int newRelationLevel = newStatus * RELATION_LEVEL_PER_STATUS;

        // only set it when the new value is lower than the current value
        if (newRelationLevel < nationRelation.AIRelationLevel)
            nationRelation.AIRelationLevel = newRelationLevel;

        SetRelationPassable(nationRecno, NATION_FRIENDLY);

        //---------- set should_attack -------//

        if (newStatus == NATION_ALLIANCE || newStatus == NATION_FRIENDLY || newStatus == NATION_TENSE)
        {
            SetRelationShouldAttack(nationRecno, false, InternalConstants.COMMAND_AUTO);
        }
        else if (newStatus == NATION_HOSTILE)
        {
            SetRelationShouldAttack(nationRecno, true, InternalConstants.COMMAND_AUTO);
        }

        //----- share the nation contact with each other -----//
        /*
            // these segment code will cause multiplayer sync problem
    
            if( newStatus == NATION_ALLIANCE )
            {
                Nation* withNation = NationArray[nationRecno];
    
                for( i=NationArray.size() ; i>0 ; i-- )
                {
                    if( i==nation_recno || i==nationRecno )
                        continue;
    
                    if( NationArray.is_deleted(i) )
                        continue;
    
                    //-- if we have contact with this nation and our ally doesn't, share the contact with it --//
    
                    if( get_relation(i).has_contact && withNation.get_relation(i).has_contact==0 )
                    {
                        withNation.establish_contact(i);
                    }
                }
            }
        */

        //--- if this is a call from a client function, not a recursive call ---//

        if (!recursiveCall)
        {
            NationArray[nationRecno].SetRelationStatus(NationId, newStatus, true);

            //-- auto terminate their trade treaty if two nations go into a war --//

            if (newStatus == NATION_HOSTILE)
                SetTradeTreaty(nationRecno, false);
        }
    }

    public int GetRelationStatus(int nationRecno)
    {
        return RelationStatuses[nationRecno - 1];
    }

    public void SetRelationPassable(int nationRecno, int status)
    {
        RelationPassable[nationRecno - 1] = (RelationStatuses[nationRecno - 1] >= status);
    }

    public bool GetRelationPassable(int nationRecno)
    {
        return RelationPassable[nationRecno - 1];
    }

    public void SetRelationShouldAttack(int nationRecno, bool newValue, int remoteAction)
    {
        //TODO remote
        //if( !remoteAction && remote.is_enable() )
        //{
        //short *shortPtr = (short *) remote.new_send_queue_msg(MSG_NATION_SET_SHOULD_ATTACK, 3*sizeof(short));
        //*shortPtr = nation_recno;
        //shortPtr[1] = nationRecno;
        //shortPtr[2] = newValue;
        //}
        //else
        //{
        RelationShouldAttack[nationRecno - 1] = newValue;
        GetRelation(nationRecno).ShouldAttack = newValue;
        //}
    }

    public bool GetRelationShouldAttack(int nationRecno)
    {
        return nationRecno == 0 || RelationShouldAttack[nationRecno - 1];
    }

    public void SetTradeTreaty(int nationRecno, bool allowFlag)
    {
        GetRelation(nationRecno).TradeTreaty = allowFlag;
        NationArray[nationRecno].GetRelation(NationId).TradeTreaty = allowFlag;
    }

    public void EstablishContact(int nationRecno)
    {
        GetRelation(nationRecno).HasContact = true;
        NationArray[nationRecno].GetRelation(NationId).HasContact = true;
    }

    public void ChangeAIRelationLevel(int nationRecno, int levelChange)
    {
        NationRelation nationRelation = GetRelation(nationRecno);

        int newLevel = nationRelation.AIRelationLevel + levelChange;

        newLevel = Math.Min(newLevel, 100);
        newLevel = Math.Max(newLevel, 0);

        nationRelation.AIRelationLevel = newLevel;
    }

    public NationRelation GetRelation(int nationRecno)
    {
        return NationRelations[nationRecno - 1];
    }

    public double TotalYearTrade(int nationRecno)
    {
        return GetRelation(nationRecno).LastYearImport[IMPORT_TOTAL] +
               NationArray[nationRecno].GetRelation(NationId).LastYearImport[IMPORT_TOTAL];
    }

    public int TradeRating(int nationRecno)
    {
        // use an absolute value 5000 as the divider.

        int tradeRating1 = 100 * (int)TotalYearTrade(nationRecno) / 5000;

        int tradeRating2 =
            50 * (int)NationArray[nationRecno].GetRelation(NationId).LastYearImport[IMPORT_TOTAL] /
            (int)(LastYearIncome + 1) +
            50 * (int)GetRelation(nationRecno).LastYearImport[IMPORT_TOTAL] / (int)(LastYearExpense + 1);

        return Math.Max(tradeRating1, tradeRating2);
    }

    public int AllPopulation()
    {
        return TotalPopulation + TotalHumanCount;
    }

    public int TotalTechLevel(int unitClass = 0)
    {
        int totalTechLevel = 0;

        for (int i = 1; i <= TechRes.TechInfos.Length; i++)
        {
            TechInfo techInfo = TechRes[i];
            int techLevel = techInfo.get_nation_tech_level(NationId);

            if (techLevel > 0)
            {
                if (unitClass == 0 || UnitRes[techInfo.unit_id].unit_class == unitClass)
                {
                    totalTechLevel += techLevel;
                }
            }
        }

        return totalTechLevel;
    }

    public int BaseTownCountInRegion(int regionId)
    {
        // regionStatId may be zero
        int regionStatId = RegionArray.GetRegionInfo(regionId).RegionStatId;
        if (regionStatId != 0)
        {
            return RegionArray.GetRegionStat(regionId).BaseTownNationCounts[NationId - 1];
        }
        else if (RegionArray.GetRegionInfo(regionId).RegionSize < InternalConstants.TOWN_WIDTH * InternalConstants.TOWN_HEIGHT)
        {
            return 0; // not enough to build any town
        }
        else
        {
            int townCount = 0;
            foreach (Town town in TownArray)
            {
                if (town.RegionId == regionId && town.NationId == NationId)
                    townCount++;
            }

            return townCount;
        }
    }


    public void UpdateNationRating()
    {
        PopulationRating = GetPopulationRating();

        EconomicRating = GetEconomicRating();

        OverallRating = GetOverallRating();
    }

    public int GetPopulationRating()
    {
        return AllPopulation();
    }

    public int GetEconomicRating()
    {
        return (int)(Cash / 300 + TrueIncome365Days() / 2 + TrueProfit365Days());
    }

    public int GetOverallRating()
    {
        return 33 * PopulationRating / 500 + 33 * MilitaryRating / 200 + 33 * EconomicRating / 10000;
    }

    public int PopulationRankRating()
    {
        if (NationArray.MaxPopulationRating == 0)
            return 0;

        return 100 * PopulationRating / NationArray.MaxPopulationRating;
    }

    public int MilitaryRankRating()
    {
        if (NationArray.MaxMilitaryRating == 0)
            return 0;

        return 100 * MilitaryRating / NationArray.MaxMilitaryRating;
    }

    public int EconomicRankRating()
    {
        if (NationArray.MaxEconomicRating == 0)
            return 0;

        return 100 * EconomicRating / NationArray.MaxEconomicRating;
    }

    public int ReputationRankRating()
    {
        if (NationArray.MaxReputation == 0)
            return 0;

        return 100 * (int)Reputation / NationArray.MaxReputation;
    }

    public int KillMonsterRankRating()
    {
        if (NationArray.MaxKillMonsterScore == 0)
            return 0;

        if (Config.monster_type == Config.OPTION_MONSTER_NONE)
            return 0;

        return 100 * (int)KillMonsterScore / NationArray.MaxKillMonsterScore;
    }

    public int OverallRankRating()
    {
        if (NationArray.MaxOverallRating == 0)
            return 0;

        return 100 * OverallRating / NationArray.MaxOverallRating;
    }

    public bool GoalDestroyNationAchieved()
    {
        return NationArray.NationCount == 1;
    }

    public bool GoalDestroyMonsterAchieved()
    {
        if (!Config.goal_destroy_monster) // this is not one of the required goals.
            return false;

        if (Config.monster_type == Config.OPTION_MONSTER_NONE)
            return false;

        //------- when all monsters have been killed -------//

        if (FirmRes[Firm.FIRM_MONSTER].total_firm_count == 0 && UnitRes.mobile_monster_count == 0)
        {
            double maxScore = 0.0;

            foreach (Nation nation in NationArray)
            {
                if (nation.KillMonsterScore > maxScore)
                    maxScore = nation.KillMonsterScore;
            }

            //-- if this nation is the one that has destroyed most monsters, it wins, otherwise it loses --//

            return (int)maxScore == (int)KillMonsterScore;
        }

        return false;
    }

    public bool GoalPopulationAchieved()
    {
        if (!Config.goal_population_flag) // this is not one of the required goals.
            return false;

        return AllPopulation() >= Config.goal_population;
    }

    public bool GoalEconomicScoreAchieved()
    {
        if (!Config.goal_economic_score_flag)
            return false;

        Info.SetRankData(false); // 0-set all nations, not just those that have contact with us

        return Info.GetRankScore(3, NationId) >= Config.goal_economic_score;
    }

    public bool GoalTotalScoreAchieved()
    {
        if (!Config.goal_total_score_flag)
            return false;

        Info.SetRankData(false); // 0-set all nations, not just those that have contact with us

        return Info.GetTotalScore(NationId) >= Config.goal_total_score;
    }

    public bool IsOwn() => NationType == NATION_OWN;
    public bool IsAI() => NationType == NATION_AI;
    public bool IsRemote() => NationType == NATION_REMOTE;

    public NationBase()
    {
        for (int i = 0; i < NationRelations.Length; i++)
            NationRelations[i] = new NationRelation();
    }

    void IIdObject.SetId(int id)
    {
        NationId = id;
    }

    public virtual void Init(int nationType, int raceId, int colorSchemeId, int playerId = 0)
    {
        //------------- set vars ---------------//

        NationType = nationType;
        RaceId = raceId;
        ColorSchemeId = colorSchemeId;
        PlayerId = playerId;

        //colorSchemeId = Math.Min(colorSchemeId, InternalConstants.MAX_COLOR_SCHEME);
        //nation_color = Sys.Instance.color_remap_array[colorSchemeId].main_color;
        NationColor = ColorRemap.GetColorRemap(ColorRemap.ColorSchemes[NationId], false).MainColor;

        LastWarDate = Info.GameDate;

        //------- if this is the local player's nation -------//

        if (NationType == NATION_OWN)
            NationArray.PlayerId = NationId;

        //---------- init game vars ------------//

        int[] start_up_cash_array = { 4000, 7000, 12000, 20000 };

        if (IsAI())
            Cash = start_up_cash_array[Config.ai_start_up_cash - 1];
        else
            Cash = start_up_cash_array[Config.start_up_cash - 1];

        Food = 5000.0; // startup food is 5000 for all nations in all settings

        //---- initialize this nation's relation on other nations ----//

        // #### Richard 6-12-2013: Moved this from init_relation to here, so it works with new independent nations spawning #### //
        IsAlliedWithPlayer = false;

        foreach (Nation nation in NationArray)
        {
            InitRelation(nation);
            nation.InitRelation(this);
        }

        //--------- init technology ----------//

        TechRes.init_nation_tech(NationId);

        //------- reset all god knowledge --------//

        GodRes.init_nation_know(NationId);

        //if(remote.is_enable() && nation_recno && !is_ai() && misc.is_file_exist("TECHGOD.SYS"))
        //{
        //tech_res.inc_all_tech_level(nation_recno);
        //tech_res.inc_all_tech_level(nation_recno);
        //tech_res.inc_all_tech_level(nation_recno);
        //god_res.enable_know_all(nation_recno);
        //}
        //else
        //{
        if (ConfigAdv.nation_start_god_level == 1)
        {
            GodRes[RaceId].enable_know(NationId);
        }
        else if (ConfigAdv.nation_start_god_level > 1)
        {
            GodRes.enable_know_all(NationId);
        }

        for (int i = 0; i < ConfigAdv.nation_start_tech_inc_all_level; i++)
            TechRes.inc_all_tech_level(NationId);
        //}
    }

    public virtual void Deinit()
    {
        if (NationId == 0) // has been deinitialized
            return;

        //---- delete all talk messages to/from this nation ----//

        TalkRes.del_all_nation_msg(NationId);

        //------- close down all firms --------//

        CloseAllFirms();

        //---- neutralize all towns belong to this nation ----//

        foreach (Town town in TownArray)
        {
            if (town.NationId == NationId)
                town.ChangeNation(0);
        }

        //------------- deinit our spies -------------//

        foreach (Spy spy in SpyArray)
        {
            //-----------------------------------------------------//
            // Convert all of spies of this nation to normal units, 
            // so there  will be no more spies of this nation. 
            //-----------------------------------------------------//

            if (spy.TrueNationId == NationId) // retire counter-spies immediately
                spy.DropSpyIdentity();

            //-----------------------------------------------------//
            // For spies of other nation cloaked as this nation,
            // their will uncover their cloak and change back
            // to their original nation. 
            //-----------------------------------------------------//

            else if (spy.CloakedNationId == NationId)
            {
                // changing cloak is normally only allowed when mobile

                if (spy.SpyPlace == Spy.SPY_FIRM)
                {
                    // at least try to return spoils before it goes poof
                    if (!spy.CanCaptureFirm() || !spy.CaptureFirm())
                        spy.MobilizeFirmSpy();
                }
                else if (spy.SpyPlace == Spy.SPY_TOWN)
                    spy.MobilizeTownSpy();

                if (spy.SpyPlace == Spy.SPY_MOBILE) // what about on transport??
                    spy.ChangeCloakedNation(spy.TrueNationId);
            }
        }

        //----- deinit all units belonging to this nation -----//

        DeinitAllUnits();

        //-------- if the viewing nation is this nation -------//

        if (Info.DefaultViewingNationId == NationId)
        {
            Info.DefaultViewingNationId = NationArray.PlayerId;
            Sys.Instance.set_view_mode(InternalConstants.MODE_NORMAL);
        }

        else if (Info.ViewingNationId == NationId)
            Sys.Instance.set_view_mode(InternalConstants.MODE_NORMAL); // it will set viewing_nation_recno to default_viewing_nation_recno

        // if deleting own nation, darken view mode buttons
        if (NationId == NationArray.PlayerId)
        {
            Sys.Instance.disp_view_mode(1);
        }

        NationId = 0;
    }

    public void InitRelation(NationBase otherNation)
    {
        int otherNationRecno = otherNation.NationId;
        NationRelation nationRelation = NationRelations[otherNationRecno - 1];

        SetRelationShouldAttack(otherNationRecno, otherNationRecno != NationId, InternalConstants.COMMAND_AUTO);

        // AI has contact with each other in the beginning of the game.
        if (IsAI() && NationArray[otherNationRecno].IsAI())
            nationRelation.HasContact = true;
        else
            nationRelation.HasContact = otherNationRecno == NationId || Config.explore_whole_map;

        nationRelation.TradeTreaty = otherNationRecno == NationId;

        nationRelation.Status = NATION_NEUTRAL;
        nationRelation.AIRelationLevel = NATION_NEUTRAL * RELATION_LEVEL_PER_STATUS;
        nationRelation.LastChangeStatusDate = Info.GameDate;

        if (otherNationRecno == NationId) // own nation
            RelationStatuses[otherNationRecno - 1] = NATION_ALLIANCE; // for facilitating searching
        else
            RelationStatuses[otherNationRecno - 1] = NATION_NEUTRAL;

        SetRelationPassable(otherNationRecno, NATION_FRIENDLY);
    }

    public virtual void ProcessAI()
    {
    }

    public void CloseAllFirms()
    {
        List<Firm> firmsToDelete = new List<Firm>();
        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId == NationId)
            {
                firmsToDelete.Add(firm);
            }
        }

        foreach (Firm firm in firmsToDelete)
        {
            FirmArray.DeleteFirm(firm);
        }
    }

    public void DeinitAllUnits()
    {
        //--- update total_human_unit so the numbers will be correct ---//

        // only do this in release version to on-fly fix bug
        NationArray.UpdateStatistic();

        //--------------------------------------//

        List<Unit> unitsToDelete = new List<Unit>();
        foreach (Unit unit in UnitArray)
        {
            if (unit.NationId != NationId)
                continue;

            //----- only human units will betray -----//

            if (unit.RaceId != 0)
            {
                unit.Loyalty = 0; // force it to betray

                if (unit.ThinkBetray())
                    continue;
            }

            //--- if the unit has not changed nation, the unit will disappear ---//

            if (!UnitArray.IsDeleted(unit.SpriteId))
                unitsToDelete.Add(unit);
        }

        foreach (Unit unit in unitsToDelete)
        {
            UnitArray.DeleteUnit(unit);
        }
    }

    public void SucceedKing(Unit newKing)
    {
        int newKingLeadership = 0;

        if (newKing.Skill.SkillId == Skill.SKILL_LEADING)
            newKingLeadership = newKing.Skill.SkillLevel;

        newKingLeadership = Math.Max(20, newKingLeadership); // give the king a minimum level of leadership

        //----- set the common loyalty change for all races ------//

        int loyaltyChange = 0;

        if (newKingLeadership < KingLeadership)
            loyaltyChange = (newKingLeadership - KingLeadership) / 2;

        if (newKing.Rank != Unit.RANK_GENERAL)
            loyaltyChange -= 20;

        //---- update loyalty of units in this nation ----//

        foreach (Unit unit in UnitArray)
        {
            if (unit.SpriteId == KingUnitId || unit.SpriteId == newKing.SpriteId)
                continue;

            if (unit.NationId != NationId)
                continue;

            //--------- update loyalty change ----------//

            unit.ChangeLoyalty(loyaltyChange +
                                SucceedKingLoyaltyChange(unit.RaceId, newKing.RaceId, RaceId));
        }

        //---- update loyalty of units in camps ----//

        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId != NationId)
                continue;

            //------ process military camps and seat of power -------//

            if (firm.FirmType == Firm.FIRM_CAMP || firm.FirmType == Firm.FIRM_BASE)
            {
                foreach (Worker worker in firm.Workers)
                {
                    //--------- update loyalty change ----------//

                    worker.ChangeLoyalty(loyaltyChange +
                                          SucceedKingLoyaltyChange(worker.RaceId, newKing.RaceId, RaceId));
                }
            }
        }

        //---- update loyalty of town people ----//

        foreach (Town town in TownArray)
        {
            if (town.NationId != NationId)
                continue;

            for (int raceId = 1; raceId <= GameConstants.MAX_RACE; raceId++)
            {
                if (town.RacesPopulation[raceId - 1] == 0)
                    continue;

                //------ update loyalty now ------//

                town.ChangeLoyalty(raceId, loyaltyChange +
                                            SucceedKingLoyaltyChange(raceId, newKing.RaceId, RaceId));
            }
        }

        //------- add news --------//

        NewsArray.new_king(NationId, newKing.SpriteId);

        //-------- set the new king now ------//

        SetKing(newKing.SpriteId, 0); // 0-not the first king, it is a succession

        //------ if the new king is a spy -------//

        if (newKing.SpyId != 0)
        {
            Spy spy = SpyArray[newKing.SpyId];

            if (newKing.TrueNationId() == NationId) // if this is your spy
                spy.DropSpyIdentity();
            else
                spy.ThinkBecomeKing();
        }
    }

    public void SetKing(int kingUnitRecno, int firstKing)
    {
        KingUnitId = kingUnitRecno;

        Unit kingUnit = UnitArray[KingUnitId];

        //--- if this unit currently has not have leadership ---//

        if (kingUnit.Skill.SkillId != Skill.SKILL_LEADING)
        {
            kingUnit.Skill.SkillId = Skill.SKILL_LEADING;
            kingUnit.Skill.SkillLevel = 0;
        }

        kingUnit.SetRank(Unit.RANK_KING);
        // clear the existing order, as there might be an assigning to firm/town order. But kings cannot be assigned to towns or firms as workers.
        kingUnit.Stop2();

        //---------- king related vars ----------//

        // for human players, the name is retrieved from NationArray::human_name_array
        if (NationType == NATION_AI || firstKing == 0) // for succession, no longer use the original player name
            NationNameId = kingUnit.NameId;
        else
            NationNameId = -NationId;

        RaceId = kingUnit.RaceId;
        KingLeadership = kingUnit.Skill.SkillLevel;
    }

    public void HandOverTo(int handoverNationRecno)
    {
        RebelArray.StopAttackNation(NationId);
        TownArray.StopAttackNation(NationId);
        UnitArray.StopAllWar(NationId);
        MonsterRes.stop_attack_nation(NationId);

        NationHandOverFlag = NationId;

        //--- hand over units (should hand over units first as you cannot change a firm's nation without changing the nation of the overseer there ---//

        foreach (Unit unit in UnitArray)
        {
            //TODO check:
            //-- If the unit is dying and isn't truly deleted yet, delete it now --//

            //---------------------------------------//

            if (unit.NationId != NationId)
                continue;

            if (unit is UnitGod)
            {
                unit.Resign(InternalConstants.COMMAND_AUTO);
                continue;
            }

            //----- if it is a spy cloaked as this nation -------//
            //
            // If the unit is a overseer of a Camp or Base, 
            // the Camp or Base will change nation as a result. 
            //
            //---------------------------------------------------//

            if (unit.SpyId != 0)
                unit.SpyChangeNation(handoverNationRecno, InternalConstants.COMMAND_AUTO);
            else
                unit.ChangeNation(handoverNationRecno);
        }

        //------- hand over firms ---------//

        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId == NationId)
            {
                firm.ChangeNation(handoverNationRecno);
            }
        }

        //------- hand over towns ---------//

        foreach (Town town in TownArray)
        {
            if (town.NationId == NationId)
            {
                town.ChangeNation(handoverNationRecno);
            }
        }

        //-------------------------------------------------//
        //
        // For the spies of this nation cloaked into other nations,
        // we need to update their true_nation_recno. 
        //
        //-------------------------------------------------//

        foreach (Spy spy in SpyArray)
        {
            if (spy.TrueNationId == NationId)
                spy.ChangeTrueNation(handoverNationRecno);
        }

        //------- delete this nation from NationArray -------//

        //TODO check
        NationArray.DeleteNation((Nation)this);
        NationHandOverFlag = 0;
    }

    public void NextDay()
    {
        //------ post is at war flag -------/

        if (IsAtWarToday)
            LastWarDate = Info.GameDate;

        IsAtWarYesterday = IsAtWarToday;
        IsAtWarToday = false;

        //--- if the king is dead, and now looking for a successor ---//

        if (KingUnitId == 0)
        {
            if (Info.TotalDays % 3 == NationId % 3) // decrease 1 loyalty point every 3 days
                ChangeAllPeopleLoyalty(-1);
        }

        //------ check if this nation has won the game -----//

        CheckWin();

        //-- if the player still hasn't selected a unit to succeed the died king, declare defeated if the all units are killed --//

        if (KingUnitId == 0)
            CheckLose();
    }

    public void NextMonth()
    {
        //--------------------------------------------------//
        // When the two nations, whose relationship is Tense,
        // do not have new conflicts for 3 years, their
        // relationship automatically becomes Neutral.
        //--------------------------------------------------//

        foreach (Nation nation in NationArray)
        {
            NationRelation nationRelation = GetRelation(nation.NationId);

            if (nationRelation.Status == NATION_TENSE &&
                Info.GameDate >= nationRelation.LastChangeStatusDate.AddDays(365.0 * 3.0))
            {
                SetRelationStatus(nation.NationId, NATION_NEUTRAL);
            }

            //--- update good_relation_duration_rating ---//

            else if (nationRelation.Status == NATION_FRIENDLY)
                nationRelation.GoodRelationDurationRating += 0.2; // this is the monthly increase

            else if (nationRelation.Status == NATION_ALLIANCE)
                nationRelation.GoodRelationDurationRating += 0.4;
        }

        //----- increase reputation gradually -----//

        if (Reputation < 100)
            ChangeReputation(0.5);
    }

    public void NextYear()
    {
        //------ post financial data --------//

        LastYearIncome = CurYearIncome;
        CurYearIncome = 0.0;

        LastYearExpense = CurYearExpense;
        CurYearExpense = 0.0;

        LastYearFixedIncome = CurYearFixedIncome;
        CurYearFixedIncome = 0.0;

        LastYearFixedExpense = CurYearFixedExpense;
        CurYearFixedExpense = 0.0;

        LastYearProfit = CurYearProfit;
        CurYearProfit = 0.0;

        LastYearCheat = CurYearCheat;
        CurYearCheat = 0.0;

        //------ post income & expense breakdown ------//

        for (int i = 0; i < INCOME_TYPE_COUNT; i++)
        {
            LastYearIncomes[i] = CurYearIncomes[i];
            CurYearIncomes[i] = 0.0;
        }

        for (int i = 0; i < EXPENSE_TYPE_COUNT; i++)
        {
            LastYearExpenses[i] = CurYearExpenses[i];
            CurYearExpenses[i] = 0.0;
        }

        //------ post good change data ------//

        LastYearFoodIn = CurYearFoodIn;
        CurYearFoodIn = 0.0;

        LastYearFoodOut = CurYearFoodOut;
        CurYearFoodOut = 0.0;

        LastYearFoodChange = CurYearFoodChange;
        CurYearFoodChange = 0.0;

        //---------- post imports ----------//

        for (int i = 0; i < GameConstants.MAX_NATION; i++)
        {
            NationRelation nationRelation = NationRelations[i];
            for (int j = 0; j < IMPORT_TYPE_COUNT; j++)
            {
                nationRelation.LastYearImport[j] = nationRelation.CurYearImport[j];
                nationRelation.CurYearImport[j] = 0.0;
            }
        }

        //--------- post reputation ----------//

        LastYearReputationChange = CurYearReputationChange;
        CurYearReputationChange = 0.0;
    }

    public void AddIncome(int incomeType, double incomeAmt, bool fixedIncome = false)
    {
        Cash += incomeAmt;

        CurYearIncomes[incomeType] += incomeAmt;

        CurYearIncome += incomeAmt;
        CurYearProfit += incomeAmt;

        if (fixedIncome)
            CurYearFixedIncome += incomeAmt;
    }

    public void AddExpense(int expenseType, double expenseAmt, bool fixedExpense = false)
    {
        Cash -= expenseAmt;

        CurYearExpenses[expenseType] += expenseAmt;

        CurYearExpense += expenseAmt;
        CurYearProfit -= expenseAmt;

        if (fixedExpense)
            CurYearFixedExpense += expenseAmt;
    }

    public void ChangeReputation(double changeLevel)
    {
        //--- reputation increase more slowly when it is close to 100 ----//

        if (changeLevel > 0.0 && Reputation > 0.0)
            changeLevel = changeLevel * (150.0 - Reputation) / 150.0;

        //-----------------------------------------------//

        Reputation += changeLevel;

        if (Reputation > 100.0)
            Reputation = 100.0;

        if (Reputation < -100.0)
            Reputation = -100.0;

        //------ update cur_year_reputation_change ------//

        CurYearReputationChange += changeLevel;
    }

    public void AddFood(double foodToAdd)
    {
        Food += foodToAdd;

        CurYearFoodIn += foodToAdd;
        CurYearFoodChange += foodToAdd;
    }

    public void ConsumeFood(double foodConsumed)
    {
        Food -= foodConsumed;

        CurYearFoodOut += foodConsumed;
        CurYearFoodChange -= foodConsumed;
    }

    public void ImportGoods(int importType, int importNationRecno, double importAmt)
    {
        if (importNationRecno == NationId)
            return;

        NationRelation nationRelation = GetRelation(importNationRecno);

        nationRelation.CurYearImport[importType] += importAmt;
        nationRelation.CurYearImport[IMPORT_TOTAL] += importAmt;

        AddExpense(EXPENSE_IMPORTS, importAmt, true);
        NationArray[importNationRecno].AddIncome(INCOME_EXPORTS, importAmt, true);
    }

    public void GiveTribute(int toNationRecno, int tributeAmt)
    {
        Nation toNation = NationArray[toNationRecno];

        AddExpense(EXPENSE_TRIBUTE, tributeAmt);

        toNation.AddIncome(INCOME_TRIBUTE, tributeAmt);

        NationRelation nationRelation = GetRelation(toNationRecno);

        nationRelation.LastGiveGiftDate = Info.GameDate;
        nationRelation.TotalGivenGiftAmount += tributeAmt;

        //---- set the last rejected date so it won't request or give again soon ----//

        nationRelation.LastTalkRejectDates[TalkMsg.TALK_GIVE_AID - 1] = default(DateTime);
        nationRelation.LastTalkRejectDates[TalkMsg.TALK_DEMAND_AID - 1] = default(DateTime);
        nationRelation.LastTalkRejectDates[TalkMsg.TALK_GIVE_TRIBUTE - 1] = default(DateTime);
        nationRelation.LastTalkRejectDates[TalkMsg.TALK_DEMAND_TRIBUTE - 1] = default(DateTime);

        NationRelation nationRelation2 = toNation.GetRelation(NationId);

        nationRelation2.LastTalkRejectDates[TalkMsg.TALK_GIVE_AID - 1] = default(DateTime);
        nationRelation2.LastTalkRejectDates[TalkMsg.TALK_DEMAND_AID - 1] = default(DateTime);
        nationRelation2.LastTalkRejectDates[TalkMsg.TALK_GIVE_TRIBUTE - 1] = default(DateTime);
        nationRelation2.LastTalkRejectDates[TalkMsg.TALK_DEMAND_TRIBUTE - 1] = default(DateTime);
    }

    public void GiveTech(int toNationRecno, int techId, int techVersion)
    {
        Nation toNation = NationArray[toNationRecno];

        int curVersion = TechRes[techId].get_nation_tech_level(toNationRecno);

        if (curVersion < techVersion)
            TechRes[techId].set_nation_tech_level(toNationRecno, techVersion);

        NationRelation nationRelation = GetRelation(toNationRecno);

        nationRelation.LastGiveGiftDate = Info.GameDate;
        nationRelation.TotalGivenGiftAmount += (techVersion - curVersion) * 500; // one version level is worth $500

        //---- set the last rejected date so it won't request or give again soon ----//

        nationRelation.LastTalkRejectDates[TalkMsg.TALK_GIVE_TECH - 1] = default(DateTime);
        nationRelation.LastTalkRejectDates[TalkMsg.TALK_DEMAND_TECH - 1] = default(DateTime);

        NationRelation nationRelation2 = toNation.GetRelation(NationId);

        nationRelation2.LastTalkRejectDates[TalkMsg.TALK_GIVE_TECH - 1] = default(DateTime);
        nationRelation2.LastTalkRejectDates[TalkMsg.TALK_DEMAND_TECH - 1] = default(DateTime);
    }

    public void SetAutoCollectTaxLoyalty(int loyaltyLevel)
    {
        AutoCollectTaxLoyalty = loyaltyLevel;

        if (loyaltyLevel != 0 && AutoGrantLoyalty >= AutoCollectTaxLoyalty)
        {
            AutoGrantLoyalty = AutoCollectTaxLoyalty - 10;
        }
    }

    public void SetAutoGrantLoyalty(int loyaltyLevel)
    {
        AutoGrantLoyalty = loyaltyLevel;

        if (loyaltyLevel != 0 && AutoGrantLoyalty >= AutoCollectTaxLoyalty)
        {
            AutoCollectTaxLoyalty = AutoGrantLoyalty + 10;

            if (AutoCollectTaxLoyalty > 100)
                AutoCollectTaxLoyalty = 0; // disable auto collect tax if it's over 100
        }
    }

    // whether the nation has any people (but not counting the king). If no, then the nation is going to end.
    public bool HasPeople()
    {
        return AllPopulation() > 0;
    }

    public void BeingAttacked(int attackNationRecno)
    {
        if (NationArray.IsDeleted(attackNationRecno) || attackNationRecno == NationId)
            return;

        //--- if it is an accidential attack (e.g. bullets attack with spreading damages) ---//

        Nation attackNation = NationArray[attackNationRecno];

        if (!attackNation.GetRelation(NationId).ShouldAttack)
            return;

        //--- check if there a treaty between these two nations ---//

        NationRelation nationRelation = GetRelation(attackNationRecno);

        if (nationRelation.Status != NATION_HOSTILE)
        {
            //--- if this nation (the one being attacked) has a higher than 0 reputation, the attacker's reputation will decrease ---//

            if (Reputation > 0)
                attackNation.ChangeReputation(-Reputation * 40.0 / 100.0);

            // how many times this nation has started a war with us, the more the times the worse this nation is.
            nationRelation.StartedWarOnUsCount++;

            if (nationRelation.Status == NATION_ALLIANCE || nationRelation.Status == NATION_FRIENDLY)
            {
                // the attacking nation abruptly terminates the treaty with us, not we terminate the treaty with them,
                // so attackNation.end_treaty() should be called instead of end_treaty()
                attackNation.EndTreaty(NationId, NATION_HOSTILE);
            }
            else
            {
                SetRelationStatus(attackNationRecno, NATION_HOSTILE);
            }
        }

        //---- reset the inter-national peace days counter ----//

        NationArray.NationPeaceDays = 0;
    }

    public void CivilianKilled(int civilianRaceId, bool isAttacker, int penaltyType)
    {
        if (isAttacker)
        {
            if (penaltyType == 0) // mobile civilian
            {
                ChangeReputation(-0.3);
            }
            else if (penaltyType == 1) // town defender
            {
                ChangeAllPeopleLoyalty(-1.0, civilianRaceId);
                ChangeReputation(-1.0);
            }
            else if (penaltyType == 2) // town resident
            {
                ChangeAllPeopleLoyalty(-2.0, civilianRaceId);
                ChangeReputation(-1.0);
            }
            else if (penaltyType == 3) // trader
            {
                ChangeAllPeopleLoyalty(-2.0, civilianRaceId);
                ChangeReputation(-10.0);
            }
        }
        else // is casualty
        {
            if (penaltyType == 0) // mobile civilian
            {
                ChangeReputation(-0.3);
            }
            else if (penaltyType == 1) // town defender
            {
                ChangeReputation(-0.3);
            }
            else if (penaltyType == 2) // town resident
            {
                ChangeAllPeopleLoyalty(-1.0, civilianRaceId);
                ChangeReputation(-0.3);
            }
            else if (penaltyType == 3) // trader
            {
                ChangeAllPeopleLoyalty(-0.6, civilianRaceId);
                ChangeReputation(-2.0);
            }
        }
    }

    public void ChangeAllPeopleLoyalty(double loyaltyChange, int raceId = 0)
    {
        //---- update loyalty of units in this nation ----//

        foreach (Unit unit in UnitArray)
        {
            if (unit.SpriteId == KingUnitId)
                continue;

            if (unit.NationId != NationId)
                continue;

            //--------- update loyalty change ----------//

            if (raceId == 0 || unit.RaceId == raceId)
                unit.ChangeLoyalty((int)loyaltyChange);
        }

        //---- update loyalty of units in camps ----//

        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId != NationId)
                continue;

            //------ process military camps and seat of power -------//

            if (firm.FirmType == Firm.FIRM_CAMP || firm.FirmType == Firm.FIRM_BASE)
            {
                foreach (Worker worker in firm.Workers)
                {
                    if (raceId == 0 || worker.RaceId == raceId)
                        worker.ChangeLoyalty((int)loyaltyChange);
                }
            }
        }

        //---- update loyalty of town people ----//

        foreach (Town town in TownArray)
        {
            if (town.NationId != NationId)
                continue;

            //--------------------------------------//

            if (raceId != 0) // decrease loyalty of a specific race
            {
                if (town.RacesPopulation[raceId - 1] > 0)
                    town.ChangeLoyalty(raceId, loyaltyChange);
            }
            else // decrease loyalty of all races
            {
                for (int j = 0; j < GameConstants.MAX_RACE; j++)
                {
                    if (town.RacesPopulation[j] == 0)
                        continue;

                    town.ChangeLoyalty(j + 1, loyaltyChange);
                }
            }
        }
    }

    public void FormFriendlyTreaty(int nationRecno)
    {
        SetRelationStatus(nationRecno, NATION_FRIENDLY);
    }

    public void FormAllianceTreaty(int nationRecno)
    {
        SetRelationStatus(nationRecno, NATION_ALLIANCE);

        //--- allied nations are oblied to trade with each other ---//

        SetTradeTreaty(nationRecno, true);

        //------ set is_allied_with_player -------//

        if (nationRecno == NationArray.PlayerId)
            IsAlliedWithPlayer = true;

        if (NationId == NationArray.PlayerId)
            NationArray[nationRecno].IsAlliedWithPlayer = true;
    }

    public void EndTreaty(int withNationRecno, int newStatus)
    {
        //----- decrease reputation when terminating a treaty -----//

        Nation withNation = NationArray[withNationRecno];

        if (withNation.Reputation > 0)
        {
            int curStatus = GetRelationStatus(withNationRecno);

            if (curStatus == TalkMsg.TALK_END_FRIENDLY_TREATY)
                ChangeReputation(-withNation.Reputation * 10.0 / 100.0);
            else
                ChangeReputation(-withNation.Reputation * 20.0 / 100.0);
        }

        //------- reset good_relation_duration_rating -----//

        if (newStatus <= NATION_NEUTRAL)
        {
            GetRelation(withNationRecno).GoodRelationDurationRating = 0.0;
            withNation.GetRelation(NationId).GoodRelationDurationRating = 0.0;
        }

        //------- set new relation status --------//

        SetRelationStatus(withNationRecno, newStatus);

        //------ set is_allied_with_player -------//

        if (withNationRecno == NationArray.PlayerId)
            IsAlliedWithPlayer = false;

        if (NationId == NationArray.PlayerId)
            NationArray[withNationRecno].IsAlliedWithPlayer = false;
    }

    public void Surrender(int toNationRecno)
    {
        NewsArray.nation_surrender(NationId, toNationRecno);

        //---- the king demote himself to General first ----//

        if (KingUnitId != 0)
        {
            UnitArray[KingUnitId].SetRank(Unit.RANK_GENERAL);
            KingUnitId = 0;
        }

        //------- if the player surrenders --------//

        if (NationId == NationArray.PlayerId)
            Sys.Instance.EndGame(0, 1, toNationRecno);

        //--- hand over the entire nation to another nation ---//

        HandOverTo(toNationRecno);
    }

    public void Defeated()
    {
        //---- if the defeated nation is the player's nation ----//

        if (NationId == NationArray.PlayerId)
        {
            Sys.Instance.EndGame(0, 1); // the player lost the game 
        }
        else // AI and remote players 
        {
            NewsArray.nation_destroyed(NationId);
        }

        //---- delete this nation from NationArray ----//

        //TODO check
        NationArray.DeleteNation((Nation)this);
    }

    public void CheckWin()
    {
        bool hasWon = GoalDestroyNationAchieved() ||
                      GoalDestroyMonsterAchieved() ||
                      GoalPopulationAchieved() ||
                      GoalEconomicScoreAchieved() ||
                      GoalTotalScoreAchieved();

        if (!hasWon)
            return;

        // if the player achieves the goal, the player wins, if one of the other kingdoms achieves the goal, it wins.
        Sys.Instance.EndGame(NationId, 0);
    }

    public void CheckLose()
    {
        //---- if the king of this nation is dead and it has no people left ----//

        if (KingUnitId == 0 && !HasPeople())
            Defeated();
    }

    public bool RevealedByPhoenix(int xLoc, int yLoc)
    {
        int effectiveRange = UnitRes[UnitConstants.UNIT_PHOENIX].visual_range;

        foreach (Unit unit in UnitArray)
        {
            if (unit.UnitType == UnitConstants.UNIT_PHOENIX && unit.NationId == NationId)
            {
                if (Misc.points_distance(xLoc, yLoc, unit.NextLocX, unit.NextLocY) <= effectiveRange)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private int SucceedKingLoyaltyChange(int thisRaceId, int newKingRaceId, int oldKingRaceId)
    {
        //----- the races of the new and old kings are different ----//

        if (newKingRaceId != oldKingRaceId)
        {
            //--- if this unit's race is the same as the new king ---//

            if (thisRaceId == newKingRaceId)
                return GameConstants.NEW_KING_SAME_RACE_LOYALTY_INC;

            //--- if this unit's race is the same as the old king ---//

            if (thisRaceId == oldKingRaceId)
                return GameConstants.NEW_KING_DIFFERENT_RACE_LOYALTY_DEC;
        }

        return 0;
    }

    public string FoodString()
    {
        int foodChange = (int)FoodChange365Days();
        return (int)Food + " (" + (foodChange >= 0 ? "+" : "-") + Math.Abs(foodChange) + ")";
    }

    public string CashString()
    {
        int cashChange = (int)Profit365Days();
        return (int)Cash + " (" + (cashChange >= 0 ? "+" : "-") + Math.Abs(cashChange) + ")";
    }

    public string ReputationString()
    {
        int reputationChange = (int)ReputationChange365Days();
        return (int)Reputation + " (" + (reputationChange >= 0 ? "+" : "-") + Math.Abs(reputationChange) + ")";
    }
}