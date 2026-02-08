using System;
using System.Collections.Generic;

namespace TenKingdoms;

[Flags]
public enum FlipMode { None = 0, Horizontal = 1, Vertical = 2 }

public enum ScreenObjectType { None, FriendTown, EnemyTown, FriendFirm, EnemyFirm, FriendUnit, EnemyUnit, SpyUnit, UnitGroup, Site }

public enum ViewMode { Normal, Kingdoms, Villages, Economy, Trade, Military, Technology, Espionage, Ranking, News }

public partial class Renderer : IRenderer
{
    public int WindowWidth => MainViewX + MainViewWidth + BorderWidth + MiniMapSize + BorderWidth;
    public int WindowHeight => MainViewY + MainViewHeight;

    private const int NormalLayer = 1;
    private const int TopLayer = 2;
    private const int BottomLayer = 4;
    private const int AirLayer = 8;

    private const int GameMenuHeight = 84;
    public const int BorderWidth = 18;

    public const int CellTextureWidth = 48;
    public const int CellTextureHeight = 48;
    private const int ViewModeX = 0;
    private const int ViewModeY = 0;
    private const int ViewModeWidth = 400;
    private const int ViewModeHeight = 84;
    public const int MainViewX = 0;
    public const int MainViewY = GameMenuHeight;
    private int MainViewWidth => Config.GameScreenWidth * CellTextureWidth;
    private int MainViewHeight => Config.GameScreenHeight * CellTextureHeight;

    private int MiniMapX => MainViewX + MainViewWidth + BorderWidth;
    private const int MiniMapY = MainViewY;
    public const int MiniMapSize = 400;
    private int MiniMapScale => MiniMapSize / Config.MapSize;

    private int DetailsX1 => MainViewX + MainViewWidth + 12;
    private int DetailsX2 => DetailsX1 + DetailsWidth;
    private int DetailsY1 => MiniMapY + MiniMapSize + 12;
    private int DetailsY2 => DetailsY1 + DetailsHeight;
    private int DetailsWidth => MiniMapSize + 12;
    private int DetailsHeight => WindowHeight - DetailsY1 - 12;
    
    private const int TownFlagShiftX = -9;
    private const int TownFlagShiftY = -97;

    private int _topLeftLocX;
    private int _topLeftLocY;
    private ViewMode _viewMode = ViewMode.Normal;
    public bool NeedFullRedraw { get; set; }
    private int _screenSquareFrameCount = 0;
    private int _screenSquareFrameStep = 1;
    private readonly int[] _warPointColors = { 0x00, 0xb4, 0xb0, 0xa0, 0xb0, 0xb4 };
    private readonly byte[] _miniMapImage = new byte[MiniMapSize * MiniMapSize];

    private int _selectedTownId;
    private int _selectedFirmId;
    //TODO keep selected caravan and trader when it enters market/harbor
    private int _selectedUnitId;
    //TODO what if _selectedUnitId is killed but _selectedUnits.Count is greater than zero?
    private readonly List<int> _selectedUnits = new List<int>();
    private int _selectedSiteId;
    private int _selectedRaceId;
    private int _selectedShipId;
    private InnUnit _selectedInnUnit;
    private Spy _selectedSpy;
    
    private int _setStopId;

    private Graphics Graphics { get; }

    private Font FontSan { get; }
    private Font FontStd { get; }
    private Font FontMid { get; }
    private Font FontSmall { get; }
    private Font FontBible { get; }

    private static TerrainRes TerrainRes => Sys.Instance.TerrainRes;
    private static HillRes HillRes => Sys.Instance.HillRes;
    private static PlantRes PlantRes => Sys.Instance.PlantRes;
    private static RockRes RockRes => Sys.Instance.RockRes;
    private static RawRes RawRes => Sys.Instance.RawRes;
    private static RaceRes RaceRes => Sys.Instance.RaceRes;
    private static TownRes TownRes => Sys.Instance.TownRes;
    private static FirmRes FirmRes => Sys.Instance.FirmRes;
    private static FirmDieRes FirmDieRes => Sys.Instance.FirmDieRes;
    private static SpriteRes SpriteRes => Sys.Instance.SpriteRes;
    private static UnitRes UnitRes => Sys.Instance.UnitRes;
    private static MonsterRes MonsterRes => Sys.Instance.MonsterRes;
    private static GodRes GodRes => Sys.Instance.GodRes;
    private static TechRes TechRes => Sys.Instance.TechRes;
    private static TalkRes TalkRes => Sys.Instance.TalkRes;
    private static CursorRes CursorRes => Sys.Instance.CursorRes;

