using System;
using System.Collections.Generic;
using System.Linq;

namespace TenKingdoms;

public class FirmRec
{
    public const int CODE_LEN = 8;
    public const int NAME_LEN = 20;
    public const int SHORT_NAME_LEN = 12;
    public const int TITLE_LEN = 10;
    public const int FIRST_BUILD_LEN = 3;
    public const int BUILD_COUNT_LEN = 3;
    public const int HIT_POINTS_LEN = 5;
    public const int COST_LEN = 5;

    public char[] code = new char[CODE_LEN];
    public char[] name = new char[NAME_LEN];
    public char[] short_name = new char[SHORT_NAME_LEN];

    public char[] overseer_title = new char[TITLE_LEN];
    public char[] worker_title = new char[TITLE_LEN];

    public char tera_type;
    public byte all_know; // whether all nations know how to build this firm in the beginning of the game
    public byte live_in_town; // whether the workers of the firm lives in towns or not.

    public char[] hit_points = new char[HIT_POINTS_LEN];

    public byte is_linkable_to_town;

    public char[] setup_cost = new char[COST_LEN];
    public char[] year_cost = new char[COST_LEN];

    public char[] first_build = new char[FIRST_BUILD_LEN];
    public char[] build_count = new char[BUILD_COUNT_LEN];

    public FirmRec(byte[] data)
    {
        int dataIndex = 0;
        for (int i = 0; i < code.Length; i++, dataIndex++)
            code[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < name.Length; i++, dataIndex++)
            name[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < short_name.Length; i++, dataIndex++)
            short_name[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < overseer_title.Length; i++, dataIndex++)
            overseer_title[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < worker_title.Length; i++, dataIndex++)
            worker_title[i] = Convert.ToChar(data[dataIndex]);
        
        tera_type = Convert.ToChar(data[dataIndex]);
        dataIndex++;
        all_know = data[dataIndex];
        dataIndex++;
        live_in_town = data[dataIndex];
        dataIndex++;
        
        for (int i = 0; i < hit_points.Length; i++, dataIndex++)
            hit_points[i] = Convert.ToChar(data[dataIndex]);
        
        is_linkable_to_town = data[dataIndex];
        dataIndex++;
        
        for (int i = 0; i < setup_cost.Length; i++, dataIndex++)
            setup_cost[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < year_cost.Length; i++, dataIndex++)
            year_cost[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < first_build.Length; i++, dataIndex++)
            first_build[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < build_count.Length; i++, dataIndex++)
            build_count[i] = Convert.ToChar(data[dataIndex]);
    }
}

public class FirmBuildRec
{
    public const int FIRM_CODE_LEN = 8;
    public const int RACE_CODE_LEN = 8;
    public const int BITMAP_RECNO_LEN = 5;
    public const int FIRST_FRAME_LEN = 5;
    public const int FRAME_COUNT_LEN = 2;
    public const int RACE_ID_LEN = 3;

    public char[] firm_code = new char[FIRM_CODE_LEN];
    public char[] race_code = new char[RACE_CODE_LEN];

    public char animate_full_size;

    public char[] under_construction_bitmap_recno = new char[BITMAP_RECNO_LEN];
    public char[] under_construction_bitmap_count = new char[FRAME_COUNT_LEN];

    public char[] idle_bitmap_recno = new char[BITMAP_RECNO_LEN];
    public char[] ground_bitmap_recno = new char[BITMAP_RECNO_LEN];

    public char[] first_frame = new char[FIRST_FRAME_LEN];
    public char[] frame_count = new char[FRAME_COUNT_LEN];

    public char[] race_id = new char[RACE_ID_LEN];

