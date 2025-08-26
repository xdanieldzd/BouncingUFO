using Foster.Framework;
using GameTest1.Game.Actors;
using GameTest1.Game.UI;
using GameTest1.Utilities;
using System.Numerics;

namespace GameTest1.Game.States
{
    public class InGame : IGameState
    {
        private const float screenFadeDuration = 0.75f;
        private const string levelsDialogFile = "Levels";

        private enum State { Initialize, FadeIn, GameIntroduction, GameStartCountdown, MainLogic, GameOver, ShowGameOverMenu, Restart, ExitToMenu, LoadNextLevel }

        private readonly Manager manager;
        private readonly object[] args;

        private readonly ScreenFader screenFader;
        private readonly Camera camera;
        private readonly DialogBox dialogBox;
        private readonly MenuBox menuBox;
        private readonly LevelManager levelManager;

        private readonly MenuBoxItem[] pauseMenuItems = [];
        private readonly MenuBoxItem[] gameOverMenuItems = [];
        private readonly MenuBoxItem nextLevelMenuItem;

        private DialogText? currentDialogText = null;

        private State currentState = State.Initialize;
        private float gameStartCountdown;

        private TimeSpan gameDuration = TimeSpan.Zero;
        private int capsuleCount;

        private float gameOverWaitTimer;

        public InGame(Manager manager, params object[] args)
        {
            this.manager = manager;
            this.args = args;

            screenFader = new(manager);
            camera = new(manager);
            dialogBox = new(manager)
            {
                Font = manager.Assets.LargeFont,
                GraphicsSheet = manager.Assets.UI["DialogBox"],
                FramePaddingTopLeft = (10, 10),
                FramePaddingBottomRight = (12, 12),
                BackgroundColor = new(0x3E4F65),
                NumTextLines = 2
            };

            menuBox = new(manager)
            {
                Font = manager.Assets.LargeFont,
                GraphicsSheet = manager.Assets.UI["DialogBox"],
                FramePaddingTopLeft = (10, 10),
                FramePaddingBottomRight = (12, 12),
                LinePadding = 4,
                BackgroundColor = new(0x3E4F65),
                HighlightTextColor = Color.Lerp(Color.Green, Color.White, 0.35f)
            };

            levelManager = new(manager);

            pauseMenuItems =
            [
                new() { Label = "Continue", Action = (m) => { m.Close(); } },
                new() { Label = "Restart", Action = (m) => { screenFader.Begin(ScreenFadeType.FadeOut, screenFadeDuration, Color.White); currentState = State.Restart; } },
                new() { Label = "Exit to Menu", Action = (m) => { screenFader.Begin(ScreenFadeType.FadeOut, screenFadeDuration, Color.Black); currentState = State.ExitToMenu; } }
            ];

            gameOverMenuItems =
            [
                new() { Label = "Retry", Action = (m) => { screenFader.Begin(ScreenFadeType.FadeOut, screenFadeDuration, Color.White); currentState = State.Restart; } },
                new() { Label = "Exit to Menu", Action = (m) => { screenFader.Begin(ScreenFadeType.FadeOut, screenFadeDuration, Color.Black); currentState = State.ExitToMenu; } }
            ];
            nextLevelMenuItem = new() { Label = "Next Level", Action = (m) => { screenFader.Begin(ScreenFadeType.FadeOut, screenFadeDuration, Color.White); currentState = State.LoadNextLevel; } };

            levelManager.Load([.. this.args.Where(x => x is string).Cast<string>()]);
        }

