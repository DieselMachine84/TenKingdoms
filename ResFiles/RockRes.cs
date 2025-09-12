using System;
using System.Linq;

namespace TenKingdoms;

public class RockRec
{
    public const int ROCKID_LEN = 5;
    public const int LOC_LEN = 2;
    public const int RECNO_LEN = 4;
    public const int MAX_FRAME_LEN = 2;

    public char[] rockId = new char[ROCKID_LEN];
    public char rockType;
    public char[] locWidth = new char[LOC_LEN];
    public char[] locHeight = new char[LOC_LEN];
    public char terrain1;
    public char terrain2;
    public char[] firstAnimRecno = new char[RECNO_LEN];
    public char[] maxFrame = new char[MAX_FRAME_LEN];

    public RockRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < rockId.Length; i++, dataIndex++)
            rockId[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        rockType = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        dataIndex++;
        
        for (int i = 0; i < locWidth.Length; i++, dataIndex++)
            locWidth[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < locHeight.Length; i++, dataIndex++)
            locHeight[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        terrain1 = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        dataIndex++;
        
        terrain2 = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        dataIndex++;
        
        for (int i = 0; i < firstAnimRecno.Length; i++, dataIndex++)
            firstAnimRecno[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < maxFrame.Length; i++, dataIndex++)
            maxFrame[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
    }
}

public class RockBlockRec
{
    public const int ROCKID_LEN = 5;
    public const int LOC_LEN = 2;
    public const int RECNO_LEN = 4;

    public char[] rockId = new char[ROCKID_LEN];
    public char[] locX = new char[LOC_LEN];
    public char[] locY = new char[LOC_LEN];
    public char[] rockRecno = new char[RECNO_LEN];
    public char[] firstBitmap = new char[RECNO_LEN];

    public RockBlockRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < rockId.Length; i++, dataIndex++)
            rockId[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < locX.Length; i++, dataIndex++)
            locX[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < locY.Length; i++, dataIndex++)
            locY[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < rockRecno.Length; i++, dataIndex++)
            rockRecno[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < firstBitmap.Length; i++, dataIndex++)
            firstBitmap[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
    }
}

public class RockBitmapRec
{
    public const int ROCKID_LEN = 5;
    public const int LOC_LEN = 2;
    public const int FRAME_NO_LEN = 2;
    public const int FILE_NAME_LEN = 8;
    public const int BITMAP_PTR_LEN = 4;

    public char[] rockId = new char[ROCKID_LEN];
    public char[] locX = new char[LOC_LEN];
    public char[] locY = new char[LOC_LEN];
    public char[] frame = new char[FRAME_NO_LEN];
    public char[] fileName = new char[FILE_NAME_LEN];
    public byte[] bitmap = new byte[BITMAP_PTR_LEN];

    public RockBitmapRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < rockId.Length; i++, dataIndex++)
            rockId[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < locX.Length; i++, dataIndex++)
            locX[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < locY.Length; i++, dataIndex++)
            locY[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < frame.Length; i++, dataIndex++)
            frame[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < fileName.Length; i++, dataIndex++)
            fileName[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < bitmap.Length; i++, dataIndex++)
            bitmap[i] = db.ReadByte(recNo, dataIndex);
    }
}

public class RockAnimRec
{
    public const int ROCKID_LEN = 5;
    public const int FRAME_NO_LEN = 2;
    public const int DELAY_LEN = 3;

    public char[] rockId = new char[ROCKID_LEN];
    public char[] frame = new char[FRAME_NO_LEN];
    public char[] delay = new char[DELAY_LEN];
    public char[] nextFrame = new char[FRAME_NO_LEN];
    public char[] altNext = new char[FRAME_NO_LEN];

    public RockAnimRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < rockId.Length; i++, dataIndex++)
            rockId[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < frame.Length; i++, dataIndex++)
            frame[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < delay.Length; i++, dataIndex++)
            delay[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < nextFrame.Length; i++, dataIndex++)
            nextFrame[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < altNext.Length; i++, dataIndex++)
            altNext[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
    }
}

