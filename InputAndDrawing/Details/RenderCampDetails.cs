namespace TenKingdoms;

public partial class Renderer
{
    public void DrawCampDetails(FirmCamp camp)
    {
        DrawOverseerPanel(DetailsX1 + 2, DetailsY1 + 96);
        Unit overseer = null;
        if (camp.OverseerId != 0)
            overseer = UnitArray[camp.OverseerId];
        
        if (overseer != null)
        {
            UnitInfo unitInfo = UnitRes[overseer.UnitType];
            Graphics.DrawBitmap(unitInfo.GetLargeIconTexture(Graphics, overseer.Rank), DetailsX1 + 12, DetailsY1 + 104,
                unitInfo.soldierIconWidth * 2, unitInfo.soldierIconHeight * 2);
            
            if (camp.SelectedWorkerId == 0)
            {
                Graphics.DrawRect(DetailsX1 + 9, DetailsY1 + 101, unitInfo.soldierIconWidth * 2 + 6, 3, Colors.V_YELLOW);
                Graphics.DrawRect(DetailsX1 + 9, DetailsY1 + 101 + unitInfo.soldierIconHeight * 2 + 3,
                    unitInfo.soldierIconWidth * 2 + 6, 3, Colors.V_YELLOW);
                Graphics.DrawRect(DetailsX1 + 9, DetailsY1 + 101, 3, unitInfo.soldierIconHeight * 2 + 6, Colors.V_YELLOW);
                Graphics.DrawRect(DetailsX1 + 9 + unitInfo.soldierIconWidth * 2 + 3, DetailsY1 + 101,
                    3, unitInfo.soldierIconHeight * 2 + 6, Colors.V_YELLOW);
            }

            if (overseer.Rank == Unit.RANK_KING)
            {
                PutText(FontSan, "King", DetailsX1 + 111, DetailsY1 + 96);
                PutText(FontSan, overseer.GetUnitName(false), DetailsX1 + 111, DetailsY1 + 126);
                PutText(FontSan, "Leadership: " + overseer.Skill.GetSkillLevel(Skill.SKILL_LEADING), DetailsX1 + 111, DetailsY1 + 156);
            }
            else
            {
                string leadershipText = "Leadership: " + overseer.Skill.GetSkillLevel(Skill.SKILL_LEADING);
                string loyaltyText = "Loyalty: " + overseer.Loyalty;
                PutText(FontSan, overseer.GetUnitName(false), DetailsX1 + 111, DetailsY1 + 96);
                PutText(FontSan, leadershipText, DetailsX1 + 111, DetailsY1 + 126);
                PutText(FontSan, loyaltyText, DetailsX1 + 111, DetailsY1 + 156);
                int targetLoyalty = overseer.TargetLoyalty;
                if (NationArray[camp.NationId].cash <= 0.0)
                    targetLoyalty = 0;
                if (targetLoyalty != overseer.Loyalty)
                {
                    int targetLoyaltyX = DetailsX1 + 111 + FontSan.TextWidth(loyaltyText) + 2;
                    int targetLoyaltyY = DetailsY1 + 156;
                    if (targetLoyalty < overseer.Loyalty)
                        Graphics.DrawBitmap(_arrowDownTexture, targetLoyaltyX, targetLoyaltyY + 4, _arrowDownWidth * 2, _arrowDownHeight * 2);
                    if (targetLoyalty > overseer.Loyalty)
                        Graphics.DrawBitmap(_arrowUpTexture, targetLoyaltyX, targetLoyaltyY + 4, _arrowUpWidth * 2, _arrowUpHeight * 2);
                    PutText(FontSan, overseer.TargetLoyalty.ToString(), targetLoyaltyX + 16, targetLoyaltyY);
                }
                if (overseer.SpyId != 0 && (overseer.TrueNationId() == NationArray.player_recno || Config.show_ai_info))
                    DrawSpyIcon(DetailsX1 + 111 + FontSan.TextWidth(leadershipText) + 2, DetailsY1 + 132, overseer.TrueNationId());
            }
        }
        
        DrawWorkers(camp);

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
            {
                PutText(FontSan, loyalty.ToString(), DetailsX1 + 307, DetailsY1 + 342, -1, true);
                if (loyalty != targetLoyalty)
                {
                    int targetLoyaltyX = DetailsX1 + 308 + FontSan.TextWidth(loyalty.ToString()) * 2 / 3;
                    int targetLoyaltyY = DetailsY1 + 342;
                    if (targetLoyalty < loyalty)
                        Graphics.DrawBitmap(_arrowDownTexture, targetLoyaltyX, targetLoyaltyY + 3, _arrowDownWidth, _arrowDownHeight);
                    if (targetLoyalty > loyalty)
                        Graphics.DrawBitmap(_arrowUpTexture, targetLoyaltyX, targetLoyaltyY + 3, _arrowUpWidth, _arrowUpHeight);
                    PutText(FontSan, targetLoyalty.ToString(), targetLoyaltyX + 8, targetLoyaltyY, -1, true);
                }
            }
        }

