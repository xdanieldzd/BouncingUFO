using BouncingUFO.Game.Levels;
using BouncingUFO.Game.Sprites;
using BouncingUFO.Game.UI;
using BouncingUFO.Utilities;
using Foster.Framework;

namespace BouncingUFO
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

        private readonly SpriteFont dummyFont;

        private SpriteFont? smallFont, largeFont, futureFont;
        public SpriteFont SmallFont => smallFont ?? dummyFont;
        public SpriteFont LargeFont => largeFont ?? dummyFont;
        public SpriteFont FutureFont => futureFont ?? dummyFont;

        public Dictionary<string, Progression> Progression = [];

        public Dictionary<string, Tileset> Tilesets = [];
        public Dictionary<string, Map> Maps = [];
        public Dictionary<string, Sprite> Sprites = [];
        public Dictionary<string, GraphicsSheet> UI = [];
        public Dictionary<string, Dictionary<string, DialogText>> DialogText = [];

        public Assets(Manager manager)
        {
            dummyFont = new(manager.GraphicsDevice);

            manager.FileSystem.OpenTitleStorage((storage) =>
            {
                smallFont = SpriteFontHelper.GenerateFromImage(
                   manager.GraphicsDevice,
                   "SmallFont",
                   storage.ReadAllBytes(Path.Join(AssetsFolderName, FontsFolderName, "SmallFont.png")),
                   new(8, 8),
                   SpriteFontSetting.None,
                   SpriteFontHighlightType.Outline,
                   Color.Black);

                largeFont = SpriteFontHelper.GenerateFromImage(
                    manager.GraphicsDevice,
                    "LargeFont",
                    storage.ReadAllBytes(Path.Join(AssetsFolderName, FontsFolderName, "LargeFont.png")),
                    new(16, 16),
                    SpriteFontSetting.None,
                    SpriteFontHighlightType.DropShadowThin,
                    Color.Black,
                    charaSpacing: 1);

                futureFont = SpriteFontHelper.GenerateFromImage(
                    manager.GraphicsDevice,
                    "FutureFont",
                    storage.ReadAllBytes(Path.Join(AssetsFolderName, FontsFolderName, "FutureFont.png")),
                    new(16, 16),
                    SpriteFontSetting.Gradient | SpriteFontSetting.FixedWidth,
                    SpriteFontHighlightType.DropShadowThick,
                    Color.Black,
                    firstChara: 0x20,
                    spaceWidth: 16);

                LoadAssets(storage, ProgressionFolder, ref Progression);

                LoadAssets(storage, TilesetFolderName, ref Tilesets, (obj) => obj.CreateTextures(manager.GraphicsDevice));
                LoadAssets(storage, MapFolderName, ref Maps);
                LoadAssets(storage, SpriteFolderName, ref Sprites, (obj) => obj.CreateTextures(manager.GraphicsDevice));
                LoadAssets(storage, UIFolderName, ref UI, (obj) => obj.CreateTextures(manager.GraphicsDevice));
                LoadAssets(storage, DialogTextFolderName, ref DialogText);
            });
        }

        private static void LoadAssets<T>(Storage storage, string folderName, ref Dictionary<string, T> assetDictionary, Action<T>? afterLoadAction = null) where T : new()
        {
            var path = Path.Join(AssetsFolderName, folderName).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!storage.DirectoryExists(path)) return;

            foreach (var file in storage.EnumerateDirectory(path, "*.json", SearchOption.AllDirectories))
            {
                var instance = storage.DeserializeFromStorage<T>(file);
                if (instance == null) continue;

                afterLoadAction?.Invoke(instance);
                assetDictionary.Add(Path.ChangeExtension(file.Replace(path, null).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), null), instance);
            }
        }
    }
}
