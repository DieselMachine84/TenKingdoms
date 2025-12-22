namespace TenKingdoms;

public partial class Renderer
{
    public void DrawShipDetails(UnitMarine ship)
    {
        DrawSmallPanel(DetailsX1 + 2, DetailsY1 + 48);
        PutTextCenter(FontSan, ship.GetUnitName(), DetailsX1 + 2, DetailsY1 + 68, DetailsX2 - 4, DetailsY1 + 68);

        if (!Config.show_ai_info && !ship.IsOwn())
            return;

        UnitInfo unitInfo = UnitRes[ship.UnitType];
        if (unitInfo.carry_goods_capacity > 0)
        {
            DrawTradeStops(ship, ship.Stops, ship.RawQty, ship.ProductQty);
            return;
        }

        if (unitInfo.carry_unit_capacity > 0)
            DrawShipUnits(ship);
    }

    private void DrawShipUnits(UnitMarine ship)
    {
        int shipPanelY = DetailsY1 + 96;
        DrawShipPanel(DetailsX1 + 2, shipPanelY);
        for (int i = 0; i < ship.UnitsOnBoard.Count; i++)
        {
            int unitX = DetailsX1 + 12 + 131 * (i % 3);
            int unitY = shipPanelY + 7 + 50 * (i / 3);
            Unit unit = UnitArray[ship.UnitsOnBoard[i]];
            UnitInfo unitInfo = UnitRes[unit.UnitType];
            Graphics.DrawBitmap(unitInfo.GetSmallIconTexture(Graphics, unit.Rank), unitX, unitY,
                unitInfo.soldierSmallIconWidth * 2, unitInfo.soldierSmallIconHeight * 2);
            PutText(FontSan, unit.Skill.CombatLevel.ToString(), unitX + 52, unitY + 6);
            
            int hitBarX1 = unitX;
            int hitBarY = unitY + 41;
            int hitBarX2 = hitBarX1 + (unitInfo.soldierSmallIconWidth * 2 - 1) * (int)unit.HitPoints / unit.MaxHitPoints;
            const int HIT_BAR_LIGHT_BORDER = 0;
            const int HIT_BAR_DARK_BORDER = 3;
            const int HIT_BAR_BODY = 1;
            int hitBarColor = 0xA8;
            if (unit.MaxHitPoints >= 51 && unit.MaxHitPoints <= 100)
                hitBarColor = 0xB4;
            if (unit.MaxHitPoints >= 101)
                hitBarColor = 0xAC;
            Graphics.DrawLine(hitBarX1, hitBarY, hitBarX2, hitBarY, hitBarColor + HIT_BAR_LIGHT_BORDER); //top
            Graphics.DrawLine(hitBarX1, hitBarY + 1, hitBarX2, hitBarY + 1, hitBarColor + HIT_BAR_LIGHT_BORDER); //top
            Graphics.DrawLine(hitBarX1 + 2, hitBarY + 4, hitBarX2, hitBarY + 4, hitBarColor + HIT_BAR_DARK_BORDER); //bottom
            Graphics.DrawLine(hitBarX1 + 2, hitBarY + 5, hitBarX2, hitBarY + 5, hitBarColor + HIT_BAR_DARK_BORDER); //bottom
            Graphics.DrawLine(hitBarX1, hitBarY, hitBarX1, hitBarY + 5, hitBarColor + HIT_BAR_LIGHT_BORDER); //left
            Graphics.DrawLine(hitBarX1 + 1, hitBarY, hitBarX1 + 1, hitBarY + 5, hitBarColor + HIT_BAR_LIGHT_BORDER); //left
            Graphics.DrawLine(hitBarX2 - 1, hitBarY + 2, hitBarX2 - 1, hitBarY + 3, hitBarColor + HIT_BAR_DARK_BORDER); //right
            Graphics.DrawLine(hitBarX2, hitBarY + 2, hitBarX2, hitBarY + 3, hitBarColor + HIT_BAR_DARK_BORDER); //right
            Graphics.DrawLine(hitBarX1 + 2, hitBarY + 2, hitBarX2 - 2, hitBarY + 2, hitBarColor + HIT_BAR_BODY); //body
            Graphics.DrawLine(hitBarX1 + 2, hitBarY + 3, hitBarX2 - 2, hitBarY + 3, hitBarColor + HIT_BAR_BODY); //body
            
            if (unit.SpyId != 0 && (SpyArray[unit.SpyId].TrueNationId == NationArray.player_recno || Config.show_ai_info))
            {
                int spyIconX = unitX + 78;
                int spyIconY = unitY + 12;
                DrawSpyIcon(spyIconX, spyIconY, SpyArray[unit.SpyId].TrueNationId);
            }

            //TODO draw frame
            //int frameColor = (i == firm.SelectedWorkerId - 1) ? Colors.V_YELLOW : Colors.V_UP;
            //Graphics.DrawRect(workerX - 1, workerY - 1, unitInfo.soldierSmallIconWidth * 2 + 2, 3, frameColor);
            //Graphics.DrawRect(workerX - 1, workerY + unitInfo.soldierSmallIconHeight * 2 - 2, unitInfo.soldierSmallIconWidth * 2, 3, frameColor);
            //Graphics.DrawRect(workerX - 1, workerY - 1, 3, unitInfo.soldierSmallIconHeight * 2 + 2, frameColor);
            //Graphics.DrawRect(workerX + unitInfo.soldierSmallIconWidth * 2 - 2, workerY - 1, 3, unitInfo.soldierSmallIconHeight * 2 + 2, frameColor);
        }

        DrawPanelWithThreeFields(DetailsX1 + 2, DetailsY1 + 261);
        
        if (ship.IsOwn())
        {
            bool mouseOnButton = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                 _mouseButtonY >= ButtonsUnitShipY + 2 && _mouseButtonY <= ButtonsUnitShipY + ButtonHeight;
            if (_leftMousePressed && mouseOnButton)
                Graphics.DrawBitmapScaled(_buttonDownTexture, Button1X, ButtonsUnitShipY, _buttonDownWidth, _buttonDownHeight);
            else
                Graphics.DrawBitmapScaled(_buttonUpTexture, Button1X, ButtonsUnitShipY, _buttonUpWidth, _buttonUpHeight);
            
            if (ship.AggressiveMode)
                Graphics.DrawBitmapScaled(_buttonAggressionOnTexture, Button1X + 10, ButtonsUnitShipY + 6, _buttonAggressionOnWidth, _buttonAggressionOnHeight);
            else
                Graphics.DrawBitmapScaled(_buttonAggressionOffTexture, Button1X + 10, ButtonsUnitShipY + 6, _buttonAggressionOffWidth, _buttonAggressionOffHeight);
            
            if (ship.CanUnloadUnit())
            {
                mouseOnButton = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                                _mouseButtonY >= ButtonsUnitShipY + 2 && _mouseButtonY <= ButtonsUnitShipY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmapScaled(_buttonDownTexture, Button2X, ButtonsUnitShipY, _buttonDownWidth, _buttonDownHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonUpTexture, Button2X, ButtonsUnitShipY, _buttonUpWidth, _buttonUpHeight);
                Graphics.DrawBitmapScaled(_buttonOutShipTexture, Button2X + 3, ButtonsUnitShipY + 4, _buttonOutShipWidth, _buttonOutShipHeight);
            }
            else
            {
                Graphics.DrawBitmapScaled(_buttonDisabledTexture, Button2X, ButtonsUnitShipY, _buttonDisabledWidth, _buttonDisabledHeight);
                Graphics.DrawBitmapScaled(_buttonOutShipTexture, Button2X + 3, ButtonsUnitShipY + 4, _buttonOutShipWidth, _buttonOutShipHeight);
            }
        }
    }

