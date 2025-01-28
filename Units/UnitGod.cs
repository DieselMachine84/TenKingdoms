using System;

namespace TenKingdoms;

public class UnitGod : Unit
{
	public int god_id;
	public int base_firm_recno; // recno of the seat of power which creates and supports this god unit
	public int cast_power_type;
	public int cast_origin_x, cast_origin_y;
	public int cast_target_x, cast_target_y;

	private GodRes GodRes => Sys.Instance.GodRes;
	private MagicWeather MagicWeather => Sys.Instance.MagicWeather;
	private TornadoArray TornadoArray => Sys.Instance.TornadoArray;

	public override void init_derived()
	{
		cast_power_type = 0;
		if (unit_id == UnitConstants.UNIT_PERSIAN_HEALER || unit_id == UnitConstants.UNIT_VIKING_GOD ||
		    unit_id == UnitConstants.UNIT_KUKULCAN || unit_id == UnitConstants.UNIT_JAPANESE_GOD)
			can_attack_flag = false; // unable to attack

		if (unit_id == UnitConstants.UNIT_EGYPTIAN_GOD || unit_id == UnitConstants.UNIT_INDIAN_GOD ||
		    unit_id == UnitConstants.UNIT_ZULU_GOD)
			can_attack_flag = false; // unable to attack
	}

	public override void pre_process()
	{
		base.pre_process();

		//---- set force_move_flag to 1 if the god does not have the ability to attack ----//

		if (god_id != GodRes.GOD_CHINESE && god_id != GodRes.GOD_NORMAN) // only Chinese and Norman dragon can attack
			force_move_flag = true;

		//--- if the seat of power supporting this unit is destroyed, this unit dies ---//

		if (FirmArray.IsDeleted(base_firm_recno))
		{
			hit_points = 0;
			set_die();
			return;
		}

		//---- this unit consume pray points as it exists ----//

		FirmBase firmBase = (FirmBase)FirmArray[base_firm_recno];

		firmBase.pray_points -= GodRes[god_id].exist_pray_points / 200.0;

		if (firmBase.pray_points < 0)
			firmBase.pray_points = 0.0;

		//--------- update hit points --------//

		hit_points = (int)firmBase.pray_points;

		if (hit_points == 0)
			set_die();
	}

	public override int process_attack()
	{
		if (base.process_attack() == 0) // return 1 if the unit just finished its current attack
			return 0;

		//------- consumer pray points --------//
		/* The other gods have their prayer points consumed
		 * in UnitGod::cast_power(). See comments there for an
		 * explanation of the exploit this avoids.
		 */
		if (god_id == GodRes.GOD_CHINESE || god_id == GodRes.GOD_NORMAN)
			consume_power_pray_points();

		return 1;
	}

	public void cast_power(int xLoc, int yLoc)
	{
		if (FirmArray.IsDeleted(base_firm_recno))
			return;

		//------- consumer pray points --------//
		/* This must be done here to avoid an exploit where a human player 
		 * can cast an ability and then order the god to move within a 
		 * few frames and not consumer prayer points. This function is not
		 * used for the Chinese and Norman dragons so the function is also
		 * called in UnitGod::process_attack() for the dragons.
		 */
		if (!consume_power_pray_points())
			return;

		//---- viking god does not need a range for casting power ----//

		if (god_id == GodRes.GOD_VIKING)
		{
			if (cast_power_type == 1)
				viking_summon_rain();
			else
				viking_summon_tornado();
			return;
		}

		//------ cast power on the selected area ------//

		GodInfo godInfo = GodRes[god_id];

		int xLoc1 = xLoc - godInfo.cast_power_range + 1;
		int yLoc1 = yLoc - godInfo.cast_power_range + 1;
		int xLoc2 = xLoc + godInfo.cast_power_range - 1;
		int yLoc2 = yLoc + godInfo.cast_power_range - 1;

		int centerY = (yLoc1 + yLoc2) / 2;

		for (yLoc = yLoc1; yLoc <= yLoc2; yLoc++)
		{
			int t = Math.Abs(yLoc - centerY) / 2;

			for (xLoc = xLoc1 + t; xLoc <= xLoc2 - t; xLoc++)
			{
				if (xLoc >= 0 && xLoc < GameConstants.MapSize && yLoc >= 0 && yLoc < GameConstants.MapSize)
				{
					cast_on_loc(xLoc, yLoc);
				}
			}
		}
	}

