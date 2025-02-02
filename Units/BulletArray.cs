using System;

namespace TenKingdoms;

public class BulletArray : SpriteArray
{
	private SpriteRes SpriteRes => Sys.Instance.SpriteRes;
	private FirmRes FirmRes => Sys.Instance.FirmRes;
	private World World => Sys.Instance.World;
	private FirmArray FirmArray => Sys.Instance.FirmArray;
	private TownArray TownArray => Sys.Instance.TownArray;

	public BulletArray()
	{
	}

	protected override Sprite CreateNewObject(int objectId)
	{
		SpriteInfo spriteInfo = SpriteRes[objectId];

		switch (spriteInfo.sprite_sub_type)
		{
			case 0:
			case ' ':
				return new Bullet();
			case 'P':
				return new Projectile();
			case 'H':
				return new BulletHoming();
			case 'F':
				return new BulletFlame();
		}

		throw new NotSupportedException();
	}

	public Bullet AddBullet(Unit parentUnit, Unit targetUnit) // unit attacks unit
	{
		//------------------------------------------------------//
		// define parameters
		//------------------------------------------------------//
		SpriteInfo targetSpriteInfo = targetUnit.sprite_info;
		int attackXLoc = parentUnit.range_attack_x_loc;
		int attackYLoc = parentUnit.range_attack_y_loc;
		int targetXLoc = targetUnit.next_x_loc();
		int targetYLoc = targetUnit.next_y_loc();

		if (attackXLoc >= targetXLoc && attackXLoc < targetXLoc + targetSpriteInfo.loc_width &&
		    attackYLoc >= targetYLoc && attackYLoc < targetYLoc + targetSpriteInfo.loc_height)
		{
			//-------------------------------------------------------//
			// the previous used range attack destination can be reused,
			// time is saved 'cos no need to check for bullet_path_possible()
			//-------------------------------------------------------//
			AttackInfo attackInfo = parentUnit.attack_info_array[parentUnit.cur_attack];
			int bulletId = attackInfo.bullet_sprite_id;
			Bullet bullet = (Bullet)AddSprite(bulletId);
			bullet.init(Bullet.BULLET_BY_UNIT, parentUnit.sprite_recno,
				attackXLoc, attackYLoc, targetUnit.mobile_type);
			return bullet;
		}

		return null;
	}

	public Bullet AddBullet(Unit parentUnit, int xLoc, int yLoc) // unit attacks firm, town
	{
		//------------------------------------------------------//
		// define parameters
		//------------------------------------------------------//
		int attackXLoc = parentUnit.range_attack_x_loc;
		int attackYLoc = parentUnit.range_attack_y_loc;
		int targetXLoc = xLoc;
		int targetYLoc = yLoc;
		int width = 0, height = 0;
		Location location = World.get_loc(xLoc, yLoc);

		if (location.is_firm())
		{
			Firm targetFirm = FirmArray[location.firm_recno()];
			FirmInfo firmInfo = FirmRes[targetFirm.firm_id];
			width = firmInfo.loc_width;
			height = firmInfo.loc_height;
		}
		else if (location.is_town())
		{
			Town targetTown = TownArray[location.town_recno()];
			width = targetTown.LocWidth();
			height = targetTown.LocHeight();
		}
		else if (location.is_wall())
			width = height = 1;

		if (attackXLoc >= targetXLoc && attackXLoc < targetXLoc + width &&
		    attackYLoc >= targetYLoc && attackYLoc < targetYLoc + height)
		{
			//-------------------------------------------------------//
			// the previous used range attack destination can be reused,
			// time is saved 'cos no need to check for bullet_path_possible()
			//-------------------------------------------------------//
			AttackInfo attackInfo = parentUnit.attack_info_array[parentUnit.cur_attack];
			int bulletId = attackInfo.bullet_sprite_id;
			Bullet bullet = (Bullet)AddSprite(bulletId);
			bullet.init(Bullet.BULLET_BY_UNIT, parentUnit.sprite_recno,
				attackXLoc, attackYLoc, UnitConstants.UNIT_LAND);
			return bullet;
		}

		return null;
	}

