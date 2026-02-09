using System;
using System.Linq;

namespace TenKingdoms;

public class RaceRec
{
	public const int CODE_LEN = 8;
	public const int NAME_LEN = 12;
	public const int ADJECTIVE_LEN = 12;
	public const int FILE_NAME_LEN = 8;
	public const int BITMAP_PTR_LEN = 4;

	public char[] code = new char[CODE_LEN];
	public char[] name = new char[NAME_LEN];
	public char[] adjective = new char[ADJECTIVE_LEN];

	public char[] icon_file_name = new char[FILE_NAME_LEN];
	public byte[] icon_bitmap_ptr = new byte[BITMAP_PTR_LEN];

	public RaceRec(Database db, int recNo)
	{
		int dataIndex = 0;
		for (int i = 0; i < code.Length; i++, dataIndex++)
			code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
		
		for (int i = 0; i < name.Length; i++, dataIndex++)
			name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
		
		for (int i = 0; i < adjective.Length; i++, dataIndex++)
			adjective[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
		
		for (int i = 0; i < icon_file_name.Length; i++, dataIndex++)
			icon_file_name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
		
		for (int i = 0; i < icon_bitmap_ptr.Length; i++, dataIndex++)
			icon_bitmap_ptr[i] = db.ReadByte(recNo, dataIndex);
	}
}

public class RaceInfo
{
	public int RaceId { get; set; }

	public string Code { get; set; }
	public string Name { get; set; }
	public string Adjective { get; set; }

	public int FirstFirstNameId { get; set; } // first <first name> of this race in first_name_array[]
	public int FirstLastNameId { get; set; } // first <last name> of this race in last_name_array[]

	public int FirstNameCount { get; set; }
	public int LastNameCount { get; set; }

	public int FirstTownNameId { get; set; }
	public int TownNameCount { get; set; }
	public int TownNameUsedCount { get; set; }

	public int BasicUnitType { get; set; }

	public byte[] IconBitmap { get; set; }
	public int IconBitmapWidth { get; set; }
	public int IconBitmapHeight { get; set; }
	private IntPtr _iconTexture;

	public byte[] ScrollBitmap { get; set; }
	public int ScrollBitmapWidth { get; set; }
	public int ScrollBitmapHeight { get; set; }
	private IntPtr _scrollTexture;

	public IntPtr GetIconTexture(Graphics graphics)
	{
		if (_iconTexture == default)
			_iconTexture = graphics.CreateTextureFromBmp(IconBitmap, IconBitmapWidth, IconBitmapHeight);

		return _iconTexture;
	}

	public IntPtr GetScrollTexture(Graphics graphics)
	{
		if (_scrollTexture == default)
			_scrollTexture = graphics.CreateTextureFromBmp(ScrollBitmap, ScrollBitmapWidth, ScrollBitmapHeight);

		return _scrollTexture;
	}
	
	public RaceRes RaceRes { get; }

	public RaceInfo(RaceRes raceRes)
	{
		RaceRes = raceRes;
	}

	public string GetName(int nameId, int nameType = 0)
	{
		if (nameId == 0)
			return String.Empty;

		string result;
		if (nameType != 2) // 2 - is for last name only
		{
			int firstNameId = (nameId >> 8);

			result = RaceRes.Names[FirstFirstNameId + firstNameId - 2].Name;

			if (nameType == 1) // first name only
				return result;
		}
		else
		{
			if (LastNameCount == 0) // if this race does not have last name
				return String.Empty;

			result = String.Empty;
		}

		int lastNameId = (nameId & 0xFF);

		if (LastNameCount == 0) // if there is no last name for this race
		{
			if (lastNameId > 1) // no need to display roman letter "I" for the first one
			{
				if (result.Length > 0)
					result += " ";

				result += Misc.RomanNumber(lastNameId);
			}
		}
		else
		{
			if (result.Length > 0)
				result += " ";

			result += RaceRes.Names[FirstLastNameId + lastNameId - 2].Name;
		}

		return result;
	}

