using Foster.Framework;
using System.Numerics;

namespace GameTest1.Game.States
{
    public class Test(Manager manager, params object[] args) : GameStateBase(manager, args)
    {
        private const float waitDuration = 5f;

        private float continueToIntroTimer = 0f;

        public override void OnEnter() { }

        public override void OnUpdateMain()
        {
            continueToIntroTimer = Calc.Approach(continueToIntroTimer, waitDuration, manager.Time.Delta);
            if (continueToIntroTimer >= waitDuration)
                ExitState();
        }

        public override void OnRenderMain()
        {
            manager.Batcher.Text(manager.Assets.SmallFont, "Hello, I am the Test GameState!", Vector2.Zero, Color.White);

            manager.Batcher.Text(manager.Assets.LargeFont, $"Nothing to test right now!\nGoing to {nameof(TitleScreen)} gamestate in {waitDuration - continueToIntroTimer:0.00} seconds...", new(0f, 30f), Color.White);
        }

        public override void OnExit()
        {
            manager.GameStates.Pop();
            manager.GameStates.Push(new TitleScreen(manager));
        }
    }
}