	private bool consume_power_pray_points()
	{
		if (FirmArray.IsDeleted(base_firm_recno))
			return false;

		FirmBase firmBase = (FirmBase)FirmArray[base_firm_recno];

		firmBase.pray_points -= GodRes[god_id].power_pray_points;

		if (firmBase.pray_points < 0.0)
			firmBase.pray_points = 0.0;

		hit_points = (int)firmBase.pray_points;

		return true;
	}

	private void cast_on_loc(int castXLoc, int castYLoc)
	{
		Location location = World.get_loc(castXLoc, castYLoc);

		//--- if there is any unit on the location ---//

		if (location.has_unit(UnitConstants.UNIT_LAND))
		{
			cast_on_unit(location.unit_recno(UnitConstants.UNIT_LAND), 1);
		}
		else if (location.has_unit(UnitConstants.UNIT_SEA))
		{
			Unit unit = UnitArray[location.unit_recno(UnitConstants.UNIT_SEA)];

			//-- only heal human units belonging to our nation in ships --//

			if (unit.nation_recno == nation_recno && UnitRes[unit.unit_id].unit_class == UnitConstants.UNIT_CLASS_SHIP)
			{
				UnitMarine unitMarine = (UnitMarine)unit;

				for (int i = 0; i < unitMarine.unit_recno_array.Count; i++)
				{
					int divider = 4; // the size of a ship is 4 locations (2x2)

					// the effects are weaken on ship units, only 50% of the original effects
					cast_on_unit(unitMarine.unit_recno_array[i], divider);
				}
			}
		}

		//--------- on firms ---------//

		else if (location.is_firm())
		{
			Firm firm = FirmArray[location.firm_recno()];
			int divider = (firm.loc_x2 - firm.loc_x1 + 1) * (firm.loc_y2 - firm.loc_y1 + 1);
			if (god_id == GodRes.GOD_ZULU)
				divider = 1; // range of zulu god is 1, no need to divide

			if (firm.overseer_recno != 0)
			{
				cast_on_unit(firm.overseer_recno, divider);
			}

			if (!FirmRes[firm.firm_id].live_in_town)
			{
				for (int i = 0; i < firm.workers.Count; i++)
				{
					Worker worker = firm.workers[i];
					cast_on_worker(worker, firm.nation_recno, divider);
				}
			}
		}

		//--------- on towns ----------//

		else if (location.is_town())
		{
			Town town = TownArray[location.town_recno()];

			if (god_id == GodRes.GOD_JAPANESE && town.nation_recno != nation_recno)
			{
				int divider = InternalConstants.TOWN_WIDTH * InternalConstants.TOWN_HEIGHT;

				for (int i = 0; i < GameConstants.MAX_RACE; i++)
				{
					if (town.race_pop_array[i] == 0)
						continue;

					double changePoints = 7.0 + Misc.Random(8); // decrease 7 to 15 loyalty points instantly

					if (town.nation_recno != 0)
						town.change_loyalty(i + 1, -changePoints / divider);
					else
						town.change_resistance(i + 1, nation_recno, -changePoints / divider);
				}
			}
			else if (god_id == GodRes.GOD_EGYPTIAN && town.nation_recno == nation_recno)
			{
				int raceId;

				for (int headCount = 5;
				     headCount > 0 && town.population < GameConstants.MAX_TOWN_GROWTH_POPULATION
				                   && (raceId = town.pick_random_race(true, true)) != 0;
				     --headCount)
				{
					town.inc_pop(raceId, false, (int)town.race_loyalty_array[raceId - 1]);
				}
			}
		}
	}

	private void cast_on_unit(int unitRecno, int divider)
	{
		switch (god_id)
		{
			case GodRes.GOD_PERSIAN:
				persian_cast_power(unitRecno, divider);
				break;

			case GodRes.GOD_JAPANESE:
				japanese_cast_power(unitRecno, divider);
				break;

			case GodRes.GOD_MAYA:
				maya_cast_power(unitRecno, divider);
				break;

			case GodRes.GOD_EGYPTIAN:
				egyptian_cast_power(unitRecno, divider);
				break;

			case GodRes.GOD_INDIAN:
				indian_cast_power(unitRecno, divider);
				break;

			case GodRes.GOD_ZULU:
				zulu_cast_power(unitRecno, divider);
				break;
		}
	}

