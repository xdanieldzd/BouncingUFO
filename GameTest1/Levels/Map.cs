using Foster.Framework;
using System.Text.Json.Serialization;

namespace GameTest1.Levels
{
    public record Map
    {
        [JsonInclude]
        public string Name = string.Empty;
        [JsonInclude]
        public Point2 Size = Point2.Zero;
        [JsonInclude]
        public string Tileset = string.Empty;
        [JsonInclude]
        public MapLayer[] Layers = [];

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

        public static Map GetTestmap()
        {
            var map = new Map()
            {
                Name = "Test map",
                Size = new(30, 17),
                Tileset = "Test",
                Layers = new MapLayer[2]
            };

            map.Layers[0] = new MapLayer(map.Size);
            map.Layers[1] = new MapLayer(map.Size);

            map.Layers[0].Tiles[0] = 1;
            map.Layers[0].Tiles[1] = 2;
            map.Layers[0].Tiles[2] = 3;
            map.Layers[0].Tiles[3] = 4;
            map.Layers[0].Tiles[4] = 5;
            map.Layers[0].Tiles[5] = 6;
            map.Layers[0].Tiles[6] = 7;
            map.Layers[0].Tiles[7] = 8;

            return map;
        }
    }
}
