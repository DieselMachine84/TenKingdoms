using System;

namespace TenKingdoms;

public class BulletHoming : Bullet
{
	private const int BULLET_TARGET_NONE = 0;
	private const int BULLET_TARGET_UNIT = 1;
	private const int BULLET_TARGET_TOWN = 2;
	private const int BULLET_TARGET_FIRM = 3;
	private const int BULLET_TARGET_WALL = 4;

	private int TargetType { get; set; }
	private int TargetId { get; set; }
	private int Speed { get; set; }
	private int MaxStep { get; set; }
	private int OriginX2 { get; set; }
	private int OriginY2 { get; set; }

	public BulletHoming()
	{
		TargetType = BULLET_TARGET_NONE;
		TargetId = 0;
	}

	public override void Init(int parentType, int parentId, int targetLocX, int targetLocY, int targetMobileType)
	{
		base.Init(parentType, parentId, targetLocX, targetLocY, targetMobileType);

		Unit parentUnit = UnitArray[parentId];
		AttackInfo attackInfo = parentUnit.AttackInfos[parentUnit.CurAttack];
		
		Speed = attackInfo.bullet_speed;
		MaxStep = (attackInfo.attack_range * InternalConstants.CellWidth + Speed - 1) / Speed;

		//--------- keep backup of center of the bullet ---------//
		SpriteFrame spriteFrame = CurSpriteFrame(out _);

		//OriginX/OriginY and OriginX2/OriginY2 are pointing at the center of the bullet bitmap
		OriginX += spriteFrame.OffsetX + spriteFrame.Width / 2;
		OriginY += spriteFrame.OffsetY + spriteFrame.Height / 2;
		OriginX2 = OriginX;
		OriginY2 = OriginY;
		GoX += spriteFrame.OffsetX + spriteFrame.Width / 2;
		GoY += spriteFrame.OffsetY + spriteFrame.Height / 2;

		Location location = World.GetLoc(targetLocX, targetLocY);
		if (location.HasUnit(targetMobileType))
		{
			TargetType = BULLET_TARGET_UNIT;
			TargetId = location.UnitId(targetMobileType);
		}
		else if (location.IsTown())
		{
			TargetType = BULLET_TARGET_TOWN;
			TargetId = location.TownId();
		}
		else if (location.IsFirm())
		{
			TargetType = BULLET_TARGET_FIRM;
			TargetId = location.FirmId();
		}
		else if (location.IsWall())
		{
			TargetType = BULLET_TARGET_WALL;
		}
	}

	public override void ProcessMove()
	{
		int actualStep = TotalStep;
		SpriteFrame spriteFrame;
		int adjX;
		int adjY;

		if (TargetType == BULLET_TARGET_UNIT)
		{
			if (UnitArray.IsDeleted(TargetId))
			{
				TargetType = BULLET_TARGET_NONE;
			}
			else
			{
				Unit unit = UnitArray[TargetId];
				if (!unit.IsVisible())
				{
					// target lost/die, proceed to Bullet.ProcessMove()
					TargetType = BULLET_TARGET_NONE;
				}
				else
				{
					TargetLocX = unit.NextLocX;
					TargetLocY = unit.NextLocY;

					// ---- re-calculate GoX, GoY  ------//
					// GoX, GoY and OriginX2, OriginY2 are pointing at the center of the bullet bitmap
					// it is different from Bullet
					GoX = unit.CurX + InternalConstants.CellWidth / 2;
					GoY = unit.CurY + InternalConstants.CellHeight / 2;

					//---------- set bullet movement steps -----------//
					spriteFrame = CurSpriteFrame(out _);
					adjX = spriteFrame.OffsetX + spriteFrame.Width / 2;
					adjY = spriteFrame.OffsetY + spriteFrame.Height / 2;

					int xStep = Math.Abs(GoX - (CurX + adjX)) / Speed;
					int yStep = Math.Abs(GoY - (CurY + adjY)) / Speed;
					TotalStep = CurStep + Math.Max(xStep, yStep);

					// a homing bullet has a limited range, if the target go outside the limit,
					// the bullet can't attack the target
					// in this case, actualStep is the number step from the source to the target;
					// TotalStep is the MaxStep otherwise, actualStep is as same as TotalStep

					actualStep = TotalStep;
					if (TotalStep > MaxStep)
					{
						TotalStep = MaxStep;
						// TargetLocX and TargetLocY are limited also
						TargetLocX = CurX + adjX + (GoX - (CurX + adjX)) / (actualStep - TotalStep) / InternalConstants.CellWidth;
						TargetLocY = CurY + adjY + (GoY - (CurY + adjY)) / (actualStep - TotalStep) / InternalConstants.CellHeight;
					}
				}
			}
		}

		//OriginX2 = OriginX;
		//OriginY2 = OriginY;

		//OriginX/OriginY and OriginX2/OriginY2 are pointing at the center of the bullet bitmap
		spriteFrame = CurSpriteFrame(out _);
		adjX = spriteFrame.OffsetX + spriteFrame.Width / 2;
		adjY = spriteFrame.OffsetY + spriteFrame.Height / 2;
		OriginX = CurX + adjX;
		OriginY = CurY + adjY;

		CurX = OriginX + (GoX - OriginX) / (actualStep + 1 - CurStep);
		CurY = OriginY + (GoY - OriginY) / (actualStep + 1 - CurStep);
		// CurX, CurY is temporarily pointing at the center of bullet bitmap

		// detect changing direction
		if (CurStep > 3) // not allow changing direction so fast
			SetDir(OriginX2, OriginY2, CurX, CurY);

		// change CurX, CurY to bitmap reference point
		spriteFrame = CurSpriteFrame(out _);
		adjX = spriteFrame.OffsetX + spriteFrame.Width / 2;
		adjY = spriteFrame.OffsetY + spriteFrame.Height / 2;
		CurX -= adjX;
		CurY -= adjY;

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
		// change of total_step may not call warn_target, so call more warn_target
		else if (TotalStep - CurStep <= 1)
		{
			WarnTarget();
		}
	}
}