internal readonly struct Note
{
    public ReadonlyImage Image { get; }
    public string Button { get; }

    public int Left { get; }
    public int Top { get; }
    public int Right { get; }
    public int Bottom { get; }

    public Note(ReadonlyImage image, string button, int left, int top, int right, int bottom)
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
    public static bool ContainsNote(this ReadonlyImage self, Note note)
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
