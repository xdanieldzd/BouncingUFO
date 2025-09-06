using BouncingUFO.Game.Actors;
using BouncingUFO.Game.UI;
using BouncingUFO.Utilities;
using Foster.Framework;
using System.Numerics;

namespace BouncingUFO.Game.States
{
    public class InGame : GameStateBase
    {
        private const string levelsDialogFile = "InGame";

        private enum State { Initialize, GameIntroduction, GameStartCountdown, MainLogic, GameOver, ShowGameOverMenu, Restart, ExitToMenu, LoadNextLevel }

        private readonly SpriteFont smallFont, largeFont, futureFont;

        private readonly Camera camera;
        private readonly DialogBox dialogBox;
        private readonly MenuBox menuBox;
        private readonly LevelManager levelManager;

        private readonly MenuBoxItem[] pauseMenuItems = [];
        private readonly MenuBoxItem[] gameOverMenuItems = [];
        private readonly MenuBoxItem nextLevelMenuItem;

        private readonly ParallaxBackground parallaxBackground;

        private readonly Queue<DialogText> currentDialogQueue = [];
        private DialogText? currentDialogText = null;

        private State currentState = State.Initialize;
        private float gameStartCountdown;

        private TimeSpan gameDuration = TimeSpan.Zero;
        private int capsuleCount;

        private float gameOverWaitTimer;

        public InGame(Manager manager, params object[] args) : base(manager, args)
        {
            smallFont = manager.Assets.Fonts["SmallFont"];
            largeFont = manager.Assets.Fonts["LargeFont"];
            futureFont = manager.Assets.Fonts["FutureFont"];

            camera = new(manager);
            dialogBox = new(manager)
            {
                Font = largeFont,
                GraphicsSheet = manager.Assets.GraphicsSheets["DialogBox"],
                FramePaddingTopLeft = (10, 10),
                FramePaddingBottomRight = (12, 12),
                BackgroundColor = new(0x3E4F65),
                NumTextLines = 2
            };

            menuBox = new(manager)
            {
                Font = largeFont,
                GraphicsSheet = manager.Assets.GraphicsSheets["DialogBox"],
                FramePaddingTopLeft = (12, 12),
                FramePaddingBottomRight = (14, 14),
                LinePadding = 6,
                BackgroundColor = new(0x3E4F65),
                SmallFont = smallFont,
                HighlightTextColor = Color.Lerp(Color.Green, Color.White, 0.35f),
                ShowLegend = false
            };

            levelManager = new(manager, camera);

            pauseMenuItems =
            [
                new() { Label = "Continue", Action = menuBox.Close },
                new() { Label = "Restart", Action = () => { currentState = State.Restart; LeaveState(); } },
                new() { Label = "Exit to Menu", Action = () => { currentState = State.ExitToMenu; LeaveState(); } }
            ];

            gameOverMenuItems =
            [
                new() { Label = "Retry", Action = () => { currentState = State.Restart; LeaveState(); } },
                new() { Label = "Exit to Menu", Action = () => { currentState = State.ExitToMenu; LeaveState(); } }
            ];
            nextLevelMenuItem = new() { Label = "Next Level", Action = () => { currentState = State.LoadNextLevel; LeaveState(); } };

            parallaxBackground = ParallaxBackground.FromGraphicsSheet(manager, manager.Assets.GraphicsSheets["MainBackground"], [new(2f, 0f), new(4f, 0f), new(6f, 0f), new(8f, 0f)]);

            levelManager.Load([.. this.args.Where(x => x is string).Cast<string>()]);
        }

        public override void OnEnterState()
        {
            if (!Globals.QuickStart)
            {
                levelManager.Reset();
                gameStartCountdown = 5f;
            }
            else
            {
                levelManager.Load(@"TestMaps/BigTestMap");
                gameStartCountdown = 0f;

                if (levelManager.GetFirstActor<Player>() is Player player)
                    player.CurrentState = Player.State.Normal;
            }

            menuBox.MenuTitle = "Paused";
            menuBox.MenuItems = pauseMenuItems;

            camera.FollowActor(levelManager.GetFirstActor<Player>());

            gameDuration = TimeSpan.Zero;

            currentDialogQueue.Clear();

            if (!string.IsNullOrWhiteSpace(levelManager.Map?.IntroID) &&
                manager.Assets.DialogCollections[levelsDialogFile].TryGetValue(levelManager.Map.IntroID, out List<DialogText>? dialogTextList))
            {
                foreach (var dialogText in dialogTextList)
                    currentDialogQueue.Enqueue(dialogText);
            }
        }

