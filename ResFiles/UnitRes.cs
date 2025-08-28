using System;
using System.Linq;

namespace TenKingdoms;

public class UnitRec
{
	public const int NAME_LEN = 15;
	public const int SPRITE_CODE_LEN = 8;
	public const int RACE_CODE_LEN = 8;
	public const int UNIT_CLASS_LEN = 8;
	public const int UNIT_PARA_LEN = 3;
	public const int BUILD_DAYS_LEN = 3;
	public const int YEAR_COST_LEN = 3;
	public const int CARRY_CAPACITY_LEN = 3;
	public const int FREE_WEAPON_COUNT_LEN = 1;
	public const int FILE_NAME_LEN = 8;
	public const int BITMAP_PTR_LEN = 4;
	public const int SPRITE_ID_LEN = 3;
	public const int RACE_ID_LEN = 3;

	public char[] name = new char[NAME_LEN];
	public char[] sprite_code = new char[SPRITE_CODE_LEN];
	public char[] race_code = new char[RACE_CODE_LEN];
	public char[] unit_class = new char[UNIT_CLASS_LEN];

	public byte mobile_type;
	public byte all_know;

	public char[] visual_range = new char[UNIT_PARA_LEN];
	public char[] visual_extend = new char[UNIT_PARA_LEN];
	public char[] stealth = new char[UNIT_PARA_LEN];
	public char[] hit_points = new char[UNIT_PARA_LEN];
	public char[] armor = new char[UNIT_PARA_LEN];

	public char[] build_days = new char[BUILD_DAYS_LEN];
	public char[] year_cost = new char[YEAR_COST_LEN];

	public byte weapon_power; // an index from 1 to 9 indicating the powerfulness of the weapon 

	public char[] carry_unit_capacity = new char[CARRY_CAPACITY_LEN];
	public char[] carry_goods_capacity = new char[CARRY_CAPACITY_LEN];
	public char[] free_weapon_count = new char[FREE_WEAPON_COUNT_LEN];

	public char[] vehicle_code = new char[SPRITE_CODE_LEN];
	public char[] vehicle_unit_code = new char[SPRITE_CODE_LEN];

	public char[] transform_unit = new char[SPRITE_CODE_LEN];
	public char[] transform_combat_level = new char[UNIT_PARA_LEN];
	public char[] guard_combat_level = new char[UNIT_PARA_LEN];

	public char[] large_icon_file_name = new char[FILE_NAME_LEN];
	public byte[] large_icon_ptr = new byte[BITMAP_PTR_LEN];
	public char[] general_icon_file_name = new char[FILE_NAME_LEN];
	public byte[] general_icon_ptr = new byte[BITMAP_PTR_LEN];
	public char[] king_icon_file_name = new char[FILE_NAME_LEN];
	public byte[] king_icon_ptr = new byte[BITMAP_PTR_LEN];

	public char[] small_icon_file_name = new char[FILE_NAME_LEN];
	public byte[] small_icon_ptr = new byte[BITMAP_PTR_LEN];

	public char[] general_small_icon_file_name = new char[FILE_NAME_LEN];
	public byte[] general_small_icon_ptr = new byte[BITMAP_PTR_LEN];
	public char[] king_small_icon_file_name = new char[FILE_NAME_LEN];
	public byte[] king_small_icon_ptr = new byte[BITMAP_PTR_LEN];

	public char[] die_effect_sprite = new char[SPRITE_CODE_LEN];

	public char[] sprite_id = new char[SPRITE_ID_LEN];
	public char[] dll_sprite_id = new char[SPRITE_ID_LEN];
	public char[] race_id = new char[RACE_ID_LEN];

	public char[] vehicle_id = new char[SPRITE_ID_LEN];
	public char[] vehicle_unit_id = new char[SPRITE_ID_LEN];

	public char[] transform_unit_id = new char[SPRITE_ID_LEN];
	public char[] die_effect_id = new char[UNIT_PARA_LEN];

	public char[] first_attack = new char[UNIT_PARA_LEN];
	public char[] attack_count = new char[UNIT_PARA_LEN];

