using System;
using System.Collections.Generic;

namespace TenKingdoms;

// Build inn when
//  1. Kingdom doesn't have any inn
//  2. Kingdom has inns already but needs more depending on kingdom power, available money and preferences - TODO

// Do not build inn when
//  1. Not enough money

public class BuildInnTask : AITask, IUnitTask
{
    private int _builderId;
    private bool _builderSent;
    private bool _noPlaceToBuild;
    private bool _shouldCancel;
    
    public int TownId { get; }
    public int UnitId => _builderId;
    
    public BuildInnTask(Nation nation, int townId) : base(nation)
    {
        TownId = townId;
    }

    public override bool ShouldCancel()
    {
        if (_shouldCancel)
            return true;
        
        if (_noPlaceToBuild)
            return true;

        if (TownIsDeletedOrChangedNation(TownId))
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
            _builderId = FindBuilder(town.LocCenterX, town.LocCenterY, town.RegionId);

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
                builder.BuildFirm(buildLocX, buildLocY, Firm.FIRM_INN, InternalConstants.COMMAND_AI);
                _builderSent = true;
            }
            else
            {
                _noPlaceToBuild = true;
            }
        }
        else
        {
            //TODO check that builder is on the way, not stuck and is able to build factory
            if (builder.IsAIAllStop())
                _builderSent = false;
        }
    }

    private (int, int) FindBestBuildLocation(Town town)
    {
        FirmInfo innInfo = FirmRes[Firm.FIRM_INN];
        int innWidth = innInfo.LocWidth;
        int innHeight = innInfo.LocHeight;
        int townWidth = town.LocX2 - town.LocX1 + 1;
        int townHeight = town.LocY2 - town.LocY1 + 1;
        List<(int, int)> bestBuildLocations = new List<(int, int)>();
        int bestRating = Int16.MinValue;
        
        for (int locY = town.LocY1 - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE - townHeight;
             locY < town.LocY2 + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE + townHeight;
             locY++)
        {
            for (int locX = town.LocX1 - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE - townWidth;
                 locX < town.LocX2 + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE + townWidth;
                 locX++)
            {
                if (World.CanBuildFirm(locX, locY, Firm.FIRM_INN, _builderId) == 0)
                    continue;

                int locX2 = locX + innWidth - 1;
                int locY2 = locY + innHeight - 1;
                if (Misc.AreTownAndFirmLinked(town.LocX1, town.LocY1, town.LocX2, town.LocY2,
                        locX, locY, locX2, locY2))
                {
                    //TODO check also distance from other towns and buildings
                    int rating = Misc.RectsDistance(locX, locY, locX2, locY2,
                        town.LocX1, town.LocY1, town.LocX2, town.LocY2);
                    rating -= CountBlockedNearLocations(locX, locY, locX2, locY2);
                        
                    if (rating > bestRating)
                    {
                        bestBuildLocations.Clear();
                        bestRating = rating;
                    }
                        
                    if (rating == bestRating)
                        bestBuildLocations.Add((locX, locY));
                }
            }
        }
        
        return bestBuildLocations.Count > 0 ? bestBuildLocations[Misc.Random(bestBuildLocations.Count)] : (-1, -1);
    }
}