using System.Collections.Generic;

namespace TenKingdoms;

public class FirmDieArray : DynArray<FirmDie>
{
    public FirmDieArray()
    {
    }

    protected override FirmDie CreateNewObject(int objectId)
    {
        return new FirmDie();
    }

    public void Add(Firm firm)
    {
        FirmDie firmDie = CreateNew();
        firmDie.firmdie_recno = nextRecNo;
        firmDie.Init(firm);
        nextRecNo++;
    }

    public override bool IsDeleted(int recNo)
    {
        if (base.IsDeleted(recNo))
            return true;

        FirmDie firmDie = this[recNo];
        return firmDie.firm_id == 0;
    }

    public void Process()
    {
        List<FirmDie> firmDiesToDelete = new List<FirmDie>();
        
        foreach (FirmDie firmDie in this)
        {
            if (firmDie.Process())
            {
                firmDiesToDelete.Add(firmDie);
            }
        }

        foreach (FirmDie firmDie in firmDiesToDelete)
        {
            Delete(firmDie.firmdie_recno);
        }
    }
}