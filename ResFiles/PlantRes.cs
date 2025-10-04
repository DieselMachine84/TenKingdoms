using System;
using System.Linq;

namespace TenKingdoms;

public class PlantBitmapRec
{
    private const int PLANT_LEN = 8;
    private const int SIZE_LEN = 2;
    private const int OFFSET_LEN = 3;
    private const int FILE_NAME_LEN = 8;
    private const int BITMAP_PTR_LEN = 4;

    public char[] plant = new char[PLANT_LEN];
    public char[] size = new char[SIZE_LEN];

    public char[] offset_x = new char[OFFSET_LEN];
    public char[] offset_y = new char[OFFSET_LEN];

    public char town_age;

    public char[] file_name = new char[FILE_NAME_LEN];
    public byte[] bitmap_ptr = new byte[BITMAP_PTR_LEN];

    public PlantBitmapRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < plant.Length; i++, dataIndex++)
            plant[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        for (int i = 0; i < size.Length; i++, dataIndex++)
            size[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        for (int i = 0; i < offset_x.Length; i++, dataIndex++)
            offset_x[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        for (int i = 0; i < offset_y.Length; i++, dataIndex++)
            offset_y[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        town_age = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        dataIndex++;

        for (int i = 0; i < file_name.Length; i++, dataIndex++)
            file_name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        for (int i = 0; i < bitmap_ptr.Length; i++, dataIndex++)
            bitmap_ptr[i] = db.ReadByte(recNo, dataIndex);
    }
}

public class PlantBitmap
{
    public int Size { get; set; }
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }
    public int TownAge { get; set; }

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

public class PlantRec
{
    private const int CODE_LEN = 8;
    private const int ZONE_LEN = 1;
    private const int TERA_TYPE_LEN = 2;
    private const int FIRST_BITMAP_LEN = 3;
    private const int BITMAP_COUNT_LEN = 3;

    public char[] code = new char[CODE_LEN];
    public char[] climate_zone = new char[ZONE_LEN];

    public char[] tera_type1 = new char[TERA_TYPE_LEN];
    public char[] tera_type2 = new char[TERA_TYPE_LEN];
    public char[] tera_type3 = new char[TERA_TYPE_LEN];

    public char[] first_bitmap = new char[FIRST_BITMAP_LEN];
    public char[] bitmap_count = new char[BITMAP_COUNT_LEN];

    public PlantRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < code.Length; i++, dataIndex++)
            code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < climate_zone.Length; i++, dataIndex++)
            climate_zone[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < tera_type1.Length; i++, dataIndex++)
            tera_type1[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < tera_type2.Length; i++, dataIndex++)
            tera_type2[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < tera_type3.Length; i++, dataIndex++)
            tera_type3[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < first_bitmap.Length; i++, dataIndex++)
            first_bitmap[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < bitmap_count.Length; i++, dataIndex++)
            bitmap_count[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
    }
}

public class PlantInfo
{
    public int ClimateZone { get; set; }
    public byte[] TeraType { get; } = new byte[3];

    public int FirstBitmap { get; set; }
    public int BitmapCount { get; set; }
}

public class PlantRes
{
    private PlantInfo[] PlantInfos { get; set; }
    private PlantBitmap[] PlantBitmaps { get; set; }
    private int[] _scanIdArray; // a buffer for scanning

    private byte PlantMapColor { get; }

    public TerrainRes TerrainRes { get; }

    public PlantRes(TerrainRes terrainRes)
    {
        TerrainRes = terrainRes;
        
        LoadPlantInfo();
        LoadPlantBitmap();

        PlantMapColor = Colors.V_DARK_GREEN;
    }

    public PlantInfo this[int plantId] => PlantInfos[plantId - 1];
    
    public int Scan(int climateZone, int teraType, int townAge)
    {
        int matchCount = 0;

        foreach (var plantInfo in PlantInfos)
        {
            if (climateZone == 0 || (plantInfo.ClimateZone & climateZone) != 0)
            {
                if (teraType == 0 || plantInfo.TeraType[0] == teraType || plantInfo.TeraType[1] == teraType || plantInfo.TeraType[2] == teraType)
                {
                    for (int j = 0; j < plantInfo.BitmapCount; j++)
                    {
                        PlantBitmap plantBitmap = PlantBitmaps[plantInfo.FirstBitmap - 1 + j];
                        // * = wildcard type, could apply to any town age level
                        if (townAge == 0 || plantBitmap.TownAge == townAge || plantBitmap.TownAge == '*')
                        {
                            _scanIdArray[matchCount++] = plantInfo.FirstBitmap + j;
                        }
                    }
                }
            }
        }

        //--- pick one from those plants that match the criteria ---//
        return matchCount > 0 ? _scanIdArray[Misc.Random(matchCount)] : 0;
    }

