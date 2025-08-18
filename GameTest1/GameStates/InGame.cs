using Foster.Framework;
using GameTest1.Game;
using GameTest1.Game.Actors;
using GameTest1.Game.Levels;
using GameTest1.Utilities;
using System.Numerics;

namespace GameTest1.GameStates
{
    public class InGame(Manager manager) : GameStateBase(manager), IGameState
    {
        private const float screenFadeDuration = 0.75f;
        private const string startOnMap = "BigTestMap";

        private enum State { Initialize, FadeIn, GameIntroduction, GameStartCountdown, MainLogic, GameOver, Restart }

        private readonly ScreenFader screenFader = new(manager);
        private readonly Camera camera = new(manager);
        private readonly DialogBox dialogBox = new(manager)
        {
            Font = manager.Assets.LargeFont,
            GraphicsSheet = manager.Assets.UI["DialogBox"],
            FramePadding = new(12, 12),
            BackgroundColor = new(0x3E4F65)
        };

        private bool hasSeenIntroDialog = false;
        private int introDialogIndex = 0;
        private readonly string[] introDialogText =
        [
            "Hello and welcome to \"Bouncing UFO\"!",
            "In this game (if you want to call it that), you control a UFO on a mission on Earth, which has lost its cargo of, erm... \"biological material for research purposes\". Yes.",
            "Your objective is to collect all the floating capsules of cargo around each stage. Not much of a difficult task, you think?",
            "Well, velocity and inertia on Earth are a b*tch, and your fancy spacecraft wasn't built for that stuff! Nor for slamming into earthly geology and structures for that matter, so watch for your shield energy!",
            "In short: Collect the capsules, be smart about accelerating and breaking. That's all there is to it! Which isn't much, I know, but eh, it's my first \"game\" that went anywhere near this far, so I hope you'll enjoy it a bit regardless.",
            "Alright, that's about it, I'll leave you to the game, then! Have fun!"
        ];

        private readonly List<ActorBase> actors = [];
        private readonly List<ActorBase> actorsToDestroy = [];

        private Map? currentMap;
        private Tileset? currentTileset;

        private State currentState = State.Initialize;
        private float gameStartCountdown;

        private DateTime gameStartTime, gameEndTime;
        private int capsuleCount;

        //

        public ActorBase CreateActor(Type type, Point2? position = null)
        {
            var actor = Activator.CreateInstance(type, manager, this, currentMap, currentTileset) as ActorBase ??
                throw new Exception($"Failed to create actor of type {type.Name}");

            actor.Position = position * currentTileset?.CellSize ?? Point2.Zero;
            actor.Created();

            return actor;
        }

        public void DestroyActor(ActorBase actor)
        {
            if (!actorsToDestroy.Contains(actor))
                actorsToDestroy.Add(actor);
        }

        public IEnumerable<T> GetActors<T>() where T : ActorBase => actors.Where(x => x is T && !actorsToDestroy.Contains(x)).Cast<T>();
        public IEnumerable<ActorBase> GetActors(ActorClass actorClass) => actors.Where(x => x.Class.Has(actorClass) && !actorsToDestroy.Contains(x));

        public T? GetFirstActor<T>() where T : ActorBase => actors.FirstOrDefault(x => x is T && !actorsToDestroy.Contains(x)) as T;
        public ActorBase? GetFirstActor(ActorClass actorClass) => actors.FirstOrDefault(x => x.Class.Has(actorClass) && !actorsToDestroy.Contains(x));

        public ActorBase? GetFirstOverlapActor(RectInt rect, ActorClass actorClass)
        {
            foreach (var actor in actors)
            {
                if (actor.Class.HasFlag(actorClass) && rect.Overlaps(actor.Hitbox.Rectangle + actor.Position))
                    return actor;
            }
            return null;
        }

        public void SetCameraFollowActor(ActorBase actor) => camera.FollowActor(actor);

