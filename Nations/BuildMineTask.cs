using System;
using System.Collections.Generic;

namespace TenKingdoms;

// Build mine when
//  1. Kingdom has no mines
//  2. Kingdom has enough money and population to support more than one mine - TODO
//  3. Kingdom has harbor if resource is on another island

// Do not build mine when
//  1. Not enough money - TODO
//  2. Not enough population - TODO
//  3. Resource is near the enemy - TODO

public class BuildMineTask : AITask, IUnitTask
{
    private int _builderId;
    private bool _builderSent;
    private bool _noPlaceToBuild;
    private bool _shouldCancel;
    public int SiteId { get; }
    public int UnitId => _builderId;

    public BuildMineTask(Nation nation, int siteId) : base(nation)
    {
        SiteId = siteId;
    }

    public override bool ShouldCancel()
    {
        if (_shouldCancel)
            return true;
        
        if (_noPlaceToBuild)
            return true;
        
        if (SiteArray.IsDeleted(SiteId))
            return true;

        Site site = SiteArray[SiteId];
        if (site.HasMine)
            return true;

        return false;
    }

    public override void Cancel()
    {
        if (_builderId != 0 && !UnitArray.IsDeleted(_builderId))
        {
            Unit builder = UnitArray[_builderId];
            if (builder.IsVisible())
                builder.Stop2();
        }
    }
    
    public override void Process()
    {
        Site site = SiteArray[SiteId];
        
        if (_builderId != 0 && UnitArray.IsDeleted(_builderId))
            _builderId = 0;

        if (_builderId == 0)
            _builderId = FindBuilder(site.LocX, site.LocY, site.RegionId, true);

        if (_builderId == 0)
            return;

        Unit builder = UnitArray[_builderId];
        if (builder.UnitMode == UnitConstants.UNIT_MODE_UNDER_TRAINING)
            return;

        if (builder.UnitMode == UnitConstants.UNIT_MODE_CONSTRUCT)
        {
            _shouldCancel = true;
            return;
        }

        if (builder.UnitMode == UnitConstants.UNIT_MODE_ON_SHIP)
        {
            Nation.AddSailShipTask((UnitMarine)UnitArray[builder.UnitModeParam], site.LocX, site.LocY);
            return;
        }

        if (!_builderSent)
        {
            if (builder.RegionId() == site.RegionId)
            {
                (int buildLocX, int buildLocY) = FindBestBuildLocation(site.LocX, site.LocY);
                if (buildLocX != -1 && buildLocY != -1)
                {
                    builder.BuildFirm(buildLocX, buildLocY, Firm.FIRM_MINE, InternalConstants.COMMAND_AI);
                    _builderSent = true;
                }
                else
                {
                    _noPlaceToBuild = true;
                }
            }
            else
            {
                UnitMarine transport = Nation.FindTransportShip(Nation.GetSeaRegion(builder.RegionId(), site.RegionId));
                if (transport != null)
                {
                    List<int> units = new List<int> { _builderId };
                    UnitArray.AssignToShip(transport.NextLocX, transport.NextLocY, false, units, InternalConstants.COMMAND_AI, transport.SpriteId);
                    _builderSent = true;
                }
                else
                {
                    FirmHarbor harbor = Nation.FindHarbor(builder.RegionId(), site.RegionId);
                    Nation.AddBuildShipTask(harbor, UnitConstants.UNIT_TRANSPORT);
                }
            }
        }
        else
        {
            //TODO check that builder is on the way, not stuck and is able to build mine
            if (builder.IsAIAllStop())
                _builderSent = false;
        }
    }

    private (int, int) FindBestBuildLocation(int siteLocX, int siteLocY)
    {
        Location siteLocation = World.GetLoc(siteLocX, siteLocY);
        
        Town nearestTown = null;
        int minDistance = Int16.MaxValue;
        foreach (Town town in TownArray)
        {
            if (town.NationId != Nation.nation_recno)
                continue;

            if (town.RegionId != siteLocation.RegionId)
                continue;
            
            if (World.GetLoc(town.LocX1, town.LocY1).IsPlateau() != siteLocation.IsPlateau())
                continue;

            int distance = Misc.RectsDistance(siteLocX, siteLocY, siteLocX, siteLocY,
                town.LocX1, town.LocY1, town.LocX2, town.LocY2);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestTown = town;
            }
        }
        
        FirmInfo firmInfo = FirmRes[Firm.FIRM_MINE];
        int mineWidth = firmInfo.LocWidth;
        int mineHeight = firmInfo.LocHeight;

        int buildLocX = -1;
        int buildLocY = -1;
        minDistance = Int16.MaxValue;
        for (int locY = siteLocY - mineWidth + 1; locY < siteLocY; locY++)
        {
            for (int locX = siteLocX - mineHeight + 1; locX < siteLocX; locX++)
            {
                if (World.CanBuildFirm(locX, locY, Firm.FIRM_MINE, _builderId) == 0)
                    continue;

                if (nearestTown != null)
                {
                    int distance = Misc.RectsDistance(locX, locY, locX + mineWidth - 1, locY + mineHeight - 1,
                        nearestTown.LocX1, nearestTown.LocY1, nearestTown.LocX2, nearestTown.LocY2);
                    if (distance < minDistance)
                    {
                        buildLocX = locX;
                        buildLocY = locY;
                        minDistance = distance;
                    }
                }
                else
                {
                    buildLocX = locX;
                    buildLocY = locY;
                }
            }
        }

        // TODO take other nation towns into account
        return (buildLocX, buildLocY);
    }
}
