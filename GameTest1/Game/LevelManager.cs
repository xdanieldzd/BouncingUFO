using Foster.Framework;
using GameTest1.Game.Actors;
using GameTest1.Game.Levels;
using System.Numerics;

namespace GameTest1.Game
{
    public class LevelManager(Manager manager)
    {
        private readonly Dictionary<string, Type> actorTypeDictionary = new()
        {
            { "Player", typeof(Player) },
            { "Capsule", typeof(Capsule) },
            { "CapsuleSpawner", typeof(CapsuleSpawner) }
        };

        public Map? Map;
        public Tileset? Tileset;

        public readonly List<ActorBase> Actors = [];
        public readonly List<ActorBase> ActorsToDestroy = [];

        public bool IsLevelLoaded => Map != null && Tileset != null;
        public Point2 SizeInPixels => Map?.Size * Tileset?.CellSize ?? Point2.Zero;

        public void Load(string mapName)
        {
            Map = manager.Assets.Maps[mapName];
            Tileset = manager.Assets.Tilesets[Map.Tileset];

            Reset();
        }

        public void Reset()
        {
            if (Map == null) return;

            Actors.Clear();
            ActorsToDestroy.Clear();

            foreach (var spawn in Map.Spawns)
                SpawnActor(spawn.ActorType, spawn.Position, spawn.MapLayer, spawn.Argument);
        }

        public void Update()
        {
            for (var i = 0; i < ActorsToDestroy.Count; i++)
            {
                ActorsToDestroy[i].Destroyed();
                Actors.Remove(ActorsToDestroy[i]);
            }
            ActorsToDestroy.Clear();

            foreach (var actor in Actors)
                actor.Update();
        }

        public void Render(bool debug = false)
        {
            if (Map == null || Tileset == null || Tileset.CellTextures == null) return;

            var actorsToRender = Actors
                .Where(x => x.IsVisible)
                .OrderBy(x => x.DrawPriority)
                .ToList();

            manager.Batcher.PushBlend(BlendMode.NonPremultiplied);

            for (var i = 0; i < Map.Layers.Count; i++)
            {
                for (var y = 0; y < Map.Size.Y; y++)
                {
                    for (var x = 0; x < Map.Size.X; x++)
                    {
                        var cellPos = new Vector2(x, y) * Tileset.CellSize;
                        var cellOffset = y * Map.Size.X + x;
                        var cellValue = Map.Layers[i].Tiles[cellOffset];
                        var cellFlags = Tileset.CellFlags[cellValue];

                        manager.Batcher.Image(Tileset.CellTextures[cellValue], cellPos, cellFlags.Has(CellFlag.Translucent) ? new(255, 255, 255, 64) : Color.White);

                        if (debug)
                            manager.Batcher.RectLine(new(cellPos, Tileset.CellSize), 1f, new(0, 0, 0, 64));
                    }

                    var actorsInRange = actorsToRender.Where(x => x.MapLayer == i && (x.Position + x.Offset).Y <= y * Tileset.CellSize.Y);

                    foreach (var actor in actorsInRange)
                        actor.RenderShadow();

                    foreach (var actor in actorsInRange)
                    {
                        actor.RenderSprite();

                        if (debug)
                        {
                            manager.Batcher.RectLine(new(actor.Position + actor.Offset, actor.Frame?.Size ?? Vector2.Zero), 2f, Color.White);
                            actor.Hitbox.Render(manager.Batcher, actor.Position, Color.Red);
                            manager.Batcher.Circle(actor.Position + actor.Sprite?.Origin ?? Vector2.Zero, 2f, 5, Color.Magenta);
                        }
                    }
                    actorsToRender.RemoveAll(x => actorsInRange.Contains(x));
                }
            }

            manager.Batcher.PopBlend();

            if (debug)
            {
                foreach (var spawn in Map.Spawns)
                {
                    var spawnPos = new Vector2(spawn.Position.X, spawn.Position.Y) * Tileset.CellSize;
                    manager.Batcher.Rect(spawnPos, Tileset.CellSize, new Color(128, 64, 0, 64));
                    manager.Batcher.RectLine(new(spawnPos, Tileset.CellSize), 2f, new Color(255, 128, 0, 128));
                }

                foreach (var actor in Actors)
                {
                    foreach (var hit in actor.GetMapCells())
                    {
                        var cellPos = new Vector2(hit.X, hit.Y) * Tileset.CellSize;
                        manager.Batcher.Rect(cellPos, Tileset.CellSize, new Color(0, 0, 64, 64));
                    }
                }
            }
        }

        public void DestroyAllActors() => ActorsToDestroy.AddRange(Actors);

        public ActorBase CreateActor(string actorType, Point2? position = null, int mapLayer = 0, int argument = 0)
        {
            if (actorTypeDictionary.TryGetValue(actorType, out Type? type))
                return CreateActor(type, position, mapLayer, argument);
            else
                throw new ActorException($"Actor type '{actorType}' not recognized");
        }

        public ActorBase CreateActor(Type type, Point2? position = null, int mapLayer = 0, int argument = 0)
        {
            var actor = Activator.CreateInstance(type, manager, this, mapLayer, argument) as ActorBase ??
                throw new ActorException(type, "Failed to create actor instance");
            actor.Position = position * Tileset?.CellSize ?? Point2.One;
            actor.Created();
            return actor;
        }

        public void SpawnActor(ActorBase actor) => Actors.Add(actor);
        public void SpawnActor(string actorType, Point2? position = null, int mapLayer = 0, int argument = 0) => Actors.Add(CreateActor(actorType, position, mapLayer, argument));
        public void SpawnActor(Type type, Point2? position = null, int mapLayer = 0, int argument = 0) => Actors.Add(CreateActor(type, position, mapLayer, argument));

        public void DestroyActor(ActorBase actor)
        {
            if (!ActorsToDestroy.Contains(actor))
                ActorsToDestroy.Add(actor);
        }

        public IEnumerable<T> GetActors<T>() where T : ActorBase => Actors.Where(x => x is T && !ActorsToDestroy.Contains(x)).Cast<T>();
        public IEnumerable<ActorBase> GetActors(ActorClass actorClass) => Actors.Where(x => x.Class.Has(actorClass) && !ActorsToDestroy.Contains(x));

        public T? GetFirstActor<T>() where T : ActorBase => Actors.FirstOrDefault(x => x is T && !ActorsToDestroy.Contains(x)) as T;
        public ActorBase? GetFirstActor(ActorClass actorClass) => Actors.FirstOrDefault(x => x.Class.Has(actorClass) && !ActorsToDestroy.Contains(x));

        public ActorBase? GetFirstOverlapActor(Point2 position, RectInt hitboxRect, ActorClass actorClass) => GetFirstOverlapActor(position, hitboxRect, actorClass, Actors);
        public ActorBase? GetFirstOverlapActor(ActorBase actor, ActorClass actorClass) => GetFirstOverlapActor(actor.Position, actor.Hitbox.Rectangle, actorClass, Actors.Where(x => x != actor));

        private static ActorBase? GetFirstOverlapActor(Point2 position, RectInt hitboxRect, ActorClass actorClass, IEnumerable<ActorBase> actorsToCheck)
        {
            foreach (var other in actorsToCheck)
            {
                if (other.Class.HasFlag(actorClass) &&
                    (hitboxRect + position).Overlaps(other.Hitbox.Rectangle + other.Position))
                    return other;
            }
            return null;
        }
    }
}
