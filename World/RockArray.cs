using System.Collections.Generic;

namespace TenKingdoms;

public class RockArray
{
    private List<Rock> rocks = new List<Rock>();
    public RockArray()
    {
    }

    public Rock this[int recNo] => rocks[recNo - 1];    
    
    public int Add(Rock rock)
    {
        rocks.Add(rock);
        return rocks.Count;
    }

    public void Process()
    {
        foreach (Rock rock in rocks)
        {
            rock.Process();
        }
    }
}