	public UnitRec(byte[] data)
	{
		int dataIndex = 0;
		for (int i = 0; i < name.Length; i++, dataIndex++)
			name[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < sprite_code.Length; i++, dataIndex++)
			sprite_code[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < race_code.Length; i++, dataIndex++)
			race_code[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < unit_class.Length; i++, dataIndex++)
			unit_class[i] = Convert.ToChar(data[dataIndex]);

		mobile_type = data[dataIndex];
		dataIndex++;
		all_know = data[dataIndex];
		dataIndex++;

		for (int i = 0; i < visual_range.Length; i++, dataIndex++)
			visual_range[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < visual_extend.Length; i++, dataIndex++)
			visual_extend[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < stealth.Length; i++, dataIndex++)
			stealth[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < hit_points.Length; i++, dataIndex++)
			hit_points[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < armor.Length; i++, dataIndex++)
			armor[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < build_days.Length; i++, dataIndex++)
			build_days[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < year_cost.Length; i++, dataIndex++)
			year_cost[i] = Convert.ToChar(data[dataIndex]);

		weapon_power = data[dataIndex];
		dataIndex++;

		for (int i = 0; i < carry_unit_capacity.Length; i++, dataIndex++)
			carry_unit_capacity[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < carry_goods_capacity.Length; i++, dataIndex++)
			carry_goods_capacity[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < free_weapon_count.Length; i++, dataIndex++)
			free_weapon_count[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < vehicle_code.Length; i++, dataIndex++)
			vehicle_code[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < vehicle_unit_code.Length; i++, dataIndex++)
			vehicle_unit_code[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < transform_unit.Length; i++, dataIndex++)
			transform_unit[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < transform_combat_level.Length; i++, dataIndex++)
			transform_combat_level[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < guard_combat_level.Length; i++, dataIndex++)
			guard_combat_level[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < large_icon_file_name.Length; i++, dataIndex++)
			large_icon_file_name[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < large_icon_ptr.Length; i++, dataIndex++)
			large_icon_ptr[i] = data[dataIndex];

		for (int i = 0; i < general_icon_file_name.Length; i++, dataIndex++)
			general_icon_file_name[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < general_icon_ptr.Length; i++, dataIndex++)
			general_icon_ptr[i] = data[dataIndex];

		for (int i = 0; i < king_icon_file_name.Length; i++, dataIndex++)
			king_icon_file_name[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < king_icon_ptr.Length; i++, dataIndex++)
			king_icon_ptr[i] = data[dataIndex];

		for (int i = 0; i < small_icon_file_name.Length; i++, dataIndex++)
			small_icon_file_name[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < small_icon_ptr.Length; i++, dataIndex++)
			small_icon_ptr[i] = data[dataIndex];

		for (int i = 0; i < general_small_icon_file_name.Length; i++, dataIndex++)
			general_small_icon_file_name[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < general_small_icon_ptr.Length; i++, dataIndex++)
			general_small_icon_ptr[i] = data[dataIndex];

		for (int i = 0; i < king_small_icon_file_name.Length; i++, dataIndex++)
			king_small_icon_file_name[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < king_small_icon_ptr.Length; i++, dataIndex++)
			king_small_icon_ptr[i] = data[dataIndex];

		for (int i = 0; i < die_effect_sprite.Length; i++, dataIndex++)
			die_effect_sprite[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < sprite_id.Length; i++, dataIndex++)
			sprite_id[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < dll_sprite_id.Length; i++, dataIndex++)
			dll_sprite_id[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < race_id.Length; i++, dataIndex++)
			race_id[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < vehicle_id.Length; i++, dataIndex++)
			vehicle_id[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < vehicle_unit_id.Length; i++, dataIndex++)
			vehicle_unit_id[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < transform_unit_id.Length; i++, dataIndex++)
			transform_unit_id[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < die_effect_id.Length; i++, dataIndex++)
			die_effect_id[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < first_attack.Length; i++, dataIndex++)
			first_attack[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < attack_count.Length; i++, dataIndex++)
			attack_count[i] = Convert.ToChar(data[dataIndex]);
	}
}

public class UnitAttackRec
{
	public const int SPRITE_CODE_LEN = 8;
	public const int UNIT_PARA_LEN = 3;
	public const int COMBAT_LEVEL_LEN = 3;

	public char[] sprite_code = new char[SPRITE_CODE_LEN];

	public char[] attack_id = new char[UNIT_PARA_LEN];
	public char[] combat_level = new char[COMBAT_LEVEL_LEN];

	public char[] attack_delay = new char[UNIT_PARA_LEN];
	public char[] attack_range = new char[UNIT_PARA_LEN];

	public char[] attack_damage = new char[UNIT_PARA_LEN];
	public char[] pierce_damage = new char[UNIT_PARA_LEN];

