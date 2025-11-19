using System;
using System.Collections.Generic;
using System.Linq;

namespace TenKingdoms;

public class FirmRec
{
    private const int CODE_LEN = 8;
    private const int NAME_LEN = 20;
    private const int SHORT_NAME_LEN = 12;
    private const int TITLE_LEN = 10;
    private const int FIRST_BUILD_LEN = 3;
    private const int BUILD_COUNT_LEN = 3;
    private const int HIT_POINTS_LEN = 5;
    private const int COST_LEN = 5;

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

    public FirmRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < code.Length; i++, dataIndex++)
            code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < name.Length; i++, dataIndex++)
            name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < short_name.Length; i++, dataIndex++)
            short_name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < overseer_title.Length; i++, dataIndex++)
            overseer_title[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < worker_title.Length; i++, dataIndex++)
            worker_title[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        tera_type = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        dataIndex++;
        all_know = db.ReadByte(recNo, dataIndex);
        dataIndex++;
        live_in_town = db.ReadByte(recNo, dataIndex);
        dataIndex++;
        
        for (int i = 0; i < hit_points.Length; i++, dataIndex++)
            hit_points[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        is_linkable_to_town = db.ReadByte(recNo, dataIndex);
        dataIndex++;
        
        for (int i = 0; i < setup_cost.Length; i++, dataIndex++)
            setup_cost[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < year_cost.Length; i++, dataIndex++)
            year_cost[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < first_build.Length; i++, dataIndex++)
            first_build[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < build_count.Length; i++, dataIndex++)
            build_count[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
    }
}

public class FirmBuildRec
{
    private const int FIRM_CODE_LEN = 8;
    private const int RACE_CODE_LEN = 8;
    private const int BITMAP_RECNO_LEN = 5;
    private const int FIRST_FRAME_LEN = 5;
    private const int FRAME_COUNT_LEN = 2;
    private const int RACE_ID_LEN = 3;

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

    public FirmBuildRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < firm_code.Length; i++, dataIndex++)
            firm_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < race_code.Length; i++, dataIndex++)
            race_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        animate_full_size = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        dataIndex++;
        
        for (int i = 0; i < under_construction_bitmap_recno.Length; i++, dataIndex++)
            under_construction_bitmap_recno[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < under_construction_bitmap_count.Length; i++, dataIndex++)
            under_construction_bitmap_count[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < idle_bitmap_recno.Length; i++, dataIndex++)
            idle_bitmap_recno[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < ground_bitmap_recno.Length; i++, dataIndex++)
            ground_bitmap_recno[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < first_frame.Length; i++, dataIndex++)
            first_frame[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < frame_count.Length; i++, dataIndex++)
            frame_count[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < race_id.Length; i++, dataIndex++)
            race_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
    }
}

public class FirmFrameRec
{
    private const int FIRM_CODE_LEN = 8;
    private const int RACE_CODE_LEN = 8;
    private const int FRAME_ID_LEN = 2;
    private const int DELAY_LEN = 2;
    private const int FIRST_BITMAP_LEN = 5;
    private const int BITMAP_COUNT_LEN = 2;

    public char[] firm_code = new char[FIRM_CODE_LEN];
    public char[] race_code = new char[RACE_CODE_LEN];

    public char[] frame_id = new char[FRAME_ID_LEN];

    public char[] delay = new char[DELAY_LEN]; // unit: 1/10 second

    public char[] first_bitmap = new char[FIRST_BITMAP_LEN];
    public char[] bitmap_count = new char[BITMAP_COUNT_LEN];

    public FirmFrameRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < firm_code.Length; i++, dataIndex++)
            firm_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < race_code.Length; i++, dataIndex++)
            race_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < frame_id.Length; i++, dataIndex++)
            frame_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < delay.Length; i++, dataIndex++)
            delay[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < first_bitmap.Length; i++, dataIndex++)
            first_bitmap[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < bitmap_count.Length; i++, dataIndex++)
            bitmap_count[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
    }
}

