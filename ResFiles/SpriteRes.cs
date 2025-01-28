using System;

namespace TenKingdoms;

public class SpriteRec
{
    public const int CODE_LEN = 8;
    public const int RECNO_LEN = 5;
    public const int COUNT_LEN = 5;
    public const int SPRITE_PARA_LEN = 2;
    public const int DAMAGE_LEN = 3;
    public const int TURN_RES_LEN = 2;

    public char[] sprite_code = new char[CODE_LEN];

    public char sprite_type;
    public char sprite_sub_type;

    public char need_turning;
    public char[] turn_resolution = new char[TURN_RES_LEN];

    public char[] loc_width = new char[SPRITE_PARA_LEN];
    public char[] loc_height = new char[SPRITE_PARA_LEN];

    public char[] speed = new char[SPRITE_PARA_LEN];
    public char[] frames_per_step = new char[SPRITE_PARA_LEN];
    public char[] max_rain_slowdown = new char[SPRITE_PARA_LEN];
    public char[] max_snow_slowdown = new char[SPRITE_PARA_LEN];
    public char[] lightning_damage = new char[DAMAGE_LEN];

    public char remap_bitmap_flag;

    public char[] first_move_recno = new char[RECNO_LEN];
    public char[] move_count = new char[COUNT_LEN];

    public SpriteRec(byte[] data)
    {
        int dataIndex = 0;
        for (int i = 0; i < sprite_code.Length; i++, dataIndex++)
            sprite_code[i] = Convert.ToChar(data[dataIndex]);

        sprite_type = Convert.ToChar(data[dataIndex]);
        dataIndex++;
        sprite_sub_type = Convert.ToChar(data[dataIndex]);
        dataIndex++;
        need_turning = Convert.ToChar(data[dataIndex]);
        dataIndex++;

        for (int i = 0; i < turn_resolution.Length; i++, dataIndex++)
            turn_resolution[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < loc_width.Length; i++, dataIndex++)
            loc_width[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < loc_height.Length; i++, dataIndex++)
            loc_height[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < speed.Length; i++, dataIndex++)
            speed[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < frames_per_step.Length; i++, dataIndex++)
            frames_per_step[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < max_rain_slowdown.Length; i++, dataIndex++)
            max_rain_slowdown[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < max_snow_slowdown.Length; i++, dataIndex++)
            max_snow_slowdown[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < lightning_damage.Length; i++, dataIndex++)
            lightning_damage[i] = Convert.ToChar(data[dataIndex]);

        remap_bitmap_flag = Convert.ToChar(data[dataIndex]);
        dataIndex++;

        for (int i = 0; i < first_move_recno.Length; i++, dataIndex++)
            first_move_recno[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < move_count.Length; i++, dataIndex++)
            move_count[i] = Convert.ToChar(data[dataIndex]);
    }
}

public class SpriteActionRec
{
    public const int NAME_LEN = 8;
    public const int ACTION_LEN = 2;
    public const int DIR_ID_LEN = 2;
    public const int RECNO_LEN = 5;
    public const int COUNT_LEN = 2;

    public char[] sprite_name = new char[NAME_LEN];
    public char[] action = new char[ACTION_LEN];
    public char[] dir_id = new char[DIR_ID_LEN];
    public char[] first_frame_recno = new char[RECNO_LEN];
    public char[] frame_count = new char[COUNT_LEN];

    public SpriteActionRec(byte[] data)
    {
        int dataIndex = 0;
        for (int i = 0; i < sprite_name.Length; i++, dataIndex++)
            sprite_name[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < action.Length; i++, dataIndex++)
            action[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < dir_id.Length; i++, dataIndex++)
            dir_id[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < first_frame_recno.Length; i++, dataIndex++)
            first_frame_recno[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < frame_count.Length; i++, dataIndex++)
            frame_count[i] = Convert.ToChar(data[dataIndex]);
    }
}

public class SpriteMove
{
    public int first_frame_recno; // first frame recno to frame_array.
    public int frame_count; // no. of frames in the movement
}

public class SpriteAttack
{
    public int first_frame_recno; // first frame recno to frame_array.
    public int frame_count; // no. of frames in the movement

    // no. of frames should be delayed between attack motions. (i.e. when one motion is complete,
    // it will delay <delay_frames> before move on to the next action motion in the cycle
    public int attack_delay;
}

public class SpriteStop
{
    public int frame_recno; // frame recno to frame_array.
    public int frame_count;
}

public class SpriteDie
{
    public int first_frame_recno; // first frame recno to frame_array.
    public int frame_count; // no. of frames in the movement
}

public class SpriteGuardStop
{
    public int first_frame_recno; // first frame recno to frame_array.
    public int frame_count;
}

public class SpriteGuardMove
{
    public int first_frame_recno; // first frame recno to frame_array.
    public int frame_count; // no. of frames in the movement
}

