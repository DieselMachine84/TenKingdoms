using System;

namespace TenKingdoms;

public partial class Renderer
{
    private void DrawPointOnMiniMap(int locX, int locY, int color)
    {
        if (MiniMapSize == GameConstants.MapSize)
        {
            Graphics.DrawPoint(MiniMapX + locX, MiniMapY + locY, color);
        }
        else
        {
            if (MiniMapSize > GameConstants.MapSize)
            {
                Graphics.DrawRect(MiniMapX + locX * MiniMapScale, MiniMapY + locY * MiniMapScale, MiniMapScale, MiniMapScale, color);
            }
            else
            {
                //TODO not supported yet
            }
        }
    }

    private void DrawLineOnMiniMap(int locX1, int locY1, int locX2, int locY2, int color)
    {
        if (MiniMapSize == GameConstants.MapSize)
        {
            Graphics.DrawLine(MiniMapX + locX1, MiniMapY + locY1, MiniMapX + locX2, MiniMapY + locY2, color);
        }
        else
        {
            if (MiniMapSize > GameConstants.MapSize)
            {
                if (locX1 == locX2)
                {
                    Graphics.DrawLine(MiniMapX + locX1 * MiniMapScale, MiniMapY + locY1 * MiniMapScale + 1,
                        MiniMapX + locX2 * MiniMapScale, MiniMapY + locY2 * MiniMapScale + 1, color);
                    Graphics.DrawLine(MiniMapX + locX1 * MiniMapScale + 1, MiniMapY + locY1 * MiniMapScale + 1,
                        MiniMapX + locX2 * MiniMapScale + 1, MiniMapY + locY2 * MiniMapScale + 1, color);
                    
                    //TODO support MiniMapScale == 4
                }

                if (locY1 == locY2)
                {
                    Graphics.DrawLine(MiniMapX + locX1 * MiniMapScale + 1, MiniMapY + locY1 * MiniMapScale,
                        MiniMapX + locX2 * MiniMapScale, MiniMapY + locY2 * MiniMapScale, color);
                    Graphics.DrawLine(MiniMapX + locX1 * MiniMapScale + 1, MiniMapY + locY1 * MiniMapScale + 1,
                        MiniMapX + locX2 * MiniMapScale, MiniMapY + locY2 * MiniMapScale + 1, color);
                    
                    //TODO support MiniMapScale == 4
                }

                if (locX1 != locX2 && locY1 != locY2)
                {
                    //TODO support 3x3 line

                    Graphics.DrawLine(MiniMapX + locX1 * MiniMapScale, MiniMapY + locY1 * MiniMapScale,
                        MiniMapX + locX2 * MiniMapScale, MiniMapY + locY2 * MiniMapScale, color);
                    Graphics.DrawLine(MiniMapX + locX1 * MiniMapScale + 1, MiniMapY + locY1 * MiniMapScale,
                        MiniMapX + locX2 * MiniMapScale + 1, MiniMapY + locY2 * MiniMapScale, color);
                    Graphics.DrawLine(MiniMapX + locX1 * MiniMapScale, MiniMapY + locY1 * MiniMapScale + 1,
                        MiniMapX + locX2 * MiniMapScale, MiniMapY + locY2 * MiniMapScale + 1, color);
                    Graphics.DrawLine(MiniMapX + locX1 * MiniMapScale + 1, MiniMapY + locY1 * MiniMapScale + 1,
                        MiniMapX + locX2 * MiniMapScale + 1, MiniMapY + locY2 * MiniMapScale + 1, color);
                    
                    //TODO support MiniMapScale == 4
                }
            }
            else
            {
                //TODO not supported yet
            }
        }
    }
    
    private void DrawRectOnMiniMap(int locX, int locY, int width, int height, int color)
    {
        if (MiniMapSize == GameConstants.MapSize)
        {
            Graphics.DrawRect(MiniMapX + locX, MiniMapY + locY, width, height, color);
        }
        else
        {
            if (MiniMapSize > GameConstants.MapSize)
            {
                Graphics.DrawRect(MiniMapX + locX * MiniMapScale, MiniMapY + locY * MiniMapScale,
                    width * MiniMapScale, height * MiniMapScale, color);
            }
            else
            {
                //TODO not supported yet
            }
        }
    }

