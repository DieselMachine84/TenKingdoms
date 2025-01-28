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
    private DbfHeader dbfHeader = new DbfHeader();
    private byte[] buffer; // buffer for reading in the whole dbf

    public int RecordCount => dbfHeader.last_rec;

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
        dbfHeader.dbf_id = reader.ReadByte();
        dbfHeader.last_update[0] = reader.ReadByte();
        dbfHeader.last_update[1] = reader.ReadByte();
        dbfHeader.last_update[2] = reader.ReadByte();
        dbfHeader.last_rec = reader.ReadInt32();
        dbfHeader.data_offset = reader.ReadUInt16();
        dbfHeader.rec_size = reader.ReadUInt16();

        stream.Seek(1 + dbfHeader.data_offset, SeekOrigin.Begin);
        buffer = reader.ReadBytes(dbfHeader.rec_size * dbfHeader.last_rec);
    }
    
    //TODO remove
    public byte[] Read(int recNo = 0)
    {
        return buffer.Skip(dbfHeader.rec_size * (recNo - 1)).ToArray();
    }
    
    public byte ReadByte(int recNo, int index)
    {
        return buffer[dbfHeader.rec_size * (recNo - 1) + index];
    }
}
