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

	public UnitRec(Database db, int recNo)
	{
		int dataIndex = 0;
		for (int i = 0; i < name.Length; i++, dataIndex++)
			name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < sprite_code.Length; i++, dataIndex++)
			sprite_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < race_code.Length; i++, dataIndex++)
			race_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < unit_class.Length; i++, dataIndex++)
			unit_class[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		mobile_type = db.ReadByte(recNo, dataIndex);
		dataIndex++;
		all_know = db.ReadByte(recNo, dataIndex);
		dataIndex++;

		for (int i = 0; i < visual_range.Length; i++, dataIndex++)
			visual_range[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < visual_extend.Length; i++, dataIndex++)
			visual_extend[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < stealth.Length; i++, dataIndex++)
			stealth[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < hit_points.Length; i++, dataIndex++)
			hit_points[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < armor.Length; i++, dataIndex++)
			armor[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < build_days.Length; i++, dataIndex++)
			build_days[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < year_cost.Length; i++, dataIndex++)
			year_cost[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		weapon_power = db.ReadByte(recNo, dataIndex);
		dataIndex++;

		for (int i = 0; i < carry_unit_capacity.Length; i++, dataIndex++)
			carry_unit_capacity[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < carry_goods_capacity.Length; i++, dataIndex++)
			carry_goods_capacity[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < free_weapon_count.Length; i++, dataIndex++)
			free_weapon_count[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < vehicle_code.Length; i++, dataIndex++)
			vehicle_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < vehicle_unit_code.Length; i++, dataIndex++)
			vehicle_unit_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < transform_unit.Length; i++, dataIndex++)
			transform_unit[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < transform_combat_level.Length; i++, dataIndex++)
			transform_combat_level[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < guard_combat_level.Length; i++, dataIndex++)
			guard_combat_level[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < large_icon_file_name.Length; i++, dataIndex++)
			large_icon_file_name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < large_icon_ptr.Length; i++, dataIndex++)
			large_icon_ptr[i] = db.ReadByte(recNo, dataIndex);

		for (int i = 0; i < general_icon_file_name.Length; i++, dataIndex++)
			general_icon_file_name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < general_icon_ptr.Length; i++, dataIndex++)
			general_icon_ptr[i] = db.ReadByte(recNo, dataIndex);

		for (int i = 0; i < king_icon_file_name.Length; i++, dataIndex++)
			king_icon_file_name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < king_icon_ptr.Length; i++, dataIndex++)
			king_icon_ptr[i] = db.ReadByte(recNo, dataIndex);

		for (int i = 0; i < small_icon_file_name.Length; i++, dataIndex++)
			small_icon_file_name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < small_icon_ptr.Length; i++, dataIndex++)
			small_icon_ptr[i] = db.ReadByte(recNo, dataIndex);

		for (int i = 0; i < general_small_icon_file_name.Length; i++, dataIndex++)
			general_small_icon_file_name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < general_small_icon_ptr.Length; i++, dataIndex++)
			general_small_icon_ptr[i] = db.ReadByte(recNo, dataIndex);

		for (int i = 0; i < king_small_icon_file_name.Length; i++, dataIndex++)
			king_small_icon_file_name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < king_small_icon_ptr.Length; i++, dataIndex++)
			king_small_icon_ptr[i] = db.ReadByte(recNo, dataIndex);

		for (int i = 0; i < die_effect_sprite.Length; i++, dataIndex++)
			die_effect_sprite[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < sprite_id.Length; i++, dataIndex++)
			sprite_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < dll_sprite_id.Length; i++, dataIndex++)
			dll_sprite_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < race_id.Length; i++, dataIndex++)
			race_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < vehicle_id.Length; i++, dataIndex++)
			vehicle_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < vehicle_unit_id.Length; i++, dataIndex++)
			vehicle_unit_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < transform_unit_id.Length; i++, dataIndex++)
			transform_unit_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < die_effect_id.Length; i++, dataIndex++)
			die_effect_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < first_attack.Length; i++, dataIndex++)
			first_attack[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < attack_count.Length; i++, dataIndex++)
			attack_count[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
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

	public UnitAttackRec(Database db, int recNo)
	{
		int dataIndex = 0;
		for (int i = 0; i < sprite_code.Length; i++, dataIndex++)
			sprite_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < attack_id.Length; i++, dataIndex++)
			attack_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < combat_level.Length; i++, dataIndex++)
			combat_level[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < attack_delay.Length; i++, dataIndex++)
			attack_delay[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < attack_range.Length; i++, dataIndex++)
			attack_range[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < attack_damage.Length; i++, dataIndex++)
			attack_damage[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < pierce_damage.Length; i++, dataIndex++)
			pierce_damage[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < bullet_out_frame.Length; i++, dataIndex++)
			bullet_out_frame[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < bullet_speed.Length; i++, dataIndex++)
			bullet_speed[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < bullet_radius.Length; i++, dataIndex++)
			bullet_radius[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < bullet_sprite_code.Length; i++, dataIndex++)
			bullet_sprite_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < bullet_sprite_id.Length; i++, dataIndex++)
			bullet_sprite_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < dll_bullet_sprite_id.Length; i++, dataIndex++)
			dll_bullet_sprite_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < eqv_attack_next.Length; i++, dataIndex++)
			eqv_attack_next[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < min_power.Length; i++, dataIndex++)
			min_power[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < consume_power.Length; i++, dataIndex++)
			consume_power[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < fire_radius.Length; i++, dataIndex++)
			fire_radius[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < effect_code.Length; i++, dataIndex++)
			effect_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

		for (int i = 0; i < effect_id.Length; i++, dataIndex++)
			effect_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
	}
}

