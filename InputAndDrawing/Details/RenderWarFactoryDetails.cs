using System.Collections.Generic;

namespace TenKingdoms;

public partial class Renderer
{
    //TODO support batch build with shift
    
    private int BuildWeaponPanelX => DetailsX1 + 2;
    private int BuildWeaponPanelY => DetailsY1 + 2;
    private int BuildWeaponButtonNumberX => DetailsX1 + 351;
    private int BuildWeaponButtonNumberY => DetailsY1 + 12;
    private int MouseOnBuildWeaponButtonX1 => DetailsX1 + 8;
    private int MouseOnBuildWeaponButtonX2 => DetailsX1 + 320;
    private int MouseOnBuildWeaponButtonY1 => DetailsY1 + 2;
    private int MouseOnBuildWeaponButtonY2 => DetailsY1 + 66;
    private int MouseOnBuildWeaponNumberButtonX1 => BuildWeaponButtonNumberX + 4;
    private int MouseOnBuildWeaponNumberButtonX2 => BuildWeaponButtonNumberX + 40;
    private int MouseOnBuildWeaponNumberButtonY1 => BuildWeaponButtonNumberY + 4;
    private int MouseOnBuildWeaponNumberButtonY2 => BuildWeaponButtonNumberY + 42;
    private int MaxBuildWeaponItems = 6;

    public void DrawWarFactoryDetails(FirmWar warFactory)
    {
        if (FirmDetailsMode == FirmDetailsMode.WarMachine)
        {
            DrawWarFactoryMenu(warFactory);
            return;
        }
        
        DrawResearchWarFactoryPanel(DetailsX1 + 2, DetailsY1 + 96);
        
        if (warFactory.BuildUnitType != 0)
        {
            UnitInfo unitInfo = UnitRes[warFactory.BuildUnitType];
            Graphics.DrawBitmap(unitInfo.GetLargeIconTexture(Graphics, Unit.RANK_SOLDIER), DetailsX1 + 6, DetailsY1 + 100,
                unitInfo.SoldierIconWidth * 2, unitInfo.SoldierIconHeight * 2);

            Nation warFactoryNation = NationArray[warFactory.NationId];
            string description = unitInfo.Name;
            int techLevel = warFactoryNation.GetTechLevelByUnitType(warFactory.BuildUnitType);
            if (techLevel > 1)
                description += " " + Misc.RomanNumber(techLevel);
            PutText(FontSan, description, DetailsX1 + 120, DetailsY1 + 107);
            //TODO draw indicator
            PutText(FontSan, (warFactory.BuildProgressInDays / unitInfo.BuildDays).ToString("0.00"), DetailsX1 + 120, DetailsY1 + 137);
            //TODO cancel weapon button
        }

        DrawWorkers(warFactory);
        DrawWorkerDetails(warFactory, "Manufacture");
        
        if (warFactory.OwnFirm())
        {
            bool mouseOnButton = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                                 _mouseButtonY >= ButtonsWarFactoryY + 2 && _mouseButtonY <= ButtonsWarFactoryY + ButtonHeight;
            if (_leftMousePressed && mouseOnButton)
                Graphics.DrawBitmapScaled(_buttonDownTexture, Button1X, ButtonsWarFactoryY, _buttonDownWidth, _buttonDownHeight);
            else
                Graphics.DrawBitmapScaled(_buttonUpTexture, Button1X, ButtonsWarFactoryY, _buttonUpWidth, _buttonUpHeight);
            Graphics.DrawBitmapScaled(_buttonBuildWeaponTexture, Button1X + 7, ButtonsWarFactoryY + 3, _buttonBuildWeaponWidth, _buttonBuildWeaponHeight);

            if (warFactory.HaveOwnWorkers())
            {
                mouseOnButton = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                                _mouseButtonY >= ButtonsWarFactoryY + 2 && _mouseButtonY <= ButtonsWarFactoryY + ButtonHeight;
                if (_leftMousePressed && mouseOnButton)
                    Graphics.DrawBitmapScaled(_buttonDownTexture, Button2X, ButtonsWarFactoryY, _buttonDownWidth, _buttonDownHeight);
                else
                    Graphics.DrawBitmapScaled(_buttonUpTexture, Button2X, ButtonsWarFactoryY, _buttonUpWidth, _buttonUpHeight);
                Graphics.DrawBitmapScaled(_buttonRecruitTexture, Button2X + 3, ButtonsWarFactoryY + 7, _buttonRecruitWidth, _buttonRecruitHeight);
            }
            else
            {
                Graphics.DrawBitmapScaled(_buttonDisabledTexture, Button2X, ButtonsWarFactoryY, _buttonDisabledWidth, _buttonDisabledHeight);
                Graphics.DrawBitmapScaled(_buttonRecruitDisabledTexture, Button2X + 3, ButtonsWarFactoryY + 7, _buttonRecruitWidth, _buttonRecruitHeight);
            }
        }
        
        if (IsFirmSpyListEnabled(warFactory))
        {
            bool mouseOnButton = _mouseButtonX >= Button3X + 2 && _mouseButtonX <= Button3X + ButtonWidth &&
                                 _mouseButtonY >= ButtonsWarFactoryY + 2 && _mouseButtonY <= ButtonsWarFactoryY + ButtonHeight;
            if (_leftMousePressed && mouseOnButton)
                Graphics.DrawBitmapScaled(_buttonDownTexture, Button3X, ButtonsWarFactoryY, _buttonDownWidth, _buttonDownHeight);
            else
                Graphics.DrawBitmapScaled(_buttonUpTexture, Button3X, ButtonsWarFactoryY, _buttonUpWidth, _buttonUpHeight);
            Graphics.DrawBitmapScaled(_buttonSpyMenuTexture, Button3X + 4, ButtonsWarFactoryY + 16, _buttonSpyMenuWidth, _buttonSpyMenuHeight);
        }
        
        // TODO display and capture button
    }