    public FirmBuildRec(byte[] data)
    {
        int dataIndex = 0;
        for (int i = 0; i < firm_code.Length; i++, dataIndex++)
            firm_code[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < race_code.Length; i++, dataIndex++)
            race_code[i] = Convert.ToChar(data[dataIndex]);

        animate_full_size = Convert.ToChar(data[dataIndex]);
        dataIndex++;
        
        for (int i = 0; i < under_construction_bitmap_recno.Length; i++, dataIndex++)
            under_construction_bitmap_recno[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < under_construction_bitmap_count.Length; i++, dataIndex++)
            under_construction_bitmap_count[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < idle_bitmap_recno.Length; i++, dataIndex++)
            idle_bitmap_recno[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < ground_bitmap_recno.Length; i++, dataIndex++)
            ground_bitmap_recno[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < first_frame.Length; i++, dataIndex++)
            first_frame[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < frame_count.Length; i++, dataIndex++)
            frame_count[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < race_id.Length; i++, dataIndex++)
            race_id[i] = Convert.ToChar(data[dataIndex]);
    }
}

public class FirmFrameRec
{
    public const int FIRM_CODE_LEN = 8;
    public const int RACE_CODE_LEN = 8;
    public const int FRAME_ID_LEN = 2;
    public const int DELAY_LEN = 2;
    public const int FIRST_BITMAP_LEN = 5;
    public const int BITMAP_COUNT_LEN = 2;

    public char[] firm_code = new char[FIRM_CODE_LEN];
    public char[] race_code = new char[RACE_CODE_LEN];

    public char[] frame_id = new char[FRAME_ID_LEN];

    public char[] delay = new char[DELAY_LEN]; // unit: 1/10 second

    public char[] first_bitmap = new char[FIRST_BITMAP_LEN];
    public char[] bitmap_count = new char[BITMAP_COUNT_LEN];

    public FirmFrameRec(byte[] data)
    {
        int dataIndex = 0;
        for (int i = 0; i < firm_code.Length; i++, dataIndex++)
            firm_code[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < race_code.Length; i++, dataIndex++)
            race_code[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < frame_id.Length; i++, dataIndex++)
            frame_id[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < delay.Length; i++, dataIndex++)
            delay[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < first_bitmap.Length; i++, dataIndex++)
            first_bitmap[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < bitmap_count.Length; i++, dataIndex++)
            bitmap_count[i] = Convert.ToChar(data[dataIndex]);
    }
}

public class FirmBitmapRec
{
    public const int FIRM_CODE_LEN = 8;
    public const int RACE_CODE_LEN = 8;
    public const int FRAME_ID_LEN = 2;
    public const int LOC_LEN = 3;
    public const int OFFSET_LEN = 3;
    public const int DELAY_LEN = 2;
    public const int FILE_NAME_LEN = 8;
    public const int BITMAP_PTR_LEN = 4;

    public char[] firm_code = new char[FIRM_CODE_LEN];
    public char[] race_code = new char[RACE_CODE_LEN];
    public char mode;

    public char[] frame_id = new char[FRAME_ID_LEN];

    public char[] loc_width = new char[LOC_LEN];
    public char[] loc_height = new char[LOC_LEN];
    public char layer;

    public char[] offset_x = new char[OFFSET_LEN];
    public char[] offset_y = new char[OFFSET_LEN];

    public char[] delay = new char[DELAY_LEN];	  // unit: 1/10 second

    public char[] file_name = new char[FILE_NAME_LEN];
    public byte[] bitmap_ptr = new byte[BITMAP_PTR_LEN];

    public FirmBitmapRec(byte[] data)
    {
        int dataIndex = 0;
        for (int i = 0; i < firm_code.Length; i++, dataIndex++)
            firm_code[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < race_code.Length; i++, dataIndex++)
            race_code[i] = Convert.ToChar(data[dataIndex]);
        
        mode = Convert.ToChar(data[dataIndex]);
        dataIndex++;
        
        for (int i = 0; i < frame_id.Length; i++, dataIndex++)
            frame_id[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < loc_width.Length; i++, dataIndex++)
            loc_width[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < loc_height.Length; i++, dataIndex++)
            loc_height[i] = Convert.ToChar(data[dataIndex]);
        
        layer = Convert.ToChar(data[dataIndex]);
        dataIndex++;
        
        for (int i = 0; i < offset_x.Length; i++, dataIndex++)
            offset_x[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < offset_y.Length; i++, dataIndex++)
            offset_y[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < delay.Length; i++, dataIndex++)
            delay[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < file_name.Length; i++, dataIndex++)
            file_name[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < bitmap_ptr.Length; i++, dataIndex++)
            bitmap_ptr[i] = data[dataIndex];
    }
}

