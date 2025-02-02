using System.Collections.Generic;

namespace TenKingdoms;

public static class Renderer
{
    public const int WindowWidth = ZoomMapX + ZoomMapWidth * ZoomTextureWidth + 12 + MiniMapSize + 12;
    public const int WindowHeight = ZoomMapY + ZoomMapHeight * ZoomTextureHeight;

    private const int ZoomMapX = 0;
    private const int ZoomMapY = 56;
    private const int ZoomMapWidth = 30; //width in cells
    private const int ZoomMapHeight = 19; //width in cells
    public const int ZoomTextureWidth = 48;
    public const int ZoomTextureHeight = 48;

    private const int MiniMapX = ZoomMapX + ZoomMapWidth * ZoomTextureWidth + 12;
    private const int MiniMapY = ZoomMapY;
    private const int MiniMapSize = 400;
    
    private const int TownFlagShiftX = -9;
    private const int TownFlagShiftY = -97;

    private const int MaxChangedLocs = 16384;
    
    private static int topLeftX;
    private static int topLeftY;
    private static long lastFrame;
    public static bool NeedFullRedraw { get; set; }
    private static int[] changedXLocs = new int[MaxChangedLocs];
    private static int[] changedYLocs = new int[MaxChangedLocs];
    private static int changedLocIndex;
    private static int screenSquareFrameCount = 0;
    private static int screenSquareFrameStep = 1;

    private static TerrainRes TerrainRes => Sys.Instance.TerrainRes;
    private static HillRes HillRes => Sys.Instance.HillRes;
    private static PlantRes PlantRes => Sys.Instance.PlantRes;
    private static TownRes TownRes => Sys.Instance.TownRes;
    private static FirmRes FirmRes => Sys.Instance.FirmRes;
    private static SpriteRes SpriteRes => Sys.Instance.SpriteRes;

    private static Config Config => Sys.Instance.Config;
    private static Info Info => Sys.Instance.Info;
    private static World World => Sys.Instance.World;

    
    private static RockArray DirtArray => Sys.Instance.DirtArray;
    private static NationArray NationArray => Sys.Instance.NationArray;
    private static TownArray TownArray => Sys.Instance.TownArray;
    private static FirmArray FirmArray => Sys.Instance.FirmArray;
    private static UnitArray UnitArray => Sys.Instance.UnitArray;
    private static SiteArray SiteArray => Sys.Instance.SiteArray;
    
    public static void ProcessInput(int eventType, int x, int y)
    {
        if (eventType == InputConstants.LeftMousePressed)
        {
            if (x >= MiniMapX && x < MiniMapX + MiniMapSize && y >= MiniMapY && y < MiniMapY + MiniMapSize)
            {
                int xLoc = x - MiniMapX;
                int yLoc = y - MiniMapY;
                if (MiniMapSize > GameConstants.MapSize)
                {
                    const int Scale = MiniMapSize / GameConstants.MapSize;
                    xLoc /= Scale;
                    yLoc /= Scale;
                }
                if (MiniMapSize < GameConstants.MapSize)
                {
                    const int Scale = GameConstants.MapSize / MiniMapSize;
                    xLoc *= Scale;
                    yLoc *= Scale;
                }
                
                topLeftX = xLoc - ZoomMapWidth / 2;
                if (topLeftX < 0)
                    topLeftX = 0;
                if (topLeftX > GameConstants.MapSize - ZoomMapWidth)
                    topLeftX = GameConstants.MapSize - ZoomMapWidth;

                topLeftY = yLoc - ZoomMapHeight / 2;
                if (topLeftY < 0)
                    topLeftY = 0;
                if (topLeftY > GameConstants.MapSize - ZoomMapHeight)
                    topLeftY = GameConstants.MapSize - ZoomMapHeight;
            }
        }
    }

    public static void DrawFrame(Graphics graphics)
    {
        if (lastFrame == Sys.Instance.FrameNumber && Sys.Instance.Speed != 0)
            return;

        lastFrame = Sys.Instance.FrameNumber;
        graphics.DrawMainScreen();
        DrawMap(graphics);
        DrawMiniMap(graphics);
    }

