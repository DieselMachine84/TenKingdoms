namespace TenKingdoms;

public partial class Renderer
{
    public void ProcessInput(int eventType, int x, int y)
    {
        if (eventType == InputConstants.LeftMousePressed)
        {
            if (x >= MainViewX && x < MainViewX + MainViewWidth && y >= MainViewY && y < MainViewY + MainViewHeight)
            {
                int locX = _topLeftLocX + (x - MainViewX) / CellTextureWidth;
                int locY = _topLeftLocY + (y - MainViewY) / CellTextureHeight;

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
                int locX = x - MiniMapX;
                int locY = y - MiniMapY;
                if (MiniMapSize > GameConstants.MapSize)
                {
                    locX /= MiniMapScale;
                    locY /= MiniMapScale;
                }
                if (MiniMapSize < GameConstants.MapSize)
                {
                    locX *= MiniMapScale;
                    locY *= MiniMapScale;
                }
                
                _topLeftLocX = locX - MainViewWidthInCells / 2;
                if (_topLeftLocX < 0)
                    _topLeftLocX = 0;
                if (_topLeftLocX > GameConstants.MapSize - MainViewWidthInCells)
                    _topLeftLocX = GameConstants.MapSize - MainViewWidthInCells;

                _topLeftLocY = locY - MainViewHeightInCells / 2;
                if (_topLeftLocY < 0)
                    _topLeftLocY = 0;
                if (_topLeftLocY > GameConstants.MapSize - MainViewHeightInCells)
                    _topLeftLocY = GameConstants.MapSize - MainViewHeightInCells;
            }
        }

        if (eventType == InputConstants.RightMousePressed)
        {
            if (x >= MainViewX && x < MainViewX + MainViewWidth && y >= MainViewY && y < MainViewY + MainViewHeight)
            {
                int locX = _topLeftLocX + (x - MainViewX) / CellTextureWidth;
                int locY = _topLeftLocY + (y - MainViewY) / CellTextureHeight;

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