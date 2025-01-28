namespace TenKingdoms;

public class ResultNode
{
	public int node_x;
    public int node_y;
}

public class NodePriorityQueue
{
	private const int MAX_ARRAY_SIZE = SeekPath.MAX_BACKGROUND_NODE + 1;
	public int size;
	public Node[] elements = new Node[MAX_ARRAY_SIZE];

	public void reset_priority_queue()
	{
		size = 0;
		//memset(elements, 0, sizeof(Node*) * (MAX_ARRAY_SIZE));
		for (int i = 0; i < elements.Length; i++)
		{
			elements[i] = new Node();
		}
	}

	public void insert_node(Node insertNode)
	{
		int i = ++size;
		int f = insertNode.node_f;
		Node[] localElements = elements;

		while (i > 1 && localElements[i / 2].node_f > f)
		{
			localElements[i] = localElements[i / 2];
			i /= 2;
		}

		localElements[i] = insertNode;
	}

	public Node return_min()
	{
		if (size == 0)
			return null;

		int i, child, doubleI;
		int localSize = size--;

		Node[] localElements = elements;
		Node minElement = localElements[1];
		Node lastElement = localElements[localSize];
		int lastF = lastElement.node_f;

		//--------- doubleI = i*2 ---------//
		for (i = 1, doubleI = 2; doubleI <= localSize; i = child, doubleI = i << 1)
		{
			child = doubleI;
			if (child != localSize && localElements[child + 1].node_f < localElements[child].node_f)
				child++;

			if (lastF > localElements[child].node_f)
				localElements[i] = localElements[child];
			else
				break;
		}

		localElements[i] = lastElement;

		return minElement;
	}
}

public class Node
{
	public int node_x, node_y;
	public int node_f, node_h;

	public int node_g;

	// the type of the node, total 16 different type, 4 points in a 2x2 node, blocked/non-blocked, so there are 2^4 combinations
	public int node_type;

	public int enter_direction;
	// enter_direction -- 1-8 for eight directions, 0 for the starting node
	//
	//		8	7	6
	//		1	x	5		where x is the reference point
	//		2	3	4

	public Node parent_node;
	public Node[] child_node = new Node[SeekPath.MAX_CHILD_NODE];
	public Node next_node;

	private const int MAX_STACK_NUM = SeekPath.MAX_BACKGROUND_NODE;
	private static int cur_stack_pos;
	private static Node[] stack_array = new Node[MAX_STACK_NUM];

	private static SeekPath SeekPath => Sys.Instance.SeekPath;
	private static UnitArray UnitArray => Sys.Instance.UnitArray;

	private static void stack_push(Node nodePtr)
	{
		stack_array[cur_stack_pos++] = nodePtr;
	}

	private static Node stack_pop()
	{
		return stack_array[--cur_stack_pos];
	}

	public void Clear()
	{
		node_x = 0;
		node_y = 0;
		node_f = 0;
		node_g = 0;
		node_h = 0;
		node_type = 0;
		enter_direction = 0;

		parent_node = null;
		next_node = null;
		for (int i = 0; i < child_node.Length; i++)
		{
			child_node[i] = null;
		}
	}

