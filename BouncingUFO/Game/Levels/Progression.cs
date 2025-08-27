using System.Text.Json.Serialization;

namespace BouncingUFO.Game.Levels
{
    public record Progression
    {
        [JsonInclude]
        public string Title = string.Empty;
        [JsonInclude]
        public List<string> MapNames = [];
    }
}
