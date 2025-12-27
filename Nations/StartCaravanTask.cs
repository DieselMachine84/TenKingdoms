using System;

namespace TenKingdoms;

// Caravan should be started when
// 1. Need to deliver resource from our mine or our market or another kingdom market to our factory - TODO
// 2. Need to deliver product from our factory to our market
// 3. Need to import product from another kingdom market
// 4. Need to move products between our markets
// 5. Need to move products out from our market that should be sold - TODO

// Before hiring caravan check if there is idle caravan available
// Check if distance between caravan stops is not too far - should depend on pref
// Check if there are existing caravans on this route already - should depend on pref

public class StartCaravanTask : AITask
{
    private bool _shouldCancel;
    public int MineId { get; }
    public int FactoryId { get; }
    public int MarketId { get; }

    public StartCaravanTask(Nation nation, int mineId, int factoryId, int marketId) : base(nation)
    {
        MineId = mineId;
        FactoryId = factoryId;
        MarketId = marketId;
    }

    public override bool ShouldCancel()
    {
        if (_shouldCancel)
            return true;

        if (MineId != 0 && FirmIsDeletedOrChangedNation(MineId))
            return true;

        if (FactoryId != 0 && FirmIsDeletedOrChangedNation(FactoryId))
            return true;
        
        if (MarketId != 0 && FirmIsDeletedOrChangedNation(MarketId))
            return true;
        
        return false;
    }

    public override void Process()
    {
        if (MineId != 0)
            StartMineCaravan();
        
        if (FactoryId != 0)
            StartFactoryCaravan();
        
        if (MarketId != 0)
            StartForeignMarketCaravan();
    }

    private void StartMineCaravan()
    {
        FirmMine mine = (FirmMine)FirmArray[MineId];
        
        FirmFactory bestFactory = null;
        int bestRating = Int16.MaxValue;
        foreach (Firm otherFirm in FirmArray)
        {
            if (otherFirm.NationId != Nation.nation_recno)
                continue;

            //TODO other region
            if (otherFirm.RegionId != mine.RegionId)
                continue;

            if (otherFirm is FirmFactory otherFactory)
            {
                if (!otherFactory.UnderConstruction && otherFactory.ProductId == mine.RawId)
                {
                    int distance = Misc.FirmsDistance(mine, otherFactory);
                    int rating = distance + (int)otherFactory.StockQty / 5;
                    if (rating < bestRating)
                    {
                        bestFactory = otherFactory;
                        bestRating = rating;
                    }
                }
            }
        }
        
        if (bestFactory != null)
            StartCaravan(mine, bestFactory);
    }

    private void StartFactoryCaravan()
    {
        FirmFactory factory = (FirmFactory)FirmArray[FactoryId];

        FirmMarket bestMarket = null;
        int bestRating = Int16.MaxValue;
        foreach (Firm otherFirm in FirmArray)
        {
            if (otherFirm.NationId != Nation.nation_recno)
                continue;

            if (otherFirm.RegionId != factory.RegionId)
                continue;

            if (otherFirm is FirmMarket otherMarket)
            {
                if (!otherMarket.UnderConstruction && otherMarket.IsRetailMarket())
                {
                    int rating = Misc.FirmsDistance(factory, otherMarket);
                    if (rating < bestRating)
                    {
                        bestMarket = otherMarket;
                        bestRating = rating;
                    }
                }
            }
        }
        
        if (bestMarket != null)
            StartCaravan(factory, bestMarket);
    }

    private void StartForeignMarketCaravan()
    {
        FirmMarket foreignMarket = (FirmMarket)FirmArray[MarketId];
        
        FirmMarket bestMarket = null;
        int bestRating = Int16.MaxValue;
        foreach (Firm otherFirm in FirmArray)
        {
            if (otherFirm.NationId != Nation.nation_recno)
                continue;

            if (otherFirm.RegionId != foreignMarket.RegionId)
                continue;

            if (otherFirm is FirmMarket otherMarket)
            {
                if (!otherMarket.UnderConstruction && otherMarket.IsRetailMarket())
                {
                    int rating = Misc.FirmsDistance(foreignMarket, otherMarket);
                    if (rating < bestRating)
                    {
                        bestMarket = otherMarket;
                        bestRating = rating;
                    }
                }
            }
        }
        
        if (bestMarket != null)
            StartCaravan(foreignMarket, bestMarket);
    }

