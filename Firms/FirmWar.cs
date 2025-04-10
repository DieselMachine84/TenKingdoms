using System;
using System.Collections.Generic;
using System.Linq;

namespace TenKingdoms;

public class FirmWar : Firm
{
    public const int MAX_BUILD_QUEUE = 20;
    public const int FIRMWAR_BUILD_BATCH_COUNT = 10;

    public int build_unit_id;
    public long last_process_build_frame_no;
    public double build_progress_days;

    public Queue<int> build_queue = new Queue<int>();

    public FirmWar()
    {
        firm_skill_id = Skill.SKILL_MFT;
    }

    public override void change_nation(int newNationRecno)
    {
        //--- empty the build queue ---//

        // Note: this fixes a bug with a nation-changed war factory building a weapon that the nation doesn't have,
        //       which leads to a crash (when selecting attack sprite) because nation_contribution (= weapon level), and from this cur_attack, is not set properly.
        if (build_unit_id != 0)
            cancel_build_unit();
        build_queue.Clear();

        //-------- change the nation of this firm now ----------//

        base.change_nation(newNationRecno);
    }

    public override void next_day()
    {
        base.next_day();

        //----------- update population -------------//

        recruit_worker();

        //-------- train up the skill ------------//

        update_worker();

        //--------- calculate productivity ----------//

        calc_productivity();

        //--------- process building weapon -------//

        if (build_unit_id != 0)
            process_build();
        else
            process_queue();
    }

    public override bool is_operating()
    {
        return productivity > 0 && build_unit_id != 0;
    }

    public override void process_ai()
    {
        //---- think about which technology to research ----//

        if (build_unit_id == 0)
            think_new_production();

        //------- recruit workers ---------//

        //TODO do not recruit workers. Sell firm if it is not linked to a village
        if (Info.TotalDays % 15 == firm_recno % 15)
        {
            if (workers.Count < MAX_WORKER)
                ai_recruit_worker();
        }

        //----- think about closing down this firm -----//

        if (Info.TotalDays % 30 == firm_recno % 30)
        {
            if (think_del())
                return;
        }
    }

    public void add_queue(int unitId, int amount = 1)
    {
        if (amount < 0)
            return;

        int queueSpace = MAX_BUILD_QUEUE - build_queue.Count - (build_unit_id > 0 ? 1 : 0);
        int enqueueAmount = Math.Min(queueSpace, amount);

        for (int i = 0; i < enqueueAmount; ++i)
            build_queue.Enqueue(unitId);
    }

    public void remove_queue(int unitId, int amount = 1)
    {
        if (amount < 1)
            return;

        List<int> newQueue = build_queue.ToList();
        for (int i = newQueue.Count - 1; i >= 0 && amount > 0; i--)
        {
            if (newQueue[i] == unitId)
            {
                newQueue.RemoveAt(i);
                amount--;
            }
        }

        build_queue.Clear();
        foreach (var item in newQueue)
            build_queue.Enqueue(item);

        // If there were less units of unitId in the queue than were requested to be removed then
        // also cancel build unit
        if (amount > 0 && build_unit_id == unitId)
            cancel_build_unit();
    }

    private void process_queue()
    {
        if (build_queue.Count == 0)
            return;

        //--- first check if the nation has enough money to build the weapon ---//

        Nation nation = NationArray[nation_recno];

        if (nation.cash < UnitRes[build_unit_id].build_cost)
        {
            build_unit_id = 0;
            return;
        }

        nation.add_expense(NationBase.EXPENSE_WEAPON, UnitRes[build_unit_id].build_cost);

        build_unit_id = build_queue.Dequeue();

        //------- set building parameters -------//

        last_process_build_frame_no = Sys.Instance.FrameNumber;
        build_progress_days = 0.0;

        if (FirmArray.selected_recno == firm_recno)
        {
            //TODO drawing 
            //disable_refresh = 1;
            //info.disp();
            //disable_refresh = 0;
        }
    }

    public void cancel_build_unit()
    {
        build_unit_id = 0;

        if (FirmArray.selected_recno == firm_recno)
        {
            //TODO drawing
            //disable_refresh = 1;
            //info.disp();
            //disable_refresh = 0;
        }
    }

