using Foster.Framework;
using System.Numerics;

namespace BouncingUFO.Utilities
{
    public enum SpriteFontHighlightType { None, Outline, DropShadowThin, DropShadowThick }

    [Flags]
    public enum SpriteFontSetting
    {
        None = 0,
        Gradient = (1 << 0),
        FixedWidth = (1 << 1)
    }

    public static class SpriteFontHelper
    {
        private const int defaultFirstCharacter = 0x00;
        private const int defaultSpaceCharaWidth = 3;
        private const int defaultCharaSpacing = 0;

        private readonly static (int, Point2[]) fontHighlightOffsetsOutline =
        (
            0,
            [
                /* ooo. */  (-1, -1), ( 0, -1), ( 1, -1),
                /* oOo. */  (-1,  0),           ( 1,  0),
                /* ooo. */  (-1,  1), ( 0,  1), ( 1,  1)
                /* .... */
            ]
        );

        private readonly static (int, Point2[]) fontHighlightOffsetsDropShadowThin =
        (
            1,
            [
                /* .... */
                /* .O.. */
                /* ..o. */                      ( 1,  1)
                /* .... */
            ]
        );

        private readonly static (int, Point2[]) fontHighlightOffsetsDropShadowThick =
        (
            0,
            [
                /* .... */
                /* .Oo. */                      ( 1,  0),
                /* .ooo */            ( 0,  1), ( 1,  1), ( 2,  1),
                /* ..oo */                      ( 1,  2), ( 2,  2)
            ]
        );

        /* Developed using font image with 16 lines of 16 characters in Windows-1252 encoding, total 256 characters, from 0x00 (space character) to 0xFF (ÿ, lowercase y with diaeresis) */

        public static SpriteFont GenerateFromImage(GraphicsDevice graphicsDevice, string name, byte[] data, Point2 charaSize, SpriteFontSetting settings, SpriteFontHighlightType highlightType = SpriteFontHighlightType.None, Color highlightColor = default, int firstChara = defaultFirstCharacter, int charaSpacing = defaultCharaSpacing, int spaceWidth = defaultSpaceCharaWidth)
        {
            var (charaGap, highlightOffsets) = highlightType switch
            {
                SpriteFontHighlightType.Outline => fontHighlightOffsetsOutline,
                SpriteFontHighlightType.DropShadowThin => fontHighlightOffsetsDropShadowThin,
                SpriteFontHighlightType.DropShadowThick => fontHighlightOffsetsDropShadowThick,
                _ => (2, []),
            };

            var fontImage = new Image(data);

            if (settings.Has(SpriteFontSetting.Gradient))
                ApplyGradient(fontImage, charaSize);

            if (highlightOffsets.Length > 0)
            {
                ResizeImageForHighlighting(fontImage, charaSize, highlightOffsets, out fontImage, out charaSize);
                PerformFontHighlighting(fontImage, highlightOffsets, highlightColor);
            }

            var spriteFont = new SpriteFont(graphicsDevice) { Name = name, LineGap = charaSize.Y };
            var characterData = GenerateCharaDictionary(fontImage, charaSize, firstChara, spaceWidth, settings.Has(SpriteFontSetting.FixedWidth));
            AddCharactersToFont(graphicsDevice, spriteFont, fontImage, charaGap + charaSpacing, characterData);

            return spriteFont;
        }

        private static void ApplyGradient(Image fontImage, Point2 charSize)
        {
            var shades = Enumerable.Range(0, charSize.Y).Select(i => Calc.ClampedMap(i, 0f, charSize.Y, 1f, 0.5f)).Select(i => new Color(i, i, i, 1f)).ToArray();
            for (var y = 0; y < fontImage.Height; y++)
                for (var x = 0; x < fontImage.Width; x++)
                    if (fontImage[x, y].A != 0)
                        fontImage[x, y] = shades[y % charSize.Y];
        }

        private static void ResizeImageForHighlighting(Image fontImage, Point2 charSize, Point2[] highlightOffsets, out Image destFontImage, out Point2 newCharSize)
        {
            var highlightOffsetsMinimum = new Point2(highlightOffsets.Min(x => x.X), highlightOffsets.Min(x => x.Y));
            var highlightOffsetsPositive = highlightOffsets.Select(i => new Point2(i.X + (highlightOffsetsMinimum.X < 0 ? -highlightOffsetsMinimum.X : 0), i.Y + (highlightOffsetsMinimum.Y < 0 ? -highlightOffsetsMinimum.Y : 0))).ToArray();

            newCharSize = new Point2(charSize.X + highlightOffsetsPositive.Max(i => i.X), charSize.Y + highlightOffsetsPositive.Max(i => i.Y));
            destFontImage = new Image(newCharSize.X * fontImage.Width / charSize.X, newCharSize.Y * fontImage.Height / charSize.Y);

            for (int sy = 0, dy = Math.Abs(Math.Min(highlightOffsetsMinimum.Y, 0)); sy < fontImage.Height && dy < destFontImage.Height; sy += charSize.Y, dy += newCharSize.Y)
                for (int sx = 0, dx = Math.Abs(Math.Min(highlightOffsetsMinimum.X, 0)); sx < fontImage.Width && dx < destFontImage.Width; sx += charSize.X, dx += newCharSize.X)
                    destFontImage.CopyPixels(fontImage, new RectInt(sx, sy, charSize.X, charSize.Y), new(dx, dy));
        }

        private static Dictionary<char, (int, Rect)> GenerateCharaDictionary(Image fontImage, Point2 charSize, int firstChar, int spaceWidth, bool fixedWidth)
        {
            var characterData = new Dictionary<char, (int, Rect)>();
            for (var y = 0; y < fontImage.Height; y += charSize.Y)
            {
                for (var x = 0; x < fontImage.Width; x += charSize.X)
                {
                    var character = (char)(firstChar + ((y / charSize.Y * fontImage.Width) + x) / charSize.X);
                    var currentCharacterWidth = fixedWidth ? charSize.X : (character == ' ' ? spaceWidth : CalculateVisibleCharacterWidth(fontImage, x, y, charSize));
                    characterData.Add(character, (currentCharacterWidth, new(x, y, charSize.X, charSize.Y)));
                }
            }
            return characterData;
        }

        private static int CalculateVisibleCharacterWidth(Image fontImage, int x, int y, Point2 charSize)
        {
            var currentCharacterWidth = 0;
            for (var pixelY = 0; pixelY < charSize.Y; pixelY++)
            {
                for (var pixelX = 0; pixelX < charSize.X; pixelX++)
                {
                    var pixelColor = fontImage[x + pixelX, y + pixelY];
                    if (pixelColor.A != 0) currentCharacterWidth = Math.Max(currentCharacterWidth, pixelX);
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

        private static void AddCharactersToFont(GraphicsDevice graphicsDevice, SpriteFont spriteFont, Image fontImage, int charaGap, Dictionary<char, (int, Rect)> characterData)
        {
            var texture = new Texture(graphicsDevice, fontImage, spriteFont.Name);
            foreach (var (character, (charaWidth, charaRect)) in characterData)
                spriteFont.AddCharacter(character, charaWidth + charaGap, Vector2.Zero, new(texture, charaRect));
        }
    }
}
