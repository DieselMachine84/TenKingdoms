using System;

namespace TenKingdoms;

public class RockRec
{
    public const int ROCKID_LEN = 5;
    public const int LOC_LEN = 2;
    public const int RECNO_LEN = 4;
    public const int MAX_FRAME_LEN = 2;

    public char[] rock_id = new char[ROCKID_LEN];
    public char rock_type;
    public char[] loc_width = new char[LOC_LEN];
    public char[] loc_height = new char[LOC_LEN];
    public char terrain_1;
    public char terrain_2;
    public char[] first_anim_recno = new char[RECNO_LEN];
    public char[] max_frame = new char[MAX_FRAME_LEN];

    public RockRec(byte[] data)
    {
        int dataIndex = 0;
        for (int i = 0; i < rock_id.Length; i++, dataIndex++)
            rock_id[i] = Convert.ToChar(data[dataIndex]);
        
        rock_type = Convert.ToChar(data[dataIndex]);
        dataIndex++;
        
        for (int i = 0; i < loc_width.Length; i++, dataIndex++)
            loc_width[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < loc_height.Length; i++, dataIndex++)
            loc_height[i] = Convert.ToChar(data[dataIndex]);
        
        terrain_1 = Convert.ToChar(data[dataIndex]);
        dataIndex++;
        
        terrain_2 = Convert.ToChar(data[dataIndex]);
        dataIndex++;
        
        for (int i = 0; i < first_anim_recno.Length; i++, dataIndex++)
            first_anim_recno[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < max_frame.Length; i++, dataIndex++)
            max_frame[i] = Convert.ToChar(data[dataIndex]);
    }
}

public class RockBlockRec
{
    public const int ROCKID_LEN = 5;
    public const int LOC_LEN = 2;
    public const int RECNO_LEN = 4;

    public char[] rock_id = new char[ROCKID_LEN];
    public char[] loc_x = new char[LOC_LEN];
    public char[] loc_y = new char[LOC_LEN];
    public char[] rock_recno = new char[RECNO_LEN];
    public char[] first_bitmap = new char[RECNO_LEN];

    public RockBlockRec(byte[] data)
    {
        int dataIndex = 0;
        for (int i = 0; i < rock_id.Length; i++, dataIndex++)
            rock_id[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < loc_x.Length; i++, dataIndex++)
            loc_x[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < loc_y.Length; i++, dataIndex++)
            loc_y[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < rock_recno.Length; i++, dataIndex++)
            rock_recno[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < first_bitmap.Length; i++, dataIndex++)
            first_bitmap[i] = Convert.ToChar(data[dataIndex]);
    }
}

public class RockBitmapRec
{
    public const int ROCKID_LEN = 5;
    public const int LOC_LEN = 2;
    public const int FRAME_NO_LEN = 2;
    public const int FILE_NAME_LEN = 8;
    public const int BITMAP_PTR_LEN = 4;

    public char[] rock_id = new char[ROCKID_LEN];
    public char[] loc_x = new char[LOC_LEN];
    public char[] loc_y = new char[LOC_LEN];
    public char[] frame = new char[FRAME_NO_LEN];
    public char[] file_name = new char[FILE_NAME_LEN];
    public byte[] bitmap_ptr = new byte[BITMAP_PTR_LEN];

    public RockBitmapRec(byte[] data)
    {
        int dataIndex = 0;
        for (int i = 0; i < rock_id.Length; i++, dataIndex++)
            rock_id[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < loc_x.Length; i++, dataIndex++)
            loc_x[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < loc_y.Length; i++, dataIndex++)
            loc_y[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < frame.Length; i++, dataIndex++)
            frame[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < file_name.Length; i++, dataIndex++)
            file_name[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < bitmap_ptr.Length; i++, dataIndex++)
            bitmap_ptr[i] = data[dataIndex];
    }
}

public class RockAnimRec
{
    public const int ROCKID_LEN = 5;
    public const int FRAME_NO_LEN = 2;
    public const int DELAY_LEN = 3;

    public char[] rock_id = new char[ROCKID_LEN];
    public char[] frame = new char[FRAME_NO_LEN];
    public char[] delay = new char[DELAY_LEN];
    public char[] next_frame = new char[FRAME_NO_LEN];
    public char[] alt_next = new char[FRAME_NO_LEN];

