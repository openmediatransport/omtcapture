/*
* MIT License
*
* Copyright (c) 2025 Open Media Transport Contributors
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*
*/

using libomtnet;
using V4L2;
using omtcapture.capture;
using System.Xml;

namespace omtcapture
{
    internal class Program
    {

        static bool running = true;
        static void Main(string[] args)
        {
            try
            {
                Console.CancelKeyPress += Console_CancelKeyPress;
                Console.WriteLine("OMT Capture");

                string configFilename = Path.Combine(AppContext.BaseDirectory, "config.xml");
                if (!File.Exists(configFilename))
                {
                    Console.WriteLine("config.xml file not found");
                    return;
                }

                XmlDocument config = new XmlDocument();
                config.Load(configFilename);

                XmlNode? settings = config.SelectSingleNode("settings");
                if (settings == null) { Console.WriteLine("config.xml missing settings element"); return;  }

                XmlNode? n = settings.SelectSingleNode("name");
                if (n == null) { Console.WriteLine("config.xml missing settings/name"); return; }
                string name = n.InnerText;

                n = settings.SelectSingleNode("devicePath");
                if (n == null) { Console.WriteLine("config.xml missing settings/devicePath"); return; }
                string devicePath = n.InnerText;

                OMTMediaFrame frame = new OMTMediaFrame();
                frame.Type = OMTFrameType.Video;
                frame.ColorSpace = OMTColorSpace.BT709;

                n = settings.SelectSingleNode("width");
                if (n == null) { Console.WriteLine("config.xml missing settings/width"); return; }
                frame.Width = int.Parse(n.InnerText);

                n = settings.SelectSingleNode("height");
                if (n == null) { Console.WriteLine("config.xml missing settings/height"); return; }
                frame.Height = int.Parse(n.InnerText);

                n = settings.SelectSingleNode("frameRateN");
                if (n == null) { Console.WriteLine("config.xml missing settings/frameRateN"); return; }
                frame.FrameRateN = int.Parse(n.InnerText);

                n = settings.SelectSingleNode("frameRateD");
                if (n == null) { Console.WriteLine("config.xml missing settings/frameRateD"); return; }
                frame.FrameRateD = int.Parse(n.InnerText);

                n = settings.SelectSingleNode("codec");
                if (n == null) { Console.WriteLine("config.xml missing settings/codec"); return; }

                switch (n.InnerText)
                {
                    case "UYVY":
                        frame.Codec = (int)OMTCodec.UYVY;
                        frame.Stride = frame.Width * 2;
                        break;
                    case "YUY2":
                        frame.Codec = (int)OMTCodec.YUY2;
                        frame.Stride = frame.Width * 2;
                        break;
                    case "NV12":
                        frame.Codec = (int)OMTCodec.NV12;
                        frame.Stride = frame.Width;
                        break;
                    default:
                        Console.WriteLine("Codec not supported"); return;
                }  
                frame.AspectRatio = (float)frame.Width / (float)frame.Height;                

                CaptureFormat fmt = new CaptureFormat(frame.Codec, frame.Width, frame.Height, frame.Stride, frame.FrameRateN, frame.FrameRateD);

                int frameCount = 0;
                long sentLength = 0;
                using (OMTSend send = new OMTSend(name, OMTQuality.Default))
                {
                    using (CaptureDevice capture = new V4L2Capture(devicePath, fmt))
                    {
                        fmt = capture.Format;
                        if (fmt.Codec == (int)V4L2Unmanaged.PIXEL_FORMAT_YUYV)
                        {
                            fmt.Codec = (int)OMTCodec.YUY2;
                        }

                        frame.Codec = fmt.Codec;
                        frame.Width = fmt.Width;
                        frame.Height = fmt.Height;
                        frame.Stride = fmt.Stride;
                        frame.FrameRateD = fmt.FrameRateD;
                        frame.FrameRateN = fmt.FrameRateN;

                        Console.WriteLine("Format: " + fmt.ToString());

                        capture.StartCapture();
                        CaptureFrame captureFrame = new CaptureFrame();
                        while (running)
                        {
                            if (capture.GetNextFrame(ref captureFrame))
                            {
                                frame.Data = captureFrame.Data;
                                frame.DataLength = captureFrame.Length;
                                frame.Timestamp = captureFrame.Timestamp;

                                int networkSend = send.Send(frame);
                                frameCount += 1;
                                sentLength += networkSend;
                                if (frameCount >= 60)
                                {
                                    Console.WriteLine("Sent " + frameCount +" frames, " + sentLength + " bytes.");
                                    frameCount = 0;
                                    sentLength = 0;
                                }
                            }
                        }
                        capture.StopCapture();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }            

        }

        private static void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            running = false;
        }
    }
}
