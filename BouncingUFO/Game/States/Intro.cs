using Foster.Framework;

namespace BouncingUFO.Game.States
{
    public class Intro(Manager manager, params object[] args) : GameStateBase(manager, args)
    {
        public override Color ClearColor => Color.Black;

        private const float waitDuration = 5f;

        private float continueToTitleScreenTimer = 0f;

        public override void OnEnterState() { }

        public override void OnFadeInComplete() { }

        public override void OnUpdate()
        {
            continueToTitleScreenTimer = Calc.Approach(continueToTitleScreenTimer, waitDuration, manager.Time.Delta);
            if (continueToTitleScreenTimer >= waitDuration || manager.Controls.Confirm.ConsumePress() || manager.Controls.Menu.ConsumePress())
                LeaveState();
        }

        public override void OnRender()
        {
            manager.Batcher.TextCenteredInBounds(
                "-- DISCLAIMER --\n" +
                "\n" +
                "This is a work-in-progress demo build. There will be missing features, bugs and glitches, and a general lack of polish.\n" +
                "\n" +
                "Suggestions for improvements, bug reports, etc. are very welcome and much appreciated.\n" +
                "\n" +
                $"Build {Manager.BuildDate:yyyy-MM-ddTHH:mm:ss:fffZ}",
                manager.Assets.LargeFont, manager.Screen.Bounds, Color.White);
        }

        public override void OnBeginFadeOut() { }

        public override void OnLeaveState()
        {
            manager.GameStates.Push(new TitleScreen(manager));
        }
    }
}