public class SpriteInfo
{
	public const int MAX_SPRITE_DIR_TYPE = 8;
	public const int MAX_UNIT_ATTACK_TYPE = 3;

	public string sprite_code;

	public int sprite_type;
	public int sprite_sub_type;

	public int need_turning;
	public int turn_resolution;

	public int loc_width; // no. of locations it takes horizontally and vertically
	public int loc_height;

	public int speed; // based on UnitRes, can be upgraded during the game.
	public int frames_per_step;
	public int max_rain_slowdown;
	public int max_snow_slowdown;
	public int lightning_damage;
	public bool remap_bitmap_flag;
	public int max_speed; // original speed
	public int can_guard_flag; // bit0= standing guard, bit1=moving guard

	public ResourceDb res_bitmap; // frame bitmap resource

	// move_array[24] to cater upward and downward directions for projectile
	// and also 16-direction movement for weapons
	public SpriteMove[] move_array = new SpriteMove[3 * MAX_SPRITE_DIR_TYPE];
	public SpriteAttack[,] attack_array = new SpriteAttack[MAX_UNIT_ATTACK_TYPE, MAX_SPRITE_DIR_TYPE];
	public SpriteStop[] stop_array = new SpriteStop[3 * MAX_SPRITE_DIR_TYPE];
	public SpriteDie die = new SpriteDie();
	public SpriteGuardStop[] guard_stop_array = new SpriteGuardStop[MAX_SPRITE_DIR_TYPE];
	public SpriteGuardMove[] guard_move_array = new SpriteGuardMove[MAX_SPRITE_DIR_TYPE];

	public SubSpriteInfo[] sub_sprite_info;

	public SpriteInfo()
	{
		for (int i = 0; i < move_array.Length; i++)
			move_array[i] = new SpriteMove();

		for (int i = 0; i < attack_array.GetLength(0); i++)
			for (int j = 0; j < attack_array.GetLength(1); j++)
				attack_array[i, j] = new SpriteAttack();

		for (int i = 0; i < stop_array.Length; i++)
			stop_array[i] = new SpriteStop();
		
		for (int i = 0; i < guard_stop_array.Length; i++)
			guard_stop_array[i] = new SpriteGuardStop();
		
		for (int i = 0; i < guard_move_array.Length; i++)
			guard_move_array[i] = new SpriteGuardMove();
	}

	/*public void load_bitmap_res()
	{
		res_bitmap = new ResourceDb($"{Sys.GameDataFolder}/Sprite/{sprite_code}.SPR");
	}*/

	public SpriteInfo get_sub_sprite(int i)
	{
		return i >= 1 && i <= sub_sprite_info.Length ? sub_sprite_info[i - 1].sprite_info : null;
	}

	public SubSpriteInfo get_sub_sprite_info(int i)
	{
		return i >= 1 && i <= sub_sprite_info.Length ? sub_sprite_info[i - 1] : null;
	}

	public int can_stand_guard()
	{
		return can_guard_flag & 1;
	}

	public int can_move_guard()
	{
		return can_guard_flag & 2;
	}

	public int travel_days(int travelDistance)
	{
		int travelFrames = InternalConstants.CellWidth * travelDistance / speed;

		// + 10% for circumstances that the units are blocked and needed to wait and turning, etc.
		return travelFrames / InternalConstants.FRAMES_PER_DAY * 110 / 100;
	}
}

public class SubSpriteRec
{
    public const int CODE_LEN = 8;
    public const int SUB_NO_LEN = 3;
    public const int OFFSET_LEN = 3;
    public const int RECNO_LEN = 3;

    public char[] sprite_code = new char[CODE_LEN];
    public char[] sub_no = new char[SUB_NO_LEN];
    public char[] sub_sprite_code = new char[CODE_LEN];
    public char[] offset_x = new char[OFFSET_LEN];
    public char[] offset_y = new char[OFFSET_LEN];
    public char[] sprite_id = new char[RECNO_LEN];
    public char[] sub_sprite_id = new char[RECNO_LEN];

    public SubSpriteRec(byte[] data)
    {
        int dataIndex = 0;
        for (int i = 0; i < sprite_code.Length; i++, dataIndex++)
            sprite_code[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < sub_no.Length; i++, dataIndex++)
            sub_no[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < sub_sprite_code.Length; i++, dataIndex++)
            sub_sprite_code[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < offset_x.Length; i++, dataIndex++)
            offset_x[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < offset_y.Length; i++, dataIndex++)
            offset_y[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < sprite_id.Length; i++, dataIndex++)
            sprite_id[i] = Convert.ToChar(data[dataIndex]);

        for (int i = 0; i < sub_sprite_id.Length; i++, dataIndex++)
            sub_sprite_id[i] = Convert.ToChar(data[dataIndex]);
    }
}

