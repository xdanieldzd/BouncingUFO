using System.Text.Json.Serialization;

namespace BouncingUFO.Game.UI
{
    public record DialogText
    {
        [JsonInclude]
        public string SpeakerName = string.Empty;
        [JsonInclude]
        public List<string> TextStrings = [];

        [JsonIgnore]
        public bool HasBeenShownOnce = false;
    }
}
