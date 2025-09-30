using System;
using System.Linq;

namespace TenKingdoms;

public class HillBlockRec
{
	private const int PATTERN_ID_LEN = 3;
	private const int SUB_PATTERN_ID_LEN = 3;
	private const int PRIORITY_LEN = 1;
	private const int OFFSET_LEN = 3;
	private const int FILE_NAME_LEN = 8;
	private const int BITMAP_PTR_LEN = 4;

    public char[] pattern_id = new char[PATTERN_ID_LEN];
    public char[] sub_pattern_id = new char[SUB_PATTERN_ID_LEN];
    public char[] priority = new char[PRIORITY_LEN];
    public char special_flag;
    public char layer;
    public char bitmap_type;
    public char[] offset_x = new char[OFFSET_LEN];
    public char[] offset_y = new char[OFFSET_LEN];
    public char[] file_name = new char[FILE_NAME_LEN];
    public byte[] bitmap_ptr = new byte[BITMAP_PTR_LEN];

    public HillBlockRec(Database db, int recNo)
    {
	    int dataIndex = 0;
	    for (int i = 0; i < pattern_id.Length; i++, dataIndex++)
		    pattern_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
	    
	    for (int i = 0; i < sub_pattern_id.Length; i++, dataIndex++)
		    sub_pattern_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
	    
	    for (int i = 0; i < priority.Length; i++, dataIndex++)
		    priority[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
	    
	    special_flag = Convert.ToChar(db.ReadByte(recNo, dataIndex));
	    dataIndex++;
	    layer = Convert.ToChar(db.ReadByte(recNo, dataIndex));
	    dataIndex++;
	    bitmap_type = Convert.ToChar(db.ReadByte(recNo, dataIndex));
	    dataIndex++;
	    
	    for (int i = 0; i < offset_x.Length; i++, dataIndex++)
		    offset_x[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
	    
	    for (int i = 0; i < offset_y.Length; i++, dataIndex++)
		    offset_y[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
	    
	    for (int i = 0; i < file_name.Length; i++, dataIndex++)
		    file_name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
	    
	    for (int i = 0; i < bitmap_ptr.Length; i++, dataIndex++)
		    bitmap_ptr[i] = db.ReadByte(recNo, dataIndex);
    }
}

public class HillBlockInfo
{
    public int BlockId { get; set; }
    public int PatternId { get; set; }
    public int SubPatternId { get; set; }
    public int Priority { get; set; } // high value on top
    public int SpecialFlag { get; set; }
    public int Layer { get; set; } // 1 = draw together with background, 2 = sort together with units
    public char BitmapType { get; set; } // W=whole square; T=transparent; O=oversize
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }
    public byte[] Bitmap { get; set; }
    public int BitmapWidth { get; set; }
    public int BitmapHeight { get; set; }
    private IntPtr _texture;
    
    public IntPtr GetTexture(Graphics graphics)
    {
	    if (_texture == default)
	    {
		    byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(Bitmap, BitmapWidth, BitmapHeight);
		    _texture = graphics.CreateTextureFromBmp(decompressedBitmap, BitmapWidth, BitmapHeight);
	    }

	    return _texture;
    }
}

public class HillRes
{
	public const int LOW_HILL_PRIORITY = 3;
	public const int HIGH_HILL_PRIORITY = 7;

	private HillBlockInfo[] HillBlockInfos { get; set; }

    private int[] FirstBlockIndexes { get; set; } // array index of first hill block of each pattern
    private int MaxPatternId { get; set; }

    public HillRes()
    {
	    LoadHillBlockInfo();
    }

    public HillBlockInfo this[int hillBlockId] => HillBlockInfos[hillBlockId - 1];
    
    private void LoadHillBlockInfo()
    {
	    Database dbHill = new Database($"{Sys.GameDataFolder}/Resource/HILL{Sys.Instance.Config.terrain_set}.RES");
	    HillBlockInfos = new HillBlockInfo[dbHill.RecordCount];

	    ResourceDb hillBlockBitmaps = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_HILL{Sys.Instance.Config.terrain_set}.RES");
	    MaxPatternId = 0;

	    for (int i = 0; i < HillBlockInfos.Length; i++)
	    {
		    HillBlockRec hillBlockRec = new HillBlockRec(dbHill, i + 1);
		    HillBlockInfo hillBlockInfo = new HillBlockInfo();
		    HillBlockInfos[i] = hillBlockInfo;

		    hillBlockInfo.BlockId = i + 1;
		    hillBlockInfo.PatternId = Misc.ToInt32(hillBlockRec.pattern_id);

		    if (hillBlockInfo.PatternId > MaxPatternId)
			    MaxPatternId = hillBlockInfo.PatternId;

		    hillBlockInfo.SubPatternId = Misc.ToInt32(hillBlockRec.sub_pattern_id);

		    hillBlockInfo.SpecialFlag = Convert.ToInt32(hillBlockRec.special_flag);
		    if (hillBlockRec.special_flag == ' ')
			    hillBlockInfo.SpecialFlag = 0;
		    hillBlockInfo.Layer = hillBlockRec.layer - '0';

		    hillBlockInfo.Priority = Misc.ToInt32(hillBlockRec.priority);
		    hillBlockInfo.BitmapType = hillBlockRec.bitmap_type;

		    hillBlockInfo.OffsetX = Misc.ToInt32(hillBlockRec.offset_x);
		    hillBlockInfo.OffsetY = Misc.ToInt32(hillBlockRec.offset_y);

		    int bitmapOffset = BitConverter.ToInt32(hillBlockRec.bitmap_ptr, 0);
		    hillBlockInfo.Bitmap = hillBlockBitmaps.Read(bitmapOffset);
		    hillBlockInfo.BitmapWidth = BitConverter.ToInt16(hillBlockInfo.Bitmap, 0);
		    hillBlockInfo.BitmapHeight = BitConverter.ToInt16(hillBlockInfo.Bitmap, 2);
		    hillBlockInfo.Bitmap = hillBlockInfo.Bitmap.Skip(4).ToArray();
	    }

	    //------ build index for the first block of each pattern -------//
	    // e.g first block id of pattern 1 is 1
	    //     first block id of pattern 3 is 4
	    //     first block id of pattern 4 is 7
	    //     last block id (which is pattern 4) is 10
	    // FirstBlockIndexes is { 1, 4, 4, 7 };
	    // such that, blocks which are pattern 1 are between [1,4)
	    //                                     2 are between [4,4) i.e. not found
	    //                                     3 are between [4,7)
	    //                                     4 are between [7,11)
	    // see also FirstBlock()
	    //
	    FirstBlockIndexes = new int[MaxPatternId];
	    int patternMarked = 0;
	    for (int i = 0; i < HillBlockInfos.Length; i++)
	    {
		    HillBlockInfo hillBlockInfo = HillBlockInfos[i];
		    while (patternMarked < hillBlockInfo.PatternId)
		    {
			    FirstBlockIndexes[patternMarked] = i + 1;
			    patternMarked++;
		    }
	    }
    }

    // find exact
    public int Locate(int patternId, int subPattern, int searchPriority, int specialFlag)
    {
	    // ------- find the range which patternId may exist ------//
	    // find the start of this pattern and next pattern
	    int startBlockIdx = FirstBlock(patternId);
	    int endBlockIdx = FirstBlock(patternId + 1);
	    for (int i = startBlockIdx; i < endBlockIdx; i++)
	    {
		    HillBlockInfo hillBlockInfo = HillBlockInfos[i - 1];
		    if (hillBlockInfo.PatternId == patternId && hillBlockInfo.SubPatternId == subPattern &&
		        hillBlockInfo.Priority == searchPriority && hillBlockInfo.SpecialFlag == specialFlag)
		    {
			    return i;
		    }
	    }

	    return 0; // not found
    }

    // return hill block id of any one of the subPattern
    public int Scan(int patternId, int searchPriority, int specialFlag, bool findFirst)
    {
	    // ------- find the range which patternId may exist ------//
	    // find the start of this pattern and next pattern
	    int startBlockIdx = FirstBlock(patternId);
	    int endBlockIdx = FirstBlock(patternId + 1);
	    int foundBlockId = 0;
	    int foundCount = 0;

	    for (int j = startBlockIdx; j < endBlockIdx; j++)
	    {
		    HillBlockInfo hillBlockInfo = HillBlockInfos[j - 1];
		    if (hillBlockInfo.PatternId == patternId && hillBlockInfo.Priority == searchPriority && hillBlockInfo.SpecialFlag == specialFlag)
		    {
			    if (findFirst)
				    return j;
			    if (Misc.Random(++foundCount) == 0)
				    foundBlockId = j;
		    }
	    }

	    return foundBlockId; // not found
    }

    private int FirstBlock(int hillPatternId)
    {
	    if (hillPatternId > MaxPatternId)
		    return HillBlockInfos.Length + 1; // return last block + 1
	    else
		    return FirstBlockIndexes[hillPatternId - 1];
    }
}
