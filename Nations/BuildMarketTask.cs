using System;
using System.Collections.Generic;

namespace TenKingdoms;

// Build market when
//  1. Near a mine if its stock is high
//  2. Near a kingdom village if there is a product to import
//  3. Near an independent village if there is a product to export - TODO
//  4. Near a harbor if kingdom wants to import or export products - TODO

public class BuildMarketTask : AITask, IUnitTask
{
    private int _builderId;
    private bool _builderSent;
    private bool _noPlaceToBuild;
    private bool _shouldCancel;
    public int FirmId { get; }
    public int TownId { get; }
    public int UnitId => _builderId;

    public BuildMarketTask(Nation nation, int firmId, int townId) : base(nation)
    {
        FirmId = firmId;
        TownId = townId;
    }

    public override bool ShouldCancel()
    {
        if (_shouldCancel)
            return true;
        
        if (_noPlaceToBuild)
            return true;

        if (FirmId != 0)
        {
            if (FirmArray.IsDeleted(FirmId))
                return true;

            Firm firm = FirmArray[FirmId];
            if (firm.NationId != Nation.nation_recno)
                return true;
        }

        if (TownId != 0)
        {
            if (TownArray.IsDeleted(TownId))
                return true;

            Town town = TownArray[TownId];
            if (town.NationId != Nation.nation_recno)
                return true;
        }

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
        Firm firm = FirmId != 0 ? FirmArray[FirmId] : null;
        Town town = TownId != 0 ? TownArray[TownId] : null;

        if (_builderId != 0 && UnitArray.IsDeleted(_builderId))
            _builderId = 0;

        if (_builderId == 0)
        {
            if (firm != null)
                _builderId = FindBuilder(firm.LocCenterX, firm.LocCenterY);

            if (town != null)
                _builderId = FindBuilder(town.LocCenterX, town.LocCenterY);
        }

        if (_builderId == 0)
            return;
        
        Unit builder = UnitArray[_builderId];
        if (builder.UnitMode == UnitConstants.UNIT_MODE_UNDER_TRAINING)
            return;

        if (builder.UnitMode == UnitConstants.UNIT_MODE_CONSTRUCT)
        {
            int restockType = FirmMarket.RESTOCK_ANY;
            if (FirmId != 0)
                restockType = FirmMarket.RESTOCK_RAW;
            if (TownId != 0)
                restockType = FirmMarket.RESTOCK_PRODUCT;
            Nation.AddChangeMarketRestockTask(builder.UnitModeParam, restockType);
            _shouldCancel = true;
            return;
        }

        if (!_builderSent)
        {
            int buildLocX = -1;
            int buildLocY = -1;

            if (firm != null)
                (buildLocX, buildLocY) = FindBestMineBuildLocation(firm);

            if (town != null)
                (buildLocX, buildLocY) = FindBestTownBuildLocation(town);

            if (buildLocX != -1 && buildLocY != -1)
            {
                builder.BuildFirm(buildLocX, buildLocY, Firm.FIRM_MARKET, InternalConstants.COMMAND_AI);
                _builderSent = true;
            }
            else
            {
                _noPlaceToBuild = true;
            }
        }
        else
        {
            //TODO check that builder is on the way, not stuck and is able to build market
            if (builder.IsAIAllStop())
                _builderSent = false;
        }
    }
    