	private void cast_on_worker(Worker worker, int nationRecno, int divider)
	{
		switch (god_id)
		{
			case GodRes.GOD_PERSIAN:
				persian_cast_power(worker, nationRecno, divider);
				break;

			case GodRes.GOD_JAPANESE:
				japanese_cast_power(worker, nationRecno, divider);
				break;

			case GodRes.GOD_MAYA:
				maya_cast_power(worker, nationRecno, divider);
				break;

			case GodRes.GOD_EGYPTIAN:
				egyptian_cast_power(worker, nationRecno, divider);
				break;

			case GodRes.GOD_INDIAN:
				indian_cast_power(worker, nationRecno, divider);
				break;

			case GodRes.GOD_ZULU:
				zulu_cast_power(worker, nationRecno, divider);
				break;
		}
	}

	private void viking_summon_rain()
	{
		MagicWeather.cast_rain(10, 8); // 10 days, rain scale 8
		MagicWeather.cast_lightning(7); // 7 days
	}

	private void viking_summon_tornado()
	{
		int xLoc = next_x_loc();
		int yLoc = next_y_loc();
		int dir = final_dir % 8;

		// put a tornado one location ahead
		if (dir == 0 || dir == 1 || dir == 7)
			if (yLoc > 0)
				yLoc--;
		if (dir >= 1 && dir <= 3)
			if (xLoc < GameConstants.MapSize - 1)
				xLoc++;
		if (dir >= 3 && dir <= 5)
			if (yLoc < GameConstants.MapSize - 1)
				yLoc++;
		if (dir >= 5 && dir <= 7)
			if (xLoc > 0)
				xLoc--;

		TornadoArray.AddTornado(xLoc, yLoc, 600);
		MagicWeather.cast_wind(10, 1, dir * 45); // 10 days
	}

	private void persian_cast_power(int unitRecno, int divider)
	{
		Unit unit = UnitArray[unitRecno];

		//-- only heal human units belonging to our nation --//

		if (unit.nation_recno == nation_recno && unit.race_id > 0)
		{
			double changePoints = (double)unit.max_hit_points / (6.0 + Misc.Random(4)); // divided by (6 to 9)

			changePoints = Math.Max(changePoints, 10);

			unit.change_hit_points(changePoints / divider);
		}
	}

	private void japanese_cast_power(int unitRecno, int divider)
	{
		Unit unit = UnitArray[unitRecno];

		//-- only cast on enemy units -----//

		if (unit.nation_recno != nation_recno && unit.race_id > 0)
		{
			int changePoints = 7 + Misc.Random(8); // decrease 7 to 15 loyalty points instantly

			unit.change_loyalty(-Math.Max(1, changePoints / divider));
		}
	}

	private void maya_cast_power(int unitRecno, int divider)
	{
		Unit unit = UnitArray[unitRecno];

		//-- only cast on mayan units belonging to our nation --//

		if (unit.nation_recno == nation_recno && unit.race_id == (int)Race.RACE_MAYA)
		{
			int changePoints = 15 + Misc.Random(10); // add 15 to 25 points to its combat level instantly

			int newCombatLevel = unit.skill.combat_level + changePoints / divider;

			if (newCombatLevel > 100)
				newCombatLevel = 100;

			double oldHitPoints = unit.hit_points;

			unit.set_combat_level(newCombatLevel);

			unit.hit_points = oldHitPoints; // keep the hit points unchanged.
		}
	}

	private void egyptian_cast_power(int unitRecno, int divider)
	{
		// no effect
	}

	private void indian_cast_power(int unitRecno, int divider)
	{
		Unit unit = UnitArray[unitRecno];

		if (unit.is_visible() && NationArray.should_attack(nation_recno, unit.nation_recno))
		{
			unit.change_loyalty(-30 + Misc.Random(11));
		}
	}

