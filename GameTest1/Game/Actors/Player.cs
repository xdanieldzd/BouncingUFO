using Foster.Framework;

namespace GameTest1.Game.Actors
{
    public class Player : ActorBase
    {
        private const float playerAcceleration = 1500f;
        private const float playerFriction = 100f;
        private const float playerMaxSpeed = 350f;
        private const float playerSpriteRotation = 10f;

        public enum State { Normal, InputDisabled }
        public State CurrentState;

        public Player(Manager manager) : base(manager)
        {
            Position = (manager.Screen.Bounds.Center - Hitbox.Rectangle.Size / 2f).FloorToPoint2();
            Sprite = manager.Assets.Sprites["PlayerUFO"];
            Hitbox = new(new(16, 16));
            PlayAnimation("Idle");

            CurrentState = State.InputDisabled;
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
        }

        private void CalcPlayerVelocityAndRotation(Point2 direction, bool action1, bool action2)
        {
            var acceleration = playerAcceleration * (action1 ? 5f : 1f) * manager.Time.Delta;

            Velocity.X += direction.X * acceleration;
            Velocity.Y += direction.Y * acceleration;

            if (Velocity.X < 0) Rotation = -playerSpriteRotation;
            else if (Velocity.X > 0) Rotation = playerSpriteRotation;

            if (MathF.Abs(Velocity.X) > playerMaxSpeed) Velocity.X = Calc.Approach(Velocity.X, MathF.Sign(Velocity.X) * playerMaxSpeed, 2000f * manager.Time.Delta);
            if (MathF.Abs(Velocity.Y) > playerMaxSpeed) Velocity.Y = Calc.Approach(Velocity.Y, MathF.Sign(Velocity.Y) * playerMaxSpeed, 2000f * manager.Time.Delta);

            var friction = playerFriction * (action2 ? 15f : 1f) * manager.Time.Delta;

            if (direction.X == 0)
            {
                Velocity.X = Calc.Approach(Velocity.X, 0f, friction);
                Rotation = Calc.Approach(Rotation, 0f, friction);
            }
            if (direction.Y == 0)
                Velocity.Y = Calc.Approach(Velocity.Y, 0f, friction);
        }
    }
}
