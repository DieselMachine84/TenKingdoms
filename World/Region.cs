using System.Collections.Generic;

namespace TenKingdoms;

public enum RegionType
{
    REGION_INPASSABLE,
    REGION_LAND,
    REGION_SEA,
}

public class RegionInfo
{
    public int region_id;
    public int region_stat_id;

    public RegionType region_type;
    public int adj_offset_bit;
    public int region_size;

    // the center location of the region
    public int center_x;
    public int center_y;
}

public class RegionPath
{
    public int sea_region_id; // region id. of the sea route
    public int land_region_stat_id;
}

public class RegionStat
{
	//public const int MIN_STAT_REGION_SIZE = 100;
	//public const int MAX_REACHABLE_REGION_PER_STAT = 10;

	public int region_id; // sorted in the order of region size

	public bool[] nation_is_present_array = new bool[GameConstants.MAX_NATION];
	public int nation_presence_count;

	public int[] firm_type_count_array = new int[Firm.MAX_FIRM_TYPE];
	public int[] firm_nation_count_array = new int[GameConstants.MAX_NATION];
	public int[] camp_nation_count_array = new int[GameConstants.MAX_NATION];
	public int[] mine_nation_count_array = new int[GameConstants.MAX_NATION];
	public int[] harbor_nation_count_array = new int[GameConstants.MAX_NATION];
	public int total_firm_count;

	public int[] town_nation_count_array = new int[GameConstants.MAX_NATION];
	public int[] base_town_nation_count_array = new int[GameConstants.MAX_NATION];
	public int independent_town_count;
	public int total_town_count;

	public int[] nation_population_array = new int[GameConstants.MAX_NATION];
	public int[] nation_jobless_population_array = new int[GameConstants.MAX_NATION];

	public int[] unit_nation_count_array = new int[GameConstants.MAX_NATION];
	public int independent_unit_count; // either rebels or monsters
	public int total_unit_count;

	public int site_count;
	public int raw_count;

	public List<RegionPath> reachableRegions = new List<RegionPath>();

	private NationArray NationArray => Sys.Instance.NationArray;
	private UnitArray UnitArray => Sys.Instance.UnitArray;
	private FirmArray FirmArray => Sys.Instance.FirmArray;
	private TownArray TownArray => Sys.Instance.TownArray;
	private RegionArray RegionArray => Sys.Instance.RegionArray;
	private SiteArray SiteArray => Sys.Instance.SiteArray;

	private void Reset()
	{
		for (int i = 0; i < nation_is_present_array.Length; i++)
			nation_is_present_array[i] = false;
		nation_presence_count = 0;

		for (int i = 0; i < firm_type_count_array.Length; i++)
			firm_type_count_array[i] = 0;
		for (int i = 0; i < firm_nation_count_array.Length; i++)
			firm_nation_count_array[i] = 0;
		for (int i = 0; i < camp_nation_count_array.Length; i++)
			camp_nation_count_array[i] = 0;
		for (int i = 0; i < mine_nation_count_array.Length; i++)
			mine_nation_count_array[i] = 0;
		for (int i = 0; i < harbor_nation_count_array.Length; i++)
			harbor_nation_count_array[i] = 0;
		total_firm_count = 0;

		for (int i = 0; i < town_nation_count_array.Length; i++)
			town_nation_count_array[i] = 0;
		for (int i = 0; i < base_town_nation_count_array.Length; i++)
			base_town_nation_count_array[i] = 0;
		independent_town_count = 0;
		total_town_count = 0;

		for (int i = 0; i < nation_population_array.Length; i++)
			nation_population_array[i] = 0;
		for (int i = 0; i < nation_jobless_population_array.Length; i++)
			nation_jobless_population_array[i] = 0;

		for (int i = 0; i < unit_nation_count_array.Length; i++)
			unit_nation_count_array[i] = 0;
		independent_unit_count = 0;
		total_unit_count = 0;

		site_count = 0;
		raw_count = 0;
	}

	public void init()
	{
		//------- init reachable region array ------//

		for (int seaRegionId = 1; seaRegionId <= RegionArray.regionInfos.Count; seaRegionId++)
		{
			if (RegionArray.GetRegionInfo(seaRegionId).region_type != RegionType.REGION_SEA)
				continue;

			if (!RegionArray.is_adjacent(region_id, seaRegionId))
				continue;

			//--- scan thru all big regions (regions in region_stat_array) ---//

			for (int i = 1; i <= RegionArray.regionStats.Count; i++)
			{
				RegionStat regionStat = RegionArray.regionStats[i - 1];
				if (regionStat.region_id == region_id)
					continue;

				if (RegionArray.is_adjacent(seaRegionId, regionStat.region_id))
				{
					RegionPath regionPath = new RegionPath();
					regionPath.land_region_stat_id = i;
					regionPath.sea_region_id = seaRegionId;
					reachableRegions.Add(regionPath);
				}
			}
		}
	}

	public void update_stat()
	{
		Reset();

		//--------- update firm stat ---------//

		foreach (Firm firm in FirmArray)
		{
			if (firm.region_id != region_id)
				continue;

			if (firm.nation_recno == 0) // monster firms
				continue;

			firm_type_count_array[firm.firm_id - 1]++;
			firm_nation_count_array[firm.nation_recno - 1]++;

			total_firm_count++;

			if (firm.firm_id == Firm.FIRM_CAMP)
				camp_nation_count_array[firm.nation_recno - 1]++;

			if (firm.firm_id == Firm.FIRM_HARBOR)
				harbor_nation_count_array[firm.nation_recno - 1]++;

			if (firm.firm_id == Firm.FIRM_MINE)
				mine_nation_count_array[firm.nation_recno - 1]++;
		}

		//--------- update town stat ---------//

		foreach (Town town in TownArray)
		{
			if (town.RegionId != region_id)
				continue;

			if (town.NationId != 0)
			{
				town_nation_count_array[town.NationId - 1]++;

				if (town.IsBaseTown)
					base_town_nation_count_array[town.NationId - 1]++;

				nation_population_array[town.NationId - 1] += town.Population;
				nation_jobless_population_array[town.NationId - 1] += town.JoblessPopulation;
			}
			else
			{
				independent_town_count++;
			}

			total_town_count++;
		}

		//--------- update unit stat ---------//

		//TODO nation_recno cannot be used as index. Check everywhere
		foreach (Unit unit in UnitArray)
		{
			if (unit.region_id() != region_id)
				continue;

			if (unit.nation_recno != 0)
				unit_nation_count_array[unit.nation_recno - 1]++;
			else
				independent_unit_count++;

			total_unit_count++;
		}

		//--------- update site count --------//

		foreach (Site site in SiteArray)
		{
			if (site.region_id != region_id)
				continue;

			if (site.site_type == Site.SITE_RAW)
				raw_count++;

			site_count++;
		}

		//----- update each nation's presence on the region -----//

		//TODO nation_recno cannot be used as index. Check everywhere
		int index = 0;
		foreach (Nation nation in NationArray)
		{
			if (firm_nation_count_array[index] > 0 || town_nation_count_array[index] > 0 || unit_nation_count_array[index] > 0)
			{
				nation_is_present_array[index] = true;
				nation_presence_count++;
			}

			index++;
		}
	}
}
