namespace TenKingdoms;

public class RecruitSoldiersTask : AITask
{
    public RecruitSoldiersTask(Nation nation) : base(nation)
    {
    }

    public override bool ShouldCancel()
    {
        return false;
    }

    public override void Process()
    {
        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId != Nation.nation_recno)
                continue;

            if (firm.UnderConstruction)
                continue;

            if (firm.Workers.Count == Firm.MAX_WORKER)
                continue;

            if (firm is FirmCamp camp)
            {
                if (camp.patrol_unit_array.Count == Firm.MAX_WORKER + 1)
                    continue;
                
                foreach (int linkedTownId in camp.LinkedTowns)
                {
                    Town linkedTown = TownArray[linkedTownId];
                    if (linkedTown.NationId != Nation.nation_recno)
                        continue;

                    if (linkedTown.Population < 20 || linkedTown.JoblessPopulation < 5)
                        continue;

                    if (linkedTown.AverageLoyalty() < 50)
                        continue;

                    camp.PullTownPeople(linkedTownId, InternalConstants.COMMAND_AI, 0, true);
                }
            }
        }
    }
}