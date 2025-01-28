using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class TalkChoice
{
    public string str;
    public int para;
}

public class TalkRes
{
    public const int MAX_WAIT_MSG_PER_NATION = 3;

    public const int REPLY_NOT_NEEDED = 0;
    public const int REPLY_WAITING = 1;
    public const int REPLY_ACCEPT = 2;
    public const int REPLY_REJECT = 3;

    public int reply_talk_msg_recno;

    public TalkMsg cur_talk_msg = new TalkMsg();
    public string choice_question;
    public string choice_question_second_line;

    public int talk_choice_count;
    public TalkChoice[] talk_choice_array = new TalkChoice[TalkMsg.MAX_TALK_CHOICE];

    public int[] available_talk_id_array = new int[TalkMsg.MAX_TALK_TYPE];

    public int cur_choice_id;
    public int save_view_mode;

    public bool msg_add_nation_color;

    private List<TalkMsg> talk_msg_array = new List<TalkMsg>();

    private static string[] nation_name_str_array = new string[GameConstants.MAX_NATION];

    private Info Info => Sys.Instance.Info;
    private SECtrl SECtrl => Sys.Instance.SECtrl;
    private TechRes TechRes => Sys.Instance.TechRes;
    private NationArray NationArray => Sys.Instance.NationArray;
    private UnitArray UnitArray => Sys.Instance.UnitArray;
    private NewsArray NewsArray => Sys.Instance.NewsArray;


    public TalkRes()
    {
    }

    public void init_conversation(int toNationRecno)
    {
        cur_talk_msg.from_nation_recno = NationArray.player_recno;
        cur_talk_msg.to_nation_recno = toNationRecno;

        set_talk_choices();
    }

    public bool ai_send_talk_msg(int toNationRecno, int fromNationRecno, int talkId,
        int talkPara1 = 0, int talkPara2 = 0, bool forceSend = false)
    {
        Nation fromNation = NationArray[fromNationRecno];

        if (!fromNation.is_ai())
            return false;

        //--- first check again if the nation should send the message now ---//

        if (!forceSend)
        {
            if (!fromNation.should_diplomacy_retry(talkId, toNationRecno))
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
            fromNation.get_relation(toNationRecno).last_talk_reject_date_array[oppTalkId - 1] = Info.game_date;
        }

        //------------------------------------------//

        TalkMsg talkMsg = new TalkMsg();
        talkMsg.to_nation_recno = toNationRecno;
        talkMsg.from_nation_recno = fromNationRecno;
        talkMsg.talk_id = talkId;
        talkMsg.talk_para1 = talkPara1;
        talkMsg.talk_para2 = talkPara2;

        send_talk_msg(talkMsg, InternalConstants.COMMAND_AI);

        return true;
    }

    public void send_talk_msg(TalkMsg talkMsg, int remoteAction)
    {
        //-------- send multiplayer -----------//

        //if( !remoteAction && remote.is_enable() )
        //{
        //// packet strcture : <TalkMsg>
        //char* dataPtr = remote.new_send_queue_msg(MSG_SEND_TALK_MSG, sizeof(TalkMsg) );

        //memcpy( dataPtr, talkMsgPtr, sizeof(TalkMsg) );
        //return;
        //}

        //------ the TalkMsg::reply_type ------//

        if (talkMsg.is_reply_needed())
            talkMsg.reply_type = REPLY_WAITING;
        else
            talkMsg.reply_type = REPLY_NOT_NEEDED;

        //-- If this is an AI message check if this message has already been sent --//

        if (NationArray[talkMsg.from_nation_recno].nation_type == NationBase.NATION_AI)
        {
            // for messages that do not need a reply, duplication in the message log is allowed.
            if (talkMsg.reply_type == REPLY_WAITING)
            {
                if (is_talk_msg_exist(talkMsg, false) != 0) // 0-don't check talk_para1 & talk_para2
                    return;
            }
        }

        //--- in a multiplayer game, when the msg comes back from the network, can_send_msg might be different, so we have to check it again ---//

        if (!can_send_msg(talkMsg.to_nation_recno, talkMsg.from_nation_recno, talkMsg.talk_id))
            return;

        //-------- send the message now ---------//

        send_talk_msg_now(talkMsg);

        //---- if it's a notification message ----//

        if (talkMsg.reply_type == REPLY_NOT_NEEDED)
            process_accepted_reply(talkMsg);
    }

