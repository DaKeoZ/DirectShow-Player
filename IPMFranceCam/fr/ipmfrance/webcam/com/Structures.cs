using System;
using System.Runtime.InteropServices;

namespace fr.ipmfrance.webcam.com
{


    // PIN_DIRECTION

    /// <summary>
    /// This enumeration indicates a pin's direction.
    /// </summary>
    /// 
    [ComVisible( false )]
    public enum PinDirection
    {
        /// <summary>
        /// Input pin.
        /// </summary>
        Input,

        /// <summary>
        /// Output pin.
        /// </summary>
        Output
    }

    [Flags]
    [ComVisible(false)]
    public enum AMRenderExFlags
    {
        None = 0,
        RenderToExistingRenderers = 1
    }

    // AM_MEDIA_TYPE

    /// <summary>
    /// The structure describes the format of a media sample.
    /// </summary>
    /// 
    [ComVisible( false ),
    StructLayout( LayoutKind.Sequential )]
    public class AMMediaType : IDisposable
    {
        /// <summary>
        /// Globally unique identifier (GUID) that specifies the major type of the media sample.
        /// </summary>
        public Guid MajorType;

        /// <summary>
        /// GUID that specifies the subtype of the media sample.
        /// </summary>
        public Guid SubType;

        /// <summary>
        /// If <b>true</b>, samples are of a fixed size.
        /// </summary>
        [MarshalAs( UnmanagedType.Bool )]
        public bool FixedSizeSamples = true;

        /// <summary>
        /// If <b>true</b>, samples are compressed using temporal (interframe) compression.
        /// </summary>
        [MarshalAs( UnmanagedType.Bool )]
        public bool TemporalCompression;

        /// <summary>
        /// Size of the sample in bytes. For compressed data, the value can be zero.
        /// </summary>
        public int SampleSize = 1;

        /// <summary>
        /// GUID that specifies the structure used for the format block.
        /// </summary>
        public Guid FormatType;

        /// <summary>
        /// Not used.
        /// </summary>
        public IntPtr unkPtr;

        /// <summary>
        /// Size of the format block, in bytes.
        /// </summary>
        public int FormatSize;

        /// <summary>
        /// Pointer to the format block.
        /// </summary>
        public IntPtr FormatPtr;

        /// <summary>
        /// Destroys the instance of the <see cref="AMMediaType"/> class.
        /// </summary>
        /// 
        ~AMMediaType( )
        {
            Dispose( false );
        }

        /// <summary>
        /// Dispose the object.
        /// </summary>
        ///
        public void Dispose( )
        {
            Dispose( true );
            // remove me from the Finalization queue 
            GC.SuppressFinalize( this );
        }

        /// <summary>
        /// Dispose the object
        /// </summary>
        /// 
        /// <param name="disposing">Indicates if disposing was initiated manually.</param>
        /// 
        protected virtual void Dispose( bool disposing )
        {
            if ( ( FormatSize != 0 ) && ( FormatPtr != IntPtr.Zero ) )
            {
                Marshal.FreeCoTaskMem( FormatPtr );
                FormatSize = 0;
            }

            if ( unkPtr != IntPtr.Zero )
            {
                Marshal.Release( unkPtr );
                unkPtr = IntPtr.Zero;
            }
        }
    }


    // PIN_INFO

    /// <summary>
    /// The structure contains information about a pin.
    /// </summary>
    /// 
    [ComVisible( false ),
    StructLayout( LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode )]
    public struct PinInfo
    {
        /// <summary>
        /// Owning filter.
        /// </summary>
        public IBaseFilter Filter;

        /// <summary>
        /// Direction of the pin.
        /// </summary>
        public PinDirection Direction;