public class SubSpriteInfo
{
    public SpriteInfo sprite_info;
    public int sprite_id;
    public int offset_x;
    public int offset_y;
}

public class SpriteRes
{
	private const string SPRITE_DB = "SPRITE";
	private const string SUB_SPRITE_DB = "SUB_SPR";
	private const string SPRITE_ACTION_DB = "SACTION";

    public SpriteInfo[] spriteInfos;
    public SubSpriteInfo[] subSpriteInfos;

    public GameSet GameSet { get; }

    public SpriteRes(GameSet gameSet)
    {
	    GameSet = gameSet;

	    LoadSpriteInfo();
        LoadSubSpriteInfo();
    }

    public void update_speed()
    {
	    int rainScale = Sys.Instance.Weather.rain_scale();
	    int snowScale = Sys.Instance.Weather.snow_scale();

	    rainScale = rainScale > 7 ? 7 : rainScale;
	    snowScale = snowScale > 7 ? 7 : snowScale;

	    foreach (SpriteInfo spriteInfo in spriteInfos)
	    {
		    int speedDrop = 0;

		    if (rainScale > 0 && spriteInfo.max_rain_slowdown > 0)
		    {
			    speedDrop += rainScale * spriteInfo.max_rain_slowdown / 8 + 1;
		    }

		    if (snowScale > 0 && spriteInfo.max_snow_slowdown > 0)
		    {
			    speedDrop += snowScale * spriteInfo.max_snow_slowdown / 8 + 1;
		    }

		    spriteInfo.speed = spriteInfo.max_speed - speedDrop;
	    }
    }

    public SpriteInfo this[int recNo] => spriteInfos[recNo - 1];