    public void send_talk_msg_now(TalkMsg talkMsg)
    {
        //--------- add the message ------------//

        Nation toNation = NationArray[talkMsg.to_nation_recno];

        talkMsg.date = Info.game_date;
        talkMsg.relation_status = toNation.get_relation_status(talkMsg.from_nation_recno);

        talk_msg_array.Add(talkMsg);

        //--------------------------------------//

        switch (toNation.nation_type)
        {
            case NationBase.NATION_OWN: // can be from both AI or a remote player
                NewsArray.diplomacy(talkMsg);
                if (toNation.get_relation(talkMsg.from_nation_recno).has_contact)
                    SECtrl.immediate_sound(talkMsg.talk_id == TalkMsg.TALK_DECLARE_WAR ? "DECL_WAR" : "GONG");
                break;

            case NationBase.NATION_AI:
                if (talkMsg.reply_type == REPLY_WAITING)
                {
                    //-- put the message in the receiver's action queue --//

                    toNation.add_action(0, 0, 0, 0,
                        Nation.ACTION_AI_PROCESS_TALK_MSG, talkMsg.RecNo);
                }
                else if (talkMsg.reply_type == REPLY_NOT_NEEDED)
                {
                    //--- notify the receiver immediately ---//

                    toNation.notify_talk_msg(talkMsg);
                }

                break;

            case NationBase.NATION_REMOTE: // do nothing here as NATION_OWN handle both msg from AI and a remote player
                break;
        }
    }

    public void reply_talk_msg(int talkMsgRecno, int replyType, int remoteAction)
    {
        //-------- send multiplayer -----------//

        //if( !remoteAction && remote.is_enable() )
        //{
        //// packet structure : <talkRecno:int> <reply type:char> <padding:char>
        //char* charPtr = remote.new_send_queue_msg( MSG_REPLY_TALK_MSG, sizeof(int)+2*sizeof(char) );
        //*(int *)charPtr = talkMsgRecno;
        //charPtr[sizeof(int)] = replyType;
        //charPtr[sizeof(int)+sizeof(char)] = 0;
        //return;
        //}

        //-------------------------------------//

        TalkMsg talkMsg = get_talk_msg(talkMsgRecno);
        Nation fromNation = NationArray[talkMsg.from_nation_recno];

        talkMsg.reply_type = replyType;
        talkMsg.reply_date = Info.game_date;

        switch (fromNation.nation_type)
        {
            case NationBase.NATION_OWN:
                NewsArray.diplomacy(talkMsg);
                SECtrl.immediate_sound("GONG");
                break;

            case NationBase.NATION_AI:
                fromNation.ai_notify_reply(talkMsgRecno); // notify the AI nation about this reply.
                if (is_talk_msg_deleted(talkMsgRecno))
                    return;
                talkMsg = get_talk_msg(talkMsgRecno);
                break;

            case NationBase.NATION_REMOTE:
                break;
        }

        //------- if the offer is accepted -------//

        if (talkMsg.reply_type == REPLY_ACCEPT)
        {
            process_accepted_reply(talkMsg);
            if (is_talk_msg_deleted(talkMsgRecno))
                return;
            talkMsg = get_talk_msg(talkMsgRecno);
        }

        //--- if the player has replyed the message, remove it from the news display ---//

        if (talkMsg.to_nation_recno == NationArray.player_recno)
            NewsArray.remove(News.NEWS_DIPLOMACY, talkMsgRecno);
    }

    public bool can_send_msg(int toNationRecno, int fromNationRecno, int talkId)
    {
        Nation fromNation = NationArray[fromNationRecno];
        Nation toNation = NationArray[toNationRecno];

        NationRelation nationRelation = fromNation.get_relation(toNationRecno);
        int relationStatus = nationRelation.status;

        switch (talkId)
        {
            case TalkMsg.TALK_PROPOSE_TRADE_TREATY:
                // allied nations are obliged to trade with each other
                return relationStatus != NationBase.NATION_ALLIANCE &&
                       relationStatus != NationBase.NATION_HOSTILE &&
                       !toNation.get_relation(fromNationRecno).trade_treaty;

            case TalkMsg.TALK_PROPOSE_FRIENDLY_TREATY:
                return relationStatus == NationBase.NATION_TENSE || relationStatus == NationBase.NATION_NEUTRAL;

            case TalkMsg.TALK_PROPOSE_ALLIANCE_TREATY:
                return relationStatus == NationBase.NATION_FRIENDLY || relationStatus == NationBase.NATION_NEUTRAL;

            case TalkMsg.TALK_END_TRADE_TREATY:
                // allied nations are obliged to trade with each other
                return relationStatus != NationBase.NATION_ALLIANCE &&
                       toNation.get_relation(fromNationRecno).trade_treaty;

            case TalkMsg.TALK_END_FRIENDLY_TREATY:
                return relationStatus == NationBase.NATION_FRIENDLY;

            case TalkMsg.TALK_END_ALLIANCE_TREATY:
                return relationStatus == NationBase.NATION_ALLIANCE;

            case TalkMsg.TALK_REQUEST_MILITARY_AID:
                return fromNation.is_at_war() &&
                       (relationStatus == NationBase.NATION_FRIENDLY || relationStatus == NationBase.NATION_ALLIANCE);

            case TalkMsg.TALK_REQUEST_TRADE_EMBARGO:
                return relationStatus == NationBase.NATION_FRIENDLY || relationStatus == NationBase.NATION_ALLIANCE;

            case TalkMsg.TALK_REQUEST_CEASE_WAR:
                return relationStatus == NationBase.NATION_HOSTILE;

            case TalkMsg.TALK_REQUEST_DECLARE_WAR:
            {
                // can only request an allied nation to declare war with another nation
                if (relationStatus != NationBase.NATION_ALLIANCE)
                    return false;

                //--- see if this nation has an enemy right now ---//

                foreach (Nation nation in NationArray)
                {
                    if (fromNation.get_relation(nation.nation_recno).status == NationBase.NATION_HOSTILE)
                        return true;
                }

                return false;
            }

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
                return fromNation.total_tech_level() > 0;

            case TalkMsg.TALK_DEMAND_TECH:
                return toNation.total_tech_level() > 0;

            case TalkMsg.TALK_REQUEST_SURRENDER:
                return true;

            case TalkMsg.TALK_SURRENDER:
                return true;
        }

        return false;
    }

