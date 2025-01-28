namespace TenKingdoms;

public class FirmResearch : Firm
{
    public int tech_id; // the id. of the tech this firm is currently researching
    public double complete_percent; // percent completed on researching the current technology

    private TechRes TechRes => Sys.Instance.TechRes;

    public FirmResearch()
    {
        firm_skill_id = Skill.SKILL_RESEARCH;
    }

    protected override void deinit_derived()
    {
        terminate_research();
    }

    public override void next_day()
    {
        base.next_day();

        //----------- update population -------------//

        recruit_worker();

        //-------- train up the skill ------------//

        update_worker();

        //--------- calculate productivity ----------//

        calc_productivity();

        //--------- process research ----------//

        process_research();
    }

    public override void change_nation(int newNationRecno)
    {
        terminate_research();

        //-------- change the nation of this firm now ----------//

        base.change_nation(newNationRecno);
    }

    public override bool is_operating()
    {
        return productivity > 0 && tech_id != 0;
    }

    public void start_research(int techId, int remoteAction)
    {
        TechInfo techInfo = TechRes[techId];

        //if( !remoteAction && remote.is_enable())
        //{
        //// packet structure : <firm recno> <tech Id>
        //short *shortPtr = (short *)remote.new_send_queue_msg(MSG_F_RESEARCH_START, 2*sizeof(short) );
        //shortPtr[0] = firm_recno;
        //shortPtr[1] = (short) techId;
        //return;
        //}

        //---- if the firm currently is already researching something ---//

        if (tech_id != 0)
            terminate_research();

        //-------- set self parameters ---------//

        tech_id = techId;

        //------- set TechRes parameters -------//

        techInfo.inc_nation_is_researching(nation_recno);
    }

    public void process_research()
    {
        if (tech_id == 0)
            return;

        //------- make a progress with the research ------//

        TechInfo techInfo = TechRes[tech_id];
        double progressPoint;

        if (Config.fast_build && nation_recno == NationArray.player_recno)
            progressPoint = productivity / 100.0 + 0.5;
        else
            progressPoint = productivity / 300.0;

        int newLevel = techInfo.get_nation_tech_level(nation_recno) + 1;
        double levelDivider = (newLevel + 1) / 2.0; // from 1.0 to 2.0

        // more complex and higher level technology will take longer to research
        progressPoint = progressPoint * 30.0 / techInfo.complex_level / levelDivider;

        // techInfo.progress() will reset tech_id if the current research level is the MAX tech level, so we have to save it now
        int techId1 = tech_id;

        if (techInfo.progress(nation_recno, progressPoint))
        {
            if (tech_id != 0) // techInfo.progress() may have called terminate_research() if the tech level reaches the maximum
            {
                int techId2 = tech_id;

                research_complete();

                //----- research next level technology automatically -----//

                if (!firm_ai) // for player's firm only
                {
                    if (techInfo.get_nation_tech_level(nation_recno) < techInfo.max_tech_level)
                    {
                        start_research(techId2, InternalConstants.COMMAND_AUTO);

                        if (firm_recno == FirmArray.selected_recno)
                            Info.disp();
                    }
                }
            }

            //--------- add news ---------//

            if (own_firm())
            {
                NewsArray.tech_researched(techId1, TechRes[techId1].get_nation_tech_level(nation_recno));

                SERes.far_sound(center_x, center_y, 1, 'F', firm_id, "FINS", 'S',
                    UnitRes[TechRes[techId1].unit_id].sprite_id);
                if (firm_recno == FirmArray.selected_recno)
                    Info.disp();
            }
        }
    }

    public void terminate_research()
    {
        if (tech_id == 0)
            return;

        TechRes[tech_id].dec_nation_is_researching(nation_recno);

        tech_id = 0; // reset parameters
        complete_percent = 0.0;
    }

    public void research_complete()
    {
        int techId = tech_id; // backup tech_id

        TechRes[tech_id].dec_nation_is_researching(nation_recno);

        tech_id = 0; // reset parameters
        complete_percent = 0.0;
    }

    public override void process_ai()
    {
        //---- think about which technology to research ----//

        if (tech_id == 0)
            think_new_research();

        //------- recruit workers ---------//

        //TODO do not recruit workers. Sell firm if it is not linked to a village
        if (Info.TotalDays % 15 == firm_recno % 15)
        {
            if (workers.Count < MAX_WORKER)
                ai_recruit_worker();
        }

        //----- think about closing down this firm -----//

        if (Info.TotalDays % 30 == firm_recno % 30)
        {
            if (think_del())
                return;
        }
    }

    //-------- AI actions ---------//

    private void think_new_research()
    {
        int bestTechId = 0, bestRating = 0;

        for (int techId = TechRes.tech_info_array.Length; techId > 0; techId--)
        {
            TechInfo techInfo = TechRes[techId];

            if (techInfo.can_research(nation_recno))
            {
                int curRating = 100 + (techInfo.is_nation_researching(nation_recno) ? 1 : 0) * 20;

                if (curRating > bestRating || (curRating == bestRating && Misc.Random(2) == 0))
                {
                    bestTechId = techId;
                    bestRating = curRating;
                }
            }
        }

        //------------------------------------//

        if (bestTechId != 0)
            start_research(bestTechId, InternalConstants.COMMAND_AI);
    }

    private bool think_del()
    {
        //----- if all technologies have been researched -----//

        // all technologies have been researched
        if (NationArray[nation_recno].total_tech_level() == TechRes.total_tech_level)
        {
            ai_del_firm();
            return true;
        }

        //----------------------------------------------// 

        if (workers.Count > 0)
            return false;

        //-- check whether the firm is linked to any towns or not --//

        for (int i = 0; i < linked_town_array.Count; i++)
        {
            if (linked_town_enable_array[i] == InternalConstants.LINK_EE)
                return false;
        }

        //------------------------------------------------//

        ai_del_firm();

        return true;
    }
}