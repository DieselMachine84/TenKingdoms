using System;
using System.Collections;
using System.Collections.Generic;

namespace TenKingdoms;

public partial class Nation : NationBase
{
    private DateTime SetupDate { get; }

    private readonly List<CaptureIndependentTask> _captureIndependentTasks = new List<CaptureIndependentTask>();
    private readonly List<BuildMineTask> _buildMineTasks = new List<BuildMineTask>();
    private readonly List<BuildFactoryTask> _buildFactoryTasks = new List<BuildFactoryTask>();
    private readonly List<BuildMarketTask> _buildMarketTasks = new List<BuildMarketTask>();
    private readonly List<BuildHarborTask> _buildHarborTasks = new List<BuildHarborTask>();
    private readonly List<BuildCampTask> _buildCampTasks = new List<BuildCampTask>();
    private readonly List<SettleTask> _settleTasks = new List<SettleTask>();
    private readonly List<RelocatePeasantsTask> _relocatePeasantsTasks = new List<RelocatePeasantsTask>();
    private readonly List<AssignGeneralTask> _assignGeneralTasks = new List<AssignGeneralTask>();
    private readonly List<ChangeFactoryProductionTask> _changeFactoryProductionTasks = new List<ChangeFactoryProductionTask>();
    private readonly List<ChangeMarketRestockTask> _changeMarketRestockTask = new List<ChangeMarketRestockTask>();
    private readonly List<StartCaravanTask> _startCaravanTasks = new List<StartCaravanTask>();
    private readonly List<WatchCaravanTask> _watchCaravanTasks = new List<WatchCaravanTask>();
    private readonly List<CollectTaxTask> _collectTaxTasks = new List<CollectTaxTask>();
    private readonly List<RecruitSoldiersTask> _recruitSoldiersTask = new List<RecruitSoldiersTask>();
    private readonly List<BuildShipTask> _buildShipTasks = new List<BuildShipTask>();
    private readonly List<SailShipTask> _sailShipTasks = new List<SailShipTask>();
    private readonly List<IdleUnitTask> _idleUnitTasks = new List<IdleUnitTask>();

    public Nation()
    {
        SetupDate = Info.game_date;
        
        _watchCaravanTasks.Add(new WatchCaravanTask(this));
        _collectTaxTasks.Add(new CollectTaxTask(this));
        _recruitSoldiersTask.Add(new RecruitSoldiersTask(this));
    }

    public void ProcessAI()
    {
        ThinkAboutNewTask();
        
        ProcessTasks();
    }

