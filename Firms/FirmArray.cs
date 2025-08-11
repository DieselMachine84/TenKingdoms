using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class FirmArray : DynArray<Firm>
{
	public int selected_recno;

	private FirmRes FirmRes => Sys.Instance.FirmRes;
	private SERes SERes => Sys.Instance.SERes;
	private Info Info => Sys.Instance.Info;
	private Power Power => Sys.Instance.Power;
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

	public int BuildFirm(int xLoc, int yLoc, int nationRecno, int firmId, string buildCode = "", int builderRecno = 0)
	{
		if (World.CanBuildFirm(xLoc, yLoc, firmId) == 0)
			return 0;

		//--------- check if there is enough cash ----------//

		if (nationRecno != 0)
		{
			if (NationArray[nationRecno].cash < FirmRes[firmId].setup_cost)
			{
				return 0;
			}
		}

		//---------- create and build the firm -------------//

		Firm firm = CreateNew(firmId);
		firm.Init(nationRecno, firmId, xLoc, yLoc, buildCode, builderRecno);

		//------ pay the land cost to the nation that owns the land ------//

		if (nationRecno != 0)
		{
			NationArray[nationRecno].add_expense(NationBase.EXPENSE_FIRM, FirmRes[firmId].setup_cost);
		}

		return firm.FirmId;
	}

	public void DeleteFirm(Firm firm)
	{
		firm.Deinit(); // we must call deinit() first
		Delete(firm.FirmId);
	}

	public void DeleteFirm(int recno)
	{
		if (IsDeleted(recno))
			return;
		
		DeleteFirm(this[recno]);
	}

	public void Process()
	{
		List<Firm> firmsToDelete = new List<Firm>();

		//----- each time process some firm only ------//
		foreach (Firm firm in this)
		{
			//-------- process visibility -----------//

			if (firm.NationId == NationArray.player_recno ||
			    (firm.NationId != 0 && NationArray[firm.NationId].is_allied_with_player))
			{
				World.Visit(firm.LocX1, firm.LocY1, firm.LocX2, firm.LocY2,
					GameConstants.EXPLORE_RANGE - 1);
			}

			//--------- process and process_ai firms ----------//

			if (firm.UnderConstruction)
			{
				firm.ProcessConstruction();
			}
			else
			{
				// only process each firm once per day
				if (firm.FirmId % InternalConstants.FRAMES_PER_DAY ==
				    Sys.Instance.FrameNumber % InternalConstants.FRAMES_PER_DAY)
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

						if (IsDeleted(firm.FirmId)) // the firm may have been deleted in process_ai()
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
	
	public void disp_next(int seekDir, bool sameNation)
	{
		if (selected_recno == 0)
			return;

		int nationRecno = this[selected_recno].NationId;
		var enumerator = (seekDir >= 0) ? EnumerateAll(selected_recno, true) : EnumerateAll(selected_recno, false);

		foreach (int recNo in enumerator)
		{
			Firm firm = this[recNo];

			//-------- if are of the same nation --------//

			if (sameNation && firm.NationId != nationRecno)
				continue;

			//--- check if the location of this town has been explored ---//

			if (!World.GetLoc(firm.LocCenterX, firm.LocCenterY).IsExplored())
				continue;

			//---------------------------------//

			Power.reset_selection();
			selected_recno = firm.FirmId;
			firm.SortWorkers();

			World.GoToLocation(firm.LocCenterX, firm.LocCenterY);
			return;
		}
	}
}