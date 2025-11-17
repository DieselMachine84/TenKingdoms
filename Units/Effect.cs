namespace TenKingdoms;

public class Effect : Sprite
{
    public int DisplayLayer { get; private set; }
    private int Life { get; set; }

    public Effect()
    {
        DisplayLayer = 1; // default in land display layer
        Life = 0; // disappear when life < 0
    }

    public void Init(int spriteId, int startX, int startY, int initAction, int initDir, int displayLayer, int effectLife)
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

        SpriteInfo = SpriteRes[SpriteResId];

        if (SpriteInfo.TurnResolution <= 1)
            CurDir = 0;
        FinalDir = CurDir;

        RemainAttackDelay = 0;
        RemainFramesPerStep = SpriteInfo.FramesPerStep;

        DisplayLayer = displayLayer;
        if (effectLife > 0)
        {
            Life = effectLife;
        }
        else
        {
            switch (CurAction)
            {
                case SPRITE_IDLE:
                    Life = SpriteInfo.Stops[CurDir].FrameCount - CurFrame;
                    break;
                case SPRITE_DIE:
                    Life = SpriteInfo.Die.FrameCount - CurFrame;
                    break;
            }
        }
    }

    public override void PreProcess()
    {
        if (--Life < 0)
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