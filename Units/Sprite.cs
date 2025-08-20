using System;

namespace TenKingdoms;

public class Sprite : IIdObject
{
	public const int SPRITE_IDLE = 1;
	public const int SPRITE_READY_TO_MOVE = 2;
	public const int SPRITE_MOVE = 3;
	public const int SPRITE_WAIT = 4;
	public const int SPRITE_ATTACK = 5;
	public const int SPRITE_TURN = 6;
	public const int SPRITE_SHIP_EXTRA_MOVE = 7;
	public const int SPRITE_DIE = 8;

	public int SpriteId { get; private set; }
	public int SpriteResId { get; protected set; } // sprite id. in SpriteRes
	public int MobileType { get; protected set; }

	public int CurAction { get; protected set; }
	public int CurDir { get; protected set; } // current direction
	public int FinalDir { get; protected set; } // for turning dir before attacking or moving
	protected int CurFrame { get; set; } // current frame
	public int CurAttack { get; protected set; } // current attack mode
	protected int TurnDelay { get; set; } // value between -60 and 60

	// shield guarding, affecting move/stop frame
	// 0 = not guarding, count up from 1 when guarding, reset to 0 after guard
	public int GuardCount { get; set; }

	public int RemainAttackDelay { get; set; } // no. of frames has to be delayed before the next attack motion
	protected int RemainFramesPerStep { get; set; } // no. of frames remained in this step

	
	//------------------------------------------------//
	//
	// Prime rule:
	//
	// World.GetLoc(NextLocX and NextLocY).cargo_recno is always = SpriteId no matter what CurAction is.
	//
	//------------------------------------------------//
	//
	// Relationship between (NextX, NextY) and (CurX, CurY)
	//
	// when SPRITE_WAIT, SPRITE_IDLE, SPRITE_READY_TO_MOVE, SPRITE_ATTACK, SPRITE_DIE:
	//
	// (NextX, NextY) == (CurX, CurY), it's the location of the sprite.
	//
	// when SPRITE_MOVE:
	//
	// (NextX, NextY) != (CurX, CurY)
	// (NextX, NextY) is where the sprite is moving towards.
	// (CurX , CurY) is the location of the sprite.
	//
	//------------------------------------------------//

	public int CurX { get; protected set; } // current location
	public int CurY { get; protected set; }
	public int NextX { get; protected set; } // next tile in the moving path
	public int NextY { get; protected set; }
	public int GoX { get; protected set; } // the destination of the path
	public int GoY { get; protected set; }

	public int CurLocX => CurX >> InternalConstants.CellWidthShift;
	public int CurLocY => CurY >> InternalConstants.CellHeightShift;
	public int NextLocX => NextX >> InternalConstants.CellWidthShift;
	public int NextLocY => NextY >> InternalConstants.CellHeightShift;
	public int GoLocX => GoX >> InternalConstants.CellWidthShift;
	public int GoLocY => GoY >> InternalConstants.CellHeightShift;

	public SpriteInfo SpriteInfo { get; protected set; }

	protected static readonly int[] MoveXPixels =
	{
		0, InternalConstants.CellWidth, InternalConstants.CellWidth, InternalConstants.CellWidth,
		0, -InternalConstants.CellWidth, -InternalConstants.CellWidth, -InternalConstants.CellWidth
	};

	protected static readonly int[] MoveYPixels =
	{
		-InternalConstants.CellHeight, -InternalConstants.CellHeight, 0, InternalConstants.CellHeight,
		InternalConstants.CellHeight, InternalConstants.CellHeight, 0, -InternalConstants.CellHeight
	};

	private static readonly int[] MoveXVectors = { 0, 1, 1, 1, 0, -1, -1, -1 };
	private static readonly int[] MoveYVectors = { -1, -1, 0, 1, 1, 1, 0, -1 };
	private static readonly int[] TurnAmount = { 60, 30, 20, 15, 12, 10, 9, 8, 7, 6 };
	
