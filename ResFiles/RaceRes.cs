using System;
using System.Linq;

namespace TenKingdoms;

//------------ Define race id. -------------//

public enum Race
{
	RACE_NORMAN = 1,
	RACE_MAYA,
	RACE_GREEK,
	RACE_VIKING,
	RACE_PERSIAN,
	RACE_CHINESE,
	RACE_JAPANESE,
	RACE_EGYPTIAN,
	RACE_INDIAN,
	RACE_ZULU
}

//------------ Define struct RaceRec ---------------//

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

	public RaceRec(byte[] data)
	{
		int dataIndex = 0;
		for (int i = 0; i < code.Length; i++, dataIndex++)
			code[i] = Convert.ToChar(data[dataIndex]);
		
		for (int i = 0; i < name.Length; i++, dataIndex++)
			name[i] = Convert.ToChar(data[dataIndex]);
		
		for (int i = 0; i < adjective.Length; i++, dataIndex++)
			adjective[i] = Convert.ToChar(data[dataIndex]);
		
		for (int i = 0; i < icon_file_name.Length; i++, dataIndex++)
			icon_file_name[i] = Convert.ToChar(data[dataIndex]);
		
		for (int i = 0; i < icon_bitmap_ptr.Length; i++, dataIndex++)
			icon_bitmap_ptr[i] = data[dataIndex];
	}
}

public class RaceInfo
{
	public int race_id;

	public string code;
	public string name;
	public string adjective;

	public byte[] iconBitmap;
	public int iconBitmapWidth;
	public int iconBitmapHeight;

	public byte[] scrollBitmap;
	public int scrollBitmapWidth;
	public int scrollBitmapHeight;
	private IntPtr _scrollTexture;

	public IntPtr GetScrollTexture(Graphics graphics)
	{
		if (_scrollTexture == default)
			_scrollTexture = graphics.CreateTextureFromBmp(scrollBitmap, scrollBitmapWidth, scrollBitmapHeight);

		return _scrollTexture;
	}
	
	//----------------------//

	public int first_first_name_id; // first <first name> of this race in first_name_array[]
	public int first_last_name_id; // first <last name> of this race in last_name_array[]

	public int first_name_count;
	public int last_name_count;

	public int first_town_name_recno;
	public int town_name_count;

	public int basic_unit_id;

	//--------- game vars ----------//

	public int town_name_used_count;
	
	public RaceRes RaceRes { get; }

	public RaceInfo(RaceRes raceRes)
	{
		RaceRes = raceRes;
	}

	public string get_name(int nameId, int nameType = 0)
	{
		//TODO rewrite why static?
		//static String str;

		if (nameId == 0)
			return "";

		string str = string.Empty;
		if (nameType != 2) // 2-is for last name only
		{
			int firstNameId = (nameId >> 8);

			int nameRecno = first_first_name_id + firstNameId - 1;

			str = new string(RaceRes.name_array[nameRecno - 1].name);

			if (nameType == 1) // first name only
				return str;
		}
		else
		{
			if (last_name_count == 0) // if this race does not have last name
				return "";

			str = "";
		}

		//--------- last name ----------//

		int lastNameId = (nameId & 0xFF);

		if (last_name_count == 0) // if there is no last name for this race
		{
			if (lastNameId > 1) // no need to display roman letter "I" for the first one
			{
				if (str.Length > 0)
					str += " ";

				str += Misc.roman_number(lastNameId);
			}
		}
		else
		{
			int nameRecno = first_last_name_id + lastNameId - 1;

			if (str.Length > 0)
				str += " ";

			str += RaceRes.name_array[nameRecno - 1].name;
		}

		return str;
	}

	public string get_single_name(int nameId)
	{
		switch (race_id)
		{
			case (int)Race.RACE_NORMAN:
			case (int)Race.RACE_VIKING:
			case (int)Race.RACE_INDIAN:
			case (int)Race.RACE_ZULU:
				return get_name(nameId, 1); // 1-first name only

			case (int)Race.RACE_CHINESE:
			case (int)Race.RACE_JAPANESE:
				return get_name(nameId, 2); // 2-last name only

			default:
				return get_name(nameId); // the whole name
		}
	}

