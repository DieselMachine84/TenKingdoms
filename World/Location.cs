using System;

namespace TenKingdoms;

public class Location
{
	public const int LOCATE_WALK_LAND = 0x01;
	public const int LOCATE_WALK_SEA = 0x02;
	public const int LOCATE_COAST = 0x08;

	// ----- govern the usage of extra_para ---------//
	public const int LOCATE_SITE_MASK = 0xf0;
	public const int LOCATE_HAS_SITE = 0x10;
	public const int LOCATE_HAD_WALL = 0x20;
	public const int LOCATE_HAS_DIRT = 0x30;

	public const int LOCATE_SITE_RESERVED = 0xf0;
	//	occupied by other block such as hill, plant

	// ----- govern the usage of cargo_recno -------//
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

	public int loc_flag;
	public int terrain_id;

	public int cargo_recno;
	public int air_cargo_recno;

	public int extra_para;

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

	public int fire_level; // -100 to 100, current fire level
	public int flammability; // -100 to 100, likelihood of fire

	public int power_nation_recno; // 0-no nation has power over this location
	public int region_id;
	public int visit_level; // drop from FULL_VISIBILITY to 0

	public TerrainRes TerrainRes { get; }
	public HillRes HillRes { get; }

	private UnitArray UnitArray => Sys.Instance.UnitArray;

	public Location(TerrainRes terrainRes, HillRes hillRes)
	{
		TerrainRes = terrainRes;
		HillRes = hillRes;
	}

	public bool walkable()
	{
		return (loc_flag & LOCATE_WALK_LAND) != 0;
	}

	public bool sailable()
	{
		return (loc_flag & LOCATE_WALK_SEA) != 0;
	}

	public bool walkable(int teraMask)
	{
		return (loc_flag & teraMask) != 0;
	}

	public void walkable_reset()
	{
		if ((loc_flag & LOCATE_BLOCK_MASK) != 0)
		{
			walkable_off();
		}
		else
		{
			if (TerrainRes[terrain_id].average_type == TerrainTypeCode.TERRAIN_OCEAN)
			{
				loc_flag |= LOCATE_WALK_SEA;
			}
			else
			{
				loc_flag |= LOCATE_WALK_LAND;
			}
		}
	}

	// void	walkable_on()		{ loc_flag |= LOCATE_WALK_LAND; }
	public void walkable_off()
	{
		loc_flag &= ~(LOCATE_WALK_LAND | LOCATE_WALK_SEA);
	}

	public void walkable_on(int teraMask)
	{
		loc_flag |= teraMask;
	}

	public void walkable_off(int teraMask)
	{
		loc_flag &= ~teraMask;
	}

	public int is_coast()
	{
		return loc_flag & LOCATE_COAST;
	}

	//	int	explored()        { return loc_flag & LOCATE_EXPLORED; }
	//	void	explored_on()		{ loc_flag |= LOCATE_EXPLORED; }
	//	void	explored_off()		{ loc_flag &= (~LOCATE_EXPLORED); }
	public bool explored()
	{
		return visit_level > 0;
	}

	public void explored_on()
	{
		if (visit_level < EXPLORED_VISIBILITY * 2) visit_level = EXPLORED_VISIBILITY * 2;
	}

	public void explored_off()
	{
		visit_level = 0;
	}

	// ---------- visibility --------//
	public int visibility()
	{
		return visit_level / 2;
	}

	public void dec_visibility()
	{
		if (visit_level > EXPLORED_VISIBILITY * 2) --visit_level;
	}

	public void set_visited()
	{
		visit_level = MAX_VISIT_LEVEL * 2;
	}

	public void set_visited(int v)
	{
		if (visit_level < v * 2)
			visit_level = v * 2;
	}

	public bool is_plateau()
	{
		//**BUGHERE, to be changed to TERRAIN_HILL when the no. of terrain type has been reduced to 4 from 7
		return TerrainRes[terrain_id].average_type == TerrainTypeCode.TERRAIN_DARK_DIRT; 
	}

	// ----------- site -------------//
	public bool can_build_site(int teraMask = LOCATE_WALK_LAND)
	{
		return ((loc_flag & teraMask) != 0) && ((loc_flag & LOCATE_SITE_MASK) == 0) && !has_site();
	}

