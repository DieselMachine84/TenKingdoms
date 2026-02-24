using System.IO;

namespace TenKingdoms;

public class Projectile : Bullet
{
    public Projectile Bullet { get; }
    public Projectile Shadow { get; }

    public Projectile()
    {
        Bullet = new Projectile(0);
        Shadow = new Projectile(0);
    }

    private Projectile(int dummy)
    {
    }

    public override void Init(int parentType, int bulletType, int parentId, int targetLocX, int targetLocY, int targetMobileType)
    {
        base.Init(parentType, bulletType, parentId, targetLocX, targetLocY, targetMobileType);
        int spriteId = SpriteInfo.GetSubSpriteInfo(1).SpriteId;
        Bullet.Init(spriteId, CurLocX, CurLocY);
        int shadowSpriteId = SpriteInfo.GetSubSpriteInfo(2).SpriteId;
        Shadow.Init(shadowSpriteId, CurLocX, CurLocY);
    }

    public override int DisplayLayer()
    {
        if (MobileType == UnitConstants.UNIT_AIR || TargetMobileType == UnitConstants.UNIT_AIR)
            return 8;
        else
            return 2;
    }

    public void UpdateSprites(int finalDir, double z)
    {
        Shadow.SetDir(finalDir);
        Shadow.CurFrame = CurFrame;
        Shadow.CurAction = Sprite.SPRITE_MOVE;
        Shadow.SetCur((int)(CurX - z / 8.0), (int)(CurY - z / 6.0));

        Bullet.SetDir(finalDir);
        Bullet.CurFrame = CurFrame;
        Bullet.CurAction = Sprite.SPRITE_MOVE;
        Bullet.SetCur(CurX, CurY - (int)z);
    }
    
    #region SaveAndLoad

    public override void SaveTo(BinaryWriter writer)
    {
        base.SaveTo(writer);
        if (Bullet != null)
            Bullet.SaveTo(writer);
        if (Shadow != null)
            Shadow.SaveTo(writer);
    }

    public override void LoadFrom(BinaryReader reader)
    {
        base.LoadFrom(reader);
        if (Bullet != null)
            Bullet.LoadFrom(reader);
        if (Shadow != null)
            Shadow.LoadFrom(reader);
    }
	
    #endregion
}