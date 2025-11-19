using System;
using System.Collections.Generic;

namespace TenKingdoms;

public partial class Renderer
{
    private readonly int[] _waveHeights = { 8, 6, 4, 2, 0, 2, 4, 6 };
    private readonly byte[] _animatedLineSegment = { 0x90, 0x93, 0x98, 0x9c, 0x9f, 0x9c, 0x98, 0x93 };
    private readonly List<IntPtr> _horizontalLineTextures = new List<nint>();
    private readonly List<IntPtr> _verticalLineTextures = new List<nint>();
    private readonly List<IntPtr> _diagonalLineTextures = new List<nint>();
    private readonly List<DisplayableObject> _displayableObjects = new List<DisplayableObject>();
    private int _displayableObjectIndex;
    private readonly List<DisplayableObject> _objectsToDrawBottom = new List<DisplayableObject>();
    private readonly List<DisplayableObject> _objectsToDraw = new List<DisplayableObject>();
    private readonly List<DisplayableObject> _objectsToDrawTop = new List<DisplayableObject>();
    private readonly List<DisplayableObject> _objectsToDrawAir = new List<DisplayableObject>();
    private readonly List<Town> _townsToDraw = new List<Town>();
    private readonly List<Firm> _firmsToDraw = new List<Firm>();
    private readonly List<Unit> _airAndSeaUnitsToDraw = new List<Unit>();
    private Unit _pointingUnit;

