using System;

namespace TenKingdoms;

public class NationArray : DynArray<Nation>
{
	public int nation_count; // no. of nations, it's different from nation_array.size() which is a DynArrayB
	public int ai_nation_count;
	public DateTime last_del_nation_date;
	public DateTime last_new_nation_date;

	public int max_nation_population; // the maximum population in a nation
	public int all_nation_population; // total population of all nations.

	public int independent_town_count;

	// the no. of independent towns each race has
	public int[] independent_town_count_race_array = new int[GameConstants.MAX_RACE];

	public int max_nation_units;
	public int max_nation_humans;
	public int max_nation_generals;
	public int max_nation_weapons;
	public int max_nation_ships;
	public int max_nation_spies;

	public int max_nation_firms;
	public int max_nation_tech_level;

	public int max_population_rating;
	public int max_military_rating;
	public int max_economic_rating;
	public int max_reputation;
	public int max_kill_monster_score;
	public int max_overall_rating;

	public int max_population_nation_recno;
	public int max_military_nation_recno;
	public int max_economic_nation_recno;
	public int max_reputation_nation_recno;
	public int max_kill_monster_nation_recno;
	public int max_overall_nation_recno;

	public int last_alliance_id;
	public int nation_peace_days; // continuous peace among nations

	public int player_recno;
	public Nation player;

	public byte[] nation_color_array = new byte[GameConstants.MAX_NATION + 1];
	public byte[] nation_power_color_array = new byte[GameConstants.MAX_NATION + 2];

	public string[] human_name_array = new string[GameConstants.MAX_NATION];

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
		nation_count = 0;
		ai_nation_count = 0;

		last_del_nation_date = default;
		nation_peace_days = 0;
		last_alliance_id = 0;