public class RockInfo
{
    public const char ROCK_BLOCKING_TYPE = 'R';
    public const char DIRT_NON_BLOCKING_TYPE = 'D';
    public const char DIRT_BLOCKING_TYPE = 'E';
    
    public string rockName;
    public char rockType;
    public int locWidth;
    public int locHeight;
    public int terrain1; // TerrainTypeCode
    public int terrain2; // TerrainTypeCode
    public int firstAnimId;
    public int maxFrame;
    public int firstBlockId;

    public readonly int[,] blockOffset = new int[InternalConstants.MAX_ROCK_HEIGHT, InternalConstants.MAX_ROCK_WIDTH];

    public bool IsTerrainValid(int terrainType)
    {
        return terrainType >= terrain1 && terrainType <= terrain2;
    }
}

public class RockBlockInfo
{
    public int locX;
    public int locY;
    public int rockId; // id in RockRec/RockInfo
    public int firstBitmap; // id in RockBitmapRec/RockBitmapInfo
}

public class RockBitmapInfo
{
    public int locX; // checking only
    public int locY; // checking only
    public int frame; // checking only
    public byte[] bitmap;
    public int bitmapWidth;
    public int bitmapHeight;
    private IntPtr _texture;
    
    public IntPtr GetTexture(Graphics graphics)
    {
        if (_texture == default)
            _texture = graphics.CreateTextureFromBmp(bitmap, bitmapWidth, bitmapHeight);

        return _texture;
    }
}

public class RockAnimInfo
{
    public int frame; // checking only
    public int delay;
    public int nextFrame;
    public int altNext;

    public int ChooseNext(int path)
    {
        return path != 0 ? nextFrame : altNext;
    }
}

public class RockRes
{
    private RockInfo[] _rockInfos;
    private RockBlockInfo[] _rockBlockInfos;
    private RockBitmapInfo[] _rockBitmapInfos;
    private RockAnimInfo[] _rockAnimInfos;
    
    private RockAnimInfo _unanimatedInfo;

    private readonly ResourceDb _resources;

    public RockRes()
    {
        _unanimatedInfo = new RockAnimInfo();
        _unanimatedInfo.frame = 1;
        _unanimatedInfo.delay = 99;
        _unanimatedInfo.nextFrame = 1;
        _unanimatedInfo.altNext = 1;
        
        _resources = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_ROCK{Sys.Instance.Config.terrain_set}.RES");

        LoadInfo();
        LoadBitmapInfo();
        LoadBlockInfo();
        LoadAnimInfo();
    }

    public RockInfo GetRockInfo(int rockId)
    {
        return _rockInfos[rockId - 1];
    }

    public RockBlockInfo GetBlockInfo(int rockBlockId)
    {
        return _rockBlockInfos[rockBlockId - 1];
    }

    public RockBitmapInfo GetBitmapInfo(int rockBitmapId)
    {
        return _rockBitmapInfos[rockBitmapId - 1];
    }

    public RockAnimInfo GetAnimInfo(int rockAnimId)
    {
        return rockAnimId == -1 ? _unanimatedInfo : _rockAnimInfos[rockAnimId - 1];
    }

    public int GetBitmapId(int rockBlockId, int curFrame)
    {
        RockBlockInfo rockBlockInfo = GetBlockInfo(rockBlockId);
        return rockBlockInfo.firstBitmap + curFrame - 1;
    }

    public int GetAnimId(int rockId, int curFrame)
    {
        RockInfo rockInfo = GetRockInfo(rockId);
        return rockInfo.firstAnimId != 0 ? rockInfo.firstAnimId + (curFrame - 1) : -1;
    }
    
    public int ChooseNext(int rockId, int curFrame, int path)
    {
        // -------- validate rockId ---------//
        RockInfo rockInfo = GetRockInfo(rockId);

        // -------- validate curFrame ----------//
        RockAnimInfo rockAnimInfo = GetAnimInfo(GetAnimId(rockId, curFrame));

        // -------- validate frame, next_frame and alt_next in rockAnimInfo -------/
        return rockAnimInfo.ChooseNext(path);
    }

