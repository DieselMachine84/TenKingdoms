using System;
using System.Collections.Generic;
using System.Linq;

namespace TenKingdoms;

public class TownLayoutRec
{
    public const int CODE_LEN = 8;
    public const int GROUND_NAME_LEN = 8;
    public const int FIRST_SLOT_LEN = 5;
    public const int SLOT_COUNT_LEN = 2;

    public char[] code = new char[CODE_LEN];
    public char[] ground_name = new char[GROUND_NAME_LEN];		// name of the ground bitmap in image_spict
    public char[] first_slot = new char[FIRST_SLOT_LEN];
    public char[] slot_count = new char[SLOT_COUNT_LEN];

    public TownLayoutRec(byte[] data)
    {
        int dataIndex = 0;
        for (int i = 0; i < code.Length; i++, dataIndex++)
            code[i] = Convert.ToChar(data[dataIndex]);
		
        for (int i = 0; i < ground_name.Length; i++, dataIndex++)
            ground_name[i] = Convert.ToChar(data[dataIndex]);
		
        for (int i = 0; i < first_slot.Length; i++, dataIndex++)
            first_slot[i] = Convert.ToChar(data[dataIndex]);
		
        for (int i = 0; i < slot_count.Length; i++, dataIndex++)
            slot_count[i] = Convert.ToChar(data[dataIndex]);
    }
}

public class TownLayout
{
    public const int MAX_TOWN_LAYOUT_SLOT = 25;
    public int build_count; // no. of building in this layout

    public int first_slot_recno;
    public int slot_count;

    public byte[] groundBitmap;
    public int groundBitmapWidth;
    public int groundBitmapHeight;
    private IntPtr texture;
    
    public IntPtr GetTexture(Graphics graphics)
    {
        if (texture == default)
        {
            byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(groundBitmap, groundBitmapWidth, groundBitmapHeight);
            texture = graphics.CreateTextureFromBmp(decompressedBitmap, groundBitmapWidth, groundBitmapHeight);
        }

        return texture;
    }
}

public class TownSlotRec
{
    public const int CODE_LEN = 8;
    public const int POS_LEN = 3;
    public const int TYPE_LEN = 8;
    public const int BUILD_CODE_LEN = 2;
    public const int TYPE_ID_LEN = 3;

    public char[] layout_code = new char[CODE_LEN];

    public char[] base_x = new char[POS_LEN];
    public char[] base_y = new char[POS_LEN];

    public char[] type = new char[TYPE_LEN];
    public char[] build_code = new char[BUILD_CODE_LEN];

    public char[] type_id = new char[TYPE_ID_LEN];

    public TownSlotRec(byte[] data)
    {
        int dataIndex = 0;
        for (int i = 0; i < layout_code.Length; i++, dataIndex++)
            layout_code[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < base_x.Length; i++, dataIndex++)
            base_x[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < base_y.Length; i++, dataIndex++)
            base_y[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < type.Length; i++, dataIndex++)
            type[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < build_code.Length; i++, dataIndex++)
            build_code[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < type_id.Length; i++, dataIndex++)
            type_id[i] = Convert.ToChar(data[dataIndex]);
    }
}

public class TownSlot
{
    public const int TOWN_OBJECT_HOUSE = 1;
    public const int TOWN_OBJECT_PLANT = 2;
    public const int TOWN_OBJECT_FARM = 3;
    public const int TOWN_OBJECT_FLAG = 4;
    
    public int base_x;
    public int base_y;
    public int build_type; // id. of the building type
    public int build_code; // building direction
}

public class TownBuildTypeRec
{
    public const int TYPE_CODE_LEN = 8;
    public const int FIRST_BUILD_LEN = 5;
    public const int BUILD_COUNT_LEN = 5;

    public char[] type_code = new char[TYPE_CODE_LEN];
    public char[] first_build = new char[FIRST_BUILD_LEN];
    public char[] build_count = new char[BUILD_COUNT_LEN];

