using Foster.Framework;
using System.Numerics;
using System.Text.Json.Serialization;

namespace GameTest1.Game.Sprites
{
    /* https://github.com/FosterFramework/Samples/blob/5b97ca5329e61768f85a45d655da5df7f882519d/TinyLink/Source/Assets/Sprite.cs */

    public record Sprite
    {
        [JsonInclude]
        public string Name = string.Empty;
        [JsonInclude]
        public string SpritesheetFile = string.Empty;
        [JsonInclude]
        public Vector2 Origin = Vector2.Zero;
        [JsonInclude]
        public List<Frame> Frames = [];
        [JsonInclude]
        public List<Animation> Animations = [];

        public Texture? SpritesheetTexture;

        public void CreateTextures(GraphicsDevice graphicsDevice)
        {
            if (!string.IsNullOrWhiteSpace(SpritesheetFile))
            {
                SpritesheetTexture = new(graphicsDevice, new(SpritesheetFile), $"Sprite {Name}");
                foreach (var frame in Frames)
                    frame.CreateTexture(SpritesheetTexture);
            }
        }

        public void CalculateAnimationDuration(Animation animation)
        {
            animation.Duration = 0f;
            for (var i = animation.FirstFrame; i < animation.FirstFrame + animation.FrameCount; i++)
                animation.Duration += Frames[i].Duration;
        }

        public Frame GetFrameAt(Animation animation, float time, bool loop)
        {
            if (animation.Duration == 0f) CalculateAnimationDuration(animation);

            if (time >= animation.Duration && !loop)
                return Frames[animation.FirstFrame + animation.FrameCount - 1];

            time %= animation.Duration;
            for (var i = animation.FirstFrame; i < animation.FirstFrame + animation.FrameCount; i++)
            {
                time -= Frames[i].Duration;
                if (time <= 0f) return Frames[i];
            }

            return animation.FirstFrame < Frames.Count ? Frames[animation.FirstFrame] : new();
        }
    }
}
