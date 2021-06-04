using DirectShow;
using DirectShow.BaseClasses;
using ExampleFilters;
using Sonic;
using System;
using System.Runtime.InteropServices;

namespace VirtualCam
{
    [ComVisible(true)]
    [Guid("9100239C-30B4-4d7f-ABA8-854A575C9DFB")]
    [AMovieSetup(Merit.Normal, AMovieSetup.CLSID_VideoInputDeviceCategory)]
    [PropPageSetup(typeof(AboutForm))]
    public class VirtualCamFilter : BaseSourceFilter, IAMFilterMiscFlags
    {
        private DesktopCapture desktopCapture = new DesktopCapture();
        private int m_nBitCount = CamConst.c_nDefaultBitCount;
        private long m_nAvgTimePerFrame = UNITS / CamConst.c_iDefaultFPS;
        private BitmapInfo bitmapInfo = new BitmapInfo();

        /// <summary>
        /// Constructeur
        /// </summary>
        public VirtualCamFilter()
            : base("IPM Virtual Camera")
        {
            bitmapInfo.bmiHeader = new BitmapInfoHeader();
            AddPin(new VirtualCamStream("Capture", this));
        }

        protected override int OnInitializePins()
        {
            return NOERROR;
        }

        public override int Pause()
        {
            if (m_State == FilterState.Stopped)
            {
                desktopCapture.GetHandle();
            }
            return base.Pause();
        }

        public override int Stop()
        {
            int hr = base.Stop();
            desktopCapture.Dispose();
            return hr;
        }

        #region Methods

        public int CheckMediaType(AMMediaType pmt)
        {
            if (pmt == null)
            {
                return E_POINTER;
            }

            if (pmt.formatPtr == IntPtr.Zero)
            {
                return VFW_E_INVALIDMEDIATYPE;
            }

            if (pmt.majorType != MediaType.Video)
            {
                return VFW_E_INVALIDMEDIATYPE;
            }

            if (
                   pmt.subType != MediaSubType.RGB24
                && pmt.subType != MediaSubType.RGB32
                && pmt.subType != MediaSubType.ARGB32
                )
            {
                return VFW_E_INVALIDMEDIATYPE;
            }

            BitmapInfoHeader _bmi = pmt;

            if (_bmi == null)
            {
                return E_UNEXPECTED;
            }

            if (_bmi.Compression != BI_RGB)
            {
                return VFW_E_TYPE_NOT_ACCEPTED;
            }

            if (_bmi.BitCount != 24 && _bmi.BitCount != 32)
            {
                return VFW_E_TYPE_NOT_ACCEPTED;
            }

            VideoStreamConfigCaps _caps;
            GetDefaultCaps(0, out _caps);
            if (
                    _bmi.Width < _caps.MinOutputSize.Width
                || _bmi.Width > _caps.MaxOutputSize.Width
                )
            {
                return VFW_E_INVALIDMEDIATYPE;
            }

            long _rate = 0;
            {
                VideoInfoHeader _pvi = pmt;
                if (_pvi != null)
                {
                    _rate = _pvi.AvgTimePerFrame;
                }
            }
            {
                VideoInfoHeader2 _pvi = pmt;
                if (_pvi != null)
                {
                    _rate = _pvi.AvgTimePerFrame;
                }
            }
            if (_rate < _caps.MinFrameInterval || _rate > _caps.MaxFrameInterval)
            {
                return VFW_E_INVALIDMEDIATYPE;
            }
            return NOERROR;
        }

        public int SetMediaType(AMMediaType pmt)
        {
            lock (m_Lock)
            {

                desktopCapture.DeleteBitmap();

                BitmapInfoHeader bitmapInfoHeader = pmt;
                bitmapInfo.bmiHeader.BitCount = bitmapInfoHeader.BitCount;
                if (bitmapInfoHeader.Height != 0)
                {
                    bitmapInfo.bmiHeader.Height = bitmapInfoHeader.Height;
                }

                if (bitmapInfoHeader.Width > 0)
                {
                    bitmapInfo.bmiHeader.Width = bitmapInfoHeader.Width;
                }

                bitmapInfo.bmiHeader.Compression = BI_RGB;
                bitmapInfo.bmiHeader.Planes = 1;
                bitmapInfo.bmiHeader.ImageSize = ALIGN16(bitmapInfo.bmiHeader.Width) * ALIGN16(Math.Abs(bitmapInfo.bmiHeader.Height)) * bitmapInfo.bmiHeader.BitCount / 8;

                desktopCapture.SetBmiSize(bitmapInfoHeader.Width, bitmapInfoHeader.Height);

                m_nBitCount = bitmapInfoHeader.BitCount;

                {
                    VideoInfoHeader _pvi = pmt;
                    if (_pvi != null)
                    {
                        m_nAvgTimePerFrame = _pvi.AvgTimePerFrame;
                    }
                }
                {
                    VideoInfoHeader2 _pvi = pmt;
                    if (_pvi != null)
                    {
                        m_nAvgTimePerFrame = _pvi.AvgTimePerFrame;
                    }
                }
            }
            return NOERROR;
        }