	public void set_site(int siteRecno)
	{
		loc_flag = loc_flag & ~LOCATE_SITE_MASK | LOCATE_HAS_SITE;
		// loc_flag |= LOCATION_HAS_SITE;

		extra_para = siteRecno;
	}

	public bool has_site()
	{
		return (loc_flag & LOCATE_SITE_MASK) == LOCATE_HAS_SITE;
	}

	public int site_recno()
	{
		return has_site() ? extra_para : 0;
	}

	public void remove_site()
	{
		loc_flag &= ~LOCATE_SITE_MASK;

		extra_para = 0;
	}

	// ------------ wall timeout ----------//
	public bool had_wall()
	{
		return (loc_flag & LOCATE_SITE_MASK) == LOCATE_HAD_WALL;
	}

	// after wall destructed, cannot build wall again for some time
	// the decrease time
	// (LOCATE_HAD_WALL)
	public void set_wall_timeout(int initTimeout)
	{
		loc_flag = (loc_flag & ~LOCATE_SITE_MASK) | LOCATE_HAD_WALL;
		extra_para = initTimeout;
	}

	public int wall_timeout()
	{
		return extra_para;
	}

	public int dec_wall_timeout(int t = 1)
	{
		if ((extra_para -= t) <= 0)
		{
			remove_wall_timeout();
			return 1;
		}

		return 0;
	}

	public void remove_wall_timeout()
	{
		loc_flag &= ~LOCATE_SITE_MASK;
		extra_para = 0;
	}

	// ----------- dirt -------------//
	public bool can_add_dirt()
	{
		return (loc_flag & LOCATE_SITE_MASK) == 0;
	}

	public void set_dirt(int dirtRecno)
	{
		loc_flag = loc_flag & ~LOCATE_SITE_MASK | LOCATE_HAS_DIRT;

		extra_para = dirtRecno;
	}

	public bool has_dirt()
	{
		return (loc_flag & LOCATE_SITE_MASK) == LOCATE_HAS_DIRT;
	}

	public int dirt_recno()
	{
		return has_dirt() ? extra_para : 0;
	}

	public void remove_dirt()
	{
		loc_flag &= ~LOCATE_SITE_MASK;

		extra_para = 0;
	}

	// ---------- firm ----------//
	public bool is_firm()
	{
		return (loc_flag & LOCATE_BLOCK_MASK) == LOCATE_IS_FIRM;
	}

	public bool can_build_firm(int teraMask = LOCATE_WALK_LAND)
	{
		return (cargo_recno == 0) && ((loc_flag & teraMask) != 0) && ((loc_flag & LOCATE_BLOCK_MASK) == 0) && !is_power_off();
	}

	public bool can_build_harbor(int teraMask = LOCATE_WALK_LAND)
	{
		return (cargo_recno == 0) && ((loc_flag & teraMask) != 0) && ((loc_flag & LOCATE_BLOCK_MASK) == 0);
	}

	public void set_firm(int firmRecno)
	{
		// can't check the terrain type here

		walkable_off();
		loc_flag = (loc_flag & ~LOCATE_BLOCK_MASK) | LOCATE_IS_FIRM;

		cargo_recno = firmRecno;
	}

	public int firm_recno()
	{
		return is_firm() ? cargo_recno : 0;
	}

	public void remove_firm()
	{
		loc_flag &= ~LOCATE_BLOCK_MASK;
		cargo_recno = 0;
		walkable_reset();
	}

	// ---------- town ------------//
	public bool is_town()
	{
		return (loc_flag & LOCATE_BLOCK_MASK) == LOCATE_IS_TOWN;
	}

	public void set_town(int townRecno)
	{
		walkable_off();
		loc_flag = loc_flag & ~LOCATE_BLOCK_MASK | LOCATE_IS_TOWN;

		cargo_recno = townRecno;
	}

	public bool can_build_town()
	{
		return (cargo_recno == 0) && ((loc_flag & LOCATE_WALK_LAND) != 0) && ((loc_flag & LOCATE_BLOCK_MASK) == 0)
		       && !has_site() && !is_power_off();
	}

	public int town_recno()
	{
		return is_town() ? cargo_recno : 0;
	}

	public void remove_town()
	{
		loc_flag &= ~LOCATE_BLOCK_MASK;
		cargo_recno = 0;
		walkable_reset();
	}