	public static bool can_move_to(int xLoc, int yLoc)
	{
		Location loc = Sys.Instance.World.get_loc(xLoc, yLoc);
		Unit unit;
		int recno;
		int powerNationRecno = loc.power_nation_recno;
		int unitCurAction;

		//------ check terrain id. -------//
		switch (SeekPath.mobile_type)
		{
			case UnitConstants.UNIT_LAND:
				if (SeekPath.search_sub_mode == SeekPath.SEARCH_SUB_MODE_PASSABLE && (powerNationRecno != 0) &&
				    !SeekPath.nation_passable[powerNationRecno])
					return false;

				//------ be careful for the checking for search_mode>=SEARCH_MODE_TO_FIRM
				if (SeekPath.search_mode < SeekPath.SEARCH_MODE_TO_FIRM)
				{
					//------------------------------------------------------------------------//
					if (!loc.walkable())
						return false;

					recno = loc.cargo_recno;
					if (recno == 0)
						return true;

					switch (SeekPath.search_mode)
					{
						case SeekPath.SEARCH_MODE_IN_A_GROUP: // group move
						case SeekPath.SEARCH_MODE_REUSE: // path-reuse
							break;

						case SeekPath.SEARCH_MODE_A_UNIT_IN_GROUP: // a unit in a group
							unit = UnitArray[recno];
							return unit.cur_action == Sprite.SPRITE_MOVE && unit.unit_id != UnitConstants.UNIT_CARAVAN;

						case SeekPath.SEARCH_MODE_TO_ATTACK: // to attack target
						case SeekPath.SEARCH_MODE_TO_VEHICLE: // move to a vehicle
							if (recno == SeekPath.target_recno)
								return true;
							break;

						case SeekPath.SEARCH_MODE_BLOCKING: // 2x2 unit blocking
							unit = UnitArray[recno];
							return unit.unit_group_id == SeekPath.group_id &&
							       (unit.cur_action == Sprite.SPRITE_MOVE ||
							        unit.cur_action == Sprite.SPRITE_READY_TO_MOVE);

						default:
							break;
					}
				}
				else
				{
					//--------------------------------------------------------------------------------//
					// for the following search_mode, location may be treated as walkable although it is not.
					//--------------------------------------------------------------------------------//
					switch (SeekPath.search_mode)
					{
						case SeekPath.SEARCH_MODE_TO_FIRM: // move to a firm, (location may be not walkable)
						case SeekPath.SEARCH_MODE_TO_TOWN: // move to a town zone, (location may be not walkable)
							if (!loc.walkable())
								return (xLoc >= SeekPath.building_x1 && xLoc <= SeekPath.building_x2 &&
								        yLoc >= SeekPath.building_y1 && yLoc <= SeekPath.building_y2);
							break;

						case SeekPath.SEARCH_MODE_TO_WALL_FOR_GROUP
							: // move to wall for a group, (location may be not walkable)
							if (!loc.walkable())
								return (xLoc == SeekPath.final_dest_x && yLoc == SeekPath.final_dest_y);
							break;

						case SeekPath.SEARCH_MODE_TO_WALL_FOR_UNIT
							: // move to wall for a unit only, (location may be not walkable)
							return (loc.walkable() && loc.cargo_recno == 0) ||
							       (xLoc == SeekPath.final_dest_x && yLoc == SeekPath.final_dest_y);

						case SeekPath.SEARCH_MODE_ATTACK_UNIT_BY_RANGE: // same as that used in SEARCH_MODE_TO_FIRM
						case SeekPath.SEARCH_MODE_ATTACK_FIRM_BY_RANGE:
						case SeekPath.SEARCH_MODE_ATTACK_TOWN_BY_RANGE:
						case SeekPath.SEARCH_MODE_ATTACK_WALL_BY_RANGE:
							if (!loc.walkable())
								return (xLoc >= SeekPath.building_x1 && xLoc <= SeekPath.building_x2 &&
								        yLoc >= SeekPath.building_y1 && yLoc <= SeekPath.building_y2);
							break;

						default:
							break;
					}

					recno = (SeekPath.mobile_type != UnitConstants.UNIT_AIR) ? loc.cargo_recno : loc.air_cargo_recno;
					if (recno == 0)
						return true;
				}

				//------- checking for unit's group_id, cur_action, nation_recno and position --------//
				unit = UnitArray[recno];
				unitCurAction = unit.cur_action;
				return (unit.unit_group_id == SeekPath.group_id && unitCurAction != Sprite.SPRITE_ATTACK) ||
				       (unitCurAction == Sprite.SPRITE_MOVE &&
				        unit.cur_x - unit.next_x <= InternalConstants.CellWidth / 2 &&
				        unit.cur_y - unit.next_y <= InternalConstants.CellHeight / 2) ||
				       (unit.nation_recno == SeekPath.seek_nation_recno && unitCurAction == Sprite.SPRITE_IDLE);

			case UnitConstants.UNIT_SEA:
				if (SeekPath.search_mode < SeekPath.SEARCH_MODE_TO_FIRM) //--------- be careful for the search_mode>=SEARCH_MODE_TO_FIRM
				{
					if (!loc.sailable())
						return false;

					recno = loc.cargo_recno;
					if (recno == 0)
						return true;

					switch (SeekPath.search_mode)
					{
						case SeekPath.SEARCH_MODE_IN_A_GROUP: // group move
						case SeekPath.SEARCH_MODE_REUSE: // path-reuse
							break;

						case SeekPath.SEARCH_MODE_A_UNIT_IN_GROUP: // a unit in a group
							return UnitArray[recno].cur_action == Sprite.SPRITE_MOVE;

						case SeekPath.SEARCH_MODE_TO_ATTACK:
							if (recno == SeekPath.target_recno)
								return true;
							break;

						default:
							break;
					}
				}
				else
				{
					//--------------------------------------------------------------------------------//
					// for the following search_mode, location may be treated as sailable although it is not.
					//--------------------------------------------------------------------------------//
					switch (SeekPath.search_mode)
					{
						case SeekPath.SEARCH_MODE_TO_FIRM: // move to a firm, (location may be not walkable)
						case SeekPath.SEARCH_MODE_TO_TOWN: // move to a town zone, (location may be not walkable)
							if (!loc.sailable())
								return (xLoc >= SeekPath.building_x1 && xLoc <= SeekPath.building_x2 &&
								        yLoc >= SeekPath.building_y1 && yLoc <= SeekPath.building_y2);
							break;

						//case SEARCH_MODE_TO_WALL_FOR_GROUP:	// move to wall for a group, (location may be not walkable)
						//case SEARCH_MODE_TO_WALL_FOR_UNIT:	// move to wall for a unit only, (location may be not walkable)

						case SeekPath.SEARCH_MODE_ATTACK_UNIT_BY_RANGE: // same as that used in SEARCH_MODE_TO_FIRM
						case SeekPath.SEARCH_MODE_ATTACK_FIRM_BY_RANGE:
						case SeekPath.SEARCH_MODE_ATTACK_TOWN_BY_RANGE:
						case SeekPath.SEARCH_MODE_ATTACK_WALL_BY_RANGE:
							if (!loc.sailable())
								return (xLoc >= SeekPath.building_x1 && xLoc <= SeekPath.building_x2 &&
								        yLoc >= SeekPath.building_y1 && yLoc <= SeekPath.building_y2);
							break;

						case SeekPath.SEARCH_MODE_TO_LAND_FOR_SHIP:
							if (loc.sailable())
							{
								recno = loc.cargo_recno;
								if (recno == 0)
									return true;

								unit = UnitArray[recno];
								unitCurAction = unit.cur_action;
								return (unit.unit_group_id == SeekPath.group_id && unitCurAction != Sprite.SPRITE_ATTACK &&
								        unit.action_mode2 != UnitConstants.ACTION_SHIP_TO_BEACH) ||
								       (unit.unit_group_id != SeekPath.group_id && unitCurAction == Sprite.SPRITE_MOVE);
							}
							else if (loc.walkable() && loc.region_id == SeekPath.region_id)
								return true;
							else
								return false;

						default:
							break;
					}

					recno = loc.cargo_recno;
					if (recno == 0)
						return true;
				}

				//------- checking for unit's group_id, cur_action, nation_recno and position --------//
				unit = UnitArray[recno];
				unitCurAction = unit.cur_action;
				return (unit.unit_group_id == SeekPath.group_id && unitCurAction != Sprite.SPRITE_ATTACK) ||
				       unitCurAction == Sprite.SPRITE_MOVE ||
				       (unit.nation_recno == SeekPath.seek_nation_recno && unitCurAction == Sprite.SPRITE_IDLE);
				break;

			case UnitConstants.UNIT_AIR:
				recno = loc.air_cargo_recno;
				if (recno == 0)
					return true;
				switch (SeekPath.search_mode)
				{
					case SeekPath.SEARCH_MODE_IN_A_GROUP:
					case SeekPath.SEARCH_MODE_REUSE:
					case SeekPath.SEARCH_MODE_TO_ATTACK:
					case SeekPath.SEARCH_MODE_TO_FIRM:
					case SeekPath.SEARCH_MODE_TO_TOWN:
					case SeekPath.SEARCH_MODE_TO_WALL_FOR_GROUP:
					case SeekPath.SEARCH_MODE_TO_WALL_FOR_UNIT:
					case SeekPath.SEARCH_MODE_ATTACK_UNIT_BY_RANGE:
					case SeekPath.SEARCH_MODE_ATTACK_FIRM_BY_RANGE:
					case SeekPath.SEARCH_MODE_ATTACK_TOWN_BY_RANGE:
					case SeekPath.SEARCH_MODE_ATTACK_WALL_BY_RANGE:
						unit = UnitArray[recno];
						unitCurAction = unit.cur_action;
						return (unit.unit_group_id == SeekPath.group_id && unitCurAction != Sprite.SPRITE_ATTACK) ||
						       unitCurAction == Sprite.SPRITE_MOVE ||
						       (unit.nation_recno == SeekPath.seek_nation_recno && unitCurAction == Sprite.SPRITE_IDLE);

					case SeekPath.SEARCH_MODE_A_UNIT_IN_GROUP: // a unit in a group
						return UnitArray[recno].cur_action == Sprite.SPRITE_MOVE;

					default:
						break;
				}

				break;
		}

		return false;
	}

