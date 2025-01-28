using System;

namespace TenKingdoms;

public class Plasma
{
    private class Sub
    {
        public int t; // top of stack
        public int[] v = new int[16]; // subdivided value
        public int[] r = new int[16]; // recursion level
    }

    public const int SHIFT_VALUE = 18;
    public const int MAX_COLOR = 256;

    private Sub subx = new Sub();
    private Sub suby = new Sub();

    public int max_x = 0; // no. of column
    public int max_y = 0; // no. of row
    public int[] matrix; // 2D matrix of (max_x+1)*(max_y+1)
    public int iparmx; // iparmx = parm.x * 16
    public int recur1;
    public int recur_level;

    private void sub_divide(int x1, int y1, int x2, int y2)
    {
        int v, i;

        if (x2 - x1 < 2 && y2 - y1 < 2)
            return;

        recur_level++;
        recur1 = 320 >> recur_level;

        int x = (x1 + x2) >> 1;
        int y = (y1 + y2) >> 1;

        if ((v = get_pix(x, y1)) == 0)
            v = adjust(x1, y1, x, y1, x2, y1);
        i = v;

        if ((v = get_pix(x2, y)) == 0)
            v = adjust(x2, y1, x2, y, x2, y2);
        i += v;

        if ((v = get_pix(x, y2)) == 0)
            v = adjust(x1, y2, x, y2, x2, y2);
        i += v;

        if ((v = get_pix(x1, y)) == 0)
            v = adjust(x1, y1, x1, y, x1, y2);
        i += v;

        if (get_pix(x, y) == 0)
            plot(x, y, (i + 2) >> 2);

        sub_divide(x1, y1, x, y);
        sub_divide(x, y1, x2, y);
        sub_divide(x, y, x2, y2);
        sub_divide(x1, y, x, y2);
        recur_level--;
    }

    private int new_sub_divide(int x1, int y1, int x2, int y2, int recur)
    {
        int x, y;
        int nx1;
        int nx;
        int ny1, ny;
        int i, v;

        recur1 = 320 >> recur;
        suby.t = 2;
        ny = suby.v[0] = y2;
        ny1 = suby.v[2] = y1;
        suby.r[0] = suby.r[2] = 0;
        suby.r[1] = 1;
        y = suby.v[1] = (ny1 + ny) >> 1;

        while (suby.t >= 1)
        {
            while (suby.r[suby.t - 1] < recur)
            {
                /*     1.  Create new entry at top of the stack  */
                /*     2.  Copy old top value to new top value.  */
                /*            This is largest y value.           */
                /*     3.  Smallest y is now old mid point       */
                /*     4.  Set new mid point recursion level     */
                /*     5.  New mid point value is average        */
                /*            of largest and smallest            */

                suby.t++;
                ny1 = suby.v[suby.t] = suby.v[suby.t - 1];
                ny = suby.v[suby.t - 2];
                suby.r[suby.t] = suby.r[suby.t - 1];
                y = suby.v[suby.t - 1] = (ny1 + ny) >> 1;
                suby.r[suby.t - 1] = Math.Max(suby.r[suby.t], suby.r[suby.t - 2]) + 1;
            }

            subx.t = 2;
            nx = subx.v[0] = x2;
            nx1 = subx.v[2] = x1;
            subx.r[0] = subx.r[2] = 0;
            subx.r[1] = 1;
            x = subx.v[1] = (nx1 + nx) >> 1;

            while (subx.t >= 1)
            {
                while (subx.r[subx.t - 1] < recur)
                {
                    subx.t++; /* move the top ofthe stack up 1 */
                    nx1 = subx.v[subx.t] = subx.v[subx.t - 1];
                    nx = subx.v[subx.t - 2];
                    subx.r[subx.t] = subx.r[subx.t - 1];
                    x = subx.v[subx.t - 1] = (nx1 + nx) >> 1;
                    subx.r[subx.t - 1] = Math.Max(subx.r[subx.t], subx.r[subx.t - 2]) + 1;
                }

                if ((i = get_pix(nx, y)) == 0)
                    i = adjust(nx, ny1, nx, y, nx, ny);

                v = i;

                if ((i = get_pix(x, ny)) == 0)
                    i = adjust(nx1, ny, x, ny, nx, ny);

                v += i;

                if (get_pix(x, y) == 0)
                {
                    if ((i = get_pix(x, ny1)) == 0)
                        i = adjust(nx1, ny1, x, ny1, nx, ny1);
                    v += i;
                    if ((i = get_pix(nx1, y)) == 0)
                        i = adjust(nx1, ny1, nx1, y, nx1, ny);
                    v += i;
                    plot(x, y, (v + 2) >> 2);
                }

                if (subx.r[subx.t - 1] == recur)
                    subx.t = subx.t - 2;
            }

            if (suby.r[suby.t - 1] == recur)
                suby.t = suby.t - 2;
        }

        return 0;
    }

