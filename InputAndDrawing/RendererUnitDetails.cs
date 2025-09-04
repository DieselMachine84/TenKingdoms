using System;

namespace TenKingdoms;

public partial class Renderer
{
    private void DrawUnitDetails(Unit unit)
    {
        DrawSmallPanel(DetailsX1 + 2, DetailsY1);
        if (unit.NationId != 0)
        {
            int textureKey = ColorRemap.GetTextureKey(ColorRemap.ColorSchemes[unit.NationId], false);
            Graphics.DrawBitmap(_colorSquareTextures[textureKey], DetailsX1 + 10, DetailsY1 + 3, _colorSquareWidth * 2, _colorSquareHeight * 2);
        }
        // TODO draw hit points bar and X button
        
        if (unit.ShouldShowInfo())
            unit.DrawDetails(this);
    }

    private void HandleUnitDetailsInput(Unit unit)
    {
        unit.HandleDetailsInput(this);
    }
}