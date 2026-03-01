using System;
using System.Collections.Generic;
using System.IO;

namespace TenKingdoms;

public class TalkChoice
{
    public string Choice { get; set; }
    public int Param { get; set; }
}

public class TalkMsgArray
{
    private const int MAX_WAIT_MSG_PER_NATION = 3;

    public const int REPLY_NOT_NEEDED = 0;
    public const int REPLY_WAITING = 1;
    public const int REPLY_ACCEPT = 2;
    public const int REPLY_REJECT = 3;

    private int _nextTalkMsgId = 1;
    public List<TalkMsg> TalkMessages { get; } = new List<TalkMsg>();
    
    public bool AddNationColor { get; set; }

    private Info Info => Sys.Instance.Info;
    private NationArray NationArray => Sys.Instance.NationArray;
    private UnitArray UnitArray => Sys.Instance.UnitArray;
    private NewsArray NewsArray => Sys.Instance.NewsArray;


    public TalkMsgArray()
    {
    }

    public bool AISendTalkMsg(int toNationId, int fromNationId, int talkId, int talkParam1 = 0, int talkParam2 = 0, bool forceSend = false)
    {
        Nation fromNation = NationArray[fromNationId];

        if (!fromNation.IsAI())
            return false;

        //--- first check again if the nation should send the message now ---//

        if (!forceSend)
        {
            if (!fromNation.should_diplomacy_retry(talkId, toNationId))
                return false;
        }

        //-------- avoid send opposite message too soon ----//

        int oppTalkId = 0;

        switch (talkId)
        {
            case TalkMsg.TALK_PROPOSE_ALLIANCE_TREATY:
                oppTalkId = TalkMsg.TALK_END_FRIENDLY_TREATY;
                break;

            case TalkMsg.TALK_GIVE_TRIBUTE:
                oppTalkId = TalkMsg.TALK_DEMAND_TRIBUTE;
                break;

            case TalkMsg.TALK_DEMAND_TRIBUTE:
                oppTalkId = TalkMsg.TALK_GIVE_TRIBUTE;
                break;

            case TalkMsg.TALK_GIVE_AID:
                oppTalkId = TalkMsg.TALK_DEMAND_AID;
                break;

            case TalkMsg.TALK_DEMAND_AID:
                oppTalkId = TalkMsg.TALK_GIVE_AID;
                break;

            case TalkMsg.TALK_GIVE_TECH:
                oppTalkId = TalkMsg.TALK_DEMAND_TECH;
                break;

            case TalkMsg.TALK_DEMAND_TECH:
                oppTalkId = TalkMsg.TALK_GIVE_TECH;
                break;
        }

        if (oppTalkId != 0)
        {
            fromNation.GetRelation(toNationId).LastTalkRejectDates[oppTalkId - 1] = Info.GameDate;
        }

        //------------------------------------------//

        TalkMsg talkMsg = new TalkMsg();
        talkMsg.ToNationId = toNationId;
        talkMsg.FromNationId = fromNationId;
        talkMsg.TalkId = talkId;
        talkMsg.TalkParam1 = talkParam1;
        talkMsg.TalkParam2 = talkParam2;

        SendTalkMsg(talkMsg, InternalConstants.COMMAND_AI);

        return true;
    }

    public void SendTalkMsg(TalkMsg talkMsg, int remoteAction)
    {
        //-------- send multiplayer -----------//

        //if (!remoteAction && remote.is_enable())
        //{
            //// packet strcture : <TalkMsg>
            //char* dataPtr = remote.new_send_queue_msg(MSG_SEND_TALK_MSG, sizeof(TalkMsg) );
            //memcpy( dataPtr, talkMsgPtr, sizeof(TalkMsg) );
            //return;
        //}

        talkMsg.ReplyType = talkMsg.IsReplyNeeded() ? REPLY_WAITING : REPLY_NOT_NEEDED;

        //-- If this is an AI message check if this message has already been sent --//

        if (NationArray[talkMsg.FromNationId].NationType == NationBase.NATION_AI)
        {
            // for messages that do not need a reply, duplication in the message log is allowed.
            if (talkMsg.ReplyType == REPLY_WAITING)
            {
                if (IsTalkMsgExist(talkMsg, false) != 0)
                    return;
            }
        }

        //--- in a multiplayer game, when the msg comes back from the network, CanSendMsg might be different, so we have to check it again ---//

        if (!CanSendMsg(talkMsg.ToNationId, talkMsg.FromNationId, talkMsg.TalkId))
            return;

        //-------- send the message now ---------//

        SendTalkMsgNow(talkMsg);

        //---- if it's a notification message ----//

        if (talkMsg.ReplyType == REPLY_NOT_NEEDED)
            ProcessAcceptedReply(talkMsg);
    }

