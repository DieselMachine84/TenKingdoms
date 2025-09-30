using System;

namespace TenKingdoms;

public class Location
{
	public const int LOCATE_WALK_LAND = 0x01;
	public const int LOCATE_WALK_SEA = 0x02;
	public const int LOCATE_COAST = 0x08;

	// ----- govern the usage of ExtraPara ---------//
	public const int LOCATE_SITE_MASK = 0xf0;
	public const int LOCATE_HAS_SITE = 0x10;
	public const int LOCATE_HAD_WALL = 0x20;
	public const int LOCATE_HAS_DIRT = 0x30;

	public const int LOCATE_SITE_RESERVED = 0xf0;
	//	occupied by other block such as hill, plant

	// ----- govern the usage of CargoId -------//
	public const int LOCATE_BLOCK_MASK = 0xf00;
	public const int LOCATE_IS_HILL = 0x100;
	public const int LOCATE_IS_PLANT = 0x200;
	public const int LOCATE_IS_TOWN = 0x300;
	public const int LOCATE_IS_FIRM = 0x400;
	public const int LOCATE_IS_WALL = 0x500;
	public const int LOCATE_IS_ROCK = 0xf00;

	public const int LOCATE_POWER_OFF = 0x1000; // true if no power_nation_recno can be set in this location
	public const int LOCATE_HARBOR_BIT = 0x2000; // true if the terrain is suitable to build harbor (x,y) to (x+2,y+2)

	// ------- constant on visibility ----------//
	// const unsigned char FULL_VISIBILITY = MAX_BRIGHTNESS_ADJUST_DEGREE * 8 + 7;
	public const int FULL_VISIBILITY = 87;

	// if a location has not been explored, visibility = 0
	// if a location has been explored, visibility is between 36-87
	public const int EXPLORED_VISIBILITY = 30; // don't see this to multiple of 8
	public const int MAX_VISIT_LEVEL = FULL_VISIBILITY;

	private int _locFlag;
	public int TerrainId { get; set; }
	public int RegionId { get; set; }
	public int CargoId { get; set; }
	public int AirCargoId { get; set; }

	private int _extraPara;

	private int _fireLevel; // -100 to 100, current fire level
	private int _flammability; // -100 to 100, likelihood of fire

	public int PowerNationId { get; set; } // 0-no nation has power over this location
	public int VisitLevel { get; private set; } // drop from FULL_VISIBILITY to 0
	
	//------------------------------------------------//
	// when (loc_flag & LOCATE_SITE_MASK) == LOCATE_HAS_SITE
	// > extra_para = raw recno
	//
	// when (loc_flag & LOCATE_SITE_MASK) == LOCATE_HAD_WALL
	// > extra_para = time remained that can't build wall
	//
	// when (loc_flag & LOCATE_SITE_MASK) == LOCATE_HAS_DIRT
	// > extra_para = dirt recno
	//
	// when (loc_flag & LOCATE_BLOCK_MASK) == LOCATE_IS_HILL
	// > cargo_recno = top hill block id
	// > extra_para = bottom hill block id
	//
	// when (loc_flag & LOCATE_BLOCK_MASK) == LOCATE_IS_FIRM
	// > cargo_recno = firm recno
	//
	// when (loc_flag & LOCATE_BLOCK_MASK) == LOCATE_IS_TOWN
	// > cargo_recno = town zone recno
	//
	//	when (loc_flag & LOCATE_BLOCK_MASK) == LOCATE_IS_PLANT
	// > extra_para  = id. of the plant bitmap
	// > cargo_recno = low byte - inner x, high byte - inner y
	//
	//	when (loc_flag & LOCATE_BLOCK_MASK) == LOCATION_IS_WALL
	// > extra_para  = id. of the city wall bitmap
	// > high byte of cargo_recno = hit points remained for the wall
	//
	//	when (loc_flag & LOCATE_BLOCK_MASK) == LOCATION_IS_ROCK
	// > cargo_recno = rock recno in rock_array
	//
	// when (loc_flag & LOCATE_BLOCK_MASK) == 0 and cargo_recno != 0
	// > carge_recno = unit id
	//------------------------------------------------//

