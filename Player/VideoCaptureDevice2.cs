
//
//

namespace CleanedProject
{
    using CleanedProject.Internals;
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Threading;
    using System.Runtime.InteropServices;
    using System.Collections.Generic;

    public class VideoCaptureDevice2
    {
        /*private string deviceMoniker = "HD WebCam";
        private int deviceWidth = 1920;
        private int deviceHeight = 1080;
        private int framesReceived;
        private long bytesReceived;

        private Thread thread = null;
        private ManualResetEvent stopEvent = null;

        private bool needToSetVideoInput = false;

        private object sourceObject = null;
        
        private DateTime startTime = new DateTime( );

        private object sync = new object( );

        // AsyncVideoSource

        private readonly VideoCaptureDevice nestedVideoSource = null;
        private Bitmap lastVideoFrame = null;

        private Thread imageProcessingThread = null;
        private AutoResetEvent isNewFrameAvailable = null;
        private AutoResetEvent isProcessingThreadAvailable = null;

        private bool skipFramesIfBusy = false;
        private int framesProcessed;

        private VideoInput[] crossbarVideoInputs = null;
        private VideoInput crossbarVideoInput = VideoInput.Default;
        private static Dictionary<string, VideoInput[]> cacheCrossbarVideoInputs = new Dictionary<string, VideoInput[]>();

        private VideoCapabilities[] videoCapabilities;
        private static Dictionary<string, VideoCapabilities[]> cacheVideoCapabilities = new Dictionary<string, VideoCapabilities[]>();

        private VideoCapabilities videoResolution = null;

        // End

        public event NewFrameEventHandler NewFrame;


        /// AsyncVideoSource
        /// 
        /// public event VideoSourceErrorEventHandler VideoSourceError
        /// {
        ///     add { nestedVideoSource.VideoSourceError += value; }
        ///     remove { nestedVideoSource.VideoSourceError -= value; }
        /// }

        public event VideoSourceErrorEventHandler VideoSourceError;

        /// AsyncVideoSource
        /// 
        /// public event PlayingFinishedEventHandler PlayingFinished
        /// {
        ///     add { nestedVideoSource.PlayingFinished += value; }
        ///     remove { nestedVideoSource.PlayingFinished -= value; }
        /// }

        public event PlayingFinishedEventHandler PlayingFinished;

        /// AsyncVideoSource
        
        public VideoCaptureDevice NestedVideoSource
        {
            get { return nestedVideoSource; }
        }

        public VideoCapabilities VideoResolution
        {
            get { return videoResolution; }
            set { videoResolution = value; }
        }

        public bool SkipFramesIfBusy
        {
            get { return skipFramesIfBusy; }
            set { skipFramesIfBusy = value; }
        }

        public virtual string Source
        {
            get { return deviceMoniker; }
            set
            {
                deviceMoniker = value;

                videoCapabilities = null;
                crossbarVideoInputs = null;
            }
        }

        public int FramesReceived
        {
            get
            {
                int frames = framesReceived;
                framesReceived = 0;
                return frames;
            }
        }

        public long BytesReceived
        {
            get
            {
                long bytes = bytesReceived;
                bytesReceived = 0;
                return bytes;
            }
        }

        public bool IsRunning
        {
            get
            {
                if ( thread != null )
                {
                    if ( thread.Join( 0 ) == false )
                        return true;

                    Free( );
                }
                return false;
            }
        }

        public VideoInput[] AvailableCrossbarVideoInputs
        {
            get
            {
                if (crossbarVideoInputs == null)
                {
                    lock (cacheCrossbarVideoInputs)
                    {
                        if ((!string.IsNullOrEmpty(deviceMoniker)) && (cacheCrossbarVideoInputs.ContainsKey(deviceMoniker)))
                        {
                            crossbarVideoInputs = cacheCrossbarVideoInputs[deviceMoniker];
                        }
                    }

                    if (crossbarVideoInputs == null)
                    {
                        if (!IsRunning)
                        {
                            // create graph without playing to collect available inputs
                            WorkerThread(false);
                        }
                        else
                        {
                            for (int i = 0; (i < 500) && (crossbarVideoInputs == null); i++)
                            {
                                Thread.Sleep(10);
                            }
                        }
                    }
                }
                // don't return null even if capabilities are not provided for some reason
                return (crossbarVideoInputs != null) ? crossbarVideoInputs : new VideoInput[0];
            }
        }

        private VideoInput[] ColletCrossbarVideoInputs(IAMCrossbar crossbar)
        {
            lock (cacheCrossbarVideoInputs)
            {
                if (cacheCrossbarVideoInputs.ContainsKey(deviceMoniker))
                {
                    return cacheCrossbarVideoInputs[deviceMoniker];
                }

                List<VideoInput> videoInputsList = new List<VideoInput>();

                if (crossbar != null)
                {
                    int inPinsCount, outPinsCount;

                    // gen number of pins in the crossbar
                    if (crossbar.get_PinCounts(out outPinsCount, out inPinsCount) == 0)
                    {
                        // collect all video inputs
                        for (int i = 0; i < inPinsCount; i++)
                        {
                            int pinIndexRelated;
                            PhysicalConnectorType type;

                            if (crossbar.get_CrossbarPinInfo(true, i, out pinIndexRelated, out type) != 0)
                                continue;

                            if (type < PhysicalConnectorType.AudioTuner)
                            {
                                videoInputsList.Add(new VideoInput(i, type));
                            }
                        }
                    }
                }

                VideoInput[] videoInputs = new VideoInput[videoInputsList.Count];
                videoInputsList.CopyTo(videoInputs);

                cacheCrossbarVideoInputs.Add(deviceMoniker, videoInputs);

                return videoInputs;
            }
        }

        // Get type of input connected to video output of the crossbar
        private VideoInput GetCurrentCrossbarInput(IAMCrossbar crossbar)
        {
            VideoInput videoInput = VideoInput.Default;

            int inPinsCount, outPinsCount;

            // gen number of pins in the crossbar
            if (crossbar.get_PinCounts(out outPinsCount, out inPinsCount) == 0)
            {
                int videoOutputPinIndex = -1;
                int pinIndexRelated;
                PhysicalConnectorType type;

                // find index of the video output pin
                for (int i = 0; i < outPinsCount; i++)
                {
                    if (crossbar.get_CrossbarPinInfo(false, i, out pinIndexRelated, out type) != 0)
                        continue;

                    if (type == PhysicalConnectorType.VideoDecoder)
                    {
                        videoOutputPinIndex = i;
                        break;
                    }
                }

                if (videoOutputPinIndex != -1)
                {
                    int videoInputPinIndex;

                    // get index of the input pin connected to the output
                    if (crossbar.get_IsRoutedTo(videoOutputPinIndex, out videoInputPinIndex) == 0)
                    {
                        PhysicalConnectorType inputType;

                        crossbar.get_CrossbarPinInfo(true, videoInputPinIndex, out pinIndexRelated, out inputType);

                        videoInput = new VideoInput(videoInputPinIndex, inputType);
                    }
                }
            }

            return videoInput;
        }

        // Set type of input connected to video output of the crossbar
        private void SetCurrentCrossbarInput(IAMCrossbar crossbar, VideoInput videoInput)
        {
            if (videoInput.Type != PhysicalConnectorType.Default)
            {
                int inPinsCount, outPinsCount;

                // gen number of pins in the crossbar
                if (crossbar.get_PinCounts(out outPinsCount, out inPinsCount) == 0)
                {
                    int videoOutputPinIndex = -1;
                    int videoInputPinIndex = -1;
                    int pinIndexRelated;
                    PhysicalConnectorType type;

                    // find index of the video output pin
                    for (int i = 0; i < outPinsCount; i++)
                    {
                        if (crossbar.get_CrossbarPinInfo(false, i, out pinIndexRelated, out type) != 0)
                            continue;

                        if (type == PhysicalConnectorType.VideoDecoder)
                        {
                            videoOutputPinIndex = i;
                            break;
                        }
                    }

                    // find index of the required input pin
                    for (int i = 0; i < inPinsCount; i++)
                    {
                        if (crossbar.get_CrossbarPinInfo(true, i, out pinIndexRelated, out type) != 0)
                            continue;

                        if ((type == videoInput.Type) && (i == videoInput.Index))
                        {
                            videoInputPinIndex = i;
                            break;
                        }
                    }

                    // try connecting pins
                    if ((videoInputPinIndex != -1) && (videoOutputPinIndex != -1) &&
                         (crossbar.CanRoute(videoOutputPinIndex, videoInputPinIndex) == 0))
                    {
                        crossbar.Route(videoOutputPinIndex, videoInputPinIndex);
                    }
                }
            }
        }

        public CleanedProject.VideoCapabilities[] VideoCapabilities
        {
            get
            {
                if (videoCapabilities == null)
                {
                    lock (cacheVideoCapabilities)
                    {
                        if ((!string.IsNullOrEmpty(deviceMoniker)) && (cacheVideoCapabilities.ContainsKey(deviceMoniker)))
                        {
                            videoCapabilities = cacheVideoCapabilities[deviceMoniker];
                        }
                    }

                    if (videoCapabilities == null)
                    {
                        if (!IsRunning)
                        {
                            // create graph without playing to get the video/snapshot capabilities only.
                            // not very clean but it works
                            WorkerThread(false);
                        }
                        else
                        {
                            for (int i = 0; (i < 500) && (videoCapabilities == null); i++)
                            {
                                Thread.Sleep(10);
                            }
                        }
                    }
                }
                // don't return null even capabilities are not provided for some reason
                return (videoCapabilities != null) ? videoCapabilities : new VideoCapabilities[0];
            }
        }

        [Obsolete]
        public Size DesiredFrameSize
        {
            get { return Size.Empty; }
            set { }
        }

        [Obsolete]
        public Size DesiredSnapshotSize
        {
            get { return Size.Empty; }
            set { }
        }

        [Obsolete]
        public int DesiredFrameRate
        {
            get { return 0; }
            set { }
        }

        public object SourceObject
        {
            get { return sourceObject; }
        }

        public void Start( )
        {
            if (!IsRunning)
            {
                framesProcessed = 0;

                // create all synchronization events
                isNewFrameAvailable = new AutoResetEvent(false);
                isProcessingThreadAvailable = new AutoResetEvent(true);

                // create image processing thread
                imageProcessingThread = new Thread(new ThreadStart(imageProcessingThread_Worker));
                imageProcessingThread.Start();

                // start the nested video source
                //nestedVideoSource.NewFrame += new NewFrameEventHandler(nestedVideoSource_NewFrame);
                //nestedVideoSource.StartBis();
            }
        }

        public void StartBis()
        {
            if (!IsRunning)
            {
                // check source
                if (string.IsNullOrEmpty(deviceMoniker))
                    throw new ArgumentException("Video source is not specified.");

                framesReceived = 0;
                bytesReceived = 0;
                needToSetVideoInput = true;

                // create events
                stopEvent = new ManualResetEvent(false);

                lock (sync)
                {
                    // create and start new thread
                    thread = new Thread(new ThreadStart(WorkerThread));
                    thread.Name = deviceMoniker; // mainly for debugging
                    thread.Start();
                }
            }
        }

        public void Stop( )
        {
            if ( this.IsRunning )
            {
                thread.Abort( );
                thread.Join();
                Free();
            }
        }

        private void Free( )
        {
            thread = null;

            stopEvent.Close( );
            stopEvent = null;

            /// AsyncVideoSource

            if (imageProcessingThread != null)
            {
                nestedVideoSource.NewFrame -= new NewFrameEventHandler(nestedVideoSource_NewFrame);

                isProcessingThreadAvailable.WaitOne();
                lastVideoFrame = null;
                isNewFrameAvailable.Set();
                imageProcessingThread.Join();
                imageProcessingThread = null;

                isNewFrameAvailable.Close();
                isNewFrameAvailable = null;

                isProcessingThreadAvailable.Close();
                isProcessingThreadAvailable = null;
            }
        }

        /// AsyncVideoSource
        
        private void nestedVideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (NewFrame == null)
                return;

            if (skipFramesIfBusy)
            {
                if (!isProcessingThreadAvailable.WaitOne(0, false))
                {
                    return;
                }
            }
            else
            {
                isProcessingThreadAvailable.WaitOne();
            }

            lastVideoFrame = CloneImage(eventArgs.Frame);
            isNewFrameAvailable.Set();
        }

        private void imageProcessingThread_Worker()
        {
            while (true)
            {
                isNewFrameAvailable.WaitOne();

                if (lastVideoFrame == null)
                {
                    break;
                }

                if (NewFrame != null)
                {
                    NewFrame(this, new NewFrameEventArgs(lastVideoFrame));
                }

                lastVideoFrame.Dispose();
                lastVideoFrame = null;
                framesProcessed++;

                isProcessingThreadAvailable.Set();
            }
        }

        private static Bitmap CloneImage(Bitmap source)
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


            destination.UnlockBits(destinationData);

            return destination;
        }

        /// End

        public bool SetCameraProperty( CameraControlProperty property, int value, CameraControlFlags controlFlags )
        {
            bool ret = true;

            if ( ( deviceMoniker == null ) || ( string.IsNullOrEmpty( deviceMoniker ) ) )
            {
                throw new ArgumentException( "Video source is not specified." );
            }

            lock ( sync )
            {
                object tempSourceObject = null;

                try
                {
                    tempSourceObject = FilterInfo.CreateFilter( deviceMoniker );
                }
                catch
                {
                    throw new ApplicationException( "Failed creating device object for moniker." );
                }

                if ( !( tempSourceObject is IAMCameraControl ) )
                {
                    throw new NotSupportedException( "The video source does not support camera control." );
                }

                IAMCameraControl pCamControl = (IAMCameraControl) tempSourceObject;
                int hr = pCamControl.Set( property, value, controlFlags );

                ret = ( hr >= 0 );

                Marshal.ReleaseComObject( tempSourceObject );
            }

            return ret;
        }

        public bool GetCameraProperty( CameraControlProperty property, out int value, out CameraControlFlags controlFlags )
        {
            bool ret = true;

            if ( ( deviceMoniker == null ) || ( string.IsNullOrEmpty( deviceMoniker ) ) )
            {
                throw new ArgumentException( "Video source is not specified." );
            }

            lock ( sync )
            {
                object tempSourceObject = null;

                try
                {
                    tempSourceObject = FilterInfo.CreateFilter( deviceMoniker );
                }
                catch
                {
                    throw new ApplicationException( "Failed creating device object for moniker." );
                }

                if ( !( tempSourceObject is IAMCameraControl ) )
                {
                    throw new NotSupportedException( "The video source does not support camera control." );
                }

                IAMCameraControl pCamControl = (IAMCameraControl) tempSourceObject;
                int hr = pCamControl.Get( property, out value, out controlFlags );

                ret = ( hr >= 0 );

                Marshal.ReleaseComObject( tempSourceObject );
            }

            return ret;
        }

        public bool GetCameraPropertyRange( CameraControlProperty property, out int minValue, out int maxValue, out int stepSize, out int defaultValue, out CameraControlFlags controlFlags )
        {
            bool ret = true;

            if ( ( deviceMoniker == null ) || ( string.IsNullOrEmpty( deviceMoniker ) ) )
            {
                throw new ArgumentException( "Video source is not specified." );
            }

            lock ( sync )
            {
                object tempSourceObject = null;

                try
                {
                    tempSourceObject = FilterInfo.CreateFilter( deviceMoniker );
                }
                catch
                {
                    throw new ApplicationException( "Failed creating device object for moniker." );
                }

                if ( !( tempSourceObject is IAMCameraControl ) )
                {
                    throw new NotSupportedException( "The video source does not support camera control." );
                }

                IAMCameraControl pCamControl = (IAMCameraControl) tempSourceObject;
                int hr = pCamControl.GetRange( property, out minValue, out maxValue, out stepSize, out defaultValue, out controlFlags );

                ret = ( hr >= 0 );

                Marshal.ReleaseComObject( tempSourceObject );
            }

            return ret;
        }

        private void WorkerThread( )
        {
            WorkerThread( true );
        }

        private void WorkerThread(bool runGraph)
        {
            ReasonToFinishPlaying reasonToStop = ReasonToFinishPlaying.StoppedByUser;

            Grabber videoGrabber = new Grabber(this);

            object captureGraphObject = null;
            object graphObject = null;
            object videoGrabberObject = null;

            ICaptureGraphBuilder2 captureGraph = null;
            IFilterGraph2 graph = null;
            IBaseFilter sourceBase = null;
            IBaseFilter videoGrabberBase = null;
            ISampleGrabber videoSampleGrabber = null;
            IMediaControl mediaControl = null;
            IAMVideoControl videoControl = null;
            IMediaEventEx mediaEvent = null;
            IPin pinStillImage = null;

            try
            {
                Type type = Type.GetTypeFromCLSID(Clsid.CaptureGraphBuilder2);
                if (type == null)
                    throw new ApplicationException("Failed creating capture graph builder");

                captureGraphObject = Activator.CreateInstance(type);
                captureGraph = (ICaptureGraphBuilder2)captureGraphObject;

                type = Type.GetTypeFromCLSID(Clsid.FilterGraph);
                if (type == null)
                    throw new ApplicationException("Failed creating filter graph");

                graphObject = Activator.CreateInstance(type);
                graph = (IFilterGraph2)graphObject;

                captureGraph.SetFiltergraph((IGraphBuilder)graph);

                sourceObject = FilterInfo.CreateFilter(deviceMoniker);
                if (sourceObject == null)
                    throw new ApplicationException("Failed creating device object for moniker");

                sourceBase = (IBaseFilter)sourceObject;

                try
                {
                    videoControl = (IAMVideoControl)sourceObject;
                }
                catch
                {
                }

                type = Type.GetTypeFromCLSID(Clsid.SampleGrabber);
                if (type == null)
                    throw new ApplicationException("Failed creating sample grabber");

                videoGrabberObject = Activator.CreateInstance(type);
                videoSampleGrabber = (ISampleGrabber)videoGrabberObject;
                videoGrabberBase = (IBaseFilter)videoGrabberObject;

                graph.AddFilter(sourceBase, "source");
                graph.AddFilter(videoGrabberBase, "grabber_video");

                AMMediaType mediaType = new AMMediaType();
                mediaType.MajorType = MediaType.Video;
                mediaType.SubType = MediaSubType.RGB24;

                videoSampleGrabber.SetMediaType(mediaType);

                GetPinCapabilitiesAndConfigureSizeAndRate(captureGraph, sourceBase,
                    PinCategory.Capture, videoResolution, ref videoCapabilities);

                lock (cacheVideoCapabilities)
                {
                    if ((videoCapabilities != null) && (!cacheVideoCapabilities.ContainsKey(deviceMoniker)))
                    {
                        cacheVideoCapabilities.Add(deviceMoniker, videoCapabilities);
                    }
                }

                if (videoControl != null)
                {
                    captureGraph.FindPin(sourceObject, PinDirection.Output,
                        PinCategory.StillImage, MediaType.Video, false, 0, out pinStillImage);
                    if (pinStillImage != null)
                    {
                        VideoControlFlags caps;
                        videoControl.GetCaps(pinStillImage, out caps);
                    }
                }

                videoSampleGrabber.SetBufferSamples(false);
                videoSampleGrabber.SetOneShot(false);
                videoSampleGrabber.SetCallback(videoGrabber, 1);
                
                if (runGraph)
                {
                    captureGraph.RenderStream(PinCategory.Capture, MediaType.Video, sourceBase, null, videoGrabberBase);

                    if (videoSampleGrabber.GetConnectedMediaType(mediaType) == 0)
                    {
                        VideoInfoHeader vih = (VideoInfoHeader)Marshal.PtrToStructure(mediaType.FormatPtr, typeof(VideoInfoHeader));

                        videoGrabber.Width = vih.BmiHeader.Width;
                        videoGrabber.Height = vih.BmiHeader.Height;

                        mediaType.Dispose();
                    }

                    mediaControl = (IMediaControl)graphObject;

                    mediaEvent = (IMediaEventEx)graphObject;
                    IntPtr p1, p2;
                    DsEvCode code;

                    mediaControl.Run();

                    do
                    {
                        if (mediaEvent != null)
                        {
                            if (mediaEvent.GetEvent(out code, out p1, out p2, 0) >= 0)
                            {
                                mediaEvent.FreeEventParams(code, p1, p2);

                                if (code == DsEvCode.DeviceLost)
                                {
                                    reasonToStop = ReasonToFinishPlaying.DeviceLost;
                                    break;
                                }
                            }
                        }
                    }
                    while (!stopEvent.WaitOne(100, false));

                    mediaControl.Stop();
                }
            }
            catch (Exception exception)
            {
                if (VideoSourceError != null)
                {
                    VideoSourceError(this, new VideoSourceErrorEventArgs(exception.Message));
                }
            }
            finally
            {
                captureGraph = null;
                graph = null;
                sourceBase = null;
                mediaControl = null;
                videoControl = null;
                mediaEvent = null;
                pinStillImage = null;

                videoGrabberBase = null;
                videoSampleGrabber = null;

                if (graphObject != null)
                {
                    Marshal.ReleaseComObject(graphObject);
                    graphObject = null;
                }
                if (sourceObject != null)
                {
                    Marshal.ReleaseComObject(sourceObject);
                    sourceObject = null;
                }
                if (videoGrabberObject != null)
                {
                    Marshal.ReleaseComObject(videoGrabberObject);
                    videoGrabberObject = null;
                }
                if (captureGraphObject != null)
                {
                    Marshal.ReleaseComObject(captureGraphObject);
                    captureGraphObject = null;
                }
            }

            if (PlayingFinished != null)
            {
                PlayingFinished(this, reasonToStop);
            }
        }

        private void SetResolution(IAMStreamConfig streamConfig, VideoCapabilities resolution)
        {
            if (resolution == null)
            {
                return;
            }

            // iterate through device's capabilities to find mediaType for desired resolution
            int capabilitiesCount = 0, capabilitySize = 0;
            AMMediaType newMediaType = null;
            VideoStreamConfigCaps caps = new VideoStreamConfigCaps();

            streamConfig.GetNumberOfCapabilities(out capabilitiesCount, out capabilitySize);

            for (int i = 0; i < capabilitiesCount; i++)
            {
                try
                {
                    VideoCapabilities vc = new VideoCapabilities(streamConfig, i);

                    if (resolution == vc)
                    {
                        if (streamConfig.GetStreamCaps(i, out newMediaType, caps) == 0)
                        {
                            break;
                        }
                    }
                }
                catch
                {
                }
            }

            // set the new format
            if (newMediaType != null)
            {
                streamConfig.SetFormat(newMediaType);
                newMediaType.Dispose();
            }
        }

        private void GetPinCapabilitiesAndConfigureSizeAndRate(ICaptureGraphBuilder2 graphBuilder, IBaseFilter baseFilter,
            Guid pinCategory, VideoCapabilities resolutionToSet, ref VideoCapabilities[] capabilities)
        {
            object streamConfigObject;
            graphBuilder.FindInterface(pinCategory, MediaType.Video, baseFilter, typeof(IAMStreamConfig).GUID, out streamConfigObject);

            if (streamConfigObject != null)
            {
                IAMStreamConfig streamConfig = null;

                try
                {
                    streamConfig = (IAMStreamConfig)streamConfigObject;
                }
                catch (InvalidCastException)
                {
                }

                if (streamConfig != null)
                {
                    if (capabilities == null)
                    {
                        try
                        {
                            // get all video capabilities
                            capabilities = CleanedProject.VideoCapabilities.FromStreamConfig(streamConfig);
                        }
                        catch
                        {
                        }
                    }

                    // check if it is required to change capture settings
                    if (resolutionToSet != null)
                    {
                        SetResolution(streamConfig, resolutionToSet);
                    }
                }

                Marshal.ReleaseComObject(streamConfigObject);
            }

            // if failed resolving capabilities, then just create empty capabilities array,
            // so we don't try again
            if (capabilities == null)
            {
                capabilities = new VideoCapabilities[0];
            }
        }

        private void OnNewFrame( Bitmap image )
        {
            framesReceived++;
            bytesReceived += image.Width * image.Height * ( Bitmap.GetPixelFormatSize( image.PixelFormat ) >> 3 );

            if ( ( !stopEvent.WaitOne( 0, false ) ) && ( NewFrame != null ) )
                NewFrame( this, new NewFrameEventArgs( image ) );
        }

        private object getCOMInterfaceFromGuid(Guid comGuid)
        {
            // ICustomQueryInterface.GetInterface(Guid guid, IntPtr ptr)
            return null;
        }

        private class Grabber : ISampleGrabberCB
        {
            private VideoCaptureDevice parent;
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

            public Grabber( VideoCaptureDevice parent )
            {
                this.parent = parent;
            }

            public int SampleCB( double sampleTime, IntPtr sample )
            {
                return 0;
            }

            public int BufferCB( double sampleTime, IntPtr buffer, int bufferLen )
            {
                if ( parent.NewFrame != null )
                {
                    System.Drawing.Bitmap image = new Bitmap( width, height, PixelFormat.Format24bppRgb );

                    BitmapData imageData = image.LockBits(
                        new Rectangle( 0, 0, width, height ),
                        ImageLockMode.ReadWrite,
                        PixelFormat.Format24bppRgb );

                    int srcStride = imageData.Stride;
                    int dstStride = imageData.Stride;

                    unsafe
                    {
                        byte* dst = (byte*) imageData.Scan0.ToPointer( ) + dstStride * ( height - 1 );
                        byte* src = (byte*) buffer.ToPointer( );

                        for ( int y = 0; y < height; y++ )
                        {
                            Win32.memcpy( dst, src, srcStride );
                            dst -= dstStride;
                            src += srcStride;
                        }
                    }

                    image.UnlockBits( imageData );
                    
                    parent.OnNewFrame( image );

                    image.Dispose( );
                }

                return 0;
            }
        }*/
    }
}