    private int adjust(int xa, int ya, int x, int y, int xb, int yb)
    {
        int pseudoRandom = iparmx * (Misc.Random() - 16383);

        pseudoRandom = pseudoRandom * recur1;
        pseudoRandom = pseudoRandom >> SHIFT_VALUE;
        pseudoRandom = ((get_pix(xa, ya) + get_pix(xb, yb) + 1) >> 1) + pseudoRandom;

        if (pseudoRandom >= MAX_COLOR)
            pseudoRandom = MAX_COLOR - 1;

        if (pseudoRandom < 1)
            pseudoRandom = 1;

        plot(x, y, pseudoRandom);
        return pseudoRandom;
    }

    public Plasma(int x, int y)
    {
        max_x = x;
        max_y = y;
        matrix = new int[(x + 1) * (y + 1)];
    }

    public void generate(int genMethod, int grainFactor, int randomSeed)
    {
        int[] rnd = new int[4];

        iparmx = grainFactor * 8;

        //srand(randomSeed);

        for (int n = 0; n < 4; n++)
            rnd[n] = 1 + ((Misc.Random() / MAX_COLOR * (MAX_COLOR - 1)) >> (SHIFT_VALUE - 11));

        plot(0, 0, rnd[0]);
        plot(max_x, 0, rnd[1]);
        plot(max_x, max_y, rnd[2]);
        plot(0, max_y, rnd[3]);

        recur_level = 0;

        if (genMethod == 0) // use original method
        {
            sub_divide(0, 0, max_x, max_y);
        }
        else // use new method
        {
            int i = 1;
            int k = 1;
            recur1 = 1;
            while (new_sub_divide(0, 0, max_x, max_y, i) == 0)
            {
                k = k * 2;
                if (k > max_x && k > max_y)
                    break;
                i++;
            }
        }
    }

    public void generate2(int genMethod, int grainFactor, int randomSeed)
    {
        int i, k;

        iparmx = grainFactor * 8;

        //srand(randomSeed);

        recur_level = 0;

        if (genMethod == 0) // use original method
        {
            sub_divide(0, 0, max_x, max_y);
        }
        else // use new method
        {
            recur1 = i = k = 1;
            while (new_sub_divide(0, 0, max_x, max_y, i) == 0)
            {
                k = k * 2;
                if (k > max_x && k > max_y)
                    break;
                i++;
            }
        }
    }

    public int get_pix(int x, int y)
    {
        return matrix[y * (max_x + 1) + x];
    }

    public void plot(int x, int y, int value)
    {
        matrix[y * (max_x + 1) + x] = value;
    }

    public void add_base_level(int baseLevel)
    {
        int totalLoc = (max_x + 1) * (max_y + 1);
        int index = 0;

        for (int i = 0; i < totalLoc; i++, index++)
        {
            matrix[index] += baseLevel;
            if (matrix[index] < 1)
                matrix[index] = 1;
            if (matrix[index] >= MAX_COLOR)
                matrix[index] = MAX_COLOR - 1;
        }
    }

