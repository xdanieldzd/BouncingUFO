using Foster.Framework;
using System.Numerics;

namespace GameTest1.Utilities
{
    public enum DialogBoxResult { InvalidFont, Idle, Printing, WaitingForInput, Closed }

    public class DialogBox(Manager manager)
    {
        private readonly static Point2 boxEndIconSize = new(8, 8);

        public SpriteFont? Font = null;
        public Point2 Position = new(0, 0);
        public Point2 Size = new(256, 64);
        public RectInt FramePadding = new(8, 8, 8, 8);
        public int LinePadding = 6;

        public RectInt MainPrintableArea => new(Position + FramePadding.TopLeft, Size - FramePadding.BottomRight);

        private string currentText = string.Empty;
        private string? currentSpeaker = string.Empty;
        private readonly Queue<(int start, int length)> textWrapPositions = [];
        private (int start, int totalLength, int currentLength)[] currentTextWrapPositions = [];
        private int currentMaxLine = 0, linesPerBox = 0;

        enum DialogBoxState { Opening, Printing, WaitingForInput, AdvancingText, Closing }

        private DialogBoxState currentState = DialogBoxState.Opening;
        private float charTimer = 0f;

        public DialogBoxResult Print(string text, string? speaker = null)
        {
            if (Font == null) return DialogBoxResult.InvalidFont;

            linesPerBox = (int)((Size.Y - FramePadding.Bottom - LinePadding) / Font.LineHeight);

            var result = DialogBoxResult.Idle;
            switch (currentState)
            {
                case DialogBoxState.Opening:
                    textWrapPositions.Clear();
                    currentSpeaker = speaker;
                    foreach (var wrapPos in Font.WrapText(currentText = text, Size.X - FramePadding.Right))
                        textWrapPositions.Enqueue(wrapPos);
                    currentMaxLine = 0;
                    currentState = DialogBoxState.AdvancingText;
                    break;

                case DialogBoxState.AdvancingText:
                    if (textWrapPositions.Count > 0)
                    {
                        var lineCount = Math.Min(linesPerBox, textWrapPositions.Count);
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
                    result = DialogBoxResult.Printing;
                    break;

                case DialogBoxState.WaitingForInput:
                    if (manager.Controls.Action1.ConsumePress() || manager.Controls.Action2.ConsumePress())
                        currentState = DialogBoxState.AdvancingText;
                    result = DialogBoxResult.WaitingForInput;
                    break;

                case DialogBoxState.Closing:
                    if (currentText != text || currentSpeaker != speaker)
                        currentState = DialogBoxState.Opening;
                    else
                        result = DialogBoxResult.Closed;
                    break;
            }

            manager.Batcher.Rect(Position, Size, Color.CornflowerBlue);
            if (currentState == DialogBoxState.Printing || currentState == DialogBoxState.WaitingForInput)
            {
                for (var i = 0; i <= Math.Min(currentMaxLine, currentTextWrapPositions.Length - 1); i++)
                    manager.Batcher.Text(Font, currentText.AsSpan(currentTextWrapPositions[i].start, currentTextWrapPositions[i].currentLength), Position + new Vector2(FramePadding.Left, FramePadding.Top + i * (Font.LineHeight + LinePadding)), Color.White);

                if (currentState == DialogBoxState.WaitingForInput && manager.Time.BetweenInterval(0.5f))
                {
                    var iconRect = new RectInt((MainPrintableArea.BottomRight - boxEndIconSize / 2f).FloorToPoint2(), boxEndIconSize);
                    if (textWrapPositions.Count > 0)
                        manager.Batcher.Rect(iconRect, Color.Green);
                    else
                        manager.Batcher.Rect(iconRect, Color.White);
                    manager.Batcher.RectLine(iconRect, 1f, Color.Black);
                }
            }
            manager.Batcher.RectLine(new Rect(Position, Size), 1f, Color.Black);

            if (currentSpeaker != null)
            {
                var speakerRect = new RectInt(Position - new Point2(0, (int)Font.LineHeight - 1) - new Point2(0, FramePadding.Bottom / 2), new Point2((int)Font.WidthOf(currentSpeaker), (int)Font.LineHeight) + FramePadding.BottomRight / 2);
                manager.Batcher.Rect(speakerRect, new(0x87, 0xCE, 0xEB, 0xFF));
                manager.Batcher.Text(Font, currentSpeaker, speakerRect.TopLeft + FramePadding.TopLeft / 2, Color.White);
                manager.Batcher.RectLine(speakerRect, 1f, Color.Black);
            }

            if (false)
            {
                manager.Batcher.RectLine(MainPrintableArea, 2f, Color.Red);
                manager.Batcher.Text(manager.Assets.SmallFont, $"{currentState}, {currentMaxLine}", new(0f, manager.Assets.SmallFont.LineHeight), Color.White);
                for (var i = 0; i < currentTextWrapPositions.Length; i++)
                {
                    var (start, totalLength, currentLength) = currentTextWrapPositions[i];
                    manager.Batcher.Text(manager.Assets.SmallFont, $"{start}, {totalLength}, {currentLength}", new(0f, (i + 2) * manager.Assets.SmallFont.LineHeight), Color.White);
                }
            }

            return result;
        }
    }
}
