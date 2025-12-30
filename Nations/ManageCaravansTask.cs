using System;
using System.Collections.Generic;

namespace TenKingdoms;

// Caravan should be started when
//  1. Need to deliver resource from our mine or our market or another kingdom market to our factory - TODO
//  2. Need to deliver product from our factory to our market - TODO
//  3. Need to import product from another kingdom market - TODO
//  4. Need to move products between our markets - TODO
//  5. Need to move products out from our market that should be sold - TODO

// Caravan should be stopped when
//  1. Raw resource is exhausted
//  2. Factory stopped manufacture its product
//  3. Cannot import product from another kingdom because no such product or trade treaty has ended

// Before hiring caravan check if there is an idle caravan available
// Check if distance between caravan stops is not too far - should depend on pref
// Check if there are existing caravans on this route already - should depend on pref

public class ManageCaravansTask : AITask
{
    private readonly List<UnitCaravan> _kingdomCaravans = new List<UnitCaravan>();
    
    public ManageCaravansTask(Nation nation) : base(nation)
    {
    }

    public override bool ShouldCancel()
    {
        return false;
    }

    //TODO take trade treaty into account
    public override void Process()
    {
        _kingdomCaravans.Clear();
        foreach (Unit unit in UnitArray)
        {
            if (unit.NationId == Nation.nation_recno && unit is UnitCaravan caravan)
                _kingdomCaravans.Add(caravan);
        }
        
        foreach (Firm firm in FirmArray)
        {
            if (firm.UnderConstruction)
                continue;
            
            if (firm.FirmType == Firm.FIRM_MINE)
            {
                FirmMine mine = (FirmMine)firm;
                if (mine.NationId != Nation.nation_recno)
                    continue;

                if (mine.StockQty > 0.0)
                    CheckMineToFactoryCaravan(mine);
            }

            if (firm.FirmType == Firm.FIRM_FACTORY)
            {
                FirmFactory factory = (FirmFactory)firm;
                if (factory.NationId != Nation.nation_recno)
                    continue;
                
                if (factory.StockQty > 0.0)
                    CheckFactoryToMarketCaravan(factory);
            }

            if (firm.FirmType == Firm.FIRM_MARKET)
            {
                FirmMarket market = (FirmMarket)firm;
                if (market.NationId == Nation.nation_recno)
                {
                    CheckDistributeProductCaravan(market);
                }
                else
                {
                    CheckImportProductCaravan(market);
                }
            }
        }
    }

    private void CheckMineToFactoryCaravan(FirmMine mine)
    {
        bool hasLinkedFactory = false;
        for (int i = 0; i < mine.LinkedFirms.Count; i++)
        {
            Firm linkedFirm = FirmArray[mine.LinkedFirms[i]];
            if (linkedFirm.NationId != mine.NationId || linkedFirm.UnderConstruction)
                continue;
                    
            if (mine.LinkedFirmsEnable[i] != InternalConstants.LINK_EE)
                continue;

            if (linkedFirm is FirmFactory linkedFactory && linkedFactory.ProductId == mine.RawId)
                hasLinkedFactory = true;
        }

        //TODO maybe there is a linked factory but we still need a caravan to move resource to another factory
        if (!hasLinkedFactory)
        {
            FirmFactory bestFactory = null;
            int bestRating = Int16.MaxValue;
            foreach (Firm otherFirm in FirmArray)
            {
                if (otherFirm.NationId != Nation.nation_recno || otherFirm.UnderConstruction)
                    continue;

                if (otherFirm.RegionId != mine.RegionId)
                    continue;

                if (otherFirm is FirmFactory factory && factory.ProductId == mine.RawId)
                {
                    int rating = Misc.FirmsDistance(mine, factory);
                    if (rating < bestRating)
                    {
                        bestFactory = factory;
                        bestRating = rating;
                    }
                }
            }

            //TODO maybe one caravan is not enough
            if (bestFactory != null && !HasCaravan(mine, bestFactory))
            {
                StartCaravan(mine, mine.RawId + TradeStop.PICK_UP_RAW_FIRST - 1, bestFactory, 0);
            }
        }
    }

