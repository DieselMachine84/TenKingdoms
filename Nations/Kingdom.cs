using System;
using System.Collections;
using System.Collections.Generic;

namespace TenKingdoms;

public partial class Nation : NationBase
{
    private const int MoneyHire = 0;
    private readonly int[] SpendMoneyLevels = new int[1];
    
    public List<Firm> KingdomFirms { get; } = new List<Firm>();
    public List<Firm> KingdomMines { get; } = new List<Firm>();
    public List<Firm> KingdomFactories { get; } = new List<Firm>();
    public List<Firm> KingdomResearches { get; } = new List<Firm>();
    public List<Firm> KingdomWarFactories { get; } = new List<Firm>();
    public List<Firm> KingdomMarkets { get; } = new List<Firm>();
    public List<Firm> KingdomHarbors { get; } = new List<Firm>();
    public List<Firm> KingdomCamps { get; } = new List<Firm>();
    public List<Firm> KingdomBases { get; } = new List<Firm>();
    public List<Firm> KingdomInns { get; } = new List<Firm>();
    public List<Town> KingdomTowns { get; } = new List<Town>();
    public List<Unit> KingdomUnits { get; } = new List<Unit>();
    public List<Unit> KingdomCaravans { get; } = new List<Unit>();
    public List<Unit> KingdomShips { get; } = new List<Unit>();
    
    private readonly List<BuildMineTask> _buildMineTasks = new List<BuildMineTask>();
    private readonly List<BuildFactoryTask> _buildFactoryTasks = new List<BuildFactoryTask>();
    private readonly List<BuildMarketTask> _buildMarketTasks = new List<BuildMarketTask>();
    private readonly List<BuildHarborTask> _buildHarborTasks = new List<BuildHarborTask>();
    private readonly List<BuildCampTask> _buildCampTasks = new List<BuildCampTask>();
    private readonly List<BuildInnTask> _buildInnTasks = new List<BuildInnTask>();
    private readonly List<CaptureIndependentTask> _captureIndependentTasks = new List<CaptureIndependentTask>();
    private readonly List<SettleTask> _settleTasks = new List<SettleTask>();
    private readonly List<RelocatePeasantsTask> _relocatePeasantsTasks = new List<RelocatePeasantsTask>();
    private readonly List<AssignGeneralTask> _assignGeneralTasks = new List<AssignGeneralTask>();
    private readonly List<ChangeFactoryProductionTask> _changeFactoryProductionTasks = new List<ChangeFactoryProductionTask>();
    private readonly List<ChangeMarketRestockTask> _changeMarketRestockTask = new List<ChangeMarketRestockTask>();
    private readonly List<BuildShipTask> _buildShipTasks = new List<BuildShipTask>();
    private readonly List<SailShipTask> _sailShipTasks = new List<SailShipTask>();
    private readonly List<IdleUnitTask> _idleUnitTasks = new List<IdleUnitTask>();
    
    private readonly ManageCaravansTask _manageCaravanTask;
    private readonly ManageTradeShipsTask _manageTradeShipsTask;
    private readonly CollectTaxTask _collectTaxTask;
    private readonly RecruitSoldiersTask _recruitSoldiersTask;
    private readonly ReduceConstructionWorkersTask _reduceConstructionWorkersTask;

    public int PrefMineFactoryDistance { get; }
    public int PrefMinFactoryRawResource { get; }
    public int PrefBaseTownMinPopulation { get; }
    public int PrefTownMinJoblessPopulation { get; }
    public int PrefMaxCaravanStopDistance { get; }
    public int PrefCashReserve { get; }
    public int PrefFoodReserve { get; }
    public int PrefConstructionWorkersCountPercent { get; }
    public int PrefMinGeneralLevelForCapturing { get; }
    public int PrefAcceptableIndependentVillageResistance { get; }
    public int PrefHireUnits { get; }

    public Nation()
    {
        PrefMineFactoryDistance = 20 + Misc.Random(20);
        PrefMinFactoryRawResource = 250 + Misc.Random(200);
        PrefBaseTownMinPopulation = 20 + Misc.Random(20);
        PrefTownMinJoblessPopulation = 8 + Misc.Random(16);
        PrefMaxCaravanStopDistance = 100 + Misc.Random(300);
        PrefCashReserve = 500 + Misc.Random(1500);
        PrefFoodReserve = 500 + Misc.Random(1500);
        PrefConstructionWorkersCountPercent = 25 + Misc.Random(50);
        PrefMinGeneralLevelForCapturing = 50 + Misc.Random(20);
        PrefAcceptableIndependentVillageResistance = 10 + Misc.Random(30);
        PrefHireUnits = 0 + Misc.Random(1);
        
        _manageCaravanTask = new ManageCaravansTask(this);
        _manageTradeShipsTask = new ManageTradeShipsTask(this);
        _collectTaxTask = new CollectTaxTask(this);
        _recruitSoldiersTask = new RecruitSoldiersTask(this);
        _reduceConstructionWorkersTask = new ReduceConstructionWorkersTask(this);
    }

    public override void ProcessAI()
    {
        UpdateKingdomData();
        ThinkAboutNewTask();
        ProcessTasks();
    }

    private void UpdateKingdomData()
    {
        KingdomFirms.Clear();
        KingdomMines.Clear();
        KingdomFactories.Clear();
        KingdomResearches.Clear();
        KingdomWarFactories.Clear();
        KingdomMarkets.Clear();
        KingdomHarbors.Clear();
        KingdomCamps.Clear();
        KingdomBases.Clear();
        KingdomInns.Clear();
        KingdomTowns.Clear();
        KingdomUnits.Clear();
        KingdomCaravans.Clear();
        KingdomShips.Clear();

        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId != NationId)
                continue;
                
