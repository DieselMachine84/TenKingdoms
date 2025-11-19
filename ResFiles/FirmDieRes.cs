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

			firmBuild.BuildCode = Misc.ToString(firmBuildRec.race_code);
			firmBuild.AnimateFullSize = (firmBuildRec.animate_full_size == '1');

			firmBuild.RaceId = Misc.ToInt32(firmBuildRec.race_id);
			firmBuild.FrameCount = Misc.ToInt32(firmBuildRec.frame_count);

			firmBuild.UnderConstructionBitmapId = Misc.ToInt32(firmBuildRec.under_construction_bitmap_recno);
			firmBuild.IdleBitmapId = Misc.ToInt32(firmBuildRec.idle_bitmap_recno);
			firmBuild.GroundBitmapId = Misc.ToInt32(firmBuildRec.ground_bitmap_recno);

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

			for (int j = 0; j < firmBuild.FrameCount; j++, firstFrameId++)
			{
				FirmFrameRec firmFrameRec = new FirmFrameRec(dbFirmFrame, firstFrameId);

				//------ following animation frames, bitmap sections -----//

				firmBuild.FirstBitmaps[j] = Misc.ToInt32(firmFrameRec.first_bitmap);
				firmBuild.BitmapCounts[j] = Misc.ToInt32(firmFrameRec.bitmap_count);
				firmBuild.FrameDelays[j] = Misc.ToInt32(firmFrameRec.delay);

				//---- get the MIN offset_x, offset_y and MAX width, height ----//
				//
				// So we can get the largest area of all the frames in this building
				// and this will serve as a normal size setting for this building,
				// with variation from frame to frame
				//
				//--------------------------------------------------------------//

				int firmBitmapIndex = 0;
				for (int k = firmBuild.BitmapCounts[j]; k > 0; k--, firmBitmapIndex++)
				{
					FirmBitmap firmBitmap = FirmBitmaps[firmBuild.FirstBitmaps[j] - 1 + firmBitmapIndex];
					if (firmBitmap.OffsetX < minOffsetX)
						minOffsetX = firmBitmap.OffsetX;

					if (firmBitmap.OffsetY < minOffsetY)
						minOffsetY = firmBitmap.OffsetY;

					if (firmBitmap.OffsetX + firmBitmap.BitmapWidth > maxX2)
						maxX2 = firmBitmap.OffsetX + firmBitmap.BitmapWidth;

					if (firmBitmap.OffsetY + firmBitmap.BitmapHeight > maxY2)
						maxY2 = firmBitmap.OffsetY + firmBitmap.BitmapHeight;
				}
			}

			int bitmapId = firmBuild.FirstBitmaps[0];

			FirmBitmap firstBitmap = FirmBitmaps[bitmapId - 1];

			firmBuild.LocWidth = firstBitmap.LocWidth;
			firmBuild.LocHeight = firstBitmap.LocHeight;

			firmBuild.MinOffsetX = minOffsetX;
			firmBuild.MinOffsetY = minOffsetY;

			firmBuild.MaxBitmapWidth = maxX2 - minOffsetX;
			firmBuild.MaxBitmapHeight = maxY2 - minOffsetY;

			if (firmBuild.UnderConstructionBitmapId == 0)
				firmBuild.UnderConstructionBitmapId = bitmapId;

			if (firmBuild.IdleBitmapId == 0)
				firmBuild.IdleBitmapId = bitmapId;
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

			firmDieBitmap.OffsetX = Misc.ToInt32(firmBitmapRec.offset_x);
			firmDieBitmap.OffsetY = Misc.ToInt32(firmBitmapRec.offset_y);

			firmDieBitmap.LocWidth = Misc.ToInt32(firmBitmapRec.loc_width);
			firmDieBitmap.LocHeight = Misc.ToInt32(firmBitmapRec.loc_height);
			firmDieBitmap.DisplayLayer = firmBitmapRec.layer - '0';

			int bitmapOffset = BitConverter.ToInt32(firmBitmapRec.bitmap_ptr, 0);
			firmDieBitmap.Bitmap = firmDieBitmaps.Read(bitmapOffset);
			firmDieBitmap.BitmapWidth = BitConverter.ToInt16(firmDieBitmap.Bitmap, 0);
			firmDieBitmap.BitmapHeight = BitConverter.ToInt16(firmDieBitmap.Bitmap, 2);
			firmDieBitmap.Bitmap = firmDieBitmap.Bitmap.Skip(4).ToArray();
		}
	}
}