	public bool generate_successors(int parentEnterDirection, int realSourX, int realSourY)
	{
		bool hasLeft = node_x > SeekPath.cur_border_x1;
		bool hasRight = node_x < SeekPath.cur_border_x2;
		bool hasUp = node_y > SeekPath.cur_border_y1;
		bool hasDown = node_y < SeekPath.cur_border_y2;

		int upperLeftX = node_x << 1;
		int upperLeftY = node_y << 1;
		int cost;

		//-------------------------------------------
		// enter_direction = (exit_direction+3)%8+1
		//-------------------------------------------

		if (hasLeft)
		{
			//--------- Left, exit_direction=1 --------//
			if ((node_type & 0x05) != 0 &&
			    (can_move_to(upperLeftX - 1, upperLeftY) || can_move_to(upperLeftX - 1, upperLeftY + 1)))
			{
				if (parentEnterDirection == 2 || parentEnterDirection == 8 ||
				    ((node_type & 0x1) != 0 && parentEnterDirection == 7) ||
				    ((node_type & 0x4) != 0 && parentEnterDirection == 3) ||
				    (parentEnterDirection == 0 && realSourX == upperLeftX))
					cost = 1;
				else
					cost = 2;

				if (generate_succ(node_x - 1, node_y, 5, cost))
					return true;
			}

			if (hasUp)
			{
				//------- Upper-Left, exit_direction=8 ---------//
				if ((node_type & 0x1) != 0 && can_move_to(upperLeftX - 1, upperLeftY - 1))
				{
					if (parentEnterDirection == 7 || parentEnterDirection == 1 ||
					    (parentEnterDirection == 0 && realSourX == upperLeftX && realSourY == upperLeftY))
						cost = 1;
					else
						cost = 2;

					if (generate_succ(node_x - 1, node_y - 1, 4, cost))
						return true;
				}
			}

			if (hasDown)
			{
				//--------- Lower-Left, exit_direction=2 ----------//
				if ((node_type & 0x4) != 0 && can_move_to(upperLeftX - 1, upperLeftY + 2))
				{
					if (parentEnterDirection == 1 || parentEnterDirection == 3 ||
					    (parentEnterDirection == 0 && realSourX == upperLeftX && realSourY == (upperLeftY + 1)))
						cost = 1;
					else
						cost = 2;

					if (generate_succ(node_x - 1, node_y + 1, 6, cost))
						return true;
				}
			}
		}

		if (hasRight)
		{
			//----------- Right, exit_direction=5 -----------//
			if ((node_type & 0xA) != 0 &&
			    (can_move_to(upperLeftX + 2, upperLeftY) || can_move_to(upperLeftX + 2, upperLeftY + 1)))
			{
				if (parentEnterDirection == 4 || parentEnterDirection == 6 ||
				    ((node_type & 0x02) != 0 && parentEnterDirection == 7) ||
				    ((node_type & 0x08) != 0 && parentEnterDirection == 3) ||
				    (parentEnterDirection == 0 && realSourX == (upperLeftX + 1)))
					cost = 1;
				else
					cost = 2;

				if (generate_succ(node_x + 1, node_y, 1, cost))
					return true;
			}

			if (hasUp)
			{
				//-------- Upper-Right, exit_direction=6 ---------//

				if ((node_type & 0x2) != 0 && can_move_to(upperLeftX + 2, upperLeftY - 1))
				{
					if (parentEnterDirection == 5 || parentEnterDirection == 7 ||
					    (parentEnterDirection == 0 && realSourX == (upperLeftX + 1) && realSourY == upperLeftY))
						cost = 1;
					else
						cost = 2;

					if (generate_succ(node_x + 1, node_y - 1, 2, cost))
						return true;
				}
			}

			if (hasDown)
			{
				//--------- Lower-Right, exit_direction=4 ---------//

				if ((node_type & 0x8) != 0 && can_move_to(upperLeftX + 2, upperLeftY + 2))
				{
					if (parentEnterDirection == 3 || parentEnterDirection == 5 ||
					    (parentEnterDirection == 0 && realSourX == (upperLeftX + 1) && realSourY == (upperLeftY + 1)))
						cost = 1;
					else
						cost = 2;

					if (generate_succ(node_x + 1, node_y + 1, 8, cost))
						return true;
				}
			}
		}

		if (hasUp)
		{
			//---------- Upper, exit_direction=7 -----------//
			if ((node_type & 0x03) != 0 &&
			    (can_move_to(upperLeftX, upperLeftY - 1) || can_move_to(upperLeftX + 1, upperLeftY - 1)))
			{
				if (parentEnterDirection == 6 || parentEnterDirection == 8 ||
				    ((node_type & 0x01) != 0 && parentEnterDirection == 1) ||
				    ((node_type & 0x02) != 0 && parentEnterDirection == 5) ||
				    (parentEnterDirection == 0 && realSourY == upperLeftY))
					cost = 1;
				else
					cost = 2;

				if (generate_succ(node_x, node_y - 1, 3, cost))
					return true;
			}
		}

		if (hasDown)
		{
			//---------- Lower, exit_direction=3 -----------// 
			if ((node_type & 0xC) != 0 &&
			    (can_move_to(upperLeftX, upperLeftY + 2) || can_move_to(upperLeftX + 1, upperLeftY + 2)))
			{
				if (parentEnterDirection == 2 || parentEnterDirection == 4 ||
				    ((node_type & 0x4) != 0 && parentEnterDirection == 1) ||
				    ((node_type & 0x8) != 0 && parentEnterDirection == 5) ||
				    (parentEnterDirection == 0 && realSourY == (upperLeftY + 1)))
					cost = 1;
				else
					cost = 2;

				if (generate_succ(node_x, node_y + 1, 7, cost))
					return true;
			}
		}

		return false;
	}