	public char[] bullet_out_frame = new char[UNIT_PARA_LEN];
	public char[] bullet_speed = new char[UNIT_PARA_LEN];
	public char[] bullet_radius = new char[UNIT_PARA_LEN];
	public char[] bullet_sprite_code = new char[SPRITE_CODE_LEN];
	public char[] bullet_sprite_id = new char[UNIT_PARA_LEN];
	public char[] dll_bullet_sprite_id = new char[UNIT_PARA_LEN];
	public char[] eqv_attack_next = new char[UNIT_PARA_LEN];
	public char[] min_power = new char[UNIT_PARA_LEN];
	public char[] consume_power = new char[UNIT_PARA_LEN];
	public char[] fire_radius = new char[UNIT_PARA_LEN];
	public char[] effect_code = new char[SPRITE_CODE_LEN];
	public char[] effect_id = new char[UNIT_PARA_LEN];

	public UnitAttackRec(byte[] data)
	{
		int dataIndex = 0;
		for (int i = 0; i < sprite_code.Length; i++, dataIndex++)
			sprite_code[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < attack_id.Length; i++, dataIndex++)
			attack_id[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < combat_level.Length; i++, dataIndex++)
			combat_level[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < attack_delay.Length; i++, dataIndex++)
			attack_delay[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < attack_range.Length; i++, dataIndex++)
			attack_range[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < attack_damage.Length; i++, dataIndex++)
			attack_damage[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < pierce_damage.Length; i++, dataIndex++)
			pierce_damage[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < bullet_out_frame.Length; i++, dataIndex++)
			bullet_out_frame[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < bullet_speed.Length; i++, dataIndex++)
			bullet_speed[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < bullet_radius.Length; i++, dataIndex++)
			bullet_radius[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < bullet_sprite_code.Length; i++, dataIndex++)
			bullet_sprite_code[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < bullet_sprite_id.Length; i++, dataIndex++)
			bullet_sprite_id[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < dll_bullet_sprite_id.Length; i++, dataIndex++)
			dll_bullet_sprite_id[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < eqv_attack_next.Length; i++, dataIndex++)
			eqv_attack_next[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < min_power.Length; i++, dataIndex++)
			min_power[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < consume_power.Length; i++, dataIndex++)
			consume_power[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < fire_radius.Length; i++, dataIndex++)
			fire_radius[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < effect_code.Length; i++, dataIndex++)
			effect_code[i] = Convert.ToChar(data[dataIndex]);

		for (int i = 0; i < effect_id.Length; i++, dataIndex++)
			effect_id[i] = Convert.ToChar(data[dataIndex]);
	}
}

public class UnitInfo
{
	public string name;

	public int unit_id;
	public int sprite_id;
	public int dll_sprite_id;
	public int race_id;
	public char unit_class;

	public int mobile_type;

	public int visual_range;
	public int visual_extend;
	public int stealth;
	public int armor;

	public int hit_points;

	public int build_days;
	public int build_cost;
	public int year_cost;

	public int weapon_power; // an index from 1 to 9 indicating the powerfulness of the weapon

	public int carry_unit_capacity;
	public int carry_goods_capacity;
	public int free_weapon_count; // only for ships. It's the no. of free weapons can be loaded onto the ship

	public int vehicle_id;
	public int vehicle_unit_id;
	public int solider_id;

	public int transform_unit_id;
	public int transform_combat_level;
	public int guard_combat_level;

	public int first_attack;
	public int attack_count;
	public int die_effect_id;

	public byte[] soldierIcon;
	public int soldierIconWidth;
	public int soldierIconHeight;
	private IntPtr _soldierIconTexture;
	public byte[] generalIcon;
	public int generalIconWidth;
	public int generalIconHeight;
	private IntPtr _generalIconTexture;
	public byte[] kingIcon;
	public int kingIconWidth;
	public int kingIconHeight;
	private IntPtr _kingIconTexture;

	public byte[] soldierSmallIcon;
	public int soldierSmallIconWidth;
	public int soldierSmallIconHeight;
	private IntPtr _soldierSmallIconTexture;
	public byte[] generalSmallIcon;
	public int generalSmallIconWidth;
	public int generalSmallIconHeight;
	private IntPtr _generalSmallIconTexture;
	public byte[] kingSmallIcon;
	public int kingSmallIconWidth;
	public int kingSmallIconHeight;
	private IntPtr _kingSmallIconTexture;