    public int PlantId(int bitmapId)
    {
        for (int i = 0; i < PlantInfos.Length; i++)
        {
            PlantInfo plantInfo = PlantInfos[i];
            if (plantInfo.FirstBitmap <= bitmapId && bitmapId < plantInfo.FirstBitmap + plantInfo.BitmapCount)
                return i + 1;
        }

        return 0;
    }

    public PlantBitmap GetBitmap(int bitmapId)
    {
        return PlantBitmaps[bitmapId - 1];
    }

    private void LoadPlantInfo()
    {
        Database dbPlant = new Database($"{Sys.GameDataFolder}/Resource/PLANT{Sys.Instance.Config.terrain_set}.RES");
        PlantInfos = new PlantInfo[dbPlant.RecordCount];

        for (int i = 0; i < PlantInfos.Length; i++)
        {
            PlantRec plantRec = new PlantRec(dbPlant, i + 1);
            PlantInfo plantInfo = new PlantInfo();
            PlantInfos[i] = plantInfo;

            plantInfo.ClimateZone = Misc.ToInt32(plantRec.climate_zone);

            if (plantRec.tera_type1[0] == 'T') // town plant
            {
                plantInfo.TeraType[0] = Convert.ToByte('T');
            }
            else
            {
                plantInfo.TeraType[0] = plantRec.tera_type1[0] != ' ' ? TerrainRes.GetTeraTypeId(plantRec.tera_type1) : (byte)0;
            }

            if (plantRec.tera_type2[0] == 'T') // town plant
            {
                plantInfo.TeraType[1] = Convert.ToByte('T');
            }
            else
            {
                plantInfo.TeraType[1] = plantRec.tera_type2[0] != ' ' ? TerrainRes.GetTeraTypeId(plantRec.tera_type2) : (byte)0;
            }

            if (plantRec.tera_type3[0] == 'T') // town plant
            {
                plantInfo.TeraType[2] = Convert.ToByte('T');
            }
            else
            {
                plantInfo.TeraType[2] = plantRec.tera_type3[0] != ' ' ? TerrainRes.GetTeraTypeId(plantRec.tera_type3) : (byte)0;
            }

            plantInfo.FirstBitmap = Misc.ToInt32(plantRec.first_bitmap);
            plantInfo.BitmapCount = 1 + Misc.ToInt32(plantRec.bitmap_count);
        }
    }

    private void LoadPlantBitmap()
    {
        ResourceDb plantBitmaps = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_PLANT{Sys.Instance.Config.terrain_set}.RES");
        Database dbPlantBitmap = new Database($"{Sys.GameDataFolder}/Resource/PLANTBM{Sys.Instance.Config.terrain_set}.RES");
        PlantBitmaps = new PlantBitmap[dbPlantBitmap.RecordCount];
        _scanIdArray = new int[PlantBitmaps.Length];

        for (int i = 0; i < PlantBitmaps.Length; i++)
        {
            PlantBitmapRec plantBitmapRec = new PlantBitmapRec(dbPlantBitmap, i + 1);
            PlantBitmap plantBitmap = new PlantBitmap();
            PlantBitmaps[i] = plantBitmap;

            plantBitmap.Size = Misc.ToInt32(plantBitmapRec.size);

            int bitmapOffset = BitConverter.ToInt32(plantBitmapRec.bitmap_ptr, 0);
            plantBitmap.Bitmap = plantBitmaps.Read(bitmapOffset);
            plantBitmap.BitmapWidth = BitConverter.ToInt16(plantBitmap.Bitmap, 0);
            plantBitmap.BitmapHeight = BitConverter.ToInt16(plantBitmap.Bitmap, 2);
            plantBitmap.Bitmap = plantBitmap.Bitmap.Skip(4).ToArray();

            plantBitmap.OffsetX = Misc.ToInt32(plantBitmapRec.offset_x);
            plantBitmap.OffsetY = Misc.ToInt32(plantBitmapRec.offset_y);

            if (plantBitmapRec.town_age >= '1' && plantBitmapRec.town_age <= '9')
                plantBitmap.TownAge = plantBitmapRec.town_age - '0';
        }
    }
}