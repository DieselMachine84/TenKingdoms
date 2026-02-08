using System;
using System.Collections.Generic;
using System.Linq;

namespace TenKingdoms;

enum TownDetailsMode { Normal, Train, Spy, CollectTax, Grant }

public partial class Renderer
{
    // TODO controlled by rebels panel conflicts with grant and spy list buttons
    // TODO list box scroll
    // TODO train unit progress

    private const int TrainSkillPanelX = DetailsX1 + 2;
    private const int TrainSkillPanelY = DetailsY1 + 48;
    private const int TrainButtonTextX = DetailsX1 + 52;
    private const int TrainButtonNumberX = DetailsX1 + 351;
    private const int TrainButtonNumberY = DetailsY1 + 55;
    private const int MouseOnTrainButtonX1 = DetailsX1 + 8;
    private const int MouseOnTrainButtonX2 = DetailsX1 + 320;
    private const int MouseOnTrainButtonY1 = DetailsY1 + 54;
    private const int MouseOnTrainButtonY2 = DetailsY1 + 102;
    private const int MouseOnTrainNumberButtonX1 = TrainButtonNumberX + 4;
    private const int MouseOnTrainNumberButtonX2 = TrainButtonNumberX + 40;
    private const int MouseOnTrainNumberButtonY1 = TrainButtonNumberY + 4;
    private const int MouseOnTrainNumberButtonY2 = TrainButtonNumberY + 42;
    private const int TaxButtonX = DetailsX1 + 2;
    private const int TaxButtonY = DetailsY1 + 109;
    private const int MouseOnTaxButtonY = DetailsY1 + 112;

    private TownDetailsMode TownDetailsMode { get; set; } = TownDetailsMode.Normal;
    
