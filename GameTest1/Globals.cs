namespace GameTest1
{
    public static class Globals
    {
#if !DEBUG
        public static bool NormalStartup { get; set; } = true;
#else
        public static bool NormalStartup { get; set; } = false;
#endif
        public static bool FixedSeed { get; set; } = true;
        public static bool QuickStart { get; set; } = false;
        public static bool ShowDebugInfo { get; set; } = false;
    }
}
