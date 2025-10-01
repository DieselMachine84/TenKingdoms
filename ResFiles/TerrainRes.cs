using System;
using System.Collections.Generic;
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
    public int FirstTerrainId { get; set; }
    public int LastTerrainId { get; set; }
    public int MinHeight { get; set; }
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

    public TerrainRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < nw_type_code.Length; i++, dataIndex++)
            nw_type_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < ne_type_code.Length; i++, dataIndex++)
            ne_type_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < sw_type_code.Length; i++, dataIndex++)
            sw_type_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < se_type_code.Length; i++, dataIndex++)
            se_type_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        extra_flag = db.ReadByte(recNo, dataIndex);
        dataIndex++;
        special_flag = db.ReadByte(recNo, dataIndex);
        dataIndex++;
        represent_type = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        dataIndex++;
        secondary_type = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        dataIndex++;

        for (int i = 0; i < pattern_id.Length; i++, dataIndex++)
            pattern_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < file_name.Length; i++, dataIndex++)
            file_name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < bitmap_ptr.Length; i++, dataIndex++)
            bitmap_ptr[i] = db.ReadByte(recNo, dataIndex);
    }
}

public class TerrainInfo
{
    public int NWType { get; set; }
    public int NWSubType { get; set; }
    public int NEType { get; set; }
    public int NESubType { get; set; }
    public int SWType { get; set; }
    public int SWSubType { get; set; }
    public int SEType { get; set; }
    public int SESubType { get; set; }

    public int AverageType { get; set; }
    public byte ExtraFlag { get; set; }
    public byte SpecialFlag { get; set; }
    public int SecondaryType { get; set; }
    public byte PatternId { get; set; }

    public int AlternativeCountWithExtra { get; set; } // no. of alternative bitmaps of this terrain
    public int AlternativeCountWithoutExtra { get; set; } // no. of alternative bitmaps of this terrain
    public TerrainFlag Flags { get; set; }

    public byte[] Bitmap { get; set; }
    public int BitmapWidth { get; set; }
    public int BitmapHeight  { get; set; }
    private IntPtr _texture;
    public int AnimationFrames  { get; set; }
    public byte[][] AnimationBitmaps  { get; set; }
    private readonly List<IntPtr> _animationTextures = new List<nint>();

    public IntPtr GetTexture(Graphics graphics)
    {
	    if (_texture == default)
		    _texture = graphics.CreateTextureFromBmp(Bitmap, BitmapWidth, BitmapHeight);

	    return _texture;
    }

    public IntPtr GetAnimationTexture(Graphics graphics, long frameNumber)
    {
	    if (AnimationFrames == 0)
		    return IntPtr.Zero;
	    
	    if (_animationTextures.Count == 0)
	    {
		    for (int i = 0; i < AnimationFrames; i++)
		    {
			    int width = BitConverter.ToInt16(AnimationBitmaps[i], 0);
			    int height = BitConverter.ToInt16(AnimationBitmaps[i], 2);
			    byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(AnimationBitmaps[i].Skip(4).ToArray(), width, height);
			    byte[] joinedBitmap = new byte[BitmapWidth * BitmapHeight];
			    Array.Copy(Bitmap, joinedBitmap, Bitmap.Length);
			    for (int j = 0; j < joinedBitmap.Length; j++)
			    {
				    if (decompressedBitmap[j] < Colors.MIN_TRANSPARENT_CODE)
					    joinedBitmap[j] = decompressedBitmap[j];
			    }
			    _animationTextures.Add(graphics.CreateTextureFromBmp(joinedBitmap, width, height));
		    }
	    }

	    return _animationTextures[(int)(frameNumber % AnimationFrames)];
    }

    public bool IsCoast()
    {
	    return AverageType == TerrainTypeCode.TERRAIN_OCEAN && SecondaryType > TerrainTypeCode.TERRAIN_OCEAN ||
	           AverageType > TerrainTypeCode.TERRAIN_OCEAN && SecondaryType == TerrainTypeCode.TERRAIN_OCEAN;
    }

