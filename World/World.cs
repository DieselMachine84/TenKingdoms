using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class World
{
	private readonly Location[] _locMatrix = new Location[GameConstants.MapSize * GameConstants.MapSize];

	private int _scanFireX; // cycle from 0 to SCAN_FIRE_DIST-1
	private int _scanFireY;
	private int _lightningSignal;
	private int _plantLimit;
	public int PlantCount { get; set; }
	private readonly int[] _optTemp = { 32, 25, 28 };

	private TerrainRes TerrainRes => Sys.Instance.TerrainRes;
	private HillRes HillRes => Sys.Instance.HillRes;
	private PlantRes PlantRes => Sys.Instance.PlantRes;
	private FirmRes FirmRes => Sys.Instance.FirmRes;
	private UnitRes UnitRes => Sys.Instance.UnitRes;
	private SERes SERes => Sys.Instance.SERes;
	private SECtrl SECtrl => Sys.Instance.SECtrl;

	private Config Config => Sys.Instance.Config;
	private Weather Weather => Sys.Instance.Weather;
	private Weather[] WeatherForecast => Sys.Instance.WeatherForecast;
	private MagicWeather MagicWeather => Sys.Instance.MagicWeather;

	private NationArray NationArray => Sys.Instance.NationArray;
	private UnitArray UnitArray => Sys.Instance.UnitArray;
	private FirmArray FirmArray => Sys.Instance.FirmArray;
	private TownArray TownArray => Sys.Instance.TownArray;
	private SiteArray SiteArray => Sys.Instance.SiteArray;
	private TornadoArray TornadoArray => Sys.Instance.TornadoArray;
	private NewsArray NewsArray => Sys.Instance.NewsArray;

	public World()
	{
		for (int i = 0; i < _locMatrix.Length; i++)
		{
			_locMatrix[i] = new Location(TerrainRes, HillRes);
		}
	}
	
	public Location GetLoc(int locX, int locY)
	{
		return _locMatrix[GetMatrixIndex(locX, locY)];
	}

	public int GetMatrixIndex(int locX, int locY)
	{
		return locY * GameConstants.MapSize + locX;
	}

	public void GetLocXAndLocY(int matrixIndex, out int locX, out int locY)
	{
		locX = matrixIndex % GameConstants.MapSize;
		locY = matrixIndex / GameConstants.MapSize;
	}

	public int GetRegionId(int locX, int locY)
	{
		return GetLoc(locX, locY).RegionId;
	}

	public void Process()
	{
		// ------- process visibility --------//
		ProcessVisibility();

		//-------- process fire -----------//
		// BUGHERE : set Location::flammability for every change in cargo

		FireSpread(Weather);

		// --------- update scan fire x y ----------//
		if (++_scanFireX >= InternalConstants.SCAN_FIRE_DIST)
			_scanFireX = 0;
		if (++_scanFireY >= InternalConstants.SCAN_FIRE_DIST)
			_scanFireY = 0;

		//-------- process lightning ------//
		if (_lightningSignal == 0 && Weather.is_lightning())
		{
			// ------- create new lightning ----------//
			_lightningSignal = 110;
		}

		if (_lightningSignal == 106 && Config.weather_effect != 0)
		{
			LightningStrike(Misc.Random(GameConstants.MapSize), Misc.Random(GameConstants.MapSize), 1);
		}

		if (_lightningSignal == 100)
			_lightningSignal = 5 + Misc.Random(10);
		else if (_lightningSignal > 0)
			_lightningSignal--;

		//---------- process ambient sound ---------//

		if (Sys.Instance.FrameNumber % 10 == 0) // process once per ten frames
			ProcessAmbientSound();
	}

	private void ProcessVisibility()
	{
		if (!Config.fog_of_war)
			return;

		//TODO performance
		for (int i = 0; i < _locMatrix.Length; i++)
		{
			_locMatrix[i].DecVisibility();
		}
	}
	
	public void NextDay()
	{
		PlantActions();

		Sys.Instance.Weather = WeatherForecast[0];

		for (int foreDay = 0; foreDay < GameConstants.MAX_WEATHER_FORECAST - 1; foreDay++)
		{
			WeatherForecast[foreDay] = WeatherForecast[foreDay + 1];
		}

		int lastIndex = GameConstants.MAX_WEATHER_FORECAST - 1;
		WeatherForecast[lastIndex] = new Weather(WeatherForecast[lastIndex]);
		WeatherForecast[lastIndex].next_day();

		MagicWeather.next_day();

		if (Weather.has_tornado() && Config.weather_effect != 0)
		{
			TornadoArray.AddTornado(Weather.tornado_x_loc(GameConstants.MapSize, GameConstants.MapSize),
				Weather.tornado_y_loc(GameConstants.MapSize, GameConstants.MapSize), 600);
		}

		if (Weather.is_quake() && Config.random_event_frequency != Config.OPTION_NONE)
		{
			EarthQuake();
		}
	}
	
	public void GoToLocation(int locX, int locY, bool select = false)
	{
		//TODO implement
	}

	public void DisplayNext(int seekDir, bool sameNation)
	{
		//--- if the selected one is a town ----//

		if (TownArray.SelectedTownId != 0)
		{
			TownArray.DisplayNext(seekDir, sameNation);
		}

		//--- if the selected one is a firm ----//

		if (FirmArray.SelectedFirmId != 0)
		{
			FirmArray.DisplayNext(seekDir, sameNation);
		}

		//--- if the selected one is a unit ----//

		if (UnitArray.SelectedUnitId != 0)
		{
			UnitArray.DisplayNext(seekDir, sameNation);
		}

		//--- if the selected one is a natural resource site ----//

		if (SiteArray.SelectedSiteId != 0)
		{
			SiteArray.DisplayNext(seekDir, sameNation);
		}
	}

	public int GetUnitId(int locX, int locY, int mobileType)
	{
		Location location = GetLoc(locX, locY);
		return mobileType == UnitConstants.UNIT_AIR ? location.AirCargoId : location.CargoId;
	}

	public void SetUnitId(int locX, int locY, int mobileType, int unitId)
	{
		Location location = GetLoc(locX, locY);
		if (mobileType == UnitConstants.UNIT_AIR)
			location.AirCargoId = unitId;
		else
			location.CargoId = unitId;
	}

	// TODO remove
	public int DistanceRating(int locX1, int locY1, int locX2, int locY2)
	{
		int curDistance = Misc.points_distance(locX1, locY1, locX2, locY2);
		return 100 - 100 * curDistance / GameConstants.MapSize;
	}

	public void Unveil(int locX1, int locY1, int locX2, int locY2)
	{
		if (Config.explore_whole_map)
			return;

		Misc.BoundLocation(ref locX1, ref locY1);
		Misc.BoundLocation(ref locX2, ref locY2);

		Explore(locX1, locY1, locX2, locY2);
	}

	public void Explore(int locX1, int locY1, int locX2, int locY2)
	{
		if (Config.explore_whole_map)
			return;

		for (int locY = locY1; locY <= locY2; locY++)
		{
			for (int locX = locX1; locX <= locX2; locX++)
			{
				Location location = GetLoc(locX, locY);
				if (location.IsExplored())
					continue;

				location.ExploredOn();

				//---- if the command base of the opponent revealed, establish contact ----//
				NationRelation relation = null;
				int nationId = 0;

				if (location.IsFirm())
				{
					Firm firm = FirmArray[location.FirmId()];
					if (firm.NationId != 0 && NationArray.player_recno != 0)
					{
						relation = NationArray.player.get_relation(firm.NationId);
						nationId = firm.NationId;
					}
				}

				if (location.IsTown())
				{
					Town town = TownArray[location.TownId()];
					if (town.NationId != 0 && NationArray.player_recno != 0)
					{
						relation = NationArray.player.get_relation(town.NationId);
						nationId = town.NationId;
					}
				}

				if (relation != null && !relation.has_contact)
				{
					//if( !remote.is_enable() )
					//{
						NationArray.player.establish_contact(nationId);
					//}
					//else
					//{
					//if( !relation.contact_msg_flag )
					//{
					// packet structure : <player nation> <explored nation>
					//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_NATION_CONTACT, 2*sizeof(short));
					//*shortPtr = nation_array.player_recno;
					//shortPtr[1] = nationId;
					//relation.contact_msg_flag = 1;
					//}
					//}
				}
			}
		}
	}

	// always call unveil before visit //
	public void Visit(int locX1, int locY1, int locX2, int locY2, int range, int extend = 0)
	{
		if (!Config.fog_of_war)
			return;
		
		int left = Math.Max(0, locX1 - range);
		int top = Math.Max(0, locY1 - range);
		int right = Math.Min(GameConstants.MapSize - 1, locX2 + range);
		int bottom = Math.Min(GameConstants.MapSize - 1, locY2 + range);

		// ----- mark the visit_level of the square around the unit ------//
		for (int locY = top; locY <= bottom; locY++)
		{
			for (int locX = left; locX <= right; locX++)
			{
				GetLoc(locX, locY).SetVisited();
			}
		}

		// ----- visit_level decreasing outside the visible range ------//
		if (extend > 0)
		{
			int visitLevel = Location.FULL_VISIBILITY;
			int levelDrop = (Location.FULL_VISIBILITY - Location.EXPLORED_VISIBILITY) / (extend + 1);
			locX1 -= range;
			locY1 -= range;
			locX2 += range;
			locY2 += range;
			for (++range; extend > 0; --extend, ++range)
			{
				locX1--;
				locY1--;
				locX2++;
				locY2++;
				visitLevel -= levelDrop;
				VisitShell(locX1, locY1, locX2, locY2, visitLevel);
			}
		}
	}

	private void VisitShell(int locX1, int locY1, int locX2, int locY2, int visitLevel)
	{
		int left = Math.Max(0, locX1);
		int top = Math.Max(0, locY1);
		int right = Math.Min(GameConstants.MapSize - 1, locX2);
		int bottom = Math.Max(GameConstants.MapSize - 1, locY2);

		// ------- left side -----------//
		if (locX1 >= 0)
		{
			for (int y = top; y <= bottom; ++y)
			{
				GetLoc(locX1, y).SetVisited(visitLevel);
			}
		}

		// ------- top side ---------//
		if (locY1 >= 0)
		{
			for (int x = left; x <= right; ++x)
			{
				GetLoc(x, locY1).SetVisited(visitLevel);
			}
		}

		// ------- right side -----------//
		if (locX2 < GameConstants.MapSize)
		{
			for (int y = top; y <= bottom; ++y)
			{
				GetLoc(locX2, y).SetVisited(visitLevel);
			}
		}

		// ------- bottom side ---------//
		if (locY2 < GameConstants.MapSize)
		{
			for (int x = left; x <= right; ++x)
			{
				GetLoc(x, locY2).SetVisited(visitLevel);
			}
		}
	}

	public int CanBuildFirm(int locX1, int locY1, int firmType, int unitId = -1)
	{
		if (!Misc.IsLocationValid(locX1, locY1))
			return 0;

		FirmInfo firmInfo = FirmRes[firmType];
		int locX2 = locX1 + firmInfo.loc_width - 1;
		int locY2 = locY1 + firmInfo.loc_height - 1;
		if (!Misc.IsLocationValid(locX2, locY2))
			return 0;

		switch (firmInfo.tera_type)
		{
			case Location.LOCATE_WALK_LAND: // default : land firm
			case Location.LOCATE_WALK_SEA: // sea firm
			case Location.LOCATE_WALK_LAND | Location.LOCATE_WALK_SEA: // land or sea firm
				int teraMask = firmInfo.tera_type;
				for (int locY = locY1; locY <= locY2; locY++)
				{
					for (int locX = locX1; locX <= locX2; locX++)
					{
						Location location = GetLoc(locX, locY);
						if (!location.CanBuildFirm(teraMask) &&
						    (!location.HasUnit(UnitConstants.UNIT_LAND) || location.UnitId(UnitConstants.UNIT_LAND) != unitId))
							return 0;

						// don't allow building any buildings other than mines on a location with a site
						if (firmType != Firm.FIRM_MINE && location.HasSite())
							return 0;
					}
				}

				return 1;

			case 4: // special firm, such as harbor must be 3x3,
				// center square of one side is land (teraMask == 1),
				// two squares on that side can be land or sea (teraMask == 3)
				// and other (6 squares) are sea (teraMask == 2)
				if (firmInfo.loc_width != 3 || firmInfo.loc_height != 3)
					return 0;

				int[,] northPierTera = { { 2, 2, 2 }, { 2, 2, 2 }, { 3, 1, 3 } };
				int[,] southPierTera = { { 3, 1, 3 }, { 2, 2, 2 }, { 2, 2, 2 } };
				int[,] westPierTera = { { 2, 2, 3 }, { 2, 2, 1 }, { 2, 2, 3 } };
				int[,] eastPierTera = { { 3, 2, 2 }, { 1, 2, 2 }, { 3, 2, 2 } };

				int pierFlag = 1 | 2 | 4 | 8; // bit0=north, bit1=south, bit2=west, bit3=east
				for (int locY = locY1; locY <= locY2; locY++)
				{
					for (int locX = locX1; locX <= locX2; locX++)
					{
						Location location = GetLoc(locX, locY);
						// don't allow building any buildings other than mines on a location with a site
						if (location.HasSite())
							return 0;

						int x = locX - locX1;
						int y = locY - locY1;
						if (!location.CanBuildHarbor(northPierTera[y, x]))
							pierFlag &= ~1;
						if (!location.CanBuildHarbor(southPierTera[y, x]))
							pierFlag &= ~2;
						if (!location.CanBuildHarbor(westPierTera[y, x]))
							pierFlag &= ~4;
						if (!location.CanBuildHarbor(eastPierTera[y, x]))
							pierFlag &= ~8;
					}
				}

				return pierFlag;

			// other tera_type here
			default:
				return 0;
		}
	}

	public bool CanBuildTown(int locX1, int locY1, int unitId = -1)
	{
		if (!Misc.IsLocationValid(locX1, locY1))
			return false;

		int locX2 = locX1 + InternalConstants.TOWN_WIDTH - 1;
		int locY2 = locY1 + InternalConstants.TOWN_HEIGHT - 1;

		if (!Misc.IsLocationValid(locX2, locY2))
			return false;

		for (int locY = locY1; locY <= locY2; locY++)
		{
			for (int locX = locX1; locX <= locX2; locX++)
			{
				Location location = GetLoc(locX, locY);
				// allow the building unit to stand in the area
				if (!location.CanBuildTown() &&
				    (!location.HasUnit(UnitConstants.UNIT_LAND) || location.UnitId(UnitConstants.UNIT_LAND) != unitId))
					return false;
			}
		}

		return true;
	}

	public bool LocateSpace(ref int locX1, ref int locY1, int locX2, int locY2,
		int spaceLocWidth, int spaceLocHeight, int mobileType = UnitConstants.UNIT_LAND, int regionId = 0, bool buildFlag = false)
	{
		Location location1 = GetLoc(locX1, locY1);
		
		if (regionId == 0)
			regionId = location1.RegionId;

		bool isPlateau = location1.IsPlateau();

		//-----------------------------------------------------------//
		// locX, locY is the adjusted upper left corner location of the firm.
		// With the adjustment, it is easier to do the following checking.
		//-----------------------------------------------------------//

		int locX = locX1 - spaceLocWidth + 1;
		int locY = locY1 - spaceLocHeight + 1;
		Misc.BoundLocation(ref locX, ref locY);

		int width = locX2 - locX + 1;
		int height = locY2 - locY + 1;

		bool CheckLocation(int x, int y, ref int locX1, ref int locY1)
		{
			if (x >= 0 && y >= 0 && x + spaceLocWidth - 1 < GameConstants.MapSize && y + spaceLocHeight - 1 < GameConstants.MapSize)
			{
				// TODO remove % 2 == 0
				if (mobileType == UnitConstants.UNIT_LAND || (x % 2 == 0 && y % 2 == 0))
				{
					Location location = GetLoc(x, y);
					if (location.RegionId == regionId && location.IsPlateau() == isPlateau &&
					    CheckUnitSpace(x, y, x + spaceLocWidth - 1, y + spaceLocHeight - 1))
					{
						locX1 = x;
						locY1 = y;
						return true;
					}
				}
			}

			return false;
		}

		bool CheckUnitSpace(int x1, int y1, int x2, int y2)
		{
			if (!Misc.IsLocationValid(x1, y1) || !Misc.IsLocationValid(x2, y2))
				return false;

			for (int y = y1; y <= y2; y++)
			{
				for (int x = x1; x <= x2; x++)
				{
					Location location = GetLoc(x, y);
					// if build a firm/town, there must not be any sites in the area
					if (!location.CanMove(mobileType) || (buildFlag && (location.IsPowerOff() || location.HasSite())))
					{
						return false;
					}
				}
			}

			return true;
		}
		
		while (true)
		{
			//-----------------------------------------------------------//
			// step 1
			//-----------------------------------------------------------//
			int xOffset = width / 2;
			int yOffset = height;
			int x = locX + xOffset;
			int y = locY + yOffset;

			if (CheckLocation(x, y, ref locX1, ref locY1))
				return true;

			//-----------------------------------------------------------//
			// step 2
			//-----------------------------------------------------------//
			int sign = -1;
			int i;
			//y = yLoc + yOffset;
			int limit = width + 2;
			
			for (i = 1; i < limit; i++)
			{
				xOffset += sign * i;
				x = locX + xOffset;

				if (CheckLocation(x, y, ref locX1, ref locY1))
					return true;

				sign *= -1;
			}

			//-----------------------------------------------------------//
			// step 3
			//-----------------------------------------------------------//
			i = limit - 1;
			limit = (height + 1) * 2;
			int r = sign * i;
			int lastX = xOffset;
			//int lastY = yOffset;

			for (int j = 0; j < limit; j++)
			{
				// TODO remove % 2 == 0
				if (j % 2 != 0)
				{
					//x = xLoc + lastX;
					xOffset = lastX;
					x = locX + xOffset;
					//y = yLoc + yOffset;

					if (CheckLocation(x, y, ref locX1, ref locY1))
						return true;
				}
				else
				{
					xOffset = lastX + r;
					yOffset--;

					x = locX + xOffset;
					y = locY + yOffset;

					if (CheckLocation(x, y, ref locX1, ref locY1))
						return true;
				}
			}

			//-----------------------------------------------------------//
			// step 4
			//-----------------------------------------------------------//
			y = locY + yOffset;
			
			for (int k = 0; k <= width; k++)
			{
				sign *= -1;
				i--;
				r = sign * i;
				xOffset -= r;
				x = locX + xOffset;

				if (CheckLocation(x, y, ref locX1, ref locY1))
					return true;
			}

			//-----------------------------------------------------------//
			// re-init the parameters
			//-----------------------------------------------------------//
			if (locX <= 0 && locY <= 0 && locX + width >= GameConstants.MapSize && locY + height >= GameConstants.MapSize)
				break; // the whole map has been checked

			width += 2;
			height += 2;

			locX -= 1;
			locY -= 1;
			if (locX < 0)
			{
				locX = 0;
				width--;
			}

			if (locY < 0)
			{
				locY = 0;
				height--;
			}

			if (locX + width > GameConstants.MapSize)
				width--;
			if (locY + height > GameConstants.MapSize)
				height--;

			//if(width==xLoc2-xLoc1+spaceLocWidth && height==yLoc2-yLoc1+spaceLocHeight) // terminate the checking
			// return 0;
		}

		return false;
	}

	// TODO rewrite
	public bool LocateSpaceRandom(ref int locX1, ref int locY1, int locX2, int locY2,
		int spaceLocWidth, int spaceLocHeight, int maxTries, int regionId = 0, bool buildSite = false, int teraMask = 1)
	{
		int scanWidth = locX2 - locX1 - spaceLocWidth + 2; //xLoc2-xLoc1+1-spaceLocWidth+1;
		int scanHeight = locY2 - locY1 - spaceLocHeight + 2; //yLoc2-yLoc1+1-spaceLocHeight+1;

		for (int i = 0; i < maxTries; i++)
		{
			int locX = locX1 + Misc.Random(scanWidth);
			int locY = locY1 + Misc.Random(scanHeight);
			var canBuildFlag = true;

			//---------- check if the area is all free ----------//

			Location location;

			for (int y = locY + spaceLocHeight - 1; y >= locY; y--)
			{
				for (int x = locX + spaceLocWidth - 1; x >= locX; x--)
				{
					location = GetLoc(x, y);
					if ((buildSite ? !location.CanBuildSite(teraMask) : !location.CanBuildFirm(teraMask)) || location.IsPowerOff())
					{
						canBuildFlag = false;
						break;
					}
				}

				if (!canBuildFlag)
					break;
			}

			if (!canBuildFlag)
				continue;

			//------ check region id. ------------//

			location = GetLoc(locX, locY);

			if (regionId != 0 && location.RegionId != regionId)
				continue;

			//------------------------------------//

			locX1 = locX;
			locY1 = locY;

			return true;
		}

		return false;
	}

	public bool IsHarborRegion(int xLoc, int yLoc, int landRegionId, int seaRegionId)
	{
		if (xLoc + 2 >= GameConstants.MapSize || yLoc + 2 >= GameConstants.MapSize)
			return false;

		for (int y = 0; y < 3; y++)
		{
			for (int x = 0; x < 3; x++)
			{
				int regionId = GetRegionId(xLoc + x, yLoc + y);
				if (regionId != landRegionId && regionId != seaRegionId)
				{
					return false;
				}
			}
		}

		return true;
	}

	public void SetAllPower()
	{
		//--------- set town's influence -----------//

		foreach (Town town in TownArray)
		{
			if (town.NationId != 0)
				SetPower(town.LocX1, town.LocY1, town.LocX2, town.LocY2, town.NationId);
		}

		//--------- set firm's influence -----------//

		foreach (Firm firm in FirmArray)
		{
			if (firm.NationId != 0 && firm.ShouldSetPower)
				SetPower(firm.LocX1, firm.LocY1, firm.LocX2, firm.LocY2, firm.NationId);
		}
	}

	public void SetPower(int locX1, int locY1, int locX2, int locY2, int nationId)
	{
		bool plateauResult = GetLoc((locX1 + locX2) / 2, (locY1 + locY2) / 2).IsPlateau();

		locX1 = Math.Max(0, locX1 - InternalConstants.EFFECTIVE_POWER_DISTANCE + 1);
		locY1 = Math.Max(0, locY1 - InternalConstants.EFFECTIVE_POWER_DISTANCE + 1);
		locX2 = Math.Min(GameConstants.MapSize - 1, locX2 + InternalConstants.EFFECTIVE_POWER_DISTANCE - 1);
		locY2 = Math.Min(GameConstants.MapSize - 1, locY2 + InternalConstants.EFFECTIVE_POWER_DISTANCE - 1);

		int centerY = (locY1 + locY2) / 2;

		for (int locY = locY1; locY <= locY2; locY++)
		{
			int t = Math.Abs(locY - centerY) / 2;

			for (int locX = locX1 + t; locX <= locX2 - t; locX++)
			{
				Location location = GetLoc(locX, locY);

				if (location.Sailable())
					continue;

				if (location.IsPowerOff())
					continue;

				if (location.IsPlateau() != plateauResult)
					continue;

				if (location.PowerNationId == 0)
					location.PowerNationId = nationId;
			}
		}
	}

	// TODO rewrite - works incorrectly
	public void RestorePower(int locX1, int locY1, int locX2, int locY2, int townId, int firmId)
	{
		//TODO rewrite bad code
		int nationRecno = 0;

		if (townId != 0)
		{
			nationRecno = TownArray[townId].NationId;
			//TownArray[townRecno].nation_recno = 0;
		}

		if (firmId != 0)
		{
			nationRecno = FirmArray[firmId].NationId;
			//FirmArray[firmRecno].nation_recno = 0;
		}

		locX1 = Math.Max(0, locX1 - InternalConstants.EFFECTIVE_POWER_DISTANCE + 1);
		locY1 = Math.Max(0, locY1 - InternalConstants.EFFECTIVE_POWER_DISTANCE + 1);
		locX2 = Math.Min(GameConstants.MapSize - 1, locX2 + InternalConstants.EFFECTIVE_POWER_DISTANCE - 1);
		locY2 = Math.Min(GameConstants.MapSize - 1, locY2 + InternalConstants.EFFECTIVE_POWER_DISTANCE - 1);

		int centerY = (locY1 + locY2) / 2;

		for (int locY = locY1; locY <= locY2; locY++)
		{
			int t = Math.Abs(locY - centerY) / 2;

			for (int locX = locX1 + t; locX <= locX2 - t; locX++)
			{
				Location location = GetLoc(locX, locY);

				if (location.PowerNationId == nationRecno)
					location.PowerNationId = 0;
			}
		}

		//--- if some power areas are freed up, see if neighbor towns/firms should take up these power areas ----//

		//TODO drawing
		//if( sys.map_need_redraw )	// when calls set_all_power(), the nation_recno of the calling firm must be reset
		//set_all_power();

		//------- restore the nation recno of the calling town/firm -------//

		//if (townRecno != 0)
			//TownArray[townRecno].nation_recno = nationRecno;

		//if (firmRecno != 0)
			//FirmArray[firmRecno].nation_recno = nationRecno;
	}

	//------- functions related to plants -----//

	private int GetRandomPlantInnerX()
	{
		return Renderer.CellTextureWidth / 4 + Misc.Random(Renderer.CellTextureWidth / 2);
	}

	private int GetRandomPlantInnerY()
	{
		return Renderer.CellTextureHeight * 3 / 8 + Misc.Random(Renderer.CellTextureHeight / 4);
	}

	public void PlantInit()
	{
		const int PLANT_ARRAY_SIZE = 8;

		//TODO should depend on map size
		PlantCount = 0;
		for (int trial = 50; trial > 0; trial--)
		{
			// ------- randomly select a place to seed plant
			int y = 1 + Misc.Random(GameConstants.MapSize - 2);
			int x = 1 + Misc.Random(GameConstants.MapSize - 2);

			Location loc = GetLoc(x, y);
			bool buildFlag = true;
			int teraType = TerrainRes[loc.TerrainId].average_type;

			// ------- all square around are the same terrain type and empty
			for (int y1 = y - 1; y1 <= y + 1; y1++)
			{
				for (int x1 = x - 1; x1 <= x + 1; x1++)
				{
					loc = GetLoc(x1, y1);
					if (!loc.CanAddPlant() || TerrainRes[loc.TerrainId].average_type != teraType)
						buildFlag = false;
				}
			}

			if (buildFlag)
			{
				int[] plantArray = new int[PLANT_ARRAY_SIZE];
				for (int i = 0; i < PLANT_ARRAY_SIZE; ++i)
				{
					plantArray[i] = PlantRes.plant_recno(PlantRes.scan(0, teraType, 0));
				}

				if (plantArray[0] != 0)
				{
					PlantSpray(plantArray, 6 + Misc.Random(4), x, y);
				}
			}
		}

		_plantLimit = PlantCount * 3 / 2;

		// ------- kill some plant ----------//
		for (int trial = 8; trial > 0; trial--)
		{
			PlantDeath(2);
		}
	}

	private void PlantSpray(int[] plantArray, int strength, int x, int y)
	{
		if (strength <= 0)
			return;

		//---------- if the space is empty put a plant on it ----------//
		Location newLoc = GetLoc(x, y);
		int basePlantId = plantArray[Misc.Random(plantArray.Length)];
		PlantInfo plantInfo = PlantRes[basePlantId];
		int plantSize = Misc.Random(plantInfo.bitmap_count);
		if (plantSize > strength)
			plantSize = strength;

		int teraType;
		if (newLoc != null && newLoc.CanAddPlant() &&
		    (plantInfo.tera_type[0] == (teraType = TerrainRes[newLoc.TerrainId].average_type) ||
		     plantInfo.tera_type[1] == teraType || plantInfo.tera_type[2] == teraType))
		{
			newLoc.SetPlant(plantInfo.first_bitmap + plantSize, GetRandomPlantInnerX(), GetRandomPlantInnerY());
			newLoc.SetFlammability(100);
			PlantCount++;
		}
		else if (newLoc != null && newLoc.IsPlant() &&
		         // 1. same type, large override small
		         // newLoc.plant_id() >= plant_res[basePlantId].first_bitmap &&
		         // newLoc.plant_id() < plant_res[basePlantId].first_bitmap + plantSize)
		         // 2. same type, small override large
		         // newLoc.plant_id() > plant_res[basePlantId].first_bitmap + plantSize &&
		         // newLoc.plant_id() < plant_res[basePlantId].first_bitmap + plant_res[basePlantId].bitmap_count)
		         // 3. all types, small override large
		         (newLoc.PlantId() - PlantRes[PlantRes.plant_recno(newLoc.PlantId())].first_bitmap) > plantSize)
		{
			// same kind of plant, but smaller, override by a smaller one
			newLoc.RemovePlant();
			newLoc.SetPlant(plantInfo.first_bitmap + plantSize, GetRandomPlantInnerX(), GetRandomPlantInnerY());
			newLoc.SetFlammability(100);
		}
		else
		{
			plantSize = -1;
		}

		if (plantSize >= 0)
		{
			int trial = 3;
			while (trial-- != 0)
			{
				switch (Misc.Random(8))
				{
					case 0: // north square
						if (y > 0)
							PlantSpray(plantArray, strength - 1, x, y - 1);
						break;
					case 1: // east square
						if (x < GameConstants.MapSize - 1)
							PlantSpray(plantArray, strength - 1, x + 1, y);
						break;
					case 2: // south square
						if (y < GameConstants.MapSize - 1)
							PlantSpray(plantArray, strength - 1, x, y + 1);
						break;
					case 3: // west square
						if (x > 0)
							PlantSpray(plantArray, strength - 1, x - 1, y);
						break;
					case 4: // north west square
						if (y > 0 && x > 0)
							PlantSpray(plantArray, strength - 1, x - 1, y - 1);
						break;
					case 5: // north east square
						if (y > 0 && x < GameConstants.MapSize - 1)
							PlantSpray(plantArray, strength - 1, x + 1, y - 1);
						break;
					case 6: // south east square
						if (y < GameConstants.MapSize - 1 && x < GameConstants.MapSize - 1)
							PlantSpray(plantArray, strength - 1, x + 1, y + 1);
						break;
					case 7: // south west square
						if (y < GameConstants.MapSize - 1 && x > 0)
							PlantSpray(plantArray, strength - 1, x - 1, y + 1);
						break;
				}
			}
		}
	}
	
	private void PlantActions()
	{
		PlantGrow(40);
		PlantReprod(10);
		PlantDeath();
		PlantSpread(50);
	}

	private void PlantGrow(int pGrow = 4, int scanDensity = 8)
	{
		// scan part of the map for plant
		int yBase = Misc.Random(scanDensity);
		int xBase = Misc.Random(scanDensity);
		for (int y = yBase; y < GameConstants.MapSize; y += scanDensity)
		{
			for (int x = xBase; x < GameConstants.MapSize; x += scanDensity)
			{
				Location location = GetLoc(x, y);
				int bitmapId;
				int basePlantId;

				// is a plant and is not at maximum grade
				if (location.IsPlant() && Misc.Random(100) < pGrow &&
				    (basePlantId = PlantRes.plant_recno(bitmapId = location.PlantId())) != 0 &&
				    bitmapId - PlantRes[basePlantId].first_bitmap < PlantRes[basePlantId].bitmap_count - 1)
				{
					// increase the grade of plant
					location.PlantGrow();
				}
			}
		}
	}

	private void PlantReprod(int pReprod = 1, int scanDensity = 8)
	{
		if (PlantCount > _plantLimit)
			return;

		if (5 * PlantCount < 4 * _plantLimit)
			pReprod++; // higher probability to grow

		// determine the rainful, temperature and sunlight
		int temp = Weather.temp_c();

		// scan the map for plant
		int yBase = Misc.Random(scanDensity);
		int xBase = Misc.Random(scanDensity);
		for (int y = yBase; y < GameConstants.MapSize; y += scanDensity)
		{
			for (int x = xBase; x < GameConstants.MapSize; x += scanDensity)
			{
				Location location = GetLoc(x, y);
				int bitmapId, basePlantId, plantGrade;
				// is a plant and grade > 3
				if (location.IsPlant() && (basePlantId = PlantRes.plant_recno(bitmapId = location.PlantId())) != 0 &&
				    ((plantGrade = bitmapId - PlantRes[basePlantId].first_bitmap) >= 3 ||
				     plantGrade == PlantRes[basePlantId].bitmap_count - 1))
				{
					// find the optimal temperature for the plant
					int oTemp = _optTemp[PlantRes[basePlantId].climate_zone - 1];
					int tempEffect = 5 - Math.Abs(oTemp - temp);
					tempEffect = tempEffect > 0 ? tempEffect : 0;

					if (Misc.Random(100) < tempEffect * pReprod)
					{
						// produce the same plant but grade 1,
						int trial = 2;
						while (trial-- != 0)
						{
							Location newLoc = null;
							switch (Misc.Random(8))
							{
								case 0: // north square
									if (y > 0)
										newLoc = GetLoc(x, y - 1);
									break;
								case 1: // east square
									if (x < GameConstants.MapSize - 1)
										newLoc = GetLoc(x + 1, y);
									break;
								case 2: // south square
									if (y < GameConstants.MapSize - 1)
										newLoc = GetLoc(x, y + 1);
									break;
								case 3: // west square
									if (x > 0)
										newLoc = GetLoc(x - 1, y);
									break;
								case 4: // north west square
									if (y > 0 && x > 0)
										newLoc = GetLoc(x - 1, y - 1);
									break;
								case 5: // north east square
									if (y > 0 && x < GameConstants.MapSize - 1)
										newLoc = GetLoc(x + 1, y - 1);
									break;
								case 6: // south east square
									if (y < GameConstants.MapSize - 1 && x < GameConstants.MapSize - 1)
										newLoc = GetLoc(x + 1, y + 1);
									break;
								case 7: // south west square
									if (y < GameConstants.MapSize - 1 && x > 0)
										newLoc = GetLoc(x - 1, y + 1);
									break;
							}

							int teraType;
							PlantInfo plantInfo = PlantRes[basePlantId];
							if (newLoc != null && newLoc.CanAddPlant() &&
							    (plantInfo.tera_type[0] == (teraType = TerrainRes[newLoc.TerrainId].average_type) ||
							     plantInfo.tera_type[1] == teraType || plantInfo.tera_type[2] == teraType))
							{
								newLoc.SetPlant(plantInfo.first_bitmap, GetRandomPlantInnerX(), GetRandomPlantInnerY());
								newLoc.SetFlammability(100);
								PlantCount++;
								break;
							}
						}
					}
				}
			}
		}
	}

	private void PlantSpread(int pSpread = 5)
	{
		if (PlantCount > _plantLimit)
			return;

		if (5 * PlantCount < 4 * _plantLimit)
			pSpread += pSpread;

		if (Misc.Random(1000) >= pSpread)
			return;

		// ------- determine temperature
		int temp = Weather.temp_c();

		// ------- randomly select a place to seed plant
		int y = 1 + Misc.Random(GameConstants.MapSize - 2);
		int x = 1 + Misc.Random(GameConstants.MapSize - 2);

		Location loc = GetLoc(x, y);
		bool buildFlag = true;
		int teraType = TerrainRes[loc.TerrainId].average_type;

		// ------- all square around are the same terrain type and empty
		for (int y1 = y - 1; y1 <= y + 1; ++y1)
		{
			for (int x1 = x - 1; x1 <= x + 1; ++x1)
			{
				loc = GetLoc(x1, y1);
				if (!loc.CanAddPlant() || TerrainRes[loc.TerrainId].average_type != teraType)
					buildFlag = false;
			}
		}

		if (buildFlag)
		{
			int climateZone = 0;
			for (int retry = 0; climateZone == 0 && retry < 5; ++retry)
			{
				for (int j = 0; j < 3; ++j)
				{
					if (Misc.Random(5) > Math.Abs(temp - _optTemp[j]))
					{
						climateZone = j + 1;
						int plantBitmap = PlantRes.scan(climateZone, teraType, 0);
						if (plantBitmap != 0)
						{
							loc = GetLoc(x, y);
							loc.SetPlant(plantBitmap, GetRandomPlantInnerX(), GetRandomPlantInnerY());
							loc.SetFlammability(100);
							PlantCount++;
						}

						break;
					}
				}
			}
		}
	}
	
	private void PlantDeath(int scanDensity = 8)
	{
		int yBase = Misc.Random(scanDensity);
		int xBase = Misc.Random(scanDensity);
		for (int y = yBase; y < GameConstants.MapSize; y += scanDensity)
		{
			for (int x = xBase; x < GameConstants.MapSize; x += scanDensity)
			{
				Location location = GetLoc(x, y);
				if (location.IsPlant())
				{
					int neighbour = 0;
					int totalSpace = 0;

					// west
					if (x > 0)
					{
						totalSpace++;
						if (GetLoc(x - 1, y).IsPlant())
							neighbour++;
					}

					// east
					if (x < GameConstants.MapSize - 1)
					{
						totalSpace++;
						if (GetLoc(x + 1, y).IsPlant())
							neighbour++;
					}

					if (y > 0)
					{
						location = GetLoc(x, y - 1);

						// north square
						totalSpace++;
						if (location.IsPlant())
							neighbour++;

						// north west
						if (x > 0)
						{
							totalSpace++;
							if (GetLoc(x - 1, y).IsPlant())
								neighbour++;
						}

						//	north east
						if (x < GameConstants.MapSize - 1)
						{
							totalSpace++;
							if (GetLoc(x + 1, y).IsPlant())
								neighbour++;
						}
					}

					if (y < GameConstants.MapSize - 1)
					{
						location = GetLoc(x, y + 1);

						// south square
						totalSpace++;
						if (location.IsPlant())
							neighbour++;

						// south west
						if (x > 0)
						{
							totalSpace++;
							if (GetLoc(x - 1, y).IsPlant())
								neighbour++;
						}

						// south east
						if (x < GameConstants.MapSize - 1)
						{
							totalSpace++;
							if (GetLoc(x + 1, y).IsPlant())
								neighbour++;
						}
					}

					// may remove plant if more than two third of the space is occupied
					if (Misc.Random(totalSpace) + 2 * totalSpace / 3 <= neighbour)
					{
						location = GetLoc(x, y);
						GetLoc(x, y).RemovePlant();
						if (location.Flammability() > 50)
							location.SetFlammability(50);
						PlantCount--;
					}
				}
			}
		}
	}

	//------- functions related to fire's spreading ----//

	public void FireInit()
	{
		for (int i = 0; i < _locMatrix.Length; i++)
		{
			Location location = _locMatrix[i];
			if (location.HasHill())
			{
				location.SetFlammability(-100);
			}
			else if (location.IsWall())
			{
				location.SetFlammability(-50);
			}
			else if (location.IsFirm() || location.IsPlant() || location.IsTown())
			{
				location.SetFlammability(100);
			}
			else
			{
				switch (TerrainRes[location.TerrainId].average_type)
				{
					case TerrainTypeCode.TERRAIN_OCEAN:
						location.SetFlammability(-100);
						break;
					case TerrainTypeCode.TERRAIN_DARK_GRASS:
						location.SetFlammability(100);
						break;
					case TerrainTypeCode.TERRAIN_LIGHT_GRASS:
						location.SetFlammability(50);
						break;
					case TerrainTypeCode.TERRAIN_DARK_DIRT:
						location.SetFlammability(-50);
						break;
				}
			}

			// --------- put off fire on the map ----------//
			location.SetFireStrength(-100);
		}
	}

	private void FireSpread(Weather weather)
	{
		int rainSnowReduction = (weather.rain_scale() > 0 || weather.snow_scale() > 0)
			? Config.rain_reduce_fire_rate + (weather.rain_scale() + weather.snow_scale()) / 4 : 0;

		double flameDamage = (double)Config.fire_damage / InternalConstants.ATTACK_SLOW_DOWN;

		// -------------update fire_level-----------
		for (int locY = _scanFireY; locY < GameConstants.MapSize; locY += InternalConstants.SCAN_FIRE_DIST)
		{
			for (int locX = _scanFireX; locX < GameConstants.MapSize; locX += InternalConstants.SCAN_FIRE_DIST)
			{
				Location location = GetLoc(locX, locY);

				int fireValue = location.FireStrength();
				int oldFireValue = fireValue;
				int flammability = location.Flammability();

				// ------- reduce fire_level on raining or snow
				fireValue -= rainSnowReduction;
				if (fireValue < -100)
					fireValue = -100;

				if (fireValue > 0)
				{
					Unit targetUnit;

					// ------- burn wall -------- //
					if (location.IsWall())
					{
						//if (location.attack_wall((int)(4.0 * flameDamage)) != 0)
							//correct_wall(x, y, 2);
					}
					// ------- burn units ---------//
					else if (location.HasUnit(UnitConstants.UNIT_LAND))
					{
						targetUnit = UnitArray[location.UnitId(UnitConstants.UNIT_LAND)];
						targetUnit.HitPoints -= 2.0 * flameDamage;
						if (targetUnit.HitPoints <= 0.0)
							targetUnit.HitPoints = 0.0;
					}
					else if (location.HasUnit(UnitConstants.UNIT_SEA))
					{
						targetUnit = UnitArray[location.UnitId(UnitConstants.UNIT_SEA)];
						targetUnit.HitPoints -= 2.0 * flameDamage;
						if (targetUnit.HitPoints <= 0.0)
							targetUnit.HitPoints = 0.0;
					}
					else if (location.IsFirm() && FirmRes[FirmArray[location.FirmId()].FirmType].buildable)
					{
						Firm targetFirm = FirmArray[location.FirmId()];
						targetFirm.HitPoints -= flameDamage;
						if (targetFirm.HitPoints <= 0.0)
						{
							targetFirm.HitPoints = 0.0;
							SERes.sound(targetFirm.LocCenterX, targetFirm.LocCenterY, 1, 'F', targetFirm.FirmType, "DIE");
							FirmArray.DeleteFirm(targetFirm);
						}
					}

					if (Config.fire_spread_rate > 0)
					{
						// -------- normalize wind_speed between -WIND_SPREADFIRE*SPREAD_RATE to +WIND_SPREADFIRE*SPREAD_RATE -------
						int windCos = (int)(weather.wind_speed() * Math.Cos(weather.wind_direct_rad()) / 100.0 * Config.fire_spread_rate *
						                    Config.wind_spread_fire_rate);
						int windSin = (int)(weather.wind_speed() * Math.Sin(weather.wind_direct_rad()) / 100.0 * Config.fire_spread_rate *
						                    Config.wind_spread_fire_rate);

						Location nearLoc;
						// spread of north square
						if (locY > 0 && (nearLoc = GetLoc(locX, locY - 1)).Flammability() > 0 && nearLoc.FireStrength() <= 0)
						{
							nearLoc.AddFireStrength(Math.Max(Config.fire_spread_rate + windSin, 0));
						}

						// spread of south square
						if (locY < GameConstants.MapSize - 1 && (nearLoc = GetLoc(locX, locY + 1)).Flammability() > 0 && nearLoc.FireStrength() <= 0)
						{
							nearLoc.AddFireStrength(Math.Max(Config.fire_spread_rate - windSin, 0));
						}

						// spread of west square
						if (locX > 0 && (nearLoc = GetLoc(locX - 1, locY)).Flammability() > 0 && nearLoc.FireStrength() <= 0)
						{
							nearLoc.AddFireStrength(Math.Max(Config.fire_spread_rate - windCos, 0));
						}

						// spread of east square
						if (locX < GameConstants.MapSize - 1 && (nearLoc = GetLoc(locX + 1, locY)).Flammability() > 0 && nearLoc.FireStrength() <= 0)
						{
							nearLoc.AddFireStrength(Math.Max(Config.fire_spread_rate + windCos, 0));
						}
					}

					if (flammability > 0)
					{
						// increase fire_level on its own
						if (++fireValue > 100)
							fireValue = 100;

						flammability -= Config.fire_fade_rate;
						// if a plant on it then remove the plant, if flammability <= 0
						if (location.IsPlant() && flammability <= 0)
						{
							location.RemovePlant();
							PlantCount--;
						}
					}
					else
					{
						// fireValue > 0, flammability < 0
						// putting of fire
						if (flammability >= -30)
						{
							fireValue -= 2;
							flammability -= Config.fire_fade_rate;
							if (flammability < -30)
								flammability = -30;
						}
						else if (flammability >= -50)
						{
							fireValue -= 2;
							flammability -= Config.fire_fade_rate;
							if (flammability < -50)
								flammability = -50;
						}
						else
						{
							fireValue = -100;
							flammability -= Config.fire_fade_rate;
							if (flammability < -100)
								flammability = -100;
						}

						// if a plant on it then remove the plant, if flammability <= 0
						if (location.IsPlant() && flammability <= 0)
						{
							location.RemovePlant();
							PlantCount--;
						}
					}
				}
				else // fireValue < 0
				{
					// ---------- fire_level drop slightly ----------
					if (fireValue > -100)
						fireValue--;

					// ---------- restore flammability ------------
					if (flammability >= -30 && flammability < 50 && Misc.Random(100) < Config.fire_restore_prob)
						flammability++;
				}

				// ---------- update new fire level -----------
				// ---------- when fire is put off so the fire will not light again very soon
				if (fireValue <= 0 && oldFireValue > 0)
				{
					fireValue -= 50;
				}

				location.SetFireStrength(fireValue);
				location.SetFlammability(flammability);
			}
		}
	}

	public void SetupFire(int locX, int locY, int fireStrength = 30)
	{
		Location location = GetLoc(locX, locY);
		if (location.FireStrength() < fireStrength)
		{
			location.SetFireStrength(fireStrength);
		}
	}

	// ------ function related to weather ---------//
	private void EarthQuake()
	{
		for (int locY = 0; locY < GameConstants.MapSize; locY++)
		{
			for (int locX = 0; locX < GameConstants.MapSize; locX++)
			{
				Location location = GetLoc(locX, locY);
				if (location.IsWall())
				{
					location.AttackWall(Weather.quake_rate(locX, locY) / 2);
				}
			}
		}

		int firmDamage = 0;
		int firmDie = 0;

		foreach (Firm firm in FirmArray)
		{
			if (!FirmRes[firm.FirmType].buildable)
				continue;

			int locX = firm.LocCenterX;
			int locY = firm.LocCenterY;
			firm.HitPoints -= Weather.quake_rate(locX, locY);
			if (firm.OwnFirm())
				firmDamage++;
			if (firm.HitPoints <= 0.0)
			{
				firm.HitPoints = 0.0;
				if (firm.OwnFirm())
					firmDie++;
				SERes.sound(firm.LocCenterX, firm.LocCenterY, 1, 'F', firm.FirmType, "DIE");
				FirmArray.DeleteFirm(firm);
			}
		}

		int townDamage = 0;
		foreach (Town town in TownArray)
		{
			bool ownTown = (town.NationId == NationArray.player_recno);
			int beforePopulation = town.Population;
			for (int damage = Weather.quake_rate(town.LocCenterX, town.LocCenterY) / 10; damage > 0 && !TownArray.IsDeleted(town.TownId); damage--)
			{
				town.KillTownPeople(0);
			}

			int peopleDied = TownArray.IsDeleted(town.TownId) ? beforePopulation : beforePopulation - town.Population;
			if (ownTown)
				townDamage += peopleDied;
		}

		int unitDamage = 0;
		int unitsDied = 0;
		foreach (Unit unit in UnitArray)
		{
			// no damage to air units, sea units and units inside camps and bases
			if (unit.MobileType == UnitConstants.UNIT_AIR || unit.MobileType == UnitConstants.UNIT_SEA || !unit.IsVisible())
				continue;

			double damage = Weather.quake_rate(unit.CurLocX, unit.CurLocY) * unit.MaxHitPoints / 200.0;
			if (damage >= unit.HitPoints)
				damage = unit.HitPoints - 1.0;
			if (damage < 5.0)
				damage = 5.0;

			unit.HitPoints -= damage;
			if (unit.IsOwn())
				unitDamage++;

			if (unit.HitPoints <= 0.0)
			{
				unit.HitPoints = 0.0;
				if (unit.IsOwn())
					unitsDied++;
			}
		}

		NewsArray.earthquake_damage(unitDamage - unitsDied, unitsDied, townDamage, firmDamage - firmDie, firmDie);
	}

	private void LightningStrike(int locX, int locY, int radius = 0)
	{
		for (int nearLocY = locY - radius; nearLocY <= locY + radius; nearLocY++)
		{
			for (int nearLocX = locX - radius; nearLocX <= locX + radius; nearLocX++)
			{
				if (!Misc.IsLocationValid(nearLocX, nearLocY))
					continue;

				Location location = GetLoc(nearLocX, nearLocY);
				if (location.IsPlant())
				{
					// ---- add a fire on it ------//
					location.SetFireStrength(80);
					if (location.CanSetFire() && location.FireStrength() < 5)
						location.SetFireStrength(5);
				}
			}
		}

		// ------ check hitting units -------//
		foreach (Unit unit in UnitArray)
		{
			// no damage to units inside camps and bases
			if (!unit.IsVisible())
				continue;

			if (unit.CurLocX <= locX + radius && unit.CurLocX + unit.SpriteInfo.LocWidth > locX - radius &&
			    unit.CurLocY <= locY + radius && unit.CurLocY + unit.SpriteInfo.LocHeight > locY - radius)
			{
				unit.HitPoints -= (double)unit.SpriteInfo.LightningDamage / InternalConstants.ATTACK_SLOW_DOWN;

				// ---- add news -------//
				if (unit.IsOwn())
					NewsArray.lightning_damage(unit.CurLocX, unit.CurLocY,
						News.NEWS_LOC_UNIT, unit.SpriteId, unit.HitPoints <= 0.0 ? 1 : 0);

				if (unit.HitPoints <= 0.0)
					unit.HitPoints = 0.0;
			}
		}

		List<Firm> firmsToDelete = new List<Firm>();
		foreach (Firm firm in FirmArray)
		{
			if (!FirmRes[firm.FirmType].buildable)
				continue;

			if (firm.LocX1 <= locX + radius && firm.LocX2 >= locX - radius && firm.LocY1 <= locY + radius && firm.LocY2 >= locY - radius)
			{
				firm.HitPoints -= 50.0 / InternalConstants.ATTACK_SLOW_DOWN;

				// ---- add news -------//
				if (firm.OwnFirm())
					NewsArray.lightning_damage(firm.LocCenterX, firm.LocCenterY,
						News.NEWS_LOC_FIRM, firm.FirmId, firm.HitPoints <= 0.0 ? 1 : 0);

				// ---- add a fire on it ------//
				Location location = GetLoc(firm.LocCenterX, firm.LocCenterY);
				if (location.CanSetFire() && location.FireStrength() < 5)
					location.SetFireStrength(5);

				if (firm.HitPoints <= 0.0)
				{
					firm.HitPoints = 0.0;
					SERes.sound(firm.LocCenterX, firm.LocCenterY, 1, 'F', firm.FirmType, "DIE");
					firmsToDelete.Add(firm);
				}
			}
		}

		foreach (Firm firm in firmsToDelete)
		{
			FirmArray.DeleteFirm(firm);
		}

		foreach (Town town in TownArray)
		{
			if (town.LocX1 <= locX + radius && town.LocX2 >= locX - radius && town.LocY1 <= locY + radius && town.LocY2 >= locY - radius)
			{
				// TODO check is objectDie: 0 correct?
				// ---- add news -------//
				if (town.NationId == NationArray.player_recno)
					NewsArray.lightning_damage(town.LocCenterX, town.LocCenterY, News.NEWS_LOC_TOWN, town.TownId, 0);

				// ---- add a fire on it ------//
				Location location = GetLoc(town.LocCenterX, town.LocCenterY);
				if (location.CanSetFire() && location.FireStrength() < 5)
					location.SetFireStrength(5);

				town.KillTownPeople(0);
			}
		}
	}

	private void ProcessAmbientSound()
	{
		int temp = Weather.temp_c();
		if (Weather.rain_scale() == 0 && temp >= 15 && Misc.Random(temp) >= 12)
		{
			int bird = Misc.Random(InternalConstants.MAX_BIRD) + 1;
			string sndFile = "BIRDS";
			sndFile += (bird / 10) + '0';
			sndFile += (bird % 10) + '0';

			//TODO rewrite
			//int xLoc = Misc.Random(GameConstants.MapSize) - (zoom_matrix.top_x_loc + zoom_matrix.disp_x_loc / 2);
			//int yLoc = Misc.Random(GameConstants.MapSize) - (zoom_matrix.top_y_loc + zoom_matrix.disp_y_loc / 2);
			int locX = Misc.Random(GameConstants.MapSize);
			int locY = Misc.Random(GameConstants.MapSize);
			PosVolume p = new PosVolume(locX, locY);
			RelVolume relVolume = new RelVolume(p, 200, GameConstants.MapSize);
			if (relVolume.rel_vol < 80)
				relVolume.rel_vol = 80;

			SECtrl.request(sndFile, relVolume);
		}
	}
}
