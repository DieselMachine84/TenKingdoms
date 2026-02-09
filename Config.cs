using System;

namespace TenKingdoms;

public class Config
{
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
}
