using System.Collections.Generic;

namespace TenKingdoms;

public partial class Renderer
{
    //TODO support batch build with shift
    //TODO draw build indicator and cancel build button

    private const int BuildShipPanelX = DetailsX1 + 2;
    private const int BuildShipPanelY = DetailsY1 + 2;
    private const int BuildShipButtonNumberX = DetailsX1 + 351;
    private const int BuildShipButtonNumberY = DetailsY1 + 12;
    private const int MouseOnBuildShipButtonX1 = DetailsX1 + 8;
    private const int MouseOnBuildShipButtonX2 = DetailsX1 + 320;
    private const int MouseOnBuildShipButtonY1 = DetailsY1 + 2;
    private const int MouseOnBuildShipButtonY2 = DetailsY1 + 66;
    private const int MouseOnBuildShipNumberButtonX1 = BuildShipButtonNumberX + 4;
    private const int MouseOnBuildShipNumberButtonX2 = BuildShipButtonNumberX + 40;
    private const int MouseOnBuildShipNumberButtonY1 = BuildShipButtonNumberY + 4;
    private const int MouseOnBuildShipNumberButtonY2 = BuildShipButtonNumberY + 42;
    private const int MaxBuildShipItems = 6;
    
    public void DrawHarborDetails(FirmHarbor harbor)
    {
        if (FirmDetailsMode == FirmDetailsMode.BuildShip)
        {
            DrawBuildShipMenu(harbor);
            return;
        }

        //TODO scroll
        DrawListBox3Panel(DetailsX1 + 2, DetailsY1 + 96);

        if (_selectedShipId != 0 && !harbor.Ships.Contains(_selectedShipId))
            _selectedShipId = 0;

        if (harbor.Ships.Count > 0 && _selectedShipId == 0)
            _selectedShipId = harbor.Ships[0];
        
        int shipY = DetailsY1 + 110;
        for (int i = 0; i < harbor.Ships.Count && i < 3; i++)
        {
            Unit ship = UnitArray[harbor.Ships[i]];
            UnitInfo unitInfo = UnitRes[ship.UnitType];
            Graphics.DrawBitmap(unitInfo.GetSmallIconTexture(Graphics, ship.Rank), DetailsX1 + 14, shipY, unitInfo.SoldierIconWidth, unitInfo.SoldierIconHeight);
            PutText(FontSan, unitInfo.Name, DetailsX1 + 70, shipY + 5);
            PutText(FontSan, (int)ship.HitPoints + "/" + ship.MaxHitPoints, DetailsX1 + 280, shipY + 5);
            
            if (harbor.Ships[i] == _selectedShipId)
                DrawSelectedBorder(DetailsX1 + 8, shipY - 7, DetailsX1 + 405, shipY + 47);

            shipY += ListItemHeight;
        }

        DrawWorkersPanel(DetailsX1 + 2, DetailsY1 + 274);
        if (_selectedShipId != 0)
        {
            UnitMarine selectedShip = (UnitMarine)UnitArray[_selectedShipId];
            bool unitsAndGoodsShip = selectedShip.UnitType == UnitConstants.UNIT_CARAVEL || selectedShip.UnitType == UnitConstants.UNIT_GALLEON;
            if (selectedShip.UnitType == UnitConstants.UNIT_TRANSPORT || (unitsAndGoodsShip && FirmDetailsMode == FirmDetailsMode.Normal))
                DrawUnitsOnBoard(selectedShip);
            if (selectedShip.UnitType == UnitConstants.UNIT_VESSEL || (unitsAndGoodsShip && FirmDetailsMode == FirmDetailsMode.ShipGoods))
                DrawShipGoods(selectedShip);

            if ((harbor.OwnFirm() || Config.show_ai_info) && unitsAndGoodsShip)
            {
                bool mouseOnUnitsButton = _mouseButtonX >= DetailsX1 + 337 && _mouseButtonX <= DetailsX1 + 406 &&
                                          _mouseButtonY >= DetailsY1 + 388 && _mouseButtonY <= DetailsY1 + 409;
                if ((_leftMousePressed && mouseOnUnitsButton) || FirmDetailsMode == FirmDetailsMode.Normal)
                    DrawClearPanelDown(DetailsX1 + 335, DetailsY1 + 385);
                else
                    DrawClearPanelUp(DetailsX1 + 335, DetailsY1 + 385);
                PutText(FontSan, "Units", DetailsX1 + 344, DetailsY1 + 388, -1, true);
                
                bool mouseOnGoodsButton = _mouseButtonX >= DetailsX1 + 337 && _mouseButtonX <= DetailsX1 + 406 &&
                                          _mouseButtonY >= DetailsY1 + 418 && _mouseButtonY <= DetailsY1 + 439;
                if ((_leftMousePressed && mouseOnGoodsButton) || FirmDetailsMode == FirmDetailsMode.ShipGoods)
                    DrawClearPanelDown(DetailsX1 + 335, DetailsY1 + 415);
                else
                    DrawClearPanelUp(DetailsX1 + 335, DetailsY1 + 415);
                PutText(FontSan, "Goods", DetailsX1 + 344, DetailsY1 + 418, -1, true);
            }
        }

        if (harbor.OwnFirm())
        {
            bool mouseOnButton = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                 _mouseButtonY >= ButtonsHarborY + 2 && _mouseButtonY <= ButtonsHarborY + ButtonHeight;
            if (_leftMousePressed && mouseOnButton)
                Graphics.DrawBitmapScaled(_buttonDownTexture, Button1X, ButtonsHarborY, _buttonDownWidth, _buttonDownHeight);
            else
                Graphics.DrawBitmapScaled(_buttonUpTexture, Button1X, ButtonsHarborY, _buttonUpWidth, _buttonUpHeight);
            Graphics.DrawBitmapScaled(_buttonBuildShipTexture, Button1X + 4, ButtonsHarborY + 7, _buttonBuildShipWidth, _buttonBuildShipHeight);

            if (harbor.Ships.Count > 0)
            {
                mouseOnButton = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                                _mouseButtonY >= ButtonsHarborY + 2 && _mouseButtonY <= ButtonsHarborY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmapScaled(_buttonDownTexture, Button2X, ButtonsHarborY, _buttonDownWidth, _buttonDownHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonUpTexture, Button2X, ButtonsHarborY, _buttonUpWidth, _buttonUpHeight);
                Graphics.DrawBitmapScaled(_buttonSailShipTexture, Button2X + 6, ButtonsHarborY + 4, _buttonSailShipWidth, _buttonSailShipHeight);
            }
            else
            {
                Graphics.DrawBitmapScaled(_buttonDisabledTexture, Button2X, ButtonsHarborY, _buttonDisabledWidth, _buttonDisabledHeight);
                Graphics.DrawBitmapScaled(_buttonSailShipDisabledTexture, Button2X + 6, ButtonsHarborY + 4, _buttonSailShipWidth, _buttonSailShipHeight);
            }
        }
    }

