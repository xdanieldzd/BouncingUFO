using Foster.Framework;
using System.Diagnostics;
using System.Numerics;

namespace BouncingUFO.Utilities
{
    public class FrameCounter
    {
        private const int maxFramerate = 60;
        private const int numSamples = 20;

        protected Manager manager;

        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        private readonly Queue<float> frametimeSeconds = new(numSamples);

        private readonly Color[] textColors;

        public float Framerate => 1f / (frametimeSeconds.Sum() / frametimeSeconds.Count);

        public FrameCounter(Manager manager)
        {
            this.manager = manager;

            var lerpAmounts = Enumerable.Range(0, maxFramerate / 2).Select(x => Calc.ClampedMap(x, 0, maxFramerate / 2, 0f, 1f)).ToArray();
            textColors = [.. Enumerable.Range(0, maxFramerate).Select(x => Color.Lerp(x < maxFramerate / 2 ? Color.Red : Color.Yellow, x < maxFramerate / 2 ? Color.Yellow : Color.Green, lerpAmounts[x % lerpAmounts.Length]))];
        }

        public void Update()
        {
            frametimeSeconds.Enqueue((float)stopwatch.Elapsed.TotalSeconds);
            if (frametimeSeconds.Count > numSamples) frametimeSeconds.Dequeue();

            stopwatch.Restart();
        }

        public void Render(Vector2 position, SpriteFont font) => manager.Batcher.Text(font, $"{Framerate:0.000}", position, textColors[Calc.Clamp((int)Framerate, 0, textColors.Length - 1)]);
    }
}
