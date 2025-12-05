using System;
using System.Linq;

namespace TenKingdoms;

public class TechClassRec
{
    public const int CODE_LEN = 8;
    public const int ICON_NAME_LEN = 8;

    public char[] class_code = new char[CODE_LEN];
    public char[] icon_name = new char[ICON_NAME_LEN];

    public TechClassRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < class_code.Length; i++, dataIndex++)
            class_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < icon_name.Length; i++, dataIndex++)
            icon_name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
    }
}

public class TechRec
{
    public const int CODE_LEN = 8;
    public const int MAX_TECH_LEVEL_LEN = 3;
    public const int COMPLEX_LEVEL_LEN = 3;
    public const int ID_LEN = 3;
    public const int ICON_NAME_LEN = 8;

    public char[] class_code = new char[CODE_LEN];

    public char[] max_tech_level = new char[MAX_TECH_LEVEL_LEN];
    public char[] complex_level = new char[COMPLEX_LEVEL_LEN];

    public char[] unit_code = new char[CODE_LEN];
    public char[] firm_code = new char[CODE_LEN];
    public char[] parent_unit_code = new char[CODE_LEN];
    public char[] parent_firm_code = new char[CODE_LEN];
    public char parent_level;

    public char[] icon_name = new char[ICON_NAME_LEN];

    public char[] class_id = new char[ID_LEN];
    public char[] unit_id = new char[ID_LEN];
    public char[] firm_id = new char[ID_LEN];
    public char[] parent_unit_id = new char[ID_LEN];
    public char[] parent_firm_id = new char[ID_LEN];

    public TechRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < class_code.Length; i++, dataIndex++)
            class_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < max_tech_level.Length; i++, dataIndex++)
            max_tech_level[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < complex_level.Length; i++, dataIndex++)
            complex_level[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < unit_code.Length; i++, dataIndex++)
            unit_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < firm_code.Length; i++, dataIndex++)
            firm_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < parent_unit_code.Length; i++, dataIndex++)
            parent_unit_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < parent_firm_code.Length; i++, dataIndex++)
            parent_firm_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        parent_level = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        dataIndex++;

        for (int i = 0; i < icon_name.Length; i++, dataIndex++)
            icon_name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < class_id.Length; i++, dataIndex++)
            class_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < unit_id.Length; i++, dataIndex++)
            unit_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < firm_id.Length; i++, dataIndex++)
            firm_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < parent_unit_id.Length; i++, dataIndex++)
            parent_unit_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < parent_firm_id.Length; i++, dataIndex++)
            parent_firm_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
    }
}

public class TechClass
{
    public int class_id;
    public int first_tech_id;
    public int tech_count;

    public int icon_index;

    public TechRes TechRes => Sys.Instance.TechRes;

    public byte[] tech_icon()
    {
        return TechRes.res_bitmap.GetData(icon_index + 1);
    }
}

public class TechInfo
{
    public int tech_id;
    public int class_id;

    public int max_tech_level; // maximum level for this technology
    public int complex_level;

    public int unit_id;
    public int firm_id;
    public int parent_unit_id;
    public int parent_firm_id;
    public int parent_level;

    public int icon_index;

    public TechRes TechRes => Sys.Instance.TechRes;
    public FirmRes FirmRes => Sys.Instance.FirmRes;
    public UnitRes UnitRes => Sys.Instance.UnitRes;
    public FirmArray FirmArray => Sys.Instance.FirmArray;

    public int TechLargeIconWidth { get; set; }
    public int TechLargeIconHeight { get; set; }

    public IntPtr GetTechLargeIconTexture(Graphics graphics)
    {
        TechLargeIconWidth = UnitRes[unit_id].soldierIconWidth;
        TechLargeIconHeight = UnitRes[unit_id].soldierIconHeight;
        return UnitRes[unit_id].GetSoldierIconTexture(graphics);
    }

    public byte[] tech_small_icon()
    {
        if (unit_id != 0)
            return UnitRes[unit_id].get_small_icon_ptr(Unit.RANK_SOLDIER);
        else
            return TechRes.res_bitmap.GetData(icon_index);
    }

    public string Description()
    {
        if (unit_id != 0)
            return UnitRes[unit_id].name;

        if (firm_id != 0)
            return FirmRes[firm_id].Name;

        return String.Empty;
    }

    //-------- dynamic game vars --------//

    // each nation's the technology level of this unit,
    public int[] nation_tech_level_array = new int[GameConstants.MAX_NATION];

    // whether the nation is researching this technology, it stores the number of firms of each nation researching on this technology.
    public int[] nation_is_researching_array = new int[GameConstants.MAX_NATION];

    // the progresses of each nation researching this technology, when it reaches complex_level, the research is done.
    public double[] nation_research_progress_array = new double[GameConstants.MAX_NATION];

    public void set_nation_tech_level(int nationRecno, int techLevel)
    {
        nation_tech_level_array[nationRecno - 1] = techLevel;

        if (unit_id != 0)
            UnitRes[unit_id].set_nation_tech_level(nationRecno, techLevel);

        else if (firm_id != 0)
            FirmRes[firm_id].set_nation_tech_level(nationRecno, techLevel);

        //--- if the MAX level has been reached and there are still other firms researching this technology ---//

        if (techLevel == max_tech_level && is_nation_researching(nationRecno))
        {
            //---- stop other firms researching the same tech -----//

            foreach (Firm firm in FirmArray)
            {
                if (firm.FirmType == Firm.FIRM_RESEARCH && firm.NationId == nationRecno)
                {
                    FirmResearch firmResearch = (FirmResearch)firm;
                    if (firmResearch.TechId == tech_id)
                        firmResearch.TerminateResearch();
                }
            }
        }
    }

