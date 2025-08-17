using Foster.Framework;
using GameTest1.Game.UI;
using System.Numerics;

namespace GameTest1.Utilities
{
    public class DialogBox(Manager manager)
    {
        public SpriteFont? Font = null;
        public Point2 Position = new(0, 0);
        public Point2 Size = new(256, 64);
        public Point2 FramePadding = new(8, 8);
        public int LinePadding = 6;
        public GraphicsSheet? GraphicsSheet = null;
        public Color BackgroundColor = Color.CornflowerBlue;

        public int Width => Size.X;
        public int Height => Size.Y;
        public int Left => Position.X;
        public int Right => Position.X + Size.X;
        public int Top => Position.Y;
        public int Bottom => Position.Y + Size.Y;
        public Point2 TopLeft => new(Left, Top);
        public Point2 TopRight => new(Right, Top);
        public Point2 BottomLeft => new(Left, Bottom);
        public Point2 BottomRight => new(Right, Bottom);

        public int LinesPerBox => (int)((Height - FramePadding.Y - LinePadding) / Font?.LineHeight ?? 8);

        private string currentText = string.Empty;
        private string? currentSpeaker = string.Empty;
        private readonly Queue<(int start, int length)> textWrapPositions = [];
        private (int start, int totalLength, int currentLength)[] currentTextWrapPositions = [];
        private int currentMaxLine = 0;
        private float charTimer = 0f;

        enum DialogBoxState { Opening, Printing, WaitingForInput, AdvancingText, Closing }
        private DialogBoxState currentState = DialogBoxState.Opening;

        public bool Print(string text, string? speaker = null)
        {
            if (Font == null) return false;

            var result = false;
            switch (currentState)
            {
                case DialogBoxState.Opening:
                    textWrapPositions.Clear();
                    currentSpeaker = speaker;
                    foreach (var wrapPos in Font.WrapText(currentText = text, Width - FramePadding.X * 2))
                        textWrapPositions.Enqueue(wrapPos);
                    currentMaxLine = 0;
                    currentState = DialogBoxState.AdvancingText;
                    break;

                case DialogBoxState.AdvancingText:
                    if (textWrapPositions.Count > 0)
                    {
                        var lineCount = Math.Min(LinesPerBox, textWrapPositions.Count);
                        currentTextWrapPositions = new (int start, int totalLength, int currentLength)[lineCount];
                        for (var i = 0; i < lineCount; i++)
                        {
                            var (start, length) = textWrapPositions.Dequeue();
                            currentTextWrapPositions[i] = new(start, length, 0);
                        }
                        currentMaxLine = 0;
                        currentState = DialogBoxState.Printing;
                    }
                    else
                        currentState = DialogBoxState.Closing;
                    break;

                case DialogBoxState.Printing:
                    charTimer += manager.Time.Delta;
                    if (charTimer >= 0.025f)
                    {
                        charTimer = 0f;

                        if (currentMaxLine >= currentTextWrapPositions.Length)
                            currentState = DialogBoxState.WaitingForInput;
                        else
                        {
                            currentTextWrapPositions[currentMaxLine].currentLength++;
                            if (currentTextWrapPositions[currentMaxLine].currentLength >= currentTextWrapPositions[currentMaxLine].totalLength)
                                currentMaxLine++;
                        }
                    }
                    break;

                case DialogBoxState.WaitingForInput:
                    if (manager.Controls.Action1.ConsumePress() || manager.Controls.Action2.ConsumePress())
                        currentState = DialogBoxState.AdvancingText;
                    break;

                case DialogBoxState.Closing:
                    if (currentText != text || currentSpeaker != speaker)
                        currentState = DialogBoxState.Opening;
                    else
                        result = true;
                    break;
            }

            Render(BackgroundColor);

            if (Globals.ShowDebugInfo)
            {
                manager.Batcher.RectLine(new RectInt(TopLeft + FramePadding, Size - FramePadding * 2), 2f, Color.Red);
                manager.Batcher.Text(manager.Assets.SmallFont, $"{currentState}, {currentMaxLine}, {LinesPerBox}", new(0f, manager.Assets.SmallFont.LineHeight), Color.White);
                for (var i = 0; i < currentTextWrapPositions.Length; i++)
                {
                    var (start, totalLength, currentLength) = currentTextWrapPositions[i];
                    manager.Batcher.Text(manager.Assets.SmallFont, $"{start}, {totalLength}, {currentLength}", new(0f, (i + 2) * manager.Assets.SmallFont.LineHeight), Color.White);
                }
            }

            return result;
        }

