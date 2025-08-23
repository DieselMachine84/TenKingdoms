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
            
            if (_selectedWorkerId == 0)
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
                PutText(FontSan, overseer.GetUnitName(false), DetailsX1 + 111, DetailsY1 + 96);
                PutText(FontSan, "Leadership: " + overseer.Skill.GetSkillLevel(Skill.SKILL_LEADING), DetailsX1 + 111, DetailsY1 + 126);
                PutText(FontSan, "Loyalty: " + overseer.Loyalty + " " + overseer.TargetLoyalty, DetailsX1 + 111, DetailsY1 + 156);
                // TODO loyalty arrow
                // TODO spy icon
            }
        }
        
        DrawWorkers(camp, DetailsY1 + 192);

        DrawPanelWithTwoFields(DetailsX1 + 2, DetailsY1 + 303);
        DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 308);
        DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 337);
        DrawFieldPanel62(DetailsX1 + 208, DetailsY1 + 308);
        if (overseer == null || overseer.Rank != Unit.RANK_KING)
            DrawFieldPanel62(DetailsX1 + 208, DetailsY1 + 337);

        if (_selectedWorkerId == 0 && overseer != null)
        {
            PutText(FontSan, "Leadership", DetailsX1 + 13, DetailsY1 + 311, -1, true);
            PutText(FontSan, overseer.Skill.GetSkillLevel(Skill.SKILL_LEADING).ToString(), DetailsX1 + 113, DetailsY1 + 313, -1, true);
            PutText(FontSan, "Combat", DetailsX1 + 13, DetailsY1 + 340, -1, true);
            PutText(FontSan, overseer.Skill.CombatLevel.ToString(), DetailsX1 + 113, DetailsY1 + 342, -1, true);
            PutText(FontSan, "Hit Points", DetailsX1 + 214, DetailsY1 + 311, -1, true);
            PutText(FontSan, (int)overseer.HitPoints + "/" + overseer.MaxHitPoints, DetailsX1 + 307, DetailsY1 + 313, -1, true);
            if (overseer.Rank != Unit.RANK_KING)
            {
                PutText(FontSan, "Loyalty", DetailsX1 + 214, DetailsY1 + 340, -1, true);
                PutText(FontSan, overseer.Loyalty + " " + overseer.TargetLoyalty, DetailsX1 + 307, DetailsY1 + 342, -1, true);
            }
        }
        else
        {
            // TODO draw worker details
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

            if (IsCampRewardEnabled(overseer))
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

        bool button1Pressed = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                              _mouseButtonY >= ButtonsCampY + 2 && _mouseButtonY <= ButtonsCampY + ButtonHeight;
        bool button2Pressed = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                              _mouseButtonY >= ButtonsCampY + 2 && _mouseButtonY <= ButtonsCampY + ButtonHeight;
        bool button3Pressed = _mouseButtonX >= Button3X + 2 && _mouseButtonX <= Button3X + ButtonWidth &&
                              _mouseButtonY >= ButtonsCampY + 2 && _mouseButtonY <= ButtonsCampY + ButtonHeight;
        bool button4Pressed = _mouseButtonX >= Button4X + 2 && _mouseButtonX <= Button4X + ButtonWidth &&
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
            
            if (button2Pressed && IsCampRewardEnabled(overseer))
            {
                camp.Reward(_selectedWorkerId, InternalConstants.COMMAND_PLAYER);
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
    
    private bool IsCampPatrolEnabled(Firm firm, Unit overseer)
    {
        return overseer != null || firm.Workers.Count > 0;
    }

    private bool IsCampRewardEnabled(Unit overseer)
    {
        return NationArray.player.cash >= GameConstants.REWARD_COST &&
               (overseer != null && overseer.Rank != Unit.RANK_KING || _selectedWorkerId != 0);
    }
}