    private (int, int) FindBestMineBuildLocation(Firm firm)
    {
        FirmInfo marketInfo = FirmRes[Firm.FIRM_MARKET];
        int marketWidth = marketInfo.LocWidth;
        int marketHeight = marketInfo.LocHeight;
        List<(int, int)> bestBuildLocations = new List<(int, int)>();
        int minRating = Int16.MaxValue;
        for (int locY = firm.LocY1 - InternalConstants.EFFECTIVE_FIRM_FIRM_DISTANCE - marketHeight;
             locY < firm.LocY2 + InternalConstants.EFFECTIVE_FIRM_FIRM_DISTANCE + marketHeight;
             locY++)
        {
            for (int locX = firm.LocX1 - InternalConstants.EFFECTIVE_FIRM_FIRM_DISTANCE - marketWidth;
                 locX < firm.LocX2 + InternalConstants.EFFECTIVE_FIRM_FIRM_DISTANCE + marketWidth;
                 locX++)
            {
                if (World.CanBuildFirm(locX, locY, Firm.FIRM_MARKET, _builderId) == 0)
                    continue;

                int locX2 = locX + marketWidth - 1;
                int locY2 = locY + marketHeight - 1;
                if (Misc.AreFirmsLinked(firm.LocX1, firm.LocY1, firm.LocX2, firm.LocY2,
                        locX, locY, locX2, locY2))
                {
                    int rating = Misc.RectsDistance(firm.LocX1, firm.LocY1, firm.LocX2, firm.LocY2,
                        locX, locY, locX2, locY2);

                    foreach (Town town in TownArray)
                    {
                        if (Misc.AreTownAndFirmLinked(town.LocX1, town.LocY1, town.LocX2, town.LocY2,
                                locX, locY, locX2, locY2))
                        {
                            rating += Misc.RectsDistance(locX, locY, locX2, locY2,
                                town.LocX1, town.LocY1, town.LocX2, town.LocY2);
                        }
                    }
                    
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

        return bestBuildLocations.Count > 0 ? bestBuildLocations[Misc.Random(bestBuildLocations.Count)] : (-1, -1);
    }

    private (int, int) FindBestTownBuildLocation(Town town)
    {
        FirmInfo marketInfo = FirmRes[Firm.FIRM_MARKET];
        int marketWidth = marketInfo.LocWidth;
        int marketHeight = marketInfo.LocHeight;
        List<(int, int)> bestBuildLocations = new List<(int, int)>();
        int maxRating = Int16.MinValue;
        for (int locY = town.LocY1 - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE - marketHeight;
             locY < town.LocY2 + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE + marketHeight;
             locY++)
        {
            for (int locX = town.LocX1 - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE - marketWidth;
                 locX < town.LocX2 + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE + marketWidth;
                 locX++)
            {
                if (World.CanBuildFirm(locX, locY, Firm.FIRM_MARKET, _builderId) == 0)
                    continue;

                int locX2 = locX + marketWidth - 1;
                int locY2 = locY + marketHeight - 1;
                if (Misc.AreTownAndFirmLinked(town.LocX1, town.LocY1, town.LocX2, town.LocY2,
                        locX, locY, locX2, locY2))
                {
                    int rating = InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE - Misc.RectsDistance(locX, locY, locX2, locY2,
                        town.LocX1, town.LocY1, town.LocX2, town.LocY2);

                    foreach (Town otherTown in TownArray)
                    {
                        if (otherTown == town)
                            continue;
                        
                        if (Misc.AreTownAndFirmLinked(otherTown.LocX1, otherTown.LocY1, otherTown.LocX2, otherTown.LocY2,
                                locX, locY, locX2, locY2))
                        {
                            rating += InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE - Misc.RectsDistance(locX, locY, locX2, locY2,
                                otherTown.LocX1, otherTown.LocY1, otherTown.LocX2, otherTown.LocY2);
                        }
                    }

                    foreach (Firm firm in FirmArray)
                    {
                        if (firm.NationId != Nation.nation_recno)
                            continue;

                        if (firm.FirmType != Firm.FIRM_FACTORY && firm.FirmType != Firm.FIRM_HARBOR)
                            continue;

                        if (Misc.AreFirmsLinked(locX, locY, locX2, locY2,
                                firm.LocX1, firm.LocY1, firm.LocX2, firm.LocY2))
                        {
                            rating += InternalConstants.EFFECTIVE_FIRM_FIRM_DISTANCE - Misc.RectsDistance(locX, locY, locX2, locY2,
                                firm.LocX1, firm.LocY1, firm.LocX2, firm.LocY2);
                        }
                    }

                    rating -= CountBlockedNearLocations(locX, locY, locX2, locY2);

                    if (rating > maxRating)
                    {
                        bestBuildLocations.Clear();
                        maxRating = rating;
                    }
                    
                    if (rating == maxRating)
                        bestBuildLocations.Add((locX, locY));
                }
            }
        }

        return bestBuildLocations.Count > 0 ? bestBuildLocations[Misc.Random(bestBuildLocations.Count)] : (-1, -1);
    }
}