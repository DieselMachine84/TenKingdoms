using System;

namespace TenKingdoms;

// Build harbor when
//  1. There is a resource on another island and we need it
//  2. There is an independent village on another island that can be captured - TODO
//  3. There is another kingdom on another island - TODO

public class BuildHarborTask : AITask, IUnitTask
{
    private int _builderId;
    private bool _builderSent;
    private bool _noPlaceToBuild;
    private bool _shouldCancel;
    private int TownId { get; }
    private int SeaRegionId { get; }
    public int UnitId => _builderId;
    
    public BuildHarborTask(Nation nation, int townId, int seaRegionId) : base(nation)
    {
        TownId = townId;
        SeaRegionId = seaRegionId;
    }

    public override bool ShouldCancel()
    {
        if (_shouldCancel)
            return true;
        
        if (_noPlaceToBuild)
            return true;

        if (TownArray.IsDeleted(TownId))
            return true;

        Town town = TownArray[TownId];
        if (town.NationId != Nation.nation_recno)
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
        Town town = TownArray[TownId];
        
        if (_builderId != 0 && UnitArray.IsDeleted(_builderId))
            _builderId = 0;
        
        if (_builderId == 0)
            _builderId = FindBuilder(town.LocCenterX, town.LocCenterY);

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

        if (!_builderSent)
        {
            (int buildLocX, int buildLocY) = FindBestBuildLocation(town);
            if (buildLocX != -1 && buildLocY != -1)
            {
                builder.BuildFirm(buildLocX, buildLocY, Firm.FIRM_HARBOR, InternalConstants.COMMAND_AI);
                _builderSent = true;
            }
            else
            {
                _noPlaceToBuild = true;
            }
        }
        else
        {
            //TODO check that builder is on the way, not stuck and is able to build harbor
            if (builder.IsAIAllStop())
                _builderSent = false;
        }
    }

    private (int, int) FindBestBuildLocation(Town town)
    {
        int bestLocX = -1;
        int bestLocY = -1;
        int bestRating = Int16.MaxValue;

        for (int locY = 0; locY < GameConstants.MapSize; locY++)
        {
            for (int locX = 0; locX < GameConstants.MapSize; locX++)
            {
                Location location = World.GetLoc(locX, locY);
                if (!location.CanBuildWholeHarbor())
                    continue;

                bool correctHarborRegion = true;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (locX + i >= GameConstants.MapSize || locY + j >= GameConstants.MapSize)
                            correctHarborRegion = false;

                        int harborRegionId = World.GetLoc(locX + i, locY + j).RegionId;
                        if (harborRegionId != town.RegionId && harborRegionId != SeaRegionId)
                            correctHarborRegion = false;
                    }
                }

                if (!correctHarborRegion)
                    continue;

                if (World.CanBuildFirm(locX, locY, Firm.FIRM_HARBOR, _builderId) == 0)
                    continue;

                int rating = Misc.RectsDistance(town.LocX1, town.LocY1, town.LocX2, town.LocY2,
                    locX, locY, locX, locY);
                if (rating < bestRating)
                {
                    bestLocX = locX;
                    bestLocY = locY;
                    bestRating = rating;
                }
            }
        }

        return (bestLocX, bestLocY);
    }
}