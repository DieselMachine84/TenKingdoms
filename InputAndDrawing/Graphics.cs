using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using SDL2;

namespace TenKingdoms;

public class Graphics
{
    public const string WindowTitle = "Ten Kingdoms";
    public const int WindowWidth = Renderer.WindowWidth;
    public const int WindowHeight = Renderer.WindowHeight;
    
    private const uint SDLSubSystems = SDL.SDL_INIT_VIDEO;
    private IntPtr _window = IntPtr.Zero;
    private IntPtr _renderer = IntPtr.Zero;
    private IntPtr _surface = IntPtr.Zero;
    private SDL.SDL_Color[] _colors;
    private IntPtr _palette = IntPtr.Zero; 
    private IntPtr _miniMapTexture = IntPtr.Zero;
    private readonly List<IntPtr> _textures = new List<IntPtr>();
    private readonly List<IntPtr> _cursors = new List<IntPtr>();
    private bool _initialized;

    public Graphics()
    {
    }

    private void LogError(string message)
    {
        Console.WriteLine(message + Environment.NewLine + SDL.SDL_GetError());
        SDL.SDL_ClearError();
    }

    public bool Init(Color[] paletteColors)
    {
        int errorCode = SDL.SDL_InitSubSystem(SDLSubSystems);
        if (errorCode != 0)
        {
            LogError("There was an error initializing SDL.");
            return false;
        }

        _window = SDL.SDL_CreateWindow(WindowTitle, SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED,
            WindowWidth, WindowHeight, SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED);
        if (_window == IntPtr.Zero)
        {
            LogError("There was an error when creating a window.");
            return false;
        }
        
        _renderer = SDL.SDL_CreateRenderer(_window, -1, 0);
        if (_renderer == IntPtr.Zero)
        {
            LogError("There was an error when creating a renderer.");
            return false;
        }

        //SDL.SDL_SetRenderDrawBlendMode(renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
        
        //TODO set SDL hints
        //SDL_SetHint(SDL_HINT_RENDER_SCALE_QUALITY, "linear");
        //SDL_SetHint(SDL_HINT_MOUSE_RELATIVE_MODE_WARP, "1");

        uint pixelFormat = SDL.SDL_GetWindowPixelFormat(_window);
        if (pixelFormat == SDL.SDL_PIXELFORMAT_UNKNOWN)
        {
            LogError("There was an error with window pixel format.");
            return false;
        }

        _surface = SDL.SDL_GetWindowSurface(_window);
        if (_surface == IntPtr.Zero)
        {
            LogError("There was an error when creating a surface.");
            return false;
        }

        //SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_JPG | SDL_image.IMG_InitFlags.IMG_INIT_PNG);

        _colors = new SDL.SDL_Color[paletteColors.Length];
        for (int i = 0; i < _colors.Length; i++)
        {
            _colors[i].a = paletteColors[i].A;
            _colors[i].r = paletteColors[i].R;
            _colors[i].g = paletteColors[i].G;
            _colors[i].b = paletteColors[i].B;
        }
        _palette = SDL.SDL_AllocPalette(_colors.Length);
        SDL.SDL_SetPaletteColors(_palette, _colors, 0, _colors.Length);

        _initialized = true;
        return true;
    }

