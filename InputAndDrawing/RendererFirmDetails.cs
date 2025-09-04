namespace TenKingdoms;

public partial class Renderer
{
    // TODO show spies list, show bribe menu, show assassination result, show view secret menu
    // TODO go to firm location when pressing color square
    
    private void DrawFirmDetails(Firm firm)
    {
        DrawSmallPanel(DetailsX1 + 2, DetailsY1);
        int firmNameX1 = DetailsX1 + 2;
        if (firm.NationId != 0)
        {
            firmNameX1 += 8 + _colorSquareWidth * 2;
            int textureKey = ColorRemap.GetTextureKey(ColorRemap.ColorSchemes[firm.NationId], false);
            Graphics.DrawBitmap(_colorSquareTextures[textureKey], DetailsX1 + 10, DetailsY1 + 3, _colorSquareWidth * 2, _colorSquareHeight * 2);
        }
        PutTextCenter(FontSan, firm.FirmName(), firmNameX1, DetailsY1, DetailsX2 - 4, DetailsY1 + 42);

        DrawSmallPanel(DetailsX1 + 2, DetailsY1 + 48);
        // TODO display sell, destroy, repair and request repair buttons
        // TODO display hit points
        
        if (firm.UnderConstruction)
        {
            DrawSmallPanel(DetailsX1 + 2, DetailsY1 + 96);
            PutTextCenter(FontSan, "Under construction", DetailsX1 + 2, DetailsY1 + 96, DetailsX2 - 4, DetailsY1 + 96 + 42);
            return;
        }

        if (firm.ShouldShowInfo())
            firm.DrawDetails(this);
    }

    private void DrawWorkers(Firm firm, int y)
    {
        DrawWorkersPanel(DetailsX1 + 2, y);

        if (_selectedWorkerId > firm.Workers.Count)
            _selectedWorkerId = 0;

        for (int i = 0; i < firm.Workers.Count; i++)
        {
            Worker worker = firm.Workers[i];
            UnitInfo unitInfo = UnitRes[worker.unit_id];
            Graphics.DrawBitmap(unitInfo.GetSmallIconTexture(Graphics, worker.rank_id), DetailsX1 + 12 + 100 * (i % 4), y + 7 + 50 * (i / 4),
                unitInfo.soldierSmallIconWidth * 2, unitInfo.soldierSmallIconHeight * 2);
            PutText(FontSan, firm.FirmType == Firm.FIRM_CAMP ? worker.combat_level.ToString() : worker.skill_level.ToString(),
                DetailsX1 + 64 + 100 * (i % 4), y + 13 + 50 * (i / 4));
            
            // TODO worker hit points bar
            // TODO spy icon
            // TODO selected worker
        }
    }

    private bool IsFirmSpyListEnabled(Firm firm)
    {
        return firm.PlayerSpyCount > 0;
    }

    private void HandleFirmDetailsInput(Firm firm)
    {
        // TODO handle sell, destroy, repair and request repair buttons
        
        firm.HandleDetailsInput(this);
    }
}