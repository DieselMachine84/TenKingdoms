using System;

namespace TenKingdoms;

// If there are too many construction workers settle them

public class ReduceConstructionWorkersTask : AITask
{
    public ReduceConstructionWorkersTask(Nation nation) : base(nation)
    {
    }

    public override bool ShouldCancel()
    {
        return false;
    }

    public override void Process()
    {
        foreach (Town town in Nation.KingdomTowns)
        {
            int constructionWorkersCount = 0;
            int nationLinkedFirmsCount = 0;
            Unit worstConstructionWorker = null;
            int worstConstructionWorkerSkill = Int16.MaxValue;
            Firm worstConstructionWorkerFirm = null;
            foreach (int linkedFirmId in town.LinkedFirms)
            {
                Firm linkedFirm = FirmArray[linkedFirmId];
                if (linkedFirm.NationId != NationId || linkedFirm.UnderConstruction)
                    continue;

                nationLinkedFirmsCount++;

                if (linkedFirm.BuilderId != 0)
                {
                    Unit constructionWorker = UnitArray[linkedFirm.BuilderId];
                    if (constructionWorker.Skill.SkillLevel < worstConstructionWorkerSkill)
                    {
                        worstConstructionWorker = constructionWorker;
                        worstConstructionWorkerSkill = constructionWorker.Skill.SkillLevel;
                        worstConstructionWorkerFirm = linkedFirm;
                    }
                    constructionWorkersCount++;
                }
            }

            if (nationLinkedFirmsCount > 2 && constructionWorkersCount * 100 / nationLinkedFirmsCount > Nation.PrefConstructionWorkersCountPercent)
            {
                if (worstConstructionWorker != null)
                {
                    Town bestTown = null;
                    int bestTownDistance = Int16.MaxValue;
                    foreach (Town otherTown in Nation.KingdomTowns)
                    {
                        if (otherTown.RegionId != town.RegionId)
                            continue;
                        
                        if (otherTown.Population > GameConstants.MAX_TOWN_POPULATION - 5)
                            continue;

                        if (bestTown == null)
                            bestTown = otherTown;

                        if (otherTown.RacesPopulation[worstConstructionWorker.RaceId - 1] > 0)
                        {
                            int townsDistance = Misc.TownsDistance(bestTown, otherTown);
                            if (townsDistance < bestTownDistance)
                            {
                                bestTown = otherTown;
                                bestTownDistance = townsDistance;
                            }
                        }
                    }

                    if (bestTown != null)
                    {
                        worstConstructionWorkerFirm.SetBuilder(0);
                        worstConstructionWorker.Settle(bestTown.LocX1, bestTown.LocY1);
                    }
                }
            }
        }
    }
}