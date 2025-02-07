using System;
using System.Collections.Generic;
using System.Linq;

namespace TenKingdoms;

public class SpriteFrameRec
{
    public const int NAME_LEN = 8;
    public const int ACTION_LEN = 2;
    public const int DIR_LEN = 2;
    public const int FRAME_ID_LEN = 2;
    public const int OFFSET_LEN = 4;
    public const int WIDTH_LEN = 3;
    public const int HEIGHT_LEN = 3;
    public const int FILE_NAME_LEN = 8;
    public const int BITMAP_OFFSET_LEN = 4;

    public char[] sprite_name = new char[NAME_LEN];
    public char[] action = new char[ACTION_LEN];
    public char[] dir = new char[DIR_LEN];
    public char[] frame_id = new char[FRAME_ID_LEN];

    public char[] offset_x = new char[OFFSET_LEN];
    public char[] offset_y = new char[OFFSET_LEN];
    public char[] width = new char[WIDTH_LEN];
    public char[] height = new char[HEIGHT_LEN];

    public char[] file_name = new char[FILE_NAME_LEN];
    public byte[] bitmap_offset = new byte[BITMAP_OFFSET_LEN];

    public SpriteFrameRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < sprite_name.Length; i++, dataIndex++)
            sprite_name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < action.Length; i++, dataIndex++)
            action[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < dir.Length; i++, dataIndex++)
            dir[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < frame_id.Length; i++, dataIndex++)
            frame_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < offset_x.Length; i++, dataIndex++)
            offset_x[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < offset_y.Length; i++, dataIndex++)
            offset_y[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < width.Length; i++, dataIndex++)
            width[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < height.Length; i++, dataIndex++)
            height[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < file_name.Length; i++, dataIndex++)
            file_name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < bitmap_offset.Length; i++, dataIndex++)
            bitmap_offset[i] = db.ReadByte(recNo, dataIndex);
    }
}

public class SpriteFrame
{
    public int offset_x;
    public int offset_y;
    public int width;
    public int height;
    public int bitmap_offset;
    private Dictionary<int, IntPtr> unitTextures = new Dictionary<int, nint>();
    private IntPtr nonUnitTexture;

    public IntPtr GetUnitTexture(Graphics graphics, SpriteInfo spriteInfo, int nationId, bool isSelected)
    {
        int colorScheme = ColorRemap.ColorSchemes[nationId];
        int textureKey = ColorRemap.GetTextureKey(colorScheme, isSelected);
        if (!unitTextures.ContainsKey(textureKey))
        {
            byte[] bitmaps = spriteInfo.res_bitmap.ReadFull();
            int bitmapSize = BitConverter.ToInt16(bitmaps, bitmap_offset);
            byte[] bitmap = bitmaps.Skip(bitmap_offset + sizeof(Int32) + 2 * sizeof(Int16)).Take(bitmapSize).ToArray();
            byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(bitmap, width, height,
                ColorRemap.GetColorRemap(colorScheme, isSelected).ColorTable);
            IntPtr texture = graphics.CreateTextureFromBmp(decompressedBitmap, width, height);
            unitTextures.Add(textureKey, texture);
        }
        
        return unitTextures[textureKey];
    }
}

public class SpriteFrameRes
{
    private const string SPRITE_FRAME_DB = "SFRAME";

    private SpriteFrame[] spriteFrames;

    public GameSet GameSet { get; }

    public SpriteFrameRes(GameSet gameSet)
    {
        GameSet = gameSet;

        LoadInfo();
    }

    public SpriteFrame this[int recNo] => spriteFrames[recNo - 1];

    private void LoadInfo()
    {
        Database dbSpriteFrame = GameSet.OpenDb(SPRITE_FRAME_DB);
        spriteFrames = new SpriteFrame[dbSpriteFrame.RecordCount];

        for (int i = 0; i < spriteFrames.Length; i++)
        {
            SpriteFrameRec frameRec = new SpriteFrameRec(dbSpriteFrame, i + 1);
            SpriteFrame spriteFrame = new SpriteFrame();
            spriteFrames[i] = spriteFrame;

            spriteFrame.offset_x = Misc.ToInt32(frameRec.offset_x);
            spriteFrame.offset_y = Misc.ToInt32(frameRec.offset_y);

            spriteFrame.width = Misc.ToInt32(frameRec.width);
            spriteFrame.height = Misc.ToInt32(frameRec.height);

            spriteFrame.bitmap_offset = BitConverter.ToInt32(frameRec.bitmap_offset, 0);
        }
    }
}
