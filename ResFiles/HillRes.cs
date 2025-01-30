using System;
using System.Linq;

namespace TenKingdoms;

public class HillBlockRec
{
    public const int PATTERN_ID_LEN = 3;
    public const int SUB_PATTERN_ID_LEN = 3;
    public const int PRIORITY_LEN = 1;
    public const int OFFSET_LEN = 3;
    public const int FILE_NAME_LEN = 8;
    public const int BITMAP_PTR_LEN = 4;

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

    public HillBlockRec(byte[] data)
    {
	    int dataIndex = 0;
	    for (int i = 0; i < pattern_id.Length; i++, dataIndex++)
		    pattern_id[i] = Convert.ToChar(data[dataIndex]);
	    
	    for (int i = 0; i < sub_pattern_id.Length; i++, dataIndex++)
		    sub_pattern_id[i] = Convert.ToChar(data[dataIndex]);
	    
	    for (int i = 0; i < priority.Length; i++, dataIndex++)
		    priority[i] = Convert.ToChar(data[dataIndex]);
	    
	    special_flag = Convert.ToChar(data[dataIndex]);
	    dataIndex++;
	    layer = Convert.ToChar(data[dataIndex]);
	    dataIndex++;
	    bitmap_type = Convert.ToChar(data[dataIndex]);
	    dataIndex++;
	    
	    for (int i = 0; i < offset_x.Length; i++, dataIndex++)
		    offset_x[i] = Convert.ToChar(data[dataIndex]);
	    
	    for (int i = 0; i < offset_y.Length; i++, dataIndex++)
		    offset_y[i] = Convert.ToChar(data[dataIndex]);
	    
	    for (int i = 0; i < file_name.Length; i++, dataIndex++)
		    file_name[i] = Convert.ToChar(data[dataIndex]);
	    
	    for (int i = 0; i < bitmap_ptr.Length; i++, dataIndex++)
		    bitmap_ptr[i] = data[dataIndex];
    }
}

public class HillBlockInfo
{
    public int block_id;
    public int pattern_id;
    public int sub_pattern_id;
    public int priority; // high value on top
    public int special_flag;
    public int layer; // 1= draw together with background, 2= sort together with units
    public char bitmap_type; // W=whole square; T=transparent; O=oversize
    public int offset_x;
    public int offset_y;
    public byte[] bitmap;
    public int bitmapWidth;
    public int bitmapHeight;
    private IntPtr texture;
    
    public IntPtr GetTexture(Graphics graphics)
    {
	    if (texture == default)
	    {
		    byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(bitmap, bitmapWidth, bitmapHeight);
		    texture = graphics.CreateTextureFromBmp(decompressedBitmap, bitmapWidth, bitmapHeight);
	    }

	    return texture;
    }
}

public class HillRes
{
	public const int LOW_HILL_PRIORITY = 3;
	public const int HIGH_HILL_PRIORITY = 7;

	public HillBlockInfo[] hill_block_info_array;

    public int[] first_block_index; // array index of first hill block of each pattern
    public int max_pattern_id;

    public ResourceDb res_bitmap;

    public HillRes()
    {
	    res_bitmap = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_HILL{Sys.Instance.Config.terrain_set}.RES");
	    
	    LoadHillBlockInfo();
    }

