using System;

namespace TenKingdoms;

[Flags]
public enum WeatherType
{
	WEATHER_SUNNY = 0x00,
	WEATHER_CLOUDY = 0x01,
	WEATHER_RAIN = 0x02,
	WEATHER_LIGHTNING = 0x04,
	WEATHER_LIGHTNING_RAIN = 0x06,
	WEATHER_WINDY = 0x08,
	WEATHER_WINDY_STORM = 0x0a,
	WEATHER_HOT_WAVE = 0x10,
	WEATHER_COLD_WAVE = 0x20,
	WEATHER_SNOW = 0x40
}

public class Weather
{
	private const int RAIN_CLOUD = 1;
	private const int LIGHTNING_CLOUD = 2;
	private const int WINDY = 4;
	private const int HOT_WAVE = 8;
	private const int COLD_WAVE = 0x10;

	private uint Seed { get; set; }
	private int SeasonPhase { get; set; } // 0 = early spring, 364 = end of winter
	private int AvgTemp { get; set; }
	private int TempAmp { get; set; }

	private int WindSpd { get; set; }
	private int HighWindDay { get; set; }
	private int WindDir { get; set; }
	private int WindySpeed { get; set; }
	private int TornadoCount { get; set; } // 0=today has tornado, 1... no. of days of last tornado

	private int CurCloudStrength { get; set; } // 0 (shine) to 10 (dark)
	private int CurCloudLen { get; set; }
	private int CurCloudType { get; set; } // type of cloud
	
	private int QuakeFrequency { get; set; }
	private int DayToQuake { get; set; }
	private int QuakeX { get; set; } // center of quake, generated on the day of quake
	private int QuakeY { get; set; }

	private Config Config => Sys.Instance.Config;
	private MagicWeather MagicWeather => Sys.Instance.MagicWeather;

	public Weather()
	{
	}

	public Weather(Weather other)
	{
		Seed = other.Seed;
		SeasonPhase = other.SeasonPhase;
		AvgTemp = other.AvgTemp;
		TempAmp = other.TempAmp;

		WindSpd = other.WindSpd;
		HighWindDay = other.HighWindDay;
		WindDir = other.WindDir;
		WindySpeed = other.WindySpeed;
		TornadoCount = other.TornadoCount;

		CurCloudStrength = other.CurCloudStrength;
		CurCloudLen = other.CurCloudLen;
		CurCloudType = other.CurCloudType;
		
		QuakeFrequency = other.QuakeFrequency;
		DayToQuake = other.DayToQuake;
		QuakeX = other.QuakeX;
		QuakeY = other.QuakeY;
	}

	public void InitDate(int year, int month, int day, int latitude, int quakeFreq)
	{
		// ----------- initialize random seed ------------//
		Seed = (uint)(2 * year + 1);
		RandomSeed(10);

		// ----------- calculate SeasonPhase from month, day ------------//
		SeasonPhase = (int)(month * 30.4 + day);
		SeasonPhase = (SeasonPhase + 365 - 98) % 365; // 7th Mar becomes 0

		// ----------- random number to earthquake -----------//
		QuakeFrequency = quakeFreq;
		DayToQuake = quakeFreq + RandomSeed(quakeFreq);

		// ----------- determine AvgTemp and TempAmp from latitude
		double angle = latitude * Math.PI / 180.0;
		AvgTemp = (int)(35.0 - Math.Abs(latitude / 90.0 * 40.0));
		TempAmp = (int)(17.0 * Math.Abs(angle)); // negative for South Hemisphere

		// ----------- determine cloud ----------- //
		CurCloudStrength = RandomSeed(4);
		CurCloudLen = 5 + RandomSeed(5);
		CurCloudType = 0;

		// ----------- determine wind ---------------//
		WindDir = RandomSeed(360);
		WindSpd = 10;
		HighWindDay = 0;
		WindySpeed = 0;

		TornadoCount = 1;
	}

