using System;

namespace TenKingdoms;

public partial class Unit
{
	public void MoveTo(int destLocX, int destLocY, int preserveAction = 0, int searchMode = SeekPath.SEARCH_MODE_IN_A_GROUP, int miscNo = 0, int numOfPath = 1)
	{
		//---------- reset way point array since new action is assigned --------//
		if (WayPoints.Count > 0)
		{
			World.GetLocXAndLocY(WayPoints[0], out var locX, out var locY);
			if (locX != destLocX || locY != destLocY)
				ResetWayPoints();
		}

		if (IsUnitDead())
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
		// mainly checked by ActionMode2. If previous action is ACTION_MOVE,
		// ActionMode2, ActionPara2, ActionLocX2 and ActionLocY2 need to be kept for this checking.
		//
		// If calling from UnitArray.MoveTo(), ActionMode is set to ACTION_MOVE, ActionPara is set to 0
		// while ActionLocX and ActionLocY are kept as original value for checking.
		// Meanwhile, ActionMode2, ActionPara2, ActionLocX2 and ActionLocY2 are kept
		// if the condition is fulfilled (ActionMode2 == ACTION_MOVE)
		//-----------------------------------------------------------------------------------//
		if (ActionMode2 == UnitConstants.ACTION_MOVE && ActionMode == UnitConstants.ACTION_MOVE)
		{
			//------ previous action is ACTION_MOVE -------//
			if (ActionLocX2 == destLocX && ActionLocY2 == destLocY)
			{
				//-------- equal order --------//
				ActionLocX = ActionLocX2;
				ActionLocY = ActionLocY2;

				if (CurAction != SPRITE_IDLE)
				{
					//-------- the old order is processing --------//
					if (PathNodes.Count == 0) // cannot move
					{
						if (UnitRes[UnitType].unit_class == UnitConstants.UNIT_CLASS_SHIP)
						{
							if (CurAction != SPRITE_SHIP_EXTRA_MOVE)
							{
								if (CurX != NextX || CurY != NextY)
									SetMove();
								else
									SetIdle();
							}
							//else keep extra_moving
						}
						else
						{
							if (CurX != NextX || CurY != NextY)
								SetMove();
							else
								SetIdle();
						}
					}

					return;
				} //else action is hold due to some problems, reactivate again
			}
		} //else, new order or searching is required

		MoveActionCallFlag = true; // set flag to avoid calling move_to_my_loc()

		ActionMode2 = UnitConstants.ACTION_MOVE;
		ActionPara2 = 0;

		Search(destLocX, destLocY, preserveAction, searchMode, miscNo, numOfPath);
		MoveActionCallFlag = false;

		//----------------------------------------------------------------//
		// store new order in action parameters
		//----------------------------------------------------------------//
		ActionMode = UnitConstants.ACTION_MOVE;
		ActionParam = 0;
		ActionLocX = ActionLocX2 = MoveToLocX;
		ActionLocY = ActionLocY2 = MoveToLocY;
	}
	
	public void MoveToUnitSurround(int destLocX, int destLocY, int width, int height, int miscNo = 0)
	{
		//----------------------------------------------------------------//
		// calculate new destination if trying to move to different territory
		//----------------------------------------------------------------//
		if (World.GetLoc(NextLocX, NextLocY).RegionId != World.GetLoc(destLocX, destLocY).RegionId)
		{
			MoveTo(destLocX, destLocY);
			return;
		}

		if (IsUnitDead())
			return;

		//----------------------------------------------------------------//
		// check for equal actions
		//----------------------------------------------------------------//
		if (ActionMode2 == UnitConstants.ACTION_MOVE && ActionMode == UnitConstants.ACTION_MOVE)
		{
			//------ previous action is ACTION_MOVE -------//
			if (ActionLocX2 == destLocX && ActionLocY2 == destLocY)
			{
				//-------- equal order --------//
				ActionLocX = ActionLocX2;
				ActionLocY = ActionLocY2;

				if (CurAction != SPRITE_IDLE)
				{
					//-------- the old order is processing --------//
					if (PathNodes.Count == 0) // cannot move
					{
						SetIdle();
					}

					return;
				} //else action is hold due to some problems, reactivate again
			}
		} //else, new order or searching is required

		int destX = Math.Max(0, (width > 1) ? destLocX : destLocX - width + 1);
		int destY = Math.Max(0, (height > 1) ? destLocY : destLocY - height + 1);

		Unit unit = UnitArray[miscNo];
		SpriteInfo spriteInfo = unit.SpriteInfo;
		Stop();
		SetMoveToSurround(destX, destY, spriteInfo.LocWidth, spriteInfo.LocHeight, UnitConstants.BUILDING_TYPE_VEHICLE);

		//----------------------------------------------------------------//
		// store new order in action parameters
		//----------------------------------------------------------------//
		ActionMode = ActionMode2 = UnitConstants.ACTION_MOVE;
		ActionParam = ActionPara2 = 0;
		ActionLocX = ActionLocX2 = MoveToLocX;
		ActionLocY = ActionLocY2 = MoveToLocY;
	}
	
	public void MoveToFirmSurround(int destLocX, int destLocY, int width, int height, int miscNo = 0)
	{
		//----------------------------------------------------------------//
		// calculate new destination if trying to move to different territory
		//----------------------------------------------------------------//
		Location loc = World.GetLoc(destLocX, destLocY);
		if (UnitRes[UnitType].unit_class == UnitConstants.UNIT_CLASS_SHIP && miscNo == Firm.FIRM_HARBOR)
		{
			Firm firm = FirmArray[loc.FirmId()];
			FirmHarbor harbor = (FirmHarbor)firm;
			if (World.GetLoc(NextLocX, NextLocY).RegionId != harbor.sea_region_id)
			{
				MoveTo(destLocX, destLocY);
				return;
			}
		}
		else
		{
			if (World.GetLoc(NextLocX, NextLocY).RegionId != loc.RegionId)
			{
				MoveTo(destLocX, destLocY);
				return;
			}
		}

		if (IsUnitDead())
			return;

		//----------------------------------------------------------------//
		// check for equal actions
		//----------------------------------------------------------------//
		if (ActionMode2 == UnitConstants.ACTION_MOVE && ActionMode == UnitConstants.ACTION_MOVE)
		{
			//------ previous action is ACTION_MOVE -------//
			if (ActionLocX2 == destLocX && ActionLocY2 == destLocY)
			{
				//-------- equal order --------//
				ActionLocX = ActionLocX2;
				ActionLocY = ActionLocY2;

				if (CurAction != SPRITE_IDLE)
				{
					//-------- the old order is processing --------//
					if (PathNodes.Count == 0) // cannot move
					{
						SetIdle();
					}

					return;
				} //else action is hold due to some problems, reactivate again
			}
		} //else, new order or searching is required

		int destX = Math.Max(0, (width > 1) ? destLocX : destLocX - width + 1);
		int destY = Math.Max(0, (height > 1) ? destLocY : destLocY - height + 1);

		FirmInfo firmInfo = FirmRes[miscNo];
		Stop();
		SetMoveToSurround(destX, destY, firmInfo.loc_width, firmInfo.loc_height, UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO, miscNo);

		//----------------------------------------------------------------//
		// store new order in action parameters
		//----------------------------------------------------------------//
		ActionMode = ActionMode2 = UnitConstants.ACTION_MOVE;
		ActionParam = ActionPara2 = 0;
		ActionLocX = ActionLocX2 = MoveToLocX;
		ActionLocY = ActionLocY2 = MoveToLocY;
	}

