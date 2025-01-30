using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using SDL2;

namespace TenKingdoms;

public class Graphics
{
    public const string WindowTitle = "Ten Kingdoms";
    public const int WindowWidth = Renderer.WindowWidth;
    public const int WindowHeight = Renderer.WindowHeight;
    
    private const uint SDLSubSystems = SDL.SDL_INIT_VIDEO;
    private SDL.SDL_Color[] colors = new SDL.SDL_Color[256];
    private IntPtr window = IntPtr.Zero;
    private IntPtr renderer = IntPtr.Zero;
    private IntPtr surface = IntPtr.Zero;
    private IntPtr mainMenuTexture = IntPtr.Zero;
    private IntPtr mainScreenTexture = IntPtr.Zero;
    private List<IntPtr> textures = new List<IntPtr>();
    private bool initialized;

    public Graphics()
    {
    }

    private void LogError(string message)
    {
        Console.WriteLine(message + Environment.NewLine + SDL.SDL_GetError());
        SDL.SDL_ClearError();
    }

    private void LoadPalette()
    {
        using FileStream stream = new FileStream($"{Sys.GameDataFolder}/Resource/PAL_STD.RES", FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new BinaryReader(stream);
        for (int i = 0; i < 8; i++)
            reader.ReadByte();
        byte[] sourceColors = reader.ReadBytes(256 * 3);
        for (int i = 0; i < 256; i++)
        {
            colors[i].a = (byte)(i < Colors.MIN_TRANSPARENT_CODE ? 255 : 0);
            colors[i].r = sourceColors[i * 3];
            colors[i].g = sourceColors[i * 3 + 1];
            colors[i].b = sourceColors[i * 3 + 2];
        }
    }

    private void LoadTextures()
    {
        mainMenuTexture = SDL_image.IMG_LoadTexture(renderer, $"{Sys.GameDataFolder}/Images/M_MAIN.jpg");
        textures.Add(mainMenuTexture);
        mainScreenTexture = SDL_image.IMG_LoadTexture(renderer, $"{Sys.GameDataFolder}/Images/MAINSCR.jpg");
        textures.Add(mainScreenTexture);
    }
    
    public bool Init()
    {
        int errorCode = SDL.SDL_InitSubSystem(SDLSubSystems);
        if (errorCode != 0)
        {
            LogError("There was an error initializing SDL.");
            return false;
        }

        //Save global mouse position
        //SDL.SDL_GetGlobalMouseState();

        window = SDL.SDL_CreateWindow(WindowTitle, SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED,
            WindowWidth, WindowHeight, SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED);
        if (window == IntPtr.Zero)
        {
            LogError("There was an error when creating a window.");
            return false;
        }
        
        //SDL.SDL_SetWindowGrab();

        renderer = SDL.SDL_CreateRenderer(window, -1, 0);
        if (renderer == IntPtr.Zero)
        {
            LogError("There was an error when creating a renderer.");
            return false;
        }

        //SDL.SDL_SetRenderDrawBlendMode(renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
        
        //TODO set SDL hints
        //SDL_SetHint(SDL_HINT_RENDER_SCALE_QUALITY, "linear");
        //SDL_SetHint(SDL_HINT_MOUSE_RELATIVE_MODE_WARP, "1");

        //Restore global mouse position to avoid jump of a mouse cursor
        //SDL.SDL_WarpMouseGlobal();

        uint pixelFormat = SDL.SDL_GetWindowPixelFormat(window);
        if (pixelFormat == SDL.SDL_PIXELFORMAT_UNKNOWN)
        {
            LogError("There was an error with window pixel format.");
            return false;
        }

        surface = SDL.SDL_GetWindowSurface(window);
        if (surface == IntPtr.Zero)
        {
            LogError("There was an error when creating a surface.");
            return false;
        }

        SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_JPG | SDL_image.IMG_InitFlags.IMG_INIT_PNG);
        LoadPalette();
        LoadTextures();

        initialized = true;
        return true;
    }

