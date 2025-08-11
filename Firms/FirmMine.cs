using System;

namespace TenKingdoms;

public class FirmMine : Firm
{
    public string[] Mining_raw_msg = new string[] { "Mining Clay", "Mining Copper", "Mining Iron" };

    public int raw_id;
    public int site_recno;
    public double reserve_qty; // non-mined raw materials reserve
    public double stock_qty; // mined raw materials stock
    public double max_stock_qty;

    public int next_output_link_id;
    public int next_output_firm_recno;

    public double cur_month_production;
    public double last_month_production;

    private SiteArray SiteArray => Sys.Instance.SiteArray;

    public FirmMine()
    {
        FirmSkillId = Skill.SKILL_MINING;
    }

    protected override void InitDerived()
    {
        //---- scan for raw site in this firm's building location ----//

        Location location = scan_raw_site();

        if (location != null)
        {
            site_recno = location.SiteId();
            raw_id = SiteArray[site_recno].ObjectId;
            reserve_qty = SiteArray[site_recno].ReserveQty;

            SiteArray[site_recno].HasMine = true;
            SiteArray.UntappedRawCount--;
        }
        else
        {
            site_recno = 0;
            raw_id = 0;
            reserve_qty = 0.0;
        }

        stock_qty = 0.0;
        max_stock_qty = GameConstants.DEFAULT_MINE_MAX_STOCK_QTY;

        //-------- increase AI raw count --------//

        if (raw_id != 0)
            NationArray[NationId].raw_count_array[raw_id - 1]++;
    }

    protected override void DeinitDerived()
    {
        if (site_recno != 0)
        {
            SiteArray.UntappedRawCount++;

            Site site = SiteArray[site_recno];
            if (reserve_qty <= 0.0) // if the reserve has been used up
            {
                SiteArray.DeleteSite(site);
            }
            else // restore the site
            {
                site.ReserveQty = (int)reserve_qty;
                site.HasMine = false;
            }
        }

        //-------- decrease AI raw count --------//

        if (raw_id != 0)
            NationArray[NationId].raw_count_array[raw_id - 1]--;
    }

    public override void ChangeNation(int newNationRecno)
    {
        if (raw_id != 0)
        {
            NationArray[NationId].raw_count_array[raw_id - 1]--;
            NationArray[newNationRecno].raw_count_array[raw_id - 1]++;
        }

        //-------- change the nation of this firm now ----------//

        base.ChangeNation(newNationRecno);
    }

    public override void NextDay()
    {
        base.NextDay();

        //----------- update population -------------//

        RecruitWorker();

        //-------- train up the skill ------------//

        UpdateWorker();

        //---------------------------------------//

        // produce raw materials once every 3 days
        if (Info.TotalDays % GameConstants.PROCESS_GOODS_INTERVAL == FirmId % GameConstants.PROCESS_GOODS_INTERVAL)
        {
            produce_raw();
            set_next_output_firm(); // set next output firm
        }
    }

    public override void NextMonth()
    {
        last_month_production = cur_month_production;
        cur_month_production = 0.0;
    }

    public override void ProcessAI()
    {
        //---- if the reserve has exhaust ----//

        if (raw_id == 0 || reserve_qty == 0)
        {
            AIDelFirm();
            return;
        }

        //------- recruit workers ---------//

        if (Info.TotalDays % 15 == FirmId % 15)
        {
            if (Workers.Count < MAX_WORKER)
                AIRecruitWorker();
        }

        //---- think about building factory and market place to link to ----//

        bool rc = Info.TotalDays % 30 == FirmId % 30;

        // if the nation doesn't have any factories yet, give building factory a higher priority
        if (NationArray[NationId].ai_factory_array.Count == 0)
            rc = Info.TotalDays % 5 == FirmId % 5;

        if (rc)
        {
            if (!ThinkBuildFactory(raw_id))
                AiShouldBuildFactoryCount = 0; // reset the counter

            think_build_market(); // don't build it in FirmMine, when it builds a factory the factory will build a mine.
        }

        //---- think about ways to increase productivity ----//

        if (Info.TotalDays % 30 == FirmId % 30)
            think_inc_productivity();
    }

    public double production_30days()
    {
        return last_month_production * (30 - Info.game_day) / 30 + cur_month_production;
    }

    public override bool IsOperating()
    {
        return Productivity > 0 && reserve_qty > 0;
    }

    public override bool AIHasExcessWorker()
    {
        //--- if the actual production is lower than the productivity, than the firm must be under-capacity.

        if (Workers.Count > 4) // at least keep 4 workers
        {
            // take 25 days instead of 30 days so there will be small chance of errors.
            return stock_qty > max_stock_qty * 0.9 && production_30days() < Productivity * 25;
        }
        else if (Workers.Count > 1)
        {
            return reserve_qty < Workers.Count * 200;
        }
        else // don't leave if there is only one miner there
        {
            return false;
        }
    }

