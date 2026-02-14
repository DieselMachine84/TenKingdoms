namespace TenKingdoms;

public partial class NationNew
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
            if (otherNation.NationId == NationId)
                continue;

            NationRelation ourRelation = GetRelation(otherNation.NationId);
            if (!ourRelation.HasContact)
                continue;

            bool exists = false;
            for (int i = 0; i < TalkRes.TalkMessages.Count; i++)
            {
                TalkMsg message = TalkRes.TalkMessages[i];
                if (message.FromNationId == NationId && message.ToNationId == otherNation.NationId &&
                    message.TalkId == TalkMsg.TALK_PROPOSE_TRADE_TREATY)
                {
                    exists = true;
                }
            }

            if (!ourRelation.TradeTreaty && !exists)
            {
                TalkRes.AISendTalkMsg(otherNation.NationId, NationId, TalkMsg.TALK_PROPOSE_TRADE_TREATY, 0, 0, true);
            }
        }
    }

    private void ProcessTalkMessages()
    {
        for (int i = TalkRes.TalkMessages.Count - 1; i >= 0; i--)
        {
            TalkMsg message = TalkRes.TalkMessages[i];
            if (message.ToNationId != NationId)
                continue;

            if (message.ReplyType == TalkRes.REPLY_WAITING)
            {
                TalkRes.ReplyTalkMsg(message.Id, TalkRes.REPLY_ACCEPT, InternalConstants.COMMAND_AI);
                TalkRes.DelTalkMsg(message.Id);
            }
        }
    }
}