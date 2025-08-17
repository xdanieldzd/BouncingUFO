using Foster.Framework;
using GameTest1.Utilities;
using System.Numerics;

namespace GameTest1.GameStates
{
    public class Test(Manager manager) : GameStateBase(manager), IGameState
    {
        private readonly DialogBox dialogBox = new(manager) { Font = manager.Assets.LargeFont, GraphicsSheet = manager.Assets.UI["DialogBox"] };

        private readonly string[] dialogText =
        [
            "The Quick Brown Fox Jumps Over The Lazy Dog 0123456789!?.,:;+-*/=\\\nTHE QUICK BROWN FOX JUMPS OVER THE LAZY DOG 0123456789!?.,:;+-*/=\\\nthe quick brown fox jumps over the lazy dog 0123456789!?.,:;+-*/=\\",
            "This is a test! Hello world, I'm a dialog box! This is a looooong line! Yeah, it's very long, so all this text needs to somehow fit in our box, right? It's text wrapping time! And we should be getting a box break roundabout here. Ah, see, there it was!",
            "... ... ...\n... ... ...\n... ... ... you're still here? No need to be, I'm done. Here, I'll kick you over to the main game state!\nSee you later!"
        ];

        private int dialogIndex = 0;

        public override void UpdateApp()
        {
            dialogBox.Size = new((int)(manager.Screen.Width / 1.25f), 60);
            dialogBox.Position = new(manager.Screen.Bounds.Center.X - dialogBox.Size.X / 2, manager.Screen.Bounds.Bottom - dialogBox.Size.Y - 16);
        }

        public override void Render()
        {
            manager.Screen.Clear(Color.DarkGray);

            manager.Batcher.Text(manager.Assets.SmallFont, "Hello, I am the Test GameState!", Vector2.Zero, Color.White);

            if (dialogBox.Print(dialogText[dialogIndex], "Char Acter"))
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