public class FirmBitmapRec
{
    private const int FIRM_CODE_LEN = 8;
    private const int RACE_CODE_LEN = 8;
    private const int FRAME_ID_LEN = 2;
    private const int LOC_LEN = 3;
    private const int OFFSET_LEN = 3;
    private const int DELAY_LEN = 2;
    private const int FILE_NAME_LEN = 8;
    private const int BITMAP_PTR_LEN = 4;

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

    public FirmBitmapRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < firm_code.Length; i++, dataIndex++)
            firm_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < race_code.Length; i++, dataIndex++)
            race_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        mode = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        dataIndex++;
        
        for (int i = 0; i < frame_id.Length; i++, dataIndex++)
            frame_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < loc_width.Length; i++, dataIndex++)
            loc_width[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < loc_height.Length; i++, dataIndex++)
            loc_height[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        layer = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        dataIndex++;
        
        for (int i = 0; i < offset_x.Length; i++, dataIndex++)
            offset_x[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < offset_y.Length; i++, dataIndex++)
            offset_y[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < delay.Length; i++, dataIndex++)
            delay[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < file_name.Length; i++, dataIndex++)
            file_name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < bitmap_ptr.Length; i++, dataIndex++)
            bitmap_ptr[i] = db.ReadByte(recNo, dataIndex);
    }
}

public class FirmInfo
{
    public int FirmType { get; set; }
    public string Name { get; set; }
    public string ShortName { get; set; }

    public string OverseerTitle { get; set; }
    public string WorkerTitle { get; set; }

    public int TeraType { get; set; }

    // whether this building can be built by the player or it exists in the game since the beginning of the game. If setup_cost==0, this firm is not buildable
    public bool Buildable { get; set; }
    public bool LiveInTown { get; set; } // whether the workers of the firm lives in towns or not.
    public int MaxHitPoints { get; set; }

    public bool NeedOverseer { get; set; }
    public bool NeedWorker { get; set; }

    public int SetupCost { get; set; }
    public int YearCost { get; set; }

    public int FirstBuildId { get; set; }
    public int BuildCount { get; set; }

    public int LocWidth { get; set; }
    public int LocHeight { get; set; }

    public int FirmSkillId { get; set; } // the id. of the skill that fits this firm
    public int FirmRaceId { get; set; } // only can be built and operated by this race
    public bool IsLinkableToTown { get; set; }

    public byte[] FlagBitmap { get; set; }
    public int FlagBitmapWidth { get; set; }
    public int FlagBitmapHeight { get; set; }
    private readonly Dictionary<int, IntPtr> _flagTextures = new Dictionary<int, nint>();
    
    private FirmRes FirmRes { get; }

    private NationArray NationArray => Sys.Instance.NationArray;

    public FirmInfo(FirmRes firmRes)
    {
        FirmRes = firmRes;
    }

    public bool NeedUnit()
    {
        return NeedOverseer || NeedWorker;
    }
    
    public bool IsLinkableToFirm(int linkFirmId)
    {
        switch (FirmType)
        {
            case Firm.FIRM_FACTORY:
                return linkFirmId == Firm.FIRM_MINE || linkFirmId == Firm.FIRM_MARKET || linkFirmId == Firm.FIRM_HARBOR;

            case Firm.FIRM_MINE:
                return linkFirmId == Firm.FIRM_FACTORY || linkFirmId == Firm.FIRM_MARKET || linkFirmId == Firm.FIRM_HARBOR;

            case Firm.FIRM_MARKET:
                return linkFirmId == Firm.FIRM_MINE || linkFirmId == Firm.FIRM_FACTORY || linkFirmId == Firm.FIRM_HARBOR;

            case Firm.FIRM_INN: // for an inn to scan for neighbor inns quickly, the link line is not displayed
                return linkFirmId == Firm.FIRM_INN;

            case Firm.FIRM_HARBOR:
                return linkFirmId == Firm.FIRM_MARKET || linkFirmId == Firm.FIRM_MINE || linkFirmId == Firm.FIRM_FACTORY;

            default:
                return false;
        }
    }

