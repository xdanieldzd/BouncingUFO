using Foster.Framework;
using System.Numerics;

namespace GameTest1.GameStates
{
    public class Game(Manager manager) : GameState(manager), IGameState
    {
        public override void UpdateApp()
        {
            //
        }

        public override void Render()
        {
            manager.Screen.Clear(0x3E4F65);

            manager.Batcher.Text(manager.Assets.Font, "This is a test! This is the 'Game' GameState!", Vector2.Zero, Color.White);
            manager.Batcher.Text(manager.Assets.Font, $"Current FPS:{manager.FrameCounter.CurrentFps:00.00}, average FPS:{manager.FrameCounter.AverageFps:00.00}", new(0f, 20f), Color.White);
        }
    }
}
