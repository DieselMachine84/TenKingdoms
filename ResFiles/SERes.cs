using System;
using System.Collections.Generic;

namespace TenKingdoms;

public class SERec
{
    private const int RECNO_LEN = 3;
    private const int CODE_LEN = 12;
    private const int VERB_LEN = 4;
    private const int OUT_FRAME_LEN = 3;
    private const int FILE_NAME_LEN = 8;

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
	public char SubjectType { get; set; } // S=sprite, U=unit, R=race, F=firm, T=town
	public int SubjectId { get; set; }
	public string Action { get; set; }
	public int ObjectType { get; set; }
	public int ObjectId { get; set; }
	public int OutFrame { get; set; }
	public string FileName { get; set; }
	public int EffectId { get; set; } // id returned from SECtrl.Scan

	public bool Match(char subjectType, int subjectId, string action, int objectType, int objectId)
	{
		// objectType == -1, match all object type
		// objectId == -1, match all object id of that type
		return SubjectType == subjectType && (SubjectId == -1 || SubjectId == subjectId) &&
		       (Action == action) &&
		       (ObjectType == -1 || (ObjectType == objectType && (ObjectId == -1 || ObjectId == objectId)));
	}
}

public class SEInfoIndex
{
    public char SubjectType { get; set; }
    public char Dummy;
    public int SubjectId { get; set; }
    public int StartRec { get; set; }
    public int EndRec { get; set; }
}

public class SETypeIndex
{
    public char SubjectType { get; set; }
    public char Dummy { get; set; }
    public int StartRec { get; set; }
    public int EndRec { get; set; }
}

public class SERes
{
	private SEInfo[] SEInfos { get; set; }
	private SEInfoIndex[] SEIndexes { get; set; }
	private SETypeIndex[] SETypeIndexes { get; set; }

	private uint Seed { get; set; }
	public List<byte[]> Sounds { get; } = new List<byte[]>();
	private readonly ResourceIdx _resWave;
	private readonly ResourceIdx _resSupp;

	private GameSet GameSet { get; }

	public SEInfo this[int i] => SEInfos[i - 1];
	
	public SERes(GameSet gameSet)
	{
		GameSet = gameSet;
		Seed = (uint)Misc.GetTime();
		_resWave = new ResourceIdx($"{Sys.GameDataFolder}/Resource/A_WAVE1.RES");
		_resSupp = new ResourceIdx($"{Sys.GameDataFolder}/Resource/A_WAVE2.RES");
		LoadInfo();
	}

	private void LoadInfo()
	{
		Database dbSE = GameSet.OpenDb("SOUNDRES");
		SEInfos = new SEInfo[dbSE.RecordCount];

		for (int i = 0; i < SEInfos.Length; i++)
		{
			SERec seRec = new SERec(dbSE, i + 1);
			SEInfo seInfo = new SEInfo();
			SEInfos[i] = seInfo;

			// ------- copy subject ---------//
			seInfo.SubjectType = seRec.subject_type;
			seInfo.SubjectId = Misc.ToInt32(seRec.subject_id);

			// -------- copy verb ---------//
			seInfo.Action = Misc.ToString(seRec.action);

			// --------- copy object ---------//
			if (seRec.object_type == ' ' || seRec.object_type == '\0')
			{
				seInfo.ObjectType = 0;
				seInfo.ObjectId = 0;
			}
			else if (seRec.object_type == '*')
			{
				seInfo.ObjectType = -1; // all object
				seInfo.ObjectId = -1;
			}
			else
			{
				seInfo.ObjectType = Convert.ToInt32(seRec.object_type);
				if (seRec.object_id[0] != '*')
					seInfo.ObjectId = Misc.ToInt32(seRec.object_id);
				else
					seInfo.ObjectId = -1; // all of the objectType
			}

			seInfo.OutFrame = Misc.ToInt32(seRec.out_frame);
			seInfo.FileName = Misc.ToString(seRec.file_name);
			seInfo.EffectId = SearchEffectId(seInfo.FileName);
		}
		
		for (int i = 0; i < _resWave.RecordCount; i++)
		{
			Sounds.Add(_resWave.GetData(i + 1));
		}

		for (int i = 0; i < _resSupp.RecordCount; i++)
		{
			Sounds.Add(_resSupp.GetData(i + 1));
		}
	}

	public int SearchEffectId(string effectName)
	{
		int idx = _resWave.GetIndex(effectName);
		if (idx != 0)
			return idx;

		idx = _resSupp.GetIndex(effectName);
		if (idx != 0)
			return idx + _resWave.RecordCount;

		return 0;
	}

	public SEInfo Scan(char subjectType, int subjectId, string action, int objectType, int objectId, bool findFirst = false)
	{
		List<SEInfo> matches = new List<SEInfo>(16);
		
		for (int i = 0; i < SEInfos.Length; i++)
		{
			SEInfo seInfo = SEInfos[i];
			if (seInfo.Match(subjectType, subjectId, action, objectType, objectId))
			{
				matches.Add(seInfo);
			}
		}

		if (matches.Count == 0)
			return null;

		if (matches.Count == 1)
			return matches[0];

		return matches[Random(matches.Count)];
	}
	
	private int Random(int bound)
	{
		const uint MULTIPLIER = 0x015a4e35;
		const uint INCREMENT = 1;
		Seed = unchecked(MULTIPLIER * Seed + INCREMENT);
		return (int)(Seed % bound);
	}
}