    private static void DrawMap(Graphics graphics)
    {
        graphics.SetClipRectangle(ZoomMapX, ZoomMapY, ZoomMapX + ZoomMapWidth * ZoomTextureWidth, ZoomMapY + ZoomMapHeight * ZoomTextureHeight);
        
        for (int x = topLeftX; (x < topLeftX + ZoomMapWidth) && x < GameConstants.MapSize; x++)
        {
            for (int y = topLeftY; (y < topLeftY + ZoomMapHeight) && y < GameConstants.MapSize; y++)
            {
                Location location = World.get_loc(x, y);
                if (location.explored())
                {
                    //Draw terrain
                    int drawX = ZoomMapX + (x - topLeftX) * ZoomTextureWidth;
                    int drawY = ZoomMapY + (y - topLeftY) * ZoomTextureHeight;
                    TerrainInfo terrainInfo = TerrainRes[location.terrain_id];
                    graphics.DrawBitmap(drawX, drawY, terrainInfo.GetTexture(graphics), terrainInfo.bitmapWidth, terrainInfo.bitmapHeight);

                    if (location.has_dirt())
                    {
                        //TODO draw dirt
                        //DirtArray[location.dirt_recno()].draw_block(drawX, drawY);
                    }
                    
                    //TODO draw snow
                    
                    if (location.has_hill())
                    {
                        if (location.hill_id2() != 0)
                            DrawHill(graphics, HillRes[location.hill_id2()], drawX, drawY, 1);
                        DrawHill(graphics, HillRes[location.hill_id1()], drawX, drawY, 1);
                    }
                    
                    //TODO draw power
                    
                    // don't display if a building/object has already been built on the location
                    if (location.has_site() && location.walkable(3))
                    {
                        //TODO draw site
                        //site_array[locPtr->site_recno()]->draw(x, y);
                    }
                }
            }
        }

        DrawTownsGround(graphics);
        
        DrawTownsStructures(graphics);

        DrawUnits(graphics);

        DrawFirms(graphics);

        //Draw firm dies

        DrawPlants(graphics);

        //Draw hills

        //Draw rocks

        //Draw fire

        //Draw air units

        //Draw bullets

        //Draw tornadoes

        //Draw effects

        //Draw unit paths and waypoints

        //DrawWeatherEffects();

        //DrawBuildMarker();

        //if (Config.blacken_map && Config.fog_of_war)
        //BlackenFogOfWar();

        //else if (!Config.explore_whole_map)
        //BlackenUnexplored();

        //DispText();
    }

    private static void DrawHill(Graphics graphics, HillBlockInfo hillBlockInfo, int drawX, int drawY, int layerMask)
    {
        //TODO check this
        //if((layerMask & hillBlockInfo.layer) == 0)
            //return;

        int hillX = drawX + hillBlockInfo.offset_x * 3 / 2;
        int hillY = drawY + hillBlockInfo.offset_y * 3 / 2;
		
        graphics.DrawBitmap(hillX, hillY, hillBlockInfo.GetTexture(graphics), hillBlockInfo.bitmapWidth, hillBlockInfo.bitmapHeight);
    }

    private static void DrawPlants(Graphics graphics)
    {
        for (int xLoc = topLeftX; (xLoc < topLeftX + ZoomMapWidth) && xLoc < GameConstants.MapSize; xLoc++)
        {
            for (int yLoc = topLeftY; (yLoc < topLeftY + ZoomMapHeight) && yLoc < GameConstants.MapSize; yLoc++)
            {
                Location location = World.get_loc(xLoc, yLoc);
                if (location.explored() && location.is_plant())
                {
                    PlantBitmap plantBitmap = PlantRes.get_bitmap(location.plant_id());
                    int drawX = ZoomMapX + (xLoc - topLeftX) * ZoomTextureWidth + plantBitmap.offset_x * 3 / 2;
                    int drawY = ZoomMapY + (yLoc - topLeftY) * ZoomTextureHeight + plantBitmap.offset_y * 3 / 2;
                    graphics.DrawBitmap(drawX, drawY, plantBitmap.GetTexture(graphics), plantBitmap.bitmapWidth, plantBitmap.bitmapHeight);
                }
            }
        }
    }