    private void DrawTownDetails(Town town)
    {
        if (TownDetailsMode == TownDetailsMode.Train)
        {
            DrawTrainMenu(town);
            return;
        }

        if (TownDetailsMode == TownDetailsMode.CollectTax || TownDetailsMode == TownDetailsMode.Grant)
        {
            DrawAutoCollectTaxOrGrantMenu();
            return;
        }
        
        DrawSmallPanel(DetailsX1 + 2, DetailsY1);
        int townNameX1 = DetailsX1 + 2;
        if (town.NationId != 0)
        {
            townNameX1 += 8 + _colorSquareWidth * 2;
            int textureKey = ColorRemap.GetTextureKey(ColorRemap.ColorSchemes[town.NationId], false);
            Graphics.DrawBitmap(_colorSquareTextures[textureKey], DetailsX1 + 10, DetailsY1 + 3, _colorSquareWidth * 2, _colorSquareHeight * 2);
        }
        
        string townName = town.Name + (Config.ShowAIInfo ? " (" + town.TownId + ")" : "");
        PutTextCenter(FontSan, townName, townNameX1, DetailsY1, DetailsX2 - 4, DetailsY1 + 42);

        if (TownDetailsMode == TownDetailsMode.Spy)
        {
            DrawSpyList(town.NationId, town.GetPlayerSpies());
            return;
        }
        
        DrawSmallPanel(DetailsX1 + 2, DetailsY1 + 48);
        int populationTextY = DetailsY1 + 55;
        PutText(FontSan, "Population", DetailsX1 + 9, populationTextY);
        if (town.NationId != 0)
        {
            PutText(FontSan, "Peasants", DetailsX1 + 166, populationTextY);
            PutText(FontSan, "Loyalty", DetailsX1 + 310, populationTextY);
        }
        else
        {
            PutText(FontSan, "Peasants", DetailsX1 + 144, populationTextY);
            PutText(FontSan, "Resistance", DetailsX1 + 265, populationTextY);
        }

        bool needScroll = town.RacesPopulation.Count(population => population > 0) > 4;
        if (needScroll)
        {
            DrawListBoxPanelWithScroll(DetailsX1 + 2, DetailsY1 + 96);
            DrawListBoxScrollPanel(DetailsX1 + 377, DetailsY1 + 96);
        }
        else
        {
            DrawListBox4Panel(DetailsX1 + 2, DetailsY1 + 96);
        }

        if (_selectedRaceId != 0 && town.RacesPopulation[_selectedRaceId - 1] == 0)
            _selectedRaceId = 0;
        
        int raceY = DetailsY1 + 109;
        for (int i = 0; i < town.RacesPopulation.Length; i++)
        {
            if (town.RacesPopulation[i] == 0)
                continue;

            if (_selectedRaceId == 0)
                _selectedRaceId = i + 1;

            RaceInfo raceInfo = RaceRes[i + 1];
            Graphics.DrawBitmap(raceInfo.GetIconTexture(Graphics), DetailsX1 + 14, raceY, raceInfo.IconBitmapWidth * 2, raceInfo.IconBitmapHeight * 2);

            int textY = raceY + 4;
            PutText(FontMid, town.RacesPopulation[i].ToString(), DetailsX1 + 100, textY);
            PutText(FontMid, town.RacesJoblessPopulation[i].ToString(), DetailsX1 + 200, textY);

            int loyalty;
            int targetLoyalty;
            if (town.NationId != 0)
            {
                loyalty = (int)town.RacesLoyalty[i];
                targetLoyalty = town.RacesTargetLoyalty[i];
            }
            else
            {
                loyalty = NationArray.PlayerId != 0 ? (int)town.RacesResistance[i, NationArray.PlayerId - 1] : 0;
                targetLoyalty = NationArray.PlayerId != 0 ? town.RacesTargetResistance[i, NationArray.PlayerId - 1] : 0;
                if (targetLoyalty > loyalty)
                    targetLoyalty = loyalty;
            }

            string loyaltyString = loyalty.ToString();
            int loyaltyWidth = FontMid.TextWidth(loyaltyString);
            string targetLoyaltyString = targetLoyalty.ToString();
            int targetLoyaltyWidth = FontMid.TextWidth(targetLoyaltyString);
            if (targetLoyalty != -1 && targetLoyalty != loyalty)
            {
                int fullLoyaltyWidth = loyaltyWidth + targetLoyaltyWidth + 8;
                fullLoyaltyWidth += (targetLoyalty < loyalty ? _arrowDownWidth * 2 : _arrowUpWidth * 2);
                int loyaltyX = DetailsX1 + (needScroll ? 318 : 352) - fullLoyaltyWidth / 2;
                PutText(FontMid, loyaltyString, loyaltyX, textY);
                PutText(FontMid, targetLoyaltyString, loyaltyX + fullLoyaltyWidth - targetLoyaltyWidth, textY);
                if (targetLoyalty < loyalty)
                    Graphics.DrawBitmap(_arrowDownTexture, loyaltyX + loyaltyWidth + 2, textY + 7, _arrowDownWidth * 2, _arrowDownHeight * 2);
                else
                    Graphics.DrawBitmap(_arrowUpTexture, loyaltyX + loyaltyWidth + 2, textY + 7, _arrowUpWidth * 2, _arrowUpHeight * 2);
            }
            else
            {
                PutText(FontMid, loyaltyString, DetailsX1 + (needScroll ? 330 : 364) - loyaltyWidth / 2, textY);
            }

            if (_selectedRaceId == i + 1)
                DrawSelectedBorder(DetailsX1 + 8, raceY - 7, DetailsX1 + 405, raceY + 47);

            raceY += ListItemHeight;
        }

        DrawSmallPanel(DetailsX1 + 2, DetailsY1 + 333);
        int totalTextY = DetailsY1 + 340;
        PutText(FontSan, "Total", DetailsX1 + 9, totalTextY + 1);
        PutText(FontMid, town.Population.ToString(), DetailsX1 + 100, totalTextY - 1);
        PutText(FontMid, town.JoblessPopulation.ToString(), DetailsX1 + 200, totalTextY - 1);
        PutText(FontSan, "Avg", DetailsX1 + (needScroll ? 260 : 294), totalTextY + 1);
        int avgLoyaltyX = DetailsX1 + (needScroll ? 320 : 359);
        if (town.NationId != 0)
            PutText(FontMid, town.AverageLoyalty().ToString(), avgLoyaltyX, totalTextY - 1);
        else
            PutText(FontMid, NationArray.PlayerId != 0 ? town.AverageResistance(NationArray.PlayerId).ToString() : "0", avgLoyaltyX, totalTextY - 1);

        if (town.RebelId != 0)
        {
            bool hasButtons = ShowGrantToNonOwnTownButton(town) || town.HasPlayerSpy();
            DrawSmallPanel(DetailsX1 + 2, hasButtons ? DetailsY1 + 440 : DetailsY1 + 381);
            PutTextCenter(FontSan, "Controlled by Rebels", DetailsX1 + 2, hasButtons ? DetailsY1 + 460 : DetailsY1 + 401,
                DetailsX2 - 4, hasButtons ? DetailsY1 + 460 : DetailsY1 + 401);
        }

        if (NationArray.PlayerId != 0 && town.NationId == NationArray.PlayerId)
        {
            if (_selectedRaceId != 0 && town.CanRecruit(_selectedRaceId))
            {
                bool mouseOnButton = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                     _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmapScaled(_buttonDownTexture, Button1X, ButtonsTownY, _buttonDownWidth, _buttonDownHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonUpTexture, Button1X, ButtonsTownY, _buttonUpWidth, _buttonUpHeight);
                Graphics.DrawBitmapScaled(_buttonRecruitTexture, Button1X + 2, ButtonsTownY + 8, _buttonRecruitWidth, _buttonRecruitHeight);
            }
            else
            {
                Graphics.DrawBitmapScaled(_buttonDisabledTexture, Button1X, ButtonsTownY, _buttonDisabledWidth, _buttonDisabledHeight);
                Graphics.DrawBitmapScaled(_buttonRecruitDisabledTexture, Button1X + 2, ButtonsTownY + 8, _buttonRecruitWidth, _buttonRecruitHeight);
            }

            if (_selectedRaceId != 0 && IsTrainEnabled(town))
            {
                bool mouseOnButton = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                                     _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmapScaled(_buttonDownTexture, Button2X, ButtonsTownY, _buttonDownWidth, _buttonDownHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonUpTexture, Button2X, ButtonsTownY, _buttonUpWidth, _buttonUpHeight);
                Graphics.DrawBitmapScaled(_buttonTrainTexture, Button2X + 4, ButtonsTownY + 4, _buttonTrainWidth, _buttonTrainHeight);
            }
            else
            {
                Graphics.DrawBitmapScaled(_buttonDisabledTexture, Button2X, ButtonsTownY, _buttonDisabledWidth, _buttonDisabledHeight);
                Graphics.DrawBitmapScaled(_buttonTrainDisabledTexture, Button2X + 4, ButtonsTownY + 4, _buttonTrainWidth, _buttonTrainHeight);
            }

            if (town.HasPlayerSpy())
            {
                bool mouseOnButton = _mouseButtonX >= Button3X + 2 && _mouseButtonX <= Button3X + ButtonWidth &&
                                     _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmapScaled(_buttonDownTexture, Button3X, ButtonsTownY, _buttonDownWidth, _buttonDownHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonUpTexture, Button3X, ButtonsTownY, _buttonUpWidth, _buttonUpHeight);
                Graphics.DrawBitmapScaled(_buttonSpyMenuTexture, Button3X + 4, ButtonsTownY + 16, _buttonSpyMenuWidth, _buttonSpyMenuHeight);
            }

            if (IsCollectTaxEnabled(town))
            {
                bool mouseOnButton = _mouseButtonX >= Button4X + 2 && _mouseButtonX <= Button4X + ButtonWidth &&
                                     _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmapScaled(_buttonDownTexture, Button4X, ButtonsTownY, _buttonDownWidth, _buttonDownHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonUpTexture, Button4X, ButtonsTownY, _buttonUpWidth, _buttonUpHeight);
                Graphics.DrawBitmapScaled(_buttonCollectTaxTexture, Button4X + 10, ButtonsTownY + 4, _buttonCollectTaxWidth, _buttonCollectTaxHeight);
            }
            else
            {
                Graphics.DrawBitmapScaled(_buttonDisabledTexture, Button4X, ButtonsTownY, _buttonDisabledWidth, _buttonDisabledHeight);
                Graphics.DrawBitmapScaled(_buttonCollectTaxDisabledTexture, Button4X + 10, ButtonsTownY + 4, _buttonCollectTaxWidth, _buttonCollectTaxHeight);
            }

            if (IsGrantEnabled(town))
            {
                bool mouseOnButton = _mouseButtonX >= Button5X + 2 && _mouseButtonX <= Button5X + ButtonWidth &&
                                     _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmapScaled(_buttonDownTexture, Button5X, ButtonsTownY, _buttonDownWidth, _buttonDownHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonUpTexture, Button5X, ButtonsTownY, _buttonUpWidth, _buttonUpHeight);
                Graphics.DrawBitmapScaled(_buttonGrantTexture, Button5X + 2, ButtonsTownY + 4, _buttonGrantWidth, _buttonGrantHeight);
            }
            else
            {
                Graphics.DrawBitmapScaled(_buttonDisabledTexture, Button5X, ButtonsTownY, _buttonDisabledWidth, _buttonDisabledHeight);
                Graphics.DrawBitmapScaled(_buttonGrantDisabledTexture, Button5X + 2, ButtonsTownY + 4, _buttonGrantWidth, _buttonGrantHeight);
            }
        }
        else
        {
            if (ShowGrantToNonOwnTownButton(town))
            {
                if (IsGrantToNonOwnTownEnabled(town))
                {
                    bool mouseOnButton = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                         _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
                    if (_leftMousePressed && mouseOnButton)
                        Graphics.DrawBitmapScaled(_buttonDownTexture, Button1X, ButtonsTownY, _buttonDownWidth, _buttonDownHeight);
                    else
                        Graphics.DrawBitmapScaled(_buttonUpTexture, Button1X, ButtonsTownY, _buttonUpWidth, _buttonUpHeight);
                    Graphics.DrawBitmapScaled(_buttonGrantTexture, Button1X + 2, ButtonsTownY + 4, _buttonGrantWidth, _buttonGrantHeight);
                }
                else
                {
                    Graphics.DrawBitmapScaled(_buttonDisabledTexture, Button1X, ButtonsTownY, _buttonDisabledWidth, _buttonDisabledHeight);
                    Graphics.DrawBitmapScaled(_buttonGrantDisabledTexture, Button1X + 2, ButtonsTownY + 4, _buttonGrantWidth, _buttonGrantHeight);
                }
            }

            if (town.HasPlayerSpy())
            {
                int buttonSpyMenuX = ShowGrantToNonOwnTownButton(town) ? Button2X : Button1X;
                bool mouseOnButton = _mouseButtonX >= buttonSpyMenuX + 2 && _mouseButtonX <= buttonSpyMenuX + ButtonWidth &&
                                     _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmapScaled(_buttonDownTexture, buttonSpyMenuX, ButtonsTownY, _buttonDownWidth, _buttonDownHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonUpTexture, buttonSpyMenuX, ButtonsTownY, _buttonUpWidth, _buttonUpHeight);
                Graphics.DrawBitmapScaled(_buttonSpyMenuTexture, buttonSpyMenuX + 4, ButtonsTownY + 16, _buttonSpyMenuWidth, _buttonSpyMenuHeight);
            }
        }

        if (town.AutoCollectTaxLoyalty != 0)
        {
            Graphics.DrawRect(Button4X + 13, ButtonsTownY + 13, 46, 30, Colors.V_WHITE);
            Graphics.DrawFrame(Button4X + 13, ButtonsTownY + 13, 46, 30, Colors.V_BLACK);
            PutTextCenter(FontMid, town.AutoCollectTaxLoyalty.ToString(), Button4X, ButtonsTownY + 25, Button4X + Scale(_buttonUpWidth), ButtonsTownY + 25);
        }

        if (town.AutoGrantLoyalty != 0)
        {
            Graphics.DrawRect(Button5X + 13, ButtonsTownY + 13, 46, 30, Colors.V_WHITE);
            Graphics.DrawFrame(Button5X + 13, ButtonsTownY + 13, 46, 30, Colors.V_BLACK);
            PutTextCenter(FontMid, town.AutoGrantLoyalty.ToString(), Button5X, ButtonsTownY + 25, Button5X + Scale(_buttonUpWidth), ButtonsTownY + 25);
        }
    }

