using System.IO;

namespace TenKingdoms;

public class FirmDieArray : DynArray<FirmDie>
{
    public FirmDieArray()
    {
    }

    protected override FirmDie CreateNewObject(int objectType)
    {
        return new FirmDie();
    }

    public void Add(Firm firm)
    {
        FirmDie firmDie = CreateNew();
        firmDie.Init(firm);
    }

    public void Process()
    {
        foreach (FirmDie firmDie in this)
        {
            if (firmDie.Process())
            {
                Delete(firmDie.FirmDieId);
            }
        }
    }
    
    #region SaveAndLoad

    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(NextId);
        int count = Count();
        writer.Write(count);
        foreach (FirmDie firmDie in EnumerateWithDeleted())
        {
            firmDie.SaveTo(writer);
        }
    }

    public void LoadFrom(BinaryReader reader)
    {
        NextId = reader.ReadInt32();
        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            FirmDie firmDie = CreateNewObject(0);
            firmDie.LoadFrom(reader);
            Load(firmDie);
        }
    }
	
    #endregion
}