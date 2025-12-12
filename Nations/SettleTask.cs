using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class SettleTask : AITask, IUnitTask
{
    private int _settlerId;
    private bool _settlerSent;
    private bool _noPlaceToSettle;
    public int FirmId { get; }

    public int UnitId => _settlerId;
    
    public SettleTask(NationBase nation, int firmId) : base(nation)
    {
        FirmId = firmId;
    }

    public override bool ShouldCancel()
    {
        if (_noPlaceToSettle)
            return true;

        if (FirmArray.IsDeleted(FirmId))
            return true;

        Firm firm = FirmArray[FirmId];
        if (firm.NationId != Nation.nation_recno)
            return true;

        foreach (int townId in firm.LinkedTowns)
        {
            Town town = TownArray[townId];
            if (town.NationId == Nation.nation_recno)
                return true;
        }

        return false;
    }

    public override void Cancel()
    {
        if (_settlerId != 0 && !UnitArray.IsDeleted(_settlerId))
        {
            Unit settler = UnitArray[_settlerId];
            settler.Stop2();
        }
    }
    
    public override void Process()
    {
        Firm firm = FirmArray[FirmId];

        if (_settlerId != 0 && UnitArray.IsDeleted(_settlerId))
            _settlerId = 0;

        if (_settlerId == 0)
            FindSettler(firm.LocX1, firm.LocY1, firm.LocX2, firm.LocY2);

        if (_settlerId == 0)
            return;

        Unit settler = UnitArray[_settlerId];
        
        if (!_settlerSent)
        {
            (int settleLocX, int settleLocY) = FindBestSettleLocation(firm.LocX1, firm.LocY1, firm.LocX2, firm.LocY2);
            if (settleLocX != -1 && settleLocY != -1)
            {
                settler.Settle(settleLocX, settleLocY);
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
            if (settler.IsAIAllStop())
                _settlerSent = false;
        }
    }

    private void FindSettler(int firmLocX1, int firmLocY1, int firmLocX2, int firmLocY2)
    {
        Location firmLocation = World.GetLoc(firmLocX1, firmLocY1);
        
        int minTownDistance = Int16.MaxValue;
        Town bestTown = null;
        int bestRace = 0;

        foreach (Town town in TownArray)
        {
            if (town.NationId != Nation.nation_recno)
                continue;

            // TODO other region
            if (town.RegionId != firmLocation.RegionId)
                continue;

            // TODO do not recruit if population is low
            if (town.JoblessPopulation == 0)
                continue;

            // TODO check all races, select race better
            int race = town.PickRandomRace(false, true);
            if (race == 0 || !town.CanRecruit(race))
                continue;

            // TODO check not only distance but also which race we are going to settle
            int townDistance = Misc.RectsDistance(town.LocX1, town.LocY1, town.LocX2, town.LocY2,
                firmLocX1, firmLocY1, firmLocX2, firmLocY2);
            if (townDistance < minTownDistance)
            {
                minTownDistance = townDistance;
                bestTown = town;
                bestRace = race;
            }
        }

        if (bestTown != null)
        {
            _settlerId = bestTown.Recruit(-1, bestRace, InternalConstants.COMMAND_AI);
        }
    }

    private (int, int) FindBestSettleLocation(int firmLocX1, int firmLocY1, int firmLocX2, int firmLocY2)
    {
        Location firmLocation = World.GetLoc(firmLocX1, firmLocY1);
        int maxRating = Int16.MinValue;
        List<(int, int)> maxRatingLocations = new List<(int, int)>();

        List<Firm> nearFirms = new List<Firm>();
        foreach (Firm firm in FirmArray)
        {
            Location otherFirmLocation = World.GetLoc(firm.LocX1, firm.LocY1);

            if (otherFirmLocation.IsPlateau() != firmLocation.IsPlateau() || otherFirmLocation.RegionId != firmLocation.RegionId)
                continue;

            if (Misc.RectsDistance(firm.LocX1, firm.LocY1, firm.LocX2, firm.LocY2,
                    firmLocX1, firmLocY1, firmLocX2, firmLocY2) <= InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE * 3)
            {
                nearFirms.Add(firm);
            }
        }

        List<Town> nearTowns = new List<Town>();
        foreach (Town town in TownArray)
        {
            Location townLocation = World.GetLoc(town.LocX1, town.LocY1);
            
            if (townLocation.IsPlateau() != firmLocation.IsPlateau() || townLocation.RegionId != firmLocation.RegionId)
                continue;

            if (Misc.RectsDistance(town.LocX1, town.LocY1, town.LocX2, town.LocY2,
                    firmLocX1, firmLocY1, firmLocX2, firmLocY2) <= InternalConstants.EFFECTIVE_TOWN_TOWN_DISTANCE * 3)
            {
                nearTowns.Add(town);
            }
        }

        for (int settleLocY = firmLocY1 - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE - InternalConstants.TOWN_HEIGHT;
             settleLocY < firmLocY2 + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE + InternalConstants.TOWN_HEIGHT;
             settleLocY++)
        {
            for (int settleLocX = firmLocX1 - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE - InternalConstants.TOWN_WIDTH;
                 settleLocX < firmLocX2 + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE + InternalConstants.TOWN_WIDTH;
                 settleLocX++)
            {
                int settleLocX2 = settleLocX + InternalConstants.TOWN_WIDTH - 1;
                int settleLocY2 = settleLocY + InternalConstants.TOWN_HEIGHT - 1;

                if (!Misc.IsLocationValid(settleLocX, settleLocY) || !Misc.IsLocationValid(settleLocX2, settleLocY2))
                    continue;

                if (!Misc.AreTownAndFirmLinked(settleLocX, settleLocY, settleLocX2, settleLocY2, firmLocX1, firmLocY1, firmLocX2, firmLocY2))
                    continue;
                
                Location settleLocation = World.GetLoc(settleLocX, settleLocY);
                if (settleLocation.RegionId != firmLocation.RegionId || settleLocation.IsPlateau() != firmLocation.IsPlateau())
                    continue;
                
                if (!World.CanBuildTown(settleLocX, settleLocY, _settlerId))
                    continue;

                int rating = 0;
                foreach (Firm nearFirm in nearFirms)
                {
                    if (!Misc.AreTownAndFirmLinked(settleLocX, settleLocY, settleLocX2, settleLocY2,
                            nearFirm.LocX1, nearFirm.LocY1, nearFirm.LocX2, nearFirm.LocY2))
                        continue;

                    if (nearFirm.NationId == Nation.nation_recno)
                    {
                        if (nearFirm.FirmType == Firm.FIRM_CAMP)
                            rating += 100;

                        if (nearFirm.FirmType == Firm.FIRM_MARKET)
                            rating += 50;
                    }
                    
                    // TODO calculate rating for other nation firms
                }

                foreach (Town nearTown in nearTowns)
                {
                    if (!Misc.AreTownsLinked(settleLocX, settleLocY, settleLocX2, settleLocY2,
                            nearTown.LocX1, nearTown.LocY1, nearTown.LocX2, nearTown.LocY2))
                        continue;
                    
                    if (nearTown.NationId == Nation.nation_recno)
                        rating += 50;
                    
                    // TODO calculate rating for other nation towns
                }

                foreach (Location nearLocation in Misc.EnumerateNearLocations(settleLocX, settleLocY, settleLocX2, settleLocY2))
                {
                    if (!nearLocation.Walkable())
                        rating -= 10;
                }

                rating -= Misc.RectsDistance(settleLocX, settleLocY, settleLocX2, settleLocY2,
                    firmLocX1, firmLocY1, firmLocX2, firmLocY2) * 5;
                
                if (rating > maxRating)
                {
                    maxRating = rating;
                    maxRatingLocations.Clear();
                }
                
                if (rating == maxRating)
                    maxRatingLocations.Add((settleLocX, settleLocY));
            }
        }

        if (maxRatingLocations.Count > 0)
        {
            return maxRatingLocations[Misc.Random(maxRatingLocations.Count)];
        }

        return (-1, -1);
    }
}