using System;
using System.Collections.Generic;

namespace TenKingdoms;

public abstract class Firm : IIdObject
{
	public const int MAX_FIRM_TYPE = 10;
	public const int FIRM_BASE = 1;
	public const int FIRM_FACTORY = 2;
	public const int FIRM_INN = 3;
	public const int FIRM_MARKET = 4;
	public const int FIRM_CAMP = 5;
	public const int FIRM_MINE = 6;
	public const int FIRM_RESEARCH = 7;
	public const int FIRM_WAR_FACTORY = 8;
	public const int FIRM_HARBOR = 9;
	public const int FIRM_MONSTER = 10;

	public const int MAX_WORKER = 8;
	public const int MAX_CARGO = 9;

	public const int FIRM_WITHOUT_ACTION = 0;
	public const int FACTORY_RELOCATE = 1;
	public const int MARKET_FOR_SELL = 2;
	public const int CAMP_IN_DEFENSE = 3;


	public int FirmType { get; private set; }
	public int FirmBuildId { get; private set; }
	public int FirmId { get; private set; }
	public int NationId { get; private set; }
	public int RaceId { get; private set; }
	protected DateTime SetupDate { get; private set; }

	public int LocX1 { get; private set; }
	public int LocY1 { get; private set; }
	public int LocX2 { get; private set; }
	public int LocY2 { get; private set; }
	public int LocCenterX { get; private set; }
	public int LocCenterY { get; private set; }
	public int RegionId { get; protected set; }
	
	public int ClosestTownNameId { get; private set; }
	private int FirmNameInstanceId { get; set; }
	public int FirmSkillId { get; protected set; }
	public int OverseerId { get; protected set; }
	public int OverseerTownId { get; private set; }
	public int BuilderId { get; set; }
	private int BuilderRegionId { get; set; }
	public double HitPoints { get; set; }
	public double MaxHitPoints { get; private set; }
	public double Productivity { get; private set; }
	public bool UnderConstruction { get; set; }
	public double LastYearIncome { get; set; }
	public double CurYearIncome { get; set; }
	public bool NoNeighborSpace { get; protected set; }
	public DateTime LastAttackedDate { get; private set; }
	public List<Worker> Workers { get; } = new List<Worker>();
	public int SelectedWorkerId { get; set; }
	public bool ShouldSetPower { get; private set; }
	public int PlayerSpyCount { get; set; }
	public int SabotageLevel { get; set; } // 0-100 for counter productivity
	private bool IsDeleting { get; set; }
	public int CurFrame { get; private set; }
	private int RemainFrameDelay { get; set; }
	
	
	public List<int> LinkedFirms { get; } = new List<int>();
	public List<int> LinkedTowns { get; } = new List<int>();
	public List<int> LinkedFirmsEnable { get; } = new List<int>();
	public List<int> LinkedTownsEnable { get; } = new List<int>();

	
	public bool AIFirm  { get; private set; } // whether Computer AI control this firm or not

	// some ai actions are processed once only in the processing day. To prevent multiple checking in the processing day
	public bool AIProcessed { get; set; }
	public int AIStatus { get; protected set; }

	// AI checks firms and towns location by links, disable checking by setting this parameter to true
	public bool AILinkChecked { get; set; }
	public bool ShouldCloseFlag { get; protected set; }
	public int AIShouldBuildFactoryCount { get; set; }


	// id of the spy that is doing the bribing or viewing secret reports of other nations
	public static int ActionSpyId { get; set; }
	public static int BribeResult { get; set; }
	public static int AssassinateResult { get; set; }
	

	protected TownRes TownRes => Sys.Instance.TownRes;
	protected FirmRes FirmRes => Sys.Instance.FirmRes;
	protected RaceRes RaceRes => Sys.Instance.RaceRes;
	protected SpriteRes SpriteRes => Sys.Instance.SpriteRes;
	protected TalkRes TalkRes => Sys.Instance.TalkRes;
	protected UnitRes UnitRes => Sys.Instance.UnitRes;
	protected SERes SERes => Sys.Instance.SERes;
	protected Config Config => Sys.Instance.Config;
	protected ConfigAdv ConfigAdv => Sys.Instance.ConfigAdv;
	protected Info Info => Sys.Instance.Info;
	protected World World => Sys.Instance.World;
	protected NationArray NationArray => Sys.Instance.NationArray;
	protected FirmArray FirmArray => Sys.Instance.FirmArray;
	protected FirmDieArray FirmDieArray => Sys.Instance.FirmDieArray;
	protected TownArray TownArray => Sys.Instance.TownArray;
	protected UnitArray UnitArray => Sys.Instance.UnitArray;
	protected RebelArray RebelArray => Sys.Instance.RebelArray;
	protected SpyArray SpyArray => Sys.Instance.SpyArray;
	protected NewsArray NewsArray => Sys.Instance.NewsArray;

	protected Firm()
	{
	}

	void IIdObject.SetId(int id)
	{
		FirmId = id;
	}

	public virtual void Init(int nationId, int firmType, int locX, int locY, string buildCode = "", int builderId = 0)
	{
		FirmType = firmType;
		FirmInfo firmInfo = FirmRes[firmType];

		if (!String.IsNullOrEmpty(buildCode))
			FirmBuildId = firmInfo.get_build_id(buildCode);
		else
			FirmBuildId = firmInfo.first_build_id;

		NationId = nationId;
		SetupDate = Info.game_date;

		FirmBuild firmBuild = FirmRes.get_build(FirmBuildId);

		RaceId = firmBuild.race_id;

		LocX1 = locX;
		LocY1 = locY;
		LocX2 = LocX1 + firmBuild.loc_width - 1;
		LocY2 = LocY1 + firmBuild.loc_height - 1;

		LocCenterX = (LocX1 + LocX2) / 2;
		LocCenterY = (LocY1 + LocY2) / 2;

		RegionId = World.GetRegionId(LocCenterX, LocCenterY);

		if (firmBuild.animate_full_size)
			CurFrame = 1;
		else
			CurFrame = 2; // start with the 2nd frame as the 1st frame is the common frame

		RemainFrameDelay = firmBuild.frame_delay(CurFrame);

		OverseerId = 0;
		HitPoints = 0.0;
		MaxHitPoints = firmInfo.max_hit_points;
		UnderConstruction = firmInfo.buildable;

		if (!UnderConstruction)
			HitPoints = MaxHitPoints;

		if (builderId != 0)
			SetBuilder(builderId);
		else
			BuilderId = 0;

		firmInfo.total_firm_count++;

		if (NationId != 0)
		{
			firmInfo.inc_nation_firm_count(NationId);
			Nation nation = NationArray[NationId];
			AIFirm = nation.is_ai();
			AIProcessed = true;
			nation.nation_firm_count++;
			nation.last_build_firm_date = Info.game_date;
		}
		else
		{
			AIFirm = false;
			AIProcessed = false;
		}

		AIStatus = FIRM_WITHOUT_ACTION;
		AILinkChecked = true;

		SetupLink();

		SetWorldMatrix();

		InitName();

		if (AIFirm)
			NationArray[NationId].add_firm_info(FirmType, FirmId);

		InitDerived();
	}

	protected virtual void InitDerived()
	{
	}

	public virtual void Deinit()
	{
		DeinitDerived();

		IsDeleting = true;

		if (AIFirm)
		{
			Nation nation = NationArray[NationId];

			if (ShouldCloseFlag)
				nation.firm_should_close_array[FirmType - 1]--;

			nation.del_firm_info(FirmType, FirmId);
		}

		RestoreWorldMatrix();
		ReleaseLink();

		if (!UnderConstruction)
		{
			FirmDieArray.Add(this);
		}

		// this function must be called before restore_world_matrix(), otherwise the power area can't be completely reset
		AssignOverseer(0);

		ResignAllWorker(); // the workers in the firm will be killed if there is no space for creating the workers

		if (BuilderId != 0)
			MobilizeBuilder(BuilderId);

		FirmInfo firmInfo = FirmRes[FirmType];
		firmInfo.total_firm_count--;

		if (NationId != 0)
		{
			firmInfo.dec_nation_firm_count(NationId);
			NationArray[NationId].nation_firm_count--;
		}

		LocX1 = -1; // mark deleted
		LocY1 = -1;

		IsDeleting = false;
	}

	protected virtual void DeinitDerived()
	{
	}

	private void SetWorldMatrix()
	{
		for (int locY = LocY1; locY <= LocY2; locY++)
		{
			for (int locX = LocX1; locX <= LocX2; locX++)
			{
				World.GetLoc(locX, locY).SetFirm(FirmId);
			}
		}

		//--- if a nation set up a town in a location that the player has explored, contact between the nation and the player is established ---//

		EstablishContactWithPlayer();

		ShouldSetPower = GetShouldSetPower();

		//---- set this firm's influence on the map ----//

		if (ShouldSetPower)
			World.SetPower(LocX1, LocY1, LocX2, LocY2, NationId);

		//------------ reveal new land ----------//

		if (NationId == NationArray.player_recno || (NationId != 0 && NationArray[NationId].is_allied_with_player))
		{
			World.Unveil(LocX1, LocY1, LocX2, LocY2);
			World.Visit(LocX1, LocY1, LocX2, LocY2, GameConstants.EXPLORE_RANGE - 1);
		}
	}

	private void RestoreWorldMatrix()
	{
		for (int locY = LocY1; locY <= LocY2; locY++)
		{
			for (int locX = LocX1; locX <= LocX2; locX++)
			{
				World.GetLoc(locX, locY).RemoveFirm();
			}
		}

		//---- restore this firm's influence on the map ----//

		if (ShouldSetPower) // no power region for harbor as it build on coast which cannot be set with power region
			World.RestorePower(LocX1, LocY1, LocX2, LocY2, 0, FirmId);
	}
	
	private void EstablishContactWithPlayer()
	{
		if (NationId == 0)
			return;

		for (int locY = LocY1; locY <= LocY2; locY++)
		{
			for (int locX = LocX1; locX <= LocX2; locX++)
			{
				Location location = World.GetLoc(locX, locY);
				if (location.IsExplored() && NationArray.player_recno != 0)
				{
					NationRelation relation = NationArray.player.get_relation(NationId);

					//if (remote.is_enable())
					//{
						//if( !relation.has_contact && !relation.contact_msg_flag )
						//{
							//// packet structure : <player nation> <explored nation>
							//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_NATION_CONTACT, 2*sizeof(short));
							//*shortPtr = NationArray.player_recno;
							//shortPtr[1] = nation_recno;
							//relation.contact_msg_flag = 1;
						//}
					//}
					//else
					//{
						relation.has_contact = true;
					//}
				}
			}
		}
	}

	private void InitName()
	{
		// if this firm does not have any short name, display the full name without displaying the town name together
		if (String.IsNullOrEmpty(FirmRes[FirmType].short_name))
			return;

		ClosestTownNameId = GetClosestTownNameId();

		List<int> usedNumbers = new List<int>();
		foreach (Firm firm in FirmArray)
		{
			if (firm.FirmType == FirmType && firm.ClosestTownNameId == ClosestTownNameId && firm.FirmNameInstanceId != 0)
			{
				usedNumbers.Add(firm.FirmNameInstanceId);
			}
		}
		
		usedNumbers.Sort();

		int unusedNumber = usedNumbers.Count + 1;
		for (int i = 0; i < usedNumbers.Count; i++)
		{
			if (usedNumbers[i] != i + 1)
			{
				unusedNumber = i;
				break;
			}
		}

		FirmNameInstanceId = unusedNumber;
	}

	public virtual string FirmName()
	{
		string result = String.Empty;

		if (ClosestTownNameId == 0)
		{
			result = FirmRes[FirmType].name;
		}
		else
		{
			// display number when there are multiple linked firms of the same type
			// TRANSLATORS: <Town> <Short Firm Name> <Firm #>
			// This is the name of the firm when there are multiple linked firms to a town.
			result = TownRes.GetName(ClosestTownNameId) + " " + FirmRes[FirmType].short_name;
			if (FirmNameInstanceId > 1)
			{
				result += " " + FirmNameInstanceId;
			}
		}

		return result;
	}