    public bool CanSnow()
    {
        return (Flags & TerrainFlag.TERRAIN_FLAG_SNOW) != TerrainFlag.TERRAIN_FLAG_NONE;
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

    public TerrainSubRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < sub_no.Length; i++, dataIndex++)
            sub_no[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < step_id.Length; i++, dataIndex++)
            step_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < old_pattern_id.Length; i++, dataIndex++)
            old_pattern_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < new_pattern_id.Length; i++, dataIndex++)
            new_pattern_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < sec_adj.Length; i++, dataIndex++)
            sec_adj[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < post_move.Length; i++, dataIndex++)
            post_move[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
    }
}

public class TerrainSubInfo
{
    public int SubNo { get; set; }
    public int StepId { get; set; }
    public byte OldPatternId { get; set; }
    public byte NewPatternId { get; set; }
    public sbyte SecAdj { get; set; }
    public sbyte PostMove { get; set; }
    public TerrainSubInfo NextStep { get; set; }
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
    public TerrainAnimRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < base_file.Length; i++, dataIndex++)
            base_file[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < frame_no.Length; i++, dataIndex++)
            frame_no[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < next_frame.Length; i++, dataIndex++)
            next_frame[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < filename.Length; i++, dataIndex++)
            filename[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < bitmap_ptr.Length; i++, dataIndex++)
            bitmap_ptr[i] = db.ReadByte(recNo, dataIndex);
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

	private int TerrainCount { get; set; }
	private TerrainInfo[] TerrainInfos { get; set; }
	private char[] FileNames { get; set; }

	private TerrainType[] TerrainTypes { get; set; } = new TerrainType[TOTAL_TERRAIN_TYPE];
	private int[] NWTypeMin { get; set; } = new int[TOTAL_TERRAIN_TYPE];
	private int[] NWTypeMax { get; set; } = new int[TOTAL_TERRAIN_TYPE];

	//----- field related to terrain pattern substitution -----//
	private int TerrainSubRecCount { get; set; }
	private int TerrainSubIndexCount { get; set; }
	private TerrainSubInfo[] TerrainSubRecs { get; set; }
	private TerrainSubInfo[] TerrainSubIndexes { get; set; }

	private static int[] TerrainTypeColors { get; set; } = new int[TOTAL_TERRAIN_TYPE] { 0x20, 0x0A, 0x0D, 0x2D };

	private static string[] MapTileNames { get; set; } = new string[TOTAL_TERRAIN_TYPE] { "TERA_S", "TERA_DG", "TERA_LG", "TERA_D" };

	private static byte[][] MapTileBitmaps { get; set; } = new byte[TOTAL_TERRAIN_TYPE][];

	private static int[,] TerrainTypeMinHeights { get; set; } = new int[TOTAL_TERRAIN_TYPE, 3]
	{
		{ 0, 20, 40 }, // S-, S0, S+
		{ 60, 80, 110 }, // G-, G0, G+
		{ 130, 150, 180 }, // F-, F0, F+
		{ 200, 215, 240 }, // D-, D0, D+
	};

	public TerrainRes()
	{
		LoadInfo();
		LoadSubInfo();
		LoadAnimationInfo();

		ResourceIdx mapTiles = new ResourceIdx($"{Sys.GameDataFolder}/Resource/I_TPICT{Sys.Instance.Config.terrain_set}.RES");
		for (int i = 0; i < TOTAL_TERRAIN_TYPE; i++)
		{
			if (!String.IsNullOrEmpty(MapTileNames[i]))
			{
				MapTileBitmaps[i] = mapTiles.Read(MapTileNames[i]).Skip(4).ToArray();
			}
		}

		for (int i = 1; i <= TOTAL_TERRAIN_TYPE; i++)
		{
			TerrainTypes[i - 1] = new TerrainType();
			// scan for terrain of all four types. 1-take the first instance of the terrain bitmap that matches the given terrain types
			int terrainId = Scan(i, (int)SubTerrainMask.ALL_MASK, i, (int)SubTerrainMask.ALL_MASK,
				i, (int)SubTerrainMask.ALL_MASK, i, (int)SubTerrainMask.ALL_MASK, 1);

			if (terrainId != 0)
			{
				TerrainTypes[i - 1].FirstTerrainId = terrainId;
				TerrainTypes[i - 1].LastTerrainId = terrainId + this[terrainId].AlternativeCountWithExtra;
				TerrainTypes[i - 1].MinHeight = TerrainTypeMinHeights[i - 1, 0];
			}
			else
			{
				// BUGHERE: should not reach this point after all terrain type are complete
				TerrainTypes[i - 1].FirstTerrainId = 0;
				TerrainTypes[i - 1].LastTerrainId = 0;
				TerrainTypes[i - 1].MinHeight = TerrainTypeMinHeights[i - 1, 0];
			}
		}
	}
	
	public TerrainInfo this[int terrainId] => TerrainInfos[terrainId - 1];

	private void LoadInfo()
	{
		TerrainInfo terrainInfo = null;
		int i = 0;

		Database dbTerrain = new Database($"{Sys.GameDataFolder}/Resource/TERRAIN{Sys.Instance.Config.terrain_set}.RES");
		TerrainCount = dbTerrain.RecordCount;
		TerrainInfos = new TerrainInfo[TerrainCount];
		for (i = 0; i < TerrainInfos.Length; i++)
			TerrainInfos[i] = new TerrainInfo();

		FileNames = new char[TerrainRec.FILE_NAME_LEN * TerrainCount];

		for (i = 0; i < TOTAL_TERRAIN_TYPE; ++i)
		{
			NWTypeMin[i] = 0;
			NWTypeMax[i] = 0;
		}

		ResourceDb terrainBitmaps = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_TERN{Sys.Instance.Config.terrain_set}.RES");

		int firstNw = 0, firstNe = 0, firstSw = 0, firstSe = 0;
		int firstNwSub = 0, firstNeSub = 0, firstSwSub = 0, firstSeSub = 0;
		int firstSpFlag = 0;
		int firstId = 0;

		for (i = 0; i < TerrainCount; i++)
		{
			TerrainRec terrainRec = new TerrainRec(dbTerrain, i + 1);
			terrainInfo = TerrainInfos[i];

			for (int j = 0; j < TerrainRec.FILE_NAME_LEN; j++)
			{
				FileNames[i * TerrainRec.FILE_NAME_LEN + j] = terrainRec.file_name[j];
			}

			terrainInfo.NWType = TerrainCode(terrainRec.nw_type_code[0]);
			terrainInfo.NWSubType = (int)TerrainMask(terrainRec.nw_type_code[1]);
			terrainInfo.NEType = TerrainCode(terrainRec.ne_type_code[0]);
			terrainInfo.NESubType = (int)TerrainMask(terrainRec.ne_type_code[1]);
			terrainInfo.SWType = TerrainCode(terrainRec.sw_type_code[0]);
			terrainInfo.SWSubType = (int)TerrainMask(terrainRec.sw_type_code[1]);
			terrainInfo.SEType = TerrainCode(terrainRec.se_type_code[0]);
			terrainInfo.SESubType = (int)TerrainMask(terrainRec.se_type_code[1]);

			terrainInfo.AverageType = TerrainCode(terrainRec.represent_type);
			char extraFlagChar = Convert.ToChar(terrainRec.extra_flag);
			if (terrainRec.extra_flag == 0 || extraFlagChar == ' ' || extraFlagChar == 'N' || extraFlagChar == 'n')
				terrainInfo.ExtraFlag = 0;
			else
				terrainInfo.ExtraFlag = 1;
			terrainInfo.SpecialFlag = Convert.ToChar(terrainRec.special_flag) == ' ' ? (byte)0 : terrainRec.special_flag;
			terrainInfo.SecondaryType = TerrainCode(terrainRec.secondary_type);
			terrainInfo.PatternId = Convert.ToByte(Misc.ToString(terrainRec.pattern_id));

			//------ set alternative_count ---------//

			if (firstNw == terrainInfo.NWType && firstNwSub == terrainInfo.NWSubType &&
			    firstNe == terrainInfo.NEType && firstNeSub == terrainInfo.NESubType &&
			    firstSw == terrainInfo.SWType && firstSwSub == terrainInfo.SWSubType &&
			    firstSe == terrainInfo.SEType && firstSeSub == terrainInfo.SESubType &&
			    firstSpFlag == terrainInfo.SpecialFlag)
			{
				TerrainInfos[firstId - 1].AlternativeCountWithExtra++;

				if (terrainInfo.ExtraFlag == 0)
					TerrainInfos[firstId - 1].AlternativeCountWithoutExtra++;

				// ----- record firstId - terrain_id -------//
				terrainInfo.AlternativeCountWithExtra = firstId - 1 - i;
				terrainInfo.AlternativeCountWithoutExtra = firstId - 1 - i;
			}
			else
			{
				// build index on nw_type
				if (firstNw != terrainInfo.NWType)
				{
					// --------- mark end of previous NWType group ---------//
					if (firstNw > 0)
						NWTypeMax[firstNw - 1] = i;

					// --------- mark start of new NWType group -----------//
					NWTypeMin[terrainInfo.NWType - 1] = i + 1;
				}

				firstNw = terrainInfo.NWType;
				firstNe = terrainInfo.NEType;
				firstSw = terrainInfo.SWType;
				firstSe = terrainInfo.SEType;
				firstNwSub = terrainInfo.NWSubType;
				firstNeSub = terrainInfo.NESubType;
				firstSwSub = terrainInfo.SWSubType;
				firstSeSub = terrainInfo.SESubType;
				firstSpFlag = terrainInfo.SpecialFlag;
				firstId = i + 1;
			}

			int bitmapOffset = BitConverter.ToInt32(terrainRec.bitmap_ptr, 0);
			terrainInfo.Bitmap = terrainBitmaps.Read(bitmapOffset);
			terrainInfo.BitmapWidth = BitConverter.ToInt16(terrainInfo.Bitmap, 0);
			terrainInfo.BitmapHeight = BitConverter.ToInt16(terrainInfo.Bitmap, 2);
			terrainInfo.Bitmap = terrainInfo.Bitmap.Skip(4).ToArray();

			terrainInfo.Flags = TerrainFlag.TERRAIN_FLAG_NONE;
			if (terrainInfo.AverageType != TerrainTypeCode.TERRAIN_OCEAN && terrainInfo.SecondaryType != TerrainTypeCode.TERRAIN_OCEAN)
			{
				terrainInfo.Flags |= TerrainFlag.TERRAIN_FLAG_SNOW;
			}
		}

		// ------- mark end of last NWType group -------//
		if (terrainInfo != null && terrainInfo.NWType > 0)
			NWTypeMax[terrainInfo.NWType - 1] = i;
	}

	private void LoadSubInfo()
	{
		Database dbTerrain = new Database($"{Sys.GameDataFolder}/Resource/TERSUB.RES");
		TerrainSubRecCount = dbTerrain.RecordCount;
		TerrainSubRecs = new TerrainSubInfo[TerrainSubRecCount];
		for (int i = 0; i < TerrainSubRecs.Length; i++)
			TerrainSubRecs[i] = new TerrainSubInfo();

		int maxSubNo = 0;

		for (int i = 0; i < TerrainSubRecCount; i++)
		{
			TerrainSubRec terrainSubRec = new TerrainSubRec(dbTerrain, i + 1);
			TerrainSubInfo terrainSubInfo = TerrainSubRecs[i];

			terrainSubInfo.SubNo = Convert.ToInt16(Misc.ToString(terrainSubRec.sub_no));
			terrainSubInfo.StepId = Convert.ToInt16(Misc.ToString(terrainSubRec.step_id));
			terrainSubInfo.OldPatternId = Convert.ToByte(Misc.ToString(terrainSubRec.old_pattern_id));
			terrainSubInfo.NewPatternId = Convert.ToByte(Misc.ToString(terrainSubRec.new_pattern_id));

			// SecAdj is useful when a pure type is changing to boundary type.
			// eg. a GG square can be changed to SG or GS square
			terrainSubInfo.SecAdj = Convert.ToSByte(Misc.ToString(terrainSubRec.sec_adj));

			switch (terrainSubRec.post_move[0])
			{
				case 'N':
					switch (terrainSubRec.post_move[1])
					{
						case 'E':
							terrainSubInfo.PostMove = 2;
							break;
						case 'W':
							terrainSubInfo.PostMove = 8;
							break;
						default:
							terrainSubInfo.PostMove = 1;
							break;
					}
					break;
				case 'S':
					switch (terrainSubRec.post_move[1])
					{
						case 'E':
							terrainSubInfo.PostMove = 4;
							break;
						case 'W':
							terrainSubInfo.PostMove = 6;
							break;
						default:
							terrainSubInfo.PostMove = 5;
							break;
					}
					break;
				case 'E':
					terrainSubInfo.PostMove = 3;
					break;
				case 'W':
					terrainSubInfo.PostMove = 7;
					break;
				case 'X':
					terrainSubInfo.PostMove = 0;
					break;
			}

			terrainSubInfo.NextStep = null;

			if (terrainSubInfo.SubNo > maxSubNo)
				maxSubNo = terrainSubInfo.SubNo;
		}

		TerrainSubIndexCount = maxSubNo;
		TerrainSubIndexes = new TerrainSubInfo[TerrainSubIndexCount];

		TerrainSubInfo lastTerrainSubInfo = null;
		for (int i = 0; i < TerrainSubRecCount; i++)
		{
			TerrainSubInfo terrainSubInfo = TerrainSubRecs[i];
			if (terrainSubInfo.StepId == 1)
			{
				TerrainSubIndexes[terrainSubInfo.SubNo - 1] = terrainSubInfo;
			}
			else
			{
				// link from the NextStep pointer of previous step
				// search the previous record first
				if (lastTerrainSubInfo != null && lastTerrainSubInfo.SubNo == terrainSubInfo.SubNo
				                               && lastTerrainSubInfo.StepId == terrainSubInfo.StepId - 1)
				{
					lastTerrainSubInfo.NextStep = terrainSubInfo;
				}
				else
				{
					// search from the array
					for (int j = 0; j < TerrainSubRecCount; j++)
					{
						if (TerrainSubRecs[j].SubNo == terrainSubInfo.SubNo && TerrainSubRecs[j].StepId == terrainSubInfo.StepId - 1)
						{
							TerrainSubRecs[j].NextStep = terrainSubInfo;
							break;
						}
					}
				}
			}

			lastTerrainSubInfo = terrainSubInfo;
		}
	}

	private void LoadAnimationInfo()
	{
		TerrainAnimRec lastAnimRec = new TerrainAnimRec();
		ResourceDb animationBitmaps = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_TERA{Sys.Instance.Config.terrain_set}.RES");
		Database dbTerAnim = new Database($"{Sys.GameDataFolder}/Resource/TERANM{Sys.Instance.Config.terrain_set}.RES");

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

		for (int i = 0; i < count; i++)
		{
			TerrainAnimRec terrainAnimRec = new TerrainAnimRec(dbTerAnim, i + 1);
			int bitmapOffset = BitConverter.ToInt32(terrainAnimRec.bitmap_ptr, 0);
			byte[] bitmap = animationBitmaps.Read(bitmapOffset);

			if (Misc.ToString(terrainAnimRec.base_file) != Misc.ToString(lastAnimRec.base_file))
			{
				// string not equal
				if (lastAnimRec.filename[0] != ' ' && animFrameCount > 0)
				{
					// replace terrainInfo.AnimationFrames and AnimationBitmaps where the bitmap filename are the same
					for (int l = 0; l < TerrainCount; l++)
					{
						bool equals = true;
						for (int index = 0; index < TerrainAnimRec.FILE_NAME_LEN; index++)
						{
							if (FileNames[l * TerrainAnimRec.FILE_NAME_LEN + index] != lastAnimRec.base_file[index])
								equals = false;
						}

						if (equals)
						{
							TerrainInfo terrainInfo = TerrainInfos[l];
							terrainInfo.AnimationFrames = animFrameCount;
							terrainInfo.AnimationBitmaps = new byte[animFrameCount][];
							for (int j = 0; j < animFrameCount; j++)
								terrainInfo.AnimationBitmaps[j] = animFrameBitmap[j];
						}
					}
				}

				lastAnimRec = terrainAnimRec;
				animFrameCount = 0;
				for (int j = 0; j < animFrameBitmap.Length; j++)
					animFrameBitmap[j] = Array.Empty<byte>();
			}

			animFrameCount++;
			animFrameBitmap[Misc.ToInt32(terrainAnimRec.frame_no) - 1] = bitmap;
		}

		if (lastAnimRec.filename[0] != ' ' && animFrameCount > 0)
		{
			// replace terrainInfo.AnimationFrames and AnimationBitmaps where the bitmap filename are the same
			for (int l = 0; l < TerrainCount; l++)
			{
				bool equals = true;
				for (int index = 0; index < TerrainAnimRec.FILE_NAME_LEN; index++)
				{
					if (FileNames[l * TerrainAnimRec.FILE_NAME_LEN + index] != lastAnimRec.base_file[index])
						equals = false;
				}

				if (equals)
				{
					TerrainInfo terrainInfo = TerrainInfos[l];
					terrainInfo.AnimationFrames = animFrameCount;
					terrainInfo.AnimationBitmaps = new byte[animFrameCount][];
					for (int j = 0; j < animFrameCount; j++)
						terrainInfo.AnimationBitmaps[j] = animFrameBitmap[j];
				}
			}
		}
	}

	public byte GetTeraTypeId(char[] teraTypeCode)
	{
		return TerrainCode(teraTypeCode[0]);
	}

	public byte[] GetMapTile(int terrainId)
	{
		TerrainInfo terrainInfo = this[terrainId];
		return MapTileBitmaps[terrainInfo.AverageType - 1];
	}

	public int Scan(int nwType, int nwSubType, int neType, int neSubType,
		int swType, int swSubType, int seType, int seSubType,
		int firstInstance = 0, int includeExtra = 0, int special = 0)
	{
		int terrainId = NWTypeMin[nwType - 1];
		int terrainIdMax = NWTypeMax[nwType - 1];
		if (terrainId <= 0 || terrainIdMax <= 0)
			return 0;

		for (int index = terrainId - 1; terrainId <= terrainIdMax; terrainId++, index++)
		{
			TerrainInfo terrainInfo = TerrainInfos[index];
			if (terrainInfo.NWType == nwType && (terrainInfo.NWSubType & nwSubType) != 0 &&
			    terrainInfo.NEType == neType && (terrainInfo.NESubType & neSubType) != 0 &&
			    terrainInfo.SWType == swType && (terrainInfo.SWSubType & swSubType) != 0 &&
			    terrainInfo.SEType == seType && (terrainInfo.SESubType & seSubType) != 0 &&
			    terrainInfo.SpecialFlag == special)
			{
				if (firstInstance != 0)
					return terrainId;

				return includeExtra != 0
					? terrainId + Misc.Random(terrainInfo.AlternativeCountWithExtra + 1)
					: terrainId + Misc.Random(terrainInfo.AlternativeCountWithoutExtra + 1);
			}
			else
			{
				//------ skip tiles of the same type and SpecialFlag
				terrainId += terrainInfo.AlternativeCountWithExtra;
				index += terrainInfo.AlternativeCountWithExtra;
			}
		}

		return 0;
	}

	public int Scan(int primaryType, int secondaryType, int patternId, int firstInstance = 0, int includeExtra = 0, int special = 0)
	{
		// if patternId is zero, that means it requires a pure terrain
		if (patternId == 0)
			secondaryType = primaryType;

		// ----------- search 1, search the lower type -------//
		int terrainId = NWTypeMin[Math.Min(primaryType, secondaryType) - 1];
		int terrainIdMax = NWTypeMax[Math.Min(primaryType, secondaryType) - 1];

		for (int index = terrainId - 1; terrainId <= terrainIdMax; terrainId++, index++)
		{
			TerrainInfo terrainInfo = TerrainInfos[index];
			if (((terrainInfo.AverageType == primaryType && terrainInfo.SecondaryType == secondaryType) ||
			     (terrainInfo.AverageType == secondaryType && terrainInfo.SecondaryType == primaryType))
			    && terrainInfo.PatternId == patternId && terrainInfo.SpecialFlag == special)
			{
				if (firstInstance != 0)
					return terrainId;

				return includeExtra != 0
					? terrainId + Misc.Random(terrainInfo.AlternativeCountWithExtra + 1)
					: terrainId + Misc.Random(terrainInfo.AlternativeCountWithoutExtra + 1);
			}
			else
			{
				//------ skip tiles of the same type and SpecialFlag
				terrainId += terrainInfo.AlternativeCountWithExtra;
				index += terrainInfo.AlternativeCountWithExtra;
			}
		}

		// ----------- search 2, search the higher type ---------//
		if (primaryType != secondaryType)
		{
			terrainId = NWTypeMin[Math.Max(primaryType, secondaryType) - 1];
			terrainIdMax = NWTypeMax[Math.Max(primaryType, secondaryType) - 1];

			for (int index = terrainId - 1; terrainId <= terrainIdMax; terrainId++, index++)
			{
				TerrainInfo terrainInfo = TerrainInfos[index];
				if (((terrainInfo.AverageType == primaryType && terrainInfo.SecondaryType == secondaryType) ||
				     (terrainInfo.AverageType == secondaryType && terrainInfo.SecondaryType == primaryType))
				    && terrainInfo.PatternId == patternId && terrainInfo.SpecialFlag == special)
				{
					if (firstInstance != 0)
						return terrainId;

					return includeExtra != 0
						? terrainId + Misc.Random(terrainInfo.AlternativeCountWithExtra + 1)
						: terrainId + Misc.Random(terrainInfo.AlternativeCountWithoutExtra + 1);
				}
				else
				{
					//------ skip tiles of the same type and SpecialFlag
					terrainId += terrainInfo.AlternativeCountWithExtra;
					index += terrainInfo.AlternativeCountWithExtra;
				}
			}
		}

		return 0;
	}

	public int SearchPattern(int nwPatternId, TerrainSubInfo[] resultArray, int maxResult)
	{
		if (resultArray == null || maxResult == 0)
			return 0;

		int occur = 0;
		for (int i = 0; i < TerrainSubIndexCount; i++)
		{
			if (TerrainSubIndexes[i] != null && nwPatternId == TerrainSubIndexes[i].OldPatternId)
			{
				resultArray[occur++] = TerrainSubIndexes[i];
				if (occur >= maxResult)
					break;
			}
		}

		return occur;
	}

	public static byte TerrainCode(char tcCode)
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

	private static SubTerrainMask TerrainMask(char subtc)
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

	public static int TerrainHeight(int height, out int sub)
	{
		sub = 0;
		for (int tc = TOTAL_TERRAIN_TYPE - 1; tc >= 0; tc--)
		{
			if (height >= TerrainTypeMinHeights[tc, 0])
			{
				for (int subtc = 2; subtc >= 0; subtc--)
				{
					if (height >= TerrainTypeMinHeights[tc, subtc])
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

	public static int MinHeight(int tc, SubTerrainMask subtc = SubTerrainMask.BOTTOM_MASK)
	{
		int s, j;
		for (s = 0, j = 1; s < 3 && (subtc & (SubTerrainMask)j) == SubTerrainMask.ZERO_MASK; s++, j += j)
		{
		}

		return TerrainTypeMinHeights[tc - 1, s];
	}

	public static int MaxHeight(int tc, SubTerrainMask subtc = SubTerrainMask.TOP_MASK)
	{
		if ((subtc & SubTerrainMask.TOP_MASK) != SubTerrainMask.ZERO_MASK)
			return tc < TOTAL_TERRAIN_TYPE ? TerrainTypeMinHeights[tc - 1 + 1, 0] - 1 : 255;

		int s, j;
		for (s = 2, j = 2; s >= 0 && (subtc & (SubTerrainMask)j) == SubTerrainMask.ZERO_MASK; s--, j >>= 1)
		{
		}

		return TerrainTypeMinHeights[tc - 1, s] - 1;
	}
}
