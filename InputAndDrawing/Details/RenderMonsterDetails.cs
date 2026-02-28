namespace TenKingdoms;

public partial class Renderer
{
    public void DrawMonsterDetails(UnitMonster monster)
    {
        DrawUnitPanel(DetailsX1 + 2, DetailsY1 + 48);
        
        UnitInfo unitInfo = UnitRes[monster.UnitType];
        Graphics.DrawBitmap(unitInfo.GetLargeIconTexture(Graphics, Unit.RANK_SOLDIER), DetailsX1 + 12, DetailsY1 + 56,
            unitInfo.SoldierIconWidth * 2, unitInfo.SoldierIconHeight * 2);

        PutTextCenter(FontSan, monster.GetUnitName(false), DetailsX1 + 10 + unitInfo.SoldierIconWidth * 2, DetailsY1 + 36 + unitInfo.SoldierIconHeight,
            DetailsX2 - 4, DetailsY1 + 36 + unitInfo.SoldierIconHeight * 2);
    }

    public void HandleMonsterDetailsInput(UnitMonster monster)
    {
    }
}