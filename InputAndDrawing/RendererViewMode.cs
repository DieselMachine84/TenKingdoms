namespace TenKingdoms;

public partial class Renderer
{
    private void SelectViewMode(int mouseEventX, int mouseEventY)
    {
        int menuWidth = 86;
        int menuHeight = 24;
        
        if (mouseEventX >= 22 && mouseEventX < 22 + menuWidth && mouseEventY >= 38 && mouseEventY < 38 + menuHeight)
            _viewMode = _viewMode == ViewMode.Kingdoms ? ViewMode.Normal : ViewMode.Kingdoms;
        
        if (mouseEventX >= 115 && mouseEventX < 115 + menuWidth && mouseEventY >= 38 && mouseEventY < 38 + menuHeight)
            _viewMode = _viewMode == ViewMode.Villages ? ViewMode.Normal : ViewMode.Villages;
        
        if (mouseEventX >= 210 && mouseEventX < 210 + menuWidth && mouseEventY >= 38 && mouseEventY < 38 + menuHeight)
            _viewMode = _viewMode == ViewMode.Economy ? ViewMode.Normal : ViewMode.Economy;
        
        if (mouseEventX >= 304 && mouseEventX < 304 + menuWidth && mouseEventY >= 38 && mouseEventY < 38 + menuHeight)
            _viewMode = _viewMode == ViewMode.Trade ? ViewMode.Normal : ViewMode.Trade;
        
        if (mouseEventX >= 31 && mouseEventX < 31 + menuWidth && mouseEventY >= 70 && mouseEventY < 70 + menuHeight)
            _viewMode = _viewMode == ViewMode.Military ? ViewMode.Normal : ViewMode.Military;
        
        if (mouseEventX >= 124 && mouseEventX < 124 + menuWidth && mouseEventY >= 70 && mouseEventY < 70 + menuHeight)
            _viewMode = _viewMode == ViewMode.Technology ? ViewMode.Normal : ViewMode.Technology;
        
        if (mouseEventX >= 219 && mouseEventX < 219 + menuWidth && mouseEventY >= 70 && mouseEventY < 70 + menuHeight)
            _viewMode = _viewMode == ViewMode.Espionage ? ViewMode.Normal : ViewMode.Espionage;
        
        if (mouseEventX >= 313 && mouseEventX < 313 + menuWidth && mouseEventY >= 70 && mouseEventY < 70 + menuHeight)
            _viewMode = _viewMode == ViewMode.Ranking ? ViewMode.Normal : ViewMode.Ranking;
    }
    
    private void DrawSelectedView()
    {
        //
    }
}