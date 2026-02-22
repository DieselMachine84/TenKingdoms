using System;

namespace TenKingdoms;

public class Plasma
{
    private class Sub
    {
        public int T; // top of stack
        public readonly int[] V = new int[16]; // subdivided value
        public readonly int[] R = new int[16]; // recursion level
    }

    private const int SHIFT_VALUE = 18;
    private const int MAX_COLOR = 256;

    private readonly Sub _subX = new Sub();
    private readonly Sub _subY = new Sub();

    private int _grainFactor;
    private int _recurFactor;
    public int MaxX { get; } // no. of column
    public int MaxY { get; } // no. of row
    private readonly int[] _matrix; // 2D matrix of (MaxX + 1) * (MaxY + 1)
    private int RecursionLevel { get; set; }

    public Plasma(int maxX, int maxY)
    {
        MaxX = maxX;
        MaxY = maxY;
        _matrix = new int[(maxX + 1) * (maxY + 1)];
    }
    
    public int GetPoint(int x, int y)
    {
        return _matrix[y * (MaxX + 1) + x];
    }

    public void SetPoint(int x, int y, int value)
    {
        _matrix[y * (MaxX + 1) + x] = value;
    }
    
    private void SubDivide(int x1, int y1, int x2, int y2)
    {
        int v, i;

        if (x2 - x1 < 2 && y2 - y1 < 2)
            return;

        RecursionLevel++;
        _recurFactor = 320 >> RecursionLevel;

        int x = (x1 + x2) >> 1;
        int y = (y1 + y2) >> 1;

        if ((v = GetPoint(x, y1)) == 0)
            v = Adjust(x1, y1, x, y1, x2, y1);
        i = v;

        if ((v = GetPoint(x2, y)) == 0)
            v = Adjust(x2, y1, x2, y, x2, y2);
        i += v;

        if ((v = GetPoint(x, y2)) == 0)
            v = Adjust(x1, y2, x, y2, x2, y2);
        i += v;

        if ((v = GetPoint(x1, y)) == 0)
            v = Adjust(x1, y1, x1, y, x1, y2);
        i += v;

        if (GetPoint(x, y) == 0)
            SetPoint(x, y, (i + 2) >> 2);

        SubDivide(x1, y1, x, y);
        SubDivide(x, y1, x2, y);
        SubDivide(x, y, x2, y2);
        SubDivide(x1, y, x, y2);
        RecursionLevel--;
    }

    private int NewSubDivide(int x1, int y1, int x2, int y2, int recur)
    {
        _recurFactor = 320 >> recur;
        _subY.T = 2;
        int ny = _subY.V[0] = y2;
        int ny1 = _subY.V[2] = y1;
        _subY.R[0] = _subY.R[2] = 0;
        _subY.R[1] = 1;
        int y = _subY.V[1] = (ny1 + ny) >> 1;

        while (_subY.T >= 1)
        {
            while (_subY.R[_subY.T - 1] < recur)
            {
                /*     1.  Create new entry at top of the stack  */
                /*     2.  Copy old top value to new top value.  */
                /*            This is largest y value.           */
                /*     3.  Smallest y is now old mid point       */
                /*     4.  Set new mid point recursion level     */
                /*     5.  New mid point value is average        */
                /*            of largest and smallest            */

                _subY.T++;
                ny1 = _subY.V[_subY.T] = _subY.V[_subY.T - 1];
                ny = _subY.V[_subY.T - 2];
                _subY.R[_subY.T] = _subY.R[_subY.T - 1];
                y = _subY.V[_subY.T - 1] = (ny1 + ny) >> 1;
                _subY.R[_subY.T - 1] = Math.Max(_subY.R[_subY.T], _subY.R[_subY.T - 2]) + 1;
            }

            _subX.T = 2;
            int nx = _subX.V[0] = x2;
            int nx1 = _subX.V[2] = x1;
            _subX.R[0] = _subX.R[2] = 0;
            _subX.R[1] = 1;
            int x = _subX.V[1] = (nx1 + nx) >> 1;

            while (_subX.T >= 1)
            {
                while (_subX.R[_subX.T - 1] < recur)
                {
                    _subX.T++; /* move the top of the stack up 1 */
                    nx1 = _subX.V[_subX.T] = _subX.V[_subX.T - 1];
                    nx = _subX.V[_subX.T - 2];
                    _subX.R[_subX.T] = _subX.R[_subX.T - 1];
                    x = _subX.V[_subX.T - 1] = (nx1 + nx) >> 1;
                    _subX.R[_subX.T - 1] = Math.Max(_subX.R[_subX.T], _subX.R[_subX.T - 2]) + 1;
                }

                int i;
                if ((i = GetPoint(nx, y)) == 0)
                    i = Adjust(nx, ny1, nx, y, nx, ny);

                int v = i;

                if ((i = GetPoint(x, ny)) == 0)
                    i = Adjust(nx1, ny, x, ny, nx, ny);

                v += i;

                if (GetPoint(x, y) == 0)
                {
                    if ((i = GetPoint(x, ny1)) == 0)
                        i = Adjust(nx1, ny1, x, ny1, nx, ny1);
                    v += i;
                    if ((i = GetPoint(nx1, y)) == 0)
                        i = Adjust(nx1, ny1, nx1, y, nx1, ny);
                    v += i;
                    SetPoint(x, y, (v + 2) >> 2);
                }

                if (_subX.R[_subX.T - 1] == recur)
                    _subX.T = _subX.T - 2;
            }

            if (_subY.R[_subY.T - 1] == recur)
                _subY.T = _subY.T - 2;
        }

        return 0;
    }

