#if UNITY_5
using UnityEngine;
using System.Collections.Generic;
#endif

using UnityEngine;

namespace Lunar.Utils
{
    public enum DitherMode
    {
        None,
        Bayer2x2,
        Bayer4x4,
        Bayer8x8,
        Cluster4x4,
        Cluster8x8
    }

    public class DitherUtils
    {
        public static float[] Bayer2x2 = new float[4] { 0.25f, 0.75f, 1.0f, 0.5f };

        //private static float[] Dither4x4 = new float[16] { 1, 33, 9, 41, 49, 17, 57, 25, 13, 45, 5, 37, 61, 29, 53, 21 };
        public static float[] Bayer4x4 = new float[16] {
            0.03125f, 0.53125f, 0.15625f, 0.65625f,
            0.78125f, 0.28125f, 0.90625f, 0.40625f,
            0.21875f, 0.71875f, 0.09375f, 0.59375f,
            0.96875f, 0.46875f, 0.84375f, 0.34375f};

        //float[] Dither4x4 = new float[16] {12, 5, 6, 13, 4,0,1,7,11, 3, 2, 8, 15, 10, 9, 14};
        public static float[] Cluster4x4 = new float[16] {
        0.8125f, 0.375f, 0.4375f, 0.875f,
        0.3125f, 0.0625f, 0.125f, 0.5f,
        0.75f, 0.25f, 0.1875f, 0.5625f,
        1f, 0.6875f, 0.625f, 0.9375f };



        /*private static float[] Dither8x8 = new float[64] {
            0, 32, 8, 40, 2, 34, 10, 42,
            48, 16, 56, 24, 50, 18, 58, 26,
            12, 44, 4, 36, 14, 46, 6, 38,
            60, 28, 52, 20, 62, 30, 54, 22,
             3, 35, 11, 43, 1, 33, 9, 41,
            51, 19, 59, 27, 49, 17, 57, 25,
            15, 47, 7, 39, 13, 45, 5, 37,
            63, 31, 55, 23, 61, 29, 53, 21 };
            */
        public static float[] Bayer8x8 = new float[64]
        {
            0.015625f, 0.515625f, 0.140625f, 0.640625f, 0.046875f, 0.546875f, 0.171875f,
            0.671875f, 0.765625f, 0.265625f, 0.890625f, 0.390625f, 0.796875f, 0.296875f,
            0.921875f, 0.421875f, 0.203125f, 0.703125f, 0.078125f, 0.578125f, 0.234375f,
            0.734375f, 0.109375f, 0.609375f, 0.953125f, 0.453125f, 0.828125f, 0.328125f,
            0.984375f, 0.484375f, 0.859375f, 0.359375f, 0.0625f, 0.5625f, 0.1875f,
            0.6875f, 0.03125f, 0.53125f, 0.15625f, 0.65625f, 0.8125f, 0.3125f, 0.9375f,
            0.4375f, 0.78125f, 0.28125f, 0.90625f, 0.40625f, 0.25f, 0.75f, 0.125f,
            0.625f, 0.21875f, 0.71875f, 0.09375f, 0.59375f, 1f, 0.5f, 0.875f, 0.375f,
            0.96875f, 0.46875f, 0.84375f, 0.34375f
        };

        public static float[] Cluster8x8 = new float[64]
        {
            0.390625f, 0.171875f, 0.203125f, 0.421875f, 0.5625f, 0.75f, 0.78125f, 0.59375f,
            0.140625f, 0.015625f, 0.046875f, 0.234375f, 0.71875f, 0.9375f, 0.96875f, 0.8125f,
            0.359375f, 0.109375f, 0.078125f, 0.265625f, 0.6875f, 0.90625f, 1f, 0.84375f,
            0.484375f, 0.328125f, 0.296875f, 0.453125f, 0.53125f, 0.65625f, 0.875f, 0.625f,
            0.546875f, 0.734375f, 0.765625f, 0.578125f, 0.40625f, 0.1875f, 0.21875f, 0.4375f,
            0.703125f, 0.921875f, 0.953125f, 0.796875f, 0.15625f, 0.03125f, 0.0625f, 0.25f,
            0.671875f, 0.890625f, 0.984375f, 0.828125f, 0.375f, 0.125f, 0.09375f, 0.28125f,
            0.515625f, 0.640625f, 0.859375f, 0.609375f, 0.5f, 0.34375f, 0.3125f, 0.46875f
        };

        private static float GetLimit(DitherMode mode, int x, int y)
        {
            switch (mode)
            {
                case DitherMode.Bayer2x2: return Bayer2x2[((y & 1) << 1) + (x & 1)];
                case DitherMode.Bayer4x4: return Bayer4x4[((y & 3) << 2) + (x & 3)];
                case DitherMode.Bayer8x8: return Bayer8x8[((y & 7) << 3) + (x & 7)];
                case DitherMode.Cluster4x4: return Bayer4x4[((y & 3) << 2) + (x & 3)];
                case DitherMode.Cluster8x8: return Bayer8x8[((y & 7) << 3) + (x & 7)];
                default: return 1;
            }
        }

        public static int ColorDitherMultiplier(DitherMode mode, int x, int y, float value)
        {
            float limit = GetLimit(mode, x, y);
            return value >= limit ? 1 : 0;
        }

        public static bool ColorDither(DitherMode mode, int x, int y, float value)
        {
            float limit = GetLimit(mode, x, y);
            return value >= limit;
        }

        public static bool ColorDither(bool[] pattern, int x, int y, int size, float value)
        {
            int row = Mathf.FloorToInt(value * size);
            if (row<0)
            {
                row = 0;
            }
            else
            if (row>=size)
            {
                row = size - 1;
            }

            if (x < 0) { x *= -1; }
            if (y < 0) { y *= -1; }        

            return pattern[(x % size) + (y % size) * size + row * (size * size)];
        }

        public static bool ColorDither(float[] pat, int width, int height, int x, int y, float value)
        {
            x = x % width;
            y = y % height;

            if (x < 0) { x += width; }
            if (y < 0) { y += height; }

            float limit = pat[x + y * width];
            return value >= limit;
        }

        public static int GetDitherSize(DitherMode mode)
        {
            switch (mode)
            {
                case DitherMode.Bayer2x2: return 2;
                case DitherMode.Bayer4x4: return 4;
                case DitherMode.Bayer8x8: return 8;
                case DitherMode.Cluster4x4: return 4;
                case DitherMode.Cluster8x8: return 8;
                default: return 1;
            }
        }

#if UNITY_5
        private static Dictionary<DitherMode, Texture2D> _textures = new Dictionary<DitherMode, Texture2D>();

        public static Texture2D GetTexture(DitherMode mode)
        {
            if (_textures.ContainsKey(mode))
            {
                return _textures[mode];
            }

            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            var pixels = new Color32[tex.width * tex.height];
            for (int i=0; i<pixels.Length; i++)
            {
                int x = i % tex.width;
                int y = i / tex.height;

                float ditherVal = GetLimit(mode, x, y);
                byte b = (byte)(255 * ditherVal);
                pixels[i] = new Color32(b, b, b, 255);
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            _textures[mode] = tex;

            return tex;
        }
#endif
    }
}