        public int GetMediaType(int iPosition, ref DirectShow.AMMediaType pMediaType)
        {
            if (iPosition < 0)
            {
                return E_INVALIDARG;
            }

            VideoStreamConfigCaps _caps;
            GetDefaultCaps(0, out _caps);

            int nWidth = 0;
            int nHeight = 0;

            if (iPosition == 0)
            {
                if (Pins.Count > 0 && Pins[0].CurrentMediaType.majorType == MediaType.Video)
                {
                    pMediaType.Set(Pins[0].CurrentMediaType);
                    return NOERROR;
                }
                nWidth = _caps.InputSize.Width;
                nHeight = _caps.InputSize.Height;
            }
            else
            {
                iPosition--;
                nWidth = _caps.MinOutputSize.Width + _caps.OutputGranularityX * iPosition;
                nHeight = _caps.MinOutputSize.Height + _caps.OutputGranularityY * iPosition;

                if (nWidth > _caps.MaxOutputSize.Width || nHeight > _caps.MaxOutputSize.Height)
                {
                    return VFW_S_NO_MORE_ITEMS;
                }
            }

            pMediaType.majorType = DirectShow.MediaType.Video;
            pMediaType.formatType = DirectShow.FormatType.VideoInfo;

            VideoInfoHeader vih = new VideoInfoHeader();
            vih.AvgTimePerFrame = m_nAvgTimePerFrame;
            vih.BmiHeader.Compression = BI_RGB;
            vih.BmiHeader.BitCount = (short)m_nBitCount;
            vih.BmiHeader.Width = nWidth;
            vih.BmiHeader.Height = nHeight;
            vih.BmiHeader.Planes = 1;
            vih.BmiHeader.ImageSize = vih.BmiHeader.Width * Math.Abs(vih.BmiHeader.Height) * vih.BmiHeader.BitCount / 8;

            if (vih.BmiHeader.BitCount == 32)
            {
                pMediaType.subType = DirectShow.MediaSubType.RGB32;
            }

            if (vih.BmiHeader.BitCount == 24)
            {
                pMediaType.subType = DirectShow.MediaSubType.RGB24;
            }

            DirectShow.AMMediaType.SetFormat(ref pMediaType, ref vih);
            pMediaType.fixedSizeSamples = true;
            pMediaType.sampleSize = vih.BmiHeader.ImageSize;

            return NOERROR;
        }

        public int DecideBufferSize(ref IMemAllocatorImpl pAlloc, ref AllocatorProperties prop)
        {
            AllocatorProperties _actual = new AllocatorProperties();

            DirectShow.BitmapInfoHeader _bmi = (DirectShow.BitmapInfoHeader)Pins[0].CurrentMediaType;
            prop.cbBuffer = _bmi.GetBitmapSize();

            if (prop.cbBuffer < _bmi.ImageSize)
            {
                prop.cbBuffer = _bmi.ImageSize;
            }

            if (prop.cbBuffer < bitmapInfo.bmiHeader.ImageSize)
            {
                prop.cbBuffer = bitmapInfo.bmiHeader.ImageSize;
            }

            prop.cBuffers = 1;
            prop.cbAlign = 1;
            prop.cbPrefix = 0;
            int hr = pAlloc.SetProperties(prop, _actual);
            return hr;
        }

        public int FillBuffer(ref IMediaSampleImpl _sample)
        {
            desktopCapture.output(ref _sample, ref bitmapInfo);

            _sample.SetActualDataLength(_sample.GetSize());
            _sample.SetSyncPoint(true);
            return NOERROR;
        }

        public int GetLatency(out long prtLatency)
        {
            prtLatency = UNITS / 30;
            DirectShow.AMMediaType mt = Pins[0].CurrentMediaType;
            if (mt.majorType == MediaType.Video)
            {
                {
                    VideoInfoHeader _pvi = mt;
                    if (_pvi != null)
                    {
                        prtLatency = _pvi.AvgTimePerFrame;
                    }
                }
                {
                    VideoInfoHeader2 _pvi = mt;
                    if (_pvi != null)
                    {
                        prtLatency = _pvi.AvgTimePerFrame;
                    }
                }
            }
            return NOERROR;
        }

        public int GetNumberOfCapabilities(out int iCount, out int iSize)
        {
            iCount = 0;
            AMMediaType mt = new AMMediaType();
            while (GetMediaType(iCount, ref mt) == S_OK) { 
                mt.Free();
                iCount++;
            };
            iSize = Marshal.SizeOf(typeof(VideoStreamConfigCaps));
            return NOERROR;
        }