	public bool generate_succ(int x, int y, int enter_direct, int cost)
	{
		//----- if it points back to this node's parent ----//
		if (parent_node != null)
		{
			if (parent_node.node_x == x && parent_node.node_y == y)
				return false;
		}

		//----- if there is an existing node at the given position ----//
		int upperLeftX, upperLeftY;
		//int cost;
		int c, g = node_g + cost; // g(Successor)=g(BestNode)+cost of getting from BestNode to Successor
		int nodeRecno;

		if ((nodeRecno = SeekPath.cur_node_matrix[y * GameConstants.MapSize / 2 + x]) > 0 && nodeRecno < SeekPath.max_node_num)
		{
			Node oldNode = SeekPath.cur_node_array[nodeRecno - 1];

			//------ Add oldNode to the list of BestNode's child_noderen (or Successors).
			for (c = 0; c < SeekPath.MAX_CHILD_NODE && child_node[c] != null; c++)
			{
			}

			child_node[c] = oldNode;

			//---- if our new g value is < oldNode's then reset oldNode's parent to point to BestNode
			if (g < oldNode.node_g)
			{
				oldNode.parent_node = this;
				oldNode.node_g = g;
				oldNode.node_f = g + oldNode.node_h;
				oldNode.enter_direction = enter_direct;

				//-------- if it's a closed node ---------//
				if (oldNode.child_node[0] != null)
				{
					//-------------------------------------------------//
					// Since we changed the g value of oldNode, we need
					// to propagate this new value downwards, i.e.
					// do a Depth-First traversal of the tree!
					//-------------------------------------------------//
					oldNode.propagate_down();
					//sys_yield();
				}
			}
		}
		else //------------ add a new node -----------//
		{
			Node succNode = SeekPath.cur_node_array[SeekPath.node_count++];

			//memset(succNode, 0, sizeof(Node));

			succNode.parent_node = this;
			succNode.node_g = g;
			// should do sqrt(), but since we don't really
			succNode.node_h = (x - SeekPath.cur_dest_x) * (x - SeekPath.cur_dest_x) + (y - SeekPath.cur_dest_y) * (y - SeekPath.cur_dest_y);
			succNode.node_f = g + succNode.node_h; // care about the distance but just which branch looks
			succNode.node_x = x; // better this should suffice. Anyayz it's faster.
			succNode.node_y = y;
			succNode.enter_direction = enter_direct;
			upperLeftX = x << 1;
			upperLeftY = y << 1;
			succNode.node_type = (can_move_to(upperLeftX, upperLeftY) ? 1 : 0) +
			                     ((can_move_to(upperLeftX + 1, upperLeftY) ? 1 : 0) << 1) +
			                     ((can_move_to(upperLeftX, upperLeftY + 1) ? 1 : 0) << 2) +
			                     ((can_move_to(upperLeftX + 1, upperLeftY + 1) ? 1 : 0) << 3);
			for (int i = 0; i < succNode.child_node.Length; i++)
			{
				succNode.child_node[i] = null;
			}

			succNode.next_node = null;

			// path-reuse node found, but checking of can_walk(final_dest_?) is requested
			if (SeekPath.search_mode == SeekPath.SEARCH_MODE_REUSE && nodeRecno > SeekPath.max_node_num)
			{
				int destIndex = nodeRecno - SeekPath.max_node_num;
				switch (destIndex)
				{
					case 1:
						SeekPath.final_dest_x = x << 1;
						SeekPath.final_dest_y = y << 1;
						break;

					case 2:
						SeekPath.final_dest_x = (x << 1) + 1;
						SeekPath.final_dest_y = y << 1;
						break;

					case 3:
						SeekPath.final_dest_x = x << 1;
						SeekPath.final_dest_y = (y << 1) + 1;
						break;

					case 4:
						SeekPath.final_dest_x = (x << 1) + 1;
						SeekPath.final_dest_y = (y << 1) + 1;
						break;

					default:
						break;
				}

				if (can_move_to(SeekPath.final_dest_x, SeekPath.final_dest_y)) // can_walk the connection point, accept this case
				{
					SeekPath.reuse_result_node_ptr = succNode;
					return true;
				} // else continue until reuse node is found and connection point can be walked
			}

			SeekPath.cur_node_matrix[y * GameConstants.MapSize / 2 + x] = SeekPath.node_count;
			//cur_seek_path.open_node_list.insert_node(succNode);
			SeekPath.open_node_list.insert_node(succNode);
			// Add oldNode to the list of BestNode's child_noderen (or succNodes).
			for (c = 0; c < SeekPath.MAX_CHILD_NODE && child_node[c] != null; c++)
			{
			}

			child_node[c] = succNode;
		}

		return false;
	}

