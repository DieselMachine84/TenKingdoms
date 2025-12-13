using System.Collections.Generic;

namespace TenKingdoms;

public class CaptureIndependentTask : AITask
{
    public int TownId { get; }
    public List<int> Capturers { get; } = new List<int>();

    public CaptureIndependentTask(Nation nation, int townId) : base(nation)
    {
        TownId = townId;
    }

    public override bool ShouldCancel()
    {
        return false;
    }

    public override void Process()
    {
    }
}
