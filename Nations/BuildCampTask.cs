namespace TenKingdoms;

public class BuildCampTask : AITask
{
    private int _generalId;
    private bool _generalSent;
    
    public int TownId { get; }
    
    public BuildCampTask(NationBase nation, int townId) : base(nation)
    {
        TownId = townId;
    }

    public override bool ShouldCancel()
    {
        if (TownArray.IsDeleted(TownId))
            return true;

        Town town = TownArray[TownId];
        if (town.NationId != Nation.nation_recno)
            return true;

        if (town.HasLinkedCamp(Nation.nation_recno, false))
            return true;
        
        return false;
    }

    public override void Process()
    {
        Town town = TownArray[TownId];
        int majorRace = town.MajorityRace();

        // TODO use builder for building camps
        if (_generalId == 0)
        {
            Town sourceTown = null;
            // TODO look for a general in camps, bases inns or train
            foreach (Town otherTown in TownArray)
            {
                if (otherTown.NationId != Nation.nation_recno)
                    continue;

                // TODO other region
                if (otherTown.RegionId != town.RegionId)
                    continue;

                // TODO check for other races too
                if (otherTown.CanRecruit(majorRace))
                {
                    sourceTown = otherTown;
                }
            }

            if (sourceTown != null)
            {
                _generalId = sourceTown.Recruit(Skill.SKILL_LEADING, majorRace, InternalConstants.COMMAND_AI);
            }
        }

        if (_generalId == 0)
            return;

        Unit general = UnitArray[_generalId];
        if (general.unit_mode == UnitConstants.UNIT_MODE_UNDER_TRAINING)
            return;
        
        if (general.rank_id != Unit.RANK_GENERAL || general.rank_id != Unit.RANK_KING)
            general.set_rank(Unit.RANK_GENERAL);

        if (!_generalSent)
        {
            //TODO find best location
            general.build_firm(town.LocX1 + 5, town.LocY1, Firm.FIRM_CAMP, InternalConstants.COMMAND_AI);
            _generalSent = true;
        }
        else
        {
            //TODO check that general is on the way, not stuck and is able to build camp
        }
    }
}