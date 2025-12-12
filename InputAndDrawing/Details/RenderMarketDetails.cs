using System;

namespace TenKingdoms;

public partial class Renderer
{
    public void DrawMarketDetails(FirmMarket market)
    {
        bool showInfo = Config.show_ai_info || market.OwnFirm();
        if (!showInfo)
        {
            DrawPanelWithThreeFields(DetailsX1 + 2, DetailsY1 + 96);
            PutTextCenter(FontSan, "You're not permitted", DetailsX1 + 2, DetailsY1 + 120, DetailsX2 - 4, DetailsY1 + 105);
            PutTextCenter(FontSan, "to trade", DetailsX1 + 2, DetailsY1 + 150, DetailsX2 - 4, DetailsY1 + 135);
            PutTextCenter(FontSan, "with this market.", DetailsX1 + 2, DetailsY1 + 180, DetailsX2 - 4, DetailsY1 + 165);
            return;
        }

        int dy = 0;
        for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
        {
            MarketGoods marketGoods = market.MarketGoods[i];
            
            DrawPanelWithThreeFields(DetailsX1 + 2, DetailsY1 + 96 + dy);

            if (marketGoods.RawId != 0)
            {
                RawInfo rawInfo = RawRes[marketGoods.RawId];
                Graphics.DrawBitmap(rawInfo.GetLargeRawTexture(Graphics), DetailsX1 + 12, DetailsY1 + 102 + dy,
                    rawInfo.LargeRawIconWidth * 3 / 4, rawInfo.LargeRawIconHeight * 3 / 4);
                PutText(FontSan, rawInfo.Name, DetailsX1 + 42, DetailsY1 + 98 + dy);
            }

            if (marketGoods.ProductId != 0)
            {
                RawInfo rawInfo = RawRes[marketGoods.ProductId];
                Graphics.DrawBitmap(rawInfo.GetLargeProductTexture(Graphics), DetailsX1 + 12, DetailsY1 + 102 + dy,
                    rawInfo.LargeProductIconWidth * 3 / 4, rawInfo.LargeProductIconHeight * 3 / 4);
                PutText(FontSan, rawInfo.Name + " Products", DetailsX1 + 42, DetailsY1 + 98 + dy);
            }

            if (marketGoods.RawId != 0 || marketGoods.ProductId != 0)
            {
                if (market.OwnFirm())
                {
                    bool mouseOnClearButton = _mouseButtonX >= DetailsX1 + 330 && _mouseButtonX <= DetailsX1 + 399 &&
                                              _mouseButtonY >= DetailsY1 + 105 + dy && _mouseButtonY <= DetailsY1 + 126 + dy;
                    if (_leftMousePressed && mouseOnClearButton)
                        DrawClearPanelDown(DetailsX1 + 328, DetailsY1 + 102 + dy);
                    else
                        DrawClearPanelUp(DetailsX1 + 328, DetailsY1 + 102 + dy);
                    PutText(FontSan, "Clear", DetailsX1 + 344, DetailsY1 + 105 + dy, -1, true);
                }

                DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 130 + dy);
                PutText(FontSan, "Sales", DetailsX1 + 13, DetailsY1 + 133 + dy, -1, true);
                PutText(FontSan, "$" + (int)marketGoods.Sales365Days(), DetailsX1 + 113, DetailsY1 + 135 + dy, -1, true);
                DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 159 + dy);
                PutText(FontSan, "Stock", DetailsX1 + 13, DetailsY1 + 162 + dy, -1, true);
                PutText(FontSan, (int)marketGoods.StockQty + "/" + (int)market.MaxStockQty, DetailsX1 + 113, DetailsY1 + 164 + dy, -1, true);
                DrawFieldPanel67(DetailsX1 + 208, DetailsY1 + 159 + dy);
                PutText(FontSan, "Demand", DetailsX1 + 214, DetailsY1 + 162 + dy, -1, true);
                PutText(FontSan, ((int)marketGoods.MonthDemand).ToString(), DetailsX1 + 314, DetailsY1 + 164 + dy, -1, true);
            }
            
            dy += 97;
        }

        if (market.OwnFirm())
        {
            if (market.CanHireCaravan())
            {
                bool mouseOnButton = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                     _mouseButtonY >= ButtonsMarketY + 2 && _mouseButtonY <= ButtonsMarketY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmapScaled(_buttonDownTexture, Button1X, ButtonsMarketY, _buttonDownWidth, _buttonDownHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonUpTexture, Button1X, ButtonsMarketY, _buttonUpWidth, _buttonUpHeight);
                Graphics.DrawBitmapScaled(_buttonHireCaravanTexture, Button1X + 8, ButtonsMarketY + 8, _buttonHireCaravanWidth, _buttonHireCaravanHeight);
            }
            else
            {
                Graphics.DrawBitmapScaled(_buttonDisabledTexture, Button1X, ButtonsTownY, _buttonDisabledWidth, _buttonDisabledHeight);
                Graphics.DrawBitmapScaled(_buttonHireCaravanTexture, Button1X + 8, ButtonsMarketY + 8, _buttonHireCaravanWidth, _buttonHireCaravanHeight);
            }
        }

        string restockType = market.RestockType switch
        {
            0 => "Any",
            1 => "Factory",
            2 => "Mine",
            3 => "None",
            _ => ""
        };
        DrawRestockPanel(DetailsX1 + 83, DetailsY1 + 387);
        DrawFieldPanel89(DetailsX1 + 88, DetailsY1 + 392);
        PutText(FontSan, "Yearly Income", DetailsX1 + 94, DetailsY1 + 395, -1, true);
        PutText(FontSan, "$" + (int)market.Income365Days(), DetailsX1 + 227, DetailsY1 + 397, -1, true);
        DrawFieldPanel89(DetailsX1 + 88, DetailsY1 + 421);
        PutText(FontSan, "Restock From", DetailsX1 + 94, DetailsY1 + 424, -1, true);
        PutText(FontSan, restockType, DetailsX1 + 227, DetailsY1 + 426, -1, true);
        
        if (market.OwnFirm())
        {
            bool mouseOnSwitchButton = _mouseButtonX >= DetailsX1 + 330 && _mouseButtonX <= DetailsX1 + 399 &&
                                       _mouseButtonY >= DetailsY1 + 424 && _mouseButtonY <= DetailsY1 + 445;
            if (_leftMousePressed && mouseOnSwitchButton)
                DrawClearPanelDown(DetailsX1 + 328, DetailsY1 + 421);
            else
                DrawClearPanelUp(DetailsX1 + 328, DetailsY1 + 421);
            PutText(FontSan, "Switch", DetailsX1 + 337, DetailsY1 + 424, -1, true);
        }
    }
    
    public void HandleMarketDetailsInput(FirmMarket market)
    {
    }
}