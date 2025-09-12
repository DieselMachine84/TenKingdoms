using System;
using System.IO;

namespace TenKingdoms;

public class SetRec
{
    private const int CODE_LEN = 8;
    private const int DES_LEN = 60;

    public char[] code = new char[CODE_LEN];
    public char[] des = new char[DES_LEN];

    public SetRec(Database db, int recNo)
    {
        int dataIndex = 0;
        for (int i = 0; i < code.Length; i++, dataIndex++)
            code[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));

        for (int i = 0; i < des.Length; i++, dataIndex++)
            des[i] = Convert.ToChar(db.ReadByte(recNo, dataIndex));
    }
}

public class SetInfo
{
    public string Code { get; set; }
    public string Description { get; set; }
}

public class GameSet
{
    private const string SET_HEADER_DB = "HEADER";

    private SetInfo[] _setInfos;

    private ResourceIdx _setResource;

    public GameSet()
    {
        LoadSetInfo();
    }

    public void OpenSet(int setId)
    {
        _setResource = new ResourceIdx($"{Sys.GameDataFolder}/Resource/{_setInfos[setId - 1].Code}.SET");
    }

    public Database OpenDb(string dbName)
    {
        return new Database(_setResource.Read(dbName));
    }

    public int FindSet(string setCode)
    {
        for (int i = 0; i < _setInfos.Length; i++)
        {
            if (_setInfos[i].Code == setCode)
                return i + 1;
        }

        return 0;
    }

    public SetInfo this[int recNo] => _setInfos[recNo - 1];

    private void LoadSetInfo()
    {
        DirectoryInfo setDir = new DirectoryInfo($"{Sys.GameDataFolder}/Resource/");
        FileInfo[] files = setDir.GetFiles("*.SET");
        _setInfos = new SetInfo[files.Length];
        
        for (int i = 0; i < files.Length; i++)
        {
            ResourceIdx setResource = new ResourceIdx($"{Sys.GameDataFolder}/Resource/" + files[i].Name);
            Database setDatabase = new Database(setResource.Read(SET_HEADER_DB));
            SetRec setRec = new SetRec(setDatabase, 1);
            SetInfo setInfo = new SetInfo();
            _setInfos[i] = setInfo;

            setInfo.Code = Misc.ToString(setRec.code);
            setInfo.Description = Misc.ToString(setRec.des);
        }
    }
}
