using System;

namespace TenKingdoms;

enum UnitDetailsMode { Normal, Settle, BuildMenu, Build, SetStop }

public partial class Renderer
{
    private UnitDetailsMode UnitDetailsMode { get; set; } = UnitDetailsMode.Normal;
    
    private void DrawUnitDetails(Unit unit)
    {
        if (UnitDetailsMode != UnitDetailsMode.BuildMenu)
        {
            DrawSmallPanel(DetailsX1 + 2, DetailsY1);
            if (unit.NationId != 0)
            {
                int textureKey = ColorRemap.GetTextureKey(ColorRemap.ColorSchemes[unit.NationId], false);
                Graphics.DrawBitmap(_colorSquareTextures[textureKey], DetailsX1 + 10, DetailsY1 + 3, _colorSquareWidth * 2, _colorSquareHeight * 2);
            }
            // TODO draw hit points bar and X button
            PutTextCenter(FontSan, (int)unit.HitPoints + "/" + unit.MaxHitPoints, DetailsX1 + 2, DetailsY1 + 21, DetailsX2 - 4, DetailsY1 + 21);
        }

        if (unit.ShouldShowInfo())
            unit.DrawDetails(this);
    }

    private void DrawTradeStops(Unit unit, TradeStop[] stops, int[] rawQty, int[] productQty)
    {
        int dy = 0;
        for (int i = 0; i < UnitCaravan.MAX_STOP_FOR_CARAVAN; i++)
        {
            DrawPanelWithThreeFields(DetailsX1 + 2, DetailsY1 + 96 + dy);

            if (unit.IsOwn())
            {
                bool mouseOnSetStopButton = _mouseButtonX >= DetailsX1 + 18 && _mouseButtonX <= DetailsX1 + 123 &&
                                          _mouseButtonY >= DetailsY1 + 162 + dy && _mouseButtonY <= DetailsY1 + 183 + dy;
                if (_leftMousePressed && mouseOnSetStopButton)
                    DrawSetStopPanelDown(DetailsX1 + 16, DetailsY1 + 159 + dy);
                else
                    DrawSetStopPanelUp(DetailsX1 + 16, DetailsY1 + 159 + dy);
                PutText(FontSan, "Set Stop", DetailsX1 + 34, DetailsY1 + 162 + dy, -1, true);
            }

            TradeStop stop = stops[i];
            int firmId = stop.FirmId;
            if (firmId != 0 && !FirmArray.IsDeleted(firmId))
            {
                Firm firm = FirmArray[firmId];
                int colorScheme = ColorRemap.ColorSchemes[firm.NationId];
                int color = ColorRemap.GetColorRemap(colorScheme, false).MainColor;
                Graphics.DrawRect(DetailsX1 + 16, DetailsY1 + 102 + dy, 24, 24, color);
                PutText(FontSan, firm.FirmName(), DetailsX1 + 46, DetailsY1 + 99 + dy);
                PutText(FontSan, "Pick up", DetailsX1 + 16, DetailsY1 + 132 + dy, -1, true);
                PutText(FontSan, ":", DetailsX1 + 79, DetailsY1 + 134 + dy, -1, true);

                if (unit.IsOwn())
                {
                    bool mouseOnViewStopButton = _mouseButtonX >= DetailsX1 + 170 && _mouseButtonX <= DetailsX1 + 275 &&
                                                 _mouseButtonY >= DetailsY1 + 162 + dy && _mouseButtonY <= DetailsY1 + 183 + dy;
                    if (_leftMousePressed && mouseOnViewStopButton)
                        DrawSetStopPanelDown(DetailsX1 + 168, DetailsY1 + 159 + dy);
                    else
                        DrawSetStopPanelUp(DetailsX1 + 168, DetailsY1 + 159 + dy);
                    PutText(FontSan, "View Stop", DetailsX1 + 180, DetailsY1 + 162 + dy, -1, true);

                    bool mouseOnClearButton = _mouseButtonX >= DetailsX1 + 322 && _mouseButtonX <= DetailsX1 + 391 &&
                                              _mouseButtonY >= DetailsY1 + 162 + dy && _mouseButtonY <= DetailsY1 + 183 + dy;
                    if (_leftMousePressed && mouseOnClearButton)
                        DrawClearPanelDown(DetailsX1 + 320, DetailsY1 + 159 + dy);
                    else
                        DrawClearPanelUp(DetailsX1 + 320, DetailsY1 + 159 + dy);
                    PutText(FontSan, "Clear", DetailsX1 + 336, DetailsY1 + 162 + dy, -1, true);
                }

                int pickupGoods = 0;
                int dx = 0;
                for (int j = 1; j <= TradeStop.MAX_PICK_UP_GOODS; j++)
                {
                    int rawId = j;
                    if (rawId < 1 || rawId > GameConstants.MAX_RAW)
                        rawId = 0;
                    int productId = j - GameConstants.MAX_RAW;
                    if (productId < 1 || productId > GameConstants.MAX_PRODUCT)
                        productId = 0;

                    int stock = -1;

                    if (firm.FirmType == Firm.FIRM_MINE)
                    {
                        FirmMine mine = (FirmMine)firm;
                        if (rawId != 0 && mine.RawId == rawId)
                            stock = (int)mine.StockQty;
                    }

                    if (firm.FirmType == Firm.FIRM_FACTORY)
                    {
                        FirmFactory factory = (FirmFactory)firm;
                        if (productId != 0 && factory.ProductId == productId)
                            stock = (int)factory.StockQty;
                    }

                    if (firm.FirmType == Firm.FIRM_MARKET)
                    {
                        FirmMarket market = (FirmMarket)firm;
                        MarketGoods marketGoods = null;
                        if (rawId != 0)
                            marketGoods = market.MarketGoods[rawId - 1];

                        if (productId != 0)
                            marketGoods = market.MarketGoods[productId - 1];

                        if (marketGoods != null)
                            stock = (int)marketGoods.StockQty;
                    }

                    if ((rawId != 0 && stock > 0) || productId != 0)
                    {
                        pickupGoods++;
                        bool pressed = stop.PickUpEnabled[j - 1];
                        if (pressed)
                        {
                            DrawResourcePanelDown(DetailsX1 + 146 + dx, DetailsY1 + 126 + dy);
                            Graphics.DrawRect(DetailsX1 + 149 + dx, DetailsY1 + 129 + dy, 27, 27, Colors.V_YELLOW);
                        }
                        else
                        {
                            DrawResourcePanelUp(DetailsX1 + 146 + dx, DetailsY1 + 126 + dy);
                        }

                        if (rawId != 0)
                        {
                            RawInfo rawInfo = RawRes[rawId];
                            Graphics.DrawBitmap(rawInfo.GetLargeRawTexture(Graphics), DetailsX1 + 150 + dx + (pressed ? 1 : 0), DetailsY1 + 130 + dy,
                                rawInfo.LargeRawIconWidth * 3 / 4, rawInfo.LargeRawIconHeight * 3 / 4);
                        }

                        if (productId != 0)
                        {
                            RawInfo rawInfo = RawRes[productId];
                            Graphics.DrawBitmap(rawInfo.GetLargeProductTexture(Graphics), DetailsX1 + 150 + dx + (pressed ? 1 : 0), DetailsY1 + 130 + dy,
                                rawInfo.LargeProductIconWidth * 3 / 4, rawInfo.LargeProductIconHeight * 3 / 4);
                        }
                    }

                    dx += 36;
                }

                if (pickupGoods > 1)
                {
                    if (stop.PickUpType == TradeStop.AUTO_PICK_UP)
                    {
                        DrawResourcePanelDown(DetailsX1 + 110, DetailsY1 + 126 + dy);
                        Graphics.DrawRect(DetailsX1 + 113, DetailsY1 + 129 + dy, 27, 27, Colors.V_YELLOW);
                    }
                    else
                    {
                        DrawResourcePanelUp(DetailsX1 + 110, DetailsY1 + 126 + dy);
                    }
                    PutText(FontSan, "A", DetailsX1 + 117, DetailsY1 + 127 + dy);
                    
                    DrawResourcePanelUp(DetailsX1 + 362, DetailsY1 + 126 + dy);
                    PutText(FontSan, "N", DetailsX1 + 371, DetailsY1 + 127 + dy);
                }
            }

            dy += 97;
        }
        
        DrawPanelWithThreeFields(DetailsX1 + 2, DetailsY1 + 387);
        for (int i = 1; i <= GameConstants.MAX_RAW; i++)
        {
            RawInfo rawInfo = RawRes[i];
            DrawResourcePanelUp(DetailsX1 - 60 + i * 120, DetailsY1 + 397);
            Graphics.DrawBitmap(rawInfo.GetLargeRawTexture(Graphics), DetailsX1 - 56 + i * 120, DetailsY1 + 401,
                rawInfo.LargeRawIconWidth * 3 / 4, rawInfo.LargeRawIconHeight * 3 / 4);
            PutText(FontSan, rawQty[i - 1].ToString(), DetailsX1 - 20 + i * 120, DetailsY1 + 399);
        }
        for (int i = 1; i <= GameConstants.MAX_PRODUCT; i++)
        {
            RawInfo rawInfo = RawRes[i];
            DrawResourcePanelUp(DetailsX1 - 60 + i * 120, DetailsY1 + 437);
            Graphics.DrawBitmap(rawInfo.GetLargeProductTexture(Graphics), DetailsX1 - 56 + i * 120, DetailsY1 + 441,
                rawInfo.LargeProductIconWidth * 3 / 4, rawInfo.LargeProductIconHeight * 3 / 4);
            PutText(FontSan, productQty[i - 1].ToString(), DetailsX1 - 20 + i * 120, DetailsY1 + 439);
        }
    }
    
