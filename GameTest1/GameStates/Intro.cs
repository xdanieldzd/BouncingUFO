using Foster.Framework;
using GameTest1.Utilities;
using System.Numerics;

namespace GameTest1.GameStates
{
    public class Intro(Manager manager) : GameStateBase(manager), IGameState
    {
        private const float screenFadeDuration = 0.75f;
        private const float waitDuration = 3f;

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

            manager.Batcher.Text(manager.Assets.Font, $"Intro stuffs goes here!\nGoing to InGame state in {waitDuration - mainStateTimer:0.00} sec.... OR press an Action button!", Vector2.Zero, Color.White);

            screenFader.Render();
        }
    }
}
