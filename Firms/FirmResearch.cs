namespace TenKingdoms;

public class FirmResearch : Firm
{
    public int TechId { get; private set; } // the id. of the tech this firm is currently researching

    private TechRes TechRes => Sys.Instance.TechRes;

    public FirmResearch()
    {
        FirmSkillId = Skill.SKILL_RESEARCH;
    }

    protected override void DeinitDerived()
    {
        TerminateResearch();
    }

    public override void NextDay()
    {
        base.NextDay();

        RecruitWorker();

        UpdateWorker();

        CalcProductivity();

        ProcessResearch();
    }

    public override void ChangeNation(int newNationId)
    {
        TerminateResearch();

        base.ChangeNation(newNationId);
    }

    public override bool IsOperating()
    {
        return Productivity > 0 && TechId != 0;
    }

    public void StartResearch(int techId, int remoteAction)
    {
        TechInfo techInfo = TechRes[techId];

        //if (!remoteAction && remote.is_enable())
        //{
            //// packet structure : <firm recno> <tech Id>
            //short *shortPtr = (short *)remote.new_send_queue_msg(MSG_F_RESEARCH_START, 2*sizeof(short) );
            //shortPtr[0] = firm_recno;
            //shortPtr[1] = (short) techId;
            //return;
        //}

        //---- if the firm currently is already researching something ---//

        if (TechId != 0)
            TerminateResearch();

        TechId = techId;
        techInfo.inc_nation_is_researching(NationId);
    }

    private void ProcessResearch()
    {
        if (TechId == 0)
            return;

        //------- make a progress with the research ------//

        TechInfo techInfo = TechRes[TechId];

        double progressPoint;
        if (Config.fast_build && NationId == NationArray.player_recno)
            progressPoint = Productivity / 100.0 + 0.5;
        else
            progressPoint = Productivity / 300.0;

        int newLevel = techInfo.get_nation_tech_level(NationId) + 1;
        double levelDivider = (newLevel + 1) / 2.0; // from 1.0 to 2.0

        // more complex and higher level technology will take longer to research
        progressPoint = progressPoint * 30.0 / techInfo.complex_level / levelDivider;

        // techInfo.Progress() will reset TechId if the current research level is the MAX tech level, so we have to save it now
        int techIdCopy = TechId;

        if (techInfo.progress(NationId, progressPoint))
        {
            if (TechId != 0) // techInfo.Progress() may have called TerminateResearch() if the tech level reaches the maximum
            {
                TerminateResearch();

                //----- research next level technology automatically for player's firm only -----//

                if (!AIFirm)
                {
                    if (techInfo.get_nation_tech_level(NationId) < techInfo.max_tech_level)
                    {
                        StartResearch(techIdCopy, InternalConstants.COMMAND_AUTO);
                    }
                }
            }

            if (OwnFirm())
            {
                NewsArray.tech_researched(techIdCopy, TechRes[techIdCopy].get_nation_tech_level(NationId));

                SERes.far_sound(LocCenterX, LocCenterY, 1, 'F', FirmType, "FINS", 'S',
                    UnitRes[TechRes[techIdCopy].unit_id].sprite_id);
            }
        }
    }

    public void TerminateResearch()
    {
        if (TechId != 0)
        {
            TechRes[TechId].dec_nation_is_researching(NationId);
            TechId = 0;
        }
    }

    #region Old AI Functions    

    public override void ProcessAI()
    {
        //---- think about which technology to research ----//

        if (TechId == 0)
            ThinkNewResearch();

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

    private void ThinkNewResearch()
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

        if (bestTechId != 0)
            StartResearch(bestTechId, InternalConstants.COMMAND_AI);
    }

    private bool ThinkDel()
    {
        //----- if all technologies have been researched -----//

        if (NationArray[NationId].total_tech_level() == TechRes.total_tech_level)
        {
            AIDelFirm();
            return true;
        }

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
    
    public override void DrawDetails(IRenderer renderer)
    {
        renderer.DrawResearchDetails(this);
    }

    public override void HandleDetailsInput(IRenderer renderer)
    {
        renderer.HandleResearchDetailsInput(this);
    }
}