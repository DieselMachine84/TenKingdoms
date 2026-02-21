using System.IO;

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
    
    #region SaveAndLoad

    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(NextId);
        int count = Count();
        writer.Write(count);
        foreach (Effect effect in EnumerateWithDeleted())
        {
            effect.SaveTo(writer);
        }
    }

    public void LoadFrom(BinaryReader reader)
    {
        NextId = reader.ReadInt32();
        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            Effect effect = CreateNewObject(0);
            effect.LoadFrom(reader);
            Load(effect);
        }
    }
	
    #endregion
}