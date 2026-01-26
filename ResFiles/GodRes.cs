using System;

namespace TenKingdoms;

public enum GodType
{
    GOD_MAYA,
    GOD_GREEK,
    GOD_VIKING,
    GOD_PERSIAN,
    GOD_CHINESE,
    GOD_JAPANESE,
    GOD_EGYPTIAN,
    GOD_INDIAN,
    GOD_ZULU
}

public class GodRec
{
    public const int RACE_CODE_LEN = 8;
    public const int UNIT_CODE_LEN = 8;
    public const int PRAY_POINTS_LEN = 3;
    public const int CAST_POWER_RANGE_LEN = 3;
    public const int RACE_ID_LEN = 3;
    public const int UNIT_ID_LEN = 3;

    public char[] race = new char[RACE_CODE_LEN];
    public char[] unit = new char[UNIT_CODE_LEN];
    public char[] exist_pray_points = new char[PRAY_POINTS_LEN];
    public char[] power_pray_points = new char[PRAY_POINTS_LEN];
    public byte can_cast_power; // whether this god creature can cast power or not
    public char[] cast_power_range = new char[CAST_POWER_RANGE_LEN];

    public char[] race_id = new char[RACE_ID_LEN];
    public char[] unit_id = new char[UNIT_ID_LEN];

    public GodRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < race.Length; i++, dataIndex++)
            race[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
	    
        for (int i = 0; i < unit.Length; i++, dataIndex++)
            unit[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
	    
        for (int i = 0; i < exist_pray_points.Length; i++, dataIndex++)
            exist_pray_points[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
	    
        for (int i = 0; i < power_pray_points.Length; i++, dataIndex++)
            power_pray_points[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        can_cast_power = db.ReadByte(recNo, dataIndex);
        dataIndex++;
        
        for (int i = 0; i < cast_power_range.Length; i++, dataIndex++)
            cast_power_range[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < race_id.Length; i++, dataIndex++)
            race_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < unit_id.Length; i++, dataIndex++)
            unit_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
    }
}

public class GodInfo
{
    public int god_id;
    public int race_id;
    public int unit_id;
    public int exist_pray_points; // pray points consumption for the god to exist for 100 frames
    public int power_pray_points; // pray points consumption for each casting of its power
    public bool can_cast_power; // whether this god creature can cast power or not
    public int cast_power_range; // location range of casting power

    //------ game vars ------//

    public bool[] nation_know_array = new bool[GameConstants.MAX_NATION];

    private SpriteRes SpriteRes => Sys.Instance.SpriteRes;
    private UnitRes UnitRes => Sys.Instance.UnitRes;
    private World World => Sys.Instance.World;
    private NationArray NationArray => Sys.Instance.NationArray;
    private UnitArray UnitArray => Sys.Instance.UnitArray;
    private FirmArray FirmArray => Sys.Instance.FirmArray;

    public bool is_nation_know(int nationRecno)
    {
        //TODO check duplicate with NationArray[NationId].KnowBases[RaceId - 1]
        return nation_know_array[nationRecno - 1];
    }

    public int invoke(int firmRecno, int xLoc, int yLoc)
    {
        FirmBase firmBase = (FirmBase)FirmArray[firmRecno];

        //------- create the god unit --------//

        SpriteInfo spriteInfo = SpriteRes[UnitRes[unit_id].SpriteId];

        if (!World.LocateSpace(ref xLoc, ref yLoc, xLoc, yLoc,
                spriteInfo.LocWidth, spriteInfo.LocHeight, UnitConstants.UNIT_AIR))
            return 0;

        //---------- add the god unit now -----------//

        Unit unit = UnitArray.AddUnit(unit_id, firmBase.NationId, Unit.RANK_SOLDIER, 0, xLoc, yLoc);

        //------- set vars of the God unit ----------//

        UnitGod unitGod = (UnitGod)unit;

        unitGod.GodId = god_id;
        unitGod.BaseFirmId = firmRecno;
        unitGod.HitPoints = Convert.ToInt32(firmBase.PrayPoints);

        return unitGod.SpriteId;
    }

    public void enable_know(int nationRecno)
    {
        nation_know_array[nationRecno - 1] = true;
        NationArray[nationRecno].KnowBases[race_id - 1] = 1; // enable the nation to build the fortress of power
    }

    public void disable_know(int nationRecno)
    {
        nation_know_array[nationRecno - 1] = false;
        NationArray[nationRecno].KnowBases[race_id - 1] = 0; // enable the nation to build the fortress of power
    }
}

public class GodRes
{
    public const int GOD_NORMAN = 1;
    public const int GOD_MAYA = 2;
    public const int GOD_GREEK = 3;
    public const int GOD_VIKING = 4;
    public const int GOD_PERSIAN = 5;
    public const int GOD_CHINESE = 6;
    public const int GOD_JAPANESE = 7;
    public const int GOD_EGYPTIAN = 8;
    public const int GOD_INDIAN = 9;
    public const int GOD_ZULU = 10;
    public const int MAX_GOD = 10;

    public GodInfo[] god_info_array;

    public GameSet GameSet { get; }

    public GodRes(GameSet gameSet)
    {
        GameSet = gameSet;
        LoadGodInfo();
    }

    public GodInfo this[int godId] => god_info_array[godId - 1];

    private void LoadGodInfo()
    {
        Database dbGod = GameSet.OpenDb("GOD");

        god_info_array = new GodInfo[dbGod.RecordCount];

        for (int i = 0; i < god_info_array.Length; i++)
        {
            GodRec godRec = new GodRec(dbGod, i + 1);
            GodInfo godInfo = new GodInfo();
            god_info_array[i] = godInfo;

            godInfo.god_id = i + 1;

            godInfo.race_id = Misc.ToInt32(godRec.race_id);
            godInfo.unit_id = Misc.ToInt32(godRec.unit_id);

            godInfo.exist_pray_points = Misc.ToInt32(godRec.exist_pray_points);
            godInfo.power_pray_points = Misc.ToInt32(godRec.power_pray_points);

            godInfo.can_cast_power = (godRec.can_cast_power == '1');
            godInfo.cast_power_range = Misc.ToInt32(godRec.cast_power_range);
        }
    }

    public void init_nation_know(int nationRecno)
    {
        for (int i = 1; i <= god_info_array.Length; i++)
        {
            this[i].disable_know(nationRecno);
        }
    }

    public void enable_know_all(int nationRecno)
    {
        for (int i = 1; i <= god_info_array.Length; i++)
        {
            this[i].enable_know(nationRecno);
        }
    }
}