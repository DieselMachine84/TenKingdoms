using System;
using System.Collections.Generic;

namespace TenKingdoms;

public partial class Renderer
{
    private void DrawSpyList(int firmTownNationId, List<Spy> playerSpies)
    {
        DrawSmallPanel(DetailsX1 + 2, DetailsY1 + 48);
        PutText(FontSan, "Spy Skill", DetailsX1 + 40, DetailsY1 + 55);
        PutText(FontSan, "Loyalty", DetailsX1 + 180, DetailsY1 + 55);
        PutText(FontSan, "Action", DetailsX1 + 310, DetailsY1 + 55);

        bool needScroll = playerSpies.Count > 4;
        if (needScroll)
        {
            DrawListBoxPanelWithScroll(DetailsX1 + 2, DetailsY1 + 96);
            DrawListBoxScrollPanel(DetailsX1 + 377, DetailsY1 + 96);
        }
        else
        {
            DrawListBox4Panel(DetailsX1 + 2, DetailsY1 + 96);
        }

        int buttonPrevMenuY = ButtonsSpyY + 70;
        bool mouseOnButton = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                        _mouseButtonY >= buttonPrevMenuY + 2 && _mouseButtonY <= buttonPrevMenuY + ButtonHeight;
        if (_leftMousePressed && mouseOnButton)
            Graphics.DrawBitmapScaled(_buttonDownTexture, Button1X, buttonPrevMenuY, _buttonDownWidth, _buttonDownHeight);
        else
            Graphics.DrawBitmapScaled(_buttonUpTexture, Button1X, buttonPrevMenuY, _buttonUpWidth, _buttonUpHeight);
        Graphics.DrawBitmapScaled(_buttonSpyPrevMenuTexture, Button1X + 10, buttonPrevMenuY + 8, _buttonSpyPrevMenuWidth, _buttonSpyPrevMenuHeight);
        
        if (playerSpies.Count == 0)
            return;

        if (_selectedSpy != null && !playerSpies.Contains(_selectedSpy))
            _selectedSpy = null;

        int spyY = DetailsY1 + 109;
        foreach (Spy playerSpy in playerSpies)
        {
            if (_selectedSpy == null)
                _selectedSpy = playerSpy;

            RaceInfo raceInfo = RaceRes[playerSpy.RaceId];
            if (playerSpy.SpyPlace == Spy.SPY_TOWN)
                Graphics.DrawBitmap(raceInfo.GetIconTexture(Graphics), DetailsX1 + 14, spyY, raceInfo.IconBitmapWidth * 2, raceInfo.IconBitmapHeight * 2);

            if (playerSpy.SpyPlace == Spy.SPY_FIRM)
            {
                int overseerId = FirmArray[playerSpy.SpyPlaceId].OverseerId;
                Unit overseer = overseerId != 0 ? UnitArray[overseerId] : null;
                if (overseer != null && overseer.SpyId == playerSpy.SpyId)
                {
                    UnitInfo unitInfo = UnitRes[overseer.UnitType];
                    Graphics.DrawBitmap(unitInfo.GetSmallIconTexture(Graphics, overseer.Rank), DetailsX1 + 14, spyY, raceInfo.IconBitmapWidth * 2, raceInfo.IconBitmapHeight * 2);
                }
                else
                {
                    Graphics.DrawBitmap(raceInfo.GetIconTexture(Graphics), DetailsX1 + 14, spyY, raceInfo.IconBitmapWidth * 2, raceInfo.IconBitmapHeight * 2);
                }
            }

            PutText(FontSan, playerSpy.SpySkill.ToString(), DetailsX1 + 82, spyY + 6);
            PutText(FontSan, playerSpy.SpyLoyalty.ToString(), DetailsX1 + 158, spyY + 6);
            PutText(FontSan, playerSpy.ActionString(), DetailsX1 + 234, spyY + 6);

            if (_selectedSpy == playerSpy)
                DrawSelectedBorder(DetailsX1 + 8, spyY - 7, DetailsX1 + 405, spyY + 47);

            spyY += ListItemHeight;
        }

        mouseOnButton = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                        _mouseButtonY >= ButtonsSpyY + 2 && _mouseButtonY <= ButtonsSpyY + ButtonHeight;
        if (_leftMousePressed && mouseOnButton)
            Graphics.DrawBitmapScaled(_buttonDownTexture, Button1X, ButtonsSpyY, _buttonDownWidth, _buttonDownHeight);
        else
            Graphics.DrawBitmapScaled(_buttonUpTexture, Button1X, ButtonsSpyY, _buttonUpWidth, _buttonUpHeight);
        Graphics.DrawBitmapScaled(_buttonMobilizeSpyTexture, Button1X + 4, ButtonsSpyY + 2, _buttonMobilizeSpyWidth, _buttonMobilizeSpyHeight);
        
        if (IsSpyRewardEnabled())
        {
            mouseOnButton = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                            _mouseButtonY >= ButtonsSpyY + 2 && _mouseButtonY <= ButtonsSpyY + ButtonHeight;
            if (_leftMousePressed && mouseOnButton)
                Graphics.DrawBitmapScaled(_buttonDownTexture, Button2X, ButtonsSpyY, _buttonDownWidth, _buttonDownHeight);
            else
                Graphics.DrawBitmapScaled(_buttonUpTexture, Button2X, ButtonsSpyY, _buttonUpWidth, _buttonUpHeight);
            Graphics.DrawBitmapScaled(_buttonRewardTexture, Button2X + 12, ButtonsSpyY + 4, _buttonRewardWidth, _buttonRewardHeight);
        }
        else
        {
            Graphics.DrawBitmapScaled(_buttonDisabledTexture, Button2X, ButtonsSpyY, _buttonDisabledWidth, _buttonDisabledHeight);
            Graphics.DrawBitmapScaled(_buttonRewardDisabledTexture, Button2X + 12, ButtonsSpyY + 4, _buttonRewardWidth, _buttonRewardHeight);
        }

        if (firmTownNationId != NationArray.PlayerId)
        {
            mouseOnButton = _mouseButtonX >= Button3X + 2 && _mouseButtonX <= Button3X + ButtonWidth &&
                            _mouseButtonY >= ButtonsSpyY + 2 && _mouseButtonY <= ButtonsSpyY + ButtonHeight;
            if (_leftMousePressed && mouseOnButton)
                Graphics.DrawBitmapScaled(_buttonDownTexture, Button3X, ButtonsSpyY, _buttonDownWidth, _buttonDownHeight);
            else
                Graphics.DrawBitmapScaled(_buttonUpTexture, Button3X, ButtonsSpyY, _buttonUpWidth, _buttonUpHeight);
            Graphics.DrawBitmapScaled(_buttonSpyChangeActionTexture, Button3X + 4, ButtonsSpyY + 16, _buttonSpyChangeActionWidth, _buttonSpyChangeActionHeight);
        }

        if (firmTownNationId != 0 && firmTownNationId != NationArray.PlayerId)
        {
            mouseOnButton = _mouseButtonX >= Button4X + 2 && _mouseButtonX <= Button4X + ButtonWidth &&
                            _mouseButtonY >= ButtonsSpyY + 2 && _mouseButtonY <= ButtonsSpyY + ButtonHeight;
            if (_leftMousePressed && mouseOnButton)
                Graphics.DrawBitmapScaled(_buttonDownTexture, Button4X, ButtonsSpyY, _buttonDownWidth, _buttonDownHeight);
            else
                Graphics.DrawBitmapScaled(_buttonUpTexture, Button4X, ButtonsSpyY, _buttonUpWidth, _buttonUpHeight);
            Graphics.DrawBitmapScaled(_buttonSpyViewSecretTexture, Button4X + 6, ButtonsSpyY + 4, _buttonSpyViewSecretWidth, _buttonSpyViewSecretHeight);
        }
    }

