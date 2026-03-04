using System;

namespace TenKingdoms;

public partial class Renderer
{
    private void SetSelectedKingdom(int newValue)
    {
        _selectedKingdomId = newValue;
        ResetCurTalkMsg();
    }

    private void ResetCurTalkMsg()
    {
        _curTalkMsg.TalkId = 0;
        _curTalkMsg.FromNationId = 0;
        _curTalkMsg.ToNationId = 0;
        _curTalkMsg.TalkParam1 = 0;
        _curTalkMsg.TalkParam2 = 0;
    }

    private void DrawKingdomsView()
    {
        Graphics.DrawBitmap(_kingdomsHeaderTexture, MainViewX + 6, MainViewY + 6, _kingdomsHeaderWidth, _kingdomsHeaderHeight);
        PutText(FontSan, "Kingdoms", MainViewX + 42, MainViewY + 12);
        PutText(FontSan, "Reputation", MainViewX + 359, MainViewY + 12);
        PutText(FontSan, "Status", MainViewX + 530, MainViewY + 12);
        PutText(FontSan, "Allow Attack", MainViewX + 660, MainViewY + 12);
        PutText(FontSan, "Trade Treaty", MainViewX + 870, MainViewY + 12);
        PutText(FontSan, "Trade Amount", MainViewX + 1080, MainViewY + 12);
        
        Graphics.DrawBitmap(_kingdomsTexture, MainViewX + 6, MainViewY + 50, _kingdomsWidth, _kingdomsHeight);

        if (_selectedKingdomId != 0 && NationArray.IsDeleted(_selectedKingdomId))
            SetSelectedKingdom(0);
        
        //TODO view secret report
        int viewingNationId = NationArray.PlayerId;
        Nation viewingNation = NationArray[viewingNationId];
        int dy = 0;
        foreach (Nation nation in NationArray)
        {
            if (viewingNation == null || viewingNation == nation || viewingNation.GetRelation(nation.NationId).HasContact)
            {
                DrawNationColor(ColorRemap.ColorSchemes[nation.NationId], MainViewX + 12, MainViewY + 57 + dy);
                PutText(FontSan, nation.NationName(), MainViewX + 42, MainViewY + 54 + dy);
                PutText(FontSan, ((int)nation.Reputation).ToString(), MainViewX + 420, MainViewY + 54 + dy);
                if (viewingNation != null && viewingNation != nation)
                {
                    NationRelation relation = viewingNation.GetRelation(nation.NationId);
                    PutText(FontSan, relation.StatusString(), MainViewX + 530, MainViewY + 54 + dy);
                    PutText(FontSan, relation.ShouldAttack ? "Yes" : "No", MainViewX + 719, MainViewY + 54 + dy);
                    PutText(FontSan, relation.TradeTreaty ? "Yes" : "No", MainViewX + 939, MainViewY + 54 + dy);
                    PutText(FontSan, "$" + (int)viewingNation.TotalYearTrade(nation.NationId), MainViewX + 1100, MainViewY + 54 + dy);
                }

                if (_selectedKingdomId == 0)
                    SetSelectedKingdom(nation.NationId);
                
                if (_selectedKingdomId == nation.NationId)
                    DrawSelectedBorder(MainViewX + 8, MainViewY + 49 + dy, MainViewX + MainViewWidth - 8, MainViewY + 89 + dy);
            }

            dy += 38;
        }
        
        Graphics.DrawBitmap(_kingdomsButtonUpTexture, MainViewX + 6, MainViewY + 434, _kingdomsButtonUpWidth, _kingdomsButtonUpHeight);
        PutTextCenter(FontSan, "Information", MainViewX + 6, MainViewY + 452, MainViewX + 306, MainViewY + 452);
        Graphics.DrawBitmap(_kingdomsButtonDownTexture, MainViewX + 308, MainViewY + 434, _kingdomsButtonDownWidth, _kingdomsButtonDownHeight);
        PutTextCenter(FontSan, "Diplomacy", MainViewX + 308, MainViewY + 452, MainViewX + 608, MainViewY + 452);
        Graphics.DrawBitmap(_kingdomsButtonUpTexture, MainViewX + 610, MainViewY + 434, _kingdomsButtonUpWidth, _kingdomsButtonUpHeight);
        PutTextCenter(FontSan, "Diplomatic Log", MainViewX + 610, MainViewY + 452, MainViewX + 910, MainViewY + 452);
        
        Graphics.DrawBitmap(_talkTexture, MainViewX + 6, MainViewY + 478, _talkWidth, _talkHeight);
        if (NationArray.PlayerId != 0 && viewingNationId == NationArray.PlayerId && _selectedKingdomId != 0 && _selectedKingdomId != NationArray.PlayerId)
        {
            if (_replyTalkMsgId == 0 && _curTalkMsg.TalkId == 0)
            {
                if (TalkMsgArray.CanSendAnyMsg(_selectedKingdomId, NationArray.PlayerId))
                {
                    _curTalkMsg.FromNationId = NationArray.PlayerId;
                    _curTalkMsg.ToNationId = _selectedKingdomId;
                    AddMainChoices();
                }
                else
                {
                    PutText(FontSan, "You've sent too many messages to this kingdom.", MainViewX + 24, MainViewY + 486);
                    PutText(FontSan, "You cannot send any new messages until the existing ones are processed.", MainViewX + 24, MainViewY + 486 + 38);
                    return;
                }
            }

            dy = 0;
            if (!String.IsNullOrEmpty(_choiceQuestion))
            {
                PutText(FontSan, _choiceQuestion, MainViewX + 24, MainViewY + 486);
                dy += 38;
            }

            if (!String.IsNullOrEmpty(_choiceQuestionSecondLine))
            {
                PutText(FontSan, _choiceQuestionSecondLine, MainViewX + 24, MainViewY + 486 + dy);
                dy += 38;
            }

            bool secondColumn = false;
            for (int i = 0; i < _talkChoiceIndex; i++)
            {
                TalkChoice talkChoice = _talkChoices[i];

                if (!secondColumn && (_replyTalkMsgId == 0) && _curTalkMsg.TalkId == 0 && talkChoice.Param >= TalkMsg.TALK_DECLARE_WAR)
                {
                    secondColumn = true;
                    dy = 0;
                }

                int x = MainViewX + 24;
                if (_curTalkMsg.TalkId == 0 && secondColumn)
                    x = MainViewX + 610;

                int y = MainViewY + 486 + dy;
                
                if (_mouseMotionX >= x && _mouseMotionX <= x + 400 && _mouseMotionY >= y && _mouseMotionY <= y + 36)
                    Graphics.DrawRect(x - 4, y, secondColumn ? 600 : 400, 36, Colors.NEWS_COLOR);
                
                PutText(FontSan, (!String.IsNullOrEmpty(_choiceQuestion) ? "- " : String.Empty) + talkChoice.Choice, x, y);

                dy += 38;
            }
        }
    }

