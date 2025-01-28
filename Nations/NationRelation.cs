using System;

namespace TenKingdoms;

public class NationRelation
{
    public bool has_contact; // whether this nation has been contacted or not

    // whether units should automatically attack units/firms of this nation when the relationship is hostile
    public bool should_attack;

    public bool trade_treaty; // whether allow trading with this nation

    public int status;

    public DateTime last_change_status_date;

    // AI's subjectively relation levels towards the others, the opposite nation's relation level is not the same as this
    public int ai_relation_level;

    public bool ai_secret_attack;
    public int ai_demand_trade_treaty;

    // a rating indicate how long does a good relation (friendly/alliance) lasts
    public double good_relation_duration_rating;

    // how many times this nation has started a war with us, the more the times the worse this nation is.
    public int started_war_on_us_count;

    public double[] cur_year_import = new double[NationBase.IMPORT_TYPE_COUNT];
    public double[] last_year_import = new double[NationBase.IMPORT_TYPE_COUNT];
    public double[] lifetime_import = new double[NationBase.IMPORT_TYPE_COUNT];

    public double import_365days(int importType)
    {
        return last_year_import[importType] * (365 - Info.year_day) / 365 + cur_year_import[importType];
    }

    // the date which the last diplomatic request was rejected.
    public DateTime[] last_talk_reject_date_array = new DateTime[TalkMsg.MAX_TALK_TYPE];

    public DateTime last_military_aid_date;

    // the last date which the current nation give tribute, aid or technology to this nation
    public DateTime last_give_gift_date;

    public int total_given_gift_amount; // the total amount of gift the current nation has given to this nation

    public bool contact_msg_flag; // used only in multiplayer

    private Info Info => Sys.Instance.Info;

    public static string[] relation_status_str_array = new string[] { "War", "Tense", "Neutral", "Friendly", "Alliance" };

    public static string[] duration_of_status_str_array = new string[]
    {
        "Duration of War Status", "Duration of Tense Status", "Duration of Neutral Status",
        "Duration of Friendly Status", "Duration of Alliance Status"
    };

    public string status_str()
    {
        return relation_status_str_array[status];
    }

    public string duration_of_status_str()
    {
        return duration_of_status_str_array[status];
    }

    public string status_duration_str()
    {
        int statusDays = (Info.game_date - last_change_status_date).Days;
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
}