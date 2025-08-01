using Foster.Framework;
using System.Numerics;

namespace GameTest1
{
    // https://github.com/FosterFramework/Samples/tree/5b97ca5329e61768f85a45d655da5df7f882519d/TinyLink

    public class Game : App
    {
        private Rect Viewport => new(0f, 0f, Window.WidthInPixels, Window.HeightInPixels);

        private readonly FrameCounter frameCounter = new();

        private readonly Controls controls;

        private readonly Batcher batcher;
        private readonly Target screen;
        private readonly SpriteFont font;

        //

        private const int screenWidth = 480;
        private const int screenHeight = 272;

        private const int playfieldEdges = 16;
        private const int playfieldEdgeTop = 40;

        private const float playerAcceleration = 1500f;
        private const float playerFriction = 100f;
        private const float playerMaxSpeed = 350f;
        private const float playerBounceCooldown = 500f;
        private const float playerSpriteRotation = 10f;

        private readonly RectInt playfieldBounds = new(
            playfieldEdges,
            playfieldEdgeTop,
            screenWidth - playfieldEdges - playfieldEdges,
            screenHeight - playfieldEdges - playfieldEdgeTop);

        private readonly List<Subtexture> playerSprites = [];
        private int playerSpriteIndex = 0;
        private Vector2 playerPosition = Vector2.Zero;
        private Vector2 playerSize = new(32f);
        private RectInt playerHitbox = new(0, 16, 32, 16);
        private Vector2 playerVelocity = Vector2.Zero;
        private Vector2 playerVelocityRemainder = Vector2.Zero;
        private Vector2 playerCurrentBounceCooldown = Vector2.Zero;
        private float playerCurrentSpriteRotation = 0f;

        private readonly List<Subtexture> targetSprites = [];
        private readonly (Vector2 position, int sprite, bool isAlive)[] targetProperties = new (Vector2, int, bool)[50];
        private Vector2 targetSize = new(16f);

        private readonly Texture skyBackground, stageBackground;

        enum GameState { Init, Start, InProgress, End }

        private GameState gameState = GameState.Init;
        private float gameStartTimer;
        private DateTime gameStartTime, gameEndTime;

        //

        public Game() : base(new AppConfig()
        {
            ApplicationName = "GameTest1",
            WindowTitle = "Game Test #1 - Bouncing UFO",
            Width = 1280,
            Height = 720,
            UpdateMode = UpdateMode.UnlockedStep(),
            Resizable = true
        })
        {
            GraphicsDevice.VSync = true;

            controls = new(Input);

            batcher = new(GraphicsDevice);
            screen = new(GraphicsDevice, screenWidth, screenHeight, "Screen");
            font = new(GraphicsDevice, Path.Join("Assets", "Fonts", "monogram-extended.ttf"), 16);

            var playerTexture = new Texture(GraphicsDevice, new Image(Path.Join("Assets", "Sprites", "UFO.png")), "Player");
            for (var i = 0; i < playerTexture.Width / playerSize.X; i++)
                playerSprites.Add(new(playerTexture, new Rect(new(playerSize.X * i, 0f), playerSize)));

            var targetTexture = new Texture(GraphicsDevice, new Image(Path.Join("Assets", "Sprites", "Target.png")), "Target");
            for (var i = 0; i < targetTexture.Width / targetSize.X; i++)
                targetSprites.Add(new(targetTexture, new Rect(new(targetSize.X * i, 0f), targetSize)));

            skyBackground = new Texture(GraphicsDevice, new Image(Path.Join("Assets", "Sprites", "Sky.png")), "Sky");
            stageBackground = new Texture(GraphicsDevice, new Image(Path.Join("Assets", "Sprites", "Stage.png")), "Stage");
        }

        protected override void Startup() { }

        protected override void Shutdown() { }

