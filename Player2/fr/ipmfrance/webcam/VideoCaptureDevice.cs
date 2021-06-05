using System;
using System.Drawing;
using System.Threading;
using System.Runtime.InteropServices;
using fr.ipmfrance.webcam.com;
using fr.ipmfrance.webcam.win32;

namespace fr.ipmfrance.webcam
{
    public class VideoCaptureDevice
    {
        public void SetDeviceMoniker(string deviceMoniker)
        {
            deviceMonikerCapture = deviceMoniker;
        }

        private string deviceMonikerCapture;
        private int framesReceivedCapture;
//        private long bytesReceived;
        private Thread threadCapture = null;
        private ManualResetEvent stopEventCapture = null;
        private object sourceObjectCapture = null;
        private object syncCapture = new object();

        public event VideoSourceErrorEventHandler VideoSourceErrorCapture;

        public event PlayingFinishedEventHandler PlayingFinishedCapture;

        public int FramesReceivedCapture
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
    //            bytesReceived = 0;

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
            IPin pinStillImage = null;
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

                if (videoControl != null)
                {
                    captureGraph.FindPin(sourceObjectCapture, PinDirection.Output,
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
                if (VideoSourceErrorCapture != null)
                {
                    VideoSourceErrorCapture(this, new VideoSourceErrorEventArgs(exception.Message));
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

            if (PlayingFinishedCapture != null)
            {
                PlayingFinishedCapture(this, reasonToStop);
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
    //        bytesReceived += image.Width * image.Height * (Bitmap.GetPixelFormatSize(image.PixelFormat) >> 3);

            if ((!stopEventCapture.WaitOne(0, false)) && (NewFrameCapture != null))
            {
                NewFrameCapture(this, new NewFrameEventArgs(image));
            }
        }

    }
}
