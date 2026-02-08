using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class MapGenerator
{
	private TerrainRes TerrainRes => Sys.Instance.TerrainRes;
	private HillRes HillRes => Sys.Instance.HillRes;
	private RockRes RockRes => Sys.Instance.RockRes;
	private RaceRes RaceRes => Sys.Instance.RaceRes;
	private FirmRes FirmRes => Sys.Instance.FirmRes;
	private SpriteRes SpriteRes => Sys.Instance.SpriteRes;
	private UnitRes UnitRes => Sys.Instance.UnitRes;
	private MonsterRes MonsterRes => Sys.Instance.MonsterRes;

	private Config Config => Sys.Instance.Config;
	private ConfigAdv ConfigAdv => Sys.Instance.ConfigAdv;
	private World World => Sys.Instance.World;

	private RockArray RockArray => Sys.Instance.RockArray;
	private RockArray DirtArray => Sys.Instance.DirtArray;
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
		Plasma heightMap = new Plasma(GameConstants.MapSize, GameConstants.MapSize);
		heightMap.Generate(Misc.Random(2), 5);

		// grouping plasma sample data, find sea or land first
		int totalLoc = (GameConstants.MapSize + 1) * (GameConstants.MapSize + 1);
		int[] heightLimit = new int[2];
		int[] heightFreq = new int[2];
		int minLandCount = 0;
		int maxLandCount = 0;
		int initHeightLimit = TerrainRes.MinHeight(TerrainTypeCode.TERRAIN_DARK_GRASS);
		switch (Config.LandMass)
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
		heightMap.Stat(2, heightLimit, heightFreq);

		int landCount = heightFreq[1];
		int seaCount = heightFreq[0];

		int loopCount = 0;
		while (++loopCount <= 4 && (landCount < minLandCount || landCount > maxLandCount))
		{
			if (landCount < minLandCount)
			{
				// positive AddBaseLevel to gain more land
				// find a level between 0 to TerrainRes::MinHeight(TERRAIN_DARK_GRASS)
				// assume heightLevel below heightLimit[1] is evenly distributed,
				// approximate a new heightLimit[1] such that landCount is avgLandCount

				heightLimit[1] -= (avgLandCount - landCount) * (heightLimit[1] - heightLimit[0]) / seaCount;
			}
			else if (landCount > maxLandCount)
			{
				// negative AddBaseLevel to reduce land
				// find a level above TerrainRes::MinHeight(TERRAIN_DARK_GRASS)
				// assume heightLevel above heightLimit[1] is evenly distributed,
				// approximate a new heightLimit[1] such that landCount is avgLandCount

				const int MAX_HEIGHT_LIMIT = 255;
				heightLimit[1] = MAX_HEIGHT_LIMIT - avgLandCount * (MAX_HEIGHT_LIMIT - heightLimit[1]) / landCount;
			}

			heightMap.Stat(2, heightLimit, heightFreq);
		}

		if (Math.Abs(heightLimit[1] - initHeightLimit) > 2)
		{
			heightMap.AddBaseLevel(initHeightLimit - heightLimit[1]);
		}

		// --------- remove odd terrain --------//

		for (int y = 0; y <= heightMap.MaxY; y++)
		{
			for (int x = 0; x <= heightMap.MaxX; x++)
			{
				RemoveOdd(heightMap, x, y, 5);
			}
		}

		// ------------ shuffle sub-terrain level ---------//

		heightMap.ShuffleLevel(TerrainRes.MinHeight(TerrainTypeCode.TERRAIN_OCEAN),
			TerrainRes.MaxHeight(TerrainTypeCode.TERRAIN_OCEAN), -3);
		heightMap.ShuffleLevel(TerrainRes.MinHeight(TerrainTypeCode.TERRAIN_DARK_GRASS),
			TerrainRes.MaxHeight(TerrainTypeCode.TERRAIN_DARK_GRASS), 3);
		heightMap.ShuffleLevel(TerrainRes.MinHeight(TerrainTypeCode.TERRAIN_LIGHT_GRASS),
			TerrainRes.MaxHeight(TerrainTypeCode.TERRAIN_LIGHT_GRASS), 3);
		heightMap.ShuffleLevel(TerrainRes.MinHeight(TerrainTypeCode.TERRAIN_DARK_DIRT),
			TerrainRes.MaxHeight(TerrainTypeCode.TERRAIN_DARK_DIRT), 3);

		SetTeraId(heightMap);

		SubstitutePattern();

		SetLocFlags();

		GenerateHills(TerrainTypeCode.TERRAIN_DARK_DIRT);

		SetRegionId();

		double scaleFactor = (GameConstants.MapSize * GameConstants.MapSize) / (200.0 * 200.0);
		
		GenDirt((int)(40 * scaleFactor), (int)(30 * scaleFactor), (int)(60 * scaleFactor));

		GenRocks((int)(5 * scaleFactor), (int)(10 * scaleFactor), (int)(30 * scaleFactor));

		SetHarborBit();

		World.PlantInit();

		World.FireInit();

		CreatePlayerNation();

		CreateAINations(Config.AINationCount);
		
		CreatePregameObjects();
	}

	private void RemoveOdd(Plasma plasma, int x, int y, int recursionLevel)
	{
		if (recursionLevel < 0)
			return;

		// -------- compare the TerrainTypeCode of four adjacent square ------//
		int center = TerrainRes.TerrainHeight(plasma.GetPoint(x, y), out _);
		int same = 0;
		int diff = 0;
		int diffTerrain = -1;
		int diffHeight = 0;
		int sameX = 0;
		int sameY = 0;

		// ------- compare north square -------//
		if (y > 0)
		{
			if (center == TerrainRes.TerrainHeight(plasma.GetPoint(x, y - 1), out _))
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
					diffHeight = plasma.GetPoint(x, y - 1);
					diffTerrain = TerrainRes.TerrainHeight(diffHeight, out _);

				}
				else
				{
					// three terrain types are close, don't change anything
					if (diffTerrain != TerrainRes.TerrainHeight(plasma.GetPoint(x, y - 1), out _))
						return;
				}
			}
		}

		// ------- compare south square -------//
		if (y < plasma.MaxY)
		{
			if (center == TerrainRes.TerrainHeight(plasma.GetPoint(x, y + 1), out _))
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
					diffHeight = plasma.GetPoint(x, y + 1);
					diffTerrain = TerrainRes.TerrainHeight(diffHeight, out _);
				}
				else
				{
					// three terrain types are close, don't change anything
					if (diffTerrain != TerrainRes.TerrainHeight(plasma.GetPoint(x, y + 1), out _))
						return;
				}
			}
		}

		// ------- compare west square -------//
		if (x > 0)
		{
			if (center == TerrainRes.TerrainHeight(plasma.GetPoint(x - 1, y), out _))
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
					diffHeight = plasma.GetPoint(x - 1, y);
					diffTerrain = TerrainRes.TerrainHeight(diffHeight, out _);
				}
				else
				{
					// three terrain types are close, don't change anything
					if (diffTerrain != TerrainRes.TerrainHeight(plasma.GetPoint(x - 1, y), out _))
						return;
				}
			}
		}

		// ------- compare east square -------//
		if (x < plasma.MaxX)
		{
			if (center == TerrainRes.TerrainHeight(plasma.GetPoint(x + 1, y), out _))
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
					diffHeight = plasma.GetPoint(x + 1, y);
					diffTerrain = TerrainRes.TerrainHeight(diffHeight, out _);
				}
				else
				{
					// three terrain types are close, don't change anything
					if (diffTerrain != TerrainRes.TerrainHeight(plasma.GetPoint(x + 1, y), out _))
						return;
				}
			}
		}

		if (same <= 1 && diff >= 2)
		{
			// flatten
			plasma.SetPoint(x, y, diffHeight);

			// propagate to next square
			if (same == 1)
			{
				RemoveOdd(plasma, sameX, sameY, recursionLevel - 1);
			}
		}
	}

	private void SetTeraId(Plasma plasma)
	{
		for (int y = 0; y < GameConstants.MapSize; y++)
		{
			for (int x = 0; x < GameConstants.MapSize; x++)
			{
				int nwType = TerrainRes.TerrainHeight(plasma.GetPoint(x, y), out int nwSubType);
				int neType = TerrainRes.TerrainHeight(plasma.GetPoint(x + 1, y), out int neSubType);
				int swType = TerrainRes.TerrainHeight(plasma.GetPoint(x, y + 1), out int swSubType);
				int seType = TerrainRes.TerrainHeight(plasma.GetPoint(x + 1, y + 1), out int seSubType);

				if ((World.GetLoc(x, y).TerrainId = TerrainRes.Scan(nwType, nwSubType, neType, neSubType,
					    swType, swSubType, seType, seSubType, 0, 1, 0)) == 0)
				{
				}
			}
		}
	}

	private void SubstitutePattern()
	{
		const int RESULT_ARRAY_SIZE = 20;
		TerrainSubInfo[] candSub = new TerrainSubInfo[RESULT_ARRAY_SIZE];

		for (int y = 0; y < GameConstants.MapSize; y++)
		{
			for (int x = 0; x < GameConstants.MapSize; x++)
			{
				int subFound = TerrainRes.SearchPattern(TerrainRes[World.GetLoc(x, y).TerrainId].PatternId, candSub, RESULT_ARRAY_SIZE);
				for (int i = 0; i < subFound; i++)
				{
					int tx = x, ty = y;
					bool flag = true;

					// ----- test if a substitution matches
					for (TerrainSubInfo terrainSubInfo = candSub[i]; terrainSubInfo != null; terrainSubInfo = terrainSubInfo.NextStep)
					{
						if (tx < 0 || tx >= GameConstants.MapSize || ty < 0 || ty >= GameConstants.MapSize ||
						    TerrainRes[World.GetLoc(tx, ty).TerrainId].PatternId != terrainSubInfo.OldPatternId)
						{
							flag = false;
							break;
						}

						// ----- update tx, ty according to PostMove -----//
						switch (terrainSubInfo.PostMove)
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
						for (TerrainSubInfo terrainSubInfo = candSub[i]; terrainSubInfo != null; terrainSubInfo = terrainSubInfo.NextStep)
						{
							TerrainInfo oldTerrain = TerrainRes[World.GetLoc(tx, ty).TerrainId];
							int terrainId = TerrainRes.Scan(oldTerrain.AverageType, oldTerrain.SecondaryType + terrainSubInfo.SecAdj,
								terrainSubInfo.NewPatternId, 0, 1, 0);
							World.GetLoc(tx, ty).TerrainId = terrainId;

							// ----- update tx, ty according to PostMove -----//
							switch (terrainSubInfo.PostMove)
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

	private void SetLocFlags()
	{
		//----- set power off of the map edges -----//

		for (int locX = 0; locX < GameConstants.MapSize; locX++) // set the top and bottom edges
		{
			World.GetLoc(locX, 0).SetPowerOff();
			World.GetLoc(locX, GameConstants.MapSize - 1).SetPowerOff();
		}

		for (int locY = 0; locY < GameConstants.MapSize; locY++) // set the left and right edges
		{
			World.GetLoc(0, locY).SetPowerOff();
			World.GetLoc(GameConstants.MapSize - 1, locY).SetPowerOff();
		}

		//-----------------------------------------//

		for (int locY = 0; locY < GameConstants.MapSize; locY++)
		{
			for (int locX = 0; locX < GameConstants.MapSize; locX++)
			{
				Location location = World.GetLoc(locX, locY);
				if (Config.ExploreWholeMap)
					location.ExploredOn();
				else
					location.ExploredOff();

				TerrainInfo terrainInfo = TerrainRes[location.TerrainId];
				if (terrainInfo.IsCoast())
				{
					location.SetCoast();
					if (terrainInfo.AverageType != TerrainTypeCode.TERRAIN_OCEAN)
						location.SetPowerOff();
					else
						SetSurroundPowerOff(locX, locY);
				}

				location.ResetWalkable();
			}
		}
	}

	private void SetSurroundPowerOff(int locX, int locY)
	{
		if (locX > 0) // west
			World.GetLoc(locX - 1, locY).SetPowerOff();

		if (locX < GameConstants.MapSize - 1) // east
			World.GetLoc(locX + 1, locY).SetPowerOff();

		if (locY > 0) // north
			World.GetLoc(locX, locY - 1).SetPowerOff();

		if (locY < GameConstants.MapSize - 1) // south
			World.GetLoc(locX, locY + 1).SetPowerOff();
	}

	private void GenerateHills(int terrainType)
	{
		// ------- scan each tile for an above-hill terrain tile -----//
		int x, y = 0;

		for (y = 0; y < GameConstants.MapSize; y++)
		{
			for (x = 0; x < GameConstants.MapSize; x++)
			{
				Location location = World.GetLoc(x, y);
				TerrainInfo terrainInfo = TerrainRes[location.TerrainId];
				int priTerrain = terrainInfo.AverageType;
				int secTerrain = terrainInfo.SecondaryType;
				int highTerrain = (priTerrain >= secTerrain ? priTerrain : secTerrain);
				int lowTerrain = (priTerrain >= secTerrain ? secTerrain : priTerrain);
				if (highTerrain >= terrainType)
				{
					// BUGHERE : ignore special or extra flag
					int patternId = terrainInfo.PatternId;
					if (lowTerrain >= terrainType)
					{
						// move this terrain one square north
						if (y > 0)
						{
							// if y is GameConstants.MapSize - 1, aboveLoc and locPtr looks the same
							// BUGHERE : repeat the same pattern below is a bug if patternId is not 0,9,10,13,14
							if (y == GameConstants.MapSize - 1)
								location.TerrainId = TerrainRes.Scan(priTerrain, secTerrain, patternId);
						}
					}
					else
					{
						int hillId = HillRes.Scan(patternId, HillRes.LOW_HILL_PRIORITY, 0, false);
						location.SetHill(hillId);
						location.SetFlammability(-100);
						location.SetPowerOff();
						SetSurroundPowerOff(x, y);
						if (y > 0)
						{
							Location aboveLoc = World.GetLoc(x, y - 1);
							aboveLoc.SetHill(HillRes.Locate(patternId, HillRes[hillId].SubPatternId, HillRes.HIGH_HILL_PRIORITY, 0));
							aboveLoc.SetFlammability(-100);
							aboveLoc.SetPowerOff();
							SetSurroundPowerOff(x, y - 1);
						}

						// set terrain type to pure teraType - 1
						location.TerrainId = TerrainRes.Scan(lowTerrain, lowTerrain, 0);
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

		for (y = 1; y < GameConstants.MapSize - 1; y++)
		{
			lastExit = 0;
			for (x = 0; x < GameConstants.MapSize - 2; x++, lastExit = lastExit > 0 ? lastExit - 1 : 0)
			{
				Location location = World.GetLoc(x, y);
				// three hill blocks on a row are pattern 11,15,19 or 23,
				// block above the second block is a walkable
				if (lastExit == 0)
				{
					if (World.GetLoc(x, y).HasHill() && World.GetLoc(x + 1, y).HasHill() && World.GetLoc(x + 2, y).HasHill())
					{
						HillBlockInfo h1 = HillRes[World.GetLoc(x, y).HillId1()];
						int h1p = h1.PatternId;
						HillBlockInfo h2 = HillRes[World.GetLoc(x + 1, y).HillId1()];
						int h2p = h2.PatternId;
						HillBlockInfo h3 = HillRes[World.GetLoc(x + 2, y).HillId1()];
						int h3p = h3.PatternId;
						if (h1.SpecialFlag == 0 &&
						    h1.Priority == HillRes.HIGH_HILL_PRIORITY && h1p != 0 && IsSouthExitPattern(h1p) &&
						    h2.Priority == HillRes.HIGH_HILL_PRIORITY && h2p != 0 && IsSouthExitPattern(h2p) &&
						    h3.Priority == HillRes.HIGH_HILL_PRIORITY && h3p != 0 && IsSouthExitPattern(h3p))
						{
							if (World.GetLoc(x + 1, y - 1).Walkable())
							{
								int terrainId;

								// change this square
								if (h1p == SOUTH_PATTERN3)
									h1p = SOUTH_PATTERN1;
								else if (h1p == SOUTH_PATTERN4)
									h1p = SOUTH_PATTERN2;
								int hillId = HillRes.Scan(h1p, HillRes.HIGH_HILL_PRIORITY, SOUTH_LEFT_SPECIAL, false);
								location.RemoveHill();
								location.SetHill(hillId);
								location.SetPowerOff();
								SetSurroundPowerOff(x, y);
								if ((terrainId = TerrainRes.Scan(terrainType - 1, terrainType - 1, 0, 0, 1, 0)) != 0)
									location.TerrainId = terrainId;

								// next row
								Location loc2 = World.GetLoc(x, y + 1);
								hillId = HillRes.Locate(h1p, HillRes[hillId].SubPatternId, HillRes.LOW_HILL_PRIORITY, SOUTH_LEFT_SPECIAL);
								if (loc2.HillId2() == 0)
								{
									// if the location has only one block, remove it
									// if the location has two block, the bottom one is replaced
									loc2.RemoveHill();
								}
								loc2.SetHill(hillId);
								loc2.SetPowerOff();
								SetSurroundPowerOff(x, y + 1);
								if ((terrainId = TerrainRes.Scan(terrainType - 1, terrainType - 1, 0, 0, 1, 0)) != 0)
									loc2.TerrainId = terrainId;

								// second square
								loc2 = World.GetLoc(x + 1, y);
								loc2.RemoveHill();
								loc2.ResetWalkable();
								if ((terrainId = TerrainRes.Scan(terrainType, (int)SubTerrainMask.BOTTOM_MASK,
									    terrainType, (int)SubTerrainMask.BOTTOM_MASK,
									    terrainType, (int)SubTerrainMask.BOTTOM_MASK,
									    terrainType, (int)SubTerrainMask.BOTTOM_MASK)) != 0)
									loc2.TerrainId = terrainId;

								// next row
								loc2 = World.GetLoc(x + 1, y + 1);
								loc2.RemoveHill();
								loc2.ResetWalkable();
								if ((terrainId = TerrainRes.Scan(terrainType, terrainType - 1, SOUTH_PATTERN2, 0, 1, SOUTH_CENTRE_SPECIAL)) != 0)
									loc2.TerrainId = terrainId;

								// prev row
								// loc2 = World.GetLoc(x + 1, y - 1);
								// if ((terrainId = TerrainRes.Scan(terrainType, terrainType - 1, SOUTH_PATTERN2, 0, 1, SOUTH_CENTRE_SPECIAL)) != 0)
								//	loc2.TerrainId = terrainId;

								// third square
								loc2 = World.GetLoc(x + 2, y);
								if (h3p == SOUTH_PATTERN4)
									h3p = SOUTH_PATTERN1;
								if (h3p == SOUTH_PATTERN3)
									h3p = SOUTH_PATTERN2;
								hillId = HillRes.Scan(h3p, HillRes.HIGH_HILL_PRIORITY, SOUTH_RIGHT_SPECIAL, false);
								loc2.RemoveHill();
								loc2.SetHill(hillId);
								loc2.SetPowerOff();
								SetSurroundPowerOff(x + 2, y);
								if ((terrainId = TerrainRes.Scan(terrainType - 1, terrainType - 1, 0, 0, 1, 0)) != 0)
									loc2.TerrainId = terrainId;

								// next row
								loc2 = World.GetLoc(x + 2, y + 1);
								hillId = HillRes.Locate(h3p, HillRes[hillId].SubPatternId, HillRes.LOW_HILL_PRIORITY, SOUTH_RIGHT_SPECIAL);
								if (loc2.HillId2() == 0)
								{
									// if the location has only one block, remove it
									// if the location has two block, the bottom one is replaced
									loc2.RemoveHill();
								}
								loc2.SetHill(hillId);
								loc2.SetPowerOff();
								SetSurroundPowerOff(x + 2, y + 1);
								if ((terrainId = TerrainRes.Scan(terrainType - 1, terrainType - 1, 0, 0, 1, 0)) != 0)
									loc2.TerrainId = terrainId;

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
				Location location = World.GetLoc(x, y);
				// three hill blocks on a row are pattern 12,16,20 or 24,
				// block below the second block is a walkable
				if (lastExit == 0)
				{
					if (World.GetLoc(x, y).HasHill() && World.GetLoc(x + 1, y).HasHill() && World.GetLoc(x + 2, y).HasHill())
					{
						HillBlockInfo h1 = HillRes[World.GetLoc(x, y).HillId1()];
						int h1p = h1.PatternId;
						HillBlockInfo h2 = HillRes[World.GetLoc(x + 1, y).HillId1()];
						int h2p = h2.PatternId;
						HillBlockInfo h3 = HillRes[World.GetLoc(x + 2, y).HillId1()];
						int h3p = h3.PatternId;
						if (h1.SpecialFlag == 0 &&
						    h1.Priority == HillRes.HIGH_HILL_PRIORITY && h1p != 0 && IsNorthExitPattern(h1p) &&
						    h2.Priority == HillRes.HIGH_HILL_PRIORITY && h2p != 0 && IsNorthExitPattern(h2p) &&
						    h3.Priority == HillRes.HIGH_HILL_PRIORITY && h3p != 0 && IsNorthExitPattern(h3p))
						{
							if (World.GetLoc(x + 1, y + 1).Walkable())
							{
								int terrainId;

								// change this square
								if (h1p == NORTH_PATTERN4)
									h1p = NORTH_PATTERN1;
								else if (h1p == NORTH_PATTERN3)
									h1p = NORTH_PATTERN2;
								int hillId = HillRes.Scan(h1p, HillRes.HIGH_HILL_PRIORITY, NORTH_LEFT_SPECIAL, false);
								location.RemoveHill();
								location.SetHill(hillId);
								location.SetPowerOff();
								SetSurroundPowerOff(x, y);
								if ((terrainId = TerrainRes.Scan(terrainType - 1, terrainType - 1, 0, 0, 1, 0)) != 0)
									location.TerrainId = terrainId;

								// second square
								Location loc2 = World.GetLoc(x + 1, y);
								loc2.RemoveHill();
								loc2.ResetWalkable();
								if ((terrainId = TerrainRes.Scan(terrainType, terrainType - 1, NORTH_PATTERN2, 0, 1, NORTH_CENTRE_SPECIAL)) != 0)
									loc2.TerrainId = terrainId;

								// next row
								//loc2 = World.GetLoc(x + 1, y + 1);
								//if ((terrainId = TerrainRes.Scan(terrainType, terrainType - 1, NORTH_PATTERN2, 0, 1, NORTH_CENTRE_SPECIAL)) != 0)
								//	loc2.TerrainId = terrainId;

								// third square
								loc2 = World.GetLoc(x + 2, y);
								if (h3p == NORTH_PATTERN3)
									h3p = NORTH_PATTERN1;
								if (h3p == NORTH_PATTERN4)
									h3p = NORTH_PATTERN2;
								hillId = HillRes.Scan(h3p, HillRes.HIGH_HILL_PRIORITY, NORTH_RIGHT_SPECIAL, false);
								loc2.RemoveHill();
								loc2.SetHill(hillId);
								loc2.SetPowerOff();
								SetSurroundPowerOff(x + 2, y);
								if ((terrainId = TerrainRes.Scan(terrainType - 1, terrainType - 1, 0, 0, 1, 0)) != 0)
									loc2.TerrainId = terrainId;

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
				Location location = World.GetLoc(x, y);
				// three hill blocks on a row are pattern 9, 13, 17, 21
				// block above the second block is a walkable
				if (lastExit == 0)
				{
					if (World.GetLoc(x, y).HasHill() && World.GetLoc(x, y + 1).HasHill() &&
					    World.GetLoc(x, y + 2).HasHill() && World.GetLoc(x, y + 3).HasHill())
					{
						HillBlockInfo h1 = HillRes[World.GetLoc(x, y).HillId1()];
						int h1p = h1.PatternId;
						HillBlockInfo h2 = HillRes[World.GetLoc(x, y + 2).HillId1()];
						int h2p = h2.PatternId;
						HillBlockInfo h3 = HillRes[World.GetLoc(x, y + 3).HillId1()];
						int h3p = h3.PatternId;
						if (h1.SpecialFlag == 0 &&
						    h1.Priority == HillRes.HIGH_HILL_PRIORITY && h1p != 0 && IsWestExitPattern(h1p) &&
						    h2.Priority == HillRes.HIGH_HILL_PRIORITY && h2p != 0 && IsWestExitPattern(h2p) &&
						    h3.Priority == HillRes.HIGH_HILL_PRIORITY && h3p != 0 && IsWestExitPattern(h3p))
						{
							if ((h3p == WEST_PATTERN1 || h3p == WEST_PATTERN4) && World.GetLoc(x + 1, y + 2).Walkable())
							{
								int terrainId;

								// change this square
								if (h1p == WEST_PATTERN3)
									h1p = WEST_PATTERN1;
								else if (h1p == WEST_PATTERN4)
									h1p = WEST_PATTERN2;
								int hillId = HillRes.Scan(h1p, HillRes.HIGH_HILL_PRIORITY, WEST_TOP_SPECIAL, false);
								int hillId2 = location.HillId2();
								location.RemoveHill();
								location.SetHill(hillId);
								location.SetPowerOff();
								SetSurroundPowerOff(x, y);
								if (hillId2 != 0)
									location.SetHill(hillId2);

								// next row
								Location loc2 = World.GetLoc(x, y + 1);
								hillId = HillRes.Locate(h1p, HillRes[hillId].SubPatternId, HillRes.LOW_HILL_PRIORITY, WEST_TOP_SPECIAL);
								loc2.RemoveHill();
								loc2.SetHill(hillId);
								loc2.SetPowerOff();
								SetSurroundPowerOff(x, y + 1);
								if ((terrainId = TerrainRes.Scan(terrainType - 1, terrainType - 1, 0, 0, 1, 0)) != 0)
									loc2.TerrainId = terrainId;

								// third row
								loc2 = World.GetLoc(x, y + 2);
								loc2.RemoveHill();
								loc2.ResetWalkable();
								if ((terrainId = TerrainRes.Scan(terrainType, terrainType - 1, WEST_PATTERN2, 0, 1, WEST_CENTRE_SPECIAL)) != 0)
									loc2.TerrainId = terrainId;

								// next column
								//loc2 = World.GetLoc(x+1, y+2);
								//if ((terrainId = TerrainRes.Scan(terrainType, terrainType - 1, WEST_PATTERN2, 0, 1, WEST_CENTRE_SPECIAL )) != 0)
								//	loc2.TerrainId = terrainId;

								// fourth row
								loc2 = World.GetLoc(x, y + 3);
								if (h3p == WEST_PATTERN4)
									h3p = WEST_PATTERN1;
								if (h3p == WEST_PATTERN3)
									h3p = WEST_PATTERN2;
								hillId = HillRes.Scan(h3p, HillRes.HIGH_HILL_PRIORITY, WEST_BOTTOM_SPECIAL, false);
								loc2.RemoveHill();
								loc2.SetHill(hillId);
								loc2.SetPowerOff();
								SetSurroundPowerOff(x, y + 3);
								if ((terrainId = TerrainRes.Scan(terrainType - 1, terrainType - 1, 0, 0, 1, 0)) != 0)
									loc2.TerrainId = terrainId;

								// next row
								loc2 = World.GetLoc(x, y + 4);
								hillId = HillRes.Locate(h3p, HillRes[hillId].SubPatternId, HillRes.LOW_HILL_PRIORITY, WEST_BOTTOM_SPECIAL);
								loc2.SetHill(hillId);
								loc2.SetPowerOff();
								SetSurroundPowerOff(x, y + 4);
								if ((terrainId = TerrainRes.Scan(terrainType - 1, terrainType - 1, 0, 0, 1, 0)) != 0)
									loc2.TerrainId = terrainId;
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
				Location location = World.GetLoc(x, y);
				// three hill blocks on a row are pattern 9, 13, 17, 21
				// block above the second block is a walkable
				if (lastExit == 0)
				{
					if (World.GetLoc(x, y).HasHill() && World.GetLoc(x, y + 1).HasHill() &&
					    World.GetLoc(x, y + 2).HasHill() && World.GetLoc(x, y + 3).HasHill())
					{
						HillBlockInfo h1 = HillRes[World.GetLoc(x, y).HillId1()];
						int h1p = h1.PatternId;
						HillBlockInfo h2 = HillRes[World.GetLoc(x, y + 2).HillId1()];
						int h2p = h2.PatternId;
						HillBlockInfo h3 = HillRes[World.GetLoc(x, y + 3).HillId1()];
						int h3p = h3.PatternId;
						if (h1.SpecialFlag == 0 &&
						    h1.Priority == HillRes.HIGH_HILL_PRIORITY && h1p != 0 && IsEastExitPattern(h1p) &&
						    h2.Priority == HillRes.HIGH_HILL_PRIORITY && h2p != 0 && IsEastExitPattern(h2p) &&
						    h3.Priority == HillRes.HIGH_HILL_PRIORITY && h3p != 0 && IsEastExitPattern(h3p))
						{
							if ((h3p == EAST_PATTERN1 || h3p == EAST_PATTERN4) && World.GetLoc(x - 1, y + 2).Walkable())
							{
								int terrainId;

								// change this square
								if (h1p == EAST_PATTERN3)
									h1p = EAST_PATTERN1;
								else if (h1p == EAST_PATTERN4)
									h1p = EAST_PATTERN2;
								int hillId = HillRes.Scan(h1p, HillRes.HIGH_HILL_PRIORITY, EAST_TOP_SPECIAL, false);
								int hillId2 = location.HillId2();
								location.RemoveHill();
								location.SetHill(hillId);
								if (hillId2 != 0)
									location.SetHill(hillId2);
								location.SetPowerOff();
								SetSurroundPowerOff(x, y);

								// next row
								Location loc2 = World.GetLoc(x, y + 1);
								hillId = HillRes.Locate(h1p, HillRes[hillId].SubPatternId, HillRes.LOW_HILL_PRIORITY, EAST_TOP_SPECIAL);
								loc2.RemoveHill();
								loc2.SetHill(hillId);
								loc2.SetPowerOff();
								SetSurroundPowerOff(x, y + 1);
								if ((terrainId = TerrainRes.Scan(terrainType - 1, terrainType - 1, 0, 0, 1, 0)) != 0)
									loc2.TerrainId = terrainId;

								// third row
								loc2 = World.GetLoc(x, y + 2);
								loc2.RemoveHill();
								loc2.ResetWalkable();
								if ((terrainId = TerrainRes.Scan(terrainType, terrainType - 1, EAST_PATTERN2, 0, 1, EAST_CENTRE_SPECIAL)) != 0)
									loc2.TerrainId = terrainId;

								// next column
								//loc2 = World.GetLoc(x - 1, y + 2);
								//if ((terrainId = TerrainRes.Scan(terrainType, terrainType - 1, EAST_PATTERN2, 0, 1, EAST_CENTRE_SPECIAL)) != 0)
								//	loc2.TerrainId = terrainId;

								// fourth row
								loc2 = World.GetLoc(x, y + 3);
								if (h3p == EAST_PATTERN4)
									h3p = EAST_PATTERN1;
								if (h3p == EAST_PATTERN3)
									h3p = EAST_PATTERN2;
								hillId = HillRes.Scan(h3p, HillRes.HIGH_HILL_PRIORITY, EAST_BOTTOM_SPECIAL, false);
								loc2.RemoveHill();
								loc2.SetHill(hillId);
								loc2.SetPowerOff();
								SetSurroundPowerOff(x, y + 3);
								if ((terrainId = TerrainRes.Scan(terrainType - 1, terrainType - 1, 0, 0, 1, 0)) != 0)
									loc2.TerrainId = terrainId;

								// next row
								loc2 = World.GetLoc(x, y + 4);
								hillId = HillRes.Locate(h3p, HillRes[hillId].SubPatternId, HillRes.LOW_HILL_PRIORITY, EAST_BOTTOM_SPECIAL);
								loc2.SetHill(hillId);
								loc2.SetPowerOff();
								SetSurroundPowerOff(x, y + 4);
								if ((terrainId = TerrainRes.Scan(terrainType - 1, terrainType - 1, 0, 0, 1, 0)) != 0)
									loc2.TerrainId = terrainId;
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
		int regionId = 0;
		for (int locY = 0; locY < GameConstants.MapSize; locY++)
		{
			for (int locX = 0; locX < GameConstants.MapSize; locX++)
			{
				Location location = World.GetLoc(locX, locY);
				if (location.RegionId == 0 && location.RegionType() != RegionType.IMPASSABLE)
				{
					regionId++;
					FillRegion(locX, locY, regionId, location.RegionType());
				}
			}
		}

		RegionArray.Init(regionId);

		// ------ update adjacency information and region area ------//

		regionId = 0;
		for (int locY = 0; locY < GameConstants.MapSize; locY++)
		{
			for (int locX = 0; locX < GameConstants.MapSize; locX++)
			{
				Location location = World.GetLoc(locX, locY);
				int thisRegionId = location.RegionId;
				if (thisRegionId > 0)
				{
					RegionArray.IncSize(thisRegionId);
				}

				if (thisRegionId > regionId)
				{
					if (thisRegionId == regionId + 1)
						regionId++;
					RegionArray.SetRegionType(thisRegionId, location.RegionType());
				}

				int adjRegionId;
				if (locY > 0)
				{
					if (locX > 0 && (adjRegionId = World.GetLoc(locX - 1, locY - 1).RegionId) < thisRegionId)
						RegionArray.SetAdjacent(thisRegionId, adjRegionId);
					if ((adjRegionId = World.GetLoc(locX, locY - 1).RegionId) < thisRegionId)
						RegionArray.SetAdjacent(thisRegionId, adjRegionId);
					if (locX < GameConstants.MapSize - 1 && (adjRegionId = World.GetLoc(locX + 1, locY - 1).RegionId) < thisRegionId)
						RegionArray.SetAdjacent(thisRegionId, adjRegionId);
				}

				if (locX > 0 && (adjRegionId = World.GetLoc(locX - 1, locY).RegionId) < thisRegionId)
					RegionArray.SetAdjacent(thisRegionId, adjRegionId);
				if (locX < GameConstants.MapSize - 1 && (adjRegionId = World.GetLoc(locX + 1, locY).RegionId) < thisRegionId)
					RegionArray.SetAdjacent(thisRegionId, adjRegionId);

				if (locY < GameConstants.MapSize - 1)
				{
					if (locX > 0 && (adjRegionId = World.GetLoc(locX - 1, locY + 1).RegionId) < thisRegionId)
						RegionArray.SetAdjacent(thisRegionId, adjRegionId);
					if ((adjRegionId = World.GetLoc(locX, locY + 1).RegionId) < thisRegionId)
						RegionArray.SetAdjacent(thisRegionId, adjRegionId);
					if (locX < GameConstants.MapSize - 1 && (adjRegionId = World.GetLoc(locX + 1, locY + 1).RegionId) < thisRegionId)
						RegionArray.SetAdjacent(thisRegionId, adjRegionId);
				}
			}
		}

		RegionArray.InitRegionStat();
	}

	private void FillRegion(int locX, int locY, int regionId, RegionType regionType)
	{
		int left, right;

		// extent x to left and right
		for (left = locX;
		     left >= 0 && World.GetLoc(left, locY).RegionId == 0 && World.GetLoc(left, locY).RegionType() == regionType;
		     left--)
		{
			World.GetLoc(left, locY).RegionId = regionId;
		}

		left++;

		for (right = locX + 1;
		     right < GameConstants.MapSize && World.GetLoc(right, locY).RegionId == 0 &&
		     World.GetLoc(right, locY).RegionType() == regionType;
		     right++)
		{
			World.GetLoc(right, locY).RegionId = regionId;
		}

		right--;

		// ------- scan line below ---------//
		locY++;
		if (locY < GameConstants.MapSize)
		{
			for (locX = left > 0 ? left - 1 : 0; locX <= right + 1 && locX < GameConstants.MapSize; ++locX)
			{
				if (World.GetLoc(locX, locY).RegionId == 0 && World.GetLoc(locX, locY).RegionType() == regionType)
				{
					FillRegion(locX, locY, regionId, regionType);
				}
			}
		}

		// ------- scan line above -------- //
		locY -= 2;
		if (locY >= 0)
		{
			for (locX = left > 0 ? left - 1 : 0; locX <= right + 1 && locX < GameConstants.MapSize; ++locX)
			{
				if (World.GetLoc(locX, locY).RegionId == 0 && World.GetLoc(locX, locY).RegionType() == regionType)
				{
					FillRegion(locX, locY, regionId, regionType);
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
				int x = (GAP + SMALL_ROCK_SIZE) + Misc.Random(GameConstants.MapSize - LARGE_ROCK_SIZE + 1 - 2 * (GAP + SMALL_ROCK_SIZE));
				int y = (GAP + SMALL_ROCK_SIZE) + Misc.Random(GameConstants.MapSize - LARGE_ROCK_SIZE + 1 - 2 * (GAP + SMALL_ROCK_SIZE));
				int x2 = x + LARGE_ROCK_SIZE - 1;
				int y2 = y + LARGE_ROCK_SIZE - 1;

				if (CanAddDirt(x, y, x2, y2))
				{
					int rockId = RockRes.Search("DE", 1, LARGE_ROCK_SIZE, 1, LARGE_ROCK_SIZE,
						-1, false, TerrainRes[World.GetLoc(x, y).TerrainId].AverageType);
					if (rockId == 0)
						continue;

					RockInfo rockInfo = RockRes.GetRockInfo(rockId);
					x2 = x + rockInfo.LocWidth - 1;
					y2 = y + rockInfo.LocHeight - 1;
					if (rockInfo.IsTerrainValid(TerrainRes[World.GetLoc(x2, y).TerrainId].AverageType) &&
					    rockInfo.IsTerrainValid(TerrainRes[World.GetLoc(x, y2).TerrainId].AverageType) &&
					    rockInfo.IsTerrainValid(TerrainRes[World.GetLoc(x2, y2).TerrainId].AverageType))
					{
						AddDirt(rockId, x, y);

						// add other smaller rock
						for (int subTrial = Misc.Random(14); subTrial > 0; --subTrial)
						{
							// sx from x-SMALL_ROCK_SIZE to x+4-1+SMALL_ROCK_SIZE
							int sx = x - SMALL_ROCK_SIZE - GAP + Misc.Random(LARGE_ROCK_SIZE + SMALL_ROCK_SIZE + 2 * GAP);
							int sy = y - SMALL_ROCK_SIZE - GAP + Misc.Random(LARGE_ROCK_SIZE + SMALL_ROCK_SIZE + 2 * GAP);
							int sx2 = sx + SMALL_ROCK_SIZE - 1;
							int sy2 = sy + SMALL_ROCK_SIZE - 1;

							if (CanAddDirt(sx, sy, sx2, sy2))
							{
								int rock2Id = RockRes.Search("DE", 1, SMALL_ROCK_SIZE, 1, SMALL_ROCK_SIZE,
									-1, false, TerrainRes[World.GetLoc(sx, sy).TerrainId].AverageType);
								if (rock2Id == 0)
									continue;

								RockInfo rock2Info = RockRes.GetRockInfo(rock2Id);
								sx2 = sx + rock2Info.LocWidth - 1;
								sy2 = sy + rock2Info.LocHeight - 1;
								if (rock2Info.IsTerrainValid(TerrainRes[World.GetLoc(sx2, sy).TerrainId].AverageType) &&
								    rock2Info.IsTerrainValid(TerrainRes[World.GetLoc(sx, sy2).TerrainId].AverageType) &&
								    rock2Info.IsTerrainValid(TerrainRes[World.GetLoc(sx2, sy2).TerrainId].AverageType))
								{
									AddDirt(rock2Id, sx, sy);
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

				if (CanAddDirt(x, y, x2, y2))
				{
					int rockId = RockRes.Search("DE", SMALL_ROCK_SIZE + 1, HUGE_ROCK_SIZE, SMALL_ROCK_SIZE + 1, HUGE_ROCK_SIZE,
						-1, false, TerrainRes[World.GetLoc(x, y).TerrainId].AverageType);
					if (rockId == 0)
						continue;

					RockInfo rockInfo = RockRes.GetRockInfo(rockId);
					x2 = x + rockInfo.LocWidth - 1;
					y2 = y + rockInfo.LocHeight - 1;
					if (rockInfo.IsTerrainValid(TerrainRes[World.GetLoc(x2, y).TerrainId].AverageType) &&
					    rockInfo.IsTerrainValid(TerrainRes[World.GetLoc(x, y2).TerrainId].AverageType) &&
					    rockInfo.IsTerrainValid(TerrainRes[World.GetLoc(x2, y2).TerrainId].AverageType))
					{
						AddDirt(rockId, x, y);
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

				if (CanAddDirt(x, y, x2, y2))
				{
					int rockId = RockRes.Search("DE", 1, SMALL_ROCK_SIZE, 1, SMALL_ROCK_SIZE,
						-1, false, TerrainRes[World.GetLoc(x, y).TerrainId].AverageType);
					if (rockId == 0)
						continue;

					RockInfo rockInfo = RockRes.GetRockInfo(rockId);
					x2 = x + rockInfo.LocWidth - 1;
					y2 = y + rockInfo.LocHeight - 1;
					if (rockInfo.IsTerrainValid(TerrainRes[World.GetLoc(x2, y).TerrainId].AverageType) &&
					    rockInfo.IsTerrainValid(TerrainRes[World.GetLoc(x, y2).TerrainId].AverageType) &&
					    rockInfo.IsTerrainValid(TerrainRes[World.GetLoc(x2, y2).TerrainId].AverageType))
					{
						AddDirt(rockId, x, y);
						nSmall--;
					}
				}
			}
		}
	}

	private bool CanAddDirt(int x1, int y1, int x2, int y2)
	{
		for (int y = y1; y <= y2; y++)
		{
			for (int x = x1; x <= x2; x++)
			{
				if (!World.GetLoc(x, y).CanAddDirt())
					return false;
			}
		}

		return true;
	}

	private void AddDirt(int dirtId, int x1, int y1)
	{
		// ------- get the delay remain count for the first frame -----//
		Rock newDirt = new Rock(dirtId, x1, y1);
		int dirtArrayId = DirtArray.Add(newDirt);
		RockInfo dirtInfo = RockRes.GetRockInfo(dirtId);

		for (int dy = 0; dy < dirtInfo.LocHeight && y1 + dy < GameConstants.MapSize; dy++)
		{
			for (int dx = 0; dx < dirtInfo.LocWidth && x1 + dx < GameConstants.MapSize; dx++)
			{
				int dirtBlockId = RockRes.LocateBlock(dirtId, dx, dy);
				if (dirtBlockId != 0)
				{
					Location location = World.GetLoc(x1 + dx, y1 + dy);
					location.SetDirt(dirtArrayId, dirtInfo.RockType == RockInfo.DIRT_BLOCKING_TYPE);
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
				int x = (GAP + SMALL_ROCK_SIZE) + Misc.Random(GameConstants.MapSize - LARGE_ROCK_SIZE + 1 - 2 * (GAP + SMALL_ROCK_SIZE));
				int y = (GAP + SMALL_ROCK_SIZE) + Misc.Random(GameConstants.MapSize - LARGE_ROCK_SIZE + 1 - 2 * (GAP + SMALL_ROCK_SIZE));
				int x2 = x + LARGE_ROCK_SIZE - 1;
				int y2 = y + LARGE_ROCK_SIZE - 1;

				if (CanAddRock(x, y, x2, y2))
				{
					int rockId = RockRes.Search("R", 1, LARGE_ROCK_SIZE, 1, LARGE_ROCK_SIZE,
						-1, false, TerrainRes[World.GetLoc(x, y).TerrainId].AverageType);
					if (rockId == 0)
						continue;

					RockInfo rockInfo = RockRes.GetRockInfo(rockId);
					x2 = x + rockInfo.LocWidth - 1;
					y2 = y + rockInfo.LocHeight - 1;
					if (rockInfo.IsTerrainValid(TerrainRes[World.GetLoc(x2, y).TerrainId].AverageType) &&
					    rockInfo.IsTerrainValid(TerrainRes[World.GetLoc(x, y2).TerrainId].AverageType) &&
					    rockInfo.IsTerrainValid(TerrainRes[World.GetLoc(x2, y2).TerrainId].AverageType))
					{
						AddRock(rockId, x, y);

						// add other smaller rock
						for (int subTrial = Misc.Random(14); subTrial > 0; --subTrial)
						{
							// sx from x-SMALL_ROCK_SIZE to x+4-1+SMALL_ROCK_SIZE
							int sx = x - SMALL_ROCK_SIZE - GAP + Misc.Random(LARGE_ROCK_SIZE + SMALL_ROCK_SIZE + 2 * GAP);
							int sy = y - SMALL_ROCK_SIZE - GAP + Misc.Random(LARGE_ROCK_SIZE + SMALL_ROCK_SIZE + 2 * GAP);
							int sx2 = sx + SMALL_ROCK_SIZE - 1;
							int sy2 = sy + SMALL_ROCK_SIZE - 1;

							if (CanAddRock(sx, sy, sx2, sy2))
							{
								int rock2Id = RockRes.Search("R", 1, SMALL_ROCK_SIZE, 1, SMALL_ROCK_SIZE,
									-1, false, TerrainRes[World.GetLoc(sx, sy).TerrainId].AverageType);
								if (rock2Id == 0)
									continue;

								RockInfo rock2Info = RockRes.GetRockInfo(rock2Id);
								sx2 = sx + rock2Info.LocWidth - 1;
								sy2 = sy + rock2Info.LocHeight - 1;
								if (rock2Info.IsTerrainValid(TerrainRes[World.GetLoc(sx2, sy).TerrainId].AverageType) &&
								    rock2Info.IsTerrainValid(TerrainRes[World.GetLoc(sx, sy2).TerrainId].AverageType) &&
								    rock2Info.IsTerrainValid(TerrainRes[World.GetLoc(sx2, sy2).TerrainId].AverageType))
								{
									AddRock(rock2Id, sx, sy);
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

				if (CanAddRock(x, y, x2, y2))
				{
					int rockId = RockRes.Search("R", SMALL_ROCK_SIZE + 1, HUGE_ROCK_SIZE, SMALL_ROCK_SIZE + 1, HUGE_ROCK_SIZE,
						-1, false, TerrainRes[World.GetLoc(x, y).TerrainId].AverageType);
					if (rockId == 0)
						continue;

					RockInfo rockInfo = RockRes.GetRockInfo(rockId);
					x2 = x + rockInfo.LocWidth - 1;
					y2 = y + rockInfo.LocHeight - 1;
					if (rockInfo.IsTerrainValid(TerrainRes[World.GetLoc(x2, y).TerrainId].AverageType) &&
					    rockInfo.IsTerrainValid(TerrainRes[World.GetLoc(x, y2).TerrainId].AverageType) &&
					    rockInfo.IsTerrainValid(TerrainRes[World.GetLoc(x2, y2).TerrainId].AverageType))
					{
						AddRock(rockId, x, y);
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

				if (CanAddRock(x, y, x2, y2))
				{
					int rockId = RockRes.Search("R", 1, SMALL_ROCK_SIZE, 1, SMALL_ROCK_SIZE,
						-1, false, TerrainRes[World.GetLoc(x, y).TerrainId].AverageType);
					if (rockId == 0)
						continue;

					RockInfo rockInfo = RockRes.GetRockInfo(rockId);
					x2 = x + rockInfo.LocWidth - 1;
					y2 = y + rockInfo.LocHeight - 1;
					if (rockInfo.IsTerrainValid(TerrainRes[World.GetLoc(x2, y).TerrainId].AverageType) &&
					    rockInfo.IsTerrainValid(TerrainRes[World.GetLoc(x, y2).TerrainId].AverageType) &&
					    rockInfo.IsTerrainValid(TerrainRes[World.GetLoc(x2, y2).TerrainId].AverageType))
					{
						AddRock(rockId, x, y);
						nSmall--;
					}
				}
			}
		}
	}

	private bool CanAddRock(int x1, int y1, int x2, int y2)
	{
		for (int y = y1; y <= y2; y++)
		{
			for (int x = x1; x <= x2; x++)
			{
				if (!World.GetLoc(x, y).CanAddRock(3))
					return false;
			}
		}

		return true;
	}

	private void AddRock(int rockId, int x1, int y1)
	{
		// ------- get the delay remain count for the first frame -----//
		Rock newRock = new Rock(rockId, x1, y1);
		int rockArrayId = RockArray.Add(newRock);
		RockInfo rockInfo = RockRes.GetRockInfo(rockId);

		for (int dy = 0; dy < rockInfo.LocHeight && y1 + dy < GameConstants.MapSize; dy++)
		{
			for (int dx = 0; dx < rockInfo.LocWidth && x1 + dx < GameConstants.MapSize; dx++)
			{
				int rockBlockId = RockRes.LocateBlock(rockId, dx, dy);
				if (rockBlockId != 0)
				{
					Location location = World.GetLoc(x1 + dx, y1 + dy);
					location.SetRock(rockArrayId);
					location.SetPowerOff();
					SetSurroundPowerOff(x1, y1);
				}
			}
		}
	}

	private void SetHarborBit()
	{
		for (int y = 0; y < GameConstants.MapSize - 2; y++)
		{
			for (int x = 0; x < GameConstants.MapSize - 2; x++)
			{
				if (World.CanBuildFirm(x, y, Firm.FIRM_HARBOR) != 0)
				{
					World.GetLoc(x, y).SetHarborBit();
				}
			}
		}
	}

	private void CreatePlayerNation()
	{
		// if Config.RaceId == 0, select a random race, but don't call Misc.Random()
		int raceId = Config.PlayerRaceId != 0 ? Config.PlayerRaceId : (int)(DateTime.Now.Ticks % GameConstants.MAX_RACE) + 1;
		Nation nation = NationArray.NewNation(NationBase.NATION_OWN, raceId, Config.PlayerColor);
		NationArray.SetHumanName(nation.NationId, Config.PlayerName);
	}
	
	private void CreateAINations(int aiNationCount)
	{
		for (int i = 0; i < aiNationCount; i++)
		{
			int raceId = Config.RandomStartUp ? ConfigAdv.GetRandomRace() : NationArray.RandomUnusedRace();
			NationArray.NewNation(NationBase.NATION_AI, raceId, NationArray.RandomUnusedColor());
		}
	}

	private Town CreateTown(int nationId, int raceId)
	{
		if (!TownArray.ThinkTownLoc(GameConstants.MapSize * GameConstants.MapSize, out int xLoc, out int yLoc))
			return null;

		Town town = TownArray.AddTown(nationId, raceId, xLoc, yLoc);

		//--------- no. of mixed races ---------//

		if (nationId != 0)
		{
			int initPop;

			if (Config.RandomStartUp)
				initPop = 25 + Misc.Random(26); // 25 to 50
			else
				initPop = 40;

			town.InitPopulation(raceId, initPop, 100, false, true);
		}
		else
		{
			int mixedRaceCount = Misc.Random(3) + 1;

			int totalPop = 0;

			for (int i = 0; i < mixedRaceCount; i++)
			{
				if (totalPop >= GameConstants.MAX_TOWN_POPULATION)
					break;

				int townResistance = TownArray.IndependentTownResistance();

				int curPop;
				if (i == 0)
				{
					curPop = 15 / mixedRaceCount + Misc.Random(15 / mixedRaceCount);
					if (curPop >= GameConstants.MAX_TOWN_POPULATION)
						curPop = GameConstants.MAX_TOWN_POPULATION;

					town.InitPopulation(raceId, curPop, townResistance, false, true);
					totalPop += curPop;
				}
				else
				{
					curPop = 10 / mixedRaceCount + Misc.Random(10 / mixedRaceCount);
					if (curPop >= GameConstants.MAX_TOWN_POPULATION - totalPop)
						curPop = GameConstants.MAX_TOWN_POPULATION - totalPop;

					town.InitPopulation(ConfigAdv.GetRandomRace(), curPop, townResistance, false, true);
					totalPop += curPop;
				}
			}
		}

		town.AutoSetLayout();

		return town;
	}

	private Unit CreateUnit(Town town, int unitId, int rankId)
	{
		SpriteInfo spriteInfo = SpriteRes[UnitRes[unitId].SpriteId];

		int locX = town.LocX1;
		int locY = town.LocY1;

		if (!World.LocateSpace(ref locX, ref locY, locX + InternalConstants.TOWN_WIDTH - 1, locY + InternalConstants.TOWN_HEIGHT - 1,
			    spriteInfo.LocWidth, spriteInfo.LocHeight))
		{
			return null;
		}

		int unitLoyalty = 80 + Misc.Random(20);

		Unit unit = UnitArray.AddUnit(unitId, town.NationId, rankId, unitLoyalty, locX, locY);

		if (unit == null)
			return null;

		//----------- set skill -------------//

		switch (rankId)
		{
			case Unit.RANK_KING:
				unit.Skill.SkillId = Skill.SKILL_LEADING;
				unit.Skill.SkillLevel = 100;
				unit.SetCombatLevel(100);
				break;

			case Unit.RANK_GENERAL:
				unit.Skill.SkillId = Skill.SKILL_LEADING;
				unit.Skill.SkillLevel = 40 + Misc.Random(50); // 40 to 90
				unit.SetCombatLevel(30 + Misc.Random(70)); // 30 to 100
				break;

			case Unit.RANK_SOLDIER:
			{
				int skillId = Misc.Random(Skill.MAX_TRAINABLE_SKILL) + 1;
				bool spyFlag = false;

				if (skillId == Skill.SKILL_SPYING)
				{
					unit.SetCombatLevel(10 + Misc.Random(10));
					spyFlag = true;
				}
				else
				{
					unit.Skill.SkillId = skillId;
					unit.Skill.SkillLevel = 30 + Misc.Random(70);

					if (skillId == Skill.SKILL_LEADING)
						unit.SetCombatLevel(30 + Misc.Random(70));
					else
						unit.SetCombatLevel(10 + Misc.Random(10));

					if (Misc.Random(5) == 0)
						spyFlag = true;
				}

				if (spyFlag)
				{
					int spySkill = 20 + Misc.Random(80); // 20 to 100
					unit.SpyId = SpyArray.AddSpy(unit.SpriteId, spySkill).SpyId;
				}

				break;
			}
		}

		return unit;
	}

	private void CreateMonsterLair()
	{
		int monsterId = Misc.Random(MonsterRes.MonsterInfos.Length) + 1;
		
		if (String.IsNullOrEmpty(MonsterRes[monsterId].FirmBuildCode)) // this monster does not have a home
			return;

		//------- locate a home place for the monster group ------//

		FirmInfo firmInfo = FirmRes[Firm.FIRM_MONSTER];

		int locX = 0, locY = 0;
		int teraMask = UnitRes.MobileTypeToMask(UnitConstants.UNIT_LAND);

		// leave at least one location space around the building
		if (!World.LocateSpaceRandom(ref locX, ref locY, GameConstants.MapSize - 1, GameConstants.MapSize - 1,
			    firmInfo.LocWidth + 2, firmInfo.LocHeight + 2,
			    GameConstants.MapSize * GameConstants.MapSize, 0, true, teraMask))
		{
			return;
		}

		//------- don't place it too close to any towns or firms ------//

		foreach (Town town in TownArray)
		{
			if (Misc.RectsDistance(locX, locY, locX + firmInfo.LocWidth - 1, locY + firmInfo.LocHeight - 1,
				    town.LocX1, town.LocY1, town.LocX2, town.LocY2) < GameConstants.MIN_MONSTER_CIVILIAN_DISTANCE)
			{
				return;
			}
		}

		foreach (Firm firm in FirmArray)
		{
			if (Misc.RectsDistance(locX, locY, locX + firmInfo.LocWidth - 1, locY + firmInfo.LocHeight - 1,
				    firm.LocX1, firm.LocY1, firm.LocX2, firm.LocY2) < GameConstants.MIN_MONSTER_CIVILIAN_DISTANCE)
			{
				return;
			}
		}

		FirmArray.BuildMonsterLair(locX + 1, locY + 1, monsterId);
	}

	private void CreatePregameObjects()
	{
		//------- create nation and units --------//

		List<Nation> uninitializedNations = new List<Nation>();
		foreach (Nation nation in NationArray)
		{
			uninitializedNations.Add(nation);
		}

		foreach (Nation nation in NationArray)
		{
			Town town = CreateTown(nation.NationId, nation.RaceId);
			if (town == null)
				break;

			//TODO randomize camp location
			int firmId = FirmArray.BuildFirm(town.LocX1 + 5, town.LocY1, nation.NationId, Firm.FIRM_CAMP, RaceRes[nation.RaceId].Code);
			if (firmId == 0)
				break;

			FirmArray[firmId].CompleteConstruction();

			int unitType = RaceRes[nation.RaceId].BasicUnitType;

			Unit king = CreateUnit(town, unitType, Unit.RANK_KING);
			if (king == null)
				break;
			
			nation.SetKing(king.SpriteId, true);
			FirmArray[firmId].AssignOverseer(king.SpriteId);

			//----- create skilled units if config.random_start_up is 1 -----//

			if (Config.RandomStartUp)
			{
				// the less population the villager has the more mobile units will be created
				int createCount = (50 - town.Population) / 3;

				for (int i = 0; i < createCount; i++)
				{
					if (Misc.Random(2) == 0)
						unitType = RaceRes[nation.RaceId].BasicUnitType;
					else
						unitType = RaceRes[ConfigAdv.GetRandomRace()].BasicUnitType;

					int rankId;
					if (Misc.Random(3) == 0)
						rankId = Unit.RANK_GENERAL;
					else
						rankId = Unit.RANK_SOLDIER;

					if (CreateUnit(town, unitType, rankId) == null)
						break;
				}
			}

			//------ create mines near towns in the beginning -----//

			if (Config.RawResourceNearby && !nation.IsAI())
				SiteArray.CreateRawSite(town.TownId);

			//------ set ai base town -----//
			if (nation.IsAI())
				nation.update_ai_region();

			uninitializedNations.Remove(nation);
		}

		//--- if there is no space for creating new town/firm or unit, delete the unprocessed nations ---//

		foreach (Nation nation in uninitializedNations)
		{
			NationArray.DeleteNation(nation);
		}

		//------ create independent towns -------//

		int startUpIndependentTown = Config.IndependentTownCount;
		int startUpMonsterFirm = Config.MonsterLairCount;

		SiteArray.GenerateRawSite(Config.RawResourceCount);

		int maxLoopCount = startUpIndependentTown + startUpMonsterFirm;

		for (int j = 0, k = 1; j < maxLoopCount; j++, k++)
		{
			if (startUpIndependentTown > 0)
			{
				//------ create independent towns -------//
				int raceId = ConfigAdv.race_random_list[k % ConfigAdv.race_random_list_max];
				if (CreateTown(0, raceId) != null)
				{
					startUpIndependentTown--;
				}
				else
				{
					startUpIndependentTown = 0;
				}
			}

			if (startUpMonsterFirm > 0)
			{
				//------- create monsters --------//
				if (Config.MonsterType != Config.OPTION_MONSTER_NONE)
				{
					CreateMonsterLair();
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

		NationArray.UpdateStatistic();
	}
}