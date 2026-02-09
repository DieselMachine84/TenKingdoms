using System;
using System.Collections.Generic;

namespace TenKingdoms;

// Find idle units and move them
//  1. To firms for construction workers
//  2. To harbors for ships
//  3. To camps for soldiers and generals - TODO
//  4. To another territory - TODO
//  5. Settle otherwise - TODO

// If an idle unit can be used for a new task - remove it from idle units - TODO

public class IdleUnitTask : AITask, IUnitTask
{
    private bool _shouldCancel;
    public int UnitId { get; }
    
    public IdleUnitTask(Nation nation, int unitId) : base(nation)
    {
        UnitId = unitId;
    }

    public override bool ShouldCancel()
    {
        if (_shouldCancel)
            return true;
        
        if (UnitIsDeletedOrChangedNation(UnitId))
            return true;

        return false;
    }

    public override void Process()
    {
        Unit unit = UnitArray[UnitId];
        if (unit.Skill.SkillId == Skill.SKILL_CONSTRUCTION)
        {
            int minFirmDistance = Int16.MaxValue;
            Firm bestFirm = null;
            foreach (Firm firm in Nation.KingdomFirms)
            {
                if (firm.UnderConstruction)
                    continue;

                if (firm.RegionId != unit.RegionId())
                    continue;
                
                //TODO replace less skilled builder or settle builder
                if (firm.BuilderId == 0)
                {
                    int firmDistance = Misc.PointsDistance(firm.LocCenterX, firm.LocCenterY, unit.NextLocX, unit.NextLocY);
                    if (firmDistance < minFirmDistance)
                    {
                        minFirmDistance = firmDistance;
                        bestFirm = firm;
                    }
                }
            }

            if (bestFirm != null)
                unit.Assign(bestFirm.LocX1, bestFirm.LocY1);
        }

        UnitInfo unitInfo = UnitRes[unit.UnitType];
        if (unitInfo.UnitClass == UnitConstants.UNIT_CLASS_SHIP)
        {
            foreach (Firm firm in Nation.KingdomHarbors)
            {
                if (firm.UnderConstruction)
                    continue;

                FirmHarbor harbor = (FirmHarbor)firm;
                if (harbor.Ships.Count < GameConstants.MAX_SHIP_IN_HARBOR)
                {
                    //TODO use Unit.Assign or UnitArray.Assign?
                    List<int> unitsToAssign = new List<int> { UnitId };
                    UnitArray.Assign(harbor.LocX1, harbor.LocY1, false, unitsToAssign, InternalConstants.COMMAND_AI);
                }
            }
        }
        
        _shouldCancel = true;
    }
}