    private void DrawWarFactoryMenu(FirmWar warFactory)
    {
        Dictionary<int, int> buildUnitCounts = new Dictionary<int, int>();
        for (int unitType = 1; unitType <= UnitConstants.MAX_UNIT_TYPE; unitType++)
        {
            UnitInfo unitInfo = UnitRes[unitType];
            if (unitInfo.UnitClass == UnitConstants.UNIT_CLASS_WEAPON)
                buildUnitCounts.Add(unitType, 0);
        }

        foreach (int buildUnitType in warFactory.BuildQueue)
        {
            buildUnitCounts[buildUnitType]++;
        }

        if (warFactory.BuildUnitType != 0)
        {
            buildUnitCounts[warFactory.BuildUnitType]++;
        }

        Nation warFactoryNation = NationArray[warFactory.NationId];
        int shownItems = 0;
        int dy = 0;
        for (int unitType = 1; unitType <= UnitConstants.MAX_UNIT_TYPE + 1; unitType++)
        {
            bool showCancelButton = (shownItems == MaxBuildWeaponItems || unitType > UnitConstants.MAX_UNIT_TYPE);
            
            UnitInfo unitInfo = !showCancelButton ? UnitRes[unitType] : null;
            if (!showCancelButton && (unitInfo.UnitClass != UnitConstants.UNIT_CLASS_WEAPON || warFactoryNation.GetTechLevelByUnitType(unitType) == 0))
                continue;

            //TODO Done button is not pressed when you press it close to the right edge
            bool mouseOnButton = _mouseButtonX >= MouseOnBuildWeaponButtonX1 && _mouseButtonX <= MouseOnBuildWeaponButtonX2 &&
                                 _mouseButtonY >= MouseOnBuildWeaponButtonY1 + dy && _mouseButtonY <= MouseOnBuildWeaponButtonY2 + dy;
            if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
                DrawResearchBuildWeaponPanelDown(BuildWeaponPanelX, BuildWeaponPanelY + dy);
            else
                DrawResearchBuildWeaponPanelUp(BuildWeaponPanelX, BuildWeaponPanelY + dy);

            if (!showCancelButton)
            {
                Graphics.DrawBitmap(unitInfo.GetLargeIconTexture(Graphics, Unit.RANK_SOLDIER), BuildWeaponPanelX + 4, BuildWeaponPanelY + dy + 4,
                    unitInfo.SoldierIconWidth * 3 / 2, unitInfo.SoldierIconHeight * 3 / 2);
                
                string description = unitInfo.Name;
                int techLevel = warFactoryNation.GetTechLevelByUnitType(unitType);
                if (techLevel > 1)
                    description += " " + Misc.RomanNumber(techLevel);
                PutText(FontBible, description, BuildWeaponPanelX + 96, BuildWeaponPanelY + dy + 10);

                mouseOnButton = _mouseButtonX >= MouseOnBuildWeaponNumberButtonX1 && _mouseButtonX <= MouseOnBuildWeaponNumberButtonX2 &&
                                _mouseButtonY >= MouseOnBuildWeaponNumberButtonY1 + dy && _mouseButtonY <= MouseOnBuildWeaponNumberButtonY2 + dy;
                if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
                    DrawNumberPanelDown(BuildWeaponButtonNumberX, BuildWeaponButtonNumberY + dy);
                else
                    DrawNumberPanelUp(BuildWeaponButtonNumberX, BuildWeaponButtonNumberY + dy);
                PutTextCenter(FontBible, buildUnitCounts[unitType].ToString(),
                    BuildWeaponButtonNumberX, BuildWeaponButtonNumberY + dy, BuildWeaponButtonNumberX + 45, BuildWeaponButtonNumberY + 40 + dy);
                
                shownItems++;
            }
            else
            {
                PutText(FontBible, "Done", BuildWeaponPanelX + 158, BuildWeaponPanelY + dy + 10);
                break;
            }
            
            dy += 70;
        }
    }

