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

public sealed class ReadonlyImage
{
    public int Width { get; }
    public int Height { get; }
    private readonly ARGBColor[] _pixels;

    public ARGBColor this[int x, int y]
    {
        get => _pixels[y * Width + x];
        set => _pixels[y * Width + x] = value;
    }

    public ReadonlyImage(Bitmap source)
    {
        Width = source.Width;
        Height = source.Height;
        _pixels = new ARGBColor[Width * Height];
        CopyFromBitmap(source);
        source.Dispose();
    }

    public ReadonlyImage(string filename)
        : this(new Bitmap(filename)) { }

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

    public bool CroppedEquals(
        ReadonlyImage other,
        int left = 0,
        int top = 0,
        double threshold = 0.108
    )
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
