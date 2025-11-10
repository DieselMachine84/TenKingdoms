using System;

namespace TenKingdoms;

public class Site : IIdObject, IDisplayable
{
    public const int SITE_RAW = 1;
    public const int SITE_SCROLL = 2;
    public const int SITE_GOLD_COIN = 3;

    public int SiteId { get; private set; }
    public int SiteType { get; private set; } // SITE_RAW, SITE_SCROLL or SITE_GOLD_COIN
    public int ObjectId { get; private set; }
    public int LocX { get; private set; }
    public int LocY { get; private set; }
    public int RegionId { get; private set; }
    public int DrawY2 { get; set; }

    public int ReserveQty { get; set; } // for raw material only
    public bool HasMine { get; set; } // whether there is a mine on this site
    
    private GodRes GodRes => Sys.Instance.GodRes;
    private World World => Sys.Instance.World;
    private NationArray NationArray => Sys.Instance.NationArray;
    private UnitArray UnitArray => Sys.Instance.UnitArray;
    private SiteArray SiteArray => Sys.Instance.SiteArray;
    private NewsArray NewsArray => Sys.Instance.NewsArray;

    public Site()
    {
    }

    void IIdObject.SetId(int id)
    {
        SiteId = id;
    }

    public void Init(int siteType, int objectId, int locX, int locY, int reserveQty)
    {
        SiteType = siteType;
        ObjectId = objectId;
        LocX = locX;
        LocY = locY;
        ReserveQty = reserveQty;

        Location location = World.GetLoc(locX, locY);
        location.SetSite(SiteId);
        RegionId = location.RegionId;
    }

    public void Deinit()
    {
        World.GetLoc(LocX, LocY).RemoveSite();
    }

    public bool GetByUnit(int unitId)
    {
        Unit unit = UnitArray[unitId];
        if (unit.NationId == 0)
            return false;

        bool objectTaken = false;

        if (SiteType == SITE_SCROLL)
        {
            if (GodRes[ObjectId].race_id == unit.RaceId)
            {
                GodRes[ObjectId].enable_know(unit.NationId);
                objectTaken = true;
                NewsArray.scroll_acquired(unit.NationId, GodRes[ObjectId].race_id);
            }
        }

        if (SiteType == SITE_GOLD_COIN)
        {
            NationArray[unit.NationId].add_income(NationBase.INCOME_TREASURE, ObjectId);
            objectTaken = true;

            if (unit.NationId == NationArray.player_recno)
                NewsArray.monster_gold_acquired(ObjectId);
        }

        if (objectTaken)
        {
            SiteArray.DeleteSite(this);
            return true;
        }

        return false;
    }

    public bool OrderAIUnitsToGetThisSite()
    {
        int unitOrderedCount = 0;
        int siteRaceId = 0;

        if (SiteType == SITE_SCROLL)
            siteRaceId = GodRes[ObjectId].race_id;

        for (int i = 2; i < InternalConstants.GET_SITE_RANGE * InternalConstants.GET_SITE_RANGE; i++)
        {
            Misc.cal_move_around_a_point(i, InternalConstants.GET_SITE_RANGE, InternalConstants.GET_SITE_RANGE, out int xOffset, out int yOffset);

            int locX = LocX + xOffset;
            int locY = LocY + yOffset;

            locX = Math.Max(0, locX);
            locX = Math.Min(GameConstants.MapSize - 1, locX);

            locY = Math.Max(0, locY);
            locY = Math.Min(GameConstants.MapSize - 1, locY);

            Location location = Sys.Instance.World.GetLoc(locX, locY);
            if (!location.HasUnit(UnitConstants.UNIT_LAND))
                continue;

            int unitId = location.UnitId(UnitConstants.UNIT_LAND);

            if (UnitArray.IsDeleted(unitId))
                continue;

            Unit unit = UnitArray[unitId];

            if (unit.RaceId == 0 || !unit.AIUnit || unit.AIActionId != 0)
                continue;

            if (siteRaceId != 0 && siteRaceId != unit.RaceId)
                continue;

            unit.MoveTo(LocX, LocY);

            //--- if the unit is just standing next to the site ---//

            if (Math.Abs(LocX - locX) <= 1 && Math.Abs(LocY - locY) <= 1)
                return true;

            // order more than one unit to get the site at the same time
            if (++unitOrderedCount >= InternalConstants.MAX_UNIT_TO_GET_SITE)
                return true;
        }

        return false;
    }
    
    public void Draw(IRenderer renderer, int layer)
    {
        renderer.DrawSite(this, layer);
    }
}