using System.Collections.Generic;

namespace TenKingdoms;

public partial class Renderer
{
    private bool _leftMousePressed;
    private bool _leftMouseReleased;
    private bool _rightMousePressed;
    private bool _rightMouseReleased;
    private int _mouseButtonX;
    private int _mouseButtonY;

    public void ProcessInput(int eventType, int screenX, int screenY)
    {
        ResetDeletedSelectedObjects();
        
        if (eventType == InputConstants.LeftMouseDown)
        {
            _leftMousePressed = true;
            _mouseButtonX = screenX;
            _mouseButtonY = screenY;
        }
        
        if (eventType == InputConstants.LeftMouseUp)
        {
            _leftMousePressed = false;
            _leftMouseReleased = true;
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
                    UnitArray[_selectedUnitId].SelectedFlag = !UnitArray[_selectedUnitId].SelectedFlag;
                }

                if (location.HasSite())
                {
                    Site site = SiteArray[location.SiteId()];
                    if (!site.HasMine)
                    {
                        ResetSelection();
                        _selectedSiteId = location.SiteId();
                    }
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

                GoToLocation(locX, locY);
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

            _leftMouseReleased = false;
        }

        if (eventType == InputConstants.RightMouseDown)
        {
            _rightMousePressed = true;
            _mouseButtonX = screenX;
            _mouseButtonY = screenY;
        }

        if (eventType == InputConstants.RightMouseUp)
        {
            _rightMousePressed = false;
            _rightMouseReleased = true;
            _mouseButtonX = screenX;
            _mouseButtonY = screenY;
            
            if (screenX >= MainViewX && screenX < MainViewX + MainViewWidth && screenY >= MainViewY && screenY < MainViewY + MainViewHeight)
            {
                int locX = _topLeftLocX + (screenX - MainViewX) / CellTextureWidth;
                int locY = _topLeftLocY + (screenY - MainViewY) / CellTextureHeight;
            }
            
            if (screenX >= DetailsX1 && screenX <= DetailsX2 && screenY >= DetailsY1 && screenY <= DetailsY2)
            {
                if (_selectedTownId != 0)
                    HandleTownDetailsInput(TownArray[_selectedTownId]);
            }

            _rightMouseReleased = false;
        }

        if (eventType == InputConstants.KeyLeftPressed)
        {
            if (_selectedSiteId != 0)
                SelectPrevOrNextSite(-1);
        }
        
        if (eventType == InputConstants.KeyRightPressed)
        {
            if (_selectedSiteId != 0)
                SelectPrevOrNextSite(1);
        }
        
        if (eventType == InputConstants.KeyDownPressed)
        {
            if (_selectedSiteId != 0)
                SelectPrevOrNextSite(-1);
        }
        
        if (eventType == InputConstants.KeyUpPressed)
        {
            if (_selectedSiteId != 0)
                SelectPrevOrNextSite(1);
        }
    }

    private void GoToLocation(int locX, int locY)
    {
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

    private void SelectPrevOrNextSite(int seekDir)
    {
        int nextSiteId = SiteArray.GetNextSite(_selectedSiteId, seekDir);
        _selectedSiteId = nextSiteId;
        Site selectedSite = SiteArray[_selectedSiteId];
        GoToLocation(selectedSite.LocX, selectedSite.LocY);
    }
}