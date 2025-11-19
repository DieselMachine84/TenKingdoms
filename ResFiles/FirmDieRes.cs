using System;
using System.Linq;

namespace TenKingdoms;

public class FirmDieRes
{
	private const string FIRM_BUILD_DB = "FDBUILD";
	private const string FIRM_FRAME_DB = "FDFRAME";
	private const string FIRM_BITMAP_DB = "FDBITMAP";

	private FirmBuild[] FirmBuilds { get; set; }
	private FirmBitmap[] FirmBitmaps { get; set; }

	public GameSet GameSet { get; }

	public FirmDieRes(GameSet gameSet)
	{
		GameSet = gameSet;

		LoadBitmapInfo();
		LoadBuildInfo();
	}

	public FirmBuild GetBuild(int buildId)
	{
		return FirmBuilds[buildId - 1];
	}

	public FirmBitmap GetBitmap(int bitmapId)
	{
		return FirmBitmaps[bitmapId - 1];
	}

	private void LoadBuildInfo()
	{
		Database dbFirmBuild = GameSet.OpenDb(FIRM_BUILD_DB);
		FirmBuilds = new FirmBuild[dbFirmBuild.RecordCount];
		int[] firstFrameIds = new int[FirmBuilds.Length];

		for (int i = 0; i < FirmBuilds.Length; i++)
		{
			FirmBuildRec firmBuildRec = new FirmBuildRec(dbFirmBuild, i + 1);
			FirmBuild firmBuild = new FirmBuild();
			FirmBuilds[i] = firmBuild;

			firmBuild.build_code = Misc.ToString(firmBuildRec.race_code);
			firmBuild.animate_full_size = (firmBuildRec.animate_full_size == '1');

			firmBuild.race_id = Misc.ToInt32(firmBuildRec.race_id);
			firmBuild.frame_count = Misc.ToInt32(firmBuildRec.frame_count);

			firmBuild.under_construction_bitmap_recno = Misc.ToInt32(firmBuildRec.under_construction_bitmap_recno);
			firmBuild.idle_bitmap_recno = Misc.ToInt32(firmBuildRec.idle_bitmap_recno);
			firmBuild.ground_bitmap_recno = Misc.ToInt32(firmBuildRec.ground_bitmap_recno);

			firstFrameIds[i] = Misc.ToInt32(firmBuildRec.first_frame);

			// BUGHERE : need to compare same Firm name and build code in firm database
		}

		Database dbFirmFrame = GameSet.OpenDb(FIRM_FRAME_DB);
		for (int i = 0; i < FirmBuilds.Length; i++)
		{
			FirmBuild firmBuild = FirmBuilds[i];
			int firstFrameId = firstFrameIds[i];

			int minOffsetX = Int32.MaxValue / 2;
			int minOffsetY = Int32.MaxValue / 2;
			int maxX2 = 0;
			int maxY2 = 0;

			for (int j = 0; j < firmBuild.frame_count; j++, firstFrameId++)
			{
				FirmFrameRec firmFrameRec = new FirmFrameRec(dbFirmFrame, firstFrameId);

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
					FirmBitmap firmBitmap = FirmBitmaps[firmBuild.first_bitmap_array[j] - 1 + firmBitmapIndex];
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

			int bitmapId = firmBuild.first_bitmap_array[0];

			FirmBitmap firstBitmap = FirmBitmaps[bitmapId - 1];

			firmBuild.loc_width = firstBitmap.loc_width;
			firmBuild.loc_height = firstBitmap.loc_height;

			firmBuild.min_offset_x = minOffsetX;
			firmBuild.min_offset_y = minOffsetY;

			firmBuild.max_bitmap_width = maxX2 - minOffsetX;
			firmBuild.max_bitmap_height = maxY2 - minOffsetY;

			if (firmBuild.under_construction_bitmap_recno == 0)
				firmBuild.under_construction_bitmap_recno = bitmapId;

			if (firmBuild.idle_bitmap_recno == 0)
				firmBuild.idle_bitmap_recno = bitmapId;
		}
	}

	private void LoadBitmapInfo()
	{
		ResourceDb firmDieBitmaps = new ResourceDb($"{Sys.GameDataFolder}/Resource/I_FIRMDI.RES");
		Database dbFirmBitmap = GameSet.OpenDb(FIRM_BITMAP_DB);
		FirmBitmaps = new FirmBitmap[dbFirmBitmap.RecordCount];

		for (int i = 0; i < FirmBitmaps.Length; i++)
		{
			FirmBitmapRec firmBitmapRec = new FirmBitmapRec(dbFirmBitmap, i + 1);
			FirmBitmap firmDieBitmap = new FirmBitmap();
			FirmBitmaps[i] = firmDieBitmap;

			firmDieBitmap.offset_x = Misc.ToInt32(firmBitmapRec.offset_x);
			firmDieBitmap.offset_y = Misc.ToInt32(firmBitmapRec.offset_y);

			firmDieBitmap.loc_width = Misc.ToInt32(firmBitmapRec.loc_width);
			firmDieBitmap.loc_height = Misc.ToInt32(firmBitmapRec.loc_height);
			firmDieBitmap.display_layer = firmBitmapRec.layer - '0';

			int bitmapOffset = BitConverter.ToInt32(firmBitmapRec.bitmap_ptr, 0);
			firmDieBitmap.bitmap = firmDieBitmaps.Read(bitmapOffset);
			firmDieBitmap.bitmapWidth = BitConverter.ToInt16(firmDieBitmap.bitmap, 0);
			firmDieBitmap.bitmapHeight = BitConverter.ToInt16(firmDieBitmap.bitmap, 2);
			firmDieBitmap.bitmap = firmDieBitmap.bitmap.Skip(4).ToArray();
		}
	}
}