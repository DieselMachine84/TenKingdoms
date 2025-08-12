using System;

namespace TenKingdoms;

public class Info
{
    public const int MAX_RANK_TYPE = 5;
    
    // set to Game::game_start_year in Info::init(), the actual date the game begins, not the scenario begins
    public DateTime game_start_date;

    public DateTime game_date;
    public int game_day;
    public int game_month;
    public int game_year;
    public int TotalDays { get; private set; }

    public DateTime goal_deadline;
    public int goal_difficulty;
    public int goal_score_bonus;

    public int year_day; // the nth day in a year
    public int year_passed; // no. of years passed since playing

    public int random_seed;

    public int start_play_time; // the time player start playing the game today
    public int total_play_time; // total time the player has played in all saved games

    public int viewing_nation_recno;		// which nation the player is viewing at with the reports.
    public int viewing_spy_recno;			// the recno of the spy viewing the secret report of other nations
    public int default_viewing_nation_recno;

    private int[,] nation_rank_data_array = new int[MAX_RANK_TYPE, GameConstants.MAX_NATION];

    public Info()
    {
        game_day = 1;
        game_month = 1;
        game_year = 1000;

        game_start_date = new DateTime(game_year, game_month, game_day);

        game_date = game_start_date;

        year_day = game_start_date.DayOfYear;

        year_passed = 0; // no. of years has been passed since the game begins

        goal_deadline = new DateTime(game_year + Sys.Instance.Config.goal_year_limit, game_month, game_day);

        goal_difficulty = 0;
        goal_score_bonus = 0;

        start_play_time = Misc.GetTime(); // the time player start playing the game
        total_play_time = 0; // total time the player has played in all saved games
    }

    public void init_random_seed(int randomSeed)
    {
        if (randomSeed != 0)
            random_seed = randomSeed;
        else
        {
            randomSeed = (int)(DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds;
            randomSeed = randomSeed << 4 | randomSeed >> (32 - 4); // _rotr( randomSeed, 4 )
            if (randomSeed < 0)
                randomSeed = ~randomSeed;
            if (randomSeed == 0)
                randomSeed = 1;
            random_seed = randomSeed;
        }

        Misc.set_random_seed(random_seed);
    }

    public void next_day()
    {
        TotalDays++;
        if (++game_day > 30)
            game_day = 30; // game_day is limited to 1-30 for
        // calculation of e.g. revenue_30days()
        game_date = game_date.AddDays(1.0);

        //-----------------------------------------//

        if (game_date.Month != game_month)
        {
            game_day = 1;
            game_month = game_date.Month;

            Sys.Instance.FirmArray.NextMonth();
            Sys.Instance.NationArray.NextMonth();
        }

        if (game_date.Year != game_year)
        {
            game_month = 1;
            game_year = game_date.Year;
            year_passed++;

            Sys.Instance.FirmArray.NextYear();
            Sys.Instance.NationArray.NextYear();
        }

        //-------- set year_day ----------//

        year_day = (new DateTime(game_year, game_month, game_day)).DayOfYear;

        //--- if a spy is viewing secret reports of other nations ---//

        if (viewing_spy_recno != 0)
            process_viewing_spy();

        //-------- deadline approaching message -------//

        if (!Sys.Instance.GameEnded && Sys.Instance.Config.goal_year_limit_flag)
        {
            int dayLeft = (goal_deadline - game_date).Days;
            int yearLeft = dayLeft / 365;

            if (dayLeft % 365 == 0 && yearLeft >= 1 && yearLeft <= 5)
                Sys.Instance.NewsArray.goal_deadline(yearLeft, 0);

            if (dayLeft == 0) // deadline arrives, everybody loses the game
                Sys.Instance.EndGame(0, 0);
        }
    }

    public void init_player_reply(int talkToNationRecno)
    {
        //TODO
    }

    public void player_reply_chat(int withNationRecno)
    {
        //TODO
    }

    public void process_viewing_spy()
    {
        //TODO
    }
    
    public void set_rank_data(bool onlyHasContact)
    {
        Nation viewingNation = null;
        NationArray nationArray = Sys.Instance.NationArray;

        if (nationArray.player_recno != 0 && !nationArray.IsDeleted(viewing_nation_recno))
            viewingNation = nationArray[viewing_nation_recno];

        for (int i = 0; i < MAX_RANK_TYPE; i++)
        {
            for (int j = 0; j < GameConstants.MAX_NATION; j++)
            {
                nation_rank_data_array[i, j] = 0;
            }
        }

        foreach (Nation nation in nationArray)
        {
            if (onlyHasContact)
            {
                if (viewingNation != null && !viewingNation.get_relation(nation.nation_recno).has_contact)
                    continue;
            }

            nation_rank_data_array[0, nation.nation_recno - 1] = nation.population_rating;

            nation_rank_data_array[1, nation.nation_recno - 1] = nation.military_rating;

            nation_rank_data_array[2, nation.nation_recno - 1] = nation.economic_rating;

            nation_rank_data_array[3, nation.nation_recno - 1] = (int)nation.reputation;

            nation_rank_data_array[4, nation.nation_recno - 1] = (int)nation.kill_monster_score;
        }
    }

    public int get_rank_score(int rankType, int nationRecno) // score functions
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

        int rankScore = 100 * nation_rank_data_array[rankType - 1, nationRecno - 1] / maxValue;

        return Math.Max(0, rankScore);
    }

    public int get_total_score(int nationRecno)
    {
        int totalScore = 0;

        for (int i = 0; i < MAX_RANK_TYPE; i++)
        {
            totalScore += get_rank_score(i + 1, nationRecno);
        }

        return totalScore;
    }
}