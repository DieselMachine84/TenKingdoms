namespace TenKingdoms;

public partial class Renderer
{
    public void DrawCaravanDetails(UnitCaravan caravan)
    {
        DrawSmallPanel(DetailsX1 + 2, DetailsY1 + 48);
        PutTextCenter(FontSan, caravan.GetUnitName(), DetailsX1 + 2, DetailsY1 + 68, DetailsX2 - 4, DetailsY1 + 68);

        if (caravan.ShouldShowInfo())
        {
            DrawTradeStops(caravan, caravan.Stops);
            DrawUnitGoods(caravan.RawQty, caravan.ProductQty);
        }
    }

    public void HandleCaravanDetailsInput(UnitCaravan caravan)
    {
        if (caravan.IsOwn() || Config.show_ai_info)
            HandleTradeStops(caravan, caravan.Stops, caravan.IsOwn());
    }
}