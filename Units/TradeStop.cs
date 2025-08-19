namespace TenKingdoms;

public class TradeStop
{
	public const int AUTO_PICK_UP = 0;
	public const int PICK_UP_RAW_FIRST = 1;
	public const int PICK_UP_RAW_LAST = PICK_UP_RAW_FIRST + GameConstants.MAX_RAW - 1;
	public const int PICK_UP_PRODUCT_FIRST = PICK_UP_RAW_LAST + 1;
	public const int PICK_UP_PRODUCT_LAST = PICK_UP_PRODUCT_FIRST + GameConstants.MAX_PRODUCT - 1;
	public const int MAX_PICK_UP_GOODS = PICK_UP_PRODUCT_LAST;
	public const int NO_PICK_UP = MAX_PICK_UP_GOODS + 1;
	public const int MAX_GOODS_SELECT_BUTTON = MAX_PICK_UP_GOODS + 2;

	public int firm_recno; // firm recno of the station
	public int firm_loc_x1; //-******* used temporarily
	public int firm_loc_y1;
	public int pick_up_type; // auto, selective, none
	public bool[] pick_up_array = new bool[MAX_PICK_UP_GOODS]; // useful for selective mode

	protected FirmArray FirmArray => Sys.Instance.FirmArray;
	
	public void pick_up_set_auto()
	{
		for (int i = 0; i < pick_up_array.Length; i++)
			pick_up_array[i] = false;

		pick_up_type = AUTO_PICK_UP;
	}

	public void pick_up_set_none()
	{
		for (int i = 0; i < pick_up_array.Length; i++)
			pick_up_array[i] = false;

		pick_up_type = NO_PICK_UP;
	}

	public void pick_up_toggle(int pos)
	{
		int index = pos - 1;
		if (pick_up_array[index])
		{
			pick_up_array[index] = false;

			int firmId = FirmArray[firm_recno].FirmType;
			if (firmId == Firm.FIRM_MARKET || firmId == Firm.FIRM_HARBOR)
			{
				bool allZero = true;
				for (int i = 0; i < MAX_PICK_UP_GOODS; ++i)
				{
					if (pick_up_array[i])
					{
						allZero = false;
						pick_up_type = i + 1;
						break;
					}
				}

				if (allZero)
					pick_up_type = NO_PICK_UP;
			}
			else
				pick_up_type = NO_PICK_UP;
		}
		else
		{
			pick_up_array[index] = true;
			pick_up_type = pos; // that means selective
		}
	}
}

public class CaravanStop : TradeStop
{
	public int firm_id;

	public int update_pick_up(bool[] enableFlag = null)
	{
		bool[] dummyBuffer = new bool[MAX_GOODS_SELECT_BUTTON];
		if (enableFlag == null)
			enableFlag = dummyBuffer;

		for (int i = 0; i < enableFlag.Length; i++)
			enableFlag[i] = false;

		Firm firm = FirmArray[firm_recno];
		int goodsNum = 0; // represent the number of cargo displayed in the menu for this stop
		int firstGoodsId = 0;
		int id;

		switch (firm.FirmType)
		{
			case Firm.FIRM_MINE:
				id = ((FirmMine)firm).RawId + PICK_UP_RAW_FIRST - 1;
				if (id != 0)
				{
					goodsNum++;
					enableFlag[id] = true;

					if (!pick_up_array[id - 1])
						pick_up_set_none(); // nothing can be taken if no cargo is matched
				}

				break;

			case Firm.FIRM_FACTORY:
				id = ((FirmFactory)firm).ProductRawId + PICK_UP_PRODUCT_FIRST - 1;
				if (id != 0)
				{
					goodsNum++;
					enableFlag[id] = true;

					if (!pick_up_array[id - 1])
						pick_up_set_none(); // nothing can be taken if no cargo is matched
				}

				break;

			case Firm.FIRM_MARKET:
				for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
				{
					MarketGoods marketGoods = ((FirmMarket)firm).market_goods_array[i];
					if ((id = marketGoods.raw_id) != 0) // 1-3
					{
						goodsNum++;
						enableFlag[id] = true;
						if (firstGoodsId == 0)
							firstGoodsId = id;
					}
					else if ((id = marketGoods.product_raw_id) != 0) // 1-3
					{
						id += GameConstants.MAX_RAW; // 4-6
						goodsNum++;
						enableFlag[id] = true;
						if (firstGoodsId == 0)
							firstGoodsId = id;
					}
				}

				for (int i = 0; i < MAX_PICK_UP_GOODS; i++)
				{
					if (pick_up_array[i])
					{
						if (!enableFlag[i + 1])
							pick_up_array[i] = false;
					}
				}

				break;
		}

		if (goodsNum == 0 && pick_up_type != NO_PICK_UP)
			pick_up_set_none();

		if (goodsNum == 1 && pick_up_type == AUTO_PICK_UP)
		{
			pick_up_type = firstGoodsId; // change to selective
			pick_up_array[pick_up_type - 1] = true;
		}

		return goodsNum;
	}
}

public class ShipStop : TradeStop
{
	public int update_pick_up(bool[] enableFlag = null)
	{
		bool[] dummyBuffer = new bool[MAX_GOODS_SELECT_BUTTON];
		if (enableFlag == null)
			enableFlag = dummyBuffer;

		for (int i = 0; i < enableFlag.Length; i++)
			enableFlag[i] = false;

		Firm harbor = FirmArray[firm_recno];
		int goodsNum = 0; // represent the number of cargo displayed in the menu for this stop
		int firstGoodsId = 0;
		int id;

		for (int i = harbor.LinkedFirms.Count - 1; i >= 0; --i)
		{
			Firm firm = FirmArray[harbor.LinkedFirms[i]];

			switch (firm.FirmType)
			{
				case Firm.FIRM_MINE:
					if ((id = ((FirmMine)firm).RawId) != 0) // 1-3
					{
						goodsNum++;
						enableFlag[id] = true;
						if (firstGoodsId == 0)
							firstGoodsId = id;
					}

					break;

				case Firm.FIRM_FACTORY:
					if ((id = ((FirmFactory)firm).ProductRawId) != 0) // 1-3
					{
						id += GameConstants.MAX_RAW; // 4-6
						goodsNum++;
						enableFlag[id] = true;
						if (firstGoodsId == 0)
							firstGoodsId = id;
					}

					break;

				case Firm.FIRM_MARKET:
					for (int j = 1; j <= GameConstants.MAX_MARKET_GOODS; j++)
					{
						MarketGoods marketGoods = ((FirmMarket)firm).market_goods_array[i];
						if ((id = marketGoods.raw_id) != 0) // 1-3
						{
							goodsNum++;
							enableFlag[id] = true;
							if (firstGoodsId == 0)
								firstGoodsId = id;
						}
						else if ((id = marketGoods.product_raw_id) != 0) // 1-3
						{
							id += GameConstants.MAX_RAW; // 4-6
							goodsNum++;
							enableFlag[id] = true;
							if (firstGoodsId == 0)
								firstGoodsId = id;
						}
					}

					break;
			}
		}

		for (int i = 0; i < MAX_PICK_UP_GOODS; ++i)
		{
			if (pick_up_array[i])
			{
				if (!enableFlag[i + 1])
					pick_up_array[i] = false;
			}
		}

		if (goodsNum == 0 && pick_up_type != NO_PICK_UP)
			pick_up_set_none();

		if (goodsNum == 1 && pick_up_type == AUTO_PICK_UP)
		{
			pick_up_type = firstGoodsId; // change to selective
			pick_up_array[pick_up_type - 1] = true;
		}

		return goodsNum;
	}
}
