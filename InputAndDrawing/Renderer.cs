using System;
using System.Collections.Generic;
using System.IO;

namespace TenKingdoms;

[Flags]
public enum FlipMode { None = 0, Horizontal = 1, Vertical = 2 }

public enum ScreenObjectType { None, FriendTown, EnemyTown, FriendFirm, EnemyFirm, FriendUnit, EnemyUnit, SpyUnit, UnitGroup, Site }

public enum GameMode { StartMenu, SinglePlayerMenu, Game, InGameMenu, Save, Load }

public enum ViewMode { Normal, Kingdoms, Villages, Economy, Trade, Military, Technology, Espionage, Ranking, News }

public partial class Renderer : IRenderer
{
    private const int NormalLayer = 1;
    private const int TopLayer = 2;
    private const int BottomLayer = 4;
    private const int AirLayer = 8;
    
    public static int StartMenuWidth = 1280;
    public static int StartMenuHeight = 853;
    public int WindowWidth => MainViewX + MainViewWidth + BorderWidth + MiniMapSize + BorderWidth;
    public int WindowHeight => MainViewY + MainViewHeight;

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

    private GameMode _oldGameMode;
    public GameMode GameMode { get; set; } = GameMode.StartMenu;
    private int _topLeftLocX;
    private int _topLeftLocY;
    private ViewMode _prevViewMode = ViewMode.Normal;
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

    private int _selectedKingdomId;
    private TalkMsg _curTalkMsg;
    private readonly TalkChoice[] _talkChoices = new TalkChoice[TalkMsg.MAX_TALK_CHOICE];
    private int _talkChoiceIndex;
    private string _choiceQuestion;
    private string _choiceQuestionSecondLine;
    private int _replyTalkMsgId;

    private Graphics Graphics { get; }
    private Audio Audio { get; }

    private Font FontSan { get; }
    private Font FontStd { get; }
    private Font FontMid { get; }
    private Font FontSmall { get; }
    private Font FontBible { get; }
    private Font FontNews { get; }

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
    private static CursorRes CursorRes => Sys.Instance.CursorRes;

    private static Config Config => Sys.Instance.Config;
    private static SaveGameProvider SaveGameProvider => Sys.Instance.SaveGameProvider;
    private static Info Info => Sys.Instance.Info;
    private static World World => Sys.Instance.World;

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
    private static TalkMsgArray TalkMsgArray => Sys.Instance.TalkMsgArray;
    private static NewsArray NewsArray => Sys.Instance.NewsArray;

