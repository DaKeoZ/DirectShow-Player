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
using System.Runtime.InteropServices.ComTypes;
using CleanedProject.Internals;
using System.Runtime.InteropServices;

namespace CleanedProject
{
    public partial class MainForm : Form
    {
        private Stopwatch stopWatch = null;
        private List<FilterInfo> devices;
        private FilterInfo theDevice;

        // Class constructor
        public MainForm( )
        {
            InitializeComponent( );

            devices = CollectFilters(FilterCategory.VideoInputDevice);

            devices.ForEach(delegate(FilterInfo filter)
            {
                if (filter.MonikerString.Contains("pnp")) theDevice = filter;
            });
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
            // create video source
            VideoCaptureDevice videoSource = new VideoCaptureDevice(theDevice.MonikerString);

            // open it
            OpenVideoSource( videoSource );
        }

        // Open video source
        private void OpenVideoSource( IVideoSource source )
        {
            // set busy cursor
            this.Cursor = Cursors.WaitCursor;

            // stop current video source
            CloseCurrentVideoSource( );

            // start new video source
            videoSourcePlayer.VideoSource = new AsyncVideoSource( source );
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
            IVideoSource videoSource = videoSourcePlayer.VideoSource;

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

        private List<FilterInfo> CollectFilters(Guid category)
        {
            object comObj = null;
            ICreateDevEnum enumDev = null;
            IEnumMoniker enumMon = null;
            IMoniker[] devMon = new IMoniker[1];
            int hr;
            List<FilterInfo> result = new List<FilterInfo>();

            try
            {
                // Get the system device enumerator
                Type srvType = Type.GetTypeFromCLSID(Clsid.SystemDeviceEnum);
                if (srvType == null)
                {
                    Debug.WriteLine("Failed creating device enumerator");
                    throw new ApplicationException("Failed creating device enumerator");
                }

                // create device enumerator
                comObj = Activator.CreateInstance(srvType);
                enumDev = (ICreateDevEnum)comObj;

                // Create an enumerator to find filters of specified category
                hr = enumDev.CreateClassEnumerator(ref category, out enumMon, 0);
                if (hr != 0)
                {

                    Debug.WriteLine("No devices of the category");
                    throw new ApplicationException("No devices of the category");
                }

                // Collect all filters
                IntPtr n = IntPtr.Zero;
                while (true)
                {
                    // Get next filter
                    hr = enumMon.Next(1, devMon, n);
                    if ((hr != 0) || (devMon[0] == null))
                        break;

                    // Add the filter
                    FilterInfo filter = new FilterInfo(devMon[0]);
                    result.Add(filter);

                    // Release COM object
                    Marshal.ReleaseComObject(devMon[0]);
                    devMon[0] = null;
                }

                // Sort the collection
                result.Sort();
            }
            catch
            {
            }
            finally
            {
                // release all COM objects
                enumDev = null;
                if (comObj != null)
                {
                    Marshal.ReleaseComObject(comObj);
                    comObj = null;
                }
                if (enumMon != null)
                {
                    Marshal.ReleaseComObject(enumMon);
                    enumMon = null;
                }
                if (devMon[0] != null)
                {
                    Marshal.ReleaseComObject(devMon[0]);
                    devMon[0] = null;
                }
            }
            return result;
        }
    }
}
