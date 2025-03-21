using System;

namespace TenKingdoms;

public class Sprite
{
	public const int SPRITE_IDLE = 1;
	public const int SPRITE_READY_TO_MOVE = 2;
	public const int SPRITE_MOVE = 3;
	public const int SPRITE_WAIT = 4;
	public const int SPRITE_ATTACK = 5;
	public const int SPRITE_TURN = 6;
	public const int SPRITE_SHIP_EXTRA_MOVE = 7;
	public const int SPRITE_DIE = 8;

	public int sprite_id; // sprite id. in SpriteRes
	public int sprite_recno;

	public int mobile_type;

	//public int cur_action; // current action
	private int _cur_action;
	public int cur_action
	{
		get
		{
			return _cur_action;
		}
		set
		{
			_cur_action = value;
		}
	}
	public int cur_dir; // current direction
	public int cur_frame; // current frame
	public int cur_attack; // current attack mode
	public int final_dir; // for turning dir before attacking or moving
	public int turn_delay; // value between -60 and 60

	// shield guarding, affecting move/stop frame
	// 0 = not guarding, count up from 1 when guarding, reset to 0 after guard
	public int guard_count;

	public int remain_attack_delay; // no. of frames has to be delayed before the next attack motion
	public int remain_frames_per_step; // no. of frames remained in this step

	public int cur_x, cur_y; // current location
	public int go_x, go_y; // the destination of the path
	public int next_x, next_y; // next tile in the moving path
	
	public SpriteInfo sprite_info;

	public static int[] move_x_pixel_array =
	{
		0, InternalConstants.CellWidth, InternalConstants.CellWidth, InternalConstants.CellWidth,
		0, -InternalConstants.CellWidth, -InternalConstants.CellWidth, -InternalConstants.CellWidth
	};

	public static int[] move_y_pixel_array =
	{
		-InternalConstants.CellHeight, -InternalConstants.CellHeight, 0, InternalConstants.CellHeight,
		InternalConstants.CellHeight, InternalConstants.CellHeight, 0, -InternalConstants.CellHeight
	};

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

	public void init(int spriteId, int startX, int startY)
	{
		sprite_id = spriteId;

		cur_x = startX * InternalConstants.CellWidth;
		cur_y = startY * InternalConstants.CellHeight;

		go_x = next_x = cur_x;
		go_y = next_y = cur_y;

		cur_attack = 0;

		cur_action = SPRITE_IDLE;
		cur_dir = Misc.Random(InternalConstants.MAX_SPRITE_DIR_TYPE); // facing any of the eight directions
		cur_frame = 1;
		final_dir = cur_dir;

		//----- clone vars from sprite_res for fast access -----//

		sprite_info = SpriteRes[sprite_id];

		//sprite_info.load_bitmap_res();

		//------------- init other vars --------------//

		remain_attack_delay = 0;
		remain_frames_per_step = sprite_info.frames_per_step;
	}

	public virtual void Deinit()
	{
	}

	//---------- function member vars -------------//
	public int cur_x_loc()
	{
		return cur_x >> InternalConstants.CellWidthShift;
	}

	public int cur_y_loc()
	{
		return cur_y >> InternalConstants.CellHeightShift;
	}

	public int next_x_loc()
	{
		return next_x >> InternalConstants.CellWidthShift;
	}

	public int next_y_loc()
	{
		return next_y >> InternalConstants.CellHeightShift;
	}

	public int go_x_loc()
	{
		return go_x >> InternalConstants.CellWidthShift;
	}

	public int go_y_loc()
	{
		return go_y >> InternalConstants.CellHeightShift;
	}

	//----- clone vars from sprite_res for fast access -----//

	public SpriteMove cur_sprite_move()
	{
		return sprite_info.move_array[cur_dir];
	}

	public SpriteAttack cur_sprite_attack()
	{
		return sprite_info.attack_array[cur_attack, cur_dir];
	}

	public SpriteStop cur_sprite_stop()
	{
		return sprite_info.stop_array[cur_dir];
	}

	public SpriteDie cur_sprite_die()
	{
		return sprite_info.die;
	}

	private bool NeedMirror(int dispDir)
	{
		return (dispDir < 8 || sprite_info.turn_resolution <= 8) ? (dispDir & 7) >= 5 : (dispDir & 7) >= 4;
	}
	
