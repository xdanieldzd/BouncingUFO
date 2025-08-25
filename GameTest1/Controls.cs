using Foster.Framework;

namespace GameTest1
{
    public class Controls
    {
        public readonly VirtualStick Move;
        public readonly VirtualAction Action1;
        public readonly VirtualAction Action2;
        public readonly VirtualAction Menu;
        public readonly VirtualAction DebugDisplay;
        public readonly VirtualAction DebugEditors;

        public Controls(Input input)
        {
            Move = new(input, nameof(Move), new StickBindingSet().AddArrowKeys().AddDPad().Add(Axes.LeftX, 0.25f, Axes.LeftY, 0.25f, 0.5f));
            Action1 = new(input, nameof(Action1), new ActionBindingSet().Add(Keys.A).Add(Buttons.South));
            Action2 = new(input, nameof(Action2), new ActionBindingSet().Add(Keys.S).Add(Buttons.East));
            Menu = new(input, nameof(Menu), new ActionBindingSet().Add(Keys.Enter).Add(Buttons.Start));
            DebugDisplay = new(input, nameof(DebugDisplay), new ActionBindingSet().Add(Keys.Space).Add(Buttons.Back));
            DebugEditors = new(input, nameof(DebugDisplay), new ActionBindingSet().Add(Keys.Backspace));
        }
    }
}
