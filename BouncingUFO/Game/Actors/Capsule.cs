namespace BouncingUFO.Game.Actors
{
    public class Capsule : ActorBase
    {
        public Capsule(Manager manager, LevelManager level, int mapLayer = 0, int argument = 0) : base(manager, level, mapLayer, argument)
        {
            Class = ActorClass.Collectible;
            Sprite = manager.Assets.Sprites["Capsule"];
            Hitbox = new(new(2, 2, 12, 12));
            DrawPriority = 50;
            HasShadow = true;
            PlayAnimation("Idle");

            Offset.Y = Random.Shared.NextSingle() * 2f - 1f;
            BobDirection = Offset.Y < 0 ? -1f : 1f;

            IsRunning = true;
        }

        public override void Created()
        {
            Position.Y -= 8;
        }
    }
}