        public void Update()
        {
            switch (currentState)
            {
                case State.Initialize:
                    {
                        if (!Globals.QuickStart)
                        {
                            levelManager.Reset();
                            gameStartCountdown = 5f;

                            screenFader.Begin(ScreenFadeType.FadeIn, screenFadeDuration, ScreenFader.PreviousColor);
                            currentState = State.FadeIn;
                        }
                        else
                        {
                            levelManager.Load(@"TestMaps\SmallTest2");
                            gameStartCountdown = 0f;

                            if (levelManager.GetFirstActor<Player>() is Player player)
                                player.CurrentState = Player.State.Normal;

                            screenFader.Cancel();
                            currentState = State.MainLogic;
                        }

                        menuBox.MenuTitle = "Paused";
                        menuBox.MenuItems = pauseMenuItems;

                        camera.FollowActor(levelManager.GetFirstActor<Player>());

                        gameDuration = TimeSpan.Zero;
                    }
                    break;

                case State.FadeIn:
                    {
                        if (screenFader.Update())
                        {
                            if (!string.IsNullOrWhiteSpace(levelManager.Map?.IntroID) &&
                                manager.Assets.DialogText[levelsDialogFile].TryGetValue(levelManager.Map.IntroID, out DialogText? dialogText))
                                currentDialogText = dialogText;

                            currentState = State.GameIntroduction;
                        }
                    }
                    break;

                case State.GameIntroduction:
                    {
                        if (!dialogBox.IsOpen)
                        {
                            if (currentDialogText != null)
                                currentDialogText.HasBeenShownOnce = true;

                            currentState = State.GameStartCountdown;
                        }
                    }
                    break;

                case State.GameStartCountdown:
                    {
                        gameStartCountdown = Calc.Approach(gameStartCountdown, 0f, manager.Time.Delta);
                        if (gameStartCountdown <= 0f)
                        {
                            if (levelManager.GetFirstActor<Player>() is Player player)
                                player.CurrentState = Player.State.Normal;

                            currentState = State.MainLogic;
                        }
                    }
                    break;

                case State.MainLogic:
                    {
                        if (!menuBox.IsOpen)
                        {
                            gameDuration += TimeSpan.FromSeconds(manager.Time.Delta);
                            if (levelManager.GetFirstActor<Player>() is Player player && (capsuleCount <= 0 || player.energy <= 0))
                            {
                                gameOverWaitTimer = 2.5f;
                                currentState = State.GameOver;
                            }
                        }

                        if (manager.Controls.Menu.ConsumePress())
                            menuBox.Toggle();
                    }
                    break;

                case State.GameOver:
                    {
                        if (levelManager.GetFirstActor<Player>() is Player player)
                        {
                            player.CurrentState = Player.State.InputDisabled;
                            player.Stop();
                            player.PlayAnimation("WarpOut", false);
                        }

                        gameOverWaitTimer = Calc.Approach(gameOverWaitTimer, 0f, manager.Time.Delta);
                        if (gameOverWaitTimer <= 0f || manager.Controls.Action1.ConsumePress() || manager.Controls.Action2.ConsumePress())
                            currentState = State.ShowGameOverMenu;
                    }
                    break;

                case State.ShowGameOverMenu:
                    {
                        if (!menuBox.IsOpen)
                        {
                            menuBox.MenuTitle = string.Empty;
                            if (capsuleCount == 0)
                                menuBox.MenuItems = [nextLevelMenuItem, .. gameOverMenuItems];
                            else
                                menuBox.MenuItems = [.. gameOverMenuItems];
                            menuBox.Open();
                        }
                    }
                    break;

                case State.Restart:
                    {
                        if (screenFader.Update())
                        {
                            menuBox.Close();

                            levelManager.DestroyAllActors();
                            currentState = State.Initialize;
                        }
                    }
                    break;

                case State.ExitToMenu:
                    {
                        if (screenFader.Update())
                        {
                            menuBox.Close();

                            manager.GameStates.Pop();
                            manager.GameStates.Push(new MainMenu(manager));
                        }
                    }
                    break;

                case State.LoadNextLevel:
                    {
                        if (screenFader.Update())
                        {
                            menuBox.Close();

                            if (levelManager.Advance())
                                currentState = State.Initialize;
                            else
                            {
                                manager.GameStates.Pop();
                                manager.GameStates.Push(new MainMenu(manager));
                            }
                        }
                    }
                    break;
            }

            if (manager.Controls.DebugEditors.ConsumePress())
                manager.GameStates.Push(new Editor(manager));

            if (!menuBox.IsOpen)
            {
                levelManager.Update();

                capsuleCount = levelManager.Actors.Count(x => x is Capsule);
            }

            camera.Update(Globals.ShowDebugInfo ? Point2.Zero : levelManager.SizeInPixels);

            menuBox.Update();
        }

