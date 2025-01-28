using System;
using System.IO;

namespace TenKingdoms;

public class SetRec
{
    public const int CODE_LEN = 8;
    public const int DES_LEN = 60;

    public char[] code = new char[CODE_LEN];
    public char[] des = new char[DES_LEN];

    public SetRec(byte[] data)
    {
        int j = 0;
        for (int i = 0; i < code.Length; i++, j++)
            code[i] = Convert.ToChar(data[j]);

        for (int i = 0; i < des.Length; i++, j++)
            des[i] = Convert.ToChar(data[j]);
    }
}

public class SetInfo
{
    public string code;
    public string des;
}

public class GameSet
{
    public const string SET_HEADER_DB = "HEADER";

    public SetInfo[] set_info_array;

    public ResourceIdx set_res;
    public Database set_db;

    public GameSet()
    {
        LoadSetHeader(); // load the info. of all sets
    }

    public void OpenSet(int setId)
    {
        set_res = new ResourceIdx($"{Sys.GameDataFolder}/Resource/{set_info_array[setId - 1].code}.SET");
    }

    public Database OpenDb(string dbName)
    {
        byte[] data = set_res.Read(dbName);
        set_db = new Database(data);
        return set_db;
    }

    public int FindSet(string setCode)
    {
        for (int i = 0; i < set_info_array.Length; i++)
        {
            if (new string(set_info_array[i].code) == setCode)
                return i + 1;
        }

        return 0;
    }

    public SetInfo this[int recNo] => set_info_array[recNo - 1];

    private void LoadSetHeader()
    {
        DirectoryInfo setDir = new DirectoryInfo($"{Sys.GameDataFolder}/Resource/");
        FileInfo[] files = setDir.GetFiles("*.SET");
        set_info_array = new SetInfo[files.Length];
        
        for (int i = 0; i < files.Length; i++)
        {
            set_res = new ResourceIdx($"{Sys.GameDataFolder}/Resource/" + files[i].Name);
            set_db = new Database(set_res.Read(SET_HEADER_DB));
            SetRec setRec = new SetRec(set_db.Read(1));
            SetInfo setInfo = new SetInfo();
            set_info_array[i] = setInfo;

            setInfo.code = Misc.ToString(setRec.code);
            setInfo.des = Misc.ToString(setRec.des);
        }
    }
}
