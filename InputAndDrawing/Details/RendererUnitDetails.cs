using System;

namespace TenKingdoms;

public partial class Renderer
{
    private void DrawUnitDetails(Unit unit)
    {
        if (UnitDetailsMode != UnitDetailsMode.BuildMenu)
        {
            DrawSmallPanel(DetailsX1 + 2, DetailsY1);
            if (unit.NationId != 0)
            {
                int textureKey = ColorRemap.GetTextureKey(ColorRemap.ColorSchemes[unit.NationId], false);
                Graphics.DrawBitmap(_colorSquareTextures[textureKey], DetailsX1 + 10, DetailsY1 + 3, _colorSquareWidth * 2, _colorSquareHeight * 2);
            }
            // TODO draw hit points bar and X button
            PutTextCenter(FontSan, (int)unit.HitPoints + "/" + unit.MaxHitPoints, DetailsX1 + 2, DetailsY1 + 21, DetailsX2 - 4, DetailsY1 + 21);
        }

        if (unit.ShouldShowInfo())
            unit.DrawDetails(this);
    }

    private void HandleUnitDetailsInput(Unit unit)
    {
        bool colorSquareButtonPressed = _leftMouseReleased && _mouseButtonX >= DetailsX1 + 18 && _mouseButtonX <= DetailsX1 + 48 &&
                                        _mouseButtonY >= DetailsY1 + 9 && _mouseButtonY <= DetailsY1 + 32;
        if (colorSquareButtonPressed)
            GoToLocation(unit.CurLocX, unit.CurLocY);

        unit.HandleDetailsInput(this);
    }
}