	// ---------- hill -------------//
	public bool has_hill()
	{
		return (loc_flag & LOCATE_BLOCK_MASK) == LOCATE_IS_HILL;
	}

	public bool can_add_hill() // exception : it has already a hill
	{
		return has_hill() || // (loc_flag & LOCATE_WALK_LAND) &&
		       (cargo_recno == 0) && ((loc_flag & (LOCATE_BLOCK_MASK | LOCATE_SITE_MASK)) == 0);
	}

	public void set_hill(int hillId)
	{
		// clear LOCATE_WALK_LAND and LOCATE_WALK_SEA bits
		walkable_off();

		if (has_hill())
		{
			// already has a hill block
			// compare which is on the top, swap if necessary
			if (HillRes[cargo_recno].priority <= HillRes[hillId].priority)
			{
				extra_para = cargo_recno;
				cargo_recno = hillId;
			}
			else
			{
				// if two hill blocks there, the lower one get replaced
				extra_para = hillId;
			}
		}
		else
		{
			// no existing hill block
			loc_flag = loc_flag & ~(LOCATE_BLOCK_MASK | LOCATE_SITE_MASK) | (LOCATE_IS_HILL | LOCATE_SITE_RESERVED);
			cargo_recno = hillId;
			extra_para = 0;
		}
	}

	public int hill_id1()
	{
		return cargo_recno;
	}

	public int hill_id2()
	{
		return extra_para;
	}

	public void remove_hill()
	{
		loc_flag &= ~(LOCATE_BLOCK_MASK | LOCATE_SITE_MASK);

		extra_para = 0;
		cargo_recno = 0;
		// BUGHERE : need to call walkable_reset();
	}

	// ---------- wall ------------//
	public bool is_wall()
	{
		return (loc_flag & LOCATE_BLOCK_MASK) == LOCATE_IS_WALL;
	}

	public bool can_build_wall()
	{
		return (cargo_recno == 0) && ((loc_flag & LOCATE_WALK_LAND) != 0) &&
		       ((loc_flag & (LOCATE_BLOCK_MASK | LOCATE_SITE_MASK)) == 0) && !has_site();
	}

	public void set_wall(int wallId, int townRecno, int hitPoints)
	{
		walkable_off();
		loc_flag = loc_flag & ~(LOCATE_BLOCK_MASK | LOCATE_SITE_MASK) | (LOCATE_IS_WALL | LOCATE_SITE_RESERVED);

		extra_para = wallId;
		cargo_recno = (hitPoints << 8) + townRecno;
	}

	public void set_wall_creating()
	{
		int hp = wall_hit_point();
		if (hp < 0)
			hp = -hp;
		cargo_recno = (hp << 8) | wall_town_recno();
	}

	public void set_wall_destructing()
	{
		int hp = wall_hit_point();
		if (hp > 0)
			hp = -hp;
		cargo_recno = (hp << 8) | wall_town_recno();
	}

	public void chg_wall_id(int wallId)
	{
		extra_para = wallId;
	}

	public int wall_id()
	{
		return is_wall() ? extra_para : 0;
	}

	public int wall_nation_recno()
	{
		return power_nation_recno;
	}

	public int wall_hit_point()
	{
		return cargo_recno >> 8;
	}

	public int wall_town_recno()
	{
		return cargo_recno | 0xFF;
	}

	//---------------------------------------------------//
	// initial 0, 1 to 100:creating, -1 to -100: destructing
	// except 0 or 100, hit point slowly increase by 1
	//---------------------------------------------------//
	public int wall_abs_hit_point()
	{
		return wall_hit_point() >= 0 ? wall_hit_point() : -wall_hit_point();
	}

	public int inc_wall_hit_point(int grow = 1)
	{
		int hp = wall_hit_point();
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

		cargo_recno = (hp << 8) | wall_town_recno();
		return hp;
	}

