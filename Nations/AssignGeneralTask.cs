using System;

namespace TenKingdoms;

public class AssignGeneralTask : AITask, IUnitTask
{
    private int _generalId;
    private bool _generalSent;
    private bool _shouldCancel;
    public int FirmId { get; }
    public int UnitId => _generalId;
    
    public AssignGeneralTask(NationBase nation, int firmId) : base(nation)
    {
        FirmId = firmId;
    }

    public override bool ShouldCancel()
    {
        if (_shouldCancel)
            return true;

        if (FirmArray.IsDeleted(FirmId))
            return true;

        Firm firm = FirmArray[FirmId];
        if (firm.NationId != Nation.nation_recno)
            return true;

        return false;
    }

    public override void Cancel()
    {
        if (_generalId != 0 && !UnitArray.IsDeleted(_generalId))
        {
            Unit general = UnitArray[_generalId];
            if (general.IsVisible())
                general.Stop2();
        }
    }
    
    public override void Process()
    {
        Firm firm = FirmArray[FirmId];
        
        if (_generalId != 0 && UnitArray.IsDeleted(_generalId))
            _generalId = 0;
        
        if (_generalId == 0)
            FindGeneral(firm.LocCenterX, firm.LocCenterY);
        
        if (_generalId == 0)
            FindPeasant(firm.LocCenterX, firm.LocCenterY);

        if (_generalId == 0)
            return;
        
        Unit general = UnitArray[_generalId];
        if (general.UnitMode == UnitConstants.UNIT_MODE_UNDER_TRAINING)
            return;

        if (general.UnitMode == UnitConstants.UNIT_MODE_OVERSEE)
        {
            _shouldCancel = true;
            return;
        }
        
        if (!_generalSent)
        {
            if (general.Skill.SkillId == Skill.SKILL_LEADING && general.Rank == Unit.RANK_SOLDIER)
                general.SetRank(Unit.RANK_GENERAL);
            
            //TODO general can be in the other region
            general.Assign(firm.LocX1, firm.LocY1);
            _generalSent = true;
        }
        else
        {
            //TODO check that general is on the way, not stuck
            if (general.IsAIAllStop())
                _generalSent = false;
        }
    }

    private void FindGeneral(int firmLocX, int firmLocY)
    {
        Location targetLocation = World.GetLoc(firmLocX, firmLocY);

        Firm bestFirm = null;
        int bestWorkerId = 0;
        int bestRating = 0;
        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId != Nation.nation_recno)
                continue;
            
            if (firm.FirmType != Firm.FIRM_CAMP)
                continue;

            //TODO other region
            if (firm.RegionId != targetLocation.RegionId)
                continue;

            for (int i = 0; i < firm.Workers.Count; i++)
            {
                int rating = firm.Workers[i].SkillLevel * GameConstants.MapSize / 100;
                rating += GameConstants.MapSize - Misc.points_distance(firm.LocCenterX, firm.LocCenterY, firmLocX, firmLocY);
                //TODO take general race into account

                if (rating > bestRating)
                {
                    bestFirm = firm;
                    bestWorkerId = i + 1;
                    bestRating = rating;
                }
            }
        }

        Town bestTown = null;
        int bestRace = 0;
        foreach (Town town in TownArray)
        {
            if (town.NationId != Nation.nation_recno)
                continue;

            //TODO other region
            if (town.RegionId != targetLocation.RegionId)
                continue;

            //TODO take race into account
            int raceId = town.MajorityRace();
            if (!town.CanTrain(raceId))
                continue;
            
            int rating = GameConstants.TRAIN_SKILL_LEVEL * GameConstants.MapSize / 100;
            rating += GameConstants.MapSize - Misc.points_distance(town.LocCenterX, town.LocCenterY, firmLocX, firmLocY);

            if (rating > bestRating)
            {
                bestTown = town;
                bestRace = raceId;
                bestRating = rating;
                bestFirm = null;
            }
        }
        
        //TODO look at inns

        if (bestFirm != null)
        {
            _generalId = bestFirm.MobilizeWorker(bestWorkerId, InternalConstants.COMMAND_AI);
        }

        if (bestTown != null)
        {
            _generalId = bestTown.Recruit(Skill.SKILL_LEADING, bestRace, InternalConstants.COMMAND_AI);
        }
    }

    private void FindPeasant(int firmLocX, int firmLocY)
    {
        Location targetLocation = World.GetLoc(firmLocX, firmLocY);

        Town bestTown = null;
        int bestRace = 0;
        int bestRating = 0;
        foreach (Town town in TownArray)
        {
            if (town.NationId != Nation.nation_recno)
                continue;

            //TODO other region
            if (town.RegionId != targetLocation.RegionId)
                continue;

            //TODO take race into account
            int raceId = town.MajorityRace();
            if (!town.CanRecruit(raceId))
                continue;
            
            int rating = GameConstants.MapSize - Misc.points_distance(town.LocCenterX, town.LocCenterY, firmLocX, firmLocY);

            if (rating > bestRating)
            {
                bestTown = town;
                bestRace = raceId;
                bestRating = rating;
            }
        }
        
        if (bestTown != null)
        {
            _generalId = bestTown.Recruit(-1, bestRace, InternalConstants.COMMAND_AI);
        }
    }
}