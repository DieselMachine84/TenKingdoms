using System.Collections.Generic;

namespace TenKingdoms;

public class RegionArray
{
    public List<RegionInfo> RegionInfos { get; } = new List<RegionInfo>();
    public List<RegionStat> RegionStats { get; } = new List<RegionStat>();

    //TODO rewrite
    private int[] _connectBits;
    private readonly List<int> _sortedRegions = new List<int>(); // regions sorted by size

    private Info Info => Sys.Instance.Info;

    public void Init(int maxRegion)
    {
        int connectBit;
        if (maxRegion > 0)
        {
            // --------- allocate memory for RegionInfo --------//
            for (int i = 0; i < maxRegion; i++)
                RegionInfos.Add(new RegionInfo());

            // ---- calculate the no. of bit required to store connection ----//
            connectBit = (maxRegion - 1) * maxRegion / 2;
            // region 1 needs 0 bit
            // region 2 needs 1 bit
            // region 3 needs 2 bits
            // region 4 needs 3 bits...
        }
        else
        {
            RegionInfos.Clear();
            connectBit = 0;
        }

        _connectBits = connectBit > 0 ? new int[(connectBit + 7) / 8] : null;

        //------ initialize adj_offset_bit and area -------//

        int j = 0;

        for (int i = 0; i < RegionInfos.Count; i++)
        {
            RegionInfos[i].RegionId = i + 1;
            RegionInfos[i].AdjOffsetBit = j;
            RegionInfos[i].RegionSize = 0;

            j += i; // j += regionId-1;
        }
    }

    public void NextDay()
    {
        if (Info.TotalDays % 7 == 0)
            UpdateRegionStat();
    }

    public void IncSize(int reg)
    {
        RegionInfos[reg - 1].RegionSize++;
    }

    public void SetRegionType(int region, RegionType regType)
    {
        RegionInfos[region - 1].RegionType = regType;
    }

    public void SetAdjacent(int region1, int region2)
    {
        if (region1 == 0 || region2 == 0 || region1 == region2)
            return;

        int bitOffset;
        if (region1 > region2)
        {
            bitOffset = RegionInfos[region1 - 1].AdjOffsetBit + (region2 - 1);
        }
        else
        {
            bitOffset = RegionInfos[region2 - 1].AdjOffsetBit + (region1 - 1);
        }

        _connectBits[bitOffset / 8] |= 1 << (bitOffset % 8);
    }

    public bool IsAdjacent(int region1, int region2)
    {
        if (region1 == region2)
            return true;

        int bitOffset;
        if (region1 > region2)
        {
            bitOffset = RegionInfos[region1 - 1].AdjOffsetBit + (region2 - 1);
        }
        else
        {
            bitOffset = RegionInfos[region2 - 1].AdjOffsetBit + (region1 - 1);
        }

        return (_connectBits[bitOffset / 8] & (1 << (bitOffset % 8))) != 0;
    }

    public void InitRegionStat()
    {
        //------ count the no. of regions with statistic -----//
        //
        // Only include land regions that are big enough.
        //
        //----------------------------------------------------//
        
        SortRegions();

        for (int i = 0; i < RegionInfos.Count; i++)
        {
            RegionInfo regionInfo = GetSortedRegion(i);
            if (regionInfo.RegionType != RegionType.LAND)
                continue;

            RegionStat regionStat = new RegionStat(regionInfo.RegionId);
            RegionStats.Add(regionStat);
            regionInfo.RegionStatId = RegionStats.Count;
        }

        foreach (RegionStat regionStat in RegionStats)
        {
            regionStat.Init();
        }

        UpdateRegionStat();
    }

    public void UpdateRegionStat()
    {
        foreach (RegionStat regionStat in RegionStats)
            regionStat.UpdateStat();
    }

    public int GetSeaPathRegionId(int regionId1, int regionId2)
    {
        RegionStat regionStat = GetRegionStat(regionId1);
        int regionStatId2 = GetRegionInfo(regionId2).RegionStatId;

        for (int i = 0; i < regionStat.ReachableRegions.Count; i++)
        {
            RegionPath regionPath = regionStat.ReachableRegions[i];
            if (regionPath.LandRegionStatId == regionStatId2)
                return regionPath.SeaRegionId;
        }

        return 0;
    }

    public bool NationHasBaseTown(int regionId, int nationId)
    {
        for (int i = 0; i < RegionStats.Count; i++)
        {
            RegionStat regionStat = RegionStats[i];
            if (regionStat.RegionId != regionId)
                continue;

            return regionStat.BaseTownNationCounts[nationId - 1] > 0;
        }

        return false;
    }

    public RegionInfo GetRegionInfo(int regionId)
    {
        return RegionInfos[regionId - 1];
    }

    public RegionStat GetRegionStat(int regionId)
    {
        return RegionStats[GetRegionInfo(regionId).RegionStatId - 1];
    }

    private void SortRegions()
    {
        _sortedRegions.Clear();
        for (int i = 0; i < RegionInfos.Count; i++)
            _sortedRegions.Add(i + 1);

        _sortedRegions.Sort((x, y) => GetRegionInfo(x).RegionSize - GetRegionInfo(y).RegionSize);
    }

    private RegionInfo GetSortedRegion(int index)
    {
        return GetRegionInfo(_sortedRegions[index]);
    }
}