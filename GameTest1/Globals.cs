namespace GameTest1
{
    public static class Globals
    {
#if !DEBUG
        public static bool NormalStartup => true;
#else
        public static bool NormalStartup => false;
#endif
        public static bool FixedSeed => true;
        public static bool QuickStart => true;
        public static bool ShowDebugInfo => false;
    }
}
