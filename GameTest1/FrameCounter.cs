namespace GameTest1
{
    public class FrameCounter
    {
        const int numSamples = 5 * 60;

        double currentFps, averageFps, elapsed;
        int frameCount;
        readonly List<double> previousFps = [];

        public double CurrentFps => currentFps;
        public double AverageFps => averageFps;

        public void Update(float delta)
        {
            elapsed += delta;
            frameCount++;

            currentFps = frameCount / elapsed;
            if (elapsed >= 1.0)
            {
                frameCount = 0;
                elapsed = 0.0;

                previousFps.Insert(0, currentFps);
                if (previousFps.Count >= numSamples) previousFps.RemoveAt(previousFps.Count - 1);

                averageFps = previousFps.Sum() / previousFps.Count;
            }
        }
    }
}