    public TownBuildTypeRec(byte[] data)
    {
        int dataIndex = 0;
        for (int i = 0; i < type_code.Length; i++, dataIndex++)
            type_code[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < first_build.Length; i++, dataIndex++)
            first_build[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < build_count.Length; i++, dataIndex++)
            build_count[i] = Convert.ToChar(data[dataIndex]);
    }
}

public class TownBuildType
{
    public int first_build_recno;
    public int build_count;
}

public class TownBuildRec
{
    public const int TYPE_LEN = 8;
    public const int BUILD_CODE_LEN = 2;
    public const int RACE_LEN = 8;
    public const int TYPE_ID_LEN = 3;
    public const int RACE_ID_LEN = 3;
    public const int FILE_NAME_LEN = 8;
    public const int BITMAP_PTR_LEN = 4;

    public char[] type = new char[TYPE_LEN];
    public char[] build_code = new char[BUILD_CODE_LEN];
    public char[] race = new char[RACE_LEN];

    public char[] type_id = new char[TYPE_ID_LEN];
    public char[] race_id = new char[RACE_ID_LEN];

    public char[] file_name = new char[FILE_NAME_LEN];
    public byte[] bitmap_ptr = new byte[BITMAP_PTR_LEN];

    public TownBuildRec(byte[] data)
    {
        int dataIndex = 0;
        for (int i = 0; i < type.Length; i++, dataIndex++)
            type[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < build_code.Length; i++, dataIndex++)
            build_code[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < race.Length; i++, dataIndex++)
            race[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < type_id.Length; i++, dataIndex++)
            type_id[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < race_id.Length; i++, dataIndex++)
            race_id[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < file_name.Length; i++, dataIndex++)
            file_name[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < bitmap_ptr.Length; i++, dataIndex++)
            bitmap_ptr[i] = data[dataIndex];
    }
}

public class TownBuild
{
    public int build_type; // building type. e.g. house, wind mill, church

    public int race_id;
    public int build_code;

    public byte[] bitmap;
    public int bitmapWidth;
    public int bitmapHeight;
    private Dictionary<int, IntPtr> textures = new Dictionary<int, nint>();

    public IntPtr GetTexture(Graphics graphics, int nationColor, bool isSelected)
    {
        int colorScheme = ColorRemap.ColorSchemes[nationColor];
        int textureKey = ColorRemap.GetTextureKey(colorScheme, isSelected);
        if (!textures.ContainsKey(textureKey))
        {
            byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(bitmap,
                bitmapWidth, bitmapHeight, ColorRemap.GetColorRemap(colorScheme, isSelected).ColorTable);
            IntPtr texture = graphics.CreateTextureFromBmp(decompressedBitmap, bitmapWidth, bitmapHeight);
            textures.Add(textureKey, texture);
        }
        
        return textures[textureKey];
    }
}

public class TownNameRec
{
    public const int NAME_LEN = 15;

    public char[] name = new char[NAME_LEN];

    public TownNameRec(byte[] data)
    {
        int dataIndex = 0;
        for (int i = 0; i < name.Length; i++, dataIndex++)
            name[i] = Convert.ToChar(data[dataIndex]);
    }
}

public class TownName
{
    public string name;
}

public class TownRes
{
    public const string TOWN_LAYOUT_DB = "TOWNLAY";
    public const string TOWN_SLOT_DB = "TOWNSLOT";
    public const string TOWN_BUILD_TYPE_DB = "TOWNBTYP";
    public const string TOWN_BUILD_DB = "TOWNBULD";
    public const string TOWN_NAME_DB = "TOWNNAME";

    public const int POPULATION_PER_HOUSE = 6;

    public TownLayout[] town_layout_array;
    public TownSlot[] town_slot_array;
    public TownBuildType[] town_build_type_array;
    public TownBuild[] town_build_array;
    public TownName[] town_name_array;
    public byte[] town_name_used_array; // store the used_count separately from town_name_array to facilitate file saving

    public ResourceDb res_bitmap;

    public List<byte[]> farmBitmaps = new List<byte[]>();
    public List<int> farmWidths = new List<int>();
    public List<int> farmHeights = new List<int>();
    private Dictionary<int, IntPtr> farmTextures = new Dictionary<int, nint>();

