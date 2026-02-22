using System.IO;

namespace TenKingdoms;

public class TornadoArray : SpriteArray
{
    public override Tornado this[int recNo] => (Tornado)base[recNo];

    protected override Tornado CreateNewObject(int objectType)
    {
        return new Tornado();
    }

    public Tornado AddTornado(int locX, int locY, int lifeTime)
    {
        Tornado tornado = (Tornado)CreateNew();
        tornado.Init(locX, locY, lifeTime);
        return tornado;
    }

    public override bool IsDeleted(int id)
    {
        if (base.IsDeleted(id))
            return true;

        Tornado tornado = this[id];
        return tornado.CurAction == Sprite.SPRITE_DIE;
    }
    
    public override void Process()
    {
        foreach (Tornado tornado in this)
        {
            tornado.PreProcess();

            switch (tornado.CurAction)
            {
                case Sprite.SPRITE_IDLE:
                case Sprite.SPRITE_READY_TO_MOVE:
                    tornado.ProcessIdle();
                    break;

                case Sprite.SPRITE_MOVE:
                    tornado.ProcessMove();
                    break;

                case Sprite.SPRITE_TURN:
                    break;

                case Sprite.SPRITE_WAIT:
                    tornado.ProcessWait();
                    break;

                case Sprite.SPRITE_ATTACK:
                    tornado.ProcessAttack();
                    break;

                case Sprite.SPRITE_DIE:
                    if (tornado.ProcessDie())
                        Delete(tornado.SpriteId);
                    break;
            }
        }
    }
    
    #region SaveAndLoad

    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(NextId);
        int count = Count();
        writer.Write(count);
        foreach (Tornado tornado in EnumerateWithDeleted())
        {
            tornado.SaveTo(writer);
        }
    }

    public void LoadFrom(BinaryReader reader)
    {
        NextId = reader.ReadInt32();
        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            Tornado tornado = CreateNewObject(0);
            tornado.LoadFrom(reader);
            Load(tornado);
        }
    }
	
    #endregion
}