using System.Collections.Generic;
using System.IO;

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
        for (int i = 0; i < _rocks.Count; i++)
        {
            _rocks[i].Process();
        }
    }
    
    #region SaveAndLoad

    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(_rocks.Count);
        for (int i = 0; i < _rocks.Count; i++)
            _rocks[i].SaveTo(writer);
    }

    public void LoadFrom(BinaryReader reader)
    {
        int rocksCount = reader.ReadInt32();
        for (int i = 0; i < rocksCount; i++)
        {
            Rock rock = new Rock();
            rock.LoadFrom(reader);
            _rocks.Add(rock);
        }
    }
	
    #endregion
}