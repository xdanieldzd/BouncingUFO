namespace BouncingUFO
{
    public sealed class Settings
    {
        public const string Filename = "Settings.json";

        public bool Fullscreen { get; set; } = false;
        public bool ShowFramerate { get; set; } = false;
        public bool ShowDebugInfo { get; set; } = false;
    }
}
