using Foster.Framework;
using GameTest1.Game.States;
using GameTest1.Game.Levels;
using System.Numerics;

namespace GameTest1.Game.Actors
{
    public class Player : ActorBase
    {
        private const float acceleration = 1500f, friction = 100f, maxSpeed = 200f;
        private const float spriteRotation = 10f;
        private const float bounceCooldown = 25f;
        private const int maxEnergy = 69;

        public enum State { Normal, InputDisabled }
        public State CurrentState;

        private Vector2 currentBounceCooldown = Vector2.Zero;

        public Vector2 BounceCooldown => currentBounceCooldown;

        public int energy = 0;

        public Player(Manager manager, InGame gameState, Map map, Tileset tileset, int mapLayer = 0, int argument = 0) : base(manager, gameState, map, tileset, mapLayer, argument)
        {
            Class = ActorClass.Solid | ActorClass.Player;
            Sprite = manager.Assets.Sprites["Player"];
            Hitbox = new(new(0, 12, 32, 12));
            DrawPriority = 100;
            HasShadow = true;
            BobSpeed = 5f;
            BobDirection = 1f;
            PlayAnimation("Idle");

            CurrentState = State.InputDisabled;

            energy = maxEnergy;

            IsRunning = true;
        }

        public override void Created()
        {
            Position -= Hitbox.Rectangle.Center / 2 + Point2.UnitY * 8;
            gameState.SetCameraFollowActor(this);
        }

        public override void OnCollisionX(ActorBase? other)
        {
            if ((other != null && other.Class.HasFlag(ActorClass.Solid)) || other == null)
            {
                Velocity.X = -Velocity.X;
                veloRemainder.X = -veloRemainder.X;
                currentBounceCooldown.X = bounceCooldown;
                energy--;
            }
        }

        public override void OnCollisionY(ActorBase? other)
        {
            if ((other != null && other.Class.HasFlag(ActorClass.Solid)) || other == null)
            {
                Velocity.Y = -Velocity.Y;
                veloRemainder.Y = -veloRemainder.Y;
                currentBounceCooldown.Y = bounceCooldown;
                energy--;
            }
        }

        public override void Update()
        {
            base.Update();

            var actorHit = gameState.GetFirstOverlapActor(this, ActorClass.None);
            if (actorHit != null)
            {
                OnCollisionX(actorHit);
                OnCollisionY(actorHit);

                gameState.DestroyActor(actorHit);
            }

            energy = Math.Clamp(energy, 0, maxEnergy);

            switch (CurrentState)
            {
                case State.Normal:
                    CalcPlayerVelocityAndRotation(manager.Controls.Move.IntValue, manager.Controls.Action1.Down, manager.Controls.Action2.Down);
                    break;

                case State.InputDisabled:
                    break;
            }

            CalcBounceCooldown();
        }

        private void CalcPlayerVelocityAndRotation(Point2 direction, bool action1, bool action2)
        {
            var accel = acceleration * (action1 ? 2f : 1f) * manager.Time.Delta;

            if (currentBounceCooldown.X == 0f) Velocity.X += direction.X * accel;
            if (currentBounceCooldown.Y == 0f) Velocity.Y += direction.Y * accel;

            var maxRotation = 0f;
            if (Velocity.X < 0f) maxRotation = -spriteRotation * (action1 ? 3f : 1f);
            else if (Velocity.X > 0f) maxRotation = spriteRotation * (action1 ? 3f : 1f);
            Rotation = Calc.Approach(Rotation, maxRotation, manager.Time.Delta * (action1 ? 100f : 25f));

            if (MathF.Abs(Velocity.X) > maxSpeed) Velocity.X = Calc.Approach(Velocity.X, MathF.Sign(Velocity.X) * maxSpeed, 2000f * manager.Time.Delta);
            if (MathF.Abs(Velocity.Y) > maxSpeed) Velocity.Y = Calc.Approach(Velocity.Y, MathF.Sign(Velocity.Y) * maxSpeed, 2000f * manager.Time.Delta);

            var fric = friction * (action2 ? 20f : 1f) * manager.Time.Delta;

            if (direction.X == 0)
            {
                Velocity.X = Calc.Approach(Velocity.X, 0f, fric);
                Rotation = Calc.Approach(Rotation, 0f, friction * manager.Time.Delta);
            }
            if (direction.Y == 0)
                Velocity.Y = Calc.Approach(Velocity.Y, 0f, fric);
        }

        private void CalcBounceCooldown()
        {
            currentBounceCooldown.X = MathF.Floor(Calc.Approach(currentBounceCooldown.X, 0f, manager.Time.Delta));
            currentBounceCooldown.Y = MathF.Floor(Calc.Approach(currentBounceCooldown.Y, 0f, manager.Time.Delta));
        }
    }
}
