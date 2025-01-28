using System;
using System.Collections.Generic;
using System.Linq;

namespace TenKingdoms;

public class Town
{
	public const int RECEIVED_HIT_PER_KILL = 200 / InternalConstants.ATTACK_SLOW_DOWN;
	public const int MAX_TRAIN_QUEUE = 10;
	public const int MAX_MIGRATE_PER_DAY = 4; // don't migrate more than 4 units per day
	public const int MIN_MIGRATE_ATTRACT_LEVEL = 30;
	public const int REBEL_INTERVAL_MONTH = 3; // don't rebel twice in less than 3 months

	// Number of units enqueued when holding shift - ensure this is less than MAX_TRAIN_QUEUE
	public const int TOWN_TRAIN_BATCH_COUNT = 8;

	public int town_recno;
	public int town_name_id;

	public int nation_recno;
	public int rebel_recno; // whether this town is controlled by a rebel
	public int race_id;

	public DateTime setup_date; // the setup date of this town

	public bool ai_town;

	// AI check firm and town locatin by links, disable checking by setting this parameter to 1
	public bool ai_link_checked;

	// each bit n is high representing this independent town will attack nation n.
	public int independ_town_nation_relation;

	public bool has_linked_own_camp; // whether the town has linked military camps of the same nation
	public bool has_linked_enemy_camp; // whether the town has linked military camps of the same nation

	public bool is_base_town; // whether this town is base town or not

	public int loc_x1, loc_y1, loc_x2, loc_y2;

	public int loc_width()
	{
		return loc_x2 - loc_x1 + 1;
	}

	public int loc_height()
	{
		return loc_y2 - loc_y1 + 1;
	}

	public int center_x;
	public int center_y;
	public int region_id;

	public int layout_id; // town layout id.
	public int first_slot_id; // the first slot id. of the layout

	public int[] slot_object_id_array = new int[TownLayout.MAX_TOWN_LAYOUT_SLOT]; // the race id. of each slot building

	public int population;
	public int jobless_population;

	// the MAX population the current town layout supports
	public int[] max_race_pop_array = new int[GameConstants.MAX_RACE];
	public int[] race_pop_array = new int[GameConstants.MAX_RACE]; // population of each race
	// population growth, when it reaches 100, there will be one more person in the town
	public int[] race_pop_growth_array = new int[GameConstants.MAX_RACE];

	public int[] jobless_race_pop_array = new int[GameConstants.MAX_RACE];

	public double[] race_loyalty_array = new double[GameConstants.MAX_RACE];
	public int[] race_target_loyalty_array = new int[GameConstants.MAX_RACE];
	public int[] race_spy_count_array = new int[GameConstants.MAX_RACE]; // no. of spies in each race

	public double[,] race_resistance_array = new double[GameConstants.MAX_RACE, GameConstants.MAX_NATION];
	public int[,] race_target_resistance_array = new int[GameConstants.MAX_RACE, GameConstants.MAX_NATION];

	public int town_defender_count; // no. of units currently defending this town
	public DateTime last_being_attacked_date;

	// no. of received hit by attackers, when this > RECEIVED_HIT_PER_KILL, a town people will be killed
	public double received_hit_count;

	//public int[] train_queue_skill_array = new int[MAX_TRAIN_QUEUE]; // it stores the skill id.
	//public int[] train_queue_race_array = new int[MAX_TRAIN_QUEUE]; // it stores the race id.
	public List<int> train_queue_skill = new List<int>(); // it stores the skill id.
	public List<int> train_queue_race =new List<int>(); // it stores the race id.
	public int train_unit_recno; // race id. of the unit the town is currently training, 0-if currently not training any
	public int train_unit_action_id; // id. of the action to be assigned to this unit when it is finished training.
	public long startTrainFrameNumber;
	public int defend_target_recno; // used in defend mode, store recno of latest target atttacking this town

	//-------- other vars ----------//

	public int accumulated_collect_tax_penalty;
	public int accumulated_reward_penalty;
	public int accumulated_recruit_penalty;
	public int accumulated_enemy_grant_penalty;

	public DateTime last_rebel_date;
	public int independent_unit_join_nation_min_rating;

	public int quality_of_life;

	//------- auto policy -------------//

	public int auto_collect_tax_loyalty; // auto collect tax if the loyalty reaches this level
	public int auto_grant_loyalty; // auto grant if the loyalty drop below this level

	//----------- AI vars ------------//

	public int town_combat_level; // combat level of the people in this town

	// whether this town has the supply of these products
	public int[] has_product_supply = new int[GameConstants.MAX_PRODUCT];

	public bool no_neighbor_space; // 1 if there is no space to build firms/towns next to this town

	//------ inter-relationship -------//

	public List<int> linked_firm_array = new List<int>();
	public List<int> linked_town_array = new List<int>();

	public List<int> linked_firm_enable_array = new List<int>();
	public List<int> linked_town_enable_array = new List<int>();

	private PlantRes PlantRes => Sys.Instance.PlantRes;
	private TownRes TownRes => Sys.Instance.TownRes;
	private FirmRes FirmRes => Sys.Instance.FirmRes;
	private RaceRes RaceRes => Sys.Instance.RaceRes;
	private SpriteRes SpriteRes => Sys.Instance.SpriteRes;
	private UnitRes UnitRes => Sys.Instance.UnitRes;
	private GodRes GodRes => Sys.Instance.GodRes;
	private TechRes TechRes => Sys.Instance.TechRes;
	private SERes SERes => Sys.Instance.SERes;
	private SECtrl SECtrl => Sys.Instance.SECtrl;
	private Config Config => Sys.Instance.Config;
	private ConfigAdv ConfigAdv => Sys.Instance.ConfigAdv;
	private Info Info => Sys.Instance.Info;
	private World World => Sys.Instance.World;
	private NationArray NationArray => Sys.Instance.NationArray;
	private UnitArray UnitArray => Sys.Instance.UnitArray;
	private RebelArray RebelArray => Sys.Instance.RebelArray;
	private SpyArray SpyArray => Sys.Instance.SpyArray;
	private FirmArray FirmArray => Sys.Instance.FirmArray;
	private TownArray TownArray => Sys.Instance.TownArray;
	private RegionArray RegionArray => Sys.Instance.RegionArray;
	private SiteArray SiteArray => Sys.Instance.SiteArray;
	private NewsArray NewsArray => Sys.Instance.NewsArray;

	public Town()
	{
	}

	public string town_name()
	{
		return TownRes.get_name(town_name_id);
	}

	public int recruitable_race_pop(int raceId, bool recruitSpy)
	{
		int recruitableCount = jobless_race_pop_array[raceId - 1];

		if (train_unit_recno != 0 && UnitArray[train_unit_recno].race_id == raceId)
			recruitableCount--;

		if (!recruitSpy)
		{
			recruitableCount -= race_spy_count_array[raceId - 1];

			if (recruitableCount == -1) // it may have been reduced twice if the unit being trained is a spy 
				recruitableCount = 0;
		}

		return recruitableCount;
	}

	public int worker_population()
	{
		return population - jobless_population;
	}

	public int race_harmony(int raceId)
	{
		return population != 0 ? 100 * race_pop_array[raceId - 1] / population : 0;
	}

	// the race that has the majority of the population
	public int majority_race()
	{
		int mostRaceCount = 0, mostRaceId = 0;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (race_pop_array[i] > mostRaceCount)
			{
				mostRaceCount = race_pop_array[i];
				mostRaceId = i + 1;
			}
		}

