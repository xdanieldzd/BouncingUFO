using Foster.Framework;
using System.Numerics;

namespace BouncingUFO.Game.States
{
    public class Intro(Manager manager, params object[] args) : GameStateBase(manager, args)
    {
        public override Color ClearColor => Color.Black;

        private const float waitDuration = 8f;

        private readonly SpriteFont smallFont = manager.Assets.Fonts["SmallFont"];
        private readonly SpriteFont largeFont = manager.Assets.Fonts["LargeFont"];

        private float continueToTitleScreenTimer = 0f;

        public override void OnEnterState() { }

        public override void OnFadeIn() { }

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
                "Suggestions for improvements, bug reports, etc. are very welcome and much appreciated.",
                largeFont, manager.Screen.Bounds, Color.White);

            var bottomText = $"Build {Manager.BuildInfo.BuildDateTime:yyyy-MM-dd HH:mm:ss 'UTC'zzz}";
            manager.Batcher.Text(
                smallFont,
                bottomText,
                manager.Screen.Bounds.BottomCenter - smallFont.SizeOf(bottomText) / 2f - new Vector2(0f, smallFont.Size * 2f),
                Color.White);

            if (manager.Settings.ShowDebugInfo)
            {
                manager.Batcher.Text(smallFont, $"Built {Manager.BuildInfo.BuildDateTime:o} by {Manager.BuildInfo.UserName} on {Manager.BuildInfo.MachineName}", Vector2.Zero, Color.White);
            }
        }

        public override void OnBeginFadeOut() { }

        public override void OnFadeOut() { }

        public override void OnLeaveState()
        {
            manager.GameStates.Push(new TitleScreen(manager));
        }
    }
}