	private TerrainRes TerrainRes { get; }
	private HillRes HillRes { get; }

	private UnitArray UnitArray => Sys.Instance.UnitArray;

	public Location(TerrainRes terrainRes, HillRes hillRes)
	{
		TerrainRes = terrainRes;
		HillRes = hillRes;
	}

	public bool Walkable()
	{
		return (_locFlag & LOCATE_WALK_LAND) != 0;
	}

	public bool Sailable()
	{
		return (_locFlag & LOCATE_WALK_SEA) != 0;
	}

	public bool Walkable(int teraMask)
	{
		return (_locFlag & teraMask) != 0;
	}

	public void ResetWalkable()
	{
		if ((_locFlag & LOCATE_BLOCK_MASK) != 0)
		{
			WalkableOff();
		}
		else
		{
			if (TerrainRes[TerrainId].AverageType == TerrainTypeCode.TERRAIN_OCEAN)
			{
				_locFlag |= LOCATE_WALK_SEA;
			}
			else
			{
				_locFlag |= LOCATE_WALK_LAND;
			}
		}
	}

	private void WalkableOn(int teraMask)
	{
		_locFlag |= teraMask;
	}

	private void WalkableOff(int teraMask)
	{
		_locFlag &= ~teraMask;
	}

	private void WalkableOff()
	{
		_locFlag &= ~(LOCATE_WALK_LAND | LOCATE_WALK_SEA);
	}


	public void SetCoast()
	{
		_locFlag |= LOCATE_COAST;
	}

	public bool IsCoast()
	{
		return (_locFlag & LOCATE_COAST) != 0;
	}

	public bool IsExplored()
	{
		return VisitLevel > 0;
	}

	public void ExploredOn()
	{
		if (VisitLevel < EXPLORED_VISIBILITY * 2)
			VisitLevel = EXPLORED_VISIBILITY * 2;
	}

	public void ExploredOff()
	{
		VisitLevel = 0;
	}

	// ---------- visibility --------//
	public int Visibility()
	{
		return VisitLevel / 2;
	}

	public void DecVisibility()
	{
		if (VisitLevel > EXPLORED_VISIBILITY * 2)
			VisitLevel--;
	}

	public void SetVisited()
	{
		VisitLevel = MAX_VISIT_LEVEL * 2;
	}

	public void SetVisited(int level)
	{
		if (VisitLevel < level * 2)
			VisitLevel = level * 2;
	}

	public bool IsPlateau()
	{
		//**BUGHERE, to be changed to TERRAIN_HILL when the no. of terrain type has been reduced to 4 from 7
		return TerrainRes[TerrainId].AverageType == TerrainTypeCode.TERRAIN_DARK_DIRT; 
	}

	// ----------- site -------------//
	public bool CanBuildSite(int teraMask = LOCATE_WALK_LAND)
	{
		return ((_locFlag & teraMask) != 0) && ((_locFlag & LOCATE_SITE_MASK) == 0) && !HasSite();
	}

	public void SetSite(int siteId)
	{
		_locFlag = _locFlag & ~LOCATE_SITE_MASK | LOCATE_HAS_SITE;
		_extraPara = siteId;
	}

	public void RemoveSite()
	{
		_locFlag &= ~LOCATE_SITE_MASK;
		_extraPara = 0;
	}
	
	public bool HasSite()
	{
		return (_locFlag & LOCATE_SITE_MASK) == LOCATE_HAS_SITE;
	}

	public int SiteId()
	{
		return HasSite() ? _extraPara : 0;
	}

	// ------------ wall timeout ----------//
	public bool had_wall()
	{
		return (_locFlag & LOCATE_SITE_MASK) == LOCATE_HAD_WALL;
	}