    private void SendTalkMsgNow(TalkMsg talkMsg)
    {
        //--------- add the message ------------//

        Nation toNation = NationArray[talkMsg.ToNationId];

        talkMsg.Id = _nextTalkMsgId;
        _nextTalkMsgId++;
        talkMsg.Date = Info.GameDate;
        talkMsg.RelationStatus = toNation.GetRelationStatus(talkMsg.FromNationId);

        TalkMessages.Add(talkMsg);

        //--------------------------------------//

        switch (toNation.NationType)
        {
            case NationBase.NATION_OWN: // can be from both AI or a remote player
                NewsArray.Diplomacy(talkMsg);
                if (toNation.GetRelation(talkMsg.FromNationId).HasContact)
                {
                    if (talkMsg.TalkId == TalkMsg.TALK_DECLARE_WAR)
                        Sys.Instance.Audio.OtherSound("DECL_WAR");
                    else
                        Sys.Instance.Audio.NewsSound("GONG");
                }

                break;

            case NationBase.NATION_AI:
                if (talkMsg.ReplyType == REPLY_WAITING)
                {
                    //-- put the message in the receiver's action queue --//

                    toNation.add_action(0, 0, 0, 0, Nation.ACTION_AI_PROCESS_TALK_MSG, talkMsg.Id);
                }
                else if (talkMsg.ReplyType == REPLY_NOT_NEEDED)
                {
                    //--- notify the receiver immediately ---//

                    toNation.notify_talk_msg(talkMsg);
                }

                break;

            case NationBase.NATION_REMOTE: // do nothing here as NATION_OWN handle both msg from AI and a remote player
                break;
        }
    }

    public void ReplyTalkMsg(int talkMsgId, int replyType, int remoteAction)
    {
        //-------- send multiplayer -----------//

        //if (!remoteAction && remote.is_enable())
        //{
            //// packet structure : <talkRecno:int> <reply type:char> <padding:char>
            //char* charPtr = remote.new_send_queue_msg( MSG_REPLY_TALK_MSG, sizeof(int)+2*sizeof(char) );
            //*(int *)charPtr = talkMsgId;
            //charPtr[sizeof(int)] = replyType;
            //charPtr[sizeof(int)+sizeof(char)] = 0;
            //return;
        //}

        //-------------------------------------//

        TalkMsg talkMsg = GetTalkMsg(talkMsgId);
        Nation fromNation = NationArray[talkMsg.FromNationId];

        talkMsg.ReplyType = replyType;
        talkMsg.ReplyDate = Info.GameDate;

        switch (fromNation.NationType)
        {
            case NationBase.NATION_OWN:
                NewsArray.Diplomacy(talkMsg);
                Sys.Instance.Audio.NewsSound("GONG");
                break;

            case NationBase.NATION_AI:
                fromNation.ai_notify_reply(talkMsgId); // notify the AI nation about this reply.
                if (IsTalkMsgDeleted(talkMsgId))
                    return;
                talkMsg = GetTalkMsg(talkMsgId);
                break;

            case NationBase.NATION_REMOTE:
                break;
        }

        if (talkMsg.ReplyType == REPLY_ACCEPT)
        {
            ProcessAcceptedReply(talkMsg);
            if (IsTalkMsgDeleted(talkMsgId))
                return;
            talkMsg = GetTalkMsg(talkMsgId);
        }

        //--- if the player has replied the message, remove it from the news display ---//

        if (talkMsg.ToNationId == NationArray.PlayerId)
            NewsArray.Remove(News.NEWS_DIPLOMACY, talkMsgId);
    }

