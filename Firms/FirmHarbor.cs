using System;
using System.Collections.Generic;
using System.Linq;

namespace TenKingdoms;

public class FirmHarbor : Firm
{
	public int LandRegionId { get; private set; }
	public int SeaRegionId { get; private set; }

	public List<int> Ships { get; } = new List<int>(GameConstants.MAX_SHIP_IN_HARBOR);

	public int BuildUnitId { get; set; }
	public List<int> BuildQueue { get; } = new List<int>();
	private long StartBuildFrameNumber { get; set; }

	public List<int> LinkedMines { get; } = new List<int>();
	public List<int> LinkedFactories { get; } = new List<int>();
	public List<int> LinkedMarkets { get; } = new List<int>();

	private RegionArray RegionArray => Sys.Instance.RegionArray;

	public FirmHarbor()
	{
	}

	public override void Init(int nationId, int firmType, int locX, int locY, string buildCode = "", int builderId = 0)
	{
		if (World.GetLoc(locX + 1, locY + 2).CanBuildHarbor(1))
		{
			// check north harbour
			base.Init(nationId, firmType, locX, locY, "N", builderId);
			LandRegionId = World.GetLoc(locX + 1, locY + 2).RegionId;
			SeaRegionId = World.GetLoc(locX + 1, locY).RegionId;
		}
		else if (World.GetLoc(locX + 1, locY).CanBuildHarbor(1))
		{
			// check south harbour
			base.Init(nationId, firmType, locX, locY, "S", builderId);
			LandRegionId = World.GetLoc(locX + 1, locY).RegionId;
			SeaRegionId = World.GetLoc(locX + 1, locY + 2).RegionId;
		}
		else if (World.GetLoc(locX + 2, locY + 1).CanBuildHarbor(1))
		{
			// check west harbour
			base.Init(nationId, firmType, locX, locY, "W", builderId);
			LandRegionId = World.GetLoc(locX + 2, locY + 1).RegionId;
			SeaRegionId = World.GetLoc(locX, locY + 1).RegionId;
		}
		else if (World.GetLoc(locX, locY + 1).CanBuildHarbor(1))
		{
			// check east harbour
			base.Init(nationId, firmType, locX, locY, "E", builderId);
			LandRegionId = World.GetLoc(locX, locY + 1).RegionId;
			SeaRegionId = World.GetLoc(locX + 2, locY + 1).RegionId;
		}

		RegionId = LandRegionId; // set region_id to land_region_id

		//------- update the harbor count of the regions ------//

		// TODO remove?
		RegionArray.UpdateRegionStat();
	}

	protected override void DeinitDerived()
	{
		for (int i = Ships.Count - 1; i >= 0; i--)
		{
			int shipUnitRecno = Ships[i];

			//---- mobilize ships in the harbor ----//

			SailShip(shipUnitRecno, InternalConstants.COMMAND_AUTO);

			//--- if the ship does not sailed out due to no limit sea space, delete it  ---//

			if (Ships.Count == i)
			{
				DelHostedShip(shipUnitRecno);
				UnitArray.DeleteUnit(UnitArray[shipUnitRecno]);
			}
		}
	}

	public override void NextDay()
	{
		base.NextDay();

		//------- process building -------//

		if (BuildUnitId != 0)
			ProcessBuild();
		else
			ProcessQueue();
	}
	
	//TODO ChangeNation?

	public override bool IsOperating()
	{
		return true;
	}
	
	public override void AssignUnit(int unitRecno)
	{
		//------- if this is a construction worker -------//

		if (UnitArray[unitRecno].Skill.SkillId == Skill.SKILL_CONSTRUCTION)
		{
			SetBuilder(unitRecno);
			return;
		}

		//---------------------------------------//

		if (Ships.Count + (BuildUnitId > 0 ? 1 : 0) == GameConstants.MAX_SHIP_IN_HARBOR)
			return; // leave a room for the building unit

		AddHostedShip(unitRecno);

		UnitArray[unitRecno].DeinitSprite();

		UnitMarine unit = (UnitMarine)UnitArray[unitRecno];
		unit.ExtraMoveInBeach = UnitMarine.NO_EXTRA_MOVE;
		unit.DeinitSprite();
	}

	public void AddQueue(int unitId, int amount = 1)
	{
		if (amount <= 0)
			return;

		int queueSpace = GameConstants.MAX_BUILD_SHIP_QUEUE - BuildQueue.Count - (BuildUnitId > 0 ? 1 : 0);
		int enqueueAmount = Math.Min(queueSpace, amount);

		for (int i = 0; i < enqueueAmount; ++i)
			BuildQueue.Add(unitId);
	}

