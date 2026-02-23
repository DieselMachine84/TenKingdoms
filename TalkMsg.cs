using System;
using System.IO;

namespace TenKingdoms;

public class TalkMsg
{
	public const int TALK_PROPOSE_TRADE_TREATY = 1;
	public const int TALK_PROPOSE_FRIENDLY_TREATY = 2;
	public const int TALK_PROPOSE_ALLIANCE_TREATY = 3;
	public const int TALK_END_TRADE_TREATY = 4;
	public const int TALK_END_FRIENDLY_TREATY = 5;
	public const int TALK_END_ALLIANCE_TREATY = 6;
	public const int TALK_REQUEST_MILITARY_AID = 7;
	public const int TALK_REQUEST_TRADE_EMBARGO = 8;
	public const int TALK_REQUEST_CEASE_WAR = 9;
	public const int TALK_REQUEST_DECLARE_WAR = 10;
	public const int TALK_REQUEST_BUY_FOOD = 11;
	public const int TALK_DECLARE_WAR = 12;
	public const int TALK_GIVE_TRIBUTE = 13;
	public const int TALK_DEMAND_TRIBUTE = 14;
	public const int TALK_GIVE_AID = 15;
	public const int TALK_DEMAND_AID = 16;
	public const int TALK_GIVE_TECH = 17;
	public const int TALK_DEMAND_TECH = 18;
	public const int TALK_REQUEST_SURRENDER = 19;
	public const int TALK_SURRENDER = 20;
	public const int MAX_TALK_TYPE = 20;
	public const int MAX_TALK_CHOICE = MAX_TALK_TYPE;

	public int Id { get; set; }

	public int TalkId { get; set; }
	public int FromNationId { get; set; }
	public int ToNationId { get; set; }
	public int TalkParam1 { get; set; }
	public int TalkParam2 { get; set; }

	public DateTime Date { get; set; }
	public int ReplyType { get; set; }
	public DateTime ReplyDate { get; set; }

	public int RelationStatus { get; set; } // the relation status of the two nations when the message is sent

	private static readonly bool[] TalkMsgReplyNeeded =
	{
		true, true, true, false, false, false, true, true, true, true,
		true, false, true, true, true, true, true, true, true, false
	};

	private TalkRes TalkRes => Sys.Instance.TalkRes;
	private NationArray NationArray => Sys.Instance.NationArray;
	private Info Info => Sys.Instance.Info;

	public TalkMsg()
	{
	}

	public TalkMsg(TalkMsg other) : this()
	{
		TalkId = other.TalkId;
		FromNationId = other.FromNationId;
		ToNationId = other.ToNationId;
		TalkParam1 = other.TalkParam1;
		TalkParam2 = other.TalkParam2;
	}
	
	private string FromNationName()
	{
		return NationArray[FromNationId].KingName(true);
	}

	private string ToNationName()
	{
		return NationArray[ToNationId].KingName(true);
	}

	private string Param1NationName()
	{
		return NationArray[TalkParam1].KingName(true);
	}

	private string FromKingName()
	{
		string str = NationArray[FromNationId].KingName();

		if (TalkRes.AddNationColor)
			str += " @COL" + Convert.ToChar(48 + NationArray[FromNationId].ColorSchemeId);

		return str;
	}

	private string ToKingName()
	{
		string str = NationArray[ToNationId].KingName();

		if (TalkRes.AddNationColor)
			str += " @COL" + Convert.ToChar(48 + NationArray[ToNationId].ColorSchemeId);

		return str;
	}

	private string NationColorStr1(int nationId)
	{
		return " @COL" + Convert.ToChar(48 + NationArray[nationId].ColorSchemeId);
	}

	private string NationColorStr2(int nationId)
	{
		return TalkRes.AddNationColor ? " @COL" + Convert.ToChar(48 + NationArray[nationId].ColorSchemeId) : String.Empty;
	}

	private string TechName(int techId)
	{
		return techId switch
		{
			1 => "Catapult",
			2 => "Porcupine",
			3 => "Ballista",
			4 => "Cannon",
			5 => "Spitfire",
			6 => "Caravel",
			7 => "Galleon",
			8 => "Unicorn",
			_ => "Unknown"
		};
	}
	
