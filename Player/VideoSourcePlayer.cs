// AForge Controls Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2005-2012
// contacts@aforgenet.com
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Data;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using CleanedProject;

namespace CleanedProject
{
    using Point = System.Drawing.Point;

    /// <summary>
    /// Video source player control.
    /// </summary>
    /// 
    /// <remarks><para>The control is aimed to play video sources, which implement
    /// <see cref="AForge.Video.IVideoSource"/> interface. To start playing a video
    /// the <see cref="VideoSource"/> property should be initialized first and then
    /// <see cref="Start"/> method should be called. In the case if user needs to
    /// perform some sort of image processing with video frames before they are displayed,
    /// the <see cref="NewFrame"/> event may be used.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // set new frame event handler if we need processing of new frames
    /// playerControl.NewFrame += new VideoSourcePlayer.NewFrameHandler( this.playerControl_NewFrame );
    /// 
    /// // create video source
    /// IVideoSource videoSource = new ...
    /// // start playing it
    /// playerControl.VideoSource = videoSource;
    /// playerControl.Start( );
    /// ...
    /// 
    /// // new frame event handler
    /// private void playerControl_NewFrame( object sender, ref Bitmap image )
    /// {
    ///     // process new frame somehow ...
    ///     
    ///     // Note: it may be even changed, so the control will display the result
    ///     // of image processing done here
    /// }
    /// </code>
    /// </remarks>
    /// 
    public partial class VideoSourcePlayer : Control
    {
        // video source to play
        private VideoCaptureDevice videoSource = null;
        // last received frame from the video source
        private Bitmap currentFrame = null;
        // converted version of the current frame (in the case if current frame is a 16 bpp 
        // per color plane image, then the converted image is its 8 bpp version for rendering)
        private Bitmap convertedFrame = null;
        // last error message provided by video source
        private string lastMessage = null;
        // controls border color
        private Color borderColor = Color.Black;

        private Size frameSize = new Size( 320, 240 );
        private bool autosize = false;
        private bool keepRatio = false; 
        private bool needSizeUpdate = false;
        private bool firstFrameNotProcessed = true;
        private volatile bool requestedToStop = false;

        // parent of the control
        private Control parent = null;

        // dummy object to lock for synchronization
        private object sync = new object( );

        /// <summary>
        /// Auto size control or not.
        /// </summary>
        /// 
        /// <remarks><para>The property specifies if the control should be autosized or not.
        /// If the property is set to <see langword="true"/>, then the control will change its size according to
        /// video size and control will change its position automatically to be in the center
        /// of parent's control.</para>
        /// 
        /// <para><note>Setting the property to <see langword="true"/> has no effect if
        /// <see cref="Control.Dock"/> property is set to <see cref="DockStyle.Fill"/>.</note></para>
        /// </remarks>
        /// 
        [DefaultValue( false )]
        public bool AutoSizeControl
        {
            get { return autosize; }
            set
            {
                autosize = value;
                UpdatePosition( );
            }
        }
        
        /// <summary>
        /// Gets or sets whether the player should keep the aspect ratio of the images being shown.
        /// </summary>
        /// 
        [DefaultValue( false )]
        public bool KeepAspectRatio
        {
            get { return keepRatio; }
            set
            {
                keepRatio = value;
                Invalidate( );
            }
        }

        /// <summary>
        /// Control's border color.
        /// </summary>
        /// 
        /// <remarks><para>Specifies color of the border drawn around video frame.</para></remarks>
        /// 
        [DefaultValue( typeof( Color ), "Black" )]
        public Color BorderColor
        {
            get { return borderColor; }
            set
            {
                borderColor = value;
                Invalidate( );
            }
        }

