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
        public const string DataFolderName = "Data";
        public const string ProgressionFolder = DataFolderName + "\\Progression";

        public const string TilesetFolderName = "Tilesets";
        public const string MapFolderName = "Maps";
        public const string SpriteFolderName = "Sprites";
        public const string UIFolderName = "UI";
        public const string DialogTextFolderName = "DialogText";

        public readonly static JsonSerializerOptions SerializerOptions = new() { WriteIndented = true, IncludeFields = true };

        public readonly SpriteFont SmallFont;
        public readonly SpriteFont LargeFont;
        public readonly SpriteFont FutureFont;

        public readonly Dictionary<string, Progression> Progression = [];

        public readonly Dictionary<string, Tileset> Tilesets = [];
        public readonly Dictionary<string, Map> Maps = [];
        public readonly Dictionary<string, Sprite> Sprites = [];
        public readonly Dictionary<string, GraphicsSheet> UI = [];
        public readonly Dictionary<string, Dictionary<string, DialogText>> DialogText = [];

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

            LoadAssets(ProgressionFolder, ref Progression);

            LoadAssets(TilesetFolderName, ref Tilesets, (obj) => obj.CreateTextures(graphicsDevice));
            LoadAssets(MapFolderName, ref Maps);
            LoadAssets(SpriteFolderName, ref Sprites, (obj) => obj.CreateTextures(graphicsDevice));
            LoadAssets(UIFolderName, ref UI, (obj) => obj.CreateTextures(graphicsDevice));
            LoadAssets(DialogTextFolderName, ref DialogText);
        }

        private static void LoadAssets<T>(string folderName, ref Dictionary<string, T> assetDictionary, Action<T>? afterLoadAction = null)
        {
            var path = Path.Join(AssetsFolderName, folderName);
            if (!Directory.Exists(path)) return;

            foreach (var file in Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories))
            {
                var instance = JsonSerializer.Deserialize<T>(File.ReadAllText(file), SerializerOptions);
                if (instance == null) continue;

                afterLoadAction?.Invoke(instance);
                assetDictionary.Add(Path.ChangeExtension(file.Replace(path, null).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), null), instance);
            }
        }
    }
}