	public void propagate_down()
	{
		Node childNode, fatherNode;
		int c, g = node_g; // alias.
		int cost;
		int xShift, yShift; // the x, y difference between parent and child nodes

		int childEnterDirection = 0;
		int exitDirection;
		int testResult;

		for (c = 0; c < 8; c++)
		{
			if ((childNode = child_node[c]) == null) // create alias for faster access.
				break;

			cost = 2; // in fact, may be 1 or 2
			if (g + cost <= childNode.node_g) // first checking
			{
				xShift = childNode.node_x - node_x;
				yShift = childNode.node_y - node_y;
				//---------- calulate the new enter direction ------------//
				switch (yShift)
				{
					case -1:
						childEnterDirection = 3 - xShift;
						break;

					case 0:
						childEnterDirection = 3 - (xShift << 1);
						break;

					case 1:
						childEnterDirection = 7 + xShift;
						break;
				}

				exitDirection = (childEnterDirection + 3) % 8 + 1;

				if (enter_direction > exitDirection)
				{
					if ((enter_direction == 8 && (exitDirection == 1 || exitDirection == 2)) ||
					    (enter_direction == 7 && exitDirection == 1))
						testResult = exitDirection + 8 - enter_direction;
					else
						testResult = enter_direction - exitDirection;
				}
				else
				{
					if ((exitDirection == 8 && (enter_direction == 1 || enter_direction == 2)) ||
					    (exitDirection == 7 && enter_direction == 1))
						testResult = enter_direction + 8 - exitDirection;
					else
						testResult = exitDirection - enter_direction;
				}

				if (exitDirection % 2 != 0 && testResult == 2)
				{
					int upperLeftX = 2 * node_x;
					int upperLeftY = 2 * node_y;
					// this case only occurs at the edge
					switch (childEnterDirection)
					{
						case 1:
							if ((exitDirection == 3 && can_move_to(upperLeftX, upperLeftY + 1)) ||
							    (exitDirection == 7 && can_move_to(upperLeftX, upperLeftY)))
								cost = 1;
							break;

						case 3:
							if ((exitDirection == 1 && can_move_to(upperLeftX, upperLeftY + 1)) ||
							    (exitDirection == 5 && can_move_to(upperLeftX + 1, upperLeftY + 1)))
								cost = 1;
							break;

						case 5:
							if ((exitDirection == 3 && can_move_to(upperLeftX + 1, upperLeftY + 1)) ||
							    (exitDirection == 7 && can_move_to(upperLeftX + 1, upperLeftY)))
								cost = 1;
							break;

						case 7:
							if ((exitDirection == 1 && can_move_to(upperLeftX, upperLeftY)) ||
							    (exitDirection == 5 && can_move_to(upperLeftX + 1, upperLeftY)))
								cost = 1;
							break;

						default:
							break;
					}
				}
				else
					cost = 2 - (testResult <= 1 ? 1 : 0); //if(testResult <= 1) cost = 1;

				if (g + cost < childNode.node_g) // second checking, mainly for cost = 2;
				{
					childNode.node_g = g + cost;
					childNode.node_f = childNode.node_g + childNode.node_h;
					childNode.parent_node = this; // reset parent to new path.
					childNode.enter_direction = childEnterDirection;
					stack_push(childNode); // Now the childNode's branch need to be checked out. Remember the new cost must be propagated down.
				}
			}
		}

		while (cur_stack_pos > 0)
		{
			fatherNode = stack_pop();
			g = fatherNode.node_g;

			for (c = 0; c < 8; c++)
			{
				if ((childNode = fatherNode.child_node[c]) == null) // we may stop the propagation 2 ways: either
					break;

				cost = 2; // in fact, may be 1 or 2
				if (g + cost <= childNode.node_g) // first checking
					// there are no children, or that the g value of the child is equal or better than the cost we're propagating
				{
					xShift = childNode.node_x - fatherNode.node_x;
					yShift = childNode.node_y - fatherNode.node_y;
					//---------- calulate the new enter direction ------------//
					switch (yShift)
					{
						case -1:
							childEnterDirection = 3 - xShift;
							break;

						case 0:
							childEnterDirection = 3 - (xShift << 1);
							break;

						case 1:
							childEnterDirection = 7 + xShift;
							break;
					}

					exitDirection = (childEnterDirection + 3) % 8 + 1;

					int fatherEnterDirection = fatherNode.enter_direction;
					if (fatherEnterDirection > exitDirection)
					{
						if ((fatherEnterDirection == 8 && (exitDirection == 1 || exitDirection == 2)) ||
						    (fatherEnterDirection == 7 && exitDirection == 1))
							testResult = exitDirection + 8 - fatherEnterDirection;
						else
							testResult = fatherEnterDirection - exitDirection;
					}
					else
					{
						if ((exitDirection == 8 && (fatherEnterDirection == 1 || fatherEnterDirection == 2)) ||
						    (exitDirection == 7 && fatherEnterDirection == 1))
							testResult = fatherEnterDirection + 8 - exitDirection;
						else
							testResult = exitDirection - fatherEnterDirection;
					}

					if (exitDirection % 2 != 0 && testResult == 2)
					{
						int upperLeftX = 2 * fatherNode.node_x;
						int upperLeftY = 2 * fatherNode.node_y;
						// this case only occurs at the edge
						switch (childEnterDirection)
						{
							case 1:
								if ((exitDirection == 3 && can_move_to(upperLeftX, upperLeftY + 1)) ||
								    (exitDirection == 7 && can_move_to(upperLeftX, upperLeftY)))
									cost = 1;
								break;

							case 3:
								if ((exitDirection == 1 && can_move_to(upperLeftX, upperLeftY + 1)) ||
								    (exitDirection == 5 && can_move_to(upperLeftX + 1, upperLeftY + 1)))
									cost = 1;
								break;

							case 5:
								if ((exitDirection == 3 && can_move_to(upperLeftX + 1, upperLeftY + 1)) ||
								    (exitDirection == 7 && can_move_to(upperLeftX + 1, upperLeftY)))
									cost = 1;
								break;

							case 7:
								if ((exitDirection == 1 && can_move_to(upperLeftX, upperLeftY)) ||
								    (exitDirection == 5 && can_move_to(upperLeftX + 1, upperLeftY)))
									cost = 1;
								break;

							default:
								break;
						}
					}
					else
						cost = 2 - (testResult <= 1 ? 1 : 0); //if(testResult <= 1) cost = 1;

					if (g + cost < childNode.node_g) // second checking, mainly for cost = 2;
					{
						childNode.node_g = g + cost;
						childNode.node_f = childNode.node_g + childNode.node_h;
						childNode.parent_node = fatherNode;
						childNode.enter_direction = childEnterDirection;
						stack_push(childNode);
					}
				}
			}
		}
	}