public class FirmInfo
{
    public int firm_id;
    public string name;
    public string short_name;

    public string overseer_title;
    public string worker_title;

    public int tera_type;

    // whether this building can be built by the player or it exists in the game since the beginning of the game. If setup_cost==0, this firm is not buildable
    public bool buildable;
    public bool live_in_town; // whether the workers of the firm lives in towns or not.
    public int max_hit_points;

    public bool need_overseer;
    public bool need_worker;

    public bool need_unit()
    {
        return need_overseer || need_worker;
    }

    public int setup_cost;
    public int year_cost;

    public int first_build_id;
    public int build_count;

    public int loc_width;
    public int loc_height;

    public int firm_skill_id; // the id. of the skill that fits this firm
    public int firm_race_id; // only can be built and operated by this race
    
    public FirmRes FirmRes { get; }

    private NationArray NationArray => Sys.Instance.NationArray;
    private UnitArray UnitArray => Sys.Instance.UnitArray;

    public FirmInfo(FirmRes firmRes)
    {
        FirmRes = firmRes;
    }

    public bool can_build(int unitRecno)
    {
        if (!buildable)
            return false;

        Unit unit = UnitArray[unitRecno];

        if (unit.NationId == 0)
            return false;

        if (get_nation_tech_level(unit.NationId) == 0)
            return false;

        //------ fortress of power ------//

        if (firm_id == Firm.FIRM_BASE) // only if the nation has acquired the myth to build it
        {
            if (unit.Rank == Unit.RANK_GENERAL ||
                unit.Rank == Unit.RANK_KING ||
                unit.Skill.skill_id == Skill.SKILL_PRAYING ||
                unit.Skill.skill_id == Skill.SKILL_CONSTRUCTION)
            {
                //----- each nation can only build one seat of power -----//

                if (unit.NationId > 0 && unit.RaceId > 0 &&
                    NationArray[unit.NationId].base_count_array[unit.RaceId - 1] == 0)
                {
                    //--- if this nation has acquired the needed scroll of power ---//

                    return NationArray[unit.NationId].know_base_array[unit.RaceId - 1] != 0;
                }
            }

            return false;
        }

        //------ a king or a unit with construction skill knows how to build all buildings -----//

        if (firm_race_id == 0)
        {
            if (unit.Rank == Unit.RANK_KING || unit.Skill.skill_id == Skill.SKILL_CONSTRUCTION)
                return true;
        }

        //----- if the firm is race specific, if the unit is right race, return true ----//

        if (firm_race_id == unit.RaceId)
            return true;

        //---- if the unit has the skill needed by the firm or the unit has general construction skill ----//

        if (firm_skill_id != 0 && firm_skill_id == unit.Skill.skill_id)
            return true;

        return false;
    }

    public bool is_linkable_to_town;

    public bool is_linkable_to_firm(int linkFirmId)
    {
        switch (firm_id)
        {
            case Firm.FIRM_FACTORY:
                return linkFirmId == Firm.FIRM_MINE || linkFirmId == Firm.FIRM_MARKET || linkFirmId == Firm.FIRM_HARBOR;

            case Firm.FIRM_MINE:
                return linkFirmId == Firm.FIRM_FACTORY || linkFirmId == Firm.FIRM_MARKET ||
                       linkFirmId == Firm.FIRM_HARBOR;

            case Firm.FIRM_MARKET:
                return linkFirmId == Firm.FIRM_MINE || linkFirmId == Firm.FIRM_FACTORY ||
                       linkFirmId == Firm.FIRM_HARBOR;

            case Firm.FIRM_INN: // for an inn to scan for neighbor inns quickly, the link line is not displayed
                return linkFirmId == Firm.FIRM_INN;

            case Firm.FIRM_HARBOR:
                return linkFirmId == Firm.FIRM_MARKET || linkFirmId == Firm.FIRM_MINE ||
                       linkFirmId == Firm.FIRM_FACTORY;

            default:
                return false;
        }
    }

