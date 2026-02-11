namespace TenKingdoms;

public class CollectTaxTask : AITask
{
    public CollectTaxTask(NationNew nation) : base(nation)
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
            if (town.AverageLoyalty() > 60.0)
                town.CollectTax(InternalConstants.COMMAND_AI);
        }
    }
}