	public void MoveToTownSurround(int destLocX, int destLocY, int width, int height, int miscNo = 0)
	{
		//----------------------------------------------------------------//
		// calculate new destination if trying to move to different territory
		//----------------------------------------------------------------//
		if (World.GetLoc(NextLocX, NextLocY).RegionId != World.GetLoc(destLocX, destLocY).RegionId)
		{
			MoveTo(destLocX, destLocY);
			return;
		}

		if (IsUnitDead())
			return;

		//----------------------------------------------------------------//
		// check for equal actions
		//----------------------------------------------------------------//
		if (ActionMode2 == UnitConstants.ACTION_MOVE && ActionMode == UnitConstants.ACTION_MOVE)
		{
			//------ previous action is ACTION_MOVE -------//
			if (ActionLocX2 == destLocX && ActionLocY2 == destLocY)
			{
				//-------- equal order --------//
				ActionLocX = ActionLocX2;
				ActionLocY = ActionLocY2;

				if (CurAction != SPRITE_IDLE)
				{
					//-------- the old order is processing --------//
					if (PathNodes.Count == 0) // cannot move
					{
						SetIdle();
					}

					return;
				} //else action is hold due to some problems, reactivate again
			}
		} //else, new order or searching is required

		int destX = Math.Max(0, (width > 1) ? destLocX : destLocX - width + 1);
		int destY = Math.Max(0, (height > 1) ? destLocY : destLocY - height + 1);

		Stop();
		SetMoveToSurround(destX, destY, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, UnitConstants.BUILDING_TYPE_TOWN_MOVE_TO);

		//----------------------------------------------------------------//
		// store new order in action parameters
		//----------------------------------------------------------------//
		ActionMode = ActionMode2 = UnitConstants.ACTION_MOVE;
		ActionParam = ActionPara2 = 0;
		ActionLocX = ActionLocX2 = MoveToLocX;
		ActionLocY = ActionLocY2 = MoveToLocY;
	}

	public void MoveToWallSurround(int destLocX, int destLocY, int width, int height, int miscNo = 0)
	{
		//----------------------------------------------------------------//
		// calculate new destination if trying to move to different territory
		//----------------------------------------------------------------//
		if (World.GetLoc(NextLocX, NextLocY).RegionId != World.GetLoc(destLocX, destLocY).RegionId)
		{
			MoveTo(destLocX, destLocY);
			return;
		}

		if (IsUnitDead())
			return;

		//----------------------------------------------------------------//
		// check for equal actions
		//----------------------------------------------------------------//
		if (ActionMode2 == UnitConstants.ACTION_MOVE && ActionMode == UnitConstants.ACTION_MOVE)
		{
			//------ previous action is ACTION_MOVE -------//
			if (ActionLocX2 == destLocX && ActionLocY2 == destLocY)
			{
				//-------- equal order --------//
				ActionLocX = ActionLocX2;
				ActionLocY = ActionLocY2;

				if (CurAction != SPRITE_IDLE)
				{
					//-------- the old order is processing --------//
					if (PathNodes.Count == 0) // cannot move
					{
						SetIdle();
					}

					return;
				} //else action is hold due to some problems, reactivate again
			}
		} //else, new order or searching is required

		int destX = Math.Max(0, (width > 1) ? destLocX : destLocX - width + 1);
		int destY = Math.Max(0, (height > 1) ? destLocY : destLocY - height + 1);

		Stop();
		SetMoveToSurround(destX, destY, 1, 1, UnitConstants.BUILDING_TYPE_WALL);

		//----------------------------------------------------------------//
		// store new order in action parameters
		//----------------------------------------------------------------//
		ActionMode = ActionMode2 = UnitConstants.ACTION_MOVE;
		ActionParam = ActionPara2 = 0;
		ActionLocX = ActionLocX2 = MoveToLocX;
		ActionLocY = ActionLocY2 = MoveToLocY;
	}
	
	private bool SetMoveToSurround(int buildLocX, int buildLocY, int width, int height, int buildingType,
		int miscNo = 0, int readyDist = 0, int curProcessUnitNum = 1)
	{
		//--------------------------------------------------------------//
		// calculate the distance from the object
		//--------------------------------------------------------------//
		// 0 for inside, 1 for surrounding, >1 for the rest
		int distance = CalcDistance(buildLocX, buildLocY, width, height);

		if (distance == 0) // inside the building
		{
			ResetPath();
			if (CurX == NextX && CurY == NextY)
				SetIdle();

			return true;
		}

		if (distance == 1) // in the surrounding, no need to move
		{
			ResetPath();

			if (CurX == NextX && CurY == NextY)
			{
				MoveToLocX = NextLocX;
				MoveToLocY = NextLocY;
				GoX = CurX;
				GoY = CurY;
				SetIdle();
				SetDir(MoveToLocX, MoveToLocY, buildLocX + width / 2, buildLocY + height / 2);
			}

			return true;
		}

		//--------------------------------------------------------------//
		// the searching is divided into 2 parts.
		//
		// part 1 using the firm_type and firm_id to find a shortest path.
		// 
		// part 2
		// if the width and height is the actual width and height of the firm,
		// the unit moves to the surrounding of the firm.
		//
		// if the width and height > the actual width and height of the firm,
		// the unit moves to a location far away from the surrounding of the firm.
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
				// dimension for settling is used and the building built is a type of town.
				// Thus, passing -1 as the miscNo to show that "settle" is processed
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
			return false; // incomplete searching

		//====================================================================//
		// part 2
		//====================================================================//
		return PathNodes.Count > 0 && EditPathToSurround(buildLocX, buildLocY,
			buildLocX + width - 1, buildLocY + height - 1, readyDist);
	}
	
