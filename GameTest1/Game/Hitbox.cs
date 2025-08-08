using Foster.Framework;

namespace GameTest1.Game
{
    public record Hitbox
    {
        public readonly RectInt Rectangle;

        public Hitbox() => Rectangle = new();
        public Hitbox(RectInt rect) => Rectangle = rect;

        public bool Overlaps(Hitbox other) => Rectangle.Overlaps(in other.Rectangle);

        public void Render(Batcher batcher, Point2 offset, Color color)
        {
            batcher.PushMatrix(offset);
            batcher.RectLine(Rectangle, 1f, color);
            batcher.PopMatrix();
        }
    }
}
