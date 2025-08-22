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
        private const string defaultMapName = "SmallTest2";
        private const string levelsDialogFile = "Levels";

        private enum State { Initialize, LoadMap, ResetMap, FadeIn, GameIntroduction, GameStartCountdown, MainLogic, GameOver, Restart }

        private readonly ScreenFader screenFader = new(manager);
        private readonly Camera camera = new(manager);
        private readonly DialogBox dialogBox = new(manager)
        {
            Font = manager.Assets.LargeFont,
            GraphicsSheet = manager.Assets.UI["DialogBox"],
            FramePadding = new(12, 12),
            BackgroundColor = new(0x3E4F65)
        };

        private readonly Dictionary<string, Type> actorTypeDictionary = new()
        {
            { "Player", typeof(Player) },
            { "Capsule", typeof(Capsule) },
            { "CapsuleSpawner", typeof(CapsuleSpawner) }
        };

        private readonly List<ActorBase> actors = [];
        private readonly List<ActorBase> actorsToDestroy = [];

        private string currentMapName = defaultMapName;
        private Map? currentMap;
        private Tileset? currentTileset;

        private DialogText? currentDialogText = null;

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

                    currentState = State.LoadMap;

                    if (Globals.QuickStart)
                    {
                        LoadMap(currentMapName);
                        ResetMap();
                        screenFader.Cancel();
                        currentState = State.MainLogic;
                        gameStartCountdown = 0f;
                        if (GetFirstActor<Player>() is Player p) p.CurrentState = Player.State.Normal;
                    }
                    break;

                case State.LoadMap:
                    if (currentMap == null || currentTileset == null)
                        LoadMap(currentMapName);

                    currentState = State.ResetMap;
                    break;

                case State.ResetMap:
                    ResetMap();
                    ResetTimer();

                    screenFader.FadeType = ScreenFadeType.FadeIn;
                    screenFader.Duration = screenFadeDuration;
                    screenFader.Color = ScreenFader.PreviousColor;
                    screenFader.Reset();
                    gameStartCountdown = 5f;

                    currentState = State.FadeIn;
                    break;

                case State.FadeIn:
                    if (screenFader.Update())
                    {
                        if (!string.IsNullOrWhiteSpace(currentMap?.IntroID) && manager.Assets.DialogText[levelsDialogFile].TryGetValue(currentMap.IntroID, out DialogText? dialogText))
                            currentDialogText = dialogText;

                        currentState = State.GameIntroduction;
                    }
                    break;

                case State.GameIntroduction:
                    if (!dialogBox.IsOpen)
                    {
                        if (currentDialogText != null)
                            currentDialogText.HasBeenShownOnce = true;

                        currentState = State.GameStartCountdown;
                    }
                    break;

                case State.GameStartCountdown:
                    gameStartCountdown = Calc.Approach(gameStartCountdown, 0f, manager.Time.Delta);
                    if (gameStartCountdown <= 0f)
                    {
                        if (player != null) player.CurrentState = Player.State.Normal;
                        ResetTimer();

                        currentState = State.MainLogic;
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

                        currentState = State.ResetMap;
                    }
                    break;
            }

            if (manager.Controls.DebugEditors.ConsumePress())
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

        public void LoadMap(string mapName)
        {
            currentMap = manager.Assets.Maps[currentMapName = mapName];
            currentTileset = manager.Assets.Tilesets[currentMap.Tileset];
        }

        private void ResetMap()
        {
            if (currentMap == null) return;

            actors.Clear();
            actorsToDestroy.Clear();

            camera.FollowActor(null);

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

            if (manager.Controls.Menu.ConsumePress())
            {
                // TODO
                if (!dialogBox.IsOpen)
                    currentDialogText = new() { SpeakerName = "xdaniel", TextStrings = ["Sorry, the pause menu hasn't been implemented yet!"], HasBeenShownOnce = false };
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
                    manager.Batcher.Text(manager.Assets.SmallFont, $"{manager.Controls.Move.Name}:{manager.Controls.Move.IntValue} {manager.Controls.Action1.Name}:{manager.Controls.Action1.Down} {manager.Controls.Action2.Name}:{manager.Controls.Action2.Down} {manager.Controls.Menu.Name}:{manager.Controls.Menu.Down} {manager.Controls.DebugDisplay.Name}:{manager.Controls.DebugDisplay.Down}", new Vector2(0f, manager.Screen.Height - manager.Assets.SmallFont.LineHeight), Color.White);

                    if (currentMap != null && currentTileset != null)
                    {
                        var cells = player.GetMapCells();
                        for (var i = 0; i < cells.Length; i++)
                            manager.Batcher.Text(manager.Assets.SmallFont, cells[i].ToString(), new(0f, 60f + i * manager.Assets.SmallFont.LineHeight), Color.CornflowerBlue);
                    }

                    manager.Batcher.Text(manager.Assets.SmallFont, $"Camera {camera.Matrix.Translation:0.0000}", new(0f, 25f), Color.Yellow);
                }
            }

            if (currentDialogText is DialogText dialogText && !dialogText.HasBeenShownOnce)
                dialogBox.Print(dialogText.SpeakerName, dialogText.TextStrings);

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
