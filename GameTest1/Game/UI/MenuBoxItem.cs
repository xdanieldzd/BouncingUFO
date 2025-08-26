namespace GameTest1.Game.UI
{
    public class MenuBoxItem
    {
        public string Label = string.Empty;
        public Action<MenuBox>? Action = null;
        public bool IsCancelAction = false;
    }
}
