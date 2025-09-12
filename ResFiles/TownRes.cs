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

    public TownLayoutRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < code.Length; i++, dataIndex++)
            code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
		
        for (int i = 0; i < ground_name.Length; i++, dataIndex++)
            ground_name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
		
        for (int i = 0; i < first_slot.Length; i++, dataIndex++)
            first_slot[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
		
        for (int i = 0; i < slot_count.Length; i++, dataIndex++)
            slot_count[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
    }
}

public class TownLayout
{
    public const int MAX_TOWN_LAYOUT_SLOT = 25;

    public int BuildCount { get; set; } // no. of building in this layout

    public int FirstSlotId { get; set; }
    public int SlotCount { get; set; }

    public byte[] groundBitmap;
    public int groundBitmapWidth;
    public int groundBitmapHeight;
    private IntPtr _texture;
    
    public IntPtr GetTexture(Graphics graphics)
    {
        if (_texture == default)
        {
            byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(groundBitmap, groundBitmapWidth, groundBitmapHeight);
            _texture = graphics.CreateTextureFromBmp(decompressedBitmap, groundBitmapWidth, groundBitmapHeight);
        }

        return _texture;
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

    public TownSlotRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < layout_code.Length; i++, dataIndex++)
            layout_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < base_x.Length; i++, dataIndex++)
            base_x[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < base_y.Length; i++, dataIndex++)
            base_y[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < type.Length; i++, dataIndex++)
            type[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < build_code.Length; i++, dataIndex++)
            build_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < type_id.Length; i++, dataIndex++)
            type_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
    }
}

public class TownSlot
{
    public const int TOWN_OBJECT_HOUSE = 1;
    public const int TOWN_OBJECT_PLANT = 2;
    public const int TOWN_OBJECT_FARM = 3;
    public const int TOWN_OBJECT_FLAG = 4;
    
    public int BaseX { get; set; }
    public int BaseY { get; set; }
    public int BuildType { get; set; } // id. of the building type
    public int BuildCode { get; set; } // building direction
}

public class TownBuildTypeRec
{
    public const int TYPE_CODE_LEN = 8;
    public const int FIRST_BUILD_LEN = 5;
    public const int BUILD_COUNT_LEN = 5;

    public char[] type_code = new char[TYPE_CODE_LEN];
    public char[] first_build = new char[FIRST_BUILD_LEN];
    public char[] build_count = new char[BUILD_COUNT_LEN];

    public TownBuildTypeRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < type_code.Length; i++, dataIndex++)
            type_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < first_build.Length; i++, dataIndex++)
            first_build[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < build_count.Length; i++, dataIndex++)
            build_count[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
    }
}

public class TownBuildType
{
    public int FirstBuildId { get; set; }
    public int BuildCount { get; set; }
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

    public TownBuildRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < type.Length; i++, dataIndex++)
            type[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < build_code.Length; i++, dataIndex++)
            build_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < race.Length; i++, dataIndex++)
            race[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < type_id.Length; i++, dataIndex++)
            type_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < race_id.Length; i++, dataIndex++)
            race_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < file_name.Length; i++, dataIndex++)
            file_name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < bitmap_ptr.Length; i++, dataIndex++)
            bitmap_ptr[i] = db.ReadByte(recNo, dataIndex);
    }
}

public class TownBuild
{
    public int BuildType { get; set; } // building type. e.g. house, wind mill, church

    public int RaceId { get; set; }
    public int BuildCode { get; set; }

    public byte[] bitmap;
    public int bitmapWidth;
    public int bitmapHeight;
    private readonly Dictionary<int, IntPtr> _textures = new Dictionary<int, nint>();

    public IntPtr GetTexture(Graphics graphics, int nationColor, bool isSelected)
    {
        int colorScheme = ColorRemap.ColorSchemes[nationColor];
        int textureKey = ColorRemap.GetTextureKey(colorScheme, isSelected);
        if (!_textures.ContainsKey(textureKey))
        {
            byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(bitmap,
                bitmapWidth, bitmapHeight, ColorRemap.GetColorRemap(colorScheme, isSelected).ColorTable);
            IntPtr texture = graphics.CreateTextureFromBmp(decompressedBitmap, bitmapWidth, bitmapHeight);
            _textures.Add(textureKey, texture);
        }
        
        return _textures[textureKey];
    }
}

