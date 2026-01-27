using System;

namespace TenKingdoms;

public class UnitGod : Unit
{
	public int GodId { get; set; }
	public int BaseFirmId { get; set; }
	public int CastPowerType { get; set; }

	private GodRes GodRes => Sys.Instance.GodRes;
	private MagicWeather MagicWeather => Sys.Instance.MagicWeather;
	private TornadoArray TornadoArray => Sys.Instance.TornadoArray;

	public override void Init(int unitType, int nationId, int rank, int unitLoyalty, int startLocX, int startLocY)
	{
		base.Init(unitType, nationId, rank, unitLoyalty, startLocX, startLocY);
		
		//TODO what about UNIT_PHOENIX?
		if (UnitType == UnitConstants.UNIT_PERSIAN_HEALER || UnitType == UnitConstants.UNIT_VIKING_GOD ||
		    UnitType == UnitConstants.UNIT_KUKULCAN || UnitType == UnitConstants.UNIT_JAPANESE_GOD)
			CanAttack = false; // unable to attack

		if (UnitType == UnitConstants.UNIT_EGYPTIAN_GOD || UnitType == UnitConstants.UNIT_INDIAN_GOD ||
		    UnitType == UnitConstants.UNIT_ZULU_GOD)
			CanAttack = false; // unable to attack
	}

	public override void PreProcess()
	{
		base.PreProcess();

		if (GodId != GodRes.GOD_CHINESE && GodId != GodRes.GOD_NORMAN) // only Chinese and Norman dragon can attack
			ForceMove = true;

		if (FirmArray.IsDeleted(BaseFirmId))
		{
			HitPoints = 0.0;
			SetDie();
			return;
		}

		//---- this unit consume pray points as it exists ----//

		FirmBase firmBase = (FirmBase)FirmArray[BaseFirmId];

		firmBase.PrayPoints -= GodRes[GodId].ExistPrayPoints / 200.0;

		if (firmBase.PrayPoints < 0.0)
			firmBase.PrayPoints = 0.0;

		HitPoints = firmBase.PrayPoints;

		if (HitPoints == 0.0)
			SetDie();
	}

	public override int ProcessAttack()
	{
		if (base.ProcessAttack() == 0) // return 1 if the unit just finished its current attack
			return 0;

		//------- consumer pray points --------//
		// The other gods have their prayer points consumed in UnitGod.CastPower().
		// See comments there for an explanation of the exploit this avoids.
		
		if (GodId == GodRes.GOD_CHINESE || GodId == GodRes.GOD_NORMAN)
			ConsumePowerPrayPoints();

		return 1;
	}

	public void CastPower(int locX, int locY)
	{
		if (FirmArray.IsDeleted(BaseFirmId))
			return;

		//------- consumer pray points --------//
		/* This must be done here to avoid an exploit where a human player 
		 * can cast an ability and then order the god to move within a 
		 * few frames and not consumer prayer points. This function is not
		 * used for the Chinese and Norman dragons so the function is also
		 * called in UnitGod.ProcessAttack() for the dragons.
		 */
		if (!ConsumePowerPrayPoints())
			return;

		//---- viking god does not need a range for casting power ----//

		if (GodId == GodRes.GOD_VIKING)
		{
			if (CastPowerType == 1)
				VikingSummonRain();
			else
				VikingSummonTornado();
			return;
		}

		//------ cast power on the selected area ------//

		GodInfo godInfo = GodRes[GodId];

		int locX1 = locX - godInfo.CastPowerRange + 1;
		int locY1 = locY - godInfo.CastPowerRange + 1;
		int locX2 = locX + godInfo.CastPowerRange - 1;
		int locY2 = locY + godInfo.CastPowerRange - 1;

		int centerY = (locY1 + locY2) / 2;

		for (locY = locY1; locY <= locY2; locY++)
		{
			int edgeX = Math.Abs(locY - centerY) / 2;

			for (locX = locX1 + edgeX; locX <= locX2 - edgeX; locX++)
			{
				if (Misc.IsLocationValid(locX, locY))
					CastOnLoc(locX, locY);
			}
		}
	}

