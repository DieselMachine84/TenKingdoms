using System;

namespace TenKingdoms;

public partial class Unit
{
	protected void process_attack_unit()
	{
		if (!can_attack())
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//------- if the targeted unit has been destroyed --------//
		if (action_para == 0)
			return;

		//--------------------------------------------------------------------------//
		// stop if the targeted unit has been killed or target belongs our nation
		//--------------------------------------------------------------------------//
		int clearOrder = 0;
		Unit targetUnit = null;

		if (UnitArray.IsDeleted(action_para) || action_para == sprite_recno)
		{
			if (!ConfigAdv.unit_finish_attack_move || cur_action == SPRITE_ATTACK)
				clearOrder++;
			else
			{
				// keep attack action alive to finish movement before going idle
				invalidate_attack_target();
				return;
			}
		}
		else
		{
			targetUnit = UnitArray[action_para];
			if (targetUnit.nation_recno != 0 && !nation_can_attack(targetUnit.nation_recno) &&
			    targetUnit.unit_id != UnitConstants.UNIT_EXPLOSIVE_CART) // cannot attack this nation
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

			stop2(UnitConstants.KEEP_DEFENSE_MODE);

			return;
		}

		//--------------------------------------------------------------------------//
		// stop action if target goes into town/firm, ships (go to other territory)
		//--------------------------------------------------------------------------//
		if (!targetUnit.is_visible())
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE); // clear order
			return;
		}

		//------------------------------------------------------------//
		// if the caravan is entered a firm, attack the firm
		//------------------------------------------------------------//
		if (targetUnit.caravan_in_firm())
		{
			//----- for caravan entering the market -------//
			// the current firm recno of the firm the caravan entered is stored in action_para
			if (FirmArray.IsDeleted(targetUnit.action_para))
				stop2(UnitConstants.KEEP_DEFENSE_MODE); // clear order
			else
			{
				Firm firm = FirmArray[targetUnit.action_para];
				attack_firm(firm.loc_x1, firm.loc_y1);
			}

			return;
		}

		//---------------- define parameters ---------------------//
		int targetXLoc = targetUnit.next_x_loc();
		int targetYLoc = targetUnit.next_y_loc();
		int spriteXLoc = next_x_loc();
		int spriteYLoc = next_y_loc();
		AttackInfo attackInfo = attack_info_array[cur_attack];

		//------------------------------------------------------------//
		// If this unit's target has moved, change the destination accordingly.
		//------------------------------------------------------------//
		if (targetXLoc != action_x_loc || targetYLoc != action_y_loc)
		{
			target_move(targetUnit);
			if (action_mode == UnitConstants.ACTION_STOP)
				return;
		}

		//-----------------------------------------------------//
		// If the unit is currently attacking somebody.
		//-----------------------------------------------------//
		//if( cur_action==SPRITE_ATTACK && next_x==cur_x && next_y==cur_y)

		if (Math.Abs(cur_x - next_x) <= sprite_info.speed && Math.Abs(cur_y - next_y) <= sprite_info.speed)
		{
			if (cur_action == SPRITE_ATTACK)
			{
				attack_target(targetUnit);
			}
			else
			{
				//-----------------------------------------------------//
				// If the unit is on its way to attack somebody and it
				// has got close next to the target, attack now
				//-----------------------------------------------------//
				on_way_to_attack(targetUnit);
			}
		}
	}

	protected void process_attack_firm()
	{
		if (!can_attack())
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//------- if the targeted firm has been destroyed --------//
		if (action_para == 0)
			return;

		Firm targetFirm = null;
		int clearOrder = 0;
		//------------------------------------------------------------//
		// check attack conditions
		//------------------------------------------------------------//
		if (FirmArray.IsDeleted(action_para))
		{
			if (!ConfigAdv.unit_finish_attack_move || cur_action == SPRITE_ATTACK)
				clearOrder++;
			else
			{
				// keep attack action alive to finish movement before going idle
				invalidate_attack_target();
				return;
			}
		}
		else
		{
			targetFirm = FirmArray[action_para];

			if (!nation_can_attack(targetFirm.nation_recno)) // cannot attack this nation
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

			stop2(UnitConstants.KEEP_DEFENSE_MODE);

			return;
		}

		//-----------------------------------------------------//
		// If the unit is currently attacking somebody.
		//-----------------------------------------------------//
		if (cur_action == SPRITE_ATTACK)
		{
			if (remain_attack_delay != 0)
				return;

			AttackInfo attackInfo = attack_info_array[cur_attack];
			if (attackInfo.attack_range > 1) // range attack
			{
				//--------- wait for bullet emit ----------//
				if (cur_frame != attackInfo.bullet_out_frame)
					return;

				//------- seek location to attack by bullet ----------//
				int curXLoc = next_x_loc();
				int curYLoc = next_y_loc();
				if (!BulletArray.bullet_path_possible(curXLoc, curYLoc, mobile_type,
					    range_attack_x_loc, range_attack_y_loc, UnitConstants.UNIT_LAND,
					    attackInfo.bullet_speed, attackInfo.bullet_sprite_id))
				{
					FirmInfo firmInfo = FirmRes[targetFirm.firm_id];
					if (!BulletArray.add_bullet_possible(curXLoc, curYLoc, mobile_type, action_x_loc, action_y_loc,
						    UnitConstants.UNIT_LAND, firmInfo.loc_width, firmInfo.loc_height,
						    out range_attack_x_loc, out range_attack_y_loc, attackInfo.bullet_speed,
						    attackInfo.bullet_sprite_id))
					{
						//------- no suitable location, so move to target again ---------//
						set_move_to_surround(action_x_loc, action_y_loc, firmInfo.loc_width, firmInfo.loc_height,
							UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO);
						return;
					}
				}

				//--------- add bullet, bullet emits ----------//
				BulletArray.AddBullet(this, action_x_loc, action_y_loc);
				add_close_attack_effect();

				// ------- reduce power --------//
				cur_power -= attackInfo.consume_power;
				if (cur_power < 0) // ***** BUGHERE
					cur_power = 0;
				set_remain_attack_delay();
				return;
			}
			else // close attack
			{
				if (cur_frame != cur_sprite_attack().frame_count)
					return; // is attacking

				hit_firm(this, action_x_loc, action_y_loc, actual_damage(), nation_recno);
				add_close_attack_effect();

				// ------- reduce power --------//
				cur_power -= attackInfo.consume_power;
				if (cur_power < 0) // ***** BUGHERE
					cur_power = 0;
				set_remain_attack_delay();
			}
		}
		//--------------------------------------------------------------------------------------------------//
		// If the unit is on its way to attack somebody, if it has gotten close next to the target, attack now
		//--------------------------------------------------------------------------------------------------//
		// it has moved to the specified location. check cur_x & go_x to make sure the sprite has completely move to the location, not just crossing it.
		else if (Math.Abs(cur_x - next_x) <= sprite_info.speed && Math.Abs(cur_y - next_y) <= sprite_info.speed)
		{
			if (mobile_type == UnitConstants.UNIT_LAND)
			{
				if (detect_surround_target())
					return;
			}

			if (attack_range == 1)
			{
				//------------------------------------------------------------//
				// for close attack, the unit unable to attack the firm if
				// it is not in the firm surrounding
				//------------------------------------------------------------//
				if (_pathNodeDistance > attack_range)
					return;
			}

			FirmInfo firmInfo = FirmRes[targetFirm.firm_id];
			int targetXLoc = targetFirm.loc_x1;
			int targetYLoc = targetFirm.loc_y1;

			int attackDistance = cal_distance(targetXLoc, targetYLoc, firmInfo.loc_width, firmInfo.loc_height);
			int curXLoc = next_x_loc();
			int curYLoc = next_y_loc();

			if (attackDistance <= attack_range) // able to attack target
			{
				if ((attackDistance == 1) && attack_range > 1) // often false condition is checked first
					choose_best_attack_mode(1); // may change to use close attack

				if (attack_range > 1) // use range attack
				{
					set_cur(next_x, next_y);

					AttackInfo attackInfo = attack_info_array[cur_attack];
					if (!BulletArray.add_bullet_possible(curXLoc, curYLoc, mobile_type, targetXLoc, targetYLoc,
						    UnitConstants.UNIT_LAND, firmInfo.loc_width, firmInfo.loc_height,
						    out range_attack_x_loc, out range_attack_y_loc, attackInfo.bullet_speed,
						    attackInfo.bullet_sprite_id))
					{
						//------- no suitable location, move to target ---------//
						if (PathNodes.Count  == 0) // no step for continue moving
							set_move_to_surround(action_x_loc, action_y_loc, firmInfo.loc_width, firmInfo.loc_height,
								UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO);

						return; // unable to attack, continue to move
					}

					//---------- able to do range attack ----------//
					set_attack_dir(curXLoc, curYLoc, range_attack_x_loc, range_attack_y_loc);
					cur_frame = 1;

					if (is_dir_correct())
						set_attack();
					else
						set_turn();
				}
				else // close attack
				{
					//---------- attack now ---------//
					set_cur(next_x, next_y);
					terminate_move();

					if (targetFirm.firm_id != Firm.FIRM_RESEARCH)
						set_attack_dir(curXLoc, curYLoc, targetFirm.center_x, targetFirm.center_y);
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

						set_attack_dir(curXLoc, curYLoc, hitXLoc, hitYLoc);
					}

					if (is_dir_correct())
						set_attack();
					else
						set_turn();
				}

			}
		}
	}

	protected void process_attack_town()
	{
		if (!can_attack())
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//------- if the targeted town has been destroyed --------//
		if (action_para == 0)
			return;

		Town targetTown = null;
		int clearOrder = 0;
		//------------------------------------------------------------//
		// check attack conditions
		//------------------------------------------------------------//
		if (TownArray.IsDeleted(action_para))
		{
			if (!ConfigAdv.unit_finish_attack_move || cur_action == SPRITE_ATTACK)
				clearOrder++;
			else
			{
				// keep attack action alive to finish movement before going idle
				invalidate_attack_target();
				return;
			}
		}
		else
		{
			targetTown = TownArray[action_para];

			if (!nation_can_attack(targetTown.NationId)) // cannot attack this nation
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

			stop2(UnitConstants.KEEP_DEFENSE_MODE);

			return;
		}

		//-----------------------------------------------------//
		// If the unit is currently attacking somebody.
		//-----------------------------------------------------//
		if (cur_action == SPRITE_ATTACK)
		{
			if (remain_attack_delay != 0)
				return;

			AttackInfo attackInfo = attack_info_array[cur_attack];
			if (attackInfo.attack_range > 1) // range attack
			{
				//---------- wait for bullet emit ---------//
				if (cur_frame != attackInfo.bullet_out_frame)
					return;

				//------- seek location to attack target by bullet --------//
				int curXLoc = next_x_loc();
				int curYLoc = next_y_loc();
				if (!BulletArray.bullet_path_possible(curXLoc, curYLoc, mobile_type, range_attack_x_loc,
					    range_attack_y_loc,
					    UnitConstants.UNIT_LAND, attackInfo.bullet_speed, attackInfo.bullet_sprite_id))
				{
					if (!BulletArray.add_bullet_possible(curXLoc, curYLoc, mobile_type, action_x_loc, action_y_loc,
						    UnitConstants.UNIT_LAND, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT,
						    out range_attack_x_loc, out range_attack_y_loc, attackInfo.bullet_speed,
						    attackInfo.bullet_sprite_id))
					{
						//----- no suitable location, move to target --------//
						set_move_to_surround(action_x_loc, action_y_loc, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT,
							UnitConstants.BUILDING_TYPE_TOWN_MOVE_TO);
						return;
					}
				}

				//--------- add bullet, bullet emits --------//
				BulletArray.AddBullet(this, action_x_loc, action_y_loc);
				add_close_attack_effect();

				// ------- reduce power --------//
				cur_power -= attackInfo.consume_power;
				if (cur_power < 0) // ***** BUGHERE
					cur_power = 0;
				set_remain_attack_delay();
				return;
			}
			else // close attack
			{
				if (cur_frame != cur_sprite_attack().frame_count)
					return; // attacking

				hit_town(this, action_x_loc, action_y_loc, actual_damage(), nation_recno);
				add_close_attack_effect();

				// ------- reduce power --------//
				cur_power -= attackInfo.consume_power;
				if (cur_power < 0) // ***** BUGHERE
					cur_power = 0;
				set_remain_attack_delay();
			}
		}
		//--------------------------------------------------------------------------------------------------//
		// If the unit is on its way to attack the town, if it has gotten close next to it, attack now
		//--------------------------------------------------------------------------------------------------//
		// it has moved to the specified location. check cur_x & go_x to make sure the sprite has completely move to the location, not just crossing it.
		else if (Math.Abs(cur_x - next_x) <= sprite_info.speed && Math.Abs(cur_y - next_y) <= sprite_info.speed)
		{
			if (mobile_type == UnitConstants.UNIT_LAND)
			{
				if (detect_surround_target())
					return;
			}

			if (attack_range == 1)
			{
				//------------------------------------------------------------//
				// for close attack, the unit unable to attack the firm if
				// it is not in the firm surrounding
				//------------------------------------------------------------//
				if (_pathNodeDistance > attack_range)
					return;
			}

			int targetXLoc = targetTown.LocX1;
			int targetYLoc = targetTown.LocY1;

			int attackDistance = cal_distance(targetXLoc, targetYLoc, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT);

			if (attackDistance <= attack_range) // able to attack target
			{
				if ((attackDistance == 1) && attack_range > 1) // often false condition is checked first
					choose_best_attack_mode(1); // may change to use close attack

				if (attack_range > 1) // use range attack
				{
					set_cur(next_x, next_y);

					AttackInfo attackInfo = attack_info_array[cur_attack];
					int curXLoc = next_x_loc();
					int curYLoc = next_y_loc();
					if (!BulletArray.add_bullet_possible(curXLoc, curYLoc, mobile_type, targetXLoc, targetYLoc,
						    UnitConstants.UNIT_LAND, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT,
						    out range_attack_x_loc, out range_attack_y_loc, attackInfo.bullet_speed,
						    attackInfo.bullet_sprite_id))
					{
						//------- no suitable location, move to target ---------//
						if (PathNodes.Count  == 0) // no step for continuing moving
							set_move_to_surround(action_x_loc, action_y_loc, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT,
								UnitConstants.BUILDING_TYPE_TOWN_MOVE_TO);

						return; // unable to attack, continue to move
					}

					//---------- able to do range attack ----------//
					set_attack_dir(next_x_loc(), next_y_loc(), range_attack_x_loc, range_attack_y_loc);
					cur_frame = 1;

					if (is_dir_correct())
						set_attack();
					else
						set_turn();
				}
				else // close attack
				{
					//---------- attack now ---------//
					set_cur(next_x, next_y);
					terminate_move();
					set_dir(next_x_loc(), next_y_loc(), targetTown.LocCenterX, targetTown.LocCenterY);

					if (is_dir_correct())
						set_attack();
					else
						set_turn();
				}
			}
		}
	}

	protected void process_attack_wall()
	{
		if (!can_attack())
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//------------------------------------------------------------//
		// if the targeted wall has been destroyed
		//------------------------------------------------------------//
		Location loc = World.get_loc(action_x_loc, action_y_loc);
		if (!loc.is_wall())
		{
			if (!ConfigAdv.unit_finish_attack_move || cur_action == SPRITE_ATTACK)
			{
				stop2(UnitConstants.KEEP_DEFENSE_MODE);
			}

			return;
		}

		//-----------------------------------------------------//
		// If the unit is currently attacking.
		//-----------------------------------------------------//
		if (cur_action == SPRITE_ATTACK)
		{
			if (remain_attack_delay != 0)
				return;

			AttackInfo attackInfo = attack_info_array[cur_attack];
			if (attackInfo.attack_range > 1) // range attack
			{
				//--------- wait for bullet emit ----------//
				if (cur_frame != attackInfo.bullet_out_frame)
					return;

				//---------- seek location to attack target by bullet --------//
				int curXLoc = next_x_loc();
				int curYLoc = next_y_loc();
				if (!BulletArray.bullet_path_possible(curXLoc, curYLoc, mobile_type, range_attack_x_loc,
					    range_attack_y_loc,
					    UnitConstants.UNIT_LAND, attackInfo.bullet_speed, attackInfo.bullet_sprite_id))
				{
					if (!BulletArray.add_bullet_possible(curXLoc, curYLoc, mobile_type, 
						    action_x_loc, action_y_loc,
						    UnitConstants.UNIT_LAND, 1, 1,
						    out range_attack_x_loc, out range_attack_y_loc,
						    attackInfo.bullet_speed, attackInfo.bullet_sprite_id))
					{
						//--------- no suitable location, move to target ----------//
						set_move_to_surround(action_x_loc, action_y_loc, 1, 1, UnitConstants.BUILDING_TYPE_WALL);
						return;
					}
				}

				//---------- add bullet, bullet emits -----------//
				BulletArray.AddBullet(this, action_x_loc, action_y_loc);
				add_close_attack_effect();

				// ------- reduce power --------//
				cur_power -= attackInfo.consume_power;
				if (cur_power < 0) // ***** BUGHERE
					cur_power = 0;
				set_remain_attack_delay();
				return;
			}
			else
			{
				if (cur_frame != cur_sprite_attack().frame_count)
					return; // attacking

				hit_wall(this, action_x_loc, action_y_loc, actual_damage(), nation_recno);
				add_close_attack_effect();

				//------- reduce power --------//
				cur_power -= attackInfo.consume_power;
				if (cur_power < 0) // ***** BUGHERE
					cur_power = 0;
				set_remain_attack_delay();
			}
		}
		//--------------------------------------------------------------------------------------------------//
		// If the unit is on its way to attack somebody, if it has gotten close next to the target, attack now
		//--------------------------------------------------------------------------------------------------//
		// it has moved to the specified location. check cur_x & go_x to make sure the sprite has completely move to the location, not just crossing it.
		else if (Math.Abs(cur_x - next_x) <= sprite_info.speed && Math.Abs(cur_y - next_y) <= sprite_info.speed)
		{
			if (mobile_type == UnitConstants.UNIT_LAND)
			{
				if (detect_surround_target())
					return;
			}

			if (attack_range == 1)
			{
				//------------------------------------------------------------//
				// for close attack, the unit unable to attack the firm if
				// it is not in the firm surrounding
				//------------------------------------------------------------//
				if (_pathNodeDistance > attack_range)
					return;
			}

			int attackDistance = cal_distance(action_x_loc, action_y_loc, 1, 1);

			if (attackDistance <= attack_range) // able to attack target
			{
				if ((attackDistance == 1) && attack_range > 1) // often false condition is checked first
					choose_best_attack_mode(1); // may change to use close attack

				if (attack_range > 1) // use range attack
				{
					set_cur(next_x, next_y);

					AttackInfo attackInfo = attack_info_array[cur_attack];
					int curXLoc = next_x_loc();
					int curYLoc = next_y_loc();
					if (!BulletArray.add_bullet_possible(curXLoc, curYLoc, mobile_type,
						    action_x_loc, action_y_loc,
						    UnitConstants.UNIT_LAND, 1, 1,
						    out range_attack_x_loc, out range_attack_y_loc,
						    attackInfo.bullet_speed, attackInfo.bullet_sprite_id))
					{
						//------- no suitable location, move to target ---------//
						if (PathNodes.Count == 0) // no step for continuing moving
							set_move_to_surround(action_x_loc, action_y_loc, 1, 1, UnitConstants.BUILDING_TYPE_WALL);

						return; // unable to attack, continue to move
					}

					//---------- able to do range attack ----------//
					set_attack_dir(curXLoc, curYLoc, range_attack_x_loc, range_attack_y_loc);
					cur_frame = 1;

					if (is_dir_correct())
						set_attack();
					else
						set_turn();
				}
				else // close attack
				{
					//---------- attack now ---------//
					set_cur(next_x, next_y);
					terminate_move();
					set_attack_dir(next_x_loc(), next_y_loc(), action_x_loc, action_y_loc);

					if (is_dir_correct())
						set_attack();
					else
						set_turn();
				}

			}
		}
	}

	public void attack_unit(int targetXLoc, int targetYLoc, int xOffset, int yOffset, bool resetBlockedEdge)
	{
		Location loc = World.get_loc(targetXLoc, targetYLoc);

		//--- AI attacking a nation which its NationRelation::should_attack is 0 ---//

		int targetNationRecno = 0;

		if (loc.has_unit(UnitConstants.UNIT_LAND))
		{
			Unit unit = UnitArray[loc.unit_recno(UnitConstants.UNIT_LAND)];

			if (unit.unit_id != UnitConstants.UNIT_EXPLOSIVE_CART) // attacking own porcupine is allowed
				targetNationRecno = unit.nation_recno;
		}
		else if (loc.is_firm())
		{
			targetNationRecno = FirmArray[loc.firm_recno()].nation_recno;
		}
		else if (loc.is_town())
		{
			targetNationRecno = TownArray[loc.town_recno()].NationId;
		}

		if (nation_recno != 0 && targetNationRecno != 0)
		{
			if (!NationArray[nation_recno].get_relation(targetNationRecno).should_attack)
				return;
		}

		//------------------------------------------------------------//
		// return if this unit cannot do the attack action, or die
		//------------------------------------------------------------//
		if (!can_attack())
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}
		else if (is_unit_dead())
		{
			return;
		}

		loc = World.get_loc(targetXLoc, targetYLoc);

		int targetMobileType = (next_x_loc() == targetXLoc && next_y_loc() == targetYLoc)
			? loc.has_any_unit(mobile_type) : loc.has_any_unit();

		if (targetMobileType != 0)
		{
			Unit targetUnit = UnitArray[loc.unit_recno(targetMobileType)];
			attack_unit(targetUnit.sprite_recno, xOffset, yOffset, resetBlockedEdge);
		}

		//------ set ai_original_target_?_loc --------//

		if (ai_unit)
		{
			ai_original_target_x_loc = targetXLoc;
			ai_original_target_y_loc = targetYLoc;
		}
	}
	
	public void attack_unit(int targetRecno, int xOffset, int yOffset, bool resetBlockedEdge)
	{
		//------------------------------------------------------------//
		// return if this unit cannot do the attack action, or die
		//------------------------------------------------------------//
		if (!can_attack())
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}
		else if (is_unit_dead())
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
		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();
		Unit targetUnit = UnitArray[targetRecno];
		int targetMobileType = targetUnit.mobile_type;
		int targetXLoc = targetUnit.next_x_loc();
		int targetYLoc = targetUnit.next_y_loc();
		int maxRange = 0;
		bool diffTerritoryAttack = false;
		Location loc = World.get_loc(targetUnit.next_x_loc(), targetUnit.next_y_loc());

		if (targetMobileType != 0 && mobile_type != UnitConstants.UNIT_AIR) // air unit can move to anywhere
		{
			//------------------------------------------------------------------------//
			// return if not feasible condition
			//------------------------------------------------------------------------//
			if ((mobile_type != UnitConstants.UNIT_LAND || targetMobileType != UnitConstants.UNIT_SEA) &&
			    mobile_type != targetMobileType)
			{
				if (!can_attack_different_target_type())
				{
					//-************ improve later **************-//
					//-******** should escape from being attacked ********-//
					if (in_any_defense_mode())
						general_defend_mode_detect_target();
					return;
				}
			}

			//------------------------------------------------------------------------//
			// handle the case the unit and the target are in different territory
			//------------------------------------------------------------------------//
			if (World.get_loc(curXLoc, curYLoc).region_id != loc.region_id)
			{
				maxRange = max_attack_range();
				Unit unit = UnitArray[loc.unit_recno(targetMobileType)];
				if (!possible_place_for_range_attack(targetXLoc, targetYLoc,
					    unit.sprite_info.loc_width, unit.sprite_info.loc_height, maxRange))
				{
					if (action_mode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
					    action_mode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
					    action_mode2 != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
						move_to(targetXLoc, targetYLoc);
					else // in defend mode, but unable to attack target
						general_defend_mode_detect_target(1);

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
			if (action_mode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
			    action_mode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
			    action_mode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
			{
				stop2(UnitConstants.KEEP_DEFENSE_MODE);
			}

			return;
		}

		//------------------------------------------------------------//
		// cannot attack this nation
		//------------------------------------------------------------//
		if (!nation_can_attack(targetUnit.nation_recno) && targetUnit.unit_id != UnitConstants.UNIT_EXPLOSIVE_CART)
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//----------------------------------------------------------------//
		// action_mode2: checking for equal action or idle action
		//----------------------------------------------------------------//
		if ((action_mode2 == UnitConstants.ACTION_ATTACK_UNIT ||
		     action_mode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
		     action_mode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
		     action_mode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET) &&
		    action_para2 == targetUnit.sprite_recno && action_x_loc2 == targetXLoc && action_y_loc2 == targetYLoc)
		{
			//------------ old order ------------//

			if (cur_action != SPRITE_IDLE)
			{
				//------- the old order is processing, return -------//
				return;
			} //else the action becomes idle
		}
		else
		{
			//-------------- store new order ----------------//
			if (action_mode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
			    action_mode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
			    action_mode2 != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
			{
				action_mode2 = UnitConstants.ACTION_ATTACK_UNIT;
			}

			action_para2 = targetUnit.sprite_recno;
			action_x_loc2 = targetXLoc;
			action_y_loc2 = targetYLoc;
		}

		//-------------------------------------------------------------//
		// process new order
		//-------------------------------------------------------------//
		stop();
		cur_attack = 0;

		int attackDistance = cal_distance(targetXLoc, targetYLoc, targetUnit.sprite_info.loc_width,
			targetUnit.sprite_info.loc_height);
		choose_best_attack_mode(attackDistance, targetMobileType);

		AttackInfo attackInfo = attack_info_array[cur_attack];
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

				search(xLoc, yLoc, 1); // offset location is given, so move there directly
			}
			else
			{
				//if(mobile_type!=targetMobileType)
				if (diffTerritoryAttack)
				{
					//--------------------------------------------------------------------------------//
					// 1) different type from target, target located in different territory from this
					//		unit. But able to attack this target by range attacking
					//--------------------------------------------------------------------------------//
					move_to_range_attack(targetXLoc, targetYLoc, targetUnit.sprite_id, SeekPath.SEARCH_MODE_ATTACK_UNIT_BY_RANGE,
						maxRange);
				}
				else
				{
					//--------------------------------------------------------------------------------//
					// 1) same type of target,
					// 2) this unit is air unit, or
					// 3) different type from target, but target located in the same territory of this
					//		unit.
					//--------------------------------------------------------------------------------//
					searchResult = search(targetXLoc, targetYLoc, 1, SeekPath.SEARCH_MODE_TO_ATTACK, targetUnit.sprite_recno);
				}
			}

			//---------------------------------------------------------------//
			// initialize parameters for blocked edge handling in attacking
			//---------------------------------------------------------------//
			if (searchResult != 0)
			{
				waiting_term = 0;
				if (resetBlockedEdge)
				{
					for (int i = 0; i < blocked_edge.Length; i++)
					{
						blocked_edge[i] = 0;
					}
				}
			}
			else
			{
				for (int i = 0; i < blocked_edge.Length; i++)
				{
					blocked_edge[i] = 0xff;
				}
			}
		}
		else if (cur_action == SPRITE_IDLE) // the target is within attack range, attacks it now if the unit is idle
		{
			//---------------------------------------------------------------//
			// attack now
			//---------------------------------------------------------------//
			set_cur(next_x, next_y);
			set_attack_dir(curXLoc, curYLoc, targetXLoc, targetYLoc);
			if (is_dir_correct())
			{
				if (attackInfo.attack_range == 1)
				{
					set_attack();
					turn_delay = 0;
				}
			}
			else
			{
				set_turn();
			}
		}

		action_mode = UnitConstants.ACTION_ATTACK_UNIT;
		action_para = targetUnit.sprite_recno;
		action_x_loc = targetXLoc;
		action_y_loc = targetYLoc;

		//------ set ai_original_target_?_loc --------//

		if (ai_unit)
		{
			ai_original_target_x_loc = targetXLoc;
			ai_original_target_y_loc = targetYLoc;
		}
	}
	
	public void attack_firm(int firmXLoc, int firmYLoc, int xOffset = 0, int yOffset = 0, int resetBlockedEdge = 1)
	{
		//------------------------------------------------------------//
		// return if this unit cannot do the attack action
		//------------------------------------------------------------//
		if (!can_attack())
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}
		else if (is_unit_dead())
		{
			return;
		}

		Location loc = World.get_loc(firmXLoc, firmYLoc);

		//------------------------------------------------------------//
		// no firm there
		//------------------------------------------------------------//
		if (!loc.is_firm())
		{
			if (action_mode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
			    action_mode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
			    action_mode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
			{
				stop2(UnitConstants.KEEP_DEFENSE_MODE);
			}

			return;
		}

		//------------------------------------------------------------//
		// cannot attack this nation
		//------------------------------------------------------------//
		Firm firm = FirmArray[loc.firm_recno()];
		if (!nation_can_attack(firm.nation_recno))
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//------------------------------------------------------------------------------------//
		// move there if cannot reach the effective attacking region
		//------------------------------------------------------------------------------------//
		FirmInfo firmInfo = FirmRes[firm.firm_id];
		int maxRange = 0;
		bool diffTerritoryAttack = false;
		if (mobile_type != UnitConstants.UNIT_AIR &&
		    World.get_loc(next_x_loc(), next_y_loc()).region_id != loc.region_id)
		{
			maxRange = max_attack_range();
			//Firm		*firm = FirmArray[loc.firm_recno()];
			if (!possible_place_for_range_attack(firmXLoc, firmYLoc, firmInfo.loc_width, firmInfo.loc_height, maxRange))
			{
				if (action_mode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
				    action_mode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
				    action_mode2 != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
				{
					move_to(firmXLoc, firmYLoc);
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
		if ((action_mode2 == UnitConstants.ACTION_ATTACK_FIRM ||
		     action_mode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
		     action_mode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
		     action_mode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET) &&
		    action_para2 == firm.firm_recno && action_x_loc2 == firmXLoc && action_y_loc2 == firmYLoc)
		{
			//-------------- old order -------------//
			if (cur_action != SPRITE_IDLE)
			{
				return;
			}
		}
		else
		{
			//-------------- new order -------------//
			if (action_mode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
			    action_mode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
			    action_mode2 != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
				action_mode2 = UnitConstants.ACTION_ATTACK_FIRM;

			action_para2 = firm.firm_recno;
			action_x_loc2 = firmXLoc;
			action_y_loc2 = firmYLoc;
		}

		//-------------------------------------------------------------//
		// process new order
		//-------------------------------------------------------------//
		stop();
		cur_attack = 0;

		int attackDistance = cal_distance(firmXLoc, firmYLoc, firmInfo.loc_width, firmInfo.loc_height);
		choose_best_attack_mode(attackDistance);

		AttackInfo attackInfo = attack_info_array[cur_attack];
		if (attackInfo.attack_range < attackDistance) // need to move to target
		{
			int searchResult = 1;

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

				search(xLoc, yLoc, 1); // offset location is given, so move there directly
			}
			else // without offset given, so call set_move_to_surround()
			{
				if (diffTerritoryAttack)
				{
					//--------------------------------------------------------------------------------//
					// 1) different type from target, target located in different territory from this
					//		unit. But able to attack this target by range attacking
					//--------------------------------------------------------------------------------//
					move_to_range_attack(firmXLoc, firmYLoc, firm.firm_id, SeekPath.SEARCH_MODE_ATTACK_FIRM_BY_RANGE, maxRange);
				}
				else
				{
					//--------------------------------------------------------------------------------//
					// 1) same type of target,
					// 2) this unit is air unit, or
					// 3) different type from target, but target located in the same territory of this
					//		unit.
					//--------------------------------------------------------------------------------//
					searchResult = set_move_to_surround(firmXLoc, firmYLoc, firmInfo.loc_width, firmInfo.loc_height,
						UnitConstants.BUILDING_TYPE_FIRM_MOVE_TO, 0, 0);
				}
			}

			//---------------------------------------------------------------//
			// initialize parameters for blocked edge handling in attacking
			//---------------------------------------------------------------//
			if (searchResult != 0)
			{
				waiting_term = 0;
				if (resetBlockedEdge != 0)
				{
					for (int i = 0; i < blocked_edge.Length; i++)
						blocked_edge[i] = 0;
				}
			}
			else
			{
				for (int i = 0; i < blocked_edge.Length; i++)
					blocked_edge[i] = 0xff;
			}
		}
		else if (cur_action == SPRITE_IDLE)
		{
			//---------------------------------------------------------------//
			// attack now
			//---------------------------------------------------------------//
			set_cur(next_x, next_y);

			if (firm.firm_id != Firm.FIRM_RESEARCH)
			{
				set_attack_dir(next_x_loc(), next_y_loc(), firm.center_x, firm.center_y);
			}
			else // FIRM_RESEARCH with size 2x3
			{
				int curXLoc = next_x_loc();
				int curYLoc = next_y_loc();

				int hitXLoc = (curXLoc > firm.loc_x1) ? firm.loc_x2 : firm.loc_x1;

				int hitYLoc;
				if (curYLoc < firm.center_y)
					hitYLoc = firm.loc_y1;
				else if (curYLoc == firm.center_y)
					hitYLoc = firm.center_y;
				else
					hitYLoc = firm.loc_y2;

				set_attack_dir(curXLoc, curYLoc, hitXLoc, hitYLoc);
			}

			if (is_dir_correct())
			{
				if (attackInfo.attack_range == 1)
					set_attack();
				//else range_attack is processed in calling process_attack_firm()
			}
			else
			{
				set_turn();
			}
		}

		action_mode = UnitConstants.ACTION_ATTACK_FIRM;
		action_para = firm.firm_recno;
		action_x_loc = firmXLoc;
		action_y_loc = firmYLoc;
	}
	
	public void attack_town(int townXLoc, int townYLoc, int xOffset = 0, int yOffset = 0, int resetBlockedEdge = 1)
	{
		//------------------------------------------------------------//
		// return if this unit cannot do the attack action
		//------------------------------------------------------------//
		if (!can_attack())
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}
		else if (is_unit_dead())
		{
			return;
		}

		Location loc = World.get_loc(townXLoc, townYLoc);

		//------------------------------------------------------------//
		// no town there
		//------------------------------------------------------------//
		if (!loc.is_town())
		{
			if (action_mode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
			    action_mode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
			    action_mode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
			{
				stop(UnitConstants.KEEP_DEFENSE_MODE);
			}

			return;
		}

		//------------------------------------------------------------//
		// cannot attack this nation
		//------------------------------------------------------------//
		Town town = TownArray[loc.town_recno()];
		if (!nation_can_attack(town.NationId))
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//------------------------------------------------------------------------------------//
		// move there if cannot reach the effective attacking region
		//------------------------------------------------------------------------------------//
		int maxRange = 0;
		bool diffTerritoryAttack = false;
		if (mobile_type != UnitConstants.UNIT_AIR &&
		    World.get_loc(next_x_loc(), next_y_loc()).region_id != loc.region_id)
		{
			maxRange = max_attack_range();
			if (!possible_place_for_range_attack(townXLoc, townYLoc, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT, maxRange))
			{
				if (action_mode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
				    action_mode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
				    action_mode != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
				{
					move_to(townXLoc, townYLoc);
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
		if ((action_mode2 == UnitConstants.ACTION_ATTACK_TOWN ||
		     action_mode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
		     action_mode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
		     action_mode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET) &&
		    action_para2 == town.TownId && action_x_loc2 == townXLoc && action_y_loc2 == townYLoc)
		{
			//----------- old order ------------//

			if (cur_action != SPRITE_IDLE)
			{
				//-------- old order is processing -------//
				return;
			}
		}
		else
		{
			//------------ new order -------------//
			if (action_mode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
			    action_mode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
			    action_mode2 != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
				action_mode2 = UnitConstants.ACTION_ATTACK_TOWN;

			action_para2 = town.TownId;
			action_x_loc2 = townXLoc;
			action_y_loc2 = townYLoc;
		}

		//-------------------------------------------------------------//
		// process new order
		//-------------------------------------------------------------//
		stop();
		cur_attack = 0;

		int attackDistance = cal_distance(townXLoc, townYLoc, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT);
		choose_best_attack_mode(attackDistance);

		AttackInfo attackInfo = attack_info_array[cur_attack];
		if (attackInfo.attack_range < attackDistance)
		{
			int searchResult = 1;

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

				search(xLoc, yLoc, 1); // offset location is given, so move there directly
			}
			else // without offset given, so call set_move_to_surround()
			{
				if (diffTerritoryAttack)
				{
					//--------------------------------------------------------------------------------//
					// 1) different type from target, target located in different territory but able to
					//		attack this target by range attacking
					//--------------------------------------------------------------------------------//
					move_to_range_attack(townXLoc, townYLoc, 0, SeekPath.SEARCH_MODE_ATTACK_TOWN_BY_RANGE, maxRange);
				}
				else
				{
					//--------------------------------------------------------------------------------//
					// 1) same type of target,
					// 2) this unit is air unit, or
					// 3) different type from target, but target located in the same territory
					//--------------------------------------------------------------------------------//
					searchResult = set_move_to_surround(townXLoc, townYLoc, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT,
						UnitConstants.BUILDING_TYPE_TOWN_MOVE_TO, 0, 0);
				}
			}

			//---------------------------------------------------------------//
			// initialize parameters for blocked edge handling in attacking
			//---------------------------------------------------------------//
			if (searchResult != 0)
			{
				waiting_term = 0;
				if (resetBlockedEdge != 0)
				{
					for (int i = 0; i < blocked_edge.Length; i++)
						blocked_edge[i] = 0;
				}
			}
			else
			{
				for (int i = 0; i < blocked_edge.Length; i++)
					blocked_edge[i] = 0xff;
			}
		}
		else if (cur_action == SPRITE_IDLE)
		{
			//---------------------------------------------------------------//
			// attack now
			//---------------------------------------------------------------//
			set_cur(next_x, next_y);
			set_attack_dir(next_x_loc(), next_y_loc(), town.LocCenterX, town.LocCenterY);
			if (is_dir_correct())
			{
				if (attackInfo.attack_range == 1)
					set_attack();
			}
			else
			{
				set_turn();
			}
		}

		action_mode = UnitConstants.ACTION_ATTACK_TOWN;
		action_para = town.TownId;
		action_x_loc = townXLoc;
		action_y_loc = townYLoc;
	}
	
	public void attack_wall(int wallXLoc, int wallYLoc, int xOffset = 0, int yOffset = 0, int resetBlockedEdge = 1)
	{
		//------------------------------------------------------------//
		// return if this unit cannot do the attack action
		//------------------------------------------------------------//
		if (!can_attack())
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}
		else if (is_unit_dead())
		{
			return;
		}

		Location loc = World.get_loc(wallXLoc, wallYLoc);

		//------------------------------------------------------------//
		// no wall there
		//------------------------------------------------------------//
		if (!loc.is_wall())
		{
			if (action_mode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
			    action_mode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
			    action_mode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
			{
				stop(UnitConstants.KEEP_DEFENSE_MODE);
			}

			return;
		}

		//------------------------------------------------------------//
		// cannot attack this nation
		//------------------------------------------------------------//
		if (!nation_can_attack(loc.wall_nation_recno()))
		{
			stop2(UnitConstants.KEEP_DEFENSE_MODE);
			return;
		}

		//------------------------------------------------------------------------------------//
		// move there if cannot reach the effective attacking region
		//------------------------------------------------------------------------------------//
		int maxRange = 0;
		bool diffTerritoryAttack = false;
		if (mobile_type != UnitConstants.UNIT_AIR &&
		    World.get_loc(next_x_loc(), next_y_loc()).region_id != loc.region_id)
		{
			maxRange = max_attack_range();
			if (!possible_place_for_range_attack(wallXLoc, wallYLoc, 1, 1, maxRange))
			{
				if (action_mode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
				    action_mode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
				    action_mode != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
				{
					move_to(wallXLoc, wallYLoc);
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
		if ((action_mode2 == UnitConstants.ACTION_ATTACK_WALL ||
		     action_mode2 == UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET ||
		     action_mode2 == UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET ||
		     action_mode2 == UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET) &&
		    action_para2 == 0 && action_x_loc2 == wallXLoc && action_y_loc2 == wallYLoc)
		{
			//------------ old order ------------//

			if (cur_action != SPRITE_IDLE)
			{
				//------- old order is processing --------//
				return;
			}
		}
		else
		{
			//-------------- new order -------------//
			if (action_mode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
			    action_mode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
			    action_mode2 != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
				action_mode2 = UnitConstants.ACTION_ATTACK_WALL;

			action_para2 = 0;
			action_x_loc2 = wallXLoc;
			action_y_loc2 = wallYLoc;
		}

		//-------------------------------------------------------------//
		// process new order
		//-------------------------------------------------------------//
		stop();
		cur_attack = 0;

		int attackDistance = cal_distance(wallXLoc, wallYLoc, 1, 1);
		choose_best_attack_mode(attackDistance);

		AttackInfo attackInfo = attack_info_array[cur_attack];
		if (attackInfo.attack_range < attackDistance)
		{
			int searchResult = 1;

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

				search(xLoc, yLoc, 1); // offset location is given, so move there directly
			}
			else
			{
				if (diffTerritoryAttack)
				{
					//--------------------------------------------------------------------------------//
					// 1) different type from target, target located in different territory but able to
					//		attack this target by range attacking
					//--------------------------------------------------------------------------------//
					move_to_range_attack(wallXLoc, wallYLoc, 0, SeekPath.SEARCH_MODE_ATTACK_WALL_BY_RANGE, maxRange);
				}
				else
				{
					//--------------------------------------------------------------------------------//
					// 1) same type of target,
					// 2) this unit is air unit, or
					// 3) different type from target, but target located in the same territory
					//--------------------------------------------------------------------------------//
					searchResult = set_move_to_surround(wallXLoc, wallYLoc, 1, 1,
						UnitConstants.BUILDING_TYPE_WALL, 0, 0);
				}
			}

			//---------------------------------------------------------------//
			// initialize parameters for blocked edge handling in attacking
			//---------------------------------------------------------------//
			if (searchResult != 0)
			{
				waiting_term = 0;
				if (resetBlockedEdge != 0)
				{
					for (int i = 0; i < blocked_edge.Length; i++)
						blocked_edge[i] = 0;
				}
			}
			else
			{
				for (int i = 0; i < blocked_edge.Length; i++)
					blocked_edge[i] = 0xff;
			}
		}
		else if (cur_action == SPRITE_IDLE)
		{
			//---------------------------------------------------------------//
			// attack now
			//---------------------------------------------------------------//
			set_cur(next_x, next_y);
			set_attack_dir(next_x_loc(), next_y_loc(), wallXLoc, wallYLoc);
			if (is_dir_correct())
			{
				if (attackInfo.attack_range == 1)
					set_attack();
			}
			else
			{
				set_turn();
			}
		}

		action_mode = UnitConstants.ACTION_ATTACK_WALL;
		action_para = 0;
		action_x_loc = wallXLoc;
		action_y_loc = wallYLoc;
	}
	
	public void hit_target(Unit parentUnit, Unit targetUnit, double attackDamage, int parentNationRecno)
	{
		int targetNationRecno = targetUnit.nation_recno;

		//------------------------------------------------------------//
		// if the attacked unit is in defense mode, order other available
		// unit in the same camp to help this unit
		// Note : checking for nation_recno since one unit can attack units
		//			 in same nation by bullet accidentally
		//------------------------------------------------------------//
		if (parentUnit != null && parentUnit.cur_action != SPRITE_DIE && parentUnit.is_visible() &&
		    parentNationRecno != targetNationRecno && parentUnit.nation_can_attack(targetNationRecno) &&
		    targetUnit.in_auto_defense_mode())
		{
			if (!FirmArray.IsDeleted(targetUnit.action_misc_para))
			{
				Firm firm = FirmArray[targetUnit.action_misc_para];
				if (firm.firm_id == Firm.FIRM_CAMP)
				{
					FirmCamp camp = (FirmCamp)firm;
					camp.defense(parentUnit.sprite_recno);
				}
			}
			else
			{
				targetUnit.clear_unit_defense_mode();
			}
		}

		// ---------- add indicator on the map ----------//
		if (NationArray.player_recno != 0 && targetUnit.is_own())
			WarPointArray.AddPoint(targetUnit.next_x_loc(), targetUnit.next_y_loc());

		//-----------------------------------------------------------------------//
		// decrease the hit points of the target Unit
		//-----------------------------------------------------------------------//
		const int DEFAULT_ARMOR = 4;
		const double DEFAULT_ARMOR_OVER_ATTACK_SLOW_DOWN = (double)DEFAULT_ARMOR / (double)InternalConstants.ATTACK_SLOW_DOWN;
		const double ONE_OVER_ATTACK_SLOW_DOWN = 1.0 / (double)InternalConstants.ATTACK_SLOW_DOWN;
		const double COMPARE_POINT = DEFAULT_ARMOR_OVER_ATTACK_SLOW_DOWN + ONE_OVER_ATTACK_SLOW_DOWN;

		if (attackDamage >= COMPARE_POINT)
			targetUnit.hit_points -= attackDamage - DEFAULT_ARMOR_OVER_ATTACK_SLOW_DOWN;
		else
			targetUnit.hit_points -= Math.Min(attackDamage, ONE_OVER_ATTACK_SLOW_DOWN);  // in case attackDamage = 0, no hit_point is reduced
		
		Nation parentNation = parentNationRecno != 0 ? NationArray[parentNationRecno] : null;
		Nation targetNation = targetNationRecno != 0 ? NationArray[targetNationRecno] : null;
		int targetUnitClass = UnitRes[targetUnit.unit_id].unit_class;

		if (targetUnit.hit_points <= 0.0)
		{
			targetUnit.hit_points = 0.0;

			//---- if the unit killed is a human unit -----//

			if (targetUnit.race_id != 0)
			{
				//---- if the unit killed is a town defender unit -----//

				if (targetUnit.is_civilian() && targetUnit.in_defend_town_mode())
				{
					if (targetNationRecno != 0)
					{
						targetNation.civilian_killed(targetUnit.race_id, false, 1);
						targetNation.own_civilian_killed++;
					}

					if (parentNation != null)
					{
						parentNation.civilian_killed(targetUnit.race_id, true, 1);
						parentNation.enemy_civilian_killed++;
					}
				}
				else if (targetUnit.is_civilian() && targetUnit.skill.combat_level < 20) //--- mobile civilian ---//
				{
					if (targetNationRecno != 0)
					{
						targetNation.civilian_killed(targetUnit.race_id, false, 0);
						targetNation.own_civilian_killed++;
					}

					if (parentNation != null)
					{
						parentNation.civilian_killed(targetUnit.race_id, true, 0);
						parentNation.enemy_civilian_killed++;
					}
				}
				else //---- if the unit killed is a soldier -----//
				{
					if (targetNationRecno != 0)
						targetNation.own_soldier_killed++;

					if (parentNation != null)
						parentNation.enemy_soldier_killed++;
				}
			}

			//--------- if it's a non-human unit ---------//

			else
			{
				switch (UnitRes[targetUnit.unit_id].unit_class)
				{
					case UnitConstants.UNIT_CLASS_WEAPON:
						if (parentNation != null)
							parentNation.enemy_weapon_destroyed++;

						if (targetNationRecno != 0)
							targetNation.own_weapon_destroyed++;
						break;


					case UnitConstants.UNIT_CLASS_SHIP:
						if (parentNation != null)
							parentNation.enemy_ship_destroyed++;

						if (targetNationRecno != 0)
							targetNation.own_ship_destroyed++;
						break;
				}

				//---- if the unit destroyed is a trader or caravan -----//

				// killing a caravan is resented by all races
				if (targetUnit.unit_id == UnitConstants.UNIT_CARAVAN || targetUnit.unit_id == UnitConstants.UNIT_VESSEL)
				{
					// Race-Id of 0 means a loyalty penalty applied for all races
					if (targetNationRecno != 0)
						targetNation.civilian_killed(0, false, 3);

					if (parentNation != null)
						parentNation.civilian_killed(0, true, 3);
				}
			}

			return;
		}

		if (parentUnit != null && parentNationRecno != targetNationRecno)
			parentUnit.gain_experience(); // gain experience to increase combat level

		//-----------------------------------------------------------------------//
		// action of the target to take
		//-----------------------------------------------------------------------//
		if (parentUnit == null) // do nothing if parent is dead
			return;

		if (parentUnit.cur_action == SPRITE_DIE) // skip for explosive cart
			return;

		// the target and the attacker's nations are different
		// (it's possible that when a unit who has just changed nation has its bullet hitting its own nation)
		if (targetNationRecno == parentNationRecno)
			return;

		//------- two nations at war ---------//

		if (parentNation != null && targetNationRecno != 0)
		{
			parentNation.set_at_war_today();
			targetNation.set_at_war_today(parentUnit.sprite_recno);
		}

		//-------- increase battling fryhtan score --------//

		if (parentNation != null && targetUnitClass == UnitConstants.UNIT_CLASS_MONSTER)
		{
			parentNation.kill_monster_score += 0.1;
		}

		//------ call target unit being attack functions -------//

		if (targetNationRecno != 0)
		{
			targetNation.being_attacked(parentNationRecno);

			if (targetUnit.ai_unit)
			{
				if (targetUnit.rank_id >= RANK_GENERAL)
					targetUnit.ai_leader_being_attacked(parentUnit.sprite_recno);

				if (UnitRes[targetUnit.unit_id].unit_class == UnitConstants.UNIT_CLASS_SHIP)
					((UnitMarine)targetUnit).ai_ship_being_attacked(parentUnit.sprite_recno);
			}

			//--- if a member in a troop is under attack, ask for other troop members to help ---//

			if (Info.TotalDays % 2 == sprite_recno % 2)
			{
				if (targetUnit.leader_unit_recno != 0 ||
				    (targetUnit.team_info != null && targetUnit.team_info.member_unit_array.Count > 1))
				{
					if (!UnitArray.IsDeleted(parentUnit
						    .sprite_recno)) // it is possible that parentUnit is dying right now 
					{
						targetUnit.ask_team_help_attack(parentUnit);
					}
				}
			}
		}

		//--- increase reputation of the nation that attacks monsters ---//

		else if (targetUnitClass == UnitConstants.UNIT_CLASS_MONSTER)
		{
			if (parentNation != null)
				parentNation.change_reputation(GameConstants.REPUTATION_INCREASE_PER_ATTACK_MONSTER);

			//--- if a member in a troop is under attack, ask for other troop members to help ---//

			if (Info.TotalDays % 2 == sprite_recno % 2)
			{
				if (targetUnit.leader_unit_recno != 0 ||
				    (targetUnit.team_info != null && targetUnit.team_info.member_unit_array.Count > 1))
				{
					if (!UnitArray.IsDeleted(parentUnit
						    .sprite_recno)) // it is possible that parentUnit is dying right now 
					{
						targetUnit.ask_team_help_attack(parentUnit);
					}
				}
			}
		}

		//------------------------------------------//

		if (!targetUnit.can_attack()) // no action if the target unit is unable to attack
			return;

		targetUnit.unit_auto_guarding(parentUnit);
	}
	
	public void hit_building(Unit attackUnit, int targetXLoc, int targetYLoc, double attackDamage, int attackNationRecno)
	{
		Location loc = World.get_loc(targetXLoc, targetYLoc);

		if (loc.is_firm())
			hit_firm(attackUnit, targetXLoc, targetYLoc, attackDamage, attackNationRecno);
		else if (loc.is_town())
			hit_town(attackUnit, targetXLoc, targetYLoc, attackDamage, attackNationRecno);
	}
	
	public void hit_firm(Unit attackUnit, int targetXLoc, int targetYLoc, double attackDamage, int attackNationRecno)
	{
		Location loc = World.get_loc(targetXLoc, targetYLoc);
		if (!loc.is_firm())
			return; // do nothing if no firm there

		//----------- attack firm ------------//
		Firm targetFirm = FirmArray[loc.firm_recno()];

		Nation attackNation = NationArray.IsDeleted(attackNationRecno) ? null : NationArray[attackNationRecno];

		//------------------------------------------------------------------------------//
		// change relation to hostile
		// check for NULL to skip unhandled case by bullets
		// check for SPRITE_DIE to skip the case by EXPLOSIVE_CART
		//------------------------------------------------------------------------------//
		// the target and the attacker's nations are different
		// (it's possible that when a unit who has just changed nation has its bullet hitting its own nation)
		if (attackUnit != null && attackUnit.cur_action != SPRITE_DIE && targetFirm.nation_recno != attackNationRecno)
		{
			if (attackNation != null && targetFirm.nation_recno != 0)
			{
				attackNation.set_at_war_today();
				NationArray[targetFirm.nation_recno].set_at_war_today(attackUnit.sprite_recno);
			}

			if (targetFirm.nation_recno != 0)
				NationArray[targetFirm.nation_recno].being_attacked(attackNationRecno);

			//------------ auto defense -----------------//
			if (attackUnit.is_visible())
				targetFirm.auto_defense(attackUnit.sprite_recno);

			if (attackNationRecno != targetFirm.nation_recno)
				attackUnit.gain_experience(); // gain experience to increase combat level

			targetFirm.being_attacked(attackUnit.sprite_recno);

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
				NewsArray.monster_firm_destroyed(((FirmMonster)targetFirm).monster_id, targetFirm.center_x,
					targetFirm.center_y);
			}

			FirmArray.DeleteFirm(targetFirm);
		}
	}
	
	public void hit_town(Unit attackUnit, int targetXLoc, int targetYLoc, double attackDamage, int attackNationRecno)
	{
		Location loc = World.get_loc(targetXLoc, targetYLoc);

		if (!loc.is_town())
			return; // do nothing if no town there

		//----------- attack town ----------//

		Town targetTown = TownArray[loc.town_recno()];
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
		if (attackUnit != null && attackUnit.cur_action != SPRITE_DIE && targetTown.NationId != attackNationRecno)
		{
			int townNationRecno = targetTown.NationId;

			//------- change to hostile relation -------//

			if (attackNationRecno != 0 && targetTown.NationId != 0)
			{
				NationArray[attackNationRecno].set_at_war_today();
				NationArray[targetTown.NationId].set_at_war_today(attackUnit.sprite_recno);
			}

			if (targetTown.NationId != 0)
			{
				NationArray[targetTown.NationId].being_attacked(attackNationRecno);
			}

			// don't add the town abandon news that might be called by Town::dec_pop() as the town is actually destroyed not abandoned
			NewsArray.disable();

			targetTown.BeingAttacked(attackUnit.sprite_recno, attackDamage);

			NewsArray.enable();

			//------ if the town is destroyed, add a news --------//

			if (TownArray.IsDeleted(targetTownRecno) && townNationRecno == NationArray.player_recno)
			{
				NewsArray.town_destroyed(targetTownNameId, targetTownXLoc, targetTownYLoc, attackUnit,
					attackNationRecno);
			}

			//---------- gain experience --------//

			if (attackNationRecno != targetTown.NationId)
				attackUnit.gain_experience(); // gain experience to increase combat level

			//------------ auto defense -----------------//

			if (!FirmArray.IsDeleted(targetTownRecno))
				targetTown.AutoDefense(attackUnit.sprite_recno);
		}
	}
	
	public void hit_wall(Unit attackUnit, int targetXLoc, int targetYLoc, double attackDamage, int attackNationRecno)
	{
		Location loc = World.get_loc(targetXLoc, targetYLoc);

		/*
		if(attackUnit!=NULL)
			attackUnit.change_relation(attackNationRecno, loc.wall_nation_recno(), NationBase.NATION_HOSTILE);
		*/

		//TODO rewrite
		//if (!loc.attack_wall((int)attackDamage))
		//World.correct_wall(targetXLoc, targetYLoc);
	}

	private void target_move(Unit targetUnit)
	{
		//------------------------------------------------------------------------------------//
		// chekcing whether ship can follow to attack target. It is always true if the unit is
		// not ship. 1 for allowing, 0 otherwise
		//------------------------------------------------------------------------------------//
		int allowMove = 1;
		if (sprite_info.sprite_sub_type == 'M')
		{
			UnitInfo unitInfo = UnitRes[unit_id];
			if (unitInfo.carry_goods_capacity != 0)
			{
				UnitMarine ship = (UnitMarine)this;
				if (ship.auto_mode != 0 && ship.stop_defined_num > 1)
					allowMove = 0;
			}
		}

		//---------------------------------------------------------//
		int targetXLoc = targetUnit.next_x_loc();
		int targetYLoc = targetUnit.next_y_loc();
		SpriteInfo targetSpriteInfo = targetUnit.sprite_info;

		int attackDistance = cal_distance(targetXLoc, targetYLoc, targetSpriteInfo.loc_width, targetSpriteInfo.loc_height);
		action_x_loc2 = action_x_loc = targetXLoc; // update target location
		action_y_loc2 = action_y_loc = targetYLoc;

		//---------------------------------------------------------------------//
		// target is out of attacking range, move closer to it
		//---------------------------------------------------------------------//
		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();
		if (attackDistance > attack_range)
		{
			//---------------- stop all actions if not allow to move -----------------//
			if (allowMove == 0)
			{
				stop2();
				return;
			}

			//---------------------------------------------------------------------//
			// follow the target using the result_path_dist
			//---------------------------------------------------------------------//
			if (!update_attack_path_dist())
			{
				if (cur_action == SPRITE_MOVE || cur_action == SPRITE_WAIT || cur_action == SPRITE_READY_TO_MOVE)
					return;
			}

			if (move_try_to_range_attack(targetUnit) != 0)
			{
				//-----------------------------------------------------------------------//
				// reset attack parameters
				//-----------------------------------------------------------------------//
				range_attack_x_loc = range_attack_y_loc = -1;

				// choose better attack mode to attack the target
				choose_best_attack_mode(attackDistance, targetUnit.mobile_type);
			}
		}
		else // attackDistance <= attack_range
		{
			//-----------------------------------------------------------------------------//
			// although the target has moved, the unit can still attack it. no need to move
			//-----------------------------------------------------------------------------//
			if (Math.Abs(cur_x - next_x) >= sprite_info.speed || Math.Abs(cur_y - next_y) >= sprite_info.speed)
				return; // return as moving

			if (attackDistance == 1 && attack_range > 1) // may change attack mode
				choose_best_attack_mode(attackDistance, targetUnit.mobile_type);

			if (attack_range > 1) // range attack
			{
				//------------------ do range attack ----------------------//
				AttackInfo attackInfo = attack_info_array[cur_attack];
				// range attack possible
				if (BulletArray.add_bullet_possible(curXLoc, curYLoc, mobile_type, targetXLoc, targetYLoc,
					    targetUnit.mobile_type, targetSpriteInfo.loc_width, targetSpriteInfo.loc_height,
					    out range_attack_x_loc, out range_attack_y_loc, attackInfo.bullet_speed, attackInfo.bullet_sprite_id))
				{
					set_cur(next_x, next_y);

					set_attack_dir(curXLoc, curYLoc, range_attack_x_loc, range_attack_y_loc);
					if (ConfigAdv.unit_target_move_range_cycle)
					{
						cycle_eqv_attack();
						attackInfo = attack_info_array[cur_attack]; // cur_attack may change
						cur_frame = 1;
					}

					if (is_dir_correct())
						set_attack();
					else
						set_turn();
				}
				else // unable to do range attack, move to target
				{
					if (allowMove == 0)
					{
						stop2();
						return;
					}

					if (move_try_to_range_attack(targetUnit) == 1)
					{
						//range_attack_x_loc = range_attack_y_loc = -1;
						choose_best_attack_mode(attackDistance, targetUnit.mobile_type);
					}
				}
			}
			else if (attackDistance == 1) // close attack
			{
				set_cur(next_x, next_y);
				set_attack_dir(curXLoc, curYLoc, targetXLoc, targetYLoc);
				cur_frame = 1;

				if (is_dir_correct())
					set_attack();
				else
					set_turn();
			}
		}
	}

	private void attack_target(Unit targetUnit)
	{
		if (remain_attack_delay != 0)
			return;

		int unitXLoc = next_x_loc();
		int unitYLoc = next_y_loc();

		if (attack_range > 1) // use range attack
		{
			//---------------- use range attack -----------------//
			AttackInfo attackInfo = attack_info_array[cur_attack];

			if (cur_frame != attackInfo.bullet_out_frame)
				return; // wait for bullet_out_frame

			if (!BulletArray.bullet_path_possible(unitXLoc, unitYLoc, mobile_type,
				    range_attack_x_loc, range_attack_y_loc, targetUnit.mobile_type,
				    attackInfo.bullet_speed, attackInfo.bullet_sprite_id))
			{
				SpriteInfo targetSpriteInfo = targetUnit.sprite_info;
				// seek for another possible point to attack if target size > 1x1
				if ((targetSpriteInfo.loc_width > 1 || targetSpriteInfo.loc_height > 1) &&
				    !BulletArray.add_bullet_possible(unitXLoc, unitYLoc, mobile_type, action_x_loc, action_y_loc,
					    targetUnit.mobile_type, targetSpriteInfo.loc_width, targetSpriteInfo.loc_height,
					    out range_attack_x_loc, out range_attack_y_loc, attackInfo.bullet_speed, attackInfo.bullet_sprite_id))
				{
					//------ no suitable location to attack target by bullet, move to target --------//
					if (PathNodes.Count == 0 || _pathNodeDistance == 0)
						if (move_try_to_range_attack(targetUnit) == 0)
							return; // can't reach a location to attack target
				}
			}

			BulletArray.AddBullet(this, targetUnit);
			add_close_attack_effect();

			// ------- reduce power --------//
			cur_power -= attackInfo.consume_power;
			if (cur_power < 0) // ***** BUGHERE
				cur_power = 0;
			set_remain_attack_delay();
			return; // bullet emits
		}
		else // close attack
		{
			//--------------------- close attack ------------------------//
			AttackInfo attackInfo = attack_info_array[cur_attack];

			if (cur_frame == cur_sprite_attack().frame_count)
			{
				if (targetUnit.unit_id == UnitConstants.UNIT_EXPLOSIVE_CART && targetUnit.is_nation(nation_recno))
					((UnitExpCart)targetUnit).trigger_explode();
				else
					hit_target(this, targetUnit, actual_damage(), nation_recno);

				add_close_attack_effect();

				//------- reduce power --------//
				cur_power -= attackInfo.consume_power;
				if (cur_power < 0) // ***** BUGHERE
					cur_power = 0;
				set_remain_attack_delay();
			}
		}
	}

	private bool on_way_to_attack(Unit targetUnit)
	{
		if (mobile_type == UnitConstants.UNIT_LAND)
		{
			if (attack_range == 1)
			{
				//------------------------------------------------------------//
				// for close attack, the unit unable to attack the target if
				// it is not in the target surrounding
				//------------------------------------------------------------//
				if (_pathNodeDistance > attack_range)
					return detect_surround_target();
			}
			else if (_pathNodeDistance != 0 && cur_action != SPRITE_TURN)
			{
				if (detect_surround_target())
					return true; // detect surrounding target while walking
			}
		}

		int targetXLoc = targetUnit.next_x_loc();
		int targetYLoc = targetUnit.next_y_loc();
		SpriteInfo targetSpriteInfo = targetUnit.sprite_info;

		int attackDistance = cal_distance(targetXLoc, targetYLoc, targetSpriteInfo.loc_width, targetSpriteInfo.loc_height);

		if (attackDistance <= attack_range) // able to attack target
		{
			if ((attackDistance == 1) && attack_range > 1) // often false condition is checked first
				choose_best_attack_mode(1, targetUnit.mobile_type); // may change to close attack

			if (attack_range > 1) // use range attack
			{
				set_cur(next_x, next_y);

				AttackInfo attackInfo = attack_info_array[cur_attack];
				int curXLoc = next_x_loc();
				int curYLoc = next_y_loc();
				if (!BulletArray.add_bullet_possible(curXLoc, curYLoc, mobile_type, targetXLoc, targetYLoc,
					    targetUnit.mobile_type, targetSpriteInfo.loc_width, targetSpriteInfo.loc_height,
					    out range_attack_x_loc, out range_attack_y_loc,
					    attackInfo.bullet_speed, attackInfo.bullet_sprite_id))
				{
					//------- no suitable location, move to target ---------//
					if (PathNodes.Count == 0 || _pathNodeDistance == 0)
						if (move_try_to_range_attack(targetUnit) == 0)
							return false; // can't reach a location to attack target

					return false;
				}

				//---------- able to do range attack ----------//
				set_attack_dir(next_x_loc(), next_y_loc(), range_attack_x_loc, range_attack_y_loc);
				cur_frame = 1;

				if (is_dir_correct())
					set_attack();
				else
					set_turn();
			}
			else // close attack
			{
				//---------- attack now ---------//
				set_cur(next_x, next_y);
				terminate_move();
				set_attack_dir(next_x_loc(), next_y_loc(), targetXLoc, targetYLoc);

				if (is_dir_correct())
					set_attack();
				else
					set_turn();
			}
		}

		return false;
	}

	private bool detect_surround_target()
	{
		const int DIMENSION = 3;
		const int CHECK_SIZE = DIMENSION * DIMENSION;

		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();
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

			Location loc = World.get_loc(checkXLoc, checkYLoc);

			if (loc.has_unit(UnitConstants.UNIT_LAND))
			{
				targetRecno = loc.unit_recno(UnitConstants.UNIT_LAND);
				if (UnitArray.IsDeleted(targetRecno))
					continue;

				target = UnitArray[targetRecno];
				if (target.nation_recno == nation_recno)
					continue;

				if (idle_detect_unit_checking(targetRecno))
				{
					attack_unit(targetRecno, 0, 0, true);
					return true;
				}
			}
		}

		return false;
	}

	private bool update_attack_path_dist()
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

	private void set_attack_dir(int curX, int curY, int targetX, int targetY)
	{
		int targetDir = get_dir(curX, curY, targetX, targetY);
		if (UnitRes[unit_id].unit_class == UnitConstants.UNIT_CLASS_SHIP)
		{
			int attackDir1 = (targetDir + 2) % InternalConstants.MAX_SPRITE_DIR_TYPE;
			int attackDir2 = (targetDir + 6) % InternalConstants.MAX_SPRITE_DIR_TYPE;

			if ((attackDir1 + 8 - final_dir) % InternalConstants.MAX_SPRITE_DIR_TYPE <=
			    (attackDir2 + 8 - final_dir) % InternalConstants.MAX_SPRITE_DIR_TYPE)
				final_dir = attackDir1;
			else
				final_dir = attackDir2;

			attack_dir = targetDir;
		}
		else
		{
			attack_dir = targetDir;
			set_dir(targetDir);
		}
	}

	private int move_try_to_range_attack(Unit targetUnit)
	{
		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();
		int targetXLoc = targetUnit.next_x_loc();
		int targetYLoc = targetUnit.next_y_loc();

		if (World.get_loc(curXLoc, curYLoc).region_id == World.get_loc(targetXLoc, targetYLoc).region_id)
		{
			//------------ for same region id, search now ---------------//
			if (search(targetXLoc, targetYLoc, 1, SeekPath.SEARCH_MODE_TO_ATTACK, action_para) != 0)
				return 1;
			else // search failure,
			{
				stop2(UnitConstants.KEEP_DEFENSE_MODE);
				return 0;
			}
		}
		else
		{
			//--------------- different territory ------------------//
			int targetWidth = targetUnit.sprite_info.loc_width;
			int targetHeight = targetUnit.sprite_info.loc_height;
			int maxRange = max_attack_range();

			if (possible_place_for_range_attack(targetXLoc, targetYLoc, targetWidth, targetHeight, maxRange))
			{
				//---------------------------------------------------------------------------------//
				// space is found, attack target now
				//---------------------------------------------------------------------------------//
				if (move_to_range_attack(targetXLoc, targetYLoc,
					    targetUnit.sprite_id, SeekPath.SEARCH_MODE_ATTACK_UNIT_BY_RANGE, maxRange) != 0)
					return 1;
				else
				{
					stop2(UnitConstants.KEEP_DEFENSE_MODE);
					return 0;
				}

				return 1;
			}
			else
			{
				//---------------------------------------------------------------------------------//
				// unable to find location to attack the target, stop or move to the target
				//---------------------------------------------------------------------------------//
				if (action_mode2 != UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
				    action_mode2 != UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
				    action_mode2 != UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET)
					move_to(targetXLoc, targetYLoc, 1); // abort attacking, just call move_to() instead
				else
					stop2(UnitConstants.KEEP_DEFENSE_MODE);
				return 0;
			}
		}

		return 0;
	}

	private bool can_attack_different_target_type()
	{
		int maxRange = max_attack_range();
		if (mobile_type == UnitConstants.UNIT_LAND && maxRange == 0)
			return false; // unable to do range attack or cannot attack

		if (maxRange > 1)
			return true;
		else
			return false;
	}

	private bool possible_place_for_range_attack(int targetXLoc, int targetYLoc, int targetWidth, int targetHeight, int maxRange)
	{
		if (mobile_type == UnitConstants.UNIT_AIR)
			return true; // air unit can reach any region

		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();

		if (Math.Abs(curXLoc - targetXLoc) <= maxRange &&
		    Math.Abs(curYLoc - targetYLoc) <= maxRange) // inside the attack range
			return true;

		//----------------- init parameters -----------------//
		Location loc = World.get_loc(curXLoc, curYLoc);
		int regionId = loc.region_id;
		int xLoc1 = Math.Max(targetXLoc - maxRange, 0);
		int yLoc1 = Math.Max(targetYLoc - maxRange, 0);
		int xLoc2 = Math.Min(targetXLoc + targetWidth - 1 + maxRange, GameConstants.MapSize - 1);
		int yLoc2 = Math.Min(targetYLoc + targetHeight - 1 + maxRange, GameConstants.MapSize - 1);
		int checkXLoc, checkYLoc;

		//--------- do adjustment for UnitConstants.UNIT_SEA and UnitConstants.UNIT_AIR ---------//
		if (mobile_type != UnitConstants.UNIT_LAND)
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
		switch (mobile_type)
		{
			case UnitConstants.UNIT_LAND:
				for (checkXLoc = xLoc1; checkXLoc <= xLoc2; checkXLoc++)
				{
					loc = World.get_loc(checkXLoc, yLoc1);
					if (loc.region_id == regionId && loc.is_accessible(mobile_type))
						return true;

					loc = World.get_loc(checkXLoc, yLoc2);
					if (loc.region_id == regionId && loc.is_accessible(mobile_type))
						return true;
				}

				for (checkYLoc = yLoc1 + 1; checkYLoc < yLoc2; checkYLoc++)
				{
					loc = World.get_loc(xLoc1, checkYLoc);
					if (loc.region_id == regionId && loc.is_accessible(mobile_type))
						return true;

					loc = World.get_loc(xLoc2, checkYLoc);
					if (loc.region_id == regionId && loc.is_accessible(mobile_type))
						return true;
				}

				break;

			case UnitConstants.UNIT_SEA:
				for (checkXLoc = xLoc1; checkXLoc <= xLoc2; checkXLoc++)
				{
					if (checkXLoc % 2 == 0 && yLoc1 % 2 == 0)
					{
						loc = World.get_loc(checkXLoc, yLoc1);
						if (loc.region_id == regionId && loc.is_accessible(mobile_type))
							return true;
					}

					if (checkXLoc % 2 == 0 && yLoc2 % 2 == 0)
					{
						loc = World.get_loc(checkXLoc, yLoc2);
						if (loc.region_id == regionId && loc.is_accessible(mobile_type))
							return true;
					}
				}

				for (checkYLoc = yLoc1 + 1; checkYLoc < yLoc2; checkYLoc++)
				{
					if (xLoc1 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.get_loc(xLoc1, checkYLoc);
						if (loc.region_id == regionId && loc.is_accessible(mobile_type))
							return true;
					}

					if (xLoc2 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.get_loc(xLoc2, checkYLoc);
						if (loc.region_id == regionId && loc.is_accessible(mobile_type))
							return true;
					}
				}

				break;

			case UnitConstants.UNIT_AIR:
				for (checkXLoc = xLoc1; checkXLoc <= xLoc2; checkXLoc++)
				{
					if (checkXLoc % 2 == 0 && yLoc1 % 2 == 0)
					{
						loc = World.get_loc(checkXLoc, yLoc1);
						if (loc.is_accessible(mobile_type))
							return true;
					}

					if (checkXLoc % 2 == 0 && yLoc2 % 2 == 0)
					{
						loc = World.get_loc(checkXLoc, yLoc2);
						if (loc.is_accessible(mobile_type))
							return true;
					}
				}

				for (checkYLoc = yLoc1 + 1; checkYLoc < yLoc2; checkYLoc++)
				{
					if (xLoc1 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.get_loc(xLoc1, checkYLoc);
						if (loc.is_accessible(mobile_type))
							return true;
					}

					if (xLoc2 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.get_loc(xLoc2, checkYLoc);
						if (loc.is_accessible(mobile_type))
							return true;
					}
				}

				break;

			default:
				break;
		}

		return false;
	}

	private bool space_for_attack(int targetXLoc, int targetYLoc, int targetMobileType, int targetWidth, int targetHeight)
	{
		if (mobile_type == UnitConstants.UNIT_LAND && targetMobileType == UnitConstants.UNIT_LAND)
			return space_around_target(targetXLoc, targetYLoc, targetWidth, targetHeight);

		if ((mobile_type == UnitConstants.UNIT_SEA && targetMobileType == UnitConstants.UNIT_SEA) ||
		    (mobile_type == UnitConstants.UNIT_AIR && targetMobileType == UnitConstants.UNIT_AIR))
			return space_around_target_ver2(targetXLoc, targetYLoc, targetWidth, targetHeight);

		//-------------------------------------------------------------------------//
		// mobile_type is differet from that of target unit
		//-------------------------------------------------------------------------//
		Location loc = World.get_loc(next_x_loc(), next_y_loc());
		if (mobile_type == UnitConstants.UNIT_LAND && targetMobileType == UnitConstants.UNIT_SEA &&
		    !can_attack_different_target_type() && ship_surr_has_free_land(targetXLoc, targetYLoc, loc.region_id))
			return true;

		int maxRange = max_attack_range();
		if (maxRange == 1)
			return false;

		if (free_space_for_range_attack(targetXLoc, targetYLoc, targetWidth, targetHeight, targetMobileType, maxRange))
			return true;

		return false;
	}

	private bool space_around_target(int squareXLoc, int squareYLoc, int width, int height)
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
				loc = World.get_loc(squareXLoc + i, testYLoc);
				if (loc.can_move(mobile_type))
					sum ^= locWeight;
				else if (loc.has_unit(mobile_type))
				{
					unit = UnitArray[loc.unit_recno(mobile_type)];
					if (unit.cur_action != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (blocked_edge[0] != sum)
		{
			blocked_edge[0] = sum;
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
				loc = World.get_loc(testXLoc, squareYLoc + i);
				if (loc.can_move(mobile_type))
					sum ^= locWeight;
				else if (loc.has_unit(mobile_type))
				{
					unit = UnitArray[loc.unit_recno(mobile_type)];
					if (unit.cur_action != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (blocked_edge[1] != sum)
		{
			blocked_edge[1] = sum;
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
				loc = World.get_loc(squareXLoc + i, testYLoc);
				if (loc.can_move(mobile_type))
					sum ^= locWeight;
				else if (loc.has_unit(mobile_type))
				{
					unit = UnitArray[loc.unit_recno(mobile_type)];
					if (unit.cur_action != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (blocked_edge[2] != sum)
		{
			blocked_edge[2] = sum;
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
				loc = World.get_loc(testXLoc, squareYLoc + i);
				if (loc.can_move(mobile_type))
					sum ^= locWeight;
				else if (loc.has_unit(mobile_type))
				{
					unit = UnitArray[loc.unit_recno(mobile_type)];
					if (unit.cur_action != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (blocked_edge[3] != sum)
		{
			blocked_edge[3] = sum;
			equal = 0;
		}

		return equal == 0;
	}

	private bool space_around_target_ver2(int targetXLoc, int targetYLoc, int targetWidth, int targetHeight)
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
				loc = World.get_loc(i, yLoc1);
				if (loc.can_move(mobile_type))
					sum ^= locWeight;
				else if (loc.has_unit(mobile_type))
				{
					unit = UnitArray[loc.unit_recno(mobile_type)];
					if (unit.cur_action != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (blocked_edge[0] != sum)
		{
			blocked_edge[0] = sum;
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
				loc = World.get_loc(xLoc1, i);
				if (loc.can_move(mobile_type))
					sum ^= locWeight;
				else if (loc.has_unit(mobile_type))
				{
					unit = UnitArray[loc.unit_recno(mobile_type)];
					if (unit.cur_action != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (blocked_edge[1] != sum)
		{
			blocked_edge[1] = sum;
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
				loc = World.get_loc(i, yLoc2);
				if (loc.can_move(mobile_type))
					sum ^= locWeight;
				else if (loc.has_unit(mobile_type))
				{
					unit = UnitArray[loc.unit_recno(mobile_type)];
					if (unit.cur_action != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (blocked_edge[2] != sum)
		{
			blocked_edge[2] = sum;
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
				loc = World.get_loc(xLoc2, i);
				if (loc.can_move(mobile_type))
					sum ^= locWeight;
				else if (loc.has_unit(mobile_type))
				{
					unit = UnitArray[loc.unit_recno(mobile_type)];
					if (unit.cur_action != SPRITE_ATTACK)
						sum ^= locWeight;
				}
			}
		}

		if (blocked_edge[3] != sum)
		{
			blocked_edge[3] = sum;
			equal = 0;
		}

		return equal == 0;
	}

	private bool ship_surr_has_free_land(int targetXLoc, int targetYLoc, int regionId)
	{
		Location loc;
		int xShift, yShift, checkXLoc, checkYLoc;

		for (int i = 2; i < 9; i++)
		{
			Misc.cal_move_around_a_point(i, 3, 3, out xShift, out yShift);
			checkXLoc = targetXLoc + xShift;
			checkYLoc = targetYLoc + yShift;

			if (checkXLoc < 0 || checkXLoc >= GameConstants.MapSize || checkYLoc < 0 || checkYLoc >= GameConstants.MapSize)
				continue;

			loc = World.get_loc(checkXLoc, checkYLoc);
			if (loc.region_id == regionId && loc.can_move(mobile_type))
				return true;
		}

		return false;
	}

	private bool free_space_for_range_attack(int targetXLoc, int targetYLoc, int targetWidth, int targetHeight, int targetMobileType, int maxRange)
	{
		//if(mobile_type==UnitConstants.UNIT_AIR)
		//	return true; // air unit can reach any region

		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();

		if (Math.Abs(curXLoc - targetXLoc) <= maxRange &&
		    Math.Abs(curYLoc - targetYLoc) <= maxRange) // inside the attack range
			return true;

		Location loc = World.get_loc(curXLoc, curYLoc);
		int regionId = loc.region_id;
		int xLoc1 = Math.Max(targetXLoc - maxRange, 0);
		int yLoc1 = Math.Max(targetYLoc - maxRange, 0);
		int xLoc2 = Math.Min(targetXLoc + targetWidth - 1 + maxRange, GameConstants.MapSize - 1);
		int yLoc2 = Math.Min(targetYLoc + targetHeight - 1 + maxRange, GameConstants.MapSize - 1);
		int checkXLoc, checkYLoc;

		//--------- do adjustment for UnitConstants.UNIT_SEA and UnitConstants.UNIT_AIR ---------//
		if (mobile_type != UnitConstants.UNIT_LAND)
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
		switch (mobile_type)
		{
			case UnitConstants.UNIT_LAND:
				for (checkXLoc = xLoc1; checkXLoc <= xLoc2; checkXLoc++)
				{
					loc = World.get_loc(checkXLoc, yLoc1);
					if (loc.region_id == regionId && loc.can_move(mobile_type))
						return true;

					loc = World.get_loc(checkXLoc, yLoc2);
					if (loc.region_id == regionId && loc.can_move(mobile_type))
						return true;
				}

				for (checkYLoc = yLoc1 + 1; checkYLoc < yLoc2; checkYLoc++)
				{
					loc = World.get_loc(xLoc1, checkYLoc);
					if (loc.region_id == regionId && loc.can_move(mobile_type))
						return true;

					loc = World.get_loc(xLoc2, checkYLoc);
					if (loc.region_id == regionId && loc.can_move(mobile_type))
						return true;
				}

				break;

			case UnitConstants.UNIT_SEA:
				for (checkXLoc = xLoc1; checkXLoc <= xLoc2; checkXLoc++)
				{
					if (checkXLoc % 2 == 0 && yLoc1 % 2 == 0)
					{
						loc = World.get_loc(checkXLoc, yLoc1);
						if (loc.region_id == regionId && loc.can_move(mobile_type))
							return true;
					}

					if (checkXLoc % 2 == 0 && yLoc2 % 2 == 0)
					{
						loc = World.get_loc(checkXLoc, yLoc2);
						if (loc.region_id == regionId && loc.can_move(mobile_type))
							return true;
					}
				}

				for (checkYLoc = yLoc1 + 1; checkYLoc < yLoc2; checkYLoc++)
				{
					if (xLoc1 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.get_loc(xLoc1, checkYLoc);
						if (loc.region_id == regionId && loc.can_move(mobile_type))
							return true;
					}

					if (xLoc2 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.get_loc(xLoc2, checkYLoc);
						if (loc.region_id == regionId && loc.can_move(mobile_type))
							return true;
					}
				}

				break;

			case UnitConstants.UNIT_AIR:
				for (checkXLoc = xLoc1; checkXLoc <= xLoc2; checkXLoc++)
				{
					if (checkXLoc % 2 == 0 && yLoc1 % 2 == 0)
					{
						loc = World.get_loc(checkXLoc, yLoc1);
						if (loc.can_move(mobile_type))
							return true;
					}

					if (checkXLoc % 2 == 0 && yLoc2 % 2 == 0)
					{
						loc = World.get_loc(checkXLoc, yLoc2);
						if (loc.can_move(mobile_type))
							return true;
					}
				}

				for (checkYLoc = yLoc1 + 1; checkYLoc < yLoc2; checkYLoc++)
				{
					if (xLoc1 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.get_loc(xLoc1, checkYLoc);
						if (loc.can_move(mobile_type))
							return true;
					}

					if (xLoc2 % 2 == 0 && checkYLoc % 2 == 0)
					{
						loc = World.get_loc(xLoc2, checkYLoc);
						if (loc.can_move(mobile_type))
							return true;
					}
				}

				break;

			default:
				break;
		}

		return false;
	}

	private void choose_best_attack_mode(int attackDistance, int targetMobileType = UnitConstants.UNIT_LAND)
	{
		//------------ enable/disable range attack -----------//
		//cur_attack = 0;
		//return;

		//-------------------- define parameters -----------------------//
		int attackModeBeingUsed = cur_attack;
		//UCHAR maxAttackRangeMode = 0;
		int maxAttackRangeMode = cur_attack;
		AttackInfo attackInfoMaxRange = attack_info_array[0];
		AttackInfo attackInfoChecking;
		AttackInfo attackInfoSelected = attack_info_array[cur_attack];

		//--------------------------------------------------------------//
		// If targetMobileType==UnitConstants.UNIT_AIR or mobile_type==UnitConstants.UNIT_AIR,
		//	force to use range_attack.
		// If there is no range_attack, return 0, i.e. cur_attack=0
		//--------------------------------------------------------------//
		if (attack_count > 1)
		{
			bool canAttack = false;
			int checkingDamageWeight, selectedDamageWeight;

			for (int i = 0; i < attack_count; i++)
			{
				if (attackModeBeingUsed == i)
					continue; // it is the mode already used

				attackInfoChecking = attack_info_array[i];
				if (can_attack_with(attackInfoChecking) && attackInfoChecking.attack_range >= attackDistance)
				{
					//-------------------- able to attack ----------------------//
					canAttack = true;

					if (attackInfoSelected.attack_range < attackDistance)
					{
						attackModeBeingUsed = i;
						attackInfoSelected = attackInfoChecking;
						continue;
					}

					checkingDamageWeight = attackInfoChecking.attack_damage;
					selectedDamageWeight = attackInfoSelected.attack_damage;

					if (attackDistance == 1 &&
					    (targetMobileType != UnitConstants.UNIT_AIR && mobile_type != UnitConstants.UNIT_AIR))
					{
						//------------ force to use close attack if possible -----------//
						if (attackInfoSelected.attack_range == attackDistance)
						{
							if (attackInfoChecking.attack_range == attackDistance &&
							    checkingDamageWeight > selectedDamageWeight)
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
							    (targetMobileType != UnitConstants.UNIT_AIR && mobile_type != UnitConstants.UNIT_AIR))
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
					if (can_attack_with(attackInfoChecking) &&
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
				cur_attack = attackModeBeingUsed; // choose the strongest damage mode if able to attack
			else
				cur_attack = maxAttackRangeMode; //	choose the longest attack range if unable to attack

			attack_range = attack_info_array[cur_attack].attack_range;
		}
		else
		{
			cur_attack = 0; // only one mode is supported
			attack_range = attack_info_array[0].attack_range;
			return;
		}
	}
	
	public int max_attack_range()
	{
		int maxRange = 0;

		for (int i = 0; i < attack_count; i++)
		{
			AttackInfo attackInfo = attack_info_array[i];
			if (can_attack_with(attackInfo) && attackInfo.attack_range > maxRange)
				maxRange = attackInfo.attack_range;
		}

		return maxRange;
	}
	
	private bool can_attack_with(int i) // 0 to attack_count-1
	{
		AttackInfo attackInfo = attack_info_array[i];
		return skill.combat_level >= attackInfo.combat_level && cur_power >= attackInfo.min_power;
	}

	private bool can_attack_with(AttackInfo attackInfo)
	{
		return skill.combat_level >= attackInfo.combat_level && cur_power >= attackInfo.min_power;
	}

	public bool can_attack()
	{
		return can_attack_flag && attack_count > 0;
	}
	
	public void cycle_eqv_attack()
	{
		int trial = SpriteInfo.MAX_UNIT_ATTACK_TYPE + 2;
		if (attack_info_array[cur_attack].eqv_attack_next > 0)
		{
			do
			{
				cur_attack = attack_info_array[cur_attack].eqv_attack_next - 1;
			} while (!can_attack_with(cur_attack));
		}
		else
		{
			if (!can_attack_with(cur_attack))
			{
				// force to search again
				int attackRange = attack_info_array[cur_attack].attack_range;
				for (int i = 0; i < attack_count; i++)
				{
					AttackInfo attackInfo = attack_info_array[i];
					if (attackInfo.attack_range >= attackRange && can_attack_with(attackInfo))
					{
						cur_attack = i;
						break;
					}
				}
			}
		}
	}

	public virtual void fix_attack_info() // set attack_info_array appropriately
	{
		UnitInfo unitInfo = UnitRes[unit_id];

		attack_count = unitInfo.attack_count;

		if (attack_count > 0 && unitInfo.first_attack > 0)
		{
			attack_info_array = new AttackInfo[attack_count];
			for (int i = 0; i < attack_count; i++)
			{
				attack_info_array[i] = UnitRes.attack_info_array[unitInfo.first_attack - 1 + i];
			}
		}
		else
		{
			attack_info_array = null;
		}

		int old_attack_count = attack_count;
		int techLevel;
		if (unitInfo.unit_class == UnitConstants.UNIT_CLASS_WEAPON && (techLevel = get_weapon_version()) > 0)
		{
			switch (unit_id)
			{
				case UnitConstants.UNIT_BALLISTA:
				case UnitConstants.UNIT_F_BALLISTA:
					attack_count = 2;
					break;
				case UnitConstants.UNIT_EXPLOSIVE_CART:
					attack_count = 0;
					break;
				default:
					attack_count = 1;
					break;
			}

			if (attack_count > 0)
			{
				//TODO check this
				attack_info_array = new AttackInfo[attack_count];
				for (int i = 0; i < attack_count; i++)
				{
					attack_info_array[i] = UnitRes.attack_info_array[old_attack_count + (techLevel - 1) * attack_count + i];
				}
			}
			else
			{
				// no attack like explosive cart
				attack_info_array = null;
			}
		}
	}
	
	public bool is_action_attack()
	{
		switch (action_mode2)
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
	
	public void defense_attack_unit(int targetRecno)
	{
		action_mode2 = UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET;
		attack_unit(targetRecno, 0, 0, true);
	}

	public void defense_attack_firm(int targetXLoc, int targetYLoc)
	{
		action_mode2 = UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET;
		attack_firm(targetXLoc, targetYLoc);
	}

	public void defense_attack_town(int targetXLoc, int targetYLoc)
	{
		action_mode2 = UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET;
		attack_town(targetXLoc, targetYLoc);
	}

	public void defense_attack_wall(int targetXLoc, int targetYLoc)
	{
		action_mode2 = UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET;
		attack_wall(targetXLoc, targetYLoc);
	}

	public void defense_detect_target()
	{
		action_mode2 = UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET;
		action_para2 = UnitConstants.AUTO_DEFENSE_DETECT_COUNT;
		action_x_loc2 = -1;
		action_y_loc2 = -1;
	}

	private void general_defend_mode_detect_target(int checkDefendMode = 0)
	{
		stop();
		switch (action_mode2)
		{
			case UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET:
				defense_detect_target();
				break;

			case UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET:
				defend_town_detect_target();
				break;

			case UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET:
				monster_defend_detect_target();
				break;

			default:
				break;
		}
	}

	private bool general_defend_mode_process_attack_target()
	{
		Location loc;
		SpriteInfo spriteInfo;
		FirmInfo firmInfo;
		Unit unit = null;
		Firm firm = null;
		int clearToDetect = 0;

		//------------------------------------------------------------------------------//
		// if the unit's action mode is in defensive attack action, process the corresponding
		// checking.
		//------------------------------------------------------------------------------//
		switch (action_mode)
		{
			case UnitConstants.ACTION_ATTACK_UNIT:
				if (UnitArray.IsDeleted(action_para2))
				{
					clearToDetect++;
				}
				else
				{
					unit = UnitArray[action_para2];

					//if(unit.cur_action==SPRITE_IDLE)
					//	clearToDetect++;

					if (!nation_can_attack(unit.nation_recno)) // cannot attack this nation
						clearToDetect++;
				}

				break;

			case UnitConstants.ACTION_ATTACK_FIRM:
				if (FirmArray.IsDeleted(action_para2))
				{
					clearToDetect++;
				}
				else
				{
					firm = FirmArray[action_para2];

					if (!nation_can_attack(firm.nation_recno)) // cannot attack this nation
						clearToDetect++;
				}

				break;

			case UnitConstants.ACTION_ATTACK_TOWN:
				if (TownArray.IsDeleted(action_para2))
				{
					clearToDetect++;
				}
				else
				{
					Town town = TownArray[action_para2];

					if (!nation_can_attack(town.NationId)) // cannot attack this nation
						clearToDetect++;
				}

				break;

			case UnitConstants.ACTION_ATTACK_WALL:
				loc = World.get_loc(action_x_loc2, action_y_loc2);

				if (!loc.is_wall() || !nation_can_attack(loc.power_nation_recno))
					clearToDetect++;
				break;

			default:
				clearToDetect++;
				break;
		}

		//------------------------------------------------------------------------------//
		// suitation changed to defensive detecting mode
		//------------------------------------------------------------------------------//
		if (clearToDetect != 0)
		{
			//----------------------------------------------------------//
			// target is dead, change to detect state for another target
			//----------------------------------------------------------//
			reset_action_para();
			return true;
		}
		else if (waiting_term < UnitConstants.ATTACK_WAITING_TERM)
			waiting_term++;
		else
		{
			//------------------------------------------------------------------------------//
			// process the corresponding attacking procedure.
			//------------------------------------------------------------------------------//
			waiting_term = 0;
			switch (action_mode)
			{
				case UnitConstants.ACTION_ATTACK_UNIT:
					spriteInfo = unit.sprite_info;

					//-----------------------------------------------------------------//
					// attack the target if able to reach the target surrounding, otherwise
					// continue to wait
					//-----------------------------------------------------------------//
					action_x_loc2 = unit.next_x_loc(); // update target location
					action_y_loc2 = unit.next_y_loc();
					if (space_for_attack(action_x_loc2, action_y_loc2, unit.mobile_type,
						    spriteInfo.loc_width, spriteInfo.loc_height))
						attack_unit(unit.sprite_recno, 0, 0, true);
					break;

				case UnitConstants.ACTION_ATTACK_FIRM:
					firmInfo = FirmRes[firm.firm_id];

					//-----------------------------------------------------------------//
					// attack the target if able to reach the target surrounding, otherwise
					// continue to wait
					//-----------------------------------------------------------------//
					attack_firm(action_x_loc2, action_y_loc2);

					if (!is_in_surrounding(move_to_x_loc, move_to_y_loc, sprite_info.loc_width,
						    action_x_loc2, action_y_loc2, firmInfo.loc_width, firmInfo.loc_height))
						waiting_term = 0;
					break;

				case UnitConstants.ACTION_ATTACK_TOWN:
					//-----------------------------------------------------------------//
					// attack the target if able to reach the target surrounding, otherwise
					// continue to wait
					//-----------------------------------------------------------------//
					attack_town(action_x_loc2, action_y_loc2);

					if (!is_in_surrounding(move_to_x_loc, move_to_y_loc, sprite_info.loc_width,
						    action_x_loc2, action_y_loc2, InternalConstants.TOWN_WIDTH, InternalConstants.TOWN_HEIGHT))
						waiting_term = 0;
					break;

				case UnitConstants.ACTION_ATTACK_WALL:
					attack_wall(action_x_loc2, action_y_loc2);
					if (!is_in_surrounding(move_to_x_loc, move_to_y_loc, sprite_info.loc_width,
						    action_x_loc2, action_y_loc2, 1, 1))
						waiting_term = 0;
					break;

				default:
					break;
			}
		}

		return false;
	}

	//========== unit's defense mode ==========//
	private void defense_back_camp(int firmXLoc, int firmYLoc)
	{
		assign(firmXLoc, firmYLoc);
		action_mode2 = UnitConstants.ACTION_AUTO_DEFENSE_BACK_CAMP;
	}

	private void process_auto_defense_attack_target()
	{
		if (general_defend_mode_process_attack_target())
		{
			defense_detect_target();
		}
	}

	private void process_auto_defense_detect_target()
	{
		//----------------------------------------------------------------//
		// no target or target is out of detect range, so change state to
		// back camp
		//----------------------------------------------------------------//
		if (action_para2 == 0)
		{
			if (FirmArray.IsDeleted(action_misc_para))
			{
				process_auto_defense_back_camp();
				return;
			}

			Firm firm = FirmArray[action_misc_para];
			if (firm.firm_id != Firm.FIRM_CAMP || firm.nation_recno != nation_recno)
			{
				process_auto_defense_back_camp();
				return;
			}

			FirmCamp camp = (FirmCamp)firm;
			if (UnitArray.IsDeleted(camp.defend_target_recno))
			{
				process_auto_defense_back_camp();
				return;
			}

			Unit target = UnitArray[camp.defend_target_recno];
			if (target.action_mode != UnitConstants.ACTION_ATTACK_FIRM || target.action_para != camp.firm_recno)
			{
				process_auto_defense_back_camp();
				return;
			}

			//action_mode2 = UnitConstants.ACTION_AUTO_DEFENSE_DETECT_TARGET;
			action_para2 = UnitConstants.AUTO_DEFENSE_DETECT_COUNT;
			return;
		}

		//----------------------------------------------------------------//
		// defense_detecting target algorithm
		//----------------------------------------------------------------//
		int startLoc;
		int dimension;

		switch (action_para2 % InternalConstants.FRAMES_PER_DAY)
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
				action_para2--;
				return;
		}

		//---------------------------------------------------------------//
		// attack the target if target detected, or change the detect region
		//---------------------------------------------------------------//
		if (!idle_detect_attack(startLoc, dimension, 1)) // defense mode is on
			action_para2--;
	}

	private void process_auto_defense_back_camp()
	{
		int clearDefenseMode = 0;
		// the unit may become idle or unable to reach firm, reactivate it
		if (action_mode != UnitConstants.ACTION_ASSIGN_TO_FIRM)
		{
			if (action_misc != UnitConstants.ACTION_MISC_DEFENSE_CAMP_RECNO ||
			    action_misc_para == 0 || FirmArray.IsDeleted(action_misc_para))
				clearDefenseMode++;
			else
			{
				Firm firm = FirmArray[action_misc_para];
				if (firm.firm_id != Firm.FIRM_CAMP || firm.nation_recno != nation_recno)
					clearDefenseMode++;
				else
				{
					defense_back_camp(firm.loc_x1, firm.loc_y1); // go back to the military camp
					return;
				}
			}
		}
		else if (cur_action == SPRITE_IDLE)
		{
			if (FirmArray.IsDeleted(action_misc_para))
				clearDefenseMode++;
			else
			{
				Firm firm = FirmArray[action_misc_para];
				defense_back_camp(firm.loc_x1, firm.loc_y1);
				return;
			}
		}

		//----------------------------------------------------------------//
		// clear order if the camp is deleted
		//----------------------------------------------------------------//
		stop2();
		reset_action_misc_para();
	}

	private bool defense_follow_target()
	{
		const int PROB_HOSTILE_RETURN = 10;
		const int PROB_FRIENDLY_RETURN = 20;
		const int PROB_NEUTRAL_RETURN = 30;

		if (UnitArray.IsDeleted(action_para))
			return true;

		if (cur_action == SPRITE_ATTACK)
			return true;

		Unit target = UnitArray[action_para];
		Location loc = World.get_loc(action_x_loc, action_y_loc);
		if (!loc.has_unit(target.mobile_type))
			return true; // the target may be dead or invisible

		int returnFactor;

		//-----------------------------------------------------------------//
		// calculate the chance to go back to military camp in following the
		// target
		//-----------------------------------------------------------------//
		if (loc.power_nation_recno == nation_recno)
			return true; // target within our nation
		else if (loc.power_nation_recno == 0) // is neutral
			returnFactor = PROB_NEUTRAL_RETURN;
		else
		{
			Nation locNation = NationArray[loc.power_nation_recno];
			if (locNation.get_relation_status(nation_recno) == NationBase.NATION_HOSTILE)
				returnFactor = PROB_HOSTILE_RETURN;
			else
				returnFactor = PROB_FRIENDLY_RETURN;
		}

		SpriteInfo targetSpriteInfo = target.sprite_info;

		//-----------------------------------------------------------------//
		// if the target moves faster than this unit, it is more likely for
		// this unit to go back to military camp.
		//-----------------------------------------------------------------//
		//-**** should also consider the combat level and hit_points of both unit ****-//
		if (targetSpriteInfo.speed > sprite_info.speed)
			returnFactor -= 5;

		if (Misc.Random(returnFactor) != 0) // return to camp if true
			return true;

		process_auto_defense_back_camp();
		return false; // cancel attack
	}

	//========== town unit's defend mode ==========//
	private void defend_town_attack_unit(int targetRecno)
	{
		action_mode2 = UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET;
		attack_unit(targetRecno, 0, 0, true);
	}

	private void defend_town_detect_target()
	{
		action_mode2 = UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET;
		action_para2 = UnitConstants.UNIT_DEFEND_TOWN_DETECT_COUNT;
		action_x_loc2 = -1;
		action_y_loc2 = -1;
	}

	private void defend_town_back_town(int townRecno)
	{
		Town town = TownArray[townRecno];

		assign(town.LocX1, town.LocY1);
		action_mode2 = UnitConstants.ACTION_DEFEND_TOWN_BACK_TOWN;
	}

	private void process_defend_town_attack_target()
	{
		if (general_defend_mode_process_attack_target())
		{
			action_mode2 = UnitConstants.ACTION_DEFEND_TOWN_DETECT_TARGET;
			action_para2 = UnitConstants.UNIT_DEFEND_TOWN_DETECT_COUNT;
			action_x_loc2 = action_y_loc2 = -1;
		}
	}

	private void process_defend_town_detect_target()
	{
		//----------------------------------------------------------------//
		// no target or target is out of detect range, so change state to
		// back camp
		//----------------------------------------------------------------//
		if (action_para2 == 0)
		{
			int back = 0;

			if (TownArray.IsDeleted(action_misc_para))
				back++;
			else
			{
				Town town = TownArray[action_misc_para];
				if (UnitArray.IsDeleted(town.DefendTargetId))
					back++;
				else
				{
					Unit target = UnitArray[town.DefendTargetId];
					if (target.action_mode != UnitConstants.ACTION_ATTACK_TOWN || target.action_para != town.TownId)
						back++;
				}
			}

			if (back == 0)
			{
				//action_mode2 = ACTION_DEFEND_TOWN_DETECT_TARGET;
				action_para2 = UnitConstants.UNIT_DEFEND_TOWN_DETECT_COUNT;
				return;
			}

			process_defend_town_back_town();
			return;
		}

		//----------------------------------------------------------------//
		// defense_detecting target algorithm
		//----------------------------------------------------------------//
		int startLoc;
		int dimension;

		switch (action_para2 % InternalConstants.FRAMES_PER_DAY)
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
				action_para2--;
				return;
		}

		//---------------------------------------------------------------//
		// attack the target if target detected, or change the detect region
		//---------------------------------------------------------------//

		if (!idle_detect_attack(startLoc, dimension, 1)) // defense mode is on
			action_para2--;
	}

	private void process_defend_town_back_town()
	{
		int clearDefenseMode = 0;
		// the unit may become idle or unable to reach town, reactivate it
		if (action_mode != UnitConstants.ACTION_ASSIGN_TO_TOWN)
		{
			if (action_misc != UnitConstants.ACTION_MISC_DEFEND_TOWN_RECNO ||
			    action_misc_para == 0 || TownArray.IsDeleted(action_misc_para))
				clearDefenseMode++;
			else
			{
				Town town = TownArray[action_misc_para];
				if (town.NationId != nation_recno)
					clearDefenseMode++;
				else
				{
					defend_town_back_town(action_misc_para); // go back to the town
					return;
				}
			}
		}
		else if (cur_action == SPRITE_IDLE)
		{
			if (TownArray.IsDeleted(action_misc_para))
				clearDefenseMode++;
			else
			{
				defend_town_back_town(action_misc_para);
				return;
			}
		}

		//----------------------------------------------------------------//
		// clear order if the town is deleted
		//----------------------------------------------------------------//
		stop2();
		reset_action_misc_para();
	}

	private bool defend_town_follow_target()
	{
		if (cur_action == SPRITE_ATTACK)
			return true;

		if (TownArray.IsDeleted(unit_mode_para))
		{
			stop2(); //**** BUGHERE
			set_mode(0); //***BUGHERE
			return false;
		}

		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();

		Town town = TownArray[unit_mode_para];
		if ((curXLoc < town.LocCenterX - UnitConstants.UNIT_DEFEND_TOWN_DISTANCE) ||
		    (curXLoc > town.LocCenterX + UnitConstants.UNIT_DEFEND_TOWN_DISTANCE) ||
		    (curYLoc < town.LocCenterY - UnitConstants.UNIT_DEFEND_TOWN_DISTANCE) ||
		    (curYLoc > town.LocCenterY + UnitConstants.UNIT_DEFEND_TOWN_DISTANCE))
		{
			defend_town_back_town(unit_mode_para);
			return false;
		}

		return true;
	}

	private void monster_defend_attack_unit(int targetRecno)
	{
		action_mode2 = UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET;
		attack_unit(targetRecno, 0, 0, true);
	}

	private void monster_defend_attack_firm(int targetXLoc, int targetYLoc)
	{
		action_mode2 = UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET;
		attack_firm(targetXLoc, targetYLoc);
	}

	private void monster_defend_attack_town(int targetXLoc, int targetYLoc)
	{
		action_mode2 = UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET;
		attack_town(targetXLoc, targetYLoc);
	}

	private void monster_defend_attack_wall(int targetXLoc, int targetYLoc)
	{
		action_mode2 = UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET;
		attack_wall(targetXLoc, targetYLoc);
	}

	private void monster_defend_detect_target()
	{
		action_mode2 = UnitConstants.ACTION_MONSTER_DEFEND_DETECT_TARGET;
		action_para2 = UnitConstants.MONSTER_DEFEND_DETECT_COUNT;
		action_x_loc2 = -1;
		action_y_loc2 = -1;
	}

	private void monster_defend_back_firm(int firmXLoc, int firmYLoc)
	{
		assign(firmXLoc, firmYLoc);
		action_mode2 = UnitConstants.ACTION_MONSTER_DEFEND_BACK_FIRM;
	}

	private void process_monster_defend_attack_target()
	{
		if (general_defend_mode_process_attack_target())
		{
			monster_defend_detect_target();
		}
	}

	private void process_monster_defend_detect_target()
	{
		//----------------------------------------------------------------//
		// no target or target is out of detect range, so change state to
		// back camp
		//----------------------------------------------------------------//
		if (action_para2 == 0)
		{
			int back = 0;

			if (FirmArray.IsDeleted(action_misc_para))
				back++;
			else
			{
				FirmMonster firmMonster = (FirmMonster)FirmArray[action_misc_para];
				if (UnitArray.IsDeleted(firmMonster.defend_target_recno))
					back++;
				else
				{
					Unit target = UnitArray[firmMonster.defend_target_recno];
					if (target.action_mode != UnitConstants.ACTION_ATTACK_FIRM ||
					    target.action_para != firmMonster.firm_recno)
						back++;
				}
			}

			if (back == 0)
			{
				//action_mode2 = ACTION_MONSTER_DEFEND_DETECT_TARGET;
				action_para2 = UnitConstants.MONSTER_DEFEND_DETECT_COUNT;
				return;
			}

			process_monster_defend_back_firm();
			return;
		}

		//----------------------------------------------------------------//
		// defense_detecting target algorithm
		//----------------------------------------------------------------//
		int startLoc;
		int dimension;

		switch (action_para2 % InternalConstants.FRAMES_PER_DAY)
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
				action_para2--;
				return;
		}

		//---------------------------------------------------------------//
		// attack the target if target detected, or change the detect region
		//---------------------------------------------------------------//
		if (!idle_detect_attack(startLoc, dimension, 1)) // defense mode is on
			action_para2--;
	}

	private void process_monster_defend_back_firm()
	{
		int clearDefendMode = 0;
		// the unit may become idle or unable to reach firm, reactivate it
		if (action_mode != UnitConstants.ACTION_ASSIGN_TO_FIRM)
		{
			if (action_misc != UnitConstants.ACTION_MISC_MONSTER_DEFEND_FIRM_RECNO ||
			    action_misc_para == 0 || FirmArray.IsDeleted(action_misc_para))
				clearDefendMode++;
			else
			{
				Firm firm = FirmArray[action_misc_para];
				if (firm.firm_id != Firm.FIRM_MONSTER || firm.nation_recno != nation_recno)
					clearDefendMode++;
				else
				{
					monster_defend_back_firm(firm.loc_x1, firm.loc_y1); // go back to the military camp
					return;
				}
			}
		}
		else if (cur_action == SPRITE_IDLE)
		{
			if (FirmArray.IsDeleted(action_misc_para))
				clearDefendMode++;
			else
			{
				Firm firm = FirmArray[action_misc_para];
				monster_defend_back_firm(firm.loc_x1, firm.loc_y1);
				return;
			}
		}

		//----------------------------------------------------------------//
		// clear order if the camp is deleted
		//----------------------------------------------------------------//
		stop2();
		reset_action_misc_para();
	}

	private bool monster_defend_follow_target()
	{
		if (cur_action == SPRITE_ATTACK)
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
		int curXLoc = next_x_loc();
		int curYLoc = next_y_loc();

		Firm firm = FirmArray[action_misc_para];
		if ((curXLoc < firm.center_x - UnitConstants.MONSTER_DEFEND_FIRM_DISTANCE) ||
		    (curXLoc > firm.center_x + UnitConstants.MONSTER_DEFEND_FIRM_DISTANCE) ||
		    (curYLoc < firm.center_y - UnitConstants.MONSTER_DEFEND_FIRM_DISTANCE) ||
		    (curYLoc > firm.center_y + UnitConstants.MONSTER_DEFEND_FIRM_DISTANCE))
		{
			monster_defend_back_firm(firm.loc_x1, firm.loc_y1);
			return false;
		}

		return true;
	}
	
	public bool in_auto_defense_mode()
	{
		return (action_mode2 >= UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
		        action_mode2 <= UnitConstants.ACTION_AUTO_DEFENSE_BACK_CAMP);
	}

	public bool in_defend_town_mode()
	{
		return (action_mode2 >= UnitConstants.ACTION_DEFEND_TOWN_ATTACK_TARGET &&
		        action_mode2 <= UnitConstants.ACTION_DEFEND_TOWN_BACK_TOWN);
	}

	public bool in_monster_defend_mode()
	{
		return (action_mode2 >= UnitConstants.ACTION_MONSTER_DEFEND_ATTACK_TARGET &&
		        action_mode2 <= UnitConstants.ACTION_MONSTER_DEFEND_BACK_FIRM);
	}

	private bool in_any_defense_mode()
	{
		return action_mode2 >= UnitConstants.ACTION_AUTO_DEFENSE_ATTACK_TARGET &&
		       action_mode2 <= UnitConstants.ACTION_MONSTER_DEFEND_BACK_FIRM;
	}
	
	public void clear_unit_defense_mode()
	{
		//------- cancel defense mode and continue the current action -------//
		action_mode2 = action_mode;
		action_para2 = action_para;
		action_x_loc2 = action_x_loc;
		action_y_loc2 = action_y_loc;

		reset_action_misc_para();

		if (unit_mode == UnitConstants.UNIT_MODE_DEFEND_TOWN)
			set_mode(0); // reset unit mode 
	}

	public void clear_town_defend_mode()
	{
		//------- cancel defense mode and continue the current action -------//
		action_mode2 = action_mode;
		action_para2 = action_para;
		action_x_loc2 = action_x_loc;
		action_y_loc2 = action_y_loc;

		reset_action_misc_para();
	}

	public void clear_monster_defend_mode()
	{
		//------- cancel defense mode and continue the current action -------//
		action_mode2 = action_mode;
		action_para2 = action_para;
		action_x_loc2 = action_x_loc;
		action_y_loc2 = action_y_loc;

		reset_action_misc_para();
	}
}