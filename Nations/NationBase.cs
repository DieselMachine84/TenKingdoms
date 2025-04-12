using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class NationBase : IIdObject
{
    public const int NATION_OWN = 1;
    public const int NATION_REMOTE = 2;
    public const int NATION_AI = 3;

    public const int NATION_HOSTILE = 0;
    public const int NATION_TENSE = 1;
    public const int NATION_NEUTRAL = 2;
    public const int NATION_FRIENDLY = 3;
    public const int NATION_ALLIANCE = 4;

    public const int RELATION_LEVEL_PER_STATUS = 20;

    public const int IMPORT_TYPE_COUNT = 3;
    public const int IMPORT_RAW = 0;
    public const int IMPORT_PRODUCT = 1;
    public const int IMPORT_TOTAL = 2;

    public const int INCOME_TYPE_COUNT = 8;
    public const int INCOME_SELL_GOODS = 0;
    public const int INCOME_EXPORTS = 1;
    public const int INCOME_TAX = 2;
    public const int INCOME_TREASURE = 3;
    public const int INCOME_FOREIGN_WORKER = 4;
    public const int INCOME_SELL_FIRM = 5;
    public const int INCOME_TRIBUTE = 6;
    public const int INCOME_CHEAT = 7;

    public const int EXPENSE_TYPE_COUNT = 16;
    public const int EXPENSE_GENERAL = 0;
    public const int EXPENSE_SPY = 1;
    public const int EXPENSE_MOBILE_UNIT = 2;
    public const int EXPENSE_CARAVAN = 3;
    public const int EXPENSE_WEAPON = 4;
    public const int EXPENSE_SHIP = 5;
    public const int EXPENSE_FIRM = 6;
    public const int EXPENSE_TRAIN_UNIT = 7;
    public const int EXPENSE_HIRE_UNIT = 8;
    public const int EXPENSE_REWARD_UNIT = 9;
    public const int EXPENSE_FOREIGN_WORKER = 10;
    public const int EXPENSE_GRANT_OWN_TOWN = 11;
    public const int EXPENSE_GRANT_OTHER_TOWN = 12;
    public const int EXPENSE_IMPORTS = 13;
    public const int EXPENSE_TRIBUTE = 14;
    public const int EXPENSE_BRIBE = 15;

    public int nation_recno;
    public int nation_type;
    public int race_id;
    public int color_scheme_id;
    public byte nation_color;
    public int king_unit_recno;
    public int king_leadership;
    public int nation_name_id;
    public string nation_name_str;
    public int player_id;
    public bool next_frame_ready;
    public int last_caravan_id;
    public int nation_firm_count;
    public DateTime last_build_firm_date;
    public int[] know_base_array = new int[GameConstants.MAX_RACE];
    public int[] base_count_array = new int[GameConstants.MAX_RACE];
    public bool is_at_war_today;
    public bool is_at_war_yesterday;
    public DateTime last_war_date;
    public int last_attacker_unit_recno;
    public DateTime last_independent_unit_join_date;

    public bool cheat_enabled_flag;

    public double cash;
    public double food;

    public double reputation;
    public double kill_monster_score;

    public int auto_collect_tax_loyalty;
    public int auto_grant_loyalty;

    public double cur_year_profit;
    public double last_year_profit;

    public double cur_year_fixed_income;
    public double last_year_fixed_income;
    public double cur_year_fixed_expense;
    public double last_year_fixed_expense;

    public double[] cur_year_income_array = new double[INCOME_TYPE_COUNT];
    public double[] last_year_income_array = new double[INCOME_TYPE_COUNT];
    public double cur_year_income;
    public double last_year_income;

    public double[] cur_year_expense_array = new double[EXPENSE_TYPE_COUNT];
    public double[] last_year_expense_array = new double[EXPENSE_TYPE_COUNT];
    public double cur_year_expense;
    public double last_year_expense;

    public double cur_year_cheat;
    public double last_year_cheat;

    public double cur_year_food_in;
    public double last_year_food_in;
    public double cur_year_food_out;
    public double last_year_food_out;
    public double cur_year_food_change;
    public double last_year_food_change;

    public double cur_year_reputation_change;
    public double last_year_reputation_change;

    public int total_population;
    public int total_jobless_population;

    public int total_unit_count;
    public int total_human_count;
    public int total_general_count;
    public int total_weapon_count;
    public int total_ship_count;
    public int total_firm_count;
    public int total_spy_count;
    public int total_ship_combat_level;

    public int largest_town_recno; // the recno of the biggest town of this nation
    public int largest_town_pop;

    // no. of natural resources site this nation possesses
    public int[] raw_count_array = new int[GameConstants.MAX_RAW];

    public int[] last_unit_name_id_array = new int[UnitConstants.MAX_UNIT_TYPE];

    public int population_rating;
    public int military_rating;
    public int economic_rating;
    public int overall_rating;

    public int enemy_soldier_killed;
    public int own_soldier_killed;
    public int enemy_civilian_killed;
    public int own_civilian_killed;
    public int enemy_weapon_destroyed;
    public int own_weapon_destroyed;
    public int enemy_ship_destroyed;
    public int own_ship_destroyed;
    public int enemy_firm_destroyed;
    public int own_firm_destroyed;

    // inter-relationship with other nations
    public NationRelation[] relation_array = new NationRelation[GameConstants.MAX_NATION];
    public int[] relation_status_array = new int[GameConstants.MAX_NATION]; // replace status in struct NationRelation
    // for seeking to indicate whether passing other nation region
    public bool[] relation_passable_array = new bool[GameConstants.MAX_NATION];
    public bool[] relation_should_attack_array = new bool[GameConstants.MAX_NATION];
    public bool is_allied_with_player; // for fast access in visiting world functions

    public static int nation_hand_over_flag;

    protected FirmRes FirmRes => Sys.Instance.FirmRes;
    protected RaceRes RaceRes => Sys.Instance.RaceRes;
    protected SpriteRes SpriteRes => Sys.Instance.SpriteRes;
    protected UnitRes UnitRes => Sys.Instance.UnitRes;
    protected MonsterRes MonsterRes => Sys.Instance.MonsterRes;
    protected GodRes GodRes => Sys.Instance.GodRes;
    protected TechRes TechRes => Sys.Instance.TechRes;

    protected Config Config => Sys.Instance.Config;
    protected ConfigAdv ConfigAdv => Sys.Instance.ConfigAdv;
    protected Info Info => Sys.Instance.Info;
    protected TalkRes TalkRes => Sys.Instance.TalkRes;

    protected NationArray NationArray => Sys.Instance.NationArray;
    protected FirmArray FirmArray => Sys.Instance.FirmArray;
    protected TownArray TownArray => Sys.Instance.TownArray;
    protected UnitArray UnitArray => Sys.Instance.UnitArray;
    protected RebelArray RebelArray => Sys.Instance.RebelArray;
    protected SpyArray SpyArray => Sys.Instance.SpyArray;
    protected RegionArray RegionArray => Sys.Instance.RegionArray;
    protected SiteArray SiteArray => Sys.Instance.SiteArray;
    protected NewsArray NewsArray => Sys.Instance.NewsArray;

    public string nation_name()
    {
        return king_name(true) + "'s Kingdom";
    }

    public string king_name(bool firstWordOnly = false)
    {
        if (nation_name_id < 0) // human player custom names
        {
            return NationArray.get_human_name(nation_name_id, firstWordOnly);
        }
        else
        {
            if (firstWordOnly)
                return RaceRes[race_id].get_single_name(nation_name_id);
            else
                return RaceRes[race_id].get_name(nation_name_id);
        }
    }

    public int peaceful_days()
    {
        return (Info.game_date - last_war_date).Days;
    }

    public string peace_duration_str()
    {
        int peaceDays = peaceful_days();
        int peaceYear = peaceDays / 365;
        int peaceMonth = (peaceDays - peaceYear * 365) / 30;

        string str = String.Empty;

        if (peaceYear > 0)
        {
            if (peaceYear != 1)
                str = $"{peaceYear} years and ";
            else
                str = "1 year and ";
        }

        if (peaceMonth != 1)
            str += peaceMonth + " months";
        else
            str += "1 month";

        return str;
    }

    public void set_at_war_today(int attackerUnitRecno = 0)
    {
        is_at_war_today = true;
        if (attackerUnitRecno != 0)
            last_attacker_unit_recno = attackerUnitRecno;
    }

    public bool is_at_war()
    {
        return is_at_war_today || is_at_war_yesterday;
    }

    public int yearly_food_consumption()
    {
        return GameConstants.PERSON_FOOD_YEAR_CONSUMPTION * all_population();
    }

    public int yearly_food_production()
    {
        return GameConstants.PEASANT_FOOD_YEAR_PRODUCTION * total_jobless_population;
    }

    public int yearly_food_change()
    {
        return yearly_food_production() - yearly_food_consumption();
    }

    public double profit_365days()
    {
        return last_year_profit * (365 - Info.year_day) / 365 + cur_year_profit;
    }

    public double fixed_income_365days()
    {
        return last_year_fixed_income * (365 - Info.year_day) / 365 + cur_year_fixed_income;
    }

    public double fixed_expense_365days()
    {
        return last_year_fixed_expense * (365 - Info.year_day) / 365 + cur_year_fixed_expense;
    }

    public double fixed_profit_365days()
    {
        return fixed_income_365days() - fixed_expense_365days();
    }

    public double income_365days()
    {
        return last_year_income * (365 - Info.year_day) / 365 + cur_year_income;
    }

    public double income_365days(int incomeType)
    {
        return last_year_income_array[incomeType] * (365 - Info.year_day) / 365 + cur_year_income_array[incomeType];
    }

    public double true_income_365days()
    {
        double curYearIncome = 0.0;
        double lastYearIncome = 0.0;

        for (int i = 0; i < INCOME_TYPE_COUNT - 1; i++) // -1 to exclude cheat
        {
            curYearIncome += cur_year_income_array[i];
            lastYearIncome += last_year_income_array[i];
        }

        return lastYearIncome * (365 - Info.year_day) / 365 + curYearIncome;
    }

    public double expense_365days()
    {
        return last_year_expense * (365 - Info.year_day) / 365 + cur_year_expense;
    }

    public double expense_365days(int expenseType)
    {
        return last_year_expense_array[expenseType] * (365 - Info.year_day) / 365 + cur_year_expense_array[expenseType];
    }

    public double cheat_365days()
    {
        return last_year_cheat * (365 - Info.year_day) / 365 + cur_year_cheat;
    }

    public double true_profit_365days()
    {
        return profit_365days() - cheat_365days();
    }

    public double food_change_365days()
    {
        return last_year_food_change * (365 - Info.year_day) / 365 + cur_year_food_change;
    }

    public double reputation_change_365days()
    {
        return last_year_reputation_change * (365 - Info.year_day) / 365 + cur_year_reputation_change;
    }

    protected virtual void set_relation_status_ai(int nationRecno, int newStatus)
    {
    }

    public void set_relation_status(int nationRecno, int newStatus, bool recursiveCall = false)
    {
        if (nationRecno == nation_recno) // cannot set relation to itself
            return;

        NationRelation nationRelation = get_relation(nationRecno);

        //-------------------------------------------------//
        //
        // When two nations agree to a cease-fire, there may
        // still be some bullets on their ways, and those
        // will set the status back to War status, so we need
        // the following code to handle this case.
        //
        //-------------------------------------------------//

        // 5 days after the cease-fire, the nation will remain cease-fire
        if (!recursiveCall && nationRelation.status == NATION_TENSE && newStatus == NATION_HOSTILE &&
            Info.game_date < nationRelation.last_change_status_date.AddDays(5.0))
        {
            return;
        }

        //-------------------------------------------------//
        //
        // If the nation cease fire or form a friendly/alliance
        // treaty with a nation. And this nation current
        // has plan to attack that nation, then cancel the plan.
        //
        //-------------------------------------------------//

        if (is_ai())
        {
            set_relation_status_ai(nationRecno, newStatus);
        }

        //------------------------------------------------//

        relation_status_array[nationRecno - 1] = newStatus;
        nationRelation.status = newStatus;
        nationRelation.last_change_status_date = Info.game_date;

        int newRelationLevel = newStatus * RELATION_LEVEL_PER_STATUS;

        // only set it when the new value is lower than the current value
        if (newRelationLevel < nationRelation.ai_relation_level)
            nationRelation.ai_relation_level = newRelationLevel;

        set_relation_passable(nationRecno, NATION_FRIENDLY);

        //---------- set should_attack -------//

        if (newStatus == NATION_ALLIANCE || newStatus == NATION_FRIENDLY || newStatus == NATION_TENSE)
        {
            set_relation_should_attack(nationRecno, false, InternalConstants.COMMAND_AUTO);
        }
        else if (newStatus == NATION_HOSTILE)
        {
            set_relation_should_attack(nationRecno, true, InternalConstants.COMMAND_AUTO);
        }

        //----- share the nation contact with each other -----//
        /*
            // these segment code will cause multiplayer sync problem
    
            if( newStatus == NATION_ALLIANCE )
            {
                Nation* withNation = NationArray[nationRecno];
    
                for( i=NationArray.size() ; i>0 ; i-- )
                {
                    if( i==nation_recno || i==nationRecno )
                        continue;
    
                    if( NationArray.is_deleted(i) )
                        continue;
    
                    //-- if we have contact with this nation and our ally doesn't, share the contact with it --//
    
                    if( get_relation(i).has_contact && withNation.get_relation(i).has_contact==0 )
                    {
                        withNation.establish_contact(i);
                    }
                }
            }
        */

        //--- if this is a call from a client function, not a recursive call ---//

        if (!recursiveCall)
        {
            NationArray[nationRecno].set_relation_status(nation_recno, newStatus, true);

            //-- auto terminate their trade treaty if two nations go into a war --//

            if (newStatus == NATION_HOSTILE)
                set_trade_treaty(nationRecno, false);
        }
    }

    public int get_relation_status(int nationRecno)
    {
        return relation_status_array[nationRecno - 1];
    }

    public void set_relation_passable(int nationRecno, int status)
    {
        relation_passable_array[nationRecno - 1] = (relation_status_array[nationRecno - 1] >= status);
    }

    public bool get_relation_passable(int nationRecno)
    {
        return relation_passable_array[nationRecno - 1];
    }

    public void set_relation_should_attack(int nationRecno, bool newValue, int remoteAction)
    {
        //TODO remote
        //if( !remoteAction && remote.is_enable() )
        //{
        //short *shortPtr = (short *) remote.new_send_queue_msg(MSG_NATION_SET_SHOULD_ATTACK, 3*sizeof(short));
        //*shortPtr = nation_recno;
        //shortPtr[1] = nationRecno;
        //shortPtr[2] = newValue;
        //}
        //else
        //{
        relation_should_attack_array[nationRecno - 1] = newValue;
        get_relation(nationRecno).should_attack = newValue;
        //}
    }

    public bool get_relation_should_attack(int nationRecno)
    {
        return nationRecno == 0 || relation_should_attack_array[nationRecno - 1];
    }

    public void set_trade_treaty(int nationRecno, bool allowFlag)
    {
        get_relation(nationRecno).trade_treaty = allowFlag;
        NationArray[nationRecno].get_relation(nation_recno).trade_treaty = allowFlag;
    }

    public void establish_contact(int nationRecno)
    {
        get_relation(nationRecno).has_contact = true;
        NationArray[nationRecno].get_relation(nation_recno).has_contact = true;
    }

    public void change_ai_relation_level(int nationRecno, int levelChange)
    {
        NationRelation nationRelation = get_relation(nationRecno);

        int newLevel = nationRelation.ai_relation_level + levelChange;

        newLevel = Math.Min(newLevel, 100);
        newLevel = Math.Max(newLevel, 0);

        nationRelation.ai_relation_level = newLevel;
    }

    public NationRelation get_relation(int nationRecno)
    {
        return relation_array[nationRecno - 1];
    }

    public double total_year_trade(int nationRecno)
    {
        return get_relation(nationRecno).last_year_import[IMPORT_TOTAL] +
               NationArray[nationRecno].get_relation(nation_recno).last_year_import[IMPORT_TOTAL];
    }

    public int trade_rating(int nationRecno)
    {
        // use an absolute value 5000 as the divider.

        int tradeRating1 = 100 * (int)total_year_trade(nationRecno) / 5000;

        int tradeRating2 =
            50 * (int)NationArray[nationRecno].get_relation(nation_recno).last_year_import[IMPORT_TOTAL] /
            (int)(last_year_income + 1) +
            50 * (int)get_relation(nationRecno).last_year_import[IMPORT_TOTAL] / (int)(last_year_expense + 1);

        return Math.Max(tradeRating1, tradeRating2);
    }

    public int all_population()
    {
        return total_population + total_human_count;
    }

    public int total_tech_level(int unitClass = 0)
    {
        int totalTechLevel = 0;

        for (int i = 1; i <= TechRes.tech_info_array.Length; i++)
        {
            TechInfo techInfo = TechRes[i];
            int techLevel = techInfo.get_nation_tech_level(nation_recno);

            if (techLevel > 0)
            {
                if (unitClass == 0 || UnitRes[techInfo.unit_id].unit_class == unitClass)
                {
                    totalTechLevel += techLevel;
                }
            }
        }

        return totalTechLevel;
    }

    public int base_town_count_in_region(int regionId)
    {
        // regionStatId may be zero
        int regionStatId = RegionArray.GetRegionInfo(regionId).RegionStatId;
        if (regionStatId != 0)
        {
            return RegionArray.GetRegionStat(regionId).BaseTownNationCounts[nation_recno - 1];
        }
        else if (RegionArray.GetRegionInfo(regionId).RegionSize < InternalConstants.TOWN_WIDTH * InternalConstants.TOWN_HEIGHT)
        {
            return 0; // not enough to build any town
        }
        else
        {
            int townCount = 0;
            foreach (Town town in TownArray)
            {
                if (town.RegionId == regionId && town.NationId == nation_recno)
                    townCount++;
            }

            return townCount;
        }
    }


    public void update_nation_rating()
    {
        population_rating = get_population_rating();

        economic_rating = get_economic_rating();

        overall_rating = get_overall_rating();
    }

    public int get_population_rating()
    {
        return all_population();
    }

    public int get_economic_rating()
    {
        return (int)(cash / 300 + true_income_365days() / 2 + true_profit_365days());
    }

    public int get_overall_rating()
    {
        return 33 * population_rating / 500 + 33 * military_rating / 200 + 33 * economic_rating / 10000;
    }

    public int population_rank_rating()
    {
        if (NationArray.max_population_rating == 0)
            return 0;

        return 100 * population_rating / NationArray.max_population_rating;
    }

    public int military_rank_rating()
    {
        if (NationArray.max_military_rating == 0)
            return 0;

        return 100 * military_rating / NationArray.max_military_rating;
    }

    public int economic_rank_rating()
    {
        if (NationArray.max_economic_rating == 0)
            return 0;

        return 100 * economic_rating / NationArray.max_economic_rating;
    }

    public int reputation_rank_rating()
    {
        if (NationArray.max_reputation == 0)
            return 0;

        return 100 * (int)reputation / NationArray.max_reputation;
    }

    public int kill_monster_rank_rating()
    {
        if (NationArray.max_kill_monster_score == 0)
            return 0;

        if (Config.monster_type == Config.OPTION_MONSTER_NONE)
            return 0;

        return 100 * (int)kill_monster_score / NationArray.max_kill_monster_score;
    }

    public int overall_rank_rating()
    {
        if (NationArray.max_overall_rating == 0)
            return 0;

        return 100 * overall_rating / NationArray.max_overall_rating;
    }

    public bool goal_destroy_nation_achieved()
    {
        return NationArray.nation_count == 1;
    }

    public bool goal_destroy_monster_achieved()
    {
        if (!Config.goal_destroy_monster) // this is not one of the required goals.
            return false;

        if (Config.monster_type == Config.OPTION_MONSTER_NONE)
            return false;

        //------- when all monsters have been killed -------//

        if (FirmRes[Firm.FIRM_MONSTER].total_firm_count == 0 && UnitRes.mobile_monster_count == 0)
        {
            double maxScore = 0.0;

            foreach (Nation nation in NationArray)
            {
                if (nation.kill_monster_score > maxScore)
                    maxScore = nation.kill_monster_score;
            }

            //-- if this nation is the one that has destroyed most monsters, it wins, otherwise it loses --//

            return (int)maxScore == (int)kill_monster_score;
        }

        return false;
    }

    public bool goal_population_achieved()
    {
        if (!Config.goal_population_flag) // this is not one of the required goals.
            return false;

        return all_population() >= Config.goal_population;
    }

    public bool goal_economic_score_achieved()
    {
        if (!Config.goal_economic_score_flag)
            return false;

        Info.set_rank_data(false); // 0-set all nations, not just those that have contact with us

        return Info.get_rank_score(3, nation_recno) >= Config.goal_economic_score;
    }

    public bool goal_total_score_achieved()
    {
        if (!Config.goal_total_score_flag)
            return false;

        Info.set_rank_data(false); // 0-set all nations, not just those that have contact with us

        return Info.get_total_score(nation_recno) >= Config.goal_total_score;
    }

    public bool is_own() => nation_type == NATION_OWN;
    public bool is_ai() => nation_type == NATION_AI;
    public bool is_remote() => nation_type == NATION_REMOTE;

    public NationBase()
    {
    }

    void IIdObject.SetId(int id)
    {
        nation_recno = id;
    }

    public virtual void init(int nationType, int raceId, int colorSchemeId, int playerId = 0)
    {
        //------------- set vars ---------------//

        nation_type = nationType;
        race_id = raceId;
        color_scheme_id = colorSchemeId;
        player_id = playerId;

        //colorSchemeId = Math.Min(colorSchemeId, InternalConstants.MAX_COLOR_SCHEME);
        //nation_color = Sys.Instance.color_remap_array[colorSchemeId].main_color;
        nation_color = ColorRemap.GetColorRemap(ColorRemap.ColorSchemes[nation_recno], false).MainColor;

        last_war_date = Info.game_date;

        //------- if this is the local player's nation -------//

        if (nation_type == NATION_OWN)
            NationArray.player_recno = nation_recno;

        //---------- init game vars ------------//

        int[] start_up_cash_array = { 4000, 7000, 12000, 20000 };

        if (is_ai())
            cash = start_up_cash_array[Config.ai_start_up_cash - 1];
        else
            cash = start_up_cash_array[Config.start_up_cash - 1];

        food = 5000.0; // startup food is 5000 for all nations in all settings

        //---- initialize this nation's relation on other nations ----//

        // #### Richard 6-12-2013: Moved this from init_relation to here, so it works with new independent nations spawning #### //
        is_allied_with_player = false;

        foreach (Nation nation in NationArray)
        {
            init_relation(nation);
            nation.init_relation(this);
        }

        //--------- init technology ----------//

        TechRes.init_nation_tech(nation_recno);

        //------- reset all god knowledge --------//

        GodRes.init_nation_know(nation_recno);

        //if(remote.is_enable() && nation_recno && !is_ai() && misc.is_file_exist("TECHGOD.SYS"))
        //{
        //tech_res.inc_all_tech_level(nation_recno);
        //tech_res.inc_all_tech_level(nation_recno);
        //tech_res.inc_all_tech_level(nation_recno);
        //god_res.enable_know_all(nation_recno);
        //}
        //else
        //{
        if (ConfigAdv.nation_start_god_level == 1)
        {
            GodRes[race_id].enable_know(nation_recno);
        }
        else if (ConfigAdv.nation_start_god_level > 1)
        {
            GodRes.enable_know_all(nation_recno);
        }

        for (int i = 0; i < ConfigAdv.nation_start_tech_inc_all_level; i++)
            TechRes.inc_all_tech_level(nation_recno);
        //}
    }

    public virtual void deinit()
    {
        if (nation_recno == 0) // has been deinitialized
            return;

        //---- delete all talk messages to/from this nation ----//

        TalkRes.del_all_nation_msg(nation_recno);

        //------- close down all firms --------//

        close_all_firm();

        //---- neutralize all towns belong to this nation ----//

        foreach (Town town in TownArray)
        {
            if (town.NationId == nation_recno)
                town.ChangeNation(0);
        }

        //------------- deinit our spies -------------//

        foreach (Spy spy in SpyArray)
        {
            //-----------------------------------------------------//
            // Convert all of spies of this nation to normal units, 
            // so there  will be no more spies of this nation. 
            //-----------------------------------------------------//

            if (spy.true_nation_recno == nation_recno) // retire counter-spies immediately
                spy.drop_spy_identity();

            //-----------------------------------------------------//
            // For spies of other nation cloaked as this nation,
            // their will uncover their cloak and change back
            // to their original nation. 
            //-----------------------------------------------------//

            else if (spy.cloaked_nation_recno == nation_recno)
            {
                // changing cloak is normally only allowed when mobile

                if (spy.spy_place == Spy.SPY_FIRM)
                {
                    // at least try to return spoils before it goes poof
                    if (!spy.can_capture_firm() || !spy.capture_firm())
                        spy.mobilize_firm_spy();
                }
                else if (spy.spy_place == Spy.SPY_TOWN)
                    spy.mobilize_town_spy();

                if (spy.spy_place == Spy.SPY_MOBILE) // what about on transport??
                    spy.change_cloaked_nation(spy.true_nation_recno);
            }
        }

        //----- deinit all units belonging to this nation -----//

        deinit_all_unit();

        //-------- if the viewing nation is this nation -------//

        if (Info.default_viewing_nation_recno == nation_recno)
        {
            Info.default_viewing_nation_recno = NationArray.player_recno;
            Sys.Instance.set_view_mode(InternalConstants.MODE_NORMAL);
        }

        else if (Info.viewing_nation_recno == nation_recno)
            Sys.Instance.set_view_mode(InternalConstants.MODE_NORMAL); // it will set viewing_nation_recno to default_viewing_nation_recno

        // if deleting own nation, darken view mode buttons
        if (nation_recno == NationArray.player_recno)
        {
            Sys.Instance.disp_view_mode(1);
        }

        nation_recno = 0;
    }

    public void init_relation(NationBase otherNation)
    {
        int otherNationRecno = otherNation.nation_recno;
        NationRelation nationRelation = new NationRelation();
        relation_array[otherNationRecno - 1] = nationRelation;

        set_relation_should_attack(otherNationRecno, otherNationRecno != nation_recno, InternalConstants.COMMAND_AUTO);

        // AI has contact with each other in the beginning of the game.
        if (is_ai() && NationArray[otherNationRecno].is_ai())
            nationRelation.has_contact = true;
        else
            nationRelation.has_contact = otherNationRecno == nation_recno || Config.explore_whole_map;

        nationRelation.trade_treaty = otherNationRecno == nation_recno;

        nationRelation.status = NATION_NEUTRAL;
        nationRelation.ai_relation_level = NATION_NEUTRAL * RELATION_LEVEL_PER_STATUS;
        nationRelation.last_change_status_date = Info.game_date;

        if (otherNationRecno == nation_recno) // own nation
            relation_status_array[otherNationRecno - 1] = NATION_ALLIANCE; // for facilitating searching
        else
            relation_status_array[otherNationRecno - 1] = NATION_NEUTRAL;

        set_relation_passable(otherNationRecno, NATION_FRIENDLY);
    }

    public void close_all_firm()
    {
        List<Firm> firmsToDelete = new List<Firm>();
        foreach (Firm firm in FirmArray)
        {
            if (firm.nation_recno == nation_recno)
            {
                firmsToDelete.Add(firm);
            }
        }

        foreach (Firm firm in firmsToDelete)
        {
            FirmArray.DeleteFirm(firm);
        }
    }

    public void deinit_all_unit()
    {
        //--- update total_human_unit so the numbers will be correct ---//

        // only do this in release version to on-fly fix bug
        NationArray.update_statistic();

        //--------------------------------------//

        List<Unit> unitsToDelete = new List<Unit>();
        foreach (Unit unit in UnitArray)
        {
            if (unit.nation_recno != nation_recno)
                continue;

            //----- only human units will betray -----//

            if (unit.race_id != 0)
            {
                unit.loyalty = 0; // force it to detray

                if (unit.think_betray())
                    continue;
            }

            //--- if the unit has not changed nation, the unit will disappear ---//

            if (!UnitArray.IsDeleted(unit.sprite_recno))
                unitsToDelete.Add(unit);
        }

        foreach (Unit unit in unitsToDelete)
        {
            UnitArray.DeleteUnit(unit);
        }
    }

    public void succeed_king(Unit newKing)
    {
        int newKingLeadership = 0;

        if (newKing.skill.skill_id == Skill.SKILL_LEADING)
            newKingLeadership = newKing.skill.skill_level;

        newKingLeadership = Math.Max(20, newKingLeadership); // give the king a minimum level of leadership

        //----- set the common loyalty change for all races ------//

        int loyaltyChange = 0;

        if (newKingLeadership < king_leadership)
            loyaltyChange = (newKingLeadership - king_leadership) / 2;

        if (newKing.rank_id != Unit.RANK_GENERAL)
            loyaltyChange -= 20;

        //---- update loyalty of units in this nation ----//

        foreach (Unit unit in UnitArray)
        {
            if (unit.sprite_recno == king_unit_recno || unit.sprite_recno == newKing.sprite_recno)
                continue;

            if (unit.nation_recno != nation_recno)
                continue;

            //--------- update loyalty change ----------//

            unit.change_loyalty(loyaltyChange +
                                succeed_king_loyalty_change(unit.race_id, newKing.race_id, race_id));
        }

        //---- update loyalty of units in camps ----//

        foreach (Firm firm in FirmArray)
        {
            if (firm.nation_recno != nation_recno)
                continue;

            //------ process military camps and seat of power -------//

            if (firm.firm_id == Firm.FIRM_CAMP || firm.firm_id == Firm.FIRM_BASE)
            {
                foreach (Worker worker in firm.workers)
                {
                    //--------- update loyalty change ----------//

                    worker.change_loyalty(loyaltyChange +
                                          succeed_king_loyalty_change(worker.race_id, newKing.race_id, race_id));
                }
            }
        }

        //---- update loyalty of town people ----//

        foreach (Town town in TownArray)
        {
            if (town.NationId != nation_recno)
                continue;

            for (int raceId = 1; raceId <= GameConstants.MAX_RACE; raceId++)
            {
                if (town.RacesPopulation[raceId - 1] == 0)
                    continue;

                //------ update loyalty now ------//

                town.ChangeLoyalty(raceId, loyaltyChange +
                                            succeed_king_loyalty_change(raceId, newKing.race_id, race_id));
            }
        }

        //------- add news --------//

        NewsArray.new_king(nation_recno, newKing.sprite_recno);

        //-------- set the new king now ------//

        set_king(newKing.sprite_recno, 0); // 0-not the first king, it is a succession

        //------ if the new king is a spy -------//

        if (newKing.spy_recno != 0)
        {
            Spy spy = SpyArray[newKing.spy_recno];

            if (newKing.true_nation_recno() == nation_recno) // if this is your spy
                spy.drop_spy_identity();
            else
                spy.think_become_king();
        }
    }

    public void set_king(int kingUnitRecno, int firstKing)
    {
        king_unit_recno = kingUnitRecno;

        Unit kingUnit = UnitArray[king_unit_recno];

        //--- if this unit currently has not have leadership ---//

        if (kingUnit.skill.skill_id != Skill.SKILL_LEADING)
        {
            kingUnit.skill.skill_id = Skill.SKILL_LEADING;
            kingUnit.skill.skill_level = 0;
        }

        kingUnit.set_rank(Unit.RANK_KING);
        // clear the existing order, as there might be an assigning to firm/town order. But kings cannot be assigned to towns or firms as workers.
        kingUnit.stop2();

        //---------- king related vars ----------//

        // for human players, the name is retrieved from NationArray::human_name_array
        if (nation_type == NATION_AI || firstKing == 0) // for succession, no longer use the original player name
            nation_name_id = kingUnit.name_id;
        else
            nation_name_id = -nation_recno;

        race_id = kingUnit.race_id;
        king_leadership = kingUnit.skill.skill_level;
    }

    public void hand_over_to(int handoverNationRecno)
    {
        RebelArray.stop_attack_nation(nation_recno);
        TownArray.StopAttackNation(nation_recno);
        UnitArray.stop_all_war(nation_recno);
        MonsterRes.stop_attack_nation(nation_recno);

        nation_hand_over_flag = nation_recno;

        //--- hand over units (should hand over units first as you cannot change a firm's nation without changing the nation of the overseer there ---//

        foreach (Unit unit in UnitArray)
        {
            //TODO check:
            //-- If the unit is dying and isn't truly deleted yet, delete it now --//

            //---------------------------------------//

            if (unit.nation_recno != nation_recno)
                continue;

            //----- if it is a god, resign it -------//

            if (GodRes.is_god_unit(unit.unit_id))
            {
                unit.resign(InternalConstants.COMMAND_AUTO);
                continue;
            }

            //----- if it is a spy cloaked as this nation -------//
            //
            // If the unit is a overseer of a Camp or Base, 
            // the Camp or Base will change nation as a result. 
            //
            //---------------------------------------------------//

            if (unit.spy_recno != 0)
                unit.spy_change_nation(handoverNationRecno, InternalConstants.COMMAND_AUTO);
            else
                unit.change_nation(handoverNationRecno);
        }

        //------- hand over firms ---------//

        foreach (Firm firm in FirmArray)
        {
            if (firm.nation_recno == nation_recno)
            {
                firm.change_nation(handoverNationRecno);
            }
        }

        //------- hand over towns ---------//

        foreach (Town town in TownArray)
        {
            if (town.NationId == nation_recno)
            {
                town.ChangeNation(handoverNationRecno);
            }
        }

        //-------------------------------------------------//
        //
        // For the spies of this nation cloaked into other nations,
        // we need to update their true_nation_recno. 
        //
        //-------------------------------------------------//

        foreach (Spy spy in SpyArray)
        {
            if (spy.true_nation_recno == nation_recno)
                spy.change_true_nation(handoverNationRecno);
        }

        //------- delete this nation from NationArray -------//

        //TODO check
        NationArray.DeleteNation((Nation)this);
        nation_hand_over_flag = 0;
    }

    public void next_day()
    {
        //------ post is at war flag -------/

        if (is_at_war_today)
            last_war_date = Info.game_date;

        is_at_war_yesterday = is_at_war_today;
        is_at_war_today = false;

        //--- if the king is dead, and now looking for a successor ---//

        if (king_unit_recno == 0)
        {
            if (Info.TotalDays % 3 == nation_recno % 3) // decrease 1 loyalty point every 3 days
                change_all_people_loyalty(-1);
        }

        //------ check if this nation has won the game -----//

        check_win();

        //-- if the player still hasn't selected a unit to succeed the died king, declare defeated if the all units are killed --//

        if (king_unit_recno == 0)
            check_lose();
    }

    public void next_month()
    {
        //--------------------------------------------------//
        // When the two nations, whose relationship is Tense,
        // do not have new conflicts for 3 years, their
        // relationship automatically becomes Neutral.
        //--------------------------------------------------//

        foreach (Nation nation in NationArray)
        {
            NationRelation nationRelation = get_relation(nation.nation_recno);

            if (nationRelation.status == NATION_TENSE &&
                Info.game_date >= nationRelation.last_change_status_date.AddDays(365.0 * 3.0))
            {
                set_relation_status(nation.nation_recno, NATION_NEUTRAL);
            }

            //--- update good_relation_duration_rating ---//

            else if (nationRelation.status == NATION_FRIENDLY)
                nationRelation.good_relation_duration_rating += 0.2; // this is the monthly increase

            else if (nationRelation.status == NATION_ALLIANCE)
                nationRelation.good_relation_duration_rating += 0.4;
        }

        //----- increase reputation gradually -----//

        if (reputation < 100)
            change_reputation(0.5);
    }

    public void next_year()
    {
        //------ post financial data --------//

        last_year_income = cur_year_income;
        cur_year_income = 0.0;

        last_year_expense = cur_year_expense;
        cur_year_expense = 0.0;

        last_year_fixed_income = cur_year_fixed_income;
        cur_year_fixed_income = 0.0;

        last_year_fixed_expense = cur_year_fixed_expense;
        cur_year_fixed_expense = 0.0;

        last_year_profit = cur_year_profit;
        cur_year_profit = 0.0;

        last_year_cheat = cur_year_cheat;
        cur_year_cheat = 0.0;

        //------ post income & expense breakdown ------//

        for (int i = 0; i < INCOME_TYPE_COUNT; i++)
        {
            last_year_income_array[i] = cur_year_income_array[i];
            cur_year_income_array[i] = 0.0;
        }

        for (int i = 0; i < EXPENSE_TYPE_COUNT; i++)
        {
            last_year_expense_array[i] = cur_year_expense_array[i];
            cur_year_expense_array[i] = 0.0;
        }

        //------ post good change data ------//

        last_year_food_in = cur_year_food_in;
        cur_year_food_in = 0.0;

        last_year_food_out = cur_year_food_out;
        cur_year_food_out = 0.0;

        last_year_food_change = cur_year_food_change;
        cur_year_food_change = 0.0;

        //---------- post imports ----------//

        for (int i = 0; i < GameConstants.MAX_NATION; i++)
        {
            NationRelation nationRelation = relation_array[i];
            for (int j = 0; j < IMPORT_TYPE_COUNT; j++)
            {
                nationRelation.last_year_import[j] = nationRelation.cur_year_import[j];
                nationRelation.cur_year_import[j] = 0.0;
            }
        }

        //--------- post reputation ----------//

        last_year_reputation_change = cur_year_reputation_change;
        cur_year_reputation_change = 0.0;
    }

    public void add_income(int incomeType, double incomeAmt, bool fixedIncome = false)
    {
        cash += incomeAmt;

        cur_year_income_array[incomeType] += incomeAmt;

        cur_year_income += incomeAmt;
        cur_year_profit += incomeAmt;

        if (fixedIncome)
            cur_year_fixed_income += incomeAmt;
    }

    public void add_expense(int expenseType, double expenseAmt, bool fixedExpense = false)
    {
        cash -= expenseAmt;

        cur_year_expense_array[expenseType] += expenseAmt;

        cur_year_expense += expenseAmt;
        cur_year_profit -= expenseAmt;

        if (fixedExpense)
            cur_year_fixed_expense += expenseAmt;
    }

    public void change_reputation(double changeLevel)
    {
        //--- reputation increase more slowly when it is close to 100 ----//

        if (changeLevel > 0.0 && reputation > 0.0)
            changeLevel = changeLevel * (150.0 - reputation) / 150.0;

        //-----------------------------------------------//

        reputation += changeLevel;

        if (reputation > 100.0)
            reputation = 100.0;

        if (reputation < -100.0)
            reputation = -100.0;

        //------ update cur_year_reputation_change ------//

        cur_year_reputation_change += changeLevel;
    }

    public void add_food(double foodToAdd)
    {
        food += foodToAdd;

        cur_year_food_in += foodToAdd;
        cur_year_food_change += foodToAdd;
    }

    public void consume_food(double foodConsumed)
    {
        food -= foodConsumed;

        cur_year_food_out += foodConsumed;
        cur_year_food_change -= foodConsumed;
    }

    public void import_goods(int importType, int importNationRecno, double importAmt)
    {
        if (importNationRecno == nation_recno)
            return;

        NationRelation nationRelation = get_relation(importNationRecno);

        nationRelation.cur_year_import[importType] += importAmt;
        nationRelation.cur_year_import[IMPORT_TOTAL] += importAmt;

        add_expense(EXPENSE_IMPORTS, importAmt, true);
        NationArray[importNationRecno].add_income(INCOME_EXPORTS, importAmt, true);
    }

    public void give_tribute(int toNationRecno, int tributeAmt)
    {
        Nation toNation = NationArray[toNationRecno];

        add_expense(EXPENSE_TRIBUTE, tributeAmt);

        toNation.add_income(INCOME_TRIBUTE, tributeAmt);

        NationRelation nationRelation = get_relation(toNationRecno);

        nationRelation.last_give_gift_date = Info.game_date;
        nationRelation.total_given_gift_amount += tributeAmt;

        //---- set the last rejected date so it won't request or give again soon ----//

        nationRelation.last_talk_reject_date_array[TalkMsg.TALK_GIVE_AID - 1] = default(DateTime);
        nationRelation.last_talk_reject_date_array[TalkMsg.TALK_DEMAND_AID - 1] = default(DateTime);
        nationRelation.last_talk_reject_date_array[TalkMsg.TALK_GIVE_TRIBUTE - 1] = default(DateTime);
        nationRelation.last_talk_reject_date_array[TalkMsg.TALK_DEMAND_TRIBUTE - 1] = default(DateTime);

        NationRelation nationRelation2 = toNation.get_relation(nation_recno);

        nationRelation2.last_talk_reject_date_array[TalkMsg.TALK_GIVE_AID - 1] = default(DateTime);
        nationRelation2.last_talk_reject_date_array[TalkMsg.TALK_DEMAND_AID - 1] = default(DateTime);
        nationRelation2.last_talk_reject_date_array[TalkMsg.TALK_GIVE_TRIBUTE - 1] = default(DateTime);
        nationRelation2.last_talk_reject_date_array[TalkMsg.TALK_DEMAND_TRIBUTE - 1] = default(DateTime);
    }

    public void give_tech(int toNationRecno, int techId, int techVersion)
    {
        Nation toNation = NationArray[toNationRecno];

        int curVersion = TechRes[techId].get_nation_tech_level(toNationRecno);

        if (curVersion < techVersion)
            TechRes[techId].set_nation_tech_level(toNationRecno, techVersion);

        NationRelation nationRelation = get_relation(toNationRecno);

        nationRelation.last_give_gift_date = Info.game_date;
        nationRelation.total_given_gift_amount += (techVersion - curVersion) * 500; // one version level is worth $500

        //---- set the last rejected date so it won't request or give again soon ----//

        nationRelation.last_talk_reject_date_array[TalkMsg.TALK_GIVE_TECH - 1] = default(DateTime);
        nationRelation.last_talk_reject_date_array[TalkMsg.TALK_DEMAND_TECH - 1] = default(DateTime);

        NationRelation nationRelation2 = toNation.get_relation(nation_recno);

        nationRelation2.last_talk_reject_date_array[TalkMsg.TALK_GIVE_TECH - 1] = default(DateTime);
        nationRelation2.last_talk_reject_date_array[TalkMsg.TALK_DEMAND_TECH - 1] = default(DateTime);
    }

    public void set_auto_collect_tax_loyalty(int loyaltyLevel)
    {
        auto_collect_tax_loyalty = loyaltyLevel;

        if (loyaltyLevel != 0 && auto_grant_loyalty >= auto_collect_tax_loyalty)
        {
            auto_grant_loyalty = auto_collect_tax_loyalty - 10;
        }
    }

    public void set_auto_grant_loyalty(int loyaltyLevel)
    {
        auto_grant_loyalty = loyaltyLevel;

        if (loyaltyLevel != 0 && auto_grant_loyalty >= auto_collect_tax_loyalty)
        {
            auto_collect_tax_loyalty = auto_grant_loyalty + 10;

            if (auto_collect_tax_loyalty > 100)
                auto_collect_tax_loyalty = 0; // disable auto collect tax if it's over 100
        }
    }

    // whether the nation has any people (but not counting the king). If no, then the nation is going to end.
    public bool has_people()
    {
        return all_population() > 0;
    }

    public void being_attacked(int attackNationRecno)
    {
        if (NationArray.IsDeleted(attackNationRecno) || attackNationRecno == nation_recno)
            return;

        //--- if it is an accidential attack (e.g. bullets attack with spreading damages) ---//

        Nation attackNation = NationArray[attackNationRecno];

        if (!attackNation.get_relation(nation_recno).should_attack)
            return;

        //--- check if there a treaty between these two nations ---//

        NationRelation nationRelation = get_relation(attackNationRecno);

        if (nationRelation.status != NATION_HOSTILE)
        {
            //--- if this nation (the one being attacked) has a higher than 0 reputation, the attacker's reputation will decrease ---//

            if (reputation > 0)
                attackNation.change_reputation(-reputation * 40.0 / 100.0);

            // how many times this nation has started a war with us, the more the times the worse this nation is.
            nationRelation.started_war_on_us_count++;

            if (nationRelation.status == NATION_ALLIANCE || nationRelation.status == NATION_FRIENDLY)
            {
                // the attacking nation abruptly terminates the treaty with us, not we terminate the treaty with them,
                // so attackNation.end_treaty() should be called instead of end_treaty()
                attackNation.end_treaty(nation_recno, NATION_HOSTILE);
            }
            else
            {
                set_relation_status(attackNationRecno, NATION_HOSTILE);
            }
        }

        //---- reset the inter-national peace days counter ----//

        NationArray.nation_peace_days = 0;
    }

    public void civilian_killed(int civilianRaceId, bool isAttacker, int penaltyType)
    {
        if (isAttacker)
        {
            if (penaltyType == 0) // mobile civilian
            {
                change_reputation(-0.3);
            }
            else if (penaltyType == 1) // town defender
            {
                change_all_people_loyalty(-1.0, civilianRaceId);
                change_reputation(-1.0);
            }
            else if (penaltyType == 2) // town resident
            {
                change_all_people_loyalty(-2.0, civilianRaceId);
                change_reputation(-1.0);
            }
            else if (penaltyType == 3) // trader
            {
                change_all_people_loyalty(-2.0, civilianRaceId);
                change_reputation(-10.0);
            }
        }
        else // is casualty
        {
            if (penaltyType == 0) // mobile civilian
            {
                change_reputation(-0.3);
            }
            else if (penaltyType == 1) // town defender
            {
                change_reputation(-0.3);
            }
            else if (penaltyType == 2) // town resident
            {
                change_all_people_loyalty(-1.0, civilianRaceId);
                change_reputation(-0.3);
            }
            else if (penaltyType == 3) // trader
            {
                change_all_people_loyalty(-0.6, civilianRaceId);
                change_reputation(-2.0);
            }
        }
    }

    public void change_all_people_loyalty(double loyaltyChange, int raceId = 0)
    {
        //---- update loyalty of units in this nation ----//

        foreach (Unit unit in UnitArray)
        {
            if (unit.sprite_recno == king_unit_recno)
                continue;

            if (unit.nation_recno != nation_recno)
                continue;

            //--------- update loyalty change ----------//

            if (raceId == 0 || unit.race_id == raceId)
                unit.change_loyalty((int)loyaltyChange);
        }

        //---- update loyalty of units in camps ----//

        foreach (Firm firm in FirmArray)
        {
            if (firm.nation_recno != nation_recno)
                continue;

            //------ process military camps and seat of power -------//

            if (firm.firm_id == Firm.FIRM_CAMP || firm.firm_id == Firm.FIRM_BASE)
            {
                foreach (Worker worker in firm.workers)
                {
                    if (raceId == 0 || worker.race_id == raceId)
                        worker.change_loyalty((int)loyaltyChange);
                }
            }
        }

        //---- update loyalty of town people ----//

        foreach (Town town in TownArray)
        {
            if (town.NationId != nation_recno)
                continue;

            //--------------------------------------//

            if (raceId != 0) // decrease loyalty of a specific race
            {
                if (town.RacesPopulation[raceId - 1] > 0)
                    town.ChangeLoyalty(raceId, loyaltyChange);
            }
            else // decrease loyalty of all races
            {
                for (int j = 0; j < GameConstants.MAX_RACE; j++)
                {
                    if (town.RacesPopulation[j] == 0)
                        continue;

                    town.ChangeLoyalty(j + 1, loyaltyChange);
                }
            }
        }
    }

    public void form_friendly_treaty(int nationRecno)
    {
        set_relation_status(nationRecno, NATION_FRIENDLY);
    }

    public void form_alliance_treaty(int nationRecno)
    {
        set_relation_status(nationRecno, NATION_ALLIANCE);

        //--- allied nations are oblied to trade with each other ---//

        set_trade_treaty(nationRecno, true);

        //------ set is_allied_with_player -------//

        if (nationRecno == NationArray.player_recno)
            is_allied_with_player = true;

        if (nation_recno == NationArray.player_recno)
            NationArray[nationRecno].is_allied_with_player = true;
    }

    public void end_treaty(int withNationRecno, int newStatus)
    {
        //----- decrease reputation when terminating a treaty -----//

        Nation withNation = NationArray[withNationRecno];

        if (withNation.reputation > 0)
        {
            int curStatus = get_relation_status(withNationRecno);

            if (curStatus == TalkMsg.TALK_END_FRIENDLY_TREATY)
                change_reputation(-withNation.reputation * 10.0 / 100.0);
            else
                change_reputation(-withNation.reputation * 20.0 / 100.0);
        }

        //------- reset good_relation_duration_rating -----//

        if (newStatus <= NATION_NEUTRAL)
        {
            get_relation(withNationRecno).good_relation_duration_rating = 0.0;
            withNation.get_relation(nation_recno).good_relation_duration_rating = 0.0;
        }

        //------- set new relation status --------//

        set_relation_status(withNationRecno, newStatus);

        //------ set is_allied_with_player -------//

        if (withNationRecno == NationArray.player_recno)
            is_allied_with_player = false;

        if (nation_recno == NationArray.player_recno)
            NationArray[withNationRecno].is_allied_with_player = false;
    }

    public void surrender(int toNationRecno)
    {
        NewsArray.nation_surrender(nation_recno, toNationRecno);

        //---- the king demote himself to General first ----//

        if (king_unit_recno != 0)
        {
            UnitArray[king_unit_recno].set_rank(Unit.RANK_GENERAL);
            king_unit_recno = 0;
        }

        //------- if the player surrenders --------//

        if (nation_recno == NationArray.player_recno)
            Sys.Instance.EndGame(0, 1, toNationRecno);

        //--- hand over the entire nation to another nation ---//

        hand_over_to(toNationRecno);
    }

    public void defeated()
    {
        //---- if the defeated nation is the player's nation ----//

        if (nation_recno == NationArray.player_recno)
        {
            Sys.Instance.EndGame(0, 1); // the player lost the game 
        }
        else // AI and remote players 
        {
            NewsArray.nation_destroyed(nation_recno);
        }

        //---- delete this nation from NationArray ----//

        //TODO check
        NationArray.DeleteNation((Nation)this);
    }

    public void check_win()
    {
        bool hasWon = goal_destroy_nation_achieved() ||
                      goal_destroy_monster_achieved() ||
                      goal_population_achieved() ||
                      goal_economic_score_achieved() ||
                      goal_total_score_achieved();

        if (!hasWon)
            return;

        // if the player achieves the goal, the player wins, if one of the other kingdoms achieves the goal, it wins.
        Sys.Instance.EndGame(nation_recno, 0);
    }

    public void check_lose()
    {
        //---- if the king of this nation is dead and it has no people left ----//

        if (king_unit_recno == 0 && !has_people())
            defeated();
    }

    public bool revealed_by_phoenix(int xLoc, int yLoc)
    {
        int effectiveRange = UnitRes[UnitConstants.UNIT_PHOENIX].visual_range;

        foreach (Unit unit in UnitArray)
        {
            if (unit.unit_id == UnitConstants.UNIT_PHOENIX && unit.nation_recno == nation_recno)
            {
                if (Misc.points_distance(xLoc, yLoc, unit.next_x_loc(), unit.next_y_loc()) <= effectiveRange)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private int succeed_king_loyalty_change(int thisRaceId, int newKingRaceId, int oldKingRaceId)
    {
        //----- the races of the new and old kings are different ----//

        if (newKingRaceId != oldKingRaceId)
        {
            //--- if this unit's race is the same as the new king ---//

            if (thisRaceId == newKingRaceId)
                return GameConstants.NEW_KING_SAME_RACE_LOYALTY_INC;

            //--- if this unit's race is the same as the old king ---//

            if (thisRaceId == oldKingRaceId)
                return GameConstants.NEW_KING_DIFFERENT_RACE_LOYALTY_DEC;
        }

        return 0;
    }
}