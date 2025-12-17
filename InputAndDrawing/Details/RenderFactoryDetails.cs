namespace TenKingdoms;

public partial class Renderer
{
    public void DrawFactoryDetails(FirmFactory factory)
    {
        DrawMineFactoryPanel(DetailsX1 + 2, DetailsY1 + 96);
        
        RawInfo rawInfo = RawRes[factory.ProductId];
        Graphics.DrawBitmap(rawInfo.GetLargeProductTexture(Graphics), DetailsX1 + 12, DetailsY1 + 104,
            rawInfo.LargeProductIconWidth * 3 / 4, rawInfo.LargeProductIconHeight * 3 / 4);
        
        string manufacturingProduct = "Producing " + factory.ProductId switch
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
        PutText(FontSan, (int)factory.RawStockQty + "/" + (int)factory.MaxRawStockQty, DetailsX1 + 191, DetailsY1 + 169, -1, true);
        PutText(FontSan, "Product Stock", DetailsX1 + 13, DetailsY1 + 197, -1, true);
        PutText(FontSan, (int)factory.StockQty + "/" + (int)factory.MaxStockQty, DetailsX1 + 191, DetailsY1 + 199, -1, true);
        
        DrawWorkers(factory);
        
        DrawPanelWithTwoFields(DetailsX1 + 2, DetailsY1 + 339);
        DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 344);
        DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 373);
        DrawFieldPanel75(DetailsX1 + 208, DetailsY1 + 373);
        PutText(FontSan, "Residence", DetailsX1 + 13, DetailsY1 + 347, -1, true);
        PutText(FontSan, "Loyalty", DetailsX1 + 13, DetailsY1 + 376, -1, true);
        PutText(FontSan, "Manufacture", DetailsX1 + 214, DetailsY1 + 376, -1, true);
        if (factory.SelectedWorkerId != 0)
        {
            Worker worker = factory.Workers[factory.SelectedWorkerId - 1];
            PutText(FontSan, TownArray[worker.TownId].Name, DetailsX1 + 113, DetailsY1 + 349, -1, true);
            PutText(FontSan, worker.Loyalty().ToString(), DetailsX1 + 113, DetailsY1 + 378, -1, true);
            PutText(FontSan, worker.SkillLevel.ToString(), DetailsX1 + 327, DetailsY1 + 378, -1, true);
        }
        
        if (factory.OwnFirm())
        {
            bool mouseOnButton = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                 _mouseButtonY >= ButtonsFactoryY + 2 && _mouseButtonY <= ButtonsFactoryY + ButtonHeight;
            if (_leftMousePressed && mouseOnButton)
                Graphics.DrawBitmapScaled(_buttonDownTexture, Button1X, ButtonsFactoryY, _buttonDownWidth, _buttonDownHeight);
            else
                Graphics.DrawBitmapScaled(_buttonUpTexture, Button1X, ButtonsFactoryY, _buttonUpWidth, _buttonUpHeight);
            Graphics.DrawBitmapScaled(_buttonChangeProductionTexture, Button1X + 5, ButtonsFactoryY + 3,
                _buttonChangeProductionWidth, _buttonChangeProductionHeight);

            if (factory.HaveOwnWorkers())
            {
                mouseOnButton = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                                _mouseButtonY >= ButtonsFactoryY + 2 && _mouseButtonY <= ButtonsFactoryY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmapScaled(_buttonDownTexture, Button2X, ButtonsFactoryY, _buttonDownWidth, _buttonDownHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonUpTexture, Button2X, ButtonsFactoryY, _buttonUpWidth, _buttonUpHeight);
                Graphics.DrawBitmapScaled(_buttonRecruitTexture, Button2X + 3, ButtonsFactoryY + 7, _buttonRecruitWidth, _buttonRecruitHeight);
            }
            else
            {
                Graphics.DrawBitmapScaled(_buttonDisabledTexture, Button2X, ButtonsFactoryY, _buttonDisabledWidth, _buttonDisabledHeight);
                Graphics.DrawBitmapScaled(_buttonRecruitDisabledTexture, Button2X + 3, ButtonsFactoryY + 7, _buttonRecruitWidth, _buttonRecruitHeight);
            }
        }
        
        // TODO display spy list and capture buttons
    }

    public void HandleFactoryDetailsInput(FirmFactory factory)
    {
        if (!factory.OwnFirm())
            return;

        bool mouseOnButton1 = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                              _mouseButtonY >= ButtonsFactoryY + 2 && _mouseButtonY <= ButtonsFactoryY + ButtonHeight;
        bool mouseOnButton2 = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                              _mouseButtonY >= ButtonsFactoryY + 2 && _mouseButtonY <= ButtonsFactoryY + ButtonHeight;

        if (_leftMouseReleased && mouseOnButton1)
        {
            factory.ChangeProduction();
            SECtrl.immediate_sound("TURN_ON");
        }

        if (_leftMouseReleased && mouseOnButton2 && factory.HaveOwnWorkers())
        {
            factory.MobilizeAllWorkers(InternalConstants.COMMAND_PLAYER);
        }
    }
}