using Foster.Framework;
using System.Text.Json.Serialization;

namespace GameTest1.Game.Levels
{
    public record Map
    {
        [JsonInclude]
        public string Title = string.Empty;
        [JsonInclude]
        public Point2 Size = Point2.One;
        [JsonInclude]
        public string Tileset = string.Empty;
        [JsonInclude]
        public List<MapLayer> Layers = [];

        public void ResizeLayers()
        {
            foreach (var layer in Layers)
            {
                var numCells = layer.Tiles.Length;
                var newSize = Size.X * Size.Y;
                if (numCells == newSize) continue;

                var newCells = new int[newSize];
                for (var i = 0; i < Math.Min(numCells, newSize); i++)
                    newCells[i] = layer.Tiles[i];

                layer.Tiles = newCells;
            }
        }
    }
}
