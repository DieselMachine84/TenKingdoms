using System;

namespace TenKingdoms;

public class Spy : IIdObject
{
	public const int SPY_UNDEFINED = 0;
	public const int SPY_MOBILE = 1;
	public const int SPY_TOWN = 2;
	public const int SPY_FIRM = 3;

	public const int SPY_IDLE = 0;
	public const int SPY_SOW_DISSENT = 1;
	public const int SPY_SABOTAGE = 2;

	public const int BRIBE_NONE = 0;
	public const int BRIBE_SUCCEED = 1;
	public const int BRIBE_FAIL = 2;

	public const int ASSASSINATE_FAIL = 0;
	public const int ASSASSINATE_SUCCEED_AT_LARGE = 1;
	public const int ASSASSINATE_SUCCEED_KILLED = 2;

	public int SpyId { get; set; }
	public int SpyPlace { get; set; } // either SPY_TOWN or SPY_FIRM
	public int SpyPlaceId { get; set; } // it can be TownId, FirmId or UnitId depending on what SpyPlace is

	public int SpySkill { get; set; }
	public int SpyLoyalty { get; set; } // the spy's loyalty to his true home nation

	public int TrueNationId { get; set; }
	public int CloakedNationId { get; set; }

	// whether the spy will send a surrendering message to the cloaked nation when it changes its cloak to the nation
	public int NotifyCloakedNation { get; set; }
	public bool Exposed { get; set; } // this is set to 1 when the spy finished stealing the secret of a nation.

	public int RaceId { get; set; }
	public int NameId { get; set; }
	public int ActionMode { get; set; }

	private RaceRes RaceRes => Sys.Instance.RaceRes;
	private UnitRes UnitRes => Sys.Instance.UnitRes;
	private FirmRes FirmRes => Sys.Instance.FirmRes;
	private ConfigAdv ConfigAdv => Sys.Instance.ConfigAdv;
	private Info Info => Sys.Instance.Info;
	private World World => Sys.Instance.World;
	private SECtrl SECtrl => Sys.Instance.SECtrl;
	private NationArray NationArray => Sys.Instance.NationArray;
	private UnitArray UnitArray => Sys.Instance.UnitArray;
	private SpyArray SpyArray => Sys.Instance.SpyArray;
	private TownArray TownArray => Sys.Instance.TownArray;
	private FirmArray FirmArray => Sys.Instance.FirmArray;
	private NewsArray NewsArray => Sys.Instance.NewsArray;

	public Spy()
	{
	}

	void IIdObject.SetId(int id)
	{
		SpyId = id;
	}

	public void Init(int unitId, int spySkill)
	{
		SpyPlace = SPY_MOBILE;
		SpySkill = spySkill;

		if (unitId != 0)
		{
			Unit unit = UnitArray[unitId];
			SpyPlaceId = unitId;
			SpyLoyalty = unit.Loyalty;
			RaceId = unit.RaceId;
			NameId = unit.NameId;
			TrueNationId = unit.NationId;
			CloakedNationId = unit.NationId;
		}

		//--- spies hold a use right of the name id even though the unit itself will register the usage right of the name already ---//

		// the spy will free it up in deinit(). Keep an additional right because when a spy is assigned to a town,
		// the normal program will free up the name id., so we have to keep an additional copy
		RaceRes[RaceId].use_name_id(NameId);
	}

	public void Deinit()
	{
		SetPlace(SPY_UNDEFINED, 0); // reset spy place vars

		RaceRes[RaceId].free_name_id(NameId);
	}

	public void NextDay()
	{
		PayExpense();

		if (Exposed)
		{
			//-- he will be killed immediately unless he is back in his original nation color ---//

			if (TrueNationId != CloakedNationId)
			{
				GetKilled();
				return;
			}
			else
			{
				Exposed = false;
			}
		}

		//------ process actions ---------//

		if (Info.TotalDays % 15 == SpyId % 15)
		{
			if (SpyPlace == SPY_TOWN)
				ProcessTownAction();

			else if (SpyPlace == SPY_FIRM)
				ProcessFirmAction();
		}

		//------ increase skill --------//

		bool incSkill;

		if (ActionMode == SPY_IDLE) // increase slower when in sleep mode
			incSkill = Info.TotalDays % 80 == SpyId % 80;
		else
			incSkill = Info.TotalDays % 40 == SpyId % 40;

		if (incSkill && SpySkill < 100)
			SpySkill++;

		//----- update loyalty & think betray -------//

		if (Info.TotalDays % 60 == SpyId % 60)
		{
			UpdateLoyalty();

			if (ThinkBetray())
				return;
		}

		//----------- visit map (for fog of war) ----------//

		if (TrueNationId == NationArray.player_recno)
		{
			if (SpyPlace == SPY_TOWN)
			{
				Town town = TownArray[SpyPlaceId];
				World.Visit(town.LocX1, town.LocY1, town.LocX2, town.LocY2, GameConstants.EXPLORE_RANGE - 1);
			}
			else if (SpyPlace == SPY_FIRM)
			{
				Firm firm = FirmArray[SpyPlaceId];
				World.Visit(firm.loc_x1, firm.loc_y1, firm.loc_x2, firm.loc_y2, GameConstants.EXPLORE_RANGE - 1);
			}
			else if (SpyPlace == SPY_MOBILE)
			{
				Unit unit = UnitArray[SpyPlaceId];
				if (unit.UnitMode == UnitConstants.UNIT_MODE_CONSTRUCT)
				{
					Firm firm = FirmArray[unit.UnitModeParam];
					World.Visit(firm.loc_x1, firm.loc_y1, firm.loc_x2, firm.loc_y2, GameConstants.EXPLORE_RANGE - 1);
				}
				else if (unit.UnitMode == UnitConstants.UNIT_MODE_ON_SHIP)
				{
					Unit ship = UnitArray[unit.UnitModeParam];
					if (ship.UnitMode == UnitConstants.UNIT_MODE_IN_HARBOR)
					{
						Firm firm = FirmArray[ship.UnitModeParam];
						World.Visit(firm.loc_x1, firm.loc_y1, firm.loc_x2, firm.loc_y2, GameConstants.EXPLORE_RANGE - 1);
					}
					else
					{
						int locX1 = ship.NextLocX;
						int locY1 = ship.NextLocY;
						int locX2 = locX1 + ship.SpriteInfo.LocWidth - 1;
						int locY2 = locY1 + ship.SpriteInfo.LocHeight - 1;
						int range = UnitRes[ship.UnitType].visual_range;

						World.Unveil(locX1, locY1, locX2, locY2);
						World.Visit(locX1, locY1, locX2, locY2, range);
					}
				}
			}
		}
	}