    private void HandleTownDetailsInput(Town town)
    {
        if (TownDetailsMode == TownDetailsMode.Train)
        {
            HandleTrainMenuInput(town);
            return;
        }

        if (TownDetailsMode == TownDetailsMode.CollectTax || TownDetailsMode == TownDetailsMode.Grant)
        {
            HandleAutoCollectTaxOrGrantMenu(town);
            return;
        }

        bool colorSquareButtonPressed = _leftMouseReleased && _mouseButtonX >= DetailsX1 + 18 && _mouseButtonX <= DetailsX1 + 48 &&
                                        _mouseButtonY >= DetailsY1 + 9 && _mouseButtonY <= DetailsY1 + 32;
        if (colorSquareButtonPressed)
            GoToLocation(town.LocCenterX, town.LocCenterY);

        if (TownDetailsMode == TownDetailsMode.Spy)
        {
            HandleSpyList(town.NationId, town.GetPlayerSpies());
            return;
        }

        bool race1Selected = _leftMouseReleased && _mouseButtonX >= DetailsX1 + 11 && _mouseButtonX <= DetailsX1 + 402 &&
                             _mouseButtonY >= DetailsY1 + 105 && _mouseButtonY <= DetailsY1 + 153;
        bool race2Selected = _leftMouseReleased && _mouseButtonX >= DetailsX1 + 11 && _mouseButtonX <= DetailsX1 + 402 &&
                             _mouseButtonY >= DetailsY1 + 105 + ListItemHeight && _mouseButtonY <= DetailsY1 + 153 + ListItemHeight;
        bool race3Selected = _leftMouseReleased && _mouseButtonX >= DetailsX1 + 11 && _mouseButtonX <= DetailsX1 + 402 &&
                             _mouseButtonY >= DetailsY1 + 105 + 2 * ListItemHeight && _mouseButtonY <= DetailsY1 + 153 + 2 * ListItemHeight;
        bool race4Selected = _leftMouseReleased && _mouseButtonX >= DetailsX1 + 11 && _mouseButtonX <= DetailsX1 + 402 &&
                             _mouseButtonY >= DetailsY1 + 105 + 3 * ListItemHeight && _mouseButtonY <= DetailsY1 + 153 + 3 * ListItemHeight;

        int raceIndex = 0;
        for (int i = 0; i < town.RacesPopulation.Length; i++)
        {
            if (town.RacesPopulation[i] == 0)
                continue;

            raceIndex++;
            if ((race1Selected && raceIndex == 1) || (race2Selected && raceIndex == 2) ||
                (race3Selected && raceIndex == 3) || (race4Selected && raceIndex == 4))
            {
                _selectedRaceId = i + 1;
                break;
            }
        }

        bool mouseOnButton1 = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                              _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
        bool mouseOnButton2 = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                              _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
        bool mouseOnButton3 = _mouseButtonX >= Button3X + 2 && _mouseButtonX <= Button3X + ButtonWidth &&
                              _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
        bool mouseOnButton4 = _mouseButtonX >= Button4X + 2 && _mouseButtonX <= Button4X + ButtonWidth &&
                              _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
        bool mouseOnButton5 = _mouseButtonX >= Button5X + 2 && _mouseButtonX <= Button5X + ButtonWidth &&
                              _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;

        if (NationArray.PlayerId != 0 && town.NationId == NationArray.PlayerId)
        {
            if (_leftMouseReleased && mouseOnButton1 && _selectedRaceId != 0 && town.CanRecruit(_selectedRaceId))
            {
                town.Recruit(-1, _selectedRaceId, InternalConstants.COMMAND_PLAYER);
            }

            if (_leftMouseReleased && mouseOnButton2 && _selectedRaceId != 0 && IsTrainEnabled(town))
            {
                TownDetailsMode = TownDetailsMode.Train;
            }
            
            if (_leftMouseReleased && mouseOnButton3 && town.HasPlayerSpy())
            {
                TownDetailsMode = TownDetailsMode.Spy;
            }

            if (_leftMouseReleased && mouseOnButton4 && IsCollectTaxEnabled(town))
            {
                town.CollectTax(InternalConstants.COMMAND_PLAYER);
                SECtrl.immediate_sound("TURN_ON");
            }

            if (_rightMouseReleased && mouseOnButton4)
            {
                TownDetailsMode = TownDetailsMode.CollectTax;
            }

            if (_leftMouseReleased && mouseOnButton5 && IsGrantEnabled(town))
            {
                town.Reward(InternalConstants.COMMAND_PLAYER);
                SECtrl.immediate_sound("TURN_ON");
            }
            
            if (_rightMouseReleased && mouseOnButton5)
            {
                TownDetailsMode = TownDetailsMode.Grant;
            }
        }
        else
        {
            if (_leftMouseReleased && mouseOnButton1 && IsGrantToNonOwnTownEnabled(town))
            {
                town.GrantToNonOwnTown(NationArray.PlayerId, InternalConstants.COMMAND_PLAYER);
                SECtrl.immediate_sound("TURN_ON");
            }
            
            bool mouseOnSpyListButton = (ShowGrantToNonOwnTownButton(town) ? mouseOnButton2 : mouseOnButton1);
            if (_leftMouseReleased && mouseOnSpyListButton && town.HasPlayerSpy())
            {
                TownDetailsMode = TownDetailsMode.Spy;
            }
        }
    }

