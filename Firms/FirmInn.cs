using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class InnUnit
{
	public int unit_id;
	public Skill skill = new Skill();
	public int hire_cost;
	public int stay_count; // how long this unit is going to stay until it leaves the inn if you do not hire him.
	public int spy_recno; // >0 if this unit is a spy

	public void set_hire_cost()
	{
		hire_cost = skill.CombatLevel + skill.SkillLevel * 2;

		if (skill.SkillId == Skill.SKILL_LEADING) // the cost of a leader unit is higher
		{
			hire_cost += skill.SkillLevel * 2;

			// increase the hiring cost with bigger steps when the skill level gets higher
			for (int i = 10; i <= 100; i += 20)
			{
				if (i > skill.SkillLevel)
					hire_cost += i / 5;
			}
		}
		else if (skill.SkillId == Skill.SKILL_SPYING) // the cost of a spy unit is higher
		{
			hire_cost += skill.SkillLevel;
		}

		hire_cost *= 2;
	}
}

public class FirmInn : Firm
{
	public int next_skill_id; // the skill id. of the next available unit

	public List<InnUnit> inn_unit_array = new List<InnUnit>(GameConstants.MAX_INN_UNIT);

	public FirmInn()
	{
	}

	protected override void init_derived()
	{
		next_skill_id = Misc.Random(Skill.MAX_TRAINABLE_SKILL) + 1;
	}

	public override void next_day()
	{
		base.next_day();

		//------------ update the hire list ------------//

		int updateInterval = 10 + Info.year_passed * 2; // there will be less and less units to hire as the game passes

		if (Info.TotalDays % updateInterval == firm_recno % updateInterval)
			update_add_hire_list();

		if (Info.TotalDays % 10 == firm_recno % 10)
			update_del_hire_list();
	}

	public override void assign_unit(int unitRecno)
	{
		//------- if this is a construction worker -------//

		if (UnitArray[unitRecno].Skill.SkillId == Skill.SKILL_CONSTRUCTION)
		{
			set_builder(unitRecno);
		}
	}

	public int hire(int recNo)
	{
		//--------- first check if you have enough money to hire ------//

		Nation nation = NationArray[nation_recno];

		InnUnit innUnit = inn_unit_array[recNo - 1];

		if (nation.cash < innUnit.hire_cost)
			return 0;

		//---------- add the unit now -----------//

		int unitRecno = create_unit(innUnit.unit_id);
		if (unitRecno == 0)
			return 0; // no space for creating the unit

		nation.add_expense(NationBase.EXPENSE_HIRE_UNIT, innUnit.hire_cost);

		//-------- set skills of the unit --------//

		Unit unit = UnitArray[unitRecno];
		unit.Skill.SkillId = innUnit.skill.SkillId;
		unit.Skill.SkillLevel = innUnit.skill.SkillLevel;
		unit.SetCombatLevel(innUnit.skill.CombatLevel);

		//-------- if the unit's skill is spying -----//

		if (unit.Skill.SkillId == Skill.SKILL_SPYING)
		{
			Spy spy = SpyArray.AddSpy(unitRecno, unit.Skill.SkillLevel);
			unit.SpyId = spy.spy_recno;
			unit.Skill.SkillId = 0; // reset its primary skill, its spying skill has been recorded in spy_array
		}

		//----------------------------------------//
		//
		// Loyalty of the hired unit
		//
		// = 30 + the nation's reputation / 2 + 30 if racially homogenous
		//
		//----------------------------------------//

		int unitLoyalty = 30 + (int)nation.reputation / 2;

		if (RaceRes.is_same_race(unit.RaceId, nation.race_id))
			unitLoyalty += 20;

		unitLoyalty = Math.Max(40, unitLoyalty);
		unitLoyalty = Math.Min(100, unitLoyalty);

		if (unit.SpyId != 0)
			SpyArray[unit.SpyId].spy_loyalty = unitLoyalty;
		else
			unit.Loyalty = unitLoyalty;

		//---- remove the record from the hire list ----//

		del_inn_unit(recNo);

		if (firm_recno == FirmArray.selected_recno && nation_recno == NationArray.player_recno)
		{
			//TODO drawing
			//put_info(INFO_UPDATE);
		}

		return unitRecno;
	}

	public int hire_remote(int unitId, int combat_level, int skill_id, int skill_level, int hire_cost, int spy_recno)
	{
		//TODO multiplayer
		int recNo;

		for (recNo = 1; recNo <= inn_unit_array.Count; recNo++)
		{
			InnUnit innUnit = inn_unit_array[recNo - 1];
			if (innUnit.unit_id != unitId)
				continue;
			if (innUnit.skill.CombatLevel != combat_level)
				continue;
			if (innUnit.skill.SkillId != skill_id)
				continue;
			if (innUnit.skill.SkillLevel != skill_level)
				continue;
			if (innUnit.hire_cost != hire_cost)
				continue;
			if (innUnit.spy_recno != spy_recno)
				continue;
			break;
		}

		if (recNo > inn_unit_array.Count) // this may happen in a multiplayer game
			return 0;

		return hire(recNo);
	}

