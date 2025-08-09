using Foster.Framework;
using GameTest1.Game.Sprites;
using System.Numerics;

namespace GameTest1.Utilities
{
    public class Shadow(Manager manager)
    {
        public bool Enabled = false;
        public Color Color = new(0f, 0f, 0f, 0.5f);
        public Vector2 Scale = Vector2.One / 2f;
        public Vector2 Offset = Vector2.Zero;

        public void Render(Sprite sprite, Frame frame, Vector2 position, float rotation = 0f)
        {
            if (frame.Texture == null || frame.Texture is not Subtexture texture) return;

            manager.Batcher.PushMatrix(
                Matrix3x2.CreateTranslation(-frame.Size / 2f) *
                Matrix3x2.CreateScale(Scale) *
                Matrix3x2.CreateTranslation(frame.Size / 2f) *
                Matrix3x2.CreateTranslation(position + Offset));
            manager.Batcher.Image(texture, Vector2.Zero, sprite.Origin, Vector2.One, rotation, Color);
            manager.Batcher.PopMatrix();
        }
    }
}
