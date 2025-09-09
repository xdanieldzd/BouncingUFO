using Foster.Framework;
using System.Text.Json.Serialization;

namespace BouncingUFO.Game.Levels
{
    public record MapLayer
    {
        [JsonInclude]
        public int[] Tiles = [];

        public MapLayer() { }
        public MapLayer(Point2 size) => Tiles = new int[size.X * size.Y];
    }
}
