using Foster.Framework;
using GameTest1.Game.Actors;
using GameTest1.Game.Levels;
using GameTest1.Utilities;
using System;
using System.Numerics;
using static Foster.Framework.Aseprite;

namespace GameTest1.GameStates
{
    public class InGame(Manager manager) : GameStateBase(manager), IGameState
    {
        private const float screenFadeDuration = 0.75f;
        private const string startOnMap = "Map1";

        private enum State { Initialize, FadeIn, GameStartCountdown, MainLogic }

        private readonly ScreenFader screenFader = new(manager);

        private readonly List<ActorBase> actors = [];
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
        }

        private void InitializeGame()
        {
            currentMap = manager.Assets.Maps[startOnMap];
            currentTileset = manager.Assets.Tilesets[currentMap.Tileset];

            foreach (var spawn in currentMap.Spawns)
            {
                switch (spawn.ActorType)
                {
                    case "Player": SpawnPlayerActor(spawn); break;
                }
            }
        }

        private void SpawnPlayerActor(Spawn spawn)
        {
            player = new Player(manager);
            player.Position = (spawn.Position * currentTileset?.CellSize ?? Point2.Zero) - player.Hitbox.Rectangle.Position;

            actors.Add(player);
        }

        private void PerformMainLogic()
        {
            foreach (var actor in actors)
                actor.Update();
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
                    manager.Batcher.Text(manager.Assets.PixelFont, $"{manager.Controls.Move.Name}:{manager.Controls.Move.IntValue} {manager.Controls.Action1.Name}:{manager.Controls.Action1.Down} {manager.Controls.Action2.Name}:{manager.Controls.Action2.Down}", new Vector2(0f, manager.Screen.Height - manager.Assets.Font.LineHeight), Color.White);

                    if (currentMap != null && currentTileset != null)
                    {
                        var cells = player.GetCells(currentMap.Size, currentTileset.CellSize);
                        for (var i = 0; i < cells.Length; i++)
                            manager.Batcher.Text(manager.Assets.PixelFont, cells[i].ToString(), new(0f, 40f + i * manager.Assets.Font.LineHeight), Color.CornflowerBlue);
                    }
                }
            }

            screenFader.Render();
        }

        private void RenderMapAndActors()
        {
            manager.MapRenderer.Render(currentMap, currentTileset, Globals.ShowDebugInfo);
            foreach (var actor in actors.Where(x => x.IsVisible))
            {
                actor.Render();
                if (Globals.ShowDebugInfo)
                    actor.Hitbox.Render(manager.Batcher, actor.Position, Color.Red);
            }

            if (Globals.ShowDebugInfo && currentMap != null && currentTileset != null && player != null)
            {
                foreach (var hit in player.GetCells(currentMap.Size, currentTileset.CellSize))
                {
                    var cellPos = new Vector2(hit.X, hit.Y) * currentTileset.CellSize;
                    manager.Batcher.Rect(cellPos, currentTileset.CellSize, Color.FromHexStringRGBA("00003F3F"));
                }
            }
        }
    }
}
