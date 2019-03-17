
#if UNITY_EDITOR
#if UNITY_EDITOR_WIN
#define WINDOWS
#endif
#if UNITY_EDITOR_OSX
#define OSX
#endif

#else
#if UNITY_STANDALONE_WIN 
#define WINDOWS
#endif
#if UNITY_STANDALONE_OSX 
#define OSX
#endif
#endif


using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

//https://gist.github.com/JohnCoates/47218d0f82f97219685c

namespace Lunar.Utils
{
    public static class Clipboard
    {
        public enum Format
        {
            Text,
            Image
        }

        public static bool isClipboardSupported(Format format)
        {
#if WINDOWS
            return true;
#else
            return false;
#endif
        }

        public static string ReadText()
        {
            if (!isClipboardSupported(Format.Text))
            {
                return null;
            }

            string result = null;
#if WINDOWS
            if (!OpenClipboard(IntPtr.Zero))
            {
                return null;
            }

            var data = GetClipboardData(CF_UNICODETEXT);            
            if (data != IntPtr.Zero)
            {
                int size = (int) GlobalSize(data);

                byte[] bytes = new byte[size];
                Marshal.Copy(data, bytes, 0, size);

                result = Encoding.Unicode.GetString(bytes);
            }

            CloseClipboard();
#endif            
            return result;
        }

        public static Texture2D ReadTexture(bool useMipmaps)
        {

            int width, height;
            var pixels = ReadPixels(out width, out height);
            if (pixels != null)
            {
                var tex = new Texture2D(width, height, TextureFormat.ARGB32, useMipmaps);
                tex.SetPixels32(pixels);
                tex.Apply();
                return tex ;
            }
            else
            {
                return null;
            }
        }

        public static Color32[] ReadPixels(out int width, out int height)
        {
            width = 0;
            height = 0;

            if (!isClipboardSupported(Format.Image))
            {
                return null;
            }

            Color32[] pixels = null;
#if WINDOWS

            if (!OpenClipboard(IntPtr.Zero))
            {
                return null;
            }

            try
            {
                var data = GetClipboardData(CF_DIB);

                if (data != IntPtr.Zero)
                {
                    int size = (int)GlobalSize(data);

                    byte[] bytes = new byte[size];
                    Marshal.Copy(data, bytes, 0, size);

                    using (var stream = new MemoryStream(bytes))
                    {
                        using (var reader = new BinaryReader(stream))
                        {
                            var infoBytes = reader.ReadBytes(40);

                            // Pin the managed memory while, copy it out the data, then unpin it
                            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                            BITMAPINFOHEADER info = (BITMAPINFOHEADER)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(BITMAPINFOHEADER));
                            handle.Free();

                            if (info.biCompression == BitmapCompressionMode.BI_RGB && (info.biBitCount == 24 || info.biBitCount == 32 || info.biBitCount == 8))
                            {
                                width = info.biWidth;
                                height = info.biHeight;
                                pixels = new Color32[width * height];

                                int count = info.biWidth * info.biHeight;

                                int index = 0;

                                int pad;

                                switch (info.biBitCount)
                                {
                                    case 8:
                                        {
                                            pad = 4 - (width % 4);
                                            if (pad == 4)
                                            {
                                                pad = 0;
                                            }
                                            break;
                                        }

                                    case 24:
                                        {
                                            pad = width % 4;
                                            break;
                                        }

                                    default:
                                        {
                                            pad = 0;
                                            break;
                                        }
                                }
                                

                                Color32[] palette = null;

                                if (info.biBitCount == 8)
                                {
                                    palette = new Color32[info.biClrUsed];
                                    for (int i = 0; i < info.biClrUsed; i++)
                                    {
                                        byte b = reader.ReadByte();
                                        byte g = reader.ReadByte();
                                        byte r = reader.ReadByte();
                                        byte a = reader.ReadByte();
                                        palette[i] = new Color32(r, g, b, a);
                                    }
                                }


                                int scanline = 0;

                                while (index < count)
                                {
                                    byte r, g, b, a;

                                    b = reader.ReadByte();
                                    if (info.biBitCount == 8)
                                    {
                                        var palEntry = palette[b];
                                        r = palEntry.r;
                                        g = palEntry.g;
                                        b = palEntry.b;
                                        a = palEntry.a;
                                    }
                                    else
                                    {
                                        g = reader.ReadByte();
                                        r = reader.ReadByte();
                                        a = (byte)(info.biBitCount == 32 ? reader.ReadByte() : 255);
                                    }

                                    pixels[index] = new Color32(r, g, b, a);
                                    index++;

                                    scanline++;

                                    if (scanline >= info.biWidth)
                                    {
                                        scanline -= pad;
                                        while (scanline < info.biWidth)
                                        {
                                            reader.ReadByte();
                                            scanline++;
                                        }
                                        scanline = 0;
                                    }
                                }
                            }

                        }
                    }
                }

            }
            finally
            {
                CloseClipboard();
            }

#endif
            return pixels;
        }

