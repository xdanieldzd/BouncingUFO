using Foster.Framework;
using System.Numerics;

namespace BouncingUFO.Game.UI
{
    public enum MenuBoxWindowAlignment { Manual, TopLeft, TopCenter, TopRight, CenterLeft, Center, CenterRight, BottomLeft, BottomCenter, BottomRight }
    public enum MenuBoxWindowSizing { Manual, Automatic }
    public enum MenuBoxTextAlignment { Left, Center }

    public class MenuBox(Manager manager) : UserInterfaceWindow(manager)
    {
        private const float titleGap = 6f;

        private readonly SpriteFont debugFont = manager.Assets.Fonts["SmallFont"];

        private Point2 manualPosition = Point2.Zero;
        private Point2 manualSize = Point2.One * 64;

        public override Point2 Size
        {
            get
            {
                if (WindowSizing == MenuBoxWindowSizing.Automatic && Font != null)
                {
                    var totalWidth = Font.SizeOf(menuItems.Append(new() { Label = MenuTitle }).MaxBy(x => x.Label.Length)?.Label).X + FramePaddingTopLeft.X + FramePaddingBottomRight.X;
                    var totalHeight = menuItems.Where(x => !string.IsNullOrWhiteSpace(x.Label)).Count() * (Font.LineHeight + LinePadding) + FramePaddingTopLeft.Y + FramePaddingBottomRight.Y - LinePadding;
                    if (!string.IsNullOrWhiteSpace(MenuTitle)) totalHeight += Font.LineHeight + LinePadding + titleGap;

                    return new Vector2(totalWidth, totalHeight).CeilingToPoint2();
                }
                else
                    return manualSize;
            }
            set => manualSize = value;
        }
        public override Point2 Position
        {
            get
            {
                return WindowAlignment switch
                {
                    MenuBoxWindowAlignment.TopLeft => manager.Screen.Bounds.TopLeft + WindowAlignmentPadding,
                    MenuBoxWindowAlignment.TopCenter => manager.Screen.Bounds.TopCenter - Size.OnlyX() / 2 + WindowAlignmentPadding.OnlyY(),
                    MenuBoxWindowAlignment.TopRight => manager.Screen.Bounds.TopRight - Size.OnlyX() + WindowAlignmentPadding.OnlyY() - WindowAlignmentPadding.OnlyX(),

                    MenuBoxWindowAlignment.CenterLeft => manager.Screen.Bounds.CenterLeft - Size.OnlyY() / 2 + WindowAlignmentPadding.OnlyX(),
                    MenuBoxWindowAlignment.Center => manager.Screen.Bounds.Center - Size / 2,
                    MenuBoxWindowAlignment.CenterRight => manager.Screen.Bounds.CenterRight - Size.OnlyY() / 2 - Size.OnlyX() - WindowAlignmentPadding.OnlyX(),

                    MenuBoxWindowAlignment.BottomLeft => manager.Screen.Bounds.BottomLeft - Size.OnlyY() - WindowAlignmentPadding.OnlyY() + WindowAlignmentPadding.OnlyX(),
                    MenuBoxWindowAlignment.BottomCenter => manager.Screen.Bounds.BottomCenter - Size.OnlyX() / 2 - Size.OnlyY() - WindowAlignmentPadding.OnlyY(),
                    MenuBoxWindowAlignment.BottomRight => manager.Screen.Bounds.BottomRight - Size - WindowAlignmentPadding,

                    MenuBoxWindowAlignment.Manual => manualPosition,
                    _ => manualPosition,
                };
            }
            set => manualPosition = value;
        }

        public SpriteFont? SmallFont = null;
        public Color NormalTextColor = Color.White;
        public Color HighlightTextColor = Color.Green;
        public MenuBoxWindowAlignment WindowAlignment = MenuBoxWindowAlignment.Center;
        public Point2 WindowAlignmentPadding = new(40, 40);
        public MenuBoxWindowSizing WindowSizing = MenuBoxWindowSizing.Automatic;
        public MenuBoxTextAlignment TextAlignment = MenuBoxTextAlignment.Center;
        public bool ShowLegend = true;

        public string MenuTitle = string.Empty;
        public MenuBoxItem[] MenuItems
        {
            get => menuItems;
            set
            {
                menuItems = value;
                currentItemIndex = 0;
            }
        }

        private MenuBoxItem[] menuItems = [];
        private int currentItemIndex = 0;

        enum MenuBoxState { WaitingForInput, InitiateAction, Closed }
        private MenuBoxState currentState = MenuBoxState.Closed;

        public bool IsOpen => currentState != MenuBoxState.Closed;
        public int SelectedIndex => currentItemIndex;