    public bool can_send_any_msg(int toNationRecno, int fromNationRecno)
    {
        return wait_msg_count(toNationRecno, fromNationRecno) < MAX_WAIT_MSG_PER_NATION;
    }

    public bool set_talk_choices()
    {
        //------------------------------------//
        talk_choice_count = 0;
        choice_question = String.Empty;
        choice_question_second_line = String.Empty;
        cur_choice_id = 0;

        //------------------------------------//

        bool rc = false;

        for (int i = 0; i < available_talk_id_array.Length; i++)
        {
            available_talk_id_array[i] = 0;
        }

        switch (cur_talk_msg.talk_id)
        {
            //--- add the main option choices ---//

            case 0:
                add_main_choices();
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
                rc = add_trade_embargo_choices();
                break;

            case TalkMsg.TALK_REQUEST_DECLARE_WAR:
                rc = add_declare_war_choices();
                break;

            case TalkMsg.TALK_REQUEST_BUY_FOOD:
                rc = add_buy_food_choices();
                break;

            case TalkMsg.TALK_GIVE_TRIBUTE:
            case TalkMsg.TALK_DEMAND_TRIBUTE:
                rc = add_tribute_choices();
                // add the choice question here because we use the same function for both tribute and aid
                if (rc)
                    choice_question = "How much tribute?";
                break;

            case TalkMsg.TALK_GIVE_AID:
            case TalkMsg.TALK_DEMAND_AID:
                rc = add_tribute_choices();
                if (rc)
                    choice_question = "How much aid?";
                break;

            case TalkMsg.TALK_GIVE_TECH:
            case TalkMsg.TALK_DEMAND_TECH:
                rc = add_give_tech_choices();
                break;

            case TalkMsg.TALK_REQUEST_SURRENDER:
                rc = add_request_surrender_choices();
                break;

            case TalkMsg.TALK_SURRENDER:
                rc = add_surrender_choices();
                break;
        }

        if (rc)
            add_talk_choice("Cancel.", 0);

        return rc;
    }

    public void player_reply(int talkMsgRecno)
    {
        //------- set the reply choices --------//

        TalkMsg talkMsg = get_talk_msg(talkMsgRecno);

        if (NationArray.IsDeleted(talkMsg.from_nation_recno))
            return;

        init_conversation(talkMsg.from_nation_recno);

        talk_choice_count = 0;
        cur_choice_id = 0;
        reply_talk_msg_recno = talkMsgRecno;

        //--------- add talk choices ---------//

        string msgStr = talkMsg.msg_str(NationArray.player_recno);
        choice_question = msgStr;

        //---- see if this message has a second line -----//

        string msgStr2 = talkMsg.msg_str(NationArray.player_recno, false, true);

        if (msgStr != msgStr2)
            choice_question_second_line = msgStr2;
        else
            choice_question_second_line = String.Empty;

        //--------- add choices to the question ---------//

        if (talkMsg.can_accept()) // whether the replier can accept the request or demand of the message
            add_talk_choice("Accept.", 1);

        add_talk_choice("Reject.", 0);

        //--- switch to the nation report mode and go to the diplomacy mode ---//

        Info.init_player_reply(talkMsg.from_nation_recno);

        //TODO drawing
        //save_view_mode = sys.view_mode;
        //sys.set_view_mode(MODE_NATION);
    }

