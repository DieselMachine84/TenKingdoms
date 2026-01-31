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

	public const int NEWS_LOC_TOWN = 1;
	public const int NEWS_LOC_FIRM = 2;
	public const int NEWS_LOC_UNIT = 3;
	public const int NEWS_LOC_ANY = 4;

	public const int DESTROYER_NATION = 1;
	public const int DESTROYER_REBEL = 2;
	public const int DESTROYER_MONSTER = 3;
	public const int DESTROYER_UNKNOWN = 4;

	public int Id { get; set; } // id. of the news, NEWS_???

	// news type, type may be > NEWS_TYPE_NUM
	// for indicating that the news has been displayed in the stock window, do display it on the newspaper again
	public int Type { get; set; }

	public DateTime NewsDate { get; set; } // date of the news

	public int NationColor1 { get; set; } // nation color, can't use NationId directly, because it may bankrupt one day
	public int NationColor2 { get; set; }
	public int NationRaceId1 { get; set; }
	public int NationRaceId2 { get; set; }
	public int NationNameId1 { get; set; }
	public int NationNameId2 { get; set; }

	public int Param1 { get; set; }
	public int Param2 { get; set; }
	public int Param3 { get; set; }
	public int Param4 { get; set; }
	public int Param5 { get; set; }
	public string Param6 { get; set; }
	public string Param7 { get; set; }

	public int LocType { get; set; }
	public int LocTypeParam { get; set; }
	public int LocTypeParam2 { get; set; } 
	public int LocX { get; set; }
	public int LocY { get; set; }

	private RaceRes RaceRes => Sys.Instance.RaceRes;
	private TownRes TownRes => Sys.Instance.TownRes;
	private FirmRes FirmRes => Sys.Instance.FirmRes;
	private TalkRes TalkRes => Sys.Instance.TalkRes;
	private Info Info => Sys.Instance.Info;
	private World World => Sys.Instance.World;
	private NationArray NationArray => Sys.Instance.NationArray;
	private TownArray TownArray => Sys.Instance.TownArray;
	private FirmArray FirmArray => Sys.Instance.FirmArray;
	private UnitArray UnitArray => Sys.Instance.UnitArray;

	public int NewsType()
	{
		return Type % NEWS_TYPE_NUM;
	}

	private string NationName1()
	{
		string str = String.Empty;

		if (NationNameId1 < 0) // human player - custom name
			str += NationArray.GetHumanName(NationNameId1, true) + "'s Kingdom";
		else
			str += RaceRes[NationRaceId1].get_single_name(NationNameId1) + "'s Kingdom";

		//------ add color bar -------//

		str += NationColorStr1();

		return str;
	}

	private string NationName2()
	{
		string str = String.Empty;

		if (NationNameId2 < 0) // human player - custom name
			str += NationArray.GetHumanName(NationNameId2, true) + "'s Kingdom";
		else
			str += RaceRes[NationRaceId2].get_single_name(NationNameId2) + "'s Kingdom";

		//------ add color bar -------//

		str += NationColorStr2();

		return str;
	}

	private string KingName1(bool addColor = false)
	{
		string str;

		if (NationNameId1 < 0) // human player - custom name
			str = NationArray.GetHumanName(NationNameId1);
		else
			str = RaceRes[NationRaceId1].get_name(NationNameId1);

		//------ add color bar -------//

		if (addColor)
			str += NationColorStr1();

		return str;
	}

	private string KingName2(bool addColor = false)
	{
		string str;

		if (NationNameId2 < 0) // human player - custom name
			str = NationArray.GetHumanName(NationNameId2);
		else
			str = RaceRes[NationRaceId2].get_name(NationNameId2);

		//------ add color bar -------//

		if (addColor)
			str += NationColorStr2();

		return str;
	}

	private string NationColorStr1()
	{
		return " @COL" + Convert.ToChar(30 + NationColor1);
	}

	private string NationColorStr2()
	{
		return " @COL" + Convert.ToChar(30 + NationColor2);
	}

	public string RaceName(int raceId)
	{
		return raceId switch
		{
			1 => "Norman",
			2 => "Mayan",
			3 => "Greek",
			4 => "Viking",
			5 => "Persian",
			6 => "Chinese",
			7 => "Japanese",
			8 => "Eqyptian",
			9 => "Mughul",
			10 => "Zulu",
			_ => "Unknown"
		};
	}
	
	private string FirmName(int firmType)
	{
		return firmType switch
		{
			Firm.FIRM_BASE => "Seat of Power",
			Firm.FIRM_FACTORY => "Factory",
			Firm.FIRM_INN => "Inn",
			Firm.FIRM_MARKET => "Market",
			Firm.FIRM_CAMP => "Fort",
			Firm.FIRM_MINE => "Mine",
			Firm.FIRM_RESEARCH => "Tower of Science",
			Firm.FIRM_WAR_FACTORY => "War Factory",
			Firm.FIRM_HARBOR => "Harbor",
			Firm.FIRM_MONSTER => "Fryhtan Lair",
			_ => "Firm"
		};
	}

	public string Message() // return the news msg
	{
		return Id switch
		{
			NEWS_DIPLOMACY => Diplomacy(),
			NEWS_TOWN_REBEL => TownRebel(),
			NEWS_MIGRATE => Migrate(),
			NEWS_NEW_NATION => NewNation(),
			NEWS_NATION_DESTROYED => NationDestroyed(),
			NEWS_NATION_SURRENDER => NationSurrender(),
			NEWS_KING_DIE => KingDie(),
			NEWS_NEW_KING => NewKing(),
			NEWS_FIRM_DESTROYED => FirmDestroyed(),
			NEWS_FIRM_CAPTURED => FirmCaptured(),
			NEWS_TOWN_DESTROYED => TownDestroyed(),
			NEWS_TOWN_ABANDONED => TownAbandoned(),
			NEWS_TOWN_SURRENDERED => TownSurrendered(),
			NEWS_MONSTER_KING_KILLED => MonsterKingKilled(),
			NEWS_MONSTER_FIRM_DESTROYED => MonsterFirmDestroyed(),
			NEWS_SCROLL_ACQUIRED => ScrollAcquired(),
			NEWS_MONSTER_GOLD_ACQUIRED => MonsterGoldAcquired(),
			NEWS_YOUR_SPY_KILLED => YourSpyKilled(),
			NEWS_ENEMY_SPY_KILLED => EnemySpyKilled(),
			NEWS_UNIT_BETRAY => UnitBetray(),
			NEWS_UNIT_ASSASSINATED => UnitAssassinated(),
			NEWS_ASSASSINATOR_CAUGHT => AssassinatorCaught(),
			NEWS_GENERAL_DIE => GeneralDie(),
			NEWS_RAW_EXHAUST => RawExhaust(),
			NEWS_TECH_RESEARCHED => TechResearched(),
			NEWS_LIGHTNING_DAMAGE => LightningDamage(),
			NEWS_EARTHQUAKE_DAMAGE => EarthquakeDamage(),
			NEWS_GOAL_DEADLINE => GoalDeadline(),
			NEWS_WEAPON_SHIP_WORN_OUT => WeaponShipWornOut(),
			NEWS_FIRM_WORN_OUT => FirmWornOut(),
			NEWS_CHAT_MSG => ChatMsg(),
			NEWS_MULTI_RETIRE => MultiRetire(),
			NEWS_MULTI_QUIT_GAME => MultiQuitGame(),
			NEWS_MULTI_SAVE_GAME => MultiSaveGame(),
			NEWS_MULTI_CONNECTION_LOST => MultiConnectionLost(),
			_ => "Unknown news type"
		};
	}

	public bool IsMajor()
	{
		return Id switch
		{
			NEWS_DIPLOMACY => true,
			NEWS_TOWN_REBEL => true,
			NEWS_MIGRATE => false,
			NEWS_NEW_NATION => true,
			NEWS_NATION_DESTROYED => true,
			NEWS_NATION_SURRENDER => true,
			NEWS_KING_DIE => true,
			NEWS_NEW_KING => true,
			NEWS_FIRM_DESTROYED => false,
			NEWS_FIRM_CAPTURED => false,
			NEWS_TOWN_DESTROYED => false,
			NEWS_TOWN_ABANDONED => false,
			NEWS_TOWN_SURRENDERED => false,
			NEWS_MONSTER_KING_KILLED => false,
			NEWS_MONSTER_FIRM_DESTROYED => false,
			NEWS_SCROLL_ACQUIRED => true,
			NEWS_MONSTER_GOLD_ACQUIRED => false,
			NEWS_YOUR_SPY_KILLED => true,
			NEWS_ENEMY_SPY_KILLED => true,
			NEWS_UNIT_BETRAY => true,
			NEWS_UNIT_ASSASSINATED => true,
			NEWS_ASSASSINATOR_CAUGHT => true,
			NEWS_GENERAL_DIE => true,
			NEWS_RAW_EXHAUST => true,
			NEWS_TECH_RESEARCHED => true,
			NEWS_LIGHTNING_DAMAGE => false,
			NEWS_EARTHQUAKE_DAMAGE => true,
			NEWS_GOAL_DEADLINE => true,
			NEWS_WEAPON_SHIP_WORN_OUT => false,
			NEWS_FIRM_WORN_OUT => false,
			NEWS_CHAT_MSG => true,
			NEWS_MULTI_RETIRE => true,
			NEWS_MULTI_QUIT_GAME => true,
			NEWS_MULTI_SAVE_GAME => true,
			NEWS_MULTI_CONNECTION_LOST => true,
			_ => false
		};
	}

	public void SetLoc(int locX, int locY, int locType, int locTypeParam = 0, int locTypeParam2 = 0)
	{
		LocX = locX;
		LocY = locY;
		LocType = locType;
		LocTypeParam = locTypeParam;
		LocTypeParam2 = locTypeParam2;
	}

	public bool IsLocValid()
	{
		if (LocType == 0)
			return false;

		bool rc = false;

		if (LocType == NEWS_LOC_TOWN)
		{
			if (!TownArray.IsDeleted(LocTypeParam))
			{
				Town town = TownArray[LocTypeParam];

				rc = town.LocCenterX == LocX && town.LocCenterY == LocY;
			}
		}
		else if (LocType == NEWS_LOC_FIRM)
		{
			if (!FirmArray.IsDeleted(LocTypeParam))
			{
				Firm firm = FirmArray[LocTypeParam];

				rc = firm.LocCenterX == LocX && firm.LocCenterY == LocY;
			}
		}
		else if (LocType == NEWS_LOC_UNIT)
		{
			if (!UnitArray.IsDeleted(LocTypeParam))
			{
				Unit unit = UnitArray[LocTypeParam];

				if (unit.NameId == LocTypeParam2)
				{
					//--- if the unit is no longer belong to our nation ----//
					//--- only keep track of the unit for one month --------//

					if (unit.NationId == NationArray.PlayerId || Info.GameDate < NewsDate.AddDays(30.0))
					{
						if (unit.GetNextLoc(out int locX, out int locY))
						{
							LocX = locX;
							LocY = locY;
							
							Location location = World.GetLoc(LocX, LocY);
							rc = location.VisitLevel > 0;
						}
					}
				}
			}
		}
		else if (LocType == NEWS_LOC_ANY)
		{
			rc = true;
		}

		if (!rc)
			LocType = 0;

		return rc;
	}

	private string Diplomacy()
	{
		TalkMsg talkMsg = TalkRes.get_talk_msg(Param1);
		return talkMsg.msg_str(NationArray.PlayerId);
	}

	private string TownRebel()
	{
		if (Param2 == 1)
		{
			return $"{Param2} peasant in {Param6} in {KingName1()}'s Kingdom {NationColorStr1()} is rebelling.";
		}
		else
		{
			return $"{Param2} peasants in {Param6} in {KingName1()}'s Kingdom {NationColorStr1()} are rebelling.";
		}
	}

	private string Migrate()
	{
		if (NationArray.PlayerId != 0 && NationNameId1 == NationArray.Player.NationNameId) // from player nation to another nation
		{
			if (NationNameId2 != 0) // only if it is not an independent town
			{
				if (Param5 != 0)
				{
					if (Param4 == 1)
					{
						return $"{Param4} {RaceRes[Param3].name} {FirmRes[Param5].WorkerTitle} has emigrated from your village of {Param6} to {Param7} in {KingName2()}'s Kingdom {NationColor2}.";
					}
					else
					{
						return $"{Param4} {RaceRes[Param3].name} {FirmRes[Param5].WorkerTitle}s have emigrated from your village of {Param6} to {Param7} in {KingName2()}'s Kingdom {NationColor2}.";
					}
				}
				else
				{
					if (Param4 == 1)
					{
						return $"{Param4} {RaceRes[Param3].name} peasant has emigrated from your village of {Param6} to {Param7} in {KingName2()}'s Kingdom {NationColor2}.";
					}
					else
					{
						return $"{Param4} {RaceRes[Param3].name} peasants have emigrated from your village of {Param6} to {Param7} in {KingName2()}'s Kingdom {NationColor2}.";
					}
				}
			}
			else
			{
				if (Param5 != 0)
				{
					if (Param4 == 1)
					{
						return $"{Param4} {RaceRes[Param3].name} {FirmRes[Param5].WorkerTitle} has emigrated from your village of {Param6} to {Param7}.";
					}
					else
					{
						return $"{Param4} {RaceRes[Param3].name} {FirmRes[Param5].WorkerTitle}s have emigrated from your village of {Param6} to {Param7}.";
					}
				}
				else
				{
					if (Param4 == 1)
					{
						return $"{Param4} {RaceRes[Param3].name} peasant has emigrated from your village of {Param6} to {Param7}.";
					}
					else
					{
						return $"{Param4} {RaceRes[Param3].name} peasants have emigrated from your village of {Param6} to {Param7}.";
					}
				}
			}
		}
		else
		{
			if (NationNameId1 != 0) // only if it is not an independent town
			{
				if (Param5 != 0)
				{
					if (Param4 == 1)
					{
						return $"{Param4} {RaceRes[Param3].name} {FirmRes[Param5].WorkerTitle} has immigrated from {Param6} in {KingName1()}'s Kingdom {NationColor1} to your village of {Param7}.";
					}
					else
					{
						return $"{Param4} {RaceRes[Param3].name} {FirmRes[Param5].WorkerTitle}s have immigrated from {Param6} in {KingName1()}'s Kingdom {NationColor1} to your village of {Param7}.";
					}
				}
				else
				{
					if (Param4 == 1)
					{
						return $"{Param4} {RaceRes[Param3].name} peasant has immigrated from {Param6} in {KingName1()}'s Kingdom {NationColor1} to your village of {Param7}.";
					}
					else
					{
						return $"{Param4} {RaceRes[Param3].name} peasants have immigrated from {Param6} in {KingName1()}'s Kingdom {NationColor1} to your village of {Param7}.";
					}
				}
			}
			else
			{
				if (Param5 != 0)
				{
					if (Param4 == 1)
					{
						return $"{Param4} {RaceRes[Param3].name} {FirmRes[Param5].WorkerTitle} has immigrated from {Param6} to your village of {Param7}.";
					}
					else
					{
						return $"{Param4} {RaceRes[Param3].name} {FirmRes[Param5].WorkerTitle}s have immigrated from {Param6} to your village of {Param7}.";
					}
				}
				else
				{
					if (Param4 == 1)
					{
						return $"{Param4} {RaceRes[Param3].name} peasant has immigrated from {Param6} to your village of {Param7}.";
					}
					else
					{
						return $"{Param4} {RaceRes[Param3].name} peasants have immigrated from {Param6} to your village of {Param7}.";
					}
				}
			}
		}
	}

	private string NewNation()
	{
		return $"A new Kingdom has emerged under the leadership of {KingName1(true)}.";
	}

	private string NationDestroyed()
	{
		return $"{KingName1()}'s Kingdom {NationColorStr1()} has been destroyed.";
	}

	private string NationSurrender()
	{
		if (NationArray.PlayerId != 0 && NationNameId2 == NationArray.Player.NationNameId)
		{
			return $"{KingName1()}'s Kingdom {NationColorStr1()} has surrendered to you.";
		}
		else
		{
			return $"{KingName1()}'s Kingdom {NationColorStr1()} has surrendered to {KingName2()}'s Kingdom {NationColorStr2()}.";
		}
	}

	private string KingDie()
	{
		if (NationArray.PlayerId != 0 && NationNameId1 == NationArray.Player.NationNameId)
		{
			return $"Your King, {KingName1()}, has been slain.";
		}
		else
		{
			return $"King {KingName1()} of {KingName1()}'s Kingdom {NationColorStr1()} has been slain.";
		}
	}

	private string NewKing()
	{
		if (NationArray.PlayerId != 0 && NationNameId1 == NationArray.Player.NationNameId)
		{
			return $"{RaceRes[Param1].get_name(Param2)} has ascended the throne as your new King.";
		}
		else
		{
			return $"{RaceRes[Param1].get_name(Param2)} has ascended the throne as the new King of {KingName1()}'s Kingdom {NationColorStr1()}.";
		}
	}

	private string FirmDestroyed()
	{
		return Param3 switch
		{
			DESTROYER_NATION => $"Your {FirmName(Param1)} near {Param6} has been destroyed by {KingName2()}'s Kingdom {NationColorStr2()}.",
			DESTROYER_REBEL => $"Your {FirmName(Param1)} near {Param6} has been destroyed by Rebels.",
			DESTROYER_MONSTER => $"Your {FirmName(Param1)} near {Param6} has been destroyed by Fryhtans.",
			_ => $"Your {FirmName(Param1)} near {Param6} has been destroyed."
		};
	}

	private string FirmCaptured()
	{
		if (Param3 != 0)
		{
			return $"Your {FirmName(Param1)} near {Param6} has been captured by a spy from {KingName2()}'s Kingdom {NationColorStr2()}.";
		}
		else
		{
			return $"Your {FirmName(Param1)} near {Param6} has been captured by {KingName2()}'s Kingdom {NationColorStr2()}.";
		}
	}

	private string TownDestroyed()
	{
		return Param2 switch
		{
			DESTROYER_NATION => $"Your village of {Param6} has been destroyed by {KingName2()}'s Kingdom {NationColorStr2()}.",
			DESTROYER_REBEL => $"Your village of {Param6} has been destroyed by Rebels.",
			DESTROYER_MONSTER => $"Your village of {Param6} has been destroyed by Fryhtans.",
			_ => $"Your village of {Param6} has been destroyed."
		};
	}

	private string TownAbandoned()
	{
		return $"Your village of {Param6} has been abandoned by its people.";
	}

	private string TownSurrendered()
	{
		if (NationArray.PlayerId != 0 && NationNameId2 == NationArray.Player.NationNameId)
		{
			return $"Your village of {Param6} has surrendered to {KingName1()}'s Kingdom {NationColorStr1()}.";
		}
		
		if (NationNameId2 != 0)
		{
			return $"The village of {Param6} in {KingName2()}'s Kingdom {NationColorStr2()} has surrendered to you.";
		}

		return $"The independent village of {Param6} has surrendered to you.";
	}

	private string MonsterKingKilled()
	{
		return $"An {UnitMonster.MonsterKingNames[Param1 - 1]} has been slain.";
	}

	private string MonsterFirmDestroyed()
	{
		return $"A {FirmMonster.MonsterFirmName[Param1 - 1]} has been destroyed.";
	}

	private string ScrollAcquired()
	{
		if (NationArray.PlayerId != 0 && NationNameId1 == NationArray.Player.NationNameId)
		{
			return $"You have acquired the {Param1} Scroll of Power.";
		}
		else
		{
			return $"{KingName1()}'s Kingdom {NationColorStr1()} has acquired the {Param1} Scroll of Power.";
		}
	}

	private string MonsterGoldAcquired()
	{
		return $"You have recovered {Param1} worth of treasure from the Fryhtans.";
	}

	private string YourSpyKilled()
	{
		if (Param3 == Spy.SPY_FIRM)
		{
			if (NationNameId2 != 0)
			{
				return $"Your spy has been exposed and executed on his mission to a {FirmName(Param1)} near {Param6} in {KingName2()}'s Kingdom {NationColorStr2()}.";
			}
			else
			{
				return $"Your spy has been exposed and executed on his mission to a {FirmName(Param1)} near {Param6}.";
			}
		}
		
		if (Param3 == Spy.SPY_TOWN)
		{
			if (NationNameId2 != 0)
			{
				return $"Your spy has been exposed and executed on his mission to {Param6} in {KingName2()}'s Kingdom {NationColorStr2()}.";
			}
			else
			{
				return $"Your spy has been exposed and executed on his mission to {Param6}.";
			}
		}
		
		if (Param3 == Spy.SPY_MOBILE)
		{
			if (NationNameId2 != 0)
			{
				return $"Your spy {RaceRes[Param1].get_name(Param2)} has been exposed and executed on his mission to {KingName2()}'s Kingdom {NationColorStr2()}.";
			}
			else
			{
				return $"Your spy {RaceRes[Param1].get_name(Param2)} has been exposed and executed on his mission.";
			}
		}

		return String.Empty;
	}

	private string EnemySpyKilled()
	{
		if (Param3 == Spy.SPY_FIRM)
		{
			return $"A spy from {KingName2()}'s Kingdom {NationColorStr2()} has been uncovered and executed in your {FirmName(Param1)} near {Param6}.";
		}

		if (Param3 == Spy.SPY_TOWN)
		{
			return $"A spy from {KingName2()}'s Kingdom {NationColorStr2()} has been uncovered and executed in your village of {Param6}.";
		}

		if (Param3 == Spy.SPY_MOBILE)
		{
			return $"Spy {RaceRes[Param1].get_name(Param2)} from {KingName2()}'s Kingdom {NationColorStr2()} has been uncovered and executed.";
		}
		
		return String.Empty;
	}

	private string UnitBetray()
	{
		return String.Empty;
	}

	private string UnitAssassinated()
	{
		return String.Empty;
	}

	private string AssassinatorCaught()
	{
		return String.Empty;
	}

	private string GeneralDie()
	{
		return String.Empty;
	}

	private string RawExhaust()
	{
		return String.Empty;
	}

	private string TechResearched()
	{
		return String.Empty;
	}

	private string LightningDamage()
	{
		return String.Empty;
	}

	private string EarthquakeDamage()
	{
		return String.Empty;
	}

	private string GoalDeadline()
	{
		return String.Empty;
	}

	private string WeaponShipWornOut()
	{
		return String.Empty;
	}

	private string FirmWornOut()
	{
		return String.Empty;
	}

	private string ChatMsg()
	{
		return String.Empty;
	}

	private string MultiRetire()
	{
		return String.Empty;
	}

	private string MultiQuitGame()
	{
		return String.Empty;
	}

	private string MultiSaveGame()
	{
		return String.Empty;
	}

	private string MultiConnectionLost()
	{
		return String.Empty;
	}
}