    private void DrawMainView()
    {
        Graphics.SetClipRectangle(MainViewX, MainViewY, MainViewX + MainViewWidthInCells * CellTextureWidth, MainViewY + MainViewHeightInCells * CellTextureHeight);
        for (int i = 0; i < _displayableObjects.Count; i++)
            _displayableObjects[i].ObjectType = DisplayableObjectType.None;
        _displayableObjectIndex = 0;
        
        _objectsToDrawBottom.Clear();
        _objectsToDraw.Clear();
        _objectsToDrawTop.Clear();
        _objectsToDrawAir.Clear();
        _townsToDraw.Clear();
        _firmsToDraw.Clear();
        _airAndSeaUnitsToDraw.Clear();
        _pointingUnit = null;

        DrawGround();

        int startLocX = Math.Max(_topLeftLocX - InternalConstants.DETECT_MARGIN, 0);
        int endLocX = Math.Min(_topLeftLocX + MainViewWidthInCells + InternalConstants.DETECT_MARGIN, GameConstants.MapSize);
        int startLocY = Math.Max(_topLeftLocY - InternalConstants.DETECT_MARGIN, 0);
        int endLocY = Math.Min(_topLeftLocY + MainViewHeightInCells + InternalConstants.DETECT_MARGIN, GameConstants.MapSize);

        for (int locY = startLocY; locY < endLocY; locY++)
        {
            for (int locX = startLocX; locX < endLocX; locX++)
            {
                Location location = World.GetLoc(locX, locY);

                if (location.IsTown())
                {
                    Town town = TownArray[location.TownId()];
                    if (!_townsToDraw.Contains(town))
                    {
                        DisplayableObject displayableObject = GetDisplayableObject();
                        displayableObject.ObjectType = DisplayableObjectType.Town;
                        displayableObject.ObjectId = town.TownId;
                        displayableObject.DrawY2 = GetScreenXAndY(town.LocX2, town.LocY2 + 1).Item2 - 1;
                        _objectsToDrawBottom.Add(displayableObject);
                        _objectsToDraw.Add(displayableObject);
                        _townsToDraw.Add(town);
                    }
                }

                if (location.IsFirm())
                {
                    Firm firm = FirmArray[location.FirmId()];
                    if (!_firmsToDraw.Contains(firm))
                    {
                        DisplayableObject displayableObject = GetDisplayableObject();
                        displayableObject.ObjectType = DisplayableObjectType.Firm;
                        displayableObject.ObjectId = firm.FirmId;
                        displayableObject.DrawY2 = GetScreenXAndY(firm.LocX2, firm.LocY2 + 1).Item2 - 1;
                        _objectsToDrawBottom.Add(displayableObject);
                        _objectsToDraw.Add(displayableObject);
                        _firmsToDraw.Add(firm);
                    }
                }

                if (location.HasSite() && location.Walkable())
                {
                    DisplayableObject displayableObject = GetDisplayableObject();
                    displayableObject.ObjectType = DisplayableObjectType.Site;
                    displayableObject.ObjectId = location.SiteId();
                    displayableObject.DrawY2 = GetScreenXAndY(locX, locY + 1).Item2 - 1;
                    _objectsToDrawBottom.Add(displayableObject);
                }

                if (location.HasUnit(UnitConstants.UNIT_LAND))
                {
                    Unit unit = UnitArray[location.UnitId(UnitConstants.UNIT_LAND)];
                    if (!unit.IsStealth())
                    {
                        DisplayableObject displayableObject = GetDisplayableObject();
                        displayableObject.ObjectType = DisplayableObjectType.Unit;
                        displayableObject.ObjectId = unit.SpriteId;
                        displayableObject.DrawY2 = GetSpriteDrawY2(unit);
                        _objectsToDraw.Add(displayableObject);
                    }
                }
                
                if (location.HasUnit(UnitConstants.UNIT_SEA))
                {
                    UnitMarine unit = (UnitMarine)UnitArray[location.UnitId(UnitConstants.UNIT_SEA)];
                    if (!unit.IsStealth() && !_airAndSeaUnitsToDraw.Contains(unit))
                    {
                        DisplayableObject displayableObject = GetDisplayableObject();
                        displayableObject.ObjectType = DisplayableObjectType.Unit;
                        displayableObject.ObjectId = unit.SpriteId;
                        displayableObject.DrawY2 = GetSpriteDrawY2(unit);
                        _objectsToDraw.Add(displayableObject);
                        _airAndSeaUnitsToDraw.Add(unit);
                    }
                }
                
                if (location.HasUnit(UnitConstants.UNIT_AIR))
                {
                    Unit unit = UnitArray[location.UnitId(UnitConstants.UNIT_AIR)];
                    if (!unit.IsStealth() && !_airAndSeaUnitsToDraw.Contains(unit))
                    {
                        DisplayableObject displayableObject = GetDisplayableObject();
                        displayableObject.ObjectType = DisplayableObjectType.Unit;
                        displayableObject.ObjectId = unit.SpriteId;
                        displayableObject.DrawY2 = GetSpriteDrawY2(unit);
                        _objectsToDrawAir.Add(displayableObject);
                        _airAndSeaUnitsToDraw.Add(unit);
                    }
                }

                if (location.IsPlant())
                {
                    PlantBitmap plantBitmap = PlantRes.GetBitmap(location.PlantId());
                    DisplayableObject displayableObject = GetDisplayableObject();
                    displayableObject.ObjectType = DisplayableObjectType.Plant;
                    displayableObject.ObjectId = location.PlantId();
                    displayableObject.DrawLocX = locX;
                    displayableObject.DrawLocY = locY;
                    displayableObject.DrawY2 = GetScreenXAndY(locX, locY).Item2 + location.PlantInnerY() - CellTextureHeight / 2
                                         + Scale(plantBitmap.OffsetY) + Scale(plantBitmap.BitmapHeight) - 1;
                    _objectsToDraw.Add(displayableObject);
                }

                if (location.HasHill())
                {
                    HillBlockInfo hillBlockInfo = HillRes[location.HillId1()];
                    if (hillBlockInfo.Layer == TopLayer)
                    {
                        DisplayableObject displayableObject = GetDisplayableObject();
                        displayableObject.ObjectType = DisplayableObjectType.Hill;
                        displayableObject.ObjectId = location.HillId1();
                        displayableObject.DrawLocX = locX;
                        displayableObject.DrawLocY = locY;
                        displayableObject.DrawY2 = GetScreenXAndY(locX, locY + 1).Item2 - 1;
                        _objectsToDrawTop.Add(displayableObject);
                    }
                }

                if (location.FireStrength() > 0)
                {
                    DisplayableObject displayableObject = GetDisplayableObject();
                    displayableObject.ObjectType = DisplayableObjectType.Fire;
                    displayableObject.ObjectId = location.FireStrength();
                    displayableObject.DrawLocX = locX;
                    displayableObject.DrawLocY = locY;
                    displayableObject.DrawY2 = GetScreenXAndY(locX, locY + 1).Item2 - ((locX + 13) * (locY + 17)) % (CellTextureHeight / 2);
                    _objectsToDraw.Add(displayableObject);
                    _objectsToDrawTop.Add(displayableObject);
                }
            }
        }

        foreach (FirmDie firmDie in FirmDieArray)
        {
            if (firmDie.LocX2 >= startLocX && firmDie.LocX1 <= endLocX && firmDie.LocY2 >= startLocY && firmDie.LocY1 <= endLocY)
            {
                DisplayableObject displayableObject = GetDisplayableObject();
                displayableObject.ObjectType = DisplayableObjectType.FirmDie;
                displayableObject.ObjectId = firmDie.FirmDieId;
                displayableObject.DrawY2 = GetScreenXAndY(firmDie.LocX2, firmDie.LocY2 + 1).Item2 - 1;
                _objectsToDrawBottom.Add(displayableObject);
                _objectsToDraw.Add(displayableObject);
            }
        }

        foreach (Bullet bullet in BulletArray)
        {
            if (bullet.IsStealth())
                continue;
            
            if (bullet.CurLocX >= startLocX && bullet.CurLocX <= endLocX && bullet.CurLocY >= startLocY && bullet.CurLocY <= endLocY)
            {
                DisplayableObject displayableObject = GetDisplayableObject();
                displayableObject.ObjectType = DisplayableObjectType.Bullet;
                displayableObject.ObjectId = bullet.SpriteId;
                displayableObject.DrawY2 = GetSpriteDrawY2(bullet);
                if (bullet.DisplayLayer() == NormalLayer)
                    _objectsToDraw.Add(displayableObject);
                if (bullet.DisplayLayer() == TopLayer)
                    _objectsToDrawTop.Add(displayableObject);
                if (bullet.DisplayLayer() == AirLayer)
                    _objectsToDrawAir.Add(displayableObject);
            }
        }

        foreach (Effect effect in EffectArray)
        {
            if (effect.IsStealth())
                continue;
            
            if (effect.CurLocX >= startLocX && effect.CurLocX <= endLocX && effect.CurLocY >= startLocY && effect.CurLocY <= endLocY)
            {
                DisplayableObject displayableObject = GetDisplayableObject();
                displayableObject.ObjectType = DisplayableObjectType.Effect;
                displayableObject.ObjectId = effect.SpriteId;
                displayableObject.DrawY2 = GetSpriteDrawY2(effect);
                if (effect.DisplayLayer == NormalLayer)
                    _objectsToDraw.Add(displayableObject);
                if (effect.DisplayLayer == TopLayer)
                    _objectsToDrawTop.Add(displayableObject);
                if (effect.DisplayLayer == AirLayer)
                    _objectsToDrawAir.Add(displayableObject);
            }
        }

        foreach (Tornado tornado in TornadoArray)
        {
            if (tornado.IsStealth())
                continue;
            
            if (tornado.CurLocX >= startLocX && tornado.CurLocX <= endLocX && tornado.CurLocY >= startLocY && tornado.CurLocY <= endLocY)
            {
                DisplayableObject displayableObject = GetDisplayableObject();
                displayableObject.ObjectType = DisplayableObjectType.Tornado;
                displayableObject.ObjectId = tornado.SpriteId;
                displayableObject.DrawY2 = GetSpriteDrawY2(tornado);
                _objectsToDrawAir.Add(displayableObject);
            }
        }

        if (_mouseMotionX >= MainViewX && _mouseMotionX < MainViewX + MainViewWidth &&
            _mouseMotionY >= MainViewY && _mouseMotionY < MainViewY + MainViewHeight)
        {
            Location pointingLocation = GetPointingLocation(_mouseMotionX, _mouseMotionY, out int mobileType);
            if (mobileType == UnitConstants.UNIT_LAND || mobileType == UnitConstants.UNIT_SEA || mobileType == UnitConstants.UNIT_AIR)
            {
                _pointingUnit = UnitArray[pointingLocation.UnitId(mobileType)];
            }
        }

        _objectsToDrawBottom.Sort((x, y) => x.DrawY2 - y.DrawY2);
        _objectsToDraw.Sort((x, y) => x.DrawY2 - y.DrawY2);
        _objectsToDrawTop.Sort((x, y) => x.DrawY2 - y.DrawY2);
        _objectsToDrawAir.Sort((x, y) => x.DrawY2 - y.DrawY2);

        for (int i = 0; i < _objectsToDrawBottom.Count; i++)
        {
            _objectsToDrawBottom[i].Draw(this, BottomLayer);
        }
        DrawUnitPaths(NormalLayer);
        for (int i = 0; i < _objectsToDraw.Count; i++)
        {
            _objectsToDraw[i].Draw(this, NormalLayer);
        }
        for (int i = 0; i < _objectsToDrawTop.Count; i++)
        {
            _objectsToDrawTop[i].Draw(this, TopLayer);
        }
        DrawUnitPaths(AirLayer);
        for (int i = 0; i < _objectsToDrawAir.Count; i++)
        {
            _objectsToDrawAir[i].Draw(this, AirLayer);
        }
        
        //TODO draw way points
        //TODO add fire sound
        
        if (_leftMousePressed)
            DrawSelectionRectangle();
    }