    private void DrawUnitsOnBoard(UnitMarine selectedShip)
    {
        Unit leader = null;
        for (int i = 0; i < selectedShip.UnitsOnBoard.Count; i++)
        {
            Unit unit = UnitArray[selectedShip.UnitsOnBoard[i]];
            if (leader == null || unit.Rank > leader.Rank)
                leader = unit;
        }

        if (leader != null)
        {
            int unitX = DetailsX1 + 12;
            int unitY = DetailsY1 + 281;
            DrawUnitIcon(unitX, unitY, UnitRes[leader.UnitType], leader.Rank, leader.Skill.SkillId, 0, (int)leader.HitPoints, leader.MaxHitPoints, leader.SpyId);
        }

        int index = 0;
        for (int i = 0; i < selectedShip.UnitsOnBoard.Count; i++, index++)
        {
            Unit unit = UnitArray[selectedShip.UnitsOnBoard[i]];
            if (unit == leader)
            {
                index--;
                continue;
            }

            int unitX = DetailsX1 + 92 + 80 * (index % 4);
            int unitY = DetailsY1 + 281 + 50 * (index / 4);
            DrawUnitIcon(unitX, unitY, UnitRes[unit.UnitType], unit.Rank, unit.Skill.SkillId, 0, (int)unit.HitPoints, unit.MaxHitPoints, unit.SpyId);
        }
    }

