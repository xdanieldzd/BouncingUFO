using Foster.Framework;
using GameTest1.Game.Levels;
using System.Numerics;

namespace GameTest1.Game.Actors
{
    public class Player : ActorBase
    {
        private const float acceleration = 1500f, friction = 100f, maxSpeed = 200f;
        private const float spriteRotation = 10f;
        private const float bobExtents = 2f, bobSpeed = 20f;
        private const float bounceCooldown = 25f;

        public enum State { Normal, InputDisabled }
        public State CurrentState;

        private float bobDirection = 0f;
        private Vector2 currentBounceCooldown = Vector2.Zero;

        public Vector2 BounceCooldown => currentBounceCooldown;

        public Player(Manager manager, Map map, Tileset tileset) : base(manager, map, tileset)
        {
            Sprite = manager.Assets.Sprites["PlayerUFO"];
            Hitbox = new(new(0, 20, 32, 12));
            MapLayer = 0;
            PlayAnimation("Idle");

            CurrentState = State.InputDisabled;
        }

        public override void OnCollisionX()
        {
            Velocity.X = -Velocity.X;
            veloRemainder.X = -veloRemainder.X;
            currentBounceCooldown.X = bounceCooldown;
        }

        public override void OnCollisionY()
        {
            Velocity.Y = -Velocity.Y;
            veloRemainder.Y = -veloRemainder.Y;
            currentBounceCooldown.Y = bounceCooldown;
        }

        public override void Update()
        {
            base.Update();

            switch (CurrentState)
            {
                case State.Normal:
                    CalcPlayerVelocityAndRotation(manager.Controls.Move.IntValue, manager.Controls.Action1.Down, manager.Controls.Action2.Down);
                    break;

                case State.InputDisabled:
                    break;
            }

            CalcPlayerBobbing();
            CalcPlayerBounceCooldown();
        }

        private void CalcPlayerVelocityAndRotation(Point2 direction, bool action1, bool action2)
        {
            var accel = acceleration * (action1 ? 2.5f : 1f) * manager.Time.Delta;

            if (currentBounceCooldown.X == 0f)
            {
                Velocity.X += direction.X * accel;

                if (Velocity.X < 0) Rotation = -spriteRotation;
                else if (Velocity.X > 0) Rotation = spriteRotation;
            }

            if (currentBounceCooldown.Y == 0f)
                Velocity.Y += direction.Y * accel;

            if (MathF.Abs(Velocity.X) > maxSpeed) Velocity.X = Calc.Approach(Velocity.X, MathF.Sign(Velocity.X) * maxSpeed, 2000f * manager.Time.Delta);
            if (MathF.Abs(Velocity.Y) > maxSpeed) Velocity.Y = Calc.Approach(Velocity.Y, MathF.Sign(Velocity.Y) * maxSpeed, 2000f * manager.Time.Delta);

            var fric = friction * (action2 ? 20f : 1f) * manager.Time.Delta;

            if (direction.X == 0)
            {
                Velocity.X = Calc.Approach(Velocity.X, 0f, fric);
                Rotation = Calc.Approach(Rotation, 0f, fric * 5f);
            }
            if (direction.Y == 0)
                Velocity.Y = Calc.Approach(Velocity.Y, 0f, fric);
        }

        private void CalcPlayerBobbing()
        {
            if (bobDirection == 0f) bobDirection = 1f;

            Elevation = Calc.Approach(Elevation, bobExtents * bobDirection, bobSpeed * manager.Time.Delta);

            if (Elevation >= bobExtents || Elevation <= -bobExtents)
                bobDirection = -bobDirection;
        }

        private void CalcPlayerBounceCooldown()
        {
            currentBounceCooldown.X = MathF.Floor(Calc.Approach(currentBounceCooldown.X, 0f, manager.Time.Delta));
            currentBounceCooldown.Y = MathF.Floor(Calc.Approach(currentBounceCooldown.Y, 0f, manager.Time.Delta));
        }
    }
}
