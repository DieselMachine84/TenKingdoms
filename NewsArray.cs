using System;
using System.Collections.Generic;
using System.IO;

namespace TenKingdoms;

public class NewsArray
{
	private readonly List<News> _newsList = new List<News>();
	private bool NewsEnabled { get; set; }
	public int LastClearId { get; set; }

	private UnitRes UnitRes => Sys.Instance.UnitRes;
	private Info Info => Sys.Instance.Info;
	private NationArray NationArray => Sys.Instance.NationArray;
	private TownArray TownArray => Sys.Instance.TownArray;
	private FirmArray FirmArray => Sys.Instance.FirmArray;
	private UnitArray UnitArray => Sys.Instance.UnitArray;
	private SpyArray SpyArray => Sys.Instance.SpyArray;

	public NewsArray()
	{
		LastClearId = -1;
		Enable();
	}

	public News this[int id] => _newsList[id];

	public int Count()
	{
		return _newsList.Count;
	}
	
	public void Enable()
	{
		NewsEnabled = true;
	}

	public void Disable()
	{
		NewsEnabled = false;
	}

	public void ClearDisplayedNews()
	{
		LastClearId = _newsList.Count - 1;
	}

	private News AddNews(int newsId, int newsType, int nationId1 = 0, int nationId2 = 0, bool forceAdd = false)
	{
		if (NationArray.PlayerId == 0) // if the player has lost
			return null;

		//----- only news of nations that have contact with the player are added ----//

		if (!forceAdd)
		{
			Nation playerNation = NationArray.Player;

			if (nationId1 != 0 && nationId1 != NationArray.PlayerId)
			{
				if (playerNation == null || !playerNation.GetRelation(nationId1).HasContact)
					return null;
			}

			if (nationId2 != 0 && nationId2 != NationArray.PlayerId)
			{
				if (playerNation == null || !playerNation.GetRelation(nationId2).HasContact)
					return null;
			}
		}

		//----------------------------------------------//

		News news = new News();
		news.Id = newsId;
		news.Type = newsType;
		news.NewsDate = Info.GameDate;
		news.LocType = 0;

		if (nationId1 != 0)
		{
			Nation nation1 = NationArray[nationId1];

			news.NationNameId1 = nation1.NationNameId;
			news.NationRaceId1 = nation1.RaceId;
			news.NationColor1 = nation1.ColorSchemeId;
		}
		else
		{
			news.NationNameId1 = 0;
			news.NationColor1 = -1;
		}

		if (nationId2 != 0)
		{
			Nation nation2 = NationArray[nationId2];

			news.NationNameId2 = nation2.NationNameId;
			news.NationRaceId2 = nation2.RaceId;
			news.NationColor2 = nation2.ColorSchemeId;
		}
		else
		{
			news.NationNameId2 = 0;
			news.NationColor2 = -1;
		}

		//--- if the news adding flag is turned off, don't add the news ---//

		if (NewsEnabled)
		{
			_newsList.Add(news);
		}

		return news;
	}

	//TODO remove diplomatic news that are no longer valid for reply
	public void Remove(int newsId, int param1)
	{
		for (int i = _newsList.Count - 1; i >= 0; i--)
		{
			News news = _newsList[i];

			if (news.Id == newsId && news.Param1 == param1)
			{
				_newsList.RemoveAt(i);

				if (i <= LastClearId)
					LastClearId--;

				break;
			}
		}
	}

	//------ functions for adding news -------//

	public void Diplomacy(TalkMsg talkMsg)
	{
		News news = AddNews(News.NEWS_DIPLOMACY, News.NEWS_NORMAL, talkMsg.FromNationId, talkMsg.ToNationId);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.Param1 = talkMsg.Id;
	}

	public void TownRebel(Town town, int rebelCount)
	{
		News news = AddNews(News.NEWS_TOWN_REBEL, News.NEWS_NORMAL, town.NationId);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.Param2 = rebelCount;
		news.Param6 = town.Name;
		news.SetLoc(town.LocCenterX, town.LocCenterY, News.NEWS_LOC_TOWN, town.TownId);
	}

