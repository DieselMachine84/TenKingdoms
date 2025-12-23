namespace TenKingdoms;

public class SailShipTask : AITask, IUnitTask
{
    private bool _shouldCancel;
    public int ShipId { get; }
    private int TargetLocX { get; }
    private int TargetLocY { get; }
    public int UnitId => ShipId;
    
    public SailShipTask(Nation nation, int shipId, int targetLocX, int targetLocY) : base(nation)
    {
        ShipId = shipId;
        TargetLocX = targetLocX;
        TargetLocY = targetLocY;
    }

    public override bool ShouldCancel()
    {
        if (_shouldCancel)
            return true;

        if (UnitArray.IsDeleted(ShipId))
            return true;

        return false;
    }

    public override void Process()
    {
        Location targetLocation = World.GetLoc(TargetLocX, TargetLocY);
        UnitMarine ship = (UnitMarine)UnitArray[ShipId];
        if (ship.CanUnloadUnit())
        {
            if (ship.GetCoastRegion() == targetLocation.RegionId)
            {
                ship.UnloadAllUnits(InternalConstants.COMMAND_AI);
                if (ship.UnitsOnBoard.Count == 0)
                    _shouldCancel = true;
            }
            else
            {
                ship.ShipToBeach(TargetLocX, TargetLocY, out _, out _);
            }
        }
        else
        {
            if (ship.IsAIAllStop())
            {
                ship.ShipToBeach(TargetLocX, TargetLocY, out _, out _);
            }
        }
    }
}