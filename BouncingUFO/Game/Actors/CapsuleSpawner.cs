using BouncingUFO.Game.Levels;
using Foster.Framework;

namespace BouncingUFO.Game.Actors
{
    public class CapsuleSpawner : ActorBase
    {
        private const string actorToSpawn = "Capsule";
        private const int maxSpawnAttempts = 30;

        public CapsuleSpawner(Manager manager, LevelManager level, int mapLayer = 0, int argument = 0) : base(manager, level, mapLayer, argument)
        {
            if (level.Map == null || level.Tileset == null) return;

            Class = ActorClass.None;

            for (var i = 0; i < argument; i++)
            {
                var actor = level.CreateActor(actorToSpawn, null, MapLayer, 0);
                for (var j = 0; j < maxSpawnAttempts; j++)
                {
                    actor.Position = new Point2(Random.Shared.Next(0, level.Map.Size.X), Random.Shared.Next(0, level.Map.Size.Y)) * level.Tileset.CellSize;
                    actor.Created();

                    actor.AnimationTimer = Random.Shared.NextSingle();

                    if (level.GetFirstOverlapActor(actor.Position, actor.Hitbox.Rectangle, ActorClass.None) != null)
                        continue;

                    var isLocationSpawnable = true;

                    foreach (var cellPos in GetMapCells(actor.Position, actor.Hitbox.Rectangle, level.Map, level.Tileset))
                    {
                        foreach (var layer in level.Map.Layers.Where((_, i) => i <= MapLayer))
                        {
                            var cellFlags = level.Tileset.CellFlags[layer.Tiles[cellPos.Y * level.Map.Size.X + cellPos.X]];
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
                        level.SpawnActor(actor);
                        break;
                    }
                }
            }

            IsVisible = false;
            IsRunning = true;
        }
    }
}
