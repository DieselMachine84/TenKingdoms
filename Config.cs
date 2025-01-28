using System;

namespace TenKingdoms;

public class Config
{
	public const int OPTION_NONE = 0;
	public const int OPTION_LOW = 1;
	public const int OPTION_MODERATE = 2;
	public const int OPTION_HIGH = 3;
	public const int OPTION_VERY_HIGH = 4;

	public const int OPTION_MONSTER_NONE = 0;
	public const int OPTION_MONSTER_DEFENSIVE = 1;
	public const int OPTION_MONSTER_OFFENSIVE = 2;

	public const int OPTION_VERY_EASY = 0;
	public const int OPTION_EASY = 1;
	public const int OPTION_MEDIUM = 2;
	public const int OPTION_HARD = 3;
	public const int OPTION_VERY_HARD = 4;
	public const int OPTION_CUSTOM = 5;

	public const int OPTION_DISPLAY_MAJOR_NEWS = 0;
	public const int OPTION_DISPLAY_ALL_NEWS = 1;
	
	public const int SMALL_STARTUP_RESOURCE = 4000;
	public const int MEDIUM_STARTUP_RESOURCE = 7000;
	public const int LARGE_STARTUP_RESOURCE = 12000;
	public const int VERY_LARGE_STARTUP_RESOURCE = 20000;

	public static int[] table_ai_nation_count = { 2, 4, 6, 6, 6 };
	public static int[] table_start_up_cash = { OPTION_HIGH, OPTION_HIGH, OPTION_MODERATE, OPTION_MODERATE, OPTION_LOW };
	public static int[] table_ai_start_up_cash = { OPTION_LOW, OPTION_MODERATE, OPTION_MODERATE, OPTION_HIGH, OPTION_VERY_HIGH };
	public static int[] table_ai_aggressiveness = { OPTION_LOW, OPTION_LOW, OPTION_MODERATE, OPTION_HIGH, OPTION_VERY_HIGH };
	public static int[] table_start_up_independent_town = { 30, 30, 15, 15, 7 };
	public static int[] table_start_up_raw_site = { 7, 6, 6, 4, 3, };
	public static bool[] table_explore_whole_map = { true, true, true, true, false };
	public static bool[] table_fog_of_war = { false, false, false, true, true };
	public static bool[] table_new_independent_town_emerge = { true, true, true, true, true };
	public static int[] table_independent_town_resistance = { OPTION_LOW, OPTION_LOW, OPTION_MODERATE, OPTION_HIGH, OPTION_HIGH };
	public static int[] table_random_event_frequency = { OPTION_NONE, OPTION_LOW, OPTION_MODERATE, OPTION_MODERATE, OPTION_MODERATE };
	public static bool[] table_new_nation_emerge = { false, false, false, true, true };
	public static int[] table_monster_type = {
		OPTION_MONSTER_DEFENSIVE, OPTION_MONSTER_DEFENSIVE, OPTION_MONSTER_DEFENSIVE,
		OPTION_MONSTER_OFFENSIVE, OPTION_MONSTER_OFFENSIVE
	};
	public static bool[] table_start_up_has_mine_nearby = { true, true, false, false, false };
	public static bool[] table_random_start_up = { false, false, false, true, true };
	
	//--------- GLOBAL GAME SETTING --------//
	//
	// parameters under GLOBAL GAME SETTING should remain unchanged
	// after the game starts, and are the same across other players
	// in a multiplayer game
	// (i.e. change_game_setting() should updates all these setting)
	//
	//--------------------------------------//

	//------- parameter settings --------//

	public int difficulty_rating;

	public int ai_nation_count; // no. of AI nations in the game

	public int start_up_cash;
	public int ai_start_up_cash;

	//public int start_up_food;
	//public int ai_start_up_food;

	public int ai_aggressiveness;
	public int start_up_independent_town;
	public int start_up_raw_site;
	public int difficulty_level;

	//-------- option settings  ---------//

	public bool explore_whole_map; // whether the map is explored at first place
	public bool fog_of_war;

	public int terrain_set;
	public int latitude;
	public int weather_effect;
	public int land_mass;

