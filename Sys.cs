using System;
using System.Drawing;
using System.IO;
using SDL2;

namespace TenKingdoms;

public class Sys
{
    public const string GameDataFolder = "Assets";
    
    public static Sys Instance { get; set; }
    public long FrameNumber { get; private set; }
    private int FrameOfDay { get; set; }
    private int Speed { get; set; } = 1;
    public bool GameEnded { get; private set; }
    public bool ExitFlag { get; set; }
    private SavedGame _errorSavedGame;
    private MemoryStream _errorSaveStream;

    private Graphics Graphics { get; set; }
    private Renderer Renderer { get; set; }
    public Color[] PaletteColors { get; } = new Color[256];

    private GameSet GameSet { get; set; }
    public TerrainRes TerrainRes { get; private set; }
    public HillRes HillRes { get; private set; }
    public PlantRes PlantRes { get; private set; }
    public RockRes RockRes { get; private set; }
    public RawRes RawRes { get; private set; }
    public RaceRes RaceRes { get; private set; }
    public TownRes TownRes { get; private set; }
    public FirmRes FirmRes { get; private set; }
    public FirmDieRes FirmDieRes { get; private set; }
    public SpriteRes SpriteRes { get; private set; }
    public SpriteFrameRes SpriteFrameRes { get; private set; }
    public UnitRes UnitRes { get; private set; }
    public MonsterRes MonsterRes { get; private set; }
    public GodRes GodRes { get; private set; }
    public TechRes TechRes { get; private set; }
    public TalkRes TalkRes { get; private set; }
    public CursorRes CursorRes { get; private set; }
    public SERes SERes { get; private set; }
    public SECtrl SECtrl { get; private set; }

    public Config Config { get; private set; }
    public SaveGameProvider SaveGameProvider { get; private set; }
    public Info Info { get; private set; }
    public World World { get; private set; }
    public Weather Weather { get; set; }
    public Weather[] WeatherForecast { get; } = new Weather[GameConstants.MAX_WEATHER_FORECAST];
    public MagicWeather MagicWeather { get; private set; }
    public SeekPath SeekPath { get; private set; }
    
    public RegionArray RegionArray { get; private set; }
    public SiteArray SiteArray { get; private set; }
    public RockArray RockArray { get; private set; }
    public RockArray DirtArray { get; private set; }
    public NationArray NationArray { get; private set; }
    public TownArray TownArray { get; private set; }
    public FirmArray FirmArray { get; private set; }
    public FirmDieArray FirmDieArray { get; private set; }
    public UnitArray UnitArray { get; private set; }
    public RebelArray RebelArray { get; private set; }
    public SpyArray SpyArray { get; private set; }
    public NewsArray NewsArray { get; private set; }
    public BulletArray BulletArray { get; private set; }
    public WarPointArray WarPointArray { get; private set; }
    public EffectArray EffectArray { get; private set; }
    public TornadoArray TornadoArray { get; private set; }

    public Sys()
    {
    }

    private void LoadResources()
    {
        GameSet = new GameSet();
        GameSet.OpenSet(1);
        
        TerrainRes = new TerrainRes();
        HillRes = new HillRes();
        PlantRes = new PlantRes(TerrainRes);
        RockRes = new RockRes();
        RawRes = new RawRes(GameSet);

        SpriteRes = new SpriteRes(GameSet);
        SpriteFrameRes = new SpriteFrameRes(GameSet);
        UnitRes = new UnitRes(GameSet);
        MonsterRes = new MonsterRes(GameSet, UnitRes);
        GodRes = new GodRes(GameSet);
        RaceRes = new RaceRes(GameSet, UnitRes);

        FirmRes = new FirmRes(GameSet);
        FirmDieRes = new FirmDieRes(GameSet);
        TownRes = new TownRes(GameSet, RaceRes);

        TechRes = new TechRes(GameSet);
        TalkRes = new TalkRes();
        CursorRes = new CursorRes();
        SERes = new SERes(GameSet);
        SECtrl = new SECtrl();
    }

