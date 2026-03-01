using System;

namespace TenKingdoms;

enum UnitDetailsMode { Normal, Settle, BuildMenu, Build, SetStop, ShipGoods, GodCastPower }

public partial class Renderer
{
    private UnitDetailsMode UnitDetailsMode { get; set; } = UnitDetailsMode.Normal;
    
    private void DrawUnitDetails(Unit unit)
    {
        if (UnitDetailsMode != UnitDetailsMode.BuildMenu)
        {
            DrawSmallPanel(DetailsX1 + 2, DetailsY1);
            int textureKey = ColorRemap.GetTextureKey(ColorRemap.ColorSchemes[unit.NationId], false);
            Graphics.DrawBitmap(_colorSquareTextures[textureKey], DetailsX1 + 10, DetailsY1 + 3, _colorSquareWidth * 2, _colorSquareHeight * 2);
            
            // TODO draw hit points bar
            PutTextCenter(FontSan, (int)unit.HitPoints + "/" + unit.MaxHitPoints, DetailsX1 + 2, DetailsY1 + 21, DetailsX2 - 4, DetailsY1 + 21);
            if (CanResign(unit))
            {
                bool mouseOnResignButton = _mouseButtonX >= DetailsX1 + 369 && _mouseButtonX <= DetailsX1 + 401 &&
                                           _mouseButtonY >= DetailsY1 + 3 && _mouseButtonY <= DetailsY1 + 38;
                if (_leftMousePressed && mouseOnResignButton)
                    Graphics.DrawBitmapScaled(_buttonResignDownTexture, DetailsX1 + 369, DetailsY1 + 3, _buttonResignDownWidth, _buttonResignDownHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonResignUpTexture, DetailsX1 + 369, DetailsY1 + 3, _buttonResignUpWidth, _buttonResignUpHeight);
            }
        }

        unit.DrawDetails(this);
    }

    private void DrawTradeStops(Unit unit, TradeStop[] stops)
    {
        int GetFirmStockQty(Firm firm, int rawId, int productId)
        {
            if (firm.FirmType == Firm.FIRM_MINE)
            {
                FirmMine mine = (FirmMine)firm;
                if (rawId != 0 && mine.RawId == rawId)
                    return (int)mine.StockQty;
            }

            if (firm.FirmType == Firm.FIRM_FACTORY)
            {
                FirmFactory factory = (FirmFactory)firm;
                if (productId != 0 && factory.ProductId == productId)
                    return (int)factory.StockQty;
            }

            if (firm.FirmType == Firm.FIRM_MARKET)
            {
                FirmMarket market = (FirmMarket)firm;
                foreach (MarketGoods marketGoods in market.MarketGoods)
                {
                    if ((marketGoods.RawId != 0 && marketGoods.RawId == rawId) ||
                        (marketGoods.ProductId != 0 && marketGoods.ProductId == productId))
                    {
                        return (int)marketGoods.StockQty;
                    }
                }
            }

            return -1;
        }
        
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
                DrawNationColor(ColorRemap.ColorSchemes[firm.NationId], DetailsX1 + 16, DetailsY1 + 102 + dy);
                PutText(FontSan, firm.FirmName(), DetailsX1 + 46, DetailsY1 + 99 + dy);
                PutText(FontSan, "Pick up", DetailsX1 + 16, DetailsY1 + 132 + dy, -1, true);
                PutText(FontSan, ":", DetailsX1 + 79, DetailsY1 + 134 + dy, -1, true);

                if (unit.IsOwn() || Config.ShowAIInfo)
                {
                    bool mouseOnViewStopButton = _mouseButtonX >= DetailsX1 + 170 && _mouseButtonX <= DetailsX1 + 275 &&
                                                 _mouseButtonY >= DetailsY1 + 162 + dy && _mouseButtonY <= DetailsY1 + 183 + dy;
                    if (_leftMousePressed && mouseOnViewStopButton)
                        DrawSetStopPanelDown(DetailsX1 + 168, DetailsY1 + 159 + dy);
                    else
                        DrawSetStopPanelUp(DetailsX1 + 168, DetailsY1 + 159 + dy);
                    PutText(FontSan, "View Stop", DetailsX1 + 180, DetailsY1 + 162 + dy, -1, true);
                }

                if (unit.IsOwn())
                {
                    bool mouseOnClearButton = _mouseButtonX >= DetailsX1 + 322 && _mouseButtonX <= DetailsX1 + 391 &&
                                              _mouseButtonY >= DetailsY1 + 162 + dy && _mouseButtonY <= DetailsY1 + 183 + dy;
                    if (_leftMousePressed && mouseOnClearButton)
                        DrawClearPanelDown(DetailsX1 + 320, DetailsY1 + 159 + dy);
                    else
                        DrawClearPanelUp(DetailsX1 + 320, DetailsY1 + 159 + dy);
                    PutText(FontSan, "Clear", DetailsX1 + 336, DetailsY1 + 162 + dy, -1, true);
                }

                bool onlyOneItem = firm.FirmType == Firm.FIRM_MINE || firm.FirmType == Firm.FIRM_FACTORY;
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

                    int stockQty = -1;

                    if (firm.FirmType == Firm.FIRM_HARBOR)
                    {
                        FirmHarbor harbor = (FirmHarbor)firm;
                        foreach (int linkedFirmId in harbor.LinkedFirms)
                        {
                            stockQty = GetFirmStockQty(FirmArray[linkedFirmId], rawId, productId);
                            if (stockQty >= 0)
                                break;
                        }
                    }
                    else
                    {
                        stockQty = GetFirmStockQty(firm, rawId, productId);
                    }

                    if (rawId != 0 && stockQty >= 0)
                        onlyOneItem = true;

                    if ((rawId != 0 && stockQty >= 0) || (productId != 0 && stockQty >= 0) || (!onlyOneItem && productId != 0))
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
    }

