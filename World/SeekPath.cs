using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class SeekPath
{
	public const int PATH_WAIT = 0;
	public const int PATH_FOUND = 1;
	public const int PATH_IMPOSSIBLE = 2;

	public const int SEARCH_MODE_IN_A_GROUP = 1;
	public const int SEARCH_MODE_A_UNIT_IN_GROUP = 2;
	public const int SEARCH_MODE_TO_ATTACK = 3;
	//public const int SEARCH_MODE_REUSE = 4;
	//public const int SEARCH_MODE_BLOCKING = 5;
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

	public const int SEARCH_SUB_MODE_NORMAL = 0;
	public const int SEARCH_SUB_MODE_PASSABLE = 1;

	public int PathStatus { get; private set; } = PATH_WAIT;

	private readonly int[] _nodeMatrix = new int[GameConstants.MapSize * GameConstants.MapSize];
	private readonly bool[] _nationPassable = new bool[GameConstants.MAX_NATION + 1];
	private int _searchSubMode;

	private int _groupId;
	private int _searchMode;
	private int _mobileType;
	private int _seekNationId;
	private int _attackRange; // used in search_mode = SEARCH_MODE_ATTACK_UNIT_BY_RANGE

	// used in search_mode = SEARCH_MODE_TO_ATTACK or SEARCH_MODE_TO_VEHICLE, get from miscNo
	private int _targetId;
	private int _regionId; // used in search_mode = SEARCH_MODE_TO_LAND_FOR_SHIP
	private int _buildingId; // used in search_mode = SEARCH_MODE_TO_FIRM or SEARCH_MODE_TO_TOWN, get from miscNo
	private int _buildingX1, _buildingY1, _buildingX2, _buildingY2;

	private int _finalDestX; //	in search_mode SEARCH_MODE_REUSE, dest_x and dest_y may set to a different value.
	private int _finalDestY; // i.e. the value used finally may not be the real dest_? given.

	private static UnitArray UnitArray => Sys.Instance.UnitArray;

	public SeekPath()
	{
	}

	public void SetAttackRange(int attackRange)
	{
		_attackRange = attackRange;
	}

	public void ResetAttackRange()
	{
		_attackRange = 0;
	}

	public void SetNationId(int nationId)
	{
		_seekNationId = nationId;
	}

	public void SetNationPassable(bool[] nationPassable)
	{
		for (int i = 0; i < _nationPassable.Length; i++)
		{
			_nationPassable[i] = nationPassable[i];
		}
	}

	public void SetSubMode(int subMode = SEARCH_SUB_MODE_NORMAL)
	{
		_searchSubMode = subMode;
	}

	private bool CanMoveTo(int locX, int locY)
	{
		Location location = Sys.Instance.World.GetLoc(locX, locY);
		Unit unit;
		int cargoId;
		int powerNationId = location.PowerNationId;
		int unitCurrentAction;

		switch (_mobileType)
		{
			case UnitConstants.UNIT_LAND:
				if (_searchSubMode == SEARCH_SUB_MODE_PASSABLE && (powerNationId != 0) && !_nationPassable[powerNationId])
					return false;

				if (_searchMode < SEARCH_MODE_TO_FIRM) //------ be careful for the checking for search_mode>=SEARCH_MODE_TO_FIRM
				{
					if (!location.Walkable())
						return false;

					cargoId = location.CargoId;
					if (cargoId == 0)
						return true;

					switch (_searchMode)
					{
						case SEARCH_MODE_IN_A_GROUP: // group move
						//case SEARCH_MODE_REUSE: // path-reuse
							break;

						case SEARCH_MODE_A_UNIT_IN_GROUP: // a unit in a group
							unit = UnitArray[cargoId];
							//TODO what's the difference between caravan and trader?
							return unit.cur_action == Sprite.SPRITE_MOVE && unit.unit_id != UnitConstants.UNIT_CARAVAN;

						case SEARCH_MODE_TO_ATTACK: // to attack target
						case SEARCH_MODE_TO_VEHICLE: // move to a vehicle
							if (cargoId == _targetId)
								return true;
							break;

						//TODO looks like unused
						//case SEARCH_MODE_BLOCKING: // 2x2 unit blocking
							//unit = UnitArray[cargoId];
							//return unit.unit_group_id == _groupId &&
							       //(unit.cur_action == Sprite.SPRITE_MOVE || unit.cur_action == Sprite.SPRITE_READY_TO_MOVE);
					}
				}
				else
				{
					//--------------------------------------------------------------------------------//
					// for the following _searchMode, location may be treated as walkable although it is not.
					//--------------------------------------------------------------------------------//
					switch (_searchMode)
					{
						case SEARCH_MODE_TO_FIRM: // move to a firm, (location may be not walkable)
						case SEARCH_MODE_TO_TOWN: // move to a town zone, (location may be not walkable)
							if (!location.Walkable())
								return (locX >= _buildingX1 && locX <= _buildingX2 && locY >= _buildingY1 && locY <= _buildingY2);
							break;

						case SEARCH_MODE_TO_WALL_FOR_GROUP: // move to wall for a group, (location may be not walkable)
							if (!location.Walkable())
								return (locX == _finalDestX && locY == _finalDestY);
							break;

						case SEARCH_MODE_TO_WALL_FOR_UNIT: // move to wall for a unit only, (location may be not walkable)
							return (location.Walkable() && location.CargoId == 0) || (locX == _finalDestX && locY == _finalDestY);

						case SEARCH_MODE_ATTACK_UNIT_BY_RANGE: // same as that used in SEARCH_MODE_TO_FIRM
						case SEARCH_MODE_ATTACK_FIRM_BY_RANGE:
						case SEARCH_MODE_ATTACK_TOWN_BY_RANGE:
						case SEARCH_MODE_ATTACK_WALL_BY_RANGE:
							if (!location.Walkable())
								return (locX >= _buildingX1 && locX <= _buildingX2 && locY >= _buildingY1 && locY <= _buildingY2);
							break;
					}

					cargoId = location.CargoId;
					if (cargoId == 0)
						return true;
				}

				//------- checking for unit's groupId, curAction, nationId and position --------//
				unit = UnitArray[cargoId];
				unitCurrentAction = unit.cur_action;
				return (unit.unit_group_id == _groupId && unitCurrentAction != Sprite.SPRITE_ATTACK) ||
				       (unitCurrentAction == Sprite.SPRITE_MOVE &&
				        unit.cur_x - unit.next_x <= InternalConstants.CellWidth / 2 &&
				        unit.cur_y - unit.next_y <= InternalConstants.CellHeight / 2) ||
				       (unit.nation_recno == _seekNationId && unitCurrentAction == Sprite.SPRITE_IDLE);

			case UnitConstants.UNIT_SEA:
				if (_searchMode < SEARCH_MODE_TO_FIRM) //--------- be careful for the search_mode >= SEARCH_MODE_TO_FIRM
				{
					if (!location.Sailable())
						return false;

					cargoId = location.CargoId;
					if (cargoId == 0)
						return true;

					switch (_searchMode)
					{
						case SEARCH_MODE_IN_A_GROUP: // group move
						//case SEARCH_MODE_REUSE: // path-reuse
							break;

						case SEARCH_MODE_A_UNIT_IN_GROUP: // a unit in a group
							return UnitArray[cargoId].cur_action == Sprite.SPRITE_MOVE;

						case SEARCH_MODE_TO_ATTACK:
							if (cargoId == _targetId)
								return true;
							break;
					}
				}
				else
				{
					//--------------------------------------------------------------------------------//
					// for the following _searchMode, location may be treated as sailable although it is not.
					//--------------------------------------------------------------------------------//
					switch (_searchMode)
					{
						case SEARCH_MODE_TO_FIRM: // move to a firm, (location may be not walkable)
						case SEARCH_MODE_TO_TOWN: // move to a town zone, (location may be not walkable)
							if (!location.Sailable())
								return (locX >= _buildingX1 && locX <= _buildingX2 && locY >= _buildingY1 && locY <= _buildingY2);
							break;

						//case SEARCH_MODE_TO_WALL_FOR_GROUP:	// move to wall for a group, (location may be not walkable)
						//case SEARCH_MODE_TO_WALL_FOR_UNIT:	// move to wall for a unit only, (location may be not walkable)

						case SEARCH_MODE_ATTACK_UNIT_BY_RANGE: // same as that used in SEARCH_MODE_TO_FIRM
						case SEARCH_MODE_ATTACK_FIRM_BY_RANGE:
						case SEARCH_MODE_ATTACK_TOWN_BY_RANGE:
						case SEARCH_MODE_ATTACK_WALL_BY_RANGE:
							if (!location.Sailable())
								return (locX >= _buildingX1 && locX <= _buildingX2 && locY >= _buildingY1 && locY <= _buildingY2);
							break;

						case SEARCH_MODE_TO_LAND_FOR_SHIP:
							if (location.Sailable())
							{
								cargoId = location.CargoId;
								if (cargoId == 0)
									return true;

								unit = UnitArray[cargoId];
								unitCurrentAction = unit.cur_action;
								return (unit.unit_group_id == _groupId && unitCurrentAction != Sprite.SPRITE_ATTACK &&
								        unit.action_mode2 != UnitConstants.ACTION_SHIP_TO_BEACH) ||
								       (unit.unit_group_id != _groupId && unitCurrentAction == Sprite.SPRITE_MOVE);
							}
							else
							{
								return location.Walkable() && location.RegionId == _regionId;
							}
					}

					cargoId = location.CargoId;
					if (cargoId == 0)
						return true;
				}

				//------- checking for unit's groupId, curAction, nationId and position --------//
				unit = UnitArray[cargoId];
				unitCurrentAction = unit.cur_action;
				//TODO different condition with UNIT_LAND
				return (unit.unit_group_id == _groupId && unitCurrentAction != Sprite.SPRITE_ATTACK) ||
				       unitCurrentAction == Sprite.SPRITE_MOVE ||
				       (unit.nation_recno == _seekNationId && unitCurrentAction == Sprite.SPRITE_IDLE);

			case UnitConstants.UNIT_AIR:
				cargoId = location.AirCargoId;
				if (cargoId == 0)
					return true;

				switch (_searchMode)
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
						unit = UnitArray[cargoId];
						unitCurrentAction = unit.cur_action;
						return (unit.unit_group_id == _groupId && unitCurrentAction != Sprite.SPRITE_ATTACK) ||
						       unitCurrentAction == Sprite.SPRITE_MOVE ||
						       (unit.nation_recno == _seekNationId && unitCurrentAction == Sprite.SPRITE_IDLE);

					case SEARCH_MODE_A_UNIT_IN_GROUP: // a unit in a group
						return UnitArray[cargoId].cur_action == Sprite.SPRITE_MOVE;
				}

				return false;
		}

		return false;
	}

	public int Seek(int sx, int sy, int dx, int dy, int groupId, int mobileType, int searchMode = SEARCH_MODE_IN_A_GROUP,
		int miscNo = 0, int numOfPath = 1)
	{
		PathStatus = PATH_WAIT;
		
		//-------- initialize vars --------------//
		_searchMode = searchMode;
		_groupId = groupId;
		_mobileType = mobileType;

		//------------------------------------------------------------------------------//
		// extract information from the parameter "miscNo"
		//------------------------------------------------------------------------------//
		_targetId = _buildingId = 0;
		_buildingX1 = _buildingY1 = _buildingX2 = _buildingY2 = -1;
		FirmInfo searchFirmInfo = null;

		switch (_searchMode)
		{
			case SEARCH_MODE_TO_ATTACK:
			case SEARCH_MODE_TO_VEHICLE:
				_targetId = miscNo;
				break;

			case SEARCH_MODE_TO_FIRM:
				_buildingId = miscNo;
				_buildingX1 = dx; // upper left corner location
				_buildingY1 = dy;
				searchFirmInfo = Sys.Instance.FirmRes[_buildingId];
				_buildingX2 = dx + searchFirmInfo.loc_width - 1;
				_buildingY2 = dy + searchFirmInfo.loc_height - 1;
				break;

			case SEARCH_MODE_TO_TOWN:
				_buildingId = miscNo;
				_buildingX1 = dx; // upper left corner location
				_buildingY1 = dy;
				if (miscNo != -1)
				{
					Location location = Sys.Instance.World.GetLoc(dx, dy);
					Town targetTown = Sys.Instance.TownArray[location.TownId()];
					_buildingX2 = targetTown.LocX2;
					_buildingY2 = targetTown.LocY2;
				}
				else // searching to settle. Detail explained in function SetMoveToSurround()
				{
					_buildingX2 = _buildingX1 + InternalConstants.TOWN_WIDTH - 1;
					_buildingY2 = _buildingY1 + InternalConstants.TOWN_HEIGHT - 1;
				}

				break;

			case SEARCH_MODE_ATTACK_UNIT_BY_RANGE:
			case SEARCH_MODE_ATTACK_WALL_BY_RANGE:
				_buildingId = miscNo;
				_buildingX1 = Math.Max(dx - _attackRange, 0);
				_buildingY1 = Math.Max(dy - _attackRange, 0);
				_buildingX2 = Math.Min(dx + _attackRange, GameConstants.MapSize - 1);
				_buildingY2 = Math.Min(dy + _attackRange, GameConstants.MapSize - 1);
				break;

			case SEARCH_MODE_ATTACK_FIRM_BY_RANGE:
				_buildingId = miscNo;
				_buildingX1 = Math.Max(dx - _attackRange, 0);
				_buildingY1 = Math.Max(dy - _attackRange, 0);
				searchFirmInfo = Sys.Instance.FirmRes[_buildingId];
				_buildingX2 = Math.Min(dx + searchFirmInfo.loc_width - 1 + _attackRange, GameConstants.MapSize - 1);
				_buildingY2 = Math.Min(dy + searchFirmInfo.loc_height - 1 + _attackRange, GameConstants.MapSize - 1);
				break;

			case SEARCH_MODE_ATTACK_TOWN_BY_RANGE:
				_buildingId = miscNo;
				_buildingX1 = Math.Max(dx - _attackRange, 0);
				_buildingY1 = Math.Max(dy - _attackRange, 0);
				_buildingX2 = Math.Min(dx + InternalConstants.TOWN_WIDTH - 1 + _attackRange, GameConstants.MapSize - 1);
				_buildingY2 = Math.Min(dy + InternalConstants.TOWN_HEIGHT - 1 + _attackRange, GameConstants.MapSize - 1);
				break;

			case SEARCH_MODE_TO_LAND_FOR_SHIP:
				_regionId = miscNo;
				break;
		}

		//------------------------------------------------------------------------------//
		// adjust destination for some kind of searching
		//------------------------------------------------------------------------------//
		switch (_searchMode)
		{
			case SEARCH_MODE_TO_FIRM:
			case SEARCH_MODE_TO_TOWN:
				//TODO why use the middle?
				_finalDestX = (_buildingX1 + _buildingX2) / 2; // the destination is set to be the middle of the building
				_finalDestY = (_buildingY1 + _buildingY2) / 2;

				//---------------------------------------------------------------------------------//
				// for group assign/settle, the final destination is adjusted by the value of numOfPath
				//---------------------------------------------------------------------------------//
				int area;
				if (_searchMode == SEARCH_MODE_TO_TOWN)
					area = InternalConstants.TOWN_WIDTH * InternalConstants.TOWN_HEIGHT;
				else // search_mode == SEARCH_MODE_TO_FIRM
					area = searchFirmInfo.loc_width * searchFirmInfo.loc_height;

				int pathNum = (numOfPath > area) ? (numOfPath - 1) % area + 1 : numOfPath;

				int xShift, yShift;
				if (_searchMode == SEARCH_MODE_TO_TOWN)
					Misc.cal_move_around_a_point(pathNum, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, out xShift, out yShift);
				else
					Misc.cal_move_around_a_point(pathNum, searchFirmInfo.loc_width, searchFirmInfo.loc_height, out xShift, out yShift);

				_finalDestX += xShift;
				_finalDestY += yShift;
				_finalDestX = Math.Max(_finalDestX, 0);
				_finalDestX = Math.Min(_finalDestX, GameConstants.MapSize - 1);
				_finalDestY = Math.Max(_finalDestY, 0);
				_finalDestY = Math.Min(_finalDestY, GameConstants.MapSize - 1);
				break;

			case SEARCH_MODE_ATTACK_UNIT_BY_RANGE:
			case SEARCH_MODE_ATTACK_FIRM_BY_RANGE:
			case SEARCH_MODE_ATTACK_TOWN_BY_RANGE:
			case SEARCH_MODE_ATTACK_WALL_BY_RANGE:
				_finalDestX = (_buildingX1 + _buildingX2) / 2; // the destination is set to be the middle of the building
				_finalDestY = (_buildingY1 + _buildingY2) / 2;
				break;

			default:
				_finalDestX = dx;
				_finalDestY = dy;
				break;
		}

		if (sx == _finalDestX && sy == _finalDestY)
			return PATH_FOUND;

		Array.Clear(_nodeMatrix);
		_nodeMatrix[sy * GameConstants.MapSize + sx] = 1;
		List<int> oldChangedNodesX = new List<int>(GameConstants.MapSize);
		List<int> oldChangedNodesY = new List<int>(GameConstants.MapSize);
		List<int> newChangedNodesX = new List<int>(GameConstants.MapSize);
		List<int> newChangedNodesY = new List<int>(GameConstants.MapSize);
		oldChangedNodesX.Add(sx);
		oldChangedNodesY.Add(sy);

		int loopCount = 0;
		while (oldChangedNodesX.Count > 0)
		{
			loopCount++;
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
						if (nearX < 0 || nearX >= GameConstants.MapSize || nearY < 0 || nearY >= GameConstants.MapSize)
							continue;

						int nearIndex = nearY * GameConstants.MapSize + nearX;
						if (_nodeMatrix[nearIndex] == -1)
							continue;

						if (_nodeMatrix[nearIndex] == 0 && !CanMoveTo(nearX, nearY))
						{
							_nodeMatrix[nearIndex] = -1;
							continue;
						}

						int newValue = 0;
						if (nearX == x || nearY == y)
							newValue = _nodeMatrix[currentIndex] + 2;
						else
							newValue = _nodeMatrix[currentIndex] + 3;

						if (_nodeMatrix[nearIndex] == 0 || _nodeMatrix[nearIndex] > newValue)
						{
							_nodeMatrix[nearIndex] = newValue;
							newChangedNodesX.Add(nearX);
							newChangedNodesY.Add(nearY);
						}
					}

					if (x == _finalDestX && y == _finalDestY && _nodeMatrix[currentIndex] > 0)
					{
						PathStatus = PATH_FOUND;
						return PathStatus;
					}
				}
			}
			
			if (loopCount == Misc.points_distance(sx, sy, _finalDestX, _finalDestY) * 2 && CheckTargetInaccessable(loopCount / 2))
				break;

			oldChangedNodesX.Clear();
			oldChangedNodesX.AddRange(newChangedNodesX);
			oldChangedNodesY.Clear();
			oldChangedNodesY.AddRange(newChangedNodesY);
		}

		PathStatus = PATH_IMPOSSIBLE;
		return PathStatus;
	}

	private bool CheckTargetInaccessable(int size)
	{
		if (size % 2 == 0)
			size++;
		int[,] matrix = new int[size, size];
		int targetLocXIndex = (size - 1) / 2;
		int targetLocYIndex = (size - 1) / 2;
		matrix[targetLocXIndex, targetLocYIndex] = 1;

		bool hasChanges = true;
		while (hasChanges)
		{
			hasChanges = false;
			for (int indexX = 0; indexX < size; indexX++)
			{
				for (int indexY = 0; indexY < size; indexY++)
				{
					if (matrix[indexX, indexY] == 1)
					{
						for (int i = -1; i <= 1; i++)
						{
							for (int j = -1; j <= 1; j++)
							{
								int indexNearX = indexX + i;
								int indexNearY = indexY + j;
								if (indexNearX < 0 || indexNearX >= size || indexNearY < 0 || indexNearY >= size ||
								    (indexNearX == indexX && indexNearY == indexY))
									continue;

								int nearLocX = _finalDestX + (indexNearX - targetLocXIndex);
								int nearLocY = _finalDestY + (indexNearY - targetLocYIndex);
								if (nearLocX < 0 || nearLocX >= GameConstants.MapSize || nearLocY < 0 || nearLocY >= GameConstants.MapSize)
									continue;

								if (matrix[indexNearX, indexNearY] == -1)
									continue;

								if (matrix[indexNearX, indexNearY] == 0)
								{
									if (CanMoveTo(nearLocX, nearLocY))
									{
										matrix[indexNearX, indexNearY] = 1;
										hasChanges = true;
									}
									else
									{
										matrix[indexNearX, indexNearY] = -1;
									}
								}
							}
						}
					}
				}
			}
		}

		for (int i = 0; i < size; i++)
		{
			if (matrix[0, i] == 1 || matrix[size - 1, i] == 1 || matrix[i, 0] == 1 || matrix[i, size - 1] == 1)
				return false;
		}

		return true;
	}

	public List<int> GetResult(out int pathDist)
	{
		//TODO pathDist calculated not like that in the original version
		pathDist = 0;
		int resultIndex = -1;
		int resultX = -1;
		int resultY = -1;
		switch (PathStatus)
		{
			case PATH_FOUND:
				resultIndex = _finalDestY * GameConstants.MapSize + _finalDestX;
				resultX = _finalDestX;
				resultY = _finalDestY;
				break;

			case PATH_IMPOSSIBLE:
				int regionSize = 0;
				int minDistance = Int32.MaxValue;

				void UpdateFunc(int locX, int locY)
				{
					if (!Misc.IsLocationValid(locX, locY))
						return;

					int currentIndex = locY * GameConstants.MapSize + locX;
					if (_nodeMatrix[currentIndex] > 0 && _nodeMatrix[currentIndex] < minDistance)
					{
						minDistance = _nodeMatrix[currentIndex];
						resultIndex = currentIndex;
						resultX = locX;
						resultY = locY;
					}
				}

				while (true)
				{
					regionSize++;
					for (int x = _finalDestX - regionSize; x <= _finalDestX + regionSize; x++)
					{
						UpdateFunc(x, _finalDestY - regionSize);
						UpdateFunc(x, _finalDestY + regionSize);
					}

					for (int y = _finalDestY - regionSize; y <= _finalDestY + regionSize; y++)
					{
						UpdateFunc(_finalDestX - regionSize, y);
						UpdateFunc(_finalDestX + regionSize, y);
					}

					if (resultIndex >= 0 || regionSize >= GameConstants.MapSize)
						break;
				}

				break;
		}

		if (resultIndex == -1)
			return new List<int>();

		int pathX = resultX;
		int pathY = resultY;
		int pathValue = _nodeMatrix[resultIndex];
		pathDist = pathValue / 2;

		List<int> reversedPath = new List<int>(pathValue);
		reversedPath.Add(resultIndex);
		while (true)
		{
			int pathXCopy = pathX;
			int pathYCopy = pathY;
			int pathIndex = -1;
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
					if (_nodeMatrix[currentIndex] > 0 && _nodeMatrix[currentIndex] < pathValue)
					{
						pathIndex = currentIndex;
						pathValue = _nodeMatrix[currentIndex];
						pathX = nearX;
						pathY = nearY;
					}
				}
			}

			reversedPath.Add(pathIndex);

			if (pathValue == 1)
				break;
		}
		
		reversedPath.Reverse();
		return reversedPath;
	}
}