    private DisplayableObject GetDisplayableObject()
    {
        DisplayableObject result;
        if (_displayableObjectIndex < _displayableObjects.Count)
        {
            result = _displayableObjects[_displayableObjectIndex];
        }
        else
        {
            result = new DisplayableObject();
            _displayableObjects.Add(result);
        }

        _displayableObjectIndex++;

        return result;
    }

    private void DrawGround()
    {
        for (int locY = _topLeftLocY; locY < _topLeftLocY + MainViewHeightInCells && locY < GameConstants.MapSize; locY++)
        {
            for (int locX = _topLeftLocX; locX < _topLeftLocX + MainViewWidthInCells && locX < GameConstants.MapSize; locX++)
            {
                Location location = World.GetLoc(locX, locY);
                if (!location.IsExplored())
                    continue;

                DrawTerrain(location, locX, locY);

                if (location.HasDirt())
                {
                    DrawDirtOrRock(DirtArray[location.DirtArrayId()], locX, locY);
                }
                
                if (location.IsRock())
                {
                    DrawDirtOrRock(RockArray[location.RockArrayId()], locX, locY);
                }

                //TODO draw snow

                if (location.HasHill())
                {
                    if (location.HillId2() != 0)
                        DrawHill(HillRes[location.HillId2()], NormalLayer, locX, locY);
                    DrawHill(HillRes[location.HillId1()], NormalLayer, locX, locY);
                }

                //TODO draw power
            }
        }
    }

    private void DrawTerrain(Location location, int locX, int locY)
    {
        (int screenX, int screenY) = GetScreenXAndY(locX, locY);
        TerrainInfo terrainInfo = TerrainRes[location.TerrainId];
        IntPtr animatedTerrain = terrainInfo.GetAnimationTexture(Graphics, Sys.Instance.FrameNumber / 4);
        if (animatedTerrain != IntPtr.Zero)
            Graphics.DrawBitmapScaled(animatedTerrain, screenX, screenY, terrainInfo.BitmapWidth, terrainInfo.BitmapHeight);
        else
            Graphics.DrawBitmapScaled(terrainInfo.GetTexture(Graphics), screenX, screenY, terrainInfo.BitmapWidth, terrainInfo.BitmapHeight);
    }

