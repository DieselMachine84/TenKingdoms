using System;

namespace TenKingdoms;

public class BulletHoming : Bullet
{
	public const int BULLET_TARGET_NONE = 0;
	public const int BULLET_TARGET_UNIT = 1;
	public const int BULLET_TARGET_TOWN = 2;
	public const int BULLET_TARGET_FIRM = 3;
	public const int BULLET_TARGET_WALL = 4;


	public int max_step;
	public int target_type;
	public int target_recno;
	public int speed;
	public int origin2_x, origin2_y;

	public BulletHoming()
	{
		target_type = BULLET_TARGET_NONE;
		target_recno = 0;
	}

	public override void init(int parentType, int parentRecno, int targetXLoc, int targetYLoc, int targetMobileType)
	{
		base.init(parentType, parentRecno, targetXLoc, targetYLoc, targetMobileType);

		// ------- find the maximum range --------//

		//**** BUGHERE, using parentType and parentRecno to allow bullet by firm, town, etc.
		//**** BUGHERE, only allow bullet by unit for this version
		Unit parentUnit = UnitArray[parentRecno];

		//---------- copy attack info from the parent unit --------//

		AttackInfo attackInfo = parentUnit.attack_info_array[parentUnit.cur_attack];
		speed = attackInfo.bullet_speed;
		max_step = (attackInfo.attack_range * InternalConstants.CellWidth + speed - 1) / speed;

		//--------- keep backup of centre of the bullet ---------//
		SpriteFrame spriteFrame = cur_sprite_frame(out _);

		// origin_x/y and origin2_x/y are pointing at the centre of the bullet bitmap //
		origin_x += spriteFrame.offset_x + spriteFrame.width / 2;
		origin_y += spriteFrame.offset_y + spriteFrame.height / 2;
		origin2_x = origin_x;
		origin2_y = origin_y;
		go_x += spriteFrame.offset_x + spriteFrame.width / 2;
		go_y += spriteFrame.offset_y + spriteFrame.height / 2;

		// ------- find the target_type and target_recno ------//
		Location location = World.get_loc(targetXLoc, targetYLoc);
		//### begin alex 16/5 ###//
		//if( locPtr->has_unit(mobile_type) )
		if (location.has_unit(targetMobileType))
		{
			target_type = BULLET_TARGET_UNIT;
			//target_recno = locPtr->unit_recno(mobile_type);
			target_recno = location.unit_recno(targetMobileType);
		}
		//#### end alex 16/5 ####//
		else if (location.is_town())
		{
			target_type = BULLET_TARGET_TOWN;
			target_recno = location.town_recno();
		}
		else if (location.is_firm())
		{
			target_type = BULLET_TARGET_FIRM;
			target_recno = location.firm_recno();
		}
		else if (location.is_wall())
		{
			target_type = BULLET_TARGET_WALL;
		}
	}

	public override void process_move()
	{
		int actualStep = total_step;
		SpriteFrame spriteFrame;
		int adjX;
		int adjY;

		if (target_type == BULLET_TARGET_UNIT)
		{
			if (UnitArray.IsDeleted(target_recno))
			{
				target_type = BULLET_TARGET_NONE;
			}
			else
			{
				Unit unit = UnitArray[target_recno];
				if (!unit.is_visible())
				{
					// target lost/die, proceed to Bullet::process_move
					target_type = BULLET_TARGET_NONE;
				}
				else
				{
					// ---- calculate new target_x_loc, target_y_loc -----//	

					target_x_loc = unit.next_x_loc();
					target_y_loc = unit.next_y_loc();

					// ---- re-calculate go_x, go_y  ------//
					// go_x/y and origin2_x/y are pointing at the centre of the bullet bitmap
					// it is different from Bullet
					go_x = unit.cur_x + InternalConstants.CellWidth / 2;
					go_y = unit.cur_y + InternalConstants.CellHeight / 2;

					//---------- set bullet movement steps -----------//
					spriteFrame = cur_sprite_frame(out _);
					adjX = spriteFrame.offset_x + spriteFrame.width / 2;
					adjY = spriteFrame.offset_y + spriteFrame.height / 2;

					int xStep = Math.Abs(go_x - (cur_x + adjX)) / speed;
					int yStep = Math.Abs(go_y - (cur_y + adjY)) / speed;
					total_step = cur_step + Math.Max(xStep, yStep);

					// a homing bullet has a limited range, if the target go outside the
					// the limit, the bullet can't attack the target
					// in this case, actualStep is the number step from the source
					// to the target; total_step is the max_step
					// otherwise, actualStep is as same as total_step

					actualStep = total_step;
					if (total_step > max_step)
					{
						total_step = max_step;
						// target_x_loc and target_y_loc is limited also
						target_x_loc = cur_x + adjX +
						               (go_x - (cur_x + adjX)) / (actualStep - total_step) / InternalConstants.CellWidth;
						target_x_loc = (cur_y + adjY) +
						               (go_y - (cur_y + adjY)) / (actualStep - total_step) / InternalConstants.CellHeight;
					}
				}
			}
		}

		//	origin2_x = origin_x;
		//	origin2_y = origin_y;

		// origin_x/y and origin2_x/y are pointing at the centre of the bullet bitmap //
		spriteFrame = cur_sprite_frame(out _);
		adjX = spriteFrame.offset_x + spriteFrame.width / 2;
		adjY = spriteFrame.offset_y + spriteFrame.height / 2;
		origin_x = cur_x + adjX;
		origin_y = cur_y + adjY;

		cur_x = origin_x + (go_x - origin_x) / (actualStep + 1 - cur_step);
		cur_y = origin_y + (go_y - origin_y) / (actualStep + 1 - cur_step);
		// cur_x, cur_y is temporary pointing at the centre of bullet bitmap

		// detect changing direction
		if (cur_step > 3) // not allow changing direction so fast
			set_dir(origin2_x, origin2_y, cur_x, cur_y);

		// change cur_x, cur_y to bitmap reference point
		spriteFrame = cur_sprite_frame(out _);
		adjX = spriteFrame.offset_x + spriteFrame.width / 2;
		adjY = spriteFrame.offset_y + spriteFrame.height / 2;
		cur_x -= adjX;
		cur_y -= adjY;

		cur_step++;

		//------- update frame id. --------//

		if (++cur_frame > cur_sprite_move().frame_count)
			cur_frame = 1;

		//----- if the sprite has reach the destintion ----//

		if (cur_step > total_step)
		{
			check_hit();

			cur_action = SPRITE_DIE; // Explosion
			// ###### begin Gilbert 17/5 ########//
			// if it has die frame, adjust cur_x, cur_y to be align with the target_x_loc, target_y_loc
			if (sprite_info.die.first_frame_recno != 0)
			{
				next_x = cur_x = target_x_loc * InternalConstants.CellWidth;
				next_y = cur_y = target_y_loc * InternalConstants.CellHeight;
			}

			// ###### end Gilbert 17/5 ########//
			cur_frame = 1;
		}
		// change of total_step may not call warn_target, so call more warn_target
		else if (total_step - cur_step <= 1)
		{
			warn_target();
		}
	}
}