public class UnitInfo
{
	public string Name { get; set; }

	public int UnitId { get; set; }
	public int SpriteId { get; set; }
	public int DllSpriteId { get; set; }
	public int RaceId { get; set; }
	public char UnitClass { get; set; }

	public int MobileType { get; set; }

	public int VisualRange { get; set; }
	public int VisualExtend { get; set; }
	public int Stealth { get; set; }
	public int Armor { get; set; }

	public int HitPoints { get; set; }

	public int BuildDays { get; set; }
	public int BuildCost { get; set; }
	public int YearCost { get; set; }

	public int WeaponPower { get; set; } // an index from 1 to 9 indicating the powerfulness of the weapon

	public int CarryUnitCapacity { get; set; }
	public int CarryGoodsCapacity { get; set; }
	public int FreeWeaponCount { get; set; } // only for ships. It's the no. of free weapons can be loaded onto the ship

	public int VehicleId { get; set; }
	public int VehicleUnitId { get; set; }
	public int SoliderId { get; set; }

	public int TransformUnitId { get; set; }
	public int TransformCombatLevel { get; set; }
	public int GuardCombatLevel { get; set; }

	public int FirstAttack { get; set; }
	public int AttackCount { get; set; }
	public int DieEffectId { get; set; }

	public byte[] SoldierIcon { get; set; }
	public int SoldierIconWidth { get; set; }
	public int SoldierIconHeight { get; set; }
	private IntPtr _soldierIconTexture;
	public byte[] GeneralIcon { get; set; }
	public int GeneralIconWidth { get; set; }
	public int GeneralIconHeight { get; set; }
	private IntPtr _generalIconTexture;
	public byte[] KingIcon { get; set; }
	public int KingIconWidth { get; set; }
	public int KingIconHeight { get; set; }
	private IntPtr _kingIconTexture;

	public byte[] SoldierSmallIcon { get; set; }
	public int SoldierSmallIconWidth { get; set; }
	public int SoldierSmallIconHeight { get; set; }
	private IntPtr _soldierSmallIconTexture;
	public byte[] GeneralSmallIcon { get; set; }
	public int GeneralSmallIconWidth { get; set; }
	public int GeneralSmallIconHeight { get; set; }
	private IntPtr _generalSmallIconTexture;
	public byte[] KingSmallIcon { get; set; }
	public int KingSmallIconWidth { get; set; }
	public int KingSmallIconHeight { get; set; }
	private IntPtr _kingSmallIconTexture;

	public IntPtr GetSoldierIconTexture(Graphics graphics)
	{
		if (_soldierIconTexture == default)
			_soldierIconTexture = graphics.CreateTextureFromBmp(SoldierIcon, SoldierIconWidth, SoldierIconHeight);

		return _soldierIconTexture;
	}
	
