using System;
using System.Linq;

namespace TenKingdoms;

public static class TerrainTypeCode
{
    public const byte TERRAIN_OCEAN = 1; // abbrev = 'S'
    public const byte TERRAIN_DARK_GRASS = 2; // 'G'
    public const byte TERRAIN_LIGHT_GRASS = 3; // 'F'
    public const byte TERRAIN_DARK_DIRT = 4; // 'D' - hill
}

[Flags]
public enum SubTerrainMask
{
    ZERO_MASK = 0,
    BOTTOM_MASK = 1, // abbrev = '-'
    MIDDLE_MASK = 2, // '0'
    TOP_MASK = 4, // '+'
    NOT_TOP_MASK = BOTTOM_MASK | MIDDLE_MASK, // 'B'
    NOT_BOTTOM_MASK = MIDDLE_MASK | TOP_MASK, // 'A'
    ALL_MASK = BOTTOM_MASK | MIDDLE_MASK | TOP_MASK // '*'
}

[Flags]
public enum TerrainFlag
{
    TERRAIN_FLAG_NONE = 0,
    TERRAIN_FLAG_SNOW = 1
}

public class TerrainType
{
    public int first_terrain_id;
    public int last_terrain_id;
    public int min_height;
}

public class TerrainRec
{
    public const int TYPE_CODE_LEN = 2;
    public const int FILE_NAME_LEN = 8;
    public const int BITMAP_PTR_LEN = 4;
    public const int PATTERN_ID_LEN = 2;

    public char[] nw_type_code = new char[TYPE_CODE_LEN];
    public char[] ne_type_code = new char[TYPE_CODE_LEN];
    public char[] sw_type_code = new char[TYPE_CODE_LEN];
    public char[] se_type_code = new char[TYPE_CODE_LEN];

    public byte extra_flag;
    public byte special_flag;
    public char represent_type;
    public char secondary_type;
    public char[] pattern_id = new char[PATTERN_ID_LEN];

    public char[] file_name = new char[FILE_NAME_LEN];
    public byte[] bitmap_ptr = new byte[BITMAP_PTR_LEN];

    public TerrainRec(byte[] data)
    {
        int dataIndex = 0;
        for (int i = 0; i < nw_type_code.Length; i++, dataIndex++)
            nw_type_code[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < ne_type_code.Length; i++, dataIndex++)
            ne_type_code[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < sw_type_code.Length; i++, dataIndex++)
            sw_type_code[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < se_type_code.Length; i++, dataIndex++)
            se_type_code[i] = Convert.ToChar(data[dataIndex]);

        extra_flag = data[dataIndex];
        dataIndex++;
        special_flag = data[dataIndex];
        dataIndex++;
        represent_type = Convert.ToChar(data[dataIndex]);
        dataIndex++;
        secondary_type = Convert.ToChar(data[dataIndex]);
        dataIndex++;

        for (int i = 0; i < pattern_id.Length; i++, dataIndex++)
            pattern_id[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < file_name.Length; i++, dataIndex++)
            file_name[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < bitmap_ptr.Length; i++, dataIndex++)
            bitmap_ptr[i] = data[dataIndex];
    }
}

public class TerrainInfo
{
    public int nw_type, nw_subtype;
    public int ne_type, ne_subtype;
    public int sw_type, sw_subtype;
    public int se_type, se_subtype;

    public int average_type;
    public byte extra_flag;
    public byte special_flag;
    public int secondary_type;
    public byte pattern_id;

    public int alternative_count_with_extra; // no. of alternative bitmaps of this terrain
    public int alternative_count_without_extra; // no. of alternative bitmaps of this terrain
    public TerrainFlag flags;

    public byte[] bitmap;
    public int bitmapWidth;
    public int bitmapHeight;
    private IntPtr texture;
    public int anim_frames;
    public byte[][] anim_bitmap_ptr;

    public IntPtr GetTexture(Graphics graphics)
    {
	    if (texture == default)
		    texture = graphics.CreateTextureFromBmp(bitmap, bitmapWidth, bitmapHeight);

	    return texture;
    }

    public byte[] get_bitmap(int frameNo)
    {
        return (frameNo > 0 && anim_frames > 0) ? anim_bitmap_ptr[frameNo % anim_frames] : null;
    }

    public bool is_coast()
    {
	    return average_type == TerrainTypeCode.TERRAIN_OCEAN && secondary_type > TerrainTypeCode.TERRAIN_OCEAN ||
	           average_type > TerrainTypeCode.TERRAIN_OCEAN && secondary_type == TerrainTypeCode.TERRAIN_OCEAN;
    }

