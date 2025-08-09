using Foster.Framework;
using System.Text.Json.Serialization;

namespace GameTest1.Game.Levels
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [Flags]
    public enum CellFlag
    {
        Empty = 0,
        Ground = 1 << 0,
        Wall = 1 << 1,
        Damaging = 1 << 2,
        Healing = 1 << 3
    }

    public record Tileset
    {
        public static readonly Point2[] ValidCellSizes = [new(8, 8), new(16, 16), new(32, 32), new(64, 64)];

        [JsonInclude]
        public Point2 CellSize = ValidCellSizes[1];
        [JsonInclude]
        public string TilesheetFile = string.Empty;
        [JsonInclude]
        public CellFlag[] CellFlags = [];

        [JsonIgnore]
        public Texture? TilesheetTexture;
        [JsonIgnore]
        public Point2 TilesheetSizeInCells;
        [JsonIgnore]
        public Subtexture[]? CellTextures;

        public void CreateTextures(GraphicsDevice graphicsDevice)
        {
            if (!string.IsNullOrWhiteSpace(TilesheetFile))
            {
                TilesheetTexture = new(graphicsDevice, new(TilesheetFile), $"Tileset {Path.GetFileNameWithoutExtension(TilesheetFile)}");
                TilesheetSizeInCells = new(TilesheetTexture.Width / CellSize.X, TilesheetTexture.Height / CellSize.Y);

                CellTextures = new Subtexture[TilesheetSizeInCells.Y * TilesheetSizeInCells.X];
                for (var x = 0; x < TilesheetSizeInCells.X; x++)
                    for (var y = 0; y < TilesheetSizeInCells.Y; y++)
                        CellTextures[y * TilesheetSizeInCells.X + x] = new(TilesheetTexture, new Rect(x, y, 1, 1) * CellSize);
            }
        }
    }
}
