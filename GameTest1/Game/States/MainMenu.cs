using Foster.Framework;
using GameTest1.Game.UI;
using GameTest1.Utilities;

namespace GameTest1.Game.States
{
    public class MainMenu(Manager manager, params object[] args) : GameStateBase(manager, args)
    {
        private readonly MenuBox menuBox = new(manager)
        {
            Font = manager.Assets.LargeFont,
            GraphicsSheet = manager.Assets.UI["DialogBox"],
            FramePaddingTopLeft = (10, 10),
            FramePaddingBottomRight = (12, 12),
            LinePadding = 4,
            BackgroundColor = new(0x3E4F65),
            HighlightTextColor = Color.Lerp(Color.Green, Color.White, 0.35f)
        };

        private string selectedMapName = string.Empty;

        public override void OnEnter()
        {
            menuBox.Initialize("Select Map", manager.Assets.Maps.Select(x => new MenuBoxItem() { Label = $"{x.Value.Title} ({x.Key})", Action = MenuAction }));
            menuBox.Open();

            selectedMapName = manager.Assets.Maps.ElementAt(0).Key;
        }

        public override void OnUpdateMain()
        {
            menuBox.Update();
        }

        public override void OnRenderMain()
        {
            menuBox.Render();
        }

        public override void OnExit()
        {
            menuBox.Close();

            manager.GameStates.Pop();
            manager.GameStates.Push(new InGame(manager, selectedMapName));
        }

        private void MenuAction(MenuBox menuBox)
        {
            selectedMapName = manager.Assets.Maps.ElementAt(menuBox.SelectedIndex).Key;

            screenFader.Begin(ScreenFadeType.FadeOut, ScreenFadeDuration, ScreenFader.PreviousColor);
            ExitState();
        }
    }
}
