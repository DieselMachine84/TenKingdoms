using System;
using System.IO;

namespace TenKingdoms;

public class Config
{
	private const string CONFIG_FILE_NAME = "Config.txt";
	
	public const int OPTION_NONE = 0;
	public const int OPTION_LOW = 1;
	public const int OPTION_MODERATE = 2;
	public const int OPTION_HIGH = 3;
	public const int OPTION_VERY_HIGH = 4;
	
	public const int OPTION_MONSTER_NONE = 0;
	public const int OPTION_MONSTER_DEFENSIVE = 1;
	public const int OPTION_MONSTER_OFFENSIVE = 2;

	public const int OPTION_DISPLAY_MAJOR_NEWS = 0;
	public const int OPTION_DISPLAY_ALL_NEWS = 1;
	
	// ---- New game settings ----
	public int PlayerRaceId { get; private set; } = 1;	
	public int PlayerColor { get; private set; } = 1;
	public string PlayerName { get; private set; } = "New Player";
	public int MapSize { get; private set; } = 200;
	public int LandMass { get; private set; } = OPTION_MODERATE;
	public int TerrainSet { get; private set; } = 1;
	public int AINationCount { get; private set; } = 9;
	public int AIAggressiveness { get; private set; } = OPTION_MODERATE;
	public int StartUpCash { get; private set; } = OPTION_MODERATE;
	public int AIStartUpCash { get; private set; } = OPTION_MODERATE;
	public double CustomStartUpCash { get; private set; }
	public double CustomAIStartUpCash { get; private set; }
	public double CustomStartUpFood { get; private set; }
	public double CustomAIStartUpFood { get; private set; }
	public int RawResourceCount { get; private set; } = 5;
	public bool RawResourceNearby { get; private set; }
	public int MonsterType { get; private set; } = OPTION_MONSTER_DEFENSIVE;
	public int MonsterLairCount { get; private set; } = 15;
	public int IndependentTownCount { get; private set; } = 15;
	public int IndependentTownResistance { get; private set; } = OPTION_MODERATE;
	public bool NewIndependentTownEmerge { get; private set; } = true;
	public bool NewNationEmerge { get; private set; }
	public bool RandomStartUp { get; private set; }
	public bool ExploreWholeMap { get; private set; } = true;
	public bool FogOfWar { get; private set; }
	public int EarthquakeFrequency { get; private set; } = OPTION_NONE;
	
	// ---- Goal settings ----
	public bool GoalDestroyMonsterFlag { get; private set; }
	public bool GoalPopulationFlag { get; private set; }
	public bool GoalEconomicScoreFlag { get; private set; }
	public bool GoalTotalScoreFlag { get; private set; }
	public bool GoalYearLimitFlag { get; private set; }
	public int GoalPopulation { get; private set; } = 300;
	public int GoalEconomicScore { get; private set; } = 300;
	public int GoalTotalScore { get; private set; } = 600;
	public int GoalYearLimit { get; private set; } = 20;
	
	// ---- Options settings ----
	public int GameScreenWidth { get; private set; } = 30;
	public int GameScreenHeight { get; private set; } = 19;
	public int ShowUnitPath { get; private set; } = 3; // bit 0 show unit path on main view, bit 1 for minimap
	public int DisplayNewsType { get; private set; } = OPTION_DISPLAY_ALL_NEWS;
	public bool MusicFlag { get; private set; } = true;
	public int MusicVolume { get; private set; } = 70; // a value from 0 to 100
	public bool SoundEffectFlag { get; private set; } = true;
	public int SoundEffectVolume { get; private set; } = 70; // a value from 0 to 100
	public bool PanControl { get; private set; } = true; // mono speaker should disable PanControl
	
	// ---- Custom settings ----
	public bool UnlimitedRawResource { get; private set; }
	public bool FastBuild { get; private set; }
	public bool ShowAIInfo { get; private set; }

