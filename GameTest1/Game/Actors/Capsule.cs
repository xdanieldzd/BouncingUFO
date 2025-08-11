using Foster.Framework;
using GameTest1.Game.Levels;
using System.Numerics;

namespace GameTest1.Game.Actors
{
    public class Capsule : ActorBase
    {
        private const float bobSpeed = 10f;

        private float bobDirection = 0f;

        public Capsule(Manager manager, Map map, Tileset tileset) : base(manager, map, tileset)
        {
            Sprite = manager.Assets.Sprites["Capsule"];
            Hitbox = new(new(0, 8, 16, 16));
            MapLayer = 0;
            DrawPriority = 50;
            Shadow.Enabled = true;
            PlayAnimation("Idle");

            Elevation = Random.Shared.NextSingle() * 2f - 1f;
            bobDirection = Elevation < 0f ? -1f : 1f;

            IsRunning = true;
        }

        public override void Update()
        {
            base.Update();

            CalcBobbing();
            CalcShadow();
        }

        private void CalcBobbing()
        {
            if (bobDirection == 0f) return;
            Elevation = Calc.Approach(Elevation, bobDirection, bobSpeed * manager.Time.Delta);
            if (Elevation >= 1f || Elevation <= -1f) bobDirection = -bobDirection;
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