    public void HandleShipDetailsInput(UnitMarine ship)
    {
        if (!ship.IsOwn())
            return;
        
        UnitInfo unitInfo = UnitRes[ship.UnitType];
        if (unitInfo.carry_goods_capacity > 0)
        {
            HandleTradeStops(ship, ship.Stops);
            return;
        }

        if (unitInfo.carry_unit_capacity > 0)
        {
            bool mouseOnButton1 = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                  _mouseButtonY >= ButtonsUnitShipY + 2 && _mouseButtonY <= ButtonsUnitShipY + ButtonHeight;
            bool mouseOnButton2 = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                                  _mouseButtonY >= ButtonsUnitShipY + 2 && _mouseButtonY <= ButtonsUnitShipY + ButtonHeight;

            if (_leftMouseReleased && mouseOnButton1)
            {
                bool newAggressiveMode = !ship.AggressiveMode;
                //if (remote.is_enable())
                //{
                    //// packet structure : <unit no> <new aggressive mode>
                    //short *shortPtr = (short *)remote.new_send_queue_msg(MSG_UNIT_CHANGE_AGGRESSIVE_MODE, sizeof(short)*2);
                    //*shortPtr = i;
                    //shortPtr[1] = newAggressiveMode;
                //}
                //else
                //{
                    ship.AggressiveMode = newAggressiveMode;
                //}

                SECtrl.immediate_sound(newAggressiveMode ? "TURN_ON" : "TURN_OFF");
            }
            
            if (_leftMouseReleased && mouseOnButton2 && ship.CanUnloadUnit())
            {
                ship.UnloadAllUnits(InternalConstants.COMMAND_PLAYER);
                SECtrl.immediate_sound("TURN_ON");
            }
        }
    }
}