    private static Config Config => Sys.Instance.Config;
    private static Info Info => Sys.Instance.Info;
    private static World World => Sys.Instance.World;
    private static SECtrl SECtrl => Sys.Instance.SECtrl;
    private static SERes SERes => Sys.Instance.SERes;

    
    private static RockArray RockArray => Sys.Instance.RockArray;
    private static RockArray DirtArray => Sys.Instance.DirtArray;
    private static NationArray NationArray => Sys.Instance.NationArray;
    private static TownArray TownArray => Sys.Instance.TownArray;
    private static FirmArray FirmArray => Sys.Instance.FirmArray;
    private static FirmDieArray FirmDieArray => Sys.Instance.FirmDieArray;
    private static UnitArray UnitArray => Sys.Instance.UnitArray;
    private static SpyArray SpyArray => Sys.Instance.SpyArray;
    private static SiteArray SiteArray => Sys.Instance.SiteArray;
    private static BulletArray BulletArray => Sys.Instance.BulletArray;
    private static EffectArray EffectArray => Sys.Instance.EffectArray;
    private static TornadoArray TornadoArray => Sys.Instance.TornadoArray;
    private static WarPointArray WarPointArray => Sys.Instance.WarPointArray;
    private static NewsArray NewsArray => Sys.Instance.NewsArray;

    public Renderer(Graphics graphics)
    {
        Graphics = graphics;

        FontSan = new Font("SAN", 0, 0);
        FontStd = new Font("STD", 2, 0);
        FontMid = new Font("MID", 1, 0);
        FontSmall = new Font("SMAL", 1, 0);
        FontBible = new Font("CASA", 1, 1);
        
        CreateUITextures();
        CreateAnimatedSegments();
        CreateIconTextures();
        CreateButtonTextures();
    }

    private int Scale(int size)
    {
        return size * 3 / 2;
    }

    private (int, int) GetMainViewLocation(int screenX, int screenY)
    {
        return (_topLeftLocX + (screenX - MainViewX) / CellTextureWidth, _topLeftLocY + (screenY - MainViewY) / CellTextureHeight);
    }

    private (int, int) GetScreenXAndY(int locX, int locY)
    {
        return (MainViewX + (locX - _topLeftLocX) * CellTextureWidth, MainViewY + (locY - _topLeftLocY) * CellTextureHeight);
    }

    private (int, int) GetMiniMapLocation(int screenX, int screenY)
    {
        int locX = screenX - MiniMapX;
        int locY = screenY - MiniMapY;
        
        if (Config.MapSize == 100 || Config.MapSize == 200)
        {
            locX /= MiniMapScale;
            locY /= MiniMapScale;
        }

        if (Config.MapSize == 300)
        {
            locX = locX * 3 / 4;
            locY = locY * 3 / 4;
        }

        return (locX, locY);
    }
    
    public void DrawFrame(bool nextFrame)
    {
        ResetDeletedSelectedObjects();
        
        Graphics.ResetClipRectangle();
        DrawMainScreen();
        Graphics.SetClipRectangle(MainViewX, MainViewY, MainViewWidth, MainViewHeight);
        DrawMainView();
        DrawNews();
        Graphics.SetClipRectangle(MiniMapX, MiniMapY, MiniMapSize, MiniMapSize);
        DrawMiniMap(nextFrame);
        Graphics.ResetClipRectangle();

        if (_selectedTownId != 0)
        {
            DrawTownDetails(TownArray[_selectedTownId]);
        }

        if (_selectedFirmId != 0)
        {
            DrawFirmDetails(FirmArray[_selectedFirmId]);
        }

        if (_selectedUnitId != 0)
        {
            DrawUnitDetails(UnitArray[_selectedUnitId]);
        }

        if (_selectedSiteId != 0 && !SiteArray[_selectedSiteId].HasMine)
        {
            DrawSiteDetails(SiteArray[_selectedSiteId]);
        }
    }

