using Foster.Framework;
using System.Numerics;

namespace GameTest1.Game.UI
{
    public class MenuBox(Manager manager) : UserInterfaceWindow(manager)
    {
        private const float titleGap = 6f;

        public Color NormalTextColor = Color.White;
        public Color HighlightTextColor = Color.Green;

        private string menuTitle = string.Empty;
        private MenuBoxItem[] menuItems = [];
        private int currentItemIndex = 0;

        enum MenuBoxState { Opening, WaitingForInput, PerformingAction, Closed }
        private MenuBoxState currentState = MenuBoxState.Closed;

        public bool IsOpen => currentState != MenuBoxState.Closed;
        public int SelectedIndex => currentItemIndex;

        public void Initialize(string title, IEnumerable<MenuBoxItem> items)
        {
            menuTitle = title;
            menuItems = [.. items];
            currentItemIndex = 0;

            if (Font != null)
            {
                var totalWidth = Font.SizeOf(menuItems.Append(new() { Label = menuTitle }).MaxBy(x => x.Label.Length)?.Label).X + FramePaddingTopLeft.X + FramePaddingBottomRight.X;
                var totalHeight = menuItems.Where(x => !string.IsNullOrWhiteSpace(x.Label)).Count() * (Font.LineHeight + LinePadding) + FramePaddingTopLeft.Y + FramePaddingBottomRight.Y - LinePadding;
                if (!string.IsNullOrWhiteSpace(menuTitle)) totalHeight += Font.LineHeight + LinePadding + titleGap;

                Size = new Vector2(totalWidth, totalHeight).CeilingToPoint2();
                Position = new(manager.Screen.Bounds.Center.X - Size.X / 2, manager.Screen.Bounds.Center.Y - Size.Y / 2);
            }

            currentState = MenuBoxState.WaitingForInput;
        }

        public void Close()
        {
            currentState = MenuBoxState.Closed;
        }

        public void Update()
        {
            switch (currentState)
            {
                case MenuBoxState.Opening:
                    currentItemIndex = 0;
                    break;

                case MenuBoxState.WaitingForInput:
                    if (manager.Controls.Move.PressedDown)
                    {
                        currentItemIndex++;
                        if (currentItemIndex > menuItems.Length - 1) currentItemIndex = 0;
                    }
                    else if (manager.Controls.Move.PressedUp)
                    {
                        currentItemIndex--;
                        if (currentItemIndex < 0) currentItemIndex = menuItems.Length - 1;
                    }
                    else if (manager.Controls.Move.PressedRight)
                    {
                        currentItemIndex = menuItems.Length - 1;
                    }
                    else if (manager.Controls.Move.PressedLeft)
                    {
                        currentItemIndex = 0;
                    }
                    else if (manager.Controls.Action1.ConsumePress() || manager.Controls.Action2.ConsumePress())
                        currentState = MenuBoxState.PerformingAction;
                    break;

                case MenuBoxState.PerformingAction:
                    if (currentItemIndex >= 0 && currentItemIndex < menuItems.Length && menuItems[currentItemIndex]?.Action is Action<MenuBox> action)
                    {
                        action(this);
                        currentState = MenuBoxState.Closed;
                    }
                    else
                        currentState = MenuBoxState.WaitingForInput;
                    break;

                case MenuBoxState.Closed:
                    break;
            }
        }

        public void Render()
        {
            if (Font == null) return;

            if (currentState != MenuBoxState.Opening && currentState != MenuBoxState.Closed)
            {
                RenderBackground();
                RenderText();
            }

            if (Globals.ShowDebugInfo)
            {
                manager.Batcher.RectLine(new(Position, Size), 1f, Color.Red);
                manager.Batcher.Circle(manager.Screen.Bounds.Center, 2f, 5, Color.Magenta);
                manager.Batcher.Text(manager.Assets.SmallFont, $"== DEBUG ==\nState:{currentState}\nIndex:{currentItemIndex}\nPosition:{Position}\nSize:{Size}\n", manager.Screen.Bounds.BottomRight - Point2.One, Vector2.One, Color.Yellow);
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

            if (!string.IsNullOrWhiteSpace(menuTitle))
            {
                var offset = (Size - FramePaddingBottomRight - FramePaddingTopLeft).OnlyX() / 2f - Font.SizeOf(menuTitle).ZeroY() / 2f;
                manager.Batcher.Text(Font, menuTitle, textPos + offset, NormalTextColor);
                textPos.Y += Font.LineHeight + LinePadding;

                var lineFrom = textPos - new Vector2(LinePadding / 1.75f, 0f);
                var lineTo = textPos + itemSize.ZeroY() + new Vector2(LinePadding / 1.75f, 0f);
                manager.Batcher.Line(lineFrom, lineTo - Vector2.One.ZeroY(), 1f, Color.Lerp(BackgroundColor, Color.White, 0.35f));
                manager.Batcher.Line(lineFrom + Vector2.One, lineTo, 1f, Color.Lerp(BackgroundColor, Color.Black, 0.35f));
                textPos.Y += titleGap;
            }

            for (var i = 0; i < menuItems.Length; i++)
            {
                var textColor = currentItemIndex == i ? HighlightTextColor : NormalTextColor;
                if (currentItemIndex == i) manager.Batcher.Rect(textPos - new Vector2(LinePadding / 1.75f), itemSize + new Vector2(LinePadding / 1.75f) * 2f, Color.Lerp(BackgroundColor, Color.Black, 0.35f));
                manager.Batcher.Text(Font, menuItems[i].Label, textPos, textColor);
                textPos.Y += Font.LineHeight + LinePadding;
            }
        }
    }
}