    private int Adjust(int xa, int ya, int x, int y, int xb, int yb)
    {
        int pseudoRandom = _grainFactor * (Misc.Random(0x7FFF) - 16383);

        pseudoRandom = pseudoRandom * _recurFactor;
        pseudoRandom = pseudoRandom >> SHIFT_VALUE;
        pseudoRandom = ((GetPoint(xa, ya) + GetPoint(xb, yb) + 1) >> 1) + pseudoRandom;

        if (pseudoRandom >= MAX_COLOR)
            pseudoRandom = MAX_COLOR - 1;

        if (pseudoRandom < 1)
            pseudoRandom = 1;

        SetPoint(x, y, pseudoRandom);
        return pseudoRandom;
    }

    public void Generate(int genMethod, int grainFactor)
    {
        int[] rnd = new int[4];

        _grainFactor = grainFactor * 8;

        for (int n = 0; n < 4; n++)
            rnd[n] = 1 + ((Misc.Random(0x7FFF) / MAX_COLOR * (MAX_COLOR - 1)) >> (SHIFT_VALUE - 11));

        SetPoint(0, 0, rnd[0]);
        SetPoint(MaxX, 0, rnd[1]);
        SetPoint(MaxX, MaxY, rnd[2]);
        SetPoint(0, MaxY, rnd[3]);

        RecursionLevel = 0;

        if (genMethod == 0) // use original method
        {
            SubDivide(0, 0, MaxX, MaxY);
        }
        else // use new method
        {
            int i = 1;
            int k = 1;
            _recurFactor = 1;
            while (NewSubDivide(0, 0, MaxX, MaxY, i) == 0)
            {
                k *= 2;
                if (k > MaxX && k > MaxY)
                    break;
                i++;
            }
        }
    }

    public void AddBaseLevel(int baseLevel)
    {
        int totalLoc = (MaxX + 1) * (MaxY + 1);
        int index = 0;

        for (int i = 0; i < totalLoc; i++, index++)
        {
            _matrix[index] += baseLevel;
            if (_matrix[index] < 1)
                _matrix[index] = 1;
            if (_matrix[index] >= MAX_COLOR)
                _matrix[index] = MAX_COLOR - 1;
        }
    }

    public int Stat(int groups, int[] minHeights, int[] freq)
    {
        int total = (MaxX + 1) * (MaxY + 1);

        // -------- initialize freq count to zero ------//
        for (int g = groups - 1; g >= 0; g--)
            freq[g] = 0;

        // -------- classify and increase counter -------//
        for (int i = 0; i < total; ++i)
        {
            int v = _matrix[i];
            for (int g = groups - 1; g >= 0; g--)
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

    public void ShuffleLevel(int minHeight, int maxHeight, int amplitude)
    {
        // don't change boundary points
        for (int y = 1; y < MaxY; y++)
        {
            for (int x = 1; x < MaxX; x++)
            {
                int h;
                if ((h = GetPoint(x, y)) >= minHeight && h <= maxHeight &&
                    (h = GetPoint(x - 1, y - 1)) >= minHeight && h <= maxHeight &&
                    (h = GetPoint(x, y - 1)) >= minHeight && h <= maxHeight &&
                    (h = GetPoint(x + 1, y - 1)) >= minHeight && h <= maxHeight &&
                    (h = GetPoint(x - 1, y)) >= minHeight && h <= maxHeight &&
                    (h = GetPoint(x + 1, y)) >= minHeight && h <= maxHeight &&
                    (h = GetPoint(x - 1, y + 1)) >= minHeight && h <= maxHeight &&
                    (h = GetPoint(x, y + 1)) >= minHeight && h <= maxHeight &&
                    (h = GetPoint(x + 1, y + 1)) >= minHeight && h <= maxHeight)
                {
                    h = GetPoint(x, y);

                    // method 1 - random amplitude
                    //h += misc.random(amplitude * 2 + 1) - amplitude;
                    //if (h > maxHeight)
                    //	h = maxHeight;
                    //if (h < minHeight)
                    //	h = minHeight;

                    // method 2 - folding (amplitude : +1, +3, +5... or -1, -3, -5...)
                    if (amplitude >= 0)
                        h = minHeight + (h - minHeight) * amplitude;
                    else
                        h = maxHeight + (maxHeight - h) * amplitude;
                    
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

                    SetPoint(x, y, h);
                }
            }
        }
    }
}
