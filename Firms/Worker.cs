using System;

namespace TenKingdoms;

public class Worker
{
    public int RaceId { get; set; }
    public int UnitId { get; set; }
    public int NameId { get; set; }
    public int RankId { get; set; }

    public int SkillId { get; set; }
    public int SkillLevel { get; set; }
    public int SkillLevelMinor { get; set; }
    public int SkillPotential { get; set; }

    public int HitPoints { get; set; }
    public int CombatLevel { get; set; }
    public int CombatLevelMinor { get; set; }

    public int SpyId { get; set; }
    public int TownId { get; set; }
    public int WorkerLoyalty { get; set; } // only for firms with LiveInTown being false
    public int ExtraPara { get; set; } // weapon version for weapons and power attack points for human units

    private RaceRes RaceRes => Sys.Instance.RaceRes;
    private UnitRes UnitRes => Sys.Instance.UnitRes;
    private FirmArray FirmArray => Sys.Instance.FirmArray;
    private TownArray TownArray => Sys.Instance.TownArray;
    private UnitArray UnitArray => Sys.Instance.UnitArray;
    private SpyArray SpyArray => Sys.Instance.SpyArray;

    public Worker()
    {
    }

    public void InitPotential()
    {
        if (Misc.Random(10) == 0) // 1 out of 10 has a higher than normal potential in this skill
        {
            SkillPotential = 50 + Misc.Random(51); // 50 to 100 potential
        }
    }

    public void ChangeHitPoints(int changePoints)
    {
        int newHitPoints = HitPoints + changePoints;
        int maxHitPoints = MaxHitPoints();

        newHitPoints = Math.Min(maxHitPoints, newHitPoints);
        HitPoints = Math.Max(0, newHitPoints);
    }

    public int MaxHitPoints()
    {
        return UnitRes[UnitId].hit_points * CombatLevel / 100;
    }

    public int MaxAttackRange()
    {
        int maxRange = 0;
        int attackCount = UnitRes[UnitId].attack_count;

        for (int i = 0; i < attackCount; i++)
        {
            AttackInfo attackInfo = UnitRes.GetAttackInfo(UnitRes[UnitId].first_attack + i);
            if (CombatLevel >= attackInfo.combat_level && attackInfo.attack_range > maxRange)
            {
                maxRange = attackInfo.attack_range;
            }
        }

        return maxRange;
    }
    
    public int Loyalty()
    {
        // TODO: town may be deleted if last worker is out
        return TownId != 0 ? (int)TownArray[TownId].RacesLoyalty[RaceId - 1] : WorkerLoyalty;
    }

    public int TargetLoyalty(int firmId)
    {
        if (TownId != 0) // if the worker lives in a town
        {
            return (int)TownArray[TownId].RacesLoyalty[RaceId - 1];
        }

        Firm firm = FirmArray[firmId];

        if (firm.OverseerId != 0)
        {
            Unit overseer = UnitArray[firm.OverseerId];

            int overseerSkill = overseer.Skill.GetSkillLevel(Skill.SKILL_LEADING);
            int targetLoyalty = 30 + overseerSkill / 2;

            //---------------------------------------------------//
            //
            // Soldiers with higher combat and leadership skill
            // will get discontented if they are led by a general with low leadership.
            //
            //---------------------------------------------------//

            targetLoyalty -= CombatLevel / 2;

            if (SkillLevel > overseerSkill)
                targetLoyalty -= SkillLevel - overseerSkill;

            if (overseer.Rank == Unit.RANK_KING)
                targetLoyalty += 20;

            if (RaceRes.is_same_race(RaceId, overseer.RaceId))
                targetLoyalty += 20;

            if (targetLoyalty < 0)
                targetLoyalty = 0;

            if (targetLoyalty > 100)
                targetLoyalty = 100;

            return targetLoyalty;
        }
        else //-- if there is no overseer, just return the current loyalty --//
        {
            return WorkerLoyalty;
        }
    }

    public void ChangeLoyalty(int loyaltyChange)
    {
        if (TownId != 0) // for those live in town, their loyalty are based on town people loyalty.
            return;

        int newLoyalty = WorkerLoyalty + loyaltyChange;
        newLoyalty = Math.Min(100, newLoyalty);
        WorkerLoyalty = Math.Max(0, newLoyalty);
    }
    
    public bool IsNation(int firmId, int nationId, bool checkSpy = false)
    {
        if (checkSpy && SpyId != 0)
            return SpyArray[SpyId].TrueNationId == nationId;

        if (TownId != 0)
            return TownArray[TownId].NationId == nationId;
        
        return FirmArray[firmId].NationId == nationId;
    }
}