	private bool EditPathToSurround(int object1LocX, int object1LocY, int object2LocX, int object2LocY, int readyDist)
	{
		if (PathNodes.Count < 2)
			return false;

		//----------------------------------------------------------------------------//
		// At this moment, the unit generally has a path to the location inside the object,
		// walk through it and extract a path to the surrounding of the object.
		//----------------------------------------------------------------------------//

		//------- calculate the surrounding top-left and bottom-right points ------//
		int moveScale = MoveStepCoeff();
		int locX1 = object1LocX - readyDist - 1;
		int locY1 = object1LocY - readyDist - 1;
		int locX2 = object2LocX + readyDist + 1;
		int locY2 = object2LocY + readyDist + 1;

		Misc.BoundLocation(ref locX1, ref locY1);
		// TODO: strange conditions, check
		if (locX2 >= GameConstants.MapSize)
			locY1 = GameConstants.MapSize - moveScale;
		if (locY2 >= GameConstants.MapSize)
			locX2 = GameConstants.MapSize - moveScale;

		//--------------- adjust for air and sea units -----------------//
		if (MobileType != UnitConstants.UNIT_LAND)
		{
			//------ assume even x, y coordinate is used for UnitConstants.UNIT_SEA and UnitConstants.UNIT_AIR -------//
			if (locX1 % 2 != 0)
				locX1--;
			if (locY1 % 2 != 0)
				locY1--;
			if (locX2 % 2 != 0)
				locX2++;
			if (locY2 % 2 != 0)
				locY2++;

			// TODO: strange conditions, check
			if (locX2 > GameConstants.MapSize - moveScale)
				locX2 = GameConstants.MapSize - moveScale;
			if (locY2 > GameConstants.MapSize - moveScale)
				locY2 = GameConstants.MapSize - moveScale;
		}

		int checkLocX = NextLocX;
		int checkLocY = NextLocY;
		int editNode1Index = 0;
		int editNode2Index = 1;
		int editNode1 = PathNodes[editNode1Index];
		World.GetLocXAndLocY(editNode1, out int editNode1LocX, out int editNode1LocY);
		int editNode2 = PathNodes[editNode2Index];
		World.GetLocXAndLocY(editNode2, out int editNode2LocX, out int editNode2LocY);

		int hasMoveStep = 0;
		if (checkLocX != editNode1LocX || checkLocY != editNode1LocY)
		{
			hasMoveStep += moveScale;
			checkLocX = editNode1LocX;
			checkLocY = editNode1LocY;
		}

		// pathDist - counts the distance of the generated path, found - whether a path to the surrounding is found
		int pathDist = 0;
		bool found = false;

		//------- find the first node that is on the surrounding of the object -------//
		for (int i = 1; i < PathNodes.Count; i++, editNode1Index++, editNode2Index++)
		{
			editNode1 = PathNodes[editNode1Index];
			World.GetLocXAndLocY(editNode1, out editNode1LocX, out editNode1LocY);
			editNode2 = PathNodes[editNode2Index];
			World.GetLocXAndLocY(editNode2, out editNode2LocX, out editNode2LocY);

			//------------ calculate parameters for checking ------------//
			int vecX = editNode2LocX - editNode1LocX;
			int vecY = editNode2LocY - editNode1LocY;

			int xMagn = Math.Abs(vecX);
			int yMagn = Math.Abs(vecY);
			int magn = (xMagn >= yMagn) ? xMagn : yMagn;
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
			int j = 0;
			for (j = 0; j < magn; j += moveScale)
			{
				checkLocX += vecX;
				checkLocY += vecY;

				if (checkLocX >= locX1 && checkLocX <= locX2 && checkLocY >= locY1 && checkLocY <= locY2)
				{
					found = true;
					break;
				}
			}

			//-------------------------------------------------------------------------------//
			// a path is found, then set unit's parameters for its movement
			//-------------------------------------------------------------------------------//
			if (found)
			{
				PathNodes[editNode2Index] = World.GetMatrixIndex(checkLocX, checkLocY);

				if (i == 1) // first editing
				{
					World.GetLocXAndLocY(PathNodes[0], out int firstNodeLocX, out int firstNodeLocY);
					if (CurX == firstNodeLocX * InternalConstants.CellWidth && CurY == firstNodeLocY * InternalConstants.CellHeight)
					{
						GoX = checkLocX * InternalConstants.CellWidth;
						GoY = checkLocY * InternalConstants.CellHeight;
					}
				}

				pathDist += (j + moveScale);
				pathDist -= hasMoveStep;
				while (PathNodes.Count > i + 1)
				{
					PathNodes.RemoveAt(PathNodes.Count - 1);
				}
				_pathNodeDistance = pathDist;
				MoveToLocX = checkLocX;
				MoveToLocY = checkLocY;
				break;
			}
			else
			{
				pathDist += magn;
			}
		}

		return found;
	}