    private void CreateObjects()
    {
        Info = new Info();
        SeekPath = new SeekPath();

        RegionArray = new RegionArray();
        SiteArray = new SiteArray();
        RockArray = new RockArray();
        DirtArray = new RockArray();
        NationArray = new NationArray();
        TownArray = new TownArray();
        FirmArray = new FirmArray();
        FirmDieArray = new FirmDieArray();
        UnitArray = new UnitArray();
        RebelArray = new RebelArray();
        SpyArray = new SpyArray();
        NewsArray = new NewsArray();
        BulletArray = new BulletArray();
        WarPointArray = new WarPointArray();
        EffectArray = new EffectArray();
        TornadoArray = new TornadoArray();
        World = new World();
        
        int quakeFreq = Int16.MaxValue;
        if (Config.EarthquakeFrequency != 0)
        {
            Random random = new Random();
            quakeFreq = 2000 - Config.EarthquakeFrequency * 400 + random.Next(300);
        }
        Weather = new Weather();
        Weather.InitDate(Info.GameYear, Info.GameMonth, Info.GameDay, GameConstants.LATITUDE, quakeFreq);
        for (int i = 0; i < WeatherForecast.Length; i++)
        {
            WeatherForecast[i] = new Weather();
        }
        WeatherForecast[0] = new Weather(Weather);
        WeatherForecast[0].NextDay();
        for (int foreDay = 1; foreDay < GameConstants.MAX_WEATHER_FORECAST; foreDay++)
        {
            WeatherForecast[foreDay] = new Weather(WeatherForecast[foreDay - 1]);
            WeatherForecast[foreDay].NextDay();
        }

        MagicWeather = new MagicWeather();

        SERes.init1();
        SERes.init2(SECtrl);
        
        //TODO
        //for(int i = 0; i < FLAME_GROW_STEP; ++i)
            //flame[i].init(Flame::default_width(i), Flame::default_height(i), Flame::base_width(i), FLAME_WIDE);
    }

