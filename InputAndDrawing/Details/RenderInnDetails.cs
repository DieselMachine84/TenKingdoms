namespace TenKingdoms;

public partial class Renderer
{
    public void DrawInnDetails(FirmInn inn)
    {
        //TODO scroll
        DrawListBox4Panel(DetailsX1 + 2, DetailsY1 + 96);
        
        if (_selectedInnUnit != null && !inn.InnUnits.Contains(_selectedInnUnit))
            _selectedInnUnit = null;
        
        if (inn.InnUnits.Count > 0 && _selectedInnUnit == null)
            _selectedInnUnit = inn.InnUnits[0];
        
        int innUnitY = DetailsY1 + 110;
        for (int i = 0; i < inn.InnUnits.Count && i < 4; i++)
        {
            InnUnit innUnit = inn.InnUnits[i];
            UnitInfo unitInfo = UnitRes[innUnit.UnitType];
            Graphics.DrawBitmap(unitInfo.GetSoldierSmallIconTexture(Graphics), DetailsX1 + 14, innUnitY, unitInfo.SoldierIconWidth, unitInfo.SoldierIconHeight);
            PutText(FontSan, innUnit.Skill.SkillDescription(), DetailsX1 + 70, innUnitY + 5);
            PutText(FontSan, innUnit.Skill.SkillLevel.ToString(), DetailsX1 + 250, innUnitY + 5);
            PutText(FontSan, "$" + innUnit.HireCost, DetailsX1 + 330, innUnitY + 5);
            
            if (innUnit == _selectedInnUnit)
                DrawSelectedBorder(DetailsX1 + 8, innUnitY - 7, DetailsX1 + 405, innUnitY + 47);

            innUnitY += ListItemHeight;
        }
        
        DrawPanelWithTwoFields(DetailsX1 + 2, DetailsY1 + 334);
        if (_selectedInnUnit != null)
        {
            DrawFieldPanel75(DetailsX1 + 7, DetailsY1 + 339);
            DrawFieldPanel75(DetailsX1 + 7, DetailsY1 + 368);
            DrawFieldPanel75(DetailsX1 + 208, DetailsY1 + 339);
            PutText(FontSan, _selectedInnUnit.Skill.SkillDescription(), DetailsX1 + 13, DetailsY1 + 344, -1, true);
            PutText(FontSan, _selectedInnUnit.Skill.SkillLevel.ToString(), DetailsX1 + 125, DetailsY1 + 346, -1, true);
            PutText(FontSan, "Combat", DetailsX1 + 13, DetailsY1 + 373, -1, true);
            PutText(FontSan, _selectedInnUnit.Skill.CombatLevel.ToString(), DetailsX1 + 125, DetailsY1 + 375, -1, true);
            PutText(FontSan, "Hiring Cost", DetailsX1 + 214, DetailsY1 + 344, -1, true);
            PutText(FontSan, "$" + _selectedInnUnit.HireCost, DetailsX1 + 327, DetailsY1 + 346, -1, true);
        }

        if (inn.OwnFirm())
        {
            if (inn.InnUnits.Count > 0 && _selectedInnUnit != null && NationArray.Player != null && NationArray.Player.Cash >= _selectedInnUnit.HireCost)
            {
                bool mouseOnButton = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                     _mouseButtonY >= ButtonsInnY + 2 && _mouseButtonY <= ButtonsInnY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmapScaled(_buttonDownTexture, Button1X, ButtonsInnY, _buttonDownWidth, _buttonDownHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonUpTexture, Button1X, ButtonsInnY, _buttonUpWidth, _buttonUpHeight);
                Graphics.DrawBitmapScaled(_buttonHireUnitTexture, Button1X + 7, ButtonsInnY + 9, _buttonHireUnitWidth, _buttonHireUnitHeight);
            }
            else
            {
                Graphics.DrawBitmapScaled(_buttonDisabledTexture, Button1X, ButtonsInnY, _buttonDisabledWidth, _buttonDisabledHeight);
                Graphics.DrawBitmapScaled(_buttonHireUnitDisabledTexture, Button1X + 7, ButtonsInnY + 9, _buttonHireUnitWidth, _buttonHireUnitHeight);
            }
        }
    }
    
    public void HandleInnDetailsInput(FirmInn inn)
    {
        if (!inn.ShouldShowInfo())
            return;
        
        bool innUnit1Selected = _leftMouseReleased && _mouseButtonX >= DetailsX1 + 11 && _mouseButtonX <= DetailsX1 + 402 &&
                             _mouseButtonY >= DetailsY1 + 105 && _mouseButtonY <= DetailsY1 + 153;
        bool innUnit2Selected = _leftMouseReleased && _mouseButtonX >= DetailsX1 + 11 && _mouseButtonX <= DetailsX1 + 402 &&
                             _mouseButtonY >= DetailsY1 + 105 + ListItemHeight && _mouseButtonY <= DetailsY1 + 153 + ListItemHeight;
        bool innUnit3Selected = _leftMouseReleased && _mouseButtonX >= DetailsX1 + 11 && _mouseButtonX <= DetailsX1 + 402 &&
                             _mouseButtonY >= DetailsY1 + 105 + 2 * ListItemHeight && _mouseButtonY <= DetailsY1 + 153 + 2 * ListItemHeight;
        bool innUnit4Selected = _leftMouseReleased && _mouseButtonX >= DetailsX1 + 11 && _mouseButtonX <= DetailsX1 + 402 &&
                                _mouseButtonY >= DetailsY1 + 105 + 3 * ListItemHeight && _mouseButtonY <= DetailsY1 + 153 + 3 * ListItemHeight;

        int innUnitIndex = 0;
        for (int i = 0; i < inn.InnUnits.Count && i < 4; i++)
        {
            innUnitIndex++;
            if ((innUnit1Selected && innUnitIndex == 1) || (innUnit2Selected && innUnitIndex == 2) ||
                (innUnit3Selected && innUnitIndex == 3) || (innUnit4Selected && innUnitIndex == 4))
            {
                _selectedInnUnit = inn.InnUnits[i];
                break;
            }
        }

        if (inn.OwnFirm() && _selectedInnUnit != null)
        {
            bool mouseOnButton1 = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                  _mouseButtonY >= ButtonsInnY + 2 && _mouseButtonY <= ButtonsInnY + ButtonHeight;

            if (_leftMouseReleased && mouseOnButton1)
            {
                /*if (remote.is_enable())
                {
                    InnUnit* innUnit = &inn_unit_array[browse_hire.recno() - 1];
                    // packet structure : <firm recno> <unit id> <combat level> <skill id> <skill_level> <hire cost> <spy recno> <nation no>
                    short* shortPtr = (short*)remote.new_send_queue_msg(MSG_F_INN_HIRE, 8 * sizeof(short));
                    shortPtr[0] = firm_recno;
                    shortPtr[1] = innUnit->unit_id;
                    shortPtr[2] = innUnit->skill.combat_level;
                    shortPtr[3] = innUnit->skill.skill_id;
                    shortPtr[4] = innUnit->skill.skill_level;
                    shortPtr[5] = innUnit->hire_cost;
                    shortPtr[6] = innUnit->spy_recno;
                    shortPtr[7] = nation_recno;
                }*/
                //else
                //{
                    inn.Hire(inn.InnUnits.IndexOf(_selectedInnUnit) + 1);
                //}
            }

            SERes.far_sound(inn.LocCenterX, inn.LocCenterY, 1, 'S', UnitRes[_selectedInnUnit.UnitType].SpriteId, "RDY");
        }
    }
}