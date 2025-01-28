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
        firm_skill_id = Skill.SKILL_PRAYING;
    }

    protected override void init_derived()
    {
        pray_points = 0.0;

        NationArray[nation_recno].base_count_array[race_id - 1]++;

        god_id = 0;
        god_unit_recno = 0;

        for (int i = 1; i <= GodRes.god_info_array.Length; i++)
        {
            if (GodRes[i].race_id == race_id && GodRes[i].is_nation_know(nation_recno))
            {
                god_id = i;
                break;
            }
        }
    }

    protected override void deinit_derived()
    {
        NationArray[nation_recno].base_count_array[race_id-1]--;
    }

    public override void assign_unit(int unitRecno)
    {
        Unit unit = UnitArray[unitRecno];

        //------- if this is a construction worker -------//

        if (unit.skill.skill_id == Skill.SKILL_CONSTRUCTION)
        {
            set_builder(unitRecno);
            return;
        }

        //------ only assign units of the right race ------//

        if (unit.race_id != race_id)
            return;

        //-------- assign the unit ----------//

        int rankId = unit.rank_id;

        if (rankId == Unit.RANK_GENERAL || rankId == Unit.RANK_KING)
        {
            assign_overseer(unitRecno);
        }
        else
        {
            assign_worker(unitRecno);
        }
    }

    public override void assign_overseer(int overseerRecno)
    {
        //---- reset the team member count of the general ----//

        if (overseerRecno != 0)
        {
            Unit unit = UnitArray[overseerRecno];

            unit.team_info.member_unit_array.Clear();
        }

        //----- assign the overseer now -------//

        base.assign_overseer(overseerRecno);
    }

    public override void change_nation(int newNationRecno)
    {
        //--- update the UnitInfo vars of the workers in this firm ---//

        foreach (Worker worker in workers)
        {
            UnitRes[worker.unit_id].unit_change_nation(newNationRecno, nation_recno, worker.rank_id);
        }

        //------ update base_count_array[] --------//

        NationArray[nation_recno].base_count_array[race_id - 1]--;

        NationArray[newNationRecno].base_count_array[race_id - 1]++;

        //----- change the nation recno of the god invoked by the base if there is any ----//

        if (god_unit_recno != 0 && !UnitArray.IsDeleted(god_unit_recno))
            UnitArray[god_unit_recno].change_nation(newNationRecno);

        //-------- change the nation of this firm now ----------//

        base.change_nation(newNationRecno);
    }

    public override void next_day()
    {
        base.next_day();

        //--------------------------------------//

        calc_productivity();

        //--------------------------------------//

        if (Info.TotalDays % 15 == firm_recno % 15) // once a week
        {
            train_unit();
            recover_hit_point();
        }

        //------- increase pray points --------//

        if (overseer_recno != 0 && pray_points < GameConstants.MAX_PRAY_POINTS)
        {
            if (Config.fast_build)
                pray_points += productivity / 10;
            else
                pray_points += productivity / 100;

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

    public override void process_ai()
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
        return god_unit_recno == 0 && overseer_recno != 0 &&
               pray_points >= GameConstants.MAX_PRAY_POINTS / 10.0;
    }

    public void invoke_god()
    {
        god_unit_recno = GodRes[god_id].invoke(firm_recno, center_x, center_y);
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
        if (overseer_recno == 0)
            return;

        Unit overseerUnit = UnitArray[overseer_recno];
        int overseerSkill = overseerUnit.skill.skill_level;

        //------- increase the commander's leadership ---------//

        if (workers.Count > 0 && overseerUnit.skill.skill_level < 100)
        {
            //-- the more soldiers this commander has, the higher the leadership will increase ---//

            int incValue = (int)(3.0 * workers.Count * overseerUnit.hit_points / overseerUnit.max_hit_points
                * (100.0 + overseerUnit.skill.skill_potential * 2.0) / 100.0);

            overseerUnit.skill.skill_level_minor += incValue;

            if (overseerUnit.skill.skill_level_minor >= 100)
            {
                overseerUnit.skill.skill_level_minor -= 100;
                overseerUnit.skill.skill_level++;
            }
        }

        //------- increase the prayer's skill level ------//


        foreach (Worker worker in workers)
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
        foreach (Worker worker in workers)
        {
            if (worker.hit_points < worker.max_hit_points())
                worker.hit_points++;
        }
    }

    //------------- AI actions --------------//

    private void think_assign_unit()
    {
        Nation nation = NationArray[nation_recno];

        //-------- assign overseer ---------//

        // do not call too often because when an AI action is queued, it will take a while to carry it out
        if (Info.TotalDays % 15 == firm_recno % 15)
        {
            if (overseer_recno == 0)
            {
                nation.add_action(loc_x1, loc_y1, -1, -1,
                    Nation.ACTION_AI_ASSIGN_OVERSEER, FIRM_BASE);
            }
        }

        //------- recruit workers ---------//

        if (Info.TotalDays % 15 == firm_recno % 15)
        {
            if (workers.Count < MAX_WORKER)
                ai_recruit_worker();
        }
    }

    private void think_invoke_god()
    {
        if (pray_points < GameConstants.MAX_PRAY_POINTS || !can_invoke())
            return;

        invoke_god();
    }
}