    private void DrawFrameOnMiniMap(int locX, int locY, int width, int height, int color)
    {
        if (MiniMapSize == GameConstants.MapSize)
        {
            Graphics.DrawFrame(MiniMapX + locX, MiniMapY + locY, width, height, color);
        }
        else
        {
            if (MiniMapSize > GameConstants.MapSize)
            {
                Graphics.DrawRect(MiniMapX + locX * MiniMapScale, MiniMapY + locY * MiniMapScale,
                    width * MiniMapScale, MiniMapScale, color);
                Graphics.DrawRect(MiniMapX + locX * MiniMapScale, MiniMapY + (locY + height - 1) * MiniMapScale,
                    width * MiniMapScale, MiniMapScale, color);
                Graphics.DrawRect(MiniMapX + locX * MiniMapScale, MiniMapY + locY * MiniMapScale,
                    MiniMapScale, height * MiniMapScale, color);
                Graphics.DrawRect(MiniMapX + (locX + width - 1) * MiniMapScale, MiniMapY + locY * MiniMapScale,
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
            for (int locY = 0; locY < GameConstants.MapSize; locY++)
            {
                for (int locX = 0; locX < GameConstants.MapSize; locX++)
                {
                    Location location = World.GetLoc(locX, locY);
                    byte color = Colors.UNEXPLORED_COLOR;
                    if (location.IsExplored())
                    {
                        if (location.Sailable())
                            color = Colors.WATER_COLOR;

                        else if (location.HasHill())
                            color = Colors.V_BROWN;

                        else if (location.IsPlant())
                            color = Colors.V_DARK_GREEN;

                        else
                            color = Colors.VGA_GRAY + 10;
                    }

                    if (MiniMapScale == 1)
                    {
                        _miniMapImage[locY * MiniMapSize + locX] = color;
                    }
                    else
                    {
                        if (MiniMapScale == 2)
                        {
                            _miniMapImage[locY * MiniMapScale * MiniMapSize + locX * MiniMapScale] = color;
                            _miniMapImage[locY * MiniMapScale * MiniMapSize + locX * MiniMapScale + 1] = color;
                            _miniMapImage[(locY * MiniMapScale + 1) * MiniMapSize + locX * MiniMapScale] = color;
                            _miniMapImage[(locY * MiniMapScale + 1) * MiniMapSize + locX * MiniMapScale + 1] = color;
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
            int locX1 = firm.loc_x1;
            int locX2 = firm.loc_x2;
            int locY1 = firm.loc_y1;
            int locY2 = firm.loc_y2;
            
            if (IsExplored(locX1, locX2, locY1, locY2))
            {
                //Monster lairs has the same size as villages but should look different on mini-map
                if (firm.firm_id == Firm.FIRM_MONSTER)
                {
                    locX2--;
                    locY2--;
                }

                DrawLineOnMiniMap(locX1 + 1, locY2 + 1, locX2 + 1, locY2 + 1, shadowColor);
                DrawLineOnMiniMap(locX2 + 1, locY1 + 1, locX2 + 1, locY2 + 1, shadowColor);
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

            int locX1 = firm.loc_x1;
            int locX2 = firm.loc_x2;
            int locY1 = firm.loc_y1;
            int locY2 = firm.loc_y2;
            
            if (IsExplored(locX1, locX2, locY1, locY2))
            {
                //Monster lairs has the same size as villages but should look different on mini-map
                if (firm.firm_id == Firm.FIRM_MONSTER)
                {
                    locX2--;
                    locY2--;
                }
                DrawRectOnMiniMap(locX1, locY1, locX2 - locX1 + 1, locY2 - locY1 + 1, nationColor);
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
            if (!unit.is_visible() || unit.IsStealth())
                continue;

            int lineColor = Colors.V_BLACK;
            if (unit.MobileType == UnitConstants.UNIT_SEA)
                lineColor = Colors.V_WHITE;

            //TODO replace with unit.selected_flag
            if (unit.SelectedFlag && (Config.show_unit_path & 2) != 0)
            {
                if (Config.show_ai_info || NationArray.player_recno == 0 || unit.is_nation(NationArray.player_recno))
                {
                    if (unit.PathNodes.Count > 0)
                    {
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
            
            byte nationColor = unit.CurAction != Sprite.SPRITE_ATTACK
                ? nationColorArray[unit.NationId]
                : excitedColorArray[ColorRemap.ColorSchemes[unit.NationId], Sys.Instance.FrameNumber % excitedColorCount];

            int locX = unit.CurLocX;
            int locY = unit.CurLocY;
            if (IsExplored(locX, locX, locY, locY))
            {
                int size = 2;
                if (unit.MobileType != UnitConstants.UNIT_LAND)
                {
                    size = 3;
                    if (locX > 0)
                        locX--;
                    if (locY > 0)
                        locY--;
                }
                DrawRectOnMiniMap(locX, locY, size, size, nationColor);
            }
        }
        
        //Draw tornadoes

        //Draw war points
        
        DrawFrameOnMiniMap(_topLeftLocX - 1, _topLeftLocY - 1, MainViewWidthInCells + 2, MainViewHeightInCells + 2,
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