	public void DifferentTerritoryDestination(ref int destLocX, ref int destLocY)
	{
		int curLocX = NextLocX;
		int curLocY = NextLocY;

		Location loc = World.GetLoc(curLocX, curLocY);
		int regionId = loc.RegionId;
		int xStep = destLocX - curLocX;
		int yStep = destLocY - curLocY;
		int absXStep = Math.Abs(xStep);
		int absYStep = Math.Abs(yStep);
		int count = (absXStep >= absYStep) ? absXStep : absYStep;

		int sameTerr = 0;

		//------------------------------------------------------------------------------//
		// draw a line from the unit location to the destination,
		// find the last location with the same region id.
		//------------------------------------------------------------------------------//
		for (int i = 1; i <= count; i++)
		{
			int locX = curLocX + (i * xStep) / count;
			int locY = curLocY + (i * yStep) / count;

			loc = World.GetLoc(locX, locY);
			if (loc.RegionId == regionId)
				sameTerr = i;
		}

		if (sameTerr != 0 && count != 0)
		{
			destLocX = curLocX + (sameTerr * xStep) / count;
			destLocY = curLocY + (sameTerr * yStep) / count;
		}
		else
		{
			destLocX = curLocX;
			destLocY = curLocY;
		}
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
			SetIdle();

			if (ActionMode2 == UnitConstants.ACTION_MOVE) //--------- used to terminate ActionMode == ACTION_MOVE
			{
				ForceMove = false;

				//------- reset ACTION_MOVE parameters ------//
				ResetActionParameters();
				if (MoveToLocX == ActionLocX2 && MoveToLocY == ActionLocY2)
					ResetActionParameters2();
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

		MoveToLocX = NextLocX;
		MoveToLocY = NextLocY;

		CurFrame = 1;

		ResetPath();
		SetIdle();
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
			MoveToLocX = preX;
			MoveToLocY = preY;

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
			if (Search(targetXLoc, targetYLoc, 1, SeekPath.SEARCH_MODE_TO_ATTACK, ActionParam) != 0)
				return 1;
			else // search failure,
			{
				Stop2(UnitConstants.KEEP_DEFENSE_MODE);
				return 0;
			}
		}
		else
		{
			//--------------- different territory ------------------//
			int targetWidth = targetUnit.SpriteInfo.LocWidth;
			int targetHeight = targetUnit.SpriteInfo.LocHeight;
			int maxRange = MaxAttackRange();

			if (PossiblePlaceForRangeAttack(targetXLoc, targetYLoc, targetWidth, targetHeight, maxRange))
			{
				//---------------------------------------------------------------------------------//
				// space is found, attack target now
				//---------------------------------------------------------------------------------//
				if (MoveToRangeAttack(targetXLoc, targetYLoc, targetUnit.SpriteResId, SeekPath.SEARCH_MODE_ATTACK_UNIT_BY_RANGE, maxRange) != 0)
					return 1;
				else
				{
					Stop2(UnitConstants.KEEP_DEFENSE_MODE);
					return 0;
				}

				return 1;
			}
			else
			{
				//---------------------------------------------------------------------------------//
				// unable to find location to attack the target, stop or move to the target
				//---------------------------------------------------------------------------------//
				if (ActionMode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
				    ActionMode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
				    ActionMode2 != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
					MoveTo(targetXLoc, targetYLoc, 1); // abort attacking, just call move_to() instead
				else
					Stop2(UnitConstants.KEEP_DEFENSE_MODE);
				return 0;
			}
		}

		return 0;
	}
	
	private void MoveToMyLoc(Unit unit)
	{
		int unitDestX, unitDestY;
		if (unit.ActionMode2 == UnitConstants.ACTION_MOVE)
		{
			unitDestX = unit.ActionLocX2;
			unitDestY = unit.ActionLocY2;
		}
		else
		{
			unitDestX = unit.MoveToLocX;
			unitDestY = unit.MoveToLocY;
		}

		//--------------- init parameters ---------------//
		int unitCurX = unit.NextLocX;
		int unitCurY = unit.NextLocY;
		int destX = ActionLocX2;
		int destY = ActionLocY2;
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
			if (unit.ActionMode2 == UnitConstants.ACTION_STOP || unit.ActionMode2 == UnitConstants.ACTION_MOVE)
			{
				//---------- activate unit pointed by unit now ------------//
				unit.ActionMode = unit.ActionMode2 = UnitConstants.ACTION_MOVE;
				unit.ActionParam = unit.ActionPara2 = 0;
				if (destX != -1 && destY != -1)
				{
					unit.ActionLocX = unit.ActionLocX2 = destX;
					unit.ActionLocY = unit.ActionLocY2 = destY;
				}
				else
				{
					World.GetLocXAndLocY(unit.PathNodes[^1], out int lastNodeLocX, out int lastNodeLocY);
					unit.ActionLocX = unit.ActionLocX2 = lastNodeLocX;
					unit.ActionLocY = unit.ActionLocY2 = lastNodeLocY;
				}
			}

			//----------------- set unit movement parameters -----------------//
			unit.PathNodeIndex = 0;
			unit._pathNodeDistance = _pathNodeDistance - moveScale;
			unit.MoveToLocX = MoveToLocX;
			unit.MoveToLocY = MoveToLocY;
			unit.NextMove();
		}

		//------------------------------------------------------------------//
		// setting for this unit
		//------------------------------------------------------------------//
		int shouldWait = 0;
		if (NextX == unit.CurX && NextY == unit.CurY)
		{
			ResetPath();
		}
		else
		{
			TerminateMove();
			shouldWait++;
			_pathNodeDistance = moveScale;
		}

		GoX = unit.CurX;
		GoY = unit.CurY;
		MoveToLocX = unitCurX;
		MoveToLocY = unitCurY;

		if (ActionMode2 == UnitConstants.ACTION_MOVE)
		{
			ActionLocX = ActionLocX2 = unitDestX;
			ActionLocY = ActionLocY2 = unitDestY;
		}

		//---------- note: the cur_dir is already the correct direction ---------------//
		PathNodes.Clear();
		PathNodes.Add(World.GetMatrixIndex(curX, curY));
		PathNodes.Add(World.GetMatrixIndex(unitCurX, unitCurY));
		//TODO check this
		PathNodeIndex = 1;
		if (shouldWait != 0)
			SetWait(); // wait for the blocking unit to move first
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
			SearchOrStop(MoveToLocX, MoveToLocY, 1);
			//search(move_to_x_loc, move_to_y_loc, 1);
			return;
		}

		if (NextLocX == MoveToLocX && NextLocY == MoveToLocY && !Swapping)
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
		BlockedByMember = 1;

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
				if (unit.NationId == NationId)
					HandleBlockedWait(unit); // check for cycle wait for our nation
				else if (WaitingTerm >= UnitConstants.MAX_WAITING_TERM_DIFF)
				{
					SearchOrStop(MoveToLocX, MoveToLocY, 1); // recall searching
					WaitingTerm = 0;
				}
				else // wait
					SetWait();

				return;

			//------------------------------------------------------------------------------------//
			// We know from the cur_action of the blocking unit it is moving to another locations,
			// the blocked unit wait for a number of terms or search again.
			//------------------------------------------------------------------------------------//
			case SPRITE_MOVE:
			case SPRITE_READY_TO_MOVE:
			case SPRITE_SHIP_EXTRA_MOVE:
				// don't wait for caravans, and caravans don't wait for other units
				if (UnitType != UnitConstants.UNIT_CARAVAN && unit.UnitType == UnitConstants.UNIT_CARAVAN)
				{
					Search(MoveToLocX, MoveToLocY, 1, SeekPath.SEARCH_MODE_A_UNIT_IN_GROUP);
				}
				else
				{
					waitTerm = (NationId == unit.NationId) ? UnitConstants.MAX_WAITING_TERM_SAME : UnitConstants.MAX_WAITING_TERM_DIFF;
					if (WaitingTerm >= waitTerm)
					{
						SearchOrWait();
						WaitingTerm = 0;
					}
					else
						SetWait();
				}

				return;

			//------------------------------------------------------------------------------------//
			// handling blocked for idle unit
			//------------------------------------------------------------------------------------//
			case SPRITE_IDLE:
				if (unit.ActionMode == UnitConstants.ACTION_SHIP_TO_BEACH)
				{
					//----------------------------------------------------------------------//
					// the blocking unit is trying to move to beach, so wait a number of terms,
					// or call searching again
					//----------------------------------------------------------------------//
					if (Math.Abs(unit.NextLocX - unit.ActionLocX2) <= moveStep &&
					    Math.Abs(unit.NextLocY - unit.ActionLocY2) <= moveStep &&
					    TerrainRes[World.GetLoc(unit.ActionLocX2, unit.ActionLocY2).TerrainId].average_type != TerrainTypeCode.TERRAIN_OCEAN)
					{
						if (ActionMode2 == UnitConstants.ACTION_SHIP_TO_BEACH &&
						    ActionLocX2 == unit.ActionLocX2 && ActionLocY2 == unit.ActionLocY2)
						{
							ShipToBeach(ActionLocX2, ActionLocY2, out _, out _);
						}
						else
						{
							waitTerm = (NationId == unit.NationId)
								? UnitConstants.MAX_WAITING_TERM_SAME
								: UnitConstants.MAX_WAITING_TERM_DIFF;
							if (WaitingTerm >= waitTerm)
								Stop2();
							else
								SetWait();
						}

						return;
					}
				}