    public List<byte[]> flagBitmaps = new List<byte[]>();
    public List<int> flagWidths = new List<int>();
    public List<int> flagHeights = new List<int>();
    private List<Dictionary<int, IntPtr>> flagTextures = new List<Dictionary<int, nint>>();

    public IntPtr GetFarmTexture(Graphics graphics, int farmIndex)
    {
        if (!farmTextures.ContainsKey(farmIndex))
        {
            byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(farmBitmaps[farmIndex],
                farmWidths[farmIndex], farmHeights[farmIndex]);
            IntPtr texture = graphics.CreateTextureFromBmp(decompressedBitmap, farmWidths[farmIndex], farmHeights[farmIndex]);
            farmTextures.Add(farmIndex, texture);
        }
        
        return farmTextures[farmIndex];
    }
    
    public IntPtr GetFlagTexture(Graphics graphics, int flagIndex, int nationColor, bool isSelected)
    {
        int colorScheme = ColorRemap.ColorSchemes[nationColor];
        int textureKey = ColorRemap.GetTextureKey(colorScheme, isSelected);
        if (!flagTextures[flagIndex].ContainsKey(textureKey))
        {
            byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(flagBitmaps[flagIndex],
                flagWidths[flagIndex], flagHeights[flagIndex], ColorRemap.GetColorRemap(colorScheme, isSelected).ColorTable);
            IntPtr texture = graphics.CreateTextureFromBmp(decompressedBitmap, flagWidths[flagIndex], flagHeights[flagIndex]);
            flagTextures[flagIndex].Add(textureKey, texture);
        }
        
        return flagTextures[flagIndex][textureKey];
    }

    public GameSet GameSet { get; }
    public RaceRes RaceRes { get; }

    public TownRes(GameSet gameSet, RaceRes raceRes)
    {
        GameSet = gameSet;
        RaceRes = raceRes;

        res_bitmap = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_TOWN.RES");

        //------- load database information --------//

        // LoadTownSlot() must be called first before LoadTownLayout(), as LoadTownLayout() accesses town_slot_array
        LoadTownSlot();
        LoadTownLayout();
        LoadTownBuildType();
        LoadTownBuild();
        LoadTownName();
        LoadFarms();
        LoadFlags();
    }

    public int scan_build(int slotId, int raceId)
    {
        const int MAX_SCAN_ID = 100;

        TownSlot townSlot = get_slot(slotId);
        int matchCount = 0;
        int[] scanIdArray = new int[MAX_SCAN_ID];

        //---- get the building type of the slot ------//

        TownBuildType buildType = get_build_type(townSlot.build_type);

        //------ scan_build buildings of the specified type ------//

        int buildRecno = buildType.first_build_recno;

        for (int i = buildType.build_count; i > 0; i--, buildRecno++)
        {
            TownBuild townBuild = get_build(buildRecno);
            if (townBuild.build_code == townSlot.build_code)
            {
                if (raceId == 0 || townBuild.race_id == raceId)
                {
                    scanIdArray[matchCount] = buildRecno;

                    if (++matchCount >= MAX_SCAN_ID)
                        break;
                }
            }
        }

        //--- pick one from those plants that match the criteria ---//
        return matchCount > 0 ? scanIdArray[Misc.Random(matchCount)] : 0;
    }

    public string get_name(int recNo)
    {
        return town_name_array[recNo - 1].name;
    }

    public int get_new_name_id(int raceId)
    {
        RaceInfo raceInfo = RaceRes[raceId];

        int townNameId = 0;

        //----- if all town names have been used already -----//
        //--- scan the town name one by one and pick an unused one ---//

        if (raceInfo.town_name_used_count == raceInfo.town_name_count)
        {
            int nameId = Misc.Random(raceInfo.town_name_count) + 1; // this is the id. of one race only

            for (int i = raceInfo.town_name_count; i > 0; i--)
            {
                if (++nameId > raceInfo.town_name_count)
                    nameId = 1;
                
                // -2 is the total of two -1, (one with first_town_name_recno, another with town_name_used_array[])
                if (town_name_used_array[raceInfo.first_town_name_recno + nameId - 2] == 0)
                    break;
            }

            townNameId = raceInfo.first_town_name_recno + nameId - 1;
        }
        else
        {
            raceInfo.town_name_used_count++;

            townNameId = raceInfo.first_town_name_recno + raceInfo.town_name_used_count - 1;
        }

        town_name_used_array[townNameId - 1]++;

        return townNameId;
    }

