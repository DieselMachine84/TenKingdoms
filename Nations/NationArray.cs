using System;

namespace TenKingdoms;

public class NationArray : DynArray<Nation>
{
	public int PlayerId { get; private set; }
	public Nation Player { get; private set; }

	public byte[] NationColors { get; } = new byte[GameConstants.MAX_NATION + 1];
	public byte[] NationPowerColors { get; } = new byte[GameConstants.MAX_NATION + 2];
	private string[] HumanNames { get; } = new string[GameConstants.MAX_NATION];
	
	public int NationCount { get; private set; }
	public int AINationCount { get; private set; }
	private DateTime LastNewNationDate { get; set; }
	private DateTime LastDelNationDate { get; set; }

	public int AllNationPopulation { get; private set; } // total population of all nations.
	public int MaxPopulationRating { get; private set; }
	public int MaxMilitaryRating { get; private set; }
	public int MaxEconomicRating { get; private set; }
	public int MaxReputation { get; private set; }
	public int MaxKillMonsterScore { get; private set; }
	public int MaxOverallRating { get; private set; }
	public int MaxOverallNationId { get; private set; }

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

		LastNewNationDate = Info.GameStartDate;
		LastDelNationDate = Info.GameStartDate;

		NationColors[0] = Colors.V_WHITE;
		NationPowerColors[0] = Colors.VGA_GRAY + 10;
		// this means there are more than one nation have influence over this location.
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

		UpdateStatistic();

