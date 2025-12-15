namespace TenKingdoms;

public partial class Nation
{
    private void ThinkDiplomacy()
    {
        ProcessTalkMessages();
        
        ThinkTradeTreaty();
    }

    private void ThinkTradeTreaty()
    {
        foreach (Nation otherNation in NationArray)
        {
            if (otherNation.nation_recno == nation_recno)
                continue;

            NationRelation ourRelation = get_relation(otherNation.nation_recno);
            if (!ourRelation.has_contact)
                continue;

            bool exists = false;
            for (int i = 0; i < TalkRes.talk_msg_array.Count; i++)
            {
                TalkMsg message = TalkRes.talk_msg_array[i];
                if (message.from_nation_recno == nation_recno && message.to_nation_recno == otherNation.nation_recno &&
                    message.talk_id == TalkMsg.TALK_PROPOSE_TRADE_TREATY)
                {
                    exists = true;
                }
            }

            if (!ourRelation.trade_treaty && !exists)
            {
                TalkRes.ai_send_talk_msg(otherNation.nation_recno, nation_recno, TalkMsg.TALK_PROPOSE_TRADE_TREATY, 0, 0, true);
            }
        }
    }

    private void ProcessTalkMessages()
    {
        for (int i = TalkRes.talk_msg_array.Count - 1; i >= 0; i--)
        {
            TalkMsg message = TalkRes.talk_msg_array[i];
            if (message.to_nation_recno != nation_recno)
                continue;

            if (message.reply_type == TalkRes.REPLY_WAITING)
            {
                TalkRes.reply_talk_msg(message.RecNo, TalkRes.REPLY_ACCEPT, InternalConstants.COMMAND_AI);
                TalkRes.del_talk_msg(message.RecNo);
            }
        }
    }
}