	protected Config Config => Sys.Instance.Config;
	protected World World => Sys.Instance.World;
	protected SpriteRes SpriteRes => Sys.Instance.SpriteRes;
	private SpriteFrameRes SpriteFrameRes => Sys.Instance.SpriteFrameRes;
	protected SERes SERes => Sys.Instance.SERes;
	protected NationArray NationArray => Sys.Instance.NationArray;
	protected TownArray TownArray => Sys.Instance.TownArray;
	protected FirmArray FirmArray => Sys.Instance.FirmArray;
	protected UnitArray UnitArray => Sys.Instance.UnitArray;

	public Sprite()
	{
	}

	void IIdObject.SetId(int id)
	{
		SpriteId = id;
	}

	public void Init(int spriteResId, int locX, int locY)
	{
		SpriteResId = spriteResId;

		CurX = locX * InternalConstants.CellWidth;
		CurY = locY * InternalConstants.CellHeight;

		GoX = NextX = CurX;
		GoY = NextY = CurY;

		CurAttack = 0;

		CurAction = SPRITE_IDLE;
		CurDir = Misc.Random(InternalConstants.MAX_SPRITE_DIR_TYPE); // facing any of the eight directions
		FinalDir = CurDir;
		CurFrame = 1;

		SpriteInfo = SpriteRes[SpriteResId];

		RemainAttackDelay = 0;
		RemainFramesPerStep = SpriteInfo.FramesPerStep;
	}

	public virtual void Deinit()
	{
	}

	//----- clone vars from sprite_res for fast access -----//

	protected SpriteMove CurSpriteMove()
	{
		return SpriteInfo.Moves[CurDir];
	}

	protected SpriteAttack CurSpriteAttack()
	{
		return SpriteInfo.Attacks[CurAttack, CurDir];
	}

	protected SpriteStop CurSpriteStop()
	{
		return SpriteInfo.Stops[CurDir];
	}

	protected SpriteDie CurSpriteDie()
	{
		return SpriteInfo.Die;
	}

	private bool NeedMirror(int displayDir)
	{
		return (displayDir < 8 || SpriteInfo.TurnResolution <= 8) ? (displayDir & 7) >= 5 : (displayDir & 7) >= 4;
	}
	
	public SpriteFrame CurSpriteFrame(out bool needMirror)
	{
		int curDir = DisplayDir();
		needMirror = NeedMirror(curDir);

		switch (CurAction)
		{
			case SPRITE_MOVE:
			case SPRITE_SHIP_EXTRA_MOVE:
				if (GuardCount != 0)
				{
					if (curDir >= InternalConstants.MAX_SPRITE_DIR_TYPE)
						curDir %= InternalConstants.MAX_SPRITE_DIR_TYPE;

					return SpriteFrameRes[SpriteInfo.GuardMoves[curDir].FirstFrameId + CurFrame - 1];
				}
				else
				{
					return SpriteFrameRes[SpriteInfo.Moves[curDir].FirstFrameId + CurFrame - 1];
				}

			case SPRITE_ATTACK:
				if (GuardCount != 0)
				{
					SpriteGuardStop guardStopAction = SpriteInfo.GuardStops[curDir];
					return SpriteFrameRes[guardStopAction.FirstFrameId + Math.Min(GuardCount, guardStopAction.FrameCount) - 1];
				}
				else
				{
					return SpriteFrameRes[SpriteInfo.Attacks[CurAttack, curDir].FirstFrameId + CurFrame - 1];
				}

			case SPRITE_TURN:
			case SPRITE_IDLE:
			case SPRITE_WAIT:
			{
				// air unit needs its own stop frames to float on air
				if (GuardCount != 0)
				{
					if (curDir >= InternalConstants.MAX_SPRITE_DIR_TYPE)
					{
						// if the sprite is turning, adjust direction to next
						if (TurnDelay > 0)
							curDir++;
						curDir %= InternalConstants.MAX_SPRITE_DIR_TYPE;
					}

					SpriteGuardStop guardStopAction = SpriteInfo.GuardStops[curDir];
					return SpriteFrameRes[guardStopAction.FirstFrameId + Math.Min(GuardCount, guardStopAction.FrameCount) - 1];
				}
				else
				{
					SpriteStop stopAction = SpriteInfo.Stops[curDir];
					if (CurFrame > stopAction.FrameCount)
						return SpriteFrameRes[stopAction.FrameId]; // first frame
					else // only few sprite has stopAction->frame_count > 1
						return SpriteFrameRes[stopAction.FrameId + CurFrame - 1];
				}
			}

			case SPRITE_DIE:
				if (SpriteInfo.Die.FirstFrameId != 0) // only if this sprite has dying frame
				{
					needMirror = false; // no need to mirror at any direction
					return SpriteFrameRes[SpriteInfo.Die.FirstFrameId + CurFrame - 1];
				}

				return null;

			default:
				return SpriteFrameRes[SpriteInfo.Moves[curDir].FirstFrameId + CurFrame - 1];
		}
	}

