namespace TenKingdoms;

public class BulletFlame : Bullet
{
    public BulletFlame()
    {
    }

    public override void Init(int parentType, int parentId, int targetLocX, int targetLocY, int targetMobileType)
    {
        // note : BulletFlame should have at least one dummy moving frame for each direction
        base.Init(parentType, parentId, targetLocX, targetLocY, targetMobileType);

        CurAction = SPRITE_IDLE;
    }

    public override void ProcessIdle()
    {
        //base.ProcessIdle();
        if (++CurFrame <= CurSpriteStop().FrameCount)
        {
            // ----- warn/attack target every frame -------//
            WarnTarget();
            CheckHit();
        }
        else
        {
            CurAction = SPRITE_DIE;
            CurFrame = 1;
        }
    }

    public override int DisplayLayer()
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