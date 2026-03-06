using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TenKingdoms;

public static class Misc
{
    private const int MOVE_AROUND_TABLE_SIZE = 900;

    private static readonly int[] MoveAroundTableX = new int[MOVE_AROUND_TABLE_SIZE];
    private static readonly int[] MoveAroundTableY = new int[MOVE_AROUND_TABLE_SIZE];
    private static int moveAroundTableSize;
    private static readonly string[] RomanNumbers = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };

    private const int MULTIPLIER = 0x015a4e35;
    private const int INCREMENT = 1;
    public static uint RandomSeed { get; set; }
    //TODO check save and load
    private static long StartTicks { get; set; }

    private static Config Config => Sys.Instance.Config;
    private static World World => Sys.Instance.World;
    
    static Misc()
    {
        StartTicks = DateTime.Now.Ticks;
        ConstructMoveAroundTable();
    }

    public static int Random(int maxNum)
    {
        if (maxNum == 0)
            return 0;
        
        RandomSeed = unchecked(MULTIPLIER * RandomSeed + INCREMENT);
        return (int)(RandomSeed % maxNum);
    }

    public static int GetTime()
    {
        return (int)((DateTime.Now.Ticks - StartTicks) / TimeSpan.TicksPerMillisecond);
    }

    public static bool IsLocationValid(int locX, int locY)
    {
        return (locX >= 0 && locX < Config.MapSize && locY >= 0 && locY < Config.MapSize);
    }

    public static void BoundLocation(ref int locX, ref int locY)
    {
        locX = Math.Max(locX, 0);
        locY = Math.Max(locY, 0);
        locX = Math.Min(locX, Config.MapSize - 1);
        locY = Math.Min(locY, Config.MapSize - 1);
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

    public static int PointsDistance(int x1, int y1, int x2, int y2)
    {
        return Math.Max(Math.Abs(x1 - x2), Math.Abs(y1 - y2));
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

    public static int FirmsDistance(Firm firm1, Firm firm2)
    {
        return RectsDistance(firm1.LocX1, firm1.LocY1, firm1.LocX2, firm1.LocY2,
            firm2.LocX1, firm2.LocY1, firm2.LocX2, firm2.LocY2);
    }

    public static int FirmTownDistance(Firm firm, Town town)
    {
        return RectsDistance(firm.LocX1, firm.LocY1, firm.LocX2, firm.LocY2,
            town.LocX1, town.LocY1, town.LocX2, town.LocY2);
    }

    public static int TownsDistance(Town town1, Town town2)
    {
        return RectsDistance(town1.LocX1, town1.LocY1, town1.LocX2, town1.LocY2,
            town2.LocX1, town2.LocY1, town2.LocX2, town2.LocY2);
    }
    
    public static bool AreTownAndFirmLinked(Town town, Firm firm)
    {
        return AreTownAndFirmLinked(town.LocX1, town.LocY1, town.LocX2, town.LocY2,
            firm.LocX1, firm.LocY1, firm.LocX2, firm.LocY2);
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
        return AreFirmsLinked(firm1.LocX1, firm1.LocY1, firm1.LocX2, firm1.LocY2, firm1.RegionId,
            firm2.LocX1, firm2.LocY1, firm2.LocX2, firm2.LocY2, firm2.RegionId);
    }

    public static bool AreFirmsLinked(int firm1LocX1, int firm1LocY1, int firm1LocX2, int firm1LocY2, int firm1RegionId,
        int firm2LocX1, int firm2LocY1, int firm2LocX2, int firm2LocY2, int firm2RegionId)
    {
        Location location1 = World.GetLoc(firm1LocX1, firm1LocY1);
        Location location2 = World.GetLoc(firm2LocX1, firm2LocY1);
        if (firm1RegionId != firm2RegionId || location1.IsPlateau() != location2.IsPlateau())
            return false;

        return RectsDistance(firm1LocX1, firm1LocY1, firm1LocX2, firm1LocY2,
            firm2LocX1, firm2LocY1, firm2LocX2, firm2LocY2) <= InternalConstants.EFFECTIVE_FIRM_FIRM_DISTANCE;
    }

    public static bool AreTownsLinked(Town town1, Town town2)
    {
        return AreTownsLinked(town1.LocX1, town1.LocY1, town1.LocX2, town1.LocY2,
            town2.LocX1, town2.LocY1, town2.LocX2, town2.LocY2);
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

    //TODO rewrite
    public static void MoveAroundAPoint(int num, int width, int height, out int xShift, out int yShift)
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
            xShift = MoveAroundTableX[num2 - 1];
            yShift = MoveAroundTableY[num2 - 1];
        }
        else
        {
            MoveAroundAPointV2(num, width, height, out xShift, out yShift);
        }
    }

    private static void MoveAroundAPointV2(int num, int width, int height, out int xShift, out int yShift)
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

        while (sqtCount < Config.MapSize + 10) // the MAX. size of the map is 200x200
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

    private static void ConstructMoveAroundTable()
    {
        if (moveAroundTableSize == MOVE_AROUND_TABLE_SIZE)
            return; // table already created

        for (int i = 0; i < MOVE_AROUND_TABLE_SIZE; ++i)
        {
            MoveAroundAPointV2(i, MOVE_AROUND_TABLE_SIZE, MOVE_AROUND_TABLE_SIZE, out int xShift, out int yShift);
            MoveAroundTableX[i] = xShift;
            MoveAroundTableY[i] = yShift;
        }

        moveAroundTableSize = MOVE_AROUND_TABLE_SIZE;
    }

    public static string RomanNumber(int inNum)
    {
        string result = string.Empty;

        if (inNum > 100)
        {
            result += RomanNumbers[inNum / 100 - 1];
            inNum = inNum - inNum / 100 * 100;
        }

        if (inNum > 10)
        {
            result += RomanNumbers[(inNum - 1) / 10 - 1];
            inNum = inNum - (inNum - 1) / 10 * 10;
        }

        result += RomanNumbers[inNum - 1];

        return result;
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
        StringBuilder result = new StringBuilder(chars.Length);
        foreach (var c in chars)
        {
            if (c != '\0')
            {
                result.Append(c);
            }
        }
        
        return result.ToString().TrimEnd();
    }

    public static string ToShortDate(DateTime date)
    {
        string result = date.Month switch
        {
            1 => "Jan",
            2 => "Feb",
            3 => "Mar",
            4 => "Apr",
            5 => "May",
            6 => "Jun",
            7 => "Jul",
            8 => "Aug",
            9 => "Sep",
            10 => "Oct",
            11 => "Nov",
            12 => "Dec",
            _ => String.Empty
        };

        return result + " " + date.Day + ", " + date.Year;
    }

    public static string ToLongDate(DateTime date)
    {
        string result = date.Month switch
        {
            1 => "January",
            2 => "February",
            3 => "March",
            4 => "April",
            5 => "May",
            6 => "June",
            7 => "July",
            8 => "August",
            9 => "September",
            10 => "October",
            11 => "November",
            12 => "December",
            _ => String.Empty
        };

        return result + " " + date.Day + ", " + date.Year;
    }
    
    #region SaveAndLoad

    public static void SaveTo(BinaryWriter writer)
    {
        writer.Write(RandomSeed);
        writer.Write(StartTicks);
    }

    public static void LoadFrom(BinaryReader reader)
    {
        RandomSeed = reader.ReadUInt32();
        StartTicks = reader.ReadInt64();
    }
	
    #endregion
}
