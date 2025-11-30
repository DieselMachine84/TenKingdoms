using System;
using System.Collections.Generic;
using System.Linq;

namespace TenKingdoms;

public partial class Renderer
{
	private const int RaceHeight = 56;
	private const int ButtonWidth = 66;
	private const int ButtonHeight = 56;
	private const int Button1X = DetailsX1 + 2;
	private const int Button2X = DetailsX1 + 85;
	private const int Button3X = DetailsX1 + 168;
	private const int Button4X = DetailsX1 + 251;
	private const int Button5X = DetailsX1 + 334;
	private const int ButtonsTownY = DetailsY1 + 385;
	private const int ButtonsMineY = DetailsY1 + 412;
	private const int ButtonsFactoryY = DetailsY1 + 412;
	private const int ButtonsCampY = DetailsY1 + 376;
	private const int ButtonsUnitHuman1Y = DetailsY1 + 245;
	private const int ButtonsUnitHuman2Y = DetailsY1 + 315;

	private readonly Dictionary<int, IntPtr> _colorSquareTextures = new Dictionary<int, nint>();
	private int _colorSquareWidth;
	private int _colorSquareHeight;
	private readonly Dictionary<string, IntPtr> _buildButtonTextures = new Dictionary<string, nint>();
	private int _buildButtonWidth;
	private int _buildButtonHeight;
	private IntPtr _gameMenuTexture1;
	private int _gameMenuTexture1Width;
	private int _gameMenuTexture1Height;
	private IntPtr _gameMenuTexture2;
	private int _gameMenuTexture2Width;
	private int _gameMenuTexture2Height;
	private IntPtr _gameMenuTexture3;
	private int _gameMenuTexture3Width;
	private int _gameMenuTexture3Height;
	
	private IntPtr _detailsTexture1;
	private int _detailsTexture1Width;
	private int _detailsTexture1Height;
	private IntPtr _detailsTexture2;
	private int _detailsTexture2Width;
	private int _detailsTexture2Height;
	private IntPtr _detailsTexture3;
	private int _detailsTexture3Width;
	private int _detailsTexture3Height;
	private IntPtr _detailsTexture4;
	private int _detailsTexture4Width;
	private int _detailsTexture4Height;

	private IntPtr _middleBorder1Texture;
	private int _middleBorder1TextureWidth;
	private int _middleBorder1TextureHeight;
	private IntPtr _middleBorder2Texture;
	private int _middleBorder2TextureWidth;
	private int _middleBorder2TextureHeight;
	private IntPtr _rightBorder1Texture;
	private int _rightBorder1TextureWidth;
	private int _rightBorder1TextureHeight;
	private IntPtr _rightBorder2Texture;
	private int _rightBorder2TextureWidth;
	private int _rightBorder2TextureHeight;
	private IntPtr _miniMapBorder1Texture;
	private int _miniMapBorder1TextureWidth;
	private int _miniMapBorder1TextureHeight;
	private IntPtr _miniMapBorder2Texture;
	private int _miniMapBorder2TextureWidth;
	private int _miniMapBorder2TextureHeight;
	private IntPtr _bottomBorder1Texture;
	private int _bottomBorder1TextureWidth;
	private int _bottomBorder1TextureHeight;
	private IntPtr _bottomBorder2Texture;
	private int _bottomBorder2TextureWidth;
	private int _bottomBorder2TextureHeight;

	private IntPtr _smallPanelTexture;
	private int _smallPanelWidth;
	private int _smallPanelHeight;
	private IntPtr _overseerPanelTexture;
	private int _overseerPanelWidth;
	private int _overseerPanelHeight;
	private IntPtr _workersPanelTexture;
	private int _workersPanelWidth;
	private int _workersPanelHeight;
	private IntPtr _mineFactoryPanelTexture;
	private int _mineFactoryPanelWidth;
	private int _mineFactoryPanelHeight;
	private IntPtr _unitPanelTexture;
	private int _unitPanelWidth;
	private int _unitPanelHeight;
	private IntPtr _panelWithOneFieldTexture;
	private int _panelWithOneFieldWidth;
	private int _panelWithOneFieldHeight;
	private IntPtr _panelWithTwoFieldsTexture;
	private int _panelWithTwoFieldsWidth;
	private int _panelWithTwoFieldsHeight;
	private IntPtr _panelWithThreeFieldsTexture;
	private int _panelWithThreeFieldsWidth;
	private int _panelWithThreeFieldsHeight;
	private IntPtr _fieldPanel62Texture;
	private int _fieldPanel62Width;
	private int _fieldPanel62Height;
	private IntPtr _fieldPanel67Texture;
	private int _fieldPanel67Width;
	private int _fieldPanel67Height;
	private IntPtr _fieldPanel75Texture;
	private int _fieldPanel75Width;
	private int _fieldPanel75Height;
	private IntPtr _fieldPanel111Texture;
	private int _fieldPanel111Width;
	private int _fieldPanel111Height;
	private IntPtr _fieldPanel119Texture;
	private int _fieldPanel119Width;
	private int _fieldPanel119Height;
	private IntPtr _cancelPanelUpTexture;
	private IntPtr _cancelPanelDownTexture;
	private int _cancelPanelWidth;
	private int _cancelPanelHeight;
	private IntPtr _skillPanelUpTexture;
	private IntPtr _skillPanelDownTexture;
	private int _skillPanelWidth;
	private int _skillPanelHeight;
	private IntPtr _numberPanelUpTexture;
	private IntPtr _numberPanelDownTexture;
	private int _numberPanelWidth;
	private int _numberPanelHeight;

	private IntPtr _listBoxPanelTexture;
	private int _listBoxPanelWidth;
	private int _listBoxPanelHeight;
	private IntPtr _listBoxPanelWithScrollTexture;
	private int _listBoxPanelWithScrollWidth;
	private int _listBoxPanelWithScrollHeight;
	private IntPtr _listBoxScrollPanelTexture;
	private int _listBoxScrollPanelWidth;
	private int _listBoxScrollPanelHeight;

	private IntPtr _arrowUpTexture;
	private int _arrowUpWidth;
	private int _arrowUpHeight;
	private IntPtr _arrowDownTexture;
	private int _arrowDownWidth;
	private int _arrowDownHeight;
	private IntPtr _kingTexture;
	private IntPtr _generalTexture;
	private IntPtr _constructionTexture;
	private IntPtr _leadershipTexture;
	private IntPtr _miningTexture;
	private IntPtr _manufactureTexture;
	private IntPtr _researchTexture;
	private IntPtr _spyingTexture;
	private IntPtr _prayingTexture;
	private int _skillWidth;
	private int _skillHeight;

