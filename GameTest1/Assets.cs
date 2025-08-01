using Foster.Framework;

namespace GameTest1
{
    public class Assets
    {
        private const string assetsFolderName = "Assets";

        public SpriteFont Font { get; private set; }
        public Dictionary<string, Tileset> Tilesets { get; private set; } = [];
        public Dictionary<string, Stage> Stages { get; private set; } = [];

        public Assets(GraphicsDevice graphicsDevice)
        {
            Font = new(graphicsDevice, Path.Join(assetsFolderName, "Fonts", "monogram-extended.ttf"), 16);

            var tilesetsPath = Path.Join(assetsFolderName, "Tilesets");
            foreach (var file in Directory.EnumerateFiles(tilesetsPath, "*.png", SearchOption.AllDirectories))
                LoadTileset(graphicsDevice, tilesetsPath, file);

            var stagesPath = Path.Join(assetsFolderName, "Stages");
            foreach (var file in Directory.EnumerateFiles(stagesPath, "*.txt", SearchOption.AllDirectories))
                LoadStage(stagesPath, file);
        }

        private void LoadTileset(GraphicsDevice graphicsDevice, string tilesetsPath, string file)
        {
            var name = Path.ChangeExtension(Path.GetRelativePath(tilesetsPath, file), null);
            var texture = new Texture(graphicsDevice, new(file), name);

            var columns = texture.Width / Game.TileSize;
            var rows = texture.Height / Game.TileSize;

            var tileset = new Tileset(name, columns, rows);

            for (var x = 0; x < columns; x++)
                for (var y = 0; y < rows; y++)
                    tileset.Tiles[y * columns + x] = new(texture, new Rect(x, y, 1, 1) * Game.TileSize);

            Tilesets.Add(name, tileset);
        }

        private void LoadStage(string stagesPath, string file)
        {
            var name = Path.ChangeExtension(Path.GetRelativePath(stagesPath, file), null);

            var (title, width, height, tileset) = (string.Empty, -1, -1, string.Empty);

            var (currentLayer, row) = (-1, -1);
            var layers = new List<int[,]>();

            var lines = File.ReadAllLines(file);
            foreach (var line in lines)
            {
                if (line.StartsWith('#'))
                {
                    /* Comment */
                    continue;
                }
                else if (line.StartsWith('!'))
                {
                    /* Header */
                    var header = line[1..].Split(';', StringSplitOptions.RemoveEmptyEntries);
                    if (header.Length != 4) break;

                    title = header[0];
                    width = int.TryParse(header[1], out width) ? width : -1;
                    height = int.TryParse(header[2], out height) ? height : -1;
                    tileset = header[3];

                    if (width == -1 || height == -1) break;
                }
                else if (line.StartsWith('-'))
                {
                    /* Begin layer */
                    currentLayer++;
                    row = 0;
                    layers.Add(new int[width, height]);
                }
                else
                {
                    /* Tile row */
                    if (line.Length != width * 2) break;

                    for (int i = 0, column = 0; i < line.Length; i += 2, column++)
                    {
                        if (int.TryParse(line.AsSpan(i, 2), out int tile))
                            layers[currentLayer][column, row] = tile;
                    }
                    row++;
                }
            }

            if (width != -1 && height != -1)
                Stages.Add(name, new Stage(title, width, height, tileset, [.. layers]));
        }
    }
}
