using BouncingUFO.Game;
using Foster.Framework;
using System.Numerics;

namespace BouncingUFO.Utilities
{
    public class ParallaxBackground(Manager manager)
    {
        private readonly SpriteFont debugFont = manager.Assets.Fonts["SmallFont"];

        private readonly Dictionary<string, ParallaxLayer> layers = [];
        private readonly Dictionary<string, Vector2> offsets = [];

        public void AddLayer(string name, ParallaxLayer layer)
        {
            layers.Add(name, layer);
            offsets.Add(name, Vector2.Zero);
        }

        public void RemoveLayer(string name)
        {
            layers.Remove(name);
            offsets.Remove(name);
        }

        public void Update()
        {
            foreach (var (name, layer) in layers)
                offsets[name] += layer.ScrollSpeed * manager.Time.Delta;
        }

        public void Render()
        {
            foreach (var (name, layer) in layers)
                manager.Batcher.Image(new Subtexture(layer.Texture, layer.SourceRectangle + offsets[name]), Color.White);

            if (manager.Settings.ShowDebugInfo)
            {
                var position = Vector2.Zero;
                foreach (var (name, layer) in layers)
                {
                    manager.Batcher.Text(debugFont, $"{name}: {layer.SourceRectangle}, {layer.ScrollSpeed} => {offsets[name]}", position, Color.White);
                    position += new Vector2(0f, debugFont.LineHeight);
                }
            }
        }

        public static ParallaxBackground FromGraphicsSheet(Manager manager, GraphicsSheet graphicsSheet, params Vector2[] speeds)
        {
            var instance = new ParallaxBackground(manager);
            foreach (var data in graphicsSheet.Rectangles.Select((x, i) => new { x.Key, x.Value, Index = i }))
            {
                instance.AddLayer(data.Key, new()
                {
                    Texture = graphicsSheet.Texture,
                    SourceRectangle = data.Value,
                    ScrollSpeed = speeds[data.Index]
                });
            }
            return instance;
        }
    }

    public record ParallaxLayer
    {
        public Texture? Texture = null;
        public Rect SourceRectangle = new();
        public Vector2 ScrollSpeed = Vector2.Zero;
    }
}
