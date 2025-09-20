namespace TenKingdoms;

public static class GameConstants
{
    public const int MapSize = 400;
    public const int MAX_RACE = 10;
    public const int MAX_NATION = 10;
    public const int NEW_NATION_INTERVAL_DAYS = 365;
    public const int PEASANT_FOOD_YEAR_PRODUCTION = 30;
    public const int PERSON_FOOD_YEAR_CONSUMPTION = 10;
    public const double PEASANT_GOODS_MONTH_DEMAND = 0.5;
    public const int WORKER_GOODS_MONTH_DEMAND = 1;
    public const int NO_FOOD_LOYALTY_DECREASE_INTERVAL = 5;
    public const int RAW_PRICE = 1;           // Cost per raw material
    public const int PRODUCT_PRICE = 2;       // Cost per product
    public const int CONSUMER_PRICE = 4;      // Cost per product for consumers
    public const int EXPLORE_RANGE = 10;
    public const int MIN_FOOD_PURCHASE_PRICE = 5;
    public const int MAX_BRIBE_AMOUNT = 4000;
    public const int DISP_NEWS_DAYS = 60;     // how long a news should stay on the screen before it disappears
    public const int DISP_NEWS_COUNT = 5;
    public const int MAX_NEWS = 1000000;
    public const int TALK_MSG_KEEP_DAYS = 3650;
    public const int TALK_MSG_VALID_DAYS = 30;
    public const int MAX_WEATHER_FORECAST = 3;

    //Town
    public const int MAX_TOWN_GROWTH_POPULATION = 60;
    public const int MAX_TOWN_POPULATION = 60;
    public const int TRAIN_SKILL_COST = 30;
    public const int TRAIN_SKILL_LEVEL = 20;
    public const int TOTAL_TRAIN_DAYS = 5;
    public const int MIN_RECRUIT_LOYALTY = 30;
    public const int MIN_NATION_DEFEND_LOYALTY = 50;
    public const int MIN_INDEPENDENT_DEFEND_LOYALTY = 30;
    public const int SURRENDER_LOYALTY = 29;
    public const int REBEL_LOYALTY = 29;
    public const int REBEL_INTERVAL_MONTH = 3; // don't rebel twice in less than 3 months
    public const int INDEPENDENT_LINK_RESISTANCE = 50;
    public const int TAX_PER_PERSON = 5;
    public const int COLLECT_TAX_LOYALTY_DECREASE = 10;
    public const int TOWN_REWARD_PER_PERSON = 10;
    public const int TOWN_REWARD_LOYALTY_INCREASE = 10;
    public const int IND_TOWN_GRANT_PER_PERSON = 30;
    public const int IND_TOWN_GRANT_RESISTANCE_DECREASE = 10;
    public const double RESISTANCE_DECREASE_PER_WORKER = 0.2;
    // if the defender defeat the attackers and return the town with victory, the resistance will further increase
    public const int RESISTANCE_INCREASE_DEFENDER = 2;
    public const int CITIZEN_COMBAT_LEVEL = 10;
    public const int CITIZEN_SKILL_LEVEL = 10;
    public const int CITIZEN_HIT_POINTS = 20;
    public const int RECEIVED_HIT_PER_KILL = 200 / InternalConstants.ATTACK_SLOW_DOWN;
    public const int MAX_MIGRATE_PER_DAY = 4; // don't migrate more than 4 units per day
    public const int MIN_MIGRATE_ATTRACT_LEVEL = 30;
    public const int TOWN_SCAN_ENEMY_RANGE = 6;

