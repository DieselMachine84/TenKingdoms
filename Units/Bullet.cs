using System;

namespace TenKingdoms;

public class Bullet : Sprite
{
	public const int BULLET_BY_UNIT = 1;
	public const int BULLET_BY_FIRM = 2;
	private const int SCAN_RADIUS = 2;
	private const int SCAN_RANGE = SCAN_RADIUS * 2 + 1;

	private static int[] TownHit { get; } = new int[SCAN_RANGE * SCAN_RANGE];
	private static int[] FirmHit { get; } = new int[SCAN_RANGE * SCAN_RANGE];
	private static int[] SpiralX { get; } = { 0, 0, -1, 0, 1, -1, -1, 1, 1, 0, -2, 0, 2, -1, -2, -2, -1, 1, 2, 2, 1, -2, -2, 2, 2 };

	private static int[] SpiralY { get; } = { 0, -1, 0, 1, 0, -1, 1, 1, -1, -2, 0, 2, 0, -2, -1, 1, 2, 2, 1, -1, -2, -2, 2, 2, -2 };
	
	private int ParentType { get; set; }
	private int ParentId { get; set; }
	private int NationId { get; set; }

	protected int TargetMobileType { get; private set; }
	private double AttackDamage { get; set; }
	private int DamageRadius { get; set; }
	private int FireRadius { get; set; }

	protected int OriginX { get; set; }
	protected int OriginY { get; set; }
	protected int TargetLocX { get; set; }
	protected int TargetLocY { get; set; }
	
	public int CurStep { get; protected set; }
	public int TotalStep { get; protected set; }

	public Bullet()
	{
		SpriteResId = 0;
	}

	public virtual void Init(int parentType, int parentId, int targetLocX, int targetLocY, int targetMobileType)
	{
		ParentType = parentType;
		ParentId = parentId;
		TargetMobileType = targetMobileType;

		Unit parentUnit = UnitArray[parentId];
		AttackInfo attackInfo = parentUnit.AttackInfos[parentUnit.CurAttack];

		AttackDamage = parentUnit.ActualDamage();
		DamageRadius = attackInfo.bullet_radius;
		NationId = parentUnit.NationId;
		FireRadius = attackInfo.fire_radius;

		SpriteResId = attackInfo.bullet_sprite_id;
		SpriteInfo = SpriteRes[SpriteResId];

		//--------- set the starting position of the bullet -------//

		CurAction = SPRITE_MOVE;
		CurFrame = 1;
		SetDir(parentUnit.AttackDirection);

		OriginX = CurX = parentUnit.CurX;
		OriginY = CurY = parentUnit.CurY;

		TargetLocX = targetLocX;
		TargetLocY = targetLocY;

		// -spriteFrame.offset_x to make abs_x1 & abs_y1 = original x1 & y1. So the bullet will be centered on the target
		SpriteFrame spriteFrame = CurSpriteFrame(out _);
		GoX = TargetLocX * InternalConstants.CellWidth + InternalConstants.CellWidth / 2 - spriteFrame.OffsetX - spriteFrame.Width / 2;
		GoY = TargetLocY * InternalConstants.CellHeight + InternalConstants.CellHeight / 2 - spriteFrame.OffsetY - spriteFrame.Height / 2;

		MobileType = parentUnit.MobileType;

		//---------- set bullet movement steps -----------//

		int xStep = (GoX - CurX) / attackInfo.bullet_speed;
		int yStep = (GoY - CurY) / attackInfo.bullet_speed;

		CurStep = 0;
		TotalStep = Math.Max(1, Math.Max(Math.Abs(xStep), Math.Abs(yStep)));
	}

	public override void ProcessMove()
	{
		// if it gets very close to the destination, fit it to the destination ignoring the normal vector.

		CurX = OriginX + (GoX - OriginX) * CurStep / TotalStep;
		CurY = OriginY + (GoY - OriginY) * CurStep / TotalStep;

		if (++CurFrame > CurSpriteMove().FrameCount)
			CurFrame = 1;

		//----- if the sprite has reach the destination ----//

		if (++CurStep > TotalStep)
		{
			CheckHit();

			CurAction = SPRITE_DIE; // Explosion

			// if it has die frame, adjust CurX, CurY to be align with the TargetLocX, TargetLocY
			if (SpriteInfo.Die.FirstFrameId != 0)
			{
				NextX = CurX = TargetLocX * InternalConstants.CellWidth;
				NextY = CurY = TargetLocY * InternalConstants.CellHeight;
			}

			CurFrame = 1;
		}
		else if (TotalStep - CurStep <= 1)
		{
			WarnTarget();
		}
	}