	public bool new_independent_town_emerge;
	public int independent_town_resistance; // whether independent towns' defenders have higher combat levels
	public int random_event_frequency;
	public bool new_nation_emerge;
	public int monster_type;
	public bool start_up_has_mine_nearby;
	public bool random_start_up;

	//--------- goal ----------//

	public bool goal_destroy_monster;
	public bool goal_population_flag;
	public bool goal_economic_score_flag;
	public bool goal_total_score_flag;
	public bool goal_year_limit_flag;

	public int goal_population;
	public int goal_economic_score;
	public int goal_total_score;
	public int goal_year_limit;

	//------- game setting on fire ---------//

	public int fire_spread_rate; // 0 to disable, 10 for normal
	public int wind_spread_fire_rate; // 0 to disable, 5 for normal
	public int fire_fade_rate; // 1 for slow, 2 for fast
	public int fire_restore_prob; // 0 to 100, 5 for normal
	public int rain_reduce_fire_rate; // 0 to 20, 5 for normal
	public int fire_damage; // 0 to disable 2 for normal

	//--------- CHEAT GAME SETTING --------//
	//
	// parameters under CHEAT GAME SETTING can be changed
	// after the game starts, and must be reset in a multiplayer game
	// (i.e. reset_cheat_setting() can reset all these setting)
	//
	//--------------------------------------//
	public bool show_ai_info;
	public bool fast_build; // fast building everything
	public bool disable_ai_flag;
	public bool king_undie_flag; // for testing game only

	//--------- LOCAL GAME SETTING --------//
	//
	// parameters under LOCAL GAME SETTING should remain unchanged
	// after the game starts, may not be the same across other players
	//
	//-------------------------------------//

	public int race_id;
	public string player_name;
	public int player_nation_color;

	public bool expired_flag;

	//--------- PREFERENCE --------//
	//
	// parameters under PREFERENCE are changeable during the game
	// the game will not be affected at any setting
	//
	//-------------------------------------//

	public int opaque_report;
	public int disp_news_flag;

	public int scroll_speed; // map scrolling speed. 1-slowest, 10-fastest
	public int frame_speed; // game speed, the desired frames per second

	public int help_mode;
	public int disp_town_name;
	public int disp_spy_sign;
	public int show_all_unit_icon; // 0:show icon when pointed, 1:always
	public int show_unit_path; // bit 0 show unit path on ZoomMatrix, bit 1 for MapMatrix

	//------- sound effect --------//

	public int music_flag;
	public int cd_music_volume; // a value from 0 to 100
	public int wav_music_volume; // a value from 0 to 100

	public bool sound_effect_flag;
	public int sound_effect_volume; // a value from 0 to 100

	public bool pan_control; // mono speaker should disable pan_control

	//------- weather visual effect flags -------//

	public int lightning_visual;
	public int earthquake_visual;
	public int rain_visual;
	public int snow_visual;
	public int snow_ground; // 0=disable, 1=i_snow, 2=snow_res

	//-------- weather audio effect flags -------//

	public int lightning_audio;
	public int earthquake_audio;
	public int rain_audio;
	public int snow_audio; // not used
	public int wind_audio;

	//--------- weather visual effect parameters --------//

	public int lightning_brightness; // 0, 20, 40 or 60
	public int cloud_darkness; // 0 to 5, 0 to disable cloud darkness

	//-------- weather audio effect parameters ----------//

	public int lightning_volume; // default 100
	public int earthquake_volume; // default 100
	public int rain_volume; // default 90
	public int snow_volume; // default 100
	public int wind_volume; // default 70

	//------------ map preference -------------//

	public bool blacken_map; // whether the map is blackened at the first place
	public int explore_mask_method; // 0 for none, 1 for masking, 2 for remapping
	public int fog_mask_method; // 1 for fast masking, 2 for slow remapping

	public Config()
	{
		default_game_setting();
		default_cheat_setting();
		default_local_game_setting();
		default_preference();
	}

	public void Init()
	{
		default_game_setting();
		default_cheat_setting();
		default_local_game_setting();
		default_preference();
	}