    public bool CanSendMsg(int toNationId, int fromNationId, int talkId)
    {
        Nation fromNation = NationArray[fromNationId];
        Nation toNation = NationArray[toNationId];

        NationRelation nationRelation = fromNation.GetRelation(toNationId);
        int relationStatus = nationRelation.Status;

        switch (talkId)
        {
            case TalkMsg.TALK_PROPOSE_TRADE_TREATY:
                // allied nations are obliged to trade with each other
                return relationStatus != NationBase.NATION_ALLIANCE &&
                       relationStatus != NationBase.NATION_HOSTILE &&
                       !toNation.GetRelation(fromNationId).TradeTreaty;

            case TalkMsg.TALK_PROPOSE_FRIENDLY_TREATY:
                return relationStatus == NationBase.NATION_TENSE || relationStatus == NationBase.NATION_NEUTRAL;

            case TalkMsg.TALK_PROPOSE_ALLIANCE_TREATY:
                return relationStatus == NationBase.NATION_FRIENDLY || relationStatus == NationBase.NATION_NEUTRAL;

            case TalkMsg.TALK_END_TRADE_TREATY:
                // allied nations are obliged to trade with each other
                return relationStatus != NationBase.NATION_ALLIANCE && toNation.GetRelation(fromNationId).TradeTreaty;

            case TalkMsg.TALK_END_FRIENDLY_TREATY:
                return relationStatus == NationBase.NATION_FRIENDLY;

            case TalkMsg.TALK_END_ALLIANCE_TREATY:
                return relationStatus == NationBase.NATION_ALLIANCE;

            case TalkMsg.TALK_REQUEST_MILITARY_AID:
                return fromNation.IsAtWar() && (relationStatus == NationBase.NATION_FRIENDLY || relationStatus == NationBase.NATION_ALLIANCE);

            case TalkMsg.TALK_REQUEST_TRADE_EMBARGO:
                return relationStatus == NationBase.NATION_FRIENDLY || relationStatus == NationBase.NATION_ALLIANCE;

            case TalkMsg.TALK_REQUEST_CEASE_WAR:
                return relationStatus == NationBase.NATION_HOSTILE;

            case TalkMsg.TALK_REQUEST_DECLARE_WAR:
                // can only request an allied nation to declare war with another nation
                if (relationStatus != NationBase.NATION_ALLIANCE)
                    return false;

                //--- see if this nation has an enemy right now ---//

                foreach (Nation nation in NationArray)
                {
                    if (fromNation.GetRelation(nation.NationId).Status == NationBase.NATION_HOSTILE)
                        return true;
                }

                return false;

            case TalkMsg.TALK_REQUEST_BUY_FOOD:
                return relationStatus != NationBase.NATION_HOSTILE;

            case TalkMsg.TALK_DECLARE_WAR:
                return relationStatus != NationBase.NATION_ALLIANCE &&
                       relationStatus != NationBase.NATION_FRIENDLY &&
                       relationStatus != NationBase.NATION_HOSTILE;

            case TalkMsg.TALK_GIVE_TRIBUTE:
            case TalkMsg.TALK_DEMAND_TRIBUTE:
                return relationStatus <= NationBase.NATION_NEUTRAL;

            case TalkMsg.TALK_GIVE_AID:
            case TalkMsg.TALK_DEMAND_AID:
                return relationStatus >= NationBase.NATION_FRIENDLY;

            case TalkMsg.TALK_GIVE_TECH:
                return fromNation.TotalTechLevel() > 0;

            case TalkMsg.TALK_DEMAND_TECH:
                return toNation.TotalTechLevel() > 0;

            case TalkMsg.TALK_REQUEST_SURRENDER:
                return true;

            case TalkMsg.TALK_SURRENDER:
                return true;
        }

        return false;
    }

    public bool CanSendAnyMsg(int toNationId, int fromNationId)
    {
        return WaitMsgCount(toNationId, fromNationId) < MAX_WAIT_MSG_PER_NATION;
    }

