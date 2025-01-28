using System;

namespace TenKingdoms;

public class Lightning
{
    public const int MAX_BRANCH = 8;
    
    public static int bound_x1, bound_y1, bound_x2, bound_y2;

    public double x, y; // particle coordinate
    public double destx, desty; // destination coordinate

    public int steps; // no. of step
    public int expect_steps; // expected no. of steps
    public double v; // magnitude of movement of x,y
    public double a, a0; // magnitude of ax, ay
    public double r, r0; // magnitude of rx, ry
    public double wide; // radian, MAX of angle of random vector
    public uint seed; // last random number
    public int energy_level; // initially 8

    public static double dist(double dx, double dy)
    {
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public static void set_clip(int x1, int y1, int x2, int y2)
    {
        bound_x1 = x1;
        bound_x2 = x2;
        bound_y1 = y1;
        bound_y2 = y2;
    }

    public bool goal()
    {
        return dist(destx - x, desty - y) < v;
    }

    public virtual void init(double fromX, double fromY, double toX, double toY, int energy)
    {
        x = fromX;
        y = fromY;
        destx = toX;
        desty = toY;
        energy_level = energy;
        v = 6.0;
        expect_steps = (int)(dist(desty - y, destx - x) / v * 1.2);
        if (expect_steps < 2)
            expect_steps = 2;
        steps = 0;
        a0 = a = 8.0;
        r0 = r = 8.0 * a;
        wide = Math.PI / 4;
        seed = (uint)(fromX + fromY + toX + toY) | 1;
        rand_seed();
    }

    public virtual void update_parameter()
    {
        double progress = (double)steps / (double)expect_steps;
        if (progress > 1)
            progress = 1;

        // a = a0;		// constant

        r = r0 * (1 - progress);
        wide = 0.25 * (1 + progress) * Math.PI;
    }

    public virtual void move_particle()
    {
        // determine attraction
        double attractionDist = dist(destx - x, desty - y);
        if (attractionDist < v)
            return;
        double aX = a * (destx - x) / attractionDist;
        double aY = a * (desty - y) / attractionDist;

        // determine random component
        double attractionAngle = Math.Atan2(desty - y, destx - x);
        double randomAngle = ((rand_seed() & 255) / 128.0 - 1.0) * wide + attractionAngle;
        double rX = r * Math.Cos(randomAngle);
        double rY = r * Math.Sin(randomAngle);

        // total
        double tX = aX + rX;
        double tY = aY + rY;
        double distt = dist(tX, tY);

        // move x and y, along tX, tY but the magnitude is v
        if (distt > 0)
        {
            x += v * tX / distt;
            y += v * tY / distt;
        }

        steps++;
        update_parameter();
    }

    public double progress() // return 0.0 to 1.0
    {
        if (goal())
            return 1.0;
        else
            return (double)steps / (double)expect_steps;
    }

    protected uint rand_seed() // shuffle and return seed
    {
        const int MULTIPLIER = 0x015a4e35;
        const int INCREMENT = 1;
        seed = MULTIPLIER * seed + INCREMENT;
        return seed;
    }
}

//--------- Define class YLightning ----------//

public class YLightning : Lightning
{
    public Lightning[] branch = new Lightning[MAX_BRANCH];
    public int used_branch;
    public int branch_prob; // probability * 1000;
    public bool branch_left;

    public YLightning()
    {
    }

    public override void init(double fromX, double fromY, double toX, double toY, int energy)
    {
        for (int i = 0; i < MAX_BRANCH; ++i)
        {
            branch[i] = null;
        }

        used_branch = 0;
        base.init(fromX, fromY, toX, toY, energy);
        branch_prob = 1000 * MAX_BRANCH / expect_steps;
        if (branch_prob < 10)
            branch_prob = 10;
        if (branch_prob > 300)
            branch_prob = 300;
        branch_left = false;
    }

    public override void move_particle()
    {
        base.move_particle();

        // determine if branching occurs
        if ((rand_seed() % 1000) <= branch_prob && used_branch < MAX_BRANCH)
        {
            int branch_energy;
            if (energy_level > 4)
            {
                branch[used_branch] = new YLightning();
                branch_energy = 4;
            }
            else
            {
                branch[used_branch] = new Lightning();
                branch_energy = 1;
            }

            //------ determine new location ------//
            // angle : attraction angle + or - PI/8 to PI*3/8
            // distant : 1/2 to 3/4 from dist(destx-x, desty-y);
            double branchDist = dist(destx - x, desty - y) * (32 + rand_seed() % 16) / 64.0;
            double branchAngle = Math.Atan2(desty - y, destx - x) +
                                 (4 + rand_seed() % 8) * (branch_left ? Math.PI / -32.0 : Math.PI / 32.0);
            branch_left = !branch_left;

            branch[used_branch].init(x, y, x + branchDist * Math.Cos(branchAngle),
                y + branchDist * Math.Sin(branchAngle), branch_energy);

            used_branch++;
        }
    }
}