    private static void DrawTownsGround(Graphics graphics)
    {
        foreach (Town town in TownArray)
        {
            if (town.loc_x2 < topLeftX || town.loc_x1 > topLeftX + ZoomMapWidth)
                continue;
            if (town.loc_y2 < topLeftY || town.loc_y1 > topLeftY + ZoomMapHeight)
                continue;
            
            TownLayout townLayout = TownRes.get_layout(town.layout_id);
            int townX = ZoomMapX + (town.loc_x1 - topLeftX) * ZoomTextureWidth;
            int townY = ZoomMapY + (town.loc_y1 - topLeftY) * ZoomTextureHeight;
            int townLayoutX = townX + (InternalConstants.TOWN_WIDTH * ZoomTextureWidth - townLayout.groundBitmapWidth * 3 / 2) / 2;
            int townLayoutY = townY + (InternalConstants.TOWN_HEIGHT * ZoomTextureHeight - townLayout.groundBitmapHeight * 3 / 2) / 2;
            graphics.DrawBitmap(townLayoutX, townLayoutY, townLayout.GetTexture(graphics), townLayout.groundBitmapWidth, townLayout.groundBitmapHeight);
        }
    }

    private static void DrawTownsStructures(Graphics graphics)
    {
        foreach (Town town in TownArray)
        {
            if (town.loc_x2 < topLeftX || town.loc_x1 > topLeftX + ZoomMapWidth)
                continue;
            if (town.loc_y2 < topLeftY || town.loc_y1 > topLeftY + ZoomMapHeight)
                continue;
            
            int townX = ZoomMapX + (town.loc_x1 - topLeftX) * ZoomTextureWidth;
            int townY = ZoomMapY + (town.loc_y1 - topLeftY) * ZoomTextureHeight;

            TownLayout townLayout = TownRes.get_layout(town.layout_id);
            for (int i = 0; i < townLayout.slot_count; i++)
            {
                TownSlot townSlot = TownRes.get_slot(townLayout.first_slot_recno + i);

                switch (townSlot.build_type)
                {
                    case TownSlot.TOWN_OBJECT_HOUSE:
                        TownBuild townBuild = TownRes.get_build(town.slot_object_id_array[i]);
                        int townBuildX = townX + townSlot.base_x * 3 / 2 - townBuild.bitmapWidth * 3 / 2 / 2;
                        int townBuildY = townY + townSlot.base_y * 3 / 2 - townBuild.bitmapHeight * 3 / 2;
                        graphics.DrawBitmap(townBuildX, townBuildY, townBuild.GetTexture(graphics, town.nation_recno, false),
                            townBuild.bitmapWidth, townBuild.bitmapHeight);
                        break;
                    
                    case TownSlot.TOWN_OBJECT_PLANT:
                        PlantBitmap plantBitmap = PlantRes.get_bitmap(town.slot_object_id_array[i]);
                        int townPlantX = townX + townSlot.base_x * 3 / 2 - plantBitmap.bitmapWidth * 3 / 2 / 2;
                        int townPlantY = townY + townSlot.base_y * 3 / 2 - plantBitmap.bitmapHeight * 3 / 2;
                        graphics.DrawBitmap(townPlantX, townPlantY, plantBitmap.GetTexture(graphics), plantBitmap.bitmapWidth, plantBitmap.bitmapHeight);
                        break;
                    
                    case TownSlot.TOWN_OBJECT_FARM:
                        int farmIndex = townSlot.build_code - 1;
                        int townFarmX = townX + townSlot.base_x * 3 / 2;
                        int townFarmY = townY + townSlot.base_y * 3 / 2;
                        var farmTexture = TownRes.GetFarmTexture(graphics, farmIndex);
                        graphics.DrawBitmap(townFarmX, townFarmY, farmTexture, TownRes.farmWidths[farmIndex], TownRes.farmHeights[farmIndex]);
                        break;
                    
                    case TownSlot.TOWN_OBJECT_FLAG:
                        if (town.nation_recno == 0)
                            break;
                        
                        //TODO fix one flag slot with base_x == 57
                        int flagIndex = (int)(((Sys.Instance.FrameNumber + town.town_recno) % 8) / 2);
                        int townFlagX = townX + townSlot.base_x * 3 / 2 + TownFlagShiftX * 3 / 2;
                        int townFlagY = townY + townSlot.base_y * 3 / 2 + TownFlagShiftY * 3 / 2;
                        var flagTexture = TownRes.GetFlagTexture(graphics, flagIndex, town.nation_recno, false);
                        graphics.DrawBitmap(townFlagX, townFlagY, flagTexture, TownRes.flagWidths[flagIndex], TownRes.flagHeights[flagIndex]);
                        break;
                }
            }
        }
    }