    public int default_link_status(int linkFirmId)
    {
        bool enabled = false;

        switch (firm_id)
        {
            case Firm.FIRM_MINE:
                enabled = (linkFirmId != Firm.FIRM_MARKET);
                break;

            case Firm.FIRM_FACTORY:
                enabled = (linkFirmId == Firm.FIRM_MARKET) || (linkFirmId == Firm.FIRM_MINE);
                break;

            case Firm.FIRM_MARKET:
                enabled = (linkFirmId == Firm.FIRM_FACTORY) || (linkFirmId == Firm.FIRM_HARBOR);
                break;

            case Firm.FIRM_HARBOR:
                enabled = (linkFirmId == Firm.FIRM_MARKET) || (linkFirmId == Firm.FIRM_MINE) ||
                          (linkFirmId == Firm.FIRM_FACTORY);
                break;

            default:
                enabled = true;
                break;
        }

        return enabled ? InternalConstants.LINK_EE : InternalConstants.LINK_DD;
    }

    //---------- game vars -----------//

    public int total_firm_count; // total no. of this firm type on the map
    public int[] nation_firm_count_array = new int[GameConstants.MAX_NATION];
    public int[] nation_tech_level_array = new int[GameConstants.MAX_NATION];

    public int get_nation_tech_level(int nationRecno)
    {
        return nation_tech_level_array[nationRecno - 1];
    }

    public void set_nation_tech_level(int nationRecno, int techLevel)
    {
        nation_tech_level_array[nationRecno - 1] = techLevel;
    }

    public int get_build_id(string buildCode)
    {
        if (build_count == 1) // if this firm has only one building type
            return first_build_id;

        int firmBuildId = first_build_id;

        for (int i = 0; i < build_count; i++, firmBuildId++) // if this firm has one graphics for each race
        {
            if (buildCode == FirmRes.get_build(firmBuildId).build_code)
                return firmBuildId;
        }

        return 0;
    }

    public void inc_nation_firm_count(int nationRecno)
    {
        nation_firm_count_array[nationRecno - 1]++;
        NationArray[nationRecno].total_firm_count++;
    }

    public void dec_nation_firm_count(int nationRecno)
    {
        nation_firm_count_array[nationRecno - 1]--;
        NationArray[nationRecno].total_firm_count--;

        if (nation_firm_count_array[nationRecno - 1] < 0) // run-time bug fixing
            nation_firm_count_array[nationRecno - 1] = 0;
    }
}

public class FirmBuild
{
    public const int MAX_FIRM_FRAME = 11;

    // building code, either a race code or a custom code for each firm's own use, it is actually read from FirmBuildRec::race_code[]
    public string build_code;
    public int race_id;
    public bool animate_full_size;

    //----- info of the first frame -----//

    public int loc_width; // no. of locations it takes horizontally and vertically
    public int loc_height;

    public int min_offset_x, min_offset_y;
    public int max_bitmap_width, max_bitmap_height;

    //----------- frame info ------------//

    public int frame_count;

    public int[] first_bitmap_array = new int[MAX_FIRM_FRAME];
    public int[] bitmap_count_array = new int[MAX_FIRM_FRAME];
    public int[] frame_delay_array = new int[MAX_FIRM_FRAME]; // unit: 1/10 second

    public int under_construction_bitmap_recno; // bitmap recno of the firm that is under construction

    // ##### begin Gilbert 18/10 ########//
    public int under_construction_bitmap_count;

