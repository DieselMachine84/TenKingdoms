using System;

namespace TenKingdoms;

public partial class Unit
{
	public void MoveTo(int destLocX, int destLocY, int preserveAction = 0, int searchMode = SeekPath.SEARCH_MODE_IN_A_GROUP, int miscNo = 0, int numOfPath = 1)
	{
		//---------- reset way point array since new action is assigned --------//
		if (wayPoints.Count > 0)
		{
			World.GetLocXAndLocY(wayPoints[0], out var locX, out var locY);
			if (locX != destLocX || locY != destLocY)
				ResetWayPoints();
		}

		if (is_unit_dead())
			return;

		//----------------------------------------------------------------//
		// calculate new destination if trying to move to different territory
		//----------------------------------------------------------------//
		Location curLocation = World.GetLoc(NextLocX, NextLocY);
		Location destLocation = World.GetLoc(destLocX, destLocY);

		if (curLocation.RegionId != destLocation.RegionId && MobileType != UnitConstants.UNIT_AIR) // different territory
			DifferentTerritoryDestination(ref destLocX, ref destLocY);

		//-----------------------------------------------------------------------------------//
		// The codes here is used to check for equal action in movement.
		//
		// mainly checked by action_mode2. If previous action is ACTION_MOVE, action_mode2,
		// action_para2, action_x_loc2 and action_x_loc2 need to be kept for this checking.
		//
		// If calling from UnitArray.MoveTo(), action_mode is set to ACTION_MOVE, action_para
		// is set to 0 while action_x_loc and action_y_loc are kept as original value for checking.
		// Meanwhile, action_mode2, action_para2, action_x_loc2 and action_y_loc2 are kept if
		// the condition is fulfilled (action_mode2==ACTION_MOVE)
		//-----------------------------------------------------------------------------------//
		if (action_mode2 == UnitConstants.ACTION_MOVE && action_mode == UnitConstants.ACTION_MOVE)
		{
			//------ previous action is ACTION_MOVE -------//
			if (action_x_loc2 == destLocX && action_y_loc2 == destLocY)
			{
				//-------- equal order --------//
				action_x_loc = action_x_loc2;
				action_y_loc = action_y_loc2;

				if (CurAction != SPRITE_IDLE)
				{
					//-------- the old order is processing --------//
					if (PathNodes.Count == 0) // cannot move
					{
						if (UnitRes[unit_id].unit_class == UnitConstants.UNIT_CLASS_SHIP)
						{
							if (CurAction != SPRITE_SHIP_EXTRA_MOVE)
							{
								if (CurX != NextX || CurY != NextY)
									set_move();
								else
									set_idle();
							}
							//else keep extra_moving
						}
						else
						{
							if (CurX != NextX || CurY != NextY)
								set_move();
							else
								set_idle();
						}
					}

					return;
				} //else action is hold due to some problems, re-activiate again
			}
		} //else, new order or searching is required

		move_action_call_flag = true; // set flag to avoid calling move_to_my_loc()

		action_mode2 = UnitConstants.ACTION_MOVE;
		action_para2 = 0;

		Search(destLocX, destLocY, preserveAction, searchMode, miscNo, numOfPath);
		move_action_call_flag = false;

		//----------------------------------------------------------------//
		// store new order in action parameters
		//----------------------------------------------------------------//
		action_mode = UnitConstants.ACTION_MOVE;
		action_para = 0;
		action_x_loc = action_x_loc2 = move_to_x_loc;
		action_y_loc = action_y_loc2 = move_to_y_loc;
	}
	
	public void MoveToUnitSurround(int destXLoc, int destYLoc, int width, int height, int miscNo = 0, int readyDist = 0)
	{
		//----------------------------------------------------------------//
		// calculate new destination if trying to move to different territory
		//----------------------------------------------------------------//
		Location loc = World.GetLoc(destXLoc, destYLoc);
		if (World.GetLoc(NextLocX, NextLocY).RegionId != loc.RegionId)
		{
			MoveTo(destXLoc, destYLoc);
			return;
		}

		//----------------------------------------------------------------//
		// return if the unit is dead
		//----------------------------------------------------------------//
		if (is_unit_dead())
			return;

		//----------------------------------------------------------------//
		// check for equal actions
		//----------------------------------------------------------------//
		if (action_mode2 == UnitConstants.ACTION_MOVE && action_mode == UnitConstants.ACTION_MOVE)
		{
			//------ previous action is ACTION_MOVE -------//
			if (action_x_loc2 == destXLoc && action_y_loc2 == destYLoc)
			{
				//-------- equal order --------//
				action_x_loc = action_x_loc2;
				action_y_loc = action_y_loc2;

				if (CurAction != SPRITE_IDLE)
				{
					//-------- the old order is processing --------//
					if (PathNodes.Count == 0) // cannot move
					{
						set_idle();
					}

					return;
				} //else action is hold due to some problems, re-activiate again
			}
		} //else, new order or searching is required

		int destX = Math.Max(0, ((width > 1) ? destXLoc : destXLoc - width + 1));
		int destY = Math.Max(0, ((height > 1) ? destYLoc : destYLoc - height + 1));

		Unit unit = UnitArray[miscNo];
		SpriteInfo spriteInfo = unit.SpriteInfo;
		stop();
		SetMoveToSurround(destX, destY, spriteInfo.LocWidth, spriteInfo.LocHeight, UnitConstants.BUILDING_TYPE_VEHICLE);

		//----------------------------------------------------------------//
		// store new order in action parameters
		//----------------------------------------------------------------//
		action_mode = action_mode2 = UnitConstants.ACTION_MOVE;
		action_para = action_para2 = 0;
		action_x_loc = action_x_loc2 = move_to_x_loc;
		action_y_loc = action_y_loc2 = move_to_y_loc;
	}
	
	public void MoveToFirmSurround(int destXLoc, int destYLoc, int width, int height, int miscNo = 0, int readyDist = 0)
	{
		//----------------------------------------------------------------//
		// calculate new destination if trying to move to different territory
		//----------------------------------------------------------------//
		Location loc = World.GetLoc(destXLoc, destYLoc);
		if (UnitRes[unit_id].unit_class == UnitConstants.UNIT_CLASS_SHIP && miscNo == Firm.FIRM_HARBOR)
		{
			Firm firm = FirmArray[loc.FirmId()];
			FirmHarbor harbor = (FirmHarbor)firm;
			if (World.GetLoc(NextLocX, NextLocY).RegionId != harbor.sea_region_id)
			{
				MoveTo(destXLoc, destYLoc);
				return;
			}
		}
		else
		{
			if (World.GetLoc(NextLocX, NextLocY).RegionId != loc.RegionId)
			{
				MoveTo(destXLoc, destYLoc);
				return;
			}
		}

		//----------------------------------------------------------------//
		// return if the unit is dead
		//----------------------------------------------------------------//
		if (is_unit_dead())
			return;

		//----------------------------------------------------------------//
		// check for equal actions
		//----------------------------------------------------------------//
		if (action_mode2 == UnitConstants.ACTION_MOVE && action_mode == UnitConstants.ACTION_MOVE)
		{
			//------ previous action is ACTION_MOVE -------//
			if (action_x_loc2 == destXLoc && action_y_loc2 == destYLoc)
			{
				//-------- equal order --------//
				action_x_loc = action_x_loc2;
				action_y_loc = action_y_loc2;

				if (CurAction != SPRITE_IDLE)
				{
					//-------- the old order is processing --------//
					if (PathNodes.Count == 0) // cannot move
					{
						set_idle();
					}

					return;
				} //else action is hold due to some problems, re-activiate again
			}
		} //else, new order or searching is required

		int destX = Math.Max(0, ((width > 1) ? destXLoc : destXLoc - width + 1));
		int destY = Math.Max(0, ((height > 1) ? destYLoc : destYLoc - height + 1));

		FirmInfo firmInfo = FirmRes[miscNo];
		stop();
		SetMoveToSurround(destX, destY, firmInfo.loc_width, firmInfo.loc_height, UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO, miscNo);

		//----------------------------------------------------------------//
		// store new order in action parameters
		//----------------------------------------------------------------//
		action_mode = action_mode2 = UnitConstants.ACTION_MOVE;
		action_para = action_para2 = 0;
		action_x_loc = action_x_loc2 = move_to_x_loc;
		action_y_loc = action_y_loc2 = move_to_y_loc;
	}

	public void MoveToTownSurround(int destXLoc, int destYLoc, int width, int height, int miscNo = 0, int readyDist = 0)
	{
		//----------------------------------------------------------------//
		// calculate new destination if trying to move to different territory
		//----------------------------------------------------------------//
		Location loc = World.GetLoc(destXLoc, destYLoc);
		if (World.GetLoc(NextLocX, NextLocY).RegionId != loc.RegionId)
		{
			MoveTo(destXLoc, destYLoc);
			return;
		}

		//----------------------------------------------------------------//
		// return if the unit is dead
		//----------------------------------------------------------------//
		if (is_unit_dead())
			return;

		//----------------------------------------------------------------//
		// check for equal actions
		//----------------------------------------------------------------//
		if (action_mode2 == UnitConstants.ACTION_MOVE && action_mode == UnitConstants.ACTION_MOVE)
		{
			//------ previous action is ACTION_MOVE -------//
			if (action_x_loc2 == destXLoc && action_y_loc2 == destYLoc)
			{
				//-------- equal order --------//
				action_x_loc = action_x_loc2;
				action_y_loc = action_y_loc2;

				if (CurAction != SPRITE_IDLE)
				{
					//-------- the old order is processing --------//
					if (PathNodes.Count == 0) // cannot move
					{
						set_idle();
					}

					return;
				} //else action is hold due to some problems, re-activiate again
			}
		} //else, new order or searching is required

		int destX = Math.Max(0, ((width > 1) ? destXLoc : destXLoc - width + 1));
		int destY = Math.Max(0, ((height > 1) ? destYLoc : destYLoc - height + 1));

		stop();
		SetMoveToSurround(destX, destY, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, UnitConstants.BUILDING_TYPE_TOWN_MOVE_TO);

		//----------------------------------------------------------------//
		// store new order in action parameters
		//----------------------------------------------------------------//
		action_mode = action_mode2 = UnitConstants.ACTION_MOVE;
		action_para = action_para2 = 0;
		action_x_loc = action_x_loc2 = move_to_x_loc;
		action_y_loc = action_y_loc2 = move_to_y_loc;
	}