    private static void DrawFirms(Graphics graphics)
    {
        //TODO
        int displayLayer = 1;

        foreach (Firm firm in FirmArray)
        {
            if (firm.loc_x2 < topLeftX || firm.loc_x1 > topLeftX + ZoomMapWidth)
                continue;
            if (firm.loc_y2 < topLeftY || firm.loc_y1 > topLeftY + ZoomMapHeight)
                continue;

            int firmX = ZoomMapX + (firm.loc_x1 - topLeftX) * ZoomTextureWidth;
            int firmY = ZoomMapY + (firm.loc_y1 - topLeftY) * ZoomTextureHeight;

            FirmBuild firmBuild = FirmRes.get_build(firm.firm_build_id);
            // if in construction, don't draw ground unless the last construction frame
            if (firmBuild.ground_bitmap_recno != 0 &&
                (!firm.under_construction || firm.construction_frame() >= firmBuild.under_construction_bitmap_count - 1))
            {
                FirmBitmap firmBitmap = FirmRes.get_bitmap(firmBuild.ground_bitmap_recno);
                int firmBitmapX = firmX + firmBitmap.offset_x * 3 / 2;
                int firmBitmapY = firmY + firmBitmap.offset_y * 3 / 2;
                graphics.DrawBitmap(firmBitmapX, firmBitmapY, firmBitmap.GetTexture(graphics, 0, false),
                    firmBitmap.bitmapWidth, firmBitmap.bitmapHeight);
            }

            if (firmBuild.animate_full_size)
            {
                DrawFirmFullSize(graphics, firm, firmX, firmY, displayLayer);
            }
            else
            {
                if (firm.under_construction)
                {
                    DrawFirmFullSize(graphics, firm, firmX, firmY, displayLayer);
                }
                else if (!firm.is_operating())
                {
                    if (FirmRes.get_bitmap(firmBuild.idle_bitmap_recno) != null)
                        DrawFirmFullSize(graphics, firm, firmX, firmY, displayLayer);
                    else
                    {
                        DrawFirmFrame(graphics, firm, firmX, firmY, 1, displayLayer);
                        DrawFirmFrame(graphics, firm, firmX, firmY, 2, displayLayer);
                    }
                }
                else
                {
                    // the first frame is the common frame for multi-segment bitmaps
                    DrawFirmFrame(graphics, firm, firmX, firmY, 1, displayLayer);
                    DrawFirmFrame(graphics, firm, firmX, firmY, firm.cur_frame, displayLayer);
                }
            }
        }
    }

    private static void DrawFirmFullSize(Graphics graphics, Firm firm, int firmX, int firmY, int displayLayer)
    {
        FirmBuild firmBuild = FirmRes.get_build(firm.firm_build_id);
        if (firm.under_construction)
        {
            //TODO
        }

        FirmBitmap firmBitmap;
        if (firm.under_construction)
        {
            int buildFraction = firm.construction_frame();
            firmBitmap = FirmRes.get_bitmap(firmBuild.under_construction_bitmap_recno + buildFraction);
        }
        else if (!firm.is_operating())
        {
            firmBitmap = FirmRes.get_bitmap(firmBuild.idle_bitmap_recno);
        }
        else
        {
            firmBitmap = FirmRes.get_bitmap(firmBuild.first_bitmap(firm.cur_frame));
        }

        // ------ check if the display layer is correct ---------//
        if ((firmBitmap.display_layer & displayLayer) == 0)
            return;

        int firmBitmapX = firmX + firmBitmap.offset_x * 3 / 2;
        int firmBitmapY = firmY + firmBitmap.offset_y * 3 / 2;
        graphics.DrawBitmap(firmBitmapX, firmBitmapY, firmBitmap.GetTexture(graphics, firm.nation_recno, false),
            firmBitmap.bitmapWidth, firmBitmap.bitmapHeight);

        if (firm.under_construction)
        {
            //TODO
        }
    }

