using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class Town : IIdObject
{
	public int TownId { get; private set; }
	public int NationId { get; private set; }
	private DateTime _setupDate; // the setup date of this town

	
	public int LocX1 { get; private set; }
	public int LocY1 { get; private set; }
	public int LocX2 { get; private set; }
	public int LocY2 { get; private set; }
	public int LocCenterX { get; private set; }
	public int LocCenterY { get; private set; }
	public int RegionId { get; private set; }
	public int TownNameId { get; private set; }
	public int LayoutId { get; private set; }
	public int[] SlotObjectIds { get; } = new int[TownLayout.MAX_TOWN_LAYOUT_SLOT]; // the race id. of each slot building
	public string Name => TownRes.GetName(TownNameId);


	public int Population { get; private set; }
	public int JoblessPopulation { get; set; }
	public int WorkerPopulation => Population - JoblessPopulation;


	public int[] RacesPopulation { get; } = new int[GameConstants.MAX_RACE]; // population of each race
	// population growth, when it reaches 100, there will be one more person in the town
	public int[] RacesJoblessPopulation { get; } = new int[GameConstants.MAX_RACE];
	private int[] RacesPopulationGrowth { get; } = new int[GameConstants.MAX_RACE];
	// the MAX population the current town layout supports
	private int[] RacesMaxPopulation { get; } = new int[GameConstants.MAX_RACE];

	public double[] RacesLoyalty { get; } = new double[GameConstants.MAX_RACE];
	public int[] RacesTargetLoyalty { get; } = new int[GameConstants.MAX_RACE];
	public double[,] RacesResistance { get; } = new double[GameConstants.MAX_RACE, GameConstants.MAX_NATION];
	public int[,] RacesTargetResistance { get; } = new int[GameConstants.MAX_RACE, GameConstants.MAX_NATION];

	public int[] RacesSpyCount { get; } = new int[GameConstants.MAX_RACE]; // no. of spies in each race


	public int AccumulatedCollectTaxPenalty { get; private set; }
	public int AccumulatedRewardPenalty { get; private set; }
	public int AccumulatedRecruitPenalty { get; private set; }
	public int AccumulatedEnemyGrantPenalty { get; private set; }
	private int _autoCollectTaxLoyalty; // auto collect tax if the loyalty reaches this level
	private int _autoGrantLoyalty; // auto grant if the loyalty drop below this level

	
	public List<int> LinkedFirms { get; } = new List<int>();
	public List<int> LinkedTowns { get; } = new List<int>();
	public List<int> LinkedFirmsEnable { get; } = new List<int>();
	public List<int> LinkedTownsEnable { get; } = new List<int>();
	public bool HasLinkedOwnCamp { get; private set; }
	public bool HasLinkedEnemyCamp { get; private set; }

	
	// each bit n is representing this independent town will attack nation n.
	private int _independentTownNationRelation;
	private int _independentUnitJoinNationMinRating;
	public int RebelId { get; set; } // whether this town is controlled by rebels
	private DateTime _lastRebelDate;

	
	public int DefendersCount { get; private set; } // no. of units currently defending this town
	public int DefendTargetId { get; private set; } // used in defend mode, store id of latest target attacking this town
	// no. of received hit by attackers, when this > RECEIVED_HIT_PER_KILL, a town people will be killed
	private double _receivedHitCount;
	public DateTime LastBeingAttackedDate { get; private set; }


	public List<int> TrainSkillQueue { get; } = new List<int>(); // it stores the skill id.
	private readonly List<int> _trainRaceQueue = new List<int>(); // it stores the race id.
	public int TrainUnitId { get; private set; } // race id. of the unit the town is currently training, 0-if currently not training any
	public int TrainUnitActionId { get; set; } // id. of the action to be assigned to this unit when it is finished training.
	private long _startTrainFrameNumber;


	private int _qualityOfLife;
	public bool[] HasProductSupply { get; } = new bool[GameConstants.MAX_PRODUCT];
	public bool NoNeighborSpace { get; set; } // true if there is no space to build firms/towns next to this town


	//----------- AI vars ------------//
	public bool AITown { get; private set; }
	// AI check firm and town location by links, disable checking by setting this parameter to 1
	public bool AILinkChecked { get; set; }
	public bool IsBaseTown { get; private set; }
	public int TownCombatLevel { get; private set; } // combat level of the people in this town

	
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

	private RegionArray RegionArray => Sys.Instance.RegionArray;
	private NationArray NationArray => Sys.Instance.NationArray;
	private FirmArray FirmArray => Sys.Instance.FirmArray;
	private TownArray TownArray => Sys.Instance.TownArray;
	private UnitArray UnitArray => Sys.Instance.UnitArray;
	private RebelArray RebelArray => Sys.Instance.RebelArray;
	private SpyArray SpyArray => Sys.Instance.SpyArray;
	private SiteArray SiteArray => Sys.Instance.SiteArray;
	private NewsArray NewsArray => Sys.Instance.NewsArray;

	
	public Town()
	{
	}

	void IIdObject.SetId(int id)
	{
		TownId = id;
	}

	public void Init(int nationId, int raceId, int locX, int locY)
	{
		NationId = nationId;

		//---- set the town section's absolute positions on the map ----//

		LocX1 = locX;
		LocY1 = locY;
		LocX2 = LocX1 + InternalConstants.TOWN_WIDTH - 1;
		LocY2 = LocY1 + InternalConstants.TOWN_HEIGHT - 1;

		LocCenterX = (LocX1 + LocX2) / 2;
		LocCenterY = (LocY1 + LocY2) / 2;

		RegionId = World.GetRegionId(LocCenterX, LocCenterY);

		AITown = (NationId == 0 || NationArray[NationId].nation_type == NationBase.NATION_AI);
		AILinkChecked = true; // check the linked towns and firms connected only if AILinkChecked is false

		// the minimum rating a nation must have in order for an independent unit to join it
		_independentUnitJoinNationMinRating = 100 + Misc.Random(150);

		_setupDate = Info.game_date;

		//-------- init resistance ------------//

		if (NationId == 0)
		{
			for (int i = 0; i < GameConstants.MAX_RACE; i++)
			{
				for (int j = 0; j < GameConstants.MAX_NATION; j++)
				{
					RacesResistance[i, j] = 60.0 + Misc.Random(40);
					RacesTargetResistance[i, j] = -1;
				}
			}

			//--- some independent towns have higher than normal combat level for its defender ---//

			switch (Config.independent_town_resistance)
			{
				case Config.OPTION_LOW:
					TownCombatLevel = GameConstants.CITIZEN_COMBAT_LEVEL;
					break;

				case Config.OPTION_MODERATE:
					TownCombatLevel = 10 + Misc.Random(20);
					break;

				case Config.OPTION_HIGH:
					TownCombatLevel = 10 + Misc.Random(30);
					if (Misc.Random(5) == 0)
						TownCombatLevel += Misc.Random(30);
					break;
			}
		}

		//-------------------------------------//

		TownNameId = TownRes.GetNewNameId(raceId);

		SetWorldMatrix();

		SetupLink();

		//-------- if this is an AI town ------//

		if (AITown && NationId != 0)
		{
			NationArray[NationId].add_town_info(TownId);

			UpdateBaseTownStatus();
		}

		//------ set national auto policy -----//

		if (NationId != 0)
		{
			Nation nation = NationArray[NationId];

			SetAutoCollectTaxLoyalty(nation.auto_collect_tax_loyalty);
			SetAutoGrantLoyalty(nation.auto_grant_loyalty);
		}
	}

	public void Deinit()
	{
		ClearDefenseMode();

		//------- if it's an AI town --------//

		if (AITown && NationId != 0)
			NationArray[NationId].del_town_info(TownId);

		//------ if this town is the nation's largest town, reset it ----//

		if (NationId != 0 && NationArray[NationId].largest_town_recno == TownId)
			NationArray[NationId].largest_town_recno = 0;

		//-----------------------------------//

		RestoreWorldMatrix();

		ReleaseLink();

		//-- if there is a unit being trained when the town vanishes --//

		if (TrainUnitId != 0)
			UnitArray.DisappearInTown(UnitArray[TrainUnitId], this);
	}

	public void NextDay()
	{
		UpdateQualityOfLife();

		if (Info.TotalDays % 30 == TownId % 30)
		{
			PopulationGrow();
		}

		UpdateCampLink();

		if (Info.TotalDays % 15 == TownId % 15)
		{
			if (NationId != 0)
			{
				UpdateTargetLoyalty();
			}
			else
			{
				UpdateTargetResistance();
			}
		}

		if (Info.TotalDays % 5 == TownId % 5)
		{
			if (NationId != 0)
			{
				UpdateLoyalty();
			}
			else
			{
				UpdateResistance();
			}
		}

		if (ConfigAdv.town_migration && Info.TotalDays % 15 == TownId % 15)
		{
			// TODO migration is too often
			ThinkMigrate();

			if (TownArray.IsDeleted(TownId))
				return;
		}

		if (NationId != 0 && Info.TotalDays % 15 == TownId % 15)
		{
			ThinkRebel();

			if (TownArray.IsDeleted(TownId))
				return;
		}

		if (NationId != 0 && (Info.TotalDays % 15 == TownId % 15 || AverageLoyalty() == 0))
		{
			ThinkSurrender(); // for nation town only, independent town surrender is handled in UpdateResistance()
		}

		if (NationId != 0)
		{
			if (TrainUnitId != 0)
			{
				ProcessTrain();
			}
			else
			{
				ProcessQueue();
			}

			// when the last peasant in the town is trained, the town disappears
			if (TownArray.IsDeleted(TownId))
				return;
		}

		if (NationId != 0)
		{
			ProcessFood();
		}

		if (NationId != 0)
		{
			ProcessAuto();

			// town may rebel when collecting taxes
			if (TownArray.IsDeleted(TownId))
				return;
		}

		/*if (NationId != 0 && Info.GameMonth == 1 && Info.GameDay == 1)
		{
			CollectYearlyTax();
		}*/

		if (Info.TotalDays % 30 == TownId % 30)
			SpyArray.CatchSpy(Spy.SPY_TOWN, TownId);

		if (TownArray.IsDeleted(TownId))
			return;

		//-------- update visibility ---------//

		if (NationId == NationArray.player_recno || (NationId != 0 && NationArray[NationId].is_allied_with_player))
		{
			World.Visit(LocX1, LocY1, LocX2, LocY2, GameConstants.EXPLORE_RANGE - 1);
		}

		//--- recheck NoNeighborSpace after a period, there may be new space available now ---//

		if (NoNeighborSpace && Info.TotalDays % 10 == TownId % 10)
		{
			// for independent town, since we can't call FindBestFirmLoc(), we just set NoNeighborSpace to false every 10 days,
			// if it still has no space, then NoNeighborSpace will be set 1 again

			// check for FIRM_RESEARCH because it has the smallest size
			if (NationId == 0 || NationArray[NationId].find_best_firm_loc(Firm.FIRM_RESEARCH, LocX1, LocY1, out _, out _))
				NoNeighborSpace = false;
		}

		//------ decrease penalties -----//

		if (AccumulatedCollectTaxPenalty > 0)
			AccumulatedCollectTaxPenalty--;

		if (AccumulatedRewardPenalty > 0)
			AccumulatedRewardPenalty--;

		if (AccumulatedRecruitPenalty > 0)
			AccumulatedRecruitPenalty--;

		if (AccumulatedEnemyGrantPenalty > 0)
			AccumulatedEnemyGrantPenalty--;
	}

	public int LocWidth()
	{
		return LocX2 - LocX1 + 1;
	}

	public int LocHeight()
	{
		return LocY2 - LocY1 + 1;
	}
	
	private void SetWorldMatrix()
	{
		for (int locY = LocY1; locY <= LocY2; locY++)
		{
			for (int locX = LocX1; locX <= LocX2; locX++)
			{
				Location location = World.GetLoc(locX, locY);

				if (location.CargoId == 0) // skip the location where the settle unit is standing
					location.SetTown(TownId);
			}
		}

		//--- if a nation sets up a town in a location that the player has explored, contact between the nation and the player is established ---//

		EstablishContactWithPlayer();

		//---- set this town's influence on the map ----//

		if (NationId != 0)
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
				World.GetLoc(locX, locY).RemoveTown();
			}
		}

		//---- restore this town's influence on the map ----//

		if (NationId != 0)
			World.RestorePower(LocX1, LocY1, LocX2, LocY2, TownId, 0);
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
						//if( !relation->contact_msg_flag && !relation->has_contact )
						//{
							// packet structure : <player nation> <explored nation>
							//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_NATION_CONTACT, 2*sizeof(short));
							//*shortPtr = nation_array.player_recno;
							//shortPtr[1] = nation_recno;
							//relation->contact_msg_flag = 1;
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

	public void AutoSetLayout()
	{
		LayoutId = ThinkLayoutId();

		TownLayout townLayout = TownRes.GetLayout(LayoutId);

		for (int i = 0; i < SlotObjectIds.Length; i++)
			SlotObjectIds[i] = 0;
		for (int i = 0; i < RacesMaxPopulation.Length; i++)
			RacesMaxPopulation[i] = 0;

		int[] raceNeedBuildCount = new int[GameConstants.MAX_RACE];
		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (RacesPopulation[i] > 0)
				raceNeedBuildCount[i] = (RacesPopulation[i] - 1) / TownRes.POPULATION_PER_HOUSE + 1;
		}

		//--- assign the first house to each race, each present race will at least have one house ---//

		for (int i = 0; i < townLayout.SlotCount; i++)
		{
			TownSlot townSlot = TownRes.GetSlot(townLayout.FirstSlotId + i);
			if (townSlot.BuildType == TownSlot.TOWN_OBJECT_HOUSE)
			{
				int raceId = ConfigAdv.GetRandomRace();
				for (int j = 1; j <= GameConstants.MAX_RACE; j++)
				{
					raceId++;
					if (raceId > GameConstants.MAX_RACE)
						raceId = 1;
					
					// if this race needs buildings, give them a building
					if (raceNeedBuildCount[raceId - 1] > 0)
						break;
				}

				SlotObjectIds[i] = TownRes.ScanBuild(townLayout.FirstSlotId + i, raceId);
				raceNeedBuildCount[raceId - 1]--;
				RacesMaxPopulation[raceId - 1] += TownRes.POPULATION_PER_HOUSE;
			}
		}

		//------- distribute the remaining houses -------//

		for (int i = 0; i < townLayout.SlotCount; i++)
		{
			TownSlot townSlot = TownRes.GetSlot(townLayout.FirstSlotId + i);
			if (townSlot.BuildType == TownSlot.TOWN_OBJECT_HOUSE && SlotObjectIds[i] == 0)
			{
				int bestRaceId = 0;
				int maxNeedBuildCount = 0;

				for (int raceId = 1; raceId <= GameConstants.MAX_RACE; raceId++)
				{
					if (raceNeedBuildCount[raceId - 1] > maxNeedBuildCount)
					{
						bestRaceId = raceId;
						maxNeedBuildCount = raceNeedBuildCount[raceId - 1];
					}
				}

				if (bestRaceId == 0) // all races have assigned with their needed houses
					break;

				SlotObjectIds[i] = TownRes.ScanBuild(townLayout.FirstSlotId + i, bestRaceId);
				raceNeedBuildCount[bestRaceId - 1]--;
				RacesMaxPopulation[bestRaceId - 1] += TownRes.POPULATION_PER_HOUSE;
			}
		}

		//------- set plants in the town layout -------//

		for (int i = 0; i < townLayout.SlotCount; i++)
		{
			TownSlot townSlot = TownRes.GetSlot(townLayout.FirstSlotId + i);
			switch (townSlot.BuildType)
			{
				case TownSlot.TOWN_OBJECT_PLANT:
					// 'T' - town only, 1st 0 - any zone area, 3rd 0 - age level
					SlotObjectIds[i] = PlantRes.scan(0, 'T', 0);
					break;

				case TownSlot.TOWN_OBJECT_FARM:
					SlotObjectIds[i] = townSlot.BuildCode;
					break;

				case TownSlot.TOWN_OBJECT_HOUSE:
					if (SlotObjectIds[i] == 0)
						SlotObjectIds[i] = TownRes.ScanBuild(townLayout.FirstSlotId + i, ConfigAdv.GetRandomRace());
					break;
			}
		}
	}

	private int ThinkLayoutId()
	{
		int needBuildCount = 0; // basic buildings needed
		int extraBuildCount = 0; // extra buildings needed beside the basic one

		//---- count the needed buildings of each race ----//

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (RacesPopulation[i] == 0)
				continue;

			needBuildCount++; // essential buildings needed

			// extra buildings, which are not necessary, but will look better if the layout plan fits with this number
			if (RacesPopulation[i] > TownRes.POPULATION_PER_HOUSE)
				extraBuildCount += (RacesPopulation[i] - TownRes.POPULATION_PER_HOUSE - 1) / TownRes.POPULATION_PER_HOUSE + 1;
		}

		//---------- scan the town layout ---------//
		int layoutId1;
		// scan from the most densed layout to the least densed layout
		for (layoutId1 = TownRes.TownLayouts.Length; layoutId1 > 0; layoutId1--)
		{
			TownLayout townLayout = TownRes.GetLayout(layoutId1);

			//--- if this plan has less than the essential need ---//

			int countDiff = townLayout.BuildCount - (needBuildCount + extraBuildCount);

			if (countDiff == 0) // the match is perfect, return now
				break;

			// since we scan from the most densed town layout to the least densed one, if cannot find anyone matched now,
			// there won't be any in the lower positions of the array
			if (countDiff < 0)
			{
				layoutId1 = TownRes.TownLayouts.Length;
				break;
			}
		}

		//--- if there are more than one layout with the same number of building, pick one randomly ---//

		int layoutBuildCount = TownRes.GetLayout(layoutId1).BuildCount;
		int layoutId2;

		for (layoutId2 = layoutId1 - 1; layoutId2 > 0; layoutId2--)
		{
			TownLayout townLayout = TownRes.GetLayout(layoutId2);

			if (layoutBuildCount != townLayout.BuildCount)
				break;
		}

		layoutId2++; // the lowest layout id. that has the same no. of buildings

		//------- return the result layout id -------//

		return layoutId2 + Misc.Random(layoutId1 - layoutId2 + 1);
	}

	public void InitPopulation(int raceId, int addPop, int loyalty, bool hasJob, bool firstInit)
	{
		if (Population >= GameConstants.MAX_TOWN_POPULATION)
			return;

		int addPopulation = Math.Min(addPop, GameConstants.MAX_TOWN_POPULATION - Population);

		//-------- update population ---------//

		RacesPopulation[raceId - 1] += addPopulation;
		Population += addPopulation;

		if (!hasJob)
		{
			JoblessPopulation += addPopulation;
			RacesJoblessPopulation[raceId - 1] += addPopulation;
		}

		//------- update loyalty --------//

		if (firstInit)
		{
			if (NationId != 0)
			{
				RacesLoyalty[raceId - 1] = loyalty;
				RacesTargetLoyalty[raceId - 1] = loyalty;
			}
			else
			{
				for (int j = 0; j < GameConstants.MAX_NATION; j++) // reset resistance for non-existing races
				{
					RacesResistance[raceId - 1, j] = loyalty;
					RacesTargetResistance[raceId - 1, j] = loyalty;
				}
			}
		}
		else
		{
			if (NationId != 0)
			{
				RacesLoyalty[raceId - 1] = (RacesLoyalty[raceId - 1] * (RacesPopulation[raceId - 1] - addPopulation)
				                            + loyalty * addPopulation) / RacesPopulation[raceId - 1];

				RacesTargetLoyalty[raceId - 1] = (int)RacesLoyalty[raceId - 1];
			}
			else
			{
				for (int j = 0; j < GameConstants.MAX_NATION; j++) // reset resistance for non-existing races
				{
					RacesResistance[raceId - 1, j] = (RacesResistance[raceId - 1, j] * (RacesPopulation[raceId - 1] - addPopulation)
					                                  + loyalty * addPopulation) / RacesPopulation[raceId - 1];

					RacesTargetResistance[raceId - 1, j] = (int)RacesResistance[raceId - 1, j];
				}
			}
		}

		//---------- update town parameters ----------//

		TownArray.DistributeDemand();

		if (NationId != 0)
			UpdateLoyalty();
		else
			UpdateResistance();
	}

	public void IncPopulation(int raceId, bool unitHasJob, int unitLoyalty)
	{
		//---------- increase population ----------//

		Population++;
		RacesPopulation[raceId - 1]++;

		if (!unitHasJob)
		{
			JoblessPopulation++;
			RacesJoblessPopulation[raceId - 1]++;
		}

		//------- update loyalty --------//

		if (NationId != 0)
		{
			RacesLoyalty[raceId - 1] = (RacesLoyalty[raceId - 1] * (RacesPopulation[raceId - 1] - 1) + unitLoyalty) / RacesPopulation[raceId - 1];
		}

		//-- if the race's population exceeds the capacity of the town layout --//

		if (RacesPopulation[raceId - 1] > RacesMaxPopulation[raceId - 1])
		{
			AutoSetLayout();
		}
	}

	public void DecPopulation(int raceId, bool unitHasJob)
	{
		Population--;
		RacesPopulation[raceId - 1]--;

		if (!unitHasJob)
		{
			JoblessPopulation--;
			RacesJoblessPopulation[raceId - 1]--;
		}

		//------- if all the population are gone --------//

		if (Population == 0)
		{
			if (NationId == NationArray.player_recno)
				NewsArray.town_abandoned(TownId);

			TownArray.DeleteTown(this);
			return;
		}

		//-- if the race's population drops to too low, change the town layout --//

		if (RacesPopulation[raceId - 1] <= RacesMaxPopulation[raceId - 1] - TownRes.POPULATION_PER_HOUSE)
		{
			AutoSetLayout();
		}
	}

	private void MovePopulation(Town destTown, int raceId, bool hasJob)
	{
		//--- if the only pop of this race in the source town are spies ---//

		// only for peasant, for job unit, spy_place==Spy.SPY_FIRM and it isn't related to RacesSpyCount[]
		if (!hasJob)
		{

			if (RacesSpyCount[raceId - 1] == RacesJoblessPopulation[raceId - 1])
			{
				int spySeqId = Misc.Random(RacesSpyCount[raceId - 1]) + 1; // randomly pick one of the spies

				int spyId = SpyArray.FindTownSpy(TownId, raceId, spySeqId);

				SpyArray[spyId].SpyPlaceId = destTown.TownId; // set the place_para of the spy

				RacesSpyCount[raceId - 1]--;
				destTown.RacesSpyCount[raceId - 1]++;
			}
		}

		//------------------------------------------//

		destTown.IncPopulation(raceId, hasJob, (int)RacesLoyalty[raceId - 1]);

		// the unit doesn't have a job - this must be called finally as dec_pop() will have the whole town deleted if there is only one pop left
		DecPopulation(raceId, hasJob);
	}

	private void PopulationGrow()
	{
		if (DefendersCount > 0)
			return;

		if (Population >= GameConstants.MAX_TOWN_GROWTH_POPULATION || Population >= GameConstants.MAX_TOWN_POPULATION)
			return;

		//-------- population growth by birth ---------//

		bool autoSetFlag = false;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			//-- the population growth in an independent town is slower than in a nation town ---//

			double loyaltyMultiplier = RacesLoyalty[i] * 4.0 / 50.0 - 3.0;
			if (loyaltyMultiplier < 0.0)
				loyaltyMultiplier = 0.0;

			if (NationId != 0)
				RacesPopulationGrowth[i] += (int)((double)RacesPopulation[i] * loyaltyMultiplier);
			else
				RacesPopulationGrowth[i] += RacesPopulation[i];

			while (RacesPopulationGrowth[i] > 100)
			{
				RacesPopulationGrowth[i] -= 100;

				RacesPopulation[i]++;
				RacesJoblessPopulation[i]++;

				Population++;
				JoblessPopulation++;

				//-- if the race's population grows too high, change the town layout --//

				if (RacesPopulation[i] > RacesMaxPopulation[i])
					autoSetFlag = true;

				if (Population >= GameConstants.MAX_TOWN_POPULATION)
					break;
			}

			if (Population >= GameConstants.MAX_TOWN_POPULATION)
				break;
		}

		if (autoSetFlag)
			AutoSetLayout();
	}
	
	public int RaceHarmony(int raceId)
	{
		return Population != 0 ? 100 * RacesPopulation[raceId - 1] / Population : 0;
	}

	public int MajorityRace()
	{
		int mostRaceCount = 0, mostRaceId = 0;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (RacesPopulation[i] > mostRaceCount)
			{
				mostRaceCount = RacesPopulation[i];
				mostRaceId = i + 1;
			}
		}

		return mostRaceId;
	}
	
	public void GetMostPopulatedRace(out int mostRaceId1, out int mostRaceId2)
	{
		//--- find the two races with most population in the town ---//

		mostRaceId1 = 0;
		mostRaceId2 = 0;

		int mostRacePop1 = 0, mostRacePop2 = 0;
		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			int racePop = RacesPopulation[i];

			if (racePop == 0)
				continue;

			if (racePop >= mostRacePop1)
			{
				mostRacePop2 = mostRacePop1;
				mostRacePop1 = racePop;

				mostRaceId2 = mostRaceId1;
				mostRaceId1 = i + 1;
			}
			else if (racePop >= mostRacePop2)
			{
				mostRacePop2 = racePop;
				mostRaceId2 = i + 1;
			}
		}
	}
	
	public int PickRandomRace(bool pickNonRecruitableAlso, bool pickSpyFlag)
	{
		int totalPop = 0;

		if (pickNonRecruitableAlso)
		{
			totalPop = Population;
		}
		else
		{
			totalPop = JoblessPopulation;
			if (TrainUnitId != 0)
				totalPop--;

			if (!pickSpyFlag) // if don't pick spies
			{
				for (int i = 0; i < GameConstants.MAX_RACE; i++)
					totalPop -= RacesSpyCount[i];

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
				popSum += RacesPopulation[i];
			else
				popSum += RecruitableRacePopulation(i + 1, pickSpyFlag);

			if (randomPersonId <= popSum)
				return i + 1;
		}

		return 0;
	}


	public int AverageLoyalty()
	{
		if (Population == 0)
			return 0;
		
		int totalLoyalty = 0;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
			totalLoyalty += (int)RacesLoyalty[i] * RacesPopulation[i];

		return totalLoyalty / Population;
	}

	public int AverageTargetLoyalty()
	{
		if (Population == 0)
			return 0;

		int totalLoyalty = 0;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
			totalLoyalty += RacesTargetLoyalty[i] * RacesPopulation[i];

		return totalLoyalty / Population;
	}

	public int AverageResistance(int nationId)
	{
		if (Population == 0)
			return 0;

		double totalResistance = 0.0;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			int thisPop = RacesPopulation[i];
			if (thisPop > 0)
				totalResistance += RacesResistance[i, nationId - 1] * thisPop;
		}

		return (int)totalResistance / Population;
	}

	public int AverageTargetResistance(int nationId)
	{
		if (Population == 0)
			return 0;

		int totalResistance = 0;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			int thisPop = RacesPopulation[i];

			if (thisPop > 0)
			{
				//TODO check this
				int t = RacesTargetResistance[i, nationId - 1];

				if (t >= 0) // -1 means no target
					totalResistance += t * thisPop;
				else
					totalResistance += (int)RacesResistance[i, nationId - 1] * thisPop;
			}
		}

		return totalResistance / Population;
	}

	public void ChangeLoyalty(int raceId, double loyaltyChange)
	{
		double newLoyalty = RacesLoyalty[raceId - 1] + loyaltyChange;

		newLoyalty = Math.Min(100.0, newLoyalty);
		newLoyalty = Math.Max(0.0, newLoyalty);

		RacesLoyalty[raceId - 1] = newLoyalty;
	}

	public void ChangeResistance(int raceId, int nationId, double resistanceChange)
	{
		double newResistance = RacesResistance[raceId - 1, nationId - 1] + resistanceChange;

		newResistance = Math.Min(100.0, newResistance);
		newResistance = Math.Max(0.0, newResistance);

		RacesResistance[raceId - 1, nationId - 1] = newResistance;
	}

	private void UpdateLoyalty()
	{
		if (NationId == 0)
			return;

		//------------- update loyalty -------------//

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (RacesPopulation[i] == 0)
				continue;

			double targetLoyalty = RacesTargetLoyalty[i];

			if (RacesLoyalty[i] < targetLoyalty)
			{
				//--- if this town is linked to enemy camps, but no own camps, no increase in loyalty, only decrease ---//

				if (!HasLinkedOwnCamp && HasLinkedEnemyCamp)
					continue;

				//-------------------------------------//

				double loyaltyInc = (targetLoyalty - RacesLoyalty[i]) / 30.0;

				ChangeLoyalty(i + 1, Math.Max(loyaltyInc, 0.5));
			}
			else if (RacesLoyalty[i] > targetLoyalty)
			{
				double loyaltyDec = (RacesLoyalty[i] - targetLoyalty) / 30.0;

				ChangeLoyalty(i + 1, -Math.Max(loyaltyDec, 0.5));
			}
		}
	}

	public void UpdateTargetLoyalty()
	{
		if (NationId == 0)
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

		Nation nation = NationArray[NationId];
		int nationRaceId = nation.race_id;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (RacesPopulation[i] == 0)
				continue;

			//------- calculate the target loyalty -------//

			// 0 to 33 + -25 to +25
			int targetLoyalty = RaceHarmony(i + 1) / 3 + (int)(nation.reputation / 4.0);

			//---- employment help increase loyalty ----//

			targetLoyalty += 30 - 30 * RacesJoblessPopulation[i] / RacesPopulation[i]; // +0 to +30

			if (RaceRes.is_same_race(i + 1, nationRaceId))
				targetLoyalty += 10;

			if (targetLoyalty < 0) // targetLoyalty can be negative if there are hostile races conflicts
				targetLoyalty = 0;

			if (targetLoyalty > 100)
				targetLoyalty = 100;

			//----------------------------------------//

			RacesTargetLoyalty[i] = targetLoyalty;
		}

		//----- process command bases that have influence on this town -----//

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			if (LinkedFirmsEnable[i] != InternalConstants.LINK_EE)
				continue;

			Firm firm = FirmArray[LinkedFirms[i]];

			if (firm.FirmType != Firm.FIRM_CAMP || firm.OverseerId == 0)
				continue;

			//-------- get nation and commander info ------------//

			Unit overseer = UnitArray[firm.OverseerId];

			Nation baseNation = NationArray[firm.NationId];

			//------ if this race is the overseer's race -------//

			//TODO targetLoyalty should depend on overseer's nation reputation
			int baseInfluence = overseer.Skill.GetSkillLevel(Skill.SKILL_LEADING) / 3; // 0 to 33

			if (overseer.Rank == Unit.RANK_KING) // 20 points bonus for king
				baseInfluence += 20;

			//------------ update all race -----------//

			for (int j = 0; j < GameConstants.MAX_RACE; j++)
			{
				if (RacesPopulation[j] == 0)
					continue;

				//---- if the overseer's race is the same as this race ---//

				int thisInfluence = baseInfluence;

				if (overseer.RaceId == j + 1)
					thisInfluence += 8;

				//--- if the overseer's nation's race is the same as this race ---//

				if (baseNation.race_id == j + 1)
					thisInfluence += 8;

				//------------------------------------------//

				if (firm.NationId == NationId) // if the command base belongs to the same nation
				{
					//TODO thisInfluence should be lesser with each other camp 
					int targetLoyalty = RacesTargetLoyalty[j] + thisInfluence;
					RacesTargetLoyalty[j] = Math.Min(100, targetLoyalty);
				}
				else if (overseer.RaceId == j + 1) // for enemy camps, only decrease same race peasants
				{
					int targetLoyalty = RacesTargetLoyalty[j] - thisInfluence;
					RacesTargetLoyalty[j] = Math.Max(0, targetLoyalty);
				}
			}
		}

		//------- apply quality of life -------//

		// -17 to +17 or 0
		int qolContribution = ConfigAdv.town_loyalty_qol ? (_qualityOfLife - 50) / 3 : 0;
		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (RacesPopulation[i] == 0)
				continue;

			int targetLoyalty = RacesTargetLoyalty[i];

			// Quality of life only applies to the part above 30 loyalty
			if (targetLoyalty > 30)
			{
				targetLoyalty = Math.Max(30, targetLoyalty + qolContribution);
				RacesTargetLoyalty[i] = Math.Min(100, targetLoyalty);
			}
		}

		//------- update link status to linked enemy camps -------//

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			Firm firm = FirmArray[LinkedFirms[i]];

			if (firm.FirmType != Firm.FIRM_CAMP)
				continue;

			//------------------------------------------//
			// If this town is linked to a own camp,
			// disable all links to enemy camps, otherwise
			// enable all links to enemy camps.
			//------------------------------------------//

			if (firm.NationId != NationId)
				ToggleFirmLink(i + 1, !HasLinkedOwnCamp, InternalConstants.COMMAND_AUTO);
		}
	}

	private void UpdateResistance()
	{
		//------------- update resistance ----------------//

		bool[] zeroResistance = new bool[GameConstants.MAX_NATION];
		for (int i = 0; i < zeroResistance.Length; i++)
			zeroResistance[i] = true;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (RacesPopulation[i] == 0)
			{
				for (int j = 0; j < GameConstants.MAX_NATION; j++) // reset resistance for non-existing races
					RacesResistance[i, j] = 0.0;

				continue;
			}

			for (int j = 0; j < GameConstants.MAX_NATION; j++)
			{
				if (NationArray.IsDeleted(j + 1))
					continue;

				if (RacesTargetResistance[i, j] >= 0)
				{
					double targetResistance = RacesTargetResistance[i, j];

					if (RacesResistance[i, j] > targetResistance) // only decrease, no increase for resistance
					{
						double decValue = (RacesResistance[i, j] - targetResistance) / 30.0;

						RacesResistance[i, j] -= Math.Max(1.0, decValue);

						// avoid resistance oscillate between targetResistance-1 and targetResistance+1
						if (RacesResistance[i, j] < targetResistance)
							RacesResistance[i, j] = targetResistance;
					}
				}

				// also values between 0 and 1 consider as zero as they are displayed as 0 in the interface
				if (RacesResistance[i, j] >= 1.0)
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
				Surrender(j + 1);
				break;
			}
		}
	}

	public void UpdateTargetResistance()
	{
		if (Population == 0 || LinkedFirms.Count == 0)
			return;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			for (int j = 0; j < GameConstants.MAX_NATION; j++)
			{
				RacesTargetResistance[i, j] = -1; // -1 means influence is not present
			}
		}

		//---- if this town is controlled by rebels, no decrease in resistance ----//

		if (RebelId != 0)
			return;

		//----- count the command base that has influence on this town -----//

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			if (LinkedFirmsEnable[i] != InternalConstants.LINK_EE)
				continue;

			Firm firm = FirmArray[LinkedFirms[i]];

			if (firm.FirmType != Firm.FIRM_CAMP || firm.OverseerId == 0)
				continue;

			//-------- get nation and commander info ------------//

			Unit overseer = UnitArray[firm.OverseerId];

			int curValue = RacesTargetResistance[overseer.RaceId - 1, overseer.NationId - 1];
			int newValue = 100 - overseer.LeaderInfluence();

			// need to do this comparison as there may be more than one command bases of the same nation linked to this town, we use the one with the most influence.
			if (curValue == -1 || newValue < curValue)
				RacesTargetResistance[overseer.RaceId - 1, overseer.NationId - 1] = newValue;
		}
	}

	private void SetAutoCollectTaxLoyalty(int loyaltyLevel)
	{
		_autoCollectTaxLoyalty = loyaltyLevel;

		if (loyaltyLevel != 0 && _autoGrantLoyalty >= _autoCollectTaxLoyalty)
		{
			_autoGrantLoyalty = _autoCollectTaxLoyalty - 10;
		}
	}

	private void SetAutoGrantLoyalty(int loyaltyLevel)
	{
		_autoGrantLoyalty = loyaltyLevel;

		if (loyaltyLevel != 0 && _autoGrantLoyalty >= _autoCollectTaxLoyalty)
		{
			_autoCollectTaxLoyalty = _autoGrantLoyalty + 10;

			if (_autoCollectTaxLoyalty > 100)
				_autoCollectTaxLoyalty = 0; // disable auto collect tax if it's over 100
		}
	}

	private void ProcessAuto()
	{
		if (!HasLinkedOwnCamp) // can only collect or grant when there is a linked camp of its own.
			return;

		Nation nation = NationArray[NationId];

		//----- auto collect tax -----//

		if (_autoCollectTaxLoyalty > 0)
		{
			if (AccumulatedCollectTaxPenalty == 0 && AverageLoyalty() >= _autoCollectTaxLoyalty)
			{
				CollectTax(InternalConstants.COMMAND_AI);
			}
		}

		//---------- auto grant -----------//

		if (_autoGrantLoyalty > 0)
		{
			if (AccumulatedRewardPenalty == 0 && AverageLoyalty() < _autoGrantLoyalty && nation.cash > 0.0)
			{
				Reward(InternalConstants.COMMAND_AI);
			}
		}
	}

	private void CollectYearlyTax()
	{
		NationArray[NationId].add_income(NationBase.INCOME_TAX, Population * GameConstants.TAX_PER_PERSON);
	}

	public void CollectTax(int remoteAction)
	{
		if (!HasLinkedOwnCamp)
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

		int loyaltyDecrease = GameConstants.COLLECT_TAX_LOYALTY_DECREASE + AccumulatedCollectTaxPenalty / 5;

		loyaltyDecrease = Math.Min(loyaltyDecrease, GameConstants.COLLECT_TAX_LOYALTY_DECREASE + 10);

		AccumulatedCollectTaxPenalty += 10;

		//------ decrease the loyalty of the town people ------//

		double taxCollected = 0.0;
		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			//TODO check
			double beforeLoyalty = RacesLoyalty[i];
			ChangeLoyalty(i + 1, -loyaltyDecrease);
			taxCollected += (beforeLoyalty - RacesLoyalty[i]) * RacesPopulation[i] * GameConstants.TAX_PER_PERSON / loyaltyDecrease;
		}

		NationArray[NationId].add_income(NationBase.INCOME_TAX, taxCollected);

		ThinkRebel();
	}

	public void Reward(int remoteAction)
	{
		if (!HasLinkedOwnCamp)
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

		int loyaltyIncrease = GameConstants.TOWN_REWARD_LOYALTY_INCREASE - AccumulatedRewardPenalty / 5;

		loyaltyIncrease = Math.Max(3, loyaltyIncrease);

		AccumulatedRewardPenalty += 10;

		//------ increase the loyalty of the town people ------//

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
			ChangeLoyalty(i + 1, loyaltyIncrease);

		NationArray[NationId].add_expense(NationBase.EXPENSE_GRANT_OWN_TOWN, Population * GameConstants.TOWN_REWARD_PER_PERSON);
	}

	public bool CanGrantToNonOwnTown(int grantNationId)
	{
		if (NationId == grantNationId)
			return false;

		if (NationId == 0)
		{
			return HasLinkedCamp(grantNationId, true);
		}
		else // for nation town, when the enemy doesn't have camps linked to it and the granting nation has camps linked to it
		{
			return !HasLinkedCamp(NationId, false) && HasLinkedCamp(grantNationId, true);
		}
	}

	public int GrantToNonOwnTown(int grantNationId, int remoteAction)
	{
		if (!CanGrantToNonOwnTown(grantNationId))
			return 0;

		Nation grantNation = NationArray[grantNationId];

		if (grantNation.cash < 0.0)
			return 0;

		//if( !remoteAction && remote.is_enable() )
		//{
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_TOWN_GRANT_INDEPENDENT, 2*sizeof(short) );
		//shortPtr[0] = town_recno;
		//shortPtr[1] = grantNationRecno;
		//return 1;
		//}

		//---- calculate the resistance to be decreased -----//

		int resistanceDec = GameConstants.IND_TOWN_GRANT_RESISTANCE_DECREASE - AccumulatedEnemyGrantPenalty / 5;

		resistanceDec = Math.Max(3, resistanceDec);

		AccumulatedEnemyGrantPenalty += 10;

		//------ decrease the resistance of the independent villagers ------//

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (RacesPopulation[i] == 0)
				continue;

			if (NationId == 0)
			{
				RacesResistance[i, grantNationId - 1] -= resistanceDec;

				if (RacesResistance[i, grantNationId - 1] < 0.0)
					RacesResistance[i, grantNationId - 1] = 0.0;
			}
			else
			{
				RacesLoyalty[i] -= resistanceDec;

				if (RacesLoyalty[i] < 0.0)
					RacesLoyalty[i] = 0.0;
			}
		}

		//----------- decrease cash ------------//

		grantNation.add_expense(NationBase.EXPENSE_GRANT_OTHER_TOWN, Population * GameConstants.IND_TOWN_GRANT_PER_PERSON);

		return 1;
	}

	
	public void DistributeDemand()
	{
		//------ scan for a firm to input raw materials --------//

		MarketGoodsInfo[] marketGoodsInfoArray = new MarketGoodsInfo[GameConstants.MAX_PRODUCT];
		for (int i = 0; i < marketGoodsInfoArray.Length; i++)
			marketGoodsInfoArray[i] = new MarketGoodsInfo();

		//------- count the no. of market place that are near to this town ----//

		for (int linkedFirmId = 0; linkedFirmId < LinkedFirms.Count; linkedFirmId++)
		{
			Firm firm = FirmArray[LinkedFirms[linkedFirmId]];

			if (firm.FirmType != Firm.FIRM_MARKET)
				continue;

			if (LinkedFirmsEnable[linkedFirmId] != InternalConstants.LINK_EE)
				continue;

			FirmMarket market = (FirmMarket)firm;

			//---------- process market -------------//

			for (int i = 0; i < GameConstants.MAX_PRODUCT; i++)
			{
				MarketGoods marketGoods = market.market_product_array[i];
				MarketGoodsInfo marketGoodsInfo = marketGoodsInfoArray[i];

				double thisSupply = marketGoods.StockQty;
				marketGoodsInfo.Markets.Add(market);
				marketGoodsInfo.TotalSupply += thisSupply;

				// vars for later use, so that towns will always try to buy goods from their own markets first.
				if (firm.NationId == NationId)
					marketGoodsInfo.TotalOwnSupply += thisSupply;
			}
		}

		//-- set the monthly demand of the town on each product --//

		double townDemand = JoblessPopulation * GameConstants.PEASANT_GOODS_MONTH_DEMAND + WorkerPopulation * GameConstants.WORKER_GOODS_MONTH_DEMAND;

		//---------- sell goods now -----------//

		for (int i = 0; i < GameConstants.MAX_PRODUCT; i++)
		{
			MarketGoodsInfo marketGoodsInfo = marketGoodsInfoArray[i];

			foreach (FirmMarket market in marketGoodsInfo.Markets)
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

				MarketGoods marketGoods = market.market_product_array[i];

				if (marketGoods != null)
				{
					//---- if the demand is larger than the supply -----//

					if (marketGoodsInfo.TotalSupply <= townDemand)
					{
						// evenly distribute the excessive demand on all markets
						marketGoods.MonthDemand += marketGoods.StockQty + (townDemand - marketGoodsInfo.TotalSupply) / marketGoodsInfo.Markets.Count;
					}
					else //---- if the supply is larger than the demand -----//
					{
						//--- towns always try to buy goods from their own markets first ---//

						double ownShareDemand = Math.Min(townDemand, marketGoodsInfo.TotalOwnSupply);

						if (market.NationId == NationId)
						{
							// if total_own_supply is 0 then ownShareDemand is also 0 and we put no demand on the product
							if (marketGoodsInfo.TotalOwnSupply > 0.0)
								marketGoods.MonthDemand += ownShareDemand * marketGoods.StockQty / marketGoodsInfo.TotalOwnSupply;
						}
						else
						{
							// Note: total_supply > 0.0, because else the first case above (demand larger than supply) will be triggered
							marketGoods.MonthDemand += (townDemand - ownShareDemand) * marketGoods.StockQty / marketGoodsInfo.TotalSupply;
						}
					}
				}
			}
		}
	}

	public void UpdateProductSupply()
	{
		for (int i = 0; i < HasProductSupply.Length; i++)
			HasProductSupply[i] = false;

		//----- scan for linked market place -----//

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			Firm firm = FirmArray[LinkedFirms[i]];

			if (firm.NationId != NationId || firm.FirmType != Firm.FIRM_MARKET)
				continue;

			FirmMarket firmMarket = (FirmMarket)firm;
			
			//---- check what type of products they are selling ----//

			for (int j = 0; j < GameConstants.MAX_MARKET_GOODS; j++)
			{
				int productId = firmMarket.market_goods_array[j].ProductId;
				if (productId > 1)
					HasProductSupply[productId - 1] = true;
			}
		}
	}

	private void UpdateQualityOfLife()
	{
		//--- calculate the estimated total purchase from this town ----//

		double townDemand = JoblessPopulation * GameConstants.PEASANT_GOODS_MONTH_DEMAND + WorkerPopulation * GameConstants.WORKER_GOODS_MONTH_DEMAND;

		double totalPurchase = 0.0;

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			if (LinkedFirmsEnable[i] != InternalConstants.LINK_EE)
				continue;

			Firm firm = FirmArray[LinkedFirms[i]];

			if (firm.FirmType != Firm.FIRM_MARKET)
				continue;

			FirmMarket firmMarket = (FirmMarket)firm;

			//-------------------------------------//

			for (int j = 0; j < GameConstants.MAX_MARKET_GOODS; j++)
			{
				MarketGoods marketGoods = firmMarket.market_goods_array[j];
				if (marketGoods.ProductId == 0 || marketGoods.MonthDemand == 0)
					continue;

				double monthSaleQty = marketGoods.SaleQty30Days();

				if (monthSaleQty > marketGoods.MonthDemand)
				{
					totalPurchase += townDemand;
				}
				else if (marketGoods.MonthDemand > townDemand)
				{
					totalPurchase += monthSaleQty * townDemand / marketGoods.MonthDemand;
				}
				else
				{
					totalPurchase += monthSaleQty;
				}
			}
		}

		//------ return the quality of life ------//

		_qualityOfLife = (int)(100.0 * totalPurchase / (townDemand * GameConstants.MAX_PRODUCT));
	}

	private void ProcessFood()
	{
		//--------- Peasants produce food ---------//

		NationArray[NationId].add_food(Convert.ToDouble(JoblessPopulation * GameConstants.PEASANT_FOOD_YEAR_PRODUCTION) / 365.0);

		//---------- People consume food ----------//

		NationArray[NationId].consume_food(Convert.ToDouble(Population * GameConstants.PERSON_FOOD_YEAR_CONSUMPTION) / 365.0);
	}


	public bool CanTrain(int raceId)
	{
		return HasLinkedOwnCamp && RecruitableRacePopulation(raceId, true) > 0 && NationArray[NationId].cash > GameConstants.TRAIN_SKILL_COST;
	}

	private void ProcessTrain()
	{
		Unit unit = UnitArray[TrainUnitId];
		int raceId = unit.RaceId;

		//---- if the unit being trained was killed -----//

		bool cancelFlag = RacesJoblessPopulation[raceId - 1] == 0;

		//-----------------------------------------------------------------//
		//
		// If after start training the unit (non-spy unit), a unit has been
		// mobilized, resulting that the spy count >= jobless_race,
		// we must cancel the training, otherwise when training finishes,
		// and dec_pop is called, spy count will > jobless count and cause error.
		//
		//-----------------------------------------------------------------//

		if (RacesSpyCount[raceId - 1] == RacesJoblessPopulation[raceId - 1])
			cancelFlag = true;

		if (cancelFlag)
		{
			UnitArray.DisappearInTown(unit, this);
			TrainUnitId = 0;
			return;
		}

		//------------- process training ---------------//

		long totalTrainDays;

		if (Config.fast_build && NationId == NationArray.player_recno)
			totalTrainDays = GameConstants.TOTAL_TRAIN_DAYS / 2;
		else
			totalTrainDays = GameConstants.TOTAL_TRAIN_DAYS;

		if ((Sys.Instance.FrameNumber - _startTrainFrameNumber) / InternalConstants.FRAMES_PER_DAY >= totalTrainDays)
		{
			FinishTrain(unit);
		}
	}

	private void FinishTrain(Unit unit)
	{
		SpriteInfo spriteInfo = unit.SpriteInfo;
		int locX = LocX1; // xLoc & yLoc are used for returning results
		int locY = LocY1;

		if (!World.LocateSpace(ref locX, ref locY, LocX2, LocY2, spriteInfo.LocWidth, spriteInfo.LocHeight))
			return;

		unit.InitSprite(locX, locY);

		if (unit.IsOwn())
			SERes.far_sound(locX, locY, 1, 'S', unit.SpriteResId, "RDY");

		unit.UnitMode = 0; // reset it to 0 from UNIT_MODE_UNDER_TRAINING
		TrainUnitId = 0;

		DecPopulation(unit.RaceId, false); // decrease the population now as the recruit() does do so

		//---- if this trained unit is tied to an AI action ----//

		if (TrainUnitActionId != 0)
		{
			NationArray[NationId].process_action_id(TrainUnitActionId);
			TrainUnitActionId = 0;
		}
	}

	private void CancelTrain()
	{
		if (TrainUnitId != 0)
		{
			Unit unit = UnitArray[TrainUnitId];
			// check whether the unit is already a spy before training
			if (unit.SpyId != 0 && unit.Skill.SkillId != 0)
			{
				//TODO check
				SpyArray[unit.SpyId].SetPlace(Spy.SPY_TOWN, TownId);
				unit.SpyId = 0; // reset it so Unit::deinit() won't delete the spy
			}

			UnitArray.DisappearInTown(unit, this);
			TrainUnitId = 0;
		}
	}

	public void AddQueue(int skillId, int raceId, int amount = 1)
	{
		if (amount <= 0)
			return;

		int queueSpace = InternalConstants.TOWN_MAX_TRAIN_QUEUE - TrainSkillQueue.Count - (TrainUnitId > 0 ? 1 : 0);
		int enqueueAmount = Math.Min(queueSpace, amount);

		for (int i = 0; i < enqueueAmount; i++)
		{
			TrainSkillQueue.Add(skillId);
			_trainRaceQueue.Add(raceId);
		}

		if (TrainUnitId == 0)
			ProcessQueue();
	}

	public void RemoveQueue(int skillId, int amount = 1)
	{
		if (amount <= 0)
			return;

		for (int i = TrainSkillQueue.Count - 1; i >= 0; i--)
		{
			if (TrainSkillQueue[i] == skillId)
			{
				TrainSkillQueue.RemoveAt(i);
				_trainRaceQueue.RemoveAt(i);
				amount--;
				if (amount == 0)
					return;
			}
		}

		// If there were less trained of skillId in the queue than were requested to be removed
		// then also cancel currently trained unit
		if (amount > 0 && TrainUnitId != 0)
		{
			Unit unit = UnitArray[TrainUnitId];
			if (unit.Skill.SkillId == skillId)
				CancelTrain();
		}
	}

	private void ProcessQueue()
	{
		if (TrainSkillQueue.Count == 0)
			return;

		if (JoblessPopulation == 0)
			return;

		for (int i = 0; i < TrainSkillQueue.Count; i++)
		{
			if (CanTrain(_trainRaceQueue[i]))
			{
				int skillId = TrainSkillQueue[i];
				int raceId = _trainRaceQueue[i];
				TrainSkillQueue.RemoveAt(i);
				_trainRaceQueue.RemoveAt(i);
				Recruit(skillId, raceId, InternalConstants.COMMAND_AUTO);
				break;
			}
		}
	}

	
	private bool CanMigrate(int destTownId, bool migrateNow = false, int raceId = 0)
	{
		if (raceId == 0)
		{
			//TODO get race from user interface
		}

		Town destTown = TownArray[destTownId];

		if (destTown.Population >= GameConstants.MAX_TOWN_POPULATION)
			return false;

		//---- if there are still jobless units ----//

		if (RecruitableRacePopulation(raceId, true) > 0) 
		{
			if (migrateNow)
				MovePopulation(destTown, raceId, false); 

			return true;
		}

		//--- if there are no jobless units left -----//

		if (RacesPopulation[raceId - 1] > 0)
		{
			//---- scan for firms that are linked to this town ----//

			for (int i = 0; i < LinkedFirms.Count; i++)
			{
				Firm firm = FirmArray[LinkedFirms[i]];

				//---- only for firms whose workers live in towns ----//

				if (!FirmRes[firm.FirmType].live_in_town)
					continue;

				//---- if the target town is within the effective range of this firm ----//

				if (!Misc.AreTownAndFirmLinked(this, firm))
					continue;

				//------- scan for workers -----------//

				foreach (Worker worker in firm.Workers)
				{
					//--- if the worker lives in this town ----//

					if (worker.race_id == raceId && worker.town_recno == TownId)
					{
						if (migrateNow)
						{
							worker.town_recno = destTownId;
							MovePopulation(destTown, raceId, true);
						}

						return true;
					}
				}
			}
		}

		return false;
	}

	private void ThinkMigrate()
	{
		// TODO check auto migration between two our villages of the same race
		
		if (JoblessPopulation == 0)
			return;

		foreach (Town town in TownArray)
		{
			if (town.NationId == 0)
				continue;

			if (town.TownId == TownId)
				continue;

			if (town.Population >= GameConstants.MAX_TOWN_POPULATION)
				continue;

			int townDistance = Misc.RectsDistance(LocX1, LocY1, LocX2, LocY2,
				town.LocX1, town.LocY1, town.LocX2, town.LocY2);

			if (townDistance > InternalConstants.EFFECTIVE_TOWN_TOWN_DISTANCE)
				continue;

			//---- scan all jobless population, see if any of them want to migrate ----//

			int raceId = ConfigAdv.GetRandomRace();

			for (int j = 0; j < GameConstants.MAX_RACE; j++)
			{
				if (++raceId > GameConstants.MAX_RACE)
					raceId = 1;

				// only if there are peasants who are jobless and are not spies
				// TODO why not migrate spies? If spies do not migrate they can be revealed
				if (RecruitableRacePopulation(raceId, false) == 0)
					continue;

				//--- migrate a number of people of the same race at the same time ---//

				int migratedCount = 0;

				while (ThinkMigrateOne(town, raceId))
				{
					migratedCount++;

					// allow a random and low max number to migrate when this happens
					if (migratedCount >= GameConstants.MAX_MIGRATE_PER_DAY || Misc.Random(4) == 0)
						break;
				}

				//------------- add news --------------//

				if (migratedCount > 0)
				{
					if (NationId == NationArray.player_recno || town.NationId == NationArray.player_recno)
					{
						NewsArray.migrate(TownId, town.TownId, raceId, migratedCount);
					}

					return;
				}
			}
		}
	}

	private bool ThinkMigrateOne(Town targetTown, int raceId)
	{
		//-- only if there are peasants who are jobless and are not spies --//
		// TODO why not migrate spies? If spies do not migrate they can be revealed
		if (RecruitableRacePopulation(raceId, false) == 0)
			return false;

		//---- if the target town's population has already reached its MAX ----//

		if (targetTown.Population >= GameConstants.MAX_TOWN_POPULATION)
			return false;

		//-- do not migrate if the target town's population of that race is less than half of the population of the current town --//

		if (targetTown.RacesPopulation[raceId - 1] < RacesPopulation[raceId - 1] / 2)
			return false;

		//-- do not migrate if the target town might not be a place this peasant will stay --//
		// TODO maybe change 40 to 50

		if (targetTown.RacesLoyalty[raceId - 1] < 40)
			return false;

		//--- calculate the attractiveness rating of the current town ---//

		int curAttractLevel = RaceHarmony(raceId);

		//------- loyalty/resistance affecting the attractiveness ------//

		if (NationId != 0)
		{
			// loyalty > 40 is considered as positive force, < 40 is considered as negative force
			curAttractLevel += (int)(NationArray[NationId].reputation + RacesLoyalty[raceId - 1] - 40.0);
		}
		else
		{
			//TODO targetTown.NationId is always nonzero?
			if (targetTown.NationId != 0)
				curAttractLevel += (int)RacesResistance[raceId - 1, targetTown.NationId - 1];
		}

		//--- calculate the attractiveness rating of the target town ---//

		int targetAttractLevel = targetTown.RaceHarmony(raceId);

		if (targetTown.NationId != 0)
			targetAttractLevel += (int)NationArray[targetTown.NationId].reputation;

		if (targetAttractLevel < GameConstants.MIN_MIGRATE_ATTRACT_LEVEL)
			return false;

		//--------- compare the attractiveness ratings ---------//

		if (targetAttractLevel - curAttractLevel > GameConstants.MIN_MIGRATE_ATTRACT_LEVEL / 2)
		{
			//---------- migrate now ----------//

			int newLoyalty = Math.Max(targetAttractLevel / 2, 40);

			Migrate(raceId, targetTown.TownId, newLoyalty);
			return true;
		}

		return false;
	}

	private void Migrate(int raceId, int destTownId, int newLoyalty)
	{
		Town destTown = TownArray[destTownId];

		if (destTown.Population >= GameConstants.MAX_TOWN_POPULATION)
			return;

		DecPopulation(raceId, false);
		destTown.IncPopulation(raceId, false, newLoyalty);
	}

	private bool MigrateTo(int destTownRecno, int remoteAction, int raceId = 0, int count = 1)
	{
		if (count <= 0 || count > GameConstants.MAX_TOWN_POPULATION)
		{
			return false;
		}

		if (raceId == 0)
		{
			//TODO get race from user interface
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
			continueMigrate = CanMigrate(destTownRecno, true, raceId);	
			if (continueMigrate)
				++migrated;
		}

		return migrated > 0;
	}

	
	public void AssignUnit(Unit unit)
	{
		if (Population >= GameConstants.MAX_TOWN_POPULATION || unit.Rank == Unit.RANK_KING)
		{
			unit.Stop2();
			//----------------------------------------------------------------------//
			// codes for handle_blocked_move set unit_group_id to a different value 
			// s.t. the members in this group will not be blocked by this unit.
			//----------------------------------------------------------------------//
			unit.GroupId = UnitArray.CurGroupId++;
			return;
		}

		//------ if the unit is a general, demote it first -------//

		if (unit.Rank == Unit.RANK_GENERAL)
			unit.SetRank(Unit.RANK_SOLDIER);

		IncPopulation(unit.RaceId, false, unit.Loyalty);

		//---- free the unit's name from the name database ----//

		RaceRes[unit.RaceId].free_name_id(unit.NameId);

		//----- if it's a town defending unit -----//

		if (unit.UnitMode == UnitConstants.UNIT_MODE_DEFEND_TOWN)
		{
			//---------------------------------------------//
			//
			// If this unit is a defender of the town, add back the loyalty
			// which was deducted from the defender left the town.
			//
			//---------------------------------------------//

			if (unit.NationId == NationId && unit.UnitModeParam == TownId)
			{
				// if the unit is a town defender, skill.skill_level is temporarily used for storing the loyalty
				// that will be added back to the town if the defender returns to the town

				int loyaltyInc = unit.Skill.SkillLevel;

				if (NationId != 0) // set the loyalty later for NationId != 0
				{
					ChangeLoyalty(unit.RaceId, loyaltyInc);
				}
				else
				{
					for (int i = 0; i < GameConstants.MAX_NATION; i++) // set the resistance
					{
						double newResistance = RacesResistance[unit.RaceId - 1, i] + loyaltyInc + GameConstants.RESISTANCE_INCREASE_DEFENDER;
						RacesResistance[unit.RaceId - 1, i] = Math.Min(newResistance, 100);
					}
				}
			}
		}

		//------ if the unit is a spy -------//

		if (unit.SpyId > 0)
		{
			SpyArray[unit.SpyId].SetPlace(Spy.SPY_TOWN, TownId);
			unit.SpyId = 0; // reset it so Unit::deinit() won't delete the spy
		}

		//----- if this is an independent town -----//

		if (NationId == 0) // update the town people's combat level with this unit's combat level
		{
			TownCombatLevel = (TownCombatLevel * (Population - 1) + unit.Skill.CombatLevel) / Population;
		}

		//--------- delete the unit --------//

		// the unit disappear from the map because it has moved into a town
		UnitArray.DisappearInTown(unit, this);
	}

	public int RecruitableRacePopulation(int raceId, bool recruitSpy)
	{
		int recruitableCount = RacesJoblessPopulation[raceId - 1];

		if (TrainUnitId != 0 && UnitArray[TrainUnitId].RaceId == raceId)
			recruitableCount--;

		if (!recruitSpy)
		{
			recruitableCount -= RacesSpyCount[raceId - 1];

			if (recruitableCount == -1) // it may have been reduced twice if the unit being trained is a spy 
				recruitableCount = 0;
		}

		return recruitableCount;
	}
	
	public bool CanRecruit(int raceId)
	{
		//----------------------------------------------------//
		// Cannot recruit when you have none of your own camps
		// linked to this town, but your enemies have camps linked to it.
		//----------------------------------------------------//

		if (!HasLinkedOwnCamp && HasLinkedEnemyCamp)
			return false;

		if (RecruitableRacePopulation(raceId, true) == 0)
			return false;

		int minRecruitLoyalty = GameConstants.MIN_RECRUIT_LOYALTY;

		//--- for the AI, only recruit if the loyalty still stay at 30 after recruiting the unit ---//

		if (AITown && NationId != 0)
			minRecruitLoyalty += 3 + RecruitDecLoyalty(raceId, false);

		return RacesLoyalty[raceId - 1] >= minRecruitLoyalty;
	}

	public int Recruit(int trainSkillId, int raceId, byte remoteAction)
	{
		if (trainSkillId >= 1 && !CanTrain(raceId))
			return 0;
		
		//---- we can't train a new one when there is one currently under training ---//

		if (trainSkillId >= 1 && TrainUnitId != 0)
			return 0;

		//--------------------------------------------//

		if (raceId == 0)
		{
			//TODO get race from user interface
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

		int recruitableCount = RecruitableRacePopulation(raceId, true);

		//--- if there are spies in this town, chances are that they will be mobilized ---//

		bool shouldTrainSpy = (RacesSpyCount[raceId - 1] >= Misc.Random(recruitableCount) + 1);

		//---- if we are trying to train an enemy to our spy, then... -----//

		if (shouldTrainSpy && trainSkillId == Skill.SKILL_SPYING)
		{
			//-- if there are other non-spy units in the town, then train the other and don't train the spy --//

			if (recruitableCount > RacesSpyCount[raceId - 1])
			{
				shouldTrainSpy = false;
			}

			//--- if all remaining units are spies, when you try to train one, all of them will become mobilized ---//
			else
			{
				int spyId = SpyArray.FindTownSpy(TownId, raceId, 1);

				Spy spy = SpyArray[spyId];

				if (spy.MobilizeTownSpy() == null)
					return 0;

				spy.ChangeCloakedNation(spy.TrueNationId);

				return 0;
			}
		}

		Unit unit = null;

		if (shouldTrainSpy)
		{
			//-----------------------------------------------------//
			// Spies from other nations will first be mobilized,
			// when all peasants and spies are mobilized and
			// the only ones left in the town are spies from our
			// nation, then mobilize them finally.
			//-----------------------------------------------------//

			for (int mobileNationType = 1; unit == null && mobileNationType <= 2; mobileNationType++)
			{
				if (mobileNationType == 2) // only mobilize our own spies, there are the only ones in the town
				{
					if (RecruitableRacePopulation(raceId, true) > RacesSpyCount[raceId - 1])
						break;
				}

				foreach (Spy spy in SpyArray.EnumerateRandom())
				{
					if (spy.SpyPlace == Spy.SPY_TOWN && spy.SpyPlaceId == TownId && spy.RaceId == raceId)
					{
						// only mobilize spies from other nations, don't mobilize spies of our own nation
						// TODO never train our spy into another skill
						if (mobileNationType == 1)
						{
							if (spy.TrueNationId == NationId)
								continue;
						}

						// the parameter is whether decreasing the population immediately, decrease immediately in recruit mode, not in training mode
						unit = spy.MobilizeTownSpy(trainSkillId == -1);
						break;
					}
				}
			}
		}

		//-------- mobilize normal peasant units -------//

		if (unit == null)
		{
			// the 2nd parameter is whether decreasing the population immediately, decrease immediately in recruit mode, not in training mode
			unit = MobilizeTownPeople(raceId, trainSkillId == -1, false);
		}

		if (unit == null)
			return 0;

		//-------- training skill -----------//

		if (trainSkillId > 0)
		{
			if (trainSkillId == Skill.SKILL_SPYING)
			{
				unit.SpyId = SpyArray.AddSpy(unit.SpriteId, GameConstants.TRAIN_SKILL_LEVEL).SpyId;
			}
			else
			{
				unit.Skill.SkillId = trainSkillId;
				unit.Skill.SkillLevel = GameConstants.TRAIN_SKILL_LEVEL;
			}
			
			//---- training solider or skilled unit takes time ----//

			TrainUnitId = unit.SpriteId;
			_startTrainFrameNumber = Sys.Instance.FrameNumber; // as an offset for displaying the progress bar correctly

			unit.DeinitSprite();
			unit.UnitMode = UnitConstants.UNIT_MODE_UNDER_TRAINING;
			unit.UnitModeParam = TownId;
			
			NationArray[NationId].add_expense(NationBase.EXPENSE_TRAIN_UNIT, GameConstants.TRAIN_SKILL_COST);
		}
		else
		{
			//------ recruitment without training decreases loyalty --------//

			RecruitDecLoyalty(raceId);

			if (unit.IsOwn())
			{
				SERes.far_sound(unit.CurLocX, unit.CurLocY, 1, 'S', unit.SpriteResId, "RDY");
			}
		}

		//--- DecPopulation() will delete the current town if population goes down to 0 ---//

		return unit.SpriteId;
	}

	public int RecruitDecLoyalty(int raceId, bool decNow = true)
	{
		double loyaltyDec = Math.Min(5.0, Convert.ToDouble(GameConstants.MAX_TOWN_POPULATION) / RacesPopulation[raceId - 1]);

		if (ConfigAdv.fix_recruit_dec_loyalty)
		{
			loyaltyDec += AccumulatedRecruitPenalty / 5.0;
			loyaltyDec = Math.Min(loyaltyDec, 10.0);
		}

		//------ recruitment without training decreases loyalty --------//

		if (decNow)
		{
			if (!ConfigAdv.fix_recruit_dec_loyalty)
			{
				loyaltyDec += AccumulatedRecruitPenalty / 5.0;
				loyaltyDec = Math.Min(loyaltyDec, 10.0);
			}

			AccumulatedRecruitPenalty += 5;

			RacesLoyalty[raceId - 1] -= loyaltyDec;

			if (RacesLoyalty[raceId - 1] < 0.0)
				RacesLoyalty[raceId - 1] = 0.0;
		}

		if (ConfigAdv.fix_recruit_dec_loyalty)
		{
			// for ai estimating, round integer conversion up, do not truncate
			return (int)Math.Ceiling(loyaltyDec);
		}

		return (int)loyaltyDec;
	}

	public Unit MobilizeTownPeople(int raceId, bool decPop, bool mobilizeSpy)
	{
		//---- if no jobless people, make workers and overseers jobless ----//

		if (RecruitableRacePopulation(raceId, mobilizeSpy) == 0)
		{
			if (!UnjobTownPeople(raceId, mobilizeSpy, false))
				return null;
		}

		//----look for an empty location for the unit to stand ----//
		//--- scan for the 5 rows right below the building ---//

		int unitId = RaceRes[raceId].basic_unit_id;
		SpriteInfo spriteInfo = SpriteRes[UnitRes[unitId].sprite_id];
		int locX = LocX1, locY = LocY1; // locX & locY are used for returning results

		if (!World.LocateSpace(ref locX, ref locY, LocX2, LocY2, spriteInfo.LocWidth, spriteInfo.LocHeight))
			return null;

		//---------- add the unit now -----------//

		Unit unit = UnitArray.AddUnit(unitId, NationId, Unit.RANK_SOLDIER, (int)RacesLoyalty[raceId - 1], locX, locY);

		//------- set the unit's parameters --------//

		unit.SetCombatLevel(GameConstants.CITIZEN_COMBAT_LEVEL);

		//-------- decrease the town's population ------//

		if (decPop)
			DecPopulation(raceId, false);

		return unit;
	}

	private bool UnjobTownPeople(int raceId, bool unjobSpy, bool unjobOverseer)
	{
		//---- if no jobless people, workers will then get killed -----//

		int racesJoblessPop = RacesJoblessPopulation[raceId - 1];

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			Firm firm = FirmArray[LinkedFirms[i]];

			//------- scan for workers -----------//

			for (int j = firm.Workers.Count - 1; j >= 0; j--)
			{
				Worker worker = firm.Workers[j];
				if (ConfigAdv.fix_town_unjob_worker && !unjobSpy && worker.spy_recno != 0)
					continue;

				//--- if the worker lives in this town ----//

				if (worker.race_id == raceId && worker.town_recno == TownId)
				{
					if (firm.ResignWorker(worker) == 0 && !ConfigAdv.fix_town_unjob_worker)
						return false;

					return RacesJoblessPopulation[raceId - 1] == racesJoblessPop + 1;
				}
			}
		}

		//----- if no worker removed, try to remove overseer ------//

		if (unjobOverseer)
		{
			for (int i = 0; i < LinkedFirms.Count; i++)
			{
				Firm firm = FirmArray[LinkedFirms[i]];

				//------- scan for overseer -----------//

				if (firm.OverseerId != 0)
				{
					//--- if the overseer lives in this town ----//

					Unit overseerUnit = UnitArray[firm.OverseerId];

					if (overseerUnit.RaceId == raceId && firm.OverseerTownId == TownId)
					{
						int overseerId = firm.OverseerId;
						Unit overseer = UnitArray[overseerId];
						firm.AssignOverseer(0);
						if (!UnitArray.IsDeleted(overseerId) && overseer.IsVisible())
							UnitArray.DisappearInTown(overseer, this);

						return true;
					}
				}
			}
		}

		return false;
	}

	public void KillTownPeople(int raceId, int attackerNationId = 0)
	{
		if (raceId == 0)
			raceId = PickRandomRace(true, true); 

		if (raceId == 0)
			return;

		//---- jobless town people get killed first, if all jobless are killed, then kill workers ----//

		if (RecruitableRacePopulation(raceId, true) == 0)
		{
			if (!UnjobTownPeople(raceId, true, true))
				return;
		}

		//------ the killed unit can be a spy -----//

		if (Misc.Random(RecruitableRacePopulation(raceId, true)) < RacesSpyCount[raceId - 1])
		{
			int spyId = SpyArray.FindTownSpy(TownId, raceId, Misc.Random(RacesSpyCount[raceId - 1]) + 1);
			SpyArray.DeleteSpy(SpyArray[spyId]);
		}

		//---- killing civilian people decreases loyalty -----//

		// your people's loyalty decreases because you cannot protect them.
		// but only when your units are killed by enemies, neutral disasters are not counted
		if (NationId != 0 && attackerNationId != 0)
			NationArray[NationId].civilian_killed(raceId, false, 2);

		if (attackerNationId != 0) // the attacker's people's loyalty decreases because of the killing actions.
			NationArray[attackerNationId].civilian_killed(raceId, true, 2); // the nation is the attacking one

		SERes.sound(LocCenterX, LocCenterY, 1, 'R', raceId, "DIE");

		DecPopulation(raceId, false);
	}


	private void SetupLink()
	{
		if (AITown)
			AILinkChecked = false;

		//----- build town-to-firm link relationship -------//

		foreach (Firm firm in FirmArray)
		{
			FirmInfo firmInfo = FirmRes[firm.FirmType];

			if (!firmInfo.is_linkable_to_town)
				continue;

			//---------- check if the firm is close enough to this firm -------//

			if (!Misc.AreTownAndFirmLinked(this, firm))
				continue;

			//------- determine the default link status ------//

			var defaultLinkStatus = firm.NationId == NationId ? InternalConstants.LINK_EE : InternalConstants.LINK_DD;

			//----- a town cannot disable a camp's link to it ----//
			if (firm.FirmType == Firm.FIRM_CAMP) // for capturing the town
				defaultLinkStatus = InternalConstants.LINK_EE;

			//-------- add the link now -------//

			LinkedFirms.Add(firm.FirmId);
			LinkedFirmsEnable.Add(defaultLinkStatus);

			// now link from the firm's side
			
			// these condition are always false
			/*if (defaultLinkStatus == InternalConstants.LINK_ED) // Reverse the link status for the opposite linker
				defaultLinkStatus = InternalConstants.LINK_DE;

			else if (defaultLinkStatus == InternalConstants.LINK_DE)
				defaultLinkStatus = InternalConstants.LINK_ED;*/

			firm.LinkedTowns.Add(TownId);
			firm.LinkedTownsEnable.Add(defaultLinkStatus);

			if (firm.AIFirm)
				firm.AILinkChecked = false;
		}

		//----- build town-to-town link relationship -------//

		foreach (Town town in TownArray)
		{
			if (town.TownId == TownId)
				continue;

			//------ check if the town is close enough to this town -------//

			if (!Misc.AreTownsLinked(this, town))
				continue;

			//------- determine the default link status ------//

			int defaultLinkStatus = InternalConstants.LINK_EE;

			//-------- add the link now -------//

			LinkedTowns.Add(town.TownId);
			LinkedTownsEnable.Add(defaultLinkStatus);

			// now link from the other town's side
			
			// these condition are always false
			/*if (defaultLinkStatus == InternalConstants.LINK_ED) // Reverse the link status for the opposite linker
				defaultLinkStatus = InternalConstants.LINK_DE;

			else if (defaultLinkStatus == InternalConstants.LINK_DE)
				defaultLinkStatus = InternalConstants.LINK_ED;*/

			town.LinkedTowns.Add(TownId);
			town.LinkedTownsEnable.Add(defaultLinkStatus);

			if (town.AITown)
				town.AILinkChecked = false;
		}
	}

	private void ReleaseLink()
	{
		foreach (var linkedFirm in LinkedFirms)
		{
			FirmArray[linkedFirm].ReleaseTownLink(TownId);
		}

		foreach (var linkedTown in LinkedTowns)
		{
			TownArray[linkedTown].ReleaseTownLink(TownId);
		}
		
		LinkedFirms.Clear();
		LinkedFirmsEnable.Clear();
		LinkedTowns.Clear();
		LinkedTownsEnable.Clear();
	}

	public void ReleaseFirmLink(int releaseFirmId)
	{
		if (AITown)
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

	private void ReleaseTownLink(int releaseTownId)
	{
		if (AITown)
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
		if (NationId == 0) // cannot toggle for independent town
			return false;

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			if (LinkedFirms[i] != firmId)
				continue;

			Firm firm = FirmArray[LinkedFirms[i]];

			switch (firm.FirmType)
			{
				//-- you can only toggle a link to a camp if the camp is yours --//

				case Firm.FIRM_CAMP:
					return firm.NationId == NationId;

				//--- town to market link is governed by trade treaty and cannot be toggled ---//

				case Firm.FIRM_MARKET:
					return false;

				default:
					return FirmRes[firm.FirmType].is_linkable_to_town;
			}
		}

		return false;
	}

	private void ToggleFirmLink(int linkId, bool toggleFlag, int remoteAction, bool setBoth = false)
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

		for (int i = linkedFirm.LinkedTowns.Count - 1; i >= 0; i--)
		{
			if (linkedFirm.LinkedTowns[i] == TownId)
			{
				if (toggleFlag)
				{
					if ((sameNation && !setBoth) || setBoth)
						linkedFirm.LinkedTownsEnable[i] = InternalConstants.LINK_EE;
					else
						linkedFirm.LinkedTownsEnable[i] |= InternalConstants.LINK_DE;
				}
				else
				{
					if ((sameNation && !setBoth) || setBoth)
						linkedFirm.LinkedTownsEnable[i] = InternalConstants.LINK_DD;
					else
						linkedFirm.LinkedTownsEnable[i] &= ~InternalConstants.LINK_DE;
				}

				break;
			}
		}

		//-------- update the town's influence --------//

		if (NationId == 0)
			UpdateTargetResistance();

		//--- redistribute demand if a link to market place has been toggled ---//

		if (linkedFirm.FirmType == Firm.FIRM_MARKET)
			TownArray.DistributeDemand();
	}

	private void ToggleTownLink(int linkId, bool toggleFlag, int remoteAction, bool setBoth = false)
	{
		// Function is unused, and not updated to support town networks.
		return;
	}


	public int LinkedActiveCampCount()
	{
		int linkedCount = 0;

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			if (LinkedFirmsEnable[i] == InternalConstants.LINK_EE)
			{
				Firm firm = FirmArray[LinkedFirms[i]];

				if (firm.FirmType == Firm.FIRM_CAMP && firm.OverseerId != 0)
				{
					linkedCount++;
				}
			}
		}

		return linkedCount;
	}

	private double LinkedCampSoldiersCount()
	{
		double townSoldiersCount = 0.0;
		for (int firmIndex = 0; firmIndex < LinkedFirms.Count; firmIndex++)
		{
			Firm firm = FirmArray[LinkedFirms[firmIndex]];

			if (firm.NationId != NationId || firm.FirmType != Firm.FIRM_CAMP)
				continue;

			FirmCamp firmCamp = (FirmCamp)firm;

			int linkedTownsCount = 0;
			for (int townIndex = 0; townIndex < firmCamp.LinkedTowns.Count; townIndex++)
			{
				Town firmTown = TownArray[firmCamp.LinkedTowns[townIndex]];

				if (firmTown.NationId != NationId)
					continue;

				linkedTownsCount++;
			}

			if (linkedTownsCount > 0)
			{
				townSoldiersCount += (double)(firmCamp.Workers.Count + firmCamp.patrol_unit_array.Count + firmCamp.coming_unit_array.Count)
				                     / (double)linkedTownsCount;
			}
		}

		return townSoldiersCount;
	}

	private void UpdateCampLink()
	{
		//--- enable the link of the town's side to all linked camps ---//

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			Firm firm = FirmArray[LinkedFirms[i]];

			if (firm.FirmType != Firm.FIRM_CAMP)
				continue;

			//--- don't set it if the town and camp both belong to a human player, the player will set it himself ---//

			if (firm.NationId == NationId && NationId != 0 && !NationArray[NationId].is_ai())
			{
				continue;
			}

			ToggleFirmLink(i + 1, true, InternalConstants.COMMAND_AUTO);
		}

		//------- update camp link status -------//

		HasLinkedOwnCamp = false;
		HasLinkedEnemyCamp = false;

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			if (LinkedFirmsEnable[i] != InternalConstants.LINK_EE)
				continue;

			Firm firm = FirmArray[LinkedFirms[i]];

			if (firm.FirmType != Firm.FIRM_CAMP || firm.OverseerId == 0)
				continue;

			if (firm.NationId == NationId)
				HasLinkedOwnCamp = true;
			else
				HasLinkedEnemyCamp = true;
		}
	}

	public bool HasLinkedCamp(int nationId, bool needOverseer)
	{
		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			Firm firm = FirmArray[LinkedFirms[i]];

			if (firm.FirmType == Firm.FIRM_CAMP && firm.NationId == nationId)
			{
				if (!needOverseer || firm.OverseerId != 0)
					return true;
			}
		}

		return false;
	}
	
	public Firm ClosestOwnCamp()
	{
		int minDistance = Int32.MaxValue;
		Firm closestCamp = null;

		for (int i = LinkedFirms.Count - 1; i >= 0; i--)
		{
			Firm firm = FirmArray[LinkedFirms[i]];

			if (firm.FirmType != Firm.FIRM_CAMP || firm.NationId != NationId)
				continue;

			int curDistance = Misc.RectsDistance(LocX1, LocY1, LocX2, LocY2,
				firm.LocX1, firm.LocY1, firm.LocX2, firm.LocY2);

			if (curDistance < minDistance)
			{
				minDistance = curDistance;
				closestCamp = firm;
			}
		}

		return closestCamp;
	}

	public bool HasPlayerSpy()
	{
		bool hasAnySpy = false;
		
		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (RacesSpyCount[i] > 0)
			{
				hasAnySpy = true;
				break;
			}
		}

		if (!hasAnySpy) // no spies in this town
			return false;

		//----- look for player spy in the SpyArray -----//

		foreach (Spy spy in SpyArray)
		{
			if (spy.SpyPlace == Spy.SPY_TOWN && spy.SpyPlaceId == TownId && spy.TrueNationId == NationArray.player_recno)
			{
				return true;
			}
		}

		return false;
	}
	
	
	public void ChangeNation(int newNationId)
	{
		if (NationId == newNationId)
			return;

		ClearDefenseMode();

		//------------- stop all actions to attack this town ------------//
		UnitArray.StopAttackTown(TownId);
		RebelArray.StopAttackTown(TownId);

		//--------- update AI town info ---------//

		if (AITown && NationId != 0)
		{
			NationArray[NationId].del_town_info(TownId);
		}

		//--------- reset vars ---------//

		IsBaseTown = false;
		DefendersCount = 0; // reset defender count

		//----- set power region of the new nation ------//

		World.RestorePower(LocX1, LocY1, LocX2, LocY2, TownId, 0); // restore power of the old nation
		World.SetPower(LocX1, LocY1, LocX2, LocY2, newNationId); // set power of the new nation

		SpyArray.ChangeCloakedNation(Spy.SPY_TOWN, TownId, NationId, newNationId);

		NationId = newNationId;

		if (NationId != 0 && RebelId != 0) // reset RebelId if the town is then ruled by a nation
		{
			RebelArray.DeleteRebel(RebelId); // delete the rebel group
			RebelId = 0;
		}

		//--------- update ai_town ----------//

		AITown = false;

		if (NationId == 0) // independent town
		{
			AITown = true;
		}
		else if (NationArray[NationId].nation_type == NationBase.NATION_AI)
		{
			AITown = true;
			NationArray[NationId].add_town_info(TownId);
		}

		//------ set the loyalty of the town people ------//

		int nationRaceId = 0;

		if (NationId != 0)
			nationRaceId = NationArray[NationId].race_id;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (NationId == 0) // independent town
			{
				RacesLoyalty[i] = 80; // this affect the no. of defender if you attack the independent town
			}
			else
			{
				if (nationRaceId == i + 1)
					RacesLoyalty[i] = 40; // loyalty is higher if the ruler and the people are the same race
				else
					RacesLoyalty[i] = 30;
			}
		}

		TownCombatLevel = 0;

		AccumulatedCollectTaxPenalty = 0;
		AccumulatedRewardPenalty = 0;
		AccumulatedEnemyGrantPenalty = 0;
		AccumulatedRecruitPenalty = 0;

		//---- if there is unit being trained currently, change its nation ---//

		if (TrainUnitId != 0)
			UnitArray[TrainUnitId].ChangeNation(newNationId);

		UpdateTargetLoyalty();

		EstablishContactWithPlayer();

		//----- if an AI nation took over this town, see if the AI can capture all firms linked to this town ----//

		if (NationId != 0 && NationArray[NationId].is_ai())
			ThinkCaptureLinkedFirm();

		//------ set national auto policy -----//

		if (NationId != 0)
		{
			Nation nation = NationArray[NationId];

			SetAutoCollectTaxLoyalty(nation.auto_collect_tax_loyalty);
			SetAutoGrantLoyalty(nation.auto_grant_loyalty);
		}

		//---- reset the action mode of all spies in this town ----//

		// we need to reset it. e.g. when we have captured an enemy town, SPY_SOW_DISSENT action must be reset to SPY_IDLE
		SpyArray.SetActionMode(Spy.SPY_TOWN, TownId, Spy.SPY_IDLE);
	}

	private void SetHostileNation(int nationId)
	{
		if (nationId == 0)
			return;

		_independentTownNationRelation |= (0x1 << nationId);
	}

	public void ResetHostileNation(int nationId)
	{
		if (nationId == 0)
			return;

		_independentTownNationRelation &= ~(0x1 << nationId);
	}

	public bool IsHostileNation(int nationId)
	{
		if (nationId == 0)
			return false;

		return (_independentTownNationRelation & (0x1 << nationId)) != 0;
	}

	private bool MobilizeDefender(int attackerNationId)
	{
		// do not call out defenders any more if there is only one person left in the town, otherwise the town will be gone.
		if (Population == 1)
			return false;

		//------- pick a race to mobilize randomly --------//

		int randomPersonId = Misc.Random(Population) + 1;
		int popSum = 0, raceId = 0;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			popSum += RacesPopulation[i];

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

		if (NationId != 0)
		{
			curLoyalty = RacesLoyalty[raceId - 1];
		}
		else
		{
			if (attackerNationId == 0) // if independent units do not attack independent towns
				return false;

			curLoyalty = RacesResistance[raceId - 1, attackerNationId - 1];
		}

		//--- only mobilize new defenders when there aren't too many existing ones ---//

		if (RebelId != 0) // if this town is controlled by rebels
		{
			if (curLoyalty < DefendersCount * 2) // rebel towns have more rebel units coming out to defend
				return false;
		}
		else
		{
			if (curLoyalty < DefendersCount * 5) // if no more defenders are allowed for the current loyalty
				return false;
		}

		//----- check if the loyalty/resistance is high enough -----//

		if (NationId != 0)
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

		if (RecruitableRacePopulation(raceId, false) == 0) // don't recruit spies
			return false;

		//---------- create a defender unit --------------------//

		//--------------------------------------------------------------//
		//									 loyalty of that race
		// decrease loyalty by:         -------------------------------
		//								no. of town people of that race
		//--------------------------------------------------------------//

		double loyaltyDec = curLoyalty / RacesPopulation[raceId - 1]; // decrease in loyalty or resistance

		if (NationId != 0)
		{
			ChangeLoyalty(raceId, -loyaltyDec);
		}
		else
		{
			for (int i = 0; i < GameConstants.MAX_NATION; i++)
				RacesResistance[raceId - 1, i] -= loyaltyDec;
		}

		//------- mobilize jobless people if there are any -------//

		Unit unit = MobilizeTownPeople(raceId, true, false); // don't mobilize spies

		unit.SetMode(UnitConstants.UNIT_MODE_DEFEND_TOWN, TownId);

		// if the unit is a town defender, this var is temporarily used for storing the loyalty that will be added back to the town if the defender returns to the town
		unit.Skill.SkillLevel = (int)loyaltyDec;

		int combatLevel = TownCombatLevel + Misc.Random(20) - 10; // -10 to +10 random difference
		combatLevel = Math.Min(combatLevel, 100);
		combatLevel = Math.Max(combatLevel, 10);

		unit.SetCombatLevel(combatLevel);

		//-----------------------------------------------------//
		// enable unit defend_town mode
		//-----------------------------------------------------//

		unit.Stop2();
		unit.ActionMode2 = UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET;
		unit.ActionPara2 = UnitConstants.UNIT_DEFEND_TOWN_DETECT_COUNT;
		unit.ActionMisc = UnitConstants.ACTION_MISC_DEFEND_TOWN_RECNO;
		unit.ActionMiscParam = TownId;

		DefendersCount++;

		if (RebelId != 0)
			RebelArray[RebelId].MobileRebelCount++; // increase the no. of mobile rebel units

		return true;
	}

	private void ClearDefenseMode()
	{
		//------------------------------------------------------------------//
		// change defense unit's to non-defense mode
		//------------------------------------------------------------------//
		foreach (Unit unit in UnitArray)
		{
			if (unit.InDefendTownMode() && unit.ActionMisc == UnitConstants.ACTION_MISC_DEFEND_TOWN_RECNO && unit.ActionMiscParam == TownId)
				unit.ClearTownDefendMode(); // note: maybe, unit.NationId != NationId
		}
	}

	public void ReduceDefenderCount()
	{
		if (--DefendersCount == 0)
			_independentTownNationRelation = 0;

		if (RebelId != 0)
			RebelArray[RebelId].MobileRebelCount--; // decrease the no. of mobile rebel units
	}

	public void AutoDefense(int targetId)
	{
		for (int i = LinkedFirms.Count - 1; i >= 0; i--)
		{
			Firm firm = FirmArray[LinkedFirms[i]];

			if (firm.NationId != NationId || firm.FirmType != Firm.FIRM_CAMP)
				continue;

			FirmCamp camp = (FirmCamp)firm;
			camp.defense(targetId);

			if (TownArray.IsDeleted(TownId))
				break;
		}
	}

	public void BeingAttacked(int attackerUnitId, double attackDamage)
	{
		if (RebelId != 0)
			RebelArray[RebelId].TownBeingAttacked(attackerUnitId);

		if (Population == 0)
			return;

		DefendTargetId = attackerUnitId; // store the target attacker id

		Unit attackerUnit = UnitArray[attackerUnitId];

		if (attackerUnit.NationId == NationId) // this can happen when the unit has just changed nation 
			return;

		int attackerNationId = attackerUnit.NationId;

		LastBeingAttackedDate = Info.game_date;

		SetHostileNation(attackerNationId);

		//----------- call out defender -----------//

		// only call out defender when the attacking unit is within the effective defending distance

		if (Misc.RectsDistance(attackerUnit.CurLocX, attackerUnit.CurLocY, attackerUnit.CurLocX, attackerUnit.CurLocY,
			    LocX1, LocY1, LocX2, LocY2) <= UnitConstants.EFFECTIVE_DEFEND_TOWN_DISTANCE)
		{
			while (true)
			{
				if (!MobilizeDefender(attackerNationId))
					break;
			}
		}

		AutoDefense(attackerUnitId);

		//----- pick a race to be attacked by the attacker randomly -----//

		int raceId = PickRandomRace(true, true);

		//-------- town people get killed ---------//

		_receivedHitCount += attackDamage;

		if (_receivedHitCount >= GameConstants.RECEIVED_HIT_PER_KILL)
		{
			_receivedHitCount = 0.0;

			KillTownPeople(raceId, attackerNationId);

			if (TownArray.IsDeleted(TownId)) // the town may have been deleted when all pop are killed
				return;
		}

		//---- decrease resistance if this is an independent town ----//

		if (DefendersCount == 0)
		{
			//--- Resistance/loyalty of the town people decrease if the attack continues ---//
			//
			// Resistance/Loyalty decreases faster:
			//
			// -when there are few people in the town
			// -when there is no defender
			//
			//---------------------------------------//

			if (NationId != 0) // if the town belongs to a nation
			{
				//---- decrease loyalty of all races in the town ----//

				for (raceId = 1; raceId <= GameConstants.MAX_RACE; raceId++)
				{
					if (RacesPopulation[raceId - 1] == 0)
						continue;

					double loyaltyDec = 0.0;

					if (HasLinkedOwnCamp) // if it is linked to one of its camp, the loyalty will decrease slower
						loyaltyDec = 5.0 / (double)RacesPopulation[raceId - 1];
					else
						loyaltyDec = 10.0 / (double)RacesPopulation[raceId - 1];

					loyaltyDec = Math.Min(loyaltyDec, 1.0);

					ChangeLoyalty(raceId, -loyaltyDec * attackDamage / (20.0 / InternalConstants.ATTACK_SLOW_DOWN));
				}

				//--- if the resistance of all the races are zero, think about surrendering ---//

				int i;
				for (i = 0; i < GameConstants.MAX_RACE; i++)
				{
					if (RacesLoyalty[i] >= 1.0) // values between 0 and 1 are considered as 0
						break;
				}

				if (i == GameConstants.MAX_RACE) // if resistance of all races drop to zero
					ThinkSurrender();
			}
			else // if the town is an independent town
			{
				if (attackerNationId == 0) // if independent units do not attack independent towns
					return;

				//---- decrease resistance of all races in the town ----//

				for (raceId = 1; raceId <= GameConstants.MAX_RACE; raceId++)
				{
					if (RacesPopulation[raceId - 1] == 0)
						continue;

					// decrease faster for independent towns than towns belonging to nations
					double loyaltyDec = 10.0 / (double)RacesPopulation[raceId - 1];
					loyaltyDec = Math.Min(loyaltyDec, 1.0);

					ChangeResistance(raceId, attackerNationId, -loyaltyDec * attackDamage / (20.0 / InternalConstants.ATTACK_SLOW_DOWN));
				}

				//--- if the resistance of all the races are zero, think about surrendering ---//

				int i;
				for (i = 0; i < GameConstants.MAX_RACE; i++)
				{
					if (RacesResistance[i, attackerNationId - 1] >= 1.0)
						break;
				}

				if (i == GameConstants.MAX_RACE) // if resistance of all races drop to zero
					Surrender(attackerNationId);
			}
		}

		//------ reinforce troops to defend against the attack ------//

		if (DefendersCount == 0 && NationId != 0)
		{
			if (attackerUnit.NationId != NationId) // they may become the same when the town has been captured 
				NationArray[NationId].ai_defend(attackerUnitId);
		}
	}

	private bool ThinkSurrender()
	{
		if (NationId == 0) // if it's an independent town
			return false;

		//--- only surrender when there is no own camps, but has enemy camps linked to this town ---//

		if (HasLinkedOwnCamp || !HasLinkedEnemyCamp)
			return false;

		//--- surrender if 2/3 of the population think about surrender ---//

		int discontentedCount = 0;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (RacesPopulation[i] <= RacesSpyCount[i]) // spies do not rebel together with the rebellion
				continue;

			if (RacesLoyalty[i] <= GameConstants.SURRENDER_LOYALTY)
				discontentedCount += RacesPopulation[i];
		}

		if (discontentedCount < Population * 2 / 3)
			return false;

		//-------- think about surrender to which nation ------//

		int bestRating = AverageLoyalty();
		int bestNationId = 0;

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			Firm firm = FirmArray[LinkedFirms[i]];

			//---- if this is an enemy camp ----//

			if (firm.FirmType == Firm.FIRM_CAMP && firm.NationId != NationId && firm.NationId != 0 && firm.OverseerId != 0)
			{
				Unit overseer = UnitArray[firm.OverseerId];
				int curRating = overseer.LeaderInfluence();

				if (curRating > bestRating)
				{
					bestRating = curRating;
					bestNationId = firm.NationId;
				}
			}
		}

		if (bestNationId != 0)
		{
			Surrender(bestNationId);
			return true;
		}
		
		return false;
	}

	private void Surrender(int toNationId)
	{
		// if this is a rebel town and the mobile rebel count is > 0, don't surrender
		// this function can be called by update_resistance() when resistance drops to zero

		if (RebelId != 0)
		{
			Rebel rebel = RebelArray[RebelId];
			if (rebel.MobileRebelCount > 0)
				return;
		}

		if (NationId == NationArray.player_recno || toNationId == NationArray.player_recno)
		{
			NewsArray.town_surrendered(TownId, toNationId);
			if (toNationId == NationArray.player_recno)
			{
				SECtrl.immediate_sound("GET_TOWN");
			}
		}

		ChangeNation(toNationId);
	}

	private void ThinkRebel()
	{
		if (NationId == 0)
			return;

		if (Info.game_date < _lastRebelDate.AddDays(GameConstants.REBEL_INTERVAL_MONTH * 30))
			return;

		// don't rebel within ten days after being attacked by a hostile unit
		if (DefendersCount > 0 || Info.game_date < LastBeingAttackedDate.AddDays(10))
			return;

		//--- rebel if 2/3 of the population becomes discontented ---//

		int discontentedCount = 0, rebelLeaderRaceId = 0, largestRebelRace = 0, trainRaceId = 0;
		int[] restrictRebelCount = new int[GameConstants.MAX_RACE];

		if (TrainUnitId != 0)
			trainRaceId = UnitArray[TrainUnitId].RaceId;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			restrictRebelCount[i] = RacesSpyCount[i]; // spies do not rebel together with the rebellion

			if (RacesPopulation[i] > 0 && RacesLoyalty[i] <= GameConstants.REBEL_LOYALTY)
			{
				discontentedCount += RacesPopulation[i];

				// count firm spies that reside in this town
				for (int j = 0; j < LinkedFirms.Count; j++)
				{
					Firm firm = FirmArray[LinkedFirms[j]];
					foreach (Worker worker in firm.Workers)
					{
						if (worker.spy_recno != 0 && worker.town_recno == TownId)
							restrictRebelCount[i]++;
					}
				}

				if (trainRaceId == i + 1)
					restrictRebelCount[i]++; // unit under training cannot rebel

				if (RacesPopulation[i] <= restrictRebelCount[i])
					continue; // no one can lead from this group

				if (RacesPopulation[i] > largestRebelRace)
				{
					largestRebelRace = RacesPopulation[i];
					rebelLeaderRaceId = i + 1;
				}
			}
		}

		if (rebelLeaderRaceId == 0) // no discontention or no one can lead
			return;

		if (Population == 1) // if population is 1 only, handle otherwise
		{
		}
		else
		{
			if (discontentedCount < Population * 2 / 3)
				return;
		}

		//----- if there was just one unit in the town and he rebels ----//

		bool oneRebelOnly = false;

		if (Population == 1)
		{
			NewsArray.town_rebel(TownId, 1);
			oneRebelOnly = true;
		}

		//----- create the rebel leader and the rebel group ------//

		int rebelCount = 1;
		Unit rebelLeader = CreateRebelUnit(rebelLeaderRaceId, true); // 1-the unit is the rebel leader

		if (rebelLeader == null)
			return;

		int curGroupId = UnitArray.CurGroupId++;
		rebelLeader.GroupId = curGroupId;

		if (oneRebelOnly) // if there was just one unit in the town and he rebels
		{
			RebelArray.AddRebel(rebelLeader, NationId);
			return;
		}

		//------- create a rebel group -----//

		Rebel rebel = RebelArray.AddRebel(rebelLeader, NationId, Rebel.REBEL_ATTACK_TOWN, TownId);

		//------- create other rebel units in the rebel group -----//

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (RacesPopulation[i] <= restrictRebelCount[i] || RacesLoyalty[i] > GameConstants.REBEL_LOYALTY)
				continue;

			if (Population == 1) // if only one peasant left, break, so not all peasants will rebel 
				break;

			// 30% - 60% of the unit will rebel.
			int raceRebelCount = (RacesPopulation[i] - restrictRebelCount[i]) * (30 + Misc.Random(30)) / 100;

			int j = 0;
			for (; j < raceRebelCount; j++) // no. of rebel units of this race
			{
				Unit rebelUnit = CreateRebelUnit(i + 1, false);

				if (rebelUnit == null)
					break;

				rebelUnit.GroupId = curGroupId;
				rebelUnit.LeaderId = rebelLeader.SpriteId;

				rebel.Join(rebelUnit);

				rebelCount++;
			}

			//--- when disloyal units left, the average loyalty increases ---//

			ChangeLoyalty(i + 1, 50.0 * j / RacesPopulation[i]);
		}

		_lastRebelDate = Info.game_date;

		//--- add the news first as after calling ai_spy_town_rebel, the town may disappear as all peasants are gone ---//

		NewsArray.town_rebel(TownId, rebelCount);

		//--- tell the AI spies in the town that a rebellion is happening ---//

		SpyArray.AISpyTownRebel(TownId);
	}
	
	private Unit CreateRebelUnit(int raceId, bool isLeader)
	{
		//--- do not mobilize spies as rebels ----//

		//---- if no jobless people, make workers and overseers jobless ----//

		if (RecruitableRacePopulation(raceId, false) == 0)
		{
			if (!UnjobTownPeople(raceId, false, false))
				return null;

			if (RecruitableRacePopulation(raceId, false) == 0) // if the unjob unit is a spy too, then don't rebel
				return null;
		}

		//----look for an empty location for the unit to stand ----//
		//--- scan for the 5 rows right below the building ---//

		int unitId = RaceRes[raceId].basic_unit_id;
		SpriteInfo spriteInfo = SpriteRes[UnitRes[unitId].sprite_id];
		int locX = LocX1, locY = LocY1; // xLoc & yLoc are used for returning results

		if (!World.LocateSpace(ref locX, ref locY, LocX2, LocY2, spriteInfo.LocWidth, spriteInfo.LocHeight))
			return null;

		//---------- add the unit now -----------//

		int rankId = isLeader ? Unit.RANK_GENERAL : Unit.RANK_SOLDIER;

		Unit unit = UnitArray.AddUnit(unitId, 0, rankId, 0, locX, locY);

		DecPopulation(raceId, false); // decrease the town's population

		//------- set the unit's parameters --------//

		if (isLeader)
		{
			// the higher the population, the higher the combat level will be
			int combatLevel = 10 + Population * 2 + Misc.Random(10);
			// the higher the population, the higher the leadership will be
			int leadershipLevel = 10 + Population + Misc.Random(10);

			unit.SetCombatLevel(Math.Min(combatLevel, 100));

			unit.Skill.SkillId = Skill.SKILL_LEADING;
			unit.Skill.SkillLevel = Math.Min(leadershipLevel, 100);
		}
		else
		{
			unit.SetCombatLevel(GameConstants.CITIZEN_COMBAT_LEVEL);
		}

		return unit;
	}
	
	public void FormNewNation()
	{
		if (NationArray.nation_count >= GameConstants.MAX_NATION)
			return;

		//----- determine the race with most population -----//

		int maxPop = 0, raceId = 0;

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
		{
			if (RacesPopulation[i] > maxPop)
			{
				maxPop = RacesPopulation[i];
				raceId = i + 1;
			}
		}

		int unitId = RaceRes[raceId].basic_unit_id;
		int locX = LocX1, locY = LocY1;
		SpriteInfo spriteInfo = SpriteRes[UnitRes[unitId].sprite_id];

		if (!World.LocateSpace(ref locX, ref locY, LocX2, LocY2, spriteInfo.LocWidth, spriteInfo.LocHeight))
			return;

		//--------- create a new nation ---------//

		Nation newNation = NationArray.new_nation(NationBase.NATION_AI, raceId, NationArray.random_unused_color());

		//-------- create the king --------//

		Unit kingUnit = UnitArray.AddUnit(unitId, newNation.nation_recno, Unit.RANK_KING, 100, locX, locY);
		kingUnit.Skill.SkillId = Skill.SKILL_LEADING;
		kingUnit.Skill.SkillLevel = 50 + Misc.Random(51);
		kingUnit.SetCombatLevel(70 + Misc.Random(31));

		newNation.set_king(kingUnit.SpriteId, 1);

		DecPopulation(raceId, false);

		ChangeNation(newNation.nation_recno);

		//------ increase the loyalty of the town -----//

		for (int i = 0; i < GameConstants.MAX_RACE; i++)
			RacesLoyalty[i] = 70 + Misc.Random(20); // 70 to 90 initial loyalty

		NewsArray.new_nation(newNation.nation_recno);

		//--- random extra beginning advantages -----//

		switch (Misc.Random(10))
		{
			case 1: // knowledge of weapon in the beginning.
				TechRes[Misc.Random(TechRes.tech_info_array.Length) + 1].set_nation_tech_level(newNation.nation_recno, 1);
				break;

			case 2: // random additional cash
				newNation.cash += Misc.Random(5000);
				break;

			case 3: // random additional food
				newNation.food += Misc.Random(5000);
				break;

			case 4: // random additional skilled units
				int mobileCount = Misc.Random(5) + 1;

				for (int i = 0; i < mobileCount && RecruitableRacePopulation(raceId, false) > 0; i++)
				{
					Unit unit = MobilizeTownPeople(raceId, true, false);

					if (unit == null)
						break;

					//------- randomly set a skill -------//

					int skillId;
					do
					{
						skillId = Misc.Random(Skill.MAX_TRAINABLE_SKILL) + 1;
					} while (skillId == Skill.SKILL_SPYING);

					unit.Skill.SkillId = skillId;
					unit.Skill.SkillLevel = 50 + Misc.Random(50);
					unit.SetCombatLevel(50 + Misc.Random(50));
				}
				break;
		}

		NationArray.update_statistic();
	}

	#region IndependentTownAIFunctions

	public void ThinkIndependentTown()
	{
		if (RebelId != 0) // if this is a rebel town, its AI will be executed in Rebel.ThinkTownAction()
			return;

		//---- think about toggling town links ----//

		if (Info.TotalDays % 15 == TownId % 15)
		{
			ThinkIndependentSetLink();
		}

		//---- think about independent units join existing nations ----//

		if (Info.TotalDays % 60 == TownId % 60)
		{
			ThinkIndependentUnitJoinNation();
		}

		//----- think about form a new nation -----//

		if (Info.TotalDays % 365 == TownId % 365)
		{
			ThinkIndependentFormNewNation();
		}
	}

	private void ThinkIndependentSetLink()
	{
		//---- think about working for foreign firms ------//

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			Firm firm = FirmArray[LinkedFirms[i]];

			if (firm.FirmType == Firm.FIRM_CAMP) // a town cannot change its status with a military camp
				continue;

			//---- think about the link status ----//

			bool linkStatus = (firm.NationId == 0); // if the firm is also an independent firm

			if (AverageResistance(firm.NationId) <= GameConstants.INDEPENDENT_LINK_RESISTANCE)
				linkStatus = true;

			//---- set the link status -------//

			ToggleFirmLink(i + 1, linkStatus, InternalConstants.COMMAND_AI);
		}

		AILinkChecked = true;
	}

	private void ThinkIndependentFormNewNation()
	{
		if (Misc.Random(10) > 0) // 1/10 chance to set up a new nation.
			return;

		//-------- check if the town is big enough -------//

		if (Population < 30)
			return;

		//---- don't form if the world is already densely populated ----//

		if (NationArray.all_nation_population > ConfigAdv.town_ai_emerge_nation_pop_limit)
			return;

		//----------------------------------------------//

		if (!NationArray.can_form_new_ai_nation())
			return;

		FormNewNation();
	}

	private bool ThinkIndependentUnitJoinNation()
	{
		if (JoblessPopulation == 0)
			return false;

		_independentUnitJoinNationMinRating -= 2; // make it easier to join nation everytime it's called
		// -2 each time, adding of 30 after a unit has been recruited and calling it once every 2 months make it a normal rate of joining once a year per town

		//------ think about which nation to turn towards -----//

		int bestNationId = 0, bestRaceId = 0;
		int bestRating = _independentUnitJoinNationMinRating;

		if (RegionArray.GetRegionInfo(RegionId).RegionStatId == 0)
			return false;

		RegionStat regionStat = RegionArray.GetRegionStat(RegionId);

		foreach (Nation nation in NationArray)
		{
			if (nation.race_id == 0)
				continue;

			if (nation.cash <= 0.0)
				continue;

			// don't join too frequently, at most 3 months a unit
			if (Info.game_date < nation.last_independent_unit_join_date.AddDays(90))
				continue;

			//--- only join the nation if the nation has town in the town's region ---//

			if (regionStat.TownNationCounts[nation.nation_recno - 1] == 0)
				continue;

			//----- calculate the rating of the nation -----//

			int curRating = (int)nation.reputation + nation.overall_rating;
			int raceId = 0;

			if (RecruitableRacePopulation(nation.race_id, false) > 0)
			{
				curRating += 30;
				raceId = nation.race_id;
			}

			if (curRating > bestRating)
			{
				bestRating = curRating;
				bestNationId = nation.nation_recno;
				bestRaceId = raceId;
			}
		}

		if (bestNationId == 0)
			return false;

		if (bestRaceId == 0)
			bestRaceId = PickRandomRace(false, false);

		if (bestRaceId == 0)
			return false;

		if (!IndependentUnitJoinNation(bestRaceId, bestNationId))
			return false;

		_independentUnitJoinNationMinRating = bestRating + 100 + Misc.Random(30); // reset it to a higher rating

		if (_independentUnitJoinNationMinRating < 100)
			_independentUnitJoinNationMinRating = 100;

		return true;
	}

	private bool IndependentUnitJoinNation(int raceId, int toNationId)
	{
		//----- mobilize a villager ----//

		Unit unit = MobilizeTownPeople(raceId, true, false);

		if (unit == null)
			return false;

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
				do
				{
					skillId = Misc.Random(Skill.MAX_TRAINABLE_SKILL) + 1;
				} while (skillId == Skill.SKILL_SPYING);

				skillLevel = 10 + Misc.Random(80);
				combatLevel = 10 + Misc.Random(30);
				break;
		}

		unit.Skill.SkillId = skillId;
		unit.Skill.SkillLevel = skillLevel;
		unit.SetCombatLevel(combatLevel);

		//------ change nation now --------//

		if (!unit.Betray(toNationId))
			return false;

		//---- the unit moves close to the newly joined nation ----//

		unit.AIMoveToNearbyTown();

		NationArray[toNationId].last_independent_unit_join_date = Info.game_date;

		return true;
	}
	
	#endregion
	
	#region Old AI Functions

	public void ProcessAI()
	{
		Nation ownNation = NationArray[NationId];

		//---- think about cancelling the base town status ----//

		if (Info.TotalDays % 30 == TownId % 30)
		{
			UpdateBaseTownStatus();
		}

		//------ think about granting villagers ---//

		if (Info.TotalDays % 30 == TownId % 30)
		{
			ThinkReward();
		}

		//----- if this town should migrate now -----//

		if (ShouldAIMigrate())
		{
			if (Info.TotalDays % 30 == TownId % 30)
			{
				ThinkAIMigrate();
			}

			return; // don't do anything else if the town is about to migrate
		}

		//------ think about building camps first -------//

		// if there is no space in the neighbor area for building a new firm.
		if (Info.TotalDays % 30 == TownId % 30 && !NoNeighborSpace && Population > 1)
		{
			if (ThinkBuildCamp())
			{
				return;
			}
		}

		//----- the following are base town only functions ----//

		if (!IsBaseTown)
			return;

		//------ think about collecting tax ---//

		if (Info.TotalDays % 30 == (TownId + 15) % 30)
		{
			ThinkCollectTax();
		}

		//---- think about scouting in an unexplored map ----//

		if (Info.TotalDays % 30 == (TownId + 20) % 30)
		{
			ThinkScout();
		}

		//---- think about splitting the town ---//

		if (Info.TotalDays % 30 == TownId % 30)
		{
			ThinkMoveBetweenTown();
			ThinkSplitTown();
		}

		//---- think about attacking firms/units nearby ---//

		if (Info.TotalDays % 30 == TownId % 30)
		{
			if (ThinkAttackNearbyEnemy())
			{
				return;
			}

			if (ThinkAttackLinkedEnemy())
			{
				return;
			}
		}

		//---- think about capturing linked enemy firms ----//

		if (Info.TotalDays % 60 == TownId % 60)
		{
			ThinkCaptureLinkedFirm();
		}

		//---- think about capturing enemy towns ----//

		if (Info.TotalDays % 120 == TownId % 120)
		{
			ThinkCaptureEnemyTown();
		}

		//---- think about using spies on enemies ----//

		if (Info.TotalDays % 60 == (TownId + 10) % 60)
		{
			if (ThinkSpyingTown())
			{
				return;
			}
		}

		//---- think about anti-spies activities ----//

		if (Info.TotalDays % 60 == (TownId + 20) % 60)
		{
			if (ThinkCounterSpy())
			{
				return;
			}
		}

		//--- think about setting up firms next to this town ---//

		// if there is no space in the neighbor area for building a new firm.
		if (Info.TotalDays % 30 == TownId % 30 && !NoNeighborSpace && Population >= 5)
		{
			if (ThinkBuildMarket())
			{
				return;
			}

			//--- the following functions will only be called when the nation has at least a mine ---//

			// don't build other structures if there are untapped raw sites and our nation still doesn't have any
			if (SiteArray.UntappedRawCount > 0 && ownNation.ai_mine_array.Count == 0 && ownNation.true_profit_365days() < 0)
			{
				return;
			}

			//---- only build the following if we have enough food ----//

			if (ownNation.ai_has_enough_food())
			{
				if (ThinkBuildResearch())
				{
					return;
				}

				if (ThinkBuildWarFactory())
				{
					return;
				}

				if (ThinkBuildBase())
				{
					return;
				}
			}

			//-------- think build inn ---------//

			ThinkBuildInn();
		}
	}

	private void ThinkCollectTax()
	{
		if (!HasLinkedOwnCamp)
			return;

		if (ShouldAIMigrate()) // if the town should migrate, do collect tax, otherwise the loyalty will be too low for mobilizing the peasants.
			return;

		if (AccumulatedCollectTaxPenalty > 0)
			return;

		//--- collect tax if the loyalty of all the races >= minLoyalty (55-85) ---//

		Nation ownNation = NationArray[NationId];

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

		if (AverageLoyalty() < minLoyalty)
			return;

		//---------- collect tax now ----------//

		CollectTax(InternalConstants.COMMAND_AI);
	}

	private void ThinkReward()
	{
		if (!HasLinkedOwnCamp)
			return;

		if (AccumulatedRewardPenalty > 0)
			return;

		//---- if accumulated_reward_penalty>0, don't grant unless the villagers are near the rebel level ----//

		Nation ownNation = NationArray[NationId];
		int averageLoyalty = AverageLoyalty();

		if (averageLoyalty < GameConstants.REBEL_LOYALTY + 5 + ownNation.pref_loyalty_concern / 10) // 35 to 45
		{
			int importanceRating;

			if (averageLoyalty < GameConstants.REBEL_LOYALTY + 5)
				importanceRating = 40 + Population;
			else
				importanceRating = Population;

			if (ownNation.ai_should_spend(importanceRating))
				Reward(InternalConstants.COMMAND_AI);
		}
	}

	private bool ThinkBuildFirm(int firmId, int maxFirm)
	{
		Nation nation = NationArray[NationId];

		//--- check whether the AI can build a new firm next this firm ---//

		if (!nation.can_ai_build(firmId))
			return false;

		//-- only build one market place next to this town, check if there is any existing one --//

		int firmCount = 0;

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			Firm firm = FirmArray[LinkedFirms[i]];

			//---- if there is one firm of this type near the town already ----//

			if (firm.FirmType == firmId && firm.NationId == NationId)
			{
				if (++firmCount >= maxFirm)
					return false;
			}
		}

		//------ queue building a new firm -------//

		return AIBuildNeighborFirm(firmId);
	}

	private bool ThinkBuildMarket()
	{
		// don't build the market too soon, as it may need to migrate to other town
		if (Info.game_date < _setupDate.AddDays(180.0))
			return false;

		Nation ownNation = NationArray[NationId];

		if (Population < 10 + (100 - ownNation.pref_trading_tendency) / 20)
			return false;

		if (NoNeighborSpace) // if there is no space in the neighbor area for building a new firm.
			return false;

		//--- check whether the AI can build a new firm next this firm ---//

		if (!ownNation.can_ai_build(Firm.FIRM_MARKET))
			return false;

		//----------------------------------------------------//
		// If there is already a firm queued for building with
		// a building location that is within the effective range
		// of the this firm.
		//----------------------------------------------------//

		if (ownNation.is_build_action_exist(Firm.FIRM_MARKET, LocCenterX, LocCenterY))
			return false;

		//-- only build one market place next to this mine, check if there is any existing one --//

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			Firm firm = FirmArray[LinkedFirms[i]];

			if (firm.FirmType != Firm.FIRM_MARKET)
				continue;

			FirmMarket firmMarket = (FirmMarket)firm;

			//------ if this market is our own one ------//

			if (firmMarket.NationId == NationId && firmMarket.IsRetailMarket())
				return false;
		}

		//------ queue building a new market -------//

		int buildXLoc, buildYLoc;

		if (!ownNation.find_best_firm_loc(Firm.FIRM_MARKET, LocX1, LocY1, out buildXLoc, out buildYLoc))
		{
			NoNeighborSpace = true;
			return false;
		}

		ownNation.add_action(buildXLoc, buildYLoc, LocX1, LocY1, Nation.ACTION_AI_BUILD_FIRM, Firm.FIRM_MARKET);

		return true;
	}

	private bool ThinkBuildCamp()
	{
		//Do not build second camp too early because we can move our first town
		if (Info.game_date < Info.game_start_date.AddDays(180.0))
			return false;

		//check if any of the other camps protecting this town is still recruiting soldiers
		//if so, wait until their recruitment is finished. So we can measure the protection available accurately.

		Nation ownNation = NationArray[NationId];
		int campCount = 0;

		for (int i = LinkedFirms.Count - 1; i >= 0; --i)
		{
			Firm firm = FirmArray[LinkedFirms[i]];

			if (firm.FirmType != Firm.FIRM_CAMP)
				continue;

			FirmCamp firmCamp = (FirmCamp)firm;

			if (firmCamp.NationId != NationId)
				continue;

			// if this camp is still trying to recruit soldiers
			if (firmCamp.UnderConstruction || firmCamp.ai_recruiting_soldier || firmCamp.Workers.Count < Firm.MAX_WORKER)
				return false;

			campCount++;
		}

		//--- this is one of the few base towns the nation has, then a camp must be built ---//

		if (campCount == 0 && IsBaseTown && ownNation.ai_base_town_count <= 2)
		{
			return AIBuildNeighborFirm(Firm.FIRM_CAMP);
		}

		//---- only build camp if we have enough cash and profit ----//

		if (!ownNation.ai_should_spend(70 + ownNation.pref_military_development / 4)) // 70 to 95
			return false;

		//---- only build camp if we need more protection than it is currently available ----//

		int protectionAvailable = ProtectionAvailable();
		int protectionNeeded = ProtectionNeeded();
		Nation nation = NationArray[NationId];
		//Protect 1.5 - 2.5 times more than needed depending on the military development
		if (nation.ai_has_enough_food())
			protectionNeeded = protectionNeeded * (150 + nation.pref_military_development) / 100;

		if (protectionAvailable >= protectionNeeded)
			return false;

		if (protectionAvailable > 0) // if protection available is 0, we must build a camp now
		{
			int needUrgency = 100 * (protectionNeeded - protectionAvailable) / protectionNeeded;

			if (nation.total_jobless_population - Firm.MAX_WORKER < (100 - needUrgency) * (200 - nation.pref_military_development) / 200)
			{
				return false;
			}
		}

		//--- check if we have enough people to recruit ---//

		bool buildFlag = nation.total_jobless_population >= 16;

		if (nation.total_jobless_population >= 8 && IsBaseTown)
		{
			buildFlag = true;
		}
		else if (nation.ai_has_should_close_camp(RegionId)) // there is camp that can be closed
		{
			buildFlag = true;
		}

		if (!buildFlag)
			return false;

		return AIBuildNeighborFirm(Firm.FIRM_CAMP);
	}

	private bool ThinkBuildResearch()
	{
		//Do not build the first year
		if (Info.game_date < Info.game_start_date.AddDays(365.0))
			return false;

		Nation nation = NationArray[NationId];

		if (!IsBaseTown)
			return false;

		if (JoblessPopulation < Firm.MAX_WORKER || nation.total_jobless_population < Firm.MAX_WORKER * 2)
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

			if (firmResearch.RegionId != RegionId)
				continue;

			if (firmResearch.Workers.Count < Firm.MAX_WORKER)
				return false;
		}
		//------- queue building a war factory -------//

		return AIBuildNeighborFirm(Firm.FIRM_RESEARCH);
	}

	private bool ThinkBuildWarFactory()
	{
		Nation nation = NationArray[NationId];

		if (!IsBaseTown)
			return false;

		if (JoblessPopulation < Firm.MAX_WORKER || nation.total_jobless_population < Firm.MAX_WORKER * 2)
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

			if (firmWar.RegionId != RegionId)
				continue;

			//TODO this code is different from the same in think_build_research()
			if (firmWar.Workers.Count < Firm.MAX_WORKER || firmWar.BuildUnitId == 0)
				return false;
		}

		//------- queue building a war factory -------//

		return AIBuildNeighborFirm(Firm.FIRM_WAR_FACTORY);
	}

	private bool ThinkBuildBase()
	{
		Nation nation = NationArray[NationId];

		if (!IsBaseTown)
			return false;

		//----- see if we have enough money to build & support the weapon ----//

		if (!nation.ai_should_spend(50))
			return false;

		if (JoblessPopulation < 15 || nation.total_jobless_population < 30)
			return false;

		//------ do a scan on the existing bases first ------//

		int[] buildRatingArray = new int[GameConstants.MAX_RACE];

		//--- increase build rating for the seats that this nation knows how to build ---//

		for (int i = 1; i <= GodRes.god_info_array.Length; i++)
		{
			GodInfo godInfo = GodRes[i];

			if (godInfo.is_nation_know(NationId))
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
			return AIBuildNeighborFirm(Firm.FIRM_BASE, bestRaceId);

		return false;
	}

	private bool ThinkBuildInn()
	{
		Nation ownNation = NationArray[NationId];

		if (ownNation.ai_inn_array.Count < ownNation.ai_supported_inn_count())
		{
			return ThinkBuildFirm(Firm.FIRM_INN, 1);
		}

		return false;
	}

	public bool AIBuildNeighborFirm(int firmId, int firmRaceId = 0)
	{
		int buildXLoc, buildYLoc;
		Nation nation = NationArray[NationId];

		if (!nation.find_best_firm_loc(firmId, LocX1, LocY1, out buildXLoc, out buildYLoc))
		{
			NoNeighborSpace = true;
			return false;
		}

		nation.add_action(buildXLoc, buildYLoc, LocX1, LocY1,
			Nation.ACTION_AI_BUILD_FIRM, firmId, 1, 0, firmRaceId);

		return true;
	}

	private bool ThinkAIMigrate()
	{
		// don't move if this town has just been set up for less than 90 days. It may be a town set up by think_split_town()
		if (Info.game_date < _setupDate.AddDays(90.0))
			return false;

		Nation nation = NationArray[NationId];

		//-- the higher the loyalty, the higher the chance all the unit can be migrated --//

		int averageLoyalty = AverageLoyalty();
		int minMigrateLoyalty = 35 + nation.pref_loyalty_concern / 10; // 35 to 45

		if (averageLoyalty < minMigrateLoyalty)
		{
			//-- if the total population is low (we need people) and the cash is high (we have money), then grant to increase loyalty for migration --//

			// if the average target loyalty is also lower than
			if (AccumulatedRewardPenalty == 0 && AverageTargetLoyalty() < minMigrateLoyalty + 5)
			{
				if (nation.ai_should_spend(20 + nation.pref_territorial_cohesiveness / 2)) // 20 to 70 
					Reward(InternalConstants.COMMAND_AI);
			}

			if (AverageLoyalty() < minMigrateLoyalty)
				return false;
		}

		if (!ShouldAIMigrate())
			return false;

		//------ think about which town to migrate to ------//

		int bestTownRecno = ThinkAIMigrateToTown();

		if (bestTownRecno == 0)
			return false;

		//--- check if there are already units currently migrating to the destination town ---//

		Town destTown = TownArray[bestTownRecno];

		// last 1-check duplication on the destination town only
		if (nation.get_action(destTown.LocX1, destTown.LocY1, LocX1, LocY1,
			    Nation.ACTION_AI_SETTLE_TO_OTHER_TOWN, 0, 0, 1) != null)
		{
			return false;
		}

		//--------- queue for migration now ---------//

		int migrateCount = (AverageLoyalty() - GameConstants.MIN_RECRUIT_LOYALTY) / 5;

		migrateCount = Math.Min(migrateCount, JoblessPopulation);

		if (migrateCount <= 0)
			return false;

		nation.add_action(destTown.LocX1, destTown.LocY1,
			LocX1, LocY1, Nation.ACTION_AI_SETTLE_TO_OTHER_TOWN, 0, migrateCount);

		return true;
	}

	private int ThinkAIMigrateToTown()
	{
		//------ think about which town to migrate to ------//

		Nation nation = NationArray[NationId];
		int bestRating = 0, bestTownRecno = 0;
		int majorityRace = MajorityRace();

		for (int i = 0; i < nation.ai_town_array.Count; i++)
		{
			int aiTown = nation.ai_town_array[i];
			if (TownId == aiTown)
				continue;

			Town town = TownArray[aiTown];

			if (!town.IsBaseTown) // only migrate to base towns
				continue;

			if (town.RegionId != RegionId)
				continue;

			// if the town does not have enough space for the migration
			if (Population > GameConstants.MAX_TOWN_POPULATION - town.Population)
				continue;

			//--------- compare the ratings ---------//

			// *1000 so that this will have a much bigger weight than the distance rating
			int curRating = 1000 * town.RacesPopulation[majorityRace - 1] / town.Population;

			curRating += World.DistanceRating(LocCenterX, LocCenterY, town.LocCenterX, town.LocCenterY);

			if (curRating > bestRating)
			{
				//--- if there is a considerable population of this race, then must migrate to a town with the same race ---//

				if (RacesPopulation[majorityRace - 1] >= 6)
				{
					// must be commented out otherwise low population town will never be optimized
					if (town.MajorityRace() != majorityRace)
						continue;
				}

				bestRating = curRating;
				bestTownRecno = town.TownId;
			}
		}

		return bestTownRecno;
	}

	private bool ThinkSplitTown()
	{
		if (JoblessPopulation == 0) // cannot move if we don't have any mobilizable unit
			return false;

		//--- split when the population is close to its limit ----//

		Nation nation = NationArray[NationId];

		if (Population < 45 + nation.pref_territorial_cohesiveness / 10)
			return false;

		//-------- think about which race to move --------//

		int mostRaceId1, mostRaceId2, raceId = 0;

		GetMostPopulatedRace(out mostRaceId1, out mostRaceId2);

		if (mostRaceId2 != 0 && RacesJoblessPopulation[mostRaceId2 - 1] > 0 && CanRecruit(mostRaceId2))
			raceId = mostRaceId2;

		else if (mostRaceId1 != 0 && RacesJoblessPopulation[mostRaceId1 - 1] > 0 && CanRecruit(mostRaceId1))
			raceId = mostRaceId1;

		else
			raceId = PickRandomRace(false, true); // 0-recruitable only, 1-also pick spy.

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
			if (mostRaceId2 != 0 && RacesJoblessPopulation[mostRaceId2] > 0
			                     && RacesPopulation[mostRaceId2] >= Population / 3 && CanRecruit(mostRaceId2))
			{
				raceId = mostRaceId2;
			}
		}

		if (raceId == 0)
			return false;

		//---- check if there is already a town of this race with low population linked to this town, then don't split a new one ---//

		for (int i = 0; i < LinkedTowns.Count; i++)
		{
			Town town = TownArray[LinkedTowns[i]];

			if (town.NationId == NationId && town.Population < 20 && town.MajorityRace() == raceId)
			{
				return false;
			}
		}

		//-------- settle to a new town ---------//

		return AISettleNew(raceId);
	}

	private void ThinkMoveBetweenTown()
	{
		//------ move people between linked towns ------//

		int ourMajorityRace = MajorityRace();

		for (int i = 0; i < LinkedTowns.Count; i++)
		{
			Town town = TownArray[LinkedTowns[i]];

			if (town.NationId != NationId)
				continue;

			//--- migrate people to the new town ---//

			while (true)
			{
				bool rc = false;
				int raceId = town.MajorityRace(); // get the linked town's major race

				// if our major race is not this race, then move the person to the target town
				if (ourMajorityRace != raceId)
				{
					rc = true;
				}
				else //-- if this town's major race is the same as the target town --//
				{
					// if this town's population is larger than the target town by 10 people, then move
					if (Population - town.Population > 10)
						rc = true;
				}

				if (rc)
				{
					if (!MigrateTo(town.TownId, InternalConstants.COMMAND_AI, raceId))
						break;
				}
				else
					break;
			}
		}
	}

	private bool ThinkAttackNearbyEnemy()
	{
		int xLoc1 = LocX1 - GameConstants.TOWN_SCAN_ENEMY_RANGE;
		int yLoc1 = LocY1 - GameConstants.TOWN_SCAN_ENEMY_RANGE;
		int xLoc2 = LocX2 + GameConstants.TOWN_SCAN_ENEMY_RANGE;
		int yLoc2 = LocY2 + GameConstants.TOWN_SCAN_ENEMY_RANGE;

		xLoc1 = Math.Max(xLoc1, 0);
		yLoc1 = Math.Max(yLoc1, 0);
		xLoc2 = Math.Min(xLoc2, GameConstants.MapSize - 1);
		yLoc2 = Math.Min(yLoc2, GameConstants.MapSize - 1);

		//------------------------------------------//

		int enemyCombatLevel = 0; // the higher the rating, the easier we can attack the target town.
		int enemyXLoc = -1;
		int enemyYLoc = -1;
		Nation nation = NationArray[NationId];

		for (int yLoc = yLoc1; yLoc <= yLoc2; yLoc++)
		{

			for (int xLoc = xLoc1; xLoc <= xLoc2; xLoc++)
			{
				Location location = World.GetLoc(xLoc, yLoc);
				//--- if there is an enemy unit here ---// 

				if (location.HasUnit(UnitConstants.UNIT_LAND))
				{
					Unit unit = UnitArray[location.UnitId(UnitConstants.UNIT_LAND)];

					if (unit.NationId == 0)
						continue;

					//--- if the unit is idle and he is our enemy ---//

					if (unit.CurAction == Sprite.SPRITE_IDLE &&
					    nation.get_relation_status(unit.NationId) == NationBase.NATION_HOSTILE)
					{
						enemyCombatLevel += (int)unit.HitPoints;

						if (enemyXLoc == -1 || Misc.Random(5) == 0)
						{
							enemyXLoc = xLoc;
							enemyYLoc = yLoc;
						}
					}
				}

				//--- if there is an enemy firm here ---//

				else if (location.IsFirm())
				{
					Firm firm = FirmArray[location.FirmId()];

					//------- if this is a monster firm ------//

					if (firm.FirmType == Firm.FIRM_MONSTER) // don't attack monster here, OAI_MONS.CPP will handle that
						continue;

					//------- if this is a firm of our enemy -------//

					if (nation.get_relation_status(firm.NationId) == NationBase.NATION_HOSTILE)
					{
						if (firm.Workers.Count == 0)
							enemyCombatLevel += 50; // empty firm
						else
						{
							for (int i = 0; i < firm.Workers.Count; i++)
							{
								Worker worker = firm.Workers[i];
								enemyCombatLevel += worker.hit_points;
							}
						}

						if (enemyXLoc == -1 || Misc.Random(5) == 0)
						{
							enemyXLoc = firm.LocX1;
							enemyYLoc = firm.LocY1;
						}
					}
				}
			}
		}

		//--------- attack the target now -----------//

		if (enemyCombatLevel > 0)
		{
			nation.ai_attack_target(enemyXLoc, enemyYLoc, enemyCombatLevel);
			return true;
		}

		return false;
	}

	private bool ThinkAttackLinkedEnemy()
	{
		Nation ownNation = NationArray[NationId];

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			if (LinkedFirmsEnable[i] != InternalConstants.LINK_EE) // don't attack if the link is not enabled
				continue;

			Firm firm = FirmArray[LinkedFirms[i]];

			if (firm.NationId == NationId)
				continue;

			if (firm.ShouldCloseFlag) // if this camp is about to close
				continue;

			//--- only attack AI firms when they belong to a hostile nation ---//

			if (firm.AIFirm && ownNation.get_relation_status(firm.NationId) != NationBase.NATION_HOSTILE)
			{
				continue;
			}

			//---- if this is a camp -----//

			int targetCombatLevel;
			if (firm.FirmType == Firm.FIRM_CAMP)
			{
				//----- if we are friendly with the target nation ------//

				int nationStatus = ownNation.get_relation_status(firm.NationId);

				if (nationStatus >= NationBase.NATION_FRIENDLY)
				{
					if (!ownNation.ai_should_attack_friendly(firm.NationId, 100))
						continue;

					ownNation.ai_end_treaty(firm.NationId);
				}
				else if (nationStatus == NationBase.NATION_NEUTRAL)
				{
					//-- if the link is off and the nation's military strength is bigger than us, don't attack --//

					if (NationArray[firm.NationId].military_rank_rating() > ownNation.military_rank_rating())
					{
						continue;
					}
				}

				//--- don't attack when the trade rating is high ----//

				int tradeRating = ownNation.trade_rating(firm.NationId);

				if (tradeRating > 50 || tradeRating + ownNation.ai_trade_with_rating(firm.NationId) > 100)
				{
					continue;
				}

				targetCombatLevel = ((FirmCamp)firm).total_combat_level();
			}
			else //--- if this is another type of firm ----//
			{
				//--- only attack other types of firm when the status is hostile ---//

				if (ownNation.get_relation_status(firm.NationId) != NationBase.NATION_HOSTILE)
					continue;

				targetCombatLevel = 50;
			}

			//--------- attack now ----------//

			ownNation.ai_attack_target(firm.LocX1, firm.LocY1, targetCombatLevel);

			return true;
		}

		return false;
	}

	private void ThinkCaptureLinkedFirm()
	{
		//----- scan for linked firms -----//

		for (int i = LinkedFirms.Count - 1; i >= 0; i--)
		{
			Firm firm = FirmArray[LinkedFirms[i]];

			if (firm.NationId == NationId) // this is our own firm
				continue;

			if (firm.CanWorkerCapture(NationId)) // if we can capture this firm, capture it now
			{
				firm.CaptureFirm(NationId);
			}
		}
	}

	private bool ThinkCaptureEnemyTown()
	{
		if (!IsBaseTown)
			return false;

		Nation nation = NationArray[NationId];

		if (nation.ai_capture_enemy_town_recno != 0) // a capturing action is already in process
			return false;

		int surplusProtection = ProtectionAvailable() - ProtectionNeeded();

		//-- only attack enemy town if we have enough military protection surplus ---//

		if (surplusProtection < 300 - nation.pref_military_courage * 2)
			return false;

		return nation.think_capture_new_enemy_town(this);
	}

	private bool ThinkScout()
	{
		Nation ownNation = NationArray[NationId];

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
				if (LocCenterX - destX > 100)
					destX = LocCenterX - (LocCenterX - destX) % 100;
				if (destX - LocCenterX > 100)
					destX = LocCenterX + (destX - LocCenterX) % 100;
				destY = Math.Max(LocCenterY - 100, 10);
				break;
			case 1:
				destX = Math.Min(LocCenterX + 100, GameConstants.MapSize - 1 - 10);
				destY = Misc.Random(GameConstants.MapSize - 1 - 20) + 10;
				if (LocCenterY - destY > 100)
					destY = LocCenterY - (LocCenterY - destY) % 100;
				if (destY - LocCenterY > 100)
					destY = LocCenterY + (destY - LocCenterY) % 100;
				break;
			case 2:
				destX = Misc.Random(GameConstants.MapSize - 1 - 20) + 10;
				if (LocCenterX - destX > 100)
					destX = LocCenterX - (LocCenterX - destX) % 100;
				if (destX - LocCenterX > 100)
					destX = LocCenterX + (destX - LocCenterX) % 100;
				destY = Math.Min(LocCenterY + 100, GameConstants.MapSize - 1 - 10);
				break;
			case 3:
				destX = Math.Max(LocCenterX - 100, 10);
				destY = Misc.Random(GameConstants.MapSize - 1 - 20) + 10;
				if (LocCenterY - destY > 100)
					destY = LocCenterY - (LocCenterY - destY) % 100;
				if (destY - LocCenterY > 100)
					destY = LocCenterY + (destY - LocCenterY) % 100;
				break;
		}

		ownNation.add_action(destX, destY, LocX1, LocY1, Nation.ACTION_AI_SCOUT, 0);

		return true;
	}

	private bool NewBaseTownStatus()
	{
		Nation ownNation = NationArray[NationId];

		//---- town near mine should be the base ---//

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			if (FirmArray[LinkedFirms[i]].FirmType == Firm.FIRM_MINE)
				return true;
		}

		//---- if there is a town near mine with low population and only two towns in total then only mine town should be the base ---//

		if (ownNation.ai_town_array.Count == 2)
		{
			int townWithMinePopulation = GameConstants.MAX_TOWN_POPULATION;
			for (int i = 0; i < ownNation.ai_town_array.Count; i++)
			{
				Town town = TownArray[ownNation.ai_town_array[i]];
				for (int j = 0; j < town.LinkedFirms.Count; j++)
				{
					if (FirmArray[town.LinkedFirms[j]].FirmType == Firm.FIRM_MINE)
					{
						townWithMinePopulation = town.Population;
					}
				}
			}

			if (townWithMinePopulation < GameConstants.MAX_TOWN_POPULATION / 2)
				return false;
		}

		if (Population > 20 + ownNation.pref_territorial_cohesiveness / 10)
			return true;

		//---- if there is only 1 town, then it must be the base town ---//

		if (ownNation.ai_town_array.Count == 1)
			return true;

		//---- if there is only 1 town in this region, then it must be the base town ---//

		AIRegion aiRegion = ownNation.get_ai_region(RegionId);

		if (aiRegion != null && aiRegion.town_count == 1)
			return true;

		//--- don't drop if there are employed villagers ---//

		if (JoblessPopulation != Population)
			return true;

		//---- if this town is linked to a base town, also don't drop it ----//

		for (int i = LinkedTowns.Count - 1; i >= 0; i--)
		{
			Town town = TownArray[LinkedTowns[i]];

			if (town.NationId == NationId && town.IsBaseTown)
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

	private void UpdateBaseTownStatus()
	{
		bool newBaseTownStatus = NewBaseTownStatus();

		if (newBaseTownStatus == IsBaseTown)
			return;

		Nation ownNation = NationArray[NationId];

		if (IsBaseTown)
			ownNation.ai_base_town_count--;

		if (newBaseTownStatus)
			ownNation.ai_base_town_count++;

		IsBaseTown = newBaseTownStatus;
	}

	private bool ThinkSpyingTown()
	{
		//---- don't send spies somewhere if we are low in cash or losing money ----//

		Nation ownNation = NationArray[NationId];

		// don't use spies if the population is too low, we need to use have people to grow population
		if (ownNation.total_population < 30 - ownNation.pref_spy / 10)
			return false;

		if (!ownNation.ai_should_create_new_spy(0)) //0 means take into account all spies
			return false;

		int curSpyLevel = SpyArray.TotalSpySkillLevel(Spy.SPY_TOWN, TownId, NationId, out _);
		int neededSpyLevel = NeededAntiSpyLevel();

		if (curSpyLevel > neededSpyLevel + 30)
		{
			foreach (Spy spy in SpyArray)
			{
				if (spy.TrueNationId != NationId)
					continue;

				if (spy.SpyPlace != Spy.SPY_TOWN)
					continue;

				if (spy.SpyPlaceId != TownId)
					continue;

				if (spy.SpySkill > 50)
				{
					int xLoc, yLoc;
					int cloakedNationRecno;

					bool hasNewMission = ownNation.think_spy_new_mission(spy.RaceId, RegionId, out xLoc, out yLoc, out cloakedNationRecno);

					if (hasNewMission)
					{
						Unit unit = spy.MobilizeTownSpy();
						if (unit != null)
						{
							ownNation.ai_start_spy_new_mission(unit, xLoc, yLoc, cloakedNationRecno);
							return true;
						}
					}
				}
			}
		}

		return false;
	}

	private bool ThinkCounterSpy()
	{
		if (Population >= GameConstants.MAX_TOWN_POPULATION)
			return false;

		Nation ownNation = NationArray[NationId];

		if (!ownNation.ai_should_create_new_spy(1)) //1 means take into account only conter-spies
			return false;

		//------- check if we need additional spies ------//

		int curSpyLevel = SpyArray.TotalSpySkillLevel(Spy.SPY_TOWN, TownId, NationId, out _);
		int neededSpyLevel = NeededAntiSpyLevel();

		if (neededSpyLevel > curSpyLevel + 30)
		{
			int majorityRace = MajorityRace();
			if (CanRecruit(majorityRace))
			{
				int unitRecno = Recruit(Skill.SKILL_SPYING, majorityRace, InternalConstants.COMMAND_AI);

				if (unitRecno == 0)
					return false;

				ActionNode actionNode = ownNation.add_action(LocX1, LocY1, -1, -1,
					Nation.ACTION_AI_ASSIGN_SPY, NationId, 1, unitRecno);

				if (actionNode == null)
					return false;

				TrainUnitActionId = actionNode.action_id;

				return true;
			}
		}

		return false;
	}

	public int NeededAntiSpyLevel()
	{
		return (LinkedFirms.Count * 10 + 100 * Population / GameConstants.MAX_TOWN_POPULATION) * (100 + NationArray[NationId].pref_counter_spy) / 100;
	}

	public bool ShouldAIMigrate()
	{
		//--- if this town is the base town of the nation's territory, don't migrate ---//
		Nation nation = NationArray[NationId];

		// don't migrate if this is a base town or there is no base town in this nation
		if (IsBaseTown || nation.ai_base_town_count == 0)
			return false;

		if (Population - JoblessPopulation > 0) // if there are workers in this town, don't migrate the town people
			return false;

		return true;
	}

	private int ProtectionNeeded() // an index from 0 to 100 indicating the military protection needed for this town
	{
		int protectionNeeded = Population * 10;

		for (int i = LinkedFirms.Count - 1; i >= 0; i--)
		{
			Firm firm = FirmArray[LinkedFirms[i]];

			if (firm.NationId != NationId)
				continue;

			//----- if this is a camp, add combat level points -----//

			if (firm.FirmType == Firm.FIRM_MARKET)
			{
				protectionNeeded += ((FirmMarket)firm).StockValueIndex() * 2;
			}
			else
			{
				protectionNeeded += (int)firm.Productivity * 2;

				if (firm.FirmType == Firm.FIRM_MINE) // more protection for mines
					protectionNeeded += 600;
			}
		}

		return protectionNeeded;
	}

	private int ProtectionAvailable()
	{
		int protectionLevel = 0;

		for (int i = LinkedFirms.Count - 1; i >= 0; i--)
		{
			Firm firm = FirmArray[LinkedFirms[i]];

			if (firm.NationId != NationId)
				continue;

			//----- if this is a camp, add combat level points -----//

			if (firm.FirmType == Firm.FIRM_CAMP)
			{
				int linkedTownsCount = 0;
				for (int townIndex = 0; townIndex < firm.LinkedTowns.Count; townIndex++)
				{
					Town firmTown = TownArray[firm.LinkedTowns[townIndex]];
					if (firmTown.NationId == NationId)
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

	public void AddProtectionCamps(List<int> protectionCamps, bool minimumProtection)
	{
		int majorRaceCampRecno = 0;
		List<int> thisTownProtectionCamps = new List<int>();
		Nation ownNation = NationArray[NationId];

		//For protection we leave camp with the king, one camp with the general of the major race,
		//camps with injured generals, camps with injured and weak soldiers
		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			int firmRecno = LinkedFirms[i];
			Firm firm = FirmArray[firmRecno];

			if (firm.NationId != NationId)
				continue;

			if (firm.FirmType != Firm.FIRM_CAMP)
				continue;

			//DieselMachine TODO if there is a battle going on, all linked camps should be protection camps
			FirmCamp firmCamp = (FirmCamp)firm;
			if (firmCamp.OverseerId == 0 || firmCamp.Workers.Count == 0 || firmCamp.patrol_unit_array.Count > 0)
				continue;

			if (firmCamp.OverseerId == ownNation.king_unit_recno)
			{
				thisTownProtectionCamps.Add(firmRecno);
			}

			Unit overseer = UnitArray[firmCamp.OverseerId];
			if (majorRaceCampRecno == 0 && overseer.RaceId == MajorityRace())
			{
				majorRaceCampRecno = firmRecno;
				thisTownProtectionCamps.Add(firmRecno);
			}

			if (!minimumProtection)
			{
				if ((overseer.HitPoints < overseer.MaxHitPoints) && (overseer.HitPoints < 100 - ownNation.pref_military_courage / 2))
				{
					thisTownProtectionCamps.Add(firmRecno);
				}

				int lowHitPointsSoldiersCount = 0;
				for (int j = 0; j < firmCamp.Workers.Count; j++)
				{
					Worker worker = firmCamp.Workers[j];
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

		int neededProtection = ProtectionNeeded();
		if (totalCombatLevel < neededProtection)
		{
			for (int i = 0; i < LinkedFirms.Count; i++)
			{
				int firmRecno = LinkedFirms[i];
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

				if (firm.NationId != NationId)
					continue;

				if (firm.FirmType != Firm.FIRM_CAMP)
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

	public bool CanRecruitPeople()
	{
		if (Population == GameConstants.MAX_TOWN_POPULATION)
			return true;

		Nation ownNation = NationArray[NationId];
		double prefRecruiting = 0.0;
		if (ownNation.yearly_food_change() > 0)
			prefRecruiting = ownNation.pref_military_development + (100.0 - ownNation.pref_inc_pop_by_growth);

		return LinkedCampSoldiersCount() <= Population * (4.0 + 4.0 * prefRecruiting / 200.0) / 20.0;
	}

	private bool AISettleNew(int raceId)
	{
		//-------- locate space for the new town --------//

		int xLoc = LocX1, yLoc = LocY1; // xLoc & yLoc are used for returning results

		// InternalConstants.TOWN_WIDTH + 2 for space around the town
		if (!World.LocateSpace(ref xLoc, ref yLoc, LocX2, LocY2,
			    InternalConstants.TOWN_WIDTH + 2, InternalConstants.TOWN_HEIGHT + 2,
			    UnitConstants.UNIT_LAND, RegionId, true))
		{
			return false;
		}

		//------- it must be within the effective town-to-town distance ---//

		if (Misc.RectsDistance(LocX1, LocY1, LocX2, LocY2,
			    xLoc, yLoc, xLoc + InternalConstants.TOWN_WIDTH - 1, yLoc + InternalConstants.TOWN_HEIGHT - 1) >
		    InternalConstants.EFFECTIVE_TOWN_TOWN_DISTANCE)
		{
			return false;
		}

		// TODO: Should preferably check for a space that has an attached camp, and if can't find then immediately issue order to build a new camp.

		//--- recruit a unit from the town and order it to settle a new town ---//

		int unitRecno = Recruit(-1, raceId, InternalConstants.COMMAND_AI);

		if (unitRecno == 0 || !UnitArray[unitRecno].IsVisible())
			return false;

		UnitArray[unitRecno].Settle(xLoc + 1, yLoc + 1);

		return true;
	}

	#endregion
}
