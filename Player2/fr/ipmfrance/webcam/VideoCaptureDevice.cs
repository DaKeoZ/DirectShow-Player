using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Runtime.InteropServices;
using fr.ipmfrance.webcam.com;

namespace fr.ipmfrance.webcam
{
    public class VideoCaptureDevice
    {
        private string deviceMoniker;
        private int framesReceived;
        private long bytesReceived;
        private Thread thread = null;
        private ManualResetEvent stopEvent = null;
        private bool needToSetVideoInput = false;
        private bool needToSimulateTrigger = false;
        private bool needToDisplayPropertyPage = false;
        private bool needToDisplayCrossBarPropertyPage = false;
        private IntPtr parentWindowForPropertyPage = IntPtr.Zero;
        private object sourceObject = null;
        private object sync = new object();
        private bool? isCrossbarAvailable = null;
        private VideoInput[] crossbarVideoInputs = null;
        private VideoInput crossbarVideoInput = VideoInput.Default;
        private static Dictionary<string, VideoInput[]> cacheCrossbarVideoInputs = new Dictionary<string, VideoInput[]>();

        public event NewFrameEventHandler NewFrame;

        public event VideoSourceErrorEventHandler VideoSourceError;

        public event PlayingFinishedEventHandler PlayingFinished;

        public virtual string Source
        {
            get { return deviceMoniker; }
            set
            {
                deviceMoniker = value;
                crossbarVideoInputs = null;
                isCrossbarAvailable = null;
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
                if (thread != null)
                {
                    if (thread.Join(0) == false)
                        return true;

                    Free();
                }
                return false;
            }
        }

        public VideoCaptureDevice(string deviceMoniker)
        {
            this.deviceMoniker = deviceMoniker;
        }

        public void Start()
        {
            if (!IsRunning)
            {
                if (string.IsNullOrEmpty(deviceMoniker))
                {
                    throw new ArgumentException("Video source is not specified.");
                }

                framesReceived = 0;
                bytesReceived = 0;
                isCrossbarAvailable = null;
                needToSetVideoInput = true;

                stopEvent = new ManualResetEvent(false);

                lock (sync)
                {
                    thread = new Thread(new ThreadStart(WorkerThread));
                    thread.Name = deviceMoniker;
                    thread.Start();
                }
            }
        }

        public void SignalToStop()
        {
            if (thread != null)
            {
                stopEvent.Set();
            }
        }

        public void WaitForStop()
        {
            if (thread != null)
            {
                thread.Join();

                Free();
            }
        }

        public void Stop()
        {
            if (this.IsRunning)
            {
                thread.Abort();
                WaitForStop();
            }
        }

        private void Free()
        {
            thread = null;

            stopEvent.Close();
            stopEvent = null;
        }

        public void DisplayPropertyPage(IntPtr parentWindow)
        {
            if ((deviceMoniker == null) || (deviceMoniker == string.Empty))
                throw new ArgumentException("Video source is not specified.");

            lock (sync)
            {
                if (IsRunning)
                {
                    parentWindowForPropertyPage = parentWindow;
                    needToDisplayPropertyPage = true;
                    return;
                }

                object tempSourceObject = null;

                try
                {
                    tempSourceObject = FilterInfo.CreateFilter(deviceMoniker);
                }
                catch
                {
                    throw new ApplicationException("Failed creating device object for moniker.");
                }

                if (!(tempSourceObject is ISpecifyPropertyPages))
                {
                    throw new NotSupportedException("The video source does not support configuration property page.");
                }

                DisplayPropertyPage(parentWindow, tempSourceObject);

                Marshal.ReleaseComObject(tempSourceObject);
            }
        }

        public void DisplayCrossbarPropertyPage(IntPtr parentWindow)
        {
            lock (sync)
            {
                for (int i = 0; (i < 500) && (!isCrossbarAvailable.HasValue) && (IsRunning); i++)
                {
                    Thread.Sleep(10);
                }

                if ((!IsRunning) || (!isCrossbarAvailable.HasValue))
                {
                    throw new ApplicationException("The video source must be running in order to display crossbar property page.");
                }

                if (!isCrossbarAvailable.Value)
                {
                    throw new NotSupportedException("Crossbar configuration is not supported by currently running video source.");
                }

                parentWindowForPropertyPage = parentWindow;
                needToDisplayCrossBarPropertyPage = true;
            }
        }

