namespace TenKingdoms;

public class MarketGoods
{
    public int RawId { get; set; }
    public int ProductId { get; set; }

    public double StockQty { get; set; }
    public double MonthDemand { get; set; }

    // supply from direct linked firms only. One of its uses is determining whether we have enough supply to export
    public double CurMonthSupply { get; set; }
    public double LastMonthSupply { get; set; }

    public double CurMonthSaleQty { get; set; }
    public double LastMonthSaleQty { get; set; }

    public double CurYearSales { get; set; }
    public double LastYearSales { get; set; }

    private Info Info => Sys.Instance.Info;

    public double Supply30Days()
    {
        return LastMonthSupply * (30 - Info.game_day) / 30 + CurMonthSupply;
    }

    public double SaleQty30Days()
    {
        return LastMonthSaleQty * (30 - Info.game_day) / 30 + CurMonthSaleQty;
    }

    public double Sales365Days()
    {
        return LastYearSales * (365 - Info.year_day) / 365 + CurYearSales;
    }
}