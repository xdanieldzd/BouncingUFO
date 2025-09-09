using Foster.Framework;
using System.Numerics;

namespace BouncingUFO.Game.States
{
    public class Test(Manager manager, params object[] args) : GameStateBase(manager, args)
    {
        private const float waitDuration = 15f;

        private float continueToIntroTimer = 0f;

        public override void OnEnterState() { }

        public override void OnFadeIn() { }

        public override void OnFadeInComplete() { }

        public override void OnUpdate()
        {
            continueToIntroTimer = Calc.Approach(continueToIntroTimer, waitDuration, manager.Time.Delta);
            if (continueToIntroTimer >= waitDuration)
                LeaveState();
        }

        public override void OnRender()
        {
            manager.Batcher.Text(manager.Assets.Fonts["SmallFont"], "Hello, I am the Test GameState!", Vector2.Zero, Color.White);
            manager.Batcher.Text(manager.Assets.Fonts["SmallFont"], $"Going to {nameof(TitleScreen)} gamestate in {waitDuration - continueToIntroTimer:0.00} seconds...", new(0f, 12f), Color.White);

            manager.Batcher.Text(manager.Assets.Fonts["SmallFont"], "Testing LargeFont Windows-1252 extension~", new(0f, 24f), Color.White);

            var fontTest = string.Empty;
            for (var i = ' '; i <= 0xFF; i++)
            {
                fontTest += i;
                if (((i + 1) % 16) == 0) fontTest += ' ';
                if (((i + 1) % 32) == 0) fontTest += '\n';
            }
            fontTest += "\n Zornig und gequält rügen jeweils Pontifex und Volk die maßlose\n bischöfliche¹ Hybris.\n";
            fontTest += "\n (¹: F****-P**** T******-v** E***)";

            manager.Batcher.Text(manager.Assets.Fonts["LargeFont"], fontTest, new(0f, 40f), Color.White);
        }

        public override void OnBeginFadeOut() { }

        public override void OnFadeOut() { }

        public override void OnLeaveState()
        {
            manager.GameStates.Pop();
            manager.GameStates.Push(new TitleScreen(manager));
        }
    }
}
