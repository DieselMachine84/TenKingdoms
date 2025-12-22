namespace TenKingdoms;

public partial class Renderer
{
    private int _setStopId;
    
    public void DrawCaravanDetails(UnitCaravan caravan)
    {
        DrawSmallPanel(DetailsX1 + 2, DetailsY1 + 48);
        PutTextCenter(FontSan, caravan.GetUnitName(), DetailsX1 + 2, DetailsY1 + 68, DetailsX2 - 4, DetailsY1 + 68);

        if (!Config.show_ai_info && !caravan.IsOwn())
            return;
        
        DrawTradeStops(caravan, caravan.Stops, caravan.RawQty, caravan.ProductQty);
    }

    public void HandleCaravanDetailsInput(UnitCaravan caravan)
    {
        if (!caravan.IsOwn())
            return;
        
        HandleTradeStops(caravan, caravan.Stops);
    }
}