using System;

namespace TenKingdoms;

public class NationArray : DynArray<Nation>
{
	public int NationCount { get; set; } // no. of nations, it's different from nation_array.size() which is a DynArrayB
	public int AINationCount { get; set; }
	public DateTime LastNewNationDate { get; set; }
	public DateTime LastDelNationDate { get; set; }

	public int MaxNationPopulation { get; set; } // the maximum population in a nation
	public int AllNationPopulation { get; set; } // total population of all nations.

	public int IndependentTownCount { get; set; }

	// the no. of independent towns each race has
	public int[] IndependentTownCountRaces { get; } = new int[GameConstants.MAX_RACE];

	public int MaxNationUnits { get; set; }
	public int MaxNationHumans { get; set; }
	public int MaxNationGenerals { get; set; }
	public int MaxNationWeapons { get; set; }
	public int MaxNationShips { get; set; }
	public int MaxNationSpies { get; set; }

	public int MaxNationFirms { get; set; }
	public int MaxNationTechLevel { get; set; }

	public int MaxPopulationRating { get; set; }
	public int MaxMilitaryRating { get; set; }
	public int MaxEconomicRating { get; set; }
	public int MaxReputation { get; set; }
	public int MaxKillMonsterScore { get; set; }
	public int MaxOverallRating { get; set; }

	public int MaxPopulationNationId { get; set; }
	public int MaxMilitaryNationId { get; set; }
	public int MaxEconomicNationId { get; set; }
	public int MaxReputationNationId { get; set; }
	public int MaxKillMonsterNationId { get; set; }
	public int MaxOverallNationId { get; set; }

	public int LastAllianceId { get; set; }
	public int NationPeaceDays { get; set; } // continuous peace among nations

	public int PlayerId { get; set; }
	public Nation Player { get; set; }

	public byte[] NationColors { get; } = new byte[GameConstants.MAX_NATION + 1];
	public byte[] NationPowerColors { get; } = new byte[GameConstants.MAX_NATION + 2];

	public string[] HumanNames { get; } = new string[GameConstants.MAX_NATION];

	private Config Config => Sys.Instance.Config;
	private ConfigAdv ConfigAdv => Sys.Instance.ConfigAdv;
	private Info Info => Sys.Instance.Info;
	private UnitRes UnitRes => Sys.Instance.UnitRes;
	private FirmArray FirmArray => Sys.Instance.FirmArray;
	private TownArray TownArray => Sys.Instance.TownArray;
	private UnitArray UnitArray => Sys.Instance.UnitArray;
	private SpyArray SpyArray => Sys.Instance.SpyArray;

	public NationArray()
	{
		NationCount = 0;
		AINationCount = 0;

		LastDelNationDate = default;
		NationPeaceDays = 0;
		LastAllianceId = 0;

		NationColors[0] = Colors.V_WHITE; // if nation_recno==0, the color is white
		NationPowerColors[0] = Colors.VGA_GRAY + 10; // if nation_recno==0, the color is white
		// if Location::power_nation_recno==MAX_NATION+1, this means there are more than one nation have influence over this location.
		NationPowerColors[GameConstants.MAX_NATION + 1] = Colors.VGA_GRAY + 10;
	}

	protected override Nation CreateNewObject(int objectType)
	{
		return new Nation();
	}

	protected override int GetNextId()
	{
		int freeId = 0;
		bool idUsed = true;
		while (idUsed)
		{
			freeId++;
			idUsed = false;

			foreach (Nation nation in this)
			{
				if (nation.NationId == freeId)
				{
					idUsed = true;
				}
			}
		}

		return freeId;
	}

	public void DeleteNation(Nation nation)
	{
		NationCount--;

		if (nation.NationType == NationBase.NATION_AI)
			AINationCount--;
		
		ColorRemap.ColorSchemes[nation.NationId] = 0;

		LastDelNationDate = Info.GameDate;

		int nationRecno = nation.NationId;
		nation.Deinit();
		Delete(nationRecno);

		//---- if the nation to be deleted is the player's nation ---//

		if (nationRecno == PlayerId)
		{
			Player = null;
			PlayerId = 0;

			// set the view mode to normal mode to prevent possible problems
			Sys.Instance.set_view_mode(InternalConstants.MODE_NORMAL);
		}

		//---------- update statistic ----------//

		UpdateStatistic(); // as max_overall_nation_recno and others may be pointing to the deleted nation
	}