	public IntPtr GetSoldierIconTexture(Graphics graphics)
	{
		if (_soldierIconTexture == default)
			_soldierIconTexture = graphics.CreateTextureFromBmp(soldierIcon, soldierIconWidth, soldierIconHeight);

		return _soldierIconTexture;
	}
	
	public IntPtr GetGeneralIconTexture(Graphics graphics)
	{
		if (_generalIconTexture == default)
			_generalIconTexture = graphics.CreateTextureFromBmp(generalIcon, generalIconWidth, generalIconHeight);

		return _generalIconTexture;
	}
	
	public IntPtr GetKingIconTexture(Graphics graphics)
	{
		if (_kingIconTexture == default)
			_kingIconTexture = graphics.CreateTextureFromBmp(kingIcon, kingIconWidth, kingIconHeight);

		return _kingIconTexture;
	}
	
	public IntPtr GetSoldierSmallIconTexture(Graphics graphics)
	{
		if (_soldierSmallIconTexture == default)
			_soldierSmallIconTexture = graphics.CreateTextureFromBmp(soldierSmallIcon, soldierSmallIconWidth, soldierSmallIconHeight);

		return _soldierSmallIconTexture;
	}
	
	public IntPtr GetGeneralSmallIconTexture(Graphics graphics)
	{
		if (_generalSmallIconTexture == default)
			_generalSmallIconTexture = graphics.CreateTextureFromBmp(generalSmallIcon, generalSmallIconWidth, generalSmallIconHeight);

		return _generalSmallIconTexture;
	}

	public IntPtr GetKingSmallIconTexture(Graphics graphics)
	{
		if (_kingSmallIconTexture == default)
			_kingSmallIconTexture = graphics.CreateTextureFromBmp(kingSmallIcon, kingSmallIconWidth, kingSmallIconHeight);

		return _kingSmallIconTexture;
	}
	
	public IntPtr GetLargeIconTexture(Graphics graphics, int rankId)
	{
		return rankId switch
		{
			Unit.RANK_KING => GetKingIconTexture(graphics),
			Unit.RANK_GENERAL => GetGeneralIconTexture(graphics),
			_ => GetSoldierIconTexture(graphics)
		};
	}

	public IntPtr GetSmallIconTexture(Graphics graphics, int rankId)
	{
		return rankId switch
		{
			Unit.RANK_KING => GetKingSmallIconTexture(graphics),
			Unit.RANK_GENERAL => GetGeneralSmallIconTexture(graphics),
			_ => GetSoldierSmallIconTexture(graphics)
		};
	}
	
	// each nation's tech level on this unit
	public int[] nation_tech_level_array = new int[GameConstants.MAX_NATION];
	
	private NationArray NationArray => Sys.Instance.NationArray;

	public int get_nation_tech_level(int nationRecno)
	{
		return nation_tech_level_array[nationRecno - 1];
	}

	public void set_nation_tech_level(int nationRecno, int techLevel)
	{
		nation_tech_level_array[nationRecno - 1] = techLevel;
	}

	// mobile units + soldiers in camps, not including workers and prayers in bases
	public int[] nation_unit_count_array = new int[GameConstants.MAX_NATION];

	public int[] nation_general_count_array = new int[GameConstants.MAX_NATION];

	public byte[] get_large_icon_ptr(int rankId)
	{
		switch (rankId)
		{
			case Unit.RANK_KING:
				return kingIcon;
			case Unit.RANK_GENERAL:
				return generalIcon;
			default:
				return soldierIcon;
		}
	}

	public byte[] get_small_icon_ptr(int rankId)
	{
		switch (rankId)
		{
			case Unit.RANK_KING:
				return kingSmallIcon;
			case Unit.RANK_GENERAL:
				return generalSmallIcon;
			default:
				return soldierSmallIcon;
		}
	}

	public void inc_nation_unit_count(int nationRecno)
	{
		nation_unit_count_array[nationRecno - 1]++;

		//------- increase the nation's unit count ---------//

		Nation nation = NationArray[nationRecno];

		nation.total_unit_count++;

		if (unit_class == UnitConstants.UNIT_CLASS_WEAPON)
		{
			nation.total_weapon_count++;
		}
		else if (unit_class == UnitConstants.UNIT_CLASS_SHIP)
		{
			nation.total_ship_count++;
		}
		else if (race_id != 0)
		{
			nation.total_human_count++;
		}
	}