    public void DeInit()
    {
        if (!initialized)
            return;
        
        SDL_image.IMG_Quit();

        foreach (var item in textures)
        {
            if (item != IntPtr.Zero)
            {
                SDL.SDL_DestroyTexture(item);
            }
        }
        
        if (renderer != IntPtr.Zero)
            SDL.SDL_DestroyRenderer(renderer);
        
        if (window != IntPtr.Zero)
            SDL.SDL_DestroyWindow(window);

        SDL.SDL_QuitSubSystem(SDLSubSystems);
        SDL.SDL_Quit();

        initialized = false;
    }

    /*public void SaveImageToDisc()
    {
        IntPtr sdlPalette = SDL.SDL_AllocPalette(256);
        SDL.SDL_SetPaletteColors(sdlPalette, colors, 0, 256);
       
        List<Resource> interfaceResources = ResourceReader.Read($"{Sys.GameDataFolder}/Resource/I_IF.RES");
        
        foreach (var interfaceResource in interfaceResources)
        {
            if (String.IsNullOrEmpty(interfaceResource.Name))
                continue;

            int width = interfaceResource.Data[1] * 256 + interfaceResource.Data[0];
            int height = interfaceResource.Data[3] * 256 + interfaceResource.Data[2];
            var pixels = interfaceResource.Data.Skip(4).Take(interfaceResource.Data.Length - 4).ToArray();
            GCHandle pinnedArray = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            IntPtr imageSurface = SDL.SDL_CreateRGBSurfaceFrom(pinnedArray.AddrOfPinnedObject(),
                width, height, 8, width, 0, 0, 0, 0);
            SDL.SDL_SetSurfacePalette(imageSurface, sdlPalette);
            IntPtr imageTexture = SDL.SDL_CreateTextureFromSurface(renderer, imageSurface);
            SDL.SDL_RenderCopy(renderer, imageTexture, IntPtr.Zero, IntPtr.Zero);
            SDL_image.IMG_SavePNG(imageSurface, $"{Sys.GameDataFolder}/Images/{interfaceResource.Name}.png");
            SDL_image.IMG_SaveJPG(imageSurface, $"{Sys.GameDataFolder}/Images/{interfaceResource.Name}.jpg", 100);
            pinnedArray.Free();
        }
    }*/

