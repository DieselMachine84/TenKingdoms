namespace TenKingdoms;

public class WarPoint
{
    public const int WARPOINT_STRENGTH = 0x100000;
    public const int WARPOINT_STRENGTH_MAX = 0x1000000;

    public int strength;

    public void Inc()
    {
        strength += WARPOINT_STRENGTH;
        if (strength > WARPOINT_STRENGTH_MAX)
            strength = WARPOINT_STRENGTH_MAX;
    }

    public void Decay()
    {
        strength >>= 1;
    }
}