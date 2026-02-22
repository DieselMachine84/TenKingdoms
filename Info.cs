using System;
using System.IO;

namespace TenKingdoms;

public class Info
{
    private const int MAX_RANK_TYPE = 5;
    
    // set to Game.GameStartDate in Info.Init(), the actual date the game begins, not the scenario begins
    public DateTime GameStartDate { get; private set; }
    public DateTime GameDate { get; private set; }
    public int GameDay { get; private set; }
    public int GameMonth { get; private set; }
    public int GameYear { get; private set; }
    public int TotalDays { get; private set; }

    public DateTime GoalDeadline { get; private set; }
    public int GoalDifficulty { get; private set; }
    public int GoalScoreBonus { get; private set; }

    public int YearDay { get; private set; } // the nth day in a year
    public int YearsPassed { get; private set; } // no. of years passed since the game begins

    public int StartPlayTime { get; private set; } // the time player start playing the game today
    public int TotalPlayTime { get; private set; } // total time the player has played in all saved games

    public int DefaultViewingNationId { get; set; }
    public int ViewingNationId { get; set; } // which nation the player is viewing at with the reports.
    public int ViewingSpyId { get; set; } // the id of the spy viewing the secret report of other nations

    private int[,] NationRanks { get; } = new int[MAX_RANK_TYPE, GameConstants.MAX_NATION];

    public Info()
    {
        GameDay = 1;
        GameMonth = 1;
        GameYear = 1000;

        GameStartDate = new DateTime(GameYear, GameMonth, GameDay);
        GameDate = GameStartDate;

        YearDay = GameStartDate.DayOfYear;
        YearsPassed = 0;

        GoalDeadline = new DateTime(GameYear + Sys.Instance.Config.GoalYearLimit, GameMonth, GameDay);
        GoalDifficulty = 0;
        GoalScoreBonus = 0;

        StartPlayTime = Misc.GetTime(); // the time player start playing the game
        TotalPlayTime = 0; // total time the player has played in all saved games
    }

    public void NextDay()
    {
        TotalDays++;
        if (++GameDay > 30)
            GameDay = 30; // game_day is limited to 1-30 for
        // calculation of e.g. revenue_30days()
        //TODO may be set to February 29
        GameDate = GameDate.AddDays(1.0);

        //-----------------------------------------//

        if (GameDate.Month != GameMonth)
        {
            GameDay = 1;
            GameMonth = GameDate.Month;

            Sys.Instance.FirmArray.NextMonth();
            Sys.Instance.NationArray.NextMonth();
        }

        if (GameDate.Year != GameYear)
        {
            GameMonth = 1;
            GameYear = GameDate.Year;
            YearsPassed++;

            Sys.Instance.FirmArray.NextYear();
            Sys.Instance.NationArray.NextYear();
        }

        //TODO in case of 31 day of month YearDay will be set to the same value
        YearDay = (new DateTime(GameYear, GameMonth, GameDay)).DayOfYear;

        //--- if a spy is viewing secret reports of other nations ---//

        if (ViewingSpyId != 0)
            ProcessViewingSpy();

        //-------- deadline approaching message -------//

        if (!Sys.Instance.GameEnded && Sys.Instance.Config.GoalYearLimitFlag)
        {
            int dayLeft = (GoalDeadline - GameDate).Days;
            int yearLeft = dayLeft / 365;

            if (dayLeft % 365 == 0 && yearLeft >= 1 && yearLeft <= 5)
                Sys.Instance.NewsArray.GoalDeadline(yearLeft, 0);

            if (dayLeft == 0) // deadline arrives, everybody loses the game
                Sys.Instance.EndGame(0, false);
        }
    }

