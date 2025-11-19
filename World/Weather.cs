using System;

namespace TenKingdoms;

[Flags]
public enum WeatherType
{
	WEATHER_SUNNY = 0x00,
	WEATHER_CLOUDY = 0x01,

	WEATHER_RAIN = 0x02,
	WEATHER_LIGHTNING = 0x04,
	WEATHER_LIGHTN_RAIN = 0x06,
	WEATHER_WINDY = 0x08,
	WEATHER_WINDY_STORM = 0x0a,
	WEATHER_HOT_WAVE = 0x10,

	WEATHER_COLD_WAVE = 0x20,
	WEATHER_SNOW = 0x40
}

public class Weather
{
	public const int RAIN_CLOUD = 1;
	public const int LIGHTNING_CLOUD = 2;
	public const int WINDY = 4;
	public const int HOT_WAVE = 8;
	public const int COLD_WAVE = 0x10;

	private uint seed;
	private int season_phase; // 0 = early spring, 364 = end of winter
	private int day_to_quake;
	private int avg_temp;
	private int temp_amp;

	private int wind_spd;
	private int high_wind_day;
	private int wind_dir;
	private int windy_speed;
	private int tornado_count; // 0=today has tornado, 1... no. of days of last tornado

	private int cur_cloud_str; // 0 (shine) to 10 (dark)
	private int cur_cloud_len;
	private int cur_cloud_type; // type of cloud
	private int quake_frequency;

	private int quake_x; // center of quake, generated on the day of quake
	private int quake_y;

	private MagicWeather MagicWeather => Sys.Instance.MagicWeather;

	public Weather()
	{
	}

	public Weather(Weather other)
	{
		seed = other.seed;
		season_phase = other.season_phase;
		day_to_quake = other.day_to_quake;
		avg_temp = other.avg_temp;
		temp_amp = other.temp_amp;

		wind_spd = other.wind_spd;
		high_wind_day = other.high_wind_day;
		wind_dir = other.wind_dir;
		windy_speed = other.windy_speed;
		tornado_count = other.tornado_count;

		cur_cloud_str = other.cur_cloud_str;
		cur_cloud_len = other.cur_cloud_len;
		cur_cloud_type = other.cur_cloud_type;
		quake_frequency = other.quake_frequency;

		quake_x = other.quake_x;
		quake_y = other.quake_y;
	}

	public void init_date(int year, int month, int day, int latitude, int quakeFreq)
	{
		// ----------- initialize random seed ------------//
		seed = (uint)(2 * year + 1);
		rand_seed(10);

		// ----------- calculate season_phase from month, day ------------//
		season_phase = (int)(month * 30.4 + day);
		season_phase = (season_phase + 365 - 98) % 365; // 7th Mar becomes 0

		// ----------- random number to earthquake -----------//
		quake_frequency = quakeFreq;
		day_to_quake = quakeFreq + rand_seed(quakeFreq);

		// ----------- determine avg_temp and temp_amp from latitude
		double angle = latitude * Math.PI / 180.0;
		avg_temp = (int)(35.0 - Math.Abs(latitude / 90.0 * 40.0));
		temp_amp = (int)(17.0 * Math.Abs(angle)); // negative for South Hemisphere

		// ----------- determine cloud ----------- //
		cur_cloud_str = rand_seed(4);
		cur_cloud_len = 5 + rand_seed(5);
		cur_cloud_type = 0;

		// ----------- determine wind ---------------//
		wind_dir = rand_seed(360);
		wind_spd = 10;
		high_wind_day = 0;
		windy_speed = 0;

		tornado_count = 1;
	}

