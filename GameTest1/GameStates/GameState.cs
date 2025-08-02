namespace GameTest1.GameStates
{
    public abstract class GameState(Manager manager) : IGameState
    {
        protected readonly Manager manager = manager;

        public abstract void Update();
        public abstract void Render();
    }
}
