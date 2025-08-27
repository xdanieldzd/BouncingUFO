namespace BouncingUFO.Game.States
{
    public interface IGameState
    {
        void Update();
        void Render();

        void RenderImGui() { }
    }
}
