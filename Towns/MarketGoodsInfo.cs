using System.Collections.Generic;

namespace TenKingdoms;

public class MarketGoodsInfo
{
    public List<FirmMarket> Markets { get; } = new List<FirmMarket>();
    public double TotalSupply { get; set; }
    public double TotalOwnSupply { get; set; }
}