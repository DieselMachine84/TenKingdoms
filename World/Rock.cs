namespace TenKingdoms;

public class Rock
{
	public const int ROCK_ALT_PATH = 19;

	public int rock_recno;
	public int cur_frame;
	public int delay_remain;
	public int loc_x;
	public int loc_y;

	private uint seed;
	
	public RockRes RockRes { get; }

	public Rock()
	{
	}

	public Rock(RockRes rockRes, int rockRecno, int xLoc, int yLoc)
	{
		RockRes = rockRes;
		rock_recno = rockRecno;
		loc_x = xLoc;
		loc_y = yLoc;
		seed = (uint)((xLoc + yLoc + 3) * (2 * xLoc + 7 * yLoc + 5));

		// ------- random frame, random initial delay_remain  -----//
		RockInfo rockInfo = RockRes.get_rock_info(rockRecno);
		cur_frame = 1 + random(rockInfo.max_frame);

		int initDelayCount = RockRes.get_anim_info(RockRes.get_anim_recno(rockRecno, cur_frame)).delay;
		delay_remain = 1 + random(initDelayCount);
	}

	public void Process()
	{
		if (--delay_remain <= 0)
		{
			cur_frame = RockRes.choose_next(rock_recno, cur_frame, random(ROCK_ALT_PATH));
			delay_remain = RockRes.get_anim_info(RockRes.get_anim_recno(rock_recno, cur_frame)).delay;
		}
	}

	private int random(int bound)
	{
		const int MULTIPLIER = 0x015a4e35;
		const int INCREMENT = 1;
		seed = MULTIPLIER * seed + INCREMENT;
		return (int)(seed % bound);
	}
}