    private void CheckFactoryToMarketCaravan(FirmFactory factory)
    {
        bool hasLinkedMarket = false;
        for (int i = 0; i < factory.LinkedFirms.Count; i++)
        {
            Firm linkedFirm = FirmArray[factory.LinkedFirms[i]];
            if (linkedFirm.NationId != factory.NationId || linkedFirm.UnderConstruction)
                continue;

            if (factory.LinkedFirmsEnable[i] != InternalConstants.LINK_EE)
                continue;

            if (linkedFirm is FirmMarket market && market.IsRetailMarket())
                hasLinkedMarket = true;
        }

        if (!hasLinkedMarket)
        {
            FirmMarket bestMarket = null;
            int bestRating = Int16.MaxValue;
            foreach (Firm otherFirm in FirmArray)
            {
                if (otherFirm.NationId != Nation.nation_recno || otherFirm.UnderConstruction)
                    continue;

                if (otherFirm.RegionId != factory.RegionId)
                    continue;

                if (otherFirm is FirmMarket market && market.IsRetailMarket())
                {
                    int rating = Misc.FirmsDistance(factory, market);
                    if (rating < bestRating)
                    {
                        bestMarket = market;
                        bestRating = rating;
                    }
                }
            }

            //TODO maybe one caravan is not enough
            if (bestMarket != null && !HasCaravan(factory, bestMarket))
            {
                StartCaravan(factory, factory.ProductId + TradeStop.PICK_UP_PRODUCT_FIRST - 1, bestMarket, 0);
            }
        }
    }

    private void CheckImportRawResourceCaravan(FirmMarket market)
    {
        //TODO
    }
    
    private void CheckImportProductCaravan(FirmMarket market)
    {
        bool hasAnyProduct = false;
        foreach (MarketGoods marketGoods in market.MarketGoods)
        {
            if (marketGoods.ProductId != 0 && marketGoods.StockQty > 100.0)
                hasAnyProduct = true;
        }

        if (hasAnyProduct)
        {
            FirmMarket bestMarket = null;
            int bestDistance = Int16.MaxValue;
            foreach (Firm otherFirm in FirmArray)
            {
                if (otherFirm.NationId != Nation.nation_recno || otherFirm.UnderConstruction)
                    continue;

                if (otherFirm.RegionId != market.RegionId)
                    continue;

                if (otherFirm is FirmMarket otherMarket && otherMarket.IsRetailMarket())
                {
                    //TODO do not import product if we already have enough
                    int distance = Misc.FirmsDistance(market, otherFirm);
                    if (distance < 100 && distance < bestDistance)
                    {
                        bestMarket = otherMarket;
                        bestDistance = distance;
                    }
                }
            }
            
            //TODO maybe one caravan is not enough
            if (bestMarket != null && !HasCaravan(market, bestMarket))
            {
                StartCaravan(market, 0, bestMarket, 0);
            }
        }
    }

    private void CheckMoveRawResourceCaravan(FirmMarket market)
    {
        //TODO
    }

    private void CheckMoveProductCaravan(FirmMarket market)
    {
        //TODO
    }

    private void CheckDistributeProductCaravan(FirmMarket market)
    {
        bool hasAnyProduct = false;
        foreach (MarketGoods marketGoods in market.MarketGoods)
        {
            if (marketGoods.ProductId != 0 && marketGoods.StockQty > 0.0)
                hasAnyProduct = true;
        }

        if (hasAnyProduct)
        {
            List<FirmMarket> nearbyMarkets = new List<FirmMarket>();
            foreach (Firm otherFirm in FirmArray)
            {
                if (otherFirm.NationId != Nation.nation_recno || otherFirm.UnderConstruction)
                    continue;

                if (otherFirm.RegionId != market.RegionId)
                    continue;

                if (otherFirm.FirmId == market.FirmId)
                    continue;

                if (Misc.FirmsDistance(market, otherFirm) > 100)
                    continue;
                
                if (otherFirm is FirmMarket otherMarket && otherMarket.IsRetailMarket())
                    nearbyMarkets.Add(otherMarket);
            }

            foreach (FirmMarket nearbyMarket in nearbyMarkets)
            {
                //TODO maybe one caravan is not enough
                if (!HasCaravan(market, nearbyMarket))
                {
                    FirmMarket bestThirdMarket = null;
                    int bestRouteDistance = Int16.MaxValue;
                    foreach (FirmMarket thirdMarket in nearbyMarkets)
                    {
                        if (thirdMarket.FirmId == nearbyMarket.FirmId)
                            continue;

                        int routeDistance = Misc.FirmsDistance(market, nearbyMarket) +
                                            Misc.FirmsDistance(nearbyMarket, thirdMarket) +
                                            Misc.FirmsDistance(thirdMarket, market);
                        if (routeDistance < 200 && routeDistance < bestRouteDistance)
                        {
                            bestThirdMarket = thirdMarket;
                            bestRouteDistance = routeDistance;
                        }
                    }
                    
                    StartCaravan(market, 0, nearbyMarket, 0, bestThirdMarket, 0);
                }
            }
        }
    }