    private bool IsTrainEnabled(Town town)
    {
        return town.HasLinkedOwnCamp && town.CanTrain(_selectedRaceId);
    }
    
    private bool IsCollectTaxEnabled(Town town)
    {
        return town.HasLinkedOwnCamp && town.AverageLoyalty() >= GameConstants.REBEL_LOYALTY;
    }

    private bool IsGrantEnabled(Town town)
    {
        return town.HasLinkedOwnCamp && NationArray.Player.Cash > 0.0;
    }

    private bool ShowGrantToNonOwnTownButton(Town town)
    {
        return NationArray.PlayerId != 0 && town.HasLinkedCamp(NationArray.PlayerId, false);
    }

    private bool IsGrantToNonOwnTownEnabled(Town town)
    {
        return NationArray.PlayerId != 0 && town.CanGrantToNonOwnTown(NationArray.PlayerId) && NationArray.Player.Cash > 0.0;
    }

    private void DrawTrainMenu(Town town)
    {
        Dictionary<int, int> trainSkillCounts = new Dictionary<int, int>();
        for (int i = Skill.SKILL_CONSTRUCTION; i <= Skill.MAX_TRAINABLE_SKILL; i++)
        {
            trainSkillCounts.Add(i, 0);
        }

        for (int i = 0; i < town.TrainSkillQueue.Count; i++)
        {
            trainSkillCounts[town.TrainSkillQueue[i]]++;
        }

        if (town.TrainUnitId != 0)
        {
            Unit trainUnit = UnitArray[town.TrainUnitId];
            if (trainUnit.Skill.SkillId == 0 && trainUnit.SpyId != 0)
                trainSkillCounts[Skill.SKILL_SPYING]++;
            else
                trainSkillCounts[trainUnit.Skill.SkillId]++;
        }

        IntPtr[] skillIconTextures =
        {
            _buttonConstructionSkillTexture, _buttonLeadershipSkillTexture, _buttonMineSkillTexture,
            _buttonManufactureSkillTexture, _buttonResearchSkillTexture, _buttonSpySkillTexture
        };
        int[] skillIconWidths =
        {
            _buttonConstructionSkillWidth, _buttonLeadershipSkillWidth, _buttonMineSkillWidth,
            _buttonManufactureSkillWidth, _buttonResearchSkillWidth, _buttonSpySkillWidth
        };

        int[] skillIconHeights =
        {
            _buttonConstructionSkillHeight, _buttonLeadershipSkillHeight, _buttonMineSkillHeight,
            _buttonManufactureSkillHeight, _buttonResearchSkillHeight, _buttonSpySkillHeight
        };
        
        DrawSmallPanel(DetailsX1 + 2, DetailsY1);
        PutTextCenter(FontSan, "Train (Cost: $30, Skill: 20)", DetailsX1 + 2, DetailsY1, DetailsX2 - 4, DetailsY1 + 42);

        int dy = 0;
        for (int i = Skill.SKILL_CONSTRUCTION; i <= Skill.MAX_TRAINABLE_SKILL; i++)
        {
            bool mouseOnButton = _mouseButtonX >= MouseOnTrainButtonX1 && _mouseButtonX <= MouseOnTrainButtonX2 &&
                                 _mouseButtonY >= MouseOnTrainButtonY1 + dy && _mouseButtonY <= MouseOnTrainButtonY2 + dy;
            if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
                DrawSkillPanelDown(TrainSkillPanelX, TrainSkillPanelY + dy);
            else
                DrawSkillPanelUp(TrainSkillPanelX, TrainSkillPanelY + dy);
            Graphics.DrawBitmap(skillIconTextures[i - 1], DetailsX1 + 2, DetailsY1 + 59 + dy, skillIconWidths[i - 1], skillIconHeights[i - 1]);
            PutText(FontBible, Skill.SkillDescriptions[i - 1], TrainButtonTextX, DetailsY1 + 53 + dy);
            mouseOnButton = _mouseButtonX >= MouseOnTrainNumberButtonX1 && _mouseButtonX <= MouseOnTrainNumberButtonX2 &&
                            _mouseButtonY >= MouseOnTrainNumberButtonY1 + dy && _mouseButtonY <= MouseOnTrainNumberButtonY2 + dy;
            if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
                DrawNumberPanelDown(TrainButtonNumberX, TrainButtonNumberY + dy);
            else
                DrawNumberPanelUp(TrainButtonNumberX, TrainButtonNumberY + dy);
            PutText(FontBible, trainSkillCounts[i].ToString(), TrainButtonNumberX + 14, DetailsY1 + 53 + dy);

            dy += 63;
        }

        bool mouseOnDoneButton = _mouseButtonX >= MouseOnTrainButtonX1 && _mouseButtonX <= MouseOnTrainButtonX2 + 82 &&
                                 _mouseButtonY >= MouseOnTrainButtonY1 + dy && _mouseButtonY <= MouseOnTrainButtonY2 + dy;
        if ((_leftMousePressed || _rightMousePressed) && mouseOnDoneButton)
            DrawSkillPanelDown(TrainSkillPanelX, TrainSkillPanelY + dy);
        else
            DrawSkillPanelUp(TrainSkillPanelX, TrainSkillPanelY + dy);
        PutTextCenter(FontBible, "Done", DetailsX1 + 2, DetailsY1 + 53 + dy, DetailsX2 - 4, DetailsY1 + 53 + dy + 40);
    }
    