	public string Message(int viewingNationId, bool displayReply = true, bool displaySecondLine = false)
	{
		return TalkId switch
		{
			TALK_PROPOSE_TRADE_TREATY => ProposeTreaty(TALK_PROPOSE_TRADE_TREATY, viewingNationId, displayReply),
			TALK_PROPOSE_FRIENDLY_TREATY => ProposeTreaty(TALK_PROPOSE_FRIENDLY_TREATY, viewingNationId, displayReply),
			TALK_PROPOSE_ALLIANCE_TREATY => ProposeTreaty(TALK_PROPOSE_ALLIANCE_TREATY, viewingNationId, displayReply),
			TALK_END_TRADE_TREATY => EndTreaty(TALK_END_TRADE_TREATY, viewingNationId),
			TALK_END_FRIENDLY_TREATY => EndTreaty(TALK_END_FRIENDLY_TREATY, viewingNationId),
			TALK_END_ALLIANCE_TREATY => EndTreaty(TALK_END_FRIENDLY_TREATY, viewingNationId),
			TALK_REQUEST_MILITARY_AID => RequestMilitaryAid(viewingNationId, displayReply),
			TALK_REQUEST_TRADE_EMBARGO => RequestTradeEmbargo(viewingNationId, displayReply),
			TALK_REQUEST_CEASE_WAR => RequestCeaseWar(viewingNationId, displayReply),
			TALK_REQUEST_DECLARE_WAR => RequestDeclareWar(viewingNationId, displayReply),
			TALK_REQUEST_BUY_FOOD => RequestBuyFood(viewingNationId, displayReply, displaySecondLine),
			TALK_DECLARE_WAR => DeclareWar(viewingNationId),
			TALK_GIVE_TRIBUTE => GiveTribute(viewingNationId, displayReply, false),
			TALK_DEMAND_TRIBUTE => DemandTribute(viewingNationId, displayReply, false),
			TALK_GIVE_AID => GiveTribute(viewingNationId, displayReply, true),
			TALK_DEMAND_AID => DemandTribute(viewingNationId, displayReply, true),
			TALK_GIVE_TECH => GiveTech(viewingNationId, displayReply),
			TALK_DEMAND_TECH => DemandTech(viewingNationId, displayReply),
			TALK_REQUEST_SURRENDER => RequestSurrender(viewingNationId, displayReply),
			TALK_SURRENDER => Surrender(viewingNationId),
			_ => "Unknown talk type"
		};
	}

	public bool IsReplyNeeded()
	{
		return TalkMsgReplyNeeded[TalkId - 1];
	}

	// can specify an additional nationId that is to be considered invalid.
	public bool IsValidToDisplay(int invalidNationId = 0)
	{
		//--- check if the nations are still there -----//

		if (NationArray.IsDeleted(FromNationId) || (invalidNationId != 0 && FromNationId == invalidNationId))
			return false;

		if (NationArray.IsDeleted(ToNationId) || (invalidNationId != 0 && ToNationId == invalidNationId))
			return false;

		//--------------------------------------//

		switch (TalkId)
		{
			case TALK_REQUEST_TRADE_EMBARGO:
			case TALK_REQUEST_DECLARE_WAR:
				if (NationArray.IsDeleted(TalkParam1) || (invalidNationId != 0 && TalkParam1 == invalidNationId))
					return false;
				break;
		}

		return true;
	}

	public bool IsValidToReply()
	{
		//-----------------------------------------------------//
		// When a diplomatic message is sent, the receiver
		// must reply the message with a month.
		//-----------------------------------------------------//

		if (Info.GameDate > Date.AddDays(GameConstants.TALK_MSG_VALID_DAYS))
			return false;

		//--- check if the nations are still there -----//

		if (NationArray.IsDeleted(FromNationId))
			return false;

		if (NationArray.IsDeleted(ToNationId))
			return false;

		//--------------------------------------//

		if (!TalkRes.CanSendMsg(ToNationId, FromNationId, TalkId))
			return false;

		//--------------------------------------//

		Nation fromNation = NationArray[FromNationId];
		Nation toNation = NationArray[ToNationId];

		switch (TalkId)
		{
			case TALK_REQUEST_TRADE_EMBARGO:
				if (NationArray.IsDeleted(TalkParam1))
					return false;

				//-- if the requesting nation is itself trading with the target nation --//

				if (fromNation.GetRelation(TalkParam1).TradeTreaty)
					return false;

				//-- or if the requested nation already doesn't have a trade treaty with the nation --//

				if (!toNation.GetRelation(TalkParam1).TradeTreaty)
					return false;

				break;

			case TALK_REQUEST_DECLARE_WAR:
				if (NationArray.IsDeleted(TalkParam1))
					return false;

				//-- if the requesting nation is no longer hostile with the nation --//

				if (fromNation.GetRelationStatus(TalkParam1) != NationBase.NATION_HOSTILE)
					return false;

				//-- or if the requested nation has become hostile with the nation --//

				if (toNation.GetRelationStatus(TalkParam1) == NationBase.NATION_HOSTILE)
					return false;

				break;

			case TALK_REQUEST_BUY_FOOD:
				return fromNation.Cash >= TalkParam1 * TalkParam2 / 10.0;

			case TALK_GIVE_TRIBUTE:
			case TALK_GIVE_AID:
				return fromNation.Cash >= TalkParam1;

			case TALK_GIVE_TECH:
				// still display the message even if the nation already has the technology
				break;

			case TALK_DEMAND_TECH:
				// still display the message even if the nation already has the technology
				break;
		}

		return true;
	}

