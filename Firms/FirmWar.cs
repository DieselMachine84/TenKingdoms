using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class FirmWar : Firm
{
    public int BuildUnitType { get; private set; }
    private long LastProcessBuildFrameNumber { get; set; }
    public double BuildProgressInDays { get; set; }
    public List<int> BuildQueue { get; } = new List<int>();

    public FirmWar()
    {
        FirmSkillId = Skill.SKILL_MFT;
    }

    public override void NextDay()
    {
        base.NextDay();

        RecruitWorker();

        UpdateWorker();

        CalcProductivity();

        if (BuildUnitType != 0)
            ProcessBuild();
        else
            ProcessQueue();
    }

    public override void ChangeNation(int newNationId)
    {
        //--- empty the build queue ---//

        // Note: this fixes a bug with a nation-changed war factory building a weapon that the nation doesn't have,
        //       which leads to a crash (when selecting attack sprite) because CurAttack is not set properly.
        if (BuildUnitType != 0)
            CancelBuildUnit();
        BuildQueue.Clear();

        base.ChangeNation(newNationId);
    }
    
    public override bool IsOperating()
    {
        return Productivity > 0.0 && BuildUnitType != 0;
    }

    public void AddQueue(int unitType, int amount = 1)
    {
        if (amount <= 0)
            return;

        int queueSpace = GameConstants.FIRMWAR_MAX_BUILD_QUEUE - BuildQueue.Count - (BuildUnitType > 0 ? 1 : 0);
        int enqueueAmount = Math.Min(queueSpace, amount);

        for (int i = 0; i < enqueueAmount; i++)
            BuildQueue.Add(unitType);
    }

    public void RemoveQueue(int unitType, int amount = 1)
    {
        if (amount <= 0)
            return;

        for (int i = BuildQueue.Count - 1; i >= 0; i--)
        {
            if (BuildQueue[i] == unitType)
            {
                BuildQueue.RemoveAt(i);
                amount--;
                if (amount == 0)
                    break;
            }
        }

        // If there were less units of unitId in the queue than were requested to be removed
        // then also cancel currently built unit
        if (amount > 0 && BuildUnitType == unitType)
            CancelBuildUnit();
    }

    private void CancelBuildUnit()
    {
        BuildUnitType = 0;
    }
    
    private void ProcessQueue()
    {
        if (BuildQueue.Count == 0)
            return;

        //--- first check if the nation has enough money to build the weapon ---//

        Nation nation = NationArray[NationId];
        if (nation.Cash < UnitRes[BuildQueue[0]].BuildCost)
            return;

        BuildUnitType = BuildQueue[0];
        BuildQueue.RemoveAt(0);
        nation.AddExpense(NationBase.EXPENSE_WEAPON, UnitRes[BuildUnitType].BuildCost);

        LastProcessBuildFrameNumber = Sys.Instance.FrameNumber;
        BuildProgressInDays = 0.0;
    }

    private void ProcessBuild()
    {
        //TODO strange formula
        BuildProgressInDays += (Workers.Count * 6 + Productivity / 2.0) / 100.0;

        LastProcessBuildFrameNumber = Sys.Instance.FrameNumber;

        if (Config.fast_build && NationId == NationArray.PlayerId)
            BuildProgressInDays += 2.0;

        UnitInfo unitInfo = UnitRes[BuildUnitType];
        int totalBuildDays = unitInfo.BuildDays;
        if (BuildProgressInDays > totalBuildDays)
        {
            SpriteInfo spriteInfo = SpriteRes[unitInfo.SpriteId];
            int locX = LocX1;
            int locY = LocY1;

            if (!World.LocateSpace(ref locX, ref locY, LocX2, LocY2, spriteInfo.LocWidth, spriteInfo.LocHeight, unitInfo.MobileType))
            {
                BuildProgressInDays = totalBuildDays + 1;
                return;
            }

            UnitArray.AddUnit(BuildUnitType, NationId, 0, 0, locX, locY);

            if (OwnFirm())
                SERes.far_sound(LocCenterX, LocCenterY, 1, 'F', FirmType, "FINS", 'S', UnitRes[BuildUnitType].SpriteId);

            BuildUnitType = 0;
        }
    }
    
    public override void DrawDetails(IRenderer renderer)
    {
        renderer.DrawWarFactoryDetails(this);
    }

    public override void HandleDetailsInput(IRenderer renderer)
    {
        renderer.HandleWarFactoryDetailsInput(this);
    }

    #region Old AI Functions

    public override void ProcessAI()
    {
        //---- think about which war machine to build ----//

        if (BuildUnitType == 0)
            ThinkNewProduction();

        //------- recruit workers ---------//

        //TODO do not recruit workers. Sell firm if it is not linked to a village
        if (Info.TotalDays % 15 == FirmId % 15)
        {
            if (Workers.Count < MAX_WORKER)
                AIRecruitWorker();
        }

        //----- think about closing down this firm -----//

        if (Info.TotalDays % 30 == FirmId % 30)
        {
            if (ThinkDel())
                return;
        }
    }
    
    private int GetTotalWeaponCount(int unitType)
    {
        int totalWeaponCount = 0;
        foreach (Unit unit in UnitArray)
        {
            if (unit.NationId == NationId && unit.UnitType == unitType)
            {
                totalWeaponCount++;
            }
        }

        foreach (Firm firm in FirmArray)
        {
            if (firm.NationId == NationId && !FirmRes[firm.FirmType].LiveInTown)
            {
                foreach (Worker worker in firm.Workers)
                {
                    if (worker.UnitId == unitType)
                        totalWeaponCount++;
                }
            }
        }

        return totalWeaponCount;
    }
    
    private void ThinkNewProduction()
    {
        //----- first see if we have enough money to build & support the weapon ----//

        if (!ShouldBuildNewWeapon())
            return;

        Nation ownNation = NationArray[NationId];

        //---- calculate the average instance count of all available weapons ---//

        int weaponTypeCount = 0, totalWeaponCount = 0;

        for (int unitType = 1; unitType <= UnitConstants.MAX_UNIT_TYPE; unitType++)
        {
            UnitInfo unitInfo = UnitRes[unitType];

            if (unitInfo.UnitClass != UnitConstants.UNIT_CLASS_WEAPON || ownNation.GetTechLevelByUnitType(unitType) == 0)
                continue;

            if (unitType == UnitConstants.UNIT_EXPLOSIVE_CART) // AI doesn't use Porcupine
                continue;

            weaponTypeCount++;
            totalWeaponCount += GetTotalWeaponCount(unitType);
        }

        if (weaponTypeCount == 0) // none of weapon technologies is available
            return;

        int averageWeaponCount = totalWeaponCount / weaponTypeCount;

        //----- think about which is best to build now ------//

        int bestRating = 0, bestUnitId = 0;

        for (int unitType = 1; unitType <= UnitConstants.MAX_UNIT_TYPE; unitType++)
        {
            UnitInfo unitInfo = UnitRes[unitType];

            if (unitInfo.UnitClass != UnitConstants.UNIT_CLASS_WEAPON)
                continue;

            int techLevel = ownNation.GetTechLevelByUnitType(unitType);

            if (techLevel == 0)
                continue;

            //**BUGHERE, don't produce it yet, it needs a different usage than than others.
            if (unitType == UnitConstants.UNIT_EXPLOSIVE_CART)
                continue;

            int unitCount = GetTotalWeaponCount(unitType);

            int curRating = averageWeaponCount - unitCount + techLevel * 3;

            if (curRating > bestRating)
            {
                bestRating = curRating;
                bestUnitId = unitType;
            }
        }

        if (bestUnitId != 0)
            AddQueue(bestUnitId);
    }

    private bool ShouldBuildNewWeapon()
    {
        //----- first see if we have enough money to build & support the weapon ----//

        Nation nation = NationArray[NationId];

        if (nation.TrueProfit365Days() < 0) // don't build new weapons if we are currently losing money
            return false;

        // if weapon expenses are larger than 30% to 80% of the total income, don't build new weapons
        if (nation.Expense365Days(NationBase.EXPENSE_WEAPON) > nation.Income365Days() * (30 + nation.pref_use_weapon / 2) / 100)
            return false;

        //----- see if there is any space on existing camps -----//

        foreach (int campId in nation.ai_camp_array)
        {
            Firm firm = FirmArray[campId];

            if (firm.RegionId != RegionId)
                continue;

            if (firm.Workers.Count < MAX_WORKER) // there is space in this firm
                return true;
        }

        return false;
    }

    private bool ThinkDel()
    {
        if (Workers.Count > 0)
            return false;

        //-- check whether the firm is linked to any towns or not --//

        for (int i = 0; i < LinkedTowns.Count; i++)
        {
            if (LinkedTownsEnable[i] == InternalConstants.LINK_EE)
                return false;
        }

        AIDelFirm();
        return true;
    }
    
    #endregion
}