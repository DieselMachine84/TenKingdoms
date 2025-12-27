namespace TenKingdoms;

//TODO join several SailShipTasks into one

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

        if (UnitIsDeletedOrChangedNation(ShipId))
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
                if (ship.InBeach)
                {
                    //TODO workaround for a bug when ship cannot move to the coast
                    int locX = Misc.Random(GameConstants.MapSize);
                    if (locX % 2 != 0)
                        locX--;
                    int locY = Misc.Random(GameConstants.MapSize);
                    if (locY % 2 != 0)
                        locY--;
                    ship.MoveTo(locX, locY);
                }
                else
                {
                    ship.ShipToBeach(TargetLocX, TargetLocY, out _, out _);
                }
            }
        }
    }
}