    //Firm
    public const int MINE_MAX_STOCK_QTY = 500;
    public const int FACTORY_MAX_RAW_STOCK_QTY = 500;
    public const int FACTORY_MAX_STOCK_QTY = 500;
    public const int MARKET_MAX_STOCK_QTY = 500;
    public const int MIN_FIRM_STOCK_QTY = 100;
    public const int MIN_FACTORY_IMPORT_STOCK_QTY = 20;
    public const int PROCESS_GOODS_INTERVAL = 3; // Process goods in mines, factories and market places once 3 days
    public const int MAX_MARKET_GOODS = 3;
    public const int MAX_RAW = 3;
    public const int MAX_PRODUCT = 3;
    public const int MAX_SHIP_IN_HARBOR = 4;
    public const int MAX_BUILD_SHIP_QUEUE = 10;
    public const int MAX_INN_UNIT = 6;
    public const int MAX_INN_UNIT_PER_REGION = 10; // Region here means region of linked inns
    public const int MAX_MONSTER_GENERAL_IN_FIRM = 8;
    public const int MAX_SOLDIER_PER_GENERAL = 8;
    public const int MAX_MONSTER_IN_FIRM = 1 + MAX_MONSTER_GENERAL_IN_FIRM * (1 + MAX_SOLDIER_PER_GENERAL);
    public const int MONSTER_ATTACK_NEIGHBOR_RANGE = 6;
    public const int MONSTER_SOLDIER_COMBAT_LEVEL_DIVIDER = 2;
    public const int MONSTER_EXPAND_RANGE = 8;
    public const int MIN_MONSTER_CIVILIAN_DISTANCE = 10; // the minimum distance between monster firms and civilian towns & firms
    public const int MIN_GENERAL_EXPAND_NUM = 3;
    public const int EXPAND_FIRM_DISTANCE = 30;
    public const int FREE_SPACE_DISTANCE = 3;
    public const int CAN_SELL_HIT_POINTS_PERCENT = 80;
    public const int MAX_PRAY_POINTS = 400;
    
    //Unit
    public const int WORKER_YEAR_SALARY = 10;
    public const int SOLDIER_YEAR_SALARY = 10;
    public const int GENERAL_YEAR_SALARY = 50;
    public const int SPY_YEAR_SALARY = 100;
    public const int SPY_KILLED_REPUTATION_DECREASE = 3;
    public const int SPY_ENEMY_RANGE = 5;
    public const int REWARD_COST = 30;
    public const int REWARD_LOYALTY_INCREASE = 10;
    public const int PROMOTE_LOYALTY_INCREASE = 20;
    public const int DEMOTE_LOYALTY_DECREASE = 40;
    public const int UNIT_BETRAY_LOYALTY = 30;
    public const int POPULATION_PER_CARAVAN = 10;
    public const double REPUTATION_INCREASE_PER_ATTACK_MONSTER = 0.001;
    public const int NEW_KING_SAME_RACE_LOYALTY_INC = 20;
    public const int NEW_KING_DIFFERENT_RACE_LOYALTY_DEC = 30;
    public const int MAX_CARAVAN_WAIT_TERM = 8;
    public const int MAX_SHIP_WAIT_TERM = 8;
    public const int CHAIN_TRIGGER_RANGE = 2;
    public const int EXPLODE_RANGE = 1;
    public const int EXPLODE_DAMAGE = 50;
    public const int EFFECTIVE_LEADING_DISTANCE = 10;
    public const int MAX_NATION_CONTRIBUTION = 1000000;
    public const int MAX_CARAVAN_CARRY_QTY = 100;	// Maximum qty of goods a caravan can carry

    //Raw resource
    public const int MAX_RAW_RESERVE_QTY = 20000;
    public const int SMALLEST_RAW_REGION = 50; // only put raw on the region if its size is larger than this
    // only sites with reserve qty >= 5% of MAX_RAW_RESERVE_QTY are counted as raw sites
    public const int EXIST_RAW_RESERVE_QTY = MAX_RAW_RESERVE_QTY / 20;
}

public static class InternalConstants
{
    public const int MAX_COLOR_SCHEME = GameConstants.MAX_NATION;
    public const int FRAMES_PER_DAY = 10;
    public const int SCROLL_WIDTH = 8;
    public const int SCROLL_INTERVAL = 100;
    public const int MIN_AUDIO_VOL = 10;
    public const int DEFAULT_VOL_DROP = 100;
    public const int DEFAULT_DIST_LIMIT = 40;
    public const int DEFAULT_PAN_DROP = 100; // distance 100, extreme left or extreme right