	public void Migrate(Town srcTown, Town destTown, int raceId, int migratedCount, int firmId = 0)
	{
		News news = AddNews(News.NEWS_MIGRATE, News.NEWS_NORMAL, srcTown.NationId, destTown.NationId);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.Param3 = raceId;
		news.Param4 = migratedCount;
		news.Param5 = firmId != 0 ? FirmArray[firmId].FirmType : 0;
		news.Param6 = srcTown.Name;
		news.Param7 = destTown.Name;

		news.SetLoc(destTown.LocCenterX, destTown.LocCenterY, News.NEWS_LOC_TOWN, destTown.TownId);
	}

	public void NewNation(int nationId)
	{
		News news = AddNews(News.NEWS_NEW_NATION, News.NEWS_NORMAL, nationId);

		if (news == null)
			return;

		//---- set the news location to one of its town ----//

		foreach (Town town in TownArray)
		{
			if (town.NationId == nationId)
			{
				news.SetLoc(town.LocCenterX, town.LocCenterY, News.NEWS_LOC_TOWN, town.TownId);
				break;
			}
		}
	}

	public void NationDestroyed(int nationId)
	{
		AddNews(News.NEWS_NATION_DESTROYED, News.NEWS_NORMAL, nationId);
	}

	public void NationSurrender(int nationId, int toNationId)
	{
		AddNews(News.NEWS_NATION_SURRENDER, News.NEWS_NORMAL, nationId, toNationId);
	}

	public void KingDie(int nationId)
	{
		AddNews(News.NEWS_KING_DIE, News.NEWS_NORMAL, nationId);
	}

	public void NewKing(int nationId, int kingUnitId)
	{
		News news = AddNews(News.NEWS_NEW_KING, News.NEWS_NORMAL, nationId);

		if (news == null)
			return;

		Unit unit = UnitArray[kingUnitId];

		news.Param1 = unit.RaceId;
		news.Param2 = unit.NameId;
	}

	public void FirmDestroyed(Firm firm, Unit attackUnit, int destroyerNationId)
	{
		News news = AddNews(News.NEWS_FIRM_DESTROYED, News.NEWS_NORMAL, firm.NationId, destroyerNationId);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.Param1 = firm.FirmType;
		news.Param3 = News.DESTROYER_UNKNOWN;
		news.Param6 = !String.IsNullOrEmpty(firm.ClosestTownName) ? firm.ClosestTownName : firm.GetClosestTownName();

		if (destroyerNationId != 0)
		{
			if (!NationArray.IsDeleted(destroyerNationId))
				news.Param3 = News.DESTROYER_NATION;
		}
		else if (attackUnit != null)
		{
			if (attackUnit.UnitMode == UnitConstants.UNIT_MODE_REBEL)
				news.Param3 = News.DESTROYER_REBEL;

			else if (UnitRes[attackUnit.UnitType].UnitClass == UnitConstants.UNIT_CLASS_MONSTER)
				news.Param3 = News.DESTROYER_MONSTER;
		}

		news.SetLoc(firm.LocCenterX, firm.LocCenterY, News.NEWS_LOC_ANY);
	}

	public void FirmCaptured(Firm firm, int takeoverNationId, int spyTakeover)
	{
		News news = AddNews(News.NEWS_FIRM_CAPTURED, News.NEWS_NORMAL, firm.NationId, takeoverNationId);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.Param1 = firm.FirmType;
		news.Param3 = spyTakeover;
		news.Param6 = !String.IsNullOrEmpty(firm.ClosestTownName) ? firm.ClosestTownName : firm.GetClosestTownName();

		news.SetLoc(firm.LocCenterX, firm.LocCenterY, News.NEWS_LOC_FIRM, firm.FirmId);
	}

