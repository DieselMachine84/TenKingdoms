using System;
using System.Linq;

namespace TenKingdoms;

public class RawRec
{
    private const int NAME_LEN = 12;
    private const int TERA_TYPE_LEN = 1;

    public char[] name = new char[NAME_LEN];
    public char[] tera_type = new char[TERA_TYPE_LEN];

    public RawRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < name.Length; i++, dataIndex++)
            name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < tera_type.Length; i++, dataIndex++)
            tera_type[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
    }
}

public class RawInfo
{
    public int RawId { get; set; }
    public string Name { get; set; }
    public int TeraType { get; set; }

    public byte[] LargeProductIcon { get; set; }
    public int LargeProductIconWidth { get; set; }
    public int LargeProductIconHeight { get; set; }
    private IntPtr _largeProductTexture;
    public byte[] SmallProductIcon { get; set; }
    public int SmallProductIconWidth { get; set; }
    public int SmallProductIconHeight { get; set; }
    private IntPtr _smallProductTexture;
    public byte[] LargeRawIcon { get; set; }
    public int LargeRawIconWidth { get; set; }
    public int LargeRawIconHeight { get; set; }
    private IntPtr _largeRawTexture;
    public byte[] SmallRawIcon { get; set; }
    public int SmallRawIconWidth { get; set; }
    public int SmallRawIconHeight { get; set; }
    private IntPtr _smallRawTexture;

    public IntPtr GetLargeProductTexture(Graphics graphics)
    {
        if (_largeProductTexture == default)
        {
            byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(LargeProductIcon, LargeProductIconWidth, LargeProductIconHeight);
            _largeProductTexture = graphics.CreateTextureFromBmp(decompressedBitmap, LargeProductIconWidth, LargeProductIconHeight);
        }

        return _largeProductTexture;
    }

    public IntPtr GetSmallProductTexture(Graphics graphics)
    {
        if (_smallProductTexture == default)
        {
            byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(SmallProductIcon, SmallProductIconWidth, SmallProductIconHeight);
            _smallProductTexture = graphics.CreateTextureFromBmp(decompressedBitmap, SmallProductIconWidth, SmallProductIconHeight);
        }

        return _smallProductTexture;
    }

    public IntPtr GetLargeRawTexture(Graphics graphics)
    {
        if (_largeRawTexture == default)
        {
            byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(LargeRawIcon, LargeRawIconWidth, LargeRawIconHeight);
            _largeRawTexture = graphics.CreateTextureFromBmp(decompressedBitmap, LargeRawIconWidth, LargeRawIconHeight);
        }

        return _largeRawTexture;
    }

    public IntPtr GetSmallRawTexture(Graphics graphics)
    {
        if (_smallRawTexture == default)
        {
            byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(SmallRawIcon, SmallRawIconWidth, SmallRawIconHeight);
            _smallRawTexture = graphics.CreateTextureFromBmp(decompressedBitmap, SmallRawIconWidth, SmallRawIconHeight);
        }

        return _smallRawTexture;
    }
}

public class RawRes
{
    private RawInfo[] _rawInfos;

    private GameSet GameSet { get; }

    public RawRes(GameSet gameSet)
    {
        GameSet = gameSet;

        LoadAllInfo();
    }

    public RawInfo this[int rawId] => _rawInfos[rawId - 1];

    private void LoadAllInfo()
    {
        Resource iconResource = new Resource($"{Sys.GameDataFolder}/Resource/I_RAW.RES");
        Database dbRaw = GameSet.OpenDb("RAW");
        _rawInfos = new RawInfo[dbRaw.RecordCount];

        for (int i = 0; i < _rawInfos.Length; i++)
        {
            RawRec rawRec = new RawRec(dbRaw, i + 1);
            RawInfo rawInfo = new RawInfo();
            _rawInfos[i] = rawInfo;

            rawInfo.RawId = i + 1;
            rawInfo.Name = Misc.ToString(rawRec.name);
            rawInfo.TeraType = Misc.ToInt32(rawRec.tera_type);
            rawInfo.LargeProductIcon = iconResource.Read(i + 1);
            rawInfo.LargeProductIconWidth = BitConverter.ToInt16(rawInfo.LargeProductIcon, 0);
            rawInfo.LargeProductIconHeight = BitConverter.ToInt16(rawInfo.LargeProductIcon, 2);
            rawInfo.LargeProductIcon = rawInfo.LargeProductIcon.Skip(4).ToArray();
            rawInfo.SmallProductIcon = iconResource.Read(GameConstants.MAX_RAW + i + 1);
            rawInfo.SmallProductIconWidth = BitConverter.ToInt16(rawInfo.SmallProductIcon, 0);
            rawInfo.SmallProductIconHeight = BitConverter.ToInt16(rawInfo.SmallProductIcon, 2);
            rawInfo.SmallProductIcon = rawInfo.SmallProductIcon.Skip(4).ToArray();
            rawInfo.LargeRawIcon = iconResource.Read(GameConstants.MAX_RAW * 2 + i + 1);
            rawInfo.LargeRawIconWidth = BitConverter.ToInt16(rawInfo.LargeRawIcon, 0);
            rawInfo.LargeRawIconHeight = BitConverter.ToInt16(rawInfo.LargeRawIcon, 2);
            rawInfo.LargeRawIcon = rawInfo.LargeRawIcon.Skip(4).ToArray();
            rawInfo.SmallRawIcon = iconResource.Read(GameConstants.MAX_RAW * 3 + i + 1);
            rawInfo.SmallRawIconWidth = BitConverter.ToInt16(rawInfo.SmallRawIcon, 0);
            rawInfo.SmallRawIconHeight = BitConverter.ToInt16(rawInfo.SmallRawIcon, 2);
            rawInfo.SmallRawIcon = rawInfo.SmallRawIcon.Skip(4).ToArray();
        }
    }
}