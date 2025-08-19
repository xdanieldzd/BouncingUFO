using Foster.Framework;
using GameTest1.Game.Actors;
using GameTest1.Game.Levels;
using System.Numerics;

namespace GameTest1
{
    public class MapRenderer(Manager manager)
    {
        public void Render(Map? map, Tileset? tileset, IEnumerable<ActorBase> actors, bool debug = false)
        {
            if (map != null && tileset != null && tileset.CellTextures != null)
            {
                for (var i = 0; i < map.Layers.Count; i++)
                {
                    manager.Batcher.PushBlend(BlendMode.NonPremultiplied);
                    for (var y = 0; y < map.Size.Y; y++)
                    {
                        for (var x = 0; x < map.Size.X; x++)
                        {
                            var cellPos = new Vector2(x, y) * tileset.CellSize;
                            var cellOffset = y * map.Size.X + x;
                            var cellValue = map.Layers[i].Tiles[cellOffset];
                            var cellFlags = tileset.CellFlags[cellValue];
                            manager.Batcher.Image(tileset.CellTextures[cellValue], cellPos, cellFlags.Has(CellFlag.Translucent) ? new(255, 255, 255, 64) : Color.White);

                            if (debug)
                                manager.Batcher.RectLine(new(cellPos, tileset.CellSize), 1f, new(0, 0, 0, 64));
                        }
                    }
                    manager.Batcher.PopBlend();

                    var actorsToRender = actors
                        .Where(x => x.IsVisible && x.MapLayer == i)
                        .OrderBy(x => x.DrawPriority)
                        .OrderBy(x => x.TransformedPosition.Y + x.Frame?.Size.Y);

                    foreach (var actor in actorsToRender)
                        actor.RenderShadow();

                    foreach (var actor in actorsToRender)
                    {
                        actor.RenderSprite();

                        if (debug)
                        {
                            actor.Hitbox.Render(manager.Batcher, actor.Position, Color.Red);
                            manager.Batcher.Circle(actor.Position + actor.Sprite?.Origin ?? Vector2.Zero, 2f, 5, Color.Magenta);
                        }
                    }
                }

                if (debug)
                {
                    foreach (var spawn in map.Spawns)
                    {
                        var spawnPos = new Vector2(spawn.Position.X, spawn.Position.Y) * tileset.CellSize;
                        manager.Batcher.Rect(spawnPos, tileset.CellSize, new Color(128, 64, 0, 64));
                        manager.Batcher.RectLine(new(spawnPos, tileset.CellSize), 2f, new Color(255, 128, 0, 128));
                    }

                    foreach (var actor in actors)
                    {
                        foreach (var hit in actor.GetMapCells())
                        {
                            var cellPos = new Vector2(hit.X, hit.Y) * tileset.CellSize;
                            manager.Batcher.Rect(cellPos, tileset.CellSize, new Color(0, 0, 64, 64));
                        }
                    }
                }
            }
        }
    }
}
