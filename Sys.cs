using System;
using System.Drawing;
using System.IO;
using SDL2;

namespace TenKingdoms;

public class Sys
{
    public const string GameDataFolder = "/home/diesel/Projects/TenKingdoms";
    
    public static Sys Instance { get; set; }
    public long FrameNumber { get; private set; }
    private int FrameOfDay { get; set; }
    public int Speed { get; private set; } = 1;
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
    public SERes SERes { get; private set; }
    public SECtrl SECtrl { get; private set; }

    public Config Config { get; private set; }
    public ConfigAdv ConfigAdv { get; private set; }
    public Info Info { get; private set; }
    public Power Power { get; private set; }
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
        SERes = new SERes(GameSet);
        SECtrl = new SECtrl();
    }

    private void CreateObjects()
    {
        Info = new Info();
        Power = new Power();
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
        
        int quakeFreq = Int32.MaxValue;
        if (Config.random_event_frequency != 0)
        {
            quakeFreq = 2000 - Config.random_event_frequency * 400 + Info.random_seed % 300;
        }
        Weather = new Weather();
        Weather.init_date(Info.game_year, Info.game_month, Info.game_day, Config.latitude, quakeFreq);
        for (int i = 0; i < WeatherForecast.Length; i++)
        {
            WeatherForecast[i] = new Weather();
        }
        WeatherForecast[0] = new Weather(Weather);
        WeatherForecast[0].next_day();
        for (int foreDay = 1; foreDay < GameConstants.MAX_WEATHER_FORECAST; foreDay++)
        {
            WeatherForecast[foreDay] = new Weather(WeatherForecast[foreDay - 1]);
            WeatherForecast[foreDay].next_day();
        }

        MagicWeather = new MagicWeather();

        SERes.init1();
        SERes.init2(SECtrl);
        
        //TODO
        //for(int i = 0; i < FLAME_GROW_STEP; ++i)
            //flame[i].init(Flame::default_width(i), Flame::default_height(i), Flame::base_width(i), FLAME_WIDE);

        FrameNumber = 0;
        FrameOfDay = 0;
        GameEnded = false;
    }

    private void InitGraphics()
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
                DeinitGraphics();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            DeinitGraphics();
        }
    }

    private void DeinitGraphics()
    {
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
            Info.next_day();
            World.next_day();
            SiteArray.NextDay();
            RebelArray.next_day();
            SpyArray.NextDay();
            if (Config.weather_effect != 0)
                SpriteRes.update_speed();
            RawRes.next_day();
            TalkRes.next_day();
            RegionArray.next_day();

            FrameOfDay = 0;
        }

        if (FrameNumber % (InternalConstants.FRAMES_PER_DAY * 30) == 0)
        {
            // recreate mini-map texture every month to display growing and dying plants
            Renderer.NeedFullRedraw = true;
        }
    }

    private void ProcessEvent(SDL.SDL_Event sdlEvent)
    {
        if (sdlEvent.type == SDL.SDL_EventType.SDL_KEYDOWN)
        {
            ProcessKeyboardEvent(sdlEvent.key);
        }

        if (sdlEvent.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN || sdlEvent.type == SDL.SDL_EventType.SDL_MOUSEBUTTONUP)
        {
            ProcessMouseButtonEvent(sdlEvent.button);
        }
    }
    
    private void MainLoop()
    {
        long lastFrameTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        while (true)
        {
            //TODO process all events
            bool hasEvent = false;
            bool nextFrameReady = false;
            SDL.SDL_Event sdlEvent = default;
            if (Speed > 0)
            {
                long nextFrameTime = lastFrameTime + 1000 / (Speed * 3);
                long currentMilliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                if (currentMilliseconds < nextFrameTime)
                {
                    hasEvent = (SDL.SDL_WaitEventTimeout(out sdlEvent, (int)(nextFrameTime - currentMilliseconds)) == 1);
                    if (hasEvent)
                    {
                        if (sdlEvent.type == SDL.SDL_EventType.SDL_QUIT)
                            return;

                        ProcessEvent(sdlEvent);
                    }
                }
                else
                {
                    while (SDL.SDL_PollEvent(out sdlEvent) == 1)
                    {
                        hasEvent = true;
                        if (sdlEvent.type == SDL.SDL_EventType.SDL_QUIT)
                            return;

                        ProcessEvent(sdlEvent);
                    }
                    
                    lastFrameTime = currentMilliseconds;
                    FrameNumber++;
                    nextFrameReady = true;
                }
            }
            else
            {
                hasEvent = (SDL.SDL_WaitEvent(out sdlEvent) == 1);
                if (hasEvent)
                {
                    if (sdlEvent.type == SDL.SDL_EventType.SDL_QUIT)
                        return;
                
                    ProcessEvent(sdlEvent);
                }
            }

            if (nextFrameReady)
                Process();

            if (hasEvent || nextFrameReady)
            {
                Renderer.DrawFrame();
                Graphics.Render();
            }
        }
    }

    private void ProcessKeyboardEvent(SDL.SDL_KeyboardEvent keyboardEvent)
    {
        if (keyboardEvent.keysym.sym == SDL.SDL_Keycode.SDLK_g)
        {
            CreateObjects();
            MapGenerator mapGenerator = new MapGenerator();
            mapGenerator.Generate();
            Renderer.NeedFullRedraw = true;
        }

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
    }

    //private List<SDL.SDL_MouseButtonEvent> mouseEvents = new List<SDL.SDL_MouseButtonEvent>();
    private void ProcessMouseButtonEvent(SDL.SDL_MouseButtonEvent mouseButtonEvent)
    {
        //mouseEvents.Add(mouseButtonEvent);
        if (mouseButtonEvent.button == 1 && mouseButtonEvent.state == 1)
        {
            //Left mouse button pressed
        }
        if (mouseButtonEvent.button == 1 && mouseButtonEvent.state == 0)
        {
            //Left mouse button released
            Renderer.ProcessInput(InputConstants.LeftMousePressed, mouseButtonEvent.x, mouseButtonEvent.y);
        }
    }

    public void Run()
    {
        Config = new Config();
        ConfigAdv = new ConfigAdv();
        ColorRemap.InitRemapTable();
        InitGraphics();
        LoadResources();
        Renderer = new Renderer(Graphics);
        CreateObjects();
        MapGenerator mapGenerator = new MapGenerator();
        mapGenerator.Generate();
        Renderer.NeedFullRedraw = true;

        /*try
        {
            MainLoop();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }*/
        MainLoop();

        DeinitGraphics();
    }

    public void EndGame(int winNationRecno, int playerDestroyed = 0, int surrenderToNationRecno = 0, int retireFlag = 0)
    {
    }

    public void set_view_mode(int viewMode, int viewingNationRecno = 0, int viewingSpyRecno = 0)
    {
    }

    public void disp_view_mode(int observeMode = 0)
    {
    }
}