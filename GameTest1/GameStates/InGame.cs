using Foster.Framework;
using GameTest1.Game.Actors;
using GameTest1.Game.Levels;
using GameTest1.Utilities;
using System.Numerics;

namespace GameTest1.GameStates
{
    public class InGame(Manager manager) : GameStateBase(manager), IGameState
    {
        private const float screenFadeDuration = 0.75f;
        private const string startOnMap = "Map1";

        private enum State { Initialize, FadeIn, GameStartCountdown, MainLogic }

        private readonly ScreenFader screenFader = new(manager);

        private readonly List<ActorBase> actors = [];
        private readonly Player playerActor = new(manager);

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
                    break;

                case State.FadeIn:
                    if (screenFader.Update()) currentState = State.GameStartCountdown;
                    break;

                case State.GameStartCountdown:
                    gameStartCountdown = Calc.Approach(gameStartCountdown, 0f, manager.Time.Delta);
                    if (gameStartCountdown <= 0f)
                    {
                        currentState = State.MainLogic;
                        playerActor.CurrentState = Player.State.Normal;
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

            actors.Add(playerActor);
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
                    manager.Batcher.Text(manager.Assets.Font, $"{manager.Controls.Move.Name}:{manager.Controls.Move.IntValue} {manager.Controls.Action1.Name}:{manager.Controls.Action1.Down} {manager.Controls.Action2.Name}:{manager.Controls.Action2.Down}", Vector2.Zero, Color.White);
                    break;
            }

            screenFader.Render();
        }

        private void RenderMapAndActors()
        {
            manager.MapRenderer.Render(currentMap, currentTileset);
            foreach (var actor in actors.Where(x => x.IsVisible))
                actor.Render();
        }
    }
}