    private void HandleTrainMenuInput(Town town)
    {
        int selectedTrainSkill = 0;

        if (_leftMouseReleased || _rightMouseReleased)
        {
            int dy = 0;
            for (int i = Skill.SKILL_CONSTRUCTION; i <= Skill.MAX_TRAINABLE_SKILL; i++)
            {
                if (_mouseButtonX >= MouseOnTrainButtonX1 && _mouseButtonX <= MouseOnTrainButtonX2 &&
                    _mouseButtonY >= MouseOnTrainButtonY1 + dy && _mouseButtonY <= MouseOnTrainButtonY2 + dy)
                {
                    selectedTrainSkill = i;
                    TownDetailsMode = TownDetailsMode.Normal;
                }
                
                if (_mouseButtonX >= MouseOnTrainNumberButtonX1 && _mouseButtonX <= MouseOnTrainNumberButtonX2 &&
                    _mouseButtonY >= MouseOnTrainNumberButtonY1 + dy && _mouseButtonY <= MouseOnTrainNumberButtonY2 + dy)
                {
                    selectedTrainSkill = i;
                }
                
                dy += 63;
            }

            if (_mouseButtonX >= MouseOnTrainButtonX1 && _mouseButtonX <= MouseOnTrainButtonX2 + 82 &&
                _mouseButtonY >= MouseOnTrainButtonY1 + dy && _mouseButtonY <= MouseOnTrainButtonY2 + dy)
            {
                TownDetailsMode = TownDetailsMode.Normal;
            }
        }

        if (selectedTrainSkill != 0)
        {
            /*if (remote.is_enable())
            {
                // packet structure : <town recno> <skill id> <race id> <amount>
                short *shortPtr = (short *)remote.new_send_queue_msg(MSG_TOWN_RECRUIT, 4*sizeof(short) );
                shortPtr[0] = town_recno;
                shortPtr[1] = b;
                if (_leftMouseReleased)
                    shortPtr[2] = race_filter(browse_race.recno());
                if (_rightMouseReleased)
                    shortPtr[2] = -1;
                shortPtr[3] = (short)trainCancelAmount;
            }*/
            //else
            //{
                if (_leftMouseReleased)
                    town.AddQueue(selectedTrainSkill, _selectedRaceId);
                if (_rightMouseReleased)
                    town.RemoveQueue(selectedTrainSkill);
            //}
            
            if (_leftMouseReleased)
                SECtrl.immediate_sound("TURN_ON");
            if (_rightMouseReleased)
                SECtrl.immediate_sound("TURN_OFF");
        }
    }
    
