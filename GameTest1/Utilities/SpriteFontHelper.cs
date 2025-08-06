using Foster.Framework;
using System.Numerics;

namespace GameTest1.Utilities
{
    public enum SpriteFontHighlightType { None, Outline, DropShadowThick, DropShadowThin }

    public static class SpriteFontHelper
    {
        private readonly static Point2[] fontHighlightOffsetsOutline =
        [
            /* ooo */   (-1, -1), ( 0, -1), ( 1, -1),
            /* oOo */   (-1,  0),           ( 1,  0),
            /* ooo */   (-1,  1), ( 0,  1), ( 1,  1)
        ];

        private readonly static Point2[] fontHighlightOffsetsDropShadowThick =
        [
            /* ... */
            /* .Oo */                       ( 1,  0),
            /* .oo */             ( 0,  1), ( 1,  1)
        ];

        private readonly static Point2[] fontHighlightOffsetsDropShadowThin =
        [
            /* ... */
            /* .O. */
            /* ..o */                       ( 1,  1)
        ];

        /* Expects font image with 6 lines of 16 characters, total 96 characters, from ASCII 0x20 (space character) to 0x7F (DEL control code)
           TODO: add support for extended ASCII (probably codepage 1252 -- https://en.wikipedia.org/wiki/Windows-1252) */

        public static SpriteFont GenerateFontFromImage(GraphicsDevice graphicsDevice, string path, float spaceWidth = 3f, float charSpacing = 1f, SpriteFontHighlightType highlightType = SpriteFontHighlightType.None, Color highlightColor = default)
        {
            var highlightOffsets = highlightType switch
            {
                SpriteFontHighlightType.Outline => fontHighlightOffsetsOutline,
                SpriteFontHighlightType.DropShadowThick => fontHighlightOffsetsDropShadowThick,
                SpriteFontHighlightType.DropShadowThin => fontHighlightOffsetsDropShadowThin,
                _ => [],
            };
            return GenerateFontFromImage(graphicsDevice, path, spaceWidth, charSpacing, highlightOffsets, highlightColor);
        }

        public static SpriteFont GenerateFontFromImage(GraphicsDevice graphicsDevice, string path, float spaceWidth = 3f, float charSpacing = 1f, Point2[]? highlightOffsets = null, Color highlightColor = default)
        {
            var fontImage = new Image(path);
            var characterSize = new Point2(fontImage.Width / 16, fontImage.Height / 6);

            var spriteFont = new SpriteFont(graphicsDevice) { LineGap = characterSize.Y };

            var characterData = new Dictionary<char, (float, Rect)>();
            for (var y = 0; y < fontImage.Height; y += characterSize.Y)
            {
                for (var x = 0; x < fontImage.Width; x += characterSize.X)
                {
                    var character = (char)(' ' + ((y / characterSize.Y * fontImage.Width) + x) / characterSize.X);

                    var currentCharacterWidth = 0f;
                    for (var pixelY = 0; pixelY < characterSize.Y; pixelY++)
                    {
                        for (var pixelX = 0; pixelX < characterSize.X; pixelX++)
                        {
                            var pixelColor = fontImage[x + pixelX, y + pixelY];
                            if (pixelColor.A != 0) currentCharacterWidth = Math.Max(currentCharacterWidth, pixelX + charSpacing);
                        }
                    }

                    if (character == ' ') currentCharacterWidth = spaceWidth;
                    characterData.Add(character, (currentCharacterWidth, new(x, y, characterSize.X, characterSize.Y)));
                }
            }

            if (highlightOffsets != null)
            {
                var highlightPositions = new List<Point2>();
                for (var y = 0; y < fontImage.Height; y++)
                {
                    for (var x = 0; x < fontImage.Width; x++)
                    {
                        if (fontImage[x, y].A != 0)
                        {
                            foreach (var offset in highlightOffsets)
                            {
                                var highlightPosition = new Point2(x + offset.X, y + offset.Y);
                                if (highlightPosition.X < 0 || highlightPosition.X >= fontImage.Width || highlightPosition.Y < 0 || highlightPosition.Y >= fontImage.Height) continue;

                                var pixelColor = fontImage[highlightPosition.X, highlightPosition.Y];
                                if (pixelColor.A != 0) continue;

                                highlightPositions.Add(highlightPosition);
                            }
                        }
                    }
                }

                foreach (var position in highlightPositions)
                    fontImage[position.X, position.Y] = highlightColor;
            }

            var texture = new Texture(graphicsDevice, fontImage, Path.GetFileNameWithoutExtension(path));
            foreach (var (character, (charaWidth, charaRect)) in characterData)
                spriteFont.AddCharacter(character, charaWidth, Vector2.Zero, new(texture, charaRect));

            return spriteFont;
        }
    }
}