	private bool ConsumePowerPrayPoints()
	{
		if (FirmArray.IsDeleted(BaseFirmId))
			return false;

		FirmBase firmBase = (FirmBase)FirmArray[BaseFirmId];

		firmBase.PrayPoints -= GodRes[GodId].PowerPrayPoints;

		if (firmBase.PrayPoints < 0.0)
			firmBase.PrayPoints = 0.0;

		HitPoints = firmBase.PrayPoints;

		return true;
	}

	private void CastOnLoc(int castLocX, int castLocY)
	{
		Location location = World.GetLoc(castLocX, castLocY);

		if (location.HasUnit(UnitConstants.UNIT_LAND))
		{
			CastOnUnit(location.UnitId(UnitConstants.UNIT_LAND), 1);
		}
		
		if (location.HasUnit(UnitConstants.UNIT_SEA))
		{
			Unit unit = UnitArray[location.UnitId(UnitConstants.UNIT_SEA)];

			//-- only heal human units belonging to our nation in ships --//

			if (unit.NationId == NationId && UnitRes[unit.UnitType].UnitClass == UnitConstants.UNIT_CLASS_SHIP)
			{
				UnitMarine unitMarine = (UnitMarine)unit;

				for (int i = 0; i < unitMarine.UnitsOnBoard.Count; i++)
				{
					int divider = 4; // the size of a ship is 4 locations (2x2)

					// the effects are weaken on ship units, only 50% of the original effects
					CastOnUnit(unitMarine.UnitsOnBoard[i], divider);
				}
			}
		}

		if (location.IsFirm())
		{
			Firm firm = FirmArray[location.FirmId()];
			int divider = (firm.LocX2 - firm.LocX1 + 1) * (firm.LocY2 - firm.LocY1 + 1);
			if (GodId == GodRes.GOD_ZULU)
				divider = 1; // range of zulu god is 1, no need to divide

			if (firm.OverseerId != 0)
			{
				CastOnUnit(firm.OverseerId, divider);
			}

			if (!FirmRes[firm.FirmType].LiveInTown)
			{
				foreach (var worker in firm.Workers)
				{
					CastOnWorker(worker, firm.NationId, divider);
				}
			}
		}

		if (location.IsTown())
		{
			Town town = TownArray[location.TownId()];

			if (GodId == GodRes.GOD_JAPANESE && town.NationId != NationId)
			{
				int divider = InternalConstants.TOWN_WIDTH * InternalConstants.TOWN_HEIGHT;

				for (int i = 0; i < GameConstants.MAX_RACE; i++)
				{
					if (town.RacesPopulation[i] == 0)
						continue;

					double changePoints = 7.0 + Misc.Random(8); // decrease 7 to 15 loyalty points instantly

					if (town.NationId != 0)
						town.ChangeLoyalty(i + 1, -changePoints / divider);
					else
						town.ChangeResistance(i + 1, NationId, -changePoints / divider);
				}
			}
			
			if (GodId == GodRes.GOD_EGYPTIAN && town.NationId == NationId)
			{
				for (int headCount = 5; headCount > 0 && town.Population < GameConstants.MAX_TOWN_POPULATION; headCount--)
				{
					int raceId = town.PickRandomRace(true, true);
					if (raceId != 0)
						town.IncPopulation(raceId, false, (int)town.RacesLoyalty[raceId - 1]);
				}
			}
		}
	}

	private void CastOnUnit(int unitId, int divider)
	{
		switch (GodId)
		{
			case GodRes.GOD_PERSIAN:
				PersianCastPower(unitId, divider);
				break;

			case GodRes.GOD_JAPANESE:
				JapaneseCastPower(unitId, divider);
				break;

			case GodRes.GOD_MAYA:
				MayaCastPower(unitId, divider);
				break;

			case GodRes.GOD_EGYPTIAN:
				EgyptianCastPower(unitId, divider);
				break;

			case GodRes.GOD_INDIAN:
				IndianCastPower(unitId, divider);
				break;

			case GodRes.GOD_ZULU:
				ZuluCastPower(unitId, divider);
				break;
		}
	}

