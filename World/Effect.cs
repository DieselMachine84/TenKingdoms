namespace TenKingdoms;

public class Effect : Sprite
{
    public int layer;
    public int life;

    public Effect()
    {
        layer = 1; // default in land display layer
        life = 0; // disappear when life < 0
    }

    public void init(int spriteId, int startX, int startY, int initAction, int initDir, int dispLayer, int effectLife)
    {
        sprite_id = spriteId;

        cur_x = startX;
        cur_y = startY;

        go_x = next_x = cur_x;
        go_y = next_y = cur_y;

        cur_attack = 0;

        cur_action = initAction;
        cur_dir = initDir;
        cur_frame = 1;

        //----- clone vars from sprite_res for fast access -----//

        sprite_info = SpriteRes[sprite_id];

        //sprite_info.load_bitmap_res();

        // -------- adjust cur_dir -----------//
        if (sprite_info.turn_resolution <= 1)
            cur_dir = 0;
        final_dir = cur_dir;

        //------------- init other vars --------------//

        remain_attack_delay = 0;
        remain_frames_per_step = sprite_info.frames_per_step;

        layer = dispLayer;
        if (effectLife > 0)
        {
            life = effectLife;
        }
        else
        {
            switch (cur_action)
            {
                case SPRITE_IDLE:
                    life = sprite_info.stop_array[cur_dir].frame_count - cur_frame;
                    break;
                case SPRITE_DIE:
                    life = sprite_info.die.frame_count - cur_frame;
                    break;
            }
        }
    }

    public override void pre_process()
    {
        if (--life < 0)
        {
            Sys.Instance.EffectArray.DeleteEffect(this);
        }
    }

    public override void process_idle()
    {
        if (++cur_frame > cur_sprite_stop().frame_count)
            cur_frame = 1;
    }

    public override bool process_die()
    {
        if (++cur_frame > cur_sprite_die().frame_count)
            cur_frame = 1;
        return false;
    }
}