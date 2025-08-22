using Foster.Framework;
using System.Numerics;

namespace GameTest1.GameStates
{
    public class Test(Manager manager) : GameStateBase(manager), IGameState
    {
        public override void UpdateApp() { }

        public override void Render()
        {
            manager.Screen.Clear(Color.DarkGray);

            manager.Batcher.Text(manager.Assets.SmallFont, "Hello, I am the Test GameState!", Vector2.Zero, Color.White);
            manager.Batcher.Text(manager.Assets.LargeFont, "Currently nothing to test! Going to Intro in 5 seconds...", new(0f, 30f), Color.White);

            if (manager.Time.OnInterval(5))
            {
                manager.GameStates.Pop();
                manager.GameStates.Push(new Intro(manager));
            }
        }
    }
}
