using System;

namespace TenKingdoms;

public class UnitExpCart : UnitWeapon
{
	public bool triggered;

	public UnitExpCart()
	{
	}

	public override bool ProcessDie()
	{
		if (triggered && CurFrame == 3)
		{
			int x1 = NextLocX;
			int x2 = x1;
			int y1 = NextLocY;
			int y2 = y1;
			x1 -= GameConstants.CHAIN_TRIGGER_RANGE;
			x2 += GameConstants.CHAIN_TRIGGER_RANGE;
			y1 -= GameConstants.CHAIN_TRIGGER_RANGE;
			y2 += GameConstants.CHAIN_TRIGGER_RANGE;
			if (x1 < 0)
				x1 = 0;
			if (x2 >= GameConstants.MapSize)
				x2 = GameConstants.MapSize - 1;
			if (y1 < 0)
				y1 = 0;
			if (y2 >= GameConstants.MapSize)
				y2 = GameConstants.MapSize - 1;

			for (int y = y1; y <= y2; ++y)
			{
				for (int x = x1; x <= x2; ++x)
				{
					Location location = World.GetLoc(x, y);
					if (location.HasUnit(UnitConstants.UNIT_LAND))
					{
						Unit unit = UnitArray[location.UnitId(UnitConstants.UNIT_LAND)];
						if (unit.UnitType == UnitConstants.UNIT_EXPLOSIVE_CART)
							((UnitExpCart)unit).trigger_explode();
					}
				}
			}
		}

		if (triggered && (CurFrame == 3 || CurFrame == 7))
		{
			int x1 = NextLocX;
			int x2 = x1;
			int y1 = NextLocY;
			int y2 = y1;
			x1 -= GameConstants.EXPLODE_RANGE;
			x2 += GameConstants.EXPLODE_RANGE;
			y1 -= GameConstants.EXPLODE_RANGE;
			y2 += GameConstants.EXPLODE_RANGE;

			if (x1 < 0)
				x1 = 0;
			if (x2 >= GameConstants.MapSize)
				x2 = GameConstants.MapSize - 1;
			if (y1 < 0)
				y1 = 0;
			if (y2 >= GameConstants.MapSize)
				y2 = GameConstants.MapSize - 1;

			if (CurFrame == 3)
			{
				for (int y = y1; y <= y2; ++y)
				{
					for (int x = x1; x <= x2; ++x)
					{
						Location location = World.GetLoc(x, y);
						if (location.HasUnit(UnitConstants.UNIT_LAND))
						{
							HitTarget(this, UnitArray[location.UnitId(UnitConstants.UNIT_LAND)],
								GameConstants.EXPLODE_DAMAGE, NationId);
						}
						else if (location.HasUnit(UnitConstants.UNIT_SEA))
						{
							HitTarget(this, UnitArray[location.UnitId(UnitConstants.UNIT_SEA)],
								GameConstants.EXPLODE_DAMAGE, NationId);
						}
						else if (location.IsWall())
						{
							HitWall(this, x, y, GameConstants.EXPLODE_DAMAGE, NationId);
						}
						else if (location.IsPlant())
						{
							location.RemovePlant();
							World.PlantCount--;
						}
						else
						{
							HitBuilding(this, x, y, GameConstants.EXPLODE_DAMAGE, NationId);
						}
					}
				}
			}
			else if (CurFrame == 7)
			{
				for (int y = y1; y <= y2; ++y)
				{
					for (int x = x1; x <= x2; ++x)
					{
						Location location = World.GetLoc(x, y);
						int fl = (Math.Abs(x - NextLocX) + Math.Abs(y - NextLocY)) * -30 + 80;
						if (location.CanSetFire() && location.FireStrength() < fl)
							location.SetFireStrength(fl);
						if (location.Flammability() > 0)
							location.SetFlammability(1); // such that the fire will be put out quickly
					}
				}
			}
		}

		return base.ProcessDie();
	}

	public void trigger_explode()
	{
		if (HitPoints > 0) // so dying cart cannot be triggered
		{
			triggered = true;
			HitPoints = 0;
		}
	}
}