using System;
using System.Collections.Generic;

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
}