using System;
using System.Collections.Generic;

namespace TenKingdoms;

public partial class Renderer
{
    private readonly byte[] _animatedLineSegment = { 0x90, 0x93, 0x98, 0x9c, 0x9f, 0x9c, 0x98, 0x93 };
    private readonly List<IntPtr> _horizontalLineTextures = new List<nint>();
    private readonly List<IntPtr> _verticalLineTextures = new List<nint>();
    private readonly List<IntPtr> _diagonalLineTextures = new List<nint>();

    private void DrawMainView()
    {
        Graphics.SetClipRectangle(MainViewX, MainViewY, MainViewX + MainViewWidthInCells * CellTextureWidth, MainViewY + MainViewHeightInCells * CellTextureHeight);
        
        for (int locX = _topLeftLocX; (locX < _topLeftLocX + MainViewWidthInCells) && locX < GameConstants.MapSize; locX++)
        {
            for (int locY = _topLeftLocY; (locY < _topLeftLocY + MainViewHeightInCells) && locY < GameConstants.MapSize; locY++)
            {
                Location location = World.GetLoc(locX, locY);
                if (!location.IsExplored())
                    continue;

                //Draw terrain
                //TODO terrain animation
                int screenX = MainViewX + (locX - _topLeftLocX) * CellTextureWidth;
                int screenY = MainViewY + (locY - _topLeftLocY) * CellTextureHeight;
                TerrainInfo terrainInfo = TerrainRes[location.TerrainId];
                Graphics.DrawBitmap(terrainInfo.GetTexture(Graphics), screenX, screenY, Scale(terrainInfo.bitmapWidth), Scale(terrainInfo.bitmapHeight));

                if (location.HasDirt())
                {
                    DrawDirt(location, locX, locY, screenX, screenY);
                }

                //TODO draw snow

                if (location.HasHill())
                {
                    if (location.HillId2() != 0)
                        DrawHill(HillRes[location.HillId2()], screenX, screenY, 1);
                    DrawHill(HillRes[location.HillId1()], screenX, screenY, 1);
                }

                //TODO draw power

                // don't display if a building/object has already been built on the location
                if (location.HasSite() && location.Walkable())
                {
                    DrawSite(location, screenX, screenY);
                }
            }
        }

        DrawTownsGround();
        
        DrawTownsStructures();
        
        DrawUnitPaths();

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

    private void DrawDirt(Location location, int locX, int locY, int screenX, int screenY)
    {
        int dirtArrayId = location.DirtArrayId();
        Rock dirt = DirtArray[dirtArrayId];
        int dirtBlockId = RockRes.LocateBlock(dirt.RockId, locX - dirt.LocX, locY - dirt.LocY);
        if (dirtBlockId != 0)
        {
            int dirtBitmapId = RockRes.GetBitmapId(dirtBlockId, dirt.CurFrame);
            if (dirtBitmapId != 0)
            {
                RockBitmapInfo dirtBitmapInfo = RockRes.GetBitmapInfo(dirtBitmapId);
                Graphics.DrawBitmap(dirtBitmapInfo.GetTexture(Graphics), screenX, screenY,
                    Scale(dirtBitmapInfo.bitmapWidth), Scale(dirtBitmapInfo.bitmapHeight));
            }
        }
    }

    private void DrawHill(HillBlockInfo hillBlockInfo, int screenX, int screenY, int layerMask)
    {
        //TODO check this
        //if((layerMask & hillBlockInfo.layer) == 0)
            //return;

        int hillX = screenX + Scale(hillBlockInfo.offset_x);
        int hillY = screenY + Scale(hillBlockInfo.offset_y);
		
        Graphics.DrawBitmap(hillBlockInfo.GetTexture(Graphics), hillX, hillY, Scale(hillBlockInfo.bitmapWidth), Scale(hillBlockInfo.bitmapHeight));
    }

    private void DrawSite(Location location, int screenX, int screenY)
    {
        Site site = SiteArray[location.SiteId()];
        switch (site.SiteType)
        {
            case Site.SITE_RAW:
                RawInfo rawInfo = RawRes[site.ObjectId];
                Graphics.DrawBitmap(rawInfo.GetLargeRawTexture(Graphics), screenX, screenY, Scale(rawInfo.largeRawIconWidth), Scale(rawInfo.largeRawIconHeight));
                break;
            case Site.SITE_SCROLL:
                RaceInfo raceInfo = RaceRes[site.ObjectId];
                Graphics.DrawBitmap(raceInfo.GetScrollTexture(Graphics), screenX, screenY, Scale(raceInfo.scrollBitmapWidth), Scale(raceInfo.scrollBitmapHeight));
                break;
            case Site.SITE_GOLD_COIN:
                Graphics.DrawBitmap(MonsterRes.GetGoldCoinTexture(Graphics, site.ObjectId), screenX, screenY,
                    Scale(MonsterRes.goldCoinWidth), Scale(MonsterRes.goldCoinHeight));
                break;
        }
        
        //TODO draw selected site
    }

    private void DrawPlants()
    {
        for (int locX = _topLeftLocX; (locX < _topLeftLocX + MainViewWidthInCells) && locX < GameConstants.MapSize; locX++)
        {
            for (int locY = _topLeftLocY; (locY < _topLeftLocY + MainViewHeightInCells) && locY < GameConstants.MapSize; locY++)
            {
                Location location = World.GetLoc(locX, locY);
                if (location.IsExplored() && location.IsPlant())
                {
                    PlantBitmap plantBitmap = PlantRes.get_bitmap(location.PlantId());
                    int drawX = MainViewX + (locX - _topLeftLocX) * CellTextureWidth + Scale(plantBitmap.offset_x);
                    int drawY = MainViewY + (locY - _topLeftLocY) * CellTextureHeight + Scale(plantBitmap.offset_y);
                    Graphics.DrawBitmap(plantBitmap.GetTexture(Graphics), drawX, drawY, Scale(plantBitmap.bitmapWidth), Scale(plantBitmap.bitmapHeight));
                }
            }
        }
    }

    private void DrawTownsGround()
    {
        foreach (Town town in TownArray)
        {
            if (town.LocX2 < _topLeftLocX || town.LocX1 > _topLeftLocX + MainViewWidthInCells)
                continue;
            if (town.LocY2 < _topLeftLocY || town.LocY1 > _topLeftLocY + MainViewHeightInCells)
                continue;
            
            TownLayout townLayout = TownRes.GetLayout(town.LayoutId);
            int townX = MainViewX + (town.LocX1 - _topLeftLocX) * CellTextureWidth;
            int townY = MainViewY + (town.LocY1 - _topLeftLocY) * CellTextureHeight;
            int townLayoutX = townX + (InternalConstants.TOWN_WIDTH * CellTextureWidth - Scale(townLayout.groundBitmapWidth)) / 2;
            int townLayoutY = townY + (InternalConstants.TOWN_HEIGHT * CellTextureHeight - Scale(townLayout.groundBitmapHeight)) / 2;
            Graphics.DrawBitmap(townLayout.GetTexture(Graphics), townLayoutX, townLayoutY, Scale(townLayout.groundBitmapWidth), Scale(townLayout.groundBitmapHeight));
        }
    }

    private void DrawTownsStructures()
    {
        foreach (Town town in TownArray)
        {
            if (town.LocX2 < _topLeftLocX || town.LocX1 > _topLeftLocX + MainViewWidthInCells)
                continue;
            if (town.LocY2 < _topLeftLocY || town.LocY1 > _topLeftLocY + MainViewHeightInCells)
                continue;
            
            int townX = MainViewX + (town.LocX1 - _topLeftLocX) * CellTextureWidth;
            int townY = MainViewY + (town.LocY1 - _topLeftLocY) * CellTextureHeight;
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
            if (firm.LocX2 < _topLeftLocX || firm.LocX1 > _topLeftLocX + MainViewWidthInCells)
                continue;
            if (firm.LocY2 < _topLeftLocY || firm.LocY1 > _topLeftLocY + MainViewHeightInCells)
                continue;

            int firmX = MainViewX + (firm.LocX1 - _topLeftLocX) * CellTextureWidth;
            int firmY = MainViewY + (firm.LocY1 - _topLeftLocY) * CellTextureHeight;

            FirmBuild firmBuild = FirmRes.get_build(firm.FirmBuildId);
            // if in construction, don't draw ground unless the last construction frame
            if (firmBuild.ground_bitmap_recno != 0 &&
                (!firm.UnderConstruction || firm.ConstructionFrame() >= firmBuild.under_construction_bitmap_count - 1))
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
                if (firm.UnderConstruction)
                {
                    DrawFirmFullSize(firm, firmX, firmY, displayLayer);
                }
                else if (!firm.IsOperating())
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
                    DrawFirmFrame(firm, firmX, firmY, firm.CurFrame, displayLayer);
                }
            }
        }
    }

    private void DrawFirmFullSize(Firm firm, int firmX, int firmY, int displayLayer)
    {
        FirmBuild firmBuild = FirmRes.get_build(firm.FirmBuildId);
        if (firm.UnderConstruction)
        {
            //TODO
        }

        FirmBitmap firmBitmap;
        if (firm.UnderConstruction)
        {
            int buildFraction = firm.ConstructionFrame();
            firmBitmap = FirmRes.get_bitmap(firmBuild.under_construction_bitmap_recno + buildFraction);
        }
        else if (!firm.IsOperating())
        {
            firmBitmap = FirmRes.get_bitmap(firmBuild.idle_bitmap_recno);
        }
        else
        {
            firmBitmap = FirmRes.get_bitmap(firmBuild.first_bitmap(firm.CurFrame));
        }

        // ------ check if the display layer is correct ---------//
        if ((firmBitmap.display_layer & displayLayer) == 0)
            return;

        int firmBitmapX = firmX + Scale(firmBitmap.offset_x);
        int firmBitmapY = firmY + Scale(firmBitmap.offset_y);
        Graphics.DrawBitmap(firmBitmap.GetTexture(Graphics, firm.NationId, false), firmBitmapX, firmBitmapY,
            Scale(firmBitmap.bitmapWidth), Scale(firmBitmap.bitmapHeight));

        if (firm.UnderConstruction)
        {
            //TODO
        }
    }

    private void DrawFirmFrame(Firm firm, int firmX, int firmY, int frameId, int displayLayer)
    {
        FirmBuild firmBuild = FirmRes.get_build(firm.FirmBuildId);
        int firstBitmap = firmBuild.first_bitmap(frameId);
        int bitmapCount = firmBuild.bitmap_count(frameId);

        for (int i = 0, bitmapRecno = firstBitmap; i < bitmapCount; i++, bitmapRecno++)
        {
            FirmBitmap firmBitmap = FirmRes.get_bitmap(bitmapRecno);
            int firmBitmapX = firmX + Scale(firmBitmap.offset_x);
            int firmBitmapY = firmY + Scale(firmBitmap.offset_y);
            Graphics.DrawBitmap(firmBitmap.GetTexture(Graphics, firm.NationId, false), firmBitmapX, firmBitmapY,
                Scale(firmBitmap.bitmapWidth), Scale(firmBitmap.bitmapHeight));
        }
    }

    private void DrawUnits()
    {
        foreach (Unit unit in UnitArray)
        {
            // TODO check conditions for big units
            // TODO CurLoc or NextLoc?
            if (unit.CurLocX < _topLeftLocX || unit.CurLocX > _topLeftLocX + MainViewWidthInCells)
                continue;
            if (unit.CurLocY < _topLeftLocY || unit.CurLocY > _topLeftLocY + MainViewHeightInCells)
                continue;

            SpriteFrame spriteFrame = unit.CurSpriteFrame(out bool needMirror);
            int unitX = MainViewX + Scale(unit.CurX) - _topLeftLocX * CellTextureWidth + spriteFrame.OffsetX;
            int unitY = MainViewY + Scale(unit.CurY) - _topLeftLocY * CellTextureHeight + spriteFrame.OffsetY;

            SpriteInfo spriteInfo = SpriteRes[unit.SpriteResId];
            //TODO select only under cursor?
            //bool isSelected = (unit.sprite_recno == _selectedUnitId);
            Graphics.DrawBitmap(spriteFrame.GetUnitTexture(Graphics, spriteInfo, unit.NationId, unit.SelectedFlag), unitX, unitY,
                Scale(spriteFrame.Width), Scale(spriteFrame.Height), needMirror ? FlipMode.Horizontal : FlipMode.None);
        }
    }

    private void DrawUnitPaths()
    {
        if ((Config.show_unit_path & 1) == 0)
            return;

        foreach (Unit unit in UnitArray)
        {
            if (!unit.IsVisible() || !unit.SelectedFlag)
                continue;

            if (!Config.show_ai_info && NationArray.player_recno != 0 && !unit.BelongsToNation(NationArray.player_recno))
                continue;
            
            //TODO draw paths of air units on top of land units

            if (unit.PathNodes.Count > 0)
            {
                if (unit.CurX != unit.GoX || unit.CurY != unit.GoY)
                {
                    //TODO draw part of animated line
                }

                //TODO optimize drawing lines - join them
                for (int i = unit.PathNodeIndex + 1; i < unit.PathNodes.Count; i++)
                {
                    int resultNode1 = unit.PathNodes[i - 1];
                    World.GetLocXAndLocY(resultNode1, out int resultNode1LocX, out int resultNode1LocY);
                    int resultNode2 = unit.PathNodes[i];
                    World.GetLocXAndLocY(resultNode2, out int resultNode2LocX, out int resultNode2LocY);
                    if (resultNode1LocX >= _topLeftLocX - 1 && resultNode1LocX <= _topLeftLocX + MainViewWidthInCells &&
                        resultNode2LocX >= _topLeftLocX - 1 && resultNode2LocX <= _topLeftLocX + MainViewWidthInCells &&
                        resultNode1LocY >= _topLeftLocY - 1 && resultNode1LocY <= _topLeftLocY + MainViewHeightInCells &&
                        resultNode2LocY >= _topLeftLocY - 1 && resultNode2LocY <= _topLeftLocY + MainViewHeightInCells)
                    {
                        int screenX1 = MainViewX + (resultNode1LocX - _topLeftLocX) * CellTextureWidth + CellTextureWidth / 2;
                        int screenX2 = MainViewX + (resultNode2LocX - _topLeftLocX) * CellTextureWidth + CellTextureWidth / 2;
                        int screenY1 = MainViewY + (resultNode1LocY - _topLeftLocY) * CellTextureHeight + CellTextureHeight / 2;
                        int screenY2 = MainViewY + (resultNode2LocY - _topLeftLocY) * CellTextureHeight + CellTextureHeight / 2;
                        DrawAnimatedLine(screenX1, screenY1, screenX2, screenY2);
                    }
                }
            }
        }
    }

    private void CreateAnimatedSegments()
    {
        for (int i = 0; i < _animatedLineSegment.Length; i++)
        {
            byte[] horizontalSegment = new byte[CellTextureWidth * 2];
            for (int j = 0; j < CellTextureWidth; j++)
            {
                horizontalSegment[j] = horizontalSegment[j + CellTextureWidth] = _animatedLineSegment[(i + j) % _animatedLineSegment.Length];
            }

            _horizontalLineTextures.Add(Graphics.CreateTextureFromBmp(horizontalSegment, CellTextureWidth, 2));

            byte[] verticalSegment = new byte[2 * CellTextureHeight];
            for (int j = 0; j < CellTextureHeight; j++)
            {
                verticalSegment[j * 2] = verticalSegment[j * 2 + 1] = _animatedLineSegment[(i + j) % _animatedLineSegment.Length];
            }
            
            _verticalLineTextures.Add(Graphics.CreateTextureFromBmp(verticalSegment, 2, CellTextureHeight));

            byte[] diagonalSegment = new byte[CellTextureWidth * CellTextureHeight];
            for (int j = 0; j < diagonalSegment.Length; j++)
            {
                diagonalSegment[j] = Colors.TRANSPARENT_CODE;
            }

            for (int j = 0; j < CellTextureWidth; j++)
            {
                byte segmentColor = _animatedLineSegment[(i + j) % _animatedLineSegment.Length];
                diagonalSegment[j * CellTextureWidth + j] = segmentColor;
                if (j != 0)
                    diagonalSegment[j * CellTextureWidth + j - 1] = segmentColor;
                if (j != CellTextureWidth - 1)
                    diagonalSegment[j * CellTextureWidth + j + 1] = segmentColor;
            }
            
            _diagonalLineTextures.Add(Graphics.CreateTextureFromBmp(diagonalSegment, CellTextureWidth, CellTextureHeight));
        }
    }

    private void DrawAnimatedLine(int screenX1, int screenY1, int screenX2, int screenY2)
    {
        if (screenX1 == screenX2) // vertical line
        {
            FlipMode flip = (screenY1 < screenY2) ? FlipMode.Vertical : FlipMode.None;
            Graphics.DrawBitmap(_verticalLineTextures[(int)(Sys.Instance.FrameNumber % _verticalLineTextures.Count)],
                screenX1 - 1, (screenY1 < screenY2) ? screenY1 : screenY2, 2, CellTextureHeight, flip);
            return;
        }
        
        if (screenY1 == screenY2) // horizontal line
        {
            FlipMode flip = (screenX1 < screenX2) ? FlipMode.Horizontal : FlipMode.None;
            Graphics.DrawBitmap(_horizontalLineTextures[(int)(Sys.Instance.FrameNumber % _horizontalLineTextures.Count)],
                (screenX1 < screenX2) ? screenX1 : screenX2, screenY1 - 1, CellTextureWidth, 2, flip);
            return;
        }
        
        // diagonal line
        FlipMode diagonalFlip = FlipMode.None;
        if (screenY1 < screenY2)
            diagonalFlip |= FlipMode.Vertical;
        if (screenX1 < screenX2)
            diagonalFlip |= FlipMode.Horizontal;

        Graphics.DrawBitmap(_diagonalLineTextures[(int)(Sys.Instance.FrameNumber % _diagonalLineTextures.Count)],
            (screenX1 < screenX2) ? screenX1 : screenX2, (screenY1 < screenY2) ? screenY1 : screenY2, CellTextureWidth, CellTextureHeight, diagonalFlip);
    }
}