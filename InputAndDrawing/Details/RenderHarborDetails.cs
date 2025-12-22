using System.Collections.Generic;

namespace TenKingdoms;

public partial class Renderer
{
    //TODO support batch build with shift
    
    private const int BuildShipPanelX = DetailsX1 + 2;
    private const int BuildShipPanelY = DetailsY1 + 2;
    private const int BuildShipButtonNumberX = DetailsX1 + 351;
    private const int BuildShipButtonNumberY = DetailsY1 + 12;
    private const int MouseOnBuildShipButtonX1 = DetailsX1 + 8;
    private const int MouseOnBuildShipButtonX2 = DetailsX1 + 320;
    private const int MouseOnBuildShipButtonY1 = DetailsY1 + 2;
    private const int MouseOnBuildShipButtonY2 = DetailsY1 + 66;
    private const int MouseOnBuildShipNumberButtonX1 = BuildShipButtonNumberX + 4;
    private const int MouseOnBuildShipNumberButtonX2 = BuildShipButtonNumberX + 40;
    private const int MouseOnBuildShipNumberButtonY1 = BuildShipButtonNumberY + 4;
    private const int MouseOnBuildShipNumberButtonY2 = BuildShipButtonNumberY + 42;
    private const int MaxBuildShipItems = 6;
    
    public void DrawHarborDetails(FirmHarbor harbor)
    {
        if (FirmDetailsMode == FirmDetailsMode.BuildShip)
        {
            DrawBuildShipMenu(harbor);
            return;
        }
        
        bool needScroll = harbor.Ships.Count > 3;
        if (needScroll)
        {
            DrawListBoxPanelWithScroll(DetailsX1 + 2, DetailsY1 + 96);
            DrawListBoxScrollPanel(DetailsX1 + 377, DetailsY1 + 96);
        }
        else
        {
            DrawListBoxPanel(DetailsX1 + 2, DetailsY1 + 96);
        }

        if (_selectedShipId != 0 && !harbor.Ships.Contains(_selectedShipId))
        {
            _selectedShipId = 0;
        }
        
        int shipY = DetailsY1 + 109;
        for (int i = 0; i < harbor.Ships.Count; i++)
        {
            if (_selectedShipId == 0)
                _selectedShipId = i + 1;

            Unit ship = UnitArray[harbor.Ships[i]];
            UnitInfo unitInfo = UnitRes[ship.UnitType];
            Graphics.DrawBitmap(unitInfo.GetSmallIconTexture(Graphics, ship.Rank), DetailsX1 + 14, shipY, unitInfo.soldierIconWidth, unitInfo.soldierIconHeight);
            PutText(FontSan, unitInfo.name, DetailsX1 + 70, shipY);
            PutText(FontSan, (int)ship.HitPoints + "/" + ship.MaxHitPoints, DetailsX1 + 280, shipY);
        }

        if (harbor.OwnFirm())
        {
            bool mouseOnButton = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                 _mouseButtonY >= ButtonsHarborY + 2 && _mouseButtonY <= ButtonsHarborY + ButtonHeight;
            if (_leftMousePressed && mouseOnButton)
                Graphics.DrawBitmapScaled(_buttonDownTexture, Button1X, ButtonsHarborY, _buttonDownWidth, _buttonDownHeight);
            else
                Graphics.DrawBitmapScaled(_buttonUpTexture, Button1X, ButtonsHarborY, _buttonUpWidth, _buttonUpHeight);
            Graphics.DrawBitmapScaled(_buttonBuildShipTexture, Button1X + 4, ButtonsHarborY + 7, _buttonBuildShipWidth, _buttonBuildShipHeight);

            if (harbor.Ships.Count > 0)
            {
                mouseOnButton = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                                _mouseButtonY >= ButtonsHarborY + 2 && _mouseButtonY <= ButtonsHarborY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmapScaled(_buttonDownTexture, Button2X, ButtonsHarborY, _buttonDownWidth, _buttonDownHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonUpTexture, Button2X, ButtonsHarborY, _buttonUpWidth, _buttonUpHeight);
                Graphics.DrawBitmapScaled(_buttonSailShipTexture, Button2X + 6, ButtonsHarborY + 4, _buttonSailShipWidth, _buttonSailShipHeight);
            }
            else
            {
                Graphics.DrawBitmapScaled(_buttonDisabledTexture, Button2X, ButtonsHarborY, _buttonDisabledWidth, _buttonDisabledHeight);
                Graphics.DrawBitmapScaled(_buttonSailShipDisabledTexture, Button2X + 6, ButtonsHarborY + 4, _buttonSailShipWidth, _buttonSailShipHeight);
            }
        }
    }