	public void dec_nation_unit_count(int nationRecno)
	{
		if (nationRecno > 0)
		{
			nation_unit_count_array[nationRecno - 1]--;

			//------ decrease the nation's unit count -------//

			Nation nation = NationArray[nationRecno];

			nation.total_unit_count--;

			if (unit_class == UnitConstants.UNIT_CLASS_WEAPON)
			{
				nation.total_weapon_count--;
			}
			else if (unit_class == UnitConstants.UNIT_CLASS_SHIP)
			{
				nation.total_ship_count--;
			}
			else if (race_id != 0)
			{
				nation.total_human_count--;

				if (nation.total_human_count < 0)
					nation.total_human_count = 0;
			}
		}
	}

	public void inc_nation_general_count(int nationRecno)
	{
		nation_general_count_array[nationRecno-1]++;
		NationArray[nationRecno].total_general_count++;
	}

	public void dec_nation_general_count(int nationRecno)
	{
		if (nationRecno != 0)
		{
			nation_general_count_array[nationRecno - 1]--;
			NationArray[nationRecno].total_general_count--;
		}
	}

	public void unit_change_nation(int newNationRecno, int oldNationRecno, int rankId)
	{
		//---- update nation_unit_count_array[] ----//

		if (oldNationRecno > 0)
		{
			if (rankId != Unit.RANK_KING)
				dec_nation_unit_count(oldNationRecno);

			if (rankId == Unit.RANK_GENERAL)
				dec_nation_general_count(oldNationRecno);
		}

		if (newNationRecno > 0)
		{
			if (rankId != Unit.RANK_KING)
				inc_nation_unit_count(newNationRecno);

			if (rankId == Unit.RANK_GENERAL) // if the new rank is general
				inc_nation_general_count(newNationRecno);
		}
	}
}

public class AttackInfo
{
	public int combat_level;

	public int attack_delay;
	public int attack_range;

	public int attack_damage;
	public int pierce_damage;

	public int bullet_out_frame; // on which attacking frames the bullet should be out
	public int bullet_speed;
	public int bullet_radius;
	public int bullet_sprite_id;
	public int dll_bullet_sprite_id;

	public int eqv_attack_next;

	// cur_attack of the next equivalent attack
	// so as to cycle through several similar attacks
	public int min_power;
	public int consume_power;
	public int fire_radius;
	public int effect_id;
}

public class UnitRes
{
	private const string UNIT_DB = "UNIT";
	private const string UNIT_ATTACK_DB = "UNITATTK";

	public UnitInfo[] unit_info_array;
	public AttackInfo[] attack_info_array;

	public ResourceDb res_large_icon;
	public ResourceDb res_general_icon;
	public ResourceDb res_king_icon;
	public ResourceDb res_small_icon;
	public ResourceDb res_general_small_icon;
	public ResourceDb res_king_small_icon;

	public int mobile_monster_count;

	public GameSet GameSet { get; }
	
	public UnitRes(GameSet gameSet)
	{
		GameSet = gameSet;
		
		//----- open unit bitmap resource file -------//

		res_large_icon = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_UNITLI.RES");
		res_general_icon = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_UNITGI.RES");
		res_king_icon = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_UNITKI.RES");
		res_small_icon = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_UNITSI.RES");
		res_general_small_icon = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_UNITTI.RES");
		res_king_small_icon = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_UNITUI.RES");

		//------- load database information --------//

		LoadInfo();
		LoadAttackInfo();
	}

	public UnitInfo this[int unitType] => unit_info_array[unitType - 1];

	public AttackInfo GetAttackInfo(int attackId)
	{
		return attack_info_array[attackId - 1];
	}

	public static int mobile_type_to_mask(char mobileType)
	{
		switch (mobileType)
		{
			case UnitConstants.UNIT_LAND:
				return 1;

			case UnitConstants.UNIT_SEA:
				return 2;

			case UnitConstants.UNIT_AIR:
				return 3;

			default:
				return 0;
		}
	}

