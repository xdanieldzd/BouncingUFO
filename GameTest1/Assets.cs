using Foster.Framework;
using GameTest1.Game.Levels;
using GameTest1.Game.Sprites;
using System.Numerics;
using System.Text.Json;

namespace GameTest1
{
    public class Assets
    {
        public const string AssetsFolderName = "Assets";
        public const string FontsFolderName = "Fonts";
        public const string TilesetFolderName = "Tilesets";
        public const string MapFolderName = "Maps";
        public const string SpriteFolderName = "Sprites";

        //

        public readonly static JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

        public SpriteFont Font { get; private set; }
        public SpriteFont PixelFont { get; private set; }
        public Dictionary<string, Tileset> Tilesets { get; private set; } = [];
        public Dictionary<string, Map> Maps { get; private set; } = [];
        public Dictionary<string, Sprite> Sprites { get; private set; } = [];

        //

        public Assets(GraphicsDevice graphicsDevice)
        {
            Font = new(graphicsDevice, Path.Join(AssetsFolderName, FontsFolderName, "monogram-extended.ttf"), 16f);
            PixelFont = GenerateFontFromImage(graphicsDevice, Path.Join(AssetsFolderName, FontsFolderName, "PixelFont.png"));

            foreach (var tilesetFile in Directory.EnumerateFiles(Path.Join(AssetsFolderName, TilesetFolderName), "*.json", SearchOption.AllDirectories))
            {
                var tileset = JsonSerializer.Deserialize<Tileset>(File.ReadAllText(tilesetFile), SerializerOptions);
                if (tileset == null) continue;

                tileset.GenerateSubtextures(graphicsDevice);
                Tilesets.Add(Path.GetFileNameWithoutExtension(tilesetFile), tileset);
            }

            foreach (var mapFile in Directory.EnumerateFiles(Path.Join(AssetsFolderName, MapFolderName), "*.json", SearchOption.AllDirectories))
            {
                var map = JsonSerializer.Deserialize<Map>(File.ReadAllText(mapFile), SerializerOptions);
                if (map == null) continue;

                Maps.Add(Path.GetFileNameWithoutExtension(mapFile), map);
            }
        }

        private static SpriteFont GenerateFontFromImage(GraphicsDevice graphicsDevice, string path, float spaceWidth = 3f)
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
                            if (pixelColor.A != 0) currentCharacterWidth = Math.Max(currentCharacterWidth, pixelX);
                        }
                    }

                    if (character == ' ') currentCharacterWidth = spaceWidth;
                    characterData.Add(character, (currentCharacterWidth, new(x, y, characterSize.X, characterSize.Y)));
                }
            }

            var texture = new Texture(graphicsDevice, fontImage, Path.GetFileNameWithoutExtension(path));
            foreach (var (character, (charaWidth, charaRect)) in characterData)
                spriteFont.AddCharacter(character, charaWidth, Vector2.Zero, new(texture, charaRect));

            return spriteFont;
        }
    }
}
