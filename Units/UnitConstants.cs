namespace TenKingdoms;

public static class UnitConstants
{
    public const int UNIT_NORMAN = 1;
    public const int UNIT_MAYA = 2;
    public const int UNIT_GREEK = 3;
    public const int UNIT_VIKING = 4;
    public const int UNIT_PERSIAN = 5;
    public const int UNIT_CHINESE = 6;
    public const int UNIT_JAPANESE = 7;
    public const int UNIT_CARAVAN = 8;
    public const int UNIT_CATAPULT = 9;
    public const int UNIT_BALLISTA = 10;
    public const int UNIT_FLAMETHROWER = 11;
    public const int UNIT_CANNON = 12;
    public const int UNIT_EXPLOSIVE_CART = 13;
    public const int UNIT_VESSEL = 14;
    public const int UNIT_TRANSPORT = 15;
    public const int UNIT_CARAVEL = 16;
    public const int UNIT_GALLEON = 17;
    public const int UNIT_DRAGON = 18;
    public const int UNIT_CHINESE_DRAGON = 19;
    public const int UNIT_PERSIAN_HEALER = 20;
    public const int UNIT_VIKING_GOD = 21;
    public const int UNIT_PHOENIX = 22;
    public const int UNIT_KUKULCAN = 23;
    public const int UNIT_JAPANESE_GOD = 24;
    public const int UNIT_SKELETON = 25;
    public const int UNIT_LYW = 26;
    public const int UNIT_HOBGLOBLIN = 27;
    public const int UNIT_GIANT_ETTIN = 28;
    public const int UNIT_GITH = 29;
    public const int UNIT_ROCKMAN = 30;
    public const int UNIT_GREMJERM = 31;
    public const int UNIT_FIREKIN = 32;
    public const int UNIT_GNOLL = 33;
    public const int UNIT_GOBLIN = 34;
    public const int UNIT_LIZARDMAN = 35;
    public const int UNIT_MAN = 36;
    public const int UNIT_HEADLESS = 37;
    public const int UNIT_EGYPTIAN = 38;
    public const int UNIT_INDIAN = 39;
    public const int UNIT_ZULU = 40;
    public const int UNIT_EGYPTIAN_GOD = 41;
    public const int UNIT_INDIAN_GOD = 42;
    public const int UNIT_ZULU_GOD = 43;
    public const int UNIT_F_BALLISTA = 44;
    public const int UNIT_LAST = 45;
        
    public const int MAX_UNIT_TYPE = UNIT_LAST - 1;
    public const int MAX_WEAPON_TYPE = 6;
    public const int MAX_SHIP_TYPE = 4;

    public const char UNIT_CLASS_HUMAN = 'H';
    public const char UNIT_CLASS_CARAVAN = 'C';
    public const char UNIT_CLASS_WEAPON = 'W';
    public const char UNIT_CLASS_SHIP = 'S';
    public const char UNIT_CLASS_MONSTER = 'M';
    public const char UNIT_CLASS_GOD = 'G';
    
    public const char UNIT_NONE = '\0';
    public const char UNIT_AIR = 'A';
    public const char UNIT_LAND = 'L';
    public const char UNIT_SEA = 'S';

    public const int UNIT_MODE_OVERSEE = 1; // unit_mode_para is the recno of the firm the unit is overseeing
    public const int UNIT_MODE_DEFEND_TOWN = 2; // unit_mode_para is the recno of the town the unit is defending
    public const int UNIT_MODE_CONSTRUCT = 3; // unit_mode_para is the recno of the firm the unit is constructing
    public const int UNIT_MODE_REBEL = 4; // unit_mode_para is the recno of the rebel group the unit belongs to
    // unit_mode_para is the recno of the firm recno of the monster firm it belongs to
    public const int UNIT_MODE_MONSTER = 5;
    public const int UNIT_MODE_ON_SHIP = 6; // unit_mode_para is the recno of the ship unit this unit is on
    // for ships only, unit_mode_para is the recno of the harbor this marine unit is in
    public const int UNIT_MODE_IN_HARBOR = 7;
    public const int UNIT_MODE_UNDER_TRAINING = 8;
    
    public const int ACTION_STOP = 0;
    public const int ACTION_ATTACK_UNIT = 1;
    public const int ACTION_ATTACK_FIRM = 2;
    public const int ACTION_ATTACK_TOWN = 3;
    public const int ACTION_ATTACK_WALL = 4;
    public const int ACTION_ASSIGN_TO_FIRM = 5;
    public const int ACTION_ASSIGN_TO_TOWN = 6;
    public const int ACTION_ASSIGN_TO_VEHICLE = 7;
    public const int ACTION_ASSIGN_TO_SHIP = 8;
    public const int ACTION_SHIP_TO_BEACH = 9; // used for UNIT_SEA only
    public const int ACTION_BUILD_FIRM = 10;
    public const int ACTION_SETTLE = 11;
    public const int ACTION_BURN = 12;
    public const int ACTION_DIE = 13;
    public const int ACTION_MOVE = 14;
    public const int ACTION_GO_CAST_POWER = 15; // for god only

