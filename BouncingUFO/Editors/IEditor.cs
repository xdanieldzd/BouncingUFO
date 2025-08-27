namespace BouncingUFO.Editors
{
    internal interface IEditor
    {
        bool IsOpen { get; set; }
        bool IsCollapsed { get; }
        bool IsFocused { get; }

        abstract string Name { get; }

        virtual void Setup() { }
        abstract void Run();
    }
}
