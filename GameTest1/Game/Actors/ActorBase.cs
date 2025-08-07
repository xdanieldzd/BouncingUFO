using Foster.Framework;
using GameTest1.Game.Sprites;
using System.Numerics;

namespace GameTest1.Game.Actors
{
    /* Based on https://github.com/FosterFramework/Samples/blob/5b97ca5329e61768f85a45d655da5df7f882519d/TinyLink/Source/Actors/Actor.cs */

    public abstract class ActorBase(Manager manager)
    {
        protected Manager manager = manager;

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
                    animation = value?.Animations.FirstOrDefault();
                    animTimer = 0f;
                }
            }
        }
        public float Rotation = 0f;
        public float Timer = 0f;
        public bool IsVisible = true;

        private Vector2 veloRemainder;
        private Sprite? sprite;
        private Animation? animation;
        private float animTimer = 0f;
        private bool isLoopingAnim = false;

        public virtual void OnCollisionX() => StopX();
        public virtual void OnCollisionY() => StopY();

        public virtual void Update()
        {
            if (Velocity != Vector2.Zero)
                Move(Velocity * manager.Time.Delta);

            Timer += manager.Time.Delta;
            animTimer += manager.Time.Delta;
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

            // TODO actor/actor collision & actor/mapcell collision!

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
                var frame = sprite.GetFrameAt(animation, animTimer, isLoopingAnim);
                if (frame.Texture != null && frame.Texture is Subtexture texture)
                {
                    manager.Batcher.PushMatrix(
                        Matrix3x2.CreateTranslation(-frame.Size / 2f) *
                        Matrix3x2.CreateRotation(Calc.DegToRad * Rotation) *
                        Matrix3x2.CreateTranslation(frame.Size / 2f) *
                        Matrix3x2.CreateTranslation(Position));

                    manager.Batcher.Image(texture, Vector2.Zero, sprite.Origin, Vector2.One, 0f, Color.White);
                    manager.Batcher.PopMatrix();
                }
            }
        }
    }
}