	protected void SpriteMove(int destX, int destY)
	{
		if (CurAction != SPRITE_MOVE)
		{
			CurAction = SPRITE_MOVE;
			CurFrame = 1;
		}

		GoX = destX;
		GoY = destY;

		//----------- determine the movement direction ---------//
		SetDir(CurX, CurY, GoX, GoY);

		//------ set the next tile to move towards -------//
		int stepCoeff = MoveStepCoeff();
		SetNext(CurX + stepCoeff * MoveXPixels[FinalDir], CurY + stepCoeff * MoveYPixels[FinalDir], -stepCoeff);
	}

	protected void SetCur(int curX, int curY)
	{
		CurX = curX;
		CurY = curY;
		UpdateAbsPos();
	}

	protected virtual void SetNext(int nextX, int nextY, int param = 0, int blockedChecked = 0)
	{
		NextX = nextX;
		NextY = nextY;
	}

	protected virtual void UpdateAbsPos(SpriteFrame spriteFrame = null)
	{
		spriteFrame ??= CurSpriteFrame(out _);

		//abs_x1 = cur_x + spriteFrame.offset_x;		// absolute position 
		//abs_y1 = cur_y + spriteFrame.offset_y;

		//abs_x2 = abs_x1 + spriteFrame.width  - 1;
		//abs_y2 = abs_y1 + spriteFrame.height - 1;
	}

	protected int MoveStepCoeff()
	{
		return MobileType == UnitConstants.UNIT_LAND ? 1 : 2;
	}

	public virtual void PreProcess()
	{
	}

	public virtual void ProcessIdle()
	{
		//-------- If it's an air unit --------//
		// note : most land units do have have stop frame, so cur_sprite_stop.frame_count is 0
		if (++CurFrame > CurSpriteStop().FrameCount)
			CurFrame = 1;
	}

	public virtual void ProcessMove()
	{
		//---- for some sprite (e.g. elephant), move one step per a few frames ----//

		if (--RemainFramesPerStep > 0)
			return;

		RemainFramesPerStep = SpriteInfo.FramesPerStep;

		//----- if the sprite has reach the destination ----//

		if (CurX == GoX && CurY == GoY)
		{
			CurAction = SPRITE_IDLE;
			SetNext(CurX, CurY); //********* BUGHERE

			CurFrame = 1;
			return;
		}

		//---- set the next tile the sprite will be moving towards ---//

		int stepX = SpriteInfo.Speed; //abs(vectorX);	//********* improve later
		int stepY = SpriteInfo.Speed; //abs(vectorY);

		// if next_x==go_x & next_y==go_y, reach destination already, don't move further.
		if (NextX != GoX || NextY != GoY)
		{
			if (Math.Abs(CurX - NextX) <= stepX && Math.Abs(CurY - NextY) <= stepY)
			{
				int stepCoeff = MoveStepCoeff();
				SetNext(NextX + stepCoeff * MoveXPixels[FinalDir], NextY + stepCoeff * MoveYPixels[FinalDir], -stepCoeff);
			}
		}

		//---- if blocked, cur_action is changed to SPRITE_WAIT, return now ----//

		if (CurAction != SPRITE_MOVE)
			return;

		//-------------- update position -----------------//
		//
		// If it gets very close to the destination, fit it to the destination ignoring the normal vector.
		//
		//------------------------------------------------//

		// cur_dir may be changed in the above set_next() call
		int vectorX = MoveXVectors[FinalDir] * SpriteInfo.Speed;
		int vectorY = MoveYVectors[FinalDir] * SpriteInfo.Speed;

		if (Math.Abs(CurX - GoX) <= stepX)
			CurX = GoX;
		else
			CurX += vectorX;

		if (Math.Abs(CurY - GoY) <= stepY)
			CurY = GoY;
		else
			CurY += vectorY;

		//------- update frame id. --------//

		if (++CurFrame > CurSpriteMove().FrameCount)
			CurFrame = 1;
	}

