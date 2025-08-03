namespace GameTest1.GameStates
{
    public interface IGameState
    {
        void UpdateApp();
        void UpdateUI();
        void Render();
    }
}
