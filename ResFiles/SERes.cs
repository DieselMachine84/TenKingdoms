using System;

namespace TenKingdoms;

public class SERec
{
    public const int RECNO_LEN = 3;
    public const int CODE_LEN = 12;
    public const int VERB_LEN = 4;
    public const int OUT_FRAME_LEN = 3;
    public const int FILE_NAME_LEN = 8;

    public char subject_type;
    public char[] subject_code = new char[CODE_LEN];
    public char[] subject_id = new char[RECNO_LEN];
    public char[] action = new char[VERB_LEN];
    public char object_type;
    public char[] object_id = new char[RECNO_LEN];
    public char[] out_frame = new char[OUT_FRAME_LEN];
    public char[] file_name = new char[FILE_NAME_LEN];

    public SERec(Database db, int recNo)
    {
        int dataIndex = 0;
        subject_type = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        dataIndex++;

        for (int i = 0; i < subject_code.Length; i++, dataIndex++)
            subject_code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < subject_id.Length; i++, dataIndex++)
            subject_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < action.Length; i++, dataIndex++)
            action[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        object_type = Convert.ToChar(db.ReadByte(recNo, dataIndex));
        dataIndex++;

        for (int i = 0; i < object_id.Length; i++, dataIndex++)
            object_id[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < out_frame.Length; i++, dataIndex++)
            out_frame[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < file_name.Length; i++, dataIndex++)
            file_name[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
    }
}

public class SEInfo
{
	public char subject_type; // S=sprite, U=unit, R=race, F=firm, T=town
	public int subject_id;
	public string action;
	public int object_type;
	public int object_id;
	public int out_frame;
	public string file_name;
	public int effect_id; // id returned from se_ctrl.scan

	public bool match(char subjectType, int subjectId, string act, int objectType, int objectId)
	{
		// object_type == -1, match all object type
		// object_id == -1, match all object id of that type
		return subject_type == subjectType &&
		       (subject_id == -1 || subject_id == subjectId) &&
		       (action == act) &&
		       (object_type == -1 || (object_type == objectType && (object_id == -1 || object_id == objectId)));
	}
}

public class SEInfoIndex
{
    public char subject_type;
    public char dummy;
    public int subject_id;
    public int start_rec;
    public int end_rec;
}

public class SETypeIndex
{
    public char subject_type;
    public char dummy;
    public int start_rec;
    public int end_rec;
}

public class SERes
{
	public const string SERES_DB = "SOUNDRES";

	public SECtrl se_output;
	public SEInfo[] se_array;
	public SEInfoIndex[] se_index_array;
	public SETypeIndex[] type_index_array;

	public static long last_select_time;
	public static long last_command_time;
	public static long select_sound_length = 600;
	public uint seed;

	public GameSet GameSet { get; }
	protected Config Config => Sys.Instance.Config;
	protected World World => Sys.Instance.World;

	public SERes(GameSet gameSet)
	{
		GameSet = gameSet;
	}

	public void init1() // init before se_ctrl.init
	{
		seed = (uint)Misc.GetTime();
		LoadInfo();
		SortInfo();
		BuildIndex();
	}

	public void init2(SECtrl seCtrl) // init after se_ctrl.init
	{
		se_output = seCtrl;
		foreach (SEInfo seInfo in se_array)
		{
			seInfo.effect_id = seCtrl.search_effect_id(seInfo.file_name);
		}
	}

