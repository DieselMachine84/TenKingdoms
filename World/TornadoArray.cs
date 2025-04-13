using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class TornadoArray : SpriteArray
{
    protected override Tornado CreateNewObject(int objectType)
    {
        return new Tornado();
    }

    public Tornado AddTornado(int xLoc, int yLoc, int lifeTime)
    {
        Tornado tornado = (Tornado)CreateNew();
        tornado.init(xLoc, yLoc, lifeTime);
        return tornado;
    }

    public void DeleteTornado(Tornado tornado)
    {
        Delete(tornado.SpriteId);
    }

    public override void Process()
    {
        List<Tornado> tornadoesToDelete = new List<Tornado>();

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
                        tornadoesToDelete.Add(tornado);
                    break;
            }
        }

        foreach (Tornado tornado in tornadoesToDelete)
        {
            DeleteTornado(tornado);
        }
    }
}