using System;
using System.IO;
using System.Linq;

namespace TenKingdoms;

public class FontInfo // info for each character
{
    public sbyte offset_y;
    public byte width;
    public byte height;
    public int bitmap_offset; // file offset relative to bitmap data
    private IntPtr texture;

    public IntPtr GetTexture(Graphics graphics, byte[] bitmaps)
    {
	    if (texture == default)
	    {
		    byte[] bitmap = bitmaps.Skip(bitmap_offset + 2 * sizeof(Int16)).Take(width * height).ToArray();
		    byte[] decompressedBitmap = graphics.DecompressTransparentBitmap(bitmap, width, height);
		    byte[] scaledBitmap = new byte[decompressedBitmap.Length * 4];
		    for (int h = 0; h < height; h++)
		    {
			    for (int w = 0; w < width; w++)
			    {
				    scaledBitmap[h * 2 * width * 2 + w * 2] = bitmap[h * width + w];
				    scaledBitmap[h * 2 * width * 2 + w * 2 + 1] = bitmap[h * width + w];
				    scaledBitmap[(h * 2 + 1) * width * 2 + w * 2] = bitmap[h * width + w];
				    scaledBitmap[(h * 2 + 1) * width * 2 + w * 2 + 1] = bitmap[h * width + w];
			    }
		    }

		    offset_y *= 2;
		    width *= 2;
		    height *= 2;
		    texture = graphics.CreateTextureFromBmp(scaledBitmap, width, height);
	    }

	    return texture;
    }
}

public class Font
{
	public const int NATION_COLOR_BAR_WIDTH = 13;
	public const int NATION_COLOR_BAR_HEIGHT = 13;

	private int _stdFontHeight;
	public int FontHeight { get; } // height of a character
	public int MaxFontWidth { get; } // width of the widest character in the font
	public int MaxFontHeight { get; }

	public int FirstChar { get; } // the starting available character
	public int LastChar { get; } // the ending available character
	public int InterCharSpace { get; }
    public int SpaceWidth { get; }

    private FontInfo[] _fontInfos;
    public byte[] FontBitmap { get; }

    public FontInfo this[int index] => _fontInfos[index];    
    
    public Font(string fontName, int interCharSpace, byte italicShift)
    {
	    InterCharSpace = interCharSpace;

        using FileStream stream = new FileStream($"{Sys.GameDataFolder}/Resource/FNT_{fontName}.RES", FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new BinaryReader(stream);

        MaxFontWidth = reader.ReadUInt16();
        MaxFontHeight = reader.ReadUInt16();
        _stdFontHeight = reader.ReadUInt16();
        FirstChar = reader.ReadUInt16();
        LastChar = reader.ReadUInt16();

        FontHeight = _stdFontHeight; // its default is std_font_height, but can be set to max_font_height

        //----------- read in font info ------------//

        _fontInfos = new FontInfo[LastChar - FirstChar + 1];
        for (int i = 0; i < _fontInfos.Length; i++)
        {
            FontInfo fontInfo = new FontInfo();
            fontInfo.offset_y = reader.ReadSByte();
            fontInfo.width = reader.ReadByte();
            fontInfo.height = reader.ReadByte();
            fontInfo.bitmap_offset = reader.ReadInt32();
            _fontInfos[i] = fontInfo;
        }

        //------- process italic shift --------//

        if (italicShift != 0)
        {
            for (int i = 0; i < LastChar - FirstChar + 1; i++)
                _fontInfos[i].width -= italicShift;
        }

        //---------- read in bitmap data ----------//

        const int bufferSize = 4096;
        using MemoryStream ms = new MemoryStream();
        byte[] tempBuffer = new byte[bufferSize];
        int count = 0;
        while ((count = reader.Read(tempBuffer, 0, tempBuffer.Length)) != 0)
            ms.Write(tempBuffer, 0, count);
        FontBitmap = ms.ToArray();

        //---- get the width of the space character ----//
        //
        // since in some font, the space char is too narrow,
        // we use the width of 't' as space instead
        //
        //----------------------------------------------//

        SpaceWidth = _fontInfos['t' - FirstChar].width;

        MaxFontWidth *= 2;
        MaxFontHeight *= 2;
        _stdFontHeight *= 2;
        FontHeight *= 2;
        InterCharSpace *= 2;
        SpaceWidth *= 2;
    }

    public int TextWidth(string text, int maxDispWidth = 0)
    {
	    int x = 0;
	    int maxLen = 0;
	    int wordWidth = 0;

	    for (int i = 0; i < text.Length; i++, x += InterCharSpace)
	    {
		    char textChar = text[i];

		    //-- if the line exceed the given MAX width, advance to next line --//

		    if (maxDispWidth != 0 && x > maxDispWidth)
		    {
			    maxLen = maxDispWidth;
			    x = wordWidth; // the last word of the prev line wraps to next line
		    }

		    //--- if the text has more than 1 line, get the longest line ---//

		    //TODO better check for the new line
		    if (textChar == '\n')
		    {
			    if (x > maxLen)
				    maxLen = x;

			    x = 0;
			    wordWidth = 0;
			    continue; // next character
		    }

		    // --------- control word: @COL# (nation color) -----------//

		    else
		    {
			    int colLength = "@COL".Length;
			    bool hasColorCode = (textChar == '@') && (i + colLength <= text.Length && text.Substring(i, colLength) == "@COL");
			    if (hasColorCode) // display nation color bar in text
			    {
				    // skip over the word
				    i += colLength;
				    x += NATION_COLOR_BAR_WIDTH;
				    wordWidth = 0;
			    }

			    //--- add the width of the character to the total line width ---//

			    else if (textChar == ' ')
			    {
				    x += SpaceWidth;
				    wordWidth = 0;
			    }

			    else if (textChar >= FirstChar && textChar <= LastChar)
			    {
				    int charWidth = _fontInfos[textChar - FirstChar].width;
				    x += charWidth;
				    wordWidth += charWidth;
			    }
			    else
			    {
				    x += SpaceWidth;
				    wordWidth += SpaceWidth;
			    }
		    }

		    if (maxDispWidth != 0 && wordWidth > maxDispWidth)
		    {
			    x -= wordWidth - maxDispWidth;
			    wordWidth = maxDispWidth;
		    }
	    }

	    return Math.Max(maxLen, x);
    }
}