    private void ThinkAboutNewTask()
    {
        int[] intervalDaysArray = { 80, 40, 20, 10 };
        int intervalDays = intervalDaysArray[Config.ai_aggressiveness - Config.OPTION_LOW];

        switch ((Info.TotalDays + nation_recno * 8) % intervalDays)
        {
            case 0:
                ThinkCaptureIndependent();
                break;
            
            case 1:
                ThinkBuildMine();
                break;
            
            case 2:
                FindIdleUnits();
                break;
            
            case 3:
                ThinkDiplomacy();
                break;
            
            case 4:
                ThinkBuildHarbor();
                break;
        }

        if ((Info.TotalDays + nation_recno) % 10 == 0)
        {
            ThinkBuildCamp();
        }

        if ((Info.TotalDays + nation_recno) % 10 == 1)
        {
            ThinkAssignGeneral();
        }
        
        if ((Info.TotalDays + nation_recno) % 10 == 2)
        {
            ThinkSettle();
        }
        
        if ((Info.TotalDays + nation_recno) % 10 == 3)
        {
            ThinkRelocatePeasants();
        }
        
        if ((Info.TotalDays + nation_recno) % 10 == 4)
        {
            ThinkBuildFactory();
            ThinkChangeFactoryProduction();
        }
        
        if ((Info.TotalDays + nation_recno) % 10 == 5)
        {
            ThinkBuildMarket();
        }
        
        if ((Info.TotalDays + nation_recno) % 10 == 6)
        {
            ThinkStartCaravan();
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

    private void ProcessTasks()
    {
        if ((Info.TotalDays + nation_recno) % 10 == 0)
        {
            for (int i = _buildMineTasks.Count - 1; i >= 0; i--)
            {
                ProcessTask(_buildMineTasks[i], _buildMineTasks, i);
            }
            for (int i = _buildHarborTasks.Count - 1; i >= 0; i--)
            {
                ProcessTask(_buildHarborTasks[i], _buildHarborTasks, i);
            }
        }

        if ((Info.TotalDays + nation_recno) % 10 == 1)
        {
            for (int i = _buildCampTasks.Count - 1; i >= 0; i--)
            {
                ProcessTask(_buildCampTasks[i], _buildCampTasks, i);
            }
        }

        if ((Info.TotalDays + nation_recno) % 10 == 2)
        {
            for (int i = _settleTasks.Count - 1; i >= 0; i--)
            {
                ProcessTask(_settleTasks[i], _settleTasks, i);
            }
        }
        
        if ((Info.TotalDays + nation_recno) % 10 == 3)
        {
            for (int i = _idleUnitTasks.Count - 1; i >= 0; i--)
            {
                ProcessTask(_idleUnitTasks[i], _idleUnitTasks, i);
            }
        }
        
        if ((Info.TotalDays + nation_recno) % 10 == 4)
        {
            for (int i = _assignGeneralTasks.Count - 1; i >= 0; i--)
            {
                ProcessTask(_assignGeneralTasks[i], _assignGeneralTasks, i);
            }
        }
        
        if ((Info.TotalDays + nation_recno) % 10 == 5)
        {
            for (int i = _relocatePeasantsTasks.Count - 1; i >= 0; i--)
            {
                ProcessTask(_relocatePeasantsTasks[i], _relocatePeasantsTasks, i);
            }
        }
        
        if ((Info.TotalDays + nation_recno) % 10 == 6)
        {
            for (int i = _buildFactoryTasks.Count - 1; i >= 0; i--)
            {
                ProcessTask(_buildFactoryTasks[i], _buildFactoryTasks, i);
            }
            for (int i = _changeFactoryProductionTasks.Count - 1; i >= 0; i--)
            {
                ProcessTask(_changeFactoryProductionTasks[i], _changeFactoryProductionTasks, i);
            }
        }
        
        if ((Info.TotalDays + nation_recno) % 10 == 7)
        {
            for (int i = _buildMarketTasks.Count - 1; i >= 0; i--)
            {
                ProcessTask(_buildMarketTasks[i], _buildMarketTasks, i);
            }
            for (int i = _changeMarketRestockTask.Count - 1; i >= 0; i--)
            {
                ProcessTask(_changeMarketRestockTask[i], _changeMarketRestockTask, i);
            }
        }
        
        if ((Info.TotalDays + nation_recno) % 10 == 8)
        {
            for (int i = _startCaravanTasks.Count - 1; i >= 0; i--)
            {
                ProcessTask(_startCaravanTasks[i], _startCaravanTasks, i);
            }
            for (int i = _watchCaravanTasks.Count - 1; i >= 0; i--)
            {
                ProcessTask(_watchCaravanTasks[i], _watchCaravanTasks, i);
            }
            for (int i = _collectTaxTasks.Count - 1; i >= 0; i--)
            {
                ProcessTask(_collectTaxTasks[i], _collectTaxTasks, i);
            }
        }

        if ((Info.TotalDays + nation_recno) % 10 == 9)
        {
            for (int i = _recruitSoldiersTask.Count - 1; i >= 0; i--)
            {
                ProcessTask(_recruitSoldiersTask[i], _recruitSoldiersTask, i);
            }
            for (int i = _buildShipTasks.Count - 1; i >= 0; i--)
            {
                ProcessTask(_buildShipTasks[i], _buildShipTasks, i);
            }
            for (int i = _sailShipTasks.Count - 1; i >= 0; i--)
            {
                ProcessTask(_sailShipTasks[i], _sailShipTasks, i);
            }
        }
    }

    private IEnumerable<AITask> EnumerateAllUnitTasks()
    {
        foreach (var task in _buildMineTasks)
            yield return task;
        foreach (var task in _buildFactoryTasks)
            yield return task;
        foreach (var task in _buildMarketTasks)
            yield return task;
        foreach (var task in _buildHarborTasks)
            yield return task;
        foreach (var task in _buildCampTasks)
            yield return task;
        foreach (var task in _settleTasks)
            yield return task;
        foreach (var task in _assignGeneralTasks)
            yield return task;
        foreach (var task in _idleUnitTasks)
            yield return task;
        foreach (var task in _sailShipTasks)
            yield return task;
    }

    public bool IsUnitOnTask(int unitId)
    {
        foreach (AITask task in EnumerateAllUnitTasks())
        {
            if (task is IUnitTask unitTask && unitTask.UnitId == unitId)
                return true;
        }

        return false;
    }
    
    private void ThinkCaptureIndependent()
    {
        List<Town> possibleTowns = new List<Town>();
        foreach (Town town in TownArray)
        {
            if (town.NationId != 0)
                continue;

            if (town.RebelId != 0)
                continue;

            bool alreadyCapturing = false;
            foreach (CaptureIndependentTask task in _captureIndependentTasks)
            {
                if (task.TownId == town.TownId)
                {
                    alreadyCapturing = true;
                    break;
                }
            }

            if (!alreadyCapturing)
                possibleTowns.Add(town);
        }

        if (possibleTowns.Count == 0)
            return;

        List<Unit> possibleCapturerUnits = new List<Unit>();
        List<(Firm, Worker)> possibleCapturerSoldiers = new List<(Firm, Worker)>();
        List<(FirmInn, InnUnit)> possibleCapturerInnUnits = new List<(FirmInn, InnUnit)>();

        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId != nation_recno)
                continue;

            if (firm.FirmType == Firm.FIRM_CAMP || firm.FirmType == Firm.FIRM_BASE)
            {
                if (firm.OverseerId != 0)
                {
                    Unit overseer = UnitArray[firm.OverseerId];
                    //TODO also take reputation into account
                    if (overseer.Skill.SkillLevel > 50) // TODO constant should depend on preferences
                    {
                        bool isCapturer = false;
                        foreach (CaptureIndependentTask task in _captureIndependentTasks)
                        {
                            if (task.Capturers.Contains(overseer.SpriteId))
                            {
                                isCapturer = true;
                                break;
                            }
                        }

                        if (!isCapturer)
                            possibleCapturerUnits.Add(overseer);
                    }
                }

                foreach (Worker worker in firm.Workers)
                {
                    if (worker.SkillId == Skill.SKILL_LEADING && worker.SkillLevel > 50) // TODO constant should depend on preferences
                        possibleCapturerSoldiers.Add((firm, worker));
                }
            }

            if (firm.FirmType == Firm.FIRM_INN)
            {
                FirmInn inn = (FirmInn)firm;
                foreach (InnUnit innUnit in inn.inn_unit_array)
                {
                    // TODO constant should depend on preferences
                    if (innUnit.skill.SkillId == Skill.SKILL_LEADING && innUnit.skill.SkillLevel > 50 && ShouldHire(inn, innUnit))
                        possibleCapturerInnUnits.Add((inn, innUnit));
                }
            }
        }

        foreach (Unit unit in UnitArray)
        {
            //TODO add possible capturers from idle units
        }

        Town bestTown = null;
        int bestTownRating = 0;
        int majorityRace = 0;
        int majorityRacePopulation = 0;

        //TODO rate by races count, population, region, available generals, distance from our base, distance from enemies
        foreach (Town town in possibleTowns)
        {
            int racesCount = 0;
            for (int i = 0; i < town.RacesPopulation.Length; i++)
            {
                int racePopulation = town.RacesPopulation[i];
                if (racePopulation <= 3) // TODO constant should depend on preferences
                    continue;

                if (racePopulation > majorityRacePopulation)
                {
                    majorityRace = i + 1;
                    majorityRacePopulation = racePopulation;
                }

                racesCount++;
            }

            if (majorityRace == 0)
                continue;

            if (racesCount > 3) // TODO constant should depend on preferences
                continue;

            int rating = 0;
        }
    }

