namespace TenKingdoms;

public static class Colors
{
    public const byte VGA_RED = 0xA0;
    public const byte VGA_LIGHT_BLUE = 0xA4;
    public const byte VGA_DARK_BLUE = 0xC4;
    public const byte VGA_LIGHT_GREEN = 0xA8;
    public const byte VGA_DARK_GREEN = 0xC8;
    public const byte VGA_PURPLE = 0xAC;
    public const byte VGA_ORANGE = 0xD0;
    public const byte VGA_YELLOW = 0xB4;
    public const byte VGA_BROWN = 0xB8;
    public const byte VGA_GRAY = 0x90;

    public const byte V_BLACK = 0x00; // single color only
    public const byte V_WHITE = 0x9F;
    public const byte V_RED = VGA_RED + 1;
    public const byte V_LIGHT_BLUE = VGA_LIGHT_BLUE + 1;
    public const byte V_DARK_BLUE = VGA_DARK_BLUE + 1;
    public const byte V_LIGHT_GREEN = VGA_LIGHT_GREEN + 1;
    public const byte V_DARK_GREEN = VGA_DARK_GREEN + 3;
    public const byte V_PURPLE = VGA_PURPLE + 1;
    public const byte V_ORANGE = VGA_ORANGE;
    public const byte V_YELLOW = VGA_YELLOW;
    public const byte V_BROWN = VGA_BROWN + 2;

    public const byte V_UP = 0x9D;
    public const byte V_BACKGROUND = 0xFF;

    //---------- Define Game Colors --------------//

    public const byte OWN_SELECT_FRAME_COLOR = V_YELLOW;
    public const byte ENEMY_SELECT_FRAME_COLOR = V_RED;

    public const byte UNEXPLORED_COLOR = V_BLACK;

    public const byte SITE_COLOR = V_BLACK;
    public const byte WATER_COLOR = 0x32;
    public const byte WALL_COLOR = VGA_GRAY + 6;
    public const byte HILL_COLOR = VGA_BROWN + 3;
    public const byte FIRE_COLOR = V_RED;
    public const byte TORNADO_COLOR = VGA_GRAY + 8;

    public const byte INDEPENDENT_NATION_COLOR = V_WHITE;

    public const int TRANSPARENT_CODE = 0xff;
    public const int MIN_TRANSPARENT_CODE = 0xf8;
    public const int MAX_TRANSPARENT_CODE = 0xff;
    public const int MANY_TRANSPARENT_CODE = 0xf8;
    public const int SHADOW_CODE = 0x00;
    public const int OUTLINE_CODE = 0xf2;
    public const int OUTLINE_SHADOW_CODE = 0xf3;
    public const byte SELECTED_COLOR = 0xEF;
}

public class ColorRemapMethod
{
    public byte PrimaryColor { get; }
    public byte SecondaryColor { get; }

    public ColorRemapMethod(byte primaryColor, byte secondaryColor)
    {
        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor;
    }
}
public class ColorRemap
{
    public byte MainColor { get; private set; }
    public byte[] ColorTable { get; } = new byte[256];

    public static int[] ColorSchemes { get; } = new int[InternalConstants.MAX_COLOR_SCHEME + 1];
    
    public static ColorRemap[] ColorRemaps { get; } = new ColorRemap[InternalConstants.MAX_COLOR_SCHEME + 1];