        public bool CheckIfCrossbarAvailable()
        {
            lock (sync)
            {
                if (!isCrossbarAvailable.HasValue)
                {
                    if (!IsRunning)
                    {
                        WorkerThread(false);
                    }
                    else
                    {
                        for (int i = 0; (i < 500) && (!isCrossbarAvailable.HasValue); i++)
                        {
                            Thread.Sleep(10);
                        }
                    }
                }

                return (!isCrossbarAvailable.HasValue) ? false : isCrossbarAvailable.Value;
            }
        }

        public void SimulateTrigger()
        {
            needToSimulateTrigger = true;
        }

        public bool SetCameraProperty(CameraControlProperty property, int value, CameraControlFlags controlFlags)
        {
            bool ret = true;

            if ((deviceMoniker == null) || (string.IsNullOrEmpty(deviceMoniker)))
            {
                throw new ArgumentException("Video source is not specified.");
            }

            lock (sync)
            {
                object tempSourceObject = null;

                try
                {
                    tempSourceObject = FilterInfo.CreateFilter(deviceMoniker);
                }
                catch
                {
                    throw new ApplicationException("Failed creating device object for moniker.");
                }

                if (!(tempSourceObject is IAMCameraControl))
                {
                    throw new NotSupportedException("The video source does not support camera control.");
                }

                IAMCameraControl pCamControl = (IAMCameraControl)tempSourceObject;
                int hr = pCamControl.Set(property, value, controlFlags);

                ret = (hr >= 0);

                Marshal.ReleaseComObject(tempSourceObject);
            }

            return ret;
        }

        public bool GetCameraProperty(CameraControlProperty property, out int value, out CameraControlFlags controlFlags)
        {
            bool ret = true;

            if ((deviceMoniker == null) || (string.IsNullOrEmpty(deviceMoniker)))
            {
                throw new ArgumentException("Video source is not specified.");
            }

            lock (sync)
            {
                object tempSourceObject = null;

                try
                {
                    tempSourceObject = FilterInfo.CreateFilter(deviceMoniker);
                }
                catch
                {
                    throw new ApplicationException("Failed creating device object for moniker.");
                }

                if (!(tempSourceObject is IAMCameraControl))
                {
                    throw new NotSupportedException("The video source does not support camera control.");
                }

                IAMCameraControl pCamControl = (IAMCameraControl)tempSourceObject;
                int hr = pCamControl.Get(property, out value, out controlFlags);

                ret = (hr >= 0);

                Marshal.ReleaseComObject(tempSourceObject);
            }

            return ret;
        }

        public bool GetCameraPropertyRange(CameraControlProperty property, out int minValue, out int maxValue, out int stepSize, out int defaultValue, out CameraControlFlags controlFlags)
        {
            bool ret = true;

            if ((deviceMoniker == null) || (string.IsNullOrEmpty(deviceMoniker)))
            {
                throw new ArgumentException("Video source is not specified.");
            }

            lock (sync)
            {
                object tempSourceObject = null;

                try
                {
                    tempSourceObject = FilterInfo.CreateFilter(deviceMoniker);
                }
                catch
                {
                    throw new ApplicationException("Failed creating device object for moniker.");
                }

                if (!(tempSourceObject is IAMCameraControl))
                {
                    throw new NotSupportedException("The video source does not support camera control.");
                }

                IAMCameraControl pCamControl = (IAMCameraControl)tempSourceObject;
                int hr = pCamControl.GetRange(property, out minValue, out maxValue, out stepSize, out defaultValue, out controlFlags);

                ret = (hr >= 0);

                Marshal.ReleaseComObject(tempSourceObject);
            }

            return ret;
        }

        private void WorkerThread()
        {
            WorkerThread(true);
        }