    private void DrawBuildShipMenu(FirmHarbor harbor)
    {
        Dictionary<int, int> buildUnitCounts = new Dictionary<int, int>();
        for (int unitId = 1; unitId <= UnitConstants.MAX_UNIT_TYPE; unitId++)
        {
            UnitInfo unitInfo = UnitRes[unitId];
            if (unitInfo.unit_class == UnitConstants.UNIT_CLASS_SHIP)
                buildUnitCounts.Add(unitId, 0);
        }

        foreach (int buildUnitId in harbor.BuildQueue)
        {
            buildUnitCounts[buildUnitId]++;
        }

        if (harbor.BuildUnitId != 0)
        {
            buildUnitCounts[harbor.BuildUnitId]++;
        }

        int shownItems = 0;
        int dy = 0;
        for (int unitId = 1; unitId <= UnitConstants.MAX_UNIT_TYPE + 1; unitId++)
        {
            bool showCancelButton = (shownItems == MaxBuildShipItems || unitId > UnitConstants.MAX_UNIT_TYPE);
            
            UnitInfo unitInfo = !showCancelButton ? UnitRes[unitId] : null;
            if (!showCancelButton && (unitInfo.unit_class != UnitConstants.UNIT_CLASS_SHIP || unitInfo.get_nation_tech_level(harbor.NationId) == 0))
                continue;

            bool mouseOnButton = _mouseButtonX >= MouseOnBuildShipButtonX1 && _mouseButtonX <= MouseOnBuildShipButtonX2 &&
                                 _mouseButtonY >= MouseOnBuildShipButtonY1 + dy && _mouseButtonY <= MouseOnBuildShipButtonY2 + dy;
            if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
                DrawResearchBuildWeaponPanelDown(BuildShipPanelX, BuildShipPanelY + dy);
            else
                DrawResearchBuildWeaponPanelUp(BuildShipPanelX, BuildShipPanelY + dy);

            if (!showCancelButton)
            {
                Graphics.DrawBitmap(unitInfo.GetLargeIconTexture(Graphics, Unit.RANK_SOLDIER), BuildShipPanelX + 4, BuildShipPanelY + dy + 4,
                    unitInfo.soldierIconWidth * 3 / 2, unitInfo.soldierIconHeight * 3 / 2);
                
                PutText(FontBible, unitInfo.name, BuildShipPanelX + 96, BuildShipPanelY + dy + 10);

                mouseOnButton = _mouseButtonX >= MouseOnBuildShipNumberButtonX1 && _mouseButtonX <= MouseOnBuildShipNumberButtonX2 &&
                                _mouseButtonY >= MouseOnBuildShipNumberButtonY1 + dy && _mouseButtonY <= MouseOnBuildShipNumberButtonY2 + dy;
                if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
                    DrawNumberPanelDown(BuildShipButtonNumberX, BuildShipButtonNumberY + dy);
                else
                    DrawNumberPanelUp(BuildShipButtonNumberX, BuildShipButtonNumberY + dy);
                PutTextCenter(FontBible, buildUnitCounts[unitId].ToString(),
                    BuildShipButtonNumberX, BuildShipButtonNumberY + dy, BuildShipButtonNumberX + 45, BuildShipButtonNumberY + 40 + dy);
                
                shownItems++;
            }
            else
            {
                PutText(FontBible, "Done", BuildShipPanelX + 158, BuildShipPanelY + dy + 10);
                break;
            }
            
            dy += 70;
        }
    }
    