	public override void auto_defense(int targetRecno)
	{
		//---------- the area to check -----------//
		int xLoc1 = center_x - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE;
		int yLoc1 = center_y - InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE;
		int xLoc2 = center_x + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE;
		int yLoc2 = center_y + InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE;

		//----------- boundary checking ----------//
		if (xLoc1 < 0)
			xLoc1 = 0;
		if (yLoc1 < 0)
			yLoc1 = 0;
		if (xLoc2 >= GameConstants.MapSize)
			xLoc2 = GameConstants.MapSize - 1;
		if (yLoc2 >= GameConstants.MapSize)
			yLoc2 = GameConstants.MapSize - 1;

		int skipWidthDist = InternalConstants.TOWN_WIDTH;
		int skipHeightDist = InternalConstants.TOWN_HEIGHT;

		//--------------------------------------------------//
		// check for our town in the effective area
		//--------------------------------------------------//
		int xLimit = xLoc2 + skipWidthDist - 1;
		int yLimit = yLoc2 + skipHeightDist - 1;
		for (int xEnd = 0, i = xLoc1; i <= xLimit && xEnd == 0; i += skipWidthDist)
		{
			if (i >= xLoc2)
			{
				xEnd++; // final
				i = xLoc2;
			}

			for (int yEnd = 0, j = yLoc1; j <= yLimit && yEnd == 0; j += skipHeightDist)
			{
				if (j >= yLoc2)
				{
					yEnd++; // final
					j = yLoc2;
				}

				Location location = World.GetLoc(i, j);
				if (!location.IsTown())
					continue;

				Town town = TownArray[location.TownId()];

				if (town.NationId != nation_recno)
					continue;

				int dist = Misc.rects_distance(loc_x1, loc_y1, loc_x2, loc_y2,
					town.LocX1, town.LocY1, town.LocX2, town.LocY2);
				if (dist <= InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE)
					town.AutoDefense(targetRecno);
			}
		}
	}

	public override void process_ai()
	{
		if (Info.TotalDays % 30 == firm_recno % 30)
		{
			if (think_del())
				return;
		}

		if (Info.TotalDays % 30 == firm_recno % 30)
		{
			think_hire_spy();
			think_hire_general();
		}
	}

	private bool should_add_inn_unit()
	{
		int totalInnUnit = inn_unit_array.Count;

		for (int i = 0; i < linked_firm_array.Count; i++)
		{
			// links between inns are stored in linked_firm_array[] for quick scan only
			FirmInn firmInn = (FirmInn)FirmArray[linked_firm_array[i]];

			totalInnUnit += firmInn.inn_unit_array.Count;
		}

		return totalInnUnit < GameConstants.MAX_INN_UNIT_PER_REGION;
	}

	private void add_inn_unit(int unitId)
	{
		InnUnit innUnit = new InnUnit();
		inn_unit_array.Add(innUnit);

		innUnit.unit_id = unitId;

		//--------- set the skill now -----------------//

		int skillId = next_skill_id;

		if (++next_skill_id > Skill.MAX_TRAINABLE_SKILL)
			next_skill_id = 1;

		innUnit.skill.SkillId = skillId;

		if (skillId > 0)
			innUnit.skill.SkillLevel = 30 + Misc.Random(70);
		else
			innUnit.skill.SkillLevel = 0;

		if (skillId == 0 || skillId == Skill.SKILL_LEADING)
			innUnit.skill.CombatLevel = 30 + Misc.Random(70);
		else
			innUnit.skill.CombatLevel = 10;

		innUnit.set_hire_cost();

		innUnit.stay_count = 5 + Misc.Random(5);

		innUnit.spy_recno = 0;
	}

	private void del_inn_unit(int recNo)
	{
		inn_unit_array.RemoveAt(recNo - 1);
	}

	private void update_add_hire_list()
	{
		//-------- new units come by --------//

		if (inn_unit_array.Count < GameConstants.MAX_INN_UNIT)
		{
			if (should_add_inn_unit())
			{
				int unitId = RaceRes[ConfigAdv.GetRandomRace()].basic_unit_id;

				if (unitId != 0)
					add_inn_unit(unitId);
			}
		}
	}

