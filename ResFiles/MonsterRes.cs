using System;
using System.Collections.Generic;
using System.Linq;

namespace TenKingdoms;

public class MonsterRec
{
    private const int UNIT_CODE_LEN = 8;
    private const int RANK_LEN = 8;
    private const int NAME_LEN = 20;
    private const int FIRM_BUILD_CODE_LEN = 8;
    private const int UNIT_ID_LEN = 3;

    public char[] unit_code = new char[UNIT_CODE_LEN];
    public char[] name = new char[NAME_LEN];
    public char level; // the level of the 1-9 monster, the higher the level is, the more powerful the monster is.
    public char[] firm_build_code = new char[FIRM_BUILD_CODE_LEN];
    public char[] unit_id = new char[UNIT_ID_LEN];

    public MonsterRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < unit_code.Length; i++, dataIndex++)
            unit_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < name.Length; i++, dataIndex++)
            name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        level = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        dataIndex++;
        
        for (int i = 0; i < firm_build_code.Length; i++, dataIndex++)
            firm_build_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        
        for (int i = 0; i < unit_id.Length; i++, dataIndex++)
            unit_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
    }
}

public class MonsterInfo
{
    public int MonsterId { get; set; }
    public string Name { get; set; }
    public int UnitType { get; set; }
    public int Level { get; set; }
    public string FirmBuildCode { get; set; }
}

public class MonsterRes
{
    public MonsterInfo[] MonsterInfos { get; private set; }
    public int GoldCoinWidth { get; private set; }
    public int GoldCoinHeight { get; private set; }
    private readonly List<byte[]> _goldCoinBitmaps = new List<byte[]>();
    private readonly List<IntPtr> _goldCoinTextures = new List<nint>();
    
    public IntPtr GetGoldCoinTexture(Graphics graphics, int value)
    {
        if (_goldCoinTextures.Count == 0)
        {
            for (int i = 0; i < _goldCoinBitmaps.Count; i++)
            {
                byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(_goldCoinBitmaps[i], GoldCoinWidth, GoldCoinHeight);
                IntPtr texture = graphics.CreateTextureFromBmp(decompressedBitmap, GoldCoinWidth, GoldCoinHeight);
                _goldCoinTextures.Add(texture);
            }
        }
        
        return _goldCoinTextures[value % _goldCoinTextures.Count];
    }

    public GameSet GameSet { get; }
    public UnitRes UnitRes { get; }

    public MonsterRes(GameSet gameSet, UnitRes unitRes)
    {
        GameSet = gameSet;
        UnitRes = unitRes;

        LoadMonsterInfo();
        LoadGoldCoins();
    }

    public MonsterInfo this[int monsterId] => MonsterInfos[monsterId - 1];

    private void LoadMonsterInfo()
    {
        Database dbMonster = GameSet.OpenDb("MONSTER");
        MonsterInfos = new MonsterInfo[dbMonster.RecordCount];

        for (int i = 0; i < MonsterInfos.Length; i++)
        {
            MonsterRec monsterRec = new MonsterRec(dbMonster, i + 1);
            MonsterInfo monsterInfo = new MonsterInfo();
            MonsterInfos[i] = monsterInfo;

            monsterInfo.MonsterId = i + 1;
            monsterInfo.Name = Misc.ToString(monsterRec.name);
            monsterInfo.UnitType = Misc.ToInt32(monsterRec.unit_id);
            monsterInfo.Level = monsterRec.level - '0';
            monsterInfo.FirmBuildCode = Misc.ToString(monsterRec.firm_build_code);
        }
    }

    private void LoadGoldCoins()
    {
        const int MAX_COINS_TYPE = 8;
        ResourceIdx images = new ResourceIdx($"{Sys.GameDataFolder}/Resource/I_SPICT.RES");
        for (int i = 0; i < MAX_COINS_TYPE; i++)
        {
            byte[] coinsData = images.Read("COINS-" + (i + 1).ToString());
            if (i == 0)
            {
                GoldCoinWidth = BitConverter.ToInt16(coinsData, 0);
                GoldCoinHeight = BitConverter.ToInt16(coinsData, 2);
            }

            _goldCoinBitmaps.Add(coinsData.Skip(4).ToArray());
        }
    }
}