    //------------ used only for action_mode2 -----------------//
    //------- put the following nine parameters together -------//

    public const int ACTION_AUTO_DEFENSE_ATTACK_TARGET = 16; // move to target for attacking
    // is idle, detect target to attack or waiting for defense action, (detect range is larger as usual)
    public const int ACTION_AUTO_DEFENSE_DETECT_TARGET = 17;
    // go back to camp for training or ready for next defense action
    public const int ACTION_AUTO_DEFENSE_BACK_CAMP = 18;

    public const int ACTION_DEFEND_TOWN_ATTACK_TARGET = 19;
    public const int ACTION_DEFEND_TOWN_DETECT_TARGET = 20;
    public const int ACTION_DEFEND_TOWN_BACK_TOWN = 21;
    public const int ACTION_MONSTER_DEFEND_ATTACK_TARGET = 22;
    public const int ACTION_MONSTER_DEFEND_DETECT_TARGET = 23;
    public const int ACTION_MONSTER_DEFEND_BACK_FIRM = 24;

    public const int ACTION_MISC_STOP = 0;
    public const int ACTION_MISC_CAPTURE_TOWN_RECNO = 1;
    public const int ACTION_MISC_DEFENSE_CAMP_RECNO = 2;
    public const int ACTION_MISC_DEFEND_TOWN_RECNO = 3;
    public const int ACTION_MISC_MONSTER_DEFEND_FIRM_RECNO = 4;

    public const int MONSTER_ACTION_STOP = 0;
    public const int MONSTER_ACTION_ATTACK = 1;
    public const int MONSTER_ACTION_DEFENSE = 2;
    public const int MONSTER_ACTION_EXPAND = 3;

    public const int KEEP_PRESERVE_ACTION = 1;  // used for stop2() to keep preserve action
    public const int KEEP_DEFENSE_MODE = 2;     // used for stop2() to keep the defense mode
    public const int KEEP_DEFEND_TOWN_MODE = 3; // used for stop2() to keep the defend town mode

    public const int MAX_WAITING_TERM_SAME = 3; // wait for same nation, used in handle_blocked...()
    public const int MAX_WAITING_TERM_DIFF = 3; // wait for diff. nation, used in handle_blocked...()

    public const int ATTACK_DETECT_DISTANCE = 6;// the distance for the unit to detect target while idle
    public const int ATTACK_SEARCH_TRIES = 250; // the node no. used to process searching when target is close to this unit
    public const int ATTACK_WAITING_TERM = 10;  // terms no. to wait before calling searching to attack target

    //MAX_SEARCH_OR_STOP_WAIT_TERM = 15; // note: should be the largest default value in waiting_term

    public const int AUTO_DEFENSE_STAY_OUTSIDE_COUNT = 4; //4 days
    public const int AUTO_DEFENSE_DETECT_COUNT = 3 + InternalConstants.FRAMES_PER_DAY * AUTO_DEFENSE_STAY_OUTSIDE_COUNT;
    public const int EFFECTIVE_AUTO_DEFENSE_DISTANCE = 9;

    public const int UNIT_DEFEND_TOWN_DISTANCE = 8;
    public const int UNIT_DEFEND_TOWN_STAY_OUTSIDE_COUNT = 4; // 4 days
    public const int UNIT_DEFEND_TOWN_DETECT_COUNT = 3 + InternalConstants.FRAMES_PER_DAY * UNIT_DEFEND_TOWN_STAY_OUTSIDE_COUNT;
    public const int UNIT_DEFEND_TOWN_WAITING_TERM = 4;
    public const int EFFECTIVE_DEFEND_TOWN_DISTANCE = 9;

    public const int MONSTER_DEFEND_FIRM_DISTANCE = 8;
    public const int MONSTER_DEFEND_STAY_OUTSIDE_COUNT = 4; // 4 days
    public const int MONSTER_DEFEND_DETECT_COUNT = 3 + InternalConstants.FRAMES_PER_DAY * MONSTER_DEFEND_STAY_OUTSIDE_COUNT;
    public const int EFFECTIVE_MONSTER_DEFEND_FIRM_DISTANCE = 9;

    public const int BUILDING_TYPE_FIRM_MOVE_TO = 0;
    public const int BUILDING_TYPE_FIRM_BUILD = 1;
    public const int BUILDING_TYPE_TOWN_MOVE_TO = 2;
    public const int BUILDING_TYPE_SETTLE = 3;
    public const int BUILDING_TYPE_VEHICLE = 4;
    public const int BUILDING_TYPE_WALL = 5;

    public const int HELP_NOTHING = 0;
    public const int HELP_ATTACK_UNIT = 1;
    public const int HELP_ATTACK_FIRM = 2;
    public const int HELP_ATTACK_TOWN = 3;
    public const int HELP_ATTACK_WALL = 4;

    public const int DO_CAST_POWER_RANGE = 3; // for god to cast power
    public const int MAX_UNIT_IN_SHIP = 9;
    public const int ATTACK_DIR = 8;
}