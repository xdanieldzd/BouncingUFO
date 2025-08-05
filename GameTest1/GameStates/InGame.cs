using Foster.Framework;
using GameTest1.Utilities;
using System.Numerics;

namespace GameTest1.GameStates
{
    public class InGame(Manager manager) : GameStateBase(manager), IGameState
    {
        private const float screenFadeDuration = 0.75f;

        private enum State { Initialize, FadeIn, GameStartCountdown, MainLogic }

        private readonly ScreenFader screenFader = new(manager);

        private State currentState = State.Initialize;
        private float gameStartCountdown;

        //

        public override void UpdateApp()
        {
            switch (currentState)
            {
                case State.Initialize:
                    screenFader.FadeType = ScreenFadeType.FadeIn;
                    screenFader.Duration = screenFadeDuration;
                    screenFader.Color = ScreenFader.PreviousColor;
                    screenFader.Reset();
                    currentState = State.FadeIn;
                    gameStartCountdown = 5f;
                    break;

                case State.FadeIn:
                    if (screenFader.Update()) currentState = State.GameStartCountdown;
                    break;

                case State.GameStartCountdown:
                    gameStartCountdown = Calc.Approach(gameStartCountdown, 0f, manager.Time.Delta);
                    if (gameStartCountdown <= 0f) currentState = State.MainLogic;
                    break;

                case State.MainLogic:
                    // TODO
                    break;
            }
        }

        public override void Render()
        {
            manager.Screen.Clear(0x3E4F65);

            manager.MapRenderer.Render("TestMap");

            switch (currentState)
            {
                case State.GameStartCountdown:
                    {
                        var timer = Math.Floor(gameStartCountdown);
                        var secondText = timer < 1f ? "GO!!" : (timer < 4f ? $"{timer}..." : "Get Ready!");
                        manager.Batcher.Text(manager.Assets.Font, secondText, manager.Screen.Bounds.Center, Color.White);
                    }
                    break;

                case State.MainLogic:
                    manager.Batcher.Text(manager.Assets.Font, "This is a test! This is the 'Game' GameState!", Vector2.Zero, Color.White);
                    manager.Batcher.Text(manager.Assets.Font, $"Current FPS:{manager.FrameCounter.CurrentFps:00.00}, average FPS:{manager.FrameCounter.AverageFps:00.00}", new(0f, 20f), Color.White);
                    break;
            }

            screenFader.Render();
        }
    }
}