    public int Search(string rockTypes, int minWidth, int maxWidth, int minHeight, int maxHeight,
        int animatedFlag = -1, bool findFirst = false, int terrainType = 0)
    {
        // -------- search a rock by rock_type, width and height ---------//
        int rockId = 0;
        int findCount = 0;

        for (int i = 0; i < _rockInfos.Length; i++)
        {
            RockInfo rockInfo = _rockInfos[i];
            if ((string.IsNullOrEmpty(rockTypes) || rockTypes.Contains(rockInfo.rockType)) &&
                (terrainType == 0 || rockInfo.IsTerrainValid(terrainType)) &&
                rockInfo.locWidth >= minWidth && rockInfo.locWidth <= maxWidth &&
                rockInfo.locHeight >= minHeight && rockInfo.locHeight <= maxHeight &&
                (animatedFlag < 0 || animatedFlag == 0 && rockInfo.maxFrame == 1 || animatedFlag > 0 && rockInfo.maxFrame > 1))
            {
                findCount++;
                if (findFirst)
                {
                    rockId = i + 1;
                    break;
                }
                else if (Misc.Random(findCount) == 0)
                {
                    rockId = i + 1;
                }
            }
        }

        return rockId;
    }

    public int LocateBlock(int rockId, int locX, int locY)
    {
        if (locX < InternalConstants.MAX_ROCK_WIDTH && locY < InternalConstants.MAX_ROCK_HEIGHT)
        {
            // if xLoc and yLoc is small enough, the block of offset x and offset y can be found in block_offset,
            return GetRockInfo(rockId).blockOffset[locY, locX];
        }
        else
        {
            // otherwise, linear search
            for (int rockBlockId = GetRockInfo(rockId).firstBlockId; rockBlockId <= _rockBlockInfos.Length; rockBlockId++)
            {
                RockBlockInfo rockBlockInfo = GetBlockInfo(rockBlockId);
                if (rockBlockInfo.rockId != rockId)
                    break;
                if (rockBlockInfo.locX == locX && rockBlockInfo.locY == locY)
                    return rockBlockId;
            }

            return 0;
        }
    }

    private void LoadInfo()
    {
        Database dbRock = new Database($"{Sys.GameDataFolder}/Resource/ROCK{Sys.Instance.Config.terrain_set}.RES");
        _rockInfos = new RockInfo[dbRock.RecordCount];

        for (int i = 0; i < _rockInfos.Length; i++)
        {
            RockRec rockRec = new RockRec(dbRock, i + 1);
            RockInfo rockInfo = new RockInfo();
            _rockInfos[i] = rockInfo;

            rockInfo.rockName = Misc.ToString(rockRec.rockId);
            rockInfo.rockType = rockRec.rockType;
            rockInfo.locWidth = Misc.ToInt32(rockRec.locWidth);
            rockInfo.locHeight = Misc.ToInt32(rockRec.locHeight);
            if (rockRec.terrain1 == 0 || rockRec.terrain1 == ' ')
                rockInfo.terrain1 = 0;
            else
                rockInfo.terrain1 = TerrainRes.terrain_code(rockRec.terrain1);
            if (rockRec.terrain2 == 0 || rockRec.terrain2 == ' ')
                rockInfo.terrain2 = 0;
            else
                rockInfo.terrain2 = TerrainRes.terrain_code(rockRec.terrain2);
            rockInfo.firstAnimId = Misc.ToInt32(rockRec.firstAnimRecno);
            if (rockInfo.firstAnimId != 0)
                rockInfo.maxFrame = Misc.ToInt32(rockRec.maxFrame);
            else
                rockInfo.maxFrame = 1; // unanimated, rock anim recno must be -1

            rockInfo.firstBlockId = 0;
        }
    }