    private void DrawUnitGoods(int[] rawQty, int[] productQty)
    {
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

    private void DrawUnitIcon(int drawX, int drawY, UnitInfo unitInfo, int rank, int skillId, int skillLevel, int hitPoints, int maxHitPoints, int spyId)
    {
        Graphics.DrawBitmap(unitInfo.GetSmallIconTexture(Graphics, rank), drawX, drawY,
            unitInfo.SoldierSmallIconWidth * 2, unitInfo.SoldierSmallIconHeight * 2);
        if (skillLevel != 0)
            PutText(FontSan, skillLevel.ToString(), drawX + 52, drawY + 6);

        int hitBarX1 = drawX;
        int hitBarY = drawY + 41;
        int hitBarX2 = hitBarX1 + (unitInfo.SoldierSmallIconWidth * 2 - 1) * hitPoints / maxHitPoints;
        const int HIT_BAR_LIGHT_BORDER = 0;
        const int HIT_BAR_DARK_BORDER = 3;
        const int HIT_BAR_BODY = 1;
        int hitBarColor = 0xA8;
        if (maxHitPoints >= 51 && maxHitPoints <= 100)
            hitBarColor = 0xB4;
        if (maxHitPoints >= 101)
            hitBarColor = 0xAC;
        Graphics.DrawLine(hitBarX1, hitBarY, hitBarX2, hitBarY, hitBarColor + HIT_BAR_LIGHT_BORDER); //top
        Graphics.DrawLine(hitBarX1, hitBarY + 1, hitBarX2, hitBarY + 1, hitBarColor + HIT_BAR_LIGHT_BORDER); //top
        Graphics.DrawLine(hitBarX1 + 2, hitBarY + 4, hitBarX2, hitBarY + 4, hitBarColor + HIT_BAR_DARK_BORDER); //bottom
        Graphics.DrawLine(hitBarX1 + 2, hitBarY + 5, hitBarX2, hitBarY + 5, hitBarColor + HIT_BAR_DARK_BORDER); //bottom
        Graphics.DrawLine(hitBarX1, hitBarY, hitBarX1, hitBarY + 5, hitBarColor + HIT_BAR_LIGHT_BORDER); //left
        Graphics.DrawLine(hitBarX1 + 1, hitBarY, hitBarX1 + 1, hitBarY + 5, hitBarColor + HIT_BAR_LIGHT_BORDER); //left
        Graphics.DrawLine(hitBarX2 - 1, hitBarY + 2, hitBarX2 - 1, hitBarY + 3, hitBarColor + HIT_BAR_DARK_BORDER); //right
        Graphics.DrawLine(hitBarX2, hitBarY + 2, hitBarX2, hitBarY + 3, hitBarColor + HIT_BAR_DARK_BORDER); //right
        Graphics.DrawLine(hitBarX1 + 2, hitBarY + 2, hitBarX2 - 2, hitBarY + 2, hitBarColor + HIT_BAR_BODY); //body
        Graphics.DrawLine(hitBarX1 + 2, hitBarY + 3, hitBarX2 - 2, hitBarY + 3, hitBarColor + HIT_BAR_BODY); //body

        bool drawSkillIcon = false;
        if (skillLevel == 0)
        {
            IntPtr skillTexture = GetSkillTexture(rank, skillId);
            if (skillTexture != IntPtr.Zero)
            {
                drawSkillIcon = true;
                Graphics.DrawBitmapScaled(skillTexture, drawX + 52, drawY + 6, _skillWidth, _skillHeight);
            }
        }

        if (spyId != 0 && (SpyArray[spyId].TrueNationId == NationArray.PlayerId || Config.ShowAIInfo))
        {
            int spyIconX = skillLevel != 0 ? drawX + 78 : drawX + 52;
            int spyIconY = skillLevel != 0 ? drawY + 12 : drawY + 6;
            if (drawSkillIcon)
                spyIconY += Scale(_skillHeight);
            DrawSpyIcon(spyIconX, spyIconY, SpyArray[spyId].TrueNationId);
        }
    }

    private void DrawLoyalty(int loyaltyX, int loyaltyY, int loyalty, int targetLoyalty, bool smallSize = true)
    {
        if (NationArray.Player != null && NationArray.Player.Cash <= 0.0)
            targetLoyalty = 0;
        
        PutText(FontSan, loyalty.ToString(), loyaltyX, loyaltyY, -1, smallSize);
        if (loyalty != targetLoyalty)
        {
            int loyaltyWidth = FontSan.TextWidth(loyalty.ToString()) + 1;
            if (smallSize)
                loyaltyWidth = loyaltyWidth * 2 / 3;
            int targetLoyaltyX = loyaltyX + loyaltyWidth;
            int targetLoyaltyY = loyaltyY;
            int arrowDownWidth = smallSize ? _arrowDownWidth : _arrowDownWidth * 2;
            int arrowDownHeight = smallSize ? _arrowDownHeight : _arrowDownHeight * 2;
            int arrowUpWidth = smallSize ? _arrowUpWidth : _arrowUpWidth * 2;
            int arrowUpHeight = smallSize ? _arrowUpHeight : _arrowUpHeight * 2;
            if (targetLoyalty < loyalty)
                Graphics.DrawBitmap(_arrowDownTexture, targetLoyaltyX, targetLoyaltyY + 4, arrowDownWidth, arrowDownHeight);
            if (targetLoyalty > loyalty)
                Graphics.DrawBitmap(_arrowUpTexture, targetLoyaltyX, targetLoyaltyY + 4, arrowUpWidth, arrowUpHeight);
            PutText(FontSan, targetLoyalty.ToString(), targetLoyaltyX + (smallSize ? 8 : 16), targetLoyaltyY, -1, smallSize);
        }
    }

    private void HandleUnitDetailsInput(Unit unit)
    {
        bool mouseOnColorSquareButton = _mouseButtonX >= DetailsX1 + 18 && _mouseButtonX <= DetailsX1 + 48 &&
                                        _mouseButtonY >= DetailsY1 + 9 && _mouseButtonY <= DetailsY1 + 32;
        if (_leftMouseReleased && mouseOnColorSquareButton)
            GoToLocation(unit.CurLocX, unit.CurLocY);

        bool mouseOnResignButton = _mouseButtonX >= DetailsX1 + 369 && _mouseButtonX <= DetailsX1 + 401 &&
                                   _mouseButtonY >= DetailsY1 + 3 && _mouseButtonY <= DetailsY1 + 38;
        if (_leftMouseReleased && mouseOnResignButton && CanResign(unit))
            unit.Resign(InternalConstants.COMMAND_PLAYER);
        
        unit.HandleDetailsInput(this);
    }

    private void HandleTradeStops(ITrader trader, TradeStop[] stops, bool isOwnTrader)
    {
        int dy = 0;
        for (int i = 0; i < UnitCaravan.MAX_STOP_FOR_CARAVAN; i++)
        {
            bool mouseOnSetStopButton = _mouseButtonX >= DetailsX1 + 18 && _mouseButtonX <= DetailsX1 + 123 &&
                                        _mouseButtonY >= DetailsY1 + 162 + dy && _mouseButtonY <= DetailsY1 + 183 + dy;
            if (_leftMouseReleased && mouseOnSetStopButton && isOwnTrader)
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
                if (_leftMouseReleased && mouseOnViewStopButton && (isOwnTrader || Config.ShowAIInfo))
                {
                    GoToLocation(firm.LocCenterX, firm.LocCenterY);
                }

                bool mouseOnClearButton = _mouseButtonX >= DetailsX1 + 322 && _mouseButtonX <= DetailsX1 + 391 &&
                                          _mouseButtonY >= DetailsY1 + 162 + dy && _mouseButtonY <= DetailsY1 + 183 + dy;
                if (_leftMouseReleased && mouseOnClearButton && isOwnTrader)
                {
                    trader.DelStop(i + 1, InternalConstants.COMMAND_PLAYER);
                    Audio.SelectionSound("TURN_OFF");
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
                        foreach (MarketGoods marketGoods in market.MarketGoods)
                        {
                            if ((marketGoods.RawId != 0 && marketGoods.RawId == rawId) ||
                                (marketGoods.ProductId != 0 && marketGoods.ProductId == productId))
                            {
                                stock = (int)marketGoods.StockQty;
                                break;
                            }
                        }
                    }

                    if ((rawId != 0 && stock > 0) || productId != 0 || j == TradeStop.AUTO_PICK_UP || j == TradeStop.NO_PICK_UP)
                    {
                        bool mouseOnResourceButton = _mouseButtonX >= DetailsX1 + 112 + dx && _mouseButtonX <= DetailsX1 + 139 + dx &&
                                                     _mouseButtonY >= DetailsY1 + 129 + dy && _mouseButtonY <= DetailsY1 + 156 + dy;
                        if (_leftMousePressed && mouseOnResourceButton && isOwnTrader)
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
    
    //TODO for ships UnitDetailsMode should be reset to UnitDetailsMode.ShipGoods
    private void CancelSetStop()
    {
        if (UnitDetailsMode == UnitDetailsMode.SetStop)
        {
            UnitDetailsMode = UnitDetailsMode.Normal;
            _setStopId = 0;
        }
    }

    private bool CanResign(Unit unit)
    {
        return NationArray.PlayerId != 0 && unit.NationId == NationArray.PlayerId && unit.Rank != Unit.RANK_KING;
    }
}