        /// <summary>
        /// Video source to play.
        /// </summary>
        /// 
        /// <remarks><para>The property sets the video source to play. After setting the property the
        /// <see cref="Start"/> method should be used to start playing the video source.</para>
        /// 
        /// <para><note>Trying to change video source while currently set video source is still playing
        /// will generate an exception. Use <see cref="IsRunning"/> property to check if current video
        /// source is still playing or <see cref="Stop"/> or <see cref="SignalToStop"/> and <see cref="WaitForStop"/>
        /// methods to stop current video source.</note></para>
        /// </remarks>
        /// 
        /// <exception cref="Exception">Video source can not be changed while current video source is still running.</exception>
        /// 
        [Browsable( false )]
        public VideoCaptureDevice VideoSource
        {
            get { return videoSource; }
            set
            {
                CheckForCrossThreadAccess( );

                // detach events
                if ( videoSource != null )
                {
                    videoSource.NewFrame -= new NewFrameEventHandler( videoSource_NewFrame );
                    videoSource.VideoSourceError -= new VideoSourceErrorEventHandler( videoSource_VideoSourceError );
                    videoSource.PlayingFinished -= new PlayingFinishedEventHandler( videoSource_PlayingFinished );
                }

                lock ( sync )
                {
                    if ( currentFrame != null )
                    {
                        currentFrame.Dispose( );
                        currentFrame = null;
                    }
                }

                videoSource = value;

                // atach events
                if ( videoSource != null )
                {
                    videoSource.NewFrame += new NewFrameEventHandler( videoSource_NewFrame );
                    videoSource.VideoSourceError += new VideoSourceErrorEventHandler( videoSource_VideoSourceError );
                    videoSource.PlayingFinished += new PlayingFinishedEventHandler( videoSource_PlayingFinished );
                }
                else
                {
                    frameSize = new Size( 320, 240 );
                }

                lastMessage = null;
                needSizeUpdate = true;
                firstFrameNotProcessed = true;
                // update the control
                Invalidate( );
            }
        }

        /// <summary>
        /// State of the current video source.
        /// </summary>
        /// 
        /// <remarks><para>Current state of the current video source object - running or not.</para></remarks>
        /// 
        [Browsable( false )]
        public bool IsRunning
        {
            get
            {
                CheckForCrossThreadAccess( );

                return ( videoSource != null ) ? videoSource.IsRunning : false;
            }
        }

        /// <summary>
        /// Delegate to notify about new frame.
        /// </summary>
        /// 
        /// <param name="sender">Event sender.</param>
        /// <param name="image">New frame.</param>
        /// 
        public delegate void NewFrameHandler( object sender, ref Bitmap image );

        /// <summary>
        /// New frame event.
        /// </summary>
        /// 
        /// <remarks><para>The event is fired on each new frame received from video source. The
        /// event is fired right after receiving and before displaying, what gives user a chance to
        /// perform some image processing on the new frame and/or update it.</para>
        /// 
        /// <para><note>Users should not keep references of the passed to the event handler image.
        /// If user needs to keep the image, it should be cloned, since the original image will be disposed
        /// by the control when it is required.</note></para>
        /// </remarks>
        /// 
        public event NewFrameHandler NewFrame;

        /// <summary>
        /// Playing finished event.
        /// </summary>
        /// 
        /// <remarks><para>The event is fired when/if video playing finishes. The reason of video
        /// stopping is provided as an argument to the event handler.</para></remarks>
        /// 
        public event PlayingFinishedEventHandler PlayingFinished;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoSourcePlayer"/> class.
        /// </summary>
        public VideoSourcePlayer( )
        {
            InitializeComponent( );

            // update control style
            SetStyle( ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw |
                ControlStyles.DoubleBuffer | ControlStyles.UserPaint, true );
        }

        // Check if the control is accessed from a none UI thread
        private void CheckForCrossThreadAccess( )
        {
            // force handle creation, so InvokeRequired() will use it instead of searching through parent's chain
            if ( !IsHandleCreated )
            {
                CreateControl( );

                // if the control is not Visible, then CreateControl() will not be enough
                if ( !IsHandleCreated )
                {
                    CreateHandle( );
                }
            }

            if ( InvokeRequired )
            {
                throw new InvalidOperationException( "Cross thread access to the control is not allowed." );
            }
        }

        /// <summary>
        /// Start video source and displaying its frames.
        /// </summary>
        public void Start( )
        {
            CheckForCrossThreadAccess( );

            requestedToStop = false;

            if ( videoSource != null )
            {
                firstFrameNotProcessed = true;

                videoSource.Start( );
                Invalidate( );
            }
        }