    public void DrawHill(HillBlockInfo hillBlockInfo, int layer, int locX, int locY)
    {
        if (hillBlockInfo.Layer != layer)
            return;

        (int screenX, int screenY) = GetScreenXAndY(locX, locY);
        int hillX = screenX + Scale(hillBlockInfo.OffsetX);
        int hillY = screenY + Scale(hillBlockInfo.OffsetY);
        Graphics.DrawBitmapScaled(hillBlockInfo.GetTexture(Graphics), hillX, hillY, hillBlockInfo.BitmapWidth, hillBlockInfo.BitmapHeight);
    }

    private void DrawDirtOrRock(Rock dirtOrRock, int locX, int locY)
    {
        (int screenX, int screenY) = GetScreenXAndY(locX, locY);
        int rockBlockId = RockRes.LocateBlock(dirtOrRock.RockId, locX - dirtOrRock.LocX, locY - dirtOrRock.LocY);
        if (rockBlockId != 0)
        {
            int rockBitmapId = RockRes.GetBitmapId(rockBlockId, dirtOrRock.CurFrame);
            if (rockBitmapId != 0)
            {
                RockBitmapInfo rockBitmapInfo = RockRes.GetBitmapInfo(rockBitmapId);
                Graphics.DrawBitmapScaled(rockBitmapInfo.GetTexture(Graphics), screenX, screenY, rockBitmapInfo.BitmapWidth, rockBitmapInfo.BitmapHeight);
            }
        }
    }
    
    public void DrawPlant(PlantBitmap plantBitmap, int locX, int locY)
    {
        Location location = World.GetLoc(locX, locY);
        (int screenX, int screenY) = GetScreenXAndY(locX, locY);
        int drawX = screenX + location.PlantInnerX() - CellTextureWidth / 2 + Scale(plantBitmap.OffsetX);
        int drawY = screenY + location.PlantInnerY() - CellTextureHeight / 2 + Scale(plantBitmap.OffsetY);
        Graphics.DrawBitmapScaled(plantBitmap.GetTexture(Graphics), drawX, drawY, plantBitmap.BitmapWidth, plantBitmap.BitmapHeight);
    }

    public void DrawFlame()
    {
        //TODO implement
    }
    
    public void DrawTown(Town town, int layer)
    {
        (int townX, int townY) = GetScreenXAndY(town.LocX1, town.LocY1);
        TownLayout townLayout = TownRes.GetLayout(town.LayoutId);

        switch (layer)
        {
            case BottomLayer:
                int townLayoutX = townX + (InternalConstants.TOWN_WIDTH * CellTextureWidth - Scale(townLayout.groundBitmapWidth)) / 2;
                int townLayoutY = townY + (InternalConstants.TOWN_HEIGHT * CellTextureHeight - Scale(townLayout.groundBitmapHeight)) / 2;
                Graphics.DrawBitmapScaled(townLayout.GetTexture(Graphics), townLayoutX, townLayoutY, townLayout.groundBitmapWidth, townLayout.groundBitmapHeight);
                break;
            
            case NormalLayer:
                bool isSelected = (town.TownId == _selectedTownId);
                for (int i = 0; i < townLayout.SlotCount; i++)
                {
                    TownSlot townSlot = TownRes.GetSlot(townLayout.FirstSlotId + i);
                    switch (townSlot.BuildType)
                    {
                        case TownSlot.TOWN_OBJECT_HOUSE:
                            TownBuild townBuild = TownRes.GetBuild(town.SlotObjectIds[i]);
                            int townBuildX = townX + Scale(townSlot.BaseX) - Scale(townBuild.bitmapWidth) / 2;
                            int townBuildY = townY + Scale(townSlot.BaseY) - Scale(townBuild.bitmapHeight);
                            Graphics.DrawBitmapScaled(townBuild.GetTexture(Graphics, town.NationId, isSelected), townBuildX, townBuildY,
                                townBuild.bitmapWidth, townBuild.bitmapHeight);
                            break;

                        case TownSlot.TOWN_OBJECT_PLANT:
                            PlantBitmap plantBitmap = PlantRes.GetBitmap(town.SlotObjectIds[i]);
                            int townPlantX = townX + Scale(townSlot.BaseX) - Scale(plantBitmap.BitmapWidth) / 2;
                            int townPlantY = townY + Scale(townSlot.BaseY) - Scale(plantBitmap.BitmapHeight);
                            Graphics.DrawBitmapScaled(plantBitmap.GetTexture(Graphics), townPlantX, townPlantY, plantBitmap.BitmapWidth, plantBitmap.BitmapHeight);
                            break;

                        case TownSlot.TOWN_OBJECT_FARM:
                            int farmIndex = townSlot.BuildCode - 1;
                            int townFarmX = townX + Scale(townSlot.BaseX);
                            int townFarmY = townY + Scale(townSlot.BaseY);
                            var farmTexture = TownRes.GetFarmTexture(Graphics, farmIndex);
                            Graphics.DrawBitmapScaled(farmTexture, townFarmX, townFarmY, TownRes.FarmWidths[farmIndex], TownRes.FarmHeights[farmIndex]);
                            break;

                        case TownSlot.TOWN_OBJECT_FLAG:
                            if (town.NationId == 0 && town.RebelId == 0)
                                break;

                            //TODO fix one flag slot with base_x == 57
                            int flagIndex = (int)(((Sys.Instance.FrameNumber + town.TownId) % 8) / 2);
                            int townFlagX = townX + Scale(townSlot.BaseX) + Scale(TownFlagShiftX);
                            int townFlagY = townY + Scale(townSlot.BaseY) + Scale(TownFlagShiftY);
                            var flagTexture = TownRes.GetFlagTexture(Graphics, flagIndex, town.NationId);
                            Graphics.DrawBitmapScaled(flagTexture, townFlagX, townFlagY, TownRes.FlagWidths[flagIndex], TownRes.FlagHeights[flagIndex]);
                            break;
                    }
                }
                break;
        }
    }

