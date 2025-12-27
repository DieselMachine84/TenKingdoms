using System;
using System.Collections.Generic;

namespace TenKingdoms;

// Build factory when
//  1. There is a mine or a market that has a resource and there is no such factory nearby
//  2. There is a mine or a market that has a resource but existing factories are not able to manufacture it - TODO
//  3. kingdom can buy resource from another kingdom - TODO
//  4. There is a mine or a market on an island and there is no place on the island to build factory, build it on another island - TODO

// Do not build factory when
//  1. Not enough money - TODO
//  2. Not enough population - TODO

// Choose a place to build near a village with enough population and not far from mine/market 

public class BuildFactoryTask : AITask, IUnitTask
{
    private int _builderId;
    private bool _builderSent;
    private bool _noPlaceToBuild;
    private bool _shouldCancel;
    public int FirmId { get; }
    private int ProductId { get; }
    public int UnitId => _builderId;

    public BuildFactoryTask(Nation nation, int firmId, int productId) : base(nation)
    {
        FirmId = firmId;
        ProductId = productId;
    }

    public override bool ShouldCancel()
    {
        if (_shouldCancel)
            return true;
        
        if (_noPlaceToBuild)
            return true;

        if (FirmArray.IsDeleted(FirmId))
            return true;
        
        Firm firm = FirmArray[FirmId];
        if (firm.NationId != Nation.nation_recno)
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
        Firm firm = FirmArray[FirmId];
        
        if (_builderId != 0 && UnitArray.IsDeleted(_builderId))
            _builderId = 0;

        if (_builderId == 0)
            _builderId = FindBuilder(firm.LocCenterX, firm.LocCenterY, firm.RegionId);

        if (_builderId == 0)
            return;
        
        Unit builder = UnitArray[_builderId];
        if (builder.UnitMode == UnitConstants.UNIT_MODE_UNDER_TRAINING)
            return;

        if (builder.UnitMode == UnitConstants.UNIT_MODE_CONSTRUCT)
        {
            Nation.AddChangeFactoryProductionTask(builder.UnitModeParam, ProductId);
            _shouldCancel = true;
            return;
        }

        if (!_builderSent)
        {
            (int buildLocX, int buildLocY) = FindBestBuildLocation(firm);
            if (buildLocX != -1 && buildLocY != -1)
            {
                builder.BuildFirm(buildLocX, buildLocY, Firm.FIRM_FACTORY, InternalConstants.COMMAND_AI);
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

    private (int, int) FindBestBuildLocation(Firm rawFirm)
    {
        Location rawFirmLocation = World.GetLoc(rawFirm.LocCenterX, rawFirm.LocCenterY);
        
        Town bestTown = null;
        int bestRating = Int16.MinValue;
        
        foreach (Town town in TownArray)
        {
            if (town.NationId != Nation.nation_recno)
                continue;

            if (town.RegionId != rawFirmLocation.RegionId)
                continue;

            if (!CanBuildFirmNearTown(Firm.FIRM_FACTORY, town))
                continue;
            
            int distance = Misc.FirmTownDistance(rawFirm, town);
            int rating = town.JoblessPopulation * 2 + (GameConstants.MapSize - distance);
            if (rating > bestRating)
            {
                bestTown = town;
                bestRating = rating;
            }
        }

        FirmInfo factoryInfo = FirmRes[Firm.FIRM_FACTORY];
        int factoryWidth = factoryInfo.LocWidth;
        int factoryHeight = factoryInfo.LocHeight;
        List<(int, int)> bestBuildLocations = new List<(int, int)>();
        int minRating = Int16.MaxValue;
        if (bestTown != null)
        {
            for (int locY = bestTown.LocY1 - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE - factoryHeight;
                 locY < bestTown.LocY2 + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE + factoryHeight;
                 locY++)
            {
                for (int locX = bestTown.LocX1 - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE - factoryWidth;
                     locX < bestTown.LocX2 + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE + factoryWidth;
                     locX++)
                {
                    if (World.CanBuildFirm(locX, locY, Firm.FIRM_FACTORY, _builderId) == 0)
                        continue;

                    int locX2 = locX + factoryWidth - 1;
                    int locY2 = locY + factoryHeight - 1;
                    if (Misc.AreTownAndFirmLinked(bestTown.LocX1, bestTown.LocY1, bestTown.LocX2, bestTown.LocY2,
                            locX, locY, locX2, locY2))
                    {
                        int rating = Misc.RectsDistance(locX, locY, locX2, locY2,
                            bestTown.LocX1, bestTown.LocY1, bestTown.LocX2, bestTown.LocY2);
                        rating += Misc.RectsDistance(locX, locY, locX2, locY2,
                            rawFirm.LocX1, rawFirm.LocY1, rawFirm.LocX2, rawFirm.LocY2);
                        rating += CountBlockedNearLocations(locX, locY, locX2, locY2);
                        
                        if (rating < minRating)
                        {
                            bestBuildLocations.Clear();
                            minRating = rating;
                        }
                        
                        if (rating == minRating)
                            bestBuildLocations.Add((locX, locY));
                    }
                }
            }
        }

        return bestBuildLocations.Count > 0 ? bestBuildLocations[Misc.Random(bestBuildLocations.Count)] : (-1, -1);
    }
}