	public virtual void NextDay()
	{
		if (NationId == 0)
			return;

		//------ think about updating link status -------//
		//
		// This part must be done here instead of in ProcessAI() because it will be too late to do it
		// in ProcessAI() as the NextDay() will call first and some wrong goods may be input to markets.
		//
		//-----------------------------------------------//

		if (AIFirm)
		{
			// once 30 days or when the link has been changed.
			if (Info.TotalDays % 30 == FirmId % 30 || !AILinkChecked)
			{
				AIUpdateLinkStatus();
				AILinkChecked = true;
			}
		}

		PayExpense();

		if (Info.TotalDays % 30 == FirmId % 30)
			UpdateLoyalty();

		if (!FirmRes[FirmType].live_in_town && Workers.Count > 0)
			ConsumeFood();

		if (Info.TotalDays % 30 == FirmId % 30)
			ThinkWorkerMigrate();

		ProcessRepair();

		if (Info.TotalDays % 30 == FirmId % 30)
			SpyArray.CatchSpy(Spy.SPY_FIRM, FirmId);

		if (FirmRes[FirmType].live_in_town)
		{
			ProcessIndependentTownWorker();
		}

		if (NoNeighborSpace && Info.TotalDays % 10 == FirmId % 10)
		{
			// use FIRM_RESEARCH because of its smallest size
			if (NationArray[NationId].find_best_firm_loc(FIRM_RESEARCH, LocX1, LocY1, out _, out _))
				NoNeighborSpace = false;
		}
	}

	public virtual void NextMonth()
	{
		bool newShouldSetPower = GetShouldSetPower();

		if (newShouldSetPower == ShouldSetPower)
			return;

		if (ShouldSetPower)
			World.RestorePower(LocX1, LocY1, LocX2, LocY2, 0, FirmId);

		ShouldSetPower = newShouldSetPower;

		if (ShouldSetPower)
			World.SetPower(LocX1, LocY1, LocX2, LocY2, NationId);
	}

	public virtual void NextYear()
	{
		LastYearIncome = CurYearIncome;
		CurYearIncome = 0.0;
	}

	public void ProcessConstruction()
	{
		if (FirmType == FIRM_MONSTER)
		{
			//--------- process construction for monster firm ----------//
			HitPoints++;

			if (HitPoints >= MaxHitPoints)
			{
				HitPoints = MaxHitPoints;
				UnderConstruction = false;
			}

			return;
		}

		if (!UnderConstruction)
			return;

		//--- can only do construction when the firm is not under attack ---//

		if (Info.game_date <= LastAttackedDate.AddDays(1.0))
			return;

		if (Sys.Instance.FrameNumber % 2 != 0) // one build every 2 frames
			return;

		//------ increase the construction progress ------//

		Unit unit = UnitArray[BuilderId];

		if (unit.Skill.SkillId == Skill.SKILL_CONSTRUCTION) // if builder unit has construction skill
			HitPoints += 1 + unit.Skill.SkillLevel / 30;
		else
			HitPoints++;

		if (Config.fast_build && NationId == NationArray.player_recno)
			HitPoints += 10;

		//----- increase skill level of the builder unit -----//

		if (unit.Skill.SkillId == Skill.SKILL_CONSTRUCTION) // if builder unit has construction skill
		{
			unit.Skill.SkillLevelMinor++;
			if (unit.Skill.SkillLevelMinor > 100)
			{
				unit.Skill.SkillLevelMinor = 0;

				if (unit.Skill.SkillLevel < 100)
					unit.Skill.SkillLevel++;
			}
		}

		//------- when the construction is complete ----------//

		if (HitPoints >= MaxHitPoints)
		{
			HitPoints = MaxHitPoints;
			UnderConstruction = false;

			bool needAssignUnit = false;

			if (NationId == NationArray.player_recno)
				SERes.far_sound(LocCenterX, LocCenterY, 1, 'S', unit.SpriteResId, "FINS", 'F', FirmType);

			FirmInfo firmInfo = FirmRes[FirmType];

			if ((firmInfo.need_overseer || firmInfo.need_worker) &&
			    (firmInfo.firm_skill_id == 0 || firmInfo.firm_skill_id == unit.Skill.SkillId)) // the builder with the skill required
			{
				unit.SetMode(0); // reset it from UNIT_MODE_CONSTRUCT
				needAssignUnit = true;
			}
			else
			{
				SetBuilder(0);
			}

			//---------------------------------------------------------------------------------------//
			// should call AssignUnit() first before calling ActionFinished(...UNDER_CONSTRUCTION)
			//---------------------------------------------------------------------------------------//

			if (needAssignUnit)
			{
				AssignUnit(BuilderId);

				//------------------------------------------------------------------------------//
				// Note: there may be chance the unit cannot be assigned into the firm
				//------------------------------------------------------------------------------//
				if (OverseerId == 0 && Workers.Count == 0) // no assignment, can't assign
				{
					//------- init_sprite or delete the builder ---------//
					int locX = LocX1, locY = LocY1;
					SpriteInfo spriteInfo = unit.SpriteInfo;
					if (!LocateSpace(IsDeleting, ref locX, ref locY, LocX2, LocY2, spriteInfo.LocWidth, spriteInfo.LocHeight))
						UnitArray.DisappearInFirm(BuilderId); // kill the unit
					else
						unit.InitSprite(locX, locY); // restore the unit
				}
			}

			BuilderId = 0;
		}
	}

	public void CompleteConstruction()
	{
		if (UnderConstruction)
		{
			HitPoints = MaxHitPoints;
			UnderConstruction = false;
		}
	}
	
	public void CancelConstruction(int remoteAction)
	{
		//if( !remoteAction && remote.is_enable())
		//{
			//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_FIRM_CANCEL, sizeof(short));
			//shortPtr[0] = firm_recno;
			//return;
		//}

		//------ get half of the construction cost back -------//

		Nation nation = NationArray[NationId];
		nation.add_expense(NationBase.EXPENSE_FIRM, -FirmRes[FirmType].setup_cost / 2.0);

		FirmArray.DeleteFirm(this);
	}
	
	private void ProcessRepair()
	{
		if (BuilderId == 0)
			return;

		if (NationArray[NationId].cash < 0) // if you don't have cash, the repair workers will not work
			return;

		Unit unit = UnitArray[BuilderId];

		//--- can only do repair when the firm is not under attack ---//

		if (Info.game_date <= LastAttackedDate.AddDays(1.0))
		{
			//---- if the construction worker is a spy, it will damage the building when the building is under attack ----//

			if (unit.SpyId != 0 && unit.TrueNationId() != NationId)
			{
				HitPoints -= SpyArray[unit.SpyId].SpySkill / 30.0;

				if (HitPoints < 0.0)
					HitPoints = 0.0;
			}

			return;
		}

		//------- repair now - only process once every 3 days -----//

		if (HitPoints >= MaxHitPoints)
			return;

		// repair once every 1 to 6 days, depending on the skill level of the construction worker
		int dayInterval = (100 - unit.Skill.SkillLevel) / 20 + 1;

		if (Info.TotalDays % dayInterval == FirmId % dayInterval)
		{
			HitPoints++;

			if (HitPoints > MaxHitPoints)
				HitPoints = MaxHitPoints;
		}
	}
	

	private bool GetShouldSetPower()
	{
		bool shouldSetPower = true;

		if (FirmType == FIRM_HARBOR) // don't set power for harbors
		{
			shouldSetPower = false;
		}
		else if (FirmType == FIRM_MARKET)
		{
			//--- don't set power for a market if it's linked to another nation's town ---//

			shouldSetPower = false;

			//--- only set the shouldSetPower to true if the market is linked to a firm of ours ---//

			for (int i = 0; i < LinkedTowns.Count; i++)
			{
				Town town = TownArray[LinkedTowns[i]];

				if (town.NationId == NationId)
				{
					shouldSetPower = true;
					break;
				}
			}
		}

		return shouldSetPower;
	}

	public int GetClosestTownNameId()
	{
		int minTownDistance = Int32.MaxValue;
		int closestTownNameId = 0;

		foreach (Town town in TownArray)
		{
			int townDistance = Misc.points_distance(town.LocCenterX, town.LocCenterY, LocCenterX, LocCenterY);

			if (townDistance < minTownDistance)
			{
				minTownDistance = townDistance;
				closestTownNameId = town.TownNameId;
			}
		}

		return closestTownNameId;
	}

	public bool OwnFirm() // whether the firm is controlled by the current player
	{
		return NationArray.player_recno != 0 && NationId == NationArray.player_recno;
	}

	public virtual bool ShouldShowInfo()
	{
		if (Config.show_ai_info || NationId == NationArray.player_recno || PlayerSpyCount > 0)
		{
			return true;
		}

		//------ if the builder is a spy of the player ------//

		if (BuilderId != 0 && UnitArray[BuilderId].TrueNationId() == NationArray.player_recno)
		{
			return true;
		}

		//----- if any of the workers belong to the player, show the info of this firm -----//

		if (HaveOwnWorkers(true))
			return true;

		//---- if there is a phoenix of the player over this firm ----//

		if (NationArray.player_recno != 0 && NationArray.player.revealed_by_phoenix(LocX1, LocY1))
			return true;

		return false;
	}

	public bool HaveOwnWorkers(bool checkSpy = false)
	{
		foreach (Worker worker in Workers)
		{
			if (worker.is_nation(FirmId, NationArray.player_recno, checkSpy))
				return true;
		}

		return false;
	}

	public virtual bool IsWorkerFull()
	{
		return Workers.Count == MAX_WORKER;
	}

	protected void CalcProductivity()
	{
		Productivity = 0.0;

		//------- calculate the productivity of the workers -----------//

		double totalSkill = 0.0;

		foreach (Worker worker in Workers)
		{
			totalSkill += Convert.ToDouble(worker.skill_level * worker.hit_points) / Convert.ToDouble(worker.max_hit_points());
		}

		//----- include skill in the calculation ------//

		Productivity = totalSkill / MAX_WORKER - SabotageLevel;

		if (Productivity < 0)
			Productivity = 0.0;
	}

	protected void AddIncome(int incomeType, double incomeAmount)
	{
		CurYearIncome += incomeAmount;

		NationArray[NationId].add_income(incomeType, incomeAmount, true);
	}

	public double Income365Days()
	{
		return LastYearIncome * (365 - Info.year_day) / 365 + CurYearIncome;
	}

	private void PayExpense()
	{
		if (NationId == 0)
			return;

		Nation nation = NationArray[NationId];

		//-------- fixed expenses ---------//

		double dayExpense = FirmRes[FirmType].year_cost / 365.0;

		if (nation.cash >= dayExpense)
		{
			nation.add_expense(NationBase.EXPENSE_FIRM, dayExpense, true);
		}
		else
		{
			if (HitPoints > 0.0)
				HitPoints--;

			if (HitPoints < 0.0)
				HitPoints = 0.0;

			//--- when the hit points drop to zero and the firm is destroyed ---//

			if (HitPoints <= 0.0 && NationId == NationArray.player_recno)
				NewsArray.firm_worn_out(FirmId);
		}

		//----- paying salary to workers from other nations -----//

		if (FirmRes[FirmType].live_in_town)
		{
			int payWorkerCount = 0;

			foreach (Worker worker in Workers)
			{
				int townNationId = TownArray[worker.town_recno].NationId;

				if (townNationId != NationId)
				{
					//--- if we don't have cash to pay the foreign workers, resign them ---//

					if (nation.cash < 0.0)
					{
						ResignWorker(worker);
					}
					else //----- pay salaries to the foreign workers now -----//
					{
						payWorkerCount++;

						if (townNationId != 0) // the nation of the worker will get income
							NationArray[townNationId].add_income(NationBase.INCOME_FOREIGN_WORKER,
								(double)GameConstants.WORKER_YEAR_SALARY / 365.0, true);
					}
				}
			}

			nation.add_expense(NationBase.EXPENSE_FOREIGN_WORKER,
				(double)GameConstants.WORKER_YEAR_SALARY * payWorkerCount / 365.0, true);
		}
	}

	public int YearExpense()
	{
		int totalExpense = FirmRes[FirmType].year_cost;

		//---- pay salary to workers from foreign towns ----//

		if (FirmRes[FirmType].live_in_town)
		{
			int payWorkerCount = 0;

			foreach (Worker worker in Workers)
			{
				if (TownArray[worker.town_recno].NationId != NationId)
					payWorkerCount++;
			}

			totalExpense += GameConstants.WORKER_YEAR_SALARY * payWorkerCount;
		}

		return totalExpense;
	}

	private void ConsumeFood()
	{
		// TODO bug. Only those workers who doesn't live in town should consume food
		if (NationArray[NationId].food > 0)
		{
			int humanUnitCount = 0;

			foreach (Worker worker in Workers)
			{
				if (worker.race_id != 0)
					humanUnitCount++;
			}

			NationArray[NationId].consume_food(Convert.ToDouble(humanUnitCount * GameConstants.PERSON_FOOD_YEAR_CONSUMPTION) / 365.0);
		}
		else //--- decrease loyalty if the food has been run out ---//
		{
			// decrease 1 loyalty point every 2 days
			if (Info.TotalDays % GameConstants.NO_FOOD_LOYALTY_DECREASE_INTERVAL == 0)
			{
				foreach (Worker worker in Workers)
				{
					if (worker.race_id != 0)
						worker.change_loyalty(-1);
				}
			}
		}
	}