    public int calc_tera_base_level(int minHeight)
    {
        //------- count the percentage of sea and land -------//

        int totalLoc = (max_x + 1) * (max_y + 1);
        int i, locHeight, landCount = 0, baseLevel = 0;

        for (i = 0; i < totalLoc; i++)
        {
            if (matrix[i] >= minHeight)
                landCount++;
        }

        // ensure that percentage of land won't be less than 1/3

        if (landCount < 2 * totalLoc / 3) // if land is less than 2/3 of the map
            baseLevel = 50 * (totalLoc - landCount) / totalLoc;

        //-------- ensure availability of sea in the map -----------//

        int xLoc, yLoc, seaCount = 0, lowestGrassHeight = 0xFFFF;

        //---------- scan top & bottom side -----------//

        for (xLoc = 0; xLoc <= max_x; xLoc++)
        {
            //----------- top side ------------//

            locHeight = get_pix(xLoc, 0) + baseLevel;

            if (locHeight < minHeight) // it's a sea
                seaCount++;
            else
            {
                if (locHeight < lowestGrassHeight)
                    lowestGrassHeight = locHeight;
            }

            //--------- bottom side -----------//

            locHeight = get_pix(xLoc, max_y) + baseLevel;

            if (locHeight < minHeight) // it's a sea
                seaCount++;
            else
            {
                if (locHeight < lowestGrassHeight)
                    lowestGrassHeight = locHeight;
            }
        }

        //---------- scan left & right side -----------//

        for (yLoc = 0; yLoc <= max_y; yLoc++)
        {
            //----------- top side ------------//

            locHeight = get_pix(0, yLoc) + baseLevel;

            if (locHeight < minHeight) // it's a sea
                seaCount++;
            else
            {
                if (locHeight < lowestGrassHeight)
                    lowestGrassHeight = locHeight;
            }

            //--------- bottom side -----------//

            locHeight = get_pix(max_x, yLoc) + baseLevel;

            if (locHeight < minHeight) // it's a sea
                seaCount++;
            else
            {
                if (locHeight < lowestGrassHeight)
                    lowestGrassHeight = locHeight;
            }
        }


        //------- If there is not enough sea --------//

        int[] min_sea_count_array = { 1600, 700, 200 };
        int minSeaCount = min_sea_count_array[Sys.Instance.Config.land_mass - Config.OPTION_LOW];

        if (seaCount < minSeaCount)
            baseLevel -= lowestGrassHeight - minHeight + (minSeaCount - seaCount) / 15;

        return baseLevel;
    }

    public int stat(int groups, int[] minHeights, int[] freq)
    {
        int total = (max_x + 1) * (max_y + 1);
        int g;

        // -------- initialize freq count to zero ------//
        for (g = groups - 1; g >= 0; --g)
            freq[g] = 0;

        // -------- classify and increase counter -------//
        for (int i = 0; i < total; ++i)
        {
            int v = matrix[i];
            for (g = groups - 1; g >= 0; --g)
            {
                if (v >= minHeights[g])
                {
                    freq[g]++;
                    break;
                }
            }
        }

        return total;
    }

    public void shuffle_level(int minHeight, int maxHeight, int amplitude)
    {
        // don't change boundary points
        for (int y = 1; y < max_y; ++y)
        {
            for (int x = 1; x < max_x; ++x)
            {
                int h;
                if ((h = get_pix(x, y)) >= minHeight && h <= maxHeight
                                                     && (h = get_pix(x - 1, y - 1)) >= minHeight && h <= maxHeight
                                                     && (h = get_pix(x, y - 1)) >= minHeight && h <= maxHeight
                                                     && (h = get_pix(x + 1, y - 1)) >= minHeight && h <= maxHeight
                                                     && (h = get_pix(x - 1, y)) >= minHeight && h <= maxHeight
                                                     && (h = get_pix(x + 1, y)) >= minHeight && h <= maxHeight
                                                     && (h = get_pix(x - 1, y + 1)) >= minHeight && h <= maxHeight
                                                     && (h = get_pix(x, y + 1)) >= minHeight && h <= maxHeight
                                                     && (h = get_pix(x + 1, y + 1)) >= minHeight && h <= maxHeight
                   )
                {
                    h = get_pix(x, y);

                    // method 1 - random amplitude
                    //h += misc.random(amplitude*2+1) - amplitude;
                    //if( h > maxHeight )
                    //	h = maxHeight;
                    //if( h < minHeight )
                    //	h = minHeight;

                    // method 2 - folding (amplitude : +1, +3, +5... or -1, -3, -5...)
                    if (amplitude >= 0)
                        h = minHeight + (h - minHeight) * amplitude;
                    else
                        h = maxHeight + (maxHeight - h) * amplitude;
                    //int loopCount = 20;
                    while (h < minHeight || h > maxHeight)
                    {
                        if (h > maxHeight)
                        {
                            h = maxHeight - (h - maxHeight);
                        }
                        else if (h < minHeight)
                        {
                            h = minHeight + (minHeight - h);
                        }
                    }

                    plot(x, y, h);
                }
            }
        }
    }
}