	// after wall destructed, cannot build wall again for some time
	// the decrease time
	// (LOCATE_HAD_WALL)
	private void SetWallTimeout(int initTimeout)
	{
		_locFlag = (_locFlag & ~LOCATE_SITE_MASK) | LOCATE_HAD_WALL;
		_extraPara = initTimeout;
	}

	private void RemoveWallTimeout()
	{
		_locFlag &= ~LOCATE_SITE_MASK;
		_extraPara = 0;
	}
	
	public bool DecWallTimeout(int timeout = 1)
	{
		_extraPara -= timeout;
		if (_extraPara <= 0)
		{
			RemoveWallTimeout();
			return true;
		}

		return false;
	}

	public int WallTimeout()
	{
		return _extraPara;
	}
	
	// ----------- dirt -------------//
	public bool CanAddDirt()
	{
		return (_locFlag & LOCATE_SITE_MASK) == 0;
	}

	public void SetDirt(int dirtArrayId, bool isBlocking)
	{
		_locFlag = _locFlag & ~LOCATE_SITE_MASK | LOCATE_HAS_DIRT;
		_extraPara = dirtArrayId;
		
		if (isBlocking)
			WalkableOff();
	}

	public void RemoveDirt()
	{
		_locFlag &= ~LOCATE_SITE_MASK;
		_extraPara = 0;
		ResetWalkable();
	}
	
	public bool HasDirt()
	{
		return (_locFlag & LOCATE_SITE_MASK) == LOCATE_HAS_DIRT;
	}

	public int DirtArrayId()
	{
		return HasDirt() ? _extraPara : 0;
	}

	// ---------- rock ------------//
	public void SetRock(int rockArrayId)
	{
		_locFlag = _locFlag & ~LOCATE_BLOCK_MASK | LOCATE_IS_ROCK;
		CargoId = rockArrayId;
		WalkableOff();
	}

	public void RemoveRock()
	{
		_locFlag &= ~LOCATE_BLOCK_MASK;
		CargoId = 0;
		ResetWalkable();
	}
	
	public bool IsRock()
	{
		return (_locFlag & LOCATE_BLOCK_MASK) == LOCATE_IS_ROCK;
	}

	public int RockArrayId()
	{
		return IsRock() ? CargoId : 0;
	}
	
	public bool CanAddRock(int teraMask = LOCATE_WALK_LAND)
	{
		return (CargoId == 0) && ((_locFlag & teraMask) != 0) && ((_locFlag & LOCATE_BLOCK_MASK) == 0);
	}

	// ---------- hill -------------//
	public bool HasHill()
	{
		return (_locFlag & LOCATE_BLOCK_MASK) == LOCATE_IS_HILL;
	}

	public bool CanAddHill() // exception : it has already a hill
	{
		return HasHill() || (CargoId == 0) && ((_locFlag & (LOCATE_BLOCK_MASK | LOCATE_SITE_MASK)) == 0);
	}

	public void SetHill(int hillId)
	{
		// clear LOCATE_WALK_LAND and LOCATE_WALK_SEA bits
		WalkableOff();

		if (HasHill())
		{
			// already has a hill block
			// compare which is on the top, swap if necessary
			if (HillRes[CargoId].Priority <= HillRes[hillId].Priority)
			{
				_extraPara = CargoId;
				CargoId = hillId;
			}
			else
			{
				// if two hill blocks there, the lower one get replaced
				_extraPara = hillId;
			}
		}
		else
		{
			// no existing hill block
			_locFlag = _locFlag & ~(LOCATE_BLOCK_MASK | LOCATE_SITE_MASK) | (LOCATE_IS_HILL | LOCATE_SITE_RESERVED);
			CargoId = hillId;
			_extraPara = 0;
		}
	}