	public IntPtr GetGeneralIconTexture(Graphics graphics)
	{
		if (_generalIconTexture == default)
			_generalIconTexture = graphics.CreateTextureFromBmp(GeneralIcon, GeneralIconWidth, GeneralIconHeight);

		return _generalIconTexture;
	}
	
	public IntPtr GetKingIconTexture(Graphics graphics)
	{
		if (_kingIconTexture == default)
			_kingIconTexture = graphics.CreateTextureFromBmp(KingIcon, KingIconWidth, KingIconHeight);

		return _kingIconTexture;
	}
	
	public IntPtr GetSoldierSmallIconTexture(Graphics graphics)
	{
		if (_soldierSmallIconTexture == default)
			_soldierSmallIconTexture = graphics.CreateTextureFromBmp(SoldierSmallIcon, SoldierSmallIconWidth, SoldierSmallIconHeight);

		return _soldierSmallIconTexture;
	}
	
	public IntPtr GetGeneralSmallIconTexture(Graphics graphics)
	{
		if (_generalSmallIconTexture == default)
			_generalSmallIconTexture = graphics.CreateTextureFromBmp(GeneralSmallIcon, GeneralSmallIconWidth, GeneralSmallIconHeight);

		return _generalSmallIconTexture;
	}

	public IntPtr GetKingSmallIconTexture(Graphics graphics)
	{
		if (_kingSmallIconTexture == default)
			_kingSmallIconTexture = graphics.CreateTextureFromBmp(KingSmallIcon, KingSmallIconWidth, KingSmallIconHeight);

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
}

public class AttackInfo
{
	public int CombatLevel { get; set; }

	public int AttackDelay { get; set; }
	public int AttackRange { get; set; }

	public int AttackDamage { get; set; }
	public int PierceDamage { get; set; }

	public int BulletOutFrame { get; set; } // on which attacking frames the bullet should be out
	public int BulletSpeed { get; set; }
	public int BulletRadius { get; set; }
	public int BulletSpriteId { get; set; }
	public int DllBulletSpriteId { get; set; }

	public int EqvAttackNext { get; set; }

	// cur_attack of the next equivalent attack
	// so as to cycle through several similar attacks
	public int MinPower { get; set; }
	public int ConsumePower { get; set; }
	public int FireRadius { get; set; }
	public int EffectId { get; set; }
}

public class UnitRes
{
	public UnitInfo[] UnitInfos { get; private set; }
	public AttackInfo[] AttackInfos { get; private set; }

	public GameSet GameSet { get; }
	
	public UnitRes(GameSet gameSet)
	{
		GameSet = gameSet;
		
		LoadInfo();
		LoadAttackInfo();
	}

	public UnitInfo this[int unitType] => UnitInfos[unitType - 1];

	public AttackInfo GetAttackInfo(int attackId)
	{
		return AttackInfos[attackId - 1];
	}

	public static int MobileTypeToMask(char mobileType)
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
		ResourceDb soldierLargeIconBitmaps = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_UNITLI.RES");
		ResourceDb generalLargeIconBitmaps = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_UNITGI.RES");
		ResourceDb kingLargeIconBitmaps = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_UNITKI.RES");
		ResourceDb soldierSmallIconBitmaps = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_UNITSI.RES");
		ResourceDb generalSmallIconBitmaps = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_UNITTI.RES");
		ResourceDb kingSmallIconBitmaps = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_UNITUI.RES");
		
		Database dbUnit = GameSet.OpenDb("UNIT");
		UnitInfos = new UnitInfo[dbUnit.RecordCount];

