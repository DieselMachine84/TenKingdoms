namespace TenKingdoms;

public interface ITrader
{
    bool CanSetStop(int firmId);
    void SetStop(int stopId, int stopLocX, int stopLocY, int remoteAction);
    void DelStop(int stopId, int remoteAction);
    void SetStopPickUp(int stopId, int newPickUpType, int remoteAction);
}