    public void DeInit()
    {
        if (!_initialized)
            return;
        
        //SDL_image.IMG_Quit();
        SDL.SDL_FreePalette(_palette);

        foreach (var texture in _textures)
        {
            if (texture != IntPtr.Zero)
            {
                SDL.SDL_DestroyTexture(texture);
            }
        }

        foreach (var cursor in _cursors)
        {
            if (cursor != IntPtr.Zero)
            {
                SDL.SDL_FreeCursor(cursor);
            }
        }

        if (_miniMapTexture != IntPtr.Zero)
            SDL.SDL_DestroyTexture(_miniMapTexture);
        
        if (_renderer != IntPtr.Zero)
            SDL.SDL_DestroyRenderer(_renderer);
        
        if (_window != IntPtr.Zero)
            SDL.SDL_DestroyWindow(_window);

        SDL.SDL_QuitSubSystem(SDLSubSystems);
        SDL.SDL_Quit();

        _initialized = false;
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

    public byte[] CopyBitmapRect(byte[] bitmap, int bitmapWidth, int bitmapHeight, int x, int y, int width, int height)
    {
        byte[] result = new byte[width * height];
        int index = 0;
        for (int h = y; h < y + height; h++)
        {
            for (int w = x; w < x + width; w++)
            {
                result[index] = bitmap[h * bitmapWidth + w];
                index++;
            }
        }

        return result;
    }

    public IntPtr CreateTextureFromBmp(byte[] bmpImage, int width, int height, int depth = 8, bool addToList = true)
    {
        GCHandle pinnedBmpImage = GCHandle.Alloc(bmpImage, GCHandleType.Pinned);
        IntPtr imageSurface = SDL.SDL_CreateRGBSurfaceFrom(pinnedBmpImage.AddrOfPinnedObject(),
            width, height, depth, width * depth / 8, 0, 0, 0, 0);
        if (depth == 8)
            SDL.SDL_SetSurfacePalette(imageSurface, _palette);
        SDL.SDL_SetSurfaceBlendMode(imageSurface, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);

        IntPtr texture = SDL.SDL_CreateTextureFromSurface(_renderer, imageSurface);
        SDL.SDL_SetTextureBlendMode(texture, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
        if (addToList)
            _textures.Add(texture);
        pinnedBmpImage.Free();
        SDL.SDL_FreeSurface(imageSurface);
        return texture;
    }

    public IntPtr CreateCursor(byte[] bmpImage, int width, int height, int hotSpotX, int hotSpotY)
    {
        GCHandle pinnedBmpImage = GCHandle.Alloc(bmpImage, GCHandleType.Pinned);
        IntPtr imageSurface = SDL.SDL_CreateRGBSurfaceFrom(pinnedBmpImage.AddrOfPinnedObject(),
            width, height, 8, width, 0, 0, 0, 0);
        SDL.SDL_SetSurfacePalette(imageSurface, _palette);
        SDL.SDL_SetSurfaceBlendMode(imageSurface, SDL.SDL_BlendMode.SDL_BLENDMODE_NONE);
        IntPtr convertedSurface = SDL.SDL_ConvertSurfaceFormat(imageSurface, SDL.SDL_PIXELFORMAT_ARGB8888, 0);
        SDL.SDL_SetSurfaceBlendMode(convertedSurface, SDL.SDL_BlendMode.SDL_BLENDMODE_NONE);
        IntPtr scaledSurface = SDL.SDL_CreateRGBSurfaceWithFormat(0, width * 3 / 2, height * 3 / 2, 32, SDL.SDL_PIXELFORMAT_ARGB8888);
        SDL.SDL_SetSurfaceBlendMode(scaledSurface, SDL.SDL_BlendMode.SDL_BLENDMODE_NONE);
        SDL.SDL_BlitScaled(convertedSurface, IntPtr.Zero, scaledSurface, IntPtr.Zero);

        IntPtr cursor = SDL.SDL_CreateColorCursor(scaledSurface, hotSpotX * 3 / 2, hotSpotY * 3 / 2);
        _cursors.Add(cursor);
        pinnedBmpImage.Free();
        SDL.SDL_FreeSurface(imageSurface);
        SDL.SDL_FreeSurface(convertedSurface);
        SDL.SDL_FreeSurface(scaledSurface);
        return cursor;
    }

    public void SetCursor(IntPtr cursor)
    {
        SDL.SDL_SetCursor(cursor);
    }

    public void SetClipRectangle(int x, int y, int width, int height)
    {
        SDL.SDL_Rect rect = new SDL.SDL_Rect();
        rect.x = x;
        rect.y = y;
        rect.w = width;
        rect.h = height;
        SDL.SDL_RenderSetClipRect(_renderer, ref rect);
    }

    public void ResetClipRectangle()
    {
        SDL.SDL_RenderSetClipRect(_renderer, IntPtr.Zero);
    }
    
    public void CreateMiniMapTexture(byte[] image, int width, int height)
    {
        if (_miniMapTexture != IntPtr.Zero)
            SDL.SDL_DestroyTexture(_miniMapTexture);
        
        _miniMapTexture = CreateTextureFromBmp(image, width, height, 8, false);
    }

    public void DrawMiniMapGround(int x, int y, int width, int height)
    {
        DrawBitmap(_miniMapTexture, x, y, width, height);
    }

    public void DrawPoint(int x, int y, int paletteColor)
    {
        var color = _colors[paletteColor];
        SDL.SDL_SetRenderDrawColor(_renderer, color.r, color.g, color.b, 255);
        SDL.SDL_RenderDrawPoint(_renderer, x ,y);
    }

    public void DrawLine(int x1, int y1, int x2, int y2, int paletteColor)
    {
        var color = _colors[paletteColor];
        SDL.SDL_SetRenderDrawColor(_renderer, color.r, color.g, color.b, 255);
        SDL.SDL_RenderDrawLine(_renderer, x1, y1, x2, y2);
    }

    public void DrawRect(int x, int y, int width, int height, int paletteColor)
    {
        var color = _colors[paletteColor];
        DrawRect(x, y, width, height, color.r, color.g, color.b);
    }

    public void DrawRect(int x, int y, int width, int height, byte r, byte g, byte b)
    {
        SDL.SDL_SetRenderDrawColor(_renderer, r, g, b, 255);
        SDL.SDL_Rect dstRect = new SDL.SDL_Rect();
        dstRect.x = x;
        dstRect.y = y;
        dstRect.w = width;
        dstRect.h = height;
        SDL.SDL_RenderFillRect(_renderer, ref dstRect);
    }

    public void DrawFrame(int x, int y, int width, int height, int paletteColor)
    {
        var color = _colors[paletteColor];
        SDL.SDL_SetRenderDrawColor(_renderer, color.r, color.g, color.b, 255);
        SDL.SDL_Rect dstRect = new SDL.SDL_Rect();
        dstRect.x = x;
        dstRect.y = y;
        dstRect.w = width;
        dstRect.h = height;
        SDL.SDL_RenderDrawRect(_renderer, ref dstRect);
    }
    
    public void DrawBitmap(IntPtr texture, int x, int y, int width, int height, FlipMode flip = FlipMode.None)
    {
        SDL.SDL_SetTextureScaleMode(texture, SDL.SDL_ScaleMode.SDL_ScaleModeBest);
        SDL.SDL_Rect dstRect = new SDL.SDL_Rect();
        dstRect.x = x;
        dstRect.y = y;
        dstRect.w = width;
        dstRect.h = height;
        switch (flip)
        {
            case FlipMode.None:
                SDL.SDL_RenderCopy(_renderer, texture, IntPtr.Zero, ref dstRect);
                break;
            case FlipMode.Horizontal:
                SDL.SDL_RenderCopyEx(_renderer, texture, IntPtr.Zero, ref dstRect, 0.0, IntPtr.Zero, SDL.SDL_RendererFlip.SDL_FLIP_HORIZONTAL);
                break;
            case FlipMode.Vertical:
                SDL.SDL_RenderCopyEx(_renderer, texture, IntPtr.Zero, ref dstRect, 0.0, IntPtr.Zero, SDL.SDL_RendererFlip.SDL_FLIP_VERTICAL);
                break;
            case FlipMode.Horizontal | FlipMode.Vertical:
                SDL.SDL_RenderCopyEx(_renderer, texture, IntPtr.Zero, ref dstRect, 0.0, IntPtr.Zero, SDL.SDL_RendererFlip.SDL_FLIP_HORIZONTAL | SDL.SDL_RendererFlip.SDL_FLIP_VERTICAL);
                break;
        }
    }

    public void DrawBitmapScaled(IntPtr texture, int x, int y, int width, int height, FlipMode flip = FlipMode.None)
    {
        DrawBitmap(texture, x, y, width * 3 / 2, height * 3 / 2, flip);
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
        SDL.SDL_RenderPresent(_renderer);
    }
}