        public override void UpdateApp()
        {
            var player = GetFirstActor<Player>();

            switch (currentState)
            {
                case State.Initialize:
                    InitializeDialogBox();
                    InitializeGame();
                    ResetTimer();

                    screenFader.FadeType = ScreenFadeType.FadeIn;
                    screenFader.Duration = screenFadeDuration;
                    screenFader.Color = ScreenFader.PreviousColor;
                    screenFader.Reset();
                    currentState = State.FadeIn;
                    gameStartCountdown = 5f;

                    if (Globals.QuickStart)
                    {
                        screenFader.Cancel();
                        currentState = State.MainLogic;
                        gameStartCountdown = 0f;
                        hasSeenIntroDialog = true;
                        if (GetFirstActor<Player>() is Player p) p.CurrentState = Player.State.Normal;
                    }
                    break;

                case State.FadeIn:
                    if (screenFader.Update())
                        currentState = State.GameIntroduction;
                    break;

                case State.GameIntroduction:
                    if (hasSeenIntroDialog || introDialogIndex == introDialogText.Length)
                    {
                        hasSeenIntroDialog = true;
                        currentState = State.GameStartCountdown;
                    }
                    break;

                case State.GameStartCountdown:
                    gameStartCountdown = Calc.Approach(gameStartCountdown, 0f, manager.Time.Delta);
                    if (gameStartCountdown <= 0f)
                    {
                        currentState = State.MainLogic;
                        if (player != null) player.CurrentState = Player.State.Normal;
                        ResetTimer();
                    }
                    break;

                case State.MainLogic:
                    PerformMainLogic();
                    break;

                case State.GameOver:
                    if (player != null) player.CurrentState = Player.State.InputDisabled;
                    if (manager.Controls.Action1.ConsumePress() || manager.Controls.Action2.ConsumePress())
                    {
                        screenFader.FadeType = ScreenFadeType.FadeOut;
                        screenFader.Duration = screenFadeDuration;
                        screenFader.Color = Color.White;
                        screenFader.Reset();
                        currentState = State.Restart;
                    }
                    break;

                case State.Restart:
                    if (screenFader.Update())
                    {
                        actorsToDestroy.AddRange(actors);
                        currentState = State.Initialize;
                    }
                    break;
            }

            if (manager.Controls.Menu.ConsumePress())
                manager.GameStates.Push(new Editor(manager));

            for (var i = 0; i < actorsToDestroy.Count; i++)
            {
                actorsToDestroy[i].Destroyed();
                actors.Remove(actorsToDestroy[i]);
            }
            actorsToDestroy.Clear();

            foreach (var actor in actors)
                actor.Update();

            capsuleCount = actors.Count(x => x is Capsule);

            camera.Update(Globals.ShowDebugInfo ? null : currentMap?.Size * currentTileset?.CellSize);
        }

        private void InitializeDialogBox()
        {
            dialogBox.Size = new((int)(manager.Screen.Width / 1.25f), 64);
            dialogBox.Position = new(manager.Screen.Bounds.Center.X - dialogBox.Size.X / 2, manager.Screen.Bounds.Bottom - dialogBox.Size.Y - 16);
        }

        private void InitializeGame()
        {
            currentMap = manager.Assets.Maps[startOnMap];
            currentTileset = manager.Assets.Tilesets[currentMap.Tileset];

            foreach (var spawn in currentMap.Spawns)
            {
                actors.Add(spawn.ActorType switch
                {
                    "Player" => CreateActor(typeof(Player), spawn.Position),
                    "Capsule" => CreateActor(typeof(Capsule), spawn.Position),
                    _ => throw new Exception($"Cannot spawn unknown actor type '{spawn.ActorType}'"),
                });
            }
        }

        private void ResetTimer()
        {
            gameStartTime = gameEndTime = DateTime.Now;
        }

        private void PerformMainLogic()
        {
            gameEndTime = DateTime.Now;

            if (GetFirstActor<Player>() is Player player && (capsuleCount <= 0 || player.energy <= 0))
            {
                currentState = State.GameOver;
                player.Stop();
                player.PlayAnimation("WarpOut", false);
            }
        }

        public override void Render()
        {
            manager.Screen.Clear(0x3E4F65);

            RenderMapAndActors();

            RenderHUD();

            if (Globals.ShowDebugInfo)
            {
                if (GetFirstActor<Player>() is Player player)
                {
                    manager.Batcher.Text(manager.Assets.SmallFont, $"Current hitbox == {player.Position + player.Hitbox.Rectangle}", Vector2.Zero, Color.White);
                    manager.Batcher.Text(manager.Assets.SmallFont, $"{manager.Controls.Move.Name}:{manager.Controls.Move.IntValue} {manager.Controls.Action1.Name}:{manager.Controls.Action1.Down} {manager.Controls.Action2.Name}:{manager.Controls.Action2.Down} {manager.Controls.Menu.Name}:{manager.Controls.Menu.Down} {manager.Controls.Debug.Name}:{manager.Controls.Debug.Down}", new Vector2(0f, manager.Screen.Height - manager.Assets.SmallFont.LineHeight), Color.White);

                    if (currentMap != null && currentTileset != null)
                    {
                        var cells = player.GetMapCells();
                        for (var i = 0; i < cells.Length; i++)
                            manager.Batcher.Text(manager.Assets.SmallFont, cells[i].ToString(), new(0f, 60f + i * manager.Assets.SmallFont.LineHeight), Color.CornflowerBlue);
                    }

                    manager.Batcher.Text(manager.Assets.SmallFont, $"Camera {camera.Matrix.Translation:0.0000}", new(0f, 25f), Color.Yellow);
                }
            }

            if (currentState == State.GameIntroduction && !hasSeenIntroDialog)
            {
                if (dialogBox.Print(introDialogText[introDialogIndex], "xdaniel"))
                    introDialogIndex++;
            }

            screenFader.Render();
        }

