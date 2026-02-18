using System;
using System.IO;

namespace TenKingdoms;

public class FirmArray : DynArray<Firm>
{
	private SERes SERes => Sys.Instance.SERes;
	private Info Info => Sys.Instance.Info;
	private World World => Sys.Instance.World;
	private FirmRes FirmRes => Sys.Instance.FirmRes;
	private MonsterRes MonsterRes => Sys.Instance.MonsterRes;
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

		if (nationId != 0 && NationArray[nationId].Cash < FirmRes[firmType].SetupCost)
			return 0;

		Firm firm = CreateNew(firmType);
		firm.Init(nationId, firmType, locX, locY, buildCode, builderId);

		if (nationId != 0)
			NationArray[nationId].AddExpense(NationBase.EXPENSE_FIRM, FirmRes[firmType].SetupCost);

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
		var dayFrameNumber = Sys.Instance.FrameNumber % InternalConstants.FRAMES_PER_DAY;

		foreach (Firm firm in this)
		{
			//-------- process visibility -----------//

			if (firm.NationId == NationArray.PlayerId || (firm.NationId != 0 && NationArray[firm.NationId].IsAlliedWithPlayer))
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
						SERes.sound(firm.LocCenterX, firm.LocCenterY, 1, 'F', firm.FirmType, "DEST");
						DeleteFirm(firm);
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
		var enumerator = EnumerateAll(currentFirmId, seekDir >= 0);

		foreach (int firmId in enumerator)
		{
			Firm firm = this[firmId];
			
			if (firm.FirmType != currentFirm.FirmType)
				continue;

			if (sameNation && firm.NationId != currentFirm.NationId)
				continue;

			if (!World.GetLoc(firm.LocCenterX, firm.LocCenterY).IsExplored())
				continue;

			return firmId;
		}

		return currentFirmId;
	}
	
	public int BuildMonsterLair(int locX, int locY, int monsterId)
	{
		//----- if this monster has a home building, create it first -----//

		int firmId = BuildFirm(locX, locY, 0, Firm.FIRM_MONSTER, MonsterRes[monsterId].FirmBuildCode);

		if (firmId == 0)
			return 0;

		FirmMonster firmMonster = (FirmMonster)this[firmId];
		firmMonster.CompleteConstruction();
		firmMonster.MonsterId = monsterId;
		firmMonster.SetKing(monsterId, 100);

		//-------- create monster generals ---------//

		int generalCount = Misc.Random(2) + 1; // 1 to 3 generals in a monster firm

		if (Misc.Random(5) == 0) // 20% chance of having 3 generals.
			generalCount = 3;

		for (int i = 0; i < generalCount; i++)
			firmMonster.RecruitGeneral();

		return firmId;
	}
	
	public void StopAttackNation(int nationId)
	{
		foreach (Firm firm in this)
		{
			if (firm.FirmType != Firm.FIRM_MONSTER)
				continue;

			FirmMonster firmMonster = (FirmMonster)firm;
			firmMonster.ResetHostileNation(nationId);
		}
	}
	
	#region SaveAndLoad

	public void SaveTo(BinaryWriter writer)
	{
		writer.Write(NextId);
		int count = Count();
		writer.Write(count);
		foreach (Firm firm in EnumerateWithDeleted())
		{
			writer.Write(firm.FirmType);
			firm.SaveTo(writer);
		}
		writer.Write(Firm.ActionSpyId);
		writer.Write(Firm.BribeResult);
		writer.Write(Firm.AssassinateResult);
	}

	public void LoadFrom(BinaryReader reader)
	{
		NextId = reader.ReadInt32();
		int count = reader.ReadInt32();
		for (int i = 0; i < count; i++)
		{
			int firmType = reader.ReadInt32();
			Firm firm = CreateNewObject(firmType);
			firm.LoadFrom(reader);
			Load(firm);
		}
		Firm.ActionSpyId = reader.ReadInt32();
		Firm.BribeResult = reader.ReadInt32();
		Firm.AssassinateResult = reader.ReadInt32();
	}
	
	#endregion
}