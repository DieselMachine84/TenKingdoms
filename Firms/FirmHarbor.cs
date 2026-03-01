using System;
using System.Collections.Generic;
using System.IO;

namespace TenKingdoms;

public class FirmHarbor : Firm
{
	public int LandRegionId { get; private set; }
	public int SeaRegionId { get; private set; }

	public List<int> Ships { get; } = new List<int>();

	public int BuildUnitType { get; private set; }
	public List<int> BuildQueue { get; } = new List<int>();
	private long _startBuildFrameNumber;

	private RegionArray RegionArray => Sys.Instance.RegionArray;

	public FirmHarbor()
	{
	}

	protected override void DeinitDerived()
	{
		for (int i = Ships.Count - 1; i >= 0; i--)
		{
			int shipUnitId = Ships[i];

			//--- if the ship does not sailed out due to no limit sea space, delete it  ---//

			if (!SailShip(shipUnitId, InternalConstants.COMMAND_AUTO))
			{
				DelHostedShip(shipUnitId);
				UnitArray.DeleteUnit(UnitArray[shipUnitId]);
			}
		}
	}

	protected override void SetFirmBuildId(int locX, int locY, string buildCode)
	{
		FirmInfo firmInfo = FirmRes[FirmType];
		if (World.GetLoc(locX + 1, locY + 2).CanBuildHarbor(1))
		{
			// check north harbour
			FirmBuildId = firmInfo.GetBuildId("N");
		}
		else if (World.GetLoc(locX + 1, locY).CanBuildHarbor(1))
		{
			// check south harbour
			FirmBuildId = firmInfo.GetBuildId("S");
		}
		else if (World.GetLoc(locX + 2, locY + 1).CanBuildHarbor(1))
		{
			// check west harbour
			FirmBuildId = firmInfo.GetBuildId("W");
		}
		else if (World.GetLoc(locX, locY + 1).CanBuildHarbor(1))
		{
			// check east harbour
			FirmBuildId = firmInfo.GetBuildId("E");
		}
	}

	protected override void SetRegionId(int locX, int locY)
	{
		if (World.GetLoc(locX + 1, locY + 2).CanBuildHarbor(1))
		{
			// check north harbour
			LandRegionId = World.GetLoc(locX + 1, locY + 2).RegionId;
			SeaRegionId = World.GetLoc(locX + 1, locY).RegionId;
		}
		else if (World.GetLoc(locX + 1, locY).CanBuildHarbor(1))
		{
			// check south harbour
			LandRegionId = World.GetLoc(locX + 1, locY).RegionId;
			SeaRegionId = World.GetLoc(locX + 1, locY + 2).RegionId;
		}
		else if (World.GetLoc(locX + 2, locY + 1).CanBuildHarbor(1))
		{
			// check west harbour
			LandRegionId = World.GetLoc(locX + 2, locY + 1).RegionId;
			SeaRegionId = World.GetLoc(locX, locY + 1).RegionId;
		}
		else if (World.GetLoc(locX, locY + 1).CanBuildHarbor(1))
		{
			// check east harbour
			LandRegionId = World.GetLoc(locX, locY + 1).RegionId;
			SeaRegionId = World.GetLoc(locX + 2, locY + 1).RegionId;
		}

		RegionId = LandRegionId;

		// TODO remove?
		RegionArray.UpdateRegionStat();
	}

	public override void NextDay()
	{
		base.NextDay();

		if (BuildUnitType != 0)
			ProcessBuild();
		else
			ProcessQueue();
	}
	
	public override void ChangeNation(int newNationId)
	{
		//--- empty the build queue ---//

		// Note: this fixes a bug with a nation-changed harbor building a ship that the nation doesn't have,
		//       which leads to a crash (when selecting attack sprite) because CurAttack is not set properly.
		if (BuildUnitType != 0)
			CancelBuildUnit();
		BuildQueue.Clear();

		base.ChangeNation(newNationId);
	}

	public override bool IsOperating()
	{
		return true;
	}
	
	public override void AssignUnit(int unitId)
	{
		Unit unit = UnitArray[unitId];
		
		if (unit.Skill.SkillId == Skill.SKILL_CONSTRUCTION)
		{
			SetBuilder(unitId);
			return;
		}

		if (Ships.Count + (BuildUnitType > 0 ? 1 : 0) == GameConstants.MAX_SHIP_IN_HARBOR)
			return; // leave a room for the building unit

		AddHostedShip(unitId);
		unit.DeinitSprite();

		UnitMarine ship = (UnitMarine)UnitArray[unitId];
		ship.ExtraMoveInBeach = UnitMarine.NO_EXTRA_MOVE;
	}

	public void AddQueue(int unitType, int amount = 1)
	{
		if (amount <= 0)
			return;

		int queueSpace = GameConstants.MAX_BUILD_SHIP_QUEUE - BuildQueue.Count - (BuildUnitType > 0 ? 1 : 0);
		int enqueueAmount = Math.Min(queueSpace, amount);

		for (int i = 0; i < enqueueAmount; ++i)
			BuildQueue.Add(unitType);
	}

	public void RemoveQueue(int unitType, int amount = 1)
	{
		if (amount <= 0)
			return;

		for (int i = BuildQueue.Count - 1; i >= 0; i--)
		{
			if (BuildQueue[i] == unitType)
			{
				BuildQueue.RemoveAt(i);
				amount--;
				if (amount == 0)
					break;
			}
		}

		// If there were less units of unitId in the queue than were requested to be removed
		// then also cancel currently built unit
		if (amount > 0 && BuildUnitType == unitType)
			CancelBuildUnit();
	}

	public void CancelBuildUnit()
	{
		BuildUnitType = 0;
	}
	