				if (unit.NationId == NationId) //-------- same nation
				{
					//------------------------------------------------------------------------------------//
					// units from our nation
					//------------------------------------------------------------------------------------//
					if (unit.GroupId == GroupId)
					{
						//--------------- from the same group -----------------//
						if (WayPoints.Count != 0 && unit.WayPoints.Count == 0)
						{
							//------------ reset way point --------------//
							Stop2();
							ResetWayPoints();
						}
						else if ((unit.NextLocX != MoveToLocX || unit.NextLocY != MoveToLocY) &&
						         (unit.CurAction == SPRITE_IDLE && unit.ActionMode2 == UnitConstants.ACTION_STOP))
							if (ConfigAdv.fix_path_blocked_by_team)
								HandleBlockedByIdleUnit(unit);
							else
								MoveToMyLoc(unit); // push the blocking unit and exchange their destination
						else if (unit.ActionMode == UnitConstants.ACTION_SETTLE)
							SetWait(); // wait for the settler
						else if (WaitingTerm > UnitConstants.MAX_WAITING_TERM_SAME)
						{
							//---------- stop if wait too long ----------//
							TerminateMove();
							WaitingTerm = 0;
						}
						else
							SetWait();
					}
					else if (unit.ActionMode2 == UnitConstants.ACTION_STOP)
						HandleBlockedByIdleUnit(unit);
					else if (WayPoints.Count != 0 && unit.WayPoints.Count == 0)
					{
						Stop2();
						ResetWayPoints();
					}
					else
						SearchOrStop(MoveToLocX, MoveToLocY, 1); // recall A* algorithm by default mode
				}
				else // different nation
				{
					//------------------------------------------------------------------------------------//
					// units from other nations
					//------------------------------------------------------------------------------------//
					if (unit.NextLocX == MoveToLocX && unit.NextLocY == MoveToLocY)
					{
						TerminateMove(); // destination occupied by other unit

						if (ActionMode == UnitConstants.ACTION_ATTACK_UNIT &&
						    unit.NationId != NationId && unit.SpriteId == ActionParam)
						{
							SetDir(NextX, NextY, unit.NextX, unit.NextY);
							if (IsDirCorrect())
								AttackUnit(ActionParam, 0, 0, true);
							else
								SetTurn();
							CurFrame = 1;
						}
					}
					else
						SearchOrStop(MoveToLocX, MoveToLocY, 1); // recall A* algorithm by default mode
				}

				return;

			//------------------------------------------------------------------------------------//
			// don't wait for attackers from other nations, search for another path.
			//------------------------------------------------------------------------------------//
			case SPRITE_ATTACK:
				//----------------------------------------------------------------//
				// don't wait for other nation unit, call searching again
				//----------------------------------------------------------------//
				if (NationId != unit.NationId)
				{
					SearchOrStop(MoveToLocX, MoveToLocY, 1);
					return;
				}

				//------------------------------------------------------------------------------------//
				// for attackers owned by our commander, handled blocked case by case as follows.
				//------------------------------------------------------------------------------------//
				switch (unit.ActionMode)
				{
					case UnitConstants.ACTION_ATTACK_UNIT:
						if (ActionParam != 0 && !UnitArray.IsDeleted(ActionParam))
						{
							Unit target = UnitArray[ActionParam];
							HandleBlockedAttackUnit(unit, target);
						}
						else
							SearchOrStop(MoveToLocX, MoveToLocY, 1, SeekPath.SEARCH_MODE_A_UNIT_IN_GROUP);

						break;

					case UnitConstants.ACTION_ATTACK_FIRM:
						if (unit.ActionParam == 0 || FirmArray.IsDeleted(unit.ActionParam))
							SetWait();
						else
							HandleBlockedAttackFirm(unit);
						break;

					case UnitConstants.ACTION_ATTACK_TOWN:
						if (unit.ActionParam == 0 || TownArray.IsDeleted(unit.ActionParam))
							SetWait();
						else
							HandleBlockedAttackTown(unit);
						break;

					case UnitConstants.ACTION_ATTACK_WALL:
						if (unit.ActionParam != 0)
							SetWait();
						else
							HandleBlockedAttackWall(unit);
						break;

					case UnitConstants.ACTION_GO_CAST_POWER:
						SetWait();
						break;

					default:
						break;
				}

				return;