        protected override void Update()
        {
            if (Input.Keyboard.Pressed(Keys.Escape)) Exit();

            switch (gameState)
            {
                case GameState.Init:
                    playerPosition = screen.Bounds.Center - playerSize / 2f + new Vector2(0f, playfieldBounds.Y);

                    var rngSeed = (ulong)DateTime.Now.Ticks;
#if DEBUG
                    rngSeed = 0x801197e3;
#endif
                    SpawnTargets(rngSeed);

                    gameState = GameState.Start;
#if DEBUG
                    gameStartTimer = 0.0f;
#else
                    gameStartTimer = 5.0f;
#endif
                    break;

                case GameState.Start:
                    if (gameStartTimer <= 0f)
                    {
                        gameState = GameState.InProgress;
                        gameStartTime = DateTime.Now;
                    }
                    gameStartTimer -= Time.Delta;
                    break;

                case GameState.InProgress:
                    CalcPlayerVelocityAndRotation(controls.Move.IntValue, controls.Action1.Down, controls.Action2.Down);
                    MovePlayer(playerVelocity * Time.Delta);
                    CalcPlayerBounceCooldown();

                    HandlePlayerTargetCollision();

                    AnimateObjects();

                    gameEndTime = DateTime.Now;
                    break;

                case GameState.End:
                    break;
            }
        }

        private void SpawnTargets(ulong rngSeed)
        {
            var rng = new Rng(rngSeed);
            var positions = new List<Vector2>();

            for (var i = 0; i < targetProperties.Length; i++)
            {
                var x = MathF.Floor(rng.Float(playfieldBounds.Left, playfieldBounds.Right - targetSize.X));
                var y = MathF.Floor(rng.Float(playfieldBounds.Top, playfieldBounds.Bottom - targetSize.Y));
                positions.Add(new Vector2(x, y));
            }
            positions.Sort((x, y) => x.Y.CompareTo(y.Y));

            for (var i = 0; i < targetProperties.Length; i++)
                targetProperties[i] = new(positions[i], i % 2, true);
        }

        private void CalcPlayerVelocityAndRotation(Point2 direction, bool action1, bool action2)
        {
            var acceleration = playerAcceleration * (action1 ? 5f : 1f) * Time.Delta;

            if (playerCurrentBounceCooldown.X == 0f)
            {
                playerVelocity.X += direction.X * acceleration;

                if (playerVelocity.X < 0) playerCurrentSpriteRotation = -playerSpriteRotation;
                else if (playerVelocity.X > 0) playerCurrentSpriteRotation = playerSpriteRotation;
            }
            if (playerCurrentBounceCooldown.Y == 0f) playerVelocity.Y += direction.Y * acceleration;

            if (MathF.Abs(playerVelocity.X) > playerMaxSpeed) playerVelocity.X = Calc.Approach(playerVelocity.X, MathF.Sign(playerVelocity.X) * playerMaxSpeed, 2000f * Time.Delta);
            if (MathF.Abs(playerVelocity.Y) > playerMaxSpeed) playerVelocity.Y = Calc.Approach(playerVelocity.Y, MathF.Sign(playerVelocity.Y) * playerMaxSpeed, 2000f * Time.Delta);

            var friction = playerFriction * (action2 ? 15f : 1f) * Time.Delta;

            if (direction.X == 0)
            {
                playerVelocity.X = Calc.Approach(playerVelocity.X, 0f, friction);
                playerCurrentSpriteRotation = Calc.Approach(playerCurrentSpriteRotation, 0f, friction);
            }

            if (direction.Y == 0) playerVelocity.Y = Calc.Approach(playerVelocity.Y, 0f, friction);
        }

        private void MovePlayer(Vector2 value)
        {
            playerVelocityRemainder += value;
            var move = (Point2)playerVelocityRemainder;
            playerVelocityRemainder -= move;

            static bool movePixel(Point2 sign, RectInt playfieldBounds, RectInt objectHitbox, ref Vector2 position)
            {
                sign.X = Math.Sign(sign.X);
                sign.Y = Math.Sign(sign.Y);

                var newPosition = position + sign;
                if (!playfieldBounds.Contains(objectHitbox.Translate(newPosition.FloorToPoint2())))
                    return false;

                position = newPosition;
                return true;
            }

            while (move.X != 0)
            {
                var sign = Math.Sign(move.X);
                if (!movePixel(Point2.UnitX * sign, playfieldBounds, playerHitbox, ref playerPosition))
                {
                    playerVelocity.X = -playerVelocity.X;
                    playerVelocityRemainder.X = -playerVelocityRemainder.X;
                    playerCurrentBounceCooldown.X = playerBounceCooldown;
                    break;
                }
                else
                    move.X -= sign;
            }

            while (move.Y != 0)
            {
                var sign = Math.Sign(move.Y);
                if (!movePixel(Point2.UnitY * sign, playfieldBounds, playerHitbox, ref playerPosition))
                {
                    playerVelocity.Y = -playerVelocity.Y;
                    playerVelocityRemainder.Y = -playerVelocityRemainder.Y;
                    playerCurrentBounceCooldown.Y = playerBounceCooldown;
                    break;
                }
                else
                    move.Y -= sign;
            }
        }