	private void ProcessQueue()
	{
		if (BuildQueue.Count == 0)
			return;
		
		if (Ships.Count >= GameConstants.MAX_SHIP_IN_HARBOR)
			return;

		Nation nation = NationArray[NationId];
		if (nation.Cash < UnitRes[BuildQueue[0]].BuildCost)
			return;

		BuildUnitType = BuildQueue[0];
		BuildQueue.RemoveAt(0);
		nation.AddExpense(NationBase.EXPENSE_SHIP, UnitRes[BuildUnitType].BuildCost);

		_startBuildFrameNumber = Sys.Instance.FrameNumber;
	}
	
	private void ProcessBuild()
	{
		int totalBuildDays = UnitRes[BuildUnitType].BuildDays;

		if ((Sys.Instance.FrameNumber - _startBuildFrameNumber) / InternalConstants.FRAMES_PER_DAY >= totalBuildDays)
		{
			Unit unit = UnitArray.AddUnit(BuildUnitType, NationId);
			AddHostedShip(unit.SpriteId);

			if (OwnFirm())
				Sys.Instance.Audio.ReadySound(LocCenterX, LocCenterY, 1, 'F', FirmType, "FINS", 'S', UnitRes[BuildUnitType].SpriteId);

			BuildUnitType = 0;
		}
	}
	
	private void AddHostedShip(int shipId)
	{
		Ships.Add(shipId);
		UnitArray[shipId].SetMode(UnitConstants.UNIT_MODE_IN_HARBOR, FirmId);
	}
	
	public void DelHostedShip(int shipId)
	{
		UnitArray[shipId].SetMode(0);

		if (Ships.Contains(shipId))
			Ships.Remove(shipId);
	}

	public bool SailShip(int unitId, int remoteAction)
	{
		if (!Ships.Contains(unitId))
			return false;
		
		//if (!remoteAction && remote.is_enable())
		//{
			//// packet structure : <firm recno> <browseRecno>
			//short *shortPtr = (short *)remote.new_send_queue_msg(MSG_F_HARBOR_SAIL_SHIP, 2*sizeof(short) );
			//shortPtr[0] = firm_recno;
			//shortPtr[1] = unitRecno;
			//return;
		//}

		Unit unit = UnitArray[unitId];

		SpriteInfo spriteInfo = unit.SpriteInfo;
		int locX = LocX1;
		int locY = LocY1;

		if (!World.LocateSpace(ref locX, ref locY, LocX2, LocY2, spriteInfo.LocWidth, spriteInfo.LocHeight, UnitConstants.UNIT_SEA, SeaRegionId))
		{
			return false;
		}

		unit.InitSprite(locX, locY);

		DelHostedShip(unitId);

		// TODO select ship
		/*if (FirmArray.SelectedFirmId == FirmId && NationId == NationArray.player_recno)
		{
			Power.reset_selection();
			unit.SelectedFlag = true;
			UnitArray.SelectedUnitId = unit.SpriteId;
			UnitArray.SelectedCount = 1;
		}*/

		return true;
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
		if (BuildUnitType != 0) // if it's currently building a ship
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

		if (ownNation.GetTechLevelByUnitType(UnitConstants.UNIT_GALLEON) > 0)
		{
			buildId = UnitConstants.UNIT_GALLEON;

			if (ownNation.ai_ship_array.Count < 2 + (ownNation.pref_use_marine > 50 ? 1 : 0))
				rc = true;
			else
				rc = enemyShipCount > aiShipCount;
		}
		else if (ownNation.GetTechLevelByUnitType(UnitConstants.UNIT_CARAVEL) > 0)
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
			AddQueue(buildId);
	}

	public void think_build_firm()
	{
		Nation ownNation = NationArray[NationId];

		if (ownNation.Cash < 2000) // don't build if the cash is too low
			return;

		if (ownNation.TrueProfit365Days() < (50 - ownNation.pref_use_marine) * 20) //	-1000 to +1000
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
		    ownNation.TotalJoblessPopulation * 2 > 150)
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

			if (unitMarine.StopDefinedNum < 2 && UnitRes[unitMarine.UnitType].CarryGoodsCapacity > 0)
			{
				return unitMarine;
			}
		}

		return null;
	}
	
	public bool can_build_ship()
	{
		return BuildUnitType == 0;
	}
	
	public int total_linked_trade_firm()
	{
		return 0;
	}
	
	#endregion
	
	#region SaveAndLoad

	public override void SaveTo(BinaryWriter writer)
	{
		base.SaveTo(writer);
		writer.Write(LandRegionId);
		writer.Write(SeaRegionId);
		writer.Write(Ships.Count);
		for (int i = 0; i < Ships.Count; i++)
			writer.Write(Ships[i]);
		writer.Write(BuildUnitType);
		writer.Write(BuildQueue.Count);
		for (int i = 0; i < BuildQueue.Count; i++)
			writer.Write(BuildQueue[i]);
		writer.Write(_startBuildFrameNumber);
	}

	public override void LoadFrom(BinaryReader reader)
	{
		base.LoadFrom(reader);
		LandRegionId = reader.ReadInt32();
		SeaRegionId = reader.ReadInt32();
		int shipsCount = reader.ReadInt32();
		for (int i = 0; i < shipsCount; i++)
			Ships.Add(reader.ReadInt32());
		BuildUnitType = reader.ReadInt32();
		int buildQueueCount = reader.ReadInt32();
		for (int i = 0; i < buildQueueCount; i++)
			BuildQueue.Add(reader.ReadInt32());
		_startBuildFrameNumber = reader.ReadInt64();
	}
	
	#endregion
}