	private void LoadInfo()
	{
		Database dbUnit = GameSet.OpenDb(UNIT_DB);
		unit_info_array = new UnitInfo[dbUnit.RecordCount];

		for (int i = 0; i < unit_info_array.Length; i++)
		{
			UnitRec unitRec = new UnitRec(dbUnit.Read(i + 1));
			UnitInfo unitInfo = new UnitInfo();
			unit_info_array[i] = unitInfo;

			unitInfo.name = Misc.ToString(unitRec.name);

			unitInfo.unit_id = i + 1;
			unitInfo.sprite_id = Misc.ToInt32(unitRec.sprite_id);
			unitInfo.dll_sprite_id = Misc.ToInt32(unitRec.dll_sprite_id);
			unitInfo.race_id = Misc.ToInt32(unitRec.race_id);

			unitInfo.unit_class = unitRec.unit_class[0];
			unitInfo.mobile_type = unitRec.mobile_type;

			unitInfo.visual_range = Misc.ToInt32(unitRec.visual_range);
			unitInfo.visual_extend = Misc.ToInt32(unitRec.visual_extend);
			unitInfo.stealth = Misc.ToInt32(unitRec.stealth);
			unitInfo.hit_points = Misc.ToInt32(unitRec.hit_points);
			unitInfo.armor = Misc.ToInt32(unitRec.armor);

			unitInfo.build_days = Misc.ToInt32(unitRec.build_days);
			unitInfo.year_cost = Misc.ToInt32(unitRec.year_cost);
			unitInfo.build_cost = unitInfo.year_cost;

			if (unitInfo.unit_class == UnitConstants.UNIT_CLASS_WEAPON)
				unitInfo.weapon_power = unitRec.weapon_power - '0';

			unitInfo.carry_unit_capacity = Misc.ToInt32(unitRec.carry_unit_capacity);
			unitInfo.carry_goods_capacity = Misc.ToInt32(unitRec.carry_goods_capacity);
			unitInfo.free_weapon_count = Misc.ToInt32(unitRec.free_weapon_count);

			unitInfo.vehicle_id = Misc.ToInt32(unitRec.vehicle_id);
			unitInfo.vehicle_unit_id = Misc.ToInt32(unitRec.vehicle_unit_id);

			unitInfo.transform_unit_id = Misc.ToInt32(unitRec.transform_unit_id);
			unitInfo.transform_combat_level = Misc.ToInt32(unitRec.transform_combat_level);
			unitInfo.guard_combat_level = Misc.ToInt32(unitRec.guard_combat_level);

			int bitmapOffset = BitConverter.ToInt32(unitRec.large_icon_ptr, 0);
			byte[] soldierIconData = res_large_icon.Read(bitmapOffset);
			unitInfo.soldierIconWidth = BitConverter.ToInt16(soldierIconData, 0);
			unitInfo.soldierIconHeight = BitConverter.ToInt16(soldierIconData, 2);
			unitInfo.soldierIcon = soldierIconData.Skip(4).ToArray();

			if (unitRec.general_icon_file_name[0] != '\0' && unitRec.general_icon_file_name[0] != ' ')
			{
				bitmapOffset = BitConverter.ToInt32(unitRec.general_icon_ptr, 0);
				byte[] generalIconData = res_general_icon.Read(bitmapOffset);
				unitInfo.generalIconWidth = BitConverter.ToInt16(generalIconData, 0);
				unitInfo.generalIconHeight = BitConverter.ToInt16(generalIconData, 2);
				unitInfo.generalIcon = generalIconData.Skip(4).ToArray();
			}
			else
			{
				unitInfo.generalIcon = soldierIconData.Skip(4).ToArray();
			}

			if (unitRec.king_icon_file_name[0] != '\0' && unitRec.king_icon_file_name[0] != ' ')
			{
				bitmapOffset = BitConverter.ToInt32(unitRec.king_icon_ptr, 0);
				byte[] kingIconData = res_king_icon.Read(bitmapOffset);
				unitInfo.kingIconWidth = BitConverter.ToInt16(kingIconData, 0);
				unitInfo.kingIconHeight = BitConverter.ToInt16(kingIconData, 2);
				unitInfo.kingIcon = kingIconData.Skip(4).ToArray();
			}
			else
			{
				unitInfo.kingIcon = soldierIconData.Skip(4).ToArray();
			}

			bitmapOffset = BitConverter.ToInt32(unitRec.small_icon_ptr, 0);
			byte[] soldierSmallIconData = res_small_icon.Read(bitmapOffset);
			unitInfo.soldierSmallIconWidth = BitConverter.ToInt16(soldierSmallIconData, 0);
			unitInfo.soldierSmallIconHeight = BitConverter.ToInt16(soldierSmallIconData, 2);
			unitInfo.soldierSmallIcon = soldierSmallIconData.Skip(4).ToArray();

			if (unitRec.general_small_icon_file_name[0] != '\0' && unitRec.general_small_icon_file_name[0] != ' ')
			{
				bitmapOffset = BitConverter.ToInt32(unitRec.general_small_icon_ptr, 0);
				byte[] generalSmallIconData = res_general_small_icon.Read(bitmapOffset);
				unitInfo.generalSmallIconWidth = BitConverter.ToInt16(generalSmallIconData, 0);
				unitInfo.generalSmallIconHeight = BitConverter.ToInt16(generalSmallIconData, 2);
				unitInfo.generalSmallIcon = generalSmallIconData.Skip(4).ToArray();
			}
			else
			{
				unitInfo.generalSmallIcon = soldierSmallIconData.Skip(4).ToArray();
			}

			if (unitRec.king_small_icon_file_name[0] != '\0' && unitRec.king_small_icon_file_name[0] != ' ')
			{
				bitmapOffset = BitConverter.ToInt32(unitRec.king_small_icon_ptr, 0);
				byte[] kingSmallIconData = res_king_small_icon.Read(bitmapOffset);
				unitInfo.kingSmallIconWidth = BitConverter.ToInt16(kingSmallIconData, 0);
				unitInfo.kingSmallIconHeight = BitConverter.ToInt16(kingSmallIconData, 2);
				unitInfo.kingSmallIcon = kingSmallIconData.Skip(4).ToArray();
			}
			else
			{
				unitInfo.kingSmallIcon = soldierSmallIconData.Skip(4).ToArray();
			}

			unitInfo.first_attack = Misc.ToInt32(unitRec.first_attack);
			unitInfo.attack_count = Misc.ToInt32(unitRec.attack_count);
			unitInfo.die_effect_id = Misc.ToInt32(unitRec.die_effect_id);

			if (unitRec.all_know == '1')
			{
				for (int j = 0; j < unitInfo.nation_tech_level_array.Length; j++)
				{
					unitInfo.nation_tech_level_array[j] = 1;
				}
			}
		}

		//--------- set vehicle info  ---------//

		for (int i = 0; i < unit_info_array.Length; i++)
		{
			UnitInfo unitInfo = unit_info_array[i];

			if (unitInfo.vehicle_unit_id != 0)
			{
				unit_info_array[unitInfo.vehicle_unit_id - 1].vehicle_id = unitInfo.vehicle_id;
				unit_info_array[unitInfo.vehicle_unit_id - 1].solider_id = i + 1;
			}
		}
	}

