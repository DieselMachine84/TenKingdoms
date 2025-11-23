using System;

namespace TenKingdoms;

public partial class Renderer
{
    private void DrawMiniMap(bool nextFrame)
    {
        Graphics.SetClipRectangle(MiniMapX, MiniMapY, MiniMapSize, MiniMapSize);

        if (NeedFullRedraw)
        {
            //TODO support different minimap modes
            //TODO draw fire
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

                    if (GameConstants.MapSize == 100)
                    {
                        for (int y = locY * 4; y < locY * 4 + 4; y++)
                        {
                            for (int x = locX * 4; x < locX * 4 + 4; x++)
                            {
                                _miniMapImage[y * MiniMapSize + x] = color;
                            }
                        }
                    }

                    if (GameConstants.MapSize == 200)
                    {
                        _miniMapImage[locY * 2 * MiniMapSize + locX * 2] = color;
                        _miniMapImage[locY * 2 * MiniMapSize + locX * 2 + 1] = color;
                        _miniMapImage[(locY * 2 + 1) * MiniMapSize + locX * 2] = color;
                        _miniMapImage[(locY * 2 + 1) * MiniMapSize + locX * 2 + 1] = color;
                    }

                    if (GameConstants.MapSize == 300)
                    {
                        _miniMapImage[locY * 4 / 3 * MiniMapSize + locX * 4 / 3] = color;
                        if ((locX * 4 / 3) % 4 == 2)
                            _miniMapImage[locY * 4 / 3 * MiniMapSize + locX * 4 / 3 + 1] = color;
                        if ((locY * 4 / 3) % 4 == 2)
                            _miniMapImage[(locY * 4 / 3 + 1) * MiniMapSize + locX * 4 / 3] = color;
                        if ((locX * 4 / 3) % 4 == 2 && (locY * 4 / 3) % 4 == 2)
                            _miniMapImage[(locY * 4 / 3 + 1) * MiniMapSize + locX * 4 / 3 + 1] = color;
                    }
                    
                    if (GameConstants.MapSize == 400)
                    {
                        _miniMapImage[locY * MiniMapSize + locX] = color;
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
        foreach (Town town in TownArray)
        {
            if (IsExplored(town.LocX1, town.LocY1, town.LocX2, town.LocY2))
            {
                DrawShadowOnMiniMap(town.LocX1, town.LocY1, town.LocX2, town.LocY2);
            }
        }

        //Draw shadows first
        foreach (Firm firm in FirmArray)
        {
            int locX1 = firm.LocX1;
            int locX2 = firm.LocX2;
            int locY1 = firm.LocY1;
            int locY2 = firm.LocY2;
            
            if (IsExplored(locX1, locY1, locX2, locY2))
            {
                //Monster lairs has the same size as villages but should look different on mini-map
                if (firm.FirmType == Firm.FIRM_MONSTER)
                {
                    locX2--;
                    locY2--;
                }

                DrawShadowOnMiniMap(locX1, locY1, locX2, locY2);
            }
        }

        foreach (Town town in TownArray)
        {
            byte nationColor = (town.LastBeingAttackedDate == default) || (Info.game_date - town.LastBeingAttackedDate).Days > 2
                ? nationColorArray[town.NationId]
                : excitedColorArray[ColorRemap.ColorSchemes[town.NationId], Sys.Instance.FrameNumber % excitedColorCount];

            if (IsExplored(town.LocX1, town.LocY1, town.LocX2, town.LocY2))
            {
                DrawRectOnMiniMap(town.LocX1, town.LocY1, town.LocX2 - town.LocX1 + 1, town.LocY2 - town.LocY1 + 1, nationColor);
            }
        }

        foreach (Firm firm in FirmArray)
        {
            byte nationColor = (firm.LastAttackedDate == default) || (Info.game_date - firm.LastAttackedDate).Days > 2
                ? nationColorArray[firm.NationId]
                : excitedColorArray[ColorRemap.ColorSchemes[firm.NationId], Sys.Instance.FrameNumber % excitedColorCount];

            int locX1 = firm.LocX1;
            int locX2 = firm.LocX2;
            int locY1 = firm.LocY1;
            int locY2 = firm.LocY2;
            
            if (IsExplored(locX1, locY1, locX2, locY2))
            {
                //Monster lairs has the same size as villages but should look different on mini-map
                if (firm.FirmType == Firm.FIRM_MONSTER)
                {
                    locX2--;
                    locY2--;
                }
                DrawRectOnMiniMap(locX1, locY1, locX2 - locX1 + 1, locY2 - locY1 + 1, nationColor);
            }
        }
        
        foreach (Site site in SiteArray)
        {
            if (IsExplored(site.LocX, site.LocY, site.LocX, site.LocY))
            {
                DrawRectOnMiniMap(site.LocX - 1, site.LocY - 1, 3, 3, Colors.SITE_COLOR);
            }
        }
        
        DrawUnitPathsOnMiniMap();

        foreach (Unit unit in UnitArray)
        {
            if (!unit.IsVisible() || unit.IsStealth())
                continue;

            byte nationColor = unit.CurAction != Sprite.SPRITE_ATTACK
                ? nationColorArray[unit.NationId]
                : excitedColorArray[ColorRemap.ColorSchemes[unit.NationId], Sys.Instance.FrameNumber % excitedColorCount];

            int locX = unit.CurLocX;
            int locY = unit.CurLocY;
            if (IsExplored(locX, locY, locX, locY))
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
                DrawRectOnMiniMap(locX, locY, size, size, nationColor, size == 2);
            }
        }

        foreach (Tornado tornado in TornadoArray)
        {
            if (IsExplored(tornado.CurLocX, tornado.CurLocY, tornado.CurLocX, tornado.CurLocY))
                DrawRectOnMiniMap(tornado.CurLocX - 1, tornado.CurLocY - 1, 3, 3, Colors.TORNADO_COLOR);
        }

        if (Sys.Instance.FrameNumber % 2 == 0)
        {
            int warPointColor = _warPointColors[(Sys.Instance.FrameNumber % 16) / 2];
            for (int i = 0; i < WarPointArray.WarPoints.GetLength(0); i++)
            {
                for (int j = 0; j < WarPointArray.WarPoints.GetLength(1); j++)
                {
                    WarPoint warPoint = WarPointArray.WarPoints[i, j];
                    if (warPoint.Strength > 0)
                    {
                        DrawLineOnMiniMap(i * InternalConstants.WARPOINT_ZONE_SIZE, j * InternalConstants.WARPOINT_ZONE_SIZE,
                            (i + 1) * InternalConstants.WARPOINT_ZONE_SIZE, (j + 1) * InternalConstants.WARPOINT_ZONE_SIZE, warPointColor);
                        DrawLineOnMiniMap((i + 1) * InternalConstants.WARPOINT_ZONE_SIZE, j * InternalConstants.WARPOINT_ZONE_SIZE,
                            i * InternalConstants.WARPOINT_ZONE_SIZE, (j + 1) * InternalConstants.WARPOINT_ZONE_SIZE, warPointColor);
                    }
                }
            }
        }

        DrawFrameOnMiniMap(_topLeftLocX - 1, _topLeftLocY - 1, MainViewWidthInCells + 2, MainViewHeightInCells + 2,
            Colors.VGA_YELLOW + _screenSquareFrameCount);

        if (nextFrame)
        {
            _screenSquareFrameCount += _screenSquareFrameStep;

            if (_screenSquareFrameCount == 0) // color with smaller number is brighter
                _screenSquareFrameStep = 1;

            if (_screenSquareFrameCount == 6) // bi-directional color shift
                _screenSquareFrameStep = -1;
        }
    }

    private void DrawUnitPathsOnMiniMap()
    {
        if ((Config.show_unit_path & 2) == 0)
            return;

        for (int i = 0; i < _selectedUnits.Count; i++)
        {
            Unit unit = UnitArray[_selectedUnits[i]];
            if (!unit.IsVisible() || unit.IsStealth())
                continue;

            int lineColor = (unit.MobileType == UnitConstants.UNIT_SEA) ? Colors.V_WHITE : Colors.V_BLACK;

            //TODO check this
            if (!Config.show_ai_info && NationArray.player_recno != 0 && !unit.BelongsToNation(NationArray.player_recno))
                continue;

            if (unit.PathNodes.Count > 0)
            {
                int prevDirection = -1;
                int startNodeLocX = -1;
                int startNodeLocY = -1;
                World.GetLocXAndLocY(unit.PathNodes[unit.PathNodeIndex], out int prevNodeLocX, out int prevNodeLocY);
                for (int j = unit.PathNodeIndex + 1; j < unit.PathNodes.Count; j++)
                {
                    World.GetLocXAndLocY(unit.PathNodes[j], out int nodeLocX, out int nodeLocY);
                    int nextDirection = GetPathDirection(prevNodeLocX, prevNodeLocY, nodeLocX, nodeLocY);

                    if (prevDirection != -1 && nextDirection != prevDirection)
                    {
                        DrawLineOnMiniMap(startNodeLocX, startNodeLocY, prevNodeLocX, prevNodeLocY, lineColor);
                    }

                    if (prevDirection == -1 || nextDirection != prevDirection)
                    {
                        prevDirection = nextDirection;
                        startNodeLocX = prevNodeLocX;
                        startNodeLocY = prevNodeLocY;
                    }

                    if (j == unit.PathNodes.Count - 1)
                    {
                        DrawLineOnMiniMap(startNodeLocX, startNodeLocY, nodeLocX, nodeLocY, lineColor);
                    }

                    prevNodeLocX = nodeLocX;
                    prevNodeLocY = nodeLocY;
                }
            }
        }
        
        //TODO draw waypoints
    }

    private int GetPathDirection(int locX1, int locY1, int locX2, int locY2)
    {
        if (locX1 == locX2)
        {
            if (locY1 < locY2)
                return InternalConstants.DIR_S;
            if (locY1 > locY2)
                return InternalConstants.DIR_N;
        }

        if (locY1 == locY2)
        {
            if (locX1 < locX2)
                return InternalConstants.DIR_E;
            if (locX1 > locX2)
                return InternalConstants.DIR_W;
        }

        if (locX1 != locX2 && locY1 != locY2)
        {
            if (locX1 < locX2)
                return locY1 < locY2 ? InternalConstants.DIR_SE : InternalConstants.DIR_NE;
            if (locX1 > locX2)
                return locY1 < locY2 ? InternalConstants.DIR_SW : InternalConstants.DIR_NW;
        }

        return -1;
    }
    
    private bool IsExplored(int locX1, int locY1, int locX2, int locY2)
    {
        if (Config.explore_whole_map)
            return true;
        
        for (int locY = locY1; locY <= locY2; locY++)
        {
            for (int locX = locX1; locX <= locX2; locX++)
            {
                Location location = World.GetLoc(locX, locY);
                if (location.IsExplored())
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void DrawLineOnMiniMap(int locX1, int locY1, int locX2, int locY2, int color)
    {
        if (GameConstants.MapSize == 100 || GameConstants.MapSize == 200)
        {
            if (locX1 == locX2)
            {
                Graphics.DrawLine(MiniMapX + locX1 * MiniMapScale, MiniMapY + locY1 * MiniMapScale,
                    MiniMapX + locX2 * MiniMapScale, MiniMapY + locY2 * MiniMapScale, color);
                Graphics.DrawLine(MiniMapX + locX1 * MiniMapScale + 1, MiniMapY + locY1 * MiniMapScale,
                    MiniMapX + locX2 * MiniMapScale + 1, MiniMapY + locY2 * MiniMapScale, color);
            }

            if (locY1 == locY2)
            {
                Graphics.DrawLine(MiniMapX + locX1 * MiniMapScale, MiniMapY + locY1 * MiniMapScale,
                    MiniMapX + locX2 * MiniMapScale, MiniMapY + locY2 * MiniMapScale, color);
                Graphics.DrawLine(MiniMapX + locX1 * MiniMapScale, MiniMapY + locY1 * MiniMapScale + 1,
                    MiniMapX + locX2 * MiniMapScale, MiniMapY + locY2 * MiniMapScale + 1, color);
            }

            if (locX1 != locX2 && locY1 != locY2)
            {
                Graphics.DrawLine(MiniMapX + locX1 * MiniMapScale, MiniMapY + locY1 * MiniMapScale,
                    MiniMapX + locX2 * MiniMapScale, MiniMapY + locY2 * MiniMapScale, color);
                Graphics.DrawLine(MiniMapX + locX1 * MiniMapScale + 1, MiniMapY + locY1 * MiniMapScale,
                    MiniMapX + locX2 * MiniMapScale + 1, MiniMapY + locY2 * MiniMapScale, color);
                Graphics.DrawLine(MiniMapX + locX1 * MiniMapScale, MiniMapY + locY1 * MiniMapScale + 1,
                    MiniMapX + locX2 * MiniMapScale, MiniMapY + locY2 * MiniMapScale + 1, color);
                Graphics.DrawLine(MiniMapX + locX1 * MiniMapScale + 1, MiniMapY + locY1 * MiniMapScale + 1,
                    MiniMapX + locX2 * MiniMapScale + 1, MiniMapY + locY2 * MiniMapScale + 1, color);
            }
        }
        
        if (GameConstants.MapSize == 300)
        {
            Graphics.DrawLine(MiniMapX + locX1 * 4 / 3, MiniMapY + locY1 * 4 / 3, MiniMapX + locX2 * 4 / 3, MiniMapY + locY2 * 4 / 3, color);
        }

        if (GameConstants.MapSize == 400)
        {
            Graphics.DrawLine(MiniMapX + locX1, MiniMapY + locY1, MiniMapX + locX2, MiniMapY + locY2, color);
        }
    }
    
    private void DrawRectOnMiniMap(int locX, int locY, int width, int height, int color, bool shiftUpAndLeft = false)
    {
        if (GameConstants.MapSize == 100 || GameConstants.MapSize == 200)
        {
            Graphics.DrawRect(MiniMapX + (shiftUpAndLeft ? locX * MiniMapScale - MiniMapScale / 2 : locX * MiniMapScale),
                MiniMapY + (shiftUpAndLeft ? locY * MiniMapScale - MiniMapScale / 2 : locY * MiniMapScale),
                width * MiniMapScale, height * MiniMapScale, color);
        }

        if (GameConstants.MapSize == 300)
        {
            Graphics.DrawRect(MiniMapX + locX * 4 / 3, MiniMapY + locY * 4 / 3, width * 4 / 3, height * 4 / 3, color);
        }
        
        if (GameConstants.MapSize == 400)
        {
            Graphics.DrawRect(MiniMapX + locX, MiniMapY + locY, width, height, color);
        }
    }

    private void DrawShadowOnMiniMap(int locX1, int locY1, int locX2, int locY2)
    {
        byte shadowColor = Colors.VGA_GRAY;
        
        if (GameConstants.MapSize == 100 || GameConstants.MapSize == 200)
        {
            DrawRectOnMiniMap(locX1 + 1, locY2 + 1, locX2 - locX1 + 1, 1, shadowColor);
            DrawRectOnMiniMap(locX2 + 1, locY1 + 1, 1, locY2 - locY1 + 1, shadowColor);
        }

        if (GameConstants.MapSize == 300)
        {
            Graphics.DrawLine(MiniMapX + locX1 * 4 / 3 + 1, MiniMapY + locY1 * 4 / 3 + (locY2 - locY1 + 1) * 4 / 3,
                MiniMapX + locX1 * 4 / 3 + (locX2 - locX1 + 1) * 4 / 3, MiniMapY + locY1 * 4 / 3 + (locY2 - locY1 + 1) * 4 / 3, shadowColor);
            Graphics.DrawLine(MiniMapX + locX1 * 4 / 3 + (locX2 - locX1 + 1) * 4 / 3, MiniMapY + locY1 * 4 / 3 + 1,
                MiniMapX + locX1 * 4 / 3 + (locX2 - locX1 + 1) * 4 / 3, MiniMapY + locY1 * 4 / 3 + (locY2 - locY1 + 1) * 4 / 3, shadowColor);
        }
        
        if (GameConstants.MapSize == 400)
        {
            Graphics.DrawLine(MiniMapX + locX1 + 1, MiniMapY + locY2 + 1, MiniMapX + locX2 + 1, MiniMapY + locY2 + 1, shadowColor);
            Graphics.DrawLine(MiniMapX + locX2 + 1, MiniMapY + locY1 + 1, MiniMapX + locX2 + 1, MiniMapY + locY2 + 1, shadowColor);
        }
    }

    private void DrawFrameOnMiniMap(int locX, int locY, int width, int height, int color)
    {
        if (GameConstants.MapSize == 100 || GameConstants.MapSize == 200)
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

        if (GameConstants.MapSize == 300)
        {
            Graphics.DrawRect(MiniMapX + locX * 4 / 3, MiniMapY + locY * 4 / 3, width * 4 / 3, 2, color);
            Graphics.DrawRect(MiniMapX + locX * 4 / 3, MiniMapY + (locY + height - 1) * 4 / 3, width * 4 / 3, 2, color);
            Graphics.DrawRect(MiniMapX + locX * 4 / 3, MiniMapY + locY * 4 / 3, 2, height * 4 / 3, color);
            Graphics.DrawRect(MiniMapX + (locX + width - 1) * 4 / 3, MiniMapY + locY * 4 / 3, 2, height * 4 / 3, color);
        }
        
        if (GameConstants.MapSize == 400)
        {
            Graphics.DrawFrame(MiniMapX + locX, MiniMapY + locY, width, height, color);
        }
    }
}