	public string ActionString()
	{
		switch (ActionMode)
		{
			case SPY_IDLE:
			{
				//---- if the spy is in a firm or town of its own nation ---//

				if ((SpyPlace == SPY_TOWN && TownArray[SpyPlaceId].NationId == TrueNationId) ||
				    (SpyPlace == SPY_FIRM && FirmArray[SpyPlaceId].nation_recno == TrueNationId))
				{
					return "Counter-Spy";
				}
				else
				{
					return "Sleep";
				}
			}

			case SPY_SOW_DISSENT:
				return "Sow Dissent";

			case SPY_SABOTAGE:
				return "Sabotage";
		}

		return "";
	}

	private void ProcessTownAction()
	{
		Town town = TownArray[SpyPlaceId];

		if (ActionMode == SPY_SOW_DISSENT)
		{
			// only when there are non-spy people
			if (town.RacesPopulation[RaceId - 1] > town.RacesSpyCount[RaceId - 1])
			{
				// the more people there, the longer it takes to decrease the loyalty
				double decValue = SpySkill / 10.0 / town.RacesPopulation[RaceId - 1];

				//----- if this is an independent town -----//

				if (town.NationId == 0)
				{
					town.RacesResistance[RaceId - 1, TrueNationId - 1] -= decValue;

					if (town.RacesResistance[RaceId - 1, TrueNationId - 1] < 0.0)
						town.RacesResistance[RaceId - 1, TrueNationId - 1] = 0.0;
				}

				//--- if this is an enemy town, decrease the town people's loyalty ---//

				else
				{
					town.RacesLoyalty[RaceId - 1] -= decValue * 4.0;

					if (town.RacesLoyalty[RaceId - 1] < 0.0)
						town.RacesLoyalty[RaceId - 1] = 0.0;
				}
			}
		}
	}

	private void ProcessFirmAction()
	{
		Firm firm = FirmArray[SpyPlaceId];

		if (ActionMode == SPY_SOW_DISSENT)
		{
			//---- decrease the loyalty of the overseer if there is any -----//

			if (firm.overseer_recno != 0)
			{
				Unit unit = UnitArray[firm.overseer_recno];

				if (unit.RaceId == RaceId)
				{
					// a commander with a higher leadership skill will be less influenced by the spy's dissents
					// TODO review this
					bool decLoyaltyChance = (Misc.Random(10 - SpySkill / 10 + 1 + unit.Skill.SkillLevel / 20) == 0);
					if (decLoyaltyChance && unit.Loyalty > 0)
					{
						unit.ChangeLoyalty(-1);
					}
				}
			}

			//----- decrease the loyalty of the workers in the firm -----//


			for (int i = 0; i < firm.workers.Count; i++)
			{
				Worker worker = firm.workers[i];
				if (worker.race_id != RaceId)
					continue;

				//---- if the worker lives in a town ----//

				if (worker.town_recno != 0)
				{
					Town town = TownArray[worker.town_recno];
					int raceId = worker.race_id;

					if (town.RacesPopulation[raceId - 1] > town.RacesSpyCount[raceId - 1]) // only when there are non-spy people
					{
						// the more people there, the longer it takes to decrease the loyalty
						town.ChangeLoyalty(raceId, -4.0 * SpySkill / 10.0 / town.RacesPopulation[raceId - 1]);
					}
				}
				else //---- if the worker does not live in a town ----//
				{
					if (worker.spy_recno == 0) // the loyalty of the spy himself does not change
					{
						bool decLoyaltyChance = (Misc.Random(10 - SpySkill / 10 + 1) == 0);
						if (decLoyaltyChance && worker.worker_loyalty > 0)
							worker.worker_loyalty--;
					}
				}
			}
		}
	}

	private void PayExpense()
	{
		Nation nation = NationArray[TrueNationId];

		//---------- reduce cash -----------//

		if (nation.cash > 0)
		{
			nation.add_expense(NationBase.EXPENSE_SPY, GameConstants.SPY_YEAR_SALARY / 365.0, true);
		}
		else // decrease loyalty if the nation cannot pay the unit
		{
			ChangeLoyalty(-1);
		}

		//---------- reduce food -----------//

		bool inOwnFirm = false;

		if (SpyPlace == SPY_FIRM)
		{
			Firm firm = FirmArray[SpyPlaceId];

			if (firm.nation_recno == TrueNationId && firm.overseer_recno != 0 && UnitArray[firm.overseer_recno].SpyId == SpyId)
			{
				inOwnFirm = true;
			}
			
			// TODO what about worker?
		}

		if (SpyPlace == SPY_MOBILE || inOwnFirm)
		{
			if (nation.food > 0)
			{
				// TODO check that spies consume food correctly
				nation.consume_food(GameConstants.PERSON_FOOD_YEAR_CONSUMPTION / 365.0);
			}
			else
			{
				// decrease 1 loyalty point every 2 days
				if (Info.TotalDays % GameConstants.NO_FOOD_LOYALTY_DECREASE_INTERVAL == 0)
					ChangeLoyalty(-1);
			}
		}
	}

