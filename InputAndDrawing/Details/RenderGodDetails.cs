using System;

namespace TenKingdoms;

public partial class Renderer
{
    public void DrawGodDetails(UnitGod god)
    {
        DrawUnitPanel(DetailsX1 + 2, DetailsY1 + 48);
        
        UnitInfo unitInfo = UnitRes[god.UnitType];
        Graphics.DrawBitmap(unitInfo.GetLargeIconTexture(Graphics, Unit.RANK_SOLDIER), DetailsX1 + 12, DetailsY1 + 56,
            unitInfo.SoldierIconWidth * 2, unitInfo.SoldierIconHeight * 2);

        PutTextCenter(FontSan, god.GetUnitName(), DetailsX1 + 10 + unitInfo.SoldierIconWidth * 2, DetailsY1 + 36 + unitInfo.SoldierIconHeight,
            DetailsX2 - 4, DetailsY1 + 36 + unitInfo.SoldierIconHeight * 2);

        if (god.IsOwn() && GodRes[god.GodId].CanCastPower)
        {
            IntPtr buttonGodTexture = god.GodId switch
            {
                GodRes.GOD_PERSIAN => _buttonPersianGodTexture,
                GodRes.GOD_JAPANESE => _buttonJapaneseGodTexture,
                GodRes.GOD_MAYA => _buttonMayaGodTexture,
                GodRes.GOD_VIKING => _buttonVikingGodRainTexture,
                GodRes.GOD_EGYPTIAN => _buttonEgyptianGodTexture,
                GodRes.GOD_INDIAN => _buttonIndianGodTexture,
                GodRes.GOD_ZULU => _buttonZuluGodTexture,
                _ => IntPtr.Zero
            };
            IntPtr buttonGodDisabledTexture = god.GodId switch
            {
                GodRes.GOD_PERSIAN => _buttonPersianGodDisabledTexture,
                GodRes.GOD_JAPANESE => _buttonJapaneseGodDisabledTexture,
                GodRes.GOD_MAYA => _buttonMayaGodDisabledTexture,
                GodRes.GOD_VIKING => _buttonVikingGodRainDisabledTexture,
                GodRes.GOD_EGYPTIAN => _buttonEgyptianGodDisabledTexture,
                GodRes.GOD_INDIAN => _buttonIndianGodDisabledTexture,
                GodRes.GOD_ZULU => _buttonZuluGodDisabledTexture,
                _ => IntPtr.Zero
            };
            int buttonGodWidth = god.GodId switch
            {
                GodRes.GOD_PERSIAN => _buttonPersianGodWidth,
                GodRes.GOD_JAPANESE => _buttonJapaneseGodWidth,
                GodRes.GOD_MAYA => _buttonMayaGodWidth,
                GodRes.GOD_VIKING => _buttonVikingGodRainWidth,
                GodRes.GOD_EGYPTIAN => _buttonEgyptianGodWidth,
                GodRes.GOD_INDIAN => _buttonIndianGodWidth,
                GodRes.GOD_ZULU => _buttonZuluGodWidth,
                _ => 0
            };
            int buttonGodHeight = god.GodId switch
            {
                GodRes.GOD_PERSIAN => _buttonPersianGodHeight,
                GodRes.GOD_JAPANESE => _buttonJapaneseGodHeight,
                GodRes.GOD_MAYA => _buttonMayaGodHeight,
                GodRes.GOD_VIKING => _buttonVikingGodRainHeight,
                GodRes.GOD_EGYPTIAN => _buttonEgyptianGodHeight,
                GodRes.GOD_INDIAN => _buttonIndianGodHeight,
                GodRes.GOD_ZULU => _buttonZuluGodHeight,
                _ => 0
            };

            if (god.HitPoints >= GodRes[god.GodId].PowerPrayPoints)
            {
                bool mouseOnButton1 = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                      _mouseButtonY >= ButtonsGodY + 2 && _mouseButtonY <= ButtonsGodY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton1)
                    Graphics.DrawBitmapScaled(_buttonDownTexture, Button1X, ButtonsGodY, _buttonDownWidth, _buttonDownHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonUpTexture, Button1X, ButtonsGodY, _buttonUpWidth, _buttonUpHeight);
                Graphics.DrawBitmapScaled(buttonGodTexture, Button1X + 4, ButtonsGodY + 4, buttonGodWidth, buttonGodHeight);

                if (god.GodId == GodRes.GOD_VIKING)
                {
                    bool mouseOnButton2 = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                                          _mouseButtonY >= ButtonsGodY + 2 && _mouseButtonY <= ButtonsGodY + ButtonHeight;
                    if (_leftMousePressed && mouseOnButton2)
                        Graphics.DrawBitmapScaled(_buttonDownTexture, Button2X, ButtonsGodY, _buttonDownWidth, _buttonDownHeight);
                    else
                        Graphics.DrawBitmapScaled(_buttonUpTexture, Button2X, ButtonsGodY, _buttonUpWidth, _buttonUpHeight);
                    Graphics.DrawBitmapScaled(_buttonVikingGodTornadoTexture, Button2X + 4, ButtonsGodY + 4, _buttonVikingGodTornadoWidth, _buttonVikingGodTornadoHeight);
                }
            }
            else
            {
                Graphics.DrawBitmapScaled(_buttonDisabledTexture, Button1X, ButtonsGodY, _buttonDisabledWidth, _buttonDisabledHeight);
                Graphics.DrawBitmapScaled(buttonGodDisabledTexture, Button1X + 4, ButtonsGodY + 4, buttonGodWidth, buttonGodHeight);

                if (god.GodId == GodRes.GOD_VIKING)
                {
                    Graphics.DrawBitmapScaled(_buttonDisabledTexture, Button2X, ButtonsGodY, _buttonDisabledWidth, _buttonDisabledHeight);
                    Graphics.DrawBitmapScaled(_buttonVikingGodTornadoDisabledTexture, Button2X + 4, ButtonsGodY + 4, _buttonVikingGodTornadoWidth, _buttonVikingGodTornadoHeight);
                }
            }
        }
    }

    public void HandleGodDetailsInput(UnitGod god)
    {
        if (god.IsOwn() && GodRes[god.GodId].CanCastPower && god.HitPoints >= GodRes[god.GodId].PowerPrayPoints)
        {
            bool mouseOnButton1 = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                  _mouseButtonY >= ButtonsGodY + 2 && _mouseButtonY <= ButtonsGodY + ButtonHeight;
            bool mouseOnButton2 = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                                  _mouseButtonY >= ButtonsGodY + 2 && _mouseButtonY <= ButtonsGodY + ButtonHeight;

            if (_leftMouseReleased && mouseOnButton1)
            {
                if (god.GodId == GodRes.GOD_VIKING)
                {
                    god.GoCastPower(god.NextLocX, god.NextLocY, 2, InternalConstants.COMMAND_PLAYER);
                }
                else
                {
                    UnitDetailsMode = UnitDetailsMode.GodCastPower;
                }
            }

            if (_leftMouseReleased && mouseOnButton2)
            {
                if (god.GodId == GodRes.GOD_VIKING)
                {
                    UnitDetailsMode = UnitDetailsMode.GodCastPower;
                }
            }
        }
    }
    
    private void CancelGodCastPower()
    {
        if (UnitDetailsMode == UnitDetailsMode.GodCastPower)
            UnitDetailsMode = UnitDetailsMode.Normal;
    }
}