	public Nation NewNation(int nationType, int raceId, int colorSchemeId, int dpPlayerId = 0)
	{
		Nation nation = CreateNew();

		ColorRemap.ColorSchemes[nation.NationId] = colorSchemeId;

		if (nationType == NationBase.NATION_OWN)
		{
			Player = nation;
			PlayerId = nation.NationId;

			Info.DefaultViewingNationId = nation.NationId;
			Info.ViewingNationId = nation.NationId;
		}

		//--- we must call init() after setting ai_type & nation_res_id ----//

		nation.Init(nationType, raceId, colorSchemeId, dpPlayerId);

		//--- store the colors of all nations into a single array for later fast access ---//

		// use a lighter color for the nation power area
		NationColors[nation.NationId] = nation.NationColor;
		NationPowerColors[nation.NationId] = nation.NationColor;

		//-------------------------------------------//

		NationCount++;

		if (nationType == NationBase.NATION_AI)
			AINationCount++;

		LastNewNationDate = Info.GameDate;

		//---------- update statistic ----------//

		UpdateStatistic();

		return nation;
	}

	public bool CanFormNewAINation()
	{
		return Config.new_nation_emerge &&
		       AINationCount < Config.ai_nation_count && NationCount < GameConstants.MAX_NATION &&
		       Info.GameDate > LastDelNationDate.AddDays(GameConstants.NEW_NATION_INTERVAL_DAYS) &&
		       Info.GameDate > LastNewNationDate.AddDays(GameConstants.NEW_NATION_INTERVAL_DAYS);
	}

	public void UpdateStatistic()
	{
		//----- reset statistic vars first ------//

		foreach (Nation nation in this)
		{
			nation.TotalPopulation = 0;
			nation.TotalJoblessPopulation = 0;

			nation.LargestTownId = 0;
			nation.LargestTownPop = 0;

			nation.TotalSpyCount = 0;
			nation.TotalShipCombatLevel = 0;
		}

		//------ calculate town statistic -------//

		IndependentTownCount = 0;
		for (int i = 0; i < IndependentTownCountRaces.Length; i++)
		{
			IndependentTownCountRaces[i] = 0;
		}

		foreach (Town town in TownArray)
		{
			if (town.NationId != 0)
			{
				Nation nation = this[town.NationId];

				nation.TotalPopulation += town.Population;
				nation.TotalJoblessPopulation += town.JoblessPopulation;

				if (town.Population > nation.LargestTownPop)
				{
					nation.LargestTownPop = town.Population;
					nation.LargestTownId = town.TownId;
				}
			}
			else
			{
				IndependentTownCount++;

				//--- the no. of independent towns each race has ---//

				for (int j = 0; j < GameConstants.MAX_RACE; j++)
				{
					if (town.RacesPopulation[j] >= 6) // only count it if the pop of the race >= 6
						IndependentTownCountRaces[j]++;
				}
			}
		}

		//------ calculate spy statistic -------//

		foreach (Spy spy in SpyArray)
		{
			this[spy.TrueNationId].TotalSpyCount++;
		}

		//--- update nation rating (this must be called after the above code, which update vars like total_population ---//

		foreach (Nation nation in this)
		{
			nation.UpdateNationRating();
		}

		//------ update the military rating of all nations -----//

		UpdateMilitaryRating();

		//--------- update nation maximum ----------//

		MaxNationPopulation = 0;
		AllNationPopulation = 0;

		MaxNationUnits = 0;
		MaxNationHumans = 0;
		MaxNationGenerals = 0;
		MaxNationWeapons = 0;
		MaxNationShips = 0;
		MaxNationSpies = 0;

		MaxNationFirms = 0;
		MaxNationTechLevel = 0;

		MaxPopulationRating = Int32.MinValue;
		MaxMilitaryRating = Int32.MinValue;
		MaxEconomicRating = Int32.MinValue;
		MaxReputation = Int32.MinValue;
		MaxKillMonsterScore = Int32.MinValue;
		MaxOverallRating = Int32.MinValue;

		MaxPopulationNationId = 0;
		MaxMilitaryNationId = 0;
		MaxEconomicNationId = 0;
		MaxReputationNationId = 0;
		MaxKillMonsterNationId = 0;
		MaxOverallNationId = 0;

		foreach (Nation nation in this)
		{
			AllNationPopulation += nation.TotalPopulation;

			if (nation.TotalPopulation > MaxNationPopulation)
				MaxNationPopulation = nation.TotalPopulation;

			if (nation.TotalUnitCount > MaxNationUnits)
				MaxNationUnits = nation.TotalUnitCount;

			if (nation.TotalHumanCount > MaxNationHumans)
				MaxNationHumans = nation.TotalHumanCount;

			if (nation.TotalGeneralCount > MaxNationGenerals)
				MaxNationGenerals = nation.TotalGeneralCount;

			if (nation.TotalWeaponCount > MaxNationWeapons)
				MaxNationWeapons = nation.TotalWeaponCount;

			if (nation.TotalShipCount > MaxNationShips)
				MaxNationShips = nation.TotalShipCount;

			if (nation.TotalSpyCount > MaxNationSpies)
				MaxNationSpies = nation.TotalSpyCount;

			if (nation.TotalFirmCount > MaxNationFirms)
				MaxNationFirms = nation.TotalFirmCount;

			if (nation.TotalTechLevel() > MaxNationTechLevel)
				MaxNationTechLevel = nation.TotalTechLevel();

			//----- update maximum nation rating ------//

			if (nation.PopulationRating > MaxPopulationRating)
			{
				MaxPopulationRating = nation.PopulationRating;
				MaxPopulationNationId = nation.NationId;
			}

			if (nation.MilitaryRating > MaxMilitaryRating)
			{
				MaxMilitaryRating = nation.MilitaryRating;
				MaxMilitaryNationId = nation.NationId;
			}

			if (nation.EconomicRating > MaxEconomicRating)
			{
				MaxEconomicRating = nation.EconomicRating;
				MaxEconomicNationId = nation.NationId;
			}

			if (nation.Reputation > MaxReputation)
			{
				MaxReputation = (int)nation.Reputation;
				MaxReputationNationId = nation.NationId;
			}

			if (nation.KillMonsterScore > MaxKillMonsterScore)
			{
				MaxKillMonsterScore = (int)nation.KillMonsterScore;
				MaxKillMonsterNationId = nation.NationId;
			}

			if (nation.OverallRating > MaxOverallRating)
			{
				MaxOverallRating = nation.OverallRating;
				MaxOverallNationId = nation.NationId;
			}
		}
	}