	private bool ThinkBetray()
	{
		if (SpyLoyalty >= GameConstants.UNIT_BETRAY_LOYALTY)
			return false;

		if (CloakedNationId == TrueNationId || CloakedNationId == 0)
			return false;

		//--- think whether the spy should turn towards the nation ---//

		Nation nation = NationArray[CloakedNationId];

		int nationScore = (int)nation.reputation; // reputation can be negative

		if (RaceRes.is_same_race(nation.race_id, RaceId))
			nationScore += 30;

		if (SpyLoyalty < nationScore || SpyLoyalty == 0)
		{
			DropSpyIdentity();
			return true;
		}

		return false;
	}

	public void DropSpyIdentity()
	{
		if (SpyPlace == SPY_FIRM)
		{
			Firm firm = FirmArray[SpyPlaceId];

			if (firm.overseer_recno != 0)
			{
				Unit unit = UnitArray[firm.overseer_recno];

				if (unit.SpyId == SpyId)
				{
					unit.SpyId = 0;
				}
			}

			foreach (var worker in firm.workers)
			{
				if (worker.spy_recno == SpyId)
				{
					worker.spy_recno = 0;
					break;
				}
			}
		}
		else if (SpyPlace == SPY_MOBILE)
		{
			Unit unit = UnitArray[SpyPlaceId];
			unit.SpyId = 0;
		}

		//------ delete this Spy record from spy_array ----//

		SpyArray.DeleteSpy(this);
	}

	public void ChangeTrueNation(int newNationId)
	{
		TrueNationId = newNationId;

		if (SpyPlace == SPY_FIRM)
		{
			SpyArray.UpdateFirmSpyCount(SpyPlaceId);
		}
	}

	public void ChangeCloakedNation(int newNationId)
	{
		if (newNationId == CloakedNationId)
			return;

		//--- only mobile units and overseers can change nation, spies in firms or towns cannot change nation,
		//--- their nation must be the same as the town or the firm's nation

		if (SpyPlace == SPY_MOBILE)
		{
			UnitArray[SpyPlaceId].SpyChangeNation(newNationId, InternalConstants.COMMAND_AUTO);
			return;
		}

		if (SpyPlace == SPY_FIRM)
		{
			Firm firm = FirmArray[SpyPlaceId];

			if (firm.overseer_recno != 0 && UnitArray[firm.overseer_recno].SpyId == SpyId)
			{
				UnitArray[firm.overseer_recno].SpyChangeNation(newNationId, InternalConstants.COMMAND_AUTO);
			}
		}
	}

	public bool CanChangeCloakedNation(int newNationId)
	{
		//---- can always change back to its original nation ----//

		if (newNationId == TrueNationId)
			return true;

		//--- only mobile units and overseers can change nation, spies in firms or towns cannot change nation,
		//--- their nation must be the same as the town or the firm's nation

		if (SpyPlace == SPY_MOBILE)
		{
			return UnitArray[SpyPlaceId].CanSpyChangeNation();
		}
		else // can't change in firms or towns.
		{
			return false;
		}
	}

	public bool CaptureFirm()
	{
		if (SpyPlace != SPY_FIRM)
			return false;

		if (!CanCaptureFirm())
			return false;

		Firm firm = FirmArray[SpyPlaceId];

		//------- if the spy is the overseer of the firm --------//

		if (FirmRes[firm.firm_id].need_overseer)
		{
			//---------------------------------------------------//
			//
			// For those soldiers who disagree with the spy general will
			// leave the command base and attack it. Soldiers who are not
			// racially homogenous to the spy general tend to disagree. Also
			// if the spy has a higher leadership, there will be a higher
			// chance for the soldiers to follow the general.
			//
			//---------------------------------------------------//

			Unit unit = UnitArray[firm.overseer_recno];

			if (!FirmRes[firm.firm_id].live_in_town) // if the workers of the firm do not live in towns
			{
				int unitLeadership = unit.Skill.SkillLevel;
				int nationReputation = (int)NationArray[TrueNationId].reputation;

				for (int i = 0; i < firm.workers.Count; i++)
				{
					Worker worker = firm.workers[i];

					//-- if this worker is a spy, it will stay with you --//

					if (worker.spy_recno != 0)
						continue;

					//---- if this is a normal worker -----//

					int obeyChance = unitLeadership / 2 + nationReputation / 2;

					if (RaceRes.is_same_race(worker.race_id, RaceId))
						obeyChance += 50;

					bool obeyFlag = (Misc.Random(100) < obeyChance); // if obeyChance >= 100, all units will object the overseer

					//--- if the worker obey, update its loyalty ---//

					if (obeyFlag)
						worker.worker_loyalty = Math.Max(GameConstants.UNIT_BETRAY_LOYALTY, obeyChance / 2);

					//--- if the worker does not obey, it is mobilized and attack the base ---//

					else
						firm.mobilize_worker(i + 1, InternalConstants.COMMAND_AUTO);
				}
			}

			//--------- add news message --------//

			if (firm.nation_recno == NationArray.player_recno)
				NewsArray.firm_captured(SpyPlaceId, TrueNationId, 1);

			//-------- if this is an AI firm --------//

			if (firm.firm_ai)
				firm.ai_firm_captured(TrueNationId);

			//----- the spy change nation and capture the firm -------//

			unit.SpyChangeNation(TrueNationId, InternalConstants.COMMAND_AUTO);
		}
		else
		{
			//------ otherwise the spy is a worker of the firm -------//

			//--------- add news message --------//

			if (firm.nation_recno == NationArray.player_recno)
				NewsArray.firm_captured(SpyPlaceId, TrueNationId, 1);

			//-------- if this is an AI firm --------//

			if (firm.firm_ai)
				firm.ai_firm_captured(TrueNationId);

			// the firm changes nation and the spies inside the firm will have their cloaked nation changed
			firm.change_nation(TrueNationId);
		}

		return true;
	}

