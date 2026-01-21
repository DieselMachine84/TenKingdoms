using System;

namespace TenKingdoms;

public class FirmBase : Firm
{
    public int GodId { get; private set; }
    private int GodUnitId { get; set; }
    public double PrayPoints { get; set; }

    private GodRes GodRes => Sys.Instance.GodRes;

    public FirmBase()
    {
        FirmSkillId = Skill.SKILL_PRAYING;
    }

    protected override void InitDerived()
    {
        NationArray[NationId].BaseCounts[RaceId - 1]++;

        for (int i = 1; i <= GodRes.god_info_array.Length; i++)
        {
            if (GodRes[i].race_id == RaceId && GodRes[i].is_nation_know(NationId))
            {
                GodId = i;
                break;
            }
        }
    }

    protected override void DeinitDerived()
    {
        NationArray[NationId].BaseCounts[RaceId-1]--;
    }

    public override void AssignUnit(int unitId)
    {
        Unit unit = UnitArray[unitId];

        if (unit.Skill.SkillId == Skill.SKILL_CONSTRUCTION)
        {
            SetBuilder(unitId);
            return;
        }

        if (unit.RaceId != RaceId)
            return;

        if (unit.Rank == Unit.RANK_GENERAL || unit.Rank == Unit.RANK_KING)
        {
            AssignOverseer(unitId);
        }
        else
        {
            AssignWorker(unitId);
        }
    }

    public override void AssignOverseer(int newOverseerId)
    {
        if (newOverseerId != 0)
        {
            Unit unit = UnitArray[newOverseerId];
            unit.TeamInfo.Members.Clear();
        }

        base.AssignOverseer(newOverseerId);
    }

    public override void ChangeNation(int newNationId)
    {
        foreach (Worker worker in Workers)
        {
            UnitRes[worker.UnitId].unit_change_nation(newNationId, NationId, worker.RankId);
        }

        NationArray[NationId].BaseCounts[RaceId - 1]--;

        NationArray[newNationId].BaseCounts[RaceId - 1]++;

        if (GodUnitId != 0 && !UnitArray.IsDeleted(GodUnitId))
            UnitArray[GodUnitId].ChangeNation(newNationId);

        base.ChangeNation(newNationId);
    }

    public override void NextDay()
    {
        base.NextDay();

        CalcProductivity();

        if (Info.TotalDays % 15 == FirmId % 15)
        {
            TrainUnit();
            RecoverHitPoint();
        }

        if (OverseerId != 0 && PrayPoints < GameConstants.MAX_PRAY_POINTS)
        {
            if (Config.fast_build)
                PrayPoints += Productivity / 10;
            else
                PrayPoints += Productivity / 100;

            if (PrayPoints > GameConstants.MAX_PRAY_POINTS)
                PrayPoints = GameConstants.MAX_PRAY_POINTS;
        }

        if (GodUnitId != 0 && UnitArray.IsDeleted(GodUnitId))
            GodUnitId = 0;
    }

    public override void ProcessAI()
    {
        think_assign_unit();

        think_invoke_god();
    }

    public bool CanInvoke()
    {
        if (GodUnitId != 0 && UnitArray.IsDeleted(GodUnitId))
            GodUnitId = 0;

        // one base can only support one god
        // there must be at least 10% of the maximum pray points to cast a creature
        return GodUnitId == 0 && OverseerId != 0 && PrayPoints >= GameConstants.MAX_PRAY_POINTS / 10.0;
    }

    public void InvokeGod()
    {
        GodUnitId = GodRes[GodId].invoke(FirmId, LocCenterX, LocCenterY);
    }

    private void TrainUnit()
    {
        if (OverseerId == 0)
            return;

        Unit overseerUnit = UnitArray[OverseerId];
        int overseerSkill = overseerUnit.Skill.SkillLevel;

        //------- increase the commander's leadership ---------//

        if (Workers.Count > 0 && overseerUnit.Skill.SkillLevel < 100)
        {
            //-- the more soldiers this commander has, the higher the leadership will increase ---//

            int incValue = (int)(3.0 * Workers.Count * overseerUnit.HitPoints / overseerUnit.MaxHitPoints
                * (100.0 + overseerUnit.Skill.SkillPotential * 2.0) / 100.0);

            overseerUnit.Skill.SkillLevelMinor += incValue;

            if (overseerUnit.Skill.SkillLevelMinor >= 100)
            {
                overseerUnit.Skill.SkillLevelMinor -= 100;
                overseerUnit.Skill.SkillLevel++;
            }
        }

        //------- increase the prayer's skill level ------//

        foreach (Worker worker in Workers)
        {
            if (worker.SkillLevel < overseerSkill)
            {
                int incValue = Math.Max(20, overseerSkill - worker.SkillLevel)
                    * worker.HitPoints / worker.MaxHitPoints()
                    * (100 + worker.SkillPotential * 2) / 100;

                // with random factors, resulting in 75% to 125% of the original
                int levelMinor = worker.SkillLevelMinor + incValue;

                while (levelMinor >= 100)
                {
                    levelMinor -= 100;
                    worker.SkillLevel++;
                }

                worker.SkillLevelMinor = levelMinor;
            }
        }
    }

    private void RecoverHitPoint()
    {
        foreach (Worker worker in Workers)
        {
            if (worker.HitPoints < worker.MaxHitPoints())
                worker.HitPoints++;
        }
    }

    //------------- AI actions --------------//

    private void think_assign_unit()
    {
        Nation nation = NationArray[NationId];

        //-------- assign overseer ---------//

        // do not call too often because when an AI action is queued, it will take a while to carry it out
        if (Info.TotalDays % 15 == FirmId % 15)
        {
            if (OverseerId == 0)
            {
                nation.add_action(LocX1, LocY1, -1, -1, Nation.ACTION_AI_ASSIGN_OVERSEER, FIRM_BASE);
            }
        }

        //------- recruit workers ---------//

        if (Info.TotalDays % 15 == FirmId % 15)
        {
            if (Workers.Count < MAX_WORKER)
                AIRecruitWorker();
        }
    }

    private void think_invoke_god()
    {
        if (PrayPoints < GameConstants.MAX_PRAY_POINTS || !CanInvoke())
            return;

        InvokeGod();
    }

    public override void DrawDetails(IRenderer renderer)
    {
        renderer.DrawBaseDetails(this);
    }

    public override void HandleDetailsInput(IRenderer renderer)
    {
        renderer.HandleBaseDetailsInput(this);
    }
}