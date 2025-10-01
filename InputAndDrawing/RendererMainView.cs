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
        
        for (int locY = _topLeftLocY; (locY < _topLeftLocY + MainViewHeightInCells) && locY < GameConstants.MapSize; locY++)
        {
            for (int locX = _topLeftLocX; (locX < _topLeftLocX + MainViewWidthInCells) && locX < GameConstants.MapSize; locX++)
            {
                Location location = World.GetLoc(locX, locY);
                if (!location.IsExplored())
                    continue;

                int screenX = MainViewX + (locX - _topLeftLocX) * CellTextureWidth;
                int screenY = MainViewY + (locY - _topLeftLocY) * CellTextureHeight;
                DrawTerrain(location, screenX, screenY);

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

        if (_leftMousePressed)
            DrawSelectionRectangle();
    }

    private void DrawTerrain(Location location, int screenX, int screenY)
    {
        TerrainInfo terrainInfo = TerrainRes[location.TerrainId];
        IntPtr animatedTerrain = terrainInfo.GetAnimationTexture(Graphics, Sys.Instance.FrameNumber / 4);
        if (animatedTerrain != IntPtr.Zero)
            Graphics.DrawBitmap(animatedTerrain, screenX, screenY, Scale(terrainInfo.BitmapWidth), Scale(terrainInfo.BitmapHeight));
        else
            Graphics.DrawBitmap(terrainInfo.GetTexture(Graphics), screenX, screenY, Scale(terrainInfo.BitmapWidth), Scale(terrainInfo.BitmapHeight));
    }

    private void DrawDirt(Location location, int locX, int locY, int screenX, int screenY)
    {
        Rock dirt = DirtArray[location.DirtArrayId()];
        int dirtBlockId = RockRes.LocateBlock(dirt.RockId, locX - dirt.LocX, locY - dirt.LocY);
        if (dirtBlockId != 0)
        {
            int dirtBitmapId = RockRes.GetBitmapId(dirtBlockId, dirt.CurFrame);
            if (dirtBitmapId != 0)
            {
                RockBitmapInfo dirtBitmapInfo = RockRes.GetBitmapInfo(dirtBitmapId);
                Graphics.DrawBitmap(dirtBitmapInfo.GetTexture(Graphics), screenX, screenY,
                    Scale(dirtBitmapInfo.BitmapWidth), Scale(dirtBitmapInfo.BitmapHeight));
            }
        }
    }

    private void DrawHill(HillBlockInfo hillBlockInfo, int screenX, int screenY, int layerMask)
    {
        //TODO check this
        //if((layerMask & hillBlockInfo.Layer) == 0)
            //return;

        int hillX = screenX + Scale(hillBlockInfo.OffsetX);
        int hillY = screenY + Scale(hillBlockInfo.OffsetY);
        Graphics.DrawBitmap(hillBlockInfo.GetTexture(Graphics), hillX, hillY, Scale(hillBlockInfo.BitmapWidth), Scale(hillBlockInfo.BitmapHeight));
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
            Graphics.DrawBitmap(spriteFrame.GetUnitTexture(Graphics, spriteInfo, unit.NationId, false), unitX, unitY,
                Scale(spriteFrame.Width), Scale(spriteFrame.Height), needMirror ? FlipMode.Horizontal : FlipMode.None);

            if (unit is UnitHuman)
            {
                IntPtr skillTexture = unit.Rank switch
                {
                    Unit.RANK_SOLDIER => unit.Skill.SkillId switch
                    {
                        Skill.SKILL_CONSTRUCTION => _constructionTexture,
                        Skill.SKILL_LEADING => _leadershipTexture,
                        Skill.SKILL_MINING => _miningTexture,
                        Skill.SKILL_MFT => _manufactureTexture,
                        Skill.SKILL_RESEARCH => _researchTexture,
                        Skill.SKILL_SPYING => _spyingTexture,
                        Skill.SKILL_PRAYING => _prayingTexture,
                        _ => IntPtr.Zero
                    },
                    Unit.RANK_GENERAL => _generalTexture,
                    Unit.RANK_KING => _kingTexture,
                    _ => IntPtr.Zero
                };

                int skillIconX = unitX - spriteFrame.OffsetX + 32;
                int skillIconY = unitY - spriteFrame.OffsetY - 34;
                int iconWidth = Scale(_skillWidth);
                int iconHeight = Scale(_skillHeight);
                if (skillTexture != IntPtr.Zero)
                {
                    Graphics.DrawBitmap(skillTexture, skillIconX, skillIconY, iconWidth, iconHeight);
                }

                if (unit.SpyId != 0 && (unit.TrueNationId() == NationArray.player_recno || Config.show_ai_info))
                {
                    int spyIconX = skillIconX;
                    int spyIconY = skillTexture != IntPtr.Zero ? skillIconY + iconHeight + 1 : skillIconY;
                    Graphics.DrawBitmap(_spyingTexture, spyIconX, spyIconY, iconWidth, iconHeight);

                    if (Config.show_ai_info)
                    {
                        int color = ColorRemap.GetColorRemap(ColorRemap.ColorSchemes[unit.TrueNationId()], false).MainColor;
                        Graphics.DrawLine(spyIconX, spyIconY, spyIconX + iconWidth - 1, spyIconY, color);
                        Graphics.DrawLine(spyIconX, spyIconY + 1, spyIconX + iconWidth - 1, spyIconY + 1, color);
                        Graphics.DrawLine(spyIconX, spyIconY + iconHeight - 2, spyIconX + iconWidth - 1, spyIconY + iconHeight - 2, color);
                        Graphics.DrawLine(spyIconX, spyIconY + iconHeight - 1, spyIconX + iconWidth - 1, spyIconY + iconHeight - 1, color);
                        Graphics.DrawLine(spyIconX, spyIconY, spyIconX, spyIconY + iconHeight - 1, color);
                        Graphics.DrawLine(spyIconX + 1, spyIconY, spyIconX + 1, spyIconY + iconHeight - 1, color);
                        Graphics.DrawLine(spyIconX + iconWidth - 2, spyIconY, spyIconX + iconWidth - 2, spyIconY + iconHeight - 1, color);
                        Graphics.DrawLine(spyIconX + iconWidth - 1, spyIconY, spyIconX + iconWidth - 1, spyIconY + iconHeight - 1, color);
                    }
                }
            }
        }

        for (int i = 0; i < _selectedUnits.Count; i++)
        {
            Unit selectedUnit = UnitArray[_selectedUnits[i]];
            if (selectedUnit.CurLocX < _topLeftLocX || selectedUnit.CurLocX > _topLeftLocX + MainViewWidthInCells)
                continue;
            if (selectedUnit.CurLocY < _topLeftLocY || selectedUnit.CurLocY > _topLeftLocY + MainViewHeightInCells)
                continue;

            int selectedUnitX = MainViewX + Scale(selectedUnit.CurX) - _topLeftLocX * CellTextureWidth;
            int selectedUnitY = MainViewY + Scale(selectedUnit.CurY) - _topLeftLocY * CellTextureHeight;
            int maxHitBarWidth = 0;
            int hitBarX = 0;
            int hitBarY = 0;
            if (selectedUnit.MobileType == UnitConstants.UNIT_LAND)
            {
                if (UnitRes[selectedUnit.UnitType].unit_class == UnitConstants.UNIT_CLASS_HUMAN)
                    maxHitBarWidth = CellTextureWidth - 16;
                else
                    maxHitBarWidth = CellTextureWidth;
                
                hitBarX = selectedUnitX;
                hitBarY = selectedUnitY - 30;
            }
            else
            {
                maxHitBarWidth = CellTextureWidth * 2 - 32;
                hitBarX = selectedUnitX - CellTextureWidth / 2 + 16;
                hitBarY = selectedUnitY - 30;

                if (selectedUnit.MobileType == UnitConstants.UNIT_AIR)
                    hitBarY -= 30;
            }


            const int HIT_BAR_LIGHT_BORDER = 0;
            const int HIT_BAR_DARK_BORDER = 3;
            const int HIT_BAR_BODY = 1;
            const int NO_BAR_LIGHT_BORDER = 0x40 + 11;
            const int NO_BAR_DARK_BORDER = 0x40 + 3;
            const int NO_BAR_BODY = 0x40 + 7;
            int hitBarColor = 0xA8;
            if (selectedUnit.MaxHitPoints >= 51 && selectedUnit.MaxHitPoints <= 100)
                hitBarColor = 0xB4;
            if (selectedUnit.MaxHitPoints >= 101)
                hitBarColor = 0xAC;
            int separatorX = hitBarX + (maxHitBarWidth - 1) * (int)selectedUnit.HitPoints / selectedUnit.MaxHitPoints;

            Graphics.DrawLine(hitBarX, hitBarY, separatorX, hitBarY, hitBarColor + HIT_BAR_LIGHT_BORDER); //top
            Graphics.DrawLine(hitBarX, hitBarY + 1, separatorX, hitBarY + 1, hitBarColor + HIT_BAR_LIGHT_BORDER); //top
            if (separatorX < hitBarX + maxHitBarWidth - 1)
            {
                Graphics.DrawLine(separatorX + 1, hitBarY, hitBarX + maxHitBarWidth - 1, hitBarY, NO_BAR_LIGHT_BORDER); //top
                Graphics.DrawLine(separatorX + 1, hitBarY + 1, hitBarX + maxHitBarWidth - 1, hitBarY + 1, NO_BAR_LIGHT_BORDER); //top
            }

            Graphics.DrawLine(hitBarX + 2, hitBarY + 4, separatorX, hitBarY + 4, hitBarColor + HIT_BAR_DARK_BORDER); //bottom
            Graphics.DrawLine(hitBarX + 2, hitBarY + 5, separatorX, hitBarY + 5, hitBarColor + HIT_BAR_DARK_BORDER); //bottom
            if (separatorX < hitBarX + maxHitBarWidth - 1)
            {
                Graphics.DrawLine(separatorX + 1, hitBarY + 4, hitBarX + maxHitBarWidth - 1, hitBarY + 4, NO_BAR_DARK_BORDER); //bottom
                Graphics.DrawLine(separatorX + 1, hitBarY + 5, hitBarX + maxHitBarWidth - 1, hitBarY + 5, NO_BAR_DARK_BORDER); //bottom
            }

            Graphics.DrawLine(hitBarX, hitBarY, hitBarX, hitBarY + 5, hitBarColor + HIT_BAR_LIGHT_BORDER); //left
            Graphics.DrawLine(hitBarX + 1, hitBarY, hitBarX + 1, hitBarY + 5, hitBarColor + HIT_BAR_LIGHT_BORDER); //left
            Graphics.DrawLine(hitBarX + maxHitBarWidth - 2, hitBarY + 2, hitBarX + maxHitBarWidth - 2, hitBarY + 3,
                separatorX == hitBarX + maxHitBarWidth - 1 ? hitBarColor + HIT_BAR_DARK_BORDER : NO_BAR_DARK_BORDER); //right
            Graphics.DrawLine(hitBarX + maxHitBarWidth - 1, hitBarY + 2, hitBarX + maxHitBarWidth - 1, hitBarY + 3,
                separatorX == hitBarX + maxHitBarWidth - 1 ? hitBarColor + HIT_BAR_DARK_BORDER : NO_BAR_DARK_BORDER); //right
            
            Graphics.DrawLine(hitBarX + 2, hitBarY + 2, Math.Min(separatorX, hitBarX + maxHitBarWidth - 3), hitBarY + 2, hitBarColor + HIT_BAR_BODY); //body
            Graphics.DrawLine(hitBarX + 2, hitBarY + 3, Math.Min(separatorX, hitBarX + maxHitBarWidth - 3), hitBarY + 3, hitBarColor + HIT_BAR_BODY); //body
            if (separatorX < hitBarX + maxHitBarWidth - 3)
            {
                Graphics.DrawLine(separatorX + 1, hitBarY + 2, hitBarX + maxHitBarWidth - 3, hitBarY + 2, NO_BAR_BODY); //body
                Graphics.DrawLine(separatorX + 1, hitBarY + 3, hitBarX + maxHitBarWidth - 3, hitBarY + 3, NO_BAR_BODY); //body
            }
        }
    }

    private void DrawUnitPaths()
    {
        if ((Config.show_unit_path & 1) == 0)
            return;

        for (int i = 0; i < _selectedUnits.Count; i++)
        {
            Unit unit = UnitArray[_selectedUnits[i]];
            //TODO unit.IsStealth()?
            if (!unit.IsVisible())
                continue;

            //TODO check this
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
                for (int j = unit.PathNodeIndex + 1; j < unit.PathNodes.Count; j++)
                {
                    int resultNode1 = unit.PathNodes[j - 1];
                    World.GetLocXAndLocY(resultNode1, out int resultNode1LocX, out int resultNode1LocY);
                    int resultNode2 = unit.PathNodes[j];
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

    private void DrawSelectionRectangle()
    {
        if (_mouseButtonX < MainViewX || _mouseButtonX >= MainViewX + MainViewWidth)
            return;
        
        if (_mouseButtonY < MainViewY || _mouseButtonY >= MainViewY + MainViewHeight)
            return;
        
        int BoundX(int x)
        {
            x = Math.Max(x, MainViewX);
            x = Math.Min(x, MainViewX + MainViewWidth);
            return x;
        }

        int BoundY(int y)
        {
            y = Math.Max(y, MainViewY);
            y = Math.Min(y, MainViewY + MainViewHeight);
            return y;
        }

        const int Thickness = 3;
        int color = Colors.VGA_YELLOW;

        int x1 = BoundX(Math.Min(_mouseButtonX, _mouseMotionX));
        int x2 = BoundX(Math.Max(_mouseButtonX, _mouseMotionX));
        int y1 = BoundY(Math.Min(_mouseButtonY, _mouseMotionY));
        int y2 = BoundY(Math.Max(_mouseButtonY, _mouseMotionY));
        Graphics.DrawRect(x1, y1, x2 - x1, Thickness, color);
        Graphics.DrawRect(x1, y2 - Thickness, x2 - x1, Thickness, color);
        Graphics.DrawRect(x1, y1, Thickness, y2 - y1, color);
        Graphics.DrawRect(x2 - Thickness, y1, Thickness, y2 - y1, color);
    }
}