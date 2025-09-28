using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class Misc
{
    public const int MOVE_AROUND_TABLE_SIZE = 900;

    private static int[] move_around_table_x = new int[MOVE_AROUND_TABLE_SIZE];
    private static int[] move_around_table_y = new int[MOVE_AROUND_TABLE_SIZE];
    private static int move_around_table_size;

    public static bool freeze_seed;
    private static int random_seed;
    private static Random InternalRandom = new Random();
    private static long StartTicks;

    private static World World => Sys.Instance.World;
    
    static Misc()
    {
        StartTicks = DateTime.Now.Ticks;
        construct_move_around_table();
    }

    public static void set_random_seed(int randomSeed)
    {
        random_seed = randomSeed;
    }

    public static int get_random_seed()
    {
        return random_seed;
    }

    /*public int rand()
    {
        const int MULTIPLIER = 0x015a4e35;
        const int INCREMENT = 1;
        const int RANDOM_MAX = 0x7FFF;

        random_seed = MULTIPLIER * random_seed + INCREMENT;

        return (random_seed >> 16) & RANDOM_MAX;
    }*/

    /*public int random(int maxNum)
    {
        //err_if( maxNum < 0 || maxNum > 0x7FFF )
        //err_now( "Misc::random()" );

        // ###### begin Gilbert 19/6 ######//
        //err_when( is_seed_locked() );
        // ###### end Gilbert 19/6 ######//
        const int MULTIPLIER = 0x015a4e35;
        const int INCREMENT = 1;
        const int RANDOM_MAX = 0x7FFF;

        random_seed = MULTIPLIER * random_seed + INCREMENT;

        return maxNum * ((random_seed >> 16) & RANDOM_MAX) / (RANDOM_MAX + 1);
    }*/

    public static int Random()
    {
        return InternalRandom.Next(0x7FFF);
    }

    public static int Random(int maxNum)
    {
        return InternalRandom.Next(maxNum);
    }

    public static int GetTime()
    {
        return (int)((DateTime.Now.Ticks - StartTicks) / TimeSpan.TicksPerMillisecond);
    }

    public static bool IsLocationValid(int locX, int locY)
    {
        return (locX >= 0 && locX < GameConstants.MapSize && locY >= 0 && locY < GameConstants.MapSize);
    }

    public static void BoundLocation(ref int locX, ref int locY)
    {
        locX = Math.Max(locX, 0);
        locY = Math.Max(locY, 0);
        locX = Math.Min(locX, GameConstants.MapSize - 1);
        locY = Math.Min(locY, GameConstants.MapSize - 1);
    }

    public static IEnumerable<Location> EnumerateNearLocations(int locX1, int locY1, int locX2, int locY2)
    {
        for (int locX = locX1 - 1; locX <= locX2 + 1; locX++)
        {
            if (IsLocationValid(locX, locY1 - 1))
                yield return World.GetLoc(locX, locY1 - 1);
            if (IsLocationValid(locX, locY2 + 1))
                yield return World.GetLoc(locX, locY2 + 1);
        }

        for (int locY = locY1 + 1; locY <= locY2 - 1; locY++)
        {
            if (IsLocationValid(locX1 - 1, locY))
                yield return World.GetLoc(locX1 - 1, locY);
            if (IsLocationValid(locX2 + 1, locY))
                yield return World.GetLoc(locX2 + 1, locY);
        }
    }

    public static int points_distance(int x1, int y1, int x2, int y2)
    {
        int x = Math.Abs(x1 - x2);
        int y = Math.Abs(y1 - y2);

        return Math.Max(x, y);
    }

    public static int RectsDistance(int obj1LocX1, int obj1LocY1, int obj1LocX2, int obj1LocY2,
        int obj2LocX1, int obj2LocY1, int obj2LocX2, int obj2LocY2)
    {
        int diffX = Math.Abs(obj1LocX1 - obj2LocX1);
        diffX = Math.Min(diffX, Math.Abs(obj1LocX1 - obj2LocX2));
        diffX = Math.Min(diffX, Math.Abs(obj1LocX2 - obj2LocX1));
        diffX = Math.Min(diffX, Math.Abs(obj1LocX2 - obj2LocX2));
        int diffY = Math.Abs(obj1LocY1 - obj2LocY1);
        diffY = Math.Min(diffY, Math.Abs(obj1LocY1 - obj2LocY2));
        diffY = Math.Min(diffY, Math.Abs(obj1LocY2 - obj2LocY1));
        diffY = Math.Min(diffY, Math.Abs(obj1LocY2 - obj2LocY2));

        return Math.Max(diffX, diffY);
    }
    
    public static bool AreTownAndFirmLinked(Town town, Firm firm)
    {
        return AreTownAndFirmLinked(town.LocX1, town.LocY1, town.LocX2, town.LocY2, firm.LocX1, firm.LocY1, firm.LocX2, firm.LocY2);
    }

    public static bool AreTownAndFirmLinked(int townLocX1, int townLocY1, int townLocX2, int townLocY2,
        int firmLocX1, int firmLocY1, int firmLocX2, int firmLocY2)
    {
        Location location1 = World.GetLoc(townLocX1, townLocY1);
        Location location2 = World.GetLoc(firmLocX1, firmLocY1);
        if (location1.RegionId != location2.RegionId || location1.IsPlateau() != location2.IsPlateau())
            return false;
        
        return RectsDistance(townLocX1, townLocY1, townLocX2, townLocY2,
            firmLocX1, firmLocY1, firmLocX2, firmLocY2) <= InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE;
    }

    public static bool AreFirmsLinked(Firm firm1, Firm firm2)
    {
        return AreFirmsLinked(firm1.LocX1, firm1.LocY1, firm1.LocX2, firm2.LocY2, firm2.LocX1, firm2.LocY1, firm2.LocX2, firm2.LocY2);
    }

    public static bool AreFirmsLinked(int firm1LocX1, int firm1LocY1, int firm1LocX2, int firm1LocY2,
        int firm2LocX1, int firm2LocY1, int firm2LocX2, int firm2LocY2)
    {
        Location location1 = World.GetLoc(firm1LocX1, firm1LocY1);
        Location location2 = World.GetLoc(firm2LocX1, firm2LocY1);
        if (location1.RegionId != location2.RegionId || location1.IsPlateau() != location2.IsPlateau())
            return false;

        return RectsDistance(firm1LocX1, firm1LocY1, firm1LocX2, firm1LocY2,
            firm2LocX1, firm2LocY1, firm2LocX2, firm2LocY2) <= InternalConstants.EFFECTIVE_FIRM_FIRM_DISTANCE;
    }

    public static bool AreTownsLinked(Town town1, Town town2)
    {
        return AreTownsLinked(town1.LocX1, town1.LocY1, town1.LocX2, town1.LocY2, town2.LocX1, town2.LocY1, town2.LocX2, town2.LocY2);
    }

    public static bool AreTownsLinked(int town1LocX1, int town1LocY1, int town1LocX2, int town1LocY2,
        int town2LocX1, int town2LocY1, int town2LocX2, int town2LocY2)
    {
        Location location1 = World.GetLoc(town1LocX1, town1LocY1);
        Location location2 = World.GetLoc(town2LocX1, town2LocY1);
        if (location1.RegionId != location2.RegionId || location1.IsPlateau() != location2.IsPlateau())
            return false;
        
        return RectsDistance(town1LocX1, town1LocY1, town1LocX2, town1LocY2,
            town2LocX1, town2LocY1, town2LocX2, town2LocY2) <= InternalConstants.EFFECTIVE_TOWN_TOWN_DISTANCE;
    }

    public static void cal_move_around_a_point(int num, int width, int height, out int xShift, out int yShift)
    {
        int maxSqtSize = (width > height) ? height + 1 : width + 1;
        //short num2 = num%(maxSqtSize*maxSqtSize) + 1;
        int num2 = (num - 1) % (maxSqtSize * maxSqtSize) + 1;

        if (num2 <= 1)
        {
            xShift = yShift = 0;
            return;
        }

        if (num2 <= MOVE_AROUND_TABLE_SIZE)
        {
            xShift = move_around_table_x[num2 - 1];
            yShift = move_around_table_y[num2 - 1];
        }
        else
        {
            cal_move_around_a_point_v2(num, width, height, out xShift, out yShift);
        }
    }

    public static void cal_move_around_a_point_v2(int num, int width, int height, out int xShift, out int yShift)
    {
        int maxSqtSize = (width > height) ? height + 1 : width + 1;
        //short num2 = num%(maxSqtSize*maxSqtSize) + 1;
        int num2 = (num - 1) % (maxSqtSize * maxSqtSize) + 1;

        if (num2 <= 1)
        {
            xShift = yShift = 0;
            return;
        }

        int sqtCount = 1;

        //TODO Strange constant
        while (sqtCount < GameConstants.MapSize + 10) // the MAX. size of the map is 200x200
        {
            if (num2 <= sqtCount * sqtCount)
                break;
            else
                sqtCount += 2;
        }

        int filter = (sqtCount - 1) / 2; // is an integer
        int refNum = num2 - (sqtCount - 2) * (sqtCount - 2);

        //=====================================//
        // some adjustment to the refNum can
        // generate different mode of result
        //=====================================//
        // note: sqtCount>=3 for this mode
        refNum = (refNum - 1 - (sqtCount - 3) / 2) % (4 * (sqtCount - 1)) + 1;

        //-------------------------------------------------//
        // determine xMag
        //-------------------------------------------------//
        int xMag = 0;
        if (refNum < sqtCount)
            xMag = refNum - 1;
        else
        {
            if (refNum >= sqtCount && refNum <= 3 * (sqtCount - 1))
                xMag = (sqtCount << 1) - 1 - refNum; //(sqtCount-1) - (refNum-sqtCount);
            else if (refNum >= sqtCount + 2 * (sqtCount - 1))
                xMag = refNum + 3 - (sqtCount << 2); //(refNum-sqtCount-2*(sqtCount-1)) - (sqtCount-1);
            //else
            //err_here();
        }

        //-------------------------------------------------//
        // calculate xShift
        //-------------------------------------------------//
        if (xMag > 0) // +ve
            xShift = (xMag > filter) ? filter : xMag;
        else // -ve
            xShift = (-xMag > filter) ? -filter : xMag;

        //-------------------------------------------------//
        // calculate yShift
        //-------------------------------------------------//
        //ySign = (refNum>sqtCount && refNum<=3*sqtCount-3) ? -1 : 1;
        int yMag = (sqtCount - 1) - Math.Abs(xMag); // abs(xMag) + abs(yMag) always = (sqtCount-1)
        if (refNum > sqtCount && refNum <= 3 * sqtCount - 3) // -ve
            yShift = (yMag > filter) ? -filter : -yMag;
        else // +ve
            yShift = (yMag > filter) ? filter : yMag;
    }

    private static void construct_move_around_table()
    {
        if (move_around_table_size == MOVE_AROUND_TABLE_SIZE)
            return; // table already created

        for (int i = 0; i < MOVE_AROUND_TABLE_SIZE; ++i)
        {
            int xShift, yShift;
            cal_move_around_a_point_v2(i, MOVE_AROUND_TABLE_SIZE, MOVE_AROUND_TABLE_SIZE,
                out xShift, out yShift);
            move_around_table_x[i] = xShift;
            move_around_table_y[i] = yShift;
        }

        move_around_table_size = MOVE_AROUND_TABLE_SIZE;
    }

    public static string roman_number(int inNum)
    {
        string[] roman_number_array = new string[] { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };

        string str = string.Empty;

        if (inNum > 100)
        {
            str += roman_number_array[inNum / 100 - 1];
            inNum = inNum - inNum / 100 * 100;
        }

        if (inNum > 10)
        {
            str += roman_number_array[(inNum - 1) / 10 - 1];
            inNum = inNum - (inNum - 1) / 10 * 10;
        }

        str += roman_number_array[inNum - 1];

        return str;
    }

    public static int ToInt32(char[] chars)
    {
        string str = ToString(chars);
        if (string.IsNullOrEmpty(str))
            return 0;
        return Convert.ToInt32(str);
    }

    public static string ToString(char[] chars)
    {
        string result = string.Empty;
        foreach (var c in chars)
        {
            if (c != '\0' && !Char.IsWhiteSpace(c))
            {
                result += c;
            }
        }

        return result;
    }
}
