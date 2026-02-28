namespace TenKingdoms;

public partial class Renderer
{
    public void DrawMonsterLairDetails(FirmMonster monsterLair)
    {
        if (!monsterLair.ShouldShowInfo())
            return;
        
        DrawOverseerPanel(DetailsX1 + 2, DetailsY1 + 96);
        if (monsterLair.MonsterKing != null && monsterLair.MonsterKing.MonsterId != 0)
        {
            MonsterInfo monsterInfo = MonsterRes[monsterLair.MonsterKing.MonsterId];
            UnitInfo unitInfo = UnitRes[monsterInfo.UnitType];
            Graphics.DrawBitmap(unitInfo.GetLargeIconTexture(Graphics, Unit.RANK_SOLDIER), DetailsX1 + 12, DetailsY1 + 104,
                unitInfo.SoldierIconWidth * 2, unitInfo.SoldierIconHeight * 2);
            PutText(FontSan, UnitMonster.MonsterKingNames[monsterLair.MonsterKing.MonsterId - 1], DetailsX1 + 116, DetailsY1 + 124);
        }
        
        DrawMonsterLairPanel(DetailsX1 + 2, DetailsY1 + 192);
        PutTextCenter(FontSan, "Ordos", DetailsX1 + 2, DetailsY1 + 210, DetailsX2 - 4, DetailsY1 + 210);
        for (int i = 0; i < monsterLair.MonsterGenerals.Count; i++)
        {
            MonsterInFirm monsterGeneral = monsterLair.MonsterGenerals[i];
            MonsterInfo monsterInfo = MonsterRes[monsterGeneral.MonsterId];
            UnitInfo unitInfo = UnitRes[monsterInfo.UnitType];
            Graphics.DrawBitmap(unitInfo.GetLargeIconTexture(Graphics, Unit.RANK_SOLDIER), DetailsX1 + 12 + (i % 3) * 131, DetailsY1 + 228 + (i / 3) * 82,
                unitInfo.SoldierIconWidth * 2, unitInfo.SoldierIconHeight * 2);
            PutText(FontSan, monsterGeneral.SoldierCount.ToString(), DetailsX1 + 112 + (i % 3) * 131, DetailsY1 + 252 + (i / 3) * 82);
        }
    }
    
    public void HandleMonsterLairDetailsInput(FirmMonster monsterLair)
    {
    }
}