        public static bool WriteText(string text)
        {
            if (!isClipboardSupported(Format.Text) || text == null)
            {
                return false;
            }

#if WINDOWS
            var isAscii = text == Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(text));
            if (isAscii)
            {
                return PushStringToClipboard(text, CF_TEXT);
            }
            else
            {
                return PushStringToClipboard(text, CF_UNICODETEXT);
            }
#else
            return false;
#endif
        }

        public static bool WriteImage(Texture2D image)
        {
            if (!isClipboardSupported(Format.Image) || image == null)
            {
                return false;
            }

#if WINDOWS
            var pixelData = image.GetPixels32();
            byte[] bytes;

            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    var info = new BITMAPINFOHEADER();
                    info.Init();
                    info.biWidth = image.width;
                    info.biHeight = image.height;
                    info.biPlanes = 1;
                    info.biBitCount = 32;
                    info.biCompression = BitmapCompressionMode.BI_RGB;
                    info.biSizeImage = (uint)pixelData.Length;
                    info.biXPelsPerMeter = 1;
                    info.biYPelsPerMeter = 1;
                    info.biClrUsed = 0;
                    info.biClrImportant = 0;

                    byte[] buffer = new byte[info.biSize];
                    GCHandle gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    Marshal.StructureToPtr(info, gcHandle.AddrOfPinnedObject(), true);
                    writer.Write(buffer);
                    gcHandle.Free();

                    foreach (var pixel in pixelData)
                    {
                        uint n = (uint)(pixel.b + (pixel.g << 8) + (pixel.r << 16) + (pixel.a << 24));
                        writer.Write(n);
                    }

                }

                bytes = stream.ToArray();
            }


            return PushDataToClipboard(bytes, CF_DIB);
#else
            return false;
#endif
        }

#if WINDOWS
#region WINDOWS_API

        private enum BitmapCompressionMode : uint
        {
            BI_RGB = 0,
            BI_RLE8 = 1,
            BI_RLE4 = 2,
            BI_BITFIELDS = 3,
            BI_JPEG = 4,
            BI_PNG = 5
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public BitmapCompressionMode biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;

            public void Init()
            {
                biSize = (uint)Marshal.SizeOf(this);
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll")]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr data);

        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardData(uint uFormat);
        
        [DllImport("kernel32.dll")]
        private static extern uint GlobalSize(IntPtr data);

        const uint CF_TEXT = 1;
        const uint CF_UNICODETEXT = 13;
        const uint CF_DIB = 8;

        private static bool PushStringToClipboard(string text, uint format)
        {
            if (text == null)
            {
                return false;
            }

            byte[] bytes;
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    byte[] textBytes;

                    int charSize;

                    switch (format)
                    {
                        case CF_TEXT:
                            textBytes = Encoding.ASCII.GetBytes(text);
                            charSize = 1;
                            break;

                        case CF_UNICODETEXT:
                            textBytes = Encoding.UTF8.GetBytes(text);
                            textBytes = Encoding.Convert(Encoding.UTF8, Encoding.Unicode, textBytes);
                            charSize = 2;
                            break;

                        default:
                            throw new Exception("Not Reachable");
                    }

                    writer.Write(textBytes);

                    while (charSize > 0)
                    {
                        writer.Write((byte)0);
                        charSize--;
                    }

                }
                bytes = stream.ToArray();
                return PushDataToClipboard(bytes, format);
            }
        }

        private static bool PushDataToClipboard(byte[] bytes, uint format)
        {
            try
            {
                try
                {
                    if (!OpenClipboard(IntPtr.Zero))
                    {
                        return false;
                    }

                    if (!EmptyClipboard())
                    {
                        return false;
                    }

                    bool result;
                    try
                    {

                        IntPtr src = Marshal.AllocHGlobal(bytes.Length);
                        Marshal.Copy(bytes, 0, src, bytes.Length);

                        result = SetClipboardData(format, src).ToInt64() != 0;
                        if (!result)
                        {
                            // IMPORTANT: SetClipboardData takes ownership of hGlobal upon success.
                            Marshal.FreeHGlobal(src);
                        }
                    }
                    finally
                    {
                        CloseClipboard();
                    }

                    return result;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion
#endif

#if OSX
        #region OSX_API
		[DllImport("libobjc.dylib")]
		public static extern IntPtr objc_getClass(string className); 
		[DllImport("libobjc.dylib")]
		public static extern IntPtr objc_msgSend(IntPtr basePtr, IntPtr selector, string argument);  
		[DllImport("libobjc.dylib")]
		public static extern IntPtr objc_msgSend(IntPtr basePtr, IntPtr selector);        
		[DllImport("libobjc.dylib")]
		public static extern void objc_msgSend_stret(ref Rect arect, IntPtr basePtr, IntPtr selector);        
		[DllImport ("libobjc.dylib", EntryPoint = "objc_msgSend")]
		public static extern bool bool_objc_msgSend (IntPtr handle, IntPtr selector);
		[DllImport("libobjc.dylib")]
		public static extern IntPtr sel_registerName(string selectorName);         


        private static IntPtr OpenClipboard()
        {
            var cls = objc_getClass("NSObject");
            var sel = sel_registerName("generalPasteboard");
            var clipboard = objc_msgSend(cls, sel);

            sel = sel_registerName("clearContents");
            objc_msgSend(cls, sel);

            return clipboard;
        }

        #endregion
#endif
    }

}
