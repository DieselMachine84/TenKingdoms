using System;

namespace TenKingdoms;

public class BuildMineTask : AITask
{
    private int _builderId;
    private bool _builderSent;
    public int SiteId { get; }
    
    public BuildMineTask(NationBase nation, int siteId) : base(nation)
    {
        SiteId = siteId;
    }

    public override bool ShouldCancel()
    {
        if (SiteArray.IsDeleted(SiteId))
            return true;

        Site site = SiteArray[SiteId];
        if (site.HasMine)
            return true;

        return false;
    }

    public override void Process()
    {
        Site site = SiteArray[SiteId];

        if (_builderId != 0 && UnitArray.IsDeleted(_builderId))
            _builderId = 0;

        if (_builderId == 0)
        {
            int minFirmDistance = Int32.MaxValue;
            Firm bestFirm = null;
            foreach (Firm firm in FirmArray)
            {
                if (firm.nation_recno != Nation.nation_recno)
                    continue;

                // TODO other region
                if (firm.region_id != site.RegionId)
                    continue;

                // TODO use pref
                if (firm.builder_recno != 0 && firm.hit_points > firm.max_hit_points / 2.0)
                {
                    int firmDistance = Misc.points_distance(firm.loc_x1, firm.loc_y1, site.LocX, site.LocY);
                    if (firmDistance < minFirmDistance)
                    {
                        //TODO prefer firms at the same region
                        minFirmDistance = firmDistance;
                        bestFirm = firm;
                    }
                }
            }
            
            int minTownDistance = Int32.MaxValue;
            Town bestTown = null;
            int bestRace = 0;

            foreach (Town town in TownArray)
            {
                if (town.NationId != Nation.nation_recno)
                    continue;

                // TODO other region
                if (town.RegionId != site.RegionId)
                    continue;

                int race = town.PickRandomRace(false, true);
                // TODO do not train if population is low
                if (race == 0 || !town.CanTrain(race))
                    continue;
                
                int townDistance = Misc.points_distance(town.LocX1, town.LocY1, site.LocX, site.LocY);
                if (townDistance < minTownDistance)
                {
                    //TODO prefer towns at the same region
                    minTownDistance = townDistance;
                    bestTown = town;
                    bestRace = race;
                }
            }

            if (minFirmDistance <= minTownDistance)
            {
                bestTown = null;
            }
            else
            {
                if (bestFirm != null && bestTown != null)
                {
                    if (Misc.PointsDistance(bestFirm.loc_x1, bestFirm.loc_y1, bestFirm.loc_x2, bestFirm.loc_y2,
                            bestTown.LocX1, bestTown.LocY1, bestTown.LocX2, bestTown.LocY2) < 10)
                    {
                        bestTown = null;
                    }
                }
                else
                {
                    bestFirm = null;
                }
            }

            if (bestFirm != null)
            {
                _builderId = bestFirm.builder_recno;
                if (!bestFirm.mobilize_builder(_builderId))
                    _builderId = 0;
            }

            if (bestTown != null)
            {
                _builderId = bestTown.Recruit(Skill.SKILL_CONSTRUCTION, bestRace, InternalConstants.COMMAND_AI);
            }
            
            //TODO hire builder from inn
            //TODO check idle construction workers
        }

        if (_builderId == 0)
            return;

        Unit builder = UnitArray[_builderId];
        if (builder.unit_mode == UnitConstants.UNIT_MODE_UNDER_TRAINING)
            return;

        if (!_builderSent)
        {
            //World.can_build_firm()
            //TODO find best location
            builder.build_firm(site.LocX, site.LocY, Firm.FIRM_MINE, InternalConstants.COMMAND_AI);
            _builderSent = true;
        }
        else
        {
            //TODO check that builder is on the way, not stuck and is able to build mine
        }
    }
}
