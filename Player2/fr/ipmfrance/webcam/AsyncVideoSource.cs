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
   //     private int framesProcessed;

        public event NewFrameEventHandler NewFrame;

        public event VideoSourceErrorEventHandler VideoSourceError
        {
            add { nestedVideoSource.VideoSourceError += value; }
            remove { nestedVideoSource.VideoSourceError -= value; }
        }

        public event PlayingFinishedEventHandler PlayingFinished
        {
            add { nestedVideoSource.PlayingFinished += value; }
            remove { nestedVideoSource.PlayingFinished -= value; }
        }

        public int FramesReceived
        {
            get { return nestedVideoSource.FramesReceived; }
        }

        public bool IsRunning
        {
            get
            {
                bool isRunning = nestedVideoSource.IsRunning;

                if (!isRunning)
                {
                    Free();
                }

                return isRunning;
            }
        }

        public AsyncVideoSource(FilterInfo filterInfo)
        {
            this.nestedVideoSource = new VideoCaptureDevice(filterInfo.MonikerString); ;
        }

        public void Start()
        {
            if (!IsRunning)
            {

                isNewFrameAvailable = new AutoResetEvent(false);
                isProcessingThreadAvailable = new AutoResetEvent(true);

                imageProcessingThread = new Thread(new ThreadStart(imageProcessingThread_Worker));
                imageProcessingThread.Start();

                nestedVideoSource.NewFrame += new NewFrameEventHandler(nestedVideoSource_NewFrame);
                nestedVideoSource.Start();
            }
        }

        public void SignalToStop()
        {
            nestedVideoSource.SignalToStop();
            Free();
        }

        public void WaitForStop()
        {
            nestedVideoSource.WaitForStop();
            Free();
        }

        public void Stop()
        {
            nestedVideoSource.Stop();
            Free();
        }

        private void Free()
        {
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