	public void Deinit()
	{
		//TODO
		//config.save("CONFIG.DAT");		// save the config when the game quits

		default_game_setting();
		default_local_game_setting();
		default_preference();
	}

	public void default_game_setting()
	{
		// -------- GLOBAL GAME SETTING -------- //

		ai_nation_count = GameConstants.MAX_NATION;
		start_up_cash = OPTION_MODERATE;
		ai_start_up_cash = OPTION_MODERATE;
		//start_up_food = MEDIUM_STARTUP_RESOURCE;
		//ai_start_up_food = MEDIUM_STARTUP_RESOURCE;
		//ai_aggressiveness = OPTION_MODERATE;
		ai_aggressiveness = OPTION_VERY_HIGH;
		start_up_independent_town = 15;
		start_up_raw_site = 5;
		difficulty_level = OPTION_CUSTOM;

		explore_whole_map = true;
		fog_of_war = false;

		terrain_set = 1;
		latitude = 45;
		weather_effect = 1; // damage done by weather
		land_mass = OPTION_MODERATE;

		new_independent_town_emerge = true;
		independent_town_resistance = OPTION_MODERATE;
		random_event_frequency = OPTION_NONE;
		monster_type = OPTION_MONSTER_OFFENSIVE;
		new_nation_emerge = true;
		start_up_has_mine_nearby = false;
		random_start_up = false;

		//change_difficulty(OPTION_VERY_EASY);

		//-------- goal --------//

		goal_destroy_monster = false;
		goal_population_flag = false;
		goal_economic_score_flag = false;
		goal_total_score_flag = false;
		goal_year_limit_flag = false;

		goal_population = 300;
		goal_economic_score = 300;
		goal_total_score = 600;
		goal_year_limit = 20;

		// game setting on fire
		fire_spread_rate = 0; // 0 to disable, 10 for normal
		wind_spread_fire_rate = 5; // 0 to disable, 5 for normal
		fire_fade_rate = 2; // 1 for slow, 2 for fast
		fire_restore_prob = 80; // 0 to 100, 5 for normal (with spreading) 
		rain_reduce_fire_rate = 5; // 0 to 20, 5 for normal
		fire_damage = 2; // 0 to disable 2 for normal
	}

	public void default_cheat_setting()
	{
		show_ai_info = false;
		fast_build = false;
		disable_ai_flag = false;
		king_undie_flag = false;
	}

	public void default_local_game_setting()
	{
		race_id = 1;
		player_name = "New Player";
		player_nation_color = 1;
		expired_flag = false;
	}

	public void default_preference()
	{
		opaque_report = 0; // opaque report instead of transparent report
		disp_news_flag = OPTION_DISPLAY_ALL_NEWS;

		scroll_speed = 5;
		frame_speed = 12;

		help_mode = 0;
		disp_town_name = 1;
		disp_spy_sign = 1;
		show_all_unit_icon = 1; // 0:show icon when pointed, 1:always
		show_unit_path = 3; // bit 0 show unit path on ZoomMatrix, bit 1 for MapMatrix

		// music setting
		music_flag = 1;
		cd_music_volume = 100;
		wav_music_volume = 100;

		// sound effect setting
		sound_effect_flag = true;
		sound_effect_volume = 100;
		pan_control = true;

		// weather visual effect flags
		lightning_visual = 1;
		earthquake_visual = 1;
		rain_visual = 1;
		snow_visual = 1;
		snow_ground = 0; // 0=disable, 1=i_snow, 2=snow_res

		// weather audio effect flags
		lightning_audio = 1;
		earthquake_audio = 1;
		rain_audio = 1;
		snow_audio = 0; // not used
		wind_audio = 1;

		// weather visual effect parameters
		lightning_brightness = 20;
		cloud_darkness = 5;

		// weather audio effect parameters
		lightning_volume = 100;
		earthquake_volume = 100;
		rain_volume = 90;
		snow_volume = 100;
		wind_volume = 70;

		// other
		blacken_map = true;
		explore_mask_method = 2;
		fog_mask_method = 2;

		enable_weather_visual();
		enable_weather_audio();
		cloud_darkness = 0;
	}