    public static void InitRemapTable()
    {
        //-------- define color remap scheme -------//

        ColorRemapMethod[] colorRemapPairs =
        {
            new ColorRemapMethod(0xBC, 0xDC), // the first remap table is for independent units
            new ColorRemapMethod(0xA0, 0xC0), // following are eight remap table for each color code
            new ColorRemapMethod(0xA4, 0xC4),
            new ColorRemapMethod(0xA8, 0xC8),
            new ColorRemapMethod(0xAC, 0xCC),
            new ColorRemapMethod(0xB0, 0xD0),
            new ColorRemapMethod(0xB4, 0xD4),
            new ColorRemapMethod(0xB8, 0xD8),
            new ColorRemapMethod(0x34, 0x13),
            new ColorRemapMethod(0x0D, 0xA8),
            new ColorRemapMethod(0x8A, 0xAC)
        };

        const int firstRemapKey = 0xE0; // the source color code of the colors to be remapped

        //---- define the main color code for each color scheme ----//

        byte[] mainColors = { 0xDC, 0xC0, 0xC4, 0xC8, 0xCC, 0xD0, 0xD4, 0xD8, 0x13, 0xA8, 0xAC };

        //-------- initialize color remap table -------//

        for (int i = 0; i < InternalConstants.MAX_COLOR_SCHEME + 1; i++) // +1 for independent units
        {
            ColorRemap colorRemap = new ColorRemap();
            ColorRemaps[i] = colorRemap;
            
            colorRemap.MainColor = mainColors[i];

            for (int j = 0; j < 256; j++)
                colorRemap.ColorTable[j] = (byte)j;

            if (i <= 7)
            {
                for (int j = 0; j < 4; j++)
                    colorRemap.ColorTable[firstRemapKey + j] = (byte)(colorRemapPairs[i].PrimaryColor + j);

                for (int j = 0; j < 4; j++)
                    colorRemap.ColorTable[firstRemapKey + 4 + j] = (byte)(colorRemapPairs[i].SecondaryColor + j);
            }

            if (i == 8)
            {
                colorRemap.ColorTable[firstRemapKey + 0] = 0x34;
                colorRemap.ColorTable[firstRemapKey + 1] = 0x33;
                colorRemap.ColorTable[firstRemapKey + 2] = 0x32;
                colorRemap.ColorTable[firstRemapKey + 3] = 0x31;
                colorRemap.ColorTable[firstRemapKey + 4] = 0x13;
                colorRemap.ColorTable[firstRemapKey + 5] = 0x32;
                colorRemap.ColorTable[firstRemapKey + 6] = 0x31;
                colorRemap.ColorTable[firstRemapKey + 7] = 0x30;
            }

            if (i == 9)
            {
                colorRemap.ColorTable[firstRemapKey + 0] = 0x0D;
                colorRemap.ColorTable[firstRemapKey + 1] = 0x0C;
                colorRemap.ColorTable[firstRemapKey + 2] = 0x0B;
                colorRemap.ColorTable[firstRemapKey + 3] = 0x0A;
                colorRemap.ColorTable[firstRemapKey + 4] = 0xA8;
                colorRemap.ColorTable[firstRemapKey + 5] = 0xA9;
                colorRemap.ColorTable[firstRemapKey + 6] = 0xAA;
                colorRemap.ColorTable[firstRemapKey + 7] = 0xAB;
            }

            if (i == 10)
            {
                colorRemap.ColorTable[firstRemapKey + 0] = 0x8A;
                colorRemap.ColorTable[firstRemapKey + 1] = 0x89;
                colorRemap.ColorTable[firstRemapKey + 2] = 0x87;
                colorRemap.ColorTable[firstRemapKey + 3] = 0xAC;
                colorRemap.ColorTable[firstRemapKey + 4] = 0xAC;
                colorRemap.ColorTable[firstRemapKey + 5] = 0xAD;
                colorRemap.ColorTable[firstRemapKey + 6] = 0xAE;
                colorRemap.ColorTable[firstRemapKey + 7] = 0xAF;
            }
        }
    }
    
    public static ColorRemap GetColorRemap(int scheme, bool isSelected)
    {
        ColorRemap colorRemap = ColorRemaps[scheme];

        if (isSelected)
        {
            colorRemap.ColorTable[Colors.OUTLINE_CODE] = Colors.SELECTED_COLOR;
            colorRemap.ColorTable[Colors.OUTLINE_SHADOW_CODE] = Colors.SELECTED_COLOR;
        }
        else
        {
            colorRemap.ColorTable[Colors.OUTLINE_CODE] = Colors.TRANSPARENT_CODE;
            colorRemap.ColorTable[Colors.OUTLINE_SHADOW_CODE] = Colors.SHADOW_CODE;
        }

        return colorRemap;
    }

    public static byte[] GetExcitedColors(int scheme)
    {
        byte[] excitedColors = new byte[4];
        byte[] remapTable = GetColorRemap(ColorSchemes[scheme], false).ColorTable;
        excitedColors[0] = remapTable[0xe0];
        excitedColors[1] = remapTable[0xe1];
        excitedColors[2] = remapTable[0xe2];
        excitedColors[3] = remapTable[0xe3];
        return excitedColors;
    }
    
    public static int GetTextureKey(int nationColor, bool isSelected)
    {
        return nationColor + (isSelected ? GameConstants.MAX_NATION + 1 : 0);
    }
}
