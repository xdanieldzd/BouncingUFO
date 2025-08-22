using System.Text.Json.Serialization;

namespace GameTest1.Game
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
