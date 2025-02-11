namespace TenKingdoms;

public abstract class AITask
{
    public NationBase Nation { get; }

    protected World World => Sys.Instance.World;
    
    protected FirmArray FirmArray => Sys.Instance.FirmArray;
    protected TownArray TownArray => Sys.Instance.TownArray;
    protected UnitArray UnitArray => Sys.Instance.UnitArray;
    protected SiteArray SiteArray => Sys.Instance.SiteArray;

    protected AITask(NationBase nation)
    {
        Nation = nation;
    }

    public abstract bool ShouldCancel();

    public abstract void Process();
}