	public void change_game_setting(Config other)
	{
		//-------- game settings  ---------//
		ai_nation_count = other.ai_nation_count;
		start_up_cash = other.start_up_cash;
		ai_start_up_cash = other.ai_start_up_cash;
		// start_up_food          = other.start_up_food;
		// ai_start_up_food       = other.ai_start_up_food;
		ai_aggressiveness = other.ai_aggressiveness;
		start_up_independent_town = other.start_up_independent_town;
		start_up_raw_site = other.start_up_raw_site;
		difficulty_level = other.difficulty_level;

		explore_whole_map = other.explore_whole_map;
		fog_of_war = other.fog_of_war;

		terrain_set = other.terrain_set;
		latitude = other.latitude;
		weather_effect = other.weather_effect;
		land_mass = other.land_mass;

		new_independent_town_emerge = other.new_independent_town_emerge;
		independent_town_resistance = other.independent_town_resistance;
		random_event_frequency = other.random_event_frequency;
		monster_type = other.monster_type;
		new_nation_emerge = other.new_nation_emerge;
		start_up_has_mine_nearby = other.start_up_has_mine_nearby;
		random_start_up = other.random_start_up;

		// --------- goal ---------//

		goal_destroy_monster = other.goal_destroy_monster;
		goal_population_flag = other.goal_population_flag;
		goal_economic_score_flag = other.goal_economic_score_flag;
		goal_total_score_flag = other.goal_total_score_flag;
		goal_year_limit_flag = other.goal_year_limit_flag;
		goal_population = other.goal_population;
		goal_economic_score = other.goal_economic_score;
		goal_total_score = other.goal_total_score;
		goal_year_limit = other.goal_year_limit;

		// ------- game setting on fire ---------//

		fire_spread_rate = other.fire_spread_rate;
		wind_spread_fire_rate = other.wind_spread_fire_rate;
		fire_fade_rate = other.fire_fade_rate;
		fire_restore_prob = other.fire_restore_prob;
		rain_reduce_fire_rate = other.rain_reduce_fire_rate;
		fire_damage = other.fire_damage;
	}

	public void change_preference(Config other)
	{
		opaque_report = other.opaque_report;
		disp_news_flag = other.disp_news_flag;

		scroll_speed = other.scroll_speed;
		frame_speed = other.frame_speed;

		help_mode = other.help_mode;
		disp_town_name = other.disp_town_name;
		disp_spy_sign = other.disp_spy_sign;
		show_all_unit_icon = other.show_all_unit_icon;
		show_unit_path = other.show_unit_path;

		//------- sound effect --------//

		music_flag = other.music_flag;
		cd_music_volume = other.cd_music_volume;
		wav_music_volume = other.wav_music_volume;
		sound_effect_flag = other.sound_effect_flag;
		sound_effect_volume = other.sound_effect_volume;
		pan_control = other.pan_control;

		//------- weather visual effect flags -------//

		lightning_visual = other.lightning_visual;
		earthquake_visual = other.earthquake_visual;
		rain_visual = other.rain_visual;
		snow_visual = other.snow_visual;
		snow_ground = other.snow_ground;

		//-------- weather audio effect flags -------//

		lightning_audio = other.lightning_audio;
		earthquake_audio = other.earthquake_audio;
		rain_audio = other.rain_audio;
		snow_audio = other.snow_audio;
		wind_audio = other.wind_audio;

		//--------- weather visual effect parameters --------//

		lightning_brightness = other.lightning_brightness;
		cloud_darkness = other.cloud_darkness;

		//-------- weather audio effect parameters ----------//

		lightning_volume = other.lightning_volume;
		earthquake_volume = other.earthquake_volume;
		rain_volume = other.rain_volume;
		snow_volume = other.snow_volume;
		wind_volume = other.wind_volume;

		//------------ map preference -------------//

		blacken_map = other.blacken_map;
		explore_mask_method = other.explore_mask_method;
		fog_mask_method = other.fog_mask_method;
	}