    public void DrawFirm(Firm firm, int layer)
    {
        (int firmX, int firmY) = GetScreenXAndY(firm.LocX1, firm.LocY1);
        FirmBuild firmBuild = FirmRes.GetBuild(firm.FirmBuildId);
        // if in construction, don't draw ground unless the last construction frame
        if (firmBuild.GroundBitmapId != 0 &&
            (!firm.UnderConstruction || firm.ConstructionFrame() >= firmBuild.UnderConstructionBitmapCount - 1))
        {
            FirmBitmap firmBitmap = FirmRes.GetBitmap(firmBuild.GroundBitmapId);
            if (firmBitmap.DisplayLayer == layer)
            {
                bool isSelected = (firm.FirmId == _selectedFirmId);
                int firmBitmapX = firmX + Scale(firmBitmap.OffsetX);
                int firmBitmapY = firmY + Scale(firmBitmap.OffsetY);
                Graphics.DrawBitmapScaled(firmBitmap.GetTexture(Graphics, firm.NationId, isSelected), firmBitmapX, firmBitmapY, firmBitmap.BitmapWidth, firmBitmap.BitmapHeight);
            }
        }
        
        if (firmBuild.AnimateFullSize)
        {
            DrawFirmFullSize(firm, firmX, firmY, layer);
        }
        else
        {
            if (firm.UnderConstruction)
            {
                DrawFirmFullSize(firm, firmX, firmY, layer);
            }
            else if (!firm.IsOperating())
            {
                if (FirmRes.GetBitmap(firmBuild.IdleBitmapId) != null)
                {
                    DrawFirmFullSize(firm, firmX, firmY, layer);
                }
                else
                {
                    DrawFirmFrame(firm, firmX, firmY, 1, layer);
                    DrawFirmFrame(firm, firmX, firmY, 2, layer);
                }
            }
            else
            {
                // the first frame is the common frame for multi-segment bitmaps
                DrawFirmFrame(firm, firmX, firmY, 1, layer);
                DrawFirmFrame(firm, firmX, firmY, firm.CurFrame, layer);
            }
        }

        if (firm.FirmType == Firm.FIRM_MINE)
        {
            FirmMine mine = (FirmMine)firm;
            if (layer == NormalLayer && firm.ShouldShowInfo() && !firm.UnderConstruction && mine.RawId != 0)
            {
                int cargoCount = (int)(Firm.MAX_CARGO * mine.StockQty / mine.MaxStockQty);
                RawInfo rawInfo = RawRes[mine.RawId];
                DrawFirmCargo(Math.Max(cargoCount, 1), firmX, firmY, rawInfo.GetSmallRawTexture(Graphics), rawInfo.SmallRawIconWidth, rawInfo.SmallRawIconHeight);
            }
        }

        if (firm.FirmType == Firm.FIRM_FACTORY)
        {
            FirmFactory factory = (FirmFactory)firm;
            if (layer == NormalLayer && firm.ShouldShowInfo() && !firm.UnderConstruction && factory.ProductRawId != 0)
            {
                int cargoCount = (int)(Firm.MAX_CARGO * factory.StockQty / factory.MaxStockQty);
                RawInfo rawInfo = RawRes[factory.ProductRawId];
                DrawFirmCargo(Math.Max(cargoCount, 1), firmX, firmY, rawInfo.GetSmallProductTexture(Graphics), rawInfo.SmallProductIconWidth, rawInfo.SmallProductIconHeight);
            }
        }

        if (firm.FirmType == Firm.FIRM_MARKET)
        {
            if (layer == NormalLayer && !firm.UnderConstruction)
            {
                FirmMarket market = (FirmMarket)firm;
                for (int i = 0; i < GameConstants.MAX_MARKET_GOODS; i++)
                {
                    IntPtr texture = IntPtr.Zero;
                    int width = 0;
                    int height = 0;
                    
                    MarketGoods marketGoods = market.market_goods_array[i];
                    if (marketGoods.RawId != 0)
                    {
                        RawInfo rawInfo = RawRes[marketGoods.RawId];
                        texture = rawInfo.GetSmallRawTexture(Graphics);
                        width = rawInfo.SmallRawIconWidth;
                        height = rawInfo.SmallRawIconHeight;
                    }

                    if (marketGoods.ProductId != 0)
                    {
                        RawInfo rawInfo = RawRes[marketGoods.ProductId];
                        texture = rawInfo.GetSmallProductTexture(Graphics);
                        width = rawInfo.SmallProductIconWidth;
                        height = rawInfo.SmallProductIconHeight;
                    }

                    if (texture != IntPtr.Zero)
                    {
                        int count = Math.Max((int)(Firm.MAX_CARGO * marketGoods.StockQty / market.MaxStockQty), 1);
                        for (int j = 0; j < count; j++)
                        {
                            Graphics.DrawBitmapScaled(texture, firmX + _marketSectionX[i] + _marketCargoX[j], firmY + _marketSectionY[i] + _marketCargoY[j], width, height);
                        }
                    }
                }
            }
        }
    }

