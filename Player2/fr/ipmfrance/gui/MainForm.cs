// Simple Player sample application
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2006-2011
// contacts@aforgenet.com
//

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using fr.ipmfrance.webcam;
using fr.ipmfrance.webcam.tools;

namespace fr.ipmfrance.gui
{
    public partial class MainForm : Form
    {
        private Stopwatch stopWatch = null;
        private static webcam.FilterInfo theDevice;

        public MainForm()
        {
            InitializeComponent();
            theDevice = WebcamHelper.FindDevice();
      }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            CloseCurrentVideoSource();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void localVideoCaptureDeviceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenVideoSource();
        }

        private void OpenVideoSource()
        {
            this.Cursor = Cursors.WaitCursor;

            CloseCurrentVideoSource();

            VideoCaptureDevice source = new VideoCaptureDevice(theDevice.MonikerString);
            videoSourcePlayer.VideoSource = new AsyncVideoSource(source);
            videoSourcePlayer.Start();

            stopWatch = null;

            timer.Start();

            this.Cursor = Cursors.Default;
        }

        private void CloseCurrentVideoSource()
        {
            if (videoSourcePlayer.VideoSource != null)
            {
                videoSourcePlayer.SignalToStop();

                for (int i = 0; i < 30; i++)
                {
                    if (!videoSourcePlayer.IsRunning)
                        break;
                    System.Threading.Thread.Sleep(100);
                }

                if (videoSourcePlayer.IsRunning)
                {
                    videoSourcePlayer.Stop();
                }

                videoSourcePlayer.VideoSource = null;
            }
        }

        private void videoSourcePlayer_NewFrame(object sender, ref Bitmap image)
        {
            // Création d'un objet graphics à partir de l'image d'un frame de la vidéo
            Graphics g = Graphics.FromImage(image);
            DrawOverlay(g);
            g.Dispose();
        }

        private void DrawOverlay(Graphics g)
        {
            SolidBrush brush = new SolidBrush(Color.Red);
            DateTime now = DateTime.Now;
            g.DrawString(now.ToString(), this.Font, brush, new PointF(505, 105));
            g.DrawString("Hello World", this.Font, brush, new PointF(505, 125));
            brush.Dispose();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            IVideoSource videoSource = videoSourcePlayer.VideoSource;

            if (videoSource != null)
            {
                int framesReceived = videoSource.FramesReceived;

                if (stopWatch == null)
                {
                    stopWatch = new Stopwatch();
                    stopWatch.Start();
                }
                else
                {
                    stopWatch.Stop();

                    float fps = 1000.0f * framesReceived / stopWatch.ElapsedMilliseconds;
                    fpsLabel.Text = fps.ToString("F2") + " fps";

                    stopWatch.Reset();
                    stopWatch.Start();
                }
            }
        }


    }
}