public class TownNameRec
{
    public const int NAME_LEN = 15;

    public char[] name = new char[NAME_LEN];

    public TownNameRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < name.Length; i++, dataIndex++)
            name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
    }
}

public class TownName
{
    public string Name { get; set; }
}

public class TownRes
{
    public const string TOWN_LAYOUT_DB = "TOWNLAY";
    public const string TOWN_SLOT_DB = "TOWNSLOT";
    public const string TOWN_BUILD_TYPE_DB = "TOWNBTYP";
    public const string TOWN_BUILD_DB = "TOWNBULD";
    public const string TOWN_NAME_DB = "TOWNNAME";

    public const int POPULATION_PER_HOUSE = 6;

    public TownLayout[] TownLayouts { get; private set; }
    private TownSlot[] _townSlots;
    private TownBuildType[] _townBuildTypes;
    private TownBuild[] _townBuilds;
    private TownName[] _townNames;
    private byte[] _townNamesUsed; // store the used_count separately from _townNames to facilitate file saving

    private readonly List<byte[]> _farmBitmaps = new List<byte[]>();
    public List<int> FarmWidths { get; } = new List<int>();
    public List<int> FarmHeights { get; } = new List<int>();
    private readonly Dictionary<int, IntPtr> _farmTextures = new Dictionary<int, nint>();

    private readonly List<byte[]> _flagBitmaps = new List<byte[]>();
    public List<int> FlagWidths { get; } = new List<int>();
    public List<int> FlagHeights { get; } = new List<int>();
    private readonly List<Dictionary<int, IntPtr>> _flagTextures = new List<Dictionary<int, nint>>();

    public IntPtr GetFarmTexture(Graphics graphics, int farmIndex)
    {
        if (!_farmTextures.ContainsKey(farmIndex))
        {
            byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(_farmBitmaps[farmIndex],
                FarmWidths[farmIndex], FarmHeights[farmIndex]);
            IntPtr texture = graphics.CreateTextureFromBmp(decompressedBitmap, FarmWidths[farmIndex], FarmHeights[farmIndex]);
            _farmTextures.Add(farmIndex, texture);
        }
        
        return _farmTextures[farmIndex];
    }
    
    public IntPtr GetFlagTexture(Graphics graphics, int flagIndex, int nationColor)
    {
        int colorScheme = ColorRemap.ColorSchemes[nationColor];
        int textureKey = ColorRemap.GetTextureKey(colorScheme, false);
        if (!_flagTextures[flagIndex].ContainsKey(textureKey))
        {
            byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(_flagBitmaps[flagIndex],
                FlagWidths[flagIndex], FlagHeights[flagIndex], ColorRemap.GetColorRemap(colorScheme, false).ColorTable);
            IntPtr texture = graphics.CreateTextureFromBmp(decompressedBitmap, FlagWidths[flagIndex], FlagHeights[flagIndex]);
            _flagTextures[flagIndex].Add(textureKey, texture);
        }
        
        return _flagTextures[flagIndex][textureKey];
    }

    private GameSet GameSet { get; }
    private RaceRes RaceRes { get; }

    public TownRes(GameSet gameSet, RaceRes raceRes)
    {
        GameSet = gameSet;
        RaceRes = raceRes;

        // LoadTownSlot() must be called first before LoadTownLayout(), as LoadTownLayout() accesses _townSlots
        LoadTownSlot();
        LoadTownLayout();
        LoadTownBuildType();
        LoadTownBuild();
        LoadTownName();
        LoadFarms();
        LoadFlags();
    }

    public int ScanBuild(int slotId, int raceId)
    {
        const int MAX_SCAN_ID = 100;

        TownSlot townSlot = GetSlot(slotId);
        int matchCount = 0;
        int[] scanIdArray = new int[MAX_SCAN_ID];

        //---- get the building type of the slot ------//

        TownBuildType buildType = GetBuildType(townSlot.BuildType);

        //------ scan_build buildings of the specified type ------//

        int buildId = buildType.FirstBuildId;

        for (int i = buildType.BuildCount; i > 0; i--, buildId++)
        {
            TownBuild townBuild = GetBuild(buildId);
            if (townBuild.BuildCode == townSlot.BuildCode)
            {
                if (raceId == 0 || townBuild.RaceId == raceId)
                {
                    scanIdArray[matchCount] = buildId;

                    if (++matchCount >= MAX_SCAN_ID)
                        break;
                }
            }
        }

        //--- pick one from those plants that match the criteria ---//
        return matchCount > 0 ? scanIdArray[Misc.Random(matchCount)] : 0;
    }

