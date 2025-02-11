using System;

namespace TenKingdoms;

public class SettleTask : AITask
{
    private int _settlerId;
    private bool _settlerSent;
    public int FirmId { get; }
    
    public SettleTask(NationBase nation, int firmId) : base(nation)
    {
        FirmId = firmId;
    }

    public override bool ShouldCancel()
    {
        if (FirmArray.IsDeleted(FirmId))
            return true;

        Firm firm = FirmArray[FirmId];
        if (firm.nation_recno != Nation.nation_recno)
            return true;

        foreach (int townId in firm.linked_town_array)
        {
            Town town = TownArray[townId];
            if (town.NationId == Nation.nation_recno)
                return true;
        }

        return false;
    }

    public override void Process()
    {
        Firm firm = FirmArray[FirmId];

        if (_settlerId != 0 && UnitArray.IsDeleted(_settlerId))
            _settlerId = 0;

        if (_settlerId == 0)
        {
            int minTownDistance = Int32.MaxValue;
            Town bestTown = null;
            foreach (Town town in TownArray)
            {
                if (town.NationId != firm.nation_recno)
                    continue;

                // TODO other region
                if (town.RegionId != firm.region_id)
                    continue;

                if (town.JoblessPopulation == 0)
                    continue;

                // TODO check not only distance but also which race we are going to settle
                int townDistance = Misc.PointsDistance(town.LocX1, town.LocY1, town.LocX2, town.LocY2,
                    firm.loc_x1, firm.loc_y1, firm.loc_x2, firm.loc_y2);
                if (townDistance < minTownDistance)
                {
                    minTownDistance = townDistance;
                    bestTown = town;
                }
            }

            if (bestTown != null)
            {
                _settlerId = bestTown.Recruit(-1, bestTown.PickRandomRace(false, true), InternalConstants.COMMAND_AI);
            }
        }

        if (_settlerId == 0)
            return;

        Unit settler = UnitArray[_settlerId];
        
        if (!_settlerSent)
        {
            //TODO find best location
            settler.settle(firm.loc_x1, firm.loc_y1 + 4);
            _settlerSent = true;
        }
        else
        {
            //TODO check that settler is on the way, not stuck and is able to settle
        }
    }
}