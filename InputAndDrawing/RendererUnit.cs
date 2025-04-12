using System;

namespace TenKingdoms;

public partial class Renderer
{
    private bool ShouldShowInfo(Unit unit)
    {
        if (Config.show_ai_info || unit.is_own())
            return true;
        
        if (NationArray.player_recno != 0 && NationArray.player.revealed_by_phoenix(unit.next_x_loc(), unit.next_y_loc()))
            return true;

        return false;
    }
    
    private void DrawUnitDetails(Unit unit)
    {
        DrawSmallPanel(DetailsX1 + 2, DetailsY1);
        if (unit.nation_recno != 0)
        {
            int textureKey = ColorRemap.GetTextureKey(ColorRemap.ColorSchemes[unit.nation_recno], false);
            Graphics.DrawBitmap(_colorSquareTextures[textureKey], DetailsX1 + 10, DetailsY1 + 3, _colorSquareWidth * 2, _colorSquareHeight * 2);
        }
        // TODO draw hit points bar and X button
        
        DrawUnitPanel(DetailsX1 + 2, DetailsY1 + 48);
        string title = String.Empty;
        if (unit.race_id != 0)
        {
            switch (unit.rank_id)
            {
                case Unit.RANK_KING:
                    title = "King";
                    break;
                case Unit.RANK_GENERAL:
                    title = (unit.unit_mode == UnitConstants.UNIT_MODE_REBEL) ? "Rebel Leader" : "General";
                    break;
                case Unit.RANK_SOLDIER:
                    if (ShouldShowInfo(unit))
                    {
                        title = unit.skill.skill_id switch
                        {
                            Skill.SKILL_LEADING => "Soldier",
                            Skill.SKILL_CONSTRUCTION => "Construction Worker",
                            Skill.SKILL_MINING => "Miner",
                            Skill.SKILL_MFT => "Worker",
                            Skill.SKILL_RESEARCH => "Scientist",
                            Skill.SKILL_SPYING => "Spy",
                            _ => "Peasant"
                        };
                    }
                    else
                    {
                        if (unit.skill.skill_id == Skill.SKILL_LEADING)
                            title = "Soldier";
                        if (unit.is_civilian())
                            title = "Civilian";
                    }
                    
                    if (unit.unit_mode == UnitConstants.UNIT_MODE_DEFEND_TOWN)
                        title = "Defending Villager";
                    if (unit.unit_mode == UnitConstants.UNIT_MODE_REBEL)
                        title = "Rebel";
                    if (UnitRes[unit.unit_id].unit_class == UnitConstants.UNIT_CLASS_GOD)
                        title = "Greater Being";
                    break;
            }
        }
        
        UnitInfo unitInfo = UnitRes[unit.unit_id];
        Graphics.DrawBitmap(unitInfo.GetLargeIconTexture(Graphics, unit.rank_id), DetailsX1 + 12, DetailsY1 + 56,
            unitInfo.soldierIconWidth * 2, unitInfo.soldierIconHeight * 2);

        if (!String.IsNullOrEmpty(title))
        {
            PutTextCenter(FontSan, title, DetailsX1 + 10 + unitInfo.soldierIconWidth * 2, DetailsY1 + 56,
                DetailsX2 - 4, DetailsY1 + 56 + unitInfo.soldierIconHeight);
            PutTextCenter(FontSan, unit.unit_name(0), DetailsX1 + 10 + unitInfo.soldierIconWidth * 2, DetailsY1 + 56 + unitInfo.soldierIconHeight,
                DetailsX2 - 4, DetailsY1 + 56 + unitInfo.soldierIconHeight * 2);
        }
        else
        {
            PutTextCenter(FontSan, unit.unit_name(), DetailsX1 + 10 + unitInfo.soldierIconWidth * 2, DetailsY1 + 56,
                DetailsX2 - 4, DetailsY1 + 56 + unitInfo.soldierIconHeight * 2);
        }
        
        DrawPanelWithThreeFields(DetailsX1 + 2, DetailsY1 + 144);
        int combatPanelDY = 0;

        if (unit.skill.skill_id != 0)
        {
            DrawFieldPanel1(DetailsX1 + 7, DetailsY1 + 149);
            PutText(FontSan, unit.skill.skill_des(), DetailsX1 + 13, DetailsY1 + 152, -1, true);
            PutText(FontSan, unit.skill.skill_level.ToString(), DetailsX1 + 113, DetailsY1 + 154, -1, true);
            combatPanelDY += 29;
        }

        DrawFieldPanel1(DetailsX1 + 7, DetailsY1 + 149 + combatPanelDY);
        PutText(FontSan, "Combat", DetailsX1 + 13, DetailsY1 + 152 + combatPanelDY, -1, true);
        PutText(FontSan, unit.skill.combat_level.ToString(), DetailsX1 + 113, DetailsY1 + 154 + combatPanelDY, -1, true);

        if (unit.rank_id != Unit.RANK_KING && !unit.is_civilian())
        {
            DrawFieldPanel1(DetailsX1 + 7, DetailsY1 + 207);
            PutText(FontSan, "Contribution", DetailsX1 + 13, DetailsY1 + 210, -1, true);
            PutText(FontSan, unit.nation_contribution.ToString(), DetailsX1 + 113, DetailsY1 + 212, -1, true);
        }

        if (unit.rank_id != Unit.RANK_KING)
        {
            if (unit.spy_recno != 0 && unit.true_nation_recno() == NationArray.player_recno)
            {
                DrawFieldPanel2(DetailsX1 + 208, DetailsY1 + 149);
                PutText(FontSan, "Loyalty", DetailsX1 + 214, DetailsY1 + 152, -1, true);
                PutText(FontSan, SpyArray[unit.spy_recno].spy_loyalty.ToString(), DetailsX1 + 307, DetailsY1 + 154, -1, true);
            }
            else
            {
                if (unit.nation_recno != 0)
                {
                    DrawFieldPanel2(DetailsX1 + 208, DetailsY1 + 149);
                    PutText(FontSan, "Loyalty", DetailsX1 + 214, DetailsY1 + 152, -1, true);
                    PutText(FontSan, unit.loyalty + " " + unit.target_loyalty, DetailsX1 + 307, DetailsY1 + 154, -1, true);
                }
            }
        }

        if (unit.spy_recno != 0 && unit.true_nation_recno() == NationArray.player_recno)
        {
            DrawFieldPanel2(DetailsX1 + 208, DetailsY1 + 178);
            PutText(FontSan, "Spying", DetailsX1 + 214, DetailsY1 + 181, -1, true);
            PutText(FontSan, SpyArray[unit.spy_recno].spy_skill.ToString(), DetailsX1 + 307, DetailsY1 + 183, -1, true);
        }
        
        if (unit.is_own_spy())
            DrawSpyCloakPanel(unit);
    }

