using System;

namespace TenKingdoms;

public class SpriteRec
{
    private const int CODE_LEN = 8;
    private const int RECNO_LEN = 5;
    private const int COUNT_LEN = 5;
    private const int SPRITE_PARA_LEN = 2;
    private const int DAMAGE_LEN = 3;
    private const int TURN_RES_LEN = 2;

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
	private const int NAME_LEN = 8;
	private const int ACTION_LEN = 2;
	private const int DIR_ID_LEN = 2;
	private const int RECNO_LEN = 5;
	private const int COUNT_LEN = 2;

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
    public int FirstFrameId; // first frame id to frame_array.
    public int FrameCount; // no. of frames in the movement
}

public class SpriteAttack
{
    public int FirstFrameId; // first frame id to frame_array.
    public int FrameCount; // no. of frames in the movement

    // no. of frames should be delayed between attack motions. (i.e. when one motion is complete,
    // it will delay <delay_frames> before move on to the next action motion in the cycle
    public int AttackDelay;
}

public class SpriteStop
{
    public int FrameId; // frame id to frame_array.
    public int FrameCount;
}

public class SpriteDie
{
    public int FirstFrameId; // first frame id to frame_array.
    public int FrameCount; // no. of frames in the movement
}

public class SpriteGuardStop
{
    public int FirstFrameId; // first frame id to frame_array.
    public int FrameCount;
}

public class SpriteGuardMove
{
    public int FirstFrameId; // first frame recno to frame_array.
    public int FrameCount; // no. of frames in the movement
}

public class SpriteInfo
{
	public const int MAX_SPRITE_DIR_TYPE = 8;
	public const int MAX_UNIT_ATTACK_TYPE = 3;

	public string SpriteCode;

	public int SpriteType;
	public int SpriteSubType;

	public int NeedTurning;
	public int TurnResolution;

	public int LocWidth; // no. of locations it takes horizontally and vertically
	public int LocHeight;

	public int Speed; // based on UnitRes, can be upgraded during the game.
	public int FramesPerStep;
	public int MaxRainSlowdown;
	public int MaxSnowSlowdown;
	public int LightningDamage;
	public bool RemapBitmap;
	public int MaxSpeed; // original speed
	public int CanGuard; // bit0= standing guard, bit1=moving guard

	public ResourceDb _resBitmap; // frame bitmap resource

	// move_array[24] to cater upward and downward directions for projectile
	// and also 16-direction movement for weapons
	public readonly SpriteMove[] Moves = new SpriteMove[3 * MAX_SPRITE_DIR_TYPE];
	public readonly SpriteAttack[,] Attacks = new SpriteAttack[MAX_UNIT_ATTACK_TYPE, MAX_SPRITE_DIR_TYPE];
	public readonly SpriteStop[] Stops = new SpriteStop[3 * MAX_SPRITE_DIR_TYPE];
	public readonly SpriteDie Die = new SpriteDie();
	public readonly SpriteGuardStop[] GuardStops = new SpriteGuardStop[MAX_SPRITE_DIR_TYPE];
	public readonly SpriteGuardMove[] GuardMoves = new SpriteGuardMove[MAX_SPRITE_DIR_TYPE];

	public SubSpriteInfo[] SubSpriteInfo;

	public SpriteInfo()
	{
		for (int i = 0; i < Moves.Length; i++)
			Moves[i] = new SpriteMove();

		for (int i = 0; i < Attacks.GetLength(0); i++)
			for (int j = 0; j < Attacks.GetLength(1); j++)
				Attacks[i, j] = new SpriteAttack();

		for (int i = 0; i < Stops.Length; i++)
			Stops[i] = new SpriteStop();
		
		for (int i = 0; i < GuardStops.Length; i++)
			GuardStops[i] = new SpriteGuardStop();
		
		for (int i = 0; i < GuardMoves.Length; i++)
			GuardMoves[i] = new SpriteGuardMove();
	}

	/*public void load_bitmap_res()
	{
		res_bitmap = new ResourceDb($"{Sys.GameDataFolder}/Sprite/{sprite_code}.SPR");
	}*/

	public SubSpriteInfo GetSubSpriteInfo(int i)
	{
		return i >= 1 && i <= SubSpriteInfo.Length ? SubSpriteInfo[i - 1] : null;
	}

	public int CanStandGuard()
	{
		return CanGuard & 1;
	}

	public int CanMoveGuard()
	{
		return CanGuard & 2;
	}
}

public class SubSpriteRec
{
    private const int CODE_LEN = 8;
    private const int SUB_NO_LEN = 3;
    private const int OFFSET_LEN = 3;
    private const int RECNO_LEN = 3;

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
    public SpriteInfo SpriteInfo;
    public int SpriteId;
    public int OffsetX;
    public int OffsetY;
}