    private void DrawAutoCollectTaxOrGrantMenu()
    {
        string header = String.Empty;
        if (TownDetailsMode == TownDetailsMode.CollectTax)
            header = "Automatically collect tax";
        if (TownDetailsMode == TownDetailsMode.Grant)
            header = "Automatically grant money";
        
        DrawAutoTaxTextPanel(DetailsX1 + 2, DetailsY1);
        PutTextCenter(FontSan, header, DetailsX1 + 2, DetailsY1 + 2, DetailsX2 - 4, DetailsY1 + 36);
        PutText(FontSan, "Left-click for this village", DetailsX1 + 8, DetailsY1 + 35);
        PutText(FontSan, "Right-click for all villages", DetailsX1 + 8, DetailsY1 + 65);
        
        int dy = 0;
        for (int i = 30; i <= 120; i += 10)
        {
            bool mouseOnButton = _mouseButtonX >= DetailsX1 + 8 && _mouseButtonX <= DetailsX2 - 8 &&
                                 _mouseButtonY >= MouseOnTaxButtonY + dy && _mouseButtonY <= MouseOnTaxButtonY + 24 + dy;
            if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
                DrawTaxPanelDown(TaxButtonX, TaxButtonY + dy);
            else
                DrawTaxPanelUp(TaxButtonX, TaxButtonY + dy);

            string text = i.ToString();
            if (i == 110)
                text = "Disabled";
            if (i == 120)
                text = "Cancel";
            PutTextCenter(FontSan, text, DetailsX1 + 2, TaxButtonY + 19 + dy, DetailsX2 - 4, TaxButtonY + 19 + dy, true);

            dy += 34;
        }
    }
    