    private void DrawShipGoods(UnitMarine selectedShip)
    {
        for (int i = 1; i <= GameConstants.MAX_RAW; i++)
        {
            RawInfo rawInfo = RawRes[i];
            DrawResourcePanelUp(DetailsX1 - 60 + i * 120, DetailsY1 + 291);
            Graphics.DrawBitmap(rawInfo.GetLargeRawTexture(Graphics), DetailsX1 - 56 + i * 120, DetailsY1 + 295,
                rawInfo.LargeRawIconWidth * 3 / 4, rawInfo.LargeRawIconHeight * 3 / 4);
            PutText(FontSan, selectedShip.RawQty[i - 1].ToString(), DetailsX1 - 20 + i * 120, DetailsY1 + 293);
        }
        for (int i = 1; i <= GameConstants.MAX_PRODUCT; i++)
        {
            RawInfo rawInfo = RawRes[i];
            DrawResourcePanelUp(DetailsX1 - 60 + i * 120, DetailsY1 + 331);
            Graphics.DrawBitmap(rawInfo.GetLargeProductTexture(Graphics), DetailsX1 - 56 + i * 120, DetailsY1 + 335,
                rawInfo.LargeProductIconWidth * 3 / 4, rawInfo.LargeProductIconHeight * 3 / 4);
            PutText(FontSan, selectedShip.ProductQty[i - 1].ToString(), DetailsX1 - 20 + i * 120, DetailsY1 + 333);
        }
    }

    private void DrawBuildShipMenu(FirmHarbor harbor)
    {
        Dictionary<int, int> buildUnitCounts = new Dictionary<int, int>();
        for (int unitType = 1; unitType <= UnitConstants.MAX_UNIT_TYPE; unitType++)
        {
            UnitInfo unitInfo = UnitRes[unitType];
            if (unitInfo.UnitClass == UnitConstants.UNIT_CLASS_SHIP)
                buildUnitCounts.Add(unitType, 0);
        }

        foreach (int buildUnitType in harbor.BuildQueue)
        {
            buildUnitCounts[buildUnitType]++;
        }

        if (harbor.BuildUnitType != 0)
        {
            buildUnitCounts[harbor.BuildUnitType]++;
        }

        Nation harborNation = NationArray[harbor.NationId];
        int shownItems = 0;
        int dy = 0;
        for (int unitType = 1; unitType <= UnitConstants.MAX_UNIT_TYPE + 1; unitType++)
        {
            bool showCancelButton = (shownItems == MaxBuildShipItems || unitType > UnitConstants.MAX_UNIT_TYPE);

            int correctedUnitType = unitType;
            if (unitType == UnitConstants.UNIT_TRANSPORT)
                correctedUnitType = UnitConstants.UNIT_VESSEL;
            if (unitType == UnitConstants.UNIT_VESSEL)
                correctedUnitType = UnitConstants.UNIT_TRANSPORT;

            UnitInfo unitInfo = !showCancelButton ? UnitRes[correctedUnitType] : null;
            if (!showCancelButton && (unitInfo.UnitClass != UnitConstants.UNIT_CLASS_SHIP || harborNation.UnitTechLevels[correctedUnitType] == 0))
                continue;

            //TODO Done button is not pressed when you press it close to the right edge
            bool mouseOnButton = _mouseButtonX >= MouseOnBuildShipButtonX1 && _mouseButtonX <= MouseOnBuildShipButtonX2 &&
                                 _mouseButtonY >= MouseOnBuildShipButtonY1 + dy && _mouseButtonY <= MouseOnBuildShipButtonY2 + dy;
            if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
                DrawResearchBuildWeaponPanelDown(BuildShipPanelX, BuildShipPanelY + dy);
            else
                DrawResearchBuildWeaponPanelUp(BuildShipPanelX, BuildShipPanelY + dy);

            if (!showCancelButton)
            {
                Graphics.DrawBitmap(unitInfo.GetLargeIconTexture(Graphics, Unit.RANK_SOLDIER), BuildShipPanelX + 4, BuildShipPanelY + dy + 4,
                    unitInfo.SoldierIconWidth * 3 / 2, unitInfo.SoldierIconHeight * 3 / 2);
                
                PutText(FontBible, unitInfo.Name, BuildShipPanelX + 96, BuildShipPanelY + dy + 10);

                mouseOnButton = _mouseButtonX >= MouseOnBuildShipNumberButtonX1 && _mouseButtonX <= MouseOnBuildShipNumberButtonX2 &&
                                _mouseButtonY >= MouseOnBuildShipNumberButtonY1 + dy && _mouseButtonY <= MouseOnBuildShipNumberButtonY2 + dy;
                if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
                    DrawNumberPanelDown(BuildShipButtonNumberX, BuildShipButtonNumberY + dy);
                else
                    DrawNumberPanelUp(BuildShipButtonNumberX, BuildShipButtonNumberY + dy);
                PutTextCenter(FontBible, buildUnitCounts[correctedUnitType].ToString(),
                    BuildShipButtonNumberX, BuildShipButtonNumberY + dy, BuildShipButtonNumberX + 45, BuildShipButtonNumberY + 40 + dy);
                
                shownItems++;
            }
            else
            {
                PutText(FontBible, "Done", BuildShipPanelX + 158, BuildShipPanelY + dy + 10);
                break;
            }
            
            dy += 70;
        }
    }
    
