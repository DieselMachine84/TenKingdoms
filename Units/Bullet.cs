using System;

namespace TenKingdoms;

public class Bullet : Sprite
{
	public const int BULLET_BY_UNIT = 1;
	public const int BULLET_BY_FIRM = 2;
	public const int SCAN_RADIUS = 2;
	public const int SCAN_RANGE = SCAN_RADIUS * 2 + 1;

	public int parent_type;
	public int parent_recno;

	//char	mobile_type;			// mobile type of the bullet
	public int target_mobile_type;
	public double attack_damage;
	public int damage_radius;
	public int nation_recno;
	public int fire_radius;

	public int origin_x, origin_y;
	public int target_x_loc, target_y_loc;
	public int cur_step, total_step;

	private static int[] spiral_x = { 0, 0, -1, 0, 1, -1, -1, 1, 1, 0, -2, 0, 2, -1, -2, -2, -1, 1, 2, 2, 1, -2, -2, 2, 2 };

	private static int[] spiral_y = { 0, -1, 0, 1, 0, -1, 1, 1, -1, -2, 0, 2, 0, -2, -1, 1, 2, 2, 1, -1, -2, -2, 2, 2, -2 };

	public Bullet()
	{
		SpriteResId = 0;
	}

	public virtual void init(int parentType, int parentRecno, int targetXLoc, int targetYLoc, int targetMobileType)
	{
		parent_type = parentType;
		parent_recno = parentRecno;
		target_mobile_type = targetMobileType;

		//**** BUGHERE, using parentType and parentRecno to allow bullet by firm, town, etc.
		//**** BUGHERE, only allow bullet by unit for this version
		Unit parentUnit = UnitArray[parentRecno];

		//---------- copy attack info from the parent unit --------//

		AttackInfo attackInfo = parentUnit.AttackInfos[parentUnit.CurAttack];

		attack_damage = parentUnit.actual_damage();
		damage_radius = attackInfo.bullet_radius;
		nation_recno = parentUnit.NationId;
		fire_radius = attackInfo.fire_radius;

		//----- clone vars from sprite_res for fast access -----//

		SpriteResId = attackInfo.bullet_sprite_id;
		SpriteInfo = SpriteRes[SpriteResId];

		//sprite_info.load_bitmap_res(); 

		//--------- set the starting position of the bullet -------//

		CurAction = SPRITE_MOVE;
		CurFrame = 1;
		SetDir(parentUnit.AttackDirection);

		SpriteFrame spriteFrame = CurSpriteFrame(out _);

		origin_x = CurX = parentUnit.CurX;
		origin_y = CurY = parentUnit.CurY;

		//------ set the target position and bullet mobile_type -------//

		target_x_loc = targetXLoc;
		target_y_loc = targetYLoc;

		// -spriteFrame.offset_x to make abs_x1 & abs_y1 = original x1 & y1. So the bullet will be centered on the target
		GoX = target_x_loc * InternalConstants.CellWidth + InternalConstants.CellWidth / 2 - spriteFrame.OffsetX - spriteFrame.Width / 2;
		GoY = target_y_loc * InternalConstants.CellHeight + InternalConstants.CellHeight / 2 - spriteFrame.OffsetY - spriteFrame.Height / 2;

		MobileType = parentUnit.MobileType;

		//---------- set bullet movement steps -----------//

		int xStep = (GoX - CurX) / attackInfo.bullet_speed;
		int yStep = (GoY - CurY) / attackInfo.bullet_speed;

		total_step = Math.Max(1, Math.Max(Math.Abs(xStep), Math.Abs(yStep)));
		cur_step = 0;
	}