    public int DefaultLinkStatus(int linkFirmId)
    {
        bool enabled = false;

        switch (FirmType)
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
                enabled = (linkFirmId == Firm.FIRM_MARKET) || (linkFirmId == Firm.FIRM_MINE) || (linkFirmId == Firm.FIRM_FACTORY);
                break;

            default:
                enabled = true;
                break;
        }

        return enabled ? InternalConstants.LINK_EE : InternalConstants.LINK_DD;
    }

    public int GetBuildId(string buildCode)
    {
        if (BuildCount == 1) // if this firm has only one building type
            return FirstBuildId;

        int firmBuildId = FirstBuildId;

        for (int i = 0; i < BuildCount; i++, firmBuildId++) // if this firm has one graphics for each race
        {
            if (buildCode == FirmRes.GetBuild(firmBuildId).BuildCode)
                return firmBuildId;
        }

        return 0;
    }
    
    public IntPtr GetFlagTexture(Graphics graphics, int nationColor)
    {
        int colorScheme = ColorRemap.ColorSchemes[nationColor];
        int textureKey = ColorRemap.GetTextureKey(colorScheme, false);
        if (!_flagTextures.ContainsKey(textureKey))
        {
            byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(FlagBitmap, FlagBitmapWidth, FlagBitmapHeight,
                ColorRemap.GetColorRemap(colorScheme, false).ColorTable);
            IntPtr texture = graphics.CreateTextureFromBmp(decompressedBitmap, FlagBitmapWidth, FlagBitmapHeight);
            _flagTextures.Add(textureKey, texture);
        }

        return _flagTextures[textureKey];
    }

    //TODO remove
    //---------- game vars -----------//

    public int total_firm_count { get; set; } // total no. of this firm type on the map
    public int[] nation_firm_count_array { get; } = new int[GameConstants.MAX_NATION];
    public int[] nation_tech_level_array { get; } = new int[GameConstants.MAX_NATION];

    public int get_nation_tech_level(int nationRecno)
    {
        return nation_tech_level_array[nationRecno - 1];
    }

    public void set_nation_tech_level(int nationRecno, int techLevel)
    {
        nation_tech_level_array[nationRecno - 1] = techLevel;
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
    private const int MAX_FIRM_FRAME = 11;

    // building code, either a race code or a custom code for each firm's own use, it is actually read from FirmBuildRec::race_code[]
    public string BuildCode { get; set; }
    public int RaceId { get; set; }
    public bool AnimateFullSize { get; set; }

    public int LocWidth { get; set; } // no. of locations it takes horizontally and vertically
    public int LocHeight { get; set; }

    public int MinOffsetX { get; set; }
    public int MinOffsetY { get; set; }
    public int MaxBitmapWidth { get; set; }
    public int MaxBitmapHeight { get; set; }

    public int FrameCount { get; set; }

    public int UnderConstructionBitmapId { get; set; }

    public int UnderConstructionBitmapCount { get; set; }

    public int GroundBitmapId { get; set; }
    public int IdleBitmapId { get; set; }
    
    public int[] FirstBitmaps { get; } = new int[MAX_FIRM_FRAME];
    public int[] BitmapCounts { get; } = new int[MAX_FIRM_FRAME];
    public int[] FrameDelays { get; } = new int[MAX_FIRM_FRAME]; // unit: 1/10 second


    public int FirstBitmap(int frameId)
    {
        return FirstBitmaps[frameId - 1];
    }

    public int BitmapCount(int frameId)
    {
        return BitmapCounts[frameId - 1];
    }

    public int FrameDelay(int frameId)
    {
        return FrameDelays[frameId - 1];
    }
}