	private void UpdateLoyalty()
	{
		if (FirmRes[FirmType].live_in_town)
			return;

		foreach (Worker worker in Workers)
		{
			int targetLoyalty = worker.target_loyalty(FirmId);

			if (targetLoyalty > worker.worker_loyalty)
			{
				int incValue = (targetLoyalty - worker.worker_loyalty) / 10;

				int newLoyalty = worker.worker_loyalty + Math.Max(1, incValue);

				if (newLoyalty > targetLoyalty)
					newLoyalty = targetLoyalty;

				worker.worker_loyalty = newLoyalty;
			}
			else if (targetLoyalty < worker.worker_loyalty)
			{
				worker.worker_loyalty--;
			}
		}
	}

	public int MajorityRace() // the race that has the majority of the population
	{
		//--- if there is an overseer, return the overseer's race ---//

		if (OverseerId != 0)
			return UnitArray[OverseerId].RaceId;

		if (Workers.Count == 0)
			return 0;

		//----- count the no. people in each race ------//

		int[] raceCountArray = new int[GameConstants.MAX_RACE];

		foreach (Worker worker in Workers)
		{
			if (worker.race_id != 0)
				raceCountArray[worker.race_id - 1]++;
		}

		//---------------------------------------------//

		int mostRaceCount = 0, mostRaceId = 0;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (raceCountArray[i] > mostRaceCount)
			{
				mostRaceCount = raceCountArray[i];
				mostRaceId = i + 1;
			}
		}