		for (int i = 0; i < UnitInfos.Length; i++)
		{
			UnitRec unitRec = new UnitRec(dbUnit, i + 1);
			UnitInfo unitInfo = new UnitInfo();
			UnitInfos[i] = unitInfo;

			unitInfo.Name = Misc.ToString(unitRec.name);

			unitInfo.UnitId = i + 1;
			unitInfo.SpriteId = Misc.ToInt32(unitRec.sprite_id);
			unitInfo.DllSpriteId = Misc.ToInt32(unitRec.dll_sprite_id);
			unitInfo.RaceId = Misc.ToInt32(unitRec.race_id);

			unitInfo.UnitClass = unitRec.unit_class[0];
			unitInfo.MobileType = unitRec.mobile_type;

			unitInfo.VisualRange = Misc.ToInt32(unitRec.visual_range);
			unitInfo.VisualExtend = Misc.ToInt32(unitRec.visual_extend);
			unitInfo.Stealth = Misc.ToInt32(unitRec.stealth);
			unitInfo.HitPoints = Misc.ToInt32(unitRec.hit_points);
			unitInfo.Armor = Misc.ToInt32(unitRec.armor);

			unitInfo.BuildDays = Misc.ToInt32(unitRec.build_days);
			unitInfo.YearCost = Misc.ToInt32(unitRec.year_cost);
			unitInfo.BuildCost = unitInfo.YearCost;

			if (unitInfo.UnitClass == UnitConstants.UNIT_CLASS_WEAPON)
				unitInfo.WeaponPower = unitRec.weapon_power - '0';

			unitInfo.CarryUnitCapacity = Misc.ToInt32(unitRec.carry_unit_capacity);
			unitInfo.CarryGoodsCapacity = Misc.ToInt32(unitRec.carry_goods_capacity);
			unitInfo.FreeWeaponCount = Misc.ToInt32(unitRec.free_weapon_count);

			unitInfo.VehicleId = Misc.ToInt32(unitRec.vehicle_id);
			unitInfo.VehicleUnitId = Misc.ToInt32(unitRec.vehicle_unit_id);

			unitInfo.TransformUnitId = Misc.ToInt32(unitRec.transform_unit_id);
			unitInfo.TransformCombatLevel = Misc.ToInt32(unitRec.transform_combat_level);
			unitInfo.GuardCombatLevel = Misc.ToInt32(unitRec.guard_combat_level);

			int bitmapOffset = BitConverter.ToInt32(unitRec.large_icon_ptr, 0);
			byte[] soldierIconData = soldierLargeIconBitmaps.Read(bitmapOffset);
			unitInfo.SoldierIconWidth = BitConverter.ToInt16(soldierIconData, 0);
			unitInfo.SoldierIconHeight = BitConverter.ToInt16(soldierIconData, 2);
			unitInfo.SoldierIcon = soldierIconData.Skip(4).ToArray();

			if (unitRec.general_icon_file_name[0] != '\0' && unitRec.general_icon_file_name[0] != ' ')
			{
				bitmapOffset = BitConverter.ToInt32(unitRec.general_icon_ptr, 0);
				byte[] generalIconData = generalLargeIconBitmaps.Read(bitmapOffset);
				unitInfo.GeneralIconWidth = BitConverter.ToInt16(generalIconData, 0);
				unitInfo.GeneralIconHeight = BitConverter.ToInt16(generalIconData, 2);
				unitInfo.GeneralIcon = generalIconData.Skip(4).ToArray();
			}
			else
			{
				unitInfo.GeneralIcon = soldierIconData.Skip(4).ToArray();
			}

			if (unitRec.king_icon_file_name[0] != '\0' && unitRec.king_icon_file_name[0] != ' ')
			{
				bitmapOffset = BitConverter.ToInt32(unitRec.king_icon_ptr, 0);
				byte[] kingIconData = kingLargeIconBitmaps.Read(bitmapOffset);
				unitInfo.KingIconWidth = BitConverter.ToInt16(kingIconData, 0);
				unitInfo.KingIconHeight = BitConverter.ToInt16(kingIconData, 2);
				unitInfo.KingIcon = kingIconData.Skip(4).ToArray();
			}
			else
			{
				unitInfo.KingIcon = soldierIconData.Skip(4).ToArray();
			}

			bitmapOffset = BitConverter.ToInt32(unitRec.small_icon_ptr, 0);
			byte[] soldierSmallIconData = soldierSmallIconBitmaps.Read(bitmapOffset);
			unitInfo.SoldierSmallIconWidth = BitConverter.ToInt16(soldierSmallIconData, 0);
			unitInfo.SoldierSmallIconHeight = BitConverter.ToInt16(soldierSmallIconData, 2);
			unitInfo.SoldierSmallIcon = soldierSmallIconData.Skip(4).ToArray();

			if (unitRec.general_small_icon_file_name[0] != '\0' && unitRec.general_small_icon_file_name[0] != ' ')
			{
				bitmapOffset = BitConverter.ToInt32(unitRec.general_small_icon_ptr, 0);
				byte[] generalSmallIconData = generalSmallIconBitmaps.Read(bitmapOffset);
				unitInfo.GeneralSmallIconWidth = BitConverter.ToInt16(generalSmallIconData, 0);
				unitInfo.GeneralSmallIconHeight = BitConverter.ToInt16(generalSmallIconData, 2);
				unitInfo.GeneralSmallIcon = generalSmallIconData.Skip(4).ToArray();
			}
			else
			{
				unitInfo.GeneralSmallIcon = soldierSmallIconData.Skip(4).ToArray();
			}

			if (unitRec.king_small_icon_file_name[0] != '\0' && unitRec.king_small_icon_file_name[0] != ' ')
			{
				bitmapOffset = BitConverter.ToInt32(unitRec.king_small_icon_ptr, 0);
				byte[] kingSmallIconData = kingSmallIconBitmaps.Read(bitmapOffset);
				unitInfo.KingSmallIconWidth = BitConverter.ToInt16(kingSmallIconData, 0);
				unitInfo.KingSmallIconHeight = BitConverter.ToInt16(kingSmallIconData, 2);
				unitInfo.KingSmallIcon = kingSmallIconData.Skip(4).ToArray();
			}
			else
			{
				unitInfo.KingSmallIcon = soldierSmallIconData.Skip(4).ToArray();
			}

			unitInfo.FirstAttack = Misc.ToInt32(unitRec.first_attack);
			unitInfo.AttackCount = Misc.ToInt32(unitRec.attack_count);
			unitInfo.DieEffectId = Misc.ToInt32(unitRec.die_effect_id);
		}