	private void CastOnWorker(Worker worker, int nationId, int divider)
	{
		switch (GodId)
		{
			case GodRes.GOD_PERSIAN:
				PersianCastPower(worker, nationId, divider);
				break;

			case GodRes.GOD_JAPANESE:
				JapaneseCastPower(worker, nationId, divider);
				break;

			case GodRes.GOD_MAYA:
				MayaCastPower(worker, nationId, divider);
				break;

			case GodRes.GOD_EGYPTIAN:
				EgyptianCastPower(worker, nationId, divider);
				break;

			case GodRes.GOD_INDIAN:
				IndianCastPower(worker, nationId, divider);
				break;

			case GodRes.GOD_ZULU:
				ZuluCastPower(worker, nationId, divider);
				break;
		}
	}


	private void VikingSummonRain()
	{
		MagicWeather.cast_rain(10, 8); // 10 days, rain scale 8
		MagicWeather.cast_lightning(7); // 7 days
	}

	private void VikingSummonTornado()
	{
		int locX = NextLocX;
		int locY = NextLocY;
		int dir = FinalDir % 8;

		// put a tornado one location ahead
		if (dir == 7 || dir == 0 || dir == 1)
			if (locY > 0)
				locY--;
		if (dir >= 1 && dir <= 3)
			if (locX < GameConstants.MapSize - 1)
				locX++;
		if (dir >= 3 && dir <= 5)
			if (locY < GameConstants.MapSize - 1)
				locY++;
		if (dir >= 5 && dir <= 7)
			if (locX > 0)
				locX--;

		TornadoArray.AddTornado(locX, locY, 600);
		MagicWeather.cast_wind(10, 1, dir * 45); // 10 days
	}

	private void PersianCastPower(int unitId, int divider)
	{
		Unit unit = UnitArray[unitId];

		//-- only heal human units belonging to our nation --//

		if (unit.NationId == NationId && unit.RaceId > 0)
		{
			double changePoints = (double)unit.MaxHitPoints / (6.0 + Misc.Random(4)); // divided by (6 to 9)

			changePoints = Math.Max(changePoints, 10.0);

			unit.ChangeHitPoints(changePoints / (double)divider);
		}
	}

	private void JapaneseCastPower(int unitId, int divider)
	{
		Unit unit = UnitArray[unitId];

		//---- only cast on enemy units -----//

		if (unit.NationId != NationId && unit.RaceId > 0)
		{
			int changePoints = 7 + Misc.Random(8); // decrease 7 to 15 loyalty points instantly

			unit.ChangeLoyalty(-Math.Max(1, changePoints / divider));
		}
	}

	private void MayaCastPower(int unitId, int divider)
	{
		Unit unit = UnitArray[unitId];

		//---- only cast on mayan units belonging to our nation --//

		if (unit.NationId == NationId && unit.RaceId == (int)Race.RACE_MAYA)
		{
			int changePoints = 15 + Misc.Random(10); // add 15 to 25 points to its combat level instantly

			int newCombatLevel = unit.Skill.CombatLevel + Math.Max(1, changePoints / divider);

			if (newCombatLevel > 100)
				newCombatLevel = 100;

			double oldHitPoints = unit.HitPoints;

			unit.SetCombatLevel(newCombatLevel);

			unit.HitPoints = oldHitPoints; // keep the hit points unchanged.
		}
	}

	private void EgyptianCastPower(int unitId, int divider)
	{
		// no effect
	}

	private void IndianCastPower(int unitId, int divider)
	{
		Unit unit = UnitArray[unitId];

		if (unit.IsVisible() && NationArray.ShouldAttack(NationId, unit.NationId))
		{
			unit.ChangeLoyalty(-30 + Misc.Random(11));
		}
	}

	private void ZuluCastPower(int unitId, int divider)
	{
		Unit unit = UnitArray[unitId];

		if (NationId == unit.NationId && unit.RaceId == (int)Race.RACE_ZULU && unit.Rank != RANK_SOLDIER)
		{
			int changePoints = 30; // add 15 twice to avoid 130 becomes -126
			unit.Skill.SkillLevel += changePoints / divider;
			if (unit.Skill.SkillLevel > 100)
				unit.Skill.SkillLevel = 100;
		}
	}