    private void process_build()
    {
        UnitInfo unitInfo = UnitRes[build_unit_id];
        int totalBuildDays = unitInfo.build_days;

        //TODO DieselMachine strange formula
        build_progress_days += (workers.Count * 6 + productivity / 2.0) / 100.0;

        last_process_build_frame_no = Sys.Instance.FrameNumber;

        if (Config.fast_build && nation_recno == NationArray.player_recno)
            build_progress_days += 2.0;

        if (build_progress_days > totalBuildDays)
        {
            SpriteInfo spriteInfo = SpriteRes[unitInfo.sprite_id];
            int xLoc = loc_x1; // xLoc & yLoc are used for returning results
            int yLoc = loc_y1;

            if (!World.LocateSpace(ref xLoc, ref yLoc, loc_x2, loc_y2,
                    spriteInfo.loc_width, spriteInfo.loc_height, unitInfo.mobile_type))
            {
                build_progress_days = totalBuildDays + 1;
                return;
            }

            UnitArray.AddUnit(build_unit_id, nation_recno, 0, 0, xLoc, yLoc);

            if (FirmArray.selected_recno == firm_recno)
            {
                //TODO drawing
                //disable_refresh = 1;
                //info.disp();
                //disable_refresh = 0;
            }

            if (own_firm())
                SERes.far_sound(center_x, center_y, 1, 'F', firm_id, "FINS", 'S',
                    UnitRes[build_unit_id].sprite_id);

            build_unit_id = 0;
        }
    }

    //-------- AI actions ---------//

    private void think_new_production()
    {
        //----- first see if we have enough money to build & support the weapon ----//

        if (!should_build_new_weapon())
            return;

        //---- calculate the average instance count of all available weapons ---//

        int weaponTypeCount = 0, totalWeaponCount = 0;

        for (int unitId = 1; unitId <= UnitConstants.MAX_UNIT_TYPE; unitId++)
        {
            UnitInfo unitInfo = UnitRes[unitId];

            if (unitInfo.unit_class != UnitConstants.UNIT_CLASS_WEAPON ||
                unitInfo.get_nation_tech_level(nation_recno) == 0)
            {
                continue;
            }

            if (unitId == UnitConstants.UNIT_EXPLOSIVE_CART) // AI doesn't use Porcupine
                continue;

            weaponTypeCount++;
            totalWeaponCount += unitInfo.nation_unit_count_array[nation_recno - 1];
        }

        if (weaponTypeCount == 0) // none of weapon technologies is available
            return;

        int averageWeaponCount = totalWeaponCount / weaponTypeCount;

        //----- think about which is best to build now ------//

        int bestRating = 0, bestUnitId = 0;

        for (int unitId = 1; unitId <= UnitConstants.MAX_UNIT_TYPE; unitId++)
        {
            UnitInfo unitInfo = UnitRes[unitId];

            if (unitInfo.unit_class != UnitConstants.UNIT_CLASS_WEAPON)
                continue;

            int techLevel = unitInfo.get_nation_tech_level(nation_recno);

            if (techLevel == 0)
                continue;

            //**BUGHERE, don't produce it yet, it needs a different usage than the others.
            if (unitId == UnitConstants.UNIT_EXPLOSIVE_CART)
                continue;

            int unitCount = unitInfo.nation_unit_count_array[nation_recno - 1];

            int curRating = averageWeaponCount - unitCount + techLevel * 3;

            if (curRating > bestRating)
            {
                bestRating = curRating;
                bestUnitId = unitId;
            }
        }

        //------------------------------------//

        if (bestUnitId != 0)
            add_queue(bestUnitId);
    }

    private bool should_build_new_weapon()
    {
        //----- first see if we have enough money to build & support the weapon ----//

        Nation nation = NationArray[nation_recno];

        if (nation.true_profit_365days() < 0) // don't build new weapons if we are currently losing money
            return false;

        // if weapon expenses are larger than 30% to 80% of the total income, don't build new weapons
        if (nation.expense_365days(NationBase.EXPENSE_WEAPON) >
            nation.income_365days() * (30 + nation.pref_use_weapon / 2) / 100)
        {
            return false;
        }

        //----- see if there is any space on existing camps -----//


        foreach (int campRecno in nation.ai_camp_array)
        {
            Firm firm = FirmArray[campRecno];

            if (firm.region_id != region_id)
                continue;

            if (firm.workers.Count < MAX_WORKER) // there is space in this firm
                return true;
        }

        return false;
    }

    private bool think_del()
    {
        if (workers.Count > 0)
            return false;

        //-- check whether the firm is linked to any towns or not --//

        for (int i = 0; i < linked_town_array.Count; i++)
        {
            if (linked_town_enable_array[i] == InternalConstants.LINK_EE)
                return false;
        }

        //------------------------------------------------//

        ai_del_firm();

        return true;
    }
    
    public override void DrawDetails(IRenderer renderer)
    {
        renderer.DrawWarFactoryDetails(this);
    }

    public override void HandleDetailsInput(IRenderer renderer)
    {
        renderer.HandleWarFactoryDetailsInput(this);
    }
}