	public int attack_wall(int damage = 1)
	{
		const int WALL_DEFENCE = 5;
		const int MIN_WALL_DAMAGE = 3;

		if (damage >= WALL_DEFENCE + MIN_WALL_DAMAGE) // damage >= 8, damage -= 5
			damage -= WALL_DEFENCE;
		else if (damage >= MIN_WALL_DAMAGE) // 3 <= damage < 8, damage = 3
			damage = MIN_WALL_DAMAGE;
		else if (damage <= 0) // 0 < damage < 3, no change to
			return wall_hit_point(); // no change to hit point to damage

		int hp = wall_hit_point();
		if (hp > 0)
		{
			hp -= damage;
			if (hp <= 0)
			{
				hp = 0;
				remove_wall();
				return 0;
			}
		}
		else if (hp < 0)
		{
			hp += damage;
			if (hp >= 0)
			{
				hp = 0;
				remove_wall();
				return 0;
			}
		}

		cargo_recno = (hp << 8) | wall_town_recno();
		return hp;
	}

	public int wall_grade()
	{
		return wall_hit_point() >= 0 ? (wall_hit_point() + 24) / 25 : (wall_hit_point() - 24) / 25;
	}

	public bool is_wall_creating()
	{
		return wall_hit_point() > 0;
	}

	public bool is_wall_destructing()
	{
		return wall_hit_point() < 0;
	}

	public void remove_wall(int setTimeOut = -1)
	{
		const int DEFAULT_WALL_TIMEOUT = 10;

		loc_flag &= ~(LOCATE_BLOCK_MASK | LOCATE_SITE_MASK);
		extra_para = 0;
		cargo_recno = 0;
		walkable_reset();

		if (setTimeOut < 0)
			set_wall_timeout(DEFAULT_WALL_TIMEOUT);
		else if (setTimeOut > 0)
		{
			set_wall_timeout(setTimeOut);
		}
	}

	// ---------- plant -----------//
	public bool is_plant()
	{
		return (loc_flag & LOCATE_BLOCK_MASK) == LOCATE_IS_PLANT;
	}

	public bool can_add_plant(int teraMask = LOCATE_WALK_LAND)
	{
		return (cargo_recno == 0) && ((loc_flag & teraMask) != 0) &&
		       ((loc_flag & (LOCATE_BLOCK_MASK | LOCATE_SITE_MASK)) == 0) && !has_site();
	}

	public void set_plant(int plantId, int offsetX, int offsetY)
	{
		walkable_off();
		loc_flag = loc_flag & ~(LOCATE_BLOCK_MASK | LOCATE_SITE_MASK)
		           | (LOCATE_IS_PLANT | LOCATE_SITE_RESERVED);

		extra_para = plantId;
		cargo_recno = (offsetY << 8) + offsetX;
	}

	public int plant_id()
	{
		return is_plant() ? extra_para : 0;
	}

	public int plant_inner_x()
	{
		return cargo_recno & 0xFF;
	}

	public int plant_inner_y()
	{
		return cargo_recno >> 8;
	}

	public void grow_plant()
	{
		extra_para++;
	}

	public void remove_plant()
	{
		loc_flag &= ~(LOCATE_BLOCK_MASK | LOCATE_SITE_MASK);
		extra_para = 0;
		cargo_recno = 0;
		walkable_reset();
	}

	// ---------- rock ------------//
	public bool is_rock()
	{
		return (loc_flag & LOCATE_BLOCK_MASK) == LOCATE_IS_ROCK;
	}

	public bool can_add_rock(int teraMask = LOCATE_WALK_LAND)
	{
		return (cargo_recno == 0) && ((loc_flag & teraMask) != 0) && ((loc_flag & LOCATE_BLOCK_MASK) == 0);
	}

	public void set_rock(int rockArrayRecno)
	{
		walkable_off();
		loc_flag = loc_flag & ~LOCATE_BLOCK_MASK | LOCATE_IS_ROCK;

		cargo_recno = rockArrayRecno;
	}

	public int rock_array_recno()
	{
		return is_rock() ? cargo_recno : 0;
	}

	public void remove_rock()
	{
		loc_flag &= ~LOCATE_BLOCK_MASK;
		cargo_recno = 0;
		walkable_reset();
	}

	// call region_type only when generating region number
	public RegionType region_type()
	{
		return walkable() ? RegionType.REGION_LAND : (sailable() ? RegionType.REGION_SEA : RegionType.REGION_INPASSABLE);
	}

	// --------- functions on fire ---------//
	public int fire_str()
	{
		return fire_level;
	}

	public int fire_src()
	{
		return flammability;
	}

