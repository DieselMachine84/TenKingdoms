using System;

namespace TenKingdoms;

public class SeekPath
{
	public const int MAX_BACKGROUND_NODE = 2400;
	public const int MAX_CHILD_NODE = 8;
	public const int VALID_BACKGROUND_SEARCH_NODE = 1600;
	public const int MIN_BACKGROUND_NODE_USED_UP = 400;
	public const int MAX_STACK_NUM = MAX_BACKGROUND_NODE;

	public const int PATH_WAIT = 0;
	public const int PATH_SEEKING = 1;
	public const int PATH_FOUND = 2;
	public const int PATH_NODE_USED_UP = 3;
	public const int PATH_IMPOSSIBLE = 4;
	public const int PATH_REUSE_FOUND = 5;

	public const int SEARCH_MODE_IN_A_GROUP = 1;
	public const int SEARCH_MODE_A_UNIT_IN_GROUP = 2;
	public const int SEARCH_MODE_TO_ATTACK = 3;
	public const int SEARCH_MODE_REUSE = 4;
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
	public int real_sour_x, real_sour_y; // the actual coordinate of the starting point
	public int real_dest_x, real_dest_y; // the actual coordinate of the destination point
	public int dest_x, dest_y; // the coordinate of the destination represented in 2x2 node form
	public bool is_dest_blocked;
	public int current_search_node_used; // count the number of nodes used in the current searching

	public int border_x1, border_y1, border_x2, border_y2;

	public NodePriorityQueue open_node_list = new NodePriorityQueue();
	public NodePriorityQueue closed_node_list = new NodePriorityQueue();

	public int[] node_matrix;
	public Node[] node_array;

	public int max_node;
	public int node_count;

	public Node result_node_ptr;

	public int total_node_avail;

	public int cur_stack_pos = 0;
	public Node[] stack_array = new Node[MAX_STACK_NUM];
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

	public int max_node_num;
	public int[] reuse_node_matrix_ptr;
	public Node reuse_result_node_ptr;
	public int final_dest_x; //	in search_mode SEARCH_MODE_REUSE, dest_x and dest_y may set to a different value.
	public int final_dest_y; // i.e. the value used finally may not be the real dest_? given.

	//------- aliasing class member vars for fast access ------//

	public SeekPath cur_seek_path;
	public int cur_dest_x, cur_dest_y;
	public int[] cur_node_matrix;
	public Node[] cur_node_array;
	public int cur_border_x1, cur_border_y1, cur_border_x2, cur_border_y2;

	public bool[] nation_passable = new bool[GameConstants.MAX_NATION + 1];
	public int search_sub_mode;

	private ResultNode max_size_result_node_ptr; // point to the temporary result node list
	private int max_size_result_node_ptr_index;
	// the parent node of the currently node pointed by max_size_result_node_ptr
	private ResultNode parent_result_node_ptr;
	private int parent_result_node_ptr_index;

	private int upper_left_x; // x coord. of upper left corner of the 2x2 node
	private int upper_left_y; // y coord. of upper left corner of the 2x2 node

	private ResultNode[] reversedPath = new ResultNode[GameConstants.MapSize * GameConstants.MapSize];

	public SeekPath()
	{
		for (int i = 0; i < reversedPath.Length; i++)
		{
			reversedPath[i] = new ResultNode();
		}
	}

	public void init(int maxNode)
	{
		max_node = maxNode;
		node_array = new Node[max_node];
		node_matrix = new int[GameConstants.MapSize * GameConstants.MapSize];

		path_status = PATH_WAIT;
		open_node_list.reset_priority_queue();
		closed_node_list.reset_priority_queue();

		reset_total_node_avail();
	}