	private void PersianCastPower(Worker worker, int nationId, int divider)
	{
		//-- only heal human units belonging to our nation --//

		if (nationId == NationId && worker.RaceId > 0)
		{
			int changePoints = worker.MaxHitPoints() / (4 + Misc.Random(4)); // divided by (4 to 7)

			changePoints = Math.Max(changePoints, 10);

			worker.ChangeHitPoints(Math.Max(1, changePoints / divider));
		}
	}

	private void JapaneseCastPower(Worker worker, int nationId, int divider)
	{
		//---- only cast on enemy units -----//

		if (nationId != NationId && worker.RaceId > 0)
		{
			int changePoints = 7 + Misc.Random(8); // decrease 7 to 15 loyalty points instantly

			worker.ChangeLoyalty(-Math.Max(1, changePoints / divider));
		}
	}

	private void MayaCastPower(Worker worker, int nationId, int divider)
	{
		//---- only cast on mayan units belonging to our nation --//

		if (nationId == NationId && worker.RaceId == (int)Race.RACE_MAYA)
		{
			int changePoints = 15 + Misc.Random(10); // add 15 to 25 points to its combat level instantly

			int newCombatLevel = worker.CombatLevel + Math.Max(1, changePoints / divider);

			if (newCombatLevel > 100)
				newCombatLevel = 100;

			worker.CombatLevel = newCombatLevel;
		}
	}

	private void EgyptianCastPower(Worker worker, int nationId, int divider)
	{
		// no effect
	}

	private void IndianCastPower(Worker worker, int nationId, int divider)
	{
		// no effect
	}

	private void ZuluCastPower(Worker worker, int nationId, int divider)
	{
		// no effect
	}


	public override void DrawDetails(IRenderer renderer)
	{
		renderer.DrawGodDetails(this);
	}

	public override void HandleDetailsInput(IRenderer renderer)
	{
		renderer.HandleGodDetailsInput(this);
	}
	

	#region Old AI Functions

	public override void ProcessAI()
	{
		if (!IsAIAllStop())
			return;

		if (Info.TotalDays % 7 != SpriteId % 7)
			return;

		switch (GodId)
		{
			case GodRes.GOD_NORMAN:
				think_dragon();
				break;

			case GodRes.GOD_MAYA:
				think_maya_god();
				break;

			case GodRes.GOD_GREEK:
				think_phoenix();
				break;

			case GodRes.GOD_VIKING:
				think_viking_god();
				break;

			case GodRes.GOD_PERSIAN:
				think_persian_god();
				break;

			case GodRes.GOD_CHINESE:
				think_chinese_dragon();
				break;

			case GodRes.GOD_JAPANESE:
				think_japanese_god();
				break;

			case GodRes.GOD_EGYPTIAN:
				think_egyptian_god();
				break;

			case GodRes.GOD_INDIAN:
				think_indian_god();
				break;

			case GodRes.GOD_ZULU:
				think_zulu_god();
				break;
		}
	}

	private void think_dragon()
	{
		int targetXLoc, targetYLoc;

		if (think_god_attack_target(out targetXLoc, out targetYLoc))
			AttackFirm(targetXLoc, targetYLoc);
	}

	private void think_maya_god()
	{
		//------- there is no action, now think a new one ------//

		Nation ownNation = NationArray[NationId];
		int bestRating = 0;
		int targetXLoc = -1, targetYLoc = -1;

		for (int i = ownNation.ai_camp_array.Count - 1; i >= 0; i--)
		{
			Firm firm = FirmArray[ownNation.ai_camp_array[i]];

			int curRating = 0;

			if (firm.OverseerId != 0)
			{
				Unit unit = UnitArray[firm.OverseerId];

				if (unit.RaceId == (int)Race.RACE_MAYA && unit.Skill.CombatLevel < 100)
					curRating += 10;
			}


			for (int j = firm.Workers.Count - 1; j >= 0; j--)
			{
				Worker worker = firm.Workers[j];
				if (worker.RaceId == (int)Race.RACE_MAYA && worker.CombatLevel < 100)
					curRating += 5;
			}

			if (curRating > bestRating)
			{
				bestRating = curRating;
				targetXLoc = firm.LocCenterX;
				targetYLoc = firm.LocCenterY;
			}
		}

		//-------------------------------------//

		if (bestRating != 0)
		{
			GoCastPower(targetXLoc, targetYLoc, 1, InternalConstants.COMMAND_AI);
		}
	}

