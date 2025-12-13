using System;

namespace TenKingdoms;

public class IdleUnitTask : AITask
{
    private bool _shouldCancel;
    public int UnitId { get; }
    
    public IdleUnitTask(Nation nation, int unitId) : base(nation)
    {
        UnitId = unitId;
    }

    public override bool ShouldCancel()
    {
        if (UnitArray.IsDeleted(UnitId))
            _shouldCancel = true;

        return _shouldCancel;
    }

    public override void Process()
    {
        Unit unit = UnitArray[UnitId];
        if (unit.Skill.SkillId == Skill.SKILL_CONSTRUCTION)
        {
            int minFirmDistance = Int32.MaxValue;
            Firm bestFirm = null;
            foreach (Firm firm in FirmArray)
            {
                if (firm.NationId != Nation.nation_recno)
                    continue;

                if (firm.UnderConstruction)
                    continue;

                // TODO other region
                if (firm.RegionId != unit.RegionId())
                    continue;
                
                if (firm.BuilderId == 0)
                {
                    int firmDistance = Misc.points_distance(firm.LocX1, firm.LocY1, unit.NextLocX, unit.NextLocY);
                    if (firmDistance < minFirmDistance)
                    {
                        //TODO prefer firms at the same region
                        minFirmDistance = firmDistance;
                        bestFirm = firm;
                    }
                }
            }

            if (bestFirm != null)
            {
                unit.Assign(bestFirm.LocX1, bestFirm.LocY1);
            }
            else
            {
                //TODO replace less skilled builder or settle builder
            }
        }

        _shouldCancel = true;
    }
}