    public int get_nation_tech_level(int nationRecno)
    {
        return nation_tech_level_array[nationRecno - 1];
    }

    public void inc_nation_is_researching(int nationRecno)
    {
        nation_is_researching_array[nationRecno - 1]++;
    }

    public void dec_nation_is_researching(int nationRecno)
    {
        nation_is_researching_array[nationRecno - 1]--;
    }

    public bool is_nation_researching(int nationRecno)
    {
        return nation_is_researching_array[nationRecno - 1] > 0;
    }

    public bool is_parent_tech_invented(int nationRecno)
    {
        if (parent_unit_id != 0)
        {
            if (UnitRes[parent_unit_id].get_nation_tech_level(nationRecno) < parent_level)
                return false;
        }

        if (parent_firm_id != 0)
        {
            if (FirmRes[parent_firm_id].get_nation_tech_level(nationRecno) < parent_level)
                return false;
        }

        return true;
    }

    public bool can_research(int nationRecno)
    {
        return get_nation_tech_level(nationRecno) < max_tech_level && is_parent_tech_invented(nationRecno);
    }

    public bool progress(int nationRecno, double progressPoint)
    {
        nation_research_progress_array[nationRecno - 1] += progressPoint;

        if (nation_research_progress_array[nationRecno - 1] > 100.0)
        {
            set_nation_tech_level(nationRecno, nation_tech_level_array[nationRecno - 1] + 1);
            nation_research_progress_array[nationRecno - 1] = 0.0;

            return true;
        }

        return false;
    }

    public double get_progress(int nationRecno)
    {
        return nation_research_progress_array[nationRecno - 1];
    }
}

public class TechRes
{
    public const string TECH_DB = "TECH";
    public const string TECH_CLASS_DB = "TECHCLAS";

    public int total_tech_level; // the sum of research levels of all technology

    public TechClass[] TechClasses { get; set; }
    public TechInfo[] TechInfos { get; set; }

    public ResourceIdx res_bitmap;

    public GameSet GameSet { get; }

    public TechRes(GameSet gameSet)
    {
        GameSet = gameSet;

        //---------- init bitmap resource ---------//
        res_bitmap = new ResourceIdx($"{Sys.GameDataFolder}/Resource/I_TECH.RES");

        //------- load database information --------//
        LoadTechClass();
        LoadTechInfo();
    }

    public void init_nation_tech(int nationId)
    {
        foreach (var techInfo in TechInfos)
        {
            techInfo.set_nation_tech_level(nationId, 0);
        }
    }

    public void inc_all_tech_level(int nationId)
    {
        foreach (var techInfo in TechInfos)
        {
            int curTechLevel = techInfo.get_nation_tech_level(nationId);

            if (curTechLevel < techInfo.max_tech_level)
                techInfo.set_nation_tech_level(nationId, curTechLevel + 1);
        }
    }

    public TechInfo this[int techId] => TechInfos[techId - 1];

    private TechClass GetTechClass(int techClassId)
    {
        return TechClasses[techClassId - 1];
    }

    private void LoadTechClass()
    {
        Database dbTechClass = GameSet.OpenDb(TECH_CLASS_DB);
        TechClasses = new TechClass[dbTechClass.RecordCount];

        for (int i = 0; i < TechClasses.Length; i++)
        {
            TechClassRec techClassRec = new TechClassRec(dbTechClass, i + 1);
            TechClass techClass = new TechClass();
            TechClasses[i] = techClass;

            techClass.class_id = i + 1;
            techClass.icon_index = res_bitmap.GetIndex(Misc.ToString(techClassRec.icon_name));
        }
    }

    private void LoadTechInfo()
    {
        Database dbTech = GameSet.OpenDb(TECH_DB);
        TechInfos = new TechInfo[dbTech.RecordCount];

        int techClassId = 0;
        total_tech_level = 0;

        TechClass techClass = null;
        for (int i = 0; i < TechInfos.Length; i++)
        {
            TechRec techRec = new TechRec(dbTech, i + 1);
            TechInfo techInfo = new TechInfo();
            TechInfos[i] = techInfo;

            techInfo.tech_id = i + 1;
            techInfo.class_id = Misc.ToInt32(techRec.class_id);

            techInfo.max_tech_level = Misc.ToInt32(techRec.max_tech_level);
            techInfo.complex_level = Misc.ToInt32(techRec.complex_level);

            techInfo.unit_id = Misc.ToInt32(techRec.unit_id);
            techInfo.firm_id = Misc.ToInt32(techRec.firm_id);
            techInfo.parent_unit_id = Misc.ToInt32(techRec.parent_unit_id);
            techInfo.parent_firm_id = Misc.ToInt32(techRec.parent_firm_id);
            techInfo.parent_level = techRec.parent_level - '0';
            techInfo.icon_index = res_bitmap.GetIndex(Misc.ToString(techRec.icon_name));

            if (techClassId != techInfo.class_id)
            {
                techClass = GetTechClass(techInfo.class_id);
                techClassId = techInfo.class_id;

                techClass.first_tech_id = i + 1;
                techClass.tech_count = 1;
            }
            else
            {
                techClass.tech_count++;
            }

            total_tech_level += techInfo.max_tech_level;
        }
    }
}
