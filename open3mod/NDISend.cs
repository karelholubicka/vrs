using NewTek;
using NewTek.NDI;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;


namespace open3mod
{
    public class NDISender
    {
        static bool doSend = true;
        static bool doCheckDroppedFrames = true;
        public VideoFrame videoFrame;
        public AudioFrame audioFrame;
        static int fps = 25;
        static int audioSamplesPerSec = 48000;
        static int audioNumSamples = audioSamplesPerSec / fps; //48000/25;
        static int scale = 10;
        public static int videoSizeX = 192*scale;
        public static int videoSizeY = 108*scale;
        public static int byteDepth = 4;
        public static int stride = videoSizeX * byteDepth;
        static float videoAR = 16f / 9f;
        public VideoFrame[] nullVideoFrame = new VideoFrame[10];
        public static int NDIchannels = 4; //useful channels
        List<Sender> senderList = new List<Sender>();
        List<String> senderNameList = new List<String>();
        List<long> senderFrameSentList = new List<long>();
        public List<VideoFrame> senderVideoFrameList = new List<VideoFrame>();
        StringFormat textFormat = new StringFormat();
        FontFamily fontFamily = new FontFamily("Arial");

        // fills the audio buffer with a test tone or silence
        static void DrawPrettyText(Graphics graphics, String text, float size, FontFamily family, Point origin, StringFormat format, Brush fill, Pen outline)
        {
            // make a text path
            GraphicsPath path = new GraphicsPath();
            path.AddString(text, family, 0, size, origin, format);
            // Draw the pretty text
            graphics.FillPath(fill, path);
            graphics.DrawPath(outline, path);//zlobi v parallel
        }
        static void FillAudioBuffer(AudioFrame audioFrame, bool doTone)
        {
            // should never happen
            if (audioFrame.AudioBuffer == IntPtr.Zero)
                return;
            // temp space for floats
            float[] floatBuffer = new float[audioFrame.NumSamples];
            // make the tone or silence
            double cycleLength = (double)audioFrame.SampleRate / 1000.0;
            int sampleNumber = 0;
            for (int i = 0; i < audioFrame.NumSamples; i++)
            {
                double time = sampleNumber++ / cycleLength;
                floatBuffer[i] = doTone ? (float)(Math.Sin(2.0f * Math.PI * time) * 0.1) : 0.0f;
            }
            // fill each channel with our floats...
            for (int ch = 0; ch < audioFrame.NumChannels; ch++)
            {
                // scary pointer math ahead...
                // where does this channel start in the unmanaged buffer?
                IntPtr destStart = new IntPtr(audioFrame.AudioBuffer.ToInt64() + (ch * audioFrame.ChannelStride));
                             // copy the float array into the channel
                Marshal.Copy(floatBuffer, 0, destStart, audioFrame.NumSamples);
            }
        }

        public void InitSender(string Name)
        {
            bool first =false; 
         if (senderNameList.Count == 0) first = true;
            int exists = senderNameList.IndexOf(Name);
            if (exists == -1)
            {
                // We are going to create a 1920x1080 16:9 frame at 25.00Hz, progressive (default).
                // We are also going to create an audio frame 
                // 48khz, stereo in the example.
                videoFrame = new VideoFrame(videoSizeX, videoSizeY, videoAR, fps * 1000, 1000);
                videoFrame.TimeCode = 0; // std je NDIlib_send_timecode_synthesize;
                audioFrame = new AudioFrame(audioNumSamples, audioSamplesPerSec, 2);
                audioFrame.NumSamples = audioNumSamples;
                audioFrame.ChannelStride = audioFrame.NumSamples * sizeof(float);
                Sender sendInstanceX = new Sender(Name, false, false); //Video/Audio clocks implicit ..first lepší, ostatní nesync streamy pak mají lepší timing vůči first
                senderList.Add(sendInstanceX);
                senderNameList.Add(Name);
                senderFrameSentList.Add(0);
                senderVideoFrameList.Add(videoFrame);
                videoFrame = new VideoFrame(videoSizeX, videoSizeY, videoAR, fps * 1000, 1000);
                senderVideoFrameList.Add(videoFrame);//so we added two frames, at positions 2* and 2*+1
                textFormat.Alignment = StringAlignment.Center;
                textFormat.LineAlignment = StringAlignment.Center;
            }
        }

        public void DisposeSender(string SenderToDispose)
        {
            int exists = senderNameList.IndexOf(SenderToDispose);
            if (exists != -1)
            {
                Sender sendInstance = senderList[exists];
                sendInstance.Dispose();
                senderNameList.RemoveAt(exists);
                senderList.RemoveAt(exists);
                senderVideoFrameList.RemoveAt(2 * exists + 1); //in this order!
                senderVideoFrameList.RemoveAt(2 * exists);
            }
        }