public class FirmBitmap
{
    public int LocWidth { get; set; }
    public int LocHeight { get; set; }
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }
    public int DisplayLayer { get; set; }

    public byte[] Bitmap { get; set; }
    public int BitmapWidth { get; set; }
    public int BitmapHeight { get; set; }
    private readonly Dictionary<int, IntPtr> _textures = new Dictionary<int, nint>();

    public IntPtr GetTexture(Graphics graphics, int nationColor, bool isSelected)
    {
        int colorScheme = ColorRemap.ColorSchemes[nationColor];
        int textureKey = ColorRemap.GetTextureKey(colorScheme, isSelected);
        if (!_textures.ContainsKey(textureKey))
        {
            byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(Bitmap, BitmapWidth, BitmapHeight,
                ColorRemap.GetColorRemap(colorScheme, isSelected).ColorTable);
            IntPtr texture = graphics.CreateTextureFromBmp(decompressedBitmap, BitmapWidth, BitmapHeight);
            _textures.Add(textureKey, texture);
        }
        
        return _textures[textureKey];
    }
}

public class FirmRes
{
    private const string FIRM_DB = "FIRM";
    private const string FIRM_BUILD_DB = "FBUILD";
    private const string FIRM_FRAME_DB = "FFRAME";
    private const string FIRM_BITMAP_DB = "FBITMAP";

    private FirmInfo[] FirmInfos { get; set; }
    private FirmBuild[] FirmBuilds { get; set; }
    private FirmBitmap[] FirmBitmaps { get; set; }

    private GameSet GameSet { get; }

    public FirmRes(GameSet gameSet)
    {
        GameSet = gameSet;
        
        // call LoadFirmBitmap() first as LoadFirmInfo() will need info loaded by LoadFirmBitmap()
        LoadFirmBitmap();
        LoadFirmBuild();
        LoadFirmInfo();

        this[Firm.FIRM_BASE].FirmSkillId = Skill.SKILL_LEADING;
        this[Firm.FIRM_CAMP].FirmSkillId = Skill.SKILL_LEADING;
        this[Firm.FIRM_MINE].FirmSkillId = Skill.SKILL_MINING;
        this[Firm.FIRM_FACTORY].FirmSkillId = Skill.SKILL_MFT;
        this[Firm.FIRM_RESEARCH].FirmSkillId = Skill.SKILL_RESEARCH;
        this[Firm.FIRM_WAR_FACTORY].FirmSkillId = Skill.SKILL_MFT;
    }

    public FirmBitmap GetBitmap(int bitmapId)
    {
        return FirmBitmaps[bitmapId - 1];
    }

    public FirmBuild GetBuild(int buildId)
    {
        return FirmBuilds[buildId - 1];
    }

    public FirmInfo this[int firmId] => FirmInfos[firmId - 1];