    public bool can_snow()
    {
        return (flags & TerrainFlag.TERRAIN_FLAG_SNOW) != TerrainFlag.TERRAIN_FLAG_NONE;
    }
}

public class TerrainSubRec
{
    public const int SUB_NO_LEN = 4;
    public const int PATTERN_ID_LEN = 2;
    public const int DIRECTION_LEN = 2;
    public const int STEP_ID_LEN = 2;
    public const int SEC_ADJ_LEN = 2;

    public char[] sub_no = new char[SUB_NO_LEN];
    public char[] step_id = new char[STEP_ID_LEN];
    public char[] old_pattern_id = new char[PATTERN_ID_LEN];
    public char[] new_pattern_id = new char[PATTERN_ID_LEN];
    public char[] sec_adj = new char[SEC_ADJ_LEN];
    public char[] post_move = new char[DIRECTION_LEN];

    public TerrainSubRec(byte[] data)
    {
        int dataIndex = 0;
        for (int i = 0; i < sub_no.Length; i++, dataIndex++)
            sub_no[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < step_id.Length; i++, dataIndex++)
            step_id[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < old_pattern_id.Length; i++, dataIndex++)
            old_pattern_id[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < new_pattern_id.Length; i++, dataIndex++)
            new_pattern_id[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < sec_adj.Length; i++, dataIndex++)
            sec_adj[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < post_move.Length; i++, dataIndex++)
            post_move[i] = Convert.ToChar(data[dataIndex]);
    }
}

public class TerrainSubInfo
{
    public int sub_no;
    public int step_id;
    public byte old_pattern_id;
    public byte new_pattern_id;
    public sbyte sec_adj;
    public sbyte post_move;
    public TerrainSubInfo next_step;
}

public class TerrainAnimRec
{
    public const int FILE_NAME_LEN = 8;
    public const int FRAME_NO_LEN = 2;
    public const int BITMAP_PTR_LEN = 4;

    public char[] base_file = new char[FILE_NAME_LEN];
    public char[] frame_no = new char[FRAME_NO_LEN];
    public char[] next_frame = new char[FRAME_NO_LEN];
    public char[] filename = new char[FILE_NAME_LEN];
    public byte[] bitmap_ptr = new byte[BITMAP_PTR_LEN];

    public TerrainAnimRec()
    {
    }
    public TerrainAnimRec(byte[] data)
    {
        int dataIndex = 0;
        for (int i = 0; i < base_file.Length; i++, dataIndex++)
            base_file[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < frame_no.Length; i++, dataIndex++)
            frame_no[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < next_frame.Length; i++, dataIndex++)
            next_frame[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < filename.Length; i++, dataIndex++)
            filename[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < bitmap_ptr.Length; i++, dataIndex++)
            bitmap_ptr[i] = data[dataIndex];
    }
}

public class TerrainRes
{
	public const int TOTAL_TERRAIN_TYPE = 4;
	public const int MAX_GEN_TERRAIN_TYPE = 4;
	public const int TERRAIN_TILE_WIDTH = 64;
	public const int TERRAIN_TILE_HEIGHT = 64;
	public const int TERRAIN_TILE_X_MASK = 63;
	public const int TERRAIN_TILE_Y_MASK = 63;
	public const int MAX_TERRAIN_ANIM_FRAME = 16;

	public int terrain_count;
	public TerrainInfo[] terrain_info_array;
	public char[] file_name_array;

	public TerrainType[] terrain_type_array = new TerrainType[TOTAL_TERRAIN_TYPE];
	public int[] nw_type_min = new int[TOTAL_TERRAIN_TYPE];
	public int[] nw_type_max = new int[TOTAL_TERRAIN_TYPE];

	//----- field related to terrain pattern substitution -----//
	public int ter_sub_rec_count;
	public int ter_sub_index_count;
	public TerrainSubInfo[] ter_sub_array;
	public TerrainSubInfo[] ter_sub_index;

	private static int[] terrain_type_color_array = new int[TOTAL_TERRAIN_TYPE] { 0x20, 0x0A, 0x0D, 0x2D };

	private static string[] map_tile_name_array = new string[TOTAL_TERRAIN_TYPE]
		{ "TERA_S", "TERA_DG", "TERA_LG", "TERA_D" };

	private static byte[][] map_tile_ptr_array = new byte[TOTAL_TERRAIN_TYPE][];