        public void PrepareFrame(string SenderToUse, long timeCode, int shift = 0, int markHeight = 250)
        {
            {
                int exists = senderNameList.IndexOf(SenderToUse);
                if (exists != -1)
                {
                    Sender sendInstance = senderList[exists];
                    // because we are clocking to the video it is better to always submit the audio first
                    // put tone in it every 25 frames
                    double frameNumber = senderFrameSentList[exists];
                    bool dotone = frameNumber % 25 == 0;
          //          FillAudioBuffer(audioFrame, dotone);
                    // submit the audio buffer
                    doSend = CoreSettings.CoreSettings.Default.SendNDI;
          //          if (doSend) sendInstance.Send(audioFrame);
                    // get the tally state of this source (we poll it),
                    NDIlib.tally_t ndi_tally = sendInstance.Tally;
                    if ((CoreSettings.CoreSettings.Default.MarkFrames) || (CoreSettings.CoreSettings.Default.CheckDroppedFrames))
                    {
                        Bitmap bmp = new Bitmap(senderVideoFrameList[2 * exists + shift].Width, senderVideoFrameList[2 * exists + shift].Height, senderVideoFrameList[2 * exists + shift].Stride, System.Drawing.Imaging.PixelFormat.Format32bppPArgb, senderVideoFrameList[2 * exists + shift].BufferPtr);
                        Graphics graphics = Graphics.FromImage(bmp);
                        graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        Pen outlinePen = new Pen(Color.Black, 2.0f);
                        Pen thinOutlinePen = new Pen(Color.Black, 1.0f);
                        Pen lineOutlinePen = new Pen(Color.White, 10.0f);
                        Pen transparentlineOutlinePen = new Pen(Color.Transparent, 100.0f);
                        switch (exists)
                        {
                            case 0:
                                lineOutlinePen = new Pen(Color.Red, 10.0f);
                                break;
                            case 1:
                                lineOutlinePen = new Pen(Color.Blue, 10.0f);
                                break;
                            case 2:
                                lineOutlinePen = new Pen(Color.Green, 10.0f);
                                break;
                            case 3:
                                lineOutlinePen = new Pen(Color.Yellow, 10.0f);
                                break;
                        };
                        if (CoreSettings.CoreSettings.Default.MarkFrames) DrawPrettyText(graphics, String.Format("Frame {0}", frameNumber.ToString()), 96.0f, fontFamily, new Point(videoFrame.Width / 2, exists * 80 + 50), textFormat, Brushes.White, outlinePen);
                        if (CoreSettings.CoreSettings.Default.MarkFrames) DrawPrettyText(graphics, System.DateTime.Now.ToString(), 96.0f, fontFamily, new Point(videoFrame.Width / 2, 1000), textFormat, Brushes.White, outlinePen);
                        if (CoreSettings.CoreSettings.Default.CheckDroppedFrames)
                        {
                            int lineHeight = videoFrame.Height / senderFrameSentList.Count;
                            int a = (int)(senderFrameSentList[exists] * 20) % videoFrame.Width;
                            Point pt1 = new Point(a, (videoFrame.Height - (lineHeight * (exists) + markHeight)));
                            Point pt2 = new Point(a, videoFrame.Height - lineHeight * (exists));
                            Point pt3 = new Point(a, 0);
                            Point pt4 = new Point(a,  videoFrame.Height);
                            graphics.CompositingMode = CompositingMode.SourceCopy;
                            graphics.DrawLine(transparentlineOutlinePen, pt3, pt4);
                            graphics.DrawLine(lineOutlinePen, pt1, pt2);
                            graphics.CompositingMode = CompositingMode.SourceOver;
                        }
                        graphics.Dispose();
                        bmp.Dispose();
                    }
                    // we now submit the frame. note that this call will be clocked so that we end up submitting 
                    // at exactly 25fps.
                    //   if (doSend) sendInstance.SendAsync(senderVideoFrameList[2 * exists + shift]);
                    senderVideoFrameList[2 * exists + shift].TimeCode = timeCode;
                    senderFrameSentList[exists]++;
                }
            } // using bmp and graphics
        } // using audioFrame and videoFrame

        public void AllSend(int shift = 0)
        {
            doSend = CoreSettings.CoreSettings.Default.SendNDI;
            if (!doSend) return;
            //       FillAudioBuffer(audioFrame, true);
        //    for (int i = senderList.Count - 1; i >= 0; i--)
            for (int i = 0; i < senderList.Count; i++)
                {
                Sender sendInstance = senderList[i];
                if ((sendInstance.GetConnections(0) < 1))
                {
                    //system.threading.thread.sleep(50);
                }
                else
                {
                   sendInstance.SendAsync(senderVideoFrameList[2 * i + shift]);
                }
            }
        }

        public void FlushSenders(int shift = 0)
        {
            Thread.Sleep(100);
       //     FillAudioBuffer(audioFrame, true);
            for (int i = 0; i < senderList.Count; i++)
            {
                Sender sendInstance = senderList[i];
                senderFrameSentList[i] = 0;
        //        if (doSend) sendInstance.Send(audioFrame);
                if (doSend) sendInstance.SendAsync(senderVideoFrameList[2 * i + shift]);
            }
            Thread.Sleep(40); //
            for (int i = 0; i < senderList.Count; i++)
            {
                Sender sendInstance = senderList[i];
   //             if (doSend) sendInstance.Send(audioFrame);
                if (doSend) sendInstance.SendAsync(senderVideoFrameList[2 * i + shift]);
                senderFrameSentList[i]++;
            }
            Thread.Sleep(40);

        }
    }
}
