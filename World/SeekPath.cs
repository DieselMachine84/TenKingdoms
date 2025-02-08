using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class ResultNode
{
	public int node_x;
	public int node_y;

	public ResultNode(int nodeX, int nodeY)
	{
		node_x = nodeX;
		node_y = nodeY;
	}
}

public class SeekPath
{
	public const int PATH_WAIT = 0;
	public const int PATH_FOUND = 1;
	public const int PATH_IMPOSSIBLE = 2;

	public const int SEARCH_MODE_IN_A_GROUP = 1;
	public const int SEARCH_MODE_A_UNIT_IN_GROUP = 2;
	public const int SEARCH_MODE_TO_ATTACK = 3;
	//public const int SEARCH_MODE_REUSE = 4;
	public const int SEARCH_MODE_BLOCKING = 5;
	public const int SEARCH_MODE_TO_VEHICLE = 6;
	public const int SEARCH_MODE_TO_FIRM = 7;
	public const int SEARCH_MODE_TO_TOWN = 8;
	public const int SEARCH_MODE_TO_WALL_FOR_GROUP = 9;
	public const int SEARCH_MODE_TO_WALL_FOR_UNIT = 10;
	public const int SEARCH_MODE_ATTACK_UNIT_BY_RANGE = 11;
	public const int SEARCH_MODE_ATTACK_FIRM_BY_RANGE = 12;
	public const int SEARCH_MODE_ATTACK_TOWN_BY_RANGE = 13;
	public const int SEARCH_MODE_ATTACK_WALL_BY_RANGE = 14;
	public const int SEARCH_MODE_TO_LAND_FOR_SHIP = 15;
	public const int SEARCH_MODE_LAST = 16;
	public const int MAX_SEARCH_MODE_TYPE = SEARCH_MODE_LAST - 1;

	public const int SEARCH_SUB_MODE_NORMAL = 0;
	public const int SEARCH_SUB_MODE_PASSABLE = 1;

	public int path_status;

	public int[] node_matrix;
	public int max_node;
	public int node_count;

	public int group_id;
	public int search_mode;
	public int mobile_type;
	public int seek_nation_recno;
	public int attack_range; // used in search_mode = SEARCH_MODE_ATTACK_UNIT_BY_RANGE

	// used in search_mode = SEARCH_MODE_TO_ATTACK or SEARCH_MODE_TO_VEHICLE, get from miscNo
	public int target_recno;
	public int region_id; // used in search_mode = SEARCH_MODE_TO_LAND_FOR_SHIP
	public int building_id; // used in search_mode = SEARCH_MODE_TO_FIRM or SEARCH_MODE_TO_TOWN, get from miscNo
	public int building_x1, building_y1, building_x2, building_y2;
	public FirmInfo search_firm_info;

	public int final_dest_x; //	in search_mode SEARCH_MODE_REUSE, dest_x and dest_y may set to a different value.
	public int final_dest_y; // i.e. the value used finally may not be the real dest_? given.

	//------- aliasing class member vars for fast access ------//

	public bool[] nation_passable = new bool[GameConstants.MAX_NATION + 1];
	public int search_sub_mode;

	private ResultNode max_size_result_node_ptr; // point to the temporary result node list
	private int max_size_result_node_ptr_index;
	// the parent node of the currently node pointed by max_size_result_node_ptr
	private ResultNode parent_result_node_ptr;
	private int parent_result_node_ptr_index;

	private int upper_left_x; // x coord. of upper left corner of the 2x2 node
	private int upper_left_y; // y coord. of upper left corner of the 2x2 node

	private static UnitArray UnitArray => Sys.Instance.UnitArray;

	public SeekPath()
	{
		node_matrix = new int[GameConstants.MapSize * GameConstants.MapSize];
		path_status = PATH_WAIT;
	}

	public void set_status(int newStatus)
	{
		path_status = newStatus;
	}

	public void set_attack_range_para(int attackRange)
	{
		attack_range = attackRange;
	}

	public void reset_attack_range_para()
	{
		attack_range = 0;
	}

	public void set_nation_recno(int nationRecno)
	{
		seek_nation_recno = nationRecno;
	}

	public void set_nation_passable(bool[] nationPassable)
	{
		for (int i = 0; i < nation_passable.Length; i++)
		{
			nation_passable[i] = nationPassable[i];
		}
	}

