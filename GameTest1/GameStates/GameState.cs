namespace GameTest1.GameStates
{
    public abstract class GameState(Manager manager) : IGameState
    {
        protected readonly Manager manager = manager;

        public abstract void UpdateApp();
        public virtual void UpdateUI() { }
        public abstract void Render();
    }
}