	public bool CanCaptureFirm()
	{
		if (SpyPlace != SPY_FIRM)
			return false;

		Firm firm = FirmArray[SpyPlaceId];

		//------- if the spy is the overseer of the firm --------//

		if (FirmRes[firm.firm_id].need_overseer)
		{
			//-----------------------------------------------------//
			//
			// If the firm needs an overseer, the firm can only be
			// captured if the spy is the overseer of the firm.
			//
			//-----------------------------------------------------//
			return firm.overseer_recno != 0 && UnitArray[firm.overseer_recno].SpyId == SpyId;
		}

		//------ otherwise the spy is a worker of the firm -------//

		else
		{

			//---- check whether it's true that the only units in the firms are our spies ---//

			foreach (var worker in firm.workers)
			{
				if (worker.spy_recno == 0) // this worker is not a spy
					return false;

				if (SpyArray[worker.spy_recno].TrueNationId != TrueNationId)
					return false; // this worker is a spy, but not belong to the same nation
			}

			return true;
		}
	}

	private void Reward(int remoteAction)
	{
		//if( !remoteAction && remote.is_enable() )
		//{
		//// packet structure <spy recno>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_SPY_REWARD, sizeof(short));
		//shortPtr[0] = spy_recno;
		//return;
		//}

		ChangeLoyalty(GameConstants.REWARD_LOYALTY_INCREASE);

		NationArray[TrueNationId].add_expense(NationBase.EXPENSE_REWARD_UNIT, GameConstants.REWARD_COST);
	}

	public void SetExposed(int remoteAction)
	{
		//if( !remoteAction && remote.is_enable() )
		//{
		//// packet structure <spy recno>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_SPY_EXPOSED, sizeof(short));
		//shortPtr[0] = spy_recno;
		//return;
		//}

		Exposed = true;
	}

	public void ThinkBecomeKing()
	{
		int hisNationPower = NationArray[CloakedNationId].overall_rating;
		int parentNationPower = NationArray[TrueNationId].overall_rating;

		//--- if his nation is more power than the player's nation, the chance of handing his nation
		//--- over to his parent nation will be low unless the loyalty is very high ---//

		int acceptLoyalty = 90 + hisNationPower - parentNationPower;

		// never will a spy take over the player's nation
		// TODO remove IsAI() check?
		if (SpyLoyalty >= acceptLoyalty && NationArray[CloakedNationId].is_ai())
		{
			//------ hand his nation over to his parent nation ------//

			NationArray[CloakedNationId].surrender(TrueNationId);
		}
		else //--- betray his parent nation and rule the nation himself ---//
		{
			DropSpyIdentity();
		}
	}

	public Unit MobilizeTownSpy(bool decPop = true)
	{
		if (SpyPlace != SPY_TOWN)
			return null;

		Town town = TownArray[SpyPlaceId];
		Unit unit = town.MobilizeTownPeople(RaceId, decPop, true);

		if (unit == null)
			return null;

		unit.SpyId = SpyId;
		unit.SetName(NameId); // set the name id. of this unit

		SetPlace(SPY_MOBILE, unit.SpriteId);

		return unit;
	}

	public int MobilizeFirmSpy()
	{
		if (SpyPlace != SPY_FIRM)
			return 0;

		Firm firm = FirmArray[SpyPlaceId];
		int spyUnitId = 0;

		//---- check if the spy is the overseer of the firm -----//

		if (firm.overseer_recno != 0)
		{
			Unit unit = UnitArray[firm.overseer_recno];
			if (unit.SpyId == SpyId)
				spyUnitId = firm.mobilize_overseer();
		}

		//---- check if the spy is one of the workers of the firm ----//

		if (spyUnitId == 0)
		{
			int i;
			for (i = 0; i < firm.workers.Count; i++)
			{
				if (firm.workers[i].spy_recno == SpyId)
					break;
			}

			//---------- create a mobile unit ---------//

			// note: MobilizeWorker() will decrease Firm.PlayerSpyCount
			spyUnitId = firm.mobilize_worker(i + 1, InternalConstants.COMMAND_AUTO);
		}

		return spyUnitId;
	}

	public void SetNextActionMode()
	{
		if (SpyPlace == SPY_TOWN)
		{
			if (ActionMode == SPY_IDLE)
				ActionMode = SPY_SOW_DISSENT;
			else
				ActionMode = SPY_IDLE;
		}
		else if (SpyPlace == SPY_FIRM)
		{
			switch (ActionMode)
			{
				case SPY_IDLE:
					if (CanSabotage())
					{
						ActionMode = SPY_SABOTAGE;
					}

					break;

				case SPY_SABOTAGE:
					ActionMode = SPY_SOW_DISSENT;
					break;

				case SPY_SOW_DISSENT:
					ActionMode = SPY_IDLE;
					break;
			}
		}
	}

	public void ChangeLoyalty(int changeAmt)
	{
		int newLoyalty = SpyLoyalty + changeAmt;

		newLoyalty = Math.Max(0, newLoyalty);

		SpyLoyalty = Math.Min(100, newLoyalty);
	}

	private void UpdateLoyalty()
	{
		Nation ownNation = NationArray[TrueNationId];

		//TODO DieselMachine (int)ownNation.reputation/2
		int targetLoyalty = 50 + (int)ownNation.reputation / 4 + ownNation.overall_rank_rating() / 4;

		if (RaceId == ownNation.race_id)
			targetLoyalty += 20;

		targetLoyalty = Math.Min(targetLoyalty, 100);

		if (SpyLoyalty > targetLoyalty)
			SpyLoyalty--;

		else if (SpyLoyalty < targetLoyalty)
			SpyLoyalty++;
	}

