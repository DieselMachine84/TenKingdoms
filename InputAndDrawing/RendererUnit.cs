namespace TenKingdoms;

public partial class Renderer
{
    private void DrawUnitDetails(Unit unit)
    {
        int colorScheme = ColorRemap.ColorSchemes[unit.nation_recno];
        int textureKey = ColorRemap.GetTextureKey(colorScheme, false);
        Graphics.DrawBitmap(_colorSquareTextures[textureKey], DetailsX1 + 10, DetailsY1 + 3, _colorSquareWidth * 2, _colorSquareHeight * 2);
    }
}