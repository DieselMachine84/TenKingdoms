using System;

namespace TenKingdoms;

public partial class Renderer
{
	private const byte ColorUp = 0x9D;
	private const byte ColorDown = 0x9C;

	private void DrawBar(int x1, int y1, int x2, int y2, byte color)
	{
		Graphics.DrawRect(x1, y1, x2 - x1, y2 - y1, color);
	}

	private void DrawPanel(int x1, int y1, int x2, int y2, bool paintCenter = true)
	{
		if (paintCenter)
		{
			DrawBar(x1 + 2, y1 + 2, x2 - 3, y2 - 3, ColorUp);
		}

		//--------- white border on top and left sides -----------//

		DrawBar(x1, y1, x2 - 3, y1 + 1, 0x9a);
		Graphics.DrawPoint(x2 - 2, y1, 0x9a);
		DrawBar(x1, y1 + 2, x1 + 1, y2 - 3, 0x9a); // left side
		Graphics.DrawPoint(x1, y2 - 2, 0x9a);

		//--------- black border on bottom and right sides -----------//

		DrawBar(x2 - 2, y1 + 2, x2 - 1, y2 - 1, 0x90); // bottom side
		Graphics.DrawPoint(x2 - 1, y1 + 1, 0x90);
		DrawBar(x1 + 2, y2 - 2, x2 - 3, y2 - 1, 0x90); // right side
		Graphics.DrawPoint(x1 + 1, y2 - 1, 0x90);

		//--------- junction between white and black border --------//
		Graphics.DrawPoint(x2 - 1, y1, 0x97);
		Graphics.DrawPoint(x2 - 2, y1 + 1, 0x97);
		Graphics.DrawPoint(x1, y2 - 1, 0x97);
		Graphics.DrawPoint(x1 + 1, y2 - 2, 0x97);

		//--------- gray shadow on bottom and right sides -----------//
		DrawBar(x2, y1 + 1, x2, y2, 0x97);
		DrawBar(x1 + 1, y2, x2 - 1, y2, 0x97);
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