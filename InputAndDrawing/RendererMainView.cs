namespace TenKingdoms;

public partial class Renderer
{
    private void DrawMainView()
    {
        Graphics.SetClipRectangle(MainViewX, MainViewY, MainViewX + MainViewWidthInCells * CellTextureWidth, MainViewY + MainViewHeightInCells * CellTextureHeight);
        
        for (int x = _topLeftX; (x < _topLeftX + MainViewWidthInCells) && x < GameConstants.MapSize; x++)
        {
            for (int y = _topLeftY; (y < _topLeftY + MainViewHeightInCells) && y < GameConstants.MapSize; y++)
            {
                Location location = World.get_loc(x, y);
                if (!location.explored())
                    continue;

                //Draw terrain
                //TODO terrain animation
                int drawX = MainViewX + (x - _topLeftX) * CellTextureWidth;
                int drawY = MainViewY + (y - _topLeftY) * CellTextureHeight;
                TerrainInfo terrainInfo = TerrainRes[location.terrain_id];
                Graphics.DrawBitmap(terrainInfo.GetTexture(Graphics), drawX, drawY, Scale(terrainInfo.bitmapWidth), Scale(terrainInfo.bitmapHeight));

                /*if (location.has_dirt())
                {
                    //rock_res.draw_block(rock_recno, xLoc, yLoc, xLoc-dirt.loc_x, yLoc-dirt.loc_y, cur_frame);
                    RockInfo rockInfo = RockRes.get_rock_info(location.dirt_recno());
                    int rockBlockRecno = locate_block(rockRecno, offsetX, offsetY);
                    int rockBitmapRecno = get_bitmap_recno(rockBlockRecno, curFrame);
                    if (rockBlockRecno != 0 && rockBitmapRecno != 0)
                    {
                        get_bitmap_info(rockBitmapRecno)->draw(xLoc, yLoc);
                    }
                }*/

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

        int hillX = drawX + Scale(hillBlockInfo.offset_x);
        int hillY = drawY + Scale(hillBlockInfo.offset_y);
		
        Graphics.DrawBitmap(hillBlockInfo.GetTexture(Graphics), hillX, hillY, Scale(hillBlockInfo.bitmapWidth), Scale(hillBlockInfo.bitmapHeight));
    }

    private void DrawPlants()
    {
        for (int xLoc = _topLeftX; (xLoc < _topLeftX + MainViewWidthInCells) && xLoc < GameConstants.MapSize; xLoc++)
        {
            for (int yLoc = _topLeftY; (yLoc < _topLeftY + MainViewHeightInCells) && yLoc < GameConstants.MapSize; yLoc++)
            {
                Location location = World.get_loc(xLoc, yLoc);
                if (location.explored() && location.is_plant())
                {
                    PlantBitmap plantBitmap = PlantRes.get_bitmap(location.plant_id());
                    int drawX = MainViewX + (xLoc - _topLeftX) * CellTextureWidth + Scale(plantBitmap.offset_x);
                    int drawY = MainViewY + (yLoc - _topLeftY) * CellTextureHeight + Scale(plantBitmap.offset_y);
                    Graphics.DrawBitmap(plantBitmap.GetTexture(Graphics), drawX, drawY, Scale(plantBitmap.bitmapWidth), Scale(plantBitmap.bitmapHeight));
                }
            }
        }
    }

    private void DrawTownsGround()
    {
        foreach (Town town in TownArray)
        {
            if (town.LocX2 < _topLeftX || town.LocX1 > _topLeftX + MainViewWidthInCells)
                continue;
            if (town.LocY2 < _topLeftY || town.LocY1 > _topLeftY + MainViewHeightInCells)
                continue;
            
            TownLayout townLayout = TownRes.GetLayout(town.LayoutId);
            int townX = MainViewX + (town.LocX1 - _topLeftX) * CellTextureWidth;
            int townY = MainViewY + (town.LocY1 - _topLeftY) * CellTextureHeight;
            int townLayoutX = townX + (InternalConstants.TOWN_WIDTH * CellTextureWidth - Scale(townLayout.groundBitmapWidth)) / 2;
            int townLayoutY = townY + (InternalConstants.TOWN_HEIGHT * CellTextureHeight - Scale(townLayout.groundBitmapHeight)) / 2;
            Graphics.DrawBitmap(townLayout.GetTexture(Graphics), townLayoutX, townLayoutY, Scale(townLayout.groundBitmapWidth), Scale(townLayout.groundBitmapHeight));
        }
    }

    private void DrawTownsStructures()
    {
        foreach (Town town in TownArray)
        {
            if (town.LocX2 < _topLeftX || town.LocX1 > _topLeftX + MainViewWidthInCells)
                continue;
            if (town.LocY2 < _topLeftY || town.LocY1 > _topLeftY + MainViewHeightInCells)
                continue;
            
            int townX = MainViewX + (town.LocX1 - _topLeftX) * CellTextureWidth;
            int townY = MainViewY + (town.LocY1 - _topLeftY) * CellTextureHeight;
            bool isSelected = (town.TownId == _selectedTownId);

            TownLayout townLayout = TownRes.GetLayout(town.LayoutId);
            for (int i = 0; i < townLayout.SlotCount; i++)
            {
                TownSlot townSlot = TownRes.GetSlot(townLayout.FirstSlotId + i);

                switch (townSlot.BuildType)
                {
                    case TownSlot.TOWN_OBJECT_HOUSE:
                        TownBuild townBuild = TownRes.GetBuild(town.SlotObjectIds[i]);
                        int townBuildX = townX + Scale(townSlot.BaseX) - Scale(townBuild.bitmapWidth) / 2;
                        int townBuildY = townY + Scale(townSlot.BaseY) - Scale(townBuild.bitmapHeight);
                        Graphics.DrawBitmap(townBuild.GetTexture(Graphics, town.NationId, isSelected), townBuildX, townBuildY,
                            Scale(townBuild.bitmapWidth), Scale(townBuild.bitmapHeight));
                        break;
                    
                    case TownSlot.TOWN_OBJECT_PLANT:
                        PlantBitmap plantBitmap = PlantRes.get_bitmap(town.SlotObjectIds[i]);
                        int townPlantX = townX + Scale(townSlot.BaseX) - Scale(plantBitmap.bitmapWidth) / 2;
                        int townPlantY = townY + Scale(townSlot.BaseY) - Scale(plantBitmap.bitmapHeight);
                        Graphics.DrawBitmap(plantBitmap.GetTexture(Graphics), townPlantX, townPlantY,
                            Scale(plantBitmap.bitmapWidth), Scale(plantBitmap.bitmapHeight));
                        break;
                    
                    case TownSlot.TOWN_OBJECT_FARM:
                        int farmIndex = townSlot.BuildCode - 1;
                        int townFarmX = townX + Scale(townSlot.BaseX);
                        int townFarmY = townY + Scale(townSlot.BaseY);
                        var farmTexture = TownRes.GetFarmTexture(Graphics, farmIndex);
                        Graphics.DrawBitmap(farmTexture, townFarmX, townFarmY,
                            Scale(TownRes.FarmWidths[farmIndex]), Scale(TownRes.FarmHeights[farmIndex]));
                        break;
                    
                    case TownSlot.TOWN_OBJECT_FLAG:
                        if (town.NationId == 0 && town.RebelId == 0)
                            break;
                        
                        //TODO fix one flag slot with base_x == 57
                        int flagIndex = (int)(((Sys.Instance.FrameNumber + town.TownId) % 8) / 2);
                        int townFlagX = townX + Scale(townSlot.BaseX) + Scale(TownFlagShiftX);
                        int townFlagY = townY + Scale(townSlot.BaseY) + Scale(TownFlagShiftY);
                        var flagTexture = TownRes.GetFlagTexture(Graphics, flagIndex, town.NationId);
                        Graphics.DrawBitmap(flagTexture, townFlagX, townFlagY,
                            Scale(TownRes.FlagWidths[flagIndex]), Scale(TownRes.FlagHeights[flagIndex]));
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
            if (firm.loc_x2 < _topLeftX || firm.loc_x1 > _topLeftX + MainViewWidthInCells)
                continue;
            if (firm.loc_y2 < _topLeftY || firm.loc_y1 > _topLeftY + MainViewHeightInCells)
                continue;

            int firmX = MainViewX + (firm.loc_x1 - _topLeftX) * CellTextureWidth;
            int firmY = MainViewY + (firm.loc_y1 - _topLeftY) * CellTextureHeight;

            FirmBuild firmBuild = FirmRes.get_build(firm.firm_build_id);
            // if in construction, don't draw ground unless the last construction frame
            if (firmBuild.ground_bitmap_recno != 0 &&
                (!firm.under_construction || firm.construction_frame() >= firmBuild.under_construction_bitmap_count - 1))
            {
                FirmBitmap firmBitmap = FirmRes.get_bitmap(firmBuild.ground_bitmap_recno);
                int firmBitmapX = firmX + Scale(firmBitmap.offset_x);
                int firmBitmapY = firmY + Scale(firmBitmap.offset_y);
                Graphics.DrawBitmap(firmBitmap.GetTexture(Graphics, 0, false), firmBitmapX, firmBitmapY,
                    Scale(firmBitmap.bitmapWidth), Scale(firmBitmap.bitmapHeight));
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

        int firmBitmapX = firmX + Scale(firmBitmap.offset_x);
        int firmBitmapY = firmY + Scale(firmBitmap.offset_y);
        Graphics.DrawBitmap(firmBitmap.GetTexture(Graphics, firm.nation_recno, false), firmBitmapX, firmBitmapY,
            Scale(firmBitmap.bitmapWidth), Scale(firmBitmap.bitmapHeight));

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
            int firmBitmapX = firmX + Scale(firmBitmap.offset_x);
            int firmBitmapY = firmY + Scale(firmBitmap.offset_y);
            Graphics.DrawBitmap(firmBitmap.GetTexture(Graphics, firm.nation_recno, false), firmBitmapX, firmBitmapY,
                Scale(firmBitmap.bitmapWidth), Scale(firmBitmap.bitmapHeight));
        }
    }

    private void DrawUnits()
    {
        foreach (Unit unit in UnitArray)
        {
            //TODO check conditions for big units
            if (unit.cur_x_loc() < _topLeftX || unit.cur_x_loc() > _topLeftX + MainViewWidthInCells)
                continue;
            if (unit.cur_y_loc() < _topLeftY || unit.cur_y_loc() > _topLeftY + MainViewHeightInCells)
                continue;

            SpriteFrame spriteFrame = unit.cur_sprite_frame(out bool needMirror);
            int unitX = MainViewX + Scale(unit.cur_x) - _topLeftX * CellTextureWidth + spriteFrame.offset_x;
            int unitY = MainViewY + Scale(unit.cur_y) - _topLeftY * CellTextureHeight + spriteFrame.offset_y;

            SpriteInfo spriteInfo = SpriteRes[unit.sprite_id];
            //TODO select only under cursor?
            bool isSelected = (unit.sprite_recno == _selectedUnitId);
            Graphics.DrawBitmap(spriteFrame.GetUnitTexture(Graphics, spriteInfo, unit.nation_recno, isSelected), unitX, unitY,
                Scale(spriteFrame.width), Scale(spriteFrame.height), needMirror);
        }
    }
}