internal readonly struct Note
{
    public static readonly Note[] Notes =
    {
        new Note(new Image2D("img/circle.png"), Program.Settings.TheButtons[0], 5, 15, 30, 32),
        new Note(new Image2D("img/left.png"), Program.Settings.TheButtons[2], 1, 14, 32, 33),
        new Note(new Image2D("img/right.png"), Program.Settings.TheButtons[3], 1, 14, 32, 33),
        new Note(new Image2D("img/up.png"), Program.Settings.TheButtons[4], 3, 13, 31, 34),
        new Note(new Image2D("img/down.png"), Program.Settings.TheButtons[5], 3, 13, 31, 34)
    };

    public Image2D Image { get; }
    public string Button { get; }

    public int Left { get; }
    public int Top { get; }
    public int Right { get; }
    public int Bottom { get; }

    public Note(Image2D image, string button, int left, int top, int right, int bottom)
    {
        Image = image;
        Button = button;
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }
}

internal static class NoteExtensions
{
    public static bool ContainsNote(this Image2D self, Note note)
    {
        for (int x = note.Left; x <= note.Right - note.Image.Width; x++)
        {
            for (int y = note.Top; y <= note.Bottom - note.Image.Height; y++)
            {
                if (self.CroppedEquals(note.Image, x, y))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
