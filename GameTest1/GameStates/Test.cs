using Foster.Framework;
using GameTest1.Game.UI;
using System.Numerics;

namespace GameTest1.GameStates
{
    public class Test(Manager manager) : GameStateBase(manager), IGameState
    {
        private const float waitDuration = 5f;

        private readonly MenuBox menuBox = new(manager)
        {
            Font = manager.Assets.LargeFont,
            GraphicsSheet = manager.Assets.UI["DialogBox"],
            FramePaddingTopLeft = (12, 12),
            FramePaddingBottomRight = (14, 14),
            LinePadding = 4
        };

        private string menuResultTest = string.Empty;
        private float continueToIntroTimer = 0f;

        public override void Initialize()
        {
            var items = new MenuBoxItem[]
            {
                new() { Label = "Continue", Action = MenuContinueAction },
                new() { Label = "Restart", Action = MenuRestartAction },
                new() { Label = "Exit to Menu", Action = MenuExitAction },
            };

            menuBox.HighlightTextColor = Color.Lerp(Color.Green, Color.White, 0.35f);
            menuBox.Initialize("PAUSE", items);
        }

        private void MenuContinueAction() => menuResultTest = "Continue was selected, return to game!";
        private void MenuRestartAction() => menuResultTest = "Restart was selected, restart the current level!";
        private void MenuExitAction() => menuResultTest = "Exit to Menu was selected, exit to main menu!";

        public override void UpdateApp()
        {
            menuBox.Update();

            if (!menuBox.IsOpen)
            {
                continueToIntroTimer = Calc.Approach(continueToIntroTimer, waitDuration, manager.Time.Delta);
                if (continueToIntroTimer >= waitDuration)
                {
                    manager.GameStates.Pop();
                    manager.GameStates.Push(new Intro(manager));
                }
            }
        }

        public override void Render()
        {
            manager.Screen.Clear(Color.DarkGray);

            manager.Batcher.Text(manager.Assets.SmallFont, "Hello, I am the Test GameState!", Vector2.Zero, Color.White);

            menuBox.Render();

            if (!menuBox.IsOpen)
            {
                manager.Batcher.Text(manager.Assets.LargeFont, $"Menu result was:\n\n'{menuResultTest}'\n\nAs this is a test, going to Intro gamestate in {waitDuration - continueToIntroTimer:0.00} seconds...", new(0f, 30f), Color.White);
            }
        }
    }
}
