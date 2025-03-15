using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class NationNew : NationBase
{
    private List<AITask> tasks = new List<AITask>();
    private List<CaptureIndependentTask> captureIndependentTasks = new List<CaptureIndependentTask>();
    private List<BuildMineTask> _buildMineTasks = new List<BuildMineTask>();
    private List<BuildCampTask> _buildCampTasks = new List<BuildCampTask>();
    private List<SettleTask> _settleTasks = new List<SettleTask>();

    public void ProcessAI()
    {
        ThinkAboutNewTask();
        
        ProcessTasks();
    }

    private void ThinkAboutNewTask()
    {
        int[] intervalDaysArray = { 90, 30, 15, 15 };
        int intervalDays = intervalDaysArray[Config.ai_aggressiveness - Config.OPTION_LOW];

        switch ((Info.TotalDays + nation_recno * 8) % intervalDays)
        {
            case 0:
                ThinkCaptureIndependent();
                break;
            
            case 1:
                ThinkBuildMine();
                break;
        }

        if ((Info.TotalDays + nation_recno) % 10 == 0)
        {
            ThinkBuildCamp();
        }

        if ((Info.TotalDays + nation_recno + 1) % 10 == 0)
        {
            ThinkSettle();
        }
    }

    private void ProcessTasks()
    {
        if ((Info.TotalDays + nation_recno) % 10 == 0)
        {
            for (int i = _buildMineTasks.Count - 1; i >= 0; i--)
            {
                BuildMineTask task = _buildMineTasks[i];
                if (task.ShouldCancel())
                {
                    _buildMineTasks.RemoveAt(i);
                    continue;
                }

                task.Process();
            }
        }

        if ((Info.TotalDays + nation_recno + 1) % 10 == 0)
        {
            for (int i = _buildCampTasks.Count - 1; i >= 0; i--)
            {
                BuildCampTask task = _buildCampTasks[i];
                if (task.ShouldCancel())
                {
                    _buildCampTasks.RemoveAt(i);
                    continue;
                }
                
                task.Process();
            }
        }

        if ((Info.TotalDays + nation_recno + 2) % 10 == 0)
        {
            for (int i = _settleTasks.Count - 1; i >= 0; i--)
            {
                SettleTask task = _settleTasks[i];
                if (task.ShouldCancel())
                {
                    _settleTasks.RemoveAt(i);
                    continue;
                }
                
                task.Process();
            }
        }
    }

    private void ThinkCaptureIndependent()
    {
        List<Town> possibleTowns = new List<Town>();
        foreach (Town town in TownArray)
        {
            if (town.NationId != 0)
                continue;

            if (town.RebelId != 0)
                continue;

            bool alreadyCapturing = false;
            foreach (CaptureIndependentTask task in captureIndependentTasks)
            {
                if (task.TownId == town.TownId)
                {
                    alreadyCapturing = true;
                    break;
                }
            }

            if (!alreadyCapturing)
                possibleTowns.Add(town);
        }

        if (possibleTowns.Count == 0)
            return;

        List<Unit> possibleCapturerUnits = new List<Unit>();
        List<(Firm, Worker)> possibleCapturerSoldiers = new List<(Firm, Worker)>();
        List<(FirmInn, InnUnit)> possibleCapturerInnUnits = new List<(FirmInn, InnUnit)>();

        foreach (Firm firm in FirmArray)
        {
            if (firm.nation_recno != nation_recno)
                continue;

            if (firm.firm_id == Firm.FIRM_CAMP || firm.firm_id == Firm.FIRM_BASE)
            {
                if (firm.overseer_recno != 0)
                {
                    Unit overseer = UnitArray[firm.overseer_recno];
                    //TODO also take reputation into account
                    if (overseer.skill.skill_level > 50) // TODO constant should depend on preferences
                    {
                        bool isCapturer = false;
                        foreach (CaptureIndependentTask task in captureIndependentTasks)
                        {
                            if (task.Capturers.Contains(overseer.sprite_recno))
                            {
                                isCapturer = true;
                                break;
                            }
                        }

                        if (!isCapturer)
                            possibleCapturerUnits.Add(overseer);
                    }
                }

                foreach (Worker worker in firm.workers)
                {
                    if (worker.skill_id == Skill.SKILL_LEADING && worker.skill_level > 50) // TODO constant should depend on preferences
                        possibleCapturerSoldiers.Add((firm, worker));
                }
            }

            if (firm.firm_id == Firm.FIRM_INN)
            {
                FirmInn inn = (FirmInn)firm;
                foreach (InnUnit innUnit in inn.inn_unit_array)
                {
                    // TODO constant should depend on preferences
                    if (innUnit.skill.skill_id == Skill.SKILL_LEADING && innUnit.skill.skill_level > 50 && ShouldHire(inn, innUnit))
                        possibleCapturerInnUnits.Add((inn, innUnit));
                }
            }
        }

        foreach (Unit unit in UnitArray)
        {
            //TODO add possible capturers from idle units
        }

        Town bestTown = null;
        int bestTownRating = 0;
        int majorityRace = 0;
        int majorityRacePopulation = 0;

        //TODO rate by races count, population, region, available generals, distance from our base, distance from enemies
        foreach (Town town in possibleTowns)
        {
            int racesCount = 0;
            for (int i = 0; i < town.RacesPopulation.Length; i++)
            {
                int racePopulation = town.RacesPopulation[i];
                if (racePopulation <= 3) // TODO constant should depend on preferences
                    continue;

                if (racePopulation > majorityRacePopulation)
                {
                    majorityRace = i + 1;
                    majorityRacePopulation = racePopulation;
                }

                racesCount++;
            }

            if (majorityRace == 0)
                continue;

            if (racesCount > 3) // TODO constant should depend on preferences
                continue;

            int rating = 0;
        }
    }

    private void ThinkBuildMine()
    {
        //TODO check if we have not enough population and should not build mine at all
        
        // find non-mined raw resource
        // rate by region, distance, distance from enemy

        int bestRating = 0;
        Site bestSite = null;
        foreach (Site site in SiteArray)
        {
            if (site.SiteType != Site.SITE_RAW || site.HasMine)
                continue;

            foreach (Town town in TownArray)
            {
                if (town.NationId != nation_recno)
                    continue;

                // TODO other region
                if (town.RegionId != site.RegionId)
                    continue;

                int siteRating = GameConstants.MapSize - Misc.PointsDistance(town.LocX1, town.LocY1, town.LocX2, town.LocY2,
                    site.LocX, site.LocY, site.LocX, site.LocY);
                if (siteRating > bestRating)
                {
                    bool hasBuildMineTask = false;
                    foreach (var buildMineTask in _buildMineTasks)
                    {
                        if (buildMineTask.SiteId == site.SiteId)
                            hasBuildMineTask = true;
                    }

                    if (!hasBuildMineTask)
                    {
                        bestRating = siteRating;
                        bestSite = site;
                    }
                }
            }
        }

        if (bestSite != null)
        {
            //TODO check if we should capture several mines simultaneously
            if (_buildMineTasks.Count == 0)
                _buildMineTasks.Add(new BuildMineTask(this, bestSite.SiteId));
        }
    }

    private void ThinkBuildCamp()
    {
        foreach (Town town in TownArray)
        {
            if (town.NationId != nation_recno)
                continue;
            
            bool hasLinkedCamp = town.HasLinkedCamp(nation_recno, false);
            if (!hasLinkedCamp)
            {
                bool hasBuildCampTask = false;
                foreach (BuildCampTask buildCampTask in _buildCampTasks)
                {
                    if (buildCampTask.TownId == town.TownId)
                        hasBuildCampTask = true;
                }
                
                if (!hasBuildCampTask)
                    _buildCampTasks.Add(new BuildCampTask(this, town.TownId));
            }
        }
    }

    private void ThinkSettle()
    {
        // Settle when
        // 1. Village population is close to maximum - TODO
        // 2. Village has different races - TODO
        // 3. A new mine is built

        foreach (Firm firm in FirmArray)
        {
            if (firm.nation_recno != nation_recno || firm.firm_id != Firm.FIRM_MINE)
                continue;

            bool linkedToOurTown = false;
            foreach (int townId in firm.linked_town_array)
            {
                Town town = TownArray[townId];
                if (town.NationId == nation_recno)
                    linkedToOurTown = true;
            }

            if (!linkedToOurTown)
            {
                bool hasSettleTask = false;
                foreach (SettleTask settleTask in _settleTasks)
                {
                    if (settleTask.FirmId == firm.firm_recno)
                        hasSettleTask = true;
                }
                
                if (!hasSettleTask)
                    _settleTasks.Add(new SettleTask(this, firm.firm_recno));
            }
        }
    }

    private bool ShouldHire(FirmInn inn, InnUnit innUnit)
    {
        //TODO
        return true;
    }
    
    #region Old AI stubs

    public const int ACTION_AI_BUILD_FIRM = 1;
    public const int ACTION_AI_ASSIGN_OVERSEER = 2;
    public const int ACTION_AI_ASSIGN_CONSTRUCTION_WORKER = 3;
    public const int ACTION_AI_ASSIGN_WORKER = 4;
    public const int ACTION_AI_ASSIGN_SPY = 5;
    public const int ACTION_AI_SCOUT = 6;
    public const int ACTION_AI_SETTLE_TO_OTHER_TOWN = 7;
    public const int ACTION_AI_PROCESS_TALK_MSG = 8;
    public const int ACTION_AI_SEA_TRAVEL = 9;
    public const int ACTION_AI_SEA_TRAVEL2 = 10;
    public const int ACTION_AI_SEA_TRAVEL3 = 11;

    public const int SEA_ACTION_SETTLE = 1;
    public const int SEA_ACTION_BUILD_CAMP = 2;
    public const int SEA_ACTION_ASSIGN_TO_FIRM = 3;
    public const int SEA_ACTION_MOVE = 4;
    public const int SEA_ACTION_NONE = 5;

    public int pref_force_projection;
    public int pref_military_development; // pref_military_development + pref_economic_development = 100
    public int pref_economic_development;
    public int pref_inc_pop_by_capture; // pref_inc_pop_by_capture + pref_inc_pop_by_growth = 100
    public int pref_inc_pop_by_growth;
    public int pref_peacefulness;
    public int pref_military_courage;
    public int pref_territorial_cohesiveness;
    public int pref_trading_tendency;
    public int pref_allying_tendency;
    public int pref_honesty;
    public int pref_town_harmony;
    public int pref_loyalty_concern;
    public int pref_forgiveness;
    public int pref_collect_tax;
    public int pref_hire_unit;
    public int pref_use_weapon;
    public int pref_keep_general; // whether to keep currently non-useful the general, or demote them.
    public int pref_keep_skilled_unit; // whether to keep currently non-useful skilled units, or assign them to towns.
    public int pref_diplomacy_retry; // tedency to retry diplomatic actions after previous ones have been rejected.
    public int pref_attack_monster;
    public int pref_spy;
    public int pref_counter_spy;
    public int pref_food_reserve;
    public int pref_cash_reserve;
    public int pref_use_marine;
    public int pref_unit_chase_distance;
    public int pref_repair_concern;
    public int pref_scout;

    public int ai_base_town_count;
    public int ai_capture_enemy_town_recno;
    public int[] firm_should_close_array = new int[Firm.MAX_FIRM_TYPE];

    public List<int> ai_town_array = new List<int>();
    public List<int> ai_base_array = new List<int>();
    public List<int> ai_mine_array = new List<int>();
    public List<int> ai_factory_array = new List<int>();
    public List<int> ai_camp_array = new List<int>();
    public List<int> ai_research_array = new List<int>();
    public List<int> ai_war_array = new List<int>();
    public List<int> ai_harbor_array = new List<int>();
    public List<int> ai_market_array = new List<int>();
    public List<int> ai_inn_array = new List<int>();
    public List<int> ai_general_array = new List<int>();
    public List<int> ai_caravan_array = new List<int>();
    public List<int> ai_ship_array = new List<int>();
    public List<AIRegion> ai_region_array = new List<AIRegion>();
    
    public List<AttackCamp> attack_camps = new List<AttackCamp>();
    
    public void add_town_info(int townRecno)
    {
    }

    public void del_town_info(int townRecno)
    {
    }

    public void add_firm_info(int firmId, int firmRecno)
    {
    }

    public void del_firm_info(int firmId, int firmRecno)
    {
    }

    public void add_general_info(int unitRecno)
    {
    }

    public void del_general_info(int unitRecno)
    {
    }
    
    public void add_caravan_info(int unitRecno)
    {
    }

    public void del_caravan_info(int unitRecno)
    {
    }

    public void add_ship_info(int unitRecno)
    {
    }

    public void del_ship_info(int unitRecno)
    {
    }

    public ActionNode add_action(int xLoc, int yLoc, int refXLoc, int refYLoc, int actionMode, int actionPara,
        int instanceCount = 1, int unitRecno = 0, int actionPara2 = 0, List<int> groupUnitArray = null)
    {
        return null;
    }

    public ActionNode get_action(int actionXLoc, int actionYLoc, int refXLoc, int refYLoc,
        int actionMode, int actionPara, int unitRecno = 0, int checkMode = 0)
    {
        return null;
    }

    public ActionNode get_action(int recNo)
    {
        return null;
    }
    
    public void del_action(ActionNode actionNode)
    {
    }

    public int action_count()
    {
        return 0;
    }

    public bool is_action_exist(int actionMode, int actionPara, int regionId = 0)
    {
        return false;
    }
    
    public bool is_build_action_exist(int firmId, int xLoc, int yLoc)
    {
        return false;
    }

    public int process_action_id(int actionId)
    {
        return 0;
    }

    public void action_finished(int aiActionId, int unitRecno = 0, int actionFailure = 0)
    {
    }

    public void action_failure(int aiActionId, int unitRecno = 0)
    {
    }

    public bool find_best_firm_loc(int buildFirmId, int refXLoc, int refYLoc, out int resultXLoc, out int resultYLoc)
    {
        resultXLoc = -1;
        resultYLoc = -1;
        return false;
    }

    public bool can_ai_build(int firmId)
    {
        return false;
    }

    public bool ai_has_enough_food()
    {
        return false;
    }

    public bool ai_should_spend(int importanceRating, double spendAmt = 0)
    {
        return false;
    }

    public bool ai_should_attack_friendly(int friendlyNationRecno, int attackTemptation)
    {
        return false;
    }

    public bool ai_attack_target(int targetXLoc, int targetYLoc, int targetCombatLevel, bool justMoveToFlag = false,
        int attackerMinCombatLevel = 0, int leadAttackCampRecno = 0, bool useAllCamp = false)
    {
        return false;
    }

    public int attack_enemy_town_defense(Town targetTown, bool useAllCamp = false)
    {
        return 0;
    }

    public int ai_defend(int attackerUnitRecno)
    {
        return 0;
    }

    public bool ai_should_hire_unit(int importanceRating)
    {
        return false;
    }

    public int recruit_jobless_worker(Firm destFirm, int preferedRaceId = 0)
    {
        return 0;
    }

    public int find_best_capturer(int townRecno, int raceId, ref int bestTargetResistance)
    {
        return 0;
    }

    public bool mobilize_capturer(int unitRecno)
    {
        return false;
    }

    public bool should_use_cash_to_capture()
    {
        return false;
    }

    public int train_unit(int skillId, int raceId, int destX, int destY, out int trainTownRecno, int actionId = 0)
    {
        trainTownRecno = 0;
        return 0;
    }

    public int max_human_battle_ship_count()
    {
        return 0;
    }

    public bool has_trade_ship(int firmRecno1, int firmRecno2)
    {
        return false;
    }

    public bool ai_has_too_many_camp()
    {
        return false;
    }

    public int ai_supported_inn_count()
    {
        return 0;
    }

    public bool ai_should_create_new_spy(int onlyCounterSpy)
    {
        return false;
    }

    public bool think_spy_new_mission(int raceId, int regionId, out int loc_x1, out int loc_y1, out int cloakedNationRecno)
    {
        loc_x1 = -1;
        loc_y1 = -1;
        cloakedNationRecno = 0;
        return false;
    }

    public void ai_start_spy_new_mission(Unit spyUnit, int loc_x1, int loc_y1, int cloakedNationRecno)
    {
    }

    public int ai_trade_with_rating(int withNationRecno)
    {
        return 0;
    }

    public int total_alliance_military()
    {
        return 0;
    }

    public int total_enemy_military()
    {
        return 0;
    }

    public bool ai_has_should_close_camp(int regionId)
    {
        return false;
    }

    public bool think_capture_new_enemy_town(Town capturerTown, bool useAllCamp = false)
    {
        return false;
    }

    public AIRegion get_ai_region(int regionId)
    {
        return null;
    }

    public void update_ai_region()
    {
    }

    public bool should_diplomacy_retry(int talkId, int nationRecno)
    {
        return false;
    }

    public void notify_talk_msg(TalkMsg talkMsg)
    {
    }

    public void ai_notify_reply(int talkMsgRecno)
    {
    }

    public void ai_end_treaty(int nationRecno)
    {
    }
    
    #endregion
}
