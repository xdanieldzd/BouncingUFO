using BouncingUFO.Game.States.Parameters;
using BouncingUFO.Utilities;
using Foster.Framework;

namespace BouncingUFO.Game.States
{
    public abstract class GameStateBase(Manager manager, IGameStateParameters? parameters = default) : IGameState
    {
        public enum FadeMode { UseFadeColor, UsePreviousColor }

        public virtual Color ClearColor { get; set; } = Color.DarkGray;
        public virtual float FadeDuration { get; set; } = 0.5f;
        public virtual Color FadeColor { get; set; } = ScreenFader.PreviousColor;
        public virtual FadeMode FadeInMode { get; set; } = FadeMode.UseFadeColor;
        public virtual FadeMode FadeOutMode { get; set; } = FadeMode.UseFadeColor;

        protected readonly Manager manager = manager;
        protected readonly IGameStateParameters? parameters = parameters;

        private readonly ScreenFader screenFader = new(manager);
        private enum BaseState { EnterState, FadeIn, Main, FadeOut }
        private BaseState currentState = BaseState.EnterState;

        public void Update()
        {
            switch (currentState)
            {
                case BaseState.EnterState:
                    OnEnterState();
                    screenFader.Begin(ScreenFadeType.FadeIn, FadeDuration, FadeInMode == FadeMode.UsePreviousColor ? ScreenFader.PreviousColor : FadeColor);
                    currentState = BaseState.FadeIn;
                    break;

                case BaseState.FadeIn:
                    OnFadeIn();
                    if (screenFader.Update())
                    {
                        OnFadeInComplete();
                        currentState = BaseState.Main;
                    }
                    break;

                case BaseState.Main:
                    OnUpdate();
                    break;

                case BaseState.FadeOut:
                    OnFadeOut();
                    if (screenFader.Update())
                    {
                        OnLeaveState();
                        currentState = BaseState.EnterState;
                    }
                    break;
            }
        }

        public void Render()
        {
            manager.Screen.Clear(ClearColor);
            OnRender();
            screenFader.Render();
        }

        public virtual void RenderImGui() { }

        public void LeaveState()
        {
            OnBeginFadeOut();

            screenFader.Begin(ScreenFadeType.FadeOut, FadeDuration, FadeOutMode == FadeMode.UsePreviousColor ? ScreenFader.PreviousColor : FadeColor);
            currentState = BaseState.FadeOut;
        }

        public abstract void OnEnterState();
        public abstract void OnFadeIn();
        public abstract void OnFadeInComplete();
        public abstract void OnUpdate();
        public abstract void OnBeginFadeOut();
        public abstract void OnFadeOut();
        public abstract void OnLeaveState();

        public abstract void OnRender();
    }
}
