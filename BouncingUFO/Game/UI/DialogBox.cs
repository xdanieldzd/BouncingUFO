using Foster.Framework;
using System.Numerics;

namespace BouncingUFO.Game.UI
{
    public class DialogBox : UserInterfaceWindow
    {
        public override Point2 Size => new((int)(manager.Screen.Width / 1.25f), (int)((Font?.LineHeight ?? 8) * NumTextLines) + LinePadding * (NumTextLines - 1) + FramePaddingTopLeft.Y + FramePaddingBottomRight.Y);
        public override Point2 Position => new(manager.Screen.Bounds.Center.X - Size.X / 2, manager.Screen.Bounds.Bottom - Size.Y - 16);

        public int NumTextLines = 3;

        public string SpeakerName = string.Empty;
        public List<string> TextStrings = [];

        private int currentTextIndex = 0;
        private readonly List<Queue<(int start, int length)>> textWrapPositions = [];
        private (int start, int totalLength, int currentLength)[] currentTextWrapPositions = [];
        private int currentMaxLine = 0;
        private float charTimer = 0f;

        enum DialogBoxState { Opening, Printing, WaitingForInput, AdvancingText, Closed }
        private DialogBoxState currentState = DialogBoxState.Closed;

        public DialogBox(Manager manager) : base(manager)
        {
            FramePaddingTopLeft = (8, 8);
            FramePaddingBottomRight = (10, 10);
            LinePadding = 6;
        }

        public bool IsOpen => currentState != DialogBoxState.Closed;

        public void Open()
        {
            currentState = DialogBoxState.Opening;
        }