public class SpriteRes
{
	private const string SPRITE_DB = "SPRITE";
	private const string SUB_SPRITE_DB = "SUB_SPR";
	private const string SPRITE_ACTION_DB = "SACTION";

    private SpriteInfo[] _spriteInfos;
    private SubSpriteInfo[] _subSpriteInfos;

    public GameSet GameSet { get; }

    public SpriteRes(GameSet gameSet)
    {
	    GameSet = gameSet;

	    LoadSpriteInfo();
        LoadSubSpriteInfo();
    }

    public void UpdateSpeed()
    {
	    int rainScale = Sys.Instance.Weather.rain_scale();
	    int snowScale = Sys.Instance.Weather.snow_scale();

	    rainScale = rainScale > 7 ? 7 : rainScale;
	    snowScale = snowScale > 7 ? 7 : snowScale;

	    foreach (SpriteInfo spriteInfo in _spriteInfos)
	    {
		    int speedDrop = 0;

		    if (rainScale > 0 && spriteInfo.MaxRainSlowdown > 0)
		    {
			    speedDrop += rainScale * spriteInfo.MaxRainSlowdown / 8 + 1;
		    }

		    if (snowScale > 0 && spriteInfo.MaxSnowSlowdown > 0)
		    {
			    speedDrop += snowScale * spriteInfo.MaxSnowSlowdown / 8 + 1;
		    }

		    spriteInfo.Speed = spriteInfo.MaxSpeed - speedDrop;
	    }
    }

    public SpriteInfo this[int recNo] => _spriteInfos[recNo - 1];

    private void LoadSpriteInfo()
    {
	    Database dbSprite = GameSet.OpenDb(SPRITE_DB);
	    _spriteInfos = new SpriteInfo[dbSprite.RecordCount];

	    int[] firstDirIds = new int[_spriteInfos.Length];
	    int[] dirCounts = new int[_spriteInfos.Length];

	    for (int i = 0; i < _spriteInfos.Length; i++)
	    {
		    SpriteRec spriteRec = new SpriteRec(dbSprite.Read(i + 1));
		    SpriteInfo spriteInfo = new SpriteInfo();
		    _spriteInfos[i] = spriteInfo;

		    spriteInfo.SpriteCode = Misc.ToString(spriteRec.sprite_code);

		    spriteInfo.SpriteType = spriteRec.sprite_type;
		    if (spriteInfo.SpriteType == ' ')
			    spriteInfo.SpriteType = 0;

		    spriteInfo.SpriteSubType = spriteRec.sprite_sub_type;
		    if (spriteInfo.SpriteSubType == ' ')
			    spriteInfo.SpriteSubType = 0;

		    if (spriteRec.need_turning != ' ')
			    spriteInfo.NeedTurning = spriteRec.need_turning - '0';

		    spriteInfo.TurnResolution = Misc.ToInt32(spriteRec.turn_resolution);
		    spriteInfo.LocWidth = Misc.ToInt32(spriteRec.loc_width);
		    spriteInfo.LocHeight = Misc.ToInt32(spriteRec.loc_height);

		    spriteInfo.Speed = Misc.ToInt32(spriteRec.speed);
		    spriteInfo.MaxSpeed = Misc.ToInt32(spriteRec.speed);
		    spriteInfo.FramesPerStep = Misc.ToInt32(spriteRec.frames_per_step);

		    spriteInfo.MaxRainSlowdown = Misc.ToInt32(spriteRec.max_rain_slowdown);
		    spriteInfo.MaxSnowSlowdown = Misc.ToInt32(spriteRec.max_snow_slowdown);
		    spriteInfo.LightningDamage = Misc.ToInt32(spriteRec.lightning_damage);
		    if (spriteRec.remap_bitmap_flag == '\0' || spriteRec.remap_bitmap_flag == ' ' || spriteRec.remap_bitmap_flag == '0')
			    spriteInfo.RemapBitmap = false;
		    else
			    spriteInfo.RemapBitmap = true;
		    
		    spriteInfo._resBitmap = new ResourceDb($"{Sys.GameDataFolder}/Sprite/{spriteInfo.SpriteCode}.SPR");

		    firstDirIds[i] = Misc.ToInt32(spriteRec.first_move_recno);
		    dirCounts[i] = Misc.ToInt32(spriteRec.move_count);
	    }

	    Database dbSpriteMove = GameSet.OpenDb(SPRITE_ACTION_DB);

	    for (int i = 0; i < _spriteInfos.Length; i++)
	    {
		    SpriteInfo spriteInfo = _spriteInfos[i];
		    int actionId = firstDirIds[i];

		    for (int j = 0; j < dirCounts[i]; j++, actionId++)
		    {
			    SpriteActionRec spriteActionRec = new SpriteActionRec(dbSpriteMove.Read(actionId));

			    int dirId = Misc.ToInt32(spriteActionRec.dir_id);

			    //--------- move motion --------//

			    if (spriteActionRec.action[0] == 'M')
			    {
				    SpriteMove spriteMove = spriteInfo.Moves[dirId];

				    spriteMove.FirstFrameId = Misc.ToInt32(spriteActionRec.first_frame_recno);
				    spriteMove.FrameCount = Misc.ToInt32(spriteActionRec.frame_count);

				    //--- the first movement frame is the default stop frame ---//

				    if (spriteInfo.Stops[dirId].FrameId == 0)
				    {
					    spriteInfo.Stops[dirId].FrameId = spriteMove.FirstFrameId;
					    spriteInfo.Stops[dirId].FrameCount = 1;
				    }
			    }

			    //-------- attacking motion or weapon motion --------//

			    else if (spriteActionRec.action[0] == 'A')
			    {
				    SpriteAttack spriteAttack = spriteInfo.Attacks[spriteActionRec.action[1] - '1', dirId];

				    spriteAttack.FirstFrameId = Misc.ToInt32(spriteActionRec.first_frame_recno);
				    spriteAttack.FrameCount = Misc.ToInt32(spriteActionRec.frame_count);
			    }

			    //--------- stop bitmap ---------//

			    else if (spriteActionRec.action[0] == 'S')
			    {
				    spriteInfo.Stops[dirId].FrameId = Misc.ToInt32(spriteActionRec.first_frame_recno);
				    spriteInfo.Stops[dirId].FrameCount = Misc.ToInt32(spriteActionRec.frame_count);
			    }

			    //-------- dying motion ---------//

			    else if (spriteActionRec.action[0] == 'D')
			    {
				    spriteInfo.Die.FirstFrameId = Misc.ToInt32(spriteActionRec.first_frame_recno);
				    spriteInfo.Die.FrameCount = Misc.ToInt32(spriteActionRec.frame_count);
			    }

			    //--------- guarding motion --------//

			    else if (spriteActionRec.action[0] == 'G')
			    {
				    if (spriteActionRec.action[1] == 'M')
				    {
					    // moving guard
					    SpriteGuardMove spriteGuardMove = spriteInfo.GuardMoves[dirId];
					    spriteGuardMove.FirstFrameId = Misc.ToInt32(spriteActionRec.first_frame_recno);
					    spriteGuardMove.FrameCount = Misc.ToInt32(spriteActionRec.frame_count);

					    spriteInfo.CanGuard |= 2;
				    }
				    else
				    {
					    // standing guard
					    SpriteGuardStop spriteGuardStop = spriteInfo.GuardStops[dirId];
					    spriteGuardStop.FirstFrameId = Misc.ToInt32(spriteActionRec.first_frame_recno);
					    spriteGuardStop.FrameCount = Misc.ToInt32(spriteActionRec.frame_count);

					    spriteInfo.CanGuard |= 1;
				    }
			    }
		    }
	    }
    }

