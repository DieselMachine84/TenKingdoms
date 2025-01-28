namespace TenKingdoms;

public class SERequest
{
	public const int MAX_SE_STORE = 4;

	public byte[] wave_ptr;
	public int resx_id;

	public int req_used;
	public RelVolume[] play_vol = new RelVolume[MAX_SE_STORE];

	public void add_request(RelVolume relVolume)
	{
		if (req_used < MAX_SE_STORE)
		{
			play_vol[req_used] = relVolume;
			req_used++;
		}
		else
		{
			// not enough space, remove the MIN volume one.
			RelVolume minVolume = relVolume;
			for (int i = 0; i < MAX_SE_STORE; ++i)
			{
				if (play_vol[i].rel_vol < minVolume.rel_vol)
				{
					// swap volume[i] and minVolume
					(play_vol[i], minVolume) = (minVolume, play_vol[i]);
				}
			}
		}
	}

	public int max_entry()
	{
		int maxEntry = 0;
		RelVolume maxVolume = play_vol[maxEntry];
		for (int i = 1; i < req_used; ++i)
		{
			if (maxVolume.rel_vol < play_vol[i].rel_vol)
			{
				maxVolume = play_vol[i];
				maxEntry = i;
			}
		}

		return maxEntry;
	}

	public void remove_request(int slot)
	{
		if (slot >= req_used || slot < 0)
			return;

		// ---- move element after slot -----/
		for (int i = slot + 1; i < req_used; ++i)
		{
			play_vol[i - 1] = play_vol[i];
		}

		req_used--;
	}

	public void clear_request()
	{
		req_used = 0;
	}
}

public class SECtrl
{
	public const int MAX_SE_CACHED = 32;

	private int biased_se;
	private SERequest[] req_pool;
	private int[] last_cycle;

	private int cached_size;
	private int[] cached_index = new int[MAX_SE_CACHED];

	public bool audio_flag; // set if audio.wav_init_flag is set during init
	public int max_sound_effect;
	public int max_supp_effect;

	public int total_effect;

	//TODO audio
	//public Audio* audio_ptr;
	public ResourceIdx res_wave;
	public ResourceIdx res_supp;

	private Config Config => Sys.Instance.Config;

	public SECtrl( /*Audio**/)
	{
	}

	public void init()
	{
		//TODO audio
		//audio_flag = audio_ptr.wav_init_flag;
		audio_flag = false;
		if (!audio_flag)
			return;

		//----- open wave resource file -------//

		res_wave = new ResourceIdx($"{Sys.GameDataFolder}/Resource/A_WAVE1.RES");

		//------- load database information --------//

		LoadInfo();

		// ----- clear last_cycle array and wave_ptr --------//
		clear();
	}

	public void request(int soundEffect, RelVolume relVolume)
	{
		if (!audio_flag || !Config.sound_effect_flag)
			return; // skip if audio cannot init wave device
		if (relVolume.rel_vol >= InternalConstants.MIN_AUDIO_VOL && soundEffect != 0)
			req_pool[soundEffect - 1].add_request(relVolume);
	}

	public void request(string soundName, RelVolume relVolume)
	{
		if (!audio_flag || !Config.sound_effect_flag)
			return; // skip if audio cannot init wave device
		int soundEffect = search_effect_id(soundName);
		if (relVolume.rel_vol >= InternalConstants.MIN_AUDIO_VOL && soundEffect != 0)
			req_pool[soundEffect - 1].add_request(relVolume);
	}

	// volume between 0 to 100; pan between -10,000=left, 10,000=right
	public void flush() // output sound effect to volume
	{
		if (!audio_flag || !Config.sound_effect_flag)
		{
			clear();
			return; // skip if audio cannot init wave device
		}
		//TODO audio
	}

	public void clear()
	{
		for (int j = 0; j < total_effect; ++j)
		{
			req_pool[j].clear_request();
		}
	}

	public string get_effect_name(int j)
	{
		if (j > max_sound_effect)
			return res_supp.GetDataName(j - max_sound_effect);
		else
			return res_wave.GetDataName(j);
	}

	public int search_effect_id(string effectName)
	{
		if (!audio_flag)
			return 0; // skip if audio cannot init wave device

		int idx = res_wave.GetIndex(effectName);
		if (idx != 0)
			return idx;

		idx = res_supp.GetIndex(effectName);
		if (idx != 0)
			return idx + max_sound_effect;

		return 0;
	}

	public int search_effect_id(string effectName, int len)
	{
		if (!audio_flag)
			return 0; // skip if audio cannot init wave device

		int idx = res_wave.GetIndex(effectName);
		if (idx != 0)
			return idx;

		idx = res_supp.GetIndex(effectName);
		if (idx != 0)
			return idx + max_sound_effect;

		return 0;
	}

	public int immediate_sound(string soundName, RelVolume relVolume = null) // mainly for button sound, interface
	{
		if (!Config.sound_effect_flag)
			return 0;

		//TODO audio
		/*int effectId = search_effect_id(soundName);
		if (effectId != 0)
		{
			if (relVolume == null)
				relVolume = DEF_REL_VOLUME;
			SERequest seRequest = req_pool[effectId - 1];
			if (seRequest.wave_ptr != null)
				return audio_ptr->play_resided_wav(seRequest.wave_ptr, relVolume);
			else
				return audio_ptr->play_wav(seRequest.resx_id, relVolume);
		}*/

		return 0;
	}

	//	static long sound_volume(short locX, short locY);
	//	static long sound_volume(short locX, short locY, short limit, short drop);
	//	static long sound_pan(short locX, short locY);
	//	static long sound_pan(short locX, short locY, short drop);

	private void LoadInfo()
	{
		int count = max_sound_effect = res_wave.rec_count;
		int suppCount = max_supp_effect = res_supp.rec_count;
		total_effect = max_sound_effect + max_supp_effect;

		req_pool = new SERequest[total_effect];
		last_cycle = new int[total_effect];

		int j = 0;
		for (j = 0; j < count; j++)
		{
			req_pool[j].resx_id = j + 1;
			req_pool[j].wave_ptr = res_wave.GetData(j + 1); // wave data pointer
			last_cycle[j] = 0;
		}

		for (int k = 0; k < suppCount; k++, j++)
		{
			req_pool[j].resx_id = k + 1;
			req_pool[j].wave_ptr = null;
			last_cycle[j] = 0;
		}
	}
}