using System;

namespace TenKingdoms;

public class Lightning
{
    protected const int MAX_BRANCH = 8;
    
    public double X { get; private set; } // particle coordinate
    public double Y { get; private set; }
    public double DestX { get; private set; } // destination coordinate
    public double DestY { get; private set; }

    public int Steps { get; set; } // no. of step
    public int ExpectSteps { get; set; } // expected no. of steps
    public double V { get; set; } // magnitude of movement of x,y
    public double A { get; set; } // magnitude of ax, ay
    public double A0 { get; set; }
    public double R { get; set; } // magnitude of rx, ry
    public double R0 { get; set; }
    public double Wide { get; set; } // radian, MAX of angle of random vector
    private uint Seed { get; set; } // last random number
    public int EnergyLevel { get; set; } // initially 8

    protected static double Distance(double dx, double dy)
    {
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private bool Goal()
    {
        return Distance(DestX - X, DestY - Y) < V;
    }

    public virtual void Init(double fromX, double fromY, double toX, double toY, int energy)
    {
        X = fromX;
        Y = fromY;
        DestX = toX;
        DestY = toY;
        EnergyLevel = energy;
        V = 6.0;
        ExpectSteps = (int)(Distance(DestY - Y, DestX - X) / V * 1.2);
        if (ExpectSteps < 2)
            ExpectSteps = 2;
        Steps = 0;
        A0 = A = 8.0;
        R0 = R = 8.0 * A;
        Wide = Math.PI / 4;
        Seed = (uint)(fromX + fromY + toX + toY) | 1;
        RandomSeed();
    }

    private void UpdateParameter()
    {
        double progress = (double)Steps / (double)ExpectSteps;
        if (progress > 1.0)
            progress = 1.0;

        A = A0;		// constant
        R = R0 * (1 - progress);
        Wide = 0.25 * (1 + progress) * Math.PI;
    }

    public virtual void MoveParticle()
    {
        // determine attraction
        double attractionDist = Distance(DestX - X, DestY - Y);
        if (attractionDist < V)
            return;
        
        double aX = A * (DestX - X) / attractionDist;
        double aY = A * (DestY - Y) / attractionDist;

        // determine random component
        double attractionAngle = Math.Atan2(DestY - Y, DestX - X);
        double randomAngle = ((RandomSeed() & 255) / 128.0 - 1.0) * Wide + attractionAngle;
        double rX = R * Math.Cos(randomAngle);
        double rY = R * Math.Sin(randomAngle);

        // total
        double tX = aX + rX;
        double tY = aY + rY;
        double distance = Distance(tX, tY);

        // move x and y, along tX, tY but the magnitude is v
        if (distance > 0)
        {
            X += V * tX / distance;
            Y += V * tY / distance;
        }

        Steps++;
        UpdateParameter();
    }

    public double Progress() // return 0.0 to 1.0
    {
        return Goal() ? 1.0 : (double)Steps / (double)ExpectSteps;
    }

    protected uint RandomSeed() // shuffle and return seed
    {
        const int MULTIPLIER = 0x015a4e35;
        const int INCREMENT = 1;
        Seed = MULTIPLIER * Seed + INCREMENT;
        return Seed;
    }
}

public class YLightning : Lightning
{
    private Lightning[] Branch { get; } = new Lightning[MAX_BRANCH];
    private int UsedBranch { get; set; }
    private int BranchProbability { get; set; } // probability * 1000;
    private bool BranchLeft { get; set; }

    public YLightning()
    {
    }

    public override void Init(double fromX, double fromY, double toX, double toY, int energy)
    {
        for (int i = 0; i < MAX_BRANCH; i++)
        {
            Branch[i] = null;
        }

        UsedBranch = 0;
        base.Init(fromX, fromY, toX, toY, energy);
        BranchProbability = 1000 * MAX_BRANCH / ExpectSteps;
        if (BranchProbability < 10)
            BranchProbability = 10;
        if (BranchProbability > 300)
            BranchProbability = 300;
        BranchLeft = false;
    }

    public override void MoveParticle()
    {
        base.MoveParticle();

        // determine if branching occurs
        if (RandomSeed() % 1000 <= BranchProbability && UsedBranch < MAX_BRANCH)
        {
            int branchEnergy;
            if (EnergyLevel > 4)
            {
                Branch[UsedBranch] = new YLightning();
                branchEnergy = 4;
            }
            else
            {
                Branch[UsedBranch] = new Lightning();
                branchEnergy = 1;
            }

            //------ determine new location ------//
            // angle : attraction angle + or - PI/8 to PI*3/8
            // distant : 1/2 to 3/4 from Distance(DestX - X, DestY - Y);
            double branchDist = Distance(DestX - X, DestY - Y) * (32 + RandomSeed() % 16) / 64.0;
            double branchAngle = Math.Atan2(DestY - Y, DestX - X) + (4 + RandomSeed() % 8) * (BranchLeft ? Math.PI / -32.0 : Math.PI / 32.0);
            
            BranchLeft = !BranchLeft;
            Branch[UsedBranch].Init(X, Y, X + branchDist * Math.Cos(branchAngle), Y + branchDist * Math.Sin(branchAngle), branchEnergy);
            UsedBranch++;
        }
    }
}