    public int wait_msg_count(int toNationRecno, int fromNationRecno)
    {
        int waitMsgCount = 0;

        for (int i = talk_msg_array.Count - 1; i >= 0; i--)
        {
            TalkMsg talkMsg = talk_msg_array[i];
            
            if (talkMsg.reply_type == REPLY_WAITING &&
                talkMsg.to_nation_recno == toNationRecno &&
                talkMsg.from_nation_recno == fromNationRecno &&
                Info.game_date < talkMsg.date.AddDays(GameConstants.TALK_MSG_VALID_DAYS)) // only count message in a month
            {
                waitMsgCount++;
            }
        }

        return waitMsgCount;
    }

    public TalkMsg get_talk_msg(int recNo)
    {
        foreach (TalkMsg talkMsg in talk_msg_array)
        {
            if (talkMsg.RecNo == recNo)
                return talkMsg;
        }
        
        return null;
    }

    public void del_talk_msg(int talkMsgRecno)
    {
        TalkMsg talkMsg = get_talk_msg(talkMsgRecno);

        //--- if this message is sent to an AI nation ---//

        Nation nation = NationArray[talkMsg.to_nation_recno];

        // even if a reply is not needed, the message will still be sent to the AI for notification.
        if (nation.nation_type == NationBase.NATION_AI &&
            (talkMsg.reply_type == REPLY_NOT_NEEDED || talkMsg.reply_type == REPLY_WAITING))
        {
            //--- it may still have the message in its action queue ---//

            for (int i = nation.action_count(); i > 0; i--)
            {
                ActionNode actionNode = nation.get_action(i);

                if (actionNode.action_mode == Nation.ACTION_AI_PROCESS_TALK_MSG &&
                    actionNode.action_para == talkMsgRecno)
                {
                    nation.del_action(actionNode);
                    break;
                }
            }
        }

        //----- delete the message from the news array -----//

        if (talkMsg.to_nation_recno == NationArray.player_recno ||
            talkMsg.from_nation_recno == NationArray.player_recno)
        {
            NewsArray.remove(News.NEWS_DIPLOMACY, talkMsgRecno);
        }

        //----- link it out from talk_msg_array -----//

        talk_msg_array.Remove(talkMsg);
    }

    public bool is_talk_msg_deleted(int recNo)
    {
        foreach (TalkMsg talkMsg in talk_msg_array)
        {
            if (talkMsg.RecNo == recNo)
                return false;
        }

        return true;
    }

    public void del_all_nation_msg(int nationRecno)
    {
        for (int i = talk_msg_array.Count - 1; i >= 0; i--)
        {
            TalkMsg talkMsg = talk_msg_array[i];

            // If the nation is referenced anywhere in the talk-message,
            // is_valid_to_disp() will return 0 and the talk message should than be deleted.
            // Need to explicitly specify nation as it hasn't been deleted yet.
            if (!talkMsg.is_valid_to_disp(nationRecno))
            {
                del_talk_msg(talkMsg.RecNo);
            }
        }
    }

    public int is_talk_msg_exist(TalkMsg thisTalkMsg, bool checkPara)
    {
        foreach (TalkMsg talkMsg in talk_msg_array)
        {
            if (talkMsg.reply_type == REPLY_WAITING || talkMsg.reply_type == REPLY_NOT_NEEDED)
            {
                if (talkMsg.talk_id == thisTalkMsg.talk_id &&
                    talkMsg.from_nation_recno == thisTalkMsg.from_nation_recno &&
                    talkMsg.to_nation_recno == thisTalkMsg.to_nation_recno)
                {
                    if (checkPara)
                    {
                        if (talkMsg.talk_para1 == thisTalkMsg.talk_para1 &&
                            talkMsg.talk_para2 == thisTalkMsg.talk_para2)
                        {
                            return talkMsg.RecNo;
                        }
                    }
                    else
                        return talkMsg.RecNo;
                }
            }
        }

        return 0;
    }

    public void next_day()
    {
        if (Info.TotalDays % 7 == 0)
            process_talk_msg();
    }

    private void add_talk_choice(string talkStr, int talkPara)
    {
        talk_choice_count++;

        talk_choice_array[talk_choice_count - 1].str = talkStr;
        talk_choice_array[talk_choice_count - 1].para = talkPara;
    }

    private void add_main_choices()
    {
        string[] talkMsgArray =
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

        //-----------------------------------------//

        for (int i = 1; i <= TalkMsg.MAX_TALK_TYPE; i++)
        {
            if (!can_send_msg(cur_talk_msg.to_nation_recno, NationArray.player_recno, i))
                continue;

            add_talk_choice(talkMsgArray[i - 1], i);
            available_talk_id_array[i - 1] = 1;
        }
    }