    private int WaitMsgCount(int toNationId, int fromNationId)
    {
        int waitMsgCount = 0;

        //TODO do not iterate all
        for (int i = TalkMessages.Count - 1; i >= 0; i--)
        {
            TalkMsg talkMsg = TalkMessages[i];
            
            if (talkMsg.ReplyType == REPLY_WAITING && talkMsg.ToNationId == toNationId && talkMsg.FromNationId == fromNationId &&
                Info.GameDate < talkMsg.Date.AddDays(GameConstants.TALK_MSG_VALID_DAYS)) // only count message in a month
            {
                waitMsgCount++;
            }
        }

        return waitMsgCount;
    }

    public TalkMsg GetTalkMsg(int id)
    {
        for (int i = TalkMessages.Count - 1; i >= 0; i--)
        {
            TalkMsg talkMsg = TalkMessages[i];
            if (talkMsg.Id == id)
                return talkMsg;
        }
        
        return null;
    }

    public void DelTalkMsg(int talkMsgId)
    {
        TalkMsg talkMsg = GetTalkMsg(talkMsgId);

        Nation nation = NationArray[talkMsg.ToNationId];

        //--- if this message is sent to an AI nation ---//
        // even if a reply is not needed, the message will still be sent to the AI for notification.
        if (nation.NationType == NationBase.NATION_AI && (talkMsg.ReplyType == REPLY_NOT_NEEDED || talkMsg.ReplyType == REPLY_WAITING))
        {
            //--- it may still have the message in its action queue ---//

            for (int i = nation.action_count(); i > 0; i--)
            {
                ActionNode actionNode = nation.get_action(i);

                if (actionNode.action_mode == Nation.ACTION_AI_PROCESS_TALK_MSG && actionNode.action_para == talkMsgId)
                {
                    nation.del_action(actionNode);
                    break;
                }
            }
        }

        if (talkMsg.ToNationId == NationArray.PlayerId || talkMsg.FromNationId == NationArray.PlayerId)
        {
            NewsArray.Remove(News.NEWS_DIPLOMACY, talkMsgId);
        }

        TalkMessages.Remove(talkMsg);
    }

    public bool IsTalkMsgDeleted(int id)
    {
        for (int i = TalkMessages.Count - 1; i >= 0; i--)
        {
            TalkMsg talkMsg = TalkMessages[i];
            if (talkMsg.Id == id)
                return false;
        }

        return true;
    }

    public void DeleteAllNationMessages(int nationId)
    {
        for (int i = TalkMessages.Count - 1; i >= 0; i--)
        {
            TalkMsg talkMsg = TalkMessages[i];

            // If the nation is referenced anywhere in the talk-message,
            // IsValidToDisplay() will return false and the talk message should than be deleted.
            // Need to explicitly specify nation as it hasn't been deleted yet.
            if (!talkMsg.IsValidToDisplay(nationId))
            {
                DelTalkMsg(talkMsg.Id);
            }
        }
    }

    public int IsTalkMsgExist(TalkMsg thisTalkMsg, bool checkPara)
    {
        for (int i = TalkMessages.Count - 1; i >= 0; i--)
        {
            TalkMsg talkMsg = TalkMessages[i];
            if (talkMsg.ReplyType == REPLY_WAITING || talkMsg.ReplyType == REPLY_NOT_NEEDED)
            {
                if (talkMsg.TalkId == thisTalkMsg.TalkId && talkMsg.FromNationId == thisTalkMsg.FromNationId && talkMsg.ToNationId == thisTalkMsg.ToNationId)
                {
                    if (checkPara)
                    {
                        if (talkMsg.TalkParam1 == thisTalkMsg.TalkParam1 && talkMsg.TalkParam2 == thisTalkMsg.TalkParam2)
                        {
                            return talkMsg.Id;
                        }
                    }
                    else
                    {
                        return talkMsg.Id;
                    }
                }
            }
        }

        return 0;
    }

