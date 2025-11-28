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

        // TODO display hit points
        
        if (_leftMousePressed && IsMouseOnBuilderOrRequestBuilderButton())
        {
            if (ShowBuilderButton(firm))
                Graphics.DrawBitmapScaled(_buttonRepairDownTexture, DetailsX1 + 10, DetailsY1 + 54, _buttonRepairDownTextureWidth, _buttonRepairDownTextureHeight);
            if (ShowRequestBuilderButton(firm))
                Graphics.DrawBitmapScaled(_buttonRequestRepairDownTexture, DetailsX1 + 10, DetailsY1 + 54, _buttonRequestRepairDownTextureWidth, _buttonRequestRepairDownTextureHeight);
        }
        else
        {
            if (ShowBuilderButton(firm))
                Graphics.DrawBitmapScaled(_buttonRepairUpTexture, DetailsX1 + 10, DetailsY1 + 54, _buttonRepairUpTextureWidth, _buttonRepairUpTextureHeight);
            if (ShowRequestBuilderButton(firm))
                Graphics.DrawBitmapScaled(_buttonRequestRepairUpTexture, DetailsX1 + 10, DetailsY1 + 54, _buttonRequestRepairUpTextureWidth, _buttonRequestRepairUpTextureHeight);
        }

        if (firm.OwnFirm())
        {
            if (_leftMousePressed && IsMouseOnSellOrDestructButton())
            {
                if (!firm.UnderConstruction && firm.CanSell())
                    Graphics.DrawBitmapScaled(_buttonSellDownTexture, DetailsX1 + 370, DetailsY1 + 52, _buttonSellDownTextureWidth, _buttonSellDownTextureHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonDestructDownTexture, DetailsX1 + 370, DetailsY1 + 52, _buttonDestructDownTextureWidth, _buttonDestructDownTextureHeight);
            }
            else
            {
                if (!firm.UnderConstruction && firm.CanSell())
                    Graphics.DrawBitmapScaled(_buttonSellUpTexture, DetailsX1 + 370, DetailsY1 + 52, _buttonSellUpTextureWidth, _buttonSellUpTextureHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonDestructUpTexture, DetailsX1 + 370, DetailsY1 + 52, _buttonDestructUpTextureWidth, _buttonDestructUpTextureHeight);
            }
        }

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
            UnitInfo unitInfo = UnitRes[worker.UnitId];
            Graphics.DrawBitmap(unitInfo.GetSmallIconTexture(Graphics, worker.RankId), DetailsX1 + 12 + 100 * (i % 4), y + 7 + 50 * (i / 4),
                unitInfo.soldierSmallIconWidth * 2, unitInfo.soldierSmallIconHeight * 2);
            PutText(FontSan, firm.FirmType == Firm.FIRM_CAMP ? worker.CombatLevel.ToString() : worker.SkillLevel.ToString(),
                DetailsX1 + 64 + 100 * (i % 4), y + 13 + 50 * (i / 4));
            
            // TODO worker hit points bar
            // TODO spy icon
            // TODO selected worker
        }
    }

    private void HandleFirmDetailsInput(Firm firm)
    {
        bool colorSquareButtonPressed = _leftMouseReleased && _mouseButtonX >= DetailsX1 + 18 && _mouseButtonX <= DetailsX1 + 48 &&
                                        _mouseButtonY >= DetailsY1 + 9 && _mouseButtonY <= DetailsY1 + 32;
        if (colorSquareButtonPressed)
            GoToLocation(firm.LocCenterX, firm.LocCenterY);
        
        if (_leftMouseReleased && IsMouseOnBuilderOrRequestBuilderButton())
        {
            if (ShowBuilderButton(firm) && UnitArray[firm.BuilderId].IsOwn())
            {
                /*if (remote.is_enable())
                {
                    // packet structure : <firm recno>
			        short *shortPtr = (short *)remote.new_send_queue_msg(MSG_FIRM_MOBL_BUILDER, sizeof(short));
			        *shortPtr = firm_recno;
                }
                else
                {*/
                    firm.SetBuilder(0);
                //}
            }
            else
            {
                if (ShowRequestBuilderButton(firm))
                {
                    firm.SendIdleBuilderHere(InternalConstants.COMMAND_PLAYER);
                }
            }
        }

        if (_leftMouseReleased && IsMouseOnSellOrDestructButton())
        {
            if (firm.OwnFirm())
            {
                if (!firm.UnderConstruction && firm.CanSell())
                {
                    firm.SellFirm(InternalConstants.COMMAND_PLAYER);
                }
                else
                {
                    if (firm.UnderConstruction)
                        firm.CancelConstruction(InternalConstants.COMMAND_PLAYER);
                    else
                        firm.DestructFirm(InternalConstants.COMMAND_PLAYER);
                }
            }
        }

        firm.HandleDetailsInput(this);
    }
    
    private bool ShowBuilderButton(Firm firm)
    {
        return !firm.UnderConstruction && firm.BuilderId != 0 && firm.ShouldShowInfo();
    }

    private bool ShowRequestBuilderButton(Firm firm)
    {
        return !firm.UnderConstruction && firm.BuilderId == 0 && firm.OwnFirm() && firm.FindIdleBuilder() != 0;
    }

    private bool IsFirmSpyListEnabled(Firm firm)
    {
        return firm.PlayerSpyCount > 0;
    }

    private bool IsMouseOnBuilderOrRequestBuilderButton()
    {
        return _mouseButtonX >= DetailsX1 + 10 && _mouseButtonX <= DetailsX1 + 38 &&
               _mouseButtonY >= DetailsY1 + 54 && _mouseButtonY <= DetailsY1 + 85;
    }

    private bool IsMouseOnSellOrDestructButton()
    {
        return _mouseButtonX >= DetailsX1 + 375 && _mouseButtonX <= DetailsX1 + 399 &&
               _mouseButtonY >= DetailsY1 + 54 && _mouseButtonY <= DetailsY1 + 85;
    }
}