	public void MoveToWallSurround(int destXLoc, int destYLoc, int width, int height, int miscNo = 0, int readyDist = 0)
	{
		//----------------------------------------------------------------//
		// calculate new destination if trying to move to different territory
		//----------------------------------------------------------------//
		Location loc = World.GetLoc(destXLoc, destYLoc);
		if (World.GetLoc(NextLocX, NextLocY).RegionId != loc.RegionId)
		{
			MoveTo(destXLoc, destYLoc);
			return;
		}

		//----------------------------------------------------------------//
		// return if the unit is dead
		//----------------------------------------------------------------//
		if (is_unit_dead())
			return;

		//----------------------------------------------------------------//
		// check for equal actions
		//----------------------------------------------------------------//
		if (action_mode2 == UnitConstants.ACTION_MOVE && action_mode == UnitConstants.ACTION_MOVE)
		{
			//------ previous action is ACTION_MOVE -------//
			if (action_x_loc2 == destXLoc && action_y_loc2 == destYLoc)
			{
				//-------- equal order --------//
				action_x_loc = action_x_loc2;
				action_y_loc = action_y_loc2;

				if (CurAction != SPRITE_IDLE)
				{
					//-------- the old order is processing --------//
					if (PathNodes.Count == 0) // cannot move
					{
						set_idle();
					}

					return;
				} //else action is hold due to some problems, re-activiate again
			}
		} //else, new order or searching is required

		int destX = Math.Max(0, (width > 1) ? destXLoc : destXLoc - width + 1);
		int destY = Math.Max(0, (height > 1) ? destYLoc : destYLoc - height + 1);

		stop();
		SetMoveToSurround(destX, destY, 1, 1, UnitConstants.BUILDING_TYPE_WALL);

		//----------------------------------------------------------------//
		// store new order in action parameters
		//----------------------------------------------------------------//
		action_mode = action_mode2 = UnitConstants.ACTION_MOVE;
		action_para = action_para2 = 0;
		action_x_loc = action_x_loc2 = move_to_x_loc;
		action_y_loc = action_y_loc2 = move_to_y_loc;
	}
	
	private int SetMoveToSurround(int buildLocX, int buildLocY, int width, int height, int buildingType,
		int miscNo = 0, int readyDist = 0, int curProcessUnitNum = 1)
	{
		//--------------------------------------------------------------//
		// calculate the distance from the object
		//--------------------------------------------------------------//
		// 0 for inside, 1 for surrounding, >1 for the rest
		int distance = cal_distance(buildLocX, buildLocY, width, height);

		//--------------------------------------------------------------//
		// inside the building
		//--------------------------------------------------------------//
		if (distance == 0)
		{
			ResetPath();
			if (CurX == NextX && CurY == NextY)
				set_idle();

			return 1;
		}

		if (distance > 1)
		{
			//--------------------------------------------------------------//
			// the searching is divided into 2 parts.
			//
			// part 1 using the firm_type and firm_id to find a shortest path.
			// 
			// part 2
			// if the width and height is the actual width and height of the
			// firm, the unit moves to the surrounding of the firm.
			//
			// if the width and height > the actual width and height of the
			// firm, the unit moves to a location far away from the surrounding
			// of the firm.
			//--------------------------------------------------------------//

			//====================================================================//
			// part 1
			//====================================================================//

			Location location = World.GetLoc(buildLocX, buildLocY);
			int searchResult = 0;

			switch (buildingType)
			{
				case UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO: // (assign) firm is on the location
					Firm targetFirm = FirmArray[location.FirmId()];
					searchResult = Search(buildLocX, buildLocY, 1, SeekPath.SEARCH_MODE_TO_FIRM, targetFirm.firm_id, curProcessUnitNum);
					break;

				case UnitConstants.BUILDING_TYPE_FIRM_BUILD: // (build firm) no firm on the location
					searchResult = Search(buildLocX, buildLocY, 1, SeekPath.SEARCH_MODE_TO_FIRM, miscNo);
					break;

				case UnitConstants.BUILDING_TYPE_TOWN_MOVE_TO: // (assign) town is on the location
					Town targetTown = TownArray[location.TownId()];
					searchResult = Search(buildLocX, buildLocY, 1, SeekPath.SEARCH_MODE_TO_TOWN, targetTown.TownId, curProcessUnitNum);
					break;

				case UnitConstants.BUILDING_TYPE_SETTLE: // (settle, first unit) no town on the location
					//---------------------------------------------------------------------//
					// the record number sent to the searching algorithm is used to determine
					// the width and the height of the building. However, the standard
					// dimension for settling is used and the building built is a type of
					// town. Thus, passing -1 as the miscNo to show that "settle" is processed
					//---------------------------------------------------------------------//
					searchResult = Search(buildLocX, buildLocY, 1, SeekPath.SEARCH_MODE_TO_TOWN, -1, curProcessUnitNum);
					break;

				case UnitConstants.BUILDING_TYPE_VEHICLE:
					searchResult = Search(buildLocX, buildLocY, 1, SeekPath.SEARCH_MODE_TO_VEHICLE, location.CargoId);
					break;

				case UnitConstants.BUILDING_TYPE_WALL: // wall is on the location
					searchResult = Search(buildLocX, buildLocY, 1,
						miscNo != 0 ? SeekPath.SEARCH_MODE_TO_WALL_FOR_UNIT : SeekPath.SEARCH_MODE_TO_WALL_FOR_GROUP);
					break;
			}

			if (searchResult == 0)
				return 0; // incomplete searching

			//====================================================================//
			// part 2
			//====================================================================//
			return PathNodes.Count > 0 ? EditPathToSurround(buildLocX, buildLocY,
					buildLocX + width - 1, buildLocY + height - 1, readyDist) : 0;
		}
		else // in the surrounding, no need to move
		{
			ResetPath();

			if (CurX == NextX && CurY == NextY)
			{
				move_to_x_loc = NextLocX;
				move_to_y_loc = NextLocY;
				GoX = CurX;
				GoY = CurY;
				set_idle();
				SetDir(move_to_x_loc, move_to_y_loc, buildLocX + width / 2, buildLocY + height / 2);
			}

			return 1;
		}
	}
	
	private int EditPathToSurround(int objectXLoc1, int objectYLoc1, int objectXLoc2, int objectYLoc2, int readyDist)
	{
		if (PathNodes.Count < 2)
			return 0;

		//----------------------------------------------------------------------------//
		// At this moment, the unit generally has a path to the location inside the object,
		// walk through it and extract a path to the surrounding of the object.
		//----------------------------------------------------------------------------//

		//------- calculate the surrounding top-left and bottom-right points ------//
		int moveScale = MoveStepCoeff();
		int xLoc1 = objectXLoc1 - readyDist - 1;
		int yLoc1 = objectYLoc1 - readyDist - 1;
		int xLoc2 = objectXLoc2 + readyDist + 1;
		int yLoc2 = objectYLoc2 + readyDist + 1;

		//------------------- boundary checking -------------------//
		if (xLoc1 < 0)
			xLoc1 = 0;
		if (yLoc1 < 0)
			yLoc1 = 0;
		if (xLoc2 >= GameConstants.MapSize)
			yLoc1 = GameConstants.MapSize - moveScale;
		if (yLoc2 >= GameConstants.MapSize)
			xLoc2 = GameConstants.MapSize - moveScale;

		//--------------- adjust for air and sea units -----------------//
		if (MobileType != UnitConstants.UNIT_LAND)
		{
			//------ assume even x, y coordinate is used for UnitConstants.UNIT_SEA and UnitConstants.UNIT_AIR -------//
			if (xLoc1 % 2 != 0)
				xLoc1--;
			if (yLoc1 % 2 != 0)
				yLoc1--;
			if (xLoc2 % 2 != 0)
				xLoc2++;
			if (yLoc2 % 2 != 0)
				yLoc2++;

			if (xLoc2 > GameConstants.MapSize - moveScale)
				xLoc2 = GameConstants.MapSize - moveScale;
			if (yLoc2 > GameConstants.MapSize - moveScale)
				yLoc2 = GameConstants.MapSize - moveScale;
		}

		int checkXLoc = NextLocX;
		int checkYLoc = NextLocY;
		int editNode1Index = 0;
		int editNode2Index = 1;
		int editNode1 = PathNodes[editNode1Index]; // alias the unit's result_node_array
		World.GetLocXAndLocY(editNode1, out int editNode1LocX, out int editNode1LocY);
		int editNode2 = PathNodes[editNode2Index]; // ditto
		World.GetLocXAndLocY(editNode2, out int editNode2LocX, out int editNode2LocY);

		int hasMoveStep = 0;
		if (checkXLoc != editNode1LocX || checkYLoc != editNode1LocY)
		{
			hasMoveStep += moveScale;
			checkXLoc = editNode1LocX;
			checkYLoc = editNode1LocY;
		}

		int i, j;
		// pathDist - counts the disitance of the generated path, found - whether a path to the surrounding is found
		int pathDist = 0, found = 0;
		int vecX, vecY, xMagn, yMagn, magn;

		//------- find the first node that is on the surrounding of the object -------//
		for (i = 1; i < PathNodes.Count; i++, editNode1Index++, editNode2Index++)
		{
			editNode1 = PathNodes[editNode1Index]; // alias the unit's result_node_array
			World.GetLocXAndLocY(editNode1, out editNode1LocX, out editNode1LocY);
			editNode2 = PathNodes[editNode2Index]; // ditto
			World.GetLocXAndLocY(editNode2, out editNode2LocX, out editNode2LocY);

			//------------ calculate parameters for checking ------------//
			vecX = editNode2LocX - editNode1LocX;
			vecY = editNode2LocY - editNode1LocY;

			magn = ((xMagn = Math.Abs(vecX)) > (yMagn = Math.Abs(vecY))) ? xMagn : yMagn;
			if (xMagn != 0)
			{
				vecX /= xMagn;
				vecX *= moveScale;
			}

			if (yMagn != 0)
			{
				vecY /= yMagn;
				vecY *= moveScale;
			}

			//------------- check each location between editNode1 and editNode2 -------------//
			for (j = 0; j < magn; j += moveScale)
			{
				checkXLoc += vecX;
				checkYLoc += vecY;

				if (checkXLoc >= xLoc1 && checkXLoc <= xLoc2 && checkYLoc >= yLoc1 && checkYLoc <= yLoc2)
				{
					found++;
					break;
				}
			}

			//-------------------------------------------------------------------------------//
			// a path is found, then set unit's parameters for its movement
			//-------------------------------------------------------------------------------//
			if (found != 0)
			{
				PathNodes[editNode2Index] = World.GetMatrixIndex(checkXLoc, checkYLoc);

				if (i == 1) // first editing
				{
					World.GetLocXAndLocY(PathNodes[0], out int firstNodeLocX, out int firstNodeLocY);
					if (CurX == firstNodeLocX * InternalConstants.CellWidth && CurY == firstNodeLocY * InternalConstants.CellHeight)
					{
						GoX = checkXLoc * InternalConstants.CellWidth;
						GoY = checkYLoc * InternalConstants.CellHeight;
					}
				}

				pathDist += (j + moveScale);
				pathDist -= hasMoveStep;
				while (PathNodes.Count > i + 1)
				{
					PathNodes.RemoveAt(PathNodes.Count - 1);
				}
				_pathNodeDistance = pathDist;
				move_to_x_loc = checkXLoc;
				move_to_y_loc = checkYLoc;
				break;
			}
			else
			{
				pathDist += magn;
			}
		}

		return found;
	}