	private IntPtr _buttonUpTexture;
	private int _buttonUpWidth;
	private int _buttonUpHeight;
	private IntPtr _buttonDownTexture;
	private int _buttonDownWidth;
	private int _buttonDownHeight;
	private IntPtr _buttonDisabledTexture;
	private int _buttonDisabledWidth;
	private int _buttonDisabledHeight;
	private IntPtr _buttonCancelTexture;
	private int _buttonCancelWidth;
	private int _buttonCancelHeight;
	private IntPtr _buttonBuildDownTexture;
	private int _buttonBuildDownWidth;
	private int _buttonBuildDownHeight;
	private IntPtr _buttonRecruitTexture;
	private IntPtr _buttonRecruitDisabledTexture;
	private int _buttonRecruitWidth;
	private int _buttonRecruitHeight;
	private IntPtr _buttonTrainTexture;
	private IntPtr _buttonTrainDisabledTexture;
	private int _buttonTrainWidth;
	private int _buttonTrainHeight;
	private IntPtr _buttonCollectTaxTexture;
	private IntPtr _buttonCollectTaxDisabledTexture;
	private int _buttonCollectTaxWidth;
	private int _buttonCollectTaxHeight;
	private IntPtr _buttonGrantTexture;
	private IntPtr _buttonGrantDisabledTexture;
	private int _buttonGrantWidth;
	private int _buttonGrantHeight;
	private IntPtr _buttonPatrolTexture;
	private IntPtr _buttonPatrolDisabledTexture;
	private int _buttonPatrolWidth;
	private int _buttonPatrolHeight;
	private IntPtr _buttonChangeProductionTexture;
	private int _buttonChangeProductionWidth;
	private int _buttonChangeProductionHeight;
	private IntPtr _buttonSucceedKingTexture;
	private int _buttonSucceedKingWidth;
	private int _buttonSucceedKingHeight;
	private IntPtr _buttonAggressionOffTexture;
	private int _buttonAggressionOffWidth;
	private int _buttonAggressionOffHeight;
	private IntPtr _buttonAggressionOnTexture;
	private int _buttonAggressionOnWidth;
	private int _buttonAggressionOnHeight;
	private IntPtr _buttonSettleTexture;
	private int _buttonSettleWidth;
	private int _buttonSettleHeight;
	private IntPtr _buttonBuildTexture;
	private int _buttonBuildWidth;
	private int _buttonBuildHeight;
	private IntPtr _buttonPromoteTexture;
	private int _buttonPromoteWidth;
	private int _buttonPromoteHeight;
	private IntPtr _buttonDemoteTexture;
	private int _buttonDemoteWidth;
	private int _buttonDemoteHeight;
	private IntPtr _buttonReturnToCampTexture;
	private int _buttonReturnToCampWidth;
	private int _buttonReturnToCampHeight;
	private IntPtr _buttonSpyNotifyOnTexture;
	private int _buttonSpyNotifyOnWidth;
	private int _buttonSpyNotifyOnHeight;
	private IntPtr _buttonSpyNotifyOffTexture;
	private int _buttonSpyNotifyOffWidth;
	private int _buttonSpyNotifyOffHeight;
	private IntPtr _buttonDropSpyIdentityTexture;
	private int _buttonDropSpyIdentityWidth;
	private int _buttonDropSpyIdentityHeight;

	private IntPtr _buttonSpyMenuTexture;
	private int _buttonSpyMenuWidth;
	private int _buttonSpyMenuHeight;
	private IntPtr _buttonRewardTexture;
	private IntPtr _buttonRewardDisabledTexture;
	private int _buttonRewardWidth;
	private int _buttonRewardHeight;
	private IntPtr _buttonDefenseOnTexture;
	private int _buttonDefenseOnWidth;
	private int _buttonDefenseOnHeight;
	private IntPtr _buttonDefenseOffTexture;
	private int _buttonDefenseOffWidth;
	private int _buttonDefenseOffHeight;

	private IntPtr _buttonConstructionSkillTexture;
	private int _buttonConstructionSkillWidth;
	private int _buttonConstructionSkillHeight;
	private IntPtr _buttonLeadershipSkillTexture;
	private int _buttonLeadershipSkillWidth;
	private int _buttonLeadershipSkillHeight;
	private IntPtr _buttonMineSkillTexture;
	private int _buttonMineSkillWidth;
	private int _buttonMineSkillHeight;
	private IntPtr _buttonManufactureSkillTexture;
	private int _buttonManufactureSkillWidth;
	private int _buttonManufactureSkillHeight;
	private IntPtr _buttonResearchSkillTexture;
	private int _buttonResearchSkillWidth;
	private int _buttonResearchSkillHeight;
	private IntPtr _buttonSpySkillTexture;
	private int _buttonSpySkillWidth;
	private int _buttonSpySkillHeight;

	private IntPtr _buttonRepairUpTexture;
	private int _buttonRepairUpTextureWidth;
	private int _buttonRepairUpTextureHeight;
	private IntPtr _buttonRepairDownTexture;
	private int _buttonRepairDownTextureWidth;
	private int _buttonRepairDownTextureHeight;
	private IntPtr _buttonRequestRepairUpTexture;
	private int _buttonRequestRepairUpTextureWidth;
	private int _buttonRequestRepairUpTextureHeight;
	private IntPtr _buttonRequestRepairDownTexture;
	private int _buttonRequestRepairDownTextureWidth;
	private int _buttonRequestRepairDownTextureHeight;
	private IntPtr _buttonSellUpTexture;
	private int _buttonSellUpTextureWidth;
	private int _buttonSellUpTextureHeight;
	private IntPtr _buttonSellDownTexture;
	private int _buttonSellDownTextureWidth;
	private int _buttonSellDownTextureHeight;
	private IntPtr _buttonDestructUpTexture;
	private int _buttonDestructUpTextureWidth;
	private int _buttonDestructUpTextureHeight;
	private IntPtr _buttonDestructDownTexture;
	private int _buttonDestructDownTextureWidth;
	private int _buttonDestructDownTextureHeight;
	