    private void HandleKingdomsView()
    {
        int dy = 0;
        int pressedItem = 0;
        for (int i = 1; i <= GameConstants.MAX_RACE; i++)
        {
            if (_leftMouseReleased)
            {
                if (_mouseButtonX >= MainViewX + 11 && _mouseButtonX <= MainViewX + MainViewWidth - 11 &&
                    _mouseButtonY >= MainViewY + 52 + dy && _mouseButtonY <= MainViewY + 86 + dy)
                {
                    pressedItem = i;
                }
            }

            dy += 38;
        }

        int kingdomIndex = 0;
        foreach (Nation nation in NationArray)
        {
            kingdomIndex++;
            if (kingdomIndex == pressedItem)
            {
                SetSelectedKingdom(nation.NationId);
                break;
            }
        }

        dy = 0;
        if (!String.IsNullOrEmpty(_choiceQuestion))
            dy += 38;
        if (!String.IsNullOrEmpty(_choiceQuestionSecondLine))
            dy += 38;

        bool secondColumn = false;
        for (int i = 0; i < _talkChoiceIndex; i++)
        {
            TalkChoice talkChoice = _talkChoices[i];

            if (!secondColumn && (_replyTalkMsgId == 0) && _curTalkMsg.TalkId == 0 && talkChoice.Param >= TalkMsg.TALK_DECLARE_WAR)
            {
                secondColumn = true;
                dy = 0;
            }

            int x = MainViewX + 24;
            if (_curTalkMsg.TalkId == 0 && secondColumn)
                x = MainViewX + 610;

            int y = MainViewY + 486 + dy;

            bool mouseOnChoice = _mouseMotionX >= x && _mouseMotionX <= x + 400 && _mouseMotionY >= y && _mouseMotionY <= y + 36;
            if (_leftMouseReleased && mouseOnChoice)
            {
                if (_replyTalkMsgId != 0)
                {
                    if (!TalkMsgArray.IsTalkMsgDeleted(_replyTalkMsgId))
                    {
                        if (talkChoice.Param == 1)
                            TalkMsgArray.ReplyTalkMsg(_replyTalkMsgId, TalkMsgArray.REPLY_ACCEPT, InternalConstants.COMMAND_PLAYER);
                        else
                            TalkMsgArray.ReplyTalkMsg(_replyTalkMsgId, TalkMsgArray.REPLY_REJECT, InternalConstants.COMMAND_PLAYER);
                    }

                    _viewMode = _prevViewMode;
                    _replyTalkMsgId = 0;
                    ResetCurTalkMsg();
                    return;
                }

                if (talkChoice.Param == -1)
                {
                    ResetCurTalkMsg();
                    return;
                }

                if (_curTalkMsg.TalkId == 0)
                    _curTalkMsg.TalkId = talkChoice.Param;
                else if (_curTalkMsg.TalkParam1 == 0)
                    _curTalkMsg.TalkParam1 = talkChoice.Param;
                else if (_curTalkMsg.TalkParam2 == 0)
                    _curTalkMsg.TalkParam2 = talkChoice.Param;

                if (!SetTalkChoices())
                {
                    TalkMsgArray.SendTalkMsg(new TalkMsg(_curTalkMsg), InternalConstants.COMMAND_PLAYER);
                    _choiceQuestion = "The message has been sent.";
                    _talkChoiceIndex = 0;
                    AddTalkChoice("Continue.", -1);
                }
            }

            dy += 38;
        }
    }
    
