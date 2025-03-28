using System.Collections.Generic;

namespace TenKingdoms;

public enum RegionType
{
    INPASSABLE, LAND, SEA
}

public class RegionInfo
{
    public int RegionId { get; set; }
    public int RegionStatId { get; set; }
    public RegionType RegionType { get; set; }
    public int RegionSize { get; set; }
    public int AdjOffsetBit { get; set; }
}

public class RegionPath
{
	public int LandRegionStatId { get; }
    public int SeaRegionId { get; }

    public RegionPath(int landRegionStatId, int seaRegionId)
    {
	    LandRegionStatId = landRegionStatId;
	    SeaRegionId = seaRegionId;
    }
}

public class RegionStat
{
	public int RegionId { get; } // sorted by region size

	public int NationPresenceCount { get; private set; }

	private readonly int[] _firmNationCounts = new int[GameConstants.MAX_NATION];
	public int[] CampNationCounts { get; } = new int[GameConstants.MAX_NATION];
	public int[] MineNationCounts { get; } = new int[GameConstants.MAX_NATION];
	public int[] HarborNationCounts { get; } = new int[GameConstants.MAX_NATION];

	public int[] TownNationCounts { get; } = new int[GameConstants.MAX_NATION];
	public int[] BaseTownNationCounts { get; } = new int[GameConstants.MAX_NATION];
	public int IndependentTownCounts { get; private set; }

	public int[] NationPopulation { get; } = new int[GameConstants.MAX_NATION];
	public int[] NationJoblessPopulation { get; } = new int[GameConstants.MAX_NATION];

	private readonly int[] _unitNationCounts = new int[GameConstants.MAX_NATION];

	public int RawResourceCount { get; private set; }

	public List<RegionPath> ReachableRegions { get; } = new List<RegionPath>();

	private NationArray NationArray => Sys.Instance.NationArray;
	private UnitArray UnitArray => Sys.Instance.UnitArray;
	private FirmArray FirmArray => Sys.Instance.FirmArray;
	private TownArray TownArray => Sys.Instance.TownArray;
	private RegionArray RegionArray => Sys.Instance.RegionArray;
	private SiteArray SiteArray => Sys.Instance.SiteArray;

	public RegionStat(int regionId)
	{
		RegionId = regionId;
	}

	public void Init()
	{
		//------- init reachable region array ------//

		for (int seaRegionId = 1; seaRegionId <= RegionArray.RegionInfos.Count; seaRegionId++)
		{
			if (RegionArray.GetRegionInfo(seaRegionId).RegionType != RegionType.SEA)
				continue;

			if (!RegionArray.IsAdjacent(RegionId, seaRegionId))
				continue;

			//--- scan through all regions ---//

			for (int i = 0; i < RegionArray.RegionStats.Count; i++)
			{
				RegionStat regionStat = RegionArray.RegionStats[i];
				if (regionStat.RegionId == RegionId)
					continue;

				if (RegionArray.IsAdjacent(seaRegionId, regionStat.RegionId))
				{
					ReachableRegions.Add(new RegionPath(i + 1, seaRegionId));
				}
			}
		}
	}

	private void Reset()
	{
		NationPresenceCount = 0;

		for (int i = 0; i < _firmNationCounts.Length; i++)
			_firmNationCounts[i] = 0;
		for (int i = 0; i < CampNationCounts.Length; i++)
			CampNationCounts[i] = 0;
		for (int i = 0; i < MineNationCounts.Length; i++)
			MineNationCounts[i] = 0;
		for (int i = 0; i < HarborNationCounts.Length; i++)
			HarborNationCounts[i] = 0;

		for (int i = 0; i < TownNationCounts.Length; i++)
			TownNationCounts[i] = 0;
		for (int i = 0; i < BaseTownNationCounts.Length; i++)
			BaseTownNationCounts[i] = 0;
		IndependentTownCounts = 0;

		for (int i = 0; i < NationPopulation.Length; i++)
			NationPopulation[i] = 0;
		for (int i = 0; i < NationJoblessPopulation.Length; i++)
			NationJoblessPopulation[i] = 0;

		for (int i = 0; i < _unitNationCounts.Length; i++)
			_unitNationCounts[i] = 0;
		
		RawResourceCount = 0;
	}
	
	public void UpdateStat()
	{
		Reset();

		foreach (Firm firm in FirmArray)
		{
			if (firm.region_id != RegionId)
				continue;

			if (firm.nation_recno == 0) // monster firms
				continue;

			_firmNationCounts[firm.nation_recno - 1]++;

			if (firm.firm_id == Firm.FIRM_CAMP)
				CampNationCounts[firm.nation_recno - 1]++;

			if (firm.firm_id == Firm.FIRM_HARBOR)
				HarborNationCounts[firm.nation_recno - 1]++;

			if (firm.firm_id == Firm.FIRM_MINE)
				MineNationCounts[firm.nation_recno - 1]++;
		}

		foreach (Town town in TownArray)
		{
			if (town.RegionId != RegionId)
				continue;

			if (town.NationId != 0)
			{
				TownNationCounts[town.NationId - 1]++;

				if (town.IsBaseTown)
					BaseTownNationCounts[town.NationId - 1]++;

				NationPopulation[town.NationId - 1] += town.Population;
				NationJoblessPopulation[town.NationId - 1] += town.JoblessPopulation;
			}
			else
			{
				IndependentTownCounts++;
			}
		}

		foreach (Unit unit in UnitArray)
		{
			if (unit.region_id() != RegionId)
				continue;

			if (unit.nation_recno != 0)
				_unitNationCounts[unit.nation_recno - 1]++;
		}

		foreach (Site site in SiteArray)
		{
			if (site.RegionId != RegionId)
				continue;

			if (site.SiteType == Site.SITE_RAW)
				RawResourceCount++;
		}

		//----- update each nation's presence on the region -----//

		foreach (Nation nation in NationArray)
		{
			int index = nation.nation_recno - 1;
			if (_firmNationCounts[index] > 0 || TownNationCounts[index] > 0 || _unitNationCounts[index] > 0)
			{
				NationPresenceCount++;
			}
		}
	}
}