    private void ThinkBuildMine()
    {
        //TODO check if we have not enough population, enough money and should not build mine at all
        
        // find non-mined raw resource
        // rate by region, distance, distance from enemy

        int bestRating = 0;
        Site bestSite = null;
        foreach (Site site in SiteArray)
        {
            if (site.SiteType != Site.SITE_RAW || site.HasMine)
                continue;

            foreach (Town town in TownArray)
            {
                if (town.NationId != nation_recno)
                    continue;

                if (town.RegionId != site.RegionId)
                {
                    if (HasHarbor(town.RegionId, site.RegionId))
                    {
                        if (bestSite == null)
                        {
                            bestSite = site;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

                int siteRating = GameConstants.MapSize - Misc.RectsDistance(town.LocX1, town.LocY1, town.LocX2, town.LocY2,
                    site.LocX, site.LocY, site.LocX, site.LocY);
                if (siteRating > bestRating)
                {
                    bool hasBuildMineTask = false;
                    foreach (var buildMineTask in _buildMineTasks)
                    {
                        if (buildMineTask.SiteId == site.SiteId)
                            hasBuildMineTask = true;
                    }

                    if (!hasBuildMineTask)
                    {
                        bestRating = siteRating;
                        bestSite = site;
                    }
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
        bool HasFactory(Firm rawFirm, int rawId)
        {
            foreach (Firm firm in FirmArray)
            {
                if (firm.NationId != nation_recno || firm.FirmType != Firm.FIRM_FACTORY)
                    continue;

                //TODO other region
                if (firm.RegionId != rawFirm.RegionId)
                    continue;

                FirmFactory factory = (FirmFactory)firm;
                if (factory.UnderConstruction || factory.ProductId == rawId)
                    return true;
            }
            
            return false;
        }

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

        //TODO check if we have enough money

        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId != nation_recno || firm.UnderConstruction)
                continue;

            if (firm.FirmType == Firm.FIRM_MINE)
            {
                FirmMine mine = (FirmMine)firm;
                if (mine.RawId != 0 && mine.StockQty > 0.0)
                {
                    if (!HasFactory(firm, mine.RawId) && !HasBuildFactoryTask(firm))
                    {
                        _buildFactoryTasks.Add(new BuildFactoryTask(this, firm.FirmId, mine.RawId));
                    }
                }
            }

            if (firm.FirmType == Firm.FIRM_MARKET)
            {
                FirmMarket market = (FirmMarket)firm;
                for (int i = 0; i < market.MarketGoods.Length; i++)
                {
                    MarketGoods marketGoods = market.MarketGoods[i];
                    if (marketGoods.RawId != 0 && marketGoods.StockQty > 0.0)
                    {
                        if (!HasFactory(firm, marketGoods.RawId) && !HasBuildFactoryTask(firm))
                        {
                            _buildFactoryTasks.Add(new BuildFactoryTask(this, firm.FirmId, marketGoods.RawId));
                        }
                    }
                }
            }
        }
    }

    private void ThinkBuildMarket()
    {
        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId != nation_recno || firm.UnderConstruction)
                continue;

            if (firm.FirmType == Firm.FIRM_MINE)
            {
                FirmMine mine = (FirmMine)firm;
                if (mine.RawId != 0 && mine.StockQty > mine.MaxStockQty * 3.0 / 4.0)
                {
                    bool hasLinkedMarket = false;
                    foreach (int linkedFirmId in mine.LinkedFirms)
                    {
                        Firm linkedFirm = FirmArray[linkedFirmId];
                        if (linkedFirm.NationId == nation_recno && linkedFirm.FirmType == Firm.FIRM_MARKET)
                        {
                            FirmMarket market = (FirmMarket)linkedFirm;
                            if (market.UnderConstruction || market.IsRawMarket())
                                hasLinkedMarket = true;
                        }
                    }

                    if (!hasLinkedMarket)
                    {
                        bool hasBuildMarketTask = false;
                        foreach (BuildMarketTask buildMarketTask in _buildMarketTasks)
                        {
                            if (buildMarketTask.FirmId == mine.FirmId)
                                hasBuildMarketTask = true;
                        }
                        
                        if (!hasBuildMarketTask)
                            _buildMarketTasks.Add(new BuildMarketTask(this, firm.FirmId, 0));
                    }
                }
            }
        }

        foreach (Town town in TownArray)
        {
            if (town.NationId != nation_recno)
                continue;
            
            bool hasLinkedMarket = false;
            foreach (int linkedFirmId in town.LinkedFirms)
            {
                Firm linkedFirm = FirmArray[linkedFirmId];
                if (linkedFirm.NationId == nation_recno && linkedFirm.FirmType == Firm.FIRM_MARKET)
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

                    if (firm.FirmType == Firm.FIRM_MINE || firm.FirmType == Firm.FIRM_FACTORY ||
                        firm.FirmType == Firm.FIRM_MARKET || firm.FirmType == Firm.FIRM_HARBOR)
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
    }

    private void ThinkBuildHarbor()
    {
        if (Info.game_date < SetupDate.AddDays(180.0))
            return;
        
        foreach (Site site in SiteArray)
        {
            if (site.SiteType != Site.SITE_RAW || site.HasMine)
                continue;

            bool hasTownInSiteRegion = false;
            foreach (Town town in TownArray)
            {
                if (town.NationId == nation_recno && town.RegionId == site.RegionId)
                {
                    hasTownInSiteRegion = true;
                    break;
                }
            }

            if (!hasTownInSiteRegion)
            {
                RegionStat siteRegionStat = RegionArray.GetRegionStat(site.RegionId);

                foreach (Town town in TownArray)
                {
                    if (town.NationId != nation_recno)
                        continue;

                    bool townInConnectedRegion = false;
                    int seaRegionId = -1;
                    RegionStat townRegionStat = RegionArray.GetRegionStat(town.RegionId);
                    foreach (RegionPath townRegionPath in townRegionStat.ReachableRegions)
                    {
                        foreach (RegionPath siteRegionPath in siteRegionStat.ReachableRegions)
                        {
                            if (townRegionPath.SeaRegionId == siteRegionPath.SeaRegionId)
                            {
                                townInConnectedRegion = true;
                                seaRegionId = townRegionPath.SeaRegionId;
                            }
                        }
                    }

                    if (townInConnectedRegion)
                    {
                        if (!HasHarbor(town.RegionId, site.RegionId) && _buildHarborTasks.Count == 0)
                            _buildHarborTasks.Add(new BuildHarborTask(this, town.TownId, seaRegionId));
                    }
                }
            }
        }
    }

    private void ThinkBuildCamp()
    {
        foreach (Town town in TownArray)
        {
            if (town.NationId != nation_recno)
                continue;
            
            bool hasLinkedCamp = town.HasLinkedCamp(nation_recno, false);
            if (!hasLinkedCamp)
            {
                bool hasBuildCampTask = false;
                foreach (BuildCampTask buildCampTask in _buildCampTasks)
                {
                    if (buildCampTask.TownId == town.TownId)
                        hasBuildCampTask = true;
                }

                if (!hasBuildCampTask)
                    _buildCampTasks.Add(new BuildCampTask(this, town.TownId));
            }
        }
    }

    private void ThinkAssignGeneral()
    {
        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId != nation_recno)
                continue;

            if (firm.FirmType != Firm.FIRM_CAMP && firm.FirmType != Firm.FIRM_BASE)
                continue;
            
            if (firm.UnderConstruction)
                continue;

            if (firm.OverseerId == 0)
            {
                bool hasAssignGeneralTask = false;
                foreach (AssignGeneralTask assignGeneralTask in _assignGeneralTasks)
                {
                    if (assignGeneralTask.FirmId == firm.FirmId)
                        hasAssignGeneralTask = true;
                }
                
                if (!hasAssignGeneralTask)
                    _assignGeneralTasks.Add(new AssignGeneralTask(this, firm.FirmId));
            }
        }
    }

    private void ThinkSettle()
    {
        // Settle when
        // 1. Village population is close to maximum - TODO
        // 2. Village has different races - TODO
        // 3. A new mine is built

        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId != nation_recno || firm.FirmType != Firm.FIRM_MINE)
                continue;

            bool linkedToOurTown = false;
            foreach (int townId in firm.LinkedTowns)
            {
                Town town = TownArray[townId];
                if (town.NationId == nation_recno)
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
        // Relocate peasants when firm don't have enough workers

        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId != nation_recno)
                continue;

            if (firm.UnderConstruction || firm.Workers.Count == Firm.MAX_WORKER)
                continue;

            if (firm.FirmType == Firm.FIRM_MINE || firm.FirmType == Firm.FIRM_FACTORY ||
                firm.FirmType == Firm.FIRM_RESEARCH || firm.FirmType == Firm.FIRM_WAR_FACTORY)
            {
                bool hasJoblessPopulation = false;
                foreach (int townId in firm.LinkedTowns)
                {
                    Town town = TownArray[townId];
                    if (town.NationId == nation_recno && town.JoblessPopulation > 0)
                        hasJoblessPopulation = true;
                }

                if (!hasJoblessPopulation)
                    _relocatePeasantsTasks.Add(new RelocatePeasantsTask(this, firm.FirmId));
            }
        }
    }

