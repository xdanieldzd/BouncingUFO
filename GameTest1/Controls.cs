using Foster.Framework;

namespace GameTest1
{
    public class Controls(Input input)
    {
        public readonly VirtualStick Move = new(input, nameof(Move), new StickBindingSet().AddArrowKeys().AddDPad().Add(Axes.LeftX, 0.25f, Axes.LeftY, 0.25f, 0.5f));
        public readonly VirtualAction Action1 = new(input, nameof(Action1), new ActionBindingSet().Add(Keys.A).Add(Buttons.South));
        public readonly VirtualAction Action2 = new(input, nameof(Action2), new ActionBindingSet().Add(Keys.S).Add(Buttons.East));
    }
}
