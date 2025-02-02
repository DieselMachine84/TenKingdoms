using System;

namespace TenKingdoms;

public class Tornado : Sprite
{
    public const int TORNADO_SPRITE_ID = 12;

    public double attack_damage;
    public int life_time;
    public int dmg_offset_x;
    public int dmg_offset_y;

    public new void init(int startX, int startY, int lifeTime)
    {
        base.init(TORNADO_SPRITE_ID, startX, startY);

        attack_damage = 2.0 / InternalConstants.ATTACK_SLOW_DOWN;
        life_time = lifeTime;

        set_dir(InternalConstants.DIR_N);
        cur_action = SPRITE_MOVE; // always moving
        dmg_offset_x = 0;
        dmg_offset_y = 0;
    }

    public override void pre_process()
    {
        double angle = Misc.Random(32) / 16.0 * Math.PI;
        dmg_offset_x = Convert.ToInt32(InternalConstants.DAMAGE_POINT_RADIUS * Math.Cos(angle));
        dmg_offset_y = Convert.ToInt32(InternalConstants.DAMAGE_POINT_RADIUS * Math.Sin(angle));
        if (--life_time <= 0)
            cur_action = SPRITE_DIE;
    }

    public override void process_move()
    {
        int speed = Sys.Instance.Weather.wind_speed() / 6;
        int minSpeed = Sys.Instance.MagicWeather.wind_day > 0 ? 1 : 5;
        if (speed < minSpeed)
            speed = minSpeed;
        if (speed > 10)
            speed = 10;

        //TODO check this
        double windDir = Sys.Instance.Weather.wind_direct_rad() + (Misc.Random(31) - 15) * Math.PI / 180.0;
        cur_x += Convert.ToInt32(speed * Math.Cos(windDir));
        cur_y -= Convert.ToInt32(speed * Math.Sin(windDir));
        if (++cur_frame > cur_sprite_move().frame_count)
            cur_frame = 1;

        hit_target();
    }

    public void hit_target()
    {
        //---- check if there is any unit in the target location ----//

        int damageXLoc = damage_x_loc();
        int damageYLoc = damage_y_loc();
        if (damageXLoc < 0 || damageXLoc >= GameConstants.MapSize || damageYLoc < 0 || damageYLoc >= GameConstants.MapSize)
            return;

        Location location = World.get_loc(damageXLoc, damageYLoc);

        Unit targetUnit;
        if (location.has_unit(UnitConstants.UNIT_AIR))
        {
            targetUnit = UnitArray[location.unit_recno(UnitConstants.UNIT_AIR)];
            targetUnit.hit_points -= 2 * (int)attack_damage;
            if (targetUnit.hit_points <= 0)
                targetUnit.hit_points = 0;
        }

        if (location.has_unit(UnitConstants.UNIT_LAND))
        {
            targetUnit = UnitArray[location.unit_recno(UnitConstants.UNIT_LAND)];
            targetUnit.hit_points -= (int)attack_damage;
            if (targetUnit.hit_points <= 0)
                targetUnit.hit_points = 0;
        }
        else if (location.has_unit(UnitConstants.UNIT_SEA))
        {
            targetUnit = UnitArray[location.unit_recno(UnitConstants.UNIT_SEA)];
            targetUnit.hit_points -= (int)attack_damage;
            if (targetUnit.hit_points <= 0)
                targetUnit.hit_points = 0;
        }
        else
        {
            hit_building(); // pass to hit_building to check whether a building is in the location
        }

        hit_plant();
        hit_fire();
    }

    public void hit_building()
    {
        int damageXLoc = damage_x_loc();
        int damageYLoc = damage_y_loc();
        if (damageXLoc < 0 || damageXLoc >= GameConstants.MapSize || damageYLoc < 0 || damageYLoc >= GameConstants.MapSize)
            return;

        Location location = World.get_loc(damageXLoc, damageYLoc);

        if (location.is_firm())
        {
            Firm firm = FirmArray[location.firm_recno()];
            firm.hit_points -= attack_damage * 2;
            if (firm.hit_points <= 0)
            {
                firm.hit_points = 0.0;

                SERes.sound(firm.center_x, firm.center_y, 1, 'F', firm.firm_id, "DIE");

                FirmArray.DeleteFirm(location.firm_recno());
            }
        }

        if (location.is_town())
        {
            Town town = TownArray[location.town_recno()];
            if (life_time % 30 == 0)
                town.KillTownPeople(0);
        }
    }

    public void hit_plant()
    {
        int damageXLoc = damage_x_loc();
        int damageYLoc = damage_y_loc();
        if (damageXLoc < 0 || damageXLoc >= GameConstants.MapSize || damageYLoc < 0 || damageYLoc >= GameConstants.MapSize)
            return;

        Location location = World.get_loc(damageXLoc, damageYLoc);
        if (location.is_plant())
        {
            location.remove_plant(damageXLoc, damageYLoc);
            World.plant_count--;
        }
    }

    public void hit_fire()
    {
        int damageXLoc = damage_x_loc();
        int damageYLoc = damage_y_loc();
        if (damageXLoc < 0 || damageXLoc >= GameConstants.MapSize || damageYLoc < 0 || damageYLoc >= GameConstants.MapSize)
            return;

        Location location = World.get_loc(damageXLoc, damageYLoc);
        if (location.fire_str() > 0)
        {
            location.set_fire_str(1);
        }
    }

    public int damage_x_loc()
    {
        return (cur_x + dmg_offset_x) >> InternalConstants.CellWidthShift;
    }

    public int damage_y_loc()
    {
        return (cur_y + dmg_offset_y) >> InternalConstants.CellHeightShift;
    }
}