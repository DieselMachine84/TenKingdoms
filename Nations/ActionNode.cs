using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class ActionNode
{
    //public const int MAX_ACTION_GROUP_UNIT = 9;
    public const int ACTION_DYNAMIC = 0;
    public const int ACTION_FIXED = 1;

    public int action_mode; // eg build firm, attack, etc
    public int action_type; // action type. For 7kaa, this is always ACTION_FIXED.
    public int action_para; // parameter for the action. e.g. firmId for AI_BUILD_FIRM
    public int action_para2; // parameter for the action. e.g. firm race id. for building FirmBase
    public int action_id; // an unique id. for identifying this node

    public DateTime add_date; // when this action is added
    public int unit_recno; // unit associated with this action.

    public int action_x_loc; // can be firm loc, or target loc, etc
    public int action_y_loc;
    public int ref_x_loc; // reference x loc, eg the raw material location
    public int ref_y_loc;

    // number of term to wait before this action is removed from the array if it cannot be processed
    public int retry_count;

    public int instance_count; // no. of times this action needs to be carried out

    // for group unit actions, the no. of units in the array is stored in instance_count
    //public int[] group_unit_array = new int[MAX_ACTION_GROUP_UNIT];
    public List<int> group_unit_array = new List<int>();

    public int processing_instance_count;
    public int processed_instance_count;

    // continue processing this action after this date, this is used when training a unit for construction}
    public DateTime next_retry_date;
}