        public int GetStreamCaps(int iIndex, out DirectShow.AMMediaType ppmt, out DirectShow.VideoStreamConfigCaps _caps)
        {
            ppmt = null;
            _caps = null;
            if (iIndex < 0) return E_INVALIDARG;

            ppmt = new DirectShow.AMMediaType();
            HRESULT hr = (HRESULT)GetMediaType(iIndex, ref ppmt);
            if (FAILED(hr))
            {
                return hr;
            }

            if (hr == VFW_S_NO_MORE_ITEMS)
            {
                return S_FALSE;
            }

            hr = (HRESULT)GetDefaultCaps(iIndex, out _caps);
            return hr;
        }

        public int SuggestAllocatorProperties(AllocatorProperties pprop)
        {
            AllocatorProperties _properties = new AllocatorProperties();
            HRESULT hr = (HRESULT)GetAllocatorProperties(_properties);
            if (FAILED(hr))
            {
                return hr;
            }

            if (pprop.cbBuffer != -1)
            {
                if (pprop.cbBuffer < _properties.cbBuffer) return E_FAIL;
            }

            if (pprop.cbAlign != -1 && pprop.cbAlign != _properties.cbAlign)
            {
                return E_FAIL;
            }

            if (pprop.cbPrefix != -1 && pprop.cbPrefix != _properties.cbPrefix)
            {
                return E_FAIL;
            }

            if (pprop.cBuffers != -1 && pprop.cBuffers < 1)
            {
                return E_FAIL;
            }

            return NOERROR;
        }

        public int GetAllocatorProperties(AllocatorProperties pprop)
        {
            DirectShow.AMMediaType mt = Pins[0].CurrentMediaType;
            if (mt.majorType == MediaType.Video)
            {
                int lSize = mt.sampleSize;
                BitmapInfoHeader _bmi = mt;
                if (_bmi != null)
                {
                    if (lSize < _bmi.GetBitmapSize())
                    {
                        lSize = _bmi.GetBitmapSize();
                    }

                    if (lSize < _bmi.ImageSize)
                    {
                        lSize = _bmi.ImageSize;
                    }

                }
                pprop.cbBuffer = lSize;
                pprop.cBuffers = 1;
                pprop.cbAlign = 1;
                pprop.cbPrefix = 0;

            }
            return NOERROR;
        }

        public int GetDefaultCaps(int nIndex, out DirectShow.VideoStreamConfigCaps _caps)
        {
            _caps = new VideoStreamConfigCaps();

            _caps.guid = FormatType.VideoInfo;
            _caps.VideoStandard = AnalogVideoStandard.None;
            _caps.InputSize.Width = CamConst.c_iDefaultWidth;
            _caps.InputSize.Height = CamConst.c_iDefaultHeight;
            _caps.MinCroppingSize.Width = CamConst.c_nMinWidth;
            _caps.MinCroppingSize.Height = CamConst.c_nMinHeight;

            _caps.MaxCroppingSize.Width = CamConst.c_nMaxWidth;
            _caps.MaxCroppingSize.Height = CamConst.c_nMaxHeight;
            _caps.CropGranularityX = CamConst.c_nGranularityW;
            _caps.CropGranularityY = CamConst.c_nGranularityH;
            _caps.CropAlignX = 0;
            _caps.CropAlignY = 0;

            _caps.MinOutputSize.Width = _caps.MinCroppingSize.Width;
            _caps.MinOutputSize.Height = _caps.MinCroppingSize.Height;
            _caps.MaxOutputSize.Width = _caps.MaxCroppingSize.Width;
            _caps.MaxOutputSize.Height = _caps.MaxCroppingSize.Height;
            _caps.OutputGranularityX = _caps.CropGranularityX;
            _caps.OutputGranularityY = _caps.CropGranularityY;
            _caps.StretchTapsX = 0;
            _caps.StretchTapsY = 0;
            _caps.ShrinkTapsX = 0;
            _caps.ShrinkTapsY = 0;
            _caps.MinFrameInterval = UNITS / CamConst.c_nMaxFPS;
            _caps.MaxFrameInterval = UNITS / CamConst.c_nMinFPS;
            _caps.MinBitsPerSecond = (_caps.MinOutputSize.Width * _caps.MinOutputSize.Height * CamConst.c_nDefaultBitCount) * CamConst.c_nMinFPS;
            _caps.MaxBitsPerSecond = (_caps.MaxOutputSize.Width * _caps.MaxOutputSize.Height * CamConst.c_nDefaultBitCount) * CamConst.c_nMaxFPS;

            return NOERROR;
        }

        #endregion

        #region IAMFilterMiscFlags Members

        public int GetMiscFlags()
        {
            return (int)AMFilterMiscFlags.IsSource;
        }

        #endregion

    }

}