        /// <summary>
        /// Name of the pin.
        /// </summary>
        [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 128 )]
        public string Name;
    }

    // FILTER_INFO
    [ComVisible( false ),
    StructLayout( LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode )]
    public struct FilterInfo
    {
        /// <summary>
        /// Filter's name.
        /// </summary>
        [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 128 )]
        public string Name;

        /// <summary>
        /// Owning graph.
        /// </summary>
        public IFilterGraph FilterGraph;
    }

    // VIDEOINFOHEADER

    /// <summary>
    /// The structure describes the bitmap and color information for a video image.
    /// </summary>
    /// 
    [ComVisible( false ),
    StructLayout( LayoutKind.Sequential )]
    internal struct VideoInfoHeader
    {
        /// <summary>
        /// <see cref="RECT"/> structure that specifies the source video window.
        /// </summary>
        public RECT SrcRect;

        /// <summary>
        /// <see cref="RECT"/> structure that specifies the destination video window.
        /// </summary>
        public RECT TargetRect;

        /// <summary>
        /// Approximate data rate of the video stream, in bits per second.
        /// </summary>
        public int BitRate;

        /// <summary>
        /// Data error rate, in bit errors per second.
        /// </summary>
        public int BitErrorRate;

        /// <summary>
        /// The desired average display time of the video frames, in 100-nanosecond units.
        /// </summary>
        public long AverageTimePerFrame;

        /// <summary>
        /// <see cref="BitmapInfoHeader"/> structure that contains color and dimension information for the video image bitmap.
        /// </summary>
        public BitmapInfoHeader BmiHeader;
    }

    // VIDEOINFOHEADER2

    /// <summary>
    /// The structure describes the bitmap and color information for a video image (v2).
    /// </summary>
    /// 
    [ComVisible( false ),
    StructLayout( LayoutKind.Sequential )]
    internal struct VideoInfoHeader2
    {
        /// <summary>
        /// <see cref="RECT"/> structure that specifies the source video window.
        /// </summary>
        public RECT SrcRect;

        /// <summary>
        /// <see cref="RECT"/> structure that specifies the destination video window.
        /// </summary>
        public RECT TargetRect;

        /// <summary>
        /// Approximate data rate of the video stream, in bits per second.
        /// </summary>
        public int BitRate;

        /// <summary>
        /// Data error rate, in bit errors per second.
        /// </summary>
        public int BitErrorRate;

        /// <summary>
        /// The desired average display time of the video frames, in 100-nanosecond units.
        /// </summary>
        public long AverageTimePerFrame;

        /// <summary>
        /// Flags that specify how the video is interlaced.
        /// </summary>
        public int InterlaceFlags;

        /// <summary>
        /// Flag set to indicate that the duplication of the stream should be restricted.
        /// </summary>
        public int CopyProtectFlags;

        /// <summary>
        /// The X dimension of picture aspect ratio.
        /// </summary>
        public int PictAspectRatioX;

        /// <summary>
        /// The Y dimension of picture aspect ratio.
        /// </summary>
        public int PictAspectRatioY;

        /// <summary>
        /// Reserved for future use.
        /// </summary>
        public int Reserved1;

        /// <summary>
        /// Reserved for future use. 
        /// </summary>
        public int Reserved2;

        /// <summary>
        /// <see cref="BitmapInfoHeader"/> structure that contains color and dimension information for the video image bitmap.
        /// </summary>
        public BitmapInfoHeader BmiHeader;
    }

    /// <summary>
    /// The structure contains information about the dimensions and color format of a device-independent bitmap (DIB).
    /// </summary>
    /// 
    [ComVisible( false ),
    StructLayout( LayoutKind.Sequential, Pack = 2 )]
    internal struct BitmapInfoHeader
    {
        /// <summary>
        /// Specifies the number of bytes required by the structure.
        /// </summary>
        public int Size;

        /// <summary>
        /// Specifies the width of the bitmap.
        /// </summary>
        public int Width;

        /// <summary>
        /// Specifies the height of the bitmap, in pixels.
        /// </summary>
        public int Height;

        /// <summary>
        /// Specifies the number of planes for the target device. This value must be set to 1.
        /// </summary>
        public short Planes;

        /// <summary>
        /// Specifies the number of bits per pixel.
        /// </summary>
        public short BitCount;

        /// <summary>
        /// If the bitmap is compressed, this member is a <b>FOURCC</b> the specifies the compression.
        /// </summary>
        public int Compression;

        /// <summary>
        /// Specifies the size, in bytes, of the image.
        /// </summary>
        public int ImageSize;

        /// <summary>
        /// Specifies the horizontal resolution, in pixels per meter, of the target device for the bitmap.
        /// </summary>
        public int XPelsPerMeter;

        /// <summary>
        /// Specifies the vertical resolution, in pixels per meter, of the target device for the bitmap.
        /// </summary>
        public int YPelsPerMeter;

        /// <summary>
        /// Specifies the number of color indices in the color table that are actually used by the bitmap.
        /// </summary>
        public int ColorsUsed;

        /// <summary>
        /// Specifies the number of color indices that are considered important for displaying the bitmap.
        /// </summary>
        public int ColorsImportant;
    }

    // RECT

    /// <summary>
    /// The structure defines the coordinates of the upper-left and lower-right corners of a rectangle.
    /// </summary>
    /// 
    [ComVisible( false ),
    StructLayout( LayoutKind.Sequential )]
    internal struct RECT
    {
        /// <summary>
        /// Specifies the x-coordinate of the upper-left corner of the rectangle.
        /// </summary>
        public int Left;

        /// <summary>
        /// Specifies the y-coordinate of the upper-left corner of the rectangle. 
        /// </summary>
        public int Top;

        /// <summary>
        /// Specifies the x-coordinate of the lower-right corner of the rectangle.
        /// </summary>
        public int Right;

        /// <summary>
        /// Specifies the y-coordinate of the lower-right corner of the rectangle.
        /// </summary>
        public int Bottom;
    }

    // CAUUID

    /// <summary>
    /// The CAUUID structure is a Counted Array of UUID or GUID types.
    /// </summary>
    /// 
    [ComVisible( false ),
    StructLayout( LayoutKind.Sequential )]
    internal struct CAUUID
    {
        public int cElems;
        public IntPtr pElems;

        public Guid[] ToGuidArray()
        {
            Guid[] retval = new Guid[this.cElems];

            for (int i = 0; i < this.cElems; i++)
            {
                IntPtr ptr = new IntPtr(this.pElems.ToInt64() + (Marshal.SizeOf(typeof(Guid)) * i));
                retval[i] = (Guid)Marshal.PtrToStructure(ptr, typeof(Guid));
            }

            return retval;
        }
    }

    /// <summary>
    /// Enumeration of DirectShow event codes.
    /// </summary>
    internal enum DsEvCode
    {
        None,
        Complete = 0x01,		// EC_COMPLETE
        UserAbort = 0x02,		// EC_USERABORT
        ErrorAbort = 0x03,		// EC_ERRORABORT
        Time = 0x04,		// EC_TIME
        Repaint = 0x05,		// EC_REPAINT
        StErrStopped = 0x06,		// EC_STREAM_ERROR_STOPPED
        StErrStPlaying = 0x07,		// EC_STREAM_ERROR_STILLPLAYING
        ErrorStPlaying = 0x08,		// EC_ERROR_STILLPLAYING
        PaletteChanged = 0x09,		// EC_PALETTE_CHANGED
        VideoSizeChanged = 0x0a,		// EC_VIDEO_SIZE_CHANGED
        QualityChange = 0x0b,		// EC_QUALITY_CHANGE
        ShuttingDown = 0x0c,		// EC_SHUTTING_DOWN
        ClockChanged = 0x0d,		// EC_CLOCK_CHANGED
        Paused = 0x0e,		// EC_PAUSED
        OpeningFile = 0x10,		// EC_OPENING_FILE
        BufferingData = 0x11,		// EC_BUFFERING_DATA
        FullScreenLost = 0x12,		// EC_FULLSCREEN_LOST
        Activate = 0x13,		// EC_ACTIVATE
        NeedRestart = 0x14,		// EC_NEED_RESTART
        WindowDestroyed = 0x15,		// EC_WINDOW_DESTROYED
        DisplayChanged = 0x16,		// EC_DISPLAY_CHANGED
        Starvation = 0x17,		// EC_STARVATION
        OleEvent = 0x18,		// EC_OLE_EVENT
        NotifyWindow = 0x19,		// EC_NOTIFY_WINDOW
        StreamControlStopped = 0x1A, // EC_STREAM_CONTROL_STOPPED
        StreamControlStarted = 0x1B, //EC_STREAM_CONTROL_STARTED
        DeviceLost = 0x1f,              // EC_DEVICE_LOST
        ProcessingLatency = 0x21,
        // EC_ ....

        // DVDevCod.h
        DvdDomChange = 0x101,	// EC_DVD_DOMAIN_CHANGE
        DvdTitleChange = 0x102,	// EC_DVD_TITLE_CHANGE
        DvdChaptStart = 0x103,	// EC_DVD_CHAPTER_START
        DvdAudioStChange = 0x104,	// EC_DVD_AUDIO_STREAM_CHANGE

        DvdSubPicStChange = 0x105,	// EC_DVD_SUBPICTURE_STREAM_CHANGE
        DvdAngleChange = 0x106,	// EC_DVD_ANGLE_CHANGE
        DvdButtonChange = 0x107,	// EC_DVD_BUTTON_CHANGE
        DvdValidUopsChange = 0x108,	// EC_DVD_VALID_UOPS_CHANGE
        DvdStillOn = 0x109,	// EC_DVD_STILL_ON
        DvdStillOff = 0x10a,	// EC_DVD_STILL_OFF
        DvdCurrentTime = 0x10b,	// EC_DVD_CURRENT_TIME
        DvdError = 0x10c,	// EC_DVD_ERROR
        DvdWarning = 0x10d,	// EC_DVD_WARNING
        DvdChaptAutoStop = 0x10e,	// EC_DVD_CHAPTER_AUTOSTOP
        DvdNoFpPgc = 0x10f,	// EC_DVD_NO_FP_PGC
        DvdPlaybRateChange = 0x110,	// EC_DVD_PLAYBACK_RATE_CHANGE
        DvdParentalLChange = 0x111,	// EC_DVD_PARENTAL_LEVEL_CHANGE
        DvdPlaybStopped = 0x112,	// EC_DVD_PLAYBACK_STOPPED
        DvdAnglesAvail = 0x113,	// EC_DVD_ANGLES_AVAILABLE
        DvdPeriodAStop = 0x114,	// EC_DVD_PLAYPERIOD_AUTOSTOP
        DvdButtonAActivated = 0x115,	// EC_DVD_BUTTON_AUTO_ACTIVATED
        DvdCmdStart = 0x116,	// EC_DVD_CMD_START
        DvdCmdEnd = 0x117,	// EC_DVD_CMD_END
        DvdDiscEjected = 0x118,	// EC_DVD_DISC_EJECTED
        DvdDiscInserted = 0x119,	// EC_DVD_DISC_INSERTED
        DvdCurrentHmsfTime = 0x11a,	// EC_DVD_CURRENT_HMSF_TIME
        DvdKaraokeMode = 0x11b		// EC_DVD_KARAOKE_MODE
    }

    [Flags, ComVisible( false )]
    internal enum VideoControlFlags
    {
        FlipHorizontal        = 0x0001,
        FlipVertical          = 0x0002,
        ExternalTriggerEnable = 0x0004,
        Trigger               = 0x0008
    }

    /// <summary>
    /// Specifies a filter's state or the state of the filter graph.
    /// </summary>
    public enum FilterState
    {
        /// <summary>
        /// Stopped. The filter is not processing data.
        /// </summary>
        State_Stopped,

        /// <summary>
        /// Paused. The filter is processing data, but not rendering it.
        /// </summary>
        State_Paused,

        /// <summary>
        /// Running. The filter is processing and rendering data.
        /// </summary>
        State_Running
    }
}
