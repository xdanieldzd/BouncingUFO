using BouncingUFO.Game.States;
using BouncingUFO.Utilities;
using Foster.Framework;
using System.Globalization;
using System.Numerics;
using System.Reflection;

using var manager = new BouncingUFO.Manager();
manager.Run();

namespace BouncingUFO
{
    public class Manager : App
    {
        private const string applicationName = nameof(BouncingUFO);
        private const string windowTitle = "Bouncing UFO";

        private const int defaultScreenWidth = 480;
        private const int defaultScreenHeight = 270;
        private const int defaultWindowScale = 2;

        public readonly static (DateTime DateTime, string UserName, string MachineName) BuildInfo = default;

        public readonly FrameCounter FrameCounter;
        public readonly Batcher Batcher;
        public readonly Target Screen;

        public readonly ImGuiRenderer ImGuiRenderer;
        public readonly Assets Assets;
        public readonly Controls Controls;

        public readonly Stack<IGameState> GameStates = [];

        public Settings Settings = new();

        public Manager() : base(new AppConfig()
        {
            ApplicationName = applicationName,
            WindowTitle = windowTitle,
            Width = !Globals.StartInEditorMode ? defaultScreenWidth * defaultWindowScale : 1600,
            Height = !Globals.StartInEditorMode ? defaultScreenHeight * defaultWindowScale : 900,
            Resizable = Globals.StartInEditorMode,
            UpdateMode = UpdateMode.FixedStep(60, false)
        })
        {
            GraphicsDevice.VSync = true;

            FileSystem.OpenUserStorage((storage) => Settings = storage.DeserializeFromStorage<Settings>(Settings.Filename) ?? new());

            FrameCounter = new(this);
            Batcher = new(GraphicsDevice);
            Screen = new(GraphicsDevice, defaultScreenWidth, defaultScreenHeight, "Screen");

            ImGuiRenderer = new(this);
            Assets = new(this);
            Controls = new(Input);
        }

        static Manager()
        {
            var metadata = Assembly.GetEntryAssembly()?.GetCustomAttributes<AssemblyMetadataAttribute>().ToDictionary(x => x.Key, x => x.Value ?? string.Empty) ?? [];
            BuildInfo.DateTime = metadata.TryGetValue("BuildDate", out var dateTime) ? DateTime.ParseExact(dateTime, "o", CultureInfo.InvariantCulture, DateTimeStyles.None) : default;
            BuildInfo.UserName = metadata.TryGetValue("BuildUserName", out var userName) ? userName : "Unknown";
            BuildInfo.MachineName = metadata.TryGetValue("BuildMachineName", out var machineName) ? machineName : "Unknown";
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
                GameStates.Push(new Intro(this));
        }

        protected override void Shutdown()
        {
            ImGuiRenderer.Dispose();

            FileSystem.OpenUserStorage((storage) => storage.SerializeToStorage(Settings, Settings.Filename));
        }

        protected override void Update()
        {
            if (Settings.Fullscreen != Window.Fullscreen) Window.Fullscreen = Settings.Fullscreen;

            if (Input.Keyboard.Pressed(Keys.Escape)) Exit();
            if (Input.Keyboard.Alt && Input.Keyboard.Pressed(Keys.Enter)) Settings.Fullscreen = !Settings.Fullscreen;

            if (Controls.DebugDisplay.ConsumePress()) Settings.ShowDebugInfo = !Settings.ShowDebugInfo;

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
            FrameCounter.Update();

            ClearWindow();

            if (GameStates.TryPeek(out IGameState? gameState))
                gameState.Render();
            else
            {
                Screen.Clear(Color.DarkGray);
                Batcher.Text(Assets.SmallFont, "Error: GameState stack is empty!", Vector2.Zero, Color.Red);
            }

            if (Settings.ShowFramerate)
                FrameCounter.Render(Vector2.Zero, Assets.SmallFont);

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
