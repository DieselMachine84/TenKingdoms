using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class World
{
	public const int SCAN_FIRE_DIST = 4;
	public const int MIN_LAND_COST = 500000;
	public const int MIN_GRASS_HEIGHT = 100;
	public const int MIN_HILL_HEIGHT = 230;
	public const int MIN_MOUNTAIN_HEIGHT = 242;
	public const int MIN_ICE_HEIGHT = 252;

	private Location[] loc_matrix = new Location[GameConstants.MapSize * GameConstants.MapSize];

	public ulong next_scroll_time; // next scroll time
	public int scan_fire_x; // cycle from 0 to SCAN_FIRE_DIST-1
	public int scan_fire_y;
	public int lightning_signal;
	public int plant_count;
	public int plant_limit;
	public int[] opt_temp = { 32, 25, 28 };

	private TerrainRes TerrainRes => Sys.Instance.TerrainRes;
	private HillRes HillRes => Sys.Instance.HillRes;
	private PlantRes PlantRes => Sys.Instance.PlantRes;
	private RockRes RockRes => Sys.Instance.RockRes;
	private FirmRes FirmRes => Sys.Instance.FirmRes;
	private UnitRes UnitRes => Sys.Instance.UnitRes;
	private SERes SERes => Sys.Instance.SERes;
	private SECtrl SECtrl => Sys.Instance.SECtrl;
	private Config Config => Sys.Instance.Config;
	private Info Info => Sys.Instance.Info;
	private Power Power => Sys.Instance.Power;
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
	private RockArray RockArray => Sys.Instance.RockArray;
	private RockArray DirtArray => Sys.Instance.DirtArray;

	public World()
	{
		scan_fire_x = 0;
		scan_fire_y = 0;
		lightning_signal = 0;
		ClearLocation();
	}
	
	public Location get_loc(int xLoc, int yLoc)
	{
		return loc_matrix[GameConstants.MapSize * yLoc + xLoc];
	}

	public int get_region_id(int xLoc, int yLoc)
	{
		return loc_matrix[GameConstants.MapSize * yLoc + xLoc].region_id;
	}

	public void load_map(byte[] para)
	{
		throw new NotImplementedException();
	}

	public void go_loc(int xLoc, int yLoc, bool selectFlag = false)
	{
		//TODO implement
	}

	public void disp_next(int seekDir, bool sameNation)
	{
		//--- if the selected one is a unit ----//

		if (UnitArray.selected_recno != 0)
		{
			UnitArray.disp_next(seekDir, sameNation);
		}

		//--- if the selected one is a firm ----//

		if (FirmArray.selected_recno != 0)
		{
			FirmArray.disp_next(seekDir, sameNation);
		}

		//--- if the selected one is a town ----//

		if (TownArray.selected_recno != 0)
		{
			TownArray.DisplayNext(seekDir, sameNation);
		}

		//--- if the selected one is a natural resource site ----//

		if (SiteArray.selected_recno != 0)
		{
			SiteArray.disp_next(seekDir, sameNation);
		}
	}

	public int get_unit_recno(int xLoc, int yLoc, int mobileType)
	{
		if (mobileType == UnitConstants.UNIT_AIR)
			return get_loc(xLoc, yLoc).air_cargo_recno;
		else
			return get_loc(xLoc, yLoc).cargo_recno;
	}

	public void set_unit_recno(int xLoc, int yLoc, int mobileType, int newCargoRecno)
	{
		if (mobileType == UnitConstants.UNIT_AIR)
			get_loc(xLoc, yLoc).air_cargo_recno = newCargoRecno;
		else
			get_loc(xLoc, yLoc).cargo_recno = newCargoRecno;
	}

	public int distance_rating(int xLoc1, int yLoc1, int xLoc2, int yLoc2)
	{
		int curDistance = Misc.points_distance(xLoc1, yLoc1, xLoc2, yLoc2);

		return 100 - 100 * curDistance / GameConstants.MapSize;
	}

	public void unveil(int xLoc1, int yLoc1, int xLoc2, int yLoc2)
	{
		if (Config.explore_whole_map)
			return;

		xLoc1 = Math.Max(0, xLoc1 - GameConstants.EXPLORE_RANGE);
		yLoc1 = Math.Max(0, yLoc1 - GameConstants.EXPLORE_RANGE);
		xLoc2 = Math.Min(GameConstants.MapSize - 1, xLoc2 + GameConstants.EXPLORE_RANGE);
		yLoc2 = Math.Min(GameConstants.MapSize - 1, yLoc2 + GameConstants.EXPLORE_RANGE);

		explore(xLoc1, yLoc1, xLoc2, yLoc2);
	}

	public void explore(int xLoc1, int yLoc1, int xLoc2, int yLoc2)
	{
		if (Config.explore_whole_map)
			return;

		//char* 	 imageBuf = map_matrix.save_image_buf + sizeof(short)*2;
		byte[] nationColorArray = NationArray.nation_power_color_array;
		//char* 	 writePtr;

		int shadowMapDist = GameConstants.MapSize + 1;
		int tileYOffset;
		//Location* northWestPtr;
		int tilePixel;

		for (int yLoc = yLoc1; yLoc <= yLoc2; yLoc++)
		{
			for (int xLoc = xLoc1; xLoc <= xLoc2; xLoc++)
			{
				Location location = get_loc(xLoc, yLoc);
				if (!location.explored())
				{
					location.explored_on();

					//TODO rewrite drawing
					//-------- draw pixel ----------//

					//writePtr = imageBuf+MAP_WIDTH*yLoc+xLoc;

					//switch( world.map_matrix.map_mode )
					//{
					//case MAP_MODE_TERRAIN:
					//if( locPtr.fire_str() > 0)
					//*writePtr = (char) FIRE_COLOR;

					//else if( locPtr.is_plant() )
					//*writePtr = plant_res.plant_map_color;

					//else
					//{
					//tileYOffset = (yLoc & TERRAIN_TILE_Y_MASK) * TERRAIN_TILE_WIDTH;

					//tilePixel = terrain_res.get_map_tile(locPtr.terrain_id)[tileYOffset + (xLoc & TERRAIN_TILE_X_MASK)];

					//if( xLoc == 0 || yLoc == 0)
					//{
					//*writePtr = tilePixel;
					//}
					//else
					//{
					//northWestPtr = locPtr - shadowMapDist;
					//if( (terrain_res[locPtr.terrain_id].average_type >=
					//terrain_res[northWestPtr.terrain_id].average_type) )
					//{
					//*writePtr = tilePixel;
					//}
					//else
					//{
					//*writePtr = (char) VGA_GRAY;
					//}
					//}
					//break;
					//}
					//break;

					//case MAP_MODE_SPOT:
					//if( locPtr.sailable() )
					//*writePtr = (char) 0x32;

					//else if( locPtr.has_hill() )
					//*writePtr = (char) V_BROWN;

					//else if( locPtr.is_plant() )
					//*writePtr = (char) V_DARK_GREEN;

					//else
					//*writePtr = (char) VGA_GRAY+10;
					//break;

					//case MAP_MODE_POWER:
					//if( locPtr.sailable() )
					//*writePtr = (char) 0x32;

					//else if( locPtr.has_hill() )
					//*writePtr = (char) V_BROWN;

					//else if( locPtr.is_plant() )
					//*writePtr = (char) V_DARK_GREEN;

					//else
					//*writePtr = nationColorArray[locPtr.power_nation_recno];
					//break;
					//}

					//---- if the command base of the opponent revealed, establish contact ----//

					if (location.is_firm())
					{
						Firm firm = FirmArray[location.firm_recno()];

						if (firm.nation_recno > 0 && NationArray.player_recno != 0)
						{
							NationRelation relation = NationArray.player.get_relation(firm.nation_recno);

							if (!relation.has_contact)
							{
								//TODO remote
								//if( !remote.is_enable() )
								//{
								NationArray.player.establish_contact(firm.nation_recno);
								//}
								//else
								//{
								//if( !relation.contact_msg_flag )
								//{
								// packet structure : <player nation> <explored nation>
								//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_NATION_CONTACT, 2*sizeof(short));
								//*shortPtr = nation_array.player_recno;
								//shortPtr[1] = firmPtr.nation_recno;
								//relation.contact_msg_flag = 1;
								//}
								//}
							}
						}
					}

					if (location.is_town())
					{
						Town town = TownArray[location.town_recno()];

						if (town.NationId > 0 && NationArray.player_recno != 0)
						{
							NationRelation relation = NationArray.player.get_relation(town.NationId);

							if (!relation.has_contact)
							{
								//if( !remote.is_enable() )
								//{
								NationArray.player.establish_contact(town.NationId);
								//}
								//else
								//{
								//if( !relation.contact_msg_flag )
								//{
								// packet structure : <player nation> <explored nation>
								//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_NATION_CONTACT, 2*sizeof(short));
								//*shortPtr = nation_array.player_recno;
								//shortPtr[1] = townPtr.nation_recno;
								//relation.contact_msg_flag = 1;
								//}
								//}
							}
						}
					}
				}
			}
		}
	}

	// always call unveil before visit //
	public void visit(int xLoc1, int yLoc1, int xLoc2, int yLoc2, int range, int extend = 0)
	{
		if (Config.fog_of_war)
		{
			int left = Math.Max(0, xLoc1 - range);
			int top = Math.Max(0, yLoc1 - range);
			int right = Math.Min(GameConstants.MapSize - 1, xLoc2 + range);
			int bottom = Math.Min(GameConstants.MapSize - 1, yLoc2 + range);

			// ----- mark the visit_level of the square around the unit ------//
			for (int yLoc = top; yLoc <= bottom; yLoc++)
			{
				for (int xLoc = left; xLoc <= right; xLoc++)
				{
					Location location = get_loc(xLoc, yLoc);
					location.set_visited();
				}
			}

			// ----- visit_level decreasing outside the visible range ------//
			if (extend > 0)
			{
				int visitLevel = Location.FULL_VISIBILITY;
				int levelDrop = (Location.FULL_VISIBILITY - Location.EXPLORED_VISIBILITY) / (extend + 1);
				xLoc1 -= range;
				xLoc2 += range;
				yLoc1 -= range;
				yLoc2 += range;
				for (++range; extend > 0; --extend, ++range)
				{
					xLoc1--;
					xLoc2++;
					yLoc1--;
					yLoc2++;
					visitLevel -= levelDrop;
					visit_shell(xLoc1, yLoc1, xLoc2, yLoc2, visitLevel);
				}
			}
		}
	}

	public void visit_shell(int xLoc1, int yLoc1, int xLoc2, int yLoc2, int visitLevel)
	{
		int left = Math.Max(0, xLoc1);
		int top = Math.Max(0, yLoc1);
		int right = Math.Min(GameConstants.MapSize - 1, xLoc2);
		int bottom = Math.Max(GameConstants.MapSize - 1, yLoc2);

		// ------- top side ---------//
		if (yLoc1 >= 0)
		{
			for (int x = left; x <= right; ++x)
			{
				get_loc(x, yLoc1).set_visited(visitLevel);
			}
		}

		// ------- bottom side ---------//
		if (yLoc2 < GameConstants.MapSize)
		{
			for (int x = left; x <= right; ++x)
			{
				get_loc(x, yLoc2).set_visited(visitLevel);
			}
		}

		// ------- left side -----------//
		if (xLoc1 >= 0)
		{
			for (int y = top; y <= bottom; ++y)
			{
				get_loc(xLoc1, y).set_visited(visitLevel);
			}
		}

		// ------- right side -----------//
		if (xLoc2 < GameConstants.MapSize)
		{
			for (int y = top; y <= bottom; ++y)
			{
				get_loc(xLoc2, y).set_visited(visitLevel);
			}
		}
	}

	public int can_build_firm(int xLoc1, int yLoc1, int firmId, int unitRecno = -1)
	{
		if (xLoc1 < 0 || yLoc1 < 0 || xLoc1 > GameConstants.MapSize || yLoc1 > GameConstants.MapSize)
			return 0;

		//------------------------------------------//

		FirmInfo firmInfo = FirmRes[firmId];

		int xLoc, yLoc;
		int xLoc2 = xLoc1 + firmInfo.loc_width - 1;
		int yLoc2 = yLoc1 + firmInfo.loc_height - 1;
		if (xLoc2 >= GameConstants.MapSize || yLoc2 > GameConstants.MapSize)
			return 0;

		int teraMask, pierFlag;

		switch (firmInfo.tera_type)
		{
			case 1: // default : land firm
			case 2: // sea firm
			case 3: // land or sea firm
				teraMask = firmInfo.tera_type;
				for (yLoc = yLoc1; yLoc <= yLoc2; yLoc++)
				{
					for (xLoc = xLoc1; xLoc <= xLoc2; xLoc++)
					{
						Location location = get_loc(xLoc, yLoc);
						if (!location.can_build_firm(teraMask) && (!location.has_unit(UnitConstants.UNIT_LAND) ||
						                                           location.unit_recno(UnitConstants.UNIT_LAND) !=
						                                           unitRecno))
							return 0;

						// don't allow building any buildings other than mines on a location with a site
						if (firmId != Firm.FIRM_MINE && location.has_site())
							return 0;
					}
				}

				return 1;

			case 4: // special firm, such as harbor
				// must be 3x3,
				// centre square of one side is land (teraMask=1),
				// two squares on that side can be land or sea (teraMask=3)
				// and other (6 squares) are sea (teraMask=2)
				if (firmInfo.loc_width != 3 || firmInfo.loc_height != 3)
					return 0;

				pierFlag = 1 | 2 | 4 | 8; // bit0=north, bit1=south, bit2=west, bit3=east
				for (yLoc = yLoc1; yLoc <= yLoc2; yLoc++)
				{
					for (xLoc = xLoc1; xLoc <= xLoc2; xLoc++)
					{
						Location location = get_loc(xLoc, yLoc);
						// don't allow building any buildings other than mines on a location with a site
						if (location.has_site())
							return 0;

						int[,] northPierTera = { { 2, 2, 2 }, { 2, 2, 2 }, { 3, 1, 3 } };
						int[,] southPierTera = { { 3, 1, 3 }, { 2, 2, 2 }, { 2, 2, 2 } };
						int[,] westPierTera = { { 2, 2, 3 }, { 2, 2, 1 }, { 2, 2, 3 } };
						int[,] eastPierTera = { { 3, 2, 2 }, { 1, 2, 2 }, { 3, 2, 2 } };

						int x = xLoc - xLoc1;
						int y = yLoc - yLoc1;
						if (!location.can_build_harbor(northPierTera[y, x]))
							pierFlag &= ~1;
						if (!location.can_build_harbor(southPierTera[y, x]))
							pierFlag &= ~2;
						if (!location.can_build_harbor(westPierTera[y, x]))
							pierFlag &= ~4;
						if (!location.can_build_harbor(eastPierTera[y, x]))
							pierFlag &= ~8;
					}
				}

				return pierFlag;

			// other tera_type here
			default:
				return 0;
		}
	}

	public bool can_build_town(int xLoc1, int yLoc1, int unitRecno = -1)
	{
		int xLoc2 = xLoc1 + InternalConstants.TOWN_WIDTH - 1;
		int yLoc2 = yLoc1 + InternalConstants.TOWN_HEIGHT - 1;

		if (xLoc2 >= GameConstants.MapSize || yLoc2 >= GameConstants.MapSize)
			return false;

		for (int yLoc = yLoc1; yLoc <= yLoc2; yLoc++)
		{
			for (int xLoc = xLoc1; xLoc <= xLoc2; xLoc++)
			{
				Location location = get_loc(xLoc, yLoc);
				// allow the building unit to stand in the area
				if (!location.can_build_town() &&
				    (!location.has_unit(UnitConstants.UNIT_LAND) ||
				     location.unit_recno(UnitConstants.UNIT_LAND) != unitRecno))
					return false;
			}
		}

		return true;
	}

	public bool locate_space(ref int xLoc1, ref int yLoc1, int xLoc2, int yLoc2,
		int spaceLocWidth, int spaceLocHeight, int mobileType = UnitConstants.UNIT_LAND, int regionId = 0,
		bool buildFlag = false)
	{
		if (regionId == 0)
			regionId = get_loc(xLoc1, yLoc1).region_id;

		bool isPlateau = get_loc(xLoc1, yLoc1).is_plateau();

		//-----------------------------------------------------------//
		// xLoc, yLoc is the adjusted upper left corner location of
		// the firm. with the adjustment, it is easier to do the following
		// checking.
		//-----------------------------------------------------------//

		int xLoc = xLoc1 - spaceLocWidth + 1;
		int yLoc = yLoc1 - spaceLocHeight + 1;

		if (xLoc < 0)
			xLoc = 0;
		if (yLoc < 0)
			yLoc = 0;

		int width = xLoc2 - xLoc + 1;
		int height = yLoc2 - yLoc + 1;

		while (true)
		{
			//-----------------------------------------------------------//
			// step 1
			//-----------------------------------------------------------//
			int xOffset = width / 2;
			int yOffset = height;
			int x, y;

			x = xLoc + xOffset;
			y = yLoc + yOffset;

			if (x >= 0 && y >= 0 && x + spaceLocWidth - 1 < GameConstants.MapSize && y + spaceLocHeight - 1 < GameConstants.MapSize)
			{
				if (mobileType == UnitConstants.UNIT_LAND || (x % 2 == 0 && y % 2 == 0))
				{
					Location location = get_loc(x, y);

					if (location.region_id == regionId && location.is_plateau() == isPlateau &&
					    check_unit_space(x, y, x + spaceLocWidth - 1, y + spaceLocHeight - 1,
						    mobileType, buildFlag))
					{
						xLoc1 = x;
						yLoc1 = y;
						return true;
					}
				}
			}

			int sign = -1;
			int i, j, k, limit;

			//-----------------------------------------------------------//
			// step 2
			//-----------------------------------------------------------//
			//y = yLoc + yOffset;
			limit = width + 2;
			for (i = 1; i < limit; i++)
			{
				xOffset += sign * i;
				x = xLoc + xOffset;

				if (x >= 0 && y >= 0 && x + spaceLocWidth - 1 < GameConstants.MapSize && y + spaceLocHeight - 1 < GameConstants.MapSize)
				{
					if (mobileType == UnitConstants.UNIT_LAND || (x % 2 == 0 && y % 2 == 0))
					{
						Location location = get_loc(x, y);

						if (location.region_id == regionId && location.is_plateau() == isPlateau &&
						    check_unit_space(x, y, x + spaceLocWidth - 1, y + spaceLocHeight - 1,
							    mobileType, buildFlag))
						{
							xLoc1 = x;
							yLoc1 = y;
							return true;
						}
					}
				}

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

			for (j = 0; j < limit; j++)
			{
				if (j % 2 != 0)
				{
					//x = xLoc + lastX;
					xOffset = lastX;
					x = xLoc + xOffset;
					//y = yLoc + yOffset;

					if (x >= 0 && y >= 0 && x + spaceLocWidth - 1 < GameConstants.MapSize && y + spaceLocHeight - 1 < GameConstants.MapSize)
					{
						if (mobileType == UnitConstants.UNIT_LAND || (x % 2 == 0 && y % 2 == 0))
						{
							Location location = get_loc(x, y);

							if (location.region_id == regionId && location.is_plateau() == isPlateau &&
							    check_unit_space(x, y, x + spaceLocWidth - 1, y + spaceLocHeight - 1,
								    mobileType, buildFlag))
							{
								xLoc1 = x;
								yLoc1 = y;
								return true;
							}
						}
					}
				}
				else
				{
					xOffset = lastX + r;
					yOffset--;

					x = xLoc + xOffset;
					y = yLoc + yOffset;

					if (x >= 0 && y >= 0 && x + spaceLocWidth - 1 < GameConstants.MapSize && y + spaceLocHeight - 1 < GameConstants.MapSize)
					{
						if (mobileType == UnitConstants.UNIT_LAND || (x % 2 == 0 && y % 2 == 0))
						{
							Location location = get_loc(x, y);

							if (location.region_id == regionId && location.is_plateau() == isPlateau &&
							    check_unit_space(x, y, x + spaceLocWidth - 1, y + spaceLocHeight - 1,
								    mobileType, buildFlag))
							{
								xLoc1 = x;
								yLoc1 = y;
								return true;
							}
						}
					}
				}
			}

			//-----------------------------------------------------------//
			// step 4
			//-----------------------------------------------------------//
			y = yLoc + yOffset;
			for (k = 0; k <= width; k++)
			{
				sign *= -1;
				i--;
				r = sign * i;
				xOffset -= r;

				x = xLoc + xOffset;

				if (x >= 0 && y >= 0 && x + spaceLocWidth - 1 < GameConstants.MapSize && y + spaceLocHeight - 1 < GameConstants.MapSize)
				{
					if (mobileType == UnitConstants.UNIT_LAND || (x % 2 == 0 && y % 2 == 0))
					{
						Location location = get_loc(x, y);

						if (location.region_id == regionId && location.is_plateau() == isPlateau &&
						    check_unit_space(x, y, x + spaceLocWidth - 1, y + spaceLocHeight - 1,
							    mobileType, buildFlag))
						{
							xLoc1 = x;
							yLoc1 = y;
							return true;
						}
					}
				}
			}

			//-----------------------------------------------------------//
			// re-init the parameters
			//-----------------------------------------------------------//
			if (xLoc <= 0 && yLoc <= 0 && width >= GameConstants.MapSize && height >= GameConstants.MapSize)
				break; // the whole map has been checked

			width += 2;
			height += 2;

			xLoc -= 1;
			yLoc -= 1;
			if (xLoc < 0)
			{
				xLoc = 0;
				width--;
			}

			if (yLoc < 0)
			{
				yLoc = 0;
				height--;
			}

			if (xLoc + width > GameConstants.MapSize)
				width--;
			if (yLoc + height > GameConstants.MapSize)
				height--;

			//if(width==xLoc2-xLoc1+spaceLocWidth && height==yLoc2-yLoc1+spaceLocHeight) // terminate the checking
			// return 0;
		}

		return false;
	}

	public bool check_unit_space(int xLoc1, int yLoc1, int xLoc2, int yLoc2,
		int mobileType = UnitConstants.UNIT_LAND, bool buildFlag = false)
	{
		if (xLoc1 < 0 || xLoc1 >= GameConstants.MapSize)
			return false;
		if (yLoc1 < 0 || yLoc1 >= GameConstants.MapSize)
			return false;
		if (xLoc2 < 0 || xLoc2 >= GameConstants.MapSize)
			return false;
		if (yLoc2 < 0 || yLoc2 >= GameConstants.MapSize)
			return false;

		bool canBuildFlag = true;

		for (int y = yLoc1; y <= yLoc2; y++)
		{
			for (int x = xLoc1; x <= xLoc2; x++)
			{
				Location location = get_loc(x, y);
				// if build a firm/town, there must not be any sites in the area
				if (!location.can_move(mobileType) || (buildFlag && (location.is_power_off() || location.has_site())))
				{
					canBuildFlag = false;
					break;
				}
			}

			if (!canBuildFlag)
				break;
		}

		return canBuildFlag;
	}

	public bool locate_space_random(ref int xLoc1, ref int yLoc1, int xLoc2, int yLoc2,
		int spaceLocWidth, int spaceLocHeight, int maxTries, int regionId = 0, bool buildSite = false, int teraMask = 1)
	{
		int scanWidth = xLoc2 - xLoc1 - spaceLocWidth + 2; //xLoc2-xLoc1+1-spaceLocWidth+1;
		int scanHeight = yLoc2 - yLoc1 - spaceLocHeight + 2; //yLoc2-yLoc1+1-spaceLocHeight+1;

		for (int i = 0; i < maxTries; i++)
		{
			int xLoc = xLoc1 + Misc.Random(scanWidth);
			int yLoc = yLoc1 + Misc.Random(scanHeight);
			var canBuildFlag = true;

			//---------- check if the area is all free ----------//

			Location location;

			for (int y = yLoc + spaceLocHeight - 1; y >= yLoc; y--)
			{
				for (int x = xLoc + spaceLocWidth - 1; x >= xLoc; x--)
				{
					location = get_loc(x, y);
					if ((buildSite ? !location.can_build_site(teraMask) : !location.can_build_firm(teraMask)) ||
					    location.is_power_off())
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

			location = get_loc(xLoc, yLoc);

			if (regionId != 0 && location.region_id != regionId)
				continue;

			//------------------------------------//

			xLoc1 = xLoc;
			yLoc1 = yLoc;

			return true;
		}

		return false;
	}

	public bool is_adjacent_region(int x, int y, int regionId)
	{
		if (y > 0)
		{
			if (x > 0)
			{
				if (get_region_id(x - 1, y - 1) == regionId)
					return true;
			}

			if (get_region_id(x, y - 1) == regionId)
				return true;
			
			if (x < GameConstants.MapSize - 1)
			{
				if (get_region_id(x + 1, y - 1) == regionId)
					return true;
			}
		}

		if (x > 0)
		{
			if (get_region_id(x - 1, y) == regionId)
				return true;
		}

		if (x < GameConstants.MapSize - 1)
		{
			if (get_region_id(x + 1, y) == regionId)
				return true;
		}

		if (y < GameConstants.MapSize - 1)
		{
			if (x > 0)
			{
				if (get_region_id(x - 1, y + 1) == regionId)
					return true;
			}

			if (get_region_id(x, y + 1) == regionId)
				return true;
			
			if (x < GameConstants.MapSize - 1)
			{
				if (get_region_id(x + 1, y + 1) == regionId)
					return true;
			}
		}

		return false;
	}

	public bool is_harbor_region(int xLoc, int yLoc, int landRegionId, int seaRegionId)
	{
		if (xLoc + 2 >= GameConstants.MapSize || yLoc + 2 >= GameConstants.MapSize)
			return false;

		for (int y = 0; y < 3; y++)
		{
			for (int x = 0; x < 3; x++)
			{
				int regionId = get_region_id(xLoc + x, yLoc + y);
				if (regionId != landRegionId && regionId != seaRegionId)
				{
					return false;
				}
			}
		}

		return true;
	}

	public void draw_link_line(int srcFirmId, int srcTownRecno, int srcXLoc1, int srcYLoc1, int srcXLoc2, int srcYLoc2,
		int giveEffectiveDis = 0)
	{
		throw new NotImplementedException();
	}

	public void set_all_power()
	{
		//--------- set town's influence -----------//

		foreach (Town town in TownArray)
		{
			if (town.NationId == 0)
				continue;

			//------- set the influence range of this town -----//

			set_power(town.LocX1, town.LocY1, town.LocX2, town.LocY2, town.NationId);
		}

		//--------- set firm's influence -----------//

		foreach (Firm firm in FirmArray)
		{
			if (firm.nation_recno == 0)
				continue;

			if (!firm.should_set_power)
				continue;

			//------- set the influence range of this firm -----//

			set_power(firm.loc_x1, firm.loc_y1, firm.loc_x2, firm.loc_y2, firm.nation_recno);
		}
	}

	public void set_power(int xLoc1, int yLoc1, int xLoc2, int yLoc2, int nationRecno)
	{
		//------- reset power_nation_recno first ------//

		bool plateauResult = get_loc((xLoc1 + xLoc2) / 2, (yLoc1 + yLoc2) / 2).is_plateau();

		xLoc1 = Math.Max(0, xLoc1 - InternalConstants.EFFECTIVE_POWER_DISTANCE + 1);
		yLoc1 = Math.Max(0, yLoc1 - InternalConstants.EFFECTIVE_POWER_DISTANCE + 1);
		xLoc2 = Math.Min(GameConstants.MapSize - 1, xLoc2 + InternalConstants.EFFECTIVE_POWER_DISTANCE - 1);
		yLoc2 = Math.Min(GameConstants.MapSize - 1, yLoc2 + InternalConstants.EFFECTIVE_POWER_DISTANCE - 1);

		int centerY = (yLoc1 + yLoc2) / 2;

		for (int yLoc = yLoc1; yLoc <= yLoc2; yLoc++)
		{
			int t = Math.Abs(yLoc - centerY) / 2;

			for (int xLoc = xLoc1 + t; xLoc <= xLoc2 - t; xLoc++)
			{
				Location location = get_loc(xLoc, yLoc);

				if (location.sailable()) //if(!locPtr.walkable())
					continue;

				if (location.is_power_off())
					continue;

				if (location.is_plateau() != plateauResult)
					continue;

				if (location.power_nation_recno == 0)
				{
					location.power_nation_recno = nationRecno;
					//sys.map_need_redraw = 1;						// request redrawing the map next time
				}
			}
		}
	}

	public void restore_power(int xLoc1, int yLoc1, int xLoc2, int yLoc2, int townRecno, int firmRecno)
	{
		//TODO rewrite bad code
		int nationRecno = 0;

		if (townRecno != 0)
		{
			nationRecno = TownArray[townRecno].NationId;
			//TownArray[townRecno].nation_recno = 0;
		}

		if (firmRecno != 0)
		{
			nationRecno = FirmArray[firmRecno].nation_recno;
			//FirmArray[firmRecno].nation_recno = 0;
		}

		//------- reset power_nation_recno first ------//

		xLoc1 = Math.Max(0, xLoc1 - InternalConstants.EFFECTIVE_POWER_DISTANCE + 1);
		yLoc1 = Math.Max(0, yLoc1 - InternalConstants.EFFECTIVE_POWER_DISTANCE + 1);
		xLoc2 = Math.Min(GameConstants.MapSize - 1, xLoc2 + InternalConstants.EFFECTIVE_POWER_DISTANCE - 1);
		yLoc2 = Math.Min(GameConstants.MapSize - 1, yLoc2 + InternalConstants.EFFECTIVE_POWER_DISTANCE - 1);

		int centerY = (yLoc1 + yLoc2) / 2;

		for (int yLoc = yLoc1; yLoc <= yLoc2; yLoc++)
		{
			int t = Math.Abs(yLoc - centerY) / 2;

			for (int xLoc = xLoc1 + t; xLoc <= xLoc2 - t; xLoc++)
			{
				Location location = get_loc(xLoc, yLoc);

				if (location.power_nation_recno == nationRecno)
				{
					location.power_nation_recno = 0;
					//sys.map_need_redraw = 1;						// request redrawing the map next time
				}
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

	public void set_surr_power_off(int xLoc, int yLoc)
	{
		if (xLoc > 0) // west
			get_loc(xLoc - 1, yLoc).set_power_off();

		if (xLoc < GameConstants.MapSize - 1)
			get_loc(xLoc + 1, yLoc).set_power_off();

		if (yLoc > 0) // north
			get_loc(xLoc, yLoc - 1).set_power_off();

		if (yLoc < GameConstants.MapSize - 1) // south
			get_loc(xLoc, yLoc + 1).set_power_off();
	}

	public void Process()
	{
		//-------- process wall ----------//

		//if( ConfigAdv.wall_building_allowed )
		//form_world_wall();

		//-------- process fire -----------//

		// BUGHERE : set Location::flammability for every change in cargo

		spread_fire(Weather);

		// ------- process visibility --------//
		process_visibility();

		//-------- process lightning ------//
		if (lightning_signal == 0 && Weather.is_lightning())
		{
			// ------- create new lightning ----------//
			lightning_signal = 110;
		}

		if (lightning_signal == 106 && Config.weather_effect != 0)
		{
			lightning_strike(Misc.Random(GameConstants.MapSize), Misc.Random(GameConstants.MapSize), 1);
		}

		if (lightning_signal == 100)
			lightning_signal = 5 + Misc.Random(10);
		else if (lightning_signal > 0)
			lightning_signal--;

		//---------- process ambient sound ---------//

		if (Sys.Instance.FrameNumber % 10 == 0) // process once per ten frames
			process_ambient_sound();

		// --------- update scan fire x y ----------//
		if (++scan_fire_x >= SCAN_FIRE_DIST)
		{
			scan_fire_x = 0;
			if (++scan_fire_y >= SCAN_FIRE_DIST)
				scan_fire_y = 0;
		}
	}

	public void process_visibility()
	{
		//TODO performance
		if (Config.fog_of_war)
		{
			for (int y = 0; y < GameConstants.MapSize; ++y)
			{
				for (int x = 0; x < GameConstants.MapSize; ++x)
				{
					get_loc(x, y).dec_visibility();
				}
			}
		}
	}

	public void next_day()
	{
		plant_ops();

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
			earth_quake();
		}
	}

	//------- functions related to plant's growth, see ow_plant.cpp -----//

	private int rand_inner_x()
	{
		return Renderer.CellTextureWidth / 4 + Misc.Random(Renderer.CellTextureWidth / 2);
	}

	private int rand_inner_y()
	{
		return (Renderer.CellTextureHeight * 3) / 8 + Misc.Random(Renderer.CellTextureHeight / 4);
	}

	public void plant_ops()
	{
		plant_grow(40);
		plant_reprod(10);
		plant_death();
		plant_spread(50);
	}

	public void plant_grow(int pGrow = 4, int scanDensity = 8)
	{
		// scan part of the map for plant
		int yBase = Misc.Random(scanDensity);
		int xBase = Misc.Random(scanDensity);
		for (int y = yBase; y < GameConstants.MapSize; y += scanDensity)
		{
			for (int x = xBase; x < GameConstants.MapSize; x += scanDensity)
			{
				Location location = get_loc(x, y);
				int bitmapId;
				int basePlantId;

				// is a plant and is not at maximum grade
				if (location.is_plant() && Misc.Random(100) < pGrow &&
				    (basePlantId = PlantRes.plant_recno(bitmapId = location.plant_id())) != 0 &&
				    bitmapId - PlantRes[basePlantId].first_bitmap < PlantRes[basePlantId].bitmap_count - 1)
				{
					// increase the grade of plant
					location.grow_plant();
				}
			}
		}
	}

	public void plant_reprod(int pReprod = 1, int scanDensity = 8)
	{
		if (plant_count > plant_limit)
			return;
		if (5 * plant_count < 4 * plant_limit)
			pReprod++; // higher probability to grow

		// determine the rainful, temperature and sunlight
		int t = Weather.temp_c();

		// scan the map for plant
		int yBase = Misc.Random(scanDensity);
		int xBase = Misc.Random(scanDensity);
		for (int y = yBase; y < GameConstants.MapSize; y += scanDensity)
		{
			for (int x = xBase; x < GameConstants.MapSize; x += scanDensity)
			{
				Location location = get_loc(x, y);
				int bitmapId, basePlantId, plantGrade;
				// is a plant and grade > 3
				if (location.is_plant() && (basePlantId = PlantRes.plant_recno(bitmapId = location.plant_id())) != 0 &&
				    ((plantGrade = bitmapId - PlantRes[basePlantId].first_bitmap) >= 3 ||
				     plantGrade == PlantRes[basePlantId].bitmap_count - 1))
				{
					// find the optimal temperature for the plant
					int oTemp = opt_temp[PlantRes[basePlantId].climate_zone - 1];
					int tempEffect = 5 - Math.Abs( oTemp - t);
					tempEffect = tempEffect > 0 ? tempEffect : 0;

					if (Misc.Random(100) < tempEffect * pReprod)
					{
						// produce the same plant but grade 1,
						int trial = 2;
						Location newl;
						while (trial-- != 0)
						{
							newl = null;
							switch (Misc.Random(8))
							{
								case 0: // north square
									if (y > 0)
										newl = get_loc(x, y - 1);
									break;
								case 1: // east square
									if (x < GameConstants.MapSize - 1)
										newl = get_loc(x + 1, y);
									break;
								case 2: // south square
									if (y < GameConstants.MapSize - 1)
										newl = get_loc(x, y + 1);
									break;
								case 3: // west square
									if (x > 0)
										newl = get_loc(x - 1, y);
									break;
								case 4: // north west square
									if (y > 0 && x > 0)
										newl = get_loc(x - 1, y - 1);
									break;
								case 5: // north east square
									if (y > 0 && x < GameConstants.MapSize - 1)
										newl = get_loc(x + 1, y - 1);
									break;
								case 6: // south east square
									if (y < GameConstants.MapSize - 1 && x < GameConstants.MapSize - 1)
										newl = get_loc(x + 1, y + 1);
									break;
								case 7: // south west square
									if (y < GameConstants.MapSize - 1 && x > 0)
										newl = get_loc(x - 1, y + 1);
									break;
							}

							int teraType;
							PlantInfo plantInfo = PlantRes[basePlantId];
							if (newl != null && newl.can_add_plant() &&
							    (plantInfo.tera_type[0] ==
							     (teraType = TerrainRes[newl.terrain_id].average_type) ||
							     plantInfo.tera_type[1] == teraType || plantInfo.tera_type[2] == teraType))
							{
								newl.set_plant(plantInfo.first_bitmap, rand_inner_x(), rand_inner_y());

								// ------- set flammability ---------
								newl.set_fire_src(100);
								plant_count++;
								//### begin alex 24/6 ###//
								//newl.set_power_off();
								//newl.power_nation_recno = 0;
								//set_surr_power_off(x, y);
								//#### end alex 24/6 ####//
								break;
							}
						}
					}
				}
			}
		}
	}

	public void plant_death(int scanDensity = 8)
	{
		int yBase = Misc.Random(scanDensity);
		int xBase = Misc.Random(scanDensity);
		for (int y = yBase; y < GameConstants.MapSize; y += scanDensity)
		{
			for (int x = xBase; x < GameConstants.MapSize; x += scanDensity)
			{
				Location location = get_loc(x, y);
				if (location.is_plant())
				{
					int neighbour = 0;
					int totalSpace = 0;

					// west
					if (x > 0)
					{
						totalSpace++;
						if (get_loc(x - 1, y).is_plant())
							neighbour++;
					}

					// east
					if (x < GameConstants.MapSize - 1)
					{
						totalSpace++;
						if (get_loc(x + 1, y).is_plant())
							neighbour++;
					}

					if (y > 0)
					{
						location = get_loc(x, y - 1);

						// north square
						totalSpace++;
						if (location.is_plant())
							neighbour++;

						// north west
						if (x > 0)
						{
							totalSpace++;
							if (get_loc(x - 1, y).is_plant())
								neighbour++;
						}

						//	north east
						if (x < GameConstants.MapSize - 1)
						{
							totalSpace++;
							if (get_loc(x + 1, y).is_plant())
								neighbour++;
						}
					}

					if (y < GameConstants.MapSize - 1)
					{
						location = get_loc(x, y + 1);

						// south square
						totalSpace++;
						if (location.is_plant())
							neighbour++;

						// south west
						if (x > 0)
						{
							totalSpace++;
							if (get_loc(x - 1, y).is_plant())
								neighbour++;
						}

						// south east
						if (x < GameConstants.MapSize - 1)
						{
							totalSpace++;
							if (get_loc(x + 1, y).is_plant())
								neighbour++;
						}
					}

					// may remove plant if more than two third of the space is occupied
					if (Misc.Random(totalSpace) + 2 * totalSpace / 3 <= neighbour)
					{
						location = get_loc(x, y);
						get_loc(x, y).remove_plant();
						if (location.fire_src() > 50)
							location.set_fire_src(50);
						plant_count--;
						//### begin alex 24/6 ###//
						//newl.set_power_off();
						//newl.power_nation_recno = 0;
						//set_surr_power_off(x, y);
						//#### end alex 24/6 ####//
					}
				}
			}
		}
	}

	public void plant_spread(int pSpread = 5)
	{
		if (plant_count > plant_limit)
			return;
		if (5 * plant_count < 4 * plant_limit)
			pSpread += pSpread;

		if (Misc.Random(1000) >= pSpread)
			return;

		// ------- determine temperature
		int t = Weather.temp_c();

		// ------- randomly select a place to seed plant
		int y = 1 + Misc.Random(GameConstants.MapSize - 2);
		int x = 1 + Misc.Random(GameConstants.MapSize - 2);

		Location l = get_loc(x, y);
		bool build_flag = true;
		int teraType = TerrainRes[l.terrain_id].average_type;

		// ------- all square around are the same terrain type and empty
		for (int y1 = y - 1; y1 <= y + 1; ++y1)
		{
			for (int x1 = x - 1; x1 <= x + 1; ++x1)
			{
				l = get_loc(x1, y1);
				if (!l.can_add_plant() || TerrainRes[l.terrain_id].average_type != teraType)
					build_flag = false;
			}
		}

		if (build_flag)
		{
			int climateZone = 0;
			for (int retry = 0; climateZone == 0 && retry < 5; ++retry)
			{
				for (int j = 0; j < 3; ++j)
				{
					if (Misc.Random(5) > Math.Abs(t - opt_temp[j]))
					{
						climateZone = j + 1;
						int plantBitmap = PlantRes.scan(climateZone, teraType, 0);
						if (plantBitmap != 0)
						{
							l = get_loc(x, y);
							l.set_plant(plantBitmap, rand_inner_x(), rand_inner_y());
							l.set_fire_src(100);
							plant_count++;
							//### begin alex 24/6 ###//
							//l.set_power_off();
							//l.power_nation_recno = 0;
							//set_surr_power_off(x, y);
							//#### end alex 24/6 ####//
						}

						break;
					}
				}
			}
		}
	}

	public void plant_init()
	{
		const int PLANT_ARRAY_SIZE = 8;

		plant_count = 0;
		int trial;
		for (trial = 50; trial > 0; --trial)
		{
			// ------- randomly select a place to seed plant
			int y = 1 + Misc.Random(GameConstants.MapSize - 2);
			int x = 1 + Misc.Random(GameConstants.MapSize - 2);

			Location l = get_loc(x, y);
			bool build_flag = true;
			int teraType = TerrainRes[l.terrain_id].average_type;

			// ------- all square around are the same terrain type and empty
			for (int y1 = y - 1; y1 <= y + 1; ++y1)
			{
				for (int x1 = x - 1; x1 <= x + 1; ++x1)
				{
					l = get_loc(x1, y1);
					if (!l.can_add_plant() || TerrainRes[l.terrain_id].average_type != teraType)
						build_flag = false;
				}
			}

			if (build_flag)
			{
				int plantBitmap = PlantRes.scan(0, teraType, 0);
				int[] plantArray = new int[PLANT_ARRAY_SIZE];
				for (int i = 0; i < PLANT_ARRAY_SIZE; ++i)
				{
					plantArray[i] = PlantRes.plant_recno(PlantRes.scan(0, teraType, 0));
				}

				if (plantArray[0] != 0)
				{
					plant_spray(plantArray, 6 + Misc.Random(4), x, y);
				}
			}
		}

		plant_limit = plant_count * 3 / 2;

		// ------- kill some plant ----------//
		for (trial = 8; trial > 0; --trial)
		{
			plant_death(2);
		}
	}

	public void plant_spray(int[] plantArray, int strength, int x, int y)
	{
		if (strength <= 0)
			return;

		//---------- if the space is empty put a plant on it ----------//
		Location newl = get_loc(x, y);
		int basePlantId = plantArray[Misc.Random(plantArray.Length)];
		PlantInfo plantInfo = PlantRes[basePlantId];
		int plantSize = Misc.Random(plantInfo.bitmap_count);
		if (plantSize > strength)
			plantSize = strength;

		int teraType;
		if (newl != null && newl.can_add_plant() &&
		    (plantInfo.tera_type[0] == (teraType = TerrainRes[newl.terrain_id].average_type) ||
		     plantInfo.tera_type[1] == teraType || plantInfo.tera_type[2] == teraType))
		{
			newl.set_plant(plantInfo.first_bitmap + plantSize, rand_inner_x(), rand_inner_y());
			newl.set_fire_src(100);
			plant_count++;
			//### begin alex 24/6 ###//
			//newl.set_power_off();
			//newl.power_nation_recno = 0;
			//set_surr_power_off(x, y);
			//#### end alex 24/6 ####//
		}
		else if (newl != null && newl.is_plant() &&
		         // 1. same type, large override small
		         // newl.plant_id() >= plant_res[basePlantId].first_bitmap &&
		         // newl.plant_id() < plant_res[basePlantId].first_bitmap + plantSize)
		         // 2. same type, small override large
		         // newl.plant_id() > plant_res[basePlantId].first_bitmap + plantSize &&
		         // newl.plant_id() < plant_res[basePlantId].first_bitmap + plant_res[basePlantId].bitmap_count)
		         // 3. all types, small override large
		         (newl.plant_id() - PlantRes[PlantRes.plant_recno(newl.plant_id())].first_bitmap) > plantSize)
		{
			// same kind of plant, but smaller, override by a smaller one
			newl.remove_plant();
			newl.set_plant(plantInfo.first_bitmap + plantSize, rand_inner_x(), rand_inner_y());
			newl.set_fire_src(100);
			//### begin alex 24/6 ###//
			//newl.set_power_off();
			//newl.power_nation_recno = 0;
			//set_surr_power_off(x, y);
			//#### end alex 24/6 ####//
		}
		else
		{
			plantSize = -1;
		}

		if (plantSize >= 0 && strength != 0)
		{
			int trial = 3;
			while (trial-- != 0)
			{
				switch (Misc.Random(8))
				{
					case 0: // north square
						if (y > 0)
							plant_spray(plantArray, strength - 1, x, y - 1);
						break;
					case 1: // east square
						if (x < GameConstants.MapSize - 1)
							plant_spray(plantArray, strength - 1, x + 1, y);
						break;
					case 2: // south square
						if (y < GameConstants.MapSize - 1)
							plant_spray(plantArray, strength - 1, x, y + 1);
						break;
					case 3: // west square
						if (x > 0)
							plant_spray(plantArray, strength - 1, x - 1, y);
						break;
					case 4: // north west square
						if (y > 0 && x > 0)
							plant_spray(plantArray, strength - 1, x - 1, y - 1);
						break;
					case 5: // north east square
						if (y > 0 && x < GameConstants.MapSize - 1)
							plant_spray(plantArray, strength - 1, x + 1, y - 1);
						break;
					case 6: // south east square
						if (y < GameConstants.MapSize - 1 && x < GameConstants.MapSize - 1)
							plant_spray(plantArray, strength - 1, x + 1, y + 1);
						break;
					case 7: // south west square
						if (y < GameConstants.MapSize - 1 && x > 0)
							plant_spray(plantArray, strength - 1, x - 1, y + 1);
						break;
				}
			}
		}
	}

	//------- functions related to fire's spreading ----//

	public void init_fire()
	{
		for (int y = 0; y < GameConstants.MapSize; y++)
		{
			for (int x = 0; x < GameConstants.MapSize; x++)
			{
				Location location = get_loc(x, y);
				if (location.has_hill())
				{
					location.set_fire_src(-100);
				}
				else if (location.is_wall())
				{
					location.set_fire_src(-50);
				}
				else if (location.is_firm() || location.is_plant() || location.is_town())
				{
					location.set_fire_src(100);
				}
				else
				{
					switch (TerrainRes[location.terrain_id].average_type)
					{
						case TerrainTypeCode.TERRAIN_OCEAN:
							location.set_fire_src(-100);
							break;
						case TerrainTypeCode.TERRAIN_DARK_GRASS:
							location.set_fire_src(100);
							break;
						case TerrainTypeCode.TERRAIN_LIGHT_GRASS:
							location.set_fire_src(50);
							break;
						case TerrainTypeCode.TERRAIN_DARK_DIRT:
							location.set_fire_src(-50);
							break;
					}
				}

				// --------- put off fire on the map ----------//
				location.set_fire_str(-100);
			}
		}
	}

	public void spread_fire(Weather w)
	{
		// -------- normalize wind_speed between -WIND_SPREADFIRE*SPREAD_RATE to +WIND_SPREADFIRE*SPREAD_RATE -------
		int windCos = (int)(w.wind_speed() * Math.Cos(w.wind_direct_rad()) / 100.0 * Config.fire_spread_rate *
		                    Config.wind_spread_fire_rate);
		int windSin = (int)(w.wind_speed() * Math.Sin(w.wind_direct_rad()) / 100.0 * Config.fire_spread_rate *
		                    Config.wind_spread_fire_rate);

		int rainSnowReduction = (w.rain_scale() > 0 || w.snow_scale() > 0)
			? Config.rain_reduce_fire_rate + (w.rain_scale() + w.snow_scale()) / 4
			: 0;

		double flameDamage = (double)Config.fire_damage / InternalConstants.ATTACK_SLOW_DOWN;

		// -------------update fire_level-----------
		for (int y = scan_fire_y; y < GameConstants.MapSize; y += SCAN_FIRE_DIST)
		{
			for (int x = scan_fire_x; x < GameConstants.MapSize; x += SCAN_FIRE_DIST)
			{
				Location location = get_loc(x, y);

				int fireValue = location.fire_str();
				int oldFireValue = fireValue;
				int flammability = location.fire_src();

				// ------- reduce fire_level on raining or snow
				fireValue -= rainSnowReduction;
				if (fireValue < -100)
					fireValue = -100;

				if (fireValue > 0)
				{
					Unit targetUnit;

					// ------- burn wall -------- //
					if (location.is_wall())
					{
						//if (location.attack_wall((int)(4.0 * flameDamage)) != 0)
							//correct_wall(x, y, 2);
					}
					// ------- burn units ---------//
					else if (location.has_unit(UnitConstants.UNIT_LAND))
					{
						targetUnit = UnitArray[location.unit_recno(UnitConstants.UNIT_LAND)];
						targetUnit.hit_points -= 2.0 * flameDamage;
						if (targetUnit.hit_points <= 0.0)
							targetUnit.hit_points = 0.0;
					}
					else if (location.has_unit(UnitConstants.UNIT_SEA))
					{
						targetUnit = UnitArray[location.unit_recno(UnitConstants.UNIT_SEA)];
						targetUnit.hit_points -= 2.0 * flameDamage;
						if (targetUnit.hit_points <= 0.0)
							targetUnit.hit_points = 0.0;
					}
					else if (location.is_firm() && FirmRes[FirmArray[location.firm_recno()].firm_id].buildable)
					{
						Firm targetFirm = FirmArray[location.firm_recno()];
						targetFirm.hit_points -= flameDamage;
						if (targetFirm.hit_points <= 0.0)
						{
							targetFirm.hit_points = 0.0;
							SERes.sound(targetFirm.center_x, targetFirm.center_y, 1,
								'F', targetFirm.firm_id, "DIE");
							FirmArray.DeleteFirm(targetFirm);
						}
					}

					if (Config.fire_spread_rate > 0)
					{
						Location sidePtr;
						// spread of north square
						if (y > 0 && (sidePtr = get_loc(x, y - 1)).fire_src() > 0 && sidePtr.fire_str() <= 0)
						{
							sidePtr.add_fire_str(Math.Max(Config.fire_spread_rate + windCos, 0));
						}

						// spread of south square
						if (y < GameConstants.MapSize - 1 && (sidePtr = get_loc(x, y + 1)).fire_src() > 0 && sidePtr.fire_str() <= 0)
						{
							sidePtr.add_fire_str(Math.Max(Config.fire_spread_rate - windCos, 0));
						}

						// spread of east square
						if (x < GameConstants.MapSize - 1 && (sidePtr = get_loc(x + 1, y)).fire_src() > 0 && sidePtr.fire_str() <= 0)
						{
							sidePtr.add_fire_str(Math.Max(Config.fire_spread_rate + windSin, 0));
						}

						// spread of west square
						if (x > 0 && (sidePtr = get_loc(x - 1, y)).fire_src() > 0 && sidePtr.fire_str() <= 0)
						{
							sidePtr.add_fire_str(Math.Max(Config.fire_spread_rate - windSin, 0));
						}
					}

					if (flammability > 0)
					{
						// increase fire_level on its own
						if (++fireValue > 100)
							fireValue = 100;

						flammability -= Config.fire_fade_rate;
						// if a plant on it then remove the plant, if flammability <= 0
						if (location.is_plant() && flammability <= 0)
						{
							location.remove_plant();
							plant_count--;
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
						if (location.is_plant() && flammability <= 0)
						{
							location.remove_plant();
							plant_count--;
						}
					}
				}
				else
				{
					// fireValue < 0
					// ---------- fire_level drop slightly ----------
					if (fireValue > -100)
						fireValue--;

					// ---------- restore flammability ------------
					if (flammability >= -30 && flammability < 50 && Misc.Random(100) < Config.fire_restore_prob)
						flammability++;
				}

				// ---------- update new fire level -----------
				//-------- when fire is put off
				// so the fire will not light again very soon
				if (fireValue <= 0 && oldFireValue > 0)
				{
					fireValue -= 50;
				}

				location.set_fire_str(fireValue);
				location.set_fire_src(flammability);
			}
		}
	}

	public void setup_fire(int x, int y, int fireStrength = 30)
	{
		Location location = get_loc(x, y);
		if (location.fire_str() < fireStrength)
		{
			location.set_fire_str(fireStrength);
		}
	}

	//-------- function related to rock ----------//
	public void add_rock(int rockRecno, int x1, int y1)
	{
		// ------- get the delay remain count for the first frame -----//
		Rock newRock = new Rock(RockRes, rockRecno, x1, y1);
		int rockArrayRecno = RockArray.Add(newRock);
		RockInfo rockInfo = RockRes.get_rock_info(rockRecno);

		for (int dy = 0; dy < rockInfo.loc_height && y1 + dy < GameConstants.MapSize; ++dy)
		{
			for (int dx = 0; dx < rockInfo.loc_width && x1 + dx < GameConstants.MapSize; ++dx)
			{
				int rockBlockRecno = RockRes.locate_block(rockRecno, dx, dy);
				if (rockBlockRecno != 0)
				{
					Location location = get_loc(x1 + dx, y1 + dy);
					location.set_rock(rockArrayRecno);
					location.set_power_off();
					set_surr_power_off(x1, y1);
				}
			}
		}
	}

	public void add_dirt(int dirtRecno, int x1, int y1)
	{
		// ------- get the delay remain count for the first frame -----//
		Rock newDirt = new Rock(RockRes, dirtRecno, x1, y1);
		int dirtArrayRecno = DirtArray.Add(newDirt);

		RockInfo dirtInfo = RockRes.get_rock_info(dirtRecno);

		for (int dy = 0; dy < dirtInfo.loc_height && y1 + dy < GameConstants.MapSize; ++dy)
		{
			for (int dx = 0; dx < dirtInfo.loc_width && x1 + dx < GameConstants.MapSize; ++dx)
			{
				int dirtBlockRecno = RockRes.locate_block(dirtRecno, dx, dy);
				if (dirtBlockRecno != 0)
				{
					Location location = get_loc(x1 + dx, y1 + dy);
					location.set_dirt(dirtArrayRecno);

					if (dirtInfo.rock_type == RockInfo.DIRT_BLOCKING_TYPE)
						location.walkable_off();
				}
			}
		}
	}

	public bool can_add_rock(int x1, int y1, int x2, int y2)
	{
		for (int y = y1; y <= y2; y++)
		{
			for (int x = x1; x <= x2; x++)
			{
				if (!get_loc(x, y).can_add_rock(3))
					return false;
			}
		}

		return true;
	}

	public bool can_add_dirt(int x1, int y1, int x2, int y2)
	{
		for (int y = y1; y <= y2; y++)
		{
			for (int x = x1; x <= x2; x++)
			{
				if (!get_loc(x, y).can_add_dirt())
					return false;
			}
		}

		return true;
	}

	// ------ function related to weather ---------//
	public void earth_quake()
	{
		for (int y = 0; y < GameConstants.MapSize; ++y)
		{
			for (int x = 0; x < GameConstants.MapSize; ++x)
			{
				Location location = get_loc(x, y);
				if (location.is_wall())
				{
					location.attack_wall(Weather.quake_rate(x, y) / 2);
				}
			}
		}

		int firmDamage = 0;
		int firmDie = 0;

		foreach (Firm firm in FirmArray)
		{
			if (!FirmRes[firm.firm_id].buildable)
				continue;

			int x = firm.center_x;
			int y = firm.center_y;
			firm.hit_points -= Weather.quake_rate(x, y);
			if (firm.own_firm())
				firmDamage++;
			if (firm.hit_points <= 0.0)
			{
				firm.hit_points = 0.0;
				if (firm.own_firm())
					firmDie++;
				SERes.sound(firm.center_x, firm.center_y, 1, 'F', firm.firm_id, "DIE");
				FirmArray.DeleteFirm(firm);
			}
		}

		int townDamage = 0;
		foreach (Town town in TownArray)
		{
			int townRecno = town.TownId;
			bool ownTown = (town.NationId == NationArray.player_recno);
			int beforePopulation = town.Population;
			int causalty = Weather.quake_rate(town.LocCenterX, town.LocCenterY) / 10;
			for (; causalty > 0 && !TownArray.IsDeleted(townRecno); --causalty)
			{
				town.KillTownPeople(0);
			}

			if (TownArray.IsDeleted(townRecno))
				causalty = beforePopulation;
			else
				causalty = beforePopulation - town.Population;

			if (ownTown)
				townDamage += causalty;
		}

		int unitDamage = 0;
		int unitDie = 0;
		foreach (Unit unit in UnitArray)
		{
			// no damage to air unit , sea unit or overseer
			if (unit.mobile_type == UnitConstants.UNIT_AIR || unit.mobile_type == UnitConstants.UNIT_SEA ||
			    !unit.is_visible())
			{
				continue;
			}

			double damage = Weather.quake_rate(unit.cur_x_loc(), unit.cur_y_loc()) * unit.max_hit_points / 200.0;
			if (damage >= unit.hit_points)
				damage = unit.hit_points - 1;
			if (damage < 5.0)
				damage = 5.0;

			unit.hit_points -= damage;
			if (unit.is_own())
				unitDamage++;

			if (unit.hit_points <= 0.0)
			{
				unit.hit_points = 0.0;
				if (unit.is_own())
					unitDie++;
			}
			else
			{
				if (UnitRes[unit.unit_id].solider_id != 0 && Weather.quake_rate(unit.cur_x_loc(), unit.cur_y_loc()) >= 60)
				{
					((UnitVehicle)unit).dismount();
				}
			}
		}

		NewsArray.earthquake_damage(unitDamage - unitDie, unitDie, townDamage, firmDamage - firmDie, firmDie);
	}

	public void lightning_strike(int cx, int cy, int radius = 0)
	{
		for (int y = cy - radius; y <= cy + radius; ++y)
		{
			if (y < 0 || y >= GameConstants.MapSize)
				continue;

			for (int x = cx - radius; x <= cx + radius; ++x)
			{
				if (x < 0 || x >= GameConstants.MapSize)
					continue;

				Location location = get_loc(x, y);
				if (location.is_plant())
				{
					// ---- add a fire on it ------//
					location.set_fire_str(80);
					if (location.can_set_fire() && location.fire_str() < 5)
						location.set_fire_str(5);
				}
			}
		}

		// ------ check hitting units -------//
		foreach (Unit unit in UnitArray)
		{
			// no damage to overseer
			if (!unit.is_visible())
				continue;

			if (unit.cur_x_loc() <= cx + radius &&
			    unit.cur_x_loc() + unit.sprite_info.loc_width > cx - radius &&
			    unit.cur_y_loc() <= cy + radius &&
			    unit.cur_y_loc() + unit.sprite_info.loc_height > cy - radius)
			{
				unit.hit_points -= unit.sprite_info.lightning_damage / InternalConstants.ATTACK_SLOW_DOWN;

				// ---- add news -------//
				if (unit.is_own())
					NewsArray.lightning_damage(unit.cur_x_loc(), unit.cur_y_loc(),
						News.NEWS_LOC_UNIT, unit.sprite_recno, unit.hit_points <= 0.0 ? 1 : 0);

				if (unit.hit_points <= 0.0)
					unit.hit_points = 0.0;
			}
		}

		List<Firm> firmsToDelete = new List<Firm>();
		foreach (Firm firm in FirmArray)
		{
			if (!FirmRes[firm.firm_id].buildable)
				continue;

			if (firm.loc_x1 <= cx + radius && firm.loc_x2 >= cx - radius && firm.loc_y1 <= cy + radius &&
			    firm.loc_y2 >= cy - radius)
			{
				firm.hit_points -= 50.0 / InternalConstants.ATTACK_SLOW_DOWN;

				// ---- add news -------//
				if (firm.own_firm())
					NewsArray.lightning_damage(firm.center_x, firm.center_y,
						News.NEWS_LOC_FIRM, firm.firm_recno, firm.hit_points <= 0.0 ? 1 : 0);

				// ---- add a fire on it ------//
				Location location = get_loc(firm.center_x, firm.center_y);
				if (location.can_set_fire() && location.fire_str() < 5)
					location.set_fire_str(5);

				if (firm.hit_points <= 0.0)
				{
					firm.hit_points = 0.0;
					SERes.sound(firm.center_x, firm.center_y, 1, 'F', firm.firm_id, "DIE");
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
			if (town.LocX1 <= cx + radius && town.LocX2 >= cx - radius && town.LocY1 <= cy + radius &&
			    town.LocY2 >= cy - radius)
			{
				// ---- add news -------//
				if (town.NationId == NationArray.player_recno)
					NewsArray.lightning_damage(town.LocCenterX, town.LocCenterY,
						News.NEWS_LOC_TOWN, town.TownId, 0);

				// ---- add a fire on it ------//
				Location location = get_loc(town.LocCenterX, town.LocCenterY);
				if (location.can_set_fire() && location.fire_str() < 5)
					location.set_fire_str(5);

				town.KillTownPeople(0);
			}
		}
	}

	private void process_ambient_sound()
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
			int xLoc = Misc.Random(GameConstants.MapSize);
			int yLoc = Misc.Random(GameConstants.MapSize);
			PosVolume p = new PosVolume(xLoc, yLoc);
			RelVolume relVolume = new RelVolume(p, 200, GameConstants.MapSize);
			if (relVolume.rel_vol < 80)
				relVolume.rel_vol = 80;

			SECtrl.request(sndFile, relVolume);
		}
	}

	public void SetLocFlags()
	{
		int totalLoc = GameConstants.MapSize * GameConstants.MapSize;

		//----- set power_off of the map edges -----//

		for (int xLoc = 0; xLoc < GameConstants.MapSize; xLoc++) // set the top and bottom edges
		{
			get_loc(xLoc, 0).set_power_off();
			get_loc(xLoc, GameConstants.MapSize - 1).set_power_off();
		}

		for (int yLoc = 0; yLoc < GameConstants.MapSize; yLoc++) // set the left and right edges
		{
			get_loc(0, yLoc).set_power_off();
			get_loc(GameConstants.MapSize - 1, yLoc).set_power_off();
		}

		//-----------------------------------------//

		if (Config.explore_whole_map)
		{
			for (int i = 0; i < loc_matrix.Length; i++)
			{
				Location location = loc_matrix[i];
				//------- set explored flag ----------//
				location.explored_on();
				TerrainInfo terrainInfo = TerrainRes[location.terrain_id];
				if (terrainInfo.is_coast())
				{
					location.loc_flag |= Location.LOCATE_COAST;
					if (terrainInfo.average_type != TerrainTypeCode.TERRAIN_OCEAN)
						location.set_power_off();
					else
						set_surr_power_off(i % GameConstants.MapSize, i / GameConstants.MapSize);
				}

				location.walkable_reset();
			}
		}
		else
		{
			for (int i = 0; i < loc_matrix.Length; i++)
			{
				Location location = loc_matrix[i];
				//------- clear explored flag ----------//
				location.explored_off();
				TerrainInfo terrainInfo = TerrainRes[location.terrain_id];
				if (terrainInfo.is_coast())
				{
					location.loc_flag |= Location.LOCATE_COAST;
					if (terrainInfo.average_type != TerrainTypeCode.TERRAIN_OCEAN)
						location.set_power_off();
					else
						set_surr_power_off(i % GameConstants.MapSize, i / GameConstants.MapSize);
				}

				location.walkable_reset();
			}
		}
	}

	public void ClearLocation()
	{
		for (int i = 0; i < loc_matrix.Length; i++)
		{
			loc_matrix[i] = new Location(TerrainRes, HillRes);
		}
	}
}
