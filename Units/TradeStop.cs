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

	public int FirmId { get; set; } // firm recno of the station
	public int FirmLocX1 { get; set; }
	public int FirmLocY1 { get; set; }
	public int PickUpType { get; protected set; }
	public bool[] PickUpEnabled { get; } = new bool[MAX_PICK_UP_GOODS]; // useful for selective mode

	protected FirmArray FirmArray => Sys.Instance.FirmArray;
	
	public void PickUpSetAuto()
	{
		for (int i = 0; i < PickUpEnabled.Length; i++)
			PickUpEnabled[i] = false;

		PickUpType = AUTO_PICK_UP;
	}

	public void PickUpSetNone()
	{
		for (int i = 0; i < PickUpEnabled.Length; i++)
			PickUpEnabled[i] = false;

		PickUpType = NO_PICK_UP;
	}

	public void PickUpToggle(int pos)
	{
		int index = pos - 1;
		if (PickUpEnabled[index])
		{
			PickUpEnabled[index] = false;

			int firmType = FirmArray[FirmId].FirmType;
			if (firmType == Firm.FIRM_MARKET || firmType == Firm.FIRM_HARBOR)
			{
				bool allZero = true;
				for (int i = 0; i < MAX_PICK_UP_GOODS; ++i)
				{
					if (PickUpEnabled[i])
					{
						allZero = false;
						PickUpType = i + 1;
						break;
					}
				}

				if (allZero)
					PickUpType = NO_PICK_UP;
			}
			else
			{
				PickUpType = NO_PICK_UP;
			}
		}
		else
		{
			PickUpEnabled[index] = true;
			PickUpType = pos; // that means selective
		}
	}
}

public class CaravanStop : TradeStop
{
	public int FirmType { get; set; }

	public void UpdatePickUp()
	{
		bool[] enableFlag = new bool[MAX_GOODS_SELECT_BUTTON];

		Firm firm = FirmArray[FirmId];
		int goodsNum = 0; // represent the number of cargo displayed in the menu for this stop
		int firstGoodsId = 0;
		int id;

		switch (firm.FirmType)
		{
			case Firm.FIRM_MINE:
				id = ((FirmMine)firm).RawId;
				if (id != 0)
				{
					goodsNum++;
					enableFlag[id] = true;

					if (!PickUpEnabled[id - 1])
						PickUpSetNone(); // nothing can be taken if no cargo is matched
				}

				break;

			case Firm.FIRM_FACTORY:
				id = ((FirmFactory)firm).ProductId;
				if (id != 0)
				{
					id += GameConstants.MAX_RAW; // 4-6
					goodsNum++;
					enableFlag[id] = true;

					if (!PickUpEnabled[id - 1])
						PickUpSetNone(); // nothing can be taken if no cargo is matched
				}

				break;

			case Firm.FIRM_MARKET:
				for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
				{
					MarketGoods marketGoods = ((FirmMarket)firm).MarketGoods[i];
					if (marketGoods.RawId != 0) // 1-3
					{
						id = marketGoods.RawId;
						goodsNum++;
						enableFlag[id] = true;
						if (firstGoodsId == 0)
							firstGoodsId = id;
					}
					
					if (marketGoods.ProductId != 0) // 1-3
					{
						id = marketGoods.ProductId;
						id += GameConstants.MAX_RAW; // 4-6
						goodsNum++;
						enableFlag[id] = true;
						if (firstGoodsId == 0)
							firstGoodsId = id;
					}
				}

				break;
		}

		for (int i = 0; i < MAX_PICK_UP_GOODS; i++)
		{
			if (!enableFlag[i + 1])
				PickUpEnabled[i] = false;
		}
		
		if (goodsNum == 0 && PickUpType != NO_PICK_UP)
			PickUpSetNone();

		if (goodsNum == 1 && PickUpType == AUTO_PICK_UP)
		{
			PickUpType = firstGoodsId; // change to selective
			PickUpEnabled[PickUpType - 1] = true;
		}
	}
}

public class ShipStop : TradeStop
{
	public void UpdatePickUp()
	{
		bool[] enableFlag = new bool[MAX_GOODS_SELECT_BUTTON];

		Firm harbor = FirmArray[FirmId];
		int goodsNum = 0; // represent the number of cargo displayed in the menu for this stop
		int firstGoodsId = 0;

		for (int i = 0; i < harbor.LinkedFirms.Count; i++)
		{
			Firm firm = FirmArray[harbor.LinkedFirms[i]];

			int id;
			switch (firm.FirmType)
			{
				case Firm.FIRM_MINE:
					id = ((FirmMine)firm).RawId;
					if (id != 0) // 1-3
					{
						goodsNum++;
						enableFlag[id] = true;
						if (firstGoodsId == 0)
							firstGoodsId = id;
					}

					break;

				case Firm.FIRM_FACTORY:
					id = ((FirmFactory)firm).ProductId;
					if (id != 0)
					{
						id += GameConstants.MAX_RAW; // 4-6
						goodsNum++;
						enableFlag[id] = true;
						if (firstGoodsId == 0)
							firstGoodsId = id;
					}

					break;

				case Firm.FIRM_MARKET:
					for (int j = 0; j < GameConstants.MAX_MARKET_GOODS; j++)
					{
						MarketGoods marketGoods = ((FirmMarket)firm).MarketGoods[j];
						if (marketGoods.RawId != 0) // 1-3
						{
							id = marketGoods.RawId;
							goodsNum++;
							enableFlag[id] = true;
							if (firstGoodsId == 0)
								firstGoodsId = id;
						}
						else if (marketGoods.ProductId != 0) // 1-3
						{
							id = marketGoods.ProductId;
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
			if (!enableFlag[i + 1])
				PickUpEnabled[i] = false;
		}

		if (goodsNum == 0 && PickUpType != NO_PICK_UP)
			PickUpSetNone();

		if (goodsNum == 1 && PickUpType == AUTO_PICK_UP)
		{
			PickUpType = firstGoodsId; // change to selective
			PickUpEnabled[PickUpType - 1] = true;
		}
	}
}
