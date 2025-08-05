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
            PixelFont = new(graphicsDevice);

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



            // TODO: prettify and make reusable and stuffs!

            var charsDict = new Dictionary<char, (float, Rect)>();
            var fontImg = new Image(Path.Join(AssetsFolderName, FontsFolderName, "PixelFont.png"));
            for (var y = 0; y < fontImg.Height; y += 8)
            {
                for (var x = 0; x < fontImg.Width; x += 8)
                {
                    var ch = (char)(' ' + ((y / 8) * fontImg.Width + x) / 8);
                    var width = 0f;
                    for (var py = 0; py < 8; py++)
                    {
                        for (var px = 0; px < 8; px++)
                        {
                            var pixel = fontImg[x + px, y + py];
                            if (pixel.A != 0) width = Math.Max(width, px);
                        }
                    }
                    if (ch == ' ') width = 3f;
                    charsDict.Add(ch, (width, new(x, y, 8f, 8f)));
                }
            }
            var texture = new Texture(graphicsDevice, fontImg, "PixelFont");
            foreach (var (ch, (width, rect)) in charsDict)
                PixelFont.AddCharacter(ch, width, Vector2.Zero, new(texture, rect));
            PixelFont.LineGap = 8f;
        }
    }
}
