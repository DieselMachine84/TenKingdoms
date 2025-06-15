using System;

namespace TenKingdoms;

public class Worker
{
    public int race_id;
    public int unit_id;
    public int town_recno;
    public int name_id;

    public int skill_id;
    public int skill_level;
    public int skill_level_minor;
    public int skill_potential;

    public int combat_level;
    public int combat_level_minor;

    public int spy_recno;

    public int rank_id;
    public int worker_loyalty; // only for firms with live_in_town being 0
    public int hit_points;
    public int extra_para; // weapon version for weapons and power attack points for human units

    private RaceRes RaceRes => Sys.Instance.RaceRes;
    private UnitRes UnitRes => Sys.Instance.UnitRes;
    private UnitArray UnitArray => Sys.Instance.UnitArray;
    private SpyArray SpyArray => Sys.Instance.SpyArray;
    private FirmArray FirmArray => Sys.Instance.FirmArray;
    private TownArray TownArray => Sys.Instance.TownArray;

    public Worker()
    {
    }

    public int max_hit_points()
    {
        return UnitRes[unit_id].hit_points * combat_level / 100;
    }

    public int loyalty()
    {
        if (town_recno != 0) // if the worker lives in a town
            return Convert.ToInt32(TownArray[town_recno].RacesLoyalty[race_id - 1]);
        else
            return worker_loyalty;
    }

    public int target_loyalty(int firmRecno)
    {
        if (town_recno != 0) // if the worker lives in a town
        {
            return Convert.ToInt32(TownArray[town_recno].RacesLoyalty[race_id - 1]);
        }
        else
        {
            Firm firmPtr = FirmArray[firmRecno];

            if (firmPtr.overseer_recno != 0)
            {
                Unit overseerUnit = UnitArray[firmPtr.overseer_recno];

                int overseerSkill = overseerUnit.Skill.GetSkillLevel(Skill.SKILL_LEADING);
                int targetLoyalty = 30 + overseerSkill / 2;

                //---------------------------------------------------//
                //
                // Soldiers with higher combat and leadership skill
                // will get discontented if they are led by a general
                // with low leadership.
                //
                //---------------------------------------------------//

                targetLoyalty -= combat_level / 2;

                if (skill_level > overseerSkill)
                    targetLoyalty -= skill_level - overseerSkill;

                if (overseerUnit.Rank == Unit.RANK_KING)
                    targetLoyalty += 20;

                if (RaceRes.is_same_race(race_id, overseerUnit.RaceId))
                    targetLoyalty += 20;

                if (targetLoyalty < 0)
                    targetLoyalty = 0;

                if (targetLoyalty > 100)
                    targetLoyalty = 100;

                return targetLoyalty;
            }
            else //-- if there is no overseer, just return the current loyalty --//
            {
                return worker_loyalty;
            }
        }
    }

    public bool is_nation(int firmRecno, int nationRecno, bool checkSpy = false)
    {
        if (checkSpy && spy_recno != 0)
            return SpyArray[spy_recno].TrueNationId == nationRecno;

        if (town_recno != 0)
            return TownArray[town_recno].NationId == nationRecno;
        else
            return FirmArray[firmRecno].nation_recno == nationRecno;
    }

    public void init_potential()
    {
        if (Misc.Random(10) == 0) // 1 out of 10 has a higher than normal potential in this skill
        {
            skill_potential = 50 + Misc.Random(51); // 50 to 100 potential
        }
    }

    public void change_loyalty(int loyaltyChange)
    {
        if (town_recno != 0) // for those live in town, their loyalty are based on town people loyalty.
            return;

        int newLoyalty = worker_loyalty + loyaltyChange;

        newLoyalty = Math.Min(100, newLoyalty);
        worker_loyalty = Math.Max(0, newLoyalty);
    }

    public void change_hit_points(int changePoints)
    {
        int newHitPoints = hit_points + changePoints;
        int maxHitPoints = max_hit_points();

        newHitPoints = Math.Min(maxHitPoints, newHitPoints);
        hit_points = Math.Max(0, newHitPoints);
    }

    public int max_attack_range()
    {
        int maxRange = 0;
        int attackCount = UnitRes[unit_id].attack_count;

        for (int i = 0; i < attackCount; i++)
        {
            AttackInfo attackInfo = UnitRes.GetAttackInfo(UnitRes[unit_id].first_attack + i);
            if (combat_level >= attackInfo.combat_level && attackInfo.attack_range > maxRange)
            {
                maxRange = attackInfo.attack_range;
            }
        }

        return maxRange;
    }
}
