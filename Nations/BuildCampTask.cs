using System;
using System.Collections.Generic;

namespace TenKingdoms;

// Build camp when
//  1. There is no camp near kingdom village
//  2. - TODO

public class BuildCampTask : AITask, IUnitTask
{
    private int _builderId;
    private bool _builderSent;
    private bool _noPlaceToBuild;
    private bool _shouldCancel;
    public int TownId { get; }
    public int UnitId => _builderId;
    
    public BuildCampTask(Nation nation, int townId) : base(nation)
    {
        TownId = townId;
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

        if (town.HasLinkedCamp(Nation.nation_recno, false))
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
            _builderId = FindBuilder(town.LocCenterX, town.LocCenterY, town.RegionId, true);

        if (_builderId == 0)
            return;

        //TODO search for soldiers and generals also
        //TODO king should build camp if there are no builders available
        
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
            Nation.AddSailShipTask((UnitMarine)UnitArray[builder.UnitModeParam], town.LocCenterX, town.LocCenterY);
            return;
        }
        
        if (!_builderSent)
        {
            if (builder.RegionId() == town.RegionId)
            {
                (int buildLocX, int buildLocY) = FindBestBuildLocation(town.LocX1, town.LocY1, town.LocX2, town.LocY2);
                if (buildLocX != -1 && buildLocY != -1)
                {
                    builder.BuildFirm(buildLocX, buildLocY, Firm.FIRM_CAMP, InternalConstants.COMMAND_AI);
                    _builderSent = true;
                }
                else
                {
                    _noPlaceToBuild = true;
                }
            }
            else
            {
                UnitMarine transport = Nation.FindTransportShip(Nation.GetSeaRegion(builder.RegionId(), town.RegionId));
                if (transport != null)
                {
                    List<int> units = new List<int> { _builderId };
                    UnitArray.AssignToShip(transport.NextLocX, transport.NextLocY, false, units, InternalConstants.COMMAND_AI, transport.SpriteId);
                    _builderSent = true;
                }
                else
                {
                    FirmHarbor harbor = Nation.FindHarbor(builder.RegionId(), town.RegionId);
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

    private (int, int) FindBestBuildLocation(int townLocX1, int townLocY1, int townLocX2, int townLocY2)
    {
        Location townLocation = World.GetLoc(townLocX1, townLocY1);
        int maxRating = Int16.MinValue;
        List<(int, int)> maxRatingLocations = new List<(int, int)>();

        List<Town> nearTowns = new List<Town>();
        foreach (Town otherTown in TownArray)
        {
            Location otherTownLocation = World.GetLoc(otherTown.LocX1, otherTown.LocY1);
            if (otherTownLocation.IsPlateau() != townLocation.IsPlateau() || otherTownLocation.RegionId != townLocation.RegionId)
                continue;
            
            if (Misc.RectsDistance(otherTown.LocX1, otherTown.LocY1, otherTown.LocX2, otherTown.LocY2,
                    townLocX1, townLocY1, townLocX2, townLocY2) <= InternalConstants.EFFECTIVE_TOWN_TOWN_DISTANCE * 3)
            {
                nearTowns.Add(otherTown);
            }
        }
        
        FirmInfo firmInfo = FirmRes[Firm.FIRM_CAMP];
        int campWidth = firmInfo.LocWidth;
        int campHeight = firmInfo.LocHeight;

        for (int buildLocY = townLocY1 - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE - campHeight;
             buildLocY < townLocY2 + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE + campHeight;
             buildLocY++)
        {
            for (int buildLocX = townLocX1 - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE - campWidth;
                 buildLocX < townLocX2 + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE + campWidth;
                 buildLocX++)
            {
                int buildLocX2 = buildLocX + campWidth - 1;
                int buildLocY2 = buildLocY + campHeight - 1;

                if (World.CanBuildFirm(buildLocX, buildLocY, Firm.FIRM_CAMP, _builderId) == 0)
                    continue;

                if (!Misc.AreTownAndFirmLinked(townLocX1, townLocY1, townLocX2, townLocY2, buildLocX, buildLocY, buildLocX2, buildLocY2))
                    continue;
                
                int rating = 0;
                foreach (Town nearTown in nearTowns)
                {
                    if (!Misc.AreTownAndFirmLinked(nearTown.LocX1, nearTown.LocY1, nearTown.LocX2, nearTown.LocY2,
                            buildLocX, buildLocY, buildLocX2, buildLocY2))
                        continue;
                    
                    if (nearTown.NationId == Nation.nation_recno)
                        rating += 100;
                    
                    if (nearTown.NationId == 0)
                        rating += 100;

                    // TODO calculate rating for other nation towns
                }

                rating -= Misc.RectsDistance(buildLocX, buildLocY, buildLocX2, buildLocY2,
                    townLocX1, townLocY1, townLocX2, townLocY2) * 5;
                rating -= 10 * CountBlockedNearLocations(buildLocX, buildLocY, buildLocX2, buildLocY2);
                
                if (rating > maxRating)
                {
                    maxRating = rating;
                    maxRatingLocations.Clear();
                }
                
                if (rating == maxRating)
                    maxRatingLocations.Add((buildLocX, buildLocY));
            }
        }

        return maxRatingLocations.Count > 0 ? maxRatingLocations[Misc.Random(maxRatingLocations.Count)] : (-1, -1);
    }
}