    private void HandleUnitDetailsInput(Unit unit)
    {
        bool colorSquareButtonPressed = _leftMouseReleased && _mouseButtonX >= DetailsX1 + 18 && _mouseButtonX <= DetailsX1 + 48 &&
                                        _mouseButtonY >= DetailsY1 + 9 && _mouseButtonY <= DetailsY1 + 32;
        if (colorSquareButtonPressed)
            GoToLocation(unit.CurLocX, unit.CurLocY);

        unit.HandleDetailsInput(this);
    }

    private void HandleTradeStops(ITrader trader, TradeStop[] stops)
    {
        int dy = 0;
        for (int i = 0; i < UnitCaravan.MAX_STOP_FOR_CARAVAN; i++)
        {
            bool mouseOnSetStopButton = _mouseButtonX >= DetailsX1 + 18 && _mouseButtonX <= DetailsX1 + 123 &&
                                        _mouseButtonY >= DetailsY1 + 162 + dy && _mouseButtonY <= DetailsY1 + 183 + dy;
            if (_leftMouseReleased && mouseOnSetStopButton)
            {
                _setStopId = i + 1;
                UnitDetailsMode = UnitDetailsMode.SetStop;
            }

            TradeStop stop = stops[i];
            int firmId = stop.FirmId;
            if (firmId != 0 && !FirmArray.IsDeleted(firmId))
            {
                Firm firm = FirmArray[firmId];
                bool mouseOnViewStopButton = _mouseButtonX >= DetailsX1 + 170 && _mouseButtonX <= DetailsX1 + 275 &&
                                             _mouseButtonY >= DetailsY1 + 162 + dy && _mouseButtonY <= DetailsY1 + 183 + dy;
                if (_leftMouseReleased && mouseOnViewStopButton)
                {
                    GoToLocation(firm.LocCenterX, firm.LocCenterY);
                }

                bool mouseOnClearButton = _mouseButtonX >= DetailsX1 + 322 && _mouseButtonX <= DetailsX1 + 391 &&
                                          _mouseButtonY >= DetailsY1 + 162 + dy && _mouseButtonY <= DetailsY1 + 183 + dy;
                if (_leftMouseReleased && mouseOnClearButton)
                {
                    trader.DelStop(i + 1, InternalConstants.COMMAND_PLAYER);
                    SECtrl.immediate_sound("TURN_OFF");
                }

                int dx = 0;
                for (int j = TradeStop.AUTO_PICK_UP; j <= TradeStop.NO_PICK_UP; j++)
                {
                    int rawId = j;
                    if (rawId < 1 || rawId > GameConstants.MAX_RAW)
                        rawId = 0;
                    int productId = j - GameConstants.MAX_RAW;
                    if (productId < 1 || productId > GameConstants.MAX_PRODUCT)
                        productId = 0;

                    int stock = -1;

                    if (firm.FirmType == Firm.FIRM_MINE)
                    {
                        FirmMine mine = (FirmMine)firm;
                        if (rawId != 0 && mine.RawId == rawId)
                            stock = (int)mine.StockQty;
                    }

                    if (firm.FirmType == Firm.FIRM_FACTORY)
                    {
                        FirmFactory factory = (FirmFactory)firm;
                        if (productId != 0 && factory.ProductId == productId)
                            stock = (int)factory.StockQty;
                    }

                    if (firm.FirmType == Firm.FIRM_MARKET)
                    {
                        FirmMarket market = (FirmMarket)firm;
                        MarketGoods marketGoods = null;
                        if (rawId != 0)
                            marketGoods = market.MarketGoods[rawId - 1];

                        if (productId != 0)
                            marketGoods = market.MarketGoods[productId - 1];

                        if (marketGoods != null)
                            stock = (int)marketGoods.StockQty;
                    }

                    if ((rawId != 0 && stock > 0) || productId != 0 || j == TradeStop.AUTO_PICK_UP || j == TradeStop.NO_PICK_UP)
                    {
                        bool mouseOnResourceButton = _mouseButtonX >= DetailsX1 + 112 + dx && _mouseButtonX <= DetailsX1 + 139 + dx &&
                                                     _mouseButtonY >= DetailsY1 + 129 + dy && _mouseButtonY <= DetailsY1 + 156 + dy;
                        if (_leftMousePressed && mouseOnResourceButton)
                        {
                            trader.SetStopPickUp(i + 1, j, InternalConstants.COMMAND_PLAYER);
                        }
                    }

                    dx += 36;
                }
            }

            dy += 97;
        }
    }
    
    private void CancelSetStop()
    {
        if (UnitDetailsMode == UnitDetailsMode.SetStop)
        {
            UnitDetailsMode = UnitDetailsMode.Normal;
            _setStopId = 0;
        }
    }
}