        /// <summary>
        /// Stop video source.
        /// </summary>
        /// 
        /// <remarks><para>The method stops video source by calling its <see cref="AForge.Video.IVideoSource.Stop"/>
        /// method, which abourts internal video source's thread. Use <see cref="SignalToStop"/> and
        /// <see cref="WaitForStop"/> for more polite video source stopping, which gives a chance for
        /// video source to perform proper shut down and clean up.
        /// </para></remarks>
        /// 
        public void Stop( )
        {
            CheckForCrossThreadAccess( );

            requestedToStop = true;

            if ( videoSource != null )
            {
                videoSource.Stop( );

                if ( currentFrame != null )
                {
                    currentFrame.Dispose( );
                    currentFrame = null;
                }

                Invalidate( );
            }
        }

        /// <summary>
        /// Signal video source to stop. 
        /// </summary>
        /// 
        /// <remarks><para>Use <see cref="WaitForStop"/> method to wait until video source
        /// stops.</para></remarks>
        /// 
        public void SignalToStop( )
        {
            CheckForCrossThreadAccess( );

            requestedToStop = true;
        }

        /// <summary>
        /// Wait for video source has stopped. 
        /// </summary>
        /// 
        /// <remarks><para>Waits for video source stopping after it was signaled to stop using
        /// <see cref="SignalToStop"/> method. If <see cref="SignalToStop"/> was not called, then
        /// it will be called automatically.</para></remarks>
        /// 
        public void WaitForStop( )
        {
            CheckForCrossThreadAccess( );

            if ( !requestedToStop )
            {
                SignalToStop( );
            }

            if ( videoSource != null )
            {

                if ( currentFrame != null )
                {
                    currentFrame.Dispose( );
                    currentFrame = null;
                }

                Invalidate( );
            }
        }

        /// <summary>
        /// Get clone of current video frame displayed by the control.
        /// </summary>
        /// 
        /// <returns>Returns copy of the video frame, which is currently displayed
        /// by the control - the last video frame received from video source. If the
        /// control did not receive any video frames yet, then the method returns
        /// <see langword="null"/>.</returns>
        /// 
        public Bitmap GetCurrentVideoFrame( )
        {
            lock ( sync )
            {
                return ( currentFrame == null ) ? null : Clone( currentFrame );
            }
        }

        public static Bitmap Clone(Bitmap source, PixelFormat format)
        {
            // copy image if pixel format is the same
            if (source.PixelFormat == format)
                return Clone(source);

            int width = source.Width;
            int height = source.Height;

            // create new image with desired pixel format
            Bitmap bitmap = new Bitmap(width, height, format);

            // draw source image on the new one using Graphics
            Graphics g = Graphics.FromImage(bitmap);
            g.DrawImage(source, 0, 0, width, height);
            g.Dispose();

            return bitmap;
        }

        public static Bitmap Clone(Bitmap source)
        {
            // lock source bitmap data
            BitmapData sourceData = source.LockBits(
                new Rectangle(0, 0, source.Width, source.Height),
                ImageLockMode.ReadOnly, source.PixelFormat);

            // create new image
            Bitmap destination = Clone(sourceData);

            // unlock source image
            source.UnlockBits(sourceData);

            //
            if (
                (source.PixelFormat == PixelFormat.Format1bppIndexed) ||
                (source.PixelFormat == PixelFormat.Format4bppIndexed) ||
                (source.PixelFormat == PixelFormat.Format8bppIndexed) ||
                (source.PixelFormat == PixelFormat.Indexed))
            {
                ColorPalette srcPalette = source.Palette;
                ColorPalette dstPalette = destination.Palette;

                int n = srcPalette.Entries.Length;

                // copy pallete
                for (int i = 0; i < n; i++)
                {
                    dstPalette.Entries[i] = srcPalette.Entries[i];
                }

                destination.Palette = dstPalette;
            }

            return destination;
        }

        public static Bitmap Clone(BitmapData sourceData)
        {
            // get source image size
            int width = sourceData.Width;
            int height = sourceData.Height;

            // create new image
            Bitmap destination = new Bitmap(width, height, sourceData.PixelFormat);

            // lock destination bitmap data
            BitmapData destinationData = destination.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite, destination.PixelFormat);
            

            // unlock destination image
            destination.UnlockBits(destinationData);