	public void UpdateMilitaryRating()
	{
		int[] nationCombatLevelArray = new int[GameConstants.MAX_NATION];

		//------ calculate firm statistic -------//

		foreach (Firm firm in FirmArray)
		{
			if (firm.NationId == 0 || firm.FirmType != Firm.FIRM_CAMP)
				continue;

			// 20 is the base military points for a unit, so the nation that has many more units can be reflected in the military rating
			nationCombatLevelArray[firm.NationId - 1] += ((FirmCamp)firm).total_combat_level() +
			                                                 ((firm.OverseerId > 0 ? 1 : 0) + firm.Workers.Count) *
			                                                 20;
		}

		//------ calculate unit statistic -------//

		foreach (Unit unit in UnitArray)
		{
			if (unit.NationId == 0)
				continue;

			//---- if this unit is a ship, increase total_ship_combat_level ----//

			if (UnitRes[unit.UnitType].unit_class == UnitConstants.UNIT_CLASS_SHIP)
			{
				this[unit.NationId].TotalShipCombatLevel += (int)unit.HitPoints;
			}

			//----------------------------------//

			if (unit.UnitMode == UnitConstants.UNIT_MODE_OVERSEE) // firm commanders are counted above with firm_array
				continue;

			int addPoints = (int)unit.HitPoints;

			UnitInfo unitInfo = UnitRes[unit.UnitType];

			if (unitInfo.unit_class == UnitConstants.UNIT_CLASS_WEAPON)
				addPoints += (unitInfo.weapon_power + unit.WeaponVersion - 1) * 30;

			if (unit.LeaderId != 0 && !UnitArray.IsDeleted(unit.LeaderId))
				addPoints += addPoints * UnitArray[unit.LeaderId].Skill.SkillLevel / 100;

			// 20 is the base military points for a unit, so the nation that has many more units can be reflected in the military rating
			nationCombatLevelArray[unit.NationId - 1] += addPoints + 20;
		}

		//------ update nation statistic ------//

		foreach (Nation nation in this)
		{
			nation.MilitaryRating = nationCombatLevelArray[nation.NationId - 1] / 50;
		}
	}

