namespace GameTest1.GameStates
{
    public abstract class GameStateBase : IGameState
    {
        protected readonly Manager manager;

        public GameStateBase(Manager manager)
        {
            this.manager = manager;
            Initialize();
        }

        public virtual void Initialize() { }
        public abstract void UpdateApp();
        public virtual void UpdateUI() { }
        public abstract void Render();
    }
}