	private bool CanSabotage()
	{
		// no sabotage action for military camp
		return SpyPlace == SPY_FIRM && FirmArray[SpyPlaceId].firm_id != Firm.FIRM_CAMP;
	}

	public int CloakedRankId()
	{
		switch (SpyPlace)
		{
			case SPY_TOWN:
				return Unit.RANK_SOLDIER;

			case SPY_FIRM:
			{
				Firm firm = FirmArray[SpyPlaceId];

				if (firm.overseer_recno != 0 && UnitArray[firm.overseer_recno].SpyId == SpyId)
				{
					return Unit.RANK_GENERAL;
				}
				else
				{
					return Unit.RANK_SOLDIER;
				}
			}

			case SPY_MOBILE:
				return UnitArray[SpyPlaceId].Rank;

			default:
				return Unit.RANK_SOLDIER;
		}
	}

	public int CloakedSkillId()
	{
		switch (SpyPlace)
		{
			case SPY_TOWN:
				return 0;

			case SPY_FIRM:
				return FirmArray[SpyPlaceId].firm_skill_id;

			case SPY_MOBILE:
				return UnitArray[SpyPlaceId].Skill.SkillId;

			default:
				return 0;
		}
	}

	public void SetPlace(int spyPlace, int spyPlaceId)
	{
		//----- reset spy counter of the current place ----//

		if (SpyPlace == SPY_FIRM)
		{
			if (TrueNationId == NationArray.player_recno)
			{
				if (!FirmArray.IsDeleted(SpyPlaceId))
				{
					FirmArray[SpyPlaceId].player_spy_count--;
				}
			}
		}

		else if (SpyPlace == SPY_TOWN)
		{
			if (!TownArray.IsDeleted(SpyPlaceId))
			{
				TownArray[SpyPlaceId].RacesSpyCount[RaceId - 1]--;
			}
		}

		//------- set the spy place now ---------//

		SpyPlace = spyPlace;
		SpyPlaceId = spyPlaceId;

		ActionMode = SPY_IDLE; // reset the spy mode

		//------- set the spy counter of the new place ------//

		if (SpyPlace == SPY_FIRM)
		{
			if (TrueNationId == NationArray.player_recno)
				FirmArray[SpyPlaceId].player_spy_count++;

			CloakedNationId = FirmArray[SpyPlaceId].nation_recno;

			// when a spy has been assigned to a firm, its notification flag should be set to 1,
			// so the nation can control it as it is one of its own units
			if (FirmArray[SpyPlaceId].nation_recno != TrueNationId)
				NotifyCloakedNation = 1;
		}
		else if (SpyPlace == SPY_TOWN)
		{
			TownArray[SpyPlaceId].RacesSpyCount[RaceId - 1]++;

			//-----------------------------------------------------------//
			// We need to update it here as this spy may have resigned from
			// a foreign firm and go back to its home village. And the
			// nation of the foreign firm and the home village are different.
			//-----------------------------------------------------------//

			CloakedNationId = TownArray[SpyPlaceId].NationId;

			if (TownArray[SpyPlaceId].NationId != TrueNationId)
				NotifyCloakedNation = 1;
		}
	}

	public int SpyPlaceNationId()
	{
		if (SpyPlace == SPY_TOWN)
			return TownArray[SpyPlaceId].NationId;

		if (SpyPlace == SPY_FIRM)
			return FirmArray[SpyPlaceId].nation_recno;

		return 0;
	}

	public void GetSpyLocation(out int locX, out int locY)
	{
		locX = -1;
		locY = -1;

		switch (SpyPlace)
		{
			case SPY_FIRM:
				if (!FirmArray.IsDeleted(SpyPlaceId))
				{
					locX = FirmArray[SpyPlaceId].center_x;
					locY = FirmArray[SpyPlaceId].center_y;
				}

				break;

			case SPY_TOWN:
				if (!TownArray.IsDeleted(SpyPlaceId))
				{
					locX = TownArray[SpyPlaceId].LocCenterX;
					locY = TownArray[SpyPlaceId].LocCenterY;
				}

				break;

			case SPY_MOBILE:
				if (!UnitArray.IsDeleted(SpyPlaceId))
				{
					Unit unit = UnitArray[SpyPlaceId];

					if (unit.UnitMode == UnitConstants.UNIT_MODE_ON_SHIP)
					{
						Unit ship = UnitArray[unit.UnitModeParam];
						locX = ship.NextLocX;
						locY = ship.NextLocY;
					}
					else
					{
						locX = unit.NextLocX;
						locY = unit.NextLocY;
					}
				}

				break;
		}
	}