    private bool add_trade_embargo_choices()
    {
        if (cur_talk_msg.talk_para1 != 0)
            return false;

        choice_question = "Request an embargo on trade with which kingdom?";

        Nation fromNation = NationArray[cur_talk_msg.from_nation_recno];
        Nation toNation = NationArray[cur_talk_msg.to_nation_recno];

        foreach (Nation nation in NationArray)
        {
            int nationRecno = nation.nation_recno;

            if (nationRecno == cur_talk_msg.from_nation_recno || nationRecno == cur_talk_msg.to_nation_recno)
            {
                continue;
            }

            if (!fromNation.get_relation(nationRecno).trade_treaty &&
                toNation.get_relation(nationRecno).trade_treaty)
            {
                //------ add color bar -------//

                nation_name_str_array[nationRecno - 1] = "@COL" + Convert.ToChar(30 + NationArray[nationRecno].color_scheme_id);

                //------ add natino name ------//

                nation_name_str_array[nationRecno - 1] += NationArray[nationRecno].nation_name();

                //---- add talk choice string ------//

                add_talk_choice(nation_name_str_array[nationRecno - 1], nationRecno);
            }
        }

        return true;
    }

    private bool add_declare_war_choices()
    {
        if (cur_talk_msg.talk_para1 != 0)
            return false;

        choice_question = "Declare war on which kingdom?";

        Nation fromNation = NationArray[cur_talk_msg.from_nation_recno];
        Nation toNation = NationArray[cur_talk_msg.to_nation_recno];

        foreach (Nation nation in NationArray)
        {
            int nationRecno = nation.nation_recno;

            //--- can only ask another nation to declare war with a nation that is currently at war with our nation ---//

            if (fromNation.get_relation_status(nationRecno) == NationBase.NATION_HOSTILE &&
                toNation.get_relation_status(nationRecno) != NationBase.NATION_HOSTILE)
            {
                //------ add color bar -------//

                nation_name_str_array[nationRecno - 1] = "@COL" + Convert.ToChar(30 + NationArray[nationRecno].color_scheme_id);

                //------ add natino name ------//

                nation_name_str_array[nationRecno - 1] += NationArray[nationRecno].nation_name();

                //---- add talk choice string ------//

                add_talk_choice(nation_name_str_array[nationRecno - 1], nationRecno);
            }
        }

        return true;
    }

    private bool add_buy_food_choices()
    {
        if (cur_talk_msg.talk_para1 == 0)
        {
            choice_question = "How much food do you want to purchase?";

            string[] qtyStrArray = { "500.", "1000.", "2000.", "4000." };
            int[] qtyArray = { 500, 1000, 2000, 4000 };

            for (int i = 0; i < qtyStrArray.Length; i++)
            {
                if (NationArray.player.cash >= qtyArray[i] * GameConstants.MIN_FOOD_PURCHASE_PRICE / 10.0)
                    add_talk_choice(qtyStrArray[i], qtyArray[i]);
            }

            return true;
        }
        else if (cur_talk_msg.talk_para2 == 0)
        {
            choice_question = "How much do you offer for 10 units of food?";

            string[] priceStrArray = { "$5.", "$10.", "$15.", "$20." };
            int[] priceArray = { 5, 10, 15, 20 };

            for (int i = 0; i < priceStrArray.Length; i++)
            {
                if (i == 0 || NationArray.player.cash >= cur_talk_msg.talk_para1 * priceArray[i] / 10.0)
                    add_talk_choice(priceStrArray[i], priceArray[i]);
            }

            return true;
        }
        else
            return false;
    }

    private bool add_tribute_choices()
    {
        if (cur_talk_msg.talk_para1 != 0)
            return false;

        string[] tributeStrArray = { "$500.", "$1000.", "$2000.", "$3000.", "$4000." };
        int[] tributeAmtArray = { 500, 1000, 2000, 3000, 4000 };

        for (int i = 0; i < tributeStrArray.Length; i++)
        {
            // when demand tribute, the amount can be sent to any
            if (cur_talk_msg.talk_id == TalkMsg.TALK_DEMAND_TRIBUTE ||
                cur_talk_msg.talk_id == TalkMsg.TALK_DEMAND_AID ||
                NationArray.player.cash >= tributeAmtArray[i])
            {
                add_talk_choice(tributeStrArray[i], tributeAmtArray[i]);
            }
        }

        return true;
    }