        private void Render(Color tint)
        {
            if (Font == null || GraphicsSheet == null) return;

            var texTL = GraphicsSheet.GetSubtexture("BorderTopLeft");
            var texTC = GraphicsSheet.GetSubtexture("BorderTopCenter");
            var texTR = GraphicsSheet.GetSubtexture("BorderTopRight");
            manager.Batcher.Image(texTL, TopLeft, tint);
            manager.Batcher.Image(texTR, new(Right - texTR.Width, Top), tint);
            manager.Batcher.ImageStretch(texTC, new(Left + texTL.Width, Top, Width - texTL.Width - texTR.Width, texTC.Height), tint);

            var texBL = GraphicsSheet.GetSubtexture("BorderBottomLeft");
            var texBC = GraphicsSheet.GetSubtexture("BorderBottomCenter");
            var texBR = GraphicsSheet.GetSubtexture("BorderBottomRight");
            manager.Batcher.Image(texBL, new(Left, Bottom - texBL.Height), tint);
            manager.Batcher.Image(texBR, new(Right - texBR.Width, Bottom - texBR.Height), tint);
            manager.Batcher.ImageStretch(texBC, new(Left + texBL.Width, Bottom - texBC.Height, Width - texBL.Width - texBR.Width, texBC.Height), tint);

            var texML = GraphicsSheet.GetSubtexture("BorderMiddleLeft");
            var texBG = GraphicsSheet.GetSubtexture("BoxBackground");
            var texMR = GraphicsSheet.GetSubtexture("BorderMiddleRight");
            manager.Batcher.ImageStretch(texML, new(Left, Top + texTL.Height, texML.Width, Height - texTL.Height - texBL.Height), tint);
            manager.Batcher.ImageStretch(texMR, new(Right - texMR.Width, Top + texTR.Height, texMR.Width, Height - texTR.Height - texBR.Height), tint);
            manager.Batcher.ImageStretch(texBG, new(Left + texTL.Width, Top + texTL.Height, Width - texML.Width - texMR.Width, Height - texTC.Height - texBC.Height), tint);

            if (currentState == DialogBoxState.Printing || currentState == DialogBoxState.WaitingForInput)
            {
                for (var i = 0; i <= Math.Min(currentMaxLine, currentTextWrapPositions.Length - 1); i++)
                    manager.Batcher.Text(Font, currentText.AsSpan(currentTextWrapPositions[i].start, currentTextWrapPositions[i].currentLength), Position + new Vector2(FramePadding.X, FramePadding.Y + i * (Font.LineHeight + LinePadding)), Color.White);

                if (currentState == DialogBoxState.WaitingForInput && manager.Time.BetweenInterval(0.5f))
                {
                    var texMarker = GraphicsSheet.GetSubtexture(textWrapPositions.Count > 0 ? "MarkerNextPage" : "MarkerEndDialog");
                    manager.Batcher.Image(texMarker, new(Right - FramePadding.X - texMarker.Width, Bottom - FramePadding.Y - texMarker.Height), Color.White);
                }
            }

            if (currentSpeaker != null)
            {
                var texIBL = GraphicsSheet.GetSubtexture("InnerBottomLeft");
                var widthBG = Font.WidthOf(currentSpeaker) + Math.Abs(FramePadding.X - texML.Width);
                manager.Batcher.ImageStretch(texML, new(Left, Top - Font.LineHeight + texBG.Height - LinePadding, texML.Width, Font.LineHeight + LinePadding), tint);
                manager.Batcher.ImageStretch(texBG, new(Left + texML.Width, Top - Font.LineHeight + texBG.Height - LinePadding, widthBG, Font.LineHeight + LinePadding), tint);
                manager.Batcher.ImageStretch(texML, new(Left + texML.Width + widthBG, Top - Font.LineHeight + texBG.Height - LinePadding, texML.Width, Font.LineHeight + LinePadding - texIBL.Height), new(texML.Width, 0f), new Vector2(-1f, 1f), 0f, tint);
                manager.Batcher.Image(texIBL, new(Left + texML.Width + widthBG, Top), tint);

                manager.Batcher.Image(texTL, new(Left, Top - Font.LineHeight - LinePadding), tint);
                manager.Batcher.ImageStretch(texTL, new(Left + texTL.Width + widthBG, Top - Font.LineHeight - LinePadding, texTL.Width, texTL.Height), new(texTL.Width, 0f), new Vector2(-1f, 1f), 0f, tint);
                manager.Batcher.ImageStretch(texTC, new(Left + texTL.Width, Top - Font.LineHeight - LinePadding, widthBG, texTC.Height), tint);

                manager.Batcher.Text(Font, currentSpeaker, new(Left + FramePadding.X, Top - Font.LineHeight + texBG.Height - LinePadding), Color.White);
            }
        }
    }
}