	private static int[,] terrain_type_min_height_array = new int[TOTAL_TERRAIN_TYPE, 3]
	{
		{ 0, 20, 40 }, // S-, S0, S+
		{ 60, 80, 110 }, // G-, G0, G+
		{ 130, 150, 180 }, // F-, F0, F+
		{ 200, 215, 240 }, // D-, D0, D+
	};

	public ResourceDb res_bitmap;
	public ResourceDb anm_bitmap;

	public TerrainRes()
	{
		res_bitmap = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_TERN{Sys.Instance.Config.terrain_set}.RES");

		LoadInfo();
		LoadSubInfo();

		anm_bitmap = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_TERA{Sys.Instance.Config.terrain_set}.RES");

		LoadAnimInfo();

		//-------- init map_tile_ptr_array --------//
		ResourceIdx image_tpict = new ResourceIdx($"{Sys.GameDataFolder}/Resource/I_TPICT{Sys.Instance.Config.terrain_set}.RES");
		for (int i = 0; i < TOTAL_TERRAIN_TYPE; i++)
		{
			if (!String.IsNullOrEmpty(map_tile_name_array[i]))
			{
				map_tile_ptr_array[i] = image_tpict.Read(map_tile_name_array[i]).Skip(4).ToArray();
			}
		}

		//-------- init terrain_type_array ---------//

		for (int i = 1; i <= TOTAL_TERRAIN_TYPE; i++)
		{
			terrain_type_array[i - 1] = new TerrainType();
			// scan for terrain of all four types. 1-take the first instance of the terrain bitmap that matches the given terrain types
			int terrainId = scan(i, (int)SubTerrainMask.ALL_MASK, i, (int)SubTerrainMask.ALL_MASK,
				i, (int)SubTerrainMask.ALL_MASK, i, (int)SubTerrainMask.ALL_MASK, 1);

			if (terrainId != 0)
			{
				terrain_type_array[i - 1].first_terrain_id = terrainId;
				terrain_type_array[i - 1].last_terrain_id =
					terrainId + this[terrainId].alternative_count_with_extra;
				terrain_type_array[i - 1].min_height = terrain_type_min_height_array[i - 1, 0];
			}
			else
			{
				// BUGHERE: should not reach this point after all terrain type are complete
				terrain_type_array[i - 1].first_terrain_id = 0;
				terrain_type_array[i - 1].last_terrain_id = 0;
				terrain_type_array[i - 1].min_height = terrain_type_min_height_array[i - 1, 0];
			}
		}
	}
	
	public TerrainInfo this[int terrainId] => terrain_info_array[terrainId - 1];

