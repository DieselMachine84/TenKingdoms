using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TenKingdoms;

public class SaveGameProvider
{
    private const string SavedGamesDir = "SavedGames";
    private DateTime _lastReadTime = DateTime.Today;
    private List<SavedGame> _cachedSavedGames;

    public List<SavedGame> GetSavedGames()
    {
        if (DateTime.Now > _lastReadTime.AddMinutes(1.0))
        {
            _cachedSavedGames = null;
        }
        
        if (_cachedSavedGames == null)
        {
            if (!Directory.Exists(SavedGamesDir))
                Directory.CreateDirectory(SavedGamesDir);

            _cachedSavedGames = new List<SavedGame>();
            DirectoryInfo directoryInfo = new DirectoryInfo(SavedGamesDir);
            List<FileInfo> saveFiles = directoryInfo.GetFiles("*.tks").OrderBy(fi => fi.Name).ToList();
            foreach (FileInfo saveFile in saveFiles)
            {
                using FileStream stream = new FileStream(saveFile.FullName, FileMode.Open, FileAccess.Read);
                using BinaryReader reader = new BinaryReader(stream);
                SavedGame savedGame = new SavedGame();
                savedGame.LoadFrom(reader);
                savedGame.FileName = saveFile.Name;
                savedGame.FileDate = saveFile.LastWriteTime;
                _cachedSavedGames.Add(savedGame);
            }
            _lastReadTime = DateTime.Now;
        }
        
        return _cachedSavedGames;
    }

    public void SaveGame(SavedGame savedGame, MemoryStream memoryStream, bool createNew)
    {
        if (!Directory.Exists(SavedGamesDir))
            Directory.CreateDirectory(SavedGamesDir);

        DirectoryInfo directoryInfo = new DirectoryInfo(SavedGamesDir);
        FileInfo[] saveFiles = directoryInfo.GetFiles("*.tks");
        string fileName = savedGame.PlayerName;
        if (fileName.Length > 4)
            fileName = fileName.Substring(0, 4).TrimEnd();
        fileName += "_";

        if (createNew)
        {
            for (int i = 1; i < 999; i++)
            {
                string newSaveFileName = fileName + i.ToString("000") + ".tks";
                bool fileExists = false;
                foreach (FileInfo saveFile in saveFiles)
                {
                    if (saveFile.Name == newSaveFileName)
                    {
                        fileExists = true;
                        break;
                    }
                }

                if (!fileExists)
                {
                    savedGame.FileName = newSaveFileName;
                    SaveToFile(memoryStream, Path.Combine(directoryInfo.FullName, newSaveFileName));
                    return;
                }
            }
        }
        else
        {
            SaveToFile(memoryStream, Path.Combine(directoryInfo.FullName, savedGame.FileName));
        }
    }

    private void SaveToFile(MemoryStream memoryStream, string path)
    {
        using FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
        memoryStream.Position = 0;
        memoryStream.CopyTo(fileStream);
        fileStream.Flush();
        _cachedSavedGames = null;
    }

    public MemoryStream LoadGame(SavedGame savedGame)
    {
        if (savedGame == null)
            return null;
        
        if (!Directory.Exists(SavedGamesDir))
            Directory.CreateDirectory(SavedGamesDir);

        DirectoryInfo directoryInfo = new DirectoryInfo(SavedGamesDir);
        string saveFilePath = Path.Combine(directoryInfo.FullName, savedGame.FileName);
        return File.Exists(saveFilePath) ? new MemoryStream(File.ReadAllBytes(saveFilePath)) : null;
    }

    public bool DeleteGame(SavedGame savedGame)
    {
        if (savedGame == null)
            return false;
        
        if (!Directory.Exists(SavedGamesDir))
            Directory.CreateDirectory(SavedGamesDir);

        DirectoryInfo directoryInfo = new DirectoryInfo(SavedGamesDir);
        string saveFilePath = Path.Combine(directoryInfo.FullName, savedGame.FileName);
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            _cachedSavedGames = null;
            return true;
        }

        return false;
    }
}

public class SavedGame
{
    public int RaceId { get; set; }
    public int ColorSchemeId { get; set; }
    public string PlayerName { get; set; }
    public DateTime GameDate { get; set; }
    
    public string FileName { get; set; }
    public DateTime FileDate { get; set; }
    
    public void SaveTo(BinaryWriter writer)
    {
        writer.Write(RaceId);
        writer.Write(ColorSchemeId);
        writer.Write(PlayerName);
        writer.Write(GameDate.ToBinary());
    }

    public void LoadFrom(BinaryReader reader)
    {
        RaceId = reader.ReadInt32();
        ColorSchemeId = reader.ReadInt32();
        PlayerName = reader.ReadString();
        GameDate = DateTime.FromBinary(reader.ReadInt64());
    }
}