		return mostRaceId;
	}

	public int average_loyalty()
	{
		int totalLoyalty = 0;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
			totalLoyalty += Convert.ToInt32(race_loyalty_array[i]) * race_pop_array[i];

		return totalLoyalty / population;
	}

	public int average_target_loyalty()
	{
		int totalLoyalty = 0;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
			totalLoyalty += Convert.ToInt32(race_target_loyalty_array[i]) * race_pop_array[i];

		return totalLoyalty / population;
	}

	public int average_resistance(int nationRecno)
	{
		double totalResistance = 0.0;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			int thisPop = race_pop_array[i];
			if (thisPop > 0)
				totalResistance += race_resistance_array[i, nationRecno - 1] * thisPop;
		}

		return Convert.ToInt32(totalResistance) / population;
	}

	public int average_target_resistance(int nationRecno)
	{
		int totalResistance = 0;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			int thisPop = race_pop_array[i];

			if (thisPop > 0)
			{
				int t = race_target_resistance_array[i, nationRecno - 1];

				if (t >= 0) // -1 means no target
					totalResistance += t * thisPop;
				else
					totalResistance += Convert.ToInt32(race_resistance_array[i, nationRecno - 1]) * thisPop;
			}
		}

		return totalResistance / population;
	}

	public void update_quality_of_life()
	{
		//--- calculate the estimated total purchase from this town ----//

		double townDemand = jobless_population * GameConstants.PEASANT_GOODS_MONTH_DEMAND
		                    + worker_population() * GameConstants.WORKER_GOODS_MONTH_DEMAND;

		double totalPurchase = 0.0;

		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			if (linked_firm_enable_array[i] != InternalConstants.LINK_EE)
				continue;

			Firm firm = FirmArray[linked_firm_array[i]];

			if (firm.firm_id != Firm.FIRM_MARKET)
				continue;

			FirmMarket firmMarket = (FirmMarket)firm;

			//-------------------------------------//

			for (int j = 0; j < GameConstants.MAX_MARKET_GOODS; j++)
			{
				MarketGoods marketGoods = firmMarket.market_goods_array[j];
				if (marketGoods.product_raw_id == 0 || marketGoods.month_demand == 0)
					continue;

				double monthSaleQty = marketGoods.sale_qty_30days();

				if (monthSaleQty > marketGoods.month_demand)
				{
					totalPurchase += townDemand;
				}
				else if (marketGoods.month_demand > townDemand)
				{
					totalPurchase += monthSaleQty * townDemand / marketGoods.month_demand;
				}
				else
					totalPurchase += monthSaleQty;
			}
		}

		//------ return the quality of life ------//

		quality_of_life = Convert.ToInt32(100 * totalPurchase / (townDemand * GameConstants.MAX_PRODUCT));
	}

	public int closest_own_camp()
	{
		int minDistance = Int32.MaxValue, closestFirmRecno = 0;

		for (int i = linked_firm_array.Count - 1; i >= 0; i--)
		{
			Firm firm = FirmArray[linked_firm_array[i]];

			if (firm.firm_id != Firm.FIRM_CAMP || firm.nation_recno != nation_recno)
				continue;

			int curDistance = Misc.rects_distance(loc_x1, loc_y1, loc_x2, loc_y2,
				firm.loc_x1, firm.loc_y1, firm.loc_x2, firm.loc_y2);

			if (curDistance < minDistance)
			{
				minDistance = curDistance;
				closestFirmRecno = firm.firm_recno;
			}
		}

		return closestFirmRecno;
	}

	private void set_world_matrix()
	{
		//--- if a nation set up a town in a location that the player has explored, contact between the nation and the player is established ---//

		for (int yLoc = loc_y1; yLoc <= loc_y2; yLoc++)
		{
			for (int xLoc = loc_x1; xLoc <= loc_x2; xLoc++)
			{
				Location locPtr = World.get_loc(xLoc, yLoc);

				if (locPtr.cargo_recno == 0) // skip the location where the settle unit is standing
					locPtr.set_town(town_recno);
			}
		}

		//--- if a nation set up a town in a location that the player has explored, contact between the nation and the player is established ---//

		establish_contact_with_player();

		//---- set this town's influence on the map ----//

		if (nation_recno != 0)
			World.set_power(loc_x1, loc_y1, loc_x2, loc_y2, nation_recno);

		//------------ reveal new land ----------//

		if (nation_recno == NationArray.player_recno ||
		    (nation_recno != 0 && NationArray[nation_recno].is_allied_with_player))
		{
			World.unveil(loc_x1, loc_y1, loc_x2, loc_y2);
			World.visit(loc_x1, loc_y1, loc_x2, loc_y2, GameConstants.EXPLORE_RANGE - 1);
		}

		//---- if the newly built firm is visual in the zoom window, redraw the zoom buffer ----//

		//TODO drawing
		//if( is_in_zoom_win() )
		//sys.zoom_need_redraw = 1;  // set the flag on so it will be redrawn in the next frame
	}

	private void restore_world_matrix()
	{
		for (int yLoc = loc_y1; yLoc <= loc_y2; yLoc++)
		{
			for (int xLoc = loc_x1; xLoc <= loc_x2; xLoc++)
			{
				World.get_loc(xLoc, yLoc).remove_town();
			}
		}

		//---- restore this town's influence on the map ----//

		if (nation_recno != 0)
			World.restore_power(loc_x1, loc_y1, loc_x2, loc_y2, town_recno, 0);

		//---- if the newly built firm is visual in the zoom window, redraw the zoom buffer ----//

		//TODO drawing
		//if( is_in_zoom_win() )
		//sys.zoom_need_redraw = 1;
	}

	private void establish_contact_with_player()
	{
		if (nation_recno == 0)
			return;

		//--- if a nation set up a town in a location that the player has explored, contact between the nation and the player is established ---//

		for (int yLoc = loc_y1; yLoc <= loc_y2; yLoc++)
		{
			for (int xLoc = loc_x1; xLoc <= loc_x2; xLoc++)
			{
				Location location = World.get_loc(xLoc, yLoc);

				if (location.explored() && NationArray.player_recno != 0)
				{
					NationRelation relation = NationArray.player.get_relation(nation_recno);

					//if( !remote.is_enable() )
					//{
					relation.has_contact = true;
					//}
					//else
					//{
					//if( !relation->contact_msg_flag && !relation->has_contact )
					//{
					// packet structure : <player nation> <explored nation>
					//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_NATION_CONTACT, 2*sizeof(short));
					//*shortPtr = nation_array.player_recno;
					//shortPtr[1] = nation_recno;
					//relation->contact_msg_flag = 1;
					//}
					//}
				}
			}
		}
	}

	private void process_food()
	{
		//--------- Peasants produce food ---------//

		NationArray[nation_recno]
			.add_food(Convert.ToDouble(jobless_population * GameConstants.PEASANT_FOOD_YEAR_PRODUCTION) / 365.0);

		//---------- People consume food ----------//

		NationArray[nation_recno]
			.consume_food(Convert.ToDouble(population * GameConstants.PERSON_FOOD_YEAR_CONSUMPTION) / 365.0);
	}

	private void process_auto()
	{
		if (!has_linked_own_camp) // can only collect or grant when there is a linked camp of its own.
			return;

		Nation nation = NationArray[nation_recno];

		//----- auto collect tax -----//

		if (auto_collect_tax_loyalty > 0)
		{
			if (accumulated_collect_tax_penalty == 0 && average_loyalty() >= auto_collect_tax_loyalty)
			{
				collect_tax(InternalConstants.COMMAND_AI);
			}
		}

		//---------- auto grant -----------//

		if (auto_grant_loyalty > 0)
		{
			if (accumulated_reward_penalty == 0 && average_loyalty() < auto_grant_loyalty && nation.cash > 0)
			{
				reward(InternalConstants.COMMAND_AI);
			}
		}
	}

	private void process_train()
	{
		Unit unit = UnitArray[train_unit_recno];
		int raceId = unit.race_id;

		//---- if the unit being trained was killed -----//

		bool cancelFlag = jobless_race_pop_array[raceId - 1] == 0;

		//-----------------------------------------------------------------//
		//
		// If after start training the unit (non-spy unit), a unit has been
		// mobilized, resulting that the spy count >= jobless_race,
		// we must cancel the training, otherwise when training finishes,
		// and dec_pop is called, spy count will > jobless count and cause error.
		//
		//-----------------------------------------------------------------//

		if (race_spy_count_array[raceId - 1] == jobless_race_pop_array[raceId - 1])
			cancelFlag = true;

		if (cancelFlag)
		{
			UnitArray.disappear_in_town(train_unit_recno, town_recno);
			train_unit_recno = 0;
			return;
		}

		//------------- process training ---------------//

		long totalTrainDays;

		if (Config.fast_build && nation_recno == NationArray.player_recno)
			totalTrainDays = GameConstants.TOTAL_TRAIN_DAYS / 2;
		else
			totalTrainDays = GameConstants.TOTAL_TRAIN_DAYS;

		if ((Sys.Instance.FrameNumber - startTrainFrameNumber) / InternalConstants.FRAMES_PER_DAY >= totalTrainDays)
		{
			finish_train(unit);
		}
	}

	private void finish_train(Unit unit)
	{
		SpriteInfo spriteInfo = unit.sprite_info;
		int xLoc = loc_x1; // xLoc & yLoc are used for returning results
		int yLoc = loc_y1;

		if (!World.locate_space(ref xLoc, ref yLoc, loc_x2, loc_y2,
			    spriteInfo.loc_width, spriteInfo.loc_height))
			return;

		unit.init_sprite(xLoc, yLoc);

		if (unit.is_own())
			SERes.far_sound(xLoc, yLoc, 1, 'S', unit.sprite_id, "RDY");

		unit.unit_mode = 0; // reset it to 0 from UNIT_MODE_UNDER_TRAINING
		train_unit_recno = 0;

		int townRecno = town_recno; // save the recno as it can be deleted in dec_pop()

		dec_pop(unit.race_id, false); // decrease the population now as the recruit() does do so

		//---- if this trained unit is tied to an AI action ----//

		if (train_unit_action_id != 0)
		{
			NationArray[nation_recno].process_action_id(train_unit_action_id);
			train_unit_action_id = 0;
		}

		//----- refresh if this town is currently selected ------//

		//TODO drawing
		//if (townRecno == Global.TownArray.selected_recno)
		//{
		//if (town_menu_mode == TOWN_MENU_MAIN)
		//{
		//info.disp();
		//}
		//else
		//{
		//disable_refresh = 1;
		//info.disp();
		//disable_refresh = 0;
		//}
		//}
	}

	private void population_grow()
	{
		if (town_defender_count > 0)
			return;

		if (population >= GameConstants.MAX_TOWN_GROWTH_POPULATION || population >= GameConstants.MAX_TOWN_POPULATION)
			return;

		//-------- population growth by birth ---------//

		bool autoSetFlag = false;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			//-- the population growth in an independent town is slower than in a nation town ---//

			double loyaltyMultiplier = race_loyalty_array[i] * 4.0 / 50.0 - 3.0;
			if (loyaltyMultiplier < 0.0)
				loyaltyMultiplier = 0.0;
			if (nation_recno != 0)
				race_pop_growth_array[i] += (int)(Convert.ToDouble(race_pop_array[i]) * loyaltyMultiplier);
			else
				race_pop_growth_array[i] += race_pop_array[i];

			while (race_pop_growth_array[i] > 100)
			{
				race_pop_growth_array[i] -= 100;

				race_pop_array[i]++;
				jobless_race_pop_array[i]++;

				population++;
				jobless_population++;

				//-- if the race's population grows too high, change the town layout --//

				if (race_pop_array[i] > max_race_pop_array[i])
					autoSetFlag = true;

				if (population >= GameConstants.MAX_TOWN_POPULATION)
					break;
			}

			if (population >= GameConstants.MAX_TOWN_POPULATION)
				break;
		}

		if (autoSetFlag)
			auto_set_layout();
	}

	public void add_queue(int skillId, int raceId, int amount = 1)
	{
		if (amount < 0)
			return;

		int queueSpace = MAX_TRAIN_QUEUE - train_queue_skill.Count - (train_unit_recno > 0 ? 1 : 0);
		int enqueueAmount = Math.Min(queueSpace, amount);

		for (int i = 0; i < enqueueAmount; ++i)
		{
			train_queue_skill.Add(skillId);
			train_queue_race.Add(raceId);
		}

		if (train_unit_recno == 0)
			process_queue();
	}

	public void remove_queue(int skillId, int amount = 1)
	{
		if (amount <= 0)
			return;

		for (int i = train_queue_skill.Count - 1; i >= 0; i--)
		{
			if (train_queue_skill[i] == skillId)
			{
				train_queue_skill.RemoveAt(i);
				train_queue_race.RemoveAt(i);
				amount--;
			}
		}

		// If there were less trained of skillId in the queue than were requested to be removed then
		// also cancel currently trained unit
		if (amount > 0 && train_unit_recno != 0)
		{
			Unit unit = UnitArray[train_unit_recno];
			if (unit.skill.skill_id == skillId)
				cancel_train_unit();
		}
	}

	private void process_queue()
	{
		if (train_queue_skill.Count == 0)
			return;

		if (jobless_population == 0)
			return;

		int queueCount = train_queue_skill.Count;
		int i = 0;
		for (i = 0; i < queueCount; i++)
		{
			if (can_train(train_queue_race[i]))
			{
				int skillId = train_queue_skill[i];
				int raceId = train_queue_race[i];
				train_queue_skill.RemoveAt(i);
				train_queue_race.RemoveAt(i);
				i--;
				recruit(skillId, raceId, InternalConstants.COMMAND_AUTO);
				break;
			}
		}

		//TODO drawing
		//if (town_menu_mode == TOWN_MENU_MAIN)
		//info.disp();
	}

	private void think_migrate()
	{
		if (jobless_population == 0)
			return;

		int raceId, migratedCount, townDistance;
		int saveTownRecno = town_recno;
		int saveTownNationRecno = nation_recno;

		foreach (Town town in TownArray)
		{
			if (town.nation_recno == 0)
				continue;

			if (town.town_recno == town_recno)
				continue;

			if (town.population >= GameConstants.MAX_TOWN_POPULATION)
				continue;

			townDistance = Misc.rects_distance(loc_x1, loc_y1, loc_x2, loc_y2,
				town.loc_x1, town.loc_y1, town.loc_x2, town.loc_y2);

			if (townDistance > InternalConstants.EFFECTIVE_TOWN_TOWN_DISTANCE)
				continue;

			//---- scan all jobless population, see if any of them want to migrate ----//

			raceId = ConfigAdv.GetRandomRace();

			for (int j = 0; j < GameConstants.MAX_RACE; j++)
			{
				if (++raceId > GameConstants.MAX_RACE)
					raceId = 1;

				// only if there are peasants who are jobless and are not spies
				if (recruitable_race_pop(raceId, false) == 0)
					continue;

				//--- migrate a number of people of the same race at the same time ---//

				migratedCount = 0;

				while (think_migrate_one(town, raceId, townDistance))
				{
					migratedCount++;

					// don't migrate more than one unit at a time for migrating to non-linked towns
					if (townDistance > InternalConstants.EFFECTIVE_TOWN_TOWN_DISTANCE)
						break;

					// allow a random and low max number to migrate when this happens
					if (migratedCount >= MAX_MIGRATE_PER_DAY || Misc.Random(4) == 0)
						break;
				}

				//------------- add news --------------//

				if (migratedCount > 0)
				{
					if (saveTownNationRecno == NationArray.player_recno ||
					    town.nation_recno == NationArray.player_recno)
					{
						NewsArray.migrate(saveTownRecno, town.town_recno, raceId, migratedCount);
					}

					return;
				}
			}
		}
	}

	private bool think_migrate_one(Town targetTown, int raceId, int townDistance)
	{
		//-- only if there are peasants who are jobless and are not spies --//

		if (recruitable_race_pop(raceId, false) == 0) //0-don't recruit spies
			return false;

		//---- if the target town's population has already reached its MAX ----//

		if (targetTown.population >= GameConstants.MAX_TOWN_POPULATION)
			return false;

		//-- do not migrate if the target town's population of that race is less than half of the population of the current town --//

		if (targetTown.race_pop_array[raceId - 1] < race_pop_array[raceId - 1] / 2)
			return false;

		//-- do not migrate if the target town might not be a place this peasant will stay --//

		if (targetTown.race_loyalty_array[raceId - 1] < 40)
			return false;

		//--- calculate the attractiveness rating of the current town ---//

		int curAttractLevel = race_harmony(raceId);

		//------- loyalty/resistance affecting the attractivness ------//

		if (nation_recno != 0)
		{
			// loyalty > 40 is considered as positive force, < 40 is considered as negative force
			curAttractLevel += Convert.ToInt32(NationArray[nation_recno].reputation +
				race_loyalty_array[raceId - 1] - 40.0);
		}
		else
		{
			if (targetTown.nation_recno != 0)
				curAttractLevel += Convert.ToInt32(race_resistance_array[raceId - 1, targetTown.nation_recno - 1]);
		}

		//--- calculate the attractiveness rating of the target town ---//

		int targetAttractLevel = targetTown.race_harmony(raceId);

		if (targetTown.nation_recno != 0)
			targetAttractLevel += Convert.ToInt32(NationArray[targetTown.nation_recno].reputation);

		if (targetAttractLevel < MIN_MIGRATE_ATTRACT_LEVEL)
			return false;

		//--------- compare the attractiveness ratings ---------//

		if (targetAttractLevel - curAttractLevel > MIN_MIGRATE_ATTRACT_LEVEL / 2)
		{
			//---------- migrate now ----------//

			int newLoyalty = Math.Max(targetAttractLevel / 2, 40);

			migrate(raceId, targetTown.town_recno, newLoyalty);
			return true;
		}

		return false;
	}

	private void migrate(int raceId, int destTownRecno, int newLoyalty)
	{
		Town destTown = TownArray[destTownRecno];

		if (destTown.population >= GameConstants.MAX_TOWN_POPULATION)
			return;

		//------- decrease the population of this town ------//

		dec_pop(raceId, false);

		//--------- increase the population of the target town ------//

		destTown.inc_pop(raceId, false, newLoyalty);
	}

	private bool unjob_town_people(int raceId, bool unjobSpy, bool unjobOverseer, bool killOverseer = false)
	{
		//---- if no jobless people, workers will then get killed -----//

		int racePop = jobless_race_pop_array[raceId - 1];

		for (int i = linked_firm_array.Count - 1; i >= 0; i--)
		{
			Firm firm = FirmArray[linked_firm_array[i]];

			//------- scan for workers -----------//

			for (int j = firm.workers.Count - 1; j >= 0; j--)
			{
				Worker worker = firm.workers[j];
				if (ConfigAdv.fix_town_unjob_worker && !unjobSpy && worker.spy_recno != 0)
					continue;

				//--- if the worker lives in this town ----//

				if (worker.race_id == raceId && worker.town_recno == town_recno)
				{
					if (firm.resign_worker(worker) == 0 && !ConfigAdv.fix_town_unjob_worker)
						return false;

					return jobless_race_pop_array[raceId - 1] == racePop + 1;
				}
			}
		}

		//----- if no worker killed, try to kill overseers ------//

		if (unjobOverseer)
		{
			for (int i = linked_firm_array.Count - 1; i >= 0; i--)
			{
				Firm firm = FirmArray[linked_firm_array[i]];

				//------- scan for overseer -----------//

				if (firm.overseer_recno != 0)
				{
					//--- if the overseer lives in this town ----//

					Unit overseerUnit = UnitArray[firm.overseer_recno];

					if (overseerUnit.race_id == raceId && firm.overseer_town_recno == town_recno)
					{
						if (killOverseer)
						{
							firm.kill_overseer();
						}
						else
						{
							int overseerUnitRecno = firm.overseer_recno;
							Unit unit = UnitArray[overseerUnitRecno];
							firm.assign_overseer(0);
							if (!UnitArray.IsDeleted(overseerUnitRecno) && unit.is_visible())
								UnitArray.disappear_in_town(overseerUnitRecno, town_recno);
						}

						return true;
					}
				}
			}
		}

		return false;
	}

	private int think_layout_id()
	{
		int needBuildCount = 0; // basic buildings needed
		int extraBuildCount = 0; // extra buildings needed beside the basic one

		//---- count the needed buildings of each race ----//

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (race_pop_array[i] == 0)
				continue;

			needBuildCount++; // essential buildings needed

			// extra buildings, which are not necessary, but will look better if the layout plan fits with this number
			const int popPerHouse = TownRes.POPULATION_PER_HOUSE;
			if (race_pop_array[i] > popPerHouse)
				extraBuildCount += (race_pop_array[i] - popPerHouse - 1) / popPerHouse + 1;
		}

		//---------- scan the town layout ---------//
		int layoutId;
		// scan from the most densed layout to the least densed layout
		for (layoutId = TownRes.town_layout_array.Length; layoutId > 0; layoutId--)
		{
			TownLayout townLayout = TownRes.get_layout(layoutId);

			//--- if this plan has less than the essential need ---//

			int countDiff = townLayout.build_count - (needBuildCount + extraBuildCount);

			if (countDiff == 0) // the match is perfect, return now
				break;

			// since we scan from the most densed town layout to the least densed one, if cannot find anyone matched now,
			// there won't be any in the lower positions of the array
			if (countDiff < 0)
			{
				layoutId = TownRes.town_layout_array.Length;
				break;
			}
		}

		//--- if there are more than one layout with the same number of building, pick one randomly ---//

		int layoutBuildCount = TownRes.get_layout(layoutId).build_count;
		int layoutId2;

		for (layoutId2 = layoutId - 1; layoutId2 > 0; layoutId2--)
		{
			TownLayout townLayout = TownRes.get_layout(layoutId2);

			if (layoutBuildCount != townLayout.build_count)
				break;
		}

		layoutId2++; // the lowest layout id. that has the same no. of buildings

		//------- return the result layout id -------//

		return layoutId2 + Misc.Random(layoutId - layoutId2 + 1);
	}

	private void think_rebel()
	{
		if (nation_recno == 0)
			return;

		if (Info.game_date < last_rebel_date.AddDays(REBEL_INTERVAL_MONTH * 30.0))
			return;

		// don't rebel within ten days after being attacked by a hostile unit
		if (town_defender_count > 0 || Info.game_date < last_being_attacked_date.AddDays(10.0))
			return;

		//--- rebel if 2/3 of the population becomes discontented ---//

		int discontentedCount = 0, rebelLeaderRaceId = 0, largestRebelRace = 0, trainRaceId = 0;
		int[] restrictRebelCount = new int[GameConstants.MAX_RACE];

		if (train_unit_recno != 0)
			trainRaceId = UnitArray[train_unit_recno].race_id;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			restrictRebelCount[i] = race_spy_count_array[i]; // spies do not rebel together with the rebellion

			if (race_pop_array[i] > 0 && race_loyalty_array[i] <= GameConstants.REBEL_LOYALTY)
			{
				discontentedCount += race_pop_array[i];

				// count firm spies that reside in this town
				for (int j = 0; j < linked_firm_array.Count; j++)
				{
					Firm firm = FirmArray[linked_firm_array[j]];
					foreach (Worker worker in firm.workers)
					{
						if (worker.spy_recno != 0 && worker.town_recno == town_recno)
							restrictRebelCount[i]++;
					}
				}

				if (trainRaceId == i + 1)
					restrictRebelCount[i]++; // unit under training cannot rebel

				if (race_pop_array[i] <= restrictRebelCount[i])
					continue; // no one can lead from this group

				if (race_pop_array[i] > largestRebelRace)
				{
					largestRebelRace = race_pop_array[i];
					rebelLeaderRaceId = i + 1;
				}
			}
		}

		if (rebelLeaderRaceId == 0) // no discontention or no one can lead
			return;

		if (population == 1) // if population is 1 only, handle otherwise
		{
		}
		else
		{
			if (discontentedCount < population * 2 / 3)
				return;
		}

		//----- if there was just one unit in the town and he rebels ----//

		bool oneRebelOnly = false;

		if (population == 1)
		{
			NewsArray.town_rebel(town_recno, 1);
			oneRebelOnly = true;
		}

		//----- create the rebel leader and the rebel group ------//

		int rebelCount = 1;
		int leaderUnitRecno = create_rebel_unit(rebelLeaderRaceId, true); // 1-the unit is the rebel leader

		if (leaderUnitRecno == 0)
			return;

		int curGroupId = UnitArray.cur_group_id++;
		Unit unit = UnitArray[leaderUnitRecno];
		unit.unit_group_id = curGroupId;

		if (oneRebelOnly) // if there was just one unit in the town and he rebels
		{
			RebelArray.AddRebel(leaderUnitRecno, nation_recno);
			return;
		}

		// create a rebel group
		Rebel rebel = RebelArray.AddRebel(leaderUnitRecno, nation_recno, Rebel.REBEL_ATTACK_TOWN, town_recno);

		//------- create other rebel units in the rebel group -----//

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (race_pop_array[i] <= restrictRebelCount[i] || race_loyalty_array[i] > GameConstants.REBEL_LOYALTY)
				continue;

			if (population == 1) // if only one peasant left, break, so not all peasants will rebel 
				break;

			// 30% - 60% of the unit will rebel.
			int raceRebelCount = (race_pop_array[i] - restrictRebelCount[i]) * (30 + Misc.Random(30)) / 100;

			int j = 0;
			for (; j < raceRebelCount; j++) // no. of rebel units of this race
			{
				int unitRecno = create_rebel_unit(i + 1, false);

				if (unitRecno == 0) // 0-the unit is not the rebel leader
					break;

				Unit rebelUnit = UnitArray[unitRecno];
				rebelUnit.unit_group_id = curGroupId;
				rebelUnit.leader_unit_recno = leaderUnitRecno;

				rebel.join(unitRecno);

				rebelCount++;
			}

			//--- when disloyal units left, the average loyalty increases ---//

			change_loyalty(i + 1, 50.0 * j / race_pop_array[i]);
		}

		//---------- add news -------------//

		last_rebel_date = Info.game_date;

		// add the news first as after callijng ai_spy_town_rebel, the town may disappear as all peasants are gone
		NewsArray.town_rebel(town_recno, rebelCount);

		//--- tell the AI spies in the town that a rebellion is happening ---//

		SpyArray.ai_spy_town_rebel(town_recno);
	}

	private bool think_surrender()
	{
		if (nation_recno == 0) // if it's an independent town
			return false;

		//--- only surrender when there is no own camps, but has enemy camps linked to this town ---//

		if (has_linked_own_camp || !has_linked_enemy_camp)
			return false;

		//--- surrender if 2/3 of the population think about surrender ---//

		int discontentedCount = 0;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (race_pop_array[i] <= race_spy_count_array[i]) // spies do not rebel together with the rebellion
				continue;

			if (race_loyalty_array[i] <= GameConstants.SURRENDER_LOYALTY)
				discontentedCount += race_pop_array[i];
		}

		if (discontentedCount < population * 2 / 3)
			return false;

		//-------- think about surrender to which nation ------//

		int curRating, bestRating = average_loyalty(), bestNationRecno = 0;

		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			Firm firm = FirmArray[linked_firm_array[i]];

			//---- if this is an enemy camp ----//

			if (firm.firm_id == Firm.FIRM_CAMP &&
			    firm.nation_recno != nation_recno &&
			    firm.nation_recno != 0 &&
			    firm.overseer_recno != 0)
			{
				// see camp_influence() for details on how the rating is calculated
				curRating = camp_influence(firm.overseer_recno);

				if (curRating > bestRating)
				{
					bestRating = curRating;
					bestNationRecno = firm.nation_recno;
				}
			}
		}

		//------------------------------------//

		if (bestNationRecno != 0)
		{
			surrender(bestNationRecno);
			return true;
		}
		else
			return false;
	}

	public void Init(int nationRecno, int raceId, int xLoc, int yLoc)
	{
		nation_recno = nationRecno;
		race_id = raceId;

		//err_when(raceId < 1 || raceId > GameConstants.MAX_RACE);

		//---- set the town section's absolute positions on the map ----//

		loc_x1 = xLoc;
		loc_y1 = yLoc;
		loc_x2 = loc_x1 + InternalConstants.TOWN_WIDTH - 1;
		loc_y2 = loc_y1 + InternalConstants.TOWN_HEIGHT - 1;

		center_x = (loc_x1 + loc_x2) / 2;
		center_y = (loc_y1 + loc_y2) / 2;

		region_id = World.get_region_id(center_x, center_y);

		// nation_recno==0 for independent towns
		ai_town = nation_recno == 0 || NationArray[nation_recno].nation_type == NationBase.NATION_AI;
		ai_link_checked = true; // check the linked towns and firms connected only if ai_link_checked==0

		// the minimum rating a nation must have in order for an independent unit to join it
		independent_unit_join_nation_min_rating = 100 + Misc.Random(150);

		setup_date = Info.game_date;

		//-------- init resistance ------------//

		if (nationRecno == 0)
		{
			for (int i = 0; i < GameConstants.MAX_RACE; i++)
			{
				for (int j = 0; j < GameConstants.MAX_NATION; j++)
				{
					race_resistance_array[i, j] = 60.0 + Misc.Random(40);
					race_target_resistance_array[i, j] = -1;
				}
			}

			//--- some independent towns have higher than normal combat level for its defender ---//

			switch (Config.independent_town_resistance)
			{
				case Config.OPTION_LOW:
					town_combat_level = GameConstants.CITIZEN_COMBAT_LEVEL;
					break;

				case Config.OPTION_MODERATE:
					town_combat_level = 10 + Misc.Random(20);
					break;

				case Config.OPTION_HIGH:
					town_combat_level = 10 + Misc.Random(30);
					if (Misc.Random(5) == 0)
						town_combat_level += Misc.Random(30);
					break;

				//default:
				//err_here();
			}
		}

		//-------------------------------------//

		town_name_id = TownRes.get_new_name_id(raceId);

		set_world_matrix();

		setup_link();

		//-------- if this is an AI town ------//

		if (ai_town && nation_recno != 0)
		{
			NationArray[nation_recno].add_town_info(town_recno);

			update_base_town_status();
		}

		//------ set national auto policy -----//

		if (nation_recno != 0)
		{
			Nation nation = NationArray[nation_recno];

			set_auto_collect_tax_loyalty(nation.auto_collect_tax_loyalty);
			set_auto_grant_loyalty(nation.auto_grant_loyalty);
		}

		//--------- setup town network ---------//

		//town_network_pulsed = false;
		//town_network_recno = town_network_array.town_created(town_recno, nation_recno, linked_town_array, linked_town_count);
	}

	public void Deinit()
	{
		if (town_recno == 0)
			return;

		//--------- remove from town network ---------//

		//town_network_array.town_destroyed(town_recno);

		clear_defense_mode();

		//------- if it's an AI town --------//

		if (ai_town && nation_recno != 0)
			NationArray[nation_recno].del_town_info(town_recno);

		//------ if this town is the nation's larget town, reset it ----//

		if (nation_recno != 0 && NationArray[nation_recno].largest_town_recno == town_recno)
			NationArray[nation_recno].largest_town_recno = 0;

		//-----------------------------------//

		restore_world_matrix();

		release_link();

		//-- if there is a unit being trained when the town vanishes --//

		if (train_unit_recno != 0)
			UnitArray.disappear_in_town(train_unit_recno, town_recno);

		//------- if the current town is the selected -----//

		if (TownArray.selected_recno == town_recno)
		{
			TownArray.selected_recno = 0;
			Info.disp();
		}

		//------- reset parameters ---------//

		//TODO check
		//town_recno = 0;
		//town_network_recno = 0;
	}

	public void next_day()
	{
		int townRecno = town_recno;

		//------ update quality_of_life --------//

		update_quality_of_life();

		//---------- update population ----------//

		if (Info.TotalDays % 30 == town_recno % 30)
		{
			population_grow();
		}

		//------- update link status to camps ------//

		update_camp_link();

		//------ update target loyalty/resistance -------//

		if (Info.TotalDays % 15 == town_recno % 15)
		{
			if (nation_recno != 0)
			{
				update_target_loyalty();
			}
			else
			{
				update_target_resistance(); // update resistance for independent towns
			}
		}

		//------ update loyalty/resistance -------//

		if (Info.TotalDays % 5 == town_recno % 5)
		{
			if (nation_recno != 0)
			{
				update_loyalty();
			}
			else
			{
				update_resistance();
			}

			if (TownArray.IsDeleted(townRecno))
				return;
		}

		//------ think town people migration -------//

		if (ConfigAdv.town_migration && Info.TotalDays % 15 == town_recno % 15)
		{
			think_migrate();

			if (TownArray.IsDeleted(townRecno))
				return;
		}

		//-------- think about rebel -----//

		if (nation_recno != 0 && Info.TotalDays % 15 == town_recno % 15)
		{
			think_rebel();

			if (TownArray.IsDeleted(townRecno))
				return;
		}

		//-------- think about surrender -----//

		if (nation_recno != 0 && (Info.TotalDays % 15 == town_recno % 15 || average_loyalty() == 0))
		{
			think_surrender(); // for nation town only, independent town surrender is handled in update_resistance()

			if (TownArray.IsDeleted(townRecno))
				return;
		}

		//------- process training -------//

		//### begin alex 6/9 ###//
		/*if( nation_recno && train_unit_recno )
		{
			process_train();
	
			if( town_array.is_deleted(townRecno) )	// when the last peasant in the town is trained, the town disappear
				return;
		}*/
		if (nation_recno != 0)
		{
			if (train_unit_recno != 0)
			{
				process_train();
			}
			else
			{
				process_queue();
			}

			// when the last peasant in the town is trained, the town disappear
			if (TownArray.IsDeleted(townRecno))
				return;
		}
		//#### end alex 6/9 ####//

		//-------- process food ---------//

		if (nation_recno != 0)
		{
			process_food();

			if (TownArray.IsDeleted(townRecno))
				return;
		}

		//------ auto collect tax and auto grant -------//

		if (nation_recno != 0)
		{
			process_auto();

			if (TownArray.IsDeleted(townRecno))
				return;
		}

		//------ collect yearly tax -------//

		/*if( nation_recno && info.game_month==1 && info.game_day==1 )
		{
			collect_yearly_tax();
		}*/

		//------ catching spies -------//

		if (Info.TotalDays % 30 == town_recno % 30)
			SpyArray.catch_spy(Spy.SPY_TOWN, town_recno);

		if (TownArray.IsDeleted(townRecno))
			return;

		//-------- update visibility ---------//

		if (nation_recno == NationArray.player_recno ||
		    (nation_recno != 0 && NationArray[nation_recno].is_allied_with_player))
		{
			World.visit(loc_x1, loc_y1, loc_x2, loc_y2, GameConstants.EXPLORE_RANGE - 1);
		}

		//--- recheck no_neighbor_space after a period, there may be new space available now ---//

		if (no_neighbor_space && Info.TotalDays % 10 == town_recno % 10)
		{
			//--- for independent town, since we can't call find_best_firm_loc(), we just set no_neighbor_space to 0 every 6 months, if it still has no space, then no_neighbor_space will be set 1 again. ---//

			// whether it's FIRM_INN or not really doesn't matter, just any firm type will do
			if (nation_recno == 0 || NationArray[nation_recno].find_best_firm_loc(Firm.FIRM_INN,
				    loc_x1, loc_y1, out _, out _))
				no_neighbor_space = false;
		}

		//------ decrease penalties -----//

		if (accumulated_collect_tax_penalty > 0)
			accumulated_collect_tax_penalty--;

		if (accumulated_reward_penalty > 0)
			accumulated_reward_penalty--;

		if (accumulated_recruit_penalty > 0)
			accumulated_recruit_penalty--;

		if (accumulated_enemy_grant_penalty > 0)
			accumulated_enemy_grant_penalty--;

		//------------------------------------------------------------//
		// check for population for each town
		//------------------------------------------------------------//

		if (TownArray.IsDeleted(townRecno))
			return;
	}

	public void process_ai()
	{
		Nation ownNation = NationArray[nation_recno];

		//---- think about cancelling the base town status ----//

		if (Info.TotalDays % 30 == town_recno % 30)
		{
			update_base_town_status();
		}

		//------ think about granting villagers ---//

		if (Info.TotalDays % 30 == town_recno % 30)
		{
			think_reward();
		}

		//----- if this town should migrate now -----//

		if (should_ai_migrate())
		{
			if (Info.TotalDays % 30 == town_recno % 30)
			{
				think_ai_migrate();
			}

			return; // don't do anything else if the town is about to migrate
		}

		//------ think about building camps first -------//

		// if there is no space in the neighbor area for building a new firm.
		if (Info.TotalDays % 30 == town_recno % 30 && !no_neighbor_space && population > 1)
		{
			if (think_build_camp())
			{
				return;
			}
		}

		//----- the following are base town only functions ----//

		if (!is_base_town)
			return;

		//------ think about collecting tax ---//

		if (Info.TotalDays % 30 == (town_recno + 15) % 30)
		{
			think_collect_tax();
		}

		//---- think about scouting in an unexplored map ----//

		if (Info.TotalDays % 30 == (town_recno + 20) % 30)
		{
			think_scout();
		}

		//---- think about splitting the town ---//

		if (Info.TotalDays % 30 == town_recno % 30)
		{
			think_move_between_town();
			think_split_town();
		}

		//---- think about attacking firms/units nearby ---//

		if (Info.TotalDays % 30 == town_recno % 30)
		{
			if (think_attack_nearby_enemy())
			{
				return;
			}

			if (think_attack_linked_enemy())
			{
				return;
			}
		}

		//---- think about capturing linked enemy firms ----//

		if (Info.TotalDays % 60 == town_recno % 60)
		{
			think_capture_linked_firm();
		}

		//---- think about capturing enemy towns ----//

		if (Info.TotalDays % 120 == town_recno % 120)
		{
			think_capture_enemy_town();
		}

		//---- think about using spies on enemies ----//

		if (Info.TotalDays % 60 == (town_recno + 10) % 60)
		{
			if (think_spying_town())
			{
				return;
			}
		}

		//---- think about anti-spies activities ----//

		if (Info.TotalDays % 60 == (town_recno + 20) % 60)
		{
			if (think_counter_spy())
			{
				return;
			}
		}

		//--- think about setting up firms next to this town ---//

		// if there is no space in the neighbor area for building a new firm.
		if (Info.TotalDays % 30 == town_recno % 30 && !no_neighbor_space && population >= 5)
		{
			if (think_build_market())
			{
				return;
			}

			//--- the following functions will only be called when the nation has at least a mine ---//

			// don't build other structures if there are untapped raw sites and our nation still doesn't have any
			if (SiteArray.untapped_raw_count > 0 && ownNation.ai_mine_array.Count == 0 && ownNation.true_profit_365days() < 0)
			{
				return;
			}

			//---- only build the following if we have enough food ----//

			if (ownNation.ai_has_enough_food())
			{
				if (think_build_research())
				{
					return;
				}

				if (think_build_war_factory())
				{
					return;
				}

				if (think_build_base())
				{
					return;
				}
			}

			//-------- think build inn ---------//

			think_build_inn();
		}
	}

	public void assign_unit(int unitRecno)
	{
		Unit unit = UnitArray[unitRecno];

		if (population >= GameConstants.MAX_TOWN_POPULATION || unit.rank_id == Unit.RANK_KING)
		{
			unit.stop2();
			//----------------------------------------------------------------------//
			// codes for handle_blocked_move 
			// set unit_group_id to a different value s.t. the members in this group
			// will not blocked by this unit.
			//----------------------------------------------------------------------//
			unit.unit_group_id = UnitArray.cur_group_id++;
			return;
		}

		//------ if the unit is a general, demote it first -------//

		if (unit.rank_id == Unit.RANK_GENERAL)
			unit.set_rank(Unit.RANK_SOLDIER);

		//-------- increase population -------//

		inc_pop(unit.race_id, false, unit.loyalty);

		//---- free the unit's name from the name database ----//

		RaceRes[unit.race_id].free_name_id(unit.name_id);

		//----- if it's a town defending unit -----//

		if (unit.unit_mode == UnitConstants.UNIT_MODE_DEFEND_TOWN)
		{
			// if the defender defeat the attackers and return the town with victory, the resistance will further increase
			const int RESISTANCE_INCREASE = 2;

			//---------------------------------------------//
			//
			// If this unit is a defender of the town, add back the
			// loyalty which was deducted from the defender left the
			// town.
			//
			//---------------------------------------------//

			if (unit.nation_recno == nation_recno && unit.unit_mode_para == town_recno)
			{
				//-- if the unit is a town defender, skill.skill_level is temporary used for storing the loyalty that will be added back to the town if the defender returns to the town

				int loyaltyInc = unit.skill.skill_level;

				if (nation_recno != 0) // set the loyalty later for nation_recno > 0
				{
					change_loyalty(unit.race_id, loyaltyInc);
				}
				else
				{
					for (int i = 0; i < GameConstants.MAX_NATION; i++) // set the resistance
					{
						double newResistance = race_resistance_array[unit.race_id - 1, i] + loyaltyInc +
						                       RESISTANCE_INCREASE;

						race_resistance_array[unit.race_id - 1, i] = Math.Min(newResistance, 100);
					}
				}
			}
		}

		//------ if the unit is a spy -------//

		if (unit.spy_recno > 0)
		{
			SpyArray[unit.spy_recno].set_place(Spy.SPY_TOWN, town_recno);
			unit.spy_recno = 0; // reset it so Unit::deinit() won't delete the spy
		}

		//----- if this is an independent town -----//

		if (nation_recno == 0) // update the town people's combat level with this unit's combat level
		{
			town_combat_level = (town_combat_level * (population - 1) + unit.skill.combat_level) / population;
		}

		//--------- delete the unit --------//

		// the unit disappear from the map because it has moved into a town
		UnitArray.disappear_in_town(unitRecno, town_recno);
	}

	public int recruit(int trainSkillId, int raceId, byte remoteAction)
	{
		//---- we can't train a new one when there is one currently under training ---//

		if (trainSkillId >= 1 && train_unit_recno != 0)
			return 0;

		//--------------------------------------------//

		if (raceId == 0)
		{
			//TODO rewrite
			//if (browse_race.recno() > race_filter())
				//return 0;

			//raceId = race_filter(browse_race.recno());
		}

		//if( !remoteAction && remote.is_enable() )
		//{
		// packet structure : <town recno> <skill id> <race id>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_TOWN_RECRUIT, 3*sizeof(short));
		//shortPtr[0] = town_recno;
		//shortPtr[1] = trainSkillId;
		//shortPtr[2] = raceId;
		//return 0;
		//}

		//---- check if there are units of the race ready for training ----//

		int recruitableCount = recruitable_race_pop(raceId, true);

		if (recruitableCount == 0)
			return 0;

		//err_when( recruitableCount < 0 );		// 1-allow recruiting spies

		//-------- create an unit ------//

		int townRecno = town_recno;
		// save this town's info that is needed as promote_pop() will delete Town if all population of the town are promoted
		int nationRecno = nation_recno;

		//--- if there are spies in this town, chances are that they will be mobilized ---//

		// 1-allow recruiting spies
		bool shouldTrainSpy = race_spy_count_array[raceId - 1] >= Misc.Random(recruitableCount) + 1;

		//---- if we are trying to train an enemy to our spy, then... -----//

		if (shouldTrainSpy && trainSkillId == Skill.SKILL_SPYING)
		{
			//-- if there are other non-spy units in the town, then train the other and don't train the spy --//

			if (recruitableCount > race_spy_count_array[raceId - 1])
			{
				shouldTrainSpy = false;
			}
			//--- if all remaining units are spies, when you try to train one, all of them will become mobilized ---//

			else
			{
				int spyRecno = SpyArray.find_town_spy(town_recno, raceId, 1);

				Spy spy = SpyArray[spyRecno];

				if (spy.mobilize_town_spy() == 0)
					return 0;

				spy.change_cloaked_nation(spy.true_nation_recno);

				return 0;
			}
		}

		//------- if we should train a spy --------//

		int unitRecno = 0;

		if (shouldTrainSpy)
		{
			//-----------------------------------------------------//
			// Spies from other nations will first be mobilized,
			// when all peasants and spies are mobilized and
			// the only ones left in the town are spies from our
			// nation, then mobilize them finally.
			//-----------------------------------------------------//

			for (int mobileNationType = 1; unitRecno == 0 && mobileNationType <= 2; mobileNationType++)
			{
				if (mobileNationType == 2) // only mobilize our own spies are there are the only ones in the town
				{
					if (recruitable_race_pop(raceId, true) >
					    race_spy_count_array[raceId - 1]) // 1-allow recruiting spies
						break;
				}

				foreach (Spy spy in SpyArray.EnumerateRandom())
				{
					if (spy.spy_place == Spy.SPY_TOWN && spy.spy_place_para == town_recno && spy.race_id == raceId)
					{
						// only mobilize spies from other nations, don't mobilize spies of our own nation
						if (mobileNationType == 1)
						{
							if (spy.true_nation_recno == nation_recno)
								continue;
						}

						// the parameter is whether decreasing the population immediately, if decrease immediately in recruit mode, not in training mode, 1-mobilize spies
						unitRecno = spy.mobilize_town_spy(trainSkillId == -1);
						break;
					}
				}
			}
		}

		//-------- mobilize normal peasant units -------//

		if (unitRecno == 0)
		{
			// the 2nd parameter is whether decreasing the population immediately, if decrease immediately in recruit mode, not in training mode, 2nd para 0-don't mobilize spies
			unitRecno = mobilize_town_people(raceId, trainSkillId == -1, false);
		}

		if (unitRecno == 0)
			return 0;

		Unit unit = UnitArray[unitRecno];

		//-------- training skill -----------//

		if (trainSkillId > 0)
		{
			if (trainSkillId == Skill.SKILL_SPYING)
			{
				unit.spy_recno = SpyArray.AddSpy(unitRecno, GameConstants.TRAIN_SKILL_LEVEL).spy_recno;
			}
			else
			{
				if (trainSkillId == Skill.SKILL_LEADING) // also increase the combat level for leadership skill training
					unit.set_combat_level(GameConstants.TRAIN_SKILL_LEVEL);

				unit.skill.skill_id = trainSkillId;
				unit.skill.skill_level = GameConstants.TRAIN_SKILL_LEVEL;
			}

			NationArray[nationRecno].add_expense(NationBase.EXPENSE_TRAIN_UNIT, GameConstants.TRAIN_SKILL_COST);
		}
		else
		{
			//------ recruitment without training decreases loyalty --------//

			recruit_dec_loyalty(raceId);

			if (unit.is_own())
			{
				SERes.far_sound(unit.cur_x_loc(), unit.cur_y_loc(), 1, 'S', unit.sprite_id, "RDY");
			}
		}

		//---- training solider or skilled unit takes time ----//

		if (trainSkillId >= 0)
		{
			train_unit_recno = unitRecno;
			startTrainFrameNumber = Sys.Instance.FrameNumber; // as an offset for displaying the progress bar correctly

			unit.deinit_sprite();
			unit.unit_mode = UnitConstants.UNIT_MODE_UNDER_TRAINING;
			unit.unit_mode_para = town_recno;
		}

		//--- mobilize_pop() will delete the current Town if population goes down to 0 ---//

		if (town_recno == TownArray.selected_recno)
		{
			if (TownArray.IsDeleted(townRecno))
				Info.disp();
		}

		return unitRecno;
	}

	public int recruit_dec_loyalty(int raceId, bool decNow = true)
	{
		double loyaltyDec =
			Math.Min(5.0, Convert.ToDouble(GameConstants.MAX_TOWN_POPULATION) / race_pop_array[raceId - 1]);

		if (ConfigAdv.fix_recruit_dec_loyalty)
		{
			loyaltyDec += accumulated_recruit_penalty / 5.0;
			loyaltyDec = Math.Min(loyaltyDec, 10);
		}

		//------ recruitment without training decreases loyalty --------//

		if (decNow)
		{
			if (!ConfigAdv.fix_recruit_dec_loyalty)
			{
				loyaltyDec += accumulated_recruit_penalty / 5.0;

				loyaltyDec = Math.Min(loyaltyDec, 10);
			}

			accumulated_recruit_penalty += 5;

			//-------------------------------------//

			race_loyalty_array[raceId - 1] -= loyaltyDec;

			if (race_loyalty_array[raceId - 1] < 0)
				race_loyalty_array[raceId - 1] = 0.0;
		}

		if (ConfigAdv.fix_recruit_dec_loyalty)
		{
			// for ai estimating, round integer conversion up, do not truncate
			return (int)Math.Ceiling(loyaltyDec);
		}

		return Convert.ToInt32(loyaltyDec);
	}

	public void cancel_train_unit()
	{
		if (train_unit_recno != 0)
		{
			//unit_array.disappear_in_town(train_unit_recno, town_recno);
			//train_unit_recno = 0;

			Unit unit = UnitArray[train_unit_recno];
			// check whether the unit is already a spy before training
			if (unit.spy_recno != 0 && unit.skill.skill_id != 0)
			{
				SpyArray[unit.spy_recno].set_place(Spy.SPY_TOWN, town_recno);
				unit.spy_recno = 0; // reset it so Unit::deinit() won't delete the spy
			}

			UnitArray.disappear_in_town(train_unit_recno, town_recno);
			train_unit_recno = 0;
		}
	}

	public bool form_new_nation()
	{
		if (NationArray.nation_count >= GameConstants.MAX_NATION)
			return false;

		//----- determine the race with most population -----//

		int maxPop = 0, raceId = 0;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (race_pop_array[i] > maxPop)
			{
				maxPop = race_pop_array[i];
				raceId = i + 1;
			}
		}


		//---- create the king of the new nation ----//

		int unitId = RaceRes[raceId].basic_unit_id;
		int xLoc = loc_x1, yLoc = loc_y1; // xLoc & yLoc are used for returning results
		SpriteInfo spriteInfo = SpriteRes[UnitRes[unitId].sprite_id];

		if (!World.locate_space(ref xLoc, ref yLoc, loc_x2, loc_y2, spriteInfo.loc_width, spriteInfo.loc_height))
			return false;

		//--------- create a new nation ---------//

		int nationRecno = NationArray.new_nation(NationBase.NATION_AI, raceId, NationArray.random_unused_color());

		//-------- create the king --------//

		Unit kingUnit = UnitArray.AddUnit(unitId, nationRecno, Unit.RANK_KING, 100, xLoc, yLoc);

		kingUnit.skill.skill_id = Skill.SKILL_LEADING;
		kingUnit.skill.skill_level = 50 + Misc.Random(51);

		kingUnit.set_combat_level(70 + Misc.Random(31));

		NationArray[nationRecno].set_king(kingUnit.sprite_recno, 1); // 1-this is the first king of the nation

		dec_pop(raceId, false); // 0-the unit doesn't have a job

		//------ set the nation of the rebel town -----//

		set_nation(nationRecno); // set the town at last because set_nation() will delete the Town object

		//------ increase the loyalty of the town -----//

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
			race_loyalty_array[i] = 70 + Misc.Random(20); // 70 to 90 initial loyalty

		//--------- add news ----------//

		NewsArray.new_nation(nationRecno);

		//--- random extra beginning advantages -----//

		int mobileCount;
		Nation nation = NationArray[nationRecno];

		switch (Misc.Random(10))
		{
			case 1: // knowledge of weapon in the beginning.
				TechRes[Misc.Random(TechRes.tech_info_array.Length) + 1].set_nation_tech_level(nationRecno, 1);
				break;

			case 2: // random additional cash
				nation.cash += Misc.Random(5000);
				break;

			case 3: // random additional food
				nation.food += Misc.Random(5000);
				break;

			case 4: // random additional skilled units
				mobileCount = Misc.Random(5) + 1;

				// 0-don't recruit spies
				for (int i = 0; i < mobileCount && recruitable_race_pop(raceId, false) > 0; i++)
				{
					int unitRecno = mobilize_town_people(raceId, true, false); // 1-dec pop, 0-don't mobilize spies

					if (unitRecno != 0)
					{
						Unit unit = UnitArray[unitRecno];

						//------- randomly set a skill -------//

						int skillId = Misc.Random(Skill.MAX_TRAINABLE_SKILL) + 1;
						int loopCount = 0; // no spying skill

						// no spy skill as skill_id can't be set as SKILL_SPY, for spies, spy_recno must be set instead
						while (skillId == Skill.SKILL_SPYING)
						{
							if (++skillId > Skill.MAX_TRAINABLE_SKILL)
							{
								skillId = 1;
							}

						}

						unit.skill.skill_id = skillId;
						unit.skill.skill_level = 50 + Misc.Random(50);
						unit.set_combat_level(50 + Misc.Random(50));
					}
					else
						break;
				}

				break;
		}

		NationArray.update_statistic();

		return nationRecno != 0;
	}

	public bool can_recruit(int raceId)
	{
		//----------------------------------------------------//
		// Cannot recruit when you have none of your own camps
		// linked to this town, but your enemies have camps
		// linked to it.
		//----------------------------------------------------//

		if (!has_linked_own_camp && has_linked_enemy_camp)
			return false;

		if (recruitable_race_pop(raceId, true) == 0)
			return false;

		//---------------------------------//

		int minRecruitLoyalty = GameConstants.MIN_RECRUIT_LOYALTY;

		//--- for the AI, only recruit if the loyalty still stay at 30 after recruiting the unit ---//

		// 0-don't actually decrease it, just return the loyalty to be decreased.
		if (ai_town && nation_recno != 0)
			minRecruitLoyalty += 3 + recruit_dec_loyalty(raceId, false);

		return race_loyalty_array[raceId - 1] >= minRecruitLoyalty;
	}

	public bool can_train(int raceId)
	{
		int recruitableCount = jobless_race_pop_array[raceId - 1];

		return has_linked_own_camp && recruitableCount > 0 &&
		       NationArray[nation_recno].cash > GameConstants.TRAIN_SKILL_COST;
	}

	// if not called by Town::migrate, don't set migrateNow to TRUE
	public bool can_migrate(int destTownRecno, bool migrateNow = false, int raceId = 0)
	{
		if (raceId == 0)
		{
			//TODO rewrite
			//raceId = browse_selected_race_id();

			if (raceId == 0)
				return false;
		}

		Town destTown = TownArray[destTownRecno];

		if (destTown.population >= GameConstants.MAX_TOWN_POPULATION)
			return false;

		//---- if there are still jobless units ----//

		if (recruitable_race_pop(raceId, true) > 0) // 1-allow migrate spy 
		{
			if (migrateNow)
				move_pop(destTown, raceId, false); // 0-doesn't have job 

			return true;
		}

		//--- if there is no jobless units left -----//

		if (race_pop_array[raceId - 1] > 0)
		{
			//---- scan for firms that are linked to this town ----//

			for (int i = linked_firm_array.Count - 1; i >= 0; i--)
			{
				Firm firm = FirmArray[linked_firm_array[i]];

				//---- only for firms whose workers live in towns ----//

				if (!FirmRes[firm.firm_id].live_in_town)
					continue;

				//---- if the target town is within the effective range of this firm ----//

				if (Misc.rects_distance(destTown.loc_x1, destTown.loc_y1, destTown.loc_x2, destTown.loc_y2,
					    firm.loc_x1, firm.loc_y1, firm.loc_x2, firm.loc_y2) >
				    InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE)
				{
					continue;
				}

				//------- scan for workers -----------//

				foreach (Worker worker in firm.workers)
				{
					//--- if the worker lives in this town ----//

					if (worker.race_id == raceId && worker.town_recno == town_recno)
					{
						if (migrateNow)
						{
							if (FirmRes[firm.firm_id].live_in_town)
								worker.town_recno = destTownRecno;
							else
								worker.town_recno = 0;

							move_pop(destTown, raceId, true); // 1-has job
						}

						return true;
					}
				}
			}
		}

		return false;
	}

	public void move_pop(Town destTown, int raceId, bool hasJob)
	{
		//--- if the only pop of this race in the source town are spies ---//

		// only for peasant, for job unit, spy_place==Spy.SPY_FIRM and it isn't related to race_spy_count_array[]
		if (!hasJob)
		{

			if (race_spy_count_array[raceId - 1] == jobless_race_pop_array[raceId - 1])
			{
				int spySeqId = Misc.Random(race_spy_count_array[raceId - 1]) + 1; // randomly pick one of the spies

				int spyRecno = SpyArray.find_town_spy(town_recno, raceId, spySeqId);

				SpyArray[spyRecno].spy_place_para = destTown.town_recno; // set the place_para of the spy

				race_spy_count_array[raceId - 1]--;
				destTown.race_spy_count_array[raceId - 1]++;
			}
		}

		//------------------------------------------//

		destTown.inc_pop(raceId, hasJob, Convert.ToInt32(race_loyalty_array[raceId - 1]));

		// the unit doesn't have a job - this must be called finally as dec_pop() will have the whole town deleted if there is only one pop left
		dec_pop(raceId, hasJob);
	}

	public int pick_random_race(bool pickNonRecruitableAlso, bool pickSpyFlag)
	{
		int totalPop = 0;

		if (pickNonRecruitableAlso)
		{
			totalPop = population;
		}
		else
		{
			totalPop = jobless_population;
			if (train_unit_recno != 0)
				totalPop -= 1;

			if (!pickSpyFlag) // if don't pick spies
			{
				for (int i = 0; i < GameConstants.MAX_RACE; i++)
					totalPop -= race_spy_count_array[i];

				if (totalPop == -1) // it can be -1 if the unit being trained is a spy
					totalPop = 0;
			}
		}

		if (totalPop == 0)
			return 0;

		int randomPersonId = Misc.Random(totalPop) + 1;
		int popSum = 0;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (pickNonRecruitableAlso)
				popSum += race_pop_array[i];
			else
				popSum += recruitable_race_pop(i + 1, pickSpyFlag);

			if (randomPersonId <= popSum)
				return i + 1;
		}

		return 0;
	}

	public int camp_influence(int unitRecno)
	{
		Unit unit = UnitArray[unitRecno];
		Nation nation = NationArray[unit.nation_recno]; // nation of the unit

		int thisInfluence = unit.skill.get_skill(Skill.SKILL_LEADING) * 2 / 3; // 66% of the leadership

		if (RaceRes.is_same_race(nation.race_id, unit.race_id))
			thisInfluence += thisInfluence / 3; // 33% bonus if the king's race is also the same as the general

		thisInfluence += Convert.ToInt32(nation.reputation / 2);

		thisInfluence = Math.Min(100, thisInfluence);

		return thisInfluence;
	}

	public void setup_link()
	{
		//-----------------------------------------------------------------------------//
		// check the connected firms location and structure if ai_link_checked is true
		//-----------------------------------------------------------------------------//
		if (ai_town)
			ai_link_checked = false;

		//----- build town-to-firm link relationship -------//

		int firmRecno, defaultLinkStatus;

		foreach (Firm firm in FirmArray)
		{
			FirmInfo firmInfo = FirmRes[firm.firm_id];

			if (!firmInfo.is_linkable_to_town)
				continue;

			//---------- check if the firm is close enough to this firm -------//

			if (Misc.rects_distance(firm.loc_x1, firm.loc_y1, firm.loc_x2, firm.loc_y2,
				    loc_x1, loc_y1, loc_x2, loc_y2) > InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE)
			{
				continue;
			}

			//------ check if both are on the same terrain type ------//

			if (World.get_loc(firm.center_x, firm.center_y).is_plateau()
			    != World.get_loc(center_x, center_y).is_plateau())
			{
				continue;
			}

			//------- determine the default link status ------//

			// if the two firms are of the same nation, get the default link status which is based on the types of the firms
			// if the two firms are of different nations, default link status is both side disabled
			if (firm.nation_recno == nation_recno)
				defaultLinkStatus = InternalConstants.LINK_EE;
			else
				defaultLinkStatus = InternalConstants.LINK_DD;

			//----- a town cannot disable a camp's link to it ----//

			if (firm.firm_id == Firm.FIRM_CAMP) // for capturing the town
				defaultLinkStatus = InternalConstants.LINK_EE;

			//-------- add the link now -------//

			linked_firm_array.Add(firm.firm_recno);
			linked_firm_enable_array.Add(defaultLinkStatus);

			// now link from the firm's side
			if (defaultLinkStatus == InternalConstants.LINK_ED) // Reverse the link status for the opposite linker
				defaultLinkStatus = InternalConstants.LINK_DE;

			else if (defaultLinkStatus == InternalConstants.LINK_DE)
				defaultLinkStatus = InternalConstants.LINK_ED;

			firm.linked_town_array.Add(town_recno);
			firm.linked_town_enable_array.Add(defaultLinkStatus);

			if (firm.firm_ai)
				firm.ai_link_checked = false;
		}

		//----- build town-to-town link relationship -------//

		foreach (Town town in TownArray)
		{
			if (town.town_recno == town_recno)
				continue;

			//------ check if the town is close enough to this town -------//

			if (Misc.rects_distance(town.loc_x1, town.loc_y1, town.loc_x2, town.loc_y2,
				    loc_x1, loc_y1, loc_x2, loc_y2) > InternalConstants.EFFECTIVE_TOWN_TOWN_DISTANCE)
			{
				continue;
			}

			//------ check if both are on the same terrain type ------//

			if (World.get_loc(town.center_x, town.center_y).is_plateau()
			    != World.get_loc(center_x, center_y).is_plateau())
			{
				continue;
			}

			//------- determine the default link status ------//

			defaultLinkStatus = InternalConstants.LINK_EE;

			//-------- add the link now -------//

			linked_town_array.Add(town.town_recno);
			linked_town_enable_array.Add(defaultLinkStatus);

			// now link from the other town's side
			if (defaultLinkStatus == InternalConstants.LINK_ED) // Reverse the link status for the opposite linker
				defaultLinkStatus = InternalConstants.LINK_DE;

			else if (defaultLinkStatus == InternalConstants.LINK_DE)
				defaultLinkStatus = InternalConstants.LINK_ED;

			town.linked_town_array.Add(town_recno);
			town.linked_town_enable_array.Add(defaultLinkStatus);

			if (town.ai_town)
				town.ai_link_checked = false;
		}
	}

	public void release_link()
	{
		//------ release linked firms ------//

		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			Firm firm = FirmArray[linked_firm_array[i]];
			firm.release_town_link(town_recno);

			if (firm.firm_ai)
				firm.ai_link_checked = false;
		}

		//------ release linked towns ------//

		for (int i = 0; i < linked_town_array.Count; i++)
		{
			Town town = TownArray[linked_town_array[i]];
			town.release_town_link(town_recno);

			if (town.ai_town)
				town.ai_link_checked = false;
		}
	}

	public void release_firm_link(int releaseFirmRecno)
	{
		//-----------------------------------------------------------------------------//
		// check the connected firms location and structure if ai_link_checked is true
		//-----------------------------------------------------------------------------//
		if (ai_town)
			ai_link_checked = false;

		for (int i = linked_firm_array.Count - 1; i >= 0; i--)
		{
			if (linked_firm_array[i] == releaseFirmRecno)
			{
				linked_firm_array.RemoveAt(i);
				linked_firm_enable_array.RemoveAt(i);
				return;
			}
		}
	}

	public void release_town_link(int releaseTownRecno)
	{
		//-----------------------------------------------------------------------------//
		// check the connected firms location and structure if ai_link_checked is true
		//-----------------------------------------------------------------------------//
		if (ai_town)
			ai_link_checked = false;

		for (int i = linked_town_array.Count - 1; i >= 0; i--)
		{
			if (linked_town_array[i] == releaseTownRecno)
			{
				linked_town_array.RemoveAt(i);
				linked_town_enable_array.RemoveAt(i);
				return;
			}
		}
	}

	public int linked_active_camp_count()
	{
		int linkedCount = 0;

		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			if (linked_firm_enable_array[i] == InternalConstants.LINK_EE)
			{
				Firm firm = FirmArray[linked_firm_array[i]];

				if (firm.firm_id == Firm.FIRM_CAMP && firm.overseer_recno != 0)
				{
					linkedCount++;
				}
			}
		}

		return linkedCount;
	}

	public double linked_camp_soldiers_count()
	{
		double townSoldiersCount = 0.0;
		for (int firmIndex = 0; firmIndex < linked_firm_array.Count; firmIndex++)
		{
			Firm firm = FirmArray[linked_firm_array[firmIndex]];

			if (firm.nation_recno != nation_recno || firm.firm_id != Firm.FIRM_CAMP)
				continue;

			FirmCamp firmCamp = (FirmCamp)firm;

			double linkedTownsCount = 0.0;
			for (int townIndex = 0; townIndex < firmCamp.linked_town_array.Count; townIndex++)
			{
				if (TownArray.IsDeleted(firmCamp.linked_town_array[townIndex]))
					continue;

				Town firmTown = TownArray[firmCamp.linked_town_array[townIndex]];

				if (firmTown.nation_recno != nation_recno)
					continue;

				linkedTownsCount += 1.0;
			}

			if (linkedTownsCount > 0.0)
			{
				townSoldiersCount += (firmCamp.workers.Count + firmCamp.patrol_unit_array.Count +
				                      firmCamp.coming_unit_array.Count) / linkedTownsCount;
			}
		}

		return townSoldiersCount;
	}

	public bool can_toggle_firm_link(int firmRecno)
	{
		if (nation_recno == 0) // cannot toggle for independent town
			return false;

		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			if (linked_firm_array[i] != firmRecno)
				continue;

			Firm firm = FirmArray[linked_firm_array[i]];

			switch (firm.firm_id)
			{
				//-- you can only toggle a link to a camp if the camp is yours --//

				case Firm.FIRM_CAMP:
					return firm.nation_recno == nation_recno;

				//--- town to market link is governed by trade treaty and cannot be toggled ---//

				case Firm.FIRM_MARKET:
					return false; // !nation_array[nation_recno].get_relation(firm.nation_recno).trade_treaty;

				default:
					return FirmRes[firm.firm_id].is_linkable_to_town;
			}
		}

		return false;
	}

	public void update_camp_link()
	{
		//--- enable the link of the town's side to all linked camps ---//

		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			Firm firm = FirmArray[linked_firm_array[i]];

			if (firm.firm_id != Firm.FIRM_CAMP)
				continue;

			//--- don't set it if the town and camp both belong to a human player, the player will set it himself ---//

			if (firm.nation_recno == nation_recno &&
			    nation_recno != 0 && !NationArray[nation_recno].is_ai())
			{
				continue;
			}

			//--------------------------------------------//

			toggle_firm_link(i + 1, true, InternalConstants.COMMAND_AUTO);
		}

		//------- update camp link status -------//

		has_linked_own_camp = false;
		has_linked_enemy_camp = false;

		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			if (linked_firm_enable_array[i] != InternalConstants.LINK_EE)
				continue;

			Firm firm = FirmArray[linked_firm_array[i]];

			if (firm.firm_id != Firm.FIRM_CAMP || firm.overseer_recno == 0)
				continue;

			if (firm.nation_recno == nation_recno)
				has_linked_own_camp = true;
			else
				has_linked_enemy_camp = true;
		}
	}

	public void init_pop(int raceId, int addPop, int loyalty, bool hasJob = false, bool firstInit = false)
	{
		if (population >= GameConstants.MAX_TOWN_POPULATION)
			return;

		int addPopulation = Math.Min(addPop, GameConstants.MAX_TOWN_POPULATION - population);

		//-------- update population ---------//

		race_pop_array[raceId - 1] += addPopulation;
		population += addPopulation;

		if (!hasJob)
		{
			jobless_population += addPopulation;
			jobless_race_pop_array[raceId - 1] += addPopulation;
		}

		//------- update loyalty --------//

		if (firstInit) // first initialization at the beginning of the game
		{
			if (nation_recno != 0)
			{
				race_loyalty_array[raceId - 1] = loyalty;
				race_target_loyalty_array[raceId - 1] = loyalty;
			}
			else
			{
				for (int j = 0; j < GameConstants.MAX_NATION; j++) // reset resistance for non-existing races
				{
					race_resistance_array[raceId - 1, j] = loyalty;
					race_target_resistance_array[raceId - 1, j] = loyalty;
				}
			}
		}
		else
		{
			if (nation_recno != 0)
			{
				race_loyalty_array[raceId - 1] =
					(race_loyalty_array[raceId - 1] * (race_pop_array[raceId - 1] - addPopulation)
					 + loyalty * addPopulation) / race_pop_array[raceId - 1];

				race_target_loyalty_array[raceId - 1] = Convert.ToInt32(race_loyalty_array[raceId - 1]);
			}
			else
			{
				for (int j = 0; j < GameConstants.MAX_NATION; j++) // reset resistance for non-existing races
				{
					race_resistance_array[raceId - 1, j] =
						(race_resistance_array[raceId - 1, j] * (race_pop_array[raceId - 1] - addPopulation)
						 + loyalty * addPopulation) / race_pop_array[raceId - 1];

					race_target_resistance_array[raceId - 1, j] = Convert.ToInt32(race_resistance_array[raceId - 1, j]);
				}
			}
		}

		//---------- update town parameters ----------//

		TownArray.distribute_demand();

		if (nation_recno != 0)
			update_loyalty();
		else
			update_resistance();
	}

	public void inc_pop(int raceId, bool unitHasJob, int unitLoyalty)
	{
		//---------- increase population ----------//

		population++;
		race_pop_array[raceId - 1]++;

		if (!unitHasJob)
		{
			jobless_population++;
			jobless_race_pop_array[raceId - 1]++;
		}

		//------- update loyalty --------//

		if (nation_recno != 0) // if the unit has an unit
		{
			race_loyalty_array[raceId - 1] =
				(race_loyalty_array[raceId - 1] * (race_pop_array[raceId - 1] - 1) + unitLoyalty)
				/ race_pop_array[raceId - 1];
		}

		//-- if the race's population exceeds the capacity of the town layout --//

		if (race_pop_array[raceId - 1] > max_race_pop_array[raceId - 1])
		{
			auto_set_layout();
		}
	}

	public void dec_pop(int raceId, bool unitHasJob)
	{
		population--;
		race_pop_array[raceId - 1]--;

		if (!unitHasJob)
		{
			jobless_population--;
			jobless_race_pop_array[raceId - 1]--;
		}

		//------- if all the population are gone --------//

		if (population == 0) // it will be deleted in TownArray::process()
		{
			if (nation_recno == NationArray.player_recno)
				NewsArray.town_abandoned(town_recno);

			Deinit();
			return;
		}

		//-- if the race's population drops to too low, change the town layout --//

		if (race_pop_array[raceId - 1] <= max_race_pop_array[raceId - 1] - TownRes.POPULATION_PER_HOUSE)
		{
			auto_set_layout();
		}
	}

	public bool has_linked_camp(int nationRecno, bool needOverseer)
	{
		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			Firm firm = FirmArray[linked_firm_array[i]];

			if (firm.firm_id == Firm.FIRM_CAMP && firm.nation_recno == nationRecno)
			{
				if (!needOverseer || firm.overseer_recno != 0)
					return true;
			}
		}

		return false;
	}

	public void auto_set_layout()
	{
		layout_id = think_layout_id();

		TownLayout townLayout = TownRes.get_layout(layout_id);
		//TownSlot   firstTownSlot = TownRes.get_slot(townLayout.first_slot_recno);
		int[] raceNeedBuildCount = new int[GameConstants.MAX_RACE];

		for (int i = 0; i < slot_object_id_array.Length; i++)
			slot_object_id_array[i] = 0;
		for (int i = 0; i < max_race_pop_array.Length; i++)
			max_race_pop_array[i] = 0;
		for (int i = 0; i < raceNeedBuildCount.Length; i++)
			raceNeedBuildCount[i] = 0;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (race_pop_array[i] > 0)
				raceNeedBuildCount[i] += (race_pop_array[i] - 1) / TownRes.POPULATION_PER_HOUSE + 1;
		}

		//--- assign the first house to each race, each present race will at least have one house ---//

		int firstRaceId = ConfigAdv.GetRandomRace(); // random match
		int raceId = firstRaceId;

		for (int i = 0; i < townLayout.slot_count; i++)
		{
			//TODO check it
			TownSlot firstTownSlot = TownRes.get_slot(townLayout.first_slot_recno + i);
			if (firstTownSlot.build_type == TownSlot.TOWN_OBJECT_HOUSE)
			{
				while (true) // next race
				{
					if (++raceId > GameConstants.MAX_RACE)
						raceId = 1;

					if (raceId == ((firstRaceId == GameConstants.MAX_RACE)
						    ? 1
						    : firstRaceId + 1)) // finished the first house for all races
						goto label_distribute_house;

					if (raceNeedBuildCount[raceId - 1] >
					    0) // if this race need buildings, skip all races that do not need buildings
						break;
				}

				slot_object_id_array[i] = TownRes.scan_build(townLayout.first_slot_recno + i, raceId);

				raceNeedBuildCount[raceId - 1]--;
				max_race_pop_array[raceId - 1] += TownRes.POPULATION_PER_HOUSE;
			}
		}

		//------- distribute the remaining houses -------//

		label_distribute_house:

		int bestRaceId, maxNeedBuildCount;

		for (int i = 0; i < townLayout.slot_count; i++)
		{
			//TODO check it
			TownSlot firstTownSlot = TownRes.get_slot(townLayout.first_slot_recno + i);
			if (firstTownSlot.build_type == TownSlot.TOWN_OBJECT_HOUSE && slot_object_id_array[i] == 0)
			{
				bestRaceId = 0;
				maxNeedBuildCount = 0;

				for (raceId = 1; raceId <= GameConstants.MAX_RACE; raceId++)
				{
					if (raceNeedBuildCount[raceId - 1] > maxNeedBuildCount)
					{
						bestRaceId = raceId;
						maxNeedBuildCount = raceNeedBuildCount[raceId - 1];
					}
				}

				if (bestRaceId == 0) // all races have assigned with their needed houses
					break;

				slot_object_id_array[i] = TownRes.scan_build(townLayout.first_slot_recno + i, bestRaceId);
				raceNeedBuildCount[bestRaceId - 1]--;
				max_race_pop_array[bestRaceId - 1] += TownRes.POPULATION_PER_HOUSE;
			}
		}

		//------- set plants in the town layout -------//

		for (int i = 0; i < townLayout.slot_count; i++)
		{
			//TODO check it
			TownSlot firstTownSlot = TownRes.get_slot(townLayout.first_slot_recno + i);
			switch (firstTownSlot.build_type)
			{
				case TownSlot.TOWN_OBJECT_PLANT:
					slot_object_id_array[i] =
						PlantRes.scan(0, 'T',
							0); // 'T'-town only, 1st 0-any zone area, 2nd 0-any terain type, 3rd-age level
					break;

				case TownSlot.TOWN_OBJECT_FARM:
					slot_object_id_array[i] = firstTownSlot.build_code;
					break;

				case TownSlot.TOWN_OBJECT_HOUSE:
					if (slot_object_id_array[i] == 0)
						slot_object_id_array[i] = TownRes.scan_build(townLayout.first_slot_recno + i, ConfigAdv.GetRandomRace());
					break;
			}
		}
	}

	public void set_nation(int newNationRecno)
	{
		if (nation_recno == newNationRecno)
			return;

		//--------- update town network (pre-step) ---------//
		//town_network_array.town_pre_changing_nation(town_recno);


		clear_defense_mode();

		//------------- stop all actions to attack this town ------------//
		UnitArray.stop_attack_town(town_recno);
		RebelArray.stop_attack_town(town_recno);

		//--------- update AI town info ---------//

		if (ai_town && nation_recno != 0)
		{
			NationArray[nation_recno].del_town_info(town_recno);
		}

		//--------- reset vars ---------//

		is_base_town = false;
		town_defender_count = 0; // reset defender count

		//----- set power region of the new nation ------//

		World.restore_power(loc_x1, loc_y1, loc_x2, loc_y2, town_recno, 0); // restore power of the old nation
		World.set_power(loc_x1, loc_y1, loc_x2, loc_y2, newNationRecno); // set power of the new nation

		//--- update the cloaked_nation_recno of all spies in the firm ---//

		SpyArray.change_cloaked_nation(Spy.SPY_TOWN, town_recno, nation_recno,
			newNationRecno); // check the cloaked nation recno of all spies in the firm

		//--------- set nation_recno --------//

		int oldNationRecno = nation_recno;
		nation_recno = newNationRecno;

		if (nation_recno != 0) // reset rebel_recno if the town is then ruled by a nation
		{
			if (rebel_recno != 0)
			{
				RebelArray.DeleteRebel(rebel_recno); // delete the rebel group
				rebel_recno = 0;
			}
		}

		//--------- update ai_town ----------//

		ai_town = false;

		if (nation_recno == 0) // independent town
		{
			ai_town = true;
		}
		else if (NationArray[nation_recno].nation_type == NationBase.NATION_AI)
		{
			ai_town = true;
			NationArray[nation_recno].add_town_info(town_recno);
		}

		//------ set the loyalty of the town people ------//

		int nationRaceId = 0;

		if (nation_recno != 0)
			nationRaceId = NationArray[nation_recno].race_id;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (nation_recno == 0) // independent town set up by rebel
				race_loyalty_array[i] = 80; // this affect the no. of defender if you attack the independent town
			else
			{
				if (nationRaceId == i + 1)
					race_loyalty_array[i] = 40; // loyalty is higher if the ruler and the people are the same race
				else
					race_loyalty_array[i] = 30;
			}
		}

		//------- reset town_combat_level -------//

		town_combat_level = 0;

		//------ reset accumulated penalty ------//

		accumulated_collect_tax_penalty = 0;
		accumulated_reward_penalty = 0;
		accumulated_enemy_grant_penalty = 0;
		accumulated_recruit_penalty = 0;

		//---- if there is unit being trained currently, change its nation ---//

		if (train_unit_recno != 0)
			UnitArray[train_unit_recno].change_nation(newNationRecno);

		//-------- update loyalty ---------//

		update_target_loyalty();

		//--- if a nation set up a town in a location that the player has explored, contact between the nation and the player is established ---//

		establish_contact_with_player();

		//----- if an AI nation took over this town, see if the AI can capture all firms linked to this town ----//

		if (nation_recno != 0 && NationArray[nation_recno].is_ai())
			think_capture_linked_firm();

		//------ set national auto policy -----//

		if (nation_recno != 0)
		{
			Nation nation = NationArray[nation_recno];

			set_auto_collect_tax_loyalty(nation.auto_collect_tax_loyalty);
			set_auto_grant_loyalty(nation.auto_grant_loyalty);
		}

		//---- reset the action mode of all spies in this town ----//

		// we need to reset it. e.g. when we have captured an enemy town, SPY_SOW_DISSENT action must be reset to SPY_IDLE
		SpyArray.set_action_mode(Spy.SPY_TOWN, town_recno, Spy.SPY_IDLE);

		//--------- update town network (post-step) ---------//
		//town_network_array.town_post_changing_nation(town_recno, newNationRecno);

		//-------- refresh display ----------//

		if (TownArray.selected_recno == town_recno)
			Info.disp();
	}

	public void surrender(int toNationRecno)
	{
		//--- if this is a rebel town and the mobile rebel count is > 0, don't surrender (this function can be called by update_resistance() when resistance drops to zero ---//

		if (rebel_recno != 0)
		{
			Rebel rebel = RebelArray[rebel_recno];

			if (rebel.mobile_rebel_count > 0)
				return;
		}

		//----------------------------------------//

		if (nation_recno == NationArray.player_recno ||
		    toNationRecno == NationArray.player_recno)
		{
			NewsArray.town_surrendered(town_recno, toNationRecno);
			// ##### begin Gilbert 9/10 ######//
			// sound effect
			if (toNationRecno == NationArray.player_recno)
			{
				SECtrl.immediate_sound("GET_TOWN");
			}
			// ##### end Gilbert 9/10 ######//
		}

		set_nation(toNationRecno);
	}

	public void set_hostile_nation(int nationRecno)
	{
		if (nationRecno == 0)
			return;

		independ_town_nation_relation |= (0x1 << nationRecno);
	}

	public void reset_hostile_nation(int nationRecno)
	{
		if (nationRecno == 0)
			return;

		//TODO check it
		independ_town_nation_relation &= ~(0x1 << nationRecno);
	}

	public bool is_hostile_nation(int nationRecno)
	{
		if (nationRecno == 0)
			return false;

		return (independ_town_nation_relation & (0x1 << nationRecno)) != 0;
	}

	public int create_rebel_unit(int raceId, bool isLeader)
	{
		/*	//--- do not mobilize spies as rebels ----//
	
			//---------------------------------------//
			//
			// If there are spies in this town, first mobilize
			// the spies whose actions are "Sow Dissent".
			//
			//---------------------------------------//
	
			int idleSpyCount=0;
	
			if( race_spy_count_array[raceId-1] > 0 )
			{
				Spy* spy;
	
				for( int i=SpyArray.size() ; i>0 ; i-- )
				{
					if( SpyArray.is_deleted(i) )
						continue;
	
					spy = SpyArray[i];
	
					if( spy.spy_place==Spy.SPY_TOWN && spy.spy_place_para==town_recno && spy.race_id==raceId )
					{
						if( spy.action_mode == SPY_SOW_DISSENT )
						{
							int unitRecno = spy.mobilize_town_spy();
	
							if( isLeader )
								unit_array[unitRecno].set_rank(Unit.RANK_GENERAL);
	
							return unitRecno;
						}
	
						idleSpyCount++;
					}
				}
			}
			//---- if the remaining population are all sleep spy, no new rebels ----//
	
			if( race_pop_array[raceId-1] == idleSpyCount )
				return 0;
	
		*/

		//---- if no jobless people, make workers and overseers jobless ----//

		// 0-don't recruit spies as the above code should have handle spies already
		if (recruitable_race_pop(raceId, false) == 0)
		{
			if (!unjob_town_people(raceId, false, false)) // 0-don't unjob spies, 0-don't unjob overseer
				return 0;

			if (recruitable_race_pop(raceId, false) == 0) // if the unjob unit is a spy too, then don't rebel
				return 0;
		}

		//----look for an empty location for the unit to stand ----//
		//--- scan for the 5 rows right below the building ---//

		int unitId = RaceRes[raceId].basic_unit_id;
		SpriteInfo spriteInfo = SpriteRes[UnitRes[unitId].sprite_id];
		int xLoc = loc_x1, yLoc = loc_y1; // xLoc & yLoc are used for returning results

		if (!World.locate_space(ref xLoc, ref yLoc, loc_x2, loc_y2, spriteInfo.loc_width, spriteInfo.loc_height))
			return 0;

		//---------- add the unit now -----------//

		int rankId;

		if (isLeader)
			rankId = Unit.RANK_GENERAL;
		else
			rankId = Unit.RANK_SOLDIER;

		Unit unit = UnitArray.AddUnit(unitId, 0, rankId, 0, xLoc, yLoc);

		dec_pop(raceId, false); // decrease the town's population

		//------- set the unit's parameters --------//

		if (isLeader)
		{
			// the higher the population is, the higher the combat level will be
			int combatLevel = 10 + population * 2 + Misc.Random(10);
			// the higher the population is, the higher the combat level will be
			int leadershipLevel = 10 + population + Misc.Random(10);

			unit.set_combat_level(Math.Min(combatLevel, 100));

			unit.skill.skill_id = Skill.SKILL_LEADING;
			unit.skill.skill_level = Math.Min(leadershipLevel, 100);
		}
		else
		{
			unit.set_combat_level(GameConstants.CITIZEN_COMBAT_LEVEL); // combat: 10
		}

		return unit.sprite_recno;
	}

	public int mobilize_town_people(int raceId, bool decPop, bool mobilizeSpy)
	{
		//---- if no jobless people, make workers and overseers jobless ----//

		if (recruitable_race_pop(raceId, mobilizeSpy) == 0)
		{
			if (!unjob_town_people(raceId, mobilizeSpy, false)) // 0-don't unjob overseer
				return 0;
		}

		//----look for an empty locatino for the unit to stand ----//
		//--- scan for the 5 rows right below the building ---//

		int unitId = RaceRes[raceId].basic_unit_id;
		SpriteInfo spriteInfo = SpriteRes[UnitRes[unitId].sprite_id];
		int xLoc = loc_x1, yLoc = loc_y1; // xLoc & yLoc are used for returning results

		if (!World.locate_space(ref xLoc, ref yLoc, loc_x2, loc_y2, spriteInfo.loc_width, spriteInfo.loc_height))
			return 0;

		//---------- add the unit now -----------//

		Unit unit = UnitArray.AddUnit(unitId, nation_recno, Unit.RANK_SOLDIER,
			Convert.ToInt32(race_loyalty_array[raceId - 1]), xLoc, yLoc);

		//------- set the unit's parameters --------//

		unit.set_combat_level(GameConstants.CITIZEN_COMBAT_LEVEL);

		//-------- decrease the town's population ------//

		if (decPop)
			dec_pop(raceId, false);

		return unit.sprite_recno;
	}

	public bool mobilize_defender(int attackerNationRecno)
	{
		// do not call out defenders any more if there is only one person left in the town, otherwise the town will be gone.
		if (population == 1)
			return false;

		//------- pick a race to mobilize randomly --------//

		int randomPersonId = Misc.Random(population) + 1;
		int popSum = 0, raceId = 0;

		int i;
		for (i = 0; i < GameConstants.MAX_RACE; i++)
		{
			popSum += race_pop_array[i];

			if (randomPersonId <= popSum)
			{
				raceId = i + 1;
				break;
			}
		}

		if (raceId == 0)
			return false;

		//---- check if the current loyalty allows additional defender ----//
		//
		// if the loyalty is high, then there will be more town defenders
		//
		//-----------------------------------------------------------------//

		double curLoyalty;

		if (nation_recno != 0)
			curLoyalty = race_loyalty_array[raceId - 1];
		else
		{
			if (attackerNationRecno == 0) // if independent units do not attack independent towns
				return false;

			curLoyalty = race_resistance_array[raceId - 1, attackerNationRecno - 1];
		}

		//--- only mobilize new defenders when there aren't too many existing ones ---//

		if (rebel_recno != 0) // if this town is controlled by rebels
		{
			if (curLoyalty < town_defender_count * 2) // rebel towns have more rebel units coming out to defend
				return false;
		}
		else
		{
			if (curLoyalty < town_defender_count * 5) // if no more defenders are allowed for the current loyalty
				return false;
		}

		//----- check if the loyalty/resistance is high enough -----//

		if (nation_recno != 0)
		{
			if (curLoyalty < GameConstants.MIN_NATION_DEFEND_LOYALTY)
				return false;
		}
		else
		{
			if (curLoyalty < GameConstants.MIN_INDEPENDENT_DEFEND_LOYALTY)
				return false;
		}

		//------ check if there are peasants to defend ------//

		if (recruitable_race_pop(raceId, false) == 0) // 0-don't recruit spies
			return false;

		//---------- create a defender unit --------------------//

		//--------------------------------------------------------------//
		//									 loyalty of that race
		// decrease loyalty by: -------------------------------
		//								no. of town people of that race
		//--------------------------------------------------------------//

		double loyaltyDec = curLoyalty / race_pop_array[raceId - 1]; // decrease in loyalty or resistance

		if (nation_recno != 0)
		{
			change_loyalty(raceId, -loyaltyDec);
		}
		else
		{
			for (i = 0; i < GameConstants.MAX_NATION; i++)
				race_resistance_array[raceId - 1, i] -= loyaltyDec;
		}

		//------- mobilize jobless people if there are any -------//

		int unitRecno = mobilize_town_people(raceId, true, false); // 1-dec pop, 0-don't mobilize spy town people

		Unit unit = UnitArray[unitRecno];

		unit.set_mode(UnitConstants.UNIT_MODE_DEFEND_TOWN, town_recno);

		// if the unit is a town defender, this var is temporary used for storing the loyalty that will be added back to the town if the defender returns to the town
		unit.skill.skill_level = Convert.ToInt32(loyaltyDec);

		int combatLevel = town_combat_level + Misc.Random(20) - 10; // -10 to +10 random difference

		combatLevel = Math.Min(combatLevel, 100);
		combatLevel = Math.Max(combatLevel, 10);

		unit.set_combat_level(combatLevel);

		//-----------------------------------------------------//
		// enable unit defend_town mode
		//-----------------------------------------------------//

		unit.stop2();
		unit.action_mode2 = UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET;
		unit.action_para2 = UnitConstants.UNIT_DEFEND_TOWN_DETECT_COUNT;

		unit.action_misc = UnitConstants.ACTION_MISC_DEFEND_TOWN_RECNO;
		unit.action_misc_para = town_recno;

		town_defender_count++;

		//------- if this town is controlled by rebels --------//

		if (rebel_recno != 0)
			RebelArray[rebel_recno].mobile_rebel_count++; // increase the no. of mobile rebel units

		return unitRecno != 0;
	}

	public bool migrate_to(int destTownRecno, int remoteAction, int raceId = 0, int count = 1)
	{
		if (count <= 0 || count > GameConstants.MAX_TOWN_POPULATION)
		{
			return false;
		}

		if (raceId == 0)
		{
			//TODO rewrite
			//raceId = browse_selected_race_id();

			if (raceId == 0)
				return false;
		}

		//if( !remoteAction && remote.is_enable() )
		//{
		//// packet structure : <town recno> <dest town recno> <race id> <count>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_TOWN_MIGRATE, 4*sizeof(short));
		//shortPtr[0] = town_recno;
		//shortPtr[1] = destTownRecno;
		//shortPtr[2] = raceId;
		//shortPtr[3] = count;
		//return 0;
		//}

		bool continueMigrate = true;
		int migrated = 0;
		while (continueMigrate && migrated < count)
		{
			continueMigrate = can_migrate(destTownRecno, true, raceId); // 1- migrate now, 1-allow migrate spy
			if (continueMigrate)
				++migrated;
		}

		return migrated > 0;
	}

	public void collect_yearly_tax()
	{
		NationArray[nation_recno].add_income(NationBase.INCOME_TAX, population * GameConstants.TAX_PER_PERSON);
	}

	public void collect_tax(int remoteAction)
	{
		if (!has_linked_own_camp)
			return;

		//------------------------------------------//

		//if( !remoteAction && remote.is_enable() )
		//{
		//// packet structure : <town recno> <race id>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_TOWN_COLLECT_TAX, sizeof(short));
		//shortPtr[0] = town_recno;
		//return;
		//}

		//----- calculate the loyalty decrease amount ------//
		//
		// If you reward too frequently, the negative effect
		// on loyalty will get larger.
		//
		//--------------------------------------------------//

		int loyaltyDecrease = GameConstants.COLLECT_TAX_LOYALTY_DECREASE + accumulated_collect_tax_penalty / 5;

		loyaltyDecrease = Math.Min(loyaltyDecrease, GameConstants.COLLECT_TAX_LOYALTY_DECREASE + 10);

		accumulated_collect_tax_penalty += 10;

		//------ decrease the loyalty of the town people ------//

		// ##### patch begin Gilbert 5/8 ######//
		//	for( int i=0 ; i<MAX_RACE ; i++ )
		//		change_loyalty( i+1, (float) -loyaltyDecrease );
		//----------- increase cash ------------//
		//	NationArray[nation_recno].add_income(INCOME_TAX, (float)population * TAX_PER_PERSON );

		// ------ cash increase depend on loyalty drop --------//
		double taxCollected = 0.0;
		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			double beforeLoyalty = race_loyalty_array[i];
			change_loyalty(i + 1, -loyaltyDecrease);
			taxCollected += (beforeLoyalty - race_loyalty_array[i]) * race_pop_array[i] * GameConstants.TAX_PER_PERSON /
			                loyaltyDecrease;
		}

		//----------- increase cash ------------//

		NationArray[nation_recno].add_income(NationBase.INCOME_TAX, taxCollected);

		// ##### patch end Gilbert 5/8 ######//

		//------------ think rebel -------------//

		think_rebel();
	}

	public void reward(byte remoteAction)
	{
		if (!has_linked_own_camp)
			return;

		//------------------------------------------//

		//if( !remoteAction && remote.is_enable() )
		//{
		//// packet structure : <town recno> <race id>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_TOWN_REWARD, sizeof(short));
		//shortPtr[0] = town_recno;
		//return;
		//}

		//----- calculate the loyalty increase amount ------//
		//
		// If you reward too frequently, the effect of the
		// granting will be diminished.
		//
		//--------------------------------------------------//

		int loyaltyIncrease = GameConstants.TOWN_REWARD_LOYALTY_INCREASE - accumulated_reward_penalty / 5;

		loyaltyIncrease = Math.Max(3, loyaltyIncrease);

		accumulated_reward_penalty += 10;

		//------ increase the loyalty of the town people ------//

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
			change_loyalty(i + 1, loyaltyIncrease);

		//----------- decrease cash ------------//

		NationArray[nation_recno].add_expense(NationBase.EXPENSE_GRANT_OWN_TOWN,
			population * GameConstants.TOWN_REWARD_PER_PERSON);
	}

	public void distribute_demand()
	{
		//------ scan for a firm to input raw materials --------//

		MarketGoodsInfo[] marketGoodsInfoArray = new MarketGoodsInfo[GameConstants.MAX_PRODUCT];
		for (int i = 0; i < marketGoodsInfoArray.Length; i++)
			marketGoodsInfoArray[i] = new MarketGoodsInfo();

		//------- count the no. of market place that are near to this town ----//

		for (int linkedFirmId = 0; linkedFirmId < linked_firm_array.Count; linkedFirmId++)
		{
			Firm firm = FirmArray[linked_firm_array[linkedFirmId]];

			if (firm.firm_id != Firm.FIRM_MARKET)
				continue;

			if (linked_firm_enable_array[linkedFirmId] != InternalConstants.LINK_EE)
				continue;

			firm = FirmArray[linked_firm_array[linkedFirmId]];

			//---------- process market -------------//

			for (int i = 0; i < GameConstants.MAX_PRODUCT; i++)
			{
				MarketGoods marketGoods = ((FirmMarket)firm).market_product_array[i];
				MarketGoodsInfo marketGoodsInfo = marketGoodsInfoArray[i];

				double thisSupply = marketGoods.stock_qty;
				marketGoodsInfo.markets.Add((FirmMarket)firm);
				marketGoodsInfo.total_supply += thisSupply;

				// vars for later use, so that towns will always try to buy goods from their own markets first.
				if (firm.nation_recno == nation_recno)
					marketGoodsInfo.total_own_supply += thisSupply;
			}
		}

		//-- set the monthly demand of the town on each product --//

		double townDemand = jobless_population * GameConstants.PEASANT_GOODS_MONTH_DEMAND
		                    + worker_population() * GameConstants.WORKER_GOODS_MONTH_DEMAND;

		//---------- sell goods now -----------//

		for (int i = 0; i < GameConstants.MAX_PRODUCT; i++)
		{
			MarketGoodsInfo marketGoodsInfo = marketGoodsInfoArray[i];

			for (int j = 0; j < marketGoodsInfo.markets.Count; j++)
			{
				//----------------------------------//
				//
				// If the totalSupply < town demand:
				// a market's demand = its_supply + (town_demand-totalSupply) / market_count
				//
				// If the totalSupply > town demand:
				// a market's demand = town_demand * its_supply / totalSupply
				//
				//----------------------------------//

				FirmMarket firmMarket = marketGoodsInfo.markets[j];

				MarketGoods marketGoods = firmMarket.market_product_array[i];

				if (marketGoods != null)
				{
					//---- if the demand is larger than the supply -----//

					if (marketGoodsInfo.total_supply <= townDemand)
					{
						// evenly distribute the excessive demand on all markets
						marketGoods.month_demand += marketGoods.stock_qty
						                            + (townDemand - marketGoodsInfo.total_supply) /
						                            marketGoodsInfo.markets.Count;
					}
					else //---- if the supply is larger than the demand -----//
					{
						//--- towns always try to buy goods from their own markets first ---//

						double ownShareDemand = Math.Min(townDemand, marketGoodsInfo.total_own_supply);

						if (firmMarket.nation_recno == nation_recno)
						{
							// if total_own_supply is 0 then ownShareDemand is also 0 and we put no demand on the product
							if (marketGoodsInfo.total_own_supply > 0.0)
								marketGoods.month_demand += ownShareDemand * marketGoods.stock_qty /
								                            marketGoodsInfo.total_own_supply;
						}
						else
						{
							// Note: total_supply > 0.0f, because else the first case above (demand larger than supply) will be triggered
							marketGoods.month_demand += (townDemand - ownShareDemand) * marketGoods.stock_qty /
							                            marketGoodsInfo.total_supply;
						}
					}
				}
			}
		}
	}

	public void being_attacked(int attackerUnitRecno, double attackDamage)
	{
		if (rebel_recno != 0) // if this town is controlled by a rebel group
			RebelArray[rebel_recno].town_being_attacked(attackerUnitRecno);

		if (population == 0)
			return;

		defend_target_recno = attackerUnitRecno; // store the target recno

		Unit attackerUnit = UnitArray[attackerUnitRecno];

		if (attackerUnit.nation_recno == nation_recno) // this can happen when the unit has just changed nation 
			return;

		int attackerNationRecno = attackerUnit.nation_recno;

		last_being_attacked_date = Info.game_date;

		//--------- store attacker nation recno -----------//

		set_hostile_nation(attackerNationRecno);

		//----------- call out defender -----------//

		// only call out defender when the attacking unit is within the effective defending distance

		if (Misc.rects_distance(attackerUnit.cur_x_loc(), attackerUnit.cur_y_loc(),
			    attackerUnit.cur_x_loc(), attackerUnit.cur_y_loc(),
			    loc_x1, loc_y1, loc_x2, loc_y2) <= UnitConstants.EFFECTIVE_DEFEND_TOWN_DISTANCE)
		{
			while (true)
			{
				if (!mobilize_defender(attackerNationRecno))
					break;
			}
		}

		auto_defense(attackerUnitRecno);

		//----- pick a race to be attacked by the attacker randomly -----//

		int raceId = pick_random_race(true, true); // 1-pick has job people, 1-pick spies

		//-------- town people get killed ---------//

		received_hit_count += attackDamage;

		if (received_hit_count >= RECEIVED_HIT_PER_KILL)
		{
			received_hit_count = 0.0;

			int townRecno = town_recno;

			kill_town_people(raceId, attackerNationRecno); // kill a town people

			if (TownArray.IsDeleted(townRecno)) // the town may have been deleted when all pop are killed
				return;
		}

		//---- decrease resistance if this is an independent town ----//

		if (town_defender_count == 0)
		{
			//--- Resistance/loyalty of the town people decrease if the attacking continues ---//
			//
			// Resistance/Loyalty decreases faster:
			//
			// -when there are few people in the town
			// -when there is no defender
			//
			//---------------------------------------//

			double loyaltyDec = 0.0;

			if (nation_recno != 0) // if the town belongs to a nation
			{
				//---- decrease loyalty of all races in the town ----//

				for (raceId = 1; raceId <= GameConstants.MAX_RACE; raceId++)
				{
					if (race_pop_array[raceId - 1] == 0)
						continue;

					if (has_linked_own_camp) // if it is linked to one of its camp, the loyalty will decrease slower
						loyaltyDec = 5.0 / Convert.ToDouble(race_pop_array[raceId - 1]);
					else
						loyaltyDec = 10.0 / Convert.ToDouble(race_pop_array[raceId - 1]);

					loyaltyDec = Math.Min(loyaltyDec, 1.0);

					change_loyalty(raceId, -loyaltyDec * attackDamage / (20 / InternalConstants.ATTACK_SLOW_DOWN));
				}

				//--- if the resistance of all the races are zero, think_change_nation() ---//

				int i;
				for (i = 0; i < GameConstants.MAX_RACE; i++)
				{
					if (race_loyalty_array[i] >= 1.0) // values between 0 and 1 are considered as 0
						break;
				}

				if (i == GameConstants.MAX_RACE) // if resistance of all races drop to zero
					think_surrender();
			}
			else // if the town is an independent town
			{
				if (attackerNationRecno == 0) // if independent units do not attack independent towns
					return;

				//---- decrease resistance of all races in the town ----//

				for (raceId = 1; raceId <= GameConstants.MAX_RACE; raceId++)
				{
					if (race_pop_array[raceId - 1] == 0)
						continue;

					// decrease faster for independent towns than towns belonging to nations
					loyaltyDec = 10.0 / Convert.ToDouble(race_pop_array[raceId - 1]);
					loyaltyDec = Math.Min(loyaltyDec, 1.0);

					race_resistance_array[raceId - 1, attackerNationRecno - 1] -=
						loyaltyDec * attackDamage / (20 / InternalConstants.ATTACK_SLOW_DOWN);

					if (race_resistance_array[raceId - 1, attackerNationRecno - 1] < 0.0)
						race_resistance_array[raceId - 1, attackerNationRecno - 1] = 0.0;
				}

				//--- if the resistance of all the races are zero, think_change_nation() ---//

				int i;
				for (i = 0; i < GameConstants.MAX_RACE; i++)
				{
					if (race_resistance_array[i, attackerNationRecno - 1] >= 1.0)
						break;
				}

				if (i == GameConstants.MAX_RACE) // if resistance of all races drop to zero
					surrender(attackerNationRecno);
			}
		}

		//------ reinforce troops to defend against the attack ------//

		if (town_defender_count == 0 && nation_recno != 0)
		{
			if (attackerUnit.nation_recno != nation_recno) // they may become the same when the town has been captured 
				NationArray[nation_recno].ai_defend(attackerUnitRecno);
		}
	}

	public void clear_defense_mode()
	{
		//------------------------------------------------------------------//
		// change defense unit's to non-defense mode
		//------------------------------------------------------------------//
		foreach (Unit unit in UnitArray)
		{
			if (unit.in_defend_town_mode() && unit.action_misc == UnitConstants.ACTION_MISC_DEFEND_TOWN_RECNO
			                               && unit.action_misc_para == town_recno)
				unit.clear_town_defend_mode(); // note: maybe, unit.nation_recno != nation_recno
		}
	}

	public void reduce_defender_count()
	{
		if (--town_defender_count == 0)
			independ_town_nation_relation = 0;

		//------- if this town is controlled by rebels --------//

		if (rebel_recno != 0)
			RebelArray[rebel_recno].mobile_rebel_count--; // decrease the no. of mobile rebel units
	}

	public void kill_town_people(int raceId, int attackerNationRecno = 0)
	{
		if (raceId == 0)
			raceId = pick_random_race(true, true); // 1-pick has job unit, 1-pick spies 

		if (raceId == 0)
			return;

		//---- jobless town people get killed first, if all jobless are killed, then kill workers ----//

		if (recruitable_race_pop(raceId, true) == 0)
		{
			// 1-unjob spies, 1-unjob overseer if the only person left is a overseer
			if (!unjob_town_people(raceId, true, true))
				return;
		}

		//------ the killed unit can be a spy -----//

		if (Misc.Random(recruitable_race_pop(raceId, true)) < race_spy_count_array[raceId - 1])
		{
			int spyRecno =
				SpyArray.find_town_spy(town_recno, raceId, Misc.Random(race_spy_count_array[raceId - 1]) + 1);

			SpyArray.DeleteSpy(SpyArray[spyRecno]);
		}

		//---- killing civilian people decreases loyalty -----//

		// your people's loyalty decreases because you cannot protect them.
		if (nation_recno != 0 && attackerNationRecno != 0)
			// but only when your units are killed by enemies, neutral disasters are not counted
			NationArray[nation_recno].civilian_killed(raceId, false, 2);

		if (attackerNationRecno != 0) //	the attacker's people's loyalty decreases because of the killing actions.
			NationArray[attackerNationRecno].civilian_killed(raceId, true, 2); // the nation is the attacking one

		// -------- sound effect ---------//

		SERes.sound(center_x, center_y, 1, 'R', raceId, "DIE");

		//-------- decrease population now --------//

		dec_pop(raceId, false); // 0-doesn't have a job
	}

	public bool can_grant_to_non_own_town(int grantNationRecno)
	{
		if (nation_recno == grantNationRecno) // only for independent town
			return false;

		if (nation_recno == 0) // independent town
		{
			return has_linked_camp(grantNationRecno, true); // 1-only count camps with overseers
		}
		else // for nation town, when the enemy doesn't have camps linked to it and the granting nation has camps linked to it
		{
			return !has_linked_camp(nation_recno, false) && // 0-count camps regardless of the presence of overseers
			       has_linked_camp(grantNationRecno, true); // 1-only count camps with overseers
		}
	}

	public int grant_to_non_own_town(int grantNationRecno, int remoteAction)
	{
		if (!can_grant_to_non_own_town(grantNationRecno))
			return 0;

		Nation grantNation = NationArray[grantNationRecno];

		if (grantNation.cash < 0)
			return 0;

		//if( !remoteAction && remote.is_enable() )
		//{
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_TOWN_GRANT_INDEPENDENT, 2*sizeof(short) );
		//shortPtr[0] = town_recno;
		//shortPtr[1] = grantNationRecno;
		//return 1;
		//}

		//---- calculate the resistance to be decreased -----//

		int resistanceDec = GameConstants.IND_TOWN_GRANT_RESISTANCE_DECREASE - accumulated_enemy_grant_penalty / 5;

		resistanceDec = Math.Max(3, resistanceDec);

		accumulated_enemy_grant_penalty += 10;

		//------ decrease the resistance of the independent villagers ------//

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (race_pop_array[i] == 0)
				continue;

			//----- if this is an independent town ------//

			if (nation_recno == 0)
			{
				race_resistance_array[i, grantNationRecno - 1] -= resistanceDec;

				if (race_resistance_array[i, grantNationRecno - 1] < 0)
					race_resistance_array[i, grantNationRecno - 1] = 0.0;
			}
			else //----- if this is an nation town ------//
			{
				race_loyalty_array[i] -= resistanceDec;

				if (race_loyalty_array[i] < 0)
					race_loyalty_array[i] = 0.0;
			}
		}

		//----------- decrease cash ------------//

		grantNation.add_expense(NationBase.EXPENSE_GRANT_OTHER_TOWN,
			population * GameConstants.IND_TOWN_GRANT_PER_PERSON);

		return 1;
	}

	public void get_most_populated_race(out int mostRaceId1, out int mostRaceId2)
	{
		//--- find the two races with most population in the town ---//

		mostRaceId1 = 0;
		mostRaceId2 = 0;

		if (population == 0)
			return;

		int mostRacePop1 = 0, mostRacePop2 = 0;
		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			int racePop = race_pop_array[i];

			if (racePop == 0)
				continue;

			if (racePop >= mostRacePop1)
			{
				mostRacePop2 = mostRacePop1;
				mostRacePop1 = racePop;

				mostRaceId2 = mostRaceId1;
				mostRaceId1 = i + 1;
			}
			else if (racePop >= mostRaceId2)
			{
				mostRacePop2 = racePop;
				mostRaceId2 = i + 1;
			}
		}
	}

	public void update_target_loyalty()
	{
		if (nation_recno == 0) // return if independent towns
			return;

		//----- update loyalty of individual races -------//
		//
		// Loyalty is determined by:
		//
		// - residential harmony
		// - whether the people are racially homogeneous to the king
		// - the nation's reputation
		// - command bases overseeing the town.
		// - quality of life
		// - employment rate
		//
		// Quality of life is determined by:
		//
		// - The provision of goods to the villagers. A more constant
		//	  supply and a bigger variety of goods give to high quality of life.
		//
		//------------------------------------------------//

		//------- set target loyalty of each race --------//

		Nation nation = NationArray[nation_recno];
		int targetLoyalty;
		int nationRaceId = nation.race_id;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (race_pop_array[i] == 0)
				continue;

			//------- calculate the target loyalty -------//

			targetLoyalty = race_harmony(i + 1) / 3 + // 0 to 33
			                (int)nation.reputation / 4; // -25 to +25

			//---- employment help increase loyalty ----//

			targetLoyalty += 30 - 30 * jobless_race_pop_array[i] / race_pop_array[i]; // +0 to +30

			if (RaceRes.is_same_race(i + 1, nationRaceId))
				targetLoyalty += 10;

			if (targetLoyalty < 0) // targetLoyalty can be negative if there are hostile races conflicts
				targetLoyalty = 0;

			if (targetLoyalty > 100)
				targetLoyalty = 100;

			//----------------------------------------//

			race_target_loyalty_array[i] = targetLoyalty;
		}

		//----- process command bases that have influence on this town -----//

		int baseInfluence, thisInfluence, commanderRaceId;

		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			if (linked_firm_enable_array[i] != InternalConstants.LINK_EE)
				continue;

			Firm firm = FirmArray[linked_firm_array[i]];

			if (firm.firm_id != Firm.FIRM_CAMP || firm.overseer_recno == 0)
				continue;

			//-------- get nation and commander info ------------//

			Unit unit = UnitArray[firm.overseer_recno];
			commanderRaceId = unit.race_id;

			Nation baseNation = NationArray[firm.nation_recno];

			//------ if this race is the overseer's race -------//

			baseInfluence = unit.skill.get_skill(Skill.SKILL_LEADING) / 3; // 0 to 33

			if (unit.rank_id == Unit.RANK_KING) // 20 points bonus for king
				baseInfluence += 20;

			//------------ update all race -----------//

			for (int j = 0; j < GameConstants.MAX_RACE; j++)
			{
				if (race_pop_array[j] == 0)
					continue;

				//---- if the overseer's race is the same as this race ---//

				thisInfluence = baseInfluence;

				if (unit.race_id == j + 1)
					thisInfluence += 8;

				//--- if the overseer's nation's race is the same as this race ---//

				if (baseNation.race_id == j + 1)
					thisInfluence += 8;

				//------------------------------------------//

				if (firm.nation_recno == nation_recno) // if the command base belongs to the same nation
				{
					targetLoyalty = race_target_loyalty_array[j] + thisInfluence;
					race_target_loyalty_array[j] = Math.Min(100, targetLoyalty);
				}
				else if (unit.race_id == j + 1) // for enemy camps, only decrease same race peasants
				{
					targetLoyalty = race_target_loyalty_array[j] - thisInfluence;
					race_target_loyalty_array[j] = Math.Max(0, targetLoyalty);
				}
			}
		}

		//------- apply quality of life -------//

		int qolContribution = ConfigAdv.town_loyalty_qol
			? (quality_of_life - 50) / 3
			: // -17 to +17
			0; // off
		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (race_pop_array[i] == 0)
				continue;

			targetLoyalty = race_target_loyalty_array[i];

			// Quality of life only applies to the part above 30 loyalty
			if (targetLoyalty > 30)
			{
				targetLoyalty = Math.Max(30, targetLoyalty + qolContribution);
				race_target_loyalty_array[i] = Math.Min(100, targetLoyalty);
			}
		}

		//------- update link status to linked enemy camps -------//

		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			Firm firm = FirmArray[linked_firm_array[i]];

			if (firm.firm_id != Firm.FIRM_CAMP)
				continue;

			//------------------------------------------//
			// If this town is linked to a own camp,
			// disable all links to enemy camps, otherwise
			// enable all links to enemy camps.
			//------------------------------------------//

			if (firm.nation_recno != nation_recno)
				toggle_firm_link(i + 1, !has_linked_own_camp, InternalConstants.COMMAND_AUTO);
		}
	}

	public void update_target_resistance()
	{
		if (population == 0 || linked_firm_array.Count == 0)
			return;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			for (int j = 0; j < GameConstants.MAX_NATION; j++)
			{
				race_target_resistance_array[i, j] = -1; // -1 means influence is not present
			}
		}

		//---- if this town is controlled by rebels, no decrease in resistance ----//

		if (rebel_recno != 0)
			return;

		//----- count the command base that has influence on this town -----//

		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			if (linked_firm_enable_array[i] != InternalConstants.LINK_EE)
				continue;

			Firm firm = FirmArray[linked_firm_array[i]];

			if (firm.firm_id != Firm.FIRM_CAMP || firm.overseer_recno == 0)
				continue;

			//-------- get nation and commander info ------------//

			Unit unit = UnitArray[firm.overseer_recno];

			int curValue = race_target_resistance_array[unit.race_id - 1, unit.nation_recno - 1];
			int newValue = 100 - camp_influence(firm.overseer_recno);

			// need to do this comparison as there may be more than one command bases of the same nation linked to this town, we use the one with the most influence.
			if (curValue == -1 || newValue < curValue)
				race_target_resistance_array[unit.race_id - 1, unit.nation_recno - 1] = newValue;
		}
	}

	public void update_loyalty()
	{
		if (nation_recno == 0)
			return;

		//------------- update loyalty -------------//

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (race_pop_array[i] == 0)
				continue;

			double targetLoyalty = race_target_loyalty_array[i];

			if (race_loyalty_array[i] < targetLoyalty)
			{
				//--- if this town is linked to enemy camps, but no own camps, no increase in loyalty, only decrease ---//

				if (!has_linked_own_camp && has_linked_enemy_camp)
					continue;

				//-------------------------------------//

				double loyaltyInc = (targetLoyalty - race_loyalty_array[i]) / 30;

				change_loyalty(i + 1, Math.Max(loyaltyInc, 0.5));
			}
			else if (race_loyalty_array[i] > targetLoyalty)
			{
				double loyaltyDec = (race_loyalty_array[i] - targetLoyalty) / 30;

				change_loyalty(i + 1, -Math.Max(loyaltyDec, 0.5));
			}
		}
	}

	public void update_resistance()
	{
		//------------- update resistance ----------------//

		bool[] zeroResistance = new bool[GameConstants.MAX_NATION];
		for (int i = 0; i < zeroResistance.Length; i++)
			zeroResistance[i] = true;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (race_pop_array[i] == 0)
			{
				for (int j = 0; j < GameConstants.MAX_NATION; j++) // reset resistance for non-existing races
					race_resistance_array[i, j] = 0.0;

				continue;
			}

			for (int j = 0; j < GameConstants.MAX_NATION; j++)
			{
				if (NationArray.IsDeleted(j + 1))
					continue;

				if (race_target_resistance_array[i, j] >= 0)
				{
					double targetResistance = race_target_resistance_array[i, j];

					if (race_resistance_array[i, j] > targetResistance) // only decrease, no increase for resistance
					{
						double decValue = (race_resistance_array[i, j] - targetResistance) / 30.0;

						race_resistance_array[i, j] -= Math.Max(1.0, decValue);

						// avoid resistance oscillate between taregtLoyalty-1 and taregtLoyalty+1
						if (race_resistance_array[i, j] < targetResistance)
							race_resistance_array[i, j] = targetResistance;
					}
				}

				// also values between consider 0 and 1 as zero as they are displayed as 0 in the interface
				if (race_resistance_array[i, j] >= 1.0)
					zeroResistance[j] = false;
			}
		}

		//--- if the town is zero resistance towards any nation, convert to that nation ---//

		for (int j = 0; j < GameConstants.MAX_NATION; j++)
		{
			if (NationArray.IsDeleted(j + 1))
				continue;

			if (zeroResistance[j])
			{
				surrender(j + 1);
				break;
			}
		}
	}

	public void update_product_supply()
	{
		for (int i = 0; i < has_product_supply.Length; i++)
			has_product_supply[i] = 0;

		//----- scan for linked market place -----//

		for (int i = linked_firm_array.Count - 1; i >= 0; i--)
		{
			Firm firm = FirmArray[linked_firm_array[i]];
			if (firm.nation_recno != nation_recno || firm.firm_id != Firm.FIRM_MARKET)
				continue;

			FirmMarket firmMarket = (FirmMarket)firm;
			
			//---- check what type of products they are selling ----//

			for (int j = 0; j < GameConstants.MAX_MARKET_GOODS; j++)
			{
				int productId = firmMarket.market_goods_array[j].product_raw_id;
				if (productId > 1)
					has_product_supply[productId - 1]++;
			}
		}
	}

	public void change_loyalty(int raceId, double loyaltyChange)
	{
		double newLoyalty = race_loyalty_array[raceId - 1] + loyaltyChange;

		newLoyalty = Math.Min(100.0, newLoyalty);
		newLoyalty = Math.Max(0.0, newLoyalty);

		race_loyalty_array[raceId - 1] = newLoyalty;
	}

	public void change_resistance(int raceId, int nationRecno, double resistanceChange)
	{
		double newResistance = race_resistance_array[raceId - 1, nationRecno - 1] + resistanceChange;

		newResistance = Math.Min(100.0, newResistance);
		newResistance = Math.Max(0.0, newResistance);

		race_resistance_array[raceId - 1, nationRecno - 1] = newResistance;
	}

	public void toggle_firm_link(int linkId, bool toggleFlag, int remoteAction, int setBoth = 0)
	{
		//if( !remoteAction && remote.is_enable() )
		//{
		//// packet structure : <town recno> <link Id> <toggle Flag>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_TOWN_TOGGLE_LINK_FIRM, 3*sizeof(short));
		//shortPtr[0] = town_recno;
		//shortPtr[1] = linkId;
		//shortPtr[2] = toggleFlag;
		//return;
		//}

		Firm linkedFirm = FirmArray[linked_firm_array[linkId - 1]];
		int linkedNationRecno = linkedFirm.nation_recno;

		// if one of the linked end is an indepdendent firm/nation, consider this link as a single nation link
		bool sameNation = linkedNationRecno == nation_recno || linkedNationRecno == 0 || nation_recno == 0;

		if (toggleFlag)
		{
			if ((sameNation && setBoth == 0) || setBoth == 1) // 0 if setBoth == -1
				linked_firm_enable_array[linkId - 1] = InternalConstants.LINK_EE;
			else
				linked_firm_enable_array[linkId - 1] |= InternalConstants.LINK_ED;
		}
		else
		{
			if ((sameNation && setBoth == 0) || setBoth == 1)
				linked_firm_enable_array[linkId - 1] = InternalConstants.LINK_DD;
			else
				linked_firm_enable_array[linkId - 1] &= ~InternalConstants.LINK_ED;
		}

		//------ set the linked flag of the opposite firm -----//

		Firm firm = FirmArray[linked_firm_array[linkId - 1]];

		for (int i = firm.linked_town_array.Count - 1; i >= 0; i--)
		{
			if (firm.linked_town_array[i] == town_recno)
			{
				if (toggleFlag)
				{
					if ((sameNation && setBoth == 0) || setBoth == 1)
						firm.linked_town_enable_array[i] = InternalConstants.LINK_EE;
					else
						firm.linked_town_enable_array[i] |= InternalConstants.LINK_DE;
				}
				else
				{
					if ((sameNation && setBoth == 0) || setBoth == 1)
						firm.linked_town_enable_array[i] = InternalConstants.LINK_DD;
					else
						firm.linked_town_enable_array[i] &= ~InternalConstants.LINK_DE;
				}

				break;
			}
		}

		//-------- update the town's influence --------//

		if (nation_recno == 0)
			update_target_resistance();

		//--- redistribute demand if a link to market place has been toggled ---//

		if (linkedFirm.firm_id == Firm.FIRM_MARKET)
			TownArray.distribute_demand();
	}

	public void toggle_town_link(int linkId, int toggleFlag, char remoteAction, int setBoth = 0)
	{
		// Function is unused, and not updated to support town networks.
		return;
	}

	public void auto_defense(int targetRecno)
	{
		int townRecno = town_recno;

		for (int i = linked_firm_array.Count - 1; i >= 0; i--)
		{
			Firm firm = FirmArray[linked_firm_array[i]];

			if (firm.nation_recno != nation_recno || firm.firm_id != Firm.FIRM_CAMP)
				continue;

			//-------------------------------------------------------//
			// the firm is a military camp of our nation
			//-------------------------------------------------------//
			FirmCamp camp = (FirmCamp)firm;
			camp.defense(targetRecno);

			if (TownArray.IsDeleted(townRecno))
				break; // the last unit in the town has be mobilized
		}
	}

	public bool has_player_spy()
	{
		int i;
		for (i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (race_spy_count_array[i] > 0)
				break;
		}

		if (i == GameConstants.MAX_RACE) // no spies in this nation
			return false;

		//----- look for player spy in the spy_array -----//

		foreach (Spy spy in SpyArray)
		{
			if (spy.spy_place == Spy.SPY_TOWN &&
			    spy.spy_place_para == town_recno &&
			    spy.true_nation_recno == NationArray.player_recno)
			{
				return true;
			}
		}

		return false;
	}

	public void verify_slot_object_id_array()
	{
		TownLayout townLayout = TownRes.get_layout(layout_id);
		//TownSlot   townSlot   = TownRes.get_slot(townLayout.first_slot_recno);

		for (int i = 0; i < townLayout.slot_count; i++)
		{
			//TODO check it
			TownSlot townSlot = TownRes.get_slot(townLayout.first_slot_recno + i);

			//----- build_type==0 if plants -----//

			switch (townSlot.build_type)
			{
				//----- build_type>0 if town buildings -----//

				case TownSlot.TOWN_OBJECT_HOUSE:
					TownRes.get_build(slot_object_id_array[i]);
					break;

				case TownSlot.TOWN_OBJECT_PLANT:
					PlantRes.get_bitmap(slot_object_id_array[i]);
					break;

				case TownSlot.TOWN_OBJECT_FARM:
					break;
			}
		}
	}

	public void set_auto_collect_tax_loyalty(int loyaltyLevel)
	{
		auto_collect_tax_loyalty = loyaltyLevel;

		if (loyaltyLevel != 0 && auto_grant_loyalty >= auto_collect_tax_loyalty)
		{
			auto_grant_loyalty = auto_collect_tax_loyalty - 10;
		}
	}

	public void set_auto_grant_loyalty(int loyaltyLevel)
	{
		auto_grant_loyalty = loyaltyLevel;

		if (loyaltyLevel != 0 && auto_grant_loyalty >= auto_collect_tax_loyalty)
		{
			auto_collect_tax_loyalty = auto_grant_loyalty + 10;

			if (auto_collect_tax_loyalty > 100)
				auto_collect_tax_loyalty = 0; // disable auto collect tax if it's over 100
		}
	}

	//-------- ai functions ---------//

	public void think_collect_tax()
	{
		if (!has_linked_own_camp)
			return;

		if (should_ai_migrate()) // if the town should migrate, do collect tax, otherwise the loyalty will be too low for mobilizing the peasants.
			return;

		if (accumulated_collect_tax_penalty > 0)
			return;

		//--- collect tax if the loyalty of all the races >= minLoyalty (55-85) ---//

		Nation ownNation = NationArray[nation_recno];

		int yearProfit = Convert.ToInt32(ownNation.profit_365days());
		double cash = ownNation.cash;
		int minLoyalty = 55 + 30 * ownNation.pref_loyalty_concern / 100;

		if (yearProfit < 0) // we are losing money now
			minLoyalty -= (-yearProfit) / 100; // more aggressive in collecting tax if we are losing a lot of money

		if (cash < 1000.0)
			minLoyalty -= 10;

		if (cash < 500.0)
			minLoyalty -= 10;

		if (cash < 100.0)
			minLoyalty -= 10;

		if (cash > 20000.0)
			minLoyalty = Math.Max(Convert.ToInt32(cash / 500.0), minLoyalty);

		minLoyalty = Math.Max(45, minLoyalty);

		//---------------------------------------------//

		if (average_loyalty() < minLoyalty)
			return;

		//---------- collect tax now ----------//

		collect_tax(InternalConstants.COMMAND_AI);
	}

	public void think_reward()
	{
		if (!has_linked_own_camp)
			return;

		if (accumulated_reward_penalty > 0)
			return;

		//---- if accumulated_reward_penalty>0, don't grant unless the villagers are near the rebel level ----//

		Nation ownNation = NationArray[nation_recno];
		int averageLoyalty = average_loyalty();

		if (averageLoyalty < GameConstants.REBEL_LOYALTY + 5 + ownNation.pref_loyalty_concern / 10) // 35 to 45
		{
			int importanceRating;

			if (averageLoyalty < GameConstants.REBEL_LOYALTY + 5)
				importanceRating = 40 + population;
			else
				importanceRating = population;

			if (ownNation.ai_should_spend(importanceRating))
				reward(InternalConstants.COMMAND_AI);
		}
	}

	public bool think_build_firm(int firmId, int maxFirm)
	{
		Nation nation = NationArray[nation_recno];

		//--- check whether the AI can build a new firm next this firm ---//

		if (!nation.can_ai_build(firmId))
			return false;

		//-- only build one market place next to this town, check if there is any existing one --//

		int firmCount = 0;

		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			Firm firm = FirmArray[linked_firm_array[i]];

			//---- if there is one firm of this type near the town already ----//

			if (firm.firm_id == firmId && firm.nation_recno == nation_recno)
			{
				if (++firmCount >= maxFirm)
					return false;
			}
		}

		//------ queue building a new firm -------//

		return ai_build_neighbor_firm(firmId);
	}

	public bool think_build_market()
	{
		// don't build the market too soon, as it may need to migrate to other town
		if (Info.game_date < setup_date.AddDays(180.0))
			return false;

		Nation ownNation = NationArray[nation_recno];

		if (population < 10 + (100 - ownNation.pref_trading_tendency) / 20)
			return false;

		if (no_neighbor_space) // if there is no space in the neighbor area for building a new firm.
			return false;

		//--- check whether the AI can build a new firm next this firm ---//

		if (!ownNation.can_ai_build(Firm.FIRM_MARKET))
			return false;

		//----------------------------------------------------//
		// If there is already a firm queued for building with
		// a building location that is within the effective range
		// of the this firm.
		//----------------------------------------------------//

		if (ownNation.is_build_action_exist(Firm.FIRM_MARKET, center_x, center_y))
			return false;

		//-- only build one market place next to this mine, check if there is any existing one --//

		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			Firm firm = FirmArray[linked_firm_array[i]];

			if (firm.firm_id != Firm.FIRM_MARKET)
				continue;

			FirmMarket firmMarket = (FirmMarket)firm;

			//------ if this market is our own one ------//

			if (firmMarket.nation_recno == nation_recno && firmMarket.is_retail_market())
				return false;
		}

		//------ queue building a new market -------//

		int buildXLoc, buildYLoc;

		if (!ownNation.find_best_firm_loc(Firm.FIRM_MARKET, loc_x1, loc_y1,
			    out buildXLoc, out buildYLoc))
		{
			no_neighbor_space = true;
			return false;
		}

		ownNation.add_action(buildXLoc, buildYLoc, loc_x1, loc_y1,
			Nation.ACTION_AI_BUILD_FIRM, Firm.FIRM_MARKET);

		return true;
	}

	public bool think_build_camp()
	{
		//Do not build second camp too early because we can move our first town
		if (Info.game_date < Info.game_start_date.AddDays(180.0))
			return false;

		//check if any of the other camps protecting this town is still recruiting soldiers
		//if so, wait until their recruitment is finished. So we can measure the protection available accurately.

		Nation ownNation = NationArray[nation_recno];
		int campCount = 0;

		for (int i = linked_firm_array.Count - 1; i >= 0; --i)
		{
			Firm firm = FirmArray[linked_firm_array[i]];

			if (firm.firm_id != Firm.FIRM_CAMP)
				continue;

			FirmCamp firmCamp = (FirmCamp)firm;

			if (firmCamp.nation_recno != nation_recno)
				continue;

			// if this camp is still trying to recruit soldiers
			if (firmCamp.under_construction || firmCamp.ai_recruiting_soldier ||
			    firmCamp.workers.Count < Firm.MAX_WORKER)
				return false;

			campCount++;
		}

		//--- this is one of the few base towns the nation has, then a camp must be built ---//

		if (campCount == 0 && is_base_town && ownNation.ai_base_town_count <= 2)
		{
			return ai_build_neighbor_firm(Firm.FIRM_CAMP);
		}

		//---- only build camp if we have enough cash and profit ----//

		if (!ownNation.ai_should_spend(70 + ownNation.pref_military_development / 4)) // 70 to 95
			return false;

		//---- only build camp if we need more protection than it is currently available ----//

		int protectionAvailable = protection_available();
		int protectionNeeded = protection_needed();
		Nation nation = NationArray[nation_recno];
		//Protect 1.5 - 2.5 times more than needed depending on the military development
		if (nation.ai_has_enough_food())
			protectionNeeded = protectionNeeded * (150 + nation.pref_military_development) / 100;

		if (protectionAvailable >= protectionNeeded)
			return false;

		if (protectionAvailable > 0) // if protection available is 0, we must build a camp now
		{
			int needUrgency = 100 * (protectionNeeded - protectionAvailable) / protectionNeeded;

			if (nation.total_jobless_population - Firm.MAX_WORKER <
			    (100 - needUrgency) * (200 - nation.pref_military_development) / 200)
			{
				return false;
			}
		}

		//--- check if we have enough people to recruit ---//

		bool buildFlag = nation.total_jobless_population >= 16;

		if (nation.total_jobless_population >= 8 && is_base_town)
		{
			buildFlag = true;
		}
		else if (nation.ai_has_should_close_camp(region_id)) // there is camp that can be closed
		{
			buildFlag = true;
		}

		if (!buildFlag)
			return false;

		return ai_build_neighbor_firm(Firm.FIRM_CAMP);
	}

	public bool think_build_research()
	{
		//Do not build the first year
		if (Info.game_date < Info.game_start_date.AddDays(365.0))
			return false;

		Nation nation = NationArray[nation_recno];

		if (!is_base_town)
			return false;

		if (jobless_population < Firm.MAX_WORKER || nation.total_jobless_population < Firm.MAX_WORKER * 2)
			return false;

		if (nation.true_profit_365days() < 0)
			return false;

		if (!nation.ai_should_spend(25 + nation.pref_use_weapon / 2 - nation.ai_research_array.Count * 10))
			return false;

		int totalTechLevel = nation.total_tech_level();

		if (totalTechLevel == TechRes.total_tech_level) // all technology have been researched
			return false;

		//--------------------------------------------//

		int maxResearch = 2 * (50 + nation.pref_use_weapon) / 50;

		maxResearch = Math.Min(nation.ai_town_array.Count, maxResearch);

		if (nation.ai_research_array.Count >= maxResearch)
			return false;

		//---- if any of the existing ones are not full employed ----//

		for (int i = 0; i < nation.ai_research_array.Count; i++)
		{
			FirmResearch firmResearch = (FirmResearch)FirmArray[nation.ai_research_array[i]];

			if (firmResearch.region_id != region_id)
				continue;

			if (firmResearch.workers.Count < Firm.MAX_WORKER)
				return false;
		}
		//------- queue building a war factory -------//

		return ai_build_neighbor_firm(Firm.FIRM_RESEARCH);
	}

	public bool think_build_war_factory()
	{
		Nation nation = NationArray[nation_recno];

		if (!is_base_town)
			return false;

		if (jobless_population < Firm.MAX_WORKER || nation.total_jobless_population < Firm.MAX_WORKER * 2)
			return false;

		int totalWeaponTechLevel = nation.total_tech_level(UnitConstants.UNIT_CLASS_WEAPON);

		if (totalWeaponTechLevel == 0)
			return false;

		//----- see if we have enough money to build & support the weapon ----//

		// if we don't have any war factory, we may want to build one despite that we are losing money
		if (nation.true_profit_365days() < 0 && nation.ai_war_array.Count > 0)
			return false;

		if (!nation.ai_should_spend(nation.pref_use_weapon / 2))
			return false;

		//--------------------------------------------//

		int maxWarFactory = (1 + totalWeaponTechLevel) * nation.pref_use_weapon / 100;

		maxWarFactory = Math.Min(nation.ai_town_array.Count, maxWarFactory);

		if (nation.ai_war_array.Count >= maxWarFactory)
			return false;

		//---- if any of the existing ones are not full employed or under capacity ----//

		for (int i = 0; i < nation.ai_war_array.Count; i++)
		{
			FirmWar firmWar = (FirmWar)FirmArray[nation.ai_war_array[i]];

			if (firmWar.region_id != region_id)
				continue;

			//TODO this code is different from the same in think_build_research()
			if (firmWar.workers.Count < Firm.MAX_WORKER || firmWar.build_unit_id == 0)
				return false;
		}

		//------- queue building a war factory -------//

		return ai_build_neighbor_firm(Firm.FIRM_WAR_FACTORY);
	}

	public bool think_build_base()
	{
		Nation nation = NationArray[nation_recno];

		if (!is_base_town)
			return false;

		//----- see if we have enough money to build & support the weapon ----//

		if (!nation.ai_should_spend(50))
			return false;

		if (jobless_population < 15 || nation.total_jobless_population < 30)
			return false;

		//------ do a scan on the existing bases first ------//

		int[] buildRatingArray = new int[GameConstants.MAX_RACE];

		//--- increase build rating for the seats that this nation knows how to build ---//

		for (int i = 1; i <= GodRes.god_info_array.Length; i++)
		{
			GodInfo godInfo = GodRes[i];

			if (godInfo.is_nation_know(nation_recno))
				buildRatingArray[godInfo.race_id - 1] += 100;
		}

		//--- decrease build rating for the seats that the nation currently has ---//

		for (int i = 0; i < nation.ai_base_array.Count; i++)
		{
			FirmBase firmBase = (FirmBase)FirmArray[nation.ai_base_array[i]];

			buildRatingArray[GodRes[firmBase.god_id].race_id - 1] = 0; // only build one 

			/*
			if( firmBase.prayer_count < MAX_BASE_PRAYER )
				buildRatingArray[ god_res[firmBase.god_id].race_id-1 ] = 0;
			else
				buildRatingArray[ god_res[firmBase.god_id].race_id-1 ] -= 10;		// -10 for one existing instance
			*/
		}

		//------ decide which is the best to build -------//

		int bestRating = 0, bestRaceId = 0;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (buildRatingArray[i] > bestRating)
			{
				bestRating = buildRatingArray[i];
				bestRaceId = i + 1;
			}
		}

		//------- queue building a seat of power ------//

		if (bestRaceId != 0)
			return ai_build_neighbor_firm(Firm.FIRM_BASE, bestRaceId);

		return false;
	}

	public bool think_build_inn()
	{
		Nation ownNation = NationArray[nation_recno];

		if (ownNation.ai_inn_array.Count < ownNation.ai_supported_inn_count())
		{
			return think_build_firm(Firm.FIRM_INN, 1);
		}

		return false;
	}

	public bool think_ai_migrate()
	{
		// don't move if this town has just been set up for less than 90 days. It may be a town set up by think_split_town()
		if (Info.game_date < setup_date.AddDays(90.0))
			return false;

		Nation nation = NationArray[nation_recno];

		//-- the higher the loyalty, the higher the chance all the unit can be migrated --//

		int averageLoyalty = average_loyalty();
		int minMigrateLoyalty = 35 + nation.pref_loyalty_concern / 10; // 35 to 45

		if (averageLoyalty < minMigrateLoyalty)
		{
			//-- if the total population is low (we need people) and the cash is high (we have money), then grant to increase loyalty for migration --//

			// if the average target loyalty is also lower than
			if (accumulated_reward_penalty == 0 && average_target_loyalty() < minMigrateLoyalty + 5)
			{
				if (nation.ai_should_spend(20 + nation.pref_territorial_cohesiveness / 2)) // 20 to 70 
					reward(InternalConstants.COMMAND_AI);
			}

			if (average_loyalty() < minMigrateLoyalty)
				return false;
		}

		if (!should_ai_migrate())
			return false;

		//------ think about which town to migrate to ------//

		int bestTownRecno = think_ai_migrate_to_town();

		if (bestTownRecno == 0)
			return false;

		//--- check if there are already units currently migrating to the destination town ---//

		Town destTown = TownArray[bestTownRecno];

		// last 1-check duplication on the destination town only
		if (nation.get_action(destTown.loc_x1, destTown.loc_y1, loc_x1, loc_y1,
			    Nation.ACTION_AI_SETTLE_TO_OTHER_TOWN, 0, 0, 1) != null)
		{
			return false;
		}

		//--------- queue for migration now ---------//

		int migrateCount = (average_loyalty() - GameConstants.MIN_RECRUIT_LOYALTY) / 5;

		migrateCount = Math.Min(migrateCount, jobless_population);

		if (migrateCount <= 0)
			return false;

		nation.add_action(destTown.loc_x1, destTown.loc_y1,
			loc_x1, loc_y1, Nation.ACTION_AI_SETTLE_TO_OTHER_TOWN, 0, migrateCount);

		return true;
	}

	public int think_ai_migrate_to_town()
	{
		//------ think about which town to migrate to ------//

		Nation nation = NationArray[nation_recno];
		int bestRating = 0, bestTownRecno = 0;
		int majorityRace = majority_race();

		for (int i = 0; i < nation.ai_town_array.Count; i++)
		{
			int aiTown = nation.ai_town_array[i];
			if (town_recno == aiTown)
				continue;

			Town town = TownArray[aiTown];

			if (!town.is_base_town) // only migrate to base towns
				continue;

			if (town.region_id != region_id)
				continue;

			// if the town does not have enough space for the migration
			if (population > GameConstants.MAX_TOWN_POPULATION - town.population)
				continue;

			//--------- compare the ratings ---------//

			// *1000 so that this will have a much bigger weight than the distance rating
			int curRating = 1000 * town.race_pop_array[majorityRace - 1] / town.population;

			curRating += World.distance_rating(center_x, center_y, town.center_x, town.center_y);

			if (curRating > bestRating)
			{
				//--- if there is a considerable population of this race, then must migrate to a town with the same race ---//

				if (race_pop_array[majorityRace - 1] >= 6)
				{
					// must be commented out otherwise low population town will never be optimized
					if (town.majority_race() != majorityRace)
						continue;
				}

				bestRating = curRating;
				bestTownRecno = town.town_recno;
			}
		}

		return bestTownRecno;
	}

	public void think_defense()
	{
		int enemyUnitRecno = detect_enemy(3); // only when 3 units are detected, we consider them as enemy

		if (enemyUnitRecno == 0)
			return;

		Unit enemyUnit = UnitArray[enemyUnitRecno];

		int enemyXLoc = enemyUnit.cur_x_loc();
		int enemyYLoc = enemyUnit.cur_y_loc();

		//----- get our unit that is closet to it to attack it -------//

		int minDis = Int32.MaxValue;
		Unit closestUnit = null;

		foreach (Unit unit in UnitArray)
		{
			if (unit.nation_recno == nation_recno)
			{
				int curDis = Math.Max(Math.Abs(unit.cur_x_loc() - enemyXLoc), Math.Abs(unit.cur_y_loc() - enemyYLoc));

				if (curDis < minDis)
				{
					minDis = curDis;
					closestUnit = unit;
				}
			}
		}

		//-------- attack the enemy now ----------//

		if (closestUnit != null)
			closestUnit.attack_unit(enemyUnit.sprite_recno, 0, 0, true);
	}

	public bool think_split_town()
	{
		if (jobless_population == 0) // cannot move if we don't have any mobilizable unit
			return false;

		//--- split when the population is close to its limit ----//

		Nation nation = NationArray[nation_recno];

		if (population < 45 + nation.pref_territorial_cohesiveness / 10)
			return false;

		//-------- think about which race to move --------//

		int mostRaceId1, mostRaceId2, raceId = 0;

		get_most_populated_race(out mostRaceId1, out mostRaceId2);

		if (mostRaceId2 != 0 && jobless_race_pop_array[mostRaceId2 - 1] > 0 && can_recruit(mostRaceId2))
			raceId = mostRaceId2;

		else if (mostRaceId1 != 0 && jobless_race_pop_array[mostRaceId1 - 1] > 0 && can_recruit(mostRaceId1))
			raceId = mostRaceId1;

		else
			raceId = pick_random_race(false, true); // 0-recruitable only, 1-also pick spy.

		if (raceId == 0)
		{
			//---- if the racial mix favors a split of town -----//
			//
			// This is when there are two major races, both of significant
			// population in the town. This is a good time for us to move
			// the second major race to a new town.
			//
			//---------------------------------------------------//

			// the race's has at least 1/3 of the town's total population
			if (mostRaceId2 != 0 && jobless_race_pop_array[mostRaceId2] > 0
			                     && race_pop_array[mostRaceId2] >= population / 3 && can_recruit(mostRaceId2))
			{
				raceId = mostRaceId2;
			}
		}

		if (raceId == 0)
			return false;

		//---- check if there is already a town of this race with low population linked to this town, then don't split a new one ---//

		for (int i = 0; i < linked_town_array.Count; i++)
		{
			Town town = TownArray[linked_town_array[i]];

			if (town.nation_recno == nation_recno &&
			    town.population < 20 &&
			    town.majority_race() == raceId)
			{
				return false;
			}
		}

		//-------- settle to a new town ---------//

		return ai_settle_new(raceId);
	}

	public void think_move_between_town()
	{
		//------ move people between linked towns ------//

		int ourMajorityRace = majority_race();

		for (int i = 0; i < linked_town_array.Count; i++)
		{
			Town town = TownArray[linked_town_array[i]];

			if (town.nation_recno != nation_recno)
				continue;

			//--- migrate people to the new town ---//

			while (true)
			{
				bool rc = false;
				int raceId = town.majority_race(); // get the linked town's major race

				// if our major race is not this race, then move the person to the target town
				if (ourMajorityRace != raceId)
				{
					rc = true;
				}
				else //-- if this town's major race is the same as the target town --//
				{
					// if this town's population is larger than the target town by 10 people, then move
					if (population - town.population > 10)
						rc = true;
				}

				if (rc)
				{
					if (!migrate_to(town.town_recno, InternalConstants.COMMAND_AI, raceId))
						break;
				}
				else
					break;
			}
		}
	}

	public bool think_attack_nearby_enemy()
	{
		const int SCAN_X_RANGE = 6;
		const int SCAN_Y_RANGE = 6;

		int xLoc1 = loc_x1 - SCAN_X_RANGE;
		int yLoc1 = loc_y1 - SCAN_Y_RANGE;
		int xLoc2 = loc_x2 + SCAN_X_RANGE;
		int yLoc2 = loc_y2 + SCAN_Y_RANGE;

		xLoc1 = Math.Max(xLoc1, 0);
		yLoc1 = Math.Max(yLoc1, 0);
		xLoc2 = Math.Min(xLoc2, GameConstants.MapSize - 1);
		yLoc2 = Math.Min(yLoc2, GameConstants.MapSize - 1);

		//------------------------------------------//

		int enemyCombatLevel = 0; // the higher the rating, the easier we can attack the target town.
		int enemyXLoc = -1;
		int enemyYLoc = -1;
		Nation nation = NationArray[nation_recno];

		for (int yLoc = yLoc1; yLoc <= yLoc2; yLoc++)
		{

			for (int xLoc = xLoc1; xLoc <= xLoc2; xLoc++)
			{
				Location location = World.get_loc(xLoc, yLoc);
				//--- if there is an enemy unit here ---// 

				if (location.has_unit(UnitConstants.UNIT_LAND))
				{
					Unit unit = UnitArray[location.unit_recno(UnitConstants.UNIT_LAND)];

					if (unit.nation_recno == 0)
						continue;

					//--- if the unit is idle and he is our enemy ---//

					if (unit.cur_action == Sprite.SPRITE_IDLE &&
					    nation.get_relation_status(unit.nation_recno) == NationBase.NATION_HOSTILE)
					{
						enemyCombatLevel += (int)unit.hit_points;

						if (enemyXLoc == -1 || Misc.Random(5) == 0)
						{
							enemyXLoc = xLoc;
							enemyYLoc = yLoc;
						}
					}
				}

				//--- if there is an enemy firm here ---//

				else if (location.is_firm())
				{
					Firm firm = FirmArray[location.firm_recno()];

					//------- if this is a monster firm ------//

					if (firm.firm_id == Firm.FIRM_MONSTER) // don't attack monster here, OAI_MONS.CPP will handle that
						continue;

					//------- if this is a firm of our enemy -------//

					if (nation.get_relation_status(firm.nation_recno) == NationBase.NATION_HOSTILE)
					{
						if (firm.workers.Count == 0)
							enemyCombatLevel += 50; // empty firm
						else
						{
							for (int i = 0; i < firm.workers.Count; i++)
							{
								Worker worker = firm.workers[i];
								enemyCombatLevel += worker.hit_points;
							}
						}

						if (enemyXLoc == -1 || Misc.Random(5) == 0)
						{
							enemyXLoc = firm.loc_x1;
							enemyYLoc = firm.loc_y1;
						}
					}
				}
			}
		}

		//--------- attack the target now -----------//

		if (enemyCombatLevel > 0)
		{
			//err_when( enemyXLoc < 0 );

			nation.ai_attack_target(enemyXLoc, enemyYLoc, enemyCombatLevel);
			return true;
		}

		return false;
	}

	public bool think_attack_linked_enemy()
	{
		Nation ownNation = NationArray[nation_recno];
		int targetCombatLevel;

		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			if (linked_firm_enable_array[i] != InternalConstants.LINK_EE) // don't attack if the link is not enabled
				continue;

			Firm firm = FirmArray[linked_firm_array[i]];

			if (firm.nation_recno == nation_recno)
				continue;

			if (firm.should_close_flag) // if this camp is about to close
				continue;

			//--- only attack AI firms when they belong to a hostile nation ---//

			if (firm.firm_ai &&
			    ownNation.get_relation_status(firm.nation_recno) != NationBase.NATION_HOSTILE)
			{
				continue;
			}

			//---- if this is a camp -----//

			if (firm.firm_id == Firm.FIRM_CAMP)
			{
				//----- if we are friendly with the target nation ------//

				int nationStatus = ownNation.get_relation_status(firm.nation_recno);

				if (nationStatus >= NationBase.NATION_FRIENDLY)
				{
					if (!ownNation.ai_should_attack_friendly(firm.nation_recno, 100))
						continue;

					ownNation.ai_end_treaty(firm.nation_recno);
				}
				else if (nationStatus == NationBase.NATION_NEUTRAL)
				{
					//-- if the link is off and the nation's miliary strength is bigger than us, don't attack --//

					if (NationArray[firm.nation_recno].military_rank_rating() >
					    ownNation.military_rank_rating())
					{
						continue;
					}
				}

				//--- don't attack when the trade rating is high ----//

				int tradeRating = ownNation.trade_rating(firm.nation_recno);

				if (tradeRating > 50 ||
				    tradeRating + ownNation.ai_trade_with_rating(firm.nation_recno) > 100)
				{
					continue;
				}

				targetCombatLevel = ((FirmCamp)firm).total_combat_level();
			}
			else //--- if this is another type of firm ----//
			{
				//--- only attack other types of firm when the status is hostile ---//

				if (ownNation.get_relation_status(firm.nation_recno) != NationBase.NATION_HOSTILE)
					continue;

				targetCombatLevel = 50;
			}

			//--------- attack now ----------//

			ownNation.ai_attack_target(firm.loc_x1, firm.loc_y1, targetCombatLevel);

			return true;
		}

		return false;
	}

	public void think_capture_linked_firm()
	{
		//----- scan for linked firms -----//

		for (int i = linked_firm_array.Count - 1; i >= 0; i--)
		{
			Firm firm = FirmArray[linked_firm_array[i]];

			if (firm.nation_recno == nation_recno) // this is our own firm
				continue;

			if (firm.can_worker_capture(nation_recno)) // if we can capture this firm, capture it now
			{
				firm.capture_firm(nation_recno);
			}
		}
	}

	public bool think_capture_enemy_town()
	{
		if (!is_base_town)
			return false;

		Nation nation = NationArray[nation_recno];

		if (nation.ai_capture_enemy_town_recno != 0) // a capturing action is already in process
			return false;

		int surplusProtection = protection_available() - protection_needed();

		//-- only attack enemy town if we have enough military protection surplus ---//

		if (surplusProtection < 300 - nation.pref_military_courage * 2)
			return false;

		return nation.think_capture_new_enemy_town(this);
	}

	public bool think_scout()
	{
		Nation ownNation = NationArray[nation_recno];

		if (Config.explore_whole_map)
			return false;

		// only in the first half year of the game
		if (Info.game_date > Info.game_start_date.AddDays(50.0 + ownNation.pref_scout))
			return false;

		if (ownNation.ai_town_array.Count > 1) // only when there is only one town
			return false;

		//-------------------------------------------//

		int destX = 0;
		int destY = 0;
		int dir = Misc.Random(4);

		switch (dir)
		{
			case 0:
				destX = Misc.Random(GameConstants.MapSize - 1 - 20) + 10;
				if (center_x - destX > 100)
					destX = center_x - (center_x - destX) % 100;
				if (destX - center_x > 100)
					destX = center_x + (destX - center_x) % 100;
				destY = Math.Max(center_y - 100, 10);
				break;
			case 1:
				destX = Math.Min(center_x + 100, GameConstants.MapSize - 1 - 10);
				destY = Misc.Random(GameConstants.MapSize - 1 - 20) + 10;
				if (center_y - destY > 100)
					destY = center_y - (center_y - destY) % 100;
				if (destY - center_y > 100)
					destY = center_y + (destY - center_y) % 100;
				break;
			case 2:
				destX = Misc.Random(GameConstants.MapSize - 1 - 20) + 10;
				if (center_x - destX > 100)
					destX = center_x - (center_x - destX) % 100;
				if (destX - center_x > 100)
					destX = center_x + (destX - center_x) % 100;
				destY = Math.Min(center_y + 100, GameConstants.MapSize - 1 - 10);
				break;
			case 3:
				destX = Math.Max(center_x - 100, 10);
				destY = Misc.Random(GameConstants.MapSize - 1 - 20) + 10;
				if (center_y - destY > 100)
					destY = center_y - (center_y - destY) % 100;
				if (destY - center_y > 100)
					destY = center_y + (destY - center_y) % 100;
				break;
		}

		ownNation.add_action(destX, destY, loc_x1, loc_y1, Nation.ACTION_AI_SCOUT, 0);

		return true;
	}

	public void update_base_town_status()
	{
		bool newBaseTownStatus = new_base_town_status();

		if (newBaseTownStatus == is_base_town)
			return;

		Nation ownNation = NationArray[nation_recno];

		if (is_base_town)
			ownNation.ai_base_town_count--;

		if (newBaseTownStatus)
			ownNation.ai_base_town_count++;

		is_base_town = newBaseTownStatus;
	}

	public bool new_base_town_status()
	{
		Nation ownNation = NationArray[nation_recno];

		//---- town near mine should be the base ---//

		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			if (FirmArray[linked_firm_array[i]].firm_id == Firm.FIRM_MINE)
				return true;
		}

		//---- if there is a town near mine with low population and only two towns in total then only mine town should be the base ---//

		if (ownNation.ai_town_array.Count == 2)
		{
			int townWithMinePopulation = GameConstants.MAX_TOWN_POPULATION;
			for (int i = 0; i < ownNation.ai_town_array.Count; i++)
			{
				Town town = TownArray[ownNation.ai_town_array[i]];
				for (int j = 0; j < town.linked_firm_array.Count; j++)
				{
					if (FirmArray[town.linked_firm_array[j]].firm_id == Firm.FIRM_MINE)
					{
						townWithMinePopulation = town.population;
					}
				}
			}

			if (townWithMinePopulation < GameConstants.MAX_TOWN_POPULATION / 2)
				return false;
		}

		if (population > 20 + ownNation.pref_territorial_cohesiveness / 10)
			return true;

		//---- if there is only 1 town, then it must be the base town ---//

		if (ownNation.ai_town_array.Count == 1)
			return true;

		//---- if there is only 1 town in this region, then it must be the base town ---//

		AIRegion aiRegion = ownNation.get_ai_region(region_id);

		if (aiRegion != null && aiRegion.town_count == 1)
			return true;

		//--- don't drop if there are employed villagers ---//

		if (jobless_population != population)
			return true;

		//---- if this town is linked to a base town, also don't drop it ----//

		for (int i = linked_town_array.Count - 1; i >= 0; i--)
		{
			Town town = TownArray[linked_town_array[i]];

			if (town.nation_recno == nation_recno && town.is_base_town)
			{
				return true;
			}
		}

		/*
		//---- if there is not a single town meeting the above criteria, then set the town with the largest population to be the base town. ----//

		if( ownNation.ai_base_town_count <= 1 )
		{
			for( i=ownNation.ai_town_count-1 ; i>=0 ; i-- )
			{
				town = TownArray[ ownNation.ai_town_array[i] ];

				if( town.population > population )		// if this town is not the largest town return 0.
					return false;
			}

			return true;		// return 1 if this town is the largest town.
		}*/

		return false;
	}

	public bool think_counter_spy()
	{
		if (population >= GameConstants.MAX_TOWN_POPULATION)
			return false;

		Nation ownNation = NationArray[nation_recno];

		if (!ownNation.ai_should_create_new_spy(1)) //1 means take into account only conter-spies
			return false;

		//------- check if we need additional spies ------//

		int spyCount;
		int curSpyLevel = SpyArray.total_spy_skill_level(Spy.SPY_TOWN, town_recno, nation_recno, out spyCount);
		int neededSpyLevel = needed_anti_spy_level();

		if (neededSpyLevel > curSpyLevel + 30)
		{
			int majorityRace = majority_race();
			if (can_recruit(majorityRace))
			{
				int unitRecno = recruit(Skill.SKILL_SPYING, majorityRace, InternalConstants.COMMAND_AI);

				if (unitRecno == 0)
					return false;

				ActionNode actionNode = ownNation.add_action(loc_x1, loc_y1, -1, -1,
					Nation.ACTION_AI_ASSIGN_SPY, nation_recno, 1, unitRecno);

				if (actionNode == null)
					return false;

				train_unit_action_id = actionNode.action_id;

				return true;
			}
		}

		return false;
	}

	public int needed_anti_spy_level()
	{
		return (linked_firm_array.Count * 10 + 100 * population / GameConstants.MAX_TOWN_POPULATION)
			* (100 + NationArray[nation_recno].pref_counter_spy) / 100;
	}

	public bool think_spying_town()
	{
		//---- don't send spies somewhere if we are low in cash or losing money ----//

		Nation ownNation = NationArray[nation_recno];

		// don't use spies if the population is too low, we need to use have people to grow population
		if (ownNation.total_population < 30 - ownNation.pref_spy / 10)
			return false;

		if (!ownNation.ai_should_create_new_spy(0)) //0 means take into account all spies
			return false;

		int spyCount;
		int curSpyLevel = SpyArray.total_spy_skill_level(Spy.SPY_TOWN, town_recno, nation_recno, out spyCount);
		int neededSpyLevel = needed_anti_spy_level();

		if (curSpyLevel > neededSpyLevel + 30)
		{
			foreach (Spy spy in SpyArray)
			{
				if (spy.true_nation_recno != nation_recno)
					continue;

				if (spy.spy_place != Spy.SPY_TOWN)
					continue;

				if (spy.spy_place_para != town_recno)
					continue;

				if (spy.spy_skill > 50)
				{
					int xLoc;
					int yLoc;
					int cloakedNationRecno;

					bool hasNewMission = ownNation.think_spy_new_mission(spy.race_id, region_id,
						out xLoc, out yLoc, out cloakedNationRecno);

					if (hasNewMission)
					{
						int unitRecno = spy.mobilize_town_spy();
						if (unitRecno != 0)
						{
							ownNation.ai_start_spy_new_mission(unitRecno, xLoc, yLoc, cloakedNationRecno);
							return true;
						}
					}
				}
			}
		}

		return false;
	}

	public bool should_ai_migrate()
	{
		//--- if this town is the base town of the nation's territory, don't migrate ---//
		Nation nation = NationArray[nation_recno];

		// don't migrate if this is a base town or there is no base town in this nation
		if (is_base_town || nation.ai_base_town_count == 0)
			return false;

		if (population - jobless_population > 0) // if there are workers in this town, don't migrate the town people
			return false;

		return true;
	}

	public int detect_enemy(int alertNum)
	{
		//------ check if any enemies have entered in the city -----//

		int xLoc1 = Math.Max(0, loc_x1 - InternalConstants.WALL_SPACE_LOC);
		int yLoc1 = Math.Max(0, loc_y1 - InternalConstants.WALL_SPACE_LOC);
		int xLoc2 = Math.Min(GameConstants.MapSize - 1, loc_x2 + InternalConstants.WALL_SPACE_LOC);
		int yLoc2 = Math.Min(GameConstants.MapSize - 1, loc_y2 + InternalConstants.WALL_SPACE_LOC);
		int unitRecno;
		int enemyCount = 0;

		for (int yLoc = yLoc1; yLoc <= yLoc2; yLoc++)
		{
			for (int xLoc = xLoc1; xLoc <= xLoc2; xLoc++)
			{
				Location loc = World.get_loc(xLoc, yLoc);
				if (loc.has_unit(UnitConstants.UNIT_LAND))
				{
					unitRecno = loc.unit_recno(UnitConstants.UNIT_LAND);

					//------- if any enemy detected -------//

					Unit unit = UnitArray[unitRecno];

					if (unit.nation_recno != nation_recno && unit.nation_recno > 0)
					{
						if (++enemyCount >= alertNum)
							return unitRecno;
					}
				}
			}
		}

		return 0;
	}

	public int protection_needed() // an index from 0 to 100 indicating the military protection needed for this town
	{
		int protectionNeeded = population * 10;

		for (int i = linked_firm_array.Count - 1; i >= 0; i--)
		{
			Firm firm = FirmArray[linked_firm_array[i]];

			if (firm.nation_recno != nation_recno)
				continue;

			//----- if this is a camp, add combat level points -----//

			if (firm.firm_id == Firm.FIRM_MARKET)
			{
				protectionNeeded += ((FirmMarket)firm).stock_value_index() * 2;
			}
			else
			{
				protectionNeeded += (int)firm.productivity * 2;

				if (firm.firm_id == Firm.FIRM_MINE) // more protection for mines
					protectionNeeded += 600;
			}
		}

		return protectionNeeded;
	}

	public int protection_available()
	{
		int protectionLevel = 0;

		for (int i = linked_firm_array.Count - 1; i >= 0; i--)
		{
			Firm firm = FirmArray[linked_firm_array[i]];

			if (firm.nation_recno != nation_recno)
				continue;

			//----- if this is a camp, add combat level points -----//

			if (firm.firm_id == Firm.FIRM_CAMP)
			{
				int linkedTownsCount = 0;
				for (int townIndex = 0; townIndex < firm.linked_town_array.Count; townIndex++)
				{
					Town firmTown = TownArray[firm.linked_town_array[townIndex]];
					if (firmTown.nation_recno == nation_recno)
						linkedTownsCount++;
				}

				int combatLevel = ((FirmCamp)firm).total_combat_level();
				if (linkedTownsCount > 1)
					combatLevel = combatLevel / linkedTownsCount;
				protectionLevel += 10 + combatLevel; // +10 for the existence of the camp structure
			}
		}

		return protectionLevel;
	}

	public void add_protection_camps(List<int> protectionCamps, bool minimumProtection)
	{
		int majorRaceCampRecno = 0;
		List<int> thisTownProtectionCamps = new List<int>();
		Nation ownNation = NationArray[nation_recno];

		//For protection we leave camp with the king, one camp with the general of the major race,
		//camps with injured generals, camps with injured and weak soldiers
		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			int firmRecno = linked_firm_array[i];
			Firm firm = FirmArray[firmRecno];

			if (firm.nation_recno != nation_recno)
				continue;

			if (firm.firm_id != Firm.FIRM_CAMP)
				continue;

			//DieselMachine TODO if there is a battle going on, all linked camps should be protection camps
			FirmCamp firmCamp = (FirmCamp)firm;
			if (firmCamp.overseer_recno == 0 || firmCamp.workers.Count == 0 || firmCamp.patrol_unit_array.Count > 0)
				continue;

			if (firmCamp.overseer_recno == ownNation.king_unit_recno)
			{
				thisTownProtectionCamps.Add(firmRecno);
			}

			Unit overseer = UnitArray[firmCamp.overseer_recno];
			if (majorRaceCampRecno == 0 && overseer.race_id == majority_race())
			{
				majorRaceCampRecno = firmRecno;
				thisTownProtectionCamps.Add(firmRecno);
			}

			if (!minimumProtection)
			{
				if ((overseer.hit_points < overseer.max_hit_points) &&
				    (overseer.hit_points < 100 - ownNation.pref_military_courage / 2))
				{
					thisTownProtectionCamps.Add(firmRecno);
				}

				int lowHitPointsSoldiersCount = 0;
				for (int j = 0; j < firmCamp.workers.Count; j++)
				{
					Worker worker = firmCamp.workers[j];
					if (worker.hit_points < 50.0)
					{
						lowHitPointsSoldiersCount++;
					}
				}

				if (lowHitPointsSoldiersCount > 2 + ownNation.pref_military_courage / 25) //from 2 to 6
				{
					thisTownProtectionCamps.Add(firmRecno);
				}
			}
		}

		//Zero duplicates
		for (int i = 0; i < thisTownProtectionCamps.Count; i++)
		{
			for (int j = i + 1; j < thisTownProtectionCamps.Count; j++)
			{
				if (thisTownProtectionCamps[j] == thisTownProtectionCamps[i])
				{
					thisTownProtectionCamps[j] = 0;
				}
			}
		}

		int totalCombatLevel = 0;
		for (int i = 0; i < thisTownProtectionCamps.Count; i++)
		{
			if (thisTownProtectionCamps[i] != 0)
			{
				FirmCamp firmCamp = (FirmCamp)FirmArray[thisTownProtectionCamps[i]];
				totalCombatLevel += firmCamp.total_combat_level();
			}
		}

		int neededProtection = protection_needed();
		if (totalCombatLevel < neededProtection)
		{
			for (int i = 0; i < linked_firm_array.Count; i++)
			{
				int firmRecno = linked_firm_array[i];
				bool alreadyAdded = false;
				for (int j = 0; j < thisTownProtectionCamps.Count; j++)
				{
					if (firmRecno == thisTownProtectionCamps[j])
					{
						alreadyAdded = true;
						break;
					}
				}

				if (alreadyAdded)
					continue;

				Firm firm = FirmArray[firmRecno];

				if (firm.nation_recno != nation_recno)
					continue;

				if (firm.firm_id != Firm.FIRM_CAMP)
					continue;

				FirmCamp firmCamp = (FirmCamp)firm;
				totalCombatLevel += firmCamp.total_combat_level();
				thisTownProtectionCamps.Add(firmRecno);
				if (totalCombatLevel >= neededProtection)
					break;
			}
		}

		for (int i = 0; i < thisTownProtectionCamps.Count; i++)
		{
			if (thisTownProtectionCamps[i] != 0)
				protectionCamps.Add(thisTownProtectionCamps[i]);
		}
	}

	public bool can_recruit_people()
	{
		if (population == GameConstants.MAX_TOWN_POPULATION)
			return true;

		Nation ownNation = NationArray[nation_recno];
		double prefRecruiting = 0.0;
		if (ownNation.yearly_food_change() > 0)
			prefRecruiting = ownNation.pref_military_development + (100.0 - ownNation.pref_inc_pop_by_growth);
		return linked_camp_soldiers_count() <= population * (4.0 + 4.0 * prefRecruiting / 200.0) / 20.0;
	}

	public bool ai_build_neighbor_firm(int firmId, int firmRaceId = 0)
	{
		int buildXLoc, buildYLoc;
		Nation nation = NationArray[nation_recno];

		if (!nation.find_best_firm_loc(firmId, loc_x1, loc_y1, out buildXLoc, out buildYLoc))
		{
			no_neighbor_space = true;
			return false;
		}

		nation.add_action(buildXLoc, buildYLoc, loc_x1, loc_y1,
			Nation.ACTION_AI_BUILD_FIRM, firmId, 1, 0, firmRaceId);

		return true;
	}

	public bool ai_settle_new(int raceId)
	{
		//-------- locate space for the new town --------//

		int xLoc = loc_x1, yLoc = loc_y1; // xLoc & yLoc are used for returning results

		// InternalConstants.TOWN_WIDTH + 2 for space around the town
		if (!World.locate_space(ref xLoc, ref yLoc, loc_x2, loc_y2,
			    InternalConstants.TOWN_WIDTH + 2, InternalConstants.TOWN_HEIGHT + 2,
			    UnitConstants.UNIT_LAND, region_id, true))
		{
			return false;
		}

		//------- it must be within the effective town-to-town distance ---//

		if (Misc.rects_distance(loc_x1, loc_y1, loc_x2, loc_y2, xLoc, yLoc,
			    xLoc + InternalConstants.TOWN_WIDTH - 1, yLoc + InternalConstants.TOWN_HEIGHT - 1) >
		    InternalConstants.EFFECTIVE_TOWN_TOWN_DISTANCE)
		{
			return false;
		}

		// TODO: Should preferably check for a space that has an attached camp, and if can't find then immediately issue order to build a new camp.

		//--- recruit a unit from the town and order it to settle a new town ---//

		int unitRecno = recruit(-1, raceId, InternalConstants.COMMAND_AI);

		if (unitRecno == 0 || !UnitArray[unitRecno].is_visible())
			return false;

		UnitArray[unitRecno].settle(xLoc + 1, yLoc + 1);

		return true;
	}

	//-------- independent town ai functions ---------//

	public void think_independent_town()
	{
		if (rebel_recno != 0) // if this is a rebel town, its AI will be executed in Rebel::think_town_action()
			return;

		//---- think about toggling town links ----//

		if (Info.TotalDays % 15 == town_recno % 15)
		{
			think_independent_set_link();
		}

		//---- think about independent units join existing nations ----//

		if (Info.TotalDays % 60 == town_recno % 60)
		{
			think_independent_unit_join_nation();
		}

		//----- think about form a new nation -----//

		if (Info.TotalDays % 365 == town_recno % 365)
		{
			think_independent_form_new_nation();
		}
	}

	public void think_independent_set_link()
	{
		//---- think about working for foreign firms ------//

		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			Firm firm = FirmArray[linked_firm_array[i]];

			if (firm.firm_id == Firm.FIRM_CAMP) // a town cannot change its status with a military camp
				continue;

			//---- think about the link status ----//

			bool linkStatus = false;

			if (firm.nation_recno == 0) // if the firm is also an independent firm
				linkStatus = true;

			if (average_resistance(firm.nation_recno) <= GameConstants.INDEPENDENT_LINK_RESISTANCE)
				linkStatus = true;

			//---- set the link status -------//

			toggle_firm_link(i + 1, linkStatus, InternalConstants.COMMAND_AI);
		}

		ai_link_checked = true;
	}

	public bool think_independent_form_new_nation()
	{
		if (Misc.Random(10) > 0) // 1/10 chance to set up a new nation.
			return false;

		//-------- check if the town is big enough -------//

		if (population < 30)
			return false;

		//---- don't form if the world is already densely populated ----//

		if (NationArray.all_nation_population > ConfigAdv.town_ai_emerge_nation_pop_limit)
			return false;

		//----------------------------------------------//

		if (!NationArray.can_form_new_ai_nation())
			return false;

		//----------------------------------------------//

		return form_new_nation();
	}

	public bool think_independent_unit_join_nation()
	{
		if (jobless_population == 0)
			return false;

		independent_unit_join_nation_min_rating -= 2; // make it easier to join nation everytime it's called
		// -2 each time, adding of 30 after a unit has been recruited and calling it once every 2 months make it a normal rate of joining once a year per town

		//------ think about which nation to turn towards -----//

		int bestNationRecno = 0, curRating, raceId, bestRaceId = 0;
		int bestRating = independent_unit_join_nation_min_rating;

		if (RegionArray.GetRegionInfo(region_id).region_stat_id == 0)
			return false;

		RegionStat regionStat = RegionArray.get_region_stat(region_id);

		foreach (Nation nation in NationArray)
		{
			if (nation.race_id == 0)
				continue;

			if (nation.cash <= 0)
				continue;

			// don't join too frequently, at most 3 months a unit
			if (Info.game_date < nation.last_independent_unit_join_date.AddDays(90.0))
				continue;

			//--- only join the nation if the nation has town in the town's region ---//

			if (regionStat.town_nation_count_array[nation.nation_recno - 1] == 0)
				continue;

			//----- calculate the rating of the nation -----//

			curRating = (int)nation.reputation + nation.overall_rating;

			if (recruitable_race_pop(nation.race_id, false) > 0) // 0-don't count spies
			{
				curRating += 30;
				raceId = nation.race_id;
			}
			else
				raceId = 0;

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestNationRecno = nation.nation_recno;
				bestRaceId = raceId;
			}
		}

		//--------------------------------------------//

		if (bestNationRecno == 0)
			return false;

		// 0-only pick jobless unit, 0-don't pick spy units
		if (bestRaceId == 0)
			bestRaceId = pick_random_race(false, false);

		if (bestRaceId == 0)
			return false;

		if (!independent_unit_join_nation(bestRaceId, bestNationRecno))
			return false;

		//--- set a new value to independent_unit_join_nation_min_rating ---//

		independent_unit_join_nation_min_rating = bestRating + 100 + Misc.Random(30); // reset it to a higher rating

		if (independent_unit_join_nation_min_rating < 100)
			independent_unit_join_nation_min_rating = 100;

		return true;
	}

	public bool independent_unit_join_nation(int raceId, int toNationRecno)
	{
		//----- mobilize a villager ----//

		// 1-dec population after mobilizing the unit, 0-don't mobilize spies
		int unitRecno = mobilize_town_people(raceId, true, false);

		if (unitRecno == 0)
			return false;

		Unit unit = UnitArray[unitRecno];

		//----- set the skills of the unit -----//

		int skillId = 0, skillLevel = 0, combatLevel = 0;

		switch (Misc.Random(3))
		{
			case 0: // leaders
				skillId = Skill.SKILL_LEADING;

				if (Misc.Random(3) == 0)
					skillLevel = Misc.Random(100);
				else
					skillLevel = Misc.Random(50);

				combatLevel = skillLevel + Misc.Random(40) - 20;
				combatLevel = Math.Min(combatLevel, 100);
				combatLevel = Math.Max(combatLevel, 10);
				break;

			case 1: // peasants
				skillId = 0;
				skillLevel = 0;
				combatLevel = 10;
				break;

			case 2: // skilled units
				skillId = Misc.Random(Skill.MAX_TRAINABLE_SKILL) + 1;
			{
				while (skillId == Skill.SKILL_SPYING)
				{
					if (++skillId > Skill.MAX_TRAINABLE_SKILL)
						skillId = 1;
				}
			}
				skillLevel = 10 + Misc.Random(80);
				combatLevel = 10 + Misc.Random(30);
				break;
		}

		//--------------------------------------//

		unit.skill.skill_id = skillId;
		unit.skill.skill_level = skillLevel;
		unit.set_combat_level(combatLevel);

		//------ change nation now --------//

		if (!unit.betray(toNationRecno))
			return false;

		//---- the unit moves close to the newly joined nation ----//

		unit.ai_move_to_nearby_town();

		//-------- set last_independent_unit_join_date --------//

		NationArray[toNationRecno].last_independent_unit_join_date = Info.game_date;

		return true;
	}
}
