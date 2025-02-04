namespace TenKingdoms;

public class EffectArray : SpriteArray
{
    protected override Effect CreateNewObject(int objectId)
    {
        return new Effect();
    }

    public Effect AddEffect(int spriteId, int startX, int startY, int initAction, int initDir, int dispLayer, int effectLife)
    {
        Effect effect = (Effect)AddSprite(spriteId);
        effect.init(spriteId, startX, startY, initAction, initDir, dispLayer, effectLife);
        return effect;
    }
    
    public void DeleteEffect(Effect effect)
    {
        Delete(effect.sprite_recno);
    }
}