    private void LoadFirmInfo()
    {
        ResourceIdx flagResources = new ResourceIdx($"{Sys.GameDataFolder}/Resource/I_SPICT.RES");

        Database dbFirm = GameSet.OpenDb(FIRM_DB);
        FirmInfos = new FirmInfo[dbFirm.RecordCount];

        for (int i = 0; i < FirmInfos.Length; i++)
        {
            FirmRec firmRec = new FirmRec(dbFirm, i + 1);
            FirmInfo firmInfo = new FirmInfo(this);
            FirmInfos[i] = firmInfo;

            firmInfo.Name = Misc.ToString(firmRec.name);
            firmInfo.ShortName = Misc.ToString(firmRec.short_name);
            firmInfo.OverseerTitle = Misc.ToString(firmRec.overseer_title);
            firmInfo.WorkerTitle = Misc.ToString(firmRec.worker_title);

            firmInfo.FirmType = i + 1;
            firmInfo.TeraType = firmRec.tera_type - '0';
            firmInfo.LiveInTown = (firmRec.live_in_town == '1');

            firmInfo.MaxHitPoints = Misc.ToInt32(firmRec.hit_points);

            firmInfo.FirstBuildId = Misc.ToInt32(firmRec.first_build);
            firmInfo.BuildCount = Misc.ToInt32(firmRec.build_count);

            firmInfo.NeedOverseer = !String.IsNullOrEmpty(firmInfo.OverseerTitle);
            firmInfo.NeedWorker = !String.IsNullOrEmpty(firmInfo.WorkerTitle);

            firmInfo.IsLinkableToTown = (firmRec.is_linkable_to_town == '1');

            firmInfo.SetupCost = Misc.ToInt32(firmRec.setup_cost);
            firmInfo.YearCost = Misc.ToInt32(firmRec.year_cost);

            firmInfo.Buildable = (firmInfo.SetupCost > 0);

            if (firmRec.all_know == '1')
            {
                for (int j = 0; j < firmInfo.nation_tech_level_array.Length; j++)
                {
                    firmInfo.nation_tech_level_array[j] = 1;
                }
            }

            FirmBuild firmBuild = FirmBuilds[firmInfo.FirstBuildId - 1];

            firmInfo.LocWidth = firmBuild.LocWidth;
            firmInfo.LocHeight = firmBuild.LocHeight;

            // if only one building style for this firm, take the race id. of the building as the race of the firm
            if (firmInfo.BuildCount == 1)
                firmInfo.FirmRaceId = firmBuild.RaceId;
            
            byte[] flagData = flagResources.Read("FLAG-S0");
            firmInfo.FlagBitmapWidth = BitConverter.ToInt16(flagData, 0);
            firmInfo.FlagBitmapHeight = BitConverter.ToInt16(flagData, 2);
            firmInfo.FlagBitmap = flagData.Skip(4).ToArray();
        }
    }

    private void LoadFirmBuild()
    {
        Database dbFirmBuild = GameSet.OpenDb(FIRM_BUILD_DB);
        FirmBuilds = new FirmBuild[dbFirmBuild.RecordCount];
        int[] firstFrameIds = new int[dbFirmBuild.RecordCount];

        for (int i = 0; i < FirmBuilds.Length; i++)
        {
            FirmBuildRec firmBuildRec = new FirmBuildRec(dbFirmBuild, i + 1);
            FirmBuild firmBuild = new FirmBuild();
            FirmBuilds[i] = firmBuild;

            firmBuild.BuildCode = Misc.ToString(firmBuildRec.race_code);

            firmBuild.AnimateFullSize = (firmBuildRec.animate_full_size == '1');

            firmBuild.RaceId = Misc.ToInt32(firmBuildRec.race_id);
            firmBuild.FrameCount = Misc.ToInt32(firmBuildRec.frame_count);

            firmBuild.UnderConstructionBitmapId = Misc.ToInt32(firmBuildRec.under_construction_bitmap_recno);
            firmBuild.UnderConstructionBitmapCount = Misc.ToInt32(firmBuildRec.under_construction_bitmap_count);
            firmBuild.IdleBitmapId = Misc.ToInt32(firmBuildRec.idle_bitmap_recno);
            firmBuild.GroundBitmapId = Misc.ToInt32(firmBuildRec.ground_bitmap_recno);

            firstFrameIds[i] = Misc.ToInt32(firmBuildRec.first_frame);
        }

        Database dbFirmFrame = GameSet.OpenDb(FIRM_FRAME_DB);
        for (int i = 0; i < FirmBuilds.Length; i++)
        {
            FirmBuild firmBuild = FirmBuilds[i];
            int firstFrameId = firstFrameIds[i];

            int minOffsetX = Int32.MaxValue / 2;
            int minOffsetY = Int32.MaxValue / 2;
            int maxX2 = 0;
            int maxY2 = 0;

            for (int j = 0; j < firmBuild.FrameCount; j++, firstFrameId++)
            {
                FirmFrameRec firmFrameRec = new FirmFrameRec(dbFirmFrame, firstFrameId);

                //------ following animation frames, bitmap sections -----//

                firmBuild.FirstBitmaps[j] = Misc.ToInt32(firmFrameRec.first_bitmap);
                firmBuild.BitmapCounts[j] = Misc.ToInt32(firmFrameRec.bitmap_count);
                firmBuild.FrameDelays[j] = Misc.ToInt32(firmFrameRec.delay);

                //---- get the MIN offset_x, offset_y and MAX width, height ----//
                //
                // So we can get the largest area of all the frames in this building
                // and this will serve as a normal size setting for this building,
                // with variation from frame to frame
                //
                //--------------------------------------------------------------//

                int firmBitmapIndex = 0;
                for (int k = firmBuild.BitmapCounts[j]; k > 0; k--, firmBitmapIndex++)
                {
                    FirmBitmap firmBitmap = FirmBitmaps[firmBuild.FirstBitmaps[j] - 1 + firmBitmapIndex];
                    if (firmBitmap.OffsetX < minOffsetX)
                        minOffsetX = firmBitmap.OffsetX;

                    if (firmBitmap.OffsetY < minOffsetY)
                        minOffsetY = firmBitmap.OffsetY;

                    if (firmBitmap.OffsetX + firmBitmap.BitmapWidth > maxX2)
                        maxX2 = firmBitmap.OffsetX + firmBitmap.BitmapWidth;

                    if (firmBitmap.OffsetY + firmBitmap.BitmapHeight > maxY2)
                        maxY2 = firmBitmap.OffsetY + firmBitmap.BitmapHeight;
                }
            }

            int bitmapId = firmBuild.FirstBitmaps[0];

            FirmBitmap firstBitmap = FirmBitmaps[bitmapId - 1];

            firmBuild.LocWidth = firstBitmap.LocWidth;
            firmBuild.LocHeight = firstBitmap.LocHeight;

            firmBuild.MinOffsetX = minOffsetX;
            firmBuild.MinOffsetY = minOffsetY;

            firmBuild.MaxBitmapWidth = maxX2 - minOffsetX;
            firmBuild.MaxBitmapHeight = maxY2 - minOffsetY;

            if (firmBuild.UnderConstructionBitmapId == 0)
            {
                firmBuild.UnderConstructionBitmapId = bitmapId;
                firmBuild.UnderConstructionBitmapCount = 1;
            }

            if (firmBuild.IdleBitmapId == 0)
                firmBuild.IdleBitmapId = bitmapId;
        }
    }

