using System;

namespace TenKingdoms;

public class SiteArray : DynArray<Site>
{
	public int SelectedSiteId { get; set; }
	public int UntappedRawCount { get; set; }
	private int _scrollCount;
	private int _goldCoinCount;

	// standard no. of raw site in one game, based on this number, new mines pop up when existing mines run out of deposit
	private int _stdRawSiteCount;

	private Info Info => Sys.Instance.Info;
	private Power Power => Sys.Instance.Power;
	private World World => Sys.Instance.World;
	private RegionArray RegionArray => Sys.Instance.RegionArray;
	private TownArray TownArray => Sys.Instance.TownArray;

	public SiteArray()
	{
	}

	protected override Site CreateNewObject(int objectType)
	{
		return new Site();
	}

	public Site AddSite(int xLoc, int yLoc, int siteType, int objectId, int reserveQty = 0)
	{
		Site site = CreateNew();
		site.Init(nextId, siteType, objectId, xLoc, yLoc, reserveQty);
		nextId++;

		switch (siteType)
		{
			case Site.SITE_RAW:
				UntappedRawCount++;
				break;

			case Site.SITE_SCROLL:
				_scrollCount++;
				break;

			case Site.SITE_GOLD_COIN:
				_goldCoinCount++;
				break;
		}

		return site;
	}

	public void DeleteSite(Site site)
	{
		switch (site.SiteType)
		{
			case Site.SITE_RAW:
				UntappedRawCount--;
				break;

			case Site.SITE_SCROLL:
				_scrollCount--;
				break;

			case Site.SITE_GOLD_COIN:
				_goldCoinCount--;
				break;
		}

		site.Deinit();
		Delete(site.SiteId);

		if (site.SiteId == SelectedSiteId)
			SelectedSiteId = 0;
	}

	public void NextDay()
	{
		if (Info.TotalDays % 30 == 0)
		{
			GenerateRawSite(); // check if we need to generate new raw sites
		}

		//-- if there is any scroll or gold coins available, ask AI to get them --//

		if (_scrollCount > 0 || _goldCoinCount > 0)
		{
			foreach (Site site in this)
			{
				if (site.SiteType == Site.SITE_SCROLL || site.SiteType == Site.SITE_GOLD_COIN)
				{
					Location location = World.get_loc(site.LocX, site.LocY);

					//---- if the unit is standing on a scroll site -----//

					if (location.has_unit(UnitConstants.UNIT_LAND))
					{
						site.GetByUnit(location.unit_recno(UnitConstants.UNIT_LAND));
					}
					else if (Info.TotalDays % 5 == 0)
					{
						site.OrderAIUnitsToGetThisSite();
					}
				}
			}
		}
	}

	public void OrderAIUnitsToGetSites()
	{
		foreach (Site site in this)
		{
			if (site.SiteType == Site.SITE_SCROLL || site.SiteType == Site.SITE_GOLD_COIN)
			{
				site.OrderAIUnitsToGetThisSite();
			}
		}
	}
	
	public void GenerateRawSite(int rawGenCount = 0)
	{
		if (rawGenCount > 0)
			_stdRawSiteCount = rawGenCount; // use this number to determine whether new sites should emerge in the future

		//----- count the no. of existing raw sites -------//

		int existRawSiteCount = 0;
		foreach (Site site in this)
		{
			if (site.SiteType == Site.SITE_RAW && site.ReserveQty >= GameConstants.EXIST_RAW_RESERVE_QTY)
				existRawSiteCount++;
		}

		if (existRawSiteCount >= _stdRawSiteCount)
			return;

		//----- 100 attempts to create raw sites -------//

		for (int i = 0; i < 100; i++)
		{
			if (CreateRawSite())
			{
				if (++existRawSiteCount == _stdRawSiteCount)
					return;
			}
		}
	}

	public bool CreateRawSite(int townId = 0)
	{
		//-------- count the no. of each raw material -------//

		int[] rawCountArray = new int[GameConstants.MAX_RAW];

		foreach (Site site in this)
		{
			if (site.SiteType == Site.SITE_RAW)
				rawCountArray[site.ObjectId - 1]++;
		}

		//---- find the minimum raw count ----//

		int minCount = Int32.MaxValue;

		for (int i = 0; i < rawCountArray.Length; i++)
		{
			if (rawCountArray[i] < minCount)
				minCount = rawCountArray[i];
		}

		//----- pick a raw material type -----//

		int rawId = Misc.Random(GameConstants.MAX_RAW) + 1;

		for (int i = 0; i < GameConstants.MAX_RAW; i++)
		{
			if (++rawId > GameConstants.MAX_RAW)
				rawId = 1;

			// don't use this raw type unless it is one of the less available ones.
			if (rawCountArray[rawId - 1] == minCount)
				break;
		}

		//--------- create the raw site now ------//

		int locX1, locY1, locX2, locY2;
		int maxTries;
		int regionId = 0;

		if (townId != 0)
		{
			const int MAX_TOWN_SITE_DISTANCE = 10;

			Town town = TownArray[townId];

			locX1 = town.LocCenterX - MAX_TOWN_SITE_DISTANCE;
			locX2 = town.LocCenterX + MAX_TOWN_SITE_DISTANCE;
			locY1 = town.LocCenterY - MAX_TOWN_SITE_DISTANCE;
			locY2 = town.LocCenterY + MAX_TOWN_SITE_DISTANCE;

			locX1 = Math.Max(0, locX1);
			locX2 = Math.Min(GameConstants.MapSize - 1, locX2);
			locY1 = Math.Max(0, locY1);
			locY2 = Math.Min(GameConstants.MapSize - 1, locY2);

			maxTries = (locX2 - locX1 + 1) * (locY2 - locY1 + 1);
			regionId = town.RegionId;
		}
		else
		{
			locX1 = 0;
			locY1 = 0;
			locX2 = GameConstants.MapSize - 1;
			locY2 = GameConstants.MapSize - 1;

			maxTries = 10000;
		}

		//----- randomly locate a space to add the site -----//
		//TODO do not place sites too close to each other

		// 5,5 are the size of the raw site, it must be large enough for a mine to build and 1 location for the edges
		if (World.locate_space_random(ref locX1, ref locY1, locX2, locY2,
			    5, 5, maxTries, regionId, true))
		{
			RegionInfo regionInfo = RegionArray.GetRegionInfo(World.get_region_id(locX1, locY1));
			if (regionInfo.region_size < GameConstants.SMALLEST_RAW_REGION)
				return false;
			
			int reserveQty = GameConstants.MAX_RAW_RESERVE_QTY * (50 + Misc.Random(50)) / 100;

			// locX1 + 2 & locY1 + 2 as the located size is 3x3, the raw site is at the center of it
			AddSite(locX1 + 2, locY1 + 2, Site.SITE_RAW, rawId, reserveQty);

			return true;
		}
		else
		{
			return false;
		}
	}

	public void DisplayNext(int seekDir, bool sameNation)
	{
		if (SelectedSiteId == 0)
			return;

		int siteType = this[SelectedSiteId].SiteType;
		var enumerator = (seekDir >= 0) ? EnumerateAll(SelectedSiteId, true) : EnumerateAll(SelectedSiteId, false);

		foreach (int siteId in enumerator)
		{
			Site site = this[siteId];

			//--- check if the location of this site has been explored ---//

			if (!World.get_loc(site.LocX, site.LocY).explored())
				continue;

			if (site.SiteType == siteType && !site.HasMine)
			{
				Power.reset_selection();
				SelectedSiteId = site.SiteId;

				World.go_loc(site.LocX, site.LocY);
				return;
			}
		}
	}
}