		nation_color_array[0] = Colors.V_WHITE; // if nation_recno==0, the color is white
		nation_power_color_array[0] = Colors.VGA_GRAY + 10; // if nation_recno==0, the color is white
		// if Location::power_nation_recno==MAX_NATION+1, this means there are more than one nation have influence over this location.
		nation_power_color_array[GameConstants.MAX_NATION + 1] = Colors.VGA_GRAY + 10;
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
				if (nation.nation_recno == freeId)
				{
					idUsed = true;
				}
			}
		}

		return freeId;
	}

	public void DeleteNation(Nation nation)
	{
		nation_count--;

		if (nation.nation_type == NationBase.NATION_AI)
			ai_nation_count--;
		
		ColorRemap.ColorSchemes[nation.nation_recno] = 0;

		last_del_nation_date = Info.game_date;

		int nationRecno = nation.nation_recno;
		nation.deinit();
		Delete(nationRecno);

		//---- if the nation to be deleted is the player's nation ---//

		if (nationRecno == player_recno)
		{
			player = null;
			player_recno = 0;

			// set the view mode to normal mode to prevent possible problems
			Sys.Instance.set_view_mode(InternalConstants.MODE_NORMAL);
		}

		//---------- update statistic ----------//

		update_statistic(); // as max_overall_nation_recno and others may be pointing to the deleted nation
	}

	public Nation new_nation(int nationType, int raceId, int colorSchemeId, int dpPlayerId = 0)
	{
		Nation nation = CreateNew();

		ColorRemap.ColorSchemes[nation.nation_recno] = colorSchemeId;

		if (nationType == NationBase.NATION_OWN)
		{
			player = nation;
			player_recno = nation.nation_recno;

			Info.default_viewing_nation_recno = nation.nation_recno;
			Info.viewing_nation_recno = nation.nation_recno;
		}

		//--- we must call init() after setting ai_type & nation_res_id ----//

		nation.init(nationType, raceId, colorSchemeId, dpPlayerId);

		//--- store the colors of all nations into a single array for later fast access ---//

		// use a lighter color for the nation power area
		nation_color_array[nation.nation_recno] = nation.nation_color;
		nation_power_color_array[nation.nation_recno] = nation.nation_color;

		//-------------------------------------------//

		nation_count++;

		if (nationType == NationBase.NATION_AI)
			ai_nation_count++;

		last_new_nation_date = Info.game_date;

		//---------- update statistic ----------//

		update_statistic();

		return nation;
	}

	public bool can_form_new_ai_nation()
	{
		return Config.new_nation_emerge &&
		       ai_nation_count < Config.ai_nation_count && nation_count < GameConstants.MAX_NATION &&
		       Info.game_date > last_del_nation_date.AddDays(GameConstants.NEW_NATION_INTERVAL_DAYS) &&
		       Info.game_date > last_new_nation_date.AddDays(GameConstants.NEW_NATION_INTERVAL_DAYS);
	}

	public void update_statistic()
	{
		//----- reset statistic vars first ------//

		foreach (Nation nation in this)
		{
			nation.total_population = 0;
			nation.total_jobless_population = 0;

			nation.largest_town_recno = 0;
			nation.largest_town_pop = 0;

			nation.total_spy_count = 0;
			nation.total_ship_combat_level = 0;
		}

		//------ calculate town statistic -------//

		independent_town_count = 0;
		for (int i = 0; i < independent_town_count_race_array.Length; i++)
		{
			independent_town_count_race_array[i] = 0;
		}

		foreach (Town town in TownArray)
		{
			if (town.NationId != 0)
			{
				Nation nation = this[town.NationId];

				nation.total_population += town.Population;
				nation.total_jobless_population += town.JoblessPopulation;

				if (town.Population > nation.largest_town_pop)
				{
					nation.largest_town_pop = town.Population;
					nation.largest_town_recno = town.TownId;
				}
			}
			else
			{
				independent_town_count++;

				//--- the no. of independent towns each race has ---//

				for (int j = 0; j < GameConstants.MAX_RACE; j++)
				{
					if (town.RacesPopulation[j] >= 6) // only count it if the pop of the race >= 6
						independent_town_count_race_array[j]++;
				}
			}
		}

		//------ calculate spy statistic -------//

		foreach (Spy spy in SpyArray)
		{
			this[spy.true_nation_recno].total_spy_count++;
		}

		//--- update nation rating (this must be called after the above code, which update vars like total_population ---//

		foreach (Nation nation in this)
		{
			nation.update_nation_rating();
		}

		//------ update the military rating of all nations -----//

		update_military_rating();

		//--------- update nation maximum ----------//

		max_nation_population = 0;
		all_nation_population = 0;

		max_nation_units = 0;
		max_nation_humans = 0;
		max_nation_generals = 0;
		max_nation_weapons = 0;
		max_nation_ships = 0;
		max_nation_spies = 0;

		max_nation_firms = 0;
		max_nation_tech_level = 0;

		max_population_rating = Int32.MinValue;
		max_military_rating = Int32.MinValue;
		max_economic_rating = Int32.MinValue;
		max_reputation = Int32.MinValue;
		max_kill_monster_score = Int32.MinValue;
		max_overall_rating = Int32.MinValue;

		max_population_nation_recno = 0;
		max_military_nation_recno = 0;
		max_economic_nation_recno = 0;
		max_reputation_nation_recno = 0;
		max_kill_monster_nation_recno = 0;
		max_overall_nation_recno = 0;

		foreach (Nation nation in this)
		{
			all_nation_population += nation.total_population;

			if (nation.total_population > max_nation_population)
				max_nation_population = nation.total_population;

			if (nation.total_unit_count > max_nation_units)
				max_nation_units = nation.total_unit_count;

			if (nation.total_human_count > max_nation_humans)
				max_nation_humans = nation.total_human_count;

			if (nation.total_general_count > max_nation_generals)
				max_nation_generals = nation.total_general_count;

			if (nation.total_weapon_count > max_nation_weapons)
				max_nation_weapons = nation.total_weapon_count;

			if (nation.total_ship_count > max_nation_ships)
				max_nation_ships = nation.total_ship_count;

			if (nation.total_spy_count > max_nation_spies)
				max_nation_spies = nation.total_spy_count;

			if (nation.total_firm_count > max_nation_firms)
				max_nation_firms = nation.total_firm_count;

			if (nation.total_tech_level() > max_nation_tech_level)
				max_nation_tech_level = nation.total_tech_level();

			//----- update maximum nation rating ------//

			if (nation.population_rating > max_population_rating)
			{
				max_population_rating = nation.population_rating;
				max_population_nation_recno = nation.nation_recno;
			}

			if (nation.military_rating > max_military_rating)
			{
				max_military_rating = nation.military_rating;
				max_military_nation_recno = nation.nation_recno;
			}

			if (nation.economic_rating > max_economic_rating)
			{
				max_economic_rating = nation.economic_rating;
				max_economic_nation_recno = nation.nation_recno;
			}

			if (nation.reputation > max_reputation)
			{
				max_reputation = (int)nation.reputation;
				max_reputation_nation_recno = nation.nation_recno;
			}

			if (nation.kill_monster_score > max_kill_monster_score)
			{
				max_kill_monster_score = (int)nation.kill_monster_score;
				max_kill_monster_nation_recno = nation.nation_recno;
			}

			if (nation.overall_rating > max_overall_rating)
			{
				max_overall_rating = nation.overall_rating;
				max_overall_nation_recno = nation.nation_recno;
			}
		}
	}

	public void update_military_rating()
	{
		int[] nationCombatLevelArray = new int[GameConstants.MAX_NATION];

		//------ calculate firm statistic -------//

		foreach (Firm firm in FirmArray)
		{
			if (firm.nation_recno == 0 || firm.firm_id != Firm.FIRM_CAMP)
				continue;

			// 20 is the base military points for a unit, so the nation that has many more units can be reflected in the military rating
			nationCombatLevelArray[firm.nation_recno - 1] += ((FirmCamp)firm).total_combat_level() +
			                                                 ((firm.overseer_recno > 0 ? 1 : 0) + firm.workers.Count) *
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
				this[unit.NationId].total_ship_combat_level += (int)unit.HitPoints;
			}

			//----------------------------------//

			if (unit.UnitMode == UnitConstants.UNIT_MODE_OVERSEE) // firm commanders are counted above with firm_array
				continue;

			int addPoints = (int)unit.HitPoints;

			UnitInfo unitInfo = UnitRes[unit.UnitType];

			if (unitInfo.unit_class == UnitConstants.UNIT_CLASS_WEAPON)
				addPoints += (unitInfo.weapon_power + unit.WeaponVersion - 1) * 30;

			if (unit.LeaderId != 0 && !UnitArray.IsDeleted(unit.LeaderId))
				addPoints += addPoints * UnitArray[unit.LeaderId].Skill.skill_level / 100;

			// 20 is the base military points for a unit, so the nation that has many more units can be reflected in the military rating
			nationCombatLevelArray[unit.NationId - 1] += addPoints + 20;
		}

		//------ update nation statistic ------//

		foreach (Nation nation in this)
		{
			nation.military_rating = nationCombatLevelArray[nation.nation_recno - 1] / 50;
		}
	}

	public void update_total_human_count()
	{
		int[] totalHumanCountArray = new int[GameConstants.MAX_NATION];

		//------ calculate firm statistic -------//

		foreach (Firm firm in FirmArray)
		{
			if (firm.nation_recno == 0)
				continue;

			if (firm.firm_id == Firm.FIRM_CAMP || firm.firm_id == Firm.FIRM_BASE)
			{
				for (int j = firm.workers.Count - 1; j >= 0; j--)
				{
					if (firm.workers[j].race_id != 0)
					{
						totalHumanCountArray[firm.nation_recno - 1]++;
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
			if (nation.total_human_count != totalHumanCountArray[nation.nation_recno - 1])
				nation.total_human_count = totalHumanCountArray[nation.nation_recno - 1];
		}
	}

	public void Process()
	{
		foreach (Nation nation in this)
		{
			//--------- process nation --------//

			// only process each nation once per day
			if (nation.nation_recno % InternalConstants.FRAMES_PER_DAY ==
			    Sys.Instance.FrameNumber % InternalConstants.FRAMES_PER_DAY)
			{
				nation.next_day();

				if (IsDeleted(nation.nation_recno))
					continue;

				if (nation.nation_type == NationBase.NATION_AI)
				{
					nation.ProcessAI();
				}
			}
		}

		if (Sys.Instance.FrameNumber % InternalConstants.FRAMES_PER_DAY == 0)
			nation_peace_days++;
	}

	public void NextMonth()
	{
		foreach (Nation nation in this)
		{
			nation.next_month();
		}

		//------- update statistic -----------//

		update_statistic();
	}

	public void NextYear()
	{
		foreach (Nation nation in this)
		{
			nation.next_year();
		}
	}

	public int random_unused_race()
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
			usedRaceArray[nation.race_id - 1] = true;
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

	public int random_unused_color()
	{
		//----- figure out which race has been used, which has not -----//

		bool[] usedColorArray = new bool[InternalConstants.MAX_COLOR_SCHEME];
		int usedCount = 0;

		foreach (Nation nation in this)
		{
			usedColorArray[nation.color_scheme_id - 1] = true;
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

	public bool should_attack(int attackingNation, int attackedNation)
	{
		if (attackingNation == attackedNation)
			return false;

		if (attackingNation == 0 || attackedNation == 0)
			return true;

		if (IsDeleted(attackingNation) || IsDeleted(attackedNation))
			return false;

		return this[attackingNation].get_relation_should_attack(attackedNation);
	}

	public string get_human_name(int nationNameId, bool firstWordOnly = false)
	{
		int nationRecno = -nationNameId;

		string humanName = human_name_array[nationRecno - 1];

		if (firstWordOnly)
		{
			return humanName.Split(' ')[0];
		}
		else
		{
			return humanName;
		}
	}

	public void set_human_name(int nationRecno, string nameStr)
	{
		human_name_array[nationRecno - 1] = nameStr;
	}
}