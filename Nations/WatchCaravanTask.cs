using System;
using System.Collections.Generic;

namespace TenKingdoms;

// Start caravans between our markets
// Stop useless caravans
// Update caravans pick up

public class WatchCaravanTask : AITask
{
    private readonly List<int> _ourRetailMarkets = new List<int>();
    private readonly List<int> _ourCaravans = new List<int>();

    public WatchCaravanTask(Nation nation) : base(nation)
    {
    }

    public override bool ShouldCancel()
    {
        return false;
    }

    public override void Process()
    {
        _ourRetailMarkets.Clear();
        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId != Nation.nation_recno)
                continue;

            if (firm.UnderConstruction)
                continue;
            
            if (firm is FirmMarket market && market.IsRetailMarket())
                _ourRetailMarkets.Add(firm.FirmId);
        }
        
        _ourCaravans.Clear();
        foreach (Unit unit in UnitArray)
        {
            if (unit.NationId != Nation.nation_recno)
                continue;
            
            if (unit is UnitCaravan)
                _ourCaravans.Add(unit.SpriteId);
        }
        
        foreach (int firmId1 in _ourRetailMarkets)
        {
            FirmMarket market1 = (FirmMarket)FirmArray[firmId1];
            
            foreach (int firmId2 in _ourRetailMarkets)
            {
                if (firmId2 == firmId1)
                    continue;
                
                FirmMarket market2 = (FirmMarket)FirmArray[firmId2];
                if (market2.RegionId != market1.RegionId)
                    continue;

                //TODO use pref
                if (Misc.FirmsDistance(market1, market2) > 100)
                    continue;

                bool hasInnerCaravan = false;
                foreach (int ourCaravanId in _ourCaravans)
                {
                    UnitCaravan ourCaravan = (UnitCaravan)UnitArray[ourCaravanId];
                    bool hasMarket1Stop = false;
                    bool hasMarket2Stop = false;
                    for (int i = 0; i < ourCaravan.Stops.Length; i++)
                    {
                        CaravanStop stop = ourCaravan.Stops[i];
                        if (stop.FirmId == market1.FirmId)
                            hasMarket1Stop = true;
                        if (stop.FirmId == market2.FirmId)
                            hasMarket2Stop = true;
                    }

                    if (hasMarket1Stop && hasMarket2Stop)
                        hasInnerCaravan = true;
                }

                if (!hasInnerCaravan)
                {
                    FirmMarket thirdMarket = null;
                    int bestRating = Int16.MaxValue;
                    foreach (int firmId3 in _ourRetailMarkets)
                    {
                        if (firmId3 == firmId1 || firmId3 == firmId2)
                            continue;
                        
                        FirmMarket market3 = (FirmMarket)FirmArray[firmId3];
                        if (market3.RegionId != market1.RegionId)
                            continue;
                        
                        int rating = Misc.FirmsDistance(market1, market3) + Misc.FirmsDistance(market2, market3);
                        if (rating < 100 && rating < bestRating)
                        {
                            thirdMarket = market3;
                            bestRating = rating;
                        }
                    }

                    int caravanId = market1.HireCaravan(InternalConstants.COMMAND_AI);
                    if (caravanId != 0)
                    {
                        UnitCaravan caravan = (UnitCaravan)UnitArray[caravanId];
                        caravan.SetStop(1, market1.LocX1, market1.LocY1, InternalConstants.COMMAND_AI);
                        caravan.SetStopPickUp(1, TradeStop.NO_PICK_UP, InternalConstants.COMMAND_AI);
                        caravan.SetStopPickUp(1, TradeStop.AUTO_PICK_UP, InternalConstants.COMMAND_AI);
                        caravan.SetStop(2, market2.LocX1, market2.LocY1, InternalConstants.COMMAND_AI);
                        caravan.SetStopPickUp(2, TradeStop.NO_PICK_UP, InternalConstants.COMMAND_AI);
                        caravan.SetStopPickUp(2, TradeStop.AUTO_PICK_UP, InternalConstants.COMMAND_AI);
                        if (thirdMarket != null)
                        {
                            caravan.SetStop(3, thirdMarket.LocX1, thirdMarket.LocY1, InternalConstants.COMMAND_AI);
                            caravan.SetStopPickUp(3, TradeStop.NO_PICK_UP, InternalConstants.COMMAND_AI);
                            caravan.SetStopPickUp(3, TradeStop.AUTO_PICK_UP, InternalConstants.COMMAND_AI);
                        }
                    }
                }
            }
        }
    }
}