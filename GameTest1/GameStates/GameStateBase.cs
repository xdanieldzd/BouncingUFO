using Foster.Framework;
using GameTest1.Utilities;

namespace GameTest1.GameStates
{
    public abstract class GameStateBase(Manager manager) : IGameState
    {
        public virtual Color ClearColor => Color.DarkGray;
        public virtual float ScreenFadeDuration => 0.75f;
        public virtual Color ScreenFadeInitialColor => Color.Black;

        protected readonly Manager manager = manager;
        protected readonly ScreenFader screenFader = new(manager);

        enum BaseState { Enter, FadeIn, Main, FadeOut }
        private BaseState currentState = BaseState.Enter;

        public void Update()
        {
            switch (currentState)
            {
                case BaseState.Enter:
                    OnEnter();
                    screenFader.Begin(ScreenFadeType.FadeIn, ScreenFadeDuration, ScreenFadeInitialColor);
                    currentState = BaseState.FadeIn;
                    break;

                case BaseState.FadeIn:
                    if (screenFader.Update())
                        currentState = BaseState.Main;
                    break;

                case BaseState.Main:
                    OnUpdateMain();
                    break;

                case BaseState.FadeOut:
                    if (screenFader.Update())
                        OnExit();
                    break;
            }
        }

        public void Render()
        {
            manager.Screen.Clear(ClearColor);
            OnRenderMain();
            screenFader.Render();
        }

        protected void ExitState()
        {
            screenFader.Begin(ScreenFadeType.FadeOut, ScreenFadeDuration, ScreenFader.PreviousColor);
            currentState = BaseState.FadeOut;
        }

        public abstract void OnEnter();
        public abstract void OnUpdateMain();
        public abstract void OnRenderMain();
        public abstract void OnExit();
    }
}
