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
        return Productivity > 0.0 && TechId != 0;
    }

    public void StartResearch(int techId, int remoteAction)
    {
        //if (!remoteAction && remote.is_enable())
        //{
            //// packet structure : <firm recno> <tech Id>
            //short *shortPtr = (short *)remote.new_send_queue_msg(MSG_F_RESEARCH_START, 2*sizeof(short) );
            //shortPtr[0] = firm_recno;
            //shortPtr[1] = (short) techId;
            //return;
        //}

        //---- if the firm currently is already researching something ---//
        TerminateResearch();

        TechId = techId;
    }

    private void ProcessResearch()
    {
        if (TechId == 0)
            return;

        //------- make a progress with the research ------//

        TechInfo techInfo = TechRes[TechId];

        double progressPoint;
        if (Config.FastBuild && NationId == NationArray.PlayerId)
            progressPoint = Productivity / 100.0 + 0.5;
        else
            progressPoint = Productivity / 300.0;

        Nation nation = NationArray[NationId];

        int newLevel = nation.GetTechLevel(TechId) + 1;
        double levelDivider = (newLevel + 1) / 2.0; // from 1.0 to 2.0

        // more complex and higher level technology will take longer to research
        progressPoint = progressPoint * 30.0 / techInfo.ComplexLevel / levelDivider;

        // techInfo.Progress() will reset TechId if the current research level is the MAX tech level, so we have to save it now
        int techIdCopy = TechId;

        if (nation.MakeResearchProgress(TechId, progressPoint))
        {
            if (OwnFirm())
            {
                NewsArray.TechResearched(techIdCopy, nation.GetTechLevel(techIdCopy));

                SERes.far_sound(LocCenterX, LocCenterY, 1, 'F', FirmType, "FINS", 'S',
                    UnitRes[TechRes[techIdCopy].UnitId].SpriteId);
            }
        }
    }

    public void TerminateResearch()
    {
        TechId = 0;
    }

    public override void DrawDetails(IRenderer renderer)
    {
        renderer.DrawResearchDetails(this);
    }

    public override void HandleDetailsInput(IRenderer renderer)
    {
        renderer.HandleResearchDetailsInput(this);
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
        Nation nation = NationArray[NationId];
        
        int bestTechId = 0, bestRating = 0;

        for (int techId = TechRes.TechInfos.Length; techId > 0; techId--)
        {
            if (nation.CanResearch(techId))
            {
                bool isResearching = false;
                foreach (Firm firm in FirmArray)
                {
                    if (firm.NationId == NationId && firm.FirmType == FIRM_RESEARCH)
                    {
                        FirmResearch research = (FirmResearch)firm;
                        if (research.TechId == techId)
                            isResearching = true;
                    }
                }
                
                int curRating = 100 + (isResearching ? 1 : 0) * 20;

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

        if (NationArray[NationId].TotalTechLevel() == TechRes.TotalTechLevel)
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
}