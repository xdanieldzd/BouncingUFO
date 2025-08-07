using System.Text.Json.Serialization;

namespace GameTest1.Game.Sprites
{
    public record Animation
    {
        [JsonInclude]
        public string Name = string.Empty;
        [JsonInclude]
        public int FirstFrame = 0;
        [JsonInclude]
        public int FrameCount = 0;

        public float Duration = 0f;
    }
}