	public override void ProcessMove()
	{
		//-------------- update position -----------------//
		//
		// If it gets very close to the destination, fit it
		// to the destination ignoring the normal vector.
		//
		//------------------------------------------------//

		CurX = origin_x + (GoX - origin_x) * cur_step / total_step;
		CurY = origin_y + (GoY - origin_y) * cur_step / total_step;

		//cur_step++;

		//------- update frame id. --------//

		if (++CurFrame > CurSpriteMove().FrameCount)
			CurFrame = 1;

		//----- if the sprite has reach the destintion ----//

		//if( cur_step > total_step )
		if (++cur_step > total_step)
		{
			check_hit();

			CurAction = SPRITE_DIE; // Explosion

			// if it has die frame, adjust cur_x, cur_y to be align with the target_x_loc, target_y_loc
			if (SpriteInfo.Die.FirstFrameId != 0)
			{
				NextX = CurX = target_x_loc * InternalConstants.CellWidth;
				NextY = CurY = target_y_loc * InternalConstants.CellHeight;
			}

			CurFrame = 1;
		}
		else if (total_step - cur_step == 1)
		{
			warn_target();
		}
	}

	public override bool ProcessDie()
	{
		// ------- sound effect --------//
		SERes.sound(CurLocX, CurLocY, CurFrame, 'S', SpriteResId, "DIE");

		//--------- next frame ---------//
		if (++CurFrame > SpriteInfo.Die.FrameCount)
			// ####### begin Gilbert 28/6 ########//
			if (++CurFrame > SpriteInfo.Die.FrameCount)
			{
				// ------- set fire on the target area --------//
				if (fire_radius > 0)
				{
					if (fire_radius == 1)
					{
						Location location = World.GetLoc(target_x_loc, target_y_loc);
						if (location.CanSetFire() && location.FireStrength() < 30)
							location.SetFireStrength(30);
						if (location.Flammability() > 0)
							location.SetFlammability(1); // such that the fire will be put out quickly
					}
					else
					{
						int x1 = target_x_loc - fire_radius + 1;
						if (x1 < 0)
							x1 = 0;
						int y1 = target_y_loc - fire_radius + 1;
						if (y1 < 0)
							y1 = 0;
						int x2 = target_x_loc + fire_radius - 1;
						if (x2 >= GameConstants.MapSize)
							x2 = GameConstants.MapSize - 1;
						int y2 = target_y_loc + fire_radius - 1;
						if (y2 >= GameConstants.MapSize)
							y2 = GameConstants.MapSize - 1;

						for (int y = y1; y <= y2; ++y)
						{
							for (int x = x1; x <= x2; ++x)
							{
								Location location = World.GetLoc(x, y);
								// ##### begin Gilbert 30/10 ######//
								int dist = Math.Abs(x - target_x_loc) + Math.Abs(y - target_y_loc);
								if (dist > fire_radius)
									continue;
								int fl = 30 - dist * 7;
								if (fl < 10)
									fl = 10;
								if (location.CanSetFire() && location.FireStrength() < fl)
									location.SetFireStrength(fl);
								if (location.Flammability() > 0)
									location.SetFlammability(1); // such that the fire will be put out quickly
								// ##### begin Gilbert 30/10 ######//
							}
						}
					}
				}

				return true;
			}

		// ####### end Gilbert 28/6 ########//
		return false;
	}

	public void hit_target(int x, int y)
	{
		//---- check if there is any unit in the target location ----//

		Location location = World.GetLoc(x, y);
		int targetUnitRecno = location.UnitId(target_mobile_type);
		if (UnitArray.IsDeleted(targetUnitRecno))
			return; // the target unit is deleted

		Unit targetUnit = UnitArray[targetUnitRecno];
		Unit parentUnit;
		if (UnitArray.IsDeleted(parent_recno))
		{
			parentUnit = null; // parent is dead
			if (NationArray.IsDeleted(nation_recno))
				return;
		}
		else
		{
			parentUnit = UnitArray[parent_recno];
			nation_recno = parentUnit.NationId;
		}

		double attackDamage = attenuated_damage(targetUnit.CurX, targetUnit.CurY);

		// -------- if the unit is guarding reduce damage ----------//
		if (attackDamage == 0)
			return;

		if (targetUnit.NationId == nation_recno)
		{
			if (targetUnit.UnitType == UnitConstants.UNIT_EXPLOSIVE_CART)
				((UnitExpCart)targetUnit).trigger_explode();
			return;
		}

		if (!NationArray.should_attack(nation_recno, targetUnit.NationId))
			return;

		if (targetUnit.IsGuarding())
		{
			switch (targetUnit.CurAction)
			{
				case SPRITE_IDLE:
				case SPRITE_READY_TO_MOVE:
				case SPRITE_TURN:
				case SPRITE_MOVE:
				case SPRITE_ATTACK:
					// check if on the opposite direction
					if ((targetUnit.CurDir & 7) == ((CurDir + 4) & 7) ||
					    (targetUnit.CurDir & 7) == ((CurDir + 3) & 7) ||
					    (targetUnit.CurDir & 7) == ((CurDir + 5) & 7))
					{
						attackDamage = (attackDamage > 10.0 / InternalConstants.ATTACK_SLOW_DOWN)
							? attackDamage - 10.0 / InternalConstants.ATTACK_SLOW_DOWN
							: 0.0;
						SERes.sound(targetUnit.CurLocX, targetUnit.CurLocY, 1, 'S',
							targetUnit.SpriteResId, "DEF", 'S', SpriteResId);
					}

					break;
			}
		}

		targetUnit.hit_target(parentUnit, targetUnit, attackDamage, nation_recno);
	}