	private void zulu_cast_power(int unitRecno, int divider)
	{
		// no effect
		Unit unit = UnitArray[unitRecno];

		if (nation_recno == unit.nation_recno && unit.race_id == (int)Race.RACE_ZULU && unit.rank_id != RANK_SOLDIER)
		{
			int changePoints = 30; // add 15 twice to avoid 130 becomes -126
			if (divider > 2)
			{
				unit.skill.skill_level += changePoints / divider;
				if (unit.skill.skill_level > 100)
					unit.skill.skill_level = 100;
			}
			else
			{
				for (int t = 2; t > 0; --t)
				{
					unit.skill.skill_level += changePoints / 2 / divider;
					if (unit.skill.skill_level > 100)
						unit.skill.skill_level = 100;
				}
			}
		}
	}

	private void persian_cast_power(Worker worker, int nationRecno, int divider)
	{
		//-- only heal human units belonging to our nation --//

		if (nationRecno == nation_recno && worker.race_id > 0)
		{
			int changePoints = worker.max_hit_points() / (4 + Misc.Random(4)); // divided by (4 to 7)

			changePoints = Math.Max(changePoints, 10);

			worker.change_hit_points(Math.Max(1, changePoints / divider));
		}
	}

	private void japanese_cast_power(Worker worker, int nationRecno, int divider)
	{
		//-- only cast on enemy units -----//

		if (nationRecno != nation_recno && worker.race_id > 0)
		{
			int changePoints = 7 + Misc.Random(8); // decrease 7 to 15 loyalty points instantly

			worker.change_loyalty(-Math.Max(1, changePoints / divider));
		}
	}

	private void maya_cast_power(Worker worker, int nationRecno, int divider)
	{
		//-- only cast on mayan units belonging to our nation --//

		if (nationRecno == nation_recno && worker.race_id == (int)Race.RACE_MAYA)
		{
			int changePoints = 15 + Misc.Random(10); // add 15 to 25 points to its combat level instantly

			int newCombatLevel = worker.combat_level + Math.Max(1, changePoints / divider);

			if (newCombatLevel > 100)
				newCombatLevel = 100;

			worker.combat_level = newCombatLevel;
		}
	}

	private void egyptian_cast_power(Worker worker, int nationRecno, int divider)
	{
		// no effect
	}

	private void indian_cast_power(Worker worker, int nationRecno, int divider)
	{
		// no effect
	}

	private void zulu_cast_power(Worker worker, int nationRecno, int divider)
	{
		// no effect
	}

	//--------- AI functions ----------//

	public override void process_ai()
	{
		if (!is_ai_all_stop())
			return;

		if (Info.TotalDays % 7 != sprite_recno % 7)
			return;

		switch (god_id)
		{
			case GodRes.GOD_NORMAN:
				think_dragon();
				break;

			case GodRes.GOD_MAYA:
				think_maya_god();
				break;

			case GodRes.GOD_GREEK:
				think_phoenix();
				break;

			case GodRes.GOD_VIKING:
				think_viking_god();
				break;

			case GodRes.GOD_PERSIAN:
				think_persian_god();
				break;

			case GodRes.GOD_CHINESE:
				think_chinese_dragon();
				break;

			case GodRes.GOD_JAPANESE:
				think_japanese_god();
				break;

			case GodRes.GOD_EGYPTIAN:
				think_egyptian_god();
				break;

			case GodRes.GOD_INDIAN:
				think_indian_god();
				break;

			case GodRes.GOD_ZULU:
				think_zulu_god();
				break;
		}
	}

	private void think_dragon()
	{
		int targetXLoc, targetYLoc;

		if (think_god_attack_target(out targetXLoc, out targetYLoc))
			attack_firm(targetXLoc, targetYLoc);
	}

	private void think_maya_god()
	{
		//------- there is no action, now think a new one ------//

		Nation ownNation = NationArray[nation_recno];
		int bestRating = 0;
		int targetXLoc = -1, targetYLoc = -1;

		for (int i = ownNation.ai_camp_array.Count - 1; i >= 0; i--)
		{
			Firm firm = FirmArray[ownNation.ai_camp_array[i]];

			int curRating = 0;

			if (firm.overseer_recno != 0)
			{
				Unit unit = UnitArray[firm.overseer_recno];

				if (unit.race_id == (int)Race.RACE_MAYA && unit.skill.combat_level < 100)
					curRating += 10;
			}


			for (int j = firm.workers.Count - 1; j >= 0; j--)
			{
				Worker worker = firm.workers[j];
				if (worker.race_id == (int)Race.RACE_MAYA && worker.combat_level < 100)
					curRating += 5;
			}

			if (curRating > bestRating)
			{
				bestRating = curRating;
				targetXLoc = firm.center_x;
				targetYLoc = firm.center_y;
			}
		}

		//-------------------------------------//

		if (bestRating != 0)
		{
			go_cast_power(targetXLoc, targetYLoc, 1, InternalConstants.COMMAND_AI);
		}
	}