    private void LoadBlockInfo()
    {
        Database dbRock = new Database($"{Sys.GameDataFolder}/Resource/ROCKBLK{Sys.Instance.Config.terrain_set}.RES");
        _rockBlockInfos = new RockBlockInfo[dbRock.RecordCount];

        for (int i = 0; i < _rockBlockInfos.Length; i++)
        {
            RockBlockRec rockBlockRec = new RockBlockRec(dbRock, i + 1);
            RockBlockInfo rockBlockInfo = new RockBlockInfo();
            _rockBlockInfos[i] = rockBlockInfo;

            rockBlockInfo.locX = Misc.ToInt32(rockBlockRec.locX);
            rockBlockInfo.locY = Misc.ToInt32(rockBlockRec.locY);
            rockBlockInfo.rockId = Misc.ToInt32(rockBlockRec.rockRecno);
            rockBlockInfo.firstBitmap = Misc.ToInt32(rockBlockRec.firstBitmap);

            // ------- validate rock_recno --------//
            RockInfo rockInfo = _rockInfos[rockBlockInfo.rockId - 1];
            if (rockInfo.firstBlockId == 0)
                rockInfo.firstBlockId = i + 1;

            // ------- set block_offset in rockInfo ----------//
            if (rockBlockInfo.locX < InternalConstants.MAX_ROCK_WIDTH && rockBlockInfo.locY < InternalConstants.MAX_ROCK_HEIGHT)
            {
                // store the rockBlockRecno (i.e. i+1) in rockInfo.block_offset
                // in order to find a rock block from rock recno and x offset, y offset
                // thus make RockRes::locate_block() faster
                rockInfo.blockOffset[rockBlockInfo.locY, rockBlockInfo.locX] = i + 1;
            }
        }
    }

    private void LoadBitmapInfo()
    {
        Database dbRock = new Database($"{Sys.GameDataFolder}/Resource/ROCKBMP{Sys.Instance.Config.terrain_set}.RES");
        _rockBitmapInfos = new RockBitmapInfo[dbRock.RecordCount];

        for (int i = 0; i < _rockBitmapInfos.Length; i++)
        {
            RockBitmapRec rockBitmapRec = new RockBitmapRec(dbRock, i + 1);
            RockBitmapInfo rockBitmapInfo = new RockBitmapInfo();
            _rockBitmapInfos[i] = rockBitmapInfo;

            rockBitmapInfo.locX = Misc.ToInt32(rockBitmapRec.locX);
            rockBitmapInfo.locY = Misc.ToInt32(rockBitmapRec.locY);
            rockBitmapInfo.frame = Misc.ToInt32(rockBitmapRec.frame);

            int bitmapOffset = BitConverter.ToInt32(rockBitmapRec.bitmap, 0);
            rockBitmapInfo.bitmap = _resources.Read(bitmapOffset);
            rockBitmapInfo.bitmapWidth = BitConverter.ToInt16(rockBitmapInfo.bitmap, 0);
            rockBitmapInfo.bitmapHeight = BitConverter.ToInt16(rockBitmapInfo.bitmap, 2);
            rockBitmapInfo.bitmap = rockBitmapInfo.bitmap.Skip(4).ToArray();
        }
    }

    private void LoadAnimInfo()
    {
        Database dbRock = new Database($"{Sys.GameDataFolder}/Resource/ROCKANI{Sys.Instance.Config.terrain_set}.RES");
        _rockAnimInfos = new RockAnimInfo[dbRock.RecordCount];

        for (int i = 0; i < _rockAnimInfos.Length; i++)
        {
            RockAnimRec rockAnimRec = new RockAnimRec(dbRock, i + 1);
            RockAnimInfo rockAnimInfo = new RockAnimInfo();
            _rockAnimInfos[i] = rockAnimInfo;

            rockAnimInfo.frame = Misc.ToInt32(rockAnimRec.frame);
            rockAnimInfo.delay = Misc.ToInt32(rockAnimRec.delay);
            rockAnimInfo.nextFrame = Misc.ToInt32(rockAnimRec.nextFrame);
            rockAnimInfo.altNext = Misc.ToInt32(rockAnimRec.altNext);
            if (rockAnimInfo.altNext == 0)
                rockAnimInfo.altNext = rockAnimInfo.nextFrame;
        }
    }
}