	public string GetSingleName(int nameId)
	{
		switch (RaceId)
		{
			case RaceRes.RACE_NORMAN:
			case RaceRes.RACE_VIKING:
			case RaceRes.RACE_INDIAN:
			case RaceRes.RACE_ZULU:
				return GetName(nameId, 1); // 1 - first name only

			case RaceRes.RACE_CHINESE:
			case RaceRes.RACE_JAPANESE:
				return GetName(nameId, 2); // 2 - last name only

			default:
				return GetName(nameId); // the whole name
		}
	}

	public int GetNewNameId()
	{
		int firstNameId = Misc.Random(FirstNameCount) + 1;

		for (int i = 1; i <= FirstNameCount; i++)
		{
			if (++firstNameId > FirstNameCount)
				firstNameId = 1;

			//--- try to get an unused first name -----//
			//--- if all names have been used (when i > FirstNameCount), use the first selected random name id. --//

			if (RaceRes.NamesUsed[FirstFirstNameId + firstNameId - 2] == 0)
				break;
		}

		RaceRes.NamesUsed[FirstFirstNameId + firstNameId - 2]++;

		int lastNameId;

		if (LastNameCount == 0) // if there is no last name for this race, add Roman letter as the last name
		{
			lastNameId = RaceRes.NamesUsed[FirstFirstNameId + firstNameId - 2];
		}
		else // this race has last names
		{
			lastNameId = Misc.Random(LastNameCount) + 1;

			for (int i = 1; i <= LastNameCount; i++)
			{
				if (++lastNameId > LastNameCount)
					lastNameId = 1;

				//--- try to get an unused last name -----//
				//--- if all names have been used, use the first selected random name id. --//

				if (RaceRes.NamesUsed[FirstLastNameId + lastNameId - 2] == 0)
					break;
			}

			RaceRes.NamesUsed[FirstLastNameId + lastNameId - 2]++;
		}

		//--- nameId is a combination of first & last name id. ----//

		return (firstNameId << 8) + lastNameId;
	}

	public void UseNameId(int nameId)
	{
		int firstNameId = (nameId >> 8);

		RaceRes.NamesUsed[FirstFirstNameId + firstNameId - 2]++;

		if (LastNameCount > 0) // some races do not have last names
		{
			int lastNameId = (nameId & 0xFF);

			RaceRes.NamesUsed[FirstLastNameId + lastNameId - 2]++;
		}
	}
	
	//TODO check that name is not feed twice when killing a spy inside a firm
	public void FreeNameId(int nameId)
	{
		int firstNameId = (nameId >> 8);
		
		RaceRes.NamesUsed[FirstFirstNameId + firstNameId - 2]--;

		if (LastNameCount > 0) // some races do not have last names
		{
			int lastNameId = (nameId & 0xFF);

			RaceRes.NamesUsed[FirstLastNameId + lastNameId - 2]--;
		}
	}
}

public class RaceNameRec
{
	public const int NAME_LEN = 20;
	public char[] name = new char[NAME_LEN + 1];

	public RaceNameRec(Database db, int recNo)
	{
		int dataIndex = 0;
		for (int i = 0; i < name.Length; i++, dataIndex++)
			name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
	}
}

public class RaceName
{
	public string Name { get; set; }
}

public class RaceRes
{
	public const int RACE_NORMAN = 1;
	public const int RACE_MAYA = 2;
	public const int RACE_GREEK = 3;
	public const int RACE_VIKING = 4;
	public const int RACE_PERSIAN = 5;
	public const int RACE_CHINESE = 6;
	public const int RACE_JAPANESE = 7;
	public const int RACE_EGYPTIAN = 8;
	public const int RACE_INDIAN = 9;
	public const int RACE_ZULU = 10;
	
	public RaceInfo[] RaceInfos { get; private set; }
	public RaceName[] Names { get; private set; }
	public byte[] NamesUsed { get; private set; }

	public GameSet GameSet { get; }
	public UnitRes UnitRes { get; }

	public RaceRes(GameSet gameSet, UnitRes unitRes)
	{
		GameSet = gameSet;
		UnitRes = unitRes;

		LoadRaceInfo();
		LoadNames();
	}