	public override bool ProcessDie()
	{
		SERes.sound(CurLocX, CurLocY, CurFrame, 'S', SpriteResId, "DIE");

		//TODO check double if conditions
		//--------- next frame ---------//
		if (++CurFrame > SpriteInfo.Die.FrameCount)
		{
			if (++CurFrame > SpriteInfo.Die.FrameCount)
			{
				// ------- set fire on the target area --------//
				if (FireRadius > 0)
				{
					if (FireRadius == 1)
					{
						Location location = World.GetLoc(TargetLocX, TargetLocY);
						if (location.CanSetFire() && location.FireStrength() < 30)
							location.SetFireStrength(30);
						if (location.Flammability() > 0)
							location.SetFlammability(1); // such that the fire will be put out quickly
					}
					else
					{
						int locX1 = TargetLocX - FireRadius + 1;
						int locY1 = TargetLocY - FireRadius + 1;
						Misc.BoundLocation(ref locX1, ref locY1);
						int locX2 = TargetLocX + FireRadius - 1;
						int locY2 = TargetLocY + FireRadius - 1;
						Misc.BoundLocation(ref locX2, ref locY2);

						for (int locY = locY1; locY <= locY2; locY++)
						{
							for (int locX = locX1; locX <= locX2; locX++)
							{
								Location location = World.GetLoc(locX, locY);
								int dist = Math.Abs(locX - TargetLocX) + Math.Abs(locY - TargetLocY);
								if (dist > FireRadius)
									continue;
								int fireStrength = 30 - dist * 7;
								if (fireStrength < 10)
									fireStrength = 10;
								if (location.CanSetFire() && location.FireStrength() < fireStrength)
									location.SetFireStrength(fireStrength);
								if (location.Flammability() > 0)
									location.SetFlammability(1); // such that the fire will be put out quickly
							}
						}
					}
				}

				return true;
			}
		}

		return false;
	}

	private void HitTarget(int locX, int locY)
	{
		//---- check if there is any unit in the target location ----//

		Location location = World.GetLoc(locX, locY);
		int targetUnitId = location.UnitId(TargetMobileType);
		if (UnitArray.IsDeleted(targetUnitId))
			return;

		Unit targetUnit = UnitArray[targetUnitId];
		Unit parentUnit = null;
		if (UnitArray.IsDeleted(ParentId))
		{
			if (NationArray.IsDeleted(NationId))
				return;
		}
		else
		{
			parentUnit = UnitArray[ParentId];
			NationId = parentUnit.NationId;
		}

		double attackDamage = AttenuatedDamage(targetUnit.CurX, targetUnit.CurY);

		// -------- if the unit is guarding reduce damage ----------//
		if (attackDamage == 0)
			return;

		if (targetUnit.NationId == NationId)
		{
			if (targetUnit.UnitType == UnitConstants.UNIT_EXPLOSIVE_CART)
				((UnitExpCart)targetUnit).trigger_explode();
			return;
		}

		if (!NationArray.should_attack(NationId, targetUnit.NationId))
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

		targetUnit.HitTarget(parentUnit, targetUnit, attackDamage, NationId);
	}

	private void HitBuilding(int locX, int locY)
	{
		Location location = World.GetLoc(locX, locY);

		if (location.IsFirm())
		{
			Firm firm = FirmArray[location.FirmId()];
			if (!NationArray.should_attack(NationId, firm.NationId))
				return;
		}
		else if (location.IsTown())
		{
			Town town = TownArray[location.TownId()];
			if (!NationArray.should_attack(NationId, town.NationId))
				return;
		}
		else
		{
			return;
		}

		double attackDamage = AttenuatedDamage(locX * InternalConstants.CellWidth, locY * InternalConstants.CellHeight);
		// BUGHERE : hit building of same nation?
		if (attackDamage <= 0.0)
			return;

		Unit parentUnit = null;
		if (UnitArray.IsDeleted(ParentId))
		{
			if (NationArray.IsDeleted(NationId))
				return;
		}
		else
		{
			parentUnit = UnitArray[ParentId];
			//TODO check if we need this line
			//NationId = parentUnit.NationId;
		}

		Unit virtualUnit = parentUnit ?? new UnitHuman();
		virtualUnit.HitBuilding(parentUnit, TargetLocX, TargetLocY, attackDamage, NationId);
	}