	private void LoadInfo()
	{
		TerrainInfo terrainInfo = null;
		int i = 0;

		//---- read in terrain count and initialize terrain info array ----//

		string terrainDbName = $"{Sys.GameDataFolder}/Resource/TERRAIN{Sys.Instance.Config.terrain_set}.RES";
		Database dbTerrain = new Database(terrainDbName);

		terrain_count = dbTerrain.RecordCount;
		terrain_info_array = new TerrainInfo[terrain_count];
		for (i = 0; i < terrain_info_array.Length; i++)
			terrain_info_array[i] = new TerrainInfo();

		file_name_array = new char[TerrainRec.FILE_NAME_LEN * terrain_count];

		// ------ initial nw_type_index -------//
		for (i = 0; i < TOTAL_TERRAIN_TYPE; ++i)
		{
			nw_type_min[i] = 0;
			nw_type_max[i] = 0;
		}

		//---------- read in TERRAIN.DBF ---------//

		int firstNw = 0, firstNe = 0, firstSw = 0, firstSe = 0;
		int firstNwSub = 0, firstNeSub = 0, firstSwSub = 0, firstSeSub = 0, firstSpFlag = 0;
		int firstId = 0;

		for (i = 0; i < terrain_count; i++)
		{
			TerrainRec terrainRec = new TerrainRec(dbTerrain.Read(i + 1));
			terrainInfo = terrain_info_array[i];

			for (int j = 0; j < TerrainRec.FILE_NAME_LEN; j++)
			{
				file_name_array[i * TerrainRec.FILE_NAME_LEN + j] = terrainRec.file_name[j];
			}

			terrainInfo.nw_type = terrain_code(terrainRec.nw_type_code[0]);
			terrainInfo.nw_subtype = (int)terrain_mask(terrainRec.nw_type_code[1]);
			terrainInfo.ne_type = terrain_code(terrainRec.ne_type_code[0]);
			terrainInfo.ne_subtype = (int)terrain_mask(terrainRec.ne_type_code[1]);
			terrainInfo.sw_type = terrain_code(terrainRec.sw_type_code[0]);
			terrainInfo.sw_subtype = (int)terrain_mask(terrainRec.sw_type_code[1]);
			terrainInfo.se_type = terrain_code(terrainRec.se_type_code[0]);
			terrainInfo.se_subtype = (int)terrain_mask(terrainRec.se_type_code[1]);

			terrainInfo.average_type = terrain_code(terrainRec.represent_type);
			char extraFlagChar = Convert.ToChar(terrainRec.extra_flag);
			if (terrainRec.extra_flag == 0 || extraFlagChar == ' ' || extraFlagChar == 'N' || extraFlagChar == 'n')
				terrainInfo.extra_flag = 0;
			else
				terrainInfo.extra_flag = 1;
			terrainInfo.special_flag = Convert.ToChar(terrainRec.special_flag) == ' ' ? (byte)0 : terrainRec.special_flag;

			terrainInfo.secondary_type = terrain_code(terrainRec.secondary_type);
			terrainInfo.pattern_id = Convert.ToByte(new string(terrainRec.pattern_id));

			//------ set alternative_count ---------//

			if (firstNw == terrainInfo.nw_type && firstNwSub == terrainInfo.nw_subtype &&
			    firstNe == terrainInfo.ne_type && firstNeSub == terrainInfo.ne_subtype &&
			    firstSw == terrainInfo.sw_type && firstSwSub == terrainInfo.sw_subtype &&
			    firstSe == terrainInfo.se_type && firstSeSub == terrainInfo.se_subtype &&
			    firstSpFlag == terrainInfo.special_flag)
			{
				terrain_info_array[firstId - 1].alternative_count_with_extra++;

				if (terrainInfo.extra_flag == 0)
					terrain_info_array[firstId - 1].alternative_count_without_extra++;

				// ----- record firstId - terrain_id -------//
				terrainInfo.alternative_count_with_extra = firstId - 1 - i;
				terrainInfo.alternative_count_without_extra = firstId - 1 - i;
			}
			else
			{
				// build index on nw_type
				if (firstNw != terrainInfo.nw_type)
				{
					// --------- mark end of previous nw_type group ---------//
					if (firstNw > 0)
						nw_type_max[firstNw - 1] = i;

					// --------- mark start of new nw_type group -----------//
					nw_type_min[terrainInfo.nw_type - 1] = i + 1;
				}

				firstNw = terrainInfo.nw_type;
				firstNe = terrainInfo.ne_type;
				firstSw = terrainInfo.sw_type;
				firstSe = terrainInfo.se_type;
				firstNwSub = terrainInfo.nw_subtype;
				firstNeSub = terrainInfo.ne_subtype;
				firstSwSub = terrainInfo.sw_subtype;
				firstSeSub = terrainInfo.se_subtype;
				firstSpFlag = terrainInfo.special_flag;
				firstId = i + 1;
			}

			//---- get the bitmap pointer of the terrain icon in res_icon ----//

			int bitmapOffset = BitConverter.ToInt32(terrainRec.bitmap_ptr, 0);
			terrainInfo.bitmap = res_bitmap.Read(bitmapOffset);
			terrainInfo.bitmapWidth = BitConverter.ToInt16(terrainInfo.bitmap, 0);
			terrainInfo.bitmapHeight = BitConverter.ToInt16(terrainInfo.bitmap, 2);
			terrainInfo.bitmap = terrainInfo.bitmap.Skip(4).ToArray();

			terrainInfo.anim_frames = 0;
			terrainInfo.anim_bitmap_ptr = null;

			terrainInfo.flags = TerrainFlag.TERRAIN_FLAG_NONE;
			if (terrainInfo.average_type != TerrainTypeCode.TERRAIN_OCEAN &&
			    terrainInfo.secondary_type != TerrainTypeCode.TERRAIN_OCEAN)
			{
				terrainInfo.flags |= TerrainFlag.TERRAIN_FLAG_SNOW;
			}

		}

		// ------- mark end of last nw_type group -------//
		if (terrainInfo != null && terrainInfo.nw_type > 0)
			nw_type_max[terrainInfo.nw_type - 1] = i;
	}