    private void HandleAutoCollectTaxOrGrantMenu(Town town)
    {
        int dy = 0;
        for (int i = 30; i <= 120; i += 10)
        {
            bool mouseOnButton = _mouseButtonX >= DetailsX1 + 8 && _mouseButtonX <= DetailsX2 - 8 &&
                                 _mouseButtonY >= MouseOnTaxButtonY + dy && _mouseButtonY <= MouseOnTaxButtonY + 24 + dy;

            if (mouseOnButton)
            {
                if (i <= 110)
                {
                    int loyaltyLevel = i;
                    if (loyaltyLevel == 110)
                        loyaltyLevel = 0;

                    if (_leftMouseReleased)
                    {
                        if (TownDetailsMode == TownDetailsMode.CollectTax)
                        {
                            /*if (remote.is_enable())
                            {
                                // packet structure <town recno> <loyalty level>
                                short *shortPtr = (short *)remote.new_send_queue_msg(MSG_TOWN_AUTO_TAX, 2*sizeof(short) );
                                *shortPtr = town_recno;
                                shortPtr[1] = loyaltyLevel;
                            }*/
                            //else
                            //{
                                town.SetAutoCollectTaxLoyalty(loyaltyLevel);
                            //}
                        }

                        if (TownDetailsMode == TownDetailsMode.Grant)
                        {
                            /*if (remote.is_enable())
                            {
				                // packet structure <town recno> <loyalty level>
				                short *shortPtr = (short *)remote.new_send_queue_msg(MSG_TOWN_AUTO_GRANT, 2*sizeof(short) );
				                *shortPtr = town_recno;
				                shortPtr[1] = loyaltyLevel;
                            }*/
                            //else
                            //{
                                town.SetAutoGrantLoyalty(loyaltyLevel);
                            //}
                        }
                        
                        SECtrl.immediate_sound("TURN_ON");
                        TownDetailsMode = TownDetailsMode.Normal;
                    }

                    if (_rightMouseReleased)
                    {
                        if (TownDetailsMode == TownDetailsMode.CollectTax)
                        {
                            /*if (remote.is_enable())
                            {
                                // packet structure <-nation recno> <loyalty level>
                                short *shortPtr = (short *)remote.new_send_queue_msg(MSG_TOWN_AUTO_TAX, 2*sizeof(short) );
                                *shortPtr = -nation_recno;
                                shortPtr[1] = loyaltyLevel;
                            }*/
                            //else
                            //{
                                Nation nation = NationArray[town.NationId];
                                nation.SetAutoCollectTaxLoyalty(loyaltyLevel);
                                foreach (Town nationTown in TownArray)
                                {
                                    nationTown.SetAutoCollectTaxLoyalty(nation.AutoCollectTaxLoyalty);
                                }
                            //}
                        }

                        if (TownDetailsMode == TownDetailsMode.Grant)
                        {
                            /*if (remote.is_enable())
                            {
				                // packet structure <-nation recno> <loyalty level>
				                short *shortPtr = (short *)remote.new_send_queue_msg(MSG_TOWN_AUTO_GRANT, 2*sizeof(short) );
				                *shortPtr = -nation_recno;
				                shortPtr[1] = loyaltyLevel;
                            }*/
                            //else
                            //{
                                Nation nation = NationArray[town.NationId];
                                nation.SetAutoGrantLoyalty(loyaltyLevel);
                                foreach (Town nationTown in TownArray)
                                {
                                    nationTown.SetAutoGrantLoyalty(nation.AutoCollectTaxLoyalty);
                                }
                            //}
                        }
                        
                        SECtrl.immediate_sound("TURN_ON");
                        TownDetailsMode = TownDetailsMode.Normal;
                    }
                }

                if (i == 120)
                {
                    if (_leftMouseReleased || _rightMouseReleased)
                    {
                        SECtrl.immediate_sound("TURN_OFF");
                        TownDetailsMode = TownDetailsMode.Normal;
                    }
                }
            }
            
            dy += 34;
        }
    }
}