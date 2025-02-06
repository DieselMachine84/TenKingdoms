using System;

namespace TenKingdoms;

public static partial class Renderer
{
    private static void DrawPointOnMiniMap(Graphics graphics, int xLoc, int yLoc, int color)
    {
        if (MiniMapSize == GameConstants.MapSize)
        {
            graphics.DrawPoint(MiniMapX + xLoc, MiniMapY + yLoc, color);
        }
        else
        {
            if (MiniMapSize > GameConstants.MapSize)
            {
                graphics.DrawRect(MiniMapX + xLoc * MiniMapScale, MiniMapY + yLoc * MiniMapScale, MiniMapScale, MiniMapScale, color);
            }
            else
            {
                //TODO not supported yet
            }
        }
    }

    private static void DrawLineOnMiniMap(Graphics graphics, int x1Loc, int y1Loc, int x2Loc, int y2Loc, int color)
    {
        if (MiniMapSize == GameConstants.MapSize)
        {
            graphics.DrawLine(MiniMapX + x1Loc, MiniMapY + y1Loc, MiniMapX + x2Loc, MiniMapY + y2Loc, color);
        }
        else
        {
            if (MiniMapSize > GameConstants.MapSize)
            {
                if (x1Loc == x2Loc)
                {
                    graphics.DrawLine(MiniMapX + x1Loc * MiniMapScale, MiniMapY + y1Loc * MiniMapScale + 1,
                        MiniMapX + x2Loc * MiniMapScale, MiniMapY + y2Loc * MiniMapScale + 1, color);
                    graphics.DrawLine(MiniMapX + x1Loc * MiniMapScale + 1, MiniMapY + y1Loc * MiniMapScale + 1,
                        MiniMapX + x2Loc * MiniMapScale + 1, MiniMapY + y2Loc * MiniMapScale + 1, color);
                    
                    //TODO support MiniMapScale == 4
                }

                if (y1Loc == y2Loc)
                {
                    graphics.DrawLine(MiniMapX + x1Loc * MiniMapScale + 1, MiniMapY + y1Loc * MiniMapScale,
                        MiniMapX + x2Loc * MiniMapScale, MiniMapY + y2Loc * MiniMapScale, color);
                    graphics.DrawLine(MiniMapX + x1Loc * MiniMapScale + 1, MiniMapY + y1Loc * MiniMapScale + 1,
                        MiniMapX + x2Loc * MiniMapScale, MiniMapY + y2Loc * MiniMapScale + 1, color);
                    
                    //TODO support MiniMapScale == 4
                }

                if (x1Loc != x2Loc && y1Loc != y2Loc)
                {
                    //TODO support 3x3 line

                    graphics.DrawLine(MiniMapX + x1Loc * MiniMapScale, MiniMapY + y1Loc * MiniMapScale,
                        MiniMapX + x2Loc * MiniMapScale, MiniMapY + y2Loc * MiniMapScale, color);
                    graphics.DrawLine(MiniMapX + x1Loc * MiniMapScale + 1, MiniMapY + y1Loc * MiniMapScale,
                        MiniMapX + x2Loc * MiniMapScale + 1, MiniMapY + y2Loc * MiniMapScale, color);
                    graphics.DrawLine(MiniMapX + x1Loc * MiniMapScale, MiniMapY + y1Loc * MiniMapScale + 1,
                        MiniMapX + x2Loc * MiniMapScale, MiniMapY + y2Loc * MiniMapScale + 1, color);
                    graphics.DrawLine(MiniMapX + x1Loc * MiniMapScale + 1, MiniMapY + y1Loc * MiniMapScale + 1,
                        MiniMapX + x2Loc * MiniMapScale + 1, MiniMapY + y2Loc * MiniMapScale + 1, color);
                    
                    //TODO support MiniMapScale == 4
                }
            }
            else
            {
                //TODO not supported yet
            }
        }
    }
    
    private static void DrawRectOnMiniMap(Graphics graphics, int xLoc, int yLoc, int width, int height, int color)
    {
        if (MiniMapSize == GameConstants.MapSize)
        {
            graphics.DrawRect(MiniMapX + xLoc, MiniMapY + yLoc, width, height, color);
        }
        else
        {
            if (MiniMapSize > GameConstants.MapSize)
            {
                graphics.DrawRect(MiniMapX + xLoc * MiniMapScale, MiniMapY + yLoc * MiniMapScale,
                    width * MiniMapScale, height * MiniMapScale, color);
            }
            else
            {
                //TODO not supported yet
            }
        }
    }