	public int Assassinate(int targetUnitId, int remoteAction)
	{
		//if( !remoteAction && remote.is_enable() )
		//{
		//// packet structure <spy recno>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_SPY_ASSASSINATE, sizeof(short)*2);
		//shortPtr[0] = spy_recno;
		//shortPtr[1] = targetUnitRecno;
		//return 0;
		//}

		//---------- validate first -----------//

		if (SpyPlace != SPY_FIRM)
			return 0;

		Unit targetUnit = UnitArray[targetUnitId];

		if (targetUnit.UnitMode != UnitConstants.UNIT_MODE_OVERSEE)
			return 0;

		Firm firm = FirmArray[targetUnit.UnitModeParam];

		if (firm.firm_recno != SpyPlaceId)
			return 0;

		//---- get the attack and defense rating ----//

		if (!GetAssassinateRating(targetUnitId, out int attackRating, out int defenseRating, out int otherDefenderCount))
			return 0;

		//-------------------------------------------//

		int rc;
		int trueNationRecno = TrueNationId; // need to save it first as the spy may be killed later

		if (attackRating >= defenseRating)
		{
			//--- whether the spy will be get caught and killed in the mission ---//

			bool spyKillFlag = otherDefenderCount > 0 && attackRating - defenseRating < 80;

			//--- if the unit assassinated is the player's unit ---//

			if (targetUnit.NationId == NationArray.player_recno)
				NewsArray.unit_assassinated(targetUnit.SpriteId, spyKillFlag);

			firm.kill_overseer();

			//-----------------------------------------------------//
			// If there are other defenders in the firm and
			// the difference between the attack rating and defense rating
			// is small, then then spy will be caught and executed.
			//-----------------------------------------------------//

			if (spyKillFlag)
			{
				GetKilled(0); // 0 - don't display news message for the spy being killed

				rc = ASSASSINATE_SUCCEED_KILLED;
			}
			else
			{
				rc = ASSASSINATE_SUCCEED_AT_LARGE;
			}
		}
		else //----- if the assassination fails --------//
		{
			//-- if the spy is attempting to assassinate the player's general or king --//

			// don't display the below news message as the killing of the spy will already be displayed in news_array.spy_killed()

			// if (targetUnit.NationId == NationArray.PlayerId)
			//	NewsArray.AssassinatorCaught(SpyId, targetUnit.RankId);

			GetKilled(0); // 0 - don't display new message for the spy being killed

			rc = ASSASSINATE_FAIL;
		}

		//--- if this firm is the selected firm and the spy is the player's spy ---//

		if (trueNationRecno == NationArray.player_recno && firm.firm_recno == FirmArray.selected_recno)
		{
			//TODO drawing
			//Firm.assassinate_result = rc;
			//Firm.firm_menu_mode = FIRM_MENU_ASSASSINATE_RESULT;
			//Info.disp();
		}

		return rc;
	}

	private bool GetAssassinateRating(int targetUnitId, out int attackRating, out int defenseRating, out int defenderCount)
	{
		attackRating = 0;
		defenseRating = 0;
		defenderCount = 0;

		//---------- validate first -----------//

		if (SpyPlace != SPY_FIRM)
			return false;

		Unit targetUnit = UnitArray[targetUnitId];

		if (targetUnit.UnitMode != UnitConstants.UNIT_MODE_OVERSEE)
			return false;

		Firm firm = FirmArray[targetUnit.UnitModeParam];

		if (firm.firm_recno != SpyPlaceId)
			return false;

		//------ get the hit points of the spy ----//

		int spyHitPoints = 0;

		foreach (var worker in firm.workers)
		{
			if (worker.spy_recno == SpyId)
			{
				spyHitPoints = worker.hit_points;
				break;
			}
		}

		//------ calculate success chance ------//

		attackRating = SpySkill + spyHitPoints / 2;
		defenseRating = (int)(targetUnit.HitPoints / 2.0);

		if (targetUnit.SpyId != 0)
			defenseRating += SpyArray[targetUnit.SpyId].SpySkill;

		if (targetUnit.Rank == Unit.RANK_KING)
			defenseRating += 50;

		for (int i = 0; i < firm.workers.Count; i++)
		{
			int spyId = firm.workers[i].spy_recno;

			//------ if this worker is a spy ------//

			if (spyId != 0)
			{
				Spy spy = SpyArray[spyId];

				if (spy.TrueNationId == TrueNationId) // our spy
				{
					attackRating += spy.SpySkill / 4;
				}
				else if (spy.TrueNationId == firm.nation_recno) // enemy spy
				{
					defenseRating += spy.SpySkill / 2;
					defenderCount++;
				}
			}
			else //----- if this worker is not a spy ------//
			{
				defenseRating += 4 + firm.workers[i].hit_points / 30;
				defenderCount++;
			}
		}

		//-------- if the assassination succeeds -------//

		defenseRating += 30 + Misc.Random(30);

		return true;
	}

	public void GetKilled(int dispNews = 1)
	{
		//-------- add news --------//

		if (TrueNationId == NationArray.player_recno || CloakedNationId == NationArray.player_recno)
		{
			NewsArray.spy_killed(SpyId);
			SECtrl.immediate_sound("SPY_DIE");
		}

		//--- If a spy is caught, the spy's nation's reputation wil decrease ---//

		NationArray[TrueNationId].change_reputation(-GameConstants.SPY_KILLED_REPUTATION_DECREASE);

		int hostNationId = 0;
		bool mobileUnit = false;

		if (SpyPlace == SPY_TOWN)
		{
			Town town = TownArray[SpyPlaceId];

			hostNationId = town.NationId;

			town.DecPopulation(RaceId, false);
		}

		else if (SpyPlace == SPY_FIRM)
		{
			Firm firm = FirmArray[SpyPlaceId];

			hostNationId = firm.nation_recno;

			//------- check if the overseer is the spy -------//

			if (firm.overseer_recno != 0)
			{
				Unit unit = UnitArray[firm.overseer_recno];

				if (unit.SpyId == SpyId)
				{
					firm.kill_overseer();
					// TODO too early return?
					return;
				}
			}

			//---- check if any of the workers is the spy ----//

			for (int i = firm.workers.Count - 1; i >= 0; i--)
			{
				Worker worker = firm.workers[i];
				if (worker.spy_recno == SpyId)
				{
					firm.kill_worker(worker);
					// TODO too early return?
					return;
				}
			}
		}
		else if (SpyPlace == SPY_MOBILE)
		{
			UnitArray.DeleteUnit(UnitArray[SpyPlaceId]);
			mobileUnit = true;
		}

		//--- If the spy is in an AI town or firm, the AI's relationship towards the spy's owner nation will decrease ---//

		if (hostNationId != 0 && NationArray[hostNationId].is_ai())
		{
			NationArray[hostNationId].change_ai_relation_level(TrueNationId, -5);
		}

		//---- delete the spy from spy_array ----//
		if (!mobileUnit)
			SpyArray.DeleteSpy(this);
		//else spy_array.del_spy() is called in UnitArray.del()
	}

