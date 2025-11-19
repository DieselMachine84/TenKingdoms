namespace TenKingdoms;

public class Colors
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

    public const byte V_BACKGROUND = 0xFF; // background color, pixels of this color are not put in VGAputIcon

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

public class ColorRemap
{
    public byte MainColor { get; private set; }
    public byte[] ColorTable { get; } = new byte[256];

    public static int[] ColorSchemes { get; } = new int[InternalConstants.MAX_COLOR_SCHEME + 1];
    
    public static ColorRemap[] color_remap_array = new ColorRemap[InternalConstants.MAX_COLOR_SCHEME + 1];

    public static byte[,] excitedColorArray = new byte[GameConstants.MAX_NATION + 1, 4];    
    public static void InitRemapTable()
    {
        const int FIRST_REMAP_KEY = 0xE0; // the source color code of the colors to be remapped

        //-------- define color remap scheme -------//

        ColorRemapMethod[] remap_method_array =
        {
            new ColorRemapMethod(0xBC, 0xDC), // the first remap table is for independent units
            new ColorRemapMethod(0xA0, 0xC0), // following are eight remap table for each color code
            new ColorRemapMethod(0xA4, 0xC4),
            new ColorRemapMethod(0xA8, 0xC8),
            new ColorRemapMethod(0xAC, 0xCC),
            new ColorRemapMethod(0xB0, 0xD0),
            new ColorRemapMethod(0xB4, 0xD4),
            new ColorRemapMethod(0xB8, 0xD8),
            new ColorRemapMethod(0xC4, 0x13),
            new ColorRemapMethod(0xC8, 0xA8),
            new ColorRemapMethod(0xCC, 0xAC),
            new ColorRemapMethod(0xBC, 0xDC)
        };

        //---- define the main color code for each color scheme ----//

        byte[] main_color_array = { 0xDC, 0xC0, 0xC4, 0xC8, 0xCC, 0xD0, 0xD4, 0xD8, 0x13, 0xA8, 0xAC };

        //-------- initialize color remap table -------//

        for (int i = 0; i < InternalConstants.MAX_COLOR_SCHEME + 1; i++) // +1 for independent units
        {
            ColorRemap colorRemap = new ColorRemap();
            color_remap_array[i] = colorRemap;
            
            colorRemap.MainColor = main_color_array[i];

            for (int j = 0; j < 256; j++)
                colorRemap.ColorTable[j] = (byte)j;

            for (int j = 0; j < 4; j++)
                colorRemap.ColorTable[FIRST_REMAP_KEY + j] = (byte)(remap_method_array[i].primary_color + j);

            for (int j = 0; j < 4; j++)
                colorRemap.ColorTable[FIRST_REMAP_KEY + 4 + j] = (byte)(remap_method_array[i].secondary_color + j);
        }
        
        for (int i = 0; i <= InternalConstants.MAX_COLOR_SCHEME; i++)
        {
            if (i == 0)
            {
                byte[] remapTable = GetColorRemap(ColorSchemes[i], false).ColorTable;
                excitedColorArray[i, 0] = remapTable[0xe0];
                excitedColorArray[i, 1] = remapTable[0xe1];
                excitedColorArray[i, 2] = remapTable[0xe2];
                excitedColorArray[i, 3] = remapTable[0xe3];
            }
            else
            {
                excitedColorArray[i, 0] = excitedColorArray[i, 1] = excitedColorArray[i, 2] = excitedColorArray[i, 3] = Colors.V_WHITE;
            }
        }
    }
    
    public static ColorRemap GetColorRemap(int scheme, bool isSelected)
    {
        ColorRemap colorRemap = color_remap_array[scheme];

        byte[] colorRemapTable = colorRemap.ColorTable;

        if (isSelected)
        {
            colorRemapTable[Colors.OUTLINE_CODE] = Colors.SELECTED_COLOR;
            colorRemapTable[Colors.OUTLINE_SHADOW_CODE] = Colors.SELECTED_COLOR;
        }
        else
        {
            colorRemapTable[Colors.OUTLINE_CODE] = Colors.TRANSPARENT_CODE;
            colorRemapTable[Colors.OUTLINE_SHADOW_CODE] = Colors.SHADOW_CODE;
        }

        return colorRemap;
    }
    
    public static int GetTextureKey(int nationColor, bool isSelected)
    {
        return nationColor + (isSelected ? GameConstants.MAX_NATION + 1 : 0);
    }
}

public class ColorRemapMethod
{
    public byte primary_color;
    public byte secondary_color;

    public ColorRemapMethod(byte primaryColor, byte secondaryColor)
    {
        primary_color = primaryColor;
        secondary_color = secondaryColor;
    }
}