using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Holocure_Auto_Fishing_Bot
{
    internal readonly struct ARGBColor
    {
        public byte A { get; }
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        public ARGBColor(int alpha, int red, int green, int blue)
        {
            A = (byte)alpha;
            R = (byte)red;
            G = (byte)green;
            B = (byte)blue;
        }

        public float ColorDiff(ARGBColor other)
        {
            int dR = Math.Abs(R - other.R);
            int dG = Math.Abs(G - other.G);
            int dB = Math.Abs(B - other.B);
            // Scale between 0 and 1
            return (float)(dR + dG + dB) / (3 * 255);
        }
    }

    internal sealed class ReadonlyImage
    {
        private readonly ARGBColor[] _pixels;
        private int _opaqueCount = -1;

        #region Properties
        public int Width { get; }
        public int Height { get; }

        public int OpaqueCount
        {
            get
            {
                if (_opaqueCount == -1)
                {
                    InitOpaqueCount();
                }

                return _opaqueCount;
            }
        }

        public ARGBColor this[int x, int y]
        {
            get => _pixels[y * Width + x];
            set => _pixels[y * Width + x] = value;
        }
        #endregion

        public ReadonlyImage(int width, int height)
        {
            Width = width;
            Height = height;
            _pixels = new ARGBColor[Width * Height];
        }

        public ReadonlyImage(Bitmap source)
            : this(source.Width, source.Height)
        {
            BitmapData data = source.LockBits(
                new Rectangle(0, 0, source.Width, source.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb
            );
            unsafe
            {
                byte* ptrCurrentRow = (byte*)data.Scan0;
                for (int y = 0; y < data.Height; y++, ptrCurrentRow += data.Stride)
                {
                    for (int x = 0; x < data.Width; x++)
                    {
                        this[x, y] = new ARGBColor(
                            ptrCurrentRow[x * 4 + 3],
                            ptrCurrentRow[x * 4 + 2],
                            ptrCurrentRow[x * 4 + 1],
                            ptrCurrentRow[x * 4 + 0]
                        );
                    }
                }
            }

            source.UnlockBits(data);
            source.Dispose();
        }

        public ReadonlyImage(string filename)
            : this(new Bitmap(filename)) { }

        private void InitOpaqueCount()
        {
            _opaqueCount = 0;
            foreach (ARGBColor color in _pixels)
            {
                if (color.A == 255)
                {
                    _opaqueCount++;
                }
            }
        }

        public ReadonlyImage Crop(int left, int top, int width, int height)
        {
            ReadonlyImage cropped = new ReadonlyImage(width, height);
            for (int i = Math.Max(-left, 0); i < Math.Min(width, Width - left); i++)
            {
                for (int j = Math.Max(-top, 0); j < Math.Min(height, Height - top); j++)
                {
                    cropped[i, j] = this[left + i, top + j];
                }
            }

            return cropped;
        }

        public ReadonlyImage ShrinkBy(int factor)
        {
            ReadonlyImage shrunk = new ReadonlyImage(Width / factor, Height / factor);
            for (int i = 0; i < shrunk.Width; i++)
            {
                for (int j = 0; j < shrunk.Height; j++)
                {
                    shrunk[i, j] = this[i * factor, j * factor];
                }
            }

            return shrunk;
        }

        public bool CroppedEquals(
            ReadonlyImage other,
            int left = 0,
            int top = 0,
            float threshold = 0.108f
        )
        {
            threshold *= other.OpaqueCount;

            float sum = 0;
            for (int i = 0; i < other.Width; i++)
            {
                for (int j = 0; j < other.Height; j++)
                {
                    if (other[i, j].A == 0)
                    {
                        continue;
                    }

                    sum += this[left + i, top + j].ColorDiff(other[i, j]);
                    if (sum > threshold)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public (int x, int y) Find(ReadonlyImage other)
        {
            for (int x = 0; x <= Width - other.Width; x++)
            {
                for (int y = 0; y <= Height - other.Height; y++)
                {
                    if (CroppedEquals(other, x, y))
                    {
                        return (x, y);
                    }
                }
            }

            return (-1, -1);
        }

        public void Save(string path)
        {
            Bitmap bmp = new Bitmap(Width, Height);
            BitmapData data = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb
            );
            unsafe
            {
                byte* ptrCurrentRow = (byte*)data.Scan0;
                for (int y = 0; y < data.Height; y++, ptrCurrentRow += data.Stride)
                {
                    for (int x = 0; x < data.Width; x++)
                    {
                        ARGBColor color = this[x, y];
                        ptrCurrentRow[x * 4 + 3] = color.A;
                        ptrCurrentRow[x * 4 + 2] = color.R;
                        ptrCurrentRow[x * 4 + 1] = color.G;
                        ptrCurrentRow[x * 4] = color.B;
                    }
                }
            }

            bmp.UnlockBits(data);
            bmp.Save(path);
            bmp.Dispose();
        }
    }
}
