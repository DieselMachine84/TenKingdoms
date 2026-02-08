namespace TenKingdoms;

public partial class Renderer
{
    private int ResearchPanelX => DetailsX1 + 2;
    private int ResearchPanelY => DetailsY1 + 2;
    private int MouseOnResearchButtonX1 => DetailsX1 + 8;
    private int MouseOnResearchButtonX2 => DetailsX1 + 320;
    private int MouseOnResearchButtonY1 => DetailsY1 + 2;
    private int MouseOnResearchButtonY2 => DetailsY1 + 66;
    private int MaxResearchItems = 6;

    public void DrawResearchDetails(FirmResearch research)
    {
        if (FirmDetailsMode == FirmDetailsMode.Research)
        {
            DrawResearchMenu(research);
            return;
        }

        DrawResearchWarFactoryPanel(DetailsX1 + 2, DetailsY1 + 96);

        if (research.TechId != 0)
        {
            TechInfo techInfo = TechRes[research.TechId];
            Graphics.DrawBitmap(techInfo.GetTechLargeIconTexture(Graphics), DetailsX1 + 6, DetailsY1 + 100,
                techInfo.TechLargeIconWidth * 2, techInfo.TechLargeIconHeight * 2);

            Nation firmNation = NationArray[research.NationId];
            string description = techInfo.Description();
            int researchVersion = firmNation.GetTechLevel(research.TechId) + 1;
            if (researchVersion > 1)
                description += " " + Misc.roman_number(researchVersion);
            PutText(FontSan, description, DetailsX1 + 120, DetailsY1 + 107);
            //TODO draw indicator
            PutText(FontSan, firmNation.GetResearchProgress(research.TechId).ToString("0.00"), DetailsX1 + 120, DetailsY1 + 137);
        }

        DrawWorkers(research);
        DrawWorkerDetails(research, "Research");
        
        if (research.OwnFirm())
        {
            bool mouseOnButton = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                 _mouseButtonY >= ButtonsResearchY + 2 && _mouseButtonY <= ButtonsResearchY + ButtonHeight;
            if (_leftMousePressed && mouseOnButton)
                Graphics.DrawBitmapScaled(_buttonDownTexture, Button1X, ButtonsResearchY, _buttonDownWidth, _buttonDownHeight);
            else
                Graphics.DrawBitmapScaled(_buttonUpTexture, Button1X, ButtonsResearchY, _buttonUpWidth, _buttonUpHeight);
            Graphics.DrawBitmapScaled(_buttonResearchTexture, Button1X + 5, ButtonsResearchY + 7, _buttonResearchWidth, _buttonResearchHeight);

            if (research.HaveOwnWorkers())
            {
                mouseOnButton = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                                _mouseButtonY >= ButtonsResearchY + 2 && _mouseButtonY <= ButtonsResearchY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmapScaled(_buttonDownTexture, Button2X, ButtonsResearchY, _buttonDownWidth, _buttonDownHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonUpTexture, Button2X, ButtonsResearchY, _buttonUpWidth, _buttonUpHeight);
                Graphics.DrawBitmapScaled(_buttonRecruitTexture, Button2X + 3, ButtonsResearchY + 7, _buttonRecruitWidth, _buttonRecruitHeight);
            }
            else
            {
                Graphics.DrawBitmapScaled(_buttonDisabledTexture, Button2X, ButtonsResearchY, _buttonDisabledWidth, _buttonDisabledHeight);
                Graphics.DrawBitmapScaled(_buttonRecruitDisabledTexture, Button2X + 3, ButtonsResearchY + 7, _buttonRecruitWidth, _buttonRecruitHeight);
            }
        }
        
        if (IsFirmSpyListEnabled(research))
        {
            bool mouseOnButton = _mouseButtonX >= Button3X + 2 && _mouseButtonX <= Button3X + ButtonWidth &&
                                 _mouseButtonY >= ButtonsResearchY + 2 && _mouseButtonY <= ButtonsResearchY + ButtonHeight;
            if (_leftMousePressed && mouseOnButton)
                Graphics.DrawBitmapScaled(_buttonDownTexture, Button3X, ButtonsResearchY, _buttonDownWidth, _buttonDownHeight);
            else
                Graphics.DrawBitmapScaled(_buttonUpTexture, Button3X, ButtonsResearchY, _buttonUpWidth, _buttonUpHeight);
            Graphics.DrawBitmapScaled(_buttonSpyMenuTexture, Button3X + 4, ButtonsResearchY + 16, _buttonSpyMenuWidth, _buttonSpyMenuHeight);
        }
        
        // TODO display and capture button
    }