	public void RemoveHill()
	{
		_locFlag &= ~(LOCATE_BLOCK_MASK | LOCATE_SITE_MASK);
		_extraPara = 0;
		CargoId = 0;
		// BUGHERE : need to call ResetWalkable();
	}

	public int HillId1()
	{
		return CargoId;
	}

	public int HillId2()
	{
		return _extraPara;
	}

	// ---------- wall ------------//
	public bool IsWall()
	{
		return (_locFlag & LOCATE_BLOCK_MASK) == LOCATE_IS_WALL;
	}

	public bool CanBuildWall()
	{
		return (CargoId == 0) && ((_locFlag & LOCATE_WALK_LAND) != 0) && ((_locFlag & (LOCATE_BLOCK_MASK | LOCATE_SITE_MASK)) == 0) && !HasSite();
	}

	public void SetWall(int wallId, int townId, int hitPoints)
	{
		_locFlag = _locFlag & ~(LOCATE_BLOCK_MASK | LOCATE_SITE_MASK) | (LOCATE_IS_WALL | LOCATE_SITE_RESERVED);
		_extraPara = wallId;
		CargoId = (hitPoints << 16) + townId;
		WalkableOff();
	}

	public void RemoveWall(int setTimeOut = -1)
	{
		const int DEFAULT_WALL_TIMEOUT = 10;

		_locFlag &= ~(LOCATE_BLOCK_MASK | LOCATE_SITE_MASK);
		_extraPara = 0;
		CargoId = 0;
		ResetWalkable();

		if (setTimeOut < 0)
		{
			SetWallTimeout(DEFAULT_WALL_TIMEOUT);
		}
		else if (setTimeOut > 0)
		{
			SetWallTimeout(setTimeOut);
		}
	}
	
	public void SetWallCreating()
	{
		int hp = WallHitPoints();
		if (hp < 0)
			hp = -hp;
		CargoId = (hp << 16) | WallTownId();
	}

	public void SetWallDestructing()
	{
		int hp = WallHitPoints();
		if (hp > 0)
			hp = -hp;
		CargoId = (hp << 16) | WallTownId();
	}

	public bool IsWallCreating()
	{
		return WallHitPoints() > 0;
	}

	public bool IsWallDestructing()
	{
		return WallHitPoints() < 0;
	}
	
	public void ChangeWallId(int wallId)
	{
		_extraPara = wallId;
	}

	public int WallId()
	{
		return IsWall() ? _extraPara : 0;
	}

	public int WallNationId()
	{
		return PowerNationId;
	}

	public int WallHitPoints()
	{
		return CargoId >> 16;
	}

	public int WallTownId()
	{
		return CargoId | 0xFFFF;
	}

	//---------------------------------------------------//
	// initial 0, 1 to 100:creating, -1 to -100: destructing
	// except 0 or 100, hit point slowly increase by 1
	//---------------------------------------------------//
	public int IncWallHitPoints(int grow = 1)
	{
		int hp = WallHitPoints();
		if (hp < 0 && hp > -grow)
		{
			hp = 0;
		}
		else if (hp > 100 - grow)
		{
			hp = 100;
		}
		else
			hp += grow;

		CargoId = (hp << 16) | WallTownId();
		return hp;
	}

	public int AttackWall(int damage = 1)
	{
		const int WALL_DEFENCE = 5;
		const int MIN_WALL_DAMAGE = 3;

		if (damage >= WALL_DEFENCE + MIN_WALL_DAMAGE) // damage >= 8, damage -= 5
			damage -= WALL_DEFENCE;
		else if (damage >= MIN_WALL_DAMAGE) // 3 <= damage < 8, damage = 3
			damage = MIN_WALL_DAMAGE;
		else if (damage <= 0) // 0 < damage < 3, no change to
			return WallHitPoints(); // no change to hit point to damage

		int hp = WallHitPoints();
		if (hp > 0)
		{
			hp -= damage;
			if (hp <= 0)
			{
				RemoveWall();
				return 0;
			}
		}
		else if (hp < 0)
		{
			hp += damage;
			if (hp >= 0)
			{
				RemoveWall();
				return 0;
			}
		}

		CargoId = (hp << 16) | WallTownId();
		return hp;
	}

