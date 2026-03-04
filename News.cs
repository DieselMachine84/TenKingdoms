using System;
using System.IO;

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
	public string Param6 { get; set; } = String.Empty;
	public string Param7 { get; set; } = String.Empty;

	public int LocX { get; private set; }
	public int LocY { get; private set; }
	public int LocType { get; set; }
	private int LocTypeParam { get; set; }
	private int LocTypeParam2 { get; set; } 

	private RaceRes RaceRes => Sys.Instance.RaceRes;
	private TechRes TechRes => Sys.Instance.TechRes;
	private FirmRes FirmRes => Sys.Instance.FirmRes;
	private UnitRes UnitRes => Sys.Instance.UnitRes;
	private Info Info => Sys.Instance.Info;
	private World World => Sys.Instance.World;
	private NationArray NationArray => Sys.Instance.NationArray;
	private TownArray TownArray => Sys.Instance.TownArray;
	private FirmArray FirmArray => Sys.Instance.FirmArray;
	private UnitArray UnitArray => Sys.Instance.UnitArray;
	private TalkMsgArray TalkMsgArray => Sys.Instance.TalkMsgArray;

	public int NewsType()
	{
		return Type % NEWS_TYPE_NUM;
	}

	private string KingName1(bool addColor = false)
	{
		string str;

		if (NationNameId1 < 0) // human player - custom name
			str = NationArray.GetHumanName(NationNameId1);
		else
			str = RaceRes[NationRaceId1].GetName(NationNameId1);

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
			str = RaceRes[NationRaceId2].GetName(NationNameId2);

		//------ add color bar -------//

		if (addColor)
			str += NationColorStr2();

		return str;
	}

	private string NationColorStr1()
	{
		return " @COL" + Convert.ToChar(48 + NationColor1);
	}

	private string NationColorStr2()
	{
		return " @COL" + Convert.ToChar(48 + NationColor2);
	}

	private string RaceName(int raceId)
	{
		return raceId switch
		{
			RaceRes.RACE_NORMAN => "Norman",
			RaceRes.RACE_MAYA => "Mayan",
			RaceRes.RACE_GREEK => "Greek",
			RaceRes.RACE_VIKING => "Viking",
			RaceRes.RACE_PERSIAN => "Persian",
			RaceRes.RACE_CHINESE => "Chinese",
			RaceRes.RACE_JAPANESE => "Japanese",
			RaceRes.RACE_EGYPTIAN => "Egyptian",
			RaceRes.RACE_INDIAN => "Mughal",
			RaceRes.RACE_ZULU => "Zulu",
			_ => "Unknown"
		};
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

	private string WarMachineName(int unitId)
	{
		return unitId switch
		{
			UnitConstants.UNIT_CATAPULT => "Catapult",
			UnitConstants.UNIT_BALLISTA => "Ballista",
			UnitConstants.UNIT_FLAMETHROWER => "Spitfire",
			UnitConstants.UNIT_CANNON => "Cannon",
			UnitConstants.UNIT_EXPLOSIVE_CART => "Porcupine",
			UnitConstants.UNIT_VESSEL => "Trader",
			UnitConstants.UNIT_TRANSPORT => "Transport",
			UnitConstants.UNIT_CARAVEL => "Caravel",
			UnitConstants.UNIT_GALLEON => "Galleon",
			UnitConstants.UNIT_F_BALLISTA => "Unicorn",
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

	private string RawName(int rawType)
	{
		return rawType switch
		{
			1 => "Clay",
			2 => "Copper",
			3 => "Iron",
			_ => "Raw Resource"
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

		//TODO town and firm location should always be valid
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
		TalkMsg talkMsg = TalkMsgArray.GetTalkMsg(Param1);
		return talkMsg.Message(NationArray.PlayerId);
	}

	private string TownRebel()
	{
		if (Param2 == 1)
			return $"{Param2} peasant in {Param6} in {KingName1()}'s Kingdom{NationColorStr1()} is rebelling.";
		else
			return $"{Param2} peasants in {Param6} in {KingName1()}'s Kingdom{NationColorStr1()} are rebelling.";
	}

	private string Migrate()
	{
		if (NationArray.Player != null && NationNameId1 == NationArray.Player.NationNameId) // from player nation to another nation
		{
			if (NationNameId2 != 0) // only if it is not an independent town
			{
				if (Param5 != 0)
				{
					if (Param4 == 1)
						return $"{Param4} {RaceRes[Param3].Name} {FirmRes[Param5].WorkerTitle} has emigrated from your village of {Param6} to {Param7} in {KingName2()}'s Kingdom.{NationColorStr2()}";
					else
						return $"{Param4} {RaceRes[Param3].Name} {FirmRes[Param5].WorkerTitle}s have emigrated from your village of {Param6} to {Param7} in {KingName2()}'s Kingdom.{NationColorStr2()}";
				}
				else
				{
					if (Param4 == 1)
						return $"{Param4} {RaceRes[Param3].Name} peasant has emigrated from your village of {Param6} to {Param7} in {KingName2()}'s Kingdom.{NationColorStr2()}";
					else
						return $"{Param4} {RaceRes[Param3].Name} peasants have emigrated from your village of {Param6} to {Param7} in {KingName2()}'s Kingdom.{NationColorStr2()}";
				}
			}
			else
			{
				if (Param5 != 0)
				{
					if (Param4 == 1)
						return $"{Param4} {RaceRes[Param3].Name} {FirmRes[Param5].WorkerTitle} has emigrated from your village of {Param6} to {Param7}.";
					else
						return $"{Param4} {RaceRes[Param3].Name} {FirmRes[Param5].WorkerTitle}s have emigrated from your village of {Param6} to {Param7}.";
				}
				else
				{
					if (Param4 == 1)
						return $"{Param4} {RaceRes[Param3].Name} peasant has emigrated from your village of {Param6} to {Param7}.";
					else
						return $"{Param4} {RaceRes[Param3].Name} peasants have emigrated from your village of {Param6} to {Param7}.";
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
						return $"{Param4} {RaceRes[Param3].Name} {FirmRes[Param5].WorkerTitle} has immigrated from {Param6} in {KingName1()}'s Kingdom{NationColorStr1()} to your village of {Param7}.";
					else
						return $"{Param4} {RaceRes[Param3].Name} {FirmRes[Param5].WorkerTitle}s have immigrated from {Param6} in {KingName1()}'s Kingdom{NationColorStr1()} to your village of {Param7}.";
				}
				else
				{
					if (Param4 == 1)
						return $"{Param4} {RaceRes[Param3].Name} peasant has immigrated from {Param6} in {KingName1()}'s Kingdom{NationColorStr1()} to your village of {Param7}.";
					else
						return $"{Param4} {RaceRes[Param3].Name} peasants have immigrated from {Param6} in {KingName1()}'s Kingdom{NationColorStr1()} to your village of {Param7}.";
				}
			}
			else
			{
				if (Param5 != 0)
				{
					if (Param4 == 1)
						return $"{Param4} {RaceRes[Param3].Name} {FirmRes[Param5].WorkerTitle} has immigrated from {Param6} to your village of {Param7}.";
					else
						return $"{Param4} {RaceRes[Param3].Name} {FirmRes[Param5].WorkerTitle}s have immigrated from {Param6} to your village of {Param7}.";
				}
				else
				{
					if (Param4 == 1)
						return $"{Param4} {RaceRes[Param3].Name} peasant has immigrated from {Param6} to your village of {Param7}.";
					else
						return $"{Param4} {RaceRes[Param3].Name} peasants have immigrated from {Param6} to your village of {Param7}.";
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
		return $"{KingName1()}'s Kingdom{NationColorStr1()} has been destroyed.";
	}

	private string NationSurrender()
	{
		if (NationArray.Player != null && NationNameId2 == NationArray.Player.NationNameId)
			return $"{KingName1()}'s Kingdom{NationColorStr1()} has surrendered to you.";
		else
			return $"{KingName1()}'s Kingdom{NationColorStr1()} has surrendered to {KingName2()}'s Kingdom.{NationColorStr2()}";
	}

	private string KingDie()
	{
		if (NationArray.Player != null && NationNameId1 == NationArray.Player.NationNameId)
			return $"Your King, {KingName1()}, has been slain.";
		else
			return $"King {KingName1()} of {KingName1()}'s Kingdom{NationColorStr1()} has been slain.";
	}

	private string NewKing()
	{
		if (NationArray.Player != null && NationNameId1 == NationArray.Player.NationNameId)
			return $"{RaceRes[Param1].GetName(Param2)} has ascended the throne as your new King.";
		else
			return $"{RaceRes[Param1].GetName(Param2)} has ascended the throne as the new King of {KingName1()}'s Kingdom.{NationColorStr1()}";
	}

	private string FirmDestroyed()
	{
		return Param3 switch
		{
			DESTROYER_NATION => $"Your {FirmName(Param1)} near {Param6} has been destroyed by {KingName2()}'s Kingdom.{NationColorStr2()}",
			DESTROYER_REBEL => $"Your {FirmName(Param1)} near {Param6} has been destroyed by Rebels.",
			DESTROYER_MONSTER => $"Your {FirmName(Param1)} near {Param6} has been destroyed by Fryhtans.",
			_ => $"Your {FirmName(Param1)} near {Param6} has been destroyed."
		};
	}

	private string FirmCaptured()
	{
		if (Param3 != 0)
			return $"Your {FirmName(Param1)} near {Param6} has been captured by a spy from {KingName2()}'s Kingdom.{NationColorStr2()}";
		else
			return $"Your {FirmName(Param1)} near {Param6} has been captured by {KingName2()}'s Kingdom.{NationColorStr2()}";
	}

	private string TownDestroyed()
	{
		return Param2 switch
		{
			DESTROYER_NATION => $"Your village of {Param6} has been destroyed by {KingName2()}'s Kingdom.{NationColorStr2()}",
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
		if (NationArray.Player != null && NationNameId2 == NationArray.Player.NationNameId)
			return $"Your village of {Param6} has surrendered to {KingName1()}'s Kingdom.{NationColorStr1()}";
		
		if (NationNameId2 != 0)
			return $"The village of {Param6} in {KingName2()}'s Kingdom{NationColorStr2()} has surrendered to you.";

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
		if (NationArray.Player != null && NationNameId1 == NationArray.Player.NationNameId)
			return $"You have acquired the {RaceName(Param1)} Scroll of Power.";
		else
			return $"{KingName1()}'s Kingdom{NationColorStr1()} has acquired the {RaceName(Param1)} Scroll of Power.";
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
				return $"Your spy has been exposed and executed on his mission to a {FirmName(Param1)} near {Param6} in {KingName2()}'s Kingdom.{NationColorStr2()}";
			else
				return $"Your spy has been exposed and executed on his mission to a {FirmName(Param1)} near {Param6}.";
		}
		
		if (Param3 == Spy.SPY_TOWN)
		{
			if (NationNameId2 != 0)
				return $"Your spy has been exposed and executed on his mission to {Param6} in {KingName2()}'s Kingdom.{NationColorStr2()}";
			else
				return $"Your spy has been exposed and executed on his mission to {Param6}.";
		}
		
		if (Param3 == Spy.SPY_MOBILE)
		{
			if (NationNameId2 != 0)
				return $"Your spy {RaceRes[Param1].GetName(Param2)} has been exposed and executed on his mission to {KingName2()}'s Kingdom.{NationColorStr2()}";
			else
				return $"Your spy {RaceRes[Param1].GetName(Param2)} has been exposed and executed on his mission.";
		}

		return String.Empty;
	}

	private string EnemySpyKilled()
	{
		if (Param3 == Spy.SPY_FIRM)
			return $"A spy from {KingName2()}'s Kingdom{NationColorStr2()} has been uncovered and executed in your {FirmName(Param1)} near {Param6}.";

		if (Param3 == Spy.SPY_TOWN)
			return $"A spy from {KingName2()}'s Kingdom{NationColorStr2()} has been uncovered and executed in your village of {Param6}.";

		if (Param3 == Spy.SPY_MOBILE)
			return $"Spy {RaceRes[Param1].GetName(Param2)} from {KingName2()}'s Kingdom{NationColorStr2()} has been uncovered and executed.";
		
		return String.Empty;
	}

	private string UnitBetray()
	{
		if (NationNameId1 == 0) // independent unit joining your force
			return $"Independent unit {RaceRes[Param1].GetName(Param2)} has joined your force.";

		if (NationNameId2 == 0) // became an independent unit
		{
			if (Param3 == Unit.RANK_GENERAL)
				return $"General {RaceRes[Param1].GetName(Param2)} has renounced you and become independent.";
			else
				return $"{RaceRes[Param1].GetName(Param2)} has renounced you and become independent.";
		}
		else
		{
			if (NationArray.Player != null && NationNameId1 == NationArray.Player.NationNameId)
			{
				if (Param3 == Unit.RANK_GENERAL)
					return $"General {RaceRes[Param1].GetName(Param2)} has betrayed you and turned towards {KingName2()}'s Kingdom.{NationColorStr2()}";
				else
					return $"{RaceRes[Param1].GetName(Param2)} has betrayed you and turned towards {KingName2()}'s Kingdom.{NationColorStr2()}";
			}
			else
			{
				if (Param3 == Unit.RANK_GENERAL)
					return $"General {RaceRes[Param1].GetName(Param2)} of {KingName1()}'s Kingdom{NationColorStr1()} has defected to your forces.";
				else
					return $"{RaceRes[Param1].GetName(Param2)} of {KingName1()}'s Kingdom{NationColorStr1()} has defected to your forces.";
			}
		}
	}

	private string UnitAssassinated()
	{
		string result;

		if (Param3 == Unit.RANK_KING)
			result = $"Your King, {RaceRes[Param1].GetSingleName(Param2)}, has been assassinated by an enemy spy.";
		else
			result = $"Your general, {RaceRes[Param1].GetSingleName(Param2)}, has been assassinated by an enemy spy.";

		if (Param4 != 0)
			result += " The enemy spy has been killed.";

		return result;
	}

	private string AssassinatorCaught()
	{
		if (Param1 == Unit.RANK_KING)
			return $"An enemy spy has been killed while attempting to assassinate your King.";
		else
			return $"An enemy spy has been killed while attempting to assassinate your General.";
	}

	private string GeneralDie()
	{
		return $"Your general, {RaceRes[Param1].GetSingleName(Param2)}, has been slain.";
	}

	private string RawExhaust()
	{
		return $"Your {RawName(Param1)} Mine has exhausted its {RawName(Param1)} deposit.";
	}

	private string TechResearched()
	{
		if (TechRes[Param1].MaxTechLevel > 1) // if the tech has more than one level
			return $"Your scientists have finished their {TechName(Param1)} Mark {Misc.RomanNumber(Param2)} research.";
		else
			return $"Your scientists have finished their {TechName(Param1)} research.";
	}

	private string LightningDamage()
	{
		switch (Param1)
		{
			case NEWS_LOC_UNIT:
				string unitName = Param2 > 0 ? RaceRes[Param2].GetName(Param3) : UnitRes[Param3].Name;

				if (Param4 == Unit.RANK_GENERAL)
				{
					if (Param5 != 0)
						return $"Your General {unitName} has been struck and killed by lightning.";
					else
						return $"Your General {unitName} has been struck and injured by lightning.";
				}
				else if (Param4 == Unit.RANK_KING)
				{
					if (Param5 != 0)
						return "Your King has been struck and killed by lightning.";
					else
						return "Your King has been struck and injured by lightning.";
				}
				else
				{
					if (Param5 != 0)
						return $"Your unit {unitName} has been struck and killed by lightning.";
					else
						return $"Your unit {unitName} has been struck and injured by lightning.";
				}

			case NEWS_LOC_FIRM:
				if (!String.IsNullOrEmpty(Param6))
				{
					if (Param5 != 0)
						return $"Your {FirmName(Param2)} near {Param6} has been destroyed by lightning.";
					else
						return $"Your {FirmName(Param2)} near {Param6} has been struck by lightning.";
				}
				else
				{
					if (Param5 != 0)
						return $"Your {FirmName(Param2)} has been destroyed by lightning.";
					else
						return $"Your {FirmName(Param2)} has been struck by lightning.";
				}

			case NEWS_LOC_TOWN:
				if (Param5 != 0)
					return $"Your village {Param6} has been destroyed by lightning.";
				else
					return $"Your village {Param6} has been struck by lightning.";

			default:
				return "Something has been struck by lightning.";
		}
	}

	private string EarthquakeDamage()
	{
		if (Param1 == 1)
		{
			if (Param3 > 0)
			{
				if (Param2 == 1)
					return $"{Param2} of your units has been injured and {Param3} killed in an earthquake.";
				else
					return $"{Param2} of your units have been injured and {Param3} killed in an earthquake.";
			}
			else
			{
				if (Param2 == 1)
					return $"{Param2} of your units has been injured in an earthquake.";
				else
					return $"{Param2} of your units have been injured in an earthquake.";
			}
		}

		if (Param1 == 2)
		{
			if (Param2 == 1)
				return $"{Param2} of your villagers has been killed in an earthquake.";
			else
				return $"{Param2} of your villagers have been killed in an earthquake.";
		}

		if (Param1 == 3)
		{
			if (Param3 > 0)
			{
				if (Param2 == 1)
					return $"{Param2} of your buildings has been damaged and {Param3} destroyed in an earthquake.";
				else
					return $"{Param2} of your buildings have been damaged and {Param3} destroyed in an earthquake.";
			}
			else
			{
				if (Param2 == 1)
					return $"{Param2} of your buildings has been damaged in an earthquake.";
				else
					return $"{Param2} of your buildings have been damaged in an earthquake.";
			}
		}

		return "Something has been damaged in an earthquake.";
	}

	private string GoalDeadline()
	{
		string result = "Make haste! ";

		if (Param1 != 0 && Param2 == 0)
		{
			if (Param1 == 1)
				result += $"You have only {Param1} year left to achieve your goal.";
			else
				result += $"You have only {Param1} years left to achieve your goal.";

			return result;
		}

		if (Param1 == 0 && Param2 != 0)
		{
			if (Param2 == 1)
				result += $"You have only {Param2} month left to achieve your goal.";
			else
				result += $"You have only {Param2} months left to achieve your goal.";

			return result;
		}

		if (Param1 == 1)
			result += $"You have only {Param1} year";
		else
			result += $"You have only {Param1} years";

		if (Param2 == 1)
			result += $" and {Param2} month left to achieve your goal.";
		else
			result += $" and {Param2} months left to achieve your goal.";

		return result;
	}

	private string WeaponShipWornOut()
	{
		if( Param2 != 0 )
			return $"A {WarMachineName(Param1)} {Misc.RomanNumber(Param2)} of yours has broken down due to the lack of maintenance funds.";
		else
			return $"A {WarMachineName(Param1)} of yours has broken down due to the lack of maintenance funds.";
	}

	private string FirmWornOut()
	{
		return $"Your {FirmName(Param1)} near {Param6} has fallen into disrepair due to the lack of maintenance funds.";
	}

	private string ChatMsg()
	{
		return String.Empty;
	}

	private string MultiRetire()
	{
		return $"{KingName1()}'s Kingdom{NationColorStr1()} has retired and quit the game.";
	}

	private string MultiQuitGame()
	{
		return $"{KingName1()}'s Kingdom{NationColorStr1()} has quit the game.";
	}

	private string MultiSaveGame()
	{
		return String.Empty;
	}

	private string MultiConnectionLost()
	{
		return $"The connection with {KingName1()}'s Kingdom{NationColorStr1()} has been lost.";
	}
	
	#region SaveAndLoad

	public void SaveTo(BinaryWriter writer)
	{
		writer.Write(Id);
		writer.Write(Type);
		writer.Write(NewsDate.ToBinary());
		writer.Write(NationColor1);
		writer.Write(NationColor2);
		writer.Write(NationRaceId1);
		writer.Write(NationRaceId2);
		writer.Write(NationNameId1);
		writer.Write(NationNameId2);
		writer.Write(Param1);
		writer.Write(Param2);
		writer.Write(Param3);
		writer.Write(Param4);
		writer.Write(Param5);
		writer.Write(Param6);
		writer.Write(Param7);
		writer.Write(LocX);
		writer.Write(LocY);
		writer.Write(LocType);
		writer.Write(LocTypeParam);
		writer.Write(LocTypeParam2);
	}

	public void LoadFrom(BinaryReader reader)
	{
		Id = reader.ReadInt32();
		Type = reader.ReadInt32();
		NewsDate = DateTime.FromBinary(reader.ReadInt64());
		NationColor1 = reader.ReadInt32();
		NationColor2 = reader.ReadInt32();
		NationRaceId1 = reader.ReadInt32();
		NationRaceId2 = reader.ReadInt32();
		NationNameId1 = reader.ReadInt32();
		NationNameId2 = reader.ReadInt32();
		Param1 = reader.ReadInt32();
		Param2 = reader.ReadInt32();
		Param3 = reader.ReadInt32();
		Param4 = reader.ReadInt32();
		Param5 = reader.ReadInt32();
		Param6 = reader.ReadString();
		Param7 = reader.ReadString();
		LocX = reader.ReadInt32();
		LocY = reader.ReadInt32();
		LocType = reader.ReadInt32();
		LocTypeParam = reader.ReadInt32();
		LocTypeParam2 = reader.ReadInt32();
	}
	
	#endregion
}