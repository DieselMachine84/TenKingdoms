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
        if (SpriteInfo.TurnResolution <= 1)
            CurDir = 0;
        FinalDir = CurDir;

        //------------- init other vars --------------//

        RemainAttackDelay = 0;
        RemainFramesPerStep = SpriteInfo.FramesPerStep;

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
                    life = SpriteInfo.Stops[CurDir].FrameCount - CurFrame;
                    break;
                case SPRITE_DIE:
                    life = SpriteInfo.Die.FrameCount - CurFrame;
                    break;
            }
        }
    }

    public override void PreProcess()
    {
        if (--life < 0)
        {
            Sys.Instance.EffectArray.DeleteEffect(this);
        }
    }

    public override void ProcessIdle()
    {
        if (++CurFrame > CurSpriteStop().FrameCount)
            CurFrame = 1;
    }

    public override bool ProcessDie()
    {
        if (++CurFrame > CurSpriteDie().FrameCount)
            CurFrame = 1;
        return false;
    }
}