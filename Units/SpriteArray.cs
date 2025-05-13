using System;

namespace TenKingdoms;

public abstract class SpriteArray : DynArray<Sprite>
{
    protected Sprite AddSprite(int objectId)
    {
	    return CreateNew(objectId);
    }

    protected void DeleteSprite(Sprite sprite)
    {
	    // TODO do not change SpriteId
	    int spriteId = sprite.SpriteId;
	    sprite.Deinit();
	    Delete(spriteId);
    }

    protected virtual void Die(int spriteId)
    {
	    DeleteSprite(this[spriteId]);
    }

    public virtual void Process()
    {
	    foreach (Sprite sprite in this)
	    {
		    if (sprite.RemainAttackDelay > 0)
			    sprite.RemainAttackDelay--;

		    if (sprite.CurX == -1) // cur_x == -1 if the unit has removed from the map and gone into a firm
			    continue;

		    // TODO do not change SpriteId
		    int spriteId = sprite.SpriteId;

		    sprite.PreProcess(); // it's actually calling Unit.PreProcess() and other derived Unit classes

		    //-----------------------------------------------------//
		    // note: for unit cur_x == -1, the unit is invisible and no pre_process is done.
		    //
		    //       for unit cur_x == -2, eg caravan, the unit is invisible but pre_process is still processed.
		    //       However, sprite cur_action should be skipped.
		    //-----------------------------------------------------//

		    if (IsDeleted(spriteId)) // in case pre_process() kills the current Sprite
			    continue;

		    if (sprite.CurX < 0) //if( spritePtr->cur_x == -1 || spritePtr->cur_x==-2)
			    continue;

		    switch (sprite.CurAction)
		    {
			    case Sprite.SPRITE_IDLE:
				    sprite.ProcessIdle();
				    break;

			    case Sprite.SPRITE_READY_TO_MOVE:
				    // TODO remove?
				    //sprite.CurAction = Sprite.SPRITE_IDLE; // to avoid problems of insensitive of mouse cursor
				    sprite.ProcessIdle();
				    break;

			    case Sprite.SPRITE_MOVE:
				    sprite.ProcessMove();
				    break;

			    case Sprite.SPRITE_WAIT:
				    sprite.ProcessWait();
				    break;

			    case Sprite.SPRITE_ATTACK:
				    sprite.ProcessAttack();
				    break;

			    case Sprite.SPRITE_TURN:
				    sprite.ProcessTurn();
				    break;

			    case Sprite.SPRITE_SHIP_EXTRA_MOVE: // for ship only
				    sprite.ProcessExtraMove();
				    break;

			    case Sprite.SPRITE_DIE:
				    if (sprite.ProcessDie())
				    {
					    Die(spriteId);
				    }
				    break;
		    }

		    if (!IsDeleted(spriteId))
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