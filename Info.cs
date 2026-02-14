using System;

namespace TenKingdoms;

public class Info
{
    private const int MAX_RANK_TYPE = 5;
    
    // set to Game.GameStartDate in Info.Init(), the actual date the game begins, not the scenario begins
    public DateTime GameStartDate { get; }
    public DateTime GameDate { get; private set; }
    public int GameDay { get; private set; }
    public int GameMonth { get; private set; }
    public int GameYear { get; private set; }
    public int TotalDays { get; private set; }

    public DateTime GoalDeadline { get; }
    public int GoalDifficulty { get; }
    public int GoalScoreBonus { get; }

    public int YearDay { get; private set; } // the nth day in a year
    public int YearsPassed { get; private set; } // no. of years passed since the game begins

    public int RandomSeed { get; private set; }

    public int StartPlayTime { get; } // the time player start playing the game today
    public int TotalPlayTime { get; } // total time the player has played in all saved games

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

    public void InitRandomSeed(int randomSeed)
    {
        if (randomSeed != 0)
            RandomSeed = randomSeed;
        else
        {
            randomSeed = (int)(DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds;
            randomSeed = randomSeed << 4 | randomSeed >> (32 - 4); // _rotr( randomSeed, 4 )
            if (randomSeed < 0)
                randomSeed = ~randomSeed;
            if (randomSeed == 0)
                randomSeed = 1;
            RandomSeed = randomSeed;
        }

        Misc.set_random_seed(RandomSeed);
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
                Sys.Instance.EndGame(0, 0);
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
}