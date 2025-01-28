using System;

namespace TenKingdoms;

public class FirmFactory : Firm
{
	public int product_raw_id; // the raw id. of the product

	public double stock_qty; // mined raw materials stock
	public double max_stock_qty;

	public double raw_stock_qty; // raw materials stock
	public double max_raw_stock_qty;

	public double cur_month_production;
	public double last_month_production;

	public int next_output_link_id;
	public int next_output_firm_recno;

	public FirmFactory()
	{
		firm_skill_id = Skill.SKILL_MFT;
		product_raw_id = 1;

		cur_month_production = 0.0;
		last_month_production = 0.0;

		stock_qty = 0.0;
		max_stock_qty = GameConstants.DEFAULT_FACTORY_MAX_STOCK_QTY;

		raw_stock_qty = 0.0;
		max_raw_stock_qty = GameConstants.DEFAULT_FACTORY_MAX_RAW_STOCK_QTY;

		next_output_link_id = 0;
		next_output_firm_recno = 0;
	}

	protected override void init_derived()
	{
		auto_set_product();
	}

	public override void next_day()
	{
		base.next_day();

		//----------- update population -------------//

		recruit_worker();

		//-------- train up the skill ------------//

		update_worker();

		//--------- daily manufacturing activities ---------//

		if (Info.TotalDays % GameConstants.PROCESS_GOODS_INTERVAL == firm_recno % GameConstants.PROCESS_GOODS_INTERVAL)
		{
			input_raw();
			production();
			set_next_output_firm();
		}
	}

	public override void next_month()
	{
		last_month_production = cur_month_production;
		cur_month_production = 0.0;
	}

	public void set_product(int rawId)
	{
		product_raw_id = rawId;
	}

