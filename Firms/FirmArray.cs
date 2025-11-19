using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class FirmArray : DynArray<Firm>
{
	private FirmRes FirmRes => Sys.Instance.FirmRes;
	private SERes SERes => Sys.Instance.SERes;
	private Info Info => Sys.Instance.Info;
	private World World => Sys.Instance.World;
	private NationArray NationArray => Sys.Instance.NationArray;

	public FirmArray()
	{
	}

	protected override Firm CreateNewObject(int objectType)
	{
		switch (objectType)
		{
			case Firm.FIRM_BASE:
				return new FirmBase();

			case Firm.FIRM_CAMP:
				return new FirmCamp();

			case Firm.FIRM_FACTORY:
				return new FirmFactory();

			case Firm.FIRM_INN:
				return new FirmInn();

			case Firm.FIRM_MARKET:
				return new FirmMarket();

			case Firm.FIRM_MINE:
				return new FirmMine();

			case Firm.FIRM_RESEARCH:
				return new FirmResearch();

			case Firm.FIRM_WAR_FACTORY:
				return new FirmWar();

			case Firm.FIRM_HARBOR:
				return new FirmHarbor();

			case Firm.FIRM_MONSTER:
				return new FirmMonster();
		}

		throw new NotSupportedException();
	}

	public int BuildFirm(int locX, int locY, int nationId, int firmType, string buildCode = "", int builderId = 0)
	{
		if (World.CanBuildFirm(locX, locY, firmType) == 0)
			return 0;

		if (nationId != 0 && NationArray[nationId].cash < FirmRes[firmType].SetupCost)
			return 0;

		Firm firm = CreateNew(firmType);
		firm.Init(nationId, firmType, locX, locY, buildCode, builderId);

		if (nationId != 0)
			NationArray[nationId].add_expense(NationBase.EXPENSE_FIRM, FirmRes[firmType].SetupCost);

		return firm.FirmId;
	}

	public void DeleteFirm(Firm firm)
	{
		firm.Deinit();
		Delete(firm.FirmId);
	}

	public void DeleteFirm(int firmId)
	{
		if (IsDeleted(firmId))
			return;
		
		DeleteFirm(this[firmId]);
	}

	public void Process()
	{
		List<Firm> firmsToDelete = new List<Firm>();
		var dayFrameNumber = Sys.Instance.FrameNumber % InternalConstants.FRAMES_PER_DAY;

		foreach (Firm firm in this)
		{
			//-------- process visibility -----------//

			if (firm.NationId == NationArray.player_recno || (firm.NationId != 0 && NationArray[firm.NationId].is_allied_with_player))
			{
				World.Visit(firm.LocX1, firm.LocY1, firm.LocX2, firm.LocY2, GameConstants.EXPLORE_RANGE - 1);
			}

			//--------- process and process AI firms ----------//

			if (firm.UnderConstruction)
			{
				firm.ProcessConstruction();
			}
			else
			{
				// only process each firm once per day
				if (firm.FirmId % InternalConstants.FRAMES_PER_DAY == dayFrameNumber)
				{
					firm.NextDay();

					//-- if the hit points drop to zero, the firm should be deleted --//

					if (firm.HitPoints <= 0.0)
					{
						firmsToDelete.Add(firm);
						continue;
					}

					//--------- process AI ------------//

					if (firm.AIFirm)
					{
						firm.ProcessCommonAI();

						firm.ProcessAI();

						if (IsDeleted(firm.FirmId)) // the firm may have been deleted in ProcessAI()
							continue;
					}

					//--- think about having other nations capturing this firm ----//

					// this is not limited to ai firms only, it is called on all firms as AI can capture other player's firm
					if (Info.TotalDays % 60 == firm.FirmId % 60)
						firm.ThinkCapture();
				}
			}

			firm.ProcessAnimation();
		}

		foreach (Firm firm in firmsToDelete)
		{
			SERes.sound(firm.LocCenterX, firm.LocCenterY, 1, 'F', firm.FirmType, "DEST");
			DeleteFirm(firm);
		}
	}

	public void NextMonth()
	{
		foreach (Firm firm in this)
		{
			if (!firm.UnderConstruction)
			{
				firm.NextMonth();
			}
		}
	}

	public void NextYear()
	{
		foreach (Firm firm in this)
		{
			if (!firm.UnderConstruction)
			{
				firm.NextYear();
			}
		}
	}
	
	public int GetNextFirm(int currentFirmId, int seekDir, bool sameNation)
	{
		Firm currentFirm = this[currentFirmId];
		var enumerator = (seekDir >= 0) ? EnumerateAll(currentFirmId, true) : EnumerateAll(currentFirmId, false);

		foreach (int firmId in enumerator)
		{
			Firm firm = this[firmId];
			
			if (firm.FirmType != currentFirm.FirmType)
				continue;

			if (sameNation && firm.NationId != currentFirm.NationId)
				continue;

			if (!World.GetLoc(firm.LocCenterX, firm.LocCenterY).IsExplored())
				continue;

			firm.SortWorkers();
			return firmId;
		}

		return currentFirmId;
	}
}