namespace GameTest1
{
    public static class Startup
    {
        static void Main()
        {
            using var game = new Game();
            game.Run();
        }
    }
}