            return destination;
        }

        // Paint control
        private void VideoSourcePlayer_Paint( object sender, PaintEventArgs e )
        {
            if ( !Visible )
            {
                return;
            }

            // is it required to update control's size/position
            if ( ( needSizeUpdate ) || ( firstFrameNotProcessed ) )
            {
                UpdatePosition( );
                needSizeUpdate = false;
            }

            lock ( sync )
            {
                Graphics  g = e.Graphics;
                Rectangle rect = this.ClientRectangle;
                Pen       borderPen = new Pen( borderColor, 1 );

                // draw rectangle
                g.DrawRectangle( borderPen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1 );

                if ( videoSource != null )
                {
                    if ( ( currentFrame != null ) && ( lastMessage == null ) )
                    {
                        Bitmap frame = ( convertedFrame != null ) ? convertedFrame : currentFrame;

                        if ( keepRatio )
                        {
                            double ratio = (double) frame.Width / frame.Height;
                            Rectangle newRect = rect;

                            if ( rect.Width < rect.Height * ratio )
                            {
                                newRect.Height = (int) ( rect.Width / ratio );
                            }
                            else
                            {
                                newRect.Width = (int) ( rect.Height * ratio );
                            }

                            newRect.X = ( rect.Width - newRect.Width ) / 2;
                            newRect.Y = ( rect.Height - newRect.Height ) / 2;

                            g.DrawImage( frame, newRect.X + 1, newRect.Y + 1, newRect.Width - 2, newRect.Height - 2);
                        }
                        else
                        {
                            // draw current frame
                            g.DrawImage( frame, rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2);
                        }

                        firstFrameNotProcessed = false;
                    }
                    else
                    {
                        // create font and brush
                        SolidBrush drawBrush = new SolidBrush( this.ForeColor );

                        g.DrawString( ( lastMessage == null ) ? "Connecting ..." : lastMessage,
                            this.Font, drawBrush, new PointF( 5, 5 ) );

                        drawBrush.Dispose( );
                    }
                }

                borderPen.Dispose( );
            }
        }

        // Update controls size and position
        private void UpdatePosition( )
        {
            if ( ( autosize ) && ( this.Dock != DockStyle.Fill ) && ( this.Parent != null ) )
            {
                Rectangle rc = this.Parent.ClientRectangle;
                int width  = frameSize.Width;
                int height = frameSize.Height;

                // update controls size and location
                this.SuspendLayout( );
                this.Location = new Point( ( rc.Width - width - 2 ) / 2, ( rc.Height - height - 2 ) / 2 );
                this.Size = new Size( width + 2, height + 2 );
                this.ResumeLayout( );
            }
        }

        // On new frame ready
        private void videoSource_NewFrame( object sender, NewFrameEventArgs eventArgs )
        {
            if ( !requestedToStop )
            {
                Bitmap newFrame = (Bitmap) eventArgs.Frame.Clone( );

                // let user process the frame first
                if ( NewFrame != null )
                {
                    NewFrame( this, ref newFrame );
                }

                // now update current frame of the control
                lock ( sync )
                {
                    // dispose previous frame
                    if ( currentFrame != null )
                    {
                        if ( currentFrame.Size != eventArgs.Frame.Size )
                        {
                            needSizeUpdate = true;
                        }

                        currentFrame.Dispose( );
                        currentFrame = null;
                    }
                    if ( convertedFrame != null )
                    {
                        convertedFrame.Dispose( );
                        convertedFrame = null;
                    }

                    currentFrame = newFrame;
                    frameSize    = currentFrame.Size;
                    lastMessage  = null;

                    // check if conversion is required to lower bpp rate
                    if ( ( currentFrame.PixelFormat == PixelFormat.Format16bppGrayScale ) ||
                         ( currentFrame.PixelFormat == PixelFormat.Format48bppRgb ) ||
                         ( currentFrame.PixelFormat == PixelFormat.Format64bppArgb ) )
                    {
                        convertedFrame = Convert16bppTo8bpp( currentFrame );
                    }
                }

                // update control
                Invalidate( );
            }
        }

        public static Bitmap CreateGrayscaleImage(int width, int height)
        {
            // create new image
            Bitmap image = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            // set palette to grayscale
            SetGrayscalePalette(image);
            // return new image
            return image;
        }

