using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class FirmMarket : Firm
{
	private const int RESTOCK_ANY = 0;
	private const int RESTOCK_PRODUCT = 1;
	private const int RESTOCK_RAW = 2;
	private const int RESTOCK_NONE = 3;

	private int RestockType { get; set; }
	public double MaxStockQty { get; set; } // maximum stock qty of each market goods

	private int NextOutputLinkId { get; set; }
	public int NextOutputFirmId { get; private set; }

	//------------ ai vars -----------//

	private DateTime NoLinkedTownSinceDate { get; set; }
	private DateTime LastImportNewGoodsDate { get; set; }

	//--------------------------------//

	public MarketGoods[] market_goods_array { get; } = new MarketGoods[GameConstants.MAX_MARKET_GOODS];
	public MarketGoods[] market_raw_array { get; } = new MarketGoods[GameConstants.MAX_RAW];
	public MarketGoods[] market_product_array { get; } = new MarketGoods[GameConstants.MAX_PRODUCT];
	
	public FirmMarket()
	{
		MaxStockQty = GameConstants.MARKET_MAX_STOCK_QTY;
		RestockType = RESTOCK_ANY;

		for (int i = 0; i < market_goods_array.Length; i++)
			market_goods_array[i] = new MarketGoods();
		
		for (int i = 0; i < market_raw_array.Length; i++)
			market_raw_array[i] = new MarketGoods();

		for (int i = 0; i < market_product_array.Length; i++)
			market_product_array[i] = new MarketGoods();
	}

	protected override void InitDerived()
	{
		TownArray.DistributeDemand();

		//-------- set restock_type for AI only --------//

		if (AIFirm)
		{
			RestockType = RESTOCK_PRODUCT; // default to product

			for (int i = 0; i < LinkedFirms.Count; i++)
			{
				Firm firm = FirmArray[LinkedFirms[i]];

				//------ if this is our mine -------//

				if (firm.FirmType != FIRM_MINE || firm.NationId != NationId)
					continue;

				//--- if the mine doesn't have links to other market ---//
				
				bool mineHasLinkedMarket = false;

				for (int j = 0; j < firm.LinkedFirms.Count; j++)
				{
					Firm otherFirm = FirmArray[firm.LinkedFirms[j]];

					if (otherFirm.NationId == NationId && otherFirm.FirmId != FirmId && otherFirm.FirmType == FIRM_MARKET &&
					    ((FirmMarket)otherFirm).IsRawMarket())
					{
						mineHasLinkedMarket = true;
						break;
					}
				}

				if (!mineHasLinkedMarket) // if the mine doesn't have any links to other markets
				{
					RestockType = RESTOCK_RAW;
					break;
				}
			}
		}
	}

	public override void NextDay()
	{
		base.NextDay();

		UpdateTradeLink();

		if (Info.TotalDays % GameConstants.PROCESS_GOODS_INTERVAL == FirmId % GameConstants.PROCESS_GOODS_INTERVAL)
		{
			InputGoods(50); // input maximum 50 qty of goods per day
			SetNextOutputFirm(); // set next output firm
		}

		SellGoods();
	}

	public override void NextMonth()
	{
		base.NextMonth();

		//------ post goods supply data ------//

		for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = market_goods_array[i];
			marketGoods.LastMonthSupply = marketGoods.CurMonthSupply;
			marketGoods.CurMonthSupply = 0.0;

			marketGoods.LastMonthSaleQty = marketGoods.CurMonthSaleQty;
			marketGoods.CurMonthSaleQty = 0.0;
		}
	}

	public override void NextYear()
	{
		base.NextYear();

		//------ post goods supply data ------//

		for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = market_goods_array[i];
			marketGoods.LastYearSales = marketGoods.CurYearSales;
			marketGoods.CurYearSales = 0.0;
		}
	}

	public bool IsRawMarket()
	{
		return RestockType == RESTOCK_RAW || RestockType == RESTOCK_ANY;
	}

	public bool IsRetailMarket()
	{
		return RestockType == RESTOCK_PRODUCT || RestockType == RESTOCK_ANY;
	}
	
	private void UpdateTradeLink()
	{
		Nation ownNation = NationArray[NationId];

		//------ update links to harbors -----//

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			Firm firm = FirmArray[LinkedFirms[i]];

			if (firm.FirmType != FIRM_HARBOR)
				continue;

			bool tradeTreaty = firm.NationId == NationId || ownNation.get_relation(firm.NationId).trade_treaty;

			if (LinkedFirmsEnable[i] != (tradeTreaty ? InternalConstants.LINK_EE : InternalConstants.LINK_DD))
				ToggleFirmLink(i + 1, tradeTreaty, InternalConstants.COMMAND_AUTO, true);
		}

		//------ update links to towns -----//

		for (int i = 0; i < LinkedTowns.Count; i++)
		{
			Town town = TownArray[LinkedTowns[i]];

			if (town.NationId == 0)
				continue;

			bool tradeTreaty = town.NationId == NationId || ownNation.get_relation(town.NationId).trade_treaty;

			if (LinkedTownsEnable[i] != (tradeTreaty ? InternalConstants.LINK_EE : InternalConstants.LINK_DD))
				ToggleTownLink(i + 1, tradeTreaty, InternalConstants.COMMAND_AUTO, true);
		}
	}

	private void SetNextOutputFirm()
	{
		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			NextOutputLinkId++;
			if (NextOutputLinkId > LinkedFirms.Count)
				NextOutputLinkId = 1;

			if (LinkedFirmsEnable[NextOutputLinkId - 1] == InternalConstants.LINK_EE)
			{
				int firmId = LinkedFirms[NextOutputLinkId - 1];

				if (FirmArray[firmId].FirmType == FIRM_FACTORY)
				{
					NextOutputFirmId = firmId;
					return;
				}
			}
		}

		NextOutputFirmId = 0;
	}
	
	private void InputGoods(int maxInputQty)
	{
		//------ scan for a firm to input raw materials --------//

		Nation nation = NationArray[NationId];
		bool[] is_inputing_array = new bool[GameConstants.MAX_MARKET_GOODS];
		int queuedFirmId = 0;

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			if (LinkedFirmsEnable[i] != InternalConstants.LINK_EE)
				continue;

			Firm firm = FirmArray[LinkedFirms[i]];

			if (firm.FirmType == FIRM_MINE && IsRawMarket())
			{
				FirmMine firmMine = (FirmMine)firm;

				if (firmMine.RawId != 0)
				{
					int j;
					for (j = 0; j < GameConstants.MAX_MARKET_GOODS; j++)
					{
						MarketGoods marketGoods = market_goods_array[j];

						//--- only assign a slot to the product if it comes from a firm of our own ---//

						if (marketGoods.RawId == firmMine.RawId)
						{
							is_inputing_array[j] = true;

							if (firmMine.NextOutputFirmId == FirmId &&
							    firmMine.StockQty > 0.0 && marketGoods.StockQty < MaxStockQty)
							{
								double inputQty = Math.Min(firmMine.StockQty, maxInputQty);
								inputQty = Math.Min(inputQty, MaxStockQty - marketGoods.StockQty);

								firmMine.StockQty -= inputQty;
								marketGoods.StockQty += inputQty;
								marketGoods.CurMonthSupply += inputQty;

								if (firm.NationId != NationId)
									nation.import_goods(NationBase.IMPORT_RAW, firm.NationId, inputQty * GameConstants.RAW_PRICE);
							}
							else if (marketGoods.StockQty >= MaxStockQty)
							{
								// add it so the other functions can know that this market has direct supply links
								// TODO check
								marketGoods.CurMonthSupply++;
							}

							break;
						}
					}

					//----- no matched slot for this goods -----//

					if (j == GameConstants.MAX_MARKET_GOODS && firmMine.StockQty > 0.0 && queuedFirmId == 0)
						queuedFirmId = firm.FirmId;
				}
			}

			if (firm.FirmType == FIRM_FACTORY && IsRetailMarket())
			{
				FirmFactory firmFactory = (FirmFactory)firm;

				if (firmFactory.ProductRawId != 0)
				{
					int j;
					for (j = 0; j < GameConstants.MAX_MARKET_GOODS; j++)
					{
						MarketGoods marketGoods = market_goods_array[j];

						if (marketGoods.ProductId == firmFactory.ProductRawId)
						{
							is_inputing_array[j] = true;

							if (firmFactory.NextOutputFirmId == FirmId &&
							    firmFactory.StockQty > 0.0 && marketGoods.StockQty < MaxStockQty)
							{
								double inputQty = Math.Min(firmFactory.StockQty, maxInputQty);
								inputQty = Math.Min(inputQty, MaxStockQty - marketGoods.StockQty);

								firmFactory.StockQty -= inputQty;
								marketGoods.StockQty += inputQty;
								marketGoods.CurMonthSupply += inputQty;

								if (firm.NationId != NationId)
									nation.import_goods(NationBase.IMPORT_PRODUCT, firm.NationId, inputQty * GameConstants.PRODUCT_PRICE);
							}
							else if (marketGoods.StockQty >= MaxStockQty)
							{
								// add it so the other functions can know that this market has direct supply links
								// TODO check
								marketGoods.CurMonthSupply++;
							}

							break;
						}
					}

					//----- no matched slot for this goods -----//

					if (j == GameConstants.MAX_MARKET_GOODS && firmFactory.StockQty > 0.0 && queuedFirmId == 0)
						queuedFirmId = firm.FirmId;
				}
			}
		}

		//---- if there are any empty slots for new goods -----//

		if (queuedFirmId > 0)
		{
			Firm firm = FirmArray[queuedFirmId];

			for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
			{
				MarketGoods marketGoods = market_goods_array[i];

				if (!is_inputing_array[i] && marketGoods.StockQty <= 0.0)
				{
					if (firm.FirmType == FIRM_MINE && IsRawMarket())
					{
						SetGoods(true, ((FirmMine)firm).RawId, i);
						break;
					}

					if (firm.FirmType == FIRM_FACTORY && IsRetailMarket())
					{
						SetGoods(false, ((FirmFactory)firm).ProductRawId, i);
						break;
					}
				}
			}
		}
	}
	
	private void SellGoods()
	{
		for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = market_goods_array[i];
			if (marketGoods.ProductId != 0 && marketGoods.StockQty > 0.0)
			{
				double saleQty = Math.Min(marketGoods.MonthDemand / 30.0, marketGoods.StockQty);

				marketGoods.StockQty -= saleQty;

				marketGoods.CurMonthSaleQty += saleQty;
				marketGoods.CurYearSales += saleQty * GameConstants.CONSUMER_PRICE;

				AddIncome(NationBase.INCOME_SELL_GOODS, saleQty * GameConstants.CONSUMER_PRICE);
			}
		}
	}

	public void SetGoods(bool isRaw, int goodsId, int position)
	{
		MarketGoods marketGoods = market_goods_array[position];

		if (isRaw)
		{
			if (marketGoods.RawId != 0)
				market_raw_array[marketGoods.RawId - 1] = null;
			else if (marketGoods.ProductId != 0)
				market_product_array[marketGoods.ProductId - 1] = null;

			marketGoods.RawId = goodsId;
			marketGoods.ProductId = 0;
			market_raw_array[goodsId - 1] = marketGoods;
		}
		else
		{
			if (marketGoods.ProductId != 0)
				market_product_array[marketGoods.ProductId - 1] = null;
			else if (marketGoods.RawId != 0)
				market_raw_array[marketGoods.RawId - 1] = null;

			marketGoods.RawId = 0;
			marketGoods.ProductId = goodsId;
			market_product_array[goodsId - 1] = marketGoods;
		}
	}
	
	public void SwitchRestock()
	{
		RestockType++;
		if (RestockType > RESTOCK_NONE)
			RestockType = RESTOCK_ANY;
	}

	public void ClearMarketGoods(int position)
	{
		MarketGoods marketGoods = market_goods_array[position - 1];

		marketGoods.StockQty = 0.0;

		if (marketGoods.RawId != 0)
		{
			market_raw_array[marketGoods.RawId - 1] = null;
			marketGoods.RawId = 0;
		}
		else
		{
			market_product_array[marketGoods.ProductId - 1] = null;
			marketGoods.ProductId = 0;
		}
	}

	public bool CanHireCaravan()
	{
		Nation nation = NationArray[NationId];

		if (nation.cash < UnitRes[UnitConstants.UNIT_CARAVAN].build_cost)
			return false;

		int supportedCaravan = nation.total_population / GameConstants.POPULATION_PER_CARAVAN;
		int caravanCount = UnitRes[UnitConstants.UNIT_CARAVAN].nation_unit_count_array[NationId - 1];

		return supportedCaravan > caravanCount;
	}

	public int HireCaravan(int remoteAction)
	{
		if (!CanHireCaravan())
			return 0;

		//---------------------------------------//

		//if (!remoteAction && remote.is_enable())
		//{
			//// packet structure : <town recno>
			//short *shortPtr = (short *) remote.new_send_queue_msg(MSG_F_MARKET_HIRE_CARA, sizeof(short));
			//*shortPtr = firm_recno;
			//return 0;
		//}

		//---------- add the unit now -----------//

		int unitId = CreateUnit(UnitConstants.UNIT_CARAVAN);
		if (unitId == 0)
			return 0;

		UnitCaravan unitCaravan = (UnitCaravan)UnitArray[unitId];
		unitCaravan.set_stop(1, LocX1, LocY1, InternalConstants.COMMAND_AUTO);
		Nation nation = NationArray[NationId];
		nation.add_expense(NationBase.EXPENSE_CARAVAN, UnitRes[UnitConstants.UNIT_CARAVAN].build_cost, true);

		return unitCaravan.SpriteId;
	}
	
	public override void DrawDetails(IRenderer renderer)
	{
		renderer.DrawMarketDetails(this);
	}

	public override void HandleDetailsInput(IRenderer renderer)
	{
		renderer.HandleMarketDetailsInput(this);
	}
	

	#region Old AI Functions

	public override void ProcessAI()
	{
		//---- think about deleting this firm ----//

		if (Info.TotalDays % 30 == FirmId % 30)
		{
			if (ThinkDel())
				return;
		}

		//----- think about demand trade treaty -----//

		if (Info.TotalDays % 30 == FirmId % 30)
			ThinkDemandTradeTreaty();

		//----- think about building a factory next to this market ----//

		if (Info.TotalDays % 60 == FirmId % 60)
			ThinkMarketBuildFactory();

		//TODO think_export_product();

		//-------- think about new trading routes --------//

		// don't new imports until it's 60 days after the last one was imported
		if (Info.game_date < LastImportNewGoodsDate.AddDays(60.0))
			return;

		if (CanHireCaravan())
		{
			Nation ownNation = NationArray[NationId];

			int thinkInterval = 10 + (100 - ownNation.pref_trading_tendency) / 5; // think once every 10 to 30 days

			if (IsMarketLinkedToTown())
			{
				if (Info.TotalDays % thinkInterval == FirmId % thinkInterval)
					ThinkImportNewProduct();

				if (Info.TotalDays % 60 == (FirmId + 20) % 60)
				{
					//------------------------------------------------------//
					// Don't think about increasing existing product supply
					// if we have just import a new goods, it takes time
					// to transport and pile up goods.
					//------------------------------------------------------//

					// only think increase existing supply 180 days after importing a new one
					if (LastImportNewGoodsDate == default || Info.game_date > LastImportNewGoodsDate.AddDays(180.0))
					{
						ThinkIncreaseExistingProductSupply();
					}
				}
			}

			if (Info.TotalDays % thinkInterval == FirmId % thinkInterval)
				ThinkExportProduct();
		}
	}
	
	private bool ThinkDel()
	{
		//if (linked_town_count > 0)
		//return 0;

		//MarketGoods* marketGoods = market_goods_array;
		//for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++, marketGoods++)
		//{
		//if (marketGoods.stock_qty > 0)
		//return 0;
		//}

		if (LinkedTowns.Count > 0)
		{
			NoLinkedTownSinceDate = default; // reset it
			return false;
		}
		else
		{
			NoLinkedTownSinceDate = Info.game_date;
		}

		//---- don't delete it if there are still significant stock here ---//

		if (StockValueIndex() >= 10)
		{
			Nation ownNation = NationArray[NationId];

			//--- if the market has been sitting idle for too long, delete it ---//

			if (Info.game_date < NoLinkedTownSinceDate.AddDays(180.0 + 180.0 * ownNation.pref_trading_tendency / 100.0))
			{
				return false;
			}
		}

		//------------------------------------------------//

		AIDelFirm();

		return true;
	}

	private bool ThinkImportNewProduct()
	{
		//---- check if the market place has free space for new supply ----//

		int emptySlot = 0;

		for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = market_goods_array[i];
			if (marketGoods.ProductId == 0 && marketGoods.RawId == 0)
				emptySlot++;
		}

		if (emptySlot == 0)
			return false;

		//--- update what products are needed for this market place ---//

		// the total population in the towns linked to the market that needs the supply of the product
		int[] needProductSupplyPop = new int[GameConstants.MAX_PRODUCT];
		Nation nation = NationArray[NationId];

		for (int i = 0; i < LinkedTowns.Count; i++)
		{
			if (LinkedTownsEnable[i] != InternalConstants.LINK_EE)
				continue;

			Town town = TownArray[LinkedTowns[i]];

			if (town.RegionId != RegionId)
				continue;

			if (!town.IsBaseTown) // don't import if it isn't a base town
				continue;

			//------------------------------------------------//
			//
			// Only if the population of the town is equal or larger than minTradePop,
			// the AI will try to do trade.
			// The minTradePop is between 10 to 20 depending on the pref_trading_tendency.
			//
			//------------------------------------------------//

			town.UpdateProductSupply();

			for (int j = 0; j < GameConstants.MAX_PRODUCT; j++)
			{
				if (!town.HasProductSupply[j])
					needProductSupplyPop[j] += town.Population;
			}
		}

		//---- think about importing the products that need supply ----//

		int minTradePop = 10;

		if (IsRetailMarket())
		{
			for (int productId = 1; productId <= GameConstants.MAX_PRODUCT; productId++)
			{
				// if  market is empty, try to import some goods
				if (needProductSupplyPop[productId - 1] >= minTradePop || emptySlot == GameConstants.MAX_MARKET_GOODS)
				{
					if (ThinkImportSpecificProduct(productId))
					{
						LastImportNewGoodsDate = Info.game_date;
						return true;
					}
				}
			}
		}

		//----------------------------------------------------------//
		// Think about importing the raw materials of the needed
		// products and build factories to manufacture them ourselves
		//----------------------------------------------------------//

		//--- first check if we can build a new factory to manufacture the products ---//

		if (IsRawMarket() && IsMarketLinkedToTown(true)) // 1-only count towns that are our own and are base towns
		{
			// if there is a shortage of caravan supplies, use it for transporting finished products instead of raw materials
			if (!NoNeighborSpace && nation.total_jobless_population >= MAX_WORKER * 2 && CanHireCaravan())
			{
				if (nation.can_ai_build(FIRM_FACTORY))
				{
					for (int productId = 1; productId <= GameConstants.MAX_PRODUCT; productId++)
					{
						if (needProductSupplyPop[productId - 1] >= minTradePop)
						{
							if (ThinkMftSpecificProduct(productId))
							{
								LastImportNewGoodsDate = Info.game_date;
								return true;
							}
						}
					}
				}
			}
		}

		return false;
	}

	private bool ThinkIncreaseExistingProductSupply()
	{
		for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = market_goods_array[i];
			if (marketGoods.ProductId != 0)
			{
				// the supply falls behind the demand by at least 20%
				if (marketGoods.StockQty < GameConstants.MARKET_MAX_STOCK_QTY / 10.0 &&
				    marketGoods.MonthDemand * 0.8 > marketGoods.Supply30Days())
				{
					if (ThinkImportSpecificProduct(marketGoods.ProductId))
						return true;
				}
			}
		}

		return false;
	}

	private bool ThinkImportSpecificProduct(int productId)
	{
		Nation nation = NationArray[NationId];
		Firm bestFirm = null;
		int bestRating = 0;

		List<int> product_supply_firm_array = new List<int>();

		foreach (Firm firm in FirmArray)
		{
			//-------- factory as a potential supplier ------//

			if (firm.FirmType == FIRM_FACTORY)
			{
				FirmFactory firmFactory = (FirmFactory)firm;

				if (firmFactory.ProductRawId == productId && firmFactory.StockQty > firmFactory.MaxStockQty / 5.0)
				{
					product_supply_firm_array.Add(firm.FirmId);
				}
			}

			//-------- market place as a potential supplier ------//

			else if (firm.FirmType == FIRM_MARKET)
			{
				FirmMarket firmMarket = (FirmMarket)firm;

				for (int i = 0; i < firmMarket.market_goods_array.Length; i++)
				{
					MarketGoods marketGoods = firmMarket.market_goods_array[i];
					if (marketGoods.StockQty > GameConstants.MARKET_MAX_STOCK_QTY / 5.0)
					{
						if (marketGoods.ProductId == productId)
							product_supply_firm_array.Add(firm.FirmId);
					}
				}
			}
		}

		for (int i = product_supply_firm_array.Count - 1; i >= 0; i--)
		{
			int firmId = product_supply_firm_array[i];

			if (FirmArray.IsDeleted(firmId) || firmId == FirmId)
				continue;

			//-- if there is already a caravan travelling between two points --//

			Firm firm = FirmArray[firmId];

			if (firm.RegionId != RegionId)
				continue;

			//-----------------------------------------//
			// The rating of a supply is determined by:
			//	- distance
			// - supply
			// - nation relationship
			//-----------------------------------------//

			//------ determine the stock level of this supply ------//

			double stockLevel = 0.0;

			//---- think about inputting goods from this factory ----//

			if (firm.FirmType == FIRM_FACTORY)
			{
				if (firm.NationId != NationId) // can import goods from own factories only
					continue;

				FirmFactory firmFactory = (FirmFactory)firm;

				if (firmFactory.ProductRawId == productId)
					stockLevel = 100.0 * firmFactory.StockQty / firmFactory.MaxStockQty;
			}

			//---- think about inputting goods from this market ----//

			else if (firm.FirmType == FIRM_MARKET)
			{
				//--- if this is a foreign sale market, don't import from it (e.g. Nation A's market built near Nation B's village -----//

				if (firm.NationId != NationId) // if this is not our market
				{
					int j;
					for (j = firm.LinkedTowns.Count - 1; j >= 0; j--)
					{
						Town town = TownArray[firm.LinkedTowns[j]];

						if (town.NationId == firm.NationId)
							break;
					}

					if (j < 0) // if this market is not linked to its own town (then it must be a foreign market)
						continue;
				}

				//-- only either from own market place or from nations that trade with you --//

				if (!NationArray[firm.NationId].get_relation(NationId).trade_treaty)
					continue;

				MarketGoods marketGoods = ((FirmMarket)firm).market_product_array[productId - 1];

				//--- if this market has the supply of this goods ----//

				if (marketGoods != null && marketGoods.Supply30Days() > 0)
				{
					stockLevel = 100.0 * marketGoods.StockQty / GameConstants.MARKET_MAX_STOCK_QTY;
				}
			}

			//----------------------------------------------//

			int curRating;
			if (firm.NationId == NationId)
			{
				if (stockLevel < 10.0) // for our own market, the stock level requirement is lower
					continue;

				curRating = 50;
			}
			else
			{
				if (stockLevel < 20.0) // for other player's market, only import when the stock level is high enough
					continue;

				curRating = nation.get_relation_status(firm.NationId) * 5;
			}

			//---- calculate the current overall rating ----//

			curRating += Convert.ToInt32(stockLevel / 2.0) +
			             World.DistanceRating(LocCenterX, LocCenterY, firm.LocCenterX, firm.LocCenterY);

			//----------- compare ratings -------------//

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestFirm = firm;
			}
		}

		//---- if a suitable supplier is found -----//

		if (bestFirm != null)
			return AICreateNewTrade(bestFirm, 0, TradeStop.PICK_UP_PRODUCT_FIRST + productId - 1);

		return false;
	}

	private bool ThinkMftSpecificProduct(int rawId)
	{
		Firm bestFirm = null;
		int bestRating = 0;
		Nation nation = NationArray[NationId];

		List<int> raw_supply_firm_array = new List<int>();

		foreach (Firm firm in FirmArray)
		{
			//-------- mine as a potential supplier ------//

			if (firm.FirmType == FIRM_MINE)
			{
				FirmMine firmMine = (FirmMine)firm;

				if (firmMine.RawId == rawId && firmMine.StockQty > firmMine.MaxStockQty / 5.0)
				{
					raw_supply_firm_array.Add(firm.FirmId);
				}
			}

			//-------- market place as a potential supplier ------//

			else if (firm.FirmType == FIRM_MARKET)
			{
				FirmMarket firmMarket = (FirmMarket)firm;

				for (int i = 0; i < firmMarket.market_goods_array.Length; i++)
				{
					MarketGoods marketGoods = firmMarket.market_goods_array[i];
					if (marketGoods.StockQty > GameConstants.MARKET_MAX_STOCK_QTY / 5.0)
					{
						if (marketGoods.RawId == rawId)
							raw_supply_firm_array.Add(firm.FirmId);
					}
				}
			}
		}
		
		for (int i = raw_supply_firm_array.Count - 1; i >= 0; i--)
		{
			int firmId = raw_supply_firm_array[i];

			if (FirmArray.IsDeleted(firmId) || firmId == FirmId)
				continue;

			//-- if there is already a caravan travelling between two points --//

			Firm firm = FirmArray[firmId];

			if (firm.RegionId != RegionId)
				continue;

			//-- if this is our own supply, don't import the raw material, but import the finished goods instead. --//

			if (firm.NationId == NationId)
				continue;

			//-----------------------------------------//
			// The rating of a supply is determined by:
			//	- distance
			// - supply
			// - nation relationship
			//-----------------------------------------//

			//------ determine the stock level of this supply ------//

			double stockLevel = 0.0;

			if (firm.FirmType == FIRM_MARKET)
			{
				//-- only either from own market place or from nations that trade with you --//

				if (!NationArray[firm.NationId].get_relation(NationId).trade_treaty)
					continue;

				//----- check if this market is linked to any mines directly ----//

				int j;
				for (j = firm.LinkedFirms.Count - 1; j >= 0; j--)
				{
					Firm linkedFirm = FirmArray[firm.LinkedFirms[j]];

					if (linkedFirm.FirmType == FIRM_MINE && linkedFirm.NationId == firm.NationId)
					{
						if (((FirmMine)linkedFirm).RawId == rawId)
							break;
					}
				}

				if (j < 0) // this market does not have any direct supplies, so don't pick up goods from it 
					continue;

				//---------------------------------------------------------------//


				for (j = 0; j < GameConstants.MAX_MARKET_GOODS; j++)
				{
					MarketGoods marketGoods = ((FirmMarket)firm).market_goods_array[j];
					if (marketGoods.StockQty > GameConstants.MARKET_MAX_STOCK_QTY / 5)
					{
						if (marketGoods.RawId == rawId)
							stockLevel = 100 * marketGoods.StockQty / GameConstants.MARKET_MAX_STOCK_QTY;
					}
				}
			}

			if (stockLevel < 50.0) // if the stock is too low, don't consider it
				continue;

			//---- calculate the current overall rating ----//

			NationRelation nationRelation = nation.get_relation(firm.NationId);

			int curRating = Convert.ToInt32(stockLevel) - 100 * Misc.points_distance(LocCenterX, LocCenterY,
				firm.LocCenterX, firm.LocCenterY) /GameConstants.MapSize;

			if (firm.NationId == NationId)
				curRating += 100;
			else
				curRating += nationRelation.status * 20;

			//----------- compare ratings -------------//

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestFirm = firm;
			}
		}

		if (bestFirm == null)
			return false;

		if (!AICreateNewTrade(bestFirm, 0, TradeStop.PICK_UP_RAW_FIRST + rawId - 1))
			return false;

		return true;
	}

	private bool ThinkExportProduct()
	{
		//--- first check if there is any excessive supply for export ---//

		int exportProductId = 0;

		for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = market_goods_array[i];
			if (marketGoods.ProductId != 0)
			{
				// the supply is at least double of the demand
				if (marketGoods.StockQty > GameConstants.MARKET_MAX_STOCK_QTY * 3.0 / 4.0 &&
				    marketGoods.MonthDemand < marketGoods.Supply30Days() / 2)
				{
					exportProductId = marketGoods.ProductId;
					break;
				}
			}
		}

		if (exportProductId == 0)
			return false;

		//----- locate for towns that do not have the supply of the product ----//

		Nation nation = NationArray[NationId];

		foreach (Town town in TownArray)
		{
			// 10 to 20 as the minimum population for considering trade
			if (town.Population < 20 - (10 * nation.pref_trading_tendency / 100))
				continue;

			// if the town already has the supply of product, return now
			if (town.HasProductSupply[exportProductId - 1])
				continue;

			if (town.RegionId != RegionId)
				continue;

			if (town.NoNeighborSpace) // if there is no space in the neighbor area for building a new firm.
				continue;

			// don't consider if it is too far away
			if (Misc.RectsDistance(LocX1, LocY1, LocX2, LocY2,
				    town.LocX1, town.LocY1, town.LocX2, town.LocY2) > GameConstants.MapSize / 4)
				continue;

			//-----------------------------------------//

			if (town.NationId != 0)
			{
				// only build markets to friendly nation's town

				if (nation.get_relation_status(town.NationId) < NationBase.NATION_FRIENDLY)
					continue;

				//--- if it's a nation town, only export if we have trade treaty with it ---//

				if (!nation.get_relation(town.NationId).trade_treaty)
					continue;
			}
			else
			{
				//--- if it's an independent town, only export if the resistance is low ---//

				if (town.AverageResistance(NationId) > GameConstants.INDEPENDENT_LINK_RESISTANCE)
					continue;
			}

			//----- think about building a new market to the town for exporting our goods -----//

			if (ThinkBuildExportMarket(town.TownId))
				return true;
		}

		return false;
	}

	private bool ThinkBuildExportMarket(int townId)
	{
		Town town = TownArray[townId];
		Nation nation = NationArray[NationId];

		//---- see if we already have a market linked to this town ----//

		for (int i = 0; i < town.LinkedFirms.Count; i++)
		{
			Firm firm = FirmArray[town.LinkedFirms[i]];

			if (firm.FirmType != FIRM_MARKET || firm.FirmId == FirmId)
				continue;

			//--- if we already have a market there, no need to build a new market ----//

			if (firm.NationId == NationId)
				return false;
		}

		//--- if there is no market place linked to this town, we can set up one ourselves ---//

		if (!nation.find_best_firm_loc(FIRM_MARKET, town.LocX1, town.LocY1, out int buildXLoc, out int buildYLoc))
		{
			town.NoNeighborSpace = true;
			return false;
		}

		nation.add_action(buildXLoc, buildYLoc, town.LocX1, town.LocY1, Nation.ACTION_AI_BUILD_FIRM, FIRM_MARKET);
		return true;
	}

	private void ThinkDemandTradeTreaty()
	{
		Nation nation = NationArray[NationId];

		//----- demand towns to open up market ----//

		for (int i = 0; i < LinkedTowns.Count; i++)
		{
			//----- if the link is not enabled -----//

			if (LinkedTownsEnable[i] != InternalConstants.LINK_EE)
			{
				int townNationId = TownArray[LinkedTowns[i]].NationId;

				if (townNationId != 0)
					nation.get_relation(townNationId).ai_demand_trade_treaty++;
			}
		}
	}

	private void ThinkMarketBuildFactory()
	{
		if (NoNeighborSpace) // if there is no space in the neighbor area for building a new firm.
			return;

		//---- think about building factories to manufacture goods using raw materials in the market place ---//

		// always set it to 2, so think_build_factory() will start to build a market as soon as there is a need
		AIShouldBuildFactoryCount = 2;

		for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = market_goods_array[i];
			if (marketGoods.RawId == 0)
				continue;

			if (marketGoods.StockQty < 250) // only when the stock is >= 250
				continue;

			//----- check if the raw materials are from a local mine, if so don't build a factory,
			//we only build a factory to manufacture goods using raw materials from a remote town.

			int j;
			for (j = 0; j < LinkedFirms.Count; j++)
			{
				Firm firm = FirmArray[LinkedFirms[j]];

				if (firm.FirmType == FIRM_MINE && firm.NationId == NationId &&
				    ((FirmMine)firm).RawId == marketGoods.RawId)
				{
					break;
				}
			}

			if (j < LinkedFirms.Count) // if this raw material is from a local mine
				continue;

			//-------------------------------------------//

			if (ThinkBuildFactory(marketGoods.RawId))
				return;
		}
	}

	public override void AIUpdateLinkStatus()
	{
		//---- make sure the restocking type is defined ----//

		if (RestockType == RESTOCK_ANY)
		{
			bool hasRawResource = false;
			for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
			{
				MarketGoods marketGoods = market_goods_array[i];
				if (marketGoods.RawId != 0)
				{
					hasRawResource = true;
					break;
				}
			}

			if (hasRawResource)
				RestockType = RESTOCK_RAW;
			else
				RestockType = RESTOCK_PRODUCT;
		}

		//---- consider enabling/disabling links to firms ----//

		bool rc;

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			Firm firm = FirmArray[LinkedFirms[i]];

			//-------- check product type ----------//

			if (IsRetailMarket())
				rc = firm.FirmType == FIRM_FACTORY;
			else
				rc = firm.FirmType == FIRM_MINE || firm.FirmType == FIRM_FACTORY;

			ToggleFirmLink(i + 1, rc, InternalConstants.COMMAND_AI); // enable the link
		}

		//----- always enable links to towns as there is no downsides for selling goods to the villagers ----//

		for (int i = 0; i < LinkedTowns.Count; i++)
		{
			ToggleTownLink(i + 1, true, InternalConstants.COMMAND_AI); // enable the link
		}
	}

	private bool AICreateNewTrade(Firm firm, int stop1PickUpType, int stop2PickUpType)
	{
		//---- see if there is already a caravan moving along the route -----//

		Nation ownNation = NationArray[NationId];
		int caravanInRouteCount = 0;
		int stop1Id, stop2Id;
		UnitCaravan unitCaravan;

		for (int i = ownNation.ai_caravan_array.Count - 1; i >= 0; i--)
		{
			unitCaravan = (UnitCaravan)UnitArray[ownNation.ai_caravan_array[i]];

			if (unitCaravan.stop_defined_num < 2)
				continue;

			if (unitCaravan.stop_array[0].firm_recno == FirmId &&
			    unitCaravan.stop_array[1].firm_recno == firm.FirmId)
			{
				stop1Id = 1;
				stop2Id = 2;
			}
			else if (unitCaravan.stop_array[1].firm_recno == FirmId &&
			         unitCaravan.stop_array[0].firm_recno == firm.FirmId)
			{
				stop1Id = 2;
				stop2Id = 1;
			}
			else
			{
				continue;
			}

			//------- add the goods to the pick up list ----//

			bool rc = false;

			if (stop1PickUpType != 0 && !unitCaravan.has_pick_up_type(stop1Id, stop1PickUpType))
			{
				if (unitCaravan.IsVisible()) // can't set stop when the caravan is in a firm
					unitCaravan.set_stop_pick_up(stop1Id, stop1PickUpType, InternalConstants.COMMAND_AI);
				rc = true;
			}

			if (stop2PickUpType != 0 && !unitCaravan.has_pick_up_type(stop2Id, stop2PickUpType))
			{
				if (unitCaravan.IsVisible()) // can't set stop when the caravan is in a firm
					unitCaravan.set_stop_pick_up(stop2Id, stop2PickUpType, InternalConstants.COMMAND_AI);
				rc = true;
			}

			if (rc) // don't add one if we can utilize an existing one.
				return true;

			caravanInRouteCount++;
		}

		if (caravanInRouteCount >= 2) // don't have more than 2 caravans on a single route
			return false;

		//----------- hire a new caravan -----------//

		int unitRecno = HireCaravan(InternalConstants.COMMAND_AI);

		if (unitRecno == 0)
			return false;

		//----------- set up the trade route ----------//

		unitCaravan = (UnitCaravan)UnitArray[unitRecno];

		unitCaravan.set_stop(2, firm.LocX1, firm.LocY1, InternalConstants.COMMAND_AI);

		unitCaravan.set_stop_pick_up(1, TradeStop.NO_PICK_UP, InternalConstants.COMMAND_AI);
		unitCaravan.set_stop_pick_up(2, TradeStop.NO_PICK_UP, InternalConstants.COMMAND_AI);

		if (stop1PickUpType != 0)
			unitCaravan.set_stop_pick_up(1, stop1PickUpType, InternalConstants.COMMAND_AI);

		if (stop2PickUpType != 0)
			unitCaravan.set_stop_pick_up(2, stop2PickUpType, InternalConstants.COMMAND_AI);

		return true;
	}
	
	public int StockValueIndex() // for AI, a 0-100 index number telling the total value of the market's stock
	{
		double totalValue = 0.0;

		for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = market_goods_array[i];
			if (marketGoods.RawId != 0)
			{
				totalValue += marketGoods.StockQty * GameConstants.RAW_PRICE;
			}
			else if (marketGoods.ProductId != 0)
			{
				totalValue += marketGoods.StockQty * GameConstants.PRODUCT_PRICE;
			}
		}

		return 100 * (int)totalValue /
		       (GameConstants.MAX_MARKET_GOODS * GameConstants.PRODUCT_PRICE * GameConstants.MARKET_MAX_STOCK_QTY);
	}

	public bool IsMarketLinkedToTown(bool ownBaseTownOnly = false)
	{
		for (int i = 0; i < LinkedTowns.Count; i++)
		{
			if (LinkedTownsEnable[i] != InternalConstants.LINK_EE)
				continue;

			if (ownBaseTownOnly)
			{
				Town town = TownArray[LinkedTowns[i]];

				if (town.NationId == NationId && town.IsBaseTown)
					return true;
			}
			else
			{
				return true;
			}
		}

		return false;
	}

	#endregion
}