    private void produce_raw()
    {
        //----- if stock capacity reached or reserve exhausted -----//

        if (Convert.ToInt32(stock_qty) == Convert.ToInt32(max_stock_qty) || reserve_qty == 0)
            return;

        //------- calculate the productivity of the workers -----------//

        CalcProductivity();

        //-------- mine raw materials -------//

        double produceQty = 100.0 * Productivity / 100.0;

        produceQty = Math.Min(produceQty, reserve_qty);
        produceQty = Math.Min(produceQty, max_stock_qty - stock_qty);

        reserve_qty -= produceQty;
        if (ConfigAdv.mine_unlimited_reserve && reserve_qty < 1500)
            reserve_qty = 1500;

        stock_qty += produceQty;

        cur_month_production += produceQty;

        Site site = SiteArray[site_recno];
        site.ReserveQty = (int)reserve_qty; // update the reserve_qty in SiteArray

        //---- add news if run out of raw deposit ----//

        if (reserve_qty == 0)
        {
            SiteArray.UntappedRawCount++; // have to restore its first as del_site() will decrease uptapped_raw_count

            SiteArray.DeleteSite(site);
            site_recno = 0;

            if (NationId == NationArray.player_recno)
                NewsArray.raw_exhaust(raw_id, LocCenterX, LocCenterY);
        }
    }

    private Location scan_raw_site()
    {
        //---- scan for raw site in this firm's building location ----//

        for (int yLoc = LocY1; yLoc <= LocY2; yLoc++)
        {
            for (int xLoc = LocX1; xLoc <= LocX2; xLoc++)
            {
                Location location = World.GetLoc(xLoc, yLoc);

                if (location.HasSite() && SiteArray[location.SiteId()].SiteType == Site.SITE_RAW)
                {
                    return location;
                }
            }
        }

        return null;
    }

    private void set_next_output_firm()
    {
        for (int i = 0; i < LinkedFirms.Count; i++) // MAX tries
        {
            if (++next_output_link_id > LinkedFirms.Count) // next firm in the link
                next_output_link_id = 1;

            if (LinkedFirmsEnable[next_output_link_id - 1] == InternalConstants.LINK_EE)
            {
                int firmRecno = LinkedFirms[next_output_link_id - 1];
                int firmId = FirmArray[firmRecno].FirmType;

                if (firmId == FIRM_FACTORY || firmId == FIRM_MARKET)
                {
                    next_output_firm_recno = firmRecno;
                    return;
                }
            }
        }

        next_output_firm_recno = 0; // this mine has no linked output firms
    }

    //------------ AI actions ---------------//

    private bool think_build_market()
    {
        if (NoNeighborSpace) // if there is no space in the neighbor area for building a new firm.
            return false;

        Nation nation = NationArray[NationId];

        //-- only build a new market when the mine has a larger supply than the demands from factories and market places

        if (stock_qty < max_stock_qty * 0.2)
            return false;

        //--- check whether the AI can build a new firm next this firm ---//

        if (!nation.can_ai_build(FIRM_MARKET))
            return false;

        //-- only build one market place next to this mine, check if there is any existing one --//

        for (int i = 0; i < LinkedFirms.Count; i++)
        {
            Firm firm = FirmArray[LinkedFirms[i]];
            if (firm.FirmType != FIRM_MARKET)
                continue;

            //------ if this market is our own one ------//
            // if it already has a raw material market, then no need to build a new one
            FirmMarket firmMarket = (FirmMarket)firm;
            if (firmMarket.NationId == NationId && firmMarket.is_raw_market())
            {
                return false;
            }
        }

        //------ queue building a new market -------//

        int buildXLoc, buildYLoc;

        if (!nation.find_best_firm_loc(FIRM_MARKET, LocX1, LocY1, out buildXLoc, out buildYLoc))
        {
            NoNeighborSpace = true;
            return false;
        }

        nation.add_action(buildXLoc, buildYLoc, LocX1, LocY1, Nation.ACTION_AI_BUILD_FIRM, FIRM_MARKET);

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

        if (reserve_qty < GameConstants.MAX_RAW_RESERVE_QTY / 10.0)
            return false;

        //------ try to get skilled workers from inns and other firms ------//

        //	return think_hire_inn_unit();

        return false;
    }
    
    public override void DrawDetails(IRenderer renderer)
    {
        renderer.DrawMineDetails(this);
    }

    public override void HandleDetailsInput(IRenderer renderer)
    {
        renderer.HandleMineDetailsInput(this);
    }
}