using System;

namespace TenKingdoms;

public abstract class AITask
{
    protected NationBase Nation { get; }

    protected FirmRes FirmRes => Sys.Instance.FirmRes;
    protected World World => Sys.Instance.World;
    
    protected FirmArray FirmArray => Sys.Instance.FirmArray;
    protected TownArray TownArray => Sys.Instance.TownArray;
    protected UnitArray UnitArray => Sys.Instance.UnitArray;
    protected SiteArray SiteArray => Sys.Instance.SiteArray;

    protected AITask(NationBase nation)
    {
        Nation = nation;
    }

    public abstract bool ShouldCancel();

    public virtual void Cancel()
    {
    }

    public abstract void Process();
    
    protected int FindBuilder(int buildLocX, int buildLocY)
    {
        int builderId = 0;
        Location location = World.GetLoc(buildLocX, buildLocY);
        
        int minFirmDistance = Int32.MaxValue / 2;
        Firm bestFirm = null;
        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId != Nation.nation_recno)
                continue;
            
            if (firm.UnderConstruction)
                continue;

            // TODO other region
            if (firm.RegionId != location.RegionId)
                continue;

            // TODO use pref
            if (firm.BuilderId != 0 && firm.HitPoints > firm.MaxHitPoints / 2.0)
            {
                int firmDistance = Misc.points_distance(firm.LocX1, firm.LocY1, buildLocX, buildLocY);
                if (firmDistance < minFirmDistance)
                {
                    //TODO prefer firms at the same region
                    minFirmDistance = firmDistance;
                    bestFirm = firm;
                }
            }
        }

        int minTownDistance = Int32.MaxValue / 2;
        Town bestTown = null;
        int bestRace = 0;
        foreach (Town town in TownArray)
        {
            if (town.NationId != Nation.nation_recno)
                continue;

            // TODO other region
            if (town.RegionId != location.RegionId)
                continue;

            // TODO do not train if population is low
            if (town.Population <= 10)
                continue;

            // TODO check all races, select race better
            int race = town.PickRandomRace(false, false);
            if (race == 0 || !town.CanTrain(race))
                continue;

            int townDistance = Misc.points_distance(town.LocX1, town.LocY1, buildLocX, buildLocY);
            if (townDistance < minTownDistance)
            {
                //TODO prefer towns at the same region
                minTownDistance = townDistance;
                bestTown = town;
                bestRace = race;
            }
        }

        if (minFirmDistance <= minTownDistance + 20)
        {
            bestTown = null;
        }
        else
        {
            bestFirm = null;
        }

        if (bestFirm != null)
        {
            builderId = bestFirm.BuilderId;
            if (!bestFirm.MobilizeBuilder(builderId))
                builderId = 0;
        }

        if (bestTown != null)
        {
            builderId = bestTown.Recruit(Skill.SKILL_CONSTRUCTION, bestRace, InternalConstants.COMMAND_AI);
        }

        //TODO hire builder from inn
        //TODO check idle construction workers
        return builderId;
    }
}