	public SEInfo scan(char subjectType, int subjectId, string act, int objectType, int objectId,
		bool findFirst = false)
	{
		int startRec = 0;
		int endRec = 0;

		// ---------- search the type_index_array ---------//
		bool foundFlag = false;
		foreach (SETypeIndex typeIndex in type_index_array)
		{
			if (subjectType == typeIndex.subject_type)
			{
				startRec = typeIndex.start_rec;
				endRec = typeIndex.end_rec;
				foundFlag = true;
				break;
			}
		}

		if (!foundFlag)
			return null;

		// ---------- search the se_index_array ---------//
		foundFlag = false;
		int i;
		for (i = startRec; i <= endRec; ++i)
		{
			SEInfoIndex seIndex = se_index_array[i];
			if (subjectId == seIndex.subject_id && subjectType == seIndex.subject_type)
			{
				startRec = seIndex.start_rec;
				endRec = seIndex.end_rec;
				foundFlag = true;
				break;
			}
		}

		if (!foundFlag) // not found
			return null;

		// ----------- search the se_array ----------//
		foundFlag = false;
		SEInfo seInfo = null;
		for (i = startRec; i <= endRec; ++i)
		{
			seInfo = se_array[i];
			if (seInfo.match(subjectType, subjectId, act, objectType, objectId))
			{
				foundFlag = true;
				break;
			}
		}

		if (foundFlag) // found
		{
			if (findFirst)
				return seInfo;
			int found = 1;
			SEInfo retSEInfo = seInfo;
			for (++i, seInfo = se_array[i];
			     i <= endRec && found <= 4 && seInfo.match(subjectType, subjectId, act, objectType, objectId);
			     ++i, seInfo = se_array[i])
			{
				found++;
				if (random(found + 1) == 0)
					retSEInfo = seInfo;
			}

			return retSEInfo;
		}

		return null;
	}

	public int scan_id(char subjectType, int subjectId, string act, int objectType, int objectId,
		bool findFirst = false)
	{
		SEInfo seInfo = scan(subjectType, subjectId, act, objectType, objectId, findFirst);
		if (seInfo != null)
			return seInfo.effect_id;
		return 0;
	}

	public SEInfo this[int i] => se_array[i - 1];

	public void sound(int xLoc, int yLoc, int frame, char subjectType, int subjectId,
		string action, int objectType = 0, int objectId = 0)
	{
		if (!Config.sound_effect_flag)
			return;

		//TODO rewrite
		//int relXLoc = xLoc - (World.zoom_matrix->top_x_loc + World.zoom_matrix->disp_x_loc / 2);
		//int relYLoc = yLoc - (World.zoom_matrix->top_y_loc + World.zoom_matrix->disp_y_loc / 2);
		int relXLoc = xLoc;
		int relYLoc = yLoc;
		PosVolume posVolume = new PosVolume(relXLoc, relYLoc);
		RelVolume relVolume = new RelVolume(posVolume);
		if (!Config.pan_control)
			relVolume.ds_pan = 0;

		if (relVolume.rel_vol < 5)
			return;

		SEInfo seInfo = scan(subjectType, subjectId, action, objectType, objectId);
		if (seInfo != null && frame == seInfo.out_frame)
		{
			se_output.request(seInfo.effect_id, relVolume);
		}
	}

	public void far_sound(int xLoc, int yLoc, int frame, char subjectType, int subjectId,
		string action, int objectType = 0, int objectId = 0)
	{
		if (!Config.sound_effect_flag)
			return;

		//TODO rewrite
		//int relXLoc = xLoc - (World.zoom_matrix->top_x_loc + World.zoom_matrix->disp_x_loc / 2);
		//int relYLoc = yLoc - (World.zoom_matrix->top_y_loc + World.zoom_matrix->disp_y_loc / 2);
		int relXLoc = xLoc;
		int relYLoc = yLoc;

		PosVolume posVolume = new PosVolume(relXLoc, relYLoc);
		RelVolume relVolume = new RelVolume(posVolume, 200, GameConstants.MapSize);
		if (!Config.pan_control)
			relVolume.ds_pan = 0;

		if (relVolume.rel_vol < 80)
			relVolume.rel_vol = 80;

		SEInfo seInfo = scan(subjectType, subjectId, action, objectType, objectId);
		if (seInfo != null && frame == seInfo.out_frame)
		{
			se_output.request(seInfo.effect_id, relVolume);
		}
	}

	public static int mark_select_object_time() // return false if this sound should be skipped due to too frequent
	{
		int t = Misc.GetTime();
		if (t - last_select_time >= select_sound_length)
		{
			last_select_time = t;
			return 1;
		}

		return 0;
	}

	public static int mark_command_time() // return false if this sound should be skipped due to too frequent
	{
		int t = Misc.GetTime();
		//	if( t - last_command_time >= select_sound_length )
		//	{
		//		last_command_time = t;
		//		return 1;
		//	}
		if (t - last_select_time >= select_sound_length)
		{
			last_select_time = t;
			return 1;
		}

		return 0;
	}

