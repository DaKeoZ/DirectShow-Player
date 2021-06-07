using fr.ipmfrance.win32;
using System.Drawing;
using System.Drawing.Imaging;

namespace fr.ipmfrance.webcam.tools
{
    public static class BitmapHelper
    {

        public static Bitmap CloneImage(Bitmap source)
        {
            BitmapData sourceData = source.LockBits(
                new Rectangle(0, 0, source.Width, source.Height),
                ImageLockMode.ReadOnly, source.PixelFormat);

            Bitmap destination = CloneImage(sourceData);

            source.UnlockBits(sourceData);

            if (
                (source.PixelFormat == PixelFormat.Format1bppIndexed) ||
                (source.PixelFormat == PixelFormat.Format4bppIndexed) ||
                (source.PixelFormat == PixelFormat.Format8bppIndexed) ||
                (source.PixelFormat == PixelFormat.Indexed))
            {
                ColorPalette srcPalette = source.Palette;
                ColorPalette dstPalette = destination.Palette;

                int n = srcPalette.Entries.Length;

                for (int i = 0; i < n; i++)
                {
                    dstPalette.Entries[i] = srcPalette.Entries[i];
                }

                destination.Palette = dstPalette;
            }

            return destination;
        }

        private static Bitmap CloneImage(BitmapData sourceData)
        {
            int width = sourceData.Width;
            int height = sourceData.Height;

            Bitmap destination = new Bitmap(width, height, sourceData.PixelFormat);

            BitmapData destinationData = destination.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite, destination.PixelFormat);

            NativeAPI.CopyUnmanagedMemory(destinationData.Scan0, sourceData.Scan0, height * sourceData.Stride);

            destination.UnlockBits(destinationData);

            return destination;
        }

    }
}
