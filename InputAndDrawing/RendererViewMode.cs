using System;

namespace TenKingdoms;

public partial class Renderer
{
    private void SelectViewMode(int mouseEventX, int mouseEventY)
    {
        int menuWidth = 86;
        int menuHeight = 24;
        
        if (mouseEventX >= 11 && mouseEventX < 11 + menuWidth && mouseEventY >= 12 && mouseEventY < 12 + menuHeight)
            _viewMode = _viewMode == ViewMode.Kingdoms ? ViewMode.Normal : ViewMode.Kingdoms;
        
        if (mouseEventX >= 104 && mouseEventX < 104 + menuWidth && mouseEventY >= 12 && mouseEventY < 12 + menuHeight)
            _viewMode = _viewMode == ViewMode.Villages ? ViewMode.Normal : ViewMode.Villages;
        
        if (mouseEventX >= 199 && mouseEventX < 199 + menuWidth && mouseEventY >= 12 && mouseEventY < 12 + menuHeight)
            _viewMode = _viewMode == ViewMode.Economy ? ViewMode.Normal : ViewMode.Economy;
        
        if (mouseEventX >= 293 && mouseEventX < 293 + menuWidth && mouseEventY >= 12 && mouseEventY < 12 + menuHeight)
            _viewMode = _viewMode == ViewMode.Trade ? ViewMode.Normal : ViewMode.Trade;
        
        if (mouseEventX >= 20 && mouseEventX < 20 + menuWidth && mouseEventY >= 44 && mouseEventY < 44 + menuHeight)
            _viewMode = _viewMode == ViewMode.Military ? ViewMode.Normal : ViewMode.Military;
        
        if (mouseEventX >= 113 && mouseEventX < 113 + menuWidth && mouseEventY >= 44 && mouseEventY < 44 + menuHeight)
            _viewMode = _viewMode == ViewMode.Technology ? ViewMode.Normal : ViewMode.Technology;
        
        if (mouseEventX >= 208 && mouseEventX < 208 + menuWidth && mouseEventY >= 44 && mouseEventY < 44 + menuHeight)
            _viewMode = _viewMode == ViewMode.Espionage ? ViewMode.Normal : ViewMode.Espionage;
        
        if (mouseEventX >= 302 && mouseEventX < 302 + menuWidth && mouseEventY >= 44 && mouseEventY < 44 + menuHeight)
            _viewMode = _viewMode == ViewMode.Ranking ? ViewMode.Normal : ViewMode.Ranking;
    }
    
    private void DrawSelectedView()
    {
        if (_viewMode == ViewMode.Kingdoms)
            DrawKingdomsView();
    }

    private void HandleSelectedView()
    {
        if (_viewMode == ViewMode.Kingdoms)
            HandleKingdomsView();
    }
}