    private void DrawFirmFullSize(Firm firm, int firmX, int firmY, int displayLayer)
    {
        if (firm.UnderConstruction)
        {
            FirmInfo firmInfo = FirmRes[firm.FirmType];
            int flag1X = firmX;
            Graphics.DrawBitmapScaled(firmInfo.GetFlagTexture(Graphics, firm.NationId), flag1X, firmY, firmInfo.FlagBitmapWidth, firmInfo.FlagBitmapHeight);
            int flag2X = MainViewX + (firm.LocX2 + 1 - _topLeftLocX) * CellTextureWidth - Scale(firmInfo.FlagBitmapWidth);
            Graphics.DrawBitmapScaled(firmInfo.GetFlagTexture(Graphics, firm.NationId), flag2X, firmY, firmInfo.FlagBitmapWidth, firmInfo.FlagBitmapHeight);
        }

        FirmBuild firmBuild = FirmRes.GetBuild(firm.FirmBuildId);
        FirmBitmap firmBitmap;
        if (firm.UnderConstruction)
        {
            int buildFraction = firm.ConstructionFrame();
            firmBitmap = FirmRes.GetBitmap(firmBuild.UnderConstructionBitmapId + buildFraction);
        }
        else if (!firm.IsOperating())
        {
            firmBitmap = FirmRes.GetBitmap(firmBuild.IdleBitmapId);
        }
        else
        {
            firmBitmap = FirmRes.GetBitmap(firmBuild.FirstBitmap(firm.CurFrame));
        }

        if (firmBitmap == null || firmBitmap.DisplayLayer != displayLayer)
            return;

        bool isSelected = (firm.FirmId == _selectedFirmId);
        int firmBitmapX = firmX + Scale(firmBitmap.OffsetX);
        int firmBitmapY = firmY + Scale(firmBitmap.OffsetY);
        Graphics.DrawBitmapScaled(firmBitmap.GetTexture(Graphics, firm.NationId, isSelected), firmBitmapX, firmBitmapY, firmBitmap.BitmapWidth, firmBitmap.BitmapHeight);

        if (firm.UnderConstruction)
        {
            FirmInfo firmInfo = FirmRes[firm.FirmType];
            int flag3X = firmX;
            int flag3Y = MainViewY + (firm.LocY2 + 1 - _topLeftLocY) * CellTextureHeight - Scale(firmInfo.FlagBitmapHeight);
            Graphics.DrawBitmapScaled(firmInfo.GetFlagTexture(Graphics, firm.NationId), flag3X, flag3Y, firmInfo.FlagBitmapWidth, firmInfo.FlagBitmapHeight);
            int flag4X = MainViewX + (firm.LocX2 + 1 - _topLeftLocX) * CellTextureWidth - Scale(firmInfo.FlagBitmapWidth);
            int flag4Y = flag3Y;
            Graphics.DrawBitmapScaled(firmInfo.GetFlagTexture(Graphics, firm.NationId), flag4X, flag4Y, firmInfo.FlagBitmapWidth, firmInfo.FlagBitmapHeight);
        }
    }

    private void DrawFirmFrame(Firm firm, int firmX, int firmY, int frameId, int layer)
    {
        FirmBuild firmBuild = FirmRes.GetBuild(firm.FirmBuildId);
        int firstBitmap = firmBuild.FirstBitmap(frameId);
        int bitmapCount = firmBuild.BitmapCount(frameId);

        for (int i = 0, bitmapId = firstBitmap; i < bitmapCount; i++, bitmapId++)
        {
            FirmBitmap firmBitmap = FirmRes.GetBitmap(bitmapId);
            if (firmBitmap.DisplayLayer == layer)
            {
                bool isSelected = (firm.FirmId == _selectedFirmId);
                int firmBitmapX = firmX + Scale(firmBitmap.OffsetX);
                int firmBitmapY = firmY + Scale(firmBitmap.OffsetY);
                Graphics.DrawBitmapScaled(firmBitmap.GetTexture(Graphics, firm.NationId, isSelected), firmBitmapX, firmBitmapY, firmBitmap.BitmapWidth, firmBitmap.BitmapHeight);
            }
        }
    }

