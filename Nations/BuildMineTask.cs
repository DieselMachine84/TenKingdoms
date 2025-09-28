using System;

namespace TenKingdoms;

public class BuildMineTask : AITask, IUnitTask
{
    private int _builderId;
    private bool _builderSent;
    private bool _noPlaceToBuild;
    public int SiteId { get; }

    public int UnitId => _builderId;

    public BuildMineTask(NationBase nation, int siteId) : base(nation)
    {
        SiteId = siteId;
    }

    public override bool ShouldCancel()
    {
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
            builder.Stop2();
        }
    }
    
    public override void Process()
    {
        Site site = SiteArray[SiteId];
        
        if (_builderId != 0 && UnitArray.IsDeleted(_builderId))
            _builderId = 0;

        if (_builderId == 0)
            _builderId = FindBuilder(site.LocX, site.LocY);

        if (_builderId == 0)
            return;

        Unit builder = UnitArray[_builderId];
        if (builder.UnitMode == UnitConstants.UNIT_MODE_UNDER_TRAINING)
            return;

        if (!_builderSent)
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
            //TODO check that builder is on the way, not stuck and is able to build mine
            if (builder.IsAIAllStop())
                _builderSent = false;
        }
    }

    private (int, int) FindBestBuildLocation(int siteLocX, int siteLocY)
    {
        Location siteLocation = World.GetLoc(siteLocX, siteLocY);
        
        Town nearestTown = null;
        int minDistance = Int32.MaxValue / 2;
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
        FirmBuild firmBuild = FirmRes.get_build(firmInfo.first_build_id);
        int mineWidth = firmBuild.loc_width;
        int mineHeight = firmBuild.loc_height;

        int buildLocX = -1;
        int buildLocY = -1;
        minDistance = Int32.MaxValue / 2;
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
