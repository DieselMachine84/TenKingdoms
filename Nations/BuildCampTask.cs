using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class BuildCampTask : AITask
{
    private int _builderId;
    private bool _builderSent;
    private bool _noPlaceToBuild;
    
    public int TownId { get; }
    
    public BuildCampTask(NationBase nation, int townId) : base(nation)
    {
        TownId = townId;
    }

    public override bool ShouldCancel()
    {
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
        
        // TODO king should build camp if there are no builders available
        
        Unit builder = UnitArray[_builderId];
        if (builder.UnitMode == UnitConstants.UNIT_MODE_UNDER_TRAINING)
            return;

        if (!_builderSent)
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
            //TODO check that builder is on the way, not stuck and is able to build mine
            if (builder.IsAIAllStop())
                _builderSent = false;
        }
    }

    private (int, int) FindBestBuildLocation(int townLocX1, int townLocY1, int townLocX2, int townLocY2)
    {
        Location townLocation = World.GetLoc(townLocX1, townLocY1);
        int maxRating = Int32.MinValue / 2;
        List<(int, int)> maxRatingLocations = new List<(int, int)>();

        List<Town> nearTowns = new List<Town>();
        foreach (Town town in TownArray)
        {
            Location otherTownLocation = World.GetLoc(town.LocX1, town.LocY1);
            if (otherTownLocation.IsPlateau() != townLocation.IsPlateau() || otherTownLocation.RegionId != townLocation.RegionId)
                continue;
            
            if (Misc.RectsDistance(town.LocX1, town.LocY1, town.LocX2, town.LocY2,
                    townLocX1, townLocY1, townLocX2, townLocY2) <= InternalConstants.EFFECTIVE_TOWN_TOWN_DISTANCE * 3)
            {
                nearTowns.Add(town);
            }
        }
        
        FirmInfo firmInfo = FirmRes[Firm.FIRM_CAMP];
        FirmBuild firmBuild = FirmRes.get_build(firmInfo.first_build_id);
        int campWidth = firmBuild.loc_width;
        int campHeight = firmBuild.loc_height;

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

                if (!Misc.IsLocationValid(buildLocX, buildLocY) || !Misc.IsLocationValid(buildLocX2, buildLocY2))
                    continue;

                if (!Misc.AreTownAndFirmLinked(buildLocX, buildLocY, buildLocX2, buildLocY2, townLocX1, townLocY1, townLocX2, townLocY2))
                    continue;
                
                Location buildLocation = World.GetLoc(buildLocX, buildLocY);
                if (buildLocation.RegionId != townLocation.RegionId || buildLocation.IsPlateau() != townLocation.IsPlateau())
                    continue;
                
                if (World.CanBuildFirm(buildLocX, buildLocY, Firm.FIRM_CAMP, _builderId) == 0)
                    continue;

                int rating = 0;
                foreach (Town nearTown in nearTowns)
                {
                    if (!Misc.AreTownAndFirmLinked(buildLocX, buildLocY, buildLocX2, buildLocY2,
                            nearTown.LocX1, nearTown.LocY1, nearTown.LocX2, nearTown.LocY2))
                        continue;
                    
                    if (nearTown.NationId == Nation.nation_recno)
                        rating += 100;
                    
                    // TODO calculate rating for other nation towns
                }

                foreach (Location nearLocation in Misc.EnumerateNearLocations(buildLocX, buildLocY, buildLocX2, buildLocY2))
                {
                    if (!nearLocation.Walkable())
                        rating -= 10;
                }

                rating -= Misc.RectsDistance(buildLocX, buildLocY, buildLocX2, buildLocY2,
                    townLocX1, townLocY1, townLocX2, townLocY2) * 5;
                
                if (rating > maxRating)
                {
                    maxRating = rating;
                    maxRatingLocations.Clear();
                }
                
                if (rating == maxRating)
                    maxRatingLocations.Add((buildLocX, buildLocY));
            }
        }

        if (maxRatingLocations.Count > 0)
        {
            return maxRatingLocations[Misc.Random(maxRatingLocations.Count)];
        }

        return (-1, -1);
    }
}