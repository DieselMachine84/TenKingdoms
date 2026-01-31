using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class InnUnit
{
	public int UnitType { get; set; }
	public Skill Skill { get; } = new Skill();
	public int HireCost { get; private set; }
	public int StayCount { get; set; } // how long this unit is going to stay until it leaves the inn if you do not hire him.
	public int SpyId { get; set; } // >0 if this unit is a spy

	public void SetHireCost()
	{
		HireCost = Skill.CombatLevel + Skill.SkillLevel * 2;

		if (Skill.SkillId == Skill.SKILL_LEADING) // the cost of a leader unit is higher
		{
			HireCost += Skill.SkillLevel * 2;

			// increase the hiring cost with bigger steps when the skill level gets higher
			for (int i = 10; i <= 100; i += 20)
			{
				if (i > Skill.SkillLevel)
					HireCost += i / 5;
			}
		}
		else if (Skill.SkillId == Skill.SKILL_SPYING) // the cost of a spy unit is higher
		{
			HireCost += Skill.SkillLevel;
		}

		HireCost *= 2;
	}
}

public class FirmInn : Firm
{
	private int NextSkillId { get; set; } // the skill id. of the next available unit

	public List<InnUnit> InnUnits { get; } = new List<InnUnit>(GameConstants.MAX_INN_UNIT);

	public FirmInn()
	{
	}

	protected override void InitDerived()
	{
		NextSkillId = Misc.Random(Skill.MAX_TRAINABLE_SKILL) + 1;
	}

	public override void NextDay()
	{
		base.NextDay();

		//------------ update the hire list ------------//

		int updateInterval = 10 + Info.YearsPassed * 2; // there will be less and less units to hire as the game passes

		if (Info.TotalDays % updateInterval == FirmId % updateInterval)
			UpdateAddHireList();

		if (Info.TotalDays % 10 == FirmId % 10)
			UpdateDelHireList();
	}

	private void UpdateAddHireList()
	{
		//-------- new units come by --------//

		if (InnUnits.Count < GameConstants.MAX_INN_UNIT)
		{
			if (ShouldAddInnUnit())
			{
				int unitType = RaceRes[ConfigAdv.GetRandomRace()].BasicUnitType;
				if (unitType != 0)
					AddInnUnit(unitType);
			}
		}
	}

	private bool ShouldAddInnUnit()
	{
		int totalInnUnit = InnUnits.Count;

		for (int i = 0; i < LinkedFirms.Count; i++)
		{
			// links between inns are stored in LinkedFirms[] for quick scan only
			FirmInn firmInn = (FirmInn)FirmArray[LinkedFirms[i]];
			totalInnUnit += firmInn.InnUnits.Count;
		}

		return totalInnUnit < GameConstants.MAX_INN_UNIT_PER_REGION;
	}

	private void AddInnUnit(int unitType)
	{
		InnUnit innUnit = new InnUnit();
		InnUnits.Add(innUnit);

		innUnit.UnitType = unitType;

		int skillId = NextSkillId;

		if (++NextSkillId > Skill.MAX_TRAINABLE_SKILL)
			NextSkillId = 1;

		innUnit.Skill.SkillId = skillId;

		if (skillId > 0)
			innUnit.Skill.SkillLevel = 30 + Misc.Random(70);
		else
			innUnit.Skill.SkillLevel = 0;

		if (skillId == 0 || skillId == Skill.SKILL_LEADING)
			innUnit.Skill.CombatLevel = 30 + Misc.Random(70);
		else
			innUnit.Skill.CombatLevel = 10;

		innUnit.SetHireCost();

		innUnit.StayCount = 5 + Misc.Random(5);

		innUnit.SpyId = 0;
	}

	private void UpdateDelHireList()
	{
		//------- existing units leave -------//

		for (int i = InnUnits.Count - 1; i >= 0; i--)
		{
			if (InnUnits[i].SpyId == 0 && --InnUnits[i].StayCount == 0)
			{
				DelInnUnit(i + 1);
			}
		}
	}

