using System;

namespace TenKingdoms;

public class MagicWeather
{
    public int RainStrength { get; private set; }
    public int WindSpeed { get; private set; }
    public int WindDirection { get; private set; }
    public double WindDirectionRadians => WindDirection * Math.PI / 180.0;
    public int RainDay { get; private set; }
    public int WindDay { get; private set; }
    public int LightningDay { get; private set; }

    public void NextDay()
    {
        if (RainDay > 0)
            RainDay--;
        if (WindDay > 0)
            WindDay--;
        if (LightningDay > 0)
            LightningDay--;

    }

    public void CastRain(int duration, int rainScale)
    {
        RainDay = duration;
        RainStrength = rainScale;
    }

    public void CastWind(int duration, int speed, int direction)
    {
        WindDay = duration;
        WindSpeed = speed;
        WindDirection = direction;
    }

    public void CastLightning(int duration)
    {
        LightningDay = duration;
    }
}