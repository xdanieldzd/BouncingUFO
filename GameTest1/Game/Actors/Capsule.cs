using GameTest1.Game.Levels;
using GameTest1.GameStates;

namespace GameTest1.Game.Actors
{
    public class Capsule : ActorBase
    {
        public Capsule(Manager manager, InGame gameState, Map map, Tileset tileset, int argument) : base(manager, gameState, map, tileset, argument)
        {
            Class = ActorClass.Collectible;
            Sprite = manager.Assets.Sprites["Capsule"];
            Hitbox = new(new(0, 0, 16, 16));
            MapLayer = 0;
            DrawPriority = 50;
            Shadow.Enabled = true;
            PlayAnimation("Idle");

            Elevation = Random.Shared.NextSingle() * 2f - 1f;
            BobDirection = Elevation < 0f ? -1f : 1f;

            IsRunning = true;
        }
    }
}
