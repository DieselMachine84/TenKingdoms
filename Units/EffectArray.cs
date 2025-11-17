namespace TenKingdoms;

public class EffectArray : SpriteArray
{
    public override Effect this[int recNo] => (Effect)base[recNo];
    
    protected override Effect CreateNewObject(int objectType)
    {
        return new Effect();
    }

    public Effect AddEffect(int spriteId, int startX, int startY, int initAction, int initDir, int displayLayer, int effectLife)
    {
        Effect effect = (Effect)AddSprite(spriteId);
        effect.Init(spriteId, startX, startY, initAction, initDir, displayLayer, effectLife);
        return effect;
    }
    
    public void DeleteEffect(Effect effect)
    {
        Delete(effect.SpriteId);
    }
}