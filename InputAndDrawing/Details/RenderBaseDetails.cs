namespace TenKingdoms;

public partial class Renderer
{
    //TODO draw god progress
    
    public void DrawBaseDetails(FirmBase firmBase)
    {
        DrawOverseer(firmBase);
        DrawWorkers(firmBase);
        
        Unit overseer = (firmBase.OverseerId != 0) ? UnitArray[firmBase.OverseerId] : null;
        
        DrawPanelWithTwoFields(DetailsX1 + 2, DetailsY1 + 303);
        DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 308);
        bool drawCombatPanel = firmBase.SelectedWorkerId == 0;
        if (drawCombatPanel)
            DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 337);
        DrawFieldPanel62(DetailsX1 + 208, DetailsY1 + 308);
        bool drawLoyaltyPanel = overseer == null || overseer.Rank != Unit.RANK_KING || firmBase.SelectedWorkerId != 0;
        if (drawLoyaltyPanel)
            DrawFieldPanel62(DetailsX1 + 208, DetailsY1 + 337);

        if (overseer != null || firmBase.SelectedWorkerId != 0)
        {
            PutText(FontSan, firmBase.SelectedWorkerId == 0 ? "Leadership" : "Praying", DetailsX1 + 13, DetailsY1 + 311, -1, true);
            if (drawCombatPanel)
                PutText(FontSan, "Combat", DetailsX1 + 13, DetailsY1 + 340, -1, true);
            PutText(FontSan, "Hit Points", DetailsX1 + 214, DetailsY1 + 311, -1, true);
            if (drawLoyaltyPanel)
                PutText(FontSan, "Loyalty", DetailsX1 + 214, DetailsY1 + 340, -1, true);

            string leadershipPrayingText;
            string combatLevelText;
            string hitPointsText;
            int loyalty;
            int targetLoyalty;
            if (firmBase.SelectedWorkerId != 0)
            {
                Worker worker = firmBase.Workers[firmBase.SelectedWorkerId - 1];
                leadershipPrayingText = worker.SkillLevel.ToString();
                combatLevelText = worker.CombatLevel.ToString();
                hitPointsText = worker.HitPoints + "/" + worker.MaxHitPoints();
                loyalty = worker.Loyalty();
                targetLoyalty = worker.TargetLoyalty(firmBase.FirmId);
            }
            else
            {
                leadershipPrayingText = overseer.Skill.GetSkillLevel(Skill.SKILL_LEADING).ToString();
                combatLevelText = overseer.Skill.CombatLevel.ToString();
                hitPointsText = (int)overseer.HitPoints + "/" + overseer.MaxHitPoints;
                loyalty = overseer.Loyalty;
                targetLoyalty = overseer.TargetLoyalty;
            }
            PutText(FontSan, leadershipPrayingText, DetailsX1 + 113, DetailsY1 + 313, -1, true);
            if (drawCombatPanel)
                PutText(FontSan, combatLevelText, DetailsX1 + 113, DetailsY1 + 342, -1, true);
            PutText(FontSan, hitPointsText, DetailsX1 + 307, DetailsY1 + 313, -1, true);
            if (drawLoyaltyPanel)
                DrawLoyalty(DetailsX1 + 307, DetailsY1 + 342, loyalty, targetLoyalty);
        }

        if (firmBase.OwnFirm())
        {
            if (firmBase.CanInvoke())
            {
                bool mouseOnButton = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                     _mouseButtonY >= ButtonsBaseY + 2 && _mouseButtonY <= ButtonsBaseY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmapScaled(_buttonDownTexture, Button1X, ButtonsBaseY, _buttonDownWidth, _buttonDownHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonUpTexture, Button1X, ButtonsBaseY, _buttonUpWidth, _buttonUpHeight);
                Graphics.DrawBitmapScaled(_buttonInvokeTexture, Button1X + 8, ButtonsBaseY + 3, _buttonInvokeWidth, _buttonInvokeHeight);
            }
            else
            {
                Graphics.DrawBitmapScaled(_buttonDisabledTexture, Button1X, ButtonsBaseY, _buttonDisabledWidth, _buttonDisabledHeight);
                Graphics.DrawBitmapScaled(_buttonInvokeDisabledTexture, Button1X + 8, ButtonsBaseY + 3, _buttonInvokeWidth, _buttonInvokeHeight);
            }
            
            if (IsFirmBaseRewardEnabled(firmBase, overseer))
            {
                bool mouseOnButton = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                                     _mouseButtonY >= ButtonsBaseY + 2 && _mouseButtonY <= ButtonsBaseY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmapScaled(_buttonDownTexture, Button2X, ButtonsBaseY, _buttonDownWidth, _buttonDownHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonUpTexture, Button2X, ButtonsBaseY, _buttonUpWidth, _buttonUpHeight);
                Graphics.DrawBitmapScaled(_buttonRewardTexture, Button2X + 12, ButtonsBaseY + 4, _buttonRewardWidth, _buttonRewardHeight);
            }
            else
            {
                Graphics.DrawBitmapScaled(_buttonDisabledTexture, Button2X, ButtonsBaseY, _buttonDisabledWidth, _buttonDisabledHeight);
                Graphics.DrawBitmapScaled(_buttonRewardDisabledTexture, Button2X + 12, ButtonsBaseY + 4, _buttonRewardWidth, _buttonRewardHeight);
            }

            if (firmBase.HaveOwnWorkers())
            {
                bool mouseOnVacateButton = _mouseButtonX >= Button4X + 2 && _mouseButtonX <= Button4X + ButtonWidth &&
                                           _mouseButtonY >= ButtonsBaseY + 2 && _mouseButtonY <= ButtonsBaseY + ButtonHeight;
                if (_leftMousePressed && mouseOnVacateButton)
                    Graphics.DrawBitmapScaled(_buttonDownTexture, Button4X, ButtonsBaseY, _buttonDownWidth, _buttonDownHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonUpTexture, Button4X, ButtonsBaseY, _buttonUpWidth, _buttonUpHeight);
                Graphics.DrawBitmapScaled(_buttonRecruitTexture, Button4X + 3, ButtonsBaseY + 7, _buttonRecruitWidth, _buttonRecruitHeight);
            }
            else
            {
                Graphics.DrawBitmapScaled(_buttonDisabledTexture, Button4X, ButtonsBaseY, _buttonDisabledWidth, _buttonDisabledHeight);
                Graphics.DrawBitmapScaled(_buttonRecruitDisabledTexture, Button4X + 3, ButtonsBaseY + 7, _buttonRecruitWidth, _buttonRecruitHeight);
            }
        }
        
        if (IsFirmSpyListEnabled(firmBase))
        {
            bool mouseOnButton = _mouseButtonX >= Button3X + 2 && _mouseButtonX <= Button3X + ButtonWidth &&
                                 _mouseButtonY >= ButtonsBaseY + 2 && _mouseButtonY <= ButtonsBaseY + ButtonHeight;
            if (_leftMousePressed && mouseOnButton)
                Graphics.DrawBitmapScaled(_buttonDownTexture, Button3X, ButtonsBaseY, _buttonDownWidth, _buttonDownHeight);
            else
                Graphics.DrawBitmapScaled(_buttonUpTexture, Button3X, ButtonsBaseY, _buttonUpWidth, _buttonUpHeight);
            Graphics.DrawBitmapScaled(_buttonSpyMenuTexture, Button3X + 4, ButtonsBaseY + 16, _buttonSpyMenuWidth, _buttonSpyMenuHeight);
        }
        
        // TODO bribe and capture buttons
    }
    
    public void HandleBaseDetailsInput(FirmBase firmBase)
    {
        if (FirmDetailsMode == FirmDetailsMode.Spy)
        {
            HandleSpyList(firmBase.NationId, firmBase.GetPlayerSpies());
            return;
        }
        
        Unit overseer = (firmBase.OverseerId != 0) ? UnitArray[firmBase.OverseerId] : null;

        if (_leftMouseReleased && IsMouseOnFirmBaseLeaderIcon())
            firmBase.SelectedWorkerId = 0;

        if (_rightMouseReleased && IsMouseOnFirmBaseLeaderIcon() && firmBase.OwnFirm())
        {
            /*if (remote.is_enable())
            {
                // packet structure : <firm recno>
                short *shortPtr=(short *)remote.new_send_queue_msg(MSG_FIRM_MOBL_OVERSEER, sizeof(short));
                shortPtr[0] = firm_recno;
            }*/
            //else
            //{
                firmBase.AssignOverseer(0);
            //}
        }

        bool button1Pressed = _leftMouseReleased && _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                              _mouseButtonY >= ButtonsCampY + 2 && _mouseButtonY <= ButtonsCampY + ButtonHeight;
        bool button2Pressed = _leftMouseReleased && _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                              _mouseButtonY >= ButtonsCampY + 2 && _mouseButtonY <= ButtonsCampY + ButtonHeight;
        bool button3Pressed = _leftMouseReleased && _mouseButtonX >= Button3X + 2 && _mouseButtonX <= Button3X + ButtonWidth &&
                              _mouseButtonY >= ButtonsCampY + 2 && _mouseButtonY <= ButtonsCampY + ButtonHeight;
        bool button4Pressed = _leftMouseReleased && _mouseButtonX >= Button4X + 2 && _mouseButtonX <= Button4X + ButtonWidth &&
                              _mouseButtonY >= ButtonsCampY + 2 && _mouseButtonY <= ButtonsCampY + ButtonHeight;

        if (firmBase.OwnFirm())
        {
            if (button1Pressed && firmBase.CanInvoke())
            {
                /*if (remote.is_enable())
                {
			        // packet structure : <firm recno>
			        short *shortPtr=(short *)remote.new_send_queue_msg(MSG_F_BASE_INVOKE_GOD, sizeof(short));
			        shortPtr[0] = firm_recno;
                }*/
                //else
                //{
                    firmBase.InvokeGod();
                //}
            }
            
            if (button2Pressed && IsFirmBaseRewardEnabled(firmBase, overseer))
            {
                firmBase.Reward(firmBase.SelectedWorkerId, InternalConstants.COMMAND_PLAYER);
                SECtrl.immediate_sound("TURN_ON");
            }

            if (button4Pressed && firmBase.HaveOwnWorkers())
            {
                firmBase.MobilizeAllWorkers(InternalConstants.COMMAND_PLAYER);
            }
        }
        
        if (button3Pressed)
        {
            FirmDetailsMode = FirmDetailsMode.Spy;
        }
    }

    private bool IsMouseOnFirmBaseLeaderIcon()
    {
        return _mouseButtonX >= DetailsX1 + 12 && _mouseButtonX <= DetailsX1 + 104 &&
               _mouseButtonY >= DetailsY1 + 104 && _mouseButtonY <= DetailsY1 + 180;
    }
    
    private bool IsFirmBaseRewardEnabled(FirmBase firmBase, Unit overseer)
    {
        return NationArray.Player.Cash >= GameConstants.REWARD_COST &&
               ((overseer != null && overseer.Rank != Unit.RANK_KING) || firmBase.SelectedWorkerId != 0);
    }
}