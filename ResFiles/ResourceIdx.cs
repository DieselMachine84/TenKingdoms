using System;
using System.IO;
using System.Linq;

namespace TenKingdoms;

public class ResourceItem
{
    public string name;
    public int pointer;
    public byte[] data;
}

public class ResourceIdx
{
    public ResourceItem[] resourceItems; // index buffer pointer
    public int rec_count; // total no. of records

    public ResourceIdx(string resFile)
    {
        using FileStream stream = new FileStream(resFile, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new BinaryReader(stream);

        rec_count = reader.ReadInt16();

        //---------- Read in record index -------------//

        resourceItems = new ResourceItem[rec_count + 1];

        // rec_count+1 is the last index pointer for calculating last record size

        for (int i = 0; i < rec_count + 1; i++)
        {
            resourceItems[i] = new ResourceItem();
            
            for (int j = 0; j < 9; j++)
            {
                byte symbol = reader.ReadByte();
                if (symbol != 0)
                    resourceItems[i].name += Convert.ToChar(symbol);
            }

            resourceItems[i].pointer = reader.ReadInt32();
        }

        //---------- Read in record data -------------//

        for (int i = 0; i < rec_count; i++)
        {
            resourceItems[i].data = reader.ReadBytes(resourceItems[i + 1].pointer - resourceItems[i].pointer);
        }
    }

    public byte[] Read(string dataName)
    {
        int indexId = GetIndex(dataName);

        return indexId != 0 ? GetData(indexId) : null;
    }

    public int GetIndex(string dataName)
    {
        for (int i = 0; i < rec_count; i++)
        {
            if (resourceItems[i].name == dataName)
                return i + 1;
        }

        return 0;
    }
    
    public string GetDataName(int index)
    {
        return resourceItems[index - 1].name;
    }

    public byte[] GetData(int indexId)
    {
        return resourceItems[indexId - 1].data;
        //return data_buf.Skip(resources[indexId].pointer - resources[0].pointer)
        //.Take(resources[indexId + 1].pointer - resources[indexId].pointer)
        //.ToArray();
    }

    public byte[] GetData(string dataName)
    {
        return GetData(GetIndex(dataName));
    }
}