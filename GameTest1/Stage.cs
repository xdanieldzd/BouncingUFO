namespace GameTest1
{
    public class Stage(string title, int width, int height, string tileset, int[][,] layers)
    {
        public string Title { get; private set; } = title;
        public int Width { get; private set; } = width;
        public int Height { get; private set; } = height;
        public string Tileset { get; private set; } = tileset;
        public int[][,] Layers { get; private set; } = layers;
    }
}
