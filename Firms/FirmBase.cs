using System;

namespace TenKingdoms;

public class FirmBase : Firm
{
    public int god_id;
    public int god_unit_recno;

    public double pray_points;

    private GodRes GodRes => Sys.Instance.GodRes;

    public FirmBase()
    {
        FirmSkillId = Skill.SKILL_PRAYING;
    }

    protected override void InitDerived()
    {
        pray_points = 0.0;

        NationArray[NationId].base_count_array[RaceId - 1]++;

        god_id = 0;
        god_unit_recno = 0;

        for (int i = 1; i <= GodRes.god_info_array.Length; i++)
        {
            if (GodRes[i].race_id == RaceId && GodRes[i].is_nation_know(NationId))
            {
                god_id = i;
                break;
            }
        }
    }

    protected override void DeinitDerived()
    {
        NationArray[NationId].base_count_array[RaceId-1]--;
    }

    public override void AssignUnit(int unitId)
    {
        Unit unit = UnitArray[unitId];

        //------- if this is a construction worker -------//

        if (unit.Skill.SkillId == Skill.SKILL_CONSTRUCTION)
        {
            SetBuilder(unitId);
            return;
        }

        //------ only assign units of the right race ------//

        if (unit.RaceId != RaceId)
            return;

        //-------- assign the unit ----------//

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
        //---- reset the team member count of the general ----//

        if (newOverseerId != 0)
        {
            UnitArray[newOverseerId].TeamInfo.Members.Clear();
        }

        base.AssignOverseer(newOverseerId);
    }

    public override void ChangeNation(int newNationRecno)
    {
        //--- update the UnitInfo vars of the workers in this firm ---//

        foreach (Worker worker in Workers)
        {
            UnitRes[worker.unit_id].unit_change_nation(newNationRecno, NationId, worker.rank_id);
        }

        //------ update base_count_array[] --------//

        NationArray[NationId].base_count_array[RaceId - 1]--;

        NationArray[newNationRecno].base_count_array[RaceId - 1]++;

        //----- change the nation recno of the god invoked by the base if there is any ----//

        if (god_unit_recno != 0 && !UnitArray.IsDeleted(god_unit_recno))
            UnitArray[god_unit_recno].ChangeNation(newNationRecno);

        //-------- change the nation of this firm now ----------//

        base.ChangeNation(newNationRecno);
    }

    public override void NextDay()
    {
        base.NextDay();

        //--------------------------------------//

        CalcProductivity();

        //--------------------------------------//

        if (Info.TotalDays % 15 == FirmId % 15) // once a week
        {
            train_unit();
            recover_hit_point();
        }

        //------- increase pray points --------//

        if (OverseerId != 0 && pray_points < GameConstants.MAX_PRAY_POINTS)
        {
            if (Config.fast_build)
                pray_points += Productivity / 10;
            else
                pray_points += Productivity / 100;

            if (pray_points > GameConstants.MAX_PRAY_POINTS)
                pray_points = GameConstants.MAX_PRAY_POINTS;
        }

        //------ validate god_unit_recno ------//

        if (god_unit_recno != 0)
        {
            if (UnitArray.IsDeleted(god_unit_recno))
                god_unit_recno = 0;
        }
    }

    public override void ProcessAI()
    {
        think_assign_unit();

        think_invoke_god();
    }

    public bool can_invoke()
    {
        //----- if the base's god creature has been destroyed -----//

        if (god_unit_recno != 0 && UnitArray.IsDeleted(god_unit_recno))
            god_unit_recno = 0;

        //---------------------------------------------------------//

        // one base can only support one god
        // there must be at least 10% of the maximum pray points to cast a creature
        return god_unit_recno == 0 && OverseerId != 0 &&
               pray_points >= GameConstants.MAX_PRAY_POINTS / 10.0;
    }

    public void invoke_god()
    {
        god_unit_recno = GodRes[god_id].invoke(FirmId, LocCenterX, LocCenterY);
    }

    //-------------- multiplayer checking codes ---------------//
    //virtual	uint8_t crc8();
    //virtual	void	clear_ptr();
    //virtual	void	init_crc(FirmBaseCrc *c);

    //public void 		put_info(int refreshFlag);
    //public int		detect_info();
    //private void 		disp_base_info(int dispY1, int refreshFlag);
    //private void 		disp_god_info(int dispY1, int refreshFlag);

    private void train_unit()
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
            //------- increase prayer skill -----------//

            if (worker.skill_level < overseerSkill)
            {
                int incValue = Math.Max(20, overseerSkill - worker.skill_level)
                    * worker.hit_points / worker.max_hit_points()
                    * (100 + worker.skill_potential * 2) / 100;

                // with random factors, resulting in 75% to 125% of the original
                int levelMinor = worker.skill_level_minor + incValue;

                while (levelMinor >= 100)
                {
                    levelMinor -= 100;
                    worker.skill_level++;
                }

                worker.skill_level_minor = levelMinor;
            }
        }
    }

    private void recover_hit_point()
    {
        foreach (Worker worker in Workers)
        {
            if (worker.hit_points < worker.max_hit_points())
                worker.hit_points++;
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
                nation.add_action(LocX1, LocY1, -1, -1,
                    Nation.ACTION_AI_ASSIGN_OVERSEER, FIRM_BASE);
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
        if (pray_points < GameConstants.MAX_PRAY_POINTS || !can_invoke())
            return;

        invoke_god();
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