using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class TownArray : DynArray<Town>
{
	// the town current being selected
	public int selected_recno;

	// no. of wandering people of each race. They are people for setting up independent towns later
	public int[] race_wander_pop_array = new int[GameConstants.MAX_RACE];

	private Config Config => Sys.Instance.Config;
	private ConfigAdv ConfigAdv => Sys.Instance.ConfigAdv;
	private Info Info => Sys.Instance.Info;
	private Power Power => Sys.Instance.Power;
	private World World => Sys.Instance.World;
	private NationArray NationArray => Sys.Instance.NationArray;
	private FirmArray FirmArray => Sys.Instance.FirmArray;
	private UnitArray UnitArray => Sys.Instance.UnitArray;

	public TownArray()
	{
	}

	protected override Town CreateNewObject(int objectId)
	{
		return new Town();
	}

	public Town AddTown(int nationRecno, int raceId, int xLoc, int yLoc)
	{
		Town town = CreateNew();
		town.Init(nextRecNo, nationRecno, raceId, xLoc, yLoc);
		nextRecNo++;

		NationArray.update_statistic(); // update largest_town_recno

		return town;
	}

	public void DeleteTown(Town town)
	{
		town.Deinit();
		Delete(town.TownId);

		NationArray.update_statistic();
	}

	public override bool IsDeleted(int recNo)
	{
		if (base.IsDeleted(recNo))
			return true;

		Town town = this[recNo];
		return town.Population == 0;
	}

	public void Process()
	{
		var dayFrameNumber = Sys.Instance.FrameNumber % InternalConstants.FRAMES_PER_DAY;

		foreach (Town town in this)
		{
			// only process each town once per day
			if (town.TownId % InternalConstants.FRAMES_PER_DAY == dayFrameNumber)
			{
				if (town.NationId == 0)
				{
					town.ThinkIndependentTown();
				}
				else
				{
					if (town.AITown)
					{
						town.ProcessAI();
					}
				}

				if (IsDeleted(town.TownId))
					continue;

				town.NextDay();
			}
		}

		//------ distribute demand -------//

		if (dayFrameNumber == 0 && Info.TotalDays % 15 == 0) // distribute demand every 15 days
			DistributeDemand();

		//------ create new independent town -----//

		if (dayFrameNumber == 0 && Info.TotalDays % 30 == 0)
			ThinkNewIndependentTown();
	}

	private void ThinkNewIndependentTown()
	{
		if (!Config.new_independent_town_emerge)
			return;

		if (Misc.Random(3) != 0) // 1/3 chance
			return;

		//---- count the number of independent towns ----//

		int independentTownCount = 0, allTotalPop = 0;

		foreach (Town town in this)
		{
			allTotalPop += town.Population;

			if (town.NationId == 0)
				independentTownCount++;
		}

		if (independentTownCount >= 10) // only when the no. of independent town is less than 10
			return;

		//--- if the total population of all nations combined > 1000, then no new independent town will emerge ---//

		if (allTotalPop > ConfigAdv.town_ai_emerge_town_pop_limit)
			return;

		//--- add 1 to 2 wanderer per month per race ---//

		int raceId;

		for (int i = 0; i < ConfigAdv.race_random_list_max; i++)
		{
			raceId = ConfigAdv.race_random_list[i];
			race_wander_pop_array[raceId - 1] += 2 + Misc.Random(5);
		}

		//----- check if there are enough wanderers to set up a new town ---//

		raceId = ConfigAdv.GetRandomRace();

		bool hasWanderers = false;
		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (++raceId > GameConstants.MAX_RACE)
				raceId = 1;

			if (race_wander_pop_array[raceId - 1] >= 10) // one of the race must have at least 10 people
			{
				hasWanderers = true;
				break;
			}
		}

		if (!hasWanderers)
			return;

		//------- locate for a space to build the town ------//

		int xLoc, yLoc;

		if (!ThinkTownLoc(GameConstants.MapSize * GameConstants.MapSize / 4, out xLoc, out yLoc))
			return;

		//--------------- create town ---------------//

		Town newTown = AddTown(0, raceId, xLoc, yLoc);

		int maxTownPop = 20 + Misc.Random(10);

		while (true)
		{
			int addPop = race_wander_pop_array[raceId - 1];
			addPop = Math.Min(maxTownPop - newTown.Population, addPop);

			int townResistance = IndependentTownResistance();

			// 0-the add pop do not have jobs, 1-first init
			newTown.InitPopulation(raceId, addPop, townResistance, false, true);

			race_wander_pop_array[raceId - 1] -= addPop;

			if (newTown.Population >= maxTownPop)
				break;

			//---- next race to be added to the independent town ----//

			raceId = ConfigAdv.GetRandomRace();

			hasWanderers = false;
			for (int i = 0; i < GameConstants.MAX_RACE; i++)
			{
				if (++raceId > GameConstants.MAX_RACE)
					raceId = 1;

				if (race_wander_pop_array[raceId - 1] >= 5)
				{
					hasWanderers = true;
					break;
				}
			}

			if (!hasWanderers) // no suitable race
				break;
		}

		//---------- set town layout -----------//

		newTown.AutoSetLayout();
	}

	public int IndependentTownResistance()
	{
		switch (Config.independent_town_resistance)
		{
			case Config.OPTION_LOW:
				return 40 + Misc.Random(20);

			case Config.OPTION_MODERATE:
				return 50 + Misc.Random(30);

			case Config.OPTION_HIGH:
				return 60 + Misc.Random(40);

			default:
				return 60 + Misc.Random(40);
		}
	}
	
	public bool ThinkTownLoc(int maxTries, out int xLoc, out int yLoc)
	{
		const int MIN_INTER_TOWN_DISTANCE = 16;
		const int BUILD_TOWN_LOC_WIDTH = 16;
		const int BUILD_TOWN_LOC_HEIGHT = 16;

		xLoc = -1;
		yLoc = -1;

		for (int i = 0; i < maxTries; i++)
		{
			// do not build on the upper most location as the flag will go beyond the view area
			xLoc = Misc.Random(GameConstants.MapSize - BUILD_TOWN_LOC_WIDTH);
			yLoc = 2 + Misc.Random(GameConstants.MapSize - BUILD_TOWN_LOC_HEIGHT - 2);

			bool canBuildFlag = true;

			//---------- check if the area is all free ----------//

			for (int y = yLoc; y < yLoc + BUILD_TOWN_LOC_HEIGHT; y++)
			{

				for (int x = xLoc; x < xLoc + BUILD_TOWN_LOC_WIDTH; x++)
				{
					Location location = World.get_loc(x, y);
					if (!location.can_build_town())
					{
						canBuildFlag = false;
						break;
					}
				}
			}

			if (!canBuildFlag)
				continue;

			//-------- check if it's too close to other towns --------//

			foreach (Town town in this)
			{
				if (Misc.rects_distance(xLoc, yLoc, xLoc + InternalConstants.TOWN_WIDTH - 1, yLoc + InternalConstants.TOWN_HEIGHT - 1,
					    town.LocX1, town.LocY1, town.LocX2, town.LocY2) < MIN_INTER_TOWN_DISTANCE)
				{
					canBuildFlag = false;
					break;
				}
			}

			if (!canBuildFlag) // if it's too close to other towns
				continue;

			//-------- check if it's too close to monster firms --------//

			foreach (Firm firm in FirmArray)
			{
				if (Misc.rects_distance(xLoc, yLoc, xLoc + InternalConstants.TOWN_WIDTH - 1, yLoc + InternalConstants.TOWN_HEIGHT - 1,
					    firm.loc_x1, firm.loc_y1, firm.loc_x2, firm.loc_y2) < GameConstants.MONSTER_ATTACK_NEIGHBOR_RANGE)
				{
					canBuildFlag = false;
					break;
				}
			}

			if (!canBuildFlag) // if it's too close to monster firms
				continue;

			//----------------------------------------//

			return true;
		}

		return false;
	}

	public bool Settle(int unitRecno, int xLoc, int yLoc)
	{
		if (!World.can_build_town(xLoc, yLoc, unitRecno))
			return false;

		//--------- get the nearest town town ------------//

		Unit unit = UnitArray[unitRecno];

		int nationRecno = unit.nation_recno;

		//----- it's far enough to form another town --------//

		Town town = AddTown(nationRecno, unit.race_id, xLoc, yLoc);

		//----------------------------------------------------//
		// if the settle unit is standing in the town area
		// cargo_recno of that location is unchanged in town_array.add_town
		// so update the location now
		//----------------------------------------------------//

		int uXLoc = unit.next_x_loc();
		int uYLoc = unit.next_y_loc();
		Location location = World.get_loc(uXLoc, uYLoc);

		town.AssignUnit(unitRecno);

		if (uXLoc >= town.LocX1 && uXLoc <= town.LocX2 && uYLoc >= town.LocY1 && uYLoc <= town.LocY2)
			location.set_town(town.TownId);

		town.UpdateTargetLoyalty();

		//--------- hide the unit from the map ----------//

		return true;
	}

	public void DistributeDemand()
	{
		//--- reset market place demand ----//

		foreach (Firm firm in FirmArray)
		{
			if (firm.firm_id == Firm.FIRM_MARKET)
			{
				FirmMarket firmMarket = (FirmMarket)firm;
				for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
				{
					firmMarket.market_goods_array[i].month_demand = 0.0;
				}
			}
		}

		//-------- distribute demand -------//

		foreach (Town town in this)
		{
			town.DistributeDemand();
		}
	}

	public void StopAttackNation(int nationRecno)
	{
		foreach (Town town in this)
		{
			town.ResetHostileNation(nationRecno);
		}
	}

	public void DisplayNext(int seekDir, bool sameNation)
	{
		if (selected_recno == 0)
			return;

		int nationRecno = this[selected_recno].NationId;
		var enumerator = (seekDir >= 0) ? EnumerateAll(selected_recno, true) : EnumerateAll(selected_recno, false);

		foreach (int recNo in enumerator)
		{
			Town town = this[recNo];

			//-------- if are of the same nation --------//

			if (sameNation && town.NationId != nationRecno)
				continue;

			//--- check if the location of this town has been explored ---//

			if (!World.get_loc(town.LocCenterX, town.LocCenterY).explored())
				continue;

			//---------------------------------//

			Power.reset_selection();
			selected_recno = town.TownId;

			World.go_loc(town.LocCenterX, town.LocCenterY);
			return;
		}
	}
}