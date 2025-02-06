using System;
using System.Collections.Generic;

namespace TenKingdoms;

public static partial class Renderer
{
    public const int WindowWidth = ZoomMapX + ZoomMapWidth + 12 + MiniMapSize + 12;
    public const int WindowHeight = ZoomMapY + ZoomMapHeight;

    private const int ZoomMapX = 0;
    private const int ZoomMapY = 56;
    private const int ZoomMapLocWidth = 30; //width in cells
    private const int ZoomMapLocHeight = 19; //width in cells
    public const int ZoomTextureWidth = 48;
    public const int ZoomTextureHeight = 48;
    private const int ZoomMapWidth = ZoomMapLocWidth * ZoomTextureWidth;
    private const int ZoomMapHeight = ZoomMapLocHeight * ZoomTextureHeight;

    private const int MiniMapX = ZoomMapX + ZoomMapLocWidth * ZoomTextureWidth + 12;
    private const int MiniMapY = ZoomMapY;
    private const int MiniMapSize = 400;
    private const int MiniMapScale = MiniMapSize / GameConstants.MapSize;
    
    private const int TownFlagShiftX = -9;
    private const int TownFlagShiftY = -97;

    private static int topLeftX;
    private static int topLeftY;
    private static long lastFrame;
    public static bool NeedFullRedraw { get; set; }
    private static int screenSquareFrameCount = 0;
    private static int screenSquareFrameStep = 1;
    private static byte[] miniMapImage = new byte[MiniMapSize * MiniMapSize];

    private static int selectedUnitId = 0;

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
    
    public static void DrawFrame(Graphics graphics)
    {
        if (lastFrame == Sys.Instance.FrameNumber && Sys.Instance.Speed != 0)
            return;

        lastFrame = Sys.Instance.FrameNumber;
        DrawMap(graphics);
        DrawMiniMap(graphics);
        //graphics.DrawMainScreen();
    }

    private static void DrawMap(Graphics graphics)
    {
        graphics.SetClipRectangle(ZoomMapX, ZoomMapY, ZoomMapX + ZoomMapLocWidth * ZoomTextureWidth, ZoomMapY + ZoomMapLocHeight * ZoomTextureHeight);
        
        for (int x = topLeftX; (x < topLeftX + ZoomMapLocWidth) && x < GameConstants.MapSize; x++)
        {
            for (int y = topLeftY; (y < topLeftY + ZoomMapLocHeight) && y < GameConstants.MapSize; y++)
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
        for (int xLoc = topLeftX; (xLoc < topLeftX + ZoomMapLocWidth) && xLoc < GameConstants.MapSize; xLoc++)
        {
            for (int yLoc = topLeftY; (yLoc < topLeftY + ZoomMapLocHeight) && yLoc < GameConstants.MapSize; yLoc++)
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
            if (town.LocX2 < topLeftX || town.LocX1 > topLeftX + ZoomMapLocWidth)
                continue;
            if (town.LocY2 < topLeftY || town.LocY1 > topLeftY + ZoomMapLocHeight)
                continue;
            
            TownLayout townLayout = TownRes.get_layout(town.LayoutId);
            int townX = ZoomMapX + (town.LocX1 - topLeftX) * ZoomTextureWidth;
            int townY = ZoomMapY + (town.LocY1 - topLeftY) * ZoomTextureHeight;
            int townLayoutX = townX + (InternalConstants.TOWN_WIDTH * ZoomTextureWidth - townLayout.groundBitmapWidth * 3 / 2) / 2;
            int townLayoutY = townY + (InternalConstants.TOWN_HEIGHT * ZoomTextureHeight - townLayout.groundBitmapHeight * 3 / 2) / 2;
            graphics.DrawBitmap(townLayoutX, townLayoutY, townLayout.GetTexture(graphics), townLayout.groundBitmapWidth, townLayout.groundBitmapHeight);
        }
    }

    private static void DrawTownsStructures(Graphics graphics)
    {
        foreach (Town town in TownArray)
        {
            if (town.LocX2 < topLeftX || town.LocX1 > topLeftX + ZoomMapLocWidth)
                continue;
            if (town.LocY2 < topLeftY || town.LocY1 > topLeftY + ZoomMapLocHeight)
                continue;
            
            int townX = ZoomMapX + (town.LocX1 - topLeftX) * ZoomTextureWidth;
            int townY = ZoomMapY + (town.LocY1 - topLeftY) * ZoomTextureHeight;

            TownLayout townLayout = TownRes.get_layout(town.LayoutId);
            for (int i = 0; i < townLayout.slot_count; i++)
            {
                TownSlot townSlot = TownRes.get_slot(townLayout.first_slot_recno + i);

                switch (townSlot.build_type)
                {
                    case TownSlot.TOWN_OBJECT_HOUSE:
                        TownBuild townBuild = TownRes.get_build(town.SlotObjectIds[i]);
                        int townBuildX = townX + townSlot.base_x * 3 / 2 - townBuild.bitmapWidth * 3 / 2 / 2;
                        int townBuildY = townY + townSlot.base_y * 3 / 2 - townBuild.bitmapHeight * 3 / 2;
                        graphics.DrawBitmap(townBuildX, townBuildY, townBuild.GetTexture(graphics, town.NationId, false),
                            townBuild.bitmapWidth, townBuild.bitmapHeight);
                        break;
                    
                    case TownSlot.TOWN_OBJECT_PLANT:
                        PlantBitmap plantBitmap = PlantRes.get_bitmap(town.SlotObjectIds[i]);
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
                        if (town.NationId == 0)
                            break;
                        
                        //TODO fix one flag slot with base_x == 57
                        int flagIndex = (int)(((Sys.Instance.FrameNumber + town.TownId) % 8) / 2);
                        int townFlagX = townX + townSlot.base_x * 3 / 2 + TownFlagShiftX * 3 / 2;
                        int townFlagY = townY + townSlot.base_y * 3 / 2 + TownFlagShiftY * 3 / 2;
                        var flagTexture = TownRes.GetFlagTexture(graphics, flagIndex, town.NationId, false);
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
            if (firm.loc_x2 < topLeftX || firm.loc_x1 > topLeftX + ZoomMapLocWidth)
                continue;
            if (firm.loc_y2 < topLeftY || firm.loc_y1 > topLeftY + ZoomMapLocHeight)
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
            if (unit.cur_x_loc() < topLeftX || unit.cur_x_loc() > topLeftX + ZoomMapLocWidth)
                continue;
            if (unit.cur_y_loc() < topLeftY || unit.cur_y_loc() > topLeftY + ZoomMapLocHeight)
                continue;

            SpriteFrame spriteFrame = unit.cur_sprite_frame(out bool needMirror);
            int unitX = ZoomMapX + unit.cur_x * 3 / 2 - topLeftX * ZoomTextureWidth + spriteFrame.offset_x;
            int unitY = ZoomMapY + unit.cur_y * 3 / 2 - topLeftY * ZoomTextureHeight + spriteFrame.offset_y;

            SpriteInfo spriteInfo = SpriteRes[unit.sprite_id];
            bool isSelected = (unit.sprite_recno == selectedUnitId);
            if (needMirror)
            {
                graphics.DrawBitmapFlip(unitX, unitY, spriteFrame.GetUnitTexture(graphics, spriteInfo, unit.nation_recno, isSelected),
                    spriteFrame.width, spriteFrame.height);
            }
            else
            {
                graphics.DrawBitmap(unitX, unitY, spriteFrame.GetUnitTexture(graphics, spriteInfo, unit.nation_recno, isSelected),
                    spriteFrame.width, spriteFrame.height);
            }
        }
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
}