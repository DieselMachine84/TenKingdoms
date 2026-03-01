using System;

namespace TenKingdoms;

public class DsVolume
{
    public long	DsVol { get; }			// -10,000 to 0 [mB]
    public long	DsPan { get; }			// -10,000 to 10,000

    public DsVolume(long dsVol, long dsPan)
    {
        DsVol = dsVol;
        DsPan = dsPan;
    }

    public DsVolume(AbsVolume absVolume) : this(absVolume.AbsVol * 100 - 10000, absVolume.DsPan)
    {
    }

    public DsVolume(RelVolume relVolume)
    {
        //TODO audio
        //ds_vol = audio.get_wav_volume() * relVolume.rel_vol - 10000;
        DsVol = Math.Min(DsVol, 0);
        DsVol = Math.Max(DsVol, -10000);
        DsPan = relVolume.DsPan;
    }
}

public class AbsVolume
{
    public long	AbsVol { get; }
    public long	DsPan { get; }

    public AbsVolume(long absVol, long dsPan)
    {
        AbsVol = absVol;
        DsPan = dsPan;
    }

    public AbsVolume(DsVolume dsVolume) : this((dsVolume.DsVol + 10000) / 100, dsVolume.DsPan)
    {
    }
}

public class RelVolume
{
    public long	RelVol { get; set; }    // 0 to 100
    public long	DsPan { get; set; }			// -10,000 to 10,000

    public RelVolume()
    {
    }

    public RelVolume(long relVol, long dsPan)
    {
        RelVol = relVol;
        DsPan = dsPan;
    }

    public RelVolume(PosVolume posVolume)
    {
        long absX = posVolume.RelLocX >= 0 ? posVolume.RelLocX : -posVolume.RelLocX;
        long absY = posVolume.RelLocY >= 0 ? posVolume.RelLocY : -posVolume.RelLocY;
        long dist = absX >= absY ? absX : absY;
        if (dist <= InternalConstants.DEFAULT_DIST_LIMIT)
            RelVol = 100 - dist * 100 / InternalConstants.DEFAULT_VOL_DROP;
        else
            RelVol = 0;

        if (posVolume.RelLocX >= InternalConstants.DEFAULT_PAN_DROP)
            DsPan = 10000;
        else if (posVolume.RelLocX <= -InternalConstants.DEFAULT_PAN_DROP)
            DsPan = -10000;
        else
            DsPan = 10000 / InternalConstants.DEFAULT_PAN_DROP * posVolume.RelLocX;
    }

    public RelVolume(PosVolume posVolume, int drop, int limit)
    {
        long absX = posVolume.RelLocX >= 0 ? posVolume.RelLocX : -posVolume.RelLocX;
        long absY = posVolume.RelLocY >= 0 ? posVolume.RelLocY : -posVolume.RelLocY;
        long dist = absX >= absY ? absX : absY;
        if (dist <= limit)
            RelVol = 100 - dist * 100 / drop;
        else
            RelVol = 0;

        if (posVolume.RelLocX >= InternalConstants.DEFAULT_PAN_DROP)
            DsPan = 10000;
        else if (posVolume.RelLocX <= -InternalConstants.DEFAULT_PAN_DROP)
            DsPan = -10000;
        else
            DsPan = 10000 / InternalConstants.DEFAULT_PAN_DROP * posVolume.RelLocX;
    }
}

public class PosVolume
{
    public long RelLocX { get; }
    public long RelLocY { get; }

    public PosVolume(long relLocX, long relLocY)
    {
        RelLocX = relLocX;
        RelLocY = relLocY;
    }
}
