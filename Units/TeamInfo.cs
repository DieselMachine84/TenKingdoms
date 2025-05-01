using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class TeamInfo
{
    public const int MAX_TEAM_MEMBER = 9;

    public List<int> Members { get; } = new List<int>();
    public DateTime AILastRequestDefenseDate { get; set; }

    public TeamInfo()
    {
    }
}