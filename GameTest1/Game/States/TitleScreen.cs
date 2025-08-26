using Foster.Framework;
using GameTest1.Game.UI;
using GameTest1.Utilities;
using System.Numerics;

namespace GameTest1.Game.States
{
    public class TitleScreen(Manager manager, params object[] args) : GameStateBase(manager, args)
    {
        private const float waitDuration = 5f;

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

        private enum State { WaitingForTimeout, InMainMenu }
        private State currentState = State.WaitingForTimeout;

        private float mainStateTimer = 0f;

        public override void OnEnter()
        {
            mainMenuItems =
            [
                new MenuBoxItem() { Label = "Start Game", Action = MenuMainStartGameAction },
                new MenuBoxItem() { Label = "Options", Action = MenuMainOptionsAction },
                new MenuBoxItem() { Label = "Exit Game", Action = MenuMainExitAction },
            ];
            optionsMenuItems =
            [
                new MenuBoxItem() { Label = "Toggle Debug Info", Action = (m) => { Globals.ShowDebugInfo = !Globals.ShowDebugInfo; m.Open(); } },
                new MenuBoxItem() { Label = "Return", Action = MenuOptionsReturnAction },
            ];

            menuBox.MenuItems = mainMenuItems;
        }

        public override void OnUpdateMain()
        {
            switch (currentState)
            {
                case State.WaitingForTimeout:
                    {
                        mainStateTimer = Calc.Approach(mainStateTimer, waitDuration, manager.Time.Delta);
                        if (mainStateTimer >= waitDuration || manager.Controls.Action1.Down || manager.Controls.Action2.Down)
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

        public override void OnRenderMain()
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

            menuBox.Render();
        }

        public override void OnExit()
        {
            menuBox.Close();

            manager.GameStates.Pop();
            manager.GameStates.Push(new MainMenu(manager));
        }

        private void MenuMainStartGameAction(MenuBox menuBox)
        {
            screenFader.Begin(ScreenFadeType.FadeOut, ScreenFadeDuration, ScreenFader.PreviousColor);
            ExitState();
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
