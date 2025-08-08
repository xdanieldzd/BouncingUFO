using Foster.Framework;

namespace GameTest1
{
    public class Controls
    {
        public VirtualStick Move { get; private set; }
        public VirtualAction Action1 { get; private set; }
        public VirtualAction Action2 { get; private set; }
        public VirtualAction Menu { get; private set; }
        public VirtualAction Debug { get; private set; }

        public Controls(Input input)
        {
            Move = new(input, nameof(Move), new StickBindingSet().AddArrowKeys().AddDPad().Add(Axes.LeftX, 0.25f, Axes.LeftY, 0.25f, 0.5f));
            Action1 = new(input, nameof(Action1), new ActionBindingSet().Add(Keys.A).Add(Buttons.South));
            Action2 = new(input, nameof(Action2), new ActionBindingSet().Add(Keys.S).Add(Buttons.East));
            Menu = new(input, nameof(Menu), new ActionBindingSet().Add(Keys.Enter).Add(Buttons.Start));
            Debug = new(input, nameof(Debug), new ActionBindingSet().Add(Keys.Space).Add(Buttons.Back));
        }
    }
}
