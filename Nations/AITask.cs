using System;
using System.Collections.Generic;

namespace TenKingdoms;

public abstract class AITask
{
    protected Nation Nation { get; }

    protected FirmRes FirmRes => Sys.Instance.FirmRes;
    protected World World => Sys.Instance.World;
    
    protected FirmArray FirmArray => Sys.Instance.FirmArray;
    protected TownArray TownArray => Sys.Instance.TownArray;
    protected UnitArray UnitArray => Sys.Instance.UnitArray;
    protected SiteArray SiteArray => Sys.Instance.SiteArray;
    protected RegionArray RegionArray => Sys.Instance.RegionArray;

    protected AITask(Nation nation)
    {
        Nation = nation;
    }

    public abstract bool ShouldCancel();

    public virtual void Cancel()
    {
    }

    public abstract void Process();
    
    protected int FindBuilder(int buildLocX, int buildLocY)
    {
        Location location = World.GetLoc(buildLocX, buildLocY);
        int builderId = FindBuilderInRegion(location.RegionId, buildLocX, buildLocY);
        
        //TODO hire builder from inn
        //TODO check idle construction workers
        
        if (builderId == 0)
        {
            List<int> connectedSeas = new List<int>();
            RegionStat regionStat = RegionArray.GetRegionStat(location.RegionId);
            foreach (RegionPath regionPath in regionStat.ReachableRegions)
            {
                if (!connectedSeas.Contains(regionPath.SeaRegionId))
                    connectedSeas.Add(regionPath.SeaRegionId);
            }
            
            List<int> connectedLands = new List<int>();
            foreach (RegionInfo regionInfo in RegionArray.RegionInfos)
            {
                if (regionInfo.RegionType != RegionType.LAND || regionInfo.RegionId == location.RegionId)
                    continue;
                
                RegionStat landRegionStat = RegionArray.GetRegionStat(regionInfo.RegionId);
                foreach (RegionPath landRegionPath in landRegionStat.ReachableRegions)
                {
                    if (connectedSeas.Contains(landRegionPath.SeaRegionId) && !connectedLands.Contains(landRegionStat.RegionId))
                        connectedLands.Add(landRegionStat.RegionId);
                }
            }
            
            foreach (int landRegionId in connectedLands)
            {
                if (landRegionId == location.RegionId)
                    continue;

                //TODO maybe there is no harbor but we can send ship for the builder
                FirmHarbor harbor = Nation.FindHarbor(landRegionId, location.RegionId);
                if (harbor != null)
                {
                    builderId = FindBuilderInRegion(landRegionId, harbor.LocCenterX, harbor.LocCenterY);
                    if (builderId != 0)
                        break;
                }
            }
        }
        return builderId;
    }

    private int FindBuilderInRegion(int regionId, int targetLocX, int targetLocY)
    {
        int builderId = 0;
        
        int minFirmDistance = Int16.MaxValue;
        Firm bestFirm = null;
        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId != Nation.nation_recno)
                continue;
            
            if (firm.UnderConstruction)
                continue;

            if (firm.RegionId != regionId)
                continue;

            // TODO use pref
            if (firm.BuilderId != 0 && firm.HitPoints > firm.MaxHitPoints / 2.0 && !Nation.IsUnitOnTask(firm.BuilderId))
            {
                int firmDistance = Misc.points_distance(firm.LocX1, firm.LocY1, targetLocX, targetLocY);
                if (firmDistance < minFirmDistance)
                {
                    //TODO prefer firms at the same region
                    minFirmDistance = firmDistance;
                    bestFirm = firm;
                }
            }
        }

        int minTownDistance = Int16.MaxValue;
        Town bestTown = null;
        int bestRace = 0;
        foreach (Town town in TownArray)
        {
            if (town.NationId != Nation.nation_recno)
                continue;

            if (town.RegionId != regionId)
                continue;

            // TODO do not train if population is low
            if (town.Population <= 10)
                continue;

            // TODO check all races, select race better
            int race = town.PickRandomRace(false, false);
            if (race == 0 || !town.CanTrain(race))
                continue;

            int townDistance = Misc.points_distance(town.LocX1, town.LocY1, targetLocX, targetLocY);
            if (townDistance < minTownDistance)
            {
                //TODO prefer towns at the same region
                minTownDistance = townDistance;
                bestTown = town;
                bestRace = race;
            }
        }

        if (minFirmDistance <= minTownDistance + 20)
        {
            bestTown = null;
        }
        else
        {
            bestFirm = null;
        }

        if (bestFirm != null)
        {
            builderId = bestFirm.BuilderId;
            if (!bestFirm.MobilizeBuilder(builderId))
                builderId = 0;
        }

        if (bestTown != null)
        {
            builderId = bestTown.Recruit(Skill.SKILL_CONSTRUCTION, bestRace, InternalConstants.COMMAND_AI);
        }

        return builderId;
    }

    protected bool CanBuildFirmNearTown(int firmType, Town town)
    {
        FirmInfo firmInfo = FirmRes[firmType];
        int firmWidth = firmInfo.LocWidth;
        int firmHeight = firmInfo.LocHeight;
        
        for (int buildLocY = town.LocY1 - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE - firmHeight;
             buildLocY < town.LocY2 + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE + firmHeight;
             buildLocY++)
        {
            for (int buildLocX = town.LocX1 - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE - firmWidth;
                 buildLocX < town.LocX2 + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE + firmWidth;
                 buildLocX++)
            {
                if (World.CanBuildFirm(buildLocX, buildLocY, firmType) == 0)
                    continue;

                int buildLocX2 = buildLocX + firmWidth - 1;
                int buildLocY2 = buildLocY + firmHeight - 1;
                if (Misc.AreTownAndFirmLinked(buildLocX, buildLocY, buildLocX2, buildLocY2,
                        town.LocX1, town.LocY1, town.LocX2, town.LocY2))
                    return true;
            }
        }

        return false;
    }

    protected int CountBlockedNearLocations(int locX1, int locY1, int locX2, int locY2)
    {
        int blockedLocations = 0;
        
        for (int locX = locX1 - 1; locX <= locX2 + 1; locX++)
        {
            if (Misc.IsLocationValid(locX, locY1 - 1))
            {
                Location location = World.GetLoc(locX, locY1 - 1);
                if (!location.Walkable())
                    blockedLocations++;
            }
            
            if (Misc.IsLocationValid(locX, locY2 + 1))
            {
                Location location = World.GetLoc(locX, locY2 + 1);
                if (!location.Walkable())
                    blockedLocations++;
            }
        }

        for (int locY = locY1; locY <= locY2; locY++)
        {
            if (Misc.IsLocationValid(locX1 - 1, locY))
            {
                Location location = World.GetLoc(locX1 - 1, locY);
                if (!location.Walkable())
                    blockedLocations++;
            }
            
            if (Misc.IsLocationValid(locX2 + 1, locY))
            {
                Location location = World.GetLoc(locX2 + 1, locY);
                if (!location.Walkable())
                    blockedLocations++;
            }
        }

        return blockedLocations;
    }
}
