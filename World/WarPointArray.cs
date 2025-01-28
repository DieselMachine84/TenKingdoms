using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class WarPointArray
{
    //TODO use List instead
    public WarPoint[] warPoints = new WarPoint[GameConstants.MapSize / InternalConstants.WARPOINT_ZONE_SIZE];
    public int draw_phase;

    public WarPointArray()
    {
        for (int i = 0; i < warPoints.Length; i++)
            warPoints[i] = new WarPoint();
    }

    public void Process()
    {
        foreach (WarPoint warPoint in warPoints)
        {
            warPoint.Decay();
        }
    }

    public void AddPoint(int xLoc, int yLoc)
    {
        warPoints[GameConstants.MapSize / xLoc].Inc();
    }
}