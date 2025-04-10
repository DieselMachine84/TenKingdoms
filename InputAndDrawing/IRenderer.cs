namespace TenKingdoms;

public interface IRenderer
{
    void DrawFrame();
    void ProcessInput(int eventType, int screenX, int screenY);
    void Reset();

   
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
}