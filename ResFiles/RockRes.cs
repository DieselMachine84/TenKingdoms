using System;
using System.Linq;

namespace TenKingdoms;

public class RockRec
{
    private const int ROCKID_LEN = 5;
    private const int LOC_LEN = 2;
    private const int RECNO_LEN = 4;
    private const int MAX_FRAME_LEN = 2;

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
    private const int ROCKID_LEN = 5;
    private const int LOC_LEN = 2;
    private const int RECNO_LEN = 4;

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
    private const int ROCKID_LEN = 5;
    private const int LOC_LEN = 2;
    private const int FRAME_NO_LEN = 2;
    private const int FILE_NAME_LEN = 8;
    private const int BITMAP_PTR_LEN = 4;

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
    private const int ROCKID_LEN = 5;
    private const int FRAME_NO_LEN = 2;
    private const int DELAY_LEN = 3;

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
    
    public string RockName { get; set; }
    public char RockType { get; set; }
    public int LocWidth { get; set; }
    public int LocHeight { get; set; }
    public int Terrain1 { get; set; } // TerrainTypeCode
    public int Terrain2 { get; set; } // TerrainTypeCode
    public int FirstAnimId { get; set; }
    public int MaxFrame { get; set; }
    public int FirstBlockId { get; set; }

    public int[,] BlockOffset { get; set; } = new int[InternalConstants.MAX_ROCK_HEIGHT, InternalConstants.MAX_ROCK_WIDTH];

    public bool IsTerrainValid(int terrainType)
    {
        return terrainType >= Terrain1 && terrainType <= Terrain2;
    }
}

public class RockBlockInfo
{
    public int LocX { get; set; }
    public int LocY { get; set; }
    public int RockId { get; set; } // id in RockRec/RockInfo
    public int FirstBitmap { get; set; } // id in RockBitmapRec/RockBitmapInfo
}

public class RockBitmapInfo
{
    public int LocX { get; set; } // checking only
    public int LocY { get; set; } // checking only
    public int Frame { get; set; } // checking only
    public byte[] Bitmap { get; set; }
    public int BitmapWidth { get; set; }
    public int BitmapHeight { get; set; }
    private IntPtr _texture;
    
    public IntPtr GetTexture(Graphics graphics)
    {
        if (_texture == default)
            _texture = graphics.CreateTextureFromBmp(Bitmap, BitmapWidth, BitmapHeight);

        return _texture;
    }
}

public class RockAnimInfo
{
    public int Frame { get; set; } // checking only
    public int Delay { get; set; }
    public int NextFrame { get; set; }
    public int AltNext { get; set; }

    public int ChooseNext(int path)
    {
        return path != 0 ? NextFrame : AltNext;
    }
}

public class RockRes
{
    private RockInfo[] _rockInfos;
    private RockBlockInfo[] _rockBlockInfos;
    private RockBitmapInfo[] _rockBitmapInfos;
    private RockAnimInfo[] _rockAnimInfos;
    
    private RockAnimInfo _unanimatedInfo;

    public RockRes()
    {
        _unanimatedInfo = new RockAnimInfo();
        _unanimatedInfo.Frame = 1;
        _unanimatedInfo.Delay = 99;
        _unanimatedInfo.NextFrame = 1;
        _unanimatedInfo.AltNext = 1;
        
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
        return rockBlockInfo.FirstBitmap + curFrame - 1;
    }

    public int GetAnimId(int rockId, int curFrame)
    {
        RockInfo rockInfo = GetRockInfo(rockId);
        return rockInfo.FirstAnimId != 0 ? rockInfo.FirstAnimId + (curFrame - 1) : -1;
    }
    
    public int ChooseNext(int rockId, int curFrame, int path)
    {
        RockAnimInfo rockAnimInfo = GetAnimInfo(GetAnimId(rockId, curFrame));
        return rockAnimInfo.ChooseNext(path);
    }

