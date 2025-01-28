using System.Collections.Generic;

namespace TenKingdoms;

public class RegionArray
{
    public List<RegionInfo> regionInfos = new List<RegionInfo>();
    public List<RegionStat> regionStats = new List<RegionStat>();

    public int[] connect_bits;
    public List<int> region_sorted_array = new List<int>(); // an array of region id. sorted by the region size

    private Info Info => Sys.Instance.Info;

    public void Init(int maxRegion)
    {
        int connectBit;
        if (maxRegion > 0)
        {
            // --------- allocate memory for RegionInfo --------//
            for (int i = 0; i < maxRegion; i++)
                regionInfos.Add(new RegionInfo());

            // ---- calculate the no. of bit required to store connection ----//
            connectBit = (maxRegion - 1) * maxRegion / 2;
            // region 1 needs 0 bit
            // region 2 needs 1 bit
            // region 3 needs 2 bits
            // region 4 needs 3 bits...
        }
        else
        {
            regionInfos.Clear();
            connectBit = 0;
        }

        connect_bits = connectBit > 0 ? new int[(connectBit + 7) / 8] : null;

        //------ initialize adj_offset_bit and area -------//

        int j = 0;

        for (int i = 0; i < regionInfos.Count; i++)
        {
            regionInfos[i].region_id = i + 1;
            regionInfos[i].adj_offset_bit = j;
            regionInfos[i].region_size = 0;

            j += i; // j += regionId-1;
        }
    }

    public void next_day()
    {
        if (Info.TotalDays % 7 == 0)
            update_region_stat();
    }

    public void inc_size(int reg)
    {
        regionInfos[reg - 1].region_size++;
    }

    public void set_region(int reg, RegionType regType)
    {
        regionInfos[reg - 1].region_type = regType;
    }

    public void set_adjacent(int reg1, int reg2)
    {
        if (reg1 == 0 || reg2 == 0)
            return;

        if (reg1 == reg2)
            return;

        int bitOffset;
        if (reg1 > reg2)
        {
            bitOffset = regionInfos[reg1 - 1].adj_offset_bit + (reg2 - 1);
        }
        else
        {
            bitOffset = regionInfos[reg2 - 1].adj_offset_bit + (reg1 - 1);
        }

        connect_bits[bitOffset / 8] |= 1 << (bitOffset % 8);

    }

    public bool is_adjacent(int reg1, int reg2)
    {
        if (reg1 == reg2)
            return true;

        int bitOffset;
        if (reg1 > reg2)
        {
            bitOffset = regionInfos[reg1 - 1].adj_offset_bit + (reg2 - 1);
        }
        else
        {
            bitOffset = regionInfos[reg2 - 1].adj_offset_bit + (reg1 - 1);
        }

        return (connect_bits[bitOffset / 8] & (1 << (bitOffset % 8))) != 0;
    }

    public void sort_region()
    {
        region_sorted_array.Clear();
        for (int i = 0; i < regionInfos.Count; i++)
            region_sorted_array.Add(i + 1);

        //----------- sort it now -----------//

        region_sorted_array.Sort((x, y) => GetRegionInfo(x).region_size - GetRegionInfo(y).region_size);
    }

    public void init_region_stat()
    {
        //------ count the no. of regions with statistic -----//
        //
        // Only include land regions that are big enough.
        //
        //----------------------------------------------------//

        int region_stat_count = 0;
        
        for (int i = 0; i < regionInfos.Count; i++)
        {
            RegionInfo regionInfo = regionInfos[i];

            if (regionInfo.region_type == RegionType.REGION_LAND)
            {
                region_stat_count++;
            }
        }

        //-------- init the region_stat_array ---------//

        for (int i = 0; i < region_stat_count; i++)
            regionStats.Add(new RegionStat());

        int regionStatId = 1;

        for (int i = 1; i <= regionInfos.Count; i++)
        {
            RegionInfo regionInfo = get_sorted_region(i);

            if (regionInfo.region_type != RegionType.REGION_LAND)
                continue;

            regionStats[regionStatId - 1].region_id = regionInfo.region_id;
            regionInfo.region_stat_id = regionStatId;

            if (++regionStatId > region_stat_count)
                break;
        }

        for (int i = 0; i < region_stat_count; i++)
            regionStats[i].init();

        update_region_stat();
    }

    public void update_region_stat()
    {
        for (int i = 0; i < regionStats.Count; i++)
            regionStats[i].update_stat();
    }

    public int get_sea_path_region_id(int regionId1, int regionId2)
    {
        RegionStat regionStat = get_region_stat(regionId1);
        int regionStatId2 = GetRegionInfo(regionId2).region_stat_id;

        for (int i = 0; i < regionStat.reachableRegions.Count; i++)
        {
            RegionPath regionPath = regionStat.reachableRegions[i];
            if (regionPath.land_region_stat_id == regionStatId2)
                return regionPath.sea_region_id;
        }

        return 0;
    }

    public bool nation_has_base_town(int regionId, int nationRecno)
    {
        for (int i = 0; i < regionStats.Count; i++)
        {
            RegionStat regionStat = regionStats[i];
            if (regionStat.region_id != regionId)
                continue;

            return regionStat.base_town_nation_count_array[nationRecno - 1] > 0;
        }

        return false;
    }

    public RegionInfo GetRegionInfo(int region)
    {
        return regionInfos[region - 1];
    }

    //TODO check it
    public RegionStat get_region_stat(int regionId)
    {
        return regionStats[GetRegionInfo(regionId).region_stat_id - 1];
    }

    public RegionStat get_region_stat2(int regionStatId)
    {
        return regionStats[regionStatId - 1];
    }

    public RegionInfo get_sorted_region(int recNo)
    {
        return GetRegionInfo(region_sorted_array[recNo - 1]);
    }
}