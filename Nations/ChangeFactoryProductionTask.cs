namespace TenKingdoms;

// Change factory production when
//  1. A new factory is built
//  2. Factory has no raw resource available but can be switched to produce another type of product - TODO

public class ChangeFactoryProductionTask : AITask
{
    private bool _shouldCancel;
    private int FactoryId { get; }
    private int ProductId { get; }
    
    public ChangeFactoryProductionTask(Nation nation, int factoryId, int productId) : base(nation)
    {
        FactoryId = factoryId;
        ProductId = productId;
    }

    public override bool ShouldCancel()
    {
        if (_shouldCancel)
            return true;
        
        if (FirmIsDeletedOrChangedNation(FactoryId))
            return true;

        return false;
    }

    public override void Process()
    {
        FirmFactory factory = (FirmFactory)FirmArray[FactoryId];
        if (factory.UnderConstruction)
            return;
        
        while (factory.ProductId != ProductId)
        {
            factory.ChangeProduction();
        }

        _shouldCancel = true;
    }
}