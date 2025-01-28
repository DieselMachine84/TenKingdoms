using System;

namespace TenKingdoms;

public class DsVolume
{
    public long	ds_vol;			// -10,000 to 0 [mB]
    public long	ds_pan;			// -10,000 to 10,000

    public DsVolume(long dsVol, long dsPan)
    {
        ds_vol = dsVol;
        ds_pan = dsPan;
    }

    public DsVolume(AbsVolume absVolume) : this(absVolume.abs_vol * 100 - 10000, absVolume.ds_pan)
    {
    }

    public DsVolume(RelVolume relVolume)
    {
        //TODO audio
        //ds_vol = audio.get_wav_volume() * relVolume.rel_vol - 10000;
        ds_vol = Math.Min(ds_vol, 0);
        ds_vol = Math.Max(ds_vol, -10000);

        ds_pan = relVolume.ds_pan;
    }
}

public class AbsVolume
{
    public long	abs_vol;
    public long	ds_pan;

    public AbsVolume(long absVol, long dsPan)
    {
        abs_vol = absVol;
        ds_pan = dsPan;
    }

    public AbsVolume(DsVolume dsVolume) : this((dsVolume.ds_vol + 10000) / 100, dsVolume.ds_pan)
    {
    }
}

public class RelVolume
{
    public long	rel_vol;			// 0 to 100
    public long	ds_pan;			// -10,000 to 10,000

    public RelVolume()
    {
    }

    public RelVolume(long relVol, long dsPan)
    {
        rel_vol = relVol;
        ds_pan = dsPan;
    }

    public RelVolume(PosVolume posVolume)
    {
        long absX = posVolume.x >= 0 ? posVolume.x : -posVolume.x;
        long absY = posVolume.y >= 0 ? posVolume.y : -posVolume.y;
        long dist = absX >= absY ? absX : absY;
        if (dist <= InternalConstants.DEFAULT_DIST_LIMIT)
            rel_vol = 100 - dist * 100 / InternalConstants.DEFAULT_VOL_DROP;
        else
            rel_vol = 0;

        if (posVolume.x >= InternalConstants.DEFAULT_PAN_DROP)
            ds_pan = 10000;
        else if (posVolume.x <= -InternalConstants.DEFAULT_PAN_DROP)
            ds_pan = -10000;
        else
            ds_pan = 10000 / InternalConstants.DEFAULT_PAN_DROP * posVolume.x;
    }

    public RelVolume(PosVolume posVolume, int drop, int limit)
    {
        long absX = posVolume.x >= 0 ? posVolume.x : -posVolume.x;
        long absY = posVolume.y >= 0 ? posVolume.y : -posVolume.y;
        long dist = absX >= absY ? absX : absY;
        if (dist <= limit)
            rel_vol = 100 - dist * 100 / drop;
        else
            rel_vol = 0;

        if (posVolume.x >= InternalConstants.DEFAULT_PAN_DROP)
            ds_pan = 10000;
        else if (posVolume.x <= -InternalConstants.DEFAULT_PAN_DROP)
            ds_pan = -10000;
        else
            ds_pan = 10000 / InternalConstants.DEFAULT_PAN_DROP * posVolume.x;
    }
}

public class PosVolume
{
    public long x;
    public long y;

    public PosVolume(long relLocX, long relLocY)
    {
        x = relLocX;
        y = relLocY;
    }
}