	//------------- for scale 2 --------------//
	public bool generate_successors2(int x, int y)
	{
		bool hasLeft = node_x > SeekPath.cur_border_x1;
		bool hasRight = node_x < SeekPath.cur_border_x2;
		bool hasUp = node_y > SeekPath.cur_border_y1;
		bool hasDown = node_y < SeekPath.cur_border_y2;
		int upperLeftX, upperLeftY;
		int cost = 2;

		upperLeftX = node_x << 1;
		upperLeftY = node_y << 1;

		if (hasLeft)
		{
			//--------- Left --------//
			if (can_move_to(upperLeftX - 2, upperLeftY) && can_move_to(upperLeftX - 1, upperLeftY))
			{
				if (generate_succ2(node_x - 1, node_y))
					return true;
			}

			//------- Upper-Left ---------//
			if (hasUp)
			{
				// can pass through the tile
				if (can_move_to(upperLeftX - 2, upperLeftY - 2) && can_move_to(upperLeftX - 1, upperLeftY - 1))
				{
					if (generate_succ2(node_x - 1, node_y - 1))
						return true;
				}
			}

			//--------- Lower-Left ----------//
			if (hasDown)
			{
				if (can_move_to(upperLeftX - 2, upperLeftY + 2) && can_move_to(upperLeftX - 1, upperLeftY + 1))
				{
					if (generate_succ2(node_x - 1, node_y + 1))
						return true;
				}
			}
		}

		if (hasRight)
		{
			//----------- Right -----------//
			if (can_move_to(upperLeftX + 2, upperLeftY) && can_move_to(upperLeftX + 1, upperLeftY))
			{
				if (generate_succ2(node_x + 1, node_y))
					return true;
			}

			//-------- Upper-Right ---------//
			if (hasUp)
			{
				if (can_move_to(upperLeftX + 2, upperLeftY - 2) && can_move_to(upperLeftX + 1, upperLeftY - 1))
				{
					if (generate_succ2(node_x + 1, node_y - 1))
						return true;
				}
			}

			//--------- Lower-Right ---------//
			if (hasDown)
			{
				if (can_move_to(upperLeftX + 2, upperLeftY + 2) && can_move_to(upperLeftX + 1, upperLeftY + 1))
				{
					if (generate_succ2(node_x + 1, node_y + 1))
						return true;
				}
			}
		}

		//---------- Upper -----------//
		if (hasUp)
		{
			if (can_move_to(upperLeftX, upperLeftY - 2) && can_move_to(upperLeftX, upperLeftY - 1))
			{
				if (generate_succ2(node_x, node_y - 1))
					return true;
			}
		}

		//---------- Lower -----------//
		if (hasDown)
		{
			if (can_move_to(upperLeftX, upperLeftY + 2) && can_move_to(upperLeftX, upperLeftY + 1))
			{
				if (generate_succ2(node_x, node_y + 1))
					return true;
			}
		}

		return false;
	}

