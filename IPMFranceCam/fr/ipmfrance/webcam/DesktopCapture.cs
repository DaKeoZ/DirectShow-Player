using fr.ipmfrance.win32;
using System;
using System.Drawing;
using System.IO;

namespace fr.ipmfrance.webcam
{
    public class DesktopCapture : IDisposable
    {

        protected IntPtr m_hMemDC = IntPtr.Zero;
        protected int m_nMaxWidth = 0;
        protected int m_nMaxHeight = 0;
        protected IntPtr m_hBitmap = IntPtr.Zero;
        protected IntPtr handleDesktop = IntPtr.Zero;
        protected int m_nWidth = CamConst.c_iDefaultWidth;
        protected int m_nHeight = CamConst.c_iDefaultHeight;
        private Bitmap imageBitmap;

        public DesktopCapture()
        {
            imageBitmap = new Bitmap(Image.FromFile("F:\\test.jpg"));
        }

        public void GetHandle()
        {
            // handleDesktop = Gdi32.CreateDC("DISPLAY", null, null, IntPtr.Zero);
            // handleDesktop = Gdi32.CreateDC("HD WebCam", null, null, IntPtr.Zero);
            handleDesktop = IntPtr.Zero;
            
            m_nMaxWidth = Gdi32.GetDeviceCaps(handleDesktop, 8);
            m_nMaxHeight = Gdi32.GetDeviceCaps(handleDesktop, 10);
            m_hMemDC = Gdi32.CreateCompatibleDC(handleDesktop);
            // m_hBitmap = Gdi32.CreateCompatibleBitmap(handleDesktop, m_nWidth, Math.Abs(m_nHeight));
            m_hBitmap = imageBitmap.GetHbitmap();
        }

        public void DeleteBitmap()
        {
            File.AppendAllText(@"F:\out.txt", "DeleteBitmap\n");
            if (m_hBitmap != IntPtr.Zero)
            {
                Gdi32.DeleteObject(m_hBitmap);
                m_hBitmap = IntPtr.Zero;
            }
        }

        private void SetTextColor(Color color)
        {
            File.AppendAllText(@"F:\out.txt", "SetTextColor\n");
            int rgb = (color.B & 0xFF) << 16 | (color.G & 0xFF) << 8 | color.R;
            Gdi32.SetTextColor(m_hMemDC, rgb);
        }

        public void output(ref IMediaSampleImpl mediaSample, ref BitmapInfo m_bmi)
        {
            File.AppendAllText(@"F:\out.txt", "output\n");
            IntPtr ptrSampleMediaBuffer;
            mediaSample.GetPointer(out ptrSampleMediaBuffer);

            if (m_hBitmap == IntPtr.Zero)
            {
                m_hBitmap = Gdi32.CreateCompatibleBitmap(handleDesktop, m_nWidth, Math.Abs(m_nHeight));
            }


            IntPtr hOldBitmap = Gdi32.SelectObject(m_hMemDC, m_hBitmap);

            Gdi32.StretchBlt(m_hMemDC, 0, 0, m_nWidth, Math.Abs(m_nHeight), handleDesktop, 0, 0, m_nMaxWidth, m_nMaxHeight, TernaryRasterOperations.SRCCOPY);


            Gdi32.SelectObject(m_hMemDC, hOldBitmap);

         

            Gdi32.GetDIBits(m_hMemDC, m_hBitmap, 0, (uint)Math.Abs(m_nHeight), ptrSampleMediaBuffer, ref m_bmi, 0);
        }

        private void WriteHello()
        {
            string text = "Hello World !!!!!!!!!!";
//            NativeTextRenderer nativeTextRenderer = new NativeTextRenderer(m_hMemDC);
//            Font newFont = new Font("Arial", 28);
//            Point pt = new Point(10, 50);
//            nativeTextRenderer.DrawString(text, newFont, Color.Red, pt);
        }

        public void SetBmiSize(int width, int height)
        {
            File.AppendAllText(@"F:\out.txt", "SetBmiSize\n");
            m_nWidth = width;
            m_nHeight = height;
        }

        public void Dispose()
        {
            File.AppendAllText(@"F:\out.txt", "Dispose\n");
            if (m_hBitmap != IntPtr.Zero)
            {
                Gdi32.DeleteObject(m_hBitmap);
                m_hBitmap = IntPtr.Zero;
            }

            if (handleDesktop != IntPtr.Zero)
            {
                Gdi32.DeleteDC(handleDesktop);
                handleDesktop = IntPtr.Zero;
            }

            if (m_hMemDC != IntPtr.Zero)
            {
                Gdi32.DeleteDC(m_hMemDC);
                m_hMemDC = IntPtr.Zero;
            }


        }
    }
}
