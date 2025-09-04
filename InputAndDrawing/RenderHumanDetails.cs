using System;

namespace TenKingdoms;

public partial class Renderer
{
    public void DrawHumanDetails(UnitHuman unit)
    {
        DrawUnitPanel(DetailsX1 + 2, DetailsY1 + 48);
        string title = String.Empty;
        if (unit.RaceId != 0)
        {
            switch (unit.Rank)
            {
                case Unit.RANK_KING:
                    title = "King";
                    break;
                case Unit.RANK_GENERAL:
                    title = (unit.UnitMode == UnitConstants.UNIT_MODE_REBEL) ? "Rebel Leader" : "General";
                    break;
                case Unit.RANK_SOLDIER:
                    if (unit.ShouldShowInfo())
                    {
                        title = unit.Skill.SkillId switch
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
                        if (unit.Skill.SkillId == Skill.SKILL_LEADING)
                            title = "Soldier";
                        if (unit.IsCivilian())
                            title = "Civilian";
                    }
                    
                    if (unit.UnitMode == UnitConstants.UNIT_MODE_DEFEND_TOWN)
                        title = "Defending Villager";
                    if (unit.UnitMode == UnitConstants.UNIT_MODE_REBEL)
                        title = "Rebel";
                    if (UnitRes[unit.UnitType].unit_class == UnitConstants.UNIT_CLASS_GOD)
                        title = "Greater Being";
                    break;
            }
        }
        
        UnitInfo unitInfo = UnitRes[unit.UnitType];
        Graphics.DrawBitmap(unitInfo.GetLargeIconTexture(Graphics, unit.Rank), DetailsX1 + 12, DetailsY1 + 56,
            unitInfo.soldierIconWidth * 2, unitInfo.soldierIconHeight * 2);

        if (!String.IsNullOrEmpty(title))
        {
            PutTextCenter(FontSan, title, DetailsX1 + 10 + unitInfo.soldierIconWidth * 2, DetailsY1 + 56,
                DetailsX2 - 4, DetailsY1 + 56 + unitInfo.soldierIconHeight);
            PutTextCenter(FontSan, unit.GetUnitName(false), DetailsX1 + 10 + unitInfo.soldierIconWidth * 2, DetailsY1 + 56 + unitInfo.soldierIconHeight,
                DetailsX2 - 4, DetailsY1 + 56 + unitInfo.soldierIconHeight * 2);
        }
        else
        {
            PutTextCenter(FontSan, unit.GetUnitName(), DetailsX1 + 10 + unitInfo.soldierIconWidth * 2, DetailsY1 + 56,
                DetailsX2 - 4, DetailsY1 + 56 + unitInfo.soldierIconHeight * 2);
        }
        
        DrawPanelWithThreeFields(DetailsX1 + 2, DetailsY1 + 144);
        int combatPanelDY = 0;

        if (unit.Skill.SkillId != 0)
        {
            DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 149);
            PutText(FontSan, unit.Skill.SkillDescription(), DetailsX1 + 13, DetailsY1 + 152, -1, true);
            PutText(FontSan, unit.Skill.SkillLevel.ToString(), DetailsX1 + 113, DetailsY1 + 154, -1, true);
            combatPanelDY += 29;
        }

        DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 149 + combatPanelDY);
        PutText(FontSan, "Combat", DetailsX1 + 13, DetailsY1 + 152 + combatPanelDY, -1, true);
        PutText(FontSan, unit.Skill.CombatLevel.ToString(), DetailsX1 + 113, DetailsY1 + 154 + combatPanelDY, -1, true);

        if (unit.Rank != Unit.RANK_KING && !unit.IsCivilian())
        {
            DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 207);
            PutText(FontSan, "Contribution", DetailsX1 + 13, DetailsY1 + 210, -1, true);
            PutText(FontSan, unit.NationContribution.ToString(), DetailsX1 + 113, DetailsY1 + 212, -1, true);
        }

        if (unit.Rank != Unit.RANK_KING)
        {
            if (unit.SpyId != 0 && unit.TrueNationId() == NationArray.player_recno)
            {
                DrawFieldPanel62(DetailsX1 + 208, DetailsY1 + 149);
                PutText(FontSan, "Loyalty", DetailsX1 + 214, DetailsY1 + 152, -1, true);
                PutText(FontSan, SpyArray[unit.SpyId].SpyLoyalty.ToString(), DetailsX1 + 307, DetailsY1 + 154, -1, true);
            }
            else
            {
                if (unit.NationId != 0)
                {
                    DrawFieldPanel62(DetailsX1 + 208, DetailsY1 + 149);
                    PutText(FontSan, "Loyalty", DetailsX1 + 214, DetailsY1 + 152, -1, true);
                    PutText(FontSan, unit.Loyalty + " " + unit.TargetLoyalty, DetailsX1 + 307, DetailsY1 + 154, -1, true);
                }
            }
        }

        if (unit.SpyId != 0 && unit.TrueNationId() == NationArray.player_recno)
        {
            DrawFieldPanel62(DetailsX1 + 208, DetailsY1 + 178);
            PutText(FontSan, "Spying", DetailsX1 + 214, DetailsY1 + 181, -1, true);
            PutText(FontSan, SpyArray[unit.SpyId].SpySkill.ToString(), DetailsX1 + 307, DetailsY1 + 183, -1, true);
        }
        
        if (unit.IsOwnSpy())
            DrawSpyCloakPanel(unit);
    }

    private void DrawSpyCloakPanel(Unit unit)
    {
        bool canChangeToOtherNation = unit.CanSpyChangeNation();
        bool canChangeToOwnNation = canChangeToOtherNation || unit.NationId != unit.TrueNationId();
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
        Nation trueNation = NationArray[unit.TrueNationId()];
        foreach (Nation nation in NationArray)
        {
            if (canChangeToOtherNation)
            {
                if (!trueNation.get_relation(nation.nation_recno).has_contact)
                    continue;
            }
            else
            {
                if (nation != trueNation && nation.nation_recno != unit.NationId)
                    continue;
            }

            byte color = ColorRemap.GetColorRemap(ColorRemap.ColorSchemes[nation.nation_recno], false).MainColor;
            Graphics.DrawRect(DetailsX1 + 160 + colorDX, DetailsY1 + 404 + colorDY, 28, 28, color);
            DrawSpyColorFrame(DetailsX1 + 160 + colorDX, DetailsY1 + 404 + colorDY, unit.NationId == nation.nation_recno);
            
            if (colorDY != 0)
                colorDX += 40;

            colorDY = (colorDY == 0) ? 42 : 0;
        }

        if (canChangeToOtherNation)
        {
            Graphics.DrawRect(DetailsX1 + 160 + colorDX, DetailsY1 + 404 + colorDY, 28, 28, Colors.V_WHITE);
            DrawSpyColorFrame(DetailsX1 + 160 + colorDX, DetailsY1 + 404 + colorDY, unit.NationId == 0);
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
    
    public void HandleHumanDetailsInput(UnitHuman unit)
    {
        bool canChangeToOtherNation = unit.CanSpyChangeNation();
        bool canChangeToOwnNation = canChangeToOtherNation || unit.NationId != unit.TrueNationId();
        if (!canChangeToOwnNation || !unit.IsOwnSpy())
            return;
        
        int colorDX = 0;
        int colorDY = 0;
        Nation trueNation = NationArray[unit.TrueNationId()];
        foreach (Nation nation in NationArray)
        {
            if (canChangeToOtherNation)
            {
                if (!trueNation.get_relation(nation.nation_recno).has_contact)
                    continue;
            }
            else
            {
                if (nation != trueNation && nation.nation_recno != unit.NationId)
                    continue;
            }

            if (_mouseButtonX >= DetailsX1 + 160 + colorDX && _mouseButtonX <= DetailsX1 + 160 + colorDX + 28 &&
                _mouseButtonY >= DetailsY1 + 404 + colorDY && _mouseButtonY <= DetailsY1 + 404 + colorDY + 28)
            {
                unit.SpyChangeNation(nation.nation_recno, InternalConstants.COMMAND_PLAYER);
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
                unit.SpyChangeNation(0, InternalConstants.COMMAND_PLAYER);
            }
        }
    }
}