    // ##### end Gilbert 18/10 ########//
    public int idle_bitmap_recno; // bitmap recno of the firm that is idle
    public int ground_bitmap_recno;

    public int first_bitmap(int frameId)
    {
        return first_bitmap_array[frameId - 1];
    }

    public int bitmap_count(int frameId)
    {
        return bitmap_count_array[frameId - 1];
    }

    public int frame_delay(int frameId)
    {
        return frame_delay_array[frameId - 1];
    }
}

public class FirmBitmap
{
    public int loc_width; // no. of locations it takes horizontally and vertically
    public int loc_height;
    public int offset_x;
    public int offset_y;
    public int display_layer;

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
            byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(bitmap, bitmapWidth, bitmapHeight,
                ColorRemap.GetColorRemap(colorScheme, isSelected).ColorTable);
            IntPtr texture = graphics.CreateTextureFromBmp(decompressedBitmap, bitmapWidth, bitmapHeight);
            textures.Add(textureKey, texture);
        }
        
        return textures[textureKey];
    }
}

public class FirmRes
{
    public const string FIRM_DB = "FIRM";
    public const string FIRM_BUILD_DB = "FBUILD";
    public const string FIRM_FRAME_DB = "FFRAME";
    public const string FIRM_BITMAP_DB = "FBITMAP";

    public FirmInfo[] firm_info_array;
    public FirmBuild[] firm_build_array;
    public FirmBitmap[] firm_bitmap_array;

    public ResourceDb res_bitmap;
    
    public GameSet GameSet { get; }

    public FirmRes(GameSet gameSet)
    {
        GameSet = gameSet;
        
        res_bitmap = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_FIRM.RES");

        // call LoadFirmBitmap() first as LoadFirmInfo() will need info loaded by LoadFirmBitmap()
        LoadFirmBitmap();
        LoadFirmBuild();
        LoadFirmInfo();

        //------------ set firm skill ------------//

        this[Firm.FIRM_BASE].firm_skill_id = Skill.SKILL_LEADING;
        this[Firm.FIRM_CAMP].firm_skill_id = Skill.SKILL_LEADING;
        this[Firm.FIRM_MINE].firm_skill_id = Skill.SKILL_MINING;
        this[Firm.FIRM_FACTORY].firm_skill_id = Skill.SKILL_MFT;
        this[Firm.FIRM_RESEARCH].firm_skill_id = Skill.SKILL_RESEARCH;
        this[Firm.FIRM_WAR_FACTORY].firm_skill_id = Skill.SKILL_MFT;
    }

    public FirmBitmap get_bitmap(int bitmapId)
    {
        return firm_bitmap_array[bitmapId - 1];
    }

    public FirmBuild get_build(int buildId)
    {
        return firm_build_array[buildId - 1];
    }

    public FirmInfo this[int firmId] => firm_info_array[firmId - 1];