    //TODO bad code
    public void SetRankData(bool onlyHasContact)
    {
        Nation viewingNation = null;
        NationArray nationArray = Sys.Instance.NationArray;

        if (nationArray.PlayerId != 0 && !nationArray.IsDeleted(ViewingNationId))
            viewingNation = nationArray[ViewingNationId];

        for (int i = 0; i < MAX_RANK_TYPE; i++)
        {
            for (int j = 0; j < GameConstants.MAX_NATION; j++)
            {
                NationRanks[i, j] = 0;
            }
        }

        foreach (Nation nation in nationArray)
        {
            if (onlyHasContact)
            {
                if (viewingNation != null && !viewingNation.GetRelation(nation.NationId).HasContact)
                    continue;
            }

            NationRanks[0, nation.NationId - 1] = nation.PopulationRating;

            NationRanks[1, nation.NationId - 1] = nation.MilitaryRating;

            NationRanks[2, nation.NationId - 1] = nation.EconomicRating;

            NationRanks[3, nation.NationId - 1] = (int)nation.Reputation;

            NationRanks[4, nation.NationId - 1] = (int)nation.KillMonsterScore;
        }
    }

    public int GetRankScore(int rankType, int nationId) // score functions
    {
        int maxValue = 1;

        switch (rankType)
        {
            case 1: // population
                maxValue = 100;
                break;

            case 2: // military strength
                maxValue = 200;
                break;

            case 3: // economic strength
                maxValue = 6000;
                break;

            case 4: // reputation
                maxValue = 100; // so the maximum score of the reputation portion is 50 only
                break;

            case 5: // monsters slain score
                maxValue = 1000;
                break;
        }

        int rankScore = 100 * NationRanks[rankType - 1, nationId - 1] / maxValue;

        return Math.Max(0, rankScore);
    }

    public int GetTotalScore(int nationId)
    {
        int totalScore = 0;

        for (int i = 0; i < MAX_RANK_TYPE; i++)
        {
            totalScore += GetRankScore(i + 1, nationId);
        }

        return totalScore;
    }
    
    public void ProcessViewingSpy()
    {
        //TODO
    }
    
    #region SaveAndLoad

    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(GameStartDate.ToBinary());
        writer.Write(GameDate.ToBinary());
        writer.Write(GameDay);
        writer.Write(GameMonth);
        writer.Write(GameYear);
        writer.Write(TotalDays);
        writer.Write(GoalDeadline.ToBinary());
        writer.Write(GoalDifficulty);
        writer.Write(GoalScoreBonus);
        writer.Write(YearDay);
        writer.Write(YearsPassed);
        writer.Write(StartPlayTime);
        writer.Write(TotalPlayTime);
        writer.Write(DefaultViewingNationId);
        writer.Write(ViewingNationId);
        writer.Write(ViewingSpyId);
        for (int i = 0; i < NationRanks.GetLength(0); i++)
            for (int j = 0; j < NationRanks.GetLength(1); j++)
                writer.Write(NationRanks[i, j]);
    }

    public void LoadFrom(BinaryReader reader)
    {
        GameStartDate = DateTime.FromBinary(reader.ReadInt64());
        GameDate = DateTime.FromBinary(reader.ReadInt64());
        GameDay = reader.ReadInt32();
        GameMonth = reader.ReadInt32();
        GameYear = reader.ReadInt32();
        TotalDays = reader.ReadInt32();
        GoalDeadline = DateTime.FromBinary(reader.ReadInt64());
        GoalDifficulty = reader.ReadInt32();
        GoalScoreBonus = reader.ReadInt32();
        YearDay = reader.ReadInt32();
        YearsPassed = reader.ReadInt32();
        StartPlayTime = reader.ReadInt32();
        TotalPlayTime = reader.ReadInt32();
        DefaultViewingNationId = reader.ReadInt32();
        ViewingNationId = reader.ReadInt32();
        ViewingSpyId = reader.ReadInt32();
        for (int i = 0; i < NationRanks.GetLength(0); i++)
            for (int j = 0; j < NationRanks.GetLength(1); j++)
                NationRanks[i, j] = reader.ReadInt32();
    }
	
    #endregion
}