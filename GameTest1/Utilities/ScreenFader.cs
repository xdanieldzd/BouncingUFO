using Foster.Framework;

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

        public void Reset() => progress = 0f;

        public void Cancel()
        {
            progress = 1f;
            color.A = 0;
        }

        public bool Update()
        {
            if (!IsRunning) return false;

            progress = Calc.Approach(progress, Duration, manager.Time.Delta);
            if (FadeType == ScreenFadeType.FadeIn)
                color.A = (byte)(255f - (progress / Duration * 255f));
            else
                color.A = (byte)(progress / Duration * 255f);
            return progress >= Duration;
        }

        public void Render()
        {
            if (color.A > 0 && progress < Duration)
            {
                manager.Batcher.PushBlend(BlendMode.NonPremultiplied);
                manager.Batcher.Rect(manager.Screen.Bounds, color);
                manager.Batcher.PopBlend();
            }
        }
    }
}
