using System.Collections.Generic;

namespace TenKingdoms;

public class RockArray
{
    private readonly List<Rock> _rocks = new List<Rock>();
    public RockArray()
    {
    }

    public Rock this[int id] => _rocks[id - 1];    
    
    public int Add(Rock rock)
    {
        _rocks.Add(rock);
        return _rocks.Count;
    }

    public void Process()
    {
        foreach (Rock rock in _rocks)
        {
            rock.Process();
        }
    }
}