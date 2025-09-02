using BouncingUFO.Game.UI;
using Foster.Framework;
using System.Numerics;

namespace BouncingUFO.Game.States
{
    public class TitleScreen(Manager manager, params object[] args) : GameStateBase(manager, args)
    {
        private readonly SpriteFont smallFont = manager.Assets.Fonts["SmallFont"];
        private readonly SpriteFont largeFont = manager.Assets.Fonts["LargeFont"];
        private readonly SpriteFont futureFont = manager.Assets.Fonts["FutureFont"];

        private readonly Texture backgroundImageTexture = manager.Assets.Textures["TitleBackground"];
        private Vector2 backgroundImagePosition = Vector2.Zero;

        private readonly MenuBox menuBox = new(manager)
        {
            Font = manager.Assets.Fonts["LargeFont"],
            GraphicsSheet = manager.Assets.GraphicsSheets["DialogBox"],
            FramePaddingTopLeft = (12, 12),
            FramePaddingBottomRight = (14, 14),
            LinePadding = 6,
            BackgroundColor = new(0x3E4F65),
            SmallFont = manager.Assets.Fonts["SmallFont"],
            HighlightTextColor = Color.Lerp(Color.Green, Color.White, 0.35f),
            WindowAlignment = MenuBoxWindowAlignment.BottomCenter,
            WindowSizing = MenuBoxWindowSizing.Automatic
        };

        private MenuBoxItem[] mainMenuItems = [];
        private MenuBoxItem[] optionsMenuItems = [];
        private MenuBoxItem[] exitMenuItems = [];

        public override void OnEnterState()
        {
            mainMenuItems =
            [
                new MenuBoxItem() { Label = "Start Game", Action = LeaveState },
                new MenuBoxItem() { Label = "Options", Action = () => menuBox.MenuItems = optionsMenuItems },
                new MenuBoxItem() { Label = "Return", Action = menuBox.Close, IsCancelAction = true }
            ];
            optionsMenuItems =
            [
                new MenuBoxItem() { Label = "Toggle Fullscreen", Action = () => { manager.Settings.Fullscreen = !manager.Settings.Fullscreen; } },
                new MenuBoxItem() { Label = "Toggle FPS Display", Action = () => { manager.Settings.ShowFramerate = !manager.Settings.ShowFramerate; } },
                new MenuBoxItem() { Label = "Toggle Debug Info", Action = () => { manager.Settings.ShowDebugInfo = !manager.Settings.ShowDebugInfo; } },
                new MenuBoxItem() { Label = "Return", Action = () => menuBox.MenuItems = mainMenuItems, IsCancelAction = true }
            ];
            exitMenuItems =
            [
                new MenuBoxItem() { Label = "Yes", Action = manager.Exit },
                new MenuBoxItem() { Label = "No", Action = menuBox.Close, IsCancelAction = true }
            ];
        }

        public override void OnFadeIn()
        {
            ScrollBackground();
        }

        public override void OnFadeInComplete() { }

        public override void OnUpdate()
        {
            if (!menuBox.IsOpen)
            {
                if (manager.Controls.Menu.ConsumePress())
                {
                    menuBox.MenuTitle = string.Empty;
                    menuBox.MenuItems = mainMenuItems;
                    menuBox.Open();
                }
                else if (manager.Controls.Cancel.ConsumePress())
                {
                    menuBox.MenuTitle = "Exit Game?";
                    menuBox.MenuItems = exitMenuItems;
                    menuBox.Open();
                }
            }
            menuBox.Update();

            ScrollBackground();
        }

        public override void OnRender()
        {
            manager.Batcher.Image(new Subtexture(backgroundImageTexture, new Rect(backgroundImagePosition, backgroundImageTexture.Size)), Vector2.Zero, Color.White);

            var titleText =
                "GAME TEST PROJECT #1\n" +
                " -- BOUNCING UFO -- \n" +
                "\n" +
                " PUBLIC PROTOTYPE 1 ";
            manager.Batcher.Text(
                futureFont,
                titleText,
                manager.Screen.Bounds.Center - futureFont.SizeOf(titleText) / 2f - new Vector2(0f, futureFont.Size * 4f),
                Color.White);

            if (!menuBox.IsOpen)
            {
                var dateString = string.Empty;
                if (Manager.BuildInfo.StartDateTime.Year < Manager.BuildInfo.BuildDateTime.Year)
                    dateString += $"{Manager.BuildInfo.StartDateTime:yyyy}-";
                dateString += $"{Manager.BuildInfo.BuildDateTime:yyyy}";

                var bottomText = $"Written {dateString} by xdaniel -- xdaniel.neocities.org";
                manager.Batcher.Text(
                    smallFont,
                    bottomText,
                    manager.Screen.Bounds.BottomCenter - smallFont.SizeOf(bottomText) / 2f - new Vector2(0f, smallFont.Size * 2f),
                    Color.White);

                if (manager.Time.BetweenInterval(0.5))
                {
                    var buttonText = "Press Menu Button!";
                    manager.Batcher.Text(
                        largeFont,
                        buttonText,
                        manager.Screen.Bounds.Center - largeFont.SizeOf(buttonText) / 2f + new Vector2(0f, largeFont.Size * 2f),
                        Color.White);
                }
            }

            menuBox.Render();
        }

        public override void OnBeginFadeOut() { }

        public override void OnFadeOut()
        {
            ScrollBackground();
        }

        public override void OnLeaveState()
        {
            menuBox.Close();

            manager.GameStates.Push(new MainMenu(manager));
        }

        private void ScrollBackground()
        {
            backgroundImagePosition.X += manager.Time.Delta * 75f;
        }
    }
}
