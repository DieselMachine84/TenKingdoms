using System;
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

    public void ProcessInput(int eventType, int mouseEventX, int mouseEventY)
    {
        bool clickOnMainView = mouseEventX >= MainViewX && mouseEventX < MainViewX + MainViewWidth &&
                               mouseEventY >= MainViewY && mouseEventY < MainViewY + MainViewHeight;
        bool clickOnMiniMap = mouseEventX >= MiniMapX && mouseEventX < MiniMapX + MiniMapSize &&
                              mouseEventY >= MiniMapY && mouseEventY < MiniMapY + MiniMapSize;
        bool clickOnDetails = mouseEventX >= DetailsX1 && mouseEventX <= DetailsX2 &&
                              mouseEventY >= DetailsY1 && mouseEventY <= DetailsY2;

        ResetDeletedSelectedObjects();
        
        if (eventType == InputConstants.LeftMouseDown)
        {
            _leftMousePressed = true;
            _mouseButtonX = mouseEventX;
            _mouseButtonY = mouseEventY;
            if (clickOnMainView)
                ProcessLeftMouseAction();
        }
        
        if (eventType == InputConstants.LeftMouseUp)
        {
            int oldMouseButtonX = _mouseButtonX;
            int oldMouseButtonY = _mouseButtonY;
            
            _leftMousePressed = false;
            _leftMouseReleased = true;
            _mouseButtonX = mouseEventX;
            _mouseButtonY = mouseEventY;

            if (!SelectObjects(oldMouseButtonX, oldMouseButtonY, mouseEventX, mouseEventY))
            {
                if (clickOnMiniMap)
                    HandleMiniMap();

                if (clickOnDetails)
                    HandleDetails();
            }

            _leftMouseReleased = false;
        }

        if (eventType == InputConstants.RightMouseDown)
        {
            _rightMousePressed = true;
            _mouseButtonX = mouseEventX;
            _mouseButtonY = mouseEventY;
        }

        if (eventType == InputConstants.RightMouseUp)
        {
            _rightMousePressed = false;
            _rightMouseReleased = true;
            _mouseButtonX = mouseEventX;
            _mouseButtonY = mouseEventY;
            
            // TODO add waypoint

            if (clickOnMainView)
            {
                (int locX, int locY) = GetMainViewLocation(_mouseButtonX, _mouseButtonY);
                ProcessRightMouseAction(locX, locY, false);
            }

            if (clickOnMiniMap)
            {
                (int locX, int locY) = GetMiniMapLocation(_mouseButtonX, _mouseButtonY);
                ProcessRightMouseAction(locX, locY, true);
            }
            
            if (clickOnDetails)
            {
                if (_selectedTownId != 0)
                    HandleTownDetailsInput(TownArray[_selectedTownId]);
                
                if (_selectedFirmId != 0)
                    HandleFirmDetailsInput(FirmArray[_selectedFirmId]);
            }

            CancelSettleAndBuild();
            CancelSetStop();
            SelectMouseCursor();
            // TODO cancel ship stop and cast power

            _rightMouseReleased = false;
        }

        if (eventType == InputConstants.MouseMotion)
        {
            _mouseMotionX = mouseEventX;
            _mouseMotionY = mouseEventY;
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

    public void Scroll(bool scrollUp, bool scrollDown, bool scrollLeft, bool scrollRight)
    {
        if (scrollUp && _topLeftLocX > 0)
            _topLeftLocX--;
        if (scrollDown && _topLeftLocX < GameConstants.MapSize - MainViewWidthInCells)
            _topLeftLocX++;
        if (scrollLeft && _topLeftLocY > 0)
            _topLeftLocY--;
        if (scrollRight && _topLeftLocY < GameConstants.MapSize - MainViewHeightInCells)
            _topLeftLocY++;
    }

    public void GoToLocation(int locX, int locY)
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

    private bool SelectObjects(int mouse1X, int mouse1Y, int mouse2X, int mouse2Y)
    {
        // TODO recallGroup
        // TODO shiftSelect
        // TODO selection sound

        if (mouse1X < MainViewX || mouse1X >= MainViewX + MainViewWidth)
            return false;
        
        if (mouse1Y < MainViewY || mouse1Y >= MainViewY + MainViewHeight)
            return false;
        
        mouse2X = Math.Max(mouse2X, MainViewX);
        mouse2X = Math.Min(mouse2X, MainViewX + MainViewWidth - 1);
        mouse2Y = Math.Max(mouse2Y, MainViewY);
        mouse2Y = Math.Min(mouse2Y, MainViewY + MainViewHeight - 1);
        (int selectLocX1, int selectLocY1) = GetMainViewLocation(Math.Min(mouse1X, mouse2X), Math.Min(mouse1Y, mouse2Y));
        (int selectLocX2, int selectLocY2) = GetMainViewLocation(Math.Max(mouse1X, mouse2X), Math.Max(mouse1Y, mouse2Y));

        bool selectOneOnly = Math.Abs(mouse1X - mouse2X) <= 3 && Math.Abs(mouse1Y - mouse2Y) <= 3;
        if (selectOneOnly)
        {
            Location pointingLocation = GetPointingLocation(mouse2X, mouse2Y, out int mobileType);
            if (mobileType == UnitConstants.UNIT_LAND || mobileType == UnitConstants.UNIT_SEA || mobileType == UnitConstants.UNIT_AIR)
            {
                ResetSelection();
                _selectedUnitId = pointingLocation.UnitId(mobileType);
                _selectedUnits.Add(_selectedUnitId);
            }
            else
            {
                if (pointingLocation.IsTown())
                {
                    ResetSelection();
                    _selectedTownId = pointingLocation.TownId();
                }
                
                if (pointingLocation.IsFirm())
                {
                    ResetSelection();
                    _selectedFirmId = pointingLocation.FirmId();
                }

                if (pointingLocation.HasSite())
                {
                    Site site = SiteArray[pointingLocation.SiteId()];
                    if (!site.HasMine)
                    {
                        ResetSelection();
                        _selectedSiteId = pointingLocation.SiteId();
                    }
                }
            }
        }
        else
        {
            int[] mobileTypes = { UnitConstants.UNIT_LAND, UnitConstants.UNIT_SEA, UnitConstants.UNIT_AIR };
            List<Unit> unitsToSelect = new List<Unit>();
            for (int locY = selectLocY1; locY <= selectLocY2; locY++)
            {
                for (int locX = selectLocX1; locX <= selectLocX2; locX++)
                {
                    Location location = World.GetLoc(locX, locY);
                    for (int i = 0; i < mobileTypes.Length; i++)
                    {
                        int mobileType = mobileTypes[i];
                        if (location.HasUnit(mobileType))
                        {
                            int unitId = location.UnitId(mobileType);
                            if (!UnitArray.IsDeleted(unitId))
                            {
                                Unit unit = UnitArray[unitId];
                                if (unit.IsVisible() && !unit.IsStealth() && unit.IsOwn())
                                {
                                    //TODO update absolute position
                                    unitsToSelect.Add(unit);
                                }
                            }
                        }
                    }
                }
            }

            bool hasPlayerNationUnits = false;
            for (int i = 0; i < unitsToSelect.Count; i++)
            {
                if (unitsToSelect[i].NationId == NationArray.player_recno)
                {
                    hasPlayerNationUnits = true;
                    break;
                }
            }

            if (hasPlayerNationUnits)
            {
                for (int i = unitsToSelect.Count - 1; i >= 0; i--)
                {
                    if (unitsToSelect[i].NationId != NationArray.player_recno)
                        unitsToSelect.RemoveAt(i);
                }
            }

            bool IsUnitHigherRank(Unit unit1, Unit unit2)
            {
                if (unit1.Rank < unit2.Rank)
                    return false;

                if (unit1.Rank > unit2.Rank)
                    return true;

                if (UnitRes[unit1.UnitType].unit_class == UnitConstants.UNIT_CLASS_HUMAN &&
                    UnitRes[unit2.UnitType].unit_class != UnitConstants.UNIT_CLASS_HUMAN)
                {
                    return true;
                }

                if (unit1.Skill.SkillId != 0 && unit2.Skill.SkillId == 0)
                    return true;

                return false;
            }
            
            if (unitsToSelect.Count > 0)
                ResetSelection();
            
            for (int i = 0; i < unitsToSelect.Count; i++)
            {
                Unit unitToSelect = unitsToSelect[i];
                _selectedUnits.Add(unitToSelect.SpriteId);
                if (_selectedUnitId == 0 || IsUnitHigherRank(unitToSelect, UnitArray[_selectedUnitId]))
                    _selectedUnitId = unitToSelect.SpriteId;
            }
        }

        return true;
    }

    private void ProcessLeftMouseAction()
    {
        // TODO process caravan stop, ship stop and cast power
        
        int locX = _topLeftLocX + (_mouseButtonX - MainViewX) / CellTextureWidth;
        int locY = _topLeftLocY + (_mouseButtonY - MainViewY) / CellTextureHeight;
        Location location = World.GetLoc(locX, locY);

        if (UnitDetailsMode == UnitDetailsMode.Build)
        {
            if (!UnitArray.IsDeleted(_selectedUnitId))
            {
                Unit unit = UnitArray[_selectedUnitId];
                if (unit.IsVisible())
                {
                    unit.BuildFirm(locX, locY, _buildFirmType, InternalConstants.COMMAND_PLAYER);
                    if (SERes.mark_command_time() != 0)
                        SERes.far_sound(unit.CurLocX, unit.CurLocY, 1, 'S', unit.SpriteId, "ACK");
                }
            }
            
            CancelSettleAndBuild();
        }

        if (UnitDetailsMode == UnitDetailsMode.Settle)
        {
            if (!UnitArray.IsDeleted(_selectedUnitId))
            {
                Unit unit = UnitArray[_selectedUnitId];
                if (unit.IsVisible())
                {
                    if (location.IsTown())
                    {
                        Town town = TownArray[location.TownId()];
                        if (town.NationId == unit.NationId)
                            UnitArray.Assign(town.LocX1, town.LocY1, false, _selectedUnits, InternalConstants.COMMAND_PLAYER);
                        else
                            UnitArray.MoveTo(town.LocX1, town.LocY1, false, _selectedUnits, InternalConstants.COMMAND_PLAYER);
                    }
                    else
                    {
                        UnitArray.Settle(locX, locY, false, _selectedUnits, InternalConstants.COMMAND_PLAYER);
                    }
                    
                    if (SERes.mark_command_time() != 0)
                        SERes.far_sound(unit.CurLocX, unit.CurLocY, 1, 'S', unit.SpriteId, "ACK");
                }
            }
            
            CancelSettleAndBuild();
        }

        if (UnitDetailsMode == UnitDetailsMode.SetStop)
        {
            if (!UnitArray.IsDeleted(_selectedUnitId))
            {
                UnitCaravan caravan = (UnitCaravan)UnitArray[_selectedUnitId];
                if (location.IsFirm() && caravan.CanSetStop(location.FirmId()))
                {
                    caravan.SetStop(_setStopId, locX, locY, InternalConstants.COMMAND_PLAYER);
                    if (SERes.mark_command_time() != 0)
                        SERes.far_sound(caravan.CurLocX, caravan.CurLocY, 1, 'S', caravan.SpriteId, "ACK");
                }
            }

            CancelSetStop();
        }
        
        SelectMouseCursor();
    }

    private void ProcessRightMouseAction(int locX, int locY, bool moveOnly)
    {
        List<int> playerNationUnits = new List<int>(_selectedUnits.Count);
        List<int> otherNationUnits = new List<int>(_selectedUnits.Count);
        for (int i = 0; i < _selectedUnits.Count; i++)
        {
            int selectedUnitId = _selectedUnits[i];
            Unit selectedUnit = UnitArray[selectedUnitId];
            if (!selectedUnit.IsOwn())
                continue;
            
            if (selectedUnit.NationId == NationArray.player_recno)
                playerNationUnits.Add(selectedUnitId);
            else
                otherNationUnits.Add(selectedUnitId);
        }

        if (moveOnly)
        {
            UnitArray.MoveTo(locX, locY, false, playerNationUnits, InternalConstants.COMMAND_PLAYER);
            UnitArray.MoveTo(locX, locY, false, otherNationUnits, InternalConstants.COMMAND_PLAYER);
            return;
        }

        // TODO update absolute position
        Location location = World.GetLoc(locX, locY);
        
        Unit targetUnit = null;
        if (location.HasUnit(UnitConstants.UNIT_AIR))
            targetUnit = UnitArray[location.UnitId(UnitConstants.UNIT_AIR)];
        if (targetUnit == null && location.HasUnit(UnitConstants.UNIT_LAND))
            targetUnit = UnitArray[location.UnitId(UnitConstants.UNIT_LAND)];
        if (targetUnit == null && location.HasUnit(UnitConstants.UNIT_SEA))
            targetUnit = UnitArray[location.UnitId(UnitConstants.UNIT_SEA)];

        if (targetUnit != null && targetUnit.HitPoints > 0.0)
        {
            if (!targetUnit.IsOwn())
            {
                if (NationArray.player.get_relation_should_attack(targetUnit.NationId))
                {
                    UnitArray.Attack(targetUnit.NextLocX, targetUnit.NextLocY, false, playerNationUnits,
                        InternalConstants.COMMAND_PLAYER, targetUnit.SpriteId);
                }
                else
                {
                    UnitArray.MoveTo(targetUnit.NextLocX, targetUnit.NextLocY, false, playerNationUnits, InternalConstants.COMMAND_PLAYER);
                }
                UnitArray.MoveTo(targetUnit.NextLocX, targetUnit.NextLocY, false, otherNationUnits, InternalConstants.COMMAND_PLAYER);
            }
            else
            {
                switch (targetUnit.MobileType)
                {
                    case UnitConstants.UNIT_LAND:
                        //TODO only when shift is pressed
                        if (targetUnit.UnitType == UnitConstants.UNIT_EXPLOSIVE_CART)
                        {
                            UnitArray.Attack(targetUnit.NextLocX, targetUnit.NextLocY, false, playerNationUnits,
                                InternalConstants.COMMAND_PLAYER, targetUnit.SpriteId);
                        }
                        break;

                    case UnitConstants.UNIT_SEA:
                        if (UnitRes[targetUnit.UnitType].carry_unit_capacity > 0)
                            UnitArray.AssignToShip(targetUnit.NextLocX, targetUnit.NextLocY, false, playerNationUnits,
                                InternalConstants.COMMAND_PLAYER, targetUnit.SpriteId);
                        UnitArray.MoveTo(targetUnit.NextLocX, targetUnit.NextLocY, false, otherNationUnits, InternalConstants.COMMAND_PLAYER);
                        break;

                    case UnitConstants.UNIT_AIR:
                        UnitArray.MoveTo(targetUnit.NextLocX, targetUnit.NextLocY, false, playerNationUnits, InternalConstants.COMMAND_PLAYER);
                        UnitArray.MoveTo(targetUnit.NextLocX, targetUnit.NextLocY, false, otherNationUnits, InternalConstants.COMMAND_PLAYER);
                        break;
                }
            }
        }
        else
        {
            if (location.IsTown())
            {
                Town targetTown = TownArray[location.TownId()];
                if (targetTown.NationId == NationArray.player_recno)
                {
                    List<int> unitsToAssign = new List<int>(playerNationUnits.Count);
                    List<int> unitsToMove = new List<int>(playerNationUnits.Count);
                    for (int i = 0; i < playerNationUnits.Count; i++)
                    {
                        int playerUnitId = playerNationUnits[i];
                        Unit playerUnit = UnitArray[playerUnitId];
            
                        if (playerUnit is UnitHuman && playerUnit.Rank == Unit.RANK_SOLDIER && playerUnit.Skill.SkillId == 0)
                            unitsToAssign.Add(playerUnitId);
                        else
                            unitsToMove.Add(playerUnitId);
                    }
                    
                    UnitArray.Assign(targetTown.LocX1, targetTown.LocY1, false, unitsToAssign, InternalConstants.COMMAND_PLAYER);
                    UnitArray.MoveTo(targetTown.LocX1, targetTown.LocY1, false, unitsToMove, InternalConstants.COMMAND_PLAYER);
                    UnitArray.MoveTo(targetTown.LocX1, targetTown.LocY1, false, otherNationUnits, InternalConstants.COMMAND_PLAYER);
                }
                else
                {
                    if (NationArray.player.get_relation_should_attack(targetTown.NationId))
                        UnitArray.Attack(targetTown.LocX1, targetTown.LocY1, false, playerNationUnits, InternalConstants.COMMAND_PLAYER, 0);
                    else
                        UnitArray.MoveTo(targetTown.LocX1, targetTown.LocY1, false, playerNationUnits, InternalConstants.COMMAND_PLAYER);
                    
                    List<int> unitsToAssign = new List<int>(otherNationUnits.Count);
                    List<int> unitsToMove = new List<int>(otherNationUnits.Count);
                    for (int i = 0; i < otherNationUnits.Count; i++)
                    {
                        int otherUnitId = otherNationUnits[i];
                        Unit otherUnit = UnitArray[otherUnitId];
            
                        if (otherUnit.NationId == targetTown.NationId)
                            unitsToAssign.Add(otherUnitId);
                        else
                            unitsToMove.Add(otherUnitId);
                    }
                    
                    UnitArray.Assign(targetTown.LocX1, targetTown.LocY1, false, unitsToAssign, InternalConstants.COMMAND_PLAYER);
                    UnitArray.MoveTo(targetTown.LocX1, targetTown.LocY1, false, unitsToMove, InternalConstants.COMMAND_PLAYER);
                }
            }

            if (location.IsFirm())
            {
                Firm targetFirm = FirmArray[location.FirmId()];
                if (targetFirm.OwnFirm())
                {
                    List<int> unitsToAssign = new List<int>(playerNationUnits.Count);
                    List<int> unitsToMove = new List<int>(playerNationUnits.Count);
                    for (int i = 0; i < playerNationUnits.Count; i++)
                    {
                        int playerUnitId = playerNationUnits[i];
                        Unit playerUnit = UnitArray[playerUnitId];
            
                        if (CanAssignUnitToFirm(playerUnit, targetFirm))
                            unitsToAssign.Add(playerUnitId);
                        else
                            unitsToMove.Add(playerUnitId);
                    }
                    
                    UnitArray.Assign(targetFirm.LocX1, targetFirm.LocY1, false, unitsToAssign, InternalConstants.COMMAND_PLAYER);
                    UnitArray.MoveTo(targetFirm.LocX1, targetFirm.LocY1, false, unitsToMove, InternalConstants.COMMAND_PLAYER);
                    UnitArray.MoveTo(targetFirm.LocX1, targetFirm.LocY1, false, otherNationUnits, InternalConstants.COMMAND_PLAYER);
                }
                else
                {
                    if (NationArray.player.get_relation_should_attack(targetFirm.NationId))
                        UnitArray.Attack(targetFirm.LocX1, targetFirm.LocY1, false, playerNationUnits, InternalConstants.COMMAND_PLAYER, 0);
                    else
                        UnitArray.MoveTo(targetFirm.LocX1, targetFirm.LocY1, false, playerNationUnits, InternalConstants.COMMAND_PLAYER);
                    
                    List<int> unitsToAssign = new List<int>(otherNationUnits.Count);
                    List<int> unitsToMove = new List<int>(otherNationUnits.Count);
                    for (int i = 0; i < otherNationUnits.Count; i++)
                    {
                        int otherUnitId = otherNationUnits[i];
                        Unit otherUnit = UnitArray[otherUnitId];
            
                        if (otherUnit.NationId == targetFirm.NationId && CanAssignUnitToFirm(otherUnit, targetFirm))
                            unitsToAssign.Add(otherUnitId);
                        else
                            unitsToMove.Add(otherUnitId);
                    }
                    
                    UnitArray.Assign(targetFirm.LocX1, targetFirm.LocY1, false, unitsToAssign, InternalConstants.COMMAND_PLAYER);
                    UnitArray.MoveTo(targetFirm.LocX1, targetFirm.LocY1, false, unitsToMove, InternalConstants.COMMAND_PLAYER);
                }
            }

            if (!location.IsTown() && !location.IsFirm())
            {
                //TODO unit.ForceMove
                UnitArray.MoveTo(locX, locY, false, playerNationUnits, InternalConstants.COMMAND_PLAYER);
                UnitArray.MoveTo(locX, locY, false, otherNationUnits, InternalConstants.COMMAND_PLAYER);
            }
        }
    }

    private void HandleMiniMap()
    {
        (int locX, int locY) = GetMiniMapLocation(_mouseButtonX, _mouseButtonY);
        GoToLocation(locX, locY);
    }

    private void HandleDetails()
    {
        if (_selectedTownId != 0)
            HandleTownDetailsInput(TownArray[_selectedTownId]);

        if (_selectedFirmId != 0)
            HandleFirmDetailsInput(FirmArray[_selectedFirmId]);

        if (_selectedUnitId != 0)
            HandleUnitDetailsInput(UnitArray[_selectedUnitId]);
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
        _selectedUnits.Clear();
        _selectedUnits.Add(_selectedUnitId);
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
            Location pointingLocation = GetPointingLocation(_mouseMotionX, _mouseMotionY, out int mobileType);
            (ScreenObjectType pointingObjectType, int pointingObjectId) = GetPointingObjectType(pointingLocation, mobileType);
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

    private (ScreenObjectType, int) GetPointingObjectType(Location pointingLocation, int mobileType)
    {
        if (mobileType == UnitConstants.UNIT_LAND || mobileType == UnitConstants.UNIT_SEA || mobileType == UnitConstants.UNIT_AIR)
        {
            int unitId = pointingLocation.UnitId(mobileType);
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

    private Location GetPointingLocation(int pointingX, int pointingY, out int mobileType)
    {
        (int pointingLocX, int pointingLocY) = GetMainViewLocation(pointingX, pointingY);
        Location pointingLocation = World.GetLoc(pointingLocX, pointingLocY);
        Location unitLocation;

        if (HasUnitAt(pointingX, pointingY, UnitConstants.UNIT_AIR, out unitLocation))
        {
            mobileType = UnitConstants.UNIT_AIR;
            return unitLocation;
        }
        
        if (pointingLocation.IsTown() || pointingLocation.IsFirm() || pointingLocation.HasSite())
        {
            mobileType = UnitConstants.UNIT_NONE;
            return pointingLocation;
        }
        
        if (HasUnitAt(pointingX, pointingY, UnitConstants.UNIT_LAND, out unitLocation))
        {
            mobileType = UnitConstants.UNIT_LAND;
            return unitLocation;
        }
        
        if (HasUnitAt(pointingX, pointingY, UnitConstants.UNIT_SEA, out unitLocation))
        {
            mobileType = UnitConstants.UNIT_SEA;
            return unitLocation;
        }

        mobileType = UnitConstants.UNIT_NONE;
        return pointingLocation;
    }

    private bool HasUnitAt(int pointingX, int pointingY, int mobileType, out Location unitLocation)
    {
        (int pointingLocX, int pointingLocY) = GetMainViewLocation(pointingX, pointingY);
        int startLocX = Math.Max(pointingLocX - InternalConstants.DETECT_MARGIN, 0);
        int endLocX = Math.Min(pointingLocX + InternalConstants.DETECT_MARGIN, GameConstants.MapSize);
        int startLocY = Math.Max(pointingLocY - InternalConstants.DETECT_MARGIN, 0);
        int endLocY = Math.Min(pointingLocY + InternalConstants.DETECT_MARGIN, GameConstants.MapSize);

        for (int locY = startLocY; locY < endLocY; locY++)
        {
            for (int locX = startLocX; locX < endLocX; locX++)
            {
                Location location = World.GetLoc(locX, locY);
                if (location.HasUnit(mobileType))
                {
                    Unit unit = UnitArray[location.UnitId(mobileType)];
                    if (!unit.IsStealth())
                    {
                        SpriteFrame spriteFrame = unit.CurSpriteFrame(out _);
                        (int unitX, int unitY) = GetSpriteScreenXAndY(unit);
                        int unitX2 = unitX + Scale(spriteFrame.Width) - 1;
                        int unitY2 = unitY + Scale(spriteFrame.Height) - 1;
                        if (pointingX >= unitX && pointingX <= unitX2 && pointingY >= unitY && pointingY <= unitY2)
                        {
                            unitLocation = location;
                            return true;
                        }
                    }
                }
            }
        }

        unitLocation = null;
        return false;
    }

    private int ChooseCursor(ScreenObjectType selectedObjectType, int selectedId, ScreenObjectType pointingObjectType, int pointingId)
    {
        //TODO CursorType.ASSIGN
        //TODO CursorType.SHIP_STOP
        //TODO CursorType.CURSOR_ON_LINK
        
        if (UnitDetailsMode == UnitDetailsMode.Build)
            return CursorType.BUILD;

        if (UnitDetailsMode == UnitDetailsMode.Settle && NationArray.player != null)
            return CursorType.SETTLE_0 + NationArray.player.color_scheme_id;

        if (UnitDetailsMode == UnitDetailsMode.SetStop)
        {
            if (pointingObjectType == ScreenObjectType.FriendFirm || pointingObjectType == ScreenObjectType.EnemyFirm)
            {
                if (!FirmArray.IsDeleted(pointingId) && !UnitArray.IsDeleted(selectedId))
                {
                    UnitCaravan caravan = (UnitCaravan)UnitArray[selectedId];
                    if (caravan.CanSetStop(pointingId))
                        return CursorType.CARAVAN_STOP;
                }
            }

            return CursorType.CANT_CARAVAN_STOP;
        }

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
                        if (unit is UnitHuman && unit.Rank == Unit.RANK_SOLDIER && unit.Skill.SkillId == 0)
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
                            if (unit is UnitHuman && unit.Rank == Unit.RANK_SOLDIER)
                                return CursorType.ASSIGN;
                            return CursorType.NORMAL_OWN;
                        }
                        return CursorType.NORMAL_ENEMY;

                    case ScreenObjectType.FriendFirm:
                    case ScreenObjectType.EnemyFirm:
                        Firm pointingFirm = FirmArray[pointingId];
                        if (unit.NationId == pointingFirm.NationId)
                        {
                            if (CanAssignUnitToFirm(unit, pointingFirm))
                                return CursorType.ASSIGN;
                            return CursorType.NORMAL_OWN;
                        }
                        return CursorType.NORMAL_ENEMY;
                    
                    case ScreenObjectType.FriendUnit:
                    case ScreenObjectType.EnemyUnit:
                        Unit pointingUnit = UnitArray[pointingId];
                        if (unit.NationId == pointingUnit.NationId)
                        {
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
                        Town pointingTown = TownArray[pointingId];
                        for (int i = 0; i < _selectedUnits.Count; i++)
                        {
                            Unit unit = UnitArray[_selectedUnits[i]];
                            if (unit is UnitHuman && unit.Rank == Unit.RANK_SOLDIER && unit.Skill.SkillId == 0 && unit.NationId == pointingTown.NationId)
                                return CursorType.ASSIGN;
                        }
                        return CursorType.NORMAL_OWN;
                    
                    case ScreenObjectType.FriendFirm:
                        Firm pointingFirm = FirmArray[pointingId];
                        for (int i = 0; i < _selectedUnits.Count; i++)
                        {
                            Unit unit = UnitArray[_selectedUnits[i]];
                            if (unit.NationId == pointingFirm.NationId && CanAssignUnitToFirm(unit, pointingFirm))
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