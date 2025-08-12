using Foster.Framework;
using GameTest1.Utilities;
using System.Numerics;

namespace GameTest1.GameStates
{
    public class Test(Manager manager) : GameStateBase(manager), IGameState
    {
        private readonly DialogBox dialogBox = new(manager) { Font = manager.Assets.Font };

        private readonly string[] dialogText =
        [
            "This is a test! Hello world, I'm a dialog box! This is a looooong line! Yeah, it's very, very long, so all this text needs to somehow fit in our box, you know? It's text wrapping time!",
            "Yeah, still testing stuff, this is yet ANOTHER dialog box! Tho this one's a bit shorter, less wordy!"
        ];

        private int dialogIndex = 0;

        public override void UpdateApp()
        {
            dialogBox.Size = new((int)(manager.Screen.Width / 1.5f), 46);
            dialogBox.Position = new(manager.Screen.Bounds.Center.X - dialogBox.Size.X / 2, manager.Screen.Bounds.Bottom - dialogBox.Size.Y - 16);
        }

        public override void Render()
        {
            manager.Screen.Clear(Color.DarkGray);

            manager.Batcher.Text(manager.Assets.Font, "Hello, I am the Test GameState!", Vector2.Zero, Color.White);

            if (dialogBox.Print(dialogText[dialogIndex]) == DialogBoxResult.Closed)
            {
                dialogIndex++;
                if (dialogIndex == dialogText.Length)
                {
                    manager.GameStates.Pop();
                    manager.GameStates.Push(new Intro(manager));
                }
            }
        }
    }
}
