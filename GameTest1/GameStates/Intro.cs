using Foster.Framework;
using GameTest1.Utilities;
using System.Numerics;

namespace GameTest1.GameStates
{
    public class Intro(Manager manager) : GameStateBase(manager), IGameState
    {
        private const float screenFadeDuration = 0.75f;
        private const float waitDuration = 10f;

        private enum State { Initialize, FadeIn, WaitForTimeoutOrInput, FadeOut }

        private readonly ScreenFader screenFader = new(manager);

        private State currentState = State.Initialize;
        private float mainStateTimer = 0f;

        public override void UpdateApp()
        {
            switch (currentState)
            {
                case State.Initialize:
                    screenFader.FadeType = ScreenFadeType.FadeIn;
                    screenFader.Duration = screenFadeDuration;
                    screenFader.Color = Color.Black;
                    screenFader.Reset();
                    currentState = State.FadeIn;

                    if (Globals.QuickStart)
                    {
                        manager.GameStates.Pop();
                        manager.GameStates.Push(new InGame(manager));
                    }
                    break;

                case State.FadeIn:
                    if (screenFader.Update()) currentState = State.WaitForTimeoutOrInput;
                    break;

                case State.WaitForTimeoutOrInput:
                    mainStateTimer = Calc.Approach(mainStateTimer, waitDuration, manager.Time.Delta);
                    if (mainStateTimer >= waitDuration || manager.Controls.Action1.Down || manager.Controls.Action2.Down)
                    {
                        screenFader.FadeType = ScreenFadeType.FadeOut;
                        screenFader.Duration = screenFadeDuration;
                        screenFader.Color = ScreenFader.PreviousColor;
                        screenFader.Reset();
                        currentState = State.FadeOut;
                    }
                    break;

                case State.FadeOut:
                    if (screenFader.Update())
                    {
                        manager.GameStates.Pop();
                        manager.GameStates.Push(new InGame(manager));
                    }
                    break;
            }
        }

        public override void Render()
        {
            manager.Screen.Clear(Color.DarkGray);

            var testStringBuilder = new System.Text.StringBuilder();
            testStringBuilder.AppendLine($"Intro stuffs goes here! Currently just a font test tho, I guess.\nGoing to InGame state in {waitDuration - mainStateTimer:0.00} sec.... OR press an Action button!");
            testStringBuilder.AppendLine();

            manager.Batcher.Text(manager.Assets.Font, testStringBuilder.ToString(), manager.Screen.Width, Vector2.Zero, Color.White);

            manager.Batcher.Text(manager.Assets.BigFont, "0123456789\nTIME 01:23:45\nENERGY 67\nLEFT 89", new(0f, 50f), Color.CornflowerBlue);

            screenFader.Render();
        }
    }
}