        public void Close()
        {
            currentState = DialogBoxState.Closed;
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Update()
        {
            if (Font == null) return;

            switch (currentState)
            {
                case DialogBoxState.Opening:
                    currentTextIndex = 0;

                    textWrapPositions.Clear();
                    for (var i = 0; i < TextStrings.Count; i++)
                    {
                        textWrapPositions.Add(new());
                        foreach (var wrapPos in Font.WrapText(TextStrings[i], Width - FramePaddingBottomRight.X - FramePaddingTopLeft.X))
                            textWrapPositions[i].Enqueue(wrapPos);
                    }
                    currentMaxLine = 0;
                    currentState = DialogBoxState.AdvancingText;
                    break;

                case DialogBoxState.AdvancingText:
                    if (currentTextIndex < textWrapPositions.Count && textWrapPositions[currentTextIndex].Count > 0)
                    {
                        var lineCount = Math.Min(
                            (int)((Height - FramePaddingBottomRight.Y - FramePaddingTopLeft.Y - LinePadding) / Font?.LineHeight ?? 8),
                            textWrapPositions[currentTextIndex].Count);
                        currentTextWrapPositions = new (int start, int totalLength, int currentLength)[lineCount];
                        for (var i = 0; i < lineCount; i++)
                        {
                            var (start, length) = textWrapPositions[currentTextIndex].Dequeue();
                            currentTextWrapPositions[i] = new(start, length, 0);
                        }
                        currentMaxLine = 0;
                        currentState = DialogBoxState.Printing;
                    }
                    else if (currentTextIndex >= textWrapPositions.Count)
                        currentState = DialogBoxState.Closed;
                    else
                        currentTextIndex++;
                    break;

                case DialogBoxState.Printing:
                    charTimer += manager.Time.Delta;
                    if (charTimer >= 1f / 30f)
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
                    if (manager.Controls.Confirm.ConsumePress() || manager.Controls.Cancel.ConsumePress())
                    {
                        /* Skippa, skippa */
                        for (var i = 0; i < currentTextWrapPositions.Length; i++)
                            currentTextWrapPositions[i].currentLength = currentTextWrapPositions[i].totalLength;
                        currentMaxLine = currentTextWrapPositions.Length;
                    }
                    break;

                case DialogBoxState.WaitingForInput:
                    if (manager.Controls.Confirm.ConsumePress() || manager.Controls.Cancel.ConsumePress())
                        currentState = DialogBoxState.AdvancingText;
                    break;

                case DialogBoxState.Closed:
                    break;
            }
        }

        public void Render()
        {
            if (Font == null || GraphicsSheet == null) return;

            if (IsOpen)
            {
                if (currentTextIndex < TextStrings.Count)
                {
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

                    if (currentState == DialogBoxState.Printing || currentState == DialogBoxState.WaitingForInput)
                    {
                        for (var i = 0; i <= Math.Min(currentMaxLine, currentTextWrapPositions.Length - 1); i++)
                            manager.Batcher.Text(Font, TextStrings[currentTextIndex].AsSpan(currentTextWrapPositions[i].start, currentTextWrapPositions[i].currentLength), Position + new Vector2(FramePaddingTopLeft.X, FramePaddingTopLeft.Y + i * (Font.LineHeight + LinePadding)), Color.White);

                        if (currentState == DialogBoxState.WaitingForInput && manager.Time.BetweenInterval(0.5f))
                        {
                            var texMarker = GraphicsSheet.GetSubtexture(currentTextIndex < textWrapPositions.Count - 1 ? "MarkerNextPage" : "MarkerEndDialog");
                            manager.Batcher.Image(texMarker, new(Right - FramePaddingBottomRight.X - texMarker.Width, Bottom - FramePaddingBottomRight.Y - texMarker.Height), Color.White);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(SpeakerName))
                    {
                        var texIBL = GraphicsSheet.GetSubtexture("InnerBottomLeft");
                        var widthBG = Font.WidthOf(SpeakerName) + Math.Abs(FramePaddingTopLeft.X - texML.Width);
                        manager.Batcher.ImageStretch(texML, new(Left, Top - Font.LineHeight + texBG.Height - LinePadding, texML.Width, Font.LineHeight + LinePadding), BackgroundColor);
                        manager.Batcher.ImageStretch(texBG, new(Left + texML.Width, Top - Font.LineHeight + texBG.Height - LinePadding, widthBG, Font.LineHeight + LinePadding), BackgroundColor);
                        manager.Batcher.ImageStretch(texML, new(Left + texML.Width + widthBG, Top - Font.LineHeight + texBG.Height - LinePadding, texML.Width, Font.LineHeight + LinePadding - texIBL.Height), new(texML.Width, 0f), new Vector2(-1f, 1f), 0f, BackgroundColor);
                        manager.Batcher.Image(texIBL, new(Left + texML.Width + widthBG, Top), BackgroundColor);

                        manager.Batcher.Image(texTL, new(Left, Top - Font.LineHeight - LinePadding), BackgroundColor);
                        manager.Batcher.ImageStretch(texTL, new(Left + texTL.Width + widthBG, Top - Font.LineHeight - LinePadding, texTL.Width, texTL.Height), new(texTL.Width, 0f), new Vector2(-1f, 1f), 0f, BackgroundColor);
                        manager.Batcher.ImageStretch(texTC, new(Left + texTL.Width, Top - Font.LineHeight - LinePadding, widthBG, texTC.Height), BackgroundColor);

                        manager.Batcher.Text(Font, SpeakerName, new(Left + FramePaddingTopLeft.X, Top - Font.LineHeight + texBG.Height - LinePadding), Color.White);
                    }
                }

                if (manager.Settings.ShowDebugInfo)
                {
                    manager.Batcher.RectLine(new RectInt(TopLeft.X + FramePaddingTopLeft.X, TopLeft.Y + FramePaddingTopLeft.Y, Size.X - FramePaddingBottomRight.X - FramePaddingTopLeft.X, Size.Y - FramePaddingBottomRight.Y - FramePaddingTopLeft.Y), 2f, Color.Red);
                    manager.Batcher.Text(manager.Assets.SmallFont, $"{currentState}, {currentMaxLine}", new(0f, manager.Assets.SmallFont.LineHeight), Color.White);
                    for (var i = 0; i < currentTextWrapPositions.Length; i++)
                    {
                        var (start, totalLength, currentLength) = currentTextWrapPositions[i];
                        manager.Batcher.Text(manager.Assets.SmallFont, $"{start}, {totalLength}, {currentLength}", new(0f, (i + 2) * manager.Assets.SmallFont.LineHeight), Color.White);
                    }
                }
            }
        }
    }
}
