namespace TenKingdoms;

public partial class Renderer
{
    private bool _leftMousePressed;
    private bool _rightMousePressed;
    private int _mouseButtonX;
    private int _mouseButtonY;

    public void ProcessInput(int eventType, int screenX, int screenY)
    {
        if (eventType == InputConstants.LeftMouseDown)
        {
            _leftMousePressed = true;
            _mouseButtonX = screenX;
            _mouseButtonY = screenY;
        }
        
        if (eventType == InputConstants.LeftMouseUp)
        {
            _leftMousePressed = false;
            _mouseButtonX = screenX;
            _mouseButtonY = screenY;
            
            if (screenX >= MainViewX && screenX < MainViewX + MainViewWidth && screenY >= MainViewY && screenY < MainViewY + MainViewHeight)
            {
                int locX = _topLeftLocX + (screenX - MainViewX) / CellTextureWidth;
                int locY = _topLeftLocY + (screenY - MainViewY) / CellTextureHeight;

                Location location = World.GetLoc(locX, locY);
                if (location.IsTown())
                {
                    ResetSelection();
                    _selectedTownId = location.TownId();
                }

                if (location.IsFirm())
                {
                    ResetSelection();
                    _selectedFirmId = location.FirmId();
                }

                if (location.HasUnit(UnitConstants.UNIT_LAND))
                {
                    ResetSelection();
                    _selectedUnitId = location.UnitId(UnitConstants.UNIT_LAND);
                    UnitArray[_selectedUnitId].selected_flag = true;
                }

                if (location.HasSite())
                {
                    ResetSelection();
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

            if (screenX >= DetailsX1 && screenX <= DetailsX2 && screenY >= DetailsY1 && screenY <= DetailsY2)
            {
                if (_selectedTownId != 0)
                    HandleTownDetailsInput(TownArray[_selectedTownId]);
                
                if (_selectedFirmId != 0)
                    HandleFirmDetailsInput(FirmArray[_selectedFirmId]);
                
                if (_selectedUnitId != 0)
                    HandleUnitDetailsInput(UnitArray[_selectedUnitId]);
            }
        }

        if (eventType == InputConstants.RightMouseDown)
        {
            _rightMousePressed = true;
        }

        if (eventType == InputConstants.RightMouseUp)
        {
            _rightMousePressed = false;
            
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