    public void free_name_id(int townNameId)
    {
        town_name_used_array[townNameId - 1]--;
    }

    public TownLayout get_layout(int recNo)
    {
        return town_layout_array[recNo - 1];
    }

    public TownSlot get_slot(int recNo)
    {
        return town_slot_array[recNo - 1];
    }

    public TownBuildType get_build_type(int recNo)
    {
        return town_build_type_array[recNo - 1];
    }

    public TownBuild get_build(int recNo)
    {
        return town_build_array[recNo - 1];
    }

    private void LoadTownLayout()
    {
        Database dbTownLayout = GameSet.OpenDb(TOWN_LAYOUT_DB);
        town_layout_array = new TownLayout[dbTownLayout.RecordCount];

        //------ read in town layout info array -------//

        string townDbName = $"{Sys.GameDataFolder}/Resource/I_TPICT{Sys.Instance.Config.terrain_set}.RES";
        ResourceIdx image_tpict = new ResourceIdx(townDbName);

        for (int i = 0; i < town_layout_array.Length; i++)
        {
            TownLayoutRec townLayoutRec = new TownLayoutRec(dbTownLayout.Read(i + 1));
            TownLayout townLayout = new TownLayout();
            town_layout_array[i] = townLayout;

            townLayout.first_slot_recno = Misc.ToInt32(townLayoutRec.first_slot);
            townLayout.slot_count = Misc.ToInt32(townLayoutRec.slot_count);
            townLayout.groundBitmap = image_tpict.Read(Misc.ToString(townLayoutRec.ground_name));
            townLayout.groundBitmapWidth = BitConverter.ToInt16(townLayout.groundBitmap, 0);
            townLayout.groundBitmapHeight = BitConverter.ToInt16(townLayout.groundBitmap, 2);
            townLayout.groundBitmap = townLayout.groundBitmap.Skip(4).ToArray();

            //----- calculate min_population & max_population -----//

            int index = townLayout.first_slot_recno - 1;

            for (int j = 0; j < townLayout.slot_count; j++, index++)
            {
                TownSlot townSlot = town_slot_array[index];
                if (townSlot.build_type == TownSlot.TOWN_OBJECT_HOUSE) // if there is a building in this slot
                    townLayout.build_count++;
            }
        }
    }

    private void LoadTownSlot()
    {
        Database dbTownSlot = GameSet.OpenDb(TOWN_SLOT_DB);

        town_slot_array = new TownSlot[dbTownSlot.RecordCount];
        for (int i = 0; i < town_slot_array.Length; i++)
            town_slot_array[i] = new TownSlot();

        //------ read in town slot info array -------//

        for (int i = 0; i < town_slot_array.Length; i++)
        {
            TownSlotRec townSlotRec = new TownSlotRec(dbTownSlot.Read(i + 1));
            TownSlot townSlot = new TownSlot();
            town_slot_array[i] = townSlot;

            townSlot.base_x = Misc.ToInt32(townSlotRec.base_x);
            townSlot.base_y = Misc.ToInt32(townSlotRec.base_y);

            townSlot.build_type = Misc.ToInt32(townSlotRec.type_id);
            townSlot.build_code = Misc.ToInt32(townSlotRec.build_code);
        }
    }

    private void LoadTownBuildType()
    {
        Database dbTownBuildType = GameSet.OpenDb(TOWN_BUILD_TYPE_DB);

        town_build_type_array = new TownBuildType[dbTownBuildType.RecordCount];

        //------ read in TownBuildType info array -------//

        for (int i = 0; i < town_build_type_array.Length; i++)
        {
            TownBuildTypeRec buildTypeRec = new TownBuildTypeRec(dbTownBuildType.Read((i + 1)));
            TownBuildType buildType = new TownBuildType();
            town_build_type_array[i] = buildType;

            buildType.first_build_recno = Misc.ToInt32(buildTypeRec.first_build);
            buildType.build_count = Misc.ToInt32(buildTypeRec.build_count);
        }
    }

