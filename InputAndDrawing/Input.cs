namespace TenKingdoms;

public partial class Renderer
{
    public void ProcessInput(int eventType, int x, int y)
    {
        if (eventType == InputConstants.LeftMousePressed)
        {
            if (x >= ZoomMapX && x < ZoomMapX + ZoomMapWidth && y >= ZoomMapY && y < ZoomMapY + ZoomMapHeight)
            {
                int xLoc = topLeftX + (x - ZoomMapX) / ZoomTextureWidth;
                int yLoc = topLeftY + (y - ZoomMapY) / ZoomTextureHeight;

                Location location = World.get_loc(xLoc, yLoc);
                if (location.is_town())
                {
                    selectedFirmId = 0;
                    selectedUnitId = 0;
                    selectedTownId = location.town_recno();
                    Town town = TownArray[selectedTownId];
                }

                if (location.is_firm())
                {
                    selectedTownId = 0;
                    selectedUnitId = 0;
                }

                if (location.has_unit(UnitConstants.UNIT_LAND))
                {
                    selectedTownId = 0;
                    selectedFirmId = 0;
                    selectedUnitId = location.unit_recno(UnitConstants.UNIT_LAND);
                    Unit unit = UnitArray[selectedUnitId];
                    unit.selected_flag = true;
                }
            }

            if (x >= MiniMapX && x < MiniMapX + MiniMapSize && y >= MiniMapY && y < MiniMapY + MiniMapSize)
            {
                int xLoc = x - MiniMapX;
                int yLoc = y - MiniMapY;
                if (MiniMapSize > GameConstants.MapSize)
                {
                    xLoc /= MiniMapScale;
                    yLoc /= MiniMapScale;
                }
                if (MiniMapSize < GameConstants.MapSize)
                {
                    xLoc *= MiniMapScale;
                    yLoc *= MiniMapScale;
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