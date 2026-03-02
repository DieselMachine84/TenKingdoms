using System.Collections.Generic;

namespace TenKingdoms;

public partial class Renderer
{
    private int _selectedSaveItem;
    
    private void DrawSaveMenu()
    {
        Graphics.GetWindowSize(out int width, out int height);
        int x = (width - Scale(_saveMenuWidth)) / 2;
        int y = (height - Scale(_saveMenuHeight)) / 2;
        Graphics.DrawBitmapScaled(_saveMenuTexture, x, y, _saveMenuWidth, _saveMenuHeight);
        DrawSavedGameItems(x, y);

        bool mouseOnSaveButton = _mouseButtonX >= x + 51 && _mouseButtonX <= x + 199 && _mouseButtonY >= y + 531 && _mouseButtonY <= y + 570;
        bool mouseOnSaveNewButton = _mouseButtonX >= x + 221 && _mouseButtonX <= x + 369 && _mouseButtonY >= y + 531 && _mouseButtonY <= y + 570;
        bool mouseOnDeleteButton = _mouseButtonX >= x + 390 && _mouseButtonX <= x + 538 && _mouseButtonY >= y + 531 && _mouseButtonY <= y + 570;
        bool mouseOnCancelButton = _mouseButtonX >= x + 710 && _mouseButtonX <= x + 858 && _mouseButtonY >= y + 531 && _mouseButtonY <= y + 570;
        if (_leftMousePressed)
        {
            if (mouseOnSaveButton)
                Graphics.DrawBitmapScaled(_savePressedButtonTexture, x + 51, y + 531, _savePressedButtonWidth, _savePressedButtonHeight);
            if (mouseOnSaveNewButton)
                Graphics.DrawBitmapScaled(_savePressedButtonTexture, x + 221, y + 531, _savePressedButtonWidth, _savePressedButtonHeight);
            if (mouseOnDeleteButton)
                Graphics.DrawBitmapScaled(_savePressedButtonTexture, x + 390, y + 531, _savePressedButtonWidth, _savePressedButtonHeight);
            if (mouseOnCancelButton)
                Graphics.DrawBitmapScaled(_savePressedButtonTexture, x + 710, y + 531, _savePressedButtonWidth, _savePressedButtonHeight);
        }
    }

    private void DrawLoadMenu()
    {
        Graphics.GetWindowSize(out int width, out int height);
        int x = (width - Scale(_loadMenuWidth)) / 2;
        int y = (height - Scale(_loadMenuHeight)) / 2;
        Graphics.DrawBitmapScaled(_loadMenuTexture, x, y, _loadMenuWidth, _loadMenuHeight);
        DrawSavedGameItems(x, y);

        bool mouseOnLoadButton = _mouseButtonX >= x + 51 && _mouseButtonX <= x + 199 && _mouseButtonY >= y + 531 && _mouseButtonY <= y + 570;
        bool mouseOnCancelButton = _mouseButtonX >= x + 710 && _mouseButtonX <= x + 858 && _mouseButtonY >= y + 531 && _mouseButtonY <= y + 570;
        if (_leftMousePressed)
        {
            if (mouseOnLoadButton)
                Graphics.DrawBitmapScaled(_savePressedButtonTexture, x + 51, y + 531, _savePressedButtonWidth, _savePressedButtonHeight);
            if (mouseOnCancelButton)
                Graphics.DrawBitmapScaled(_savePressedButtonTexture, x + 710, y + 531, _savePressedButtonWidth, _savePressedButtonHeight);
        }
    }