	public void set_node_matrix(int[] reuseNodeMatrix)
	{
		reuse_node_matrix_ptr = reuseNodeMatrix;
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

	public bool is_valid_searching()
	{
		return total_node_avail > VALID_BACKGROUND_SEARCH_NODE;
	}

	public void reset()
	{
		path_status = PATH_WAIT;
		open_node_list.reset_priority_queue();
		closed_node_list.reset_priority_queue();
	}

	public void reset_total_node_avail()
	{
		total_node_avail = MAX_BACKGROUND_NODE;
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

	public int seek(int sx, int sy, int dx, int dy, int groupId, int mobileType,
		int searchMode = SEARCH_MODE_IN_A_GROUP, int miscNo = 0, int numOfPath = 1, int maxTries = 0,
		int borderX1 = 0, int borderY1 = 0, int borderX2 = GameConstants.MapSize - 1,
		int borderY2 = GameConstants.MapSize - 1)
	{
		//-------- initialize vars --------------//
		path_status = PATH_SEEKING;
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
					building_x2 = targetTown.loc_x2;
					building_y2 = targetTown.loc_y2;
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

		for (int i = 0; i < node_matrix.Length; i++)
		{
			node_matrix[i] = 0;
		}

		node_matrix[sy * GameConstants.MapSize + sx] = 1;

		int startX = sx;
		int startY = sy;
		//int distance = abs(final_dest_x - startX) + abs(final_dest_y - startY);
		int regionSize = 0;
		bool hasProgress = true;
		while (hasProgress)
		{
			//begin:
			regionSize++;
			hasProgress = false;
			for (int x = Math.Max(startX - regionSize, 0); x <= Math.Min(startX + regionSize, GameConstants.MapSize - 1); x++)
			{
				for (int y = Math.Max(startY - regionSize, 0); y <= Math.Min(startY + regionSize, GameConstants.MapSize - 1); y++)
				{
					int currentIndex = y * GameConstants.MapSize + x;
					if (node_matrix[currentIndex] == 0 && !Node.can_move_to(x, y))
						node_matrix[currentIndex] = -1;

					if (node_matrix[currentIndex] >= 0)
					{
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
								if (node_matrix[nearIndex] > 0)
								{
									int newValue = 0;
									if (nearX == x || nearY == y)
										newValue = node_matrix[nearIndex] + 2;
									else
										newValue = node_matrix[nearIndex] + 3;

									if (node_matrix[currentIndex] == 0)
									{
										node_matrix[currentIndex] = newValue;
										hasProgress = true;
									}
									else
									{
										if (node_matrix[currentIndex] > newValue)
										{
											node_matrix[currentIndex] = newValue;
											hasProgress = true;
										}
									}
								}
							}
						}

						if (x == final_dest_x && y == final_dest_y && node_matrix[currentIndex] > 0)
						{
							path_status = PATH_FOUND;
							return path_status;
						}
					}

					/*if (node_matrix[currentIndex] > 0)
					{
						int newDistance = abs(final_dest_x - x) + abs(final_dest_y - y);
						if (newDistance < distance)
						{
							startX = x;
							startY = y;
							distance = newDistance;
							regionSize = 0;
							hasProgress = true;
							goto begin;
						}
					}*/
				}
			}
		}

		path_status = PATH_NODE_USED_UP;
		return path_status;
	}

	public ResultNode[] get_result(ref int resultNodeCount, ref int pathDist)
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

			case PATH_NODE_USED_UP:
				int regionSize = 0;
				while (true)
				{
					regionSize++;
					int minDistance = GameConstants.MapSize * GameConstants.MapSize;
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

					if (resultIndex >= 0 || regionSize > Math.Min(GameConstants.MapSize, GameConstants.MapSize))
						break;
				}

				break;
		}

		if (resultIndex == -1)
			return null;

		int pathX = resultX;
		int pathY = resultY;
		int pathValue = node_matrix[resultIndex];
		int pathIndex = 0;
		for (int i = 0; i < reversedPath.Length; i++)
		{
			reversedPath[i].node_x = 0;
			reversedPath[i].node_y = 0;
		}

		reversedPath[pathIndex].node_x = pathX;
		reversedPath[pathIndex].node_y = pathY;
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

			pathIndex++;
			reversedPath[pathIndex].node_x = pathX;
			reversedPath[pathIndex].node_y = pathY;

			if (pathValue == 1)
				break;
		}

		ResultNode[] resultPath = new ResultNode[pathIndex + 1];
		for (int i = 0; i <= pathIndex; i++)
		{
			resultPath[i] = new ResultNode();
			resultPath[i].node_x = reversedPath[pathIndex - i].node_x;
			resultPath[i].node_y = reversedPath[pathIndex - i].node_y;
		}

		resultNodeCount = pathIndex + 1;
		return resultPath;
	}

	private void add_result_node(int x, int y, ResultNode curPtr, ResultNode prePtr, ref int count)
	{
		//TODO rewrite
		count++;
	}

	private void get_real_result_node(ref int count, int enterDirection, int exitDirection, int nodeType,
		int xCoord, int yCoord)
	{
		//TODO rewrite
	}

	private static Node return_best_node()
	{
		//TODO rewrite
		return null;
	}

	public Node return_closest_node()
	{
		//TODO rewrite
		return null;
	}

	public ResultNode[] smooth_the_path(ResultNode[] nodeArray, ref int nodeCount) // smoothing the path
	{
		//TODO rewrite
		return null;
	}

	//------------ for scale 2 -------------//
	public int seek2(int sx, int sy, int dx, int dy, int miscNo, int numOfPath, int maxTries)
	{
		//TODO rewrite
		return 0;
	}

	public int continue_seek2(int maxTries, bool firstSeek = false)
	{
		//TODO rewrite
		return 0;
	}

	public ResultNode[] get_result2(ref int resultNodeCount, ref int pathDist)
	{
		//TODO rewrite
		return null;
	}

	private int result_node_distance(ResultNode node1, ResultNode node2)
	{
		int xDist = Math.Abs(node1.node_x - node2.node_x);
		int yDist = Math.Abs(node1.node_y - node2.node_y);

		if (xDist != 0)
		{
			return xDist;
		}
		else // xDist = 0;
		{
			return yDist;
		}
	}
}