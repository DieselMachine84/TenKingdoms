namespace TenKingdoms;

public class ChangeMarketRestockTask : AITask
{
    private bool _shouldCancel;
    private int MarketId { get; }
    private int RestockType { get; }
    
    public ChangeMarketRestockTask(Nation nation, int marketId, int restockType) : base(nation)
    {
        MarketId = marketId;
        RestockType = restockType;
    }

    public override bool ShouldCancel()
    {
        if (_shouldCancel)
            return true;
        
        if (FirmIsDeletedOrChangedNation(MarketId))
            return true;

        return false;
    }

    public override void Process()
    {
        FirmMarket market = (FirmMarket)FirmArray[MarketId];
        if (market.UnderConstruction)
            return;
        
        while (market.RestockType != RestockType)
        {
            market.SwitchRestock();
        }

        for (int i = 0; i < market.LinkedFirms.Count; i++)
        {
            Firm linkedFirm = FirmArray[market.LinkedFirms[i]];

            if (market.IsRawMarket())
            {
                market.ToggleFirmLink(i + 1, linkedFirm.FirmType == Firm.FIRM_MINE, InternalConstants.COMMAND_AI);
                for (int j = 0; j < market.MarketGoods.Length; j++)
                {
                    if (market.MarketGoods[j].ProductId != 0)
                        market.ClearMarketGoods(j + 1);
                }
            }

            if (market.IsRetailMarket())
            {
                market.ToggleFirmLink(i + 1, linkedFirm.FirmType == Firm.FIRM_FACTORY, InternalConstants.COMMAND_AI);
                for (int j = 0; j < market.MarketGoods.Length; j++)
                {
                    if (market.MarketGoods[j].RawId != 0)
                        market.ClearMarketGoods(j + 1);
                }
            }
        }
        
        _shouldCancel = true;
    }
}