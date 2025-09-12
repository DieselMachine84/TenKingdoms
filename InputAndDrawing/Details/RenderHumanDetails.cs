using System;

namespace TenKingdoms;

enum HumanDetailsMode { Normal, Settle, BuildMenu, Build }

public partial class Renderer
{
    // TODO main menu
    // TODO draw fields
    // TODO draw and handle buttons
    // TODO draw and handle spy panel
    // TODO draw spy loyalty or loyalty?
    // TODO draw loyalty arrow
    // TODO group change aggressive mode
    // TODO group reward
    // TODO group settle
    // TODO group return to camp
    // TODO check succeed king
    // TODO disable reward button when not enough money
    // TODO enable/disable buttons dependent on group count

    private const int MouseOnBuildSettleCancelButtonX1 = DetailsX1 + 8;
    private const int MouseOnBuildSettleCancelButtonX2 = DetailsX2 - 8;
    private const int MouseOnBuildSettleCancelButtonY1 = DetailsY1 + 120;
    private const int MouseOnBuildSettleCancelButtonY2 = DetailsY1 + 152;
    
    private readonly int[] _buildFirmOrder =
    {
        Firm.FIRM_CAMP, Firm.FIRM_INN, Firm.FIRM_MINE, Firm.FIRM_FACTORY, Firm.FIRM_RESEARCH, Firm.FIRM_WAR_FACTORY,
        Firm.FIRM_MARKET, Firm.FIRM_HARBOR, Firm.FIRM_BASE
    };

    private int _buildFirmType;

    private HumanDetailsMode HumanDetailsMode { get; set; } = HumanDetailsMode.Normal;

    public void DrawHumanDetails(UnitHuman unit)
    {
        if (HumanDetailsMode == HumanDetailsMode.Settle)
        {
            DrawSettlePanel();
            return;
        }

        if (HumanDetailsMode == HumanDetailsMode.BuildMenu)
        {
            DrawBuildMenu(unit);
            return;
        }

        if (HumanDetailsMode == HumanDetailsMode.Build)
        {
            DrawBuildPanel();
            return;
        }
        
        DrawUnitPanel(DetailsX1 + 2, DetailsY1 + 48);
        string title = String.Empty;
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
                break;
        }

        UnitInfo unitInfo = UnitRes[unit.UnitType];
        Graphics.DrawBitmap(unitInfo.GetLargeIconTexture(Graphics, unit.Rank), DetailsX1 + 12, DetailsY1 + 56,
            unitInfo.soldierIconWidth * 2, unitInfo.soldierIconHeight * 2);

        PutTextCenter(FontSan, title, DetailsX1 + 10 + unitInfo.soldierIconWidth * 2, DetailsY1 + 56,
            DetailsX2 - 4, DetailsY1 + 56 + unitInfo.soldierIconHeight);
        PutTextCenter(FontSan, unit.GetUnitName(false), DetailsX1 + 10 + unitInfo.soldierIconWidth * 2, DetailsY1 + 56 + unitInfo.soldierIconHeight,
            DetailsX2 - 4, DetailsY1 + 56 + unitInfo.soldierIconHeight * 2);
        
        DrawPanelWithThreeFields(DetailsX1 + 2, DetailsY1 + 144);
        int combatPanelDY = 0;

        if (unit.Skill.SkillId != 0)
        {
            DrawFieldPanel75(DetailsX1 + 7, DetailsY1 + 149);
            PutText(FontSan, unit.Skill.SkillDescription(), DetailsX1 + 13, DetailsY1 + 152, -1, true);
            PutText(FontSan, unit.Skill.SkillLevel.ToString(), DetailsX1 + 124, DetailsY1 + 154, -1, true);
            combatPanelDY += 29;
        }

        DrawFieldPanel75(DetailsX1 + 7, DetailsY1 + 149 + combatPanelDY);
        PutText(FontSan, "Combat", DetailsX1 + 13, DetailsY1 + 152 + combatPanelDY, -1, true);
        PutText(FontSan, unit.Skill.CombatLevel.ToString(), DetailsX1 + 124, DetailsY1 + 154 + combatPanelDY, -1, true);