	public void RemoveQueue(int unitId, int amount = 1)
	{
		if (amount <= 0)
			return;

		for (int i = BuildQueue.Count - 1; i >= 0; i--)
		{
			if (BuildQueue[i] == unitId)
			{
				BuildQueue.RemoveAt(i);
				amount--;
				if (amount == 0)
					break;
			}
		}

		// If there were less units of unitId in the queue than were requested to be removed
		// then also cancel currently built unit
		if (amount > 0 && BuildUnitId == unitId)
			CancelBuildUnit();
	}

	public void CancelBuildUnit()
	{
		BuildUnitId = 0;
	}
	
	private void ProcessQueue()
	{
		if (BuildQueue.Count == 0)
			return;
		
		//TODO rewrite
		// remove the queue no matter build_ship success or not
		//build_ship(build_queue.Dequeue(), InternalConstants.COMMAND_AUTO);
	}
	
	private void ProcessBuild()
	{
		int totalBuildDays = UnitRes[BuildUnitId].build_days;


		if ((Sys.Instance.FrameNumber - StartBuildFrameNumber) / InternalConstants.FRAMES_PER_DAY >= totalBuildDays)
		{
			Unit unit = UnitArray.AddUnit(BuildUnitId, NationId);

			AddHostedShip(unit.SpriteId);

			if (OwnFirm())
				SERes.far_sound(LocCenterX, LocCenterY, 1, 'F', FirmType,
					"FINS", 'S', UnitRes[BuildUnitId].sprite_id);

			BuildUnitId = 0;
		}
	}
	
	public void BuildShip(int unitId, int remoteAction)
	{
		if (Ships.Count >= GameConstants.MAX_SHIP_IN_HARBOR)
			return;

		Nation nation = NationArray[NationId];

		if (nation.cash < UnitRes[unitId].build_cost)
			return;

		nation.add_expense(NationBase.EXPENSE_SHIP, UnitRes[unitId].build_cost);

		BuildUnitId = unitId;
		StartBuildFrameNumber = Sys.Instance.FrameNumber;
	}
	
	private void AddHostedShip(int shipRecno)
	{
		Ships.Add(shipRecno);

		//---- set the unit_mode of the ship ----//

		UnitArray[shipRecno].SetMode(UnitConstants.UNIT_MODE_IN_HARBOR, FirmId);

		//---------------------------------------//

		//TODO drawing
		//if( firm_recno == FirmArray.selected_recno )
		//put_info(INFO_UPDATE);
	}
	
	public void DelHostedShip(int delUnitRecno)
	{
		//---- reset the unit_mode of the ship ----//

		UnitArray[delUnitRecno].SetMode(0);

		//-----------------------------------------//

		for (int i = Ships.Count - 1; i >= 0; i--)
		{
			if (Ships[i] == delUnitRecno)
			{
				Ships.RemoveAt(i);
				break;
			}
		}

		//TODO drawing
		//if( firm_recno == FirmArray.selected_recno )
		//put_info(INFO_UPDATE);
	}

	public void SailShip(int unitRecno, int remoteAction)
	{
		//if( !remoteAction && remote.is_enable() )
		//{
		//// packet structure : <firm recno> <browseRecno>
		//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_F_HARBOR_SAIL_SHIP, 2*sizeof(short) );
		//shortPtr[0] = firm_recno;
		//shortPtr[1] = unitRecno;
		//return;
		//}

		//----- get the browse recno of the ship in the harbor's ship_recno_array[] ----//

		int browseRecno = 0;

		for (int i = Ships.Count - 1; i >= 0; i--)
		{
			if (Ships[i] == unitRecno)
			{
				browseRecno = i + 1;
				break;
			}
		}

		if (browseRecno == 0)
			return;

		//------------------------------------------------------------//

		Unit unit = UnitArray[unitRecno];

		SpriteInfo spriteInfo = unit.SpriteInfo;
		int xLoc = LocX1; // xLoc & yLoc are used for returning results
		int yLoc = LocY1;

		if (!World.LocateSpace(ref xLoc, ref yLoc, LocX2, LocY2,
			    spriteInfo.LocWidth, spriteInfo.LocHeight, UnitConstants.UNIT_SEA, SeaRegionId))
		{
			return;
		}

		unit.InitSprite(xLoc, yLoc);

		DelHostedShip(Ships[browseRecno - 1]);

		// TODO select ship

		/*if (FirmArray.SelectedFirmId == FirmId && NationId == NationArray.player_recno)
		{
			Power.reset_selection();
			unit.SelectedFlag = true;
			UnitArray.SelectedUnitId = unit.SpriteId;
			UnitArray.SelectedCount = 1;
		}*/
	}
	
