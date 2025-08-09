using Foster.Framework;
using GameTest1.Game.Levels;
using GameTest1.Game.Sprites;
using GameTest1.Utilities;
using System.Numerics;

namespace GameTest1.Game.Actors
{
    /* Based on https://github.com/FosterFramework/Samples/blob/5b97ca5329e61768f85a45d655da5df7f882519d/TinyLink/Source/Actors/Actor.cs */

    public abstract class ActorBase(Manager manager, Map map, Tileset tileset)
    {
        protected Manager manager = manager;
        protected Map map = map;
        protected Tileset tileset = tileset;

        public Point2 Position = Point2.Zero;
        public Vector2 Velocity = Vector2.Zero;
        public Hitbox Hitbox = new();
        public Sprite? Sprite
        {
            get => sprite;
            set
            {
                if (sprite != value)
                {
                    sprite = value;
                    animation = null;
                    animTimer = 0f;
                }
            }
        }
        public Frame? Frame => currentFrame;
        public int MapLayer = 0;
        public float Rotation = 0f;
        public Shadow Shadow = new(manager);
        public float Elevation = 0f;
        public float Timer = 0f;
        public bool IsVisible = true;

        protected Vector2 veloRemainder;
        protected Sprite? sprite;
        protected Animation? animation;
        protected Frame? currentFrame;
        protected float animTimer = 0f;
        protected bool isLoopingAnim = false;

        public virtual void OnCollisionX() => StopX();
        public virtual void OnCollisionY() => StopY();

        public virtual void Update()
        {
            if (Velocity != Vector2.Zero)
                Move(Velocity * manager.Time.Delta);

            Timer += manager.Time.Delta;
            animTimer += manager.Time.Delta;

            if (sprite != null && animation != null)
                currentFrame = sprite.GetFrameAt(animation, animTimer, isLoopingAnim);
        }

        public Point2[] GetMapCells()
        {
            var localHitbox = (Position + Hitbox.Rectangle) / (Vector2)tileset.CellSize;
            var topLeftFloor = localHitbox.TopLeft.FloorToPoint2();
            var bottomRightCeil = localHitbox.BottomRight.CeilingToPoint2();

            var cellList = new List<Point2>();
            for (var y = topLeftFloor.Y; y < bottomRightCeil.Y; y++)
            {
                for (var x = topLeftFloor.X; x < bottomRightCeil.X; x++)
                {
                    if (x < 0 || x >= map.Size.X || y < 0 || y >= map.Size.Y) continue;
                    cellList.Add(new(x, y));
                }
            }
            return [.. cellList];
        }

        public void Stop() => Velocity = veloRemainder = Vector2.Zero;
        private void StopX() => Velocity.X = veloRemainder.X = 0f;
        private void StopY() => Velocity.Y = veloRemainder.Y = 0f;

        public void Move(Vector2 value)
        {
            veloRemainder += value;
            var move = (Point2)veloRemainder;
            veloRemainder -= value;

            while (move.X != 0)
            {
                var sign = Math.Sign(move.X);
                if (!MovePixel(Point2.UnitX * sign))
                {
                    OnCollisionX();
                    break;
                }
                else
                    move.X -= sign;
            }

            while (move.Y != 0)
            {
                var sign = Math.Sign(move.Y);
                if (!MovePixel(Point2.UnitY * sign))
                {
                    OnCollisionY();
                    break;
                }
                else
                    move.Y -= sign;
            }
        }

        private bool MovePixel(Point2 sign)
        {
            sign.X = Math.Sign(sign.X);
            sign.Y = Math.Sign(sign.Y);

            Point2[] getMatches(Point2[] cells, Point2 sign)
            {
                var matches = new List<Point2>();
                if (sign.X != 0f)
                {
                    var check = sign.X < 0f ? cells.Min(x => x.X) : cells.Max(x => x.X);
                    matches.AddRange([.. cells.Where(x => x.X == check)]);
                }
                else if (sign.Y != 0f)
                {
                    var check = sign.Y < 0f ? cells.Min(x => x.Y) : cells.Max(x => x.Y);
                    matches.AddRange([.. cells.Where(x => x.Y == check)]);
                }
                return [.. matches];
            }

            bool checkMatches(Point2[] matches, MapLayer[] layers, Point2 sign, RectInt destRect)
            {
                foreach (var match in matches)
                {
                    var destMatch = match + sign;
                    if (destMatch.X < 0 || destMatch.X >= map.Size.X || destMatch.Y < 0 || destMatch.Y >= map.Size.Y) return false;
                    foreach (var layer in layers)
                    {
                        var nextCellRect = new RectInt(destMatch * tileset.CellSize, tileset.CellSize);
                        var nextCellType = layer.Tiles[destMatch.Y * map.Size.X + destMatch.X];
                        var nextCellFlags = tileset.CellFlags[nextCellType];
                        if (nextCellRect.Overlaps(destRect) && nextCellFlags != CellFlag.Empty && (!nextCellFlags.Has(CellFlag.Ground) || nextCellFlags.Has(CellFlag.Wall))) return false;
                    }
                }
                return true;
            }

            var layers = map.Layers.Where((_, i) => i >= MapLayer).ToArray();
            var cells = GetMapCells();
            var destRect = Position + Hitbox.Rectangle + sign;

            if (destRect.Left < 0 || destRect.Right >= map.Size.X * tileset.CellSize.X)
                return false;
            if (destRect.Top < 0 || destRect.Bottom >= map.Size.Y * tileset.CellSize.Y)
                return false;

            if (!checkMatches(getMatches(cells, sign.OnlyX()), layers, sign.OnlyX(), destRect))
                return false;

            if (!checkMatches(getMatches(cells, sign.OnlyY()), layers, sign.OnlyY(), destRect))
                return false;

            Position += sign;
            return true;
        }

        public void PlayAnimation(string name, bool loop = true)
        {
            if (sprite != null && sprite.Animations.FirstOrDefault(x => x.Name == name) is Animation anim && animation?.Name != name)
            {
                animation = anim;
                animTimer = 0f;
            }
            isLoopingAnim = loop;
        }

        public virtual void Render()
        {
            if (sprite != null && animation != null)
            {
                if (currentFrame != null && currentFrame.Texture != null && currentFrame.Texture is Subtexture texture)
                {
                    if (Shadow.Enabled)
                        Shadow.Render(sprite, currentFrame, Position);

                    manager.Batcher.PushMatrix(
                        Matrix3x2.CreateTranslation(-currentFrame.Size / 2f) *
                        Matrix3x2.CreateRotation(Calc.DegToRad * Rotation) *
                        Matrix3x2.CreateTranslation(currentFrame.Size / 2f) *
                        Matrix3x2.CreateTranslation(new Vector2(Position.X, Position.Y + Elevation)));
                    manager.Batcher.Image(texture, Vector2.Zero, sprite.Origin, Vector2.One, 0f, Color.White);
                    manager.Batcher.PopMatrix();
                }
            }
        }
    }
}