	private void think_phoenix()
	{
		int xLoc = Misc.Random(GameConstants.MapSize);
		int yLoc = Misc.Random(GameConstants.MapSize);

		move_to(xLoc, yLoc);
	}

	private void think_viking_god()
	{
		int targetXLoc, targetYLoc;

		if (think_god_attack_target(out targetXLoc, out targetYLoc))
		{
			go_cast_power(targetXLoc + 1, targetYLoc + 1, 2, InternalConstants.COMMAND_AI);
		}
	}

	private void think_persian_god()
	{
		//------- there is no action, now think a new one ------//

		Nation ownNation = NationArray[nation_recno];
		int bestRating = 0;
		int targetXLoc = -1, targetYLoc = -1;

		for (int i = ownNation.ai_camp_array.Count - 1; i >= 0; i--)
		{
			Firm firm = FirmArray[ownNation.ai_camp_array[i]];

			//----- calculate the injured rating of the camp ----//

			int totalHitPoints = 0;
			int totalMaxHitPoints = 0;

			for (int j = 0; j < firm.workers.Count; j++)
			{
				Worker worker = firm.workers[j];
				totalHitPoints += worker.hit_points;
				totalMaxHitPoints += worker.max_hit_points();
			}

			if (totalMaxHitPoints == 0)
				continue;

			int curRating = 100 * (totalMaxHitPoints - totalHitPoints) / totalMaxHitPoints;

			//---- if the king is the commander of this camp -----//

			if (firm.overseer_recno != 0 && UnitArray[firm.overseer_recno].rank_id == RANK_KING)
			{
				curRating += 20;
			}

			if (curRating > bestRating)
			{
				bestRating = curRating;
				targetXLoc = firm.center_x;
				targetYLoc = firm.center_y;
			}
		}

		//-------------------------------------//

		if (bestRating != 0)
		{
			go_cast_power(targetXLoc, targetYLoc, 1, InternalConstants.COMMAND_AI);
		}
	}

	private void think_chinese_dragon()
	{
		int targetXLoc, targetYLoc;

		if (think_god_attack_target(out targetXLoc, out targetYLoc))
			attack_firm(targetXLoc, targetYLoc);
	}

	private void think_japanese_god()
	{
		//------- there is no action, now think a new one ------//

		Nation ownNation = NationArray[nation_recno];
		int bestRating = 0;
		int targetXLoc = -1, targetYLoc = -1;

		//------ think firm target --------//

		if (Misc.Random(2) == 0)
		{
			foreach (Firm firm in FirmArray)
			{
				//------- only cast on camps ---------//

				if (firm.firm_id != Firm.FIRM_CAMP)
					continue;

				//------ only cast on hostile and tense nations ------//

				if (ownNation.get_relation(firm.nation_recno).status > NationBase.NATION_TENSE)
					continue;

				//------ calculate the rating of the firm -------//

				int curRating = ((FirmCamp)firm).total_combat_level() / 10;

				if (curRating > bestRating)
				{
					bestRating = curRating;
					targetXLoc = firm.center_x;
					targetYLoc = firm.center_y;
				}
			}
		}
		else
		{
			foreach (Town town in TownArray)
			{
				//------ only cast on hostile and tense nations ------//

				if (town.nation_recno != 0 &&
				    ownNation.get_relation(town.nation_recno).status > NationBase.NATION_TENSE)
					continue;

				//------ calculate the rating of the firm -------//

				int curRating = town.population + (100 - town.average_loyalty());

				if (curRating > bestRating)
				{
					bestRating = curRating;
					targetXLoc = town.center_x;
					targetYLoc = town.center_y;
				}
			}
		}

		//-------------------------------------//

		if (bestRating != 0)
		{
			go_cast_power(targetXLoc, targetYLoc, 1, InternalConstants.COMMAND_AI);
		}
	}

