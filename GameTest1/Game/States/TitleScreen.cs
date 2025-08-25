using Foster.Framework;
using System.Numerics;

namespace GameTest1.Game.States
{
    public class TitleScreen(Manager manager, params object[] args) : GameStateBase(manager, args)
    {
        private const float waitDuration = 5f;

        private float mainStateTimer = 0f;

        public override void OnEnter() { }

        public override void OnUpdateMain()
        {
            mainStateTimer = Calc.Approach(mainStateTimer, waitDuration, manager.Time.Delta);
            if (mainStateTimer >= waitDuration || manager.Controls.Action1.Down || manager.Controls.Action2.Down)
                ExitState();
        }

        public override void OnRenderMain()
        {
            var titleText =
                "GAME TEST PROJECT #1\n" +
                " -- BOUNCING UFO -- \n" +
                "\n" +
                "PROTOTYPE  VERSION 2";
            manager.Batcher.Text(
                manager.Assets.FutureFont,
                titleText,
                manager.Screen.Bounds.Center - manager.Assets.FutureFont.SizeOf(titleText) / 2f - new Vector2(0f, manager.Assets.FutureFont.Size * 4f),
                Color.White);

            var bottomText = "August 2025 by xdaniel -- xdaniel.neocities.org";
            manager.Batcher.Text(
                manager.Assets.SmallFont,
                bottomText,
                manager.Screen.Bounds.BottomCenter - manager.Assets.SmallFont.SizeOf(bottomText) / 2f - new Vector2(0f, manager.Assets.SmallFont.Size * 2f),
                Color.White);
        }

        public override void OnExit()
        {
            manager.GameStates.Pop();
            manager.GameStates.Push(new MainMenu(manager));
        }
    }
}