        private void CalcPlayerBounceCooldown()
        {
            playerCurrentBounceCooldown.X = MathF.Floor(Calc.Approach(playerCurrentBounceCooldown.X, 0f, Time.Delta));
            playerCurrentBounceCooldown.Y = MathF.Floor(Calc.Approach(playerCurrentBounceCooldown.Y, 0f, Time.Delta));
        }

        private void HandlePlayerTargetCollision()
        {
            var playerRect = new Rect(playerPosition.X, playerPosition.Y + (playerSize.Y / 2f), playerSize.X, playerSize.Y / 2f);
            for (var i = 0; i < targetProperties.Length; i++)
            {
                var (targetPos, _, targetAlive) = targetProperties[i];
                if (!targetAlive) continue;

                var targetRect = new Rect(targetPos.X, targetPos.Y, targetSize.X, targetSize.Y);
                if (playerRect.Overlaps(targetRect))
                    targetProperties[i].isAlive = false;
            }

            if (targetProperties.All(x => !x.isAlive))
                gameState = GameState.End;
        }

        private void AnimateObjects()
        {
            if (Time.OnInterval(0.1f))
            {
                playerSpriteIndex++;
                playerSpriteIndex %= playerSprites.Count;
            }

            if (Time.OnInterval(0.5f))
            {
                for (var i = 0; i < targetProperties.Length; i++)
                {
                    targetProperties[i].sprite++;
                    targetProperties[i].sprite %= targetSprites.Count;
                }
            }
        }

        protected override void Render()
        {
            frameCounter.Update(Time.Delta);

            ClearWindow();
            RenderGameToScreen();
            RenderScreenToWindow();
        }

        private void ClearWindow()
        {
            Window.Clear(Color.Black);
            batcher.Render(Window);
            batcher.Clear();
        }

        private void RenderGameToScreen()
        {
            screen.Clear(0x3E4F65);

            RenderBackground();
            RenderGameObjects();
            RenderBigMessage();
            RenderGameHud();

#if DEBUG
            RenderDebugText();
#endif
            batcher.Render(screen);
            batcher.Clear();
        }

        private void RenderBackground()
        {
            batcher.Image(skyBackground, Color.White);
            batcher.Image(stageBackground, new(0f, playfieldEdgeTop), Color.White);

#if DEBUG
            var skyRect = new Rect(0f, 0f, screen.Width, playfieldEdgeTop);
            batcher.RectLine(skyRect, 2f, Color.Blue);

            batcher.RectLine(playfieldBounds, 2f, Color.Yellow);
#endif
        }

        private void RenderGameObjects()
        {
            foreach (var (targetPos, targetSpriteIdx, targetAlive) in targetProperties)
            {
                if (!targetAlive) continue;

                batcher.Image(targetSprites[targetSpriteIdx], targetPos, Color.White);
#if DEBUG
                var targetRect = new Rect(targetPos.X, targetPos.Y, targetSize.X, targetSize.Y);
                batcher.RectLine(targetRect, 1f, Color.Red);
#endif
            }

            batcher.PushMatrix(
                Matrix3x2.CreateTranslation(-playerSize / 2f) *
                Matrix3x2.CreateRotation(Calc.DegToRad * playerCurrentSpriteRotation) *
                Matrix3x2.CreateTranslation(playerSize / 2f) *
                Matrix3x2.CreateTranslation(playerPosition));
            batcher.Image(playerSprites[playerSpriteIndex], Color.White);
            batcher.PopMatrix();
#if DEBUG
            var playerRect = new Rect(playerPosition.X, playerPosition.Y + (playerSize.Y / 2f), playerSize.X, playerSize.Y / 2f);
            batcher.RectLine(playerRect, 1f, Color.Red);
#endif
        }

