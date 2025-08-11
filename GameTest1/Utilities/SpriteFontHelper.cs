using Foster.Framework;
using System.Numerics;

namespace GameTest1.Utilities
{
    public enum SpriteFontHighlightType { None, Outline, DropShadowThick, DropShadowThin }

    public static class SpriteFontHelper
    {
        private const int defaultFirstCharacter = 0x20;
        private const float defaultSpaceCharaWidth = 3f;
        private const float defaultCharaSpacing = 1f;

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

        /* Developed using font image with 6 lines of 16 characters, total 96 characters, from ASCII 0x20 (space character) to 0x7F (DEL control code)
           TODO: - verify support for font images with different dimensions, etc.
                 - add support for extended ASCII (probably codepage 1252 -- https://en.wikipedia.org/wiki/Windows-1252) */

        public static SpriteFont GenerateFromImage(GraphicsDevice graphicsDevice, string path, Point2 charSize) =>
            GenerateFromImage(graphicsDevice, path, charSize, defaultFirstCharacter, defaultSpaceCharaWidth, defaultCharaSpacing, SpriteFontHighlightType.None, 0);
        public static SpriteFont GenerateFromImage(GraphicsDevice graphicsDevice, string path, Point2 charSize, int firstChar, float spaceWidth, float charSpacing) =>
            GenerateFromImage(graphicsDevice, path, charSize, firstChar, spaceWidth, charSpacing, SpriteFontHighlightType.None, 0);
        public static SpriteFont GenerateFromImage(GraphicsDevice graphicsDevice, string path, Point2 charSize, SpriteFontHighlightType highlightType, Color highlightColor) =>
            GenerateFromImage(graphicsDevice, path, charSize, defaultFirstCharacter, defaultSpaceCharaWidth, defaultCharaSpacing, highlightType, highlightColor);

        public static SpriteFont GenerateFromImage(GraphicsDevice graphicsDevice, string path, Point2 charSize, int firstChar, float spaceWidth, float charSpacing, SpriteFontHighlightType highlightType, Color highlightColor)
        {
            var highlightOffsets = highlightType switch
            {
                SpriteFontHighlightType.Outline => fontHighlightOffsetsOutline,
                SpriteFontHighlightType.DropShadowThick => fontHighlightOffsetsDropShadowThick,
                SpriteFontHighlightType.DropShadowThin => fontHighlightOffsetsDropShadowThin,
                _ => [],
            };

            var fontImage = new Image(path);
            var characterData = GenerateCharaDictionary(fontImage, charSize, firstChar, spaceWidth, charSpacing);
            if (highlightOffsets.Length > 0) PerformFontHighlighting(fontImage, highlightOffsets, highlightColor);

            var spriteFont = new SpriteFont(graphicsDevice) { LineGap = charSize.Y };
            AddCharactersToFont(graphicsDevice, spriteFont, fontImage, Path.GetFileNameWithoutExtension(path), characterData);

            return spriteFont;
        }

        private static Dictionary<char, (float, Rect)> GenerateCharaDictionary(Image fontImage, Point2 charSize, int firstChar, float spaceWidth, float charSpacing)
        {
            var characterData = new Dictionary<char, (float, Rect)>();
            for (var y = 0; y < fontImage.Height; y += charSize.Y)
            {
                for (var x = 0; x < fontImage.Width; x += charSize.X)
                {
                    var character = (char)(firstChar + ((y / charSize.Y * fontImage.Width) + x) / charSize.X);
                    var currentCharacterWidth = character == ' ' ? spaceWidth : CalculateVisibleCharacterWidth(fontImage, x, y, charSize, charSpacing);
                    characterData.Add(character, (currentCharacterWidth, new(x, y, charSize.X, charSize.Y)));
                }
            }
            return characterData;
        }

        private static float CalculateVisibleCharacterWidth(Image fontImage, int x, int y, Point2 charSize, float charSpacing)
        {
            var currentCharacterWidth = 0f;
            for (var pixelY = 0; pixelY < charSize.Y; pixelY++)
            {
                for (var pixelX = 0; pixelX < charSize.X; pixelX++)
                {
                    var pixelColor = fontImage[x + pixelX, y + pixelY];
                    if (pixelColor.A != 0) currentCharacterWidth = Math.Max(currentCharacterWidth, pixelX + charSpacing);
                }
            }
            return currentCharacterWidth;
        }

        private static void PerformFontHighlighting(Image fontImage, Point2[] highlightOffsets, Color highlightColor)
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

        private static void AddCharactersToFont(GraphicsDevice graphicsDevice, SpriteFont spriteFont, Image fontImage, string fontName, Dictionary<char, (float, Rect)> characterData)
        {
            var texture = new Texture(graphicsDevice, fontImage, fontName);
            foreach (var (character, (charaWidth, charaRect)) in characterData)
                spriteFont.AddCharacter(character, charaWidth, Vector2.Zero, new(texture, charaRect));
        }
    }
}
