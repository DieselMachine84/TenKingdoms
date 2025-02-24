using System;
using System.Collections.Generic;
using System.Linq;

namespace TenKingdoms;

public partial class Renderer
{
	private readonly Dictionary<int, IntPtr> _colorSquareTextures = new Dictionary<int, nint>();
	private int _colorSquareWidth;
	private int _colorSquareHeight;
	private IntPtr _gameMenuTexture1;
	private int _gameMenuTexture1Width;
	private int _gameMenuTexture1Height;
	private IntPtr _gameMenuTexture2;
	private int _gameMenuTexture2Width;
	private int _gameMenuTexture2Height;
	private IntPtr _gameMenuTexture3;
	private int _gameMenuTexture3Width;
	private int _gameMenuTexture3Height;
	
	private IntPtr _detailsTexture1;
	private int _detailsTexture1Width;
	private int _detailsTexture1Height;
	private IntPtr _detailsTexture2;
	private int _detailsTexture2Width;
	private int _detailsTexture2Height;
	private IntPtr _detailsTexture3;
	private int _detailsTexture3Width;
	private int _detailsTexture3Height;
	private IntPtr _detailsTexture4;
	private int _detailsTexture4Width;
	private int _detailsTexture4Height;

	private IntPtr _middleBorder1Texture;
	private int _middleBorder1TextureWidth;
	private int _middleBorder1TextureHeight;
	private IntPtr _middleBorder2Texture;
	private int _middleBorder2TextureWidth;
	private int _middleBorder2TextureHeight;
	private IntPtr _rightBorder1Texture;
	private int _rightBorder1TextureWidth;
	private int _rightBorder1TextureHeight;
	private IntPtr _rightBorder2Texture;
	private int _rightBorder2TextureWidth;
	private int _rightBorder2TextureHeight;
	private IntPtr _miniMapBorder1Texture;
	private int _miniMapBorder1TextureWidth;
	private int _miniMapBorder1TextureHeight;
	private IntPtr _miniMapBorder2Texture;
	private int _miniMapBorder2TextureWidth;
	private int _miniMapBorder2TextureHeight;
	private IntPtr _bottomBorder1Texture;
	private int _bottomBorder1TextureWidth;
	private int _bottomBorder1TextureHeight;
	private IntPtr _bottomBorder2Texture;
	private int _bottomBorder2TextureWidth;
	private int _bottomBorder2TextureHeight;

	private IntPtr _smallPanelTexture;
	private int _smallPanelTextureWidth;
	private int _smallPanelTextureHeight;

