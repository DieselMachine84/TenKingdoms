using System;
using System.IO;
using System.Linq;

namespace TenKingdoms;

public class ResourceDb
{
    private byte[] _buffer;
    
    public ResourceDb(string resName)
    {
        using FileStream stream = new FileStream(resName, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new BinaryReader(stream);

        const int bufferSize = 4096;
        using MemoryStream ms = new MemoryStream();
        byte[] tempBuffer = new byte[bufferSize];
        int count = 0;
        while ((count = reader.Read(tempBuffer, 0, tempBuffer.Length)) != 0)
            ms.Write(tempBuffer, 0, count);
        _buffer = ms.ToArray();
    }

    public byte[] Read(int offset)
    {
        int size = BitConverter.ToInt32(_buffer, offset);
        return _buffer.Skip(offset + sizeof(int)).Take(size).ToArray();
    }

    public byte[] ReadFull()
    {
        return _buffer;
    }
}