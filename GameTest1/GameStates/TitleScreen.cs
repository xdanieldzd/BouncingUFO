using Foster.Framework;
using GameTest1.Utilities;
using System.Numerics;

namespace GameTest1.GameStates
{
    public class TitleScreen(Manager manager) : GameStateBase(manager), IGameState
    {
        private const float screenFadeDuration = 0.75f;

        private enum State { Initialize, FadeIn, MainMenu, FadeOut }

        private readonly ScreenFader screenFader = new(manager);

        private State currentState = State.Initialize;
        private string selectedMapName = manager.Assets.Maps.ElementAt(0).Key;
        private int selectedMapIndex = 0;

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
                    if (screenFader.Update())
                        currentState = State.MainMenu;
                    break;

                case State.MainMenu:
                    if (manager.Controls.Move.PressedDown)
                    {
                        selectedMapIndex++;
                        if (selectedMapIndex > manager.Assets.Maps.Count - 1) selectedMapIndex = 0;
                        selectedMapName = manager.Assets.Maps.ElementAt(selectedMapIndex).Key;
                    }
                    else if (manager.Controls.Move.PressedUp)
                    {
                        selectedMapIndex--;
                        if (selectedMapIndex < 0) selectedMapIndex = manager.Assets.Maps.Count - 1;
                        selectedMapName = manager.Assets.Maps.ElementAt(selectedMapIndex).Key;
                    }
                    else if (manager.Controls.Action1.Down || manager.Controls.Action2.Down)
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
                        var inGameState = new InGame(manager);
                        inGameState.LoadMap(selectedMapName, true);

                        manager.GameStates.Pop();
                        manager.GameStates.Push(inGameState);
                    }
                    break;
            }
        }

        public override void Render()
        {
            manager.Screen.Clear(Color.DarkGray);

            var textPos = new Vector2(16f, 16f);
            manager.Batcher.Text(manager.Assets.LargeFont, "Select Map", textPos, Color.White);
            textPos.Y += manager.Assets.LargeFont.LineHeight;

            for (var i = 0; i < manager.Assets.Maps.Count; i++)
            {
                var (name, map) = manager.Assets.Maps.ElementAt(i);
                var textColor = selectedMapName == name ? Color.Green : Color.White;
                if (selectedMapName == name) manager.Batcher.Text(manager.Assets.LargeFont, ">", textPos, textColor);
                manager.Batcher.Text(manager.Assets.LargeFont, $"{map.Title} ({name})", textPos + new Vector2(12f, 0f), textColor);
                textPos.Y += manager.Assets.LargeFont.LineHeight;
            }

            screenFader.Render();
        }
    }
}
