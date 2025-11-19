namespace TenKingdoms;

public class FirmDie : IIdObject
{
    public int FirmDieId { get; private set; }
    public int NationId { get; private set; }
    private int FirmType { get; set; }
    public int FirmBuildId { get; private set; }
    public int Frame { get; private set; }
    private int FrameDelayCount { get; set; }
    public int LocX1 { get; private set; }
    public int LocY1 { get; private set; }
    public int LocX2  { get; private set; }
    public int LocY2 { get; private set; }

    private FirmDieRes FirmDieRes => Sys.Instance.FirmDieRes;

    void IIdObject.SetId(int id)
    {
        FirmDieId = id;
    }

    public void Init(Firm firm)
    {
        NationId = firm.NationId;
        FirmType = firm.FirmType;
        FirmBuildId = firm.FirmBuildId;
        LocX1 = firm.LocX1;
        LocY1 = firm.LocY1;
        LocX2 = firm.LocX2;
        LocY2 = firm.LocY2;
        Frame = 1;
    }

    public bool Process()
    {
        FirmBuild firmBuild = FirmDieRes.GetBuild(FirmBuildId);
        if (++FrameDelayCount > firmBuild.frame_delay_array[Frame - 1])
        {
            FrameDelayCount = 0;
            if (++Frame > firmBuild.frame_count)
            {
                return true;
            }
        }

        return false;
    }
}