    private void HandleSpyList(int firmTownNationId, List<Spy> playerSpies)
    {
        bool spy1Selected = _leftMouseReleased && _mouseButtonX >= DetailsX1 + 11 && _mouseButtonX <= DetailsX1 + 402 &&
                             _mouseButtonY >= DetailsY1 + 105 && _mouseButtonY <= DetailsY1 + 153;
        bool spy2Selected = _leftMouseReleased && _mouseButtonX >= DetailsX1 + 11 && _mouseButtonX <= DetailsX1 + 402 &&
                            _mouseButtonY >= DetailsY1 + 105 + ListItemHeight && _mouseButtonY <= DetailsY1 + 153 + ListItemHeight;
        bool spy3Selected = _leftMouseReleased && _mouseButtonX >= DetailsX1 + 11 && _mouseButtonX <= DetailsX1 + 402 &&
                            _mouseButtonY >= DetailsY1 + 105 + 2 * ListItemHeight && _mouseButtonY <= DetailsY1 + 153 + 2 * ListItemHeight;
        bool spy4Selected = _leftMouseReleased && _mouseButtonX >= DetailsX1 + 11 && _mouseButtonX <= DetailsX1 + 402 &&
                            _mouseButtonY >= DetailsY1 + 105 + 3 * ListItemHeight && _mouseButtonY <= DetailsY1 + 153 + 3 * ListItemHeight;

        int spyIndex = 0;
        foreach (Spy playerSpy in playerSpies)
        {
            spyIndex++;
            if ((spy1Selected && spyIndex == 1) || (spy2Selected && spyIndex == 2) ||
                (spy3Selected && spyIndex == 3) || (spy4Selected && spyIndex == 4))
            {
                _selectedSpy = playerSpy;
                break;
            }
        }

        bool mouseOnButton1 = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                              _mouseButtonY >= ButtonsSpyY + 2 && _mouseButtonY <= ButtonsSpyY + ButtonHeight;
        bool mouseOnButton2 = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                              _mouseButtonY >= ButtonsSpyY + 2 && _mouseButtonY <= ButtonsSpyY + ButtonHeight;
        bool mouseOnButton3 = _mouseButtonX >= Button3X + 2 && _mouseButtonX <= Button3X + ButtonWidth &&
                              _mouseButtonY >= ButtonsSpyY + 2 && _mouseButtonY <= ButtonsSpyY + ButtonHeight;
        bool mouseOnButton4 = _mouseButtonX >= Button4X + 2 && _mouseButtonX <= Button4X + ButtonWidth &&
                              _mouseButtonY >= ButtonsSpyY + 2 && _mouseButtonY <= ButtonsSpyY + ButtonHeight;
        bool mouseOnButton5 = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                              _mouseButtonY >= ButtonsSpyY + 70 + 2 && _mouseButtonY <= ButtonsSpyY + 70 + ButtonHeight;

        if (_leftMouseReleased && mouseOnButton5)
        {
            FirmDetailsMode = FirmDetailsMode.Normal;
            TownDetailsMode = TownDetailsMode.Normal;
        }
        
        if (playerSpies.Count == 0)
            return;
        
        if (_leftMouseReleased && mouseOnButton1)
        {
            /*if (remote.is_enable())
            {
                // packet structure <spy recno>
                int msgType = 0;
                if (_selectedSpy.SpyPlace == Spy.SPY_FIRM)
                    msgType = MSG_SPY_LEAVE_FIRM;
                if (_selectedSpy.SpyPlace == Spy.SPY_TOWN)
                    msgType = MSG_SPY_LEAVE_TOWN;
                short *shortPtr = (short *)remote.new_send_queue_msg(msgType, sizeof(short));
                *shortPtr = spyPtr->spy_recno;
            }*/
            //else
            //{
                Unit unit = null;
                if (_selectedSpy.SpyPlace == Spy.SPY_FIRM)
                    unit = _selectedSpy.MobilizeFirmSpy();
                if (_selectedSpy.SpyPlace == Spy.SPY_TOWN)
                    unit = _selectedSpy.MobilizeTownSpy();
                
                if (unit != null)
                {
                    SpyArray[unit.SpyId].NotifyCloakedNation = false; // reset it so the player can control it
                }
            //}
        }

        if (_leftMouseReleased && mouseOnButton2 && IsSpyRewardEnabled())
        {
            _selectedSpy.Reward(InternalConstants.COMMAND_PLAYER);
        }

        if (firmTownNationId != NationArray.PlayerId)
        {
            if (_leftMouseReleased && mouseOnButton3)
            {
                /*if (remote.is_enable())
                {
				    // packet structure <spy recno>
				    short *shortPtr = (short *)remote.new_send_queue_msg(MSG_SPY_CYCLE_ACTION, sizeof(short) );
				    *shortPtr = spyPtr->spy_recno;
                }*/
                //else
                //{
                    _selectedSpy.SetNextActionMode();
                //}
            }
        }

        if (firmTownNationId != 0 && firmTownNationId != NationArray.PlayerId)
        {
            if (_leftMouseReleased && mouseOnButton4)
            {
                //TODO view secret report
            }
        }
    }

    private bool IsSpyRewardEnabled()
    {
        return NationArray.Player != null && NationArray.Player.Cash > 0.0;
    }
}