    private bool add_give_tech_choices()
    {
        int techNationRecno;

        if (cur_talk_msg.talk_id == TalkMsg.TALK_GIVE_TECH)
            techNationRecno = cur_talk_msg.from_nation_recno;
        else // demand tech
            techNationRecno = cur_talk_msg.to_nation_recno;

        if (cur_talk_msg.talk_para1 == 0)
        {
            choice_question = "Which technology?";

            for (int i = 1; i <= TechRes.tech_info_array.Length; i++)
            {
                if (TechRes[i].get_nation_tech_level(techNationRecno) > 0)
                {
                    add_talk_choice(TechRes[i].tech_des(), i);
                }
            }

            return true;
        }
        else if (cur_talk_msg.talk_para2 == 0 && cur_talk_msg.talk_id == TalkMsg.TALK_GIVE_TECH)
        {
            TechInfo techInfo = TechRes[cur_talk_msg.talk_para1];

            if (techInfo.max_tech_level == 1) // this tech only has one level
                return false;

            choice_question = "Which version?";

            int nationLevel = techInfo.get_nation_tech_level(techNationRecno);

            string[] verStrArray = { "Mark I", "Mark II", "Mark III" };

            for (int i = 1; i <= Math.Min(3, nationLevel); i++)
                add_talk_choice(verStrArray[i - 1], i);

            return true;
        }
        else
            return false;
    }

    private bool add_surrender_choices()
    {
        if (cur_talk_msg.talk_para1 != 0)
            return false;

        string kingName = NationArray[cur_talk_msg.to_nation_recno].king_name(true);
        choice_question = $"Do you really want to Surrender to {kingName}'s Kingdom?";

        add_talk_choice("Confirm.", 1);

        return true;
    }

    private bool add_request_surrender_choices()
    {
        if (cur_talk_msg.talk_para1 != 0)
            return false;

        choice_question = "How much do you offer?";

        string[] strArray = { "$5000.", "$7500.", "$10000.", "$15000.", "$20000.", "$30000.", "$40000.", "$50000." };

        int[] amtArray = { 5000, 7500, 10000, 15000, 20000, 30000, 40000, 50000 };

        for (int i = 0; i < strArray.Length; i++)
        {
            if (NationArray.player.cash >= amtArray[i])
            {
                add_talk_choice(strArray[i], amtArray[i] / 10); // divided by 10 to cope with the limit of <short>
            }
        }

        return true;
    }

    private void process_talk_msg()
    {
        foreach (TalkMsg talkMsg in talk_msg_array)
        {
            //--------------------------------------------------------//
            // If this is an AI message and there is no response from
            // the player after one month the message has been sent,
            // it presumes that the message has been rejected.
            //--------------------------------------------------------//

            if (NationArray[talkMsg.from_nation_recno].nation_type == NationBase.NATION_AI &&
                talkMsg.reply_type == REPLY_WAITING &&
                Info.game_date > talkMsg.date.AddDays(GameConstants.TALK_MSG_VALID_DAYS + 1))
            {
                talkMsg.reply_type = REPLY_REJECT;

                NationArray[talkMsg.from_nation_recno].ai_notify_reply(talkMsg.RecNo);
                //talkMsg = get_talk_msg(i); // in case talkMsg ptr was invalidated in resize
            }

            //--- delete the talk message after a year ---//

            if (Info.game_date > talkMsg.date.AddDays(GameConstants.TALK_MSG_KEEP_DAYS))
                del_talk_msg(talkMsg.RecNo);
        }
    }