	public virtual void ProcessWait()
	{
	}

	public virtual int ProcessAttack()
	{
		if (RemainAttackDelay != 0 && CurFrame == 1)
			return 0;

		SERes.sound(CurLocX, CurLocY, CurFrame, 'S', SpriteResId, "A" + (CurAttack + 1));

		//------- next attack frame --------//
		SpriteAttack spriteAttack = CurSpriteAttack();
		if (++CurFrame > spriteAttack.FrameCount)
		{
			((Unit)this).CycleEqvAttack(); // assume only unit can attack
			CurFrame = 1;
			return 1;
		}

		return 0;
	}

	public virtual bool ProcessDie()
	{
		//--------- next frame ---------//

		if (Sys.Instance.FrameNumber % 3 == 0)
		{
			SERes.sound(CurLocX, CurLocY, CurFrame, 'S', SpriteResId, "DIE");
			if (++CurFrame > SpriteInfo.Die.FrameCount)
				return true;
		}

		return false;
	}

	public void ProcessTurn()
	{
		MatchDir();
	}

	public virtual void ProcessExtraMove()
	{
	}

	protected void SetDir(int curX, int curY, int destX, int destY)
	{
		SetDir(GetDir(curX, curY, destX, destY));
	}

	public void SetDir(int newDir) //	 overloading function
	{
		if (newDir != FinalDir)
		{
			FinalDir = newDir;
			TurnDelay = 0;
		}

		if (SpriteInfo.NeedTurning == 0)
			CurDir = FinalDir;
		else
			MatchDir();
	}

	protected int GetDir(int curX, int curY, int destX, int destY)
	{
		int xDiff = Math.Abs(destX - curX);
		int yDiff = Math.Abs(destY - curY);
		int squSize = Math.Max(xDiff, yDiff); // the size of the square we consider

		if (destX == curX)
		{
			return destY > curY ? InternalConstants.DIR_S : InternalConstants.DIR_N;
		}
		else if (destX < curX)
		{
			// west side
			if (destY > curY)
			{
				// south west quadrant
				if (2 * xDiff <= squSize)
					return InternalConstants.DIR_S;
				else if (2 * yDiff <= squSize)
					return InternalConstants.DIR_W;
				else
					return InternalConstants.DIR_SW;
			}
			else
			{
				// north west quadrant
				if (2 * xDiff <= squSize)
					return InternalConstants.DIR_N;
				else if (2 * yDiff <= squSize)
					return InternalConstants.DIR_W;
				else
					return InternalConstants.DIR_NW;
			}
		}
		else // destX > curX
		{
			// east side
			if (destY > curY)
			{
				// south east quadrant
				if (2 * xDiff <= squSize)
					return InternalConstants.DIR_S;
				else if (2 * yDiff <= squSize)
					return InternalConstants.DIR_E;
				else
					return InternalConstants.DIR_SE;
			}
			else
			{
				// north east quadrant
				if (2 * xDiff <= squSize)
					return InternalConstants.DIR_N;
				else if (2 * yDiff <= squSize)
					return InternalConstants.DIR_E;
				else
					return InternalConstants.DIR_NE;
			}
		}

		return InternalConstants.DIR_N;
	}