		//--------- set vehicle info  ---------//

		for (int i = 0; i < UnitInfos.Length; i++)
		{
			UnitInfo unitInfo = UnitInfos[i];

			if (unitInfo.VehicleUnitId != 0)
			{
				UnitInfos[unitInfo.VehicleUnitId - 1].VehicleId = unitInfo.VehicleId;
				UnitInfos[unitInfo.VehicleUnitId - 1].SoliderId = i + 1;
			}
		}
	}

	private void LoadAttackInfo()
	{
		Database dbUnitAttack = GameSet.OpenDb("UNITATTK");
		AttackInfos = new AttackInfo[dbUnitAttack.RecordCount];

		for (int i = 0; i < AttackInfos.Length; i++)
		{
			UnitAttackRec attackRec = new UnitAttackRec(dbUnitAttack, i + 1);
			AttackInfo attackInfo = new AttackInfo();
			AttackInfos[i] = attackInfo;

			attackInfo.CombatLevel = Misc.ToInt32(attackRec.combat_level);
			attackInfo.AttackDelay = Misc.ToInt32(attackRec.attack_delay);
			attackInfo.AttackRange = Misc.ToInt32(attackRec.attack_range);
			attackInfo.AttackDamage = Misc.ToInt32(attackRec.attack_damage);
			attackInfo.PierceDamage = Misc.ToInt32(attackRec.pierce_damage);
			attackInfo.BulletOutFrame = Misc.ToInt32(attackRec.bullet_out_frame);
			attackInfo.BulletSpeed = Misc.ToInt32(attackRec.bullet_speed);
			attackInfo.BulletRadius = Misc.ToInt32(attackRec.bullet_radius);
			attackInfo.BulletSpriteId = Misc.ToInt32(attackRec.bullet_sprite_id);
			attackInfo.DllBulletSpriteId = Misc.ToInt32(attackRec.dll_bullet_sprite_id);
			attackInfo.EqvAttackNext = Misc.ToInt32(attackRec.eqv_attack_next);
			attackInfo.MinPower = Misc.ToInt32(attackRec.min_power);
			attackInfo.ConsumePower = Misc.ToInt32(attackRec.consume_power);
			attackInfo.FireRadius = Misc.ToInt32(attackRec.fire_radius);
			attackInfo.EffectId = Misc.ToInt32(attackRec.effect_id);
		}
	}
}