        public void Render()
        {
            manager.Screen.Clear(0x3E4F65);

            RenderMap();
            RenderHUD();

            if (Globals.ShowDebugInfo)
            {
                if (levelManager.GetFirstActor<Player>() is Player player)
                {
                    manager.Batcher.Text(manager.Assets.SmallFont, $"Current hitbox == {player.Position + player.Hitbox.Rectangle}", Vector2.Zero, Color.White);
                    manager.Batcher.Text(manager.Assets.SmallFont, $"{manager.Controls.Move.Name}:{manager.Controls.Move.IntValue} {manager.Controls.Action1.Name}:{manager.Controls.Action1.Down} {manager.Controls.Action2.Name}:{manager.Controls.Action2.Down} {manager.Controls.Menu.Name}:{manager.Controls.Menu.Down} {manager.Controls.DebugDisplay.Name}:{manager.Controls.DebugDisplay.Down}", new Vector2(0f, manager.Screen.Height - manager.Assets.SmallFont.LineHeight), Color.White);

                    var cells = player.GetMapCells();
                    for (var i = 0; i < cells.Length; i++)
                        manager.Batcher.Text(manager.Assets.SmallFont, cells[i].ToString(), new(0f, 60f + i * manager.Assets.SmallFont.LineHeight), Color.CornflowerBlue);

                    manager.Batcher.Text(manager.Assets.SmallFont, $"Camera {camera.Matrix.Translation:0.0000}", new(0f, 25f), Color.Yellow);
                }
            }

            if (currentDialogText is DialogText dialogText && !dialogText.HasBeenShownOnce)
                dialogBox.Print(dialogText.SpeakerName, dialogText.TextStrings);

            menuBox.Render();

            screenFader.Render();
        }

        private void RenderMap()
        {
            manager.Batcher.PushMatrix(camera.Matrix);
            manager.Batcher.PushScissor(Globals.ShowDebugInfo ? null : new(camera.Position, levelManager.SizeInPixels));
            levelManager.Render(Globals.ShowDebugInfo);
            manager.Batcher.PopScissor();
            manager.Batcher.PopMatrix();
        }

        private void RenderHUD()
        {
            var blinkEnergy = true;

            switch (currentState)
            {
                case State.GameStartCountdown:
                    {
                        var startTimer = Math.Floor(gameStartCountdown);
                        var startText = startTimer < 1f ? "GO!!" : startTimer < 4f ? $"{startTimer}" : "GET READY...";
                        manager.Batcher.Text(manager.Assets.FutureFont, startText, manager.Screen.Bounds.Center - manager.Assets.FutureFont.SizeOf(startText + Environment.NewLine) / 2f, Color.White);
                    }
                    break;

                case State.GameOver:
                    {
                        var gameOverText = capsuleCount <= 0 ? "YOU WON!" : "GAME OVER";
                        manager.Batcher.Text(manager.Assets.FutureFont, gameOverText, manager.Screen.Bounds.Center - manager.Assets.FutureFont.SizeOf(gameOverText + Environment.NewLine) / 2f, Color.White);

                        blinkEnergy = false;
                    }
                    break;

                case State.ShowGameOverMenu:
                case State.Restart:
                case State.ExitToMenu:
                    blinkEnergy = false;
                    break;
            }

            manager.Batcher.Text(manager.Assets.FutureFont, $"TIME {gameDuration:mm\\:ss\\:ff}", new(8f), capsuleCount == 0 ? Color.Green : Color.White);
            manager.Batcher.Text(manager.Assets.FutureFont, $"LEFT {capsuleCount:00}", new Vector2(manager.Screen.Bounds.Right - 8f, 8f), new Vector2(1f, 0f), Color.White);
            if (levelManager.GetFirstActor<Player>() is Player player)
                manager.Batcher.Text(manager.Assets.FutureFont, $"ENERGY {player?.energy:00}", new Vector2(manager.Screen.Bounds.Right - 8f, manager.Screen.Bounds.Bottom - 8f - manager.Assets.FutureFont.Size), new Vector2(1f, 1f), blinkEnergy && player?.energy <= 25 && manager.Time.BetweenInterval(0.5) ? Color.Red : Color.White);
        }
    }
}
