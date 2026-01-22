namespace TenKingdoms;

public partial class Renderer
{
    public void DrawCampDetails(FirmCamp camp)
    {
        DrawOverseer(camp);
        DrawWorkers(camp);

        Unit overseer = (camp.OverseerId != 0) ? UnitArray[camp.OverseerId] : null;
        
        DrawPanelWithTwoFields(DetailsX1 + 2, DetailsY1 + 303);
        DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 308);
        DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 337);
        DrawFieldPanel62(DetailsX1 + 208, DetailsY1 + 308);
        bool drawLoyaltyPanel = overseer == null || overseer.Rank != Unit.RANK_KING || camp.SelectedWorkerId != 0;
        if (drawLoyaltyPanel)
            DrawFieldPanel62(DetailsX1 + 208, DetailsY1 + 337);

        if (overseer != null || camp.SelectedWorkerId != 0)
        {
            PutText(FontSan, "Leadership", DetailsX1 + 13, DetailsY1 + 311, -1, true);
            PutText(FontSan, "Combat", DetailsX1 + 13, DetailsY1 + 340, -1, true);
            PutText(FontSan, "Hit Points", DetailsX1 + 214, DetailsY1 + 311, -1, true);
            if (drawLoyaltyPanel)
                PutText(FontSan, "Loyalty", DetailsX1 + 214, DetailsY1 + 340, -1, true);

            string leadershipText;
            string combatLevelText;
            string hitPointsText;
            int loyalty;
            int targetLoyalty;
            if (camp.SelectedWorkerId != 0)
            {
                Worker worker = camp.Workers[camp.SelectedWorkerId - 1];
                leadershipText = worker.SkillLevel.ToString();
                combatLevelText = worker.CombatLevel.ToString();
                hitPointsText = worker.HitPoints + "/" + worker.MaxHitPoints();
                loyalty = worker.Loyalty();
                targetLoyalty = worker.TargetLoyalty(camp.FirmId);
            }
            else
            {
                leadershipText = overseer.Skill.GetSkillLevel(Skill.SKILL_LEADING).ToString();
                combatLevelText = overseer.Skill.CombatLevel.ToString();
                hitPointsText = (int)overseer.HitPoints + "/" + overseer.MaxHitPoints;
                loyalty = overseer.Loyalty;
                targetLoyalty = overseer.TargetLoyalty;
            }
            PutText(FontSan, leadershipText, DetailsX1 + 113, DetailsY1 + 313, -1, true);
            PutText(FontSan, combatLevelText, DetailsX1 + 113, DetailsY1 + 342, -1, true);
            PutText(FontSan, hitPointsText, DetailsX1 + 307, DetailsY1 + 313, -1, true);
            if (drawLoyaltyPanel)
                DrawLoyalty(DetailsX1 + 307, DetailsY1 + 342, loyalty, targetLoyalty);
        }

        if (camp.OwnFirm())
        {
            if (IsCampPatrolEnabled(camp, overseer))
            {
                bool mouseOnButton = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                     _mouseButtonY >= ButtonsCampY + 2 && _mouseButtonY <= ButtonsCampY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmapScaled(_buttonDownTexture, Button1X, ButtonsCampY, _buttonDownWidth, _buttonDownHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonUpTexture, Button1X, ButtonsCampY, _buttonUpWidth, _buttonUpHeight);
                Graphics.DrawBitmapScaled(_buttonPatrolTexture, Button1X + 3, ButtonsCampY + 3, _buttonPatrolWidth, _buttonPatrolHeight);
            }
            else
            {
                Graphics.DrawBitmapScaled(_buttonDisabledTexture, Button1X, ButtonsCampY, _buttonDisabledWidth, _buttonDisabledHeight);
                Graphics.DrawBitmapScaled(_buttonPatrolDisabledTexture, Button1X + 3, ButtonsCampY + 3, _buttonPatrolWidth, _buttonPatrolHeight);
            }

            if (IsCampRewardEnabled(camp, overseer))
            {
                bool mouseOnButton = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                                     _mouseButtonY >= ButtonsCampY + 2 && _mouseButtonY <= ButtonsCampY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmapScaled(_buttonDownTexture, Button2X, ButtonsCampY, _buttonDownWidth, _buttonDownHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonUpTexture, Button2X, ButtonsCampY, _buttonUpWidth, _buttonUpHeight);
                Graphics.DrawBitmapScaled(_buttonRewardTexture, Button2X + 12, ButtonsCampY + 4, _buttonRewardWidth, _buttonRewardHeight);
            }
            else
            {
                Graphics.DrawBitmapScaled(_buttonDisabledTexture, Button2X, ButtonsCampY, _buttonDisabledWidth, _buttonDisabledHeight);
                Graphics.DrawBitmapScaled(_buttonRewardDisabledTexture, Button2X + 12, ButtonsCampY + 4, _buttonRewardWidth, _buttonRewardHeight);
            }
            
            bool mouseOnDefenseButton = _mouseButtonX >= Button4X + 2 && _mouseButtonX <= Button4X + ButtonWidth &&
                                 _mouseButtonY >= ButtonsCampY + 2 && _mouseButtonY <= ButtonsCampY + ButtonHeight;
            if (_leftMousePressed && mouseOnDefenseButton)
                Graphics.DrawBitmapScaled(_buttonDownTexture, Button4X, ButtonsCampY, _buttonDownWidth, _buttonDownHeight);
            else
                Graphics.DrawBitmapScaled(_buttonUpTexture, Button4X, ButtonsCampY, _buttonUpWidth, _buttonUpHeight);

            if (camp.DefenseFlag)
                Graphics.DrawBitmapScaled(_buttonDefenseOnTexture, Button4X + 10, ButtonsCampY + 4, _buttonDefenseOnWidth, _buttonDefenseOnHeight);
            else
                Graphics.DrawBitmapScaled(_buttonDefenseOffTexture, Button4X + 10, ButtonsCampY + 5, _buttonDefenseOffWidth, _buttonDefenseOffHeight);
        }
        
        if (IsFirmSpyListEnabled(camp))
        {
            bool mouseOnButton = _mouseButtonX >= Button3X + 2 && _mouseButtonX <= Button3X + ButtonWidth &&
                                 _mouseButtonY >= ButtonsCampY + 2 && _mouseButtonY <= ButtonsCampY + ButtonHeight;
            if (_leftMousePressed && mouseOnButton)
                Graphics.DrawBitmapScaled(_buttonDownTexture, Button3X, ButtonsCampY, _buttonDownWidth, _buttonDownHeight);
            else
                Graphics.DrawBitmapScaled(_buttonUpTexture, Button3X, ButtonsCampY, _buttonUpWidth, _buttonUpHeight);
            Graphics.DrawBitmapScaled(_buttonSpyMenuTexture, Button3X + 4, ButtonsCampY + 16, _buttonSpyMenuWidth, _buttonSpyMenuHeight);
        }
        
        // TODO spy list, bribe and capture buttons
    }
    
    public void HandleCampDetailsInput(FirmCamp camp)
    {
        Unit overseer = (camp.OverseerId != 0) ? UnitArray[camp.OverseerId] : null;

        if (_leftMouseReleased && IsMouseOnCampLeaderIcon())
            camp.SelectedWorkerId = 0;

        if (_rightMouseReleased && IsMouseOnCampLeaderIcon() && camp.OwnFirm())
        {
            /*if (remote.is_enable())
            {
                // packet structure : <firm recno>
                short *shortPtr=(short *)remote.new_send_queue_msg(MSG_FIRM_MOBL_OVERSEER, sizeof(short));
                shortPtr[0] = firm_recno;
            }*/
            //else
            //{
                camp.AssignOverseer(0);
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

        if (camp.OwnFirm())
        {
            if (button1Pressed && IsCampPatrolEnabled(camp, overseer))
            {
                /*if (remote.is_enable())
                {
                    // packet structure : <firm recno>
                    short *shortPtr=(short *)remote.new_send_queue_msg(MSG_F_CAMP_PATROL, sizeof(short));
                    shortPtr[0] = firm_recno;
                }*/
                //else
                //{
                    camp.Patrol();
                //}
            }
            
            if (button2Pressed && IsCampRewardEnabled(camp, overseer))
            {
                camp.Reward(camp.SelectedWorkerId, InternalConstants.COMMAND_PLAYER);
                SECtrl.immediate_sound("TURN_ON");
            }

            if (button4Pressed)
            {
                /*if (remote.is_enable())
                {
                    // packet structure : <firm recno> <defense_flag>
                    short *shortPtr=(short *)remote.new_send_queue_msg(MSG_F_CAMP_TOGGLE_PATROL, 2*sizeof(short));
                    shortPtr[0] = firm_recno;
                    shortPtr[1] = !defense_flag;
                }
                else
                {*/
                    camp.DefenseFlag = !camp.DefenseFlag;
                //}
                SECtrl.immediate_sound(camp.DefenseFlag ? "TURN_OFF" : "TURN_ON");
            }
        }
        
        if (button3Pressed)
        {
            //
        }
    }
    
    private bool IsMouseOnCampLeaderIcon()
    {
        return _mouseButtonX >= DetailsX1 + 12 && _mouseButtonX <= DetailsX1 + 104 &&
               _mouseButtonY >= DetailsY1 + 104 && _mouseButtonY <= DetailsY1 + 180;
    }
    
    private bool IsCampPatrolEnabled(Firm camp, Unit overseer)
    {
        return overseer != null || camp.Workers.Count > 0;
    }

    private bool IsCampRewardEnabled(Firm camp, Unit overseer)
    {
        return NationArray.Player.Cash >= GameConstants.REWARD_COST &&
               ((overseer != null && overseer.Rank != Unit.RANK_KING) || camp.SelectedWorkerId != 0);
    }
}