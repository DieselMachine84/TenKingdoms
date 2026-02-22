using System.Collections.Generic;
using System.IO;

namespace TenKingdoms;

public enum RegionType
{
    IMPASSABLE, LAND, SEA
}

public class RegionInfo
{
    public int RegionId { get; set; }
    public int RegionStatId { get; set; }
    public RegionType RegionType { get; set; }
    public int RegionSize { get; set; }
    public int AdjOffsetBit { get; set; }
    
    #region SaveAndLoad

    public void SaveTo(BinaryWriter writer)
    {
	    writer.Write(RegionId);
	    writer.Write(RegionStatId);
	    writer.Write((int)RegionType);
	    writer.Write(RegionSize);
	    writer.Write(AdjOffsetBit);
    }

    public void LoadFrom(BinaryReader reader)
    {
	    RegionId = reader.ReadInt32();
	    RegionStatId = reader.ReadInt32();
	    RegionType = (RegionType)reader.ReadInt32();
	    RegionSize = reader.ReadInt32();
	    AdjOffsetBit = reader.ReadInt32();
    }
	
    #endregion
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

//TODO rewrite region code
public class RegionStat
{
	public int RegionId { get; private set; } // sorted by region size

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

	public RegionStat()
	{
	}

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
			if (firm.RegionId != RegionId)
				continue;

			if (firm.NationId == 0) // monster firms
				continue;

			_firmNationCounts[firm.NationId - 1]++;

			if (firm.FirmType == Firm.FIRM_CAMP)
				CampNationCounts[firm.NationId - 1]++;

			if (firm.FirmType == Firm.FIRM_HARBOR)
				HarborNationCounts[firm.NationId - 1]++;

			if (firm.FirmType == Firm.FIRM_MINE)
				MineNationCounts[firm.NationId - 1]++;
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
			if (unit.RegionId() != RegionId)
				continue;

			if (unit.NationId != 0)
				_unitNationCounts[unit.NationId - 1]++;
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
			int index = nation.NationId - 1;
			if (_firmNationCounts[index] > 0 || TownNationCounts[index] > 0 || _unitNationCounts[index] > 0)
			{
				NationPresenceCount++;
			}
		}
	}
	
	#region SaveAndLoad

	public void SaveTo(BinaryWriter writer)
	{
		writer.Write(RegionId);
		writer.Write(NationPresenceCount);
		for (int i = 0; i < _firmNationCounts.Length; i++)
			writer.Write(_firmNationCounts[i]);
		for (int i = 0; i < CampNationCounts.Length; i++)
			writer.Write(CampNationCounts[i]);
		for (int i = 0; i < MineNationCounts.Length; i++)
			writer.Write(MineNationCounts[i]);
		for (int i = 0; i < HarborNationCounts.Length; i++)
			writer.Write(HarborNationCounts[i]);
		for (int i = 0; i < TownNationCounts.Length; i++)
			writer.Write(TownNationCounts[i]);
		for (int i = 0; i < BaseTownNationCounts.Length; i++)
			writer.Write(BaseTownNationCounts[i]);
		writer.Write(IndependentTownCounts);
		for (int i = 0; i < NationPopulation.Length; i++)
			writer.Write(NationPopulation[i]);
		for (int i = 0; i < NationJoblessPopulation.Length; i++)
			writer.Write(NationJoblessPopulation[i]);
		for (int i = 0; i < _unitNationCounts.Length; i++)
			writer.Write(_unitNationCounts[i]);
		writer.Write(RawResourceCount);
		writer.Write(ReachableRegions.Count);
		for (int i = 0; i < ReachableRegions.Count; i++)
		{
			writer.Write(ReachableRegions[i].LandRegionStatId);
			writer.Write(ReachableRegions[i].SeaRegionId);
		}
	}

	public void LoadFrom(BinaryReader reader)
	{
		RegionId = reader.ReadInt32();
		NationPresenceCount = reader.ReadInt32();
		for (int i = 0; i < _firmNationCounts.Length; i++)
			_firmNationCounts[i] = reader.ReadInt32();
		for (int i = 0; i < CampNationCounts.Length; i++)
			CampNationCounts[i] = reader.ReadInt32();
		for (int i = 0; i < MineNationCounts.Length; i++)
			MineNationCounts[i] = reader.ReadInt32();
		for (int i = 0; i < HarborNationCounts.Length; i++)
			HarborNationCounts[i] = reader.ReadInt32();
		for (int i = 0; i < TownNationCounts.Length; i++)
			TownNationCounts[i] = reader.ReadInt32();
		for (int i = 0; i < BaseTownNationCounts.Length; i++)
			BaseTownNationCounts[i] = reader.ReadInt32();
		IndependentTownCounts = reader.ReadInt32();
		for (int i = 0; i < NationPopulation.Length; i++)
			NationPopulation[i] = reader.ReadInt32();
		for (int i = 0; i < NationJoblessPopulation.Length; i++)
			NationJoblessPopulation[i] = reader.ReadInt32();
		for (int i = 0; i < _unitNationCounts.Length; i++)
			_unitNationCounts[i] = reader.ReadInt32();
		RawResourceCount = reader.ReadInt32();
		int reachableRegionsCount = reader.ReadInt32();
		for (int i = 0; i < reachableRegionsCount; i++)
			ReachableRegions.Add(new RegionPath(reader.ReadInt32(), reader.ReadInt32()));
	}
	
	#endregion
}
