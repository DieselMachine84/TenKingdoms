using System;

namespace TenKingdoms;

public class Spy
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

	public int spy_recno;
	public int spy_place; // either SPY_TOWN or SPY_FIRM
	public int spy_place_para; // it can be town_recno, firm_recno or unit_recno depending on what spy_place is

	public int spy_skill;
	public int spy_loyalty; // the spy's loyalty to his true home nation

	public int true_nation_recno;
	public int cloaked_nation_recno;

	// whether the spy will send a surrendering message to the cloaked nation when it changes its cloak to the nation
	public int notify_cloaked_nation_flag;
	public bool exposed_flag; // this is set to 1 when the spy finished stealing the secret of a nation.

	public int race_id;
	public int name_id;
	public int action_mode;

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

	public void Init(int unitRecno, int spySkill)
	{
		spy_place = SPY_MOBILE;
		spy_skill = spySkill;

		if (unitRecno != 0)
		{
			Unit unit = UnitArray[unitRecno];
			spy_place_para = unitRecno;
			spy_loyalty = unit.loyalty;
			race_id = unit.race_id;
			name_id = unit.name_id;
			true_nation_recno = unit.nation_recno;
			cloaked_nation_recno = unit.nation_recno;
		}

		//--- spies hold a use right of the name id even though the unit itself will register the usage right of the name already ---//

		// the spy will free it up in deinit(). Keep an additional right because when a spy is assigned to a town,
		// the normal program will free up the name id., so we have to keep an additional copy
		RaceRes[race_id].use_name_id(name_id);
	}

	public void Deinit()
	{
		set_place(SPY_UNDEFINED, 0); // reset spy place vars

		RaceRes[race_id].free_name_id(name_id);

		//TODO check
		//spy_recno = 0;
	}

	public void next_day()
	{
		//------- pay expenses --------//

		pay_expense();

		//------ when the spy has been exposed -------//

		if (exposed_flag)
		{
			//-- he will be killed immediately unless he is back in his original nation color ---//

			if (true_nation_recno != cloaked_nation_recno)
			{
				get_killed();
				return;
			}
			else
			{
				exposed_flag = false; // reset exposed_flag.
			}
		}

		//------ process actions ---------//

		if (Info.TotalDays % 15 == spy_recno % 15)
		{
			if (spy_place == SPY_TOWN)
				process_town_action();

			else if (spy_place == SPY_FIRM)
				process_firm_action();
		}

		//------ increase skill --------//

		bool rc;

		if (action_mode == SPY_IDLE) // increase slower when in sleep mode
			rc = Info.TotalDays % 80 == spy_recno % 80;
		else
			rc = Info.TotalDays % 40 == spy_recno % 40;

		if (rc && spy_skill < 100)
			spy_skill++;

		//----- update loyalty & think betray -------//

		if (Info.TotalDays % 60 == spy_recno % 60)
		{
			update_loyalty();

			if (think_betray())
				return;
		}

		//----------- visit map (for fog of war) ----------//

		if (true_nation_recno == NationArray.player_recno)
		{
			if (spy_place == SPY_TOWN)
			{
				Town town = TownArray[spy_place_para];
				World.visit(town.LocX1, town.LocY1, town.LocX2, town.LocY2, GameConstants.EXPLORE_RANGE - 1);
			}
			else if (spy_place == SPY_FIRM)
			{
				Firm firm = FirmArray[spy_place_para];
				World.visit(firm.loc_x1, firm.loc_y1, firm.loc_x2, firm.loc_y2, GameConstants.EXPLORE_RANGE - 1);
			}
			else if (spy_place == SPY_MOBILE)
			{
				Unit unit = UnitArray[spy_place_para];
				if (unit.unit_mode == UnitConstants.UNIT_MODE_CONSTRUCT)
				{
					Firm firm = FirmArray[unit.unit_mode_para];
					World.visit(firm.loc_x1, firm.loc_y1, firm.loc_x2, firm.loc_y2, GameConstants.EXPLORE_RANGE - 1);
				}
				else if (unit.unit_mode == UnitConstants.UNIT_MODE_ON_SHIP)
				{
					Unit ship = UnitArray[unit.unit_mode_para];
					if (ship.unit_mode == UnitConstants.UNIT_MODE_IN_HARBOR)
					{
						Firm firm = FirmArray[ship.unit_mode_para];
						World.visit(firm.loc_x1, firm.loc_y1, firm.loc_x2, firm.loc_y2,
							GameConstants.EXPLORE_RANGE - 1);
					}
					else
					{
						int xLoc1 = ship.next_x_loc();
						int yLoc1 = ship.next_y_loc();
						int xLoc2 = xLoc1 + ship.sprite_info.loc_width - 1;
						int yLoc2 = yLoc1 + ship.sprite_info.loc_height - 1;
						int range = UnitRes[ship.unit_id].visual_range;

						World.unveil(xLoc1, yLoc1, xLoc2, yLoc2);
						World.visit(xLoc1, yLoc1, xLoc2, yLoc2, range);
					}
				}
			}
		}
	}

	public string action_str()
	{
		switch (action_mode)
		{
			case SPY_IDLE:
			{
				//---- if the spy is in a firm or town of its own nation ---//

				if ((spy_place == SPY_TOWN && TownArray[spy_place_para].NationId == true_nation_recno) ||
				    (spy_place == SPY_FIRM && FirmArray[spy_place_para].nation_recno == true_nation_recno))
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

	public void process_town_action()
	{
		Town town = TownArray[spy_place_para];

		if (action_mode == SPY_SOW_DISSENT)
		{
			// only when there are non-spy people
			if (town.RacesPopulation[race_id - 1] > town.RacesSpyCount[race_id - 1])
			{
				// the more people there, the longer it takes to decrease the loyalty
				double decValue = spy_skill / 10.0 / town.RacesPopulation[race_id - 1];

				//----- if this is an independent town -----//

				if (town.NationId == 0)
				{
					town.RacesResistance[race_id - 1, true_nation_recno - 1] -= decValue;

					if (town.RacesResistance[race_id - 1, true_nation_recno - 1] < 0.0)
						town.RacesResistance[race_id - 1, true_nation_recno - 1] = 0.0;
				}

				//--- if this is an enemy town, decrease the town people's loyalty ---//

				else
				{
					town.RacesLoyalty[race_id - 1] -= decValue * 4.0;

					if (town.RacesLoyalty[race_id - 1] < 0.0)
						town.RacesLoyalty[race_id - 1] = 0.0;
				}
			}
		}
	}

	public void process_firm_action()
	{
		Firm firm = FirmArray[spy_place_para];

		//---------- Sow Dissent ----------//

		if (action_mode == SPY_SOW_DISSENT)
		{
			//---- decrease the loyalty of the overseer if there is any -----//

			if (firm.overseer_recno != 0)
			{
				Unit unit = UnitArray[firm.overseer_recno];

				if (unit.race_id == race_id)
				{
					// a commander with a higher leadership skill will be less influenced by the spy's dissents
					bool decLoyaltyChance = (Misc.Random(10 - spy_skill / 10 + 1 + unit.skill.skill_level / 20) == 0);
					if (decLoyaltyChance && unit.loyalty > 0)
					{
						unit.change_loyalty(-1);
					}
				}
			}

			//----- decrease the loyalty of the workers in the firm -----//


			for (int i = 0; i < firm.workers.Count; i++)
			{
				Worker worker = firm.workers[i];
				if (worker.race_id != race_id)
					continue;

				//---- if the worker lives in a town ----//

				if (worker.town_recno != 0)
				{
					Town town = TownArray[worker.town_recno];
					int raceId = worker.race_id;

					if (town.RacesPopulation[raceId - 1] >
					    town.RacesSpyCount[raceId - 1]) // only when there are non-spy people
					{
						// the more people there, the longer it takes to decrease the loyalty
						town.ChangeLoyalty(raceId, -4.0 * spy_skill / 10.0 / town.RacesPopulation[raceId - 1]);
					}
				}
				else //---- if the worker does not live in a town ----//
				{
					if (worker.spy_recno == 0) // the loyalty of the spy himself does not change
					{
						bool decLoyaltyChance = (Misc.Random(10 - spy_skill / 10 + 1) == 0);
						if (decLoyaltyChance && worker.worker_loyalty > 0)
							worker.worker_loyalty--;
					}
				}
			}
		}
	}

	public void pay_expense()
	{
		Nation nation = NationArray[true_nation_recno];

		//---------- reduce cash -----------//

		if (nation.cash > 0)
		{
			nation.add_expense(NationBase.EXPENSE_SPY, GameConstants.SPY_YEAR_SALARY / 365.0, true);
		}
		else // decrease loyalty if the nation cannot pay the unit
		{
			change_loyalty(-1);
		}

		//---------- reduce food -----------//

		bool inOwnFirm = false;

		if (spy_place == SPY_FIRM)
		{
			Firm firm = FirmArray[spy_place_para];

			if (firm.nation_recno == true_nation_recno && firm.overseer_recno != 0 &&
			    UnitArray[firm.overseer_recno].spy_recno == spy_recno)
			{
				inOwnFirm = true;
			}
		}

		if (spy_place == SPY_MOBILE || inOwnFirm)
		{
			if (nation.food > 0)
			{
				nation.consume_food(GameConstants.PERSON_FOOD_YEAR_CONSUMPTION / 365.0);
			}
			else
			{
				// decrease 1 loyalty point every 2 days
				if (Info.TotalDays % GameConstants.NO_FOOD_LOYALTY_DECREASE_INTERVAL == 0)
					change_loyalty(-1);
			}
		}
	}

	public bool think_betray()
	{
		if (spy_loyalty >= GameConstants.UNIT_BETRAY_LOYALTY) // you when unit is
			return false;

		if (cloaked_nation_recno == true_nation_recno || cloaked_nation_recno == 0)
			return false;

		//--- think whether the spy should turn towards the nation ---//

		Nation nation = NationArray[cloaked_nation_recno];

		int nationScore = (int)nation.reputation; // reputation can be negative

		if (RaceRes.is_same_race(nation.race_id, race_id))
			nationScore += 30;

		if (spy_loyalty < nationScore || spy_loyalty == 0)
		{
			drop_spy_identity();
			return true;
		}

		return false;
	}

	public void drop_spy_identity()
	{
		if (spy_place == SPY_FIRM)
		{
			Firm firm = FirmArray[spy_place_para];
			bool rc = false;

			if (firm.overseer_recno != 0)
			{
				Unit unit = UnitArray[firm.overseer_recno];

				if (unit.spy_recno == spy_recno)
				{
					unit.spy_recno = 0;
					rc = true;
				}
			}

			if (!rc)
			{
				for (int i = 0; i < firm.workers.Count; i++)
				{
					if (firm.workers[i].spy_recno == spy_recno)
					{
						firm.workers[i].spy_recno = 0;
						rc = true;
						break;
					}
				}
			}
		}
		else if (spy_place == SPY_MOBILE)
		{
			Unit unit = UnitArray[spy_place_para];

			unit.spy_recno = 0;
		}

		//------ delete this Spy record from spy_array ----//

		SpyArray.DeleteSpy(this); // Spy::deinit() will take care of the rest of the initialization for the spy
	}

	public void change_true_nation(int newNationRecno)
	{
		true_nation_recno = newNationRecno;

		//--- update Firm::player_spy_count if the spy is in a firm ---//

		if (spy_place == SPY_FIRM)
		{
			SpyArray.update_firm_spy_count(spy_place_para);
		}
	}

	public void change_cloaked_nation(int newNationRecno)
	{
		if (newNationRecno == cloaked_nation_recno)
			return;

		//--- only mobile units and overseers can change nation, spies in firms or towns cannot change nation,
		//--- their nation recno must be the same as the town or the firm's nation recno

		if (spy_place == SPY_MOBILE)
		{
			UnitArray[spy_place_para].spy_change_nation(newNationRecno, InternalConstants.COMMAND_AUTO);
			return;
		}

		if (spy_place == SPY_FIRM)
		{
			Firm firm = FirmArray[spy_place_para];

			if (firm.overseer_recno != 0 && UnitArray[firm.overseer_recno].spy_recno == spy_recno)
			{
				UnitArray[firm.overseer_recno].spy_change_nation(newNationRecno, InternalConstants.COMMAND_AUTO);
				return;
			}
		}
	}

	public bool can_change_cloaked_nation(int newNationRecno)
	{
		//---- can always change back to its original nation ----//

		if (newNationRecno == true_nation_recno)
			return true;

		//--- only mobile units and overseers can change nation, spies in firms or towns cannot change nation,
		//--- their nation recno must be the same as the town or the firm's nation recno

		if (spy_place == SPY_MOBILE)
		{
			return UnitArray[spy_place_para].can_spy_change_nation();
		}
		else // can't change in firms or towns.
		{
			return false;
		}
	}

	public bool capture_firm()
	{
		if (spy_place != SPY_FIRM)
			return false;

		if (!can_capture_firm())
			return false;

		Firm firm = FirmArray[spy_place_para];

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
				int unitLeadership = unit.skill.skill_level;
				int nationReputation = (int)NationArray[true_nation_recno].reputation;

				for (int i = 0; i < firm.workers.Count; i++)
				{
					Worker worker = firm.workers[i];

					//-- if this worker is a spy, it will stay with you --//

					if (worker.spy_recno != 0)
						continue;

					//---- if this is a normal worker -----//

					int obeyChance = unitLeadership / 2 + nationReputation / 2;

					if (RaceRes.is_same_race(worker.race_id, race_id))
						obeyChance += 50;

					bool obeyFlag =
						Misc.Random(100) < obeyChance; // if obeyChance >= 100, all units will object the overseer

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
				NewsArray.firm_captured(spy_place_para, true_nation_recno, 1); // 1 - the capturer is a spy

			//-------- if this is an AI firm --------//

			if (firm.firm_ai)
				firm.ai_firm_captured(true_nation_recno);

			//----- the spy change nation and capture the firm -------//

			unit.spy_change_nation(true_nation_recno, InternalConstants.COMMAND_AUTO);
		}
		else
		{
			//------ otherwise the spy is a worker of the firm -------//

			//--------- add news message --------//

			if (firm.nation_recno == NationArray.player_recno)
				NewsArray.firm_captured(spy_place_para, true_nation_recno, 1); // 1 - the capturer is a spy

			//-------- if this is an AI firm --------//

			if (firm.firm_ai)
				firm.ai_firm_captured(true_nation_recno);

			//----- change the firm's nation recno -----//

			// the firm change nation and the spies inside the firm will have their cloaked nation recno changed
			firm.change_nation(true_nation_recno);
		}

		return true;
	}

	public bool can_capture_firm()
	{
		if (spy_place != SPY_FIRM)
			return false;

		Firm firm = FirmArray[spy_place_para];

		//------- if the spy is the overseer of the firm --------//

		if (FirmRes[firm.firm_id].need_overseer)
		{
			//-----------------------------------------------------//
			//
			// If the firm needs an overseer, the firm can only be
			// captured if the spy is the overseer of the firm.
			//
			//-----------------------------------------------------//

			if (firm.overseer_recno == 0 || UnitArray[firm.overseer_recno].spy_recno != spy_recno)
			{
				return false;
			}

			return true;
		}

		//------ otherwise the spy is a worker of the firm -------//

		//---- check whether it's true that the only units in the firms are our spies ---//

		for (int i = 0; i < firm.workers.Count; i++)
		{
			Worker worker = firm.workers[i];

			if (worker.spy_recno == 0) // this worker is not a spy
				return false;

			if (SpyArray[worker.spy_recno].true_nation_recno != true_nation_recno)
				return false; // this worker is a spy, but not belong to the same nation
		}

		return true;
	}

	public void reward(int remoteAction)
	{
		//if( !remoteAction && remote.is_enable() )
		//{
		//// packet structure <spy recno>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_SPY_REWARD, sizeof(short));
		//shortPtr[0] = spy_recno;
		//return;
		//}

		change_loyalty(GameConstants.REWARD_LOYALTY_INCREASE);

		NationArray[true_nation_recno].add_expense(NationBase.EXPENSE_REWARD_UNIT, GameConstants.REWARD_COST);
	}

	public void set_exposed(int remoteAction)
	{
		//if( !remoteAction && remote.is_enable() )
		//{
		//// packet structure <spy recno>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_SPY_EXPOSED, sizeof(short));
		//shortPtr[0] = spy_recno;
		//return;
		//}

		exposed_flag = true;
	}

	public void think_become_king()
	{
		int hisNationPower = NationArray[cloaked_nation_recno].overall_rating;
		int parentNationPower = NationArray[true_nation_recno].overall_rating;

		//--- if his nation is more power than the player's nation, the chance of handing his nation
		//--- over to his parent nation will be low unless the loyalty is very high ---//

		int acceptLoyalty = 90 + hisNationPower - parentNationPower;

		// never will a spy take over the player's nation
		if (spy_loyalty >= acceptLoyalty && NationArray[cloaked_nation_recno].is_ai())
		{
			//------ hand his nation over to his parent nation ------//

			NationArray[cloaked_nation_recno].surrender(true_nation_recno);
		}
		else //--- betray his parent nation and rule the nation himself ---//
		{
			drop_spy_identity();
		}
	}

	public int mobilize_spy()
	{
		switch (spy_place)
		{
			case SPY_TOWN:
				return mobilize_town_spy();

			case SPY_FIRM:
				return mobilize_firm_spy();

			case SPY_MOBILE:
				return spy_place_para;

			default:
				return 0;
		}
	}

	public int mobilize_town_spy(bool decPop = true)
	{
		if (spy_place != SPY_TOWN)
			return 0;

		Town town = TownArray[spy_place_para];

		int unitRecno = town.MobilizeTownPeople(race_id, decPop, true); //1-mobilize spies

		if (unitRecno == 0)
			return 0;

		Unit unit = UnitArray[unitRecno]; // set the spy vars of the mobilized unit

		unit.spy_recno = spy_recno;
		unit.set_name(name_id); // set the name id. of this unit

		set_place(SPY_MOBILE, unitRecno);

		return unitRecno;
	}

	public int mobilize_firm_spy()
	{
		if (spy_place != SPY_FIRM)
			return 0;

		Firm firm = FirmArray[spy_place_para];
		int spyUnitRecno = 0;

		//---- check if the spy is the overseer of the firm -----//

		if (firm.overseer_recno != 0)
		{
			Unit unit = UnitArray[firm.overseer_recno];

			if (unit.spy_recno == spy_recno)
				spyUnitRecno = firm.mobilize_overseer();
		}

		//---- check if the spy is one of the workers of the firm ----//

		if (spyUnitRecno == 0)
		{
			int i;
			for (i = 0; i < firm.workers.Count; i++)
			{
				if (firm.workers[i].spy_recno == spy_recno)
					break;
			}

			//---------- create a mobile unit ---------//

			// note: mobilize_woker() will decrease Firm::player_spy_count
			spyUnitRecno = firm.mobilize_worker(i + 1, InternalConstants.COMMAND_AUTO);
		}

		return spyUnitRecno;
	}

	public void set_action_mode(int actionMode)
	{
		action_mode = actionMode;
	}

	public void set_next_action_mode()
	{
		if (spy_place == SPY_TOWN)
		{
			if (action_mode == SPY_IDLE)
				set_action_mode(SPY_SOW_DISSENT);
			else
				set_action_mode(SPY_IDLE);
		}
		else if (spy_place == SPY_FIRM)
		{
			switch (action_mode)
			{
				case SPY_IDLE:
					if (can_sabotage())
					{
						set_action_mode(SPY_SABOTAGE);
					}

					break;

				case SPY_SABOTAGE:
					set_action_mode(SPY_SOW_DISSENT);
					break;

				case SPY_SOW_DISSENT:
					set_action_mode(SPY_IDLE);
					break;
			}
		}
	}

	public void change_loyalty(int changeAmt)
	{
		int newLoyalty = spy_loyalty + changeAmt;

		newLoyalty = Math.Max(0, newLoyalty);

		spy_loyalty = Math.Min(100, newLoyalty);
	}

	public void update_loyalty()
	{
		Nation ownNation = NationArray[true_nation_recno];

		//TODO DieselMachine (int)ownNation.reputation/2
		int targetLoyalty = 50 + (int)ownNation.reputation / 4 + ownNation.overall_rank_rating() / 4;

		if (race_id == ownNation.race_id)
			targetLoyalty += 20;

		targetLoyalty = Math.Min(targetLoyalty, 100);

		if (spy_loyalty > targetLoyalty)
			spy_loyalty--;

		else if (spy_loyalty < targetLoyalty)
			spy_loyalty++;
	}

	public bool can_sabotage()
	{
		// no sabotage actino for military camp
		return spy_place == SPY_FIRM && FirmArray[spy_place_para].firm_id != Firm.FIRM_CAMP;
	}

	public int cloaked_rank_id()
	{
		switch (spy_place)
		{
			case SPY_TOWN:
				return Unit.RANK_SOLDIER;

			case SPY_FIRM:
			{
				Firm firm = FirmArray[spy_place_para];

				if (firm.overseer_recno != 0 && UnitArray[firm.overseer_recno].spy_recno == spy_recno)
				{
					return Unit.RANK_GENERAL;
				}
				else
				{
					return Unit.RANK_SOLDIER;
				}
			}

			case SPY_MOBILE:
				return UnitArray[spy_place_para].rank_id;

			default:
				return Unit.RANK_SOLDIER;
		}
	}

	public int cloaked_skill_id()
	{
		switch (spy_place)
		{
			case SPY_TOWN:
				return 0;

			case SPY_FIRM:
				return FirmArray[spy_place_para].firm_skill_id;

			case SPY_MOBILE:
				return UnitArray[spy_place_para].skill.skill_id;

			default:
				return 0;
		}
	}

	public void set_place(int spyPlace, int spyPlacePara)
	{
		//----- reset spy counter of the current place ----//

		if (spy_place == SPY_FIRM)
		{
			if (true_nation_recno == NationArray.player_recno)
			{
				if (!FirmArray.IsDeleted(spy_place_para))
				{
					FirmArray[spy_place_para].player_spy_count--;
				}
			}
		}

		else if (spy_place == SPY_TOWN)
		{
			if (!TownArray.IsDeleted(spy_place_para))
			{
				TownArray[spy_place_para].RacesSpyCount[race_id - 1]--;
			}
		}

		//------- set the spy place now ---------//

		spy_place = spyPlace;
		spy_place_para = spyPlacePara;

		action_mode = SPY_IDLE; // reset the spy mode

		//------- set the spy counter of the new place ------//

		if (spy_place == SPY_FIRM)
		{
			if (true_nation_recno == NationArray.player_recno)
				FirmArray[spy_place_para].player_spy_count++;

			cloaked_nation_recno = FirmArray[spy_place_para].nation_recno;

			// when a spy has been assigned to a firm, its notification flag should be set to 1,
			// so the nation can control it as it is one of its own units
			if (FirmArray[spy_place_para].nation_recno != true_nation_recno)
				notify_cloaked_nation_flag = 1;
		}
		else if (spy_place == SPY_TOWN)
		{
			TownArray[spy_place_para].RacesSpyCount[race_id - 1]++;

			//-----------------------------------------------------------//
			// We need to update it here as this spy may have resigned from
			// a foreign firm and go back to its home village. And the
			// nation recno of the foreign firm and the home village are
			// different.
			//-----------------------------------------------------------//

			cloaked_nation_recno = TownArray[spy_place_para].NationId;

			// if it's our own town, don't change notify_cloaked_nation_flag
			if (TownArray[spy_place_para].NationId != true_nation_recno)
				notify_cloaked_nation_flag = 1;
		}
	}

	public int spy_place_nation_recno()
	{
		if (spy_place == SPY_TOWN)
			return TownArray[spy_place_para].NationId;

		else if (spy_place == SPY_FIRM)
			return FirmArray[spy_place_para].nation_recno;

		else
			return 0;
	}

	public bool get_loc(out int xLoc, out int yLoc)
	{
		xLoc = -1;
		yLoc = -1;

		switch (spy_place)
		{
			case SPY_FIRM:
				if (!FirmArray.IsDeleted(spy_place_para))
				{
					xLoc = FirmArray[spy_place_para].center_x;
					yLoc = FirmArray[spy_place_para].center_y;
					return true;
				}

				break;

			case SPY_TOWN:
				if (!TownArray.IsDeleted(spy_place_para))
				{
					xLoc = TownArray[spy_place_para].LocCenterX;
					yLoc = TownArray[spy_place_para].LocCenterY;
					return true;
				}

				break;

			case SPY_MOBILE:
				if (!UnitArray.IsDeleted(spy_place_para))
				{
					Unit unit = UnitArray[spy_place_para];

					if (unit.unit_mode == UnitConstants.UNIT_MODE_ON_SHIP)
					{
						Unit ship = UnitArray[unit.unit_mode_para];
						xLoc = ship.next_x_loc();
						yLoc = ship.next_y_loc();
					}
					else
					{
						xLoc = unit.next_x_loc();
						yLoc = unit.next_y_loc();
					}

					return true;
				}

				break;
		}

		return false;
	}

	public int assassinate(int targetUnitRecno, int remoteAction)
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

		if (spy_place != SPY_FIRM)
			return 0;

		Unit targetUnit = UnitArray[targetUnitRecno];

		if (targetUnit.unit_mode != UnitConstants.UNIT_MODE_OVERSEE)
			return 0;

		Firm firm = FirmArray[targetUnit.unit_mode_para];

		if (firm.firm_recno != spy_place_para)
			return 0;

		//---- get the attack and defense rating ----//

		int attackRating, defenseRating, otherDefenderCount;

		if (!get_assassinate_rating(targetUnitRecno, out attackRating, out defenseRating, out otherDefenderCount))
			return 0;

		//-------------------------------------------//

		int rc;
		int trueNationRecno = true_nation_recno; // need to save it first as the spy may be killed later

		if (attackRating >= defenseRating)
		{
			//--- whether the spy will be get caught and killed in the mission ---//

			bool spyKillFlag = otherDefenderCount > 0 && attackRating - defenseRating < 80;

			//--- if the unit assassinated is the player's unit ---//

			if (targetUnit.nation_recno == NationArray.player_recno)
				NewsArray.unit_assassinated(targetUnit.sprite_recno, spyKillFlag);

			firm.kill_overseer();

			//-----------------------------------------------------//
			// If there are other defenders in the firm and
			// the difference between the attack rating and defense rating
			// is small, then then spy will be caught and executed.
			//-----------------------------------------------------//

			if (spyKillFlag)
			{
				get_killed(0); // 0 - don't display new message for the spy being killed

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

			//		if( targetUnit.nation_recno == NationArray.player_recno )
			//			news_array.assassinator_caught(spy_recno, targetUnit.rank_id);

			get_killed(0); // 0 - don't display new message for the spy being killed

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

	public bool get_assassinate_rating(int targetUnitRecno, out int attackRating,
		out int defenseRating, out int defenderCount)
	{
		attackRating = 0;
		defenseRating = 0;
		defenderCount = 0;

		//---------- validate first -----------//

		if (spy_place != SPY_FIRM)
			return false;

		Unit targetUnit = UnitArray[targetUnitRecno];

		if (targetUnit.unit_mode != UnitConstants.UNIT_MODE_OVERSEE)
			return false;

		Firm firm = FirmArray[targetUnit.unit_mode_para];

		if (firm.firm_recno != spy_place_para)
			return false;

		//------ get the hit points of the spy ----//

		int spyHitPoints = 0;

		for (int i = 0; i < firm.workers.Count; i++)
		{
			if (firm.workers[i].spy_recno == spy_recno)
			{
				spyHitPoints = firm.workers[i].hit_points;
				break;
			}
		}

		//------ calculate success chance ------//

		attackRating = spy_skill + spyHitPoints / 2;
		defenseRating = (int)(targetUnit.hit_points / 2.0);
		defenderCount = 0;

		if (targetUnit.spy_recno != 0)
			defenseRating += SpyArray[targetUnit.spy_recno].spy_skill;

		if (targetUnit.rank_id == Unit.RANK_KING)
			defenseRating += 50;

		for (int i = 0; i < firm.workers.Count; i++)
		{
			int spyRecno = firm.workers[i].spy_recno;

			//------ if this worker is a spy ------//

			if (spyRecno != 0)
			{
				Spy spy = SpyArray[spyRecno];

				if (spy.true_nation_recno == true_nation_recno) // our spy
				{
					attackRating += spy.spy_skill / 4;
				}
				else if (spy.true_nation_recno == firm.nation_recno) // enemy spy
				{
					defenseRating += spy.spy_skill / 2;
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

	public void get_killed(int dispNews = 1)
	{
		//-------- add news --------//

		// the player's spy is killed
		// a spy cloaked as the player's people is killed in the player's firm or firm
		if (true_nation_recno == NationArray.player_recno || cloaked_nation_recno == NationArray.player_recno)
		{
			NewsArray.spy_killed(spy_recno);
			SECtrl.immediate_sound("SPY_DIE");
		}

		//--- If a spy is caught, the spy's nation's reputation wil decrease ---//

		NationArray[true_nation_recno].change_reputation(-GameConstants.SPY_KILLED_REPUTATION_DECREASE);

		//------- if the spy is in a town -------//

		int hostNationRecno = 0;
		int mobileUnit = 0;

		if (spy_place == SPY_TOWN)
		{
			Town town = TownArray[spy_place_para];

			hostNationRecno = town.NationId;

			town.DecPopulation(race_id, false);
		}

		//------- if the spy is in a firm -------//

		else if (spy_place == SPY_FIRM)
		{
			Firm firm = FirmArray[spy_place_para];

			hostNationRecno = firm.nation_recno;

			//------- check if the overseer is the spy -------//

			if (firm.overseer_recno != 0)
			{
				Unit unit = UnitArray[firm.overseer_recno];

				if (unit.spy_recno == spy_recno)
				{
					firm.kill_overseer();
					return;
				}
			}

			//---- check if any of the workers is the spy ----//

			for (int i = firm.workers.Count - 1; i >= 0; i--)
			{
				Worker worker = firm.workers[i];
				if (worker.spy_recno == spy_recno)
				{
					firm.kill_worker(worker);
					return;
				}
			}
		}
		else if (spy_place == SPY_MOBILE)
		{
			UnitArray.DeleteUnit(UnitArray[spy_place_para]);
			mobileUnit = 1;
		}

		//--- If the spy is in an AI town or firm, the AI's relationship towards the spy's owner nation will decrease ---//

		if (hostNationRecno != 0 && NationArray[hostNationRecno].is_ai())
		{
			NationArray[hostNationRecno].change_ai_relation_level(true_nation_recno, -5);
		}

		//---- delete the spy from spy_array ----//
		if (mobileUnit == 0)
			SpyArray.DeleteSpy(this);
		//else spy_array.del_spy() is called in UnitArray.del()
	}

	//------- AI functions -------//

	public void process_ai()
	{
		if (spy_recno % 30 == Info.TotalDays % 30) // think about changing actions once 30 days
			think_reward();

		switch (spy_place)
		{
			case SPY_TOWN:
				if (spy_recno % 30 == Info.TotalDays % 30)
					think_town_spy();
				break;

			case SPY_FIRM:
				if (spy_recno % 30 == Info.TotalDays % 30)
					think_firm_spy();
				break;

			case SPY_MOBILE:
				if (spy_recno % 5 == Info.TotalDays % 5)
					think_mobile_spy();
				break;
		}
	}

	public void think_town_spy()
	{
		Nation trueNation = NationArray[true_nation_recno];

		Town town = TownArray[spy_place_para];

		if (town.NationId == true_nation_recno) // anti-spy
			return;

		//------ if it's an independent town ------//

		if (town.NationId == 0)
		{
			if (trueNation.reputation > 0)
				set_action_mode(SPY_SOW_DISSENT);

			//--- if the resistance has already drop low enough, the spy no longer needs to be in the town ---//

			if (town.RacesLoyalty[race_id - 1] < GameConstants.MIN_INDEPENDENT_DEFEND_LOYALTY)
			{
				if (town.Population < GameConstants.MAX_TOWN_POPULATION - 5 && Misc.Random(6) == 0)
				{
					mobilize_town_spy();
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
				set_action_mode(SPY_SOW_DISSENT);
			}
			else
			{
				if (trueNation.reputation > 0 && Misc.Random(1) == 0) // 50% chance of sowing dissents.
					set_action_mode(SPY_SOW_DISSENT);
				else
					set_action_mode(SPY_IDLE);
			}

			if (town.Population < GameConstants.MAX_TOWN_POPULATION - 5 && Misc.Random(6) == 0)
			{
				mobilize_town_spy();
			}
		}
	}

	public void think_firm_spy()
	{
		Firm firm = FirmArray[spy_place_para];

		if (firm.nation_recno == true_nation_recno) // anti-spy
			return;

		bool isHumanPlayer = false;
		if (cloaked_nation_recno != 0)
		{
			Nation cloackedNation = NationArray[cloaked_nation_recno];
			isHumanPlayer = !cloackedNation.is_ai();
		}

		Nation trueNation = NationArray[true_nation_recno];
		if (isHumanPlayer || trueNation.get_relation_status(cloaked_nation_recno) != NationBase.NATION_ALLIANCE)
		{
			//-------- try to capturing the firm --------//

			if (capture_firm())
				return;

			//-------- think about bribing ---------//

			if (think_bribe())
				return;

			//-------- think about assassinating ---------//

			if (think_assassinate())
				return;
		}

		//------ think about changing spy mode ----//

		// 1/10 chance to set it to idle to prevent from being caught
		else if (trueNation.reputation < 0 || Misc.Random(3) == 0)
		{
			set_action_mode(SPY_IDLE);
		}
		else if (Misc.Random(2) == 0 && can_sabotage() && firm.is_operating() && firm.productivity >= 20)
		{
			set_action_mode(SPY_SABOTAGE);
		}
		else
		{
			set_action_mode(SPY_SOW_DISSENT);
		}
	}

	public bool think_mobile_spy()
	{
		Unit unit = UnitArray[spy_place_para];

		//--- if the spy is on the ship, nothing can be done ---//

		if (!unit.is_visible())
			return false;

		//---- if the spy has stopped and there is no new action ----//

		if (unit.is_ai_all_stop() /* && (!notify_cloaked_nation_flag || cloaked_nation_recno==0) */)
		{
			return think_mobile_spy_new_action();
		}

		return false;
	}

	public bool think_bribe()
	{
		Firm firm = FirmArray[spy_place_para];

		//--- only bribe enemies in military camps ---//

		if (firm.firm_id != Firm.FIRM_CAMP)
			return false;

		//----- only if there is an overseer in the camp -----//

		if (firm.overseer_recno == 0)
			return false;

		//---- see if the overseer can be bribe (kings and your own spies can't be bribed) ----//

		if (!firm.can_spy_bribe(0, true_nation_recno)) // 0-bribe the overseer
			return false;

		//------ first check our financial status ------//

		Nation ownNation = NationArray[true_nation_recno];
		Unit overseerUnit = UnitArray[firm.overseer_recno];

		if (spy_skill < Math.Min(50, overseerUnit.skill.skill_level) || !ownNation.ai_should_spend(30))
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
		int succeedChange = firm.spy_bribe_succeed_chance(0, spy_recno, 0);

		//-- then based on it, figure out how much we have to offer to bribe successfully --//

		int bribeAmount = GameConstants.MAX_BRIBE_AMOUNT * (100 - succeedChange) / 100;

		bribeAmount = Math.Max(100, bribeAmount);

		//--- only bribe when the nation has enough money ---//

		if (!ownNation.ai_should_spend(30, bribeAmount))
			return false;

		//------- try to bribe the commander ----//

		int newSpyRecno = firm.spy_bribe(bribeAmount, spy_recno, 0);

		if (newSpyRecno == 0) // bribing failed
			return true; // return 1 as the spy has been killed

		Spy newSpy = SpyArray[newSpyRecno];

		if (newSpy.capture_firm()) // try to capture the firm now
		{
			//newSpy.drop_spy_identity(); // drop the spy identity of the newly bribed spy if the capture is successful, this will save the spying costs
		}

		return true;
	}

	public bool think_assassinate()
	{
		Firm firm = FirmArray[spy_place_para];

		//--- only bribe enemies in military camps ---//

		if (firm.firm_id != Firm.FIRM_CAMP)
			return false;

		//----- only if there is an overseer in the camp -----//

		if (firm.overseer_recno == 0)
			return false;

		//---- get the attack and defense rating ----//

		int attackRating, defenseRating, otherDefenderCount;

		// return 0 if assassination is not possible
		if (!get_assassinate_rating(firm.overseer_recno, out attackRating, out defenseRating, out otherDefenderCount))
			return false;

		Nation trueNation = NationArray[true_nation_recno];

		// the random number is to increase the chance of attempting assassination
		if (attackRating + Misc.Random(20 + trueNation.pref_spy / 2) > defenseRating)
		{
			assassinate(firm.overseer_recno, InternalConstants.COMMAND_AI);
			return true;
		}

		return false;
	}

	public bool think_reward()
	{
		Nation ownNation = NationArray[true_nation_recno];

		//----------------------------------------------------------//
		// The need to secure high loyalty on this unit is based on:
		// -its skill
		// -its combat level
		// -soldiers commanded by this unit
		//----------------------------------------------------------//

		int neededLoyalty = spy_skill * (100 + ownNation.pref_loyalty_concern) / 100;

		// 10 points above the betray loyalty level to prevent betrayal
		neededLoyalty = Math.Max(GameConstants.UNIT_BETRAY_LOYALTY + 10, neededLoyalty);
		neededLoyalty = Math.Min(90, neededLoyalty);

		//------- if the loyalty is already high enough ------//

		if (spy_loyalty >= neededLoyalty)
			return false;

		//---------- see how many cash & profit we have now ---------//

		int rewardNeedRating = neededLoyalty - spy_loyalty;

		if (spy_loyalty < GameConstants.UNIT_BETRAY_LOYALTY + 5)
			rewardNeedRating += 50;

		if (ownNation.ai_should_spend(rewardNeedRating))
		{
			reward(InternalConstants.COMMAND_AI);
			return true;
		}

		return false;
	}

	public bool think_mobile_spy_new_action()
	{
		Nation trueNation = NationArray[true_nation_recno];
		Unit spyUnit = UnitArray[spy_place_para];
		int loc_x1;
		int loc_y1;
		int cloakedNationRecno;
		int spyRegionId = spyUnit.region_id();

		bool hasNewMission = trueNation.think_spy_new_mission(race_id, spyRegionId,
			out loc_x1, out loc_y1, out cloakedNationRecno);

		if (hasNewMission)
		{
			return add_assign_spy_action(loc_x1, loc_y1, cloakedNationRecno);
		}
		else
		{
			return false;
		}
	}

	public bool add_assign_spy_action(int destXLoc, int destYLoc, int cloakedNationRecno)
	{
		return NationArray[true_nation_recno].add_action(destXLoc, destYLoc, -1, -1,
			Nation.ACTION_AI_ASSIGN_SPY, cloakedNationRecno, 1, spy_place_para) != null;
	}

	public int ai_spy_being_attacked(int attackerUnitRecno)
	{
		Unit attackerUnit = UnitArray[attackerUnitRecno];
		Unit spyUnit = UnitArray[spy_place_para];
		Nation trueNation = NationArray[true_nation_recno];

		//----- if we are attacking our own units -----//

		if (attackerUnit.true_nation_recno() == true_nation_recno)
		{
			if (spy_skill > 50 - trueNation.pref_spy / 10 ||
			    spyUnit.hit_points < spyUnit.max_hit_points * (100 - trueNation.pref_military_courage / 2) / 100)
			{
				change_cloaked_nation(true_nation_recno);
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

			if (trueNation.get_relation_status(attackerUnit.nation_recno) != NationBase.NATION_HOSTILE &&
			    trueNation.get_relation_status(cloaked_nation_recno) == NationBase.NATION_HOSTILE)
			{
				if (spy_skill > 50 - trueNation.pref_spy / 10 ||
				    spyUnit.hit_points < spyUnit.max_hit_points * (100 - trueNation.pref_military_courage / 2) / 100)
				{
					change_cloaked_nation(true_nation_recno);
					return 1;
				}
			}
		}

		return 0;
	}
}