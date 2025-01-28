using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class TeamInfo
{
    public const int MAX_TEAM_MEMBER = 9;

    public List<int> member_unit_array = new List<int>();
    public DateTime ai_last_request_defense_date;

    public TeamInfo()
    {
    }
}