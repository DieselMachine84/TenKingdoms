namespace TenKingdoms;

public class FirmResearch : Firm
{
    public int tech_id; // the id. of the tech this firm is currently researching
    public double complete_percent; // percent completed on researching the current technology

    private TechRes TechRes => Sys.Instance.TechRes;

    public FirmResearch()
    {
        FirmSkillId = Skill.SKILL_RESEARCH;
    }

    protected override void DeinitDerived()
    {
        terminate_research();
    }

    public override void NextDay()
    {
        base.NextDay();

        //----------- update population -------------//

        RecruitWorker();

        //-------- train up the skill ------------//

        UpdateWorker();

        //--------- calculate productivity ----------//

        CalcProductivity();

        //--------- process research ----------//

        process_research();
    }

    public override void ChangeNation(int newNationRecno)
    {
        terminate_research();

        //-------- change the nation of this firm now ----------//

        base.ChangeNation(newNationRecno);
    }

    public override bool IsOperating()
    {
        return Productivity > 0 && tech_id != 0;
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

        techInfo.inc_nation_is_researching(NationId);
    }

    public void process_research()
    {
        if (tech_id == 0)
            return;

        //------- make a progress with the research ------//

        TechInfo techInfo = TechRes[tech_id];
        double progressPoint;

        if (Config.fast_build && NationId == NationArray.player_recno)
            progressPoint = Productivity / 100.0 + 0.5;
        else
            progressPoint = Productivity / 300.0;

        int newLevel = techInfo.get_nation_tech_level(NationId) + 1;
        double levelDivider = (newLevel + 1) / 2.0; // from 1.0 to 2.0

        // more complex and higher level technology will take longer to research
        progressPoint = progressPoint * 30.0 / techInfo.complex_level / levelDivider;

        // techInfo.progress() will reset tech_id if the current research level is the MAX tech level, so we have to save it now
        int techId1 = tech_id;

        if (techInfo.progress(NationId, progressPoint))
        {
            if (tech_id != 0) // techInfo.progress() may have called terminate_research() if the tech level reaches the maximum
            {
                int techId2 = tech_id;

                research_complete();

                //----- research next level technology automatically -----//

                if (!AIFirm) // for player's firm only
                {
                    if (techInfo.get_nation_tech_level(NationId) < techInfo.max_tech_level)
                    {
                        start_research(techId2, InternalConstants.COMMAND_AUTO);

                        if (FirmId == FirmArray.selected_recno)
                            Info.disp();
                    }
                }
            }

            //--------- add news ---------//

            if (OwnFirm())
            {
                NewsArray.tech_researched(techId1, TechRes[techId1].get_nation_tech_level(NationId));

                SERes.far_sound(LocCenterX, LocCenterY, 1, 'F', FirmType, "FINS", 'S',
                    UnitRes[TechRes[techId1].unit_id].sprite_id);
                if (FirmId == FirmArray.selected_recno)
                    Info.disp();
            }
        }
    }

    public void terminate_research()
    {
        if (tech_id == 0)
            return;

        TechRes[tech_id].dec_nation_is_researching(NationId);

        tech_id = 0; // reset parameters
        complete_percent = 0.0;
    }

    public void research_complete()
    {
        int techId = tech_id; // backup tech_id

        TechRes[tech_id].dec_nation_is_researching(NationId);

        tech_id = 0; // reset parameters
        complete_percent = 0.0;
    }

    public override void ProcessAI()
    {
        //---- think about which technology to research ----//

        if (tech_id == 0)
            think_new_research();

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

            if (techInfo.can_research(NationId))
            {
                int curRating = 100 + (techInfo.is_nation_researching(NationId) ? 1 : 0) * 20;

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
        if (NationArray[NationId].total_tech_level() == TechRes.total_tech_level)
        {
            AIDelFirm();
            return true;
        }

        //----------------------------------------------// 

        if (Workers.Count > 0)
            return false;

        //-- check whether the firm is linked to any towns or not --//

        for (int i = 0; i < LinkedTowns.Count; i++)
        {
            if (LinkedTownsEnable[i] == InternalConstants.LINK_EE)
                return false;
        }

        //------------------------------------------------//

        AIDelFirm();

        return true;
    }
    
    public override void DrawDetails(IRenderer renderer)
    {
        renderer.DrawResearchDetails(this);
    }

    public override void HandleDetailsInput(IRenderer renderer)
    {
        renderer.HandleResearchDetailsInput(this);
    }
}