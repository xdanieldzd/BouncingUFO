using Foster.Framework;
using System.Text.Json.Serialization;

namespace GameTest1.Game.Sprites
{
    public record Frame
    {
        [JsonInclude]
        public Point2 SourceCoord = Point2.Zero;
        [JsonInclude]
        public Point2 SourceSize = Point2.Zero;
        [JsonInclude]
        public float Duration = 0f;

        public Subtexture? Texture;

        [JsonIgnore]
        public Rect SourceRectangle => new(SourceCoord, SourceSize);
    }
}