    public void HandleWarFactoryDetailsInput(FirmWar warFactory)
    {
        if (FirmDetailsMode == FirmDetailsMode.Spy)
        {
            HandleSpyList(warFactory.NationId, warFactory.GetPlayerSpies());
            return;
        }

        if (!warFactory.OwnFirm())
            return;

        if (FirmDetailsMode == FirmDetailsMode.WarMachine)
        {
            HandleWarFactoryMenu(warFactory);
            return;
        }

        bool mouseOnButton1 = _mouseButtonX >= Button1X + 2 && _mouseButtonX <= Button1X + ButtonWidth &&
                              _mouseButtonY >= ButtonsWarFactoryY + 2 && _mouseButtonY <= ButtonsWarFactoryY + ButtonHeight;
        bool mouseOnButton2 = _mouseButtonX >= Button2X + 2 && _mouseButtonX <= Button2X + ButtonWidth &&
                              _mouseButtonY >= ButtonsWarFactoryY + 2 && _mouseButtonY <= ButtonsWarFactoryY + ButtonHeight;
        bool mouseOnButton3 = _mouseButtonX >= Button3X + 2 && _mouseButtonX <= Button3X + ButtonWidth &&
                              _mouseButtonY >= ButtonsWarFactoryY + 2 && _mouseButtonY <= ButtonsWarFactoryY + ButtonHeight;

        if (_leftMouseReleased && mouseOnButton1)
        {
            FirmDetailsMode = FirmDetailsMode.WarMachine;
        }

        if (_leftMouseReleased && mouseOnButton2 && warFactory.HaveOwnWorkers())
        {
            warFactory.MobilizeAllWorkers(InternalConstants.COMMAND_PLAYER);
        }
        
        if (_leftMouseReleased && mouseOnButton3)
        {
            FirmDetailsMode = FirmDetailsMode.Spy;
        }
    }

    private void HandleWarFactoryMenu(FirmWar warFactory)
    {
        Nation warFactoryNation = NationArray[warFactory.NationId];
        int shownItems = 0;
        int dy = 0;
        for (int unitType = 1; unitType <= UnitConstants.MAX_UNIT_TYPE + 1; unitType++)
        {
            bool onCancelButton = (shownItems == MaxBuildWeaponItems || unitType > UnitConstants.MAX_UNIT_TYPE);
            
            UnitInfo unitInfo = !onCancelButton ? UnitRes[unitType] : null;
            if (!onCancelButton && (unitInfo.UnitClass != UnitConstants.UNIT_CLASS_WEAPON || warFactoryNation.GetTechLevelByUnitType(unitType) == 0))
                continue;

            bool mouseOnBuildButton = _mouseButtonX >= MouseOnBuildWeaponButtonX1 && _mouseButtonX <= MouseOnBuildWeaponButtonX2 &&
                                      _mouseButtonY >= MouseOnBuildWeaponButtonY1 + dy && _mouseButtonY <= MouseOnBuildWeaponButtonY2 + dy;
            bool mouseOnBuildNumberButton = _mouseButtonX >= MouseOnBuildWeaponNumberButtonX1 && _mouseButtonX <= MouseOnBuildWeaponNumberButtonX2 &&
                                            _mouseButtonY >= MouseOnBuildWeaponNumberButtonY1 + dy && _mouseButtonY <= MouseOnBuildWeaponNumberButtonY2 + dy;
            
            if ((mouseOnBuildButton || mouseOnBuildNumberButton) && (_leftMouseReleased || _rightMouseReleased))
            {
                if (!onCancelButton)
                {
                    if (_leftMouseReleased)
                    {
                        /*if (remote.is_enable())
                        {
                            // packet structure : <firm recno> <unit Id>
                            short *shortPtr = (short *)remote.new_send_queue_msg(MSG_F_WAR_BUILD_WEAPON, 3*sizeof(short) );
                            shortPtr[0] = firm_recno;
                            shortPtr[1] = unitId;
                            shortPtr[2] = (short)createRemoveAmount;
                        }*/
                        //else
                        //{
                            warFactory.AddQueue(unitType);
                        //}
                        SECtrl.immediate_sound("TURN_ON");
                    }

                    if (_rightMouseReleased)
                    {
                        /*if (remote.is_enable())
                        {
					        // packet structure : <firm recno> <unit Id>
					        short *shortPtr = (short *)remote.new_send_queue_msg(MSG_F_WAR_CANCEL_WEAPON, 3*sizeof(short) );
					        shortPtr[0] = firm_recno;
					        shortPtr[1] = unitId;
					        shortPtr[2] = (short)createRemoveAmount;
                        }*/
                        //else
                        //{
                            warFactory.RemoveQueue(unitType);
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