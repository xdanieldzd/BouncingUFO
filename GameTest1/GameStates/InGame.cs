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
        private const string startOnMap = "SmallTest2";

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

        private readonly Dictionary<string, Type> actorTypeDictionary = new()
        {
            { "Player", typeof(Player) },
            { "Capsule", typeof(Capsule) },
            { "CapsuleSpawner", typeof(CapsuleSpawner) }
        };

        private readonly List<ActorBase> actors = [];
        private readonly List<ActorBase> actorsToDestroy = [];

        private string currentMapName = startOnMap;
        private Map? currentMap;
        private Tileset? currentTileset;

        private State currentState = State.Initialize;
        private float gameStartCountdown;

        private DateTime gameStartTime, gameEndTime;
        private int capsuleCount;

        //

        public ActorBase CreateActor(string actorType, Point2? position = null, int mapLayer = 0, int argument = 0)
        {
            if (actorTypeDictionary.TryGetValue(actorType, out Type? type))
                return CreateActor(type, position, mapLayer, argument);
            else
                throw new ActorException($"Actor type '{actorType}' not recognized");
        }

        public ActorBase CreateActor(Type type, Point2? position = null, int mapLayer = 0, int argument = 0)
        {
            var actor = Activator.CreateInstance(type, manager, this, currentMap, currentTileset, mapLayer, argument) as ActorBase ??
                throw new ActorException(type, "Failed to create actor instance");
            actor.Position = position * currentTileset?.CellSize ?? Point2.One;
            actor.Created();
            return actor;
        }

        public void SpawnActor(ActorBase actor) => actors.Add(actor);
        public void SpawnActor(string actorType, Point2? position = null, int mapLayer = 0, int argument = 0) => actors.Add(CreateActor(actorType, position, mapLayer, argument));
        public void SpawnActor(Type type, Point2? position = null, int mapLayer = 0, int argument = 0) => actors.Add(CreateActor(type, position, mapLayer, argument));

        public void DestroyActor(ActorBase actor)
        {
            if (!actorsToDestroy.Contains(actor))
                actorsToDestroy.Add(actor);
        }

        public IEnumerable<T> GetActors<T>() where T : ActorBase => actors.Where(x => x is T && !actorsToDestroy.Contains(x)).Cast<T>();
        public IEnumerable<ActorBase> GetActors(ActorClass actorClass) => actors.Where(x => x.Class.Has(actorClass) && !actorsToDestroy.Contains(x));

        public T? GetFirstActor<T>() where T : ActorBase => actors.FirstOrDefault(x => x is T && !actorsToDestroy.Contains(x)) as T;
        public ActorBase? GetFirstActor(ActorClass actorClass) => actors.FirstOrDefault(x => x.Class.Has(actorClass) && !actorsToDestroy.Contains(x));

        public ActorBase? GetFirstOverlapActor(Point2 position, RectInt hitboxRect, ActorClass actorClass) => GetFirstOverlapActor(position, hitboxRect, actorClass, actors);
        public ActorBase? GetFirstOverlapActor(ActorBase actor, ActorClass actorClass) => GetFirstOverlapActor(actor.Position, actor.Hitbox.Rectangle, actorClass, actors.Where(x => x != actor));

        private static ActorBase? GetFirstOverlapActor(Point2 position, RectInt hitboxRect, ActorClass actorClass, IEnumerable<ActorBase> actorsToCheck)
        {
            foreach (var other in actorsToCheck)
            {
                if (other.Class.HasFlag(actorClass) &&
                    (hitboxRect + position).Overlaps(other.Hitbox.Rectangle + other.Position))
                    return other;
            }
            return null;
        }

        public void SetCameraFollowActor(ActorBase? actor) => camera.FollowActor(actor);

        public override void UpdateApp()
        {
            var player = GetFirstActor<Player>();

            switch (currentState)
            {
                case State.Initialize:
                    InitializeDialogBox();
                    ResetTimer();

                    if (currentMap == null || currentTileset == null)
                        LoadMap(currentMapName, hasSeenIntroDialog);

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
                        currentMap = null;
                        currentTileset = null;
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

            camera.Update(Globals.ShowDebugInfo ? Point2.Zero : (currentMap?.Size * currentTileset?.CellSize) ?? Point2.Zero);
        }

        private void InitializeDialogBox()
        {
            dialogBox.Size = new((int)(manager.Screen.Width / 1.25f), 64);
            dialogBox.Position = new(manager.Screen.Bounds.Center.X - dialogBox.Size.X / 2, manager.Screen.Bounds.Bottom - dialogBox.Size.Y - 16);
        }

        private void ResetTimer()
        {
            gameStartTime = gameEndTime = DateTime.Now;
        }

        public void LoadMap(string mapName, bool skipIntro = false)
        {
            hasSeenIntroDialog = skipIntro;

            actors.Clear();
            actorsToDestroy.Clear();

            camera.FollowActor(null);

            currentMap = manager.Assets.Maps[currentMapName = mapName];
            currentTileset = manager.Assets.Tilesets[currentMap.Tileset];

            foreach (var spawn in currentMap.Spawns)
                SpawnActor(spawn.ActorType, spawn.Position, spawn.MapLayer, spawn.Argument);
        }

        private void PerformMainLogic()
        {
            gameEndTime = DateTime.Now;

            if (GetFirstActor<Player>() is Player player && (capsuleCount <= 0 || player.energy <= 0))
            {
                currentState = State.GameOver;
                player.CurrentState = Player.State.InputDisabled;
                player.Stop();
                player.PlayAnimation("WarpOut", false);
            }
        }

        public override void Render()
        {
            manager.Screen.Clear(0x3E4F65);

            manager.Batcher.PushMatrix(camera.Matrix);
            manager.MapRenderer.Render(currentMap, currentTileset, actors, Globals.ShowDebugInfo);
            manager.Batcher.PopMatrix();

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
