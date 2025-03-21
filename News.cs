using System;

namespace TenKingdoms;

public class News
{
	public const int NEWS_DIPLOMACY = 1;
	public const int NEWS_TOWN_REBEL = 2;
	public const int NEWS_MIGRATE = 3;
	public const int NEWS_NEW_NATION = 4;
	public const int NEWS_NATION_DESTROYED = 5;
	public const int NEWS_NATION_SURRENDER = 6;
	public const int NEWS_KING_DIE = 7;
	public const int NEWS_NEW_KING = 8;
	public const int NEWS_FIRM_DESTROYED = 9; // when your firm is destroyed
	public const int NEWS_FIRM_CAPTURED = 10;
	public const int NEWS_TOWN_DESTROYED = 11; // when the last unit in the town is killed by an enemy
	public const int NEWS_TOWN_ABANDONED = 12; // when all villagers have left the village
	public const int NEWS_TOWN_SURRENDERED = 13; // A town surrenders to another nation
	public const int NEWS_MONSTER_KING_KILLED = 14;
	public const int NEWS_MONSTER_FIRM_DESTROYED = 15;
	public const int NEWS_SCROLL_ACQUIRED = 16;
	public const int NEWS_MONSTER_GOLD_ACQUIRED = 17;
	public const int NEWS_YOUR_SPY_KILLED = 18;
	public const int NEWS_ENEMY_SPY_KILLED = 19;
	public const int NEWS_UNIT_BETRAY = 20; // your unit betray or other nation's betray and turn towards you
	public const int NEWS_UNIT_ASSASSINATED = 21;
	public const int NEWS_ASSASSINATOR_CAUGHT = 22;
	public const int NEWS_GENERAL_DIE = 23;
	public const int NEWS_RAW_EXHAUST = 24;
	public const int NEWS_TECH_RESEARCHED = 25;
	public const int NEWS_LIGHTNING_DAMAGE = 26;
	public const int NEWS_EARTHQUAKE_DAMAGE = 27;
	public const int NEWS_GOAL_DEADLINE = 28;
	public const int NEWS_WEAPON_SHIP_WORN_OUT = 29;
	public const int NEWS_FIRM_WORN_OUT = 30;
	public const int NEWS_CHAT_MSG = 31;
	public const int NEWS_MULTI_RETIRE = 32;
	public const int NEWS_MULTI_QUIT_GAME = 33;
	public const int NEWS_MULTI_SAVE_GAME = 34;
	public const int NEWS_MULTI_CONNECTION_LOST = 35;

	public const int NEWS_TYPE_NUM = 1;
	public const int NEWS_NORMAL = 0;

	public const int NEWS_DISP_ALL = 0;
	public const int NEWS_DISP_FRIENDLY = 1;
	public const int NEWS_DISP_PLAYER = 2;
	public const int NEWS_DISP_NONE = 3;

	public const int NEWS_LOC_TOWN = 1;
	public const int NEWS_LOC_FIRM = 2;
	public const int NEWS_LOC_UNIT = 3;
	public const int NEWS_LOC_ANY = 4;

	public const int DESTROYER_NATION = 1;
	public const int DESTROYER_REBEL = 2;
	public const int DESTROYER_MONSTER = 3;
	public const int DESTROYER_UNKNOWN = 4;

	public int id; // id. of the news, NEWS_???

	// news type, type may be > NEWS_TYPE_NUM
	// for indicating that the news has been displayed in the stock window, do display it on the newspaper again
	public int type;

	public DateTime news_date; // date of the news

	public int nation_color1; // nation color, can't use nation_recno directly, because it may bankrupt one day
	public int nation_color2;
	public int nation_race_id1;
	public int nation_race_id2;

	public int nation_name_id1; // nation res. id of the nation that generate the news

	// if the news is related to two nations (e.g. one nation buys the stock of another nation)
	public int nation_name_id2;

	public int short_para1;
	public int short_para2;
	public int short_para3;
	public int short_para4;
	public int short_para5;

	public int loc_type;
	public int loc_type_para;
	public int loc_type_para2; // must use uint16_t as it will be used to store unit name id. 
	public int loc_x, loc_y; // location where the news happens

	private RaceRes RaceRes => Sys.Instance.RaceRes;
	private Info Info => Sys.Instance.Info;
	private World World => Sys.Instance.World;
	private NationArray NationArray => Sys.Instance.NationArray;
	private TownArray TownArray => Sys.Instance.TownArray;
	private FirmArray FirmArray => Sys.Instance.FirmArray;
	private UnitArray UnitArray => Sys.Instance.UnitArray;

	public int news_type()
	{
		return type % NEWS_TYPE_NUM;
	}

	public string nation_name1()
	{
		string str = String.Empty;

		if (nation_name_id1 < 0) // human player - custom name
			str += NationArray.get_human_name(nation_name_id1, true) + "'s Kingdom";
		else
			str += RaceRes[nation_race_id1].get_single_name(nation_name_id1) + "'s Kingdom";

		//------ add color bar -------//

		str += nation_color_str1();

		return str;
	}

