using Foster.Framework;
using GameTest1.Game.Levels;
using GameTest1.Game.Sprites;
using GameTest1.Game.UI;
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
        public const string UIFolderName = "UI";
        public const string DialogTextFolderName = "DialogText";

        public readonly static JsonSerializerOptions SerializerOptions = new() { WriteIndented = true, IncludeFields = true };

        public SpriteFont SmallFont { get; private set; }
        public SpriteFont LargeFont { get; private set; }
        public SpriteFont FutureFont { get; private set; }
        public Dictionary<string, Tileset> Tilesets { get; private set; } = [];
        public Dictionary<string, Map> Maps { get; private set; } = [];
        public Dictionary<string, Sprite> Sprites { get; private set; } = [];
        public Dictionary<string, GraphicsSheet> UI { get; private set; } = [];
        public Dictionary<string, Dictionary<string, DialogText>> DialogText { get; private set; } = [];

        public Assets(GraphicsDevice graphicsDevice)
        {
            SmallFont = SpriteFontHelper.GenerateFromImage(
                graphicsDevice,
                "SmallFont",
                Path.Join(AssetsFolderName, FontsFolderName, "SmallFont.png"),
                new(8, 8),
                SpriteFontSetting.None,
                SpriteFontHighlightType.Outline,
                Color.Black);

            LargeFont = SpriteFontHelper.GenerateFromImage(
                graphicsDevice,
                "LargeFont",
                Path.Join(AssetsFolderName, FontsFolderName, "LargeFont.png"),
                new(16, 16),
                SpriteFontSetting.None,
                SpriteFontHighlightType.DropShadowThin,
                Color.Black,
                charaSpacing: 1);

            FutureFont = SpriteFontHelper.GenerateFromImage(
                graphicsDevice,
                "FutureFont",
                Path.Join(AssetsFolderName, FontsFolderName, "FutureFont.png"),
                new(16, 16),
                SpriteFontSetting.Gradient | SpriteFontSetting.FixedWidth,
                SpriteFontHighlightType.DropShadowThick,
                Color.Black,
                firstChara: 0x20,
                spaceWidth: 16);

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

            foreach (var uiElementsFiles in Directory.EnumerateFiles(Path.Join(AssetsFolderName, UIFolderName), "*.json", SearchOption.AllDirectories))
            {
                var uiElements = JsonSerializer.Deserialize<GraphicsSheet>(File.ReadAllText(uiElementsFiles), SerializerOptions);
                if (uiElements == null) continue;

                uiElements.CreateTextures(graphicsDevice);
                UI.Add(Path.GetFileNameWithoutExtension(uiElementsFiles), uiElements);
            }

            foreach (var dialogTextFiles in Directory.EnumerateFiles(Path.Join(AssetsFolderName, DialogTextFolderName), "*.json", SearchOption.AllDirectories))
            {
                var dialogText = JsonSerializer.Deserialize<Dictionary<string, DialogText>>(File.ReadAllText(dialogTextFiles), SerializerOptions);
                if (dialogText == null) continue;

                DialogText.Add(Path.GetFileNameWithoutExtension(dialogTextFiles), dialogText);
            }
        }
    }
}
