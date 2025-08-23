namespace GameTest1.GameStates
{
    public interface IGameState
    {
        void Initialize();
        void UpdateApp();
        void UpdateUI();
        void Render();
    }
}
