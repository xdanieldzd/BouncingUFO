using Foster.Framework;
using GameTest1.Game.Levels;
using GameTest1.Game.Sprites;
using GameTest1.Utilities;
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

        public readonly static JsonSerializerOptions SerializerOptions = new() { WriteIndented = true, IncludeFields = true };

        public SpriteFont Font { get; private set; }
        public SpriteFont PixelFont { get; private set; }
        public Dictionary<string, Tileset> Tilesets { get; private set; } = [];
        public Dictionary<string, Map> Maps { get; private set; } = [];
        public Dictionary<string, Sprite> Sprites { get; private set; } = [];

        public Assets(GraphicsDevice graphicsDevice)
        {
            Font = new(graphicsDevice, Path.Join(AssetsFolderName, FontsFolderName, "monogram-extended.ttf"), 16f);
            PixelFont = SpriteFontHelper.GenerateFromImage(
                graphicsDevice,
                Path.Join(AssetsFolderName, FontsFolderName, "PixelFont.png"),
                new(10, 10),
                SpriteFontHighlightType.Outline,
                Color.Black);

            foreach (var tilesetFile in Directory.EnumerateFiles(Path.Join(AssetsFolderName, TilesetFolderName), "*.json", SearchOption.AllDirectories))
            {
                var tileset = JsonSerializer.Deserialize<Tileset>(File.ReadAllText(tilesetFile), SerializerOptions);
                if (tileset == null) continue;

                tileset.CreateTextures(graphicsDevice);
                Tilesets.Add(Path.GetFileNameWithoutExtension(tilesetFile), tileset);
            }

            foreach (var mapFile in Directory.EnumerateFiles(Path.Join(AssetsFolderName, MapFolderName), "*.json", SearchOption.AllDirectories))
            {
                var map = JsonSerializer.Deserialize<Map>(File.ReadAllText(mapFile), SerializerOptions);
                if (map == null) continue;

                Maps.Add(Path.GetFileNameWithoutExtension(mapFile), map);
            }

            foreach (var spriteFile in Directory.EnumerateFiles(Path.Join(AssetsFolderName, SpriteFolderName), "*.json", SearchOption.AllDirectories))
            {
                var sprite = JsonSerializer.Deserialize<Sprite>(File.ReadAllText(spriteFile), SerializerOptions);
                if (sprite == null) continue;

                sprite.CreateTextures(graphicsDevice);
                Sprites.Add(Path.GetFileNameWithoutExtension(spriteFile), sprite);
            }
        }
    }
}