	public void UpdateTotalHumanCount()
	{
		int[] totalHumanCountArray = new int[GameConstants.MAX_NATION];

		//------ calculate firm statistic -------//

		foreach (Firm firm in FirmArray)
		{
			if (firm.NationId == 0)
				continue;

			if (firm.FirmType == Firm.FIRM_CAMP || firm.FirmType == Firm.FIRM_BASE)
			{
				for (int j = firm.Workers.Count - 1; j >= 0; j--)
				{
					if (firm.Workers[j].RaceId != 0)
					{
						totalHumanCountArray[firm.NationId - 1]++;
					}
				}
			}
		}

		//------ calculate unit statistic -------//

		foreach (Unit unit in UnitArray)
		{
			// does not count kings
			if (unit.NationId != 0 && unit.RaceId != 0 && unit.Rank != Unit.RANK_KING)
			{
				totalHumanCountArray[unit.NationId - 1]++;
			}
		}

		//------ update nation statistic ------//

		foreach (Nation nation in this)
		{
			if (nation.TotalHumanCount != totalHumanCountArray[nation.NationId - 1])
				nation.TotalHumanCount = totalHumanCountArray[nation.NationId - 1];
		}
	}

	public void Process()
	{
		foreach (Nation nation in this)
		{
			//--------- process nation --------//

			// only process each nation once per day
			if (nation.NationId % InternalConstants.FRAMES_PER_DAY ==
			    Sys.Instance.FrameNumber % InternalConstants.FRAMES_PER_DAY)
			{
				nation.NextDay();

				if (IsDeleted(nation.NationId))
					continue;

				if (nation.NationType == NationBase.NATION_AI)
				{
					nation.ProcessAI();
				}
			}
		}

		if (Sys.Instance.FrameNumber % InternalConstants.FRAMES_PER_DAY == 0)
			NationPeaceDays++;
	}

	public void NextMonth()
	{
		foreach (Nation nation in this)
		{
			nation.NextMonth();
		}

		//------- update statistic -----------//

		UpdateStatistic();
	}

	public void NextYear()
	{
		foreach (Nation nation in this)
		{
			nation.NextYear();
		}
	}

	public int RandomUnusedRace()
	{
		//----- figure out which race has been used, which has not -----//

		bool[] usedRaceArray = new bool[GameConstants.MAX_RACE];
		int usedCount = GameConstants.MAX_RACE;

		// need to make sure disable races aren't included, work backwards
		for (int i = 0; i < usedRaceArray.Length; i++)
			usedRaceArray[i] = true;

		for (int i = 0; i < ConfigAdv.race_random_list_max; i++)
		{
			usedRaceArray[ConfigAdv.race_random_list[i] - 1] = false; // reset on
			usedCount--;
		}

		foreach (Nation nation in this)
		{
			usedRaceArray[nation.RaceId - 1] = true;
			usedCount++;
		}

		//----- pick a race randomly from the unused list -----//

		int pickedInstance = Misc.Random(GameConstants.MAX_RACE - usedCount) + 1;
		int usedId = 0;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (!usedRaceArray[i])
			{
				usedId++;

				if (usedId == pickedInstance)
					return i + 1;
			}
		}

		return 0;
	}

	public int RandomUnusedColor()
	{
		//----- figure out which race has been used, which has not -----//

		bool[] usedColorArray = new bool[InternalConstants.MAX_COLOR_SCHEME];
		int usedCount = 0;

		foreach (Nation nation in this)
		{
			usedColorArray[nation.ColorSchemeId - 1] = true;
			usedCount++;
		}

		//----- pick a race randomly from the unused list -----//

		int pickedInstance = Misc.Random(InternalConstants.MAX_COLOR_SCHEME - usedCount) + 1;
		int usedId = 0;

		for (int i = 0; i < InternalConstants.MAX_COLOR_SCHEME; i++)
		{
			if (!usedColorArray[i])
			{
				usedId++;

				if (usedId == pickedInstance)
					return i + 1;
			}
		}

		return 0;
	}

	public bool ShouldAttack(int attackingNation, int attackedNation)
	{
		if (attackingNation == attackedNation)
			return false;

		if (attackingNation == 0 || attackedNation == 0)
			return true;

		if (IsDeleted(attackingNation) || IsDeleted(attackedNation))
			return false;

		return this[attackingNation].GetRelationShouldAttack(attackedNation);
	}

	public string GetHumanName(int nationNameId, bool firstWordOnly = false)
	{
		int nationRecno = -nationNameId;

		string humanName = HumanNames[nationRecno - 1];

		if (firstWordOnly)
		{
			return humanName.Split(' ')[0];
		}
		else
		{
			return humanName;
		}
	}

	public void SetHumanName(int nationRecno, string nameStr)
	{
		HumanNames[nationRecno - 1] = nameStr;
	}
}