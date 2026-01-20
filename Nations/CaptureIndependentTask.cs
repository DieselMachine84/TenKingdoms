using System;
using System.Collections.Generic;

namespace TenKingdoms;

// Start capture independent village when
//  1. There is an appropriate leader
//  2. Village is not too close to the enemy - TODO
//  3. Kingdom has enough money - TODO
//  4. Do not capture on enemy island - TODO

// Choose an appropriate leader from
//  1. Generals from forts and bases
//  2. Soldiers in forts
//  3. Leaders in inns
//  4. Leadership should depend on reputation - TODO

// Rate villages by
//  1. Population
//  2. Number of races
//  3. Distance from kingdom towns
//  4. Distance from enemies
//  5. Region

// When capturing
//  1. Replace general by more skilled
//  2. Build another camp if there are more than two races
//  3. Build mine, factory, research or war factory
//  4. Build market
//  5. Use cash for capture
//  6. Attack

// Decrease race resistance until acceptable value. This value depends on
//  1. Population
//  2. Preferences
//  3. If another kingdom wants to capture the same village - TODO

public class CaptureIndependentTask : AITask
{
    private int _builderId;
    private bool _builderSent;
    private bool _noPlaceToBuild;
    private readonly int[] _generalIds = new int[GameConstants.MAX_RACE];
    private bool _shouldCancel;
    
    public int TownId { get; }

    public CaptureIndependentTask(Nation nation, int townId) : base(nation)
    {
        TownId = townId;
    }

    public override bool ShouldCancel()
    {
        if (_shouldCancel)
            return true;
        
        if (_noPlaceToBuild)
            return true;
        
        if (TownArray.IsDeleted(TownId))
            return true;

        Town town = TownArray[TownId];
        if (town.NationId != 0 || town.RebelId != 0)
            return true;
        
        return false;
    }

    public override void Cancel()
    {
        if (_builderId != 0 && !UnitArray.IsDeleted(_builderId))
        {
            Unit builder = UnitArray[_builderId];
            if (builder.IsVisible())
                builder.Stop2();
        }
    }

    public override void Process()
    {
        Town town = TownArray[TownId];
        UpdateCapturers(town);
        SendGeneral(town);
        BuildFactoryResearch();
        GrantMoney();
        Attack();
    }

    private void UpdateCapturers(Town town)
    {
        for (int i = 0; i < GameConstants.MAX_RACE; i++)
        {
            if (town.RacesPopulation[i] == 0)
                _generalIds[i] = 0;
        }
        
        for (int i = 0; i < _generalIds.Length; i++)
        {
            if (_generalIds[i] != 0 && UnitArray.IsDeleted(_generalIds[i]))
                _generalIds[i] = 0;
        }

        for (int i = 0; i < town.LinkedFirms.Count; i++)
        {
            Firm firm = FirmArray[town.LinkedFirms[i]];
            if (firm.NationId != NationId || firm.FirmType != Firm.FIRM_CAMP)
                continue;

            FirmCamp camp = (FirmCamp)firm;
            if (camp.OverseerId == 0)
                continue;

            Unit overseer = UnitArray[camp.OverseerId];
            if (town.RacesPopulation[overseer.RaceId - 1] > 0 && _generalIds[overseer.RaceId - 1] == camp.OverseerId)
            {
                int raceResistance = (int)town.RacesResistance[overseer.RaceId - 1, NationId - 1];
                int raceTargetResistance = town.RacesTargetResistance[overseer.RaceId - 1, NationId - 1];
                if (raceTargetResistance == -1 || raceResistance - raceTargetResistance < 3)
                    _generalIds[overseer.RaceId - 1] = 0;
            }
        }
    }

    private bool HasEmptyCamp(Town town)
    {
        for (int i = 0; i < town.LinkedFirms.Count; i++)
        {
            Firm firm = FirmArray[town.LinkedFirms[i]];
            if (firm.NationId != NationId || firm.FirmType != Firm.FIRM_CAMP)
                continue;

            FirmCamp camp = (FirmCamp)firm;
            if (camp.OverseerId == 0)
                return true;

            bool isCapturingCamp = false;
            foreach (var generalId in _generalIds)
            {
                if (camp.OverseerId == generalId)
                {
                    isCapturingCamp = true;
                }
            }

            if (!isCapturingCamp)
                return true;
        }

        return false;
    }

    private void SendGeneral(Town town)
    {
        List<(int, int)> racesPopulation = new List<(int, int)>();
        for (int i = 0; i < GameConstants.MAX_RACE; i++)
        {
            int racePopulation = town.RacesPopulation[i];
            if (racePopulation > 0)
                racesPopulation.Add((i + 1, racePopulation));
        }
        
        racesPopulation.Sort((rp1, rp2) => rp2.Item2 - rp1.Item2);

        for (int i = 0; i < racesPopulation.Count; i++)
        {
            int race = racesPopulation[i].Item1;
            int population = racesPopulation[i].Item2;
            
            if (_generalIds[race - 1] != 0)
                continue;
            
            int acceptableResistance = Nation.PrefAcceptableIndependentVillageResistance;
            if (population <= 5)
                acceptableResistance += 20 / population;
            
            int raceResistance = (int)town.RacesResistance[race - 1, NationId - 1];
            if (raceResistance > acceptableResistance)
            {
                var (selectedFirm, selectedOverseerId, selectedWorkerId, selectedInnUnitId, bestRating) =
                    Nation.FindBestGeneral(town, race, Nation.PrefMinGeneralLevelForCapturing);

                if (selectedFirm != null)
                {
                    if (HasEmptyCamp(town))
                    {
                        if (selectedOverseerId != 0)
                        {
                            //TODO mobilize and move with soldiers
                            //TODO find a replacement for this overseer first
                            int overseerId = selectedFirm.MobilizeOverseer();
                            if (overseerId != 0)
                            {
                                Nation.AddAssignGeneralTask(selectedFirm.FirmId, overseerId);
                                _generalIds[race - 1] = overseerId;
                            }
                        }

                        if (selectedWorkerId != 0)
                        {
                            int soldierId = selectedFirm.MobilizeWorker(selectedWorkerId, InternalConstants.COMMAND_AI);
                            if (soldierId != 0)
                            {
                                Nation.AddAssignGeneralTask(selectedFirm.FirmId, soldierId);
                                _generalIds[race - 1] = soldierId;
                            }
                        }

                        if (selectedInnUnitId != 0)
                        {
                            FirmInn inn = (FirmInn)selectedFirm;
                            int unitId = inn.Hire(selectedInnUnitId);
                            if (unitId != 0)
                            {
                                Nation.AddAssignGeneralTask(selectedFirm.FirmId, unitId);
                                _generalIds[race - 1] = unitId;
                            }
                        }
                    }
                    else
                    {
                        int linkedCampsCount = 0;
                        for (int j = 0; j < town.LinkedFirms.Count; j++)
                        {
                            Firm firm = FirmArray[town.LinkedFirms[j]];
                            if (firm.NationId == NationId && firm.FirmType == Firm.FIRM_CAMP)
                                linkedCampsCount++;
                        }
                        
                        if (linkedCampsCount < 2)
                            Nation.AddBuildCampTask(town.TownId);
                    }
                }
            }
        }
    }

    private void BuildFactoryResearch()
    {
        //
    }

    private void GrantMoney()
    {
        //
    }

    private void Attack()
    {
        //
    }

    public bool IsUnitOnTask(int unitId)
    {
        foreach (var generalId in _generalIds)
            if (generalId == unitId)
                return true;

        return false;
    }
}