	private void LoadSubInfo()
	{
		//---- read in terrain count and initialize terrain info array ----//

		Database dbTerrain = new Database($"{Sys.GameDataFolder}/Resource/TERSUB.RES");

		ter_sub_rec_count = dbTerrain.RecordCount;
		ter_sub_array = new TerrainSubInfo[ter_sub_rec_count];
		for (int i = 0; i < ter_sub_array.Length; i++)
			ter_sub_array[i] = new TerrainSubInfo();

		//---------- read in TERSUB.DBF ---------//
		int maxSubNo = 0;

		for (int i = 0; i < ter_sub_rec_count; i++)
		{
			TerrainSubRec terrainSubRec = new TerrainSubRec(dbTerrain.Read(i + 1));
			TerrainSubInfo terrainSubInfo = ter_sub_array[i];

			terrainSubInfo.sub_no = Convert.ToInt16(new string(terrainSubRec.sub_no));
			terrainSubInfo.step_id = Convert.ToInt16(new string(terrainSubRec.step_id));
			terrainSubInfo.old_pattern_id = Convert.ToByte(new string(terrainSubRec.old_pattern_id));
			terrainSubInfo.new_pattern_id = Convert.ToByte(new string(terrainSubRec.new_pattern_id));

			// sec_adj is useful when a pure type is changing to boundary type.
			// eg. a GG square can be changed to SG or GS sqare
			terrainSubInfo.sec_adj = Convert.ToSByte(new string(terrainSubRec.sec_adj));

			switch (terrainSubRec.post_move[0])
			{
				case 'N':
					switch (terrainSubRec.post_move[1])
					{
						case 'E':
							terrainSubInfo.post_move = 2;
							break;
						case 'W':
							terrainSubInfo.post_move = 8;
							break;
						default:
							terrainSubInfo.post_move = 1;
							break;
					}
					break;
				case 'S':
					switch (terrainSubRec.post_move[1])
					{
						case 'E':
							terrainSubInfo.post_move = 4;
							break;
						case 'W':
							terrainSubInfo.post_move = 6;
							break;
						default:
							terrainSubInfo.post_move = 5;
							break;
					}
					break;
				case 'E':
					terrainSubInfo.post_move = 3;
					break;
				case 'W':
					terrainSubInfo.post_move = 7;
					break;
				case 'X':
					terrainSubInfo.post_move = 0;
					break;
			}

			terrainSubInfo.next_step = null;

			if (terrainSubInfo.sub_no > maxSubNo)
				maxSubNo = terrainSubInfo.sub_no;
		}

		// ------- build ter_sub_index ------//
		ter_sub_index_count = maxSubNo;
		ter_sub_index = new TerrainSubInfo[ter_sub_index_count];

		TerrainSubInfo lastTerrainSubInfo = null;
		for (int i = 0; i < ter_sub_rec_count; i++)
		{
			TerrainSubInfo terrainSubInfo = ter_sub_array[i];
			if (terrainSubInfo.step_id == 1)
			{
				ter_sub_index[terrainSubInfo.sub_no - 1] = terrainSubInfo;
			}
			else
			{
				// link from the next_step pointer of previous step
				// search the previous record first
				if (lastTerrainSubInfo != null && lastTerrainSubInfo.sub_no == terrainSubInfo.sub_no
				                               && lastTerrainSubInfo.step_id == terrainSubInfo.step_id - 1)
				{
					lastTerrainSubInfo.next_step = terrainSubInfo;
				}
				else
				{
					// search from the array
					for (int j = 0; j < ter_sub_rec_count; j++)
					{
						if (ter_sub_array[j].sub_no == terrainSubInfo.sub_no &&
						    ter_sub_array[j].step_id == terrainSubInfo.step_id - 1)
						{
							ter_sub_array[j].next_step = terrainSubInfo;
							break;
						}
					}
				}
			}

			lastTerrainSubInfo = terrainSubInfo;
		}
	}

