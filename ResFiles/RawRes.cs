using System;
using System.Linq;

namespace TenKingdoms;

public class RawRec
{
    public const int NAME_LEN = 12;
    public const int TERA_TYPE_LEN = 1;

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
    public int rawId;
    public string name;
    public int teraType;

    public byte[] largeProductIcon;
    public int largeProductIconWidth;
    public int largeProductIconHeight;
    private IntPtr _largeProductTexture;
    public byte[] smallProductIcon;
    public int smallProductIconWidth;
    public int smallProductIconHeight;
    private IntPtr _smallProductTexture;
    public byte[] largeRawIcon;
    public int largeRawIconWidth;
    public int largeRawIconHeight;
    private IntPtr _largeRawTexture;
    public byte[] smallRawIcon;
    public int smallRawIconWidth;
    public int smallRawIconHeight;
    private IntPtr _smallRawTexture;

    public IntPtr GetLargeProductTexture(Graphics graphics)
    {
        if (_largeProductTexture == default)
        {
            byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(largeProductIcon, largeProductIconWidth, largeProductIconHeight);
            _largeProductTexture = graphics.CreateTextureFromBmp(decompressedBitmap, largeProductIconWidth, largeProductIconHeight);
        }

        return _largeProductTexture;
    }

    public IntPtr GetSmallProductTexture(Graphics graphics)
    {
        if (_smallProductTexture == default)
        {
            byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(smallProductIcon, smallProductIconWidth, smallProductIconHeight);
            _smallProductTexture = graphics.CreateTextureFromBmp(decompressedBitmap, smallProductIconWidth, smallProductIconHeight);
        }

        return _smallProductTexture;
    }

    public IntPtr GetLargeRawTexture(Graphics graphics)
    {
        if (_largeRawTexture == default)
        {
            byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(largeRawIcon, largeRawIconWidth, largeRawIconHeight);
            _largeRawTexture = graphics.CreateTextureFromBmp(decompressedBitmap, largeRawIconWidth, largeRawIconHeight);
        }

        return _largeRawTexture;
    }

    public IntPtr GetSmallRawTexture(Graphics graphics)
    {
        if (_smallRawTexture == default)
        {
            byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(smallRawIcon, smallRawIconWidth, smallRawIconHeight);
            _smallRawTexture = graphics.CreateTextureFromBmp(decompressedBitmap, smallRawIconWidth, smallRawIconHeight);
        }

        return _smallRawTexture;
    }
}

public class RawRes
{
    private const string RAW_DB = "RAW";

    private RawInfo[] _rawInfos;

    private readonly Resource _iconResource;

    private GameSet GameSet { get; }

    public RawRes(GameSet gameSet)
    {
        GameSet = gameSet;

        _iconResource = new Resource($"{Sys.GameDataFolder}/Resource/I_RAW.RES");

        LoadAllInfo();
    }

    public RawInfo this[int rawId] => _rawInfos[rawId - 1];

    private void LoadAllInfo()
    {
        Database dbRaw = GameSet.OpenDb(RAW_DB);
        _rawInfos = new RawInfo[dbRaw.RecordCount];

        for (int i = 0; i < _rawInfos.Length; i++)
        {
            RawRec rawRec = new RawRec(dbRaw, i + 1);
            RawInfo rawInfo = new RawInfo();
            _rawInfos[i] = rawInfo;

            rawInfo.rawId = i + 1;
            rawInfo.name = Misc.ToString(rawRec.name);
            rawInfo.teraType = Misc.ToInt32(rawRec.tera_type);
            rawInfo.largeProductIcon = _iconResource.Read(i + 1);
            rawInfo.largeProductIconWidth = BitConverter.ToInt16(rawInfo.largeProductIcon, 0);
            rawInfo.largeProductIconHeight = BitConverter.ToInt16(rawInfo.largeProductIcon, 2);
            rawInfo.largeProductIcon = rawInfo.largeProductIcon.Skip(4).ToArray();
            rawInfo.smallProductIcon = _iconResource.Read(GameConstants.MAX_RAW + i + 1);
            rawInfo.smallProductIconWidth = BitConverter.ToInt16(rawInfo.smallProductIcon, 0);
            rawInfo.smallProductIconHeight = BitConverter.ToInt16(rawInfo.smallProductIcon, 2);
            rawInfo.smallProductIcon = rawInfo.smallProductIcon.Skip(4).ToArray();
            rawInfo.largeRawIcon = _iconResource.Read(GameConstants.MAX_RAW * 2 + i + 1);
            rawInfo.largeRawIconWidth = BitConverter.ToInt16(rawInfo.largeRawIcon, 0);
            rawInfo.largeRawIconHeight = BitConverter.ToInt16(rawInfo.largeRawIcon, 2);
            rawInfo.largeRawIcon = rawInfo.largeRawIcon.Skip(4).ToArray();
            rawInfo.smallRawIcon = _iconResource.Read(GameConstants.MAX_RAW * 3 + i + 1);
            rawInfo.smallRawIconWidth = BitConverter.ToInt16(rawInfo.smallRawIcon, 0);
            rawInfo.smallRawIconHeight = BitConverter.ToInt16(rawInfo.smallRawIcon, 2);
            rawInfo.smallRawIcon = rawInfo.smallRawIcon.Skip(4).ToArray();
        }
    }
}