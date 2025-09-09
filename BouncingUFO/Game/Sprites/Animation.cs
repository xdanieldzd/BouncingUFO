using System.Text.Json.Serialization;

namespace BouncingUFO.Game.Sprites
{
    public record Animation
    {
        [JsonInclude]
        public string Name = string.Empty;
        [JsonInclude]
        public int FirstFrame = 0;
        [JsonInclude]
        public int FrameCount = 0;

        [JsonIgnore]
        public float Duration = 0f;
    }
}