	public void set_sub_mode(int subMode = SEARCH_SUB_MODE_NORMAL)
	{
		search_sub_mode = subMode;
	}

	private void bound_check_x(ref int paraX)
	{
		if (paraX < 0)
			paraX = 0;
		else if (paraX >= GameConstants.MapSize - 1)
			paraX = GameConstants.MapSize - 1;
	}

	private void bound_check_y(ref int paraY)
	{
		if (paraY < 0)
			paraY = 0;
		else if (paraY >= GameConstants.MapSize - 1)
			paraY = GameConstants.MapSize - 1;
	}

	private bool can_move_to(int xLoc, int yLoc)
	{
		Location loc = Sys.Instance.World.get_loc(xLoc, yLoc);
		Unit unit;
		int recno;
		int powerNationRecno = loc.power_nation_recno;
		int unitCurAction;

		//------ check terrain id. -------//
		switch (mobile_type)
		{
			case UnitConstants.UNIT_LAND:
				if (search_sub_mode == SEARCH_SUB_MODE_PASSABLE && (powerNationRecno != 0) && !nation_passable[powerNationRecno])
					return false;

				//------ be careful for the checking for search_mode>=SEARCH_MODE_TO_FIRM
				if (search_mode < SEARCH_MODE_TO_FIRM)
				{
					//------------------------------------------------------------------------//
					if (!loc.walkable())
						return false;

					recno = loc.cargo_recno;
					if (recno == 0)
						return true;

					switch (search_mode)
					{
						case SEARCH_MODE_IN_A_GROUP: // group move
						//case SEARCH_MODE_REUSE: // path-reuse
							break;

						case SEARCH_MODE_A_UNIT_IN_GROUP: // a unit in a group
							unit = UnitArray[recno];
							return unit.cur_action == Sprite.SPRITE_MOVE && unit.unit_id != UnitConstants.UNIT_CARAVAN;

						case SEARCH_MODE_TO_ATTACK: // to attack target
						case SEARCH_MODE_TO_VEHICLE: // move to a vehicle
							if (recno == target_recno)
								return true;
							break;

						case SEARCH_MODE_BLOCKING: // 2x2 unit blocking
							unit = UnitArray[recno];
							return unit.unit_group_id == group_id &&
							       (unit.cur_action == Sprite.SPRITE_MOVE || unit.cur_action == Sprite.SPRITE_READY_TO_MOVE);
					}
				}
				else
				{
					//--------------------------------------------------------------------------------//
					// for the following search_mode, location may be treated as walkable although it is not.
					//--------------------------------------------------------------------------------//
					switch (search_mode)
					{
						case SEARCH_MODE_TO_FIRM: // move to a firm, (location may be not walkable)
						case SEARCH_MODE_TO_TOWN: // move to a town zone, (location may be not walkable)
							if (!loc.walkable())
								return (xLoc >= building_x1 && xLoc <= building_x2 && yLoc >= building_y1 && yLoc <= building_y2);
							break;

						case SEARCH_MODE_TO_WALL_FOR_GROUP: // move to wall for a group, (location may be not walkable)
							if (!loc.walkable())
								return (xLoc == final_dest_x && yLoc == final_dest_y);
							break;

						case SEARCH_MODE_TO_WALL_FOR_UNIT: // move to wall for a unit only, (location may be not walkable)
							return (loc.walkable() && loc.cargo_recno == 0) || (xLoc == final_dest_x && yLoc == final_dest_y);

						case SEARCH_MODE_ATTACK_UNIT_BY_RANGE: // same as that used in SEARCH_MODE_TO_FIRM
						case SEARCH_MODE_ATTACK_FIRM_BY_RANGE:
						case SEARCH_MODE_ATTACK_TOWN_BY_RANGE:
						case SEARCH_MODE_ATTACK_WALL_BY_RANGE:
							if (!loc.walkable())
								return (xLoc >= building_x1 && xLoc <= building_x2 && yLoc >= building_y1 && yLoc <= building_y2);
							break;
					}

					recno = (mobile_type != UnitConstants.UNIT_AIR) ? loc.cargo_recno : loc.air_cargo_recno;
					if (recno == 0)
						return true;
				}

				//------- checking for unit's group_id, cur_action, nation_recno and position --------//
				unit = UnitArray[recno];
				unitCurAction = unit.cur_action;
				return (unit.unit_group_id == group_id && unitCurAction != Sprite.SPRITE_ATTACK) ||
				       (unitCurAction == Sprite.SPRITE_MOVE &&
				        unit.cur_x - unit.next_x <= InternalConstants.CellWidth / 2 &&
				        unit.cur_y - unit.next_y <= InternalConstants.CellHeight / 2) ||
				       (unit.nation_recno == seek_nation_recno && unitCurAction == Sprite.SPRITE_IDLE);

			case UnitConstants.UNIT_SEA:
				if (search_mode < SEARCH_MODE_TO_FIRM) //--------- be careful for the search_mode >= SEARCH_MODE_TO_FIRM
				{
					if (!loc.sailable())
						return false;

					recno = loc.cargo_recno;
					if (recno == 0)
						return true;

					switch (search_mode)
					{
						case SEARCH_MODE_IN_A_GROUP: // group move
						//case SEARCH_MODE_REUSE: // path-reuse
							break;

						case SEARCH_MODE_A_UNIT_IN_GROUP: // a unit in a group
							return UnitArray[recno].cur_action == Sprite.SPRITE_MOVE;

						case SEARCH_MODE_TO_ATTACK:
							if (recno == target_recno)
								return true;
							break;
					}
				}
				else
				{
					//--------------------------------------------------------------------------------//
					// for the following search_mode, location may be treated as sailable although it is not.
					//--------------------------------------------------------------------------------//
					switch (search_mode)
					{
						case SEARCH_MODE_TO_FIRM: // move to a firm, (location may be not walkable)
						case SEARCH_MODE_TO_TOWN: // move to a town zone, (location may be not walkable)
							if (!loc.sailable())
								return (xLoc >= building_x1 && xLoc <= building_x2 && yLoc >= building_y1 && yLoc <= building_y2);
							break;

						//case SEARCH_MODE_TO_WALL_FOR_GROUP:	// move to wall for a group, (location may be not walkable)
						//case SEARCH_MODE_TO_WALL_FOR_UNIT:	// move to wall for a unit only, (location may be not walkable)

						case SEARCH_MODE_ATTACK_UNIT_BY_RANGE: // same as that used in SEARCH_MODE_TO_FIRM
						case SEARCH_MODE_ATTACK_FIRM_BY_RANGE:
						case SEARCH_MODE_ATTACK_TOWN_BY_RANGE:
						case SEARCH_MODE_ATTACK_WALL_BY_RANGE:
							if (!loc.sailable())
								return (xLoc >= building_x1 && xLoc <= building_x2 && yLoc >= building_y1 && yLoc <= building_y2);
							break;

						case SEARCH_MODE_TO_LAND_FOR_SHIP:
							if (loc.sailable())
							{
								recno = loc.cargo_recno;
								if (recno == 0)
									return true;

								unit = UnitArray[recno];
								unitCurAction = unit.cur_action;
								return (unit.unit_group_id == group_id && unitCurAction != Sprite.SPRITE_ATTACK &&
								        unit.action_mode2 != UnitConstants.ACTION_SHIP_TO_BEACH) ||
								       (unit.unit_group_id != group_id && unitCurAction == Sprite.SPRITE_MOVE);
							}
							else if (loc.walkable() && loc.region_id == region_id)
								return true;
							else
								return false;
					}

					recno = loc.cargo_recno;
					if (recno == 0)
						return true;
				}

				//------- checking for unit's group_id, cur_action, nation_recno and position --------//
				unit = UnitArray[recno];
				unitCurAction = unit.cur_action;
				return (unit.unit_group_id == group_id && unitCurAction != Sprite.SPRITE_ATTACK) ||
				       unitCurAction == Sprite.SPRITE_MOVE ||
				       (unit.nation_recno == seek_nation_recno && unitCurAction == Sprite.SPRITE_IDLE);

			case UnitConstants.UNIT_AIR:
				recno = loc.air_cargo_recno;
				if (recno == 0)
					return true;
				switch (search_mode)
				{
					case SEARCH_MODE_IN_A_GROUP:
					//case SEARCH_MODE_REUSE:
					case SEARCH_MODE_TO_ATTACK:
					case SEARCH_MODE_TO_FIRM:
					case SEARCH_MODE_TO_TOWN:
					case SEARCH_MODE_TO_WALL_FOR_GROUP:
					case SEARCH_MODE_TO_WALL_FOR_UNIT:
					case SEARCH_MODE_ATTACK_UNIT_BY_RANGE:
					case SEARCH_MODE_ATTACK_FIRM_BY_RANGE:
					case SEARCH_MODE_ATTACK_TOWN_BY_RANGE:
					case SEARCH_MODE_ATTACK_WALL_BY_RANGE:
						unit = UnitArray[recno];
						unitCurAction = unit.cur_action;
						return (unit.unit_group_id == group_id && unitCurAction != Sprite.SPRITE_ATTACK) ||
						       unitCurAction == Sprite.SPRITE_MOVE ||
						       (unit.nation_recno == seek_nation_recno && unitCurAction == Sprite.SPRITE_IDLE);

					case SEARCH_MODE_A_UNIT_IN_GROUP: // a unit in a group
						return UnitArray[recno].cur_action == Sprite.SPRITE_MOVE;
				}

				break;
		}

		return false;
	}