	public string Load()
	{
		if (!File.Exists(CONFIG_FILE_NAME))
			return CONFIG_FILE_NAME + " is not found";

		try
		{
			using FileStream stream = new FileStream(CONFIG_FILE_NAME, FileMode.Open, FileAccess.Read);
			using StreamReader reader = new StreamReader(stream);
			string config = reader.ReadToEnd();
			string[] lines = config.Split(Environment.NewLine);
			for (int i = 0; i < lines.Length; i++)
			{
				string line = lines[i].Trim();
				if (String.IsNullOrEmpty(line) || line.StartsWith("//"))
					continue;

				if (line.StartsWith("PlayerRaceId"))
				{
					PlayerRaceId = ParseIntParameter(line, "PlayerRaceId");
					PlayerRaceId = Math.Max(PlayerRaceId, 1);
					PlayerRaceId = Math.Min(PlayerRaceId, 10);
				}

				if (line.StartsWith("PlayerColor"))
				{
					PlayerColor = ParseIntParameter(line, "PlayerColor");
					PlayerColor = Math.Max(PlayerColor, 1);
					PlayerColor = Math.Min(PlayerColor, 10);
				}

				if (line.StartsWith("PlayerName"))
				{
					PlayerName = ParseStringParameter(line, "PlayerName");
				}

				if (line.StartsWith("MapSize"))
				{
					MapSize = ParseIntParameter(line, "MapSize");
					if (MapSize != 100 && MapSize != 200 && MapSize != 300 && MapSize != 400)
						return "Incorrect MapSize value";
				}

				if (line.StartsWith("LandMass"))
				{
					LandMass = ParseIntParameter(line, "LandMass");
					if (LandMass != 1 && LandMass != 2 && LandMass != 3)
						return "Incorrect LandMass value";
				}
				
				if (line.StartsWith("TerrainSet"))
				{
					TerrainSet = ParseIntParameter(line, "TerrainSet");
					if (TerrainSet != 1 && TerrainSet != 2 && TerrainSet != 3)
						return "Incorrect TerrainSet value";
				}
				
				if (line.StartsWith("AINationCount"))
				{
					AINationCount = ParseIntParameter(line, "AINationCount");
					AINationCount = Math.Max(AINationCount, 0);
					AINationCount = Math.Min(AINationCount, 10);
				}
				
				if (line.StartsWith("AIAggressiveness"))
				{
					AIAggressiveness = ParseIntParameter(line, "AIAggressiveness");
					if (AIAggressiveness != 1 && AIAggressiveness != 2 && AIAggressiveness != 3 && AIAggressiveness != 4)
						return "Incorrect AIAggressiveness value";
				}
				
				if (line.StartsWith("StartUpCash"))
				{
					StartUpCash = ParseIntParameter(line, "StartUpCash");
					if (StartUpCash != 1 && StartUpCash != 2 && StartUpCash != 3 && StartUpCash != 4)
						return "Incorrect StartUpCash value";
				}
				
				if (line.StartsWith("AIStartUpCash"))
				{
					AIStartUpCash = ParseIntParameter(line, "AIStartUpCash");
					if (AIStartUpCash != 1 && AIStartUpCash != 2 && AIStartUpCash != 3 && AIStartUpCash != 4)
						return "Incorrect AIStartUpCash value";
				}
				
				if (line.StartsWith("CustomStartUpCash"))
				{
					CustomStartUpCash = ParseIntParameter(line, "CustomStartUpCash");
					CustomStartUpCash = Math.Max(CustomStartUpCash, 0);
					CustomStartUpCash = Math.Min(CustomStartUpCash, 100000);
				}
				
				if (line.StartsWith("CustomAIStartUpCash"))
				{
					CustomAIStartUpCash = ParseIntParameter(line, "CustomAIStartUpCash");
					CustomAIStartUpCash = Math.Max(CustomAIStartUpCash, 0);
					CustomAIStartUpCash = Math.Min(CustomAIStartUpCash, 100000);
				}
				
				if (line.StartsWith("CustomStartUpFood"))
				{
					CustomStartUpFood = ParseIntParameter(line, "CustomStartUpFood");
					CustomStartUpFood = Math.Max(CustomStartUpFood, 0);
					CustomStartUpFood = Math.Min(CustomStartUpFood, 100000);
				}
				
				if (line.StartsWith("CustomAIStartUpFood"))
				{
					CustomAIStartUpFood = ParseIntParameter(line, "CustomAIStartUpFood");
					CustomAIStartUpFood = Math.Max(CustomAIStartUpFood, 0);
					CustomAIStartUpFood = Math.Min(CustomAIStartUpFood, 100000);
				}
				
				if (line.StartsWith("RawResourceCount"))
				{
					RawResourceCount = ParseIntParameter(line, "RawResourceCount");
					RawResourceCount = Math.Max(RawResourceCount, 0);
					RawResourceCount = Math.Min(RawResourceCount, 100);
				}
				
				if (line.StartsWith("RawResourceNearby"))
				{
					RawResourceNearby = ParseBoolParameter(line, "RawResourceNearby");
				}
				
				if (line.StartsWith("MonsterType"))
				{
					MonsterType = ParseIntParameter(line, "MonsterType");
					if (MonsterType != 0 && MonsterType != 1 && MonsterType != 2)
						return "Incorrect MonsterType value";
				}
				
				if (line.StartsWith("MonsterLairCount"))
				{
					MonsterLairCount = ParseIntParameter(line, "MonsterLairCount");
					MonsterLairCount = Math.Max(MonsterLairCount, 0);
					MonsterLairCount = Math.Min(MonsterLairCount, 1000);
				}
				
				if (line.StartsWith("IndependentTownCount"))
				{
					IndependentTownCount = ParseIntParameter(line, "IndependentTownCount");
					IndependentTownCount = Math.Max(IndependentTownCount, 0);
					IndependentTownCount = Math.Min(IndependentTownCount, 1000);
				}
				
				if (line.StartsWith("IndependentTownResistance"))
				{
					IndependentTownResistance = ParseIntParameter(line, "IndependentTownResistance");
					if (IndependentTownResistance != 1 && IndependentTownResistance != 2 && IndependentTownResistance != 3)
						return "Incorrect IndependentTownResistance value";
				}
				
				if (line.StartsWith("NewIndependentTownEmerge"))
				{
					NewIndependentTownEmerge = ParseBoolParameter(line, "NewIndependentTownEmerge");
				}
				
				if (line.StartsWith("NewNationEmerge"))
				{
					NewNationEmerge = ParseBoolParameter(line, "NewNationEmerge");
				}
				
				if (line.StartsWith("RandomStartUp"))
				{
					RandomStartUp = ParseBoolParameter(line, "RandomStartUp");
				}
				
				if (line.StartsWith("ExploreWholeMap"))
				{
					ExploreWholeMap = ParseBoolParameter(line, "ExploreWholeMap");
				}
				
				if (line.StartsWith("FogOfWar"))
				{
					FogOfWar = ParseBoolParameter(line, "FogOfWar");
				}
				
				if (line.StartsWith("EarthquakeFrequency"))
				{
					EarthquakeFrequency = ParseIntParameter(line, "EarthquakeFrequency");
					if (EarthquakeFrequency != 0 && EarthquakeFrequency != 1 && EarthquakeFrequency != 2 && EarthquakeFrequency != 3)
						return "Incorrect EarthquakeFrequency value";
				}
				
				if (line.StartsWith("GoalDestroyMonsterFlag"))
				{
					GoalDestroyMonsterFlag = ParseBoolParameter(line, "GoalDestroyMonsterFlag");
				}
				
				if (line.StartsWith("GoalPopulationFlag"))
				{
					GoalPopulationFlag = ParseBoolParameter(line, "GoalPopulationFlag");
				}
				
				if (line.StartsWith("GoalEconomicScoreFlag"))
				{
					GoalEconomicScoreFlag = ParseBoolParameter(line, "GoalEconomicScoreFlag");
				}
				
				if (line.StartsWith("GoalTotalScoreFlag"))
				{
					GoalTotalScoreFlag = ParseBoolParameter(line, "GoalTotalScoreFlag");
				}
				
				if (line.StartsWith("GoalYearLimitFlag"))
				{
					GoalYearLimitFlag = ParseBoolParameter(line, "GoalYearLimitFlag");
				}
				
				if (line.StartsWith("GoalPopulation") && !line.StartsWith("GoalPopulationFlag"))
				{
					GoalPopulation = ParseIntParameter(line, "GoalPopulation");
					GoalPopulation = Math.Max(GoalPopulation, 100);
					GoalPopulation = Math.Min(GoalPopulation, 10000);
				}
				
				if (line.StartsWith("GoalEconomicScore") && !line.StartsWith("GoalEconomicScoreFlag"))
				{
					GoalEconomicScore = ParseIntParameter(line, "GoalEconomicScore");
					GoalEconomicScore = Math.Max(GoalEconomicScore, 100);
					GoalEconomicScore = Math.Min(GoalEconomicScore, 100000);
				}
				
				if (line.StartsWith("GoalTotalScore") && !line.StartsWith("GoalTotalScoreFlag"))
				{
					GoalTotalScore = ParseIntParameter(line, "GoalTotalScore");
					GoalTotalScore = Math.Max(GoalTotalScore, 100);
					GoalTotalScore = Math.Min(GoalTotalScore, 100000);
				}
				
				if (line.StartsWith("GoalYearLimit") && !line.StartsWith("GoalYearLimitFlag"))
				{
					GoalYearLimit = ParseIntParameter(line, "GoalYearLimit");
					GoalYearLimit = Math.Max(GoalYearLimit, 1);
					GoalYearLimit = Math.Min(GoalYearLimit, 1000);
				}
				
				if (line.StartsWith("GameScreenWidth"))
				{
					GameScreenWidth = ParseIntParameter(line, "GameScreenWidth");
					GameScreenWidth = Math.Max(GameScreenWidth, 20);
					GameScreenWidth = Math.Min(GameScreenWidth, 200);
				}
				
				if (line.StartsWith("GameScreenHeight"))
				{
					GameScreenHeight = ParseIntParameter(line, "GameScreenHeight");
					GameScreenHeight = Math.Max(GameScreenHeight, 18);
					GameScreenHeight = Math.Min(GameScreenHeight, 100);
				}
				
				if (line.StartsWith("ShowUnitPath"))
				{
					ShowUnitPath = ParseIntParameter(line, "ShowUnitPath");
					if (ShowUnitPath != 0 && ShowUnitPath != 1 && ShowUnitPath != 2 && ShowUnitPath != 3)
						return "Incorrect ShowUnitPath value";
				}
				
				if (line.StartsWith("DisplayNewsType"))
				{
					DisplayNewsType = ParseIntParameter(line, "DisplayNewsType");
					if (DisplayNewsType != 0 && DisplayNewsType != 1)
						return "Incorrect DisplayNewsType value";
				}
				
				if (line.StartsWith("UnlimitedRawResource"))
				{
					UnlimitedRawResource = ParseBoolParameter(line, "UnlimitedRawResource");
				}
				
				if (line.StartsWith("FastBuild"))
				{
					FastBuild = ParseBoolParameter(line, "FastBuild");
				}
				
				if (line.StartsWith("ShowAIInfo"))
				{
					ShowAIInfo = ParseBoolParameter(line, "ShowAIInfo");
				}
			}
		}
		catch (Exception e)
		{
			return e.Message;
		}

		return String.Empty;
	}

