using System;
using System.Collections.Generic;
using System.Linq;

namespace TenKingdoms;

public partial class Renderer
{
    public const int WindowWidth = ZoomMapX + ZoomMapWidth + 12 + MiniMapSize + 12;
    public const int WindowHeight = ZoomMapY + ZoomMapHeight;

    private const int GameMenuX = 4;
    private const int GameMenuY = 2;
    private const int GameMenuWidth = 302;
    private const int GameMenuHeight = 48;

    private const int ZoomMapX = 0;
    private const int ZoomMapY = 8 + GameMenuHeight * 3 / 2;
    private const int ZoomMapLocWidth = 30; //width in cells
    private const int ZoomMapLocHeight = 19; //width in cells
    public const int ZoomTextureWidth = 48;
    public const int ZoomTextureHeight = 48;
    private const int ZoomMapWidth = ZoomMapLocWidth * ZoomTextureWidth;
    private const int ZoomMapHeight = ZoomMapLocHeight * ZoomTextureHeight;

    private const int MiniMapX = ZoomMapX + ZoomMapLocWidth * ZoomTextureWidth + 12;
    private const int MiniMapY = ZoomMapY;
    private const int MiniMapSize = 400;
    private const int MiniMapScale = MiniMapSize / GameConstants.MapSize;

    private const int DetailsX1 = ZoomMapX + ZoomMapLocWidth * ZoomTextureWidth + 8;
    private const int DetailsX2 = DetailsX1 + DetailsWidth;
    private const int DetailsY1 = MiniMapY + MiniMapSize + 9;
    private const int DetailsY2 = DetailsY1 + DetailsHeight;
    private const int DetailsWidth = 408;
    private const int DetailsHeight = 324;
    private const int Details1Width = 208;
    private const int Details1Height = 208;
    private const int Details2Width = 64;
    private const int Details2Height = Details1Height;
    private const int Details3Height = 104;
    private const int Details4Height = Details3Height;
    
    private const int TownFlagShiftX = -9;
    private const int TownFlagShiftY = -97;

    private int topLeftX;
    private int topLeftY;
    private long lastFrame;
    public bool NeedFullRedraw { get; set; }
    private int screenSquareFrameCount = 0;
    private int screenSquareFrameStep = 1;
    private readonly byte[] miniMapImage = new byte[MiniMapSize * MiniMapSize];
    private Dictionary<int, IntPtr> colorSquareTextures = new Dictionary<int, nint>();
    private int colorSquareWidth;
    private int colorSquareHeight;
    private IntPtr gameMenuTexture;
    private IntPtr detailsTexture1;
    private IntPtr detailsTexture2;
    private IntPtr detailsTexture3;
    private IntPtr detailsTexture4;

    private int selectedTownId;
    private int selectedFirmId;
    private int selectedUnitId;
    
    public Graphics Graphics { get; }

    public Font FontSan { get; private set; }
    public Font FontStd { get; private set; }

    private static TerrainRes TerrainRes => Sys.Instance.TerrainRes;
    private static HillRes HillRes => Sys.Instance.HillRes;
    private static PlantRes PlantRes => Sys.Instance.PlantRes;
    private static TownRes TownRes => Sys.Instance.TownRes;
    private static FirmRes FirmRes => Sys.Instance.FirmRes;
    private static SpriteRes SpriteRes => Sys.Instance.SpriteRes;

    private static Config Config => Sys.Instance.Config;
    private static Info Info => Sys.Instance.Info;
    private static World World => Sys.Instance.World;

    
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
        
        ResourceIdx imageButtons = new ResourceIdx($"{Sys.GameDataFolder}/Resource/I_BUTTON.RES");
        byte[] colorSquare = imageButtons.Read("V_COLCOD");
        colorSquareWidth = BitConverter.ToInt16(colorSquare, 0);
        colorSquareHeight = BitConverter.ToInt16(colorSquare, 2);
        byte[] colorSquareBitmap = colorSquare.Skip(4).ToArray();
        for (int i = 0; i <= InternalConstants.MAX_COLOR_SCHEME; i++)
        {
            int textureKey = ColorRemap.GetTextureKey(i, false);
            byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(colorSquareBitmap, colorSquareWidth, colorSquareHeight,
                ColorRemap.GetColorRemap(i, false).ColorTable);
            colorSquareTextures.Add(textureKey, graphics.CreateTextureFromBmp(decompressedBitmap, colorSquareWidth, colorSquareHeight));
        }