	public int seek(int sx, int sy, int dx, int dy, int groupId, int mobileType, int searchMode = SEARCH_MODE_IN_A_GROUP,
		int miscNo = 0, int numOfPath = 1)
	{
		//-------- initialize vars --------------//
		search_mode = searchMode;
		group_id = groupId;
		mobile_type = mobileType;

		//------------------------------------------------------------------------------//
		// extract information from the parameter "miscNo"
		//------------------------------------------------------------------------------//
		target_recno = building_id = 0;
		building_x1 = building_y1 = building_x2 = building_y2 = -1;

		switch (search_mode)
		{
			case SEARCH_MODE_TO_ATTACK:
			case SEARCH_MODE_TO_VEHICLE:
				target_recno = miscNo;
				break;

			case SEARCH_MODE_TO_FIRM:
				building_id = miscNo;
				building_x1 = dx; // upper left corner location
				building_y1 = dy;
				search_firm_info = Sys.Instance.FirmRes[building_id];
				building_x2 = dx + search_firm_info.loc_width - 1;
				building_y2 = dy + search_firm_info.loc_height - 1;
				break;

			case SEARCH_MODE_TO_TOWN:
				building_id = miscNo;
				building_x1 = dx; // upper left corner location
				building_y1 = dy;
				if (miscNo != -1)
				{
					Location buildPtr = Sys.Instance.World.get_loc(dx, dy);
					Town targetTown = Sys.Instance.TownArray[buildPtr.town_recno()];
					building_x2 = targetTown.LocX2;
					building_y2 = targetTown.LocY2;
				}
				else // searching to settle. Detail explained in function set_move_to_surround()
				{
					building_x2 = building_x1 + InternalConstants.TOWN_WIDTH - 1;
					building_y2 = building_y1 + InternalConstants.TOWN_HEIGHT - 1;
				}

				break;

			case SEARCH_MODE_ATTACK_UNIT_BY_RANGE:
			case SEARCH_MODE_ATTACK_WALL_BY_RANGE:
				building_id = miscNo;
				building_x1 = Math.Max(dx - attack_range, 0);
				building_y1 = Math.Max(dy - attack_range, 0);
				building_x2 = Math.Min(dx + attack_range, GameConstants.MapSize - 1);
				building_y2 = Math.Min(dy + attack_range, GameConstants.MapSize - 1);
				break;

			case SEARCH_MODE_ATTACK_FIRM_BY_RANGE:
				building_id = miscNo;
				building_x1 = Math.Max(dx - attack_range, 0);
				building_y1 = Math.Max(dy - attack_range, 0);
				search_firm_info = Sys.Instance.FirmRes[building_id];
				building_x2 = Math.Min(dx + search_firm_info.loc_width - 1 + attack_range, GameConstants.MapSize - 1);
				building_y2 = Math.Min(dy + search_firm_info.loc_height - 1 + attack_range, GameConstants.MapSize - 1);
				break;

			case SEARCH_MODE_ATTACK_TOWN_BY_RANGE:
				building_id = miscNo;
				building_x1 = Math.Max(dx - attack_range, 0);
				building_y1 = Math.Max(dy - attack_range, 0);
				building_x2 = Math.Min(dx + InternalConstants.TOWN_WIDTH - 1 + attack_range, GameConstants.MapSize - 1);
				building_y2 = Math.Min(dy + InternalConstants.TOWN_HEIGHT - 1 + attack_range, GameConstants.MapSize - 1);
				break;

			case SEARCH_MODE_TO_LAND_FOR_SHIP:
				region_id = miscNo;
				break;
		}

		//------------------------------------------------------------------------------//
		// set start location and destination location
		//------------------------------------------------------------------------------//
		//---------- adjust destination for some kind of searching ------------//
		switch (search_mode)
		{
			case SEARCH_MODE_TO_FIRM:
			case SEARCH_MODE_TO_TOWN:
				final_dest_x = (building_x1 + building_x2) / 2; // the destination is set to be the middle of the building
				final_dest_y = (building_y1 + building_y2) / 2;

				//---------------------------------------------------------------------------------//
				// for group assign/settle, the final destination is adjusted by the value of numOfPath
				//---------------------------------------------------------------------------------//
				int area;
				if (search_mode == SEARCH_MODE_TO_TOWN)
					area = InternalConstants.TOWN_WIDTH * InternalConstants.TOWN_HEIGHT;
				else // search_mode == SEARCH_MODE_TO_FIRM
					area = search_firm_info.loc_width * search_firm_info.loc_height;

				int pathNum = (numOfPath > area) ? (numOfPath - 1) % area + 1 : numOfPath;

				int xShift, yShift;
				if (search_mode == SEARCH_MODE_TO_TOWN)
					Misc.cal_move_around_a_point(pathNum, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, out xShift, out yShift);
				else
					Misc.cal_move_around_a_point(pathNum, search_firm_info.loc_width, search_firm_info.loc_height, out xShift, out yShift);

				final_dest_x += xShift;
				final_dest_y += yShift;

				bound_check_x(ref final_dest_x);
				bound_check_y(ref final_dest_y);
				break;

			case SEARCH_MODE_ATTACK_UNIT_BY_RANGE:
			case SEARCH_MODE_ATTACK_FIRM_BY_RANGE:
			case SEARCH_MODE_ATTACK_TOWN_BY_RANGE:
			case SEARCH_MODE_ATTACK_WALL_BY_RANGE:
				final_dest_x = (building_x1 + building_x2) / 2; // the destination is set to be the middle of the building
				final_dest_y = (building_y1 + building_y2) / 2;
				break;

			default:
				final_dest_x = dx;
				final_dest_y = dy;
				break;
		}

		if (sx == final_dest_x && sy == final_dest_y)
			return PATH_FOUND;

		Array.Clear(node_matrix);
		node_matrix[sy * GameConstants.MapSize + sx] = 1;
		List<int> oldChangedNodesX = new List<int>(GameConstants.MapSize);
		List<int> oldChangedNodesY = new List<int>(GameConstants.MapSize);
		List<int> newChangedNodesX = new List<int>(GameConstants.MapSize);
		List<int> newChangedNodesY = new List<int>(GameConstants.MapSize);
		oldChangedNodesX.Add(sx);
		oldChangedNodesY.Add(sy);

		while (oldChangedNodesX.Count > 0)
		{
			newChangedNodesX.Clear();
			newChangedNodesY.Clear();
			for (int changedNodesIndex = 0; changedNodesIndex < oldChangedNodesX.Count; changedNodesIndex++)
			{
				int x = oldChangedNodesX[changedNodesIndex];
				int y = oldChangedNodesY[changedNodesIndex];
				int currentIndex = y * GameConstants.MapSize + x;

				for (int i = -1; i <= 1; i++)
				{
					for (int j = -1; j <= 1; j++)
					{
						int nearX = x + i;
						int nearY = y + j;
						if (nearX == x && nearY == y)
							continue;
						if (nearX < 0 || nearX >= GameConstants.MapSize)
							continue;
						if (nearY < 0 || nearY >= GameConstants.MapSize)
							continue;

						int nearIndex = nearY * GameConstants.MapSize + nearX;
						if (node_matrix[nearIndex] == 0 && !can_move_to(nearX, nearY))
							node_matrix[nearIndex] = -1;

						int newValue = 0;
						if (nearX == x || nearY == y)
							newValue = node_matrix[currentIndex] + 2;
						else
							newValue = node_matrix[currentIndex] + 3;

						if (node_matrix[nearIndex] == 0)
						{
							node_matrix[nearIndex] = newValue;
							newChangedNodesX.Add(nearX);
							newChangedNodesY.Add(nearY);
						}
						else
						{
							if (node_matrix[nearIndex] > newValue)
							{
								node_matrix[nearIndex] = newValue;
								newChangedNodesX.Add(nearX);
								newChangedNodesY.Add(nearY);
							}
						}
					}

					if (x == final_dest_x && y == final_dest_y && node_matrix[currentIndex] > 0)
					{
						path_status = PATH_FOUND;
						return path_status;
					}
				}
			}

			oldChangedNodesX.Clear();
			oldChangedNodesX.AddRange(newChangedNodesX);
			oldChangedNodesY.Clear();
			oldChangedNodesY.AddRange(newChangedNodesY);
		}

		path_status = PATH_IMPOSSIBLE;
		return path_status;
	}

