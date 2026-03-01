using System;
using System.IO;

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
    public byte NationColor { get; private set; }
    public int ColorSchemeId { get; private set; }
    public int KingUnitId { get; set; }
    private int KingLeadership { get; set; }
    public int NationNameId { get; private set; }

    public double Cash { get; set; }
    public double Food { get; set; }
    public double Reputation { get; private set; }
    public double KillMonsterScore { get; set; }

    private bool IsAtWarToday { get; set; }
    private bool IsAtWarYesterday { get; set; }
    private DateTime LastWarDate { get; set; }
    public int LastAttackerUnitId { get; private set; }
    public DateTime LastIndependentUnitJoinDate { get; set; }

    public int AutoCollectTaxLoyalty { get; private set; }
    public int AutoGrantLoyalty { get; private set; }

    private double CurYearProfit { get; set; }
    private double LastYearProfit { get; set; }

    private double[] CurYearIncomes { get; } = new double[INCOME_TYPE_COUNT];
    private double[] LastYearIncomes { get; } = new double[INCOME_TYPE_COUNT];
    private double CurYearIncome { get; set; }
    private double LastYearIncome { get; set; }
    private double CurYearFixedIncome { get; set; }
    private double LastYearFixedIncome { get; set; }

    private double[] CurYearExpenses { get; } = new double[EXPENSE_TYPE_COUNT];
    private double[] LastYearExpenses { get; } = new double[EXPENSE_TYPE_COUNT];
    private double CurYearExpense { get; set; }
    private double LastYearExpense { get; set; }
    private double CurYearFixedExpense { get; set; }
    private double LastYearFixedExpense { get; set; }

    private double CurYearCheat { get; set; }
    private double LastYearCheat { get; set; }

    private double CurYearFoodIn { get; set; }
    private double LastYearFoodIn { get; set; }
    private double CurYearFoodOut { get; set; }
    private double LastYearFoodOut { get; set; }
    private double CurYearFoodChange { get; set; }
    private double LastYearFoodChange { get; set; }

    private double CurYearReputationChange { get; set; }
    private double LastYearReputationChange { get; set; }

    private readonly int[] _techLevels;
    private readonly double[] _researchProgress;
    public int[] KnownBases { get; } = new int[GameConstants.MAX_RACE];
    public int[] BaseCounts { get; } = new int[GameConstants.MAX_RACE];
    
    public int TotalPopulation { get; set; }
    public int TotalJoblessPopulation { get; set; }
    public int TotalShipCombatLevel { get; set; }

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
    private NationRelation[] NationRelations { get; } = new NationRelation[GameConstants.MAX_NATION];
    // replace status in struct NationRelation
    private int[] RelationStatuses { get; } = new int[GameConstants.MAX_NATION];
    // for seeking to indicate whether passing other nation region
    public bool[] RelationPassable { get; } = new bool[GameConstants.MAX_NATION];
    public bool[] RelationShouldAttack { get; } = new bool[GameConstants.MAX_NATION];
    public bool IsAlliedWithPlayer { get; private set; } // for fast access in visiting world functions

    //TODO bad code. Remove this flag
    public static int NationHandOverFlag { get; private set; }

    protected FirmRes FirmRes => Sys.Instance.FirmRes;
    protected RaceRes RaceRes => Sys.Instance.RaceRes;
    protected SpriteRes SpriteRes => Sys.Instance.SpriteRes;
    protected UnitRes UnitRes => Sys.Instance.UnitRes;
    protected MonsterRes MonsterRes => Sys.Instance.MonsterRes;
    protected TechRes TechRes => Sys.Instance.TechRes;

    protected Config Config => Sys.Instance.Config;
    protected Info Info => Sys.Instance.Info;

    protected NationArray NationArray => Sys.Instance.NationArray;
    protected FirmArray FirmArray => Sys.Instance.FirmArray;
    protected TownArray TownArray => Sys.Instance.TownArray;
    protected UnitArray UnitArray => Sys.Instance.UnitArray;
    protected RebelArray RebelArray => Sys.Instance.RebelArray;
    protected SpyArray SpyArray => Sys.Instance.SpyArray;
    protected RegionArray RegionArray => Sys.Instance.RegionArray;
    protected SiteArray SiteArray => Sys.Instance.SiteArray;
    protected TalkMsgArray TalkMsgArray => Sys.Instance.TalkMsgArray;
    protected NewsArray NewsArray => Sys.Instance.NewsArray;

    protected NationBase()
    {
        for (int i = 0; i < NationRelations.Length; i++)
            NationRelations[i] = new NationRelation();

        _techLevels = new int[TechRes.TechInfos.Length];
        _researchProgress = new double[TechRes.TechInfos.Length];
    }

    int IIdObject.GetId() => NationId;
    
    void IIdObject.SetId(int id)
    {
        NationId = id;
    }

    public virtual void Init(int nationType, int raceId, int colorSchemeId, int playerId = 0)
    {
        NationType = nationType;
        RaceId = raceId;
        ColorSchemeId = colorSchemeId;
        NationColor = ColorRemap.GetColorRemap(ColorRemap.ColorSchemes[NationId], false).MainColor;

        LastWarDate = Info.GameDate;

        //---------- init game vars ------------//

        double[] startUpCash = { 4000.0, 7000.0, 12000.0, 20000.0 };

        if (IsAI())
        {
            Cash = startUpCash[Config.AIStartUpCash - 1];
            if (Config.CustomAIStartUpCash > 0.0)
                Cash = Config.CustomAIStartUpCash;
        }
        else
        {
            Cash = startUpCash[Config.StartUpCash - 1];
            if (Config.CustomStartUpCash > 0.0)
                Cash = Config.CustomStartUpCash;
        }

        Food = 5000.0; // startup food is 5000 for all nations in all settings
        if (IsAI() && Config.CustomAIStartUpFood > 0.0)
            Food = Config.CustomAIStartUpFood;
        if (!IsAI() && Config.CustomStartUpFood > 0.0)
            Food = Config.CustomStartUpFood;

        //---- initialize this nation's relation on other nations ----//

        // #### Richard 6-12-2013: Moved this from InitRelation to here, so it works with new independent nations spawning #### //
        IsAlliedWithPlayer = false;

        foreach (Nation nation in NationArray)
        {
            InitRelation(nation);
            nation.InitRelation(this);
        }
    }

    private void InitRelation(NationBase otherNation)
    {
        int otherNationId = otherNation.NationId;
        NationRelation nationRelation = NationRelations[otherNationId - 1];

        SetRelationShouldAttack(otherNationId, otherNationId != NationId, InternalConstants.COMMAND_AUTO);

        // AI has contact with each other in the beginning of the game.
        if (IsAI() && NationArray[otherNationId].IsAI())
            nationRelation.HasContact = true;
        else
            nationRelation.HasContact = otherNationId == NationId || Config.ExploreWholeMap;

        nationRelation.TradeTreaty = otherNationId == NationId;

        nationRelation.Status = NATION_NEUTRAL;
        nationRelation.AIRelationLevel = NATION_NEUTRAL * RELATION_LEVEL_PER_STATUS;
        nationRelation.LastChangeStatusDate = Info.GameDate;

        if (otherNationId == NationId) // own nation
            RelationStatuses[otherNationId - 1] = NATION_ALLIANCE; // for facilitating searching
        else
            RelationStatuses[otherNationId - 1] = NATION_NEUTRAL;

        SetRelationPassable(otherNationId, NATION_FRIENDLY);
    }
    
    public void Deinit()
    {
        //---- delete all talk messages to/from this nation ----//

        TalkMsgArray.DeleteAllNationMessages(NationId);

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

                //TODO what about on ships??
                if (spy.SpyPlace == Spy.SPY_MOBILE)
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
            Sys.Instance.set_view_mode(InternalConstants.MODE_NORMAL);
    }

    private void CloseAllFirms()
    {
        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId == NationId)
            {
                FirmArray.DeleteFirm(firm);
            }
        }
    }

    private void DeinitAllUnits()
    {
        //--- update TotalHumanUnit so the numbers will be correct ---//

        // only do this in release version to on-fly fix bug
        //TODO
        NationArray.UpdateStatistic();

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
                UnitArray.DeleteUnit(unit);
        }
    }
    
    public void NextDay()
    {
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

        CheckWin();

        //-- if the player still hasn't selected a unit to succeed the died king, declare defeated if the all units are killed --//

        if (KingUnitId == 0)
            CheckLose();
    }

    public void NextMonth()
    {
        //--------------------------------------------------//
        // When the two nations, whose relationship is Tense,
        // do not have new conflicts for 3 years,
        // their relationship automatically becomes Neutral.
        //--------------------------------------------------//

        foreach (Nation nation in NationArray)
        {
            NationRelation nationRelation = GetRelation(nation.NationId);

            if (nationRelation.Status == NATION_TENSE && Info.GameDate >= nationRelation.LastChangeStatusDate.AddDays(365.0 * 3.0))
                SetRelationStatus(nation.NationId, NATION_NEUTRAL);

            if (nationRelation.Status == NATION_FRIENDLY)
                nationRelation.GoodRelationDurationRating += 0.2; // this is the monthly increase

            if (nationRelation.Status == NATION_ALLIANCE)
                nationRelation.GoodRelationDurationRating += 0.4;
        }

        //----- increase reputation gradually -----//

        if (Reputation < 100)
            ChangeReputation(0.5);

        if (KingUnitId != 0)
            KingLeadership = UnitArray[KingUnitId].Skill.SkillLevel;
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
    
    public virtual void ProcessAI()
    {
    }
    
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
                return RaceRes[RaceId].GetSingleName(NationNameId);
            else
                return RaceRes[RaceId].GetName(NationNameId);
        }
    }

    public void SetAtWarToday(int attackerUnitId = 0)
    {
        IsAtWarToday = true;
        if (attackerUnitId != 0)
            LastAttackerUnitId = attackerUnitId;
    }

    public bool IsOwn() => NationType == NATION_OWN;
    public bool IsAI() => NationType == NATION_AI;
    public bool IsRemote() => NationType == NATION_REMOTE;
    
    public bool IsAtWar()
    {
        return IsAtWarToday || IsAtWarYesterday;
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


    public NationRelation GetRelation(int nationId)
    {
        return NationRelations[nationId - 1];
    }
    
    protected virtual void SetRelationStatusAI(int nationId, int newStatus)
    {
    }

    public void ChangeAIRelationLevel(int nationId, int levelChange)
    {
        NationRelation nationRelation = GetRelation(nationId);

        int newLevel = nationRelation.AIRelationLevel + levelChange;

        newLevel = Math.Min(newLevel, 100);
        newLevel = Math.Max(newLevel, 0);

        nationRelation.AIRelationLevel = newLevel;
    }
    
    public void SetRelationStatus(int nationId, int newStatus, bool recursiveCall = false)
    {
        if (nationId == NationId) // cannot set relation to itself
            return;

        NationRelation nationRelation = GetRelation(nationId);

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
        // If the nation cease fire or form a friendly/alliance treaty with a nation.
        // And this nation current has plan to attack that nation, then cancel the plan.
        //
        //-------------------------------------------------//

        if (IsAI())
        {
            SetRelationStatusAI(nationId, newStatus);
        }

        //------------------------------------------------//

        RelationStatuses[nationId - 1] = newStatus;
        nationRelation.Status = newStatus;
        nationRelation.LastChangeStatusDate = Info.GameDate;

        int newRelationLevel = newStatus * RELATION_LEVEL_PER_STATUS;

        // only set it when the new value is lower than the current value
        if (newRelationLevel < nationRelation.AIRelationLevel)
            nationRelation.AIRelationLevel = newRelationLevel;

        SetRelationPassable(nationId, NATION_FRIENDLY);

        //---------- set should_attack -------//

        if (newStatus == NATION_ALLIANCE || newStatus == NATION_FRIENDLY || newStatus == NATION_TENSE)
        {
            SetRelationShouldAttack(nationId, false, InternalConstants.COMMAND_AUTO);
        }
        else if (newStatus == NATION_HOSTILE)
        {
            SetRelationShouldAttack(nationId, true, InternalConstants.COMMAND_AUTO);
        }

        //----- share the nation contact with each other -----//
        /*
            // these code segment will cause multiplayer sync problem
    
            if (newStatus == NATION_ALLIANCE)
            {
                Nation withNation = NationArray[nationId];
    
                foreach (Nation nation in NationArray)
                {
                    if (nation.NationId == NationId || nation.NationId == nationId)
                        continue;
                    
                    //-- if we have contact with this nation and our ally doesn't, share the contact with it --//
    
                    if (GetRelation(nation.NationId).HasContact && !withNation.GetRelation(nation.NationId).HasContact)
                    {
                        withNation.EstablishContact(nation.NationId);
                    }
                }
            }
        */

        //--- if this is a call from a client function, not a recursive call ---//

        if (!recursiveCall)
        {
            NationArray[nationId].SetRelationStatus(NationId, newStatus, true);

            //-- auto terminate their trade treaty if two nations go into a war --//

            if (newStatus == NATION_HOSTILE)
                SetTradeTreaty(nationId, false);
        }
    }

    public int GetRelationStatus(int nationId)
    {
        return RelationStatuses[nationId - 1];
    }

    private void SetRelationPassable(int nationId, int status)
    {
        RelationPassable[nationId - 1] = (RelationStatuses[nationId - 1] >= status);
    }

    public bool GetRelationPassable(int nationId)
    {
        return RelationPassable[nationId - 1];
    }

    public void SetRelationShouldAttack(int nationId, bool newValue, int remoteAction)
    {
        //if (!remoteAction && remote.is_enable())
        //{
            //short *shortPtr = (short *) remote.new_send_queue_msg(MSG_NATION_SET_SHOULD_ATTACK, 3*sizeof(short));
            //*shortPtr = nation_recno;
            //shortPtr[1] = nationRecno;
            //shortPtr[2] = newValue;
        //}
        //else
        //{
            RelationShouldAttack[nationId - 1] = newValue;
            GetRelation(nationId).ShouldAttack = newValue;
        //}
    }

    public bool GetRelationShouldAttack(int nationId)
    {
        return nationId == 0 || RelationShouldAttack[nationId - 1];
    }

    public void SetTradeTreaty(int nationId, bool allowFlag)
    {
        GetRelation(nationId).TradeTreaty = allowFlag;
        NationArray[nationId].GetRelation(NationId).TradeTreaty = allowFlag;
    }

    public void FormFriendlyTreaty(int withNationId)
    {
        SetRelationStatus(withNationId, NATION_FRIENDLY);
    }

    public void FormAllianceTreaty(int withNationId)
    {
        SetRelationStatus(withNationId, NATION_ALLIANCE);

        //--- allied nations are obliged to trade with each other ---//

        SetTradeTreaty(withNationId, true);

        if (withNationId == NationArray.PlayerId)
            IsAlliedWithPlayer = true;

        if (NationId == NationArray.PlayerId)
            NationArray[withNationId].IsAlliedWithPlayer = true;
    }

    public void EndTreaty(int withNationId, int newStatus)
    {
        //----- decrease reputation when terminating a treaty -----//

        Nation withNation = NationArray[withNationId];

        if (withNation.Reputation > 0)
        {
            int curStatus = GetRelationStatus(withNationId);

            if (curStatus == TalkMsg.TALK_END_FRIENDLY_TREATY)
                ChangeReputation(-withNation.Reputation * 10.0 / 100.0);
            if (curStatus == TalkMsg.TALK_END_ALLIANCE_TREATY)
                ChangeReputation(-withNation.Reputation * 20.0 / 100.0);
        }

        if (newStatus <= NATION_NEUTRAL)
        {
            GetRelation(withNationId).GoodRelationDurationRating = 0.0;
            withNation.GetRelation(NationId).GoodRelationDurationRating = 0.0;
        }

        SetRelationStatus(withNationId, newStatus);

        if (withNationId == NationArray.PlayerId)
            IsAlliedWithPlayer = false;

        if (NationId == NationArray.PlayerId)
            NationArray[withNationId].IsAlliedWithPlayer = false;
    }
    
    public void EstablishContact(int nationId)
    {
        GetRelation(nationId).HasContact = true;
        NationArray[nationId].GetRelation(NationId).HasContact = true;
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

    public void ImportGoods(int importType, int importNationId, double importAmt)
    {
        if (importNationId == NationId)
            return;

        NationRelation nationRelation = GetRelation(importNationId);

        nationRelation.CurYearImport[importType] += importAmt;
        nationRelation.CurYearImport[IMPORT_TOTAL] += importAmt;

        AddExpense(EXPENSE_IMPORTS, importAmt, true);
        NationArray[importNationId].AddIncome(INCOME_EXPORTS, importAmt, true);
    }

    public void GiveTribute(int toNationId, int tributeAmt)
    {
        Nation toNation = NationArray[toNationId];

        AddExpense(EXPENSE_TRIBUTE, tributeAmt);

        toNation.AddIncome(INCOME_TRIBUTE, tributeAmt);

        NationRelation nationRelation = GetRelation(toNationId);

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

    public void GiveTech(int toNationId, int techId, int techVersion)
    {
        Nation toNation = NationArray[toNationId];

        int curVersion = toNation.GetTechLevel(techId);

        if (curVersion < techVersion)
            toNation.SetTechLevel(techId, techVersion);

        NationRelation nationRelation = GetRelation(toNationId);

        nationRelation.LastGiveGiftDate = Info.GameDate;
        nationRelation.TotalGivenGiftAmount += (techVersion - curVersion) * 500; // one version level is worth $500

        //---- set the last rejected date so it won't request or give again soon ----//

        nationRelation.LastTalkRejectDates[TalkMsg.TALK_GIVE_TECH - 1] = default(DateTime);
        nationRelation.LastTalkRejectDates[TalkMsg.TALK_DEMAND_TECH - 1] = default(DateTime);

        NationRelation nationRelation2 = toNation.GetRelation(NationId);

        nationRelation2.LastTalkRejectDates[TalkMsg.TALK_GIVE_TECH - 1] = default(DateTime);
        nationRelation2.LastTalkRejectDates[TalkMsg.TALK_DEMAND_TECH - 1] = default(DateTime);
    }

    public int GetTechLevel(int techId)
    {
        return _techLevels[techId - 1];
    }

    public int GetTechLevelByUnitType(int unitType)
    {
        if (unitType == UnitConstants.UNIT_TRANSPORT || unitType == UnitConstants.UNIT_VESSEL)
            return 1;
        
        for (int techId = 1; techId <= TechRes.TechInfos.Length; techId++)
        {
            TechInfo techInfo = TechRes[techId];
            if (techInfo.UnitId == unitType)
                return GetTechLevel(techId);
        }

        return 0;
    }
    
    public void SetTechLevel(int techId, int techLevel)
    {
        _techLevels[techId - 1] = techLevel;

        //--- if the MAX level has been reached and there are still other firms researching this technology ---//

        TechInfo techInfo = TechRes[techId];
        if (techLevel == techInfo.MaxTechLevel)
        {
            //---- stop other firms researching the same tech -----//

            foreach (Firm firm in FirmArray)
            {
                if (firm.FirmType == Firm.FIRM_RESEARCH && firm.NationId == NationId)
                {
                    FirmResearch firmResearch = (FirmResearch)firm;
                    if (firmResearch.TechId == techId)
                        firmResearch.TerminateResearch();
                }
            }
        }
    }
    
    public bool CanResearch(int techId)
    {
        TechInfo techInfo = TechRes[techId];

        bool isParentTechInvented = techInfo.ParentUnitId == 0 || GetTechLevelByUnitType(techInfo.ParentUnitId) >= techInfo.ParentLevel;
        return isParentTechInvented && GetTechLevel(techId) < techInfo.MaxTechLevel;
    }

    public bool MakeResearchProgress(int techId, double progressPoint)
    {
        _researchProgress[techId - 1] += progressPoint;

        if (_researchProgress[techId - 1] > 100.0)
        {
            _researchProgress[techId - 1] = 0.0;
            SetTechLevel(techId, GetTechLevel(techId) + 1);
            
            foreach (Firm firm in FirmArray)
            {
                if (firm.FirmType == Firm.FIRM_RESEARCH && firm.NationId == NationId && firm.AIFirm)
                {
                    FirmResearch firmResearch = (FirmResearch)firm;
                    if (firmResearch.TechId == techId)
                        firmResearch.TerminateResearch();
                }
            }
            return true;
        }

        return false;
    }

    public double GetResearchProgress(int techId)
    {
        return _researchProgress[techId - 1];
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

        CurYearReputationChange += changeLevel;
    }
    
    public void BeingAttacked(int attackNationId)
    {
        if (NationArray.IsDeleted(attackNationId) || attackNationId == NationId)
            return;

        //--- if it is an accidental attack (e.g. bullets attack with spreading damages) ---//

        Nation attackNation = NationArray[attackNationId];

        if (!attackNation.GetRelation(NationId).ShouldAttack)
            return;

        //--- check if there a treaty between these two nations ---//

        NationRelation nationRelation = GetRelation(attackNationId);

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
                // so attackNation.EndTreaty() should be called instead of EndTreaty()
                attackNation.EndTreaty(NationId, NATION_HOSTILE);
            }
            else
            {
                SetRelationStatus(attackNationId, NATION_HOSTILE);
            }
        }
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

    private void ChangeAllPeopleLoyalty(double loyaltyChange, int raceId = 0)
    {
        //---- update loyalty of units in this nation ----//

        foreach (Unit unit in UnitArray)
        {
            if (unit.NationId != NationId)
                continue;
            
            if (unit.SpriteId == KingUnitId)
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

    public bool RevealedByPhoenix(int locX, int locY)
    {
        int effectiveRange = UnitRes[UnitConstants.UNIT_PHOENIX].VisualRange;

        foreach (Unit unit in UnitArray)
        {
            if (unit.UnitType == UnitConstants.UNIT_PHOENIX && unit.NationId == NationId)
            {
                if (Misc.PointsDistance(locX, locY, unit.NextLocX, unit.NextLocY) <= effectiveRange)
                {
                    return true;
                }
            }
        }

        return false;
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

        //--- if this unit currently has not have leadership ---//

        if (newKing.Skill.SkillId != Skill.SKILL_LEADING)
        {
            newKing.Skill.SkillId = Skill.SKILL_LEADING;
            newKing.Skill.SkillLevel = newKingLeadership;
        }

        //---- update loyalty of units in this nation ----//

        foreach (Unit unit in UnitArray)
        {
            if (unit.NationId != NationId)
                continue;
            
            if (unit.SpriteId == KingUnitId || unit.SpriteId == newKing.SpriteId)
                continue;

            unit.ChangeLoyalty(loyaltyChange + SucceedKingLoyaltyChange(unit.RaceId, newKing.RaceId, RaceId));
        }

        //---- update loyalty of units in camps and bases ----//

        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId != NationId)
                continue;

            if (firm.FirmType == Firm.FIRM_CAMP || firm.FirmType == Firm.FIRM_BASE)
            {
                foreach (Worker worker in firm.Workers)
                {
                    worker.ChangeLoyalty(loyaltyChange + SucceedKingLoyaltyChange(worker.RaceId, newKing.RaceId, RaceId));
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

                town.ChangeLoyalty(raceId, loyaltyChange + SucceedKingLoyaltyChange(raceId, newKing.RaceId, RaceId));
            }
        }

        SetKing(newKing.SpriteId, false);

        if (newKing.SpyId != 0)
        {
            Spy spy = SpyArray[newKing.SpyId];

            if (newKing.TrueNationId() == NationId) // if this is your spy
                spy.DropSpyIdentity();
            else
                spy.ThinkBecomeKing();
        }
        
        NewsArray.NewKing(NationId, newKing.SpriteId);
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
                return -GameConstants.NEW_KING_DIFFERENT_RACE_LOYALTY_DEC;
        }

        return 0;
    }
    
    public void SetKing(int kingUnitId, bool firstKing)
    {
        KingUnitId = kingUnitId;

        Unit kingUnit = UnitArray[KingUnitId];
        kingUnit.SetRank(Unit.RANK_KING);
        // clear the existing order, as there might be an assigning to firm/town order. But kings cannot be assigned to towns or firms as workers.
        kingUnit.Stop2();

        // for human players, the name is retrieved from NationArray.HumanNames
        if (NationType == NATION_AI || !firstKing) // for succession, no longer use the original player name
            NationNameId = kingUnit.NameId;
        else
            NationNameId = -NationId;

        RaceId = kingUnit.RaceId;
        KingLeadership = kingUnit.Skill.SkillLevel;
    }
    
    private void CheckWin()
    {
        bool hasWon = GoalDestroyNationAchieved() ||
                      GoalDestroyMonsterAchieved() ||
                      GoalPopulationAchieved() ||
                      GoalEconomicScoreAchieved() ||
                      GoalTotalScoreAchieved();

        if (!hasWon)
            return;

        // if the player achieves the goal, the player wins, if one of the other kingdoms achieves the goal, it wins.
        Sys.Instance.EndGame(NationId, false);
    }

    private bool GoalDestroyNationAchieved()
    {
        return NationArray.NationCount == 1;
    }

    private bool GoalDestroyMonsterAchieved()
    {
        if (!Config.GoalDestroyMonsterFlag) // this is not one of the required goals.
            return false;

        if (Config.MonsterType == Config.OPTION_MONSTER_NONE)
            return false;

        int monsterFirmCount = 0;
        foreach (Firm firm in FirmArray)
        {
            if (firm.FirmType == Firm.FIRM_MONSTER)
                monsterFirmCount++;
        }

        //------- when all monsters have been killed -------//
        if (monsterFirmCount == 0)
        {
            int mobileMonsterCount = 0;
            foreach (Unit unit in UnitArray)
            {
                if (UnitRes[unit.UnitType].UnitClass == UnitConstants.UNIT_CLASS_MONSTER)
                    mobileMonsterCount++;
            }

            if (mobileMonsterCount == 0)
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
        }

        return false;
    }

    private bool GoalPopulationAchieved()
    {
        if (!Config.GoalPopulationFlag) // this is not one of the required goals.
            return false;

        return AllPopulation() >= Config.GoalPopulation;
    }

    private bool GoalEconomicScoreAchieved()
    {
        if (!Config.GoalEconomicScoreFlag)
            return false;

        Info.SetRankData(false); // 0-set all nations, not just those that have contact with us

        return Info.GetRankScore(3, NationId) >= Config.GoalEconomicScore;
    }

    private bool GoalTotalScoreAchieved()
    {
        if (!Config.GoalTotalScoreFlag)
            return false;

        Info.SetRankData(false); // 0-set all nations, not just those that have contact with us

        return Info.GetTotalScore(NationId) >= Config.GoalTotalScore;
    }
    
    private void CheckLose()
    {
        //---- if the king of this nation is dead and it has no people left ----//

        if (KingUnitId == 0 && AllPopulation() == 0)
            Defeated();
    }
    
    protected void Defeated()
    {
        //---- if the defeated nation is the player's nation ----//

        if (NationId == NationArray.PlayerId)
        {
            Sys.Instance.EndGame(0, true); // the player lost the game 
        }
        else // AI and remote players 
        {
            NewsArray.NationDestroyed(NationId);
        }

        //---- delete this nation from NationArray ----//

        //TODO check
        NationArray.DeleteNation((Nation)this);
    }
    
    public void Surrender(int toNationId)
    {
        NewsArray.NationSurrender(NationId, toNationId);

        //---- the king demote himself to general first ----//

        if (KingUnitId != 0)
        {
            UnitArray[KingUnitId].SetRank(Unit.RANK_GENERAL);
            KingUnitId = 0;
        }

        //------- if the player surrenders --------//

        if (NationId == NationArray.PlayerId)
            Sys.Instance.EndGame(0, true, toNationId);

        //--- hand over the entire nation to another nation ---//

        HandOverTo(toNationId);
    }

    private void HandOverTo(int handoverNationId)
    {
        RebelArray.StopAttackNation(NationId);
        FirmArray.StopAttackNation(NationId);
        TownArray.StopAttackNation(NationId);
        UnitArray.StopAllWar(NationId);

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
            // If the unit is an overseer of a Camp or Base, 
            // the Camp or Base will change nation as a result. 
            //
            //---------------------------------------------------//

            if (unit.SpyId != 0)
                unit.SpyChangeNation(handoverNationId, InternalConstants.COMMAND_AUTO);
            else
                unit.ChangeNation(handoverNationId);
        }

        //------- hand over firms ---------//

        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId == NationId)
            {
                firm.ChangeNation(handoverNationId);
            }
        }

        //------- hand over towns ---------//

        foreach (Town town in TownArray)
        {
            if (town.NationId == NationId)
            {
                town.ChangeNation(handoverNationId);
            }
        }

        //-------------------------------------------------//
        //
        // For the spies of this nation cloaked into other nations,
        // we need to update their TrueNationId. 
        //
        //-------------------------------------------------//

        foreach (Spy spy in SpyArray)
        {
            if (spy.TrueNationId == NationId)
                spy.ChangeTrueNation(handoverNationId);
        }

        //------- delete this nation from NationArray -------//

        //TODO check
        NationArray.DeleteNation((Nation)this);
        NationHandOverFlag = 0;
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

    public double FoodChange365Days()
    {
        return LastYearFoodChange * (365 - Info.YearDay) / 365 + CurYearFoodChange;
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

        for (int i = 0; i < INCOME_TYPE_COUNT; i++)
        {
            if (i == INCOME_CHEAT)
                continue;
            
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

    public double ReputationChange365Days()
    {
        return LastYearReputationChange * (365 - Info.YearDay) / 365 + CurYearReputationChange;
    }
    
    
    public double TotalYearTrade(int nationId)
    {
        //TODO use CurYearImport also
        return GetRelation(nationId).LastYearImport[IMPORT_TOTAL] +
               NationArray[nationId].GetRelation(NationId).LastYearImport[IMPORT_TOTAL];
    }

    public int TradeRating(int nationId)
    {
        // use an absolute value 5000 as the divider.

        int tradeRating1 = 100 * (int)TotalYearTrade(nationId) / 5000;

        int tradeRating2 =
            50 * (int)NationArray[nationId].GetRelation(NationId).LastYearImport[IMPORT_TOTAL] / (int)(LastYearIncome + 1) +
            50 * (int)GetRelation(nationId).LastYearImport[IMPORT_TOTAL] / (int)(LastYearExpense + 1);

        return Math.Max(tradeRating1, tradeRating2);
    }

    protected int GetTotalHumanCount()
    {
        int totalHumanCount = 0;
        foreach (Unit unit in UnitArray)
        {
            if (unit.NationId == NationId && unit.RaceId != 0)
                totalHumanCount++;
        }

        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId == NationId && !FirmRes[firm.FirmType].LiveInTown)
            {
                foreach (Worker worker in firm.Workers)
                {
                    if (worker.RaceId != 0)
                        totalHumanCount++;
                }
            }
        }

        // Do not use unit.BelongsToNation(NationId) and check all spies because spy can be a worker
        foreach (Spy spy in SpyArray)
        {
            if (spy.TrueNationId == NationId)
                totalHumanCount++;
        }

        return totalHumanCount;
    }
	
    public int AllPopulation()
    {
        return TotalPopulation + GetTotalHumanCount();
    }

    public int TotalTechLevel(int unitClass = 0)
    {
        int totalTechLevel = 0;

        for (int techId = 1; techId <= TechRes.TechInfos.Length; techId++)
        {
            TechInfo techInfo = TechRes[techId];
            int techLevel = GetTechLevel(techId);

            if (techLevel > 0)
            {
                if (unitClass == 0 || UnitRes[techInfo.UnitId].UnitClass == unitClass)
                {
                    totalTechLevel += techLevel;
                }
            }
        }

        return totalTechLevel;
    }

    public void UpdateNationRating()
    {
        PopulationRating = GetPopulationRating();

        EconomicRating = GetEconomicRating();

        OverallRating = GetOverallRating();
    }

    private int GetPopulationRating()
    {
        return AllPopulation();
    }

    private int GetEconomicRating()
    {
        return (int)(Cash / 300 + TrueIncome365Days() / 2 + TrueProfit365Days());
    }

    private int GetOverallRating()
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
        if (Config.MonsterType == Config.OPTION_MONSTER_NONE)
            return 0;

        if (NationArray.MaxKillMonsterScore == 0)
            return 0;
        
        return 100 * (int)KillMonsterScore / NationArray.MaxKillMonsterScore;
    }

    public int OverallRankRating()
    {
        if (NationArray.MaxOverallRating == 0)
            return 0;

        return 100 * OverallRating / NationArray.MaxOverallRating;
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
    
    public string PeaceDurationString()
    {
        int peaceDays = (Info.GameDate - LastWarDate).Days;
        int peaceYear = peaceDays / 365;
        int peaceMonth = (peaceDays - peaceYear * 365) / 30;

        string str = String.Empty;

        if (peaceYear > 0)
        {
            str = peaceYear != 1 ? $"{peaceYear} years and " : "1 year and ";
        }

        str += peaceMonth != 1 ? $"{peaceMonth} months" : "1 month";

        return str;
    }
    
    #region SaveAndLoad

    public virtual void SaveTo(BinaryWriter writer)
    {
        writer.Write(NationId);
        writer.Write(NationType);
        writer.Write(RaceId);
        writer.Write(NationColor);
        writer.Write(ColorSchemeId);
        writer.Write(KingUnitId);
        writer.Write(KingLeadership);
        writer.Write(NationNameId);
        writer.Write(Cash);
        writer.Write(Food);
        writer.Write(Reputation);
        writer.Write(KillMonsterScore);
        writer.Write(IsAtWarToday);
        writer.Write(IsAtWarYesterday);
        writer.Write(LastWarDate.ToBinary());
        writer.Write(LastAttackerUnitId);
        writer.Write(LastIndependentUnitJoinDate.ToBinary());
        writer.Write(AutoCollectTaxLoyalty);
        writer.Write(AutoGrantLoyalty);
        writer.Write(CurYearProfit);
        writer.Write(LastYearProfit);
        for (int i = 0; i < CurYearIncomes.Length; i++)
            writer.Write(CurYearIncomes[i]);
        for (int i = 0; i < LastYearIncomes.Length; i++)
            writer.Write(LastYearIncomes[i]);
        writer.Write(CurYearIncome);
        writer.Write(LastYearIncome);
        writer.Write(CurYearFixedIncome);
        writer.Write(LastYearFixedIncome);
        for (int i = 0; i < CurYearExpenses.Length; i++)
            writer.Write(CurYearExpenses[i]);
        for (int i = 0; i < LastYearExpenses.Length; i++)
            writer.Write(LastYearExpenses[i]);
        writer.Write(CurYearExpense);
        writer.Write(LastYearExpense);
        writer.Write(CurYearFixedExpense);
        writer.Write(LastYearFixedExpense);
        writer.Write(CurYearCheat);
        writer.Write(LastYearCheat);
        writer.Write(CurYearFoodIn);
        writer.Write(LastYearFoodIn);
        writer.Write(CurYearFoodOut);
        writer.Write(LastYearFoodOut);
        writer.Write(CurYearFoodChange);
        writer.Write(LastYearFoodChange);
        writer.Write(CurYearReputationChange);
        writer.Write(LastYearReputationChange);
        for (int i = 0; i < _techLevels.Length; i++)
            writer.Write(_techLevels[i]);
        for (int i = 0; i < _researchProgress.Length; i++)
            writer.Write(_researchProgress[i]);
        for (int i = 0; i < KnownBases.Length; i++)
            writer.Write(KnownBases[i]);
        for (int i = 0; i < BaseCounts.Length; i++)
            writer.Write(BaseCounts[i]);
        writer.Write(TotalPopulation);
        writer.Write(TotalJoblessPopulation);
        writer.Write(TotalShipCombatLevel);
        for (int i = 0; i < RawCounts.Length; i++)
            writer.Write(RawCounts[i]);
        for (int i = 0; i < LastUnitNameIds.Length; i++)
            writer.Write(LastUnitNameIds[i]);
        writer.Write(PopulationRating);
        writer.Write(MilitaryRating);
        writer.Write(EconomicRating);
        writer.Write(OverallRating);
        writer.Write(EnemySoldierKilled);
        writer.Write(OwnSoldierKilled);
        writer.Write(EnemyCivilianKilled);
        writer.Write(OwnCivilianKilled);
        writer.Write(EnemyWeaponDestroyed);
        writer.Write(OwnWeaponDestroyed);
        writer.Write(EnemyShipDestroyed);
        writer.Write(OwnShipDestroyed);
        writer.Write(EnemyFirmDestroyed);
        writer.Write(OwnFirmDestroyed);
        for (int i = 0; i < NationRelations.Length; i++)
            NationRelations[i].SaveTo(writer);
        for (int i = 0; i < RelationStatuses.Length; i++)
            writer.Write(RelationStatuses[i]);
        for (int i = 0; i < RelationPassable.Length; i++)
            writer.Write(RelationPassable[i]);
        for (int i = 0; i < RelationShouldAttack.Length; i++)
            writer.Write(RelationShouldAttack[i]);
        writer.Write(IsAlliedWithPlayer);
    }

    public virtual void LoadFrom(BinaryReader reader)
    {
        NationId = reader.ReadInt32();
        NationType = reader.ReadInt32();
        RaceId = reader.ReadInt32();
        NationColor = reader.ReadByte();
        ColorSchemeId = reader.ReadInt32();
        KingUnitId = reader.ReadInt32();
        KingLeadership = reader.ReadInt32();
        NationNameId = reader.ReadInt32();
        Cash = reader.ReadDouble();
        Food = reader.ReadDouble();
        Reputation = reader.ReadDouble();
        KillMonsterScore = reader.ReadDouble();
        IsAtWarToday = reader.ReadBoolean();
        IsAtWarYesterday = reader.ReadBoolean();
        LastWarDate = DateTime.FromBinary(reader.ReadInt64());
        LastAttackerUnitId = reader.ReadInt32();
        LastIndependentUnitJoinDate = DateTime.FromBinary(reader.ReadInt64());
        AutoCollectTaxLoyalty = reader.ReadInt32();
        AutoGrantLoyalty = reader.ReadInt32();
        CurYearProfit = reader.ReadDouble();
        LastYearProfit = reader.ReadDouble();
        for (int i = 0; i < CurYearIncomes.Length; i++)
            CurYearIncomes[i] = reader.ReadDouble();
        for (int i = 0; i < LastYearIncomes.Length; i++)
            LastYearIncomes[i] = reader.ReadDouble();
        CurYearIncome = reader.ReadDouble();
        LastYearIncome = reader.ReadDouble();
        CurYearFixedIncome = reader.ReadDouble();
        LastYearFixedIncome = reader.ReadDouble();
        for (int i = 0; i < CurYearExpenses.Length; i++)
            CurYearExpenses[i] = reader.ReadDouble();
        for (int i = 0; i < LastYearExpenses.Length; i++)
            LastYearExpenses[i] = reader.ReadDouble();
        CurYearExpense = reader.ReadDouble();
        LastYearExpense = reader.ReadDouble();
        CurYearFixedExpense = reader.ReadDouble();
        LastYearFixedExpense = reader.ReadDouble();
        CurYearCheat = reader.ReadDouble();
        LastYearCheat = reader.ReadDouble();
        CurYearFoodIn = reader.ReadDouble();
        LastYearFoodIn = reader.ReadDouble();
        CurYearFoodOut = reader.ReadDouble();
        LastYearFoodOut = reader.ReadDouble();
        CurYearFoodChange = reader.ReadDouble();
        LastYearFoodChange = reader.ReadDouble();
        CurYearReputationChange = reader.ReadDouble();
        LastYearReputationChange = reader.ReadDouble();
        for (int i = 0; i < _techLevels.Length; i++)
            _techLevels[i] = reader.ReadInt32();
        for (int i = 0; i < _researchProgress.Length; i++)
            _researchProgress[i] = reader.ReadDouble();
        for (int i = 0; i < KnownBases.Length; i++)
            KnownBases[i] = reader.ReadInt32();
        for (int i = 0; i < BaseCounts.Length; i++)
            BaseCounts[i] = reader.ReadInt32();
        TotalPopulation = reader.ReadInt32();
        TotalJoblessPopulation = reader.ReadInt32();
        TotalShipCombatLevel = reader.ReadInt32();
        for (int i = 0; i < RawCounts.Length; i++)
            RawCounts[i] = reader.ReadInt32();
        for (int i = 0; i < LastUnitNameIds.Length; i++)
            LastUnitNameIds[i] = reader.ReadInt32();
        PopulationRating = reader.ReadInt32();
        MilitaryRating = reader.ReadInt32();
        EconomicRating = reader.ReadInt32();
        OverallRating = reader.ReadInt32();
        EnemySoldierKilled = reader.ReadInt32();
        OwnSoldierKilled = reader.ReadInt32();
        EnemyCivilianKilled = reader.ReadInt32();
        OwnCivilianKilled = reader.ReadInt32();
        EnemyWeaponDestroyed = reader.ReadInt32();
        OwnWeaponDestroyed = reader.ReadInt32();
        EnemyShipDestroyed = reader.ReadInt32();
        OwnShipDestroyed = reader.ReadInt32();
        EnemyFirmDestroyed = reader.ReadInt32();
        OwnFirmDestroyed = reader.ReadInt32();
        for (int i = 0; i < NationRelations.Length; i++)
            NationRelations[i].LoadFrom(reader);
        for (int i = 0; i < RelationStatuses.Length; i++)
            RelationStatuses[i] = reader.ReadInt32();
        for (int i = 0; i < RelationPassable.Length; i++)
            RelationPassable[i] = reader.ReadBoolean();
        for (int i = 0; i < RelationShouldAttack.Length; i++)
            RelationShouldAttack[i] = reader.ReadBoolean();
        IsAlliedWithPlayer = reader.ReadBoolean();
    }
	
    #endregion
}