	private void think_phoenix()
	{
		int xLoc = Misc.Random(GameConstants.MapSize);
		int yLoc = Misc.Random(GameConstants.MapSize);

		MoveTo(xLoc, yLoc);
	}

	private void think_viking_god()
	{
		int targetXLoc, targetYLoc;

		if (think_god_attack_target(out targetXLoc, out targetYLoc))
		{
			GoCastPower(targetXLoc + 1, targetYLoc + 1, 2, InternalConstants.COMMAND_AI);
		}
	}

	private void think_persian_god()
	{
		//------- there is no action, now think a new one ------//

		Nation ownNation = NationArray[NationId];
		int bestRating = 0;
		int targetXLoc = -1, targetYLoc = -1;

		for (int i = ownNation.ai_camp_array.Count - 1; i >= 0; i--)
		{
			Firm firm = FirmArray[ownNation.ai_camp_array[i]];

			//----- calculate the injured rating of the camp ----//

			int totalHitPoints = 0;
			int totalMaxHitPoints = 0;

			for (int j = 0; j < firm.Workers.Count; j++)
			{
				Worker worker = firm.Workers[j];
				totalHitPoints += worker.HitPoints;
				totalMaxHitPoints += worker.MaxHitPoints();
			}

			if (totalMaxHitPoints == 0)
				continue;

			int curRating = 100 * (totalMaxHitPoints - totalHitPoints) / totalMaxHitPoints;

			//---- if the king is the commander of this camp -----//

			if (firm.OverseerId != 0 && UnitArray[firm.OverseerId].Rank == RANK_KING)
			{
				curRating += 20;
			}

			if (curRating > bestRating)
			{
				bestRating = curRating;
				targetXLoc = firm.LocCenterX;
				targetYLoc = firm.LocCenterY;
			}
		}

		//-------------------------------------//

		if (bestRating != 0)
		{
			GoCastPower(targetXLoc, targetYLoc, 1, InternalConstants.COMMAND_AI);
		}
	}

	private void think_chinese_dragon()
	{
		int targetXLoc, targetYLoc;

		if (think_god_attack_target(out targetXLoc, out targetYLoc))
			AttackFirm(targetXLoc, targetYLoc);
	}

	private void think_japanese_god()
	{
		//------- there is no action, now think a new one ------//

		Nation ownNation = NationArray[NationId];
		int bestRating = 0;
		int targetXLoc = -1, targetYLoc = -1;

		//------ think firm target --------//

		if (Misc.Random(2) == 0)
		{
			foreach (Firm firm in FirmArray)
			{
				//------- only cast on camps ---------//

				if (firm.FirmType != Firm.FIRM_CAMP)
					continue;

				//------ only cast on hostile and tense nations ------//

				if (ownNation.GetRelation(firm.NationId).Status > NationBase.NATION_TENSE)
					continue;

				//------ calculate the rating of the firm -------//

				int curRating = ((FirmCamp)firm).TotalCombatLevel() / 10;

				if (curRating > bestRating)
				{
					bestRating = curRating;
					targetXLoc = firm.LocCenterX;
					targetYLoc = firm.LocCenterY;
				}
			}
		}
		else
		{
			foreach (Town town in TownArray)
			{
				//------ only cast on hostile and tense nations ------//

				if (town.NationId != 0 &&
				    ownNation.GetRelation(town.NationId).Status > NationBase.NATION_TENSE)
					continue;

				//------ calculate the rating of the firm -------//

				int curRating = town.Population + (100 - town.AverageLoyalty());

				if (curRating > bestRating)
				{
					bestRating = curRating;
					targetXLoc = town.LocCenterX;
					targetYLoc = town.LocCenterY;
				}
			}
		}

		//-------------------------------------//

		if (bestRating != 0)
		{
			GoCastPower(targetXLoc, targetYLoc, 1, InternalConstants.COMMAND_AI);
		}
	}

