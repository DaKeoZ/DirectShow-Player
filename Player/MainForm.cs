// Simple Player sample application
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2006-2011
// contacts@aforgenet.com
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using CleanedProject;
using AForge.Video;
using AForge.Video.DirectShow;

namespace Player
{
    public partial class MainForm : Form
    {
        private Stopwatch stopWatch = null;

        // Class constructor
        public MainForm( )
        {
            InitializeComponent( );
        }

        

        private void MainForm_FormClosing( object sender, FormClosingEventArgs e )
        {
            CloseCurrentVideoSource( );
        }

        // "Exit" menu item clicked
        private void exitToolStripMenuItem_Click( object sender, EventArgs e )
        {
            this.Close( );
        }

        // Open local video capture device
        private void localVideoCaptureDeviceToolStripMenuItem_Click( object sender, EventArgs e )
        {
            CleanedProject.VideoCaptureDeviceForm form = new CleanedProject.VideoCaptureDeviceForm( );

            if ( form.ShowDialog( this ) == DialogResult.OK )
            {
                // create video source
                CleanedProject.VideoCaptureDevice videoSource = form.VideoDevice;
                // open it
                OpenVideoSource( videoSource );
            }
        }

        /*// Capture 1st display in the system
        private void capture1stDisplayToolStripMenuItem_Click( object sender, EventArgs e )
        {
            OpenVideoSource( new CleanedProject.ScreenCaptureStream( Screen.AllScreens[0].Bounds, 100 ) );
        }*/

        // Open video source
        private void OpenVideoSource( CleanedProject.VideoCaptureDevice source )
        {
            // set busy cursor
            this.Cursor = Cursors.WaitCursor;

            // stop current video source
            CloseCurrentVideoSource( );

            // start new video source
            videoSourcePlayer.VideoSource = source;
            videoSourcePlayer.Start( );

            // reset stop watch
            stopWatch = null;

            // start timer
            timer.Start( );

            this.Cursor = Cursors.Default;
        }

        // Close video source if it is running
        private void CloseCurrentVideoSource( )
        {
            if ( videoSourcePlayer.VideoSource != null )
            {
                videoSourcePlayer.SignalToStop( );

                // wait ~ 3 seconds
                for ( int i = 0; i < 30; i++ )
                {
                    if ( !videoSourcePlayer.IsRunning )
                        break;
                    System.Threading.Thread.Sleep( 100 );
                }

                if ( videoSourcePlayer.IsRunning )
                {
                    videoSourcePlayer.Stop( );
                }

                videoSourcePlayer.VideoSource = null;
            }
        }

        // New frame received by the player
        private void videoSourcePlayer_NewFrame( object sender, ref Bitmap image )
        {
            DateTime now = DateTime.Now;
            Graphics g = Graphics.FromImage( image );

            // paint current time
            SolidBrush brush = new SolidBrush( Color.Red );
            g.DrawString( now.ToString( ), this.Font, brush, new PointF( 5, 5 ) );
            brush.Dispose( );

            g.Dispose( );
        }

        // On timer event - gather statistics
        private void timer_Tick( object sender, EventArgs e )
        {
            CleanedProject.VideoCaptureDevice videoSource = videoSourcePlayer.VideoSource;

            if ( videoSource != null )
            {
                // get number of frames since the last timer tick
                int framesReceived = videoSource.FramesReceived;

                if ( stopWatch == null )
                {
                    stopWatch = new Stopwatch( );
                    stopWatch.Start( );
                }
                else
                {
                    stopWatch.Stop( );

                    float fps = 1000.0f * framesReceived / stopWatch.ElapsedMilliseconds;
                    fpsLabel.Text = fps.ToString( "F2" ) + " fps";

                    stopWatch.Reset( );
                    stopWatch.Start( );
                }
            }
        }
    }
}