    private int[] _cargoX = { 1, 12, 22, 1, 12, 22, 1, 12, 22 };
    private int[] _cargoY = { 129, 126, 129, 119, 116, 119, 108, 105, 108 };
    private int[] _marketSectionX = { 60, 44, 36 };
    private int[] _marketSectionY = { 45, 63, 84 };
    private int[] _marketCargoX = { 0, 9, 18, 12, 21, 30, 24, 33, 42 };
    private int[] _marketCargoY = { 0, 1, 3, 9, 10, 12, 18, 19, 21 };
    private void DrawFirmCargo(int count, int firmX, int firmY, IntPtr texture, int width, int height)
    {
        for (int i = 0; i < count; i++)
        {
            Graphics.DrawBitmapScaled(texture, firmX + _cargoX[i], firmY + _cargoY[i], width, height);
        }
    }

    public void DrawFirmDie(FirmDie firmDie, int layer)
    {
        FirmBuild firmBuild = FirmDieRes.GetBuild(firmDie.FirmBuildId);
        int firstBitmap = firmBuild.FirstBitmap(firmDie.Frame);
        FirmBitmap firmDieBitmap = FirmDieRes.GetBitmap(firstBitmap);
        if (firmDieBitmap.DisplayLayer == layer)
        {
            (int firmDieX, int firmDieY) = GetScreenXAndY(firmDie.LocX1, firmDie.LocY1);
            int firmBitmapX = firmDieX + Scale(firmDieBitmap.OffsetX);
            int firmBitmapY = firmDieY + Scale(firmDieBitmap.OffsetY);
            Graphics.DrawBitmapScaled(firmDieBitmap.GetTexture(Graphics, firmDie.NationId, false),
                firmBitmapX, firmBitmapY, firmDieBitmap.BitmapWidth, firmDieBitmap.BitmapHeight);
        }
    }

    public void DrawSite(Site site, int layer)
    {
        (int siteX, int siteY) = GetScreenXAndY(site.LocX, site.LocY);

        switch (site.SiteType)
        {
            case Site.SITE_RAW:
                RawInfo rawInfo = RawRes[site.ObjectId];
                Graphics.DrawBitmapScaled(rawInfo.GetLargeRawTexture(Graphics), siteX, siteY, rawInfo.LargeRawIconWidth, rawInfo.LargeRawIconHeight);
                break;
            case Site.SITE_SCROLL:
                RaceInfo raceInfo = RaceRes[site.ObjectId];
                Graphics.DrawBitmapScaled(raceInfo.GetScrollTexture(Graphics), siteX, siteY, raceInfo.scrollBitmapWidth, raceInfo.scrollBitmapHeight);
                break;
            case Site.SITE_GOLD_COIN:
                Graphics.DrawBitmapScaled(MonsterRes.GetGoldCoinTexture(Graphics, site.ObjectId), siteX, siteY, MonsterRes.goldCoinWidth, MonsterRes.goldCoinHeight);
                break;
        }

        if (site.SiteId == _selectedSiteId)
        {
            const int thickness = 2;
            const int color = Colors.VGA_YELLOW;

            Graphics.DrawRect(siteX, siteY, CellTextureWidth, thickness, color);
            Graphics.DrawRect(siteX, siteY + CellTextureHeight - thickness, CellTextureWidth, thickness, color);
            Graphics.DrawRect(siteX, siteY, thickness, CellTextureHeight, color);
            Graphics.DrawRect(siteX + CellTextureWidth - thickness, siteY, thickness, CellTextureHeight, color);
        }
    }

