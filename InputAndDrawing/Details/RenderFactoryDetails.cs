namespace TenKingdoms;

public partial class Renderer
{
    public void DrawFactoryDetails(FirmFactory factory)
    {
        DrawMineFactoryPanel(DetailsX1 + 2, DetailsY1 + 96);
        
        RawInfo rawInfo = RawRes[factory.ProductRawId];
        Graphics.DrawBitmap(rawInfo.GetLargeProductTexture(Graphics), DetailsX1 + 12, DetailsY1 + 104,
            rawInfo.LargeProductIconWidth * 3 / 4, rawInfo.LargeProductIconHeight * 3 / 4);
        
        string manufacturingProduct = "Producing " + factory.ProductRawId switch
        {
            1 => "Clay",
            2 => "Copper",
            3 => "Iron",
            _ => ""
        } + " Products";
        PutText(FontSan, manufacturingProduct, DetailsX1 + 42, DetailsY1 + 100);
        
        DrawFieldPanel119(DetailsX1 + 7, DetailsY1 + 134);
        DrawFieldPanel119(DetailsX1 + 7, DetailsY1 + 164);
        DrawFieldPanel119(DetailsX1 + 7, DetailsY1 + 194);
        PutText(FontSan, "Monthly Production", DetailsX1 + 13, DetailsY1 + 137, -1, true);
        PutText(FontSan, ((int)factory.Production30Days()).ToString(), DetailsX1 + 191, DetailsY1 + 139, -1, true);
        PutText(FontSan, "Raw Material Stock", DetailsX1 + 13, DetailsY1 + 167, -1, true);
        PutText(FontSan, (int)factory.RawStockQty + " / " + (int)factory.MaxRawStockQty, DetailsX1 + 191, DetailsY1 + 169, -1, true);
        PutText(FontSan, "Product Stock", DetailsX1 + 13, DetailsY1 + 197, -1, true);
        PutText(FontSan, (int)factory.StockQty + " / " + (int)factory.MaxStockQty, DetailsX1 + 191, DetailsY1 + 199, -1, true);
        
        DrawWorkers(factory, DetailsY1 + 228);
        
        DrawPanelWithTwoFields(DetailsX1 + 2, DetailsY1 + 339);
        
        if (factory.OwnFirm())
        {
            bool mouseOnButton = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                 _mouseButtonY >= ButtonsFactoryY + 2 && _mouseButtonY <= ButtonsFactoryY + ButtonHeight;
            if (_leftMousePressed && mouseOnButton)
                Graphics.DrawBitmap(_buttonDownTexture, Button1X, ButtonsFactoryY, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
            else
                Graphics.DrawBitmap(_buttonUpTexture, Button1X, ButtonsFactoryY, Scale(_buttonUpWidth), Scale(_buttonUpHeight));
            Graphics.DrawBitmap(_buttonChangeProductionTexture, Button1X + 3, ButtonsFactoryY + 3,
                Scale(_buttonChangeProductionWidth), Scale(_buttonChangeProductionHeight));

            if (factory.HaveOwnWorkers())
            {
                mouseOnButton = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                                     _mouseButtonY >= ButtonsFactoryY + 2 && _mouseButtonY <= ButtonsFactoryY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmap(_buttonDownTexture, Button2X, ButtonsFactoryY, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
                else
                    Graphics.DrawBitmap(_buttonUpTexture, Button2X, ButtonsFactoryY, Scale(_buttonUpWidth), Scale(_buttonUpHeight));
                Graphics.DrawBitmap(_buttonRecruitTexture, Button2X + 3, ButtonsFactoryY + 3, Scale(_buttonRecruitWidth), Scale(_buttonRecruitHeight));
            }
            else
            {
                Graphics.DrawBitmap(_buttonDisabledTexture, Button2X, ButtonsFactoryY, Scale(_buttonDisabledWidth), Scale(_buttonDisabledHeight));
                Graphics.DrawBitmap(_buttonRecruitDisabledTexture, Button2X + 3, ButtonsFactoryY + 3, Scale(_buttonRecruitWidth),
                    Scale(_buttonRecruitHeight));
            }
        }
        
        // TODO display spy list and capture buttons
    }
    
    public void HandleFactoryDetailsInput(FirmFactory factory)
    {
        bool button1Pressed = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                              _mouseButtonY >= ButtonsFactoryY + 2 && _mouseButtonY <= ButtonsFactoryY + ButtonHeight;
        bool button2Pressed = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                              _mouseButtonY >= ButtonsFactoryY + 2 && _mouseButtonY <= ButtonsFactoryY + ButtonHeight;

        if (factory.OwnFirm())
        {
            if (button1Pressed)
            {
                factory.ChangeProduction();
                SECtrl.immediate_sound("TURN_ON");
            }
            
            if (button2Pressed && factory.HaveOwnWorkers())
            {
                factory.MobilizeAllWorkers(InternalConstants.COMMAND_PLAYER);
            }
        }
    }
}