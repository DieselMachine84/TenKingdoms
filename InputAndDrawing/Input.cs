using System.Collections.Generic;

namespace TenKingdoms;

public partial class Renderer
{
    private int _currentCursor;
    private bool _leftMousePressed;
    private bool _leftMouseReleased;
    private bool _rightMousePressed;
    private bool _rightMouseReleased;
    private int _mouseButtonX;
    private int _mouseButtonY;
    private int _mouseMotionX;
    private int _mouseMotionY;

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
                    _selectedUnits.Clear();
                    _selectedUnits.Add(_selectedUnitId);
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

            CancelSettleAndBuild();
            SelectMouseCursor();
            // TODO cancel caravan stop, ship stop and cast power

            _rightMouseReleased = false;
        }

        if (eventType == InputConstants.MouseMotion)
        {
            _mouseMotionX = screenX;
            _mouseMotionY = screenY;
            SelectMouseCursor();
        }

        if (eventType == InputConstants.KeyLeftPressed)
        {
            if (_selectedTownId != 0)
                SelectPrevOrNextTown(-1, true);
            
            if (_selectedFirmId != 0)
                SelectPrevOrNextFirm(-1, true);
            
            if (_selectedUnitId != 0)
                SelectPrevOrNextUnit(-1, true);
            
            if (_selectedSiteId != 0)
                SelectPrevOrNextSite(-1);
        }
        
        if (eventType == InputConstants.KeyRightPressed)
        {
            if (_selectedTownId != 0)
                SelectPrevOrNextTown(1, true);

            if (_selectedFirmId != 0)
                SelectPrevOrNextFirm(1, true);

            if (_selectedUnitId != 0)
                SelectPrevOrNextUnit(1, true);

            if (_selectedSiteId != 0)
                SelectPrevOrNextSite(1);
        }
        
        if (eventType == InputConstants.KeyDownPressed)
        {
            if (_selectedTownId != 0)
                SelectPrevOrNextTown(-1, false);

            if (_selectedFirmId != 0)
                SelectPrevOrNextFirm(-1, false);

            if (_selectedUnitId != 0)
                SelectPrevOrNextUnit(-1, false);

            if (_selectedSiteId != 0)
                SelectPrevOrNextSite(-1);
        }
        
        if (eventType == InputConstants.KeyUpPressed)
        {
            if (_selectedTownId != 0)
                SelectPrevOrNextTown(1, false);

            if (_selectedFirmId != 0)
                SelectPrevOrNextFirm(1, false);
            
            if (_selectedUnitId != 0)
                SelectPrevOrNextUnit(1, false);

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

    private void SelectPrevOrNextTown(int seekDir, bool sameNation)
    {
        int nextTownId = TownArray.GetNextTown(_selectedTownId, seekDir, sameNation);
        _selectedTownId = nextTownId;
        Town selectedTown = TownArray[_selectedTownId];
        GoToLocation(selectedTown.LocCenterX, selectedTown.LocCenterY);
    }

    private void SelectPrevOrNextFirm(int seekDir, bool sameNation)
    {
        int nextFirmId = FirmArray.GetNextFirm(_selectedFirmId, seekDir, sameNation);
        _selectedFirmId = nextFirmId;
        Firm selectedFirm = FirmArray[_selectedFirmId];
        GoToLocation(selectedFirm.LocCenterX, selectedFirm.LocCenterY);
    }

    private void SelectPrevOrNextUnit(int seekDir, bool sameNation)
    {
        int nextUnitId = UnitArray.GetNextUnit(_selectedUnitId, seekDir, sameNation);
        _selectedUnitId = nextUnitId;
        Unit selectedUnit = UnitArray[_selectedUnitId];
        GoToLocation(selectedUnit.NextLocX, selectedUnit.NextLocY);
    }

    private void SelectPrevOrNextSite(int seekDir)
    {
        int nextSiteId = SiteArray.GetNextSite(_selectedSiteId, seekDir);
        _selectedSiteId = nextSiteId;
        Site selectedSite = SiteArray[_selectedSiteId];
        GoToLocation(selectedSite.LocX, selectedSite.LocY);
    }
    
    private void SelectMouseCursor()
    {
        int newCursor = CursorType.NORMAL;
        
        // TODO reset cursor to normal when in game menu or scroll menu is opened
        if (_mouseMotionX >= MainViewX && _mouseMotionX < MainViewX + MainViewWidth &&
            _mouseMotionY >= MainViewY && _mouseMotionY < MainViewY + MainViewHeight)
        {
            (ScreenObjectType selectedObjectType, int selectedObjectId) = GetSelectedObjectType();
            (ScreenObjectType pointingObjectType, int pointingObjectId) = GetPointingObjectType(GetPointingLocation());
            newCursor = ChooseCursor(selectedObjectType, selectedObjectId, pointingObjectType, pointingObjectId);
        }

        if (_currentCursor != newCursor)
        {
            _currentCursor = newCursor;
            Graphics.SetCursor(CursorRes[_currentCursor].GetCursor(Graphics));
        }
    }

    private (ScreenObjectType, int) GetSelectedObjectType()
    {
        if (_selectedTownId != 0 && !TownArray.IsDeleted(_selectedTownId))
        {
            Town town = TownArray[_selectedTownId];
            return town.NationId == NationArray.player_recno
                ? (ScreenObjectType.FriendTown, _selectedTownId)
                : (ScreenObjectType.EnemyTown, _selectedTownId);
        }
        
        if (_selectedFirmId != 0 && !FirmArray.IsDeleted(_selectedFirmId))
        {
            Firm firm = FirmArray[_selectedFirmId];
            return firm.NationId == NationArray.player_recno
                ? (ScreenObjectType.FriendFirm, _selectedFirmId)
                : (ScreenObjectType.EnemyFirm, _selectedFirmId);
        }

        if (_selectedUnitId != 0 && !UnitArray.IsDeleted(_selectedUnitId))
        {
            if (_selectedUnits.Count == 1)
            {
                Unit unit = UnitArray[_selectedUnitId];
                return unit.NationId == NationArray.player_recno
                    ? (ScreenObjectType.FriendUnit, _selectedUnitId)
                    : unit.TrueNationId() == NationArray.player_recno
                        ? (ScreenObjectType.SpyUnit, _selectedUnitId)
                        : (ScreenObjectType.EnemyUnit, _selectedUnitId);
            }
            else
            {
                return (ScreenObjectType.UnitGroup, _selectedUnitId);
            }
        }

        if (_selectedSiteId != 0 && !SiteArray.IsDeleted(_selectedSiteId))
        {
            return (ScreenObjectType.Site, _selectedSiteId);
        }

        return (ScreenObjectType.None, 0);
    }

    private (ScreenObjectType, int) GetPointingObjectType(Location pointingLocation)
    {
        if (pointingLocation.HasUnit(UnitConstants.UNIT_AIR))
        {
            int unitId = pointingLocation.UnitId(UnitConstants.UNIT_AIR);
            Unit unit = UnitArray[unitId];
            return unit.NationId == NationArray.player_recno ? (ScreenObjectType.FriendUnit, unitId) : (ScreenObjectType.EnemyUnit, unitId);
        }

        if (pointingLocation.HasUnit(UnitConstants.UNIT_LAND))
        {
            int unitId = pointingLocation.UnitId(UnitConstants.UNIT_LAND);
            Unit unit = UnitArray[unitId];
            return unit.NationId == NationArray.player_recno ? (ScreenObjectType.FriendUnit, unitId) : (ScreenObjectType.EnemyUnit, unitId);
        }
        
        if (pointingLocation.HasUnit(UnitConstants.UNIT_SEA))
        {
            int unitId = pointingLocation.UnitId(UnitConstants.UNIT_SEA);
            Unit unit = UnitArray[unitId];
            return unit.NationId == NationArray.player_recno ? (ScreenObjectType.FriendUnit, unitId) : (ScreenObjectType.EnemyUnit, unitId);
        }

        if (pointingLocation.IsTown())
        {
            int townId = pointingLocation.TownId();
            Town town = TownArray[townId];
            return town.NationId == NationArray.player_recno ? (ScreenObjectType.FriendTown, townId) : (ScreenObjectType.EnemyTown, townId);
        }

        if (pointingLocation.IsFirm())
        {
            int firmId = pointingLocation.FirmId();
            Firm firm = FirmArray[firmId];
            return firm.NationId == NationArray.player_recno ? (ScreenObjectType.FriendFirm, firmId) : (ScreenObjectType.EnemyFirm, firmId);
        }

        if (pointingLocation.HasSite())
        {
            return (ScreenObjectType.Site, pointingLocation.SiteId());
        }
        
        return (ScreenObjectType.None, 0);
    }

    private Location GetPointingLocation()
    {
        int locX = _topLeftLocX + (_mouseMotionX - MainViewX) / CellTextureWidth;
        int locY = _topLeftLocY + (_mouseMotionY - MainViewY) / CellTextureHeight;
        
        //TODO detect spread
        Location location = World.GetLoc(locX, locY);
        return location;

        //TODO update absolute position
        //if (location.HasUnit(UnitConstants.UNIT_AIR) && !UnitArray.IsDeleted(location.AirCargoId))
        //{
        //Unit unit = UnitArray[location.AirCargoId];
        //}
    }

    private int ChooseCursor(ScreenObjectType selectedObjectType, int selectedId, ScreenObjectType pointingObjectType, int pointingId)
    {
        //TODO CursorType.ASSIGN
        //TODO CursorType.CARAVAN_STOP
        //TODO CursorType.SHIP_STOP
        //TODO CursorType.CURSOR_ON_LINK
        
        if (HumanDetailsMode == HumanDetailsMode.Build)
            return CursorType.BUILD;

        if (HumanDetailsMode == HumanDetailsMode.Settle && NationArray.player != null)
            return CursorType.SETTLE_0 + NationArray.player.color_scheme_id;

        if (pointingObjectType == ScreenObjectType.None || pointingObjectType == ScreenObjectType.Site)
            return CursorType.NORMAL;

        switch (selectedObjectType)
        {
            case ScreenObjectType.None:
            case ScreenObjectType.Site:
            case ScreenObjectType.EnemyUnit:
                switch (pointingObjectType)
                {
                    case ScreenObjectType.FriendTown:
                    case ScreenObjectType.FriendFirm:
                    case ScreenObjectType.FriendUnit:
                    case ScreenObjectType.UnitGroup:
                        return CursorType.NORMAL_OWN;
                    
                    case ScreenObjectType.EnemyTown:
                    case ScreenObjectType.EnemyFirm:
                    case ScreenObjectType.EnemyUnit:
                        return CursorType.NORMAL_ENEMY;
                }
                break;
            
            case ScreenObjectType.FriendTown:
                switch (pointingObjectType)
                {
                    case ScreenObjectType.FriendTown:
                    case ScreenObjectType.FriendFirm:
                    case ScreenObjectType.FriendUnit:
                    case ScreenObjectType.UnitGroup:
                        return CursorType.NORMAL_OWN;

                    case ScreenObjectType.EnemyTown:
                    case ScreenObjectType.EnemyFirm:
                    case ScreenObjectType.EnemyUnit:
                        return CursorType.NORMAL_ENEMY;
                }

                break;

            case ScreenObjectType.EnemyTown:
                switch (pointingObjectType)
                {
                    case ScreenObjectType.FriendTown:
                    case ScreenObjectType.FriendFirm:
                    case ScreenObjectType.FriendUnit:
                    case ScreenObjectType.UnitGroup:
                        return CursorType.NORMAL_OWN;

                    case ScreenObjectType.EnemyTown:
                    case ScreenObjectType.EnemyFirm:
                    case ScreenObjectType.EnemyUnit:
                        return CursorType.NORMAL_ENEMY;
                }
                break;
            
            case ScreenObjectType.FriendFirm:
                switch (pointingObjectType)
                {
                    case ScreenObjectType.FriendTown:
                    case ScreenObjectType.FriendFirm:
                    case ScreenObjectType.FriendUnit:
                    case ScreenObjectType.UnitGroup:
                        return CursorType.NORMAL_OWN;

                    case ScreenObjectType.EnemyTown:
                    case ScreenObjectType.EnemyFirm:
                    case ScreenObjectType.EnemyUnit:
                        return CursorType.NORMAL_ENEMY;
                }
                break;
            
            case ScreenObjectType.EnemyFirm:
                switch (pointingObjectType)
                {
                    case ScreenObjectType.FriendTown:
                    case ScreenObjectType.FriendFirm:
                    case ScreenObjectType.FriendUnit:
                    case ScreenObjectType.UnitGroup:
                        return CursorType.NORMAL_OWN;

                    case ScreenObjectType.EnemyTown:
                    case ScreenObjectType.EnemyFirm:
                    case ScreenObjectType.EnemyUnit:
                        return CursorType.NORMAL_ENEMY;
                }
                break;

            case ScreenObjectType.FriendUnit:
            {
                Unit unit = UnitArray[selectedId];
                switch (pointingObjectType)
                {
                    case ScreenObjectType.FriendTown:
                        if (unit.RaceId != 0 && unit.Rank == Unit.RANK_SOLDIER && unit.Skill.SkillId == 0)
                            return CursorType.ASSIGN;
                        return CursorType.NORMAL_OWN;

                    case ScreenObjectType.FriendFirm:
                        Firm pointingFirm = FirmArray[pointingId];
                        if (CanAssignUnitToFirm(unit, pointingFirm))
                            return CursorType.ASSIGN;
                        return CursorType.NORMAL_OWN;
                    
                    case ScreenObjectType.FriendUnit:
                        //TODO assign to ship or trigger explode
                        return CursorType.NORMAL_OWN;
                    
                    case ScreenObjectType.EnemyTown:
                    case ScreenObjectType.EnemyFirm:
                    case ScreenObjectType.EnemyUnit:
                        return CursorType.NORMAL_ENEMY;
                }

                break;
            }

            case ScreenObjectType.SpyUnit:
            {
                Unit unit = UnitArray[selectedId];
                switch (pointingObjectType)
                {
                    case ScreenObjectType.FriendTown:
                    case ScreenObjectType.EnemyTown:
                        Town pointingTown = TownArray[pointingId];
                        if (unit.NationId == pointingTown.NationId)
                        {
                            if (unit.RaceId != 0 && unit.Rank == Unit.RANK_SOLDIER)
                                return CursorType.ASSIGN;
                            return CursorType.NORMAL_OWN;
                        }
                        return CursorType.NORMAL_ENEMY;

                    case ScreenObjectType.FriendFirm:
                    case ScreenObjectType.EnemyFirm:
                        Firm pointingFirm = FirmArray[pointingId];
                        if (CanAssignUnitToFirm(unit, pointingFirm))
                            return CursorType.ASSIGN;
                        if (unit.NationId == pointingFirm.NationId)
                            return CursorType.NORMAL_OWN;
                        return CursorType.NORMAL_ENEMY;
                    
                    case ScreenObjectType.FriendUnit:
                    case ScreenObjectType.EnemyUnit:
                        Unit pointingUnit = UnitArray[pointingId];
                        if (unit.NationId == pointingUnit.NationId)
                        {
                            //TODO assign to ship or trigger explode
                            return CursorType.NORMAL_OWN;
                        }
                        return CursorType.NORMAL_ENEMY;
                }

                break;
            }

            case ScreenObjectType.UnitGroup:
            {
                switch (pointingObjectType)
                {
                    case ScreenObjectType.FriendTown:
                        for (int i = 0; i < _selectedUnits.Count; i++)
                        {
                            Unit unit = UnitArray[_selectedUnits[i]];
                            if (unit.RaceId != 0 && unit.Rank == Unit.RANK_SOLDIER && unit.Skill.SkillId == 0)
                                return CursorType.ASSIGN;
                        }
                        return CursorType.NORMAL_OWN;
                    
                    case ScreenObjectType.FriendFirm:
                        Firm pointingFirm = FirmArray[pointingId];
                        for (int i = 0; i < _selectedUnits.Count; i++)
                        {
                            Unit unit = UnitArray[_selectedUnits[i]];
                            if (CanAssignUnitToFirm(unit, pointingFirm))
                                return CursorType.ASSIGN;
                        }
                        return CursorType.NORMAL_OWN;
                    
                    case ScreenObjectType.FriendUnit:
                        //TODO assign to ship or trigger explode
                        return CursorType.NORMAL_OWN;

                    case ScreenObjectType.EnemyTown:
                    case ScreenObjectType.EnemyFirm:
                    case ScreenObjectType.EnemyUnit:
                        return CursorType.NORMAL_ENEMY;
                }

                break;
            }
        }

        return CursorType.NORMAL;
    }

    private bool CanAssignUnitToFirm(Unit unit, Firm firm)
    {
        int canAssign = unit.CanAssignToFirm(firm.FirmId);
        if (canAssign == 0)
            return false;
        
        //----------------------------------------//
        // If this is a spy, then he can only be assigned to an enemy firm when there is space for the unit.
        //----------------------------------------//
        if (unit.TrueNationId() != firm.NationId)
        {
            switch (canAssign)
            {
                case 1:
                    return firm.Workers.Count < Firm.MAX_WORKER;
                case 2:
                    return firm.OverseerId == 0;
                case 3:
                    return firm.BuilderId == 0;
            }
        }

        return true;
    }
}