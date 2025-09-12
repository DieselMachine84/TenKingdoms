using System;
using System.IO;
using System.Linq;

namespace TenKingdoms;

public class DbfHeader
{
    public byte dbf_id;
    public byte[] last_update = new byte[3];
    public int last_rec;
    public ushort data_offset;
    public ushort rec_size;
}

public class Database
{
    private readonly DbfHeader _dbfHeader = new DbfHeader();
    private byte[] _buffer; // buffer for reading in the whole dbf

    public int RecordCount => _dbfHeader.last_rec;

    public Database(string fileName)
    {
        using FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Init(stream);
    }

    public Database(byte[] data)
    {
        using MemoryStream stream = new MemoryStream(data);
        Init(stream);
    }

    private void Init(Stream stream)
    {
        using BinaryReader reader = new BinaryReader(stream);
        _dbfHeader.dbf_id = reader.ReadByte();
        _dbfHeader.last_update[0] = reader.ReadByte();
        _dbfHeader.last_update[1] = reader.ReadByte();
        _dbfHeader.last_update[2] = reader.ReadByte();
        _dbfHeader.last_rec = reader.ReadInt32();
        _dbfHeader.data_offset = reader.ReadUInt16();
        _dbfHeader.rec_size = reader.ReadUInt16();

        stream.Seek(1 + _dbfHeader.data_offset, SeekOrigin.Begin);
        _buffer = reader.ReadBytes(_dbfHeader.rec_size * _dbfHeader.last_rec);
    }
    
    public byte ReadByte(int recNo, int index)
    {
        return _buffer[_dbfHeader.rec_size * (recNo - 1) + index];
    }
}
