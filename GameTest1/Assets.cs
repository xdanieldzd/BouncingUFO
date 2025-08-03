using Foster.Framework;
using System.Text.Json;

namespace GameTest1
{
    public class Assets
    {
        private const string assetsFolderName = "Assets";

        public readonly static JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

        public SpriteFont Font { get; private set; }

        //

        public Assets(GraphicsDevice graphicsDevice)
        {
            Font = new(graphicsDevice, Path.Join(assetsFolderName, "Fonts", "monogram-extended.ttf"), 16);

            //
        }
    }
}