	public void TownDestroyed(string townName, int locX, int locY, Unit attackUnit, int destroyerNationId)
	{
		News news = AddNews(News.NEWS_TOWN_DESTROYED, News.NEWS_NORMAL, NationArray.PlayerId, destroyerNationId);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.Param2 = News.DESTROYER_UNKNOWN;
		news.Param6 = townName;

		if (destroyerNationId != 0)
		{
			if (!NationArray.IsDeleted(destroyerNationId))
				news.Param2 = News.DESTROYER_NATION;
		}
		else if (attackUnit != null)
		{
			if (attackUnit.UnitMode == UnitConstants.UNIT_MODE_REBEL)
				news.Param2 = News.DESTROYER_REBEL;

			else if (UnitRes[attackUnit.UnitType].UnitClass == UnitConstants.UNIT_CLASS_MONSTER)
				news.Param2 = News.DESTROYER_MONSTER;
		}

		news.SetLoc(locX, locY, News.NEWS_LOC_ANY);
	}

	public void TownAbandoned(Town town)
	{
		News news = AddNews(News.NEWS_TOWN_ABANDONED, News.NEWS_NORMAL, town.NationId);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.Param6 = town.Name;
		news.SetLoc(town.LocCenterX, town.LocCenterY, News.NEWS_LOC_ANY);
	}

	public void TownSurrendered(Town town, int toNationId)
	{
		News news = AddNews(News.NEWS_TOWN_SURRENDERED, News.NEWS_NORMAL, toNationId, town.NationId);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.Param6 = town.Name;

		news.SetLoc(town.LocCenterX, town.LocCenterY, News.NEWS_LOC_TOWN, town.TownId);
	}

	public void MonsterKingKilled(int monsterId, int locX, int locY)
	{
		News news = AddNews(News.NEWS_MONSTER_KING_KILLED, News.NEWS_NORMAL);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.Param1 = monsterId;

		news.SetLoc(locX, locY, News.NEWS_LOC_ANY);
	}

	public void MonsterFirmDestroyed(int monsterId, int locX, int locY)
	{
		News news = AddNews(News.NEWS_MONSTER_FIRM_DESTROYED, News.NEWS_NORMAL);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.Param1 = monsterId;

		news.SetLoc(locX, locY, News.NEWS_LOC_ANY);
	}

	public void ScrollAcquired(int acquireNationId, int scrollRaceId)
	{
		News news = AddNews(News.NEWS_SCROLL_ACQUIRED, News.NEWS_NORMAL, acquireNationId);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.Param1 = scrollRaceId;
	}

	public void MonsterGoldAcquired(int goldAmount)
	{
		News news = AddNews(News.NEWS_MONSTER_GOLD_ACQUIRED, News.NEWS_NORMAL, NationArray.PlayerId);

		if (news == null)
			return;

		news.Param1 = goldAmount;
	}

	public void SpyKilled(int spyId)
	{
		Spy spy = SpyArray[spyId];
		News news = null;

		//---------- your spy is killed in an enemy nation ---------//

		if (spy.TrueNationId == NationArray.PlayerId)
		{
			news = AddNews(News.NEWS_YOUR_SPY_KILLED, News.NEWS_NORMAL, NationArray.PlayerId, spy.CloakedNationId);
		}
		else //----- an enemy spy in your nation is uncovered and executed ----//
		{
			news = AddNews(News.NEWS_ENEMY_SPY_KILLED, News.NEWS_NORMAL, NationArray.PlayerId, spy.TrueNationId);
		}

		if (news == null) // only news of nations that have contact with the player are added
			return;

		//-------------------------------------------//

		news.Param3 = spy.SpyPlace;

		if (spy.SpyPlace == Spy.SPY_FIRM)
		{
			Firm firm = FirmArray[spy.SpyPlaceId];

			news.Param1 = firm.FirmType;
			news.Param6 = firm.GetClosestTownName();

			news.SetLoc(firm.LocCenterX, firm.LocCenterY, News.NEWS_LOC_FIRM, firm.FirmId);
		}
		else if (spy.SpyPlace == Spy.SPY_TOWN)
		{
			Town town = TownArray[spy.SpyPlaceId];

			news.Param1 = 0;
			news.Param6 = town.Name;

			news.SetLoc(town.LocCenterX, town.LocCenterY, News.NEWS_LOC_TOWN, town.TownId);
		}
		else if (spy.SpyPlace == Spy.SPY_MOBILE)
		{
			Unit unit = UnitArray[spy.SpyPlaceId];

			news.Param1 = unit.RaceId;
			news.Param2 = unit.NameId;
		}
	}