			//------------------------------------------------------------------------------------//
			// the blocked unit can pass after the blocking unit disappears in air.
			//------------------------------------------------------------------------------------//
			case SPRITE_DIE:
				SetWait(); // assume this unit will not wait too long
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
			SetWait();
			return;
		}

		Stop(UnitConstants.KEEP_DEFENSE_MODE);

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
			CycleWaitUnitArrayDefSize = arraySize;
			CycleWaitUnitIndex = 0;
			CycleWaitUnitArrayMultiplier = 1;
			CycleWaitUnitArray = new int[CycleWaitUnitArrayDefSize];

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
				if (blocked && (blockedUnit.MoveToLocX != blockedUnit.CurLocX ||
				                blockedUnit.MoveToLocY != blockedUnit.CurLocY))
				{
					if (loc.UnitId(MobileType) == SpriteId)
						cycleWait = 1;
					else
					{
						for (i = 0; i < CycleWaitUnitIndex; i++)
						{
							//---------- checking for forever loop ----------------//
							if (CycleWaitUnitArray[i] == blockedUnit.SpriteId)
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
						if (CycleWaitUnitIndex >= arraySize)
						{
							CycleWaitUnitArrayMultiplier++;
							arraySize = CycleWaitUnitArrayDefSize * CycleWaitUnitArrayMultiplier;
							int[] cycle_wait_unit_array_new = new int[arraySize];
							for (int j = 0; j < CycleWaitUnitArray.Length; j++)
							{
								cycle_wait_unit_array_new[j] = CycleWaitUnitArray[j];
							}

							CycleWaitUnitArray = cycle_wait_unit_array_new;
						}
						else
						{
							//-------- store recno of next blocked unit ----------//
							CycleWaitUnitArray[CycleWaitUnitIndex++] = blockedUnit.SpriteId;
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
			CycleWaitUnitArray = null;
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
			SetMove();
			World.SetUnitId(unit.CurLocX, unit.CurLocY, MobileType, SpriteId);
			World.SetUnitId(CurLocX, CurLocY, MobileType, backupSpriteRecno);
			Swapping = true;
		}
		else // not in a cycle
		{
			SetWait();

			//if(waiting_term>=MAX_WAITING_TERM_SAME)
			if (WaitingTerm >= UnitConstants.MAX_WAITING_TERM_SAME * MoveStepCoeff())
			{
				//-----------------------------------------------------------------//
				// codes used to speed up frame rate
				//-----------------------------------------------------------------//
				loc = World.GetLoc(MoveToLocX, MoveToLocY);
				if (!loc.CanMove(MobileType) && ActionMode2 != UnitConstants.ACTION_MOVE)
					Stop(UnitConstants.KEEP_PRESERVE_ACTION); // let reactivate..() call searching later
				else
					SearchOrWait();

				WaitingTerm = 0;
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
			nextUnit.SetMove();
			World.SetUnitId(blockedUnit.CurLocX, blockedUnit.CurLocY,
				nextUnit.MobileType, nextUnit.SpriteId);
			World.SetUnitId(nextUnit.CurLocX, nextUnit.CurLocY, nextUnit.MobileType, 0);
			nextUnit.Swapping = true;
		}
		else // the cycle shift is ended
		{
			nextUnit.SetNext(CurX, CurY, -stepMagn, 1);
			nextUnit.SetMove();
			World.SetUnitId(CurLocX, CurLocY, nextUnit.MobileType, nextUnit.SpriteId);
			World.SetUnitId(nextUnit.CurLocX, nextUnit.CurLocY, nextUnit.MobileType, 0);

			nextUnit.Swapping = true;
		}
	}

	private void HandleBlockedAttackUnit(Unit unit, Unit target)
	{
		if (ActionParam == target.SpriteId && unit.ActionParam == target.SpriteId &&
		    ActionMode == unit.ActionMode)
		{
			//----------------- both attack the same target --------------------//
			HandleBlockedSameTargetAttack(unit, target);
		}
		else
		{
			SearchOrStop(MoveToLocX, MoveToLocY, 1, SeekPath.SEARCH_MODE_A_UNIT_IN_GROUP); // recall A* algorithm
		}
		//search(move_to_x_loc, move_to_y_loc, 1, SEARCH_MODE_A_UNIT_IN_GROUP); // recall A* algorithm
	}

	private void HandleBlockedAttackFirm(Unit unit)
	{
		if (ActionLocX == unit.ActionLocX && ActionLocY == unit.ActionLocY &&
		    ActionParam == unit.ActionParam && ActionMode == unit.ActionMode)
		{
			//------------- both attacks the same firm ------------//
			Location loc = World.GetLoc(ActionLocX, ActionLocY);
			if (!loc.IsFirm())
				Stop2(UnitConstants.KEEP_DEFENSE_MODE); // stop since firm is deleted
			else
			{
				Firm firm = FirmArray[ActionParam];
				FirmInfo firmInfo = FirmRes[firm.firm_id];

				if (SpaceForAttack(ActionLocX, ActionLocY, UnitConstants.UNIT_LAND,
					    firmInfo.loc_width, firmInfo.loc_height))
				{
					//------------ found surrounding place to attack the firm -------------//
					if (MobileType == UnitConstants.UNIT_LAND)
						SetMoveToSurround(firm.loc_x1, firm.loc_y1,
							firmInfo.loc_width, firmInfo.loc_height, UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO);
					else
						AttackFirm(firm.loc_x1, firm.loc_y1);
				}
				else // no surrounding place found, stop now
					Stop(UnitConstants.KEEP_PRESERVE_ACTION);
			}
		}
		else // let process_idle() handle it
			Stop();
	}

	private void HandleBlockedAttackTown(Unit unit)
	{
		if (ActionLocX == unit.ActionLocX && ActionLocY == unit.ActionLocY &&
		    ActionParam == unit.ActionParam && ActionMode == unit.ActionMode)
		{
			//---------------- both attacks the same town ----------------------//
			Location loc = World.GetLoc(ActionLocX, ActionLocY);
			if (!loc.IsTown())
				Stop2(UnitConstants.KEEP_DEFENSE_MODE); // stop since town is deleted
			else if (SpaceForAttack(ActionLocX, ActionLocY, UnitConstants.UNIT_LAND,
				         InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT))
			{
				//------------ found surrounding place to attack the town -------------//
				Town town = TownArray[ActionParam];
				{
					if (MobileType == UnitConstants.UNIT_LAND)
						SetMoveToSurround(town.LocX1, town.LocY1,
							InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, UnitConstants.BUILDING_TYPE_TOWN_MOVE_TO);
					else
						AttackTown(town.LocX1, town.LocY1);
				}
			}
			else // no surrounding place found, stop now
				Stop(UnitConstants.KEEP_PRESERVE_ACTION);
		}
		else
			Stop();
	}

	private void HandleBlockedAttackWall(Unit unit)
	{
		if (ActionLocX == unit.ActionLocX && ActionLocY == unit.ActionLocY && ActionMode == unit.ActionMode)
		{
			//------------- both attacks the same wall ------------//
			Location loc = World.GetLoc(ActionLocX, ActionLocY);
			if (!loc.IsWall())
				Stop2(UnitConstants.KEEP_DEFENSE_MODE); // stop since wall is deleted
			else if (SpaceForAttack(ActionLocX, ActionLocY, UnitConstants.UNIT_LAND, 1, 1))
			{
				//------------ found surrounding place to attack the wall -------------//
				// search for a unit only, not for a group
				if (MobileType == UnitConstants.UNIT_LAND)
					SetMoveToSurround(ActionLocX, ActionLocY, 1, 1, UnitConstants.BUILDING_TYPE_WALL);
				else
					AttackWall(ActionLocX, ActionLocY);
			}
			else // no surrounding place found, stop now
				Stop(UnitConstants.KEEP_PRESERVE_ACTION); // no space available, so stop to wait for space to attack the wall
		}
		else
		{
			if (ActionLocX == -1 || ActionLocY == -1)
				Stop();
			else
				SetWait();
		}
	}

	private void HandleBlockedSameTargetAttack(Unit unit, Unit target)
	{
		//----------------------------------------------------------//
		// this unit is now waiting and the unit pointed by unit
		// is attacking the unit pointed by target
		//----------------------------------------------------------//
		if (SpaceForAttack(ActionLocX, ActionLocY, target.MobileType,
			    target.SpriteInfo.LocWidth, target.SpriteInfo.LocHeight))
		{
			SearchOrStop(MoveToLocX, MoveToLocY, 1, SeekPath.SEARCH_MODE_TO_ATTACK, target.SpriteId);
			//search(move_to_x_loc, move_to_y_loc, 1, SEARCH_MODE_TO_ATTACK, target.sprite_recno);
		}
		else if (InAnyDefenseMode())
		{
			GeneralDefendModeDetectTarget();
		}
		else if (Misc.points_distance(NextLocX, NextLocY, ActionLocX, ActionLocY) < UnitConstants.ATTACK_DETECT_DISTANCE)
		{
			//------------------------------------------------------------------------//
			// if the target is within the detect range, stop the unit's action to detect
			// another target if any exist. In case, there is no other target, the unit
			// will still attack the original target since it is the only target in the
			// detect range
			//------------------------------------------------------------------------//
			Stop2(UnitConstants.KEEP_DEFENSE_MODE);
		}
		else
			SetWait(); // set wait to stop the movement
	}

	private bool ShipToBeachPathEdit(ref int resultLocX, ref int resultYLoc, int regionId)
	{
		int curLocX = NextLocX;
		int curLocY = NextLocY;
		if (Math.Abs(curLocX - resultLocX) <= 1 && Math.Abs(curLocY - resultYLoc) <= 1)
			return true;

		//--------------- find a path to land area -------------------//
		int result = Search(resultLocX, resultYLoc, 1, SeekPath.SEARCH_MODE_TO_LAND_FOR_SHIP, regionId);
		if (result == 0)
			return true;

		curLocX = NextLocX;
		curLocY = NextLocY;

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
			int checkXLoc = curLocX;
			int checkYLoc = curLocY;
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
			for (pathDist = 0, found = 0, i = 1; i < nodeCount; ++i, curNodeIndex++, nextNodeIndex++)
			{
				curNode = PathNodes[curNodeIndex];
				World.GetLocXAndLocY(curNode, out curNodeLocX, out curNodeLocY);
				nextNode = PathNodes[nextNodeIndex];
				World.GetLocXAndLocY(nextNode, out nextNodeLocX, out nextNodeLocY);
				int vecX = nextNodeLocX - curNodeLocX;
				int vecY = nextNodeLocY - curNodeLocY;
				int xMagn = Math.Abs(vecX);
				int yMagn = Math.Abs(vecY);
				int magn = (xMagn > yMagn) ? xMagn : yMagn;
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

					MoveToLocX = preXLoc;
					MoveToLocY = preYLoc;
					loc = World.GetLoc((preXLoc + checkXLoc) / 2, (preYLoc + checkYLoc) / 2);
					if (TerrainRes[loc.TerrainId].average_type != TerrainTypeCode.TERRAIN_OCEAN)
					{
						resultLocX = (preXLoc + checkXLoc) / 2;
						resultYLoc = (preYLoc + checkYLoc) / 2;
					}
					else
					{
						resultLocX = checkXLoc;
						resultYLoc = checkYLoc;
					}

					break;
				}
				else
				{
					pathDist += magn;
				}
			}

			if (found == 0)
			{
				World.GetLocXAndLocY(PathNodes[^1], out int endNodeLocX, out int endNodeLocY);
				if (Math.Abs(endNodeLocX - resultLocX) > 1 || Math.Abs(endNodeLocY - resultYLoc) > 1)
				{
					//TODO: preserveAction should be 1?
					MoveTo(resultLocX, resultYLoc, -1);
					return false;
				}
			}
		}
		else
		{
			//------------- scan for the surrounding for a land location -----------//
			for (int i = 2; i <= 9; ++i)
			{
				Misc.cal_move_around_a_point(i, 3, 3, out int xShift, out int yShift);
				int checkLocX = curLocX + xShift;
				int checkLocY = curLocY + yShift;
				if (!Misc.IsLocationValid(checkLocX, checkLocY))
					continue;

				Location loc = World.GetLoc(checkLocX, checkLocY);
				if (loc.RegionId != regionId)
					continue;

				if (TerrainRes[loc.TerrainId].average_type != TerrainTypeCode.TERRAIN_OCEAN && loc.CanMove(UnitConstants.UNIT_LAND))
				{
					resultLocX = checkLocX;
					resultYLoc = checkLocY;
					return true;
				}
			}

			return false;
		}

		return true;
	}

	private void ShipLeaveBeach(int shipOldLocX, int shipOldLocY)
	{
		//--------------------------------------------------------------------------------//
		// scan for location to leave the beach
		//--------------------------------------------------------------------------------//
		int curLocX = NextLocX;
		int curLocY = NextLocY;
		int checkLocX = -1, checkLocY = -1;
		bool found = false;

		//------------- find a location to leave the beach ------------//
		for (int i = 2; i <= 9; i++)
		{
			Misc.cal_move_around_a_point(i, 3, 3, out int xShift, out int yShift);
			checkLocX = curLocX + xShift;
			checkLocY = curLocY + yShift;

			if (!Misc.IsLocationValid(checkLocX, checkLocY))
				continue;

			if (checkLocX % 2 != 0 || checkLocY % 2 != 0)
				continue;

			Location loc = World.GetLoc(checkLocX, checkLocY);
			if (TerrainRes[loc.TerrainId].average_type == TerrainTypeCode.TERRAIN_OCEAN && loc.CanMove(MobileType))
			{
				found = true;
				break;
			}
		}

		if (!found)
			return; // no suitable location, wait until finding suitable location

		//---------------- leave now --------------------//
		SetDir(shipOldLocX, shipOldLocY, checkLocX, checkLocY);
		SetShipExtraMove();
		GoX = checkLocX * InternalConstants.CellWidth;
		GoY = checkLocY * InternalConstants.CellHeight;
	}

	private void BurnPathEdit(int burnLocX, int burnLocY)
	{
		if (PathNodes.Count == 0)
			return;

		//--------------------------------------------------------//
		// edit the result path such that the unit can reach the burning location surrounding
		// there should be at least two nodes, and should take at least two steps to the destination
		//--------------------------------------------------------//

		int lastNode1 = PathNodes[^1]; // the last node
		World.GetLocXAndLocY(lastNode1, out int lastNode1LocX, out int lastNode1LocY);
		int lastNode2 = PathNodes[^2]; // the node before the last node
		World.GetLocXAndLocY(lastNode2, out int lastNode2LocX, out int lastNode2LocY);

		int vX = lastNode1LocX - lastNode2LocX; // get the vectors direction
		int vY = lastNode1LocY - lastNode2LocY;
		int vDirX = (vX != 0) ? vX / Math.Abs(vX) : 0;
		int vDirY = (vY != 0) ? vY / Math.Abs(vY) : 0;

		if (PathNodes.Count > 2) // go_? should not be the burning location 
		{
			if (Math.Abs(vX) > 1 || Math.Abs(vY) > 1)
			{
				lastNode1LocX -= vDirX;
				lastNode1LocY -= vDirY;
				PathNodes[^1] = World.GetMatrixIndex(lastNode1LocX, lastNode1LocY);

				MoveToLocX = lastNode1LocX;
				MoveToLocY = lastNode1LocY;
			}
			else // move only one step
			{
				//TODO check
				PathNodes.RemoveAt(PathNodes.Count - 1); // remove a node
				MoveToLocX = lastNode2LocX;
				MoveToLocY = lastNode2LocY;
			}
		}
		else // go_? may be the burning location
		{
			lastNode1LocX -= vDirX;
			lastNode1LocY -= vDirY;
			PathNodes[^1] = World.GetMatrixIndex(lastNode1LocX, lastNode1LocY);

			if (GoX >> InternalConstants.CellWidthShift == burnLocX && GoY >> InternalConstants.CellHeightShift == burnLocY)
			{
				// go_? is the burning location
				//--- edit parameters such that only moving to the nearby location to do the action ---//

				GoX = lastNode1LocX * InternalConstants.CellWidth;
				GoY = lastNode1LocY * InternalConstants.CellHeight;
			}
			//else the unit is still doing something else, no action here

			MoveToLocX = lastNode1LocX;
			MoveToLocY = lastNode1LocY;
		}

		_pathNodeDistance--;
	}

	public void SelectSearchSubMode(int sx, int sy, int dx, int dy, int nationId)
	{
		if (!ConfigAdv.unit_allow_path_power_mode)
		{
			// cancel the selection
			SeekPath.SetSubMode();
			return;
		}

		if (NationId == 0 || IgnorePowerNation != 0)
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
		Nation nation = NationArray[nationId];

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
		if (!Misc.IsLocationValid(destLocX, destLocY) || IsUnitDead())
		{
			//TODO check, this code should be never executed
			Stop2(UnitConstants.KEEP_DEFENSE_MODE); //-********** BUGHERE, err_handling for retailed version
			return 1;
		}

		int result = 0;
		if (UnitRes[UnitType].unit_class == UnitConstants.UNIT_CLASS_SHIP)
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

		if (WayPoints.Count > 0 && PathNodes.Count == 0) // can move no more
			ResetWayPoints();

		// 0 means extra_move_in_beach != UnitMarine.NO_EXTRA_MOVE
		return result != 0 ? 1 : 0;
	}

	private int Searching(int destLocX, int destLocY, int preserveAction, int searchMode, int miscNo, int numOfPaths)
	{
		Stop(preserveAction); // stop the unit as soon as possible

		int startLocX = NextLocX; // next location the sprite is moving towards
		int startLocY = NextLocY;

		MoveToLocX = destLocX;
		MoveToLocY = destLocY;

		//------------------------------------------------------------//
		// fast checking for destination == current location
		//------------------------------------------------------------//
		if (startLocX == destLocX && startLocY == destLocY) // already here
		{
			if (CurX != NextX || CurY != NextY)
				SetMove();
			else
				SetIdle();

			return 1;
		}

		//------------------------ find the shortest path --------------------------//
		// decide the searching to use according to the unit size
		// assume the unit size is always 1x1, 2x2, 3x3 and so on
		// i.e. sprite_info.loc_width == sprite_info.loc_height
		//--------------------------------------------------------------------------//

		ResetPath();

		SeekPath.SetNationId(NationId);

		if (MobileType == UnitConstants.UNIT_LAND)
			SelectSearchSubMode(startLocX, startLocY, destLocX, destLocY, NationId);
		int seekResult = SeekPath.Seek(startLocX, startLocY, destLocX, destLocY, GroupId, MobileType, searchMode, miscNo, numOfPaths);

		PathNodes.AddRange(SeekPath.GetResult(out _pathNodeDistance));
		SeekPath.SetSubMode(); // reset sub_mode searching

		//-----------------------------------------------------------------------//
		// update ignore_power_nation
		//-----------------------------------------------------------------------//

		if (AIUnit)
		{
			//------- set ignore_power_nation -------//

			if (seekResult == SeekPath.PATH_IMPOSSIBLE)
			{
				switch (IgnorePowerNation)
				{
					case 0:
						IgnorePowerNation = 1;
						break;
					case 1:
						IgnorePowerNation = 2;
						break;
					case 2:
						break;
				}
			}
			else
			{
				if (IgnorePowerNation == 1)
					IgnorePowerNation = 0;
			}
		}

		//-----------------------------------------------------------------------//
		// if closest node is returned, the destination should not be the real
		// location to go to. Thus, move_to_?_loc should be adjusted
		//-----------------------------------------------------------------------//
		if (PathNodes.Count > 0)
		{
			int lastNode = PathNodes[^1];
			World.GetLocXAndLocY(lastNode, out int locX, out int locY);
			MoveToLocX = locX;
			MoveToLocY = locY;

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
			MoveToLocX = startLocX;
			MoveToLocY = startLocY;

			if (CurX != NextX || CurY != NextY)
				SetMove();
			else
				SetIdle();
		}

		return 1;
	}

	private void SearchOrStop(int destLocX, int destLocY, int preserveAction = 0, int searchMode = 1, int miscNo = 0)
	{
		Location loc = World.GetLoc(destLocX, destLocY);
		if (!loc.CanMove(MobileType))
		{
			Stop(UnitConstants.KEEP_PRESERVE_ACTION); // let reactivate..() call searching later
		}
		else
		{
			Search(destLocX, destLocY, preserveAction, searchMode, miscNo);
		}
	}

	private void SearchOrWait()
	{
		const int SQUARE1 = 9;
		const int SQUARE2 = 25;
		const int SQUARE3 = 49;
		const int DIMENSION = 7;

		int curLocX = NextLocX, curLocY = NextLocY;
		int[] surrArray = new int[SQUARE3];
		bool hasFree = false;
		bool shouldWait = false;

		//----------------------------------------------------------------------------//
		// wait if the unit is totally blocked.  Otherwise, call searching
		//----------------------------------------------------------------------------//
		for (int i = 2; i <= SQUARE3; i++)
		{
			if (i == SQUARE1 || i == SQUARE2 || i == SQUARE3)
			{
				if (!hasFree)
				{
					shouldWait = true;
					break;
				}
				
				hasFree = false;
			}

			Misc.cal_move_around_a_point(i, DIMENSION, DIMENSION, out int xShift, out int yShift);
			int checkLocX = curLocX + xShift;
			int checkLocY = curLocY + yShift;
			if (!Misc.IsLocationValid(checkLocX, checkLocY))
				continue;

			Location location = World.GetLoc(checkLocX, checkLocY);
			if (!location.HasUnit(MobileType))
			{
				hasFree = true;
				continue;
			}

			int unitId = location.UnitId(MobileType);
			if (UnitArray.IsDeleted(unitId))
				continue;

			Unit unit = UnitArray[unitId];
			if (unit.NationId == NationId && unit.GroupId == GroupId &&
			    ((unit.CurAction == SPRITE_WAIT && unit.WaitingTerm > 1) || unit.CurAction == SPRITE_TURN || unit.CurAction == SPRITE_MOVE))
			{
				surrArray[i - 2] = unitId;
				unit.GroupId++;
			}
		}

		//------------------- call searching if should not wait --------------------//
		if (!shouldWait)
			Search(MoveToLocX, MoveToLocY, 1, SeekPath.SEARCH_MODE_IN_A_GROUP);

		for (int i = 0; i < SQUARE3; i++)
		{
			if (surrArray[i] != 0)
			{
				Unit unit = UnitArray[surrArray[i]];
				unit.GroupId--;
			}
		}

		if (shouldWait)
			SetWait();
	}
	
	protected void ResetPath()
	{
		PathNodes.Clear();
		PathNodeIndex = -1;
		_pathNodeDistance = 0;
	}

	public void AddWayPoint(int locX, int locY)
	{
		if (WayPoints.Count > 1) // don't allow to remove the 1st node, since the unit is moving there
		{
			for (int i = WayPoints.Count - 1; i >= 0; i--)
			{
				World.GetLocXAndLocY(WayPoints[i], out int wayPointLocX, out int wayPointLocY);
				if (wayPointLocX == locX && wayPointLocY == locY) // remove this node
				{
					WayPoints.RemoveAt(i);
					return; // there should be one and only one node with the same value
				}
			}
		}

		WayPoints.Add(World.GetMatrixIndex(locX, locY));

		if (WayPoints.Count == 1)
			MoveTo(locX, locY);
	}

	public void ResetWayPoints()
	{
		//------------------------------------------------------------------------------------//
		// There are only two conditions to reset the wayPoints
		// 1) ActionMode2 != ACTION_MOVE in Unit.Stop()
		// 2) Dest? != Node? in the first node of wayPoints in calling Unit.MoveTo()
		//------------------------------------------------------------------------------------//
		WayPoints.Clear();
	}

	private void ProcessWayPoint()
	{
		if (WayPoints.Count > 1)
		{
			World.GetLocXAndLocY(WayPoints[1], out int wayPointLocX, out int wayPointLocY);
			WayPoints.RemoveAt(1);
			MoveTo(wayPointLocX, wayPointLocY);
		}
		else // only one unprocessed node
		{
			World.GetLocXAndLocY(WayPoints[0], out int wayPointLocX, out int wayPointLocY);
			MoveTo(wayPointLocX, wayPointLocY);
		}
	}
}