using BouncingUFO.Game.UI;
using Foster.Framework;

namespace BouncingUFO.Game.States
{
    public class MainMenu(Manager manager, params object[] args) : GameStateBase(manager, args)
    {
        private readonly MenuBox menuBox = new(manager)
        {
            Font = manager.Assets.LargeFont,
            GraphicsSheet = manager.Assets.UI["DialogBox"],
            FramePaddingTopLeft = (12, 12),
            FramePaddingBottomRight = (14, 14),
            LinePadding = 6,
            SmallFont = manager.Assets.SmallFont,
            BackgroundColor = new(0x3E4F65),
            HighlightTextColor = Color.Lerp(Color.Green, Color.White, 0.35f)
        };

        private MenuBoxItem[] gameSelectMenuItems = [];
        private MenuBoxItem[] modeSelectMenuItems = [];
        private MenuBoxItem[] levelSelectMenuItems = [];

        private IGameState? nextGameState = null;

        public override void OnEnterState()
        {
            gameSelectMenuItems =
            [
                new() { Label = "Arcade Mode", Action = MenuArcadeModeSelectAction },
                new() { Label = "Play Single Level", Action = MenuSingleLevelSelectAction },
                new() { Label = "Cancel", Action = MenuCancelAction, IsCancelAction = true }
            ];
            modeSelectMenuItems = [.. manager.Assets.Progression.Select(x => new MenuBoxItem() { Label = x.Value.Title, Action = MenuArcadeModeStartAction }), new() { Label = "Cancel", Action = MenuEnterMainMenuAction, IsCancelAction = true }];
            levelSelectMenuItems = [.. manager.Assets.Maps.Select(x => new MenuBoxItem() { Label = $"{x.Value.Title} ({x.Key})", Action = MenuSingleLevelStartAction }), new() { Label = "Cancel", Action = MenuEnterMainMenuAction, IsCancelAction = true }];

            MenuEnterMainMenuAction();

            menuBox.Open();
        }

        public override void OnFadeInComplete() { }

        public override void OnUpdate()
        {
            menuBox.Update();
        }

        public override void OnRender()
        {
            menuBox.Render();
        }

        public override void OnBeginFadeOut() { }

        public override void OnLeaveState()
        {
            menuBox.Close();

            if (nextGameState == null) manager.GameStates.Pop();
            else manager.GameStates.Push(nextGameState);
        }

        private void MenuEnterMainMenuAction()
        {
            nextGameState = null;

            menuBox.TextAlignment = MenuBoxTextAlignment.Center;
            menuBox.MenuTitle = "Select Mode";
            menuBox.MenuItems = gameSelectMenuItems;
        }

        private void MenuArcadeModeSelectAction()
        {
            menuBox.TextAlignment = MenuBoxTextAlignment.Center;
            menuBox.MenuTitle = "Select Difficulty";
            menuBox.MenuItems = modeSelectMenuItems;
        }

        private void MenuArcadeModeStartAction()
        {
            nextGameState = new InGame(manager, [.. manager.Assets.Progression.ElementAt(menuBox.SelectedIndex).Value.MapNames]);
            LeaveState();
        }

        private void MenuSingleLevelSelectAction()
        {
            menuBox.TextAlignment = MenuBoxTextAlignment.Left;
            menuBox.MenuTitle = "Select Level";
            menuBox.MenuItems = levelSelectMenuItems;
        }

        private void MenuSingleLevelStartAction()
        {
            nextGameState = new InGame(manager, [manager.Assets.Maps.ElementAt(menuBox.SelectedIndex).Key]);
            LeaveState();
        }

        private void MenuCancelAction()
        {
            nextGameState = null;
            LeaveState();
        }
    }
}
