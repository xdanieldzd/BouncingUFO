using Foster.Framework;
using System.Numerics;
using System.Text.Json.Serialization;

namespace BouncingUFO.Game
{
    public class GraphicsSheet
    {
        [JsonInclude]
        public string Name = string.Empty;
        [JsonInclude]
        public string ImageFile = string.Empty;
        [JsonInclude]
        public Dictionary<string, Rect> Rectangles = [];

        [JsonIgnore]
        public Texture? Texture = null;

        public void CreateTextures(GraphicsDevice graphicsDevice)
        {
            if (!string.IsNullOrWhiteSpace(ImageFile))
                Texture = new(graphicsDevice, new(ImageFile), $"GraphicsSheet {Name}");
        }

        public Subtexture GetSubtexture(string name, Vector2 offset = new()) =>
            Rectangles.TryGetValue(name, out Rect rect) ? new(Texture, rect + offset) : Subtexture.Empty;

        public Subtexture GetSubtexture(int idx, Vector2 offset = new()) =>
            new(Texture, Rectangles.ElementAtOrDefault(idx).Value + offset);
    }
}