    private void LoadFirmInfo()
    {
        //---- read in firm count and initialize firm info array ----//

        // only one database can be opened at a time, so we read FIRM.DBF first
        Database dbFirm = GameSet.OpenDb(FIRM_DB);

        firm_info_array = new FirmInfo[dbFirm.RecordCount];

        //---------- read in FIRM.DBF ---------//

        for (int i = 0; i < firm_info_array.Length; i++)
        {
            FirmRec firmRec = new FirmRec(dbFirm.Read(i + 1));
            FirmInfo firmInfo = new FirmInfo(this);
            firm_info_array[i] = firmInfo;

            firmInfo.name = Misc.ToString(firmRec.name);
            firmInfo.short_name = Misc.ToString(firmRec.short_name);
            firmInfo.overseer_title = Misc.ToString(firmRec.overseer_title);
            firmInfo.worker_title = Misc.ToString(firmRec.worker_title);

            firmInfo.firm_id = i + 1;
            firmInfo.tera_type = firmRec.tera_type - '0';
            firmInfo.live_in_town = (firmRec.live_in_town == '1');

            firmInfo.max_hit_points = Misc.ToInt32(firmRec.hit_points);

            firmInfo.first_build_id = Misc.ToInt32(firmRec.first_build);
            firmInfo.build_count = Misc.ToInt32(firmRec.build_count);

            firmInfo.need_overseer = !String.IsNullOrEmpty(firmInfo.overseer_title);
            firmInfo.need_worker = !String.IsNullOrEmpty(firmInfo.worker_title);

            firmInfo.is_linkable_to_town = (firmRec.is_linkable_to_town == '1');

            firmInfo.setup_cost = Misc.ToInt32(firmRec.setup_cost);
            firmInfo.year_cost = Misc.ToInt32(firmRec.year_cost);

            firmInfo.buildable = (firmInfo.setup_cost > 0);

            if (firmRec.all_know == '1')
            {
                for (int j = 0; j < firmInfo.nation_tech_level_array.Length; j++)
                {
                    firmInfo.nation_tech_level_array[j] = 1;
                }
            }

            //------- set loc_width & loc_height in FirmInfo --------//

            FirmBuild firmBuild = firm_build_array[firmInfo.first_build_id - 1];

            firmInfo.loc_width = firmBuild.loc_width;
            firmInfo.loc_height = firmBuild.loc_height;

            //------------- set firm_race_id --------------//

            // if only one building style for this firm, take the race id. of the building as the race of the firm
            if (firmInfo.build_count == 1)
                firmInfo.firm_race_id = firmBuild.race_id;
        }
    }

    private void LoadFirmBuild()
    {
        //---- read in firm count and initialize firm info array ----//

        // only one database can be opened at a time, so we read FIRM.DBF first
        Database dbFirmBuild = GameSet.OpenDb(FIRM_BUILD_DB);

        firm_build_array = new FirmBuild[dbFirmBuild.RecordCount];
        int[] firstFrameArray = new int[dbFirmBuild.RecordCount];

        //------ allocate an array for storing firstFrameRecno -----//

        //---------- read in FBUILD.DBF ---------//

        for (int i = 0; i < firm_build_array.Length; i++)
        {
            FirmBuildRec firmBuildRec = new FirmBuildRec(dbFirmBuild.Read(i + 1));
            FirmBuild firmBuild = new FirmBuild();
            firm_build_array[i] = firmBuild;

            firmBuild.build_code = Misc.ToString(firmBuildRec.race_code);

            firmBuild.animate_full_size = (firmBuildRec.animate_full_size == '1');

            firmBuild.race_id = Misc.ToInt32(firmBuildRec.race_id);
            firmBuild.frame_count = Misc.ToInt32(firmBuildRec.frame_count);

            firmBuild.under_construction_bitmap_recno = Misc.ToInt32(firmBuildRec.under_construction_bitmap_recno);
            firmBuild.under_construction_bitmap_count = Misc.ToInt32(firmBuildRec.under_construction_bitmap_count);
            firmBuild.idle_bitmap_recno = Misc.ToInt32(firmBuildRec.idle_bitmap_recno);
            firmBuild.ground_bitmap_recno = Misc.ToInt32(firmBuildRec.ground_bitmap_recno);

            firstFrameArray[i] = Misc.ToInt32(firmBuildRec.first_frame);
        }

        //-------- read in FFRAME.DBF --------//

        Database dbFirmFrame = GameSet.OpenDb(FIRM_FRAME_DB);
        int minOffsetX, minOffsetY;
        int maxX2, maxY2;

        for (int i = 0; i < firm_build_array.Length; i++)
        {
            FirmBuild firmBuild = firm_build_array[i];
            int frameRecno = firstFrameArray[i];

            minOffsetX = minOffsetY = 0xFFFF;
            maxX2 = maxY2 = 0;

            for (int j = 0; j < firmBuild.frame_count; j++, frameRecno++)
            {
                FirmFrameRec firmFrameRec = new FirmFrameRec(dbFirmFrame.Read(frameRecno));

                //------ following animation frames, bitmap sections -----//

                firmBuild.first_bitmap_array[j] = Misc.ToInt32(firmFrameRec.first_bitmap);
                firmBuild.bitmap_count_array[j] = Misc.ToInt32(firmFrameRec.bitmap_count);
                firmBuild.frame_delay_array[j] = Misc.ToInt32(firmFrameRec.delay);

                //---- get the MIN offset_x, offset_y and MAX width, height ----//
                //
                // So we can get the largest area of all the frames in this building
                // and this will serve as a normal size setting for this building,
                // with variation from frame to frame
                //
                //--------------------------------------------------------------//

                for (int k = firmBuild.bitmap_count_array[j]; k > 0; k--)
                {
                    FirmBitmap firmBitmap = firm_bitmap_array[k - 1];
                    if (firmBitmap.offset_x < minOffsetX)
                        minOffsetX = firmBitmap.offset_x;

                    if (firmBitmap.offset_y < minOffsetY)
                        minOffsetY = firmBitmap.offset_y;

                    if (firmBitmap.offset_x + firmBitmap.bitmapWidth > maxX2)
                        maxX2 = firmBitmap.offset_x + firmBitmap.bitmapWidth;

                    if (firmBitmap.offset_y + firmBitmap.bitmapHeight > maxY2)
                        maxY2 = firmBitmap.offset_y + firmBitmap.bitmapHeight;
                }
            }

            //------- set FirmBuild Info -------//

            int bitmapRecno = firmBuild.first_bitmap_array[0];

            //----- get the info of the first frame bitmap ----//

            FirmBitmap firstBitmap = firm_bitmap_array[bitmapRecno - 1];

            firmBuild.loc_width = firstBitmap.loc_width;
            firmBuild.loc_height = firstBitmap.loc_height;

            firmBuild.min_offset_x = minOffsetX;
            firmBuild.min_offset_y = minOffsetY;

            firmBuild.max_bitmap_width = maxX2 - minOffsetX;
            firmBuild.max_bitmap_height = maxY2 - minOffsetY;

            //------ set firmBuild's under construction and idle bitmap recno -----//

            if (firmBuild.under_construction_bitmap_recno == 0)
            {
                firmBuild.under_construction_bitmap_recno = bitmapRecno;
                firmBuild.under_construction_bitmap_count = 1;
            }

            if (firmBuild.idle_bitmap_recno == 0)
                firmBuild.idle_bitmap_recno = bitmapRecno;
        }
    }

