using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class UnitMonster : Unit
{
    public int MonsterActionMode { get; set; }
    
    public int MonsterId { get; set; }
    public int SoldierMonsterId { get; set; }

    private MonsterRes MonsterRes => Sys.Instance.MonsterRes;
    private SiteArray SiteArray => Sys.Instance.SiteArray;

    public static string[] MonsterKingNames { get; } =
    {
        "All High Deezboanz", "All High Rattus", "All High Broosken", "All High Haubudam", "All High Pfith", "All High Rokken",
        "All High Doink", "All High Wyrm", "All High Droog", "All High Ick", "All High Sauroid", "All High Karrotten", "All High Holgh"
    };

    public static string[] MonsterGeneralNames { get; } =
    {
        "Deezboanz Ordo", "Rattus Ordo", "Broosken Ordo", "Haubudam Ordo", "Pfith Ordo", "Rokken Ordo",
        "Doink Ordo", "Wyrm Ordo", "Droog Ordo", "Ick Ordo", "Sauroid Ordo", "Karrotten Ordo", "Holgh Ordo"
    };


    public UnitMonster()
    {
        MonsterActionMode = UnitConstants.MONSTER_ACTION_STOP;
    }

    public override string GetUnitName(bool withTitle = true)
    {
        return Rank switch
        {
            RANK_KING => MonsterKingNames[MonsterId - 1],
            RANK_GENERAL => MonsterGeneralNames[MonsterId - 1],
            _ => MonsterRes[MonsterId].Name
        };
    }

    public override void Die()
    {
        if (!IsVisible())
            return;

        //--- check if the location where the unit dies already has an item ---//

        int locX = CurLocX;
        int locY = CurLocY;
        Location location = World.GetLoc(locX, locY);

        if (!location.CanBuildSite())
        {
            bool found = false;

            for (int nearLocY = Math.Max(locY - 1, 0); nearLocY <= Math.Min(locY + 1, Config.MapSize - 1) && !found; nearLocY++)
            {
                for (int nearLocX = Math.Max(locX - 1, 0); nearLocX <= Math.Min(locX + 1, Config.MapSize - 1); nearLocX++)
                {
                    Location nearLocation = World.GetLoc(nearLocX, nearLocY);
                    if (nearLocation.CanBuildSite())
                    {
                        locX = nearLocX;
                        locY = nearLocY;
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
                return;
        }

        //--- when a general monster is killed, it leaves gold coins ---//

        if (NationId == 0 && MonsterId != 0) // to skip monster_res[ get_monster_id() ] error in test game 2
        {
            if (Rank == RANK_GENERAL)
            {
                MonsterInfo monsterInfo = MonsterRes[MonsterId];
                int goldAmount = 2 * MaxHitPoints * monsterInfo.Level * (100 + Misc.Random(30)) / 100;

                SiteArray.AddSite(locX, locY, Site.SITE_GOLD_COIN, goldAmount);
                SiteArray.OrderAIUnitsToGetSites(); // ask AI units to get the gold coins
            }

            //--- when a king monster is killed, it leaves a scroll of power ---//

            else if (Rank == RANK_KING)
            {
                int scrollRace = ChooseScrollRace();
                if (scrollRace != 0)
                {
                    SiteArray.AddSite(locX, locY, Site.SITE_SCROLL, scrollRace);
                    SiteArray.OrderAIUnitsToGetSites(); // ask AI units to get the scroll
                }
            }
        }

        //---------- add news ----------//

        if (Rank == RANK_KING)
            NewsArray.MonsterKingKilled(MonsterId, NextLocX, NextLocY);
    }

    public override void ProcessAI()
    {
        //----- when it is idle -------//

        if (!IsVisible() || !IsAIAllStop())
            return;

        if (Info.TotalDays % 15 == SpriteId % 15)
        {
            RandomAttack(); // randomly attacking targets
        }
    }
    
    private int RandomAttack()
    {
        const int ATTACK_SCAN_RANGE = 100;

        int curLocX = NextLocX, curLocY = NextLocY;
        int regionId = World.GetRegionId(curLocX, curLocY);

        for (int i = 2; i < ATTACK_SCAN_RANGE * ATTACK_SCAN_RANGE; i++)
        {
            Misc.cal_move_around_a_point(i, ATTACK_SCAN_RANGE, ATTACK_SCAN_RANGE, out int xOffset, out int yOffset);

            int locX = curLocX + xOffset;
            int locY = curLocY + yOffset;

            Misc.BoundLocation(ref locX, ref locY);

            Location location = World.GetLoc(locX, locY);

            if (location.RegionId != regionId)
                continue;

            bool rc = false;

            //----- if there is a unit on the location ------//

            if (location.HasUnit(UnitConstants.UNIT_LAND))
            {
                int unitId = location.UnitId(UnitConstants.UNIT_LAND);

                if (UnitArray.IsDeleted(unitId))
                    continue;

                rc = true;
            }

            //----- if there is a firm on the location ------//

            if (!rc && location.IsFirm())
            {
                int firmId = location.FirmId();

                if (FirmArray.IsDeleted(firmId))
                    continue;

                rc = true;
            }

            //----- if there is a town on the location ------//

            if (!rc && location.IsTown())
            {
                int townId = location.TownId();

                if (TownArray.IsDeleted(townId))
                    continue;

                rc = true;
            }

            //-------------------------------------//

            if (rc)
            {
                GroupOrderMonster(locX, locY, 1); // 1 - the action is attack
                return 1;
            }
        }

        return 0;
    }

    private int AssignToFirm()
    {
        int curLocX = NextLocX, curLocY = NextLocY;
        int regionId = World.GetRegionId(curLocX, curLocY);

        foreach (Firm firm in FirmArray.EnumerateRandom())
        {
            if (firm.RegionId != regionId)
                continue;

            if (firm.FirmType == Firm.FIRM_MONSTER)
            {
                bool canAssignMonster = FirmRes.GetBuild(firm.FirmBuildId).BuildCode == MonsterRes[MonsterId].FirmBuildCode;
                if (canAssignMonster)
                {
                    GroupOrderMonster(firm.LocX1, firm.LocY1, 2); // 2 - the action is assign
                    return 1;
                }
            }
        }

        return 0;
    }

    private void GroupOrderMonster(int destXLoc, int destYLoc, int actionType)
    {
        const int GROUP_ACTION_RANGE = 30; // only notify units within this range

        int curLocX = NextLocX, curLocY = NextLocY;
        int regionId = World.GetRegionId(curLocX, curLocY);

        List<int> orderedUnits = new List<int>();

        //----------------------------------------------//

        for (int i = 1; i < GROUP_ACTION_RANGE * GROUP_ACTION_RANGE; i++)
        {
            Misc.cal_move_around_a_point(i, GROUP_ACTION_RANGE, GROUP_ACTION_RANGE, out int xOffset, out int yOffset);

            int locX = curLocX + xOffset;
            int locY = curLocY + yOffset;

            Misc.BoundLocation(ref locX, ref locY);

            Location location = World.GetLoc(locX, locY);

            if (location.RegionId != regionId)
                continue;

            if (!location.HasUnit(UnitConstants.UNIT_LAND))
                continue;

            int unitId = location.UnitId(UnitConstants.UNIT_LAND);

            if (UnitArray.IsDeleted(unitId))
                continue;

            Unit unit = UnitArray[unitId];

            if (UnitRes[unit.UnitType].UnitClass != UnitConstants.UNIT_CLASS_MONSTER)
                continue;

            orderedUnits.Add(unitId);
        }

        if (orderedUnits.Count == 0)
            return;

        //---------------------------------------//

        if (actionType == 1) // attack
        {
            UnitArray.Attack(destXLoc, destYLoc, false, orderedUnits, InternalConstants.COMMAND_AI, 0);
        }
        else
        {
            UnitArray.Assign(destXLoc, destYLoc, false, orderedUnits, InternalConstants.COMMAND_AI);
        }
    }

    private int ChooseScrollRace()
    {
        const int SCROLL_SCAN_RANGE = 10;

        int curLocX = NextLocX, curLocY = NextLocY;
        int regionId = World.GetRegionId(curLocX, curLocY);
        int[] racesCount = new int[GameConstants.MAX_RACE];

        for (int i = 2; i < SCROLL_SCAN_RANGE * SCROLL_SCAN_RANGE; i++)
        {
            Misc.cal_move_around_a_point(i, SCROLL_SCAN_RANGE, SCROLL_SCAN_RANGE, out int xOffset, out int yOffset);

            int locX = curLocX + xOffset;
            int locY = curLocY + yOffset;

            Misc.BoundLocation(ref locX, ref locY);

            Location location = World.GetLoc(locX, locY);

            //----- if there is a unit on the location ------//

            if (location.HasUnit(UnitConstants.UNIT_LAND))
            {
                Unit unit = UnitArray[location.UnitId(UnitConstants.UNIT_LAND)];
                if (unit.RaceId > 0)
                {
                    racesCount[unit.RaceId - 1]++;
                }
            }
        }

        //------ find out which race is most populated in the area -----//

        int maxRaceCount = 0, bestRaceId = 0;

        for (int i = 0; i < GameConstants.MAX_RACE; i++)
        {
            if (racesCount[i] > maxRaceCount)
            {
                maxRaceCount = racesCount[i];
                bestRaceId = i + 1;
            }
        }

        return bestRaceId;
    }

    public override void DrawDetails(IRenderer renderer)
    {
        renderer.DrawMonsterDetails(this);
    }

    public override void HandleDetailsInput(IRenderer renderer)
    {
        renderer.HandleMonsterDetailsInput(this);
    }
}