        if (unit.Rank != Unit.RANK_KING && !unit.IsCivilian())
        {
            DrawFieldPanel75(DetailsX1 + 7, DetailsY1 + 207);
            PutText(FontSan, "Contribution", DetailsX1 + 13, DetailsY1 + 210, -1, true);
            PutText(FontSan, unit.NationContribution.ToString(), DetailsX1 + 124, DetailsY1 + 212, -1, true);
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
        
        if (unit.IsOwn())
            DrawButtons(unit);

        if (unit.IsOwnSpy())
            DrawSpyCloakPanel(unit);
    }

    private void DrawButtons(Unit unit)
    {
        bool mouseOnButton = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                             _mouseButtonY >= ButtonsUnitHuman1Y + 2 && _mouseButtonY <= ButtonsUnitHuman1Y + ButtonHeight;
        if (_leftMousePressed && mouseOnButton)
            Graphics.DrawBitmap(_buttonDownTexture, Button1X, ButtonsUnitHuman1Y, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
        else
            Graphics.DrawBitmap(_buttonUpTexture, Button1X, ButtonsUnitHuman1Y, Scale(_buttonUpWidth), Scale(_buttonUpHeight));

        if (IsSucceedKingEnabled(unit))
        {
            Graphics.DrawBitmap(_buttonSucceedKingTexture, Button1X + 2, ButtonsUnitHuman1Y + 8, Scale(_buttonSucceedKingWidth), Scale(_buttonSucceedKingHeight));
            return;
        }
        
        if (unit.AggressiveMode)
            Graphics.DrawBitmap(_buttonAggressionOnTexture, Button1X + 10, ButtonsUnitHuman1Y + 6, Scale(_buttonAggressionOnWidth), Scale(_buttonAggressionOnHeight));
        else
            Graphics.DrawBitmap(_buttonAggressionOffTexture, Button1X + 10, ButtonsUnitHuman1Y + 6, Scale(_buttonAggressionOffWidth), Scale(_buttonAggressionOffHeight));

        if (IsRewardEnabled(unit))
        {
            mouseOnButton = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                            _mouseButtonY >= ButtonsUnitHuman1Y + 2 && _mouseButtonY <= ButtonsUnitHuman1Y + ButtonHeight;
            if (_leftMousePressed && mouseOnButton)
                Graphics.DrawBitmap(_buttonDownTexture, Button2X, ButtonsUnitHuman1Y, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
            else
                Graphics.DrawBitmap(_buttonUpTexture, Button2X, ButtonsUnitHuman1Y, Scale(_buttonUpWidth), Scale(_buttonUpHeight));
            Graphics.DrawBitmap(_buttonRewardTexture, Button2X + 12, ButtonsUnitHuman1Y + 4, Scale(_buttonRewardWidth), Scale(_buttonRewardHeight));
        }

        if (IsSettleEnabled(unit))
        {
            mouseOnButton = _mouseButtonX >= Button3X + 2 && _mouseButtonX <= Button3X + ButtonWidth &&
                            _mouseButtonY >= ButtonsUnitHuman1Y + 2 && _mouseButtonY <= ButtonsUnitHuman1Y + ButtonHeight;
            if (_leftMousePressed && mouseOnButton)
                Graphics.DrawBitmap(_buttonDownTexture, Button3X, ButtonsUnitHuman1Y, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
            else
                Graphics.DrawBitmap(_buttonUpTexture, Button3X, ButtonsUnitHuman1Y, Scale(_buttonUpWidth), Scale(_buttonUpHeight));
            Graphics.DrawBitmap(_buttonSettleTexture, Button3X + 12, ButtonsUnitHuman1Y + 4, Scale(_buttonSettleWidth), Scale(_buttonSettleHeight));
        }

        if (IsBuildEnabled(unit))
        {
            mouseOnButton = _mouseButtonX >= Button4X + 2 && _mouseButtonX <= Button4X + ButtonWidth &&
                            _mouseButtonY >= ButtonsUnitHuman1Y + 2 && _mouseButtonY <= ButtonsUnitHuman1Y + ButtonHeight;
            if (_leftMousePressed && mouseOnButton)
                Graphics.DrawBitmap(_buttonDownTexture, Button4X, ButtonsUnitHuman1Y, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
            else
                Graphics.DrawBitmap(_buttonUpTexture, Button4X, ButtonsUnitHuman1Y, Scale(_buttonUpWidth), Scale(_buttonUpHeight));
            Graphics.DrawBitmap(_buttonBuildTexture, Button4X + 12, ButtonsUnitHuman1Y + 8, Scale(_buttonBuildWidth), Scale(_buttonBuildHeight));
        }

        if (IsPromoteEnabled(unit))
        {
            mouseOnButton = _mouseButtonX >= Button5X + 2 && _mouseButtonX <= Button5X + ButtonWidth &&
                            _mouseButtonY >= ButtonsUnitHuman1Y + 2 && _mouseButtonY <= ButtonsUnitHuman1Y + ButtonHeight;
            if (_leftMousePressed && mouseOnButton)
                Graphics.DrawBitmap(_buttonDownTexture, Button5X, ButtonsUnitHuman1Y, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
            else
                Graphics.DrawBitmap(_buttonUpTexture, Button5X, ButtonsUnitHuman1Y, Scale(_buttonUpWidth), Scale(_buttonUpHeight));
            Graphics.DrawBitmap(_buttonPromoteTexture, Button5X + 5, ButtonsUnitHuman1Y + 4, Scale(_buttonPromoteWidth), Scale(_buttonPromoteHeight));
        }

        if (IsDemoteEnabled(unit))
        {
            mouseOnButton = _mouseButtonX >= Button5X + 2 && _mouseButtonX <= Button5X + ButtonWidth &&
                            _mouseButtonY >= ButtonsUnitHuman1Y + 2 && _mouseButtonY <= ButtonsUnitHuman1Y + ButtonHeight;
            if (_leftMousePressed && mouseOnButton)
                Graphics.DrawBitmap(_buttonDownTexture, Button5X, ButtonsUnitHuman1Y, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
            else
                Graphics.DrawBitmap(_buttonUpTexture, Button5X, ButtonsUnitHuman1Y, Scale(_buttonUpWidth), Scale(_buttonUpHeight));
            Graphics.DrawBitmap(_buttonDemoteTexture, Button5X + 12, ButtonsUnitHuman1Y + 8, Scale(_buttonDemoteWidth), Scale(_buttonDemoteHeight));
        }

        if (IsReturnToCampEnabled(unit))
        {
            mouseOnButton = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                            _mouseButtonY >= ButtonsUnitHuman2Y + 2 && _mouseButtonY <= ButtonsUnitHuman2Y + ButtonHeight;
            if (_leftMousePressed && mouseOnButton)
                Graphics.DrawBitmap(_buttonDownTexture, Button1X, ButtonsUnitHuman2Y, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
            else
                Graphics.DrawBitmap(_buttonUpTexture, Button1X, ButtonsUnitHuman2Y, Scale(_buttonUpWidth), Scale(_buttonUpHeight));
            Graphics.DrawBitmap(_buttonReturnToCampTexture, Button1X + 4, ButtonsUnitHuman2Y + 8, Scale(_buttonReturnToCampWidth), Scale(_buttonReturnToCampHeight));
        }

        if (IsSpyButtonsEnabled(unit))
        {
            mouseOnButton = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                            _mouseButtonY >= ButtonsUnitHuman2Y + 2 && _mouseButtonY <= ButtonsUnitHuman2Y + ButtonHeight;
            if (_leftMousePressed && mouseOnButton)
                Graphics.DrawBitmap(_buttonDownTexture, Button2X, ButtonsUnitHuman2Y, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
            else
                Graphics.DrawBitmap(_buttonUpTexture, Button2X, ButtonsUnitHuman2Y, Scale(_buttonUpWidth), Scale(_buttonUpHeight));
            
            if (SpyArray[unit.SpyId].NotifyCloakedNation)
                Graphics.DrawBitmap(_buttonSpyNotifyOnTexture, Button2X + 22, ButtonsUnitHuman2Y + 4, Scale(_buttonSpyNotifyOnWidth), Scale(_buttonSpyNotifyOnHeight));
            else
                Graphics.DrawBitmap(_buttonSpyNotifyOffTexture, Button2X + 16, ButtonsUnitHuman2Y + 6, Scale(_buttonSpyNotifyOffWidth), Scale(_buttonSpyNotifyOffHeight));
            
            mouseOnButton = _mouseButtonX >= Button3X + 2 && _mouseButtonX <= Button3X + ButtonWidth &&
                            _mouseButtonY >= ButtonsUnitHuman2Y + 2 && _mouseButtonY <= ButtonsUnitHuman2Y + ButtonHeight;
            if (_leftMousePressed && mouseOnButton)
                Graphics.DrawBitmap(_buttonDownTexture, Button3X, ButtonsUnitHuman2Y, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
            else
                Graphics.DrawBitmap(_buttonUpTexture, Button3X, ButtonsUnitHuman2Y, Scale(_buttonUpWidth), Scale(_buttonUpHeight));
            Graphics.DrawBitmap(_buttonDropSpyIdentityTexture, Button3X + 4, ButtonsUnitHuman2Y + 6, Scale(_buttonDropSpyIdentityWidth), Scale(_buttonDropSpyIdentityHeight));
        }
    }

    private void DrawSpyCloakPanel(Unit unit)
    {
        bool canChangeToOtherNation = unit.CanSpyChangeNation();
        if (!IsSpyCloakPanelVisible(unit, canChangeToOtherNation))
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
        if (HumanDetailsMode == HumanDetailsMode.Settle)
        {
            HandleSettlePanelInput();
            return;
        }

        if (HumanDetailsMode == HumanDetailsMode.BuildMenu)
        {
            HandleBuildMenuInput(unit);
            return;
        }

        if (HumanDetailsMode == HumanDetailsMode.Build)
        {
            HandleBuildPanelInput();
            return;
        }

        if (unit.IsOwn())
        {
            bool button1Pressed = _leftMouseReleased && _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                  _mouseButtonY >= ButtonsUnitHuman1Y + 2 && _mouseButtonY <= ButtonsUnitHuman1Y + ButtonHeight;
            bool button2Pressed = _leftMouseReleased && _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                                  _mouseButtonY >= ButtonsUnitHuman1Y + 2 && _mouseButtonY <= ButtonsUnitHuman1Y + ButtonHeight;
            bool button3Pressed = _leftMouseReleased && _mouseButtonX >= Button3X + 2 && _mouseButtonX <= Button3X + ButtonWidth &&
                                  _mouseButtonY >= ButtonsUnitHuman1Y + 2 && _mouseButtonY <= ButtonsUnitHuman1Y + ButtonHeight;
            bool button4Pressed = _leftMouseReleased && _mouseButtonX >= Button4X + 2 && _mouseButtonX <= Button4X + ButtonWidth &&
                                  _mouseButtonY >= ButtonsUnitHuman1Y + 2 && _mouseButtonY <= ButtonsUnitHuman1Y + ButtonHeight;
            bool button5Pressed = _leftMouseReleased && _mouseButtonX >= Button5X + 2 && _mouseButtonX <= Button5X + ButtonWidth &&
                                  _mouseButtonY >= ButtonsUnitHuman1Y + 2 && _mouseButtonY <= ButtonsUnitHuman1Y + ButtonHeight;
            bool button6Pressed = _leftMouseReleased && _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                  _mouseButtonY >= ButtonsUnitHuman2Y + 2 && _mouseButtonY <= ButtonsUnitHuman2Y + ButtonHeight;
            bool button7Pressed = _leftMouseReleased && _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                                  _mouseButtonY >= ButtonsUnitHuman2Y + 2 && _mouseButtonY <= ButtonsUnitHuman2Y + ButtonHeight;
            bool button8Pressed = _leftMouseReleased && _mouseButtonX >= Button3X + 2 && _mouseButtonX <= Button3X + ButtonWidth &&
                                  _mouseButtonY >= ButtonsUnitHuman2Y + 2 && _mouseButtonY <= ButtonsUnitHuman2Y + ButtonHeight;

            if (button1Pressed)
            {
                if (IsSucceedKingEnabled(unit))
                {
                    //if (remote.is_enable())
                    //{
                        //// packet structure : <unit recno> <nation recno>
                        //short *shortPtr = (short *)remote.new_send_queue_msg(MSG_UNIT_SUCCEED_KING, 2*sizeof(short));
                        //*shortPtr = sprite_recno;
                        //shortPtr[1] = nation_array.player_recno;
                    //}
                    //else
                    //{
                        NationArray.player.succeed_king(unit);
                    //}
                    return;
                }

                bool newAggressiveMode = !unit.AggressiveMode;
                //if (remote.is_enable())
                //{
                    //// packet structure : <unit no> <new aggressive mode>
                    //short *shortPtr = (short *)remote.new_send_queue_msg(MSG_UNIT_CHANGE_AGGRESSIVE_MODE, sizeof(short)*2);
                    //*shortPtr = i;
                    //shortPtr[1] = newAggressiveMode;
                //}
                //else
                //{
                    unit.AggressiveMode = newAggressiveMode;
                //}

                if (newAggressiveMode)
                    SECtrl.immediate_sound("TURN_ON");
                else
                    SECtrl.immediate_sound("TURN_OFF");
            }

            if (button2Pressed && IsRewardEnabled(unit))
            {
                //if (remote.is_enable())
                //{
                    //// packet structure : <unit no> + <rewarding nation recno>
                    //short *shortPtr = (short *)remote.new_send_queue_msg(MSG_UNIT_REWARD,sizeof(short)*2);
                    //*shortPtr = i;
                    //shortPtr[1] = nation_array.player_recno;
                //}
                //else
                //{
                    unit.Reward(NationArray.player_recno);
                //}

                SECtrl.immediate_sound("TURN_ON");
            }

            if (button3Pressed && IsSettleEnabled(unit))
            {
                HumanDetailsMode = HumanDetailsMode.Settle;
            }

            if (button4Pressed && IsBuildEnabled(unit))
            {
                HumanDetailsMode = HumanDetailsMode.BuildMenu;
            }

            if (button5Pressed && (IsPromoteEnabled(unit) || IsDemoteEnabled(unit)))
            {
                bool promote = IsPromoteEnabled(unit);
                //if (remote.is_enable())
                //{
                    //// packet structure : <unit recno> <new rank>
                    //short *shortPtr = (short *)remote.new_send_queue_msg(MSG_UNIT_SET_RANK, 2*sizeof(short));
                    //*shortPtr = sprite_recno;
                    //shortPtr[1] = promote ? RANK_GENERAL : RANK_SOLDIER;
                //}
                //else
                //{
                    unit.SetRank(promote ? Unit.RANK_GENERAL : Unit.RANK_SOLDIER);
                //}

                SECtrl.immediate_sound(promote ? "TURN_ON" : "TURN_OFF");
            }

            if (button6Pressed && IsReturnToCampEnabled(unit))
            {
                //if (remote.is_enable())
                //{
                    //// packet structure : <no. of units> <unit recno>...
                    //short* shortPtr = (short*)remote.new_send_queue_msg(MSG_UNITS_RETURN_CAMP, (1 + selectedCount) * sizeof(short));
                    //*shortPtr = selectedCount;
                    //shortPtr++;
                    //memcpy(shortPtr, selectedUnitArray, sizeof(short) * selectedCount);
                //}
                //else
                //{
                    unit.ReturnCamp();
                //}
                SERes.far_sound(unit.NextLocX, unit.NextLocY, 1, 'S', unit.SpriteId, "ACK");
            }

            if (button7Pressed && IsSpyButtonsEnabled(unit))
            {
                Spy spy = SpyArray[unit.SpyId];
                bool newNotifyFlag = !spy.NotifyCloakedNation;
                //if (remote.is_enable())
                //{
                    //// packet structure : <spy recno> <new notify_cloaked_nation_flag>
                    //short *shortPtr = (short *)remote.new_send_queue_msg(MSG_SPY_CHANGE_NOTIFY_FLAG, sizeof(short)*2);
                    //*shortPtr = unitPtr->spy_recno;
                    //shortPtr[1] = newNotifyFlag;
                //}
                //else
                //{
                    spy.NotifyCloakedNation = newNotifyFlag;
                //}

                SECtrl.immediate_sound(newNotifyFlag ? "TURN_ON" : "TURN_OFF");
            }

            if (button8Pressed && IsSpyButtonsEnabled(unit))
            {
                Spy spy = SpyArray[unit.SpyId];
                //if (remote.is_enable())
                //{
                    //// packet structure : <spy recno>
                    //short *shortPtr = (short *)remote.new_send_queue_msg(MSG_SPY_DROP_IDENTITY, sizeof(short));
                    //shortPtr[0] = unitPtr->spy_recno;
                //}
                //else
                //{
                    spy.DropSpyIdentity();
                //}

                SECtrl.immediate_sound("TURN_OFF");
            }
        }

        bool canChangeToOtherNation = unit.CanSpyChangeNation();
        if (unit.IsOwnSpy() && IsSpyCloakPanelVisible(unit, canChangeToOtherNation))
        {
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

                if (_leftMouseReleased && _mouseButtonX >= DetailsX1 + 160 + colorDX && _mouseButtonX <= DetailsX1 + 160 + colorDX + 28 &&
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
                if (_leftMouseReleased && _mouseButtonX >= DetailsX1 + 160 + colorDX && _mouseButtonX <= DetailsX1 + 160 + colorDX + 28 &&
                    _mouseButtonY >= DetailsY1 + 404 + colorDY && _mouseButtonY <= DetailsY1 + 404 + colorDY + 28)
                {
                    unit.SpyChangeNation(0, InternalConstants.COMMAND_PLAYER);
                }
            }
        }
    }

    private bool IsSucceedKingEnabled(Unit unit)
    {
        return NationArray.player_recno != 0 && unit.NationId == NationArray.player_recno && NationArray.player.king_unit_recno == 0;
    }

    private bool IsRewardEnabled(Unit unit)
    {
        return unit.IsOwn() && unit.Rank != Unit.RANK_KING;
    }

    private bool IsSettleEnabled(Unit unit)
    {
        return unit.NationId == NationArray.player_recno && unit.Rank != Unit.RANK_KING;
    }

    private bool IsBuildEnabled(Unit unit)
    {
        bool canBuildSomething = false;
        for (int i = 1; i < Firm.MAX_FIRM_TYPE; i++)
        {
            if (unit.CanBuild(i))
            {
                canBuildSomething = true;
                break;
            }
        }
        
        return unit.NationId == NationArray.player_recno && canBuildSomething;
    }

    private bool IsPromoteEnabled(Unit unit)
    {
        return unit.NationId == NationArray.player_recno && unit.Rank == Unit.RANK_SOLDIER && unit.Skill.SkillId == Skill.SKILL_LEADING;
    }

    private bool IsDemoteEnabled(Unit unit)
    {
        return unit.NationId == NationArray.player_recno && unit.Rank == Unit.RANK_GENERAL;
    }

    private bool IsReturnToCampEnabled(Unit unit)
    {
        return unit.HomeCampId != 0 && FirmArray[unit.HomeCampId].RegionId == unit.RegionId();
    }

    private bool IsSpyButtonsEnabled(Unit unit)
    {
        return unit.SpyId != 0 && unit.TrueNationId() == NationArray.player_recno;
    }

    private bool IsSpyCloakPanelVisible(Unit unit, bool canChangeToOtherNation)
    {
        return canChangeToOtherNation || unit.NationId != unit.TrueNationId();
    }

    private void DrawSettlePanel()
    {
        DrawPanelWithTwoFields(DetailsX1 + 2, DetailsY1 + 48);
        PutTextCenter(FontSan, "Please select a location", DetailsX1 + 2, DetailsY1 + 64, DetailsX2 - 4, DetailsY1 + 64);
        PutTextCenter(FontSan, "to settle", DetailsX1 + 2, DetailsY1 + 90, DetailsX2 - 4, DetailsY1 + 90);

        bool mouseOnCancelButton = _mouseButtonX >= MouseOnBuildSettleCancelButtonX1 && _mouseButtonX <= MouseOnBuildSettleCancelButtonX2 &&
                                   _mouseButtonY >= MouseOnBuildSettleCancelButtonY1 && _mouseButtonY <= MouseOnBuildSettleCancelButtonY2;
        if (_leftMousePressed && mouseOnCancelButton)
            DrawCancelPanelDown(DetailsX1 + 2, DetailsY1 + 117);
        else
            DrawCancelPanelUp(DetailsX1 + 2, DetailsY1 + 117);
        PutTextCenter(FontSan, "Cancel", DetailsX1 + 2, DetailsY1 + 132, DetailsX2 - 4, DetailsY1 + 132);
    }

    private void HandleSettlePanelInput()
    {
        if (_leftMouseReleased)
        {
            bool mouseOnCancelButton = _mouseButtonX >= MouseOnBuildSettleCancelButtonX1 && _mouseButtonX <= MouseOnBuildSettleCancelButtonX2 &&
                                       _mouseButtonY >= MouseOnBuildSettleCancelButtonY1 && _mouseButtonY <= MouseOnBuildSettleCancelButtonY2;
            if (mouseOnCancelButton)
            {
                HumanDetailsMode = HumanDetailsMode.Normal;
            }
        }
    }

    private void DrawBuildMenu(Unit unit)
    {
        if (_buildButtonTextures.Count == 0)
            CreateBuildButtonTextures(ColorRemap.ColorSchemes[unit.NationId]);

        bool mouseOnButton = false;
        int buttonsCount = 0;
        for (int i = 0; i < _buildFirmOrder.Length; i++)
        {
            int firmType = _buildFirmOrder[i];
            if (unit.CanBuild(firmType))
            {
                string resourceName = "F-" + firmType;
                if (firmType == Firm.FIRM_BASE)
                    resourceName += "-" + unit.RaceId;
                int buttonX = DetailsX1 + 56 + 150 * (buttonsCount % 2);
                int buttonY = DetailsY1 + 2 + 92 * (buttonsCount / 2);
                Graphics.DrawBitmap(_buildButtonTextures[resourceName], buttonX, buttonY, Scale(_buildButtonWidth), Scale(_buildButtonHeight));
                mouseOnButton = _mouseButtonX >= buttonX && _mouseButtonX <= buttonX + Scale(_buildButtonWidth) &&
                                _mouseButtonY >= buttonY && _mouseButtonY <= buttonY + Scale(_buildButtonHeight);
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmap(_buttonBuildDownTexture, buttonX, buttonY, Scale(_buttonBuildDownWidth), Scale(_buttonBuildDownHeight));
                buttonsCount++;
            }
        }

        int buttonCancelX = DetailsX1 + 56 + 150 * (buttonsCount % 2);
        int buttonCancelY = DetailsY1 + 2 + 92 * (buttonsCount / 2);
        mouseOnButton = _mouseButtonX >= buttonCancelX && _mouseButtonX <= buttonCancelX + ButtonWidth &&
                        _mouseButtonY >= buttonCancelY && _mouseButtonY <= buttonCancelY + ButtonHeight;
        if (_leftMousePressed && mouseOnButton)
            Graphics.DrawBitmap(_buttonDownTexture, buttonCancelX, buttonCancelY, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
        else
            Graphics.DrawBitmap(_buttonUpTexture, buttonCancelX, buttonCancelY, Scale(_buttonUpWidth), Scale(_buttonUpHeight));
        Graphics.DrawBitmap(_buttonCancelTexture, buttonCancelX + 12, buttonCancelY + 8, Scale(_buttonCancelWidth), Scale(_buttonCancelHeight));
    }

    private void HandleBuildMenuInput(Unit unit)
    {
        if (_leftMouseReleased)
        {
            int buttonsCount = 0;
            for (int i = 0; i < _buildFirmOrder.Length; i++)
            {
                int firmType = _buildFirmOrder[i];
                if (unit.CanBuild(firmType))
                {
                    int buttonX = DetailsX1 + 56 + 150 * (buttonsCount % 2);
                    int buttonY = DetailsY1 + 2 + 92 * (buttonsCount / 2);
                    if (_mouseButtonX >= buttonX && _mouseButtonX <= buttonX + Scale(_buildButtonWidth) &&
                        _mouseButtonY >= buttonY && _mouseButtonY <= buttonY + Scale(_buildButtonHeight))
                    {
                        _buildFirmType = firmType;
                        HumanDetailsMode = HumanDetailsMode.Build;
                    }
                    buttonsCount++;
                }
            }

            int buttonCancelX = DetailsX1 + 56 + 150 * (buttonsCount % 2);
            int buttonCancelY = DetailsY1 + 2 + 92 * (buttonsCount / 2);
            if (_mouseButtonX >= buttonCancelX && _mouseButtonX <= buttonCancelX + ButtonWidth &&
                _mouseButtonY >= buttonCancelY && _mouseButtonY <= buttonCancelY + ButtonHeight)
            {
                HumanDetailsMode = HumanDetailsMode.Normal;
            }
        }
    }

    private void DrawBuildPanel()
    {
        string buildFirmText = _buildFirmType switch
        {
            Firm.FIRM_CAMP => "Fort",
            Firm.FIRM_INN => "Inn",
            Firm.FIRM_MINE => "Mine",
            Firm.FIRM_FACTORY => "Factory",
            Firm.FIRM_RESEARCH => "Tower of Science",
            Firm.FIRM_WAR_FACTORY => "War Factory",
            Firm.FIRM_MARKET => "Market",
            Firm.FIRM_HARBOR => "Harbor",
            Firm.FIRM_BASE => "Seat of Power",
            _ => ""
        };
        DrawPanelWithTwoFields(DetailsX1 + 2, DetailsY1 + 48);
        PutTextCenter(FontSan, "Please select a location", DetailsX1 + 2, DetailsY1 + 64, DetailsX2 - 4, DetailsY1 + 64);
        PutTextCenter(FontSan, "to build the " + buildFirmText, DetailsX1 + 2, DetailsY1 + 90, DetailsX2 - 4, DetailsY1 + 90);

        bool mouseOnCancelButton = _mouseButtonX >= MouseOnBuildSettleCancelButtonX1 && _mouseButtonX <= MouseOnBuildSettleCancelButtonX2 &&
                                   _mouseButtonY >= MouseOnBuildSettleCancelButtonY1 && _mouseButtonY <= MouseOnBuildSettleCancelButtonY2;
        if (_leftMousePressed && mouseOnCancelButton)
            DrawCancelPanelDown(DetailsX1 + 2, DetailsY1 + 117);
        else
            DrawCancelPanelUp(DetailsX1 + 2, DetailsY1 + 117);
        PutTextCenter(FontSan, "Cancel", DetailsX1 + 2, DetailsY1 + 132, DetailsX2 - 4, DetailsY1 + 132);
    }

    private void HandleBuildPanelInput()
    {
        if (_leftMouseReleased)
        {
            bool mouseOnCancelButton = _mouseButtonX >= MouseOnBuildSettleCancelButtonX1 && _mouseButtonX <= MouseOnBuildSettleCancelButtonX2 &&
                                       _mouseButtonY >= MouseOnBuildSettleCancelButtonY1 && _mouseButtonY <= MouseOnBuildSettleCancelButtonY2;
            if (mouseOnCancelButton)
            {
                HumanDetailsMode = HumanDetailsMode.Normal;
                _buildFirmType = 0;
            }
        }
    }

    private void CancelSettleAndBuild()
    {
        if (HumanDetailsMode == HumanDetailsMode.Build || HumanDetailsMode == HumanDetailsMode.Settle)
        {
            HumanDetailsMode = HumanDetailsMode.Normal;
            _buildFirmType = 0;
        }
    }
}