    private void DrawSpyCloakPanel(Unit unit)
    {
        bool canChangeToOtherNation = unit.can_spy_change_nation();
        bool canChangeToOwnNation = canChangeToOtherNation || unit.nation_recno != unit.true_nation_recno();
        if (!canChangeToOwnNation)
        {
            DrawSmallPanel(DetailsX1 + 2, DetailsY1 + 392);
            PutTextCenter(FontSan, "Enemies Nearby", DetailsX1 + 2, DetailsY1 + 392, DetailsX2 - 4, DetailsY1 + 434);
            return;
        }
        
        DrawPanelWithThreeFields(DetailsX1 + 2, DetailsY1 + 392);
        PutTextCenter(FontSan, "Spy", DetailsX1 + 5, DetailsY1 + 402, DetailsX1 + 160, DetailsY1 + 436);
        PutTextCenter(FontSan, "Cloak", DetailsX1 + 5, DetailsY1 + 442, DetailsX1 + 160, DetailsY1 + 476);
        int colorDX = 0;
        int colorDY = 0;
        Nation trueNation = NationArray[unit.true_nation_recno()];
        foreach (Nation nation in NationArray)
        {
            if (canChangeToOtherNation)
            {
                if (!trueNation.get_relation(nation.nation_recno).has_contact)
                    continue;
            }
            else
            {
                if (nation != trueNation && nation.nation_recno != unit.nation_recno)
                    continue;
            }

            byte color = ColorRemap.GetColorRemap(ColorRemap.ColorSchemes[nation.nation_recno], false).MainColor;
            Graphics.DrawRect(DetailsX1 + 160 + colorDX, DetailsY1 + 404 + colorDY, 28, 28, color);
            DrawSpyColorFrame(DetailsX1 + 160 + colorDX, DetailsY1 + 404 + colorDY, unit.nation_recno == nation.nation_recno);
            
            if (colorDY != 0)
                colorDX += 40;

            colorDY = (colorDY == 0) ? 42 : 0;
        }

        if (canChangeToOtherNation)
        {
            Graphics.DrawRect(DetailsX1 + 160 + colorDX, DetailsY1 + 404 + colorDY, 28, 28, Colors.V_WHITE);
            DrawSpyColorFrame(DetailsX1 + 160 + colorDX, DetailsY1 + 404 + colorDY, unit.nation_recno == 0);
        }
    }

    private void DrawSpyColorFrame(int x, int y, bool selected)
    {
        byte color = selected ? Colors.V_YELLOW : (byte)(Colors.VGA_GRAY + 8);
        Graphics.DrawRect(x - 3, y - 3, 28 + 6, 3, color);
        Graphics.DrawRect(x - 3, y - 3, 3, 28 + 6, color);
        Graphics.DrawRect(x - 3, y + 28, 28 + 6, 3, color);
        Graphics.DrawRect(x + 28, y - 3, 3, 28 + 6, color);
    }
    
    private void HandleUnitDetailsInput(Unit unit)
    {
        bool canChangeToOtherNation = unit.can_spy_change_nation();
        bool canChangeToOwnNation = canChangeToOtherNation || unit.nation_recno != unit.true_nation_recno();
        if (!canChangeToOwnNation || !unit.is_own_spy())
            return;
        
        int colorDX = 0;
        int colorDY = 0;
        Nation trueNation = NationArray[unit.true_nation_recno()];
        foreach (Nation nation in NationArray)
        {
            if (canChangeToOtherNation)
            {
                if (!trueNation.get_relation(nation.nation_recno).has_contact)
                    continue;
            }
            else
            {
                if (nation != trueNation && nation.nation_recno != unit.nation_recno)
                    continue;
            }

            if (_mouseButtonX >= DetailsX1 + 160 + colorDX && _mouseButtonX <= DetailsX1 + 160 + colorDX + 28 &&
                _mouseButtonY >= DetailsY1 + 404 + colorDY && _mouseButtonY <= DetailsY1 + 404 + colorDY + 28)
            {
                unit.spy_change_nation(nation.nation_recno, InternalConstants.COMMAND_PLAYER);
                return;
            }
            
            if (colorDY != 0)
                colorDX += 40;

            colorDY = (colorDY == 0) ? 42 : 0;
        }

        if (canChangeToOtherNation)
        {
            if (_mouseButtonX >= DetailsX1 + 160 + colorDX && _mouseButtonX <= DetailsX1 + 160 + colorDX + 28 &&
                _mouseButtonY >= DetailsY1 + 404 + colorDY && _mouseButtonY <= DetailsY1 + 404 + colorDY + 28)
            {
                unit.spy_change_nation(0, InternalConstants.COMMAND_PLAYER);
            }
        }
    }
}