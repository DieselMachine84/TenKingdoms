using System;

namespace TenKingdoms;

public class FirmMine : Firm
{
    public int RawId { get; private set; }
    private int SiteId { get; set; }
    public double ReserveQty { get; private set; } // non-mined raw materials reserve
    public double StockQty { get; set; } // mined raw materials stock
    public double MaxStockQty { get; private set; }

    private int NextOutputLinkId { get; set; }
    public int NextOutputFirmId { get; private set; }

    private double CurMonthProduction { get; set; }
    private double LastMonthProduction { get; set; }

    private SiteArray SiteArray => Sys.Instance.SiteArray;

    public FirmMine()
    {
        FirmSkillId = Skill.SKILL_MINING;
        StockQty = 0.0;
        MaxStockQty = GameConstants.MINE_MAX_STOCK_QTY;
    }

    protected override void InitDerived()
    {
        //---- scan for raw site in this firm's building location ----//

        Location location = ScanRawSite();

        if (location != null)
        {
            SiteId = location.SiteId();
            RawId = SiteArray[SiteId].ObjectId;
            ReserveQty = SiteArray[SiteId].ReserveQty;

            SiteArray[SiteId].HasMine = true;
            SiteArray.UntappedRawCount--;
        }
        else
        {
            SiteId = 0;
            RawId = 0;
            ReserveQty = 0.0;
        }

        if (RawId != 0)
            NationArray[NationId].raw_count_array[RawId - 1]++;
    }

    protected override void DeinitDerived()
    {
        if (SiteId != 0)
        {
            SiteArray.UntappedRawCount++;

            Site site = SiteArray[SiteId];
            if (ReserveQty <= 0.0) // if the reserve has been used up
            {
                SiteArray.DeleteSite(site);
            }
            else // restore the site
            {
                site.ReserveQty = (int)ReserveQty;
                site.HasMine = false;
            }
        }

        if (RawId != 0)
            NationArray[NationId].raw_count_array[RawId - 1]--;
    }

    public override void NextDay()
    {
        base.NextDay();

        RecruitWorker();

        UpdateWorker();

        // produce raw materials once every 3 days
        if (Info.TotalDays % GameConstants.PROCESS_GOODS_INTERVAL == FirmId % GameConstants.PROCESS_GOODS_INTERVAL)
        {
            ProduceRaw();
            SetNextOutputFirm();
        }
    }

    public override void NextMonth()
    {
        LastMonthProduction = CurMonthProduction;
        CurMonthProduction = 0.0;
    }

    public override void ChangeNation(int newNationId)
    {
        if (RawId != 0)
        {
            NationArray[NationId].raw_count_array[RawId - 1]--;
            NationArray[newNationId].raw_count_array[RawId - 1]++;
        }

        base.ChangeNation(newNationId);
    }

    private Location ScanRawSite()
    {
        //---- scan for raw site in this firm's building location ----//

        for (int locY = LocY1; locY <= LocY2; locY++)
        {
            for (int locX = LocX1; locX <= LocX2; locX++)
            {
                Location location = World.GetLoc(locX, locY);

                if (location.HasSite() && SiteArray[location.SiteId()].SiteType == Site.SITE_RAW)
                {
                    return location;
                }
            }
        }

        return null;
    }
    
    public double Production30Days()
    {
        return LastMonthProduction * (30 - Info.game_day) / 30.0 + CurMonthProduction;
    }

    public override bool IsOperating()
    {
        return Productivity > 0.0 && ReserveQty > 0.0;
    }

    private void ProduceRaw()
    {
        //----- if stock capacity reached or reserve exhausted -----//

        if (StockQty >= MaxStockQty || ReserveQty <= 0.0)
            return;

        //------- calculate the productivity of the workers -----------//

        CalcProductivity();

        //-------- mine raw materials -------//

        double produceQty = Productivity;

        produceQty = Math.Min(produceQty, ReserveQty);
        produceQty = Math.Min(produceQty, MaxStockQty - StockQty);

        ReserveQty -= produceQty;
        if (ConfigAdv.mine_unlimited_reserve && ReserveQty < 1500)
            ReserveQty = 1500;

        StockQty += produceQty;
        CurMonthProduction += produceQty;

        Site site = SiteArray[SiteId];
        site.ReserveQty = (int)ReserveQty;

        //---- add news if run out of raw deposit ----//

        if (ReserveQty <= 0.0)
        {
            SiteArray.UntappedRawCount++; // have to restore its first as DeleteSite() will decrease UntappedRawCount

            SiteArray.DeleteSite(site);
            SiteId = 0;

            if (NationId == NationArray.player_recno)
                NewsArray.raw_exhaust(RawId, LocCenterX, LocCenterY);
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
                int firmType = FirmArray[firmId].FirmType;

                if (firmType == FIRM_FACTORY || firmType == FIRM_MARKET)
                {
                    NextOutputFirmId = firmId;
                    return;
                }
            }
        }

        NextOutputFirmId = 0; // this mine has no linked output firms
    }

    public override void DrawDetails(IRenderer renderer)
    {
        renderer.DrawMineDetails(this);
    }

    public override void HandleDetailsInput(IRenderer renderer)
    {
        renderer.HandleMineDetailsInput(this);
    }
    
    #region Old AI Functions

    public override void ProcessAI()
    {
        //---- if the reserve has exhaust ----//

        if (RawId == 0 || ReserveQty <= 0.0)
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
            if (!ThinkBuildFactory(RawId))
                AIShouldBuildFactoryCount = 0; // reset the counter

            ThinkBuildMarket(); // don't build it in FirmMine, when it builds a factory the factory will build a mine.
        }

        //---- think about ways to increase productivity ----//

        if (Info.TotalDays % 30 == FirmId % 30)
            ThinkIncProductivity();
    }

     private bool ThinkBuildMarket()
    {
        if (NoNeighborSpace) // if there is no space in the neighbor area for building a new firm.
            return false;

        Nation nation = NationArray[NationId];

        //-- only build a new market when the mine has a larger supply than the demands from factories and market places

        if (StockQty < MaxStockQty * 0.2)
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
            if (firmMarket.NationId == NationId && firmMarket.IsRawMarket())
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

        if (ReserveQty < GameConstants.MAX_RAW_RESERVE_QTY / 10.0)
            return false;

        //------ try to get skilled workers from inns and other firms ------//

        //return ThinkHireInnUnit();

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
        
        if (Workers.Count > 1)
        {
            return ReserveQty < Workers.Count * 200;
        }

        // don't leave if there is only one miner there
        return false;
    }
    
    #endregion
}