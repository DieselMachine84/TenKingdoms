using System;

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

	public int RecNo;

	public int talk_id;
	public int talk_para1;
	public int talk_para2;

	public DateTime date;

	public int from_nation_recno;
	public int to_nation_recno;

	public int reply_type; // whether this is a reply message.
	public DateTime reply_date;

	public int relation_status; // the relation status of the two nations when the message is sent

	private static int lastRecNo = 0;

	public static bool[] talk_msg_reply_needed_array =
	{
		true, true, true, false, false, false, true, true, true, true,
		true, false, true, true, true, true, true, true, true, false
	};

	public static int viewing_nation_recno;
	public static bool should_disp_reply;
	public static bool disp_second_line;

	private TechRes TechRes => Sys.Instance.TechRes;
	private TalkRes TalkRes => Sys.Instance.TalkRes;
	private Info Info => Sys.Instance.Info;
	private NationArray NationArray => Sys.Instance.NationArray;

	public TalkMsg()
	{
		lastRecNo++;
		RecNo = lastRecNo;
	}
	
	public string from_nation_name()
	{
		return NationArray[from_nation_recno].king_name(true);
	}

	public string to_nation_name()
	{
		return NationArray[to_nation_recno].king_name(true);
	}

	public string para1_nation_name()
	{
		return NationArray[talk_para1].king_name(true);
	}

	public string from_king_name()
	{
		string str = NationArray[from_nation_recno].king_name();

		if (TalkRes.msg_add_nation_color)
			str += " @COL" + Convert.ToChar(30 + NationArray[from_nation_recno].color_scheme_id);

		return str;
	}

	public string to_king_name()
	{
		string str = NationArray[to_nation_recno].king_name();

		if (TalkRes.msg_add_nation_color)
			str += " @COL" + Convert.ToChar(30 + NationArray[to_nation_recno].color_scheme_id);

		return str;
	}

	public string nation_color_code_str(int nationRecno)
	{
		return " @COL" + Convert.ToChar(30 + NationArray[nationRecno].color_scheme_id);
	}

	public string nation_color_code_str2(int nationRecno)
	{
		if (TalkRes.msg_add_nation_color)
			return " @COL" + Convert.ToChar(30 + NationArray[nationRecno].color_scheme_id);
		else
			return String.Empty;
	}

	public string msg_str(int viewingNationRecno, bool dispReply = true, bool dispSecondLine = false)
	{
		viewing_nation_recno = viewingNationRecno;
		should_disp_reply = dispReply;
		disp_second_line = dispSecondLine;

		//-------- compose the message str -------//

		switch (talk_id)
		{
			case TALK_PROPOSE_TRADE_TREATY:
				propose_treaty(TALK_PROPOSE_TRADE_TREATY);
				break;

			case TALK_PROPOSE_FRIENDLY_TREATY:
				propose_treaty(TALK_PROPOSE_FRIENDLY_TREATY);
				break;

			case TALK_PROPOSE_ALLIANCE_TREATY:
				propose_treaty(TALK_PROPOSE_ALLIANCE_TREATY);
				break;

			case TALK_END_TRADE_TREATY:
				end_treaty(TALK_END_TRADE_TREATY);
				break;

			case TALK_END_FRIENDLY_TREATY:
				end_treaty(TALK_END_FRIENDLY_TREATY);
				break;

			case TALK_END_ALLIANCE_TREATY:
				end_treaty(TALK_END_FRIENDLY_TREATY);
				break;

			case TALK_REQUEST_MILITARY_AID:
				request_military_aid();
				break;

			case TALK_REQUEST_TRADE_EMBARGO:
				request_trade_embargo();
				break;

			case TALK_REQUEST_CEASE_WAR:
				request_cease_war();
				break;

			case TALK_REQUEST_DECLARE_WAR:
				request_declare_war();
				break;

			case TALK_REQUEST_BUY_FOOD:
				request_buy_food();
				break;

			case TALK_DECLARE_WAR:
				declare_war();
				break;

			case TALK_GIVE_TRIBUTE:
				give_tribute("tribute");
				break;

			case TALK_DEMAND_TRIBUTE:
				demand_tribute(0); // 0-is tribute, not aid
				break;

			case TALK_GIVE_AID:
				give_tribute("aid");
				break;

			case TALK_DEMAND_AID:
				demand_tribute(1); // 1-is aid, not tribute
				break;

			case TALK_GIVE_TECH:
				give_tech();
				break;

			case TALK_DEMAND_TECH:
				demand_tech();
				break;

			case TALK_REQUEST_SURRENDER:
				request_surrender();
				break;

			case TALK_SURRENDER:
				surrender();
				break;
		}

		//TODO return correct message
		return String.Empty;
	}

	public bool is_reply_needed()
	{
		return talk_msg_reply_needed_array[talk_id - 1];
	}

	// can specify an additional nation recno that is to be considered invalid.
	public bool is_valid_to_disp(int invalid_nation_recno = 0)
	{
		//--- check if the nations are still there -----//

		if (NationArray.IsDeleted(from_nation_recno) ||
		    (invalid_nation_recno != 0 && from_nation_recno == invalid_nation_recno))
			return false;

		if (NationArray.IsDeleted(to_nation_recno) ||
		    (invalid_nation_recno != 0 && to_nation_recno == invalid_nation_recno))
			return false;

		//--------------------------------------//

		switch (talk_id)
		{
			case TALK_REQUEST_TRADE_EMBARGO:
			case TALK_REQUEST_DECLARE_WAR:
				if (NationArray.IsDeleted(talk_para1) ||
				    (invalid_nation_recno != 0 && talk_para1 == invalid_nation_recno))
					return false;
				break;
		}

		return true;
	}

	public bool is_valid_to_reply()
	{
		//-----------------------------------------------------//
		// When a diplomatic message is sent, the receiver must
		//	reply the message with a month.
		//-----------------------------------------------------//

		if (Info.game_date > date.AddDays(GameConstants.TALK_MSG_VALID_DAYS))
			return false;

		//--- check if the nations are still there -----//

		if (NationArray.IsDeleted(from_nation_recno))
			return false;

		if (NationArray.IsDeleted(to_nation_recno))
			return false;

		//--------------------------------------//

		if (!TalkRes.can_send_msg(to_nation_recno, from_nation_recno, talk_id))
			return false;

		//--------------------------------------//

		Nation toNation = NationArray[to_nation_recno];
		Nation fromNation = NationArray[from_nation_recno];

		switch (talk_id)
		{
			case TALK_REQUEST_TRADE_EMBARGO:
				if (NationArray.IsDeleted(talk_para1))
					return false;

				//-- if the requesting nation is itself trading with the target nation --//

				if (fromNation.get_relation(talk_para1).trade_treaty)
					return false;

				//-- or if the requested nation already doesn't have a trade treaty with the nation --//

				if (!toNation.get_relation(talk_para1).trade_treaty)
					return false;

				break;

			case TALK_REQUEST_DECLARE_WAR:
				if (NationArray.IsDeleted(talk_para1))
					return false;

				//-- if the requesting nation is no longer hostile with the nation --//

				if (fromNation.get_relation_status(talk_para1) != NationBase.NATION_HOSTILE)
					return false;

				//-- or if the requested nation has become hostile with the nation --//

				if (toNation.get_relation_status(talk_para1) == NationBase.NATION_HOSTILE)
					return false;

				break;

			case TALK_REQUEST_BUY_FOOD:
				return fromNation.cash >= talk_para1 * talk_para2 / 10.0;

			case TALK_GIVE_TRIBUTE:
			case TALK_GIVE_AID:
				return fromNation.cash >= talk_para1;

			case TALK_GIVE_TECH:
				/* 		// still display the message even if the nation already has the technology
						//---- if the nation has acquired the technology itself ----//
	
						if( tech_res[talk_para1]->get_nation_tech_level(to_nation_recno) >= talk_para2 )
							return 0;
				*/
				break;

			case TALK_DEMAND_TECH:
				/*
						//---- if the requesting nation has acquired the technology itself ----//
	
						if( tech_res[talk_para1]->get_nation_tech_level(from_nation_recno) >= talk_para2 )
							return 0;
				*/
				break;
		}

		return true;
	}

	public bool can_accept()
	{
		Nation toNation = NationArray[to_nation_recno];

		switch (talk_id)
		{
			case TALK_REQUEST_BUY_FOOD:
				return toNation.food >= talk_para1;

			case TALK_DEMAND_TRIBUTE:
			case TALK_DEMAND_AID:
				return toNation.cash >= talk_para1;

			case TALK_DEMAND_TECH: // the requested nation has the technology
				return TechRes[talk_para1].get_nation_tech_level(to_nation_recno) > 0;
		}

		return true;
	}

	public void propose_treaty(int treatyType)
	{
	}

	public void end_treaty(int treatyType)
	{
	}

	public void request_military_aid()
	{
	}

	public void request_trade_embargo()
	{
	}

	public void request_cease_war()
	{
	}

	public void request_declare_war()
	{
	}

	public void request_buy_food()
	{
	}

	public void declare_war()
	{
	}

	public void give_tribute(string tributeStr)
	{
	}

	public void demand_tribute(int isAid)
	{
	}

	public void give_tech()
	{
	}

	public void demand_tech()
	{
	}

	public void request_surrender()
	{
	}

	public void surrender()
	{
	}
}