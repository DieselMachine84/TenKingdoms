namespace TenKingdoms;

public class WarPoint
{
    private const int WARPOINT_STRENGTH = 0x100000;
    private const int WARPOINT_STRENGTH_MAX = 0x1000000;

    public int Strength { get; private set; }

    public void Inc()
    {
        Strength += WARPOINT_STRENGTH;
        if (Strength > WARPOINT_STRENGTH_MAX)
            Strength = WARPOINT_STRENGTH_MAX;
    }

    public void Decay()
    {
        Strength >>= 1;
    }
}