    public Renderer(Graphics graphics, Audio audio)
    {
        Graphics = graphics;
        Audio = audio;

        FontSan = new Font("SAN", 0, 0);
        FontStd = new Font("STD", 2, 0);
        FontMid = new Font("MID", 1, 0);
        FontSmall = new Font("SMAL", 1, 0);
        FontBible = new Font("CASA", 1, 1);
        FontNews = new Font("NEWS", 0, 0);
        
        CreateUITextures();
        CreateAnimatedSegments();
        CreateIconTextures();
        CreateButtonTextures();
        
        for (int i = 0; i < _talkChoices.Length; i++)
            _talkChoices[i] = new TalkChoice();
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

    public bool IsLocationOnScreen(int locX, int locY)
    {
        return locX >= _topLeftLocX && locX <= _topLeftLocX + Config.GameScreenWidth - 1 &&
               locY >= _topLeftLocY && locY <= _topLeftLocY + Config.GameScreenHeight - 1;
    }

    private void DrawStartMenu()
    {
        Graphics.DrawBitmap(_mainMenuTexture, 0, 0, _mainMenuWidth, _mainMenuHeight, FlipMode.Vertical);

        int x = 397;
        int y = 300;
        int dy = 0;
        if (_mouseMotionX >= x && _mouseMotionX <= x + Scale(_sword1Width) && _mouseMotionY >= y && _mouseMotionY <= y + Scale(_swordSinglePlayerHeight))
            Graphics.DrawBitmapScaled(_swordSinglePlayerSelectedTexture, x, y, _swordSinglePlayerWidth, _swordSinglePlayerHeight);
        else
            Graphics.DrawBitmapScaled(_swordSinglePlayerTexture, x, y, _swordSinglePlayerWidth, _swordSinglePlayerHeight);
        dy += Scale(_swordSinglePlayerHeight);
        Graphics.DrawBitmapScaled(_swordMultiPlayerDisabledTexture, x, y + dy, _swordMultiPlayerWidth, _swordMultiPlayerHeight);
        dy += Scale(_swordMultiPlayerHeight);
        Graphics.DrawBitmapScaled(_swordHallOfFameDisabledTexture, x, y + dy, _swordHallOfFameWidth, _swordHallOfFameHeight);
        dy += Scale(_swordHallOfFameHeight);
        Graphics.DrawBitmapScaled(_swordCreditsDisabledTexture, x, y + dy, _swordCreditsWidth, _swordCreditsHeight);
        dy += Scale(_swordCreditsHeight);
        if (_mouseMotionX >= x && _mouseMotionX <= x + Scale(_sword1Width) && _mouseMotionY >= y + dy && _mouseMotionY <= y + dy + Scale(_swordQuitHeight))
            Graphics.DrawBitmapScaled(_swordQuitSelectedTexture, x, y + dy, _swordQuitWidth, _swordQuitHeight);
        else
            Graphics.DrawBitmapScaled(_swordQuitTexture, x, y + dy, _swordQuitWidth, _swordQuitHeight);
        
        PutText(FontNews, "Version 1", _mainMenuWidth - 150, _mainMenuHeight - 50);
    }

    private void DrawSinglePlayerMenu()
    {
        Graphics.DrawBitmap(_mainMenuTexture, 0, 0, _mainMenuWidth, _mainMenuHeight, FlipMode.Vertical);
        
        int x = 397;
        int y = 300;
        int dy = 0;
        Graphics.DrawBitmapScaled(_swordTrainingDisabledTexture, x, y, _swordTrainingWidth, _swordTrainingHeight);
        dy += Scale(_swordTrainingHeight);
        if (_mouseMotionX >= x && _mouseMotionX <= x + Scale(_sword2Width) && _mouseMotionY >= y + dy && _mouseMotionY <= y + dy + Scale(_swordNewGameHeight))
            Graphics.DrawBitmapScaled(_swordNewGameSelectedTexture, x, y + dy, _swordNewGameWidth, _swordNewGameHeight);
        else
            Graphics.DrawBitmapScaled(_swordNewGameTexture, x, y + dy, _swordNewGameWidth, _swordNewGameHeight);
        dy += Scale(_swordNewGameHeight);
        if (_mouseMotionX >= x && _mouseMotionX <= x + Scale(_sword2Width) && _mouseMotionY >= y + dy && _mouseMotionY <= y + dy + Scale(_swordLoadGameHeight))
            Graphics.DrawBitmapScaled(_swordLoadGameSelectedTexture, x, y + dy, _swordLoadGameWidth, _swordLoadGameHeight);
        else
            Graphics.DrawBitmapScaled(_swordLoadGameTexture, x, y + dy, _swordLoadGameWidth, _swordLoadGameHeight);
        dy += Scale(_swordLoadGameHeight);
        Graphics.DrawBitmapScaled(_swordScenarioDisabledTexture, x, y + dy, _swordScenarioWidth, _swordScenarioHeight);
        dy += Scale(_swordScenarioHeight);
        if (_mouseMotionX >= x && _mouseMotionX <= x + Scale(_sword2Width) && _mouseMotionY >= y + dy && _mouseMotionY <= y + dy + Scale(_swordCancelHeight))
            Graphics.DrawBitmapScaled(_swordCancelSelectedTexture, x, y + dy, _swordCancelWidth, _swordCancelHeight);
        else
            Graphics.DrawBitmapScaled(_swordCancelTexture, x, y + dy, _swordCancelWidth, _swordCancelHeight);
        
        PutText(FontNews, "Version 1", _mainMenuWidth - 150, _mainMenuHeight - 50);
    }

    private void DrawInGameMenu()
    {
        int x = (WindowWidth - Scale(_inGameMenuWidth)) / 2;
        int y = (WindowHeight - Scale(_inGameMenuHeight)) / 2;
        Graphics.DrawBitmapScaled(_inGameMenuTexture, x, y, _inGameMenuWidth, _inGameMenuHeight);

        int dy = 51;
        for (int i = 0; i < 8; i++)
        {
            bool mouseOnButton = _mouseButtonX > x + 135 && _mouseButtonX < x + 135 + 255 &&
                                 _mouseButtonY > y + 139 + i * dy && _mouseButtonY < y + 139 + (i + 1) * dy;
            if (_leftMousePressed && mouseOnButton)
                Graphics.DrawBitmapScaled(_inGameMenuPressedTexture, x + 134, y + 139 + i * dy, _inGameMenuPressedWidth, _inGameMenuPressedHeight);
        }
    }
    
    public void DrawFrame(bool nextFrame)
    {
        if (GameMode == GameMode.StartMenu)
        {
            DrawStartMenu();
            return;
        }

        if (GameMode == GameMode.SinglePlayerMenu)
        {
            DrawSinglePlayerMenu();
            return;
        }

        if (GameMode == GameMode.InGameMenu)
        {
            DrawInGameMenu();
            return;
        }
        
        if (GameMode == GameMode.Save)
        {
            DrawSaveMenu();
            return;
        }
        
        if (GameMode == GameMode.Load)
        {
            DrawLoadMenu();
            return;
        }
        
        ResetDeletedSelectedObjects();
        
        Graphics.ResetClipRectangle();
        DrawMainScreen();
        Graphics.SetClipRectangle(MainViewX, MainViewY, MainViewWidth, MainViewHeight);
        DrawMainView();
        DrawNews();
        DrawSelectedView();
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
        
        if (_viewMode == ViewMode.Kingdoms)
            Graphics.DrawBitmapScaled(_scrollMenuTextures[0], -6, 0, _scrollMenuWidths[0], _scrollMenuHeights[0]);
        if (_viewMode == ViewMode.Villages)
            Graphics.DrawBitmapScaled(_scrollMenuTextures[1], 87, 0, _scrollMenuWidths[1], _scrollMenuHeights[1]);
        if (_viewMode == ViewMode.Economy)
            Graphics.DrawBitmapScaled(_scrollMenuTextures[2], 180, 0, _scrollMenuWidths[2], _scrollMenuHeights[2]);
        if (_viewMode == ViewMode.Trade)
            Graphics.DrawBitmapScaled(_scrollMenuTextures[3], 273, 0, _scrollMenuWidths[3], _scrollMenuHeights[3]);
        if (_viewMode == ViewMode.Military)
            Graphics.DrawBitmapScaled(_scrollMenuTextures[4], 4, 29, _scrollMenuWidths[4], _scrollMenuHeights[4]);
        if (_viewMode == ViewMode.Technology)
            Graphics.DrawBitmapScaled(_scrollMenuTextures[5], 96, 29, _scrollMenuWidths[5], _scrollMenuHeights[5]);
        if (_viewMode == ViewMode.Espionage)
            Graphics.DrawBitmapScaled(_scrollMenuTextures[6], 187, 29, _scrollMenuWidths[6], _scrollMenuHeights[6]);
        if (_viewMode == ViewMode.Ranking)
            Graphics.DrawBitmapScaled(_scrollMenuTextures[7], 282, 29, _scrollMenuWidths[7], _scrollMenuHeights[7]);

        if (NationArray.Player != null)
        {
            Nation player = NationArray.Player;
            PutText(FontMid, player.FoodString(), 468, 6);
            PutText(FontMid, player.CashString(), 468, 38);
            PutText(FontMid, Misc.ToShortDate(Info.GameDate), 878, 6);
            if (player.ReputationChange365Days() >= 0.0)
                Graphics.DrawBitmapScaled(_reputationUpTexture, 830, 39, _reputationUpWidth, _reputationUpHeight);
            else
                Graphics.DrawBitmapScaled(_reputationDownTexture, 830, 39, _reputationDownWidth, _reputationDownHeight);
            PutText(FontMid, player.ReputationString(), 878, 38);
        }
        else
        {
            PutText(FontMid, Misc.ToShortDate(Info.GameDate), 878, 6);
            Graphics.DrawBitmapScaled(_reputationDownTexture, 830, 39, _reputationDownWidth, _reputationDownHeight);
        }
        
        Graphics.DrawBitmapScaled(_map3Texture, DetailsX1 - 8, 3, _map3Width, _map3Height);

        bool mouseOnMenuButton = _mouseButtonX >= DetailsX1 + 312 && _mouseButtonX <= DetailsX1 + 404 &&
                                 _mouseButtonY >= 17 && _mouseButtonY <= 63;
        if (_leftMousePressed && mouseOnMenuButton)
            Graphics.DrawBitmapScaled(_menuDownTexture, DetailsX1 + 304, 9, _menuDownWidth, _menuDownHeight);
        else
            Graphics.DrawBitmapScaled(_menuUpTexture, DetailsX1 + 304, 9, _menuUpWidth, _menuUpHeight);
    }

    private void DrawNews()
    {
        bool hasNews = false;
        int dy = 38;
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
                TalkMsg talkMsg = TalkMsgArray.GetTalkMsg(news.Param1);
                if (talkMsg.ReplyType == TalkMsgArray.REPLY_WAITING && !talkMsg.IsValidToReply())
                {
                    continue;
                }
            }

            Graphics.DrawBitmap(_newsTexture, MainViewX + 6, MainViewY + MainViewHeight - dy - 6, _newsWidth, _newsHeight);

            if (news.Id == News.NEWS_DIPLOMACY)
            {
                TalkMsg talkMsg = TalkMsgArray.GetTalkMsg(news.Param1);
                
                int nationId;
                if( talkMsg.ReplyType == TalkMsgArray.REPLY_WAITING || talkMsg.ReplyType == TalkMsgArray.REPLY_NOT_NEEDED )
                    nationId = talkMsg.FromNationId;
                else
                    nationId = talkMsg.ToNationId;
                
                DrawNationColor(ColorRemap.ColorSchemes[nationId], MainViewX + 12, MainViewY + MainViewHeight + 2 - dy);
            }
            else if (news.Id == News.NEWS_CHAT_MSG)
            {
                DrawNationColor(news.NationColor1, MainViewX + 12, MainViewY + MainViewHeight + 2 - dy);
            }
            else
            {
                if (news.IsLocValid())
                    Graphics.DrawBitmap(_newsLocTexture, MainViewX + 12, MainViewY + MainViewHeight + 2 - dy, _newsLocWidth * 2, _newsLocHeight * 2);
            }

            TalkMsgArray.AddNationColor = true;
            string newsText = Misc.ToShortDate(news.NewsDate) + " " + news.Message();
            PutText(FontSan, newsText, MainViewX + 46, MainViewY + MainViewHeight - 2 - dy);
            TalkMsgArray.AddNationColor = false;
            
            hasNews = true;
            dy += 38;
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
        
        if (_selectedUnitId != 0)
        {
            if (UnitArray.IsDeleted(_selectedUnitId) || !KeepSelected(UnitArray[_selectedUnitId]))
            {
                _selectedUnitId = 0;
                UnitDetailsMode = UnitDetailsMode.Normal;
            }
        }

        if (_selectedSiteId != 0 && SiteArray.IsDeleted(_selectedSiteId))
            _selectedSiteId = 0;
        
        for (int i = _selectedUnits.Count - 1; i >= 0; i--)
        {
            if (UnitArray.IsDeleted(_selectedUnits[i]) || !KeepSelected(UnitArray[_selectedUnits[i]]))
                _selectedUnits.RemoveAt(i);
        }
    }