	public bool add_bullet_possible(int startXLoc, int startYLoc, int attackerMobileType,
		int targetXLoc, int targetYLoc, int targetMobileType, int targetWidth, int targetHeight,
		out int resultXLoc, out int resultYLoc, int bulletSpeed, int bulletSpriteId)
	{
		resultXLoc = resultYLoc = -1;

		//----------------------------------------------------------------------//
		// for target with size 1x1
		//----------------------------------------------------------------------//
		if (targetWidth == 1 && targetHeight == 1)
		{
			if (bullet_path_possible(startXLoc, startYLoc, attackerMobileType,
				    targetXLoc, targetYLoc, targetMobileType, bulletSpeed, bulletSpriteId))
			{
				resultXLoc = targetXLoc;
				resultYLoc = targetYLoc;
				return true;
			}
			else
				return false;
		}

		//----------------------------------------------------------------------//
		// choose the closest corner to be the default attacking point of range attack
		//
		// generalized case for range-attack is coded below. Work for target with
		// size > 1x1 
		//----------------------------------------------------------------------//

		//-------------- define parameters --------------------//
		int adjWidth = targetWidth - 1; // adjusted width;
		int adjHeight = targetHeight - 1; // adjusted height
		int xOffset = 0, yOffset = 0; // the 1st(default) location for range attack
		int atEdge = 0; // i.e. the attacking point is at the corner or edge of the target
		// 1 for at the edge, 0 for at corner

		//----------------------------------------------------------------------//
		// determine initial xOffset
		//----------------------------------------------------------------------//
		if (startXLoc <= targetXLoc)
			xOffset = 0; // the left hand side of the target
		else if (startXLoc >= targetXLoc + adjWidth)
			xOffset = adjWidth; // the right hand side of the target
		else
		{
			xOffset = startXLoc - targetXLoc; // in the middle(vertical) of the target
			atEdge++;
		}

		//----------------------------------------------------------------------//
		// determine initial yOffset
		//----------------------------------------------------------------------//
		if (startYLoc <= targetYLoc)
			yOffset = 0; // the upper of the target
		else if (startYLoc >= targetYLoc + adjHeight)
			yOffset = adjHeight;
		else
		{
			yOffset = startYLoc - targetYLoc; // in the middle(horizontal) of the target
			atEdge++;
		}

		//----------------------------------------------------------------------//
		// checking whether it is possible to add bullet
		//----------------------------------------------------------------------//
		if (bullet_path_possible(startXLoc, startYLoc, attackerMobileType,
			    targetXLoc + xOffset, targetYLoc + yOffset, targetMobileType, bulletSpeed, bulletSpriteId))
		{
			resultXLoc = targetXLoc + xOffset;
			resultYLoc = targetYLoc + yOffset;
			return true;
		}

		int leftXOffset = 0, leftYOffset = 0;
		int rightXOffset = 0, rightYOffset = 0;

		if (atEdge > 0) // only check one edge of the target
		{
			if (xOffset == 0 || xOffset == adjWidth) // horizontal edge
			{
				leftYOffset = rightYOffset = 0;
				leftXOffset = 1;
				rightXOffset = -1;
			}
			else if (yOffset == 0 || yOffset == adjHeight) // vertical edge
			{
				leftXOffset = rightXOffset = 0;
				leftYOffset = 1;
				rightYOffset = -1;
			}
		}
		else // at the corner,	need to check two edges of the target
		{
			leftYOffset = rightXOffset = 0;
			leftXOffset = (xOffset == 0) ? 1 : -1;
			rightYOffset = (yOffset == 0) ? 1 : -1;
		}

		int leftX = xOffset;
		int leftY = yOffset;
		int rightX = xOffset;
		int rightY = yOffset;

		while (true)
		{
			bool end = false;

			//-------------------------------------------//
			// for the leftX, leftY
			//-------------------------------------------//
			leftX += leftXOffset;
			leftY += leftYOffset;
			if (leftX >= 0 && leftX < targetWidth && leftY >= 0 && leftY < targetHeight)
			{
				if (bullet_path_possible(startXLoc, startYLoc, attackerMobileType,
					    targetXLoc + leftX, targetYLoc + leftY, targetMobileType, bulletSpeed, bulletSpriteId))
				{
					resultXLoc = targetXLoc + leftX;
					resultYLoc = targetYLoc + leftY;
					return true;
				}
			}
			else
			{
				end = true;
			}

			//-------------------------------------------//
			// for the rightX, rightY
			//-------------------------------------------//
			rightX += rightXOffset;
			rightY += rightYOffset;
			if (rightX >= 0 && rightX < targetWidth && rightY >= 0 && rightY < targetHeight)
			{
				if (bullet_path_possible(startXLoc, startYLoc, attackerMobileType,
					    targetXLoc + rightX, targetYLoc + rightY, targetMobileType, bulletSpeed, bulletSpriteId))
				{
					resultXLoc = targetXLoc + rightX;
					resultYLoc = targetYLoc + rightY;
					return true;
				}
			}
			else
			{
				if (end) // all locations have been checked, all are blocked
					return false;
			}
		}
	}

	public bool bullet_path_possible(int startXLoc, int startYLoc, int attackerMobileType,
		int destXLoc, int destYLoc, int targetMobileType, int bulletSpeed, int bulletSpriteId)
	{
		if (attackerMobileType == UnitConstants.UNIT_AIR || targetMobileType == UnitConstants.UNIT_AIR)
			return true;

		//-------- skip the checking for projectile -----------//
		SpriteInfo spriteInfo = SpriteRes[bulletSpriteId];
		if (spriteInfo.sprite_sub_type == 'P')
			return true;

		//----------------------- define variables ---------------//

		int originX = startXLoc * InternalConstants.CellWidth;
		int originY = startYLoc * InternalConstants.CellHeight;
		int goX = destXLoc * InternalConstants.CellWidth;
		int goY = destYLoc * InternalConstants.CellHeight;

		int xStep = (goX - originX) / bulletSpeed;
		int yStep = (goY - originY) / bulletSpeed;

		int totalStep = Math.Max(1, Math.Max(Math.Abs(xStep), Math.Abs(yStep)));
		int curStep = 0;

		//------------------------------------------------------//
		// if the path of the bullet is blocked, return 0
		//------------------------------------------------------//
		int curX = originX + InternalConstants.CellWidth / 2;
		int curY = originY + InternalConstants.CellHeight / 2;

		while (curStep++ < totalStep)
		{
			curX += xStep;
			curY += yStep;

			int curXLoc = curX / InternalConstants.CellWidth;
			int curYLoc = curY / InternalConstants.CellHeight;

			if (curXLoc == startXLoc && curYLoc == startYLoc)
				continue;

			if (curXLoc == destXLoc && curYLoc == destYLoc)
				break; // is destination

			Location location = World.get_loc(curXLoc, curYLoc);

			//if(!locPtr->walkable(3) || locPtr->has_unit(UNIT_LAND) || locPtr->has_unit(UNIT_SEA))
			if (!location.walkable(3))
				return false;
		}

		return true;
	}
}