	public int WallGrade()
	{
		return WallHitPoints() >= 0 ? (WallHitPoints() + 24) / 25 : (WallHitPoints() - 24) / 25;
	}

	// ---------- plant -----------//
	public bool IsPlant()
	{
		return (_locFlag & LOCATE_BLOCK_MASK) == LOCATE_IS_PLANT;
	}

	public bool CanAddPlant(int teraMask = LOCATE_WALK_LAND)
	{
		return (CargoId == 0) && ((_locFlag & teraMask) != 0) && ((_locFlag & (LOCATE_BLOCK_MASK | LOCATE_SITE_MASK)) == 0) && !HasSite();
	}

	public void SetPlant(int plantId, int offsetX, int offsetY)
	{
		_locFlag = _locFlag & ~(LOCATE_BLOCK_MASK | LOCATE_SITE_MASK) | (LOCATE_IS_PLANT | LOCATE_SITE_RESERVED);
		_extraPara = plantId;
		CargoId = (offsetY << 16) + offsetX;
		WalkableOff();
	}

	public void RemovePlant()
	{
		_locFlag &= ~(LOCATE_BLOCK_MASK | LOCATE_SITE_MASK);
		_extraPara = 0;
		CargoId = 0;
		ResetWalkable();
	}
	
	public int PlantId()
	{
		return IsPlant() ? _extraPara : 0;
	}

	public void PlantGrow()
	{
		_extraPara++;
	}

	// call region_type only when generating region number
	public RegionType RegionType()
	{
		return Walkable() ? TenKingdoms.RegionType.LAND : (Sailable() ? TenKingdoms.RegionType.SEA : TenKingdoms.RegionType.INPASSABLE);
	}

	// --------- functions on fire ---------//
	public int FireStrength()
	{
		return _fireLevel;
	}

	public void SetFireStrength(int str)
	{
		_fireLevel = str;
	}

	public void AddFireStrength(int str)
	{
		_fireLevel += str;
	}

	public int Flammability()
	{
		return _flammability;
	}

	public void SetFlammability(int src)
	{
		_flammability = src;
	}

	public void AddFlammability(int src)
	{
		_flammability += src;
	}

	public bool CanSetFire()
	{
		return _flammability >= -50;
	}

	// ---------- firm ----------//
	public bool IsFirm()
	{
		return (_locFlag & LOCATE_BLOCK_MASK) == LOCATE_IS_FIRM;
	}

	public bool CanBuildFirm(int teraMask = LOCATE_WALK_LAND)
	{
		return (CargoId == 0) && ((_locFlag & teraMask) != 0) && ((_locFlag & LOCATE_BLOCK_MASK) == 0) && !IsPowerOff();
	}

	public bool CanBuildHarbor(int teraMask = LOCATE_WALK_LAND)
	{
		return (CargoId == 0) && ((_locFlag & teraMask) != 0) && ((_locFlag & LOCATE_BLOCK_MASK) == 0);
	}

	public void SetFirm(int firmId)
	{
		// can't check the terrain type here
		_locFlag = (_locFlag & ~LOCATE_BLOCK_MASK) | LOCATE_IS_FIRM;
		CargoId = firmId;
		WalkableOff();
	}

	public void RemoveFirm()
	{
		_locFlag &= ~LOCATE_BLOCK_MASK;
		CargoId = 0;
		ResetWalkable();
	}

	public int FirmId()
	{
		return IsFirm() ? CargoId : 0;
	}

	// ---------- town ------------//
	public bool IsTown()
	{
		return (_locFlag & LOCATE_BLOCK_MASK) == LOCATE_IS_TOWN;
	}

