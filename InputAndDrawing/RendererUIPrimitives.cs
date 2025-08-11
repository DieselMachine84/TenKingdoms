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
	private const int ButtonsCampY = DetailsY1 + 376;

	private readonly Dictionary<int, IntPtr> _colorSquareTextures = new Dictionary<int, nint>();
	private int _colorSquareWidth;
	private int _colorSquareHeight;
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
	private IntPtr _unitPanelTexture;
	private int _unitPanelWidth;
	private int _unitPanelHeight;
	private IntPtr _panelWithTwoFieldsTexture;
	private int _panelWithTwoFieldsWidth;
	private int _panelWithTwoFieldsHeight;
	private IntPtr _panelWithThreeFieldsTexture;
	private int _panelWithThreeFieldsWidth;
	private int _panelWithThreeFieldsHeight;
	private IntPtr _fieldPanel1Texture;
	private int _fieldPanel1Width;
	private int _fieldPanel1Height;
	private IntPtr _fieldPanel2Texture;
	private int _fieldPanel2Width;
	private int _fieldPanel2Height;
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

	private IntPtr _buttonUpTexture;
	private int _buttonUpWidth;
	private int _buttonUpHeight;
	private IntPtr _buttonDownTexture;
	private int _buttonDownWidth;
	private int _buttonDownHeight;
	private IntPtr _buttonDisabledTexture;
	private int _buttonDisabledWidth;
	private int _buttonDisabledHeight;
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
		_unitPanelWidth = _smallPanelWidth;
		_unitPanelHeight = 62;
		byte[] unitPanelBitmap = CreatePanelUpBitmap(detailsBitmap1, detailsBitmap2, _unitPanelWidth, _unitPanelHeight);
		_unitPanelTexture = Graphics.CreateTextureFromBmp(unitPanelBitmap, _unitPanelWidth, _unitPanelHeight, 32);
		_panelWithTwoFieldsWidth = _smallPanelWidth;
		_panelWithTwoFieldsHeight = 44;
		byte[] panelWithTwoFieldsBitmap = CreatePanelUpBitmap(detailsBitmap1, detailsBitmap2, _panelWithTwoFieldsWidth, _panelWithTwoFieldsHeight);
		_panelWithTwoFieldsTexture = Graphics.CreateTextureFromBmp(panelWithTwoFieldsBitmap, _panelWithTwoFieldsWidth, _panelWithTwoFieldsHeight, 32);
		_panelWithThreeFieldsWidth = _smallPanelWidth;
		_panelWithThreeFieldsHeight = 63;
		byte[] panelWithThreeFieldsBitmap = CreatePanelUpBitmap(detailsBitmap1, detailsBitmap2, _panelWithThreeFieldsWidth, _panelWithThreeFieldsHeight);
		_panelWithThreeFieldsTexture = Graphics.CreateTextureFromBmp(panelWithThreeFieldsBitmap, _panelWithThreeFieldsWidth, _panelWithThreeFieldsHeight, 32);
		_fieldPanel1Width = 67;
		_fieldPanel1Height = 18;
		byte[] fieldPanel1Bitmap = CreatePanelUpBitmap(detailsBitmap1, detailsBitmap2, _fieldPanel1Width, _fieldPanel1Height);
		_fieldPanel1Texture = Graphics.CreateTextureFromBmp(fieldPanel1Bitmap, _fieldPanel1Width, _fieldPanel1Height, 32);
		_fieldPanel2Width = 62;
		_fieldPanel2Height = 18;
		byte[] fieldPanel2Bitmap = CreatePanelUpBitmap(detailsBitmap1, detailsBitmap2, _fieldPanel2Width, _fieldPanel2Height);
		_fieldPanel2Texture = Graphics.CreateTextureFromBmp(fieldPanel2Bitmap, _fieldPanel2Width, _fieldPanel2Height, 32);
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
	
	private void CreateArrowTextures()
	{
		ResourceIdx arrowResource = new ResourceIdx($"{Sys.GameDataFolder}/Resource/I_ICON.RES");
		byte[] arrowUpData = arrowResource.Read("ARROWUP");
		_arrowUpWidth = BitConverter.ToInt16(arrowUpData, 0);
		_arrowUpHeight = BitConverter.ToInt16(arrowUpData, 2);
		_arrowUpTexture = Graphics.CreateTextureFromBmp(arrowUpData.Skip(4).ToArray(), _arrowUpWidth, _arrowUpHeight);
		byte[] arrowDownData = arrowResource.Read("ARROWDWN");
		_arrowDownWidth = BitConverter.ToInt16(arrowDownData, 0);
		_arrowDownHeight = BitConverter.ToInt16(arrowDownData, 2);
		_arrowDownTexture = Graphics.CreateTextureFromBmp(arrowDownData.Skip(4).ToArray(), _arrowDownWidth, _arrowDownHeight);
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

	private void DrawUnitPanel(int x, int y)
	{
		Graphics.DrawBitmap(_unitPanelTexture, x, y, Scale(_unitPanelWidth), Scale(_unitPanelHeight));
	}
	
	private void DrawPanelWithTwoFields(int x, int y)
	{
		Graphics.DrawBitmap(_panelWithTwoFieldsTexture, x, y, Scale(_panelWithTwoFieldsWidth), Scale(_panelWithTwoFieldsHeight));
	}

	private void DrawPanelWithThreeFields(int x, int y)
	{
		Graphics.DrawBitmap(_panelWithThreeFieldsTexture, x, y, Scale(_panelWithThreeFieldsWidth), Scale(_panelWithThreeFieldsHeight));
	}

	private void DrawFieldPanel1(int x, int y)
	{
		Graphics.DrawBitmap(_fieldPanel1Texture, x, y, Scale(_fieldPanel1Width), Scale(_fieldPanel1Height));
	}

	private void DrawFieldPanel2(int x, int y)
	{
		Graphics.DrawBitmap(_fieldPanel2Texture, x, y, Scale(_fieldPanel2Width), Scale(_fieldPanel2Height));
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

					byte colorCode = ColorRemap.color_remap_array[textChar - '0'].MainColor;

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