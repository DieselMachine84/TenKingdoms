namespace TenKingdoms;

public class Projectile : Bullet
{
    public double z_coff; // height = z_coff * (cur_step) * (total_step - cur_step)
    public Sprite act_bullet = new Sprite();
    public Sprite bullet_shadow = new Sprite();

    public Projectile()
    {
    }

    public override void init(int parentType, int parentRecno, int targetXLoc, int targetYLoc, int targetMobileType)
    {
        base.init(parentType, parentRecno, targetXLoc, targetYLoc, targetMobileType);
        int spriteId = SpriteInfo.GetSubSpriteInfo(1).SpriteId;
        act_bullet.Init(spriteId, CurLocX, CurLocY);
        int shadowSpriteId = SpriteInfo.GetSubSpriteInfo(2).SpriteId;
        bullet_shadow.Init(shadowSpriteId, CurLocX, CurLocY);

        // calculate z_coff;
        z_coff = 1.0;
        /*
        float dz = z_coff * total_step;
        if( dz >= 10.0)
            cur_dir = cur_dir & 7 | 8;					// pointing up
        else if( dz <= -10.0)
            cur_dir = cur_dir & 7 | 16;				// pointing down
        else
            cur_dir &= 7;
        */

        // --------- recalculate spriteFrame pointer ----------//
        SpriteFrame spriteFrame = CurSpriteFrame(out _);
    }

    public override int display_layer()
    {
        if (MobileType == UnitConstants.UNIT_AIR || target_mobile_type == UnitConstants.UNIT_AIR)
            return 8;
        else
            return 2;
    }
}