using System.Collections.Generic;

namespace TenKingdoms;

public partial class Renderer
{
    //TODO support batch build with shift
    
    private const int BuildWeaponPanelX = DetailsX1 + 2;
    private const int BuildWeaponPanelY = DetailsY1 + 2;
    private const int BuildButtonNumberX = DetailsX1 + 351;
    private const int BuildButtonNumberY = DetailsY1 + 12;
    private const int MouseOnBuildWeaponButtonX1 = DetailsX1 + 8;
    private const int MouseOnBuildWeaponButtonX2 = DetailsX1 + 320;
    private const int MouseOnBuildWeaponButtonY1 = DetailsY1 + 2;
    private const int MouseOnBuildWeaponButtonY2 = DetailsY1 + 66;
    private const int MouseOnBuildNumberButtonX1 = BuildButtonNumberX + 4;
    private const int MouseOnBuildNumberButtonX2 = BuildButtonNumberX + 40;
    private const int MouseOnBuildNumberButtonY1 = BuildButtonNumberY + 4;
    private const int MouseOnBuildNumberButtonY2 = BuildButtonNumberY + 42;
    private const int MaxBuildWeaponItems = 6;

    public void DrawWarFactoryDetails(FirmWar warFactory)
    {
        if (FirmDetailsMode == FirmDetailsMode.WarMachine)
        {
            DrawWarFactoryMenu(warFactory);
            return;
        }
        
        DrawResearchWarFactoryPanel(DetailsX1 + 2, DetailsY1 + 96);
        
        if (warFactory.BuildUnitId != 0)
        {
            UnitInfo unitInfo = UnitRes[warFactory.BuildUnitId];
            Graphics.DrawBitmap(unitInfo.GetLargeIconTexture(Graphics, Unit.RANK_SOLDIER), DetailsX1 + 6, DetailsY1 + 100,
                unitInfo.soldierIconWidth * 2, unitInfo.soldierIconHeight * 2);

            string description = unitInfo.name;
            int techLevel = unitInfo.get_nation_tech_level(warFactory.NationId);
            if (techLevel > 1)
                description += " " + Misc.roman_number(techLevel);
            PutText(FontSan, description, DetailsX1 + 120, DetailsY1 + 107);
            //TODO draw indicator
            PutText(FontSan, (warFactory.BuildProgressInDays / unitInfo.build_days).ToString("0.00"), DetailsX1 + 120, DetailsY1 + 137);
            //TODO cancel weapon button
        }

        DrawWorkers(warFactory);
        
        DrawPanelWithTwoFields(DetailsX1 + 2, DetailsY1 + 294);
        DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 299);
        DrawFieldPanel67(DetailsX1 + 7, DetailsY1 + 328);
        DrawFieldPanel75(DetailsX1 + 208, DetailsY1 + 328);
        PutText(FontSan, "Residence", DetailsX1 + 13, DetailsY1 + 302, -1, true);
        PutText(FontSan, "Loyalty", DetailsX1 + 13, DetailsY1 + 331, -1, true);
        PutText(FontSan, "Manufacture", DetailsX1 + 214, DetailsY1 + 331, -1, true);
        if (warFactory.SelectedWorkerId != 0)
        {
            Worker worker = warFactory.Workers[warFactory.SelectedWorkerId - 1];
            PutText(FontSan, TownArray[worker.TownId].Name, DetailsX1 + 113, DetailsY1 + 304, -1, true);
            PutText(FontSan, worker.Loyalty().ToString(), DetailsX1 + 113, DetailsY1 + 333, -1, true);
            PutText(FontSan, worker.SkillLevel.ToString(), DetailsX1 + 327, DetailsY1 + 333, -1, true);
        }
        
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
        
