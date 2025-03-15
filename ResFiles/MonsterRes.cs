using System;
using System.Collections.Generic;
using System.Linq;

namespace TenKingdoms;

public class MonsterRec
{
    public const int UNIT_CODE_LEN = 8;
    public const int RANK_LEN = 8;
    public const int NAME_LEN = 20;
    public const int FIRM_BUILD_CODE_LEN = 8;
    public const int UNIT_ID_LEN = 3;

    public char[] unit_code = new char[UNIT_CODE_LEN];
    public char[] name = new char[NAME_LEN];
    public char level; // the level of the 1-9 monster, the higher the level is, the more powerful the monster is.
    public char[] firm_build_code = new char[FIRM_BUILD_CODE_LEN];
    public char[] unit_id = new char[UNIT_ID_LEN];

    public MonsterRec(byte[] data)
    {
        int dataIndex = 0;
        for (int i = 0; i < unit_code.Length; i++, dataIndex++)
            unit_code[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < name.Length; i++, dataIndex++)
            name[i] = Convert.ToChar(data[dataIndex]);
        
        level = Convert.ToChar(data[dataIndex]);
        dataIndex++;
        
        for (int i = 0; i < firm_build_code.Length; i++, dataIndex++)
            firm_build_code[i] = Convert.ToChar(data[dataIndex]);
        
        for (int i = 0; i < unit_id.Length; i++, dataIndex++)
            unit_id[i] = Convert.ToChar(data[dataIndex]);
    }
}

public class MonsterInfo
{
    public int monster_id;

    public string name;
    public int unit_id;
    public int level;
    public string firm_build_code;

    private FirmRes FirmRes => Sys.Instance.FirmRes;
    private World World => Sys.Instance.World;
    private FirmArray FirmArray => Sys.Instance.FirmArray;
    private TownArray TownArray => Sys.Instance.TownArray;

    public int create_firm_monster()
    {
        if (String.IsNullOrEmpty(firm_build_code)) // this monster does not have a home
            return 0;

        //------- locate a home place for the monster group ------//

        FirmInfo firmInfo = FirmRes[Firm.FIRM_MONSTER];

        int xLoc = 0, yLoc = 0;
        int teraMask = UnitRes.mobile_type_to_mask(UnitConstants.UNIT_LAND);

        // leave at least one location space around the building
        if (!World.locate_space_random(ref xLoc, ref yLoc, GameConstants.MapSize - 1,
                GameConstants.MapSize - 1, firmInfo.loc_width + 2, firmInfo.loc_height + 2,
                GameConstants.MapSize * GameConstants.MapSize, 0, true, teraMask))
        {
            return 0;
        }

        //------- don't place it too close to any towns or firms ------//

        foreach (Town town in TownArray)
        {
            if (Misc.rects_distance(xLoc, yLoc, xLoc + firmInfo.loc_width, yLoc + firmInfo.loc_height,
                    town.LocX1, town.LocY1, town.LocX2, town.LocY2) <
                GameConstants.MIN_MONSTER_CIVILIAN_DISTANCE)
            {
                return 0;
            }
        }

        foreach (Firm firm in FirmArray)
        {
            if (Misc.points_distance(xLoc, yLoc, firm.center_x, firm.center_y) <
                GameConstants.MIN_MONSTER_CIVILIAN_DISTANCE)
            {
                return 0;
            }
        }

        return build_firm_monster(xLoc + 1, yLoc + 1);
    }

    public int build_firm_monster(int xLoc, int yLoc, int fullHitPoints = 1)
    {
        //----- if this monster has a home building, create it first -----//

        int firmRecno = FirmArray.BuildFirm(xLoc, yLoc, 0, Firm.FIRM_MONSTER, firm_build_code);

        if (firmRecno == 0)
            return 0;

        FirmMonster firmMonster = (FirmMonster)FirmArray[firmRecno];

        if (fullHitPoints != 0)
            firmMonster.complete_construction();
        else
        {
            firmMonster.hit_points = 0.1;
            firmMonster.under_construction = true;
        }

        firmMonster.set_king(monster_id, 100);

        //-------- create monster generals ---------//

        int generalCount = Misc.Random(2) + 1; // 1 to 3 generals in a monster firm

        if (Misc.Random(5) == 0) // 20% chance of having 3 generals.
            generalCount = 3;

        for (int i = 0; i < generalCount; i++)
            firmMonster.recruit_general();

        firmMonster.monster_id = monster_id;

        return firmRecno;
    }
}

