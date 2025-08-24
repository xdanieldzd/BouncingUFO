using Foster.Framework;
using GameTest1.Game.UI;
using GameTest1.Utilities;

namespace GameTest1.GameStates
{
    public class MainMenu(Manager manager) : GameStateBase(manager), IGameState
    {
        private const float screenFadeDuration = 0.5f;

        private enum State { Initialize, FadeIn, MainMenu, FadeOut }

        private readonly ScreenFader screenFader = new(manager);
        private readonly MenuBox menuBox = new(manager)
        {
            Font = manager.Assets.LargeFont,
            GraphicsSheet = manager.Assets.UI["DialogBox"],
            FramePaddingTopLeft = (10, 10),
            FramePaddingBottomRight = (12, 12),
            LinePadding = 4,
            BackgroundColor = new(0x3E4F65)
        };

        private State currentState = State.Initialize;
        private string selectedMapName = manager.Assets.Maps.ElementAt(0).Key;

        public override void Initialize()
        {
            menuBox.HighlightTextColor = Color.Lerp(Color.Green, Color.White, 0.35f);
            menuBox.Initialize("Select Map", manager.Assets.Maps.Select(x => new MenuBoxItem() { Label = $"{x.Value.Title} ({x.Key})", Action = MenuAction }));
        }

        private void MenuAction(MenuBox menuBox)
        {
            selectedMapName = manager.Assets.Maps.ElementAt(menuBox.SelectedIndex).Key;

            screenFader.Begin(ScreenFadeType.FadeOut, screenFadeDuration, ScreenFader.PreviousColor);
            currentState = State.FadeOut;
        }

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
                        currentState = State.MainMenu;
                    break;

                case State.MainMenu:
                    menuBox.Update();
                    break;

                case State.FadeOut:
                    if (screenFader.Update())
                    {
                        var inGameState = new InGame(manager);
                        inGameState.LoadMap(selectedMapName);

                        manager.GameStates.Pop();
                        manager.GameStates.Push(inGameState);
                    }
                    break;
            }
        }

        public override void Render()
        {
            manager.Screen.Clear(Color.DarkGray);

            menuBox.Render();

            screenFader.Render();
        }
    }
}
