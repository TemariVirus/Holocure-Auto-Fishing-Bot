namespace Holocure_Auto_Fishing_Bot
{
    internal struct Note
    {
        #region Properties
        public ReadonlyImage Image { get; }
        public string Button { get; }

        public int Left { get; set; }
        public int Top { get; }
        public int Right { get; }
        public int Bottom { get; }
        #endregion

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
        public static bool ContainsNote(
            this ReadonlyImage self,
            Note note,
            int xOffset,
            int yOffset
        )
        {
            for (int x = note.Left; x <= note.Right - note.Image.Width; x++)
            {
                for (int y = note.Top; y <= note.Bottom - note.Image.Height; y++)
                {
                    if (self.CroppedEquals(note.Image, x - xOffset, y - yOffset))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
