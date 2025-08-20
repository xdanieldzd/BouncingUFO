using Foster.Framework;
using GameTest1.Game.Levels;
using GameTest1.GameStates;

namespace GameTest1.Game.Actors
{
    public class CapsuleSpawner : ActorBase
    {
        private const string actorToSpawn = "Capsule";
        private const int maxSpawnAttempts = 30;

        public CapsuleSpawner(Manager manager, InGame gameState, Map map, Tileset tileset, int mapLayer = 0, int argument = 0) : base(manager, gameState, map, tileset, mapLayer, argument)
        {
            Class = ActorClass.None;

            for (var i = 0; i < argument; i++)
            {
                var actor = gameState.CreateActor(actorToSpawn, null, MapLayer, 0);
                for (var j = 0; j < maxSpawnAttempts; j++)
                {
                    actor.Position = new Point2(Random.Shared.Next(0, map.Size.X), Random.Shared.Next(0, map.Size.Y)) * tileset.CellSize;
                    actor.Created();

                    actor.AnimationTimer = Random.Shared.NextSingle();

                    if (gameState.GetFirstOverlapActor(actor.Position, actor.Hitbox.Rectangle, ActorClass.None) != null)
                        continue;

                    var isLocationSpawnable = true;

                    foreach (var cellPos in GetMapCells(actor.Position, actor.Hitbox.Rectangle, tileset, map))
                    {
                        foreach (var layer in map.Layers.Where((_, i) => i <= MapLayer))
                        {
                            var cellFlags = tileset.CellFlags[layer.Tiles[cellPos.Y * map.Size.X + cellPos.X]];
                            if (cellFlags != CellFlag.Empty &&
                                (!cellFlags.Has(CellFlag.Ground) || cellFlags.Has(CellFlag.Wall) || cellFlags.Has(CellFlag.Damaging) || cellFlags.Has(CellFlag.Healing)))
                            {
                                isLocationSpawnable = false;
                                break;
                            }
                        }
                    }

                    if (isLocationSpawnable)
                    {
                        gameState.SpawnActor(actor);
                        break;
                    }
                }
            }

            IsVisible = false;
            IsRunning = true;
        }
    }
}