    private void DrawSavedGameItems(int x, int y)
    {
        List<SavedGame> savedGames = SaveGameProvider.GetSavedGames();
        for (int i = 0; i < savedGames.Count && i < 5; i++)
        {
            SavedGame savedGame = savedGames[i];
            if (savedGame.RaceId != 0)
            {
                foreach (UnitInfo unitInfo in UnitRes.UnitInfos)
                {
                    if (unitInfo.RaceId == savedGame.RaceId)
                    {
                        Graphics.DrawBitmapScaled(unitInfo.GetKingIconTexture(Graphics), x + 58, y + 64 + i * 93, unitInfo.KingIconWidth, unitInfo.KingIconHeight);
                        break;
                    }
                }
            }
            
            DrawNationColor(savedGame.ColorSchemeId, x + 137, y + 64 + i * 93);
            PutText(FontBible, "King " + savedGame.PlayerName, x + 170, y + 60 + i * 93, -1, true);
            PutText(FontBible, "Game Date: " + Misc.ToLongDate(savedGame.GameDate), x + 170, y + 94 + i * 93, -1, true);
            PutText(FontSmall, "File Name: " + savedGame.FileName, x + 640, y + 63 + i * 93, -1, true);
            PutText(FontSmall, "File Date: " + Misc.ToLongDate(savedGame.FileDate) + " " + savedGame.FileDate.ToString("hh:mm"),
                x + 640, y + 99 + i * 93, -1, true);
        }
        
        if (_selectedSaveItem == 1)
            Graphics.DrawRect(x + 50, y + 46, Scale(_saveItemWidth), Scale(_saveItemHeight), 0, 0, 0, 64);
        if (_selectedSaveItem == 2)
            Graphics.DrawRect(x + 50, y + 138, Scale(_saveItemWidth), Scale(_saveItemHeight), 0, 0, 0, 64);
        if (_selectedSaveItem == 3)
            Graphics.DrawRect(x + 50, y + 231, Scale(_saveItemWidth), Scale(_saveItemHeight), 0, 0, 0, 64);
        if (_selectedSaveItem == 4)
            Graphics.DrawRect(x + 50, y + 324, Scale(_saveItemWidth), Scale(_saveItemHeight), 0, 0, 0, 64);
        if (_selectedSaveItem == 5)
            Graphics.DrawRect(x + 50, y + 417, Scale(_saveItemWidth), Scale(_saveItemHeight), 0, 0, 0, 64);

        bool mouseOnItem1 = _mouseButtonX >= x + 53 && _mouseButtonX <= x + 856 && _mouseButtonY >= y + 49 && _mouseButtonY <= y + 136;
        bool mouseOnItem2 = _mouseButtonX >= x + 53 && _mouseButtonX <= x + 856 && _mouseButtonY >= y + 141 && _mouseButtonY <= y + 228;
        bool mouseOnItem3 = _mouseButtonX >= x + 53 && _mouseButtonX <= x + 856 && _mouseButtonY >= y + 234 && _mouseButtonY <= y + 321;
        bool mouseOnItem4 = _mouseButtonX >= x + 53 && _mouseButtonX <= x + 856 && _mouseButtonY >= y + 327 && _mouseButtonY <= y + 414;
        bool mouseOnItem5 = _mouseButtonX >= x + 53 && _mouseButtonX <= x + 856 && _mouseButtonY >= y + 420 && _mouseButtonY <= y + 507;
        if (_leftMousePressed)
        {
            if (mouseOnItem1)
            {
                Graphics.DrawRect(x + 50, y + 46, Scale(_saveItemWidth), Scale(_saveItemHeight), 0, 0, 0, 64);
                Graphics.DrawBitmapScaled(_saveItemTexture, x + 50, y + 46, _saveItemWidth, _saveItemHeight);
            }

            if (mouseOnItem2)
            {
                Graphics.DrawRect(x + 50, y + 138, Scale(_saveItemWidth), Scale(_saveItemHeight), 0, 0, 0, 64);
                Graphics.DrawBitmapScaled(_saveItemTexture, x + 50, y + 138, _saveItemWidth, _saveItemHeight);
            }

            if (mouseOnItem3)
            {
                Graphics.DrawRect(x + 50, y + 231, Scale(_saveItemWidth), Scale(_saveItemHeight), 0, 0, 0, 64);
                Graphics.DrawBitmapScaled(_saveItemTexture, x + 50, y + 231, _saveItemWidth, _saveItemHeight);
            }

            if (mouseOnItem4)
            {
                Graphics.DrawRect(x + 50, y + 324, Scale(_saveItemWidth), Scale(_saveItemHeight), 0, 0, 0, 64);
                Graphics.DrawBitmapScaled(_saveItemTexture, x + 50, y + 324, _saveItemWidth, _saveItemHeight);
            }

            if (mouseOnItem5)
            {
                Graphics.DrawRect(x + 50, y + 417, Scale(_saveItemWidth), Scale(_saveItemHeight), 0, 0, 0, 64);
                Graphics.DrawBitmapScaled(_saveItemTexture, x + 50, y + 417, _saveItemWidth, _saveItemHeight);
            }
        }
    }
    