	public bool generate_succ2(int x, int y, int cost = 1)
	{
		//----- if it points back to this node's parent ----//
		if (parent_node != null)
		{
			if (parent_node.node_x == x && parent_node.node_y == y)
				return false;
		}

		//----- if there is an existing node at the given position ----//
		//int upperLeftX, upperLeftY;
		int c, g = node_g + 1; // g(Successor)=g(BestNode)+cost of getting from BestNode to Successor
		int nodeRecno;

		if ((nodeRecno = SeekPath.cur_node_matrix[y * GameConstants.MapSize / 2 + x]) > 0 && nodeRecno < SeekPath.max_node_num)
		{
			Node oldNode = SeekPath.cur_node_array[nodeRecno - 1];

			//------ Add oldNode to the list of BestNode's child_noderen (or Successors).
			for (c = 0; c < SeekPath.MAX_CHILD_NODE && child_node[c] != null; c++)
			{
			}

			child_node[c] = oldNode;

			//---- if our new g value is < oldNode's then reset oldNode's parent to point to BestNode
			if (g < oldNode.node_g)
			{
				oldNode.parent_node = this;
				oldNode.node_g = g;
				oldNode.node_f = g + oldNode.node_h;

				//-------- if it's a closed node ---------//
				if (oldNode.child_node[0] != null)
				{
					//-------------------------------------------------//
					// Since we changed the g value of oldNode, we need
					// to propagate this new value downwards, i.e.
					// do a Depth-First traversal of the tree!
					//-------------------------------------------------//
					oldNode.propagate_down();
					//sys_yield();
				}
			}
		}
		else //------------ add a new node -----------//
		{
			Node succNode = SeekPath.cur_node_array[SeekPath.node_count++];

			//memset(succNode, 0, sizeof(Node));

			succNode.parent_node = this;
			succNode.node_g = g;
			// should do sqrt(), but since we don't really
			succNode.node_h = (x - SeekPath.cur_dest_x) * (x - SeekPath.cur_dest_x) + (y - SeekPath.cur_dest_y) * (y - SeekPath.cur_dest_y);
			succNode.node_f = g + succNode.node_h; // care about the distance but just which branch looks
			succNode.node_x = x; // better this should suffice. Anyayz it's faster.
			succNode.node_y = y;
			succNode.enter_direction = 0;
			succNode.node_type = 0;
			for (int i = 0; i < succNode.child_node.Length; i++)
			{
				succNode.child_node[i] = null;
			}

			succNode.next_node = null;

			// path-reuse node found, but checking of can_walk(final_dest_?) is requested
			if (SeekPath.search_mode == SeekPath.SEARCH_MODE_REUSE && nodeRecno > SeekPath.max_node_num)
			{
				SeekPath.final_dest_x = x << 1;
				SeekPath.final_dest_y = y << 1;

				if (can_move_to(SeekPath.final_dest_x, SeekPath.final_dest_y)) // can_walk the connection point, accept this case
				{
					SeekPath.reuse_result_node_ptr = succNode;
					return true;
				} // else continue until reuse node is found and connection point can be walked
			}

			SeekPath.cur_node_matrix[y * GameConstants.MapSize / 2 + x] = SeekPath.node_count;
			SeekPath.open_node_list.insert_node(succNode);
			// Add oldNode to the list of BestNode's child_noderen (or succNodes).
			for (c = 0; c < SeekPath.MAX_CHILD_NODE && child_node[c] != null; c++)
			{
			}

			child_node[c] = succNode;
		}

		return false;
	}