	public int get_new_name_id()
	{
		//---------- get the first name ----------//

		int firstNameId = Misc.Random(first_name_count) + 1;

		for (int i = 1; i <= first_name_count; i++)
		{
			if (++firstNameId > first_name_count)
				firstNameId = 1;

			//--- try to get an unused first name -----//
			//--- if all names have been used (when i>first_name_count), use the first selected random name id. --//

			if (RaceRes.name_used_array[first_first_name_id + firstNameId - 2] == 0)
				break;
		}

		int nameRecno = first_first_name_id + firstNameId - 1;

		RaceRes.name_used_array[nameRecno - 1]++;

		//---------- get the last name ----------//

		int lastNameId;

		if (last_name_count == 0) // if there is no last name for this race, add Roman letter as the last name
		{
			lastNameId = RaceRes.name_used_array[first_first_name_id + firstNameId - 2];
		}
		else // this race has last names
		{
			lastNameId = Misc.Random(last_name_count) + 1;

			for (int i = 1; i <= last_name_count; i++)
			{
				if (++lastNameId > last_name_count)
					lastNameId = 1;

				//--- try to get an unused last name -----//
				//--- if all names have been used, use the first selected random name id. --//

				if (RaceRes.name_used_array[first_last_name_id + lastNameId - 2] == 0)
					break;
			}

			nameRecno = first_last_name_id + lastNameId - 1;

			RaceRes.name_used_array[nameRecno - 1]++;
		}

		//--- nameId is a combination of first & last name id. ----//

		return (firstNameId << 8) + lastNameId;
	}

	public void free_name_id(int nameId)
	{
		int firstNameId = (nameId >> 8);
		int nameRecno = first_first_name_id + firstNameId - 1;

		RaceRes.name_used_array[nameRecno - 1]--;

		if (last_name_count > 0) // some races do not have last names
		{
			int lastNameId = (nameId & 0xFF);
			nameRecno = first_last_name_id + lastNameId - 1;

			RaceRes.name_used_array[nameRecno - 1]--;
		}
	}

	public void use_name_id(int nameId)
	{
		int firstNameId = (nameId >> 8);
		int nameRecno = first_first_name_id + firstNameId - 1;

		RaceRes.name_used_array[nameRecno - 1]++;

		if (last_name_count > 0) // some races do not have last names
		{
			int lastNameId = (nameId & 0xFF);
			nameRecno = first_last_name_id + lastNameId - 1;

			RaceRes.name_used_array[nameRecno - 1]++;
		}
	}
}

//-------- Define struct NameRec ----------//

public class RaceNameRec
{
	public const int NAME_LEN = 20;
	public char[] name = new char[NAME_LEN + 1];

	public RaceNameRec(byte[] data)
	{
		int dataIndex = 0;
		for (int i = 0; i < name.Length; i++, dataIndex++)
			name[i] = Convert.ToChar(data[dataIndex]);
	}
}

//-------- Define struct NameInfo ----------//

public class RaceName
{
	public string name;
}

//----------- Define class RaceRes ---------------//

public class RaceRes
{
	public const int RACE_ICON_WIDTH = 24;
	public const int RACE_ICON_HEIGHT = 20;
	public const string RACE_DB = "RACE";
	public const string RACE_NAME_DB = "RACENAME";

	public RaceInfo[] race_info_array;
	public RaceName[] name_array;
	public byte[] name_used_array;

	private ResourceDb res_bitmap;
	private ResourceIdx _scrollResources;

	public GameSet GameSet { get; }
	public UnitRes UnitRes { get; }

	public RaceRes(GameSet gameSet, UnitRes unitRes)
	{
		GameSet = gameSet;
		UnitRes = unitRes;

		res_bitmap = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_RACE.RES");
		_scrollResources = new ResourceIdx($"{Sys.GameDataFolder}/Resource/I_SPICT.RES");

		//------- load database information --------//

		LoadRaceInfo();
		LoadName();
	}

	//TODO rewrite
	//public int write_file(File*);
	//public int read_file(File*);

	public bool is_same_race(int raceId1, int raceId2)
	{
		return raceId1 == raceId2;
	}

	public RaceInfo this[int raceId] => race_info_array[raceId - 1];