	public void next_day() // called when a day has passed
	{
		season_phase = (season_phase + 1) % 365;

		//---------- update/determine earthquake day ---------//
		if (day_to_quake != 0)
		{
			day_to_quake--;
			if (is_quake())
			{
				// generate quake_x, quake_y
				quake_x = rand_seed(0x10000) * GameConstants.MapSize / 0x10000;
				quake_y = rand_seed(0x10000) * GameConstants.MapSize / 0x10000;
			}
		}
		else
		{
			day_to_quake = quake_frequency + rand_seed(quake_frequency);
		}

		//---------- update wind ----------//
		wind_dir = (wind_dir + rand_seed(5)) % 360;
		wind_spd += rand_seed(9) - 4 - (high_wind_day / 16);
		if (wind_spd < -10)
			wind_spd = -10;
		if (wind_spd > 110)
			wind_spd = 110;

		if (wind_spd >= 20)
			high_wind_day++;
		else
			high_wind_day--;

		//---------- generate cloud --------//
		if (cur_cloud_len > 0)
		{
			cur_cloud_len--;
		}
		else
		{
			int t = base_temp();
			int maxCloudStr;
			if (t >= 30)
				maxCloudStr = 10;
			else if (t <= 18)
				maxCloudStr = 4;
			else
				maxCloudStr = (t - 18) / 2 + 4;
			cur_cloud_str = rand_seed(maxCloudStr + 4) - 3; // range : -2 to maxCloudStr
			if (cur_cloud_str < 0)
				cur_cloud_str = 0;
			cur_cloud_len = 2 + rand_seed(3) + rand_seed(3);

			cur_cloud_type = 0;

			// ------- summer weather
			if (cur_cloud_str > 4)
			{
				if ((char)rand_seed(10) < cur_cloud_str)
					cur_cloud_type |= RAIN_CLOUD;
				if (cur_cloud_str >= 6 && (char)rand_seed(10) < cur_cloud_str - 4)
					cur_cloud_type |= WINDY;
			}

			if (cur_cloud_str <= 1 && t >= 30 && rand_seed(10) <= 1)
			{
				cur_cloud_type |= HOT_WAVE;
			}

			// ------- winter weather
			if (t < 15)
			{
				if (rand_seed(20) < 2)
					cur_cloud_type |= COLD_WAVE;

				if (t >= 10 && rand_seed(10) < 3)
					cur_cloud_type |= WINDY;
				if (t < 10 && rand_seed(10) < 7)
					cur_cloud_type |= WINDY;
			}

			if ((cur_cloud_type & WINDY) != 0)
				windy_speed = 10 + cur_cloud_str * 5 + rand_seed(2 * cur_cloud_str + 1);
			else
			{
				windy_speed = 0;
				if (cur_cloud_str > 4 && (char)rand_seed(50) < cur_cloud_str + 2)
					cur_cloud_type |= LIGHTNING_CLOUD;
			}

			// ---- double the time of snow ------ //
			if (snow_scale() != 0)
				cur_cloud_len += cur_cloud_len;

		}

		//TODO check conditions
		// -------- update tornado_count, at least 20 days between two tornadoes -------//
		if (tornado_count > 20/* && base_temp() >= 30 && wind_speed() >= 40*/ && rand_seed(10) == 0)
		{
			tornado_count = 0; // today has a tornado
		}
		else
		{
			tornado_count++;
		}
	}

	public int cloud() // return 0 (shine) to 10 (dark)
	{
		if (cur_cloud_str < 0)
			return 0;
		if (cur_cloud_str > 10)
			return 10;
		return cur_cloud_str;
	}

	public int temp_c() // temperature in degree C
	{
		return base_temp() - (cur_cloud_str < 1 ? 0 : (cur_cloud_str < 4 ? 2 : 4)) +
			((cur_cloud_type & HOT_WAVE) != 0 ? 8 : 0) - ((cur_cloud_type & COLD_WAVE) != 0 ? 10 : 0);
	}

	public int wind_speed() // wind speed 0 to 100
	{
		if (this == Sys.Instance.Weather && MagicWeather.wind_day > 0)
			return MagicWeather.wind_speed();
		int w = wind_spd + windy_speed;
		if (w < 0)
			return 0;
		if (w > 100)
			return 100;
		return w;
	}

	public int wind_direct() // 0 to 360
	{
		if (this == Sys.Instance.Weather && MagicWeather.wind_day > 0)
			return MagicWeather.wind_direct();
		return wind_dir;
	}

