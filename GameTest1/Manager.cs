using Foster.Framework;
using GameTest1.Game.States;
using GameTest1.Utilities;
using System.Numerics;

using var manager = new GameTest1.Manager();
manager.Run();

namespace GameTest1
{
    public class Manager : App
    {
        private const string applicationName = nameof(GameTest1);
        private const string windowTitle = "Game Test #1 - Bouncing UFO (REWRITE)";

        private const int defaultScreenWidth = 480;
        private const int defaultScreenHeight = 272;
        private const int defaultWindowScale = 2;

        public readonly Batcher Batcher;
        public readonly Target Screen;

        public readonly ImGuiRenderer ImGuiRenderer;
        public readonly Assets Assets;
        public readonly Controls Controls;

        public readonly Stack<IGameState> GameStates = [];

        public Manager() : base(new AppConfig()
        {
            ApplicationName = applicationName,
            WindowTitle = windowTitle,
            Width = !Globals.StartInEditorMode ? defaultScreenWidth * defaultWindowScale : 1600,
            Height = !Globals.StartInEditorMode ? defaultScreenHeight * defaultWindowScale : 900,
            Resizable = Globals.StartInEditorMode
        })
        {
            GraphicsDevice.VSync = true;

            Batcher = new(GraphicsDevice);
            Screen = new(GraphicsDevice, defaultScreenWidth, defaultScreenHeight, "Screen");

            ImGuiRenderer = new(this);
            Assets = new(GraphicsDevice);
            Controls = new(Input);
        }

        protected override void Startup()
        {
            if (Globals.StartInEditorMode)
                GameStates.Push(new Editor(this));
            else if (Globals.StartInTestGameState)
                GameStates.Push(new Test(this));
            else if (Globals.QuickStart)
                GameStates.Push(new InGame(this));
            else
                GameStates.Push(new TitleScreen(this));
        }

        protected override void Shutdown()
        {
            ImGuiRenderer.Dispose();
        }

        protected override void Update()
        {
            if (Input.Keyboard.Pressed(Keys.Escape)) Exit();
            if (Controls.DebugDisplay.ConsumePress()) Globals.ShowDebugInfo = !Globals.ShowDebugInfo;

            if (GameStates.TryPeek(out IGameState? gameState))
                gameState.Update();

            ImGuiRenderer.BeginLayout();

            if (ImGuiRenderer.WantsTextInput) Window.StartTextInput();
            else Window.StopTextInput();

            gameState?.RenderImGui();

            ImGuiRenderer.EndLayout();
        }

        protected override void Render()
        {
            ClearWindow();

            if (GameStates.TryPeek(out IGameState? gameState))
                gameState.Render();
            else
            {
                Screen.Clear(Color.DarkGray);
                Batcher.Text(Assets.SmallFont, "Error: GameState stack is empty!", Vector2.Zero, Color.Red);
            }

            Batcher.Render(Screen);
            Batcher.Clear();

            RenderScreenToWindow();

            ImGuiRenderer.Render();
        }

        private void ClearWindow()
        {
            Window.Clear(Color.Black);
            Batcher.Render(Window);
            Batcher.Clear();
        }

        private void RenderScreenToWindow()
        {
            var viewport = new Rect(0f, 0f, Window.WidthInPixels, Window.HeightInPixels);
            var scale = MathF.Max(1f, MathF.Floor(Calc.Min(viewport.Size.X / Screen.Width, viewport.Size.Y / Screen.Height)));

            Batcher.PushSampler(new(TextureFilter.Nearest, TextureWrap.Clamp, TextureWrap.Clamp));
            Batcher.Image(Screen, viewport.Center.Floor(), Screen.Bounds.Size / 2f, Vector2.One * scale, 0f, Color.White);
            Batcher.PopSampler();
            Batcher.Render(Window);
            Batcher.Clear();
        }
    }
}