	public void set_fire_str(int str)
	{
		fire_level = str;
	}

	public void set_fire_src(int src)
	{
		flammability = src;
	}

	public void add_fire_str(int str)
	{
		fire_level += str;
	}

	public void add_fire_src(int src)
	{
		flammability += src;
	}

	public bool can_set_fire()
	{
		return flammability >= -50;
	}

	//----- functions whose results affected by mobile_type -----//

	//int   is_blocked(int mobileType)    { return mobileType==UnitConstants.UNIT_AIR ? air_cargo_recno : cargo_recno; }     // return 1 or 0 (although both are the same)
	public int unit_recno(int mobileType)
	{
		return mobileType == UnitConstants.UNIT_AIR ? air_cargo_recno : cargo_recno;
	} // return the exact cargo recno

	public bool has_unit(int mobileType)
	{
		switch (mobileType)
		{
			case UnitConstants.UNIT_LAND:
				if (walkable())
					return cargo_recno != 0;
				break;

			case UnitConstants.UNIT_SEA:
				if (sailable())
					return cargo_recno != 0;
				break;

			case UnitConstants.UNIT_AIR:
				return air_cargo_recno != 0;
		}

		return false;
	}

	public int has_any_unit(int mobileType = UnitConstants.UNIT_LAND)
	{
		if (mobileType == UnitConstants.UNIT_LAND)
		{
			if (air_cargo_recno != 0)
				return UnitConstants.UNIT_AIR;
			else if (walkable() && cargo_recno != 0)
				return UnitConstants.UNIT_LAND;
			else if (sailable() && cargo_recno != 0)
				return UnitConstants.UNIT_SEA;
		}
		else
		{
			if (walkable() && cargo_recno != 0)
				return UnitConstants.UNIT_LAND;
			else if (sailable() && cargo_recno != 0)
				return UnitConstants.UNIT_SEA;
			else if (air_cargo_recno != 0)
				return UnitConstants.UNIT_AIR;
		}

		return UnitConstants.UNIT_NONE;
	}

	public int get_any_unit(out int mobileType)
	{
		if (air_cargo_recno != 0)
		{
			mobileType = UnitConstants.UNIT_AIR;
			return air_cargo_recno;
		}
		else if (walkable() && cargo_recno != 0)
		{
			mobileType = UnitConstants.UNIT_LAND;
			return cargo_recno;
		}
		else if (sailable() && cargo_recno != 0)
		{
			mobileType = UnitConstants.UNIT_SEA;
			return cargo_recno;
		}

		mobileType = UnitConstants.UNIT_NONE;
		return 0;
	}

	// whether the location is accessible to the unit of the specific mobile type
	public bool is_accessible(int mobileType)
	{
		switch (mobileType)
		{
			case UnitConstants.UNIT_LAND:
				return walkable();

			case UnitConstants.UNIT_SEA:
				return sailable();

			case UnitConstants.UNIT_AIR:
				return true;
		}

		return false;
	}

	public bool is_unit_group_accessible(int mobileType, int curGroupId)
	{
		if (is_accessible(mobileType))
		{
			int unitRecno = unit_recno(mobileType);

			return unitRecno == 0 || UnitArray[unitRecno].unit_group_id == curGroupId;
		}

		return false;
	}

	//int   can_move(int mobileType)      { return is_accessible(mobileType) && cargo_recno==0; }
	public bool can_move(int mobileType)
	{
		return is_accessible(mobileType) && (mobileType == UnitConstants.UNIT_AIR ? air_cargo_recno == 0 : cargo_recno == 0);
	}

	//------------ power --------------//
	public void set_power_on()
	{
		loc_flag &= ~LOCATE_POWER_OFF;
	}

	public void set_power_off()
	{
		loc_flag |= LOCATE_POWER_OFF;
	}

	public bool is_power_off()
	{
		return (loc_flag & LOCATE_POWER_OFF) != 0;
	}

	//----------- harbor bit -----------//
	public void set_harbor_bit()
	{
		loc_flag |= LOCATE_HARBOR_BIT;
	}

	public void clear_harbor_bit()
	{
		loc_flag &= ~LOCATE_HARBOR_BIT;
	}

	public bool can_build_whole_harbor()
	{
		return (loc_flag & LOCATE_HARBOR_BIT) != 0;
	}
}