	private void think_egyptian_god()
	{
		int bestRating = 0;
		int targetXLoc = -1, targetYLoc = -1;
		const int MaxTownPop = GameConstants.MAX_TOWN_GROWTH_POPULATION;

		foreach (Town town in TownArray)
		{
			//------ only cast on own nations ------//

			if (town.nation_recno != nation_recno)
				continue;

			//------ calculate the rating of the firm -------//

			if (town.population > MaxTownPop - 5)
				continue;

			// maximize the total loyalty gain.
			int curRating = 5 * town.average_loyalty();

			// calc rating on the number of people
			if (town.population >= MaxTownPop / 2)
				curRating -= (town.population - MaxTownPop / 2) * 300 / MaxTownPop;
			else
				curRating -= (MaxTownPop / 2 - town.population) * 300 / MaxTownPop;

			if (curRating > bestRating)
			{
				bestRating = curRating;
				targetXLoc = town.center_x;
				targetYLoc = town.center_y;
			}
		}

		//-------------------------------------//

		if (bestRating != 0)
		{
			go_cast_power(targetXLoc, targetYLoc, 1, InternalConstants.COMMAND_AI);
		}
	}

	private void think_indian_god()
	{
		Nation ownNation = NationArray[nation_recno];

		// see if any unit near by

		int castRadius = 2;
		int leftLocX = next_x_loc() - castRadius;
		if (leftLocX < 0)
			leftLocX = 0;

		int rightLocX = next_x_loc() + castRadius;
		if (rightLocX >= GameConstants.MapSize)
			rightLocX = GameConstants.MapSize - 1;

		int topLocY = next_y_loc() - castRadius;
		if (topLocY < 0)
			topLocY = 0;

		int bottomLocY = next_y_loc() + castRadius;
		if (bottomLocY >= GameConstants.MapSize)
			bottomLocY = GameConstants.MapSize - 1;

		int curRating = 0;
		int xLoc = -1;
		int yLoc = -1;
		for (yLoc = topLocY; yLoc <= bottomLocY; ++yLoc)
		{
			for (xLoc = leftLocX; xLoc <= rightLocX; ++xLoc)
			{
				Location location = World.get_loc(xLoc, yLoc);
				int unitRecno;
				Unit unit;
				if (location.has_unit(UnitConstants.UNIT_LAND)
				    && (unitRecno = location.unit_recno(UnitConstants.UNIT_LAND)) != 0
				    && !UnitArray.IsDeleted(unitRecno)
				    && (unit = UnitArray[unitRecno]) != null

				    && unit.nation_recno != 0 // don't affect independent unit
				    && unit.nation_recno != nation_recno
				    && (unit.loyalty >= 20 && unit.loyalty <= 60 ||
				        unit.loyalty <= 80 && unit.target_loyalty < 30))
				{
					switch (ownNation.get_relation(unit.nation_recno).status)
					{
						case NationBase.NATION_HOSTILE:
							curRating += 3;
							break;

						case NationBase.NATION_TENSE:
						case NationBase.NATION_NEUTRAL:
							// curRating += 0;		// unchange
							break;

						case NationBase.NATION_FRIENDLY:
							curRating -= 1; // actually friendly humans are not affected
							break;

						case NationBase.NATION_ALLIANCE:
							curRating -= 1; // actually allied humans are not affected
							break;
					}
				}
			}
		}

		if (curRating > 1)
		{
			// if enemy unit come near, cast
			go_cast_power(next_x_loc(), next_y_loc(), 1, InternalConstants.COMMAND_AI);
		}
		else
		{
			// find any unit suitable, go to that area first
			int bestUnitCost = Int32.MaxValue;
			foreach (Unit unit in UnitArray)
			{
				// don't affect independent unit
				if (unit.is_visible() && unit.mobile_type == UnitConstants.UNIT_LAND &&
				    unit.nation_recno != 0 && unit.nation_recno != nation_recno &&
				    (unit.loyalty >= 20 && unit.loyalty <= 60 || unit.loyalty <= 80 && unit.target_loyalty < 30) &&
				    ownNation.get_relation(unit.nation_recno).status == NationBase.NATION_HOSTILE)
				{
					int cost = Misc.points_distance(next_x_loc(), next_y_loc(), unit.next_x_loc(), unit.next_y_loc());
					if (cost < bestUnitCost)
					{
						bestUnitCost = cost;
						xLoc = unit.next_x_loc();
						yLoc = unit.next_y_loc();
					}
				}
			}

			if (bestUnitCost < 100)
			{
				if (Misc.points_distance(next_x_loc(), next_y_loc(), xLoc, yLoc) <= GodRes[god_id].cast_power_range)
					go_cast_power(xLoc, yLoc, 1, InternalConstants.COMMAND_AI);
				else
					move_to(xLoc, yLoc);
			}
			else if (Misc.Random(4) == 0)
			{
				// move to a near random location
				xLoc = next_x_loc() + Misc.Random(100) - 50;
				if (xLoc < 0)
					xLoc = 0;
				if (xLoc >= GameConstants.MapSize)
					xLoc = GameConstants.MapSize - 1;
				yLoc = next_y_loc() + Misc.Random(100) - 50;
				if (yLoc < 0)
					yLoc = 0;
				if (yLoc >= GameConstants.MapSize)
					yLoc = GameConstants.MapSize - 1;
				move_to(xLoc, yLoc);
			}
		}
	}