    private void DrawMainScreen()
    {
        Graphics.DrawBitmapScaled(_gameMenuTexture1, 0, 0, _gameMenuTexture1Width, _gameMenuTexture1Height);
        Graphics.DrawBitmapScaled(_gameMenuTexture2, Scale(_gameMenuTexture1Width), 0, _gameMenuTexture2Width, _gameMenuTexture2Height);
        int gameMenuWidth = Scale(_gameMenuTexture1Width) + Scale(_gameMenuTexture2Width);
        while (gameMenuWidth < WindowWidth)
        {
            Graphics.DrawBitmapScaled(_gameMenuTexture3, gameMenuWidth, 0, _gameMenuTexture3Width, _gameMenuTexture3Height);
            gameMenuWidth += Scale(_gameMenuTexture3Width);
        }

        Graphics.DrawBitmapScaled(_middleBorder1Texture, MainViewX + MainViewWidth, 0, _middleBorder1TextureWidth, _middleBorder1TextureHeight);
        int middleBorderHeight = Scale(_middleBorder1TextureHeight);
        while (middleBorderHeight < WindowHeight)
        {
            Graphics.DrawBitmapScaled(_middleBorder2Texture, MainViewX + MainViewWidth, middleBorderHeight, _middleBorder2TextureWidth, _middleBorder2TextureHeight);
            middleBorderHeight += Scale(_middleBorder2TextureHeight);
        }

        Graphics.DrawBitmapScaled(_detailsTexture1, DetailsX1, DetailsY1, _detailsTexture1Width, _detailsTexture1Height);
        Graphics.DrawBitmapScaled(_detailsTexture2, DetailsX2 - Scale(_detailsTexture2Width), DetailsY1, _detailsTexture2Width, _detailsTexture2Height);
        int detailsHeight = DetailsY1 + Scale(_detailsTexture1Width);
        while (detailsHeight < WindowHeight)
        {
            Graphics.DrawBitmapScaled(_detailsTexture3, DetailsX1, detailsHeight, _detailsTexture3Width, _detailsTexture3Height);
            Graphics.DrawBitmapScaled(_detailsTexture4, DetailsX2 - Scale(_detailsTexture4Width), detailsHeight, _detailsTexture4Width, _detailsTexture4Height);
            detailsHeight += Scale(_detailsTexture3Width);
        }

        Graphics.DrawBitmapScaled(_rightBorder1Texture, WindowWidth - Scale(_rightBorder1TextureWidth), 0, _rightBorder1TextureWidth, _rightBorder1TextureHeight);
        int rightBorderHeight = Scale(_rightBorder1TextureHeight);
        while (rightBorderHeight < WindowHeight)
        {
            Graphics.DrawBitmapScaled(_rightBorder2Texture, WindowWidth - Scale(_rightBorder2TextureWidth), rightBorderHeight, _rightBorder2TextureWidth, _rightBorder2TextureHeight);
            rightBorderHeight += Scale(_rightBorder2TextureHeight);
        }
        
        Graphics.DrawBitmapScaled(_miniMapBorder1Texture, MainViewX + MainViewWidth, MiniMapY + MiniMapSize,
            _miniMapBorder1TextureWidth, _miniMapBorder1TextureHeight);
        Graphics.DrawBitmapScaled(_miniMapBorder2Texture, WindowWidth - Scale(_miniMapBorder2TextureWidth), MiniMapY + MiniMapSize,
            _miniMapBorder2TextureWidth, _miniMapBorder2TextureHeight);
        Graphics.DrawBitmapScaled(_bottomBorder1Texture, MainViewX + MainViewWidth, WindowHeight - Scale(_bottomBorder1TextureHeight),
            _bottomBorder1TextureWidth, _bottomBorder1TextureHeight);
        Graphics.DrawBitmapScaled(_bottomBorder2Texture, WindowWidth - Scale(_miniMapBorder2TextureWidth), WindowHeight - Scale(_bottomBorder1TextureHeight),
            _bottomBorder2TextureWidth, _bottomBorder2TextureHeight);

        if (NationArray.PlayerId != 0)
        {
            Nation player = NationArray.Player;
            PutText(FontMid, player.FoodString(), 468, 6);
            PutText(FontMid, player.CashString(), 468, 38);
            PutText(FontMid, Info.GameDate.ToString("MMM d, yyyy"), 878, 6);
            if (player.ReputationChange365Days() >= 0.0)
                Graphics.DrawBitmapScaled(_reputationUpTexture, 830, 39, _reputationUpWidth, _reputationUpHeight);
            else
                Graphics.DrawBitmapScaled(_reputationDownTexture, 830, 39, _reputationDownWidth, _reputationDownHeight);
            PutText(FontMid, player.ReputationString(), 878, 38);
        }
        else
        {
            PutText(FontMid, Info.GameDate.ToString("MMM d, yyyy"), 878, 6);
            Graphics.DrawBitmapScaled(_reputationDownTexture, 830, 39, _reputationDownWidth, _reputationDownHeight);
        }
    }