    public RockAnimRec(byte[] data)
    {
        int dataIndex = 0;
        for (int i = 0; i < rock_id.Length; i++, dataIndex++)
            rock_id[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < frame.Length; i++, dataIndex++)
            frame[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < delay.Length; i++, dataIndex++)
            delay[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < next_frame.Length; i++, dataIndex++)
            next_frame[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < alt_next.Length; i++, dataIndex++)
            alt_next[i] = Convert.ToChar(data[dataIndex]);
    }
}

public class RockInfo
{
    public const char DIRT_BLOCKING_TYPE = 'E';
    
    public string rock_name;
    public char rock_type;
    public int loc_width;
    public int loc_height;
    public int terrain_1; // TerrainTypeCode
    public int terrain_2; // TerrainTypeCode
    public int first_anim_recno;
    public int max_frame;
    public int first_block_recno;

    public int[,] block_offset = new int[RockRes.MAX_ROCK_HEIGHT, RockRes.MAX_ROCK_WIDTH];

    public bool valid_terrain(int terrainType)
    {
        return terrainType >= terrain_1 && terrainType <= terrain_2;
    }
}

public class RockBlockInfo
{
    public int loc_x;
    public int loc_y;
    public int rock_recno; // recno in RockRec/RockInfo
    public int first_bitmap; // recno in RockBitmapRec/RockBitmapInfo
}

public class RockBitmapInfo
{
    public int loc_x; // checking only
    public int loc_y; // checking only
    public int frame; // checking only
    public byte[] bitmap;
    public int bitmapWidth;
    public int bitmapHeight;
}

public class RockAnimInfo
{
    public int frame; // checking only
    public int delay;
    public int next_frame;
    public int alt_next;

    public int choose_next(long path)
    {
        return path != 0 ? next_frame : alt_next;
    }
}

public class RockRes
{
    public const int MAX_ROCK_WIDTH = 4;
    public const int MAX_ROCK_HEIGHT = 4;

    public RockInfo[] rock_info_array;
    public RockBlockInfo[] rock_block_array;
    public RockBitmapInfo[] rock_bitmap_array;
    public RockAnimInfo[] rock_anim_array;

    public ResourceDb res_bitmap;

    public RockRes()
    {
        string rockDbName = $"{Sys.GameDataFolder}/Resource/I_ROCK{Sys.Instance.Config.terrain_set}.RES";
        res_bitmap = new ResourceDb(rockDbName);

        LoadInfo();
        LoadBitmapInfo();
        LoadBlockInfo();
        LoadAnimInfo();
    }

    public RockInfo get_rock_info(int rockRecno)
    {
        return rock_info_array[rockRecno - 1];
    }

    public RockBlockInfo get_block_info(int rockBlockRecno)
    {
        return rock_block_array[rockBlockRecno - 1];
    }

    public RockBitmapInfo get_bitmap_info(int rockBitmapRecno)
    {
        return rock_bitmap_array[rockBitmapRecno - 1];
    }

    public RockAnimInfo get_anim_info(int rockAnimRecno)
    {
        if (rockAnimRecno == -1) // non-animated rock
        {
            RockAnimInfo unanimatedInfo = new RockAnimInfo();
            unanimatedInfo.frame = 1;
            unanimatedInfo.delay = 99;
            unanimatedInfo.next_frame = 1;
            unanimatedInfo.alt_next = 1;
            return unanimatedInfo;
        }

        return rock_anim_array[rockAnimRecno - 1];
    }

    public int choose_next(int rockRecno, int curFrame, long path)
    {
        // -------- validate rockRecno ---------//
        RockInfo rockInfo = get_rock_info(rockRecno);

        // -------- validate curFrame ----------//
        RockAnimInfo rockAnimInfo = get_anim_info(get_anim_recno(rockRecno, curFrame));

        // -------- validate frame, next_frame and alt_next in rockAnimInfo -------/
        return rockAnimInfo.choose_next(path);
    }

    public RockBlockInfo this[int rockBlockRecno] => rock_block_array[rockBlockRecno - 1];

