using Foster.Framework;
using GameTest1.Utilities;
using System.Numerics;

namespace GameTest1.GameStates
{
    public class TitleScreen(Manager manager) : GameStateBase(manager), IGameState
    {
        private const float screenFadeDuration = 0.75f;
        private const float waitDuration = 5f;

        private enum State { Initialize, FadeIn, WaitForTimeoutOrInput, FadeOut }

        private readonly ScreenFader screenFader = new(manager);

        private State currentState = State.Initialize;
        private float mainStateTimer = 0f;

        public override void UpdateApp()
        {
            switch (currentState)
            {
                case State.Initialize:
                    screenFader.Begin(ScreenFadeType.FadeIn, screenFadeDuration, Color.Black);
                    currentState = State.FadeIn;
                    break;

                case State.FadeIn:
                    if (screenFader.Update())
                        currentState = State.WaitForTimeoutOrInput;
                    break;

                case State.WaitForTimeoutOrInput:
                    mainStateTimer = Calc.Approach(mainStateTimer, waitDuration, manager.Time.Delta);
                    if (mainStateTimer >= waitDuration || manager.Controls.Action1.Down || manager.Controls.Action2.Down)
                    {
                        screenFader.Begin(ScreenFadeType.FadeOut, screenFadeDuration, ScreenFader.PreviousColor);
                        currentState = State.FadeOut;
                    }
                    break;

                case State.FadeOut:
                    if (screenFader.Update())
                    {
                        manager.GameStates.Pop();
                        manager.GameStates.Push(new MainMenu(manager));
                    }
                    break;
            }
        }

        public override void Render()
        {
            manager.Screen.Clear(Color.DarkGray);

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

            screenFader.Render();
        }
    }
}