    private void ThinkChangeFactoryProduction()
    {
        //
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
        if (harbor.BuildQueue.Count > 0 || harbor.BuildUnitId != 0)
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
        foreach (Unit unit in UnitArray)
        {
            if (unit.NationId != nation_recno)
                continue;

            if (!unit.IsVisible() || !unit.IsAIAllStop())
                continue;

            if (IsUnitOnTask(unit.SpriteId))
                continue;
            
            _idleUnitTasks.Add(new IdleUnitTask(this, unit.SpriteId));
        }
    }

    private void ThinkStartCaravan()
    {
        foreach (Firm firm in FirmArray)
        {
            if (firm is FirmMine mine)
            {
                if (mine.NationId != nation_recno)
                    continue;
                
                if (mine.UnderConstruction || mine.ReserveQty <= 0.0 || mine.StockQty < mine.MaxStockQty * 3.0 / 4.0)
                    continue;
                
                bool hasLinkedFactory = false;
                for (int i = 0; i < mine.LinkedFirms.Count; i++)
                {
                    Firm linkedFirm = FirmArray[mine.LinkedFirms[i]];
                    if (linkedFirm.NationId != mine.NationId)
                        continue;
                    
                    if (mine.LinkedFirmsEnable[i] != InternalConstants.LINK_EE)
                        continue;

                    if (linkedFirm is FirmFactory linkedFactory)
                    {
                        if (linkedFactory.UnderConstruction || linkedFactory.ProductId == mine.RawId)
                            hasLinkedFactory = true;
                    }
                }

                if (!hasLinkedFactory)
                {
                    bool hasStartCaravanTask = false;
                    foreach (StartCaravanTask startCaravanTask in _startCaravanTasks)
                    {
                        if (startCaravanTask.MineId == mine.FirmId)
                            hasStartCaravanTask = true;
                    }

                    if (!hasStartCaravanTask)
                        _startCaravanTasks.Add(new StartCaravanTask(this, mine.FirmId, 0, 0));
                }
            }

            if (firm is FirmFactory factory)
            {
                if (factory.NationId != nation_recno)
                    continue;
                
                if (factory.UnderConstruction || factory.StockQty <= 0.0 || factory.StockQty < factory.MaxStockQty * 3.0 / 4.0)
                    continue;
                
                bool hasLinkedMarket = false;
                for (int i = 0; i < factory.LinkedFirms.Count; i++)
                {
                    Firm linkedFirm = FirmArray[factory.LinkedFirms[i]];
                    if (linkedFirm.NationId != factory.NationId)
                        continue;
                    
                    if (factory.LinkedFirmsEnable[i] != InternalConstants.LINK_EE)
                        continue;

                    if (linkedFirm is FirmMarket linkedMarket)
                    {
                        if (linkedMarket.UnderConstruction || linkedMarket.IsRetailMarket())
                            hasLinkedMarket = true;
                    }
                }
                
                if (!hasLinkedMarket)
                {
                    bool hasStartCaravanTask = false;
                    foreach (StartCaravanTask startCaravanTask in _startCaravanTasks)
                    {
                        if (startCaravanTask.FactoryId == factory.FirmId)
                            hasStartCaravanTask = true;
                    }

                    if (!hasStartCaravanTask)
                        _startCaravanTasks.Add(new StartCaravanTask(this, 0, factory.FirmId, 0));
                }
            }

            if (firm is FirmMarket market)
            {
                if (market.NationId == nation_recno)
                    continue;

                if (!get_relation(market.NationId).trade_treaty)
                    continue;

                bool hasGoodsToImport = false;
                foreach (MarketGoods marketGoods in market.MarketGoods)
                {
                    if (marketGoods.ProductId != 0 && marketGoods.StockQty > market.MaxStockQty / 2.0)
                        hasGoodsToImport = true;
                }

                if (hasGoodsToImport)
                {
                    bool hasStartCaravanTask = false;
                    foreach (StartCaravanTask startCaravanTask in _startCaravanTasks)
                    {
                        if (startCaravanTask.MarketId == market.FirmId)
                            hasStartCaravanTask = true;
                    }

                    if (!hasStartCaravanTask)
                        _startCaravanTasks.Add(new StartCaravanTask(this, 0, 0, market.FirmId));
                }
            }
        }
    }
    
    private bool ShouldHire(FirmInn inn, InnUnit innUnit)
    {
        //TODO
        return true;
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
            foreach (Firm firm in FirmArray)
            {
                if (firm.NationId != nation_recno || firm.FirmType != Firm.FIRM_HARBOR)
                    continue;

                FirmHarbor harbor = (FirmHarbor)firm;
                if (harbor.LandRegionId == fromRegionId || harbor.SeaRegionId == seaRegionId)
                    return harbor;
            }
        }

        return null;
    }

    public UnitMarine FindTransportShip(int seaRegionId)
    {
        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId != nation_recno || firm.FirmType != Firm.FIRM_HARBOR || firm.UnderConstruction)
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
        
        foreach (Unit unit in UnitArray)
        {
            if (unit.NationId != nation_recno || unit.UnitType != UnitConstants.UNIT_TRANSPORT)
                continue;

            if (unit.RegionId() == seaRegionId && !IsUnitOnTask(unit.SpriteId))
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
