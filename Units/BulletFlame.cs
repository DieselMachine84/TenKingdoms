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

        cur_action = SPRITE_IDLE;
    }

    public override void process_idle()
    {
        // Sprite::process_idle();
        if (++cur_frame <= cur_sprite_stop().frame_count)
        {
            // ----- warn/ attack target every frame -------//
            warn_target();
            check_hit();
        }
        else
        {
            cur_action = SPRITE_DIE;
            cur_frame = 1;
        }
    }

    public override int display_layer()
    {
        switch (mobile_type)
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