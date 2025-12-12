namespace TenKingdoms;

public interface IRenderer
{
    void DrawFrame(bool nextFrame);
    void ProcessInput(int eventType, int screenX, int screenY);
    void Reset();


    void DrawTown(Town town, int layer);
    void DrawFirm(Firm firm, int layer);
    void DrawFirmDie(FirmDie firmDie, int layer);
    void DrawSite(Site site, int layer);
    void DrawUnit(Unit unit, int layer);
    void DrawBullet(Bullet bullet);
    void DrawEffect(Effect effect);
    void DrawPlant(PlantBitmap plantBitmap, int locX, int locY);
    void DrawHill(HillBlockInfo hillBlockInfo, int layer, int locX, int locY);
    void DrawFlame();
    void DrawTornado(Tornado tornado);


    void DrawMineDetails(FirmMine mine);
    void DrawFactoryDetails(FirmFactory factory);
    void DrawResearchDetails(FirmResearch research);
    void DrawWarFactoryDetails(FirmWar warFactory);
    void DrawCampDetails(FirmCamp camp);
    void DrawBaseDetails(FirmBase firmBase);
    void DrawMarketDetails(FirmMarket market);
    void DrawHarborDetails(FirmHarbor harbor);
    void DrawInnDetails(FirmInn inn);
    void DrawMonsterLairDetails(FirmMonster monsterLair);

    void DrawHumanDetails(UnitHuman unit);
    void DrawWeaponDetails(UnitWeapon weapon);
    void DrawCaravanDetails(UnitCaravan caravan);
    void DrawShipDetails(UnitMarine ship);
    void DrawGodDetails(UnitGod god);
    void DrawMonsterDetails(UnitMonster monster);

    
    void HandleMineDetailsInput(FirmMine mine);
    void HandleFactoryDetailsInput(FirmFactory factory);
    void HandleResearchDetailsInput(FirmResearch research);
    void HandleWarFactoryDetailsInput(FirmWar warFactory);
    void HandleCampDetailsInput(FirmCamp camp);
    void HandleBaseDetailsInput(FirmBase firmBase);
    void HandleMarketDetailsInput(FirmMarket market);
    void HandleHarborDetailsInput(FirmHarbor harbor);
    void HandleInnDetailsInput(FirmInn inn);
    void HandleMonsterLairDetailsInput(FirmMonster monsterLair);
    
    void HandleHumanDetailsInput(UnitHuman unit);
    void HandleWeaponDetailsInput(UnitWeapon weapon);
    void HandleCaravanDetailsInput(UnitCaravan caravan);
    void HandleShipDetailsInput(UnitMarine ship);
    void HandleGodDetailsInput(UnitGod god);
    void HandleMonsterDetailsInput(UnitMonster monster);
}