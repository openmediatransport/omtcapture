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

using System.Runtime.InteropServices;

namespace V4L2
{
    public enum FileOpenFlags
    {
        O_RDONLY = 0x00,
        O_RDWR = 0x02,
        O_NONBLOCK = 0x800,
        O_SYNC = 0x101000
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct v4l2_pix_format
    {
        public uint width;
        public uint height;
        public uint pixelformat;
        public uint field;
        public uint bytesperline;
        public uint sizeimage;
        public uint colorspace;
        public uint priv;
        public uint flags;
        public uint ycbr_enc;
        public uint quantization;
        public uint xfer_func;
    }

    [StructLayout(LayoutKind.Sequential, Size = 208)]
    public struct v4l2_format
    {
        public uint type;
        public uint padding;
        public v4l2_pix_format pix;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct v4l2_requestbuffers
    {
        public uint count;
        public uint type;
        public uint memory;
        public uint reserved1;
        public uint reserved2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct v4l2_timecode
    {
        public uint type;
        public uint flags;
        public byte frames;
        public byte seconds;
        public byte minutes;
        public byte hours;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] userbits;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct linux_timeval
    {
        public UInt64 tv_sec;
        public UInt64 tv_usec;
    }

    [StructLayout(LayoutKind.Sequential, Size = 208)]
    public struct v4l2_streamparm
    {
        public uint type;
        public v4l2_captureparm capture;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct v4l2_fract
    {
        public uint numerator;
        public uint denominator;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct v4l2_captureparm
    {
        public uint capability;
        public uint capturemode;
        public v4l2_fract timeperframe;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct v4l2_buffer
    {
        public uint index;
        public uint type;
        public uint bytesused;
        public uint flags;
        public uint field;
        public linux_timeval timestamp;
        public v4l2_timecode timecode;
        public uint sequence;
        public uint memory;
        public uint offset;
        public uint mother;
        public uint length;
        public uint reserved2;
        public uint request_fd;
    }

    [Flags]
    public enum MemoryMappedProtections
    {
        PROT_NONE = 0x0,
        PROT_READ = 0x1,
        PROT_WRITE = 0x2,
        PROT_EXEC = 0x4
    }
    [Flags]
    public enum MemoryMappedFlags
    {
        MAP_SHARED = 0x01,
        MAP_PRIVATE = 0x02,
        MAP_FIXED = 0x10
    }
    internal class V4L2Unmanaged
    {
        private const string DLL_PATH = "libc";

        const int _IOC_NONE = 0;
        const int _IOC_WRITE = 1;
        const int _IOC_READ = 2;
        const int _IOC_SIZEBITS = 14;
        const int _IOC_NRBITS = 8;
        const int _IOC_NRSHIFT = 0;
        const int _IOC_TYPEBITS = 8;
        const int _IOC_TYPESHIFT = _IOC_NRSHIFT + _IOC_NRBITS;
        const int _IOC_SIZESHIFT = _IOC_TYPESHIFT + _IOC_TYPEBITS;
        const int _IOC_DIRSHIFT = _IOC_SIZESHIFT + _IOC_SIZEBITS;
        internal static uint _IOC(uint dir, uint type, uint nr, uint size)
           => ((dir) << _IOC_DIRSHIFT) | ((type) << _IOC_TYPESHIFT) | ((nr) << _IOC_NRSHIFT) | ((size) << _IOC_SIZESHIFT);

        public const uint FORMAT_TYPE_VIDEO_CAPTURE = 1;
        public const uint PIXEL_FORMAT_YUYV = 0x56595559;
        public const uint PIXEL_FORMAT_YUY2 = 0x32595559;
        public const uint PIXEL_FORMAT_NV12 = 0x3231564E;
        public const uint FIELD_FORMAT_NONE = 1;
        public const uint V4L2_MEMORY_MMAP = 1;

        public static uint VIDIOC_S_FMT = _IOC(_IOC_READ | _IOC_WRITE, 'V', 5, 208);
        public static uint VIDIOC_REQBUFS = _IOC(_IOC_READ | _IOC_WRITE, 'V', 8, 20);
        public static uint VIDIOC_QUERYBUF = _IOC(_IOC_READ | _IOC_WRITE, 'V', 9, 88);
        public static uint VIDIOC_QBUF = _IOC(_IOC_READ | _IOC_WRITE, 'V', 15, 88);
        public static uint VIDIOC_DQBUF = _IOC(_IOC_READ | _IOC_WRITE, 'V', 17, 88);
        public static uint VIDIOC_STREAMON = _IOC(_IOC_WRITE, 'V', 18, 4);
        public static uint VIDIOC_STREAMOFF = _IOC(_IOC_WRITE, 'V', 19, 4);
        public static uint VIDIOC_S_PARM = _IOC(_IOC_READ | _IOC_WRITE, 'V', 22, 204);

        [DllImport(DLL_PATH, SetLastError = true)]
        public static extern int open([MarshalAs(UnmanagedType.LPStr)] string pathname, FileOpenFlags flags);
        [DllImport(DLL_PATH, SetLastError = true)]
        public static extern int close(int fd);
        [DllImport(DLL_PATH, SetLastError = true)]
        public static extern int ioctl(int fd, uint request, ref v4l2_format fmt);
        [DllImport(DLL_PATH, SetLastError = true)]
        public static extern int ioctl(int fd, uint request, ref v4l2_requestbuffers rb);
        [DllImport(DLL_PATH, SetLastError = true)]
        public static extern int ioctl(int fd, uint request, ref v4l2_buffer rb);
        [DllImport(DLL_PATH, SetLastError = true)]
        public static extern int ioctl(int fd, uint request, ref v4l2_streamparm rb);
        [DllImport(DLL_PATH, SetLastError = true)]
        public static extern int ioctl(int fd, uint request, ref uint arg);
        [DllImport(DLL_PATH, SetLastError = true)]
        public static extern IntPtr mmap(IntPtr addr, IntPtr length, MemoryMappedProtections prot, MemoryMappedFlags flags, int fd, IntPtr offset);
        [DllImport(DLL_PATH, SetLastError = true)]
        public static extern IntPtr munmap(IntPtr addr, IntPtr length);
    }

}