    public int search(string rockTypes, int minWidth, int maxWidth, int minHeight, int maxHeight,
        int animatedFlag = -1, bool findFirst = false, int terrainType = 0)
    {
        // -------- search a rock by rock_type, width and height ---------//
        int rockRecno = 0;
        int findCount = 0;

        for (int i = 0; i < rock_info_array.Length; i++)
        {
            RockInfo rockInfo = rock_info_array[i];
            if ((string.IsNullOrEmpty(rockTypes) || rockTypes.Contains(rockInfo.rock_type))
                && (terrainType == 0 || rockInfo.valid_terrain(terrainType))
                && rockInfo.loc_width >= minWidth && rockInfo.loc_width <= maxWidth
                && rockInfo.loc_height >= minHeight && rockInfo.loc_height <= maxHeight
                && (animatedFlag < 0 || animatedFlag == 0 && rockInfo.max_frame == 1 ||
                    animatedFlag > 0 && rockInfo.max_frame > 1))
            {
                ++findCount;
                if (findFirst)
                {
                    rockRecno = i + 1;
                    break;
                }
                else if (Misc.Random(findCount) == 0)
                {
                    rockRecno = i + 1;
                }
            }
        }

        return rockRecno;
    }

    public int locate(string rockName)
    {
        int rockRecno = 0;

        for (int i = 1; i <= rock_info_array.Length; ++i)
        {
            RockInfo rockInfo = rock_info_array[i];
            if (rockName == new string(rockInfo.rock_name))
            {
                rockRecno = i;
                break;
            }
        }

        return rockRecno;
    }

    public int locate_block(int rockRecno, int xLoc, int yLoc)
    {
        if (xLoc < MAX_ROCK_WIDTH && yLoc < MAX_ROCK_HEIGHT)
        {
            // if xLoc and yLoc is small enough, the block of offset x and offset y
            // can be found in block_offset,
            return get_rock_info(rockRecno).block_offset[yLoc, xLoc];
        }
        else
        {
            // otherwise, linear search
            int rockBlockRecno = get_rock_info(rockRecno).first_block_recno;
            for (; rockBlockRecno <= rock_block_array.Length; rockBlockRecno++)
            {
                RockBlockInfo rockBlockInfo = get_block_info(rockBlockRecno);
                if (rockBlockInfo.rock_recno != rockRecno)
                    break;
                if (rockBlockInfo.loc_x == xLoc && rockBlockInfo.loc_y == yLoc)
                    return rockBlockRecno;
            }

            return 0;
        }
    }

    public int get_bitmap_recno(int rockBlockRecno, int curFrame)
    {
        RockBlockInfo rockBlockInfo = get_block_info(rockBlockRecno);
        return rockBlockInfo.first_bitmap + curFrame - 1;
    }

    // return rockAnimRecno
    public int get_anim_recno(int rockRecno, int curFrame)
    {
        RockInfo rockInfo = get_rock_info(rockRecno);

        return rockInfo.first_anim_recno != 0 ? rockInfo.first_anim_recno + (curFrame - 1) : -1;
    }

    private void LoadInfo()
    {
        //---- read in rock count and initialize rock info array ----//

        string rockDbName = $"{Sys.GameDataFolder}/Resource/ROCK{Sys.Instance.Config.terrain_set}.RES";
        Database dbRock = new Database(rockDbName);

        rock_info_array = new RockInfo[dbRock.RecordCount];

        //---------- read in ROCK.DBF ---------//

        for (int i = 0; i < rock_info_array.Length; i++)
        {
            RockRec rockRec = new RockRec(dbRock.Read(i + 1));
            RockInfo rockInfo = new RockInfo();
            rock_info_array[i] = rockInfo;

            rockInfo.rock_name = Misc.ToString(rockRec.rock_id);
            rockInfo.rock_type = rockRec.rock_type;
            rockInfo.loc_width = Misc.ToInt32(rockRec.loc_width);
            rockInfo.loc_height = Misc.ToInt32(rockRec.loc_height);
            if (rockRec.terrain_1 == 0 || rockRec.terrain_1 == ' ')
                rockInfo.terrain_1 = 0;
            else
                rockInfo.terrain_1 = TerrainRes.terrain_code(rockRec.terrain_1);
            if (rockRec.terrain_2 == 0 || rockRec.terrain_2 == ' ')
                rockInfo.terrain_2 = 0;
            else
                rockInfo.terrain_2 = TerrainRes.terrain_code(rockRec.terrain_2);
            rockInfo.first_anim_recno = Misc.ToInt32(rockRec.first_anim_recno);
            if (rockInfo.first_anim_recno != 0)
                rockInfo.max_frame = Misc.ToInt32(rockRec.max_frame);
            else
                rockInfo.max_frame = 1; // unanimated, rock anim recno must be -1

            rockInfo.first_block_recno = 0;
        }
    }

