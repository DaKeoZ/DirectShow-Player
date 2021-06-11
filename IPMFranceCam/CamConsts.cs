using System;
using System.Collections.Generic;
using System.Text;

namespace VirtualCam
{
    public class CamConst
    {
        public const int c_iDefaultWidth = 1024;
        public const int c_iDefaultHeight = 756;
        public const int c_nDefaultBitCount = 32;
        public const int c_iDefaultFPS = 20;
        public const int c_iFormatsCount = 8;
        public const int c_nGranularityW = 160;
        public const int c_nGranularityH = 120;
        public const int c_nMinWidth = 320;
        public const int c_nMinHeight = 240;
        public const int c_nMaxWidth = c_nMinWidth + c_nGranularityW * (c_iFormatsCount - 1);
        public const int c_nMaxHeight = c_nMinHeight + c_nGranularityH * (c_iFormatsCount - 1);
        public const int c_nMinFPS = 1;
        public const int c_nMaxFPS = 30;

    }
}
