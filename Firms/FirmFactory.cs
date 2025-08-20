using System;

namespace TenKingdoms;

public class FirmFactory : Firm
{
	public int ProductRawId { get; set; } // the raw id. of the product

	public double RawStockQty { get; set; } // raw materials stock
	public double MaxRawStockQty { get; private set; }

	public double StockQty { get; set; } // mined raw materials stock
	public double MaxStockQty { get; private set; }
	
	private bool IsManufacturing { get; set; }

	private int NextOutputLinkId { get; set; }
	public int NextOutputFirmId { get; private set; }

	private double CurMonthProduction { get; set; }
	private double LastMonthProduction { get; set; }

	public FirmFactory()
	{
		FirmSkillId = Skill.SKILL_MFT;
		ProductRawId = 1;

		StockQty = 0.0;
		MaxStockQty = GameConstants.FACTORY_MAX_STOCK_QTY;

		RawStockQty = 0.0;
		MaxRawStockQty = GameConstants.FACTORY_MAX_RAW_STOCK_QTY;
	}

	protected override void InitDerived()
	{
		//---- automatically set the factory product type -----//

		int minDistance = Int32.MaxValue;

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			Firm firm = FirmArray[LinkedFirms[i]];

			int firmDistance = Misc.points_distance(firm.LocCenterX, firm.LocCenterY, LocCenterX, LocCenterY);

			if (firm.FirmType == FIRM_MINE)
			{
				FirmMine firmMine = (FirmMine)firm;

				int rawId = firmMine.RawId;

				if (rawId == 0)
					continue;

				//--- if this mine hasn't been used by any factories yet, then select it ---//

				bool mineHasLinkedFactory = false;

				for (int j = 0; j < firm.LinkedFirms.Count; j++)
				{
					if (firm.LinkedFirmsEnable[j] == 0)
						continue;

					Firm otherFirm = FirmArray[firm.LinkedFirms[j]];
					if (otherFirm.FirmType == FIRM_FACTORY && ((FirmFactory)otherFirm).ProductRawId == rawId)
					{
						mineHasLinkedFactory = true;
						break;
					}

				}

				if (!mineHasLinkedFactory)
				{
					ProductRawId = rawId;
					return;
				}

				if (firmDistance < minDistance)
				{
					ProductRawId = firmMine.RawId;
					minDistance = firmDistance;
				}
			}

			if (firm.FirmType == FIRM_MARKET)
			{
				FirmMarket firmMarket = (FirmMarket)firm;

				for (int j = 0; j < GameConstants.MAX_MARKET_GOODS; j++)
				{
					int rawId = firmMarket.market_goods_array[j].RawId;

					if (rawId == 0)
						continue;

					//--- if this raw material in this market hasn't been used by any factories yet, then select it ---//

					bool marketHasLinkedFactory = false;

					for (int k = 0; k < firm.LinkedFirms.Count; k++)
					{
						if (firm.LinkedFirmsEnable[k] != InternalConstants.LINK_EE)
							continue;

						Firm otherFirm = FirmArray[firm.LinkedFirms[k]];
						if (otherFirm.FirmType == FIRM_FACTORY && ((FirmFactory)otherFirm).ProductRawId == rawId)
						{
							marketHasLinkedFactory = true;
							break;
						}
					}

					if (!marketHasLinkedFactory)
					{
						ProductRawId = rawId;
						return;
					}

					if (firmDistance < minDistance)
					{
						ProductRawId = rawId;
						minDistance = firmDistance;
					}
				}
			}
		}
	}

	public override void NextDay()
	{
		base.NextDay();

		RecruitWorker();

		UpdateWorker();

		//--------- daily manufacturing activities ---------//

		if (Info.TotalDays % GameConstants.PROCESS_GOODS_INTERVAL == FirmId % GameConstants.PROCESS_GOODS_INTERVAL)
		{
			InputRaw();
			Manufacture();
			SetNextOutputFirm();
		}
	}

	public override void NextMonth()
	{
		LastMonthProduction = CurMonthProduction;
		CurMonthProduction = 0.0;
	}

	private void InputRaw()
	{
		//------ scan for a firm to input raw materials --------//

		Nation nation = NationArray[NationId];

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			if (LinkedFirmsEnable[i] != InternalConstants.LINK_EE)
				continue;

			Firm firm = FirmArray[LinkedFirms[i]];

			if (firm.FirmType == FIRM_MINE)
			{
				FirmMine firmMine = (FirmMine)firm;

				if (firmMine.NextOutputFirmId == FirmId && firmMine.RawId == ProductRawId && firmMine.StockQty > 0.0)
				{
					double inputQty = Math.Min(firmMine.StockQty, MaxRawStockQty - RawStockQty);

					// make sure it has the cash to pay for the raw materials
					if (firmMine.NationId != NationId)
						inputQty = Math.Min(inputQty, nation.cash / GameConstants.RAW_PRICE);

					if (inputQty > 0.0)
					{
						firmMine.StockQty -= inputQty;
						RawStockQty += inputQty;

						//---- import from other nation -----//

						if (firmMine.NationId != NationId)
							nation.import_goods(NationBase.IMPORT_RAW, firmMine.NationId, inputQty * GameConstants.RAW_PRICE);
					}
				}
			}

			if (firm.FirmType == FIRM_MARKET)
			{
				FirmMarket firmMarket = (FirmMarket)firm;

				if (firmMarket.NextOutputFirmId == FirmId)
				{
					for (int j = 0; j < GameConstants.MAX_MARKET_GOODS; j++)
					{
						MarketGoods marketGoods = firmMarket.market_goods_array[j];
						if (marketGoods.RawId == ProductRawId && marketGoods.StockQty > 0.0)
						{
							double inputQty = Math.Min(marketGoods.StockQty, MaxRawStockQty - RawStockQty);

							// make sure it has the cash to pay for the raw materials
							if (firmMarket.NationId != NationId)
								inputQty = Math.Min(inputQty, nation.cash / GameConstants.RAW_PRICE);

							if (inputQty > 0.0)
							{
								marketGoods.StockQty -= inputQty;
								RawStockQty += inputQty;

								//---- import from other nation -----//

								if (firmMarket.NationId != NationId)
									nation.import_goods(NationBase.IMPORT_RAW, firmMarket.NationId, inputQty * GameConstants.RAW_PRICE);
							}
						}
					}
				}
			}
		}
	}

	private void Manufacture()
	{
		//----- if stock capacity reached or raw resource is not available -----//

		if (StockQty >= MaxStockQty || RawStockQty <= 0.0)
		{
			IsManufacturing = false;
			return;
		}

		//------- calculate the productivity of the workers -----------//

		CalcProductivity();

		//------- manufacture product --------//

		double produceQty = 20.0 * Productivity / 100.0;

		produceQty = Math.Min(produceQty, RawStockQty);
		produceQty = Math.Min(produceQty, MaxStockQty - StockQty);

		RawStockQty -= produceQty;
		StockQty += produceQty;
		CurMonthProduction += produceQty;
		IsManufacturing = produceQty > 0.0;
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

				if (FirmArray[firmId].FirmType == FIRM_MARKET)
				{
					NextOutputFirmId = firmId;
					return;
				}
			}
		}

		NextOutputFirmId = 0; // this mine has no linked output firms
	}
	
	private void ChangeProduction()
	{
		//if (remote.is_enable())
		//{
			//// packet structure : <firm recno> <product id>
			//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_F_FACTORY_CHG_PROD, 2*sizeof(short) );
			//shortPtr[0] = firm_recno;
			//shortPtr[1] = ProductRawId;
			//return;
		//}

		ProductRawId++;
		if (ProductRawId > GameConstants.MAX_PRODUCT)
			ProductRawId = 1;

		SetProduction(ProductRawId);
	}

	private void SetProduction(int newProductId)
	{
		ProductRawId = newProductId;

		StockQty = 0.0;
		MaxStockQty = GameConstants.FACTORY_MAX_STOCK_QTY;
		RawStockQty = 0.0;
		MaxRawStockQty = GameConstants.FACTORY_MAX_RAW_STOCK_QTY;
	}

	public double Production30Days()
	{
		return LastMonthProduction * (30 - Info.game_day) / 30.0 + CurMonthProduction;
	}

	public override bool IsOperating()
	{
		return Productivity > 0.0 && IsManufacturing;
	}

	public override void DrawDetails(IRenderer renderer)
	{
		renderer.DrawFactoryDetails(this);
	}

	public override void HandleDetailsInput(IRenderer renderer)
	{
		renderer.HandleFactoryDetailsInput(this);
	}
	
	#region Old AI Functions

	public override void ProcessAI()
	{
		if (Info.TotalDays % 15 == FirmId % 15)
		{
			if (ThinkChangeProduction())
				return;
		}

		//------- recruit workers ---------//

		if (Info.TotalDays % 15 == FirmId % 15)
		{
			if (Workers.Count < MAX_WORKER)
				AIRecruitWorker();
		}

		//---- think about building market place to link to ----//

		if (Info.TotalDays % 30 == FirmId % 30)
			ThinkBuildMarket();

		//---- think about ways to increase productivity ----//

		if (Info.TotalDays % 30 == FirmId % 30)
			ThinkIncProductivity();
	}

	private bool ThinkBuildMarket()
	{
		if (NoNeighborSpace) // if there is no space in the neighbor area for building a new firm.
			return false;

		Nation nation = NationArray[NationId];

		//--- check whether the AI can build a new firm next this firm ---//

		if (!nation.can_ai_build(FIRM_MARKET))
			return false;

		//----------------------------------------------------//
		// If there is already a firm queued for building with a building location
		// that is within the effective range of the this firm.
		//----------------------------------------------------//

		if (nation.is_build_action_exist(FIRM_MARKET, LocCenterX, LocCenterY))
			return false;

		//-- only build one market place next to this factory, check if there is any existing one --//

		foreach (int linkedFirmRecno in LinkedFirms)
		{
			Firm firm = FirmArray[linkedFirmRecno];

			if (firm.FirmType != FIRM_MARKET)
				continue;

			//----- if this is a retail market of our own ------//

			FirmMarket firmMarket = (FirmMarket)firm;
			if (firmMarket.NationId == NationId && firmMarket.IsRetailMarket())
			{
				return false;
			}
		}

		//------ queue building a new market -------//

		if (!nation.find_best_firm_loc(FIRM_MARKET, LocX1, LocY1, out int buildLocX, out int buildLocY))
		{
			NoNeighborSpace = true;
			return false;
		}

		nation.add_action(buildLocX, buildLocY, LocX1, LocY1, Nation.ACTION_AI_BUILD_FIRM, FIRM_MARKET);
		return true;
	}

	private bool ThinkIncProductivity()
	{
		//----------------------------------------------//
		//
		// If this factory has a medium to high level of stock,
		// this means the bottleneck is not at the factories,
		// building more factories won't solve the problem.
		//
		//----------------------------------------------//

		if (StockQty > MaxStockQty * 0.1 && Production30Days() > 30)
			return false;

		//----------------------------------------------//
		//
		// If this factory has a low level of raw materials,
		// this means the bottleneck is at the raw material supply.
		//
		//----------------------------------------------//

		if (RawStockQty < MaxRawStockQty * 0.2)
			return false;

		return ThinkHireInnUnit();
	}

	private bool ThinkChangeProduction()
	{
		if (CurMonthProduction + LastMonthProduction > 0.0 || RawStockQty > 0.0)
			return false;

		// only change production after the factory has been running for at least one month
		if (Info.game_date < SetupDate.AddDays(30.0))
			return false;

		//-- only build one market place next to this factory, check if there is any existing one --//

		int bestRating = 0, bestProductId = 0;
		bool bestIsOwn = false;

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			Firm firm = FirmArray[LinkedFirms[i]];

			if (firm.FirmType != FIRM_MINE && firm.FirmType != FIRM_MARKET)
				continue;

			//--- if this link to this market is disabled, enable it now ---//

			if (LinkedFirmsEnable[i] == InternalConstants.LINK_DE)
				ToggleFirmLink(i + 1, true, InternalConstants.COMMAND_AI);

			if (LinkedFirmsEnable[i] != InternalConstants.LINK_EE)
				continue;

			int curRating = 0;

			//-------- if this is a mine ------//

			if (firm.FirmType == FIRM_MINE)
			{
				FirmMine firmMine = (FirmMine)firm;

				if (firmMine.StockQty >= GameConstants.MIN_FACTORY_IMPORT_STOCK_QTY)
				{
					curRating = Convert.ToInt32(firmMine.StockQty);

					if (curRating > bestRating)
					{
						// try to get raw materials from own firms first
						if (firm.NationId == NationId || !bestIsOwn)
						{
							bestRating = curRating;
							bestProductId = firmMine.RawId;
							bestIsOwn = firm.NationId == NationId;
						}
					}
				}
			}

			//-------- if this is a market ------//

			if (firm.FirmType == FIRM_MARKET)
			{
				FirmMarket firmMarket = (FirmMarket)firm;

				for (int j = 0; j < GameConstants.MAX_MARKET_GOODS; j++)
				{
					MarketGoods marketGoods = firmMarket.market_goods_array[j];
					if (marketGoods.RawId != 0 && marketGoods.StockQty >= GameConstants.MIN_FACTORY_IMPORT_STOCK_QTY)
					{
						curRating = Convert.ToInt32(marketGoods.StockQty);

						if (curRating > bestRating)
						{
							// try to get raw materials from own firms first
							if (firm.NationId == NationId || !bestIsOwn)
							{
								bestRating = curRating;
								bestProductId = marketGoods.RawId;
								bestIsOwn = firm.NationId == NationId;
							}
						}
					}
				}
			}
		}

		if (bestProductId != 0)
		{
			SetProduction(bestProductId);
			return true;
		}

		if (Info.game_date > SetupDate.AddDays(60.0))
		{
			AIDelFirm(); // delete the firm if there is no raw materials available after it has been built for over 2 months
			return true;
		}

		return false;
	}
	
	public override bool AIHasExcessWorker()
	{
		//--- if the actual production is lower than the productivity, than the firm must be under-capacity.

		if (Workers.Count > 4) // at least keep 4 workers
		{
			// take 25 days instead of 30 days so there will be small chance of errors.
			return StockQty > MaxStockQty * 0.9 && Production30Days() < Productivity * 25;
		}

		return false;
	}

	#endregion
}