	private void CreateUITextures()
	{
        ResourceIdx imageButtons = new ResourceIdx($"{Sys.GameDataFolder}/Resource/I_BUTTON.RES");
        byte[] colorSquare = imageButtons.Read("V_COLCOD");
        _colorSquareWidth = BitConverter.ToInt16(colorSquare, 0);
        _colorSquareHeight = BitConverter.ToInt16(colorSquare, 2);
        byte[] colorSquareBitmap = colorSquare.Skip(4).ToArray();
        for (int i = 0; i <= InternalConstants.MAX_COLOR_SCHEME; i++)
        {
            int textureKey = ColorRemap.GetTextureKey(i, false);
            byte[] decompressedBitmap = Graphics.DecompressTransparentBitmap(colorSquareBitmap, _colorSquareWidth, _colorSquareHeight,
                ColorRemap.GetColorRemap(i, false).ColorTable);
            _colorSquareTextures.Add(textureKey, Graphics.CreateTextureFromBmp(decompressedBitmap, _colorSquareWidth, _colorSquareHeight));
        }

        ResourceIdx interfaceImages = new ResourceIdx($"{Sys.GameDataFolder}/Resource/I_IF.RES");
        byte[] mainScreenBitmap = interfaceImages.Read("MAINSCR");
        int mainScreenWidth = BitConverter.ToInt16(mainScreenBitmap, 0);
        int mainScreenHeight = BitConverter.ToInt16(mainScreenBitmap, 2);
        mainScreenBitmap = mainScreenBitmap.Skip(4).ToArray();

        _gameMenuTexture1Width = 306;
        _gameMenuTexture1Height = 56;
        byte[] gameMenu1Bitmap = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
	        0, 0, _gameMenuTexture1Width, _gameMenuTexture1Height);
        _gameMenuTexture1 = Graphics.CreateTextureFromBmp(gameMenu1Bitmap, _gameMenuTexture1Width, _gameMenuTexture1Height);
        _gameMenuTexture2Width = 270;
        _gameMenuTexture2Height = 56;
        byte[] gameMenu2Bitmap = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
	        _gameMenuTexture1Width, 0, _gameMenuTexture2Width, _gameMenuTexture2Height);
        _gameMenuTexture2 = Graphics.CreateTextureFromBmp(gameMenu2Bitmap, _gameMenuTexture2Width, _gameMenuTexture2Height);
        _gameMenuTexture3Width = 208;
        _gameMenuTexture3Height = 56;
        byte[] gameMenu3Bitmap = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
	        mainScreenWidth - 8 - _gameMenuTexture3Width, 0, _gameMenuTexture3Width, _gameMenuTexture3Height);
        _gameMenuTexture3 = Graphics.CreateTextureFromBmp(gameMenu3Bitmap, _gameMenuTexture3Width, _gameMenuTexture3Height);

        _middleBorder1TextureWidth = 12;
        _middleBorder1TextureHeight = 56;
        byte[] middleBorder1Bitmap = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
	        576, 0, _middleBorder1TextureWidth, _middleBorder1TextureHeight);
        _middleBorder1Texture = Graphics.CreateTextureFromBmp(middleBorder1Bitmap, _middleBorder1TextureWidth, _middleBorder1TextureHeight);
        _middleBorder2TextureWidth = 12;
        _middleBorder2TextureHeight = 200;
        byte[] middleBorder2Bitmap = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
	        576, _middleBorder1TextureHeight, _middleBorder2TextureWidth, _middleBorder2TextureHeight);
        _middleBorder2Texture = Graphics.CreateTextureFromBmp(middleBorder2Bitmap, _middleBorder2TextureWidth, _middleBorder2TextureHeight);

        _rightBorder1TextureWidth = 12;
        _rightBorder1TextureHeight = 56;
        byte[] rightBorder1Bitmap = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
	        mainScreenWidth - _rightBorder1TextureWidth, 0, _rightBorder1TextureWidth, _rightBorder1TextureHeight);
        _rightBorder1Texture = Graphics.CreateTextureFromBmp(rightBorder1Bitmap, _rightBorder1TextureWidth, _rightBorder1TextureHeight);
        _rightBorder2TextureWidth = 12;
        _rightBorder2TextureHeight = 200;
        byte[] rightBorder2Bitmap = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
	        mainScreenWidth - _rightBorder1TextureWidth, 264, _rightBorder2TextureWidth, _rightBorder2TextureHeight);
        _rightBorder2Texture = Graphics.CreateTextureFromBmp(rightBorder2Bitmap, _rightBorder2TextureWidth, _rightBorder2TextureHeight);

        _miniMapBorder1TextureWidth = 146;
        _miniMapBorder1TextureHeight = 8;
        byte[] miniMapBorder1Bitmap = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
	        576, 256, _miniMapBorder1TextureWidth, _miniMapBorder1TextureHeight);
        _miniMapBorder1Texture = Graphics.CreateTextureFromBmp(miniMapBorder1Bitmap, _miniMapBorder1TextureWidth, _miniMapBorder1TextureHeight);
        _miniMapBorder2TextureWidth = 146;
        _miniMapBorder2TextureHeight = 8;
        byte[] miniMapBorder2Bitmap = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
	        mainScreenWidth - _miniMapBorder1TextureWidth, 256, _miniMapBorder2TextureWidth, _miniMapBorder2TextureHeight);
        _miniMapBorder2Texture = Graphics.CreateTextureFromBmp(miniMapBorder2Bitmap, _miniMapBorder2TextureWidth, _miniMapBorder2TextureHeight);

        _bottomBorder1TextureWidth = 146;
        _bottomBorder1TextureHeight = 8;
        byte[] bottomBorder1Bitmap = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
	        576, mainScreenHeight - _bottomBorder1TextureHeight, _bottomBorder1TextureWidth, _bottomBorder1TextureHeight);
        _bottomBorder1Texture = Graphics.CreateTextureFromBmp(bottomBorder1Bitmap, _bottomBorder1TextureWidth, _bottomBorder1TextureHeight);
        _bottomBorder2TextureWidth = 146;
        _bottomBorder2TextureHeight = 8;
        byte[] bottomBorder2Bitmap = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
	        mainScreenWidth - _bottomBorder1TextureWidth, mainScreenHeight - _bottomBorder2TextureHeight, _bottomBorder2TextureWidth, _bottomBorder2TextureHeight);
        _bottomBorder2Texture = Graphics.CreateTextureFromBmp(bottomBorder2Bitmap, _bottomBorder2TextureWidth, _bottomBorder2TextureHeight);

        _detailsTexture1Width = 208;
        _detailsTexture1Height = 208;
        byte[] detailsBitmap1 = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
            584, 264, _detailsTexture1Width, _detailsTexture1Height);
        _detailsTexture1 = Graphics.CreateTextureFromBmp(detailsBitmap1, _detailsTexture1Width, _detailsTexture1Height);
        _detailsTexture2Width = 68;
        _detailsTexture2Height = 208;
        byte[] detailsBitmap2 = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
            584 + _detailsTexture1Width - _detailsTexture2Width, 264, _detailsTexture2Width, _detailsTexture2Height);
        _detailsTexture2 = Graphics.CreateTextureFromBmp(detailsBitmap2, _detailsTexture2Width, _detailsTexture2Height);
        _detailsTexture3Width = 208;
        _detailsTexture3Height = 120;
        byte[] detailsBitmap3 = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
            584, 264 + _detailsTexture1Height, _detailsTexture3Width, _detailsTexture3Height);
        _detailsTexture3 = Graphics.CreateTextureFromBmp(detailsBitmap3, _detailsTexture3Width, _detailsTexture3Height);
        _detailsTexture4Width = 68;
        _detailsTexture4Height = 120;
        byte[] detailsBitmap4 = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
            584 + _detailsTexture1Width - _detailsTexture2Width, 264 + _detailsTexture2Height, _detailsTexture4Width, _detailsTexture4Height);
        _detailsTexture4 = Graphics.CreateTextureFromBmp(detailsBitmap4, _detailsTexture4Width, _detailsTexture4Height);
        
        CreateSmallPanel(detailsBitmap1, detailsBitmap2);
	}

	private void CreateSmallPanel(byte[] detailsBitmap1, byte[] detailsBitmap2)
	{
		_smallPanelTextureWidth = (DetailsWidth - 4) / 3 * 2;
		_smallPanelTextureHeight = 30;
        byte[] smallPanelBitmap = new byte[_smallPanelTextureWidth * _smallPanelTextureHeight * 4];
        int index = 0;
        for (int h = 0; h < _smallPanelTextureHeight; h++)
        {
            for (int w = 0; w < _smallPanelTextureWidth; w++)
            {
                byte paletteColor = w < 208 ? detailsBitmap1[h * 208 + w] : detailsBitmap2[h * 68 + w - 208];
                System.Drawing.Color color = Sys.Instance.PaletteColors[paletteColor];
                smallPanelBitmap[index] = (byte)(color.B + (255 - color.B) * 6 / 10);
                smallPanelBitmap[index + 1] = (byte)(color.G + (255 - color.G) * 6 / 10);
                smallPanelBitmap[index + 2] = (byte)(color.R + (255 - color.R) * 6 / 10);
                smallPanelBitmap[index + 3] = 0;
                index += 4;
            }
        }
        for (int w = 0; w < _smallPanelTextureWidth; w++)
        {
            index = w * 4;
            smallPanelBitmap[index] = smallPanelBitmap[index + 1] = smallPanelBitmap[index + 2] = 255;
            index = _smallPanelTextureWidth * 4 + w * 4;
            smallPanelBitmap[index] = smallPanelBitmap[index + 1] = smallPanelBitmap[index + 2] = 255;
            index = (_smallPanelTextureHeight - 2) * _smallPanelTextureWidth * 4 + w * 4;
            smallPanelBitmap[index] = smallPanelBitmap[index + 1] = smallPanelBitmap[index + 2] = 0;
            index = (_smallPanelTextureHeight - 1) * _smallPanelTextureWidth * 4 + w * 4;
            smallPanelBitmap[index] = smallPanelBitmap[index + 1] = smallPanelBitmap[index + 2] = 0;
        }
        for (int h = 2; h < _smallPanelTextureHeight; h++)
        {
            index = h * _smallPanelTextureWidth * 4;
            smallPanelBitmap[index] = smallPanelBitmap[index + 1] = smallPanelBitmap[index + 2] = 255;
            index = h * _smallPanelTextureWidth * 4 + 4;
            smallPanelBitmap[index] = smallPanelBitmap[index + 1] = smallPanelBitmap[index + 2] = 255;
            index = h * _smallPanelTextureWidth * 4 + (_smallPanelTextureWidth - 2) * 4;
            smallPanelBitmap[index] = smallPanelBitmap[index + 1] = smallPanelBitmap[index + 2] = 0;
            index = h * _smallPanelTextureWidth * 4 + (_smallPanelTextureWidth - 1) * 4;
            smallPanelBitmap[index] = smallPanelBitmap[index + 1] = smallPanelBitmap[index + 2] = 0;
        }
        _smallPanelTexture = Graphics.CreateTextureFromBmp(smallPanelBitmap, _smallPanelTextureWidth, _smallPanelTextureHeight, 32);
	}

	private void DrawSmallPanel(int x, int y)
	{
		Graphics.DrawBitmap(_smallPanelTexture, x, y, Scale(_smallPanelTextureWidth), Scale(_smallPanelTextureHeight));
	}

	private void PutTextCenter(Font font, string text, int x1, int y1, int x2, int y2, bool clearBack = false)
	{
		int tx = x1 + ((x2 - x1 + 1) - font.TextWidth(text)) / 2;
		int ty = y1 + ((y2 - y1 + 1) - font.FontHeight) / 2;

		if (tx < 0)
			tx = 0;

		if (clearBack && tx > x1)
		{
			//TODO
		}

		PutText(font, text, tx, ty, x2, clearBack);
	}

	private void PutText(Font font, string text, int x, int y, int x2, bool clearBack)
	{
		if (x2 < 0) // default
			x2 = x + font.MaxFontWidth * text.Length;

		x2 = Math.Min(x2, WindowWidth - 1);

		int y2 = y + font.FontHeight - 1;

		//-------------------------------------//

		for (int i = 0; i < text.Length; i++)
		{
			char textChar = text[i];

			//--------------- space character ------------------//

			if (textChar == ' ')
			{
				if (x + font.SpaceWidth > x2)
					break;

				if (clearBack)
				{
					//TODO
				}

				x += font.SpaceWidth;
			}

			// --------- control word: @COL# (nation color) -----------//

			else
			{
				int colLength = "@COL".Length;
				bool hasColorCode = (textChar == '@') && (i + colLength <= text.Length && text.Substring(i, colLength) == "@COL");
				if (hasColorCode) // display nation color bar in text
				{
					if (x2 >= 0 && x + Font.NATION_COLOR_BAR_WIDTH - 1 > x2) // exceed right border x2
						break;

					// get nation color and skip over the word
					i += colLength;
					textChar = text[i];

					byte colorCode = ColorRemap.color_remap_array[textChar - '0'].MainColor;

					//TODO
					//NationArray.disp_nation_color(x, y + 2, colorCode);

					x += Font.NATION_COLOR_BAR_WIDTH;
				}

				//------------- normal character ----------------//

				else if (textChar >= font.FirstChar && textChar <= font.LastChar)
				{
					FontInfo fontInfo = font[textChar - font.FirstChar];

					if (x + fontInfo.width > x2)
						break;

					if (fontInfo.width > 0)
					{
						if (clearBack)
						{
							//TODO
						}
						else
						{
							Graphics.DrawBitmap(fontInfo.GetTexture(Graphics, font.FontBitmap), x, y + fontInfo.offset_y,
								fontInfo.width, fontInfo.height);
						}

						x += fontInfo.width; // inter-character space
					}
				}
				else
				{
					//------ tab or unknown character -------//

					if (textChar == '\t') // Tab
						x += font.SpaceWidth * 8; // one tab = 8 space chars
					else
						x += font.SpaceWidth;
				}
			}

			//--------- inter-character space ---------//

			if (clearBack) // copy texture from the back buffer as the background color
			{
				//TODO
			}

			x += font.InterCharSpace;
		}

		//------ clear remaining area -------//

		if (clearBack) // copy texture from the back buffer as the background color
		{
			//TODO
		}

		//return x-1;
	}
}