	private void LoadAnimInfo()
	{
		TerrainAnimRec lastAnimRec = new TerrainAnimRec();

		string terAnimDbName = $"{Sys.GameDataFolder}/Resource/TERANM{Sys.Instance.Config.terrain_set}.RES";
		Database dbTerAnim = new Database(terAnimDbName);

		int count = dbTerAnim.RecordCount;

		int animFrameCount = 0;
		byte[][] animFrameBitmap = new byte[MAX_TERRAIN_ANIM_FRAME][];
		for (int i = 0; i < lastAnimRec.filename.Length; i++)
		{
			lastAnimRec.filename[i] = ' ';
		}

		for (int i = 0; i < lastAnimRec.base_file.Length; i++)
		{
			lastAnimRec.base_file[i] = ' ';
		}

		//---------- read in TERANM.DBF -------//
		for (int i = 0; i < count; i++)
		{
			TerrainAnimRec terrainAnimRec = new TerrainAnimRec(dbTerAnim.Read(i + 1));

			int bitmapOffset = BitConverter.ToInt32(terrainAnimRec.bitmap_ptr, 0);
			byte[] bitmapPtr = anm_bitmap.Read(bitmapOffset);

			if (new string(terrainAnimRec.base_file) != new string(lastAnimRec.base_file))
			{
				// string not equal
				if (lastAnimRec.filename[0] != ' ' && animFrameCount > 0)
				{
					// replace terrainInfo->anim_frames and anim_frame_ptr
					// where the bitmap filename are the same
					int replaceCount = 0;
					for (int l = 0; l < terrain_count; l++)
					{
						bool equals = true;
						for (int index = 0; index < TerrainAnimRec.FILE_NAME_LEN; index++)
						{
							if (file_name_array[l * TerrainAnimRec.FILE_NAME_LEN + index] !=
							    lastAnimRec.base_file[index])
								equals = false;
						}

						if (equals)
						{
							TerrainInfo terrainInfo = terrain_info_array[l];
							//err_when(terrainInfo->anim_frames > 0);
							terrainInfo.anim_frames = animFrameCount;
							terrainInfo.anim_bitmap_ptr = new byte[animFrameCount][];
							for (int j = 0; j < animFrameCount; j++)
								terrainInfo.anim_bitmap_ptr[j] = animFrameBitmap[j];
							replaceCount++;
						}
					}
					/*
					for(int special = 0; special <= 1; ++special)
					{
						j = scan( terrain_code(lastAnimRec.average_type),
							terrain_code(lastAnimRec.secondary_type),
							misc.atoi(lastAnimRec.pattern_id, lastAnimRec.PATTERN_ID_LEN), 1,0,special);
						if( j > 0)
						{
							k = terrain_info_array[j-1].alternative_count_with_extra;
							for(l = j; l <= j+k; ++l)
							{
								if( memcmp(&file_name_array[terrainAnimRec.FILE_NAME_LEN *(l-1)],
									lastAnimRec.base_file, terrainAnimRec.FILE_NAME_LEN) == 0)
								{
									TerrainInfo *terrainInfo = terrain_info_array+j-1;
									err_when(terrainInfo->anim_frames > 0);
									terrainInfo->anim_frames = animFrameCount;
									terrainInfo->anim_bitmap_ptr = (char **) mem_add(sizeof(char *)*animFrameCount);
									memcpy(terrainInfo->anim_bitmap_ptr, animFrameBitmap, sizeof(char *)*animFrameCount);
									replaceCount++;
								}
							}
						}
					}
					*/
				}

				lastAnimRec = terrainAnimRec;
				animFrameCount = 0;
				for (int j = 0; j < animFrameBitmap.Length; j++)
					animFrameBitmap[j] = Array.Empty<byte>();
			}

			animFrameCount++;
			animFrameBitmap[Convert.ToInt32(new string(terrainAnimRec.frame_no)) - 1] = bitmapPtr;
		}

		if (lastAnimRec.filename[0] != ' ' && animFrameCount > 0)
		{
			// replace terrainInfo->anim_frames and anim_frame_ptr
			// where the bitmap filename are the same
			int replaceCount = 0;
			for (int l = 0; l < terrain_count; l++)
			{
				bool equals = true;
				for (int index = 0; index < TerrainAnimRec.FILE_NAME_LEN; index++)
				{
					if (file_name_array[l * TerrainAnimRec.FILE_NAME_LEN + index] !=
					    lastAnimRec.base_file[index])
						equals = false;
				}

				if (equals)
				{
					TerrainInfo terrainInfo = terrain_info_array[l];
					terrainInfo.anim_frames = animFrameCount;
					terrainInfo.anim_bitmap_ptr = new byte[animFrameCount][];
					for (int j = 0; j < animFrameCount; j++)
						terrainInfo.anim_bitmap_ptr[j] = animFrameBitmap[j];
					replaceCount++;
				}
			}
			/*
			for(int special = 0; special <= 1; ++special)
			{
				j = scan( terrain_code(lastAnimRec.average_type),
					terrain_code(lastAnimRec.secondary_type),
					misc.atoi(lastAnimRec.pattern_id, lastAnimRec.PATTERN_ID_LEN), 1,0,special);
				if( j > 0)
				{
					k = terrain_info_array[j-1].alternative_count_with_extra;
					for(l = j; l <= j+k; ++l)
					{
						if( memcmp(&file_name_array[terrainAnimRec.FILE_NAME_LEN *(l-1)],
							lastAnimRec.base_file, terrainAnimRec.FILE_NAME_LEN) == 0)
						{
							TerrainInfo *terrainInfo = terrain_info_array+j-1;
							err_when(terrainInfo->anim_frames > 0);
							terrainInfo->anim_frames = animFrameCount;
							terrainInfo->anim_bitmap_ptr = (char **) mem_add(sizeof(char *)*animFrameCount);
							memcpy(terrainInfo->anim_bitmap_ptr, animFrameBitmap, sizeof(char *)*animFrameCount);
							replaceCount++;
						}
					}
				}
			}
			*/
		}
	}

