using System;
using System.Collections.Generic;

namespace TenKingdoms;

// Build a factory when there is a mine or a market that has a resource and there is no such factory
// Build a factory when existing factories are not able to manufacture available resources
// Build a factory when another factory is too far from mine/market
// Build a factory when kingdom can buy resource from another kingdom
// Choose a place to build near a village with enough population and not far from mine/market 

public class BuildFactoryTask : AITask, IUnitTask
{
    private int _builderId;
    private bool _builderSent;
    private bool _noPlaceToBuild;
    private bool _shouldCancel;
    public int FirmId { get; }
    public int ProductId { get; }
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
            _builderId = FindBuilder(firm.LocCenterX, firm.LocCenterY);

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
            if (builder.RegionId() == firm.RegionId)
            {
                (int buildLocX, int buildLocY) = FindBestBuildLocation(firm.LocX1, firm.LocY1, firm.LocX2, firm.LocY2);
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
                //TODO other region
            }
        }
        else
        {
            //TODO check that builder is on the way, not stuck and is able to build factory
            if (builder.IsAIAllStop())
                _builderSent = false;
        }
    }

    private (int, int) FindBestBuildLocation(int rawFirmLocX1, int rawFirmLocY1, int rawFirmLocX2, int rawFirmLocY2)
    {
        Location rawFirmLocation = World.GetLoc(rawFirmLocX1, rawFirmLocY1);
        
        Town bestTown = null;
        int bestRating = -1;
        
        foreach (Town town in TownArray)
        {
            if (town.NationId != Nation.nation_recno)
                continue;

            if (town.RegionId != rawFirmLocation.RegionId)
                continue;

            if (!CanBuildFirmNearTown(Firm.FIRM_FACTORY, town))
                continue;
            
            int distance = Misc.RectsDistance(rawFirmLocX1, rawFirmLocY1, rawFirmLocX2, rawFirmLocY2,
                town.LocX1, town.LocY1, town.LocX2, town.LocY2);
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
                            rawFirmLocX1, rawFirmLocY1, rawFirmLocX2, rawFirmLocY2);
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