        public static void SetGrayscalePalette(Bitmap image)
        {
            // check pixel format
            if (image.PixelFormat != PixelFormat.Format8bppIndexed)
                throw new Exception("Source image is not 8 bpp image.");

            // get palette
            ColorPalette cp = image.Palette;
            // init palette
            for (int i = 0; i < 256; i++)
            {
                cp.Entries[i] = Color.FromArgb(i, i, i);
            }
            // set palette back
            image.Palette = cp;
        }

        public static Bitmap Convert16bppTo8bpp(Bitmap bimap)
        {
            Bitmap newImage = null;
            int layers = 0;

            // get image size
            int width = bimap.Width;
            int height = bimap.Height;

            // create new image depending on source image format
            switch (bimap.PixelFormat)
            {
                case PixelFormat.Format16bppGrayScale:
                    // create new grayscale image
                    newImage = CreateGrayscaleImage(width, height);
                    layers = 1;
                    break;

                case PixelFormat.Format48bppRgb:
                    // create new color 24 bpp image
                    newImage = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                    layers = 3;
                    break;

                case PixelFormat.Format64bppArgb:
                    // create new color 32 bpp image
                    newImage = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    layers = 4;
                    break;

                case PixelFormat.Format64bppPArgb:
                    // create new color 32 bpp image
                    newImage = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
                    layers = 4;
                    break;

                default:
                    throw new Exception("Invalid pixel format of the source image.");
            }

            // lock both images
            BitmapData sourceData = bimap.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly, bimap.PixelFormat);
            BitmapData newData = newImage.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite, newImage.PixelFormat);

            unsafe
            {
                // base pointers
                byte* sourceBasePtr = (byte*)sourceData.Scan0.ToPointer();
                byte* newBasePtr = (byte*)newData.Scan0.ToPointer();
                // image strides
                int sourceStride = sourceData.Stride;
                int newStride = newData.Stride;

                for (int y = 0; y < height; y++)
                {
                    ushort* sourcePtr = (ushort*)(sourceBasePtr + y * sourceStride);
                    byte* newPtr = (byte*)(newBasePtr + y * newStride);

                    for (int x = 0, lineSize = width * layers; x < lineSize; x++, sourcePtr++, newPtr++)
                    {
                        *newPtr = (byte)(*sourcePtr >> 8);
                    }
                }
            }

            // unlock both image
            bimap.UnlockBits(sourceData);
            newImage.UnlockBits(newData);

            return newImage;
        }

        // Error occured in video source
        private void videoSource_VideoSourceError( object sender, VideoSourceErrorEventArgs eventArgs )
        {
            lastMessage = eventArgs.Description;
            Invalidate( );
        }

        // Video source has finished playing video
        private void videoSource_PlayingFinished( object sender, ReasonToFinishPlaying reason )
        {
            switch ( reason )
            {
                case ReasonToFinishPlaying.EndOfStreamReached:
                    lastMessage = "Video has finished";
                    break;

                case ReasonToFinishPlaying.StoppedByUser:
                    lastMessage = "Video was stopped";
                    break;

                case ReasonToFinishPlaying.DeviceLost:
                    lastMessage = "Video device was unplugged";
                    break;

                case ReasonToFinishPlaying.VideoSourceError:
                    lastMessage = "Video has finished because of error in video source";
                    break;

                default:
                    lastMessage = "Video has finished for unknown reason";
                    break;
            }
            Invalidate( );

            // notify users
            if ( PlayingFinished != null )
            {
                PlayingFinished( this, reason );
            }
        }

        // Parent Changed event handler
        private void VideoSourcePlayer_ParentChanged( object sender, EventArgs e )
        {
            if ( parent != null )
            {
                parent.SizeChanged -= new EventHandler( parent_SizeChanged );
            }

            parent = this.Parent;

            // set handler for Size Changed parent's event
            if ( parent != null )
            {
                parent.SizeChanged += new EventHandler( parent_SizeChanged );
            }
        }

        // Parent control has changed its size
        private void parent_SizeChanged( object sender, EventArgs e )
        {
            UpdatePosition( );
        }
    }
}
