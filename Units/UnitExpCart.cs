using System;

namespace TenKingdoms;

public class UnitExpCart : Unit
{
	public bool triggered;

	public UnitExpCart()
	{
	}

	public override bool process_die()
	{
		if (triggered && cur_frame == 3)
		{
			int x1 = next_x_loc();
			int x2 = x1;
			int y1 = next_y_loc();
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
					Location location = World.get_loc(x, y);
					if (location.has_unit(UnitConstants.UNIT_LAND))
					{
						Unit unit = UnitArray[location.unit_recno(UnitConstants.UNIT_LAND)];
						if (unit.unit_id == UnitConstants.UNIT_EXPLOSIVE_CART)
							((UnitExpCart)unit).trigger_explode();
					}
				}
			}
		}

		if (triggered && (cur_frame == 3 || cur_frame == 7))
		{
			int x1 = next_x_loc();
			int x2 = x1;
			int y1 = next_y_loc();
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

			if (cur_frame == 3)
			{
				for (int y = y1; y <= y2; ++y)
				{
					for (int x = x1; x <= x2; ++x)
					{
						Location location = World.get_loc(x, y);
						if (location.has_unit(UnitConstants.UNIT_LAND))
						{
							hit_target(this, UnitArray[location.unit_recno(UnitConstants.UNIT_LAND)],
								GameConstants.EXPLODE_DAMAGE, nation_recno);
						}
						else if (location.has_unit(UnitConstants.UNIT_SEA))
						{
							hit_target(this, UnitArray[location.unit_recno(UnitConstants.UNIT_SEA)],
								GameConstants.EXPLODE_DAMAGE, nation_recno);
						}
						else if (location.is_wall())
						{
							hit_wall(this, x, y, GameConstants.EXPLODE_DAMAGE, nation_recno);
						}
						else if (location.is_plant())
						{
							location.remove_plant();
							World.plant_count--;
						}
						else
						{
							hit_building(this, x, y, GameConstants.EXPLODE_DAMAGE, nation_recno);
						}
					}
				}
			}
			else if (cur_frame == 7)
			{
				for (int y = y1; y <= y2; ++y)
				{
					for (int x = x1; x <= x2; ++x)
					{
						Location location = World.get_loc(x, y);
						int fl = (Math.Abs(x - next_x_loc()) + Math.Abs(y - next_y_loc())) * -30 + 80;
						if (location.can_set_fire() && location.fire_str() < fl)
							location.set_fire_str(fl);
						if (location.fire_src() > 0)
							location.set_fire_src(1); // such that the fire will be put out quickly
					}
				}
			}
		}

		return base.process_die();
	}

	public void trigger_explode()
	{
		if (hit_points > 0) // so dying cart cannot be triggered
		{
			triggered = true;
			hit_points = 0;
		}
	}
}