    private void DrawResearchMenu(FirmResearch research)
    {
        Nation firmNation = NationArray[research.NationId];
        int shownItems = 0;
        int dy = 0;
        for (int techId = 1; techId <= TechRes.TechInfos.Length + 1; techId++)
        {
            bool showCancelButton = (shownItems == MaxResearchItems || techId > TechRes.TechInfos.Length);
            
            if (!showCancelButton && !firmNation.CanResearch(techId))
                continue;

            bool mouseOnButton = _mouseButtonX >= MouseOnResearchButtonX1 && _mouseButtonX <= MouseOnResearchButtonX2 &&
                                 _mouseButtonY >= MouseOnResearchButtonY1 + dy && _mouseButtonY <= MouseOnResearchButtonY2 + dy;
            if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
                DrawResearchBuildWeaponPanelDown(ResearchPanelX, ResearchPanelY + dy);
            else
                DrawResearchBuildWeaponPanelUp(ResearchPanelX, ResearchPanelY + dy);

            if (!showCancelButton)
            {
                TechInfo techInfo = TechRes[techId];
                Graphics.DrawBitmap(techInfo.GetTechLargeIconTexture(Graphics), ResearchPanelX + 4, ResearchPanelY + dy + 4,
                    techInfo.TechLargeIconWidth * 3 / 2, techInfo.TechLargeIconHeight * 3 / 2);
                
                string description = techInfo.Description();
                int researchVersion = firmNation.GetTechLevel(techId) + 1;
                if (researchVersion > 1)
                    description += " " + Misc.roman_number(researchVersion);
                PutText(FontBible, description, ResearchPanelX + 96, ResearchPanelY + dy + 10);
                shownItems++;
            }
            else
            {
                PutText(FontBible, "Cancel", ResearchPanelX + 158, ResearchPanelY + dy + 10);
                break;
            }
            
            dy += 70;
        }
    }

    public void HandleResearchDetailsInput(FirmResearch research)
    {
        if (FirmDetailsMode == FirmDetailsMode.Spy)
        {
            HandleSpyList(research.NationId, research.GetPlayerSpies());
            return;
        }
        
        if (!research.OwnFirm())
            return;

        if (FirmDetailsMode == FirmDetailsMode.Research)
        {
            HandleResearchMenu(research);
            return;
        }

        bool mouseOnButton1 = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                              _mouseButtonY >= ButtonsResearchY + 2 && _mouseButtonY <= ButtonsResearchY + ButtonHeight;
        bool mouseOnButton2 = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                              _mouseButtonY >= ButtonsResearchY + 2 && _mouseButtonY <= ButtonsResearchY + ButtonHeight;
        bool mouseOnButton3 = _mouseButtonX >= Button3X + 2 && _mouseButtonX <= Button3X + ButtonWidth &&
                              _mouseButtonY >= ButtonsResearchY + 2 && _mouseButtonY <= ButtonsResearchY + ButtonHeight;

        if (_leftMouseReleased && mouseOnButton1)
        {
            FirmDetailsMode = FirmDetailsMode.Research;
        }

        if (_leftMouseReleased && mouseOnButton2 && research.HaveOwnWorkers())
        {
            research.MobilizeAllWorkers(InternalConstants.COMMAND_PLAYER);
        }

        if (_leftMouseReleased && mouseOnButton3)
        {
            FirmDetailsMode = FirmDetailsMode.Spy;
        }
    }

    private void HandleResearchMenu(FirmResearch research)
    {
        Nation firmNation = NationArray[research.NationId];
        int shownItems = 0;
        int dy = 0;
        for (int techId = 1; techId <= TechRes.TechInfos.Length + 1; techId++)
        {
            bool onCancelButton = (shownItems == MaxResearchItems || techId > TechRes.TechInfos.Length);
            
            if (!onCancelButton && !firmNation.CanResearch(techId))
                continue;

            bool mouseOnButton = _mouseButtonX >= MouseOnResearchButtonX1 && _mouseButtonX <= MouseOnResearchButtonX2 &&
                                 _mouseButtonY >= MouseOnResearchButtonY1 + dy && _mouseButtonY <= MouseOnResearchButtonY2 + dy;
            if (mouseOnButton && (_leftMouseReleased || _rightMouseReleased))
            {
                if (!onCancelButton)
                {
                    if (_leftMouseReleased)
                    {
                        research.StartResearch(techId, InternalConstants.COMMAND_PLAYER);
                    }

                    if (_rightMouseReleased)
                    {
                        foreach (Firm firm in FirmArray)
                        {
                            if (NationArray.PlayerId != 0 && firm.NationId == NationArray.PlayerId && firm is FirmResearch)
                            {
                                ((FirmResearch)firm).StartResearch(techId, InternalConstants.COMMAND_PLAYER);
                            }
                        }
                    }

                    SECtrl.immediate_sound("TURN_ON");
                    FirmDetailsMode = FirmDetailsMode.Normal;
                }
                else
                {
                    if (_leftMouseReleased)
                    {
                        SECtrl.immediate_sound("TURN_OFF");
                        FirmDetailsMode = FirmDetailsMode.Normal;
                    }
                }
            }

            if (!onCancelButton)
                shownItems++;
            else
                break;
            
            dy += 70;
        }
    }
}