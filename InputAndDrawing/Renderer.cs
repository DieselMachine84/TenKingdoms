using System;
using System.Collections.Generic;

namespace TenKingdoms;

[Flags]
public enum FlipMode { None = 0, Horizontal = 1, Vertical = 2 }

public partial class Renderer : IRenderer
{
    public const int WindowWidth = MainViewX + MainViewWidth + BorderWidth + MiniMapSize + BorderWidth;
    public const int WindowHeight = MainViewY + MainViewHeight;

    private const int GameMenuHeight = 84;
    private const int BorderWidth = 18;

    public const int CellTextureWidth = 48;
    public const int CellTextureHeight = 48;
    private const int MainViewX = 0;
    private const int MainViewY = GameMenuHeight;
    private const int MainViewWidthInCells = 30;
    private const int MainViewHeightInCells = 19;
    private const int MainViewWidth = MainViewWidthInCells * CellTextureWidth;
    private const int MainViewHeight = MainViewHeightInCells * CellTextureHeight;

    private const int MiniMapX = MainViewX + MainViewWidth + BorderWidth;
    private const int MiniMapY = MainViewY;
    private const int MiniMapSize = 400;
    private const int MiniMapScale = MiniMapSize / GameConstants.MapSize;

    private const int DetailsX1 = MainViewX + MainViewWidth + 12;
    private const int DetailsX2 = DetailsX1 + DetailsWidth;
    private const int DetailsY1 = MiniMapY + MiniMapSize + 12;
    private const int DetailsY2 = DetailsY1 + DetailsHeight;
    private const int DetailsWidth = MiniMapSize + 12;
    private const int DetailsHeight = WindowHeight - DetailsY1 - 12;
    
    private const int TownFlagShiftX = -9;
    private const int TownFlagShiftY = -97;

    private int _topLeftLocX;
    private int _topLeftLocY;
    private long _lastFrame;
    public bool NeedFullRedraw { get; set; }
    private int _screenSquareFrameCount = 0;
    private int _screenSquareFrameStep = 1;
    private readonly byte[] _miniMapImage = new byte[MiniMapSize * MiniMapSize];

    private int _selectedTownId;
    private int _selectedFirmId;
    private int _selectedUnitId;
    private int _selectedSiteId;
    private int _selectedRaceId;
    private int _selectedWorkerId;

    private Graphics Graphics { get; }

    private Font FontSan { get; }
    private Font FontStd { get; }
    private Font FontMid { get; }
    private Font FontSmall { get; }

    private static TerrainRes TerrainRes => Sys.Instance.TerrainRes;
    private static HillRes HillRes => Sys.Instance.HillRes;
    private static PlantRes PlantRes => Sys.Instance.PlantRes;
    private static RockRes RockRes => Sys.Instance.RockRes;
    private static RawRes RawRes => Sys.Instance.RawRes;
    private static RaceRes RaceRes => Sys.Instance.RaceRes;
    private static TownRes TownRes => Sys.Instance.TownRes;
    private static FirmRes FirmRes => Sys.Instance.FirmRes;
    private static SpriteRes SpriteRes => Sys.Instance.SpriteRes;
    private static UnitRes UnitRes => Sys.Instance.UnitRes;
    private static MonsterRes MonsterRes => Sys.Instance.MonsterRes;
    private static GodRes GodRes => Sys.Instance.GodRes;

    private static Config Config => Sys.Instance.Config;
    private static Info Info => Sys.Instance.Info;
    private static World World => Sys.Instance.World;
    private static SECtrl SECtrl => Sys.Instance.SECtrl;

    
    private static RockArray DirtArray => Sys.Instance.DirtArray;
    private static NationArray NationArray => Sys.Instance.NationArray;
    private static TownArray TownArray => Sys.Instance.TownArray;
    private static FirmArray FirmArray => Sys.Instance.FirmArray;
    private static UnitArray UnitArray => Sys.Instance.UnitArray;
    private static SiteArray SiteArray => Sys.Instance.SiteArray;

    public Renderer(Graphics graphics)
    {
        Graphics = graphics;

        FontSan = new Font("SAN", 0, 0);
        FontStd = new Font("STD", 2, 0);
        FontMid = new Font("MID", 1, 0);
        FontSmall = new Font("SMAL", 1, 0);
        
        CreateUITextures();
        CreateAnimatedSegments();
        CreateArrowTextures();
        CreateButtonTextures();
    }

    private int Scale(int size)
    {
        return size * 3 / 2;
    }
    
    public void DrawFrame()
    {
        Graphics.ResetClipRectangle();
        DrawMainScreen();
        if (_selectedTownId != 0 && !TownArray.IsDeleted(_selectedTownId))
        {
            DrawTownDetails(TownArray[_selectedTownId]);
        }

        if (_selectedFirmId != 0 && !FirmArray.IsDeleted(_selectedFirmId))
        {
            DrawFirmDetails(FirmArray[_selectedFirmId]);
        }

        if (_selectedUnitId != 0 && !UnitArray.IsDeleted(_selectedUnitId))
        {
            DrawUnitDetails(UnitArray[_selectedUnitId]);
        }

        if (_selectedSiteId != 0 && !SiteArray.IsDeleted(_selectedSiteId))
        {
            DrawSiteDetails(SiteArray[_selectedSiteId]);
        }

        if (_lastFrame == Sys.Instance.FrameNumber && Sys.Instance.Speed != 0)
            return;

        _lastFrame = Sys.Instance.FrameNumber;
        DrawMainView();
        DrawMiniMap();
    }

