using Foster.Framework;

namespace GameTest1
{
    public class Tileset(string name, int columns, int rows)
    {
        public string Name { get; private set; } = name;
        public int Columns { get; private set; } = columns;
        public int Rows { get; private set; } = rows;
        public Subtexture[] Tiles { get; private set; } = new Subtexture[columns * rows];
    }
}