	public ResultNode[] get_result(out int resultNodeCount, out int pathDist)
	{
		resultNodeCount = 0;
		pathDist = 0;
		int resultIndex = -1;
		int resultX = -1;
		int resultY = -1;
		switch (path_status)
		{
			case PATH_FOUND:
				resultIndex = final_dest_y * GameConstants.MapSize + final_dest_x;
				resultX = final_dest_x;
				resultY = final_dest_y;
				break;

			case PATH_IMPOSSIBLE:
				int regionSize = 0;
				while (true)
				{
					regionSize++;
					int minDistance = GameConstants.MapSize * GameConstants.MapSize;
					//TODO move bounds
					//TODO scan only borders
					for (int x = Math.Max(final_dest_x - regionSize, 0); x < Math.Min(final_dest_x + regionSize, GameConstants.MapSize - 1); x++)
					{
						for (int y = Math.Max(final_dest_y - regionSize, 0); y < Math.Min(final_dest_y + regionSize, GameConstants.MapSize - 1); y++)
						{
							int currentIndex = y * GameConstants.MapSize + x;
							if (node_matrix[currentIndex] > 0)
							{
								if (node_matrix[currentIndex] < minDistance)
								{
									minDistance = node_matrix[currentIndex];
									resultIndex = currentIndex;
									resultX = x;
									resultY = y;
								}
							}
						}
					}

					if (resultIndex >= 0 || regionSize > GameConstants.MapSize)
						break;
				}

				break;
		}

		if (resultIndex == -1)
			return null;

		int pathX = resultX;
		int pathY = resultY;
		int pathValue = node_matrix[resultIndex];

		List<ResultNode> reversedPath = new List<ResultNode>(pathValue);
		reversedPath.Add(new ResultNode(pathX, pathY));

		pathDist = pathValue / 2;
		while (true)
		{
			int pathXCopy = pathX;
			int pathYCopy = pathY;
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					int nearX = pathXCopy + i;
					int nearY = pathYCopy + j;
					if (nearX == pathXCopy && nearY == pathYCopy)
						continue;
					if (nearX < 0 || nearX >= GameConstants.MapSize)
						continue;
					if (nearY < 0 || nearY >= GameConstants.MapSize)
						continue;

					int currentIndex = nearY * GameConstants.MapSize + nearX;
					if (node_matrix[currentIndex] > 0 && node_matrix[currentIndex] < pathValue)
					{
						pathValue = node_matrix[currentIndex];
						pathX = nearX;
						pathY = nearY;
					}
				}
			}

			reversedPath.Add(new ResultNode(pathX, pathY));

			if (pathValue == 1)
				break;
		}

		ResultNode[] resultPath = new ResultNode[reversedPath.Count];
		for (int i = 0; i < reversedPath.Count; i++)
		{
			resultPath[i] = new ResultNode(reversedPath[reversedPath.Count - i - 1].node_x, reversedPath[reversedPath.Count - i - 1].node_y);
		}

		resultNodeCount = resultPath.Length;
		return resultPath;
	}
}