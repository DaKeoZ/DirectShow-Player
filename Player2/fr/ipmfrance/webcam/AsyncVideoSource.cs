using fr.ipmfrance.webcam.com;
using fr.ipmfrance.webcam.tools;
using fr.ipmfrance.webcam.win32;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;

namespace fr.ipmfrance.webcam
{

    public class AsyncVideoSource
    {
        private Bitmap lastVideoFrame = null;
        private Thread imageProcessingThread = null;
        private AutoResetEvent isNewFrameAvailable = null;
        private AutoResetEvent isProcessingThreadAvailable = null;
        private bool skipFramesIfBusy = false;
        private string deviceMonikerCapture;
        private int framesReceivedCapture;
        private Thread threadCapture = null;
        private ManualResetEvent stopEventCapture = null;
        private object sourceObjectCapture = null;
        private object syncCapture = new object();


        public event NewFrameEventHandler NewFrame;

        public event VideoSourceErrorEventHandler VideoSourceError;

        public event PlayingFinishedEventHandler PlayingFinished;

        public AsyncVideoSource(FilterInfo filterInfo)
        {
            deviceMonikerCapture = filterInfo.MonikerString;
        }

        public bool IsRunning
        {
            get
            {
                bool isRunning = IsRunningCapture;

                if (!isRunning)
                {
                    Free();
                }

                return isRunning;
            }
        }

        public void Start()
        {
            if (!IsRunning)
            {

                isNewFrameAvailable = new AutoResetEvent(false);
                isProcessingThreadAvailable = new AutoResetEvent(true);

                imageProcessingThread = new Thread(new ThreadStart(imageProcessingThread_Worker));
                imageProcessingThread.Start();

                NewFrameCapture += new NewFrameEventHandler(nestedVideoSource_NewFrame);
                StartCapture();
            }
        }

        public void SignalToStop()
        {
            SignalToStopCapture();
            Free();
        }

        public void WaitForStop()
        {
            WaitForStopCapture();
            Free();
        }

        public void Stop()
        {
            StopCapture();
            Free();
        }

        private void Free()
        {
            if (imageProcessingThread != null)
            {
                NewFrameCapture -= new NewFrameEventHandler(nestedVideoSource_NewFrame);

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

            lastVideoFrame = BitmapHelper.CloneImage(eventArgs.Frame);
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

                isProcessingThreadAvailable.Set();
            }
        }

        public int FramesReceived
        {
            get
            {
                int frames = framesReceivedCapture;
                framesReceivedCapture = 0;
                return frames;
            }
        }

        public bool IsRunningCapture
        {
            get
            {
                if (threadCapture != null)
                {
                    if (threadCapture.Join(0) == false)
                    {
                        return true;
                    }
                    FreeCapture();
                }
                return false;
            }
        }


        public void StartCapture()
        {
            if (!IsRunningCapture)
            {
                if (string.IsNullOrEmpty(deviceMonikerCapture))
                {
                    throw new ArgumentException("Video source is not specified.");
                }

                framesReceivedCapture = 0;

                stopEventCapture = new ManualResetEvent(false);

                lock (syncCapture)
                {
                    threadCapture = new Thread(new ThreadStart(WorkerThread));
                    threadCapture.Name = deviceMonikerCapture;
                    threadCapture.Start();
                }
            }
        }

        public void SignalToStopCapture()
        {
            if (threadCapture != null)
            {
                stopEventCapture.Set();
            }
        }

        public void WaitForStopCapture()
        {
            if (threadCapture != null)
            {
                threadCapture.Join();
                FreeCapture();
            }
        }

        public void StopCapture()
        {
            if (this.IsRunningCapture)
            {
                threadCapture.Abort();
                WaitForStopCapture();
            }
        }

        private void FreeCapture()
        {
            threadCapture = null;
            stopEventCapture.Close();
            stopEventCapture = null;
        }

        private void WorkerThread()
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
            //IPin pinStillImage = null;
            try
            {
                captureGraphObject = ComFactory.Create(Clsid.CaptureGraphBuilder2);
                captureGraph = (ICaptureGraphBuilder2)captureGraphObject;

                graphObject = ComFactory.Create(Clsid.FilterGraph);
                graph = (IFilterGraph2)graphObject;

                captureGraph.SetFiltergraph((IGraphBuilder)graph);

                sourceObjectCapture = FilterInfo.CreateFilter(deviceMonikerCapture);
                if (sourceObjectCapture == null)
                {
                    throw new ApplicationException("Failed creating device object for moniker");
                }

                sourceBase = (IBaseFilter)sourceObjectCapture;

                try
                {
                    videoControl = (IAMVideoControl)sourceObjectCapture;
                }
                catch
                {
                }

                videoGrabberObject = ComFactory.Create(Clsid.SampleGrabber);

                videoSampleGrabber = (ISampleGrabber)videoGrabberObject;
                videoGrabberBase = (IBaseFilter)videoGrabberObject;

                graph.AddFilter(sourceBase, "source");
                graph.AddFilter(videoGrabberBase, "grabber_video");

                AMMediaType mediaType = SetVideoRGB24(videoSampleGrabber);

                //if (videoControl != null)
                //{
                //    captureGraph.FindPin(sourceObjectCapture, PinDirection.Output,
                //        PinCategory.StillImage, MediaType.Video, false, 0, out pinStillImage);
                //    if (pinStillImage != null)
                //    {
                //        VideoControlFlags caps;
                //        videoControl.GetCaps(pinStillImage, out caps);
                //    }
                //}

                videoSampleGrabber.SetBufferSamples(false);
                videoSampleGrabber.SetOneShot(false);
                videoSampleGrabber.SetCallback(videoGrabber, 1);

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
                }

                while (!stopEventCapture.WaitOne(100, false));

                mediaControl.Stop();

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
                if (sourceObjectCapture != null)
                {
                    Marshal.ReleaseComObject(sourceObjectCapture);
                    sourceObjectCapture = null;
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

        private static AMMediaType SetVideoRGB24(ISampleGrabber videoSampleGrabber)
        {
            AMMediaType mediaType = new AMMediaType();
            mediaType.MajorType = MediaType.Video;
            mediaType.SubType = MediaSubType.RGB24;
            videoSampleGrabber.SetMediaType(mediaType);
            return mediaType;
        }

        public event NewFrameEventHandler NewFrameCapture;

        public bool isNewFrameExists()
        {
            return (NewFrameCapture != null);
        }

        public void OnNewFrameCapture(Bitmap image)
        {
            framesReceivedCapture++;

            if ((!stopEventCapture.WaitOne(0, false)) && (NewFrameCapture != null))
            {
                NewFrameCapture(this, new NewFrameEventArgs(image));
            }
        }

    }
}
