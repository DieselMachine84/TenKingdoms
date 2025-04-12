using System.Linq;

namespace TenKingdoms;

public partial class Renderer
{
    // TODO train menu, show spies list, auto collect tax, auto grant
    // TODO Controlled by Rebels panel
    // TODO train unit progress
    // TODO display auto collect and auto grant values
    // TODO go to town location when pressing color square
    // TODO list box scroll
    
    private void DrawTownDetails(Town town)
    {
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
                loyalty = (int)town.RacesResistance[i, NationArray.player_recno];
                targetLoyalty = town.RacesTargetResistance[i, NationArray.player_recno];
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
            PutText(FontMid, town.AverageResistance(NationArray.player_recno).ToString(), avgLoyaltyX, totalTextY - 1);

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
            bool displayGrantButton = town.HasLinkedCamp(NationArray.player_recno, false);
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
        bool race1Selected = _mouseButtonX >= DetailsX1 + 11 && _mouseButtonX <= DetailsX1 + 402 &&
                             _mouseButtonY >= DetailsY1 + 105 && _mouseButtonY <= DetailsY1 + 153;
        bool race2Selected = _mouseButtonX >= DetailsX1 + 11 && _mouseButtonX <= DetailsX1 + 402 &&
                             _mouseButtonY >= DetailsY1 + 105 + RaceHeight && _mouseButtonY <= DetailsY1 + 153 + RaceHeight;
        bool race3Selected = _mouseButtonX >= DetailsX1 + 11 && _mouseButtonX <= DetailsX1 + 402 &&
                             _mouseButtonY >= DetailsY1 + 105 + 2 * RaceHeight && _mouseButtonY <= DetailsY1 + 153 + 2 * RaceHeight;
        bool race4Selected = _mouseButtonX >= DetailsX1 + 11 && _mouseButtonX <= DetailsX1 + 402 &&
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

        bool button1Pressed = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                              _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
        bool button2Pressed = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                              _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
        bool button3Pressed = _mouseButtonX >= Button3X + 2 && _mouseButtonX <= Button3X + ButtonWidth &&
                              _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
        bool button4Pressed = _mouseButtonX >= Button4X + 2 && _mouseButtonX <= Button4X + ButtonWidth &&
                              _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;
        bool button5Pressed = _mouseButtonX >= Button5X + 2 && _mouseButtonX <= Button5X + ButtonWidth &&
                              _mouseButtonY >= ButtonsTownY + 2 && _mouseButtonY <= ButtonsTownY + ButtonHeight;

        if (NationArray.player_recno != 0 && town.NationId == NationArray.player_recno && _selectedRaceId != 0)
        {
            if (button1Pressed && town.CanRecruit(_selectedRaceId))
            {
                town.Recruit(-1, _selectedRaceId, InternalConstants.COMMAND_PLAYER);
            }

            if (button2Pressed && IsTrainEnabled(town))
            {
                // TODO train
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
            
            bool displayGrantButton = town.HasLinkedCamp(NationArray.player_recno, false);
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
}