    private void LoadSubSpriteInfo()
    {
	    Database dbSubSprite = GameSet.OpenDb(SUB_SPRITE_DB);
	    _subSpriteInfos = new SubSpriteInfo[dbSubSprite.RecordCount];

	    for (int i = 0; i < _subSpriteInfos.Length; i++)
	    {
		    SubSpriteRec subSpriteRec = new SubSpriteRec(dbSubSprite.Read(i + 1));
		    SubSpriteInfo subSpriteInfo = new SubSpriteInfo();
		    _subSpriteInfos[i] = subSpriteInfo;

		    subSpriteInfo.SpriteId = Misc.ToInt32(subSpriteRec.sub_sprite_id);
		    subSpriteInfo.SpriteInfo = _spriteInfos[subSpriteInfo.SpriteId - 1];
		    subSpriteInfo.OffsetX = Misc.ToInt32(subSpriteRec.offset_x);
		    subSpriteInfo.OffsetY = Misc.ToInt32(subSpriteRec.offset_y);

		    // set link from parent
		    // assume SUB_SPR database is sorted by sprite_name and sub_no

		    int subNo = Misc.ToInt32(subSpriteRec.sub_no);
		    SpriteInfo parentSprite = this[Misc.ToInt32(subSpriteRec.sprite_id)];

		    var oldSubSpriteInfo = parentSprite.SubSpriteInfo;
		    parentSprite.SubSpriteInfo = new SubSpriteInfo[subNo];

		    if (oldSubSpriteInfo != null)
		    {
			    for (int j = 0; j < oldSubSpriteInfo.Length; j++)
			    {
				    parentSprite.SubSpriteInfo[j] = oldSubSpriteInfo[j];
			    }
		    }
		    
		    parentSprite.SubSpriteInfo[^1] = subSpriteInfo;
	    }
    }
}