	public bool CanAccept()
	{
		Nation toNation = NationArray[ToNationId];

		switch (TalkId)
		{
			case TALK_REQUEST_BUY_FOOD:
				return toNation.Food >= TalkParam1;

			case TALK_DEMAND_TRIBUTE:
			case TALK_DEMAND_AID:
				return toNation.Cash >= TalkParam1;

			case TALK_DEMAND_TECH: // the requested nation has the technology
				return toNation.GetTechLevel(TalkParam1) > 0;
		}

		return true;
	}

	private string ProposeTreaty(int treatyType, int viewingNationId, bool shouldDisplayReply)
	{
		if (ReplyType == TalkRes.REPLY_WAITING || !shouldDisplayReply)
		{
			if (viewingNationId == FromNationId)
			{
				if (treatyType == TALK_PROPOSE_TRADE_TREATY)
				{
					return $"You propose a trade treaty to {ToNationName()}'s Kingdom.{NationColorStr2(ToNationId)}";
				}

				if (treatyType == TALK_PROPOSE_FRIENDLY_TREATY)
				{
					return $"You propose a friendly treaty to {ToNationName()}'s Kingdom.{NationColorStr2(ToNationId)}";
				}

				if (treatyType == TALK_PROPOSE_ALLIANCE_TREATY)
				{
					return $"You propose an alliance treaty to {ToNationName()}'s Kingdom.{NationColorStr2(ToNationId)}";
				}
			}
			else
			{
				if (treatyType == TALK_PROPOSE_TRADE_TREATY)
				{
					return $"{FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} proposes a trade treaty to you.";
				}

				if (treatyType == TALK_PROPOSE_FRIENDLY_TREATY)
				{
					return $"{FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} proposes a friendly treaty to you.";
				}

				if (treatyType == TALK_PROPOSE_ALLIANCE_TREATY)
				{
					return $"{FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} proposes an alliance treaty to you.";
				}
			}
		}
		else
		{
			if (viewingNationId == FromNationId)
			{
				if (ReplyType == TalkRes.REPLY_ACCEPT)
				{
					if (treatyType == TALK_PROPOSE_TRADE_TREATY)
					{
						return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} accepts your proposed trade treaty.";
					}

					if (treatyType == TALK_PROPOSE_FRIENDLY_TREATY)
					{
						return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} accepts your proposed friendly treaty.";
					}

					if (treatyType == TALK_PROPOSE_ALLIANCE_TREATY)
					{
						return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} accepts your proposed alliance treaty.";
					}
				}
				else
				{
					if (treatyType == TALK_PROPOSE_TRADE_TREATY)
					{
						return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} rejects your proposed trade treaty.";
					}