	public void NextDay()
	{
		SeasonPhase = (SeasonPhase + 1) % 365;

		//---------- update/determine earthquake day ---------//
		if (DayToQuake != 0)
		{
			DayToQuake--;
			if (IsQuake())
			{
				QuakeX = RandomSeed(0x10000) * Config.MapSize / 0x10000;
				QuakeY = RandomSeed(0x10000) * Config.MapSize / 0x10000;
			}
		}
		else
		{
			DayToQuake = QuakeFrequency + RandomSeed(QuakeFrequency);
		}

		WindDir = (WindDir + RandomSeed(5)) % 360;
		WindSpd += RandomSeed(9) - 4 - (HighWindDay / 16);
		if (WindSpd < -10)
			WindSpd = -10;
		if (WindSpd > 110)
			WindSpd = 110;

		if (WindSpd >= 20)
			HighWindDay++;
		else
			HighWindDay--;

		if (CurCloudLen > 0)
		{
			CurCloudLen--;
		}
		else
		{
			int temperature = BaseTemperature();
			int maxCloudStrength;
			if (temperature >= 30)
				maxCloudStrength = 10;
			else if (temperature <= 18)
				maxCloudStrength = 4;
			else
				maxCloudStrength = (temperature - 18) / 2 + 4;
			
			CurCloudStrength = RandomSeed(maxCloudStrength + 4) - 3; // range : -2 to maxCloudStr
			if (CurCloudStrength < 0)
				CurCloudStrength = 0;
			CurCloudLen = 2 + RandomSeed(3) + RandomSeed(3);
			CurCloudType = 0;

			// ------- summer weather -------
			if (CurCloudStrength > 4)
			{
				if (RandomSeed(10) < CurCloudStrength)
					CurCloudType |= RAIN_CLOUD;
				if (CurCloudStrength >= 6 && RandomSeed(10) < CurCloudStrength - 4)
					CurCloudType |= WINDY;
			}

			if (CurCloudStrength <= 1 && temperature >= 30 && RandomSeed(10) <= 1)
			{
				CurCloudType |= HOT_WAVE;
			}

			// ------- winter weather -------
			if (temperature < 15)
			{
				if (RandomSeed(20) < 2)
					CurCloudType |= COLD_WAVE;

				if (temperature >= 10 && RandomSeed(10) < 3)
					CurCloudType |= WINDY;
				if (temperature < 10 && RandomSeed(10) < 7)
					CurCloudType |= WINDY;
			}

			if ((CurCloudType & WINDY) != 0)
			{
				WindySpeed = 10 + CurCloudStrength * 5 + RandomSeed(2 * CurCloudStrength + 1);
			}
			else
			{
				WindySpeed = 0;
				if (CurCloudStrength > 4 && RandomSeed(50) < CurCloudStrength + 2)
					CurCloudType |= LIGHTNING_CLOUD;
			}

			// ---- double the time of snow ------ //
			if (SnowScale() != 0)
				CurCloudLen += CurCloudLen;

		}

		//TODO check conditions
		// -------- update TornadoCount, at least 30 days between two tornadoes -------//
		if (TornadoCount > 30/* && base_temp() >= 30 && wind_speed() >= 40*/ && RandomSeed(10) == 0)
		{
			TornadoCount = 0; // today has a tornado
		}
		else
		{
			TornadoCount++;
		}
	}

	private int Cloud() // return 0 (shine) to 10 (dark)
	{
		if (CurCloudStrength < 0)
			return 0;
		if (CurCloudStrength > 10)
			return 10;
		return CurCloudStrength;
	}

	private int BaseTemperature()
	{
		return (int)(AvgTemp + TempAmp * Math.Sin(SeasonPhase / 365.0 * 2.0 * Math.PI));
	}
	
	public int Temperature() // temperature in degree C
	{
		return BaseTemperature() - (CurCloudStrength < 1 ? 0 : (CurCloudStrength < 4 ? 2 : 4)) +
			((CurCloudType & HOT_WAVE) != 0 ? 8 : 0) - ((CurCloudType & COLD_WAVE) != 0 ? 10 : 0);
	}

	public int WindSpeed() // wind speed 0 to 100
	{
		if (this == Sys.Instance.Weather && MagicWeather.WindDay > 0)
			return MagicWeather.WindSpeed;
		
		int w = WindSpd + WindySpeed;
		if (w < 0)
			return 0;
		if (w > 100)
			return 100;
		return w;
	}

