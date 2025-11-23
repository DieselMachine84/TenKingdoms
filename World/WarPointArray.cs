using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class WarPointArray
{
    public WarPoint[,] WarPoints { get; } = new WarPoint[
        GameConstants.MapSize / InternalConstants.WARPOINT_ZONE_SIZE + 1,
        GameConstants.MapSize / InternalConstants.WARPOINT_ZONE_SIZE + 1];
 
    public WarPointArray()
    {
        for (int i = 0; i < WarPoints.GetLength(0); i++)
            for (int j = 0; j < WarPoints.GetLength(1); j++)
                WarPoints[i, j] = new WarPoint();
    }

    public void Process()
    {
        foreach (WarPoint warPoint in WarPoints)
        {
            warPoint.Decay();
        }
    }

    public void AddPoint(int locX, int locY)
    {
        WarPoints[locX / InternalConstants.WARPOINT_ZONE_SIZE, locY / InternalConstants.WARPOINT_ZONE_SIZE].Inc();
    }
}