	private void think_egyptian_god()
	{
		int bestRating = 0;
		int targetXLoc = -1, targetYLoc = -1;
		const int MAX_TOWN_POP = GameConstants.MAX_TOWN_POPULATION;

		foreach (Town town in TownArray)
		{
			//------ only cast on own nations ------//

			if (town.NationId != NationId)
				continue;

			//------ calculate the rating of the firm -------//

			if (town.Population > MAX_TOWN_POP - 5)
				continue;

			// maximize the total loyalty gain.
			int curRating = 5 * town.AverageLoyalty();

			// calc rating on the number of people
			if (town.Population >= MAX_TOWN_POP / 2)
				curRating -= (town.Population - MAX_TOWN_POP / 2) * 300 / MAX_TOWN_POP;
			else
				curRating -= (MAX_TOWN_POP / 2 - town.Population) * 300 / MAX_TOWN_POP;

			if (curRating > bestRating)
			{
				bestRating = curRating;
				targetXLoc = town.LocCenterX;
				targetYLoc = town.LocCenterY;
			}
		}

		//-------------------------------------//

		if (bestRating != 0)
		{
			GoCastPower(targetXLoc, targetYLoc, 1, InternalConstants.COMMAND_AI);
		}
	}

	private void think_indian_god()
	{
		Nation ownNation = NationArray[NationId];

		// see if any unit near by

		int castRadius = 2;
		int leftLocX = NextLocX - castRadius;
		if (leftLocX < 0)
			leftLocX = 0;

		int rightLocX = NextLocX + castRadius;
		if (rightLocX >= GameConstants.MapSize)
			rightLocX = GameConstants.MapSize - 1;

		int topLocY = NextLocY - castRadius;
		if (topLocY < 0)
			topLocY = 0;

		int bottomLocY = NextLocY + castRadius;
		if (bottomLocY >= GameConstants.MapSize)
			bottomLocY = GameConstants.MapSize - 1;

		int curRating = 0;
		int xLoc = -1;
		int yLoc = -1;
		for (yLoc = topLocY; yLoc <= bottomLocY; ++yLoc)
		{
			for (xLoc = leftLocX; xLoc <= rightLocX; ++xLoc)
			{
				Location location = World.GetLoc(xLoc, yLoc);
				int unitRecno;
				Unit unit;
				if (location.HasUnit(UnitConstants.UNIT_LAND)
				    && (unitRecno = location.UnitId(UnitConstants.UNIT_LAND)) != 0
				    && !UnitArray.IsDeleted(unitRecno)
				    && (unit = UnitArray[unitRecno]) != null

				    && unit.NationId != 0 // don't affect independent unit
				    && unit.NationId != NationId
				    && (unit.Loyalty >= 20 && unit.Loyalty <= 60 ||
				        unit.Loyalty <= 80 && unit.TargetLoyalty < 30))
				{
					switch (ownNation.GetRelation(unit.NationId).Status)
					{
						case NationBase.NATION_HOSTILE:
							curRating += 3;
							break;

						case NationBase.NATION_TENSE:
						case NationBase.NATION_NEUTRAL:
							// curRating += 0;		// unchange
							break;

						case NationBase.NATION_FRIENDLY:
							curRating -= 1; // actually friendly humans are not affected
							break;

						case NationBase.NATION_ALLIANCE:
							curRating -= 1; // actually allied humans are not affected
							break;
					}
				}
			}
		}

		if (curRating > 1)
		{
			// if enemy unit come near, cast
			GoCastPower(NextLocX, NextLocY, 1, InternalConstants.COMMAND_AI);
		}
		else
		{
			// find any unit suitable, go to that area first
			int bestUnitCost = Int16.MaxValue;
			foreach (Unit unit in UnitArray)
			{
				// don't affect independent unit
				if (unit.IsVisible() && unit.MobileType == UnitConstants.UNIT_LAND &&
				    unit.NationId != 0 && unit.NationId != NationId &&
				    (unit.Loyalty >= 20 && unit.Loyalty <= 60 || unit.Loyalty <= 80 && unit.TargetLoyalty < 30) &&
				    ownNation.GetRelation(unit.NationId).Status == NationBase.NATION_HOSTILE)
				{
					int cost = Misc.points_distance(NextLocX, NextLocY, unit.NextLocX, unit.NextLocY);
					if (cost < bestUnitCost)
					{
						bestUnitCost = cost;
						xLoc = unit.NextLocX;
						yLoc = unit.NextLocY;
					}
				}
			}

			if (bestUnitCost < 100)
			{
				if (Misc.points_distance(NextLocX, NextLocY, xLoc, yLoc) <= GodRes[GodId].CastPowerRange)
					GoCastPower(xLoc, yLoc, 1, InternalConstants.COMMAND_AI);
				else
					MoveTo(xLoc, yLoc);
			}
			else if (Misc.Random(4) == 0)
			{
				// move to a near random location
				xLoc = NextLocX + Misc.Random(100) - 50;
				if (xLoc < 0)
					xLoc = 0;
				if (xLoc >= GameConstants.MapSize)
					xLoc = GameConstants.MapSize - 1;
				yLoc = NextLocY + Misc.Random(100) - 50;
				if (yLoc < 0)
					yLoc = 0;
				if (yLoc >= GameConstants.MapSize)
					yLoc = GameConstants.MapSize - 1;
				MoveTo(xLoc, yLoc);
			}
		}
	}

