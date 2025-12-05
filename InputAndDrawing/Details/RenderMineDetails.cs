namespace TenKingdoms;

public partial class Renderer
{
    public void DrawMineDetails(FirmMine mine)
    {
        DrawMineFactoryPanel(DetailsX1 + 2, DetailsY1 + 96);
        if (mine.RawId != 0)
        {
            RawInfo rawInfo = RawRes[mine.RawId];
            Graphics.DrawBitmap(rawInfo.GetLargeRawTexture(Graphics), DetailsX1 + 12, DetailsY1 + 104,
                rawInfo.LargeRawIconWidth * 3 / 4, rawInfo.LargeRawIconHeight * 3 / 4);
            
            string miningResource = "Mining " + mine.RawId switch
            {
                1 => "Clay",
                2 => "Copper",
                3 => "Iron",
                _ => ""
            };
            PutText(FontSan, miningResource, DetailsX1 + 42, DetailsY1 + 100);
        
            DrawFieldPanel111(DetailsX1 + 7, DetailsY1 + 134);
            DrawFieldPanel111(DetailsX1 + 7, DetailsY1 + 164);
            DrawFieldPanel111(DetailsX1 + 7, DetailsY1 + 194);
            PutText(FontSan, "Monthly Production", DetailsX1 + 13, DetailsY1 + 137, -1, true);
            PutText(FontSan, ((int)mine.Production30Days()).ToString(), DetailsX1 + 179, DetailsY1 + 139, -1, true);
            PutText(FontSan, "Mined Stock", DetailsX1 + 13, DetailsY1 + 167, -1, true);
            PutText(FontSan, (int)mine.StockQty + "/" + (int)mine.MaxStockQty, DetailsX1 + 179, DetailsY1 + 169, -1, true);
            PutText(FontSan, "Untapped Reserve", DetailsX1 + 13, DetailsY1 + 197, -1, true);
            PutText(FontSan, ((int)mine.ReserveQty).ToString(), DetailsX1 + 179, DetailsY1 + 199, -1, true);
        }
        else
        {
            PutTextCenter(FontSan, "No Natural Resources", DetailsX1 + 2, DetailsY1 + 158, DetailsX1 + 400, DetailsY1 + 158);
        }
        
        DrawWorkers(mine);
        
        DrawPanelWithTwoFields(DetailsX1 + 2, DetailsY1 + 339);
        DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 344);
        DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 373);
        DrawFieldPanel75(DetailsX1 + 208, DetailsY1 + 373);
        PutText(FontSan, "Residence", DetailsX1 + 13, DetailsY1 + 347, -1, true);
        PutText(FontSan, "Loyalty", DetailsX1 + 13, DetailsY1 + 376, -1, true);
        PutText(FontSan, "Mining", DetailsX1 + 214, DetailsY1 + 376, -1, true);
        if (mine.SelectedWorkerId != 0)
        {
            Worker worker = mine.Workers[mine.SelectedWorkerId - 1];
            PutText(FontSan, TownArray[worker.TownId].Name, DetailsX1 + 113, DetailsY1 + 349, -1, true);
            PutText(FontSan, worker.Loyalty().ToString(), DetailsX1 + 113, DetailsY1 + 378, -1, true);
            PutText(FontSan, worker.SkillLevel.ToString(), DetailsX1 + 327, DetailsY1 + 378, -1, true);
        }

        if (mine.OwnFirm())
        {
            if (mine.HaveOwnWorkers())
            {
                bool mouseOnButton = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                     _mouseButtonY >= ButtonsMineY + 2 && _mouseButtonY <= ButtonsMineY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmapScaled(_buttonDownTexture, Button1X, ButtonsMineY, _buttonDownWidth, _buttonDownHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonUpTexture, Button1X, ButtonsMineY, _buttonUpWidth, _buttonUpHeight);
                Graphics.DrawBitmapScaled(_buttonRecruitTexture, Button1X + 3, ButtonsMineY + 7, _buttonRecruitWidth, _buttonRecruitHeight);
            }
            else
            {
                Graphics.DrawBitmapScaled(_buttonDisabledTexture, Button1X, ButtonsMineY, _buttonDisabledWidth, _buttonDisabledHeight);
                Graphics.DrawBitmapScaled(_buttonRecruitDisabledTexture, Button1X + 3, ButtonsMineY + 7, _buttonRecruitWidth, _buttonRecruitHeight);
            }
        }
        
        // TODO display spy list and capture buttons
    }
    
    public void HandleMineDetailsInput(FirmMine mine)
    {
        bool button1Pressed = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                              _mouseButtonY >= ButtonsMineY + 2 && _mouseButtonY <= ButtonsMineY + ButtonHeight;
        
        if (mine.OwnFirm())
        {
            if (button1Pressed && mine.HaveOwnWorkers())
            {
                mine.MobilizeAllWorkers(InternalConstants.COMMAND_PLAYER);
            }
        }
    }
}