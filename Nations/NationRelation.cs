using System;
using System.IO;

namespace TenKingdoms;

public class NationRelation
{
    public bool HasContact { get; set; } // whether this nation has been contacted or not

    // whether units should automatically attack units/firms of this nation when the relationship is hostile
    public bool ShouldAttack { get; set; }

    public bool TradeTreaty { get; set; } // whether allow trading with this nation

    public int Status { get; set; }
    public DateTime LastChangeStatusDate { get; set; }

    // a rating indicate how long does a good relation (friendly/alliance) lasts
    public double GoodRelationDurationRating { get; set; }

    // how many times this nation has started a war with us, the more the times the worse this nation is.
    public int StartedWarOnUsCount { get; set; }

    public double[] CurYearImport { get; } = new double[NationBase.IMPORT_TYPE_COUNT];
    public double[] LastYearImport { get; } = new double[NationBase.IMPORT_TYPE_COUNT];

    // the date which the last diplomatic request was rejected.
    public DateTime[] LastTalkRejectDates { get; } = new DateTime[TalkMsg.MAX_TALK_TYPE];
    public DateTime LastMilitaryAidDate { get; set; }
    // the last date which the current nation give tribute, aid or technology to this nation
    public DateTime LastGiveGiftDate { get; set; }
    public int TotalGivenGiftAmount { get; set; } // the total amount of gift the current nation has given to this nation

    // AI's subjectively relation levels towards the others, the opposite nation's relation level is not the same as this
    public int AIRelationLevel { get; set; }
    public bool AISecretAttack { get; set; }
    public int AIDemandTradeTreaty { get; set; }
    
    public bool ContactMsgFlag { get; set; } // used only in multiplayer
    
    private static string[] RelationStatusStrings { get; } = { "War", "Tense", "Neutral", "Friendly", "Alliance" };

    private static string[] DurationOfStatusStrings { get; } =
    {
        "Duration of War Status", "Duration of Tense Status", "Duration of Neutral Status",
        "Duration of Friendly Status", "Duration of Alliance Status"
    };

    private Info Info => Sys.Instance.Info;
    
    public double Import365Days(int importType)
    {
        return LastYearImport[importType] * (365 - Info.YearDay) / 365 + CurYearImport[importType];
    }
    
    public string StatusString()
    {
        return RelationStatusStrings[Status];
    }

    public string DurationOfStatusString()
    {
        return DurationOfStatusStrings[Status];
    }

    public string StatusDurationString()
    {
        int statusDays = (Info.GameDate - LastChangeStatusDate).Days;
        int statusYear = statusDays / 365;
        int statusMonth = (statusDays - statusYear * 365) / 30;

        string str = String.Empty;

        if (statusYear >= 1)
        {
            str += statusYear.ToString() + " year";
            if (statusYear > 1)
                str += "s";
            str += " and ";
        }

        str += statusMonth.ToString() + " month";
        if (statusMonth > 1)
            str += "s";
        return str;
    }
    
    #region SaveAndLoad

    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(HasContact);
        writer.Write(ShouldAttack);
        writer.Write(TradeTreaty);
        writer.Write(Status);
        writer.Write(LastChangeStatusDate.ToBinary());
        writer.Write(GoodRelationDurationRating);
        writer.Write(StartedWarOnUsCount);
        for (int i = 0; i < CurYearImport.Length; i++)
            writer.Write(CurYearImport[i]);
        for (int i = 0; i < LastYearImport.Length; i++)
            writer.Write(LastYearImport[i]);
        for (int i = 0; i < LastTalkRejectDates.Length; i++)
            writer.Write(LastTalkRejectDates[i].ToBinary());
        writer.Write(LastMilitaryAidDate.ToBinary());
        writer.Write(LastGiveGiftDate.ToBinary());
        writer.Write(TotalGivenGiftAmount);
        writer.Write(AIRelationLevel);
        writer.Write(AISecretAttack);
        writer.Write(AIDemandTradeTreaty);
        writer.Write(ContactMsgFlag);
    }

    public void LoadFrom(BinaryReader reader)
    {
        HasContact = reader.ReadBoolean();
        ShouldAttack = reader.ReadBoolean();
        TradeTreaty = reader.ReadBoolean();
        Status = reader.ReadInt32();
        LastChangeStatusDate = DateTime.FromBinary(reader.ReadInt64());
        GoodRelationDurationRating = reader.ReadDouble();
        StartedWarOnUsCount = reader.ReadInt32();
        for (int i = 0; i < CurYearImport.Length; i++)
            CurYearImport[i] = reader.ReadDouble();
        for (int i = 0; i < LastYearImport.Length; i++)
            LastYearImport[i] = reader.ReadDouble();
        for (int i = 0; i < LastTalkRejectDates.Length; i++)
            LastTalkRejectDates[i] = DateTime.FromBinary(reader.ReadInt64());
        LastMilitaryAidDate = DateTime.FromBinary(reader.ReadInt64());
        LastGiveGiftDate = DateTime.FromBinary(reader.ReadInt64());
        TotalGivenGiftAmount = reader.ReadInt32();
        AIRelationLevel = reader.ReadInt32();
        AISecretAttack = reader.ReadBoolean();
        AIDemandTradeTreaty = reader.ReadInt32();
        ContactMsgFlag = reader.ReadBoolean();
    }
	
    #endregion
}