	private void think_zulu_god()
	{
		//------- there is no action, now think a new one ------//

		Nation ownNation = NationArray[NationId];
		int bestRating = 0;
		int targetXLoc = -1, targetYLoc = -1;

		for (int i = ownNation.ai_camp_array.Count - 1; i >= 0; i--)
		{
			Firm firm = FirmArray[ownNation.ai_camp_array[i]];

			int curRating = 0;

			Unit unit;
			if (firm.OverseerId != 0
			    && (unit = UnitArray[firm.OverseerId]) != null
			    && unit.RaceId == (int)Race.RACE_ZULU // only consider ZULU leader
			    && unit.Skill.SkillLevel <= 70)
			{
				if (unit.Rank == RANK_KING)
					curRating += 5000; // weak king need leadership very much

				if (unit.Skill.SkillLevel >= 40)
					curRating += 5000 - (unit.Skill.SkillLevel - 40) * 60; // strong leader need not be enhanced
				else
					curRating += 5000 - (40 - unit.Skill.SkillLevel) * 80; // don't add weak leader

				// calculate the benefits to his soldiers
				for (int j = firm.Workers.Count - 1; j >= 0; j--)
				{
					Worker worker = firm.Workers[j];
					if (worker.RaceId == (int)Race.RACE_ZULU)
						curRating += (unit.Skill.CombatLevel - worker.CombatLevel) * 2;
					else
						curRating += unit.Skill.CombatLevel - worker.CombatLevel;
				}

				if (curRating > bestRating)
				{
					bestRating = curRating;
					targetXLoc = firm.LocCenterX;
					targetYLoc = firm.LocCenterY;
				}
			}
		}

		//-------------------------------------//

		if (bestRating != 0)
		{
			GoCastPower(targetXLoc, targetYLoc, 1, InternalConstants.COMMAND_AI);
		}
	}

	private bool think_god_attack_target(out int targetXLoc, out int targetYLoc)
	{
		targetXLoc = -1;
		targetYLoc = -1;
		Nation ownNation = NationArray[NationId];

		foreach (Firm firm in FirmArray.EnumerateRandom())
		{
			if (firm.FirmType == Firm.FIRM_MONSTER)
				continue;

			//-------- only attack enemies ----------//

			if (ownNation.GetRelation(firm.NationId).Status != NationBase.NATION_HOSTILE)
				continue;

			//---- only attack enemy base and camp ----//

			if (firm.FirmType != Firm.FIRM_BASE && firm.FirmType != Firm.FIRM_CAMP)
				continue;

			//------- attack now --------//

			targetXLoc = firm.LocX1;
			targetYLoc = firm.LocY1;

			return true;
		}

		//----- if there is no enemy to attack, attack Fryhtans ----//

		foreach (Firm firm in FirmArray.EnumerateRandom())
		{
			if (firm.FirmType == Firm.FIRM_MONSTER)
			{
				targetXLoc = firm.LocX1;
				targetYLoc = firm.LocY1;

				return true;
			}
		}

		//---------------------------------------------------//

		return false;
	}
	
	#endregion
}