	public void UnitBetray(Unit unit, int betrayToNationId)
	{
		News news = AddNews(News.NEWS_UNIT_BETRAY, News.NEWS_NORMAL, unit.NationId, betrayToNationId);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.Param1 = unit.RaceId;
		news.Param2 = unit.NameId;
		news.Param3 = unit.Rank;

		if (betrayToNationId == NationArray.PlayerId)
			news.SetLoc(unit.NextLocX, unit.NextLocY, News.NEWS_LOC_UNIT, unit.SpriteId, unit.NameId);
		else
			news.SetLoc(unit.NextLocX, unit.NextLocY, News.NEWS_LOC_ANY);
	}

	public void UnitAssassinated(int unitId, bool spyKilled)
	{
		Unit unit = UnitArray[unitId];

		News news = AddNews(News.NEWS_UNIT_ASSASSINATED, News.NEWS_NORMAL, unit.NationId);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.Param1 = unit.RaceId;
		news.Param2 = unit.NameId;
		news.Param3 = unit.Rank;
		news.Param4 = spyKilled ? 1 : 0;

		unit.GetNextLoc(out int locX, out int locY);

		news.SetLoc(locX, locY, News.NEWS_LOC_ANY);
	}

	public void AssassinatorCaught(int spyId, int targetRankId)
	{
		News news = AddNews(News.NEWS_ASSASSINATOR_CAUGHT, News.NEWS_NORMAL);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.Param1 = targetRankId;

		if (SpyArray[spyId].GetSpyLocation(out int locX, out int locY))
			news.SetLoc(locX, locY, News.NEWS_LOC_ANY);
	}

	public void GeneralDie(Unit unit)
	{
		News news = AddNews(News.NEWS_GENERAL_DIE, News.NEWS_NORMAL, unit.NationId);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.Param1 = unit.RaceId;
		news.Param2 = unit.NameId;

		news.SetLoc(unit.NextLocX, unit.NextLocY, News.NEWS_LOC_ANY);
	}

	public void RawExhaust(int rawId, int locX, int locY)
	{
		News news = AddNews(News.NEWS_RAW_EXHAUST, News.NEWS_NORMAL);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.Param1 = rawId;

		news.SetLoc(locX, locY, News.NEWS_LOC_ANY);
	}

	public void TechResearched(int techId, int techVersion)
	{
		News news = AddNews(News.NEWS_TECH_RESEARCHED, News.NEWS_NORMAL, NationArray.PlayerId);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.Param1 = techId;
		news.Param2 = techVersion;
	}

	public void LightningDamage(int locX, int locY, int objectType, int objectId, int objectDie)
	{
		News news = AddNews(News.NEWS_LIGHTNING_DAMAGE, News.NEWS_NORMAL);

		if (news == null)
			return;

		news.SetLoc(locX, locY, objectType, objectId);

		news.Param1 = objectType;
		news.Param2 = 0;
		news.Param3 = 0;
		news.Param4 = 0;
		switch (objectType)
		{
			case News.NEWS_LOC_UNIT:
				Unit unit = UnitArray[objectId];
				news.Param2 = unit.RaceId;
				if (news.Param2 > 0)
					news.Param3 = unit.NameId;
				else
					news.Param3 = unit.UnitType;
				news.Param4 = unit.Rank;
				break;
			case News.NEWS_LOC_FIRM:
				Firm firm = FirmArray[objectId];
				news.Param2 = firm.FirmType;
				news.Param6 = firm.ClosestTownName;
				break;
			case News.NEWS_LOC_TOWN:
				Town town = TownArray[objectId];
				news.Param6 = town.Name;
				break;
		}

		news.Param5 = objectDie;
	}