    private void LoadSpriteInfo()
    {
	    Database dbSprite = GameSet.OpenDb(SPRITE_DB);
	    spriteInfos = new SpriteInfo[dbSprite.RecordCount];

	    int[] first_dir_recno_array = new int[spriteInfos.Length];
	    int[] dir_count_array = new int[spriteInfos.Length];

	    for (int i = 0; i < spriteInfos.Length; i++)
	    {
		    SpriteRec spriteRec = new SpriteRec(dbSprite.Read(i + 1));
		    SpriteInfo spriteInfo = new SpriteInfo();
		    spriteInfos[i] = spriteInfo;

		    spriteInfo.sprite_code = Misc.ToString(spriteRec.sprite_code);

		    spriteInfo.sprite_type = spriteRec.sprite_type;
		    if (spriteInfo.sprite_type == ' ')
			    spriteInfo.sprite_type = 0;

		    spriteInfo.sprite_sub_type = spriteRec.sprite_sub_type;
		    if (spriteInfo.sprite_sub_type == ' ')
			    spriteInfo.sprite_sub_type = 0;

		    if (spriteRec.need_turning != ' ')
			    spriteInfo.need_turning = spriteRec.need_turning - '0';

		    spriteInfo.turn_resolution = Misc.ToInt32(spriteRec.turn_resolution);
		    spriteInfo.loc_width = Misc.ToInt32(spriteRec.loc_width);
		    spriteInfo.loc_height = Misc.ToInt32(spriteRec.loc_height);

		    spriteInfo.speed = Misc.ToInt32(spriteRec.speed);
		    spriteInfo.max_speed = Misc.ToInt32(spriteRec.speed);
		    spriteInfo.frames_per_step = Misc.ToInt32(spriteRec.frames_per_step);

		    spriteInfo.max_rain_slowdown = Misc.ToInt32(spriteRec.max_rain_slowdown);
		    spriteInfo.max_snow_slowdown = Misc.ToInt32(spriteRec.max_snow_slowdown);
		    spriteInfo.lightning_damage = Misc.ToInt32(spriteRec.lightning_damage);
		    if (spriteRec.remap_bitmap_flag == '\0' || spriteRec.remap_bitmap_flag == ' ' || spriteRec.remap_bitmap_flag == '0')
			    spriteInfo.remap_bitmap_flag = false;
		    else
			    spriteInfo.remap_bitmap_flag = true;
		    
		    spriteInfo.res_bitmap = new ResourceDb($"{Sys.GameDataFolder}/Sprite/{spriteInfo.sprite_code}.SPR");

		    first_dir_recno_array[i] = Misc.ToInt32(spriteRec.first_move_recno);
		    dir_count_array[i] = Misc.ToInt32(spriteRec.move_count);
	    }

	    Database dbSpriteMove = GameSet.OpenDb(SPRITE_ACTION_DB);

	    for (int i = 0; i < spriteInfos.Length; i++)
	    {
		    SpriteInfo spriteInfo = spriteInfos[i];
		    int actionRecno = first_dir_recno_array[i];

		    for (int j = 0; j < dir_count_array[i]; j++, actionRecno++)
		    {
			    SpriteActionRec spriteActionRec = new SpriteActionRec(dbSpriteMove.Read(actionRecno));

			    int dirId = Misc.ToInt32(spriteActionRec.dir_id);

			    //--------- move motion --------//

			    if (spriteActionRec.action[0] == 'M')
			    {
				    SpriteMove spriteMove = spriteInfo.move_array[dirId];

				    spriteMove.first_frame_recno = Misc.ToInt32(spriteActionRec.first_frame_recno);
				    spriteMove.frame_count = Misc.ToInt32(spriteActionRec.frame_count);

				    //--- the first movement frame is the default stop frame ---//

				    if (spriteInfo.stop_array[dirId].frame_recno == 0)
				    {
					    spriteInfo.stop_array[dirId].frame_recno = spriteMove.first_frame_recno;
					    spriteInfo.stop_array[dirId].frame_count = 1;
				    }
			    }

			    //-------- attacking motion or weapon motion --------//

			    else if (spriteActionRec.action[0] == 'A')
			    {
				    SpriteAttack spriteAttack = spriteInfo.attack_array[spriteActionRec.action[1] - '1', dirId];

				    spriteAttack.first_frame_recno = Misc.ToInt32(spriteActionRec.first_frame_recno);
				    spriteAttack.frame_count = Misc.ToInt32(spriteActionRec.frame_count);
			    }

			    //--------- stop bitmap ---------//

			    else if (spriteActionRec.action[0] == 'S')
			    {
				    spriteInfo.stop_array[dirId].frame_recno = Misc.ToInt32(spriteActionRec.first_frame_recno);
				    spriteInfo.stop_array[dirId].frame_count = Misc.ToInt32(spriteActionRec.frame_count);
			    }

			    //-------- dying motion ---------//

			    else if (spriteActionRec.action[0] == 'D')
			    {
				    spriteInfo.die.first_frame_recno = Misc.ToInt32(spriteActionRec.first_frame_recno);
				    spriteInfo.die.frame_count = Misc.ToInt32(spriteActionRec.frame_count);
			    }

			    //--------- guarding motion --------//

			    else if (spriteActionRec.action[0] == 'G')
			    {
				    if (spriteActionRec.action[1] == 'M')
				    {
					    // moving guard
					    SpriteGuardMove spriteGuardMove = spriteInfo.guard_move_array[dirId];
					    spriteGuardMove.first_frame_recno = Misc.ToInt32(spriteActionRec.first_frame_recno);
					    spriteGuardMove.frame_count = Misc.ToInt32(spriteActionRec.frame_count);

					    // set can_guard_flag
					    spriteInfo.can_guard_flag |= 2;
				    }
				    else
				    {
					    // standing guard
					    SpriteGuardStop spriteGuardStop = spriteInfo.guard_stop_array[dirId];
					    spriteGuardStop.first_frame_recno = Misc.ToInt32(spriteActionRec.first_frame_recno);
					    spriteGuardStop.frame_count = Misc.ToInt32(spriteActionRec.frame_count);

					    // set can_guard_flag
					    spriteInfo.can_guard_flag |= 1;
				    }
			    }
		    }
	    }
    }

    private void LoadSubSpriteInfo()
    {
	    Database dbSubSprite = GameSet.OpenDb(SUB_SPRITE_DB);
	    subSpriteInfos = new SubSpriteInfo[dbSubSprite.RecordCount];

	    for (int i = 0; i < subSpriteInfos.Length; i++)
	    {
		    SubSpriteRec subSpriteRec = new SubSpriteRec(dbSubSprite.Read(i + 1));
		    SubSpriteInfo subSpriteInfo = new SubSpriteInfo();
		    subSpriteInfos[i] = subSpriteInfo;

		    subSpriteInfo.sprite_id = Misc.ToInt32(subSpriteRec.sub_sprite_id);
		    subSpriteInfo.sprite_info = spriteInfos[subSpriteInfo.sprite_id - 1];
		    subSpriteInfo.offset_x = Misc.ToInt32(subSpriteRec.offset_x);
		    subSpriteInfo.offset_y = Misc.ToInt32(subSpriteRec.offset_y);

		    // set link from parent
		    // assume SUB_SPR database is sorted by sprite_name and sub_no

		    int subNo = Misc.ToInt32(subSpriteRec.sub_no);
		    SpriteInfo parentSprite = this[Misc.ToInt32(subSpriteRec.sprite_id)];

		    parentSprite.sub_sprite_info = new SubSpriteInfo[subNo];
		    for (int j = 0; j < parentSprite.sub_sprite_info.Length; j++)
		    {
			    parentSprite.sub_sprite_info[j] = new SubSpriteInfo();
		    }

		    if (subNo == 1)
			    parentSprite.sub_sprite_info[0] = subSpriteInfo;
	    }
    }
}