    public void HandleHarborDetailsInput(FirmHarbor harbor)
    {
        if (!harbor.OwnFirm())
            return;
        
        if (FirmDetailsMode == FirmDetailsMode.BuildShip)
        {
            HandleBuildShipMenu(harbor);
            return;
        }
        
        bool mouseOnButton1 = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                              _mouseButtonY >= ButtonsHarborY + 2 && _mouseButtonY <= ButtonsHarborY + ButtonHeight;
        bool mouseOnButton2 = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                              _mouseButtonY >= ButtonsHarborY + 2 && _mouseButtonY <= ButtonsHarborY + ButtonHeight;

        if (_leftMouseReleased && mouseOnButton1)
        {
            FirmDetailsMode = FirmDetailsMode.BuildShip;
        }

        if (_leftMouseReleased && mouseOnButton2 && harbor.Ships.Count > 0)
        {
            harbor.SailShip(harbor.Ships[_selectedShipId - 1], InternalConstants.COMMAND_PLAYER);
        }
    }

    private void HandleBuildShipMenu(FirmHarbor harbor)
    {
        int shownItems = 0;
        int dy = 0;
        for (int unitId = 1; unitId <= UnitConstants.MAX_UNIT_TYPE + 1; unitId++)
        {
            bool onCancelButton = (shownItems == MaxBuildShipItems || unitId > UnitConstants.MAX_UNIT_TYPE);
            
            UnitInfo unitInfo = !onCancelButton ? UnitRes[unitId] : null;
            if (!onCancelButton && (unitInfo.unit_class != UnitConstants.UNIT_CLASS_SHIP || unitInfo.get_nation_tech_level(harbor.NationId) == 0))
                continue;

            bool mouseOnBuildButton = _mouseButtonX >= MouseOnBuildShipButtonX1 && _mouseButtonX <= MouseOnBuildShipButtonX2 &&
                                      _mouseButtonY >= MouseOnBuildShipButtonY1 + dy && _mouseButtonY <= MouseOnBuildShipButtonY2 + dy;
            bool mouseOnBuildNumberButton = _mouseButtonX >= MouseOnBuildShipNumberButtonX1 && _mouseButtonX <= MouseOnBuildShipNumberButtonX2 &&
                                            _mouseButtonY >= MouseOnBuildShipNumberButtonY1 + dy && _mouseButtonY <= MouseOnBuildShipNumberButtonY2 + dy;
            
            if ((mouseOnBuildButton || mouseOnBuildNumberButton) && (_leftMouseReleased || _rightMouseReleased))
            {
                if (!onCancelButton)
                {
                    if (_leftMouseReleased)
                    {
                        /*if (remote.is_enable())
                        {
					        // packet structure : <firm recno> <unit Id> <amount>
					        short *shortPtr = (short *)remote.new_send_queue_msg(MSG_F_HARBOR_BUILD_SHIP, 3*sizeof(short) );
					        shortPtr[0] = firm_recno;
					        shortPtr[1] = unitId;
					        shortPtr[2] = createRemoveAmount;
                        }*/
                        //else
                        //{
                            harbor.AddQueue(unitId);
                        //}
                        SECtrl.immediate_sound("TURN_ON");
                    }

                    if (_rightMouseReleased)
                    {
                        /*if (remote.is_enable())
                        {
					        // packet structure : <firm recno> <unit Id> <amount>
					        short *shortPtr = (short *)remote.new_send_queue_msg(MSG_F_HARBOR_BUILD_SHIP, 3*sizeof(short) );
					        shortPtr[0] = firm_recno;
					        shortPtr[1] = -unitId;
					        shortPtr[2] = createRemoveAmount;
                        }*/
                        //else
                        //{
                            harbor.RemoveQueue(unitId);
                        //}
                        SECtrl.immediate_sound("TURN_OFF");
                    }

                    if (mouseOnBuildButton)
                        FirmDetailsMode = FirmDetailsMode.Normal;
                }
                else
                {
                    if (_leftMouseReleased)
                    {
                        SECtrl.immediate_sound("TURN_OFF");
                        FirmDetailsMode = FirmDetailsMode.Normal;
                    }
                }
            }

            if (!onCancelButton)
                shownItems++;
            else
                break;
            
            dy += 70;
        }
    }
}