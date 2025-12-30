using System;

namespace TenKingdoms;

// Trader ship should be started when
// 1. Need to deliver resource from our mine or our market or another kingdom market to our factory - TODO
// 2. Need to import product from another kingdom harbor
// 3. Need to move products between our harbors

public class ManageTradeShipsTask : AITask
{
    public ManageTradeShipsTask(Nation nation) : base(nation)
    {
    }

    public override bool ShouldCancel()
    {
        return false;
    }

    //TODO take trade treaty into account
    public override void Process()
    {
        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId != Nation.nation_recno || firm.UnderConstruction)
                continue;

            if (firm.FirmType != Firm.FIRM_HARBOR)
                continue;

            FirmHarbor harbor = (FirmHarbor)firm;
            if (!HasLinkedMarket(harbor))
                continue;

            foreach (Firm otherFirm in FirmArray)
            {
                if (otherFirm.FirmId == harbor.FirmId || otherFirm.UnderConstruction)
                    continue;
            
                if (otherFirm.FirmType != Firm.FIRM_HARBOR)
                    continue;

                FirmHarbor otherHarbor = (FirmHarbor)otherFirm;
                if (otherHarbor.SeaRegionId != harbor.SeaRegionId || otherHarbor.LandRegionId == harbor.LandRegionId)
                    continue;

                if (HasLinkedMarket(otherHarbor) && !HasTradeShip(harbor, otherHarbor))
                    StartTradeShip(harbor, 0, otherHarbor, 0);
            }
        }
    }

    private bool HasLinkedMarket(FirmHarbor harbor)
    {
        foreach (int linkedFirmId in harbor.LinkedFirms)
        {
            Firm linkedFirm = FirmArray[linkedFirmId];
            if (linkedFirm.NationId == harbor.NationId && linkedFirm.FirmType == Firm.FIRM_MARKET)
                return true;
        }

        return false;
    }
    
    private bool HasTradeShip(Firm firm1, Firm firm2)
    {
        foreach (Unit unit in UnitArray)
        {
            if (unit.NationId != Nation.nation_recno)
                continue;

            if (unit.UnitType != UnitConstants.UNIT_VESSEL)
                continue;

            UnitMarine ship = (UnitMarine)unit;
            
            bool hasFirm1Stop = false;
            bool hasFirm2Stop = false;
            foreach (ShipStop stop in ship.Stops)
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

    private UnitMarine FindTradeShip(FirmHarbor harbor)
    {
        //TODO look for an idle trade ship first
        
        foreach (int shipId in harbor.Ships)
        {
            UnitMarine ship = (UnitMarine)UnitArray[shipId];
            if (ship.UnitType == UnitConstants.UNIT_VESSEL && harbor.SailShip(shipId, InternalConstants.COMMAND_AI))
            {
                return ship;
            }
        }

        Nation.AddBuildShipTask(harbor, UnitConstants.UNIT_VESSEL);
        return null;
    }

    private void StartTradeShip(FirmHarbor harbor1, int pickUp1, FirmHarbor harbor2, int pickUp2)
    {
        UnitMarine ship = FindTradeShip(harbor1);
        if (ship == null)
            ship = FindTradeShip(harbor2);
        
        if (ship == null)
            return;

        bool ourHarborsTrader = harbor1.NationId == Nation.nation_recno && harbor2.NationId == Nation.nation_recno;
        SetShipStop(ship, harbor1, 1, pickUp1, ourHarborsTrader);
        SetShipStop(ship, harbor2, 2, pickUp2, ourHarborsTrader);
    }

    private void SetShipStop(UnitMarine ship, FirmHarbor harbor, int stopId, int pickUpType, bool autoPickUp)
    {
        ship.SetStop(stopId, harbor.LocX1, harbor.LocY1, InternalConstants.COMMAND_AI);
        ship.SetStopPickUp(stopId, TradeStop.NO_PICK_UP, InternalConstants.COMMAND_AI);
        if (pickUpType != 0)
        {
            ship.SetStopPickUp(stopId, pickUpType, InternalConstants.COMMAND_AI);
        }
        else
        {
            if (harbor.NationId != Nation.nation_recno)
            {
                for (int i = TradeStop.PICK_UP_PRODUCT_FIRST; i <= TradeStop.PICK_UP_PRODUCT_LAST; i++)
                    ship.SetStopPickUp(stopId, i, InternalConstants.COMMAND_AI);
            }

            if (autoPickUp)
                ship.SetStopPickUp(stopId, TradeStop.AUTO_PICK_UP, InternalConstants.COMMAND_AI);
        }
    }
}