	public void propagate_down2()
	{
		Node childNode, fatherNode;
		int c, g = node_g; // alias.
		int cost = 1;

		for (c = 0; c < 8; c++)
		{
			if ((childNode = child_node[c]) == null) // create alias for faster access.
				break;

			if (g + cost < childNode.node_g)
			{
				if (can_move_to(node_x + childNode.node_x, node_y + childNode.node_y))
				{
					childNode.node_g = g + cost;
					childNode.node_f = childNode.node_g + childNode.node_h;
					childNode.parent_node = this; // reset parent to new path.

					stack_push(childNode); // Now the childNode's branch need to be
				}
			} // checked out. Remember the new cost must be propagated down.
		}

		while (cur_stack_pos > 0)
		{
			fatherNode = stack_pop();
			g = fatherNode.node_g;

			for (c = 0; c < 8; c++)
			{
				if ((childNode = fatherNode.child_node[c]) == null) // we may stop the propagation 2 ways: either
					break;

				if (g + cost < childNode.node_g) // there are no children, or that the g value of
				{
					// the child is equal or better than the cost we're propagating
					if (can_move_to(node_x + childNode.node_x, node_y + childNode.node_y))
					{
						childNode.node_g = g + cost;
						childNode.node_f = childNode.node_g + childNode.node_h;
						childNode.parent_node = fatherNode;
						stack_push(childNode);
					}
				}
			}
		}
	}
}