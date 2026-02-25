using System.IO;

namespace TenKingdoms;

public class WarPointArray
{
    public WarPoint[,] WarPoints { get; private set; }

    private Config Config => Sys.Instance.Config;
 
    public WarPointArray()
    {
        WarPoints = new WarPoint[Config.MapSize / InternalConstants.WARPOINT_ZONE_SIZE + 1, Config.MapSize / InternalConstants.WARPOINT_ZONE_SIZE + 1];
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
    
    #region SaveAndLoad

    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(WarPoints.GetLength(0));
        writer.Write(WarPoints.GetLength(1));
        for (int i = 0; i < WarPoints.GetLength(0); i++)
            for (int j = 0; j < WarPoints.GetLength(1); j++)
                WarPoints[i, j].SaveTo(writer);
    }

    public void LoadFrom(BinaryReader reader)
    {
        int length0 = reader.ReadInt32();
        int length1 = reader.ReadInt32();
        WarPoints = new WarPoint[length0, length1];
        for (int i = 0; i < length0; i++)
        {
            for (int j = 0; j < length1; j++)
            {
                WarPoints[i, j] = new WarPoint();
                WarPoints[i, j].LoadFrom(reader);
            }
        }
    }
	
    #endregion
}