        private void WorkerThread(bool runGraph)
        {
            ReasonToFinishPlaying reasonToStop = ReasonToFinishPlaying.StoppedByUser;

            Grabber videoGrabber = new Grabber(this);

            object captureGraphObject = null;
            object graphObject = null;
            object videoGrabberObject = null;
            object crossbarObject = null;

            ICaptureGraphBuilder2 captureGraph = null;
            IFilterGraph2 graph = null;
            IBaseFilter sourceBase = null;
            IBaseFilter videoGrabberBase = null;
            ISampleGrabber videoSampleGrabber = null;
            IMediaControl mediaControl = null;
            IAMVideoControl videoControl = null;
            IMediaEventEx mediaEvent = null;
            IPin pinStillImage = null;
            IAMCrossbar crossbar = null;

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

                captureGraph.FindInterface(FindDirection.UpstreamOnly, Guid.Empty, sourceBase, typeof(IAMCrossbar).GUID, out crossbarObject);
                if (crossbarObject != null)
                {
                    crossbar = (IAMCrossbar)crossbarObject;
                }
                isCrossbarAvailable = (crossbar != null);
                crossbarVideoInputs = ColletCrossbarVideoInputs(crossbar);

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
                    int p1, p2;
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

                        if (needToSetVideoInput)
                        {
                            needToSetVideoInput = false;
                            if (isCrossbarAvailable.Value)
                            {
                                SetCurrentCrossbarInput(crossbar, crossbarVideoInput);
                                crossbarVideoInput = GetCurrentCrossbarInput(crossbar);
                            }
                        }

                        if (needToSimulateTrigger)
                        {
                            needToSimulateTrigger = false;
                        }

                        if (needToDisplayPropertyPage)
                        {
                            needToDisplayPropertyPage = false;
                            DisplayPropertyPage(parentWindowForPropertyPage, sourceObject);

                            if (crossbar != null)
                            {
                                crossbarVideoInput = GetCurrentCrossbarInput(crossbar);
                            }
                        }

                        if (needToDisplayCrossBarPropertyPage)
                        {
                            needToDisplayCrossBarPropertyPage = false;

                            if (crossbar != null)
                            {
                                DisplayPropertyPage(parentWindowForPropertyPage, crossbar);
                                crossbarVideoInput = GetCurrentCrossbarInput(crossbar);
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
                crossbar = null;

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
                if (crossbarObject != null)
                {
                    Marshal.ReleaseComObject(crossbarObject);
                    crossbarObject = null;
                }
            }

            if (PlayingFinished != null)
            {
                PlayingFinished(this, reasonToStop);
            }
        }

        private void DisplayPropertyPage(IntPtr parentWindow, object sourceObject)
        {
            try
            {
                ISpecifyPropertyPages pPropPages = (ISpecifyPropertyPages)sourceObject;
                CAUUID caGUID;
                pPropPages.GetPages(out caGUID);
                FilterInfo filterInfo = new FilterInfo(deviceMoniker);
                Win32.OleCreatePropertyFrame(parentWindow, 0, 0, filterInfo.Name, 1, ref sourceObject, caGUID.cElems, caGUID.pElems, 0, 0, IntPtr.Zero);
                Marshal.FreeCoTaskMem(caGUID.pElems);
            }
            catch
            {
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

                    if (crossbar.get_PinCounts(out outPinsCount, out inPinsCount) == 0)
                    {
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

        private VideoInput GetCurrentCrossbarInput(IAMCrossbar crossbar)
        {
            VideoInput videoInput = VideoInput.Default;

            int inPinsCount, outPinsCount;

            if (crossbar.get_PinCounts(out outPinsCount, out inPinsCount) == 0)
            {
                int videoOutputPinIndex = -1;
                int pinIndexRelated;
                PhysicalConnectorType type;

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

        private void SetCurrentCrossbarInput(IAMCrossbar crossbar, VideoInput videoInput)
        {
            if (videoInput.Type != PhysicalConnectorType.Default)
            {
                int inPinsCount, outPinsCount;

                if (crossbar.get_PinCounts(out outPinsCount, out inPinsCount) == 0)
                {
                    int videoOutputPinIndex = -1;
                    int videoInputPinIndex = -1;
                    int pinIndexRelated;
                    PhysicalConnectorType type;

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

                    if ((videoInputPinIndex != -1) && (videoOutputPinIndex != -1) &&
                         (crossbar.CanRoute(videoOutputPinIndex, videoInputPinIndex) == 0))
                    {
                        crossbar.Route(videoOutputPinIndex, videoInputPinIndex);
                    }
                }
            }
        }

        private void OnNewFrame(Bitmap image)
        {
            framesReceived++;
            bytesReceived += image.Width * image.Height * (Bitmap.GetPixelFormatSize(image.PixelFormat) >> 3);

            if ((!stopEvent.WaitOne(0, false)) && (NewFrame != null))
                NewFrame(this, new NewFrameEventArgs(image));
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

            public Grabber(VideoCaptureDevice parent)
            {
                this.parent = parent;
            }

            public int SampleCB(double sampleTime, IntPtr sample)
            {
                return 0;
            }

            public int BufferCB(double sampleTime, IntPtr buffer, int bufferLen)
            {
                if (parent.NewFrame != null)
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
                    parent.OnNewFrame(image);

                    image.Dispose();
                }

                return 0;
            }
        }
    }
}
