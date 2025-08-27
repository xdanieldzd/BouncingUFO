using Foster.Framework;
using System.Numerics;

namespace BouncingUFO
{
    public static class Extensions
    {
        public static void TextCenteredInBounds(this Batcher batcher, string text, SpriteFont font, RectInt bounds, Color color)
        {
            var wrappedLines = font.WrapText(text, bounds.Width).Select((x, i) => new { Value = x, Index = i }).ToArray();
            foreach (var wrap in wrappedLines)
            {
                var textSegment = text.AsSpan(wrap.Value.Start, wrap.Value.Length);
                var position = bounds.Center - new Vector2(font.WidthOf(textSegment), font.LineHeight * wrappedLines.Length) / 2f + (Vector2.One * font.LineHeight * wrap.Index).ZeroX();
                batcher.Text(font, textSegment, bounds.Width, position, color);
            }
        }
    }
}
