using System;
using System.IO;
using System.Linq;

namespace TenKingdoms;

public class ResourceItem
{
    public string Name { get; set; }
    public int Pointer { get; set; }
    public byte[] Data { get; set; }
}

public class ResourceIdx
{
    private readonly ResourceItem[] _resourceItems;
    public int RecordCount { get; }

    public ResourceIdx(string resFile)
    {
        using FileStream stream = new FileStream(resFile, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new BinaryReader(stream);

        RecordCount = reader.ReadInt16();

        _resourceItems = new ResourceItem[RecordCount + 1];

        // RecordCount + 1 is the last index pointer for calculating last record size

        for (int i = 0; i < RecordCount + 1; i++)
        {
            _resourceItems[i] = new ResourceItem();
            
            for (int j = 0; j < 9; j++)
            {
                byte symbol = reader.ReadByte();
                if (symbol != 0)
                    _resourceItems[i].Name += Convert.ToChar(symbol);
            }

            _resourceItems[i].Pointer = reader.ReadInt32();
        }

        for (int i = 0; i < RecordCount; i++)
        {
            _resourceItems[i].Data = reader.ReadBytes(_resourceItems[i + 1].Pointer - _resourceItems[i].Pointer);
        }
    }

    public byte[] Read(string dataName)
    {
        int indexId = GetIndex(dataName);

        return indexId != 0 ? GetData(indexId) : null;
    }

    public int GetIndex(string dataName)
    {
        for (int i = 0; i < RecordCount; i++)
        {
            if (_resourceItems[i].Name == dataName)
                return i + 1;
        }

        return 0;
    }
    
    public string GetDataName(int index)
    {
        return _resourceItems[index - 1].Name;
    }

    public byte[] GetData(int indexId)
    {
        return _resourceItems[indexId - 1].Data;
    }

    public byte[] GetData(string dataName)
    {
        return GetData(GetIndex(dataName));
    }
}