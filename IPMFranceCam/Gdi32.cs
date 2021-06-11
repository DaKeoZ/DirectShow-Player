using DirectShow;
using System;
using System.Runtime.InteropServices;

namespace VirtualCam
{

    [StructLayout(LayoutKind.Sequential)]
    public struct BitmapInfo
    {
        public BitmapInfoHeader bmiHeader;
        public int[] bmiColors;
    }

    public enum TernaryRasterOperations : uint
    {
        SRCCOPY = 0x00CC0020,
        SRCPAINT = 0x00EE0086,
        SRCAND = 0x008800C6,
        SRCINVERT = 0x00660046,
        SRCERASE = 0x00440328,
        NOTSRCCOPY = 0x00330008,
        NOTSRCERASE = 0x001100A6,
        MERGECOPY = 0x00C000CA,
        MERGEPAINT = 0x00BB0226,
        PATCOPY = 0x00F00021,
        PATPAINT = 0x00FB0A09,
        PATINVERT = 0x005A0049,
        DSTINVERT = 0x00550009,
        BLACKNESS = 0x00000042,
        WHITENESS = 0x00FF0062,
        CAPTUREBLT = 0x40000000
    }

    public static class Gdi32
    {
        [DllImport("gdi32.dll")]
        public static extern int SetTextColor(IntPtr hdc, int color);

        [DllImport("gdi32.dll")]
        public static extern int SetBkMode(IntPtr hdc, int mode);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll", ExactSpelling = true, PreserveSig = true, SetLastError = true)]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern bool StretchBlt(IntPtr hdcDest, int nXOriginDest, int nYOriginDest,
            int nWidthDest, int nHeightDest,
            IntPtr hdcSrc, int nXOriginSrc, int nYOriginSrc, int nWidthSrc, int nHeightSrc,
            TernaryRasterOperations dwRop);

        [DllImport("gdi32.dll")]
        public static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint uStartScan,
           uint cScanLines, [Out] IntPtr lpvBits, ref BitmapInfo lpbmi, uint uUsage);

        [DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
      public  static extern bool TextOut(IntPtr hdc, int nXStart, int nYStart, string lpString, int cbString);


    }
}
