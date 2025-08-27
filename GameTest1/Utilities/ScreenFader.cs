using Foster.Framework;
using System.Numerics;

namespace GameTest1.Utilities
{
    public enum ScreenFadeType { FadeIn, FadeOut }

    public class ScreenFader(Manager manager)
    {
        private Color color = Color.Black;
        private float progress = 0f;

        public ScreenFadeType FadeType { get; set; } = ScreenFadeType.FadeIn;
        public float Duration { get; set; } = 1f;
        public Color Color { get => color; set => PreviousColor = color = value; }
        public float Progress => progress;
        public bool IsRunning { get; set; } = false;

        public static Color PreviousColor { get; private set; }

        public void Begin(ScreenFadeType type, float duration, Color color)
        {
            FadeType = type;
            Duration = duration;
            Color = color;
            Reset();
        }

        public void Reset()
        {
            IsRunning = true;

            progress = 0f;
            color.A = (byte)(FadeType == ScreenFadeType.FadeIn ? 255 : 0);
        }

        public void Cancel()
        {
            IsRunning = false;

            progress = Duration;
            color.A = (byte)(FadeType == ScreenFadeType.FadeIn ? 0 : 255);
        }

        public bool Update()
        {
            if (!IsRunning) return false;

            progress = Calc.Approach(progress, Duration, manager.Time.Delta);
            if (FadeType == ScreenFadeType.FadeIn)
                color.A = (byte)(255f - (progress / Duration * 255f));
            else
                color.A = (byte)(progress / Duration * 255f);

            IsRunning = progress < Duration;
            return !IsRunning;
        }

        public void Render()
        {
            if (color.A != 0)
            {
                manager.Batcher.PushBlend(BlendMode.NonPremultiplied);
                manager.Batcher.Rect(manager.Screen.Bounds, color);
                manager.Batcher.PopBlend();
            }

            if (manager.Settings.ShowDebugInfo)
            {
                manager.Batcher.Text(manager.Assets.SmallFont, $"{FadeType}\nProgress:{progress:0.0000}\nAlpha:{color.A}", manager.Screen.Bounds.TopRight - Point2.One.OnlyX(), Vector2.One.ZeroY(), Color.CornflowerBlue);
            }
        }
    }
}
