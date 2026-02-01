namespace TenKingdoms;

public class RainDrop
{
    public int CurX { get; set; }
    public int CurY { get; set; }
    public int DestX { get; set; }
    public int DestY { get; set; }
    public int FallSpeed { get; set; }

    public void Init(int fromX, int fromY, int toX, int toY, int speed)
    {
        CurX = fromX;
        CurY = fromY;
        DestX = toX;
        DestY = toY;
        FallSpeed = speed;
    }

    public void Fall()
    {
        if (CurY + FallSpeed >= DestY)
        {
            CurY = DestY;
            CurX = DestX;
        }
        else
        {
            CurX += (DestX - CurX) * FallSpeed / (DestY - CurY);
            CurY += FallSpeed;
        }
    }

    public bool IsGoal()
    {
        return CurY + FallSpeed >= DestY;
    }
}

public class RainSpot
{
    public int CenterX { get; set; }
    public int CenterY { get; set; }
    public int Step { get; set; }
    public int MaxStep { get; set; }

    public void Init(int destX, int destY, int maxStep)
    {
        CenterX = destX;
        CenterY = destY;
        Step = 0;
        MaxStep = maxStep;
    }

    public void Fall()
    {
        Step++;
    }

    public bool IsGoal()
    {
        return Step > MaxStep;
    }
}

public class Rain
{
    public const int MAX_RAINDROP = 50;

    public int BoundX1 { get; set; }
    public int BoundY1 { get; set; }
    public int BoundX2 { get; set; }
    public int BoundY2 { get; set; }
    public double WindSlope { get; set; }
    public int DropPerTurn { get; set; }

    private RainDrop[] _drops = new RainDrop[MAX_RAINDROP];
    private bool[] _dropsFlag = new bool[MAX_RAINDROP];
    private int ActiveDrop { get; set; } // no. of active drops
    private int LastDrop { get; set; }

    private RainSpot[] _spots = new RainSpot[MAX_RAINDROP];
    private bool[] _spotsFlag = new bool[MAX_RAINDROP];
    private int ActiveSpot { get; set; } // no. of active drops
    private int LastSpot { get; set; }
    private uint Seed { get; set; }

    public void StartRain(int x1, int y1, int x2, int y2, int density, double slope)
    {
        BoundX1 = x1;
        BoundY1 = y1;
        BoundX2 = x2;
        BoundY2 = y2;
        WindSlope = slope;
        DropPerTurn = density;
        Clear();
        LastDrop = -1;
    }

    public void Clear() // remove all rain drops
    {
        for (int i = 0; i < MAX_RAINDROP; i++)
        {
            _dropsFlag[i] = false;
            _spotsFlag[i] = false;
        }

        ActiveDrop = 0;
        ActiveSpot = 0;
    }

    public void StopRain() // no more new rain drops
    {
        DropPerTurn = 0;
    }

    public void NewDrops()
    {
        int dropRemain = DropPerTurn;
        int maxScan = MAX_RAINDROP;
        int i = LastDrop;
        while (dropRemain > 0 && maxScan-- > 0)
        {
            i = (i + 1) % MAX_RAINDROP;
            if (!_dropsFlag[i])
            {
                int fromX = BoundX1 + (int)(RandomSeed() % (BoundX2 - BoundX1));
                int height = (BoundY2 - BoundY1) / 8 + (int)(RandomSeed() % ((BoundY2 - BoundY1) * 7 / 8));
                int speed = height / 4;
                _drops[i].Init(fromX, BoundY1, (int)(fromX + height * WindSlope), BoundY1 + height, speed);
                _dropsFlag[i] = true;
                ActiveDrop++;
                dropRemain--;
                LastDrop = i;
            }
        }
    }

    public void NewSpots(int x, int y)
    {
        int spotRemain = 1;
        int maxScan = MAX_RAINDROP;
        int i = LastSpot;
        while (spotRemain > 0 && maxScan-- > 0)
        {
            i = (i + 1) % MAX_RAINDROP;
            if (!_spotsFlag[i])
            {
                _spots[i].Init(x, y, DropPerTurn > 4 ? 6 : 4);
                _spotsFlag[i] = true;
                ActiveSpot++;
                spotRemain--;
                LastSpot = i;
            }
        }
    }

    public bool IsAllClear() // any raindrop is active?
    {
        int dropsCount = 0;
        for (int i = 0; i < MAX_RAINDROP; i++)
            if (_dropsFlag[i])
                dropsCount++;
        
        ActiveDrop = dropsCount;

        int spotsCount = 0;
        for (int i = 0; i < MAX_RAINDROP; i++)
            if (_spotsFlag[i])
                spotsCount++;
        
        ActiveSpot = spotsCount;
        
        return dropsCount + spotsCount > 0;
    }

    public bool IsRaining()
    {
        return ActiveDrop > 0 || ActiveSpot > 0;
    }

    private uint RandomSeed()
    {
        const int MULTIPLIER = 0x015a4e35;
        const int INCREMENT = 1;
        Seed = MULTIPLIER * Seed + INCREMENT;
        return Seed;
    }
}