	public void UpdateLinkedFirmsInfo()
	{
		LinkedMines.Clear();
		LinkedFactories.Clear();
		LinkedMarkets.Clear();

		for (int i = LinkedFirms.Count - 1; i >= 0; i--)
		{
			if (LinkedFirmsEnable[i] != InternalConstants.LINK_EE)
				continue;

			Firm firm = FirmArray[LinkedFirms[i]];
			if (!NationArray[NationId].get_relation(firm.NationId).trade_treaty)
				continue;

			switch (firm.FirmType)
			{
				case FIRM_MINE:
					LinkedMines.Add(firm.FirmId);
					break;

				case FIRM_FACTORY:
					LinkedFactories.Add(firm.FirmId);
					break;

				case FIRM_MARKET:
					LinkedMarkets.Add(firm.FirmId);
					break;
			}
		}
	}

	public override bool ShouldShowInfo()
	{
		if (base.ShouldShowInfo())
			return true;

		//--- if any of the ships in the harbor has the spies of the player ---//

		for (int i = 0; i < Ships.Count; i++)
		{
			if (UnitArray[Ships[i]].ShouldShowInfo())
				return true;
		}

		return false;
	}

	public override void DrawDetails(IRenderer renderer)
	{
		renderer.DrawHarborDetails(this);
	}

	public override void HandleDetailsInput(IRenderer renderer)
	{
		renderer.HandleHarborDetailsInput(this);
	}
	
	#region Old AI Functions
	
	public override void ProcessAI()
	{
		if (Info.TotalDays % 30 == FirmId % 30)
			think_build_ship();

		/*
		if( info.game_date%90 == firm_recno%90 )
			think_build_firm();

		if( info.game_date%60 == firm_recno%60 )
			think_trade();
		*/
	}

	public void think_build_ship()
	{
		if (BuildUnitId != 0) // if it's currently building a ship
			return;

		if (!can_build_ship()) // full, cannot build anymore
			return;

		Nation ownNation = NationArray[NationId];

		if (!ownNation.ai_should_spend(50 + ownNation.pref_use_marine / 4))
			return;

		//---------------------------------------------//
		//
		// For Transport, in most cases, an AI will just
		// need one to two.
		//
		// For Caravel and Galleon, the AI will build as many
		// as the harbor can hold if any of the human players
		// has more ships than the AI has.
		//
		//---------------------------------------------//

		int buildId = 0;
		bool rc = false;
		// return the no. of ships of the enemy that is strongest on the sea
		int enemyShipCount = ownNation.max_human_battle_ship_count();
		int aiShipCount = ownNation.ai_ship_array.Count;

		if (UnitRes[UnitConstants.UNIT_GALLEON].get_nation_tech_level(NationId) > 0)
		{
			buildId = UnitConstants.UNIT_GALLEON;

			if (ownNation.ai_ship_array.Count < 2 + (ownNation.pref_use_marine > 50 ? 1 : 0))
				rc = true;
			else
				rc = enemyShipCount > aiShipCount;
		}
		else if (UnitRes[UnitConstants.UNIT_CARAVEL].get_nation_tech_level(NationId) > 0)
		{
			buildId = UnitConstants.UNIT_CARAVEL;

			if (aiShipCount < 2 + (ownNation.pref_use_marine > 50 ? 1 : 0))
				rc = true;
			else
				rc = enemyShipCount > aiShipCount;
		}
		else
		{
			buildId = UnitConstants.UNIT_TRANSPORT;

			if (aiShipCount < 2)
				rc = true;
		}

		//---------------------------------------------//

		if (rc)
			BuildShip(buildId, InternalConstants.COMMAND_AI);
	}

	public void think_build_firm()
	{
		Nation ownNation = NationArray[NationId];

		if (ownNation.cash < 2000) // don't build if the cash is too low
			return;

		if (ownNation.true_profit_365days() < (50 - ownNation.pref_use_marine) * 20) //	-1000 to +1000
			return;

		//----- think about building markets ------//

		if (ownNation.pref_trading_tendency >= 60)
		{
			if (ai_build_firm(FIRM_MARKET))
				return;
		}

		//----- think about building camps ------//

		if (ownNation.pref_military_development / 2 +
		    (LinkedFirms.Count + ownNation.ai_ship_array.Count + Ships.Count) * 10 +
		    ownNation.total_jobless_population * 2 > 150)
		{
			ai_build_firm(FIRM_CAMP);
		}
	}

