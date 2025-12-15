using System;

namespace TenKingdoms;

public class RelocatePeasantsTask : AITask
{
    public int FirmId { get; }

    public RelocatePeasantsTask(Nation nation, int firmId) : base(nation)
    {
        FirmId = firmId;
    }

    public override bool ShouldCancel()
    {
        if (FirmArray.IsDeleted(FirmId))
            return true;
        
        Firm firm = FirmArray[FirmId];
        if (firm.NationId != Nation.nation_recno)
            return true;
        
        bool hasJoblessPopulation = false;
        foreach (int townId in firm.LinkedTowns)
        {
            Town town = TownArray[townId];
            if (town.NationId == Nation.nation_recno && town.JoblessPopulation > 0)
                hasJoblessPopulation = true;
        }

        if (hasJoblessPopulation)
            return true;

        return false;
    }

    public override void Process()
    {
        Firm firm = FirmArray[FirmId];

        foreach (int townId in firm.LinkedTowns)
        {
            Town town = TownArray[townId];
            if (town.NationId != Nation.nation_recno || town.JoblessPopulation > 0)
                continue;

            Town bestTown = null;
            int minDistance = Int16.MaxValue;
            int bestRace = -1;

            foreach (Town otherTown in TownArray)
            {
                if (otherTown.NationId != Nation.nation_recno || otherTown.TownId == town.TownId)
                    continue;

                if (otherTown.JoblessPopulation == 0)
                    continue;

                if (otherTown.AverageLoyalty() < 40)
                    continue;

                //TODO other region
                if (otherTown.RegionId != town.RegionId)
                    continue;

                //TODO should depend on kingdom preferences
                if (otherTown.Population < 20)
                    continue;

                int distance = Misc.RectsDistance(town.LocX1, town.LocY1, town.LocX2, town.LocY2,
                    otherTown.LocX1, otherTown.LocY1, otherTown.LocX2, otherTown.LocY2);
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

            if (bestTown != null)
            {
                int unitId = bestTown.Recruit(-1, bestRace, InternalConstants.COMMAND_AI);
                if (unitId != 0)
                {
                    Unit unit = UnitArray[unitId];
                    unit.Settle(town.LocX1, town.LocY1);
                    return;
                }
            }
        }
    }
}