    private static void DrawFirmFrame(Graphics graphics, Firm firm, int firmX, int firmY, int frameId, int displayLayer)
    {
        FirmBuild firmBuild = FirmRes.get_build(firm.firm_build_id);
        int firstBitmap = firmBuild.first_bitmap(frameId);
        int bitmapCount = firmBuild.bitmap_count(frameId);

        for (int i = 0, bitmapRecno = firstBitmap; i < bitmapCount; i++, bitmapRecno++)
        {
            FirmBitmap firmBitmap = FirmRes.get_bitmap(bitmapRecno);
            int firmBitmapX = firmX + firmBitmap.offset_x * 3 / 2;
            int firmBitmapY = firmY + firmBitmap.offset_y * 3 / 2;
            graphics.DrawBitmap(firmBitmapX, firmBitmapY, firmBitmap.GetTexture(graphics, firm.nation_recno, false),
                firmBitmap.bitmapWidth, firmBitmap.bitmapHeight);
        }
    }

    private static void DrawUnits(Graphics graphics)
    {
        foreach (Unit unit in UnitArray)
        {
            //TODO check conditions for big units
            if (unit.cur_x_loc() < topLeftX || unit.cur_x_loc() > topLeftX + ZoomMapWidth)
                continue;
            if (unit.cur_y_loc() < topLeftY || unit.cur_y_loc() > topLeftY + ZoomMapHeight)
                continue;

            SpriteFrame spriteFrame = unit.cur_sprite_frame(out bool needMirror);
            int unitX = ZoomMapX + unit.cur_x * 3 / 2 - topLeftX * ZoomTextureWidth + spriteFrame.offset_x;
            int unitY = ZoomMapY + unit.cur_y * 3 / 2 - topLeftY * ZoomTextureHeight + spriteFrame.offset_y;

            SpriteInfo spriteInfo = SpriteRes[unit.sprite_id];
            if (needMirror)
            {
                graphics.DrawBitmapFlip(unitX, unitY, spriteFrame.GetUnitTexture(graphics, spriteInfo, unit.nation_recno, false),
                    spriteFrame.width, spriteFrame.height);
            }
            else
            {
                graphics.DrawBitmap(unitX, unitY, spriteFrame.GetUnitTexture(graphics, spriteInfo, unit.nation_recno, false),
                    spriteFrame.width, spriteFrame.height);
            }
        }
    }

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
                const int Scale = MiniMapSize / GameConstants.MapSize;
                graphics.DrawRect(MiniMapX + xLoc * Scale, MiniMapY + yLoc * Scale, Scale, Scale, color);
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
                const int Scale = MiniMapSize / GameConstants.MapSize;
                if (x1Loc == x2Loc)
                    graphics.DrawRect(MiniMapX + x1Loc * Scale, MiniMapY + y1Loc * Scale, Scale, (y2Loc - y1Loc + 1) * Scale, color);
                if (y1Loc == y2Loc)
                    graphics.DrawRect(MiniMapX + x1Loc * Scale, MiniMapY + y1Loc * Scale, (x2Loc - x1Loc + 1) * Scale, Scale, color);
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
                const int Scale = MiniMapSize / GameConstants.MapSize;
                graphics.DrawRect(MiniMapX + xLoc * Scale, MiniMapY + yLoc * Scale, width * Scale, height * Scale, color);
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
                const int Scale = MiniMapSize / GameConstants.MapSize;
                graphics.DrawRect(MiniMapX + xLoc * Scale, MiniMapY + yLoc * Scale, width * Scale, Scale, color);
                graphics.DrawRect(MiniMapX + xLoc * Scale, MiniMapY + (yLoc + height - 1) * Scale, width * Scale, Scale, color);
                graphics.DrawRect(MiniMapX + xLoc * Scale, MiniMapY + yLoc * Scale, Scale, height * Scale, color);
                graphics.DrawRect(MiniMapX + (xLoc + width - 1) * Scale, MiniMapY + yLoc * Scale, Scale, height * Scale, color);
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

        if (NeedFullRedraw || changedLocIndex == MaxChangedLocs)
        {
            for (int yLoc = 0; yLoc < GameConstants.MapSize; yLoc++)
            {
                for (int xLoc = 0; xLoc < GameConstants.MapSize; xLoc++)
                {
                    DrawGroundOnMiniMap(graphics, xLoc, yLoc);
                }
            }

            NeedFullRedraw = false;
        }
        else
        {
            for (int i = 0; i < changedLocIndex; i++)
            {
                DrawGroundOnMiniMap(graphics, changedXLocs[i], changedYLocs[i]);
            }
        }