	public bool ai_build_firm(int firmId)
	{
		if (NoNeighborSpace) // if there is no space in the neighbor area for building a new firm.
			return false;

		Nation ownNation = NationArray[NationId];

		//--- check whether the AI can build a new firm next this firm ---//

		if (!ownNation.can_ai_build(firmId))
			return false;

		//-- only build one market place next to this mine, check if there is any existing one --//

		//### begin alex 24/9 ###//
		/*FirmMarket* firm;
	
		for(int i=0; i<linked_firm_count; i++)
		{
			err_when(!linked_firm_array[i] || FirmArray.is_deleted(linked_firm_array[i]));
	
			firm = (FirmMarket*) FirmArray[linked_firm_array[i]];
	
			if( firm.firm_id!=firmId )
				continue;
	
			//------ if this market is our own one ------//
	
			if( firm.nation_recno == nation_recno )
				return 0;
		}*/

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			Firm firm = FirmArray[LinkedFirms[i]];

			if (firm.FirmType != firmId)
				continue;

			//------ if this market is our own one ------//

			if (firm.NationId == NationId)
				return false;
		}
		//#### end alex 24/9 ####//

		//------ queue building a new market -------//

		int buildXLoc, buildYLoc;

		if (!ownNation.find_best_firm_loc(firmId, LocX1, LocY1, out buildXLoc, out buildYLoc))
		{
			NoNeighborSpace = true;
			return false;
		}

		ownNation.add_action(buildXLoc, buildYLoc, LocX1, LocY1,
			Nation.ACTION_AI_BUILD_FIRM, firmId);

		return true;
	}

	public bool think_trade()
	{
		//---- see if we have any free ship available ----//

		Nation ownNation = NationArray[NationId];
		UnitMarine unitMarine = ai_get_free_trade_ship();

		if (unitMarine == null)
			return false;

		//--- if this harbor is not linked to any trade firms, return now ---//

		if (total_linked_trade_firm() == 0)
			return false;

		//----- scan for another harbor to trade with this harbor ----//

		bool hasTradeShip = false;
		FirmHarbor toHarbor = null;
		foreach (Firm firm in FirmArray)
		{
			if (firm.FirmType != FIRM_HARBOR)
				continue;

			FirmHarbor firmHarbor = (FirmHarbor)firm;

			if (firmHarbor.total_linked_trade_firm() == 0)
				continue;

			// if there has been any ships trading between these two harbors yet
			if (!ownNation.has_trade_ship(FirmId, firmHarbor.FirmId))
			{
				hasTradeShip = true;
				toHarbor = firmHarbor;
				break;
			}
		}

		if (!hasTradeShip)
			return false;

		//------ try to set up a sea trade now ------//

		unitMarine.SetStop(1, LocX1, LocY1, InternalConstants.COMMAND_AI);
		unitMarine.SetStop(2, toHarbor.LocX1, toHarbor.LocY1, InternalConstants.COMMAND_AI);

		unitMarine.SetStopPickUp(1, TradeStop.AUTO_PICK_UP, InternalConstants.COMMAND_AI);
		unitMarine.SetStopPickUp(2, TradeStop.AUTO_PICK_UP, InternalConstants.COMMAND_AI);

		return true;
	}

	public UnitMarine ai_get_free_trade_ship()
	{
		Nation ownNation = NationArray[NationId];

		for (int i = ownNation.ai_ship_array.Count - 1; i >= 0; i--)
		{
			UnitMarine unitMarine = (UnitMarine)UnitArray[ownNation.ai_ship_array[i]];

			//--- if this is a goods carrying ship and it doesn't have a defined trade route ---//

			if (unitMarine.StopDefinedNum < 2 && UnitRes[unitMarine.UnitType].carry_goods_capacity > 0)
			{
				return unitMarine;
			}
		}

		return null;
	}
	
	public bool can_build_ship()
	{
		return BuildUnitId == 0;
	}
	
	public int total_linked_trade_firm()
	{
		return LinkedMines.Count + LinkedFactories.Count + LinkedMarkets.Count;
	}
	
	#endregion
}