	public byte get_tera_type_id(char[] teraTypeCode)
	{
		return terrain_code(teraTypeCode[0]);
	}

	public byte[] get_map_tile(int terrainId)
	{
		TerrainInfo terrainInfo = this[terrainId];
		return map_tile_ptr_array[terrainInfo.average_type - 1];
	}

	public int scan(int nwType, int nwSubType, int neType, int neSubType,
		int swType, int swSubType, int seType, int seSubType,
		int firstInstance = 0, int includeExtra = 0, int special = 0)
	{
		int terrainId = nw_type_min[nwType - 1];
		int terrainIdMax = nw_type_max[nwType - 1];
		if (terrainId <= 0 || terrainIdMax <= 0)
			return 0;

		//	int firstTerrainId=0, instanceCount=0;
		int index = terrainId - 1;

		for (; terrainId <= terrainIdMax; terrainId++, index++)
		{
			TerrainInfo terrainInfo = terrain_info_array[index];
			if (terrainInfo.nw_type == nwType && (terrainInfo.nw_subtype & nwSubType) != 0 &&
			    terrainInfo.ne_type == neType && (terrainInfo.ne_subtype & neSubType) != 0 &&
			    terrainInfo.sw_type == swType && (terrainInfo.sw_subtype & swSubType) != 0 &&
			    terrainInfo.se_type == seType && (terrainInfo.se_subtype & seSubType) != 0 &&
			    terrainInfo.special_flag == special)
			{
				if (firstInstance != 0)
					return terrainId;

				if (includeExtra != 0)
				{
					return terrainId + Misc.Random(terrainInfo.alternative_count_with_extra + 1);
				}
				else
				{
					return terrainId + Misc.Random(terrainInfo.alternative_count_without_extra + 1);
				}
			}
			else
			{
				//------ skip tiles of the same type and special_flag
				terrainId += terrainInfo.alternative_count_with_extra;
				index += terrainInfo.alternative_count_with_extra;
			}
		}

		return 0;
	}

	public int scan(int primaryType, int secondaryType, int patternId, int firstInstance = 0, int includeExtra = 0,
		int special = 0)
	{
		// if patternId is zero, that means it requires a pure terrain
		if (patternId == 0)
			secondaryType = primaryType;

		// ----------- search 1, search the lower type -------//
		int terrainId = nw_type_min[Math.Min(primaryType, secondaryType) - 1];
		int terrainIdMax = nw_type_max[Math.Min(primaryType, secondaryType) - 1];
		// int firstTerrainId=0, instanceCount=0;

		int index = terrainId - 1;
		for (; terrainId <= terrainIdMax; terrainId++, index++)
		{
			TerrainInfo terrainInfo = terrain_info_array[index];
			if (((terrainInfo.average_type == primaryType && terrainInfo.secondary_type == secondaryType) ||
			     (terrainInfo.average_type == secondaryType && terrainInfo.secondary_type == primaryType)) &&
			    terrainInfo.pattern_id == patternId && terrainInfo.special_flag == special)
			{
				if (firstInstance != 0)
					return terrainId;

				if (includeExtra != 0)
				{
					return terrainId + Misc.Random(terrainInfo.alternative_count_with_extra + 1);
				}
				else
				{
					return terrainId + Misc.Random(terrainInfo.alternative_count_without_extra + 1);
				}
			}
			else
			{
				//------ skip tiles of the same type and special_flag
				terrainId += terrainInfo.alternative_count_with_extra;
				index += terrainInfo.alternative_count_with_extra;
			}
		}

		// ----------- search 2, search the higher type ---------//
		if (primaryType != secondaryType)
		{
			terrainId = nw_type_min[Math.Max(primaryType, secondaryType) - 1];
			terrainIdMax = nw_type_max[Math.Max(primaryType, secondaryType) - 1];
			// int firstTerrainId=0, instanceCount=0;

			index = terrainId - 1;
			for (; terrainId <= terrainIdMax; terrainId++, index++)
			{
				TerrainInfo terrainInfo = terrain_info_array[index];
				if (((terrainInfo.average_type == primaryType && terrainInfo.secondary_type == secondaryType) ||
				     (terrainInfo.average_type == secondaryType && terrainInfo.secondary_type == primaryType)) &&
				    terrainInfo.pattern_id == patternId && terrainInfo.special_flag == special)
				{
					if (firstInstance != 0)
						return terrainId;
						
					if (includeExtra != 0)
					{
						return terrainId + Misc.Random(terrainInfo.alternative_count_with_extra + 1);
					}
					else
					{
						return terrainId + Misc.Random(terrainInfo.alternative_count_without_extra + 1);
					}
				}
				else
				{
					//------ skip tiles of the same type and special_flag
					terrainId += terrainInfo.alternative_count_with_extra;
					index += terrainInfo.alternative_count_with_extra;
				}
			}
		}

		return 0;
	}

