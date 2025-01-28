using System;

namespace TenKingdoms;

public class MagicWeather
{
    private int rain_str;
    private int wind_spd;
    private int wind_dir;

    public int rain_day;
    public int wind_day;
    public int lightning_day;

    public void init()
    {
        rain_day = 0;
        wind_day = 0;
    }

    public void next_day()
    {
        if (rain_day > 0)
            --rain_day;
        if (wind_day > 0)
            --wind_day;
        if (lightning_day > 0)
            --lightning_day;

    }

    public void cast_rain(int duration, int rainScale)
    {
        // override last cast_rain
        rain_day = duration;
        rain_str = rainScale;
    }

    public void cast_wind(int duration, int speed, int direction)
    {
        // override last cast_wind
        wind_day = duration;
        wind_spd = speed;
        wind_dir = direction;
    }

    public void cast_lightning(int duration)
    {
        // override last cast_lightning
        lightning_day = duration;
    }

    public int wind_speed() // wind speed 0 to 100
    {
        return wind_spd;
    }

    public int wind_direct() // 0 to 360
    {
        return wind_dir;
    }

    public double wind_direct_rad() // in radian
    {
        return wind_dir * Math.PI / 180.0;
    }

    public int rain_scale() // rain scale, 0 (no rain) to 12 (heavy rain)
    {
        return rain_str;
    }
}