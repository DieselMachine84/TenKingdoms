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
        foreach (Firm firm in Nation.KingdomCamps)
        {
            if (firm.UnderConstruction)
                continue;

            if (firm.Workers.Count == Firm.MAX_WORKER)
                continue;

            FirmCamp camp = (FirmCamp)firm;
            if (camp.PatrolUnits.Count == Firm.MAX_WORKER + 1)
                continue;

            foreach (int linkedTownId in camp.LinkedTowns)
            {
                Town linkedTown = TownArray[linkedTownId];
                if (linkedTown.NationId != NationId)
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