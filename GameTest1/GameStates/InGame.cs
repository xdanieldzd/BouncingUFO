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

        private enum State { Initialize, FadeIn, GameStartCountdown, MainLogic }

        private readonly ScreenFader screenFader = new(manager);
        private readonly Camera camera = new(manager);

        private readonly List<ActorBase> actors = [];
        private readonly List<ActorBase> actorsToDestroy = [];
        private Player? player;

        private Map? currentMap;
        private Tileset? currentTileset;

        private State currentState = State.Initialize;
        private float gameStartCountdown;

        //

        public override void UpdateApp()
        {
            switch (currentState)
            {
                case State.Initialize:
                    InitializeGame();

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
                        if (player != null) player.CurrentState = Player.State.Normal;
                    }
                    break;

                case State.FadeIn:
                    if (screenFader.Update()) currentState = State.GameStartCountdown;
                    break;

                case State.GameStartCountdown:
                    gameStartCountdown = Calc.Approach(gameStartCountdown, 0f, manager.Time.Delta);
                    if (gameStartCountdown <= 0f)
                    {
                        currentState = State.MainLogic;
                        if (player != null) player.CurrentState = Player.State.Normal;
                    }
                    break;

                case State.MainLogic:
                    PerformMainLogic();
                    break;
            }

            camera.Update(Globals.ShowDebugInfo ? null : currentMap?.Size * currentTileset?.CellSize);
        }

        private void InitializeGame()
        {
            currentMap = manager.Assets.Maps[startOnMap];
            currentTileset = manager.Assets.Tilesets[currentMap.Tileset];

            // TODO: make actor spawning less janky?

            foreach (var spawn in currentMap.Spawns)
            {
                switch (spawn.ActorType)
                {
                    case "Player": SpawnPlayerActor(spawn); break;
                    case "Capsule": SpawnCapsuleActor(spawn); break;
                }
            }
        }

        private void SpawnPlayerActor(Spawn spawn)
        {
            if (currentMap == null || currentTileset == null) return;

            player = new Player(manager, currentMap, currentTileset);
            player.Position = (spawn.Position * currentTileset?.CellSize ?? Point2.Zero) - player.Hitbox.Rectangle.Center / 2;

            camera.FollowActor(player);

            actors.Add(player);
        }

        private void SpawnCapsuleActor(Spawn spawn)
        {
            if (currentMap == null || currentTileset == null) return;

            var capsule = new Capsule(manager, currentMap, currentTileset)
            {
                Position = spawn.Position * currentTileset?.CellSize ?? Point2.Zero
            };
            actors.Add(capsule);
        }

        private void PerformMainLogic()
        {
            for (var i = 0; i < actorsToDestroy.Count; i++)
            {
                actorsToDestroy[i].Destroyed();
                actors.Remove(actorsToDestroy[i]);
            }
            actorsToDestroy.Clear();

            foreach (var actor in actors)
                actor.Update();




            //TEMP TESTING
            if (player != null)
            {
                foreach (var other in actors.Where(x => x != player))
                {
                    if ((player.Hitbox.Rectangle + player.Position).Overlaps(other.Hitbox.Rectangle + other.Position))
                    {
                        player.OnCollisionX();
                        player.OnCollisionY();
                        actorsToDestroy.Add(other);
                    }
                }
            }





            if (manager.Controls.Menu.ConsumePress())
                manager.GameStates.Push(new Editor(manager));
        }

        public override void Render()
        {
            manager.Screen.Clear(0x3E4F65);

            RenderMapAndActors();

            switch (currentState)
            {
                case State.GameStartCountdown:
                    {
                        var timer = Math.Floor(gameStartCountdown);
                        var secondText = timer < 1f ? "GO!!" : (timer < 4f ? $"{timer}..." : "Get Ready!");
                        manager.Batcher.Text(manager.Assets.PixelFont, secondText, manager.Screen.Bounds.Center, Color.White);
                    }
                    break;

                case State.MainLogic:
                    break;
            }

            if (Globals.ShowDebugInfo)
            {
                if (player != null)
                {
                    manager.Batcher.Text(manager.Assets.PixelFont, $"Current hitbox == {player.Position + player.Hitbox.Rectangle}", Vector2.Zero, Color.White);
                    manager.Batcher.Text(manager.Assets.PixelFont, $"{manager.Controls.Move.Name}:{manager.Controls.Move.IntValue} {manager.Controls.Action1.Name}:{manager.Controls.Action1.Down} {manager.Controls.Action2.Name}:{manager.Controls.Action2.Down} {manager.Controls.Menu.Name}:{manager.Controls.Menu.Down} {manager.Controls.Debug.Name}:{manager.Controls.Debug.Down}", new Vector2(0f, manager.Screen.Height - manager.Assets.Font.LineHeight), Color.White);

                    if (currentMap != null && currentTileset != null)
                    {
                        var cells = player.GetMapCells();
                        for (var i = 0; i < cells.Length; i++)
                            manager.Batcher.Text(manager.Assets.PixelFont, cells[i].ToString(), new(0f, 60f + i * manager.Assets.Font.LineHeight), Color.CornflowerBlue);
                    }

                    manager.Batcher.Text(manager.Assets.PixelFont, $"Camera {camera.Matrix.Translation:0.0000}", new(0f, 25f), Color.Yellow);
                }
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

            if (Globals.ShowDebugInfo && currentMap != null && currentTileset != null && player != null)
            {
                foreach (var hit in player.GetMapCells())
                {
                    var cellPos = new Vector2(hit.X, hit.Y) * currentTileset.CellSize;
                    manager.Batcher.Rect(cellPos, currentTileset.CellSize, Color.FromHexStringRGBA("00003F3F"));
                }
            }

            manager.Batcher.PopMatrix();
        }
    }
}
