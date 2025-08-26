using System.Text.Json.Serialization;

namespace GameTest1.Game.Levels
{
    public record Progression
    {
        [JsonInclude]
        public string Title = string.Empty;
        [JsonInclude]
        public List<string> MapNames = [];
    }
}