            KingdomFirms.Add(firm);
            if (firm.FirmType == Firm.FIRM_MINE)
                KingdomMines.Add(firm);
            if (firm.FirmType == Firm.FIRM_FACTORY)
                KingdomFactories.Add(firm);
            if (firm.FirmType == Firm.FIRM_RESEARCH)
                KingdomResearches.Add(firm);
            if (firm.FirmType == Firm.FIRM_WAR_FACTORY)
                KingdomWarFactories.Add(firm);
            if (firm.FirmType == Firm.FIRM_MARKET)
                KingdomMarkets.Add(firm);
            if (firm.FirmType == Firm.FIRM_HARBOR)
                KingdomHarbors.Add(firm);
            if (firm.FirmType == Firm.FIRM_CAMP)
                KingdomCamps.Add(firm);
            if (firm.FirmType == Firm.FIRM_BASE)
                KingdomBases.Add(firm);
            if (firm.FirmType == Firm.FIRM_INN)
                KingdomInns.Add(firm);
        }

        foreach (Town town in TownArray)
        {
            if (town.NationId != NationId)
                continue;
            
            KingdomTowns.Add(town);
        }

        foreach (Unit unit in UnitArray)
        {
            if (unit.NationId != NationId)
                continue;
            
            KingdomUnits.Add(unit);
            
            if (unit.UnitType == UnitConstants.UNIT_CARAVAN)
                KingdomCaravans.Add(unit);
            
            if (unit.UnitType == UnitConstants.UNIT_TRANSPORT || unit.UnitType == UnitConstants.UNIT_VESSEL ||
                unit.UnitType == UnitConstants.UNIT_CARAVEL || unit.UnitType == UnitConstants.UNIT_GALLEON)
                KingdomShips.Add(unit);
        }
    }

    private void ThinkAboutNewTask()
    {
        int shortInterval = Config.AIAggressiveness switch
        {
            Config.OPTION_LOW => 32,
            Config.OPTION_MODERATE => 16,
            Config.OPTION_HIGH => 8,
            Config.OPTION_VERY_HIGH => 4,
            _ => 0
        };

        for (int i = shortInterval; i <= 32; i += shortInterval)
        {
            switch ((Info.TotalDays + NationId * 15) % shortInterval + i - shortInterval)
            {
                case 0:
                    ThinkBuildCamp();
                    break;
                case 1:
                    ThinkAssignGeneral();
                    break;
                case 2:
                    ThinkDiplomacy();
                    break;
            }
        }

        int longInterval = shortInterval * 2;
        for (int i = longInterval; i <= 32 * 2; i += longInterval)
        {
            switch ((Info.TotalDays + NationId * 15) % longInterval + i - longInterval)
            {
                case 0:
                    ThinkBuildMine();
                    break;
                case 1:
                    ThinkBuildFactory();
                    break;
                case 2:
                    ThinkBuildMarket();
                    break;
                case 3:
                    ThinkBuildHarbor();
                    break;
                case 4:
                    ThinkBuildInn();
                    break;
                case 5:
                    ThinkSettle();
                    break;
                case 6:
                    ThinkRelocatePeasants();
                    break;
                case 7:
                    ThinkCaptureIndependent();
                    break;
                case 8:
                    FindIdleUnits();
                    break;
            }
        }
    }

    private void ProcessTasks()
    {
        int shortInterval = Config.AIAggressiveness switch
        {
            Config.OPTION_LOW => 32,
            Config.OPTION_MODERATE => 16,
            Config.OPTION_HIGH => 8,
            Config.OPTION_VERY_HIGH => 4,
            _ => 0
        };

        for (int i = shortInterval; i <= 32; i += shortInterval)
        {
            int taskIndex = 0;
            switch ((Info.TotalDays + NationId * 15) % shortInterval + i - shortInterval)
            {
                case 0:
                    for (taskIndex = _buildMineTasks.Count - 1; taskIndex >= 0; taskIndex--)
                        ProcessTask(_buildMineTasks[taskIndex], _buildMineTasks, taskIndex);
                    break;
                case 1:
                    for (taskIndex = _buildFactoryTasks.Count - 1; taskIndex >= 0; taskIndex--)
                        ProcessTask(_buildFactoryTasks[taskIndex], _buildFactoryTasks, taskIndex);
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    for (taskIndex = _buildMarketTasks.Count - 1; taskIndex >= 0; taskIndex--)
                        ProcessTask(_buildMarketTasks[taskIndex], _buildMarketTasks, taskIndex);
                    break;
                case 5:
                    for (taskIndex = _buildHarborTasks.Count - 1; taskIndex >= 0; taskIndex--)
                        ProcessTask(_buildHarborTasks[taskIndex], _buildHarborTasks, taskIndex);
                    break;
                case 6:
                    for (taskIndex = _buildCampTasks.Count - 1; taskIndex >= 0; taskIndex--)
                        ProcessTask(_buildCampTasks[taskIndex], _buildCampTasks, taskIndex);
                    break;
                case 7:
                    break;
                case 8:
                    for (taskIndex = _buildInnTasks.Count - 1; taskIndex >= 0; taskIndex--)
                        ProcessTask(_buildInnTasks[taskIndex], _buildInnTasks, taskIndex);
                    break;
                case 9:
                    for (taskIndex = _assignGeneralTasks.Count - 1; taskIndex >= 0; taskIndex--)
                        ProcessTask(_assignGeneralTasks[taskIndex], _assignGeneralTasks, taskIndex);
                    break;
                case 10:
                    for (taskIndex = _settleTasks.Count - 1; taskIndex >= 0; taskIndex--)
                        ProcessTask(_settleTasks[taskIndex], _settleTasks, taskIndex);
                    break;
                case 11:
                    for (taskIndex = _relocatePeasantsTasks.Count - 1; taskIndex >= 0; taskIndex--)
                        ProcessTask(_relocatePeasantsTasks[taskIndex], _relocatePeasantsTasks, taskIndex);
                    break;
                case 12:
                    for (taskIndex = _changeFactoryProductionTasks.Count - 1; taskIndex >= 0; taskIndex--)
                        ProcessTask(_changeFactoryProductionTasks[taskIndex], _changeFactoryProductionTasks, taskIndex);
                    break;
                case 13:
                    for (taskIndex = _changeMarketRestockTask.Count - 1; taskIndex >= 0; taskIndex--)
                        ProcessTask(_changeMarketRestockTask[taskIndex], _changeMarketRestockTask, taskIndex);
                    break;
                case 14:
                    _manageCaravanTask.Process();
                    break;
                case 15:
                    _manageTradeShipsTask.Process();
                    break;
                case 16:
                    _collectTaxTask.Process();
                    break;
                case 17:
                    _recruitSoldiersTask.Process();
                    break;
                case 18:
                    for (taskIndex = _buildShipTasks.Count - 1; taskIndex >= 0; taskIndex--)
                        ProcessTask(_buildShipTasks[taskIndex], _buildShipTasks, taskIndex);
                    break;
                case 19:
                    for (taskIndex = _sailShipTasks.Count - 1; taskIndex >= 0; taskIndex--)
                        ProcessTask(_sailShipTasks[taskIndex], _sailShipTasks, taskIndex);
                    break;
                case 20:
                    for (taskIndex = _captureIndependentTasks.Count - 1; taskIndex >= 0; taskIndex--)
                        ProcessTask(_captureIndependentTasks[taskIndex], _captureIndependentTasks, taskIndex);
                    break;
                case 21:
                    break;
                case 22:
                    break;
                case 23:
                    break;
                case 24:
                    break;
                case 25:
                    break;
                case 26:
                    break;
                case 27:
                    break;
                case 28:
                    break;
                case 29:
                    break;
                case 30:
                    _reduceConstructionWorkersTask.Process();
                    break;
                case 31:
                    for (taskIndex = _idleUnitTasks.Count - 1; taskIndex >= 0; taskIndex--)
                        ProcessTask(_idleUnitTasks[taskIndex], _idleUnitTasks, taskIndex);
                    break;
            }
        }
    }

    private void ProcessTask(AITask task, IList tasks, int index)
    {
        if (task.ShouldCancel())
        {
            task.Cancel();
            tasks.RemoveAt(index);
            return;
        }

        task.Process();
    }
    
    public bool IsUnitOnTask(int unitId)
    {
        foreach (var task in _buildMineTasks)
            if (task.UnitId == unitId)
                return true;
        foreach (var task in _buildFactoryTasks)
            if (task.UnitId == unitId)
                return true;
        foreach (var task in _buildMarketTasks)
            if (task.UnitId == unitId)
                return true;
        foreach (var task in _buildHarborTasks)
            if (task.UnitId == unitId)
                return true;
        foreach (var task in _buildCampTasks)
            if (task.UnitId == unitId)
                return true;
        foreach (var task in _buildInnTasks)
            if (task.UnitId == unitId)
                return true;
        foreach (var task in _settleTasks)
            if (task.UnitId == unitId)
                return true;
        foreach (var task in _relocatePeasantsTasks)
            if (task.UnitId == unitId)
                return true;
        foreach (var task in _assignGeneralTasks)
            if (task.UnitId == unitId)
                return true;
        foreach (var task in _idleUnitTasks)
            if (task.UnitId == unitId)
                return true;
        foreach (var task in _sailShipTasks)
            if (task.UnitId == unitId)
                return true;
        foreach (var task in _captureIndependentTasks)
            if (task.IsUnitOnTask(unitId))
                return true;

        return false;
    }

    public (Firm, int, int, int, int) FindBestGeneral(Town town, int raceId, int minSkillLevel)
    {
        Firm selectedFirm = null;
        int selectedOverseerId = 0;
        int selectedWorkerId = 0;
        int selectedInnUnitId = 0;
        int bestRating = Int16.MinValue;
        
        foreach (Firm firm in KingdomFirms)
        {
            if (firm.FirmType == Firm.FIRM_CAMP || firm.FirmType == Firm.FIRM_BASE)
            {
                if (firm.OverseerId != 0)
                {
                    Unit overseer = UnitArray[firm.OverseerId];
                    if (overseer.RaceId == raceId && overseer.Skill.SkillLevel > minSkillLevel)
                    {
                        //TODO check that this general is not capturing another town or can be replaced
                        int rating = overseer.Skill.SkillLevel;
                        rating -= Misc.FirmTownDistance(firm, town) / 10;
                        if (rating > bestRating)
                        {
                            bestRating = rating;
                            selectedFirm = firm;
                            selectedOverseerId = firm.OverseerId;
                            selectedWorkerId = 0;
                            selectedInnUnitId = 0;
                        }
                    }
                }
            }

            if (firm.FirmType == Firm.FIRM_CAMP)
            {
                for (int i = 0; i < firm.Workers.Count; i++)
                {
                    Worker worker = firm.Workers[i];
                    if (worker.RaceId == raceId && worker.SkillId == Skill.SKILL_LEADING && worker.SkillLevel > minSkillLevel)
                    {
                        int rating = worker.SkillLevel;
                        rating -= Misc.FirmTownDistance(firm, town) / 10;
                        if (rating > bestRating)
                        {
                            bestRating = rating;
                            selectedFirm = firm;
                            selectedOverseerId = 0;
                            selectedWorkerId = i + 1;
                            selectedInnUnitId = 0;
                        }
                    }
                }
            }

            if (firm.FirmType == Firm.FIRM_INN)
            {
                FirmInn inn = (FirmInn)firm;
                for (int i = 0; i < inn.InnUnits.Count; i++)
                {
                    InnUnit innUnit = inn.InnUnits[i];
                    if (UnitRes[innUnit.UnitType].RaceId == raceId && innUnit.Skill.SkillId == Skill.SKILL_LEADING &&
                        innUnit.Skill.SkillLevel > minSkillLevel && ShouldHire(inn, innUnit))
                    {
                        int rating = innUnit.Skill.SkillLevel;
                        rating -= Misc.FirmTownDistance(firm, town) / 10;
                        if (rating > bestRating)
                        {
                            bestRating = rating;
                            selectedFirm = firm;
                            selectedOverseerId = 0;
                            selectedWorkerId = 0;
                            selectedInnUnitId = i + 1;
                        }
                    }
                }
            }
        }

        return (selectedFirm, selectedOverseerId, selectedWorkerId, selectedInnUnitId, bestRating);
    }
    
    private void ThinkCaptureIndependent()
    {
        bool HasCaptureIndependentTask(int townId)
        {
            foreach (CaptureIndependentTask task in _captureIndependentTasks)
            {
                if (task.TownId == townId)
                    return true;
            }

            return false;
        }

        //TODO use pref
        if (_captureIndependentTasks.Count >= 3)
            return;
        
        List<Town> possibleTowns = new List<Town>();
        foreach (Town town in TownArray)
        {
            if (town.NationId != 0 || town.RebelId != 0)
                continue;

            if (town.Population < 10)
                continue;
            
            if (!HasCaptureIndependentTask(town.TownId))
                possibleTowns.Add(town);
        }

        if (possibleTowns.Count == 0)
            return;

        Town bestTown = null;
        int bestTownRating = Int16.MinValue;

        //TODO rate by races count, population, region, available generals, distance from our base, distance from enemies
        foreach (Town town in possibleTowns)
        {
            int townRating = town.Population * 3;
            
            int racesCount = 0;
            for (int i = 0; i < town.RacesPopulation.Length; i++)
            {
                int racePopulation = town.RacesPopulation[i];
                if (racePopulation > 3) // TODO constant should depend on preferences
                    racesCount++;
            }

            townRating += 100 / racesCount;

            var (selectedFirm, _, _, _, bestRating) = FindBestGeneral(town, town.MajorityRace(), PrefMinGeneralLevelForCapturing);

            if (selectedFirm == null)
                continue;

            townRating += bestRating;

            if (townRating > bestTownRating)
            {
                bestTownRating = townRating;
                bestTown = town;
            }
        }

        if (bestTown != null)
        {
            _captureIndependentTasks.Add(new CaptureIndependentTask(this, bestTown.TownId));
        }
    }

    private void ThinkBuildMine()
    {
        // find non-mined raw resource
        // rate by region, distance, distance from enemy

        int bestRating = Int16.MaxValue;
        Site bestSite = null;
        foreach (Site site in SiteArray)
        {
            if (site.SiteType != Site.SITE_RAW || site.HasMine)
                continue;

            bool hasBuildMineTask = false;
            foreach (var buildMineTask in _buildMineTasks)
            {
                if (buildMineTask.SiteId == site.SiteId)
                    hasBuildMineTask = true;
            }

            if (hasBuildMineTask)
                continue;
            
            foreach (Town town in KingdomTowns)
            {
                if (town.RegionId != site.RegionId && !HasHarbor(town.RegionId, site.RegionId))
                    continue;

                //TODO use path distance instead of RectsDistance
                //TODO if kingdom has already mine of the same resource take it into account
                //TODO take distance to enemies into account
                //TODO if raw resource is on another island take it into account
                int siteRating = Misc.RectsDistance(town.LocX1, town.LocY1, town.LocX2, town.LocY2,
                    site.LocX, site.LocY, site.LocX, site.LocY);
                if (siteRating < bestRating)
                {
                    bestRating = siteRating;
                    bestSite = site;
                }
            }
        }

        if (bestSite != null)
        {
            //TODO check if we should capture several mines simultaneously
            if (_buildMineTasks.Count == 0)
                _buildMineTasks.Add(new BuildMineTask(this, bestSite.SiteId));
        }
    }

    private void ThinkBuildFactory()
    {
        bool HasBuildFactoryTask(Firm rawFirm)
        {
            foreach (BuildFactoryTask buildFactoryTask in _buildFactoryTasks)
            {
                if (buildFactoryTask.FirmId == rawFirm.FirmId)
                    return true;
            }

            if (_changeFactoryProductionTasks.Count > 0)
                return true;

            return false;
        }
        
        void CheckAddBuildFactoryTask(Firm rawFirm, int rawId, double rawStockQty)
        {
            List<FirmFactory> availableFactories = new List<FirmFactory>();
            int minFactoryRawResource = Int16.MaxValue;
            foreach (Firm firm in KingdomFactories)
            {
                //TODO other region
                if (firm.RegionId != rawFirm.RegionId)
                    continue;

                FirmFactory factory = (FirmFactory)firm;
                //TODO use path distance
                if ((factory.UnderConstruction || factory.ProductId == rawId) && Misc.FirmsDistance(rawFirm, factory) <= PrefMineFactoryDistance * 3 / 2)
                    availableFactories.Add(factory);

                if (!factory.UnderConstruction)
                {
                    if ((int)factory.RawStockQty < minFactoryRawResource)
                        minFactoryRawResource = (int)factory.RawStockQty;
                }
                else
                {
                    minFactoryRawResource = 0;
                }
            }
            
            if (availableFactories.Count != 0)
            {
                if (minFactoryRawResource > PrefMinFactoryRawResource && rawStockQty > 450.0)
                    _buildFactoryTasks.Add(new BuildFactoryTask(this, rawFirm.FirmId, rawId));
            }
            else
            {
                _buildFactoryTasks.Add(new BuildFactoryTask(this, rawFirm.FirmId, rawId));
            }
        }

        if (Cash < PrefCashReserve || Food < PrefFoodReserve)
            return;

        foreach (Firm firm in KingdomMines)
        {
            if (firm.UnderConstruction)
                continue;
            
            FirmMine mine = (FirmMine)firm;
            if (mine.RawId != 0 && mine.StockQty > 0.0)
            {
                if (!HasBuildFactoryTask(firm))
                    CheckAddBuildFactoryTask(firm, mine.RawId, mine.StockQty);
            }
        }

        foreach (Firm firm in KingdomMarkets)
        {
            if (firm.UnderConstruction)
                continue;
            
            FirmMarket market = (FirmMarket)firm;
            for (int i = 0; i < market.MarketGoods.Length; i++)
            {
                MarketGoods marketGoods = market.MarketGoods[i];
                if (marketGoods.RawId != 0 && marketGoods.StockQty > 0.0)
                {
                    if (!HasBuildFactoryTask(firm))
                        CheckAddBuildFactoryTask(firm, marketGoods.RawId, marketGoods.StockQty);
                }
            }
        }
    }

    private void ThinkBuildMarket()
    {
        foreach (Firm firm in KingdomMines)
        {
            if (firm.UnderConstruction)
                continue;
            
            FirmMine mine = (FirmMine)firm;
            if (mine.RawId != 0 && mine.StockQty > mine.MaxStockQty * 3.0 / 4.0)
                AddBuildFirmMarketTask(mine);
        }
        
        foreach (Firm firm in KingdomHarbors)
        {
            if (firm.UnderConstruction)
                continue;
            
            AddBuildFirmMarketTask(firm);
        }

        foreach (Town town in KingdomTowns)
        {
            bool hasLinkedMarket = false;
            foreach (int linkedFirmId in town.LinkedFirms)
            {
                Firm linkedFirm = FirmArray[linkedFirmId];
                if (linkedFirm.NationId == NationId && linkedFirm.FirmType == Firm.FIRM_MARKET)
                {
                    FirmMarket market = (FirmMarket)linkedFirm;
                    if (market.UnderConstruction || market.IsRetailMarket())
                        hasLinkedMarket = true;
                }
            }

            if (!hasLinkedMarket)
            {
                bool hasOtherFirmsToTrade = false;
                foreach (Firm firm in FirmArray)
                {
                    if (firm.RegionId != town.RegionId)
                        continue;

                    if (firm.FirmType == Firm.FIRM_FACTORY || firm.FirmType == Firm.FIRM_MARKET || firm.FirmType == Firm.FIRM_HARBOR)
                    {
                        hasOtherFirmsToTrade = true;
                        break;
                    }
                }

                if (hasOtherFirmsToTrade)
                {
                    bool hasBuildMarketTask = false;
                    foreach (BuildMarketTask buildMarketTask in _buildMarketTasks)
                    {
                        if (buildMarketTask.TownId == town.TownId)
                            hasBuildMarketTask = true;
                    }

                    if (!hasBuildMarketTask)
                        _buildMarketTasks.Add(new BuildMarketTask(this, 0, town.TownId));
                }
            }
        }
        
        //TODO build market near independent village
    }

    private void AddBuildFirmMarketTask(Firm firm)
    {
        bool hasLinkedMarket = false;
        foreach (int linkedFirmId in firm.LinkedFirms)
        {
            Firm linkedFirm = FirmArray[linkedFirmId];
            if (linkedFirm.NationId == NationId && linkedFirm.FirmType == Firm.FIRM_MARKET)
            {
                FirmMarket market = (FirmMarket)linkedFirm;
                if (market.UnderConstruction || (firm.FirmType == Firm.FIRM_MINE ? market.IsRawMarket() : market.IsRetailMarket()))
                    hasLinkedMarket = true;
            }
        }

        if (!hasLinkedMarket)
        {
            bool hasBuildMarketTask = false;
            foreach (BuildMarketTask buildMarketTask in _buildMarketTasks)
            {
                if (buildMarketTask.FirmId == firm.FirmId)
                    hasBuildMarketTask = true;
            }

            if (!hasBuildMarketTask)
                _buildMarketTasks.Add(new BuildMarketTask(this, firm.FirmId, 0));
        }
    }

    private void ThinkBuildHarbor()
    {
        bool hasRawResource = false;
        bool hasRawResourceOnKingdomIsland = false;
        foreach (Site site in SiteArray)
        {
            if (site.SiteType != Site.SITE_RAW || site.HasMine)
                continue;

            hasRawResource = true;
            
            foreach (Town town in KingdomTowns)
            {
                if (town.RegionId == site.RegionId)
                    hasRawResourceOnKingdomIsland = true;
            }
        }

        if (hasRawResource && !hasRawResourceOnKingdomIsland)
        {
            foreach (Site site in SiteArray)
            {
                if (site.SiteType != Site.SITE_RAW || site.HasMine)
                    continue;

                //TODO duplicate code - rewrite
                bool hasTownInSiteRegion = false;
                foreach (Town town in KingdomTowns)
                {
                    if (town.RegionId == site.RegionId)
                        hasTownInSiteRegion = true;
                }

                if (!hasTownInSiteRegion)
                {
                    AddBuildHarborTask(site.RegionId);
                }
            }
        }

        List<int> kingdomRegions = new List<int>();
        foreach (Town town in KingdomTowns)
        {
            if (!kingdomRegions.Contains(town.RegionId))
                kingdomRegions.Add(town.RegionId);
        }
        
        foreach (Town otherTown in TownArray)
        {
            if (otherTown.NationId != NationId && !kingdomRegions.Contains(otherTown.RegionId))
                AddBuildHarborTask(otherTown.RegionId);
        }
    }

    private void AddBuildHarborTask(int toRegionId)
    {
        foreach (Town town in KingdomTowns)
        {
            //TODO select best town
            int seaRegionId = GetSeaRegion(town.RegionId, toRegionId);
            if (seaRegionId != -1)
            {
                if (!HasHarbor(town.RegionId, toRegionId) && _buildHarborTasks.Count == 0)
                    _buildHarborTasks.Add(new BuildHarborTask(this, town.TownId, seaRegionId));
            }
        }
    }
    
    private void ThinkBuildCamp()
    {
        foreach (Town town in KingdomTowns)
        {
            if (!town.HasLinkedCamp(NationId, false))
                AddBuildCampTask(town.TownId);
        }
    }

    public void AddBuildCampTask(int townId)
    {
        foreach (BuildCampTask buildCampTask in _buildCampTasks)
        {
            if (buildCampTask.TownId == townId)
                return;
        }

        _buildCampTasks.Add(new BuildCampTask(this, townId));
    }

    private void ThinkBuildInn()
    {
        if (Cash < PrefCashReserve)
            return;

        if (_buildInnTasks.Count > 0)
            return;
        
        if (KingdomInns.Count == 0)
        {
            Town bestTown = null;
            int bestRating = Int16.MinValue;
            foreach (Town town in KingdomTowns)
            {
                //TODO check if there is a place available to build inn and if there is already an inn near this town
                //TODO try to build inns in different regions and different places of the map
                if (town.Population > bestRating)
                {
                    bestRating = town.Population;
                    bestTown = town;
                }
            }
            
            if (bestTown != null)
                _buildInnTasks.Add(new BuildInnTask(this, bestTown.TownId));
        }
        
        //TODO build more inns depending on kingdom power and preferences
    }

    public void AddAssignGeneralTask(int firmId, int generalId)
    {
        foreach (AssignGeneralTask assignGeneralTask in _assignGeneralTasks)
        {
            if (assignGeneralTask.FirmId == firmId)
                return;
        }
                
        _assignGeneralTasks.Add(new AssignGeneralTask(this, firmId, generalId));
    }

    private void ThinkAssignGeneral()
    {
        foreach (Firm firm in KingdomFirms)
        {
            if (firm.UnderConstruction)
                continue;

            if (firm.FirmType != Firm.FIRM_CAMP && firm.FirmType != Firm.FIRM_BASE)
                continue;

            //TODO overseer may be on attack/defence task
            if (firm.OverseerId == 0)
                AddAssignGeneralTask(firm.FirmId, 0);
        }
    }

    private void ThinkSettle()
    {
        foreach (Firm firm in KingdomMines)
        {
            bool linkedToOurTown = false;
            foreach (int townId in firm.LinkedTowns)
            {
                Town town = TownArray[townId];
                if (town.NationId == NationId)
                    linkedToOurTown = true;
            }

            if (!linkedToOurTown)
            {
                bool hasSettleTask = false;
                foreach (SettleTask settleTask in _settleTasks)
                {
                    if (settleTask.FirmId == firm.FirmId)
                        hasSettleTask = true;
                }

                if (!hasSettleTask)
                    _settleTasks.Add(new SettleTask(this, firm.FirmId));
            }
        }
    }

    private void ThinkRelocatePeasants()
    {
        foreach (Firm firm in KingdomFirms)
        {
            if (firm.UnderConstruction || firm.Workers.Count == Firm.MAX_WORKER)
                continue;

            if (firm.FirmType == Firm.FIRM_MINE || firm.FirmType == Firm.FIRM_FACTORY ||
                firm.FirmType == Firm.FIRM_RESEARCH || firm.FirmType == Firm.FIRM_WAR_FACTORY)
            {
                foreach (int townId in firm.LinkedTowns)
                {
                    Town town = TownArray[townId];
                    if (town.NationId != NationId || town.JoblessPopulation > 0 || town.Population >= GameConstants.MAX_TOWN_POPULATION - 5)
                        continue;

                    int runningTasksCount = 0;
                    foreach (RelocatePeasantsTask relocatePeasantsTask in _relocatePeasantsTasks)
                    {
                        if (relocatePeasantsTask.TownId == town.TownId)
                            runningTasksCount++;
                    }
                    
                    if (runningTasksCount < 4)
                        _relocatePeasantsTasks.Add(new RelocatePeasantsTask(this, town.TownId));
                }
            }
        }
    }

    public void AddChangeFactoryProductionTask(int factoryId, int productId)
    {
        _changeFactoryProductionTasks.Add(new ChangeFactoryProductionTask(this, factoryId, productId));
    }

    public void AddChangeMarketRestockTask(int marketId, int restockType)
    {
        _changeMarketRestockTask.Add(new ChangeMarketRestockTask(this, marketId, restockType));
    }

    public void AddBuildShipTask(FirmHarbor harbor, int shipType)
    {
        if (harbor.UnderConstruction)
            return;
        
        if (harbor.BuildQueue.Count > 0 || harbor.BuildUnitType != 0)
            return;

        int shipCount = 0;
        foreach (Unit unit in KingdomShips)
        {
            if (unit.UnitType == shipType)
                shipCount++;
        }

        //TODO do not build too many ships
        if (shipCount >= 3)
            return;
        
        bool hasBuildShipTask = false;
        foreach (BuildShipTask buildShipTask in _buildShipTasks)
        {
            if (buildShipTask.HarborId == harbor.FirmId)
                hasBuildShipTask = true;
        }
        
        if (!hasBuildShipTask)
            _buildShipTasks.Add(new BuildShipTask(this, harbor.FirmId, shipType));
    }

    public void AddSailShipTask(UnitMarine ship, int targetLocX, int targetLocY)
    {
        bool hasSailShipTask = false;
        foreach (SailShipTask sailShipTask in _sailShipTasks)
        {
            if (sailShipTask.ShipId == ship.SpriteId)
                hasSailShipTask = true;
        }
        
        if (!hasSailShipTask)
            _sailShipTasks.Add(new SailShipTask(this, ship.SpriteId, targetLocX, targetLocY));
    }

    private void FindIdleUnits()
    {
        foreach (Unit unit in KingdomUnits)
        {
            //TODO better check that if ship is waiting for people that it means it is not idle
            if (!unit.IsVisible() || !unit.IsAIAllStop())
                continue;

            if (unit is UnitMarine ship && ship.UnitsOnBoard.Count > 0)
                continue;

            if (IsUnitOnTask(unit.SpriteId))
                continue;
            
            _idleUnitTasks.Add(new IdleUnitTask(this, unit.SpriteId));
        }
    }
    
    private bool ShouldHire(FirmInn inn, InnUnit innUnit)
    {
        return ShouldSpendMoney(MoneyHire, innUnit.HireCost);
    }

    private bool ShouldSpendMoney(int target, int amount)
    {
        return false;
    }
    
    public int GetSeaRegion(int fromRegionId, int toRegionId)
    {
        RegionStat fromRegionStat = RegionArray.GetRegionStat(fromRegionId);
        RegionStat toRegionStat = RegionArray.GetRegionStat(toRegionId);
        foreach (RegionPath fromRegionPath in fromRegionStat.ReachableRegions)
        {
            foreach (RegionPath toRegionPath in toRegionStat.ReachableRegions)
            {
                if (fromRegionPath.SeaRegionId == toRegionPath.SeaRegionId)
                {
                    return fromRegionPath.SeaRegionId;
                }
            }
        }

        return -1;
    }

    private bool HasHarbor(int fromRegionId, int toRegionId)
    {
        return FindHarbor(fromRegionId, toRegionId) != null;
    }

    public FirmHarbor FindHarbor(int fromRegionId, int toRegionId)
    {
        int seaRegionId = GetSeaRegion(fromRegionId, toRegionId);
        if (seaRegionId != -1)
        {
            foreach (Firm firm in KingdomHarbors)
            {
                FirmHarbor harbor = (FirmHarbor)firm;
                if (harbor.LandRegionId == fromRegionId || harbor.SeaRegionId == seaRegionId)
                    return harbor;
            }
        }

        return null;
    }

    public UnitMarine FindTransportShip(int seaRegionId)
    {
        foreach (Firm firm in KingdomHarbors)
        {
            if (firm.UnderConstruction)
                continue;

            FirmHarbor harbor = (FirmHarbor)firm;
            if (harbor.SeaRegionId != seaRegionId)
                continue;

            foreach (int shipId in harbor.Ships)
            {
                UnitMarine ship = (UnitMarine)UnitArray[shipId];
                if (ship.UnitType == UnitConstants.UNIT_TRANSPORT && harbor.SailShip(shipId, InternalConstants.COMMAND_AI))
                {
                    return ship;
                }
            }
        }
        
        foreach (Unit unit in KingdomShips)
        {
            if (unit.UnitType != UnitConstants.UNIT_TRANSPORT)
                continue;

            if (unit.RegionId() == seaRegionId && unit.IsAIAllStop() && !IsUnitOnTask(unit.SpriteId))
                return (UnitMarine)unit;
        }
        
        return null;
    }
    
    #region Old AI stubs

    public const int ACTION_AI_BUILD_FIRM = 1;
    public const int ACTION_AI_ASSIGN_OVERSEER = 2;
    public const int ACTION_AI_ASSIGN_CONSTRUCTION_WORKER = 3;
    public const int ACTION_AI_ASSIGN_WORKER = 4;
    public const int ACTION_AI_ASSIGN_SPY = 5;
    public const int ACTION_AI_SCOUT = 6;
    public const int ACTION_AI_SETTLE_TO_OTHER_TOWN = 7;
    public const int ACTION_AI_PROCESS_TALK_MSG = 8;
    public const int ACTION_AI_SEA_TRAVEL = 9;
    public const int ACTION_AI_SEA_TRAVEL2 = 10;
    public const int ACTION_AI_SEA_TRAVEL3 = 11;

    public const int SEA_ACTION_SETTLE = 1;
    public const int SEA_ACTION_BUILD_CAMP = 2;
    public const int SEA_ACTION_ASSIGN_TO_FIRM = 3;
    public const int SEA_ACTION_MOVE = 4;
    public const int SEA_ACTION_NONE = 5;

    public int pref_force_projection;
    public int pref_military_development; // pref_military_development + pref_economic_development = 100
    public int pref_economic_development;
    public int pref_inc_pop_by_capture; // pref_inc_pop_by_capture + pref_inc_pop_by_growth = 100
    public int pref_inc_pop_by_growth;
    public int pref_peacefulness;
    public int pref_military_courage;
    public int pref_territorial_cohesiveness;
    public int pref_trading_tendency;
    public int pref_allying_tendency;
    public int pref_honesty;
    public int pref_town_harmony;
    public int pref_loyalty_concern;
    public int pref_forgiveness;
    public int pref_collect_tax;
    public int pref_hire_unit;
    public int pref_use_weapon;
    public int pref_keep_general; // whether to keep currently non-useful the general, or demote them.
    public int pref_keep_skilled_unit; // whether to keep currently non-useful skilled units, or assign them to towns.
    public int pref_diplomacy_retry; // tedency to retry diplomatic actions after previous ones have been rejected.
    public int pref_attack_monster;
    public int pref_spy;
    public int pref_counter_spy;
    public int pref_food_reserve;
    public int pref_cash_reserve;
    public int pref_use_marine;
    public int pref_unit_chase_distance;
    public int pref_repair_concern;
    public int pref_scout;

    public int ai_base_town_count;
    public int ai_capture_enemy_town_recno;
    public int[] firm_should_close_array = new int[Firm.MAX_FIRM_TYPE];

    public List<int> ai_town_array = new List<int>();
    public List<int> ai_base_array = new List<int>();
    public List<int> ai_mine_array = new List<int>();
    public List<int> ai_factory_array = new List<int>();
    public List<int> ai_camp_array = new List<int>();
    public List<int> ai_research_array = new List<int>();
    public List<int> ai_war_array = new List<int>();
    public List<int> ai_harbor_array = new List<int>();
    public List<int> ai_market_array = new List<int>();
    public List<int> ai_inn_array = new List<int>();
    public List<int> ai_general_array = new List<int>();
    public List<int> ai_caravan_array = new List<int>();
    public List<int> ai_ship_array = new List<int>();
    public List<AIRegion> ai_region_array = new List<AIRegion>();
    
    public List<AttackCamp> attack_camps = new List<AttackCamp>();
    
    public void add_town_info(int townRecno)
    {
    }

    public void del_town_info(int townRecno)
    {
    }

    public void add_firm_info(int firmId, int firmRecno)
    {
    }

    public void del_firm_info(int firmId, int firmRecno)
    {
    }

    public void add_general_info(int unitRecno)
    {
    }

    public void del_general_info(int unitRecno)
    {
    }
    
    public void add_caravan_info(int unitRecno)
    {
    }

    public void del_caravan_info(int unitRecno)
    {
    }

    public void add_ship_info(int unitRecno)
    {
    }

    public void del_ship_info(int unitRecno)
    {
    }

    public ActionNode add_action(int xLoc, int yLoc, int refXLoc, int refYLoc, int actionMode, int actionPara,
        int instanceCount = 1, int unitRecno = 0, int actionPara2 = 0, List<int> groupUnitArray = null)
    {
        return null;
    }

    public ActionNode get_action(int actionXLoc, int actionYLoc, int refXLoc, int refYLoc,
        int actionMode, int actionPara, int unitRecno = 0, int checkMode = 0)
    {
        return null;
    }

    public ActionNode get_action(int recNo)
    {
        return null;
    }
    
    public void del_action(ActionNode actionNode)
    {
    }

    public int action_count()
    {
        return 0;
    }

    public bool is_action_exist(int actionMode, int actionPara, int regionId = 0)
    {
        return false;
    }
    
    public bool is_build_action_exist(int firmId, int xLoc, int yLoc)
    {
        return false;
    }

    public int process_action_id(int actionId)
    {
        return 0;
    }

    public void action_finished(int aiActionId, int unitRecno = 0, int actionFailure = 0)
    {
    }

    public void action_failure(int aiActionId, int unitRecno = 0)
    {
    }

    public bool find_best_firm_loc(int buildFirmId, int refXLoc, int refYLoc, out int resultXLoc, out int resultYLoc)
    {
        resultXLoc = -1;
        resultYLoc = -1;
        return false;
    }

    public bool can_ai_build(int firmId)
    {
        return false;
    }

    public bool ai_has_enough_food()
    {
        return false;
    }

    public bool ai_should_spend(int importanceRating, double spendAmt = 0)
    {
        return false;
    }

    public bool ai_should_attack_friendly(int friendlyNationRecno, int attackTemptation)
    {
        return false;
    }

    public bool ai_attack_target(int targetXLoc, int targetYLoc, int targetCombatLevel, bool justMoveToFlag = false,
        int attackerMinCombatLevel = 0, int leadAttackCampRecno = 0, bool useAllCamp = false)
    {
        return false;
    }

    public int attack_enemy_town_defense(Town targetTown, bool useAllCamp = false)
    {
        return 0;
    }

    public int ai_defend(int attackerUnitRecno)
    {
        return 0;
    }

    public bool ai_should_hire_unit(int importanceRating)
    {
        return false;
    }

    public int recruit_jobless_worker(Firm destFirm, int preferedRaceId = 0)
    {
        return 0;
    }

    public int find_best_capturer(int townRecno, int raceId, ref int bestTargetResistance)
    {
        return 0;
    }

    public bool mobilize_capturer(int unitRecno)
    {
        return false;
    }

    public bool should_use_cash_to_capture()
    {
        return false;
    }

    public int train_unit(int skillId, int raceId, int destX, int destY, out int trainTownRecno, int actionId = 0)
    {
        trainTownRecno = 0;
        return 0;
    }

    public int max_human_battle_ship_count()
    {
        return 0;
    }

    public bool has_trade_ship(int firmRecno1, int firmRecno2)
    {
        return false;
    }

    public bool ai_has_too_many_camp()
    {
        return false;
    }

    public int ai_supported_inn_count()
    {
        return 0;
    }

    public bool ai_should_create_new_spy(int onlyCounterSpy)
    {
        return false;
    }

    public bool think_spy_new_mission(int raceId, int regionId, out int loc_x1, out int loc_y1, out int cloakedNationRecno)
    {
        loc_x1 = -1;
        loc_y1 = -1;
        cloakedNationRecno = 0;
        return false;
    }

    public void ai_start_spy_new_mission(Unit spyUnit, int loc_x1, int loc_y1, int cloakedNationRecno)
    {
    }

    public int ai_trade_with_rating(int withNationRecno)
    {
        return 0;
    }

    public int total_alliance_military()
    {
        return 0;
    }

    public int total_enemy_military()
    {
        return 0;
    }

    public bool ai_has_should_close_camp(int regionId)
    {
        return false;
    }

    public bool think_capture_new_enemy_town(Town capturerTown, bool useAllCamp = false)
    {
        return false;
    }

    public AIRegion get_ai_region(int regionId)
    {
        return null;
    }

    public void update_ai_region()
    {
    }

    public bool should_diplomacy_retry(int talkId, int nationRecno)
    {
        return false;
    }

    public void notify_talk_msg(TalkMsg talkMsg)
    {
    }

    public void ai_notify_reply(int talkMsgRecno)
    {
    }

    public void ai_end_treaty(int nationRecno)
    {
    }

    public bool ai_is_sea_travel_safe()
    {
        return false;
    }

    public void ai_sea_attack_target(int locX, int locY)
    {
    }
    
    #endregion
}
