using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class NewsArray
{
	//------ display options ------//

	public int[] news_type_option = new int[News.NEWS_TYPE_NUM];
	public int news_who_option;
	public bool news_add_flag;

	public int last_clear_recno;

	private List<News> newsList = new List<News>();

	private UnitRes UnitRes => Sys.Instance.UnitRes;
	private Info Info => Sys.Instance.Info;
	private NationArray NationArray => Sys.Instance.NationArray;
	private TownArray TownArray => Sys.Instance.TownArray;
	private FirmArray FirmArray => Sys.Instance.FirmArray;
	private UnitArray UnitArray => Sys.Instance.UnitArray;
	private SpyArray SpyArray => Sys.Instance.SpyArray;

	public NewsArray()
	{
		reset();

		//TODO fonts
		//set_font(&font_san); // use black font
	}

	public void enable()
	{
		news_add_flag = true;
	}

	public void disable()
	{
		news_add_flag = false;
	}

	public void reset()
	{
		newsList.Clear();

		last_clear_recno = 0;
		news_add_flag = true;

		default_setting();
	}

	public void default_setting()
	{
		news_type_option[News.NEWS_NORMAL] = 1;

		news_who_option = News.NEWS_DISP_PLAYER; // default display news of groups controlled by the player
	}

	//public void set_font(Font font);

	public void clear_news_disp()
	{
		last_clear_recno = newsList.Count - 1;
	}

	public News add_news(int newsId, int newsType, int nationRecno1 = 0, int nationRecno2 = 0, bool forceAdd = false)
	{
		if (NationArray.player_recno == 0) // if the player has lost
			return null;

		//----- only news of nations that have contact with the player are added ----//

		if (!forceAdd)
		{
			Nation playerNation = NationArray.player;

			if (nationRecno1 != 0 && nationRecno1 != NationArray.player_recno)
			{
				if (!playerNation.get_relation(nationRecno1).has_contact)
					return null;
			}

			if (nationRecno2 != 0 && nationRecno2 != NationArray.player_recno)
			{
				if (!playerNation.get_relation(nationRecno2).has_contact)
					return null;
			}
		}

		//----------------------------------------------//

		News news = new News();
		news.id = newsId;
		news.type = newsType;
		news.news_date = Info.game_date;
		news.loc_type = 0;

		if (nationRecno1 != 0)
		{
			Nation nation1 = NationArray[nationRecno1];

			news.nation_name_id1 = nation1.nation_name_id;
			news.nation_race_id1 = nation1.race_id;
			news.nation_color1 = nation1.color_scheme_id;
		}
		else
		{
			news.nation_name_id1 = 0;
			news.nation_color1 = -1;
		}

		if (nationRecno2 != 0)
		{
			Nation nation2 = NationArray[nationRecno2];

			news.nation_name_id2 = nation2.nation_name_id;
			news.nation_race_id2 = nation2.race_id;
			news.nation_color2 = nation2.color_scheme_id;
		}
		else
		{
			news.nation_name_id2 = 0;
			news.nation_color2 = -1;
		}

		//--- if the news adding flag is turned off, don't add the news ---//

		if (news_add_flag)
		{
			//--- if no. of news reaches MAX., delete the oldest one ---//

			if (newsList.Count >= GameConstants.MAX_NEWS)
			{
				newsList.RemoveAt(0);

				if (last_clear_recno > 0)
					last_clear_recno--;
			}
			
			newsList.Add(news);
		}

		return news;
	}

	public void remove(int newsId, int shortPara1)
	{
		for (int i = newsList.Count - 1; i >= 0; i--)
		{
			News news = newsList[i];

			if (news.id == newsId && news.short_para1 == shortPara1)
			{
				newsList.RemoveAt(i);

				//if( i<last_clear_recno && last_clear_recno > 1 )
				if (i <= last_clear_recno && last_clear_recno > 0)
					last_clear_recno--;

				break;
			}
		}
	}

	//------ functions for adding news -------//

	public void diplomacy(TalkMsg talkMsg)
	{
		News news = add_news(News.NEWS_DIPLOMACY, News.NEWS_NORMAL,
			talkMsg.from_nation_recno, talkMsg.to_nation_recno);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.short_para1 = talkMsg.RecNo;
	}

	public void town_rebel(int townRecno, int rebelCount)
	{
		Town town = TownArray[townRecno];

		//----------- add news --------------//

		News news = add_news(News.NEWS_TOWN_REBEL, News.NEWS_NORMAL, town.NationId);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.short_para1 = town.TownNameId;
		news.short_para2 = rebelCount;

		//-------- set location ----------//

		news.set_loc(town.LocCenterX, town.LocCenterY, News.NEWS_LOC_TOWN, townRecno);
	}

	public void migrate(int srcTownRecno, int desTownRecno, int raceId, int migratedCount, int firmRecno = 0)
	{
		Town srcTown = TownArray[srcTownRecno];
		Town desTown = TownArray[desTownRecno];

		//----------- add news --------------//

		News news = add_news(News.NEWS_MIGRATE, News.NEWS_NORMAL,
			srcTown.NationId, desTown.NationId);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.short_para1 = srcTown.TownNameId;
		news.short_para2 = desTown.TownNameId;
		news.short_para3 = raceId;
		news.short_para4 = migratedCount;

		if (firmRecno != 0)
			news.short_para5 = FirmArray[firmRecno].firm_id;
		else
			news.short_para5 = 0;

		//-------- set location ----------//

		news.set_loc(desTown.LocCenterX, desTown.LocCenterY, News.NEWS_LOC_TOWN, desTownRecno);
	}

	public void new_nation(int nationRecno)
	{
		News news = add_news(News.NEWS_NEW_NATION, News.NEWS_NORMAL, nationRecno);

		if (news == null)
			return;

		//---- set the news location to one of its town ----//

		foreach (Town town in TownArray)
		{
			if (town.NationId == nationRecno)
			{
				news.set_loc(town.LocCenterX, town.LocCenterY, News.NEWS_LOC_TOWN, town.TownId);
				break;
			}
		}
	}

	public void nation_destroyed(int nationRecno)
	{
		add_news(News.NEWS_NATION_DESTROYED, News.NEWS_NORMAL, nationRecno);
	}

	public void nation_surrender(int nationRecno, int toNationRecno)
	{
		add_news(News.NEWS_NATION_SURRENDER, News.NEWS_NORMAL, nationRecno, toNationRecno);
	}

	public void king_die(int nationRecno)
	{
		add_news(News.NEWS_KING_DIE, News.NEWS_NORMAL, nationRecno);
	}

	public void new_king(int nationRecno, int kingUnitRecno)
	{
		News news = add_news(News.NEWS_NEW_KING, News.NEWS_NORMAL, nationRecno);

		if (news == null)
			return;

		Unit unit = UnitArray[kingUnitRecno];

		news.short_para1 = unit.RaceId;
		news.short_para2 = unit.NameId;
	}

	public void firm_destroyed(int firmRecno, Unit attackUnit, int destroyerNationRecno)
	{
		Firm firm = FirmArray[firmRecno];

		News news = add_news(News.NEWS_FIRM_DESTROYED, News.NEWS_NORMAL,
			firm.nation_recno, destroyerNationRecno);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.short_para1 = firm.firm_id;

		if (firm.closest_town_name_id != 0)
			news.short_para2 = firm.closest_town_name_id;
		else
			news.short_para2 = firm.get_closest_town_name_id();

		//-------- set destroyer type ------//

		news.short_para3 = News.DESTROYER_UNKNOWN;

		if (destroyerNationRecno != 0)
		{
			if (!NationArray.IsDeleted(destroyerNationRecno))
				news.short_para3 = News.DESTROYER_NATION;
		}
		else if (attackUnit != null)
		{
			if (attackUnit.UnitMode == UnitConstants.UNIT_MODE_REBEL)
				news.short_para3 = News.DESTROYER_REBEL;

			else if (UnitRes[attackUnit.UnitType].unit_class == UnitConstants.UNIT_CLASS_MONSTER)
				news.short_para3 = News.DESTROYER_MONSTER;
		}

		news.set_loc(firm.center_x, firm.center_y, News.NEWS_LOC_ANY);
	}

	public void firm_captured(int firmRecno, int takeoverNationRecno, int spyTakeover)
	{
		Firm firm = FirmArray[firmRecno];

		News news = add_news(News.NEWS_FIRM_CAPTURED, News.NEWS_NORMAL,
			firm.nation_recno, takeoverNationRecno);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.short_para1 = firm.firm_id;

		if (firm.closest_town_name_id != 0)
			news.short_para2 = firm.closest_town_name_id;
		else
			news.short_para2 = firm.get_closest_town_name_id();

		news.short_para3 = spyTakeover;

		//--------- set location ---------//

		news.set_loc(firm.center_x, firm.center_y, News.NEWS_LOC_FIRM, firmRecno);
	}

	public void town_destroyed(int townNameId, int xLoc, int yLoc, Unit attackUnit, int destroyerNationRecno)
	{
		News news = add_news(News.NEWS_TOWN_DESTROYED, News.NEWS_NORMAL,
			NationArray.player_recno, destroyerNationRecno);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.short_para1 = townNameId;

		//-------- set destroyer type ------//

		news.short_para2 = News.DESTROYER_UNKNOWN;

		if (destroyerNationRecno != 0)
		{
			if (!NationArray.IsDeleted(destroyerNationRecno))
				news.short_para2 = News.DESTROYER_NATION;
		}
		else if (attackUnit != null)
		{
			if (attackUnit.UnitMode == UnitConstants.UNIT_MODE_REBEL)
				news.short_para2 = News.DESTROYER_REBEL;

			else if (UnitRes[attackUnit.UnitType].unit_class == UnitConstants.UNIT_CLASS_MONSTER)
				news.short_para2 = News.DESTROYER_MONSTER;
		}

		news.set_loc(xLoc, yLoc, News.NEWS_LOC_ANY);
	}

	public void town_abandoned(int townRecno)
	{
		Town town = TownArray[townRecno];

		News news = add_news(News.NEWS_TOWN_ABANDONED, News.NEWS_NORMAL, town.NationId);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.short_para1 = town.TownNameId;

		news.set_loc(town.LocCenterX, town.LocCenterY, News.NEWS_LOC_ANY);
	}

	public void town_surrendered(int townRecno, int toNationRecno)
	{
		Town town = TownArray[townRecno];

		News news = add_news(News.NEWS_TOWN_SURRENDERED, News.NEWS_NORMAL,
			toNationRecno, town.NationId);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.short_para1 = town.TownNameId;

		news.set_loc(town.LocCenterX, town.LocCenterY, News.NEWS_LOC_TOWN, townRecno);
	}

	public void monster_king_killed(int monsterId, int xLoc, int yLoc)
	{
		News news = add_news(News.NEWS_MONSTER_KING_KILLED, News.NEWS_NORMAL);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.short_para1 = monsterId;

		news.set_loc(xLoc, yLoc, News.NEWS_LOC_ANY);
	}

	public void monster_firm_destroyed(int monsterId, int xLoc, int yLoc)
	{
		News news = add_news(News.NEWS_MONSTER_FIRM_DESTROYED, News.NEWS_NORMAL);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.short_para1 = monsterId;

		news.set_loc(xLoc, yLoc, News.NEWS_LOC_ANY);
	}

	public void scroll_acquired(int acquireNationRecno, int scrollRaceId)
	{
		News news = add_news(News.NEWS_SCROLL_ACQUIRED, News.NEWS_NORMAL, acquireNationRecno);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.short_para1 = scrollRaceId;
	}

	public void monster_gold_acquired(int goldAmt)
	{
		News news = add_news(News.NEWS_MONSTER_GOLD_ACQUIRED, News.NEWS_NORMAL, NationArray.player_recno);

		if (news == null)
			return;

		news.short_para1 = goldAmt;
	}

	public void spy_killed(int spyRecno)
	{
		Spy spy = SpyArray[spyRecno];
		News news = null;

		//---------- your spy is killed in an enemy nation ---------//

		if (spy.true_nation_recno == NationArray.player_recno)
		{
			news = add_news(News.NEWS_YOUR_SPY_KILLED, News.NEWS_NORMAL,
				NationArray.player_recno, spy.cloaked_nation_recno);
		}
		else //----- an enemy spy in your nation is uncovered and executed ----//
		{
			news = add_news(News.NEWS_ENEMY_SPY_KILLED, News.NEWS_NORMAL,
				NationArray.player_recno, spy.true_nation_recno);
		}

		if (news == null) // only news of nations that have contact with the player are added
			return;

		//-------------------------------------------//

		news.short_para3 = spy.spy_place;

		if (spy.spy_place == Spy.SPY_FIRM)
		{
			Firm firm = FirmArray[spy.spy_place_para];

			news.short_para1 = firm.firm_id;
			news.short_para2 = firm.get_closest_town_name_id();

			news.set_loc(firm.center_x, firm.center_y, News.NEWS_LOC_FIRM, firm.firm_recno);
		}
		else if (spy.spy_place == Spy.SPY_TOWN)
		{
			Town town = TownArray[spy.spy_place_para];

			news.short_para1 = 0;
			news.short_para2 = town.TownNameId;

			news.set_loc(town.LocCenterX, town.LocCenterY, News.NEWS_LOC_TOWN, town.TownId);
		}
		else if (spy.spy_place == Spy.SPY_MOBILE)
		{
			Unit unit = UnitArray[spy.spy_place_para];

			news.short_para1 = unit.RaceId;
			news.short_para2 = unit.NameId;
		}
	}

	public void unit_betray(int unitRecno, int betrayToNationRecno)
	{
		Unit unit = UnitArray[unitRecno];

		News news = add_news(News.NEWS_UNIT_BETRAY, News.NEWS_NORMAL,
			unit.NationId, betrayToNationRecno);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.short_para1 = unit.RaceId;
		news.short_para2 = unit.NameId;
		news.short_para3 = unit.Rank;

		//------- set location --------//

		if (betrayToNationRecno == NationArray.player_recno)
			news.set_loc(unit.NextLocX, unit.NextLocY, News.NEWS_LOC_UNIT, unitRecno, unit.NameId);
		else
			news.set_loc(unit.NextLocX, unit.NextLocY, News.NEWS_LOC_ANY);
	}

	public void unit_assassinated(int unitRecno, bool spyKilled)
	{
		Unit unit = UnitArray[unitRecno];

		News news = add_news(News.NEWS_UNIT_ASSASSINATED, News.NEWS_NORMAL, unit.NationId);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.short_para1 = unit.RaceId;
		news.short_para2 = unit.NameId;
		news.short_para3 = unit.Rank;
		news.short_para4 = spyKilled ? 1 : 0;

		//------- set location --------//

		int xLoc, yLoc;

		unit.get_cur_loc(out xLoc, out yLoc);

		news.set_loc(xLoc, yLoc, News.NEWS_LOC_ANY);
	}

	public void assassinator_caught(int spyRecno, int targetRankId)
	{
		News news = add_news(News.NEWS_ASSASSINATOR_CAUGHT, News.NEWS_NORMAL);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.short_para1 = targetRankId;

		//------- set location --------//

		int xLoc, yLoc;

		SpyArray[spyRecno].get_loc(out xLoc, out yLoc);

		news.set_loc(xLoc, yLoc, News.NEWS_LOC_ANY);
	}

	public void general_die(int unitRecno)
	{
		Unit unit = UnitArray[unitRecno];

		News news = add_news(News.NEWS_GENERAL_DIE, News.NEWS_NORMAL, unit.NationId);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.short_para1 = unit.RaceId;
		news.short_para2 = unit.NameId;

		news.set_loc(unit.NextLocX, unit.NextLocY, News.NEWS_LOC_ANY);
	}

	public void raw_exhaust(int rawId, int xLoc, int yLoc)
	{
		News news = add_news(News.NEWS_RAW_EXHAUST, News.NEWS_NORMAL);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.short_para1 = rawId;

		news.set_loc(xLoc, yLoc, News.NEWS_LOC_ANY);
	}

	public void tech_researched(int techId, int techVersion)
	{
		News news = add_news(News.NEWS_TECH_RESEARCHED, News.NEWS_NORMAL, NationArray.player_recno);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.short_para1 = techId;
		news.short_para2 = techVersion;
	}

	public void lightning_damage(int xLoc, int yLoc, int objectId, int recno, int objectDie)
	{
		News news = add_news(News.NEWS_LIGHTNING_DAMAGE, News.NEWS_NORMAL);

		if (news == null)
			return;

		news.set_loc(xLoc, yLoc, objectId, recno);

		news.short_para1 = objectId;
		news.short_para2 = 0;
		news.short_para3 = 0;
		news.short_para4 = 0;
		switch (objectId)
		{
			case News.NEWS_LOC_UNIT:
				news.short_para2 = UnitArray[recno].RaceId;
				if (news.short_para2 > 0)
					news.short_para3 = UnitArray[recno].NameId;
				else
					news.short_para3 = UnitArray[recno].UnitType;
				news.short_para4 = UnitArray[recno].Rank;
				break;
			case News.NEWS_LOC_FIRM:
				news.short_para2 = FirmArray[recno].firm_id;
				news.short_para3 = FirmArray[recno].closest_town_name_id;
				break;
			case News.NEWS_LOC_TOWN:
				news.short_para3 = TownArray[recno].TownNameId;
				break;
		}

		news.short_para5 = objectDie;
	}

	public void earthquake_damage(int unitDamage, int unitDie, int townDamage, int firmDamage, int firmDie)
	{
		if (unitDamage > 0 || unitDie > 0)
		{
			News news = add_news(News.NEWS_EARTHQUAKE_DAMAGE, News.NEWS_NORMAL);
			if (news != null)
			{
				news.short_para1 = 1;
				news.short_para2 = unitDamage;
				news.short_para3 = unitDie;
			}
		}

		if (townDamage > 0)
		{
			News news = add_news(News.NEWS_EARTHQUAKE_DAMAGE, News.NEWS_NORMAL);
			if (news != null)
			{
				news.short_para1 = 2;
				news.short_para2 = townDamage;
			}
		}

		if (firmDamage > 0 || firmDie > 0)
		{
			News news = add_news(News.NEWS_EARTHQUAKE_DAMAGE, News.NEWS_NORMAL);
			if (news != null)
			{
				news.short_para1 = 3;
				news.short_para2 = firmDamage;
				news.short_para3 = firmDie;
			}
		}
	}

	public void goal_deadline(int yearLeft, int monthLeft)
	{
		News news = add_news(News.NEWS_GOAL_DEADLINE, News.NEWS_NORMAL, NationArray.player_recno);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.short_para1 = yearLeft;
		news.short_para2 = monthLeft;
	}

	public void weapon_ship_worn_out(int unitId, int weaponLevel)
	{
		News news = add_news(News.NEWS_WEAPON_SHIP_WORN_OUT, News.NEWS_NORMAL, NationArray.player_recno);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.short_para1 = unitId;
		news.short_para2 = weaponLevel;
	}

	public void firm_worn_out(int firmRecno)
	{
		Firm firm = FirmArray[firmRecno];

		News news = add_news(News.NEWS_FIRM_WORN_OUT, News.NEWS_NORMAL, firm.nation_recno);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.short_para1 = firm.firm_id;

		if (firm.closest_town_name_id != 0)
			news.short_para2 = firm.closest_town_name_id;
		else
			news.short_para2 = firm.get_closest_town_name_id();
	}

	public void chat_msg(int fromNationRecno, string chatStr)
	{
		//---- add the chat string into Info::remote_chat_str_array[] ----//

		int useChatId = 0;
		DateTime minDate = Info.game_date.AddDays(1.0);

		//TODO rewrite
		/*for (int i = 0; i < MAX_REMOTE_CHAT_STR; i++)
		{
			if (Info.remote_chat_array[i].received_date < minDate) // replace the oldest one
			{
				minDate = Info.remote_chat_array[i].received_date;
				useChatId = i + 1;
			}
		}

		if (useChatId != 0)
		{
			ChatInfo* chatInfo = Info.remote_chat_array[useChatId - 1];

			chatInfo.received_date = Info.game_date;
			chatInfo.from_nation_recno = fromNationRecno;

			strncpy(chatInfo.chat_str, chatStr, CHAT_STR_LEN);
			chatInfo.chat_str[CHAT_STR_LEN] = '\0';
		}*/

		//----------------------------------------------//

		News news = add_news(News.NEWS_CHAT_MSG, News.NEWS_NORMAL, fromNationRecno);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.short_para1 = useChatId;
	}

	public void multi_retire(int nationRecno)
	{
		// add player recno as the 2nd parameter so this message is always displayed even if the player doesn't yet have contact with this nation
		add_news(News.NEWS_MULTI_RETIRE, News.NEWS_NORMAL,
			nationRecno, NationArray.player_recno, true);
	}

	public void multi_quit_game(int nationRecno)
	{
		// add player recno as the 2nd parameter so this message is always displayed even if the player doesn't yet have contact with this nation
		add_news(News.NEWS_MULTI_QUIT_GAME, News.NEWS_NORMAL,
			nationRecno, NationArray.player_recno, true);
	}

	public void multi_save_game()
	{
		add_news(News.NEWS_MULTI_SAVE_GAME, News.NEWS_NORMAL);
	}

	public void multi_connection_lost(int nationRecno)
	{
		// add player recno as the 2nd parameter so this message is always displayed even if the player doesn't yet have contact with this nation
		add_news(News.NEWS_MULTI_CONNECTION_LOST, News.NEWS_NORMAL,
			nationRecno, NationArray.player_recno, true);
	}
}