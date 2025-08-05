using Foster.Framework;
using System.Text.Json.Serialization;

namespace GameTest1.Game.Sprites
{
    public record Frame
    {
        [JsonInclude]
        public Subtexture Texture = default;
        [JsonInclude]
        public float Duration = 0f;
    }
}