	//---- function related to terrain pattern substitution ----//

	public int search_pattern(int nwPatternId, TerrainSubInfo[] resultArray, int maxResult)
	{
		if (resultArray == null || maxResult == 0)
			return 0;

		int occur = 0;
		for (int i = 0; i < ter_sub_index_count; i++)
		{
			if (ter_sub_index[i] != null && nwPatternId == ter_sub_index[i].old_pattern_id)
			{
				resultArray[occur++] = ter_sub_index[i];
				if (occur >= maxResult)
					break;
			}
		}

		return occur;
	}

	//---------- define code conversion function -------//

	public static byte terrain_code(char tcCode)
	{
		switch (tcCode)
		{
			case 'S':
				return TerrainTypeCode.TERRAIN_OCEAN;
			case 'G':
				return TerrainTypeCode.TERRAIN_DARK_GRASS;
			case 'F':
				return TerrainTypeCode.TERRAIN_LIGHT_GRASS;
			case 'D':
				return TerrainTypeCode.TERRAIN_DARK_DIRT;
			default:
				return TerrainTypeCode.TERRAIN_OCEAN;
		}
	}

	public static SubTerrainMask terrain_mask(char subtc)
	{
		switch (subtc)
		{
			case '0':
				return SubTerrainMask.MIDDLE_MASK;
			case '+':
				return SubTerrainMask.TOP_MASK;
			case '-':
				return SubTerrainMask.BOTTOM_MASK;
			case 'A':
				return SubTerrainMask.NOT_BOTTOM_MASK;
			case 'B':
				return SubTerrainMask.NOT_TOP_MASK;
			case '*':
				return SubTerrainMask.ALL_MASK;
			default:
				return SubTerrainMask.MIDDLE_MASK;
		}
	}

	public static int terrain_height(int height, out int sub)
	{
		sub = 0;
		for (int tc = TerrainRes.TOTAL_TERRAIN_TYPE - 1; tc >= 0; tc--)
		{
			if (height >= terrain_type_min_height_array[tc, 0])
			{
				for (int subtc = 2; subtc >= 0; subtc--)
				{
					if (height >= terrain_type_min_height_array[tc, subtc])
					{
						sub = 1 << subtc;
						break;
					}
				}

				return tc + 1;
			}
		}

		return 0;
	}

	public static int min_height(int tc, SubTerrainMask subtc = SubTerrainMask.BOTTOM_MASK)
	{
		int s, j;
		for (s = 0, j = 1; s < 3 && (subtc & (SubTerrainMask)j) == SubTerrainMask.ZERO_MASK; s++, j += j)
		{
		}

		return terrain_type_min_height_array[tc - 1, s];
	}

	public static int max_height(int tc, SubTerrainMask subtc = SubTerrainMask.TOP_MASK)
	{
		if ((subtc & SubTerrainMask.TOP_MASK) != SubTerrainMask.ZERO_MASK)
			return tc < TOTAL_TERRAIN_TYPE ? terrain_type_min_height_array[tc - 1 + 1, 0] - 1 : 255;

		int s, j;
		for (s = 2, j = 2; s >= 0 && (subtc & (SubTerrainMask)j) == SubTerrainMask.ZERO_MASK; s--, j >>= 1)
		{
		}

		return terrain_type_min_height_array[tc - 1, s] - 1;
	}
}