    private void LoadFirmBitmap()
    {
        Database dbFirmBitmap = GameSet.OpenDb(FIRM_BITMAP_DB);

        firm_bitmap_array = new FirmBitmap[dbFirmBitmap.RecordCount];

        for (int i = 0; i < firm_bitmap_array.Length; i++)
        {
            FirmBitmapRec firmBitmapRec = new FirmBitmapRec(dbFirmBitmap.Read(i + 1));
            FirmBitmap firmBitmap = new FirmBitmap();
            firm_bitmap_array[i] = firmBitmap;

            int bitmapOffset = BitConverter.ToInt32(firmBitmapRec.bitmap_ptr, 0);
            firmBitmap.bitmap = res_bitmap.Read(bitmapOffset);
            firmBitmap.bitmapWidth = BitConverter.ToInt16(firmBitmap.bitmap, 0);
            firmBitmap.bitmapHeight = BitConverter.ToInt16(firmBitmap.bitmap, 2);
            firmBitmap.bitmap = firmBitmap.bitmap.Skip(4).ToArray();

            firmBitmap.offset_x = Misc.ToInt32(firmBitmapRec.offset_x);
            firmBitmap.offset_y = Misc.ToInt32(firmBitmapRec.offset_y);

            firmBitmap.loc_width = Misc.ToInt32(firmBitmapRec.loc_width);
            firmBitmap.loc_height = Misc.ToInt32(firmBitmapRec.loc_height);
            firmBitmap.display_layer = firmBitmapRec.layer - '0';
        }
    }
}