    private void LoadHillBlockInfo()
    {
	    Database dbHill = new Database($"{Sys.GameDataFolder}/Resource/HILL{Sys.Instance.Config.terrain_set}.RES");
	    hill_block_info_array = new HillBlockInfo[dbHill.RecordCount];

	    max_pattern_id = 0;

	    //---------- read in HILL.DBF ---------//

	    for (int i = 0; i < hill_block_info_array.Length; i++)
	    {
		    HillBlockRec hillBlockRec = new HillBlockRec(dbHill.Read(i + 1));
		    HillBlockInfo hillBlockInfo = new HillBlockInfo();
		    hill_block_info_array[i] = hillBlockInfo;

		    hillBlockInfo.block_id = i + 1;
		    hillBlockInfo.pattern_id = Convert.ToByte(new string(hillBlockRec.pattern_id));

		    if (hillBlockInfo.pattern_id > max_pattern_id)
			    max_pattern_id = hillBlockInfo.pattern_id;

		    string subPatternId = string.Empty;
		    hillBlockInfo.sub_pattern_id = Convert.ToInt32(new string(hillBlockRec.sub_pattern_id));

		    hillBlockInfo.special_flag = Convert.ToInt32(hillBlockRec.special_flag);
		    if (hillBlockRec.special_flag == ' ')
			    hillBlockInfo.special_flag = 0;
		    hillBlockInfo.layer = hillBlockRec.layer - '0';

		    hillBlockInfo.priority = Convert.ToInt32(new string(hillBlockRec.priority));
		    hillBlockInfo.bitmap_type = hillBlockRec.bitmap_type;

		    hillBlockInfo.offset_x = Convert.ToInt32(new string(hillBlockRec.offset_x));
		    hillBlockInfo.offset_y = Convert.ToInt32(new string(hillBlockRec.offset_y));

		    int bitmapOffset = BitConverter.ToInt32(hillBlockRec.bitmap_ptr, 0);
		    hillBlockInfo.bitmap = res_bitmap.Read(bitmapOffset);
		    hillBlockInfo.bitmapWidth = BitConverter.ToInt16(hillBlockInfo.bitmap, 0);
		    hillBlockInfo.bitmapHeight = BitConverter.ToInt16(hillBlockInfo.bitmap, 2);
		    hillBlockInfo.bitmap = hillBlockInfo.bitmap.Skip(4).ToArray();
	    }

	    //------ build index for the first block of each pattern -------//
	    // e.g first block id of pattern 1 is 1
	    //     first block id of pattern 3 is 4
	    //     first block id of pattern 4 is 7
	    //     last block id (which is pattern 4) is 10
	    // first_block_index is { 1, 4, 4, 7 };
	    // such that, blocks which are pattern 1 are between [1,4)
	    //                                     2 are between [4,4) i.e. not found
	    //                                     3 are between [4,7)
	    //                                     4 are between [7,11)
	    // see also first_block()
	    //
	    first_block_index = new int[max_pattern_id];
	    int patternMarked = 0;
	    foreach (HillBlockInfo hillBlockInfo in hill_block_info_array)
	    {
		    
	    }
	    for (int i = 0; i < hill_block_info_array.Length; i++)
	    {
		    HillBlockInfo hillBlockInfo = hill_block_info_array[i];
		    while (patternMarked < hillBlockInfo.pattern_id)
		    {
			    first_block_index[patternMarked] = i + 1;
			    patternMarked++;
		    }
	    }
    }

    // find exact
    public int locate(int patternId, int subPattern, int searchPriority, int specialFlag)
    {
	    // ------- find the range which patternId may exist ------//
	    // find the start of this pattern and next pattern
	    int startBlockIdx = first_block(patternId);
	    int endBlockIdx = first_block(patternId + 1);
	    for (int j = startBlockIdx; j < endBlockIdx; j++)
	    {
		    HillBlockInfo hillBlockInfo = hill_block_info_array[j - 1];
		    if (hillBlockInfo.pattern_id == patternId &&
		        hillBlockInfo.sub_pattern_id == subPattern &&
		        hillBlockInfo.priority == searchPriority &&
		        hillBlockInfo.special_flag == specialFlag)
		    {
			    return j;
		    }
	    }

	    return 0; // not found
    }

    // return hill block id of any one of the subPattern
    public int scan(int patternId, int searchPriority, int specialFlag, bool findFirst)
    {
	    // ------- find the range which patternId may exist ------//
	    // find the start of this pattern and next pattern
	    int startBlockIdx = first_block(patternId);
	    int endBlockIdx = first_block(patternId + 1);
	    int foundBlockId = 0;
	    int foundCount = 0;

	    for (int j = startBlockIdx; j < endBlockIdx; j++)
	    {
		    HillBlockInfo hillBlockInfo = hill_block_info_array[j - 1];
		    if (hillBlockInfo.pattern_id == patternId &&
		        hillBlockInfo.priority == searchPriority &&
		        hillBlockInfo.special_flag == specialFlag)
		    {
			    if (findFirst)
				    return j;
			    if (Misc.Random(++foundCount) == 0)
				    foundBlockId = j;
		    }
	    }

	    return foundBlockId; // not found
    }

    //-------- function related to HillBlock ---------//
    public HillBlockInfo this[int hillBlockId] => hill_block_info_array[hillBlockId - 1];

    public int first_block(int hillPatternId)
    {
	    if (hillPatternId > max_pattern_id)
		    return hill_block_info_array.Length + 1; // return last block+1
	    else
		    return first_block_index[hillPatternId - 1];
    }
}
