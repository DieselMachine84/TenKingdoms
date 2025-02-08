namespace TenKingdoms;

public partial class Renderer
{
    private void DrawMap()
    {
        Graphics.SetClipRectangle(ZoomMapX, ZoomMapY, ZoomMapX + ZoomMapLocWidth * ZoomTextureWidth, ZoomMapY + ZoomMapLocHeight * ZoomTextureHeight);
        
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
                    Graphics.DrawBitmapScale(terrainInfo.GetTexture(Graphics), drawX, drawY, terrainInfo.bitmapWidth, terrainInfo.bitmapHeight);

                    if (location.has_dirt())
                    {
                        //TODO draw dirt
                        //DirtArray[location.dirt_recno()].draw_block(drawX, drawY);
                    }
                    
                    //TODO draw snow
                    
                    if (location.has_hill())
                    {
                        if (location.hill_id2() != 0)
                            DrawHill(HillRes[location.hill_id2()], drawX, drawY, 1);
                        DrawHill(HillRes[location.hill_id1()], drawX, drawY, 1);
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

        DrawTownsGround();
        
        DrawTownsStructures();

        DrawUnits();

        DrawFirms();

        //Draw firm dies

        DrawPlants();

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

    private void DrawHill(HillBlockInfo hillBlockInfo, int drawX, int drawY, int layerMask)
    {
        //TODO check this
        //if((layerMask & hillBlockInfo.layer) == 0)
            //return;

        int hillX = drawX + hillBlockInfo.offset_x * 3 / 2;
        int hillY = drawY + hillBlockInfo.offset_y * 3 / 2;
		
        Graphics.DrawBitmapScale(hillBlockInfo.GetTexture(Graphics), hillX, hillY, hillBlockInfo.bitmapWidth, hillBlockInfo.bitmapHeight);
    }

    private void DrawPlants()
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
                    Graphics.DrawBitmapScale(plantBitmap.GetTexture(Graphics), drawX, drawY, plantBitmap.bitmapWidth, plantBitmap.bitmapHeight);
                }
            }
        }
    }

    private void DrawTownsGround()
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
            Graphics.DrawBitmapScale(townLayout.GetTexture(Graphics), townLayoutX, townLayoutY, townLayout.groundBitmapWidth, townLayout.groundBitmapHeight);
        }
    }

    private void DrawTownsStructures()
    {
        foreach (Town town in TownArray)
        {
            if (town.LocX2 < topLeftX || town.LocX1 > topLeftX + ZoomMapLocWidth)
                continue;
            if (town.LocY2 < topLeftY || town.LocY1 > topLeftY + ZoomMapLocHeight)
                continue;
            
            int townX = ZoomMapX + (town.LocX1 - topLeftX) * ZoomTextureWidth;
            int townY = ZoomMapY + (town.LocY1 - topLeftY) * ZoomTextureHeight;
            bool isSelected = (town.TownId == selectedTownId);

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
                        Graphics.DrawBitmapScale(townBuild.GetTexture(Graphics, town.NationId, isSelected), townBuildX, townBuildY,
                            townBuild.bitmapWidth, townBuild.bitmapHeight);
                        break;
                    
                    case TownSlot.TOWN_OBJECT_PLANT:
                        PlantBitmap plantBitmap = PlantRes.get_bitmap(town.SlotObjectIds[i]);
                        int townPlantX = townX + townSlot.base_x * 3 / 2 - plantBitmap.bitmapWidth * 3 / 2 / 2;
                        int townPlantY = townY + townSlot.base_y * 3 / 2 - plantBitmap.bitmapHeight * 3 / 2;
                        Graphics.DrawBitmapScale(plantBitmap.GetTexture(Graphics), townPlantX, townPlantY,
                            plantBitmap.bitmapWidth, plantBitmap.bitmapHeight);
                        break;
                    
                    case TownSlot.TOWN_OBJECT_FARM:
                        int farmIndex = townSlot.build_code - 1;
                        int townFarmX = townX + townSlot.base_x * 3 / 2;
                        int townFarmY = townY + townSlot.base_y * 3 / 2;
                        var farmTexture = TownRes.GetFarmTexture(Graphics, farmIndex);
                        Graphics.DrawBitmapScale(farmTexture, townFarmX, townFarmY,
                            TownRes.farmWidths[farmIndex], TownRes.farmHeights[farmIndex]);
                        break;
                    
                    case TownSlot.TOWN_OBJECT_FLAG:
                        if (town.NationId == 0 && town.RebelId == 0)
                            break;
                        
                        //TODO fix one flag slot with base_x == 57
                        int flagIndex = (int)(((Sys.Instance.FrameNumber + town.TownId) % 8) / 2);
                        int townFlagX = townX + townSlot.base_x * 3 / 2 + TownFlagShiftX * 3 / 2;
                        int townFlagY = townY + townSlot.base_y * 3 / 2 + TownFlagShiftY * 3 / 2;
                        var flagTexture = TownRes.GetFlagTexture(Graphics, flagIndex, town.NationId);
                        Graphics.DrawBitmapScale(flagTexture, townFlagX, townFlagY,
                            TownRes.flagWidths[flagIndex], TownRes.flagHeights[flagIndex]);
                        break;
                }
            }
        }
    }

    private void DrawFirms()
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
                Graphics.DrawBitmapScale(firmBitmap.GetTexture(Graphics, 0, false), firmBitmapX, firmBitmapY,
                    firmBitmap.bitmapWidth, firmBitmap.bitmapHeight);
            }

            if (firmBuild.animate_full_size)
            {
                DrawFirmFullSize(firm, firmX, firmY, displayLayer);
            }
            else
            {
                if (firm.under_construction)
                {
                    DrawFirmFullSize(firm, firmX, firmY, displayLayer);
                }
                else if (!firm.is_operating())
                {
                    if (FirmRes.get_bitmap(firmBuild.idle_bitmap_recno) != null)
                        DrawFirmFullSize(firm, firmX, firmY, displayLayer);
                    else
                    {
                        DrawFirmFrame(firm, firmX, firmY, 1, displayLayer);
                        DrawFirmFrame(firm, firmX, firmY, 2, displayLayer);
                    }
                }
                else
                {
                    // the first frame is the common frame for multi-segment bitmaps
                    DrawFirmFrame(firm, firmX, firmY, 1, displayLayer);
                    DrawFirmFrame(firm, firmX, firmY, firm.cur_frame, displayLayer);
                }
            }
        }
    }

    private void DrawFirmFullSize(Firm firm, int firmX, int firmY, int displayLayer)
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
        Graphics.DrawBitmapScale(firmBitmap.GetTexture(Graphics, firm.nation_recno, false), firmBitmapX, firmBitmapY,
            firmBitmap.bitmapWidth, firmBitmap.bitmapHeight);

        if (firm.under_construction)
        {
            //TODO
        }
    }

    private void DrawFirmFrame(Firm firm, int firmX, int firmY, int frameId, int displayLayer)
    {
        FirmBuild firmBuild = FirmRes.get_build(firm.firm_build_id);
        int firstBitmap = firmBuild.first_bitmap(frameId);
        int bitmapCount = firmBuild.bitmap_count(frameId);

        for (int i = 0, bitmapRecno = firstBitmap; i < bitmapCount; i++, bitmapRecno++)
        {
            FirmBitmap firmBitmap = FirmRes.get_bitmap(bitmapRecno);
            int firmBitmapX = firmX + firmBitmap.offset_x * 3 / 2;
            int firmBitmapY = firmY + firmBitmap.offset_y * 3 / 2;
            Graphics.DrawBitmapScale(firmBitmap.GetTexture(Graphics, firm.nation_recno, false), firmBitmapX, firmBitmapY,
                firmBitmap.bitmapWidth, firmBitmap.bitmapHeight);
        }
    }

    private void DrawUnits()
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
                Graphics.DrawBitmapScaleAndFlip(spriteFrame.GetUnitTexture(Graphics, spriteInfo, unit.nation_recno, isSelected), unitX, unitY,
                    spriteFrame.width, spriteFrame.height);
            }
            else
            {
                Graphics.DrawBitmapScale(spriteFrame.GetUnitTexture(Graphics, spriteInfo, unit.nation_recno, isSelected), unitX, unitY,
                    spriteFrame.width, spriteFrame.height);
            }
        }
    }
}