        ResourceIdx interfaceImages = new ResourceIdx($"{Sys.GameDataFolder}/Resource/I_IF.RES");
        byte[] mainScreenBitmap = interfaceImages.Read("MAINSCR");
        int mainScreenWidth = BitConverter.ToInt16(mainScreenBitmap, 0);
        int mainScreenHeight = BitConverter.ToInt16(mainScreenBitmap, 2);
        mainScreenBitmap = mainScreenBitmap.Skip(4).ToArray();

        byte[] gameMenuBitmap = graphics.CutBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight, 4, 2, GameMenuWidth, GameMenuHeight);
        gameMenuTexture = graphics.CreateTextureFromBmp(gameMenuBitmap, GameMenuWidth, GameMenuHeight);
        
        byte[] detailsBitmap1 = graphics.CutBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight, 584, 265,
            Details1Width, Details1Height);
        detailsTexture1 = graphics.CreateTextureFromBmp(detailsBitmap1, Details1Width, Details1Height);
        byte[] detailsBitmap2 = graphics.CutBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight, 584 + Details1Width - Details2Width, 265,
            Details2Width, Details2Height);
        detailsTexture2 = graphics.CreateTextureFromBmp(detailsBitmap2, Details2Width, Details2Height);
        byte[] detailsBitmap3 = graphics.CutBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight, 584, 265 + Details1Height,
            Details1Width, Details3Height);
        detailsTexture3 = graphics.CreateTextureFromBmp(detailsBitmap3, Details1Width, Details3Height);
        byte[] detailsBitmap4 = graphics.CutBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight, 584 + Details1Width - Details2Width, 265 + Details1Height,
            Details2Width, Details4Height);
        detailsTexture4 = graphics.CreateTextureFromBmp(detailsBitmap4, Details2Width, Details4Height);
    }
    
    public void DrawFrame()
    {
        if (lastFrame == Sys.Instance.FrameNumber && Sys.Instance.Speed != 0)
            return;

        lastFrame = Sys.Instance.FrameNumber;
        DrawMainScreen();
        DrawMap();
        DrawMiniMap();

        Graphics.ResetClipRectangle();
        if (selectedTownId != 0 && !TownArray.IsDeleted(selectedTownId))
        {
            DrawTownUI(TownArray[selectedTownId]);
        }
    }

    private void DrawMainScreen()
    {
        Graphics.DrawBitmapScale(gameMenuTexture, GameMenuX, GameMenuY, GameMenuWidth, GameMenuHeight);
        Graphics.DrawBitmapScale(detailsTexture1, DetailsX1, DetailsY1, Details1Width, Details1Height);
        Graphics.DrawBitmapScale(detailsTexture2, DetailsX1 + Details1Width * 3 / 2, DetailsY1, Details2Width, Details2Height);
        int totalHeight = DetailsY1 + Details1Height;
        int i = 0;
        while (totalHeight < WindowHeight)
        {
            Graphics.DrawBitmapScale(detailsTexture3, DetailsX1, DetailsY1 + Details1Height * 3 / 2 + i * Details3Height * 3 / 2,
                Details1Width, Details3Height);
            Graphics.DrawBitmapScale(detailsTexture4, DetailsX1 + Details1Width * 3 / 2, DetailsY1 + Details2Height * 3 / 2 + i * Details3Height * 3 / 2,
                Details2Width, Details4Height);
            totalHeight += Details3Height * 3 / 2;
            i++;
        }
    }

    private bool IsExplored(int x1Loc, int x2Loc, int y1Loc, int y2Loc)
    {
        if (Config.explore_whole_map)
            return true;
        
        for (int xLoc = x1Loc; xLoc <= x2Loc; xLoc++)
        {
            for (int yLoc = y1Loc; yLoc <= y2Loc; yLoc++)
            {
                Location location = World.get_loc(xLoc, yLoc);
                if (location.explored())
                {
                    return true;
                }
            }
        }

        return false;
    }
}