	private void auto_set_product()
	{
		//---- automatically set the factory product type -----//

		int minDistance = Int32.MaxValue;

		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			Firm firm = FirmArray[linked_firm_array[i]];

			int firmDistance = Misc.points_distance(firm.center_x, firm.center_y, center_x, center_y);

			//----------- if the firm is a mine ----------//

			if (firm.firm_id == FIRM_MINE)
			{
				FirmMine firmMine = (FirmMine)firm;

				int rawId = firmMine.raw_id;

				if (rawId == 0)
					continue;

				//--- if this mine hasn't been used by any factories yet, then select it ---//

				int j;
				for (j = firm.linked_firm_array.Count - 1; j >= 0; j--)
				{
					if (firm.linked_firm_enable_array[j] == 0)
						continue;

					Firm otherFirm = FirmArray[firm.linked_firm_array[j]];

					if (otherFirm.firm_id == FIRM_FACTORY && ((FirmFactory)otherFirm).product_raw_id == rawId)
					{
						break;
					}

				}

				if (j < 0)
				{
					product_raw_id = rawId;
					return;
				}

				//--------------------------------//

				if (firmDistance < minDistance)
				{
					product_raw_id = firmMine.raw_id;
					minDistance = firmDistance;
				}
			}

			//----------- if the firm is a market place ----------//

			else if (firm.firm_id == FIRM_MARKET)
			{
				FirmMarket firmMarket = (FirmMarket)firm;

				int j;
				for (j = 0; j < GameConstants.MAX_MARKET_GOODS; j++)
				{
					int rawId = firmMarket.market_goods_array[j].raw_id;

					if (rawId == 0)
						continue;

					//--- if this raw material in this market hasn't been used by any factories yet, then select it ---//

					int k;
					for (k = firm.linked_firm_array.Count - 1; k >= 0; k--)
					{
						if (firm.linked_firm_enable_array[k] != InternalConstants.LINK_EE)
							continue;

						Firm otherFirm = FirmArray[firm.linked_firm_array[k]];

						if (otherFirm.firm_id == FIRM_FACTORY && ((FirmFactory)otherFirm).product_raw_id == rawId)
						{
							break;
						}
					}

					if (k < 0)
					{
						product_raw_id = rawId;
						return;
					}

					//-----------------------------------//

					if (firmDistance < minDistance)
					{
						product_raw_id = rawId;
						minDistance = firmDistance;
					}
				}
			}
		}
	}

	private void change_production()
	{
		//if( remote.is_enable() )
		//{
		//// packet structure : <firm recno> <product id>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_F_FACTORY_CHG_PROD, 2*sizeof(short) );
		//shortPtr[0] = firm_recno;
		//shortPtr[1] = product_raw_id >= MAX_PRODUCT ? 1 : product_raw_id + 1;
		//}
		//else
		//{
		// update RemoteMsg::factory_change_product
		if (++product_raw_id > GameConstants.MAX_PRODUCT)
			product_raw_id = 1;

		set_production(product_raw_id == GameConstants.MAX_PRODUCT ? 1 : product_raw_id + 1);
		//}
	}

	private void set_production(int newProductId)
	{
		product_raw_id = newProductId;

		stock_qty = 0.0;
		max_stock_qty = GameConstants.DEFAULT_FACTORY_MAX_STOCK_QTY;
		raw_stock_qty = 0.0;
		max_raw_stock_qty = GameConstants.DEFAULT_FACTORY_MAX_RAW_STOCK_QTY;
	}

	public double production_30days()
	{
		return last_month_production * (30 - Info.game_day) / 30 + cur_month_production;
	}

	public override bool is_operating()
	{
		return productivity > 0 && production_30days() > 0;
	}

	public override bool ai_has_excess_worker()
	{
		//--- if the actual production is lower than the productivity, than the firm must be under-capacity.

		if (workers.Count > 4) // at least keep 4 workers
		{
			// take 25 days instead of 30 days so there will be small chance of errors.
			return stock_qty > max_stock_qty * 0.9 && production_30days() < productivity * 25;
		}

		return false;
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

				if (firmId == FIRM_MARKET)
				{
					next_output_firm_recno = firmRecno;
					return;
				}
			}
		}

		next_output_firm_recno = 0; // this mine has no linked output firms
	}

	private void production()
	{
		//----- if stock capacity reached or reserve exhausted -----//

		if (stock_qty == max_stock_qty)
			return;

		//------- calculate the productivity of the workers -----------//

		calc_productivity();

		//------- generate revenue for the nation --------//

		double produceQty = 20.0 * productivity / 100.0;

		produceQty = Math.Min(produceQty, max_stock_qty - stock_qty);

		manufacture(produceQty);
	}

	private void input_raw()
	{
		//------ scan for a firm to input raw materials --------//

		Nation nation = NationArray[nation_recno];

		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			if (linked_firm_enable_array[i] != InternalConstants.LINK_EE)
				continue;

			Firm firm = FirmArray[linked_firm_array[i]];

			//----------- check if the firm is a mine ----------//

			if (firm.firm_id != FIRM_MINE && firm.firm_id != FIRM_MARKET)
				continue;

			//--------- if the firm is a mine ------------//

			if (firm.firm_id == FIRM_MINE)
			{
				FirmMine firmMine = (FirmMine)firm;

				if (firmMine.next_output_firm_recno == firm_recno &&
				    firmMine.raw_id == product_raw_id && firmMine.stock_qty > 0)
				{
					double inputQty = Math.Min(firmMine.stock_qty, max_raw_stock_qty - raw_stock_qty);

					if (firmMine.nation_recno != nation_recno) // make sure it has the cash to pay for the raw materials
						inputQty = Math.Min(inputQty, nation.cash / GameConstants.RAW_PRICE);

					if (inputQty > 0)
					{
						firmMine.stock_qty -= inputQty;
						raw_stock_qty += inputQty;

						//---- import from other nation -----//

						if (firmMine.nation_recno != nation_recno)
							nation.import_goods(NationBase.IMPORT_RAW, firmMine.nation_recno,
								inputQty * GameConstants.RAW_PRICE);
					}
				}
			}

			//------- if the firm is a market place --------//

			if (firm.firm_id == FIRM_MARKET)
			{
				FirmMarket firmMarket = (FirmMarket)firm;

				if (firmMarket.next_output_firm_recno == firm_recno)
				{
					for (int j = 0; j < GameConstants.MAX_MARKET_GOODS; j++)
					{
						MarketGoods marketGoods = firmMarket.market_goods_array[j];
						if (marketGoods.raw_id == product_raw_id &&
						    marketGoods.stock_qty > 0)
						{
							double inputQty = Math.Min(marketGoods.stock_qty, max_raw_stock_qty - raw_stock_qty);

							// make sure it has the cash to pay for the raw materials
							if (firmMarket.nation_recno != nation_recno)
								inputQty = Math.Min(inputQty, nation.cash / GameConstants.RAW_PRICE);

							if (inputQty > 0)
							{
								marketGoods.stock_qty -= inputQty;
								raw_stock_qty += inputQty;

								//---- import from other nation -----//

								if (firmMarket.nation_recno != nation_recno)
									nation.import_goods(NationBase.IMPORT_RAW, firmMarket.nation_recno,
										inputQty * GameConstants.RAW_PRICE);
							}
						}
					}
				}
			}
		}
	}

	private void manufacture(double maxMftQty)
	{
		if (raw_stock_qty == 0)
			return;

		double inputQty = Math.Min(raw_stock_qty, maxMftQty);
		inputQty = Math.Min(inputQty, max_stock_qty - stock_qty);

		if (inputQty <= 0)
			return;

		raw_stock_qty -= inputQty;
		stock_qty += inputQty;
		cur_month_production += inputQty;
	}

	//--------------- AI actions ----------------//

	public override void process_ai()
	{
		if (Info.TotalDays % 15 == firm_recno % 15)
		{
			if (think_change_production())
				return;
		}

		//------- recruit workers ---------//

		if (Info.TotalDays % 15 == firm_recno % 15)
		{
			if (workers.Count < MAX_WORKER)
				ai_recruit_worker();
		}

		//---- think about building market place to link to ----//

		if (Info.TotalDays % 30 == firm_recno % 30)
			think_build_market();

		//---- think about ways to increase productivity ----//

		if (Info.TotalDays % 30 == firm_recno % 30)
			think_inc_productivity();
	}

	private bool think_build_market()
	{
		if (no_neighbor_space) // if there is no space in the neighbor area for building a new firm.
			return false;

		Nation nation = NationArray[nation_recno];

		//--- check whether the AI can build a new firm next this firm ---//

		if (!nation.can_ai_build(FIRM_MARKET))
			return false;

		//----------------------------------------------------//
		// If there is already a firm queued for building with
		// a building location that is within the effective range
		// of the this firm.
		//----------------------------------------------------//

		if (nation.is_build_action_exist(FIRM_MARKET, center_x, center_y))
			return false;

		//-- only build one market place next to this factory, check if there is any existing one --//

		foreach (int linkedFirmRecno in linked_firm_array)
		{
			Firm firm = FirmArray[linkedFirmRecno];

			if (firm.firm_id != FIRM_MARKET)
				continue;

			//----- if this is a retail market of our own ------//

			FirmMarket firmMarket = (FirmMarket)firm;
			if (firmMarket.nation_recno == nation_recno && firmMarket.is_retail_market())
			{
				return false;
			}
		}

		//------ queue building a new market -------//

		int buildXLoc, buildYLoc;

		if (!nation.find_best_firm_loc(FIRM_MARKET, loc_x1, loc_y1, out buildXLoc, out buildYLoc))
		{
			no_neighbor_space = true;
			return false;
		}

		nation.add_action(buildXLoc, buildYLoc, loc_x1, loc_y1,
			Nation.ACTION_AI_BUILD_FIRM, FIRM_MARKET);
		return true;
	}

	private bool think_inc_productivity()
	{
		//----------------------------------------------//
		//
		// If this factory has a medium to high level of stock,
		// this means the bottleneck is not at the factories,
		// building more factories won't solve the problem.
		//
		//----------------------------------------------//

		if (stock_qty > max_stock_qty * 0.1 && production_30days() > 30)
			return false;

		//----------------------------------------------//
		//
		// If this factory has a low level of raw materials,
		// this means the bottleneck is at the raw material supply.
		//
		//----------------------------------------------//

		if (raw_stock_qty < max_raw_stock_qty * 0.2)
			return false;

		return think_hire_inn_unit();
	}

	private bool think_change_production()
	{
		if (cur_month_production + last_month_production > 0 || raw_stock_qty > 0)
			return false;

		// only change production after the factory has been running for at least one month
		if (Info.game_date < setup_date.AddDays(30.0))
			return false;

		//-- only build one market place next to this factory, check if there is any existing one --//

		int bestRating = 0, bestProductId = 0;
		bool bestIsOwn = false;

		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			Firm firm = FirmArray[linked_firm_array[i]];

			if (firm.firm_id != FIRM_MINE && firm.firm_id != FIRM_MARKET)
				continue;

			//--- if this link to this market is disabled, enable it now ---//

			if (linked_firm_enable_array[i] == InternalConstants.LINK_DE)
				toggle_firm_link(i + 1, true, InternalConstants.COMMAND_AI);

			if (linked_firm_enable_array[i] != InternalConstants.LINK_EE)
				continue;

			int curRating = 0;

			//-------- if this is a mine ------//

			if (firm.firm_id == FIRM_MINE)
			{
				FirmMine firmMine = (FirmMine)firm;

				if (firmMine.stock_qty >= GameConstants.MIN_FACTORY_IMPORT_STOCK_QTY)
				{
					curRating = Convert.ToInt32(firmMine.stock_qty);

					if (curRating > bestRating)
					{
						// try to get raw materials from own firms first
						if (firm.nation_recno == nation_recno || !bestIsOwn)
						{
							bestRating = curRating;
							bestProductId = firmMine.raw_id;
							bestIsOwn = firm.nation_recno == nation_recno;
						}
					}
				}
			}

			//-------- if this is a market ------//

			else if (firm.firm_id == FIRM_MARKET)
			{
				FirmMarket firmMarket = (FirmMarket)firm;

				for (int j = 0; j < GameConstants.MAX_MARKET_GOODS; j++)
				{
					MarketGoods marketGoods = firmMarket.market_goods_array[j];
					if (marketGoods.raw_id != 0 && marketGoods.stock_qty >= GameConstants.MIN_FACTORY_IMPORT_STOCK_QTY)
					{
						curRating = Convert.ToInt32(marketGoods.stock_qty);

						if (curRating > bestRating)
						{
							// try to get raw materials from own firms first
							if (firm.nation_recno == nation_recno || !bestIsOwn)
							{
								bestRating = curRating;
								bestProductId = marketGoods.raw_id;
								bestIsOwn = firm.nation_recno == nation_recno;
							}
						}
					}
				}
			}
		}

		//------------------------------------//

		if (bestProductId != 0)
		{
			set_production(bestProductId);
			return true;
		}
		else
		{
			if (Info.game_date > setup_date.AddDays(60.0))
			{
				ai_del_firm(); // delete the firm if there is no raw materials available after it has been built for over 2 months
				return true;
			}
		}

		return false;
	}
}