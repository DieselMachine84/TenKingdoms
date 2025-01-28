using System.IO;
using System.Linq;

namespace TenKingdoms;

public class Resource
{
    public int rec_count; // total no. of records

    public int[] index_buf; // index buffer pointer
    public byte[] data_buf; // data buffer pointer

    public Resource(string resFile)
    {
        using FileStream stream = new FileStream(resFile, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new BinaryReader(stream);

        rec_count = reader.ReadInt16();
        index_buf = new int[rec_count + 1];

        for (int i = 0; i < index_buf.Length; i++)
            index_buf[i] = reader.ReadInt32();
        
        int dataSize = index_buf[rec_count] - index_buf[0];
        data_buf = new byte[dataSize];
        for (int i = 0; i < data_buf.Length; i++)
            data_buf[i] = reader.ReadByte();
    }

    public byte[] Read(int recNo)
    {
        if (recNo < 1 || recNo > rec_count) // when no in debug mode, err_when() will be removed
            return null;

        //return data_buf + index_buf[recNo - 1] - index_buf[0];
        //TODO optimize
        return data_buf.Skip(index_buf[recNo - 1] - index_buf[0]).ToArray();
    }
}