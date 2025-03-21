namespace TenKingdoms;

public partial class Renderer
{
    public void ProcessInput(int eventType, int screenX, int screenY)
    {
        if (eventType == InputConstants.LeftMousePressed)
        {
            if (screenX >= MainViewX && screenX < MainViewX + MainViewWidth && screenY >= MainViewY && screenY < MainViewY + MainViewHeight)
            {
                int locX = _topLeftLocX + (screenX - MainViewX) / CellTextureWidth;
                int locY = _topLeftLocY + (screenY - MainViewY) / CellTextureHeight;

                Location location = World.get_loc(locX, locY);
                if (location.IsTown())
                {
                    _selectedFirmId = _selectedUnitId = _selectedSiteId = 0;
                    _selectedTownId = location.TownId();
                }

                if (location.IsFirm())
                {
                    _selectedTownId = _selectedUnitId = _selectedSiteId = 0;
                    _selectedFirmId = location.FirmId();
                }

                if (location.HasUnit(UnitConstants.UNIT_LAND))
                {
                    _selectedTownId = _selectedFirmId = _selectedSiteId = 0;
                    _selectedUnitId = location.UnitId(UnitConstants.UNIT_LAND);
                    UnitArray[_selectedUnitId].selected_flag = true;
                }

                if (location.HasSite())
                {
                    _selectedTownId = _selectedFirmId = _selectedUnitId = 0;
                    _selectedSiteId = location.SiteId();
                }
            }

            if (screenX >= MiniMapX && screenX < MiniMapX + MiniMapSize && screenY >= MiniMapY && screenY < MiniMapY + MiniMapSize)
            {
                int locX = screenX - MiniMapX;
                int locY = screenY - MiniMapY;
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
            if (screenX >= MainViewX && screenX < MainViewX + MainViewWidth && screenY >= MainViewY && screenY < MainViewY + MainViewHeight)
            {
                int locX = _topLeftLocX + (screenX - MainViewX) / CellTextureWidth;
                int locY = _topLeftLocY + (screenY - MainViewY) / CellTextureHeight;

                foreach (Unit unit in UnitArray)
                {
                    if (unit.selected_flag)
                    {
                        if (unit.nation_recno == 1)
                        {
                            Nation nation = NationArray[unit.nation_recno];
                            if (nation.nation_type == NationBase.NATION_OWN)
                                unit.MoveTo(locX, locY);
                        }
                    }
                }
            }
        }
    }
}