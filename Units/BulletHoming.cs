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

		AttackInfo attackInfo = parentUnit.AttackInfos[parentUnit.CurAttack];
		speed = attackInfo.bullet_speed;
		max_step = (attackInfo.attack_range * InternalConstants.CellWidth + speed - 1) / speed;

		//--------- keep backup of centre of the bullet ---------//
		SpriteFrame spriteFrame = CurSpriteFrame(out _);

		// origin_x/y and origin2_x/y are pointing at the centre of the bullet bitmap //
		origin_x += spriteFrame.OffsetX + spriteFrame.Width / 2;
		origin_y += spriteFrame.OffsetY + spriteFrame.Height / 2;
		origin2_x = origin_x;
		origin2_y = origin_y;
		GoX += spriteFrame.OffsetX + spriteFrame.Width / 2;
		GoY += spriteFrame.OffsetY + spriteFrame.Height / 2;

		// ------- find the target_type and target_recno ------//
		Location location = World.GetLoc(targetXLoc, targetYLoc);
		//### begin alex 16/5 ###//
		//if( locPtr->has_unit(mobile_type) )
		if (location.HasUnit(targetMobileType))
		{
			target_type = BULLET_TARGET_UNIT;
			//target_recno = locPtr->unit_recno(mobile_type);
			target_recno = location.UnitId(targetMobileType);
		}
		//#### end alex 16/5 ####//
		else if (location.IsTown())
		{
			target_type = BULLET_TARGET_TOWN;
			target_recno = location.TownId();
		}
		else if (location.IsFirm())
		{
			target_type = BULLET_TARGET_FIRM;
			target_recno = location.FirmId();
		}
		else if (location.IsWall())
		{
			target_type = BULLET_TARGET_WALL;
		}
	}

	public override void ProcessMove()
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

					target_x_loc = unit.NextLocX;
					target_y_loc = unit.NextLocY;

					// ---- re-calculate go_x, go_y  ------//
					// go_x/y and origin2_x/y are pointing at the centre of the bullet bitmap
					// it is different from Bullet
					GoX = unit.CurX + InternalConstants.CellWidth / 2;
					GoY = unit.CurY + InternalConstants.CellHeight / 2;

					//---------- set bullet movement steps -----------//
					spriteFrame = CurSpriteFrame(out _);
					adjX = spriteFrame.OffsetX + spriteFrame.Width / 2;
					adjY = spriteFrame.OffsetY + spriteFrame.Height / 2;

					int xStep = Math.Abs(GoX - (CurX + adjX)) / speed;
					int yStep = Math.Abs(GoY - (CurY + adjY)) / speed;
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
						target_x_loc = CurX + adjX +
						               (GoX - (CurX + adjX)) / (actualStep - total_step) / InternalConstants.CellWidth;
						target_x_loc = (CurY + adjY) +
						               (GoY - (CurY + adjY)) / (actualStep - total_step) / InternalConstants.CellHeight;
					}
				}
			}
		}

		//	origin2_x = origin_x;
		//	origin2_y = origin_y;

		// origin_x/y and origin2_x/y are pointing at the centre of the bullet bitmap //
		spriteFrame = CurSpriteFrame(out _);
		adjX = spriteFrame.OffsetX + spriteFrame.Width / 2;
		adjY = spriteFrame.OffsetY + spriteFrame.Height / 2;
		origin_x = CurX + adjX;
		origin_y = CurY + adjY;

		CurX = origin_x + (GoX - origin_x) / (actualStep + 1 - cur_step);
		CurY = origin_y + (GoY - origin_y) / (actualStep + 1 - cur_step);
		// cur_x, cur_y is temporary pointing at the centre of bullet bitmap

		// detect changing direction
		if (cur_step > 3) // not allow changing direction so fast
			SetDir(origin2_x, origin2_y, CurX, CurY);

		// change cur_x, cur_y to bitmap reference point
		spriteFrame = CurSpriteFrame(out _);
		adjX = spriteFrame.OffsetX + spriteFrame.Width / 2;
		adjY = spriteFrame.OffsetY + spriteFrame.Height / 2;
		CurX -= adjX;
		CurY -= adjY;

		cur_step++;

		//------- update frame id. --------//

		if (++CurFrame > CurSpriteMove().FrameCount)
			CurFrame = 1;

		//----- if the sprite has reach the destintion ----//

		if (cur_step > total_step)
		{
			check_hit();

			CurAction = SPRITE_DIE; // Explosion
			// ###### begin Gilbert 17/5 ########//
			// if it has die frame, adjust cur_x, cur_y to be align with the target_x_loc, target_y_loc
			if (SpriteInfo.Die.FirstFrameId != 0)
			{
				NextX = CurX = target_x_loc * InternalConstants.CellWidth;
				NextY = CurY = target_y_loc * InternalConstants.CellHeight;
			}

			// ###### end Gilbert 17/5 ########//
			CurFrame = 1;
		}
		// change of total_step may not call warn_target, so call more warn_target
		else if (total_step - cur_step <= 1)
		{
			warn_target();
		}
	}
}