        // TODO display spy list and capture buttons
    }

    private void DrawWarFactoryMenu(FirmWar warFactory)
    {
        Dictionary<int, int> buildUnitCounts = new Dictionary<int, int>();
        for (int unitId = 1; unitId <= UnitConstants.MAX_UNIT_TYPE; unitId++)
        {
            UnitInfo unitInfo = UnitRes[unitId];
            if (unitInfo.unit_class == UnitConstants.UNIT_CLASS_WEAPON)
                buildUnitCounts.Add(unitId, 0);
        }

        foreach (int buildUnitId in warFactory.BuildQueue)
        {
            buildUnitCounts[buildUnitId]++;
        }

        if (warFactory.BuildUnitId != 0)
        {
            buildUnitCounts[warFactory.BuildUnitId]++;
        }

        int shownItems = 0;
        int dy = 0;
        for (int unitId = 1; unitId <= UnitConstants.MAX_UNIT_TYPE + 1; unitId++)
        {
            bool showCancelButton = (shownItems == MaxBuildWeaponItems || unitId > UnitConstants.MAX_UNIT_TYPE);
            
            UnitInfo unitInfo = !showCancelButton ? UnitRes[unitId] : null;
            if (!showCancelButton && (unitInfo.unit_class != UnitConstants.UNIT_CLASS_WEAPON || unitInfo.get_nation_tech_level(warFactory.NationId) == 0))
                continue;

            bool mouseOnButton = _mouseButtonX >= MouseOnBuildWeaponButtonX1 && _mouseButtonX <= MouseOnBuildWeaponButtonX2 &&
                                 _mouseButtonY >= MouseOnBuildWeaponButtonY1 + dy && _mouseButtonY <= MouseOnBuildWeaponButtonY2 + dy;
            if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
                DrawResearchBuildWeaponPanelDown(BuildWeaponPanelX, BuildWeaponPanelY + dy);
            else
                DrawResearchBuildWeaponPanelUp(BuildWeaponPanelX, BuildWeaponPanelY + dy);

            if (!showCancelButton)
            {
                Graphics.DrawBitmap(unitInfo.GetLargeIconTexture(Graphics, Unit.RANK_SOLDIER), BuildWeaponPanelX + 4, BuildWeaponPanelY + dy + 4,
                    unitInfo.soldierIconWidth * 3 / 2, unitInfo.soldierIconHeight * 3 / 2);
                
                string description = unitInfo.name;
                int techLevel = unitInfo.get_nation_tech_level(warFactory.NationId);
                if (techLevel > 1)
                    description += " " + Misc.roman_number(techLevel);
                PutText(FontBible, description, BuildWeaponPanelX + 96, BuildWeaponPanelY + dy + 10);

                mouseOnButton = _mouseButtonX >= MouseOnBuildNumberButtonX1 && _mouseButtonX <= MouseOnBuildNumberButtonX2 &&
                                _mouseButtonY >= MouseOnBuildNumberButtonY1 + dy && _mouseButtonY <= MouseOnBuildNumberButtonY2 + dy;
                if ((_leftMousePressed || _rightMousePressed) && mouseOnButton)
                    DrawNumberPanelDown(BuildButtonNumberX, BuildButtonNumberY + dy);
                else
                    DrawNumberPanelUp(BuildButtonNumberX, BuildButtonNumberY + dy);
                PutTextCenter(FontBible, buildUnitCounts[unitId].ToString(),
                    BuildButtonNumberX, BuildButtonNumberY + dy, BuildButtonNumberX + 45, BuildButtonNumberY + 40 + dy);
                
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

        if (_leftMouseReleased && mouseOnButton1)
        {
            FirmDetailsMode = FirmDetailsMode.WarMachine;
        }

        if (_leftMouseReleased && mouseOnButton2 && warFactory.HaveOwnWorkers())
        {
            warFactory.MobilizeAllWorkers(InternalConstants.COMMAND_PLAYER);
        }
    }

    private void HandleWarFactoryMenu(FirmWar warFactory)
    {
        int shownItems = 0;
        int dy = 0;
        for (int unitId = 1; unitId <= UnitConstants.MAX_UNIT_TYPE + 1; unitId++)
        {
            bool onCancelButton = (shownItems == MaxBuildWeaponItems || unitId > UnitConstants.MAX_UNIT_TYPE);
            
            UnitInfo unitInfo = !onCancelButton ? UnitRes[unitId] : null;
            if (!onCancelButton && (unitInfo.unit_class != UnitConstants.UNIT_CLASS_WEAPON || unitInfo.get_nation_tech_level(warFactory.NationId) == 0))
                continue;

            bool mouseOnBuildButton = _mouseButtonX >= MouseOnBuildWeaponButtonX1 && _mouseButtonX <= MouseOnBuildWeaponButtonX2 &&
                                      _mouseButtonY >= MouseOnBuildWeaponButtonY1 + dy && _mouseButtonY <= MouseOnBuildWeaponButtonY2 + dy;
            bool mouseOnBuildNumberButton = _mouseButtonX >= MouseOnBuildNumberButtonX1 && _mouseButtonX <= MouseOnBuildNumberButtonX2 &&
                                            _mouseButtonY >= MouseOnBuildNumberButtonY1 + dy && _mouseButtonY <= MouseOnBuildNumberButtonY2 + dy;
            
            if (mouseOnBuildButton || mouseOnBuildNumberButton)
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
                            warFactory.AddQueue(unitId);
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
                            warFactory.RemoveQueue(unitId);
                        //}
                        SECtrl.immediate_sound("TURN_OFF");
                    }

                    if (mouseOnBuildButton)
                        FirmDetailsMode = FirmDetailsMode.Normal;
                }
                else
                {
                    SECtrl.immediate_sound("TURN_OFF");
                    FirmDetailsMode = FirmDetailsMode.Normal;
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