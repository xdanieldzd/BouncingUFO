using Foster.Framework;
using GameTest1.Game.Levels;
using GameTest1.GameStates;
using GameTest1.Utilities;
using System.Numerics;

namespace GameTest1.Game.Actors
{
    public class Player : ActorBase
    {
        private const float acceleration = 1500f, friction = 100f, maxSpeed = 200f;
        private const float spriteRotation = 10f;
        private const float bobSpeed = 5f;
        private const float bounceCooldown = 25f;
        private const int maxEnergy = 99;

        public enum State { Normal, InputDisabled }
        public State CurrentState;

        private float bobDirection = 0f;
        private Vector2 currentBounceCooldown = Vector2.Zero;

        public Vector2 BounceCooldown => currentBounceCooldown;

        public int energy = 0;

        public Player(Manager manager, InGame gameState, Map map, Tileset tileset) : base(manager, gameState, map, tileset)
        {
            Class = ActorClass.Solid | ActorClass.Player;
            Sprite = manager.Assets.Sprites["Player"];
            Hitbox = new(new(2, 20, 28, 12));
            MapLayer = 0;
            DrawPriority = 100;
            Shadow.Enabled = true;
            PlayAnimation("Idle");

            CurrentState = State.InputDisabled;

            bobDirection = 1f;
            energy = maxEnergy;

            IsRunning = true;
        }

        public override void OnCollisionX()
        {
            Velocity.X = -Velocity.X;
            veloRemainder.X = -veloRemainder.X;
            currentBounceCooldown.X = bounceCooldown;
            energy--;
        }

        public override void OnCollisionY()
        {
            Velocity.Y = -Velocity.Y;
            veloRemainder.Y = -veloRemainder.Y;
            currentBounceCooldown.Y = bounceCooldown;
            energy--;
        }

        public override void Update()
        {
            base.Update();

            var actorHit = gameState.GetFirstOverlapActor(Hitbox.Rectangle + Position, ActorClass.Solid | ActorClass.Collectible);
            if (actorHit != null)
            {
                OnCollisionX();
                OnCollisionY();

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

            CalcBobbing();
            CalcBounceCooldown();
            CalcShadow();
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

        private void CalcBobbing()
        {
            if (bobDirection == 0f) return;
            Elevation = Calc.Approach(Elevation, bobDirection, bobSpeed * manager.Time.Delta);
            if (Elevation >= 1f || Elevation <= -1f) bobDirection = -bobDirection;
        }

        private void CalcBounceCooldown()
        {
            currentBounceCooldown.X = MathF.Floor(Calc.Approach(currentBounceCooldown.X, 0f, manager.Time.Delta));
            currentBounceCooldown.Y = MathF.Floor(Calc.Approach(currentBounceCooldown.Y, 0f, manager.Time.Delta));
        }

        private void CalcShadow()
        {
            if (sprite == null || animation == null) return;

            var frame = sprite.GetFrameAt(animation, animTimer, isLoopingAnim);
            Shadow.Offset = new(0f, frame.Size.Y * 0.35f);
            Shadow.Scale = new Vector2(0.75f, 0.425f) * Calc.ClampedMap(Elevation, -1f, 1f, 0.9f, 1f);
        }
    }
}
