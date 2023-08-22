using System;
using System.Drawing;
using System.Drawing.Imaging;

public readonly struct ARGBColor
{
    public byte A { get; }
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }

    public ARGBColor(int alpha, int red, int green, int blue)
    {
#if DEBUG
        if (alpha < 0 || alpha > 255)
            throw new ArgumentOutOfRangeException(nameof(alpha));
        if (red < 0 || red > 255)
            throw new ArgumentOutOfRangeException(nameof(red));
        if (green < 0 || green > 255)
            throw new ArgumentOutOfRangeException(nameof(green));
        if (blue < 0 || blue > 255)
            throw new ArgumentOutOfRangeException(nameof(blue));
#endif

        A = (byte)alpha;
        R = (byte)red;
        G = (byte)green;
        B = (byte)blue;
    }

    public double ColorDiff(ARGBColor other)
    {
        int dR = Math.Abs(R - other.R);
        int dG = Math.Abs(G - other.G);
        int dB = Math.Abs(B - other.B);
        // Scale between 0 and 1
        return (double)(dR + dG + dB) / (3 * 255);
    }

    public override string ToString()
    {
        return $"R: {R}, G: {G}, B: {B}, A: {A}";
    }
}

public sealed class Image2D
{
    public int Width { get; }
    public int Height { get; }
    private readonly ARGBColor[] _pixels;

    private int _opaqueCount = -1;
    public int OpaqueCount
    {
        get
        {
            if (_opaqueCount == -1)
            {
                _opaqueCount = 0;
                foreach (ARGBColor pixel in _pixels)
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

    public ARGBColor this[int x, int y]
    {
        get => _pixels[GetIndex(x, y)];
        set => _pixels[GetIndex(x, y)] = value;
    }

    public Image2D(Bitmap source)
    {
        Width = source.Width;
        Height = source.Height;
        _pixels = new ARGBColor[Width * Height];
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
                    this[x, y] = new ARGBColor(
                        ptrCurrentRow[x * 4 + 3],
                        ptrCurrentRow[x * 4 + 2],
                        ptrCurrentRow[x * 4 + 1],
                        ptrCurrentRow[x * 4]
                    );
                }
            }
        }

        src.UnlockBits(data);
    }

    public bool MaskedEquals(Image2D other, int left = 0, int top = 0, double threshold = 0.02)
    {
        for (int i = 0; i < other.Width; i++)
        {
            for (int j = 0; j < other.Height; j++)
            {
                if (other[i, j].A == 0)
                {
                    continue;
                }

                double diff = this[left + i, top + j].ColorDiff(other[i, j]);
                if (diff >= threshold)
                {
                    return false;
                }
            }
        }

        return true;
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
    //                 ptrCurrentRow[x * 4 + 3] = color.R;
    //                 ptrCurrentRow[x * 4 + 2] = color.G;
    //                 ptrCurrentRow[x * 4 + 1] = color.B;
    //                 ptrCurrentRow[x * 4] = color.A;
    //             }
    //         }
    //     }

    //     bmp.UnlockBits(data);
    //     bmp.Save(path);
    //     bmp.Dispose();
    // }
}