	private void LoadRaceInfo()
	{
		Database dbRace = GameSet.OpenDb(RACE_DB);
		race_info_array = new RaceInfo[dbRace.RecordCount];

		for (int i = 0; i < race_info_array.Length; i++)
		{
			RaceRec raceRec = new RaceRec(dbRace.Read(i + 1));
			RaceInfo raceInfo = new RaceInfo(this);
			race_info_array[i] = raceInfo;
			raceInfo.race_id = i + 1;

			//misc.rtrim_fld( raceInfo->code, raceRec->code, raceRec->CODE_LEN );
			raceInfo.code = Misc.ToString(raceRec.code);

			//misc.rtrim_fld( raceInfo->name, raceRec->name, raceRec->NAME_LEN );
			raceInfo.name = Misc.ToString(raceRec.name);

			//misc.rtrim_fld( raceInfo->adjective, raceRec->adjective, raceRec->ADJECTIVE_LEN );
			raceInfo.adjective = Misc.ToString(raceRec.adjective);

			int bitmapOffset = BitConverter.ToInt32(raceRec.icon_bitmap_ptr, 0);
			byte[] iconBitmapData = res_bitmap.Read(bitmapOffset);
			raceInfo.iconBitmapWidth = BitConverter.ToInt16(iconBitmapData, 0);
			raceInfo.iconBitmapHeight = BitConverter.ToInt16(iconBitmapData, 2);
			raceInfo.iconBitmap = iconBitmapData.Skip(4).ToArray();

			byte[] scrollData = _scrollResources.Read("SCROLL-" + raceInfo.code[0]);
			raceInfo.scrollBitmapWidth = BitConverter.ToInt16(scrollData, 0);
			raceInfo.scrollBitmapHeight = BitConverter.ToInt16(scrollData, 2);
			raceInfo.scrollBitmap = scrollData.Skip(4).ToArray();

			for (int unitId = 1; unitId <= UnitConstants.MAX_UNIT_TYPE; unitId++)
			{
				if (UnitRes[unitId].race_id == i + 1)
				{
					raceInfo.basic_unit_id = unitId;
					break;
				}
			}
		}
	}

	private void LoadName()
	{
		// cannot be more than 255 in each name group, as Unit::name_id is an <int> and half of it is for the first name and another half of it is for the last name
		const int MAX_SINGLE_RACE_NAME = 255;

		Database dbRaceName = GameSet.OpenDb(RACE_NAME_DB);

		name_array = new RaceName[dbRaceName.RecordCount];
		name_used_array = new byte[name_array.Length];

		//------ read in RaceName info array -------//

		int raceId = 0;
		bool isFirstName = false;
		int i = 1;

		for (i = 1; i <= name_array.Length; i++)
		{
			RaceNameRec raceNameRec = new RaceNameRec(dbRaceName.Read(i));
			RaceName raceName = new RaceName();
			name_array[i - 1] = raceName;

			//misc.rtrim_fld( raceName->name, raceNameRec->name, raceNameRec->NAME_LEN );
			raceName.name = Misc.ToString(raceNameRec.name);

			//TODO Rewrite
			// The default STD.SET uses a different code page than the used fonts.
			//misc.dos_encoding_to_win(raceName.name, raceName.NAME_LEN);

			if (raceName.name[0] == '@')
			{
				if (raceId != 0)
				{
					if (isFirstName)
						this[raceId].first_name_count = i - this[raceId].first_first_name_id;
					else
						this[raceId].last_name_count = i - this[raceId].first_last_name_id;
				}

				//----- get the race id. of the following names -----//

				for (int j = 1; j <= GameConstants.MAX_RACE; j++)
				{
					if (this[j].code == raceName.name.Substring(2))
					{
						raceId = j;
						break;
					}
				}

				//----------------------------------------------//

				isFirstName = (raceName.name[1] == '1'); // whether the following names are first names

				if (isFirstName)
					this[raceId].first_first_name_id = i + 1; // next record following the "@RACECODE" is the first name record
				else
					this[raceId].first_last_name_id = i + 1; // next record following the "@RACECODE" is the first name record
			}
		}

		//------- finish up the last race in the list ------//

		if (raceId != 0)
		{
			if (isFirstName)
				this[raceId].first_name_count = i - this[raceId].first_first_name_id;
			else
				this[raceId].last_name_count = i - this[raceId].first_last_name_id;
		}

	}
}
