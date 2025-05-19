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
	    sprite.Deinit();
	    Delete(sprite.SpriteId);
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

		    if (sprite.CurX == -1) // CurX == -1 if the unit has removed from the map and gone into a firm
			    continue;

		    sprite.PreProcess(); // it's actually calling Unit.PreProcess() and other derived Unit classes

		    //-----------------------------------------------------//
		    // note: for unit CurX == -1, the unit is invisible and no PreProcess is done.
		    //
		    //       for unit CurX == -2, eg caravan, the unit is invisible but PreProcess is still processed.
		    //       However, sprite CurAction should be skipped.
		    //-----------------------------------------------------//

		    if (IsDeleted(sprite.SpriteId)) // in case PreProcess() kills the current Sprite
			    continue;

		    if (sprite.CurX < 0) // if(sprite.CurX == -1 || sprite.CurX==-2)
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
					    Die(sprite.SpriteId);
				    }
				    break;
		    }

		    if (!IsDeleted(sprite.SpriteId))
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