	private int WindDirection() // 0 to 360
	{
		if (this == Sys.Instance.Weather && MagicWeather.WindDay > 0)
			return MagicWeather.WindDirection;
		
		return WindDir;
	}

	public double WindDirectionRadians() // in radian
	{
		if (this == Sys.Instance.Weather && MagicWeather.WindDay > 0)
			return MagicWeather.WindDirectionRadians;
		
		return WindDir * Math.PI / 180.0;
	}

	public int RainScale() // rain scale, 0 (no rain) to 12 (heavy rain)
	{
		if (this == Sys.Instance.Weather && MagicWeather.RainDay > 0)
			return MagicWeather.RainStrength;
		
		return CurCloudStrength > 4 ? CurCloudStrength * 2 - 8 : 0;
	}

	public int SnowScale() // snow scale, 0 (no snow) to 8 (heavy snow)
	{
		int temperature = Temperature();
		if (temperature > 0)
			return 0;

		if (temperature <= -30)
			return 8;
		if (temperature <= -25)
			return 7;
		if (temperature <= -20)
			return 6;
		if (temperature <= -15)
			return 5;
		if (temperature <= -10)
			return 4;
		if (temperature <= -5)
			return 3;
		if (temperature <= -2)
			return 2;
		return 1;
	}

	public bool IsLightning()
	{
		if (MagicWeather.LightningDay > 0)
			return true;
		
		return (CurCloudType & LIGHTNING_CLOUD) != 0;
	}

	public bool IsQuake()
	{
		return DayToQuake == 0;
	}

	public int QuakeRate(int x, int y) // 0-100
	{
		int dist = Math.Max(Math.Abs(x - QuakeX), Math.Abs(y - QuakeY));
		int damage = 100 - dist / 2;
		return damage > 0 ? damage : 0;
	}
	
	public bool HasTornado()
	{
		return TornadoCount == 0;
	}

	public int TornadoLocX(int maxLocX)
	{
		int dir = (WindDirection() + 180) % 360;

		if (dir < 45)
		{
			// north side
			return maxLocX * (dir + 45) / 90;
		}
		else if (dir < 135)
		{
			// east side
			return maxLocX - 1;
		}
		else if (dir < 225)
		{
			// south side
			return maxLocX * (224 - dir) / 90;
		}
		else if (dir < 315)
		{
			// west side
			return 0;
		}
		else
		{
			// north side
			return maxLocX * (dir - 315) / 90;
		}
	}

	public int TornadoLocY(int maxLocY)
	{
		int dir = (WindDirection() + 180) % 360;

		if (dir < 45)
		{
			// north side
			return 0;
		}
		else if (dir < 135)
		{
			// east side
			return maxLocY * (dir - 45) / 90;
		}
		else if (dir < 225)
		{
			// south side
			return maxLocY - 1;
		}
		else if (dir < 315)
		{
			// west side
			return maxLocY * (314 - dir) / 90;
		}
		else
		{
			// north side
			return 0;
		}
	}

	public WeatherType Description()
	{
		WeatherType w = WeatherType.WEATHER_SUNNY;
		if (RainScale() > 0)
			w |= WeatherType.WEATHER_RAIN;
		if (IsLightning())
			w |= WeatherType.WEATHER_LIGHTNING;
		if (SnowScale() > 0)
			w |= WeatherType.WEATHER_SNOW;
		else if ((CurCloudType & COLD_WAVE) != 0)
			w |= WeatherType.WEATHER_COLD_WAVE;
		if ((CurCloudType & HOT_WAVE) != 0)
			w |= WeatherType.WEATHER_HOT_WAVE;
		if ((CurCloudType & WINDY) != 0)
			w |= WeatherType.WEATHER_WINDY;

		if (w == WeatherType.WEATHER_SUNNY && Cloud() >= 4)
			w |= WeatherType.WEATHER_CLOUDY;

		return w;
	}

	private int RandomSeed(int max)
	{
		const int MULTIPLIER = 0x015a4e35;
		const int INCREMENT = 1;
		Seed = MULTIPLIER * Seed + INCREMENT;
		return (int)(Seed % max);
	}
}