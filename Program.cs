using System;

namespace TenKingdoms;

public class Program
{
    public static void Main(string[] args)
    {
        Sys.Instance = new Sys();
        Sys.Instance.Run();
    }
}
