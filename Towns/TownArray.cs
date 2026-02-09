using System;

namespace TenKingdoms;

public class TownArray : DynArray<Town>
{
	// no. of wandering people of each race. They are people for setting up independent towns later
	private readonly int[] _raceWanderers = new int[GameConstants.MAX_RACE];

	private Config Config => Sys.Instance.Config;
	private Info Info => Sys.Instance.Info;
	private World World => Sys.Instance.World;
	private NationArray NationArray => Sys.Instance.NationArray;
	private FirmArray FirmArray => Sys.Instance.FirmArray;
	private UnitArray UnitArray => Sys.Instance.UnitArray;

	public TownArray()
	{
	}

	protected override Town CreateNewObject(int objectType)
	{
		return new Town();
	}

	public Town AddTown(int nationId, int raceId, int locX, int locY)
	{
		Town town = CreateNew();
		town.Init(nationId, raceId, locX, locY);

		NationArray.UpdateStatistic();

		return town;
	}

	public void DeleteTown(Town town)
	{
		town.Deinit();
		Delete(town.TownId);

		NationArray.UpdateStatistic();
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
		//TODO distribute demand more often?

		if (dayFrameNumber == 0 && Info.TotalDays % 15 == 0) // distribute demand every 15 days
			DistributeDemand();

		//------ create new independent town -----//

		if (dayFrameNumber == 0 && Info.TotalDays % 30 == 0)
			ThinkNewIndependentTown();
	}

	public void DistributeDemand()
	{
		//--- reset market place demand ----//

		foreach (Firm firm in FirmArray)
		{
			if (firm.FirmType == Firm.FIRM_MARKET)
			{
				FirmMarket firmMarket = (FirmMarket)firm;
				for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
				{
					firmMarket.MarketGoods[i].MonthDemand = 0.0;
				}
			}
		}

		foreach (Town town in this)
		{
			town.DistributeDemand();
		}
	}
	
	private void ThinkNewIndependentTown()
	{
		if (!Config.NewIndependentTownEmerge)
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

		//TODO this constant should depend on the map size
		if (independentTownCount >= 10) // only when the no. of independent town is less than 10
			return;

		//--- if the total population of all nations combined > 1000, then no new independent town will emerge ---//
		//TODO this constant should depend on the map size
		if (allTotalPop > 1000)
			return;

		//--- add 1 to 2 wanderer per month per race ---//

		for (int i = 1; i <= GameConstants.MAX_RACE; i++)
		{
			_raceWanderers[i - 1] += 2 + Misc.Random(5);
		}

		//----- check if there are enough wanderers to set up a new town ---//

		int raceId = Misc.Random(GameConstants.MAX_RACE) + 1;

		bool hasWanderers = false;
		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (++raceId > GameConstants.MAX_RACE)
				raceId = 1;

			if (_raceWanderers[raceId - 1] >= 10) // one of the race must have at least 10 people
			{
				hasWanderers = true;
				break;
			}
		}

		if (!hasWanderers)
			return;

		//------- locate for a space to build the town ------//

		if (!ThinkTownLoc(Config.MapSize * Config.MapSize / 4, out int locX, out int locY))
			return;

		//--------------- create town ---------------//

		Town newTown = AddTown(0, raceId, locX, locY);

		int maxTownPop = 20 + Misc.Random(10);

		while (true)
		{
			int addPop = _raceWanderers[raceId - 1];
			addPop = Math.Min(maxTownPop - newTown.Population, addPop);

			int townResistance = IndependentTownResistance();

			newTown.InitPopulation(raceId, addPop, townResistance, false, true);

			_raceWanderers[raceId - 1] -= addPop;

			if (newTown.Population >= maxTownPop)
				break;

			//---- next race to be added to the independent town ----//

			raceId = Misc.Random(GameConstants.MAX_RACE) + 1;

			hasWanderers = false;
			for (int i = 0; i < GameConstants.MAX_RACE; i++)
			{
				if (++raceId > GameConstants.MAX_RACE)
					raceId = 1;

				if (_raceWanderers[raceId - 1] >= 5)
				{
					hasWanderers = true;
					break;
				}
			}

			if (!hasWanderers) // no suitable race
				break;
		}

