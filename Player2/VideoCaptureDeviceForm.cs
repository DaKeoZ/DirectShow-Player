// AForge Direct Show Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2009-2013
// contacts@aforgenet.com
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
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
        private List<FilterInfo> videoDevices;
        // selected video device
        private VideoCaptureDevice videoDevice;

        // supported capabilities of video and snapshots
        private Dictionary<string, VideoCapabilities> videoCapabilitiesDictionary = new Dictionary<string, VideoCapabilities>( );

        // available video inputs
        private VideoInput[] availableVideoInputs = null;

        /// <summary>
        /// Provides configured video device.
        /// </summary>
        /// 
        /// <remarks><para>The property provides configured video device if user confirmed
        /// the dialog using "OK" button. If user canceled the dialog, the property is
        /// set to <see langword="null"/>.</para></remarks>
        /// 
        public VideoCaptureDevice VideoDevice
        {
            get { return videoDevice; }
        }

        private string videoDeviceMoniker = string.Empty;
        private Size captureSize = new Size( 0, 0 );
        private VideoInput videoInput = VideoInput.Default;

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
        public VideoInput VideoInput
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
                videoDevices = CollectFilters( FilterCategory.VideoInputDevice );

                if ( videoDevices.Count == 0 )
                    throw new ApplicationException( );

                // add all devices to combo
                foreach ( FilterInfo device in videoDevices )
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
                VideoCapabilities caps = videoCapabilitiesDictionary[(string) videoResolutionsCombo.SelectedItem];

                videoDevice.VideoResolution = caps;
                Debug.WriteLine(caps.ToString());
                captureSize = caps.FrameSize;
            }

            if ( availableVideoInputs.Length != 0 )
            {
                videoInput = availableVideoInputs[videoInputsCombo.SelectedIndex];
                videoDevice.CrossbarVideoInput = videoInput;
            }
        }

        // New video device is selected
        private void devicesCombo_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( videoDevices.Count != 0 )
            {
                videoDevice = new VideoCaptureDevice( videoDevices[devicesCombo.SelectedIndex].MonikerString );
                EnumeratedSupportedFrameSizes( videoDevice );
            }
        }

        // Collect supported video and snapshot sizes
        private void EnumeratedSupportedFrameSizes( VideoCaptureDevice videoDevice )
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