    private bool InitGraphics()
    {
        using FileStream stream = new FileStream($"{Sys.GameDataFolder}/Resource/PAL_STD.RES", FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new BinaryReader(stream);
        for (int i = 0; i < 8; i++)
            reader.ReadByte();
        byte[] sourceColors = reader.ReadBytes(256 * 3);
        for (int i = 0; i < PaletteColors.Length; i++)
        {
            PaletteColors[i] = Color.FromArgb(i < Colors.MIN_TRANSPARENT_CODE ? 255 : 0, sourceColors[i * 3], sourceColors[i * 3 + 1], sourceColors[i * 3 + 2]);
        }

        try
        {
            Graphics = new Graphics();
            if (!Graphics.Init(PaletteColors))
            {
                Graphics.DeInit();
                return false;
            }
        }
        catch (Exception e)
        {
            Graphics.ShowSimpleMessageBox("Error when initializing SDL: " + e.Message);
            Graphics.DeInit();
            return false;
        }

        return true;
    }

    private void ShowMainMenu()
    {
        Renderer.GameMode = GameMode.StartMenu;
        Graphics.SetWindowSize(Renderer.StartMenuWidth, Renderer.StartMenuHeight);
        Graphics.SetCursor(CursorRes[CursorType.NORMAL].GetCursor(Graphics));
    }

    public void StartNewGame()
    {
        FrameNumber = 0;
        FrameOfDay = 0;
        GameEnded = false;
        RaceRes.Reset();
        SpriteRes.Reset();
        TownRes.Reset();
        CreateObjects();
        Misc.RandomSeed = (uint)(new Random()).Next();
        MapGenerator mapGenerator = new MapGenerator();
        mapGenerator.Generate();
        Renderer.Reset();
        GoToPlayerTown();
        Renderer.GameMode = GameMode.Game;
        Graphics.SetWindowSize(Renderer.WindowWidth, Renderer.WindowHeight);
    }

    private void GoToPlayerTown()
    {
        if (NationArray.PlayerId != 0)
        {
            foreach (Town town in TownArray)
            {
                if (town.TownId == NationArray.PlayerId)
                {
                    Renderer.GoToLocation(town.LocCenterX, town.LocCenterY);
                }
            }
        }
    }

    public void Run()
    {
        Config = new Config();
        string error = Config.Load();
        if (!String.IsNullOrEmpty(error))
        {
            Graphics.ShowSimpleMessageBox("Error when loading Config.txt: " + error);
            return;
        }

        SaveGameProvider = new SaveGameProvider();
        
        ColorRemap.InitRemapTable();
        if (!InitGraphics())
            return;
        
        LoadResources();
        
        Renderer = new Renderer(Graphics);
        ShowMainMenu();

        try
        {
            MainLoop();
        }
        catch (Exception e)
        {
            if (_errorSaveStream != null && _errorSavedGame != null)
            {
                SaveGameProvider.SaveGame(_errorSavedGame, _errorSaveStream, true);
                Graphics.ShowSimpleMessageBox("Something went wrong. Game saved to " + _errorSavedGame.FileName);
            }
        }

        Graphics.DeInit();
    }
    
    private void Process()
    {
        UnitArray.Process();
        FirmArray.Process();
        TownArray.Process();
        NationArray.Process();
        BulletArray.Process();
        World.Process();
        TornadoArray.Process();
        RockArray.Process();
        DirtArray.Process();
        EffectArray.Process();
        WarPointArray.Process();
        FirmDieArray.Process();

        FrameOfDay++;
        if (FrameOfDay >= InternalConstants.FRAMES_PER_DAY)
        {
            Info.NextDay();
            World.NextDay();
            SiteArray.NextDay();
            RebelArray.NextDay();
            SpyArray.NextDay();
            if (GameConstants.WEATHER_EFFECT)
                SpriteRes.UpdateSpeed();
            TalkRes.NextDay();
            RegionArray.NextDay();

            FrameOfDay = 0;
        }

        if (FrameNumber % (InternalConstants.FRAMES_PER_DAY * 30) == 0)
        {
            // recreate mini-map texture every month to display growing and dying plants
            Renderer.NeedFullRedraw = true;
        }
        
        if (Config.SaveOnError && FrameNumber % 100 == 0)
            SaveCurrentGame();
    }

    private void MainLoop()
    {
        Renderer.DrawFrame(false);
        Graphics.Render();
        
        long lastFrameTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        long lastScrollTime = DateTime.Now.Ticks;
        while (true)
        {
            if (ExitFlag)
                return;

            if (Renderer.GameMode != GameMode.Game)
            {
                if (SDL.SDL_WaitEventTimeout(out SDL.SDL_Event sdlEvent, InternalConstants.SCROLL_INTERVAL) == 1)
                {
                    if (sdlEvent.type == SDL.SDL_EventType.SDL_QUIT)
                        return;
                
                    ProcessEvent(sdlEvent);
                    Renderer.DrawFrame(false);
                    Graphics.Render();
                }
                
                continue;
            }
            
            bool needRedraw = false;
            bool nextFrameReady = false;
            if (Speed > 0)
            {
                long nextFrameTime = lastFrameTime + 1000 / (Speed * 3);
                long currentMilliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                if (currentMilliseconds < nextFrameTime)
                {
                    if (SDL.SDL_WaitEventTimeout(out SDL.SDL_Event sdlEvent, Math.Min((int)(nextFrameTime - currentMilliseconds), InternalConstants.SCROLL_INTERVAL)) == 1)
                    {
                        if (sdlEvent.type == SDL.SDL_EventType.SDL_QUIT)
                            return;

                        needRedraw = ProcessEvent(sdlEvent);
                    }
                }
                else
                {
                    lastFrameTime = currentMilliseconds;
                    FrameNumber++;
                    nextFrameReady = true;
                    int hasEvent = SDL.SDL_PollEvent(out SDL.SDL_Event sdlEvent);
                    if (hasEvent == 1)
                    {
                        if (sdlEvent.type == SDL.SDL_EventType.SDL_QUIT)
                            return;

                        needRedraw = ProcessEvent(sdlEvent);
                    }
                }
            }
            else
            {
                if (SDL.SDL_WaitEventTimeout(out SDL.SDL_Event sdlEvent, InternalConstants.SCROLL_INTERVAL) == 1)
                {
                    if (sdlEvent.type == SDL.SDL_EventType.SDL_QUIT)
                        return;
                
                    needRedraw = ProcessEvent(sdlEvent);
                }
            }

            if (nextFrameReady)
                Process();

            if (DateTime.Now.Ticks - lastScrollTime > InternalConstants.SCROLL_INTERVAL * TimeSpan.TicksPerMillisecond)
            {
                uint buttonMask = SDL.SDL_GetMouseState(out int mouseX, out int mouseY);
                bool scrollUp = (mouseX >= 0 && mouseX < InternalConstants.SCROLL_WIDTH);
                bool scrollDown = (mouseX >= Renderer.WindowWidth - InternalConstants.SCROLL_WIDTH && mouseX < Renderer.WindowWidth);
                bool scrollLeft = (mouseY >= 0 && mouseY < InternalConstants.SCROLL_WIDTH);
                bool scrollRight = (mouseY >= Renderer.WindowHeight - InternalConstants.SCROLL_WIDTH && mouseY < Renderer.WindowHeight);
                bool leftButtonPressed = ((buttonMask & SDL.SDL_BUTTON_LMASK) != 0);
                if ((scrollUp || scrollDown || scrollLeft || scrollRight) && !leftButtonPressed)
                {
                    Renderer.Scroll(scrollUp, scrollDown, scrollLeft, scrollRight);
                    needRedraw = true;
                    lastScrollTime = DateTime.Now.Ticks;
                }
            }

            if (needRedraw || nextFrameReady)
            {
                Renderer.DrawFrame(nextFrameReady);
                Graphics.Render();
            }
        }
    }

    private bool ProcessEvent(SDL.SDL_Event sdlEvent)
    {
        if (sdlEvent.type == SDL.SDL_EventType.SDL_KEYDOWN)
        {
            ProcessKeyboardEvent(sdlEvent.key);
            return true;
        }

        if (sdlEvent.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN || sdlEvent.type == SDL.SDL_EventType.SDL_MOUSEBUTTONUP)
        {
            ProcessMouseButtonEvent(sdlEvent.button);
            return true;
        }

        if (sdlEvent.type == SDL.SDL_EventType.SDL_MOUSEMOTION)
        {
            ProcessMouseMotionEvent(sdlEvent.motion);
            return true;
        }

        return false;
    }

    private void ProcessKeyboardEvent(SDL.SDL_KeyboardEvent keyboardEvent)
    {
        if (keyboardEvent.keysym.sym >= SDL.SDL_Keycode.SDLK_0 && keyboardEvent.keysym.sym <= SDL.SDL_Keycode.SDLK_9)
        {
            Speed = keyboardEvent.keysym.sym - SDL.SDL_Keycode.SDLK_0;
        }
        if (keyboardEvent.keysym.sym >= SDL.SDL_Keycode.SDLK_KP_1 && keyboardEvent.keysym.sym <= SDL.SDL_Keycode.SDLK_KP_0)
        {
            Speed = keyboardEvent.keysym.sym - SDL.SDL_Keycode.SDLK_KP_1 + 1;
            if (keyboardEvent.keysym.sym >= SDL.SDL_Keycode.SDLK_KP_0)
                Speed = 0;
        }

        if (Speed == 9)
            Speed = 50;

        if (keyboardEvent.keysym.sym == SDL.SDL_Keycode.SDLK_g && Info.GameYear == 1000 && Info.GameMonth == 1)
            StartNewGame();
        
        if (keyboardEvent.keysym.sym == SDL.SDL_Keycode.SDLK_x)
            Renderer.ProcessInput(InputConstants.KeyXPressed, 0, 0);
        
        if (keyboardEvent.keysym.sym == SDL.SDL_Keycode.SDLK_LEFT)
            Renderer.ProcessInput(InputConstants.KeyLeftPressed, 0, 0);

        if (keyboardEvent.keysym.sym == SDL.SDL_Keycode.SDLK_RIGHT)
            Renderer.ProcessInput(InputConstants.KeyRightPressed, 0, 0);

        if (keyboardEvent.keysym.sym == SDL.SDL_Keycode.SDLK_DOWN)
            Renderer.ProcessInput(InputConstants.KeyDownPressed, 0, 0);

        if (keyboardEvent.keysym.sym == SDL.SDL_Keycode.SDLK_UP)
            Renderer.ProcessInput(InputConstants.KeyUpPressed, 0, 0);
    }

    private void ProcessMouseButtonEvent(SDL.SDL_MouseButtonEvent mouseButtonEvent)
    {
        if (mouseButtonEvent.button == 1 && mouseButtonEvent.state == 1)
        {
            //Left mouse button pressed
            Renderer.ProcessInput(InputConstants.LeftMouseDown, mouseButtonEvent.x, mouseButtonEvent.y);
        }
        if (mouseButtonEvent.button == 1 && mouseButtonEvent.state == 0)
        {
            //Left mouse button released
            Renderer.ProcessInput(InputConstants.LeftMouseUp, mouseButtonEvent.x, mouseButtonEvent.y);
        }
        if (mouseButtonEvent.button == 3 && mouseButtonEvent.state == 1)
        {
            //Right mouse button pressed
            Renderer.ProcessInput(InputConstants.RightMouseDown, mouseButtonEvent.x, mouseButtonEvent.y);
        }
        if (mouseButtonEvent.button == 3 && mouseButtonEvent.state == 0)
        {
            //Right mouse button released
            Renderer.ProcessInput(InputConstants.RightMouseUp, mouseButtonEvent.x, mouseButtonEvent.y);
        }
    }

    private void ProcessMouseMotionEvent(SDL.SDL_MouseMotionEvent mouseMotionEvent)
    {
        Renderer.ProcessInput(InputConstants.MouseMotion, mouseMotionEvent.x, mouseMotionEvent.y);
    }

    public void EndGame(int winNationId, bool playerDestroyed, int surrenderToNationId = 0, bool retire = false)
    {
    }

    public void set_view_mode(int viewMode, int viewingNationRecno = 0, int viewingSpyRecno = 0)
    {
        //TODO move to Renderer and use events
    }

    private void Save(Stream stream, SavedGame savedGame)
    {
        BinaryWriter writer = new BinaryWriter(stream);
        savedGame.SaveTo(writer);
        SaveTo(writer);
        Config.SaveTo(writer);
        SpriteRes.SaveTo(writer);
        RaceRes.SaveTo(writer);
        TownRes.SaveTo(writer);
        TalkRes.SaveTo(writer);
        Renderer.SaveTo(writer);
        ColorRemap.SaveTo(writer);
        Misc.SaveTo(writer);
        Info.SaveTo(writer);
        SeekPath.SaveTo(writer);
        RegionArray.SaveTo(writer);
        SiteArray.SaveTo(writer);
        RockArray.SaveTo(writer);
        DirtArray.SaveTo(writer);
        NationArray.SaveTo(writer);
        TownArray.SaveTo(writer);
        FirmArray.SaveTo(writer);
        FirmDieArray.SaveTo(writer);
        UnitArray.SaveTo(writer);
        RebelArray.SaveTo(writer);
        SpyArray.SaveTo(writer);
        NewsArray.SaveTo(writer);
        BulletArray.SaveTo(writer);
        WarPointArray.SaveTo(writer);
        EffectArray.SaveTo(writer);
        TornadoArray.SaveTo(writer);
        World.SaveTo(writer);
        Weather.SaveTo(writer);
        for (int i = 0; i < WeatherForecast.Length; i++)
            WeatherForecast[i].SaveTo(writer);
        MagicWeather.SaveTo(writer);
    }

    private void Load(Stream stream, SavedGame savedGame)
    {
        BinaryReader reader = new BinaryReader(stream);
        savedGame.LoadFrom(reader);
        LoadFrom(reader);
        Config.LoadFrom(reader);
        SpriteRes.LoadFrom(reader);
        RaceRes.LoadFrom(reader);
        TownRes.LoadFrom(reader);
        TalkRes.LoadFrom(reader);
        Renderer.LoadFrom(reader);
        ColorRemap.LoadFrom(reader);
        Misc.LoadFrom(reader);
        Info.LoadFrom(reader);
        SeekPath.LoadFrom(reader);
        RegionArray.LoadFrom(reader);
        SiteArray.LoadFrom(reader);
        RockArray.LoadFrom(reader);
        DirtArray.LoadFrom(reader);
        NationArray.LoadFrom(reader);
        TownArray.LoadFrom(reader);
        FirmArray.LoadFrom(reader);
        FirmDieArray.LoadFrom(reader);
        UnitArray.LoadFrom(reader);
        RebelArray.LoadFrom(reader);
        SpyArray.LoadFrom(reader);
        NewsArray.LoadFrom(reader);
        BulletArray.LoadFrom(reader);
        WarPointArray.LoadFrom(reader);
        EffectArray.LoadFrom(reader);
        TornadoArray.LoadFrom(reader);
        World.LoadFrom(reader);
        Weather.LoadFrom(reader);
        for (int i = 0; i < WeatherForecast.Length; i++)
            WeatherForecast[i].LoadFrom(reader);
        MagicWeather.LoadFrom(reader);
    }

    public void SaveGame(SavedGame savedGame)
    {
        bool createNew = savedGame == null;
        savedGame ??= new SavedGame();
        savedGame.RaceId = (NationArray.Player != null ? NationArray.Player.RaceId : 0);
        savedGame.ColorSchemeId = (NationArray.Player != null ? NationArray.Player.ColorSchemeId : 0);
        savedGame.PlayerName = (NationArray.Player != null ? NationArray.GetHumanName(NationArray.Player.NationNameId) : "Unknown");
        savedGame.GameDate = Info.GameDate;
        
        MemoryStream stream = new MemoryStream();
        Save(stream, savedGame);
        SaveGameProvider.SaveGame(savedGame, stream, createNew);
        Graphics.ShowSimpleMessageBox("Game saved to " + savedGame.FileName);
    }

    public bool LoadGame(SavedGame savedGame)
    {
        if (savedGame == null)
            return true;
        
        try
        {
            MemoryStream stream = SaveGameProvider.LoadGame(savedGame);
            if (stream != null)
            {
                CreateObjects();
                Renderer.Reset();
                Load(stream, savedGame);
                return true;
            }
        }
        catch (Exception e)
        {
            Graphics.ShowSimpleMessageBox("Something went wrong while loading " + savedGame.FileName + Environment.NewLine + e.Message);
            return false;
        }

        return false;
    }

    public void DeleteGame(SavedGame savedGame)
    {
        if (SaveGameProvider.DeleteGame(savedGame))
        {
            Graphics.ShowSimpleMessageBox("Game " + savedGame.FileName + " is deleted");
        }
    }

    private void SaveCurrentGame()
    {
        _errorSaveStream = new MemoryStream();
        _errorSavedGame = new SavedGame();
        _errorSavedGame.RaceId = (NationArray.Player != null ? NationArray.Player.RaceId : 0);
        _errorSavedGame.ColorSchemeId = (NationArray.Player != null ? NationArray.Player.ColorSchemeId : 0);
        _errorSavedGame.PlayerName = "Error";
        _errorSavedGame.GameDate = Info.GameDate;
        Save(_errorSaveStream, _errorSavedGame);
    }
    
    #region SaveAndLoad

    private void SaveTo(BinaryWriter writer)
    {
        writer.Write(FrameNumber);
        writer.Write(FrameOfDay);
        writer.Write(GameEnded);
    }

    private void LoadFrom(BinaryReader reader)
    {
        FrameNumber = reader.ReadInt64();
        FrameOfDay = reader.ReadInt32();
        GameEnded = reader.ReadBoolean();
    }
	
    #endregion
}