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
            quakeFreq = 2000 - Config.EarthquakeFrequency * 400 + Info.RandomSeed % 300;
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
            SDL.SDL_ShowSimpleMessageBox(SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR, "Ten Kingdoms", 
                "Error when initializing SDL: " + e.Message, IntPtr.Zero);
            Graphics.DeInit();
            return false;
        }

        return true;
    }

    private void Reset()
    {
        //TODO reset all static variables
        FrameNumber = 0;
        FrameOfDay = 0;
        GameEnded = false;
        CreateObjects();
        MapGenerator mapGenerator = new MapGenerator();
        mapGenerator.Generate();
        Renderer.Reset();
        GoToPlayerTown();
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
            SDL.SDL_ShowSimpleMessageBox(SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR, "Ten Kingdoms", 
                "Error when loading Config.txt: " + error, IntPtr.Zero);
            return;
        }
        
        ColorRemap.InitRemapTable();
        if (!InitGraphics())
            return;
        
        LoadResources();
        
        Renderer = new Renderer(Graphics);
        Reset();

        /*try
        {
            MainLoop();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }*/
        MainLoop();

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
    }

    private void MainLoop()
    {
        long lastFrameTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        long lastScrollTime = DateTime.Now.Ticks;
        while (true)
        {
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
            Reset();
        
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
}