	protected void NextMove()
	{
		if (PathNodes.Count == 0)
			return;

		PathNodeIndex++;
		if (PathNodeIndex == PathNodes.Count)
		{
			//------------ all nodes are visited --------------//
			ResetPath();
			set_idle();

			if (action_mode2 == UnitConstants.ACTION_MOVE) //--------- used to terminate action_mode==ACTION_MOVE
			{
				force_move_flag = false;

				//------- reset ACTION_MOVE parameters ------//
				reset_action_para();
				if (move_to_x_loc == action_x_loc2 && move_to_y_loc == action_y_loc2)
					reset_action_para2();
			}

			return;
		}

		//---- order the unit to move to the next checkpoint following the path ----//

		int resultNode = PathNodes[PathNodeIndex];
		World.GetLocXAndLocY(resultNode, out int resultNodeLocX, out int resultNodeLocY);

		SpriteMove(resultNodeLocX * InternalConstants.CellWidth, resultNodeLocY * InternalConstants.CellHeight);
	}

	private void TerminateMove()
	{
		GoX = NextX;
		GoY = NextY;

		move_to_x_loc = NextLocX;
		move_to_y_loc = NextLocY;

		CurFrame = 1;

		ResetPath();
		set_idle();
	}

	private void MoveToMyLoc(Unit unit)
	{
		int unitDestX, unitDestY;
		if (unit.action_mode2 == UnitConstants.ACTION_MOVE)
		{
			unitDestX = unit.action_x_loc2;
			unitDestY = unit.action_y_loc2;
		}
		else
		{
			unitDestX = unit.move_to_x_loc;
			unitDestY = unit.move_to_y_loc;
		}

		//--------------- init parameters ---------------//
		int unitCurX = unit.NextLocX;
		int unitCurY = unit.NextLocY;
		int destX = action_x_loc2;
		int destY = action_y_loc2;
		int curX = NextLocX;
		int curY = NextLocY;
		int moveScale = MoveStepCoeff();

		//------------------------------------------------------------------//
		// setting for unit pointed by unit
		//------------------------------------------------------------------//
		if (PathNodes.Count == 0) //************BUGHERE
		{
			unit.MoveTo(destX, destY, 1); // unit pointed by unit is idle before calling searching
		}
		else
		{
			//TODO check
			unit.PathNodes.Clear();
			if (GoX != unit.NextX || GoY != unit.NextY)
			{
				unit.PathNodes.Add(World.GetMatrixIndex(unitCurX, unitCurY));
			}
			for (int i = PathNodeIndex; i < PathNodes.Count; i++)
			{
				unit.PathNodes.Add(PathNodes[i]);
			}

			//--------------- set unit action ---------------//
			// unit is idle now
			if (unit.action_mode2 == UnitConstants.ACTION_STOP || unit.action_mode2 == UnitConstants.ACTION_MOVE)
			{
				//---------- activate unit pointed by unit now ------------//
				unit.action_mode = unit.action_mode2 = UnitConstants.ACTION_MOVE;
				unit.action_para = unit.action_para2 = 0;
				if (destX != -1 && destY != -1)
				{
					unit.action_x_loc = unit.action_x_loc2 = destX;
					unit.action_y_loc = unit.action_y_loc2 = destY;
				}
				else
				{
					World.GetLocXAndLocY(unit.PathNodes[^1], out int lastNodeLocX, out int lastNodeLocY);
					unit.action_x_loc = unit.action_x_loc2 = lastNodeLocX;
					unit.action_y_loc = unit.action_y_loc2 = lastNodeLocY;
				}
			}

			//----------------- set unit movement parameters -----------------//
			unit.PathNodeIndex = 0;
			unit._pathNodeDistance = _pathNodeDistance - moveScale;
			unit.move_to_x_loc = move_to_x_loc;
			unit.move_to_y_loc = move_to_y_loc;
			unit.NextMove();
		}

		//------------------------------------------------------------------//
		// setting for this unit
		//------------------------------------------------------------------//
		int shouldWait = 0;
		if (NextX == unit.CurX && NextY == unit.CurY)
		{
			ResetPath();
			_pathNodeDistance = 0;
		}
		else
		{
			TerminateMove();
			shouldWait++;
			_pathNodeDistance = moveScale;
		}

		GoX = unit.CurX;
		GoY = unit.CurY;
		move_to_x_loc = unitCurX;
		move_to_y_loc = unitCurY;

		if (action_mode2 == UnitConstants.ACTION_MOVE)
		{
			action_x_loc = action_x_loc2 = unitDestX;
			action_y_loc = action_y_loc2 = unitDestY;
		}

		//---------- note: the cur_dir is already the correct direction ---------------//
		PathNodes.Clear();
		PathNodes.Add(World.GetMatrixIndex(curX, curY));
		PathNodes.Add(World.GetMatrixIndex(unitCurX, unitCurY));
		//TODO check this
		PathNodeIndex = 1;
		if (shouldWait != 0)
			set_wait(); // wait for the blocking unit to move first
	}
	
	private int MoveToRangeAttack(int targetXLoc, int targetYLoc, int miscNo, int searchMode, int maxRange)
	{
		//---------------------------------------------------------------------------------//
		// part 1, searching
		//---------------------------------------------------------------------------------//
		SeekPath.SetAttackRange(maxRange);
		Search(targetXLoc, targetYLoc, 1, searchMode, miscNo);
		SeekPath.ResetAttackRange();
		//search(targetXLoc, targetYLoc, 1, searchMode, maxRange);

		if (PathNodes.Count == 0)
			return 0;

		//---------------------------------------------------------------------------------//
		// part 2, editing result path
		//---------------------------------------------------------------------------------//
		Location loc = World.GetLoc(NextLocX, NextLocY);

		int regionId = loc.RegionId; // the region_id this unit in

		//----------------------------------------------------//
		int editNode1Index = PathNodes.Count - 1;
		int editNode2Index = PathNodes.Count - 2;
		int editNode1 = PathNodes[editNode1Index];
		World.GetLocXAndLocY(editNode1, out int editNode1LocX, out int editNode1LocY);
		int editNode2 = PathNodes[editNode2Index];
		World.GetLocXAndLocY(editNode2, out int editNode2LocX, out int editNode2LocY);
		int vecX = editNode1LocX - editNode2LocX;
		int vecY = editNode1LocY - editNode2LocY;

		if (vecX != 0)
			vecX = ((vecX > 0) ? 1 : -1) * MoveStepCoeff();
		if (vecY != 0)
			vecY = ((vecY > 0) ? 1 : -1) * MoveStepCoeff();

		int x = editNode1LocX;
		int y = editNode1LocY;
		int i, found = 0, removedStep = 0, preX = 0, preY = 0;

		for (i = PathNodes.Count; i > 1; i--)
		{
			while (x != editNode2LocX || y != editNode2LocY)
			{
				loc = World.GetLoc(x, y);
				if (loc.RegionId == regionId)
				{
					found = i;
					preX = x;
					preY = y;
					break;
				}

				x -= vecX;
				y -= vecY;
				removedStep++;
			}

			if (found != 0 || i < 3) // found a spot or there is at least more node to try
				break;

			// one more node to try

			editNode1Index = editNode2Index;
			editNode2Index--;
			editNode1 = PathNodes[editNode1Index];
			World.GetLocXAndLocY(editNode1, out editNode1LocX, out editNode1LocY);
			editNode2 = PathNodes[editNode2Index];
			World.GetLocXAndLocY(editNode2, out editNode2LocX, out editNode2LocY);

			vecX = editNode1LocX - editNode2LocX;
			vecY = editNode1LocY - editNode2LocY;
			if (vecX != 0)
				vecX = ((vecX > 0) ? 1 : -1) * MoveStepCoeff();
			if (vecY != 0)
				vecY = ((vecY > 0) ? 1 : -1) * MoveStepCoeff();

			x = editNode1LocX;
			y = editNode1LocY;
		}

		//---------------------------------------------------------------------------//
		// update unit parameters
		//---------------------------------------------------------------------------//
		if (found != 0)
		{
			while (PathNodes.Count > found)
			{
				PathNodes.RemoveAt(PathNodes.Count - 1);
			}
			int goX = GoX >> InternalConstants.CellWidthShift;
			int goY = GoY >> InternalConstants.CellHeightShift;

			//---------------------------------------------------------------------//
			// note: build?Loc-1, build?Loc+width, build?Loc+height may <0 or
			//			>MAX_WORLD_?_LOC.  To prevent errors from occuring, goX, goY
			//			must not be outside the map boundary
			//---------------------------------------------------------------------//
			if (goX == editNode1LocX && goY == editNode1LocY)
			{
				GoX = preX * InternalConstants.CellWidth;
				GoY = preY * InternalConstants.CellHeight;
			}
			else if (PathNodes.Count == 2)
			{
				int magnCG = Misc.points_distance(CurX, CurY, GoX, GoY);
				int magnNG = Misc.points_distance(NextX, NextY, GoX, GoY);

				if (magnCG != 0 && magnNG != 0)
				{
					//---------- lie on the same line -----------//
					if ((GoX - CurX) / magnCG == (GoX - NextX) / magnNG && (GoY - CurY) / magnCG == (GoY - NextY) / magnNG)
					{
						GoX = preX * InternalConstants.CellWidth;
						GoY = preY * InternalConstants.CellHeight;
					}
				}
			}

			PathNodes[^1] = World.GetMatrixIndex(preX, preY);
			move_to_x_loc = preX;
			move_to_y_loc = preY;

			_pathNodeDistance -= (removedStep) * MoveStepCoeff();
		}

		return found;
	}