    public void HandleHarborDetailsInput(FirmHarbor harbor)
    {
        if (!harbor.ShouldShowInfo())
            return;
        
        if (_selectedShipId != 0)
        {
            UnitMarine selectedShip = (UnitMarine)UnitArray[_selectedShipId];
            bool unitsAndGoodsShip = selectedShip.UnitType == UnitConstants.UNIT_CARAVEL || selectedShip.UnitType == UnitConstants.UNIT_GALLEON;

            if (unitsAndGoodsShip)
            {
                bool mouseOnUnitsButton = _mouseButtonX >= DetailsX1 + 337 && _mouseButtonX <= DetailsX1 + 406 &&
                                          _mouseButtonY >= DetailsY1 + 388 && _mouseButtonY <= DetailsY1 + 409;
                if (_leftMouseReleased && mouseOnUnitsButton && FirmDetailsMode == FirmDetailsMode.ShipGoods)
                    FirmDetailsMode = FirmDetailsMode.Normal;
                
                bool mouseOnGoodsButton = _mouseButtonX >= DetailsX1 + 337 && _mouseButtonX <= DetailsX1 + 406 &&
                                          _mouseButtonY >= DetailsY1 + 418 && _mouseButtonY <= DetailsY1 + 439;
                if (_leftMouseReleased && mouseOnGoodsButton && FirmDetailsMode == FirmDetailsMode.Normal)
                    FirmDetailsMode = FirmDetailsMode.ShipGoods;
            }
        }
        
        if (FirmDetailsMode == FirmDetailsMode.BuildShip && harbor.OwnFirm())
        {
            HandleBuildShipMenu(harbor);
            return;
        }
        
        bool ship1Selected = _leftMouseReleased && _mouseButtonX >= DetailsX1 + 11 && _mouseButtonX <= DetailsX1 + 402 &&
                             _mouseButtonY >= DetailsY1 + 105 && _mouseButtonY <= DetailsY1 + 153;
        bool ship2Selected = _leftMouseReleased && _mouseButtonX >= DetailsX1 + 11 && _mouseButtonX <= DetailsX1 + 402 &&
                             _mouseButtonY >= DetailsY1 + 105 + ListItemHeight && _mouseButtonY <= DetailsY1 + 153 + ListItemHeight;
        bool ship3Selected = _leftMouseReleased && _mouseButtonX >= DetailsX1 + 11 && _mouseButtonX <= DetailsX1 + 402 &&
                             _mouseButtonY >= DetailsY1 + 105 + 2 * ListItemHeight && _mouseButtonY <= DetailsY1 + 153 + 2 * ListItemHeight;

        int shipIndex = 0;
        for (int i = 0; i < harbor.Ships.Count && i < 3; i++)
        {
            shipIndex++;
            if ((ship1Selected && shipIndex == 1) || (ship2Selected && shipIndex == 2) || (ship3Selected && shipIndex == 3))
            {
                _selectedShipId = harbor.Ships[i];
                break;
            }
        }
        
        if (!harbor.OwnFirm())
            return;
        
        bool mouseOnButton1 = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                              _mouseButtonY >= ButtonsHarborY + 2 && _mouseButtonY <= ButtonsHarborY + ButtonHeight;
        bool mouseOnButton2 = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                              _mouseButtonY >= ButtonsHarborY + 2 && _mouseButtonY <= ButtonsHarborY + ButtonHeight;

        if (_leftMouseReleased && mouseOnButton1)
        {
            FirmDetailsMode = FirmDetailsMode.BuildShip;
        }

        if (_leftMouseReleased && mouseOnButton2 && _selectedShipId != 0)
        {
            harbor.SailShip(_selectedShipId, InternalConstants.COMMAND_PLAYER);
        }
    }

