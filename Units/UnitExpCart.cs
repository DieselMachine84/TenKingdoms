using System;
using System.IO;

namespace TenKingdoms;

public class UnitExpCart : UnitWeapon
{
	private bool _triggered;

	public UnitExpCart()
	{
	}

	public override bool ProcessDie()
	{
		if (_triggered && CurFrame == 3)
		{
			int locX1 = NextLocX;
			int locX2 = NextLocX;
			int locY1 = NextLocY;
			int locY2 = NextLocY;
			locX1 -= GameConstants.CHAIN_TRIGGER_RANGE;
			locX2 += GameConstants.CHAIN_TRIGGER_RANGE;
			locY1 -= GameConstants.CHAIN_TRIGGER_RANGE;
			locY2 += GameConstants.CHAIN_TRIGGER_RANGE;
			if (locX1 < 0)
				locX1 = 0;
			if (locX2 >= Config.MapSize)
				locX2 = Config.MapSize - 1;
			if (locY1 < 0)
				locY1 = 0;
			if (locY2 >= Config.MapSize)
				locY2 = Config.MapSize - 1;

			for (int locY = locY1; locY <= locY2; locY++)
			{
				for (int locX = locX1; locX <= locX2; locX++)
				{
					Location location = World.GetLoc(locX, locY);
					if (location.HasUnit(UnitConstants.UNIT_LAND))
					{
						Unit unit = UnitArray[location.UnitId(UnitConstants.UNIT_LAND)];
						if (unit.UnitType == UnitConstants.UNIT_EXPLOSIVE_CART)
							((UnitExpCart)unit).TriggerExplode();
					}
				}
			}
		}

		if (_triggered && (CurFrame == 3 || CurFrame == 7))
		{
			int locX1 = NextLocX;
			int locX2 = NextLocX;
			int locY1 = NextLocY;
			int locY2 = NextLocY;
			locX1 -= GameConstants.EXPLODE_RANGE;
			locX2 += GameConstants.EXPLODE_RANGE;
			locY1 -= GameConstants.EXPLODE_RANGE;
			locY2 += GameConstants.EXPLODE_RANGE;

			if (locX1 < 0)
				locX1 = 0;
			if (locX2 >= Config.MapSize)
				locX2 = Config.MapSize - 1;
			if (locY1 < 0)
				locY1 = 0;
			if (locY2 >= Config.MapSize)
				locY2 = Config.MapSize - 1;

			if (CurFrame == 3)
			{
				for (int locY = locY1; locY <= locY2; locY++)
				{
					for (int locX = locX1; locX <= locX2; locX++)
					{
						Location location = World.GetLoc(locX, locY);
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
							HitWall(this, locX, locY, GameConstants.EXPLODE_DAMAGE, NationId);
						}
						else if (location.IsPlant())
						{
							location.RemovePlant();
							World.PlantCount--;
						}
						else
						{
							HitBuilding(this, locX, locY, GameConstants.EXPLODE_DAMAGE, NationId);
						}
					}
				}
			}
			
			if (CurFrame == 7)
			{
				for (int locY = locY1; locY <= locY2; locY++)
				{
					for (int locX = locX1; locX <= locX2; locX++)
					{
						Location location = World.GetLoc(locX, locY);
						int fireStrength = (Math.Abs(locX - NextLocX) + Math.Abs(locY - NextLocY)) * -30 + 80;
						if (location.CanSetFire() && location.FireStrength() < fireStrength)
							location.SetFireStrength(fireStrength);
						if (location.Flammability() > 0)
							location.SetFlammability(1); // such that the fire will be put out quickly
					}
				}
			}
		}

		return base.ProcessDie();
	}

	public void TriggerExplode()
	{
		if (HitPoints > 0.0) // so dying cart cannot be triggered
		{
			_triggered = true;
			HitPoints = 0.0;
		}
	}
	
	#region SaveAndLoad

	public override void SaveTo(BinaryWriter writer)
	{
		base.SaveTo(writer);
		writer.Write(_triggered);
	}

	public override void LoadFrom(BinaryReader reader)
	{
		base.LoadFrom(reader);
		_triggered = reader.ReadBoolean();
	}
	
	#endregion
}