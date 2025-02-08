using System;

namespace TenKingdoms;

public class FirmMarket : Firm
{
	public const int RESTOCK_ANY = 0;
	public const int RESTOCK_PRODUCT = 1;
	public const int RESTOCK_RAW = 2;
	public const int RESTOCK_NONE = 3;

	public double max_stock_qty; // maximum stock qty of each market goods

	public MarketGoods[] market_goods_array = new MarketGoods[GameConstants.MAX_MARKET_GOODS];
	public MarketGoods[] market_raw_array = new MarketGoods[GameConstants.MAX_RAW];
	public MarketGoods[] market_product_array = new MarketGoods[GameConstants.MAX_PRODUCT];

	public int next_output_link_id;
	public int next_output_firm_recno;

	//------------ ai vars -----------//

	public DateTime no_linked_town_since_date;
	public DateTime last_import_new_goods_date;

	//--------------------------------//

	public int restock_type;

	protected RawRes RawRes => Sys.Instance.RawRes;

	public FirmMarket()
	{
		max_stock_qty = GameConstants.MAX_MARKET_STOCK;
		restock_type = RESTOCK_ANY;

		for (int i = 0; i < market_goods_array.Length; i++)
			market_goods_array[i] = new MarketGoods();
		
		for (int i = 0; i < market_raw_array.Length; i++)
			market_raw_array[i] = new MarketGoods();

		for (int i = 0; i < market_product_array.Length; i++)
			market_product_array[i] = new MarketGoods();
	}

	protected override void init_derived()
	{
		//------ redistribute town demand --------//

		TownArray.DistributeDemand();

		//-------- set restock_type for AI only --------//

		if (firm_ai)
		{
			restock_type = RESTOCK_PRODUCT; // default to product

			for (int i = 0; i < linked_firm_array.Count; i++)
			{
				Firm firm = FirmArray[linked_firm_array[i]];

				//------ if this is our mine -------//

				if (firm.firm_id != FIRM_MINE || firm.nation_recno != nation_recno)
				{
					continue;
				}

				//--- if the mine doesn't have links to other market ---//

				int j;
				for (j = firm.linked_firm_array.Count - 1; j >= 0; j--)
				{
					Firm otherFirm = FirmArray[firm.linked_firm_array[j]];

					if (otherFirm.nation_recno == nation_recno &&
					    otherFirm.firm_recno != firm_recno &&
					    otherFirm.firm_id == FIRM_MARKET &&
					    ((FirmMarket)otherFirm).is_raw_market())
					{
						break;
					}
				}

				if (j < 0) // if the mine doesn't have any links to other markets
				{
					restock_type = RESTOCK_RAW;
					break;
				}
			}
		}
	}


	public override void next_day()
	{
		base.next_day();

		//---- update trade link to harbors to towns -----//

		update_trade_link();

		//-------- input goods ----------//

		if (Info.TotalDays % GameConstants.PROCESS_GOODS_INTERVAL == firm_recno % GameConstants.PROCESS_GOODS_INTERVAL)
		{
			input_goods(50); // input maximum 50 qty of goods per day
			set_next_output_firm(); // set next output firm
		}

		//-------- sell goods --------//

		sell_goods();
	}

