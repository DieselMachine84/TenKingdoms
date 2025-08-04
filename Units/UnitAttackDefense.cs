using System;

namespace TenKingdoms;

public partial class Unit
{
	private void ProcessAttackUnit()
	{
		if (!CanAttack)
		{
			Stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//------- if the targeted unit has been destroyed --------//
		if (ActionParam == 0)
			return;

		//--------------------------------------------------------------------------//
		// stop if the targeted unit has been killed or target belongs our nation
		//--------------------------------------------------------------------------//
		int clearOrder = 0;
		Unit targetUnit = null;

		if (UnitArray.IsDeleted(ActionParam) || ActionParam == SpriteId)
		{
			if (!ConfigAdv.unit_finish_attack_move || CurAction == SPRITE_ATTACK)
				clearOrder++;
			else
			{
				// keep attack action alive to finish movement before going idle
				InvalidateAttackTarget();
				return;
			}
		}
		else
		{
			targetUnit = UnitArray[ActionParam];
			if (targetUnit.NationId != 0 && !CanAttackNation(targetUnit.NationId) && targetUnit.UnitType != UnitConstants.UNIT_EXPLOSIVE_CART)
				clearOrder++;
		}

		if (clearOrder != 0)
		{
			//------------------------------------------------------------//
			// change to detect target if in defense mode
			//------------------------------------------------------------//
			/*if(action_mode2==UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET || action_mode2==ACTION_DEFEND_TOWN_ATTACK_TARGET ||
				action_mode2==ACTION_MONSTER_DEFEND_ATTACK_TARGET)
			{
				stop2(UnitConstants.KEEP_DEFENSE_MODE);
				err_when((action_misc!=UnitConstants.ACTION_MISC_DEFENSE_CAMP_RECNO && action_misc!=UnitConstants.ACTION_MISC_DEFEND_TOWN_RECNO &&
							 action_misc!=UnitConstants.ACTION_MISC_MONSTER_DEFEND_FIRM_RECNO) || !action_misc_para);
				return;
			}
	
			stop2(); // clear order
			err_when(cur_action==SPRITE_ATTACK && (move_to_x_loc!=next_x_loc() || move_to_y_loc!=next_y_loc()));
			err_when(action_mode==ACTION_ATTACK_UNIT && !action_para);
			err_when(cur_action==SPRITE_ATTACK && action_mode==UnitConstants.ACTION_STOP);
			return;*/

			Stop2(UnitConstants.KEEP_DEFENSE_MODE);

			return;
		}

		//--------------------------------------------------------------------------//
		// stop action if target goes into town/firm, ships (go to other territory)
		//--------------------------------------------------------------------------//
		if (!targetUnit.IsVisible())
		{
			Stop2(UnitConstants.KEEP_DEFENSE_MODE); // clear order
			return;
		}

		//------------------------------------------------------------//
		// if the caravan is entered a firm, attack the firm
		//------------------------------------------------------------//
		if (targetUnit.IsCaravanInsideFirm())
		{
			//----- for caravan entering the market -------//
			// the current firm recno of the firm the caravan entered is stored in action_para
			if (FirmArray.IsDeleted(targetUnit.ActionParam))
				Stop2(UnitConstants.KEEP_DEFENSE_MODE); // clear order
			else
			{
				Firm firm = FirmArray[targetUnit.ActionParam];
				AttackFirm(firm.loc_x1, firm.loc_y1);
			}

			return;
		}

		//---------------- define parameters ---------------------//
		int targetXLoc = targetUnit.NextLocX;
		int targetYLoc = targetUnit.NextLocY;
		int spriteXLoc = NextLocX;
		int spriteYLoc = NextLocY;
		AttackInfo attackInfo = AttackInfos[CurAttack];

		//------------------------------------------------------------//
		// If this unit's target has moved, change the destination accordingly.
		//------------------------------------------------------------//
		if (targetXLoc != ActionLocX || targetYLoc != ActionLocY)
		{
			TargetMove(targetUnit);
			if (ActionMode == UnitConstants.ACTION_STOP)
				return;
		}

		//-----------------------------------------------------//
		// If the unit is currently attacking somebody.
		//-----------------------------------------------------//
		//if( cur_action==SPRITE_ATTACK && next_x==cur_x && next_y==cur_y)

		if (Math.Abs(CurX - NextX) <= SpriteInfo.Speed && Math.Abs(CurY - NextY) <= SpriteInfo.Speed)
		{
			if (CurAction == SPRITE_ATTACK)
			{
				AttackTarget(targetUnit);
			}
			else
			{
				//-----------------------------------------------------//
				// If the unit is on its way to attack somebody and it
				// has got close next to the target, attack now
				//-----------------------------------------------------//
				OnWayToAttack(targetUnit);
			}
		}
	}

	private void ProcessAttackFirm()
	{
		if (!CanAttack)
		{
			Stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//------- if the targeted firm has been destroyed --------//
		if (ActionParam == 0)
			return;

		Firm targetFirm = null;
		int clearOrder = 0;
		//------------------------------------------------------------//
		// check attack conditions
		//------------------------------------------------------------//
		if (FirmArray.IsDeleted(ActionParam))
		{
			if (!ConfigAdv.unit_finish_attack_move || CurAction == SPRITE_ATTACK)
				clearOrder++;
			else
			{
				// keep attack action alive to finish movement before going idle
				InvalidateAttackTarget();
				return;
			}
		}
		else
		{
			targetFirm = FirmArray[ActionParam];

			if (!CanAttackNation(targetFirm.nation_recno)) // cannot attack this nation
				clearOrder++;
		}

		if (clearOrder != 0)
		{
			//------------------------------------------------------------//
			// change to detect target if in defend mode
			//------------------------------------------------------------//
			/*if(action_mode2==UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET || action_mode2==ACTION_DEFEND_TOWN_ATTACK_TARGET ||
				action_mode2==ACTION_MONSTER_DEFEND_ATTACK_TARGET)
			{
				err_when((action_misc!=UnitConstants.ACTION_MISC_DEFENSE_CAMP_RECNO && action_misc!=UnitConstants.ACTION_MISC_DEFEND_TOWN_RECNO &&
							 action_misc!=UnitConstants.ACTION_MISC_MONSTER_DEFEND_FIRM_RECNO) || !action_misc_para);
				stop2(UnitConstants.KEEP_DEFENSE_MODE);
				return;
			}
	
			err_when(action_mode2==ACTION_AUTO_DEFENSE_DETECT_TARGET || action_mode2==ACTION_AUTO_DEFENSE_BACK_CAMP ||
						action_mode2==ACTION_DEFEND_TOWN_DETECT_TARGET || action_mode2==ACTION_DEFEND_TOWN_BACK_TOWN ||
						action_mode2==ACTION_MONSTER_DEFEND_DETECT_TARGET || action_mode2==ACTION_MONSTER_DEFEND_BACK_FIRM);
	
			stop2(); // clear order
			err_when(cur_action==SPRITE_ATTACK && (move_to_x_loc!=next_x_loc() || move_to_y_loc!=next_y_loc()));
			err_when(action_mode==ACTION_ATTACK_FIRM && !action_para);
			err_when(cur_action==SPRITE_ATTACK && action_mode==UnitConstants.ACTION_STOP);
			return;*/

			Stop2(UnitConstants.KEEP_DEFENSE_MODE);

			return;
		}

		//-----------------------------------------------------//
		// If the unit is currently attacking somebody.
		//-----------------------------------------------------//
		if (CurAction == SPRITE_ATTACK)
		{
			if (RemainAttackDelay != 0)
				return;

			AttackInfo attackInfo = AttackInfos[CurAttack];
			if (attackInfo.attack_range > 1) // range attack
			{
				//--------- wait for bullet emit ----------//
				if (CurFrame != attackInfo.bullet_out_frame)
					return;

				//------- seek location to attack by bullet ----------//
				int curXLoc = NextLocX;
				int curYLoc = NextLocY;
				if (!BulletArray.bullet_path_possible(curXLoc, curYLoc, MobileType,
					    RangeAttackLocX, RangeAttackLocY, UnitConstants.UNIT_LAND,
					    attackInfo.bullet_speed, attackInfo.bullet_sprite_id))
				{
					FirmInfo firmInfo = FirmRes[targetFirm.firm_id];
					bool canAddBullet = BulletArray.add_bullet_possible(curXLoc, curYLoc, MobileType,
						ActionLocX, ActionLocY, UnitConstants.UNIT_LAND, firmInfo.loc_width, firmInfo.loc_height,
						out int resultLocX, out int resultLocY, attackInfo.bullet_speed, attackInfo.bullet_sprite_id);
					RangeAttackLocX = resultLocX;
					RangeAttackLocY = resultLocY;
					if (!canAddBullet)
					{
						//------- no suitable location, so move to target again ---------//
						SetMoveToSurround(ActionLocX, ActionLocY, firmInfo.loc_width, firmInfo.loc_height, UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO);
						return;
					}
				}

				//--------- add bullet, bullet emits ----------//
				BulletArray.AddBullet(this, ActionLocX, ActionLocY);
				AddCloseAttackEffect();

				// ------- reduce power --------//
				CurPower -= attackInfo.consume_power;
				if (CurPower < 0) // ***** BUGHERE
					CurPower = 0;
				SetRemainAttackDelay();
				return;
			}
			else // close attack
			{
				if (CurFrame != CurSpriteAttack().frameCount)
					return; // is attacking

				HitFirm(this, ActionLocX, ActionLocY, ActualDamage(), NationId);
				AddCloseAttackEffect();

				// ------- reduce power --------//
				CurPower -= attackInfo.consume_power;
				if (CurPower < 0) // ***** BUGHERE
					CurPower = 0;
				SetRemainAttackDelay();
			}
		}
		//--------------------------------------------------------------------------------------------------//
		// If the unit is on its way to attack somebody, if it has gotten close next to the target, attack now
		//--------------------------------------------------------------------------------------------------//
		// it has moved to the specified location. check cur_x & go_x to make sure the sprite has completely move to the location, not just crossing it.
		else if (Math.Abs(CurX - NextX) <= SpriteInfo.Speed && Math.Abs(CurY - NextY) <= SpriteInfo.Speed)
		{
			if (MobileType == UnitConstants.UNIT_LAND)
			{
				if (DetectSurroundTarget())
					return;
			}

			if (AttackRange == 1)
			{
				//------------------------------------------------------------//
				// for close attack, the unit unable to attack the firm if
				// it is not in the firm surrounding
				//------------------------------------------------------------//
				if (_pathNodeDistance > AttackRange)
					return;
			}

			FirmInfo firmInfo = FirmRes[targetFirm.firm_id];
			int targetXLoc = targetFirm.loc_x1;
			int targetYLoc = targetFirm.loc_y1;

			int attackDistance = CalcDistance(targetXLoc, targetYLoc, firmInfo.loc_width, firmInfo.loc_height);
			int curXLoc = NextLocX;
			int curYLoc = NextLocY;

			if (attackDistance <= AttackRange) // able to attack target
			{
				if ((attackDistance == 1) && AttackRange > 1) // often false condition is checked first
					ChooseBestAttackMode(1); // may change to use close attack

				if (AttackRange > 1) // use range attack
				{
					SetCur(NextX, NextY);

					AttackInfo attackInfo = AttackInfos[CurAttack];
					bool canAddBullet = BulletArray.add_bullet_possible(curXLoc, curYLoc, MobileType,
						targetXLoc, targetYLoc, UnitConstants.UNIT_LAND, firmInfo.loc_width, firmInfo.loc_height,
						out int resultLocX, out int resultLocY, attackInfo.bullet_speed, attackInfo.bullet_sprite_id);
					RangeAttackLocX = resultLocX;
					RangeAttackLocY = resultLocY;
					if (!canAddBullet)
					{
						//------- no suitable location, move to target ---------//
						if (PathNodes.Count  == 0) // no step for continue moving
							SetMoveToSurround(ActionLocX, ActionLocY, firmInfo.loc_width, firmInfo.loc_height, UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO);

						return; // unable to attack, continue to move
					}

					//---------- able to do range attack ----------//
					SetAttackDir(curXLoc, curYLoc, RangeAttackLocX, RangeAttackLocY);
					CurFrame = 1;

					if (IsDirCorrect())
						SetAttack();
					else
						SetTurn();
				}
				else // close attack
				{
					//---------- attack now ---------//
					SetCur(NextX, NextY);
					TerminateMove();

					if (targetFirm.firm_id != Firm.FIRM_RESEARCH)
						SetAttackDir(curXLoc, curYLoc, targetFirm.center_x, targetFirm.center_y);
					else // FIRM_RESEARCH with size 2x3
					{
						int hitXLoc = (curXLoc > targetFirm.loc_x1) ? targetFirm.loc_x2 : targetFirm.loc_x1;

						int hitYLoc;
						if (curYLoc < targetFirm.center_y)
							hitYLoc = targetFirm.loc_y1;
						else if (curYLoc == targetFirm.center_y)
							hitYLoc = targetFirm.center_y;
						else
							hitYLoc = targetFirm.loc_y2;

						SetAttackDir(curXLoc, curYLoc, hitXLoc, hitYLoc);
					}

					if (IsDirCorrect())
						SetAttack();
					else
						SetTurn();
				}

			}
		}
	}

	private void ProcessAttackTown()
	{
		if (!CanAttack)
		{
			Stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//------- if the targeted town has been destroyed --------//
		if (ActionParam == 0)
			return;

		Town targetTown = null;
		int clearOrder = 0;
		//------------------------------------------------------------//
		// check attack conditions
		//------------------------------------------------------------//
		if (TownArray.IsDeleted(ActionParam))
		{
			if (!ConfigAdv.unit_finish_attack_move || CurAction == SPRITE_ATTACK)
				clearOrder++;
			else
			{
				// keep attack action alive to finish movement before going idle
				InvalidateAttackTarget();
				return;
			}
		}
		else
		{
			targetTown = TownArray[ActionParam];

			if (!CanAttackNation(targetTown.NationId)) // cannot attack this nation
				clearOrder++;
		}

		if (clearOrder != 0)
		{
			//------------------------------------------------------------//
			// change to detect target if in defend mode
			//------------------------------------------------------------//
			/*if(action_mode2==UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET || action_mode2==ACTION_DEFEND_TOWN_ATTACK_TARGET ||
				action_mode2==ACTION_MONSTER_DEFEND_ATTACK_TARGET)
			{
				err_when((action_misc!=UnitConstants.ACTION_MISC_DEFENSE_CAMP_RECNO && action_misc!=UnitConstants.ACTION_MISC_DEFEND_TOWN_RECNO &&
							 action_misc!=UnitConstants.ACTION_MISC_MONSTER_DEFEND_FIRM_RECNO) || !action_misc_para);
				stop2(UnitConstants.KEEP_DEFENSE_MODE);
				return;
			}
	
			err_when(action_mode2==ACTION_AUTO_DEFENSE_DETECT_TARGET || action_mode2==ACTION_AUTO_DEFENSE_BACK_CAMP ||
						action_mode2==ACTION_DEFEND_TOWN_DETECT_TARGET || action_mode2==ACTION_DEFEND_TOWN_BACK_TOWN ||
						action_mode2==ACTION_MONSTER_DEFEND_DETECT_TARGET || action_mode2==ACTION_MONSTER_DEFEND_BACK_FIRM);
	
			stop2(); // clear order
			err_when(cur_action==SPRITE_ATTACK && (move_to_x_loc!=next_x_loc() || move_to_y_loc!=next_y_loc()));
			err_when(action_mode==ACTION_ATTACK_TOWN && !action_para);
			err_when(cur_action==SPRITE_ATTACK && action_mode==UnitConstants.ACTION_STOP);
			return;*/

			Stop2(UnitConstants.KEEP_DEFENSE_MODE);

			return;
		}

		//-----------------------------------------------------//
		// If the unit is currently attacking somebody.
		//-----------------------------------------------------//
		if (CurAction == SPRITE_ATTACK)
		{
			if (RemainAttackDelay != 0)
				return;

			AttackInfo attackInfo = AttackInfos[CurAttack];
			if (attackInfo.attack_range > 1) // range attack
			{
				//---------- wait for bullet emit ---------//
				if (CurFrame != attackInfo.bullet_out_frame)
					return;

				//------- seek location to attack target by bullet --------//
				int curXLoc = NextLocX;
				int curYLoc = NextLocY;
				if (!BulletArray.bullet_path_possible(curXLoc, curYLoc, MobileType, RangeAttackLocX,
					    RangeAttackLocY,
					    UnitConstants.UNIT_LAND, attackInfo.bullet_speed, attackInfo.bullet_sprite_id))
				{
					bool canAddBullet = BulletArray.add_bullet_possible(curXLoc, curYLoc, MobileType,
						ActionLocX, ActionLocY, UnitConstants.UNIT_LAND, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT,
						out int resultLocX, out int resultLocY, attackInfo.bullet_speed, attackInfo.bullet_sprite_id);
					RangeAttackLocX = resultLocX;
					RangeAttackLocY = resultLocY;
					if (!canAddBullet)
					{
						//----- no suitable location, move to target --------//
						SetMoveToSurround(ActionLocX, ActionLocY, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT,
							UnitConstants.BUILDING_TYPE_TOWN_MOVE_TO);
						return;
					}
				}

				//--------- add bullet, bullet emits --------//
				BulletArray.AddBullet(this, ActionLocX, ActionLocY);
				AddCloseAttackEffect();

				// ------- reduce power --------//
				CurPower -= attackInfo.consume_power;
				if (CurPower < 0) // ***** BUGHERE
					CurPower = 0;
				SetRemainAttackDelay();
				return;
			}
			else // close attack
			{
				if (CurFrame != CurSpriteAttack().frameCount)
					return; // attacking

				HitTown(this, ActionLocX, ActionLocY, ActualDamage(), NationId);
				AddCloseAttackEffect();

				// ------- reduce power --------//
				CurPower -= attackInfo.consume_power;
				if (CurPower < 0) // ***** BUGHERE
					CurPower = 0;
				SetRemainAttackDelay();
			}
		}
		//--------------------------------------------------------------------------------------------------//
		// If the unit is on its way to attack the town, if it has gotten close next to it, attack now
		//--------------------------------------------------------------------------------------------------//
		// it has moved to the specified location. check cur_x & go_x to make sure the sprite has completely move to the location, not just crossing it.
		else if (Math.Abs(CurX - NextX) <= SpriteInfo.Speed && Math.Abs(CurY - NextY) <= SpriteInfo.Speed)
		{
			if (MobileType == UnitConstants.UNIT_LAND)
			{
				if (DetectSurroundTarget())
					return;
			}

			if (AttackRange == 1)
			{
				//------------------------------------------------------------//
				// for close attack, the unit unable to attack the firm if
				// it is not in the firm surrounding
				//------------------------------------------------------------//
				if (_pathNodeDistance > AttackRange)
					return;
			}

			int targetXLoc = targetTown.LocX1;
			int targetYLoc = targetTown.LocY1;

			int attackDistance = CalcDistance(targetXLoc, targetYLoc, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT);

			if (attackDistance <= AttackRange) // able to attack target
			{
				if ((attackDistance == 1) && AttackRange > 1) // often false condition is checked first
					ChooseBestAttackMode(1); // may change to use close attack

				if (AttackRange > 1) // use range attack
				{
					SetCur(NextX, NextY);

					AttackInfo attackInfo = AttackInfos[CurAttack];
					int curXLoc = NextLocX;
					int curYLoc = NextLocY;
					bool canAddBullet = BulletArray.add_bullet_possible(curXLoc, curYLoc, MobileType,
						targetXLoc, targetYLoc, UnitConstants.UNIT_LAND, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT,
						out int resultLocX, out int resultLocY, attackInfo.bullet_speed, attackInfo.bullet_sprite_id);
					RangeAttackLocX = resultLocX;
					RangeAttackLocY = resultLocY;
					if (!canAddBullet)
					{
						//------- no suitable location, move to target ---------//
						if (PathNodes.Count  == 0) // no step for continuing moving
							SetMoveToSurround(ActionLocX, ActionLocY, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT,
								UnitConstants.BUILDING_TYPE_TOWN_MOVE_TO);

						return; // unable to attack, continue to move
					}

					//---------- able to do range attack ----------//
					SetAttackDir(NextLocX, NextLocY, RangeAttackLocX, RangeAttackLocY);
					CurFrame = 1;

					if (IsDirCorrect())
						SetAttack();
					else
						SetTurn();
				}
				else // close attack
				{
					//---------- attack now ---------//
					SetCur(NextX, NextY);
					TerminateMove();
					SetDir(NextLocX, NextLocY, targetTown.LocCenterX, targetTown.LocCenterY);

					if (IsDirCorrect())
						SetAttack();
					else
						SetTurn();
				}
			}
		}
	}

	private void ProcessAttackWall()
	{
		if (!CanAttack)
		{
			Stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//------------------------------------------------------------//
		// if the targeted wall has been destroyed
		//------------------------------------------------------------//
		Location loc = World.GetLoc(ActionLocX, ActionLocY);
		if (!loc.IsWall())
		{
			if (!ConfigAdv.unit_finish_attack_move || CurAction == SPRITE_ATTACK)
			{
				Stop2(UnitConstants.KEEP_DEFENSE_MODE);
			}

			return;
		}

		//-----------------------------------------------------//
		// If the unit is currently attacking.
		//-----------------------------------------------------//
		if (CurAction == SPRITE_ATTACK)
		{
			if (RemainAttackDelay != 0)
				return;

			AttackInfo attackInfo = AttackInfos[CurAttack];
			if (attackInfo.attack_range > 1) // range attack
			{
				//--------- wait for bullet emit ----------//
				if (CurFrame != attackInfo.bullet_out_frame)
					return;

				//---------- seek location to attack target by bullet --------//
				int curXLoc = NextLocX;
				int curYLoc = NextLocY;
				if (!BulletArray.bullet_path_possible(curXLoc, curYLoc, MobileType,
					    RangeAttackLocX, RangeAttackLocY,
					    UnitConstants.UNIT_LAND, attackInfo.bullet_speed, attackInfo.bullet_sprite_id))
				{
					bool canAddBullet = BulletArray.add_bullet_possible(curXLoc, curYLoc, MobileType,
						ActionLocX, ActionLocY, UnitConstants.UNIT_LAND, 1, 1,
						out int resultLocX, out int resultLocY, attackInfo.bullet_speed, attackInfo.bullet_sprite_id);
					RangeAttackLocX = resultLocX;
					RangeAttackLocY = resultLocY;
					if (!canAddBullet)
					{
						//--------- no suitable location, move to target ----------//
						SetMoveToSurround(ActionLocX, ActionLocY, 1, 1, UnitConstants.BUILDING_TYPE_WALL);
						return;
					}
				}

				//---------- add bullet, bullet emits -----------//
				BulletArray.AddBullet(this, ActionLocX, ActionLocY);
				AddCloseAttackEffect();

				// ------- reduce power --------//
				CurPower -= attackInfo.consume_power;
				if (CurPower < 0) // ***** BUGHERE
					CurPower = 0;
				SetRemainAttackDelay();
				return;
			}
			else
			{
				if (CurFrame != CurSpriteAttack().frameCount)
					return; // attacking

				HitWall(this, ActionLocX, ActionLocY, ActualDamage(), NationId);
				AddCloseAttackEffect();

				//------- reduce power --------//
				CurPower -= attackInfo.consume_power;
				if (CurPower < 0) // ***** BUGHERE
					CurPower = 0;
				SetRemainAttackDelay();
			}
		}
		//--------------------------------------------------------------------------------------------------//
		// If the unit is on its way to attack somebody, if it has gotten close next to the target, attack now
		//--------------------------------------------------------------------------------------------------//
		// it has moved to the specified location. check cur_x & go_x to make sure the sprite has completely move to the location, not just crossing it.
		else if (Math.Abs(CurX - NextX) <= SpriteInfo.Speed && Math.Abs(CurY - NextY) <= SpriteInfo.Speed)
		{
			if (MobileType == UnitConstants.UNIT_LAND)
			{
				if (DetectSurroundTarget())
					return;
			}

			if (AttackRange == 1)
			{
				//------------------------------------------------------------//
				// for close attack, the unit unable to attack the firm if
				// it is not in the firm surrounding
				//------------------------------------------------------------//
				if (_pathNodeDistance > AttackRange)
					return;
			}

			int attackDistance = CalcDistance(ActionLocX, ActionLocY, 1, 1);

			if (attackDistance <= AttackRange) // able to attack target
			{
				if ((attackDistance == 1) && AttackRange > 1) // often false condition is checked first
					ChooseBestAttackMode(1); // may change to use close attack

				if (AttackRange > 1) // use range attack
				{
					SetCur(NextX, NextY);

					AttackInfo attackInfo = AttackInfos[CurAttack];
					int curXLoc = NextLocX;
					int curYLoc = NextLocY;
					bool canAddBullet = BulletArray.add_bullet_possible(curXLoc, curYLoc, MobileType,
						ActionLocX, ActionLocY, UnitConstants.UNIT_LAND, 1, 1,
						out int resultLocX, out int resultLocY, attackInfo.bullet_speed, attackInfo.bullet_sprite_id);
					RangeAttackLocX = resultLocX;
					RangeAttackLocY = resultLocY;
					if (!canAddBullet)
					{
						//------- no suitable location, move to target ---------//
						if (PathNodes.Count == 0) // no step for continuing moving
							SetMoveToSurround(ActionLocX, ActionLocY, 1, 1, UnitConstants.BUILDING_TYPE_WALL);

						return; // unable to attack, continue to move
					}

					//---------- able to do range attack ----------//
					SetAttackDir(curXLoc, curYLoc, RangeAttackLocX, RangeAttackLocY);
					CurFrame = 1;

					if (IsDirCorrect())
						SetAttack();
					else
						SetTurn();
				}
				else // close attack
				{
					//---------- attack now ---------//
					SetCur(NextX, NextY);
					TerminateMove();
					SetAttackDir(NextLocX, NextLocY, ActionLocX, ActionLocY);

					if (IsDirCorrect())
						SetAttack();
					else
						SetTurn();
				}

			}
		}
	}

	private void InvalidateAttackTarget()
	{
		// TODO bug
		if (ActionMode2 == ActionMode && ActionPara2 == ActionPara2 && ActionLocX2 == ActionLocX && ActionLocY2 == ActionLocY)
		{
			ActionPara2 = 0;
		}

		ActionParam = 0;
	}

	public void AttackUnit(int targetXLoc, int targetYLoc, int xOffset, int yOffset, bool resetBlockedEdge)
	{
		Location loc = World.GetLoc(targetXLoc, targetYLoc);

		//--- AI attacking a nation which its NationRelation::should_attack is 0 ---//

		int targetNationRecno = 0;

		if (loc.HasUnit(UnitConstants.UNIT_LAND))
		{
			Unit unit = UnitArray[loc.UnitId(UnitConstants.UNIT_LAND)];

			if (unit.UnitType != UnitConstants.UNIT_EXPLOSIVE_CART) // attacking own porcupine is allowed
				targetNationRecno = unit.NationId;
		}
		else if (loc.IsFirm())
		{
			targetNationRecno = FirmArray[loc.FirmId()].nation_recno;
		}
		else if (loc.IsTown())
		{
			targetNationRecno = TownArray[loc.TownId()].NationId;
		}

		if (NationId != 0 && targetNationRecno != 0)
		{
			if (!NationArray[NationId].get_relation(targetNationRecno).should_attack)
				return;
		}

		//------------------------------------------------------------//
		// return if this unit cannot do the attack action, or die
		//------------------------------------------------------------//
		if (!CanAttack)
		{
			Stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}
		else if (IsUnitDead())
		{
			return;
		}

		loc = World.GetLoc(targetXLoc, targetYLoc);

		int targetMobileType = (NextLocX == targetXLoc && NextLocY == targetYLoc) ? loc.HasAnyUnit(MobileType) : loc.HasAnyUnit();

		if (targetMobileType != 0)
		{
			Unit targetUnit = UnitArray[loc.UnitId(targetMobileType)];
			AttackUnit(targetUnit.SpriteId, xOffset, yOffset, resetBlockedEdge);
		}

		//------ set ai_original_target_?_loc --------//

		if (AIUnit)
		{
			AIOriginalTargetLocX = targetXLoc;
			AIOriginalTargetLocY = targetYLoc;
		}
	}
	
	private void AttackUnit(int targetRecno, int xOffset, int yOffset, bool resetBlockedEdge)
	{
		//------------------------------------------------------------//
		// return if this unit cannot do the attack action, or die
		//------------------------------------------------------------//
		if (!CanAttack)
		{
			Stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}
		else if (IsUnitDead())
		{
			return;
		}

		//----------------------------------------------------------------------------------//
		// Note for non-air unit,
		// 1) If target's mobile type == mobile_type and thir territory id are different,
		//		call move_to() instead of attacking.
		// 2) In the case, this unit is a land unit and the target is a sea unit, skip
		//		checking for range attacking.  It is because the ship may be in the coast side
		//		there this unit can attack it by close attack.  In other cases, a unit without
		//		the ability of doing range attacking cannot attack target with different mobile
		//		type to its.
		// 3) If the region_id of the target located is same as that of thus unit located,
		//		order this unit to move to it and process attacking
		// 4) If the unit can't reach a location there it can do range attack, call move_to()
		//		rather than resume the action.  The reason not to resume the action, even though
		//		the unit reach a location there can do range attack later, is that the action
		//		will be resumed in function idle_detect_target()
		//----------------------------------------------------------------------------------//
		int curXLoc = NextLocX;
		int curYLoc = NextLocY;
		Unit targetUnit = UnitArray[targetRecno];
		int targetMobileType = targetUnit.MobileType;
		int targetXLoc = targetUnit.NextLocX;
		int targetYLoc = targetUnit.NextLocY;
		int maxRange = 0;
		bool diffTerritoryAttack = false;
		Location loc = World.GetLoc(targetUnit.NextLocX, targetUnit.NextLocY);

		if (targetMobileType != 0 && MobileType != UnitConstants.UNIT_AIR) // air unit can move to anywhere
		{
			//------------------------------------------------------------------------//
			// return if not feasible condition
			//------------------------------------------------------------------------//
			if ((MobileType != UnitConstants.UNIT_LAND || targetMobileType != UnitConstants.UNIT_SEA) && MobileType != targetMobileType)
			{
				if (!CanAttackDifferentTargetType())
				{
					//-************ improve later **************-//
					//-******** should escape from being attacked ********-//
					if (InAnyDefenseMode())
						GeneralDefendModeDetectTarget();
					return;
				}
			}

			//------------------------------------------------------------------------//
			// handle the case the unit and the target are in different territory
			//------------------------------------------------------------------------//
			if (World.GetLoc(curXLoc, curYLoc).RegionId != loc.RegionId)
			{
				maxRange = MaxAttackRange();
				Unit unit = UnitArray[loc.UnitId(targetMobileType)];
				if (!PossiblePlaceForRangeAttack(targetXLoc, targetYLoc, unit.SpriteInfo.LocWidth, unit.SpriteInfo.LocHeight, maxRange))
				{
					if (ActionMode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
					    ActionMode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
					    ActionMode2 != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
						MoveTo(targetXLoc, targetYLoc);
					else // in defend mode, but unable to attack target
						GeneralDefendModeDetectTarget(1);

					return;
				}
				else // can reach
				{
					diffTerritoryAttack = true;
				}
			}
		}

		//------------------------------------------------------------//
		// no unit there
		//------------------------------------------------------------//
		if (targetMobileType == 0)
		{
			if (ActionMode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
			    ActionMode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
			    ActionMode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
			{
				Stop2(UnitConstants.KEEP_DEFENSE_MODE);
			}

			return;
		}

		//------------------------------------------------------------//
		// cannot attack this nation
		//------------------------------------------------------------//
		if (!CanAttackNation(targetUnit.NationId) && targetUnit.UnitType != UnitConstants.UNIT_EXPLOSIVE_CART)
		{
			Stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//----------------------------------------------------------------//
		// action_mode2: checking for equal action or idle action
		//----------------------------------------------------------------//
		if ((ActionMode2 == UnitConstants.ACTION_ATTACK_UNIT ||
		     ActionMode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
		     ActionMode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
		     ActionMode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET) &&
		    ActionPara2 == targetUnit.SpriteId && ActionLocX2 == targetXLoc && ActionLocY2 == targetYLoc)
		{
			//------------ old order ------------//

			if (CurAction != SPRITE_IDLE)
			{
				//------- the old order is processing, return -------//
				return;
			} //else the action becomes idle
		}
		else
		{
			//-------------- store new order ----------------//
			if (ActionMode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
			    ActionMode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
			    ActionMode2 != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
			{
				ActionMode2 = UnitConstants.ACTION_ATTACK_UNIT;
			}

			ActionPara2 = targetUnit.SpriteId;
			ActionLocX2 = targetXLoc;
			ActionLocY2 = targetYLoc;
		}

		//-------------------------------------------------------------//
		// process new order
		//-------------------------------------------------------------//
		Stop();
		CurAttack = 0;

		int attackDistance = CalcDistance(targetXLoc, targetYLoc, targetUnit.SpriteInfo.LocWidth, targetUnit.SpriteInfo.LocHeight);
		ChooseBestAttackMode(attackDistance, targetMobileType);

		AttackInfo attackInfo = AttackInfos[CurAttack];
		if (attackInfo.attack_range < attackDistance) // need to move to target
		{
			int searchResult = 1;

			if (xOffset != 0 || yOffset != 0)
			{
				int xLoc = targetXLoc + xOffset, yLoc = targetYLoc + yOffset;
				if (xLoc < 0)
					xLoc = 0;
				else if (xLoc >= GameConstants.MapSize)
					xLoc = GameConstants.MapSize - 1;
				if (yLoc < 0)
					yLoc = 0;
				else if (yLoc >= GameConstants.MapSize)
					yLoc = GameConstants.MapSize - 1;

				Search(xLoc, yLoc, 1); // offset location is given, so move there directly
			}
			else
			{
				//if(mobile_type!=targetMobileType)
				if (diffTerritoryAttack)
				{
					//--------------------------------------------------------------------------------//
					// 1) different type from target, target located in different territory from this unit.
					//		But able to attack this target by range attacking
					//--------------------------------------------------------------------------------//
					MoveToRangeAttack(targetXLoc, targetYLoc, targetUnit.SpriteResId, SeekPath.SEARCH_MODE_ATTACK_UNIT_BY_RANGE, maxRange);
				}
				else
				{
					//--------------------------------------------------------------------------------//
					// 1) same type of target,
					// 2) this unit is air unit, or
					// 3) different type from target, but target located in the same territory of this unit.
					//--------------------------------------------------------------------------------//
					searchResult = Search(targetXLoc, targetYLoc, 1, SeekPath.SEARCH_MODE_TO_ATTACK, targetUnit.SpriteId);
				}
			}

			//---------------------------------------------------------------//
			// initialize parameters for blocked edge handling in attacking
			//---------------------------------------------------------------//
			if (searchResult != 0)
			{
				WaitingTerm = 0;
				if (resetBlockedEdge)
				{
					for (int i = 0; i < BlockedEdges.Length; i++)
					{
						BlockedEdges[i] = 0;
					}
				}
			}
			else
			{
				for (int i = 0; i < BlockedEdges.Length; i++)
				{
					BlockedEdges[i] = 0xff;
				}
			}
		}
		else if (CurAction == SPRITE_IDLE) // the target is within attack range, attacks it now if the unit is idle
		{
			//---------------------------------------------------------------//
			// attack now
			//---------------------------------------------------------------//
			SetCur(NextX, NextY);
			SetAttackDir(curXLoc, curYLoc, targetXLoc, targetYLoc);
			if (IsDirCorrect())
			{
				if (attackInfo.attack_range == 1)
				{
					SetAttack();
				}
			}
			else
			{
				SetTurn();
			}
		}

		ActionMode = UnitConstants.ACTION_ATTACK_UNIT;
		ActionParam = targetUnit.SpriteId;
		ActionLocX = targetXLoc;
		ActionLocY = targetYLoc;

		//------ set ai_original_target_?_loc --------//

		if (AIUnit)
		{
			AIOriginalTargetLocX = targetXLoc;
			AIOriginalTargetLocY = targetYLoc;
		}
	}
	
	public void AttackFirm(int firmXLoc, int firmYLoc, int xOffset = 0, int yOffset = 0, int resetBlockedEdge = 1)
	{
		//------------------------------------------------------------//
		// return if this unit cannot do the attack action
		//------------------------------------------------------------//
		if (!CanAttack)
		{
			Stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}
		else if (IsUnitDead())
		{
			return;
		}

		Location loc = World.GetLoc(firmXLoc, firmYLoc);

		//------------------------------------------------------------//
		// no firm there
		//------------------------------------------------------------//
		if (!loc.IsFirm())
		{
			if (ActionMode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
			    ActionMode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
			    ActionMode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
			{
				Stop2(UnitConstants.KEEP_DEFENSE_MODE);
			}

			return;
		}

		//------------------------------------------------------------//
		// cannot attack this nation
		//------------------------------------------------------------//
		Firm firm = FirmArray[loc.FirmId()];
		if (!CanAttackNation(firm.nation_recno))
		{
			Stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//------------------------------------------------------------------------------------//
		// move there if cannot reach the effective attacking region
		//------------------------------------------------------------------------------------//
		FirmInfo firmInfo = FirmRes[firm.firm_id];
		int maxRange = 0;
		bool diffTerritoryAttack = false;
		if (MobileType != UnitConstants.UNIT_AIR && World.GetLoc(NextLocX, NextLocY).RegionId != loc.RegionId)
		{
			maxRange = MaxAttackRange();
			//Firm		*firm = FirmArray[loc.firm_recno()];
			if (!PossiblePlaceForRangeAttack(firmXLoc, firmYLoc, firmInfo.loc_width, firmInfo.loc_height, maxRange))
			{
				if (ActionMode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
				    ActionMode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
				    ActionMode2 != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
				{
					MoveTo(firmXLoc, firmYLoc);
				}

				return;
			}
			else // can reach
			{
				diffTerritoryAttack = true;
			}
		}

		//----------------------------------------------------------------//
		// action_mode2: checking for equal action or idle action
		//----------------------------------------------------------------//
		if ((ActionMode2 == UnitConstants.ACTION_ATTACK_FIRM ||
		     ActionMode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
		     ActionMode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
		     ActionMode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET) &&
		    ActionPara2 == firm.firm_recno && ActionLocX2 == firmXLoc && ActionLocY2 == firmYLoc)
		{
			//-------------- old order -------------//
			if (CurAction != SPRITE_IDLE)
			{
				return;
			}
		}
		else
		{
			//-------------- new order -------------//
			if (ActionMode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
			    ActionMode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
			    ActionMode2 != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
				ActionMode2 = UnitConstants.ACTION_ATTACK_FIRM;

			ActionPara2 = firm.firm_recno;
			ActionLocX2 = firmXLoc;
			ActionLocY2 = firmYLoc;
		}

		//-------------------------------------------------------------//
		// process new order
		//-------------------------------------------------------------//
		Stop();
		CurAttack = 0;

		int attackDistance = CalcDistance(firmXLoc, firmYLoc, firmInfo.loc_width, firmInfo.loc_height);
		ChooseBestAttackMode(attackDistance);

		AttackInfo attackInfo = AttackInfos[CurAttack];
		if (attackInfo.attack_range < attackDistance) // need to move to target
		{
			bool pathEdited = true;

			if (xOffset != 0 || yOffset != 0)
			{
				int xLoc = firmXLoc + xOffset, yLoc = firmYLoc + yOffset;
				if (xLoc < 0)
					xLoc = 0;
				else if (xLoc >= GameConstants.MapSize)
					xLoc = GameConstants.MapSize - 1;
				if (yLoc < 0)
					yLoc = 0;
				else if (yLoc >= GameConstants.MapSize)
					yLoc = GameConstants.MapSize - 1;

				Search(xLoc, yLoc, 1); // offset location is given, so move there directly
			}
			else // without offset given, so call set_move_to_surround()
			{
				if (diffTerritoryAttack)
				{
					//--------------------------------------------------------------------------------//
					// 1) different type from target, target located in different territory from this unit.
					//		But able to attack this target by range attacking
					//--------------------------------------------------------------------------------//
					MoveToRangeAttack(firmXLoc, firmYLoc, firm.firm_id, SeekPath.SEARCH_MODE_ATTACK_FIRM_BY_RANGE, maxRange);
				}
				else
				{
					//--------------------------------------------------------------------------------//
					// 1) same type of target,
					// 2) this unit is air unit, or
					// 3) different type from target, but target located in the same territory of this unit.
					//--------------------------------------------------------------------------------//
					pathEdited = SetMoveToSurround(firmXLoc, firmYLoc, firmInfo.loc_width, firmInfo.loc_height,
						UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO, 0, 0);
				}
			}

			//---------------------------------------------------------------//
			// initialize parameters for blocked edge handling in attacking
			//---------------------------------------------------------------//
			if (pathEdited)
			{
				WaitingTerm = 0;
				if (resetBlockedEdge != 0)
				{
					for (int i = 0; i < BlockedEdges.Length; i++)
						BlockedEdges[i] = 0;
				}
			}
			else
			{
				for (int i = 0; i < BlockedEdges.Length; i++)
					BlockedEdges[i] = 0xff;
			}
		}
		else if (CurAction == SPRITE_IDLE)
		{
			//---------------------------------------------------------------//
			// attack now
			//---------------------------------------------------------------//
			SetCur(NextX, NextY);

			if (firm.firm_id != Firm.FIRM_RESEARCH)
			{
				SetAttackDir(NextLocX, NextLocY, firm.center_x, firm.center_y);
			}
			else // FIRM_RESEARCH with size 2x3
			{
				int curXLoc = NextLocX;
				int curYLoc = NextLocY;

				int hitXLoc = (curXLoc > firm.loc_x1) ? firm.loc_x2 : firm.loc_x1;

				int hitYLoc;
				if (curYLoc < firm.center_y)
					hitYLoc = firm.loc_y1;
				else if (curYLoc == firm.center_y)
					hitYLoc = firm.center_y;
				else
					hitYLoc = firm.loc_y2;

				SetAttackDir(curXLoc, curYLoc, hitXLoc, hitYLoc);
			}

			if (IsDirCorrect())
			{
				if (attackInfo.attack_range == 1)
					SetAttack();
				//else range_attack is processed in calling process_attack_firm()
			}
			else
			{
				SetTurn();
			}
		}

		ActionMode = UnitConstants.ACTION_ATTACK_FIRM;
		ActionParam = firm.firm_recno;
		ActionLocX = firmXLoc;
		ActionLocY = firmYLoc;
	}
	
	public void AttackTown(int townXLoc, int townYLoc, int xOffset = 0, int yOffset = 0, int resetBlockedEdge = 1)
	{
		//------------------------------------------------------------//
		// return if this unit cannot do the attack action
		//------------------------------------------------------------//
		if (!CanAttack)
		{
			Stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}
		else if (IsUnitDead())
		{
			return;
		}

		Location loc = World.GetLoc(townXLoc, townYLoc);

		//------------------------------------------------------------//
		// no town there
		//------------------------------------------------------------//
		if (!loc.IsTown())
		{
			if (ActionMode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
			    ActionMode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
			    ActionMode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
			{
				Stop(UnitConstants.KEEP_DEFENSE_MODE);
			}

			return;
		}

		//------------------------------------------------------------//
		// cannot attack this nation
		//------------------------------------------------------------//
		Town town = TownArray[loc.TownId()];
		if (!CanAttackNation(town.NationId))
		{
			Stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//------------------------------------------------------------------------------------//
		// move there if cannot reach the effective attacking region
		//------------------------------------------------------------------------------------//
		int maxRange = 0;
		bool diffTerritoryAttack = false;
		if (MobileType != UnitConstants.UNIT_AIR && World.GetLoc(NextLocX, NextLocY).RegionId != loc.RegionId)
		{
			maxRange = MaxAttackRange();
			if (!PossiblePlaceForRangeAttack(townXLoc, townYLoc, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, maxRange))
			{
				if (ActionMode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
				    ActionMode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
				    ActionMode != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
				{
					MoveTo(townXLoc, townYLoc);
				}

				return;
			}
			else // can reach
			{
				diffTerritoryAttack = true;
			}
		}

		//----------------------------------------------------------------//
		// action_mode2: checking for equal action or idle action
		//----------------------------------------------------------------//
		if ((ActionMode2 == UnitConstants.ACTION_ATTACK_TOWN ||
		     ActionMode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
		     ActionMode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
		     ActionMode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET) &&
		    ActionPara2 == town.TownId && ActionLocX2 == townXLoc && ActionLocY2 == townYLoc)
		{
			//----------- old order ------------//

			if (CurAction != SPRITE_IDLE)
			{
				//-------- old order is processing -------//
				return;
			}
		}
		else
		{
			//------------ new order -------------//
			if (ActionMode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
			    ActionMode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
			    ActionMode2 != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
				ActionMode2 = UnitConstants.ACTION_ATTACK_TOWN;

			ActionPara2 = town.TownId;
			ActionLocX2 = townXLoc;
			ActionLocY2 = townYLoc;
		}

		//-------------------------------------------------------------//
		// process new order
		//-------------------------------------------------------------//
		Stop();
		CurAttack = 0;

		int attackDistance = CalcDistance(townXLoc, townYLoc, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT);
		ChooseBestAttackMode(attackDistance);

		AttackInfo attackInfo = AttackInfos[CurAttack];
		if (attackInfo.attack_range < attackDistance)
		{
			bool pathEdited = true;

			if (xOffset != 0 || yOffset != 0)
			{
				int xLoc = townXLoc + xOffset, yLoc = townYLoc + yOffset;
				if (xLoc < 0)
					xLoc = 0;
				else if (xLoc >= GameConstants.MapSize)
					xLoc = GameConstants.MapSize - 1;
				if (yLoc < 0)
					yLoc = 0;
				else if (yLoc >= GameConstants.MapSize)
					yLoc = GameConstants.MapSize - 1;

				Search(xLoc, yLoc, 1); // offset location is given, so move there directly
			}
			else // without offset given, so call set_move_to_surround()
			{
				if (diffTerritoryAttack)
				{
					//--------------------------------------------------------------------------------//
					// 1) different type from target, target located in different territory but able to
					//		attack this target by range attacking
					//--------------------------------------------------------------------------------//
					MoveToRangeAttack(townXLoc, townYLoc, 0, SeekPath.SEARCH_MODE_ATTACK_TOWN_BY_RANGE, maxRange);
				}
				else
				{
					//--------------------------------------------------------------------------------//
					// 1) same type of target,
					// 2) this unit is air unit, or
					// 3) different type from target, but target located in the same territory
					//--------------------------------------------------------------------------------//
					pathEdited = SetMoveToSurround(townXLoc, townYLoc, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT,
						UnitConstants.BUILDING_TYPE_TOWN_MOVE_TO, 0, 0);
				}
			}

			//---------------------------------------------------------------//
			// initialize parameters for blocked edge handling in attacking
			//---------------------------------------------------------------//
			if (pathEdited)
			{
				WaitingTerm = 0;
				if (resetBlockedEdge != 0)
				{
					for (int i = 0; i < BlockedEdges.Length; i++)
						BlockedEdges[i] = 0;
				}
			}
			else
			{
				for (int i = 0; i < BlockedEdges.Length; i++)
					BlockedEdges[i] = 0xff;
			}
		}
		else if (CurAction == SPRITE_IDLE)
		{
			//---------------------------------------------------------------//
			// attack now
			//---------------------------------------------------------------//
			SetCur(NextX, NextY);
			SetAttackDir(NextLocX, NextLocY, town.LocCenterX, town.LocCenterY);
			if (IsDirCorrect())
			{
				if (attackInfo.attack_range == 1)
					SetAttack();
			}
			else
			{
				SetTurn();
			}
		}

		ActionMode = UnitConstants.ACTION_ATTACK_TOWN;
		ActionParam = town.TownId;
		ActionLocX = townXLoc;
		ActionLocY = townYLoc;
	}
	
	public void AttackWall(int wallXLoc, int wallYLoc, int xOffset = 0, int yOffset = 0, int resetBlockedEdge = 1)
	{
		//------------------------------------------------------------//
		// return if this unit cannot do the attack action
		//------------------------------------------------------------//
		if (!CanAttack)
		{
			Stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}
		else if (IsUnitDead())
		{
			return;
		}

		Location loc = World.GetLoc(wallXLoc, wallYLoc);

		//------------------------------------------------------------//
		// no wall there
		//------------------------------------------------------------//
		if (!loc.IsWall())
		{
			if (ActionMode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
			    ActionMode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
			    ActionMode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
			{
				Stop(UnitConstants.KEEP_DEFENSE_MODE);
			}

			return;
		}

		//------------------------------------------------------------//
		// cannot attack this nation
		//------------------------------------------------------------//
		if (!CanAttackNation(loc.WallNationId()))
		{
			Stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//------------------------------------------------------------------------------------//
		// move there if cannot reach the effective attacking region
		//------------------------------------------------------------------------------------//
		int maxRange = 0;
		bool diffTerritoryAttack = false;
		if (MobileType != UnitConstants.UNIT_AIR && World.GetLoc(NextLocX, NextLocY).RegionId != loc.RegionId)
		{
			maxRange = MaxAttackRange();
			if (!PossiblePlaceForRangeAttack(wallXLoc, wallYLoc, 1, 1, maxRange))
			{
				if (ActionMode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
				    ActionMode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
				    ActionMode != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
				{
					MoveTo(wallXLoc, wallYLoc);
				}

				return;
			}
			else // can reach
			{
				diffTerritoryAttack = true;
			}
		}

		//----------------------------------------------------------------//
		// action_mode2: checking for equal action or idle action
		//----------------------------------------------------------------//
		if ((ActionMode2 == UnitConstants.ACTION_ATTACK_WALL ||
		     ActionMode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
		     ActionMode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
		     ActionMode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET) &&
		    ActionPara2 == 0 && ActionLocX2 == wallXLoc && ActionLocY2 == wallYLoc)
		{
			//------------ old order ------------//

			if (CurAction != SPRITE_IDLE)
			{
				//------- old order is processing --------//
				return;
			}
		}
		else
		{
			//-------------- new order -------------//
			if (ActionMode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
			    ActionMode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
			    ActionMode2 != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
				ActionMode2 = UnitConstants.ACTION_ATTACK_WALL;

			ActionPara2 = 0;
			ActionLocX2 = wallXLoc;
			ActionLocY2 = wallYLoc;
		}

		//-------------------------------------------------------------//
		// process new order
		//-------------------------------------------------------------//
		Stop();
		CurAttack = 0;

		int attackDistance = CalcDistance(wallXLoc, wallYLoc, 1, 1);
		ChooseBestAttackMode(attackDistance);

		AttackInfo attackInfo = AttackInfos[CurAttack];
		if (attackInfo.attack_range < attackDistance)
		{
			bool pathEdited = true;

			if (xOffset != 0 || yOffset != 0)
			{
				int xLoc = wallXLoc + xOffset, yLoc = wallYLoc + yOffset;
				if (xLoc < 0)
					xLoc = 0;
				else if (xLoc >= GameConstants.MapSize)
					xLoc = GameConstants.MapSize - 1;
				if (yLoc < 0)
					yLoc = 0;
				else if (yLoc >= GameConstants.MapSize)
					yLoc = GameConstants.MapSize - 1;

				Search(xLoc, yLoc, 1); // offset location is given, so move there directly
			}
			else
			{
				if (diffTerritoryAttack)
				{
					//--------------------------------------------------------------------------------//
					// 1) different type from target, target located in different territory but able to
					//		attack this target by range attacking
					//--------------------------------------------------------------------------------//
					MoveToRangeAttack(wallXLoc, wallYLoc, 0, SeekPath.SEARCH_MODE_ATTACK_WALL_BY_RANGE, maxRange);
				}
				else
				{
					//--------------------------------------------------------------------------------//
					// 1) same type of target,
					// 2) this unit is air unit, or
					// 3) different type from target, but target located in the same territory
					//--------------------------------------------------------------------------------//
					pathEdited = SetMoveToSurround(wallXLoc, wallYLoc, 1, 1,
						UnitConstants.BUILDING_TYPE_WALL, 0, 0);
				}
			}

			//---------------------------------------------------------------//
			// initialize parameters for blocked edge handling in attacking
			//---------------------------------------------------------------//
			if (pathEdited)
			{
				WaitingTerm = 0;
				if (resetBlockedEdge != 0)
				{
					for (int i = 0; i < BlockedEdges.Length; i++)
						BlockedEdges[i] = 0;
				}
			}
			else
			{
				for (int i = 0; i < BlockedEdges.Length; i++)
					BlockedEdges[i] = 0xff;
			}
		}
		else if (CurAction == SPRITE_IDLE)
		{
			//---------------------------------------------------------------//
			// attack now
			//---------------------------------------------------------------//
			SetCur(NextX, NextY);
			SetAttackDir(NextLocX, NextLocY, wallXLoc, wallYLoc);
			if (IsDirCorrect())
			{
				if (attackInfo.attack_range == 1)
					SetAttack();
			}
			else
			{
				SetTurn();
			}
		}

		ActionMode = UnitConstants.ACTION_ATTACK_WALL;
		ActionParam = 0;
		ActionLocX = wallXLoc;
		ActionLocY = wallYLoc;
	}
	
	public void HitTarget(Unit parentUnit, Unit targetUnit, double attackDamage, int parentNationRecno)
	{
		int targetNationRecno = targetUnit.NationId;

		//------------------------------------------------------------//
		// if the attacked unit is in defense mode, order other available
		// unit in the same camp to help this unit
		// Note : checking for nation_recno since one unit can attack units
		//			 in same nation by bullet accidentally
		//------------------------------------------------------------//
		if (parentUnit != null && parentUnit.CurAction != SPRITE_DIE && parentUnit.IsVisible() &&
		    parentNationRecno != targetNationRecno && parentUnit.CanAttackNation(targetNationRecno) &&
		    targetUnit.InAutoDefenseMode())
		{
			if (!FirmArray.IsDeleted(targetUnit.ActionMiscParam))
			{
				Firm firm = FirmArray[targetUnit.ActionMiscParam];
				if (firm.firm_id == Firm.FIRM_CAMP)
				{
					FirmCamp camp = (FirmCamp)firm;
					camp.defense(parentUnit.SpriteId);
				}
			}
			else
			{
				targetUnit.ClearUnitDefenseMode();
			}
		}

		// ---------- add indicator on the map ----------//
		if (NationArray.player_recno != 0 && targetUnit.IsOwn())
			WarPointArray.AddPoint(targetUnit.NextLocX, targetUnit.NextLocY);

		//-----------------------------------------------------------------------//
		// decrease the hit points of the target Unit
		//-----------------------------------------------------------------------//
		const int DEFAULT_ARMOR = 4;
		const double DEFAULT_ARMOR_OVER_ATTACK_SLOW_DOWN = (double)DEFAULT_ARMOR / (double)InternalConstants.ATTACK_SLOW_DOWN;
		const double ONE_OVER_ATTACK_SLOW_DOWN = 1.0 / (double)InternalConstants.ATTACK_SLOW_DOWN;
		const double COMPARE_POINT = DEFAULT_ARMOR_OVER_ATTACK_SLOW_DOWN + ONE_OVER_ATTACK_SLOW_DOWN;

		if (attackDamage >= COMPARE_POINT)
			targetUnit.HitPoints -= attackDamage - DEFAULT_ARMOR_OVER_ATTACK_SLOW_DOWN;
		else
			targetUnit.HitPoints -= Math.Min(attackDamage, ONE_OVER_ATTACK_SLOW_DOWN);  // in case attackDamage = 0, no hit_point is reduced
		
		Nation parentNation = parentNationRecno != 0 ? NationArray[parentNationRecno] : null;
		Nation targetNation = targetNationRecno != 0 ? NationArray[targetNationRecno] : null;
		int targetUnitClass = UnitRes[targetUnit.UnitType].unit_class;

		if (targetUnit.HitPoints <= 0.0)
		{
			targetUnit.HitPoints = 0.0;

			//---- if the unit killed is a human unit -----//

			if (targetUnit.RaceId != 0)
			{
				//---- if the unit killed is a town defender unit -----//

				if (targetUnit.IsCivilian() && targetUnit.InDefendTownMode())
				{
					if (targetNation != null)
					{
						targetNation.civilian_killed(targetUnit.RaceId, false, 1);
						targetNation.own_civilian_killed++;
					}

					if (parentNation != null)
					{
						parentNation.civilian_killed(targetUnit.RaceId, true, 1);
						parentNation.enemy_civilian_killed++;
					}
				}
				else if (targetUnit.IsCivilian() && targetUnit.Skill.CombatLevel < 20) //--- mobile civilian ---//
				{
					if (targetNation != null)
					{
						targetNation.civilian_killed(targetUnit.RaceId, false, 0);
						targetNation.own_civilian_killed++;
					}

					if (parentNation != null)
					{
						parentNation.civilian_killed(targetUnit.RaceId, true, 0);
						parentNation.enemy_civilian_killed++;
					}
				}
				else //---- if the unit killed is a soldier -----//
				{
					if (targetNation != null)
						targetNation.own_soldier_killed++;

					if (parentNation != null)
						parentNation.enemy_soldier_killed++;
				}
			}

			//--------- if it's a non-human unit ---------//

			else
			{
				switch (UnitRes[targetUnit.UnitType].unit_class)
				{
					case UnitConstants.UNIT_CLASS_WEAPON:
						if (parentNation != null)
							parentNation.enemy_weapon_destroyed++;

						if (targetNation != null)
							targetNation.own_weapon_destroyed++;
						break;

					case UnitConstants.UNIT_CLASS_SHIP:
						if (parentNation != null)
							parentNation.enemy_ship_destroyed++;

						if (targetNation != null)
							targetNation.own_ship_destroyed++;
						break;
				}

				//---- if the unit destroyed is a trader or caravan -----//

				// killing a caravan is resented by all races
				if (targetUnit.UnitType == UnitConstants.UNIT_CARAVAN || targetUnit.UnitType == UnitConstants.UNIT_VESSEL)
				{
					// Race-Id of 0 means a loyalty penalty applied for all races
					if (targetNation != null)
						targetNation.civilian_killed(0, false, 3);

					if (parentNation != null)
						parentNation.civilian_killed(0, true, 3);
				}
			}

			return;
		}

		if (parentUnit != null && parentNationRecno != targetNationRecno)
			parentUnit.GainExperience(); // gain experience to increase combat level

		//-----------------------------------------------------------------------//
		// action of the target to take
		//-----------------------------------------------------------------------//
		if (parentUnit == null) // do nothing if parent is dead
			return;

		if (parentUnit.CurAction == SPRITE_DIE) // skip for explosive cart
			return;

		// the target and the attacker's nations are different
		// (it's possible that when a unit who has just changed nation has its bullet hitting its own nation)
		if (targetNationRecno == parentNationRecno)
			return;

		//------- two nations at war ---------//

		if (parentNation != null && targetNation != null)
		{
			parentNation.set_at_war_today();
			targetNation.set_at_war_today(parentUnit.SpriteId);
		}

		//-------- increase battling fryhtan score --------//

		if (parentNation != null && targetUnitClass == UnitConstants.UNIT_CLASS_MONSTER)
		{
			parentNation.kill_monster_score += 0.1;
		}

		//------ call target unit being attack functions -------//

		if (targetNation != null)
		{
			targetNation.being_attacked(parentNationRecno);

			if (targetUnit.AIUnit)
			{
				if (targetUnit.Rank >= RANK_GENERAL)
					targetUnit.AILeaderBeingAttacked(parentUnit.SpriteId);

				if (UnitRes[targetUnit.UnitType].unit_class == UnitConstants.UNIT_CLASS_SHIP)
					((UnitMarine)targetUnit).ai_ship_being_attacked(parentUnit.SpriteId);
			}

			//--- if a member in a troop is under attack, ask for other troop members to help ---//

			if (Info.TotalDays % 2 == SpriteId % 2)
			{
				if (targetUnit.LeaderId != 0 || targetUnit.TeamInfo.Members.Count > 1)
				{
					// it is possible that parentUnit is dying right now
					if (!UnitArray.IsDeleted(parentUnit.SpriteId)) 
						targetUnit.AskTeamHelpAttack(parentUnit);
				}
			}
		}

		//--- increase reputation of the nation that attacks monsters ---//

		else if (targetUnitClass == UnitConstants.UNIT_CLASS_MONSTER)
		{
			if (parentNation != null)
				parentNation.change_reputation(GameConstants.REPUTATION_INCREASE_PER_ATTACK_MONSTER);

			//--- if a member in a troop is under attack, ask for other troop members to help ---//

			if (Info.TotalDays % 2 == SpriteId % 2)
			{
				if (targetUnit.LeaderId != 0 || targetUnit.TeamInfo.Members.Count > 1)
				{
					// it is possible that parentUnit is dying right now
					if (!UnitArray.IsDeleted(parentUnit.SpriteId)) 
						targetUnit.AskTeamHelpAttack(parentUnit);
				}
			}
		}

		//------------------------------------------//

		if (!targetUnit.CanAttack) // no action if the target unit is unable to attack
			return;

		targetUnit.UnitAutoGuarding(parentUnit);
	}

	private void AskTeamHelpAttack(Unit attackerUnit)
	{
		//--- if the attacking unit is our unit (this can happen if the unit is porcupine) ---//

		if (attackerUnit.NationId == NationId)
			return;

		//-----------------------------------------//

		int leaderUnitRecno = SpriteId;

		if (LeaderId != 0) // if the current unit is a soldier, get its leader's recno
			leaderUnitRecno = LeaderId;

		TeamInfo teamInfo = UnitArray[leaderUnitRecno].TeamInfo;

		for (int i = teamInfo.Members.Count - 1; i >= 0; i--)
		{
			int unitRecno = teamInfo.Members[i];

			if (UnitArray.IsDeleted(unitRecno))
				continue;

			Unit unit = UnitArray[unitRecno];

			if (unit.CurAction == SPRITE_IDLE && unit.IsVisible())
			{
				unit.AttackUnit(attackerUnit.SpriteId, 0, 0, true);
				return;
			}

			if (ConfigAdv.unit_ai_team_help && (unit.AIUnit || NationId == 0) &&
			    unit.IsVisible() && unit.HitPoints > 15.0 &&
			    (unit.ActionMode == UnitConstants.ACTION_STOP ||
			     unit.ActionMode == UnitConstants.ACTION_ASSIGN_TO_FIRM ||
			     unit.ActionMode == UnitConstants.ACTION_ASSIGN_TO_TOWN ||
			     unit.ActionMode == UnitConstants.ACTION_SETTLE))
			{
				unit.AttackUnit(attackerUnit.SpriteId, 0, 0, true);
				return;
			}
		}
	}
	
	private void UnitAutoGuarding(Unit attackUnit)
	{
		if (ForceMove)
			return;

		//---------------------------------------//
		//
		// If the aggressive_mode is off, then don't fight back when the unit is moving,
		// only fight back when the unit is already fighting or is idle.
		//
		//---------------------------------------//

		if (!AggressiveMode && CurAction != SPRITE_ATTACK && CurAction != SPRITE_IDLE)
		{
			return;
		}

		//--------------------------------------------------------------------//
		// decide attack or not
		//--------------------------------------------------------------------//

		int changeToAttack = 0;
		if (CurAction == SPRITE_ATTACK || (SpriteInfo.NeedTurning != 0 && CurAction == SPRITE_TURN &&
		                                    (Math.Abs(NextLocX - ActionLocX) < AttackRange ||
		                                     Math.Abs(NextLocY - ActionLocY) < AttackRange)))
		{
			if (ActionMode != UnitConstants.ACTION_ATTACK_UNIT)
			{
				changeToAttack++; //else continue to attack the target unit
			}
			else
			{
				if (ActionParam == 0 || UnitArray.IsDeleted(ActionParam))
					changeToAttack++; // attack new target
			}
		}
		else if
			(CurAction != SPRITE_DIE) // && abs(cur_x-next_x)<spriteInfo.speed && abs(cur_y-next_y)<spriteInfo.speed)
		{
			changeToAttack++;
			/*if(!ai_unit) // player unit
			{
				if(action_mode!=ACTION_ATTACK_UNIT)
					changeToAttack++;  //else continue to attack the target unit
				else
				{
					err_when(!action_para);
					if(UnitArray.IsDeleted(action_para))
						changeToAttack++; // attack new target
				}
			}
			else
				changeToAttack++;*/
		}

		if (changeToAttack == 0)
		{
			if (AIUnit) // allow ai unit to select target to attack
			{
				//------------------------------------------------------------//
				// conditions to let the unit escape
				//------------------------------------------------------------//
				//-************* codes here ************-//

				//------------------------------------------------------------//
				// select the weaker target to attack first, if more than one
				// unit attack this unit
				//------------------------------------------------------------//
				int attackXLoc = attackUnit.NextLocX;
				int attackYLoc = attackUnit.NextLocY;

				int attackDistance = CalcDistance(attackXLoc, attackYLoc, attackUnit.SpriteInfo.LocWidth, attackUnit.SpriteInfo.LocHeight);
				if (attackDistance == 1) // only consider close attack
				{
					Unit targetUnit = UnitArray[ActionParam];
					if (targetUnit.HitPoints > attackUnit.HitPoints) // the new attacker is weaker
						AttackUnit(attackUnit.SpriteId, 0, 0, true);
				}
			}

			return;
		}

		//--------------------------------------------------------------------//
		// cancel AI actions
		//--------------------------------------------------------------------//
		if (AIActionId != 0 && NationId != 0)
			NationArray[NationId].action_failure(AIActionId, SpriteId);

		if (!attackUnit.IsVisible())
			return;

		//--------------------------------------------------------------------------------//
		// checking for ship processing trading
		//--------------------------------------------------------------------------------//
		if (SpriteInfo.SpriteSubType == 'M') //**** BUGHERE, is sprite_sub_type really representing UNIT_MARINE???
		{
			UnitInfo unitInfo = UnitRes[UnitType];
			if (unitInfo.carry_goods_capacity != 0)
			{
				UnitMarine ship = (UnitMarine)this;
				if (ship.auto_mode != 0 && ship.stop_defined_num > 1)
				{
					int targetXLoc = attackUnit.NextLocX;
					int targetYLoc = attackUnit.NextLocY;
					SpriteInfo targetSpriteInfo = attackUnit.SpriteInfo;
					int attackDistance = CalcDistance(targetXLoc, targetYLoc, targetSpriteInfo.LocWidth, targetSpriteInfo.LocHeight);
					int maxAttackRange = MaxAttackRange();
					if (maxAttackRange < attackDistance)
						return; // can't attack the target
				}
			}
		}

		switch (ActionMode2)
		{
			case UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET:
			case UnitConstants.ACTION_AUTO_DEFENSE_BACK_CAMP:
				ActionMode2 = UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET;
				break;

			case UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET:
			case UnitConstants.ACTION_DEFEND_TOWN_BACK_TOWN:
				ActionMode2 = UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET;
				break;

			case UnitConstants.ACTION_MONSTER_DEFEND_DETECT_TARGET:
			case UnitConstants.ACTION_MONSTER_DEFEND_BACK_FIRM:
				ActionMode2 = UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET;
				break;
		}

		SaveOriginalAction();

		//----------------------------------------------------------//
		// set the original location of the attacking target when
		// the attack() function is called, action_x_loc2 & action_y_loc2
		// will change when the unit move, but these two will not.
		//----------------------------------------------------------//

		OriginalTargetLocX = attackUnit.NextLocX;
		OriginalTargetLocY = attackUnit.NextLocY;

		if (!UnitArray.IsDeleted(attackUnit.SpriteId))
			AttackUnit(attackUnit.SpriteId, 0, 0, true);
	}
	
	public void HitBuilding(Unit attackUnit, int targetXLoc, int targetYLoc, double attackDamage, int attackNationRecno)
	{
		Location loc = World.GetLoc(targetXLoc, targetYLoc);

		if (loc.IsFirm())
			HitFirm(attackUnit, targetXLoc, targetYLoc, attackDamage, attackNationRecno);
		else if (loc.IsTown())
			HitTown(attackUnit, targetXLoc, targetYLoc, attackDamage, attackNationRecno);
	}
	
	public void HitFirm(Unit attackUnit, int targetXLoc, int targetYLoc, double attackDamage, int attackNationRecno)
	{
		Location loc = World.GetLoc(targetXLoc, targetYLoc);
		if (!loc.IsFirm())
			return; // do nothing if no firm there

		//----------- attack firm ------------//
		Firm targetFirm = FirmArray[loc.FirmId()];

		Nation attackNation = NationArray.IsDeleted(attackNationRecno) ? null : NationArray[attackNationRecno];

		//------------------------------------------------------------------------------//
		// change relation to hostile
		// check for NULL to skip unhandled case by bullets
		// check for SPRITE_DIE to skip the case by EXPLOSIVE_CART
		//------------------------------------------------------------------------------//
		// the target and the attacker's nations are different
		// (it's possible that when a unit who has just changed nation has its bullet hitting its own nation)
		if (attackUnit != null && attackUnit.CurAction != SPRITE_DIE && targetFirm.nation_recno != attackNationRecno)
		{
			if (attackNation != null && targetFirm.nation_recno != 0)
			{
				attackNation.set_at_war_today();
				NationArray[targetFirm.nation_recno].set_at_war_today(attackUnit.SpriteId);
			}

			if (targetFirm.nation_recno != 0)
				NationArray[targetFirm.nation_recno].being_attacked(attackNationRecno);

			//------------ auto defense -----------------//
			if (attackUnit.IsVisible())
				targetFirm.auto_defense(attackUnit.SpriteId);

			if (attackNationRecno != targetFirm.nation_recno)
				attackUnit.GainExperience(); // gain experience to increase combat level

			targetFirm.being_attacked(attackUnit.SpriteId);

			//------ increase battling fryhtan score -------//

			if (attackNation != null && targetFirm.firm_id == Firm.FIRM_MONSTER)
				attackNation.kill_monster_score += 0.01;
		}

		//---------- add indicator on the map ----------//

		if (NationArray.player_recno != 0 && targetFirm.own_firm())
			WarPointArray.AddPoint(targetFirm.center_x, targetFirm.center_y);

		//---------- damage to the firm ------------//

		targetFirm.hit_points -= attackDamage / 3.0; // /3 so that it takes longer to destroy a firm

		if (targetFirm.hit_points <= 0.0)
		{
			targetFirm.hit_points = 0.0;

			SERes.sound(targetFirm.center_x, targetFirm.center_y, 1, 'F', targetFirm.firm_id, "DIE");

			if (targetFirm.nation_recno == NationArray.player_recno)
				NewsArray.firm_destroyed(targetFirm.firm_recno, attackUnit, attackNationRecno);

			if (targetFirm.nation_recno != 0)
			{
				if (attackNation != null)
					attackNation.enemy_firm_destroyed++;

				NationArray[targetFirm.nation_recno].own_firm_destroyed++;
			}

			else if (targetFirm.firm_id == Firm.FIRM_MONSTER)
			{
				NewsArray.monster_firm_destroyed(((FirmMonster)targetFirm).monster_id, targetFirm.center_x, targetFirm.center_y);
			}

			FirmArray.DeleteFirm(targetFirm);
		}
	}
	
	public void HitTown(Unit attackUnit, int targetXLoc, int targetYLoc, double attackDamage, int attackNationRecno)
	{
		Location loc = World.GetLoc(targetXLoc, targetYLoc);

		if (!loc.IsTown())
			return; // do nothing if no town there

		//----------- attack town ----------//

		Town targetTown = TownArray[loc.TownId()];
		int targetTownRecno = targetTown.TownId;
		int targetTownNameId = targetTown.TownNameId;
		int targetTownXLoc = targetTown.LocCenterX;
		int targetTownYLoc = targetTown.LocCenterY;

		// ---------- add indicator on the map ----------//
		if (NationArray.player_recno != 0 && targetTown.NationId == NationArray.player_recno)
			WarPointArray.AddPoint(targetTown.LocCenterX, targetTown.LocCenterY);

		//------------------------------------------------------------------------------//
		// change relation to hostile
		// check for NULL to skip unhandled case by bullets
		// check for SPRITE_DIE to skip the case by EXPLOSIVE_CART
		//------------------------------------------------------------------------------//
		// the target and the attacker's nations are different
		// (it's possible that when a unit who has just changed nation has its bullet hitting its own nation)
		if (attackUnit != null && attackUnit.CurAction != SPRITE_DIE && targetTown.NationId != attackNationRecno)
		{
			int townNationRecno = targetTown.NationId;

			//------- change to hostile relation -------//

			if (attackNationRecno != 0 && targetTown.NationId != 0)
			{
				NationArray[attackNationRecno].set_at_war_today();
				NationArray[targetTown.NationId].set_at_war_today(attackUnit.SpriteId);
			}

			if (targetTown.NationId != 0)
			{
				NationArray[targetTown.NationId].being_attacked(attackNationRecno);
			}

			// don't add the town abandon news that might be called by Town::dec_pop() as the town is actually destroyed not abandoned
			NewsArray.disable();

			targetTown.BeingAttacked(attackUnit.SpriteId, attackDamage);

			NewsArray.enable();

			//------ if the town is destroyed, add a news --------//

			if (TownArray.IsDeleted(targetTownRecno) && townNationRecno == NationArray.player_recno)
			{
				NewsArray.town_destroyed(targetTownNameId, targetTownXLoc, targetTownYLoc, attackUnit, attackNationRecno);
			}

			//---------- gain experience --------//

			if (attackNationRecno != targetTown.NationId)
				attackUnit.GainExperience(); // gain experience to increase combat level

			//------------ auto defense -----------------//

			if (!FirmArray.IsDeleted(targetTownRecno))
				targetTown.AutoDefense(attackUnit.SpriteId);
		}
	}
	
	public void HitWall(Unit attackUnit, int targetXLoc, int targetYLoc, double attackDamage, int attackNationRecno)
	{
		Location loc = World.GetLoc(targetXLoc, targetYLoc);

		/*if(attackUnit!=NULL)
			attackUnit.change_relation(attackNationRecno, loc.wall_nation_recno(), NationBase.NATION_HOSTILE);*/

		//TODO rewrite
		//if (!loc.attack_wall((int)attackDamage))
		//World.correct_wall(targetXLoc, targetYLoc);
	}

	private void TargetMove(Unit targetUnit)
	{
		//------------------------------------------------------------------------------------//
		// checking whether ship can follow to attack target. It is always true if the unit is
		// not ship. 1 for allowing, 0 otherwise
		//------------------------------------------------------------------------------------//
		int allowMove = 1;
		if (SpriteInfo.SpriteSubType == 'M')
		{
			UnitInfo unitInfo = UnitRes[UnitType];
			if (unitInfo.carry_goods_capacity != 0)
			{
				UnitMarine ship = (UnitMarine)this;
				if (ship.auto_mode != 0 && ship.stop_defined_num > 1)
					allowMove = 0;
			}
		}

		//---------------------------------------------------------//
		int targetXLoc = targetUnit.NextLocX;
		int targetYLoc = targetUnit.NextLocY;
		SpriteInfo targetSpriteInfo = targetUnit.SpriteInfo;

		int attackDistance = CalcDistance(targetXLoc, targetYLoc, targetSpriteInfo.LocWidth, targetSpriteInfo.LocHeight);
		ActionLocX2 = ActionLocX = targetXLoc; // update target location
		ActionLocY2 = ActionLocY = targetYLoc;

		//---------------------------------------------------------------------//
		// target is out of attacking range, move closer to it
		//---------------------------------------------------------------------//
		int curXLoc = NextLocX;
		int curYLoc = NextLocY;
		if (attackDistance > AttackRange)
		{
			//---------------- stop all actions if not allow to move -----------------//
			if (allowMove == 0)
			{
				Stop2();
				return;
			}

			//---------------------------------------------------------------------//
			// follow the target using the result_path_dist
			//---------------------------------------------------------------------//
			if (!UpdateAttackPathDist())
			{
				if (CurAction == SPRITE_MOVE || CurAction == SPRITE_WAIT || CurAction == SPRITE_READY_TO_MOVE)
					return;
			}

			if (TryMoveToRangeAttack(targetUnit) != 0)
			{
				//-----------------------------------------------------------------------//
				// reset attack parameters
				//-----------------------------------------------------------------------//
				RangeAttackLocX = RangeAttackLocY = -1;

				// choose better attack mode to attack the target
				ChooseBestAttackMode(attackDistance, targetUnit.MobileType);
			}
		}
		else // attackDistance <= attack_range
		{
			//-----------------------------------------------------------------------------//
			// although the target has moved, the unit can still attack it. no need to move
			//-----------------------------------------------------------------------------//
			if (Math.Abs(CurX - NextX) >= SpriteInfo.Speed || Math.Abs(CurY - NextY) >= SpriteInfo.Speed)
				return; // return as moving

			if (attackDistance == 1 && AttackRange > 1) // may change attack mode
				ChooseBestAttackMode(attackDistance, targetUnit.MobileType);

			if (AttackRange > 1) // range attack
			{
				//------------------ do range attack ----------------------//
				AttackInfo attackInfo = AttackInfos[CurAttack];
				// range attack possible
				bool canAddBullet = BulletArray.add_bullet_possible(curXLoc, curYLoc, MobileType,
					targetXLoc, targetYLoc, targetUnit.MobileType, targetSpriteInfo.LocWidth, targetSpriteInfo.LocHeight,
					out int resultLocX, out int resultLocY, attackInfo.bullet_speed, attackInfo.bullet_sprite_id);
				RangeAttackLocX = resultLocX;
				RangeAttackLocY = resultLocY;
				if (canAddBullet)
				{
					SetCur(NextX, NextY);

					SetAttackDir(curXLoc, curYLoc, RangeAttackLocX, RangeAttackLocY);
					if (ConfigAdv.unit_target_move_range_cycle)
					{
						CycleEqvAttack();
						attackInfo = AttackInfos[CurAttack]; // cur_attack may change
						CurFrame = 1;
					}

					if (IsDirCorrect())
						SetAttack();
					else
						SetTurn();
				}
				else // unable to do range attack, move to target
				{
					if (allowMove == 0)
					{
						Stop2();
						return;
					}

					if (TryMoveToRangeAttack(targetUnit) == 1)
					{
						//range_attack_x_loc = range_attack_y_loc = -1;
						ChooseBestAttackMode(attackDistance, targetUnit.MobileType);
					}
				}
			}
			else if (attackDistance == 1) // close attack
			{
				SetCur(NextX, NextY);
				SetAttackDir(curXLoc, curYLoc, targetXLoc, targetYLoc);
				CurFrame = 1;

				if (IsDirCorrect())
					SetAttack();
				else
					SetTurn();
			}
		}
	}

	private void AttackTarget(Unit targetUnit)
	{
		if (RemainAttackDelay != 0)
			return;

		int unitXLoc = NextLocX;
		int unitYLoc = NextLocY;

		if (AttackRange > 1) // use range attack
		{
			//---------------- use range attack -----------------//
			AttackInfo attackInfo = AttackInfos[CurAttack];

			if (CurFrame != attackInfo.bullet_out_frame)
				return; // wait for bullet_out_frame

			if (!BulletArray.bullet_path_possible(unitXLoc, unitYLoc, MobileType,
				    RangeAttackLocX, RangeAttackLocY, targetUnit.MobileType,
				    attackInfo.bullet_speed, attackInfo.bullet_sprite_id))
			{
				SpriteInfo targetSpriteInfo = targetUnit.SpriteInfo;
				// seek for another possible point to attack if target size > 1x1
				if (targetSpriteInfo.LocWidth > 1 || targetSpriteInfo.LocHeight > 1)
				{
					bool canAddBullet = BulletArray.add_bullet_possible(unitXLoc, unitYLoc, MobileType,
						ActionLocX, ActionLocY, targetUnit.MobileType, targetSpriteInfo.LocWidth, targetSpriteInfo.LocHeight,
						out int resultLocX, out int resultLocY, attackInfo.bullet_speed, attackInfo.bullet_sprite_id);
					RangeAttackLocX = resultLocX;
					RangeAttackLocY = resultLocY;
					if (!canAddBullet)
					{
						//------ no suitable location to attack target by bullet, move to target --------//
						if (PathNodes.Count == 0 || _pathNodeDistance == 0)
							if (TryMoveToRangeAttack(targetUnit) == 0)
								return; // can't reach a location to attack target
					}
				}
			}

			BulletArray.AddBullet(this, targetUnit);
			AddCloseAttackEffect();

			// ------- reduce power --------//
			CurPower -= attackInfo.consume_power;
			if (CurPower < 0) // ***** BUGHERE
				CurPower = 0;
			SetRemainAttackDelay();
			return; // bullet emits
		}
		else // close attack
		{
			//--------------------- close attack ------------------------//
			AttackInfo attackInfo = AttackInfos[CurAttack];

			if (CurFrame == CurSpriteAttack().frameCount)
			{
				if (targetUnit.UnitType == UnitConstants.UNIT_EXPLOSIVE_CART && targetUnit.BelongsToNation(NationId))
					((UnitExpCart)targetUnit).trigger_explode();
				else
					HitTarget(this, targetUnit, ActualDamage(), NationId);

				AddCloseAttackEffect();

				//------- reduce power --------//
				CurPower -= attackInfo.consume_power;
				if (CurPower < 0) // ***** BUGHERE
					CurPower = 0;
				SetRemainAttackDelay();
			}
		}
	}

	private void AddCloseAttackEffect()
	{
		int effectId = AttackInfos[CurAttack].effect_id;
		if (effectId != 0)
		{
			int x = CurX;
			int y = CurY;
			switch (CurDir)
			{
				case 0: // north
					y -= InternalConstants.CellHeight;
					break;
				case 1: // north east
					x += InternalConstants.CellWidth;
					y -= InternalConstants.CellHeight;
					break;
				case 2: // east
					x += InternalConstants.CellWidth;
					break;
				case 3: // south east
					x += InternalConstants.CellWidth;
					y += InternalConstants.CellHeight;
					break;
				case 4: // south
					y += InternalConstants.CellHeight;
					break;
				case 5: // south west
					x -= InternalConstants.CellWidth;
					y += InternalConstants.CellHeight;
					break;
				case 6: // west
					x -= InternalConstants.CellWidth;
					break;
				case 7: // north west
					x -= InternalConstants.CellWidth;
					y -= InternalConstants.CellHeight;
					break;
			}
			EffectArray.AddEffect(effectId, x, y, SPRITE_IDLE, CurDir, MobileType == UnitConstants.UNIT_AIR ? 8 : 2, 0);
		}
	}

	private bool OnWayToAttack(Unit targetUnit)
	{
		if (MobileType == UnitConstants.UNIT_LAND)
		{
			if (AttackRange == 1)
			{
				//------------------------------------------------------------//
				// for close attack, the unit unable to attack the target if
				// it is not in the target surrounding
				//------------------------------------------------------------//
				if (_pathNodeDistance > AttackRange)
					return DetectSurroundTarget();
			}
			else if (_pathNodeDistance != 0 && CurAction != SPRITE_TURN)
			{
				if (DetectSurroundTarget())
					return true; // detect surrounding target while walking
			}
		}

		int targetXLoc = targetUnit.NextLocX;
		int targetYLoc = targetUnit.NextLocY;
		SpriteInfo targetSpriteInfo = targetUnit.SpriteInfo;

		int attackDistance = CalcDistance(targetXLoc, targetYLoc, targetSpriteInfo.LocWidth, targetSpriteInfo.LocHeight);

		if (attackDistance <= AttackRange) // able to attack target
		{
			if ((attackDistance == 1) && AttackRange > 1) // often false condition is checked first
				ChooseBestAttackMode(1, targetUnit.MobileType); // may change to close attack

			if (AttackRange > 1) // use range attack
			{
				SetCur(NextX, NextY);

				AttackInfo attackInfo = AttackInfos[CurAttack];
				int curXLoc = NextLocX;
				int curYLoc = NextLocY;
				bool canAddBullet = BulletArray.add_bullet_possible(curXLoc, curYLoc, MobileType,
					targetXLoc, targetYLoc, targetUnit.MobileType, targetSpriteInfo.LocWidth, targetSpriteInfo.LocHeight,
					out int resultLocX, out int resultLocY, attackInfo.bullet_speed, attackInfo.bullet_sprite_id);
				RangeAttackLocX = resultLocX;
				RangeAttackLocY = resultLocY;
				if (!canAddBullet)
				{
					//------- no suitable location, move to target ---------//
					if (PathNodes.Count == 0 || _pathNodeDistance == 0)
						if (TryMoveToRangeAttack(targetUnit) == 0)
							return false; // can't reach a location to attack target

					return false;
				}

				//---------- able to do range attack ----------//
				SetAttackDir(NextLocX, NextLocY, RangeAttackLocX, RangeAttackLocY);
				CurFrame = 1;

				if (IsDirCorrect())
					SetAttack();
				else
					SetTurn();
			}
			else // close attack
			{
				//---------- attack now ---------//
				SetCur(NextX, NextY);
				TerminateMove();
				SetAttackDir(NextLocX, NextLocY, targetXLoc, targetYLoc);

				if (IsDirCorrect())
					SetAttack();
				else
					SetTurn();
			}
		}

		return false;
	}

	private bool DetectSurroundTarget()
	{
		const int DIMENSION = 3;
		const int CHECK_SIZE = DIMENSION * DIMENSION;

		int curXLoc = NextLocX;
		int curYLoc = NextLocY;
		int checkXLoc, checkYLoc, xShift, yShift;
		Unit target;
		int targetRecno;

		for (int i = 2; i <= CHECK_SIZE; ++i)
		{
			Misc.cal_move_around_a_point(i, DIMENSION, DIMENSION, out xShift, out yShift);
			checkXLoc = curXLoc + xShift;
			checkYLoc = curYLoc + yShift;
			if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
				continue;

			Location loc = World.GetLoc(checkXLoc, checkYLoc);

			if (loc.HasUnit(UnitConstants.UNIT_LAND))
			{
				targetRecno = loc.UnitId(UnitConstants.UNIT_LAND);
				if (UnitArray.IsDeleted(targetRecno))
					continue;

				target = UnitArray[targetRecno];
				if (target.NationId == NationId)
					continue;

				if (IdleDetectUnitChecking(targetRecno))
				{
					AttackUnit(targetRecno, 0, 0, true);
					return true;
				}
			}
		}

		return false;
	}

	private bool UpdateAttackPathDist()
	{
		if (_pathNodeDistance <= 6) //1-6
		{
			return true;
		}
		else if (_pathNodeDistance <= 10) // 8, 10
		{
			return ((_pathNodeDistance - 6) % 2) == 0;
		}
		else if (_pathNodeDistance <= 20) // 15, 20
		{
			return ((_pathNodeDistance - 10) % 5) == 0;
		}
		else if (_pathNodeDistance <= 60) // 28, 36, 44, 52, 60
		{
			return ((_pathNodeDistance - 20) % 8) == 0;
		}
		else if (_pathNodeDistance <= 90) // 75, 90
		{
			return ((_pathNodeDistance - 60) % 15) == 0;
		}
		else // 110, 130, 150, etc
		{
			return ((_pathNodeDistance - 90) % 20) == 0;
		}
	}

	private void SetAttackDir(int curX, int curY, int targetX, int targetY)
	{
		int targetDir = GetDir(curX, curY, targetX, targetY);
		if (UnitRes[UnitType].unit_class == UnitConstants.UNIT_CLASS_SHIP)
		{
			int attackDir1 = (targetDir + 2) % InternalConstants.MAX_SPRITE_DIR_TYPE;
			int attackDir2 = (targetDir + 6) % InternalConstants.MAX_SPRITE_DIR_TYPE;

			if ((attackDir1 + 8 - FinalDir) % InternalConstants.MAX_SPRITE_DIR_TYPE <=
			    (attackDir2 + 8 - FinalDir) % InternalConstants.MAX_SPRITE_DIR_TYPE)
				FinalDir = attackDir1;
			else
				FinalDir = attackDir2;

			AttackDirection = targetDir;
		}
		else
		{
			AttackDirection = targetDir;
			SetDir(targetDir);
		}
	}

	private void SetRemainAttackDelay()
	{
		RemainAttackDelay = AttackInfos[CurAttack].attack_delay;
	}
	
	private bool CanAttackDifferentTargetType()
	{
		int maxRange = MaxAttackRange();
		if (MobileType == UnitConstants.UNIT_LAND && maxRange == 0)
			return false; // unable to do range attack or cannot attack

		return maxRange > 1;
	}

	private bool PossiblePlaceForRangeAttack(int targetXLoc, int targetYLoc, int targetWidth, int targetHeight, int maxRange)
	{
		if (MobileType == UnitConstants.UNIT_AIR)
			return true; // air unit can reach any region

		int curXLoc = NextLocX;
		int curYLoc = NextLocY;

		if (Math.Abs(curXLoc - targetXLoc) <= maxRange && Math.Abs(curYLoc - targetYLoc) <= maxRange) // inside the attack range
			return true;

		//----------------- init parameters -----------------//
		Location loc = World.GetLoc(curXLoc, curYLoc);
		int regionId = loc.RegionId;
		int xLoc1 = Math.Max(targetXLoc - maxRange, 0);
		int yLoc1 = Math.Max(targetYLoc - maxRange, 0);
		int xLoc2 = Math.Min(targetXLoc + targetWidth - 1 + maxRange, GameConstants.MapSize - 1);
		int yLoc2 = Math.Min(targetYLoc + targetHeight - 1 + maxRange, GameConstants.MapSize - 1);
		int checkXLoc, checkYLoc;

		//--------- do adjustment for UnitConstants.UNIT_SEA and UnitConstants.UNIT_AIR ---------//
		if (MobileType != UnitConstants.UNIT_LAND)
		{
			if (xLoc1 % 2 != 0)
				xLoc1++;
			if (yLoc1 % 2 != 0)
				yLoc1++;
			if (xLoc2 % 2 != 0)
				xLoc2--;
			if (yLoc2 % 2 != 0)
				yLoc2--;
		}

		//-------- checking for surrounding location ----------//
		switch (MobileType)
		{
			case UnitConstants.UNIT_LAND:
				for (checkXLoc = xLoc1; checkXLoc <= xLoc2; checkXLoc++)
				{
					loc = World.GetLoc(checkXLoc, yLoc1);
					if (loc.RegionId == regionId && loc.IsAccessible(MobileType))
						return true;

					loc = World.GetLoc(checkXLoc, yLoc2);
					if (loc.RegionId == regionId && loc.IsAccessible(MobileType))
						return true;
				}

				for (checkYLoc = yLoc1 + 1; checkYLoc < yLoc2; checkYLoc++)
				{
					loc = World.GetLoc(xLoc1, checkYLoc);
					if (loc.RegionId == regionId && loc.IsAccessible(MobileType))
						return true;

					loc = World.GetLoc(xLoc2, checkYLoc);
					if (loc.RegionId == regionId && loc.IsAccessible(MobileType))
						return true;
				}

				break;

			case UnitConstants.UNIT_SEA:
				for (checkXLoc = xLoc1; checkXLoc <= xLoc2; checkXLoc++)
				{
					if (checkXLoc % 2 == 0 && yLoc1 % 2 == 0)
					{
						loc = World.GetLoc(checkXLoc, yLoc1);
						if (loc.RegionId == regionId && loc.IsAccessible(MobileType))
							return true;
					}

					if (checkXLoc % 2 == 0 && yLoc2 % 2 == 0)
					{
						loc = World.GetLoc(checkXLoc, yLoc2);
						if (loc.RegionId == regionId && loc.IsAccessible(MobileType))
							return true;
					}
				}

				for (checkYLoc = yLoc1 + 1; checkYLoc < yLoc2; checkYLoc++)
				{
					if (xLoc1 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.GetLoc(xLoc1, checkYLoc);
						if (loc.RegionId == regionId && loc.IsAccessible(MobileType))
							return true;
					}

					if (xLoc2 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.GetLoc(xLoc2, checkYLoc);
						if (loc.RegionId == regionId && loc.IsAccessible(MobileType))
							return true;
					}
				}

				break;

			case UnitConstants.UNIT_AIR:
				for (checkXLoc = xLoc1; checkXLoc <= xLoc2; checkXLoc++)
				{
					if (checkXLoc % 2 == 0 && yLoc1 % 2 == 0)
					{
						loc = World.GetLoc(checkXLoc, yLoc1);
						if (loc.IsAccessible(MobileType))
							return true;
					}

					if (checkXLoc % 2 == 0 && yLoc2 % 2 == 0)
					{
						loc = World.GetLoc(checkXLoc, yLoc2);
						if (loc.IsAccessible(MobileType))
							return true;
					}
				}

				for (checkYLoc = yLoc1 + 1; checkYLoc < yLoc2; checkYLoc++)
				{
					if (xLoc1 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.GetLoc(xLoc1, checkYLoc);
						if (loc.IsAccessible(MobileType))
							return true;
					}

					if (xLoc2 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.GetLoc(xLoc2, checkYLoc);
						if (loc.IsAccessible(MobileType))
							return true;
					}
				}

				break;
		}

		return false;
	}

	private bool SpaceForAttack(int targetXLoc, int targetYLoc, int targetMobileType, int targetWidth, int targetHeight)
	{
		if (MobileType == UnitConstants.UNIT_LAND && targetMobileType == UnitConstants.UNIT_LAND)
			return SpaceAroundTarget(targetXLoc, targetYLoc, targetWidth, targetHeight);

		if ((MobileType == UnitConstants.UNIT_SEA && targetMobileType == UnitConstants.UNIT_SEA) ||
		    (MobileType == UnitConstants.UNIT_AIR && targetMobileType == UnitConstants.UNIT_AIR))
			return SpaceAroundTargetVer2(targetXLoc, targetYLoc, targetWidth, targetHeight);

		//-------------------------------------------------------------------------//
		// mobile_type is different from that of target unit
		//-------------------------------------------------------------------------//
		Location loc = World.GetLoc(NextLocX, NextLocY);
		if (MobileType == UnitConstants.UNIT_LAND && targetMobileType == UnitConstants.UNIT_SEA &&
		    !CanAttackDifferentTargetType() && ShipSurrHasFreeLand(targetXLoc, targetYLoc, loc.RegionId))
			return true;

		int maxRange = MaxAttackRange();
		if (maxRange == 1)
			return false;

		if (FreeSpaceForRangeAttack(targetXLoc, targetYLoc, targetWidth, targetHeight, targetMobileType, maxRange))
			return true;

		return false;
	}

	private bool SpaceAroundTarget(int squareXLoc, int squareYLoc, int width, int height)
	{
		//				edge 1
		//				1 1 4
		// edge 2	2 x 4		edge 4
		//				2 3 3
		//				edge3

		Location loc;
		Unit unit;
		byte sum, locWeight;
		int testXLoc, testYLoc, i, equal = 1;

		//------------------ top edge ---------------//
		sum = 0;
		if ((testYLoc = squareYLoc - 1) >= 0)
		{
			if (squareXLoc >= 1) // have upper left corner
			{
				i = -1;
				locWeight = 1;
			}
			else
			{
				i = 0;
				locWeight = 2;
			}

			for (; i < width; i++, locWeight <<= 1)
			{
				loc = World.GetLoc(squareXLoc + i, testYLoc);
				if (loc.CanMove(MobileType))
					sum ^= locWeight;
				else if (loc.HasUnit(MobileType))
				{
					unit = UnitArray[loc.UnitId(MobileType)];
					if (unit.CurAction != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (BlockedEdges[0] != sum)
		{
			BlockedEdges[0] = sum;
			equal = 0;
		}

		//----------------- left edge -----------------//
		sum = 0;
		if ((testXLoc = squareXLoc - 1) >= 0)
		{
			if (squareYLoc + height <= GameConstants.MapSize - 1) // have lower left corner
			{
				i = height;
				locWeight = 1;
			}
			else
			{
				i = height - 1;
				locWeight = 2;
			}

			for (; i >= 0; i--, locWeight <<= 1)
			{
				loc = World.GetLoc(testXLoc, squareYLoc + i);
				if (loc.CanMove(MobileType))
					sum ^= locWeight;
				else if (loc.HasUnit(MobileType))
				{
					unit = UnitArray[loc.UnitId(MobileType)];
					if (unit.CurAction != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (BlockedEdges[1] != sum)
		{
			BlockedEdges[1] = sum;
			equal = 0;
		}

		//------------------- bottom edge ------------------//
		sum = 0;
		if ((testYLoc = squareYLoc + height) <= GameConstants.MapSize - 1)
		{
			if (squareXLoc + width <= GameConstants.MapSize - 1) // have lower right corner
			{
				i = width;
				locWeight = 1;
			}
			else
			{
				i = width - 1;
				locWeight = 2;
			}

			for (; i >= 0; i--, locWeight <<= 1)
			{
				loc = World.GetLoc(squareXLoc + i, testYLoc);
				if (loc.CanMove(MobileType))
					sum ^= locWeight;
				else if (loc.HasUnit(MobileType))
				{
					unit = UnitArray[loc.UnitId(MobileType)];
					if (unit.CurAction != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (BlockedEdges[2] != sum)
		{
			BlockedEdges[2] = sum;
			equal = 0;
		}

		//---------------------- right edge ----------------------//
		sum = 0;
		if ((testXLoc = squareXLoc + width) <= GameConstants.MapSize - 1)
		{
			if (squareYLoc >= 1) // have upper right corner
			{
				i = -1;
				locWeight = 1;
			}
			else
			{
				i = 0;
				locWeight = 2;
			}

			for (; i < height; i++, locWeight <<= 1)
			{
				loc = World.GetLoc(testXLoc, squareYLoc + i);
				if (loc.CanMove(MobileType))
					sum ^= locWeight;
				else if (loc.HasUnit(MobileType))
				{
					unit = UnitArray[loc.UnitId(MobileType)];
					if (unit.CurAction != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (BlockedEdges[3] != sum)
		{
			BlockedEdges[3] = sum;
			equal = 0;
		}

		return equal == 0;
	}

	private bool SpaceAroundTargetVer2(int targetXLoc, int targetYLoc, int targetWidth, int targetHeight)
	{
		Location loc;
		Unit unit;
		byte sum, locWeight;
		int xLoc1, yLoc1, xLoc2, yLoc2;
		int i, equal = 1;
		//int testXLoc, testYLoc, 

		xLoc1 = (targetXLoc % 2 != 0) ? targetXLoc - 1 : targetXLoc - 2;
		yLoc1 = (targetYLoc % 2 != 0) ? targetYLoc - 1 : targetYLoc - 2;
		xLoc2 = ((targetXLoc + targetWidth - 1) % 2 != 0) ? targetXLoc + targetWidth : targetXLoc + targetWidth + 1;
		yLoc2 = ((targetYLoc + targetHeight - 1) % 2 != 0) ? targetYLoc + targetHeight : targetYLoc + targetHeight + 1;

		//------------------------ top edge ------------------------//
		sum = 0;
		if (yLoc1 >= 0)
		{
			if (xLoc1 >= 0)
			{
				i = xLoc1;
				locWeight = 1;
			}
			else
			{
				i = xLoc1 + 2;
				locWeight = 2;
			}

			for (; i <= xLoc2; i += 2, locWeight <<= 1)
			{
				loc = World.GetLoc(i, yLoc1);
				if (loc.CanMove(MobileType))
					sum ^= locWeight;
				else if (loc.HasUnit(MobileType))
				{
					unit = UnitArray[loc.UnitId(MobileType)];
					if (unit.CurAction != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (BlockedEdges[0] != sum)
		{
			BlockedEdges[0] = sum;
			equal = 0;
		}

		//---------------------- left edge -----------------------//
		sum = 0;
		if (xLoc1 >= 0)
		{
			if (yLoc2 <= GameConstants.MapSize - 1)
			{
				i = yLoc2;
				locWeight = 1;
			}
			else
			{
				i = yLoc2 - 2;
				locWeight = 2;
			}

			for (; i > yLoc1; i -= 2, locWeight <<= 1)
			{
				loc = World.GetLoc(xLoc1, i);
				if (loc.CanMove(MobileType))
					sum ^= locWeight;
				else if (loc.HasUnit(MobileType))
				{
					unit = UnitArray[loc.UnitId(MobileType)];
					if (unit.CurAction != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (BlockedEdges[1] != sum)
		{
			BlockedEdges[1] = sum;
			equal = 0;
		}

		//----------------------- bottom edge ---------------------------//
		sum = 0;
		if (yLoc2 <= GameConstants.MapSize - 1)
		{
			if (xLoc2 <= GameConstants.MapSize - 1)
			{
				i = xLoc2;
				locWeight = 1;
			}
			else
			{
				i = xLoc2 - 2;
				locWeight = 2;
			}

			for (; i > xLoc1; i -= 2, locWeight <<= 1)
			{
				loc = World.GetLoc(i, yLoc2);
				if (loc.CanMove(MobileType))
					sum ^= locWeight;
				else if (loc.HasUnit(MobileType))
				{
					unit = UnitArray[loc.UnitId(MobileType)];
					if (unit.CurAction != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (BlockedEdges[2] != sum)
		{
			BlockedEdges[2] = sum;
			equal = 0;
		}

		//---------------------- right edge ------------------------//
		sum = 0;
		if (xLoc2 <= GameConstants.MapSize - 1)
		{
			if (yLoc1 >= 0)
			{
				i = yLoc1;
				locWeight = 1;
			}
			else
			{
				i = yLoc1 + 2;
				locWeight = 2;
			}

			for (; i < yLoc2; i += 2, locWeight <<= 1)
			{
				loc = World.GetLoc(xLoc2, i);
				if (loc.CanMove(MobileType))
					sum ^= locWeight;
				else if (loc.HasUnit(MobileType))
				{
					unit = UnitArray[loc.UnitId(MobileType)];
					if (unit.CurAction != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (BlockedEdges[3] != sum)
		{
			BlockedEdges[3] = sum;
			equal = 0;
		}

		return equal == 0;
	}

	private bool ShipSurrHasFreeLand(int targetXLoc, int targetYLoc, int regionId)
	{
		for (int i = 2; i < 9; i++)
		{
			Misc.cal_move_around_a_point(i, 3, 3, out int xShift, out int yShift);
			int checkXLoc = targetXLoc + xShift;
			int checkYLoc = targetYLoc + yShift;

			if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
				continue;

			Location loc = World.GetLoc(checkXLoc, checkYLoc);
			if (loc.RegionId == regionId && loc.CanMove(MobileType))
				return true;
		}

		return false;
	}

	private bool FreeSpaceForRangeAttack(int targetXLoc, int targetYLoc, int targetWidth, int targetHeight, int targetMobileType, int maxRange)
	{
		//if(mobile_type==UnitConstants.UNIT_AIR)
		//	return true; // air unit can reach any region

		int curXLoc = NextLocX;
		int curYLoc = NextLocY;

		if (Math.Abs(curXLoc - targetXLoc) <= maxRange && Math.Abs(curYLoc - targetYLoc) <= maxRange) // inside the attack range
			return true;

		Location loc = World.GetLoc(curXLoc, curYLoc);
		int regionId = loc.RegionId;
		int xLoc1 = Math.Max(targetXLoc - maxRange, 0);
		int yLoc1 = Math.Max(targetYLoc - maxRange, 0);
		int xLoc2 = Math.Min(targetXLoc + targetWidth - 1 + maxRange, GameConstants.MapSize - 1);
		int yLoc2 = Math.Min(targetYLoc + targetHeight - 1 + maxRange, GameConstants.MapSize - 1);
		int checkXLoc, checkYLoc;

		//--------- do adjustment for UnitConstants.UNIT_SEA and UnitConstants.UNIT_AIR ---------//
		if (MobileType != UnitConstants.UNIT_LAND)
		{
			if (xLoc1 % 2 != 0)
				xLoc1++;
			if (yLoc1 % 2 != 0)
				yLoc1++;
			if (xLoc2 % 2 != 0)
				xLoc2--;
			if (yLoc2 % 2 != 0)
				yLoc2--;
		}

		//-------- checking for surrounding location ----------//
		switch (MobileType)
		{
			case UnitConstants.UNIT_LAND:
				for (checkXLoc = xLoc1; checkXLoc <= xLoc2; checkXLoc++)
				{
					loc = World.GetLoc(checkXLoc, yLoc1);
					if (loc.RegionId == regionId && loc.CanMove(MobileType))
						return true;

					loc = World.GetLoc(checkXLoc, yLoc2);
					if (loc.RegionId == regionId && loc.CanMove(MobileType))
						return true;
				}

				for (checkYLoc = yLoc1 + 1; checkYLoc < yLoc2; checkYLoc++)
				{
					loc = World.GetLoc(xLoc1, checkYLoc);
					if (loc.RegionId == regionId && loc.CanMove(MobileType))
						return true;

					loc = World.GetLoc(xLoc2, checkYLoc);
					if (loc.RegionId == regionId && loc.CanMove(MobileType))
						return true;
				}

				break;

			case UnitConstants.UNIT_SEA:
				for (checkXLoc = xLoc1; checkXLoc <= xLoc2; checkXLoc++)
				{
					if (checkXLoc % 2 == 0 && yLoc1 % 2 == 0)
					{
						loc = World.GetLoc(checkXLoc, yLoc1);
						if (loc.RegionId == regionId && loc.CanMove(MobileType))
							return true;
					}

					if (checkXLoc % 2 == 0 && yLoc2 % 2 == 0)
					{
						loc = World.GetLoc(checkXLoc, yLoc2);
						if (loc.RegionId == regionId && loc.CanMove(MobileType))
							return true;
					}
				}

				for (checkYLoc = yLoc1 + 1; checkYLoc < yLoc2; checkYLoc++)
				{
					if (xLoc1 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.GetLoc(xLoc1, checkYLoc);
						if (loc.RegionId == regionId && loc.CanMove(MobileType))
							return true;
					}

					if (xLoc2 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.GetLoc(xLoc2, checkYLoc);
						if (loc.RegionId == regionId && loc.CanMove(MobileType))
							return true;
					}
				}

				break;

			case UnitConstants.UNIT_AIR:
				for (checkXLoc = xLoc1; checkXLoc <= xLoc2; checkXLoc++)
				{
					if (checkXLoc % 2 == 0 && yLoc1 % 2 == 0)
					{
						loc = World.GetLoc(checkXLoc, yLoc1);
						if (loc.CanMove(MobileType))
							return true;
					}

					if (checkXLoc % 2 == 0 && yLoc2 % 2 == 0)
					{
						loc = World.GetLoc(checkXLoc, yLoc2);
						if (loc.CanMove(MobileType))
							return true;
					}
				}

				for (checkYLoc = yLoc1 + 1; checkYLoc < yLoc2; checkYLoc++)
				{
					if (xLoc1 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.GetLoc(xLoc1, checkYLoc);
						if (loc.CanMove(MobileType))
							return true;
					}

					if (xLoc2 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.GetLoc(xLoc2, checkYLoc);
						if (loc.CanMove(MobileType))
							return true;
					}
				}

				break;
		}

		return false;
	}

	private void ChooseBestAttackMode(int attackDistance, int targetMobileType = UnitConstants.UNIT_LAND)
	{
		//------------ enable/disable range attack -----------//
		//cur_attack = 0;
		//return;

		//-------------------- define parameters -----------------------//
		int attackModeBeingUsed = CurAttack;
		//UCHAR maxAttackRangeMode = 0;
		int maxAttackRangeMode = CurAttack;
		AttackInfo attackInfoMaxRange = AttackInfos[0];
		AttackInfo attackInfoChecking;
		AttackInfo attackInfoSelected = AttackInfos[CurAttack];

		//--------------------------------------------------------------//
		// If targetMobileType==UnitConstants.UNIT_AIR or mobile_type==UnitConstants.UNIT_AIR,
		//	force to use range_attack.
		// If there is no range_attack, return 0, i.e. cur_attack=0
		//--------------------------------------------------------------//
		if (AttackCount > 1)
		{
			bool canAttack = false;

			for (int i = 0; i < AttackCount; i++)
			{
				if (attackModeBeingUsed == i)
					continue; // it is the mode already used

				attackInfoChecking = AttackInfos[i];
				if (CanAttackWith(attackInfoChecking) && attackInfoChecking.attack_range >= attackDistance)
				{
					//-------------------- able to attack ----------------------//
					canAttack = true;

					if (attackInfoSelected.attack_range < attackDistance)
					{
						attackModeBeingUsed = i;
						attackInfoSelected = attackInfoChecking;
						continue;
					}

					int checkingDamageWeight = attackInfoChecking.attack_damage;
					int selectedDamageWeight = attackInfoSelected.attack_damage;

					if (attackDistance == 1 &&
					    (targetMobileType != UnitConstants.UNIT_AIR && MobileType != UnitConstants.UNIT_AIR))
					{
						//------------ force to use close attack if possible -----------//
						if (attackInfoSelected.attack_range == attackDistance)
						{
							if (attackInfoChecking.attack_range == attackDistance && checkingDamageWeight > selectedDamageWeight)
							{
								attackModeBeingUsed = i; // choose the one with strongest damage
								attackInfoSelected = attackInfoChecking;
							}

							continue;
						}
						else if (attackInfoChecking.attack_range == 1)
						{
							attackModeBeingUsed = i;
							attackInfoSelected = attackInfoChecking;
							continue;
						}
					}

					//----------------------------------------------------------------------//
					// further selection
					//----------------------------------------------------------------------//
					if (checkingDamageWeight == selectedDamageWeight)
					{
						if (attackInfoChecking.attack_range < attackInfoSelected.attack_range)
						{
							if (attackInfoChecking.attack_range > 1 ||
							    (targetMobileType != UnitConstants.UNIT_AIR && MobileType != UnitConstants.UNIT_AIR))
							{
								//--------------------------------------------------------------------------//
								// select one with shortest attack_range
								//--------------------------------------------------------------------------//
								attackModeBeingUsed = i;
								attackInfoSelected = attackInfoChecking;
							}
						}
					}
					else
					{
						//--------------------------------------------------------------------------//
						// select one that can do the attacking immediately with the strongest damage point
						//--------------------------------------------------------------------------//
						attackModeBeingUsed = i;
						attackInfoSelected = attackInfoChecking;
					}
				}

				if (!canAttack)
				{
					//------------------------------------------------------------------------------//
					// if unable to attack the target, choose the mode with longer attack_range and
					// heavier damage
					//------------------------------------------------------------------------------//
					if (CanAttackWith(attackInfoChecking) &&
					    (attackInfoChecking.attack_range > attackInfoMaxRange.attack_range ||
					     (attackInfoChecking.attack_range == attackInfoMaxRange.attack_range &&
					      attackInfoChecking.attack_damage > attackInfoMaxRange.attack_damage)))
					{
						maxAttackRangeMode = i;
						attackInfoMaxRange = attackInfoChecking;
					}
				}
			}

			if (canAttack)
				CurAttack = attackModeBeingUsed; // choose the strongest damage mode if able to attack
			else
				CurAttack = maxAttackRangeMode; //	choose the longest attack range if unable to attack

			AttackRange = AttackInfos[CurAttack].attack_range;
		}
		else
		{
			CurAttack = 0; // only one mode is supported
			AttackRange = AttackInfos[0].attack_range;
		}
	}
	
	private int MaxAttackRange()
	{
		int maxRange = 0;

		for (int i = 0; i < AttackCount; i++)
		{
			AttackInfo attackInfo = AttackInfos[i];
			if (CanAttackWith(attackInfo) && attackInfo.attack_range > maxRange)
				maxRange = attackInfo.attack_range;
		}

		return maxRange;
	}
	
	private bool CanAttackWith(int i) // 0 to attack_count-1
	{
		return CanAttackWith(AttackInfos[i]);
	}

	private bool CanAttackWith(AttackInfo attackInfo)
	{
		return Skill.CombatLevel >= attackInfo.combat_level && CurPower >= attackInfo.min_power;
	}

	public void CycleEqvAttack()
	{
		if (AttackInfos[CurAttack].eqv_attack_next > 0)
		{
			do
			{
				CurAttack = AttackInfos[CurAttack].eqv_attack_next - 1;
			} while (!CanAttackWith(CurAttack));
		}
		else
		{
			if (!CanAttackWith(CurAttack))
			{
				// force to search again
				int attackRange = AttackInfos[CurAttack].attack_range;
				for (int i = 0; i < AttackCount; i++)
				{
					AttackInfo attackInfo = AttackInfos[i];
					if (attackInfo.attack_range >= attackRange && CanAttackWith(attackInfo))
					{
						CurAttack = i;
						break;
					}
				}
			}
		}
	}

	protected virtual void FixAttackInfo() // set AttackInfos appropriately
	{
		UnitInfo unitInfo = UnitRes[UnitType];

		AttackCount = unitInfo.attack_count;

		if (AttackCount > 0 && unitInfo.first_attack > 0)
		{
			AttackInfos = new AttackInfo[AttackCount];
			for (int i = 0; i < AttackCount; i++)
			{
				AttackInfos[i] = UnitRes.attack_info_array[unitInfo.first_attack - 1 + i];
			}
		}
		else
		{
			AttackInfos = Array.Empty<AttackInfo>();
		}

		int oldAttackCount = AttackCount;
		int techLevel = WeaponVersion;
		if (unitInfo.unit_class == UnitConstants.UNIT_CLASS_WEAPON && techLevel > 0)
		{
			switch (UnitType)
			{
				case UnitConstants.UNIT_BALLISTA:
				case UnitConstants.UNIT_F_BALLISTA:
					AttackCount = 2;
					break;
				case UnitConstants.UNIT_EXPLOSIVE_CART:
					AttackCount = 0;
					break;
				default:
					AttackCount = 1;
					break;
			}

			if (AttackCount > 0)
			{
				//TODO check this
				AttackInfos = new AttackInfo[AttackCount];
				for (int i = 0; i < AttackCount; i++)
				{
					AttackInfos[i] = UnitRes.attack_info_array[oldAttackCount + (techLevel - 1) * AttackCount + i];
				}
			}
			else
			{
				// no attack like explosive cart
				AttackInfos = Array.Empty<AttackInfo>();
			}
		}
	}
	
	public bool IsAttackAction()
	{
		switch (ActionMode2)
		{
			case UnitConstants.ACTION_ATTACK_UNIT:
			case UnitConstants.ACTION_ATTACK_FIRM:
			case UnitConstants.ACTION_ATTACK_TOWN:
			case UnitConstants.ACTION_ATTACK_WALL:
			case UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET:
			case UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET:
			case UnitConstants.ACTION_AUTO_DEFENSE_BACK_CAMP:
			case UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET:
			case UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET:
			case UnitConstants.ACTION_DEFEND_TOWN_BACK_TOWN:
			case UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET:
			case UnitConstants.ACTION_MONSTER_DEFEND_DETECT_TARGET:
			case UnitConstants.ACTION_MONSTER_DEFEND_BACK_FIRM:
				return true;

			default:
				return false;
		}
	}
	
	public void DefenseAttackUnit(int targetRecno)
	{
		ActionMode2 = UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET;
		AttackUnit(targetRecno, 0, 0, true);
	}

	public void DefenseAttackFirm(int targetXLoc, int targetYLoc)
	{
		ActionMode2 = UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET;
		AttackFirm(targetXLoc, targetYLoc);
	}

	public void DefenseAttackTown(int targetXLoc, int targetYLoc)
	{
		ActionMode2 = UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET;
		AttackTown(targetXLoc, targetYLoc);
	}

	public void DefenseAttackWall(int targetXLoc, int targetYLoc)
	{
		ActionMode2 = UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET;
		AttackWall(targetXLoc, targetYLoc);
	}

	public void DefenseDetectTarget()
	{
		ActionMode2 = UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET;
		ActionPara2 = UnitConstants.AUTO_DEFENSE_DETECT_COUNT;
		ActionLocX2 = -1;
		ActionLocY2 = -1;
	}

	private void GeneralDefendModeDetectTarget(int checkDefendMode = 0)
	{
		Stop();
		switch (ActionMode2)
		{
			case UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET:
				DefenseDetectTarget();
				break;

			case UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET:
				DefendTownDetectTarget();
				break;

			case UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET:
				MonsterDefendDetectTarget();
				break;
		}
	}

	private bool GeneralDefendModeProcessAttackTarget()
	{
		Unit unit = null;
		Firm firm = null;
		int clearToDetect = 0;

		//------------------------------------------------------------------------------//
		// if the unit's action mode is in defensive attack action, process the corresponding checking.
		//------------------------------------------------------------------------------//
		switch (ActionMode)
		{
			case UnitConstants.ACTION_ATTACK_UNIT:
				if (UnitArray.IsDeleted(ActionPara2))
				{
					clearToDetect++;
				}
				else
				{
					unit = UnitArray[ActionPara2];

					//if(unit.cur_action==SPRITE_IDLE)
					//	clearToDetect++;

					if (!CanAttackNation(unit.NationId)) // cannot attack this nation
						clearToDetect++;
				}

				break;

			case UnitConstants.ACTION_ATTACK_FIRM:
				if (FirmArray.IsDeleted(ActionPara2))
				{
					clearToDetect++;
				}
				else
				{
					firm = FirmArray[ActionPara2];

					if (!CanAttackNation(firm.nation_recno)) // cannot attack this nation
						clearToDetect++;
				}

				break;

			case UnitConstants.ACTION_ATTACK_TOWN:
				if (TownArray.IsDeleted(ActionPara2))
				{
					clearToDetect++;
				}
				else
				{
					Town town = TownArray[ActionPara2];

					if (!CanAttackNation(town.NationId)) // cannot attack this nation
						clearToDetect++;
				}

				break;

			case UnitConstants.ACTION_ATTACK_WALL:
				Location location = World.GetLoc(ActionLocX2, ActionLocY2);
				if (!location.IsWall() || !CanAttackNation(location.PowerNationId))
					clearToDetect++;
				break;

			default:
				clearToDetect++;
				break;
		}

		//------------------------------------------------------------------------------//
		// situation changed to defensive detecting mode
		//------------------------------------------------------------------------------//
		if (clearToDetect != 0)
		{
			//----------------------------------------------------------//
			// target is dead, change to detect state for another target
			//----------------------------------------------------------//
			ResetActionParameters();
			return true;
		}
		else if (WaitingTerm < UnitConstants.ATTACK_WAITING_TERM)
			WaitingTerm++;
		else
		{
			//------------------------------------------------------------------------------//
			// process the corresponding attacking procedure.
			//------------------------------------------------------------------------------//
			WaitingTerm = 0;
			switch (ActionMode)
			{
				case UnitConstants.ACTION_ATTACK_UNIT:
					SpriteInfo spriteInfo = unit.SpriteInfo;

					//-----------------------------------------------------------------//
					// attack the target if able to reach the target surrounding, otherwise continue to wait
					//-----------------------------------------------------------------//
					ActionLocX2 = unit.NextLocX; // update target location
					ActionLocY2 = unit.NextLocY;
					if (SpaceForAttack(ActionLocX2, ActionLocY2, unit.MobileType, spriteInfo.LocWidth, spriteInfo.LocHeight))
						AttackUnit(unit.SpriteId, 0, 0, true);
					break;

				case UnitConstants.ACTION_ATTACK_FIRM:
					FirmInfo firmInfo = FirmRes[firm.firm_id];

					//-----------------------------------------------------------------//
					// attack the target if able to reach the target surrounding, otherwise continue to wait
					//-----------------------------------------------------------------//
					AttackFirm(ActionLocX2, ActionLocY2);

					if (!IsInSurrounding(MoveToLocX, MoveToLocY, SpriteInfo.LocWidth,
						    ActionLocX2, ActionLocY2, firmInfo.loc_width, firmInfo.loc_height))
						WaitingTerm = 0;
					break;

				case UnitConstants.ACTION_ATTACK_TOWN:
					//-----------------------------------------------------------------//
					// attack the target if able to reach the target surrounding, otherwise continue to wait
					//-----------------------------------------------------------------//
					AttackTown(ActionLocX2, ActionLocY2);

					if (!IsInSurrounding(MoveToLocX, MoveToLocY, SpriteInfo.LocWidth,
						    ActionLocX2, ActionLocY2, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT))
						WaitingTerm = 0;
					break;

				case UnitConstants.ACTION_ATTACK_WALL:
					AttackWall(ActionLocX2, ActionLocY2);
					if (!IsInSurrounding(MoveToLocX, MoveToLocY, SpriteInfo.LocWidth,
						    ActionLocX2, ActionLocY2, 1, 1))
						WaitingTerm = 0;
					break;
			}
		}

		return false;
	}

	//========== unit's defense mode ==========//
	private void DefenseBackCamp(int firmXLoc, int firmYLoc)
	{
		Assign(firmXLoc, firmYLoc);
		ActionMode2 = UnitConstants.ACTION_AUTO_DEFENSE_BACK_CAMP;
	}

	private void ProcessAutoDefenseAttackTarget()
	{
		if (GeneralDefendModeProcessAttackTarget())
		{
			DefenseDetectTarget();
		}
	}

	private void ProcessAutoDefenseDetectTarget()
	{
		//----------------------------------------------------------------//
		// no target or target is out of detect range, so change state to back camp
		//----------------------------------------------------------------//
		if (ActionPara2 == 0)
		{
			if (FirmArray.IsDeleted(ActionMiscParam))
			{
				ProcessAutoDefenseBackCamp();
				return;
			}

			Firm firm = FirmArray[ActionMiscParam];
			if (firm.firm_id != Firm.FIRM_CAMP || firm.nation_recno != NationId)
			{
				ProcessAutoDefenseBackCamp();
				return;
			}

			FirmCamp camp = (FirmCamp)firm;
			if (UnitArray.IsDeleted(camp.defend_target_recno))
			{
				ProcessAutoDefenseBackCamp();
				return;
			}

			Unit target = UnitArray[camp.defend_target_recno];
			if (target.ActionMode != UnitConstants.ACTION_ATTACK_FIRM || target.ActionParam != camp.firm_recno)
			{
				ProcessAutoDefenseBackCamp();
				return;
			}

			//action_mode2 = UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET;
			ActionPara2 = UnitConstants.AUTO_DEFENSE_DETECT_COUNT;
			return;
		}

		//----------------------------------------------------------------//
		// defense_detecting target algorithm
		//----------------------------------------------------------------//
		int startLoc;
		int dimension;

		switch (ActionPara2 % InternalConstants.FRAMES_PER_DAY)
		{
			case 3:
				startLoc = 2; // 1-7, check 224 = 15^2-1
				dimension = 7;
				break;

			case 2:
				startLoc = 122; // 6-8, check 168 = 17^2-11^2
				dimension = 8;
				break;

			case 1:
				startLoc = 170; // 7-9, check 192 = 19^2-13^2
				dimension = UnitConstants.EFFECTIVE_AUTO_DEFENSE_DISTANCE;
				break;

			default:
				ActionPara2--;
				return;
		}

		//---------------------------------------------------------------//
		// attack the target if target detected, or change the detect region
		//---------------------------------------------------------------//
		if (!IdleDetectAttack(startLoc, dimension, 1)) // defense mode is on
			ActionPara2--;
	}

	private void ProcessAutoDefenseBackCamp()
	{
		int clearDefenseMode = 0;
		// the unit may become idle or unable to reach firm, reactivate it
		if (ActionMode != UnitConstants.ACTION_ASSIGN_TO_FIRM)
		{
			if (ActionMisc != UnitConstants.ACTION_MISC_DEFENSE_CAMP_RECNO ||
			    ActionMiscParam == 0 || FirmArray.IsDeleted(ActionMiscParam))
				clearDefenseMode++;
			else
			{
				Firm firm = FirmArray[ActionMiscParam];
				if (firm.firm_id != Firm.FIRM_CAMP || firm.nation_recno != NationId)
					clearDefenseMode++;
				else
				{
					DefenseBackCamp(firm.loc_x1, firm.loc_y1); // go back to the military camp
					return;
				}
			}
		}
		else if (CurAction == SPRITE_IDLE)
		{
			if (FirmArray.IsDeleted(ActionMiscParam))
				clearDefenseMode++;
			else
			{
				Firm firm = FirmArray[ActionMiscParam];
				DefenseBackCamp(firm.loc_x1, firm.loc_y1);
				return;
			}
		}

		//----------------------------------------------------------------//
		// clear order if the camp is deleted
		//----------------------------------------------------------------//
		Stop2();
		ResetActionMiscParameters();
	}

	private bool DefenseFollowTarget()
	{
		const int PROB_HOSTILE_RETURN = 10;
		const int PROB_FRIENDLY_RETURN = 20;
		const int PROB_NEUTRAL_RETURN = 30;

		if (UnitArray.IsDeleted(ActionParam))
			return true;

		if (CurAction == SPRITE_ATTACK)
			return true;

		Unit target = UnitArray[ActionParam];
		Location loc = World.GetLoc(ActionLocX, ActionLocY);
		if (!loc.HasUnit(target.MobileType))
			return true; // the target may be dead or invisible

		int returnFactor;

		//-----------------------------------------------------------------//
		// calculate the chance to go back to military camp in following the target
		//-----------------------------------------------------------------//
		if (loc.PowerNationId == NationId)
			return true; // target within our nation
		else if (loc.PowerNationId == 0) // is neutral
			returnFactor = PROB_NEUTRAL_RETURN;
		else
		{
			Nation locNation = NationArray[loc.PowerNationId];
			if (locNation.get_relation_status(NationId) == NationBase.NATION_HOSTILE)
				returnFactor = PROB_HOSTILE_RETURN;
			else
				returnFactor = PROB_FRIENDLY_RETURN;
		}

		SpriteInfo targetSpriteInfo = target.SpriteInfo;

		//-----------------------------------------------------------------//
		// if the target moves faster than this unit, it is more likely for
		// this unit to go back to military camp.
		//-----------------------------------------------------------------//
		//-**** should also consider the combat level and hit_points of both unit ****-//
		if (targetSpriteInfo.Speed > SpriteInfo.Speed)
			returnFactor -= 5;

		if (Misc.Random(returnFactor) != 0) // return to camp if true
			return true;

		ProcessAutoDefenseBackCamp();
		return false; // cancel attack
	}

	//========== town unit's defend mode ==========//
	private void DefendTownAttackUnit(int targetRecno)
	{
		ActionMode2 = UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET;
		AttackUnit(targetRecno, 0, 0, true);
	}

	private void DefendTownDetectTarget()
	{
		ActionMode2 = UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET;
		ActionPara2 = UnitConstants.UNIT_DEFEND_TOWN_DETECT_COUNT;
		ActionLocX2 = -1;
		ActionLocY2 = -1;
	}

	private void DefendTownBackTown(int townRecno)
	{
		Town town = TownArray[townRecno];
		Assign(town.LocX1, town.LocY1);
		ActionMode2 = UnitConstants.ACTION_DEFEND_TOWN_BACK_TOWN;
	}

	private void ProcessDefendTownAttackTarget()
	{
		if (GeneralDefendModeProcessAttackTarget())
		{
			ActionMode2 = UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET;
			ActionPara2 = UnitConstants.UNIT_DEFEND_TOWN_DETECT_COUNT;
			ActionLocX2 = ActionLocY2 = -1;
		}
	}

	private void ProcessDefendTownDetectTarget()
	{
		//----------------------------------------------------------------//
		// no target or target is out of detect range, so change state to back camp
		//----------------------------------------------------------------//
		if (ActionPara2 == 0)
		{
			int back = 0;

			if (TownArray.IsDeleted(ActionMiscParam))
				back++;
			else
			{
				Town town = TownArray[ActionMiscParam];
				if (UnitArray.IsDeleted(town.DefendTargetId))
					back++;
				else
				{
					Unit target = UnitArray[town.DefendTargetId];
					if (target.ActionMode != UnitConstants.ACTION_ATTACK_TOWN || target.ActionParam != town.TownId)
						back++;
				}
			}

			if (back == 0)
			{
				//action_mode2 = ACTION_DEFEND_TOWN_DETECT_TARGET;
				ActionPara2 = UnitConstants.UNIT_DEFEND_TOWN_DETECT_COUNT;
				return;
			}

			ProcessDefendTownBackTown();
			return;
		}

		//----------------------------------------------------------------//
		// defense_detecting target algorithm
		//----------------------------------------------------------------//
		int startLoc;
		int dimension;

		switch (ActionPara2 % InternalConstants.FRAMES_PER_DAY)
		{
			case 3:
				startLoc = 2; // 1-7, check 224 = 15^2-1
				dimension = 7;
				break;

			case 2:
				startLoc = 122; // 6-8, check 168 = 17^2-11^2
				dimension = 8;
				break;

			case 1:
				startLoc = 170; // 7-9, check 192 = 19^2-13^2
				dimension = UnitConstants.EFFECTIVE_DEFEND_TOWN_DISTANCE;
				break;

			default:
				ActionPara2--;
				return;
		}

		//---------------------------------------------------------------//
		// attack the target if target detected, or change the detect region
		//---------------------------------------------------------------//

		if (!IdleDetectAttack(startLoc, dimension, 1)) // defense mode is on
			ActionPara2--;
	}

	private void ProcessDefendTownBackTown()
	{
		int clearDefenseMode = 0;
		// the unit may become idle or unable to reach town, reactivate it
		if (ActionMode != UnitConstants.ACTION_ASSIGN_TO_TOWN)
		{
			if (ActionMisc != UnitConstants.ACTION_MISC_DEFEND_TOWN_RECNO ||
			    ActionMiscParam == 0 || TownArray.IsDeleted(ActionMiscParam))
				clearDefenseMode++;
			else
			{
				Town town = TownArray[ActionMiscParam];
				if (town.NationId != NationId)
					clearDefenseMode++;
				else
				{
					DefendTownBackTown(ActionMiscParam); // go back to the town
					return;
				}
			}
		}
		else if (CurAction == SPRITE_IDLE)
		{
			if (TownArray.IsDeleted(ActionMiscParam))
				clearDefenseMode++;
			else
			{
				DefendTownBackTown(ActionMiscParam);
				return;
			}
		}

		//----------------------------------------------------------------//
		// clear order if the town is deleted
		//----------------------------------------------------------------//
		Stop2();
		ResetActionMiscParameters();
	}

	private bool DefendTownFollowTarget()
	{
		if (CurAction == SPRITE_ATTACK)
			return true;

		if (TownArray.IsDeleted(UnitModeParam))
		{
			Stop2(); //**** BUGHERE
			SetMode(0); //***BUGHERE
			return false;
		}

		int curXLoc = NextLocX;
		int curYLoc = NextLocY;

		Town town = TownArray[UnitModeParam];
		if ((curXLoc < town.LocCenterX - UnitConstants.UNIT_DEFEND_TOWN_DISTANCE) ||
		    (curXLoc > town.LocCenterX + UnitConstants.UNIT_DEFEND_TOWN_DISTANCE) ||
		    (curYLoc < town.LocCenterY - UnitConstants.UNIT_DEFEND_TOWN_DISTANCE) ||
		    (curYLoc > town.LocCenterY + UnitConstants.UNIT_DEFEND_TOWN_DISTANCE))
		{
			DefendTownBackTown(UnitModeParam);
			return false;
		}

		return true;
	}

	private void MonsterDefendAttackUnit(int targetRecno)
	{
		ActionMode2 = UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET;
		AttackUnit(targetRecno, 0, 0, true);
	}

	private void MonsterDefendAttackFirm(int targetXLoc, int targetYLoc)
	{
		ActionMode2 = UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET;
		AttackFirm(targetXLoc, targetYLoc);
	}

	private void MonsterDefendAttackTown(int targetXLoc, int targetYLoc)
	{
		ActionMode2 = UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET;
		AttackTown(targetXLoc, targetYLoc);
	}

	private void MonsterDefendAttackWall(int targetXLoc, int targetYLoc)
	{
		ActionMode2 = UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET;
		AttackWall(targetXLoc, targetYLoc);
	}

	private void MonsterDefendDetectTarget()
	{
		ActionMode2 = UnitConstants.ACTION_MONSTER_DEFEND_DETECT_TARGET;
		ActionPara2 = UnitConstants.MONSTER_DEFEND_DETECT_COUNT;
		ActionLocX2 = -1;
		ActionLocY2 = -1;
	}

	private void MonsterDefendBackFirm(int firmXLoc, int firmYLoc)
	{
		Assign(firmXLoc, firmYLoc);
		ActionMode2 = UnitConstants.ACTION_MONSTER_DEFEND_BACK_FIRM;
	}

	private void ProcessMonsterDefendAttackTarget()
	{
		if (GeneralDefendModeProcessAttackTarget())
		{
			MonsterDefendDetectTarget();
		}
	}

	private void ProcessMonsterDefendDetectTarget()
	{
		//----------------------------------------------------------------//
		// no target or target is out of detect range, so change state to back camp
		//----------------------------------------------------------------//
		if (ActionPara2 == 0)
		{
			int back = 0;

			if (FirmArray.IsDeleted(ActionMiscParam))
				back++;
			else
			{
				FirmMonster firmMonster = (FirmMonster)FirmArray[ActionMiscParam];
				if (UnitArray.IsDeleted(firmMonster.defend_target_recno))
					back++;
				else
				{
					Unit target = UnitArray[firmMonster.defend_target_recno];
					if (target.ActionMode != UnitConstants.ACTION_ATTACK_FIRM || target.ActionParam != firmMonster.firm_recno)
						back++;
				}
			}

			if (back == 0)
			{
				//action_mode2 = ACTION_MONSTER_DEFEND_DETECT_TARGET;
				ActionPara2 = UnitConstants.MONSTER_DEFEND_DETECT_COUNT;
				return;
			}

			ProcessMonsterDefendBackFirm();
			return;
		}

		//----------------------------------------------------------------//
		// defense_detecting target algorithm
		//----------------------------------------------------------------//
		int startLoc;
		int dimension;

		switch (ActionPara2 % InternalConstants.FRAMES_PER_DAY)
		{
			case 3:
				startLoc = 2; // 1-7, check 224 = 15^2-1
				dimension = 7;
				break;

			case 2:
				startLoc = 122; // 6-8, check 168 = 17^2-11^2
				dimension = 8;
				break;

			case 1:
				startLoc = 170; // 7-9, check 192 = 19^2-13^2
				dimension = UnitConstants.EFFECTIVE_MONSTER_DEFEND_FIRM_DISTANCE;
				break;

			default:
				ActionPara2--;
				return;
		}

		//---------------------------------------------------------------//
		// attack the target if target detected, or change the detect region
		//---------------------------------------------------------------//
		if (!IdleDetectAttack(startLoc, dimension, 1)) // defense mode is on
			ActionPara2--;
	}

	private void ProcessMonsterDefendBackFirm()
	{
		int clearDefendMode = 0;
		// the unit may become idle or unable to reach firm, reactivate it
		if (ActionMode != UnitConstants.ACTION_ASSIGN_TO_FIRM)
		{
			if (ActionMisc != UnitConstants.ACTION_MISC_MONSTER_DEFEND_FIRM_RECNO ||
			    ActionMiscParam == 0 || FirmArray.IsDeleted(ActionMiscParam))
				clearDefendMode++;
			else
			{
				Firm firm = FirmArray[ActionMiscParam];
				if (firm.firm_id != Firm.FIRM_MONSTER || firm.nation_recno != NationId)
					clearDefendMode++;
				else
				{
					MonsterDefendBackFirm(firm.loc_x1, firm.loc_y1); // go back to the military camp
					return;
				}
			}
		}
		else if (CurAction == SPRITE_IDLE)
		{
			if (FirmArray.IsDeleted(ActionMiscParam))
				clearDefendMode++;
			else
			{
				Firm firm = FirmArray[ActionMiscParam];
				MonsterDefendBackFirm(firm.loc_x1, firm.loc_y1);
				return;
			}
		}

		//----------------------------------------------------------------//
		// clear order if the camp is deleted
		//----------------------------------------------------------------//
		Stop2();
		ResetActionMiscParameters();
	}

	private bool MonsterDefendFollowTarget()
	{
		if (CurAction == SPRITE_ATTACK)
			return true;
		/*
		if(FirmArray.IsDeleted(action_misc_para))
		{
			stop2(); //**** BUGHERE
			//set_mode(0); //***BUGHERE
			if(monster_array.IsDeleted(unit_mode_para))
				return 0;

			Monster *monster = monster_array[unit_mode_para];
			monster.firm_recno = 0;
			return 0;
		}
		*/

		//--------------------------------------------------------------------------------//
		// choose to return to firm
		//--------------------------------------------------------------------------------//
		int curXLoc = NextLocX;
		int curYLoc = NextLocY;

		Firm firm = FirmArray[ActionMiscParam];
		if ((curXLoc < firm.center_x - UnitConstants.MONSTER_DEFEND_FIRM_DISTANCE) ||
		    (curXLoc > firm.center_x + UnitConstants.MONSTER_DEFEND_FIRM_DISTANCE) ||
		    (curYLoc < firm.center_y - UnitConstants.MONSTER_DEFEND_FIRM_DISTANCE) ||
		    (curYLoc > firm.center_y + UnitConstants.MONSTER_DEFEND_FIRM_DISTANCE))
		{
			MonsterDefendBackFirm(firm.loc_x1, firm.loc_y1);
			return false;
		}

		return true;
	}
	
	private bool IdleDetectAttack(int startLoc = 0, int dimensionInput = 0, int defenseMode = 0)
	{
		//---------------------------------------------------//
		// Set detectDelay.
		//
		// The larger its value, the less CPU time it will takes,
		// but it will also take longer to detect enemies.
		//---------------------------------------------------//

		int detectDelay = 1;

		Location loc;
		Unit unit;
		int targetMobileType;
		int countLimit;
		int targetRecno;
		bool idle_detect_default_mode = (startLoc == 0 && dimensionInput == 0 && defenseMode == 0); //----- true when all zero
		IdleDetectHasUnit = IdleDetectHasFirm = IdleDetectHasTown = IdleDetectHasWall = false;
		HelpMode = UnitConstants.HELP_NOTHING;

		//-----------------------------------------------------------------------------------------------//
		// adjust waiting_term for default_mode
		//-----------------------------------------------------------------------------------------------//
		++WaitingTerm;
		WaitingTerm = Math.Max(WaitingTerm, 0); //**BUGHERE
		int lowestBit = WaitingTerm % detectDelay;

		if (ActionMode2 == UnitConstants.ACTION_STOP)
		{
			WaitingTerm = lowestBit;
		}

		int dimension = (dimensionInput != 0 ? dimensionInput : UnitConstants.ATTACK_DETECT_DISTANCE) << 1;
		dimension++;
		countLimit = dimension * dimension;
		int i = startLoc != 0 ? startLoc : 1 + lowestBit;
		int incAmount = (idle_detect_default_mode) ? detectDelay : 1;

		//-----------------------------------------------------------------------------------------------//
		// check the location around the unit
		//
		// The priority to choose target is (value of targetType)
		// 1) unit, 2) firm, 3) wall
		//-----------------------------------------------------------------------------------------------//
		for (; i <= countLimit; i += incAmount) // 1 is the self location
		{
			int xOffset, yOffset;
			Misc.cal_move_around_a_point(i, dimension, dimension, out xOffset, out yOffset);
			int checkXLoc = MoveToLocX + xOffset;
			int checkYLoc = MoveToLocY + yOffset;
			if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
				continue;

			//------------------ verify location ---------------//
			loc = World.GetLoc(checkXLoc, checkYLoc);
			if (defenseMode != 0 && ActionMode2 != UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET)
			{
				if (ActionMode2 == UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET)
					if (loc.PowerNationId != NationId && loc.PowerNationId != 0)
						continue; // skip this location because it is not neutral nation or our nation
			}

			//----------------------------------------------------------------------------//
			// checking the target type
			//----------------------------------------------------------------------------//
			if ((targetMobileType = loc.HasAnyUnit(i == 1 ? MobileType : UnitConstants.UNIT_LAND)) != 0 &&
			    (targetRecno = loc.UnitId(targetMobileType)) != 0 && !UnitArray.IsDeleted(targetRecno))
			{
				//=================== is unit ======================//
				if (IdleDetectHasUnit || (ActionParam == targetRecno &&
				                             ActionMode == UnitConstants.ACTION_ATTACK_UNIT &&
				                             checkXLoc == ActionLocX && checkYLoc == ActionLocY))
					continue; // same target as before

				unit = UnitArray[targetRecno];
				if (NationId != 0 && unit.NationId == NationId && HelpMode != UnitConstants.HELP_ATTACK_UNIT)
					IdleDetectHelperAttack(targetRecno); // help our troop
				else if ((HelpMode == UnitConstants.HELP_ATTACK_UNIT && HelpAttackTargetId == targetRecno) ||
				         (unit.NationId != NationId && IdleDetectUnitChecking(targetRecno)))
				{
					IdleDetectTargetUnitId = targetRecno;
					IdleDetectHasUnit = true;
					break; // break with highest priority
				}
			}
			else if (loc.IsFirm() && (targetRecno = loc.FirmId()) != 0)
			{
				//=============== is firm ===============//
				if (IdleDetectHasFirm || (ActionParam == targetRecno &&
				                             ActionMode == UnitConstants.ACTION_ATTACK_FIRM &&
				                             ActionLocX == checkXLoc && ActionLocY == checkYLoc))
					continue; // same target as before

				if (IdleDetectFirmChecking(targetRecno) != 0)
				{
					IdleDetectTargetFirmId = targetRecno;
					IdleDetectHasFirm = true;
				}
			}
			/*else if(loc.is_town() && (targetRecno = loc.town_recno()))
			{
			   //=============== is town ===========//
			   if(idle_detect_has_town || (action_para==targetRecno && action_mode==ACTION_ATTACK_TOWN &&
			      action_x_loc==checkXLoc && action_y_loc==checkYLoc))
			      continue; // same target as before
	  
			   if(idle_detect_town_checking(targetRecno))
			   {
					  idle_detect_target_town_recno = targetRecno;
					  idle_detect_has_town++;
			   }
			}
			else if(loc.is_wall())
			{
			   //================ is wall ==============//
			   if(idle_detect_has_wall || (action_mode==ACTION_ATTACK_WALL && action_para==targetRecno &&
			      action_x_loc==checkXLoc && action_y_loc==checkYLoc))
			      continue; // same target as before
	  
			   if(idle_detect_wall_checking(checkXLoc, checkYLoc))
			   {
					  idle_detect_target_wall_x1 = checkXLoc;
					  idle_detect_target_wall_y1 = checkYLoc;
					  idle_detect_has_wall++;
			   }
			}*/

			//if(hasUnit && hasFirm && hasTown && hasWall)
			//if(hasUnit && hasFirm && hasWall)
			//  break; // there is target for attacking
		}

		return IdleDetectChooseTarget(defenseMode);
	}

	private bool IdleDetectChooseTarget(int defenseMode)
	{
		//-----------------------------------------------------------------------------------------------//
		// Decision making for choosing target to attack
		//-----------------------------------------------------------------------------------------------//
		if (defenseMode != 0)
		{
			if (ActionMode2 == UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET)
			{
				//----------- defense units allow to attack units and firms -----------//

				if (IdleDetectHasUnit)
					DefenseAttackUnit(IdleDetectTargetUnitId);
				else if (IdleDetectHasFirm)
				{
					Firm targetFirm = FirmArray[IdleDetectTargetFirmId];
					DefenseAttackFirm(targetFirm.loc_x1, targetFirm.loc_y1);
				}
				/*else if(idle_detect_has_town)
				{
					Town *targetTown = TownArray[idle_detect_target_town_recno];
					defense_attack_town(targetTown.loc_x1, targetTown.loc_y1);
				}
				else if(idle_detect_has_wall)
				   defense_attack_wall(idle_detect_target_wall_x1, idle_detect_target_wall_y1);*/
				else
					return false;

				return true;
			}
			else if (ActionMode2 == UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET)
			{
				//----------- town units only attack units ------------//

				if (IdleDetectHasUnit)
					DefendTownAttackUnit(IdleDetectTargetUnitId);
				else
					return false;

				return true;
			}
			else if (ActionMode2 == UnitConstants.ACTION_MONSTER_DEFEND_DETECT_TARGET)
			{
				//---------- monsters can attack units and firms -----------//

				if (IdleDetectHasUnit)
					MonsterDefendAttackUnit(IdleDetectTargetUnitId);
				else if (IdleDetectHasFirm)
				{
					Firm targetFirm = FirmArray[IdleDetectTargetFirmId];
					MonsterDefendAttackFirm(targetFirm.loc_x1, targetFirm.loc_y1);
				}
				/*else if(idle_detect_has_town)
				{
				   Town *targetTown = TownArray[idle_detect_target_town_recno];
				   monster_defend_attack_town(targetTown.loc_x1, targetTown.loc_y1);
				}
				else if(idle_detect_has_wall)
				   monster_defend_attack_wall(idle_detect_target_wall_x1, idle_detect_target_wall_y1);*/
				else
					return false;

				return true;
			}
		}
		else // default mode
		{
			int rc = 0;

			if (IdleDetectHasUnit)
			{
				AttackUnit(IdleDetectTargetUnitId, 0, 0, true);

				//--- set the original position of the target, so the unit won't chase too far away ---//

				Unit unit = UnitArray[IdleDetectTargetUnitId];

				OriginalTargetLocX = unit.NextLocX;
				OriginalTargetLocY = unit.NextLocY;

				rc = 1;
			}

			else if (HelpMode == UnitConstants.HELP_ATTACK_UNIT)
			{
				AttackUnit(HelpAttackTargetId, 0, 0, true);

				//--- set the original position of the target, so the unit won't chase too far away ---//

				Unit unit = UnitArray[HelpAttackTargetId];

				OriginalTargetLocX = unit.NextLocX;
				OriginalTargetLocY = unit.NextLocY;

				rc = 1;
			}
			else if (IdleDetectHasFirm)
			{
				Firm targetFirm = FirmArray[IdleDetectTargetFirmId];
				AttackFirm(targetFirm.loc_x1, targetFirm.loc_y1);
			}
			/*else if(idle_detect_has_town)
			{
				Town *targetTown = TownArray[idle_detect_target_town_recno];
				attack_town(targetTown.loc_x1, targetTown.loc_y1);
			}
			else if(idle_detect_has_wall)
				attack_wall(idle_detect_target_wall_x1, idle_detect_target_wall_y1);*/
			else
				return false;

			//---- set original action vars ----//

			if (rc != 0 && OriginalActionMode == 0)
			{
				OriginalActionMode = UnitConstants.ACTION_MOVE;
				OriginalActionParam = 0;
				OriginalActionLocX = NextLocX;
				OriginalActionLocY = NextLocY;
			}

			return true;
		}

		return false;
	}

	private void IdleDetectHelperAttack(int unitRecno)
	{
		const int HELP_DISTANCE = 15;

		Unit unit = UnitArray[unitRecno];
		if (unit.UnitType == UnitConstants.UNIT_CARAVAN)
			return;

		//char	actionMode;
		int actionPara = 0;
		//short actionXLoc, actionYLoc;
		int isUnit = 0;

		//------------- is the unit attacking other unit ------------//
		switch (unit.ActionMode2)
		{
			case UnitConstants.ACTION_ATTACK_UNIT:
				actionPara = unit.ActionPara2;
				isUnit++;
				break;

			default:
				switch (unit.ActionMode)
				{
					case UnitConstants.ACTION_ATTACK_UNIT:
						actionPara = unit.ActionParam;
						isUnit++;
						break;
				}

				break;
		}

		if (isUnit != 0 && !UnitArray.IsDeleted(actionPara))
		{
			Unit targetUnit = UnitArray[actionPara];

			if (targetUnit.NationId == NationId)
				return;

			// the targetUnit this unit is attacking may have entered a
			// building by now due to processing order -- skip this one
			if (!targetUnit.IsVisible())
				return;

			if (Misc.points_distance(NextLocX, NextLocY, targetUnit.NextLocX, targetUnit.NextLocY) < HELP_DISTANCE)
			{
				if (IdleDetectUnitChecking(actionPara))
				{
					HelpAttackTargetId = actionPara;
					HelpMode = UnitConstants.HELP_ATTACK_UNIT;
				}

				for (int i = 0; i < BlockedEdges.Length; i++)
					BlockedEdges[i] = 0;
			}
		}
	}

	private bool IdleDetectUnitChecking(int targetRecno)
	{
		Unit targetUnit = UnitArray[targetRecno];

		if (targetUnit.UnitType == UnitConstants.UNIT_CARAVAN)
			return false;

		//-------------------------------------------//
		// If the target is moving, don't attack it.
		// Only attack when the unit stands still or
		// is attacking.
		//-------------------------------------------//

		if (targetUnit.CurAction != SPRITE_ATTACK && targetUnit.CurAction != SPRITE_IDLE)
			return false;

		//-------------------------------------------//
		// If the target is a spy of our own and the
		// notification flag is set to 0, then don't
		// attack.
		//-------------------------------------------//

		if (targetUnit.SpyId != 0) // if the target unit is our spy, don't attack 
		{
			Spy spy = SpyArray[targetUnit.SpyId];

			if (spy.TrueNationId == NationId && spy.NotifyCloakedNation == 0)
				return false;
		}

		if (SpyId != 0) // if this unit is our spy, don't attack own units
		{
			Spy spy = SpyArray[SpyId];

			if (spy.TrueNationId == targetUnit.NationId && spy.NotifyCloakedNation == 0)
				return false;
		}

		SpriteInfo spriteInfo = targetUnit.SpriteInfo;
		Nation nation = NationId != 0 ? NationArray[NationId] : null;
		int targetNationRecno = targetUnit.NationId;

		//-------------------------------------------------------------------//
		// checking nation relationship
		//-------------------------------------------------------------------//

		if (NationId != 0)
		{
			if (targetNationRecno != 0)
			{
				//------- don't attack own units and non-hostile units -------//

				//--------------------------------------------------------------//
				// if the unit is hostile, only attack if should_attack flag to
				// that nation is true or the unit is attacking somebody or something.
				//--------------------------------------------------------------//
				NationRelation nationRelation = nation.get_relation(targetNationRecno);

				if (nationRelation.status != NationBase.NATION_HOSTILE || !nationRelation.should_attack)
					return false;
			}
			else if (!targetUnit.CanIndependentUnitAttackNation(NationId))
				return false;
		}
		else if (!CanIndependentUnitAttackNation(targetNationRecno)) // independent unit
			return false;

		//---------------------------------------------//
		if (SpaceForAttack(targetUnit.NextLocX, targetUnit.NextLocY, targetUnit.MobileType, spriteInfo.LocWidth, spriteInfo.LocHeight))
			return true;
		else
			return false;
	}

	private int IdleDetectFirmChecking(int targetRecno)
	{
		Firm firm = FirmArray[targetRecno];

		//------------ code to select firm for attacking -----------//
		switch (firm.firm_id)
		{
			case Firm.FIRM_CAMP:
			case Firm.FIRM_BASE:
			case Firm.FIRM_WAR_FACTORY:
				break;

			default:
				return 0;
		}

		Nation nation = NationId != 0 ? NationArray[NationId] : null;
		int targetNationRecno = firm.nation_recno;
		int targetMobileType = MobileType == UnitConstants.UNIT_SEA ? UnitConstants.UNIT_SEA : UnitConstants.UNIT_LAND;

		//-------------------------------------------------------------------------------//
		// checking nation relationship
		//-------------------------------------------------------------------------------//
		if (NationId != 0)
		{
			if (targetNationRecno != 0)
			{
				//------- don't attack own units and non-hostile units -------//

				if (targetNationRecno == NationId)
					return 0;

				//--------------------------------------------------------------//
				// if the unit is hostile, only attack if should_attack flag to
				// that nation is true or the unit is attacking somebody or something.
				//--------------------------------------------------------------//

				NationRelation nationRelation = nation.get_relation(targetNationRecno);

				if (nationRelation.status != NationBase.NATION_HOSTILE || !nationRelation.should_attack)
					return 0;
			}
			else // independent firm
			{
				FirmMonster monsterFirm = (FirmMonster)FirmArray[targetRecno];

				if (!monsterFirm.is_hostile_nation(NationId))
					return 0;
			}
		}
		else if (!CanIndependentUnitAttackNation(targetNationRecno)) // independent town
			return 0;

		FirmInfo firmInfo = FirmRes[firm.firm_id];
		if (SpaceForAttack(firm.loc_x1, firm.loc_y1, UnitConstants.UNIT_LAND, firmInfo.loc_width, firmInfo.loc_height))
			return 1;
		else
			return 0;
	}

	public bool InAutoDefenseMode()
	{
		return (ActionMode2 >= UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
		        ActionMode2 <= UnitConstants.ACTION_AUTO_DEFENSE_BACK_CAMP);
	}

	public bool InDefendTownMode()
	{
		return (ActionMode2 >= UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
		        ActionMode2 <= UnitConstants.ACTION_DEFEND_TOWN_BACK_TOWN);
	}

	public bool InMonsterDefendMode()
	{
		return (ActionMode2 >= UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET &&
		        ActionMode2 <= UnitConstants.ACTION_MONSTER_DEFEND_BACK_FIRM);
	}

	private bool InAnyDefenseMode()
	{
		return ActionMode2 >= UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
		       ActionMode2 <= UnitConstants.ACTION_MONSTER_DEFEND_BACK_FIRM;
	}
	
	public void ClearUnitDefenseMode()
	{
		//------- cancel defense mode and continue the current action -------//
		ActionMode2 = ActionMode;
		ActionPara2 = ActionParam;
		ActionLocX2 = ActionLocX;
		ActionLocY2 = ActionLocY;

		ResetActionMiscParameters();

		if (UnitMode == UnitConstants.UNIT_MODE_DEFEND_TOWN)
			SetMode(0); // reset unit mode 
	}

	public void ClearTownDefendMode()
	{
		//------- cancel defense mode and continue the current action -------//
		ActionMode2 = ActionMode;
		ActionPara2 = ActionParam;
		ActionLocX2 = ActionLocX;
		ActionLocY2 = ActionLocY;

		ResetActionMiscParameters();
	}

	public void ClearMonsterDefendMode()
	{
		//------- cancel defense mode and continue the current action -------//
		ActionMode2 = ActionMode;
		ActionPara2 = ActionParam;
		ActionLocX2 = ActionLocX;
		ActionLocY2 = ActionLocY;

		ResetActionMiscParameters();
	}
}