    private void HandleSaveMenu()
    {
        Graphics.GetWindowSize(out int width, out int height);
        int x = (width - Scale(_saveMenuWidth)) / 2;
        int y = (height - Scale(_saveMenuHeight)) / 2;
        bool mouseOnItem1 = _mouseButtonX >= x + 53 && _mouseButtonX <= x + 856 && _mouseButtonY >= y + 49 && _mouseButtonY <= y + 136;
        bool mouseOnItem2 = _mouseButtonX >= x + 53 && _mouseButtonX <= x + 856 && _mouseButtonY >= y + 141 && _mouseButtonY <= y + 228;
        bool mouseOnItem3 = _mouseButtonX >= x + 53 && _mouseButtonX <= x + 856 && _mouseButtonY >= y + 234 && _mouseButtonY <= y + 321;
        bool mouseOnItem4 = _mouseButtonX >= x + 53 && _mouseButtonX <= x + 856 && _mouseButtonY >= y + 327 && _mouseButtonY <= y + 414;
        bool mouseOnItem5 = _mouseButtonX >= x + 53 && _mouseButtonX <= x + 856 && _mouseButtonY >= y + 420 && _mouseButtonY <= y + 507;
        bool mouseOnSaveButton = _mouseButtonX >= x + 51 && _mouseButtonX <= x + 199 && _mouseButtonY >= y + 531 && _mouseButtonY <= y + 570;
        bool mouseOnSaveNewButton = _mouseButtonX >= x + 221 && _mouseButtonX <= x + 369 && _mouseButtonY >= y + 531 && _mouseButtonY <= y + 570;
        bool mouseOnDeleteButton = _mouseButtonX >= x + 390 && _mouseButtonX <= x + 538 && _mouseButtonY >= y + 531 && _mouseButtonY <= y + 570;
        bool mouseOnCancelButton = _mouseButtonX >= x + 710 && _mouseButtonX <= x + 858 && _mouseButtonY >= y + 531 && _mouseButtonY <= y + 570;
        if (_leftMouseReleased)
        {
            if (mouseOnItem1)
                _selectedSaveItem = 1;
            if (mouseOnItem2)
                _selectedSaveItem = 2;
            if (mouseOnItem3)
                _selectedSaveItem = 3;
            if (mouseOnItem4)
                _selectedSaveItem = 4;
            if (mouseOnItem5)
                _selectedSaveItem = 5;

            if (mouseOnSaveButton && _selectedSaveItem > 0)
            {
                SavedGame selectedSavedGame = null;
                List<SavedGame> savedGames = SaveGameProvider.GetSavedGames();
                for (int i = 0; i < savedGames.Count && i < 5; i++)
                {
                    if (i == _selectedSaveItem - 1)
                    {
                        selectedSavedGame = savedGames[i];
                        break;
                    }
                }
                Sys.Instance.SaveGame(selectedSavedGame);
                GameMode = GameMode.Game;
            }

            if (mouseOnSaveNewButton)
            {
                Sys.Instance.SaveGame(null);
                GameMode = GameMode.Game;
            }

            if (mouseOnDeleteButton && _selectedSaveItem > 0)
            {
                SavedGame selectedSavedGame = null;
                List<SavedGame> savedGames = SaveGameProvider.GetSavedGames();
                for (int i = 0; i < savedGames.Count && i < 5; i++)
                {
                    if (i == _selectedSaveItem - 1)
                    {
                        selectedSavedGame = savedGames[i];
                        break;
                    }
                }
                Sys.Instance.DeleteGame(selectedSavedGame);
            }
            
            if (mouseOnCancelButton)
                GameMode = GameMode.Game;
        }
    }

    private void HandleLoadMenu()
    {
        Graphics.GetWindowSize(out int width, out int height);
        int x = (width - Scale(_loadMenuWidth)) / 2;
        int y = (height - Scale(_loadMenuHeight)) / 2;
        bool mouseOnItem1 = _mouseButtonX >= x + 53 && _mouseButtonX <= x + 856 && _mouseButtonY >= y + 49 && _mouseButtonY <= y + 136;
        bool mouseOnItem2 = _mouseButtonX >= x + 53 && _mouseButtonX <= x + 856 && _mouseButtonY >= y + 141 && _mouseButtonY <= y + 228;
        bool mouseOnItem3 = _mouseButtonX >= x + 53 && _mouseButtonX <= x + 856 && _mouseButtonY >= y + 234 && _mouseButtonY <= y + 321;
        bool mouseOnItem4 = _mouseButtonX >= x + 53 && _mouseButtonX <= x + 856 && _mouseButtonY >= y + 327 && _mouseButtonY <= y + 414;
        bool mouseOnItem5 = _mouseButtonX >= x + 53 && _mouseButtonX <= x + 856 && _mouseButtonY >= y + 420 && _mouseButtonY <= y + 507;
        bool mouseOnLoadButton = _mouseButtonX >= x + 51 && _mouseButtonX <= x + 199 && _mouseButtonY >= y + 531 && _mouseButtonY <= y + 570;
        bool mouseOnCancelButton = _mouseButtonX >= x + 710 && _mouseButtonX <= x + 858 && _mouseButtonY >= y + 531 && _mouseButtonY <= y + 570;
        if (_leftMouseReleased)
        {
            if (mouseOnItem1)
                _selectedSaveItem = 1;
            if (mouseOnItem2)
                _selectedSaveItem = 2;
            if (mouseOnItem3)
                _selectedSaveItem = 3;
            if (mouseOnItem4)
                _selectedSaveItem = 4;
            if (mouseOnItem5)
                _selectedSaveItem = 5;

            if (mouseOnLoadButton && _selectedSaveItem > 0)
            {
                SavedGame selectedSavedGame = null;
                List<SavedGame> savedGames = SaveGameProvider.GetSavedGames();
                for (int i = 0; i < savedGames.Count && i < 5; i++)
                {
                    if (i == _selectedSaveItem - 1)
                    {
                        selectedSavedGame = savedGames[i];
                        break;
                    }
                }

                if (Sys.Instance.LoadGame(selectedSavedGame))
                {
                    GameMode = GameMode.Game;
                    Audio.StopMusicTheme();
                    Audio.PlayGameTheme(NationArray.Player != null ? NationArray.Player.RaceId : 0);
                    Graphics.SetWindowSize(WindowWidth, WindowHeight);
                }
                else
                {
                    GameMode = GameMode.StartMenu;
                    Graphics.SetWindowSize(StartMenuWidth, StartMenuHeight);
                }
            }

            if (mouseOnCancelButton)
                GameMode = _oldGameMode;
        }
    }
}