using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using NewTek;
using NewTek.NDI;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;


namespace open3mod
{
    class NDIReceiver : IDisposable
    {
        // a pointer to our unmanaged NDI receiver instance
        IntPtr _recvInstancePtr = IntPtr.Zero;

        // a thread to receive frames on so that the UI is still functional
        Thread _receiveThread = null;

        // a way to exit the thread safely
        bool _exitThread = false;

        // should we send video to Windows or not?
        private bool _videoEnabled = true;

        const int m_memSize = 2;
        Bitmap[] m_videoMem = new Bitmap[m_memSize];
        private int m_frameNo = -1;
        private int m_lastFrameNo = -1;

        private Bitmap _received;

        public NDIReceiver()
        {
            ReceiverLock = new Object();
        }

        Source _source;
        public Source ConnectedSource
        {
            get { return _source; }
            set { _source = value; }
        }

      //  private delegate void DelegateUpdateDynImage(System.Drawing.Bitmap bmp, Texture tex);
      //  private DelegateUpdateDynImage _delegateUpdateDynImage;

        public Bitmap Received
        {
            get { return _received; }
        }

        public Object ReceiverLock;      

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // tell the thread to exit
                    _exitThread = true;

                    // wait for it to exit
                    if (_receiveThread != null)
                    {
                        _receiveThread.Join();

                        _receiveThread = null;
                    }
                }

                // Destroy the receiver
                if (_recvInstancePtr != IntPtr.Zero)
                {
                    NDIlib.recv_destroy(_recvInstancePtr);
                    _recvInstancePtr = IntPtr.Zero;
                }

                // Not required, but "correct". (see the SDK documentation)
                NDIlib.destroy();

