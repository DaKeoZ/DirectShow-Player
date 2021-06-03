// AForge Direct Show Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2009-2013
// contacts@aforgenet.com
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using CleanedProject.Internals;


namespace CleanedProject
{
    /// <summary>
    /// Local video device selection form.
    /// </summary>
    /// 
    /// <remarks><para>The form provides a standard way of selecting local video
    /// device (USB web camera, capture board, etc. - anything supporting DirectShow
    /// interface), which can be reused across applications. It allows selecting video
    /// device, video size and snapshots size (if device supports snapshots and
    /// <see cref="ConfigureSnapshots">user needs them</see>).</para>
    /// 
    /// <para><img src="img/video/VideoCaptureDeviceForm.png" width="478" height="205" /></para>
    /// </remarks>
    /// 
    public partial class VideoCaptureDeviceForm : Form
    {
        // collection of available video devices
        private CleanedProject.FilterInfoCollection videoDevices;
        // selected video device
        private CleanedProject.VideoCaptureDevice videoDevice;

        // supported capabilities of video and snapshots
        private Dictionary<string, CleanedProject.VideoCapabilities> videoCapabilitiesDictionary = new Dictionary<string, CleanedProject.VideoCapabilities>( );

        // available video inputs
        private CleanedProject.VideoInput[] availableVideoInputs = null;

        /// <summary>
        /// Provides configured video device.
        /// </summary>
        /// 
        /// <remarks><para>The property provides configured video device if user confirmed
        /// the dialog using "OK" button. If user canceled the dialog, the property is
        /// set to <see langword="null"/>.</para></remarks>
        /// 
        public CleanedProject.VideoCaptureDevice VideoDevice
        {
            get { return videoDevice; }
        }

        private string videoDeviceMoniker = string.Empty;
        private Size captureSize = new Size( 0, 0 );
        private CleanedProject.VideoInput videoInput = CleanedProject.VideoInput.Default;

        /// <summary>
        /// Moniker string of the selected video device.
        /// </summary>
        /// 
        /// <remarks><para>The property allows to get moniker string of the selected device
        /// on form completion or set video device which should be selected by default on
        /// form loading.</para></remarks>
        /// 
        public string VideoDeviceMoniker
        {
            get { return videoDeviceMoniker; }
            set { videoDeviceMoniker = value; }
        }

        /// <summary>
        /// Video frame size of the selected device.
        /// </summary>
        /// 
        /// <remarks><para>The property allows to get video size of the selected device
        /// on form completion or set the size to be selected by default on form loading.</para>
        /// </remarks>
        /// 
        public Size CaptureSize
        {
            get { return captureSize; }
            set { captureSize = value; }
        }

        /// <summary>
        /// Video input to use with video capture card.
        /// </summary>
        /// 
        /// <remarks><para>The property allows to get video input of the selected device
        /// on form completion or set it to be selected by default on form loading.</para></remarks>
        /// 
        public CleanedProject.VideoInput VideoInput
        {
            get { return videoInput; }
            set { videoInput = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoCaptureDeviceForm"/> class.
        /// </summary>
        /// 
        public VideoCaptureDeviceForm( )
        {
            InitializeComponent( );

            // show device list
			try
			{
                // enumerate video devices
                videoDevices = new CleanedProject.FilterInfoCollection(FilterCategory.VideoInputDevice );

                if ( videoDevices.Count == 0 )
                    throw new ApplicationException( );

                // add all devices to combo
                foreach ( CleanedProject.FilterInfo device in videoDevices )
                {
                    devicesCombo.Items.Add( device.Name );
                }
            }
            catch ( ApplicationException )
            {
                devicesCombo.Items.Add( "No local capture devices" );
                devicesCombo.Enabled = false;
                okButton.Enabled = false;
            }
        }

        // On form loaded
        private void VideoCaptureDeviceForm_Load( object sender, EventArgs e )
        {
            int selectedCameraIndex = 0;

            for ( int i = 0; i < videoDevices.Count; i++ )
            {
                if ( videoDeviceMoniker == videoDevices[i].MonikerString )
                {
                    selectedCameraIndex = i;
                    break;
                }
            }

            devicesCombo.SelectedIndex = selectedCameraIndex;
        }

        // Ok button clicked
        private void okButton_Click( object sender, EventArgs e )
        {
            videoDeviceMoniker = videoDevice.Source;

            // set video size
            if ( videoCapabilitiesDictionary.Count != 0 )
            {
                CleanedProject.VideoCapabilities caps = videoCapabilitiesDictionary[(string) videoResolutionsCombo.SelectedItem];
                
                captureSize = caps.FrameSize;
            }

            if ( availableVideoInputs.Length != 0 )
            {
                videoInput = availableVideoInputs[videoInputsCombo.SelectedIndex];
            }
        }

        // New video device is selected
        private void devicesCombo_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( videoDevices.Count != 0 )
            {
                videoDevice = new CleanedProject.VideoCaptureDevice( "HD WebCam" );
                EnumeratedSupportedFrameSizes(videoDevice);
            }
        }

        private void EnumeratedSupportedFrameSizes(CleanedProject.VideoCaptureDevice videoDevice)
        {
            this.Cursor = Cursors.WaitCursor;

            videoResolutionsCombo.Items.Clear();
            videoInputsCombo.Items.Clear();

            videoCapabilitiesDictionary.Clear();

            try
            {
                // collect video capabilities
                CleanedProject.VideoCapabilities[] videoCapabilities = videoDevice.VideoCapabilities;
                int videoResolutionIndex = 0;

                foreach (CleanedProject.VideoCapabilities capabilty in videoCapabilities)
                {
                    string item = string.Format(
                        "{0} x {1}", capabilty.FrameSize.Width, capabilty.FrameSize.Height);

                    if (!videoResolutionsCombo.Items.Contains(item))
                    {
                        if (captureSize == capabilty.FrameSize)
                        {
                            videoResolutionIndex = videoResolutionsCombo.Items.Count;
                        }

                        videoResolutionsCombo.Items.Add(item);
                    }

                    if (!videoCapabilitiesDictionary.ContainsKey(item))
                    {
                        videoCapabilitiesDictionary.Add(item, capabilty);
                    }
                }

                if (videoCapabilities.Length == 0)
                {
                    videoResolutionsCombo.Items.Add("Not supported");
                }

                videoResolutionsCombo.SelectedIndex = videoResolutionIndex;

                

                // get video inputs
                availableVideoInputs = videoDevice.AvailableCrossbarVideoInputs;
                int videoInputIndex = 0;

                foreach (CleanedProject.VideoInput input in availableVideoInputs)
                {
                    string item = string.Format("{0}: {1}", input.Index, input.Type);

                    if ((input.Index == videoInput.Index) && (input.Type == videoInput.Type))
                    {
                        videoInputIndex = videoInputsCombo.Items.Count;
                    }

                    videoInputsCombo.Items.Add(item);
                }

                if (availableVideoInputs.Length == 0)
                {
                    videoInputsCombo.Items.Add("Not supported");
                }

                videoInputsCombo.SelectedIndex = videoInputIndex;
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }
    }
}
