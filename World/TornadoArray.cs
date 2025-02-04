using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class TornadoArray : SpriteArray
{
    protected override Tornado CreateNewObject(int objectId)
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
        Delete(tornado.sprite_recno);
    }

    public override void Process()
    {
        List<Tornado> tornadoesToDelete = new List<Tornado>();

        foreach (Tornado tornado in this)
        {
            tornado.pre_process();

            switch (tornado.cur_action)
            {
                case Sprite.SPRITE_IDLE:
                case Sprite.SPRITE_READY_TO_MOVE:
                    tornado.process_idle();
                    break;

                case Sprite.SPRITE_MOVE:
                    tornado.process_move();
                    break;

                case Sprite.SPRITE_TURN:
                    break;

                case Sprite.SPRITE_WAIT:
                    tornado.process_wait();
                    break;

                case Sprite.SPRITE_ATTACK:
                    tornado.process_attack();
                    break;

                case Sprite.SPRITE_DIE:
                    if (tornado.process_die())
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