    public IntPtr CreateTextureFromBmp(byte[] bmpImage, int width, int height)
    {
        IntPtr sdlPalette = SDL.SDL_AllocPalette(256);
        SDL.SDL_SetPaletteColors(sdlPalette, colors, 0, 256);

        GCHandle pinnedBmpImage = GCHandle.Alloc(bmpImage, GCHandleType.Pinned);
        IntPtr imageSurface = SDL.SDL_CreateRGBSurfaceFrom(pinnedBmpImage.AddrOfPinnedObject(),
            width, height, 8, width, 0, 0, 0, 0);
        SDL.SDL_SetSurfacePalette(imageSurface, sdlPalette);
        SDL.SDL_SetSurfaceBlendMode(imageSurface, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
        IntPtr texture = SDL.SDL_CreateTextureFromSurface(renderer, imageSurface);
        SDL.SDL_SetTextureBlendMode(texture, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
        textures.Add(texture);
        pinnedBmpImage.Free();

        return texture;
    }
    
    public void DrawMainMenu()
    {
        SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
        //TODO should we call SDL_RenderClear?
        //SDL.SDL_RenderClear(renderer);
        SDL.SDL_RenderCopy(renderer, mainMenuTexture, IntPtr.Zero, IntPtr.Zero);

        //SDL_mixer.Mix_LoadWAV("/home/diesel/Projects/SevenKingdoms/WAR.WAV");
    }

    public void DrawMainScreen()
    {
        SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
        //TODO should we call SDL_RenderClear?
        //SDL.SDL_RenderClear(renderer);
        //SDL.SDL_RenderCopy(renderer, mainScreenTexture, IntPtr.Zero, IntPtr.Zero);
    }

    public void DrawPoint(int x, int y, int paletteColor)
    {
        var color = colors[paletteColor];
        SDL.SDL_SetRenderDrawColor(renderer, color.r, color.g, color.b, 255);
        SDL.SDL_RenderDrawPoint(renderer, x ,y);
    }

    public void DrawRect(int x, int y, int width, int height, int paletteColor)
    {
        var color = colors[paletteColor];
        SDL.SDL_SetRenderDrawColor(renderer, color.r, color.g, color.b, 255);
        SDL.SDL_Rect dstRect = new SDL.SDL_Rect();
        dstRect.x = x;
        dstRect.y = y;
        dstRect.w = width;
        dstRect.h = height;
        SDL.SDL_RenderFillRect(renderer, ref dstRect);
    }
    
    public void DrawBitmap(int x, int y, IntPtr texture, int width, int height)
    {
        SDL.SDL_SetTextureScaleMode(texture, SDL.SDL_ScaleMode.SDL_ScaleModeBest);
        SDL.SDL_Rect dstRect = new SDL.SDL_Rect();
        dstRect.x = x;
        dstRect.y = y;
        dstRect.w = width * 3 / 2;
        dstRect.h = height * 3 / 2;
        SDL.SDL_RenderCopy(renderer, texture, IntPtr.Zero, ref dstRect);
    }

    public void DrawBitmapFlip(int x, int y, IntPtr texture, int width, int height)
    {
        SDL.SDL_SetTextureScaleMode(texture, SDL.SDL_ScaleMode.SDL_ScaleModeBest);
        SDL.SDL_Rect dstRect = new SDL.SDL_Rect();
        dstRect.x = x;
        dstRect.y = y;
        dstRect.w = width * 3 / 2;
        dstRect.h = height * 3 / 2;
        SDL.SDL_RenderCopyEx(renderer, texture, IntPtr.Zero, ref dstRect, 0.0, IntPtr.Zero, SDL.SDL_RendererFlip.SDL_FLIP_HORIZONTAL);
    }
    
    public void DrawBitmapByPoints(int x, int y, byte[] bitmap, int width, int height)
    {
        for (int j = y; j < y + height; j++)
        {
            for (int i = x; i < x + width; i++)
            {
                var color = colors[bitmap[(j - y) * width + (i - x)]];
                SDL.SDL_SetRenderDrawColor(renderer, color.r, color.g, color.b, 255);
                SDL.SDL_RenderDrawPoint(renderer, i, j);
            }
        }
    }

    public byte[] DecompressTransparentBitmap(byte[] bitmap, int width, int height, byte[] colorTable = null)
    {
        int esi = 0;
        int pixelsToSkip = 0;

        byte[] result = new byte[width * height];
        int index = 0;

        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                if (pixelsToSkip != 0)
                {
                    if (pixelsToSkip >= width - i)
                    {
                        // skip to next line
                        pixelsToSkip -= width - i;
                        for (int k = 0; k < width - i; k++)
                        {
                            result[index] = Colors.MIN_TRANSPARENT_CODE;
                            index++;
                        }
                        break;
                    }

                    i += pixelsToSkip;
                    for (int k = 0; k < pixelsToSkip; k++)
                    {
                        result[index] = Colors.MIN_TRANSPARENT_CODE;
                        index++;
                    }
                    pixelsToSkip = 0;
                }

                byte al = bitmap[esi++]; // load source byte
                if (colorTable != null)
                    al = colorTable[al]; // translate
                if (al < Colors.MIN_TRANSPARENT_CODE)
                {
                    result[index] = al; // normal pixel
                    index++;
                }
                else if (al == Colors.MANY_TRANSPARENT_CODE)
                {
                    pixelsToSkip = bitmap[esi++] - 1; // skip many pixels
                    result[index] = Colors.MIN_TRANSPARENT_CODE;
                    index++;
                }
                else
                {
                    pixelsToSkip = 256 - al - 1; // skip (neg al) pixels
                    result[index] = Colors.MIN_TRANSPARENT_CODE;
                    index++;
                }
            }
        }

        return result;
    }

    public void Render()
    {
        SDL.SDL_RenderPresent(renderer);
    }
}