		return mostRaceId;
	}
	
	public bool CanSell()
	{
		return HitPoints >= MaxHitPoints * GameConstants.CAN_SELL_HIT_POINTS_PERCENT / 100.0;
	}

	public virtual bool IsOperating()
	{
		return Productivity > 0.0;
	}
	

	public virtual void AssignUnit(int unitId)
	{
		Unit unit = UnitArray[unitId];

		//------- if this is a construction worker -------//

		if (unit.Skill.SkillId == Skill.SKILL_CONSTRUCTION)
		{
			SetBuilder(unitId);
			return;
		}

		//-- if there isn't any overseer in this firm or this unit's skill is higher than the current overseer's skill --//

		FirmInfo firmInfo = FirmRes[FirmType];

		if (firmInfo.need_overseer &&
		    (OverseerId == 0 || (unit.Skill.SkillId == FirmSkillId && UnitArray[OverseerId].Skill.SkillId != FirmSkillId) ||
		     (unit.Skill.SkillId == FirmSkillId && unit.Skill.SkillLevel > UnitArray[OverseerId].Skill.SkillLevel)))
		{
			AssignOverseer(unitId);
		}
		else if (firmInfo.need_worker)
		{
			AssignWorker(unitId);
		}
	}

	public virtual void AssignOverseer(int newOverseerId)
	{
		if (!FirmRes[FirmType].need_overseer)
			return;

		if (newOverseerId == 0 && OverseerId == 0)
			return;

		//--- if the new overseer's nation is not the same as the firm's nation, don't assign ---//

		if (newOverseerId != 0 && UnitArray[newOverseerId].NationId != NationId)
			return;

		if (newOverseerId == 0)
		{
			//------------------------------------------------------------------------------------------------//
			// the old overseer may be kept in firm or killed if IsDeleting is true
			//------------------------------------------------------------------------------------------------//
			Unit oldUnit = UnitArray[OverseerId];
			SpriteInfo spriteInfo = SpriteRes[UnitRes[oldUnit.UnitType].sprite_id];
			int locX = LocX1;
			int locY = LocY1;

			if (!LocateSpace(IsDeleting, ref locX, ref locY, LocX2, LocY2, spriteInfo.LocWidth, spriteInfo.LocHeight))
			{
				if (IsDeleting)
					KillOverseer();
			}
			else
			{
				MobilizeOverseer();
			}
		}
		else
		{
			//----------------------------------------------------------------------------------------//
			// There should be at least one location (occupied by the new overseer) for creating the old overseer.
			//
			// 1) If a town is already created, the new overseer settles down there,
			//		free its space for creating the new overseer.
			// 2) If the overseer and the workers live in the firm, no town will be created.
			//		Thus, the space occupied by the old overseer is free for creating the new overseer.
			// 3) If the overseer and the workers need live in town, and a town is created.  i.e. there
			//		is no overseer or worker in the firm, so just assign the new overseer in the firm
			//----------------------------------------------------------------------------------------//

			Unit newOverseer = UnitArray[newOverseerId];

			if (FirmRes[FirmType].live_in_town)
			{
				// the overseer settles down
				OverseerTownId = AssignSettle(newOverseer.RaceId, newOverseer.Loyalty);
				if (OverseerTownId == 0)
					return; // no space for creating the town or reach MAX population, just return without assigning
			}

			newOverseer.DeinitSprite(); // hide the unit from the world map
			
			if (OverseerId != 0)
				MobilizeOverseer();
			
			OverseerId = newOverseerId;
			newOverseer.SetMode(UnitConstants.UNIT_MODE_OVERSEE, FirmId);

			//--------- if the unit is a spy -----------//

			if (newOverseer.SpyId != 0)
				SpyArray[newOverseer.SpyId].SetPlace(Spy.SPY_FIRM, FirmId);
		}

		if (newOverseerId != 0 && !UnitArray.IsDeleted(newOverseerId))
			UnitArray[newOverseerId].UpdateLoyalty();
	}

	protected virtual void AssignWorker(int workerUnitId)
	{
		//-- if the unit is a spy, only allow assign when there is room in the firm --//

		Unit unit = UnitArray[workerUnitId];

		if (unit.TrueNationId() != NationId && Workers.Count == MAX_WORKER)
			return;

		//---- if all worker space are full, resign the worst worker to release one worker space for the worker ----//

		int unitLocX = -1;
		int unitLocY = -1;

		if (Workers.Count == MAX_WORKER)
		{
			int minWorkerSkill = Int32.MaxValue;
			Worker worstWorker = Workers[0];

			foreach (Worker worker in Workers)
			{
				int workerSkill = worker.skill_level;

				if (workerSkill < minWorkerSkill)
				{
					minWorkerSkill = workerSkill;
					worstWorker = worker;
				}
			}

			unitLocX = unit.NextLocX;
			unitLocY = unit.NextLocY;

			unit.DeinitSprite(); // free the location for creating the worst unit

			ResignWorker(worstWorker);
		}

		//---------- there is room for the new worker ------------//

		Worker newWorker = new Worker();

		if (FirmRes[FirmType].live_in_town)
		{
			newWorker.town_recno = AssignSettle(unit.RaceId, unit.Loyalty); // the worker settles down

			if (newWorker.town_recno == 0)
			{
				//--- the unit was DeinitSprite(), and now the assign settle action failed, we need to InitSprite() to restore it ---//

				if (unitLocX >= 0 && !unit.IsVisible())
					unit.InitSprite(unitLocX, unitLocY);

				return;
			}
		}
		else
		{
			newWorker.town_recno = 0;
			newWorker.worker_loyalty = unit.Loyalty;
		}

		//------- add the worker to the firm -------//

		Workers.Add(newWorker);

		newWorker.name_id = unit.NameId;
		newWorker.race_id = unit.RaceId;
		newWorker.unit_id = unit.UnitType;
		newWorker.rank_id = unit.Rank;

		newWorker.skill_id = FirmSkillId;
		newWorker.skill_level = unit.Skill.GetSkillLevel(FirmSkillId);

		if (newWorker.skill_level == 0 && newWorker.race_id != 0)
			newWorker.skill_level = GameConstants.CITIZEN_SKILL_LEVEL;

		newWorker.combat_level = unit.Skill.CombatLevel;
		newWorker.hit_points = (int)unit.HitPoints;

		if (newWorker.hit_points == 0) // 0.? will become 0 in (double) to (int) conversion
			newWorker.hit_points = 1;

		if (UnitRes[unit.UnitType].unit_class == UnitConstants.UNIT_CLASS_WEAPON)
		{
			newWorker.extra_para = unit.WeaponVersion;
		}
		else if (unit.RaceId != 0)
		{
			newWorker.extra_para = unit.CurPower;
		}
		else
		{
			newWorker.extra_para = 0;
		}

		newWorker.init_potential();

		//------ if the recruited worker is a spy -----//

		if (unit.SpyId != 0)
		{
			SpyArray[unit.SpyId].SetPlace(Spy.SPY_FIRM, FirmId);

			newWorker.spy_recno = unit.SpyId;
			unit.SpyId = 0; // reset it now so Unit.Deinit() won't delete the Spy in SpyArray
		}
		
		SortWorkers();

		if (!FirmRes[FirmType].live_in_town) // if the unit does not live in town, increase the unit count now
			UnitRes[unit.UnitType].inc_nation_unit_count(NationId);

		UnitArray.DisappearInFirm(workerUnitId);
	}

	private int AssignSettle(int raceId, int unitLoyalty)
	{
		//--- if there is a town of our nation within the effective distance ---//

		int townId = FindSettleTown();

		if (townId != 0)
		{
			TownArray[townId].IncPopulation(raceId, true, unitLoyalty);
			return townId;
		}

		//--- should create a town near this firm, if there is no other town in the map ---//

		int locX = LocX1, locY = LocY1;

		// the town must be in the same region as this firm.
		if (World.LocateSpace(ref locX, ref locY, LocX2, LocY2,
			    InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, UnitConstants.UNIT_LAND, RegionId, true))
		{
			if (Misc.RectsDistance(locX, locY, locX + InternalConstants.TOWN_WIDTH - 1, locY + InternalConstants.TOWN_HEIGHT - 1,
				    LocX1, LocY1, LocX2, LocY2) <= InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE)
			{
				Town town = TownArray.AddTown(NationId, raceId, locX, locY);
				town.InitPopulation(raceId, 1, unitLoyalty, true, false);
				town.AutoSetLayout();
				return town.TownId;
			}
		}

		//---- not able to find a space for a new town within the effective distance ----//

		return 0;
	}

	private int FindSettleTown()
	{
		int minDistance = Int32.MaxValue;
		int nearestTownId = 0;

		//-------- scan for our own town first -----------//

		for (int i = 0; i < LinkedTowns.Count; i++)
		{
			Town town = TownArray[LinkedTowns[i]];

			if (town.Population >= GameConstants.MAX_TOWN_POPULATION)
				continue;

			if (town.NationId != NationId)
				continue;

			int townDistance = Misc.RectsDistance(town.LocX1, town.LocY1, town.LocX2, town.LocY2,
				LocX1, LocY1, LocX2, LocY2);

			if (townDistance < minDistance)
			{
				minDistance = townDistance;
				nearestTownId = town.TownId;
			}
		}

		return nearestTownId;
	}
	
	public void KillOverseer()
	{
		if (OverseerId == 0)
			return;

		Unit overseer = UnitArray[OverseerId];

		if (overseer.SpyId != 0)
			SpyArray[overseer.SpyId].SetPlace(Spy.SPY_UNDEFINED, 0);

		//-- no need to del the spy here, UnitArray.DeleteUnit() will del the spy --//

		if (OverseerTownId != 0)
			TownArray[OverseerTownId].DecPopulation(UnitArray[OverseerId].RaceId, true);

		UnitArray.DeleteUnit(overseer);

		OverseerId = 0;
	}

	public void KillWorker(Worker worker)
	{
		//------- decrease worker no. and create an unit -----//

		if (worker.race_id != 0 && worker.name_id != 0)
			RaceRes[worker.race_id].free_name_id(worker.name_id);

		if (worker.town_recno != 0)
			TownArray[worker.town_recno].DecPopulation(worker.race_id, true);

		//-------- if this worker is a spy ---------//

		if (worker.spy_recno != 0)
		{
			Spy spy = SpyArray[worker.spy_recno];
			spy.SetPlace(Spy.SPY_UNDEFINED, 0);
			SpyArray.DeleteSpy(spy);
		}

		//--- decrease the nation unit count as the Unit has already increased it ----//

		if (!FirmRes[FirmType].live_in_town)
			UnitRes[worker.unit_id].dec_nation_unit_count(NationId);

		Workers.Remove(worker);

		//TODO rewrite it
		//if( selected_worker_id > workerId || selected_worker_id == worker_count )
		//selected_worker_id--;

		if (Workers.Count == 0)
			SelectedWorkerId = 0;
	}


	private void SetupLink()
	{
		//-----------------------------------------------------------------------------//
		// check the connected firms location and structure if AILinkChecked is true
		//-----------------------------------------------------------------------------//

		if (AIFirm)
			AILinkChecked = false;


		FirmInfo firmInfo = FirmRes[FirmType];

		//----- build firm-to-firm link relationship -------//

		foreach (Firm firm in FirmArray)
		{
			if (firm.FirmId == FirmId)
				continue;

			//---- do not allow links between firms of different nation ----//

			if (firm.NationId != NationId)
				continue;

			//---------- check if the firm is close enough to this firm -------//

			if (!Misc.AreFirmsLinked(this, firm))
				continue;

			if (!firmInfo.is_linkable_to_firm(firm.FirmType))
				continue;

			//------- determine the default link status ------//

			// if the two firms are of the same nation, get the default link status which is based on the types of the firms
			// if the two firms are of different nations, default link status is both side disabled
			int defaultLinkStatus;
			if (firm.NationId == NationId)
				defaultLinkStatus = firmInfo.default_link_status(firm.FirmType);
			else
				defaultLinkStatus = InternalConstants.LINK_DD;

			//-------- add the link now -------//

			LinkedFirms.Add(firm.FirmId);
			LinkedFirmsEnable.Add(defaultLinkStatus);

			// now from the other firm's side
			if (defaultLinkStatus == InternalConstants.LINK_ED) // Reverse the link status for the opposite linker
				defaultLinkStatus = InternalConstants.LINK_DE;

			else if (defaultLinkStatus == InternalConstants.LINK_DE)
				defaultLinkStatus = InternalConstants.LINK_ED;

			firm.LinkedFirms.Add(FirmId);
			firm.LinkedFirmsEnable.Add(defaultLinkStatus);

			if (firm.AIFirm)
				firm.AILinkChecked = false;
		}

		//----- build firm-to-town link relationship -------//

		foreach (Town town in TownArray)
		{
			//------ check if the town is close enough to this firm -------//

			if (!Misc.AreTownAndFirmLinked(town, this))
				continue;

			if (!firmInfo.is_linkable_to_town)
				return;

			//------- determine the default link status ------//
			// if the two firms are of the same nation, get the default link status which is based on the types of the firms
			// if the two firms are of different nations, default link status is both side disabled
			int defaultLinkStatus;
			if (town.NationId == NationId)
				defaultLinkStatus = InternalConstants.LINK_EE;
			else
				defaultLinkStatus = InternalConstants.LINK_DD;

			//---------------------------------------------------//
			// If this is a camp, it can be linked to the town when either the town is an independent one
			// or the town is not linked to any camps of its own.
			//---------------------------------------------------//

			if (FirmType == FIRM_CAMP)
			{
				// TODO enable link only for enemy town?
				if (town.NationId == 0 || !town.HasLinkedOwnCamp)
					defaultLinkStatus = InternalConstants.LINK_EE;
			}

			//-------- add the link now -------//

			LinkedTowns.Add(town.TownId);
			LinkedTownsEnable.Add(defaultLinkStatus);

			// now from the town's side
			if (defaultLinkStatus == InternalConstants.LINK_ED) // Reverse the link status for the opposite linker
				defaultLinkStatus = InternalConstants.LINK_DE;

			else if (defaultLinkStatus == InternalConstants.LINK_DE)
				defaultLinkStatus = InternalConstants.LINK_ED;

			town.LinkedFirms.Add(FirmId);
			town.LinkedFirmsEnable.Add(defaultLinkStatus);

			if (town.AITown)
				town.AILinkChecked = false;
		}
	}

	private void ReleaseLink()
	{
		foreach (var linkedFirm in LinkedFirms)
		{
			FirmArray[linkedFirm].ReleaseFirmLink(FirmId);
		}

		foreach (var linkedTown in LinkedTowns)
		{
			TownArray[linkedTown].ReleaseFirmLink(FirmId);
		}
		
		LinkedFirms.Clear();
		LinkedFirmsEnable.Clear();
		LinkedTowns.Clear();
		LinkedTownsEnable.Clear();
	}

	private void ReleaseFirmLink(int releaseFirmId)
	{
		if (AIFirm)
			AILinkChecked = false;

		for (int i = LinkedFirms.Count - 1; i >= 0; i--)
		{
			if (LinkedFirms[i] == releaseFirmId)
			{
				LinkedFirms.RemoveAt(i);
				LinkedFirmsEnable.RemoveAt(i);
				return;
			}
		}
	}

	public void ReleaseTownLink(int releaseTownId)
	{
		if (AIFirm)
			AILinkChecked = false;

		for (int i = LinkedTowns.Count - 1; i >= 0; i--)
		{
			if (LinkedTowns[i] == releaseTownId)
			{
				LinkedTowns.RemoveAt(i);
				LinkedTownsEnable.RemoveAt(i);
				return;
			}
		}
	}

	public bool CanToggleFirmLink(int firmId)
	{
		Firm firm = FirmArray[firmId];

		//--- market to harbor link is determined by trade treaty ---//

		if ((FirmType == FIRM_MARKET && firm.FirmType == FIRM_HARBOR) || (FirmType == FIRM_HARBOR && firm.FirmType == FIRM_MARKET))
			return false;

		return FirmRes[FirmType].is_linkable_to_firm(firm.FirmType);
	}

	public void ToggleFirmLink(int linkId, bool toggleFlag, int remoteAction, bool setBoth = false)
	{
		//if( !remoteAction && remote.is_enable() )
		//{
			//// packet structure : <firm recno> <link Id> <toggle Flag>
			//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_FIRM_TOGGLE_LINK_FIRM, 3*sizeof(short));
			//shortPtr[0] = firm_recno;
			//shortPtr[1] = linkId;
			//shortPtr[2] = toggleFlag;
			//return;
		//}

		Firm linkedFirm = FirmArray[LinkedFirms[linkId - 1]];
		int linkedNationId = linkedFirm.NationId;

		// if one of the linked end is an independent firm/nation, consider this link as a single nation link
		bool sameNation = (linkedNationId == NationId || linkedNationId == 0 || NationId == 0);

		if (toggleFlag)
		{
			if ((sameNation && !setBoth) || setBoth)
				LinkedFirmsEnable[linkId - 1] = InternalConstants.LINK_EE;
			else
				LinkedFirmsEnable[linkId - 1] |= InternalConstants.LINK_ED;
		}
		else
		{
			if ((sameNation && !setBoth) || setBoth)
				LinkedFirmsEnable[linkId - 1] = InternalConstants.LINK_DD;
			else
				LinkedFirmsEnable[linkId - 1] &= ~InternalConstants.LINK_ED;
		}

		//------ set the linked flag of the opposite firm -----//

		for (int i = 0; i < linkedFirm.LinkedFirms.Count; i++)
		{
			if (linkedFirm.LinkedFirms[i] == FirmId)
			{
				if (toggleFlag)
				{
					if ((sameNation && !setBoth) || setBoth)
						linkedFirm.LinkedFirmsEnable[i] = InternalConstants.LINK_EE;
					else
						linkedFirm.LinkedFirmsEnable[i] |= InternalConstants.LINK_DE;
				}
				else
				{
					if ((sameNation && !setBoth) || setBoth)
						linkedFirm.LinkedFirmsEnable[i] = InternalConstants.LINK_DD;
					else
						linkedFirm.LinkedFirmsEnable[i] &= ~InternalConstants.LINK_DE;
				}

				break;
			}
		}
	}

	public bool CanToggleTownLink()
	{
		return FirmType != FIRM_MARKET; // only a market cannot toggle its link as it is
	}

	public void ToggleTownLink(int linkId, bool toggleFlag, int remoteAction, bool setBoth = false)
	{
		//if( !remoteAction && remote.is_enable() )
		//{
			//// packet structure : <firm recno> <link Id> <toggle Flag>
			//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_FIRM_TOGGLE_LINK_TOWN, 3*sizeof(short));
			//shortPtr[0] = firm_recno;
			//shortPtr[1] = linkId;
			//shortPtr[2] = toggleFlag;
			//return;
		//}

		Town linkedTown = TownArray[LinkedTowns[linkId - 1]];
		int linkedNationId = linkedTown.NationId;

		// if one of the linked end is an independent firm/nation, consider this link as a single nation link
		// town cannot decide whether it wants to link to Command Base or not, it is the Command Base which influences the town.
		bool sameNation = (linkedNationId == NationId || FirmType == FIRM_BASE);

		if (toggleFlag)
		{
			if ((sameNation && !setBoth) || setBoth)
				LinkedTownsEnable[linkId - 1] = InternalConstants.LINK_EE;
			else
				LinkedTownsEnable[linkId - 1] |= InternalConstants.LINK_ED;
		}
		else
		{
			if ((sameNation && !setBoth) || setBoth)
				LinkedTownsEnable[linkId - 1] = InternalConstants.LINK_DD;
			else
				LinkedTownsEnable[linkId - 1] &= ~InternalConstants.LINK_ED;
		}

		//------ set the linked flag of the opposite town -----//

		for (int i = 0; i < linkedTown.LinkedFirms.Count; i++)
		{
			if (linkedTown.LinkedFirms[i] == FirmId)
			{
				if (toggleFlag)
				{
					if ((sameNation && !setBoth) || setBoth)
						linkedTown.LinkedFirmsEnable[i] = InternalConstants.LINK_EE;
					else
						linkedTown.LinkedFirmsEnable[i] |= InternalConstants.LINK_DE;
				}
				else
				{
					if ((sameNation && !setBoth) || setBoth)
						linkedTown.LinkedFirmsEnable[i] = InternalConstants.LINK_DD;
					else
						linkedTown.LinkedFirmsEnable[i] &= ~InternalConstants.LINK_DE;
				}

				break;
			}
		}

		//-------- update the town's influence --------//

		if (linkedTown.NationId == 0)
			linkedTown.UpdateTargetResistance();

		//--- redistribute demand if a link to market place has been toggled ---//

		if (FirmType == FIRM_MARKET)
			TownArray.DistributeDemand();
	}
	
	
	public void MobilizeAllWorkers(int remoteAction)
	{
		//if (!remoteAction && remote.is_enable())
		//{
			//// packet strcture : <firm_recno>
			//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_FIRM_MOBL_ALL_WORKERS, sizeof(short) );
			//shortPtr[0] = firm_recno;
			//return;
		//}

		//------- detect buttons on hiring firm workers -------//

		int mobileWorkerId = 1;

		while (Workers.Count > 0 && mobileWorkerId <= Workers.Count)
		{
			Worker worker = Workers[mobileWorkerId - 1];

			if (!worker.is_nation(FirmId, NationId))
			{
				// prohibit mobilizing workers not under your color
				mobileWorkerId++;
				continue;
			}

			// always record 1 as the workers info are moved forward from the back to the front
			int unitRecno = MobilizeWorker(mobileWorkerId, InternalConstants.COMMAND_AUTO);

			if (unitRecno == 0)
				break; // keep the rest workers as there is no space for creating the unit

			Unit unit = UnitArray[unitRecno];
			unit.TeamId = UnitArray.CurTeamId;

			//TODO selection
			/*if (NationId == NationArray.player_recno)
			{
				unit.SelectedFlag = true;
				UnitArray.SelectedCount++;
				if (UnitArray.SelectedUnitId == 0)
					UnitArray.SelectedUnitId = unitRecno; // set first worker as selected
			}*/
		}

		UnitArray.CurTeamId++;
	}

	public virtual int MobilizeWorker(int workerId, int remoteAction)
	{
		Worker worker = Workers[workerId - 1];

		if (remoteAction <= InternalConstants.COMMAND_REMOTE && !worker.is_nation(FirmId, NationId))
		{
			// cannot order mobilization of foreign workers
			return 0;
		}

		//if(!remoteAction && remote.is_enable() )
		//{
		//// packet strcture : <firm_recno> <workerId>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_FIRM_MOBL_WORKER, 2*sizeof(short) );
		//shortPtr[0] = firm_recno;
		//shortPtr[1] = workerId;
		//return 0;
		//}

		//err_when( !worker_array );    // this function shouldn't be called if this firm does not need worker

		//------------- resign worker --------------//

		int oldWorkerCount = Workers.Count;

		int unitRecno2 = ResignWorker(worker);

		if (unitRecno2 == 0 && Workers.Count == oldWorkerCount)
			return 0;

		//------ create a mobile unit -------//

		int unitRecno = 0;

		// if does not live_in_town, resign_worker() create the unit already, so don't create it again here.
		if (FirmRes[FirmType].live_in_town)
		{
			//TODO check. It seems that resign_worker() also calls create_worker_unit()
			unitRecno = CreateWorkerUnit(worker);

			if (unitRecno == 0) // no space for creating units
				return 0;
		}

		//------------------------------------//

		SortWorkers();

		return unitRecno != 0 ? unitRecno : unitRecno2;
	}

	public virtual int MobilizeOverseer()
	{
		if (OverseerId == 0)
			return 0;

		//--------- restore overseer's harmony ---------//

		int overseerRecno = OverseerId;

		Unit overseer = UnitArray[OverseerId];

		//-------- if the overseer is a spy -------//

		if (overseer.SpyId != 0)
			SpyArray[overseer.SpyId].SetPlace(Spy.SPY_MOBILE, overseer.SpriteId);

		//---- cancel the overseer's presence in the town -----//

		if (FirmRes[FirmType].live_in_town)
			TownArray[OverseerTownId].DecPopulation(overseer.RaceId, true);

		//----- get this overseer out of the firm -----//

		SpriteInfo spriteInfo = SpriteRes[UnitRes[overseer.UnitType].sprite_id];
		int xLoc = LocX1, yLoc = LocY1; // xLoc & yLoc are used for returning results

		bool spaceFound = LocateSpace(IsDeleting, ref xLoc, ref yLoc, LocX2, LocY2,
			spriteInfo.LocWidth, spriteInfo.LocHeight);

		if (spaceFound)
		{
			overseer.InitSprite(xLoc, yLoc);
			overseer.SetMode(0); // reset overseen firm recno
		}
		else
		{
			UnitArray.DeleteUnit(overseer); // delete it when there is no space for the unit
			return 0;
		}

		//--------- reset overseer_recno -------------//

		OverseerId = 0;
		OverseerTownId = 0;

		//------- update loyalty -------//

		if (overseerRecno != 0 && !UnitArray.IsDeleted(overseerRecno))
			UnitArray[overseerRecno].UpdateLoyalty();

		return overseerRecno;
	}

	public bool MobilizeBuilder(int recno)
	{
		//----------- mobilize the builder -------------//
		Unit unit = UnitArray[recno];

		SpriteInfo spriteInfo = unit.SpriteInfo;
		int xLoc = LocX1, yLoc = LocY1;

		if (!LocateSpace(IsDeleting, ref xLoc, ref yLoc, LocX2, LocY2,
			    spriteInfo.LocWidth, spriteInfo.LocHeight, UnitConstants.UNIT_LAND, BuilderRegionId) &&
		    !World.LocateSpace(ref xLoc, ref yLoc, LocX2, LocY2,
			    spriteInfo.LocWidth, spriteInfo.LocHeight, UnitConstants.UNIT_LAND, BuilderRegionId))
		{
			UnitArray.DeleteUnit(UnitArray[recno]);
			return false;
		}

		unit.InitSprite(xLoc, yLoc);
		unit.Stop2(); // clear all previously defined action
		unit.SetMode(0);

		//--- set builder to non-aggressive, except ai ---//
		if (!ConfigAdv.firm_mobilize_civilian_aggressive && !unit.AIUnit)
			unit.AggressiveMode = false;

		return true;
	}

	public void ResignAllWorker(bool disappearFlag = false)
	{
		//------- detect buttons on hiring firm workers -------//

		while (Workers.Count > 0)
		{
			Worker worker = Workers[0];
			int townRecno = worker.town_recno;
			int raceId = worker.race_id;
			int oldWorkerCount = Workers.Count;

			if (ResignWorker(worker) == 0)
			{
				if (oldWorkerCount == Workers.Count)
					break; // no space to resign the worker, keep them in firm
			}

			if (disappearFlag && townRecno != 0)
				TownArray[townRecno].DecPopulation(raceId, false);
		}
	}

	public virtual int ResignWorker(Worker worker)
	{
		//------- decrease worker no. and create an unit -----//
		int unitRecno = 0;

		if (worker.race_id != 0 && worker.name_id != 0)
			RaceRes[worker.race_id].free_name_id(worker.name_id);

		if (worker.town_recno != 0) // town_recno is 0 if the workers in the firm do not live in towns
		{
			Town town = TownArray[worker.town_recno];

			town.RacesJoblessPopulation[worker.race_id - 1]++; // move into jobless population
			town.JoblessPopulation++;

			//------ put the spy in the town -------//

			if (worker.spy_recno != 0)
				SpyArray[worker.spy_recno].SetPlace(Spy.SPY_TOWN, worker.town_recno);
		}
		else
		{
			unitRecno = CreateWorkerUnit(worker); // if he is a spy, create_worker_unit wil call set_place(SPY_MOBILE)

			if (unitRecno == 0)
				return 0; // return 0 eg there is no space to create the unit
		}

		//------- delete the record from the worker_array ------//

		Workers.Remove(worker);

		//TODO rewrite it
		//if (selected_worker_id > workerId || selected_worker_id == worker_count)
		//selected_worker_id--;

		return unitRecno;
	}

	public int CreateWorkerUnit(Worker worker)
	{
		//------------ create a unit --------------//

		// this worker no longer has a job as it has been resigned
		int unitRecno = CreateUnit(worker.unit_id, worker.town_recno, false);

		if (unitRecno == 0)
			return 0;

		Unit unit = UnitArray[unitRecno];
		unit.InitFromWorker(worker);

		//--- decrease the nation unit count as the Unit has already increased it ----//

		if (!FirmRes[FirmType].live_in_town) // if the unit does not live in town, increase the unit count now
			UnitRes[unit.UnitType].dec_nation_unit_count(NationId);

		//--- set non-military units to non-aggressive, except ai ---//
		if (!ConfigAdv.firm_mobilize_civilian_aggressive && unit.RaceId > 0 && unit.Skill.SkillId != Skill.SKILL_LEADING && !unit.AIUnit)
			unit.AggressiveMode = false;

		return unitRecno;
	}

	protected int CreateUnit(int unitId, int townRecno = 0, bool unitHasJob = false)
	{
		//----look for an empty location for the unit to stand ----//
		//--- scan for the 5 rows right below the building ---//

		SpriteInfo spriteInfo = SpriteRes[UnitRes[unitId].sprite_id];
		int xLoc = LocX1, yLoc = LocY1;

		if (!LocateSpace(IsDeleting, ref xLoc, ref yLoc, LocX2, LocY2,
			    spriteInfo.LocWidth, spriteInfo.LocHeight))
			return 0;

		//------------ add the unit now ----------------//

		int unitNationRecno = townRecno != 0 ? TownArray[townRecno].NationId : NationId;

		Unit unit = UnitArray.AddUnit(unitId, unitNationRecno, Unit.RANK_SOLDIER, 0, xLoc, yLoc);

		//----- update the population of the town ------//

		if (townRecno != 0)
			TownArray[townRecno].DecPopulation(unit.RaceId, unitHasJob);

		return unit.SpriteId;
	}
	
	public void SortWorkers()
	{
		//TODO this function is for UI
	}

	protected void RecruitWorker()
	{
		if (Workers.Count == MAX_WORKER)
			return;

		if (Info.TotalDays % 5 != FirmId % 5) // update population once 10 days
			return;

		//-------- pull from neighbor towns --------//

		Nation nation = NationArray[NationId];

		for (int i = 0; i < LinkedTowns.Count; i++)
		{
			if (LinkedTownsEnable[i] != InternalConstants.LINK_EE)
				continue;

			Town town = TownArray[LinkedTowns[i]];

			//--- don't hire foreign workers if we don't have cash to pay them ---//

			if (nation.cash < 0 && NationId != town.NationId)
				continue;

			//-------- if the town has any unit ready for jobs -------//

			if (town.JoblessPopulation == 0)
				continue;

			//---- if nation of the town is not hositle to this firm's nation ---//

			if (PullTownPeople(town.TownId, InternalConstants.COMMAND_AUTO))
				return;
		}
	}
	
	protected void UpdateWorker()
	{
		if (Info.TotalDays % 15 != FirmId % 15)
			return;

		if (Workers.Count == 0)
			return;

		//------- update the worker's para ---------//

		int incValue, levelMinor;

		foreach (Worker worker in Workers)
		{
			//------- increase worker skill -----------//

			if (IsOperating() && worker.skill_level < 100) // only train when the workers are working
			{
				incValue = Math.Max(10, 100 - worker.skill_level)
					* worker.hit_points / worker.max_hit_points()
					* (100 + worker.skill_potential) / 100 / 2;

				//-------- increase level minor now --------//

				// with random factors, resulting in 75% to 125% of the original
				levelMinor = worker.skill_level_minor + incValue * (75 + Misc.Random(50)) / 100;

				while (levelMinor >= 100)
				{
					levelMinor -= 100;
					worker.skill_level++;
				}

				worker.skill_level_minor = levelMinor;
			}

			//------- increase worker hit points --------//

			int maxHitPoints = worker.max_hit_points();

			if (worker.hit_points < maxHitPoints)
			{
				worker.hit_points += 2; // units in firms recover twice as fast as they are mobile

				if (worker.hit_points > maxHitPoints)
					worker.hit_points = maxHitPoints;
			}
		}

		SortWorkers();
	}

	protected void ThinkWorkerMigrate()
	{

		if (Workers.Count == 0 || !FirmRes[FirmType].live_in_town)
			return;

		foreach (Town town in TownArray.EnumerateRandom())
		{
			if (town.Population >= GameConstants.MAX_TOWN_POPULATION)
				continue;

			//------ check if this town is linked to the current firm -----//

			int j;
			for (j = town.LinkedFirms.Count - 1; j >= 0; j--)
			{
				if (town.LinkedFirms[j] == FirmId &&
				    town.LinkedFirmsEnable[j] != 0)
				{
					break;
				}
			}

			if (j < 0)
				continue;

			//------------------------------------------------//
			//
			// Calculate the attractive factor, it is based on:
			//
			// - the reputation of the target nation (+0 to 100)
			// - the racial harmony of the race in the target town (+0 to 100)
			// - the no. of people of the race in the target town
			// - distance between the current town and the target town (-0 to 100)
			//
			// Attractiveness level range: 0 to 200
			//
			//------------------------------------------------//

			int targetBaseAttractLevel = 0;

			if (town.NationId != 0)
				targetBaseAttractLevel += (int)NationArray[town.NationId].reputation;

			//---- scan all workers, see if any of them want to worker_migrate ----//

			int workerId = Misc.Random(Workers.Count) + 1;

			for (j = 0; j < Workers.Count; j++)
			{
				if (++workerId > Workers.Count)
					workerId = 1;

				Worker worker = Workers[workerId - 1];

				if (worker.town_recno == town.TownId)
					continue;

				int raceId = worker.race_id;
				Town workerTown = TownArray[worker.town_recno];

				//-- do not migrate if the target town's population of that race is less than half of the population of the current town --//

				if (town.RacesPopulation[raceId - 1] < workerTown.RacesPopulation[raceId - 1] / 2)
					continue;

				//-- do not migrate if the target town might not be a place this worker will stay --//

				if (ConfigAdv.firm_migrate_stricter_rules &&
				    town.RacesLoyalty[raceId - 1] < 40) // < 40 is considered as negative force
					continue;

				//------ calc the current and target attractiveness level ------//

				int curBaseAttractLevel;
				if (workerTown.NationId != 0)
					curBaseAttractLevel = (int)NationArray[workerTown.NationId].reputation;
				else
					curBaseAttractLevel = 0;

				int targetAttractLevel = targetBaseAttractLevel + town.RaceHarmony(raceId);

				if (targetAttractLevel < GameConstants.MIN_MIGRATE_ATTRACT_LEVEL)
					continue;

				// loyalty > 40 is considered as positive force, < 40 is considered as negative force
				int curAttractLevel = curBaseAttractLevel + workerTown.RaceHarmony(raceId) + (worker.loyalty() - 40);

				if (ConfigAdv.firm_migrate_stricter_rules
					    ? targetAttractLevel - curAttractLevel > GameConstants.MIN_MIGRATE_ATTRACT_LEVEL / 2
					    : targetAttractLevel > curAttractLevel)
				{
					int newLoyalty = Math.Max(GameConstants.REBEL_LOYALTY + 1, targetAttractLevel / 2);

					WorkerMigrate(workerId, town.TownId, newLoyalty);
					return;
				}
			}
		}
	}

	protected void WorkerMigrate(int workerId, int destTownRecno, int newLoyalty)
	{
		Worker worker = Workers[workerId - 1];

		int raceId = worker.race_id;
		Town srcTown = TownArray[worker.town_recno];
		Town destTown = TownArray[destTownRecno];

		//------------- add news --------------//

		if (srcTown.NationId == NationArray.player_recno || destTown.NationId == NationArray.player_recno)
		{
			if (srcTown.NationId != destTown.NationId) // don't add news for migrating between own towns 
				NewsArray.migrate(srcTown.TownId, destTownRecno, raceId, 1, FirmId);
		}

		//--------- migrate now ----------//

		worker.town_recno = destTownRecno;

		//--------- decrease the population of the home town ------//

		srcTown.DecPopulation(raceId, true);

		//--------- increase the population of the target town ------//

		destTown.IncPopulation(raceId, true, newLoyalty);
	}

	protected void ProcessIndependentTownWorker()
	{
		if (Info.TotalDays % 15 != FirmId % 15)
			return;

		foreach (Worker worker in Workers)
		{
			Town town = TownArray[worker.town_recno];

			if (town.NationId == 0) // if it's an independent town
			{
				town.RacesResistance[worker.race_id - 1, NationId - 1] -=
					GameConstants.RESISTANCE_DECREASE_PER_WORKER;

				if (town.RacesResistance[worker.race_id - 1, NationId - 1] < 0.0)
					town.RacesResistance[worker.race_id - 1, NationId - 1] = 0.0;
			}
		}
	}

	public virtual bool PullTownPeople(int townRecno, int remoteAction, int raceId = 0, bool forcePull = false)
	{
		// this can happen in a multiplayer game as Town::draw_detect_link_line() still have the old worker_count and thus allow this function being called.
		if (Workers.Count == MAX_WORKER)
			return false;

		//if(!remoteAction && remote.is_enable() )
		//{
		//// packet structure : <firm recno> <town recno> <race Id or 0> <force Pull>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_FIRM_PULL_TOWN_PEOPLE, 4*sizeof(short));
		//shortPtr[0] = firm_recno;
		//shortPtr[1] = townRecno;
		//shortPtr[2] = raceId;	
		//// if raceId == 0, let each player choose the race by random number, to sychronize the random number
		//shortPtr[3] = forcePull;
		//return 0;
		//}

		//---- people in the town go to work for the firm ---//

		Town town = TownArray[townRecno];
		int popAdded = 0;

		//---- if doesn't specific a race, randomly pick one ----//

		if (raceId == 0)
			raceId = Misc.Random(GameConstants.MAX_RACE) + 1;

		//----------- scan the races -----------//

		for (int i = 0; i < GameConstants.MAX_RACE; i++) // maximum 8 tries
		{
			//---- see if there is any population of this race to move to the firm ----//

			int recruitableCount = town.RecruitableRacePopulation(raceId, true); // 1-allow recruiting spies

			if (recruitableCount > 0)
			{
				//----- if the unit is forced to move to the firm ---//

				if (forcePull) // right-click to force pulling a worker from the village
				{
					if (town.RacesLoyalty[raceId - 1] <
					    GameConstants.MIN_RECRUIT_LOYALTY + town.RecruitDecLoyalty(raceId, false))
						return false;

					town.RecruitDecLoyalty(raceId);
				}
				else //--- see if the unit will voluntarily move to the firm ---//
				{
					//--- the higher the loyalty is, the higher the chance of working for the firm ---//

					if (town.NationId != 0)
					{
						if (Misc.Random((100 - Convert.ToInt32(town.RacesLoyalty[raceId - 1])) / 10) > 0)
							return false;
					}
					else
					{
						if (Misc.Random(Convert.ToInt32(town.RacesResistance[raceId - 1, NationId - 1]) / 10) > 0)
							return false;
					}
				}

				//----- get the chance of getting people to your command base is higher when the loyalty is higher ----//

				if (FirmRes[FirmType].live_in_town)
				{
					town.RacesJoblessPopulation[raceId - 1]--; // decrease the town's population
					town.JoblessPopulation--;
				}
				else
				{
					town.DecPopulation(raceId, false);
				}

				//------- add the worker to the firm -----//

				Worker worker = new Worker();
				Workers.Add(worker);

				worker.race_id = raceId;
				worker.rank_id = Unit.RANK_SOLDIER;
				worker.unit_id = RaceRes[raceId].basic_unit_id;
				worker.worker_loyalty = Convert.ToInt32(town.RacesLoyalty[raceId - 1]);

				if (FirmRes[FirmType].live_in_town)
					worker.town_recno = townRecno;

				worker.combat_level = GameConstants.CITIZEN_COMBAT_LEVEL;
				worker.hit_points = GameConstants.CITIZEN_HIT_POINTS;

				worker.skill_id = FirmSkillId;
				worker.skill_level = GameConstants.CITIZEN_SKILL_LEVEL;

				worker.init_potential();

				//--------- if this is a military camp ---------//
				//
				// Increase armed unit count of the race of the worker assigned,
				// as when a unit is assigned to a camp, Unit::deinit() will decrease
				// the counter, so we need to increase it back here.
				//
				//---------------------------------------------------//

				if (!FirmRes[FirmType].live_in_town)
					UnitRes[worker.unit_id].inc_nation_unit_count(NationId);

				//------ if the recruited worker is a spy -----//

				int spyCount = town.RacesSpyCount[raceId - 1];

				if (spyCount >= Misc.Random(recruitableCount) + 1)
				{
					// the 3rd parameter is which spy to recruit
					int spyRecno = SpyArray.FindTownSpy(townRecno, raceId, Misc.Random(spyCount) + 1);

					worker.spy_recno = spyRecno;

					SpyArray[spyRecno].SetPlace(Spy.SPY_FIRM, FirmId);
				}

				return true;
			}

			if (++raceId > GameConstants.MAX_RACE)
				raceId = 1;
		}

		return false;
	}
	
	public void SetWorkerHomeTown(int townRecno, char remoteAction, int workerId = 0)
	{
		if (workerId == 0)
			workerId = SelectedWorkerId;

		if (workerId == 0 || workerId > Workers.Count)
			return;

		Town town = TownArray[townRecno];
		Worker worker = Workers[workerId - 1];

		if (worker.town_recno != townRecno)
		{
			if (!worker.is_nation(FirmId, NationId))
				return;
			if (town.Population >= GameConstants.MAX_TOWN_POPULATION)
				return;
		}

		//if(!remoteAction && remote.is_enable() )
		//{
		//// packet structure : <firm recno> <town recno> <workderId>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_FIRM_SET_WORKER_HOME, 3*sizeof(short));
		//shortPtr[0] = firm_recno;
		//shortPtr[1] = townRecno;
		//shortPtr[2] = workerId;
		//return;
		//}

		//-------------------------------------------------//

		if (worker.town_recno == townRecno)
		{
			ResignWorker(worker);
		}

		//--- otherwise, set the worker's home town to the new one ---//

		else if (worker.is_nation(FirmId, NationId) &&
		         town.NationId ==
		         NationId) // only allow when the worker lives in a town belonging to the same nation and moving domestically
		{
			int workerLoyalty = worker.loyalty();

			TownArray[worker.town_recno].DecPopulation(worker.race_id, true);
			town.IncPopulation(worker.race_id, true, workerLoyalty);

			worker.town_recno = townRecno;
		}
	}
	

	public bool CanAssignCapture()
	{
		return OverseerId == 0 && Workers.Count == 0;
	}

	public bool CanWorkerCapture(int captureNationRecno)
	{
		if (captureNationRecno == 0) // neutral units cannot capture
			return false;

		if (NationId == captureNationRecno) // cannot capture its own firm
			return false;

		//----- if this firm needs an overseer, can only capture it when the overseer is the spy ---//

		if (FirmRes[FirmType].need_overseer)
		{
			return OverseerId != 0 && UnitArray[OverseerId].TrueNationId() == captureNationRecno;
		}

		//--- if this firm doesn't need an overseer, can capture it if all the units in the firm are the player's spies ---//

		int captureUnitCount = 0, otherUnitCount = 0;

		foreach (Worker worker in Workers)
		{
			if (worker.spy_recno != 0 && SpyArray[worker.spy_recno].TrueNationId == captureNationRecno)
			{
				captureUnitCount++;
			}
			else if (worker.town_recno != 0)
			{
				if (TownArray[worker.town_recno].NationId == captureNationRecno)
					captureUnitCount++;
				else
					otherUnitCount++;
			}
			else
			{
				otherUnitCount++; // must be an own unit in camps and bases if the unit is not a spy
			}
		}

		return captureUnitCount > 0 && otherUnitCount == 0;
	}

	public void CaptureFirm(int newNationRecno)
	{
		if (NationId == NationArray.player_recno)
			NewsArray.firm_captured(FirmId, newNationRecno, 0); // 0 - the capturer is not a spy

		//-------- if this is an AI firm --------//

		if (AIFirm)
			AIFirmCaptured(newNationRecno);

		//------------------------------------------//
		//
		// If there is an overseer in this firm, then the only
		// unit who can capture this firm will be the overseer only,
		// so calling its betray() function will capture the whole
		// firm already.
		//
		//------------------------------------------//

		if (OverseerId != 0 && UnitArray[OverseerId].SpyId != 0)
			UnitArray[OverseerId].SpyChangeNation(newNationRecno, InternalConstants.COMMAND_AUTO);
		else
			ChangeNation(newNationRecno);
	}

	public virtual void ChangeNation(int newNationId)
	{
		if (NationId == newNationId)
			return;

		UnitArray.StopAttackFirm(FirmId);
		RebelArray.StopAttackFirm(FirmId);

		Nation oldNation = NationArray[NationId];
		Nation newNation = NationArray[newNationId];

		if (BuilderId != 0)
		{
			Unit unit = UnitArray[BuilderId];
			unit.ChangeNation(newNationId);

			//--- if this is a spy, chance its cloak ----//

			if (unit.SpyId != 0)
				SpyArray[unit.SpyId].CloakedNationId = newNationId;
		}

		if (FirmType == FIRM_CAMP)
			((FirmCamp)this).clear_defense_mode(FirmId);

		FirmInfo firmInfo = FirmRes[FirmType];

		if (NationId != 0)
			firmInfo.dec_nation_firm_count(NationId);

		if (newNationId != 0)
			firmInfo.inc_nation_firm_count(newNationId);

		//---- reset should_close_flag -----//

		if (AIFirm)
		{
			if (ShouldCloseFlag)
			{
				oldNation.firm_should_close_array[FirmType - 1]--;
				ShouldCloseFlag = false;
			}
		}

		SpyArray.UpdateFirmSpyCount(FirmId);

		SpyArray.ChangeCloakedNation(Spy.SPY_FIRM, FirmId, NationId, newNationId);

		//-----------------------------------------//

		if (AIFirm)
			oldNation.del_firm_info(FirmType, FirmId);

		if (ShouldSetPower)
			World.RestorePower(LocX1, LocY1, LocX2, LocY2, 0, FirmId);

		ShouldSetPower = GetShouldSetPower();

		if (ShouldSetPower)
			World.SetPower(LocX1, LocY1, LocX2, LocY2, newNationId);

		//------------ update link --------------//

		ReleaseLink(); // need to update link because firms are only linked to firms of the same nation

		NationId = newNationId;

		SetupLink();

		//---------------------------------------//

		AIFirm = NationArray[NationId].is_ai();

		if (AIFirm)
			newNation.add_firm_info(FirmType, FirmId);

		EstablishContactWithPlayer();

		//---- reset the action mode of all spies in this firm ----//

		// we need to reset it. e.g. when we have captured an enemy firm, SPY_SOW_DISSENT action must be reset to SPY_IDLE
		SpyArray.SetActionMode(Spy.SPY_FIRM, FirmId, Spy.SPY_IDLE);
	}


	public bool SetBuilder(int newBuilderRecno)
	{
		//------------------------------------//

		int oldBuilderRecno = BuilderId; // store the old builder recno

		BuilderId = newBuilderRecno;

		//-------- assign the new builder ---------//

		if (BuilderId != 0)
		{
			Unit unit = UnitArray[BuilderId];
			if (unit.IsVisible()) // is visible if the unit is not inside the firm location
			{
				BuilderRegionId = World.GetRegionId(unit.CurLocX, unit.CurLocY);
				unit.DeinitSprite();

				//TODO selection
				/*if (unit.SelectedFlag)
				{
					unit.SelectedFlag = false;
					UnitArray.SelectedCount--;
				}*/
			}

			unit.SetMode(UnitConstants.UNIT_MODE_CONSTRUCT, FirmId);
		}

		if (oldBuilderRecno != 0)
			MobilizeBuilder(oldBuilderRecno);

		return true;
	}

	public int FindIdleBuilder(bool nearest)
	{
		int minDist = Int32.MaxValue;
		int resultRecno = 0;

		foreach (Unit unit in UnitArray)
		{
			if (unit.NationId != NationId || unit.RaceId == 0)
				continue;

			if (unit.Skill.SkillId != Skill.SKILL_CONSTRUCTION)
				continue;

			if (unit.IsVisible() && unit.RegionId() != RegionId)
				continue;

			if (unit.UnitMode == UnitConstants.UNIT_MODE_CONSTRUCT)
			{
				Firm firm = FirmArray[unit.UnitModeParam];

				if (firm.UnderConstruction || (firm.HitPoints * 100 / firm.MaxHitPoints) <= 90 ||
				    Info.game_date <= firm.LastAttackedDate.AddDays(8))
					continue;
			}
			else if (unit.UnitMode == UnitConstants.UNIT_MODE_UNDER_TRAINING)
			{
				continue;
			}
			else if (unit.ActionMode == UnitConstants.ACTION_ASSIGN_TO_FIRM && unit.ActionPara2 == FirmId)
			{
				return unit.SpriteId;
			}
			else if (unit.ActionMode != UnitConstants.ACTION_STOP)
			{
				continue;
			}

			if (!nearest)
				return unit.SpriteId;

			int curDist = Misc.points_distance(unit.NextLocX, unit.NextLocY, LocX1, LocY1);
			if (curDist < minDist)
			{
				resultRecno = unit.SpriteId;
				minDist = curDist;
			}
		}

		return resultRecno;
	}

	public void SendIdleBuilderHere(char remoteAction)
	{
		if (BuilderId != 0)
			return;

		//if( remote.is_enable() && !remoteAction )
		//{
		//// packet structure : <firm recno>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_FIRM_REQ_BUILDER, sizeof(short));
		//*shortPtr = firm_recno;
		//return;
		//}

		int unitRecno = FindIdleBuilder(true);
		if (unitRecno == 0)
			return;

		Unit unit = UnitArray[unitRecno];
		if (unit.UnitMode == UnitConstants.UNIT_MODE_CONSTRUCT)
		{
			Firm firm = FirmArray[unit.UnitModeParam];

			// order idle unit out of the building
			if (!firm.SetBuilder(0))
			{
				return;
			}
		}

		unit.Assign(LocX1, LocY1);
	}


	public virtual void SellFirm(int remoteAction)
	{
		//if( !remoteAction && remote.is_enable() )
		//{
		//// packet structure : <firm recno>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_FIRM_SELL, sizeof(short));
		//*shortPtr = firm_recno;
		//return;
		//}
		//------- sell at 50% of the original cost -------//

		Nation nation = NationArray[NationId];

		double sellIncome = FirmRes[FirmType].setup_cost / 2.0 * HitPoints / MaxHitPoints;

		nation.add_income(NationBase.INCOME_SELL_FIRM, sellIncome);

		SERes.sound(LocCenterX, LocCenterY, 1, 'F', FirmType, "SELL");

		FirmArray.DeleteFirm(this);
	}

	public void DestructFirm(int remoteAction)
	{
		//if( !remoteAction && remote.is_enable() )
		//{
		//// packet structure : <firm recno>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_FIRM_DESTRUCT, sizeof(short));
		//*shortPtr = firm_recno;
		//return;
		//}

		SERes.sound(LocCenterX, LocCenterY, 1, 'F', FirmType, "DEST");

		FirmArray.DeleteFirm(this);
	}


	public bool CanSpyBribe(int bribeWorkerId, int briberNationRecno)
	{
		bool canBribe = false;
		int spyRecno;

		if (bribeWorkerId != 0) // the overseer is selected
			spyRecno = Workers[bribeWorkerId - 1].spy_recno;
		else
			spyRecno = UnitArray[OverseerId].SpyId;

		if (spyRecno != 0)
		{
			// only when the unit is not yet a spy of the player. Still display the bribe button when it's a spy of another nation
			canBribe = SpyArray[spyRecno].TrueNationId != briberNationRecno;
		}
		else
		{
			if (bribeWorkerId != 0)
				canBribe = Workers[bribeWorkerId - 1].race_id > 0; // cannot bribe if it's a weapon
			else
				canBribe = UnitArray[OverseerId].Rank != Unit.RANK_KING; // cannot bribe a king
		}

		return canBribe;
	}

	public int SpyBribe(int bribeAmount, int briberSpyRecno, int workerId)
	{
		// this can happen in multiplayer as there is a one frame delay when the message is sent and when it is processed
		if (!CanSpyBribe(workerId, SpyArray[briberSpyRecno].TrueNationId))
			return 0;

		//---------------------------------------//

		int succeedChance = SpyBribeSucceedChance(bribeAmount, briberSpyRecno, workerId);

		NationArray[SpyArray[briberSpyRecno].TrueNationId].add_expense(NationBase.EXPENSE_BRIBE, bribeAmount, false);

		//------ if the bribe succeeds ------//

		if (succeedChance > 0 && Misc.Random(100) < succeedChance)
		{
			Spy briber = SpyArray[briberSpyRecno];
			// TODO crash NameId == 0
			Spy newSpy = SpyArray.AddSpy(0, 10); // add a new Spy record

			newSpy.ActionMode = Spy.SPY_IDLE;
			newSpy.SpyLoyalty = Math.Min(100, Math.Max(30, succeedChance)); // within the 30-100 range

			newSpy.TrueNationId = briber.TrueNationId;
			newSpy.CloakedNationId = briber.CloakedNationId;

			if (workerId != 0)
			{
				Worker worker = Workers[workerId - 1];
				worker.spy_recno = newSpy.SpyId;
				newSpy.RaceId = worker.race_id;
				newSpy.NameId = worker.name_id;

				// if this worker does not have a name, give him one now as a spy must reserve a name (see below on use_name_id() for reasons)
				if (newSpy.NameId == 0)
					newSpy.NameId = RaceRes[newSpy.RaceId].get_new_name_id();
			}
			else if (OverseerId != 0)
			{
				Unit unit = UnitArray[OverseerId];
				unit.SpyId = newSpy.SpyId;
				newSpy.RaceId = unit.RaceId;
				newSpy.NameId = unit.NameId;
			}

			newSpy.SetPlace(Spy.SPY_FIRM, FirmId);

			//-- Spy always registers its name twice as his name will be freed up in deinit(). Keep an additional right because when a spy is assigned to a town, the normal program will free up the name id., so we have to keep an additional copy

			RaceRes[newSpy.RaceId].use_name_id(newSpy.NameId);

			BribeResult = Spy.BRIBE_SUCCEED;

			return newSpy.SpyId;
		}
		else //------- if the bribe fails --------//
		{
			SpyArray[briberSpyRecno].GetKilled(0); // the spy gets killed when the action failed.
			// 0 - don't display new message for the spy being killed, so we already display the msg on the interface
			BribeResult = Spy.BRIBE_FAIL;
			return 0;
		}
	}

	public int SpyBribeSucceedChance(int bribeAmount, int briberSpyRecno, int workerId)
	{
		Spy spy = SpyArray[briberSpyRecno];

		//---- if the bribing target is a worker ----//

		int unitLoyalty = 0, unitRaceId = 0, unitCommandPower = 0;
		int targetSpyRecno = 0;

		if (workerId != 0)
		{
			Worker worker = Workers[workerId - 1];

			unitLoyalty = worker.loyalty();
			unitRaceId = worker.race_id;
			unitCommandPower = 0;
			targetSpyRecno = worker.spy_recno;
		}
		else if (OverseerId != 0)
		{
			Unit unit = UnitArray[OverseerId];

			unitLoyalty = unit.Loyalty;
			unitRaceId = unit.RaceId;
			unitCommandPower = unit.CommanderPower();
			targetSpyRecno = unit.SpyId;
		}

		//---- determine whether the bribe will be successful ----//

		int succeedChance;

		if (targetSpyRecno != 0) // if the bribe target is also a spy
		{
			succeedChance = 0;
		}
		else
		{
			succeedChance = spy.SpySkill - unitLoyalty - unitCommandPower
			                + (int)NationArray[spy.TrueNationId].reputation
			                + 200 * bribeAmount / GameConstants.MAX_BRIBE_AMOUNT;

			//-- the chance is higher if the spy or the spy's king is racially homongenous to the bribe target,

			int spyKingRaceId = NationArray[spy.TrueNationId].race_id;

			succeedChance += (RaceRes.is_same_race(spy.RaceId, unitRaceId) ? 1 : 0) * 10 +
			                 (RaceRes.is_same_race(spyKingRaceId, unitRaceId) ? 1 : 0) * 10;

			if (unitLoyalty > 60) // harder for bribe units with over 60 loyalty
				succeedChance -= (unitLoyalty - 60);

			if (unitLoyalty > 70) // harder for bribe units with over 70 loyalty
				succeedChance -= (unitLoyalty - 70);

			if (unitLoyalty > 80) // harder for bribe units with over 80 loyalty
				succeedChance -= (unitLoyalty - 80);

			if (unitLoyalty > 90) // harder for bribe units with over 90 loyalty
				succeedChance -= (unitLoyalty - 90);

			if (unitLoyalty == 100)
				succeedChance = 0;
		}

		return succeedChance;
	}

	public bool ValidateCurBribe()
	{
		if (SpyArray.IsDeleted(ActionSpyId) ||
		    SpyArray[ActionSpyId].TrueNationId != NationArray.player_recno)
		{
			return false;
		}

		return CanSpyBribe(SelectedWorkerId, SpyArray[ActionSpyId].TrueNationId);
	}


	public virtual void BeingAttacked(int attackerUnitRecno)
	{
		LastAttackedDate = Info.game_date;

		if (NationId != 0 && AIFirm)
		{
			// this can happen when the unit has just changed nation
			if (UnitArray[attackerUnitRecno].NationId == NationId)
				return;

			NationArray[NationId].ai_defend(attackerUnitRecno);
		}
	}

	public virtual void AutoDefense(int targetRecno)
	{
		//--------------------------------------------------------//
		// if the firm_id is FIRM_CAMP, send the units to defense
		// the firm
		//--------------------------------------------------------//
		if (FirmType == FIRM_CAMP)
		{
			FirmCamp camp = (FirmCamp)this;
			camp.defend_target_recno = targetRecno;
			camp.defense(targetRecno);
		}

		for (int i = LinkedTowns.Count - 1; i >= 0; i--)
		{
			if (LinkedTowns[i] == 0 || TownArray.IsDeleted(LinkedTowns[i]))
				continue;

			Town town = TownArray[LinkedTowns[i]];

			//-------------------------------------------------------//
			// find whether military camp is linked to this town. If
			// so, defense for this firm
			//-------------------------------------------------------//
			if (town.NationId == NationId)
				town.AutoDefense(targetRecno);

			//-------------------------------------------------------//
			// some linked town may be deleted after calling auto_defense().
			// Also, the data in the linked_town_array may also be changed.
			//-------------------------------------------------------//
			if (i > LinkedTowns.Count)
				i = LinkedTowns.Count;
		}
	}

	public bool LocateSpace(bool removeFirm, ref int xLoc, ref int yLoc, int xLoc2, int yLoc2,
		int width, int height, int mobileType = UnitConstants.UNIT_LAND, int regionId = 0)
	{
		int checkXLoc, checkYLoc;

		if (removeFirm)
		{
			//*** note only for land unit with size 1x1 ***//
			int mType = UnitConstants.UNIT_LAND;

			for (checkYLoc = LocY1; checkYLoc <= LocY2; checkYLoc++)
			{
				for (checkXLoc = LocX1; checkXLoc <= LocX2; checkXLoc++)
				{
					if (World.GetLoc(checkXLoc, checkYLoc).CanMove(mType))
					{
						xLoc = checkXLoc;
						yLoc = checkYLoc;
						return true;
					}
				}
			}
		}
		else
		{
			checkXLoc = LocX1;
			checkYLoc = LocY1;
			if (!World.LocateSpace(ref checkXLoc, ref checkYLoc, xLoc2, yLoc2, width, height, mobileType, regionId))
			{
				return false;
			}
			else
			{
				xLoc = checkXLoc;
				yLoc = checkYLoc;
				return true;
			}
		}

		return false;
	}

	public void Reward(int workerId, int remoteAction)
	{
		//if( remoteAction==InternalConstants.COMMAND_PLAYER && remote.is_enable() )
		//{
		//if( !remoteAction && remote.is_enable() )
		//{
		//// packet structure : <firm recno> <worker id>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_FIRM_REWARD, 2*sizeof(short) );
		//*shortPtr = firm_recno;
		//shortPtr[1] = workerId;
		//}
		//}
		//else
		//{
		if (workerId == 0)
		{
			if (OverseerId != 0)
				UnitArray[OverseerId].Reward(NationId);
		}
		else
		{
			Workers[workerId - 1].change_loyalty(GameConstants.REWARD_LOYALTY_INCREASE);

			NationArray[NationId].add_expense(NationBase.EXPENSE_REWARD_UNIT, GameConstants.REWARD_COST);
		}
		//}
	}


	public void ProcessAnimation()
	{
		//-------- process animation ----------//

		FirmBuild firmBuild = FirmRes.get_build(FirmBuildId);
		int frameCount = firmBuild.frame_count;

		if (frameCount == 1) // no animation for this firm
			return;

		//---------- next frame -----------//

		if (--RemainFrameDelay == 0) // if it is in the delay between frames
		{
			RemainFrameDelay = firmBuild.frame_delay(CurFrame);

			if (++CurFrame > frameCount)
			{
				if (firmBuild.animate_full_size)
					CurFrame = 1;
				else
				{
					CurFrame = 2; // start with the 2nd frame as the 1st frame is the common frame
				}
			}
		}
	}
	
	public int ConstructionFrame() // for under construction only
	{
		FirmBuild firmBuild = FirmRes.get_build(FirmBuildId);
		int r = Convert.ToInt32(HitPoints * firmBuild.under_construction_bitmap_count / MaxHitPoints);
		if (r >= firmBuild.under_construction_bitmap_count)
			r = firmBuild.under_construction_bitmap_count - 1;
		return r;
	}

	public abstract void DrawDetails(IRenderer renderer);

	public abstract void HandleDetailsInput(IRenderer renderer);
	

	#region Old AI Functions
	
	public void ProcessCommonAI()
	{
		if (Info.TotalDays % 30 == FirmId % 30)
			ThinkRepair();

		//------ think about closing this firm ------//

		if (!ShouldCloseFlag)
		{
			if (AIShouldClose())
			{
				ShouldCloseFlag = true;
				NationArray[NationId].firm_should_close_array[FirmType - 1]++;
			}
		}
	}

	public abstract void ProcessAI();

	public void ThinkRepair()
	{
		Nation ownNation = NationArray[NationId];

		//----- check if the damage is serious enough -----//

		// 70% to 95%
		if (HitPoints >= MaxHitPoints * (70 + ownNation.pref_repair_concern / 4) / 100.0)
			return;

		//--- if it's no too heavily damaged, it is just that the AI has a high concern on this ---//

		if (HitPoints >= MaxHitPoints * 80.0 / 100.0)
		{
			if (ownNation.total_jobless_population < 15)
				return;
		}

		//------- queue assigning a construction worker now ------//

		ownNation.add_action(LocX1, LocY1, -1, -1, Nation.ACTION_AI_ASSIGN_CONSTRUCTION_WORKER, FirmType);
	}

	public void AIDelFirm()
	{
		if (UnderConstruction)
		{
			CancelConstruction(InternalConstants.COMMAND_AI);
		}
		else
		{
			if (CanSell())
				SellFirm(InternalConstants.COMMAND_AI);
			else
				DestructFirm(InternalConstants.COMMAND_AI);
		}
	}

	public bool AIRecruitWorker()
	{
		if (Workers.Count == MAX_WORKER)
			return false;

		Nation nation = NationArray[NationId];

		for (int i = 0; i < LinkedTowns.Count; i++)
		{
			if (LinkedTownsEnable[i] != InternalConstants.LINK_EE)
				continue;

			Town town = TownArray[LinkedTowns[i]];

			//-- only recruit workers from towns of other nations if we don't have labor ourselves

			if (town.NationId != NationId && nation.total_jobless_population > MAX_WORKER)
				continue;

			// don't order units to move into it as they will be recruited from the town automatically
			if (town.JoblessPopulation > 0)
				return false;
		}

		//---- order workers to move into the firm ----//

		nation.add_action(LocX1, LocY1, -1, -1, Nation.ACTION_AI_ASSIGN_WORKER,
			FirmType, MAX_WORKER - Workers.Count);

		return true;
	}

	public virtual bool AIHasExcessWorker()
	{
		return false;
	}

	public bool ThinkBuildFactory(int rawId)
	{
		if (NoNeighborSpace) // if there is no space in the neighbor area for building a new firm.
			return false;

		Nation nation = NationArray[NationId];

		//--- check whether the AI can build a new firm next this firm ---//

		if (!nation.can_ai_build(FIRM_FACTORY))
			return false;

		//---------------------------------------------------//

		int factoryCount = 0;
		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			Firm firm = FirmArray[LinkedFirms[i]];

			if (firm.FirmType != FIRM_FACTORY || firm.NationId != NationId)
				continue;

			FirmFactory firmFactory = (FirmFactory)firm;
			if (firmFactory.ProductRawId != rawId)
				continue;

			//--- if one of own factories still has not recruited enough workers ---//

			if (firmFactory.Workers.Count < MAX_WORKER)
				return false;

			//---------------------------------------------------//
			//
			// If this factory has a medium to high level of stock,
			// this means the bottleneck is not at the factories,
			// building more factories won't solve the problem.
			//
			//---------------------------------------------------//

			if (firmFactory.StockQty > firmFactory.MaxStockQty * 0.1)
				return false;

			//---------------------------------------------------//
			//
			// Check if this factory is just outputing goods to
			// a market and it is actually not overcapacity.
			//
			//---------------------------------------------------//

			for (int j = firmFactory.LinkedFirms.Count - 1; j >= 0; j--)
			{
				if (firmFactory.LinkedFirmsEnable[j] != InternalConstants.LINK_EE)
					continue;

				Firm linkedFirm = FirmArray[firmFactory.LinkedFirms[j]];

				if (linkedFirm.FirmType != FIRM_MARKET)
					continue;

				//--- if this factory is producing enough goods to the market place, then it means it is still quite efficient

				MarketGoods marketGoods = ((FirmMarket)linkedFirm).market_product_array[rawId - 1];

				if (marketGoods != null && marketGoods.StockQty > 100)
					return false;
			}

			//----------------------------------------------//

			factoryCount++;
		}

		//---- don't build additional factory if we don't have enough peasants ---//

		if (factoryCount >= 1 && !nation.ai_has_enough_food())
			return false;

		//-- if there isn't much raw reserve left, don't build new factories --//

		if (FirmType == FIRM_MINE)
		{
			if (((FirmMine)this).ReserveQty < 1000 && factoryCount >= 1)
				return false;
		}

		//--- only build additional factories if we have a surplus of labor ---//

		if (nation.total_jobless_population < factoryCount * MAX_WORKER)
			return false;

		//--- only when we have checked it three times and all say it needs a factory, we then build a factory ---//

		if (++AIShouldBuildFactoryCount >= 3)
		{
			int buildXLoc, buildYLoc;

			if (!nation.find_best_firm_loc(FIRM_FACTORY, LocX1, LocY1, out buildXLoc, out buildYLoc))
			{
				NoNeighborSpace = true;
				return false;
			}

			nation.add_action(buildXLoc, buildYLoc, LocX1, LocY1, Nation.ACTION_AI_BUILD_FIRM, FIRM_FACTORY);

			AIShouldBuildFactoryCount = 0;
		}

		return true;
	}

	public virtual bool AIShouldClose()
	{
		return false;
	}

	public bool AIBuildNeighborFirm(int firmId)
	{
		Nation nation = NationArray[NationId];
		int buildXLoc, buildYLoc;

		if (!nation.find_best_firm_loc(firmId, LocX1, LocY1, out buildXLoc, out buildYLoc))
		{
			NoNeighborSpace = true;
			return false;
		}

		nation.add_action(buildXLoc, buildYLoc, LocX1, LocY1, Nation.ACTION_AI_BUILD_FIRM, firmId);
		return true;
	}

	public virtual void AIUpdateLinkStatus()
	{
		if (Workers.Count == 0) // if this firm does not need any workers. 
			return;

		if (IsWorkerFull()) // if this firm already has all the workers it needs. 
			return;

		//------------------------------------------------//

		Nation ownNation = NationArray[NationId];
		bool rc = false;

		for (int i = 0; i < LinkedTowns.Count; i++)
		{
			Town town = TownArray[LinkedTowns[i]];

			//--- enable link to hire people from the town ---//

			// either it's an independent town or it's friendly or allied to our nation
			rc = town.NationId == 0 || ownNation.get_relation_status(town.NationId) >= NationBase.NATION_FRIENDLY;

			ToggleTownLink(i + 1, rc, InternalConstants.COMMAND_AI);
		}
	}

	public bool ThinkHireInnUnit()
	{
		if (!NationArray[NationId].ai_should_hire_unit(30)) // 30 - importance rating
			return false;

		//---- one firm only hire one foreign race worker ----//

		int foreignRaceCount = 0;
		int majorityRace = MajorityRace();

		if (majorityRace != 0)
		{
			foreach (var worker in Workers)
			{
				if (worker.race_id != majorityRace)
					foreignRaceCount++;
			}
		}

		//-------- try to get skilled workers from inns --------//

		Nation nation = NationArray[NationId];
		FirmInn bestInn = null;
		int curRating, bestRating = 0, bestInnUnitId = 0;
		int prefTownHarmony = nation.pref_town_harmony;

		foreach (var innRecNo in nation.ai_inn_array)
		{
			FirmInn firmInn = (FirmInn)FirmArray[innRecNo];

			if (firmInn.RegionId != RegionId)
				continue;

			for (int j = 0; j < firmInn.inn_unit_array.Count; j++)
			{
				InnUnit innUnit = firmInn.inn_unit_array[j];
				if (innUnit.skill.SkillId != FirmSkillId)
					continue;

				//-------------------------------------------//
				// Rating of a unit to be hired is based on:
				//
				// -distance between the inn and this firm.
				// -whether the unit is racially homogenous to the majority of the firm workers
				//
				//-------------------------------------------//

				curRating = World.DistanceRating(LocCenterX, LocCenterY,
					firmInn.LocCenterX, firmInn.LocCenterY);

				curRating += innUnit.skill.SkillLevel;

				if (majorityRace == UnitRes[innUnit.unit_id].race_id)
				{
					curRating += prefTownHarmony;
				}
				else
				{
					//----------------------------------------------------//
					// Don't pick this unit if it isn't racially homogenous
					// to the villagers, and its pref_town_harmony is higher
					// than its skill level. (This means if its skill level
					// is low, its chance of being selected is lower.
					//----------------------------------------------------//

					if (majorityRace != 0)
					{
						if (foreignRaceCount > 0 || prefTownHarmony > innUnit.skill.SkillLevel - 50)
							continue;
					}
				}

				if (curRating > bestRating)
				{
					bestRating = curRating;
					bestInn = firmInn;
					bestInnUnitId = j + 1;
				}
			}
		}

		//-----------------------------------------//

		if (bestInn != null)
		{
			int unitRecno = bestInn.hire(bestInnUnitId);

			if (unitRecno != 0)
			{
				UnitArray[unitRecno].Assign(LocX1, LocY1);
				return true;
			}
		}

		return false;
	}

	public bool ThinkCapture()
	{
		Nation captureNation = null;

		foreach (Nation nation in NationArray)
		{
			if (nation.is_ai() && CanWorkerCapture(nation.nation_recno))
			{
				captureNation = nation;
				break;
			}
		}

		if (captureNation == null)
			return false;

		//------- do not capture firms of our ally --------//
		if (NationArray[NationId].is_ai() && captureNation.get_relation_status(NationId) == NationBase.NATION_ALLIANCE)
			return false;

		//------- capture the firm --------//

		CaptureFirm(captureNation.nation_recno);

		//------ order troops to attack nearby enemy camps -----//

		FirmCamp bestTarget = null;
		int minDistance = Int32.MaxValue;

		foreach (Firm firm in FirmArray)
		{
			//----- only attack enemy camps -----//

			if (firm.NationId != NationId || firm.FirmType != FIRM_CAMP)
				continue;

			int curDistance = Misc.points_distance(LocCenterX, LocCenterY, firm.LocCenterX, firm.LocCenterY);

			//--- only attack camps within 15 location distance to this firm ---//

			if (curDistance < 15 && curDistance < minDistance)
			{
				minDistance = curDistance;
				bestTarget = (FirmCamp)firm;
			}
		}

		if (bestTarget != null)
		{
			bool useAllCamp = captureNation.pref_military_courage > 60 || Misc.Random(3) == 0;

			captureNation.ai_attack_target(bestTarget.LocX1, bestTarget.LocY1,
				bestTarget.total_combat_level(), false, 0, 0, useAllCamp);
		}

		return true;
	}

	public void AIFirmCaptured(int capturerNationRecno)
	{
		Nation ownNation = NationArray[NationId];

		if (!ownNation.is_ai()) //**BUGHERE
			return;

		if (ownNation.get_relation(capturerNationRecno).status >= NationBase.NATION_FRIENDLY)
			ownNation.ai_end_treaty(capturerNationRecno);

		TalkRes.ai_send_talk_msg(capturerNationRecno, NationId, TalkMsg.TALK_DECLARE_WAR);
	}

	#endregion
}