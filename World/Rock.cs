namespace TenKingdoms;

public class Rock
{
	private const int ROCK_ALT_PATH = 19;

	private int _remainDelay;

	public int RockId { get; }
	public int LocX { get; }
	public int LocY { get; }
	public int CurFrame { get; private set; }
	
	private RockRes RockRes => Sys.Instance.RockRes;

	public Rock()
	{
	}

	public Rock(int rockId, int locX, int locY)
	{
		RockId = rockId;
		LocX = locX;
		LocY = locY;

		// ------- random frame, random initial _remainDelay  -----//
		RockInfo rockInfo = RockRes.GetRockInfo(rockId);
		CurFrame = 1 + Misc.Random(rockInfo.MaxFrame);

		int initDelayCount = RockRes.GetAnimInfo(RockRes.GetAnimId(rockId, CurFrame)).Delay;
		_remainDelay = 1 + Misc.Random(initDelayCount);
	}

	public void Process()
	{
		if (--_remainDelay <= 0)
		{
			CurFrame = RockRes.ChooseNext(RockId, CurFrame, Misc.Random(ROCK_ALT_PATH));
			_remainDelay = RockRes.GetAnimInfo(RockRes.GetAnimId(RockId, CurFrame)).Delay;
		}
	}
}