	#region Old AI Functions

	public void ProcessAI()
	{
		if (SpyId % 30 == Info.TotalDays % 30)
			ThinkReward();

		switch (SpyPlace)
		{
			case SPY_TOWN:
				if (SpyId % 30 == Info.TotalDays % 30)
					ThinkTownSpy();
				break;

			case SPY_FIRM:
				if (SpyId % 30 == Info.TotalDays % 30)
					ThinkFirmSpy();
				break;

			case SPY_MOBILE:
				if (SpyId % 5 == Info.TotalDays % 5)
					ThinkMobileSpy();
				break;
		}
	}

	private void ThinkTownSpy()
	{
		Nation trueNation = NationArray[TrueNationId];

		Town town = TownArray[SpyPlaceId];

		if (town.NationId == TrueNationId) // anti-spy
			return;

		//------ if it's an independent town ------//

		if (town.NationId == 0)
		{
			if (trueNation.reputation > 0)
				ActionMode = SPY_SOW_DISSENT;

			//--- if the resistance has already drop low enough, the spy no longer needs to be in the town ---//

			if (town.RacesLoyalty[RaceId - 1] < GameConstants.MIN_INDEPENDENT_DEFEND_LOYALTY)
			{
				if (town.Population < GameConstants.MAX_TOWN_POPULATION - 5 && Misc.Random(6) == 0)
				{
					MobilizeTownSpy();
				}
			}
		}
		else
		{
			//-------------- if it's a nation town -------------//
			//
			// Set to sleep mode in most time so the spying skill can increase
			// gradually, when the loyalty level of the village falls to near
			// rebel level, set all of your spies in the village to sow dissent
			// mode and cause rebellion in the enemy village.
			//
			//--------------------------------------------------//

			// pref_loyalty_concern actually does apply to here, we just use a preference var so that the decision making process will vary between nations
			if (town.AverageLoyalty() < 50 - trueNation.pref_loyalty_concern / 10)
			{
				ActionMode = SPY_SOW_DISSENT;
			}
			else
			{
				if (trueNation.reputation > 0 && Misc.Random(1) == 0) // 50% chance of sowing dissents.
					ActionMode = SPY_SOW_DISSENT;
				else
					ActionMode = SPY_IDLE;
			}

			if (town.Population < GameConstants.MAX_TOWN_POPULATION - 5 && Misc.Random(6) == 0)
			{
				MobilizeTownSpy();
			}
		}
	}

	private void ThinkFirmSpy()
	{
		Firm firm = FirmArray[SpyPlaceId];

		if (firm.nation_recno == TrueNationId) // anti-spy
			return;

		bool isHumanPlayer = false;
		if (CloakedNationId != 0)
		{
			Nation cloakedNation = NationArray[CloakedNationId];
			isHumanPlayer = !cloakedNation.is_ai();
		}

		Nation trueNation = NationArray[TrueNationId];
		if (isHumanPlayer || trueNation.get_relation_status(CloakedNationId) != NationBase.NATION_ALLIANCE)
		{
			//-------- try to capturing the firm --------//

			if (CaptureFirm())
				return;

			//-------- think about bribing ---------//

			if (ThinkBribe())
				return;

			//-------- think about assassinating ---------//

			if (ThinkAssassinate())
				return;
		}

		//------ think about changing spy mode ----//

		// 1/10 chance to set it to idle to prevent from being caught
		else if (trueNation.reputation < 0 || Misc.Random(3) == 0)
		{
			ActionMode = SPY_IDLE;
		}
		else if (Misc.Random(2) == 0 && CanSabotage() && firm.is_operating() && firm.productivity >= 20)
		{
			ActionMode = SPY_SABOTAGE;
		}
		else
		{
			ActionMode = SPY_SOW_DISSENT;
		}
	}

	private bool ThinkMobileSpy()
	{
		Unit unit = UnitArray[SpyPlaceId];

		//--- if the spy is on the ship, nothing can be done ---//

		if (!unit.IsVisible())
			return false;

		//---- if the spy has stopped and there is no new action ----//

		if (unit.IsAIAllStop() /* && (!notify_cloaked_nation_flag || cloaked_nation_recno==0) */)
		{
			return ThinkMobileSpyNewAction();
		}

		return false;
	}

