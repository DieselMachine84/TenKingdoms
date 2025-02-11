using System;

namespace TenKingdoms;

public class Site
{
    public const int SITE_RAW = 1;
    public const int SITE_SCROLL = 2;
    public const int SITE_GOLD_COIN = 3;

    public int site_recno;

    public int site_type; // SITE_RAW, SITE_ARTIFACT or SITE_SCROLL

    public int object_id; // id. of the object,
    public int reserve_qty; // for raw material only
    public bool has_mine; // whether there is a mine on this site

    public int map_x_loc;
    public int map_y_loc;

    public int region_id;
    
    private GodRes GodRes => Sys.Instance.GodRes;
    private World World => Sys.Instance.World;
    private NationArray NationArray => Sys.Instance.NationArray;
    private UnitArray UnitArray => Sys.Instance.UnitArray;
    private SiteArray SiteArray => Sys.Instance.SiteArray;
    private NewsArray NewsArray => Sys.Instance.NewsArray;

    public Site()
    {
    }

    public void Init(int siteType, int xLoc, int yLoc, int objectId, int reserveQty)
    {
        site_type = siteType;
        map_x_loc = xLoc;
        map_y_loc = yLoc;
        object_id = objectId;
        reserve_qty = reserveQty;

        //------- set world's location --------//

        Location location = World.get_loc(xLoc, yLoc);
        location.set_site(site_recno);
        region_id = location.region_id;
    }

    public void Deinit()
    {
        World.get_loc(map_x_loc, map_y_loc).remove_site();
    }

    public bool get_site_object(int unitRecno)
    {
        Unit unit = UnitArray[unitRecno];
        bool objectTaken = false;

        if (unit.nation_recno == 0)
            return false;

        //----- if this is a scroll site ------//

        if (site_type == SITE_SCROLL)
        {
            if (GodRes[object_id].race_id == unit.race_id)
            {
                GodRes[object_id].enable_know(unit.nation_recno);

                objectTaken = true;

                NewsArray.scroll_acquired(unit.nation_recno, GodRes[object_id].race_id);
            }
        }

        //------ if there are gold coins on this site -----//

        if (site_type == SITE_GOLD_COIN)
        {
            NationArray[unit.nation_recno].add_income(NationBase.INCOME_TREASURE, object_id);
            objectTaken = true;

            if (unit.nation_recno == NationArray.player_recno)
                NewsArray.monster_gold_acquired(object_id);
        }

        //---- if the object has been taken by the unit ----//

        if (objectTaken)
        {
            SiteArray.DeleteSite(this);
            return true;
        }

        return false;
    }

    public bool ai_get_site_object()
    {
        const int NOTIFY_GET_RANGE = 30; // only notify units within this range
        const int MAX_UNIT_TO_ORDER = 5;

        int xOffset = 0, yOffset = 0;
        int unitOrderedCount = 0;
        int siteRaceId = 0;

        if (site_type == SITE_SCROLL)
            siteRaceId = GodRes[object_id].race_id;

        for (int i = 2; i < NOTIFY_GET_RANGE * NOTIFY_GET_RANGE; i++)
        {
            Misc.cal_move_around_a_point(i, NOTIFY_GET_RANGE, NOTIFY_GET_RANGE, out xOffset, out yOffset);

            int xLoc = map_x_loc + xOffset;
            int yLoc = map_y_loc + yOffset;

            xLoc = Math.Max(0, xLoc);
            xLoc = Math.Min(GameConstants.MapSize - 1, xLoc);

            yLoc = Math.Max(0, yLoc);
            yLoc = Math.Min(GameConstants.MapSize - 1, yLoc);

            Location location = Sys.Instance.World.get_loc(xLoc, yLoc);

            if (!location.has_unit(UnitConstants.UNIT_LAND))
                continue;

            //------------------------------//

            int unitRecno = location.unit_recno(UnitConstants.UNIT_LAND);

            if (UnitArray.IsDeleted(unitRecno))
                continue;

            Unit unit = UnitArray[unitRecno];

            if (unit.race_id == 0 || !unit.ai_unit || unit.ai_action_id != 0)
                continue;

            if (siteRaceId != 0 && siteRaceId != unit.race_id)
                continue;

            unit.move_to(map_x_loc, map_y_loc);

            //--- if the unit is just standing next to the site ---//

            if (Math.Abs(map_x_loc - xLoc) <= 1 && Math.Abs(map_y_loc - yLoc) <= 1)
                return true;

            // order more than one unit to get the site at the same time
            if (++unitOrderedCount >= MAX_UNIT_TO_ORDER)
                return true;
        }

        return false;
    }
}