	public SpriteFrame cur_sprite_frame(out bool needMirror)
	{
		int curDir = display_dir();
		needMirror = NeedMirror(curDir);

		switch (cur_action)
		{
			case SPRITE_MOVE:
			case SPRITE_SHIP_EXTRA_MOVE:
				if (guard_count != 0)
				{
					if (curDir >= InternalConstants.MAX_SPRITE_DIR_TYPE)
						curDir %= InternalConstants.MAX_SPRITE_DIR_TYPE;

					return SpriteFrameRes[sprite_info.guard_move_array[curDir].first_frame_recno + cur_frame - 1];
				}
				else
				{
					return SpriteFrameRes[sprite_info.move_array[curDir].first_frame_recno + cur_frame - 1];
				}

			case SPRITE_ATTACK:
				if (guard_count != 0)
				{
					SpriteGuardStop guardStopAction = sprite_info.guard_stop_array[curDir];
					return SpriteFrameRes[guardStopAction.first_frame_recno + Math.Min(guard_count, guardStopAction.frame_count) - 1];
				}
				else
				{
					return SpriteFrameRes[sprite_info.attack_array[cur_attack, curDir].first_frame_recno + cur_frame - 1];
				}

			case SPRITE_TURN:
			case SPRITE_IDLE:
			case SPRITE_WAIT:
			{
				// air unit needs it own stop frames to float on air
				if (guard_count != 0)
				{
					if (curDir >= InternalConstants.MAX_SPRITE_DIR_TYPE)
					{
						// if the sprite is turning, adjust direction to next
						if (turn_delay > 0)
							curDir++;
						curDir %= InternalConstants.MAX_SPRITE_DIR_TYPE;
					}

					SpriteGuardStop guardStopAction = sprite_info.guard_stop_array[curDir];
					return SpriteFrameRes[guardStopAction.first_frame_recno + Math.Min(guard_count, guardStopAction.frame_count) - 1];
				}
				else
				{
					SpriteStop stopAction = sprite_info.stop_array[curDir];
					if (cur_frame > stopAction.frame_count)
						return SpriteFrameRes[stopAction.frame_recno]; // first frame
					else // only few sprite has stopAction->frame_count > 1
						return SpriteFrameRes[stopAction.frame_recno + cur_frame - 1];
				}
			}

			case SPRITE_DIE:
				if (sprite_info.die.first_frame_recno != 0) // only if this sprite has dying frame
				{
					needMirror = false; // no need to mirror at any direction
					return SpriteFrameRes[sprite_info.die.first_frame_recno + cur_frame - 1];
				}

				return null;

			default:
				return SpriteFrameRes[sprite_info.move_array[curDir].first_frame_recno + cur_frame - 1];
		}
	}

	public void sprite_move(int desX, int desY)
	{
		if (cur_action != SPRITE_MOVE)
		{
			cur_action = SPRITE_MOVE;
			cur_frame = 1;
		}

		go_x = desX;
		go_y = desY;

		//----------- determine the movement direction ---------//
		set_dir(cur_x, cur_y, go_x, go_y);

		//------ set the next tile to move towards -------//
		int stepMagn = move_step_magn();
		set_next(cur_x + stepMagn * move_x_pixel_array[final_dir], cur_y + stepMagn * move_y_pixel_array[final_dir], -stepMagn);
	}

	public void set_cur(int curX, int curY)
	{
		cur_x = curX;
		cur_y = curY;
		update_abs_pos();
	}

	public virtual void set_next(int nextX, int nextY, int para = 0, int blockedChecked = 0)
	{
		next_x = nextX;
		next_y = nextY;
	}

	protected virtual void update_abs_pos(SpriteFrame spriteFrame = null)
	{
		spriteFrame ??= cur_sprite_frame(out _);

		//abs_x1 = cur_x + spriteFrame.offset_x;		// absolute position 
		//abs_y1 = cur_y + spriteFrame.offset_y;

		//abs_x2 = abs_x1 + spriteFrame.width  - 1;
		//abs_y2 = abs_y1 + spriteFrame.height - 1;
	}

	public int move_step_magn()
	{
		return mobile_type == UnitConstants.UNIT_LAND ? 1 : 2;
	}

