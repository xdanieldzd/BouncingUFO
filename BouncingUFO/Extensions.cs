using Foster.Framework;
using System.Numerics;
using System.Text.Json;

namespace BouncingUFO
{
    public static class Extensions
    {
        private readonly static JsonSerializerOptions serializerOptions = new() { WriteIndented = true, IncludeFields = true };

        public static void TextCenteredInBounds(this Batcher batcher, string text, SpriteFont font, RectInt bounds, Color color)
        {
            var wrappedLines = font.WrapText(text, bounds.Width).Select((x, i) => new { Value = x, Index = i }).ToArray();
            foreach (var wrap in wrappedLines)
            {
                var textSegment = text.AsSpan(wrap.Value.Start, wrap.Value.Length);
                var position = bounds.Center - new Vector2(font.WidthOf(textSegment), font.Size * wrappedLines.Length) / 2f + (Vector2.One * font.Size * wrap.Index).ZeroX();
                batcher.Text(font, textSegment, bounds.Width, position, color);
            }
        }

        public static void SerializeToStorage<T>(this Storage storage, T? objToSerialize, string path) where T : new() =>
            storage.WriteAllText(path, JsonSerializer.Serialize(objToSerialize, serializerOptions));

        public static T? DeserializeFromStorage<T>(this Storage storage, string path) where T : new() =>
            storage.FileExists(path) ? JsonSerializer.Deserialize<T>(storage.ReadAllText(path), serializerOptions) ?? default : default;
    }
}