    private void DrawNews()
    {
        bool hasNews = false;
        int dy = 40;
        for (int i = NewsArray.LastClearId + 1; i < NewsArray.Count(); i++)
        {
            News news = NewsArray[i];
            if (Info.GameDate > news.NewsDate.AddDays(GameConstants.DISP_NEWS_DAYS))
            {
                NewsArray.LastClearId = i;
                continue;
            }

            if (Config.DisplayNewsType == Config.OPTION_DISPLAY_MAJOR_NEWS && !news.IsMajor())
            {
                continue;
            }

            if (news.Id == News.NEWS_DIPLOMACY)
            {
                TalkMsg talkMsg = TalkRes.get_talk_msg(news.Param1);
                if (talkMsg.reply_type == TalkRes.REPLY_WAITING && !talkMsg.is_valid_to_reply())
                {
                    continue;
                }
            }

            Graphics.DrawRect(MainViewX + 6, MainViewY + MainViewHeight - dy - 6, MainViewWidth - 40, 40, Colors.NEWS_COLOR);
            if (news.IsLocValid())
                Graphics.DrawBitmap(_newsLocTexture, MainViewX + 12, MainViewY + MainViewHeight - dy + 3, _newsLocWidth * 2, _newsLocHeight * 2);
            string newsText = news.NewsDate.ToString("MMM d, yyyy") + " " + news.Message();
            PutText(FontSan, newsText, MainViewX + 47, MainViewY + MainViewHeight - dy);
            
            hasNews = true;
            dy += 40;
        }

        if (hasNews)
            Graphics.DrawBitmap(_clearNewsTexture, MainViewX + MainViewWidth - 30, MainViewY + MainViewHeight - 54, _clearNewsWidth * 2, _clearNewsHeight * 2);
        Graphics.DrawBitmap(_newsLogTexture, MainViewX + MainViewWidth - 30, MainViewY + MainViewHeight - 28, _newsLogWidth * 2, _newsLogHeight * 2);
    }

    private void ResetDeletedSelectedObjects()
    {
        if (_selectedTownId != 0 && TownArray.IsDeleted(_selectedTownId))
            _selectedTownId = 0;
        if (_selectedFirmId != 0 && FirmArray.IsDeleted(_selectedFirmId))
            _selectedFirmId = 0;
        if (_selectedUnitId != 0 && UnitArray.IsDeleted(_selectedUnitId))
            _selectedUnitId = 0;
        if (_selectedSiteId != 0 && SiteArray.IsDeleted(_selectedSiteId))
            _selectedSiteId = 0;
        for (int i = _selectedUnits.Count - 1; i >= 0; i--)
        {
            if (UnitArray.IsDeleted(_selectedUnits[i]))
                _selectedUnits.RemoveAt(i);
        }
    }

    private void ResetSelection()
    {
        _selectedTownId = _selectedFirmId = _selectedUnitId = _selectedSiteId = _selectedRaceId = _selectedShipId = 0;
        _selectedInnUnit = null;
        _selectedSpy = null;
        _selectedUnits.Clear();
        TownDetailsMode = TownDetailsMode.Normal;
        FirmDetailsMode = FirmDetailsMode.Normal;
        UnitDetailsMode = UnitDetailsMode.Normal;
        _setStopId = 0;
    }
    
    public void Reset()
    {
        _screenSquareFrameCount = 0;
        _screenSquareFrameStep = 1;
        ResetSelection();
        NeedFullRedraw = true;
    }
}