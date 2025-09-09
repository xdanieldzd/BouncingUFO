using Foster.Framework;
using System.Text.Json.Serialization;

namespace BouncingUFO.Game.Levels
{
    public record Spawn
    {
        [JsonInclude]
        public string ActorType = string.Empty;
        [JsonInclude]
        public Point2 Position = Point2.Zero;
        [JsonInclude]
        public int MapLayer = 0;
        [JsonInclude]
        public int Argument = 0;
    }
}
