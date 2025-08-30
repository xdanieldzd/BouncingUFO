using System.Text.Json.Serialization;

namespace BouncingUFO.Game.Levels
{
    public class LevelCollection : List<LevelProgression> { }

    public record LevelProgression
    {
        [JsonInclude]
        public string Title = string.Empty;
        [JsonInclude]
        public List<string> MapNames = [];
    }
}
