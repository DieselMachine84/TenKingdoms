using System;

namespace TenKingdoms;

public class FirmDieBitmap : FirmBitmap
{
    public int bitmap_offset;
}

public class FirmDieRes
{
	public const string FIRM_BUILD_DB = "FDBUILD";
	public const string FIRM_FRAME_DB = "FDFRAME";
	public const string FIRM_BITMAP_DB = "FDBITMAP";

	public FirmBuild[] firm_build_array;
	public FirmDieBitmap[] firm_bitmap_array;

	public ResourceDb res_bitmap;

	public GameSet GameSet { get; }

	public FirmDieRes(GameSet gameSet)
	{
		GameSet = gameSet;
		res_bitmap = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_FIRMDI.RES");

		LoadBitmapInfo();
		LoadBuildInfo();
	}

	public FirmBuild get_build(int buildId)
	{
		return firm_build_array[buildId - 1];
	}

	public FirmDieBitmap get_bitmap(int bitmapId)
	{
		return firm_bitmap_array[bitmapId - 1];
	}

	private void LoadBuildInfo()
	{
		//---- read in firm count and initialize firm info array ----//

		// only one database can be opened at a time, so we read FIRM.DBF first
		Database dbFirmBuild = GameSet.OpenDb(FIRM_BUILD_DB);

		firm_build_array = new FirmBuild[dbFirmBuild.RecordCount];

		//------ allocate an array for storing firstFrameRecno -----//

		int[] firstFrameArray = new int[dbFirmBuild.RecordCount];

		//---------- read in FDBUILD.DBF ---------//

		for (int i = 0; i < firm_build_array.Length; i++)
		{
			FirmBuildRec firmBuildRec = new FirmBuildRec(dbFirmBuild.Read(i + 1));
			FirmBuild firmBuild = new FirmBuild();
			firm_build_array[i] = firmBuild;

			firmBuild.build_code = Misc.ToString(firmBuildRec.race_code);
			firmBuild.animate_full_size = (firmBuildRec.animate_full_size == '1');

			firmBuild.race_id = Misc.ToInt32(firmBuildRec.race_id);
			firmBuild.frame_count = Misc.ToInt32(firmBuildRec.frame_count);

			firmBuild.under_construction_bitmap_recno = Misc.ToInt32(firmBuildRec.under_construction_bitmap_recno);
			firmBuild.idle_bitmap_recno = Misc.ToInt32(firmBuildRec.idle_bitmap_recno);
			firmBuild.ground_bitmap_recno = Misc.ToInt32(firmBuildRec.ground_bitmap_recno);

			firstFrameArray[i] = Misc.ToInt32(firmBuildRec.first_frame);

			// BUGHERE : need to compare same Firm name and build code in firm database
		}

		//-------- read in FDFRAME.DBF --------//

		Database dbFirmFrame = GameSet.OpenDb(FIRM_FRAME_DB);
		int minOffsetX, minOffsetY;
		int maxX2, maxY2;

		for (int i = 0; i < firm_build_array.Length; i++)
		{
			FirmBuild firmBuild = firm_build_array[i];
			int frameRecno = firstFrameArray[i];

			minOffsetX = minOffsetY = 0xFFFF;
			maxX2 = maxY2 = 0;

			for (int j = 0; j < firmBuild.frame_count; j++, frameRecno++)
			{
				FirmFrameRec firmFrameRec = new FirmFrameRec(dbFirmFrame.Read(frameRecno));

				//------ following animation frames, bitmap sections -----//

				firmBuild.first_bitmap_array[j] = Misc.ToInt32(firmFrameRec.first_bitmap);
				firmBuild.bitmap_count_array[j] = Misc.ToInt32(firmFrameRec.bitmap_count);

				firmBuild.frame_delay_array[j] = Misc.ToInt32(firmFrameRec.delay);

				//---- get the MIN offset_x, offset_y and MAX width, height ----//
				//
				// So we can get the largest area of all the frames in this building
				// and this will serve as a normal size setting for this building,
				// with variation from frame to frame
				//
				//--------------------------------------------------------------//

				int firmBitmapIndex = 0;
				for (int k = firmBuild.bitmap_count_array[j]; k > 0; k--, firmBitmapIndex++)
				{
					FirmBitmap firmBitmap = firm_bitmap_array[firmBuild.first_bitmap_array[j] - 1 + firmBitmapIndex];
					if (firmBitmap.offset_x < minOffsetX)
						minOffsetX = firmBitmap.offset_x;

					if (firmBitmap.offset_y < minOffsetY)
						minOffsetY = firmBitmap.offset_y;

					if (firmBitmap.offset_x + firmBitmap.bitmapWidth > maxX2)
						maxX2 = firmBitmap.offset_x + firmBitmap.bitmapWidth;

					if (firmBitmap.offset_y + firmBitmap.bitmapHeight > maxY2)
						maxY2 = firmBitmap.offset_y + firmBitmap.bitmapHeight;
				}
			}

			//------- set FirmBuild Info -------//

			int bitmapRecno = firmBuild.first_bitmap_array[0];

			//----- get the info of the first frame bitmap ----//

			FirmBitmap firstBitmap = firm_bitmap_array[bitmapRecno - 1];

			firmBuild.loc_width = firstBitmap.loc_width;
			firmBuild.loc_height = firstBitmap.loc_height;

			firmBuild.min_offset_x = minOffsetX;
			firmBuild.min_offset_y = minOffsetY;

			firmBuild.max_bitmap_width = maxX2 - minOffsetX;
			firmBuild.max_bitmap_height = maxY2 - minOffsetY;

			//------ set firmBuild's under construction and idle bitmap recno -----//

			if (firmBuild.under_construction_bitmap_recno == 0)
				firmBuild.under_construction_bitmap_recno = bitmapRecno;

			if (firmBuild.idle_bitmap_recno == 0)
				firmBuild.idle_bitmap_recno = bitmapRecno;
		}
	}

	private void LoadBitmapInfo()
	{
		Database dbFirmBitmap = GameSet.OpenDb(FIRM_BITMAP_DB);
		firm_bitmap_array = new FirmDieBitmap[dbFirmBitmap.RecordCount];

		for (int i = 0; i < firm_bitmap_array.Length; i++)
		{
			FirmBitmapRec firmBitmapRec = new FirmBitmapRec(dbFirmBitmap.Read(i + 1));
			FirmDieBitmap firmDieBitmap = new FirmDieBitmap();
			firm_bitmap_array[i] = firmDieBitmap;

			// BUGHERE : bitmap is not yet loaded into memory, fill them before draw()
			firmDieBitmap.bitmap = null;
			firmDieBitmap.bitmapWidth = 0;
			firmDieBitmap.bitmapHeight = 0;

			firmDieBitmap.offset_x = Misc.ToInt32(firmBitmapRec.offset_x);
			firmDieBitmap.offset_y = Misc.ToInt32(firmBitmapRec.offset_y);

			firmDieBitmap.loc_width = Misc.ToInt32(firmBitmapRec.loc_width);
			firmDieBitmap.loc_height = Misc.ToInt32(firmBitmapRec.loc_height);
			firmDieBitmap.display_layer = firmBitmapRec.layer - '0';

			firmDieBitmap.bitmap_offset = BitConverter.ToInt32(firmBitmapRec.bitmap_ptr, 0);
		}
	}
}