        public override void OnFadeIn() => UpdateGame();

        public override void OnFadeInComplete() => currentState = State.GameIntroduction;

        public override void OnUpdate() => UpdateGame();

        public override void OnRender()
        {
            manager.Screen.Clear(0x3E4F65);

            if (levelManager.SizeInPixels.X < manager.Screen.Width || levelManager.SizeInPixels.Y < manager.Screen.Height)
                parallaxBackground.Render();

            RenderMap();
            RenderHUD();

            if (manager.Settings.ShowDebugInfo)
            {
                if (levelManager.GetFirstActor<Player>() is Player player)
                {
                    manager.Batcher.Text(smallFont, $"Current hitbox == {player.Position + player.Hitbox.Rectangle}", Vector2.Zero, Color.White);
                    manager.Batcher.Text(smallFont, $"{manager.Controls.Move.Name}:{manager.Controls.Move.IntValue} {manager.Controls.Confirm.Name}:{manager.Controls.Confirm.Down} {manager.Controls.Cancel.Name}:{manager.Controls.Cancel.Down} {manager.Controls.Menu.Name}:{manager.Controls.Menu.Down} {manager.Controls.DebugDisplay.Name}:{manager.Controls.DebugDisplay.Down}", new Vector2(0f, manager.Screen.Height - smallFont.LineHeight), Color.White);

                    var cells = player.GetMapCells();
                    for (var i = 0; i < cells.Length; i++)
                        manager.Batcher.Text(smallFont, cells[i].ToString(), new(0f, 60f + i * smallFont.LineHeight), Color.CornflowerBlue);

                    manager.Batcher.Text(smallFont, $"Camera {camera.Matrix.Translation:0.0000}", new(0f, 25f), Color.Yellow);
                }
            }

            dialogBox.Render();
            menuBox.Render();
        }

        public override void OnBeginFadeOut()
        {
            switch (currentState)
            {
                case State.Restart:
                case State.LoadNextLevel:
                    FadeColor = Color.White;
                    FadeOutMode = FadeMode.UseFadeColor;
                    break;
                case State.ExitToMenu:
                    FadeColor = Color.Black;
                    FadeOutMode = FadeMode.UseFadeColor;
                    break;
                default:
                    FadeColor = ScreenFader.PreviousColor;
                    FadeOutMode = FadeMode.UsePreviousColor;
                    break;
            }
        }

        public override void OnFadeOut() => UpdateGame();

        public override void OnLeaveState()
        {
            menuBox.Close();

            switch (currentState)
            {
                case State.Restart:
                    levelManager.DestroyAllActors();
                    currentState = State.Initialize;
                    break;

                case State.ExitToMenu:
                    manager.GameStates.Pop();
                    break;

                case State.LoadNextLevel:
                    if (levelManager.Advance())
                        currentState = State.Initialize;
                    else
                        manager.GameStates.Pop();
                    break;
            }
        }