	public void hit_building(int x, int y)
	{
		Location location = World.GetLoc(x, y);

		if (location.IsFirm())
		{
			Firm firm = FirmArray[location.FirmId()];
			if (!NationArray.should_attack(nation_recno, firm.nation_recno))
				return;
		}
		else if (location.IsTown())
		{
			Town town = TownArray[location.TownId()];
			if (!NationArray.should_attack(nation_recno, town.NationId))
				return;
		}
		else
			return;

		double attackDamage = attenuated_damage(x * InternalConstants.CellWidth, y * InternalConstants.CellHeight);
		// BUGHERE : hit building of same nation?
		if (attackDamage <= 0.0)
			return;

		Unit virtualUnit = null;
		Unit parentUnit;
		if (UnitArray.IsDeleted(parent_recno))
		{
			parentUnit = null;
			if (NationArray.IsDeleted(nation_recno))
				return;

			foreach (Unit unit in UnitArray)
			{
				virtualUnit = unit;
				break;
			}

			if (virtualUnit == null)
				return; //**** BUGHERE
		}
		else
		{
			virtualUnit = parentUnit = UnitArray[parent_recno];
		}

		virtualUnit.hit_building(parentUnit, target_x_loc, target_y_loc, attackDamage, nation_recno);
	}

	public void hit_wall(int x, int y)
	{
		Location location = World.GetLoc(x, y);

		if (!location.IsWall())
			return;

		double attackDamage = attenuated_damage(x * InternalConstants.CellWidth, y * InternalConstants.CellHeight);
		if (attackDamage == 0)
			return;

		Unit virtualUnit = null;
		Unit parentUnit;
		if (UnitArray.IsDeleted(parent_recno))
		{
			parentUnit = null;
			if (NationArray.IsDeleted(nation_recno))
				return;

			foreach (Unit unit in UnitArray)
			{
				virtualUnit = unit;
				break;
			}

			if (virtualUnit == null)
				return; //**** BUGHERE
		}
		else
		{
			virtualUnit = parentUnit = UnitArray[parent_recno];
		}

		virtualUnit.hit_wall(parentUnit, target_x_loc, target_y_loc, attackDamage, nation_recno);
	}

	public double attenuated_damage(int curX, int curY)
	{
		int d = Misc.points_distance(curX, curY,
			target_x_loc * InternalConstants.CellWidth,
			target_y_loc * InternalConstants.CellHeight);
		// damage drops from attack_damage to attack_damage/2, as range drops from 0 to damage_radius
		if (d > damage_radius)
			return 0.0;
		else
			//return ((attack_damage * (2*damage_radius-d) + 2*damage_radius-1)/ (2*damage_radius) );		// ceiling
			return attack_damage - attack_damage * d / (2.0 * damage_radius);
	}

