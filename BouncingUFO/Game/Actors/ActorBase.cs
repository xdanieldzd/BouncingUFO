using BouncingUFO.Game.Levels;
using BouncingUFO.Game.Sprites;
using Foster.Framework;
using System.Numerics;

namespace BouncingUFO.Game.Actors
{
    /* Based on https://github.com/FosterFramework/Samples/blob/5b97ca5329e61768f85a45d655da5df7f882519d/TinyLink/Source/Actors/Actor.cs */

    [Flags]
    public enum ActorClass
    {
        None = 0,
        Solid = 1 << 0,
        Player = 1 << 1,
        Collectible = 1 << 2
    }

    public abstract class ActorBase(Manager manager, LevelManager level, int mapLayer = 0, int argument = 0)
    {
        protected Manager manager = manager;
        protected LevelManager level = level;
        protected int argument = argument;

        public ActorClass Class = ActorClass.None;
        public Point2 Position = Point2.Zero;
        public Vector2 Offset = Vector2.Zero;
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
                    AnimationTimer = 0f;
                }
            }
        }
        public Frame? Frame => currentFrame;
        public int MapLayer = mapLayer, DrawPriority = 0;
        public float Rotation = 0f;
        public float BobSpeed = 10f, BobDirection = 0f;
        public bool HasShadow = false;
        public Color ShadowColor = new(0f, 0f, 0f, 0.5f);
        public Vector2 ShadowScale = Vector2.One / 2f;
        public Vector2 ShadowOffset = Vector2.Zero;
        public float LogicTimer = 0f, AnimationTimer = 0f;
        public bool IsVisible = true;
        public bool IsRunning = false;

        public Vector2 TransformedPosition => Position + Offset + (sprite?.Origin ?? Vector2.Zero);

        protected Vector2 veloRemainder;
        protected Sprite? sprite;
        protected Animation? animation;
        protected Frame? currentFrame;
        protected bool isLoopingAnim = false;

        public virtual void OnCollisionX(ActorBase? other) => StopX();
        public virtual void OnCollisionY(ActorBase? other) => StopY();

        public virtual void Created() { }

        public virtual void Update()
        {
            if (Velocity != Vector2.Zero)
                Move(Velocity * manager.Time.Delta);

            LogicTimer += manager.Time.Delta;
            AnimationTimer += manager.Time.Delta;

            if (sprite != null && animation != null)
                currentFrame = sprite.GetFrameAt(animation, AnimationTimer, isLoopingAnim);

            CalcBobbing();
            CalcShadow();
        }

        public Point2[] GetMapCells() => GetMapCells(Position, Hitbox.Rectangle, level.Map, level.Tileset);

        public static Point2[] GetMapCells(Point2 position, RectInt hitboxRect, Map? map, Tileset? tileset)
        {
            if (map == null || tileset == null) return [];

            var localHitbox = (position + hitboxRect) / (Vector2)tileset.CellSize;
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
                    OnCollisionX(null);
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
                    OnCollisionY(null);
                    break;
                }
                else
                    move.Y -= sign;
            }
        }

        private bool MovePixel(Point2 sign)
        {
            if (level.Map == null || level.Tileset == null) return false;

            sign.X = Math.Sign(sign.X);
            sign.Y = Math.Sign(sign.Y);

            Point2[] getCellMatches(Point2[] cells, Point2 sign)
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

            bool checkCellMatches(Point2[] matches, MapLayer[] layers, Point2 sign, RectInt destRect)
            {
                foreach (var match in matches.Where(x => level.Map.Rectangle.Contains(x + sign)))
                {
                    var destMatch = match + sign;

                    var nextCellRect = new RectInt(destMatch * level.Tileset.CellSize, level.Tileset.CellSize);
                    var nextCellIdx = destMatch.Y * level.Map.Size.X + destMatch.X;

                    foreach (var layer in layers)
                    {
                        var nextCellFlags = level.Tileset.CellFlags[layer.Tiles[nextCellIdx]];
                        if (nextCellRect.Overlaps(destRect) &&
                            nextCellFlags != CellFlag.Empty &&
                            (!nextCellFlags.Has(CellFlag.Ground) || nextCellFlags.Has(CellFlag.Wall))) return false;
                    }
                }
                return true;
            }

            var destRect = Position + Hitbox.Rectangle + sign;

            if (!(level.Map.Rectangle * level.Tileset.CellSize).Contains(destRect))
                return false;

            var layers = level.Map.Layers.Where((_, i) => i <= MapLayer).Reverse().ToArray();
            var cells = GetMapCells();

            if (!checkCellMatches(getCellMatches(cells, sign), layers, sign, destRect))
                return false;

            Position += sign;
            return true;
        }

        public void PlayAnimation(string name, bool loop = true)
        {
            if (sprite != null && sprite.Animations.FirstOrDefault(x => x.Name == name) is Animation anim && animation?.Name != name)
            {
                animation = anim;
                AnimationTimer = 0f;
            }
            isLoopingAnim = loop;
        }

        public virtual void CalcShadow()
        {
            if (sprite == null || animation == null || currentFrame == null) return;

            ShadowOffset = new(0f, currentFrame.Size.Y * 0.35f);
            ShadowScale = new Vector2(0.75f, 0.425f) * Calc.ClampedMap(Offset.Y, -1f, 1f, 0.9f, 1f);
        }

        public virtual void CalcBobbing()
        {
            if (BobDirection == 0f) return;
            Offset.Y = Calc.Approach(Offset.Y, BobDirection, BobSpeed * manager.Time.Delta);
            if (Offset.Y >= 1f || Offset.Y <= -1f) BobDirection = -BobDirection;
        }

        public virtual void RenderSprite()
        {
            if (sprite != null && currentFrame != null && currentFrame.Texture is Subtexture texture)
                manager.Batcher.Image(texture, Position + Offset + sprite.Origin, sprite.Origin, Vector2.One, Calc.DegToRad * Rotation, Color.White);
        }

        public virtual void RenderShadow()
        {
            if (HasShadow && sprite != null && currentFrame != null && currentFrame.Texture is Subtexture texture)
            {
                manager.Batcher.PushMode(Batcher.Modes.Wash);
                manager.Batcher.Image(texture, Position + ShadowOffset + sprite.Origin, sprite.Origin, ShadowScale, 0f, ShadowColor);
                manager.Batcher.PopMode();
            }
        }

        public virtual void Destroyed() { }
    }

    public class ActorException : Exception
    {
        public ActorException(string message) : base(message) { }
        public ActorException(Type actorType, string message) : base($"{actorType.Name}: {message}") { }
    }
}