    private static void DrawFrameOnMiniMap(Graphics graphics, int xLoc, int yLoc, int width, int height, int color)
    {
        if (MiniMapSize == GameConstants.MapSize)
        {
            graphics.DrawFrame(MiniMapX + xLoc, MiniMapY + yLoc, width, height, color);
        }
        else
        {
            if (MiniMapSize > GameConstants.MapSize)
            {
                graphics.DrawRect(MiniMapX + xLoc * MiniMapScale, MiniMapY + yLoc * MiniMapScale,
                    width * MiniMapScale, MiniMapScale, color);
                graphics.DrawRect(MiniMapX + xLoc * MiniMapScale, MiniMapY + (yLoc + height - 1) * MiniMapScale,
                    width * MiniMapScale, MiniMapScale, color);
                graphics.DrawRect(MiniMapX + xLoc * MiniMapScale, MiniMapY + yLoc * MiniMapScale,
                    MiniMapScale, height * MiniMapScale, color);
                graphics.DrawRect(MiniMapX + (xLoc + width - 1) * MiniMapScale, MiniMapY + yLoc * MiniMapScale,
                    MiniMapScale, height * MiniMapScale, color);
            }
            else
            {
                //TODO not supported yet
            }
        }
    }
    
    private static void DrawMiniMap(Graphics graphics)
    {
        //TODO Better support scaling
        graphics.SetClipRectangle(MiniMapX, MiniMapY, MiniMapSize, MiniMapSize);

        if (NeedFullRedraw)
        {
            Array.Clear(miniMapImage);
            for (int yLoc = 0; yLoc < GameConstants.MapSize; yLoc++)
            {
                for (int xLoc = 0; xLoc < GameConstants.MapSize; xLoc++)
                {
                    Location location = World.get_loc(xLoc, yLoc);
                    byte color = Colors.UNEXPLORED_COLOR;
                    if (location.explored())
                    {
                        if (location.sailable())
                            color = Colors.WATER_COLOR;

                        else if (location.has_hill())
                            color = Colors.V_BROWN;

                        else if (location.is_plant())
                            color = Colors.V_DARK_GREEN;

                        else
                            color = Colors.VGA_GRAY + 10;
                    }

                    if (MiniMapScale == 1)
                    {
                        miniMapImage[yLoc * MiniMapSize + xLoc] = color;
                    }
                    else
                    {
                        if (MiniMapScale == 2)
                        {
                            miniMapImage[yLoc * MiniMapScale * MiniMapSize + xLoc * MiniMapScale] = color;
                            miniMapImage[yLoc * MiniMapScale * MiniMapSize + xLoc * MiniMapScale + 1] = color;
                            miniMapImage[(yLoc * MiniMapScale + 1) * MiniMapSize + xLoc * MiniMapScale] = color;
                            miniMapImage[(yLoc * MiniMapScale + 1) * MiniMapSize + xLoc * MiniMapScale + 1] = color;
                        }
                    }
                }
            }

            graphics.CreateMiniMapTexture(miniMapImage, MiniMapSize, MiniMapSize);
            NeedFullRedraw = false;
        }

        graphics.DrawMiniMapGround(MiniMapX, MiniMapY, MiniMapSize, MiniMapSize);

        byte[] nationColorArray = NationArray.nation_color_array;
        byte[,] excitedColorArray = ColorRemap.excitedColorArray;
        int excitedColorCount = excitedColorArray.GetLength(1);
        
        //Draw shadows first
        byte shadowColor = Colors.VGA_GRAY;
        foreach (Town town in TownArray)
        {
            if (IsExplored(town.LocX1, town.LocX2, town.LocY1, town.LocY2))
            {
                DrawLineOnMiniMap(graphics, town.LocX1 + 1, town.LocY2 + 1, town.LocX2 + 1, town.LocY2 + 1, shadowColor);
                DrawLineOnMiniMap(graphics, town.LocX2 + 1, town.LocY1 + 1, town.LocX2 + 1, town.LocY2 + 1, shadowColor);
            }
        }

        //Draw shadows first
        foreach (Firm firm in FirmArray)
        {
            int x1Loc = firm.loc_x1;
            int x2Loc = firm.loc_x2;
            int y1Loc = firm.loc_y1;
            int y2Loc = firm.loc_y2;
            
            if (IsExplored(x1Loc, x2Loc, y1Loc, y2Loc))
            {
                //Monster lairs has the same size as villages but should look different on mini-map
                if (firm.firm_id == Firm.FIRM_MONSTER)
                {
                    x2Loc--;
                    y2Loc--;
                }

                DrawLineOnMiniMap(graphics, x1Loc + 1, y2Loc + 1, x2Loc + 1, y2Loc + 1, shadowColor);
                DrawLineOnMiniMap(graphics, x2Loc + 1, y1Loc + 1, x2Loc + 1, y2Loc + 1, shadowColor);
            }
        }

        foreach (Town town in TownArray)
        {
            byte nationColor = (town.LastBeingAttackedDate == default) || (Info.game_date - town.LastBeingAttackedDate).Days > 2
                ? nationColorArray[town.NationId]
                : excitedColorArray[ColorRemap.ColorSchemes[town.NationId], Sys.Instance.FrameNumber % excitedColorCount];

            if (IsExplored(town.LocX1, town.LocX2, town.LocY1, town.LocY2))
            {
                DrawRectOnMiniMap(graphics, town.LocX1, town.LocY1, town.LocX2 - town.LocX1 + 1, town.LocY2 - town.LocY1 + 1, nationColor);
            }
        }

        foreach (Firm firm in FirmArray)
        {
            byte nationColor = (firm.last_attacked_date == default) || (Info.game_date - firm.last_attacked_date).Days > 2
                ? nationColorArray[firm.nation_recno]
                : excitedColorArray[ColorRemap.ColorSchemes[firm.nation_recno], Sys.Instance.FrameNumber % excitedColorCount];

            int x1Loc = firm.loc_x1;
            int x2Loc = firm.loc_x2;
            int y1Loc = firm.loc_y1;
            int y2Loc = firm.loc_y2;
            
            if (IsExplored(x1Loc, x2Loc, y1Loc, y2Loc))
            {
                //Monster lairs has the same size as villages but should look different on mini-map
                if (firm.firm_id == Firm.FIRM_MONSTER)
                {
                    x2Loc--;
                    y2Loc--;
                }
                DrawRectOnMiniMap(graphics, x1Loc, y1Loc, x2Loc - x1Loc + 1, y2Loc - y1Loc + 1, nationColor);
            }
        }
        
        foreach (Site site in SiteArray)
        {
            if (IsExplored(site.map_x_loc, site.map_y_loc, site.map_x_loc, site.map_y_loc))
            {
                DrawRectOnMiniMap(graphics, site.map_x_loc - 1, site.map_y_loc - 1, 3, 3, Colors.SITE_COLOR);
            }
        }
        
        foreach (Unit unit in UnitArray)
        {
            if (!unit.is_visible() || unit.is_shealth())
                continue;
            
            byte nationColor = unit.cur_action != Sprite.SPRITE_ATTACK
                ? nationColorArray[unit.nation_recno]
                : excitedColorArray[ColorRemap.ColorSchemes[unit.nation_recno], Sys.Instance.FrameNumber % excitedColorCount];

            int xLoc = unit.cur_x_loc();
            int yLoc = unit.cur_y_loc();
            if (IsExplored(xLoc, xLoc, yLoc, yLoc))
            {
                int size = 2;
                if (unit.mobile_type != UnitConstants.UNIT_LAND)
                {
                    size = 3;
                    if (xLoc > 0)
                        xLoc--;
                    if (yLoc > 0)
                        yLoc--;
                }
                DrawRectOnMiniMap(graphics, xLoc, yLoc, size, size, nationColor);
            }
        }
        
        //Draw tornadoes

        //Draw war points
        
        DrawFrameOnMiniMap(graphics, topLeftX - 1, topLeftY - 1, ZoomMapLocWidth + 2, ZoomMapLocHeight + 2,
            Colors.VGA_YELLOW + screenSquareFrameCount);

        if (Sys.Instance.Speed != 0)
        {
            screenSquareFrameCount += screenSquareFrameStep;

            if (screenSquareFrameCount == 0) // color with smaller number is brighter
                screenSquareFrameStep = 1;

            if (screenSquareFrameCount == 6) // bi-directional color shift
                screenSquareFrameStep = -1;
        }
    }
}