	public int check_hit()
	{
		int[] townHit = new int[SCAN_RANGE * SCAN_RANGE];
		int[] firmHit = new int[SCAN_RANGE * SCAN_RANGE];
		int hitCount = 0;
		int townHitCount = 0;
		int firmHitCount = 0;

		for (int c = 0; c < SCAN_RANGE * SCAN_RANGE; ++c)
		{
			int x = target_x_loc + spiral_x[c];
			int y = target_y_loc + spiral_y[c];
			if (x >= 0 && x < GameConstants.MapSize && y >= 0 && y < GameConstants.MapSize)
			{
				Location location = World.GetLoc(x, y);
				if (target_mobile_type == UnitConstants.UNIT_AIR)
				{
					if (location.HasUnit(UnitConstants.UNIT_AIR))
					{
						hit_target(x, y);
						hitCount++;
					}
				}
				else
				{
					if (location.IsFirm())
					{
						int firmRecno = location.FirmId();
						// check this firm has not been attacked
						bool found = false;
						for (int i = firmHitCount - 1; i >= 0; i--)
						{
							if (firmHit[i] == firmRecno)
							{
								found = true;
								break;
							}
						}

						if (!found) // not found
						{
							firmHit[firmHitCount++] = firmRecno;
							hit_building(x, y);
							hitCount++;
						}
					}
					else if (location.IsTown())
					{
						int townRecno = location.TownId();
						// check this town has not been attacked
						bool found = false;
						for (int i = townHitCount - 1; i >= 0; i--)
						{
							if (townHit[i] == townRecno)
							{
								found = true;
								break;
							}
						}

						if (!found) // not found
						{
							townHit[townHitCount++] = townRecno;
							hit_building(x, y);
							hitCount++;
						}
					}
					else if (location.IsWall())
					{
						hit_wall(x, y);
						hitCount++;
					}
					else
					{
						// note: no error checking here because mobile_type should be taken into account
						hit_target(x, y);
						hitCount++;
					}
				}
			}
		}

		return hitCount;
	}

	public int warn_target()
	{
		int warnCount = 0;

		for (int c = 0; c < SCAN_RANGE * SCAN_RANGE; ++c)
		{
			int x = target_x_loc + spiral_x[c];
			int y = target_y_loc + spiral_y[c];
			if (x >= 0 && x < GameConstants.MapSize && y >= 0 && y < GameConstants.MapSize)
			{
				Location locPtr = World.GetLoc(x, y);
				//char targetMobileType;
				//if( (targetMobileType = locPtr.has_any_unit()) != 0)
				//{
				//	short unitRecno = locPtr.unit_recno(UnitConstants.UNIT_LAND);
				int unitRecno = locPtr.UnitId(target_mobile_type);
				if (!UnitArray.IsDeleted(unitRecno))
				{
					Unit unit = UnitArray[unitRecno];
					if (attenuated_damage(unit.CurX, unit.CurY) > 0)
					{
						warnCount++;
						switch (unit.CurAction)
						{
							case SPRITE_IDLE:
							case SPRITE_READY_TO_MOVE:
								//case SPRITE_TURN:
								if (unit.can_stand_guard() && !unit.IsGuarding())
								{
									unit.SetDir((CurDir + 4) & 7); // opposite direction of arrow
									unit.SetGuardOn();
								}

								break;
							case SPRITE_MOVE:
								if (unit.can_move_guard() && !unit.IsGuarding() &&
								    ((unit.CurDir & 7) == ((CurDir + 4) & 7) ||
								     (unit.CurDir & 7) == ((CurDir + 5) & 7) ||
								     (unit.CurDir & 7) == ((CurDir + 3) & 7)))
								{
									unit.SetGuardOn();
								}

								break;
							case SPRITE_ATTACK:
								if (unit.can_attack_guard() && !unit.IsGuarding() &&
								    unit.RemainAttackDelay >= InternalConstants.GUARD_COUNT_MAX &&
								    ((unit.CurDir & 7) == ((CurDir + 4) & 7) ||
								     (unit.CurDir & 7) == ((CurDir + 5) & 7) ||
								     (unit.CurDir & 7) == ((CurDir + 3) & 7)))
								{
									unit.SetGuardOn();
								}

								break;
						}
					}
				}
				//}
			}
		}

		return warnCount;
	}

	public virtual int display_layer()
	{
		if (MobileType == UnitConstants.UNIT_AIR || target_mobile_type == UnitConstants.UNIT_AIR)
			return 8;
		else
			return 1;
	}
}