	private void update_del_hire_list()
	{
		//------- existing units leave -------//

		for (int i = inn_unit_array.Count - 1; i >= 0; i--)
		{
			if (inn_unit_array[i].spy_recno == 0 && --inn_unit_array[i].stay_count == 0)
			{
				del_inn_unit(i + 1);

				if (firm_recno == FirmArray.selected_recno && should_show_info())
				{
					//TODO drawing
					//if( browse_hire.recno() > i && browse_hire.recno() > 1 )
					//browse_hire.refresh( browse_hire.recno()-1, inn_unit_count );
				}
			}
		}
	}

	//-------- AI actions ---------//

	private bool think_del()
	{
		Nation ownNation = NationArray[nation_recno];

		if (ownNation.cash < 500.0 + 500.0 * ownNation.pref_cash_reserve / 100.0 && ownNation.profit_365days() < 0)
		{
			ai_del_firm();
			return true;
		}

		// if the current number of inns is more than the number the nation can support plus 2, then destroy the current one
		if (ownNation.ai_inn_array.Count > ownNation.ai_supported_inn_count() + 2)
		{
			ai_del_firm();
			return true;
		}

		//-------- delete it if it is near no base town ------//

		foreach (Town town in TownArray)
		{
			if (town.NationId == nation_recno)
			{
				if (Misc.rects_distance(town.LocX1, town.LocY1, town.LocX2, town.LocY2,
					    loc_x1, loc_y1, loc_x2, loc_y2) <= InternalConstants.EFFECTIVE_FIRM_TOWN_DISTANCE)
				{
					return false;
				}
			}
		}

		ai_del_firm();
		return true;
	}

	private bool think_hire_spy()
	{
		Nation ownNation = NationArray[nation_recno];

		if (!ownNation.ai_should_create_new_spy(0)) //0 means take into account all spies
			return false;

		//--------------------------------------------//

		for (int i = 0; i < inn_unit_array.Count; i++)
		{
			InnUnit innUnit = inn_unit_array[i];
			if (innUnit.skill.SkillId != Skill.SKILL_SPYING)
				continue;

			int raceId = UnitRes[innUnit.unit_id].race_id;
			int loc_x1 = 0;
			int loc_y1 = 0;
			int cloakedNationRecno = 0;
			if (ownNation.think_spy_new_mission(raceId, region_id, out loc_x1, out loc_y1, out cloakedNationRecno))
			{
				int unitRecno = hire(i + 1);
				if (unitRecno != 0)
				{
					ownNation.ai_start_spy_new_mission(UnitArray[unitRecno], loc_x1, loc_y1, cloakedNationRecno);
					return true;
				}
			}
		}

		return false;
	}

	private bool think_assign_spy_to(int raceId, int innUnitRecno)
	{
		return false;
	}

	private bool think_hire_general()
	{
		Nation ownNation = NationArray[nation_recno];

		if (!ownNation.ai_should_spend(ownNation.pref_military_development / 2))
			return false;

		//--------------------------------------------//

		for (int i = 0; i < inn_unit_array.Count; i++)
		{
			InnUnit innUnit = inn_unit_array[i];
			if (innUnit.skill.SkillId != Skill.SKILL_LEADING)
				continue;

			int raceId = UnitRes[innUnit.unit_id].race_id;

			if (think_assign_general_to(raceId, innUnit))
				return true;
		}

		return false;
	}

	private bool think_assign_general_to(int raceId, InnUnit innUnit)
	{
		Nation ownNation = NationArray[nation_recno];
		int bestRating = 30; // the new one needs to be at least 30 points better than the existing one
		FirmCamp bestCamp = null;

		//----- think about which camp to move to -----//

		for (int i = ownNation.ai_camp_array.Count - 1; i >= 0; i--)
		{
			FirmCamp firmCamp = (FirmCamp)FirmArray[ownNation.ai_camp_array[i]];

			if (firmCamp.region_id != region_id)
				continue;

			int curLeadership = firmCamp.cur_commander_leadership();
			int newLeadership = firmCamp.new_commander_leadership(raceId, innUnit.skill.SkillLevel);

			int curRating = newLeadership - curLeadership;

			//-------------------------------------//

			if (curRating > bestRating)
			{
				//--- if there is already somebody being assigned to it ---//

				if (ownNation.get_action(firmCamp.loc_x1, firmCamp.loc_y1,
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

		int unitRecno = hire(inn_unit_array.IndexOf(innUnit) + 1);

		ownNation.add_action(bestCamp.loc_x1, bestCamp.loc_y1, -1, -1,
			Nation.ACTION_AI_ASSIGN_OVERSEER, FIRM_CAMP, 1, unitRecno);

		return true;
	}

	public override void DrawDetails(IRenderer renderer)
	{
		renderer.DrawInnDetails(this);
	}

	public override void HandleDetailsInput(IRenderer renderer)
	{
		renderer.HandleInnDetailsInput(this);
	}
}