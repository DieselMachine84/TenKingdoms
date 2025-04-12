using System;

namespace TenKingdoms;

public abstract class SpriteArray : DynArray<Sprite>
{
    public Sprite AddSprite(int objectId)
    {
	    return CreateNew(objectId);
    }

    public void DeleteSprite(Sprite sprite)
    {
	    int spriteRecno = sprite.SpriteId;
	    sprite.Deinit();
	    Delete(spriteRecno);
    }

    public virtual void die(int spriteRecno)
    {
	    DeleteSprite(this[spriteRecno]);
    }

    public virtual void Process()
    {
	    foreach (Sprite sprite in this)
	    {
		    if (sprite.RemainAttackDelay > 0)
			    sprite.RemainAttackDelay--;

		    if (sprite.CurX == -1) // cur_x == -1 if the unit has removed from the map and gone into a firm
			    continue;

		    int spriteRecno = sprite.SpriteId;

		    sprite.pre_process(); // it's actually calling Unit::pre_process() and other derived Unit classes

		    //-----------------------------------------------------//
		    // note: for unit cur_x == -1, the unit is invisible and
		    //			no pre_process is done.
		    //
		    //			for unit cur_x == -2, eg caravan, the unit is
		    //			invisible but pre_process is still processed.
		    //			However, sprite cur_action should be skipped.
		    //-----------------------------------------------------//

		    if (IsDeleted(spriteRecno)) // in case pre_process() kills the current Sprite
			    continue;

		    if (sprite.CurX < 0) //if( spritePtr->cur_x == -1 || spritePtr->cur_x==-2)
			    continue;

		    switch (sprite.CurAction)
		    {
			    case Sprite.SPRITE_IDLE:
				    sprite.process_idle();
				    break;

			    case Sprite.SPRITE_READY_TO_MOVE:
				    // TODO remove?
				    //sprite.CurAction = Sprite.SPRITE_IDLE; // to avoid problems of insensitive of mouse cursor
				    sprite.process_idle();
				    break;

			    case Sprite.SPRITE_MOVE:
				    sprite.process_move();
				    break;

			    case Sprite.SPRITE_WAIT:
				    sprite.process_wait();
				    break;

			    case Sprite.SPRITE_ATTACK:
				    sprite.process_attack();
				    break;

			    case Sprite.SPRITE_TURN:
				    sprite.process_turn();
				    break;

			    case Sprite.SPRITE_SHIP_EXTRA_MOVE: // for ship only
				    sprite.process_extra_move();
				    break;

			    case Sprite.SPRITE_DIE:
				    if (sprite.process_die())
				    {
					    die(spriteRecno);
				    }

				    break;
		    }

		    //----- can use other reasonable value to replace MIN_BACKGROUND_NODE_USED_UP ----//
		    if (!IsDeleted(spriteRecno))
		    {
			    if (sprite.GuardCount > 0)
			    {
				    if (++sprite.GuardCount > InternalConstants.GUARD_COUNT_MAX)
					    sprite.GuardCount = 0;
			    }
		    }
	    }
    }
}