using BouncingUFO.Game;
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
        public const string TilesetsFolderName = "Tilesets";
        public const string MapsFolderName = "Maps";
        public const string SpritesFolderName = "Sprites";
        public const string GraphicsSheetsFolderName = "GraphicsSheets";
        public const string DialogCollectionsFolderName = "DialogCollections";
        public const string LevelCollectionsFolderName = "LevelCollections";

        public Dictionary<string, SpriteFont> Fonts = [];
        public Dictionary<string, Tileset> Tilesets = [];
        public Dictionary<string, Map> Maps = [];
        public Dictionary<string, Sprite> Sprites = [];
        public Dictionary<string, GraphicsSheet> GraphicsSheets = [];
        public Dictionary<string, DialogCollection> DialogCollections = [];
        public Dictionary<string, LevelCollection> LevelCollections = [];

        public Assets(Manager manager)
        {
            manager.FileSystem.OpenTitleStorage((storage) =>
            {
                Fonts.Add("SmallFont", SpriteFontHelper.GenerateFromImage(
                    manager.GraphicsDevice,
                    "SmallFont",
                    storage.ReadAllBytes(Path.Join(AssetsFolderName, FontsFolderName, "SmallFont.png")),
                    new(8, 8),
                    SpriteFontSetting.None,
                    SpriteFontHighlightType.Outline,
                    Color.Black));

                Fonts.Add("LargeFont", SpriteFontHelper.GenerateFromImage(
                    manager.GraphicsDevice,
                    "LargeFont",
                    storage.ReadAllBytes(Path.Join(AssetsFolderName, FontsFolderName, "LargeFont.png")),
                    new(16, 16),
                    SpriteFontSetting.None,
                    SpriteFontHighlightType.DropShadowThin,
                    Color.Black,
                    charaSpacing: 1));

                Fonts.Add("FutureFont", SpriteFontHelper.GenerateFromImage(
                    manager.GraphicsDevice,
                    "FutureFont",
                    storage.ReadAllBytes(Path.Join(AssetsFolderName, FontsFolderName, "FutureFont.png")),
                    new(16, 16),
                    SpriteFontSetting.Gradient | SpriteFontSetting.FixedWidth,
                    SpriteFontHighlightType.DropShadowThick,
                    Color.Black,
                    firstChara: 0x20,
                    spaceWidth: 16));

                LoadAssets(storage, TilesetsFolderName, ref Tilesets, (obj) => obj.CreateTextures(manager.GraphicsDevice));
                LoadAssets(storage, MapsFolderName, ref Maps);
                LoadAssets(storage, SpritesFolderName, ref Sprites, (obj) => obj.CreateTextures(manager.GraphicsDevice));
                LoadAssets(storage, GraphicsSheetsFolderName, ref GraphicsSheets, (obj) => obj.CreateTextures(manager.GraphicsDevice));
                LoadAssets(storage, DialogCollectionsFolderName, ref DialogCollections);
                LoadAssets(storage, LevelCollectionsFolderName, ref LevelCollections);
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