    private void StartCaravan(FirmMine mine, FirmFactory factory)
    {
        if (HasCaravan(mine, factory))
        {
            _shouldCancel = true;
            return;
        }
        
        FirmMarket bestMarket = null;
        int bestRating = Int16.MaxValue;
        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId != Nation.nation_recno)
                continue;
            
            //TODO other region
            if (firm.RegionId != mine.RegionId)
                continue;

            if (firm is FirmMarket market)
            {
                int rating = Misc.FirmsDistance(mine, market);
                if (rating < bestRating)
                {
                    bestMarket = market;
                    bestRating = rating;
                }
            }
        }

        if (bestMarket != null)
        {
            int caravanId = bestMarket.HireCaravan(InternalConstants.COMMAND_AI);
            if (caravanId != 0)
            {
                UnitCaravan caravan = (UnitCaravan)UnitArray[caravanId];
                caravan.SetStop(1, mine.LocX1, mine.LocY1, InternalConstants.COMMAND_AI);
                caravan.SetStopPickUp(1, TradeStop.NO_PICK_UP, InternalConstants.COMMAND_AI);
                caravan.SetStopPickUp(1, mine.RawId, InternalConstants.COMMAND_AI);
                caravan.SetStop(2, factory.LocX1, factory.LocY1, InternalConstants.COMMAND_AI);
                caravan.SetStopPickUp(2, TradeStop.NO_PICK_UP, InternalConstants.COMMAND_AI);
                _shouldCancel = true;
            }
        }
    }

    private void StartCaravan(FirmFactory factory, FirmMarket market)
    {
        if (HasCaravan(factory, market))
        {
            _shouldCancel = true;
            return;
        }
        
        int caravanId = market.HireCaravan(InternalConstants.COMMAND_AI);
        if (caravanId != 0)
        {
            UnitCaravan caravan = (UnitCaravan)UnitArray[caravanId];
            caravan.SetStop(1, factory.LocX1, factory.LocY1, InternalConstants.COMMAND_AI);
            caravan.SetStopPickUp(1, TradeStop.NO_PICK_UP, InternalConstants.COMMAND_AI);
            caravan.SetStopPickUp(1, TradeStop.PICK_UP_PRODUCT_FIRST + factory.ProductId - 1, InternalConstants.COMMAND_AI);
            caravan.SetStop(2, market.LocX1, market.LocY1, InternalConstants.COMMAND_AI);
            caravan.SetStopPickUp(2, TradeStop.NO_PICK_UP, InternalConstants.COMMAND_AI);
            _shouldCancel = true;
        }
    }

    private void StartCaravan(FirmMarket foreignMarket, FirmMarket market)
    {
        if (HasCaravan(foreignMarket, market))
        {
            _shouldCancel = true;
            return;
        }
        
        int caravanId = market.HireCaravan(InternalConstants.COMMAND_AI);
        if (caravanId != 0)
        {
            UnitCaravan caravan = (UnitCaravan)UnitArray[caravanId];
            caravan.SetStop(1, foreignMarket.LocX1, foreignMarket.LocY1, InternalConstants.COMMAND_AI);
            caravan.SetStopPickUp(1, TradeStop.NO_PICK_UP, InternalConstants.COMMAND_AI);
            for(int i = TradeStop.PICK_UP_PRODUCT_FIRST; i <= TradeStop.PICK_UP_PRODUCT_LAST; i++)
                caravan.SetStopPickUp(1, i, InternalConstants.COMMAND_AI);
            caravan.SetStop(2, market.LocX1, market.LocY1, InternalConstants.COMMAND_AI);
            caravan.SetStopPickUp(2, TradeStop.NO_PICK_UP, InternalConstants.COMMAND_AI);
            _shouldCancel = true;
        }
    }
    
    private bool HasCaravan(Firm firm1, Firm firm2)
    {
        foreach (Unit unit in UnitArray)
        {
            if (unit is UnitCaravan caravan)
            {
                bool hasMineStop = false;
                bool hasFactoryStop = false;
                for (int i = 0; i < caravan.Stops.Length; i++)
                {
                    CaravanStop stop = caravan.Stops[i];
                    if (stop.FirmId == firm1.FirmId)
                        hasMineStop = true;
                    if (stop.FirmId == firm2.FirmId)
                        hasFactoryStop = true;
                }

                if (hasMineStop && hasFactoryStop)
                {
                    return true;
                }
            }
        }

        return false;
    }
}