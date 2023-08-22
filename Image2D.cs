using System;
using System.Drawing;
using System.Drawing.Imaging;

public readonly struct RGBAColor
{
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }

    public RGBAColor(int red, int green, int blue, int alpha)
    {
#if DEBUG
        if (red < 0 || red > 255)
            throw new ArgumentOutOfRangeException(nameof(red));
        if (green < 0 || green > 255)
            throw new ArgumentOutOfRangeException(nameof(green));
        if (blue < 0 || blue > 255)
            throw new ArgumentOutOfRangeException(nameof(blue));
        if (alpha < 0 || alpha > 255)
            throw new ArgumentOutOfRangeException(nameof(alpha));
#endif

        R = (byte)red;
        G = (byte)green;
        B = (byte)blue;
        A = (byte)alpha;
    }

    // public static implicit operator RGBAColor(Color color) =>
    //     new RGBAColor(color.R, color.G, color.B, color.A);

    public double ColorDiff(RGBAColor other)
    {
        int dR = Math.Abs(R - (int)other.R);
        int dG = Math.Abs(G - (int)other.G);
        int dB = Math.Abs(B - (int)other.B);
        // Scale between 0 and 1
        return (double)(dR + dG + dB) / 3 / 255;
    }
}

public sealed class Image2D
{
    public int Width { get; }
    public int Height { get; }
    private readonly RGBAColor[] _pixels;

    private int _opaqueCount = -1;
    public int OpaqueCount
    {
        get
        {
            if (_opaqueCount == -1)
            {
                _opaqueCount = 0;
                foreach (RGBAColor pixel in _pixels)
                {
                    if (pixel.A != 0)
                    {
                        _opaqueCount++;
                    }
                }
            }
            return _opaqueCount;
        }
    }

    public RGBAColor this[int x, int y]
    {
        get => _pixels[GetIndex(x, y)];
        set => _pixels[GetIndex(x, y)] = value;
    }

    public Image2D(Bitmap source)
    {
        Width = source.Width;
        Height = source.Height;
        _pixels = new RGBAColor[Width * Height];
        CopyFromBitmap(source);
        source.Dispose();
    }

    public Image2D(string filename)
        : this(new Bitmap(filename)) { }

    private int GetIndex(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
        {
            throw new IndexOutOfRangeException();
        }

        return y * Width + x;
    }

    private void CopyFromBitmap(Bitmap src)
    {
        BitmapData data = src.LockBits(
            new Rectangle(0, 0, src.Width, src.Height),
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
                    this[x, y] = new RGBAColor(
                        ptrCurrentRow[x * 3 + 3],
                        ptrCurrentRow[x * 3 + 2],
                        ptrCurrentRow[x * 3 + 1],
                        ptrCurrentRow[x * 3]
                    );
                }
            }
        }

        src.UnlockBits(data);
    }

    public bool MaskedEquals(Image2D other, int left = 0, int top = 0, double threshold = 0.0069)
    {
        double sum = 0;
        threshold *= other.OpaqueCount;
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

        return sum <= threshold;
    }

    public bool MaskedContains(Image2D other)
    {
        for (int x = 0; x <= Width - other.Width; x++)
        {
            for (int y = 0; y <= Height - other.Height; y++)
            {
                if (MaskedEquals(other, x, y))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // public void Save(string path)
    // {
    //     Bitmap bmp = new Bitmap(Width, Height);
    //     BitmapData data = bmp.LockBits(
    //         new Rectangle(0, 0, bmp.Width, bmp.Height),
    //         ImageLockMode.WriteOnly,
    //         PixelFormat.Format32bppArgb
    //     );
    //     unsafe
    //     {
    //         byte* ptrCurrentRow = (byte*)data.Scan0;
    //         for (int y = 0; y < data.Height; y++, ptrCurrentRow += data.Stride)
    //         {
    //             for (int x = 0; x < data.Width; x++)
    //             {
    //                 RGBAColor color = this[x, y];
    //                 ptrCurrentRow[x * 3 + 3] = color.R;
    //                 ptrCurrentRow[x * 3 + 2] = color.G;
    //                 ptrCurrentRow[x * 3 + 1] = color.B;
    //                 ptrCurrentRow[x * 3] = color.A;
    //             }
    //         }
    //     }

    //     bmp.UnlockBits(data);
    //     bmp.Save(path);
    //     bmp.Dispose();
    // }
}