	private void LoadAttackInfo()
	{
		Database dbUnitAttack = GameSet.OpenDb(UNIT_ATTACK_DB);
		attack_info_array = new AttackInfo[dbUnitAttack.RecordCount];

		for (int i = 0; i < attack_info_array.Length; i++)
		{
			UnitAttackRec attackRec = new UnitAttackRec(dbUnitAttack.Read(i + 1));
			AttackInfo attackInfo = new AttackInfo();
			attack_info_array[i] = attackInfo;

			attackInfo.combat_level = Misc.ToInt32(attackRec.combat_level);
			attackInfo.attack_delay = Misc.ToInt32(attackRec.attack_delay);
			attackInfo.attack_range = Misc.ToInt32(attackRec.attack_range);
			attackInfo.attack_damage = Misc.ToInt32(attackRec.attack_damage);
			attackInfo.pierce_damage = Misc.ToInt32(attackRec.pierce_damage);
			attackInfo.bullet_out_frame = Misc.ToInt32(attackRec.bullet_out_frame);
			attackInfo.bullet_speed = Misc.ToInt32(attackRec.bullet_speed);
			attackInfo.bullet_radius = Misc.ToInt32(attackRec.bullet_radius);
			attackInfo.bullet_sprite_id = Misc.ToInt32(attackRec.bullet_sprite_id);
			attackInfo.dll_bullet_sprite_id = Misc.ToInt32(attackRec.dll_bullet_sprite_id);
			attackInfo.eqv_attack_next = Misc.ToInt32(attackRec.eqv_attack_next);
			attackInfo.min_power = Misc.ToInt32(attackRec.min_power);
			attackInfo.consume_power = Misc.ToInt32(attackRec.consume_power);
			attackInfo.fire_radius = Misc.ToInt32(attackRec.fire_radius);
			attackInfo.effect_id = Misc.ToInt32(attackRec.effect_id);
		}
	}
}