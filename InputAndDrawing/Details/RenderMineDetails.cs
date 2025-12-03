namespace TenKingdoms;

public partial class Renderer
{
    public void DrawMineDetails(FirmMine mine)
    {
        DrawMineFactoryPanel(DetailsX1 + 2, DetailsY1 + 96);
        if (mine.RawId == 0)
        {
            //TODO check
            PutTextCenter(FontSan, "No Natural Resources", DetailsX1 + 2, DetailsY1 + 96, DetailsX1 + 400, DetailsY1 + 196);
            return;
        }

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
        PutText(FontSan, (int)mine.StockQty + " / " + (int)mine.MaxStockQty, DetailsX1 + 179, DetailsY1 + 169, -1, true);
        PutText(FontSan, "Untapped Reserve", DetailsX1 + 13, DetailsY1 + 197, -1, true);
        PutText(FontSan, ((int)mine.ReserveQty).ToString(), DetailsX1 + 179, DetailsY1 + 199, -1, true);
        
        DrawWorkers(mine);
        
        DrawPanelWithTwoFields(DetailsX1 + 2, DetailsY1 + 339);

        if (mine.OwnFirm())
        {
            if (mine.HaveOwnWorkers())
            {
                bool mouseOnButton = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                     _mouseButtonY >= ButtonsMineY + 2 && _mouseButtonY <= ButtonsMineY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmap(_buttonDownTexture, Button1X, ButtonsMineY, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
                else
                    Graphics.DrawBitmap(_buttonUpTexture, Button1X, ButtonsMineY, Scale(_buttonUpWidth), Scale(_buttonUpHeight));
                Graphics.DrawBitmap(_buttonRecruitTexture, Button1X + 3, ButtonsMineY + 3, Scale(_buttonRecruitWidth), Scale(_buttonRecruitHeight));
            }
            else
            {
                Graphics.DrawBitmap(_buttonDisabledTexture, Button1X, ButtonsMineY, Scale(_buttonDisabledWidth), Scale(_buttonDisabledHeight));
                Graphics.DrawBitmap(_buttonRecruitDisabledTexture, Button1X + 3, ButtonsMineY + 3, Scale(_buttonRecruitWidth),
                    Scale(_buttonRecruitHeight));
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