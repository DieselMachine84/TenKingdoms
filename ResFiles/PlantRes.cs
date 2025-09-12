using System;
using System.Linq;

namespace TenKingdoms;

public class PlantBitmapRec
{
    public const int PLANT_LEN = 8;
    public const int SIZE_LEN = 2;
    public const int OFFSET_LEN = 3;
    public const int FILE_NAME_LEN = 8;
    public const int BITMAP_PTR_LEN = 4;

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
    public int size;
    public int offset_x;
    public int offset_y;
    public int town_age;

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

public class PlantRec
{
    public const int CODE_LEN = 8;
    public const int ZONE_LEN = 1;
    public const int TERA_TYPE_LEN = 2;
    public const int FIRST_BITMAP_LEN = 3;
    public const int BITMAP_COUNT_LEN = 3;

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
    public int climate_zone;
    public byte[] tera_type = new byte[3];

    public int first_bitmap;
    public int bitmap_count;
}

public class PlantRes
{
    public PlantInfo[] plant_info_array;
    public PlantBitmap[] plant_bitmap_array;
    public int[] scan_id_array; // a buffer for scaning

    public byte plant_map_color;

    public ResourceDb res_bitmap;
    
    public TerrainRes TerrainRes { get; private set; }

    public PlantRes(TerrainRes terrainRes)
    {
        TerrainRes = terrainRes;
        
        res_bitmap = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_PLANT{Sys.Instance.Config.terrain_set}.RES");

        LoadPlantInfo();
        LoadPlantBitmap();

        plant_map_color = Colors.V_DARK_GREEN;
    }

    public int scan(int climateZone, int teraType, int townAge)
    {
        int matchCount = 0;

        //-------- scan plant id. ----------//

        foreach (var plantInfo in plant_info_array)
        {
            if (climateZone == 0 || (plantInfo.climate_zone & climateZone) != 0)
            {
                if (teraType == 0 ||
                    plantInfo.tera_type[0] == teraType ||
                    plantInfo.tera_type[1] == teraType ||
                    plantInfo.tera_type[2] == teraType)
                {
                    //------ scan plant bitmap ----------//
                    for (int j = 0; j < plantInfo.bitmap_count; j++)
                    {
                        PlantBitmap plantBitmap = plant_bitmap_array[plantInfo.first_bitmap - 1 + j];
                        // * = wildcard type, could apply to any town age level
                        if (townAge == 0 || plantBitmap.town_age == townAge ||
                            plantBitmap.town_age == Convert.ToInt32('*'))
                        {
                            scan_id_array[matchCount++] = plantInfo.first_bitmap + j;
                        }
                    }
                }
            }
        }

        //--- pick one from those plants that match the criteria ---//
        return matchCount > 0 ? scan_id_array[Misc.Random(matchCount)] : 0;
    }

    public int plant_recno(int bitmapId)
    {
        for (int i = 0; i < plant_info_array.Length; ++i)
        {
            PlantInfo plantInfo = plant_info_array[i];
            if (plantInfo.first_bitmap <= bitmapId && bitmapId < plantInfo.first_bitmap + plantInfo.bitmap_count)
                return i + 1;
        }

        return 0;
    }

    public PlantBitmap get_bitmap(int bitmapId)
    {
        return plant_bitmap_array[bitmapId - 1];
    }

    public PlantInfo this[int plantId] => plant_info_array[plantId - 1];

    private void LoadPlantInfo()
    {
        Database dbPlant = new Database($"{Sys.GameDataFolder}/Resource/PLANT{Sys.Instance.Config.terrain_set}.RES");
        plant_info_array = new PlantInfo[dbPlant.RecordCount];

        for (int i = 0; i < plant_info_array.Length; i++)
        {
            PlantRec plantRec = new PlantRec(dbPlant, i + 1);
            PlantInfo plantInfo = new PlantInfo();
            plant_info_array[i] = plantInfo;

            plantInfo.climate_zone = Convert.ToInt32(new string(plantRec.climate_zone));

            if (plantRec.tera_type1[0] == 'T') // town plant
            {
                plantInfo.tera_type[0] = Convert.ToByte('T');
            }
            else
            {
                if (plantRec.tera_type1[0] != ' ')
                    plantInfo.tera_type[0] = TerrainRes.get_tera_type_id(plantRec.tera_type1);
                else
                    plantInfo.tera_type[0] = 0;
            }

            if (plantRec.tera_type2[0] == 'T') // town plant
            {
                plantInfo.tera_type[1] = Convert.ToByte('T');
            }
            else
            {
                if (plantRec.tera_type2[0] != ' ')
                    plantInfo.tera_type[1] = TerrainRes.get_tera_type_id(plantRec.tera_type2);
                else
                    plantInfo.tera_type[1] = 0;
            }

            if (plantRec.tera_type3[0] == 'T') // town plant
            {
                plantInfo.tera_type[2] = Convert.ToByte('T');
            }
            else
            {
                if (plantRec.tera_type3[0] != ' ')
                    plantInfo.tera_type[2] = TerrainRes.get_tera_type_id(plantRec.tera_type3);
                else
                    plantInfo.tera_type[2] = 0;
            }

            plantInfo.first_bitmap = Misc.ToInt32(plantRec.first_bitmap);
            plantInfo.bitmap_count = 1 + Misc.ToInt32(plantRec.bitmap_count);
        }
    }

    private void LoadPlantBitmap()
    {
        Database dbPlantBitmap = new Database($"{Sys.GameDataFolder}/Resource/PLANTBM{Sys.Instance.Config.terrain_set}.RES");
        plant_bitmap_array = new PlantBitmap[dbPlantBitmap.RecordCount];

        scan_id_array = new int[plant_bitmap_array.Length];

        for (int i = 0; i < plant_bitmap_array.Length; i++)
        {
            PlantBitmapRec plantBitmapRec = new PlantBitmapRec(dbPlantBitmap, i + 1);
            PlantBitmap plantBitmap = new PlantBitmap();
            plant_bitmap_array[i] = plantBitmap;

            plantBitmap.size = Misc.ToInt32(plantBitmapRec.size);

            int bitmapOffset = BitConverter.ToInt32(plantBitmapRec.bitmap_ptr, 0);
            plantBitmap.bitmap = res_bitmap.Read(bitmapOffset);
            plantBitmap.bitmapWidth = BitConverter.ToInt16(plantBitmap.bitmap, 0);
            plantBitmap.bitmapHeight = BitConverter.ToInt16(plantBitmap.bitmap, 2);
            plantBitmap.bitmap = plantBitmap.bitmap.Skip(4).ToArray();

            plantBitmap.offset_x = Misc.ToInt32(plantBitmapRec.offset_x);
            plantBitmap.offset_y = Misc.ToInt32(plantBitmapRec.offset_y);

            if (plantBitmapRec.town_age >= '1' && plantBitmapRec.town_age <= '9')
                plantBitmap.town_age = plantBitmapRec.town_age - '0';
        }
    }
}