	public RaceInfo this[int raceId] => RaceInfos[raceId - 1];

	private void LoadRaceInfo()
	{
		ResourceDb raceImages = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_RACE.RES");
		ResourceIdx scrollImages = new ResourceIdx($"{Sys.GameDataFolder}/Resource/I_SPICT.RES");
		
		Database dbRace = GameSet.OpenDb("RACE");
		RaceInfos = new RaceInfo[dbRace.RecordCount];

		for (int i = 0; i < RaceInfos.Length; i++)
		{
			RaceRec raceRec = new RaceRec(dbRace, i + 1);
			RaceInfo raceInfo = new RaceInfo(this);
			RaceInfos[i] = raceInfo;
			raceInfo.RaceId = i + 1;

			raceInfo.Code = Misc.ToString(raceRec.code);
			raceInfo.Name = Misc.ToString(raceRec.name);
			raceInfo.Adjective = Misc.ToString(raceRec.adjective);

			int bitmapOffset = BitConverter.ToInt32(raceRec.icon_bitmap_ptr, 0);
			byte[] iconBitmapData = raceImages.Read(bitmapOffset);
			raceInfo.IconBitmapWidth = BitConverter.ToInt16(iconBitmapData, 0);
			raceInfo.IconBitmapHeight = BitConverter.ToInt16(iconBitmapData, 2);
			raceInfo.IconBitmap = iconBitmapData.Skip(4).ToArray();

			byte[] scrollData = scrollImages.Read("SCROLL-" + raceInfo.Code[0]);
			raceInfo.ScrollBitmapWidth = BitConverter.ToInt16(scrollData, 0);
			raceInfo.ScrollBitmapHeight = BitConverter.ToInt16(scrollData, 2);
			raceInfo.ScrollBitmap = scrollData.Skip(4).ToArray();

			for (int unitType = 1; unitType <= UnitConstants.MAX_UNIT_TYPE; unitType++)
			{
				if (UnitRes[unitType].RaceId == i + 1)
				{
					raceInfo.BasicUnitType = unitType;
					break;
				}
			}
		}
	}

	private void LoadNames()
	{
		Database dbRaceName = GameSet.OpenDb("RACENAME");

		Names = new RaceName[dbRaceName.RecordCount];
		NamesUsed = new byte[Names.Length];

		int raceId = 0;
		bool isFirstName = false;
		int i;

		for (i = 1; i <= Names.Length; i++)
		{
			RaceNameRec raceNameRec = new RaceNameRec(dbRaceName, i);
			RaceName raceName = new RaceName();
			Names[i - 1] = raceName;

			raceName.Name = Misc.ToString(raceNameRec.name);

			//TODO Rewrite
			// The default STD.SET uses a different code page than the used fonts.
			//misc.dos_encoding_to_win(raceName.name, raceName.NAME_LEN);

			if (raceName.Name[0] == '@')
			{
				if (raceId != 0)
				{
					if (isFirstName)
						this[raceId].FirstNameCount = i - this[raceId].FirstFirstNameId;
					else
						this[raceId].LastNameCount = i - this[raceId].FirstLastNameId;
				}

				//----- get the race id. of the following names -----//

				for (int j = 1; j <= GameConstants.MAX_RACE; j++)
				{
					if (this[j].Code == raceName.Name.Substring(2))
					{
						raceId = j;
						break;
					}
				}

				//----------------------------------------------//

				isFirstName = (raceName.Name[1] == '1'); // whether the following names are first names

				if (isFirstName)
					this[raceId].FirstFirstNameId = i + 1; // next record following the "@RACECODE" is the first name record
				else
					this[raceId].FirstLastNameId = i + 1; // next record following the "@RACECODE" is the first name record
			}
		}

		//------- finish up the last race in the list ------//

		if (raceId != 0)
		{
			if (isFirstName)
				this[raceId].FirstNameCount = i - this[raceId].FirstFirstNameId;
			else
				this[raceId].LastNameCount = i - this[raceId].FirstLastNameId;
		}
	}
}