        public void Open()
        {
            currentItemIndex = 0;
            currentState = MenuBoxState.WaitingForInput;
        }

        public void Close()
        {
            currentState = MenuBoxState.Closed;
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Update()
        {
            switch (currentState)
            {
                case MenuBoxState.WaitingForInput:
                    {
                        /* Wait for any input to come in and process it */
                        if (manager.Controls.Move.PressedDown)
                        {
                            /* Go down one item, wrap back to start if needed */
                            currentItemIndex++;
                            if (currentItemIndex > menuItems.Length - 1) currentItemIndex = 0;
                        }
                        else if (manager.Controls.Move.PressedUp)
                        {
                            /* Go up one item, wrap down to end if needed */
                            currentItemIndex--;
                            if (currentItemIndex < 0) currentItemIndex = menuItems.Length - 1;
                        }
                        else if (manager.Controls.Move.PressedRight)
                        {
                            /* Skip to end */
                            currentItemIndex = menuItems.Length - 1;
                        }
                        else if (manager.Controls.Move.PressedLeft)
                        {
                            /* Back to start */
                            currentItemIndex = 0;
                        }
                        else if (manager.Controls.Confirm.ConsumePress())
                        {
                            /* Initiate action associated with selected item */
                            currentState = MenuBoxState.InitiateAction;
                        }
                        else if (manager.Controls.Cancel.ConsumePress())
                        {
                            /* Find first item assigned cancel */
                            var cancelIndex = Array.IndexOf(menuItems, menuItems.FirstOrDefault(x => x.IsCancelAction));
                            if (cancelIndex != -1)
                            {
                                /* If a cancel item was found, either select it, or if it's already selected, initiate its action */
                                if (currentItemIndex == cancelIndex)
                                    currentState = MenuBoxState.InitiateAction;
                                else
                                    currentItemIndex = cancelIndex;
                            }
                        }

                        /* Debug feature; to be removed */
                        if (manager.Settings.ShowDebugInfo && manager.Controls.Menu.ConsumePress())
                        {
                            WindowAlignment++;
                            if (WindowAlignment > MenuBoxWindowAlignment.BottomRight)
                            {
                                WindowAlignment = 0;
                                WindowSizing++;
                                if (WindowSizing > MenuBoxWindowSizing.Automatic)
                                {
                                    WindowSizing = 0;
                                    TextAlignment++;
                                    if (TextAlignment > MenuBoxTextAlignment.Center)
                                        TextAlignment = 0;
                                }
                            }
                        }
                    }
                    break;

                case MenuBoxState.InitiateAction:
                    {
                        /* The action *might* try to close the MenuBox, so set the current state to waiting for input first */
                        currentState = MenuBoxState.WaitingForInput;

                        if (currentItemIndex >= 0 && currentItemIndex < menuItems.Length && menuItems[currentItemIndex]?.Action is Action action)
                            action();
                    }
                    break;

                case MenuBoxState.Closed:
                    /* Signals that MenuBox is closed; does not render in this state */
                    break;
            }
        }

        public void Render()
        {
            if (Font == null) return;

            if (IsOpen && menuItems.Length != 0)
            {
                var menuRect = new Rect(Position, Size);

                manager.Batcher.PushScissor(menuRect.Int());
                RenderBackground();
                RenderText();
                manager.Batcher.PopScissor();

                if (ShowLegend) RenderLegend();

                if (manager.Settings.ShowDebugInfo)
                {
                    manager.Batcher.RectLine(menuRect, 1f, Color.Red);
                    manager.Batcher.Circle(menuRect.Center, 2f, 5, Color.Magenta);
                    manager.Batcher.Text(
                        debugFont,
                        "== MENU BOX DEBUG ==\n" +
                        $"State:{currentState}\n" +
                        $"Index:{currentItemIndex}\n" +
                        $"Position:{Position}\n" +
                        $"Size:{Size}\n" +
                        $"WindowAlignment:{WindowAlignment}\n" +
                        $"WindowAlignmentPadding:{WindowAlignmentPadding}\n" +
                        $"WindowSizing:{WindowSizing}\n" +
                        $"TextAlignment:{TextAlignment}\n",
                        manager.Screen.Bounds.BottomRight - Point2.One,
                        Vector2.One,
                        Color.Yellow);
                }
            }
        }

        private void RenderBackground()
        {
            if (Font == null || GraphicsSheet == null) return;

            var texTL = GraphicsSheet.GetSubtexture("BorderTopLeft");
            var texTC = GraphicsSheet.GetSubtexture("BorderTopCenter");
            var texTR = GraphicsSheet.GetSubtexture("BorderTopRight");
            manager.Batcher.Image(texTL, TopLeft, BackgroundColor);
            manager.Batcher.Image(texTR, new(Right - texTR.Width, Top), BackgroundColor);
            manager.Batcher.ImageStretch(texTC, new(Left + texTL.Width, Top, Width - texTL.Width - texTR.Width, texTC.Height), BackgroundColor);

            var texBL = GraphicsSheet.GetSubtexture("BorderBottomLeft");
            var texBC = GraphicsSheet.GetSubtexture("BorderBottomCenter");
            var texBR = GraphicsSheet.GetSubtexture("BorderBottomRight");
            manager.Batcher.Image(texBL, new(Left, Bottom - texBL.Height), BackgroundColor);
            manager.Batcher.Image(texBR, new(Right - texBR.Width, Bottom - texBR.Height), BackgroundColor);
            manager.Batcher.ImageStretch(texBC, new(Left + texBL.Width, Bottom - texBC.Height, Width - texBL.Width - texBR.Width, texBC.Height), BackgroundColor);

            var texML = GraphicsSheet.GetSubtexture("BorderMiddleLeft");
            var texBG = GraphicsSheet.GetSubtexture("BoxBackground");
            var texMR = GraphicsSheet.GetSubtexture("BorderMiddleRight");
            manager.Batcher.ImageStretch(texML, new(Left, Top + texTL.Height, texML.Width, Height - texTL.Height - texBL.Height), BackgroundColor);
            manager.Batcher.ImageStretch(texMR, new(Right - texMR.Width, Top + texTR.Height, texMR.Width, Height - texTR.Height - texBR.Height), BackgroundColor);
            manager.Batcher.ImageStretch(texBG, new(Left + texTL.Width, Top + texTL.Height, Width - texML.Width - texMR.Width, Height - texTC.Height - texBC.Height), BackgroundColor);
        }

        private void RenderText()
        {
            if (Font == null) return;

            var textPos = new Vector2(Position.X + FramePaddingTopLeft.X, Position.Y + FramePaddingTopLeft.Y);
            var itemSize = (Size - FramePaddingBottomRight - FramePaddingTopLeft).OnlyX() + new Vector2(0f, Font.LineHeight);

            if (!string.IsNullOrWhiteSpace(MenuTitle))
            {
                var offset = (Size - FramePaddingBottomRight - FramePaddingTopLeft).OnlyX() / 2f - Font.SizeOf(MenuTitle).ZeroY() / 2f;
                manager.Batcher.Text(Font, MenuTitle, textPos + offset, NormalTextColor);
                textPos.Y += Font.LineHeight + LinePadding;

                var lineFrom = textPos - new Vector2(LinePadding / 1.75f, 0f);
                var lineTo = textPos + itemSize.ZeroY() + new Vector2(LinePadding / 1.75f, 0f);
                manager.Batcher.Line(lineFrom, lineTo - Vector2.One.ZeroY(), 1f, Color.Lerp(BackgroundColor, Color.White, 0.35f));
                manager.Batcher.Line(lineFrom + Vector2.One, lineTo, 1f, Color.Lerp(BackgroundColor, Color.Black, 0.35f));
                textPos.Y += titleGap;
            }

            for (var i = 0; i < menuItems.Length; i++)
            {
                var offset = TextAlignment == MenuBoxTextAlignment.Center ? (Size - FramePaddingBottomRight - FramePaddingTopLeft).OnlyX() / 2f - Font.SizeOf(menuItems[i].Label).ZeroY() / 2f : Vector2.Zero;
                var textColor = currentItemIndex == i ? HighlightTextColor : NormalTextColor;
                if (currentItemIndex == i) manager.Batcher.Rect(textPos - new Vector2(LinePadding / 1.75f), itemSize + new Vector2(LinePadding / 1.75f) * 2f, Color.Lerp(BackgroundColor, Color.Black, 0.35f));
                manager.Batcher.Text(Font, menuItems[i].Label, textPos + offset, textColor);
                textPos.Y += Font.LineHeight + LinePadding;
            }
        }

        private void RenderLegend()
        {
            var font = SmallFont ?? Font;
            if (font == null) return;

            manager.Batcher.Text(font,
                $"{manager.Controls.Confirm.Name} -- {string.Join(',', manager.Controls.Confirm.Entries.Select(x => x.Binding.Descriptor))}\n" +
                $"{manager.Controls.Cancel.Name} -- {string.Join(',', manager.Controls.Cancel.Entries.Select(x => x.Binding.Descriptor))}",
                manager.Screen.Bounds.BottomRight - new Vector2(8f, font.LineHeight + 8f), Vector2.One, Color.White);
        }
    }
}
