using System;
using System.Linq;

namespace TenKingdoms;

public class CursorType
{
    public const int NORMAL = 1; 
    public const int NORMAL_OWN = 2;
    public const int NORMAL_ENEMY = 3;
    public const int UNIT = 4;
    public const int UNIT_C = 5;
    public const int UNIT_O = 6;
    public const int C_TOWN = 7;
    public const int C_TOWN_C = 8;
    public const int C_TOWN_O = 9;
    public const int O_TOWN = 10;
    public const int O_TOWN_C = 11;
    public const int O_TOWN_O = 12;
    public const int C_FIRM = 13;
    public const int C_FIRM_C = 14;
    public const int C_FIRM_O = 15;
    public const int O_FIRM = 16;
    public const int O_FIRM_C = 17;
    public const int O_FIRM_O = 18;

    public const int WAITING = 19;
    public const int BUILD = 20;
    public const int DESTRUCT = 21;
    public const int ASSIGN = 22;
    public const int CARAVAN_STOP = 23;
    public const int CANT_CARAVAN_STOP = 24;
    public const int SHIP_STOP = 25;
    public const int CANT_SHIP_STOP = 26;
    public const int BURN = 27;
    public const int SETTLE_0 = 28;
    public const int SETTLE_1 = 29;
    public const int SETTLE_2 = 30;
    public const int SETTLE_3 = 31;
    public const int SETTLE_4 = 32;
    public const int SETTLE_5 = 33;
    public const int SETTLE_6 = 34;
    public const int SETTLE_7 = 35;
    public const int ON_LINK = 36;
    public const int TRIGGER_EXPLODE = 37;
    public const int CAPTURE_FIRM = 38;
    public const int ENCYC = 39;
}

public class CursorRec
{
    private const int FILE_NAME_LEN = 8;
    private const int HOT_SPOT_LEN = 3;
    private const int BITMAP_PTR_LEN = 4;
    
    public char[] file_name = new char[FILE_NAME_LEN];
    public char[] hot_spot_x = new char[HOT_SPOT_LEN];
    public char[] hot_spot_y = new char[HOT_SPOT_LEN];
    public byte[] bitmap_ptr = new byte[BITMAP_PTR_LEN];

    public CursorRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < file_name.Length; i++, dataIndex++)
            file_name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < hot_spot_x.Length; i++, dataIndex++)
            hot_spot_x[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < hot_spot_y.Length; i++, dataIndex++)
            hot_spot_y[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < bitmap_ptr.Length; i++, dataIndex++)
            bitmap_ptr[i] = db.ReadByte(recNo, dataIndex);
    }
}

public class CursorInfo
{
    public int HotSpotX { get; set; }
    public int HotSpotY { get; set; }
    
    public byte[] bitmap;
    public int bitmapWidth;
    public int bitmapHeight;
    private IntPtr _cursor;
    
    public IntPtr GetCursor(Graphics graphics)
    {
        if (_cursor == default)
        {
            byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(bitmap, bitmapWidth, bitmapHeight);
            _cursor = graphics.CreateCursor(decompressedBitmap, bitmapWidth, bitmapHeight, HotSpotX, HotSpotY);
        }

        return _cursor;
    }
}

public class CursorRes
{
    private CursorInfo[] _cursorInfos;
    
    public CursorRes()
    {
        LoadCursorInfo();
    }
    
    public CursorInfo this[int cursorId] => _cursorInfos[cursorId - 1];

    private void LoadCursorInfo()
    {
        ResourceDb resources = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_CURSOR.RES");
        Database dbCursor = new Database($"{Sys.GameDataFolder}/Resource/CURSOR.RES");
        _cursorInfos = new CursorInfo[dbCursor.RecordCount];

        for (int i = 0; i < _cursorInfos.Length; i++)
        {
            CursorRec cursorRec = new CursorRec(dbCursor, i + 1);
            CursorInfo cursorInfo = new CursorInfo();
            _cursorInfos[i] = cursorInfo;
            
            int bitmapOffset = BitConverter.ToInt32(cursorRec.bitmap_ptr, 0);
            cursorInfo.bitmap = resources.Read(bitmapOffset);
            cursorInfo.bitmapWidth = BitConverter.ToInt16(cursorInfo.bitmap, 0);
            cursorInfo.bitmapHeight = BitConverter.ToInt16(cursorInfo.bitmap, 2);
            cursorInfo.bitmap = cursorInfo.bitmap.Skip(4).ToArray();
            cursorInfo.HotSpotX = Misc.ToInt32(cursorRec.hot_spot_x);
            cursorInfo.HotSpotY = Misc.ToInt32(cursorRec.hot_spot_y);
        }
    }
}