	private void think_zulu_god()
	{
		//------- there is no action, now think a new one ------//

		Nation ownNation = NationArray[nation_recno];
		int bestRating = 0;
		int targetXLoc = -1, targetYLoc = -1;

		for (int i = ownNation.ai_camp_array.Count - 1; i >= 0; i--)
		{
			Firm firm = FirmArray[ownNation.ai_camp_array[i]];

			int curRating = 0;

			Unit unit;
			if (firm.overseer_recno != 0
			    && (unit = UnitArray[firm.overseer_recno]) != null
			    && unit.race_id == (int)Race.RACE_ZULU // only consider ZULU leader
			    && unit.skill.skill_level <= 70)
			{
				if (unit.rank_id == RANK_KING)
					curRating += 5000; // weak king need leadership very much

				if (unit.skill.skill_level >= 40)
					curRating += 5000 - (unit.skill.skill_level - 40) * 60; // strong leader need not be enhanced
				else
					curRating += 5000 - (40 - unit.skill.skill_level) * 80; // don't add weak leader

				// calculate the benefits to his soldiers
				for (int j = firm.workers.Count - 1; j >= 0; j--)
				{
					Worker worker = firm.workers[j];
					if (worker.race_id == (int)Race.RACE_ZULU)
						curRating += (unit.skill.combat_level - worker.combat_level) * 2;
					else
						curRating += unit.skill.combat_level - worker.combat_level;
				}

				if (curRating > bestRating)
				{
					bestRating = curRating;
					targetXLoc = firm.center_x;
					targetYLoc = firm.center_y;
				}
			}
		}

		//-------------------------------------//

		if (bestRating != 0)
		{
			go_cast_power(targetXLoc, targetYLoc, 1, InternalConstants.COMMAND_AI);
		}
	}

	private bool think_god_attack_target(out int targetXLoc, out int targetYLoc)
	{
		targetXLoc = -1;
		targetYLoc = -1;
		Nation ownNation = NationArray[nation_recno];

		foreach (Firm firm in FirmArray.EnumerateRandom())
		{
			if (firm.firm_id == Firm.FIRM_MONSTER)
				continue;

			//-------- only attack enemies ----------//

			if (ownNation.get_relation(firm.nation_recno).status != NationBase.NATION_HOSTILE)
				continue;

			//---- only attack enemy base and camp ----//

			if (firm.firm_id != Firm.FIRM_BASE && firm.firm_id != Firm.FIRM_CAMP)
				continue;

			//------- attack now --------//

			targetXLoc = firm.loc_x1;
			targetYLoc = firm.loc_y1;

			return true;
		}

		//----- if there is no enemy to attack, attack Fryhtans ----//

		foreach (Firm firm in FirmArray.EnumerateRandom())
		{
			if (firm.firm_id == Firm.FIRM_MONSTER)
			{
				targetXLoc = firm.loc_x1;
				targetYLoc = firm.loc_y1;

				return true;
			}
		}

		//---------------------------------------------------//

		return false;
	}
}