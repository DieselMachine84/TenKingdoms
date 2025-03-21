using System;

namespace TenKingdoms;

public class BuildCampTask : AITask
{
    private int _generalId;
    private bool _generalSent;
    private bool _noPlaceToBuild;
    
    public int TownId { get; }
    
    public BuildCampTask(NationBase nation, int townId) : base(nation)
    {
        TownId = townId;
    }

    public override bool ShouldCancel()
    {
        if (TownArray.IsDeleted(TownId))
            return true;

        if (_noPlaceToBuild)
            return true;

        Town town = TownArray[TownId];
        if (town.NationId != Nation.nation_recno)
            return true;

        if (town.HasLinkedCamp(Nation.nation_recno, false))
            return true;
        
        return false;
    }

    public override void Process()
    {
        Town town = TownArray[TownId];
        int majorRace = town.MajorityRace();

        // TODO use builder for building camps
        if (_generalId == 0)
        {
            Town sourceTown = null;
            // TODO look for a general in camps, bases inns or train
            foreach (Town otherTown in TownArray)
            {
                if (otherTown.NationId != Nation.nation_recno)
                    continue;

                // TODO other region
                if (otherTown.RegionId != town.RegionId)
                    continue;

                // TODO check for other races too
                // TODO do not train if population is low
                if (otherTown.CanTrain(majorRace))
                {
                    sourceTown = otherTown;
                }
            }

            if (sourceTown != null)
            {
                _generalId = sourceTown.Recruit(Skill.SKILL_LEADING, majorRace, InternalConstants.COMMAND_AI);
            }
        }

        if (_generalId == 0)
            return;

        Unit general = UnitArray[_generalId];
        if (general.unit_mode == UnitConstants.UNIT_MODE_UNDER_TRAINING)
            return;
        
        if (general.rank_id != Unit.RANK_GENERAL || general.rank_id != Unit.RANK_KING)
            general.set_rank(Unit.RANK_GENERAL);

        if (!_generalSent)
        {
            FirmInfo firmInfo = FirmRes[Firm.FIRM_CAMP];
            Location townLocation = World.get_loc(town.LocX1, town.LocY1);
            int minRating = Int32.MaxValue;
            int bestBuildLocX = -1;
            int bestBuildLocY = -1;
            
            // TODO use better bounds
            for (int buildLocX = town.LocX1 - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE - InternalConstants.TOWN_WIDTH;
                 buildLocX < town.LocX2 + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE + InternalConstants.TOWN_WIDTH;
                 buildLocX++)
            {
                if (!Misc.IsLocationValid(buildLocX, 0))
                    continue;

                for (int buildLocY = town.LocY1 - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE - InternalConstants.TOWN_HEIGHT;
                     buildLocY < town.LocY2 + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE + InternalConstants.TOWN_HEIGHT;
                     buildLocY++)
                {
                    if (!Misc.IsLocationValid(0, buildLocY))
                        continue;

                    int buildLocX2 = buildLocX + firmInfo.loc_width - 1;
                    int buildLocY2 = buildLocY + firmInfo.loc_height - 1;
                    if (Misc.rects_distance(buildLocX, buildLocY, buildLocX2, buildLocY2,
                            town.LocX1, town.LocY1, town.LocX2, town.LocY2) > InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE)
                    {
                        continue;
                    }

                    Location buildLocation = World.get_loc(buildLocX, buildLocY);
                    if (buildLocation.region_id != townLocation.region_id || buildLocation.is_plateau() != townLocation.is_plateau())
                        continue;

                    if (World.can_build_firm(buildLocX, buildLocY, Firm.FIRM_CAMP, _generalId) == 0)
                        continue;

                    int rating = Misc.PointsDistance(buildLocX, buildLocY, buildLocX2, buildLocY2,
                        town.LocX1, town.LocY1, town.LocX2, town.LocY2);
                    foreach ((int, int) locXlocY in Misc.EnumerateNearLocations(buildLocX, buildLocY, buildLocX2, buildLocY2, 1))
                    {
                        Location nearLocation = World.get_loc(locXlocY.Item1, locXlocY.Item2);
                        if (nearLocation.is_firm() || nearLocation.is_town())
                            rating++;
                    }
                    
                    // TODO rating should also depend on distance to the enemy and to our other villages

                    if (rating < minRating)
                    {
                        minRating = rating;
                        bestBuildLocX = buildLocX;
                        bestBuildLocY = buildLocY;
                    }
                }
            }

            if (bestBuildLocX != -1 && bestBuildLocY != -1)
            {
                general.build_firm(bestBuildLocX, bestBuildLocY, Firm.FIRM_CAMP, InternalConstants.COMMAND_AI);
                _generalSent = true;
            }
            else
            {
                _noPlaceToBuild = true;
            }
        }
        else
        {
            //TODO check that general is on the way, not stuck and is able to build camp
            if (general.is_ai_all_stop())
                _generalSent = false;
        }
    }
}