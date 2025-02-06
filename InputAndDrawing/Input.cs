namespace TenKingdoms;

public static partial class Renderer
{
    public static void ProcessInput(int eventType, int x, int y)
    {
        if (eventType == InputConstants.LeftMousePressed)
        {
            if (x >= MiniMapX && x < MiniMapX + MiniMapSize && y >= MiniMapY && y < MiniMapY + MiniMapSize)
            {
                int xLoc = x - MiniMapX;
                int yLoc = y - MiniMapY;
                if (MiniMapSize > GameConstants.MapSize)
                {
                    const int Scale = MiniMapSize / GameConstants.MapSize;
                    xLoc /= Scale;
                    yLoc /= Scale;
                }
                if (MiniMapSize < GameConstants.MapSize)
                {
                    const int Scale = GameConstants.MapSize / MiniMapSize;
                    xLoc *= Scale;
                    yLoc *= Scale;
                }
                
                topLeftX = xLoc - ZoomMapLocWidth / 2;
                if (topLeftX < 0)
                    topLeftX = 0;
                if (topLeftX > GameConstants.MapSize - ZoomMapLocWidth)
                    topLeftX = GameConstants.MapSize - ZoomMapLocWidth;

                topLeftY = yLoc - ZoomMapLocHeight / 2;
                if (topLeftY < 0)
                    topLeftY = 0;
                if (topLeftY > GameConstants.MapSize - ZoomMapLocHeight)
                    topLeftY = GameConstants.MapSize - ZoomMapLocHeight;
            }
        }
    }
}