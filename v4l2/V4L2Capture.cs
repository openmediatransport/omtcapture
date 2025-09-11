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

using omtcapture.capture;

namespace V4L2
{
    internal class V4L2Capture : CaptureDevice
    {
        private int devHandle;
        private v4l2_buffer[] buffers;
        private IntPtr[] bufferData;
        private v4l2_buffer lastBuffer;
        private bool running = false;

        public V4L2Capture(string deviceName, CaptureFormat format) : base(deviceName, format)
        {
            devHandle = V4L2Unmanaged.open(deviceName, FileOpenFlags.O_RDWR);
            if (devHandle == -1)
            {
                throw new Exception("Unable to open device: " + deviceName);
            }

            v4l2_format fmt = new v4l2_format();
            fmt.type = V4L2Unmanaged.FORMAT_TYPE_VIDEO_CAPTURE;
            fmt.pix.width = (uint)format.Width;
            fmt.pix.height = (uint)format.Height;
            fmt.pix.pixelformat = (uint)format.Codec;
            fmt.pix.bytesperline = (uint)format.Stride;
            fmt.pix.field = V4L2Unmanaged.FIELD_FORMAT_NONE;
            fmt.pix.sizeimage = fmt.pix.bytesperline * fmt.pix.height;

            int hr = V4L2Unmanaged.ioctl(devHandle, V4L2Unmanaged.VIDIOC_S_FMT, ref fmt);
            if (hr != 0)
            {
                throw new Exception("Unable to set video device format: " + hr);
            }

            v4l2_streamparm parm = new v4l2_streamparm();
            parm.type = V4L2Unmanaged.FORMAT_TYPE_VIDEO_CAPTURE;
            parm.capture.capability = 0x1000; //V4L2_CAP_TIMEPERFRAME
            parm.capture.timeperframe.numerator = (uint)format.FrameRateD;
            parm.capture.timeperframe.denominator = (uint)format.FrameRateN; //Flip them as it expects time per frame (1/60) vs frame rate (60/1)

            hr = V4L2Unmanaged.ioctl(devHandle, V4L2Unmanaged.VIDIOC_S_PARM, ref parm);
            if (hr != 0)
            {
                throw new Exception("Unable to set video device frame rate: " + hr + "," + V4L2Unmanaged.VIDIOC_S_PARM);
            }

            this.format.Width = (int)fmt.pix.width;
            this.format.Height = (int)fmt.pix.height;
            this.format.Codec = (int)fmt.pix.pixelformat;
            this.format.Stride = (int)fmt.pix.bytesperline;
            this.format.FrameRateD = (int)parm.capture.timeperframe.numerator;
            this.format.FrameRateN = (int)parm.capture.timeperframe.denominator;

            v4l2_requestbuffers rb = new v4l2_requestbuffers();
            rb.type = V4L2Unmanaged.FORMAT_TYPE_VIDEO_CAPTURE;
            rb.count = 5;
            rb.memory = V4L2Unmanaged.V4L2_MEMORY_MMAP;

            hr = V4L2Unmanaged.ioctl(devHandle, V4L2Unmanaged.VIDIOC_REQBUFS, ref rb);

            if (hr != 0)
            {
                throw new Exception("Unable to request video device buffers: " + hr);
            }

            buffers = new v4l2_buffer[rb.count];
            bufferData = new IntPtr[rb.count];

            for (int i = 0; i < rb.count; i++)
            {

                buffers[i].type = V4L2Unmanaged.FORMAT_TYPE_VIDEO_CAPTURE;
                buffers[i].index = (uint)i;
                buffers[i].memory = V4L2Unmanaged.V4L2_MEMORY_MMAP;
                hr = V4L2Unmanaged.ioctl(devHandle, V4L2Unmanaged.VIDIOC_QUERYBUF, ref buffers[i]);

                if (hr != 0)
                {
                    throw new Exception("Unable to query video buffer: " + hr);
                }

                if (buffers[i].length == 0)
                {
                    throw new Exception("Invalid buffer length: " + buffers[i].length);
                }

                bufferData[i] = V4L2Unmanaged.mmap(IntPtr.Zero, (int)buffers[i].length, MemoryMappedProtections.PROT_READ | MemoryMappedProtections.PROT_WRITE,
                    MemoryMappedFlags.MAP_SHARED, devHandle, (int)buffers[i].offset);

                if (bufferData[i] == IntPtr.Zero)
                {
                    throw new Exception("Unable to map memory: " + i);
                }

                hr = V4L2Unmanaged.ioctl(devHandle, V4L2Unmanaged.VIDIOC_QBUF, ref buffers[i]);
                if (hr != 0)
                {
                    throw new Exception("Unable to queue buffer: " + hr);
                }

                Console.WriteLine("Mapped Buffer: " + buffers[i].offset + "," + buffers[i].mother + "," +  buffers[i].length);

            }
        }

        public override void StartCapture()
        {
            if (!running)
            {
                uint type = V4L2Unmanaged.FORMAT_TYPE_VIDEO_CAPTURE;
                int hr = V4L2Unmanaged.ioctl(devHandle, V4L2Unmanaged.VIDIOC_STREAMON, ref type);
                if (hr != 0)
                {
                    throw new Exception("Unable to start stream: " + hr);
                }
                running = true;
            }
        }

        public override void StopCapture()
        {
            if (running)
            {
                running = false;
                uint type = V4L2Unmanaged.FORMAT_TYPE_VIDEO_CAPTURE;
                int hr = V4L2Unmanaged.ioctl(devHandle, V4L2Unmanaged.VIDIOC_STREAMOFF, ref type);
                if (hr != 0)
                {
                    throw new Exception("Unable to stop stream: " + hr);
                }
            }
        }

        public override bool GetNextFrame(ref CaptureFrame frame)
        {
            if (!running) return false;
            int hr = 0;
            if (lastBuffer.length > 0)
            {
                hr = V4L2Unmanaged.ioctl(devHandle, V4L2Unmanaged.VIDIOC_QBUF, ref lastBuffer);
                if (hr != 0)
                {
                    return false;
                }
                lastBuffer.length = 0;
            }

            v4l2_buffer readbuf = new v4l2_buffer();
            readbuf.type = V4L2Unmanaged.FORMAT_TYPE_VIDEO_CAPTURE;
            readbuf.memory = V4L2Unmanaged.V4L2_MEMORY_MMAP;

            hr = V4L2Unmanaged.ioctl(devHandle, V4L2Unmanaged.VIDIOC_DQBUF, ref readbuf);
            if (hr != 0)
            {
                return false;
            }
            lastBuffer = readbuf;

            Int64 timestamp = (Int64)readbuf.timestamp.tv_sec * 10000000;
            timestamp += (Int64)readbuf.timestamp.tv_usec * 10;

            frame.Timestamp = timestamp;
            frame.Length = (int)readbuf.length;
            frame.Data =  bufferData[readbuf.index];
            return true;
        }

        protected override void DisposeInternal()
        {
            StopCapture();
            if (bufferData != null)
            {
                for (int i = 0; i < bufferData.Length; i++)
                {
                    V4L2Unmanaged.munmap(bufferData[i], (int)buffers[i].length);
                }
                bufferData = new IntPtr[0];
            }
            if (devHandle > 0)
            {
                V4L2Unmanaged.close(devHandle);
                devHandle = 0;
            }
            base.DisposeInternal();
        }
    }
}