	protected bool IsDirCorrect()
	{
		return CurDir == FinalDir && TurnDelay == 0;
	}

	protected bool MatchDir()
	{
		if (SpriteInfo.NeedTurning == 0)
		{
			CurDir = FinalDir;
			return true;
		}

		if (CurDir == FinalDir) // same direction
		{
			TurnDelay = 0;
			return true;
		}

		const int HALF_SPRITE_DIR_TYPE = InternalConstants.MAX_SPRITE_DIR_TYPE / 2;
		const int TURN_REQUIRE_AMOUNT = 60;
		int turnAmount = TurnAmount[SpriteInfo.NeedTurning];

		if ((CurDir + HALF_SPRITE_DIR_TYPE) % InternalConstants.MAX_SPRITE_DIR_TYPE == FinalDir) // opposite direction
		{
			CurDir += (FinalDir % 2 != 0) ? 1 : -1;
			CurDir %= InternalConstants.MAX_SPRITE_DIR_TYPE;
		}
		else
		{
			int dirDiff = (FinalDir - CurDir + InternalConstants.MAX_SPRITE_DIR_TYPE) % InternalConstants.MAX_SPRITE_DIR_TYPE;
			if (dirDiff < HALF_SPRITE_DIR_TYPE)
			{
				TurnDelay += turnAmount;
				if (TurnDelay >= TURN_REQUIRE_AMOUNT)
				{
					CurDir = (CurDir + 1) % InternalConstants.MAX_SPRITE_DIR_TYPE;
					TurnDelay = 0;
				}
			}
			else
			{
				TurnDelay -= turnAmount;
				if (TurnDelay <= -TURN_REQUIRE_AMOUNT)
				{
					CurDir = (CurDir - 1 + InternalConstants.MAX_SPRITE_DIR_TYPE) % InternalConstants.MAX_SPRITE_DIR_TYPE;
					TurnDelay = 0;
				}
			}
		}

		return FinalDir == CurDir;
	}

	private int DisplayDir()
	{
		int curDir = CurDir;
		switch (SpriteInfo.TurnResolution)
		{
			case 0: // fall through
			case 1:
				curDir &= ~7; // direction less, remain upward or downard, but set to north
				break;
			case 8:
				// cur_dir can be 0 to 3*MAX_SPRITE_DIR_TYPE-1, such as projectile;
				// curDir = cur_dir;
				break;
			case 16:
				// curDir should be (from due north, clockwisely) { 0,8,1,9,2,10,3,11,4,12,5,13,6,14,7,15 }
				if (TurnDelay <= -30)
				{
					curDir = ((curDir + 7) & 7) + 8;
				}
				else if (TurnDelay >= 30)
				{
					curDir += 8;
				}

				break;
			case 24:
				// curDir should be (from due north, clockwisely) 
				// { 0,8,16,1,9,17,2,10,18,3,11,19,4,12,20,5,13,21,6,14,22,7,15,23 }
				if (TurnDelay <= -20)
				{
					if (TurnDelay <= -40)
						curDir = ((curDir + 7) & 7) + 8;
					else
						curDir = ((curDir + 7) & 7) + 16;
				}
				else if (TurnDelay >= 20)
				{
					if (TurnDelay >= 40)
						curDir += 16;
					else
						curDir += 8;
				}

				break;
		}

		return curDir;
	}

	public void SetGuardOn()
	{
		GuardCount = 1;
	}

	public void SetGuardOff()
	{
		GuardCount = 0;
	}

	public bool IsGuarding()
	{
		return GuardCount > 0;
	}

	public virtual bool IsStealth()
	{
		// if the visibility of location is just explored, consider stealth
		return Config.fog_of_war && World.GetLoc(CurLocX, CurLocY).Visibility() <= Location.EXPLORED_VISIBILITY;
	}
}