    private void HandlePlayerReply(int talkMsgId)
    {
        TalkMsg talkMsg = TalkMsgArray.GetTalkMsg(talkMsgId);

        if (NationArray.IsDeleted(talkMsg.FromNationId))
            return;

        ResetCurTalkMsg();
        _curTalkMsg.FromNationId = NationArray.PlayerId;
        _curTalkMsg.ToNationId = talkMsg.FromNationId;

        //--------- add talk choices ---------//

        string message = talkMsg.Message(NationArray.PlayerId);
        _choiceQuestion = message;

        //---- see if this message has a second line -----//

        string message2 = talkMsg.Message(NationArray.PlayerId, false, true);
        _choiceQuestionSecondLine = message != message2 ? message2 : String.Empty;

        _talkChoiceIndex = 0;
        if (talkMsg.CanAccept())
            AddTalkChoice("Accept.", 1);

        AddTalkChoice("Reject.", 0);

        _prevViewMode = _viewMode;
        _viewMode = ViewMode.Kingdoms;
        _selectedKingdomId = talkMsg.FromNationId;
        _replyTalkMsgId = talkMsgId;
    }
    
    private readonly string[] _mainTalkChoices =
    {
        "Propose a trade treaty.",
        "Propose a friendly treaty.",
        "Propose an alliance treaty.",
        "Terminate our trade treaty.",
        "Terminate our friendly treaty.",
        "Terminate our alliance treaty.",
        "Request immediate military aid.",
        "Request a trade embargo.",
        "Request a cease-fire.",
        "Request a declaration of war against a foe.",
        "Request to purchase food.",
        "Declare war.",
        "Offer to pay tribute.",
        "Demand tribute.",
        "Offer aid.",
        "Request aid.",
        "Offer to transfer technology.",
        "Request technology.",
        "Offer to purchase throne and unite kingdoms.",
        "Surrender."
    };
    
