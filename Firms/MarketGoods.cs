using System.IO;

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
        return LastMonthSupply * (30 - Info.GameDay) / 30 + CurMonthSupply;
    }

    public double SaleQty30Days()
    {
        return LastMonthSaleQty * (30 - Info.GameDay) / 30 + CurMonthSaleQty;
    }

    public double Sales365Days()
    {
        return LastYearSales * (365 - Info.YearDay) / 365 + CurYearSales;
    }
    
    #region SaveAndLoad

    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(RawId);
        writer.Write(ProductId);
        writer.Write(StockQty);
        writer.Write(MonthDemand);
        writer.Write(CurMonthSupply);
        writer.Write(LastMonthSupply);
        writer.Write(CurMonthSaleQty);
        writer.Write(LastMonthSaleQty);
        writer.Write(CurYearSales);
        writer.Write(LastYearSales);
    }

    public void LoadFrom(BinaryReader reader)
    {
        RawId = reader.ReadInt32();
        ProductId = reader.ReadInt32();
        StockQty = reader.ReadDouble();
        MonthDemand = reader.ReadDouble();
        CurMonthSupply = reader.ReadDouble();
        LastMonthSupply = reader.ReadDouble();
        CurMonthSaleQty = reader.ReadDouble();
        LastMonthSaleQty = reader.ReadDouble();
        CurYearSales = reader.ReadDouble();
        LastYearSales = reader.ReadDouble();
    }
	
    #endregion
}