	public double wind_direct_rad() // in radian
	{
		if (this == Sys.Instance.Weather && MagicWeather.wind_day > 0)
			return MagicWeather.wind_direct_rad();
		return wind_dir * Math.PI / 180.0;
	}

	public int rain_scale() // rain scale, 0 (no rain) to 12 (heavy rain)
	{
		if (this == Sys.Instance.Weather && MagicWeather.rain_day > 0)
			return MagicWeather.rain_scale();
		return cur_cloud_str > 4 ? cur_cloud_str * 2 - 8 : 0;
	}

	public int snow_scale() // snow scale, 0 (no snow) to 8 (heavy snow)
	{
		int t = temp_c();
		if (t > 0)
			return 0;

		if (t <= -15)
		{
			if (t <= -30)
				return 8;
			if (t <= -25)
				return 7;
			if (t <= -20)
				return 6;
			return 5;
		}
		else
		{
			if (t <= -10)
				return 4;
			if (t <= -5)
				return 3;
			if (t <= -2)
				return 2;
			return 1;
		}
	}

	public bool is_lightning()
	{
		if (MagicWeather.lightning_day > 0)
			return true;
		return (cur_cloud_type & LIGHTNING_CLOUD) != 0;
	}

	public bool is_quake()
	{
		return day_to_quake == 0;
	}

	public bool has_tornado()
	{
		return tornado_count == 0;
	}

	public int tornado_x_loc(int maxXLoc, int maxYLoc)
	{
		int dir = (wind_direct() + 180) % 360;

		if (dir < 45)
		{
			// north side
			return maxXLoc * (dir + 45) / 90;
		}
		else if (dir < 135)
		{
			// east side
			return maxXLoc - 1;
		}
		else if (dir < 225)
		{
			// south side
			return maxXLoc * (224 - dir) / 90;
		}
		else if (dir < 315)
		{
			// west side
			return 0;
		}
		else
		{
			// north side
			return maxXLoc * (dir - 315) / 90;
		}
	}

	public int tornado_y_loc(int maxXLoc, int maxYLoc)
	{
		int dir = (wind_direct() + 180) % 360;

		if (dir < 45)
		{
			// north side
			return 0;
		}
		else if (dir < 135)
		{
			// east side
			return maxYLoc * (dir - 45) / 90;
		}
		else if (dir < 225)
		{
			// south side
			return maxYLoc - 1;
		}
		else if (dir < 315)
		{
			// west side
			return maxYLoc * (314 - dir) / 90;
		}
		else
		{
			// north side
			return 0;
		}
	}

	public WeatherType desc()
	{
		WeatherType w = WeatherType.WEATHER_SUNNY;
		if (rain_scale() > 0)
			w |= WeatherType.WEATHER_RAIN;
		if (is_lightning())
			w |= WeatherType.WEATHER_LIGHTNING;
		if (snow_scale() > 0)
			w |= WeatherType.WEATHER_SNOW;
		else if ((cur_cloud_type & COLD_WAVE) != 0)
			w |= WeatherType.WEATHER_COLD_WAVE;
		if ((cur_cloud_type & HOT_WAVE) != 0)
			w |= WeatherType.WEATHER_HOT_WAVE;
		if ((cur_cloud_type & WINDY) != 0)
			w |= WeatherType.WEATHER_WINDY;

		if (w == WeatherType.WEATHER_SUNNY && cloud() >= 4)
			w |= WeatherType.WEATHER_CLOUDY;

		return w;
	}

	public int quake_rate(int x, int y) // 0-100
	{
		int dist = Math.Max(Math.Abs(x - quake_x), Math.Abs(y - quake_y));
		int damage = 100 - dist / 2;
		return damage > 0 ? damage : 0;
	}

	private int base_temp()
	{
		return (int)(avg_temp + temp_amp * Math.Sin(season_phase / 365.0 * 2.0 * Math.PI));
	}

	public int rand_seed(int max)
	{
		const int MULTIPLIER = 0x015a4e35;
		const int INCREMENT = 1;
		seed = MULTIPLIER * seed + INCREMENT;
		return (int)(seed % max);
	}
}