    public void DrawUnit(Unit unit, int layer)
    {
        (int unitX, int unitY) = GetSpriteScreenXAndY(unit);
        SpriteFrame spriteFrame = unit.CurSpriteFrame(out bool needMirror);
        SpriteInfo spriteInfo = SpriteRes[unit.SpriteResId];
        Graphics.DrawBitmapScaled(spriteFrame.GetSpriteTexture(Graphics, spriteInfo, unit.NationId, unit == _pointingUnit), unitX, unitY,
            spriteFrame.Width, spriteFrame.Height, needMirror ? FlipMode.Horizontal : FlipMode.None);

        //Draw skill icons
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

            int skillIconX = unitX - Scale(spriteFrame.OffsetX) + 32;
            int skillIconY = unitY - Scale(spriteFrame.OffsetY) - 37;
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
        
        //TODO draw splash for sea units
        
        if (_selectedUnits.Contains(unit.SpriteId))
        {
            int maxHitBarWidth = 0;
            int hitBarX = 0;
            int hitBarY = 0;
            if (unit.MobileType == UnitConstants.UNIT_LAND)
            {
                if (UnitRes[unit.UnitType].unit_class == UnitConstants.UNIT_CLASS_HUMAN)
                    maxHitBarWidth = CellTextureWidth - 16;
                else
                    maxHitBarWidth = CellTextureWidth;
                
                hitBarX = unitX;
                hitBarY = unitY - 33;
            }
            else
            {
                maxHitBarWidth = CellTextureWidth * 2 - 32;
                hitBarX = unitX - CellTextureWidth / 2 + 16;
                hitBarY = unitY - 30;

                if (unit.MobileType == UnitConstants.UNIT_AIR)
                    hitBarY -= 30;
            }

            hitBarX -= Scale(spriteFrame.OffsetX);
            hitBarY -= Scale(spriteFrame.OffsetY);


            const int HIT_BAR_LIGHT_BORDER = 0;
            const int HIT_BAR_DARK_BORDER = 3;
            const int HIT_BAR_BODY = 1;
            const int NO_BAR_LIGHT_BORDER = 0x40 + 11;
            const int NO_BAR_DARK_BORDER = 0x40 + 3;
            const int NO_BAR_BODY = 0x40 + 7;
            int hitBarColor = 0xA8;
            if (unit.MaxHitPoints >= 51 && unit.MaxHitPoints <= 100)
                hitBarColor = 0xB4;
            if (unit.MaxHitPoints >= 101)
                hitBarColor = 0xAC;
            int separatorX = hitBarX + (maxHitBarWidth - 1) * (int)unit.HitPoints / unit.MaxHitPoints;

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
        
        //TODO if unit is attacking another unit or firm, select it also
    }

    private void DrawSprite(Sprite sprite)
    {
        (int spriteX, int spriteY) = GetSpriteScreenXAndY(sprite);
        SpriteFrame spriteFrame = sprite.CurSpriteFrame(out bool needMirror);
        SpriteInfo spriteInfo = SpriteRes[sprite.SpriteResId];
        Graphics.DrawBitmapScaled(spriteFrame.GetSpriteTexture(Graphics, spriteInfo, 0, false), spriteX, spriteY,
            spriteFrame.Width, spriteFrame.Height, needMirror ? FlipMode.Horizontal : FlipMode.None);
    }

    private void DrawSprite(Sprite sprite, int direction)
    {
        (int spriteX, int spriteY) = GetSpriteScreenXAndY(sprite);
        SpriteFrame spriteFrame = sprite.CurSpriteFrame(direction, out bool needMirror);
        SpriteInfo spriteInfo = SpriteRes[sprite.SpriteResId];
        Graphics.DrawBitmapScaled(spriteFrame.GetSpriteTexture(Graphics, spriteInfo, 0, false), spriteX, spriteY,
            spriteFrame.Width, spriteFrame.Height, needMirror ? FlipMode.Horizontal : FlipMode.None);
    }
    
    public void DrawBullet(Bullet bullet)
    {
        if (bullet is Projectile)
        {
            Projectile projectile = (Projectile)bullet;
            double z = (projectile.CurStep + 1) * (projectile.TotalStep + 1 - projectile.CurStep);
            if (z < 0.0)
                z = 0.0;

            if (projectile.CurAction == Sprite.SPRITE_MOVE)
            {
                double dz = (projectile.TotalStep - 2 * (projectile.CurStep));
                int finalDir = projectile.FinalDir;
                if (dz >= 10.0)
                    finalDir = (finalDir & 7 ) | 8; // pointing up
                else if (dz <= -10.0)
                    finalDir = (finalDir & 7 ) | 16; // pointing down
                else
                    finalDir &= 7;
                
                projectile.UpdateSprites(finalDir, z);
                DrawSprite(projectile.Shadow);
                DrawSprite(projectile.Bullet);
            }
            else
            {
                if (projectile.CurAction == Sprite.SPRITE_DIE)
                    DrawSprite(projectile, (projectile.FinalDir & 7) | 16);
                else
                    DrawSprite(projectile);
            }
        }
        else
        {
            DrawSprite(bullet);
        }
    }

    public void DrawEffect(Effect effect)
    {
        DrawSprite(effect);
    }

    public void DrawTornado(Tornado tornado)
    {
        DrawSprite(tornado);
    }
    
    private void DrawUnitPaths(int layer)
    {
        if ((Config.show_unit_path & 1) == 0)
            return;

        for (int i = 0; i < _selectedUnits.Count; i++)
        {
            Unit unit = UnitArray[_selectedUnits[i]];
            if (layer == NormalLayer && unit.MobileType == UnitConstants.UNIT_AIR)
                continue;
            if (layer == AirLayer && unit.MobileType != UnitConstants.UNIT_AIR)
                continue;

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

    private int GetSpriteDrawY2(Sprite sprite)
    {
        SpriteFrame spriteFrame = sprite.CurSpriteFrame(out _);
        int spriteY = MainViewY + Scale(sprite.CurY + spriteFrame.OffsetY) - _topLeftLocY * CellTextureHeight + Scale(spriteFrame.Height) - 1;
        if (sprite is UnitMarine)
            spriteY -= WaveHeight(6);
        return spriteY;
    }

    private (int, int) GetSpriteScreenXAndY(Sprite sprite)
    {
        SpriteFrame spriteFrame = sprite.CurSpriteFrame(out _);
        int spriteX = MainViewX + Scale(sprite.CurX + spriteFrame.OffsetX) - _topLeftLocX * CellTextureWidth;
        int spriteY = MainViewY + Scale(sprite.CurY + spriteFrame.OffsetY) - _topLeftLocY * CellTextureHeight;
        if (sprite is UnitMarine)
            spriteY -= WaveHeight(6);
        return (spriteX, spriteY);
    }
    
    private int WaveHeight(int phase = 0)
    {
        return _waveHeights[(Sys.Instance.FrameNumber / 4 + phase) % InternalConstants.WAVE_CYCLE];
    }
}