    private bool SetTalkChoices()
    {
        _talkChoiceIndex = 0;
        _choiceQuestion = String.Empty;
        _choiceQuestionSecondLine = String.Empty;

        bool rc = false;

        switch (_curTalkMsg.TalkId)
        {
            case 0:
                AddMainChoices();
                return true;

            case TalkMsg.TALK_PROPOSE_TRADE_TREATY:
            case TalkMsg.TALK_PROPOSE_FRIENDLY_TREATY:
            case TalkMsg.TALK_PROPOSE_ALLIANCE_TREATY:
            case TalkMsg.TALK_END_TRADE_TREATY:
            case TalkMsg.TALK_END_FRIENDLY_TREATY:
            case TalkMsg.TALK_END_ALLIANCE_TREATY:
            case TalkMsg.TALK_REQUEST_CEASE_WAR:
            case TalkMsg.TALK_DECLARE_WAR:
            case TalkMsg.TALK_REQUEST_MILITARY_AID:
                return false;

            case TalkMsg.TALK_REQUEST_TRADE_EMBARGO:
                rc = AddTradeEmbargoChoices();
                break;

            case TalkMsg.TALK_REQUEST_DECLARE_WAR:
                rc = AddDeclareWarChoices();
                break;

            case TalkMsg.TALK_REQUEST_BUY_FOOD:
                rc = AddBuyFoodChoices();
                break;

            case TalkMsg.TALK_GIVE_TRIBUTE:
            case TalkMsg.TALK_DEMAND_TRIBUTE:
                rc = AddTributeChoices();
                // add the choice question here because we use the same function for both tribute and aid
                if (rc)
                    _choiceQuestion = "How much tribute?";
                break;

            case TalkMsg.TALK_GIVE_AID:
            case TalkMsg.TALK_DEMAND_AID:
                rc = AddTributeChoices();
                if (rc)
                    _choiceQuestion = "How much aid?";
                break;

            case TalkMsg.TALK_GIVE_TECH:
            case TalkMsg.TALK_DEMAND_TECH:
                rc = AddGiveTechChoices();
                break;

            case TalkMsg.TALK_REQUEST_SURRENDER:
                rc = AddRequestSurrenderChoices();
                break;

            case TalkMsg.TALK_SURRENDER:
                rc = AddSurrenderChoices();
                break;
        }

        if (rc)
            AddTalkChoice("Cancel.", -1);

        return rc;
    }
    
    private void AddTalkChoice(string talkChoice, int talkParam)
    {
        _talkChoices[_talkChoiceIndex].Choice = talkChoice;
        _talkChoices[_talkChoiceIndex].Param = talkParam;
        _talkChoiceIndex++;
    }

    private void AddMainChoices()
    {
        _talkChoiceIndex = 0;
        _choiceQuestion = String.Empty;
        _choiceQuestionSecondLine = String.Empty;
        
        for (int i = 1; i <= TalkMsg.MAX_TALK_TYPE; i++)
        {
            if (!TalkMsgArray.CanSendMsg(_curTalkMsg.ToNationId, NationArray.PlayerId, i))
                continue;

            AddTalkChoice(_mainTalkChoices[i - 1], i);
        }
    }

    private bool AddTradeEmbargoChoices()
    {
        if (_curTalkMsg.TalkParam1 != 0)
            return false;

        _choiceQuestion = "Request an embargo on trade with which kingdom?";

        Nation fromNation = NationArray[_curTalkMsg.FromNationId];
        Nation toNation = NationArray[_curTalkMsg.ToNationId];

        foreach (Nation nation in NationArray)
        {
            int nationId = nation.NationId;

            if (nationId == _curTalkMsg.FromNationId || nationId == _curTalkMsg.ToNationId)
            {
                continue;
            }

            if (!fromNation.GetRelation(nationId).TradeTreaty && toNation.GetRelation(nationId).TradeTreaty)
            {
                string withNationString = "@COL" + Convert.ToChar(48 + nation.ColorSchemeId) + " " + nation.NationName();
                AddTalkChoice(withNationString, nationId);
            }
        }

        return true;
    }

    private bool AddDeclareWarChoices()
    {
        if (_curTalkMsg.TalkParam1 != 0)
            return false;

        _choiceQuestion = "Declare war on which kingdom?";

        Nation fromNation = NationArray[_curTalkMsg.FromNationId];
        Nation toNation = NationArray[_curTalkMsg.ToNationId];

        foreach (Nation nation in NationArray)
        {
            int nationId = nation.NationId;

            //--- can only ask another nation to declare war with a nation that is currently at war with our nation ---//

            if (fromNation.GetRelationStatus(nationId) == NationBase.NATION_HOSTILE && toNation.GetRelationStatus(nationId) != NationBase.NATION_HOSTILE)
            {
                string withNationString = "@COL" + Convert.ToChar(48 + nation.ColorSchemeId) + " " + nation.NationName();
                AddTalkChoice(withNationString, nationId);
            }
        }

        return true;
    }