    private void LoadTownBuild()
    {
        Database dbTownBuild = GameSet.OpenDb(TOWN_BUILD_DB);

        town_build_array = new TownBuild[dbTownBuild.RecordCount];

        //------ read in town build info array -------//

        for (int i = 0; i < town_build_array.Length; i++)
        {
            TownBuildRec townBuildRec = new TownBuildRec(dbTownBuild.Read(i + 1));
            TownBuild townBuild = new TownBuild();
            town_build_array[i] = townBuild;

            townBuild.build_type = Misc.ToInt32(townBuildRec.type_id);
            townBuild.build_code = Misc.ToInt32(townBuildRec.build_code);
            townBuild.race_id = Misc.ToInt32(townBuildRec.race_id);

            int bitmapOffset = BitConverter.ToInt32(townBuildRec.bitmap_ptr, 0);
            townBuild.bitmap = res_bitmap.Read(bitmapOffset);
            townBuild.bitmapWidth = BitConverter.ToInt16(townBuild.bitmap, 0);
            townBuild.bitmapHeight = BitConverter.ToInt16(townBuild.bitmap, 2);
            townBuild.bitmap = townBuild.bitmap.Skip(4).ToArray();
        }
    }

    private void LoadTownName()
    {
        Database dbTownName = GameSet.OpenDb(TOWN_NAME_DB);

        town_name_array = new TownName[dbTownName.RecordCount];
        town_name_used_array = new byte[town_name_array.Length];

        //------ read in TownName info array -------//

        int raceId = 0;

        int i = 1;
        for (i = 1; i <= town_name_array.Length; i++)
        {
            TownNameRec townNameRec = new TownNameRec(dbTownName.Read(i));
            TownName townName = new TownName();
            town_name_array[i - 1] = townName;

            //misc.rtrim_fld( townName->name, townNameRec->name, townNameRec->NAME_LEN );
            townName.name = Misc.ToString(townNameRec.name);

            if (townName.name[0] == '@') // next race
            {
                int j = 1;
                for (j = 1; j <= GameConstants.MAX_RACE; j++)
                {
                    if (RaceRes[j].code == townName.name.Substring(1))
                        break;
                }

                if (raceId != 0)
                    RaceRes[raceId].town_name_count = i - RaceRes[raceId].first_town_name_recno;

                raceId = j;
                RaceRes[raceId].first_town_name_recno = i + 1;
            }
        }

        //-- set the town_name_count of the last  town in TOWNNAME.DBF --//

        RaceRes[raceId].town_name_count = i - RaceRes[raceId].first_town_name_recno;
    }

    private void LoadFarms()
    {
        ResourceIdx images = new ResourceIdx($"{Sys.GameDataFolder}/Resource/I_SPICT.RES");
        for (int i = 0; i < 2; i++)
        {
            byte[] farmData = images.Read("FARM-" + (i + 1).ToString());
            farmWidths.Add(BitConverter.ToInt16(farmData, 0));
            farmHeights.Add(BitConverter.ToInt16(farmData, 2));
            farmBitmaps.Add(farmData.Skip(4).ToArray());
        }
    }

    private void LoadFlags()
    {
        ResourceIdx images = new ResourceIdx($"{Sys.GameDataFolder}/Resource/I_SPICT.RES");
        for (int i = 0; i < 4; i++)
        {
            byte[] flagData = images.Read("FLAG-" + (i + 1).ToString());
            flagWidths.Add(BitConverter.ToInt16(flagData, 0));
            flagHeights.Add(BitConverter.ToInt16(flagData, 2));
            flagBitmaps.Add(flagData.Skip(4).ToArray());
            flagTextures.Add(new Dictionary<int, nint>());
        }
    }
}