    private void LoadBlockInfo()
    {
        //---- read in rock count and initialize rock info array ----//
        string rockDbName = $"{Sys.GameDataFolder}/Resource/ROCKBLK{Sys.Instance.Config.terrain_set}.RES";
        Database dbRock = new Database(rockDbName);

        rock_block_array = new RockBlockInfo[dbRock.RecordCount];

        //---------- read in ROCKBLK.DBF ---------//

        for (int i = 0; i < rock_block_array.Length; i++)
        {
            RockBlockRec rockBlockRec = new RockBlockRec(dbRock.Read(i + 1));
            RockBlockInfo rockBlockInfo = new RockBlockInfo();
            rock_block_array[i] = rockBlockInfo;

            rockBlockInfo.loc_x = Misc.ToInt32(rockBlockRec.loc_x);
            rockBlockInfo.loc_y = Misc.ToInt32(rockBlockRec.loc_y);
            rockBlockInfo.rock_recno = Misc.ToInt32(rockBlockRec.rock_recno);
            rockBlockInfo.first_bitmap = Misc.ToInt32(rockBlockRec.first_bitmap);

            // ------- validate rock_recno --------//
            RockInfo rockInfo = rock_info_array[rockBlockInfo.rock_recno - 1];
            if (rockInfo.first_block_recno == 0)
                rockInfo.first_block_recno = i + 1;

            // ------- set block_offset in rockInfo ----------//
            if (rockBlockInfo.loc_x < MAX_ROCK_WIDTH && rockBlockInfo.loc_y < MAX_ROCK_HEIGHT)
            {
                // store the rockBlockRecno (i.e. i+1) in rockInfo->block_offset
                // in order to find a rock block from rock recno and x offset, y offset
                // thus make RockRes::locate_block() faster
                rockInfo.block_offset[rockBlockInfo.loc_y, rockBlockInfo.loc_x] = i + 1;
            }
        }
    }

    private void LoadBitmapInfo()
    {
        //---- read in rock count and initialize rock info array ----//

        string rockDbName = $"{Sys.GameDataFolder}/Resource/ROCKBMP{Sys.Instance.Config.terrain_set}.RES";
        Database dbRock = new Database(rockDbName);

        rock_bitmap_array = new RockBitmapInfo[dbRock.RecordCount];

        //---------- read in ROCKBMP.DBF ---------//

        for (int i = 0; i < rock_bitmap_array.Length; i++)
        {
            RockBitmapRec rockBitmapRec = new RockBitmapRec(dbRock.Read(i + 1));
            RockBitmapInfo rockBitmapInfo = new RockBitmapInfo();
            rock_bitmap_array[i] = rockBitmapInfo;

            rockBitmapInfo.loc_x = Misc.ToInt32(rockBitmapRec.loc_x);
            rockBitmapInfo.loc_y = Misc.ToInt32(rockBitmapRec.loc_y);
            rockBitmapInfo.frame = Misc.ToInt32(rockBitmapRec.frame);

            int bitmapOffset = BitConverter.ToInt32(rockBitmapRec.bitmap_ptr, 0);
            rockBitmapInfo.bitmap = res_bitmap.Read(bitmapOffset);
            rockBitmapInfo.bitmapWidth = BitConverter.ToInt16(rockBitmapInfo.bitmap, 0);
            rockBitmapInfo.bitmapHeight = BitConverter.ToInt16(rockBitmapInfo.bitmap, 2);
        }
    }

    private void LoadAnimInfo()
    {
        //---- read in rock count and initialize rock info array ----//
        string rockDbName = $"{Sys.GameDataFolder}/Resource/ROCKANI{Sys.Instance.Config.terrain_set}.RES";
        Database dbRock = new Database(rockDbName);

        rock_anim_array = new RockAnimInfo[dbRock.RecordCount];

        //---------- read in ROCKANIM.DBF ---------//

        for (int i = 0; i < rock_anim_array.Length; i++)
        {
            RockAnimRec rockAnimRec = new RockAnimRec(dbRock.Read(i + 1));
            RockAnimInfo rockAnimInfo = new RockAnimInfo();
            rock_anim_array[i] = rockAnimInfo;

            rockAnimInfo.frame = Misc.ToInt32(rockAnimRec.frame);
            rockAnimInfo.delay = Misc.ToInt32(rockAnimRec.delay);
            rockAnimInfo.next_frame = Misc.ToInt32(rockAnimRec.next_frame);
            rockAnimInfo.alt_next = Misc.ToInt32(rockAnimRec.alt_next);
            if (rockAnimInfo.alt_next == 0)
                rockAnimInfo.alt_next = rockAnimInfo.next_frame;
        }
    }
}