    private void process_accepted_reply(TalkMsg talkMsg)
    {
        //---- delete duplicate message in reverse now this has been accepted ----//
        delete_msg_in_reverse(talkMsg); // do this now because talkMsg may be invalid after sending a reply

        Nation toNation = NationArray[talkMsg.to_nation_recno];
        Nation fromNation = NationArray[talkMsg.from_nation_recno];

        NationRelation fromRelation = fromNation.get_relation(talkMsg.to_nation_recno);
        NationRelation toRelation = toNation.get_relation(talkMsg.from_nation_recno);

        int goodRelationDec = 0; // whether the message is for requesting help.

        switch (talkMsg.talk_id)
        {
            case TalkMsg.TALK_PROPOSE_TRADE_TREATY:
                toNation.set_trade_treaty(talkMsg.from_nation_recno, true);
                break;

            case TalkMsg.TALK_PROPOSE_FRIENDLY_TREATY:
                toNation.form_friendly_treaty(talkMsg.from_nation_recno);
                break;

            case TalkMsg.TALK_PROPOSE_ALLIANCE_TREATY:
                toNation.form_alliance_treaty(talkMsg.from_nation_recno);
                break;

            case TalkMsg.TALK_END_TRADE_TREATY:
                toNation.set_trade_treaty(talkMsg.from_nation_recno, false);

                //---- set reject date on proposing treaty ----//

                fromRelation.last_talk_reject_date_array[TalkMsg.TALK_PROPOSE_TRADE_TREATY - 1] = Info.game_date;
                toRelation.last_talk_reject_date_array[TalkMsg.TALK_PROPOSE_TRADE_TREATY - 1] = Info.game_date;

                fromRelation.last_talk_reject_date_array[TalkMsg.TALK_END_TRADE_TREATY - 1] = Info.game_date;
                toRelation.last_talk_reject_date_array[TalkMsg.TALK_END_TRADE_TREATY - 1] = Info.game_date;

                //----- decrease reputation -----//

                if (toNation.reputation > 0)
                    fromNation.change_reputation(-toNation.reputation * 5.0 / 100.0);
                break;

            case TalkMsg.TALK_END_FRIENDLY_TREATY:
            case TalkMsg.TALK_END_ALLIANCE_TREATY:
                fromNation.end_treaty(talkMsg.to_nation_recno, NationBase.NATION_NEUTRAL);

                //---- set reject date on proposing treaty ----//
                //
                // If a friendly treaty is rejected, assuming an alliance treaty
                // is even more impossible. (thus set reject date on both friendly
                // and alliance treaties.)
                //
                // If a alliance treaty is rejected, only set reject date on
                // alliance treaty, it may still try proposing friendly treaty later.
                //
                //---------------------------------------------//

                fromRelation.last_talk_reject_date_array[TalkMsg.TALK_PROPOSE_FRIENDLY_TREATY - 1] = Info.game_date;
                toRelation.last_talk_reject_date_array[TalkMsg.TALK_PROPOSE_FRIENDLY_TREATY - 1] = Info.game_date;

                fromRelation.last_talk_reject_date_array[TalkMsg.TALK_PROPOSE_ALLIANCE_TREATY - 1] = Info.game_date;
                toRelation.last_talk_reject_date_array[TalkMsg.TALK_PROPOSE_ALLIANCE_TREATY - 1] = Info.game_date;
                break;

            case TalkMsg.TALK_REQUEST_MILITARY_AID:
                goodRelationDec = 10;
                break;

            case TalkMsg.TALK_REQUEST_TRADE_EMBARGO:
            {
                //--- send an end treaty message to the target kingdom ---//

                TalkMsg talkMsgReply = new TalkMsg();
                talkMsgReply.to_nation_recno = talkMsg.talk_para1;
                talkMsgReply.from_nation_recno = talkMsg.to_nation_recno;
                talkMsgReply.talk_id = TalkMsg.TALK_END_TRADE_TREATY;

                send_talk_msg(talkMsgReply, InternalConstants.COMMAND_AUTO);
                goodRelationDec = 4;
            }
                break;

            case TalkMsg.TALK_REQUEST_CEASE_WAR:
                UnitArray.stop_war_between(talkMsg.to_nation_recno, talkMsg.from_nation_recno);
                toNation.set_relation_status(talkMsg.from_nation_recno, NationBase.NATION_TENSE);

                fromRelation.last_talk_reject_date_array[TalkMsg.TALK_DECLARE_WAR - 1] = Info.game_date;
                toRelation.last_talk_reject_date_array[TalkMsg.TALK_DECLARE_WAR - 1] = Info.game_date;
                break;

            case TalkMsg.TALK_REQUEST_DECLARE_WAR:
                // the requesting nation must be at war with the enemy
                if (fromNation.get_relation_status(talkMsg.talk_para1) == NationBase.NATION_HOSTILE)
                {
                    TalkMsg talkMsgReply = new TalkMsg();
                    talkMsgReply.to_nation_recno = talkMsg.talk_para1;
                    talkMsgReply.from_nation_recno = talkMsg.to_nation_recno;
                    talkMsgReply.talk_id = TalkMsg.TALK_DECLARE_WAR;

                    //-- if we are currently allied or friendly with the nation, we need to terminate the friendly/alliance treaty first --//

                    if (toNation.get_relation_status(talkMsg.talk_para1) == NationBase.NATION_ALLIANCE)
                    {
                        TalkMsg breakTreatyMsg = new TalkMsg();
                        breakTreatyMsg.to_nation_recno = talkMsg.talk_para1;
                        breakTreatyMsg.from_nation_recno = talkMsg.to_nation_recno;
                        breakTreatyMsg.talk_id = TalkMsg.TALK_END_ALLIANCE_TREATY;

                        send_talk_msg(breakTreatyMsg, InternalConstants.COMMAND_AUTO);
                    }

                    else if (toNation.get_relation_status(talkMsg.talk_para1) == NationBase.NATION_FRIENDLY)
                    {
                        TalkMsg breakTreatyMsg = new TalkMsg();
                        breakTreatyMsg.to_nation_recno = talkMsg.talk_para1;
                        breakTreatyMsg.from_nation_recno = talkMsg.to_nation_recno;
                        breakTreatyMsg.talk_id = TalkMsg.TALK_END_FRIENDLY_TREATY;
                        send_talk_msg(breakTreatyMsg, InternalConstants.COMMAND_AUTO);
                    }

                    //--- send a declare war message to the target kingdom ---//

                    send_talk_msg(talkMsgReply, InternalConstants.COMMAND_AUTO);

                    //----------------------------------------------------//

                    goodRelationDec = 10;
                }

                break;

            case TalkMsg.TALK_REQUEST_BUY_FOOD:
            {
                int buyCost = talkMsg.talk_para1 * talkMsg.talk_para2 / 10;

                fromNation.add_food(talkMsg.talk_para1);
                fromNation.add_expense(NationBase.EXPENSE_IMPORTS, buyCost, false);
                toNation.consume_food(talkMsg.talk_para1);
                toNation.add_income(NationBase.INCOME_EXPORTS, buyCost, false);
                break;
            }

            case TalkMsg.TALK_DECLARE_WAR:
                // how many times this nation has started a war with us, the more the times the worse this nation is.
                toRelation.started_war_on_us_count++;
                toNation.set_relation_status(talkMsg.from_nation_recno, NationBase.NATION_HOSTILE);

                fromRelation.last_talk_reject_date_array[TalkMsg.TALK_REQUEST_CEASE_WAR - 1] = Info.game_date;
                toRelation.last_talk_reject_date_array[TalkMsg.TALK_REQUEST_CEASE_WAR - 1] = Info.game_date;

                //--- decrease reputation of the nation which declares war ---//

                if (toNation.reputation > 0)
                    fromNation.change_reputation(-toNation.reputation * 20.0 / 100.0);
                break;

            case TalkMsg.TALK_GIVE_TRIBUTE:
            case TalkMsg.TALK_GIVE_AID:
                fromNation.give_tribute(talkMsg.to_nation_recno, talkMsg.talk_para1);
                break;

            case TalkMsg.TALK_DEMAND_TRIBUTE:
            case TalkMsg.TALK_DEMAND_AID:
                toNation.give_tribute(talkMsg.from_nation_recno, talkMsg.talk_para1);
                goodRelationDec = talkMsg.talk_para1 / 200;
                break;

            case TalkMsg.TALK_GIVE_TECH:
                fromNation.give_tech(talkMsg.to_nation_recno, talkMsg.talk_para1, talkMsg.talk_para2);
                break;

            case TalkMsg.TALK_DEMAND_TECH:
                // get the latest tech version id. of the agreed nation.
                talkMsg.talk_para2 = TechRes[talkMsg.talk_para1].get_nation_tech_level(talkMsg.to_nation_recno);
                toNation.give_tech(talkMsg.from_nation_recno, talkMsg.talk_para1, talkMsg.talk_para2);
                goodRelationDec = talkMsg.talk_para2 * 3;
                break;

            case TalkMsg.TALK_REQUEST_SURRENDER:
            {
                // * 10 is to restore its original value. It has been divided by 10 to cope with the upper limit of <short>
                double offeredAmt = talkMsg.talk_para1 * 10.0;

                toNation.add_income(NationBase.INCOME_TRIBUTE, offeredAmt);
                fromNation.add_expense(NationBase.EXPENSE_TRIBUTE, offeredAmt);

                toNation.surrender(talkMsg.from_nation_recno);
                break;
            }

            case TalkMsg.TALK_SURRENDER:
                fromNation.surrender(talkMsg.to_nation_recno);
                break;
        }

        //---- if the nation accepts a message that is requesting help, then decrease its good_relation_duration_rating, so it won't accept so easily next time ---//

        if (goodRelationDec > 0)
            toRelation.good_relation_duration_rating -= goodRelationDec;
    }

    private void delete_msg_in_reverse(TalkMsg talkMsg)
    {
        TalkMsg talkMsgReverse = new TalkMsg();

        talkMsgReverse.from_nation_recno = talkMsg.to_nation_recno;
        talkMsgReverse.to_nation_recno = talkMsg.from_nation_recno;
        talkMsgReverse.talk_id = talkMsg.talk_id;

        while (true)
        {
            int talkMsgRecno = is_talk_msg_exist(talkMsgReverse, false); // don't check talk_para1 & talk_para2

            if (talkMsgRecno != 0)
                del_talk_msg(talkMsgRecno);
            else
                break;
        }
    }
}