	public override void next_month()
	{
		base.next_month();

		//------ post goods supply data ------//

		for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = market_goods_array[i];
			marketGoods.last_month_supply = marketGoods.cur_month_supply;
			marketGoods.cur_month_supply = 0.0;

			marketGoods.last_month_sale_qty = marketGoods.cur_month_sale_qty;
			marketGoods.cur_month_sale_qty = 0.0;
		}
	}

	public override void next_year()
	{
		base.next_year();

		//------ post goods supply data ------//

		for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = market_goods_array[i];
			marketGoods.last_year_sales = marketGoods.cur_year_sales;
			marketGoods.cur_year_sales = 0.0;
		}
	}

	public int free_slot_count()
	{
		int freeSlotCount = 0;

		for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = market_goods_array[i];
			if (marketGoods.raw_id == 0 && marketGoods.product_raw_id == 0)
				freeSlotCount++;
		}

		return freeSlotCount;
	}

	public int stock_value_index() // for AI, a 0-100 index number telling the total value of the market's stock
	{
		double totalValue = 0.0;

		for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = market_goods_array[i];
			if (marketGoods.raw_id != 0)
			{
				totalValue += marketGoods.stock_qty * GameConstants.RAW_PRICE;
			}
			else if (marketGoods.product_raw_id != 0)
			{
				totalValue += marketGoods.stock_qty * GameConstants.PRODUCT_PRICE;
			}
		}

		return 100 * (int)totalValue /
		       (GameConstants.MAX_MARKET_GOODS * GameConstants.PRODUCT_PRICE * GameConstants.MAX_MARKET_STOCK);
	}

	public void sell_goods()
	{
		//----------- sell products now ------------//

		for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = market_goods_array[i];
			if (marketGoods.product_raw_id != 0 && marketGoods.stock_qty > 0)
			{
				double saleQty = Math.Min(marketGoods.month_demand / 30.0, marketGoods.stock_qty);

				marketGoods.stock_qty -= saleQty;

				marketGoods.cur_month_sale_qty += saleQty;
				marketGoods.cur_year_sales += saleQty * GameConstants.CONSUMER_PRICE;

				add_income(NationBase.INCOME_SELL_GOODS, saleQty * GameConstants.CONSUMER_PRICE);
			}
		}
	}

	public int hire_caravan(int remoteAction)
	{
		return 0;
		
		if (can_hire_caravan() == 0)
			return 0;

		//---------------------------------------//

		Nation nation = NationArray[nation_recno];

		//if(!remoteAction && remote.is_enable())
		//{
		//// packet structure : <town recno>
		//short *shortPtr = (short *) remote.new_send_queue_msg(MSG_F_MARKET_HIRE_CARA, sizeof(short));
		//*shortPtr = firm_recno;
		//return 0;
		//}

		//---------- add the unit now -----------//

		int unitRecno = create_unit(UnitConstants.UNIT_CARAVAN);
		if (unitRecno == 0)
			return 0;

		UnitCaravan unitCaravan = (UnitCaravan)UnitArray[unitRecno];

		unitCaravan.loyalty = 100;
		unitCaravan.set_stop(1, loc_x1, loc_y1, InternalConstants.COMMAND_AUTO);
		nation.add_expense(NationBase.EXPENSE_CARAVAN, UnitRes[UnitConstants.UNIT_CARAVAN].build_cost, true);

		return unitCaravan.sprite_recno;
	}

	public int can_hire_caravan()
	{
		Nation nation = NationArray[nation_recno];

		if (nation.cash < UnitRes[UnitConstants.UNIT_CARAVAN].build_cost)
			return 0;

		int supportedCaravan = nation.total_population / GameConstants.POPULATION_PER_CARAVAN;
		int caravanCount = UnitRes[UnitConstants.UNIT_CARAVAN].nation_unit_count_array[nation_recno - 1];

		return supportedCaravan > caravanCount ? supportedCaravan - caravanCount : 0;
	}

	public void set_goods(bool isRaw, int goodsId, int position)
	{
		MarketGoods marketGoods = market_goods_array[position];
		if (isRaw)
		{
			if (marketGoods.raw_id != 0)
				market_raw_array[marketGoods.raw_id - 1] = null;
			else if (marketGoods.product_raw_id != 0)
				market_product_array[marketGoods.product_raw_id - 1] = null;

			marketGoods.raw_id = goodsId;
			marketGoods.product_raw_id = 0;
			market_raw_array[goodsId - 1] = marketGoods;
		}
		else
		{
			if (marketGoods.product_raw_id != 0)
				market_product_array[marketGoods.product_raw_id - 1] = null;
			else if (marketGoods.raw_id != 0)
				market_raw_array[marketGoods.raw_id - 1] = null;

			marketGoods.raw_id = 0;
			marketGoods.product_raw_id = goodsId;
			market_product_array[goodsId - 1] = marketGoods;
		}

		if (FirmArray.selected_recno == firm_recno)
			Info.disp();
	}

	public void clear_market_goods(int position)
	{
		MarketGoods marketGoods = market_goods_array[position - 1];

		marketGoods.stock_qty = 0.0;

		if (marketGoods.raw_id != 0)
		{
			market_raw_array[marketGoods.raw_id - 1] = null;
			marketGoods.raw_id = 0;
		}
		else
		{
			market_product_array[marketGoods.product_raw_id - 1] = null;
			marketGoods.product_raw_id = 0;
		}
	}

	public bool is_market_linked_to_town(bool ownBaseTownOnly = false)
	{
		for (int i = 0; i < linked_town_array.Count; i++)
		{
			if (linked_town_enable_array[i] != InternalConstants.LINK_EE)
				continue;

			if (ownBaseTownOnly)
			{
				Town town = TownArray[linked_town_array[i]];

				if (town.NationId == nation_recno && town.IsBaseTown)
					return true;
			}
			else
			{
				return true;
			}
		}

		return false;
	}

	public override void process_ai() // ai process entry point
	{
		//---- think about deleting this firm ----//

		if (Info.TotalDays % 30 == firm_recno % 30)
		{
			if (think_del())
				return;
		}

		//----- think about demand trade treaty -----//

		if (Info.TotalDays % 30 == firm_recno % 30)
			think_demand_trade_treaty();

		//----- think about building a factory next to this market ----//

		if (Info.TotalDays % 60 == firm_recno % 60)
			think_market_build_factory();

		//TODO think_export_product();

		//-------- think about new trading routes --------//

		// don't new imports until it's 60 days after the last one was imported
		if (Info.game_date < last_import_new_goods_date.AddDays(60.0))
			return;

		if (can_hire_caravan() > 0)
		{
			Nation ownNation = NationArray[nation_recno];

			int thinkInterval = 10 + (100 - ownNation.pref_trading_tendency) / 5; // think once every 10 to 30 days

			if (is_market_linked_to_town())
			{
				if (Info.TotalDays % thinkInterval == firm_recno % thinkInterval)
					think_import_new_product();

				if (Info.TotalDays % 60 == (firm_recno + 20) % 60)
				{
					//------------------------------------------------------//
					// Don't think about increaseing existing product supply
					// if we have just import a new goods, it takes time
					// to transport and pile up goods.
					//------------------------------------------------------//

					// only think increase existing supply 180 days after importing a new one
					if (last_import_new_goods_date == default || Info.game_date > last_import_new_goods_date.AddDays(180.0))
					{
						think_increase_existing_product_supply();
					}
				}
			}

			if (Info.TotalDays % thinkInterval == firm_recno % thinkInterval)
				think_export_product();
		}
	}

	public bool is_raw_market()
	{
		return restock_type == RESTOCK_RAW || restock_type == RESTOCK_ANY;
	}

	public bool is_retail_market()
	{
		return restock_type == RESTOCK_PRODUCT || restock_type == RESTOCK_ANY;
	}

	public void switch_restock()
	{
		if (++restock_type > RESTOCK_NONE)
			restock_type = RESTOCK_ANY;
		if (FirmArray.selected_recno == firm_recno)
			Info.disp();
	}

	private void input_goods(int maxInputQty)
	{
		//------ scan for a firm to input raw materials --------//

		Nation nation = NationArray[nation_recno];
		bool[] is_inputing_array = new bool[GameConstants.MAX_MARKET_GOODS];
		int queued_firm_recno = 0;

		for (int t = 0; t < linked_firm_array.Count; t++)
		{
			if (linked_firm_enable_array[t] != InternalConstants.LINK_EE)
				continue;

			Firm firm = FirmArray[linked_firm_array[t]];

			//----------- check if the firm is a mine ----------//

			if (firm.firm_id != FIRM_MINE && firm.firm_id != FIRM_FACTORY)
				continue;

			//--------- if it's a mine ------------//

			if (firm.firm_id == FIRM_MINE && is_raw_market())
			{
				FirmMine firmMine = (FirmMine)firm;

				if (firmMine.raw_id != 0)
				{
					int i;
					for (i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
					{
						MarketGoods marketGoods = market_goods_array[i];

						//--- only assign a slot to the product if it comes from a firm of our own ---//

						if (marketGoods.raw_id == firmMine.raw_id)
						{
							is_inputing_array[i] = true;

							if (firmMine.next_output_firm_recno == firm_recno &&
							    firmMine.stock_qty > 0 && marketGoods.stock_qty < max_stock_qty)
							{
								double inputQty = Math.Min(firmMine.stock_qty, maxInputQty);
								inputQty = Math.Min(inputQty, max_stock_qty - marketGoods.stock_qty);

								firmMine.stock_qty -= inputQty;
								marketGoods.stock_qty += inputQty;
								marketGoods.cur_month_supply += inputQty;

								if (firm.nation_recno != nation_recno)
									nation.import_goods(NationBase.IMPORT_RAW,
										firm.nation_recno, inputQty * GameConstants.RAW_PRICE);
							}
							else if (Convert.ToInt32(marketGoods.stock_qty) == Convert.ToInt32(max_stock_qty))
							{
								// add it so the other functions can know that this market has direct supply links
								marketGoods.cur_month_supply++;
							}

							break;
						}
					}

					//----- no matched slot for this goods -----//

					if (i == GameConstants.MAX_MARKET_GOODS && firmMine.stock_qty > 0 && queued_firm_recno == 0)
						queued_firm_recno = firm.firm_recno;
				}
			}

			//--------- if it's a factory ------------//

			else if (firm.firm_id == FIRM_FACTORY && is_retail_market())
			{
				FirmFactory firmFactory = (FirmFactory)firm;

				if (firmFactory.product_raw_id != 0)
				{
					int i;
					for (i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
					{
						MarketGoods marketGoods = market_goods_array[i];

						if (marketGoods.product_raw_id == firmFactory.product_raw_id)
						{
							is_inputing_array[i] = true;

							if (firmFactory.next_output_firm_recno == firm_recno &&
							    firmFactory.stock_qty > 0 && marketGoods.stock_qty < max_stock_qty)
							{
								double inputQty = Math.Min(firmFactory.stock_qty, maxInputQty);
								inputQty = Math.Min(inputQty, max_stock_qty - marketGoods.stock_qty);

								firmFactory.stock_qty -= inputQty;
								marketGoods.stock_qty += inputQty;
								marketGoods.cur_month_supply += inputQty;

								if (firm.nation_recno != nation_recno)
									nation.import_goods(NationBase.IMPORT_PRODUCT, firm.nation_recno,
										inputQty * GameConstants.PRODUCT_PRICE);
							}
							else if (Convert.ToInt32(marketGoods.stock_qty) == Convert.ToInt32(max_stock_qty))
							{
								// add it so the other functions can know that this market has direct supply links
								marketGoods.cur_month_supply++;
							}

							break;
						}
					}

					//----- no matched slot for this goods -----//

					if (i == GameConstants.MAX_MARKET_GOODS && firmFactory.stock_qty > 0 && queued_firm_recno == 0)
						queued_firm_recno = firm.firm_recno;
				}
			}
		}

		//---- if there are any empty slots for new goods -----//

		if (queued_firm_recno > 0)
		{
			Firm firm = FirmArray[queued_firm_recno];

			for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
			{
				MarketGoods marketGoods = market_goods_array[i];

				if (!is_inputing_array[i] && marketGoods.stock_qty == 0)
				{
					if (firm.firm_id == FIRM_MINE && is_raw_market())
					{
						set_goods(true, ((FirmMine)firm).raw_id, i);
						break;
					}

					if (firm.firm_id == FIRM_FACTORY && is_retail_market())
					{
						set_goods(false, ((FirmFactory)firm).product_raw_id, i);
						break;
					}
				}
			}
		}
	}

	private void set_next_output_firm()
	{
		for (int i = 0; i < linked_firm_array.Count; i++) // MAX tries
		{
			if (++next_output_link_id > linked_firm_array.Count) // next firm in the link
				next_output_link_id = 1;

			if (linked_firm_enable_array[next_output_link_id - 1] == InternalConstants.LINK_EE)
			{
				int firmRecno = linked_firm_array[next_output_link_id - 1];
				int firmId = FirmArray[firmRecno].firm_id;

				if (firmId == FIRM_FACTORY)
				{
					next_output_firm_recno = firmRecno;
					return;
				}
			}
		}

		next_output_firm_recno = 0; // this mine has no linked output firms
	}

	private void update_trade_link()
	{
		Nation ownNation = NationArray[nation_recno];

		//------ update links to harbors -----//

		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			Firm firm = FirmArray[linked_firm_array[i]];

			if (firm.firm_id != FIRM_HARBOR)
				continue;

			bool tradeTreaty = ownNation.get_relation(firm.nation_recno).trade_treaty ||
			                   firm.nation_recno == nation_recno;

			if (linked_firm_enable_array[i] != (tradeTreaty ? InternalConstants.LINK_EE : InternalConstants.LINK_DD))
				toggle_firm_link(i + 1, tradeTreaty, InternalConstants.COMMAND_AUTO, 1); // 1-toggle both side
		}

		//------ update links to towns -----//

		for (int i = 0; i < linked_town_array.Count; i++)
		{
			Town town = TownArray[linked_town_array[i]];

			if (town.NationId == 0)
				continue;

			bool tradeTreaty = ownNation.get_relation(town.NationId).trade_treaty ||
			                   town.NationId == nation_recno;

			if (linked_town_enable_array[i] != (tradeTreaty ? InternalConstants.LINK_EE : InternalConstants.LINK_DD))
				toggle_town_link(i + 1, tradeTreaty, InternalConstants.COMMAND_AUTO, 1); // 1-toggle both side
		}
	}

	private void free_unused_slot()
	{
		for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = market_goods_array[i];
			if (marketGoods.product_raw_id != 0 || marketGoods.raw_id != 0)
			{
				if (marketGoods.sales_365days() == 0 && marketGoods.supply_30days() == 0 && marketGoods.stock_qty == 0)
				{
					clear_market_goods(i + 1);
				}
			}
		}
	}

	//------------------ AI actions --------------------//

	private bool think_del()
	{
		//if (linked_town_count > 0)
		//return 0;

		//MarketGoods* marketGoods = market_goods_array;
		//for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++, marketGoods++)
		//{
		//if (marketGoods.stock_qty > 0)
		//return 0;
		//}

		if (linked_town_array.Count > 0)
		{
			no_linked_town_since_date = default; // reset it
			return false;
		}
		else
		{
			no_linked_town_since_date = Info.game_date;
		}

		//---- don't delete it if there are still signiciant stockhere ---//

		if (stock_value_index() >= 10)
		{
			Nation ownNation = NationArray[nation_recno];

			//--- if the market has been sitting idle for too long, delete it ---//

			if (Info.game_date < no_linked_town_since_date.AddDays(180.0 + 180.0 * ownNation.pref_trading_tendency / 100.0))
			{
				return false;
			}
		}

		//------------------------------------------------//

		ai_del_firm();

		return true;
	}

	public override void ai_update_link_status()
	{
		//---- make sure the restocking type is defined ----//

		if (restock_type == RESTOCK_ANY)
		{
			bool hasRawResource = false;
			for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
			{
				MarketGoods marketGoods = market_goods_array[i];
				if (marketGoods.raw_id != 0)
				{
					hasRawResource = true;
					break;
				}
			}

			if (hasRawResource)
				restock_type = RESTOCK_RAW;
			else
				restock_type = RESTOCK_PRODUCT;
		}

		//---- consider enabling/disabling links to firms ----//

		bool rc;

		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			Firm firm = FirmArray[linked_firm_array[i]];

			//-------- check product type ----------//

			if (is_retail_market())
				rc = firm.firm_id == FIRM_FACTORY;
			else
				rc = firm.firm_id == FIRM_MINE || firm.firm_id == FIRM_FACTORY;

			toggle_firm_link(i + 1, rc, InternalConstants.COMMAND_AI); // enable the link
		}

		//----- always enable links to towns as there is no downsides for selling goods to the villagers ----//

		for (int i = 0; i < linked_town_array.Count; i++)
		{
			toggle_town_link(i + 1, true, InternalConstants.COMMAND_AI); // enable the link
		}
	}

	private bool think_import_new_product()
	{
		//---- check if the market place has free space for new supply ----//

		int emptySlot = 0;

		for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = market_goods_array[i];
			if (marketGoods.product_raw_id == 0 && marketGoods.raw_id == 0)
				emptySlot++;
		}

		if (emptySlot == 0)
			return false;

		//--- update what products are needed for this market place ---//

		// the total population in the towns linked to the market that needs the supply of the product
		int[] needProductSupplyPop = new int[GameConstants.MAX_PRODUCT];
		Nation nation = NationArray[nation_recno];

		for (int i = 0; i < linked_town_array.Count; i++)
		{
			if (linked_town_enable_array[i] != InternalConstants.LINK_EE)
				continue;

			Town town = TownArray[linked_town_array[i]];

			if (town.RegionId != region_id)
				continue;

			if (!town.IsBaseTown) // don't import if it isn't a base town
				continue;

			//------------------------------------------------//
			//
			// Only if the population of the town is equal or
			// larger than minTradePop, the AI will try to do trade.
			// The minTradePop is between 10 to 20 depending on the
			// pref_trading_tendency.
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

		if (is_retail_market())
		{
			for (int productId = 1; productId <= GameConstants.MAX_PRODUCT; productId++)
			{
				// if  market is empty, try to import some goods
				if (needProductSupplyPop[productId - 1] >= minTradePop || emptySlot == GameConstants.MAX_MARKET_GOODS)
				{
					if (think_import_specific_product(productId))
					{
						last_import_new_goods_date = Info.game_date;
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

		if (is_raw_market() && is_market_linked_to_town(true)) // 1-only count towns that are our own and are base towns
		{
			// if there is a shortage of caravan supplies, use it for transporting finished products instead of raw materials
			if (!no_neighbor_space && nation.total_jobless_population >= MAX_WORKER * 2 && can_hire_caravan() >= 2)
			{
				if (nation.can_ai_build(FIRM_FACTORY))
				{
					for (int productId = 1; productId <= GameConstants.MAX_PRODUCT; productId++)
					{
						if (needProductSupplyPop[productId - 1] >= minTradePop)
						{
							if (think_mft_specific_product(productId))
							{
								last_import_new_goods_date = Info.game_date;
								return true;
							}
						}
					}
				}
			}
		}

		return false;
	}

	private bool think_increase_existing_product_supply()
	{
		for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = market_goods_array[i];
			if (marketGoods.product_raw_id != 0)
			{
				// the supply falls behind the demand by at least 20%
				if (marketGoods.stock_qty < GameConstants.MAX_MARKET_STOCK / 10.0 &&
				    marketGoods.month_demand * 0.8 > marketGoods.supply_30days())
				{
					if (think_import_specific_product(marketGoods.product_raw_id))
						return true;
				}
			}
		}

		return false;
	}

	private bool think_import_specific_product(int productId)
	{
		Nation nation = NationArray[nation_recno];
		Firm bestFirm = null;
		int bestRating = 0;

		RawInfo rawInfo = RawRes[productId];

		for (int i = rawInfo.product_supply_firm_array.Count - 1; i >= 0; i--)
		{
			int firmRecno = rawInfo.get_product_supply_firm(i);

			if (FirmArray.IsDeleted(firmRecno) || firmRecno == firm_recno)
				continue;

			//-- if there is already a caravan travelling between two points --//

			Firm firm = FirmArray[firmRecno];

			if (firm.region_id != region_id)
				continue;

			//-----------------------------------------//
			// The rating of a supply is determined by:
			//	- distance
			// - supply
			// - nation relationship
			//-----------------------------------------//

			//------ determine the stock level of this supply ------//

			double stockLevel = 0.0;

			//---- think about inputing goods from this factory ----//

			if (firm.firm_id == FIRM_FACTORY)
			{
				if (firm.nation_recno != nation_recno) // can import goods from own factories only
					continue;

				FirmFactory firmFactory = (FirmFactory)firm;

				if (firmFactory.product_raw_id == productId)
					stockLevel = 100.0 * firmFactory.stock_qty / firmFactory.max_stock_qty;
			}

			//---- think about inputting goods from this market ----//

			else if (firm.firm_id == FIRM_MARKET)
			{
				//--- if this is a foreign sale market, don't import from it (e.g. Nation A's market built near Nation B's village -----//

				if (firm.nation_recno != nation_recno) // if this is not our market
				{
					int j;
					for (j = firm.linked_town_array.Count - 1; j >= 0; j--)
					{
						Town town = TownArray[firm.linked_town_array[j]];

						if (town.NationId == firm.nation_recno)
							break;
					}

					if (j < 0) // if this market is not linked to its own town (then it must be a foreign market)
						continue;
				}

				//-- only either from own market place or from nations that trade with you --//

				if (!NationArray[firm.nation_recno].get_relation(nation_recno).trade_treaty)
					continue;

				MarketGoods marketGoods = ((FirmMarket)firm).market_product_array[productId - 1];

				//--- if this market has the supply of this goods ----//

				if (marketGoods != null && marketGoods.supply_30days() > 0)
				{
					stockLevel = 100.0 * marketGoods.stock_qty / GameConstants.MAX_MARKET_STOCK;
				}
			}

			//----------------------------------------------//

			int curRating;
			if (firm.nation_recno == nation_recno)
			{
				if (stockLevel < 10.0) // for our own market, the stock level requirement is lower
					continue;

				curRating = 50;
			}
			else
			{
				if (stockLevel < 20.0) // for other player's market, only import when the stock level is high enough
					continue;

				curRating = nation.get_relation_status(firm.nation_recno) * 5;
			}

			//---- calculate the current overall rating ----//

			curRating += Convert.ToInt32(stockLevel / 2.0) +
			             World.distance_rating(center_x, center_y, firm.center_x, firm.center_y);

			//----------- compare ratings -------------//

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestFirm = firm;
			}
		}

		//---- if a suitable supplier is found -----//

		if (bestFirm != null)
			return ai_create_new_trade(bestFirm, 0, TradeStop.PICK_UP_PRODUCT_FIRST + productId - 1);

		return false;
	}

	private bool think_mft_specific_product(int rawId)
	{
		Firm bestFirm = null;
		int bestRating = 0;
		Nation nation = NationArray[nation_recno];

		RawInfo rawInfo = RawRes[rawId];

		for (int i = rawInfo.raw_supply_firm_array.Count - 1; i >= 0; i--)
		{
			int firmRecno = rawInfo.get_raw_supply_firm(i);

			if (FirmArray.IsDeleted(firmRecno) || firmRecno == firm_recno)
				continue;

			//-- if there is already a caravan travelling between two points --//

			Firm firm = FirmArray[firmRecno];

			if (firm.region_id != region_id)
				continue;

			//-- if this is our own supply, don't import the raw material, but import the finished goods instead. --//

			if (firm.nation_recno == nation_recno)
				continue;

			//-----------------------------------------//
			// The rating of a supply is determined by:
			//	- distance
			// - supply
			// - nation relationship
			//-----------------------------------------//

			//------ determine the stock level of this supply ------//

			double stockLevel = 0.0;

			if (firm.firm_id == FIRM_MARKET)
			{
				//-- only either from own market place or from nations that trade with you --//

				if (!NationArray[firm.nation_recno].get_relation(nation_recno).trade_treaty)
					continue;

				//----- check if this market is linked to any mines directly ----//

				int j;
				for (j = firm.linked_firm_array.Count - 1; j >= 0; j--)
				{
					Firm linkedFirm = FirmArray[firm.linked_firm_array[j]];

					if (linkedFirm.firm_id == FIRM_MINE && linkedFirm.nation_recno == firm.nation_recno)
					{
						if (((FirmMine)linkedFirm).raw_id == rawId)
							break;
					}
				}

				if (j < 0) // this market does not have any direct supplies, so don't pick up goods from it 
					continue;

				//---------------------------------------------------------------//


				for (j = 0; j < GameConstants.MAX_MARKET_GOODS; j++)
				{
					MarketGoods marketGoods = ((FirmMarket)firm).market_goods_array[j];
					if (marketGoods.stock_qty > GameConstants.MAX_MARKET_STOCK / 5)
					{
						if (marketGoods.raw_id == rawId)
							stockLevel = 100 * marketGoods.stock_qty / GameConstants.MAX_MARKET_STOCK;
					}
				}
			}

			if (stockLevel < 50.0) // if the stock is too low, don't consider it
				continue;

			//---- calculate the current overall rating ----//

			NationRelation nationRelation = nation.get_relation(firm.nation_recno);

			int curRating = Convert.ToInt32(stockLevel) - 100 * Misc.points_distance(center_x, center_y,
				firm.center_x, firm.center_y) /GameConstants.MapSize;

			if (firm.nation_recno == nation_recno)
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

		if (!ai_create_new_trade(bestFirm, 0, TradeStop.PICK_UP_RAW_FIRST + rawId - 1))
			return false;

		return true;
	}

	private bool think_export_product()
	{
		//--- first check if there is any excessive supply for export ---//

		int exportProductId = 0;

		for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = market_goods_array[i];
			if (marketGoods.product_raw_id != 0)
			{
				// the supply is at least double of the demand
				if (marketGoods.stock_qty > GameConstants.MAX_MARKET_STOCK * 3.0 / 4.0 &&
				    marketGoods.month_demand < marketGoods.supply_30days() / 2)
				{
					exportProductId = marketGoods.product_raw_id;
					break;
				}
			}
		}

		if (exportProductId == 0)
			return false;

		//----- locate for towns that do not have the supply of the product ----//

		Nation nation = NationArray[nation_recno];

		foreach (Town town in TownArray)
		{
			// 10 to 20 as the minimum population for considering trade
			if (town.Population < 20 - (10 * nation.pref_trading_tendency / 100))
				continue;

			// if the town already has the supply of product, return now
			if (town.HasProductSupply[exportProductId - 1])
				continue;

			if (town.RegionId != region_id)
				continue;

			if (town.NoNeighborSpace) // if there is no space in the neighbor area for building a new firm.
				continue;

			// don't consider if it is too far away
			if (Misc.rects_distance(loc_x1, loc_y1, loc_x2, loc_y2,
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

				if (town.AverageResistance(nation_recno) > GameConstants.INDEPENDENT_LINK_RESISTANCE)
					continue;
			}

			//----- think about building a new market to the town for exporting our goods -----//

			if (think_build_export_market(town.TownId))
				return true;
		}

		return false;
	}

	private bool think_build_export_market(int townRecno)
	{
		Town town = TownArray[townRecno];
		Nation nation = NationArray[nation_recno];

		//---- see if we already have a market linked to this town ----//

		for (int i = 0; i < town.LinkedFirms.Count; i++)
		{
			Firm firm = FirmArray[town.LinkedFirms[i]];

			if (firm.firm_id != FIRM_MARKET || firm.firm_recno == firm_recno)
				continue;

			//--- if we already have a market there, no need to build a new market ----//

			if (firm.nation_recno == nation_recno)
				return false;
		}

		//--- if there is no market place linked to this town, we can set up one ourselves ---//

		int buildXLoc, buildYLoc;

		if (!nation.find_best_firm_loc(FIRM_MARKET, town.LocX1, town.LocY1,
			    out buildXLoc, out buildYLoc))
		{
			town.NoNeighborSpace = true;
			return false;
		}

		nation.add_action(buildXLoc, buildYLoc, town.LocX1, town.LocY1, Nation.ACTION_AI_BUILD_FIRM, FIRM_MARKET);
		return true;
	}

	private void think_demand_trade_treaty()
	{
		Nation nation = NationArray[nation_recno];

		//----- demand towns to open up market ----//

		for (int i = 0; i < linked_town_array.Count; i++)
		{
			//----- if the link is not enabled -----//

			if (linked_town_enable_array[i] != InternalConstants.LINK_EE)
			{
				int townNationRecno = TownArray[linked_town_array[i]].NationId;

				if (townNationRecno != 0)
					nation.get_relation(townNationRecno).ai_demand_trade_treaty++;
			}
		}
	}

	private void think_market_build_factory()
	{
		if (no_neighbor_space) // if there is no space in the neighbor area for building a new firm.
			return;

		//---- think about building factories to manufacture goods using raw materials in the market place ---//

		// always set it to 2, so think_build_factory() will start to build a market as soon as there is a need
		ai_should_build_factory_count = 2;

		for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
		{
			MarketGoods marketGoods = market_goods_array[i];
			if (marketGoods.raw_id == 0)
				continue;

			if (marketGoods.stock_qty < 250) // only when the stock is >= 250
				continue;

			//----- check if the raw materials are from a local mine, if so don't build a factory,
			//we only build a factory to manufacture goods using raw materials from a remote town.

			int j;
			for (j = 0; j < linked_firm_array.Count; j++)
			{
				Firm firm = FirmArray[linked_firm_array[j]];

				if (firm.firm_id == FIRM_MINE && firm.nation_recno == nation_recno &&
				    ((FirmMine)firm).raw_id == marketGoods.raw_id)
				{
					break;
				}
			}

			if (j < linked_firm_array.Count) // if this raw material is from a local mine
				continue;

			//-------------------------------------------//

			if (think_build_factory(marketGoods.raw_id))
				return;
		}
	}

	private bool ai_create_new_trade(Firm firm, int stop1PickUpType, int stop2PickUpType)
	{
		//---- see if there is already a caravan moving along the route -----//

		Nation ownNation = NationArray[nation_recno];
		int caravanInRouteCount = 0;
		int stop1Id, stop2Id;
		UnitCaravan unitCaravan;

		for (int i = ownNation.ai_caravan_array.Count - 1; i >= 0; i--)
		{
			unitCaravan = (UnitCaravan)UnitArray[ownNation.ai_caravan_array[i]];

			if (unitCaravan.stop_defined_num < 2)
				continue;

			if (unitCaravan.stop_array[0].firm_recno == firm_recno &&
			    unitCaravan.stop_array[1].firm_recno == firm.firm_recno)
			{
				stop1Id = 1;
				stop2Id = 2;
			}
			else if (unitCaravan.stop_array[1].firm_recno == firm_recno &&
			         unitCaravan.stop_array[0].firm_recno == firm.firm_recno)
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
				if (unitCaravan.is_visible()) // can't set stop when the caravan is in a firm
					unitCaravan.set_stop_pick_up(stop1Id, stop1PickUpType, InternalConstants.COMMAND_AI);
				rc = true;
			}

			if (stop2PickUpType != 0 && !unitCaravan.has_pick_up_type(stop2Id, stop2PickUpType))
			{
				if (unitCaravan.is_visible()) // can't set stop when the caravan is in a firm
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

		int unitRecno = hire_caravan(InternalConstants.COMMAND_AI);

		if (unitRecno == 0)
			return false;

		//----------- set up the trade route ----------//

		unitCaravan = (UnitCaravan)UnitArray[unitRecno];

		unitCaravan.set_stop(2, firm.loc_x1, firm.loc_y1, InternalConstants.COMMAND_AI);

		unitCaravan.set_stop_pick_up(1, TradeStop.NO_PICK_UP, InternalConstants.COMMAND_AI);
		unitCaravan.set_stop_pick_up(2, TradeStop.NO_PICK_UP, InternalConstants.COMMAND_AI);

		if (stop1PickUpType != 0)
			unitCaravan.set_stop_pick_up(1, stop1PickUpType, InternalConstants.COMMAND_AI);

		if (stop2PickUpType != 0)
			unitCaravan.set_stop_pick_up(2, stop2PickUpType, InternalConstants.COMMAND_AI);

		return true;
	}
}