	public virtual void pre_process()
	{
	}

	public virtual void process_idle()
	{
		//-------- If it's an air unit --------//
		// note : most land units do have have stop frame,
		// so cur_sprite_stop.frame_count is 0
		if (++cur_frame > cur_sprite_stop().frame_count)
			cur_frame = 1;
	}

	public virtual void process_move()
	{
		//---- for some sprite (e.g. elephant), move one step per a few frames ----//

		if (--remain_frames_per_step > 0)
			return;
		else
			remain_frames_per_step = sprite_info.frames_per_step;

		//----- if the sprite has reach the destintion ----//

		if (cur_x == go_x && cur_y == go_y)
		{
			cur_action = SPRITE_IDLE;
			set_next(cur_x, cur_y); //********* BUGHERE

			cur_frame = 1;
			return;
		}

		//---- set the next tile the sprite will be moving towards ---//

		int[] vector_x_array = { 0, 1, 1, 1, 0, -1, -1, -1 }; // default vectors, temporary only
		int[] vector_y_array = { -1, -1, 0, 1, 1, 1, 0, -1 };

		int stepX = sprite_info.speed; //abs(vectorX);	//********* improve later
		int stepY = sprite_info.speed; //abs(vectorY);

		// if next_x==go_x & next_y==go_y, reach destination already, don't move further.
		if (next_x != go_x || next_y != go_y)
		{
			if (Math.Abs(cur_x - next_x) <= stepX && Math.Abs(cur_y - next_y) <= stepY)
			{
				int stepMagn = move_step_magn();
				set_next(next_x + stepMagn * move_x_pixel_array[final_dir],
					next_y + stepMagn * move_y_pixel_array[final_dir], -stepMagn);
			}
		}

		//---- if the is blocked, cur_action is changed to SPRITE_WAIT, return now ----//

		if (cur_action != SPRITE_MOVE)
			return;

		//-------------- update position -----------------//
		//
		// If it gets very close to the destination, fit it
		// to the destination ingoring the normal vector.
		//
		//------------------------------------------------//

		// cur_dir may be changed in the above set_next() call
		int vectorX = vector_x_array[final_dir] * sprite_info.speed;
		int vectorY = vector_y_array[final_dir] * sprite_info.speed;

		if (Math.Abs(cur_x - go_x) <= stepX)
			cur_x = go_x;
		else
			cur_x += vectorX;

		if (Math.Abs(cur_y - go_y) <= stepY)
			cur_y = go_y;
		else
			cur_y += vectorY;

		//------- update frame id. --------//

		if (++cur_frame > cur_sprite_move().frame_count)
			cur_frame = 1;
	}

	public virtual void process_wait()
	{
	}

	public virtual int process_attack()
	{
		if (remain_attack_delay != 0 && cur_frame == 1)
			return 0;

		//------- next attack frame --------//
		SpriteAttack spriteAttack = cur_sprite_attack();

		// ------ sound effect --------//
		string action = "A" + (cur_attack + 1);
		SERes.sound(cur_x_loc(), cur_y_loc(), cur_frame, 'S', sprite_id, action);

		if (++cur_frame > spriteAttack.frame_count)
		{
			((Unit)this).cycle_eqv_attack(); // assume only unit can attack
			cur_frame = 1;
			return 1;
		}

		return 0;
	}

	public virtual bool process_die()
	{
		//--------- next frame ---------//

		if (Sys.Instance.FrameNumber % 3 == 0)
		{
			SERes.sound(cur_x_loc(), cur_y_loc(), cur_frame, 'S', sprite_id, "DIE");
			if (++cur_frame > sprite_info.die.frame_count)
				return true;
		}

		return false;
	}

	public void process_turn()
	{
		match_dir();
	}

	public virtual void process_extra_move()
	{
	}

	public void set_dir(int curX, int curY, int destX, int destY)
	{
		int newDir = get_dir(curX, curY, destX, destY);
		if (newDir != final_dir)
		{
			final_dir = newDir;
			turn_delay = 0;
		}

		if (sprite_info.need_turning == 0)
			cur_dir = final_dir;
		else
			match_dir(); // start turning
	}

