namespace TenKingdoms;

public class Skill
{
    public const int MAX_SKILL = 7;
    public const int MAX_TRAINABLE_SKILL = MAX_SKILL - 1; // exclude praying
    public const int SKILL_CONSTRUCTION = 1;
    public const int SKILL_LEADING = 2;
    public const int SKILL_MINING = 3;
    public const int SKILL_MFT = 4;
    public const int SKILL_RESEARCH = 5;
    public const int SKILL_SPYING = 6;
    public const int SKILL_PRAYING = 7;

    public int combat_level;
    public int skill_id;

    // if the unit is a town defender, this var is temporary used for storing the loyalty that will be added back to the town if the defender returns to the town
    public int skill_level;

    public int combat_level_minor; // when combat_level_mirror >= 100, combat_level + 1
    public int skill_level_minor;
    public int skill_potential; // skill potential

    public static string[] skill_str_array =
        { "Construction", "Leadership", "Mining", "Manufacture", "Research", "Spying", "Praying" };

    public static string[] skill_code_array = { "CONS", "LEAD", "MINE", "MANU", "RESE", "SPY", "PRAY" };

    // the id. of the race that specialized in this skill.
    public static int[] skilled_race_id_array = new int[MAX_SKILL];

    public static int[] skill_train_cost_array = new int[MAX_SKILL];

    public Skill()
    {
    }

    public string skill_des()
    {
        return skill_id == 0 ? string.Empty : skill_str_array[skill_id - 1];
    }

    public int get_skill(int skillId)
    {
        return skill_id == skillId ? skill_level : 0;
    }

    public void set_skill(int skillId)
    {
        skill_id = skillId;
    }
}