    public int Search(string rockTypes, int minWidth, int maxWidth, int minHeight, int maxHeight,
        int animatedFlag = -1, bool findFirst = false, int terrainType = 0)
    {
        // -------- search a rock by RockType, width and height ---------//
        int rockId = 0;
        int findCount = 0;

        for (int i = 0; i < _rockInfos.Length; i++)
        {
            RockInfo rockInfo = _rockInfos[i];
            if ((string.IsNullOrEmpty(rockTypes) || rockTypes.Contains(rockInfo.RockType)) &&
                (terrainType == 0 || rockInfo.IsTerrainValid(terrainType)) &&
                rockInfo.LocWidth >= minWidth && rockInfo.LocWidth <= maxWidth &&
                rockInfo.LocHeight >= minHeight && rockInfo.LocHeight <= maxHeight &&
                (animatedFlag < 0 || animatedFlag == 0 && rockInfo.MaxFrame == 1 || animatedFlag > 0 && rockInfo.MaxFrame > 1))
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
            // if locX and locY is small enough, the block of offset x and offset y can be found in BlockOffset,
            return GetRockInfo(rockId).BlockOffset[locY, locX];
        }
        else
        {
            // otherwise, linear search
            for (int rockBlockId = GetRockInfo(rockId).FirstBlockId; rockBlockId <= _rockBlockInfos.Length; rockBlockId++)
            {
                RockBlockInfo rockBlockInfo = GetBlockInfo(rockBlockId);
                if (rockBlockInfo.RockId != rockId)
                    break;
                if (rockBlockInfo.LocX == locX && rockBlockInfo.LocY == locY)
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

            rockInfo.RockName = Misc.ToString(rockRec.rockId);
            rockInfo.RockType = rockRec.rockType;
            rockInfo.LocWidth = Misc.ToInt32(rockRec.locWidth);
            rockInfo.LocHeight = Misc.ToInt32(rockRec.locHeight);
            if (rockRec.terrain1 == 0 || rockRec.terrain1 == ' ')
                rockInfo.Terrain1 = 0;
            else
                rockInfo.Terrain1 = TerrainRes.TerrainCode(rockRec.terrain1);
            if (rockRec.terrain2 == 0 || rockRec.terrain2 == ' ')
                rockInfo.Terrain2 = 0;
            else
                rockInfo.Terrain2 = TerrainRes.TerrainCode(rockRec.terrain2);
            rockInfo.FirstAnimId = Misc.ToInt32(rockRec.firstAnimRecno);
            if (rockInfo.FirstAnimId != 0)
                rockInfo.MaxFrame = Misc.ToInt32(rockRec.maxFrame);
            else
                rockInfo.MaxFrame = 1; // unanimated, rock anim id must be -1

            rockInfo.FirstBlockId = 0;
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

            rockBlockInfo.LocX = Misc.ToInt32(rockBlockRec.locX);
            rockBlockInfo.LocY = Misc.ToInt32(rockBlockRec.locY);
            rockBlockInfo.RockId = Misc.ToInt32(rockBlockRec.rockRecno);
            rockBlockInfo.FirstBitmap = Misc.ToInt32(rockBlockRec.firstBitmap);

            RockInfo rockInfo = _rockInfos[rockBlockInfo.RockId - 1];
            if (rockInfo.FirstBlockId == 0)
                rockInfo.FirstBlockId = i + 1;

            // ------- set BlockOffset in rockInfo ----------//
            if (rockBlockInfo.LocX < InternalConstants.MAX_ROCK_WIDTH && rockBlockInfo.LocY < InternalConstants.MAX_ROCK_HEIGHT)
            {
                // store the rockBlockId (i.e. i+1) in rockInfo.BlockOffset
                // in order to find a rock block from rock id and x offset, y offset
                // thus make RockRes.LocateBlock() faster
                rockInfo.BlockOffset[rockBlockInfo.LocY, rockBlockInfo.LocX] = i + 1;
            }
        }
    }

    private void LoadBitmapInfo()
    {
        ResourceDb rockBitmaps = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_ROCK{Sys.Instance.Config.terrain_set}.RES");
        Database dbRock = new Database($"{Sys.GameDataFolder}/Resource/ROCKBMP{Sys.Instance.Config.terrain_set}.RES");
        _rockBitmapInfos = new RockBitmapInfo[dbRock.RecordCount];

        for (int i = 0; i < _rockBitmapInfos.Length; i++)
        {
            RockBitmapRec rockBitmapRec = new RockBitmapRec(dbRock, i + 1);
            RockBitmapInfo rockBitmapInfo = new RockBitmapInfo();
            _rockBitmapInfos[i] = rockBitmapInfo;

            rockBitmapInfo.LocX = Misc.ToInt32(rockBitmapRec.locX);
            rockBitmapInfo.LocY = Misc.ToInt32(rockBitmapRec.locY);
            rockBitmapInfo.Frame = Misc.ToInt32(rockBitmapRec.frame);

            int bitmapOffset = BitConverter.ToInt32(rockBitmapRec.bitmap, 0);
            rockBitmapInfo.Bitmap = rockBitmaps.Read(bitmapOffset);
            rockBitmapInfo.BitmapWidth = BitConverter.ToInt16(rockBitmapInfo.Bitmap, 0);
            rockBitmapInfo.BitmapHeight = BitConverter.ToInt16(rockBitmapInfo.Bitmap, 2);
            rockBitmapInfo.Bitmap = rockBitmapInfo.Bitmap.Skip(4).ToArray();
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

            rockAnimInfo.Frame = Misc.ToInt32(rockAnimRec.frame);
            rockAnimInfo.Delay = Misc.ToInt32(rockAnimRec.delay);
            rockAnimInfo.NextFrame = Misc.ToInt32(rockAnimRec.nextFrame);
            rockAnimInfo.AltNext = Misc.ToInt32(rockAnimRec.altNext);
            if (rockAnimInfo.AltNext == 0)
                rockAnimInfo.AltNext = rockAnimInfo.NextFrame;
        }
    }
}