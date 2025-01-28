namespace TenKingdoms;

public class MarketGoods
{
    public int raw_id;
    public int product_raw_id;
    public int input_firm_recno;

    public double stock_qty;
    public double month_demand;

    // supply from direct linked firms only. One of its uses is determining whether we have enough supply to export
    public double cur_month_supply;
    public double last_month_supply;

    public double cur_month_sale_qty;
    public double last_month_sale_qty;

    public double cur_year_sales;
    public double last_year_sales;

    private Info Info => Sys.Instance.Info;

    public double supply_30days()
    {
        return last_month_supply * (30 - Info.game_day) / 30 + cur_month_supply;
    }

    public double sale_qty_30days()
    {
        return last_month_sale_qty * (30 - Info.game_day) / 30 + cur_month_sale_qty;
    }

    public double sales_365days()
    {
        return last_year_sales * (365 - Info.year_day) / 365 + cur_year_sales;
    }
}