	private void DelInnUnit(int recNo)
	{
		InnUnits.RemoveAt(recNo - 1);
	}
	
	public int Hire(int innUnitId)
	{
		//--------- first check if you have enough money to hire ------//

		Nation nation = NationArray[NationId];

		InnUnit innUnit = InnUnits[innUnitId - 1];

		if (nation.Cash < innUnit.HireCost)
			return 0;

		//---------- add the unit now -----------//

		int unitId = CreateUnit(innUnit.UnitType);
		if (unitId == 0)
			return 0; // no space for creating the unit

		nation.AddExpense(NationBase.EXPENSE_HIRE_UNIT, innUnit.HireCost);

		Unit unit = UnitArray[unitId];
		unit.Skill.SkillId = innUnit.Skill.SkillId;
		unit.Skill.SkillLevel = innUnit.Skill.SkillLevel;
		unit.SetCombatLevel(innUnit.Skill.CombatLevel);

		if (unit.Skill.SkillId == Skill.SKILL_SPYING)
		{
			Spy spy = SpyArray.AddSpy(unitId, unit.Skill.SkillLevel);
			unit.SpyId = spy.SpyId;
			unit.Skill.SkillId = 0; // reset its primary skill, its spying skill has been recorded in SpyArray
		}

		//----------------------------------------//
		//
		// Loyalty of the hired unit
		//
		// = 30 + the nation's reputation / 2 + 20 if racially homogenous
		//
		//----------------------------------------//

		int unitLoyalty = 30 + (int)nation.Reputation / 2;

		if (unit.RaceId == nation.RaceId)
			unitLoyalty += 20;

		unitLoyalty = Math.Max(40, unitLoyalty);
		unitLoyalty = Math.Min(100, unitLoyalty);

		if (unit.SpyId != 0)
			SpyArray[unit.SpyId].SpyLoyalty = unitLoyalty;
		else
			unit.Loyalty = unitLoyalty;

		//---- remove the record from the hire list ----//

		DelInnUnit(innUnitId);

		return unitId;
	}

	public int HireRemote(int unitType, int combatLevel, int skillId, int skillLevel, int hireCost, int spyId)
	{
		//TODO multiplayer
		int recNo;

		for (recNo = 1; recNo <= InnUnits.Count; recNo++)
		{
			InnUnit innUnit = InnUnits[recNo - 1];
			if (innUnit.UnitType != unitType)
				continue;
			if (innUnit.Skill.CombatLevel != combatLevel)
				continue;
			if (innUnit.Skill.SkillId != skillId)
				continue;
			if (innUnit.Skill.SkillLevel != skillLevel)
				continue;
			if (innUnit.HireCost != hireCost)
				continue;
			if (innUnit.SpyId != spyId)
				continue;
			break;
		}

		if (recNo > InnUnits.Count) // this may happen in a multiplayer game
			return 0;

		return Hire(recNo);
	}

	public override void AutoDefense(int targetId)
	{
		foreach (Town town in TownArray)
		{
			if (town.NationId != NationId)
				continue;
			
			if (Misc.FirmTownDistance(this, town) <= InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE)
				town.AutoDefense(targetId);
		}
	}

	public override void DrawDetails(IRenderer renderer)
	{
		renderer.DrawInnDetails(this);
	}

	public override void HandleDetailsInput(IRenderer renderer)
	{
		renderer.HandleInnDetailsInput(this);
	}
	
	#region Old AI Functions

	public override void ProcessAI()
	{
		if (Info.TotalDays % 30 == FirmId % 30)
		{
			if (ThinkDel())
				return;
		}

		if (Info.TotalDays % 30 == FirmId % 30)
		{
			ThinkHireSpy();
			ThinkHireGeneral();
		}
	}
	