		newTown.AutoSetLayout();
	}

	public int IndependentTownResistance()
	{
		switch (Config.IndependentTownResistance)
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
	
	public bool ThinkTownLoc(int maxTries, out int locX, out int locY)
	{
		const int BUILD_TOWN_LOC_WIDTH = 16;
		const int BUILD_TOWN_LOC_HEIGHT = 16;

		locX = -1;
		locY = -1;

		for (int i = 0; i < maxTries; i++)
		{
			// do not build on the upper most location as the flag will go beyond the view area
			locX = Misc.Random(Config.MapSize - BUILD_TOWN_LOC_WIDTH);
			locY = 2 + Misc.Random(Config.MapSize - BUILD_TOWN_LOC_HEIGHT - 2);

			bool canBuildFlag = true;

			//---------- check if the area is all free ----------//

			for (int y = locY; y < locY + BUILD_TOWN_LOC_HEIGHT; y++)
			{
				for (int x = locX; x < locX + BUILD_TOWN_LOC_WIDTH; x++)
				{
					Location location = World.GetLoc(x, y);
					if (!location.CanBuildTown())
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
				if (Misc.RectsDistance(locX, locY, locX + InternalConstants.TOWN_WIDTH - 1, locY + InternalConstants.TOWN_HEIGHT - 1,
					    town.LocX1, town.LocY1, town.LocX2, town.LocY2) < InternalConstants.MIN_INTER_TOWN_DISTANCE)
				{
					canBuildFlag = false;
					break;
				}
			}

			if (!canBuildFlag) // if it's too close to other towns
				continue;

			//-------- check if it's too close to monster firms --------//
			//TODO this code actually checks all firms

			foreach (Firm firm in FirmArray)
			{
				if (Misc.RectsDistance(locX, locY, locX + InternalConstants.TOWN_WIDTH - 1, locY + InternalConstants.TOWN_HEIGHT - 1,
					    firm.LocX1, firm.LocY1, firm.LocX2, firm.LocY2) < GameConstants.MONSTER_ATTACK_NEIGHBOR_RANGE)
				{
					canBuildFlag = false;
					break;
				}
			}

			if (!canBuildFlag) // if it's too close to monster firms
				continue;

			return true;
		}

		return false;
	}

	public bool Settle(int unitId, int locX, int locY)
	{
		if (!World.CanBuildTown(locX, locY, unitId))
			return false;

		Unit unit = UnitArray[unitId];

		//----- it's far enough to form another town --------//

		Town town = AddTown(unit.NationId, unit.RaceId, locX, locY);

		//----------------------------------------------------//
		// if the settle unit is standing in the town area
		// cargoId of that location is unchanged in AddTown()
		// so update the location now
		//----------------------------------------------------//

		int unitLocX = unit.NextLocX;
		int unitLocY = unit.NextLocY;
		Location location = World.GetLoc(unitLocX, unitLocY);

		town.AssignUnit(unit);

		//TODO move to Town.SetWorldMatrix()
		if (unitLocX >= town.LocX1 && unitLocX <= town.LocX2 && unitLocY >= town.LocY1 && unitLocY <= town.LocY2)
			location.SetTown(town.TownId);

		town.UpdateTargetLoyalty();

		return true;
	}

	public void StopAttackNation(int nationId)
	{
		foreach (Town town in this)
		{
			town.ResetHostileNation(nationId);
		}
	}

	public int GetNextTown(int currentTownId, int seekDir, bool sameNation)
	{
		int nationId = this[currentTownId].NationId;
		var enumerator = EnumerateAll(currentTownId, seekDir >= 0);

		foreach (int townId in enumerator)
		{
			Town town = this[townId];

			if (sameNation && town.NationId != nationId)
				continue;

			if (!World.GetLoc(town.LocCenterX, town.LocCenterY).IsExplored())
				continue;

			return townId;
		}

		return currentTownId;
	}
}