    public void NextDay()
    {
        if (Info.TotalDays % 7 == 0)
        {
            //TODO do not iterate all
            for (int i = TalkMessages.Count - 1; i >= 0; i --)
            {
                TalkMsg talkMsg = TalkMessages[i];
                //--------------------------------------------------------//
                // If this is an AI message and there is no response from
                // the player after one month the message has been sent,
                // it presumes that the message has been rejected.
                //--------------------------------------------------------//

                if (talkMsg.ReplyType == REPLY_WAITING && NationArray[talkMsg.FromNationId].NationType == NationBase.NATION_AI &&
                    Info.GameDate > talkMsg.Date.AddDays(GameConstants.TALK_MSG_VALID_DAYS + 1))
                {
                    talkMsg.ReplyType = REPLY_REJECT;

                    NationArray[talkMsg.FromNationId].ai_notify_reply(talkMsg.Id);
                }

                //--- delete the talk message after a year ---//

                //if (Info.GameDate > talkMsg.Date.AddDays(GameConstants.TALK_MSG_KEEP_DAYS))
                    //DelTalkMsg(talkMsg.RecNo);
            }
        }
    }

    private void ProcessAcceptedReply(TalkMsg talkMsg)
    {
        //---- delete duplicate message in reverse now this has been accepted ----//
        DeleteMsgInReverse(talkMsg); // do this now because talkMsg may be invalid after sending a reply

        Nation fromNation = NationArray[talkMsg.FromNationId];
        Nation toNation = NationArray[talkMsg.ToNationId];

        NationRelation fromRelation = fromNation.GetRelation(talkMsg.ToNationId);
        NationRelation toRelation = toNation.GetRelation(talkMsg.FromNationId);

        int goodRelationDec = 0; // whether the message is for requesting help.

        switch (talkMsg.TalkId)
        {
            case TalkMsg.TALK_PROPOSE_TRADE_TREATY:
                toNation.SetTradeTreaty(talkMsg.FromNationId, true);
                break;

            case TalkMsg.TALK_PROPOSE_FRIENDLY_TREATY:
                toNation.FormFriendlyTreaty(talkMsg.FromNationId);
                break;

            case TalkMsg.TALK_PROPOSE_ALLIANCE_TREATY:
                toNation.FormAllianceTreaty(talkMsg.FromNationId);
                break;

            case TalkMsg.TALK_END_TRADE_TREATY:
                toNation.SetTradeTreaty(talkMsg.FromNationId, false);

                //---- set reject date on proposing treaty ----//

                fromRelation.LastTalkRejectDates[TalkMsg.TALK_PROPOSE_TRADE_TREATY - 1] = Info.GameDate;
                toRelation.LastTalkRejectDates[TalkMsg.TALK_PROPOSE_TRADE_TREATY - 1] = Info.GameDate;

                fromRelation.LastTalkRejectDates[TalkMsg.TALK_END_TRADE_TREATY - 1] = Info.GameDate;
                toRelation.LastTalkRejectDates[TalkMsg.TALK_END_TRADE_TREATY - 1] = Info.GameDate;

                //----- decrease reputation -----//

                if (toNation.Reputation > 0)
                    fromNation.ChangeReputation(-toNation.Reputation * 5.0 / 100.0);
                break;

            case TalkMsg.TALK_END_FRIENDLY_TREATY:
            case TalkMsg.TALK_END_ALLIANCE_TREATY:
                fromNation.EndTreaty(talkMsg.ToNationId, NationBase.NATION_NEUTRAL);

                //---- set reject date on proposing treaty ----//
                //
                // If a friendly treaty is rejected, assuming an alliance treaty is even more impossible.
                // (thus set reject date on both friendly and alliance treaties.)
                //
                // If an alliance treaty is rejected, only set reject date on alliance treaty,
                // it may still try proposing friendly treaty later.
                //
                //---------------------------------------------//

                fromRelation.LastTalkRejectDates[TalkMsg.TALK_PROPOSE_FRIENDLY_TREATY - 1] = Info.GameDate;
                toRelation.LastTalkRejectDates[TalkMsg.TALK_PROPOSE_FRIENDLY_TREATY - 1] = Info.GameDate;

                fromRelation.LastTalkRejectDates[TalkMsg.TALK_PROPOSE_ALLIANCE_TREATY - 1] = Info.GameDate;
                toRelation.LastTalkRejectDates[TalkMsg.TALK_PROPOSE_ALLIANCE_TREATY - 1] = Info.GameDate;
                break;

            case TalkMsg.TALK_REQUEST_MILITARY_AID:
                goodRelationDec = 10;
                break;

            case TalkMsg.TALK_REQUEST_TRADE_EMBARGO:
            {
                //--- send an end treaty message to the target kingdom ---//

                TalkMsg talkMsgReply = new TalkMsg();
                talkMsgReply.ToNationId = talkMsg.TalkParam1;
                talkMsgReply.FromNationId = talkMsg.ToNationId;
                talkMsgReply.TalkId = TalkMsg.TALK_END_TRADE_TREATY;

                SendTalkMsg(talkMsgReply, InternalConstants.COMMAND_AUTO);
                goodRelationDec = 4;
                break;
            }

            case TalkMsg.TALK_REQUEST_CEASE_WAR:
                UnitArray.StopWarBetween(talkMsg.ToNationId, talkMsg.FromNationId);
                toNation.SetRelationStatus(talkMsg.FromNationId, NationBase.NATION_TENSE);

                fromRelation.LastTalkRejectDates[TalkMsg.TALK_DECLARE_WAR - 1] = Info.GameDate;
                toRelation.LastTalkRejectDates[TalkMsg.TALK_DECLARE_WAR - 1] = Info.GameDate;
                break;

            case TalkMsg.TALK_REQUEST_DECLARE_WAR:
                // the requesting nation must be at war with the enemy
                if (fromNation.GetRelationStatus(talkMsg.TalkParam1) == NationBase.NATION_HOSTILE)
                {
                    TalkMsg talkMsgReply = new TalkMsg();
                    talkMsgReply.ToNationId = talkMsg.TalkParam1;
                    talkMsgReply.FromNationId = talkMsg.ToNationId;
                    talkMsgReply.TalkId = TalkMsg.TALK_DECLARE_WAR;

                    //-- if we are currently allied or friendly with the nation, we need to terminate the friendly/alliance treaty first --//

                    if (toNation.GetRelationStatus(talkMsg.TalkParam1) == NationBase.NATION_ALLIANCE)
                    {
                        TalkMsg breakTreatyMsg = new TalkMsg();
                        breakTreatyMsg.ToNationId = talkMsg.TalkParam1;
                        breakTreatyMsg.FromNationId = talkMsg.ToNationId;
                        breakTreatyMsg.TalkId = TalkMsg.TALK_END_ALLIANCE_TREATY;

                        SendTalkMsg(breakTreatyMsg, InternalConstants.COMMAND_AUTO);
                    }

                    else if (toNation.GetRelationStatus(talkMsg.TalkParam1) == NationBase.NATION_FRIENDLY)
                    {
                        TalkMsg breakTreatyMsg = new TalkMsg();
                        breakTreatyMsg.ToNationId = talkMsg.TalkParam1;
                        breakTreatyMsg.FromNationId = talkMsg.ToNationId;
                        breakTreatyMsg.TalkId = TalkMsg.TALK_END_FRIENDLY_TREATY;
                        SendTalkMsg(breakTreatyMsg, InternalConstants.COMMAND_AUTO);
                    }

                    //--- send a declare war message to the target kingdom ---//

                    SendTalkMsg(talkMsgReply, InternalConstants.COMMAND_AUTO);

                    //----------------------------------------------------//

                    goodRelationDec = 10;
                }

                break;

            case TalkMsg.TALK_REQUEST_BUY_FOOD:
            {
                int buyCost = talkMsg.TalkParam1 * talkMsg.TalkParam2 / 10;

                fromNation.AddFood(talkMsg.TalkParam1);
                fromNation.AddExpense(NationBase.EXPENSE_IMPORTS, buyCost, false);
                toNation.ConsumeFood(talkMsg.TalkParam1);
                toNation.AddIncome(NationBase.INCOME_EXPORTS, buyCost, false);
                break;
            }

            case TalkMsg.TALK_DECLARE_WAR:
                // how many times this nation has started a war with us, the more the times the worse this nation is.
                toRelation.StartedWarOnUsCount++;
                toNation.SetRelationStatus(talkMsg.FromNationId, NationBase.NATION_HOSTILE);

                fromRelation.LastTalkRejectDates[TalkMsg.TALK_REQUEST_CEASE_WAR - 1] = Info.GameDate;
                toRelation.LastTalkRejectDates[TalkMsg.TALK_REQUEST_CEASE_WAR - 1] = Info.GameDate;

                //--- decrease reputation of the nation which declares war ---//

                if (toNation.Reputation > 0)
                    fromNation.ChangeReputation(-toNation.Reputation * 20.0 / 100.0);
                break;

            case TalkMsg.TALK_GIVE_TRIBUTE:
            case TalkMsg.TALK_GIVE_AID:
                fromNation.GiveTribute(talkMsg.ToNationId, talkMsg.TalkParam1);
                break;

            case TalkMsg.TALK_DEMAND_TRIBUTE:
            case TalkMsg.TALK_DEMAND_AID:
                toNation.GiveTribute(talkMsg.FromNationId, talkMsg.TalkParam1);
                goodRelationDec = talkMsg.TalkParam1 / 200;
                break;

            case TalkMsg.TALK_GIVE_TECH:
                fromNation.GiveTech(talkMsg.ToNationId, talkMsg.TalkParam1, talkMsg.TalkParam2);
                break;

            case TalkMsg.TALK_DEMAND_TECH:
                // get the latest tech version id. of the agreed nation.
                talkMsg.TalkParam2 = toNation.GetTechLevel(talkMsg.TalkParam1);
                toNation.GiveTech(talkMsg.FromNationId, talkMsg.TalkParam1, talkMsg.TalkParam2);
                goodRelationDec = talkMsg.TalkParam2 * 3;
                break;

            case TalkMsg.TALK_REQUEST_SURRENDER:
                // * 10 is to restore its original value. It has been divided by 10 to cope with the upper limit of <short>
                double offeredAmt = talkMsg.TalkParam1 * 10.0;

                toNation.AddIncome(NationBase.INCOME_TRIBUTE, offeredAmt);
                fromNation.AddExpense(NationBase.EXPENSE_TRIBUTE, offeredAmt);

                toNation.Surrender(talkMsg.FromNationId);
                break;

            case TalkMsg.TALK_SURRENDER:
                fromNation.Surrender(talkMsg.ToNationId);
                break;
        }

        //---- if the nation accepts a message that is requesting help, then decrease its GoodRelationDurationRating, so it won't accept so easily next time ---//

        if (goodRelationDec > 0)
            toRelation.GoodRelationDurationRating -= goodRelationDec;
    }

    private void DeleteMsgInReverse(TalkMsg talkMsg)
    {
        TalkMsg talkMsgReverse = new TalkMsg();
        talkMsgReverse.FromNationId = talkMsg.ToNationId;
        talkMsgReverse.ToNationId = talkMsg.FromNationId;
        talkMsgReverse.TalkId = talkMsg.TalkId;

        while (true)
        {
            int talkMsgId = IsTalkMsgExist(talkMsgReverse, false);

            if (talkMsgId != 0)
                DelTalkMsg(talkMsgId);
            else
                break;
        }
    }
    
    #region SaveAndLoad

    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(_nextTalkMsgId);
        writer.Write(TalkMessages.Count);
        for (int i = 0; i < TalkMessages.Count; i++)
            TalkMessages[i].SaveTo(writer);
    }

    public void LoadFrom(BinaryReader reader)
    {
        _nextTalkMsgId = reader.ReadInt32();
        int talkMessagesCount = reader.ReadInt32();
        for (int i = 0; i < talkMessagesCount; i++)
        {
            TalkMsg talkMsg = new TalkMsg();
            talkMsg.LoadFrom(reader);
            TalkMessages.Add(talkMsg);
        }
    }
	
    #endregion
}