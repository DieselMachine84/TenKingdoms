using System;
using System.Collections.Generic;

namespace TenKingdoms;

// Relocate peasants when firm does not have enough workers

public class RelocatePeasantsTask : AITask, IUnitTask
{
    private int _settlerId;
    private bool _settlerSent;
    public int TownId { get; }
    public int UnitId => _settlerId;

    public RelocatePeasantsTask(Nation nation, int townId) : base(nation)
    {
        TownId = townId;
    }

    public override bool ShouldCancel()
    {
        if (TownArray.IsDeleted(TownId))
            return true;
        
        Town town = TownArray[TownId];
        if (town.NationId != Nation.nation_recno || town.JoblessPopulation > 0)
            return true;
        
        return false;
    }

    public override void Process()
    {
        Town town = TownArray[TownId];

        if (_settlerId != 0 && UnitArray.IsDeleted(_settlerId))
            _settlerId = 0;
        
        if (_settlerId == 0)
            _settlerId = FindSettler(town);

        if (_settlerId == 0)
            return;

        Unit settler = UnitArray[_settlerId];
        if (settler.UnitMode == UnitConstants.UNIT_MODE_ON_SHIP)
        {
            Nation.AddSailShipTask((UnitMarine)UnitArray[settler.UnitModeParam], town.LocCenterX, town.LocCenterY);
            return;
        }

        if (!_settlerSent)
        {
            if (settler.RegionId() == town.RegionId)
            {
                settler.Settle(town.LocX1, town.LocY1);
                _settlerSent = true;
            }
            else
            {
                UnitMarine transport = Nation.FindTransportShip(Nation.GetSeaRegion(settler.RegionId(), town.RegionId));
                if (transport != null)
                {
                    List<int> units = new List<int> { _settlerId };
                    UnitArray.AssignToShip(transport.NextLocX, transport.NextLocY, false, units, InternalConstants.COMMAND_AI, transport.SpriteId);
                    _settlerSent = true;
                }
                else
                {
                    FirmHarbor harbor = Nation.FindHarbor(settler.RegionId(), town.RegionId);
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

    private int FindSettler(Town town)
    {
        int settlerId = FindSettlerInRegion(town, town.RegionId);

        if (settlerId == 0)
        {
            List<int> connectedLands = GetConnectedLands(town.RegionId);
            foreach (int landRegionId in connectedLands)
            {
                if (landRegionId == town.RegionId)
                    continue;

                //TODO maybe there is no harbor but we can send ship for the settler
                FirmHarbor harbor = Nation.FindHarbor(landRegionId, town.RegionId);
                if (harbor != null)
                {
                    settlerId = FindSettlerInRegion(town, landRegionId);
                    
                    //TODO do not choose the first one, select from all available
                    if (settlerId != 0)
                        break;
                }
            }
        }

        return settlerId;
    }

    private int FindSettlerInRegion(Town town, int regionId)
    {
        Town bestTown = null;
        int minDistance = Int16.MaxValue;
        int bestRace = -1;

        foreach (Town otherTown in TownArray)
        {
            if (otherTown.NationId != Nation.nation_recno || otherTown.TownId == town.TownId)
                continue;

            if (otherTown.RegionId != regionId || otherTown.JoblessPopulation == 0)
                continue;

            if (otherTown.AverageLoyalty() < 40)
                continue;

            if (otherTown.Population < 20)
                continue;

            int distance = Misc.TownsDistance(town, otherTown);
            if (distance < minDistance)
            {
                int maxRacePop = 0;
                for (int i = 1; i <= GameConstants.MAX_RACE; i++)
                {
                    if (!otherTown.CanRecruit(i))
                        continue;

                    if (town.RacesPopulation[i - 1] != 0 && town.RacesPopulation[i - 1] > maxRacePop &&
                        otherTown.RacesJoblessPopulation[i - 1] != 0 && otherTown.RacesLoyalty[i - 1] > 40.0)
                    {
                        bestTown = otherTown;
                        minDistance = distance;
                        bestRace = i;
                        maxRacePop = town.RacesPopulation[i - 1];
                    }
                }
            }
        }

        return bestTown != null ? bestTown.Recruit(-1, bestRace, InternalConstants.COMMAND_AI) : 0;
    }
}