                _disposed = true;
            }
        }

        private bool _disposed = false;


        // connect to an NDI source in our Dictionary by name
        public void Connect(Source source)
        {
            // just in case we're already connected
           // _delegateUpdateDynImage = UpdateDynImage;
            Disconnect();
            // Sanity
            if (source == null || (String.IsNullOrEmpty(source.Name)))
                return;

            // a source_t to describe the source to connect to.
            NDIlib.source_t source_t = new NDIlib.source_t()
            {
                p_ndi_name = UTF.StringToUtf8(source.Name)
            };

            // make a description of the receiver we want
            NDIlib.recv_create_v3_t recvDescription = new NDIlib.recv_create_v3_t()
            {
                // the source we selected
                source_to_connect_to = source_t,

                // we want BGRA frames for this example
                color_format = NDIlib.recv_color_format_e.recv_color_format_BGRX_BGRA,

                // we want full quality - for small previews or limited bandwidth, choose lowest
                bandwidth = NDIlib.recv_bandwidth_e.recv_bandwidth_highest,

                // let NDIlib deinterlace for us if needed
                allow_video_fields = false
            };

            // create a new instance connected to this source
            _recvInstancePtr = NDIlib.recv_create_v3(ref recvDescription);

            // free the memory we allocated with StringToUtf8
            Marshal.FreeHGlobal(source_t.p_ndi_name);

            // did it work?
            System.Diagnostics.Debug.Assert(_recvInstancePtr != IntPtr.Zero, "Failed to create NDI receive instance.");

            if (_recvInstancePtr != IntPtr.Zero)
            {
                // We are now going to mark this source as being on program output for tally purposes (but not on preview)
                SetTallyIndicators(true, false);

                _source = source;
                // start up a thread to receive on
                _receiveThread = new Thread(ReceiveThreadProc) { IsBackground = true, Name = "NdiReceiveThread" };
                _receiveThread.Start();
            }
        }

        public void Disconnect()
        {
            // in case we're connected, reset the tally indicators
            SetTallyIndicators(false, false);

            // check for a running thread
            if (_receiveThread != null)
            {
                // tell it to exit
                _exitThread = true;

                // wait for it to end
                _receiveThread.Join();
            }

            // reset thread defaults
            _receiveThread = null;
            _exitThread = false;

            // Destroy the receiver
            NDIlib.recv_destroy(_recvInstancePtr);

            // set it to a safe value
            _recvInstancePtr = IntPtr.Zero;

        }

        void SetTallyIndicators(bool onProgram, bool onPreview)
        {
            // we need to have a receive instance
            if (_recvInstancePtr != IntPtr.Zero)
            {
                // set up a state descriptor
                NDIlib.tally_t tallyState = new NDIlib.tally_t()
                {
                    on_program = onProgram,
                    on_preview = onPreview
                };

                // set it on the receiver instance
                NDIlib.recv_set_tally(_recvInstancePtr, ref tallyState);
            }
        }

        // the receive thread runs though this loop until told to exit
        void ReceiveThreadProc()
        {
            while (!_exitThread && _recvInstancePtr != IntPtr.Zero)
            {
                // The descriptors
                NDIlib.video_frame_v2_t videoFrame = new NDIlib.video_frame_v2_t();
                NDIlib.audio_frame_v2_t audioFrame = new NDIlib.audio_frame_v2_t();
                NDIlib.metadata_frame_t metadataFrame = new NDIlib.metadata_frame_t();

                switch (NDIlib.recv_capture_v2(_recvInstancePtr, ref videoFrame, ref audioFrame, ref metadataFrame, 1000))
                {
                    // No data
                    case NDIlib.frame_type_e.frame_type_none:
                        // No data received
                        break;

                    // frame settings - check for extended functionality
                    case NDIlib.frame_type_e.frame_type_status_change:

                        break;

                    // Video data
                    case NDIlib.frame_type_e.frame_type_video:
                        // if not enabled, just discard
                        // this can also occasionally happen when changing sources
                        if (!_videoEnabled || videoFrame.p_data == IntPtr.Zero)
                        {
                            // alreays free received frames
                            NDIlib.recv_free_video_v2(_recvInstancePtr, ref videoFrame);

                            break;
                        }

                        // get all our info so that we can free the frame
                        int yres = (int)videoFrame.yres;
                        int xres = (int)videoFrame.xres;

                        // quick and dirty aspect ratio correction for non-square pixels - SD 4:3, 16:9, etc.
                        double dpiX = 96.0 * (videoFrame.picture_aspect_ratio / ((double)xres / (double)yres));

                        int stride = (int)videoFrame.line_stride_in_bytes;
                        int bufferSize = yres * stride;
                        Bitmap tempVideoFrame;
                        Bitmap finalVideoFrame;
                        lock (ReceiverLock)
                        {
                            tempVideoFrame = new Bitmap(xres, yres, stride, System.Drawing.Imaging.PixelFormat.Format32bppPArgb, videoFrame.p_data);
                            // apply texture resolution bias? (i.e. low quality textures)
                            if (GraphicsSettings.Default.NdiTexQualityBias > 0)
                            {
                                finalVideoFrame = ApplyResolutionBias(tempVideoFrame, GraphicsSettings.Default.NdiTexQualityBias);
                            }
                            else
                            {
                                finalVideoFrame = new Bitmap(tempVideoFrame);//we need a copy, so we can free videoframe
                            }
                            tempVideoFrame.Dispose();

                            if (m_videoMem[m_memSize - 1] != null)
                            {
                                m_videoMem[m_memSize - 1].Dispose();
                                m_videoMem[m_memSize - 1] = null;
                            }
                            for (int i = m_memSize - 1; i > 0; i--)
                            {
                                m_videoMem[i] = m_videoMem[i - 1];
                            }
                            m_videoMem[0] = finalVideoFrame;
                            m_frameNo++;
                        }
                        NDIlib.recv_free_video_v2(_recvInstancePtr, ref videoFrame);
                        break;

                    // audio is not used
                    case NDIlib.frame_type_e.frame_type_audio:

                        // free the frame that was received
                        NDIlib.recv_free_audio_v2(_recvInstancePtr, ref audioFrame);

                        break;
                    // Metadata
                    case NDIlib.frame_type_e.frame_type_metadata:

                        // UTF-8 strings must be converted for use - length includes the terminating zero
                        //String metadata = Utf8ToString(metadataFrame.p_data, metadataFrame.length-1);

                        //System.Diagnostics.Debug.Print(metadata);

                        // free frames that were received
                        NDIlib.recv_free_metadata(_recvInstancePtr, ref metadataFrame);
                        break;
                }
            }
        }


        public Bitmap getNextFrame(out bool skipping)
        {
            Bitmap videoFrame;
            skipping = false;
            lock (ReceiverLock)
            {
                switch (m_frameNo - m_lastFrameNo)
                {
                    default:// we must skip/reuse frame as we have no new one, or simply any other frame;
                        skipping = true;
                        videoFrame = m_videoMem[0];
                        m_lastFrameNo = m_frameNo;
                        break;
                    case 1:
                        videoFrame = m_videoMem[0];
                        m_lastFrameNo = m_frameNo;
                        break;
                    case 2:
                        videoFrame = m_videoMem[1];
                        m_lastFrameNo = m_frameNo - 1;
                        break;
                }
            }
            return videoFrame;
        }

        private static Bitmap ApplyResolutionBias(Bitmap textureBitmap, int bias)
        {
            var width = textureBitmap.Width >> bias;
            var height = textureBitmap.Height >> bias;

            var b = new Bitmap(width, height);
            using (var g = Graphics.FromImage(b))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.DrawImage(textureBitmap, 0, 0, width, height);
            }

            return b;
        }

    }

}