    private void HandleBuildShipMenu(FirmHarbor harbor)
    {
        Nation harborNation = NationArray[harbor.NationId];
        int shownItems = 0;
        int dy = 0;
        for (int unitType = 1; unitType <= UnitConstants.MAX_UNIT_TYPE + 1; unitType++)
        {
            bool onCancelButton = (shownItems == MaxBuildShipItems || unitType > UnitConstants.MAX_UNIT_TYPE);
            
            int correctedUnitType = unitType;
            if (unitType == UnitConstants.UNIT_TRANSPORT)
                correctedUnitType = UnitConstants.UNIT_VESSEL;
            if (unitType == UnitConstants.UNIT_VESSEL)
                correctedUnitType = UnitConstants.UNIT_TRANSPORT;
            
            UnitInfo unitInfo = !onCancelButton ? UnitRes[correctedUnitType] : null;
            if (!onCancelButton && (unitInfo.UnitClass != UnitConstants.UNIT_CLASS_SHIP || harborNation.UnitTechLevels[correctedUnitType] == 0))
                continue;

            bool mouseOnBuildButton = _mouseButtonX >= MouseOnBuildShipButtonX1 && _mouseButtonX <= MouseOnBuildShipButtonX2 &&
                                      _mouseButtonY >= MouseOnBuildShipButtonY1 + dy && _mouseButtonY <= MouseOnBuildShipButtonY2 + dy;
            bool mouseOnBuildNumberButton = _mouseButtonX >= MouseOnBuildShipNumberButtonX1 && _mouseButtonX <= MouseOnBuildShipNumberButtonX2 &&
                                            _mouseButtonY >= MouseOnBuildShipNumberButtonY1 + dy && _mouseButtonY <= MouseOnBuildShipNumberButtonY2 + dy;
            
            if ((mouseOnBuildButton || mouseOnBuildNumberButton) && (_leftMouseReleased || _rightMouseReleased))
            {
                if (!onCancelButton)
                {
                    if (_leftMouseReleased)
                    {
                        /*if (remote.is_enable())
                        {
					        // packet structure : <firm recno> <unit Id> <amount>
					        short *shortPtr = (short *)remote.new_send_queue_msg(MSG_F_HARBOR_BUILD_SHIP, 3*sizeof(short) );
					        shortPtr[0] = firm_recno;
					        shortPtr[1] = unitId;
					        shortPtr[2] = createRemoveAmount;
                        }*/
                        //else
                        //{
                            harbor.AddQueue(correctedUnitType);
                        //}
                        SECtrl.immediate_sound("TURN_ON");
                    }

                    if (_rightMouseReleased)
                    {
                        /*if (remote.is_enable())
                        {
					        // packet structure : <firm recno> <unit Id> <amount>
					        short *shortPtr = (short *)remote.new_send_queue_msg(MSG_F_HARBOR_BUILD_SHIP, 3*sizeof(short) );
					        shortPtr[0] = firm_recno;
					        shortPtr[1] = -unitId;
					        shortPtr[2] = createRemoveAmount;
                        }*/
                        //else
                        //{
                            harbor.RemoveQueue(correctedUnitType);
                        //}
                        SECtrl.immediate_sound("TURN_OFF");
                    }

                    if (mouseOnBuildButton)
                        FirmDetailsMode = FirmDetailsMode.Normal;
                }
                else
                {
                    if (_leftMouseReleased)
                    {
                        SECtrl.immediate_sound("TURN_OFF");
                        FirmDetailsMode = FirmDetailsMode.Normal;
                    }
                }
            }

            if (!onCancelButton)
                shownItems++;
            else
                break;
            
            dy += 70;
        }
    }
}