using Foster.Framework;
using GameTest1.Game.UI;
using System.Numerics;

namespace GameTest1.Game.States
{
    public class TitleScreen(Manager manager, params object[] args) : GameStateBase(manager, args)
    {
        private readonly MenuBox menuBox = new(manager)
        {
            Font = manager.Assets.LargeFont,
            GraphicsSheet = manager.Assets.UI["DialogBox"],
            FramePaddingTopLeft = (10, 10),
            FramePaddingBottomRight = (12, 12),
            LinePadding = 4,
            BackgroundColor = new(0x3E4F65),
            HighlightTextColor = Color.Lerp(Color.Green, Color.White, 0.35f),
            WindowAlignment = MenuBoxWindowAlignment.BottomCenter,
            WindowSizing = MenuBoxWindowSizing.Automatic,
            MenuTitle = string.Empty
        };

        private MenuBoxItem[] mainMenuItems = [];
        private MenuBoxItem[] optionsMenuItems = [];

        private enum State { WaitingForMenuButton, InMainMenu }
        private State currentState = State.WaitingForMenuButton;

        public override void OnEnterState()
        {
            mainMenuItems =
            [
                new MenuBoxItem() { Label = "Start Game", Action = MenuMainStartGameAction },
                new MenuBoxItem() { Label = "Options", Action = MenuMainOptionsAction },
                new MenuBoxItem() { Label = "Exit Game", Action = MenuMainExitAction, IsCancelAction = true }
            ];
            optionsMenuItems =
            [
                new MenuBoxItem() { Label = "Toggle Fullscreen", Action = (m) => { manager.Window.Fullscreen = !manager.Window.Fullscreen; m.Open(); } },
                new MenuBoxItem() { Label = "Toggle Debug Info", Action = (m) => { Globals.ShowDebugInfo = !Globals.ShowDebugInfo; m.Open(); } },
                new MenuBoxItem() { Label = "Return", Action = MenuOptionsReturnAction, IsCancelAction = true }
            ];

            menuBox.MenuItems = mainMenuItems;
        }

        public override void OnFadeInComplete() { }

        public override void OnUpdate()
        {
            switch (currentState)
            {
                case State.WaitingForMenuButton:
                    {
                        if (manager.Controls.Menu.ConsumePress())
                        {
                            menuBox.Open();
                            currentState = State.InMainMenu;
                        }
                    }
                    break;

                case State.InMainMenu:
                    {
                        menuBox.Update();
                    }
                    break;
            }
        }

        public override void OnRender()
        {
            var titleText =
                "GAME TEST PROJECT #1\n" +
                " -- BOUNCING UFO -- \n" +
                "\n" +
                "PROTOTYPE  VERSION 2";
            manager.Batcher.Text(
                manager.Assets.FutureFont,
                titleText,
                manager.Screen.Bounds.Center - manager.Assets.FutureFont.SizeOf(titleText) / 2f - new Vector2(0f, manager.Assets.FutureFont.Size * 4f),
                Color.White);

            var bottomText = "August 2025 by xdaniel -- xdaniel.neocities.org";
            manager.Batcher.Text(
                manager.Assets.SmallFont,
                bottomText,
                manager.Screen.Bounds.BottomCenter - manager.Assets.SmallFont.SizeOf(bottomText) / 2f - new Vector2(0f, manager.Assets.SmallFont.Size * 2f),
                Color.White);

            if (currentState == State.WaitingForMenuButton && manager.Time.BetweenInterval(0.5))
            {
                var buttonText = "Press Menu Button!";
                manager.Batcher.Text(
                    manager.Assets.LargeFont,
                    buttonText,
                    manager.Screen.Bounds.Center - manager.Assets.LargeFont.SizeOf(buttonText) / 2f + new Vector2(0f, manager.Assets.LargeFont.Size * 2f),
                    Color.White);
            }

            menuBox.Render();
        }

        public override void OnBeginFadeOut() { }

        public override void OnLeaveState()
        {
            currentState = State.WaitingForMenuButton;

            menuBox.Close();

            manager.GameStates.Push(new MainMenu(manager));
        }

        private void MenuMainStartGameAction(MenuBox menuBox)
        {
            LeaveState();
        }

        private void MenuMainOptionsAction(MenuBox menuBox)
        {
            menuBox.MenuItems = optionsMenuItems;
            menuBox.Open();
        }

        private void MenuMainExitAction(MenuBox menuBox)
        {
            manager.Exit();
        }

        private void MenuOptionsReturnAction(MenuBox menuBox)
        {
            menuBox.MenuItems = mainMenuItems;
            menuBox.Open();
        }
    }
}
