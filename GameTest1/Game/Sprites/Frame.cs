using Foster.Framework;
using System.Text.Json.Serialization;

namespace GameTest1.Game.Sprites
{
    public record Frame
    {
        [JsonInclude]
        public Rect Rectangle = new();
        [JsonInclude]
        public float Duration = 0f;

        public Subtexture? Texture;
    }
}