    private bool AddBuyFoodChoices()
    {
        if (_curTalkMsg.TalkParam1 == 0)
        {
            _choiceQuestion = "How much food do you want to purchase?";

            string[] qtyStrArray = { "500.", "1000.", "2000.", "4000." };
            int[] qtyArray = { 500, 1000, 2000, 4000 };

            for (int i = 0; i < qtyStrArray.Length; i++)
            {
                if (NationArray.Player != null && NationArray.Player.Cash >= qtyArray[i] * GameConstants.MIN_FOOD_PURCHASE_PRICE / 10.0)
                    AddTalkChoice(qtyStrArray[i], qtyArray[i]);
            }

            return true;
        }
        
        if (_curTalkMsg.TalkParam2 == 0)
        {
            _choiceQuestion = "How much do you offer for 10 units of food?";

            string[] priceStrArray = { "$5.", "$10.", "$15.", "$20." };
            int[] priceArray = { 5, 10, 15, 20 };

            for (int i = 0; i < priceStrArray.Length; i++)
            {
                if (i == 0 || (NationArray.Player != null && NationArray.Player.Cash >= _curTalkMsg.TalkParam1 * priceArray[i] / 10.0))
                    AddTalkChoice(priceStrArray[i], priceArray[i]);
            }

            return true;
        }
        
        return false;
    }

    private bool AddTributeChoices()
    {
        if (_curTalkMsg.TalkParam1 != 0)
            return false;

        string[] tributeStrArray = { "$500.", "$1000.", "$2000.", "$3000.", "$4000." };
        int[] tributeAmtArray = { 500, 1000, 2000, 3000, 4000 };

        for (int i = 0; i < tributeStrArray.Length; i++)
        {
            // when demand tribute, the amount can be sent to any
            if (_curTalkMsg.TalkId == TalkMsg.TALK_DEMAND_TRIBUTE ||
                _curTalkMsg.TalkId == TalkMsg.TALK_DEMAND_AID ||
                (NationArray.Player != null && NationArray.Player.Cash >= tributeAmtArray[i]))
            {
                AddTalkChoice(tributeStrArray[i], tributeAmtArray[i]);
            }
        }

        return true;
    }

    private bool AddGiveTechChoices()
    {
        int techNationId = _curTalkMsg.TalkId == TalkMsg.TALK_GIVE_TECH ? _curTalkMsg.FromNationId : _curTalkMsg.ToNationId;

        Nation techNation = NationArray[techNationId];

        if (_curTalkMsg.TalkParam1 == 0)
        {
            _choiceQuestion = "Which technology?";

            for (int techId = 1; techId <= TechRes.TechInfos.Length; techId++)
            {
                if (techNation.GetTechLevel(techId) > 0)
                {
                    AddTalkChoice(TechRes[techId].Description(), techId);
                }
            }

            return true;
        }
        
        if (_curTalkMsg.TalkParam2 == 0 && _curTalkMsg.TalkId == TalkMsg.TALK_GIVE_TECH)
        {
            TechInfo techInfo = TechRes[_curTalkMsg.TalkParam1];

            if (techInfo.MaxTechLevel == 1) // this tech only has one level
                return false;

            _choiceQuestion = "Which version?";

            int nationLevel = techNation.GetTechLevel(_curTalkMsg.TalkParam1);

            string[] verStrArray = { "Mark I", "Mark II", "Mark III" };

            for (int i = 1; i <= Math.Min(3, nationLevel); i++)
                AddTalkChoice(verStrArray[i - 1], i);

            return true;
        }
        
        return false;
    }

    private bool AddRequestSurrenderChoices()
    {
        if (_curTalkMsg.TalkParam1 != 0)
            return false;

        _choiceQuestion = "How much do you offer?";

        //TODO too many choices
        string[] strArray = { "$5000.", "$7500.", "$10000.", "$15000.", "$20000.", "$30000.", "$40000.", "$50000." };
        int[] amtArray = { 5000, 7500, 10000, 15000, 20000, 30000, 40000, 50000 };

        for (int i = 0; i < strArray.Length; i++)
        {
            if (NationArray.Player != null && NationArray.Player.Cash >= amtArray[i])
            {
                AddTalkChoice(strArray[i], amtArray[i] / 10); // divided by 10 to cope with the limit of <short>
            }
        }

        return true;
    }
    
    private bool AddSurrenderChoices()
    {
        if (_curTalkMsg.TalkParam1 != 0)
            return false;

        string kingName = NationArray[_curTalkMsg.ToNationId].KingName(true);
        _choiceQuestion = $"Do you really want to Surrender to {kingName}'s Kingdom?";

        AddTalkChoice("Confirm.", 1);

        return true;
    }
}