    private bool HasCaravan(Firm firm1, Firm firm2)
    {
        foreach (UnitCaravan caravan in _kingdomCaravans)
        {
            bool hasFirm1Stop = false;
            bool hasFirm2Stop = false;
            foreach (CaravanStop stop in caravan.Stops)
            {
                if (stop.FirmId == firm1.FirmId)
                    hasFirm1Stop = true;
                if (stop.FirmId == firm2.FirmId)
                    hasFirm2Stop = true;
            }

            if (hasFirm1Stop && hasFirm2Stop)
                return true;
        }

        return false;
    }

    private UnitCaravan FindCaravan(Firm firm1, Firm firm2)
    {
        //TODO look for an idle caravan first
        
        FirmMarket bestMarket = null;
        int bestRating = Int16.MaxValue;
        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId != Nation.nation_recno || firm.UnderConstruction)
                continue;

            if (firm.FirmType != Firm.FIRM_MARKET)
                continue;

            if (firm.RegionId != firm1.RegionId || firm.RegionId != firm2.RegionId)
                continue;

            FirmMarket market = (FirmMarket)firm;
            int rating = Misc.FirmsDistance(market, firm1);
            if (rating < bestRating)
            {
                bestMarket = market;
                bestRating = rating;
            }
            rating = Misc.FirmsDistance(market, firm2);
            if (rating < bestRating)
            {
                bestMarket = market;
                bestRating = rating;
            }
        }

        if (bestMarket != null && bestMarket.CanHireCaravan())
        {
            int caravanId = bestMarket.HireCaravan(InternalConstants.COMMAND_AI);
            if (caravanId != 0)
                return (UnitCaravan)UnitArray[caravanId];
        }

        return null;
    }

    private void StartCaravan(Firm firm1, int pickUp1, Firm firm2, int pickUp2, Firm firm3 = null, int pickUp3 = 0)
    {
        UnitCaravan caravan = FindCaravan(firm1, firm2);
        if (caravan == null)
            return;

        bool ourMarketsCaravan = firm1.FirmType == Firm.FIRM_MARKET && firm2.FirmType == Firm.FIRM_MARKET &&
                                 firm1.NationId == Nation.nation_recno && firm2.NationId == Nation.nation_recno;
        SetCaravanStop(caravan, firm1, 1, pickUp1, ourMarketsCaravan);
        SetCaravanStop(caravan, firm2, 2, pickUp2, ourMarketsCaravan);

        if (firm3 != null)
        {
            ourMarketsCaravan = ourMarketsCaravan && firm3.FirmType == Firm.FIRM_MARKET && firm3.NationId == Nation.nation_recno;
            SetCaravanStop(caravan, firm3, 3, pickUp3, ourMarketsCaravan);
        }
    }

    private void SetCaravanStop(UnitCaravan caravan, Firm firm, int stopId, int pickUpType, bool autoPickUp)
    {
        caravan.SetStop(stopId, firm.LocX1, firm.LocY1, InternalConstants.COMMAND_AI);
        caravan.SetStopPickUp(stopId, TradeStop.NO_PICK_UP, InternalConstants.COMMAND_AI);
        if (pickUpType != 0)
        {
            caravan.SetStopPickUp(stopId, pickUpType, InternalConstants.COMMAND_AI);
        }
        else
        {
            if (firm.NationId != Nation.nation_recno)
            {
                for (int i = TradeStop.PICK_UP_PRODUCT_FIRST; i <= TradeStop.PICK_UP_PRODUCT_LAST; i++)
                    caravan.SetStopPickUp(stopId, i, InternalConstants.COMMAND_AI);
            }

            if (autoPickUp)
                caravan.SetStopPickUp(stopId, TradeStop.AUTO_PICK_UP, InternalConstants.COMMAND_AI);
        }
    }
}