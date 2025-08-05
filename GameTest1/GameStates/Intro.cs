using Foster.Framework;
using System.Numerics;

namespace GameTest1.GameStates
{
    public class Intro(Manager manager) : GameStateBase(manager), IGameState
    {
        private const float screenFadeDuration = 0.75f;
        private const float mainStateDuration = 3f;

        private enum State { Start, Main, End }

        private State currentState = State.Start;

        private float screenFadeTimer = 0f;
        private Color screenFadeColor = Color.Black;

        private float mainStateTimer = 0f;

        public override void UpdateApp()
        {
            switch (currentState)
            {
                case State.Start:
                    screenFadeTimer = Calc.Approach(screenFadeTimer, screenFadeDuration, manager.Time.Delta);
                    screenFadeColor.A = (byte)(255f - (screenFadeTimer / screenFadeDuration * 255f));
                    if (screenFadeTimer >= screenFadeDuration) currentState = State.Main;
                    break;

                case State.Main:
                    mainStateTimer = Calc.Approach(mainStateTimer, mainStateDuration, manager.Time.Delta);
                    if (mainStateTimer >= mainStateDuration)
                    {
                        currentState = State.End;
                        screenFadeTimer = 0f;
                    }
                    break;

                case State.End:
                    screenFadeTimer = Calc.Approach(screenFadeTimer, screenFadeDuration, manager.Time.Delta);
                    screenFadeColor.A = (byte)(screenFadeTimer / screenFadeDuration * 255f);
                    if (screenFadeTimer >= screenFadeDuration)
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

            manager.Batcher.Text(manager.Assets.Font, $"Intro stuffs goes here! Going to InGame state in {mainStateDuration - mainStateTimer:0.00} sec....", Vector2.Zero, Color.White);

            manager.Batcher.Rect(manager.Screen.Bounds, screenFadeColor);
        }
    }
}