	private void LoadInfo()
	{
		Database dbSE = GameSet.OpenDb(SERES_DB);

		se_array = new SEInfo[dbSE.RecordCount];

		for (int i = 0; i < se_array.Length; i++)
		{
			SERec seRec = new SERec(dbSE, i + 1);
			SEInfo seInfo = new SEInfo();
			se_array[i] = seInfo;

			// ------- copy subject ---------//
			seInfo.subject_type = seRec.subject_type;
			seInfo.subject_id = Misc.ToInt32(seRec.subject_id);

			// -------- copy verb ---------//
			seInfo.action = Misc.ToString(seRec.action);

			// --------- copy object ---------//
			if (seRec.object_type == ' ' || seRec.object_type == '\0')
			{
				seInfo.object_type = 0;
				seInfo.object_id = 0;
			}
			else if (seRec.object_type == '*')
			{
				seInfo.object_type = -1; // all object
				seInfo.object_id = -1;
			}
			else
			{
				seInfo.object_type = Convert.ToInt32(seRec.object_type);
				if (seRec.object_id[0] != '*')
					seInfo.object_id = Misc.ToInt32(seRec.object_id);
				else
					seInfo.object_id = -1; // all of the objectType
			}

			// -------- copy out frame ---------//
			seInfo.out_frame = Misc.ToInt32(seRec.out_frame);

			// -------- copy file name --------//
			seInfo.file_name = Misc.ToString(seRec.file_name);
			seInfo.effect_id = 0;
		}
	}

	private void SortInfo()
	{
		//qsort(se_array, se_array_count, sizeof(SEInfo), seinfo_cmp);
	}

	private void BuildIndex()
	{
		// ---------- first pass, count the size of index ---------//

		int lastType = -1;
		int lastId = -1;

		int type_index_count = 0;
		int se_index_count = 0;

		for (int i = 0; i < se_array.Length; ++i)
		{
			SEInfo seInfo = se_array[i];
			if (lastType != seInfo.subject_type)
			{
				type_index_count++;
				se_index_count++;
				lastType = seInfo.subject_type;
				lastId = seInfo.subject_id;
			}
			else if (lastId != seInfo.subject_id)
			{
				se_index_count++;
				lastId = seInfo.subject_id;
			}
		}

		// --------- allocate memory for index ----------//

		se_index_array = new SEInfoIndex[se_index_count];
		for (int i = 0; i < se_index_array.Length; i++)
		{
			se_index_array[i] = new SEInfoIndex();
		}

		type_index_array = new SETypeIndex[type_index_count];
		for (int i = 0; i < type_index_array.Length; i++)
		{
			type_index_array[i] = new SETypeIndex();
		}

		// ---------- pass 2, build indices -----------//
		int seIndex = -1;
		int typeIndex = -1;
		lastType = -1;
		for (int i = 0; i < se_array.Length; ++i)
		{
			SEInfo seInfo = se_array[i];
			if (lastType != seInfo.subject_type)
			{
				// ----------- new (type,Id) ---------//
				seIndex++;
				se_index_array[seIndex].subject_type = seInfo.subject_type;
				se_index_array[seIndex].subject_id = seInfo.subject_id;
				se_index_array[seIndex].start_rec = se_index_array[seIndex].end_rec = i;

				// ----------- new type ---------//
				typeIndex++;
				type_index_array[typeIndex].subject_type = seInfo.subject_type;
				type_index_array[typeIndex].start_rec = type_index_array[typeIndex].end_rec = seIndex;

				lastType = seInfo.subject_type;
				lastId = seInfo.subject_id;
			}
			else
			{
				if (lastId != seInfo.subject_id)
				{
					// ----------- new (type,Id) ---------//
					seIndex++;
					se_index_array[seIndex].subject_type = seInfo.subject_type;
					se_index_array[seIndex].subject_id = seInfo.subject_id;
					se_index_array[seIndex].start_rec = i;
					se_index_array[seIndex].end_rec = i;

					lastId = seInfo.subject_id;
				}
				else
				{
					se_index_array[seIndex].end_rec = i;
				}

				type_index_array[typeIndex].end_rec = seIndex;
			}
		}
	}

	private uint random(int bound)
	{
		uint MULTIPLIER = 0x015a4e35;
		uint INCREMENT = 1;
		seed = unchecked(MULTIPLIER * seed + INCREMENT);
		return (uint)(seed % bound);
	}
}