	public void EarthquakeDamage(int unitDamage, int unitDie, int townDamage, int firmDamage, int firmDie)
	{
		if (unitDamage > 0 || unitDie > 0)
		{
			News news = AddNews(News.NEWS_EARTHQUAKE_DAMAGE, News.NEWS_NORMAL);
			if (news != null)
			{
				news.Param1 = 1;
				news.Param2 = unitDamage;
				news.Param3 = unitDie;
			}
		}

		if (townDamage > 0)
		{
			News news = AddNews(News.NEWS_EARTHQUAKE_DAMAGE, News.NEWS_NORMAL);
			if (news != null)
			{
				news.Param1 = 2;
				news.Param2 = townDamage;
			}
		}

		if (firmDamage > 0 || firmDie > 0)
		{
			News news = AddNews(News.NEWS_EARTHQUAKE_DAMAGE, News.NEWS_NORMAL);
			if (news != null)
			{
				news.Param1 = 3;
				news.Param2 = firmDamage;
				news.Param3 = firmDie;
			}
		}
	}

	public void GoalDeadline(int yearLeft, int monthLeft)
	{
		News news = AddNews(News.NEWS_GOAL_DEADLINE, News.NEWS_NORMAL, NationArray.PlayerId);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.Param1 = yearLeft;
		news.Param2 = monthLeft;
	}

	public void WeaponShipWornOut(int unitType, int weaponLevel)
	{
		News news = AddNews(News.NEWS_WEAPON_SHIP_WORN_OUT, News.NEWS_NORMAL, NationArray.PlayerId);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.Param1 = unitType;
		news.Param2 = weaponLevel;
	}

	public void FirmWornOut(Firm firm)
	{
		News news = AddNews(News.NEWS_FIRM_WORN_OUT, News.NEWS_NORMAL, firm.NationId);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.Param1 = firm.FirmType;
		news.Param6 = !String.IsNullOrEmpty(firm.ClosestTownName) ? firm.ClosestTownName : firm.GetClosestTownName();
	}

	public void ChatMsg(int fromNationId, string chatStr)
	{
		//---- add the chat string into Info::remote_chat_str_array[] ----//

		int useChatId = 0;
		DateTime minDate = Info.GameDate.AddDays(1.0);

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

		News news = AddNews(News.NEWS_CHAT_MSG, News.NEWS_NORMAL, fromNationId);

		if (news == null) // only news of nations that have contact with the player are added
			return;

		news.Param1 = useChatId;
	}

	public void MultiRetire(int nationId)
	{
		// add player id as the 2nd parameter so this message is always displayed even if the player doesn't yet have contact with this nation
		AddNews(News.NEWS_MULTI_RETIRE, News.NEWS_NORMAL, nationId, NationArray.PlayerId, true);
	}

	public void MultiQuitGame(int nationId)
	{
		// add player id as the 2nd parameter so this message is always displayed even if the player doesn't yet have contact with this nation
		AddNews(News.NEWS_MULTI_QUIT_GAME, News.NEWS_NORMAL, nationId, NationArray.PlayerId, true);
	}

	public void MultiSaveGame()
	{
		AddNews(News.NEWS_MULTI_SAVE_GAME, News.NEWS_NORMAL);
	}

	public void MultiConnectionLost(int nationId)
	{
		// add player id as the 2nd parameter so this message is always displayed even if the player doesn't yet have contact with this nation
		AddNews(News.NEWS_MULTI_CONNECTION_LOST, News.NEWS_NORMAL, nationId, NationArray.PlayerId, true);
	}
	
	#region SaveAndLoad

	public void SaveTo(BinaryWriter writer)
	{
		writer.Write(_newsList.Count);
		for (int i = 0; i < _newsList.Count; i++)
			_newsList[i].SaveTo(writer);
		writer.Write(NewsEnabled);
		writer.Write(LastClearId);
	}

	public void LoadFrom(BinaryReader reader)
	{
		int newsListCount = reader.ReadInt32();
		for (int i = 0; i < newsListCount; i++)
		{
			News news = new News();
			news.LoadFrom(reader);
			_newsList.Add(news);
		}
		NewsEnabled = reader.ReadBoolean();
		LastClearId = reader.ReadInt32();
	}
	
	#endregion
}