        if (camp.OwnFirm())
        {
            if (IsCampPatrolEnabled(camp, overseer))
            {
                bool mouseOnButton = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                     _mouseButtonY >= ButtonsCampY + 2 && _mouseButtonY <= ButtonsCampY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmap(_buttonDownTexture, Button1X, ButtonsCampY, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
                else
                    Graphics.DrawBitmap(_buttonUpTexture, Button1X, ButtonsCampY, Scale(_buttonUpWidth), Scale(_buttonUpHeight));
                Graphics.DrawBitmap(_buttonPatrolTexture, Button1X + 3, ButtonsCampY + 3, Scale(_buttonPatrolWidth), Scale(_buttonPatrolHeight));
            }
            else
            {
                Graphics.DrawBitmap(_buttonDisabledTexture, Button1X, ButtonsCampY, Scale(_buttonDisabledWidth), Scale(_buttonDisabledHeight));
                Graphics.DrawBitmap(_buttonPatrolDisabledTexture, Button1X + 3, ButtonsCampY + 3, Scale(_buttonPatrolWidth), Scale(_buttonPatrolHeight));
            }

            if (IsCampRewardEnabled(camp, overseer))
            {
                bool mouseOnButton = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                                     _mouseButtonY >= ButtonsCampY + 2 && _mouseButtonY <= ButtonsCampY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmap(_buttonDownTexture, Button2X, ButtonsCampY, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
                else
                    Graphics.DrawBitmap(_buttonUpTexture, Button2X, ButtonsCampY, Scale(_buttonUpWidth), Scale(_buttonUpHeight));
                Graphics.DrawBitmap(_buttonRewardTexture, Button2X + 12, ButtonsCampY + 4, Scale(_buttonRewardWidth), Scale(_buttonRewardHeight));
            }
            else
            {
                Graphics.DrawBitmap(_buttonDisabledTexture, Button2X, ButtonsCampY, Scale(_buttonDisabledWidth), Scale(_buttonDisabledHeight));
                Graphics.DrawBitmap(_buttonRewardDisabledTexture, Button2X + 12, ButtonsCampY + 4, Scale(_buttonRewardWidth), Scale(_buttonRewardHeight));
            }
            
            if (IsFirmSpyListEnabled(camp))
            {
                bool mouseOnButton = _mouseButtonX >= Button3X + 2 && _mouseButtonX <= Button3X + ButtonWidth &&
                                     _mouseButtonY >= ButtonsCampY + 2 && _mouseButtonY <= ButtonsCampY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmap(_buttonDownTexture, Button3X, ButtonsCampY, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
                else
                    Graphics.DrawBitmap(_buttonUpTexture, Button3X, ButtonsCampY, Scale(_buttonUpWidth), Scale(_buttonUpHeight));
                Graphics.DrawBitmap(_buttonSpyMenuTexture, Button3X + 4, ButtonsCampY + 16, Scale(_buttonSpyMenuWidth), Scale(_buttonSpyMenuHeight));
            }

            bool mouseOnDefenseButton = _mouseButtonX >= Button4X + 2 && _mouseButtonX <= Button4X + ButtonWidth &&
                                 _mouseButtonY >= ButtonsCampY + 2 && _mouseButtonY <= ButtonsCampY + ButtonHeight;
            if (_leftMousePressed && mouseOnDefenseButton)
                Graphics.DrawBitmap(_buttonDownTexture, Button4X, ButtonsCampY, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
            else
                Graphics.DrawBitmap(_buttonUpTexture, Button4X, ButtonsCampY, Scale(_buttonUpWidth), Scale(_buttonUpHeight));

            if (camp.defense_flag)
                Graphics.DrawBitmap(_buttonDefenseOnTexture, Button4X + 10, ButtonsCampY + 4, Scale(_buttonDefenseOnWidth), Scale(_buttonDefenseOnHeight));
            else
                Graphics.DrawBitmap(_buttonDefenseOffTexture, Button4X + 10, ButtonsCampY + 5, Scale(_buttonDefenseOffWidth), Scale(_buttonDefenseOffHeight));
        }
        else
        {
            // TODO spy list, bribe and capture buttons
        }
    }
    
    public void HandleCampDetailsInput(FirmCamp camp)
    {
        Unit overseer = null;
        if (camp.OverseerId != 0)
            overseer = UnitArray[camp.OverseerId];

        if (_leftMouseReleased && IsMouseOnCampLeaderIcon())
            camp.SelectedWorkerId = 0;

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
                }
                else
                {*/
                    camp.patrol();
                //}
            }
            
            if (button2Pressed && IsCampRewardEnabled(camp, overseer))
            {
                camp.Reward(camp.SelectedWorkerId, InternalConstants.COMMAND_PLAYER);
                SECtrl.immediate_sound("TURN_ON");
            }

            if (button3Pressed)
            {
                //
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
                    camp.defense_flag = !camp.defense_flag;
                //}
                SECtrl.immediate_sound(camp.defense_flag ? "TURN_OFF" : "TURN_ON");
            }
        }
    }
    
    private bool IsMouseOnCampLeaderIcon()
    {
        return _mouseButtonX >= DetailsX1 + 12 && _mouseButtonX <= DetailsX1 + 104 &&
               _mouseButtonY >= DetailsY1 + 104 && _mouseButtonY <= DetailsY1 + 180;
    }
    
    private bool IsCampPatrolEnabled(Firm firm, Unit overseer)
    {
        return overseer != null || firm.Workers.Count > 0;
    }

    private bool IsCampRewardEnabled(Firm firm, Unit overseer)
    {
        return NationArray.player.cash >= GameConstants.REWARD_COST &&
               (overseer != null && overseer.Rank != Unit.RANK_KING || firm.SelectedWorkerId != 0);
    }
}