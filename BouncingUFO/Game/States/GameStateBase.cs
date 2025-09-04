using BouncingUFO.Utilities;
using Foster.Framework;

namespace BouncingUFO.Game.States
{
    public abstract class GameStateBase(Manager manager, params object[] args) : IGameState
    {
        public virtual Color ClearColor { get; set; } = Color.DarkGray;
        public virtual float FadeDuration { get; set; } = 0.5f;
        public virtual Color FadeColor { get; set; } = ScreenFader.PreviousColor;

        protected readonly Manager manager = manager;
        protected readonly object[] args = args;

        private readonly ScreenFader screenFader = new(manager);
        private enum BaseState { EnterState, FadeIn, Main, FadeOut }
        private BaseState currentState = BaseState.EnterState;

        protected enum FadeOutMode { UseFadeColor, UsePreviousColor }
        protected FadeOutMode fadeOutColor = FadeOutMode.UseFadeColor;

        public void Update()
        {
            switch (currentState)
            {
                case BaseState.EnterState:
                    OnEnterState();
                    screenFader.Begin(ScreenFadeType.FadeIn, FadeDuration, FadeColor);
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

        public void LeaveState()
        {
            OnBeginFadeOut();

            screenFader.Begin(ScreenFadeType.FadeOut, FadeDuration, fadeOutColor == FadeOutMode.UsePreviousColor ? ScreenFader.PreviousColor : FadeColor);
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
