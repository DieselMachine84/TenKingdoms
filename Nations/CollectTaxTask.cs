namespace TenKingdoms;

public class CollectTaxTask : AITask
{
    public CollectTaxTask(Nation nation) : base(nation)
    {
    }

    public override bool ShouldCancel()
    {
        return false;
    }

    public override void Process()
    {
        foreach (Town town in TownArray)
        {
            if (town.NationId != Nation.nation_recno)
                continue;
            
            if (town.AverageLoyalty() > 60.0)
                town.CollectTax(InternalConstants.COMMAND_AI);
        }
    }
}