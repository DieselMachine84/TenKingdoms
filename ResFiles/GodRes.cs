using System;

namespace TenKingdoms;

public class GodRec
{
    private const int RACE_CODE_LEN = 8;
    private const int UNIT_CODE_LEN = 8;
    private const int PRAY_POINTS_LEN = 3;
    private const int CAST_POWER_RANGE_LEN = 3;
    private const int RACE_ID_LEN = 3;
    private const int UNIT_ID_LEN = 3;

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
    public int GodId { get; set; }
    public int RaceId { get; set; }
    public int UnitType { get; set; }
    public int ExistPrayPoints { get; set; } // pray points consumption for the god to exist for 100 frames
    public int PowerPrayPoints { get; set; } // pray points consumption for each casting of its power
    public bool CanCastPower { get; set; } // whether this god creature can cast power or not
    public int CastPowerRange { get; set; } // location range of casting power
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

    public GodInfo[] GodInfos { get; private set; }

    public GameSet GameSet { get; }

    public GodRes(GameSet gameSet)
    {
        GameSet = gameSet;
        LoadGodInfo();
    }

    public GodInfo this[int godId] => GodInfos[godId - 1];

    private void LoadGodInfo()
    {
        Database dbGod = GameSet.OpenDb("GOD");

        GodInfos = new GodInfo[dbGod.RecordCount];

        for (int i = 0; i < GodInfos.Length; i++)
        {
            GodRec godRec = new GodRec(dbGod, i + 1);
            GodInfo godInfo = new GodInfo();
            GodInfos[i] = godInfo;

            godInfo.GodId = i + 1;

            godInfo.RaceId = Misc.ToInt32(godRec.race_id);
            godInfo.UnitType = Misc.ToInt32(godRec.unit_id);

            godInfo.ExistPrayPoints = Misc.ToInt32(godRec.exist_pray_points);
            godInfo.PowerPrayPoints = Misc.ToInt32(godRec.power_pray_points);

            godInfo.CanCastPower = (godRec.can_cast_power == '1');
            godInfo.CastPowerRange = Misc.ToInt32(godRec.cast_power_range);
        }
    }
}