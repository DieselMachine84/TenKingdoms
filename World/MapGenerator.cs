using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class MapGenerator
{
	private TerrainRes TerrainRes => Sys.Instance.TerrainRes;
	private HillRes HillRes => Sys.Instance.HillRes;
	private RockRes RockRes => Sys.Instance.RockRes;
	private RaceRes RaceRes => Sys.Instance.RaceRes;
	private SpriteRes SpriteRes => Sys.Instance.SpriteRes;
	private UnitRes UnitRes => Sys.Instance.UnitRes;
	private MonsterRes MonsterRes => Sys.Instance.MonsterRes;

	private Config Config => Sys.Instance.Config;
	private ConfigAdv ConfigAdv => Sys.Instance.ConfigAdv;
	private World World => Sys.Instance.World;

	private RegionArray RegionArray => Sys.Instance.RegionArray;
	private SiteArray SiteArray => Sys.Instance.SiteArray;
	private NationArray NationArray => Sys.Instance.NationArray;
	private TownArray TownArray => Sys.Instance.TownArray;
	private FirmArray FirmArray => Sys.Instance.FirmArray;
	private UnitArray UnitArray => Sys.Instance.UnitArray;
	private SpyArray SpyArray => Sys.Instance.SpyArray;
	
	public MapGenerator()
	{
	}
	
	public void Generate()
	{
		//----------- start generating -----------//

		// ---------- generate plasma map ----------//

		Plasma heightMap = new Plasma(GameConstants.MapSize, GameConstants.MapSize);
		heightMap.generate(Misc.Random(2), 5, Misc.Random());

		// ###### begin Gilbert 27/8 ########//
		// ---------- add base level --------//
		// heightMap.add_base_level(heightMap.calc_tera_base_level(TerrainRes::min_height(TERRAIN_DARK_GRASS)));

		// grouping plasma sample data, find sea or land first
		int totalLoc = (GameConstants.MapSize + 1) * (GameConstants.MapSize + 1);
		int[] heightLimit = new int[2];
		int[] heightFreq = new int[2];
		int minLandCount = 0;
		int maxLandCount = 0;
		int initHeightLimit = TerrainRes.min_height(TerrainTypeCode.TERRAIN_DARK_GRASS);
		switch (Config.land_mass)
		{
			case Config.OPTION_LOW:
				minLandCount = totalLoc * 4 / 10;
				maxLandCount = totalLoc * 6 / 10;
				break;
			case Config.OPTION_MODERATE:
				minLandCount = totalLoc * 6 / 10;
				maxLandCount = totalLoc * 8 / 10;
				break;
			case Config.OPTION_HIGH:
				minLandCount = totalLoc * 8 / 10;
				maxLandCount = totalLoc;
				break;
			default:
				throw new Exception();
		}

		int avgLandCount = (minLandCount + maxLandCount) / 2;

		heightLimit[0] = 0;
		heightLimit[1] = initHeightLimit;
		heightMap.stat(2, heightLimit, heightFreq);

		int landCount = heightFreq[1];
		int seaCount = heightFreq[0];

		int loopCount = 0;
		while (++loopCount <= 4 && (landCount < minLandCount || landCount > maxLandCount))
		{
			if (landCount < minLandCount)
			{
				// positive add_base_level to gain more land
				// find a level between 0 to TerrainRes::min_height(TERRAIN_DARK_GRASS)
				// assume heightlevel below heightLimit[1] is evenly distributed,
				// approximate a new heightLimit[1] such that landCount is avgLandCount

				// (heightLimit[1] - newheightLimit[1]) * seaCount / (heightLimit[1] - heightLimit[0]) + landCount = avgLandCount

				heightLimit[1] -= (avgLandCount - landCount) * (heightLimit[1] - heightLimit[0]) / seaCount;
			}
			else if (landCount > maxLandCount)
			{
				// negative add_base_level to reduce land
				// find a level above TerrainRes::min_height(TERRAIN_DARK_GRASS)
				// assume heightlevel above heightLimit[1] is evenly distributed,
				// approximate a new heightLimit[1] such that landCount is avgLandCount

				const int maxHeightLimit = 255;
				// landCount * (maxHeightLimit - newheightLimit[1])/ (maxHeightLimit - heightLimit[1]) = avgLandCount
				heightLimit[1] = maxHeightLimit - avgLandCount * (maxHeightLimit - heightLimit[1]) / landCount;
			}

			heightMap.stat(2, heightLimit, heightFreq);
		}

		if (Math.Abs(heightLimit[1] - initHeightLimit) > 2)
		{
			heightMap.add_base_level(initHeightLimit - heightLimit[1]);
		}

		// --------- remove odd terrain --------//

		for (int y = 0; y <= heightMap.max_y; ++y)
		{
			for (int x = 0; x <= heightMap.max_x; ++x)
			{
				RemoveOdd(heightMap, x, y, 5);
			}
		}

		// ------------ shuffle sub-terrain level ---------//

		heightMap.shuffle_level(TerrainRes.min_height(TerrainTypeCode.TERRAIN_OCEAN),
			TerrainRes.max_height(TerrainTypeCode.TERRAIN_OCEAN), -3);
		heightMap.shuffle_level(TerrainRes.min_height(TerrainTypeCode.TERRAIN_DARK_GRASS),
			TerrainRes.max_height(TerrainTypeCode.TERRAIN_DARK_GRASS), 3);
		heightMap.shuffle_level(TerrainRes.min_height(TerrainTypeCode.TERRAIN_LIGHT_GRASS),
			TerrainRes.max_height(TerrainTypeCode.TERRAIN_LIGHT_GRASS), 3);
		heightMap.shuffle_level(TerrainRes.min_height(TerrainTypeCode.TERRAIN_DARK_DIRT),
			TerrainRes.max_height(TerrainTypeCode.TERRAIN_DARK_DIRT), 3);

		SetTeraId(heightMap);

		SubstitutePattern();

		World.SetLocFlags();

		GenerateHills(TerrainTypeCode.TERRAIN_DARK_DIRT);

		SetRegionId();

		GenDirt(40, 30, 60);

		GenRocks(5, 10, 30);

		SetHarborBit();

		World.plant_init();

		World.init_fire();

		CreateAINation(Config.ai_nation_count);
		
		CreatePregameObjects();
	}

	private void RemoveOdd(Plasma plasma, int x, int y, int recur)
	{
		if (recur < 0)
			return;

		// -------- compare the TerrainTypeCode of four adjacent square ------//
		int subPtr = 0;
		int center = TerrainRes.terrain_height(plasma.get_pix(x, y), out subPtr);
		int same = 0;
		int diff = 0;
		int diffTerrain = -1;
		int diffHeight = 0;
		int sameX = 0;
		int sameY = 0;

		// ------- compare north square -------//
		if (y > 0)
		{
			if (center == TerrainRes.terrain_height(plasma.get_pix(x, y - 1), out subPtr))
			{
				same++;
				sameX = x;
				sameY = y - 1;
			}
			else
			{
				diff++;
				if (diffTerrain < 0)
				{
					// new diffHeight
					diffHeight = plasma.get_pix(x, y - 1);
					diffTerrain = TerrainRes.terrain_height(diffHeight, out subPtr);

				}
				else
				{
					// three terrain types are close, don't change anything
					if (diffTerrain != TerrainRes.terrain_height(plasma.get_pix(x, y - 1), out subPtr))
						return;
				}
			}
		}

		// ------- compare south square -------//
		if (y < plasma.max_y)
		{
			if (center == TerrainRes.terrain_height(plasma.get_pix(x, y + 1), out subPtr))
			{
				same++;
				sameX = x;
				sameY = y + 1;
			}
			else
			{
				diff++;
				if (diffTerrain < 0)
				{
					// new diffHeight
					diffHeight = plasma.get_pix(x, y + 1);
					diffTerrain = TerrainRes.terrain_height(diffHeight, out subPtr);
				}
				else
				{
					// three terrain types are close, don't change anything
					if (diffTerrain != TerrainRes.terrain_height(plasma.get_pix(x, y + 1), out subPtr))
						return;
				}
			}
		}

		// ------- compare west square -------//
		if (x > 0)
		{
			if (center == TerrainRes.terrain_height(plasma.get_pix(x - 1, y), out subPtr))
			{
				same++;
				sameX = x - 1;
				sameY = y;
			}
			else
			{
				diff++;
				if (diffTerrain < 0)
				{
					// new diffHeight
					diffHeight = plasma.get_pix(x - 1, y);
					diffTerrain = TerrainRes.terrain_height(diffHeight, out subPtr);
				}
				else
				{
					// three terrain types are close, don't change anything
					if (diffTerrain != TerrainRes.terrain_height(plasma.get_pix(x - 1, y), out subPtr))
						return;
				}
			}
		}

		// ------- compare east square -------//
		if (x < plasma.max_x)
		{
			if (center == TerrainRes.terrain_height(plasma.get_pix(x + 1, y), out subPtr))
			{
				same++;
				sameX = x + 1;
				sameY = y;
			}
			else
			{
				diff++;
				if (diffTerrain < 0)
				{
					// new diffHeight
					diffHeight = plasma.get_pix(x + 1, y);
					diffTerrain = TerrainRes.terrain_height(diffHeight, out subPtr);
				}
				else
				{
					// three terrain types are close, don't change anything
					if (diffTerrain != TerrainRes.terrain_height(plasma.get_pix(x + 1, y), out subPtr))
						return;
				}
			}
		}

		if (same <= 1 && diff >= 2)
		{
			// flatten
			plasma.plot(x, y, diffHeight);

			// propagate to next square
			if (same == 1)
			{
				RemoveOdd(plasma, sameX, sameY, recur - 1);
			}
		}
	}

	private void SetTeraId(Plasma plasma)
	{
		for (int y = 0; y < GameConstants.MapSize; ++y)
		{
			for (int x = 0; x < GameConstants.MapSize; ++x)
			{
				int nwType, neType, swType, seType;
				int nwSubType, neSubType, swSubType, seSubType;
				nwType = TerrainRes.terrain_height(plasma.get_pix(x, y), out nwSubType);
				neType = TerrainRes.terrain_height(plasma.get_pix(x + 1, y), out neSubType);
				swType = TerrainRes.terrain_height(plasma.get_pix(x, y + 1), out swSubType);
				seType = TerrainRes.terrain_height(plasma.get_pix(x + 1, y + 1), out seSubType);

				if ((World.get_loc(x, y).terrain_id = TerrainRes.scan(nwType, nwSubType,
					    neType, neSubType, swType, swSubType, seType, seSubType, 0, 1, 0)) == 0)
				{
					//err.run("Error World::set_tera_id, Cannot find terrain type %d:%d, %d:%d, %d:%d, %d:%d",
					//nwType, nwSubType, neType, neSubType, swType, swSubType, seType, seSubType);
				}
			}
		}
	}

	private void SubstitutePattern()
	{
		const int resultArraySize = 20;
		TerrainSubInfo[] candSub = new TerrainSubInfo[resultArraySize];

		for (int y = 0; y < GameConstants.MapSize; ++y)
		{
			for (int x = 0; x < GameConstants.MapSize; ++x)
			{
				int terrainId = World.get_loc(x, y).terrain_id;
				int SubFound = TerrainRes.search_pattern(TerrainRes[terrainId].pattern_id,
					candSub, resultArraySize);
				for (int i = 0; i < SubFound; ++i)
				{
					int tx = x, ty = y;
					bool flag = true;
					TerrainSubInfo terrainSubInfo = candSub[i];

					// ----- test if a substitution matches
					for (terrainSubInfo = candSub[i]; terrainSubInfo != null; terrainSubInfo = terrainSubInfo.next_step)
					{
						if (tx < 0 || tx >= GameConstants.MapSize || ty < 0 || ty >= GameConstants.MapSize ||
						    TerrainRes[World.get_loc(tx, ty).terrain_id].pattern_id !=
						    terrainSubInfo.old_pattern_id)
						{
							flag = false;
							break;
						}

						// ----- update tx, ty according to post_move -----//
						switch (terrainSubInfo.post_move)
						{
							case 1:
								ty--;
								break; // North
							case 2:
								ty--;
								tx++;
								break; // NE
							case 3:
								tx++;
								break; // East
							case 4:
								tx++;
								ty++;
								break; // SE
							case 5:
								ty++;
								break; // South
							case 6:
								ty++;
								tx--;
								break; // SW
							case 7:
								tx--;
								break; // West
							case 8:
								tx--;
								ty--;
								break; // NW
						}
					}

					// ------ replace pattern -------//
					if (flag)
					{
						tx = x;
						ty = y;
						for (terrainSubInfo = candSub[i];
						     terrainSubInfo != null;
						     terrainSubInfo = terrainSubInfo.next_step)
						{
							TerrainInfo oldTerrain = TerrainRes[World.get_loc(tx, ty).terrain_id];
							int terrain_id = TerrainRes.scan(oldTerrain.average_type,
								oldTerrain.secondary_type + terrainSubInfo.sec_adj,
								terrainSubInfo.new_pattern_id, 0, 1, 0);
							World.get_loc(tx, ty).terrain_id = terrain_id;
							if (terrain_id == 0)
							{
								//err_here();		// cannot find terrain_id
							}

							// ----- update tx, ty according to post_move -----//
							switch (terrainSubInfo.post_move)
							{
								case 1:
									ty--;
									break; // North
								case 2:
									ty--;
									tx++;
									break; // NE
								case 3:
									tx++;
									break; // East
								case 4:
									tx++;
									ty++;
									break; // SE
								case 5:
									ty++;
									break; // South
								case 6:
									ty++;
									tx--;
									break; // SW
								case 7:
									tx--;
									break; // West
								case 8:
									tx--;
									ty--;
									break; // NW
							}
						}

						break;
					}
				}
			}
		}
	}

	private void GenerateHills(int terrainType)
	{
		// ------- scan each tile for an above-hill terrain tile -----//
		int x, y = 0;
		int priTerrain, secTerrain, lowTerrain, highTerrain;
		int patternId;
		Location aboveLoc;
		TerrainInfo terrainInfo;

		for (y = 0; y < GameConstants.MapSize; ++y)
		{
			for (x = 0; x < GameConstants.MapSize; ++x)
			{
				Location location = World.get_loc(x, y);
				aboveLoc = y > 0 ? World.get_loc(x, y - 1) : null;
				terrainInfo = TerrainRes[location.terrain_id];
				priTerrain = terrainInfo.average_type;
				secTerrain = terrainInfo.secondary_type;
				highTerrain = (priTerrain >= secTerrain ? priTerrain : secTerrain);
				lowTerrain = (priTerrain >= secTerrain ? secTerrain : priTerrain);
				if (highTerrain >= terrainType)
				{
					// BUGHERE : ignore special or extra flag
					patternId = terrainInfo.pattern_id;
					if (lowTerrain >= terrainType)
					{
						// move this terrain one square north
						if (y > 0)
						{
							aboveLoc = location;

							// if y is max_y_loc-1, aboveLoc and locPtr looks the same
							// BUGHERE : repeat the same pattern below is a bug if patternId is not 0,9,10,13,14
							if (y == GameConstants.MapSize - 1)
								location.terrain_id = TerrainRes.scan(priTerrain, secTerrain, patternId);
						}
					}
					else
					{
						int hillId = HillRes.scan(patternId, HillRes.LOW_HILL_PRIORITY, 0, false);
						//err_when( !hillId );
						location.set_hill(hillId);
						location.set_fire_src(-100);
						//### begin alex 24/6 ###//
						location.set_power_off();
						World.set_surr_power_off(x, y);
						//#### end alex 24/6 ####//
						if (y > 0)
						{
							aboveLoc.set_hill(HillRes.locate(patternId,
								HillRes[hillId].sub_pattern_id, HillRes.HIGH_HILL_PRIORITY, 0));
							aboveLoc.set_fire_src(-100);
							//### begin alex 24/6 ###//
							aboveLoc.set_power_off();
							World.set_surr_power_off(x, y - 1);
							//#### end alex 24/6 ####//
						}

						// set terrain type to pure teraType-1
						location.terrain_id = TerrainRes.scan(lowTerrain, lowTerrain, 0);
					}
				}
			}
		}


		// ------ checking exit -------//
		// if an exit is set, no exit is scanned in next 7 squares
		const int MIN_EXIT_SEPARATION = 7;
		int lastExit;

		// ------ scan for south exit, width 1 --------//

		const int SOUTH_PATTERN1 = 11;
		const int SOUTH_PATTERN2 = 15;
		const int SOUTH_PATTERN3 = 19;
		const int SOUTH_PATTERN4 = 23;

		bool IsSouthExitPattern(int h)
		{
			return h == SOUTH_PATTERN1 || h == SOUTH_PATTERN2 || h == SOUTH_PATTERN3 || h == SOUTH_PATTERN4;
		}

		const char SOUTH_LEFT_SPECIAL = 'B';
		const char SOUTH_RIGHT_SPECIAL = 'C';
		const char SOUTH_CENTRE_SPECIAL = 'A';

		for (y = 1; y < GameConstants.MapSize - 1; ++y)
		{
			lastExit = 0;
			for (x = 0; x < GameConstants.MapSize - 2; ++x, lastExit = lastExit > 0 ? lastExit - 1 : 0)
			{
				Location location = World.get_loc(x, y);
				// three hill blocks on a row are pattern 11,15,19 or 23,
				// block above the second block is a walkable
				if (lastExit == 0)
				{
					if (World.get_loc(x, y).has_hill() && World.get_loc(x + 1, y).has_hill() &&
					    World.get_loc(x + 2, y).has_hill())
					{
						HillBlockInfo h1 = HillRes[World.get_loc(x, y).hill_id1()];
						int h1p = h1.pattern_id;
						HillBlockInfo h2 = HillRes[World.get_loc(x + 1, y).hill_id1()];
						int h2p = h2.pattern_id;
						HillBlockInfo h3 = HillRes[World.get_loc(x + 2, y).hill_id1()];
						int h3p = h3.pattern_id;
						if (h1.special_flag == 0 &&
						    h1.priority == HillRes.HIGH_HILL_PRIORITY && h1p != 0 && IsSouthExitPattern(h1p) &&
						    h2.priority == HillRes.HIGH_HILL_PRIORITY && h2p != 0 && IsSouthExitPattern(h2p) &&
						    h3.priority == HillRes.HIGH_HILL_PRIORITY && h3p != 0 && IsSouthExitPattern(h3p))
						{
							if (World.get_loc(x + 1, y - 1).walkable())
							{
								int hillId, terrainId;

								// change this square
								if (h1p == SOUTH_PATTERN3)
									h1p = SOUTH_PATTERN1;
								else if (h1p == SOUTH_PATTERN4)
									h1p = SOUTH_PATTERN2;
								hillId = HillRes.scan(h1p, HillRes.HIGH_HILL_PRIORITY, SOUTH_LEFT_SPECIAL, false);
								location.remove_hill();
								location.set_hill(hillId);
								location.set_power_off();
								World.set_surr_power_off(x, y);
								if ((terrainId = TerrainRes.scan(terrainType - 1, terrainType - 1, 0, 0, 1, 0)) != 0)
									location.terrain_id = terrainId;

								// next row
								Location loc2 = World.get_loc(x, y + 1);
								hillId = HillRes.locate(h1p,
									HillRes[hillId].sub_pattern_id,
									HillRes.LOW_HILL_PRIORITY, SOUTH_LEFT_SPECIAL);
								if (loc2.hill_id2() == 0)
								{
									// if the location has only one block, remove it
									// if the location has two block, the bottom one is replaced
									loc2.remove_hill();
								}

								loc2.set_hill(hillId);
								loc2.set_power_off();
								World.set_surr_power_off(x, y + 1);
								if ((terrainId = TerrainRes.scan(terrainType - 1, terrainType - 1,
									    0, 0, 1, 0)) != 0)
									loc2.terrain_id = terrainId;

								// second square
								loc2 = World.get_loc(x + 1, y);
								loc2.remove_hill();
								loc2.walkable_reset();
								//if((terrainId = terrain_res.scan(terrainType, terrainType,
								//	0, 0, 1, 0)) != 0 )
								if ((terrainId = TerrainRes.scan(terrainType, (int)SubTerrainMask.BOTTOM_MASK,
									    terrainType,
									    (int)SubTerrainMask.BOTTOM_MASK, terrainType, (int)SubTerrainMask.BOTTOM_MASK,
									    terrainType, (int)SubTerrainMask.BOTTOM_MASK)) != 0)
									loc2.terrain_id = terrainId;

								// next row
								loc2 = World.get_loc(x + 1, y + 1);
								loc2.remove_hill();
								loc2.walkable_reset();
								if ((terrainId = TerrainRes.scan(terrainType, terrainType - 1,
									    SOUTH_PATTERN2, 0, 1, SOUTH_CENTRE_SPECIAL)) != 0)
									loc2.terrain_id = terrainId;

								// prev row
								// loc2 = get_loc(x+1, y-1);
								// if((terrainId = terrain_res.scan(terrainType, terrainType-1,
								// 	SOUTH_PATTERN2, 0, 1, SOUTH_CENTRE_SPECIAL )) != 0 )
								//	loc2->terrain_id = terrainId;

								// third square
								loc2 = World.get_loc(x + 2, y);
								if (h3p == SOUTH_PATTERN4)
									h3p = SOUTH_PATTERN1;
								if (h3p == SOUTH_PATTERN3)
									h3p = SOUTH_PATTERN2;
								hillId = HillRes.scan(h3p, HillRes.HIGH_HILL_PRIORITY, SOUTH_RIGHT_SPECIAL,
									false);
								loc2.remove_hill();
								loc2.set_hill(hillId);
								loc2.set_power_off();
								World.set_surr_power_off(x + 2, y);
								if ((terrainId = TerrainRes.scan(terrainType - 1, terrainType - 1,
									    0, 0, 1, 0)) != 0)
									loc2.terrain_id = terrainId;

								// next row
								loc2 = World.get_loc(x + 2, y + 1);
								hillId = HillRes.locate(h3p,
									HillRes[hillId].sub_pattern_id,
									HillRes.LOW_HILL_PRIORITY, SOUTH_RIGHT_SPECIAL);
								if (loc2.hill_id2() == 0)
								{
									// if the location has only one block, remove it
									// if the location has two block, the bottom one is replaced
									loc2.remove_hill();
								}

								loc2.set_hill(hillId);
								loc2.set_power_off();
								World.set_surr_power_off(x + 2, y + 1);
								if ((terrainId = TerrainRes.scan(terrainType - 1, terrainType - 1, 0, 0, 1, 0)) != 0)
									loc2.terrain_id = terrainId;

								lastExit = MIN_EXIT_SEPARATION;
							}
						}
					}
				}
			}
		}


		// ------ scan for north exit, width 1 --------//

		const int NORTH_PATTERN1 = 12;
		const int NORTH_PATTERN2 = 16;
		const int NORTH_PATTERN3 = 20;
		const int NORTH_PATTERN4 = 24;

		bool IsNorthExitPattern(int h)
		{
			return h == NORTH_PATTERN1 || h == NORTH_PATTERN2 || h == NORTH_PATTERN3 || h == NORTH_PATTERN4;
		}

		const char NORTH_LEFT_SPECIAL = 'D';
		const char NORTH_RIGHT_SPECIAL = 'E';
		const char NORTH_CENTRE_SPECIAL = 'F';

		for (y = 1; y < GameConstants.MapSize - 2; ++y)
		{
			lastExit = 0;
			for (x = GameConstants.MapSize - 3; x >= 0; --x, lastExit = lastExit > 0 ? lastExit - 1 : 0)
			{
				Location location = World.get_loc(x, y);
				// three hill blocks on a row are pattern 12,16,20 or 24,
				// block below the second block is a walkable
				if (lastExit == 0)
				{
					if (World.get_loc(x, y).has_hill() && World.get_loc(x + 1, y).has_hill() &&
					    World.get_loc(x + 2, y).has_hill())
					{
						HillBlockInfo h1 = HillRes[World.get_loc(x, y).hill_id1()];
						int h1p = h1.pattern_id;
						HillBlockInfo h2 = HillRes[World.get_loc(x + 1, y).hill_id1()];
						int h2p = h2.pattern_id;
						HillBlockInfo h3 = HillRes[World.get_loc(x + 2, y).hill_id1()];
						int h3p = h3.pattern_id;
						if (h1.special_flag == 0 &&
						    h1.priority == HillRes.HIGH_HILL_PRIORITY && h1p != 0 && IsNorthExitPattern(h1p) &&
						    h2.priority == HillRes.HIGH_HILL_PRIORITY && h2p != 0 && IsNorthExitPattern(h2p) &&
						    h3.priority == HillRes.HIGH_HILL_PRIORITY && h3p != 0 && IsNorthExitPattern(h3p))
						{
							if (World.get_loc(x + 1, y + 1).walkable())
							{
								int hillId, terrainId;

								// change this square
								if (h1p == NORTH_PATTERN4)
									h1p = NORTH_PATTERN1;
								else if (h1p == NORTH_PATTERN3)
									h1p = NORTH_PATTERN2;
								hillId = HillRes.scan(h1p, HillRes.HIGH_HILL_PRIORITY, NORTH_LEFT_SPECIAL,
									false);
								location.remove_hill();
								location.set_hill(hillId);
								location.set_power_off();
								World.set_surr_power_off(x, y);
								if ((terrainId = TerrainRes.scan(terrainType - 1, terrainType - 1, 0, 0, 1, 0)) != 0)
									location.terrain_id = terrainId;

								// second square
								Location loc2 = World.get_loc(x + 1, y);
								loc2.remove_hill();
								loc2.walkable_reset();
								//if((terrainId = terrain_res.scan(terrainType-1, terrainType-1,
								//	0, 0, 1, NORTH_CENTRE_SPECIAL)) != 0 )
								//	loc2->terrain_id = terrainId;
								if ((terrainId = TerrainRes.scan(terrainType, terrainType - 1, NORTH_PATTERN2, 0, 1,
									    NORTH_CENTRE_SPECIAL)) != 0)
									loc2.terrain_id = terrainId;

								// next row
								//loc2 = get_loc(x+1, y+1);
								//if((terrainId = terrain_res.scan(terrainType, terrainType-1,
								//	NORTH_PATTERN2, 0, 1, NORTH_CENTRE_SPECIAL )) != 0 )
								//	loc2->terrain_id = terrainId;

								// third square
								loc2 = World.get_loc(x + 2, y);
								if (h3p == NORTH_PATTERN3)
									h3p = NORTH_PATTERN1;
								if (h3p == NORTH_PATTERN4)
									h3p = NORTH_PATTERN2;
								hillId = HillRes.scan(h3p, HillRes.HIGH_HILL_PRIORITY, NORTH_RIGHT_SPECIAL, false);
								loc2.remove_hill();
								loc2.set_hill(hillId);
								loc2.set_power_off();
								World.set_surr_power_off(x + 2, y);
								if ((terrainId = TerrainRes.scan(terrainType - 1, terrainType - 1, 0, 0, 1, 0)) != 0)
									loc2.terrain_id = terrainId;

								lastExit = MIN_EXIT_SEPARATION;
							}
						}
					}
				}
			}
		}


		// ------ scan for west exit, width 1 --------//

		const int WEST_PATTERN1 = 9;
		const int WEST_PATTERN2 = 13;
		const int WEST_PATTERN3 = 17;
		const int WEST_PATTERN4 = 21;

		bool IsWestExitPattern(int h)
		{
			return h == WEST_PATTERN1 || h == WEST_PATTERN2 || h == WEST_PATTERN3 || h == WEST_PATTERN4;
		}

		const char WEST_TOP_SPECIAL = 'G';
		const char WEST_BOTTOM_SPECIAL = 'I';
		const char WEST_CENTRE_SPECIAL = 'H';

		for (x = 1; x < GameConstants.MapSize - 1; ++x)
		{
			lastExit = 0;
			for (y = 0; y < GameConstants.MapSize - 4; ++y, lastExit = lastExit > 0 ? lastExit - 1 : 0)
			{
				Location location = World.get_loc(x, y);
				// three hill blocks on a row are pattern 9, 13, 17, 21
				// block above the second block is a walkable
				if (lastExit == 0)
				{
					if (World.get_loc(x, y).has_hill() && World.get_loc(x, y + 1).has_hill() &&
					    World.get_loc(x, y + 2).has_hill() && World.get_loc(x, y + 3).has_hill())
					{
						HillBlockInfo h1 = HillRes[World.get_loc(x, y).hill_id1()];
						int h1p = h1.pattern_id;
						HillBlockInfo h2 = HillRes[World.get_loc(x, y + 2).hill_id1()];
						int h2p = h2.pattern_id;
						HillBlockInfo h3 = HillRes[World.get_loc(x, y + 3).hill_id1()];
						int h3p = h3.pattern_id;
						if (h1.special_flag == 0 &&
						    h1.priority == HillRes.HIGH_HILL_PRIORITY && h1p != 0 && IsWestExitPattern(h1p) &&
						    h2.priority == HillRes.HIGH_HILL_PRIORITY && h2p != 0 && IsWestExitPattern(h2p) &&
						    h3.priority == HillRes.HIGH_HILL_PRIORITY && h3p != 0 && IsWestExitPattern(h3p))
						{
							if ((h3p == WEST_PATTERN1 || h3p == WEST_PATTERN4) &&
							    World.get_loc(x + 1, y + 2).walkable())
							{
								int hillId, terrainId, hill2;

								// change this square
								if (h1p == WEST_PATTERN3)
									h1p = WEST_PATTERN1;
								else if (h1p == WEST_PATTERN4)
									h1p = WEST_PATTERN2;
								hillId = HillRes.scan(h1p, HillRes.HIGH_HILL_PRIORITY, WEST_TOP_SPECIAL, false);
								hill2 = location.hill_id2();
								location.remove_hill();
								location.set_hill(hillId);
								location.set_power_off();
								World.set_surr_power_off(x, y);
								if (hill2 != 0)
									location.set_hill(hill2);

								// next row
								Location loc2 = World.get_loc(x, y + 1);
								hillId = HillRes.locate(h1p,
									HillRes[hillId].sub_pattern_id,
									HillRes.LOW_HILL_PRIORITY, WEST_TOP_SPECIAL);
								loc2.remove_hill();
								loc2.set_hill(hillId);
								loc2.set_power_off();
								World.set_surr_power_off(x, y + 1);
								if ((terrainId = TerrainRes.scan(terrainType - 1, terrainType - 1, 0, 0, 1, 0)) != 0)
									loc2.terrain_id = terrainId;

								// third row
								loc2 = World.get_loc(x, y + 2);
								loc2.remove_hill();
								loc2.walkable_reset();
								//if((terrainId = terrain_res.scan(terrainType-1, terrainType-1,
								//	0, 0, 1, WEST_CENTRE_SPECIAL)) != 0 )
								//	loc2->terrain_id = terrainId;
								if ((terrainId = TerrainRes.scan(terrainType, terrainType - 1, WEST_PATTERN2, 0, 1,
									    WEST_CENTRE_SPECIAL)) != 0)
									loc2.terrain_id = terrainId;

								// next column
								//loc2 = get_loc(x+1, y+2);
								//if((terrainId = terrain_res.scan(terrainType, terrainType-1,
								//	WEST_PATTERN2, 0, 1, WEST_CENTRE_SPECIAL )) != 0 )
								//	loc2->terrain_id = terrainId;

								// fourth row
								loc2 = World.get_loc(x, y + 3);
								if (h3p == WEST_PATTERN4)
									h3p = WEST_PATTERN1;
								if (h3p == WEST_PATTERN3)
									h3p = WEST_PATTERN2;
								hillId = HillRes.scan(h3p, HillRes.HIGH_HILL_PRIORITY, WEST_BOTTOM_SPECIAL, false);
								loc2.remove_hill();
								loc2.set_hill(hillId);
								loc2.set_power_off();
								World.set_surr_power_off(x, y + 3);
								if ((terrainId = TerrainRes.scan(terrainType - 1, terrainType - 1, 0, 0, 1, 0)) != 0)
									loc2.terrain_id = terrainId;

								// next row
								loc2 = World.get_loc(x, y + 4);
								hillId = HillRes.locate(h3p,
									HillRes[hillId].sub_pattern_id,
									HillRes.LOW_HILL_PRIORITY, WEST_BOTTOM_SPECIAL);
								loc2.set_hill(hillId);
								loc2.set_power_off();
								World.set_surr_power_off(x, y + 4);
								if ((terrainId = TerrainRes.scan(terrainType - 1, terrainType - 1, 0, 0, 1, 0)) != 0)
									loc2.terrain_id = terrainId;
								lastExit = MIN_EXIT_SEPARATION;
							}
						}
					}
				}
			}
		}

		// ------ scan for east exit, width 1 --------//

		const int EAST_PATTERN1 = 10;
		const int EAST_PATTERN2 = 14;
		const int EAST_PATTERN3 = 18;
		const int EAST_PATTERN4 = 22;

		bool IsEastExitPattern(int h)
		{
			return h == EAST_PATTERN1 || h == EAST_PATTERN2 || h == EAST_PATTERN3 || h == EAST_PATTERN4;
		}

		const char EAST_TOP_SPECIAL = 'J';
		const char EAST_BOTTOM_SPECIAL = 'L';
		const char EAST_CENTRE_SPECIAL = 'K';

		for (x = 1; x < GameConstants.MapSize - 1; ++x)
		{
			lastExit = 0;
			for (y = GameConstants.MapSize - 5; y >= 0; --y, lastExit = lastExit > 0 ? lastExit - 1 : 0)
			{
				Location location = World.get_loc(x, y);
				// three hill blocks on a row are pattern 9, 13, 17, 21
				// block above the second block is a walkable
				if (lastExit == 0)
				{
					if (World.get_loc(x, y).has_hill() && World.get_loc(x, y + 1).has_hill() &&
					    World.get_loc(x, y + 2).has_hill() && World.get_loc(x, y + 3).has_hill())
					{
						HillBlockInfo h1 = HillRes[World.get_loc(x, y).hill_id1()];
						int h1p = h1.pattern_id;
						HillBlockInfo h2 = HillRes[World.get_loc(x, y + 2).hill_id1()];
						int h2p = h2.pattern_id;
						HillBlockInfo h3 = HillRes[World.get_loc(x, y + 3).hill_id1()];
						int h3p = h3.pattern_id;
						if (h1.special_flag == 0 &&
						    h1.priority == HillRes.HIGH_HILL_PRIORITY && h1p != 0 && IsEastExitPattern(h1p) &&
						    h2.priority == HillRes.HIGH_HILL_PRIORITY && h2p != 0 && IsEastExitPattern(h2p) &&
						    h3.priority == HillRes.HIGH_HILL_PRIORITY && h3p != 0 && IsEastExitPattern(h3p))
						{
							if ((h3p == EAST_PATTERN1 || h3p == EAST_PATTERN4) &&
							    World.get_loc(x - 1, y + 2).walkable())
							{
								int hillId, terrainId, hill2;

								// change this square
								if (h1p == EAST_PATTERN3)
									h1p = EAST_PATTERN1;
								else if (h1p == EAST_PATTERN4)
									h1p = EAST_PATTERN2;
								hillId = HillRes.scan(h1p, HillRes.HIGH_HILL_PRIORITY, EAST_TOP_SPECIAL, false);
								hill2 = location.hill_id2();
								location.remove_hill();
								location.set_hill(hillId);
								if (hill2 != 0)
									location.set_hill(hill2);
								location.set_power_off();
								World.set_surr_power_off(x, y);

								// next row
								Location loc2 = World.get_loc(x, y + 1);
								hillId = HillRes.locate(h1p,
									HillRes[hillId].sub_pattern_id, HillRes.LOW_HILL_PRIORITY,
									EAST_TOP_SPECIAL);
								loc2.remove_hill();
								loc2.set_hill(hillId);
								loc2.set_power_off();
								World.set_surr_power_off(x, y + 1);
								if ((terrainId = TerrainRes.scan(terrainType - 1, terrainType - 1, 0, 0, 1, 0)) != 0)
									loc2.terrain_id = terrainId;

								// third row
								loc2 = World.get_loc(x, y + 2);
								loc2.remove_hill();
								loc2.walkable_reset();
								//if((terrainId = terrain_res.scan(terrainType-1, terrainType-1,
								//	0, 0, 1, EAST_CENTRE_SPECIAL)) != 0 )
								//	loc2->terrain_id = terrainId;
								if ((terrainId = TerrainRes.scan(terrainType, terrainType - 1, EAST_PATTERN2,
									    0, 1, EAST_CENTRE_SPECIAL)) != 0)
									loc2.terrain_id = terrainId;

								// next column
								//loc2 = get_loc(x-1, y+2);
								//if((terrainId = terrain_res.scan(terrainType, terrainType-1,
								//	EAST_PATTERN2, 0, 1, EAST_CENTRE_SPECIAL )) != 0 )
								//	loc2->terrain_id = terrainId;

								// fourth row
								loc2 = World.get_loc(x, y + 3);
								if (h3p == EAST_PATTERN4)
									h3p = EAST_PATTERN1;
								if (h3p == EAST_PATTERN3)
									h3p = EAST_PATTERN2;
								hillId = HillRes.scan(h3p, HillRes.HIGH_HILL_PRIORITY, EAST_BOTTOM_SPECIAL, false);
								loc2.remove_hill();
								loc2.set_hill(hillId);
								loc2.set_power_off();
								World.set_surr_power_off(x, y + 3);
								if ((terrainId = TerrainRes.scan(terrainType - 1, terrainType - 1, 0, 0, 1, 0)) != 0)
									loc2.terrain_id = terrainId;

								// next row
								loc2 = World.get_loc(x, y + 4);
								hillId = HillRes.locate(h3p,
									HillRes[hillId].sub_pattern_id, HillRes.LOW_HILL_PRIORITY,
									EAST_BOTTOM_SPECIAL);
								loc2.set_hill(hillId);
								loc2.set_power_off();
								World.set_surr_power_off(x, y + 4);
								if ((terrainId = TerrainRes.scan(terrainType - 1, terrainType - 1, 0, 0, 1, 0)) != 0)
									loc2.terrain_id = terrainId;
								lastExit = MIN_EXIT_SEPARATION;
							}
						}
					}
				}
			}
		}
	}

	private void SetRegionId()
	{
		for (int y = 0; y < GameConstants.MapSize; ++y)
		{
			for (int x = 0; x < GameConstants.MapSize; ++x)
			{
				World.get_loc(x, y).region_id = 0;
			}
		}

		int regionId = 0;
		for (int y = 0; y < GameConstants.MapSize; ++y)
		{
			for (int x = 0; x < GameConstants.MapSize; ++x)
			{
				Location location = World.get_loc(x, y);
				if (location.region_id == 0 && location.region_type() != RegionType.REGION_INPASSABLE)
				{
					regionId++;
					FillRegion(x, y, regionId, location.region_type());
				}
			}
		}

		RegionArray.Init(regionId);

		// ------ update adjacency information and region area ------//

		regionId = 0;
		for (int y = 0; y < GameConstants.MapSize; ++y)
		{
			for (int x = 0; x < GameConstants.MapSize; ++x)
			{
				Location location = World.get_loc(x, y);
				int thisRegionId = location.region_id;
				if (thisRegionId > 0)
				{
					RegionArray.inc_size(thisRegionId);
				}

				if (thisRegionId > regionId)
				{
					if (thisRegionId == regionId + 1)
						regionId++;
					RegionArray.set_region(thisRegionId, location.region_type());
				}

				int adjRegionId;
				if (y > 0)
				{
					if (x > 0 && (adjRegionId = World.get_loc(x - 1, y - 1).region_id) < thisRegionId)
						RegionArray.set_adjacent(thisRegionId, adjRegionId);
					if ((adjRegionId = World.get_loc(x, y - 1).region_id) < thisRegionId)
						RegionArray.set_adjacent(thisRegionId, adjRegionId);
					if (x < GameConstants.MapSize - 1 && (adjRegionId = World.get_loc(x + 1, y - 1).region_id) < thisRegionId)
						RegionArray.set_adjacent(thisRegionId, adjRegionId);
				}

				if (x > 0 && (adjRegionId = World.get_loc(x - 1, y).region_id) < thisRegionId)
					RegionArray.set_adjacent(thisRegionId, adjRegionId);
				if (x < GameConstants.MapSize - 1 && (adjRegionId = World.get_loc(x + 1, y).region_id) < thisRegionId)
					RegionArray.set_adjacent(thisRegionId, adjRegionId);

				if (y < GameConstants.MapSize - 1)
				{
					if (x > 0 && (adjRegionId = World.get_loc(x - 1, y + 1).region_id) < thisRegionId)
						RegionArray.set_adjacent(thisRegionId, adjRegionId);
					if ((adjRegionId = World.get_loc(x, y + 1).region_id) < thisRegionId)
						RegionArray.set_adjacent(thisRegionId, adjRegionId);
					if (x < GameConstants.MapSize - 1 && (adjRegionId = World.get_loc(x + 1, y + 1).region_id) < thisRegionId)
						RegionArray.set_adjacent(thisRegionId, adjRegionId);
				}
			}
		}

		RegionArray.sort_region();
		RegionArray.init_region_stat();
	}

	private void FillRegion(int x, int y, int regionId, RegionType regionType)
	{
		int left, right;

		// extent x to left and right
		for (left = x;
		     left >= 0 && World.get_loc(left, y).region_id == 0 && World.get_loc(left, y).region_type() == regionType;
		     --left)
		{
			World.get_loc(left, y).region_id = regionId;
		}

		++left;

		for (right = x + 1;
		     right < GameConstants.MapSize && World.get_loc(right, y).region_id == 0 &&
		     World.get_loc(right, y).region_type() == regionType;
		     ++right)
		{
			World.get_loc(right, y).region_id = regionId;
		}

		--right;

		// ------- scan line below ---------//
		y++;
		if (y < GameConstants.MapSize)
		{
			for (x = left > 0 ? left - 1 : 0; x <= right + 1 && x < GameConstants.MapSize; ++x)
			{
				if (World.get_loc(x, y).region_id == 0 && World.get_loc(x, y).region_type() == regionType)
				{
					FillRegion(x, y, regionId, regionType);
				}
			}
		}

		// ------- scan line above -------- //
		y -= 2;
		if (y >= 0)
		{
			for (x = left > 0 ? left - 1 : 0; x <= right + 1 && x < GameConstants.MapSize; ++x)
			{
				if (World.get_loc(x, y).region_id == 0 && World.get_loc(x, y).region_type() == regionType)
				{
					FillRegion(x, y, regionId, regionType);
				}
			}
		}
	}

	private void GenDirt(int nGrouped, int nLarge, int nSmall)
	{
		// one 'large' (size 1 to 4) at the center
		// and a number 'small' (size 1 to 2) at the surroundings

		const int GAP = 4;
		const int HUGE_ROCK_SIZE = 6;
		const int LARGE_ROCK_SIZE = 4;
		const int SMALL_ROCK_SIZE = 2;

		int trial = (nGrouped + nLarge + nSmall) * 2;

		while ((nGrouped > 0 || nLarge > 0 || nSmall > 0) && --trial > 0)
		{
			// generate grouped dirt
			if (nGrouped > 0)
			{
				int x = (GAP + SMALL_ROCK_SIZE) +
				        Misc.Random(GameConstants.MapSize - LARGE_ROCK_SIZE + 1 - 2 * (GAP + SMALL_ROCK_SIZE));
				int y = (GAP + SMALL_ROCK_SIZE) +
				        Misc.Random(GameConstants.MapSize - LARGE_ROCK_SIZE + 1 - 2 * (GAP + SMALL_ROCK_SIZE));
				int x2 = x + LARGE_ROCK_SIZE - 1;
				int y2 = y + LARGE_ROCK_SIZE - 1;

				if (World.can_add_dirt(x, y, x2, y2))
				{
					int rockRecno = RockRes.search("DE", 1, LARGE_ROCK_SIZE,
						1, LARGE_ROCK_SIZE, -1, false,
						TerrainRes[World.get_loc(x, y).terrain_id].average_type);
					if (rockRecno == 0)
						continue;

					RockInfo rockInfo = RockRes.get_rock_info(rockRecno);
					x2 = x + rockInfo.loc_width - 1;
					y2 = y + rockInfo.loc_height - 1;
					if (rockInfo.valid_terrain(TerrainRes[World.get_loc(x2, y).terrain_id].average_type)
					    && rockInfo.valid_terrain(TerrainRes[World.get_loc(x, y2).terrain_id].average_type)
					    && rockInfo.valid_terrain(TerrainRes[World.get_loc(x2, y2).terrain_id].average_type))
					{
						World.add_dirt(rockRecno, x, y);

						// add other smaller rock
						for (int subTrial = Misc.Random(14); subTrial > 0; --subTrial)
						{
							// sx from x-SMALL_ROCK_SIZE to x+4-1+SMALL_ROCK_SIZE
							int sx = x - SMALL_ROCK_SIZE - GAP +
							         Misc.Random(LARGE_ROCK_SIZE + SMALL_ROCK_SIZE + 2 * GAP);
							int sy = y - SMALL_ROCK_SIZE - GAP +
							         Misc.Random(LARGE_ROCK_SIZE + SMALL_ROCK_SIZE + 2 * GAP);
							int sx2 = sx + SMALL_ROCK_SIZE - 1;
							int sy2 = sy + SMALL_ROCK_SIZE - 1;

							if (World.can_add_dirt(sx, sy, sx2, sy2))
							{
								int rock2Recno = RockRes.search("DE", 1, SMALL_ROCK_SIZE,
									1, SMALL_ROCK_SIZE, -1, false,
									TerrainRes[World.get_loc(sx, sy).terrain_id].average_type);
								if (rock2Recno == 0)
									continue;

								RockInfo rock2Info = RockRes.get_rock_info(rock2Recno);
								sx2 = sx + rock2Info.loc_width - 1;
								sy2 = sy + rock2Info.loc_height - 1;
								if (rock2Info.valid_terrain(TerrainRes[World.get_loc(sx2, sy).terrain_id].average_type) &&
								    rock2Info.valid_terrain(TerrainRes[World.get_loc(sx, sy2).terrain_id].average_type) &&
								    rock2Info.valid_terrain(TerrainRes[World.get_loc(sx2, sy2).terrain_id].average_type))
								{
									World.add_dirt(rock2Recno, sx, sy);
								}
							}
						}

						nGrouped--;
					}
				}
			}

			// generate stand-alone large dirt
			if (nLarge > 0)
			{
				int x = Misc.Random(GameConstants.MapSize - HUGE_ROCK_SIZE);
				int y = Misc.Random(GameConstants.MapSize - HUGE_ROCK_SIZE);
				int x2 = x + HUGE_ROCK_SIZE - 1;
				int y2 = y + HUGE_ROCK_SIZE - 1;

				if (World.can_add_dirt(x, y, x2, y2))
				{
					int rockRecno = RockRes.search("DE", SMALL_ROCK_SIZE + 1, HUGE_ROCK_SIZE,
						SMALL_ROCK_SIZE + 1, HUGE_ROCK_SIZE, -1, false,
						TerrainRes[World.get_loc(x, y).terrain_id].average_type);
					if (rockRecno == 0)
						continue;

					RockInfo rockInfo = RockRes.get_rock_info(rockRecno);
					x2 = x + rockInfo.loc_width - 1;
					y2 = y + rockInfo.loc_height - 1;
					if (rockInfo.valid_terrain(TerrainRes[World.get_loc(x2, y).terrain_id].average_type) &&
					    rockInfo.valid_terrain(TerrainRes[World.get_loc(x, y2).terrain_id].average_type) &&
					    rockInfo.valid_terrain(TerrainRes[World.get_loc(x2, y2).terrain_id].average_type))
					{
						World.add_dirt(rockRecno, x, y);
						nLarge--;
					}
				}
			}

			// generate stand-alone small dirt
			if (nSmall > 0)
			{
				int x = Misc.Random(GameConstants.MapSize - SMALL_ROCK_SIZE);
				int y = Misc.Random(GameConstants.MapSize - SMALL_ROCK_SIZE);
				int x2 = x + SMALL_ROCK_SIZE - 1;
				int y2 = y + SMALL_ROCK_SIZE - 1;

				if (World.can_add_dirt(x, y, x2, y2))
				{
					int rockRecno = RockRes.search("DE", 1, SMALL_ROCK_SIZE,
						1, SMALL_ROCK_SIZE, -1, false,
						TerrainRes[World.get_loc(x, y).terrain_id].average_type);
					if (rockRecno == 0)
						continue;

					RockInfo rockInfo = RockRes.get_rock_info(rockRecno);
					x2 = x + rockInfo.loc_width - 1;
					y2 = y + rockInfo.loc_height - 1;
					if (rockInfo.valid_terrain(TerrainRes[World.get_loc(x2, y).terrain_id].average_type) &&
					    rockInfo.valid_terrain(TerrainRes[World.get_loc(x, y2).terrain_id].average_type) &&
					    rockInfo.valid_terrain(TerrainRes[World.get_loc(x2, y2).terrain_id].average_type))
					{
						World.add_dirt(rockRecno, x, y);
						nSmall--;
					}
				}
			}
		}
	}

	private void GenRocks(int nGrouped, int nLarge, int nSmall)
	{
		// one 'large' (size 1 to 4) at the center
		// and a number 'small' (size 1 to 2) at the surroundings

		const int GAP = 4;
		const int HUGE_ROCK_SIZE = 6;
		const int LARGE_ROCK_SIZE = 4;
		const int SMALL_ROCK_SIZE = 2;

		int trial = (nGrouped + nLarge + nSmall) * 2;

		while ((nGrouped > 0 || nLarge > 0 || nSmall > 0) && --trial > 0)
		{
			// generate grouped rocks
			if (nGrouped > 0)
			{
				int x = (GAP + SMALL_ROCK_SIZE) +
				        Misc.Random(GameConstants.MapSize - LARGE_ROCK_SIZE + 1 - 2 * (GAP + SMALL_ROCK_SIZE));
				int y = (GAP + SMALL_ROCK_SIZE) +
				        Misc.Random(GameConstants.MapSize - LARGE_ROCK_SIZE + 1 - 2 * (GAP + SMALL_ROCK_SIZE));
				int x2 = x + LARGE_ROCK_SIZE - 1;
				int y2 = y + LARGE_ROCK_SIZE - 1;

				if (World.can_add_rock(x, y, x2, y2))
				{
					int rockRecno = RockRes.search("R", 1, LARGE_ROCK_SIZE,
						1, LARGE_ROCK_SIZE, -1, false,
						TerrainRes[World.get_loc(x, y).terrain_id].average_type);
					if (rockRecno == 0)
						continue;

					RockInfo rockInfo = RockRes.get_rock_info(rockRecno);
					x2 = x + rockInfo.loc_width - 1;
					y2 = y + rockInfo.loc_height - 1;
					if (rockInfo.valid_terrain(TerrainRes[World.get_loc(x2, y).terrain_id].average_type) &&
					    rockInfo.valid_terrain(TerrainRes[World.get_loc(x, y2).terrain_id].average_type) &&
					    rockInfo.valid_terrain(TerrainRes[World.get_loc(x2, y2).terrain_id].average_type))
					{
						World.add_rock(rockRecno, x, y);

						// add other smaller rock
						for (int subTrial = Misc.Random(14); subTrial > 0; --subTrial)
						{
							// sx from x-SMALL_ROCK_SIZE to x+4-1+SMALL_ROCK_SIZE
							int sx = x - SMALL_ROCK_SIZE - GAP +
							         Misc.Random(LARGE_ROCK_SIZE + SMALL_ROCK_SIZE + 2 * GAP);
							int sy = y - SMALL_ROCK_SIZE - GAP +
							         Misc.Random(LARGE_ROCK_SIZE + SMALL_ROCK_SIZE + 2 * GAP);
							int sx2 = sx + SMALL_ROCK_SIZE - 1;
							int sy2 = sy + SMALL_ROCK_SIZE - 1;

							if (World.can_add_rock(sx, sy, sx2, sy2))
							{
								int rock2Recno = RockRes.search("R", 1, SMALL_ROCK_SIZE,
									1, SMALL_ROCK_SIZE, -1, false,
									TerrainRes[World.get_loc(sx, sy).terrain_id].average_type);
								if (rock2Recno == 0)
									continue;

								RockInfo rock2Info = RockRes.get_rock_info(rock2Recno);
								sx2 = sx + rock2Info.loc_width - 1;
								sy2 = sy + rock2Info.loc_height - 1;
								if (rock2Info.valid_terrain(TerrainRes[World.get_loc(sx2, sy).terrain_id].average_type) &&
								    rock2Info.valid_terrain(TerrainRes[World.get_loc(sx, sy2).terrain_id].average_type) &&
								    rock2Info.valid_terrain(TerrainRes[World.get_loc(sx2, sy2).terrain_id].average_type))
								{
									World.add_rock(rock2Recno, sx, sy);
								}
							}
						}

						nGrouped--;
					}
				}
			}

			// generate stand-alone large rock
			if (nLarge > 0)
			{
				int x = Misc.Random(GameConstants.MapSize - HUGE_ROCK_SIZE);
				int y = Misc.Random(GameConstants.MapSize - HUGE_ROCK_SIZE);
				int x2 = x + HUGE_ROCK_SIZE - 1;
				int y2 = y + HUGE_ROCK_SIZE - 1;

				if (World.can_add_rock(x, y, x2, y2))
				{
					int rockRecno = RockRes.search("R", SMALL_ROCK_SIZE + 1, HUGE_ROCK_SIZE,
						LARGE_ROCK_SIZE + 1, HUGE_ROCK_SIZE, -1, false,
						TerrainRes[World.get_loc(x, y).terrain_id].average_type);
					if (rockRecno == 0)
						continue;

					RockInfo rockInfo = RockRes.get_rock_info(rockRecno);
					x2 = x + rockInfo.loc_width - 1;
					y2 = y + rockInfo.loc_height - 1;
					if (rockInfo.valid_terrain(TerrainRes[World.get_loc(x2, y).terrain_id].average_type) &&
					    rockInfo.valid_terrain(TerrainRes[World.get_loc(x, y2).terrain_id].average_type) &&
					    rockInfo.valid_terrain(TerrainRes[World.get_loc(x2, y2).terrain_id].average_type))
					{
						World.add_rock(rockRecno, x, y);
						nLarge--;
					}
				}
			}

			// generate stand-alone small rock
			if (nSmall > 0)
			{
				int x = Misc.Random(GameConstants.MapSize - SMALL_ROCK_SIZE);
				int y = Misc.Random(GameConstants.MapSize - SMALL_ROCK_SIZE);
				int x2 = x + SMALL_ROCK_SIZE - 1;
				int y2 = y + SMALL_ROCK_SIZE - 1;

				if (World.can_add_rock(x, y, x2, y2))
				{
					int rockRecno = RockRes.search("R", 1, SMALL_ROCK_SIZE,
						1, SMALL_ROCK_SIZE, -1, false,
						TerrainRes[World.get_loc(x, y).terrain_id].average_type);
					if (rockRecno == 0)
						continue;

					RockInfo rockInfo = RockRes.get_rock_info(rockRecno);
					x2 = x + rockInfo.loc_width - 1;
					y2 = y + rockInfo.loc_height - 1;
					if (rockInfo.valid_terrain(TerrainRes[World.get_loc(x2, y).terrain_id].average_type) &&
					    rockInfo.valid_terrain(TerrainRes[World.get_loc(x, y2).terrain_id].average_type) &&
					    rockInfo.valid_terrain(TerrainRes[World.get_loc(x2, y2).terrain_id].average_type))
					{
						World.add_rock(rockRecno, x, y);
						nSmall--;
					}
				}
			}
		}
	}

	private void SetHarborBit()
	{
		// a pass during genmap to set LOCATE_HARBOR_BIT
		// notice this bit is only a necessary condition to build harbor
		for (int y = 0; y < GameConstants.MapSize - 2; ++y)
		{
			for (int x = 0; x < GameConstants.MapSize - 2; ++x)
			{
				if (World.can_build_firm(x, y, Firm.FIRM_HARBOR) != 0)
				{
					World.get_loc(x, y).set_harbor_bit();
				}
			}
		}
	}

	private void CreateAINation(int aiNationCount)
	{
		for (int i = 0; i < aiNationCount; i++)
		{
			int raceId;
			if (Config.random_start_up)
				raceId = ConfigAdv.GetRandomRace();
			else
				raceId = NationArray.random_unused_race();

			NationArray.new_nation(NationBase.NATION_AI, raceId, NationArray.random_unused_color());
		}
	}

	private Town CreateTown(int nationRecno, int raceId, out int xLoc, out int yLoc)
	{
		if (!TownArray.think_town_loc(GameConstants.MapSize * GameConstants.MapSize, out xLoc, out yLoc))
			return null;

		Town town = TownArray.AddTown(nationRecno, raceId, xLoc, yLoc);

		//--------- no. of mixed races ---------//

		if (nationRecno != 0)
		{
			int initPop;

			if (Config.random_start_up)
				initPop = 25 + Misc.Random(26); // 25 to 50
			else
				initPop = 40;

			town.init_pop(raceId, initPop, 100, false, true);
		}
		else
		{
			int mixedRaceCount = Misc.Random(3) + 1;

			int totalPop = 0;

			for (int i = 0; i < mixedRaceCount; i++)
			{
				if (totalPop >= GameConstants.MAX_TOWN_POPULATION)
					break;

				int townResistance = TownArray.independent_town_resistance();

				int curPop;
				if (i == 0)
				{
					curPop = 15 / mixedRaceCount + Misc.Random(15 / mixedRaceCount);
					if (curPop >= GameConstants.MAX_TOWN_POPULATION)
						curPop = GameConstants.MAX_TOWN_POPULATION;

					town.init_pop(raceId, curPop, townResistance, false, true);
					totalPop += curPop;
				}
				else
				{
					curPop = 10 / mixedRaceCount + Misc.Random(10 / mixedRaceCount);
					if (curPop >= GameConstants.MAX_TOWN_POPULATION - totalPop)
						curPop = GameConstants.MAX_TOWN_POPULATION - totalPop;

					town.init_pop(ConfigAdv.GetRandomRace(), curPop, townResistance, false, true);
					totalPop += curPop;
				}
			}
		}

		//---------- set town layout -----------//

		town.auto_set_layout();

		return town;
	}

	private Unit CreateUnit(Town town, int unitId, int rankId)
	{
		SpriteInfo spriteInfo = SpriteRes[UnitRes[unitId].sprite_id];

		//------ locate space for the unit ------//

		int xLoc = town.loc_x1;
		int yLoc = town.loc_y1;

		if (!World.locate_space(ref xLoc, ref yLoc,
			    xLoc + InternalConstants.TOWN_WIDTH - 1, yLoc + InternalConstants.TOWN_HEIGHT - 1,
			    spriteInfo.loc_width, spriteInfo.loc_height))
		{
			return null;
		}

		//---------- create the unit --------//

		int unitLoyalty = 80 + Misc.Random(20);

		Unit unit = UnitArray.AddUnit(unitId, town.nation_recno, rankId, unitLoyalty, xLoc, yLoc);

		if (unit == null)
			return null;

		//----------- set skill -------------//

		switch (rankId)
		{
			case Unit.RANK_KING:
				unit.skill.set_skill(Skill.SKILL_LEADING);
				unit.skill.skill_level = 100;
				unit.set_combat_level(100);
				break;

			case Unit.RANK_GENERAL:
				unit.skill.set_skill(Skill.SKILL_LEADING);
				unit.skill.skill_level = 40 + Misc.Random(50); // 40 to 90
				unit.set_combat_level(30 + Misc.Random(70)); // 30 to 100
				break;

			case Unit.RANK_SOLDIER:
			{
				int skillId = Misc.Random(Skill.MAX_TRAINABLE_SKILL) + 1;
				bool spyFlag = false;

				if (skillId == Skill.SKILL_SPYING)
				{
					unit.set_combat_level(10 + Misc.Random(10));
					spyFlag = true;
				}
				else
				{
					unit.skill.skill_id = skillId;
					unit.skill.skill_level = 30 + Misc.Random(70);

					if (skillId == Skill.SKILL_LEADING)
						unit.set_combat_level(30 + Misc.Random(70));
					else
						unit.set_combat_level(10 + Misc.Random(10));

					if (Misc.Random(5) == 0)
						spyFlag = true;
				}

				if (spyFlag)
				{
					int spySkill = 20 + Misc.Random(80); // 20 to 100
					unit.spy_recno = SpyArray.AddSpy(unit.sprite_recno, spySkill).spy_recno;
				}

				break;
			}
		}

		return unit;
	}

	private void CreatePregameObjects()
	{
		//------- create nation and units --------//

		List<Nation> uninitializedNations = new List<Nation>();
		foreach (Nation nation in NationArray)
		{
			uninitializedNations.Add(nation);
		}

		bool noSpaceFlag = false;

		foreach (Nation nation in NationArray)
		{
			//--------- create town -----------//

			Town town = CreateTown(nation.nation_recno, nation.race_id, out _, out _);

			if (town == null)
			{
				noSpaceFlag = true;
				break;
			}

			//------- create military camp -------//

			int firmRecno = FirmArray.BuildFirm(town.loc_x1 + 6, town.loc_y1,
				nation.nation_recno, Firm.FIRM_CAMP, RaceRes[nation.race_id].code);

			if (firmRecno == 0)
			{
				noSpaceFlag = true;
				break;
			}

			FirmArray[firmRecno].complete_construction();

			//--------- create units ----------//

			int unitId = RaceRes[nation.race_id].basic_unit_id;

			Unit king = CreateUnit(town, unitId, Unit.RANK_KING);

			if (king != null)
			{
				nation.set_king(king.sprite_recno, 1);
				FirmArray[firmRecno].assign_overseer(king.sprite_recno);
			}
			else
			{
				noSpaceFlag = true;
				break;
			}

			//----- create skilled units if config.random_start_up is 1 -----//

			if (Config.random_start_up)
			{
				// the less population the villager has the more mobile units will be created
				int createCount = (50 - town.population) / 3;

				for (int i = 0; i < createCount; i++)
				{
					if (Misc.Random(2) == 0)
						unitId = RaceRes[nation.race_id].basic_unit_id;
					else
						unitId = RaceRes[ConfigAdv.GetRandomRace()].basic_unit_id;

					int rankId;
					if (Misc.Random(3) == 0)
						rankId = Unit.RANK_GENERAL;
					else
						rankId = Unit.RANK_SOLDIER;

					if (CreateUnit(town, unitId, rankId) == null)
						break;
				}
			}

			//------ create mines near towns in the beginning -----//

			if (Config.start_up_has_mine_nearby && !nation.is_ai())
				SiteArray.create_raw_site(town.town_recno);

			//------ set ai base town -----//
			if (nation.is_ai())
				nation.update_ai_region();

			uninitializedNations.Remove(nation);
		}

		//--- if there is no space for creating new town/firm or unit, delete the unprocessed nations ---//

		foreach (Nation nation in uninitializedNations)
		{
			NationArray.DeleteNation(nation);
		}

		//---- init the type of active monsters in this game ----//

		MonsterRes.init_active_monster();

		//------ create independent towns -------//

		int startUpIndependentTown = Config.start_up_independent_town;
		//int startUpRawSite = config.start_up_raw_site;
		int startUpMonsterFirm = 15;

		SiteArray.generate_raw_site(Config.start_up_raw_site);

		int maxLoopCount = startUpIndependentTown + startUpMonsterFirm;

		for (int j = 0, k = 1; j < maxLoopCount; j++, k++)
		{
			if (startUpIndependentTown > 0)
			{
				//------ create independent towns -------//
				int raceId = ConfigAdv.race_random_list[k % ConfigAdv.race_random_list_max];
				if (CreateTown(0, raceId, out _, out _) == null)
				{
					startUpIndependentTown = 0;
					break;
				}
				else
				{
					startUpIndependentTown--;
				}
			}

			if (startUpMonsterFirm > 0)
			{
				//------- create monsters --------//
				if (Config.monster_type != Config.OPTION_MONSTER_NONE)
				{
					MonsterRes.generate(1);
					startUpMonsterFirm--;
				}
				else
				{
					startUpMonsterFirm = 0;
				}
			}

			if (startUpIndependentTown + startUpMonsterFirm == 0)
				break;
		}

		Sys.Instance.NationArray.update_statistic();
	}
}