namespace TenKingdoms;

public class BulletFlame : Bullet
{
    public BulletFlame()
    {
    }

    public override void init(int parentType, int parentRecno, int targetXLoc, int targetYLoc, int targetMobileType)
    {
        // note : BulletFlame should have at least one dummy moving frame for each direction
        base.init(parentType, parentRecno, targetXLoc, targetYLoc, targetMobileType);

        CurAction = SPRITE_IDLE;
    }

    public override void ProcessIdle()
    {
        // Sprite::process_idle();
        if (++CurFrame <= CurSpriteStop().FrameCount)
        {
            // ----- warn/ attack target every frame -------//
            warn_target();
            check_hit();
        }
        else
        {
            CurAction = SPRITE_DIE;
            CurFrame = 1;
        }
    }

    public override int display_layer()
    {
        switch (MobileType)
        {
            case UnitConstants.UNIT_LAND:
            case UnitConstants.UNIT_SEA:
                return 2;
            case UnitConstants.UNIT_AIR:
                return 8;
            default:
                return 1;
        }
    }
}