public class MonsterRes
{
    public const int MAX_ACTIVE_MONSTER = 13; // No. of monster type in each game
    public const string MONSTER_DB = "MONSTER";

    public MonsterInfo[] monster_info_array;

    public int[] active_monster_array = new int[MAX_ACTIVE_MONSTER];

    public ResourceDb res_bitmap;

    private readonly List<byte[]> _goldCoinBitmaps = new List<byte[]>();
    public int goldCoinWidth;
    public int goldCoinHeight;
    private readonly List<IntPtr> _goldCoinTextures = new List<nint>();
    
    public IntPtr GetGoldCoinTexture(Graphics graphics, int value)
    {
        if (_goldCoinTextures.Count == 0)
        {
            for (int i = 0; i < _goldCoinBitmaps.Count; i++)
            {
                byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(_goldCoinBitmaps[i], goldCoinWidth, goldCoinHeight);
                IntPtr texture = graphics.CreateTextureFromBmp(decompressedBitmap, goldCoinWidth, goldCoinHeight);
                _goldCoinTextures.Add(texture);
            }
        }
        
        return _goldCoinTextures[_goldCoinTextures.Count % value];
    }

    private FirmArray FirmArray => Sys.Instance.FirmArray;

    public GameSet GameSet { get; }
    public UnitRes UnitRes { get; }

    public MonsterRes(GameSet gameSet, UnitRes unitRes)
    {
        GameSet = gameSet;
        UnitRes = unitRes;

        LoadMonsterInfo();
        LoadGoldCoins();
    }

    public void init_active_monster()
    {
        int monsterId;
        int activeCount = 0;

        //----- reset the active_monster_array ----//

        while (true)
        {
            monsterId = Misc.Random(monster_info_array.Length) + 1;

            //--- check if this monster id. is already in the active monster array ---//

            int i;
            for (i = 0; i < activeCount; i++)
            {
                if (active_monster_array[i] == monsterId)
                    break;
            }

            //------- if it's a new one --------//

            if (i == activeCount)
            {
                active_monster_array[activeCount] = monsterId;

                if (++activeCount == MAX_ACTIVE_MONSTER)
                    return;
            }
        }
    }

    public void generate(int generateCount)
    {
        for (int i = 0; i < generateCount; i++)
        {
            int monsterId = active_monster_array[Misc.Random(MAX_ACTIVE_MONSTER)];
            this[monsterId].create_firm_monster();
        }
    }

    public void stop_attack_nation(int nationRecno)
    {
        foreach (Firm firm in FirmArray)
        {
            if (firm.firm_id != Firm.FIRM_MONSTER)
                continue;

            FirmMonster firmMonster = (FirmMonster)firm;

            firmMonster.reset_hostile_nation(nationRecno);
        }
    }

    public MonsterInfo this[int monsterId] => monster_info_array[monsterId - 1];

    public MonsterInfo get_monster_by_unit_id(int unitId)
    {
        for (int i = 0; i < monster_info_array.Length; i++)
        {
            MonsterInfo monsterInfo = monster_info_array[i];
            if (monsterInfo.unit_id == unitId)
                return monsterInfo;
        }

        return null;
    }

    private void LoadMonsterInfo()
    {
        Database dbMonster = GameSet.OpenDb(MONSTER_DB);

        monster_info_array = new MonsterInfo[dbMonster.RecordCount];

        //------ read in monster information array -------//

        for (int i = 0; i < monster_info_array.Length; i++)
        {
            MonsterRec monsterRec = new MonsterRec(dbMonster.Read(i + 1));
            MonsterInfo monsterInfo = new MonsterInfo();
            monster_info_array[i] = monsterInfo;

            monsterInfo.monster_id = i + 1;
            monsterInfo.name = Misc.ToString(monsterRec.name);
            monsterInfo.unit_id = Misc.ToInt32(monsterRec.unit_id);
            monsterInfo.level = monsterRec.level - '0';
            monsterInfo.firm_build_code = Misc.ToString(monsterRec.firm_build_code);

            //---- set the monster_id in UnitInfo ----//

            UnitRes[monsterInfo.unit_id].is_monster = 1;
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
                goldCoinWidth = BitConverter.ToInt16(coinsData, 0);
                goldCoinHeight = BitConverter.ToInt16(coinsData, 2);
            }

            _goldCoinBitmaps.Add(coinsData.Skip(4).ToArray());
        }
    }
}