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
        base.Init(TORNADO_SPRITE_ID, startX, startY);

        attack_damage = 2.0 / InternalConstants.ATTACK_SLOW_DOWN;
        life_time = lifeTime;

        SetDir(InternalConstants.DIR_N);
        CurAction = SPRITE_MOVE; // always moving
        dmg_offset_x = 0;
        dmg_offset_y = 0;
    }

    public override void PreProcess()
    {
        double angle = Misc.Random(32) / 16.0 * Math.PI;
        dmg_offset_x = Convert.ToInt32(InternalConstants.DAMAGE_POINT_RADIUS * Math.Cos(angle));
        dmg_offset_y = Convert.ToInt32(InternalConstants.DAMAGE_POINT_RADIUS * Math.Sin(angle));
        if (--life_time <= 0)
            CurAction = SPRITE_DIE;
    }

    public override void ProcessMove()
    {
        int speed = Sys.Instance.Weather.wind_speed() / 6;
        int minSpeed = Sys.Instance.MagicWeather.wind_day > 0 ? 1 : 5;
        if (speed < minSpeed)
            speed = minSpeed;
        if (speed > 10)
            speed = 10;

        //TODO check this
        double windDir = Sys.Instance.Weather.wind_direct_rad() + (Misc.Random(31) - 15) * Math.PI / 180.0;
        CurX += Convert.ToInt32(speed * Math.Cos(windDir));
        CurY -= Convert.ToInt32(speed * Math.Sin(windDir));
        if (++CurFrame > CurSpriteMove().FrameCount)
            CurFrame = 1;

        hit_target();
    }

    public void hit_target()
    {
        //---- check if there is any unit in the target location ----//

        int damageXLoc = damage_x_loc();
        int damageYLoc = damage_y_loc();
        if (damageXLoc < 0 || damageXLoc >= GameConstants.MapSize || damageYLoc < 0 || damageYLoc >= GameConstants.MapSize)
            return;

        Location location = World.GetLoc(damageXLoc, damageYLoc);

        Unit targetUnit;
        if (location.HasUnit(UnitConstants.UNIT_AIR))
        {
            targetUnit = UnitArray[location.UnitId(UnitConstants.UNIT_AIR)];
            targetUnit.HitPoints -= 2 * (int)attack_damage;
            if (targetUnit.HitPoints <= 0)
                targetUnit.HitPoints = 0;
        }

        if (location.HasUnit(UnitConstants.UNIT_LAND))
        {
            targetUnit = UnitArray[location.UnitId(UnitConstants.UNIT_LAND)];
            targetUnit.HitPoints -= (int)attack_damage;
            if (targetUnit.HitPoints <= 0)
                targetUnit.HitPoints = 0;
        }
        else if (location.HasUnit(UnitConstants.UNIT_SEA))
        {
            targetUnit = UnitArray[location.UnitId(UnitConstants.UNIT_SEA)];
            targetUnit.HitPoints -= (int)attack_damage;
            if (targetUnit.HitPoints <= 0)
                targetUnit.HitPoints = 0;
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

        Location location = World.GetLoc(damageXLoc, damageYLoc);

        if (location.IsFirm())
        {
            Firm firm = FirmArray[location.FirmId()];
            firm.hit_points -= attack_damage * 2;
            if (firm.hit_points <= 0)
            {
                firm.hit_points = 0.0;

                SERes.sound(firm.center_x, firm.center_y, 1, 'F', firm.firm_id, "DIE");

                FirmArray.DeleteFirm(location.FirmId());
            }
        }

        if (location.IsTown())
        {
            Town town = TownArray[location.TownId()];
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

        Location location = World.GetLoc(damageXLoc, damageYLoc);
        if (location.IsPlant())
        {
            location.RemovePlant();
            World.PlantCount--;
        }
    }

    public void hit_fire()
    {
        int damageXLoc = damage_x_loc();
        int damageYLoc = damage_y_loc();
        if (damageXLoc < 0 || damageXLoc >= GameConstants.MapSize || damageYLoc < 0 || damageYLoc >= GameConstants.MapSize)
            return;

        Location location = World.GetLoc(damageXLoc, damageYLoc);
        if (location.FireStrength() > 0)
        {
            location.SetFireStrength(1);
        }
    }

    public int damage_x_loc()
    {
        return (CurX + dmg_offset_x) >> InternalConstants.CellWidthShift;
    }

    public int damage_y_loc()
    {
        return (CurY + dmg_offset_y) >> InternalConstants.CellHeightShift;
    }
}