        private void RenderMapAndActors()
        {
            manager.Batcher.PushMatrix(camera.Matrix);

            manager.MapRenderer.Render(currentMap, currentTileset, Globals.ShowDebugInfo);
            foreach (var actor in actors.Where(x => x.IsVisible).OrderBy(x => x.DrawPriority))
            {
                actor.Render();
                if (Globals.ShowDebugInfo)
                    actor.Hitbox.Render(manager.Batcher, actor.Position, Color.Red);
            }

            if (Globals.ShowDebugInfo && currentMap != null && currentTileset != null)
            {
                foreach (var spawn in currentMap.Spawns)
                {
                    var spawnPos = new Vector2(spawn.Position.X, spawn.Position.Y) * currentTileset.CellSize;
                    manager.Batcher.Rect(spawnPos, currentTileset.CellSize, new Color(128, 64, 0, 64));
                    manager.Batcher.RectLine(new(spawnPos, currentTileset.CellSize), 2f, new Color(255, 128, 0, 128));
                }

                if (GetFirstActor<Player>() is Player player)
                {
                    foreach (var hit in player.GetMapCells())
                    {
                        var cellPos = new Vector2(hit.X, hit.Y) * currentTileset.CellSize;
                        manager.Batcher.Rect(cellPos, currentTileset.CellSize, new Color(0, 0, 64, 64));
                    }
                }
            }

            manager.Batcher.PopMatrix();
        }

        private void RenderHUD()
        {
            switch (currentState)
            {
                case State.GameStartCountdown:
                    {
                        var startTimer = Math.Floor(gameStartCountdown);
                        var startText = startTimer < 1f ? "GO!!" : (startTimer < 4f ? $"{startTimer}" : "GET READY...");
                        manager.Batcher.Text(manager.Assets.FutureFont, startText, manager.Screen.Bounds.Center - manager.Assets.FutureFont.SizeOf(startText) / 2f, Color.White);
                    }
                    break;

                case State.GameOver:
                    var gameOverText = capsuleCount <= 0 ? "YOU WON!" : "GAME OVER";
                    manager.Batcher.Text(manager.Assets.FutureFont, gameOverText, manager.Screen.Bounds.Center - manager.Assets.FutureFont.SizeOf(gameOverText) / 2f - new Vector2(0f, manager.Assets.FutureFont.Size / 3f), Color.White);

                    var tryAgainText = "\n\nPRESS ACTION TO TRY AGAIN";
                    manager.Batcher.Text(manager.Assets.FutureFont, tryAgainText, manager.Screen.Bounds.Center - manager.Assets.FutureFont.SizeOf(tryAgainText) / 2f + new Vector2(0f, manager.Assets.FutureFont.Size / 3f), Color.White);
                    break;
            }

            manager.Batcher.Text(manager.Assets.FutureFont, $"TIME {gameEndTime - gameStartTime:mm\\:ss\\:ff}", new(8f), currentState == State.GameOver || currentState == State.Restart ? Color.Green : Color.White);
            manager.Batcher.Text(manager.Assets.FutureFont, $"LEFT {capsuleCount:00}", new Vector2(manager.Screen.Bounds.Right - 8f, 8f), new Vector2(1f, 0f), Color.White);
            if (currentState != State.GameOver && currentState != State.Restart && GetFirstActor<Player>() is Player player)
                manager.Batcher.Text(manager.Assets.FutureFont, $"ENERGY {player?.energy:00}", new Vector2(manager.Screen.Bounds.Right - 8f, manager.Screen.Bounds.Bottom - 8f - manager.Assets.FutureFont.Size), new Vector2(1f, 1f), player?.energy < 50 && manager.Time.BetweenInterval(0.5) ? Color.Red : Color.White);
        }
    }
}