    // how many times attacking damages should be reduced, this lessens all attacking damages, thus slowing down all battles.
    public const int ATTACK_SLOW_DOWN = 4;
    public const int GUARD_COUNT_MAX = 5;
    public const int DAMAGE_POINT_RADIUS = 32;
    public const int STD_ACTION_RETRY_COUNT = 4;
    public const int WARPOINT_ZONE_SIZE = 8;
    public const int MAX_BIRD = 17;
    public const int WAVE_CYCLE = 8;
    public const int MAX_ROCK_WIDTH = 4;
    public const int MAX_ROCK_HEIGHT = 4;
    public const int GET_SITE_RANGE = 30; // only notify units within this range
    public const int MAX_UNIT_TO_GET_SITE = 5;
    public const int SCAN_FIRE_DIST = 4;

    public const int TOWN_WIDTH = 4;
    public const int TOWN_HEIGHT = 4;
    public const int MIN_INTER_TOWN_DISTANCE = 16;
    public const int TOWN_MAX_TRAIN_QUEUE = 10;
    // Number of units enqueued when holding shift - ensure this is less than MAX_TRAIN_QUEUE
    public const int TOWN_TRAIN_BATCH_COUNT = 8;
    
    public const int EFFECTIVE_TOWN_TOWN_DISTANCE = 8;
    public const int EFFECTIVE_FIRM_TOWN_DISTANCE = 8;
    public const int EFFECTIVE_FIRM_FIRM_DISTANCE = 8;
    public const int EFFECTIVE_POWER_DISTANCE = 3;
    public const int WALL_SPACE_LOC = 5;

    public const int NO_STOP_DEFINED = 0;
    public const int ON_WAY_TO_FIRM = 1;
    public const int SURROUND_FIRM = 2;
    public const int INSIDE_FIRM = 3;
    public const int SURROUND_FIRM_WAIT_FACTOR = 10;
    
    public const int CellWidth = 32;
    public const int CellHeight = 32;
    public const int CellWidthShift = 5; // 2^5 = CellWidth
    public const int CellHeightShift = 5; // 2^5 = CellHeight
    
    public const int COMMAND_PLAYER = 0;
    public const int COMMAND_REMOTE = 1;
    public const int COMMAND_AI = 2;
    public const int COMMAND_AUTO = 3;

    public const int LINK_DD = 0; // both sides disabled
    public const int LINK_ED = 1; // host side enabled, client side disabled
    public const int LINK_DE = 2; // host side disabled, client side enabled
    public const int LINK_EE = 3; // both sides enabled

    public const int DIR_N = 0;     // building directions
    public const int DIR_NE = 1;
    public const int DIR_E = 2;
    public const int DIR_SE = 3;
    public const int DIR_S = 4;
    public const int DIR_SW = 5;
    public const int DIR_W = 6;
    public const int DIR_NW = 7;
    public const int MAX_SPRITE_DIR_TYPE = 8;

    public const int MODE_NORMAL = 0;
    public const int MODE_NATION = 1;
    public const int MODE_TOWN = 2;
    public const int MODE_ECONOMY = 3;
    public const int MODE_TRADE = 4;
    public const int MODE_MILITARY = 5;
    public const int MODE_TECH = 6;
    public const int MODE_SPY = 7;
    public const int MODE_RANK = 8;
    public const int MODE_NEWS_LOG = 9;
    public const int MODE_AI_ACTION = 10;
}

public static class InputConstants
{
    public const int LeftMouseDown = 1;
    public const int LeftMouseUp = 2;
    public const int RightMouseDown = 3;
    public const int RightMouseUp = 4;
    public const int MouseMotion = 5;
    public const int KeyBPressed = 98;
    public const int KeySPressed = 115;
    public const int KeyTPressed = 116;
    public const int KeyLeftPressed = 129;
    public const int KeyRightPressed = 130;
    public const int KeyDownPressed = 131;
    public const int KeyUpPressed = 132;
}