	private void CreateUITextures()
	{
        ResourceIdx buttonImages = new ResourceIdx($"{Sys.GameDataFolder}/Resource/I_BUTTON.RES");
        byte[] colorSquare = buttonImages.Read("V_COLCOD");
        _colorSquareWidth = BitConverter.ToInt16(colorSquare, 0);
        _colorSquareHeight = BitConverter.ToInt16(colorSquare, 2);
        byte[] colorSquareBitmap = colorSquare.Skip(4).ToArray();
        for (int i = 0; i <= InternalConstants.MAX_COLOR_SCHEME; i++)
        {
            int textureKey = ColorRemap.GetTextureKey(i, false);
            byte[] decompressedBitmap = Graphics.DecompressTransparentBitmap(colorSquareBitmap, _colorSquareWidth, _colorSquareHeight,
                ColorRemap.GetColorRemap(i, false).ColorTable);
            _colorSquareTextures.Add(textureKey, Graphics.CreateTextureFromBmp(decompressedBitmap, _colorSquareWidth, _colorSquareHeight));
        }

        ResourceIdx interfaceImages = new ResourceIdx($"{Sys.GameDataFolder}/Resource/I_IF.RES");
        byte[] mainScreenBitmap = interfaceImages.Read("MAINSCR");
        int mainScreenWidth = BitConverter.ToInt16(mainScreenBitmap, 0);
        int mainScreenHeight = BitConverter.ToInt16(mainScreenBitmap, 2);
        mainScreenBitmap = mainScreenBitmap.Skip(4).ToArray();

        _gameMenuTexture1Width = 306;
        _gameMenuTexture1Height = 56;
        byte[] gameMenu1Bitmap = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
	        0, 0, _gameMenuTexture1Width, _gameMenuTexture1Height);
        _gameMenuTexture1 = Graphics.CreateTextureFromBmp(gameMenu1Bitmap, _gameMenuTexture1Width, _gameMenuTexture1Height);
        _gameMenuTexture2Width = 270;
        _gameMenuTexture2Height = 56;
        byte[] gameMenu2Bitmap = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
	        _gameMenuTexture1Width, 0, _gameMenuTexture2Width, _gameMenuTexture2Height);
        _gameMenuTexture2 = Graphics.CreateTextureFromBmp(gameMenu2Bitmap, _gameMenuTexture2Width, _gameMenuTexture2Height);
        _gameMenuTexture3Width = 208;
        _gameMenuTexture3Height = 56;
        byte[] gameMenu3Bitmap = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
	        mainScreenWidth - 8 - _gameMenuTexture3Width, 0, _gameMenuTexture3Width, _gameMenuTexture3Height);
        _gameMenuTexture3 = Graphics.CreateTextureFromBmp(gameMenu3Bitmap, _gameMenuTexture3Width, _gameMenuTexture3Height);

        _middleBorder1TextureWidth = 12;
        _middleBorder1TextureHeight = 56;
        byte[] middleBorder1Bitmap = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
	        576, 0, _middleBorder1TextureWidth, _middleBorder1TextureHeight);
        _middleBorder1Texture = Graphics.CreateTextureFromBmp(middleBorder1Bitmap, _middleBorder1TextureWidth, _middleBorder1TextureHeight);
        _middleBorder2TextureWidth = 12;
        _middleBorder2TextureHeight = 200;
        byte[] middleBorder2Bitmap = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
	        576, _middleBorder1TextureHeight, _middleBorder2TextureWidth, _middleBorder2TextureHeight);
        _middleBorder2Texture = Graphics.CreateTextureFromBmp(middleBorder2Bitmap, _middleBorder2TextureWidth, _middleBorder2TextureHeight);

        _rightBorder1TextureWidth = 12;
        _rightBorder1TextureHeight = 56;
        byte[] rightBorder1Bitmap = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
	        mainScreenWidth - _rightBorder1TextureWidth, 0, _rightBorder1TextureWidth, _rightBorder1TextureHeight);
        _rightBorder1Texture = Graphics.CreateTextureFromBmp(rightBorder1Bitmap, _rightBorder1TextureWidth, _rightBorder1TextureHeight);
        _rightBorder2TextureWidth = 12;
        _rightBorder2TextureHeight = 200;
        byte[] rightBorder2Bitmap = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
	        mainScreenWidth - _rightBorder1TextureWidth, 264, _rightBorder2TextureWidth, _rightBorder2TextureHeight);
        _rightBorder2Texture = Graphics.CreateTextureFromBmp(rightBorder2Bitmap, _rightBorder2TextureWidth, _rightBorder2TextureHeight);

        _miniMapBorder1TextureWidth = 146;
        _miniMapBorder1TextureHeight = 8;
        byte[] miniMapBorder1Bitmap = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
	        576, 256, _miniMapBorder1TextureWidth, _miniMapBorder1TextureHeight);
        _miniMapBorder1Texture = Graphics.CreateTextureFromBmp(miniMapBorder1Bitmap, _miniMapBorder1TextureWidth, _miniMapBorder1TextureHeight);
        _miniMapBorder2TextureWidth = 146;
        _miniMapBorder2TextureHeight = 8;
        byte[] miniMapBorder2Bitmap = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
	        mainScreenWidth - _miniMapBorder1TextureWidth, 256, _miniMapBorder2TextureWidth, _miniMapBorder2TextureHeight);
        _miniMapBorder2Texture = Graphics.CreateTextureFromBmp(miniMapBorder2Bitmap, _miniMapBorder2TextureWidth, _miniMapBorder2TextureHeight);

        _bottomBorder1TextureWidth = 146;
        _bottomBorder1TextureHeight = 8;
        byte[] bottomBorder1Bitmap = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
	        576, mainScreenHeight - _bottomBorder1TextureHeight, _bottomBorder1TextureWidth, _bottomBorder1TextureHeight);
        _bottomBorder1Texture = Graphics.CreateTextureFromBmp(bottomBorder1Bitmap, _bottomBorder1TextureWidth, _bottomBorder1TextureHeight);
        _bottomBorder2TextureWidth = 146;
        _bottomBorder2TextureHeight = 8;
        byte[] bottomBorder2Bitmap = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
	        mainScreenWidth - _bottomBorder1TextureWidth, mainScreenHeight - _bottomBorder2TextureHeight, _bottomBorder2TextureWidth, _bottomBorder2TextureHeight);
        _bottomBorder2Texture = Graphics.CreateTextureFromBmp(bottomBorder2Bitmap, _bottomBorder2TextureWidth, _bottomBorder2TextureHeight);

        _detailsTexture1Width = 208;
        _detailsTexture1Height = 208;
        byte[] detailsBitmap1 = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
            584, 264, _detailsTexture1Width, _detailsTexture1Height);
        _detailsTexture1 = Graphics.CreateTextureFromBmp(detailsBitmap1, _detailsTexture1Width, _detailsTexture1Height);
        _detailsTexture2Width = 68;
        _detailsTexture2Height = 208;
        byte[] detailsBitmap2 = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
            584 + _detailsTexture1Width - _detailsTexture2Width, 264, _detailsTexture2Width, _detailsTexture2Height);
        _detailsTexture2 = Graphics.CreateTextureFromBmp(detailsBitmap2, _detailsTexture2Width, _detailsTexture2Height);
        _detailsTexture3Width = 208;
        _detailsTexture3Height = 120;
        byte[] detailsBitmap3 = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
            584, 264 + _detailsTexture1Height, _detailsTexture3Width, _detailsTexture3Height);
        _detailsTexture3 = Graphics.CreateTextureFromBmp(detailsBitmap3, _detailsTexture3Width, _detailsTexture3Height);
        _detailsTexture4Width = 68;
        _detailsTexture4Height = 120;
        byte[] detailsBitmap4 = Graphics.CopyBitmapRect(mainScreenBitmap, mainScreenWidth, mainScreenHeight,
            584 + _detailsTexture1Width - _detailsTexture2Width, 264 + _detailsTexture2Height, _detailsTexture4Width, _detailsTexture4Height);
        _detailsTexture4 = Graphics.CreateTextureFromBmp(detailsBitmap4, _detailsTexture4Width, _detailsTexture4Height);
        
        CreatePanels(detailsBitmap1, detailsBitmap2);

        CreateListBoxPanels(detailsBitmap1, detailsBitmap2);
        
        CreateListBoxScrollPanel(detailsBitmap1, detailsBitmap2);
	}

	private void CreatePanels(byte[] detailsBitmap1, byte[] detailsBitmap2)
	{
		_smallPanelWidth = (DetailsWidth - 4) / 3 * 2;
		_smallPanelHeight = 30;
		byte[] smallPanelBitmap = CreatePanelUpBitmap(detailsBitmap1, detailsBitmap2, _smallPanelWidth, _smallPanelHeight);
		_smallPanelTexture = Graphics.CreateTextureFromBmp(smallPanelBitmap, _smallPanelWidth, _smallPanelHeight, 32);
		_overseerPanelWidth = _smallPanelWidth;
		_overseerPanelHeight = 62;
		byte[] overseerPanelBitmap = CreatePanelUpBitmap(detailsBitmap1, detailsBitmap2, _overseerPanelWidth, _overseerPanelHeight);
		_overseerPanelTexture = Graphics.CreateTextureFromBmp(overseerPanelBitmap, _overseerPanelWidth, _overseerPanelHeight, 32);
		_workersPanelWidth = _smallPanelWidth;
		_workersPanelHeight = 72;
		byte[] workersPanelBitmap = CreatePanelUpBitmap(detailsBitmap1, detailsBitmap2, _workersPanelWidth, _workersPanelHeight);
		_workersPanelTexture = Graphics.CreateTextureFromBmp(workersPanelBitmap, _workersPanelWidth, _workersPanelHeight, 32);
		_mineFactoryPanelWidth = _smallPanelWidth;
		_mineFactoryPanelHeight = 86;
		byte[] mineFactoryPanelBitmap = CreatePanelUpBitmap(detailsBitmap1, detailsBitmap2, _mineFactoryPanelWidth, _mineFactoryPanelHeight);
		_mineFactoryPanelTexture = Graphics.CreateTextureFromBmp(mineFactoryPanelBitmap, _mineFactoryPanelWidth, _mineFactoryPanelHeight, 32);
		_unitPanelWidth = _smallPanelWidth;
		_unitPanelHeight = 62;
		byte[] unitPanelBitmap = CreatePanelUpBitmap(detailsBitmap1, detailsBitmap2, _unitPanelWidth, _unitPanelHeight);
		_unitPanelTexture = Graphics.CreateTextureFromBmp(unitPanelBitmap, _unitPanelWidth, _unitPanelHeight, 32);
		_panelWithOneFieldWidth = _smallPanelWidth;
		_panelWithOneFieldHeight = 25;
		byte[] panelWithOneFieldBitmap = CreatePanelUpBitmap(detailsBitmap1, detailsBitmap2, _panelWithOneFieldWidth, _panelWithOneFieldHeight);
		_panelWithOneFieldTexture = Graphics.CreateTextureFromBmp(panelWithOneFieldBitmap, _panelWithOneFieldWidth, _panelWithOneFieldHeight, 32);
		_panelWithTwoFieldsWidth = _smallPanelWidth;
		_panelWithTwoFieldsHeight = 44;
		byte[] panelWithTwoFieldsBitmap = CreatePanelUpBitmap(detailsBitmap1, detailsBitmap2, _panelWithTwoFieldsWidth, _panelWithTwoFieldsHeight);
		_panelWithTwoFieldsTexture = Graphics.CreateTextureFromBmp(panelWithTwoFieldsBitmap, _panelWithTwoFieldsWidth, _panelWithTwoFieldsHeight, 32);
		_panelWithThreeFieldsWidth = _smallPanelWidth;
		_panelWithThreeFieldsHeight = 63;
		byte[] panelWithThreeFieldsBitmap = CreatePanelUpBitmap(detailsBitmap1, detailsBitmap2, _panelWithThreeFieldsWidth, _panelWithThreeFieldsHeight);
		_panelWithThreeFieldsTexture = Graphics.CreateTextureFromBmp(panelWithThreeFieldsBitmap, _panelWithThreeFieldsWidth, _panelWithThreeFieldsHeight, 32);
		_fieldPanel62Width = 62;
		_fieldPanel62Height = 18;
		byte[] fieldPanel62Bitmap = CreatePanelUpBitmap(detailsBitmap1, detailsBitmap2, _fieldPanel62Width, _fieldPanel62Height);
		_fieldPanel62Texture = Graphics.CreateTextureFromBmp(fieldPanel62Bitmap, _fieldPanel62Width, _fieldPanel62Height, 32);
		_fieldPanel67Width = 67;
		_fieldPanel67Height = 18;
		byte[] fieldPanel67Bitmap = CreatePanelUpBitmap(detailsBitmap1, detailsBitmap2, _fieldPanel67Width, _fieldPanel67Height);
		_fieldPanel67Texture = Graphics.CreateTextureFromBmp(fieldPanel67Bitmap, _fieldPanel67Width, _fieldPanel67Height, 32);
		_fieldPanel75Width = 75;
		_fieldPanel75Height = 18;
		byte[] fieldPanel75Bitmap = CreatePanelUpBitmap(detailsBitmap1, detailsBitmap2, _fieldPanel75Width, _fieldPanel75Height);
		_fieldPanel75Texture = Graphics.CreateTextureFromBmp(fieldPanel75Bitmap, _fieldPanel75Width, _fieldPanel75Height, 32);
		_fieldPanel111Width = 111;
		_fieldPanel111Height = 18;
		byte[] fieldPanel111Bitmap = CreatePanelUpBitmap(detailsBitmap1, detailsBitmap2, _fieldPanel111Width, _fieldPanel111Height);
		_fieldPanel111Texture = Graphics.CreateTextureFromBmp(fieldPanel111Bitmap, _fieldPanel111Width, _fieldPanel111Height, 32);
		_fieldPanel119Width = 119;
		_fieldPanel119Height = 18;
		byte[] fieldPanel119Bitmap = CreatePanelUpBitmap(detailsBitmap1, detailsBitmap2, _fieldPanel119Width, _fieldPanel119Height);
		_fieldPanel119Texture = Graphics.CreateTextureFromBmp(fieldPanel119Bitmap, _fieldPanel119Width, _fieldPanel119Height, 32);
		_cancelPanelWidth = _smallPanelWidth;
		_cancelPanelHeight = 25;
		byte[] cancelPanelUpBitmap = CreatePanelUpBitmap(detailsBitmap1, detailsBitmap2, _cancelPanelWidth, _cancelPanelHeight);
		_cancelPanelUpTexture = Graphics.CreateTextureFromBmp(cancelPanelUpBitmap, _cancelPanelWidth, _cancelPanelHeight, 32);
		byte[] cancelPanelDownBitmap = CreatePanelDownBitmap(detailsBitmap1, detailsBitmap2, _cancelPanelWidth, _cancelPanelHeight);
		_cancelPanelDownTexture = Graphics.CreateTextureFromBmp(cancelPanelDownBitmap, _cancelPanelWidth, _cancelPanelHeight, 32);
		_skillPanelWidth = _smallPanelWidth;
		_skillPanelHeight = 40;
		byte[] skillPanelUpBitmap = CreatePanelUpBitmap(detailsBitmap1, detailsBitmap2, _skillPanelWidth, _skillPanelHeight);
		_skillPanelUpTexture = Graphics.CreateTextureFromBmp(skillPanelUpBitmap, _skillPanelWidth, _skillPanelHeight, 32);
		byte[] skillPanelDownBitmap = CreatePanelDownBitmap(detailsBitmap1, detailsBitmap2, _skillPanelWidth, _skillPanelHeight);
		_skillPanelDownTexture = Graphics.CreateTextureFromBmp(skillPanelDownBitmap, _skillPanelWidth, _skillPanelHeight, 32);
		_numberPanelWidth = 30;
		_numberPanelHeight = _smallPanelHeight;
		byte[] numberPanelUpBitmap = CreatePanelUpBitmap(detailsBitmap1, detailsBitmap2, _numberPanelWidth, _numberPanelHeight);
		_numberPanelUpTexture = Graphics.CreateTextureFromBmp(numberPanelUpBitmap, _numberPanelWidth, _numberPanelHeight, 32);
		byte[] numberPanelDownBitmap = CreatePanelDownBitmap(detailsBitmap1, detailsBitmap2, _numberPanelWidth, _numberPanelHeight);
		_numberPanelDownTexture = Graphics.CreateTextureFromBmp(numberPanelDownBitmap, _numberPanelWidth, _numberPanelHeight, 32);
	}

	private void CreateListBoxPanels(byte[] detailsBitmap1, byte[] detailsBitmap2)
	{
		_listBoxPanelWidth = (DetailsWidth - 3) / 3 * 2;
		_listBoxPanelHeight = 156;
		byte[] listBoxPanelBitmap = CreatePanelDownBitmap(detailsBitmap1, detailsBitmap2, _listBoxPanelWidth, _listBoxPanelHeight);
		_listBoxPanelTexture = Graphics.CreateTextureFromBmp(listBoxPanelBitmap, _listBoxPanelWidth, _listBoxPanelHeight, 32);

		_listBoxPanelWithScrollWidth = (DetailsWidth - 40) / 3 * 2;
		_listBoxPanelWithScrollHeight = 156;
		byte[] listBoxPanelWithScrollBitmap = CreatePanelDownBitmap(detailsBitmap1, detailsBitmap2, _listBoxPanelWithScrollWidth, _listBoxPanelWithScrollHeight);
		_listBoxPanelWithScrollTexture = Graphics.CreateTextureFromBmp(listBoxPanelWithScrollBitmap, _listBoxPanelWithScrollWidth, _listBoxPanelWithScrollHeight, 32);
	}

	private void CreateListBoxScrollPanel(byte[] detailsBitmap1, byte[] detailsBitmap2)
	{
		_listBoxScrollPanelWidth = 22;
		_listBoxScrollPanelHeight = _listBoxPanelWithScrollHeight;
		byte[] listBoxScrollPanelBitmap = CreatePanelDownBitmap(detailsBitmap1, detailsBitmap2, _listBoxScrollPanelWidth, _listBoxScrollPanelHeight);
		_listBoxScrollPanelTexture = Graphics.CreateTextureFromBmp(listBoxScrollPanelBitmap, _listBoxScrollPanelWidth, _listBoxScrollPanelHeight, 32);
	}
	
	private void CreateIconTextures()
	{
		ResourceIdx iconResource = new ResourceIdx($"{Sys.GameDataFolder}/Resource/I_ICON.RES");
		byte[] arrowUpData = iconResource.Read("ARROWUP");
		_arrowUpWidth = BitConverter.ToInt16(arrowUpData, 0);
		_arrowUpHeight = BitConverter.ToInt16(arrowUpData, 2);
		_arrowUpTexture = Graphics.CreateTextureFromBmp(arrowUpData.Skip(4).ToArray(), _arrowUpWidth, _arrowUpHeight);
		byte[] arrowDownData = iconResource.Read("ARROWDWN");
		_arrowDownWidth = BitConverter.ToInt16(arrowDownData, 0);
		_arrowDownHeight = BitConverter.ToInt16(arrowDownData, 2);
		_arrowDownTexture = Graphics.CreateTextureFromBmp(arrowDownData.Skip(4).ToArray(), _arrowDownWidth, _arrowDownHeight);
		byte[] kingData = iconResource.Read("U_KING");
		_skillWidth = BitConverter.ToInt16(kingData, 0);
		_skillHeight = BitConverter.ToInt16(kingData, 2);
		_kingTexture = Graphics.CreateTextureFromBmp(kingData.Skip(4).ToArray(), _skillWidth, _skillHeight);
		byte[] generalData = iconResource.Read("U_GENE");
		_generalTexture = Graphics.CreateTextureFromBmp(generalData.Skip(4).ToArray(), _skillWidth, _skillHeight);
		byte[] constructionData = iconResource.Read("U_CONS");
		_constructionTexture = Graphics.CreateTextureFromBmp(constructionData.Skip(4).ToArray(), _skillWidth, _skillHeight);
		byte[] leadershipData = iconResource.Read("U_LEAD");
		_leadershipTexture = Graphics.CreateTextureFromBmp(leadershipData.Skip(4).ToArray(), _skillWidth, _skillHeight);
		byte[] miningData = iconResource.Read("U_MINE");
		_miningTexture = Graphics.CreateTextureFromBmp(miningData.Skip(4).ToArray(), _skillWidth, _skillHeight);
		byte[] manufactureData = iconResource.Read("U_MANU");
		_manufactureTexture = Graphics.CreateTextureFromBmp(manufactureData.Skip(4).ToArray(), _skillWidth, _skillHeight);
		byte[] researchData = iconResource.Read("U_RESE");
		_researchTexture = Graphics.CreateTextureFromBmp(researchData.Skip(4).ToArray(), _skillWidth, _skillHeight);
		byte[] spyingData = iconResource.Read("U_SPY");
		_spyingTexture = Graphics.CreateTextureFromBmp(spyingData.Skip(4).ToArray(), _skillWidth, _skillHeight);
		byte[] prayingData = iconResource.Read("U_PRAY");
		_prayingTexture = Graphics.CreateTextureFromBmp(prayingData.Skip(4).ToArray(), _skillWidth, _skillHeight);
	}

	private void CreateButtonTextures()
	{
		ResourceIdx buttonImages = new ResourceIdx($"{Sys.GameDataFolder}/Resource/I_BUTTON.RES");
		byte[] buttonData = buttonImages.Read("BUTUP_A");
		_buttonUpWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonUpHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonUpWidth, _buttonUpHeight);
		_buttonUpTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonUpWidth, _buttonUpHeight);
		buttonData = CreateDisabledButtonTexture(buttonData, _buttonUpWidth, _buttonUpHeight);
		_buttonDisabledWidth = _buttonUpWidth;
		_buttonDisabledHeight = _buttonUpHeight;
		_buttonDisabledTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonDisabledWidth, _buttonDisabledHeight, 32);
		buttonData = buttonImages.Read("BUTDN_A");
		_buttonDownWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonDownHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonDownWidth, _buttonDownHeight);
		_buttonDownTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonDownWidth, _buttonDownHeight);

		buttonData = buttonImages.Read("CANCEL");
		_buttonCancelWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonCancelHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonCancelWidth, _buttonCancelHeight);
		_buttonCancelTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonCancelWidth, _buttonCancelHeight);
		buttonData = buttonImages.Read("F-DOWN");
		_buttonBuildDownWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonBuildDownHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonBuildDownWidth, _buttonBuildDownHeight);
		_buttonBuildDownTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonBuildDownWidth, _buttonBuildDownHeight);
		buttonData = buttonImages.Read("RECRUIT");
		_buttonRecruitWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonRecruitHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonRecruitWidth, _buttonRecruitHeight);
		_buttonRecruitTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonRecruitWidth, _buttonRecruitHeight);
		buttonData = CreateDisabledButtonTexture(buttonData, _buttonRecruitWidth, _buttonRecruitHeight);
		_buttonRecruitDisabledTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonRecruitWidth, _buttonRecruitHeight, 32);
		buttonData = buttonImages.Read("TRAIN");
		_buttonTrainWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonTrainHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonTrainWidth, _buttonTrainHeight);
		_buttonTrainTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonTrainWidth, _buttonTrainHeight);
		buttonData = CreateDisabledButtonTexture(buttonData, _buttonTrainWidth, _buttonTrainHeight);
		_buttonTrainDisabledTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonTrainWidth, _buttonTrainHeight, 32);
		buttonData = buttonImages.Read("COLLTAX");
		_buttonCollectTaxWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonCollectTaxHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonCollectTaxWidth, _buttonCollectTaxHeight);
		_buttonCollectTaxTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonCollectTaxWidth, _buttonCollectTaxHeight);
		buttonData = CreateDisabledButtonTexture(buttonData, _buttonCollectTaxWidth, _buttonCollectTaxHeight);
		_buttonCollectTaxDisabledTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonCollectTaxWidth, _buttonCollectTaxHeight, 32);
		buttonData = buttonImages.Read("GRANT");
		_buttonGrantWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonGrantHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonGrantWidth, _buttonGrantHeight);
		_buttonGrantTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonGrantWidth, _buttonGrantHeight);
		buttonData = CreateDisabledButtonTexture(buttonData, _buttonGrantWidth, _buttonGrantHeight);
		_buttonGrantDisabledTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonGrantWidth, _buttonGrantHeight, 32);
		buttonData = buttonImages.Read("PATROL");
		_buttonPatrolWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonPatrolHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonPatrolWidth, _buttonPatrolHeight);
		_buttonPatrolTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonPatrolWidth, _buttonPatrolHeight);
		buttonData = CreateDisabledButtonTexture(buttonData, _buttonPatrolWidth, _buttonPatrolHeight);
		_buttonPatrolDisabledTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonPatrolWidth, _buttonPatrolHeight, 32);
		buttonData = buttonImages.Read("CHGPROD");
		_buttonChangeProductionWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonChangeProductionHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonChangeProductionWidth, _buttonChangeProductionHeight);
		_buttonChangeProductionTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonChangeProductionWidth, _buttonChangeProductionHeight);
		buttonData = buttonImages.Read("SUCCEED");
		_buttonSucceedKingWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonSucceedKingHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonSucceedKingWidth, _buttonSucceedKingHeight);
		_buttonSucceedKingTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonSucceedKingWidth, _buttonSucceedKingHeight);
		buttonData = buttonImages.Read("AGGRESS0");
		_buttonAggressionOffWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonAggressionOffHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonAggressionOffWidth, _buttonAggressionOffHeight);
		_buttonAggressionOffTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonAggressionOffWidth, _buttonAggressionOffHeight);
		buttonData = buttonImages.Read("AGGRESS1");
		_buttonAggressionOnWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonAggressionOnHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonAggressionOnWidth, _buttonAggressionOnHeight);
		_buttonAggressionOnTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonAggressionOnWidth, _buttonAggressionOnHeight);
		buttonData = buttonImages.Read("SETTLE");
		_buttonSettleWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonSettleHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonSettleWidth, _buttonSettleHeight);
		_buttonSettleTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonSettleWidth, _buttonSettleHeight);
		buttonData = buttonImages.Read("BUILD");
		_buttonBuildWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonBuildHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonBuildWidth, _buttonBuildHeight);
		_buttonBuildTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonBuildWidth, _buttonBuildHeight);
		buttonData = buttonImages.Read("PROMOTE");
		_buttonPromoteWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonPromoteHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonPromoteWidth, _buttonPromoteHeight);
		_buttonPromoteTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonPromoteWidth, _buttonPromoteHeight);
		buttonData = buttonImages.Read("DEMOTE");
		_buttonDemoteWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonDemoteHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonDemoteWidth, _buttonDemoteHeight);
		_buttonDemoteTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonDemoteWidth, _buttonDemoteHeight);
		buttonData = buttonImages.Read("RETCAMP");
		_buttonReturnToCampWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonReturnToCampHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonReturnToCampWidth, _buttonReturnToCampHeight);
		_buttonReturnToCampTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonReturnToCampWidth, _buttonReturnToCampHeight);
		buttonData = buttonImages.Read("DEMOTE");
		_buttonDemoteWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonDemoteHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonDemoteWidth, _buttonDemoteHeight);
		_buttonDemoteTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonDemoteWidth, _buttonDemoteHeight);
		buttonData = buttonImages.Read("SPYNOTI1");
		_buttonSpyNotifyOnWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonSpyNotifyOnHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonSpyNotifyOnWidth, _buttonSpyNotifyOnHeight);
		_buttonSpyNotifyOnTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonSpyNotifyOnWidth, _buttonSpyNotifyOnHeight);
		buttonData = buttonImages.Read("SPYNOTI0");
		_buttonSpyNotifyOffWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonSpyNotifyOffHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonSpyNotifyOffWidth, _buttonSpyNotifyOffHeight);
		_buttonSpyNotifyOffTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonSpyNotifyOffWidth, _buttonSpyNotifyOffHeight);
		buttonData = buttonImages.Read("NOSPY");
		_buttonDropSpyIdentityWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonDropSpyIdentityHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonDropSpyIdentityWidth, _buttonDropSpyIdentityHeight);
		_buttonDropSpyIdentityTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonDropSpyIdentityWidth, _buttonDropSpyIdentityHeight);
		
		buttonData = buttonImages.Read("SPYMENU");
		_buttonSpyMenuWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonSpyMenuHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonSpyMenuWidth, _buttonSpyMenuHeight);
		_buttonSpyMenuTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonSpyMenuWidth, _buttonSpyMenuHeight);
		buttonData = buttonImages.Read("REWARDCB");
		_buttonRewardWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonRewardHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonRewardWidth, _buttonRewardHeight);
		_buttonRewardTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonRewardWidth, _buttonRewardHeight);
		buttonData = CreateDisabledButtonTexture(buttonData, _buttonRewardWidth, _buttonRewardHeight);
		_buttonRewardDisabledTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonRewardWidth, _buttonRewardHeight, 32);
		buttonData = buttonImages.Read("DEFENSE1");
		_buttonDefenseOnWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonDefenseOnHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonDefenseOnWidth, _buttonDefenseOnHeight);
		_buttonDefenseOnTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonDefenseOnWidth, _buttonDefenseOnHeight);
		buttonData = buttonImages.Read("DEFENSE0");
		_buttonDefenseOffWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonDefenseOffHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonDefenseOffWidth, _buttonDefenseOffHeight);
		_buttonDefenseOffTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonDefenseOffWidth, _buttonDefenseOffHeight);

		buttonData = buttonImages.Read("U_CONS");
		_buttonConstructionSkillWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonConstructionSkillHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonConstructionSkillWidth, _buttonConstructionSkillHeight);
		_buttonConstructionSkillTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonConstructionSkillWidth, _buttonConstructionSkillHeight);
		buttonData = buttonImages.Read("U_LEAD");
		_buttonLeadershipSkillWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonLeadershipSkillHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonLeadershipSkillWidth, _buttonLeadershipSkillHeight);
		_buttonLeadershipSkillTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonLeadershipSkillWidth, _buttonLeadershipSkillHeight);
		buttonData = buttonImages.Read("U_MINE");
		_buttonMineSkillWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonMineSkillHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonMineSkillWidth, _buttonMineSkillHeight);
		_buttonMineSkillTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonMineSkillWidth, _buttonMineSkillHeight);
		buttonData = buttonImages.Read("U_MANU");
		_buttonManufactureSkillWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonManufactureSkillHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonManufactureSkillWidth, _buttonManufactureSkillHeight);
		_buttonManufactureSkillTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonManufactureSkillWidth, _buttonManufactureSkillHeight);
		buttonData = buttonImages.Read("U_RESE");
		_buttonResearchSkillWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonResearchSkillHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonResearchSkillWidth, _buttonResearchSkillHeight);
		_buttonResearchSkillTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonResearchSkillWidth, _buttonResearchSkillHeight);
		buttonData = buttonImages.Read("U_SPY");
		_buttonSpySkillWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonSpySkillHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonSpySkillWidth, _buttonSpySkillHeight);
		_buttonSpySkillTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonSpySkillWidth, _buttonSpySkillHeight);
		
		buttonData = buttonImages.Read("REPAIRU");
		_buttonRepairUpTextureWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonRepairUpTextureHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonRepairUpTextureWidth, _buttonRepairUpTextureHeight);
		_buttonRepairUpTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonRepairUpTextureWidth, _buttonRepairUpTextureHeight);
		buttonData = buttonImages.Read("REPAIRD");
		_buttonRepairDownTextureWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonRepairDownTextureHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonRepairDownTextureWidth, _buttonRepairDownTextureHeight);
		_buttonRepairDownTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonRepairDownTextureWidth, _buttonRepairDownTextureHeight);
		buttonData = buttonImages.Read("REPAIRQU");
		_buttonRequestRepairUpTextureWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonRequestRepairUpTextureHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonRequestRepairUpTextureWidth, _buttonRequestRepairUpTextureHeight);
		_buttonRequestRepairUpTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonRequestRepairUpTextureWidth, _buttonRequestRepairUpTextureHeight);
		buttonData = buttonImages.Read("REPAIRQD");
		_buttonRequestRepairDownTextureWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonRequestRepairDownTextureHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonRequestRepairDownTextureWidth, _buttonRequestRepairDownTextureHeight);
		_buttonRequestRepairDownTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonRequestRepairDownTextureWidth, _buttonRequestRepairDownTextureHeight);
		buttonData = buttonImages.Read("V_SEL-U");
		_buttonSellUpTextureWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonSellUpTextureHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonSellUpTextureWidth, _buttonSellUpTextureHeight);
		_buttonSellUpTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonSellUpTextureWidth, _buttonSellUpTextureHeight);
		buttonData = buttonImages.Read("V_SEL-D");
		_buttonSellDownTextureWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonSellDownTextureHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonSellDownTextureWidth, _buttonSellDownTextureHeight);
		_buttonSellDownTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonSellDownTextureWidth, _buttonSellDownTextureHeight);
		buttonData = buttonImages.Read("V_DEM-U");
		_buttonDestructUpTextureWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonDestructUpTextureHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonDestructUpTextureWidth, _buttonDestructUpTextureHeight);
		_buttonDestructUpTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonDestructUpTextureWidth, _buttonDestructUpTextureHeight);
		buttonData = buttonImages.Read("V_DEM-D");
		_buttonDestructDownTextureWidth = BitConverter.ToInt16(buttonData, 0);
		_buttonDestructDownTextureHeight = BitConverter.ToInt16(buttonData, 2);
		buttonData = Graphics.DecompressTransparentBitmap(buttonData.Skip(4).ToArray(), _buttonDestructDownTextureWidth, _buttonDestructDownTextureHeight);
		_buttonDestructDownTexture = Graphics.CreateTextureFromBmp(buttonData, _buttonDestructDownTextureWidth, _buttonDestructDownTextureHeight);
	}

	private void CreateBuildButtonTextures(int colorScheme)
	{
		ResourceIdx buttonImages = new ResourceIdx($"{Sys.GameDataFolder}/Resource/I_BUTTON.RES");

		void CreateBuildButtonTexture(string key)
		{
			byte[] buildButton = buttonImages.Read(key);
			_buildButtonWidth = BitConverter.ToInt16(buildButton, 0);
			_buildButtonHeight = BitConverter.ToInt16(buildButton, 2);
			byte[] buildButtonBitmap = buildButton.Skip(4).ToArray();
			byte[] decompressedBitmap = Graphics.DecompressTransparentBitmap(buildButtonBitmap, _buildButtonWidth, _buildButtonHeight,
				ColorRemap.GetColorRemap(colorScheme, false).ColorTable);
			_buildButtonTextures.Add(key, Graphics.CreateTextureFromBmp(decompressedBitmap, _buildButtonWidth, _buildButtonHeight));
		}

		for (int i = 1; i < Firm.MAX_FIRM_TYPE; i++)
		{
			string key = "F-" + i;
			if (i == Firm.FIRM_BASE)
			{
				for (int j = 1; j <= GameConstants.MAX_RACE; j++)
				{
					CreateBuildButtonTexture(key + "-" + j);
				}
			}
			else
			{
				CreateBuildButtonTexture(key);
			}
		}
	}

	private byte[] CreateDisabledButtonTexture(byte[] buttonData, int width, int height)
	{
		byte[] disabledButtonBitmap = new byte[width * height * 4];
		int index = 0;
		for (int h = 0; h < height; h++)
		{
			for (int w = 0; w < width; w++)
			{
				byte paletteColor = buttonData[h * width + w];
				System.Drawing.Color color = Sys.Instance.PaletteColors[paletteColor];
				double factor = Math.Sqrt(1.0 / 2.0);
				disabledButtonBitmap[index] = (byte)(color.B * factor);
				disabledButtonBitmap[index + 1] = (byte)(color.G * factor);
				disabledButtonBitmap[index + 2] = (byte)(color.R * factor);
				disabledButtonBitmap[index + 3] = (byte)(paletteColor < Colors.MIN_TRANSPARENT_CODE ? 255 : 0);
				index += 4;
			}
		}

		return disabledButtonBitmap;
	}

	private byte[] CreatePanelUpBitmap(byte[] detailsBitmap1, byte[] detailsBitmap2, int width, int height)
	{
        byte[] panelUpBitmap = new byte[width * height * 4];
        int index = 0;
        for (int h = 0; h < height; h++)
        {
            for (int w = 0; w < width; w++)
            {
                byte paletteColor = w < 208 ? detailsBitmap1[h * 208 + w] : detailsBitmap2[h * 68 + w - 208];
                System.Drawing.Color color = Sys.Instance.PaletteColors[paletteColor];
                panelUpBitmap[index] = (byte)(color.B + (255 - color.B) * 6 / 10);
                panelUpBitmap[index + 1] = (byte)(color.G + (255 - color.G) * 6 / 10);
                panelUpBitmap[index + 2] = (byte)(color.R + (255 - color.R) * 6 / 10);
                panelUpBitmap[index + 3] = 0;
                index += 4;
            }
        }
        for (int w = 0; w < width; w++)
        {
            index = w * 4;
            panelUpBitmap[index] = panelUpBitmap[index + 1] = panelUpBitmap[index + 2] = 255;
            index = width * 4 + w * 4;
            panelUpBitmap[index] = panelUpBitmap[index + 1] = panelUpBitmap[index + 2] = 255;
            index = (height - 2) * width * 4 + w * 4;
            panelUpBitmap[index] = panelUpBitmap[index + 1] = panelUpBitmap[index + 2] = 0;
            index = (height - 1) * width * 4 + w * 4;
            panelUpBitmap[index] = panelUpBitmap[index + 1] = panelUpBitmap[index + 2] = 0;
        }
        for (int h = 2; h < height; h++)
        {
            index = h * width * 4;
            panelUpBitmap[index] = panelUpBitmap[index + 1] = panelUpBitmap[index + 2] = 255;
            index = h * width * 4 + 4;
            panelUpBitmap[index] = panelUpBitmap[index + 1] = panelUpBitmap[index + 2] = 255;
            index = h * width * 4 + (width - 2) * 4;
            panelUpBitmap[index] = panelUpBitmap[index + 1] = panelUpBitmap[index + 2] = 0;
            index = h * width * 4 + (width - 1) * 4;
            panelUpBitmap[index] = panelUpBitmap[index + 1] = panelUpBitmap[index + 2] = 0;
        }

        return panelUpBitmap;
	}
	
	private byte[] CreatePanelDownBitmap(byte[] detailsBitmap1, byte[] detailsBitmap2, int width, int height)
	{
		byte[] panelDownBitmap = new byte[width * height * 4];
        int index = 0;
        for (int h = 0; h < height; h++)
        {
            for (int w = 0; w < width; w++)
            {
                byte paletteColor = w < 208 ? detailsBitmap1[h * 208 + w] : detailsBitmap2[h * 68 + w - 208];
                System.Drawing.Color color = Sys.Instance.PaletteColors[paletteColor];
                panelDownBitmap[index] = (byte)(color.B + (255 - color.B) * 7 / 10);
                panelDownBitmap[index + 1] = (byte)(color.G + (255 - color.G) * 7 / 10);
                panelDownBitmap[index + 2] = (byte)(color.R + (255 - color.R) * 7 / 10);
                panelDownBitmap[index + 3] = 0;
                index += 4;
            }
        }
        for (int w = 0; w < width; w++)
        {
            index = w * 4;
            panelDownBitmap[index] = panelDownBitmap[index + 1] = panelDownBitmap[index + 2] = 0;
            index = width * 4 + w * 4;
            panelDownBitmap[index] = panelDownBitmap[index + 1] = panelDownBitmap[index + 2] = 0;
            index = (height - 2) * width * 4 + w * 4;
            panelDownBitmap[index] = panelDownBitmap[index + 1] = panelDownBitmap[index + 2] = 255;
            index = (height - 1) * width * 4 + w * 4;
            panelDownBitmap[index] = panelDownBitmap[index + 1] = panelDownBitmap[index + 2] = 255;
        }
        for (int h = 2; h < height; h++)
        {
            index = h * width * 4;
            panelDownBitmap[index] = panelDownBitmap[index + 1] = panelDownBitmap[index + 2] = 0;
            index = h * width * 4 + 4;
            panelDownBitmap[index] = panelDownBitmap[index + 1] = panelDownBitmap[index + 2] = 0;
            index = h * width * 4 + (width - 2) * 4;
            panelDownBitmap[index] = panelDownBitmap[index + 1] = panelDownBitmap[index + 2] = 255;
            index = h * width * 4 + (width - 1) * 4;
            panelDownBitmap[index] = panelDownBitmap[index + 1] = panelDownBitmap[index + 2] = 255;
        }

        return panelDownBitmap;
	}

	private void DrawSmallPanel(int x, int y)
	{
		Graphics.DrawBitmap(_smallPanelTexture, x, y, Scale(_smallPanelWidth), Scale(_smallPanelHeight));
	}

	private void DrawOverseerPanel(int x, int y)
	{
		Graphics.DrawBitmap(_overseerPanelTexture, x, y, Scale(_overseerPanelWidth), Scale(_overseerPanelHeight));
	}

	private void DrawWorkersPanel(int x, int y)
	{
		Graphics.DrawBitmap(_workersPanelTexture, x, y, Scale(_workersPanelWidth), Scale(_workersPanelHeight));
	}

	private void DrawMineFactoryPanel(int x, int y)
	{
		Graphics.DrawBitmap(_mineFactoryPanelTexture, x, y, Scale(_mineFactoryPanelWidth), Scale(_mineFactoryPanelHeight));
	}
	
	private void DrawUnitPanel(int x, int y)
	{
		Graphics.DrawBitmap(_unitPanelTexture, x, y, Scale(_unitPanelWidth), Scale(_unitPanelHeight));
	}
	
	private void DrawPanelWithOneField(int x, int y)
	{
		Graphics.DrawBitmap(_panelWithOneFieldTexture, x, y, Scale(_panelWithOneFieldWidth), Scale(_panelWithOneFieldHeight));
	}

	private void DrawPanelWithTwoFields(int x, int y)
	{
		Graphics.DrawBitmap(_panelWithTwoFieldsTexture, x, y, Scale(_panelWithTwoFieldsWidth), Scale(_panelWithTwoFieldsHeight));
	}

	private void DrawPanelWithThreeFields(int x, int y)
	{
		Graphics.DrawBitmap(_panelWithThreeFieldsTexture, x, y, Scale(_panelWithThreeFieldsWidth), Scale(_panelWithThreeFieldsHeight));
	}

	private void DrawFieldPanel62(int x, int y)
	{
		Graphics.DrawBitmap(_fieldPanel62Texture, x, y, Scale(_fieldPanel62Width), Scale(_fieldPanel62Height));
	}

	private void DrawFieldPanel67(int x, int y)
	{
		Graphics.DrawBitmap(_fieldPanel67Texture, x, y, Scale(_fieldPanel67Width), Scale(_fieldPanel67Height));
	}

	private void DrawFieldPanel75(int x, int y)
	{
		Graphics.DrawBitmap(_fieldPanel75Texture, x, y, Scale(_fieldPanel75Width), Scale(_fieldPanel75Height));
	}
	
	private void DrawFieldPanel111(int x, int y)
	{
		Graphics.DrawBitmap(_fieldPanel111Texture, x, y, Scale(_fieldPanel111Width), Scale(_fieldPanel111Height));
	}

	private void DrawFieldPanel119(int x, int y)
	{
		Graphics.DrawBitmap(_fieldPanel119Texture, x, y, Scale(_fieldPanel119Width), Scale(_fieldPanel119Height));
	}

	private void DrawCancelPanelUp(int x, int y)
	{
		Graphics.DrawBitmap(_cancelPanelUpTexture, x, y, Scale(_cancelPanelWidth), Scale(_cancelPanelHeight));
	}
	
	private void DrawCancelPanelDown(int x, int y)
	{
		Graphics.DrawBitmap(_cancelPanelDownTexture, x, y, Scale(_cancelPanelWidth), Scale(_cancelPanelHeight));
	}
	
	private void DrawSkillPanelUp(int x, int y)
	{
		Graphics.DrawBitmap(_skillPanelUpTexture, x, y, Scale(_skillPanelWidth), Scale(_skillPanelHeight));
	}

	private void DrawSkillPanelDown(int x, int y)
	{
		Graphics.DrawBitmap(_skillPanelDownTexture, x, y, Scale(_skillPanelWidth), Scale(_skillPanelHeight));
	}

	private void DrawNumberPanelUp(int x, int y)
	{
		Graphics.DrawBitmap(_numberPanelUpTexture, x, y, Scale(_numberPanelWidth), Scale(_numberPanelHeight));
	}

	private void DrawNumberPanelDown(int x, int y)
	{
		Graphics.DrawBitmap(_numberPanelDownTexture, x, y, Scale(_numberPanelWidth), Scale(_numberPanelHeight));
	}
	
	private void DrawListBoxPanel(int x, int y)
	{
		Graphics.DrawBitmap(_listBoxPanelTexture, x, y, Scale(_listBoxPanelWidth), Scale(_listBoxPanelHeight));
	}

	private void DrawListBoxPanelWithScroll(int x, int y)
	{
		Graphics.DrawBitmap(_listBoxPanelWithScrollTexture, x, y, Scale(_listBoxPanelWithScrollWidth), Scale(_listBoxPanelWithScrollHeight));
	}
	
	private void DrawListBoxScrollPanel(int x, int y)
	{
		Graphics.DrawBitmap(_listBoxScrollPanelTexture, x, y, Scale(_listBoxScrollPanelWidth), Scale(_listBoxScrollPanelHeight));
	}

	private void DrawSelectedBorder(int x1, int y1, int x2, int y2)
	{
		Graphics.DrawRect(x1, y1, x2 - x1, 3, 255, 255, 255);
		Graphics.DrawRect(x1, y1, 3, y2 - y1, 255, 255, 255);
		Graphics.DrawRect(x1 + 3, y2 - 3, x2 - x1 - 3, 3, 0, 0, 0);
		Graphics.DrawRect(x2 - 3, y1 + 3, 3, y2 - y1 - 3, 0, 0, 0);
	}

	private void DrawSpyIcon(int spyIconX, int spyIconY, int trueNationId)
	{
		Graphics.DrawBitmapScaled(_spyingTexture, spyIconX, spyIconY, _skillWidth, _skillHeight);

		if (Config.show_ai_info)
		{
			int iconWidth = Scale(_skillWidth);
			int iconHeight = Scale(_skillHeight);
			int color = ColorRemap.GetColorRemap(ColorRemap.ColorSchemes[trueNationId], false).MainColor;
			Graphics.DrawLine(spyIconX, spyIconY, spyIconX + iconWidth - 1, spyIconY, color);
			Graphics.DrawLine(spyIconX, spyIconY + 1, spyIconX + iconWidth - 1, spyIconY + 1, color);
			Graphics.DrawLine(spyIconX, spyIconY + iconHeight - 2, spyIconX + iconWidth - 1, spyIconY + iconHeight - 2, color);
			Graphics.DrawLine(spyIconX, spyIconY + iconHeight - 1, spyIconX + iconWidth - 1, spyIconY + iconHeight - 1, color);
			Graphics.DrawLine(spyIconX, spyIconY, spyIconX, spyIconY + iconHeight - 1, color);
			Graphics.DrawLine(spyIconX + 1, spyIconY, spyIconX + 1, spyIconY + iconHeight - 1, color);
			Graphics.DrawLine(spyIconX + iconWidth - 2, spyIconY, spyIconX + iconWidth - 2, spyIconY + iconHeight - 1, color);
			Graphics.DrawLine(spyIconX + iconWidth - 1, spyIconY, spyIconX + iconWidth - 1, spyIconY + iconHeight - 1, color);
		}
	}

	private void PutTextCenter(Font font, string text, int x1, int y1, int x2, int y2)
	{
		int textX = x1 + ((x2 - x1 + 1) - font.TextWidth(text)) / 2;
		int textY = y1 + ((y2 - y1 + 1) - font.FontHeight) / 2;

		if (textX < 0)
			textX = 0;

		PutText(font, text, textX, textY, x2);
	}

	private void PutText(Font font, string text, int x, int y, int x2 = -1, bool smallSize = false)
	{
		if (x2 < 0) // default
			x2 = x + font.MaxFontWidth * text.Length;

		x2 = Math.Min(x2, WindowWidth - 1);

		int y2 = y + font.FontHeight - 1;

		//-------------------------------------//

		for (int i = 0; i < text.Length; i++)
		{
			char textChar = text[i];

			//--------------- space character ------------------//

			if (textChar == ' ')
			{
				if (x + font.SpaceWidth > x2)
					break;

				x += font.SpaceWidth;
			}

			// --------- control word: @COL# (nation color) -----------//

			else
			{
				int colLength = "@COL".Length;
				bool hasColorCode = (textChar == '@') && (i + colLength <= text.Length && text.Substring(i, colLength) == "@COL");
				if (hasColorCode) // display nation color bar in text
				{
					if (x2 >= 0 && x + Font.NATION_COLOR_BAR_WIDTH - 1 > x2) // exceed right border x2
						break;

					// get nation color and skip over the word
					i += colLength;
					textChar = text[i];

					byte colorCode = ColorRemap.ColorRemaps[textChar - '0'].MainColor;

					//TODO
					//NationArray.disp_nation_color(x, y + 2, colorCode);

					x += Font.NATION_COLOR_BAR_WIDTH;
				}

				//------------- normal character ----------------//

				else if (textChar >= font.FirstChar && textChar <= font.LastChar)
				{
					FontInfo fontInfo = font[textChar - font.FirstChar];

					if (x + fontInfo.width > x2)
						break;

					if (fontInfo.width > 0)
					{
						if (smallSize)
						{
							Graphics.DrawBitmap(fontInfo.GetTexture(Graphics, font.FontBitmap),
								x, y + fontInfo.offset_y * 2 / 3, fontInfo.width * 2 / 3, fontInfo.height * 2 / 3);
							x += fontInfo.width * 2 / 3; // inter-character space
						}
						else
						{
							Graphics.DrawBitmap(fontInfo.GetTexture(Graphics, font.FontBitmap),
								x, y + fontInfo.offset_y, fontInfo.width, fontInfo.height);
							x += fontInfo.width; // inter-character space
						}
					}
				}
				else
				{
					//------ tab or unknown character -------//

					if (textChar == '\t') // Tab
						x += font.SpaceWidth * 8; // one tab = 8 space chars
					else
						x += font.SpaceWidth;
				}
			}

			x += smallSize ? font.InterCharSpace * 2 / 3 : font.InterCharSpace;
		}
	}
}