        private void RenderBigMessage()
        {
            if (gameState == GameState.Start)
            {
                var timer = MathF.Floor(gameStartTimer);
                var secondText = timer < 1f ? "GO!!" : (timer < 4f ? $"{timer}..." : string.Empty);
                batcher.PushMatrix(Matrix3x2.CreateTranslation(screen.Bounds.Center));
                batcher.PushMatrix(Matrix3x2.CreateTranslation(0f, -5f) * Matrix3x2.CreateScale(4f));
                batcher.Text(font, "Get Ready!", Vector2.One, new(0.5f), Color.Black);
                batcher.Text(font, "Get Ready!", Vector2.Zero, new(0.5f), Color.Yellow);
                batcher.PopMatrix();
                batcher.PushMatrix(Matrix3x2.CreateTranslation(0f, 7.5f) * Matrix3x2.CreateScale(2f));
                batcher.Text(font, secondText, Vector2.One, new(0.5f), Color.Black);
                batcher.Text(font, secondText, Vector2.Zero, new(0.5f), Color.White);
                batcher.PopMatrix();
                batcher.PopMatrix();
            }
            else if (gameState == GameState.End)
            {
                batcher.PushMatrix(Matrix3x2.CreateTranslation(screen.Bounds.Center));
                batcher.PushMatrix(Matrix3x2.CreateTranslation(0f, -5f) * Matrix3x2.CreateScale(4f));
                batcher.Text(font, "GAME OVER!", Vector2.One, new(0.5f), Color.Black);
                batcher.Text(font, "GAME OVER!", Vector2.Zero, new(0.5f), Color.Green);
                batcher.PopMatrix();
                batcher.PushMatrix(Matrix3x2.CreateTranslation(0f, 7.5f) * Matrix3x2.CreateScale(2f));
                batcher.Text(font, $"Your time: {gameEndTime - gameStartTime:mm\\:ss\\:ff}", Vector2.One, new(0.5f), Color.Black);
                batcher.Text(font, $"Your time: {gameEndTime - gameStartTime:mm\\:ss\\:ff}", Vector2.Zero, new(0.5f), Color.White);
                batcher.PopMatrix();
                batcher.PopMatrix();
            }
        }

        private void RenderGameHud()
        {
            batcher.Text(font, $"Time: {gameEndTime - gameStartTime:mm\\:ss\\:ff}", new(9f), Color.Black);
            batcher.Text(font, $"Time: {gameEndTime - gameStartTime:mm\\:ss\\:ff}", new(8f), Color.White);
            batcher.Text(font, $"Left: {targetProperties.Count(x => x.isAlive)}", new Vector2(screen.Width - 7f, 9f), new Vector2(1f, 0f), Color.Black);
            batcher.Text(font, $"Left: {targetProperties.Count(x => x.isAlive)}", new Vector2(screen.Width - 8f, 8f), new Vector2(1f, 0f), Color.White);
        }

        private void RenderDebugText()
        {
            var text = $"Current FPS:{frameCounter.CurrentFps:00.00} Average FPS:{frameCounter.AverageFps:00.00}\nPosition:{playerPosition:0.0000} Velocity:{playerVelocity:0.0000} Cooldown:{playerCurrentBounceCooldown}\n{controls.Move.Name}:{controls.Move.IntValue} {controls.Action1.Name}:{controls.Action1.Down} {controls.Action2.Name}:{controls.Action2.Down}";
            batcher.Text(font, text, new Vector2(9f, screen.Height - 7f), new Vector2(0f, 1f), Color.Black);
            batcher.Text(font, text, new Vector2(8f, screen.Height - 8f), new Vector2(0f, 1f), Color.White);
        }

        private void RenderScreenToWindow()
        {
            var scale = MathF.Max(1f, MathF.Floor(Calc.Min(Viewport.Size.X / screen.Width, Viewport.Size.Y / screen.Height)));

            batcher.PushSampler(new(TextureFilter.Nearest, TextureWrap.Clamp, TextureWrap.Clamp));
            batcher.Image(screen, Viewport.Center.Floor(), screen.Bounds.Size / 2f, Vector2.One * scale, 0f, Color.White);
            batcher.PopSampler();
            batcher.Render(Window);
            batcher.Clear();
        }
    }
}
