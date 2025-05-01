using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class UnitMonster : Unit
{
    public int monster_action_mode;

    private MonsterRes MonsterRes => Sys.Instance.MonsterRes;
    private SiteArray SiteArray => Sys.Instance.SiteArray;

    private string[] monster_name_king =
    {
        "All High Deezboanz", "All High Rattus", "All High Broosken", "All High Haubudam", "All High Pfith",
        "All High Rokken", "All High Doink", "All High Wyrm", "All High Droog", "All High Ick", "All High Sauroid",
        "All High Karrotten", "All High Holgh"
    };

    private string[] monster_name_general =
    {
        "Deezboanz Ordo", "Rattus Ordo", "Broosken Ordo", "Haubudam Ordo", "Pfith Ordo", "Rokken Ordo",
        "Doink Ordo", "Wyrm Ordo", "Droog Ordo", "Ick Ordo", "Sauroid Ordo", "Karrotten Ordo", "Holgh Ordo"
    };


    public UnitMonster()
    {
        monster_action_mode = UnitConstants.MONSTER_ACTION_STOP;
    }

    public override string unit_name(int withTitle = 1)
    {
        string str = String.Empty;

        switch (Rank)
        {
            case RANK_KING:
                str = monster_name_king[get_monster_id() - 1];
                break;

            case RANK_GENERAL:
                str = monster_name_general[get_monster_id() - 1];
                break;

            default:
                str = MonsterRes[get_monster_id()].name;
                break;
        }

        return str;
    }

    public void set_monster_action_mode(int monsterActionMode)
    {
        monster_action_mode = monsterActionMode;
    }

    public override void process_ai()
    {
        //----- when it is idle -------//

        if (!is_visible() || !is_ai_all_stop())
            return;

        if (Info.TotalDays % 15 == SpriteId % 15)
        {
            random_attack(); // randomly attacking targets

            /*switch(misc.random(2))
            {
                case 0:
                    random_attack();		// randomly attacking targets
                    break;
        
                case 1:
                    assign_to_firm();			// assign the monsters to other monster structures
                    break;
            }*/
        }
    }

    public override void die()
    {
        if (!is_visible())
            return;

        //--- check if the location where the unit dies already has an item ---//

        int xLoc = CurLocX;
        int yLoc = CurLocY;

        if (!World.GetLoc(xLoc, yLoc).CanBuildSite())
        {
            int txLoc, tyLoc;
            bool found = false;

            for (tyLoc = Math.Max(yLoc - 1, 0);
                 tyLoc <= Math.Min(yLoc + 1, GameConstants.MapSize - 1) && !found;
                 tyLoc++)
            {
                for (txLoc = Math.Max(xLoc - 1, 0); txLoc <= Math.Min(xLoc + 1, GameConstants.MapSize - 1); txLoc++)
                {
                    if (World.GetLoc(txLoc, tyLoc).CanBuildSite())
                    {
                        xLoc = txLoc;
                        yLoc = tyLoc;
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
                return;
        }

        //--- when a general monster is killed, it leaves gold coins ---//

        if (NationId == 0 && get_monster_id() != 0) // to skip monster_res[ get_monster_id() ] error in test game 2
        {
            MonsterInfo monsterInfo = MonsterRes[get_monster_id()];

            if (Rank == RANK_GENERAL)
            {
                int goldAmount = 2 * MaxHitPoints * monsterInfo.level * (100 + Misc.Random(30)) / 100;

                SiteArray.AddSite(xLoc, yLoc, Site.SITE_GOLD_COIN, goldAmount);
                SiteArray.OrderAIUnitsToGetSites(); // ask AI units to get the gold coins
            }

            //--- when a king monster is killed, it leaves a scroll of power ---//

            else if (Rank == RANK_KING)
            {
                king_leave_scroll();
            }
        }

        //---------- add news ----------//

        if (Rank == RANK_KING)
            NewsArray.monster_king_killed(get_monster_id(), NextLocX, NextLocY);
    }

    private int random_attack()
    {
        const int ATTACK_SCAN_RANGE = 100;

        int curXLoc = NextLocX, curYLoc = NextLocY;
        int regionId = World.GetRegionId(curXLoc, curYLoc);

        for (int i = 2; i < ATTACK_SCAN_RANGE * ATTACK_SCAN_RANGE; i++)
        {
            int xOffset, yOffset;
            Misc.cal_move_around_a_point(i, ATTACK_SCAN_RANGE, ATTACK_SCAN_RANGE, out xOffset, out yOffset);

            int xLoc = curXLoc + xOffset;
            int yLoc = curYLoc + yOffset;

            xLoc = Math.Max(0, xLoc);
            xLoc = Math.Min(GameConstants.MapSize - 1, xLoc);

            yLoc = Math.Max(0, yLoc);
            yLoc = Math.Min(GameConstants.MapSize - 1, yLoc);

            Location location = World.GetLoc(xLoc, yLoc);

            if (location.RegionId != regionId)
                continue;

            bool rc = false;

            //----- if there is a unit on the location ------//

            if (location.HasUnit(UnitConstants.UNIT_LAND))
            {
                int unitRecno = location.UnitId(UnitConstants.UNIT_LAND);

                if (UnitArray.IsDeleted(unitRecno))
                    continue;

                rc = true;
            }

            //----- if there is a firm on the location ------//

            if (!rc && location.IsFirm())
            {
                int firmRecno = location.FirmId();

                if (FirmArray.IsDeleted(firmRecno))
                    continue;

                rc = true;
            }

            //----- if there is a town on the location ------//

            if (!rc && location.IsTown())
            {
                int townRecno = location.TownId();

                if (TownArray.IsDeleted(townRecno))
                    continue;

                rc = true;
            }

            //-------------------------------------//

            if (rc)
            {
                group_order_monster(xLoc, yLoc, 1); // 1-the action is attack
                return 1;
            }
        }

        return 0;
    }

    private int assign_to_firm()
    {
        int curXLoc = NextLocX, curYLoc = NextLocY;
        int regionId = World.GetRegionId(curXLoc, curYLoc);

        foreach (Firm firm in FirmArray.EnumerateRandom())
        {
            if (firm.region_id != regionId)
                continue;

            if (firm.firm_id == Firm.FIRM_MONSTER)
            {
                if (((FirmMonster)firm).can_assign_monster(SpriteId))
                {
                    group_order_monster(firm.loc_x1, firm.loc_y1, 2); // 2-the action is assign
                    return 1;
                }
            }
        }

        return 0;
    }

    private void group_order_monster(int destXLoc, int destYLoc, int actionType)
    {
        const int GROUP_ACTION_RANGE = 30; // only notify units within this range

        int curXLoc = NextLocX, curYLoc = NextLocY;
        int regionId = World.GetRegionId(curXLoc, curYLoc);

        List<int> unitOrderedArray = new List<int>();
        int unitOrderedCount = 0;

        //----------------------------------------------//

        for (int i = 1; i < GROUP_ACTION_RANGE * GROUP_ACTION_RANGE; i++)
        {
            int xOffset, yOffset;
            Misc.cal_move_around_a_point(i, GROUP_ACTION_RANGE, GROUP_ACTION_RANGE, out xOffset, out yOffset);

            int xLoc = curXLoc + xOffset;
            int yLoc = curYLoc + yOffset;

            xLoc = Math.Max(0, xLoc);
            xLoc = Math.Min(GameConstants.MapSize - 1, xLoc);

            yLoc = Math.Max(0, yLoc);
            yLoc = Math.Min(GameConstants.MapSize - 1, yLoc);

            Location location = World.GetLoc(xLoc, yLoc);

            if (!location.HasUnit(UnitConstants.UNIT_LAND))
                continue;

            //------------------------------//

            int unitRecno = location.UnitId(UnitConstants.UNIT_LAND);

            if (UnitArray.IsDeleted(unitRecno))
                continue;

            Unit unit = UnitArray[unitRecno];

            if (UnitRes[unit.UnitType].unit_class != UnitConstants.UNIT_CLASS_MONSTER)
                continue;

            unitOrderedArray.Add(unitRecno);
        }

        if (unitOrderedArray.Count == 0)
            return;

        //---------------------------------------//

        if (actionType == 1) // attack
        {
            UnitArray.attack(destXLoc, destYLoc, false, unitOrderedArray, InternalConstants.COMMAND_AI, 0);
        }
        else
        {
            UnitArray.assign(destXLoc, destYLoc, false, InternalConstants.COMMAND_AI, unitOrderedArray);
        }
    }

    private void king_leave_scroll()
    {
        const int SCROLL_SCAN_RANGE = 10;

        int curXLoc = NextLocX, curYLoc = NextLocY;
        int regionId = World.GetRegionId(curXLoc, curYLoc);
        int[] raceCountArray = new int[GameConstants.MAX_RACE];

        for (int i = 2; i < SCROLL_SCAN_RANGE * SCROLL_SCAN_RANGE; i++)
        {
            int xOffset, yOffset;
            Misc.cal_move_around_a_point(i, SCROLL_SCAN_RANGE, SCROLL_SCAN_RANGE, out xOffset, out yOffset);

            int xLoc = curXLoc + xOffset;
            int yLoc = curYLoc + yOffset;

            xLoc = Math.Max(0, xLoc);
            xLoc = Math.Min(GameConstants.MapSize - 1, xLoc);

            yLoc = Math.Max(0, yLoc);
            yLoc = Math.Min(GameConstants.MapSize - 1, yLoc);

            Location location = World.GetLoc(xLoc, yLoc);

            //----- if there is a unit on the location ------//

            if (location.HasUnit(UnitConstants.UNIT_LAND))
            {
                Unit unit = UnitArray[location.UnitId(UnitConstants.UNIT_LAND)];
                if (unit.RaceId > 0)
                {
                    raceCountArray[unit.RaceId - 1]++;
                }
            }
        }

        //------ find out which race is most populated in the area -----//

        int maxRaceCount = 0, bestRaceId = 0;

        for (int i = 0; i < GameConstants.MAX_RACE; i++)
        {
            if (raceCountArray[i] > maxRaceCount)
            {
                maxRaceCount = raceCountArray[i];
                bestRaceId = i + 1;
            }
        }

        if (bestRaceId == 0)
            bestRaceId = ConfigAdv.GetRandomRace(); // if there is no human units nearby (perhaps just using weapons)

        //------ locate for space to add the scroll -------//

        const int ADD_SITE_RANGE = 5;

        for (int i = 1; i < ADD_SITE_RANGE * ADD_SITE_RANGE; i++)
        {
            int xOffset, yOffset;
            Misc.cal_move_around_a_point(i, ADD_SITE_RANGE, ADD_SITE_RANGE, out xOffset, out yOffset);

            int xLoc = curXLoc + xOffset;
            int yLoc = curYLoc + yOffset;

            xLoc = Math.Max(0, xLoc);
            xLoc = Math.Min(GameConstants.MapSize - 1, xLoc);

            yLoc = Math.Max(0, yLoc);
            yLoc = Math.Min(GameConstants.MapSize - 1, yLoc);

            Location location = World.GetLoc(xLoc, yLoc);

            if (location.CanBuildSite() && location.RegionId == regionId)
            {
                int scrollGodId = bestRaceId;

                SiteArray.AddSite(xLoc, yLoc, Site.SITE_SCROLL, scrollGodId);
                SiteArray.OrderAIUnitsToGetSites(); // ask AI units to get the scroll
                break;
            }
        }
    }
}