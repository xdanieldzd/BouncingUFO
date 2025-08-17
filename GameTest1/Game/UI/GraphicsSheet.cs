using Foster.Framework;
using System.Text.Json.Serialization;

namespace GameTest1.Game.UI
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
        [JsonIgnore]
        public Dictionary<string, Subtexture?> Subtextures = [];

        public void CreateTextures(GraphicsDevice graphicsDevice)
        {
            if (!string.IsNullOrWhiteSpace(ImageFile))
            {
                Texture = new(graphicsDevice, new(ImageFile), $"GraphicsSheet {Name}");
                foreach (var (name, rectangle) in Rectangles)
                    Subtextures.Add(name, new(Texture, rectangle));
            }
        }
    }
}
