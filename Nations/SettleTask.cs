using System;

namespace TenKingdoms;

public class SettleTask : AITask
{
    private int _settlerId;
    private bool _settlerSent;
    private bool _noPlaceToSettle;
    public int FirmId { get; }
    
    public SettleTask(NationBase nation, int firmId) : base(nation)
    {
        FirmId = firmId;
    }

    public override bool ShouldCancel()
    {
        if (FirmArray.IsDeleted(FirmId))
            return true;

        if (_noPlaceToSettle)
            return true;

        Firm firm = FirmArray[FirmId];
        if (firm.nation_recno != Nation.nation_recno)
            return true;

        foreach (int townId in firm.linked_town_array)
        {
            Town town = TownArray[townId];
            if (town.NationId == Nation.nation_recno)
                return true;
        }

        return false;
    }

    public override void Process()
    {
        Firm firm = FirmArray[FirmId];

        if (_settlerId != 0 && UnitArray.IsDeleted(_settlerId))
            _settlerId = 0;

        if (_settlerId == 0)
        {
            int minTownDistance = Int32.MaxValue;
            Town bestTown = null;
            int bestRace = 0;

            foreach (Town town in TownArray)
            {
                if (town.NationId != firm.nation_recno)
                    continue;

                // TODO other region
                if (town.RegionId != firm.region_id)
                    continue;

                // TODO do not recruit if population is low
                if (town.JoblessPopulation == 0)
                    continue;

                // TODO Choose race better
                int race = town.PickRandomRace(false, true);
                if (race == 0 || !town.CanRecruit(race))
                    continue;

                // TODO check not only distance but also which race we are going to settle
                int townDistance = Misc.PointsDistance(town.LocX1, town.LocY1, town.LocX2, town.LocY2,
                    firm.loc_x1, firm.loc_y1, firm.loc_x2, firm.loc_y2);
                if (townDistance < minTownDistance)
                {
                    minTownDistance = townDistance;
                    bestTown = town;
                    bestRace = race;
                }
            }

            if (bestTown != null)
            {
                // TODO Choose race better
                _settlerId = bestTown.Recruit(-1, bestRace, InternalConstants.COMMAND_AI);
            }
        }

        if (_settlerId == 0)
            return;

        Unit settler = UnitArray[_settlerId];
        
        if (!_settlerSent)
        {
            Location firmLocation = World.GetLoc(firm.loc_x1, firm.loc_y1);
            int minRating = Int32.MaxValue;
            int bestSettleLocX = -1;
            int bestSettleLocY = -1;
            
            // TODO use better bounds
            for (int settleLocX = firm.loc_x1 - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE - InternalConstants.TOWN_WIDTH;
                 settleLocX < firm.loc_x2 + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE + InternalConstants.TOWN_WIDTH;
                 settleLocX++)
            {
                if (!Misc.IsLocationValid(settleLocX, 0))
                    continue;
                
                for (int settleLocY = firm.loc_y1 - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE - InternalConstants.TOWN_HEIGHT;
                     settleLocY < firm.loc_y2 + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE + InternalConstants.TOWN_HEIGHT;
                     settleLocY++)
                {
                    if (!Misc.IsLocationValid(0, settleLocY))
                        continue;

                    int settleLocX2 = settleLocX + InternalConstants.TOWN_WIDTH - 1;
                    int settleLocY2 = settleLocY + InternalConstants.TOWN_HEIGHT - 1;
                    if (Misc.rects_distance(settleLocX, settleLocY, settleLocX2, settleLocY2,
                            firm.loc_x1, firm.loc_y1, firm.loc_x2, firm.loc_y2) > InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE)
                    {
                        continue;
                    }

                    Location settleLocation = World.GetLoc(settleLocX, settleLocY);
                    if (settleLocation.RegionId != firmLocation.RegionId || settleLocation.IsPlateau() != firmLocation.IsPlateau())
                        continue;

                    if (!World.CanBuildTown(settleLocX, settleLocY, _settlerId))
                        continue;

                    int rating = Misc.PointsDistance(settleLocX, settleLocY, settleLocX2, settleLocY2,
                        firm.loc_x1, firm.loc_y1, firm.loc_x2, firm.loc_y2);
                    foreach ((int, int) locXlocY in Misc.EnumerateNearLocations(settleLocX, settleLocY, settleLocX2, settleLocY2, 1))
                    {
                        Location nearLocation = World.GetLoc(locXlocY.Item1, locXlocY.Item2);
                        if (nearLocation.IsFirm() || nearLocation.IsTown())
                            rating++;
                    }
                    
                    // TODO rating should also depend on distance to the enemy and to our other villages

                    if (rating < minRating)
                    {
                        minRating = rating;
                        bestSettleLocX = settleLocX;
                        bestSettleLocY = settleLocY;
                    }
                }
            }

            if (bestSettleLocX != -1 && bestSettleLocY != -1)
            {
                settler.settle(bestSettleLocX, bestSettleLocY);
                _settlerSent = true;
            }
            else
            {
                _noPlaceToSettle = true;
            }
        }
        else
        {
            //TODO check that settler is on the way, not stuck and is able to settle
            if (settler.is_ai_all_stop())
                _settlerSent = false;
        }
    }
}