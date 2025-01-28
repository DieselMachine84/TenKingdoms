namespace TenKingdoms;

public class Projectile : Bullet
{
    public double z_coff; // height = z_coff * (cur_step) * (total_step - cur_step)
    public Sprite act_bullet;
    public Sprite bullet_shadow;

    public Projectile()
    {
        act_bullet.sprite_recno = 0;
        bullet_shadow.sprite_recno = 0;
    }

    public override void init(int parentType, int parentRecno, int targetXLoc, int targetYLoc, int targetMobileType)
    {
        base.init(parentType, parentRecno, targetXLoc, targetYLoc, targetMobileType);
        int spriteId = sprite_info.get_sub_sprite_info(1).sprite_id;
        act_bullet.init(spriteId, cur_x_loc(), cur_y_loc());
        int shadowSpriteId = sprite_info.get_sub_sprite_info(2).sprite_id;
        bullet_shadow.init(shadowSpriteId, cur_x_loc(), cur_y_loc());

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

        // --------- recalcuate spriteFrame pointer ----------//
        SpriteFrame spriteFrame = cur_sprite_frame(out _);
    }

    public override int display_layer()
    {
        if (mobile_type == UnitConstants.UNIT_AIR || target_mobile_type == UnitConstants.UNIT_AIR)
            return 8;
        else
            return 2;
    }
}