using System;
using System.Collections.Generic;
using System.IO;

namespace TenKingdoms;

public class TeamInfo
{
    public const int MAX_TEAM_MEMBER = 9;

    public List<int> Members { get; } = new List<int>();
    public DateTime AILastRequestDefenseDate { get; set; }

    public TeamInfo()
    {
    }
    
	#region SaveAndLoad

	public void SaveTo(BinaryWriter writer)
	{
		writer.Write(Members.Count);
		for (int i = 0; i < Members.Count; i++)
			writer.Write(Members[i]);
		writer.Write(AILastRequestDefenseDate.ToBinary());
	}

	public void LoadFrom(BinaryReader reader)
	{
		int membersCount = reader.ReadInt32();
		for (int i = 0; i < membersCount; i++)
			Members.Add(reader.ReadInt32());
		AILastRequestDefenseDate = DateTime.FromBinary(reader.ReadInt64());
	}
	
	#endregion
}