	private int TryMoveToRangeAttack(Unit targetUnit)
	{
		int curXLoc = NextLocX;
		int curYLoc = NextLocY;
		int targetXLoc = targetUnit.NextLocX;
		int targetYLoc = targetUnit.NextLocY;

		if (World.GetLoc(curXLoc, curYLoc).RegionId == World.GetLoc(targetXLoc, targetYLoc).RegionId)
		{
			//------------ for same region id, search now ---------------//
			if (Search(targetXLoc, targetYLoc, 1, SeekPath.SEARCH_MODE_TO_ATTACK, action_para) != 0)
				return 1;
			else // search failure,
			{
				stop2(UnitConstants.KEEP_DEFENSE_MODE);
				return 0;
			}
		}
		else
		{
			//--------------- different territory ------------------//
			int targetWidth = targetUnit.SpriteInfo.LocWidth;
			int targetHeight = targetUnit.SpriteInfo.LocHeight;
			int maxRange = max_attack_range();

			if (possible_place_for_range_attack(targetXLoc, targetYLoc, targetWidth, targetHeight, maxRange))
			{
				//---------------------------------------------------------------------------------//
				// space is found, attack target now
				//---------------------------------------------------------------------------------//
				if (MoveToRangeAttack(targetXLoc, targetYLoc, targetUnit.SpriteResId, SeekPath.SEARCH_MODE_ATTACK_UNIT_BY_RANGE, maxRange) != 0)
					return 1;
				else
				{
					stop2(UnitConstants.KEEP_DEFENSE_MODE);
					return 0;
				}

				return 1;
			}
			else
			{
				//---------------------------------------------------------------------------------//
				// unable to find location to attack the target, stop or move to the target
				//---------------------------------------------------------------------------------//
				if (action_mode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
				    action_mode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
				    action_mode2 != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
					MoveTo(targetXLoc, targetYLoc, 1); // abort attacking, just call move_to() instead
				else
					stop2(UnitConstants.KEEP_DEFENSE_MODE);
				return 0;
			}
		}

		return 0;
	}
	
	public void DifferentTerritoryDestination(ref int destX, ref int destY)
	{
		int curXLoc = NextLocX;
		int curYLoc = NextLocY;

		Location loc = World.GetLoc(curXLoc, curYLoc);
		int regionId = loc.RegionId;
		int xStep = destX - curXLoc;
		int yStep = destY - curYLoc;
		int absXStep = Math.Abs(xStep);
		int absYStep = Math.Abs(yStep);
		int count = (absXStep >= absYStep) ? absXStep : absYStep;

		int sameTerr = 0;

		//------------------------------------------------------------------------------//
		// draw a line from the unit location to the destination, find the last location
		// with the same region id.
		//------------------------------------------------------------------------------//
		for (int i = 1; i <= count; i++)
		{
			int x = curXLoc + (i * xStep) / count;
			int y = curYLoc + (i * yStep) / count;

			loc = World.GetLoc(x, y);
			if (loc.RegionId == regionId)
				sameTerr = i;
		}

		if (sameTerr != 0 && count != 0)
		{
			destX = curXLoc + (sameTerr * xStep) / count;
			destY = curYLoc + (sameTerr * yStep) / count;
		}
		else
		{
			destX = curXLoc;
			destY = curYLoc;
		}
	}

	private void HandleBlockedMove(Location blockedLoc)
	{
		//--- check if the tile we are moving at is blocked by a building ---//
		if (blockedLoc.IsFirm() || blockedLoc.IsTown() || blockedLoc.IsWall())
		{
			//------------------------------------------------//
			// firm/town/wall is on the blocked location
			//------------------------------------------------//
			ResetPath();
			SearchOrStop(move_to_x_loc, move_to_y_loc, 1);
			//search(move_to_x_loc, move_to_y_loc, 1);
			return;
		}

		if (NextLocX == move_to_x_loc && NextLocY == move_to_y_loc && swapping == 0)
		{
			TerminateMove(); // terminate since already reaching destination
			return;
		}

		if (!blockedLoc.IsAccessible(MobileType))
		{
			TerminateMove(); // the location is not accessible
			return;
		}

		//-----------------------------------------------------------------------------------//
		// there is another sprite on the move_to location, check the combination of both sizes
		//-----------------------------------------------------------------------------------//
		blocked_by_member = 1;

		Unit unit = UnitArray[blockedLoc.UnitId(MobileType)];
		//if(unit.sprite_info.loc_width>1 || sprite_info.loc_width>1)
		//{
		//	set_wait();
		//	return;
		//}
		//else
		HandleBlockedMoveS11(unit); //------ both units size 1x1

		return;
	}

	private void HandleBlockedMoveS11(Unit unit)
	{
		int waitTerm;
		int moveStep = MoveStepCoeff();

		switch (unit.CurAction)
		{
			//------------------------------------------------------------------------------------//
			// handle blocked for units belonging to the same nation.  For those belonging to other
			// nations, wait for it moving to other locations or search for another path.
			//------------------------------------------------------------------------------------//
			case SPRITE_WAIT: // the blocking unit is waiting
			case SPRITE_TURN:
				if (unit.nation_recno == nation_recno)
					HandleBlockedWait(unit); // check for cycle wait for our nation
				else if (waiting_term >= UnitConstants.MAX_WAITING_TERM_DIFF)
				{
					SearchOrStop(move_to_x_loc, move_to_y_loc, 1); // recall searching
					waiting_term = 0;
				}
				else // wait
					set_wait();

				return;

			//------------------------------------------------------------------------------------//
			// We know from the cur_action of the blocking unit it is moving to another locations,
			// the blocked unit wait for a number of terms or search again.
			//------------------------------------------------------------------------------------//
			case SPRITE_MOVE:
			case SPRITE_READY_TO_MOVE:
			case SPRITE_SHIP_EXTRA_MOVE:
				// don't wait for caravans, and caravans don't wait for other units
				if (unit_id != UnitConstants.UNIT_CARAVAN && unit.unit_id == UnitConstants.UNIT_CARAVAN)
				{
					Search(move_to_x_loc, move_to_y_loc, 1, SeekPath.SEARCH_MODE_A_UNIT_IN_GROUP);
				}
				else
				{
					waitTerm = (nation_recno == unit.nation_recno) ? UnitConstants.MAX_WAITING_TERM_SAME : UnitConstants.MAX_WAITING_TERM_DIFF;
					if (waiting_term >= waitTerm)
					{
						SearchOrWait();
						waiting_term = 0;
					}
					else
						set_wait();
				}

				return;

			//------------------------------------------------------------------------------------//
			// handling blocked for idle unit
			//------------------------------------------------------------------------------------//
			case SPRITE_IDLE:
				if (unit.action_mode == UnitConstants.ACTION_SHIP_TO_BEACH)
				{
					//----------------------------------------------------------------------//
					// the blocking unit is trying to move to beach, so wait a number of terms,
					// or call searching again
					//----------------------------------------------------------------------//
					if (Math.Abs(unit.NextLocX - unit.action_x_loc2) <= moveStep &&
					    Math.Abs(unit.NextLocY - unit.action_y_loc2) <= moveStep &&
					    TerrainRes[World.GetLoc(unit.action_x_loc2, unit.action_y_loc2).TerrainId].average_type != TerrainTypeCode.TERRAIN_OCEAN)
					{
						if (action_mode2 == UnitConstants.ACTION_SHIP_TO_BEACH &&
						    action_x_loc2 == unit.action_x_loc2 && action_y_loc2 == unit.action_y_loc2)
						{
							int tempX, tempY;
							ship_to_beach(action_x_loc2, action_y_loc2, out tempX, out tempY);
						}
						else
						{
							waitTerm = (nation_recno == unit.nation_recno)
								? UnitConstants.MAX_WAITING_TERM_SAME
								: UnitConstants.MAX_WAITING_TERM_DIFF;
							if (waiting_term >= waitTerm)
								stop2();
							else
								set_wait();
						}

						return;
					}
				}

				if (unit.nation_recno == nation_recno) //-------- same nation
				{
					//------------------------------------------------------------------------------------//
					// units from our nation
					//------------------------------------------------------------------------------------//
					if (unit.unit_group_id == unit_group_id)
					{
						//--------------- from the same group -----------------//
						if (wayPoints.Count != 0 && unit.wayPoints.Count == 0)
						{
							//------------ reset way point --------------//
							stop2();
							ResetWayPoints();
						}
						else if ((unit.NextLocX != move_to_x_loc || unit.NextLocY != move_to_y_loc) &&
						         (unit.CurAction == SPRITE_IDLE && unit.action_mode2 == UnitConstants.ACTION_STOP))
							if (ConfigAdv.fix_path_blocked_by_team)
								HandleBlockedByIdleUnit(unit);
							else
								MoveToMyLoc(unit); // push the blocking unit and exchange their destination
						else if (unit.action_mode == UnitConstants.ACTION_SETTLE)
							set_wait(); // wait for the settler
						else if (waiting_term > UnitConstants.MAX_WAITING_TERM_SAME)
						{
							//---------- stop if wait too long ----------//
							TerminateMove();
							waiting_term = 0;
						}
						else
							set_wait();
					}
					else if (unit.action_mode2 == UnitConstants.ACTION_STOP)
						HandleBlockedByIdleUnit(unit);
					else if (wayPoints.Count != 0 && unit.wayPoints.Count == 0)
					{
						stop2();
						ResetWayPoints();
					}
					else
						SearchOrStop(move_to_x_loc, move_to_y_loc, 1); // recall A* algorithm by default mode
				}
				else // different nation
				{
					//------------------------------------------------------------------------------------//
					// units from other nations
					//------------------------------------------------------------------------------------//
					if (unit.NextLocX == move_to_x_loc && unit.NextLocY == move_to_y_loc)
					{
						TerminateMove(); // destination occupied by other unit

						if (action_mode == UnitConstants.ACTION_ATTACK_UNIT &&
						    unit.nation_recno != nation_recno && unit.SpriteId == action_para)
						{
							SetDir(NextX, NextY, unit.NextX, unit.NextY);
							if (IsDirCorrect())
								attack_unit(action_para, 0, 0, true);
							else
								set_turn();
							CurFrame = 1;
						}
					}
					else
						SearchOrStop(move_to_x_loc, move_to_y_loc, 1); // recall A* algorithm by default mode
				}

				return;

			//------------------------------------------------------------------------------------//
			// don't wait for attackers from other nations, search for another path.
			//------------------------------------------------------------------------------------//
			case SPRITE_ATTACK:
				//----------------------------------------------------------------//
				// don't wait for other nation unit, call searching again
				//----------------------------------------------------------------//
				if (nation_recno != unit.nation_recno)
				{
					SearchOrStop(move_to_x_loc, move_to_y_loc, 1);
					return;
				}

				//------------------------------------------------------------------------------------//
				// for attackers owned by our commander, handled blocked case by case as follows.
				//------------------------------------------------------------------------------------//
				switch (unit.action_mode)
				{
					case UnitConstants.ACTION_ATTACK_UNIT:
						if (action_para != 0 && !UnitArray.IsDeleted(action_para))
						{
							Unit target = UnitArray[action_para];
							HandleBlockedAttackUnit(unit, target);
						}
						else
							SearchOrStop(move_to_x_loc, move_to_y_loc, 1, SeekPath.SEARCH_MODE_A_UNIT_IN_GROUP);

						break;

					case UnitConstants.ACTION_ATTACK_FIRM:
						if (unit.action_para == 0 || FirmArray.IsDeleted(unit.action_para))
							set_wait();
						else
							HandleBlockedAttackFirm(unit);
						break;

					case UnitConstants.ACTION_ATTACK_TOWN:
						if (unit.action_para == 0 || TownArray.IsDeleted(unit.action_para))
							set_wait();
						else
							HandleBlockedAttackTown(unit);
						break;

					case UnitConstants.ACTION_ATTACK_WALL:
						if (unit.action_para != 0)
							set_wait();
						else
							HandleBlockedAttackWall(unit);
						break;

					case UnitConstants.ACTION_GO_CAST_POWER:
						set_wait();
						break;

					default:
						break;
				}

				return;

			//------------------------------------------------------------------------------------//
			// the blocked unit can pass after the blocking unit disappears in air.
			//------------------------------------------------------------------------------------//
			case SPRITE_DIE:
				set_wait(); // assume this unit will not wait too long
				return;

			default:
				break;
		}
	}

	private void HandleBlockedByIdleUnit(Unit unit)
	{
		const int TEST_DIMENSION = 10;
		const int TEST_LIMIT = TEST_DIMENSION * TEST_DIMENSION;

		bool notLandUnit = (MobileType != UnitConstants.UNIT_LAND);
		int unitXLoc = unit.NextLocX;
		int unitYLoc = unit.NextLocY;
		int xShift, yShift;
		int checkXLoc, checkYLoc;

		int xSign = Misc.Random(2) != 0 ? 1 : -1;
		int ySign = Misc.Random(2) != 0 ? 1 : -1;

		for (int i = 2; i <= TEST_LIMIT; i++)
		{
			Misc.cal_move_around_a_point(i, TEST_DIMENSION, TEST_DIMENSION, out xShift, out yShift);
			xShift *= xSign;
			yShift *= ySign;

			if (notLandUnit)
			{
				checkXLoc = unitXLoc + xShift * 2;
				checkYLoc = unitYLoc + yShift * 2;
			}
			else
			{
				checkXLoc = unitXLoc + xShift;
				checkYLoc = unitYLoc + yShift;
			}

			if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
				continue;

			Location loc = World.GetLoc(checkXLoc, checkYLoc);
			if (!loc.CanMove(unit.MobileType))
				continue;

			if (OnMyPath(checkXLoc, checkYLoc))
				continue;

			unit.MoveTo(checkXLoc, checkYLoc);
			set_wait();
			return;
		}

		stop(UnitConstants.KEEP_DEFENSE_MODE);

		//--------------------------------------------------------------------------------//
		// improved version!!!
		//--------------------------------------------------------------------------------//
		/*int testResult = 0, worstCase=0;
		int worstXLoc=-1, worstYLoc=-1;
		int startCount, endCount;
		int i, j;
		
		for(j=0; j<2; j++)
		{
			//----------- set the startCount and endCount ------------//
			if(j==0)
			{
				startCount = 2;
				endCount = 9;
			}
			else
			{
				startCount = 10;
				endCount = TEST_LIMIT;
			}
	
			for(i=startCount; i<=endCount; i++)
			{
				misc.cal_move_around_a_point(i, TEST_DIMENSION, TEST_DIMENSION, xShift, yShift);
				if(notLandUnit)
				{
					checkXLoc = unitXLoc + xShift*2;
					checkYLoc = unitYLoc + yShift*2;
				}
				else
				{
					checkXLoc = unitXLoc + xShift;
					checkYLoc = unitYLoc + yShift;
				}
			
				if(checkXLoc<0 || checkXLoc>=MAX_WORLD_X_LOC || checkYLoc<0 || checkYLoc>=MAX_WORLD_Y_LOC)
					continue;
	
				loc = world.get_loc(checkXLoc , checkYLoc);
				if(!loc.can_move(unit.mobile_type))
					continue;
	
				//-------------------------------------------------------------------//
				// a possible location
				//-------------------------------------------------------------------//
				testResult = on_my_path(checkXLoc, checkYLoc);
				if(testResult)
				{
					if(j==0 && !worstCase)
					{
						worstCase++;
						worstXLoc = checkXLoc;
						worstYLoc = checkYLoc;
					}
					continue;
				}
	
				unit.move_to(checkXLoc, checkYLoc):
				set_wait();
				return;
			}
		}
	
		//-------------------------------------------------------------------//
		if(worstCase)
		{
			unit.move_to(worstXLoc, worstYLoc);
			set_wait();
		}
		else
			stop(UnitConstants.KEEP_DEFENSE_MODE);*/
	}

	private bool OnMyPath(int checkXLoc, int checkYLoc)
	{
		for (int i = PathNodeIndex; i < PathNodes.Count; i++)
		{
			int curNode = PathNodes[i - 1];
			World.GetLocXAndLocY(curNode, out int curNodeLocX, out int curNodeLocY);
			int nextNode = PathNodes[i];
			World.GetLocXAndLocY(nextNode, out int nextNodeLocX, out int nextNodeLocY);
			if ((curNodeLocX - checkXLoc) * (checkYLoc - nextNodeLocY) == (curNodeLocY - checkYLoc) * (checkXLoc - nextNodeLocX)) // point of division
				return true;
		}

		return false;
	}

	private void HandleBlockedWait(Unit unit)
	{
		int stepMagn = MoveStepCoeff();
		int cycleWait = 0;
		Location loc;

		if (IsDirCorrect())
		{
			Unit blockedUnit = unit;
			SpriteInfo unitSpriteInfo = unit.SpriteInfo;
			int nextX, nextY, loop = 0, i;

			//---------------------------------------------------------------//
			// construct a cycle_waiting array to store the sprite_recno of
			// those units in cycle_waiting in order to prevent forever looping
			// in the checking
			//---------------------------------------------------------------//
			int arraySize = 20;
			cycle_wait_unit_array_def_size = arraySize;
			cycle_wait_unit_index = 0;
			cycle_wait_unit_array_multipler = 1;
			cycle_wait_unit_array = new int[cycle_wait_unit_array_def_size];

			//---------------------------------------------------------------//
			// don't handle the case blocked by size 2x2 unit in this moment
			//---------------------------------------------------------------//
			while (cycleWait == 0 && blockedUnit.CurAction == SPRITE_WAIT)
			{
				if (unitSpriteInfo.LocWidth > 1)
					break; // don't handle unit size > 1

				if (!blockedUnit.IsDirCorrect())
					break;

				//----------------------------------------------------------------------------------------//
				// cur_x, cur_y of unit pointed by blockedUnit should be exactly inside a tile
				//----------------------------------------------------------------------------------------//
				nextX = blockedUnit.CurX + stepMagn * MoveXPixels[blockedUnit.FinalDir];
				nextY = blockedUnit.CurY + stepMagn * MoveYPixels[blockedUnit.FinalDir];

				//---------- calculate location blocked unit attempts to move to ---------//
				nextX >>= InternalConstants.CellWidthShift;
				nextY >>= InternalConstants.CellHeightShift;

				loc = World.GetLoc(nextX, nextY);
				bool blocked = loc.HasUnit(MobileType);

				//---------------- the unit is also waiting ---------------//
				if (blocked && (blockedUnit.move_to_x_loc != blockedUnit.CurLocX ||
				                blockedUnit.move_to_y_loc != blockedUnit.CurLocY))
				{
					if (loc.UnitId(MobileType) == SpriteId)
						cycleWait = 1;
					else
					{
						for (i = 0; i < cycle_wait_unit_index; i++)
						{
							//---------- checking for forever loop ----------------//
							if (cycle_wait_unit_array[i] == blockedUnit.SpriteId)
							{
								loop = 1;
								break;
							}
						}

						if (loop != 0)
							break;

						//------------------------------------------------------//
						// resize array if required size is larger than arraySize
						//------------------------------------------------------//
						if (cycle_wait_unit_index >= arraySize)
						{
							cycle_wait_unit_array_multipler++;
							arraySize = cycle_wait_unit_array_def_size * cycle_wait_unit_array_multipler;
							int[] cycle_wait_unit_array_new = new int[arraySize];
							for (int j = 0; j < cycle_wait_unit_array.Length; j++)
							{
								cycle_wait_unit_array_new[j] = cycle_wait_unit_array[j];
							}

							cycle_wait_unit_array = cycle_wait_unit_array_new;
						}
						else
						{
							//-------- store recno of next blocked unit ----------//
							cycle_wait_unit_array[cycle_wait_unit_index++] = blockedUnit.SpriteId;
							loc = World.GetLoc(nextX, nextY);
							blockedUnit = UnitArray[loc.UnitId(MobileType)];
							unitSpriteInfo = blockedUnit.SpriteInfo;
						}
					}
				}
				else
					break;
			}

			//---------- deinit data structure -------//
			cycle_wait_unit_array = null;
		}

		if (cycleWait != 0)
		{
			//----------------------------------------------------------------------//
			// shift the recno of all the unit in the cycle
			//----------------------------------------------------------------------//
			int backupSpriteRecno;
			World.SetUnitId(CurLocX, CurLocY, MobileType, 0); // empty the firt node in the cycle
			CycleWaitShiftRecno(this, unit); // shift all the unit in the cycle
			backupSpriteRecno = World.GetUnitId(CurLocX, CurLocY, MobileType);
			World.SetUnitId(CurLocX, CurLocY, MobileType, SpriteId);
			SetNext(unit.CurX, unit.CurY, -stepMagn, 1);
			set_move();
			World.SetUnitId(unit.CurLocX, unit.CurLocY, MobileType, SpriteId);
			World.SetUnitId(CurLocX, CurLocY, MobileType, backupSpriteRecno);
			swapping = 1;
		}
		else // not in a cycle
		{
			set_wait();

			//if(waiting_term>=MAX_WAITING_TERM_SAME)
			if (waiting_term >= UnitConstants.MAX_WAITING_TERM_SAME * MoveStepCoeff())
			{
				//-----------------------------------------------------------------//
				// codes used to speed up frame rate
				//-----------------------------------------------------------------//
				loc = World.GetLoc(move_to_x_loc, move_to_y_loc);
				if (!loc.CanMove(MobileType) && action_mode2 != UnitConstants.ACTION_MOVE)
					stop(UnitConstants.KEEP_PRESERVE_ACTION); // let reactivate..() call searching later
				else
					SearchOrWait();

				waiting_term = 0;
			}
		}
	}

	private void CycleWaitShiftRecno(Unit curUnit, Unit nextUnit)
	{
		int stepMagn = MoveStepCoeff();
		Unit blockedUnit;
		Location loc;

		//----------- find the next location ------------//
		int nextX = nextUnit.CurX + stepMagn * MoveXPixels[nextUnit.FinalDir];
		int nextY = nextUnit.CurY + stepMagn * MoveYPixels[nextUnit.FinalDir];

		nextX >>= InternalConstants.CellWidthShift;
		nextY >>= InternalConstants.CellHeightShift;

		if (nextX != CurLocX || nextY != CurLocY)
		{
			loc = World.GetLoc(nextX, nextY);
			blockedUnit = UnitArray[loc.UnitId(nextUnit.MobileType)];
		}
		else
		{
			blockedUnit = this;
		}

		if (blockedUnit != this)
		{
			CycleWaitShiftRecno(nextUnit, blockedUnit);
			nextUnit.SetNext(blockedUnit.CurX, blockedUnit.CurY, -stepMagn, 1);
			nextUnit.set_move();
			World.SetUnitId(blockedUnit.CurLocX, blockedUnit.CurLocY,
				nextUnit.MobileType, nextUnit.SpriteId);
			World.SetUnitId(nextUnit.CurLocX, nextUnit.CurLocY, nextUnit.MobileType, 0);
			nextUnit.swapping = 1;
		}
		else // the cycle shift is ended
		{
			nextUnit.SetNext(CurX, CurY, -stepMagn, 1);
			nextUnit.set_move();
			World.SetUnitId(CurLocX, CurLocY, nextUnit.MobileType, nextUnit.SpriteId);
			World.SetUnitId(nextUnit.CurLocX, nextUnit.CurLocY, nextUnit.MobileType, 0);

			nextUnit.swapping = 1;
		}
	}

	private void HandleBlockedAttackUnit(Unit unit, Unit target)
	{
		if (action_para == target.SpriteId && unit.action_para == target.SpriteId &&
		    action_mode == unit.action_mode)
		{
			//----------------- both attack the same target --------------------//
			HandleBlockedSameTargetAttack(unit, target);
		}
		else
		{
			SearchOrStop(move_to_x_loc, move_to_y_loc, 1, SeekPath.SEARCH_MODE_A_UNIT_IN_GROUP); // recall A* algorithm
		}
		//search(move_to_x_loc, move_to_y_loc, 1, SEARCH_MODE_A_UNIT_IN_GROUP); // recall A* algorithm
	}

	private void HandleBlockedAttackFirm(Unit unit)
	{
		if (action_x_loc == unit.action_x_loc && action_y_loc == unit.action_y_loc &&
		    action_para == unit.action_para && action_mode == unit.action_mode)
		{
			//------------- both attacks the same firm ------------//
			Location loc = World.GetLoc(action_x_loc, action_y_loc);
			if (!loc.IsFirm())
				stop2(UnitConstants.KEEP_DEFENSE_MODE); // stop since firm is deleted
			else
			{
				Firm firm = FirmArray[action_para];
				FirmInfo firmInfo = FirmRes[firm.firm_id];

				if (space_for_attack(action_x_loc, action_y_loc, UnitConstants.UNIT_LAND,
					    firmInfo.loc_width, firmInfo.loc_height))
				{
					//------------ found surrounding place to attack the firm -------------//
					if (MobileType == UnitConstants.UNIT_LAND)
						SetMoveToSurround(firm.loc_x1, firm.loc_y1,
							firmInfo.loc_width, firmInfo.loc_height, UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO);
					else
						attack_firm(firm.loc_x1, firm.loc_y1);
				}
				else // no surrounding place found, stop now
					stop(UnitConstants.KEEP_PRESERVE_ACTION);
			}
		}
		else // let process_idle() handle it
			stop();
	}

	private void HandleBlockedAttackTown(Unit unit)
	{
		if (action_x_loc == unit.action_x_loc && action_y_loc == unit.action_y_loc &&
		    action_para == unit.action_para && action_mode == unit.action_mode)
		{
			//---------------- both attacks the same town ----------------------//
			Location loc = World.GetLoc(action_x_loc, action_y_loc);
			if (!loc.IsTown())
				stop2(UnitConstants.KEEP_DEFENSE_MODE); // stop since town is deleted
			else if (space_for_attack(action_x_loc, action_y_loc, UnitConstants.UNIT_LAND,
				         InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT))
			{
				//------------ found surrounding place to attack the town -------------//
				Town town = TownArray[action_para];
				{
					if (MobileType == UnitConstants.UNIT_LAND)
						SetMoveToSurround(town.LocX1, town.LocY1,
							InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, UnitConstants.BUILDING_TYPE_TOWN_MOVE_TO);
					else
						attack_town(town.LocX1, town.LocY1);
				}
			}
			else // no surrounding place found, stop now
				stop(UnitConstants.KEEP_PRESERVE_ACTION);
		}
		else
			stop();
	}

	private void HandleBlockedAttackWall(Unit unit)
	{
		if (action_x_loc == unit.action_x_loc && action_y_loc == unit.action_y_loc && action_mode == unit.action_mode)
		{
			//------------- both attacks the same wall ------------//
			Location loc = World.GetLoc(action_x_loc, action_y_loc);
			if (!loc.IsWall())
				stop2(UnitConstants.KEEP_DEFENSE_MODE); // stop since wall is deleted
			else if (space_for_attack(action_x_loc, action_y_loc, UnitConstants.UNIT_LAND, 1, 1))
			{
				//------------ found surrounding place to attack the wall -------------//
				// search for a unit only, not for a group
				if (MobileType == UnitConstants.UNIT_LAND)
					SetMoveToSurround(action_x_loc, action_y_loc, 1, 1, UnitConstants.BUILDING_TYPE_WALL);
				else
					attack_wall(action_x_loc, action_y_loc);
			}
			else // no surrounding place found, stop now
				stop(UnitConstants.KEEP_PRESERVE_ACTION); // no space available, so stop to wait for space to attack the wall
		}
		else
		{
			if (action_x_loc == -1 || action_y_loc == -1)
				stop();
			else
				set_wait();
		}
	}

	private void HandleBlockedSameTargetAttack(Unit unit, Unit target)
	{
		//----------------------------------------------------------//
		// this unit is now waiting and the unit pointed by unit
		// is attacking the unit pointed by target
		//----------------------------------------------------------//
		if (space_for_attack(action_x_loc, action_y_loc, target.MobileType,
			    target.SpriteInfo.LocWidth, target.SpriteInfo.LocHeight))
		{
			SearchOrStop(move_to_x_loc, move_to_y_loc, 1, SeekPath.SEARCH_MODE_TO_ATTACK, target.SpriteId);
			//search(move_to_x_loc, move_to_y_loc, 1, SEARCH_MODE_TO_ATTACK, target.sprite_recno);
		}
		else if (in_any_defense_mode())
		{
			general_defend_mode_detect_target();
		}
		else if (Misc.points_distance(NextLocX, NextLocY, action_x_loc, action_y_loc) < UnitConstants.ATTACK_DETECT_DISTANCE)
		{
			//------------------------------------------------------------------------//
			// if the target is within the detect range, stop the unit's action to detect
			// another target if any exist. In case, there is no other target, the unit
			// will still attack the original target since it is the only target in the
			// detect range
			//------------------------------------------------------------------------//
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
		}
		else
			set_wait(); // set wait to stop the movement
	}

	private bool ShipToBeachPathEdit(ref int resultXLoc, ref int resultYLoc, int regionId)
	{
		int curXLoc = NextLocX;
		int curYLoc = NextLocY;
		if (Math.Abs(curXLoc - resultXLoc) <= 1 && Math.Abs(curYLoc - resultYLoc) <= 1)
			return true;

		//--------------- find a path to land area -------------------//
		UnitMarine ship = (UnitMarine)this;
		int result = Search(resultXLoc, resultYLoc, 1, SeekPath.SEARCH_MODE_TO_LAND_FOR_SHIP, regionId);
		if (result == 0)
			return true;

		//----------- update cur location --------//
		curXLoc = NextLocX;
		curYLoc = NextLocY;

		//------------------------------------------------------------------------------//
		// edit the result path to get a location for embarking
		//------------------------------------------------------------------------------//

		if (PathNodes.Count > 0)
		{
			int curNodeIndex = 0;
			int nextNodeIndex = 1;
			int curNode = PathNodes[curNodeIndex];
			World.GetLocXAndLocY(curNode, out int curNodeLocX, out int curNodeLocY);
			int nextNode = PathNodes[nextNodeIndex];
			World.GetLocXAndLocY(nextNode, out int nextNodeLocX, out int nextNodeLocY);
			int moveScale = MoveStepCoeff();
			int nodeCount = PathNodes.Count;
			Location loc;
			int i, j, found, pathDist;

			int preXLoc = -1, preYLoc = -1;
			int checkXLoc = curXLoc;
			int checkYLoc = curYLoc;
			int hasMoveStep = 0;
			if (checkXLoc != curNodeLocX || checkYLoc != curNodeLocY)
			{
				hasMoveStep += moveScale;
				checkXLoc = curNodeLocX;
				checkYLoc = curNodeLocY;
			}

			//-----------------------------------------------------------------//
			// find the pair of points that one is in ocean and one in land
			//-----------------------------------------------------------------//
			int xMagn, yMagn, magn;

			for (pathDist = 0, found = 0, i = 1; i < nodeCount; ++i, curNodeIndex++, nextNodeIndex++)
			{
				curNode = PathNodes[curNodeIndex];
				World.GetLocXAndLocY(curNode, out curNodeLocX, out curNodeLocY);
				nextNode = PathNodes[nextNodeIndex];
				World.GetLocXAndLocY(nextNode, out nextNodeLocX, out nextNodeLocY);
				int vecX = nextNodeLocX - curNodeLocX;
				int vecY = nextNodeLocY - curNodeLocY;
				magn = ((xMagn = Math.Abs(vecX)) > (yMagn = Math.Abs(vecY))) ? xMagn : yMagn;
				if (xMagn != 0)
				{
					vecX /= xMagn;
					vecX *= moveScale;
				}

				if (yMagn != 0)
				{
					vecY /= yMagn;
					vecY *= moveScale;
				}

				//------------- check each location between editNode1 and editNode2 -------------//
				for (j = 0; j < magn; j += moveScale)
				{
					preXLoc = checkXLoc;
					preYLoc = checkYLoc;
					checkXLoc += vecX;
					checkYLoc += vecY;

					loc = World.GetLoc(checkXLoc, checkYLoc);
					if (TerrainRes[loc.TerrainId].average_type != TerrainTypeCode.TERRAIN_OCEAN) // found
					{
						found++;
						break;
					}
				}

				if (found != 0)
				{
					//------------ a soln is found ---------------//
					if (j == 0) // end node should be curNode pointed at
					{
						pathDist -= hasMoveStep;
						while (PathNodes.Count > i)
						{
							PathNodes.RemoveAt(PathNodes.Count - 1);
						}
						_pathNodeDistance = pathDist;
					}
					else
					{
						PathNodes[nextNodeIndex] = World.GetMatrixIndex(checkXLoc, checkYLoc);

						if (i == 1) // first editing
						{
							World.GetLocXAndLocY(PathNodes[0], out int firstNodeLocX, out int firstNodeLocY);
							if (CurX == firstNodeLocX * InternalConstants.CellWidth && CurY == firstNodeLocY * InternalConstants.CellHeight)
							{
								GoX = checkXLoc * InternalConstants.CellWidth;
								GoY = checkYLoc * InternalConstants.CellHeight;
							}
						}

						pathDist += (j + moveScale);
						pathDist -= hasMoveStep;
						while (PathNodes.Count > i + 1)
						{
							PathNodes.RemoveAt(PathNodes.Count - 1);
						}
						_pathNodeDistance = pathDist;
					}

					move_to_x_loc = preXLoc;
					move_to_y_loc = preYLoc;
					loc = World.GetLoc((preXLoc + checkXLoc) / 2, (preYLoc + checkYLoc) / 2);
					if (TerrainRes[loc.TerrainId].average_type != TerrainTypeCode.TERRAIN_OCEAN)
					{
						resultXLoc = (preXLoc + checkXLoc) / 2;
						resultYLoc = (preYLoc + checkYLoc) / 2;
					}
					else
					{
						resultXLoc = checkXLoc;
						resultYLoc = checkYLoc;
					}

					break;
				}
				else
					pathDist += magn;
			}

			if (found == 0)
			{
				World.GetLocXAndLocY(PathNodes[^1], out int endNodeLocX, out int endNodeLocY);
				if (Math.Abs(endNodeLocX - resultXLoc) > 1 || Math.Abs(endNodeLocY - resultYLoc) > 1)
				{
					MoveTo(resultXLoc, resultYLoc, -1);
					return false;
				}
			}
		}
		else
		{
			//------------- scan for the surrounding for a land location -----------//
			for (int i = 2; i <= 9; ++i)
			{
				int xShift, yShift;
				Misc.cal_move_around_a_point(i, 3, 3, out xShift, out yShift);
				int checkXLoc = curXLoc + xShift;
				int checkYLoc = curYLoc + yShift;
				if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
					continue;

				Location loc = World.GetLoc(checkXLoc, checkYLoc);
				if (loc.RegionId != regionId)
					continue;

				if (TerrainRes[loc.TerrainId].average_type != TerrainTypeCode.TERRAIN_OCEAN &&
				    loc.CanMove(UnitConstants.UNIT_LAND))
				{
					resultXLoc = checkXLoc;
					resultYLoc = checkYLoc;
					return true;
				}
			}

			return false;
		}

		return true;
	}

	private void ShipLeaveBeach(int shipOldXLoc, int shipOldYLoc)
	{
		//--------------------------------------------------------------------------------//
		// scan for location to leave the beach
		//--------------------------------------------------------------------------------//
		int curXLoc = NextLocX;
		int curYLoc = NextLocY;
		int xShift, yShift, checkXLoc = -1, checkYLoc = -1, found = 0;

		//------------- find a location to leave the beach ------------//
		for (int i = 2; i <= 9; i++)
		{
			Misc.cal_move_around_a_point(i, 3, 3, out xShift, out yShift);
			checkXLoc = curXLoc + xShift;
			checkYLoc = curYLoc + yShift;

			if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
				continue;

			if (checkXLoc % 2 != 0 || checkYLoc % 2 != 0)
				continue;

			Location loc = World.GetLoc(checkXLoc, checkYLoc);
			if (TerrainRes[loc.TerrainId].average_type == TerrainTypeCode.TERRAIN_OCEAN && loc.CanMove(MobileType))
			{
				found++;
				break;
			}
		}

		if (found == 0)
			return; // no suitable location, wait until finding suitable location

		//---------------- leave now --------------------//
		SetDir(shipOldXLoc, shipOldYLoc, checkXLoc, checkYLoc);
		set_ship_extra_move();
		GoX = checkXLoc * InternalConstants.CellWidth;
		GoY = checkYLoc * InternalConstants.CellHeight;
	}
	
	public void SelectSearchSubMode(int sx, int sy, int dx, int dy, int nationRecno, int searchMode)
	{
		if (!ConfigAdv.unit_allow_path_power_mode)
		{
			// cancel the selection
			SeekPath.SetSubMode();
			return;
		}

		if (nation_recno == 0 || ignore_power_nation != 0)
		{
			SeekPath.SetSubMode(); // always using normal mode for independent unit
			return;
		}

		//--------------------------------------------------------------------------------//
		// Checking for starting location and destination to determine sub_mode used
		// N - not hostile, H - hostile
		// 1) N . N, using normal mode
		// 2) N . H, H . N, H . H, using sub_mode SEARCH_SUB_MODE_PASSABLE
		//--------------------------------------------------------------------------------//
		Location startLoc = World.GetLoc(sx, sy);
		Location destLoc = World.GetLoc(dx, dy);
		Nation nation = NationArray[nationRecno];

		bool subModeOn = (startLoc.PowerNationId == 0 || nation.get_relation_passable(startLoc.PowerNationId)) &&
		                 (destLoc.PowerNationId == 0 || nation.get_relation_passable(destLoc.PowerNationId));

		if (subModeOn) // true only when both start and end locations are passable for this nation
		{
			SeekPath.SetNationPassable(nation.relation_passable_array);
			SeekPath.SetSubMode(SeekPath.SEARCH_SUB_MODE_PASSABLE);
		}
		else
		{
			SeekPath.SetSubMode(); // normal sub mode, normal searching
		}
	}

	private int Search(int destLocX, int destLocY, int preserveAction = 0, int searchMode = SeekPath.SEARCH_MODE_IN_A_GROUP, int miscNo = 0, int numOfPaths = 1)
	{
		if (destLocX < 0 || destLocX >= GameConstants.MapSize || destLocY < 0 || destLocY >= GameConstants.MapSize ||
		    hit_points <= 0.0 || action_mode == UnitConstants.ACTION_DIE || CurAction == SPRITE_DIE)
		{
			//TODO check, this code should be never executed
			stop2(UnitConstants.KEEP_DEFENSE_MODE); //-********** BUGHERE, err_handling for retailed version
			return 1;
		}

		int result = 0;
		if (UnitRes[unit_id].unit_class == UnitConstants.UNIT_CLASS_SHIP)
		{
			UnitMarine ship = (UnitMarine)this;
			switch (ship.extra_move_in_beach)
			{
				case UnitMarine.NO_EXTRA_MOVE:
					result = Searching(destLocX, destLocY, preserveAction, searchMode, miscNo, numOfPaths);
					break;

				case UnitMarine.EXTRA_MOVING_IN:
				case UnitMarine.EXTRA_MOVING_OUT:
					return 0;

				case UnitMarine.EXTRA_MOVE_FINISH:
					ShipLeaveBeach(NextLocX, NextLocY);
					break;
			}
		}
		else
		{
			result = Searching(destLocX, destLocY, preserveAction, searchMode, miscNo, numOfPaths);
		}

		if (wayPoints.Count > 0 && PathNodes.Count == 0) // can move no more
			ResetWayPoints();

		// 0 means extra_move_in_beach != UnitMarine.NO_EXTRA_MOVE
		return result != 0 ? 1 : 0;
	}

	private int Searching(int destLocX, int destLocY, int preserveAction, int searchMode, int miscNo, int numOfPaths)
	{
		stop(preserveAction); // stop the unit as soon as possible

		int startLocX = NextLocX; // next location the sprite is moving towards
		int startLocY = NextLocY;

		move_to_x_loc = destLocX;
		move_to_y_loc = destLocY;

		//------------------------------------------------------------//
		// fast checking for destination == current location
		//------------------------------------------------------------//
		if (startLocX == destLocX && startLocY == destLocY) // already here
		{
			if (CurX != NextX || CurY != NextY)
				set_move();
			else
				set_idle();

			return 1;
		}

		//------------------------ find the shortest path --------------------------//
		//
		// decide the searching to use according to the unit size
		// assume the unit size is always 1x1, 2x2, 3x3 and so on
		// i.e. sprite_info.loc_width == sprite_info.loc_height
		//--------------------------------------------------------------------------//

		ResetPath();

		SeekPath.SetNationId(nation_recno);

		if (MobileType == UnitConstants.UNIT_LAND)
			SelectSearchSubMode(startLocX, startLocY, destLocX, destLocY, nation_recno, searchMode);
		int seekResult = SeekPath.Seek(startLocX, startLocY, destLocX, destLocY, unit_group_id,
			MobileType, searchMode, miscNo, numOfPaths);

		PathNodes.AddRange(SeekPath.GetResult(out _pathNodeDistance));
		SeekPath.SetSubMode(); // reset sub_mode searching

		//-----------------------------------------------------------------------//
		// update ignore_power_nation
		//-----------------------------------------------------------------------//

		if (ai_unit)
		{
			//------- set ignore_power_nation -------//

			if (seekResult == SeekPath.PATH_IMPOSSIBLE)
			{
				switch (ignore_power_nation)
				{
					case 0:
						ignore_power_nation = 1;
						break;
					case 1:
						ignore_power_nation = 2;
						break;
					case 2:
						break;
				}
			}
			else
			{
				if (ignore_power_nation == 1)
					ignore_power_nation = 0;
			}
		}

		//-----------------------------------------------------------------------//
		// if closest node is returned, the destination should not be the real
		// location to go to.  Thus, move_to_?_loc should be adjusted
		//-----------------------------------------------------------------------//
		if (PathNodes.Count > 0)
		{
			int lastNode = PathNodes[^1];
			World.GetLocXAndLocY(lastNode, out move_to_x_loc, out move_to_y_loc);

			PathNodeIndex = 0;
			// check if the unit is moving right now, wait until it reaches the nearest complete tile.
			if (CurAction != SPRITE_MOVE)
			{
				int nextNode = PathNodes[1];
				World.GetLocXAndLocY(nextNode, out int nextNodeLocX, out int nextNodeLocY);
				SetDir(startLocX, startLocY, nextNodeLocX, nextNodeLocY);
				NextMove();
			}
		}
		else // stay in the current location
		{
			move_to_x_loc = startLocX; // adjust move_to_?_loc
			move_to_y_loc = startLocY;

			if (CurX != NextX || CurY != NextY)
				set_move();
			else
				set_idle();
		}

		//-------------------------------------------------------//
		// PATH_NODE_USED_UP happens when:
		// Exceed the object's MAX's node limitation, the closest path
		// is returned. Get to the closest path first and continue
		// to seek the path in the background.
		//-------------------------------------------------------//

		return 1;
	}

	private void SearchOrStop(int destX, int destY, int preserveAction = 0, int searchMode = 1, int miscNo = 0)
	{
		Location loc = World.GetLoc(destX, destY);
		if (!loc.CanMove(MobileType))
		{
			stop(UnitConstants.KEEP_PRESERVE_ACTION); // let reactivate..() call searching later
			//waiting_term = MAX_SEARCH_OT_STOP_WAIT_TERM;
		}
		else
		{
			Search(destX, destY, preserveAction, searchMode, miscNo);
			/*if(mobile_type==UnitRes.UNIT_LAND)
				search(destX, destY, preserveAction, searchMode, miscNo);
			else
				waiting_term = 0;*/
		}
	}

	private void SearchOrWait()
	{
		const int SQUARE1 = 9;
		const int SQUARE2 = 25;
		const int SQUARE3 = 49;
		const int DIMENSION = 7;

		int curXLoc = NextLocX, curYLoc = NextLocY;
		int[] surrArray = new int[SQUARE3];
		int xShift, yShift, checkXLoc, checkYLoc, hasFree, i, shouldWait;
		int unitRecno;
		Unit unit;
		Location loc;

		//----------------------------------------------------------------------------//
		// wait if the unit is totally blocked.  Otherwise, call searching
		//----------------------------------------------------------------------------//
		for (shouldWait = 0, hasFree = 0, i = 2; i <= SQUARE3; i++)
		{
			if (i == SQUARE1 || i == SQUARE2 || i == SQUARE3)
			{
				if (hasFree == 0)
				{
					shouldWait++;
					break;
				}
				else
					hasFree = 0;
			}

			Misc.cal_move_around_a_point(i, DIMENSION, DIMENSION, out xShift, out yShift);
			checkXLoc = curXLoc + xShift;
			checkYLoc = curYLoc + yShift;
			if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
				continue;

			loc = World.GetLoc(checkXLoc, checkYLoc);
			if (!loc.HasUnit(MobileType))
			{
				hasFree++;
				continue;
			}

			unitRecno = loc.UnitId(MobileType);
			if (UnitArray.IsDeleted(unitRecno))
				continue;

			unit = UnitArray[unitRecno];
			if (unit.nation_recno == nation_recno && unit.unit_group_id == unit_group_id &&
			    ((unit.CurAction == SPRITE_WAIT && unit.waiting_term > 1) || unit.CurAction == SPRITE_TURN ||
			     unit.CurAction == SPRITE_MOVE))
			{
				surrArray[i - 2] = unitRecno;
				unit.unit_group_id++;
			}
		}

		//------------------- call searching if should not wait --------------------//
		if (shouldWait == 0)
			Search(move_to_x_loc, move_to_y_loc, 1, SeekPath.SEARCH_MODE_IN_A_GROUP);
		//search_or_stop(move_to_x_loc, move_to_y_loc, 1, SEARCH_MODE_IN_A_GROUP);

		for (i = 0; i < SQUARE3; i++)
		{
			if (surrArray[i] != 0)
			{
				unit = UnitArray[surrArray[i]];
				unit.unit_group_id--;
			}
		}

		if (shouldWait == 1)
			set_wait();
	}
	
	protected void ResetPath()
	{
		PathNodes.Clear();
		PathNodeIndex = -1;
		_pathNodeDistance = 0;
	}

	public void AddWayPoint(int locX, int locY)
	{
		if (wayPoints.Count > 1) // don't allow to remove the 1st node, since the unit is moving there
		{
			for (int i = wayPoints.Count - 1; i >= 0; i--)
			{
				World.GetLocXAndLocY(wayPoints[i], out int wayPointLocX, out int wayPointLocY);
				if (wayPointLocX == locX && wayPointLocY == locY) // remove this node
				{
					wayPoints.RemoveAt(i);
					return; // there should be one and only one node with the same value
				}
			}
		}

		wayPoints.Add(World.GetMatrixIndex(locX, locY));

		if (wayPoints.Count == 1)
			MoveTo(locX, locY);
	}

	public void ResetWayPoints()
	{
		//------------------------------------------------------------------------------------//
		// There are only two conditions to reset the wayPoints
		// 1) action_mode2!=ACTION_MOVE in Unit::stop()
		// 2) dest? != node_? in the first node of wayPoints in calling Unit::move_to()
		//------------------------------------------------------------------------------------//
		wayPoints.Clear();
	}

	public void ProcessWayPoint()
	{
		int destX, destY;
		if (wayPoints.Count > 1)
		{
			World.GetLocXAndLocY(wayPoints[1], out int wayPointLocX, out int wayPointLocY);
			destX = wayPointLocX;
			destY = wayPointLocY;
			wayPoints.RemoveAt(1);
		}
		else // only one unprocessed node
		{
			World.GetLocXAndLocY(wayPoints[0], out int wayPointLocX, out int wayPointLocY);
			destX = wayPointLocX;
			destY = wayPointLocY;
		}

		MoveTo(destX, destY);
	}
}