using Foster.Framework;

namespace GameTest1.Game.UI
{
    public abstract class UserInterfaceWindow(Manager manager)
    {
        protected Manager manager = manager;

        public SpriteFont? Font = null;
        public Point2 Position = new(0, 0);
        public Point2 Size = new(64, 64);
        public Point2 FramePaddingTopLeft = new(0, 0);
        public Point2 FramePaddingBottomRight = new(0, 0);
        public int LinePadding = 8;
        public GraphicsSheet? GraphicsSheet = null;
        public Color BackgroundColor = Color.CornflowerBlue;

        public int Width => Size.X;
        public int Height => Size.Y;
        public int Left => Position.X;
        public int Right => Position.X + Size.X;
        public int Top => Position.Y;
        public int Bottom => Position.Y + Size.Y;
        public Point2 TopLeft => new(Left, Top);
        public Point2 TopRight => new(Right, Top);
        public Point2 BottomLeft => new(Left, Bottom);
        public Point2 BottomRight => new(Right, Bottom);
    }
}
