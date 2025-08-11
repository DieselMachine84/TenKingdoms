namespace TenKingdoms;

public class FirmDie : IIdObject
{
    public int firm_id;
    public int firm_build_id;
    public int nation_recno;
    public int firmdie_recno;
    public int frame;
    public int frame_delay_count;
    public int loc_x1, loc_y1, loc_x2, loc_y2;

    private FirmDieRes FirmDieRes => Sys.Instance.FirmDieRes;

    void IIdObject.SetId(int id)
    {
        firmdie_recno = id;
    }

    public void Init(int firmId, int firmBuildId, int nationRecno,
        int locX1, int locY1, int locX2, int locY2)
    {
        firm_id = firmId;
        firm_build_id = firmBuildId;
        nation_recno = nationRecno;
        loc_x1 = locX1;
        loc_y1 = locY1;
        loc_x2 = locX2;
        loc_y2 = locY2;
        frame = 1;
    }

    public void Init(Firm firm)
    {
        firm_id = firm.FirmType;
        firm_build_id = firm.FirmBuildId;
        nation_recno = firm.NationId;
        loc_x1 = firm.LocX1;
        loc_y1 = firm.LocY1;
        loc_x2 = firm.LocX2;
        loc_y2 = firm.LocY2;
        frame = 1;
    }

    public bool Process()
    {
        FirmBuild firmBuild = FirmDieRes.get_build(firm_build_id);
        if (++frame_delay_count > firmBuild.frame_delay_array[frame - 1])
        {
            frame_delay_count = 0;
            if (++frame > firmBuild.frame_count)
            {
                return true;
            }
        }

        return false;
    }
}