	public bool CanBuildTown()
	{
		return (CargoId == 0) && ((_locFlag & LOCATE_WALK_LAND) != 0) && ((_locFlag & LOCATE_BLOCK_MASK) == 0) && !IsPowerOff() && !HasSite();
	}

	public void SetTown(int townId)
	{
		_locFlag = _locFlag & ~LOCATE_BLOCK_MASK | LOCATE_IS_TOWN;
		CargoId = townId;
		WalkableOff();
	}

	public void RemoveTown()
	{
		_locFlag &= ~LOCATE_BLOCK_MASK;
		CargoId = 0;
		ResetWalkable();
	}

	public int TownId()
	{
		return IsTown() ? CargoId : 0;
	}

	// ---------- unit ------------//
	public bool HasUnit(int mobileType)
	{
		return mobileType switch
		{
			UnitConstants.UNIT_LAND => Walkable() && CargoId != 0,
			UnitConstants.UNIT_SEA => Sailable() && CargoId != 0,
			UnitConstants.UNIT_AIR => AirCargoId != 0,
			_ => false
		};
	}

	public int HasAnyUnit(int mobileType = UnitConstants.UNIT_LAND)
	{
		if (mobileType == UnitConstants.UNIT_LAND)
		{
			if (AirCargoId != 0)
				return UnitConstants.UNIT_AIR;
			if (Walkable() && CargoId != 0)
				return UnitConstants.UNIT_LAND;
			if (Sailable() && CargoId != 0)
				return UnitConstants.UNIT_SEA;
		}
		else
		{
			if (Walkable() && CargoId != 0)
				return UnitConstants.UNIT_LAND;
			if (Sailable() && CargoId != 0)
				return UnitConstants.UNIT_SEA;
			if (AirCargoId != 0)
				return UnitConstants.UNIT_AIR;
		}

		return UnitConstants.UNIT_NONE;
	}

	public int GetAnyUnit()
	{
		if (AirCargoId != 0)
		{
			return AirCargoId;
		}
		if ((Walkable() || Sailable()) && CargoId != 0)
		{
			return CargoId;
		}

		return 0;
	}

	public int UnitId(int mobileType)
	{
		return mobileType == UnitConstants.UNIT_AIR ? AirCargoId : CargoId;
	}
	
	// whether the location is accessible to the unit of the specific mobile type
	public bool IsAccessible(int mobileType)
	{
		return mobileType switch
		{
			UnitConstants.UNIT_LAND => Walkable(),
			UnitConstants.UNIT_SEA => Sailable(),
			UnitConstants.UNIT_AIR => true,
			_ => false
		};
	}

	public bool IsUnitGroupAccessible(int mobileType, int curGroupId)
	{
		if (IsAccessible(mobileType))
		{
			int unitId = UnitId(mobileType);
			return unitId == 0 || UnitArray[unitId].GroupId == curGroupId;
		}

		return false;
	}

	public bool CanMove(int mobileType)
	{
		return IsAccessible(mobileType) && (mobileType == UnitConstants.UNIT_AIR ? AirCargoId == 0 : CargoId == 0);
	}

	//------------ power --------------//
	public void SetPowerOn()
	{
		_locFlag &= ~LOCATE_POWER_OFF;
	}

	public void SetPowerOff()
	{
		_locFlag |= LOCATE_POWER_OFF;
	}

	public bool IsPowerOff()
	{
		return (_locFlag & LOCATE_POWER_OFF) != 0;
	}

	//----------- harbor bit -----------//
	public void SetHarborBit()
	{
		_locFlag |= LOCATE_HARBOR_BIT;
	}

	public void ClearHarborBit()
	{
		_locFlag &= ~LOCATE_HARBOR_BIT;
	}

	public bool CanBuildWholeHarbor()
	{
		return (_locFlag & LOCATE_HARBOR_BIT) != 0;
	}
}