	public void set_dir(int newDir) //	 overloading function
	{
		if (newDir != final_dir)
		{
			final_dir = newDir;
			turn_delay = 0;
		}

		if (sprite_info.need_turning == 0)
			cur_dir = final_dir;
		else
			match_dir();
	}

	public int get_dir(int curX, int curY, int destX, int destY)
	{
		int xDiff = Math.Abs(destX - curX);
		int yDiff = Math.Abs(destY - curY);
		int squSize = Math.Max(xDiff, yDiff); // the size of the square we consider

		if (destX == curX)
		{
			if (destY > curY)
				return InternalConstants.DIR_S;
			else
				return InternalConstants.DIR_N;
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

	public bool is_dir_correct()
	{
		return cur_dir == final_dir && turn_delay == 0;
	}

	public bool match_dir()
	{
		if (sprite_info.need_turning == 0)
		{
			cur_dir = final_dir;
			return true;
		}

		int[] turn_amount = { 60, 30, 20, 15, 12, 10, 9, 8, 7, 6 };
		int HALF_SPRITE_DIR_TYPE = InternalConstants.MAX_SPRITE_DIR_TYPE / 2;
		int TURN_REQUIRE_AMOUNT = 60;

		if (cur_dir == final_dir) // same direction
		{
			turn_delay = 0;
			return true;
		}

		int turnAmount = turn_amount[sprite_info.need_turning];

		if ((cur_dir + HALF_SPRITE_DIR_TYPE) % InternalConstants.MAX_SPRITE_DIR_TYPE == final_dir) // opposite direction
		{
			cur_dir += (final_dir % 2 != 0) ? 1 : -1;
			cur_dir %= InternalConstants.MAX_SPRITE_DIR_TYPE;
		}
		else
		{
			int dirDiff = (final_dir - cur_dir + InternalConstants.MAX_SPRITE_DIR_TYPE) % InternalConstants.MAX_SPRITE_DIR_TYPE;
			if (dirDiff < HALF_SPRITE_DIR_TYPE)
			{
				turn_delay += turnAmount;
				if (turn_delay >= TURN_REQUIRE_AMOUNT)
				{
					cur_dir = (cur_dir + 1) % InternalConstants.MAX_SPRITE_DIR_TYPE;
					turn_delay = 0;
				}
			}
			else
			{
				turn_delay -= turnAmount;
				if (turn_delay <= -TURN_REQUIRE_AMOUNT)
				{
					cur_dir = (cur_dir - 1 + InternalConstants.MAX_SPRITE_DIR_TYPE) % InternalConstants.MAX_SPRITE_DIR_TYPE;
					turn_delay = 0;
				}
			}
		}

		return final_dir == cur_dir;
	}

	public void set_remain_attack_delay()
	{
		Unit unit = (Unit)this; //**BUGHERE, assuming all Sprite that call process_attack() are Unit
		remain_attack_delay = unit.attack_info_array[unit.cur_attack].attack_delay;
	}

	public int display_dir()
	{
		int curDir = cur_dir;
		switch (sprite_info.turn_resolution)
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
				if (turn_delay <= -30)
				{
					curDir = ((curDir + 7) & 7) + 8;
				}
				else if (turn_delay >= 30)
				{
					curDir += 8;
				}

				break;
			case 24:
				// curDir should be (from due north, clockwisely) 
				// { 0,8,16,1,9,17,2,10,18,3,11,19,4,12,20,5,13,21,6,14,22,7,15,23 }
				if (turn_delay <= -20)
				{
					if (turn_delay <= -40)
						curDir = ((curDir + 7) & 7) + 8;
					else
						curDir = ((curDir + 7) & 7) + 16;
				}
				else if (turn_delay >= 20)
				{
					if (turn_delay >= 40)
						curDir += 16;
					else
						curDir += 8;
				}

				break;
		}

		return curDir;
	}

	public void set_guard_on()
	{
		guard_count = 1;
	}

	public void set_guard_off()
	{
		guard_count = 0;
	}

	public bool is_guarding()
	{
		return guard_count > 0;
	}

	public virtual bool is_shealth()
	{
		// if the visibility of location is just explored, consider shealth
		return Config.fog_of_war && World.get_loc(cur_x_loc(), cur_y_loc()).Visibility() <= Location.EXPLORED_VISIBILITY;
	}
}