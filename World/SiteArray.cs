using System;

namespace TenKingdoms;

public class SiteArray : DynArray<Site>
{
	public const int SMALLEST_RAW_REGION = 50; // only put raw on the region if its size is larger than this

	public int selected_recno; // the firm current being selected
	public int untapped_raw_count; // no. of unoccupied raw site available
	public int scroll_count;
	public int gold_coin_count;

	// standard no. of raw site in one game, based on this number, new mines pop up when existing mines run out of deposit
	public int std_raw_site_count;

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
		site.site_recno = nextId;
		nextId++;
		site.Init(siteType, xLoc, yLoc, objectId, reserveQty);

		switch (siteType)
		{
			case Site.SITE_RAW:
				untapped_raw_count++;
				break;

			case Site.SITE_SCROLL:
				scroll_count++;
				break;

			case Site.SITE_GOLD_COIN:
				gold_coin_count++;
				break;
		}

		return site;
	}

	public void DeleteSite(Site site)
	{
		switch (site.site_type)
		{
			case Site.SITE_RAW:
				untapped_raw_count--;
				break;

			case Site.SITE_SCROLL:
				scroll_count--;
				break;

			case Site.SITE_GOLD_COIN:
				gold_coin_count--;
				break;
		}

		int siteRecno = site.site_recno;
		site.Deinit();
		Delete(siteRecno);

		if (siteRecno == selected_recno)
			selected_recno = 0;
	}

	public void NextDay()
	{
		if (Info.TotalDays % 30 == 0)
		{
			generate_raw_site(); // check if we need to generate existing raw sites are being used up and if we need to generate new ones
		}

		//-- if there is any scroll or gold coins available, ask AI to get them --//

		if (scroll_count > 0 || gold_coin_count > 0)
		{
			bool aiGetSiteObject = (Info.TotalDays % 5 == 0);

			foreach (Site site in this)
			{
				switch (site.site_type)
				{
					case Site.SITE_SCROLL:
					case Site.SITE_GOLD_COIN:
						Location location = Sys.Instance.World.get_loc(site.map_x_loc, site.map_y_loc);

						//---- if the unit is standing on a scroll site -----//

						if (location.has_unit(UnitConstants.UNIT_LAND))
						{
							site.get_site_object(location.unit_recno(UnitConstants.UNIT_LAND));
						}
						else if (aiGetSiteObject)
						{
							site.ai_get_site_object();
						}

						break;
				}
			}
		}
	}
	
	public void generate_raw_site(int rawGenCount = 0)
	{
		if (rawGenCount > 0)
			std_raw_site_count = rawGenCount; // use this number to determine whether new sites should emerge in the future

		//----- count the no. of existing raw sites -------//

		int existRawSiteCount = 0;
		foreach (Site site in this)
		{
			if (site.site_type == Site.SITE_RAW && site.reserve_qty >= GameConstants.EXIST_RAW_RESERVE_QTY)
				existRawSiteCount++;
		}

		if (existRawSiteCount >= std_raw_site_count)
			return;

		//----- 100 attempts to create raw sites -------//

		for (int i = 0; i < 100; i++)
		{
			if (create_raw_site())
			{
				if (++existRawSiteCount == std_raw_site_count)
					return;
			}
		}
	}

	public bool create_raw_site(int townRecno = 0)
	{
		//-------- count the no. of each raw material -------//

		int[] rawCountArray = new int[GameConstants.MAX_RAW];

		foreach (Site site in this)
		{
			if (site.site_type == Site.SITE_RAW)
				rawCountArray[site.object_id - 1]++;
		}

		//---- find the minimum raw count ----//

		int minCount = Int32.MaxValue;

		for (int i = 0; i < GameConstants.MAX_RAW; i++)
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

		if (townRecno != 0)
		{
			const int MAX_TOWN_SITE_DISTANCE = 10;

			Town town = TownArray[townRecno];

			locX1 = town.LocCenterX - MAX_TOWN_SITE_DISTANCE;
			locX2 = town.LocCenterX + MAX_TOWN_SITE_DISTANCE;
			locY1 = town.LocCenterY - MAX_TOWN_SITE_DISTANCE;
			locY2 = town.LocCenterY + MAX_TOWN_SITE_DISTANCE;

			if (locX1 < 0)
				locX1 = 0;
			else if (locX2 >= GameConstants.MapSize)
				locX2 = GameConstants.MapSize - 1;

			if (locY1 < 0)
				locY1 = 0;
			else if (locY2 >= GameConstants.MapSize)
				locY2 = GameConstants.MapSize - 1;

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

		// 5,5 are the size of the raw site, it must be large enough for a mine to build and 1 location for the edges
		if (Sys.Instance.World.locate_space_random(ref locX1, ref locY1, locX2, locY2,
			    5, 5, maxTries, regionId, true))
		{
			RegionInfo regionInfo = RegionArray.GetRegionInfo(World.get_region_id(locX1, locY1));
			if (regionInfo.region_size < SMALLEST_RAW_REGION)
				return false;
			
			int reserveQty = GameConstants.MAX_RAW_RESERVE_QTY * (50 + Misc.Random(50)) / 100;

			// xLoc+1 & yLoc+1 as the located size is 3x3, the raw site is at the center of it
			AddSite(locX1 + 2, locY1 + 2, Site.SITE_RAW, rawId, reserveQty);

			return true;
		}
		else
		{
			return false;
		}
	}

	public int scan_site(int xLoc, int yLoc, int siteType = 0)
	{
		int minDis = Int32.MaxValue, nearestSiteRecno = 0;

		foreach (Site site in this)
		{
			if (siteType == 0 || site.site_type == siteType)
			{
				int siteDis = Misc.points_distance(xLoc, yLoc, site.map_x_loc, site.map_y_loc);

				if (siteDis < minDis)
				{
					minDis = siteDis;
					nearestSiteRecno = site.site_recno;
				}
			}
		}

		return nearestSiteRecno;
	}

	public void ai_get_site_object()
	{
		foreach (Site site in this)
		{
			if (site.site_type == Site.SITE_SCROLL || site.site_type == Site.SITE_GOLD_COIN)
			{
				site.ai_get_site_object();
			}
		}
	}

	public void disp_next(int seekDir, bool sameNation)
	{
		if (selected_recno == 0)
			return;

		int siteType = this[selected_recno].site_type;
		var enumerator = (seekDir >= 0) ? EnumerateAll(selected_recno, true) : EnumerateAll(selected_recno, false);

		foreach (int recNo in enumerator)
		{
			Site site = this[recNo];

			//--- check if the location of this site has been explored ---//

			if (!World.get_loc(site.map_x_loc, site.map_y_loc).explored())
				continue;

			//---------------------------------//

			if (site.site_type == siteType)
			{
				Power.reset_selection();
				selected_recno = site.site_recno;

				World.go_loc(site.map_x_loc, site.map_y_loc);
				return;
			}
		}
	}
}