using System;

namespace TenKingdoms;

public partial class Renderer
{
    private void DrawPointOnMiniMap(int xLoc, int yLoc, int color)
    {
        if (MiniMapSize == GameConstants.MapSize)
        {
            Graphics.DrawPoint(MiniMapX + xLoc, MiniMapY + yLoc, color);
        }
        else
        {
            if (MiniMapSize > GameConstants.MapSize)
            {
                Graphics.DrawRect(MiniMapX + xLoc * MiniMapScale, MiniMapY + yLoc * MiniMapScale, MiniMapScale, MiniMapScale, color);
            }
            else
            {
                //TODO not supported yet
            }
        }
    }

    private void DrawLineOnMiniMap(int x1Loc, int y1Loc, int x2Loc, int y2Loc, int color)
    {
        if (MiniMapSize == GameConstants.MapSize)
        {
            Graphics.DrawLine(MiniMapX + x1Loc, MiniMapY + y1Loc, MiniMapX + x2Loc, MiniMapY + y2Loc, color);
        }
        else
        {
            if (MiniMapSize > GameConstants.MapSize)
            {
                if (x1Loc == x2Loc)
                {
                    Graphics.DrawLine(MiniMapX + x1Loc * MiniMapScale, MiniMapY + y1Loc * MiniMapScale + 1,
                        MiniMapX + x2Loc * MiniMapScale, MiniMapY + y2Loc * MiniMapScale + 1, color);
                    Graphics.DrawLine(MiniMapX + x1Loc * MiniMapScale + 1, MiniMapY + y1Loc * MiniMapScale + 1,
                        MiniMapX + x2Loc * MiniMapScale + 1, MiniMapY + y2Loc * MiniMapScale + 1, color);
                    
                    //TODO support MiniMapScale == 4
                }

                if (y1Loc == y2Loc)
                {
                    Graphics.DrawLine(MiniMapX + x1Loc * MiniMapScale + 1, MiniMapY + y1Loc * MiniMapScale,
                        MiniMapX + x2Loc * MiniMapScale, MiniMapY + y2Loc * MiniMapScale, color);
                    Graphics.DrawLine(MiniMapX + x1Loc * MiniMapScale + 1, MiniMapY + y1Loc * MiniMapScale + 1,
                        MiniMapX + x2Loc * MiniMapScale, MiniMapY + y2Loc * MiniMapScale + 1, color);
                    
                    //TODO support MiniMapScale == 4
                }

                if (x1Loc != x2Loc && y1Loc != y2Loc)
                {
                    //TODO support 3x3 line

                    Graphics.DrawLine(MiniMapX + x1Loc * MiniMapScale, MiniMapY + y1Loc * MiniMapScale,
                        MiniMapX + x2Loc * MiniMapScale, MiniMapY + y2Loc * MiniMapScale, color);
                    Graphics.DrawLine(MiniMapX + x1Loc * MiniMapScale + 1, MiniMapY + y1Loc * MiniMapScale,
                        MiniMapX + x2Loc * MiniMapScale + 1, MiniMapY + y2Loc * MiniMapScale, color);
                    Graphics.DrawLine(MiniMapX + x1Loc * MiniMapScale, MiniMapY + y1Loc * MiniMapScale + 1,
                        MiniMapX + x2Loc * MiniMapScale, MiniMapY + y2Loc * MiniMapScale + 1, color);
                    Graphics.DrawLine(MiniMapX + x1Loc * MiniMapScale + 1, MiniMapY + y1Loc * MiniMapScale + 1,
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
    
    private void DrawRectOnMiniMap(int xLoc, int yLoc, int width, int height, int color)
    {
        if (MiniMapSize == GameConstants.MapSize)
        {
            Graphics.DrawRect(MiniMapX + xLoc, MiniMapY + yLoc, width, height, color);
        }
        else
        {
            if (MiniMapSize > GameConstants.MapSize)
            {
                Graphics.DrawRect(MiniMapX + xLoc * MiniMapScale, MiniMapY + yLoc * MiniMapScale,
                    width * MiniMapScale, height * MiniMapScale, color);
            }
            else
            {
                //TODO not supported yet
            }
        }
    }

    private void DrawFrameOnMiniMap(int xLoc, int yLoc, int width, int height, int color)
    {
        if (MiniMapSize == GameConstants.MapSize)
        {
            Graphics.DrawFrame(MiniMapX + xLoc, MiniMapY + yLoc, width, height, color);
        }
        else
        {
            if (MiniMapSize > GameConstants.MapSize)
            {
                Graphics.DrawRect(MiniMapX + xLoc * MiniMapScale, MiniMapY + yLoc * MiniMapScale,
                    width * MiniMapScale, MiniMapScale, color);
                Graphics.DrawRect(MiniMapX + xLoc * MiniMapScale, MiniMapY + (yLoc + height - 1) * MiniMapScale,
                    width * MiniMapScale, MiniMapScale, color);
                Graphics.DrawRect(MiniMapX + xLoc * MiniMapScale, MiniMapY + yLoc * MiniMapScale,
                    MiniMapScale, height * MiniMapScale, color);
                Graphics.DrawRect(MiniMapX + (xLoc + width - 1) * MiniMapScale, MiniMapY + yLoc * MiniMapScale,
                    MiniMapScale, height * MiniMapScale, color);
            }
            else
            {
                //TODO not supported yet
            }
        }
    }
    
    private void DrawMiniMap()
    {
        //TODO Better support scaling
        Graphics.SetClipRectangle(MiniMapX, MiniMapY, MiniMapSize, MiniMapSize);

        if (NeedFullRedraw)
        {
            Array.Clear(_miniMapImage);
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
                        _miniMapImage[yLoc * MiniMapSize + xLoc] = color;
                    }
                    else
                    {
                        if (MiniMapScale == 2)
                        {
                            _miniMapImage[yLoc * MiniMapScale * MiniMapSize + xLoc * MiniMapScale] = color;
                            _miniMapImage[yLoc * MiniMapScale * MiniMapSize + xLoc * MiniMapScale + 1] = color;
                            _miniMapImage[(yLoc * MiniMapScale + 1) * MiniMapSize + xLoc * MiniMapScale] = color;
                            _miniMapImage[(yLoc * MiniMapScale + 1) * MiniMapSize + xLoc * MiniMapScale + 1] = color;
                        }
                    }
                }
            }

            Graphics.CreateMiniMapTexture(_miniMapImage, MiniMapSize, MiniMapSize);
            NeedFullRedraw = false;
        }

        Graphics.DrawMiniMapGround(MiniMapX, MiniMapY, MiniMapSize, MiniMapSize);

        byte[] nationColorArray = NationArray.nation_color_array;
        byte[,] excitedColorArray = ColorRemap.excitedColorArray;
        int excitedColorCount = excitedColorArray.GetLength(1);
        
        //Draw shadows first
        byte shadowColor = Colors.VGA_GRAY;
        foreach (Town town in TownArray)
        {
            if (IsExplored(town.LocX1, town.LocX2, town.LocY1, town.LocY2))
            {
                DrawLineOnMiniMap(town.LocX1 + 1, town.LocY2 + 1, town.LocX2 + 1, town.LocY2 + 1, shadowColor);
                DrawLineOnMiniMap(town.LocX2 + 1, town.LocY1 + 1, town.LocX2 + 1, town.LocY2 + 1, shadowColor);
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

                DrawLineOnMiniMap(x1Loc + 1, y2Loc + 1, x2Loc + 1, y2Loc + 1, shadowColor);
                DrawLineOnMiniMap(x2Loc + 1, y1Loc + 1, x2Loc + 1, y2Loc + 1, shadowColor);
            }
        }

        foreach (Town town in TownArray)
        {
            byte nationColor = (town.LastBeingAttackedDate == default) || (Info.game_date - town.LastBeingAttackedDate).Days > 2
                ? nationColorArray[town.NationId]
                : excitedColorArray[ColorRemap.ColorSchemes[town.NationId], Sys.Instance.FrameNumber % excitedColorCount];

            if (IsExplored(town.LocX1, town.LocX2, town.LocY1, town.LocY2))
            {
                DrawRectOnMiniMap(town.LocX1, town.LocY1, town.LocX2 - town.LocX1 + 1, town.LocY2 - town.LocY1 + 1, nationColor);
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
                DrawRectOnMiniMap(x1Loc, y1Loc, x2Loc - x1Loc + 1, y2Loc - y1Loc + 1, nationColor);
            }
        }
        
        foreach (Site site in SiteArray)
        {
            if (IsExplored(site.LocX, site.LocX, site.LocY, site.LocY))
            {
                DrawRectOnMiniMap(site.LocX - 1, site.LocY - 1, 3, 3, Colors.SITE_COLOR);
            }
        }
        
        foreach (Unit unit in UnitArray)
        {
            if (!unit.is_visible() || unit.is_shealth())
                continue;

            int lineColor = Colors.V_BLACK;
            if (unit.mobile_type == UnitConstants.UNIT_SEA)
                lineColor = Colors.V_WHITE;

            //TODO replace with unit.selected_flag
            if (unit.sprite_recno == _selectedUnitId && (Config.show_unit_path & 2) != 0)
            {
                if (Config.show_ai_info || NationArray.player_recno == 0 || unit.is_nation(NationArray.player_recno))
                {
                    if (unit.PathNodes.Count > 0)
                    {
                        if (unit.cur_x_loc() != unit.go_x_loc() || unit.cur_y_loc() != unit.go_y_loc())
                        {
                            //TODO is this code executed?
                            DrawLineOnMiniMap(unit.go_x_loc(), unit.go_y_loc(), unit.next_x_loc(), unit.next_y_loc(), lineColor);
                        }

                        //TODO optimize drawing lines - join them
                        for (int i = unit.PathNodeIndex + 1; i < unit.PathNodes.Count; i++)
                        {
                            int resultNode1 = unit.PathNodes[i - 1];
                            World.GetLocXAndLocY(resultNode1, out int resultNode1LocX, out int resultNode1LocY);
                            int resultNode2 = unit.PathNodes[i];
                            World.GetLocXAndLocY(resultNode2, out int resultNode2LocX, out int resultNode2LocY);
                            DrawLineOnMiniMap(resultNode2LocX, resultNode2LocY, resultNode1LocX, resultNode1LocY, lineColor);
                        }
                    }

                    //TODO draw waypoints
                }
            }
            
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
                DrawRectOnMiniMap(xLoc, yLoc, size, size, nationColor);
            }
        }
        
        //Draw tornadoes

        //Draw war points
        
        DrawFrameOnMiniMap(_topLeftX - 1, _topLeftY - 1, MainViewWidthInCells + 2, MainViewHeightInCells + 2,
            Colors.VGA_YELLOW + _screenSquareFrameCount);

        if (Sys.Instance.Speed != 0)
        {
            _screenSquareFrameCount += _screenSquareFrameStep;

            if (_screenSquareFrameCount == 0) // color with smaller number is brighter
                _screenSquareFrameStep = 1;

            if (_screenSquareFrameCount == 6) // bi-directional color shift
                _screenSquareFrameStep = -1;
        }
    }
}