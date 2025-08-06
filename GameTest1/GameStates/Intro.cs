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

            manager.Batcher.Text(manager.Assets.PixelFont, $"Intro stuffs goes here!\nGoing to InGame state in {waitDuration - mainStateTimer:0.00} sec.... OR press an Action button!", Vector2.Zero, Color.White);
            manager.Batcher.Text(manager.Assets.PixelFont, "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent at felis vitae quam posuere venenatis sed vel lacus. Quisque venenatis nisi ut nibh accumsan, non volutpat sapien venenatis. Suspendisse sem erat, venenatis eu eros porttitor, vulputate fermentum arcu. Donec et lectus vitae ex interdum tempor. Nunc ipsum erat, fringilla id euismod eu, rutrum maximus quam. Suspendisse sit amet odio ut libero feugiat tristique nec et justo. Sed hendrerit dolor a tellus blandit, sit amet efficitur leo accumsan. Ut porta velit nisl, ac gravida sem mattis pulvinar. Maecenas ornare consequat porttitor. Fusce ac volutpat nibh, id ultrices magna.\n\nDonec efficitur, elit et hendrerit luctus, elit metus condimentum arcu, in auctor leo nibh vel ligula. Integer suscipit tortor vestibulum, tincidunt ex vitae, suscipit lorem. Proin vestibulum nec sapien vel fermentum. Quisque ac dictum nunc, at maximus ipsum. Nullam in auctor eros. Donec at ipsum consequat, eleifend justo at, malesuada metus. Ut eget libero quam. Donec feugiat mollis metus, in laoreet neque elementum sit amet. Quisque bibendum hendrerit massa, a scelerisque turpis vulputate vel. Aenean venenatis dolor et finibus tincidunt. Etiam at eleifend mi, eu dapibus purus. \r\n", manager.Screen.Width, new(0f, 32f), Color.White);

            screenFader.Render();
        }
    }
}
