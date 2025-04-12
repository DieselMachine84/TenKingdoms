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
        SpriteResId = spriteId;

        CurX = startX;
        CurY = startY;

        GoX = NextX = CurX;
        GoY = NextY = CurY;

        CurAttack = 0;

        CurAction = initAction;
        CurDir = initDir;
        CurFrame = 1;

        //----- clone vars from sprite_res for fast access -----//

        SpriteInfo = SpriteRes[SpriteResId];

        //sprite_info.load_bitmap_res();

        // -------- adjust cur_dir -----------//
        if (SpriteInfo.turn_resolution <= 1)
            CurDir = 0;
        FinalDir = CurDir;

        //------------- init other vars --------------//

        RemainAttackDelay = 0;
        RemainFramesPerStep = SpriteInfo.frames_per_step;

        layer = dispLayer;
        if (effectLife > 0)
        {
            life = effectLife;
        }
        else
        {
            switch (CurAction)
            {
                case SPRITE_IDLE:
                    life = SpriteInfo.stop_array[CurDir].frame_count - CurFrame;
                    break;
                case SPRITE_DIE:
                    life = SpriteInfo.die.frame_count - CurFrame;
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
        if (++CurFrame > cur_sprite_stop().frame_count)
            CurFrame = 1;
    }

    public override bool process_die()
    {
        if (++CurFrame > cur_sprite_die().frame_count)
            CurFrame = 1;
        return false;
    }
}