using System.Collections.Generic;
using System.Linq;

namespace TenKingdoms;

enum TownDetailsMode { Normal, Train, Spy }

public partial class Renderer
{
    // TODO show spies list, auto collect tax, auto grant
    // TODO Controlled by Rebels panel
    // TODO train unit progress
    // TODO display auto collect and auto grant values
    // TODO go to town location when pressing color square
    // TODO list box scroll

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
    private const int MouseOnTrainNumberButtonY1 = DetailsY1 + 59;
    private const int MouseOnTrainNumberButtonY2 = DetailsY1 + 59 + 38;

    private TownDetailsMode TownDetailsMode { get; set; } = TownDetailsMode.Normal;
    
    private void DrawTownDetails(Town town)
    {
        if (TownDetailsMode == TownDetailsMode.Train)
        {
            DrawTrainMenu(town);
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
        PutTextCenter(FontSan, town.Name, townNameX1, DetailsY1, DetailsX2 - 4, DetailsY1 + 42);
        
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
            DrawListBoxPanel(DetailsX1 + 2, DetailsY1 + 96);
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
            Graphics.DrawBitmap(raceInfo.GetIconTexture(Graphics), DetailsX1 + 14, raceY, raceInfo.iconBitmapWidth * 2, raceInfo.iconBitmapHeight * 2);

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
                loyalty = NationArray.player_recno != 0 ? (int)town.RacesResistance[i, NationArray.player_recno - 1] : 0;
                targetLoyalty = NationArray.player_recno != 0 ? town.RacesTargetResistance[i, NationArray.player_recno - 1] : 0;
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

            raceY += RaceHeight;
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
            PutText(FontMid, NationArray.player_recno != 0 ? town.AverageResistance(NationArray.player_recno).ToString() : "0", avgLoyaltyX, totalTextY - 1);

        if (NationArray.player_recno != 0 && town.NationId == NationArray.player_recno && _selectedRaceId != 0)
        {
            if (town.CanRecruit(_selectedRaceId))
            {
                bool mouseOnButton = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                     _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmap(_buttonDownTexture, Button1X, ButtonsTownY, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
                else
                    Graphics.DrawBitmap(_buttonUpTexture, Button1X, ButtonsTownY, Scale(_buttonUpWidth), Scale(_buttonUpHeight));
                Graphics.DrawBitmap(_buttonRecruitTexture, Button1X + 2, ButtonsTownY + 8, Scale(_buttonRecruitWidth), Scale(_buttonRecruitHeight));
            }
            else
            {
                Graphics.DrawBitmap(_buttonDisabledTexture, Button1X, ButtonsTownY, Scale(_buttonDisabledWidth), Scale(_buttonDisabledHeight));
                Graphics.DrawBitmap(_buttonRecruitDisabledTexture, Button1X + 2, ButtonsTownY + 8, Scale(_buttonRecruitWidth), Scale(_buttonRecruitHeight));
            }

            if (IsTrainEnabled(town))
            {
                bool mouseOnButton = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                                     _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmap(_buttonDownTexture, Button2X, ButtonsTownY, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
                else
                    Graphics.DrawBitmap(_buttonUpTexture, Button2X, ButtonsTownY, Scale(_buttonUpWidth), Scale(_buttonUpHeight));
                Graphics.DrawBitmap(_buttonTrainTexture, Button2X + 4, ButtonsTownY + 4, Scale(_buttonTrainWidth), Scale(_buttonTrainHeight));
            }
            else
            {
                Graphics.DrawBitmap(_buttonDisabledTexture, Button2X, ButtonsTownY, Scale(_buttonDisabledWidth), Scale(_buttonDisabledHeight));
                Graphics.DrawBitmap(_buttonTrainDisabledTexture, Button2X + 4, ButtonsTownY + 4, Scale(_buttonTrainWidth), Scale(_buttonTrainHeight));
            }

            if (town.HasPlayerSpy())
            {
                bool mouseOnButton = _mouseButtonX >= Button3X + 2 && _mouseButtonX <= Button3X + ButtonWidth &&
                                     _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmap(_buttonDownTexture, Button3X, ButtonsTownY, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
                else
                    Graphics.DrawBitmap(_buttonUpTexture, Button3X, ButtonsTownY, Scale(_buttonUpWidth), Scale(_buttonUpHeight));
                Graphics.DrawBitmap(_buttonSpyMenuTexture, Button3X + 4, ButtonsTownY + 16, Scale(_buttonSpyMenuWidth), Scale(_buttonSpyMenuHeight));
            }

            if (IsCollectTaxEnabled(town))
            {
                bool mouseOnButton = _mouseButtonX >= Button4X + 2 && _mouseButtonX <= Button4X + ButtonWidth &&
                                     _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmap(_buttonDownTexture, Button4X, ButtonsTownY, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
                else
                    Graphics.DrawBitmap(_buttonUpTexture, Button4X, ButtonsTownY, Scale(_buttonUpWidth), Scale(_buttonUpHeight));
                Graphics.DrawBitmap(_buttonCollectTaxTexture, Button4X + 10, ButtonsTownY + 4, Scale(_buttonCollectTaxWidth), Scale(_buttonCollectTaxHeight));
            }
            else
            {
                Graphics.DrawBitmap(_buttonDisabledTexture, Button4X, ButtonsTownY, Scale(_buttonDisabledWidth), Scale(_buttonDisabledHeight));
                Graphics.DrawBitmap(_buttonCollectTaxDisabledTexture, Button4X + 10, ButtonsTownY + 4, Scale(_buttonCollectTaxWidth), Scale(_buttonCollectTaxHeight));
            }

            if (IsGrantEnabled(town))
            {
                bool mouseOnButton = _mouseButtonX >= Button5X + 2 && _mouseButtonX <= Button5X + ButtonWidth &&
                                     _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmap(_buttonDownTexture, Button5X, ButtonsTownY, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
                else
                    Graphics.DrawBitmap(_buttonUpTexture, Button5X, ButtonsTownY, Scale(_buttonUpWidth), Scale(_buttonUpHeight));
                Graphics.DrawBitmap(_buttonGrantTexture, Button5X + 2, ButtonsTownY + 4, Scale(_buttonGrantWidth), Scale(_buttonGrantHeight));
            }
            else
            {
                Graphics.DrawBitmap(_buttonDisabledTexture, Button5X, ButtonsTownY, Scale(_buttonDisabledWidth), Scale(_buttonDisabledHeight));
                Graphics.DrawBitmap(_buttonGrantDisabledTexture, Button5X + 2, ButtonsTownY + 4, Scale(_buttonGrantWidth), Scale(_buttonGrantHeight));
            }
        }
        else
        {
            bool displayGrantButton = NationArray.player_recno != 0 && town.HasLinkedCamp(NationArray.player_recno, false);
            if (displayGrantButton)
            {
                if (IsGrantToNonOwnTownEnabled(town))
                {
                    bool mouseOnButton = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                         _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
                    if (_leftMousePressed && mouseOnButton)
                        Graphics.DrawBitmap(_buttonDownTexture, Button1X, ButtonsTownY, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
                    else
                        Graphics.DrawBitmap(_buttonUpTexture, Button1X, ButtonsTownY, Scale(_buttonUpWidth), Scale(_buttonUpHeight));
                    Graphics.DrawBitmap(_buttonGrantTexture, Button1X + 2, ButtonsTownY + 4, Scale(_buttonGrantWidth), Scale(_buttonGrantHeight));
                }
                else
                {
                    Graphics.DrawBitmap(_buttonDisabledTexture, Button1X, ButtonsTownY, Scale(_buttonDisabledWidth), Scale(_buttonDisabledHeight));
                    Graphics.DrawBitmap(_buttonGrantDisabledTexture, Button1X + 2, ButtonsTownY + 4, Scale(_buttonGrantWidth),
                        Scale(_buttonGrantHeight));
                }
            }

            if (town.HasPlayerSpy())
            {
                int buttonSpyMenuX = displayGrantButton ? Button2X : Button1X;
                bool mouseOnButton = _mouseButtonX >= buttonSpyMenuX + 2 && _mouseButtonX <= buttonSpyMenuX + ButtonWidth &&
                                     _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmap(_buttonDownTexture, buttonSpyMenuX, ButtonsTownY, Scale(_buttonDownWidth), Scale(_buttonDownHeight));
                else
                    Graphics.DrawBitmap(_buttonUpTexture, buttonSpyMenuX, ButtonsTownY, Scale(_buttonUpWidth), Scale(_buttonUpHeight));
                Graphics.DrawBitmap(_buttonSpyMenuTexture, buttonSpyMenuX + 4, ButtonsTownY + 16, Scale(_buttonSpyMenuWidth), Scale(_buttonSpyMenuHeight));
            }
        }
    }

    private void HandleTownDetailsInput(Town town)
    {
        if (TownDetailsMode == TownDetailsMode.Train)
        {
            HandleTrainMenuInput(town);
            return;
        }

        bool race1Selected = _leftMouseReleased && _mouseButtonX >= DetailsX1 + 11 && _mouseButtonX <= DetailsX1 + 402 &&
                             _mouseButtonY >= DetailsY1 + 105 && _mouseButtonY <= DetailsY1 + 153;
        bool race2Selected = _leftMouseReleased && _mouseButtonX >= DetailsX1 + 11 && _mouseButtonX <= DetailsX1 + 402 &&
                             _mouseButtonY >= DetailsY1 + 105 + RaceHeight && _mouseButtonY <= DetailsY1 + 153 + RaceHeight;
        bool race3Selected = _leftMouseReleased && _mouseButtonX >= DetailsX1 + 11 && _mouseButtonX <= DetailsX1 + 402 &&
                             _mouseButtonY >= DetailsY1 + 105 + 2 * RaceHeight && _mouseButtonY <= DetailsY1 + 153 + 2 * RaceHeight;
        bool race4Selected = _leftMouseReleased && _mouseButtonX >= DetailsX1 + 11 && _mouseButtonX <= DetailsX1 + 402 &&
                             _mouseButtonY >= DetailsY1 + 105 + 3 * RaceHeight && _mouseButtonY <= DetailsY1 + 153 + 3 * RaceHeight;

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

        bool button1Pressed = _leftMouseReleased && _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                              _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
        bool button2Pressed = _leftMouseReleased && _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                              _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
        bool button3Pressed = _leftMouseReleased && _mouseButtonX >= Button3X + 2 && _mouseButtonX <= Button3X + ButtonWidth &&
                              _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
        bool button4Pressed = _leftMouseReleased && _mouseButtonX >= Button4X + 2 && _mouseButtonX <= Button4X + ButtonWidth &&
                              _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
        bool button5Pressed = _leftMouseReleased && _mouseButtonX >= Button5X + 2 && _mouseButtonX <= Button5X + ButtonWidth &&
                              _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;

        if (NationArray.player_recno != 0 && town.NationId == NationArray.player_recno && _selectedRaceId != 0)
        {
            if (button1Pressed && town.CanRecruit(_selectedRaceId))
            {
                town.Recruit(-1, _selectedRaceId, InternalConstants.COMMAND_PLAYER);
            }

            if (button2Pressed && IsTrainEnabled(town))
            {
                TownDetailsMode = TownDetailsMode.Train;
            }
            
            if (button3Pressed && town.HasPlayerSpy())
            {
                // TODO show spies list
            }

            if (button4Pressed && IsCollectTaxEnabled(town))
            {
                SECtrl.immediate_sound("TURN_ON");
                town.CollectTax(InternalConstants.COMMAND_PLAYER);
            }

            if (button5Pressed && IsGrantEnabled(town))
            {
                SECtrl.immediate_sound("TURN_ON");
                town.Reward(InternalConstants.COMMAND_PLAYER);
            }
        }
        else
        {
            if (button1Pressed && IsGrantToNonOwnTownEnabled(town))
            {
                // TODO grant
            }
            
            bool displayGrantButton = NationArray.player_recno != 0 && town.HasLinkedCamp(NationArray.player_recno, false);
            bool spiesButtonPressed = (displayGrantButton ? button2Pressed : button1Pressed);
            if (spiesButtonPressed && town.HasPlayerSpy())
            {
                // TODO show spies list
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
        return town.HasLinkedOwnCamp && NationArray.player.cash > 0.0;
    }

    private bool IsGrantToNonOwnTownEnabled(Town town)
    {
        return NationArray.player_recno != 0 && town.CanGrantToNonOwnTown(NationArray.player_recno) && NationArray.player.cash > 0.0;
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
        
        DrawSmallPanel(DetailsX1 + 2, DetailsY1);
        PutTextCenter(FontSan, "Train (Cost: $30, Skill: 20)", DetailsX1 + 2, DetailsY1, DetailsX2 - 4, DetailsY1 + 42);

        bool mouseOnButton = _mouseButtonX >= MouseOnTrainButtonX1 && _mouseButtonX <= MouseOnTrainButtonX2 &&
                             _mouseButtonY >= MouseOnTrainButtonY1 && _mouseButtonY <= MouseOnTrainButtonY2;
        if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
            DrawSkillPanelDown(TrainSkillPanelX, TrainSkillPanelY);
        else
            DrawSkillPanelUp(TrainSkillPanelX, TrainSkillPanelY);
        Graphics.DrawBitmap(_buttonConstructionSkillTexture, DetailsX1 + 2, DetailsY1 + 59, _buttonConstructionSkillWidth, _buttonConstructionSkillHeight);
        PutText(FontBible, "Construction", TrainButtonTextX, DetailsY1 + 53);
        mouseOnButton = _mouseButtonX >= MouseOnTrainNumberButtonX1 && _mouseButtonX <= MouseOnTrainNumberButtonX2 &&
                        _mouseButtonY >= MouseOnTrainNumberButtonY1 && _mouseButtonY <= MouseOnTrainNumberButtonY2;
        if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
            DrawNumberPanelDown(TrainButtonNumberX, TrainButtonNumberY);
        else
            DrawNumberPanelUp(TrainButtonNumberX, TrainButtonNumberY);
        PutText(FontBible, trainSkillCounts[Skill.SKILL_CONSTRUCTION].ToString(), TrainButtonNumberX + 14, DetailsY1 + 53);

        mouseOnButton = _mouseButtonX >= MouseOnTrainButtonX1 && _mouseButtonX <= MouseOnTrainButtonX2 &&
                        _mouseButtonY >= MouseOnTrainButtonY1 + 63 && _mouseButtonY <= MouseOnTrainButtonY2 + 63;
        if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
            DrawSkillPanelDown(TrainSkillPanelX, TrainSkillPanelY + 63);
        else
            DrawSkillPanelUp(TrainSkillPanelX, TrainSkillPanelY + 63);
        Graphics.DrawBitmap(_buttonLeadershipSkillTexture, DetailsX1 + 2, DetailsY1 + 59 + 63, _buttonLeadershipSkillWidth, _buttonLeadershipSkillHeight);
        PutText(FontBible, "Leadership", TrainButtonTextX, DetailsY1 + 53 + 63);
        mouseOnButton = _mouseButtonX >= MouseOnTrainNumberButtonX1 && _mouseButtonX <= MouseOnTrainNumberButtonX2 &&
                        _mouseButtonY >= MouseOnTrainNumberButtonY1 + 63 && _mouseButtonY <= MouseOnTrainNumberButtonY2 + 63;
        if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
            DrawNumberPanelDown(TrainButtonNumberX, TrainButtonNumberY + 63);
        else
            DrawNumberPanelUp(TrainButtonNumberX, TrainButtonNumberY + 63);
        PutText(FontBible, trainSkillCounts[Skill.SKILL_LEADING].ToString(), TrainButtonNumberX + 14, DetailsY1 + 53 + 63);
        
        mouseOnButton = _mouseButtonX >= MouseOnTrainButtonX1 && _mouseButtonX <= MouseOnTrainButtonX2 &&
                        _mouseButtonY >= MouseOnTrainButtonY1 + 63 * 2 && _mouseButtonY <= MouseOnTrainButtonY2 + 63 * 2;
        if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
            DrawSkillPanelDown(TrainSkillPanelX, TrainSkillPanelY + 63 * 2);
        else
            DrawSkillPanelUp(TrainSkillPanelX, TrainSkillPanelY + 63 * 2);
        Graphics.DrawBitmap(_buttonMineSkillTexture, DetailsX1 + 2, DetailsY1 + 59 + 63 * 2, _buttonMineSkillWidth, _buttonMineSkillHeight);
        PutText(FontBible, "Mining", TrainButtonTextX, DetailsY1 + 53 + 63 * 2);
        mouseOnButton = _mouseButtonX >= MouseOnTrainNumberButtonX1 && _mouseButtonX <= MouseOnTrainNumberButtonX2 &&
                        _mouseButtonY >= MouseOnTrainNumberButtonY1 + 63 * 2 && _mouseButtonY <= MouseOnTrainNumberButtonY2 + 63 * 2;
        if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
            DrawNumberPanelDown(TrainButtonNumberX, TrainButtonNumberY + 63 * 2);
        else
            DrawNumberPanelUp(TrainButtonNumberX, TrainButtonNumberY + 63 * 2);
        PutText(FontBible, trainSkillCounts[Skill.SKILL_MINING].ToString(), TrainButtonNumberX + 14, DetailsY1 + 53 + 63 * 2);
        
        mouseOnButton = _mouseButtonX >= MouseOnTrainButtonX1 && _mouseButtonX <= MouseOnTrainButtonX2 &&
                        _mouseButtonY >= MouseOnTrainButtonY1 + 63 * 3 && _mouseButtonY <= MouseOnTrainButtonY2 + 63 * 3;
        if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
            DrawSkillPanelDown(TrainSkillPanelX, TrainSkillPanelY + 63 * 3);
        else
            DrawSkillPanelUp(TrainSkillPanelX, TrainSkillPanelY + 63 * 3);
        Graphics.DrawBitmap(_buttonManufactureSkillTexture, DetailsX1 + 2, DetailsY1 + 59 + 63 * 3, _buttonManufactureSkillWidth, _buttonManufactureSkillHeight);
        PutText(FontBible, "Manufacturing", TrainButtonTextX, DetailsY1 + 53 + 63 * 3);
        mouseOnButton = _mouseButtonX >= MouseOnTrainNumberButtonX1 && _mouseButtonX <= MouseOnTrainNumberButtonX2 &&
                        _mouseButtonY >= MouseOnTrainNumberButtonY1 + 63 * 3 && _mouseButtonY <= MouseOnTrainNumberButtonY2 + 63 * 3;
        if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
            DrawNumberPanelDown(TrainButtonNumberX, TrainButtonNumberY + 63 * 3);
        else
            DrawNumberPanelUp(TrainButtonNumberX, TrainButtonNumberY + 63 * 3);
        PutText(FontBible, trainSkillCounts[Skill.SKILL_MFT].ToString(), TrainButtonNumberX + 14, DetailsY1 + 53 + 63 * 3);
        
        mouseOnButton = _mouseButtonX >= MouseOnTrainButtonX1 && _mouseButtonX <= MouseOnTrainButtonX2 &&
                        _mouseButtonY >= MouseOnTrainButtonY1 + 63 * 4 && _mouseButtonY <= MouseOnTrainButtonY2 + 63 * 4;
        if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
            DrawSkillPanelDown(TrainSkillPanelX, TrainSkillPanelY + 63 * 4);
        else
            DrawSkillPanelUp(TrainSkillPanelX, TrainSkillPanelY + 63 * 4);
        Graphics.DrawBitmap(_buttonResearchSkillTexture, DetailsX1 + 2, DetailsY1 + 59 + 63 * 4, _buttonResearchSkillWidth, _buttonResearchSkillHeight);
        PutText(FontBible, "Research", TrainButtonTextX, DetailsY1 + 53 + 63 * 4);
        mouseOnButton = _mouseButtonX >= MouseOnTrainNumberButtonX1 && _mouseButtonX <= MouseOnTrainNumberButtonX2 &&
                        _mouseButtonY >= MouseOnTrainNumberButtonY1 + 63 * 4 && _mouseButtonY <= MouseOnTrainNumberButtonY2 + 63 * 4;
        if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
            DrawNumberPanelDown(TrainButtonNumberX, TrainButtonNumberY + 63 * 4);
        else
            DrawNumberPanelUp(TrainButtonNumberX, TrainButtonNumberY + 63 * 4);
        PutText(FontBible, trainSkillCounts[Skill.SKILL_RESEARCH].ToString(), TrainButtonNumberX + 14, DetailsY1 + 53 + 63 * 4);
        
        mouseOnButton = _mouseButtonX >= MouseOnTrainButtonX1 && _mouseButtonX <= MouseOnTrainButtonX2 &&
                        _mouseButtonY >= MouseOnTrainButtonY1 + 63 * 5 && _mouseButtonY <= MouseOnTrainButtonY2 + 63 * 5;
        if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
            DrawSkillPanelDown(TrainSkillPanelX, TrainSkillPanelY + 63 * 5);
        else
            DrawSkillPanelUp(TrainSkillPanelX, TrainSkillPanelY + 63 * 5);
        Graphics.DrawBitmap(_buttonSpySkillTexture, DetailsX1 + 2, DetailsY1 + 59 + 63 * 5, _buttonSpySkillWidth, _buttonSpySkillHeight);
        PutText(FontBible, "Spying", TrainButtonTextX, DetailsY1 + 53 + 63 * 5);
        mouseOnButton = _mouseButtonX >= MouseOnTrainNumberButtonX1 && _mouseButtonX <= MouseOnTrainNumberButtonX2 &&
                        _mouseButtonY >= MouseOnTrainNumberButtonY1 + 63 * 5 && _mouseButtonY <= MouseOnTrainNumberButtonY2 + 63 * 5;
        if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
            DrawNumberPanelDown(TrainButtonNumberX, TrainButtonNumberY + 63 * 5);
        else
            DrawNumberPanelUp(TrainButtonNumberX, TrainButtonNumberY + 63 * 5);
        PutText(FontBible, trainSkillCounts[Skill.SKILL_SPYING].ToString(), TrainButtonNumberX + 14, DetailsY1 + 53 + 63 * 5);
        
        mouseOnButton = _mouseButtonX >= MouseOnTrainButtonX1 && _mouseButtonX <= MouseOnTrainButtonX2 + 82 &&
                        _mouseButtonY >= MouseOnTrainButtonY1 + 63 * 6 && _mouseButtonY <= MouseOnTrainButtonY2 + 63 * 6;
        if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
            DrawSkillPanelDown(TrainSkillPanelX, TrainSkillPanelY + 63 * 6);
        else
            DrawSkillPanelUp(TrainSkillPanelX, TrainSkillPanelY + 63 * 6);
        PutTextCenter(FontBible, "Done", DetailsX1 + 2, DetailsY1 + 53 + 63 * 6, DetailsX2 - 4, DetailsY1 + 53 + 63 * 6 + 40);
    }

    private void HandleTrainMenuInput(Town town)
    {
        int selectedTrainSkill = 0;

        if (_leftMouseReleased || _rightMouseReleased)
        {
            if (_mouseButtonX >= MouseOnTrainButtonX1 && _mouseButtonX <= MouseOnTrainButtonX2 &&
                _mouseButtonY >= MouseOnTrainButtonY1 && _mouseButtonY <= MouseOnTrainButtonY2)
            {
                selectedTrainSkill = Skill.SKILL_CONSTRUCTION;
                TownDetailsMode = TownDetailsMode.Normal;
            }

            if (_mouseButtonX >= MouseOnTrainButtonX1 && _mouseButtonX <= MouseOnTrainButtonX2 &&
                _mouseButtonY >= MouseOnTrainButtonY1 + 63 && _mouseButtonY <= MouseOnTrainButtonY2 + 63)
            {
                selectedTrainSkill = Skill.SKILL_LEADING;
                TownDetailsMode = TownDetailsMode.Normal;
            }

            if (_mouseButtonX >= MouseOnTrainButtonX1 && _mouseButtonX <= MouseOnTrainButtonX2 &&
                _mouseButtonY >= MouseOnTrainButtonY1 + 63 * 2 && _mouseButtonY <= MouseOnTrainButtonY2 + 63 * 2)
            {
                selectedTrainSkill = Skill.SKILL_MINING;
                TownDetailsMode = TownDetailsMode.Normal;
            }

            if (_mouseButtonX >= MouseOnTrainButtonX1 && _mouseButtonX <= MouseOnTrainButtonX2 &&
                _mouseButtonY >= MouseOnTrainButtonY1 + 63 * 3 && _mouseButtonY <= MouseOnTrainButtonY2 + 63 * 3)
            {
                selectedTrainSkill = Skill.SKILL_MFT;
                TownDetailsMode = TownDetailsMode.Normal;
            }

            if (_mouseButtonX >= MouseOnTrainButtonX1 && _mouseButtonX <= MouseOnTrainButtonX2 &&
                _mouseButtonY >= MouseOnTrainButtonY1 + 63 * 4 && _mouseButtonY <= MouseOnTrainButtonY2 + 63 * 4)
            {
                selectedTrainSkill = Skill.SKILL_RESEARCH;
                TownDetailsMode = TownDetailsMode.Normal;
            }

            if (_mouseButtonX >= MouseOnTrainButtonX1 && _mouseButtonX <= MouseOnTrainButtonX2 &&
                _mouseButtonY >= MouseOnTrainButtonY1 + 63 * 5 && _mouseButtonY <= MouseOnTrainButtonY2 + 63 * 5)
            {
                selectedTrainSkill = Skill.SKILL_SPYING;
                TownDetailsMode = TownDetailsMode.Normal;
            }

            if (_mouseButtonX >= MouseOnTrainButtonX1 && _mouseButtonX <= MouseOnTrainButtonX2 + 82 &&
                _mouseButtonY >= MouseOnTrainButtonY1 + 63 * 6 && _mouseButtonY <= MouseOnTrainButtonY2 + 63 * 6)
            {
                TownDetailsMode = TownDetailsMode.Normal;
            }

            if (_mouseButtonX >= MouseOnTrainNumberButtonX1 && _mouseButtonX <= MouseOnTrainNumberButtonX2 &&
                _mouseButtonY >= MouseOnTrainNumberButtonY1 && _mouseButtonY <= MouseOnTrainNumberButtonY2)
            {
                selectedTrainSkill = Skill.SKILL_CONSTRUCTION;
            }

            if (_mouseButtonX >= MouseOnTrainNumberButtonX1 && _mouseButtonX <= MouseOnTrainNumberButtonX2 &&
                _mouseButtonY >= MouseOnTrainNumberButtonY1 + 63 && _mouseButtonY <= MouseOnTrainNumberButtonY2 + 63)
            {
                selectedTrainSkill = Skill.SKILL_LEADING;
            }

            if (_mouseButtonX >= MouseOnTrainNumberButtonX1 && _mouseButtonX <= MouseOnTrainNumberButtonX2 &&
                _mouseButtonY >= MouseOnTrainNumberButtonY1 + 63 * 2 && _mouseButtonY <= MouseOnTrainNumberButtonY2 + 63 * 2)
            {
                selectedTrainSkill = Skill.SKILL_MINING;
            }

            if (_mouseButtonX >= MouseOnTrainNumberButtonX1 && _mouseButtonX <= MouseOnTrainNumberButtonX2 &&
                _mouseButtonY >= MouseOnTrainNumberButtonY1 + 63 * 3 && _mouseButtonY <= MouseOnTrainNumberButtonY2 + 63 * 3)
            {
                selectedTrainSkill = Skill.SKILL_MFT;
            }

            if (_mouseButtonX >= MouseOnTrainNumberButtonX1 && _mouseButtonX <= MouseOnTrainNumberButtonX2 &&
                _mouseButtonY >= MouseOnTrainNumberButtonY1 + 63 * 4 && _mouseButtonY <= MouseOnTrainNumberButtonY2 + 63 * 4)
            {
                selectedTrainSkill = Skill.SKILL_RESEARCH;
            }

            if (_mouseButtonX >= MouseOnTrainNumberButtonX1 && _mouseButtonX <= MouseOnTrainNumberButtonX2 &&
                _mouseButtonY >= MouseOnTrainNumberButtonY1 + 63 * 5 && _mouseButtonY <= MouseOnTrainNumberButtonY2 + 63 * 5)
            {
                selectedTrainSkill = Skill.SKILL_SPYING;
            }
        }

        if (selectedTrainSkill != 0)
        {
            // if (remote.is_enable())
            //{
                //// packet structure : <town recno> <skill id> <race id> <amount>
                //short *shortPtr = (short *)remote.new_send_queue_msg(MSG_TOWN_RECRUIT, 4*sizeof(short) );
                //shortPtr[0] = town_recno;
                //shortPtr[1] = b;
                // if (_leftMouseReleased)
                    //shortPtr[2] = race_filter(browse_race.recno());
                // if (_rightMouseReleased)
                    //shortPtr[2] = -1;
                //shortPtr[3] = (short)trainCancelAmount;
            //}
            //else
            
            if (_leftMouseReleased)
                town.AddQueue(selectedTrainSkill, _selectedRaceId);
            if (_rightMouseReleased)
                town.RemoveQueue(selectedTrainSkill);
            
            if (_leftMouseReleased)
                SECtrl.immediate_sound("TURN_ON");
            if (_rightMouseReleased)
                SECtrl.immediate_sound("TURN_OFF");
        }
    }
}