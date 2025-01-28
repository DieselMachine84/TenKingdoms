using System;

namespace TenKingdoms;

[Flags]
public enum ConfigVersion
{
    FLAG_DEBUG_VER = 1,
    FLAG_DEVEL_VER = 2,
    FLAG_CKSUM_REQ = 4,
    FLAG_UNKNOWN_BUILD = 8,
}

public class ConfigAdv
{
    public int checksum;
    public ConfigVersion flags;

    // firm settings
    public bool firm_mobilize_civilian_aggressive;
    public bool firm_migrate_stricter_rules;

    // bug fix settings
    public bool fix_path_blocked_by_team;
    public bool fix_recruit_dec_loyalty;
    public bool fix_sea_travel_final_move;
    public bool fix_town_unjob_worker;

    // locale settings
    public string locale = String.Empty;

    // mine settings
    public bool mine_unlimited_reserve;

    // monster settings
    public bool monster_alternate_attack_curve;
    public int monster_attack_divisor;

    // nation settings
    public int nation_ai_unite_min_relation_level;
    public int nation_start_god_level;
    public int nation_start_tech_inc_all_level;

    // race settings
    public int[] race_random_list = new int[GameConstants.MAX_RACE];
    public int race_random_list_max;

    // remote settings
    public int remote_compare_object_crc;
    public int remote_compare_random_seed;

    // scenario settings
    public int scenario_config;

    // town settings
    public int town_ai_emerge_nation_pop_limit;
    public int town_ai_emerge_town_pop_limit;
    public bool town_migration;
    public bool town_loyalty_qol;

    // unit settings
    public bool unit_ai_team_help;
    public bool unit_finish_attack_move;
    public bool unit_loyalty_require_local_leader;
    public bool unit_allow_path_power_mode;
    public bool unit_spy_fixed_target_loyalty;
    public bool unit_target_move_range_cycle;

    // vga settings
    public bool vga_allow_highdpi;
    public bool vga_full_screen;
    public bool vga_full_screen_desktop;
    public bool vga_keep_aspect_ratio;
    public bool vga_pause_on_focus_loss;

    public int vga_window_width;
    public int vga_window_height;

    // wall settings
    public bool wall_building_allowed;

    public ConfigAdv()
    {
        Reset();
    }

    private void Reset()
    {
        firm_mobilize_civilian_aggressive = false;
        firm_migrate_stricter_rules = true;

        fix_path_blocked_by_team = true;
        fix_recruit_dec_loyalty = true;
        fix_sea_travel_final_move = true;
        fix_town_unjob_worker = true;

        mine_unlimited_reserve = false;

        monster_alternate_attack_curve = false;
        monster_attack_divisor = 4;

        nation_ai_unite_min_relation_level = NationBase.NATION_NEUTRAL;
        nation_start_god_level = 0;
        nation_start_tech_inc_all_level = 0;

        race_random_list_max = GameConstants.MAX_RACE;
        for (int i = 0; i < race_random_list_max; i++)
            race_random_list[i] = i + 1;

        remote_compare_object_crc = 1;
        remote_compare_random_seed = 1;

        scenario_config = 1;

        town_ai_emerge_nation_pop_limit = 60 * GameConstants.MAX_NATION;
        town_ai_emerge_town_pop_limit = 1000;
        town_migration = true;
        town_loyalty_qol = true;

        unit_ai_team_help = true;
        unit_allow_path_power_mode = false;
        unit_finish_attack_move = true;
        unit_loyalty_require_local_leader = true;
        unit_spy_fixed_target_loyalty = false;
        unit_target_move_range_cycle = false;

        vga_allow_highdpi = false;
        vga_full_screen = true;
        vga_full_screen_desktop = true;
        vga_keep_aspect_ratio = true;
        vga_pause_on_focus_loss = false;

        vga_window_width = 0;
        vga_window_height = 0;

        wall_building_allowed = false;

        // after applying defaults, checksum is not required
        checksum = 0;
        flags &= ~ConfigVersion.FLAG_CKSUM_REQ;
    }
    
    public int GetRandomRace()
    {
        return race_random_list[Misc.Random(race_random_list_max)];
    }
}
