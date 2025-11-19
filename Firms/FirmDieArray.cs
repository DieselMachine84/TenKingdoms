using System.Collections.Generic;

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
}