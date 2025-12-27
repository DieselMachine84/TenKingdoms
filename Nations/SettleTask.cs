using System;
using System.Collections.Generic;

namespace TenKingdoms;

// Settle when
//  1. Village population is close to maximum - TODO
//  2. Village has different races - TODO
//  3. A new mine is built

public class SettleTask : AITask, IUnitTask
{
    private int _settlerId;
    private bool _settlerSent;
    private bool _noPlaceToSettle;
    public int FirmId { get; }
    public int UnitId => _settlerId;
    
    public SettleTask(Nation nation, int firmId) : base(nation)
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
            if (settler.IsVisible())
                settler.Stop2();
        }
    }
    
    public override void Process()
    {
        Firm firm = FirmArray[FirmId];

        if (_settlerId != 0 && UnitArray.IsDeleted(_settlerId))
            _settlerId = 0;

        if (_settlerId == 0)
            _settlerId = FindSettler(firm);

        if (_settlerId == 0)
            return;

        Unit settler = UnitArray[_settlerId];
        if (settler.UnitMode == UnitConstants.UNIT_MODE_ON_SHIP)
        {
            Nation.AddSailShipTask((UnitMarine)UnitArray[settler.UnitModeParam], firm.LocCenterX, firm.LocCenterY);
            return;
        }

        if (!_settlerSent)
        {
            if (settler.RegionId() == firm.RegionId)
            {
                (int settleLocX, int settleLocY) = FindBestSettleLocation(firm);
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
                UnitMarine transport = Nation.FindTransportShip(Nation.GetSeaRegion(settler.RegionId(), firm.RegionId));
                if (transport != null)
                {
                    List<int> units = new List<int> { _settlerId };
                    UnitArray.AssignToShip(transport.NextLocX, transport.NextLocY, false, units, InternalConstants.COMMAND_AI, transport.SpriteId);
                    _settlerSent = true;
                }
                else
                {
                    FirmHarbor harbor = Nation.FindHarbor(settler.RegionId(), firm.RegionId);
                    Nation.AddBuildShipTask(harbor, UnitConstants.UNIT_TRANSPORT);
                }
            }
        }
        else
        {
            //TODO check that settler is on the way, not stuck and is able to settle
            if (settler.IsAIAllStop())
                _settlerSent = false;
        }
    }

    private int FindSettler(Firm firm)
    {
        int settlerId = FindSettlerInRegion(firm, firm.RegionId);

        if (settlerId == 0)
        {
            List<int> connectedLands = GetConnectedLands(firm.RegionId);
            foreach (int landRegionId in connectedLands)
            {
                if (landRegionId == firm.RegionId)
                    continue;

                //TODO maybe there is no harbor but we can send ship for the settler
                FirmHarbor harbor = Nation.FindHarbor(landRegionId, firm.RegionId);
                if (harbor != null)
                {
                    settlerId = FindSettlerInRegion(firm, landRegionId);
                    
                    //TODO do not choose the first one, select from all available
                    if (settlerId != 0)
                        break;
                }
            }
        }

        return settlerId;
    }
    
    private int FindSettlerInRegion(Firm firm, int regionId)
    {
        int minTownDistance = Int16.MaxValue;
        Town bestTown = null;
        int bestRace = 0;

        foreach (Town town in TownArray)
        {
            if (town.NationId != Nation.nation_recno)
                continue;

            if (town.RegionId != regionId)
                continue;

            if (town.JoblessPopulation == 0)
                continue;
            
            // TODO do not recruit if population is low
            if (town.Population < 20)
                continue;

            // TODO check all races, select race better
            int race = town.PickRandomRace(false, true);
            if (race == 0 || !town.CanRecruit(race))
                continue;

            // TODO check not only distance but also which race we are going to settle
            int townDistance = Misc.FirmTownDistance(firm, town);
            if (townDistance < minTownDistance)
            {
                minTownDistance = townDistance;
                bestTown = town;
                bestRace = race;
            }
        }

        int settlerId = 0;
        if (bestTown != null)
        {
            settlerId = bestTown.Recruit(-1, bestRace, InternalConstants.COMMAND_AI);
        }

        return settlerId;
    }

    private (int, int) FindBestSettleLocation(Firm firm)
    {
        Location firmLocation = World.GetLoc(firm.LocCenterX, firm.LocCenterY);
        int maxRating = Int16.MinValue;
        List<(int, int)> maxRatingLocations = new List<(int, int)>();

        List<Firm> nearFirms = new List<Firm>();
        foreach (Firm otherFirm in FirmArray)
        {
            Location otherFirmLocation = World.GetLoc(otherFirm.LocX1, otherFirm.LocY1);

            if (otherFirmLocation.IsPlateau() != firmLocation.IsPlateau() || otherFirmLocation.RegionId != firmLocation.RegionId)
                continue;

            if (Misc.FirmsDistance(firm, otherFirm) <= InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE * 3)
                nearFirms.Add(otherFirm);
        }

        List<Town> nearTowns = new List<Town>();
        foreach (Town town in TownArray)
        {
            Location townLocation = World.GetLoc(town.LocX1, town.LocY1);
            
            if (townLocation.IsPlateau() != firmLocation.IsPlateau() || townLocation.RegionId != firmLocation.RegionId)
                continue;

            if (Misc.FirmTownDistance(firm, town) <= InternalConstants.EFFECTIVE_TOWN_TOWN_DISTANCE * 3)
                nearTowns.Add(town);
        }

        for (int settleLocY = firm.LocY1 - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE - InternalConstants.TOWN_HEIGHT;
             settleLocY < firm.LocY2 + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE + InternalConstants.TOWN_HEIGHT;
             settleLocY++)
        {
            for (int settleLocX = firm.LocX1 - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE - InternalConstants.TOWN_WIDTH;
                 settleLocX < firm.LocX2 + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE + InternalConstants.TOWN_WIDTH;
                 settleLocX++)
            {
                int settleLocX2 = settleLocX + InternalConstants.TOWN_WIDTH - 1;
                int settleLocY2 = settleLocY + InternalConstants.TOWN_HEIGHT - 1;

                if (!Misc.IsLocationValid(settleLocX, settleLocY) || !Misc.IsLocationValid(settleLocX2, settleLocY2))
                    continue;

                if (!Misc.AreTownAndFirmLinked(settleLocX, settleLocY, settleLocX2, settleLocY2,
                        firm.LocX1, firm.LocY1, firm.LocX2, firm.LocY2))
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
                    firm.LocX1, firm.LocY1, firm.LocX2, firm.LocY2) * 5;
                
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