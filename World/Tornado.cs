using System;

namespace TenKingdoms;

public class Tornado : Sprite
{
    private const int TORNADO_SPRITE_ID = 12;

    private double AttackDamage { get; set; }
    private int LifeTime { get; set; }
    private int DamageOffsetX { get; set; }
    private int DamageOffsetY { get; set; }

    public new void Init(int locX, int locY, int lifeTime)
    {
        base.Init(TORNADO_SPRITE_ID, locX, locY);

        AttackDamage = 2.0 / InternalConstants.ATTACK_SLOW_DOWN;
        LifeTime = lifeTime;

        SetDir(InternalConstants.DIR_N);
        CurAction = SPRITE_MOVE; // always moving
    }

    public override void PreProcess()
    {
        double angle = Misc.Random(32) / 16.0 * Math.PI;
        DamageOffsetX = (int)(InternalConstants.DAMAGE_POINT_RADIUS * Math.Cos(angle));
        DamageOffsetY = (int)(InternalConstants.DAMAGE_POINT_RADIUS * Math.Sin(angle));
        if (--LifeTime <= 0)
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
        CurX += (int)(speed * Math.Cos(windDir));
        CurY -= (int)(speed * Math.Sin(windDir));
        if (++CurFrame > CurSpriteMove().FrameCount)
            CurFrame = 1;

        HitTarget();
    }

    private void HitTarget()
    {
        //---- check if there is any unit in the target location ----//

        int damageLocX = (CurX + DamageOffsetX) >> InternalConstants.CellWidthShift;
        int damageLocY = (CurY + DamageOffsetY) >> InternalConstants.CellHeightShift;
        if (!Misc.IsLocationValid(damageLocX, damageLocY))
            return;

        Location location = World.GetLoc(damageLocX, damageLocY);

        Unit targetUnit;
        if (location.HasUnit(UnitConstants.UNIT_AIR))
        {
            targetUnit = UnitArray[location.UnitId(UnitConstants.UNIT_AIR)];
            targetUnit.HitPoints -= 2.0 * AttackDamage;
            if (targetUnit.HitPoints <= 0.0)
                targetUnit.HitPoints = 0.0;
        }

        if (location.HasUnit(UnitConstants.UNIT_LAND))
        {
            targetUnit = UnitArray[location.UnitId(UnitConstants.UNIT_LAND)];
            targetUnit.HitPoints -= AttackDamage;
            if (targetUnit.HitPoints <= 0.0)
                targetUnit.HitPoints = 0.0;
        }
        if (location.HasUnit(UnitConstants.UNIT_SEA))
        {
            targetUnit = UnitArray[location.UnitId(UnitConstants.UNIT_SEA)];
            targetUnit.HitPoints -= AttackDamage;
            if (targetUnit.HitPoints <= 0.0)
                targetUnit.HitPoints = 0.0;
        }

        if (location.IsFirm())
        {
            Firm firm = FirmArray[location.FirmId()];
            firm.HitPoints -= 2.0 * AttackDamage;
            if (firm.HitPoints <= 0.0)
            {
                firm.HitPoints = 0.0;

                SERes.sound(firm.LocCenterX, firm.LocCenterY, 1, 'F', firm.FirmType, "DIE");

                FirmArray.DeleteFirm(firm.FirmId);
            }
        }

        if (location.IsTown())
        {
            Town town = TownArray[location.TownId()];
            if (LifeTime % 30 == 0)
                town.KillTownPeople(0);
        }

        if (location.IsPlant())
        {
            location.RemovePlant();
            World.PlantCount--;
        }

        if (location.FireStrength() > 0)
        {
            location.SetFireStrength(1);
        }
    }
}