using System;

namespace TenKingdoms;

public class UnitVehicle : Unit
{
    public int solider_hit_points; // the original hit points of the solider before it gets on the vehicle
    public int vehicle_hit_points; // the original hit points of the vehicle before the soldiers gets on it

    public new void set_combat_level(int combatLevel)
    {
        skill.combat_level = combatLevel;

        UnitInfo unitInfo = UnitRes[unit_id];

        max_hit_points = UnitRes[unitInfo.vehicle_id].hit_points +
                         UnitRes[unitInfo.solider_id].hit_points * combatLevel / 100;

        hit_points = Math.Min(hit_points, max_hit_points);
    }

    public void dismount()
    {
        UnitInfo unitInfo = UnitRes[unit_id];

        SpriteInfo soliderSpriteInfo = SpriteRes[UnitRes[unitInfo.solider_id].sprite_id];

        //--- calc the hit points of the solider and the vehicle after deforming ---//

        double soliderHitPoints = (double)hit_points * solider_hit_points / (solider_hit_points + vehicle_hit_points);
        double vehicleHitPoints = (double)hit_points * vehicle_hit_points / (solider_hit_points + vehicle_hit_points);

        soliderHitPoints = Math.Max(1.0, soliderHitPoints);
        vehicleHitPoints = Math.Max(1.0, vehicleHitPoints);

        //-------- add the solider unit ---------//

        //---- look for an empty location for the unit to stand ----//

        int xLoc = next_x_loc();
        int yLoc = next_y_loc();

        if (!World.locate_space(ref xLoc, ref yLoc,
                xLoc + sprite_info.loc_width - 1, yLoc + sprite_info.loc_height - 1,
                soliderSpriteInfo.loc_width, soliderSpriteInfo.loc_height))
        {
            return;
        }

        Unit unit = UnitArray.AddUnit(unitInfo.solider_id, nation_recno, rank_id, loyalty, xLoc, yLoc);

        unit.skill = skill;
        unit.set_combat_level(skill.combat_level);
        unit.hit_points = (int)soliderHitPoints;

        //-------- delete current unit ----------//

        int curXLoc = next_x_loc(), curYLoc = next_y_loc();

        UnitArray.DeleteUnit(this); // delete the vehicle (e.g. horse)

        //------- add the vehicle unit ----------//

        unit = UnitArray.AddUnit(unitInfo.vehicle_id, 0, 0, 0, curXLoc, curYLoc);
        unit.hit_points = (int)vehicleHitPoints;
    }
}