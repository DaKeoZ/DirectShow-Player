 using fr.ipmfrance.webcam.tools;
using fr.ipmfrance.win32;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace fr.ipmfrance.webcam
{
    public class DesktopCapture : IDisposable
    {

        public IntPtr m_hMemDC = IntPtr.Zero;
        public int m_nMaxWidth = 0;
        public int m_nMaxHeight = 0;
        public IntPtr m_hBitmap = IntPtr.Zero;
        public IntPtr handleDesktop = IntPtr.Zero;
        public int m_nWidth = CamConst.c_iDefaultWidth;
        public int m_nHeight = CamConst.c_iDefaultHeight;
        public Bitmap imageBitmap;
        public IPMWebcam videoSource;
        public FilterInfo filter;
        public Bitmap currentFrame = null;
        public Bitmap convertedFrame = null;
        public string lastMessage = null;
        public bool firstFrameNotProcessed = true;
        public volatile bool requestedToStop = false;
        private object verrou = new object();
        private int width, height;
        private Graphics bmp;

        public DesktopCapture()
        {
            File.Delete(@"F:\out.txt");
            //imageBitmap = new Bitmap(System.Drawing.Image.FromFile("F:\\test.jpg"));
            filter = WebcamHelper.FindDevice();

            OpenVideoSource();
        }

        public void OpenVideoSource()
        {
            File.AppendAllText(@"F:\out.txt", "OpenVideoSource\n");
            CloseCurrentVideoSource();
            VideoSource = new IPMWebcam(filter);
            VideoSource.Start();
        }

        public void CloseCurrentVideoSource()
        {
            File.AppendAllText(@"F:\out.txt", "CloseCurrentVideoSource\n");

            if (VideoSource != null)
            {
                VideoSource.SignalToStop();

                for (int i = 0; i < 30; i++)
                {
                    if (!VideoSource.IsRunning)
                        break;
                    System.Threading.Thread.Sleep(100);
                }

                if (VideoSource.IsRunning)
                {
                    VideoSource.Stop();
                }

                VideoSource = null;
            }
        }

        public IPMWebcam VideoSource
        {
            get { return videoSource; }
            set
            {

                // detach events
                

                videoSource = value;

                // atach events
                

                lastMessage = null;
                firstFrameNotProcessed = true;
            }
        }

        public delegate void NewFrameHandler(object sender, ref Bitmap image);
        public event NewFrameHandler NewFrame;
        public event PlayingFinishedEventHandler PlayingFinished;

        public void videoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            File.AppendAllText(@"F:\out.txt", "videoSource_NewFrame\n");
            if (!requestedToStop)
            {
                Bitmap newFrame = (Bitmap)eventArgs.Frame.Clone();

                // let user process the frame first
                if (NewFrame != null)
                {
                    NewFrame(this, ref newFrame);
                }
                // dispose previous frame
                if (currentFrame != null)
                {
                    currentFrame.Dispose();
                    currentFrame = null;
                }
                if (convertedFrame != null)
                {
                    convertedFrame.Dispose();
                    convertedFrame = null;
                }

                currentFrame = newFrame;
                lastMessage = null;

                // check if conversion is required to lower bpp rate
                if ((currentFrame.PixelFormat == PixelFormat.Format16bppGrayScale) ||
                        (currentFrame.PixelFormat == PixelFormat.Format48bppRgb) ||
                        (currentFrame.PixelFormat == PixelFormat.Format64bppArgb))
                {
                    convertedFrame = webcam.Image.Convert16bppTo8bpp(currentFrame);
                }
                
                lock (verrou)
                {
                    height = eventArgs.Frame.Height;
                    width = eventArgs.Frame.Width;
                    bmp = Graphics.FromImage(eventArgs.Frame);
                    imageBitmap = currentFrame;

                    imageBitmap.Save("image" + DateTimeOffset.Now.ToUnixTimeMilliseconds(), ImageFormat.Jpeg);
                }
            }
        }

        // Error occured in video source
        public void videoSource_VideoSourceError(object sender, VideoSourceErrorEventArgs eventArgs)
        {
            File.AppendAllText(@"F:\out.txt", "videoSource_VideoSourceError\n");
            lastMessage = eventArgs.Description;
        }

        // Video source has finished playing video
        public void videoSource_PlayingFinished(object sender, ReasonToFinishPlaying reason)
        {
            switch (reason)
            {
                case ReasonToFinishPlaying.EndOfStreamReached:
                    File.AppendAllText(@"F:\out.txt", reason.ToString() + "\n");
                    lastMessage = "Video has finished";
                    break;

                case ReasonToFinishPlaying.StoppedByUser:
                    File.AppendAllText(@"F:\out.txt", reason.ToString() + "\n");
                    lastMessage = "Video was stopped";
                    break;

                case ReasonToFinishPlaying.DeviceLost:
                    File.AppendAllText(@"F:\out.txt", reason.ToString() + "\n");
                    lastMessage = "Video device was unplugged";
                    break;

                case ReasonToFinishPlaying.VideoSourceError:
                    File.AppendAllText(@"F:\out.txt", reason.ToString() + "\n");
                    lastMessage = "Video has finished because of error in video source";
                    break;

                default:
                    File.AppendAllText(@"F:\out.txt", reason.ToString() + "\n");
                    lastMessage = "Video has finished for unknown reason";
                    break;
            }

            // notify users
            if (PlayingFinished != null)
            {
                PlayingFinished(this, reason);
            }
        }

        public void GetHandle()
        {
            // handleDesktop = Gdi32.CreateDC("DISPLAY", null, null, IntPtr.Zero);
            // handleDesktop = Gdi32.CreateDC("HD WebCam", null, null, IntPtr.Zero);
            handleDesktop = IntPtr.Zero;
            
            m_nMaxWidth = Gdi32.GetDeviceCaps(handleDesktop, 8);
            m_nMaxHeight = Gdi32.GetDeviceCaps(handleDesktop, 10);
            m_hMemDC = Gdi32.CreateCompatibleDC(handleDesktop);
            // m_hBitmap = Gdi32.CreateCompatibleBitmap(handleDesktop, m_nWidth, Math.Abs(m_nHeight));
            m_hBitmap = imageBitmap.GetHbitmap();
        }

        public void DeleteBitmap()
        {
            File.AppendAllText(@"F:\out.txt", "DeleteBitmap\n");
            if (m_hBitmap != IntPtr.Zero)
            {
                Gdi32.DeleteObject(m_hBitmap);
                m_hBitmap = IntPtr.Zero;
            }
        }

        public void SetTextColor(Color color)
        {
            File.AppendAllText(@"F:\out.txt", "SetTextColor\n");
            int rgb = (color.B & 0xFF) << 16 | (color.G & 0xFF) << 8 | color.R;
            Gdi32.SetTextColor(m_hMemDC, rgb);
        }

        public void output(ref IMediaSampleImpl mediaSample, ref BitmapInfo m_bmi)
        {
            File.AppendAllText(@"F:\out.txt", "output\n");
            IntPtr ptrSampleMediaBuffer;
            mediaSample.GetPointer(out ptrSampleMediaBuffer);

            /*if (m_hBitmap == IntPtr.Zero)
            {
                m_hBitmap = Gdi32.CreateCompatibleBitmap(handleDesktop, m_nWidth, Math.Abs(m_nHeight));
            }*/

            // IntPtr hOldBitmap = Gdi32.SelectObject(m_hMemDC, m_hBitmap);
            // Gdi32.SelectObject(m_hMemDC, hOldBitmap);

            if (videoSource != null)
            {
                videoSource.NewFrame -= new NewFrameEventHandler(videoSource_NewFrame);
                videoSource.VideoSourceError -= new VideoSourceErrorEventHandler(videoSource_VideoSourceError);
                videoSource.PlayingFinished -= new PlayingFinishedEventHandler(videoSource_PlayingFinished);
            }

            if (currentFrame != null)
            {
                currentFrame.Dispose();
                currentFrame = null;
            }

            if (videoSource != null)
            {
                videoSource.NewFrame += new NewFrameEventHandler(videoSource_NewFrame);
                videoSource.VideoSourceError += new VideoSourceErrorEventHandler(videoSource_VideoSourceError);
                videoSource.PlayingFinished += new PlayingFinishedEventHandler(videoSource_PlayingFinished);
            }

            if (imageBitmap != null)
            {
                lock (verrou)
                {
                    IntPtr srcHdc = bmp.GetHdc();
                    m_hBitmap = imageBitmap.GetHbitmap();
                    if (Gdi32.StretchBlt(m_hMemDC, 0, 0, m_nWidth, Math.Abs(m_nHeight), srcHdc, 0, 0, width, height, TernaryRasterOperations.SRCCOPY))
                    {
                        Gdi32.GetDIBits(m_hMemDC, m_hBitmap, 0, (uint)Math.Abs(m_nHeight), ptrSampleMediaBuffer, ref m_bmi, 0);
                    } else
                    {
                        Marshal.GetLastWin32Error();
                    }
                }
            }
        }

        public void WriteHello()
        {
            string text = "Hello World !!!!!!!!!!";
//            NativeTextRenderer nativeTextRenderer = new NativeTextRenderer(m_hMemDC);
//            Font newFont = new Font("Arial", 28);
//            Point pt = new Point(10, 50);
//            nativeTextRenderer.DrawString(text, newFont, Color.Red, pt);
        }

        public void SetBmiSize(int width, int height)
        {
            File.AppendAllText(@"F:\out.txt", "SetBmiSize\n");
            m_nWidth = width;
            m_nHeight = height;
        }

        public void Dispose()
        {
            File.AppendAllText(@"F:\out.txt", "Dispose\n");
            if (m_hBitmap != IntPtr.Zero)
            {
                Gdi32.DeleteObject(m_hBitmap);
                m_hBitmap = IntPtr.Zero;
            }

            if (handleDesktop != IntPtr.Zero)
            {
                Gdi32.DeleteDC(handleDesktop);
                handleDesktop = IntPtr.Zero;
            }

            if (m_hMemDC != IntPtr.Zero)
            {
                Gdi32.DeleteDC(m_hMemDC);
                m_hMemDC = IntPtr.Zero;
            }


        }
    }
}
