namespace TenKingdoms;

public partial class Renderer
{
    public void DrawWeaponDetails(UnitWeapon weapon)
    {
        DrawUnitPanel(DetailsX1 + 2, DetailsY1 + 48);
        
        UnitInfo unitInfo = UnitRes[weapon.UnitType];
        Graphics.DrawBitmap(unitInfo.GetLargeIconTexture(Graphics, Unit.RANK_SOLDIER), DetailsX1 + 12, DetailsY1 + 56,
            unitInfo.SoldierIconWidth * 2, unitInfo.SoldierIconHeight * 2);

        PutTextCenter(FontSan, weapon.GetUnitName(), DetailsX1 + 10 + unitInfo.SoldierIconWidth * 2, DetailsY1 + 36 + unitInfo.SoldierIconHeight,
            DetailsX2 - 4, DetailsY1 + 36 + unitInfo.SoldierIconHeight * 2);

        if (weapon.IsOwn())
        {
            bool mouseOnButton1 = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                  _mouseButtonY >= ButtonsWeaponY + 2 && _mouseButtonY <= ButtonsWeaponY + ButtonHeight;
            if (_leftMousePressed && mouseOnButton1)
                Graphics.DrawBitmapScaled(_buttonDownTexture, Button1X, ButtonsWeaponY, _buttonDownWidth, _buttonDownHeight);
            else
                Graphics.DrawBitmapScaled(_buttonUpTexture, Button1X, ButtonsWeaponY, _buttonUpWidth, _buttonUpHeight);

            if (weapon.AggressiveMode)
                Graphics.DrawBitmapScaled(_buttonAggressionOnTexture, Button1X + 10, ButtonsWeaponY + 6, _buttonAggressionOnWidth, _buttonAggressionOnHeight);
            else
                Graphics.DrawBitmapScaled(_buttonAggressionOffTexture, Button1X + 10, ButtonsWeaponY + 6, _buttonAggressionOffWidth, _buttonAggressionOffHeight);

            if (IsReturnToCampEnabled(weapon))
            {
                bool mouseOnButton2 = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                                      _mouseButtonY >= ButtonsWeaponY + 2 && _mouseButtonY <= ButtonsWeaponY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton2)
                    Graphics.DrawBitmapScaled(_buttonDownTexture, Button2X, ButtonsWeaponY, _buttonDownWidth, _buttonDownHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonUpTexture, Button2X, ButtonsWeaponY, _buttonUpWidth, _buttonUpHeight);
                Graphics.DrawBitmapScaled(_buttonReturnToCampTexture, Button2X + 2, ButtonsWeaponY, _buttonReturnToCampWidth, _buttonReturnToCampHeight);
            }
        }
    }

    public void HandleWeaponDetailsInput(UnitWeapon weapon)
    {
        if (weapon.IsOwn())
        {
            bool mouseOnButton1 = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                  _mouseButtonY >= ButtonsWeaponY + 2 && _mouseButtonY <= ButtonsWeaponY + ButtonHeight;
            if (_leftMouseReleased && mouseOnButton1)
            {
                GroupChangeAggressiveMode(weapon);
            }
            
            bool mouseOnButton2 = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                                  _mouseButtonY >= ButtonsWeaponY + 2 && _mouseButtonY <= ButtonsWeaponY + ButtonHeight;
            if (_leftMouseReleased && mouseOnButton2 && IsReturnToCampEnabled(weapon))
            {
                GroupReturnCamp(weapon);
            }
        }
    }
}