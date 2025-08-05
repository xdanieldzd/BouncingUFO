using Foster.Framework;
using GameTest1.GameStates;
using GameTest1.Utilities;
using System.Numerics;

using var manager = new GameTest1.Manager();
manager.Run();

namespace GameTest1
{
    public class Manager : App
    {
        public const int DefaultZoom = 2;

        public readonly Batcher Batcher;
        public readonly Target Screen;

        public readonly ImGuiRenderer ImGuiRenderer;
        public readonly Assets Assets;
        public readonly Controls Controls;
        public readonly FrameCounter FrameCounter;

        public readonly MapRenderer MapRenderer;

        public Stack<IGameState> GameStates = [];

        public Manager() : base(new AppConfig()
        {
            ApplicationName = "GameTest1",
            WindowTitle = "Game Test #1 - Bouncing UFO (REWRITE)",
            Width = Globals.NormalStartup ? 480 * DefaultZoom : 1280,
            Height = Globals.NormalStartup ? 272 * DefaultZoom : 720,
            UpdateMode = UpdateMode.UnlockedStep(),
            Resizable = !Globals.NormalStartup
        })
        {
            GraphicsDevice.VSync = true;

            Batcher = new(GraphicsDevice);
            Screen = new(GraphicsDevice, 480, 272, "Screen");

            ImGuiRenderer = new(this);
            Assets = new(GraphicsDevice);
            Controls = new(Input);
            FrameCounter = new();

            MapRenderer = new(this);

            //GameStates.Push(Globals.NormalStartup ? new Intro(this) : new Editor(this));
            GameStates.Push(new Intro(this));
        }

        protected override void Startup() { }

        protected override void Shutdown()
        {
            ImGuiRenderer.Dispose();
        }

        protected override void Update()
        {
            if (Input.Keyboard.Pressed(Keys.Escape)) Exit();

            if (GameStates.TryPeek(out IGameState? gameState))
                gameState.UpdateApp();

            ImGuiRenderer.BeginLayout();

            if (ImGuiRenderer.WantsTextInput) Window.StartTextInput();
            else Window.StopTextInput();

            gameState?.UpdateUI();

            ImGuiRenderer.EndLayout();
        }

        protected override void Render()
        {
            FrameCounter.Update(Time.Delta);

            ClearWindow();
            if (GameStates.TryPeek(out IGameState? gameState)) gameState.Render();
            else Batcher.Text(Assets.Font, "Error: GameState stack is empty!", Vector2.Zero, Color.Red);
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