					if (treatyType == TALK_PROPOSE_FRIENDLY_TREATY)
					{
						return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} rejects your proposed friendly treaty.";
					}

					if (treatyType == TALK_PROPOSE_ALLIANCE_TREATY)
					{
						return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} rejects your proposed alliance treaty.";
					}
				}
			}
			else
			{
				if (ReplyType == TalkRes.REPLY_ACCEPT)
				{
					if (treatyType == TALK_PROPOSE_TRADE_TREATY)
					{
						return $"You accept the trade treaty proposed by {FromNationName()}'s Kingdom.{NationColorStr2(FromNationId)}";
					}

					if (treatyType == TALK_PROPOSE_FRIENDLY_TREATY)
					{
						return $"You accept the friendly treaty proposed by {FromNationName()}'s Kingdom.{NationColorStr2(FromNationId)}";
					}

					if (treatyType == TALK_PROPOSE_ALLIANCE_TREATY)
					{
						return $"You accept the alliance treaty proposed by {FromNationName()}'s Kingdom.{NationColorStr2(FromNationId)}";
					}
				}
				else
				{
					if (treatyType == TALK_PROPOSE_TRADE_TREATY)
					{
						return $"You reject the trade treaty proposed by {FromNationName()}'s Kingdom.{NationColorStr2(FromNationId)}";
					}

					if (treatyType == TALK_PROPOSE_FRIENDLY_TREATY)
					{
						return $"You reject the friendly treaty proposed by {FromNationName()}'s Kingdom.{NationColorStr2(FromNationId)}";
					}

					if (treatyType == TALK_PROPOSE_ALLIANCE_TREATY)
					{
						return $"You reject the alliance treaty proposed by {FromNationName()}'s Kingdom.{NationColorStr2(FromNationId)}";
					}
				}
			}
		}

		return "Unknown talk type";
	}

	private string EndTreaty(int treatyType, int viewingNationId)
	{
		if (viewingNationId == FromNationId)
		{
			if (treatyType == TALK_END_TRADE_TREATY)
			{
				return $"You terminate your trade treaty with {ToNationName()}'s Kingdom.{NationColorStr2(ToNationId)}";
			}

			if (treatyType == TALK_END_FRIENDLY_TREATY)
			{
				return $"You terminate your friendly treaty with {ToNationName()}'s Kingdom.{NationColorStr2(ToNationId)}";
			}

			if (treatyType == TALK_END_ALLIANCE_TREATY)
			{
				return $"You terminate your alliance treaty with {ToNationName()}'s Kingdom.{NationColorStr2(ToNationId)}";
			}
		}
		else
		{
			if (treatyType == TALK_END_TRADE_TREATY)
			{
				return $"{FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} terminates its trade treaty with you.";
			}

			if (treatyType == TALK_END_FRIENDLY_TREATY)
			{
				return $"{FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} terminates its friendly treaty with you.";
			}

			if (treatyType == TALK_END_ALLIANCE_TREATY)
			{
				return $"{FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} terminates its alliance treaty with you.";
			}
		}

		return "Unknown talk type";
	}

	private string RequestMilitaryAid(int viewingNationId, bool shouldDisplayReply)
	{
		if (ReplyType == TalkRes.REPLY_WAITING || !shouldDisplayReply)
		{
			if (viewingNationId == FromNationId)
				return $"You request immediate military aid from {ToNationName()}'s Kingdom.{NationColorStr2(ToNationId)}";
			else
				return $"{FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} requests immediate military aid from you.";
		}
		else
		{
			if (viewingNationId == FromNationId)
			{
				if (ReplyType == TalkRes.REPLY_ACCEPT)
					return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} agrees to immediately send your requested military aid.";
				else
					return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} denies you your requested military aid.";
			}
			else
			{
				if (ReplyType == TalkRes.REPLY_ACCEPT)
					return $"You agree to immediately send military aid to {FromNationName()}'s Kingdom.{NationColorStr2(FromNationId)}";
				else
					return $"You refuse to send military aid to {FromNationName()}'s Kingdom.{NationColorStr2(FromNationId)}";
			}
		}
	}

	private string RequestTradeEmbargo(int viewingNationId, bool shouldDisplayReply)
	{
		if (ReplyType == TalkRes.REPLY_WAITING || !shouldDisplayReply)
		{
			if (viewingNationId == FromNationId)
				return $"You request {ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} to join an embargo on trade with {Param1NationName()}'s Kingdom.{NationColorStr1(TalkParam1)}";
			else
				return $"{FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} requests you to join an embargo on trade with {Param1NationName()}'s Kingdom.{NationColorStr1(TalkParam1)}";
		}
		else
		{
			if (viewingNationId == FromNationId)
			{
				if (ReplyType == TalkRes.REPLY_ACCEPT)
					return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} agrees to join an embargo on trade with {Param1NationName()}'s Kingdom.{NationColorStr1(TalkParam1)}";
				else
					return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} refuses to join an embargo on trade with {Param1NationName()}'s Kingdom.{NationColorStr1(TalkParam1)}";
			}
			else
			{
				if (ReplyType == TalkRes.REPLY_ACCEPT)
					return $"You agree to join an embargo on trade with {Param1NationName()}'s Kingdom{NationColorStr1(TalkParam1)} as requested by {FromNationName()}'s Kingdom.{NationColorStr2(FromNationId)}";
				else
					return $"You refuse to join an embargo on trade with {Param1NationName()}'s Kingdom{NationColorStr1(TalkParam1)} as requested by {FromNationName()}'s Kingdom.{NationColorStr2(FromNationId)}";
			}
		}
	}

	private string RequestCeaseWar(int viewingNationId, bool shouldDisplayReply)
	{
		if (ReplyType == TalkRes.REPLY_WAITING || !shouldDisplayReply)
		{
			if (viewingNationId == FromNationId)
				return $"You request a cease-fire with {ToNationName()}'s Kingdom.{NationColorStr2(ToNationId)}";
			else
				return $"{FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} requests a cease-fire.";
		}
		else
		{
			if (viewingNationId == FromNationId)
			{
				if (ReplyType == TalkRes.REPLY_ACCEPT)
					return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} agrees to a cease-fire.";
				else
					return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} refuses a cease-fire.";
			}
			else
			{
				if (ReplyType == TalkRes.REPLY_ACCEPT)
					return $"You agree to a cease-fire with {FromNationName()}'s Kingdom.{NationColorStr2(FromNationId)}";
				else
					return $"You refuse a cease-fire with {FromNationName()}'s Kingdom.{NationColorStr2(FromNationId)}";
			}
		}
	}

	private string RequestDeclareWar(int viewingNationId, bool shouldDisplayReply)
	{
		if (ReplyType == TalkRes.REPLY_WAITING || !shouldDisplayReply)
		{
			if (viewingNationId == FromNationId)
				return $"You request {ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} to declare war on {Param1NationName()}'s Kingdom.{NationColorStr1(TalkParam1)}";
			else
				return $"{FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} requests that you declare war on {Param1NationName()}'s Kingdom.{NationColorStr1(TalkParam1)}";
		}
		else
		{
			if (viewingNationId == FromNationId)
			{
				if (ReplyType == TalkRes.REPLY_ACCEPT)
					return
						$"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} agrees to declare war on {Param1NationName()}'s Kingdom.{NationColorStr1(TalkParam1)}";
				else
					return
						$"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} refuses to declare war on {Param1NationName()}'s Kingdom.{NationColorStr1(TalkParam1)}";
			}
			else
			{
				if (ReplyType == TalkRes.REPLY_ACCEPT)
					return $"You agree to declare war on {Param1NationName()}'s Kingdom.{NationColorStr1(TalkParam1)}";
				else
					return $"You refuse to declare war on {Param1NationName()}'s Kingdom.{NationColorStr1(TalkParam1)}";
			}
		}
	}

	private string RequestBuyFood(int viewingNationId, bool displayReply, bool displaySecondLine)
	{
		if (displaySecondLine)
		{
			return $"{FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} offers ${TalkParam2} for 10 units of food.";
		}

		if (ReplyType == TalkRes.REPLY_WAITING || !displayReply)
		{
			if (viewingNationId == FromNationId)
				return $"You request to purchase {TalkParam1} units of food from {ToNationName()}'s Kingdom.{NationColorStr2(ToNationId)}";
			else
				return $"{FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} requests to purchase {TalkParam1} units of food from you.";
		}
		else
		{
			if (viewingNationId == FromNationId)
			{
				if (ReplyType == TalkRes.REPLY_ACCEPT)
					return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} agrees to sell {TalkParam1} units of food to you.";
				else
					return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} refuses to sell {TalkParam1} units of food to you.";
			}
			else
			{
				if (ReplyType == TalkRes.REPLY_ACCEPT)
					return $"You agree to sell {TalkParam1} units of food to {FromNationName()}'s Kingdom.{NationColorStr2(FromNationId)}";
				else
					return $"You refuse to sell {TalkParam1} units of food to {FromNationName()}'s Kingdom.{NationColorStr2(FromNationId)}";
			}
		}
	}

	private string DeclareWar(int viewingNationId)
	{
		if (viewingNationId == FromNationId)
			return $"You declare war on {FromNationName()}'s Kingdom.{NationColorStr2(FromNationId)}";
		else
			return $"{FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} declares war on you.";
	}

	private string GiveTribute(int viewingNationId, bool displayReply, bool isAid)
	{
		if (ReplyType == TalkRes.REPLY_WAITING || !displayReply)
		{
			if (viewingNationId == FromNationId)
			{
				if (isAid)
					return $"You offer {ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} ${TalkParam1} in aid.";
				else
					return $"You offer {ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} ${TalkParam1} in tribute.";
			}
			else
			{
				if (isAid)
					return $"{FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} offers you ${TalkParam1} in aid.";
				else
					return $"{FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} offers you ${TalkParam1} in tribute.";
			}
		}
		else
		{
			if (viewingNationId == FromNationId)
			{
				if (ReplyType == TalkRes.REPLY_ACCEPT)
				{
					if (isAid)
						return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} accepts your aid of ${TalkParam1}.";
					else
						return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} accepts your tribute of ${TalkParam1}.";
				}
				else
				{
					if (isAid)
						return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} rejects your aid of ${TalkParam1}.";
					else
						return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} rejects your tribute of ${TalkParam1}.";
				}
			}
			else
			{
				if (ReplyType == TalkRes.REPLY_ACCEPT)
				{
					if (isAid)
						return $"You accept the {FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} aid of ${TalkParam1}.";
					else
						return $"You accept the {FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} tribute of ${TalkParam1}.";
				}
				else
				{
					if (isAid)
						return $"You reject the {FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} aid of ${TalkParam1}.";
					else
						return $"You reject the {FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} tribute of ${TalkParam1}.";
				}
			}
		}
	}

	private string DemandTribute(int viewingNationId, bool displayReply, bool isAid)
	{
		if (ReplyType == TalkRes.REPLY_WAITING || !displayReply)
		{
			if (viewingNationId == FromNationId)
			{
				if (isAid)
					return $"You request ${TalkParam1} in aid from {ToNationName()}'s Kingdom.{NationColorStr2(ToNationId)}";
				else
					return $"You demand ${TalkParam1} in tribute from {ToNationName()}'s Kingdom.{NationColorStr2(ToNationId)}";
			}
			else
			{
				if (isAid)
					return $"{FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} requests ${TalkParam1} in aid from you.";
				else
					return $"{FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} demands ${TalkParam1} in tribute from you.";
			}
		}
		else
		{
			if (viewingNationId == FromNationId)
			{
				if (ReplyType == TalkRes.REPLY_ACCEPT)
				{
					if (isAid)
						return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} agrees to give you ${TalkParam1} in aid.";
					else
						return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} agrees to pay you ${TalkParam1} in tribute.";
				}
				else
				{
					if (isAid)
						return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} refuses to give you ${TalkParam1} in aid.";
					else
						return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} refuses to pay you ${TalkParam1} in tribute.";
				}
			}
			else
			{
				if (ReplyType == TalkRes.REPLY_ACCEPT)
				{
					if (isAid)
						return $"You agree to give {FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} ${TalkParam1} in aid.";
					else
						return $"You agree to pay {FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} ${TalkParam1} in tribute.";
				}
				else
				{
					if (isAid)
						return $"You refuse to give {FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} ${TalkParam1} in aid.";
					else
						return $"You refuse to pay {FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} ${TalkParam1} in tribute.";
				}
			}
		}
	}

	private string GiveTech(int viewingNationId, bool displayReply)
	{
		string version = string.Empty;
		if (TalkParam2 != 0) // Ships do not have different versions
			version += " " + Misc.RomanNumber(TalkParam2);

		if (ReplyType == TalkRes.REPLY_WAITING || !displayReply)
		{
			if (viewingNationId == FromNationId)
				return $"You offer {TechName(TalkParam1)}{version} technology to {ToNationName()}'s Kingdom.{NationColorStr2(ToNationId)}";
			else
				return $"{FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} offers {TechName(TalkParam1)}{version} technology to you.";
		}
		else
		{
			if (viewingNationId == FromNationId)
			{
				if (ReplyType == TalkRes.REPLY_ACCEPT)
					return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} accepts your gift of {TechName(TalkParam1)}{version} technology.";
				else
					return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} rejects your gift of {TechName(TalkParam1)}{version} technology.";
			}
			else
			{
				if (ReplyType == TalkRes.REPLY_ACCEPT)
					return $"You accept the gift of {TechName(TalkParam1)}{version} technology from {FromNationName()}'s Kingdom.{NationColorStr2(FromNationId)}";
				else
					return $"You reject the gift of {TechName(TalkParam1)}{version} technology from {FromNationName()}'s Kingdom.{NationColorStr2(FromNationId)}";
			}
		}
	}

	private string DemandTech(int viewingNationId, bool displayReply)
	{
		bool friendlyRequest = !NationArray.IsDeleted(FromNationId) &&
		                       NationArray[FromNationId].GetRelationStatus(ToNationId) >= NationBase.NATION_FRIENDLY;

		if (ReplyType == TalkRes.REPLY_WAITING || !displayReply)
		{
			if (viewingNationId == FromNationId)
			{
				if (friendlyRequest)
					return $"You request the latest {TechName(TalkParam1)} technology from {ToNationName()}'s Kingdom.{NationColorStr2(ToNationId)}";
				else
					return $"You demand the latest {TechName(TalkParam1)} technology from {ToNationName()}'s Kingdom.{NationColorStr2(ToNationId)}";
			}
			else
			{
				if (friendlyRequest)
					return $"{FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} requests the latest {TechName(TalkParam1)} technology from you.";
				else
					return $"{FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} demands the latest {TechName(TalkParam1)} technology from you.";
			}
		}
		else
		{
			if (viewingNationId == FromNationId)
			{
				if (ReplyType == TalkRes.REPLY_ACCEPT)
					return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} agrees to transfer its latest {TechName(TalkParam1)} technology to you.";
				else
					return $"{ToNationName()}'s Kingdom{NationColorStr2(ToNationId)} refuses to transfer its latest {TechName(TalkParam1)} technology to you.";
			}
			else
			{
				if (ReplyType == TalkRes.REPLY_ACCEPT)
					return $"You agree to transfer your latest {TechName(TalkParam1)} technology to {FromNationName()}'s Kingdom.{NationColorStr2(FromNationId)}";
				else
					return $"You refuse to transfer your latest {TechName(TalkParam1)} technology to {FromNationName()}'s Kingdom.{NationColorStr2(FromNationId)}";
			}
		}
	}

	private string RequestSurrender(int viewingNationId, bool displayReply)
	{
		if (ReplyType == TalkRes.REPLY_WAITING || !displayReply)
		{
			if (viewingNationId == FromNationId)
				return $"You offer ${TalkParam1 * 10} for the throne of {ToNationName()}'s Kingdom.{NationColorStr2(ToNationId)}";
			else
				return $"To unite our two Kingdoms under his rule, King {FromKingName()} offers ${TalkParam1 * 10} for your throne.";
		}
		else
		{
			if (viewingNationId == FromNationId)
			{
				if (ReplyType == TalkRes.REPLY_ACCEPT)
					return $"King {ToKingName()} agrees to take your money in exchange for his throne.";
				else
					return $"King {ToKingName()} refuses to dishonor himself by selling his throne!";
			}
			else
			{
				if (ReplyType == TalkRes.REPLY_ACCEPT)
					return "You agree to take the money in exchange for your throne.";
				else
					return $"You refuse to dishonor yourself by selling your throne to {FromNationName()}'s Kingdom.{NationColorStr2(FromNationId)}";
			}
		}
	}

	private string Surrender(int viewingNationId)
	{
		if (viewingNationId == FromNationId)
			return $"You have surrendered to {ToNationName()}'s Kingdom.{NationColorStr2(ToNationId)}";
		else
			return $"{FromNationName()}'s Kingdom{NationColorStr2(FromNationId)} has surrendered to you.";
	}
	
	#region SaveAndLoad

	public void SaveTo(BinaryWriter writer)
	{
		writer.Write(Id);
		writer.Write(TalkId);
		writer.Write(FromNationId);
		writer.Write(ToNationId);
		writer.Write(TalkParam1);
		writer.Write(TalkParam2);
		writer.Write(Date.ToBinary());
		writer.Write(ReplyType);
		writer.Write(ReplyDate.ToBinary());
		writer.Write(RelationStatus);
	}

	public void LoadFrom(BinaryReader reader)
	{
		Id = reader.ReadInt32();
		TalkId = reader.ReadInt32();
		FromNationId = reader.ReadInt32();
		ToNationId = reader.ReadInt32();
		TalkParam1 = reader.ReadInt32();
		TalkParam2 = reader.ReadInt32();
		Date = DateTime.FromBinary(reader.ReadInt64());
		ReplyType = reader.ReadInt32();
		ReplyDate = DateTime.FromBinary(reader.ReadInt64());
		RelationStatus = reader.ReadInt32();
	}
	
	#endregion
}