		return nation;
	}

	public void DeleteNation(Nation nation)
	{
		ColorRemap.ColorSchemes[nation.NationId] = 0;
		
		NationCount--;

		if (nation.NationType == NationBase.NATION_AI)
			AINationCount--;
		
		LastDelNationDate = Info.GameDate;

		int nationId = nation.NationId;
		nation.Deinit();
		Delete(nationId);

		//---- if the nation to be deleted is the player's nation ---//

		if (nationId == PlayerId)
		{
			Player = null;
			PlayerId = 0;

			// set the view mode to normal mode to prevent possible problems
			Sys.Instance.set_view_mode(InternalConstants.MODE_NORMAL);
		}

		UpdateStatistic();
	}
	
	public void Process()
	{
		foreach (Nation nation in this)
		{
			// only process each nation once per day
			if (nation.NationId % InternalConstants.FRAMES_PER_DAY == Sys.Instance.FrameNumber % InternalConstants.FRAMES_PER_DAY)
			{
				nation.NextDay();

				if (IsDeleted(nation.NationId))
					continue;

				if (nation.NationType == NationBase.NATION_AI)
					nation.ProcessAI();
			}
		}
	}

	public void NextMonth()
	{
		foreach (Nation nation in this)
		{
			nation.NextMonth();
		}

		UpdateStatistic();
	}

	public void NextYear()
	{
		foreach (Nation nation in this)
		{
			nation.NextYear();
		}
	}

	public string GetHumanName(int nationNameId, bool firstWordOnly = false)
	{
		int nationId = -nationNameId;

		string humanName = HumanNames[nationId - 1];

		return firstWordOnly ? humanName.Split(' ')[0] : humanName;
	}

	public void SetHumanName(int nationId, string name)
	{
		HumanNames[nationId - 1] = name;
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
	
	public int RandomUnusedRace()
	{
		//----- figure out which race has been used, which has not -----//

		bool[] usedRaces = new bool[GameConstants.MAX_RACE];
		int usedCount = GameConstants.MAX_RACE;

		// need to make sure disable races aren't included, work backwards
		for (int i = 0; i < usedRaces.Length; i++)
			usedRaces[i] = true;

		for (int i = 0; i < ConfigAdv.race_random_list_max; i++)
		{
			usedRaces[ConfigAdv.race_random_list[i] - 1] = false; // reset on
			usedCount--;
		}

		foreach (Nation nation in this)
		{
			usedRaces[nation.RaceId - 1] = true;
			usedCount++;
		}

		//----- pick a race randomly from the unused list -----//

		int pickedInstance = Misc.Random(GameConstants.MAX_RACE - usedCount) + 1;
		int usedId = 0;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (!usedRaces[i])
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
		//----- figure out which color has been used, which has not -----//

		bool[] usedColors = new bool[InternalConstants.MAX_COLOR_SCHEME];
		int usedCount = InternalConstants.MAX_COLOR_SCHEME;

		// need to make sure disable colors aren't included, work backwards
		for (int i = 0; i < usedColors.Length; i++)
			usedColors[i] = true;
		
		for (int i = 0; i < ConfigAdv.race_random_list_max; i++)
		{
			usedColors[ConfigAdv.race_random_list[i] - 1] = false; // reset on
			usedCount--;
		}
		
		foreach (Nation nation in this)
		{
			usedColors[nation.ColorSchemeId - 1] = true;
			usedCount++;
		}

		//----- pick color randomly from the unused list -----//

		int pickedInstance = Misc.Random(InternalConstants.MAX_COLOR_SCHEME - usedCount) + 1;
		int usedId = 0;

		for (int i = 0; i < InternalConstants.MAX_COLOR_SCHEME; i++)
		{
			if (!usedColors[i])
			{
				usedId++;

				if (usedId == pickedInstance)
					return i + 1;
			}
		}

		return 0;
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
		}

		//------ calculate spy statistic -------//

		foreach (Spy spy in SpyArray)
		{
			this[spy.TrueNationId].TotalSpyCount++;
		}

		//--- update nation rating (this must be called after the above code, which update vars like TotalPopulation ---//

		foreach (Nation nation in this)
		{
			nation.UpdateNationRating();
		}

		//------ update the military rating of all nations -----//

		UpdateMilitaryRating();

		//--------- update nation maximum ----------//

		AllNationPopulation = 0;

		MaxPopulationRating = Int16.MinValue;
		MaxMilitaryRating = Int16.MinValue;
		MaxEconomicRating = Int16.MinValue;
		MaxReputation = Int16.MinValue;
		MaxKillMonsterScore = Int16.MinValue;
		MaxOverallRating = Int16.MinValue;
		MaxOverallNationId = 0;

		foreach (Nation nation in this)
		{
			AllNationPopulation += nation.TotalPopulation;

			//----- update maximum nation rating ------//

			if (nation.PopulationRating > MaxPopulationRating)
				MaxPopulationRating = nation.PopulationRating;

			if (nation.MilitaryRating > MaxMilitaryRating)
				MaxMilitaryRating = nation.MilitaryRating;

			if (nation.EconomicRating > MaxEconomicRating)
				MaxEconomicRating = nation.EconomicRating;

			if (nation.Reputation > MaxReputation)
				MaxReputation = (int)nation.Reputation;

			if (nation.KillMonsterScore > MaxKillMonsterScore)
				MaxKillMonsterScore = (int)nation.KillMonsterScore;

			if (nation.OverallRating > MaxOverallRating)
			{
				MaxOverallRating = nation.OverallRating;
				MaxOverallNationId = nation.NationId;
			}
		}
	}

	private void UpdateMilitaryRating()
	{
		int[] nationCombatLevels = new int[GameConstants.MAX_NATION];

		//------ calculate firm statistic -------//

		foreach (Firm firm in FirmArray)
		{
			if (firm.NationId == 0 || firm.FirmType != Firm.FIRM_CAMP)
				continue;

			// 20 is the base military points for a unit, so the nation that has many more units can be reflected in the military rating
			nationCombatLevels[firm.NationId - 1] += ((FirmCamp)firm).TotalCombatLevel() +
			                                         ((firm.OverseerId > 0 ? 1 : 0) + firm.Workers.Count) * 20;
		}

		//------ calculate unit statistic -------//

		foreach (Unit unit in UnitArray)
		{
			if (unit.NationId == 0)
				continue;

			if (UnitRes[unit.UnitType].unit_class == UnitConstants.UNIT_CLASS_SHIP)
			{
				this[unit.NationId].TotalShipCombatLevel += (int)unit.HitPoints;
			}

			if (unit.UnitMode == UnitConstants.UNIT_MODE_OVERSEE) // firm commanders are counted above with FirmArray
				continue;

			int addPoints = (int)unit.HitPoints;

			UnitInfo unitInfo = UnitRes[unit.UnitType];

			if (unitInfo.unit_class == UnitConstants.UNIT_CLASS_WEAPON)
				addPoints += (unitInfo.weapon_power + unit.WeaponVersion - 1) * 30;

			if (unit.LeaderId != 0 && !UnitArray.IsDeleted(unit.LeaderId))
				addPoints += addPoints * UnitArray[unit.LeaderId].Skill.SkillLevel / 100;

			// 20 is the base military points for a unit, so the nation that has many more units can be reflected in the military rating
			nationCombatLevels[unit.NationId - 1] += addPoints + 20;
		}

		//------ update nation statistic ------//

		foreach (Nation nation in this)
		{
			nation.MilitaryRating = nationCombatLevels[nation.NationId - 1] / 50;
		}
	}
}