	public void change_difficulty(int newDifficulty)
	{
		difficulty_level = newDifficulty;

		ai_nation_count = table_ai_nation_count[newDifficulty];
		start_up_cash = table_start_up_cash[newDifficulty];
		ai_start_up_cash = table_ai_start_up_cash[newDifficulty];
		//start_up_food = table_start_up_food[newDifficulty];
		//ai_start_up_food = table_ai_start_up_food[newDifficulty];
		ai_aggressiveness = table_ai_aggressiveness[newDifficulty];
		start_up_independent_town = table_start_up_independent_town[newDifficulty];
		start_up_raw_site = table_start_up_raw_site[newDifficulty];
		explore_whole_map = table_explore_whole_map[newDifficulty];
		fog_of_war = table_fog_of_war[newDifficulty];

		new_independent_town_emerge = table_new_independent_town_emerge[newDifficulty];
		independent_town_resistance = table_independent_town_resistance[newDifficulty];
		random_event_frequency = table_random_event_frequency[newDifficulty];
		new_nation_emerge = table_new_nation_emerge[newDifficulty];
		monster_type = table_monster_type[newDifficulty];
		start_up_has_mine_nearby = table_start_up_has_mine_nearby[newDifficulty];
		random_start_up = table_random_start_up[newDifficulty];
	}

	public int single_player_difficulty()
	{
		int score = 10;
		score += ai_nation_count * 6;
		if (!explore_whole_map)
			score += 7;
		if (fog_of_war)
			score += 7;
		score += (7 - Math.Max(start_up_raw_site, 7)) * 5;

		//	if( start_up_cash <= SMALL_STARTUP_RESOURCE )
		//		score += 16;
		//	else if( start_up_cash < LARGE_STARTUP_RESOURCE )
		//		score += 8;
		//	else
		//		score += 0;
		score += (4 - start_up_cash) * 6;

		//	if( ai_start_up_cash <= SMALL_STARTUP_RESOURCE )
		//		score += 0;
		//	else if( ai_start_up_cash < LARGE_STARTUP_RESOURCE )
		//		score += 8;
		//	else
		//		score += 16;
		score += (ai_start_up_cash - 1) * 6;

		score += (ai_aggressiveness - 1) * 10;
		score += (independent_town_resistance - 1) * 5;
		if (new_nation_emerge)
			score += 6;
		score += random_event_frequency * 2;
		switch (monster_type)
		{
			case OPTION_MONSTER_NONE:
				break;
			case OPTION_MONSTER_DEFENSIVE:
				score += 6;
				break;
			case OPTION_MONSTER_OFFENSIVE:
				score += 16;
				break;
		}

		if (!start_up_has_mine_nearby)
			score += 6;
		return score;
	}

	public int multi_player_difficulty(int remotePlayers)
	{
		int totalOpp = ai_nation_count + remotePlayers;
		if (totalOpp > GameConstants.MAX_NATION - 1)
			totalOpp = GameConstants.MAX_NATION - 1;
		return single_player_difficulty() + (totalOpp - ai_nation_count) * 6;
	}

	public void reset_cheat_setting()
	{
		show_ai_info = false;
		fast_build = false;
		disable_ai_flag = false;
		king_undie_flag = false;
	}

	public void enable_weather_visual()
	{
		lightning_visual = 1;
		earthquake_visual = 1;
		rain_visual = 1;
		snow_visual = 1;
		snow_ground = 0; // 0=disable, 1=i_snow, 2=snow_res
		cloud_darkness = 1;
	}

	public void disable_weather_visual()
	{
		lightning_visual = 0;
		earthquake_visual = 0;
		rain_visual = 0;
		snow_visual = 0;
		snow_ground = 0; // 0=disable, 1=i_snow, 2=snow_res
		cloud_darkness = 0;
	}

	public void enable_weather_audio()
	{
		lightning_audio = 1;
		earthquake_audio = 1;
		rain_audio = 1;
		snow_audio = 1; // not used
		wind_audio = 1;
	}

	public void disable_weather_audio()
	{
		lightning_audio = 0;
		earthquake_audio = 0;
		rain_audio = 0;
		snow_audio = 0; // not used
		wind_audio = 0;
	}
}