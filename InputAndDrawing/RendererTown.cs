namespace TenKingdoms;

public partial class Renderer
{
    private void DrawTownUI(Town town)
    {
        DrawPanel(DetailsX1, DetailsY1, DetailsX2, DetailsY1 + 42);
        int townNameX1 = DetailsX1;
        if (town.NationId != 0)
        {
            townNameX1 += 42;
            int colorScheme = ColorRemap.ColorSchemes[town.NationId];
            int textureKey = ColorRemap.GetTextureKey(colorScheme, false);
            Graphics.DrawBitmap(colorSquareTextures[textureKey], DetailsX1 + 6, DetailsY1 + 4,
                colorSquareWidth * 2, colorSquareHeight * 2);
        }
        PutTextCenter(FontSan, town.TownName(), townNameX1, DetailsY1, DetailsX2 - 4, DetailsY1 + 42);
    }
}