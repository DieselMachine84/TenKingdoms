namespace TenKingdoms;

public class Skill
{
    private const int MAX_SKILL = 7;
    public const int MAX_TRAINABLE_SKILL = MAX_SKILL - 1; // exclude praying
    public const int SKILL_CONSTRUCTION = 1;
    public const int SKILL_LEADING = 2;
    public const int SKILL_MINING = 3;
    public const int SKILL_MFT = 4;
    public const int SKILL_RESEARCH = 5;
    public const int SKILL_SPYING = 6;
    public const int SKILL_PRAYING = 7;

    public int SkillId { get; set; }
    // if the unit is a town defender, this var is temporary used for storing the loyalty that will be added back to the town if the defender returns to the town
    public int SkillLevel { get; set; }
    public int SkillLevelMinor { get; set; }
    public int CombatLevel { get; set; }
    public int CombatLevelMinor { get; set; } // when combat_level_mirror >= 100, combat_level + 1
    public int SkillPotential { get; set; }

    public static string[] SkillDescriptions = { "Construction", "Leadership", "Mining", "Manufacture", "Research", "Spying", "Praying" };
    public static string[] SkillCodes = { "CONS", "LEAD", "MINE", "MANU", "RESE", "SPY", "PRAY" };

    public Skill()
    {
    }

    public string SkillDescription()
    {
        return SkillId == 0 ? string.Empty : SkillDescriptions[SkillId - 1];
    }

    public int GetSkillLevel(int skillId)
    {
        return SkillId == skillId ? SkillLevel : 0;
    }
}