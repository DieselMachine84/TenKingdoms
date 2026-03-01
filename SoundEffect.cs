using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using SDL2;

namespace TenKingdoms;

public class Audio
{
	private bool _initialized;
	private GCHandle[] _pinnedWaves;
	private IntPtr[] _chunks;
	private readonly Dictionary<string, (GCHandle, IntPtr)> _musicFiles = new Dictionary<string, (GCHandle, nint)>();
	private Random _random = new Random();

	private Config Config => Sys.Instance.Config;
	private SERes SERes => Sys.Instance.SERes;
	
	public bool Init()
	{
		_initialized = SDL.SDL_InitSubSystem(SDL.SDL_INIT_AUDIO) == 0;
		if (_initialized)
			_initialized = (SDL_mixer.Mix_OpenAudio(22050, SDL.AUDIO_S16SYS, 2, 4096) == 0);

		if (_initialized)
		{
			_pinnedWaves = new GCHandle[SERes.Sounds.Count];
			_chunks = new IntPtr[SERes.Sounds.Count];
			for (int i = 0; i < SERes.Sounds.Count; i++)
			{
				_pinnedWaves[i] = GCHandle.Alloc(SERes.Sounds[i], GCHandleType.Pinned);
				_chunks[i] = SDL_mixer.Mix_LoadWAV_RW(SDL.SDL_RWFromMem(_pinnedWaves[i].AddrOfPinnedObject(), SERes.Sounds[i].Length), 0);
			}
			
			SDL_mixer.Mix_AllocateChannels(10);
			LoadMusic();
		}

		return _initialized;
	}

	public void Deinit()
	{
		if (_initialized)
		{
			for (int i = 0; i < _chunks.Length; i++)
			{
				SDL_mixer.Mix_FreeChunk(_chunks[i]);
				_pinnedWaves[i].Free();
			}

			foreach (var musicFile in _musicFiles)
			{
				SDL_mixer.Mix_FreeChunk(musicFile.Value.Item2);
				musicFile.Value.Item1.Free();
			}
			
			SDL_mixer.Mix_CloseAudio();
			SDL_mixer.Mix_Quit();
			SDL.SDL_QuitSubSystem(SDL.SDL_INIT_AUDIO);
		}
	}

	private void LoadMusic()
	{
		DirectoryInfo directoryInfo = new DirectoryInfo($"{Sys.GameDataFolder}/Music/");
		FileInfo[] musicFiles = directoryInfo.GetFiles("*.WAV");
		foreach (FileInfo musicFile in musicFiles)
		{
			byte[] content = File.ReadAllBytes(musicFile.FullName);
			GCHandle gcHandle = GCHandle.Alloc(content, GCHandleType.Pinned);
			IntPtr chunk = SDL_mixer.Mix_LoadWAV_RW(SDL.SDL_RWFromMem(gcHandle.AddrOfPinnedObject(), content.Length), 0);
			_musicFiles.Add(musicFile.Name, (gcHandle, chunk));
		}
	}

	public void Play7KTheme()
	{
		if (!_initialized)
			return;

		if (SDL_mixer.Mix_Playing(0) != 0)
			return;

		foreach (var musicFile in _musicFiles)
		{
			if (musicFile.Key.ToLower() == "war.wav")
				SDL_mixer.Mix_PlayChannel(0, musicFile.Value.Item2, 0);
		}
	}

	public void PlayGameTheme(int raceId = 0)
	{
		if (SDL_mixer.Mix_Playing(0) != 0)
			return;
		
		string fileName = raceId switch
		{
			1 => "norman.wav",
			2 => "maya.wav",
			3 => "greek.wav",
			4 => "viking.wav",
			5 => "persian.wav",
			6 => "chinese.wav",
			7 => "japanese.wav",
			_ => String.Empty
		};
		
		if (!String.IsNullOrEmpty(fileName))
		{
			foreach (var musicFile in _musicFiles)
			{
				if (musicFile.Key.ToLower() == fileName)
				{
					SDL_mixer.Mix_PlayChannel(0, musicFile.Value.Item2, 0);
					return;
				}
			}
		}

		List<IntPtr> availableThemes = new List<nint>();
		foreach (var musicFile in _musicFiles)
		{
			string key = musicFile.Key.ToLower();
			if (key == "norman.wav" || key == "maya.wav" || key == "greek.wav" || key == "viking.wav" ||
			    key == "persian.wav" || key == "chinese.wav" || key == "japanese.wav")
			{
				availableThemes.Add(musicFile.Value.Item2);
			}
		}
		
		if (availableThemes.Count > 0)
			SDL_mixer.Mix_PlayChannel(0, availableThemes[_random.Next(availableThemes.Count)], 0);
	}

	public void StopMusicTheme()
	{
		SDL_mixer.Mix_HaltChannel(0);
	}

	private void PlaySound(int channel, string soundName)
	{
		if (!_initialized)
			return;
		
		int effectId = SERes.SearchEffectId(soundName);
		if (effectId != 0)
			SDL_mixer.Mix_PlayChannel(channel, _chunks[effectId - 1], 0);
	}

	private void PlaySound(int channel, int frame, char subjectType, int subjectId, string action, int objectType = 0, int objectId = 0)
	{
		if (!_initialized)
			return;
		
		SEInfo seInfo = Sys.Instance.SERes.Scan(subjectType, subjectId, action, objectType, objectId);
		if (seInfo != null && frame == seInfo.OutFrame)
		{
			SDL_mixer.Mix_PlayChannel(channel, _chunks[seInfo.EffectId - 1], 0);
		}
	}

	public void SelectionSound(string soundName)
	{
		PlaySound(1, soundName);
	}
	
	public void NewsSound(string soundName)
	{
		PlaySound(2, soundName);
	}

	public void OtherSound(string soundName)
	{
		PlaySound(3, soundName);
	}

	public void BirdsSound(string soundName)
	{
		if (SDL_mixer.Mix_Playing(3) == 0)
			PlaySound(3, soundName);
	}
	
	public void SelectionSound(int locX, int locY, int frame, char subjectType, int subjectId, string action, int objectType = 0, int objectId = 0)
	{
		PlaySound(1, frame, subjectType, subjectId, action, objectType, objectId);
	}
	
	public void OtherSound(int locX, int locY, int frame, char subjectType, int subjectId, string action, int objectType = 0, int objectId = 0)
	{
		if (SDL_mixer.Mix_Playing(3) == 0)
			PlaySound(3, frame, subjectType, subjectId, action, objectType, objectId);
	}
	
	public void DieSound(int locX, int locY, int frame, char subjectType, int subjectId, string action, int objectType = 0, int objectId = 0)
	{
		if (Sys.Instance.IsLocationOnScreen(locX, locY) && SDL_mixer.Mix_Playing(4) == 0)
			PlaySound(4, frame, subjectType, subjectId, action, objectType, objectId);
	}

	public void ReadySound(int locX, int locY, int frame, char subjectType, int subjectId, string action, int objectType = 0, int objectId = 0)
	{
		if (SDL_mixer.Mix_Playing(5) == 0)
			PlaySound(5, frame, subjectType, subjectId, action, objectType, objectId);
	}
	
	public void BattleSound(int locX, int locY, int frame, char subjectType, int subjectId, string action, int objectType = 0, int objectId = 0)
	{
		for (int i = 6; i < 9; i++)
		{
			if (Sys.Instance.IsLocationOnScreen(locX, locY) && SDL_mixer.Mix_Playing(i) == 0)
			{
				PlaySound(i, frame, subjectType, subjectId, action, objectType, objectId);
				return;
			}
		}
	}
}