    private void LoadFirmBitmap()
    {
        ResourceDb firmBitmaps = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_FIRM.RES");
        Database dbFirmBitmap = GameSet.OpenDb(FIRM_BITMAP_DB);
        FirmBitmaps = new FirmBitmap[dbFirmBitmap.RecordCount];

        for (int i = 0; i < FirmBitmaps.Length; i++)
        {
            FirmBitmapRec firmBitmapRec = new FirmBitmapRec(dbFirmBitmap, i + 1);
            FirmBitmap firmBitmap = new FirmBitmap();
            FirmBitmaps[i] = firmBitmap;

            firmBitmap.OffsetX = Misc.ToInt32(firmBitmapRec.offset_x);
            firmBitmap.OffsetY = Misc.ToInt32(firmBitmapRec.offset_y);

            firmBitmap.LocWidth = Misc.ToInt32(firmBitmapRec.loc_width);
            firmBitmap.LocHeight = Misc.ToInt32(firmBitmapRec.loc_height);
            firmBitmap.DisplayLayer = firmBitmapRec.layer - '0';
            
            int bitmapOffset = BitConverter.ToInt32(firmBitmapRec.bitmap_ptr, 0);
            firmBitmap.Bitmap = firmBitmaps.Read(bitmapOffset);
            firmBitmap.BitmapWidth = BitConverter.ToInt16(firmBitmap.Bitmap, 0);
            firmBitmap.BitmapHeight = BitConverter.ToInt16(firmBitmap.Bitmap, 2);
            firmBitmap.Bitmap = firmBitmap.Bitmap.Skip(4).ToArray();
        }
    }
}
