namespace TenKingdoms;

public partial class Renderer
{
    private void DrawTownDetails(Town town)
    {
        DrawSmallPanel(DetailsX1 + 2, DetailsY1);
        int townNameX1 = DetailsX1;
        if (town.NationId != 0)
        {
            townNameX1 += 8 + _colorSquareWidth * 2;
            int colorScheme = ColorRemap.ColorSchemes[town.NationId];
            int textureKey = ColorRemap.GetTextureKey(colorScheme, false);
            Graphics.DrawBitmap(_colorSquareTextures[textureKey], DetailsX1 + 10, DetailsY1 + 3, _colorSquareWidth * 2, _colorSquareHeight * 2);
        }
        PutTextCenter(FontSan, town.Name, townNameX1, DetailsY1, DetailsX2 - 4, DetailsY1 + 42);
    }
}