        private void UpdateGame()
        {
            switch (currentState)
            {
                case State.Initialize:
                    {
                        /* Wait ... */
                    }
                    break;

                case State.GameIntroduction:
                    {
                        if (!dialogBox.IsOpen)
                        {
                            if (currentDialogQueue.TryDequeue(out currentDialogText))
                            {
                                if (!currentDialogText.HasBeenShownOnce)
                                {
                                    dialogBox.DialogText = currentDialogText;
                                    dialogBox.Open();

                                    currentDialogText.HasBeenShownOnce = true;
                                }
                            }
                            else
                            {
                                dialogBox.Close();
                                currentState = State.GameStartCountdown;
                            }
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
                            if (levelManager.GetFirstActor<Player>() is Player player && (capsuleCount <= 0 || player.Energy <= 0))
                            {
                                gameOverWaitTimer = 2f;
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
                        if (gameOverWaitTimer <= 0f)
                            currentState = State.ShowGameOverMenu;
                    }
                    break;

                case State.ShowGameOverMenu:
                    {
                        if (!menuBox.IsOpen)
                        {
                            menuBox.MenuTitle = string.Empty;
                            if (capsuleCount == 0 && levelManager.QueuedMaps.Count > 0)
                                menuBox.MenuItems = [nextLevelMenuItem, .. gameOverMenuItems];
                            else
                                menuBox.MenuItems = [.. gameOverMenuItems];
                            menuBox.Open();
                        }
                    }
                    break;
            }

            if (manager.Controls.DebugEditors.ConsumePress())
                manager.GameStates.Push(new Editor(manager));

            parallaxBackground.Update();

            camera.Update(manager.Settings.ShowDebugInfo ? Point2.Zero : levelManager.SizeInPixels);

            if (!menuBox.IsOpen)
            {
                levelManager.Update();
                capsuleCount = levelManager.Actors.Count(x => x is Capsule);
            }

            dialogBox.Update();
            menuBox.Update();
        }

        private void RenderMap()
        {
            var scissorRect = new RectInt(camera.Position, levelManager.SizeInPixels);
            if (levelManager.SizeInPixels.X < manager.Screen.Width || levelManager.SizeInPixels.Y < manager.Screen.Height)
                manager.Batcher.Rect(scissorRect.Inflate(4), Color.Black);

            manager.Batcher.PushMatrix(camera.Matrix);
            manager.Batcher.PushScissor(manager.Settings.ShowDebugInfo ? null : scissorRect);
            levelManager.Render(manager.Settings.ShowDebugInfo);
            manager.Batcher.PopScissor();
            manager.Batcher.PopMatrix();
        }

        private void RenderHUD()
        {
            var blinkEnergyOrShield = true;

            switch (currentState)
            {
                case State.GameStartCountdown:
                    {
                        var startTimer = Math.Floor(gameStartCountdown);
                        var startText = startTimer < 1f ? "GO!!" : startTimer < 4f ? $"{startTimer}" : "GET READY...";
                        manager.Batcher.Text(futureFont, startText, manager.Screen.Bounds.Center - futureFont.SizeOf(startText + Environment.NewLine) / 2f, Color.White);
                    }
                    break;

                case State.GameOver:
                    {
                        var gameOverText = capsuleCount <= 0 ? "YOU WON!" : "GAME OVER";
                        manager.Batcher.Text(futureFont, gameOverText, manager.Screen.Bounds.Center - futureFont.SizeOf(gameOverText + Environment.NewLine) / 2f, Color.White);

                        blinkEnergyOrShield = false;
                    }
                    break;

                case State.ShowGameOverMenu:
                case State.Restart:
                case State.ExitToMenu:
                    blinkEnergyOrShield = false;
                    break;
            }

            manager.Batcher.Text(futureFont, $"TIME {gameDuration:mm\\:ss\\:ff}", new(8f), capsuleCount == 0 ? Color.Green : Color.White);
            manager.Batcher.Text(futureFont, $"LEFT {capsuleCount:00}", new Vector2(manager.Screen.Bounds.Right - 8f, 8f), new Vector2(1f, 0f), Color.White);
            if (levelManager.GetFirstActor<Player>() is Player player)
            {
                manager.Batcher.Text(futureFont, $"SHIELD {player.Shield:00}", new Vector2(8f, manager.Screen.Bounds.Bottom - 8f - futureFont.Size), player.Shield == Player.MaxShield ? Color.CornflowerBlue : blinkEnergyOrShield && player.Shield == 0 && manager.Time.BetweenInterval(0.5) ? Color.Red : Color.White);
                manager.Batcher.Text(futureFont, $"ENERGY {player.Energy:00}", new Vector2(manager.Screen.Bounds.Right - 8f, manager.Screen.Bounds.Bottom - 8f - futureFont.Size), new Vector2(1f, 1f), blinkEnergyOrShield && player.Energy <= Player.MaxEnergy / 3 && manager.Time.BetweenInterval(0.5) ? Color.Red : player.Energy == Player.MaxEnergy ? Color.CornflowerBlue : Color.White);
            }
        }
    }
}
