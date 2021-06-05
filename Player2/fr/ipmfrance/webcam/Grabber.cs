using fr.ipmfrance.webcam.com;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace fr.ipmfrance.webcam
{
    public class Grabber : ISampleGrabberCB
    {
        private IPMWebcam parent;
        private int width, height;

        public int Width
        {
            get { return width; }
            set { width = value; }
        }
        public int Height
        {
            get { return height; }
            set { height = value; }
        }

        public Grabber(IPMWebcam parent)
        {
            this.parent = parent;
        }

        public int SampleCB(double sampleTime, IntPtr sample)
        {
            return 0;
        }

        public int BufferCB(double sampleTime, IntPtr buffer, int bufferLen)
        {
            if (parent.isNewFrameExists())
            {
                System.Drawing.Bitmap image = new Bitmap(width, height, PixelFormat.Format24bppRgb);

                BitmapData imageData = image.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadWrite,
                    PixelFormat.Format24bppRgb);

                int srcStride = imageData.Stride;
                int dstStride = imageData.Stride;

                unsafe
                {
                    byte* dst = (byte*)imageData.Scan0.ToPointer() + dstStride * (height - 1);
                    byte* src = (byte*)buffer.ToPointer();

                    for (int y = 0; y < height; y++)
                    {
                        Win32.memcpy(dst, src, srcStride);
                        dst -= dstStride;
                        src += srcStride;
                    }
                }

                image.UnlockBits(imageData);
                parent.OnNewFrameCapture(image);

                image.Dispose();
            }

            return 0;
        }
    }

}
