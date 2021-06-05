using fr.ipmfrance.webcam.tools;
using System.Drawing;
using System.Threading;

namespace fr.ipmfrance.webcam
{

    public class AsyncVideoSource
    {
        private readonly VideoCaptureDevice nestedVideoSource = null;
        private Bitmap lastVideoFrame = null;
        private Thread imageProcessingThread = null;
        private AutoResetEvent isNewFrameAvailable = null;
        private AutoResetEvent isProcessingThreadAvailable = null;
        private bool skipFramesIfBusy = false;

        public event NewFrameEventHandler NewFrame;

        public event VideoSourceErrorEventHandler VideoSourceError
        {
            add { nestedVideoSource.VideoSourceErrorCapture += value; }
            remove { nestedVideoSource.VideoSourceErrorCapture -= value; }
        }

        public event PlayingFinishedEventHandler PlayingFinished
        {
            add { nestedVideoSource.PlayingFinishedCapture += value; }
            remove { nestedVideoSource.PlayingFinishedCapture -= value; }
        }

        public int FramesReceived
        {
            get { return nestedVideoSource.FramesReceivedCapture; }
        }

        public bool IsRunning
        {
            get
            {
                bool isRunning = nestedVideoSource.IsRunningCapture;

                if (!isRunning)
                {
                    Free();
                }

                return isRunning;
            }
        }

        public AsyncVideoSource(FilterInfo filterInfo)
        {
            nestedVideoSource = new VideoCaptureDevice();
            nestedVideoSource.SetDeviceMoniker(filterInfo.MonikerString);
        }

        public void Start()
        {
            if (!IsRunning)
            {

                isNewFrameAvailable = new AutoResetEvent(false);
                isProcessingThreadAvailable = new AutoResetEvent(true);

                imageProcessingThread = new Thread(new ThreadStart(imageProcessingThread_Worker));
                imageProcessingThread.Start();

                nestedVideoSource.NewFrameCapture += new NewFrameEventHandler(nestedVideoSource_NewFrame);
                nestedVideoSource.StartCapture();
            }
        }

        public void SignalToStop()
        {
            nestedVideoSource.SignalToStopCapture();
            Free();
        }

        public void WaitForStop()
        {
            nestedVideoSource.WaitForStopCapture();
            Free();
        }

        public void Stop()
        {
            nestedVideoSource.StopCapture();
            Free();
        }

        private void Free()
        {
            if (imageProcessingThread != null)
            {
                nestedVideoSource.NewFrameCapture -= new NewFrameEventHandler(nestedVideoSource_NewFrame);

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
    }
}
