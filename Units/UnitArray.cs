using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class UnitArray : SpriteArray
{
	private const int MAX_TARGET_SIZE = 4;
	private const int MAX_UNIT_SURROUND_SIZE = MAX_TARGET_SIZE + 2;
	private const int SHIFT_ADJUST = 1;
	
    public int CurGroupId { get; set; }            // for Unit::unit_group_id
    public int CurTeamId { get; set; }             // for Unit::team_id

    public int IdleBlockedUnitResetCount { get; set; } // used to improve performance for searching related to attack

    private readonly List<int> _selectedLandUnits = new List<int>();
    private readonly List<int> _selectedSeaUnits = new List<int>();
    private readonly List<int> _selectedAirUnits = new List<int>();

    public override Unit this[int recNo] => (Unit)base[recNo];

    private Power Power => Sys.Instance.Power;
    private World World => Sys.Instance.World;
    private SeekPath SeekPath => Sys.Instance.SeekPath;
    private TerrainRes TerrainRes => Sys.Instance.TerrainRes;
    private FirmRes FirmRes => Sys.Instance.FirmRes;
    private UnitRes UnitRes => Sys.Instance.UnitRes;
    private GodRes GodRes => Sys.Instance.GodRes;
    private NationArray NationArray => Sys.Instance.NationArray;
    private FirmArray FirmArray => Sys.Instance.FirmArray;
    private TownArray TownArray => Sys.Instance.TownArray;
    private RebelArray RebelArray => Sys.Instance.RebelArray;
    
    public UnitArray()
    {
	    CurGroupId = 1;
	    CurTeamId  = 1;
    }

    protected override Sprite CreateNewObject(int objectType)
    {
	    UnitInfo unitInfo = UnitRes[objectType];
	    switch (unitInfo.unit_class)
	    {
		    case UnitConstants.UNIT_CLASS_HUMAN:
			    return new UnitHuman();
		    
		    case UnitConstants.UNIT_CLASS_WEAPON:
			    return objectType == UnitConstants.UNIT_EXPLOSIVE_CART ? new UnitExpCart() : new UnitWeapon();

		    case UnitConstants.UNIT_CLASS_CARAVAN:
			    return new UnitCaravan();

		    case UnitConstants.UNIT_CLASS_SHIP:
			    return new UnitMarine();
		    
		    case UnitConstants.UNIT_CLASS_GOD:
			    return new UnitGod();
		    
		    case UnitConstants.UNIT_CLASS_MONSTER:
			    return new UnitMonster();

	    }
	    throw new NotSupportedException();
    }

    public Unit AddUnit(int unitType, int nationId, int rankId = 0, int unitLoyalty = 0, int startLocX = -1, int startLocY = -1)
    {
	    Unit unit = (Unit)AddSprite(unitType);
	    unit.Init(unitType, nationId, rankId, unitLoyalty, startLocX, startLocY);
	    return unit;
    }

    public void DeleteUnit(Unit unit)
    {
	    DeleteSprite(unit);
    }

    public override bool IsDeleted(int recNo)
    {
	    if (base.IsDeleted(recNo))
		    return true;

	    Unit unit = this[recNo];
	    return unit.HitPoints <= 0 || unit.CurAction == Sprite.SPRITE_DIE || unit.ActionMode == UnitConstants.ACTION_DIE;
    }

    public override void Process()
    {
	    foreach (Unit unit in this)
	    {
		    int unitId = unit.SpriteId;

		    // only process each firm once per day
		    if (unitId % InternalConstants.FRAMES_PER_DAY == Sys.Instance.FrameNumber % InternalConstants.FRAMES_PER_DAY)
		    {
			    if ((unitId % (365 * InternalConstants.FRAMES_PER_DAY) == Sys.Instance.FrameNumber % (365 * InternalConstants.FRAMES_PER_DAY))
			        && unit.UnitType != UnitConstants.UNIT_CARAVAN)
			    {
				    unit.IgnorePowerNation = 0;
			    }

			    unit.NextDay();

			    if (IsDeleted(unitId))
				    continue;

			    if (unit.AIUnit)
				    unit.ProcessAI();

			    //----- if it's an independent unit -------//

			    else if (unit.NationId == 0 && unit.RaceId != 0 && unit.SpyId == 0)
				    unit.ThinkIndependentUnit();
		    }
	    }

	    if (IdleBlockedUnitResetCount < 50)
		    IdleBlockedUnitResetCount++; // the ability to restart idle blocked attacking unit

	    base.Process();
    }
    
    protected override void Die(int unitId)
    {
	    Unit unit = this[unitId];
	    unit.Die();

	    DeleteUnit(unit);
    }

    public void DisappearInTown(Unit unit, Town town)
    {
	    if (unit.UnitMode == UnitConstants.UNIT_MODE_REBEL)
		    RebelArray.SettleTown(unit, town);

	    DeleteUnit(unit);
    }

    public void DisappearInFirm(int unitId)
    {
	    DeleteUnit(this[unitId]);
    }

    public void Stop(List<int> selectedUnits, int remoteAction)
    {
	    //-------- if it's a multiplayer game --------//
	    //
	    // Queue the action.
	    //
	    // Local: this action will be processed when all action messages of all
	    //			 remote players are ready from the next frame.
	    //
	    // Remote: this action will be processed when the remote has received
	    //			  frame sync notification from all other players.
	    //
	    //--------------------------------------------//

	    /*if (!remoteAction && remote.is_enable())
	    {
		    short* shortPtr = (short*)remote.new_send_queue_msg(MSG_UNIT_STOP, sizeof(short) * (1 + selectedCount));
		    shortPtr[0] = selectedCount;
		    memcpy(shortPtr + 1, selectedUnitArray, sizeof(short) * selectedCount);
	    }*/
	    //else
	    //{
		    int curGroupId = CurGroupId++;

		    //-------------- stop now ---------------//

		    foreach (var selectedUnit in selectedUnits)
		    {
			    Unit unit = this[selectedUnit];
			    unit.GroupId = curGroupId;
			    unit.Stop2();
		    }
		    //}
    }

    private void StopAttackNation(Unit unit, int targetNationId)
    {
	    if (!unit.IsAttackAction())
		    return;
	    
	    if (unit.IsAttackAction())
	    {
		    int targetType;
		    int targetId;
		    int targetLocX, targetLocY;

		    if (unit.ActionMode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET)
		    {
			    targetType = unit.ActionMode;
			    targetId = unit.ActionParam;
			    targetLocX = unit.ActionLocX;
			    targetLocY = unit.ActionLocY;
		    }
		    else
		    {
			    targetType = unit.ActionMode2;
			    targetId = unit.ActionPara2;
			    targetLocX = unit.ActionLocX2;
			    targetLocY = unit.ActionLocY2;
		    }

		    switch (targetType)
		    {
			    case UnitConstants.ACTION_ATTACK_UNIT:
			    case UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET:
			    case UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET:
				    if (IsDeleted(targetId))
					    return;

				    Unit targetUnit = this[targetId];
				    if (targetUnit.NationId == targetNationId)
					    unit.Stop2(UnitConstants.KEEP_DEFENSE_MODE);
				    break;

			    case UnitConstants.ACTION_ATTACK_FIRM:
				    if (FirmArray.IsDeleted(targetId))
					    return;

				    Firm targetFirm = FirmArray[targetId];
				    if (targetFirm.NationId == targetNationId)
					    unit.Stop2(UnitConstants.KEEP_DEFENSE_MODE);
				    break;

			    case UnitConstants.ACTION_ATTACK_TOWN:
				    if (TownArray.IsDeleted(targetId))
					    return;

				    Town targetTown = TownArray[targetId];
				    if (targetTown.NationId == targetNationId)
					    unit.Stop2(UnitConstants.KEEP_DEFENSE_MODE);
				    break;

			    case UnitConstants.ACTION_ATTACK_WALL:
				    Location targetLoc = World.GetLoc(targetLocX, targetLocY);
				    if (targetLoc.WallNationId() == targetNationId)
					    unit.Stop2(UnitConstants.KEEP_DEFENSE_MODE);
				    break;
		    }
	    }
    }

    public void StopAllWar(int nationId)
    {
	    foreach (Unit unit in this)
	    {
		    if (!unit.IsVisible())
			    continue;

		    if (unit.NationId == nationId)
		    {
			    if (unit.IsAttackAction())
				    unit.Stop2(UnitConstants.KEEP_DEFENSE_MODE);
		    }
		    else
		    {
			    StopAttackNation(unit, nationId);
		    }
	    }
    }

    public void StopWarBetween(int nationId1, int nationId2)
    {
	    foreach (Unit unit in this)
	    {
		    if (unit.NationId == nationId1)
			    StopAttackNation(unit, nationId2);
		    if (unit.NationId == nationId2)
			    StopAttackNation(unit, nationId1);
	    }
    }

    public void StopAttackUnit(int unitId)
    {
	    if (NationBase.nation_hand_over_flag != 0)
		    return;

	    foreach (Unit unit in this)
	    {
		    if (!unit.IsVisible())
			    continue;

		    if ((unit.ActionParam == unitId && unit.ActionMode == UnitConstants.ACTION_ATTACK_UNIT) ||
		        (unit.ActionPara2 == unitId && unit.ActionMode2 == UnitConstants.ACTION_ATTACK_UNIT))
			    unit.Stop2(UnitConstants.KEEP_DEFENSE_MODE);
	    }
    }

    public void StopAttackFirm(int firmId)
    {
	    if (NationBase.nation_hand_over_flag != 0)
		    return;

	    foreach (Unit unit in this)
	    {
		    if (!unit.IsVisible())
			    continue;

		    if ((unit.ActionParam == firmId && unit.ActionMode == UnitConstants.ACTION_ATTACK_FIRM) ||
		        (unit.ActionPara2 == firmId && unit.ActionMode2 == UnitConstants.ACTION_ATTACK_FIRM))
			    unit.Stop2(UnitConstants.KEEP_DEFENSE_MODE);
	    }
    }

    public void StopAttackTown(int townId)
    {
	    if (NationBase.nation_hand_over_flag != 0)
		    return;

	    foreach (Unit unit in this)
	    {
		    if (!unit.IsVisible())
			    continue;

		    if ((unit.ActionParam == townId && unit.ActionMode == UnitConstants.ACTION_ATTACK_TOWN) ||
		        (unit.ActionPara2 == townId && unit.ActionMode2 == UnitConstants.ACTION_ATTACK_TOWN))
			    unit.Stop2(UnitConstants.KEEP_DEFENSE_MODE);
	    }
    }

    private void DivideUnits(int locX, int locY, List<int> selectedUnits, int excludeSelectedLocUnit = 0)
    {
	    _selectedLandUnits.Clear();
	    _selectedSeaUnits.Clear();
	    _selectedAirUnits.Clear();

	    int excludeUnitId = 0;
	    if (excludeSelectedLocUnit != 0)
	    {
		    Location location = World.GetLoc(locX, locY);
		    int targetMobileType = location.HasAnyUnit();
		    excludeUnitId = targetMobileType != 0 ? location.UnitId(targetMobileType) : 0;
	    }

	    for (int i = 0; i < selectedUnits.Count; i++)
	    {
		    int selectedUnitId = selectedUnits[i];
		    if (excludeUnitId != 0 && selectedUnitId == excludeUnitId)
			    continue;

		    Unit unit = this[selectedUnitId];
		    switch (unit.MobileType)
		    {
			    case UnitConstants.UNIT_LAND:
				    _selectedLandUnits.Add(selectedUnitId);
				    break;

			    case UnitConstants.UNIT_SEA:
				    _selectedSeaUnits.Add(selectedUnitId);
				    break;

			    case UnitConstants.UNIT_AIR:
				    _selectedAirUnits.Add(selectedUnitId);
				    break;
		    }
	    }
    }
	
    public int DivideAttackByNation(int nationId, List<int> selectedUnits)
    {
	    // elements before i are pass, elements on or after passCount are not pass
	    int passCount = selectedUnits.Count;
	    for (int i = 0; i < passCount;)
	    {
		    int unitId = selectedUnits[i];

		    // a clocked spy cannot be commanded by original nation to attack
		    if (!IsDeleted(unitId) && this[unitId].NationId == nationId)
		    {
			    // pass
			    i++;
		    }
		    else
		    {
			    // fail, swap [i] with [passCount - 1]
			    passCount--;

			    selectedUnits[i] = selectedUnits[passCount];
			    selectedUnits[passCount] = unitId;
		    }
	    }

	    return passCount;
    }
	
    private void SetGroupId(List<int> selectedUnits)
    {
	    int curGroupId = CurGroupId++;

	    for (int i = 0; i < selectedUnits.Count; i++)
	    {
		    Unit unit = this[selectedUnits[i]];

		    unit.GroupId = curGroupId;

		    if (unit.CurAction == Sprite.SPRITE_IDLE) //**maybe need to include SPRITE_ATTACK as well
			    unit.SetReady();
	    }
    }
    
    public void MoveTo(int destLocX, int destLocY, bool divided, List<int> selectedUnits, int remoteAction)
    {
	    //-------- if it's a multiplayer game --------//
	    /*if (!remoteAction && remote.is_enable())
	    {
		    short* shortPtr = (short*)remote.new_send_queue_msg(MSG_UNIT_MOVE, sizeof(short) * (4 + selectedCount));

		    shortPtr[0] = destLocX;
		    shortPtr[1] = destLocY;
		    shortPtr[2] = selectedUnits.Count;
		    shortPtr[3] = divided;

		    memcpy(shortPtr + 4, selectedUnits, sizeof(int) * selectedUnits.Count);
		    return;
	    }*/
	    
	    if (!divided)
	    {
		    DivideUnits(destLocX, destLocY, selectedUnits);

		    if (_selectedLandUnits.Count > 0)
			    MoveTo(destLocX, destLocY, true, _selectedLandUnits, InternalConstants.COMMAND_AUTO);

		    if (_selectedSeaUnits.Count > 0)
		    {
			    Location location = World.GetLoc(destLocX, destLocY);
			    if (TerrainRes[location.TerrainId].average_type == TerrainTypeCode.TERRAIN_OCEAN)
				    MoveTo(destLocX, destLocY, true, _selectedSeaUnits, InternalConstants.COMMAND_AUTO);
			    else
				    ShipToBeach(destLocX, destLocY, true, _selectedSeaUnits, InternalConstants.COMMAND_AUTO);
		    }

		    if (_selectedAirUnits.Count > 0)
			    MoveTo(destLocX, destLocY, true, _selectedAirUnits, InternalConstants.COMMAND_AUTO);
	    }
	    else
	    {
		    int curGroupId = CurGroupId++;

		    for (int i = 0; i < selectedUnits.Count; i++)
		    {
			    Unit unit = this[selectedUnits[i]];
			    unit.GroupId = curGroupId;
			    unit.ActionMode = UnitConstants.ACTION_MOVE;
			    unit.ActionParam = 0;

			    if (unit.ActionMode2 != UnitConstants.ACTION_MOVE)
			    {
				    unit.ActionMode2 = UnitConstants.ACTION_MOVE;
				    unit.ActionPara2 = 0;
				    unit.ActionLocX2 = unit.ActionLocY2 = -1;
			    } // else keep the data to check whether same action mode is ordered
		    }

		    //--------------------------------------------------------------//
		    // if only the leader unit is moving, no need to use formation movement although the button is pushed
		    //--------------------------------------------------------------//
		    if (selectedUnits.Count == 1)
		    {
			    Unit unit = this[selectedUnits[0]];
			    unit.MoveTo(destLocX, destLocY, 1);
		    }
		    else
		    {
			    MoveToNowWithFilter(destLocX, destLocY, selectedUnits);
			    //TODO why reset sub mode?
			    Unit firstUnit = this[selectedUnits[0]];
			    if (firstUnit.MobileType == UnitConstants.UNIT_LAND)
				    SeekPath.SetSubMode();
		    }
	    }
    }

    //TODO: check and rewrite
	private void MoveToNowWithFilter(int destLocX, int destLocY, List<int> selectedUnits)
	{
		//-------------- no filtering for unit air --------------------------//
		Unit unit = this[selectedUnits[0]];
		if (unit.MobileType == UnitConstants.UNIT_AIR)
		{
			MoveToNow(destLocX, destLocY, selectedUnits);
			return;
		}

		//----------------- init data structure --------------//
		List<int> filtered_unit_array = new List<int>();
		List<int> filtering_unit_array = new List<int>();
		filtering_unit_array.AddRange(selectedUnits);
		int filtering_unit_count = selectedUnits.Count;

		int unprocessCount = selectedUnits.Count;
		int filterRegionId = World.GetLoc(destLocX, destLocY).RegionId;
		int filterDestX = destLocX;
		int filterDestY = destLocY;

		//-------------------------------------------------------------------------------//
		// group the unit by their region id and process group searching for each group
		//-------------------------------------------------------------------------------//
		// checking for unprocessCount+1, plus one for the case that unit not on the same territory of destination
		for (int loopCount = 0; loopCount <= unprocessCount; loopCount++)
		{
			filtered_unit_array.Clear();
			int filteringCount = filtering_unit_array.Count;
			filtering_unit_count = 0;

			//-------------- filter for filterRegionId --------------//
			for (int i = 0; i < filteringCount; i++)
			{
				unit = this[filtering_unit_array[i]];
				if (World.GetLoc(unit.NextLocX, unit.NextLocY).RegionId == filterRegionId)
					filtered_unit_array.Add(filtering_unit_array[i]);
				else
					filtering_unit_array[filtering_unit_count++] = filtering_unit_array[i];
			}

			//---- process for filtered_unit_array and prepare for looping ----//
			if (filtered_unit_array.Count > 0)
				MoveToNow(filterDestX, filterDestY, filtered_unit_array);

			if (filtering_unit_count == 0)
				break;

			//---------------- update parameters for next checking ------------------//
			unit = this[filtering_unit_array[0]];
			filterRegionId = World.GetLoc(unit.NextLocX, unit.NextLocY).RegionId;
			filterDestX = destLocX;
			filterDestY = destLocY;
			//TODO: maybe do not call this but just move to the destination?
			unit.DifferentTerritoryDestination(ref filterDestX, ref filterDestY);
		}
	}

	private void MoveToNow(int destLocX, int destLocY, List<int> selectedUnits)
	{
		//------------ define vars -----------------------//
		int unprocessCount; // = selectedCount;		// num. of unprocessed sprite
		int k; // for counting
		int vecX, vecY; // used to reset x, y
		int oddCount, evenCount;
		Unit firstUnit = this[selectedUnits[0]];
		int curGroupId = firstUnit.GroupId;
		int mobileType = firstUnit.MobileType;
		int sizeOneSelectedCount = selectedUnits.Count;

		for (int i = 0; i < selectedUnits.Count; i++)
		{
			Unit unit = this[selectedUnits[i]];

			if (unit.CurAction == Sprite.SPRITE_ATTACK)
				unit.Stop();

			if (unit.CurAction == Sprite.SPRITE_IDLE)
				unit.SetReady();
		}

		unprocessCount = sizeOneSelectedCount;

		//---- construct array to store size one selected unit ----//
		List<int> selectedSizeOneUnitArray = new List<int>();
		if (sizeOneSelectedCount > 0)
		{
			for (int i = 0; i < selectedUnits.Count && unprocessCount > 0; i++)
			{
				Unit unit = this[selectedUnits[i]];
				if (unit.SpriteInfo.LocWidth == 1)
				{
					selectedSizeOneUnitArray.Add(selectedUnits[i]);
					unprocessCount--;
				}
			}
		}

		unprocessCount = sizeOneSelectedCount;

		//----------- variables initialization ---------------//
		int destX, destY, x, y, moveScale;
		if (mobileType == UnitConstants.UNIT_LAND)
		{
			x = destX = destLocX;
			y = destY = destLocY;
			moveScale = 1;
		}
		else // UnitConstants.UNIT_AIR, UnitConstants.UNIT_SEA
		{
			x = destX = (destLocX / 2) * 2;
			y = destY = (destLocY / 2) * 2;
			moveScale = 2;
		}

		int square_size, not_tested_loc, lower_right_case, upper_left_case;
		int[] distance = new int[sizeOneSelectedCount];
		int[] sorted_distance = new int[sizeOneSelectedCount];
		int[] sorted_member = new int[sizeOneSelectedCount];
		int[] done_flag = new int[sizeOneSelectedCount];
		//----- initialize parameters and construct data structure -----//
		oddCount = 1;
		evenCount = 3;
		square_size = not_tested_loc = lower_right_case = upper_left_case = 0;

		//--- calculate the rectangle size used to allocate space for the sprites----//
		unprocessCount = sizeOneSelectedCount;
		while (unprocessCount > 0)
		{
			//=============================
			// process odd size square
			//=============================
			vecX = (oddCount / 4) * moveScale;
			vecY = vecX;
			k = 0;

			int j;
			for (j = 0; j < oddCount && unprocessCount > 0; j++)
			{
				x = destX + vecX;
				y = destY + vecY;

				if (x >= 0 && y >= 0 && x < GameConstants.MapSize && y < GameConstants.MapSize)
				{
					Location location = World.GetLoc(x, y);
					if (location.IsUnitGroupAccessible(mobileType, curGroupId))
						unprocessCount--;
				}

				if (k++ < oddCount / 2) // reset vecX, vecY
					vecX -= moveScale;
				else
					vecY -= moveScale;
			}

			square_size += moveScale;
			if (j < oddCount)
				not_tested_loc = oddCount - j;
			oddCount += 4;

			if (unprocessCount > 0)
			{
				//=============================
				// process even size square
				//=============================
				vecY = (-(evenCount / 4) - 1) * moveScale;
				vecX = vecY + moveScale;
				k = 0;

				for (j = 0; j < evenCount && unprocessCount > 0; j++)
				{
					x = destX + vecX;
					y = destY + vecY;

					if (x >= 0 && y >= 0 && x < GameConstants.MapSize && y < GameConstants.MapSize)
					{
						Location location = World.GetLoc(x, y);
						if (location.IsUnitGroupAccessible(mobileType, curGroupId))
							unprocessCount--;
					}

					if (k++ < evenCount / 2) // reset vecX, vecY
						vecX += moveScale;
					else
						vecY += moveScale;
				}

				square_size += moveScale;
				if (j < evenCount)
					not_tested_loc = evenCount - j;
				evenCount += 4;
			}
		}

		int rec_height = square_size; // get the height and width of the rectangle
		int rec_width = square_size;
		if (not_tested_loc >= (square_size / moveScale))
			rec_width -= moveScale;

		//--- decide to use upper_left_case or lower_right_case, however, it maybe changed for boundary improvement----//
		x = CalcRectangleLowerRightLocX(destX, moveScale, rec_width);
		y = CalcRectangleLowerRightLocY(destY, moveScale, rec_height);

		for (int i = 0; i < sizeOneSelectedCount; i++)
		{
			Unit unit = this[selectedSizeOneUnitArray[i]];

			if (unit.NextLocY < y) // lower_right_case or upper_left_case
				lower_right_case++;
			else if (unit.NextLocY > y)
				upper_left_case++;
		}

		if (lower_right_case == upper_left_case) // in case both values are equal, check by upper_left_case
		{
			x = CalcRectangleUpperLeftLocX(destX, moveScale, rec_width);
			y = CalcRectangleUpperLeftLocY(destY, moveScale, rec_height);

			lower_right_case = upper_left_case = 0;
			for (int i = 0; i < sizeOneSelectedCount; i++)
			{
				Unit unit = this[selectedSizeOneUnitArray[i]];

				if (unit.NextLocY < y) // lower_right_case or upper_left_case
					lower_right_case++;
				else if (unit.NextLocY > y)
					upper_left_case++;
			}
		}

		//------------ determine x, y and lower_right_case/upper_left_case-----------//
		DeterminePositionToConstructTable(selectedUnits.Count, destX, destY, mobileType,
			ref x, ref y, moveScale, ref rec_width, ref rec_height,
			ref lower_right_case, ref upper_left_case, not_tested_loc, square_size);

		//------------ construct a table to store distance -------//
		// distance and sorted_member should be initialized first
		ConstructSortedArray(selectedSizeOneUnitArray, sizeOneSelectedCount, x, y,
			lower_right_case, upper_left_case, distance, sorted_distance, sorted_member, done_flag);

		//------------ process the movement -----------//
		unprocessCount = sizeOneSelectedCount; //selectedCount;
		k = 0;

		//-******************* auto correct ***********************-//
		int autoCorrectStartX = x;
		int autoCorrectStartY = y;
		//-******************* auto correct ***********************-//

		if (lower_right_case >= upper_left_case)
		{
			while (unprocessCount > 0)
			{
				for (int i = x; i > x - rec_width && unprocessCount > 0; i -= moveScale)
				{
					Location location = World.GetLoc(i, y);
					if (location.IsUnitGroupAccessible(mobileType, curGroupId))
					{
						Unit unit;
						do
						{
							unit = this[selectedSizeOneUnitArray[sorted_member[k++]]];
						} while (unit.SpriteInfo.LocWidth > 1);

						if (sizeOneSelectedCount > 1)
						{
							if (unprocessCount == sizeOneSelectedCount) // the first unit to move
							{
								unit.MoveTo(i, y, 1, SeekPath.SEARCH_MODE_IN_A_GROUP, 0, sizeOneSelectedCount);
								if (unit.MobileType == UnitConstants.UNIT_LAND && unit.NationId != 0)
									unit.SelectSearchSubMode(unit.NextLocX, unit.NextLocY, i, y, unit.NationId);
								unit.MoveTo(i, y, 1, SeekPath.SEARCH_MODE_IN_A_GROUP, 0, sizeOneSelectedCount);
							}
							else
							{
								if (unit.MobileType == UnitConstants.UNIT_LAND && unit.NationId != 0)
									unit.SelectSearchSubMode(unit.NextLocX, unit.NextLocY, i, y, unit.NationId);
								unit.MoveTo(i, y, 1, SeekPath.SEARCH_MODE_IN_A_GROUP, 0, sizeOneSelectedCount);
							}
						}
						else
						{
							unit.MoveTo(i, y, 1);
						}

						unprocessCount--;
					}
				}

				y -= moveScale;
			}
		}
		else // upper_left_case
		{
			while (unprocessCount > 0)
			{
				for (int i = x; i < x + rec_width && unprocessCount > 0; i += moveScale)
				{
					Location location = World.GetLoc(i, y);
					if (location.IsUnitGroupAccessible(mobileType, curGroupId))
					{
						Unit unit;
						do
						{
							unit = this[selectedSizeOneUnitArray[sorted_member[k++]]];
						} while (unit.SpriteInfo.LocWidth > 1);

						if (sizeOneSelectedCount > 1)
						{
							if (unprocessCount == sizeOneSelectedCount) // the first unit to move
							{
								unit.MoveTo(i, y, 1, SeekPath.SEARCH_MODE_IN_A_GROUP, 0, sizeOneSelectedCount);
								if (unit.MobileType == UnitConstants.UNIT_LAND && unit.NationId != 0)
									unit.SelectSearchSubMode(unit.NextLocX, unit.NextLocY, i, y, unit.NationId);
								unit.MoveTo(i, y, 1, SeekPath.SEARCH_MODE_IN_A_GROUP, 0, sizeOneSelectedCount);
							}
							else
							{
								if (unit.MobileType == UnitConstants.UNIT_LAND && unit.NationId != 0)
									unit.SelectSearchSubMode(unit.NextLocX, unit.NextLocY, i, y, unit.NationId);
								unit.MoveTo(i, y, 1, SeekPath.SEARCH_MODE_IN_A_GROUP, 0, sizeOneSelectedCount);
							}
						}
						else
						{
							unit.MoveTo(i, y, 1);
						}

						unprocessCount--;
					}
				}

				y += moveScale;
				//-******************* auto correct ***********************-//
				if (unprocessCount > 0 && y >= GameConstants.MapSize)
				{
					y = autoCorrectStartY;
				}
				//-******************* auto correct ***********************-//
			}
		}
	}

	private int CalcRectangleLowerRightLocX(int refLocX, int moveScale, int recWidth)
	{
		// the rule:	refLocX + (recWidth / (moveScale * 2)) * moveScale
		if (moveScale == 1)
			return refLocX + recWidth / 2;
		else // moveScale == 2
			return refLocX + (recWidth / 4) * 2;
	}

	private int CalcRectangleLowerRightLocY(int refLocY, int moveScale, int recHeight)
	{
		// the rule:	refLocY + ((recHeight - moveScale) / (moveScale * 2)) * moveScale
		if (moveScale == 1)
			return refLocY + (recHeight - 1) / 2;
		else // moveScale == 2
			return refLocY + ((recHeight - 2) / 4) * 2;
	}

	private int CalcRectangleUpperLeftLocX(int refLocX, int moveScale, int recWidth)
	{
		// the rule:	refLocX - ((recWidth - moveScale) / (moveScale * 2)) * moveScale
		if (moveScale == 1)
			return refLocX - (recWidth - 1) / 2;
		else // moveScale == 2
			return refLocX - ((recWidth - 2) / 4) * 2;
	}

	private int CalcRectangleUpperLeftLocY(int refLocY, int moveScale, int recHeight)
	{
		// the rule:	refLocY - (recHeight / (moveScale * 2)) * moveScale
		if (moveScale == 1)
			return refLocY - recHeight / 2;
		else // moveScale == 2
			return refLocY - (recHeight / 4) * 2;
	}

	private void ConstructSortedArray(List<int> selectedUnitArray, int selectedCount, int x, int y,
		int lowerRightCase, int upperLeftCase, int[] distance, int[] sortedDistance,
		int[] sortedMember, int[] doneFlag)
	{
		int min, dist; // for comparison
		int i, j, k = 0;
		const int c = 1000; // c value for the d(x,y) function

		if (lowerRightCase >= upperLeftCase)
		{
			for (i = 0; i < selectedCount; i++)
			{
				Unit unit = this[selectedUnitArray[i]];
				// d(x,y)=x+c*|y|
				distance[i] = GameConstants.MapSize; // to avoid -ve no. in the following line
				// plus/minus x coord difference
				distance[i] += (x - unit.CurLocX + c * Math.Abs(unit.CurLocY - y));
			}
		}
		else // upper_left_case
		{
			for (i = 0; i < selectedCount; i++)
			{
				Unit unit = this[selectedUnitArray[i]];
				// d(x,y)=x+c*|y|
				distance[i] = GameConstants.MapSize; // to avoid -ve no. in the following line
				// plus/minus x coord difference
				distance[i] += (unit.CurLocX - x + c * Math.Abs(unit.CurLocY - y));
			}
		}

		//---------------------------------------------------------------//
		// this part of code using a technique to adjust the distance value
		// such that the selected group can change from lower right form
		// to upper left form or upper left form to lower right form in a better way.
		//---------------------------------------------------------------//

		//------ sorting the distance and store in sortedDistance Array -------//
		for (j = 0; j < selectedCount; j++)
		{
			min = Int32.MaxValue;
			for (i = 0; i < selectedCount; i++)
			{
				if (doneFlag[i] == 0 && (dist = distance[i]) < min)
				{
					min = dist;
					k = i;
				}
			}

			sortedDistance[j] = k;
			doneFlag[k] = 1;
		}

		//----------------- find the minimum value --------------//
		min = distance[sortedDistance[0]];

		int defArraySize = 5; //****** BUGHERE, 5 is chosen arbitrary
		int[] leftQuotZ = new int[defArraySize];
		int[] rightQuotZ = new int[defArraySize];
		int remainder = min % c;
		int index;

		//-- adjust the value to allow changing form between upper left and lower right shape --//

		for (j = 0; j < defArraySize; j++)
			leftQuotZ[j] = rightQuotZ[j] = min - remainder;

		for (j = 0; j < selectedCount; j++)
		{
			if ((dist = distance[sortedDistance[j]] % c) < remainder)
			{
				if ((index = remainder - dist) <= defArraySize) // the case can be handled by this array size
				{
					distance[sortedDistance[j]] = leftQuotZ[index - 1] + dist;
					leftQuotZ[index - 1] += c;
				}
			}
			else
			{
				if (dist >= remainder)
				{
					if ((index = dist - remainder) < defArraySize) // the case can be handled by this array size
					{
						distance[sortedDistance[j]] = rightQuotZ[index] + dist;
						rightQuotZ[index] += c;
					}
				}
			}
		}

		//---------- sorting -------------//
		for (j = 0; j < selectedCount; j++)
		{
			min = Int32.MaxValue;
			for (i = 0; i < selectedCount; i++)
			{
				if ((dist = distance[i]) < min)
				{
					min = dist;
					k = i;
				}
			}

			sortedMember[j] = k;
			distance[k] = Int32.MaxValue;
		}
	}

	private void DeterminePositionToConstructTable(int selectedCount, int destXLoc, int destYLoc, int mobileType,
		ref int x, ref int y, int moveScale, ref int recWidth, ref int recHeight,
		ref int lowerRightCase, ref int upperLeftCase, int notTestedLoc, int square_size)
	{
		//======================================================================//
		// boundary, corner improvement
		//======================================================================//
		int sqrtValue;

		//======================================================================//
		// lower right case
		//======================================================================//
		if (lowerRightCase >= upperLeftCase)
		{
			//--------- calculate x, y location for lower right case ---------//
			x = CalcRectangleLowerRightLocX(destXLoc, moveScale, recWidth);
			y = CalcRectangleLowerRightLocY(destYLoc, moveScale, recHeight);

			if (x < recWidth)
			{
				//================== left edge =================//
				sqrtValue = (int)Math.Sqrt(selectedCount);
				if (sqrtValue * sqrtValue != selectedCount)
					sqrtValue++;
				if (mobileType != UnitConstants.UNIT_LAND)
					sqrtValue = sqrtValue << 1; // change to scale 2
				recWidth = recHeight = sqrtValue;

				//------------- top left corner --------------//
				if (y < recHeight)
				{
					upperLeftCase = lowerRightCase + 1;
					x = y = 0;
				}
				//------------ bottom left corner ---------------//
				else if (y >= GameConstants.MapSize - moveScale)
				{
					if (notTestedLoc >= square_size / moveScale)
						recWidth -= moveScale;

					x = recWidth - moveScale;
					y = GameConstants.MapSize - moveScale;
				}
				//------------- just left edge -------------//
				else
					x = recWidth - moveScale;
			}
			else if (x >= GameConstants.MapSize - moveScale)
			{
				//============== right edge ==============//

				//----------- top right corner -----------//
				if (y < recHeight)
				{
					sqrtValue = (int)Math.Sqrt(selectedCount);
					if (sqrtValue * sqrtValue != selectedCount)
						sqrtValue++;
					if (mobileType != UnitConstants.UNIT_LAND)
						sqrtValue = sqrtValue << 1; // change to scale 2
					recWidth = recHeight = sqrtValue;

					upperLeftCase = lowerRightCase + 1;
					x = GameConstants.MapSize - recWidth;
					y = 0;
				}
				//---------- bottom right corner ------------//
				else if (y >= GameConstants.MapSize - moveScale)
				{
					y = GameConstants.MapSize - moveScale;
					x = GameConstants.MapSize - moveScale;
				}
				//---------- just right edge ---------------//
				else
				{
					int squareSize = square_size / moveScale;
					if (squareSize * (squareSize - 1) >= selectedCount)
						recWidth -= moveScale;
					x = GameConstants.MapSize - moveScale;
				}
			}
			else if (y < recHeight)
			{
				//================= top edge ===============//
				sqrtValue = (int)Math.Sqrt(selectedCount);
				if (sqrtValue * sqrtValue != selectedCount)
					sqrtValue++;
				if (mobileType != UnitConstants.UNIT_LAND)
					sqrtValue = sqrtValue << 1; // change to scale 2
				recWidth = recHeight = sqrtValue;

				upperLeftCase = lowerRightCase + 1;
				//if(mobileType==UnitConstants.UNIT_LAND)
				//	x = destXLoc-((rec_width-1)/2);
				//else
				//	x = destXLoc-(rec_width/4)*2;
				x = CalcRectangleUpperLeftLocX(destXLoc, moveScale, recWidth);
				y = 0;
			}
			else if (y >= GameConstants.MapSize - moveScale)
			{
				//================== bottom edge ====================//
				if (notTestedLoc >= square_size / moveScale)
					recWidth += moveScale;

				//if(mobileType==UnitConstants.UNIT_LAND)
				//	x = destXLoc+(rec_width/2);
				//else
				//	x = destXLoc+(rec_width/4)*2;
				x = CalcRectangleLowerRightLocX(destXLoc, moveScale, recWidth);
				y = GameConstants.MapSize - moveScale;
			}
		}
		//======================================================================//
		// upper left case
		//======================================================================//
		else
		{
			//--------- calculate x, y location for upper left case ---------//
			x = CalcRectangleUpperLeftLocX(destXLoc, moveScale, recWidth);
			y = CalcRectangleUpperLeftLocY(destYLoc, moveScale, recHeight);

			if (x < 0)
			{
				//================= left edge ==================//

				//------------- top left corner --------------//
				if (y < 0)
				{
					sqrtValue = (int)Math.Sqrt(selectedCount);
					if (sqrtValue * sqrtValue != selectedCount)
						sqrtValue++;
					if (mobileType != UnitConstants.UNIT_LAND)
						sqrtValue = sqrtValue << 1; // change to scale 2
					recWidth = recHeight = sqrtValue;
					x = y = 0;
				}
				//------------- bottom left corner --------------//
				else if (y + recHeight >= GameConstants.MapSize - moveScale)
				{
					lowerRightCase = upperLeftCase + 1;
					x = recWidth - moveScale;
					y = GameConstants.MapSize - moveScale;
				}
				//------------- just left edge ------------------//
				else
				{
					sqrtValue = (int)Math.Sqrt(selectedCount);
					if (sqrtValue * sqrtValue != selectedCount)
						sqrtValue++;
					if (mobileType != UnitConstants.UNIT_LAND)
						sqrtValue = sqrtValue << 1; // change to scale 2
					recWidth = recHeight = sqrtValue;
					x = 0;
				}
			}
			//================ right edge ================//
			else if (x + recWidth >= GameConstants.MapSize - moveScale)
			{
				//------------- top right corner ------------------//
				if (y < 0)
				{
					sqrtValue = (int)Math.Sqrt(selectedCount);
					if (sqrtValue * sqrtValue != selectedCount)
						sqrtValue++;
					if (mobileType != UnitConstants.UNIT_LAND)
						sqrtValue = sqrtValue << 1; // change to scale 2
					recWidth = recHeight = sqrtValue;
					x = GameConstants.MapSize - recWidth;
					y = 0;
				}
				//------------- bottom right corner ------------------//
				else if (y + recHeight >= GameConstants.MapSize - moveScale)
				{
					lowerRightCase = upperLeftCase + 1;
					x = GameConstants.MapSize - moveScale;
					y = GameConstants.MapSize - moveScale;
				}
				//------------- just right edge ------------------//
				else
				{
					sqrtValue = (int)Math.Sqrt(selectedCount);
					if (sqrtValue * sqrtValue != selectedCount)
						sqrtValue++;
					if (mobileType != UnitConstants.UNIT_LAND)
						sqrtValue = sqrtValue << 1; // change to scale 2
					recWidth = recHeight = sqrtValue;

					int squareSize = square_size / moveScale;
					if (squareSize * (squareSize - 1) >= selectedCount)
						recWidth -= moveScale;
					lowerRightCase = upperLeftCase + 1;
					x = GameConstants.MapSize - moveScale;
					//if(mobileType==UNIT_LAND)
					//	y = destYLoc+((rec_height-1)/2);
					//else
					//	y = destYLoc+((rec_height-2)/4)*2;
					y = CalcRectangleLowerRightLocY(destYLoc, moveScale, recHeight);
				}
			}
			//================= top edge ================//
			else if (y < 0)
			{
				sqrtValue = (int)Math.Sqrt(selectedCount);
				if (sqrtValue * sqrtValue != selectedCount)
					sqrtValue++;

				recWidth = recHeight = sqrtValue;
				y = 0;
			}
			//================= bottom edge ================//
			else if (y + recHeight >= GameConstants.MapSize - moveScale)
			{
				if (notTestedLoc >= square_size)
					recWidth += moveScale;
				y = GameConstants.MapSize - moveScale;
			}
		}
	}

	private void ShipToBeach(int destX, int destY, bool divided, List<int> selectedUnits, int remoteAction)
    {
	    /*if (!remoteAction && remote.is_enable())
	    {
		    // packet structure : <xLoc> <yLoc> <no. of units> <divided> <unit recno ...>
		    short* shortPtr = (short*)remote.new_send_queue_msg(MSG_UNITS_SHIP_TO_BEACH, sizeof(short) * (4 + selectedCount));
		    shortPtr[0] = destX;
		    shortPtr[1] = destY;
		    shortPtr[2] = selectedCount;
		    shortPtr[3] = divided;
		    memcpy(shortPtr + 4, selectedArray, sizeof(short) * selectedCount);

		    return;
	    }*/

	    SetGroupId(selectedUnits);

	    //--------------------------------------------------------------------//
	    const int CHECK_SEA_DIMENSION = 50;
	    const int CHECK_SEA_SIZE = CHECK_SEA_DIMENSION * CHECK_SEA_DIMENSION;
	    Location loc = World.GetLoc(destX, destY);
	    int regionId = loc.RegionId;
	    int landLocX = -1, landLocY = -1;

	    int i = 0;

	    //--------------------------------------------------------------------//
	    // find a unit that can carrying units.  Let it to do the first searching.
	    // Use the returned reference parameters (landX, landY) for the other
	    // ships to calculate their final location to move to
	    //--------------------------------------------------------------------//
	    for (; i < selectedUnits.Count; i++) // for first unit
	    {
		    Unit unit = this[selectedUnits[i]];
		    if (UnitRes[unit.UnitType].carry_unit_capacity > 0)
		    {
			    unit.ShipToBeach(destX, destY, out landLocX, out landLocY);
			    i++;
			    break;
		    }
		    else
		    {
			    unit.MoveTo(destX, destY, 1);
		    }
	    }

	    int totalCheck = 0;
	    for (; i < selectedUnits.Count; i++) // for the rest units
	    {
		    Unit unit = this[selectedUnits[i]];
		    if (UnitRes[unit.UnitType].carry_unit_capacity > 0 && landLocX != -1 && landLocY != -1)
		    {
			    bool found = false;
			    for (int j = 1; j <= CHECK_SEA_SIZE; j++, totalCheck++)
			    {
				    Misc.cal_move_around_a_point(j, CHECK_SEA_DIMENSION, CHECK_SEA_DIMENSION, out int xShift, out int yShift);

				    if (j >= CHECK_SEA_SIZE)
					    j = 1;

				    if (totalCheck == CHECK_SEA_SIZE)
				    {
					    //--------------------------------------------------------------------//
					    // can't handle this case
					    //--------------------------------------------------------------------//
					    unit.ShipToBeach(landLocX, landLocY, out _, out _);
					    totalCheck = 0;
					    break;
				    }

				    int seaLocX = landLocX + xShift;
				    int seaLocY = landLocY + yShift;
				    if (!Misc.IsLocationValid(seaLocX, seaLocY))
					    continue;

				    loc = World.GetLoc(seaLocX, seaLocY);
				    if (TerrainRes[loc.TerrainId].average_type != TerrainTypeCode.TERRAIN_OCEAN)
					    continue;

				    //--------------------------------------------------------------------//
				    // if it is able to find a location around the surrounding location with
				    // same region id we prefer, order the unit to move there.
				    //--------------------------------------------------------------------//
				    for (int k = 2; k <= 9; k++)
				    {
					    Misc.cal_move_around_a_point(k, 3, 3, out xShift, out yShift);
					    int checkLocX = seaLocX + xShift;
					    int checkLocY = seaLocY + yShift;
					    if (!Misc.IsLocationValid(checkLocX, checkLocY))
						    continue;

					    loc = World.GetLoc(checkLocX, checkLocY);
					    if (loc.RegionId != regionId)
						    continue;

					    unit.ShipToBeach(checkLocX, checkLocY, out _, out _);
					    found = true;
					    break;
				    }

				    if (found)
				    {
					    totalCheck = 0;
					    break;
				    }
			    }
		    }
		    else // cannot carry units
		    {
			    unit.MoveTo(destX, destY, 1);
		    }
	    }
    }

    public void Assign(int destLocX, int destLocY, bool divided, int remoteAction, List<int> selectedUnits)
    {
	    //--- set the destination to the top left position of the town/firm ---//

	    Location location = World.GetLoc(destLocX, destLocY);

	    //---- if there is a firm on this location -----//

	    if (location.IsFirm())
	    {
		    Firm firm = FirmArray[location.FirmId()];
		    destLocX = firm.LocX1;
		    destLocY = firm.LocY1;
	    }

	    //---- if there is a town on this location -----//

	    else if (location.IsTown())
	    {
		    Town town = TownArray[location.TownId()];
		    destLocX = town.LocX1;
		    destLocY = town.LocY1;
	    }

	    //-------------------------------------------//

	    //TODO remove this code
	    if (selectedUnits.Count == 0)
	    {
		    // find myself
		    foreach (Unit unit in this)
		    {
			    if (!unit.IsVisible())
				    continue;

			    if (unit.SelectedFlag && unit.IsOwn())
			    {
				    selectedUnits.Add(unit.SpriteId);
			    }
		    }
	    }

	    /*if (!remoteAction && remote.is_enable())
	    {
		    // packet structure : <xLoc> <yLoc> <no. of units> <unit recno ...>
		    short* shortPtr = (short*)remote.new_send_queue_msg(MSG_UNIT_ASSIGN, sizeof(short) * (4 + selectedCount));
		    shortPtr[0] = destX;
		    shortPtr[1] = destY;
		    shortPtr[2] = selectedCount;
		    shortPtr[3] = divided;
		    memcpy(shortPtr + 4, selectedArray, sizeof(short) * selectedCount);
	    }*/
	    //else
	    //{
		    if (!divided)
		    {
			    for (int i = 0; i < selectedUnits.Count; i++)
			    {
				    int unitId = selectedUnits[i];

				    if (IsDeleted(unitId))
					    continue;

				    Unit unit = this[unitId];
				    unit.Stop2();
			    }

			    DivideUnits(destLocX, destLocY, selectedUnits);

			    if (_selectedLandUnits.Count > 0)
				    Assign(destLocX, destLocY, true, remoteAction, _selectedLandUnits);

			    if (_selectedSeaUnits.Count > 0)
			    {
				    Location loc = World.GetLoc(destLocX, destLocY);
				    if (loc.IsFirm())
				    {
					    Firm firm = FirmArray[loc.FirmId()];
					    if (firm.FirmType == Firm.FIRM_HARBOR) // recursive call
						    Assign(destLocX, destLocY, true, remoteAction, _selectedSeaUnits);
					    else
						    ShipToBeach(destLocX, destLocY, true, _selectedSeaUnits, remoteAction);
				    }
				    else
				    {
					    ShipToBeach(destLocX, destLocY, true, _selectedSeaUnits, remoteAction);
				    }
			    }

			    if (_selectedAirUnits.Count > 0) // no assign for air units
				    MoveTo(destLocX, destLocY, true, _selectedAirUnits, remoteAction);
		    }
		    else
		    {
			    //---------- set unit to assign -----------//
			    if (selectedUnits.Count == 1)
			    {
				    Unit unit = this[selectedUnits[0]];

				    if (unit.SpriteInfo.LocWidth <= 1)
				    {
					    unit.GroupId = CurGroupId++;
					    unit.Assign(destLocX, destLocY);
				    }
				    else // move to object surrounding
				    {
					    Location loc = World.GetLoc(destLocX, destLocY);
					    if (loc.IsFirm())
						    unit.MoveToFirmSurround(destLocX, destLocY, unit.SpriteInfo.LocWidth, unit.SpriteInfo.LocHeight, loc.FirmId());
					    else if (loc.IsTown())
						    unit.MoveToTownSurround(destLocX, destLocY, unit.SpriteInfo.LocWidth, unit.SpriteInfo.LocHeight);
					    else if (loc.HasUnit(UnitConstants.UNIT_LAND))
						    unit.MoveToUnitSurround(destLocX, destLocY, unit.SpriteInfo.LocWidth,
							    unit.SpriteInfo.LocHeight, loc.UnitId(UnitConstants.UNIT_LAND));
				    }
			    }
			    else // for more than one unit selecting, call GroupAssign() to take care of it
			    {
				    SetGroupId(selectedUnits);
				    GroupAssign(destLocX, destLocY, selectedUnits);
			    }
		    }
	    //}
    }

    private void GroupAssign(int destLocX, int destLocY, List<int> selectedUnits)
    {
	    const int ASSIGN_TYPE_UNIT = 1;
	    const int ASSIGN_TYPE_FIRM = 2;
	    const int ASSIGN_TYPE_TOWN = 3;

	    int assignType = 0; // 1 for unit, 2 for firm, 3 for town
	    int miscNo = 0; // used to store the id of the object

	    Location loc = World.GetLoc(destLocX, destLocY);
	    if (loc.HasUnit(UnitConstants.UNIT_LAND))
	    {
		    assignType = ASSIGN_TYPE_UNIT;
		    miscNo = loc.UnitId(UnitConstants.UNIT_LAND);
	    }
	    else if (loc.IsFirm())
	    {
		    assignType = ASSIGN_TYPE_FIRM;
		    miscNo = FirmArray[loc.FirmId()].FirmType;
	    }
	    else if (loc.IsTown())
	    {
		    assignType = ASSIGN_TYPE_TOWN;
		    miscNo = 0;
	    }

	    for (int i = 0; i < selectedUnits.Count; i++)
	    {
		    Unit unit = this[selectedUnits[i]];
		    if (unit.SpriteInfo.LocWidth <= 1)
		    {
			    // the third parameter is used to generate different result for the searching
			    unit.Assign(destLocX, destLocY, i + 1);
		    }
		    else
		    {
			    //-----------------------------------------------------------------//
			    // for 2x2 unit, unable to assign, so move to the surrounding
			    //-----------------------------------------------------------------//
			    switch (assignType)
			    {
				    case ASSIGN_TYPE_UNIT:
					    unit.MoveToUnitSurround(destLocX, destLocY, unit.SpriteInfo.LocWidth, unit.SpriteInfo.LocHeight, miscNo);
					    break;

				    case ASSIGN_TYPE_FIRM:
					    unit.MoveToFirmSurround(destLocX, destLocY, unit.SpriteInfo.LocWidth, unit.SpriteInfo.LocHeight, miscNo);
					    break;

				    case ASSIGN_TYPE_TOWN:
					    unit.MoveToTownSurround(destLocX, destLocY, unit.SpriteInfo.LocWidth, unit.SpriteInfo.LocHeight);
					    break;
			    }
		    }
	    }
    }
    
    public void AssignToCamp(int destLocX, int destLocY, int remoteAction, List<int> selectedUnits)
    {
	    DivideUnits(destLocX, destLocY, selectedUnits);

	    if (_selectedLandUnits.Count > 0)
	    {
		    List<int> unitsToAssign = new List<int>();
		    List<int> unitsToMove = new List<int>();

		    //----------------------------------------------------------------//
		    // Only human and weapon can be assigned to the camp. Others are
		    // ordered to move to camp as close as possible.
		    //----------------------------------------------------------------//
		    for (int i = 0; i < selectedUnits.Count; i++)
		    {
			    Unit unit = this[selectedUnits[i]];
			    int unitClass = UnitRes[unit.UnitType].unit_class;
			    if (unitClass == UnitConstants.UNIT_CLASS_HUMAN || unitClass == UnitConstants.UNIT_CLASS_WEAPON)
				    unitsToAssign.Add(selectedUnits[i]);
			    else
				    unitsToMove.Add(selectedUnits[i]);
		    }

		    if (unitsToAssign.Count > 0)
			    Assign(destLocX, destLocY, true, remoteAction, unitsToAssign);
		    if (unitsToMove.Count > 0)
			    MoveTo(destLocX, destLocY, true, unitsToMove, remoteAction);
	    }

	    if (_selectedSeaUnits.Count > 0)
		    ShipToBeach(destLocX, destLocY, true, _selectedSeaUnits, remoteAction);

	    if (_selectedAirUnits.Count > 0)
		    MoveTo(destLocX, destLocY, true, _selectedAirUnits, remoteAction);
    }

    public void AssignToShip(int shipLocX, int shipLocY, bool divided, List<int> selectedUnits, int remoteAction, int shipId)
    {
	    /*if (!remoteAction && remote.is_enable())
	    {
		    // packet structure : <xLoc> <yLoc> <ship recno> <no. of units> <divided> <unit recno ...>
		    short* shortPtr = (short*)remote.new_send_queue_msg(MSG_UNITS_ASSIGN_TO_SHIP, sizeof(short) * (5 + selectedCount));
		    shortPtr[0] = shipXLoc;
		    shortPtr[1] = shipYLoc;
		    shortPtr[2] = shipRecno;
		    shortPtr[3] = selectedCount;
		    shortPtr[4] = divided;
		    memcpy(shortPtr + 5, selectedArray, sizeof(short) * selectedCount);

		    return;
	    }*/

	    if (!divided)
	    {
		    DivideUnits(shipLocX, shipLocY, selectedUnits);

		    if (_selectedSeaUnits.Count > 0) // Note: the order to call ship unit first
			    MoveTo(shipLocX, shipLocY, true, _selectedSeaUnits, remoteAction);

		    if (_selectedLandUnits.Count > 0)
			    AssignToShip(shipLocX, shipLocY, true, _selectedLandUnits, remoteAction, shipId);

		    if (_selectedAirUnits.Count > 0)
			    MoveTo(shipLocX, shipLocY, true, _selectedAirUnits, remoteAction);
	    }
	    else
	    {
		    // ----- dont not use shipLocX, shipLocY passed, use shipId --------//

		    UnitMarine ship = (UnitMarine)this[shipId];

		    if (ship == null || !ship.IsVisible())
			    return;

		    shipLocX = ship.NextLocX;
		    shipLocY = ship.NextLocY;

		    //------------------------------------------------------------------------------------//
		    // find the closest unit to the ship
		    //------------------------------------------------------------------------------------//
		    int minDist = Int32.MaxValue;
		    int closestUnitId = -1;
		    for (int i = 0; i < selectedUnits.Count; i++)
		    {
			    Unit unit = this[selectedUnits[i]];
			    int distX = Math.Abs(shipLocX - unit.NextLocX);
			    int distY = Math.Abs(shipLocY - unit.NextLocY);
			    int dist = (distX > distY) ? distX : distY;
			    if (dist < minDist)
			    {
				    minDist = dist;
				    closestUnitId = i;
			    }
		    }

		    //------------------------------------------------------------------------------------//
		    // If the selected units are distributed on different territories, select those unit
		    // on the same territory as the closet unit for processing,
		    // there will be no action for the rest units.
		    //------------------------------------------------------------------------------------//
		    int curGroupId = CurGroupId++;
		    Unit closestUnit = this[selectedUnits[closestUnitId]];
		    int closestUnitLocX = closestUnit.NextLocX;
		    int closestUnitLocY = closestUnit.NextLocY;
		    int defaultRegionId = World.GetLoc(closestUnitLocX, closestUnitLocY).RegionId;
		    List<int> newSelectedArray = new List<int>();

		    if (selectedUnits.Count > 1)
		    {
			    for (int i = 0; i < selectedUnits.Count; i++)
			    {
				    Unit unit = this[selectedUnits[i]];
				    if (World.GetLoc(unit.NextLocX, unit.NextLocY).RegionId == defaultRegionId)
				    {
					    newSelectedArray.Add(selectedUnits[i]); // on the same territory
					    unit.GroupId = curGroupId;
				    }

				    if (unit.CurAction == Sprite.SPRITE_IDLE)
					    unit.SetReady();
			    }
		    }
		    else
		    {
			    newSelectedArray.Add(selectedUnits[0]);
			    closestUnit.GroupId = curGroupId;
		    }

		    //-------------- ordering the ship move near the coast ----------------//
		    int curLocX = closestUnit.NextLocX;
		    int curLocY = closestUnit.NextLocY;

		    ship.GroupId = curGroupId;
		    ship.ShipToBeach(curLocX, curLocY, out int landX, out int landY);

		    if (landX != -1 && landY != -1)
		    {
			    //-------------- ordering the units ---------------//
			    const int TRY_SIZE = 5;
			    int countLimit = TRY_SIZE * TRY_SIZE;
			    int regionId = World.GetLoc(landX, landY).RegionId;
			    for (int i = 0, k = 0; i < newSelectedArray.Count; i++)
			    {
				    for (int j = 1; j < countLimit; j++)
				    {
					    if (++k > countLimit)
						    k = 1;

					    Misc.cal_move_around_a_point(k, TRY_SIZE, TRY_SIZE, out int xShift, out int yShift);
					    int checkLocX = landX + xShift;
					    int checkLocY = landY + yShift;
					    if (!Misc.IsLocationValid(checkLocX, checkLocY))
						    continue;

					    Location loc = World.GetLoc(checkLocX, checkLocY);
					    if (loc.RegionId != regionId || !loc.Walkable())
						    continue;

					    Unit unit = this[newSelectedArray[i]];
					    unit.AssignToShip(landX, landY, ship.SpriteId, k);
					    break;
				    }
			    }
		    }
	    }
    }

    public void Settle(int destLocX, int destLocY, bool divided, int remoteAction, List<int> selectedUnits)
    {
	    //TODO remove this code
	    if (selectedUnits.Count == 0)
	    {
		    // find myself
		    foreach (Unit unit in this)
		    {
			    if (!unit.IsVisible())
				    continue;

			    if (unit.SelectedFlag && unit.IsOwn())
			    {
				    selectedUnits.Add(unit.SpriteId);
			    }
		    }
	    }

	    /*if (!remoteAction && remote.is_enable())
	    {
		    // packet structure : <xLoc> <yLoc> <no. of units> <divided> <unit recno ...>
		    short* shortPtr = (short*)remote.new_send_queue_msg(MSG_UNITS_SETTLE, sizeof(short) * (4 + selectedCount));
		    shortPtr[0] = destX;
		    shortPtr[1] = destY;
		    shortPtr[2] = selectedCount;
		    shortPtr[3] = divided;
		    memcpy(shortPtr + 4, selectedArray, sizeof(short) * selectedCount);
	    }*/
	    //else
	    //{
		    if (!divided)
		    {
			    for (int j = 0; j < selectedUnits.Count; j++)
			    {
				    int unitId = selectedUnits[j];

				    if (IsDeleted(unitId))
					    continue;

				    Unit unit = this[unitId];
				    unit.Stop2();
			    }

			    DivideUnits(destLocX, destLocY, selectedUnits);

			    if (_selectedLandUnits.Count > 0)
				    Settle(destLocX, destLocY, true, remoteAction, _selectedLandUnits);

			    if (_selectedSeaUnits.Count > 0)
				    ShipToBeach(destLocX, destLocY, true, _selectedSeaUnits, remoteAction);

			    if (_selectedAirUnits.Count > 0)
				    MoveTo(destLocX, destLocY, true, _selectedAirUnits, remoteAction);
		    }
		    else
		    {
			    //---------- set unit to settle -----------//
			    if (selectedUnits.Count == 1)
			    {
				    Unit unit = this[selectedUnits[0]];
				    unit.GroupId = CurGroupId++;
				    unit.Settle(destLocX, destLocY);
			    }
			    else
			    {
				    SetGroupId(selectedUnits);
				    for (int i = 0; i < selectedUnits.Count; i++)
				    {
					    Unit unit = this[selectedUnits[i]];
					    unit.Settle(destLocX, destLocY, i + 1);
				    }
			    }
		    }
	    //}
    }

    public void Attack(int targetLocX, int targetLocY, bool divided, List<int> selectedUnits, int remoteAction, int targetUnitId)
    {
	    int targetNationId = 0;

	    if (targetUnitId == 0)
	    {
		    // unit determined from the location

		    Location location = World.GetLoc(targetLocX, targetLocY);
		    targetUnitId = location.GetAnyUnit();
	    }
	    else
	    {
		    // location determined by the unit
		    if (!IsDeleted(targetUnitId))
		    {
			    Unit unit = this[targetUnitId];
			    if (unit.IsVisible())
			    {
				    targetLocX = unit.NextLocX;
				    targetLocY = unit.NextLocY;
				    if (unit.UnitType != UnitConstants.UNIT_EXPLOSIVE_CART) // attacking own porcupine is allowed
					    targetNationId = unit.NationId;
			    }
			    else
			    {
				    targetUnitId = 0;
			    }
		    }
		    else
		    {
			    targetUnitId = 0;
		    }
	    }

	    if (targetUnitId == 0)
	    {
		    //--- set the target coordination to the top left position of the town/firm ---//

		    Location location = World.GetLoc(targetLocX, targetLocY);

		    //---- if there is a firm on this location -----//

		    if (location.IsFirm())
		    {
			    Firm firm = FirmArray[location.FirmId()];

			    targetLocX = firm.LocX1;
			    targetLocY = firm.LocY1;
			    targetNationId = firm.NationId;
		    }

		    //---- if there is a town on this location -----//

		    else if (location.IsTown())
		    {
			    Town town = TownArray[location.TownId()];

			    targetLocX = town.LocX1;
			    targetLocY = town.LocY1;
			    targetNationId = town.NationId;
		    }

		    else
			    return;
	    }

	    //--------- AI debug code ---------//

	    //--- AI attacking a nation which its NationRelation::should_attack is 0 ---//

	    Unit attackUnit = this[selectedUnits[0]];

	    if (attackUnit.NationId != 0 && targetNationId != 0)
	    {
		    if (!NationArray[attackUnit.NationId].get_relation(targetNationId).should_attack)
			    return;
	    }

	    //-------- if it's a multiplayer game --------//
	    /*if (!remoteAction && remote.is_enable())
	    {
		    short* shortPtr = (short*)remote.new_send_queue_msg(MSG_UNIT_ATTACK, sizeof(short) * (5 + selectedCount));

		    shortPtr[0] = targetXLoc;
		    shortPtr[1] = targetYLoc;
		    shortPtr[2] = targetUnitRecno;
		    shortPtr[3] = selectedCount;
		    shortPtr[4] = divided;

		    memcpy(shortPtr + 5, selectedUnitArray, sizeof(short) * selectedCount);
	    }*/
	    //else
	    //{
		    if (!divided)
		    {
			    // 1 for excluding the recno in target location
			    DivideUnits(targetLocX, targetLocY, selectedUnits, 1);

			    Location location = World.GetLoc(targetLocX, targetLocY);
			    int targetMobileType = location.HasAnyUnit();

			    if (_selectedLandUnits.Count > 0)
				    AttackCall(targetLocX, targetLocY, UnitConstants.UNIT_LAND, targetMobileType, true,
					    _selectedLandUnits, targetUnitId);

			    if (_selectedSeaUnits.Count > 0)
				    AttackCall(targetLocX, targetLocY, UnitConstants.UNIT_SEA, targetMobileType, true,
					    _selectedSeaUnits, targetUnitId);

			    if (_selectedAirUnits.Count > 0)
				    AttackCall(targetLocX, targetLocY, UnitConstants.UNIT_AIR, targetMobileType, true,
					    _selectedAirUnits, targetUnitId);
		    }
	    //}
    }

    private void AttackCall(int targetXLoc, int targetYLoc, int mobileType, int targetMobileType, bool divided,
	    List<int> selectedUnits, int targetUnitRecno)
    {
	    //------------- attack now -------------//
	    Location loc = World.GetLoc(targetXLoc, targetYLoc);

	    //if(targetMobileType)
	    if (targetUnitRecno != 0 && !IsDeleted(targetUnitRecno))
	    {
		    //---------------- attack unit --------------//
		    //Unit *targetUnit = unit_array[loc.unit_recno(targetMobileType)];
		    Unit targetUnit = this[targetUnitRecno];
		    if (!targetUnit.IsVisible() || targetUnit.HitPoints <= 0)
			    return;

		    // short targetUnitRecno = targetUnit.sprite_recno;
		    AttackUnit(targetXLoc, targetYLoc, targetUnitRecno, selectedUnits);
	    }
	    else if (loc.IsFirm())
	    {
		    Firm firm = FirmArray[loc.FirmId()];
		    if (firm.HitPoints <= 0.0)
			    return;

		    AttackFirm(targetXLoc, targetYLoc, firm.FirmId, selectedUnits);
	    }
	    else if (loc.IsTown())
	    {
		    Town town = TownArray[loc.TownId()];
		    AttackTown(targetXLoc, targetYLoc, town.TownId, selectedUnits);
	    }
	    else if (loc.IsWall())
	    {
		    AttackWall(targetXLoc, targetYLoc, selectedUnits);
	    }
    }

    private void AttackUnit(int targetLocX, int targetLocY, int targetUnitRecno, List<int> selectedUnits)
    {
	    if (selectedUnits.Count == 1)
	    {
		    Unit selectedUnit = this[selectedUnits[0]];
		    selectedUnit.GroupId = CurGroupId++;
		    selectedUnit.AttackUnit(targetLocX, targetLocY, 0, 0, true);
		    return;
	    }

	    //********************** improve later begin **************************//
	    //---------------------------------------------------------------------//
	    // codes for different territory or different mobile_type attacking should
	    // be added in the future.
	    //---------------------------------------------------------------------//
	    Unit firstUnit = this[selectedUnits[0]];
	    Unit targetUnit = this[targetUnitRecno];
	    if ((World.GetLoc(targetLocX, targetLocY).RegionId != World.GetLoc(firstUnit.NextLocX, firstUnit.NextLocY).RegionId) ||
	        (targetUnit.MobileType != firstUnit.MobileType))
	    {
		    SetGroupId(selectedUnits);

		    for (int i = 0; i < selectedUnits.Count; i++)
		    {
			    Unit selectedUnit = this[selectedUnits[i]];
			    selectedUnit.AttackUnit(targetLocX, targetLocY, 0, 0, true);
		    }

		    return;
	    }
	    //*********************** improve later end ***************************//

	    //----------- initialize local parameters ------------//
	    int targetWidth = targetUnit.SpriteInfo.LocWidth;
	    int targetHeight = targetUnit.SpriteInfo.LocHeight;
	    int targetXLoc2 = targetLocX + targetWidth - 1;
	    int targetYLoc2 = targetLocY + targetHeight - 1;
	    int surroundLoc = GetTargetSurroundLoc(targetWidth, targetHeight);
	    int[] xOffsetPtr = UnitAttackHelper.get_target_x_offset(targetWidth, targetHeight, 0);
	    int[] yOffsetPtr = UnitAttackHelper.get_target_y_offset(targetWidth, targetHeight, 0);

	    //---------------------------------------------------------------------//
	    // construct data structure
	    //---------------------------------------------------------------------//
	    int[][] dir_array_ptr = new int[UnitConstants.ATTACK_DIR][];
	    int[] dir_array_count = new int[UnitConstants.ATTACK_DIR];
	    int[] unit_processed_array = new int[selectedUnits.Count];
	    int unit_processed_count = 0;

	    for (int count = 0; count < UnitConstants.ATTACK_DIR; count++)
	    {
		    dir_array_ptr[count] = new int[selectedUnits.Count];
	    }

	    //---------------------------------------------------------------------//
	    // divide the units into each region
	    //---------------------------------------------------------------------//
	    Unit unit = this[selectedUnits[0]];
	    int groupId = CurGroupId++;
	    ArrangeUnitsInGroup(targetLocX, targetLocY, targetXLoc2, targetYLoc2,
		    selectedUnits, groupId, 1, dir_array_ptr, dir_array_count);

	    //---------------------------------------------------------------------//
	    // now the attackers are divided into 8 groups to attack the target
	    //---------------------------------------------------------------------//
	    int xOffset, yOffset; // offset location of the target
	    int dist, xDist, yDist;
	    int destCount;
	    int unitPos = 0; // store the position of the unit with minDist

	    bool[,] unreachable_table = new bool[MAX_UNIT_SURROUND_SIZE, MAX_UNIT_SURROUND_SIZE];

	    //---------------------------------------------------------------------//
	    // analyze the surrounding location of the target
	    //---------------------------------------------------------------------//
	    int analyseResult = AnalyseSurroundLocation(targetLocX, targetLocY, targetWidth, targetHeight,
			    targetUnit.MobileType, unreachable_table);

	    if (analyseResult == 0) // 0 if all surround location is not accessible
	    {
		    //------------------------------------------------------------//
		    // special handling for this case
		    //------------------------------------------------------------//
		    HandleAttackTargetTotallyBlocked(targetLocX, targetLocY, targetUnitRecno, selectedUnits, 1,
			    unit_processed_array, unit_processed_count);

		    return;
	    }

	    //---------------------------------------------------------------------//
	    // let the units move to the rest accessible location
	    //---------------------------------------------------------------------//
	    for (int count = 0; count < UnitConstants.ATTACK_DIR; count++) // for each array/group
	    {
		    //--------- initialize for each group --------//
		    int unprocessed = dir_array_count[count]; // get the number of units in this region
		    if (unprocessed == 0)
			    continue;

		    destCount = surroundLoc - 1;
		    int[] curArray = dir_array_ptr[count]; // get the recno of units in this region

		    xOffsetPtr = UnitAttackHelper.get_target_x_offset(targetWidth, targetHeight, count);
		    yOffsetPtr = UnitAttackHelper.get_target_y_offset(targetWidth, targetHeight, count);
		    int xOffsetPtrIndex = 0;
		    int yOffsetPtrIndex = 0;
		    xOffset = xOffsetPtr[xOffsetPtrIndex];
		    yOffset = yOffsetPtr[yOffsetPtrIndex];

		    //-----------------------------------------------------------------//
		    // determine a suitable location for the attacker to move to
		    //-----------------------------------------------------------------//
		    while (unprocessed > 0)
		    {
			    //-----------------------------------------------------//
			    // find a reachable location, or not searched location
			    //-----------------------------------------------------//
			    if (analyseResult == 0)
			    {
				    HandleAttackTargetTotallyBlocked(targetLocX, targetLocY, targetUnitRecno, selectedUnits, 1,
					    unit_processed_array, unit_processed_count);

				    return;
			    }
			    else
			    {
				    for (int i = 0; i < surroundLoc; i++)
				    {
					    if ((++destCount) >= surroundLoc)
					    {
						    destCount = 0;
						    xOffsetPtr = UnitAttackHelper.get_target_x_offset(targetWidth, targetHeight, count);
						    yOffsetPtr = UnitAttackHelper.get_target_y_offset(targetWidth, targetHeight, count);
						    xOffsetPtrIndex = 0;
						    yOffsetPtrIndex = 0;
						    xOffset = xOffsetPtr[xOffsetPtrIndex];
						    yOffset = yOffsetPtr[yOffsetPtrIndex];
					    }
					    else
					    {
						    xOffsetPtrIndex++;
						    yOffsetPtrIndex++;
						    xOffset = xOffsetPtr[xOffsetPtrIndex];
						    yOffset = yOffsetPtr[yOffsetPtrIndex];
					    }

					    if (!unreachable_table[xOffset + SHIFT_ADJUST, yOffset + SHIFT_ADJUST])
						    break;
				    }
			    }

			    //------------------------------------------------------------//
			    // find the closest attacker
			    //------------------------------------------------------------//
			    // actual target surrounding location to move to
			    int xLoc = targetLocX + xOffset;
			    int yLoc = targetLocY + yOffset;

			    int minDist = Int32.MaxValue;
			    for (int i = 0; i < unprocessed; i++)
			    {
				    unit = this[curArray[i]];
				    xDist = Math.Abs(xLoc - unit.NextLocX);
				    yDist = Math.Abs(yLoc - unit.NextLocY);
				    dist = (xDist >= yDist) ? xDist * 10 + yDist : yDist * 10 + xDist;

				    if (dist < minDist)
				    {
					    minDist = dist;
					    unitPos = i;
				    }
			    }

			    unit = this[curArray[unitPos]];
			    curArray[unitPos] = curArray[--unprocessed]; // move the units in the back to the front

			    //TODO remove
			    //SeekPath.set_status(SeekPath.PATH_WAIT);
			    unit.AttackUnit(targetLocX, targetLocY, xOffset, yOffset, true);

			    //------------------------------------------------------------//
			    // store the unit sprite_recno in the array
			    //------------------------------------------------------------//
			    unit_processed_array[unit_processed_count++] = unit.SpriteId;

			    //------------------------------------------------------------//
			    // set the flag if unreachable
			    //------------------------------------------------------------//
			    if (SeekPath.PathStatus == SeekPath.PATH_IMPOSSIBLE)
			    {
				    unreachable_table[xLoc - targetLocX + SHIFT_ADJUST, yLoc - targetLocY + SHIFT_ADJUST] = true;
				    analyseResult--;

				    //------------------------------------------------------------//
				    // the nearby location should also be unreachable
				    //------------------------------------------------------------//
				    CheckNearbyLocation(targetLocX, targetLocY, xOffset, yOffset, targetWidth, targetHeight,
					    targetUnit.MobileType, unreachable_table, ref analyseResult);
			    }

			    UpdateUnreachableTable(targetLocX, targetLocY, targetWidth, targetHeight, unit.MobileType,
				    ref analyseResult, unreachable_table);
		    }
	    }

	    //---------------------------------------------------------------------//
	    // set the unreachable flag for each units
	    //---------------------------------------------------------------------//
	    //-************** codes here ***************-//
    }

    private void AttackFirm(int targetLocX, int targetLocY, int firmRecno, List<int> selectedUnits)
    {
	    if (selectedUnits.Count == 1)
	    {
		    Unit selectedUnit = this[selectedUnits[0]];
		    selectedUnit.GroupId = CurGroupId++;
		    selectedUnit.AttackFirm(targetLocX, targetLocY);
		    return;
	    }

	    //********************** improve later begin **************************//
	    //---------------------------------------------------------------------//
	    // codes for different territory or different mobile_type attacking should
	    // be added in the future.
	    //---------------------------------------------------------------------//
	    Unit firstUnit = this[selectedUnits[0]];
	    if (World.GetLoc(targetLocX, targetLocY).RegionId != World.GetLoc(firstUnit.NextLocX, firstUnit.NextLocY).RegionId)
	    {
		    SetGroupId(selectedUnits);

		    for (int i = 0; i < selectedUnits.Count; i++)
		    {
			    Unit selectedUnit = this[selectedUnits[i]];
			    selectedUnit.AttackFirm(targetLocX, targetLocY);
		    }

		    return;
	    }
	    //*********************** improve later end ***************************//

	    //----------- initialize local parameters ------------//
	    Firm firm = FirmArray[firmRecno];
	    FirmInfo firmInfo = FirmRes[firm.FirmType];
	    int firmWidth = firmInfo.loc_width;
	    int firmHeight = firmInfo.loc_height;
	    int targetXLoc2 = targetLocX + firmWidth - 1; // the lower right corner of the firm
	    int targetYLoc2 = targetLocY + firmHeight - 1;

	    //---------------------------------------------------------------------//
	    // construct data structure
	    //---------------------------------------------------------------------//
	    int[][] dir_array_ptr = new int[UnitConstants.ATTACK_DIR][];
	    int[] dir_array_count = new int[UnitConstants.ATTACK_DIR];
	    int[] unit_processed_array = new int[selectedUnits.Count];
	    int unit_processed_count = 0;

	    for (int count = 0; count < UnitConstants.ATTACK_DIR; count++)
	    {
		    dir_array_ptr[count] = new int[selectedUnits.Count];
	    }

	    //---------------------------------------------------------------------//
	    // divide the units into each region
	    //---------------------------------------------------------------------//
	    Unit unit = this[selectedUnits[0]];
	    int groupId = CurGroupId++;
	    ArrangeUnitsInGroup(targetLocX, targetLocY, targetXLoc2, targetYLoc2,
		    selectedUnits, groupId, 2, dir_array_ptr, dir_array_count);

	    //---------------------------------------------------------------------//
	    // now the attackers are divided into 8 groups to attack the target
	    //---------------------------------------------------------------------//
	    int xOffset, yOffset; // offset location of the target
	    int unprocessed; // number of units in this group
	    int destCount;
	    int dist, xDist, yDist;
	    int unitPos = 0; // store the position of the unit with minDist
	    int surroundLoc = GetTargetSurroundLoc(firmWidth, firmHeight);

	    bool[,] unreachable_table = new bool[MAX_UNIT_SURROUND_SIZE, MAX_UNIT_SURROUND_SIZE];

	    //---------------------------------------------------------------------//
	    // analyse the surrounding location of the target
	    //---------------------------------------------------------------------//
	    int analyseResult = AnalyseSurroundLocation(targetLocX, targetLocY, firmWidth, firmHeight,
		    unit.MobileType, unreachable_table);

	    if (analyseResult == 0) // 0 if all surround location is not accessible
	    {
		    //------------------------------------------------------------//
		    // special handling for this case
		    //------------------------------------------------------------//
		    HandleAttackTargetTotallyBlocked(targetLocX, targetLocY, firmRecno, selectedUnits, 2,
			    unit_processed_array, unit_processed_count);

		    return;
	    }

	    //---------------------------------------------------------------------//
	    // let the units move to the rest accessible location
	    //---------------------------------------------------------------------//
	    for (int count = 0; count < UnitConstants.ATTACK_DIR; count++) // for each array/group
	    {
		    //--------- initialize for each group --------//
		    unprocessed = dir_array_count[count]; // get the number of units in this region
		    int[] curArray = dir_array_ptr[count]; // get the recno of units in this region
		    destCount = surroundLoc - 1;
		    int[] xOffsetPtr = UnitAttackHelper.get_target_x_offset(firmWidth, firmHeight, count);
		    int[] yOffsetPtr = UnitAttackHelper.get_target_y_offset(firmWidth, firmHeight, count);
		    int xOffsetPtrIndex = 0;
		    int yOffsetPtrIndex = 0;
		    xOffset = xOffsetPtr[xOffsetPtrIndex];
		    yOffset = yOffsetPtr[yOffsetPtrIndex];

		    //-----------------------------------------------------------------//
		    // determine a suitable location for the attacker to move to
		    //-----------------------------------------------------------------//
		    while (unprocessed > 0)
		    {
			    //-----------------------------------------------------//
			    // find a reachable location, or not searched location
			    //-----------------------------------------------------//

			    if (analyseResult == 0)
			    {
				    HandleAttackTargetTotallyBlocked(targetLocX, targetLocY, firmRecno, selectedUnits, 2,
					    unit_processed_array, unit_processed_count);

				    return;
			    }
			    else
			    {
				    for (int i = 0; i < surroundLoc; i++)
				    {
					    if ((++destCount) >= surroundLoc)
					    {
						    destCount = 0;
						    xOffsetPtr = UnitAttackHelper.get_target_x_offset(firmWidth, firmHeight, count);
						    yOffsetPtr = UnitAttackHelper.get_target_y_offset(firmWidth, firmHeight, count);
						    xOffsetPtrIndex = 0;
						    yOffsetPtrIndex = 0;
						    xOffset = xOffsetPtr[xOffsetPtrIndex];
						    yOffset = yOffsetPtr[yOffsetPtrIndex];
					    }
					    else
					    {
						    xOffsetPtrIndex++;
						    yOffsetPtrIndex++;
						    xOffset = xOffsetPtr[xOffsetPtrIndex];
						    yOffset = yOffsetPtr[yOffsetPtrIndex];
					    }

					    if (!unreachable_table[xOffset + SHIFT_ADJUST, yOffset + SHIFT_ADJUST])
						    break;
				    }
			    }

			    //------------------------------------------------------------//
			    // find the closest attacker
			    //------------------------------------------------------------//
			    // actual target surrounding location to move to
			    int xLoc = targetLocX + xOffset;
			    int yLoc = targetLocY + yOffset;

			    int minDist = Int32.MaxValue;
			    for (int i = 0; i < unprocessed; i++)
			    {
				    unit = this[curArray[i]];
				    xDist = Math.Abs(xLoc - unit.NextLocX);
				    yDist = Math.Abs(yLoc - unit.NextLocY);
				    dist = (xDist >= yDist) ? xDist * 10 + yDist : yDist * 10 + xDist;

				    if (dist < minDist)
				    {
					    minDist = dist;
					    unitPos = i;
				    }
			    }

			    unit = this[curArray[unitPos]];
			    curArray[unitPos] = curArray[--unprocessed]; // move the units in the back to the front

			    //TODO remove
			    //SeekPath.set_status(SeekPath.PATH_WAIT);
			    unit.AttackFirm(targetLocX, targetLocY, xOffset, yOffset);

			    //------------------------------------------------------------//
			    // set the flag if unreachable
			    //------------------------------------------------------------//
			    if (SeekPath.PathStatus == SeekPath.PATH_IMPOSSIBLE)
			    {
				    unreachable_table[xOffset + SHIFT_ADJUST, yOffset + SHIFT_ADJUST] = true;
				    analyseResult--;

				    //------------------------------------------------------------//
				    // the nearby location should also be unreachable
				    //------------------------------------------------------------//
				    CheckNearbyLocation(targetLocX, targetLocY, xOffset, yOffset, firmWidth, firmHeight,
					    unit.MobileType, unreachable_table, ref analyseResult);
			    }

			    UpdateUnreachableTable(targetLocX, targetLocY, firmWidth, firmHeight, unit.MobileType,
				    ref analyseResult, unreachable_table);
		    }
	    }

	    //---------------------------------------------------------------------//
	    // set the unreachable flag for each units
	    //---------------------------------------------------------------------//
	    //-************** codes here ***************-//
    }

    private void AttackTown(int targetLocX, int targetLocY, int townRecno, List<int> selectedUnits)
    {
	    if (selectedUnits.Count == 1)
	    {
		    Unit selectedUnit = this[selectedUnits[0]];
		    selectedUnit.GroupId = CurGroupId++;
		    selectedUnit.AttackTown(targetLocX, targetLocY);
		    return;
	    }

	    //********************** improve later begin **************************//
	    //---------------------------------------------------------------------//
	    // codes for different territory or different mobile_type attacking should
	    // be added in the future.
	    //---------------------------------------------------------------------//
	    Unit firstUnit = this[selectedUnits[0]];
	    if (World.GetLoc(targetLocX, targetLocY).RegionId != World.GetLoc(firstUnit.NextLocX, firstUnit.NextLocY).RegionId)
	    {
		    SetGroupId(selectedUnits);

		    for (int i = 0; i < selectedUnits.Count; i++)
		    {
			    Unit selectedUnit = this[selectedUnits[i]];
			    selectedUnit.AttackTown(targetLocX, targetLocY);
		    }

		    return;
	    }
	    //*********************** improve later end ***************************//

	    //----------- initialize local parameters ------------//
	    int targetXLoc2 = targetLocX + InternalConstants.TOWN_WIDTH - 1; // the lower right corner of the firm
	    int targetYLoc2 = targetLocY + InternalConstants.TOWN_HEIGHT - 1;

	    //---------------------------------------------------------------------//
	    // construct data structure
	    //---------------------------------------------------------------------//
	    int[][] dir_array_ptr = new int[UnitConstants.ATTACK_DIR][];
	    int[] dir_array_count = new int[UnitConstants.ATTACK_DIR];
	    int[] unit_processed_array = new int[selectedUnits.Count];
	    int unit_processed_count = 0;

	    for (int count = 0; count < UnitConstants.ATTACK_DIR; count++)
	    {
		    dir_array_ptr[count] = new int[selectedUnits.Count];
	    }

	    //---------------------------------------------------------------------//
	    // divide the units into each region
	    //---------------------------------------------------------------------//
	    Unit unit = this[selectedUnits[0]];
	    int groupId = CurGroupId++;
	    ArrangeUnitsInGroup(targetLocX, targetLocY, targetXLoc2, targetYLoc2,
		    selectedUnits, groupId, 3, dir_array_ptr, dir_array_count);

	    //---------------------------------------------------------------------//
	    // now the attackers are divided into 8 groups to attack the target
	    //---------------------------------------------------------------------//
	    int xOffset, yOffset; // offset location of the target
	    int unprocessed; // number of units in this group
	    int dist, xDist, yDist;
	    int destCount;
	    int unitPos = 0; // store the position of the unit with minDist
	    int surroundLoc = GetTargetSurroundLoc(InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT);

	    bool[,] unreachable_table = new bool[MAX_UNIT_SURROUND_SIZE, MAX_UNIT_SURROUND_SIZE];

	    //---------------------------------------------------------------------//
	    // analyse the surrounding location of the target
	    //---------------------------------------------------------------------//
	    int analyseResult = AnalyseSurroundLocation(targetLocX, targetLocY,
		    InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, unit.MobileType, unreachable_table);

	    if (analyseResult == 0) // 0 if all surround location is not accessible
	    {
		    //------------------------------------------------------------//
		    // special handling for this case
		    //------------------------------------------------------------//
		    HandleAttackTargetTotallyBlocked(targetLocX, targetLocY, townRecno, selectedUnits, 3,
			    unit_processed_array, unit_processed_count);

		    return;
	    }

	    //---------------------------------------------------------------------//
	    // let the units move to the rest accessible location
	    //---------------------------------------------------------------------//
	    for (int count = 0; count < UnitConstants.ATTACK_DIR; count++) // for each array/group
	    {
		    //--------- initialize for each group --------//
		    unprocessed = dir_array_count[count]; // get the number of units in this region
		    int[] curArray = dir_array_ptr[count]; // get the recno of units in this region
		    destCount = surroundLoc - 1;
		    int[] xOffsetPtr = UnitAttackHelper.get_target_x_offset(InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, count);
		    int[] yOffsetPtr = UnitAttackHelper.get_target_y_offset(InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, count);
		    int xOffsetPtrIndex = 0;
		    int yOffsetPtrIndex = 0;
		    xOffset = xOffsetPtr[xOffsetPtrIndex];
		    yOffset = yOffsetPtr[yOffsetPtrIndex];

		    //-----------------------------------------------------------------//
		    // determine a suitable location for the attacker to move to
		    //-----------------------------------------------------------------//
		    while (unprocessed > 0)
		    {
			    //-----------------------------------------------------//
			    // find a reachable location, or not searched location
			    //-----------------------------------------------------//
			    if (analyseResult == 0)
			    {
				    HandleAttackTargetTotallyBlocked(targetLocX, targetLocY, townRecno, selectedUnits, 3,
					    unit_processed_array, unit_processed_count);

				    return;
			    }
			    else
			    {
				    for (int i = 0; i < surroundLoc; i++)
				    {
					    if ((++destCount) >= surroundLoc)
					    {
						    destCount = 0;
						    xOffsetPtr = UnitAttackHelper.get_target_x_offset(InternalConstants.TOWN_WIDTH,
							    InternalConstants.TOWN_HEIGHT, count);
						    yOffsetPtr = UnitAttackHelper.get_target_y_offset(InternalConstants.TOWN_WIDTH,
							    InternalConstants.TOWN_HEIGHT, count);
						    xOffsetPtrIndex = 0;
						    yOffsetPtrIndex = 0;
						    xOffset = xOffsetPtr[xOffsetPtrIndex];
						    yOffset = yOffsetPtr[yOffsetPtrIndex];
					    }
					    else
					    {
						    xOffsetPtrIndex++;
						    yOffsetPtrIndex++;
						    xOffset = xOffsetPtr[xOffsetPtrIndex];
						    yOffset = yOffsetPtr[yOffsetPtrIndex];
					    }

					    if (!unreachable_table[xOffset + SHIFT_ADJUST, yOffset + SHIFT_ADJUST])
						    break;
				    }
			    }

			    //------------------------------------------------------------//
			    // find the closest attacker
			    //------------------------------------------------------------//
			    // actual target surrounding location to move to
			    int xLoc = targetLocX + xOffset;
			    int yLoc = targetLocY + yOffset;

			    int minDist = Int32.MaxValue;
			    for (int i = 0; i < unprocessed; i++)
			    {
				    unit = this[curArray[i]];
				    xDist = Math.Abs(xLoc - unit.NextLocX);
				    yDist = Math.Abs(yLoc - unit.NextLocY);
				    dist = (xDist >= yDist) ? xDist * 10 + yDist : yDist * 10 + xDist;

				    if (dist < minDist)
				    {
					    minDist = dist;
					    unitPos = i;
				    }
			    }

			    unit = this[curArray[unitPos]];
			    curArray[unitPos] = curArray[--unprocessed]; // move the units in the back to the front

			    //TODO remove
			    //SeekPath.set_status(SeekPath.PATH_WAIT);
			    unit.AttackTown(targetLocX, targetLocY, xOffset, yOffset);

			    //------------------------------------------------------------//
			    // set the flag if unreachable
			    //------------------------------------------------------------//
			    if (SeekPath.PathStatus == SeekPath.PATH_IMPOSSIBLE)
			    {
				    unreachable_table[xOffset + SHIFT_ADJUST, yOffset + SHIFT_ADJUST] = true;
				    analyseResult--;

				    //------------------------------------------------------------//
				    // the nearby location should also be unreachable
				    //------------------------------------------------------------//
				    CheckNearbyLocation(targetLocX, targetLocY, xOffset, yOffset, InternalConstants.TOWN_WIDTH,
					    InternalConstants.TOWN_HEIGHT, unit.MobileType, unreachable_table, ref analyseResult);
			    }

			    UpdateUnreachableTable(targetLocX, targetLocY, InternalConstants.TOWN_WIDTH,
				    InternalConstants.TOWN_HEIGHT, unit.MobileType, ref analyseResult, unreachable_table);
		    }
	    }

	    //---------------------------------------------------------------------//
	    // set the unreachable flag for each units
	    //---------------------------------------------------------------------//
	    //-************** codes here ***************-//
    }

    private void AttackWall(int targetLocX, int targetLocY, List<int> selectedUnits)
    {
	    if (selectedUnits.Count == 1)
	    {
		    Unit selectedUnit = this[selectedUnits[0]];
		    selectedUnit.GroupId = CurGroupId++;
		    selectedUnit.AttackWall(targetLocX, targetLocY);
		    return;
	    }

	    //********************** improve later begin **************************//
	    //---------------------------------------------------------------------//
	    // codes for different territory or different mobile_type attacking should
	    // be added in the future.
	    //---------------------------------------------------------------------//
	    Unit firstUnit = this[selectedUnits[0]];
	    if (World.GetLoc(targetLocX, targetLocY).RegionId != World.GetLoc(firstUnit.NextLocX, firstUnit.NextLocY).RegionId)
	    {
		    SetGroupId(selectedUnits);

		    for (int i = 0; i < selectedUnits.Count; i++)
		    {
			    Unit selectedUnit = this[selectedUnits[i]];
			    selectedUnit.AttackWall(targetLocX, targetLocY);
		    }

		    return;
	    }
	    //*********************** improve later end ***************************//

	    //----------- initialize local parameters ------------//

	    //---------------------------------------------------------------------//
	    // construct data structure
	    //---------------------------------------------------------------------//
	    int[][] dir_array_ptr = new int[UnitConstants.ATTACK_DIR][];
	    int[] dir_array_count = new int[UnitConstants.ATTACK_DIR];
	    int[] unit_processed_array = new int[selectedUnits.Count];
	    int unit_processed_count = 0;

	    for (int count = 0; count < UnitConstants.ATTACK_DIR; count++)
	    {
		    dir_array_ptr[count] = new int[selectedUnits.Count];
	    }

	    //---------------------------------------------------------------------//
	    // divide the units into each region
	    //---------------------------------------------------------------------//
	    Unit unit = this[selectedUnits[0]];
	    int groupId = CurGroupId++;
	    ArrangeUnitsInGroup(targetLocX, targetLocY, targetLocX, targetLocY,
		    selectedUnits, groupId, 0, dir_array_ptr, dir_array_count);

	    //---------------------------------------------------------------------//
	    // now the attackers are divided into 8 groups to attack the target
	    //---------------------------------------------------------------------//
	    int xOffset, yOffset; // offset location of the target
	    int unprocessed; // number of units in this group
	    int dist, xDist, yDist;
	    int destCount;
	    int unitPos = 0; // store the position of the unit with minDist
	    int surroundLoc = GetTargetSurroundLoc(1, 1);

	    bool[,] unreachable_table = new bool[MAX_UNIT_SURROUND_SIZE, MAX_UNIT_SURROUND_SIZE];

	    //---------------------------------------------------------------------//
	    // analyse the surrounding location of the target
	    //---------------------------------------------------------------------//
	    int analyseResult = AnalyseSurroundLocation(targetLocX, targetLocY, 1, 1, unit.MobileType, unreachable_table);

	    if (analyseResult == 0) // 0 if all surround location is not accessible
	    {
		    //------------------------------------------------------------//
		    // special handling for this case
		    //------------------------------------------------------------//
		    HandleAttackTargetTotallyBlocked(targetLocX, targetLocY, 0, selectedUnits, 0,
			    unit_processed_array, unit_processed_count);

		    return;
	    }

	    //---------------------------------------------------------------------//
	    // let the units move to the rest accessible location
	    //---------------------------------------------------------------------//
	    for (int count = 0; count < UnitConstants.ATTACK_DIR; count++) // for each array/group
	    {
		    //--------- initialize for each group --------//
		    unprocessed = dir_array_count[count]; // get the number of units in this region
		    int[] curArray = dir_array_ptr[count]; // get the recno of units in this region
		    destCount = surroundLoc - 1;
		    int[] xOffsetPtr = UnitAttackHelper.get_target_x_offset(1, 1, count);
		    int[] yOffsetPtr = UnitAttackHelper.get_target_y_offset(1, 1, count);
		    int xOffsetPtrIndex = 0;
		    int yOffsetPtrIndex = 0;
		    xOffset = xOffsetPtr[xOffsetPtrIndex];
		    yOffset = yOffsetPtr[yOffsetPtrIndex];

		    //-----------------------------------------------------------------//
		    // determine a suitable location for the attacker to move to
		    //-----------------------------------------------------------------//
		    while (unprocessed > 0)
		    {
			    //-----------------------------------------------------//
			    // find a reachable location, or not searched location
			    //-----------------------------------------------------//
			    if (analyseResult == 0)
			    {
				    HandleAttackTargetTotallyBlocked(targetLocX, targetLocY, 0, selectedUnits, 0,
					    unit_processed_array, unit_processed_count);

				    return;
			    }
			    else
			    {
				    for (int i = 0; i < surroundLoc; i++)
				    {
					    if ((++destCount) >= surroundLoc)
					    {
						    destCount = 0;
						    xOffsetPtr = UnitAttackHelper.get_target_x_offset(1, 1, count);
						    yOffsetPtr = UnitAttackHelper.get_target_y_offset(1, 1, count);
						    xOffsetPtrIndex = 0;
						    yOffsetPtrIndex = 0;
						    xOffset = xOffsetPtr[xOffsetPtrIndex];
						    yOffset = yOffsetPtr[yOffsetPtrIndex];
					    }
					    else
					    {
						    xOffsetPtrIndex++;
						    yOffsetPtrIndex++;
						    xOffset = xOffsetPtr[xOffsetPtrIndex];
						    yOffset = yOffsetPtr[yOffsetPtrIndex];
					    }

					    if (!unreachable_table[xOffset + SHIFT_ADJUST, yOffset + SHIFT_ADJUST])
						    break;
				    }
			    }

			    //------------------------------------------------------------//
			    // find the closest attacker
			    //------------------------------------------------------------//
			    // actual target surrounding location to move to
			    int xLoc = targetLocX + xOffset;
			    int yLoc = targetLocY + yOffset;

			    int minDist = Int32.MaxValue;
			    for (int i = 0; i < unprocessed; i++)
			    {
				    unit = this[curArray[i]];
				    xDist = Math.Abs(xLoc - unit.NextLocX);
				    yDist = Math.Abs(yLoc - unit.NextLocY);
				    dist = (xDist >= yDist) ? xDist * 10 + yDist : yDist * 10 + xDist;

				    if (dist < minDist)
				    {
					    minDist = dist;
					    unitPos = i;
				    }
			    }

			    unit = this[curArray[unitPos]];
			    curArray[unitPos] = curArray[--unprocessed]; // move the units in the back to the front

			    //TODO remove
			    //SeekPath.set_status(SeekPath.PATH_WAIT);
			    unit.AttackWall(targetLocX, targetLocY, xOffset, yOffset);

			    //------------------------------------------------------------//
			    // set the flag if unreachable
			    //------------------------------------------------------------//
			    if (SeekPath.PathStatus == SeekPath.PATH_IMPOSSIBLE)
			    {
				    unreachable_table[xOffset + SHIFT_ADJUST, yOffset + SHIFT_ADJUST] = true;
				    analyseResult--;
				    //------------------------------------------------------------//
				    // the nearby location should also be unreachable
				    //------------------------------------------------------------//
				    CheckNearbyLocation(targetLocX, targetLocY, xOffset, yOffset, 1, 1,
					    unit.MobileType, unreachable_table, ref analyseResult);
			    }

			    UpdateUnreachableTable(targetLocX, targetLocY, 1, 1, unit.MobileType,
				    ref analyseResult, unreachable_table);
		    }
	    }

	    //---------------------------------------------------------------------//
	    // set the unreachable flag for each units
	    //---------------------------------------------------------------------//
	    //-************** codes here ***************-//
    }

	private void UpdateUnreachableTable(int targetXLoc, int targetYLoc, int targetWidth, int targetHeight,
		int mobileType, ref int analyseResult, bool[,] unreachable_table)
	{
		int xLoc1, yLoc1, xLoc2, yLoc2, i;

		//----------------------------------------------------------------------//
		// checking for left & right edges, calculate location and counter
		//----------------------------------------------------------------------//
		xLoc1 = targetXLoc - 1;
		xLoc2 = targetXLoc + targetWidth;

		if (targetYLoc == 0)
		{
			i = targetHeight + 1;
			yLoc1 = targetYLoc;
		}
		else
		{
			i = targetHeight + 2;
			yLoc1 = targetYLoc - 1;
		}

		if (targetYLoc + targetHeight >= GameConstants.MapSize)
			i--;

		for (yLoc2 = yLoc1 - targetYLoc + SHIFT_ADJUST; i > 0; i--, yLoc1++, yLoc2++)
		{
			//---- left edge ----//
			if (xLoc1 >= 0 && !unreachable_table[0, yLoc2] && !World.GetLoc(xLoc1, yLoc1).CanMove(mobileType))
			{
				unreachable_table[0, yLoc2] = true;
				analyseResult--;
			}

			//----- right edge -----//
			if (xLoc2 < GameConstants.MapSize && !unreachable_table[targetWidth + 1, yLoc2] &&
			    !World.GetLoc(xLoc2, yLoc1).CanMove(mobileType))
			{
				unreachable_table[targetWidth + 1, yLoc2] = true;
				analyseResult--;
			}

			if (analyseResult == 0)
				return;
		}

		//----------------------------------------------------------------------//
		// checking for the top and bottom edges
		//----------------------------------------------------------------------//
		yLoc1 = targetYLoc - 1;
		yLoc2 = targetYLoc + targetHeight;
		for (i = 0, xLoc1 = targetXLoc, xLoc2 = xLoc1 - targetXLoc + SHIFT_ADJUST; i < targetWidth; i++, xLoc1++, xLoc2++)
		{
			//---- top edge ----//
			if (yLoc1 >= 0 && !unreachable_table[xLoc2, 0] && !World.GetLoc(xLoc1, yLoc1).CanMove(mobileType))
			{
				unreachable_table[xLoc2, 0] = true;
				analyseResult--;
			}

			//----- bottom edge -----//
			if (yLoc2 < GameConstants.MapSize && !unreachable_table[xLoc2, targetHeight + 1] &&
			    !World.GetLoc(xLoc1, yLoc2).CanMove(mobileType))
			{
				unreachable_table[xLoc2, targetHeight + 1] = true;
				analyseResult--;
			}

			if (analyseResult == 0)
				return;
		}
	}

	private int GetTargetSurroundLoc(int targetWidth, int targetHeight)
	{
		int[,] surround_loc = { { 8, 10, 12, 14 }, { 10, 12, 14, 16 }, { 12, 14, 16, 18 }, { 14, 16, 18, 20 } };

		return surround_loc[targetWidth - 1, targetHeight - 1];
	}

	private void ArrangeUnitsInGroup(int xLoc1, int yLoc1, int xLoc2, int yLoc2,
		List<int> selectedUnits, int unitGroupId, int targetType, int[][] dir_array_ptr, int[] dir_array_count)
	{
		for (int i = 0; i < selectedUnits.Count; i++)
		{
			Unit unit = this[selectedUnits[i]];

			unit.GroupId = unitGroupId; // set unit_group_id
			if (unit.CurAction == Sprite.SPRITE_IDLE)
				unit.SetReady();

			int curXLoc = unit.NextLocX;
			int curYLoc = unit.NextLocY;
			if (curXLoc >= xLoc1 - 1 && curXLoc <= xLoc2 + 1 && curYLoc >= yLoc1 - 1 && curYLoc <= yLoc2 + 1)
			{
				//------------- already in the target surrounding ----------------//
				switch (targetType)
				{
					case 0:
						unit.AttackWall(xLoc1, yLoc1);
						break;

					case 1:
						unit.AttackUnit(xLoc1, yLoc1, 0, 0, true);
						break;

					case 2:
						unit.AttackFirm(xLoc1, yLoc1);
						break;

					case 3:
						unit.AttackTown(xLoc1, yLoc1);
						break;
				}

				continue;
			}

			//---- the attacker need to call searching to reach the target ----//
			if (curXLoc < xLoc1)
			{
				if (curYLoc < yLoc1) // 8
					dir_array_ptr[7][dir_array_count[7]++] = selectedUnits[i];
				else if (curYLoc > yLoc2) // 2
					dir_array_ptr[1][dir_array_count[1]++] = selectedUnits[i];
				else // 1
					dir_array_ptr[0][dir_array_count[0]++] = selectedUnits[i];
			}
			else if (curXLoc > xLoc2)
			{
				if (curYLoc < yLoc1) // 6
					dir_array_ptr[5][dir_array_count[5]++] = selectedUnits[i];
				else if (curYLoc > yLoc2) // 4
					dir_array_ptr[3][dir_array_count[3]++] = selectedUnits[i];
				else // 5
					dir_array_ptr[4][dir_array_count[4]++] = selectedUnits[i];
			}
			else // curXLoc==targetXLoc2
			{
				if (curYLoc < yLoc1) // 7
					dir_array_ptr[6][dir_array_count[6]++] = selectedUnits[i];
				else if (curYLoc > yLoc2) // 3
					dir_array_ptr[2][dir_array_count[2]++] = selectedUnits[i];
				else // curXLoc==xLoc2 && curYLoc==yLoc2
				{
					// target is one of the selected unit, error
				}
			}
		}
	}

	private int AnalyseSurroundLocation(int targetXLoc, int targetYLoc, int targetWidth, int targetHeight,
		int mobileType, bool[,] unreachable_table)
	{
		int[] xIncreTable = { 1, 0, -1, 0 };
		int[] yIncreTable = { 0, 1, 0, -1 };

		int xLoc = targetXLoc - 1;
		int yLoc = targetYLoc - 1;
		int targetXLoc2 = targetXLoc + targetWidth - 1;
		int targetYLoc2 = targetYLoc + targetHeight - 1;
		int bound = 2 * (targetWidth + targetHeight) + 4; // (x+2)*(y+2) - xy
		int increCount = 4, xIncre = 0, yIncre = 0, found = 0;

		for (int i = 0; i < bound; i++)
		{
			if (xLoc < 0 || xLoc >= GameConstants.MapSize || yLoc < 0 || yLoc >= GameConstants.MapSize)
				unreachable_table[xLoc - targetXLoc + SHIFT_ADJUST, yLoc - targetYLoc + SHIFT_ADJUST] = true;
			else
			{
				Location location = World.GetLoc(xLoc, yLoc);
				if (!location.CanMove(mobileType))
					unreachable_table[xLoc - targetXLoc + SHIFT_ADJUST, yLoc - targetYLoc + SHIFT_ADJUST] = true;
				else
					found++;
			}

			if ((xLoc == targetXLoc - 1 || xLoc == targetXLoc2 + 1) &&
			    (yLoc == targetYLoc - 1 || yLoc == targetYLoc2 + 1)) // at the corner
			{
				if ((++increCount) >= 4)
					increCount = 0;

				xIncre = xIncreTable[increCount];
				yIncre = yIncreTable[increCount];
			}

			xLoc += xIncre;
			yLoc += yIncre;
		}

		return found;
	}

	private void CheckNearbyLocation(int targetXLoc, int targetYLoc, int xOffset, int yOffset,
		int targetWidth, int targetHeight, int targetMobileType, bool[,] unreachable_table, ref int analyseResult)
	{
		int[] leftXIncreTable = { 1, 0, -1, 0 };
		int[] leftYIncreTable = { 0, 1, 0, -1 };
		int[] rightXIncreTable = { -1, 0, 1, 0 };
		int[] rightYIncreTable = { 0, 1, 0, -1 };

		int targetXLoc2 = targetXLoc + targetWidth - 1;
		int targetYLoc2 = targetYLoc + targetHeight - 1;
		int bound = 2 * (targetWidth + targetHeight) + 4; // (x+2)*(y+2) - xy

		int leftXLoc, leftYLoc, leftContinue = 1;
		int leftXIncre = 0, leftYIncre = 0, leftIncreCount = 0;
		int rightXLoc, rightYLoc, rightContinue = 1;
		int rightXIncre = 0, rightYIncre = 0, rightIncreCount = 1;

		bool haveValidSituation = true;

		//-------------------------------------------------------------------------------------//
		// determine the initial situation
		//-------------------------------------------------------------------------------------//
		if ((xOffset == -1 || xOffset == targetWidth) && (yOffset == -1 || yOffset == targetHeight)) // at the corner
		{
			if (xOffset == -1)
			{
				if (yOffset == -1) // upper left corner
				{
					leftXIncre = 1;
					leftYIncre = 0;
					leftIncreCount = 0;

					rightXIncre = 0;
					rightYIncre = 1;
					rightIncreCount = 1;
				}
				else // lower left corner
				{
					leftXIncre = 0;
					leftYIncre = -1;
					leftIncreCount = 3;

					rightXIncre = 1;
					rightYIncre = 0;
					rightIncreCount = 2;
				}
			}
			else
			{
				if (yOffset == -1) // upper right corner
				{
					leftXIncre = 0;
					leftYIncre = 1;
					leftIncreCount = 1;

					rightXIncre = -1;
					rightYIncre = 0;
					rightIncreCount = 0;
				}
				else // lower right corner
				{
					leftXIncre = -1;
					leftYIncre = 0;
					leftIncreCount = 2;

					rightXIncre = 0;
					rightYIncre = -1;
					rightIncreCount = 3;
				}
			}
		}
		else // at the edge
		{
			if (xOffset == -1) // left edge
			{
				leftXIncre = 0;
				leftYIncre = -1;
				leftIncreCount = 3;

				rightXIncre = 0;
				rightYIncre = 1;
				rightIncreCount = 1;
			}
			else if (xOffset == targetWidth) // right edge
			{
				leftXIncre = 0;
				leftYIncre = 1;
				leftIncreCount = 1;

				rightXIncre = 0;
				rightYIncre = -1;
				rightIncreCount = 3;
			}
			else if (yOffset == -1) // upper edge
			{
				leftXIncre = 1;
				leftYIncre = 0;
				leftIncreCount = 0;

				rightXIncre = -1;
				rightYIncre = 0;
				rightIncreCount = 0;
			}
			else if (yOffset == targetHeight) // lower edge
			{
				leftXIncre = -1;
				leftYIncre = 0;
				leftIncreCount = 2;

				rightXIncre = 1;
				rightYIncre = 0;
				rightIncreCount = 2;
			}
			else
			{
				haveValidSituation = false;
			}
		}

		leftXLoc = rightXLoc = targetXLoc + xOffset;
		leftYLoc = rightYLoc = targetYLoc + yOffset;
		int canReach;
		int outBoundary; // true if out of map boundary

		//-------------------------------------------------------------------------------------//
		// count the reachable location
		//-------------------------------------------------------------------------------------//
		for (int i = 1; i < bound; i++) // exclude the starting location
		{
			//------------------------------------------------------------//
			// process left hand side checking
			//------------------------------------------------------------//
			if (leftContinue == 1)
			{
				canReach = 0;
				outBoundary = 0;

				leftXLoc += leftXIncre;
				leftYLoc += leftYIncre;
				if ((leftXLoc == targetXLoc - 1 || leftXLoc == targetXLoc2 + 1) &&
				    (leftYLoc == targetYLoc - 1 || leftYLoc == targetYLoc2 + 1))
				{
					if ((++leftIncreCount) >= 4)
						leftIncreCount = 0;

					leftXIncre = leftXIncreTable[leftIncreCount];
					leftYIncre = leftYIncreTable[leftIncreCount];
				}

				if (leftXLoc >= 0 && leftXLoc < GameConstants.MapSize && leftYLoc >= 0 && leftYLoc < GameConstants.MapSize)
				{
					// concept incorrect, but it is used to terminate this part of checking
					if (unreachable_table[leftXLoc - targetXLoc + SHIFT_ADJUST, leftYLoc - targetYLoc + SHIFT_ADJUST])
						canReach = 1;
					else
					{
						Location location = World.GetLoc(leftXLoc, leftYLoc);
						if (location.CanMove(targetMobileType))
							canReach = 1;
					}
				}
				else
					outBoundary = 1;

				if (canReach != 0)
					leftContinue = 0;
				else if (outBoundary == 0)
				{
					unreachable_table[leftXLoc - targetXLoc + SHIFT_ADJUST, leftYLoc - targetYLoc + SHIFT_ADJUST] = true;
					analyseResult--;
				}

				i++;
			}

			//------------------------------------------------------------//
			// process right hand side checking
			//------------------------------------------------------------//
			if (rightContinue != 0)
			{
				canReach = 0;
				outBoundary = 0;

				rightXLoc += rightXIncre;
				rightYLoc += rightYIncre;
				if ((rightXLoc == targetXLoc - 1 || rightXLoc == targetXLoc2 + 1) &&
				    (rightYLoc == targetYLoc - 1 || rightYLoc == targetYLoc2 + 1))
				{
					if ((++rightIncreCount) >= 4)
						rightIncreCount = 0;

					rightXIncre = rightXIncreTable[rightIncreCount];
					rightYIncre = rightYIncreTable[rightIncreCount];
				}

				if (rightXLoc >= 0 && rightXLoc < GameConstants.MapSize && rightYLoc >= 0 && rightYLoc < GameConstants.MapSize)
				{
					if (unreachable_table[rightXLoc - targetXLoc + SHIFT_ADJUST, rightYLoc - targetYLoc + SHIFT_ADJUST])
						canReach = 1; // concept incorrect, but it is used to terminate this part of checking
					else
					{
						Location location = World.GetLoc(rightXLoc, rightYLoc);
						if (location.CanMove(targetMobileType))
							canReach = 1;
					}
				}
				else
					outBoundary = 1;

				if (canReach != 0)
					rightContinue = 0;
				else if (outBoundary == 0)
				{
					unreachable_table[rightXLoc - targetXLoc + SHIFT_ADJUST, rightYLoc - targetYLoc + SHIFT_ADJUST] = true;
					analyseResult--;
				}
			}

			if (leftContinue == 0 && rightContinue == 0)
				break;
		}
	}

	private void HandleAttackTargetTotallyBlocked(int targetXLoc, int targetYLoc, int targetRecno,
		List<int> selectedUnits, int targetType, int[] unit_processed_array, int unit_processed_count)
	{
		if (unit_processed_count > 0) // some units can reach the target surrounding
		{
			int proCount = unit_processed_count - 1;
			int unproCount = selectedUnits.Count - proCount - 1; // number of unprocessed
			int sCount = selectedUnits.Count - 1;
			int found, i, recno;

			//------------------------------------------------------------------------------------//
			// use the result of those processed units as a reference of those unprocessed units
			//------------------------------------------------------------------------------------//
			while (unproCount > 0)
			{
				Unit processed = this[unit_processed_array[proCount]];
				do
				{
					found = 0;
					recno = selectedUnits[sCount];
					for (i = 0; i < unit_processed_count; i++)
					{
						if (unit_processed_array[i] == recno)
						{
							found++;
							break;
						}
					}

					sCount--;
				} while (found > 0);

				Unit unit = this[recno];
				unit.MoveTo(processed.MoveToLocX, processed.MoveToLocY);

				switch (targetType)
				{
					case 0: // wall
						unit.ActionMode = unit.ActionMode2 = UnitConstants.ACTION_ATTACK_WALL;
						break;

					case 1: // unit
						unit.ActionMode = unit.ActionMode2 = UnitConstants.ACTION_ATTACK_UNIT;
						break;

					case 2: // firm
						unit.ActionMode = unit.ActionMode2 = UnitConstants.ACTION_ATTACK_FIRM;
						break;

					case 3: // town
						unit.ActionMode = unit.ActionMode2 = UnitConstants.ACTION_ATTACK_TOWN;
						break;
				}

				unit.ActionParam = unit.ActionPara2 = targetRecno;
				unit.ActionLocX = unit.ActionLocX2 = targetXLoc;
				unit.ActionLocY = unit.ActionLocY2 = targetYLoc;

				proCount--;
				if (proCount < 0)
					proCount = unit_processed_count - 1;

				unproCount--;
			}
		}
		else // none of the units reaches the target surrounding
		{
			//----------------------------------------------------------------//
			// handle the case for 1x1 units now, no 2x2 units
			//----------------------------------------------------------------//
			//-*********** improve later ************-//
			int unprocessed = selectedUnits.Count;
			Unit first = this[selectedUnits[unprocessed - 1]];

			switch (targetType)
			{
				case 0:
					first.AttackWall(targetXLoc, targetYLoc);
					break;

				case 1:
					first.AttackUnit(targetXLoc, targetYLoc, 0, 0, true);
					break;

				case 2:
					first.AttackFirm(targetXLoc, targetYLoc);
					break;

				case 3:
					first.AttackTown(targetXLoc, targetYLoc);
					break;
			}

			int moveToXLoc = first.MoveToLocX;
			int moveToYLoc = first.MoveToLocY;

			while (unprocessed > 0)
			{
				Unit unit = this[selectedUnits[unprocessed - 1]];
				unit.MoveTo(moveToXLoc, moveToYLoc);

				switch (targetType)
				{
					case 0: // wall
						unit.ActionMode = unit.ActionMode2 = UnitConstants.ACTION_ATTACK_WALL;
						unit.ActionParam = unit.ActionPara2 = 0;
						break;

					case 1: // unit
						unit.ActionMode = unit.ActionMode2 = UnitConstants.ACTION_ATTACK_UNIT;
						unit.ActionParam = unit.ActionPara2 = targetRecno;
						break;

					case 2: // firm
						unit.ActionMode = unit.ActionMode2 = UnitConstants.ACTION_ATTACK_FIRM;
						unit.ActionParam = unit.ActionPara2 = targetRecno;
						break;

					case 3: // town
						unit.ActionMode = unit.ActionMode2 = UnitConstants.ACTION_ATTACK_TOWN;
						unit.ActionParam = unit.ActionPara2 = targetRecno;
						break;
				}

				unit.ActionLocX = unit.ActionLocX2 = targetXLoc;
				unit.ActionLocY = unit.ActionLocY2 = targetYLoc;

				unprocessed--;
			}
		}
	}
	
	public void AddWayPoint(int pointX, int pointY, List<int> selectedUnits, int remoteAction)
	{
		/*if (!remoteAction && remote.is_enable())
		{
			// packet structure : <xLoc> <yLoc> <no. of units> <unit recno ...>
			short* shortPtr = (short*)remote.new_send_queue_msg(MSG_UNIT_ADD_WAY_POINT, sizeof(short) * (3 + selectedCount));
			shortPtr[0] = pointX;
			shortPtr[1] = pointY;
			shortPtr[2] = selectedCount;
			memcpy(shortPtr + 3, selectedArray, sizeof(short) * selectedCount);
		}*/
		//else
		//{
		int groupId = CurGroupId;

		for (int i = 0; i < selectedUnits.Count; i++)
		{
			Unit unit = this[selectedUnits[i]];
			unit.GroupId = groupId;
		}

		CurGroupId++;

		for (int i = 0; i < selectedUnits.Count; i++)
		{
			Unit unit = this[selectedUnits[i]];
			unit.AddWayPoint(pointX, pointY);
		}
		//}
	}

	public int GetNextUnit(int currentUnitId, int seekDir, bool sameNation)
	{
		Unit selectedUnit = this[currentUnitId];
		var enumerator = (seekDir >= 0) ? EnumerateAll(currentUnitId, true) : EnumerateAll(currentUnitId, false);

		foreach (int unitId in enumerator)
		{
			Unit unit = this[unitId];

			if (!unit.IsVisible())
				continue;

			if (sameNation && unit.NationId != selectedUnit.NationId)
				continue;
			
			if (UnitRes[unit.UnitType].unit_class != UnitRes[selectedUnit.UnitType].unit_class)
				continue;

			if (!World.GetLoc(unit.NextLocX, unit.NextLocY).IsExplored())
				continue;

			Power.reset_selection();
			return unitId;
		}

		return currentUnitId;
	}
}