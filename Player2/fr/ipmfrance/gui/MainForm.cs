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
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using fr.ipmfrance.webcam;
using fr.ipmfrance.webcam.com;

namespace fr.ipmfrance.gui
{
    public partial class MainForm : Form
    {
        private Stopwatch stopWatch = null;
        private List<webcam.FilterInfo> devices;
        private webcam.FilterInfo theDevice;

        public MainForm()
        {
            InitializeComponent();

            devices = CollectFilters(FilterCategory.VideoInputDevice);

            devices.ForEach(delegate (webcam.FilterInfo filter)
            {
                if (filter.MonikerString.Contains("pnp")) theDevice = filter;
            });
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
            VideoCaptureDevice videoSource = new VideoCaptureDevice(theDevice.MonikerString);

            OpenVideoSource(videoSource);
        }

        private void OpenVideoSource(VideoCaptureDevice source)
        {
            this.Cursor = Cursors.WaitCursor;

            CloseCurrentVideoSource();

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

        private List<webcam.FilterInfo> CollectFilters(Guid category)
        {
            object comObj = null;
            ICreateDevEnum enumDev = null;
            IEnumMoniker enumMon = null;
            IMoniker[] devMon = new IMoniker[1];
            int hr;
            List<webcam.FilterInfo> result = new List<webcam.FilterInfo>();

            try
            {
                Type srvType = Type.GetTypeFromCLSID(Clsid.SystemDeviceEnum);
                if (srvType == null)
                {
                    Debug.WriteLine("Failed creating device enumerator");
                    throw new ApplicationException("Failed creating device enumerator");
                }

                comObj = Activator.CreateInstance(srvType);
                enumDev = (ICreateDevEnum)comObj;

                hr = enumDev.CreateClassEnumerator(ref category, out enumMon, 0);
                if (hr != 0)
                {

                    Debug.WriteLine("No devices of the category");
                    throw new ApplicationException("No devices of the category");
                }

                IntPtr n = IntPtr.Zero;
                while (true)
                {
                    hr = enumMon.Next(1, devMon, n);
                    if ((hr != 0) || (devMon[0] == null))
                        break;

                    webcam.FilterInfo filter = new webcam.FilterInfo(devMon[0]);
                    result.Add(filter);

                    Marshal.ReleaseComObject(devMon[0]);
                    devMon[0] = null;
                }

                result.Sort();
            }
            catch
            {
            }
            finally
            {
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

        /*private void EnumeratedSupportedFrameSizes( VideoCaptureDevice videoDevice )
        {
            this.Cursor = Cursors.WaitCursor;

            videoResolutionsCombo.Items.Clear( );
            videoInputsCombo.Items.Clear( );

            videoCapabilitiesDictionary.Clear( );

            try
            {
                // collect video capabilities
                VideoCapabilities[] videoCapabilities = videoDevice.VideoCapabilities;
                int videoResolutionIndex = 0;

                foreach ( VideoCapabilities capabilty in videoCapabilities )
                {
                    string item = string.Format(
                        "{0} x {1}", capabilty.FrameSize.Width, capabilty.FrameSize.Height );

                    if ( !videoResolutionsCombo.Items.Contains( item ) )
                    {
                        if ( captureSize == capabilty.FrameSize )
                        {
                            videoResolutionIndex = videoResolutionsCombo.Items.Count;
                        }

                        videoResolutionsCombo.Items.Add( item );
                    }

                    if ( !videoCapabilitiesDictionary.ContainsKey( item ) )
                    {
                        videoCapabilitiesDictionary.Add( item, capabilty );
                    }
                }

                if ( videoCapabilities.Length == 0 )
                {
                    videoResolutionsCombo.Items.Add( "Not supported" );
                }

                videoResolutionsCombo.SelectedIndex = videoResolutionIndex;

                // get video inputs
                availableVideoInputs = videoDevice.AvailableCrossbarVideoInputs;
                int videoInputIndex = 0;

                foreach ( VideoInput input in availableVideoInputs )
                {
                    string item = string.Format( "{0}: {1}", input.Index, input.Type );

                    if ( ( input.Index == videoInput.Index ) && ( input.Type == videoInput.Type ) )
                    {
                        videoInputIndex = videoInputsCombo.Items.Count;
                    }

                    videoInputsCombo.Items.Add( item );
                }

                if ( availableVideoInputs.Length == 0 )
                {
                    videoInputsCombo.Items.Add( "Not supported" );
                }

                videoInputsCombo.SelectedIndex = videoInputIndex;
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }*/
    }
}
