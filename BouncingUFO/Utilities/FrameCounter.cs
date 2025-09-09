using Foster.Framework;
using System.Diagnostics;
using System.Numerics;

namespace BouncingUFO.Utilities
{
    public class FrameCounter
    {
        private const int maxFramerate = 60;
        private const int numSamples = 120;

        protected Manager manager;

        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        private readonly Queue<TimeSpan> frametimeQueue = new(numSamples);

        private readonly Color[] textColors;

        public float AverageFrametime => frametimeQueue.Select(x => (float)x.TotalMilliseconds).Sum() / frametimeQueue.Count;
        public float AverageFramerate => 1000f / AverageFrametime;

        public float LastFrametime => (float)frametimeQueue.LastOrDefault().TotalMilliseconds;
        public float LastFramerate => 1000f / LastFrametime;

        public FrameCounter(Manager manager)
        {
            this.manager = manager;

            var lerpAmounts = Enumerable.Range(0, maxFramerate / 2).Select(x => Calc.ClampedMap(x, 0, maxFramerate / 2, 0f, 1f)).ToArray();
            textColors = [.. Enumerable.Range(0, maxFramerate).Select(x => Color.Lerp(x < maxFramerate / 2 ? Color.Red : Color.Yellow, x < maxFramerate / 2 ? Color.Yellow : Color.Green, lerpAmounts[x % lerpAmounts.Length]))];
        }

        public void Update()
        {
            frametimeQueue.Enqueue(stopwatch.Elapsed);
            if (frametimeQueue.Count > numSamples) frametimeQueue.Dequeue();
            stopwatch.Restart();
        }

        public void Render(Vector2 position, SpriteFont font)
        {
            manager.Batcher.Text(font, $"{manager.GraphicsDevice.Driver}   {LastFramerate:0} FPS", position, Color.White);
            manager.Batcher.Text(font, $"{LastFrametime:0.0} ms", position + new Vector2(120f, 16f), textColors[Calc.Clamp((int)LastFramerate, 0, textColors.Length - 1)]);

            var graphPosition = position + new Vector2(0f, 16f);
            var graphData = frametimeQueue.Select((x, i) => (Time: x, Coord: new Vector2(i * 120f / numSamples, Calc.ClampedMap((float)x.TotalMilliseconds, 0f, (float)manager.UpdateMode.FixedMaxTime.TotalMilliseconds, 20f, 0f)))).ToArray();
            for (var i = 0; i < graphData.Length - 1; i++)
                manager.Batcher.Line(
                    graphPosition + graphData[i].Coord,
                    graphPosition + graphData[i + 1].Coord,
                    1f,
                    textColors[(int)Calc.ClampedMap((float)graphData[i].Time.TotalMilliseconds, 0f, (float)manager.UpdateMode.FixedMaxTime.TotalMilliseconds, textColors.Length - 1, 0f)],
                    textColors[(int)Calc.ClampedMap((float)graphData[i + 1].Time.TotalMilliseconds, 0f, (float)manager.UpdateMode.FixedMaxTime.TotalMilliseconds, textColors.Length - 1, 0f)]);
        }
    }
}