    private bool KeepSelected(Unit unit)
    {
        return unit.IsVisible() || unit.UnitType == UnitConstants.UNIT_CARAVAN || unit.UnitType == UnitConstants.UNIT_VESSEL;
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
        _buildFirmType = 0;
        _setStopId = 0;
    }
    
    public void Reset()
    {
        _prevViewMode = ViewMode.Normal;
        _viewMode = ViewMode.Normal;
        _selectedKingdomId = 0;
        _curTalkMsg = new TalkMsg();
        _talkChoiceIndex = 0;
        _choiceQuestion = String.Empty;
        _choiceQuestionSecondLine = String.Empty;
        _replyTalkMsgId = 0;
        
        _screenSquareFrameCount = 0;
        _screenSquareFrameStep = 1;
        ResetSelection();
        NeedFullRedraw = true;
    }
    
    #region SaveAndLoad

    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(_topLeftLocX);
        writer.Write(_topLeftLocY);
        writer.Write(_screenSquareFrameCount);
        writer.Write(_screenSquareFrameStep);
        writer.Write(_selectedTownId);
        writer.Write(_selectedFirmId);
        writer.Write(_selectedUnitId);
        writer.Write(_selectedUnits.Count);
        for (int i = 0; i < _selectedUnits.Count; i++)
            writer.Write(_selectedUnits[i]);
        writer.Write(_selectedSiteId);
        writer.Write(_selectedRaceId);
        writer.Write(_selectedShipId);
    }

    public void LoadFrom(BinaryReader reader)
    {
        _topLeftLocX = reader.ReadInt32();
        _topLeftLocY = reader.ReadInt32();
        _screenSquareFrameCount = reader.ReadInt32();
        _screenSquareFrameStep = reader.ReadInt32();
        _selectedTownId = reader.ReadInt32();
        _selectedFirmId = reader.ReadInt32();
        _selectedUnitId = reader.ReadInt32();
        int selectedUnitsCount = reader.ReadInt32();
        for (int i = 0; i < selectedUnitsCount; i++)
            _selectedUnits.Add(reader.ReadInt32());
        _selectedSiteId = reader.ReadInt32();
        _selectedRaceId = reader.ReadInt32();
        _selectedShipId = reader.ReadInt32();
    }
	
    #endregion
}