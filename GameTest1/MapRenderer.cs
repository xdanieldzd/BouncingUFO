using Foster.Framework;
using GameTest1.Levels;
using System.Numerics;

namespace GameTest1
{
    public class MapRenderer(Manager manager)
    {
        private void DrawErrorMessage(string text)
        {
            manager.Batcher.Text(manager.Assets.Font, text, 1024f, new(0f, manager.Screen.Height), new(0f, 1.5f), Color.Red);
        }

        public void Render(string mapName)
        {
            if (!manager.Assets.Maps.TryGetValue(mapName, out Map? map))
            {
                DrawErrorMessage($"Error: Map '{mapName}' not found!");
                return;
            }

            if (!manager.Assets.Tilesets.TryGetValue(map.Tileset, out Tileset? tileset))
            {
                DrawErrorMessage($"Error: Tileset {map.Tileset} not found!");
                return;
            }

            if (tileset.CellTextures == null)
                tileset.GenerateSubtextures(manager.GraphicsDevice);

            Render(map, tileset);
        }

        public void Render(Map? map, Tileset? tileset)
        {
            if (map == null)
            {
                DrawErrorMessage("Error: Map is null!");
                return;
            }

            if (tileset == null)
            {
                DrawErrorMessage("Error: Tileset is null!");
                return;
            }

            for (var i = 0; i < map.Layers.Count; i++)
            {
                for (var x = 0; x < map.Size.X; x++)
                {
                    for (var y = 0; y < map.Size.Y; y++)
                    {
                        var cellPos = new Vector2(x, y) * tileset.CellSize;
                        if (tileset.CellTextures == null)
                            manager.Batcher.Rect(cellPos, tileset.CellSize, Color.Red);
                        else
                        {
                            var cellOffset = y * map.Size.X + x;
                            var cellValue = map.Layers[i].Tiles[cellOffset];
                            manager.Batcher.Image(tileset.CellTextures[cellValue], cellPos, Color.White);
                        }
                    }
                }
            }
        }
    }
}