	private void HitWall(int locX, int locY)
	{
		Location location = World.GetLoc(locX, locY);

		if (!location.IsWall())
			return;

		double attackDamage = AttenuatedDamage(locX * InternalConstants.CellWidth, locY * InternalConstants.CellHeight);
		if (attackDamage <= 0.0)
			return;

		Unit parentUnit = null;
		if (UnitArray.IsDeleted(ParentId))
		{
			if (NationArray.IsDeleted(NationId))
				return;
		}
		else
		{
			parentUnit = UnitArray[ParentId];
			//TODO check if we need this line
			//NationId = parentUnit.NationId;
		}

		Unit virtualUnit = new UnitHuman();
		virtualUnit.HitWall(parentUnit, TargetLocX, TargetLocY, attackDamage, NationId);
	}

	private double AttenuatedDamage(int curX, int curY)
	{
		int distance = Misc.points_distance(curX, curY, TargetLocX * InternalConstants.CellWidth, TargetLocY * InternalConstants.CellHeight);
		// damage drops from AttackDamage to AttackDamage / 2, as range drops from 0 to DamageRadius
		if (distance > DamageRadius)
			return 0.0;
		else
			return AttackDamage - AttackDamage * distance / (2.0 * DamageRadius);
	}

	protected int CheckHit()
	{
		Array.Clear(TownHit);
		Array.Clear(FirmHit);
		int hitCount = 0;
		int townHitCount = 0;
		int firmHitCount = 0;

		for (int c = 0; c < SCAN_RANGE * SCAN_RANGE; ++c)
		{
			int locX = TargetLocX + SpiralX[c];
			int locY = TargetLocY + SpiralY[c];
			if (Misc.IsLocationValid(locX, locY))
			{
				Location location = World.GetLoc(locX, locY);
				if (TargetMobileType == UnitConstants.UNIT_AIR)
				{
					if (location.HasUnit(UnitConstants.UNIT_AIR))
					{
						HitTarget(locX, locY);
						hitCount++;
					}
				}
				else
				{
					if (location.IsFirm())
					{
						int firmId = location.FirmId();
						// check this firm has not been attacked
						bool found = false;
						for (int i = firmHitCount - 1; i >= 0; i--)
						{
							if (FirmHit[i] == firmId)
							{
								found = true;
								break;
							}
						}

						if (!found) // not found
						{
							FirmHit[firmHitCount++] = firmId;
							HitBuilding(locX, locY);
							hitCount++;
						}
					}
					else if (location.IsTown())
					{
						int townId = location.TownId();
						// check this town has not been attacked
						bool found = false;
						for (int i = townHitCount - 1; i >= 0; i--)
						{
							if (TownHit[i] == townId)
							{
								found = true;
								break;
							}
						}

						if (!found) // not found
						{
							TownHit[townHitCount++] = townId;
							HitBuilding(locX, locY);
							hitCount++;
						}
					}
					else if (location.IsWall())
					{
						HitWall(locX, locY);
						hitCount++;
					}
					else
					{
						// note: no error checking here because MobileType should be taken into account
						HitTarget(locX, locY);
						hitCount++;
					}
				}
			}
		}

		return hitCount;
	}

	protected int WarnTarget()
	{
		int warnCount = 0;

		for (int c = 0; c < SCAN_RANGE * SCAN_RANGE; ++c)
		{
			int locX = TargetLocX + SpiralX[c];
			int locY = TargetLocY + SpiralY[c];
			if (Misc.IsLocationValid(locX, locY))
			{
				Location location = World.GetLoc(locX, locY);
				int unitId = location.UnitId(TargetMobileType);
				if (!UnitArray.IsDeleted(unitId))
				{
					Unit unit = UnitArray[unitId];
					if (AttenuatedDamage(unit.CurX, unit.CurY) > 0)
					{
						warnCount++;
						switch (unit.CurAction)
						{
							case SPRITE_IDLE:
							case SPRITE_READY_TO_MOVE:
								//case SPRITE_TURN:
								if (unit.CanStandGuard && !unit.IsGuarding())
								{
									unit.SetDir((CurDir + 4) & 7); // opposite direction of arrow
									unit.SetGuardOn();
								}
								break;
							
							case SPRITE_MOVE:
								if (unit.CanMoveGuard && !unit.IsGuarding() &&
								    ((unit.CurDir & 7) == ((CurDir + 4) & 7) ||
								     (unit.CurDir & 7) == ((CurDir + 5) & 7) ||
								     (unit.CurDir & 7) == ((CurDir + 3) & 7)))
								{
									unit.SetGuardOn();
								}
								break;
							
							case SPRITE_ATTACK:
								if (unit.CanAttackGuard && !unit.IsGuarding() &&
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
			}
		}

		return warnCount;
	}

	public virtual int DisplayLayer()
	{
		if (MobileType == UnitConstants.UNIT_AIR || TargetMobileType == UnitConstants.UNIT_AIR)
			return 8;
		else
			return 1;
	}
}