	private string ParseParameter(string line, string parameter)
	{
		if (line.Trim().StartsWith(parameter))
		{
			int equalsIndex = line.IndexOf("=", StringComparison.InvariantCulture);
			int commentIndex = line.IndexOf("//", StringComparison.InvariantCulture);
			if (equalsIndex >= 0)
			{
				if (commentIndex >= 0)
					return line.Substring(equalsIndex + 1, commentIndex - equalsIndex - 1).Trim();
				else
					return line.Substring(equalsIndex + 1).Trim();
			}
		}

		return String.Empty;
	}

	private int ParseIntParameter(string line, string parameter)
	{
		string parameterValue = ParseParameter(line, parameter);
		if (String.IsNullOrEmpty(parameterValue))
			throw new Exception("Cannot parse value of " + parameter);
		
		try
		{
			return Int32.Parse(parameterValue);
		}
		catch (Exception e)
		{
			throw new Exception(e.Message + " when parsing parameter " + parameter);
		}
	}
	
	private string ParseStringParameter(string line, string parameter)
	{
		string parameterValue = ParseParameter(line, parameter);
		if (String.IsNullOrEmpty(parameterValue))
			throw new Exception("Cannot parse value of " + parameter);
		return parameterValue;
	}

	private bool ParseBoolParameter(string line, string parameter)
	{
		string parameterValue = ParseParameter(line, parameter);
		if (String.IsNullOrEmpty(parameterValue))
			throw new Exception("Cannot parse value of " + parameter);
		
		try
		{
			return Boolean.Parse(parameterValue);
		}
		catch (Exception e)
		{
			throw new Exception(e.Message + " when parsing parameter " + parameter);
		}
	}
}