	private bool ThinkDel()
	{
		Nation ownNation = NationArray[NationId];

		if (ownNation.Cash < 500.0 + 500.0 * ownNation.pref_cash_reserve / 100.0 && ownNation.Profit365Days() < 0)
		{
			AIDelFirm();
			return true;
		}

		// if the current number of inns is more than the number the nation can support plus 2, then destroy the current one
		if (ownNation.ai_inn_array.Count > ownNation.ai_supported_inn_count() + 2)
		{
			AIDelFirm();
			return true;
		}

		//-------- delete it if it is near no base town ------//

		foreach (Town town in TownArray)
		{
			if (town.NationId == NationId)
			{
				if (Misc.RectsDistance(town.LocX1, town.LocY1, town.LocX2, town.LocY2,
					    LocX1, LocY1, LocX2, LocY2) <= InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE)
				{
					return false;
				}
			}
		}

		AIDelFirm();
		return true;
	}

	private bool ThinkHireSpy()
	{
		Nation ownNation = NationArray[NationId];

		if (!ownNation.ai_should_create_new_spy(0)) //0 means take into account all spies
			return false;

		//--------------------------------------------//

		for (int i = 0; i < InnUnits.Count; i++)
		{
			InnUnit innUnit = InnUnits[i];
			if (innUnit.Skill.SkillId != Skill.SKILL_SPYING)
				continue;

			int raceId = UnitRes[innUnit.UnitType].RaceId;
			int locX1 = 0;
			int locY1 = 0;
			int cloakedNationRecno = 0;
			if (ownNation.think_spy_new_mission(raceId, RegionId, out locX1, out locY1, out cloakedNationRecno))
			{
				int unitRecno = Hire(i + 1);
				if (unitRecno != 0)
				{
					ownNation.ai_start_spy_new_mission(UnitArray[unitRecno], locX1, locY1, cloakedNationRecno);
					return true;
				}
			}
		}

		return false;
	}

	private bool ThinkAssignSpyTo(int raceId, int innUnitRecno)
	{
		return false;
	}

	private bool ThinkHireGeneral()
	{
		Nation ownNation = NationArray[NationId];

		if (!ownNation.ai_should_spend(ownNation.pref_military_development / 2))
			return false;

		//--------------------------------------------//

		for (int i = 0; i < InnUnits.Count; i++)
		{
			InnUnit innUnit = InnUnits[i];
			if (innUnit.Skill.SkillId != Skill.SKILL_LEADING)
				continue;

			int raceId = UnitRes[innUnit.UnitType].RaceId;

			if (ThinkAssignGeneralTo(raceId, innUnit))
				return true;
		}

		return false;
	}

	private bool ThinkAssignGeneralTo(int raceId, InnUnit innUnit)
	{
		Nation ownNation = NationArray[NationId];
		int bestRating = 30; // the new one needs to be at least 30 points better than the existing one
		FirmCamp bestCamp = null;

		//----- think about which camp to move to -----//

		for (int i = ownNation.ai_camp_array.Count - 1; i >= 0; i--)
		{
			FirmCamp firmCamp = (FirmCamp)FirmArray[ownNation.ai_camp_array[i]];

			if (firmCamp.RegionId != RegionId)
				continue;

			int curLeadership = firmCamp.cur_commander_leadership();
			int newLeadership = firmCamp.new_commander_leadership(raceId, innUnit.Skill.SkillLevel);

			int curRating = newLeadership - curLeadership;

			//-------------------------------------//

			if (curRating > bestRating)
			{
				//--- if there is already somebody being assigned to it ---//

				if (ownNation.get_action(firmCamp.LocX1, firmCamp.LocY1,
					    -1, -1, Nation.ACTION_AI_ASSIGN_OVERSEER, FIRM_CAMP) != null)
				{
					continue;
				}

				bestRating = curRating;
				bestCamp = firmCamp;
			}
		}

		if (bestCamp == null)
			return false;

		//--------------------------------------------//

		int unitRecno = Hire(InnUnits.IndexOf(innUnit) + 1);

		ownNation.add_action(bestCamp.LocX1, bestCamp.LocY1, -1, -1,
			Nation.ACTION_AI_ASSIGN_OVERSEER, FIRM_CAMP, 1, unitRecno);

		return true;
	}

	#endregion
}