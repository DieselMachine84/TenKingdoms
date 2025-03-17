namespace TenKingdoms;

public partial class Renderer
{
    public void ProcessInput(int eventType, int x, int y)
    {
        if (eventType == InputConstants.LeftMousePressed)
        {
            if (x >= MainViewX && x < MainViewX + MainViewWidth && y >= MainViewY && y < MainViewY + MainViewHeight)
            {
                int locX = _topLeftX + (x - MainViewX) / CellTextureWidth;
                int locY = _topLeftY + (y - MainViewY) / CellTextureHeight;

                Location location = World.get_loc(locX, locY);
                if (location.is_town())
                {
                    _selectedFirmId = _selectedUnitId = _selectedSiteId = 0;
                    _selectedTownId = location.town_recno();
                }

                if (location.is_firm())
                {
                    _selectedTownId = _selectedUnitId = _selectedSiteId = 0;
                    _selectedFirmId = location.firm_recno();
                }

                if (location.has_unit(UnitConstants.UNIT_LAND))
                {
                    _selectedTownId = _selectedFirmId = _selectedSiteId = 0;
                    _selectedUnitId = location.unit_recno(UnitConstants.UNIT_LAND);
                    UnitArray[_selectedUnitId].selected_flag = true;
                }

                if (location.has_site())
                {
                    _selectedTownId = _selectedFirmId = _selectedUnitId = 0;
                    _selectedSiteId = location.site_recno();
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
                
                _topLeftX = xLoc - MainViewWidthInCells / 2;
                if (_topLeftX < 0)
                    _topLeftX = 0;
                if (_topLeftX > GameConstants.MapSize - MainViewWidthInCells)
                    _topLeftX = GameConstants.MapSize - MainViewWidthInCells;

                _topLeftY = yLoc - MainViewHeightInCells / 2;
                if (_topLeftY < 0)
                    _topLeftY = 0;
                if (_topLeftY > GameConstants.MapSize - MainViewHeightInCells)
                    _topLeftY = GameConstants.MapSize - MainViewHeightInCells;
            }
        }

        if (eventType == InputConstants.RightMousePressed)
        {
            if (x >= MainViewX && x < MainViewX + MainViewWidth && y >= MainViewY && y < MainViewY + MainViewHeight)
            {
                int locX = _topLeftX + (x - MainViewX) / CellTextureWidth;
                int locY = _topLeftY + (y - MainViewY) / CellTextureHeight;

                foreach (Unit unit in UnitArray)
                {
                    if (unit.selected_flag)
                    {
                        if (unit.nation_recno == 1)
                        {
                            Nation nation = NationArray[unit.nation_recno];
                            if (nation.nation_type == NationBase.NATION_OWN)
                                unit.move_to(locX, locY);
                        }
                    }
                }
            }
        }
    }
}