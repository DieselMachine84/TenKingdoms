namespace TenKingdoms;

public class BuildShipTask : AITask
{
    private bool _shouldCancel;
    public int HarborId { get; }
    private int ShipType { get; }
    
    public BuildShipTask(Nation nation, int harborId, int shipType) : base(nation)
    {
        HarborId = harborId;
        ShipType = shipType;
    }

    public override bool ShouldCancel()
    {
        if (_shouldCancel)
            return true;

        if (FirmArray.IsDeleted(HarborId))
            return true;
        
        return false;
    }

    public override void Process()
    {
        FirmHarbor harbor = (FirmHarbor)FirmArray[HarborId];
        harbor.AddQueue(ShipType);
        _shouldCancel = true;
    }
}