	private bool ThinkBribe()
	{
		Firm firm = FirmArray[SpyPlaceId];

		//--- only bribe enemies in military camps ---//

		if (firm.firm_id != Firm.FIRM_CAMP)
			return false;

		//----- only if there is an overseer in the camp -----//

		if (firm.overseer_recno == 0)
			return false;

		//---- see if the overseer can be bribe (kings and your own spies can't be bribed) ----//

		if (!firm.can_spy_bribe(0, TrueNationId)) // 0-bribe the overseer
			return false;

		//------ first check our financial status ------//

		Nation ownNation = NationArray[TrueNationId];
		Unit overseerUnit = UnitArray[firm.overseer_recno];

		if (SpySkill < Math.Min(50, overseerUnit.Skill.SkillLevel) || !ownNation.ai_should_spend(30))
		{
			return false;
		}

		//----- think about how important is this firm -----//

		int firmImportance = 0;

		for (int i = firm.linked_town_array.Count - 1; i >= 0; i--)
		{
			Town town = TownArray[firm.linked_town_array[i]];

			if (town.NationId == firm.nation_recno)
				firmImportance += town.Population * 2;
			else
				firmImportance += town.Population;
		}

		//------- think about which one to bribe -------//

		//-- first get the succeedChange if the bribe amount is zero --//

		// first 0 - $0 bribe amount, 3rd 0 - bribe the overseer
		int succeedChange = firm.spy_bribe_succeed_chance(0, SpyId, 0);

		//-- then based on it, figure out how much we have to offer to bribe successfully --//

		int bribeAmount = GameConstants.MAX_BRIBE_AMOUNT * (100 - succeedChange) / 100;

		bribeAmount = Math.Max(100, bribeAmount);

		//--- only bribe when the nation has enough money ---//

		if (!ownNation.ai_should_spend(30, bribeAmount))
			return false;

		//------- try to bribe the commander ----//

		int newSpyId = firm.spy_bribe(bribeAmount, SpyId, 0);

		if (newSpyId == 0) // bribing failed
			return true; // return 1 as the spy has been killed

		Spy newSpy = SpyArray[newSpyId];

		if (newSpy.CaptureFirm()) // try to capture the firm now
		{
			//newSpy.drop_spy_identity(); // drop the spy identity of the newly bribed spy if the capture is successful, this will save the spying costs
		}

		return true;
	}

	private bool ThinkAssassinate()
	{
		Firm firm = FirmArray[SpyPlaceId];

		//--- only bribe enemies in military camps ---//

		if (firm.firm_id != Firm.FIRM_CAMP)
			return false;

		//----- only if there is an overseer in the camp -----//

		if (firm.overseer_recno == 0)
			return false;

		//---- get the attack and defense rating ----//

		// return 0 if assassination is not possible
		if (!GetAssassinateRating(firm.overseer_recno, out int attackRating, out int defenseRating, out int otherDefenderCount))
			return false;

		Nation trueNation = NationArray[TrueNationId];

		// the random number is to increase the chance of attempting assassination
		if (attackRating + Misc.Random(20 + trueNation.pref_spy / 2) > defenseRating)
		{
			Assassinate(firm.overseer_recno, InternalConstants.COMMAND_AI);
			return true;
		}

		return false;
	}

	private bool ThinkReward()
	{
		Nation ownNation = NationArray[TrueNationId];

		//----------------------------------------------------------//
		// The need to secure high loyalty on this unit is based on:
		// -its skill
		// -its combat level
		// -soldiers commanded by this unit
		//----------------------------------------------------------//

		int neededLoyalty = SpySkill * (100 + ownNation.pref_loyalty_concern) / 100;

		// 10 points above the betray loyalty level to prevent betrayal
		neededLoyalty = Math.Max(GameConstants.UNIT_BETRAY_LOYALTY + 10, neededLoyalty);
		neededLoyalty = Math.Min(90, neededLoyalty);

		//------- if the loyalty is already high enough ------//

		if (SpyLoyalty >= neededLoyalty)
			return false;

		//---------- see how many cash & profit we have now ---------//

		int rewardNeedRating = neededLoyalty - SpyLoyalty;

		if (SpyLoyalty < GameConstants.UNIT_BETRAY_LOYALTY + 5)
			rewardNeedRating += 50;

		if (ownNation.ai_should_spend(rewardNeedRating))
		{
			Reward(InternalConstants.COMMAND_AI);
			return true;
		}

		return false;
	}

	public bool ThinkMobileSpyNewAction()
	{
		Nation trueNation = NationArray[TrueNationId];
		Unit spyUnit = UnitArray[SpyPlaceId];
		int spyRegionId = spyUnit.RegionId();

		bool hasNewMission = trueNation.think_spy_new_mission(RaceId, spyRegionId, out int locX, out int locY, out int cloakedNationId);

		if (hasNewMission)
		{
			return AddAssignSpyAction(locX, locY, cloakedNationId);
		}

		return false;
	}

	private bool AddAssignSpyAction(int destXLoc, int destYLoc, int cloakedNationRecno)
	{
		return NationArray[TrueNationId].add_action(destXLoc, destYLoc, -1, -1,
			Nation.ACTION_AI_ASSIGN_SPY, cloakedNationRecno, 1, SpyPlaceId) != null;
	}

	public int AISpyBeingAttacked(int attackerUnitRecno)
	{
		Unit attackerUnit = UnitArray[attackerUnitRecno];
		Unit spyUnit = UnitArray[SpyPlaceId];
		Nation trueNation = NationArray[TrueNationId];

		//----- if we are attacking our own units -----//

		if (attackerUnit.TrueNationId() == TrueNationId)
		{
			if (SpySkill > 50 - trueNation.pref_spy / 10 ||
			    spyUnit.HitPoints < spyUnit.MaxHitPoints * (100.0 - trueNation.pref_military_courage / 2.0) / 100.0)
			{
				ChangeCloakedNation(TrueNationId);
				return 1;
			}
		}
		else
		{
			//---- if this unit is attacking units of other nations -----//
			//
			// If the nation this spy cloaked into is at war with the spy's
			// true nation and the nation which the spy is currently attacking
			// is not at war with the spy's true nation, then change
			// the spy's cloak to the non-hostile nation.
			//
			//-----------------------------------------------------------//

			if (trueNation.get_relation_status(attackerUnit.NationId) != NationBase.NATION_HOSTILE &&
			    trueNation.get_relation_status(CloakedNationId) == NationBase.NATION_HOSTILE)
			{
				if (SpySkill > 50 - trueNation.pref_spy / 10 ||
				    spyUnit.HitPoints < spyUnit.MaxHitPoints * (100.0 - trueNation.pref_military_courage / 2.0) / 100.0)
				{
					ChangeCloakedNation(TrueNationId);
					return 1;
				}
			}
		}

		return 0;
	}
	
	#endregion
}