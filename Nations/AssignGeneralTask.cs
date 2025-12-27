using System;
using System.Collections.Generic;

namespace TenKingdoms;

// Assign general when
//  1. Camp or base has no general
//  2. There is a better general - TODO

public class AssignGeneralTask : AITask, IUnitTask
{
    private int _generalId;
    private bool _generalSent;
    private bool _shouldCancel;
    public int FirmId { get; }
    public int UnitId => _generalId;
    
    public AssignGeneralTask(Nation nation, int firmId) : base(nation)
    {
        FirmId = firmId;
    }

    public override bool ShouldCancel()
    {
        if (_shouldCancel)
            return true;

        if (FirmIsDeletedOrChangedNation(FirmId))
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
            _generalId = FindGeneral(firm);
        
        if (_generalId == 0)
            _generalId = FindPeasant(firm);

        if (_generalId == 0)
            return;
        
        Unit general = UnitArray[_generalId];
        if (general.UnitMode == UnitConstants.UNIT_MODE_ON_SHIP)
        {
            Nation.AddSailShipTask((UnitMarine)UnitArray[general.UnitModeParam], firm.LocCenterX, firm.LocCenterY);
            return;
        }
        
        if (general.UnitMode == UnitConstants.UNIT_MODE_UNDER_TRAINING)
            return;

        if (general.UnitMode == UnitConstants.UNIT_MODE_OVERSEE)
        {
            _shouldCancel = true;
            return;
        }
        
        if (!_generalSent)
        {
            if (general.RegionId() == firm.RegionId)
            {
                if (general.Skill.SkillId == Skill.SKILL_LEADING && general.Rank == Unit.RANK_SOLDIER)
                    general.SetRank(Unit.RANK_GENERAL);

                general.Assign(firm.LocX1, firm.LocY1);
                _generalSent = true;
            }
            else
            {
                UnitMarine transport = Nation.FindTransportShip(Nation.GetSeaRegion(general.RegionId(), firm.RegionId));
                if (transport != null)
                {
                    List<int> units = new List<int> { _generalId };
                    UnitArray.AssignToShip(transport.NextLocX, transport.NextLocY, false, units, InternalConstants.COMMAND_AI, transport.SpriteId);
                    _generalSent = true;
                }
                else
                {
                    FirmHarbor harbor = Nation.FindHarbor(general.RegionId(), firm.RegionId);
                    Nation.AddBuildShipTask(harbor, UnitConstants.UNIT_TRANSPORT);
                }
            }
        }
        else
        {
            //TODO check that general is on the way, not stuck
            if (general.IsAIAllStop())
                _generalSent = false;
        }
    }

    private int FindGeneral(Firm firm)
    {
        int generalId = FindGeneralInRegion(firm, firm.RegionId);
        
        if (generalId == 0)
        {
            List<int> connectedLands = GetConnectedLands(firm.RegionId);
            foreach (int landRegionId in connectedLands)
            {
                if (landRegionId == firm.RegionId)
                    continue;

                //TODO maybe there is no harbor but we can send ship for the settler
                FirmHarbor harbor = Nation.FindHarbor(landRegionId, firm.RegionId);
                if (harbor != null)
                {
                    generalId = FindGeneralInRegion(firm, landRegionId);
                    
                    //TODO do not choose the first one, select from all available
                    if (generalId != 0)
                        break;
                }
            }
        }

        return generalId;
    }
    
    private int FindGeneralInRegion(Firm firm, int regionId)
    {
        Firm bestFirm = null;
        int bestWorkerId = 0;
        int bestRating = Int16.MinValue;
        foreach (Firm otherFirm in FirmArray)
        {
            if (otherFirm.NationId != Nation.nation_recno)
                continue;
            
            if (otherFirm.FirmType != Firm.FIRM_CAMP)
                continue;

            if (otherFirm.RegionId != regionId)
                continue;

            for (int i = 0; i < otherFirm.Workers.Count; i++)
            {
                int rating = otherFirm.Workers[i].SkillLevel * GameConstants.MapSize / 100;
                rating += GameConstants.MapSize - Misc.FirmsDistance(firm, otherFirm);
                //TODO take general race into account

                if (rating > bestRating)
                {
                    bestFirm = otherFirm;
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

            if (town.RegionId != regionId)
                continue;

            //TODO take race into account
            int raceId = town.MajorityRace();
            if (!town.CanTrain(raceId))
                continue;
            
            int rating = GameConstants.TRAIN_SKILL_LEVEL * GameConstants.MapSize / 100;
            rating += GameConstants.MapSize - Misc.FirmTownDistance(firm, town);

            if (rating > bestRating)
            {
                bestTown = town;
                bestRace = raceId;
                bestRating = rating;
                bestFirm = null;
            }
        }
        
        //TODO look at inns

        int generalId = 0;
        if (bestFirm != null)
        {
            generalId = bestFirm.MobilizeWorker(bestWorkerId, InternalConstants.COMMAND_AI);
        }

        if (bestTown != null)
        {
            generalId = bestTown.Recruit(Skill.SKILL_LEADING, bestRace, InternalConstants.COMMAND_AI);
        }

        return generalId;
    }

    private int FindPeasant(Firm firm)
    {
        int generalId = FindPeasantInRegion(firm, firm.RegionId);
        
        if (generalId == 0)
        {
            List<int> connectedLands = GetConnectedLands(firm.RegionId);
            foreach (int landRegionId in connectedLands)
            {
                if (landRegionId == firm.RegionId)
                    continue;

                //TODO maybe there is no harbor but we can send ship for the settler
                FirmHarbor harbor = Nation.FindHarbor(landRegionId, firm.RegionId);
                if (harbor != null)
                {
                    generalId = FindPeasantInRegion(firm, landRegionId);
                    
                    //TODO do not choose the first one, select from all available
                    if (generalId != 0)
                        break;
                }
            }
        }

        return generalId;
    }
    
    private int FindPeasantInRegion(Firm firm, int regionId)
    {
        Town bestTown = null;
        int bestRace = 0;
        int bestRating = Int16.MaxValue;
        foreach (Town town in TownArray)
        {
            if (town.NationId != Nation.nation_recno)
                continue;

            if (town.RegionId != regionId)
                continue;

            //TODO take race into account
            int raceId = town.MajorityRace();
            if (!town.CanRecruit(raceId))
                continue;
            
            int rating = Misc.FirmTownDistance(firm, town);

            if (rating < bestRating)
            {
                bestTown = town;
                bestRace = raceId;
                bestRating = rating;
            }
        }

        return bestTown != null ? bestTown.Recruit(-1, bestRace, InternalConstants.COMMAND_AI) : 0;
    }
}