    public string GetName(int recNo)
    {
        return _townNames[recNo - 1].Name;
    }

    public int GetNewNameId(int raceId)
    {
        // TODO reuse town names, add numbers to town names
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
                if (_townNamesUsed[raceInfo.first_town_name_recno + nameId - 2] == 0)
                    break;
            }

            townNameId = raceInfo.first_town_name_recno + nameId - 1;
        }
        else
        {
            raceInfo.town_name_used_count++;

            townNameId = raceInfo.first_town_name_recno + raceInfo.town_name_used_count - 1;
        }

        _townNamesUsed[townNameId - 1]++;

        return townNameId;
    }

    public void FreeNameId(int townNameId)
    {
        _townNamesUsed[townNameId - 1]--;
    }

    public TownLayout GetLayout(int id)
    {
        return TownLayouts[id - 1];
    }

    public TownSlot GetSlot(int id)
    {
        return _townSlots[id - 1];
    }

    public TownBuildType GetBuildType(int id)
    {
        return _townBuildTypes[id - 1];
    }

    public TownBuild GetBuild(int id)
    {
        return _townBuilds[id - 1];
    }

    private void LoadTownLayout()
    {
        Database dbTownLayout = GameSet.OpenDb(TOWN_LAYOUT_DB);
        TownLayouts = new TownLayout[dbTownLayout.RecordCount];
        ResourceIdx groundImages = new ResourceIdx($"{Sys.GameDataFolder}/Resource/I_TPICT{Sys.Instance.Config.terrain_set}.RES");

        for (int i = 0; i < TownLayouts.Length; i++)
        {
            TownLayoutRec townLayoutRec = new TownLayoutRec(dbTownLayout, i + 1);
            TownLayout townLayout = new TownLayout();
            TownLayouts[i] = townLayout;

            townLayout.FirstSlotId = Misc.ToInt32(townLayoutRec.first_slot);
            townLayout.SlotCount = Misc.ToInt32(townLayoutRec.slot_count);
            townLayout.groundBitmap = groundImages.Read(Misc.ToString(townLayoutRec.ground_name));
            townLayout.groundBitmapWidth = BitConverter.ToInt16(townLayout.groundBitmap, 0);
            townLayout.groundBitmapHeight = BitConverter.ToInt16(townLayout.groundBitmap, 2);
            townLayout.groundBitmap = townLayout.groundBitmap.Skip(4).ToArray();

            //----- calculate min_population & max_population -----//

            int index = townLayout.FirstSlotId - 1;

            for (int j = 0; j < townLayout.SlotCount; j++, index++)
            {
                TownSlot townSlot = _townSlots[index];
                if (townSlot.BuildType == TownSlot.TOWN_OBJECT_HOUSE) // if there is a building in this slot
                    townLayout.BuildCount++;
            }
        }
    }

    private void LoadTownSlot()
    {
        Database dbTownSlot = GameSet.OpenDb(TOWN_SLOT_DB);
        _townSlots = new TownSlot[dbTownSlot.RecordCount];

        for (int i = 0; i < _townSlots.Length; i++)
        {
            TownSlotRec townSlotRec = new TownSlotRec(dbTownSlot, i + 1);
            TownSlot townSlot = new TownSlot();
            _townSlots[i] = townSlot;

            townSlot.BaseX = Misc.ToInt32(townSlotRec.base_x);
            townSlot.BaseY = Misc.ToInt32(townSlotRec.base_y);

            townSlot.BuildType = Misc.ToInt32(townSlotRec.type_id);
            townSlot.BuildCode = Misc.ToInt32(townSlotRec.build_code);
        }
    }

    private void LoadTownBuildType()
    {
        Database dbTownBuildType = GameSet.OpenDb(TOWN_BUILD_TYPE_DB);
        _townBuildTypes = new TownBuildType[dbTownBuildType.RecordCount];

        for (int i = 0; i < _townBuildTypes.Length; i++)
        {
            TownBuildTypeRec buildTypeRec = new TownBuildTypeRec(dbTownBuildType, i + 1);
            TownBuildType buildType = new TownBuildType();
            _townBuildTypes[i] = buildType;

            buildType.FirstBuildId = Misc.ToInt32(buildTypeRec.first_build);
            buildType.BuildCount = Misc.ToInt32(buildTypeRec.build_count);
        }
    }

    private void LoadTownBuild()
    {
        Database dbTownBuild = GameSet.OpenDb(TOWN_BUILD_DB);
        _townBuilds = new TownBuild[dbTownBuild.RecordCount];
        ResourceDb images = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_TOWN.RES");

        for (int i = 0; i < _townBuilds.Length; i++)
        {
            TownBuildRec townBuildRec = new TownBuildRec(dbTownBuild, i + 1);
            TownBuild townBuild = new TownBuild();
            _townBuilds[i] = townBuild;

            townBuild.BuildType = Misc.ToInt32(townBuildRec.type_id);
            townBuild.BuildCode = Misc.ToInt32(townBuildRec.build_code);
            townBuild.RaceId = Misc.ToInt32(townBuildRec.race_id);

            int bitmapOffset = BitConverter.ToInt32(townBuildRec.bitmap_ptr, 0);
            townBuild.bitmap = images.Read(bitmapOffset);
            townBuild.bitmapWidth = BitConverter.ToInt16(townBuild.bitmap, 0);
            townBuild.bitmapHeight = BitConverter.ToInt16(townBuild.bitmap, 2);
            townBuild.bitmap = townBuild.bitmap.Skip(4).ToArray();
        }
    }

    private void LoadTownName()
    {
        Database dbTownName = GameSet.OpenDb(TOWN_NAME_DB);
        _townNames = new TownName[dbTownName.RecordCount];
        _townNamesUsed = new byte[_townNames.Length];

        //------ read in TownName info array -------//

        int raceId = 0;

        int i = 1;
        for (i = 1; i <= _townNames.Length; i++)
        {
            TownNameRec townNameRec = new TownNameRec(dbTownName, i);
            TownName townName = new TownName();
            _townNames[i - 1] = townName;

            //misc.rtrim_fld( townName->name, townNameRec->name, townNameRec->NAME_LEN );
            townName.Name = Misc.ToString(townNameRec.name);

            if (townName.Name[0] == '@') // next race
            {
                int j = 1;
                for (j = 1; j <= GameConstants.MAX_RACE; j++)
                {
                    if (RaceRes[j].code == townName.Name.Substring(1))
                        break;
                }

                if (raceId != 0)
                    RaceRes[raceId].town_name_count = i - RaceRes[raceId].first_town_name_recno;

                raceId = j;
                RaceRes[raceId].first_town_name_recno = i + 1;
            }
        }

        //-- set the town_name_count of the last town in TOWNNAME.DBF --//

        RaceRes[raceId].town_name_count = i - RaceRes[raceId].first_town_name_recno;
    }

    private void LoadFarms()
    {
        ResourceIdx images = new ResourceIdx($"{Sys.GameDataFolder}/Resource/I_SPICT.RES");
        for (int i = 0; i < 2; i++)
        {
            byte[] farmData = images.Read("FARM-" + (i + 1).ToString());
            FarmWidths.Add(BitConverter.ToInt16(farmData, 0));
            FarmHeights.Add(BitConverter.ToInt16(farmData, 2));
            _farmBitmaps.Add(farmData.Skip(4).ToArray());
        }
    }

    private void LoadFlags()
    {
        ResourceIdx images = new ResourceIdx($"{Sys.GameDataFolder}/Resource/I_SPICT.RES");
        for (int i = 0; i < 4; i++)
        {
            byte[] flagData = images.Read("FLAG-" + (i + 1).ToString());
            FlagWidths.Add(BitConverter.ToInt16(flagData, 0));
            FlagHeights.Add(BitConverter.ToInt16(flagData, 2));
            _flagBitmaps.Add(flagData.Skip(4).ToArray());
            _flagTextures.Add(new Dictionary<int, nint>());
        }
    }
}
