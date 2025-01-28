namespace TenKingdoms;

public class RainDrop
{
    public int cur_x, cur_y;
    public int dest_x, dest_y;
    public int fall_speed;

    public void init(int fromX, int fromY, int toX, int toY, int speed)
    {
        cur_x = fromX;
        cur_y = fromY;
        dest_x = toX;
        dest_y = toY;
        fall_speed = speed;
    }

    public void fall()
    {
        if (cur_y + fall_speed >= dest_y)
        {
            cur_y = dest_y;
            cur_x = dest_x;
        }
        else
        {
            cur_x += (dest_x - cur_x) * fall_speed / (dest_y - cur_y);
            cur_y += fall_speed;
        }
    }

    public bool is_goal()
    {
        return cur_y + fall_speed >= dest_y;
    }
}

public class RainSpot
{
    public int center_x, center_y;
    public int step;
    public int max_step;

    public void init(int destX, int destY, int maxStep)
    {
        center_x = destX;
        center_y = destY;
        step = 0;
        max_step = maxStep;
    }

    public void fall()
    {
        step++;
    }

    public bool is_goal()
    {
        return step > max_step;
    }
}

public class Rain
{
    public const int MAX_RAINDROP = 50;

    public int bound_x1, bound_x2, bound_y1, bound_y2;
    public double wind_slope;
    public int drop_per_turn;

    private RainDrop[] drop = new RainDrop[MAX_RAINDROP];
    private bool[] drop_flag = new bool[MAX_RAINDROP];
    private int active_drop; // no. of active drops
    private int last_drop;

    private RainSpot[] spot = new RainSpot[MAX_RAINDROP];
    private bool[] spot_flag = new bool[MAX_RAINDROP];
    private int active_spot; // no. of active drops
    private int last_spot;
    private uint seed;

    public void start_rain(int x1, int y1, int x2, int y2, int density, double slope)
    {
        bound_x1 = x1;
        bound_y1 = y1;
        bound_x2 = x2;
        bound_y2 = y2;
        wind_slope = slope;
        drop_per_turn = density;
        clear();
        last_drop = -1;
    }

    public void clear() // remove all rain drops
    {
        for (int i = 0; i < MAX_RAINDROP; i++)
        {
            drop_flag[i] = false;
            spot_flag[i] = false;
        }

        active_drop = 0;
        active_spot = 0;
    }

    public void stop_rain() // no more new rain drops
    {
        drop_per_turn = 0;
    }

    public void new_drops()
    {
        int dropRemain = drop_per_turn;
        int maxScan = MAX_RAINDROP;
        int i = last_drop;
        while (dropRemain > 0 && maxScan-- > 0)
        {
            i = (i + 1) % MAX_RAINDROP;
            if (!drop_flag[i])
            {
                int fromX = bound_x1 + (int)(rand_seed() % (bound_x2 - bound_x1));
                int height = (bound_y2 - bound_y1) / 8 + (int)(rand_seed() % (((bound_y2 - bound_y1) * 7) / 8));
                int speed = height / 4;
                drop[i].init(fromX, bound_y1, (int)(fromX + height * wind_slope), bound_y1 + height, speed);
                drop_flag[i] = true;
                active_drop++;
                dropRemain--;
                last_drop = i;
            }
        }
    }

    public void new_spot(int x, int y)
    {
        int spotRemain = 1;
        int maxScan = MAX_RAINDROP;
        int i = last_spot;
        while (spotRemain > 0 && maxScan-- > 0)
        {
            i = (i + 1) % MAX_RAINDROP;
            if (!spot_flag[i])
            {
                spot[i].init(x, y, drop_per_turn > 4 ? 6 : 4);
                spot_flag[i] = true;
                active_spot++;
                spotRemain--;
                last_spot = i;
            }
        }
    }

    public bool is_all_clear() // any raindrop is active?
    {
        int count = 0;
        int i;
        for (i = 0; i < MAX_RAINDROP; ++i)
            if (drop_flag[i])
                count++;
        // update active_drop;
        active_drop = count;

        int count2 = 0;
        for (i = 0; i < MAX_RAINDROP; ++i)
            if (spot_flag[i])
                count2++;
        // update active_spot;
        active_spot = count2;
        return count + count2 > 0;
    }

    public bool is_raining()
    {
        return active_drop > 0 || active_spot > 0;
    }

    private uint rand_seed()
    {
        const int MULTIPLIER = 0x015a4e35;
        const int INCREMENT = 1;
        seed = MULTIPLIER * seed + INCREMENT;
        return seed;
    }
}