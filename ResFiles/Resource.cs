using System.IO;
using System.Linq;

namespace TenKingdoms;

public class Resource
{
    private int _recordCount; // total no. of records
    private int[] _indexBuf; // index buffer pointer
    private byte[] _dataBuf; // data buffer pointer

    public Resource(string resFile)
    {
        using FileStream stream = new FileStream(resFile, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new BinaryReader(stream);

        _recordCount = reader.ReadInt16();

        _indexBuf = new int[_recordCount + 1];
        for (int i = 0; i < _indexBuf.Length; i++)
            _indexBuf[i] = reader.ReadInt32();
        
        int dataSize = _indexBuf[_recordCount] - _indexBuf[0];
        _dataBuf = new byte[dataSize];
        for (int i = 0; i < _dataBuf.Length; i++)
            _dataBuf[i] = reader.ReadByte();
    }

    public byte[] Read(int recNo)
    {
        if (recNo < 1 || recNo > _recordCount)
            return null;

        return _dataBuf.Skip(_indexBuf[recNo - 1] - _indexBuf[0]).ToArray();
    }
}