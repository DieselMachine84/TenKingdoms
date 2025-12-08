namespace TenKingdoms;

public partial class Renderer
{
    private const int ResearchPanelX = DetailsX1 + 2;
    private const int ResearchPanelY = DetailsY1 + 2;
    private const int MouseOnResearchButtonX1 = DetailsX1 + 8;
    private const int MouseOnResearchButtonX2 = DetailsX1 + 320;
    private const int MouseOnResearchButtonY1 = DetailsY1 + 2;
    private const int MouseOnResearchButtonY2 = DetailsY1 + 66;
    private const int MaxResearchItems = 6;

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

            string description = techInfo.Description();
            int researchVersion = techInfo.get_nation_tech_level(research.NationId) + 1;
            if (researchVersion > 1)
                description += " " + Misc.roman_number(researchVersion);
            PutText(FontSan, description, DetailsX1 + 120, DetailsY1 + 107);
            //TODO draw indicator
            PutText(FontSan, techInfo.get_progress(research.NationId).ToString("0.00"), DetailsX1 + 120, DetailsY1 + 137);
        }

        DrawWorkers(research);
        
        DrawPanelWithTwoFields(DetailsX1 + 2, DetailsY1 + 294);
        DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 299);
        DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 328);
        DrawFieldPanel75(DetailsX1 + 208, DetailsY1 + 328);
        PutText(FontSan, "Residence", DetailsX1 + 13, DetailsY1 + 302, -1, true);
        PutText(FontSan, "Loyalty", DetailsX1 + 13, DetailsY1 + 331, -1, true);
        PutText(FontSan, "Research", DetailsX1 + 214, DetailsY1 + 331, -1, true);
        if (research.SelectedWorkerId != 0)
        {
            Worker worker = research.Workers[research.SelectedWorkerId - 1];
            PutText(FontSan, TownArray[worker.TownId].Name, DetailsX1 + 113, DetailsY1 + 304, -1, true);
            PutText(FontSan, worker.Loyalty().ToString(), DetailsX1 + 113, DetailsY1 + 333, -1, true);
            PutText(FontSan, worker.SkillLevel.ToString(), DetailsX1 + 327, DetailsY1 + 333, -1, true);
        }
        
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
        
        // TODO display spy list and capture buttons
    }

    private void DrawResearchMenu(FirmResearch research)
    {
        int shownItems = 0;
        int dy = 0;
        for (int techId = 1; techId <= TechRes.TechInfos.Length + 1; techId++)
        {
            bool showCancelButton = (shownItems == MaxResearchItems || techId > TechRes.TechInfos.Length);
            
            TechInfo techInfo = !showCancelButton ? TechRes[techId] : null;
            if (!showCancelButton && !techInfo.can_research(research.NationId))
                continue;

            bool mouseOnButton = _mouseButtonX >= MouseOnResearchButtonX1 && _mouseButtonX <= MouseOnResearchButtonX2 &&
                                 _mouseButtonY >= MouseOnResearchButtonY1 + dy && _mouseButtonY <= MouseOnResearchButtonY2 + dy;
            if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
                DrawResearchBuildWeaponPanelDown(ResearchPanelX, ResearchPanelY + dy);
            else
                DrawResearchBuildWeaponPanelUp(ResearchPanelX, ResearchPanelY + dy);

            if (!showCancelButton)
            {
                Graphics.DrawBitmap(techInfo.GetTechLargeIconTexture(Graphics), ResearchPanelX + 4, ResearchPanelY + dy + 4,
                    techInfo.TechLargeIconWidth * 3 / 2, techInfo.TechLargeIconHeight * 3 / 2);
                
                string description = techInfo.Description();
                int researchVersion = techInfo.get_nation_tech_level(research.NationId) + 1;
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
        if (FirmDetailsMode == FirmDetailsMode.Research)
        {
            HandleResearchMenu(research);
            return;
        }

        bool button1Pressed = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                              _mouseButtonY >= ButtonsResearchY + 2 && _mouseButtonY <= ButtonsResearchY + ButtonHeight;
        bool button2Pressed = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                              _mouseButtonY >= ButtonsResearchY + 2 && _mouseButtonY <= ButtonsResearchY + ButtonHeight;

        if (research.OwnFirm())
        {
            if (button1Pressed)
            {
                FirmDetailsMode = FirmDetailsMode.Research;
            }
            
            if (button2Pressed && research.HaveOwnWorkers())
            {
                research.MobilizeAllWorkers(InternalConstants.COMMAND_PLAYER);
            }
        }
    }
    
    private void HandleResearchMenu(FirmResearch research)
    {
        int shownItems = 0;
        int dy = 0;
        for (int techId = 1; techId <= TechRes.TechInfos.Length + 1; techId++)
        {
            bool onCancelButton = (shownItems == MaxResearchItems || techId > TechRes.TechInfos.Length);
            
            TechInfo techInfo = !onCancelButton ? TechRes[techId] : null;
            if (!onCancelButton && !techInfo.can_research(research.NationId))
                continue;

            bool mouseOnButton = _mouseButtonX >= MouseOnResearchButtonX1 && _mouseButtonX <= MouseOnResearchButtonX2 &&
                                 _mouseButtonY >= MouseOnResearchButtonY1 + dy && _mouseButtonY <= MouseOnResearchButtonY2 + dy;
            if (mouseOnButton)
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
                            if (NationArray.player_recno != 0 && firm.NationId == NationArray.player_recno && firm is FirmResearch)
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
                    SECtrl.immediate_sound("TURN_OFF");
                    FirmDetailsMode = FirmDetailsMode.Normal;
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