    private void DrawMainScreen()
    {
        Graphics.DrawBitmap(_gameMenuTexture1, 0, 0, Scale(_gameMenuTexture1Width), Scale(_gameMenuTexture1Height));
        Graphics.DrawBitmap(_gameMenuTexture2, Scale(_gameMenuTexture1Width), 0, Scale(_gameMenuTexture2Width), Scale(_gameMenuTexture2Height));
        int gameMenuWidth = Scale(_gameMenuTexture1Width) + Scale(_gameMenuTexture2Width);
        while (gameMenuWidth < WindowWidth)
        {
            Graphics.DrawBitmap(_gameMenuTexture3, gameMenuWidth, 0, Scale(_gameMenuTexture3Width), Scale(_gameMenuTexture3Height));
            gameMenuWidth += Scale(_gameMenuTexture3Width);
        }

        Graphics.DrawBitmap(_middleBorder1Texture, MainViewX + MainViewWidth, 0, Scale(_middleBorder1TextureWidth), Scale(_middleBorder1TextureHeight));
        int middleBorderHeight = Scale(_middleBorder1TextureHeight);
        while (middleBorderHeight < WindowHeight)
        {
            Graphics.DrawBitmap(_middleBorder2Texture, MainViewX + MainViewWidth, middleBorderHeight, Scale(_middleBorder2TextureWidth), Scale(_middleBorder2TextureHeight));
            middleBorderHeight += Scale(_middleBorder2TextureHeight);
        }

        Graphics.DrawBitmap(_detailsTexture1, DetailsX1, DetailsY1, Scale(_detailsTexture1Width), Scale(_detailsTexture1Height));
        Graphics.DrawBitmap(_detailsTexture2, DetailsX2 - Scale(_detailsTexture2Width), DetailsY1, Scale(_detailsTexture2Width), Scale(_detailsTexture2Height));
        int detailsHeight = DetailsY1 + Scale(_detailsTexture1Width);
        while (detailsHeight < WindowHeight)
        {
            Graphics.DrawBitmap(_detailsTexture3, DetailsX1, detailsHeight, Scale(_detailsTexture3Width), Scale(_detailsTexture3Height));
            Graphics.DrawBitmap(_detailsTexture4, DetailsX2 - Scale(_detailsTexture4Width), detailsHeight, Scale(_detailsTexture4Width), Scale(_detailsTexture4Height));
            detailsHeight += Scale(_detailsTexture3Width);
        }

        Graphics.DrawBitmap(_rightBorder1Texture, WindowWidth - Scale(_rightBorder1TextureWidth), 0, Scale(_rightBorder1TextureWidth), Scale(_rightBorder1TextureHeight));
        int rightBorderHeight = Scale(_rightBorder1TextureHeight);
        while (rightBorderHeight < WindowHeight)
        {
            Graphics.DrawBitmap(_rightBorder2Texture, WindowWidth - Scale(_rightBorder2TextureWidth), rightBorderHeight, Scale(_rightBorder2TextureWidth), Scale(_rightBorder2TextureHeight));
            rightBorderHeight += Scale(_rightBorder2TextureHeight);
        }
        
        Graphics.DrawBitmap(_miniMapBorder1Texture, MainViewX + MainViewWidth, MiniMapY + MiniMapSize,
            Scale(_miniMapBorder1TextureWidth), Scale(_miniMapBorder1TextureHeight));
        Graphics.DrawBitmap(_miniMapBorder2Texture, WindowWidth - Scale(_miniMapBorder2TextureWidth), MiniMapY + MiniMapSize,
            Scale(_miniMapBorder2TextureWidth), Scale(_miniMapBorder2TextureHeight));
        Graphics.DrawBitmap(_bottomBorder1Texture, MainViewX + MainViewWidth, WindowHeight - Scale(_bottomBorder1TextureHeight),
            Scale(_bottomBorder1TextureWidth), Scale(_bottomBorder1TextureHeight));
        Graphics.DrawBitmap(_bottomBorder2Texture, WindowWidth - Scale(_miniMapBorder2TextureWidth), WindowHeight - Scale(_bottomBorder1TextureHeight),
            Scale(_bottomBorder2TextureWidth), Scale(_bottomBorder2TextureHeight));
    }

    private void ResetSelection()
    {
        _selectedTownId = _selectedFirmId = _selectedUnitId = _selectedSiteId = _selectedRaceId = _selectedWorkerId = 0;
    }
    
    public void Reset()
    {
        _lastFrame = 0;
        _screenSquareFrameCount = 0;
        _screenSquareFrameStep = 1;
        ResetSelection();
        NeedFullRedraw = true;
    }

    private bool IsExplored(int locX1, int locX2, int locY1, int locY2)
    {
        if (Config.explore_whole_map)
            return true;
        
        for (int locX = locX1; locX <= locX2; locX++)
        {
            for (int locY = locY1; locY <= locY2; locY++)
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
}