	public string nation_name2()
	{
		string str = String.Empty;

		if (nation_name_id2 < 0) // human player - custom name
			str += NationArray.get_human_name(nation_name_id2, true) + "'s Kingdom";
		else
			str += RaceRes[nation_race_id2].get_single_name(nation_name_id2) + "'s Kingdom";

		//------ add color bar -------//

		str += nation_color_str2();

		return str;
	}

	public string king_name1(bool addColor = false)
	{
		string str;

		if (nation_name_id1 < 0) // human player - custom name
			str = NationArray.get_human_name(nation_name_id1);
		else
			str = RaceRes[nation_race_id1].get_name(nation_name_id1);

		//------ add color bar -------//

		if (addColor)
			str += nation_color_str1();

		return str;
	}

	public string king_name2(bool addColor = false)
	{
		string str;

		if (nation_name_id2 < 0) // human player - custom name
			str = NationArray.get_human_name(nation_name_id2);
		else
			str = RaceRes[nation_race_id2].get_name(nation_name_id2);

		//------ add color bar -------//

		if (addColor)
			str += nation_color_str2();

		return str;
	}

	public string nation_color_str1()
	{
		return " @COL" + Convert.ToChar(30 + nation_color1);
	}

	public string nation_color_str2()
	{
		return " @COL" + Convert.ToChar(30 + nation_color2);
	}

	public string msg() // return the news msg
	{
		//TODO rewrite
		return "This is a news message.";
	}

	public bool is_major()
	{
		//TODO rewrite
		return true;
	}

	public void set_loc(int xLoc, int yLoc, int locType, int locTypePara = 0, int locTypePara2 = 0)
	{
		loc_type = locType;
		loc_type_para = locTypePara;
		loc_type_para2 = locTypePara2;

		loc_x = xLoc;
		loc_y = yLoc;
	}

	public bool is_loc_valid()
	{
		if (loc_type == 0)
			return false;

		bool rc = false;

		if (loc_type == NEWS_LOC_TOWN)
		{
			if (!TownArray.IsDeleted(loc_type_para))
			{
				Town town = TownArray[loc_type_para];

				rc = town.LocCenterX == loc_x && town.LocCenterY == loc_y;
			}
		}
		else if (loc_type == NEWS_LOC_FIRM)
		{
			if (!FirmArray.IsDeleted(loc_type_para))
			{
				Firm firm = FirmArray[loc_type_para];

				rc = firm.center_x == loc_x && firm.center_y == loc_y;
			}
		}
		else if (loc_type == NEWS_LOC_UNIT)
		{
			if (!UnitArray.IsDeleted(loc_type_para))
			{
				Unit unit = UnitArray[loc_type_para];

				if (unit.name_id == loc_type_para2)
				{
					//--- if the unit is no longer belong to our nation ----//
					//--- only keep track of the unit for one month --------//

					if (unit.nation_recno == NationArray.player_recno || Info.game_date < news_date.AddDays(30.0))
					{
						if (unit.get_cur_loc(out loc_x, out loc_y))
						{
							Location location = World.GetLoc(loc_x, loc_y);

							rc = location.VisitLevel > 0;
						}
					}
				}
			}
		}
		else if (loc_type == NEWS_LOC_ANY)
		{
			rc = true;
		}

		if (!rc)
			loc_type = 0;

		return rc;
	}

	//---- functions for return news string ----//

	public void diplomacy()
	{
	}

	public void town_rebel()
	{
	}

	public void migrate()
	{
	}

	public void new_nation()
	{
	}

	public void nation_destroyed()
	{
	}

	public void nation_surrender()
	{
	}

	public void king_die()
	{
	}

	public void new_king()
	{
	}

	public void firm_destroyed()
	{
	}

	public void firm_captured()
	{
	}

	public void town_destroyed()
	{
	}

	public void town_abandoned()
	{
	}

	public void town_surrendered()
	{
	}

	public void monster_king_killed()
	{
	}

	public void monster_firm_destroyed()
	{
	}

	public void scroll_acquired()
	{
	}

	public void monster_gold_acquired()
	{
	}

	public void your_spy_killed()
	{
	}

	public void enemy_spy_killed()
	{
	}

	public void unit_betray()
	{
	}

	public void unit_assassinated()
	{
	}

	public void assassinator_caught()
	{
	}

	public void general_die()
	{
	}

	public void raw_exhaust()
	{
	}

	public void tech_researched()
	{
	}

	public void lightning_damage()
	{
	}

	public void earthquake_damage()
	{
	}

	public void goal_deadline()
	{
	}

	public void weapon_ship_worn_out()
	{
	}

	public void firm_worn_out()
	{
	}

	public void chat_msg()
	{
	}

	public void multi_retire()
	{
	}

	public void multi_quit_game()
	{
	}

	public void multi_save_game()
	{
	}

	public void multi_connection_lost()
	{
	}
}