using Foster.Framework;

namespace BouncingUFO
{
    public class Controls(Input input)
    {
        public readonly VirtualStick Move = new(input, nameof(Move), new StickBindingSet().AddArrowKeys().AddDPad().Add(Axes.LeftX, 0.25f, Axes.LeftY, 0.25f, 0.5f));
        public readonly VirtualAction Confirm = new(input, nameof(Confirm), new ActionBindingSet().Add(Keys.S).Add(Buttons.South));
        public readonly VirtualAction Cancel = new(input, nameof(Cancel), new ActionBindingSet().Add(Keys.A).Add(Buttons.East));
        public readonly VirtualAction Menu = new(input, nameof(Menu), new ActionBindingSet().Add(Keys.Enter).Add(Buttons.Start));
        public readonly VirtualAction DebugDisplay = new(input, nameof(DebugDisplay), new ActionBindingSet().Add(Keys.Space).Add(Buttons.Back));
        public readonly VirtualAction DebugEditors = new(input, nameof(DebugDisplay), new ActionBindingSet().Add(Keys.Backspace));
    }
}
