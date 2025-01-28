using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class RawRec
{
    public const int NAME_LEN = 12;
    public const int TERA_TYPE_LEN = 1;

    public char[] name = new char[NAME_LEN];
    public char[] tera_type = new char[TERA_TYPE_LEN];

    public RawRec(byte[] data)
    {
        int dataIndex = 0;
        for (int i = 0; i < name.Length; i++, dataIndex++)
            name[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < tera_type.Length; i++, dataIndex++)
            tera_type[i] = Convert.ToChar(data[dataIndex]);
    }
}

public class RawInfo
{
    public int	raw_id;
    public string name;
    public int  tera_type;

    public List<int> raw_supply_firm_array = new List<int>();
    public List<int> product_supply_firm_array = new List<int>();

    public RawInfo()
    {
    }

    public void add_raw_supply_firm(int firmRecno)
    {
        raw_supply_firm_array.Add(firmRecno);
    }

    public void add_product_supply_firm(int firmRecno)
    {
        product_supply_firm_array.Add(firmRecno);
    }

    public int get_raw_supply_firm(int index)
    {
        return raw_supply_firm_array[index];
    }

    public int get_product_supply_firm(int index)
    {
        return product_supply_firm_array[index];
    }
}

public class RawRes
{
    public const string RAW_DB = "RAW";

    public RawInfo[] raw_info_array;

    public Resource res_icon;

    public GameSet GameSet { get; }
    protected Info Info => Sys.Instance.Info;
    protected FirmArray FirmArray => Sys.Instance.FirmArray;

    public RawRes(GameSet gameSet)
    {
        GameSet = gameSet;

        res_icon = new Resource($"{Sys.GameDataFolder}/Resource/I_RAW.RES");

        LoadAllInfo();
    }

    public void next_day()
    {
        if (Info.TotalDays % 15 == 0)
            update_supply_firm();
    }

    public void update_supply_firm()
    {
        //----- reset the supply array of each raw and product ----//

        for (int i = 0; i < GameConstants.MAX_RAW; i++)
        {
            raw_info_array[i].raw_supply_firm_array.Clear();
            raw_info_array[i].product_supply_firm_array.Clear();
        }

        //---- locate for suppliers that supply the products needed ----//

        foreach (Firm firm in FirmArray)
        {
            //-------- factory as a potential supplier ------//

            if (firm.firm_id == Firm.FIRM_FACTORY)
            {
                FirmFactory firmFactory = (FirmFactory)firm;

                if (firmFactory.product_raw_id != 0 && firmFactory.stock_qty > firmFactory.max_stock_qty / 5.0)
                {
                    this[firmFactory.product_raw_id].add_product_supply_firm(firm.firm_recno);
                }
            }

            //-------- mine as a potential supplier ------//

            if (firm.firm_id == Firm.FIRM_MINE)
            {
                FirmMine firmMine = (FirmMine)firm;

                if (firmMine.raw_id != 0 && firmMine.stock_qty > firmMine.max_stock_qty / 5.0)
                {
                    this[firmMine.raw_id].add_raw_supply_firm(firm.firm_recno);
                }
            }

            //-------- market place as a potential supplier ------//

            else if (firm.firm_id == Firm.FIRM_MARKET)
            {
                FirmMarket firmMarket = (FirmMarket)firm;

                for (int i = 0; i < firmMarket.market_goods_array.Length; i++)
                {
                    MarketGoods marketGoods = firmMarket.market_goods_array[i];
                    if (marketGoods.stock_qty > GameConstants.MAX_MARKET_STOCK / 5.0)
                    {
                        if (marketGoods.product_raw_id != 0)
                            this[marketGoods.product_raw_id].add_product_supply_firm(firm.firm_recno);

                        else if (marketGoods.raw_id != 0)
                            this[marketGoods.raw_id].add_raw_supply_firm(firm.firm_recno);
                    }
                }
            }
        }
    }

    private string[] product_name_str = { "Clay Products", "Copper Products", "Iron Products" };

    public string product_name(int rawId)
    {
        return product_name_str[rawId - 1];
    }

    public byte[] large_product_icon(int rawId)
    {
        return res_icon.Read(rawId);
    }

    public byte[] small_product_icon(int rawId)
    {
        return res_icon.Read(GameConstants.MAX_RAW + rawId);
    }

    public byte[] large_raw_icon(int rawId)
    {
        return res_icon.Read(GameConstants.MAX_RAW * 2 + rawId);
    }

    public byte[] small_raw_icon(int rawId)
    {
        return res_icon.Read(GameConstants.MAX_RAW * 3 + rawId);
    }

    public RawInfo this[int rawId] => raw_info_array[rawId - 1];

    private void LoadAllInfo()
    {
        Database dbRaw = GameSet.OpenDb(RAW_DB);

        raw_info_array = new RawInfo[dbRaw.RecordCount];

        for (int i = 0; i < raw_info_array.Length; i++)
        {
            RawRec rawRec = new RawRec(dbRaw.Read(i + 1));
            RawInfo rawInfo = new RawInfo();
            raw_info_array[i] = rawInfo;

            rawInfo.name = Misc.ToString(rawRec.name);
            rawInfo.raw_id = i + 1;
            rawInfo.tera_type = Misc.ToInt32(rawRec.tera_type);
        }
    }
}