        changedLocIndex = 0;

        byte[] nationColorArray = NationArray.nation_color_array;
        byte[,] excitedColorArray = ColorRemap.excitedColorArray;
        int excitedColorCount = excitedColorArray.GetLength(1);
        
        //Draw shadows first
        byte shadowColor = Colors.VGA_GRAY;
        foreach (Town town in TownArray)
        {
            if (IsExplored(town.loc_x1, town.loc_x2, town.loc_y1, town.loc_y2))
            {
                DrawLineOnMiniMap(graphics, town.loc_x1 + 1, town.loc_y2 + 1, town.loc_x2 + 1, town.loc_y2 + 1, shadowColor);
                DrawLineOnMiniMap(graphics, town.loc_x2 + 1, town.loc_y1 + 1, town.loc_x2 + 1, town.loc_y2 + 1, shadowColor);
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
            byte nationColor = (town.last_being_attacked_date == default) || (Info.game_date - town.last_being_attacked_date).Days > 2
                ? nationColorArray[town.nation_recno]
                : excitedColorArray[ColorRemap.ColorSchemes[town.nation_recno], Sys.Instance.FrameNumber % excitedColorCount];

            if (IsExplored(town.loc_x1, town.loc_x2, town.loc_y1, town.loc_y2))
            {
                DrawRectOnMiniMap(graphics, town.loc_x1, town.loc_y1, town.loc_x2 - town.loc_x1 + 1, town.loc_y2 - town.loc_y1 + 1, nationColor);
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
                int size = (unit.mobile_type == UnitConstants.UNIT_LAND ? 2 : 3);
                DrawRectOnMiniMap(graphics, xLoc, yLoc, size, size, nationColor);

                for (int x = xLoc; x < xLoc + size; x++)
                {
                    for (int y = yLoc; y < yLoc + size; y++)
                    {
                        AddChangedLoc(x, y);
                    }
                }
            }
        }
        
        //Draw tornadoes

        //Draw war points
        
        DrawFrameOnMiniMap(graphics, topLeftX - 1, topLeftY - 1, ZoomMapWidth + 2, ZoomMapHeight + 2,
            Colors.VGA_YELLOW + screenSquareFrameCount);

        for (int x = topLeftX - 1; x < topLeftX - 1 + ZoomMapWidth + 2; x++)
        {
            AddChangedLoc(x, topLeftY - 1);
            AddChangedLoc(x, topLeftY + ZoomMapHeight);
        }

        for (int y = topLeftY - 1; y < topLeftY - 1 + ZoomMapHeight + 2; y++)
        {
            AddChangedLoc(topLeftX - 1, y);
            AddChangedLoc(topLeftX + ZoomMapWidth, y);
        }

        if (Sys.Instance.Speed != 0)
        {
            screenSquareFrameCount += screenSquareFrameStep;

            if (screenSquareFrameCount == 0) // color with smaller number is brighter
                screenSquareFrameStep = 1;

            if (screenSquareFrameCount == 6) // bi-directional color shift
                screenSquareFrameStep = -1;
        }
    }

    private static void DrawGroundOnMiniMap(Graphics graphics, int xLoc, int yLoc)
    {
        Location location = World.get_loc(xLoc, yLoc);
        int color = Colors.UNEXPLORED_COLOR;
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

        DrawPointOnMiniMap(graphics, xLoc, yLoc, color);
    }

    private static bool IsExplored(int x1Loc, int x2Loc, int y1Loc, int y2Loc)
    {
        if (Config.explore_whole_map)
            return true;
        
        for (int xLoc = x1Loc; xLoc <= x2Loc; xLoc++)
        {
            for (int yLoc = y1Loc; yLoc <= y2Loc; yLoc++)
            {
                Location location = World.get_loc(xLoc, yLoc);
                if (location.explored())
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static void AddChangedLoc(int xLoc, int yLoc)
    {
        if (changedLocIndex == MaxChangedLocs)
            return;
        
        if (xLoc < 0 || xLoc >= GameConstants.MapSize || yLoc < 0 || yLoc >= GameConstants.MapSize)
            return;
        
        changedXLocs[changedLocIndex] = xLoc;
        changedYLocs[changedLocIndex] = yLoc;
        changedLocIndex++;
    }
}